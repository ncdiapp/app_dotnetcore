using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        private static AppConfigPackDto BuildExportPack(int saasApplicationId, List<int> transactionIds, List<int> searchIds, bool exportAll)
        {
            var pack = new AppConfigPackDto
            {
                SchemaVersion = 1,
                GeneratedAt = DateTime.UtcNow.ToString("o"),
                Source = new AppConfigPackSourceDto
                {
                    GeneratedBy = "export",
                    SaasApplicationId = saasApplicationId
                }
            };

            var txList = AppTransactionBL.RetrieveSaasApplicationTransactionList(saasApplicationId) ?? new List<AppTransactionDto>();
            var searchList = AppSearchConfigBL.RetrieveSaasApplicationSearchList(saasApplicationId) ?? new List<AppSearchDto>();
            if (!exportAll)
            {
                var txIdSet = new HashSet<int>(transactionIds ?? new List<int>());
                txList = txList.Where(t => txIdSet.Contains(Convert.ToInt32(t.Id))).ToList();

                var searchIdSet = new HashSet<int>(searchIds ?? new List<int>());
                searchList = searchList.Where(s => searchIdSet.Contains(Convert.ToInt32(s.Id))).ToList();
            }

            int tenantDataSourceId = GetTenantDataSourceId();
            var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var viewNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var txIdToIntegration = new Dictionary<int, string>();

            using (var conn = OpenTenantConnection())
            {
                foreach (var txDto in txList)
                {
                    int txId = Convert.ToInt32(txDto.Id);
                    var exported = ExportOneTransaction(conn, tenantDataSourceId, txDto, tableNames, viewNames);
                    if (exported != null)
                    {
                        pack.Transactions.Add(exported);
                        txIdToIntegration[txId] = exported.IntegrationId;
                    }
                }

                pack.TransactionGroup = ExportTransactionGroup(conn, saasApplicationId, txIdToIntegration);

                foreach (var searchDto in searchList)
                {
                    var exported = ExportOneSearch(conn, searchDto, txIdToIntegration);
                    if (exported != null)
                        pack.Searches.Add(exported);
                }

                foreach (var tableName in tableNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    var table = ExportOneTable(conn, tenantDataSourceId, tableName);
                    if (table != null)
                        pack.Tables.Add(table);
                }

                foreach (var viewName in viewNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    var view = ExportOneView(conn, viewName);
                    if (view != null)
                        pack.Views.Add(view);
                }
            }

            return pack;
        }

        private static AppConfigPackTransactionDto ExportOneTransaction(
            SqlConnection conn,
            int tenantDataSourceId,
            AppTransactionDto txDto,
            HashSet<string> tableNames,
            HashSet<string> viewNames)
        {
            int txId = Convert.ToInt32(txDto.Id);
            AppTransactionExDto hierarchy;
            try
            {
                hierarchy = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(txId);
            }
            catch
            {
                return null;
            }

            if (hierarchy?.AppTransactionUnitList == null)
                return null;

            string integrationId = GetOrCreateTransactionIntegrationId(conn, txId, txDto.TransactionName);
            var root = hierarchy.AppTransactionUnitList.FirstOrDefault(u => !(u.IsMasterSiblingUnit.HasValue && u.IsMasterSiblingUnit.Value));
            if (root == null)
                root = hierarchy.AppTransactionUnitList.FirstOrDefault();
            if (root == null || string.IsNullOrWhiteSpace(root.DataBaseTableName))
                return null;

            CollectTableOrView(conn, root.DataBaseTableName, tableNames, viewNames);

            var siblingUnits = hierarchy.AppTransactionUnitList
                .Where(u => u.IsMasterSiblingUnit.HasValue && u.IsMasterSiblingUnit.Value && !string.IsNullOrWhiteSpace(u.DataBaseTableName))
                .ToList();

            var exported = new AppConfigPackTransactionDto
            {
                IntegrationId = integrationId,
                Name = txDto.TransactionName,
                Description = txDto.Description,
                FormMode = "Default",
                UnitStructure = new AppConfigPackUnitStructureDto
                {
                    RootTableName = root.DataBaseTableName,
                    RootDisplayName = root.UnitDisplayName,
                    SiblingTableNames = siblingUnits
                        .Select(u => u.DataBaseTableName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    SiblingUnits = siblingUnits
                        .GroupBy(u => u.DataBaseTableName, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .Select(u => new AppConfigPackSiblingUnitDto
                        {
                            TableName = u.DataBaseTableName,
                            DisplayName = u.UnitDisplayName
                        })
                        .ToList(),
                    ChildUnits = (root.Children ?? new List<AppTransactionUnitExDto>())
                        .Where(c => c != null && !string.IsNullOrWhiteSpace(c.DataBaseTableName))
                        .Select(c => ExportChildUnit(conn, c, tableNames, viewNames))
                        .ToList()
                }
            };

            foreach (var sib in exported.UnitStructure.SiblingTableNames)
                CollectTableOrView(conn, sib, tableNames, viewNames);

            int? formId = GetTransactionFormId(conn, txId);
            if (formId.HasValue && exported.UnitStructure.ChildUnits != null)
            {
                foreach (var child in exported.UnitStructure.ChildUnits)
                    ApplyExportedLayoutTab(conn, formId.Value, child);
            }

            exported.Fields = ExportTransactionFields(conn, hierarchy);
            exported.Commands = ExportTransactionCommands(conn, txId, formId);
            if (formId.HasValue)
            {
                exported.FormLayout = ExportFormLayout(conn, formId.Value, txId);
                if (exported.FormLayout != null)
                    exported.FormMode = "Flex";
            }
            return exported;
        }

        private static AppConfigPackChildUnitDto ExportChildUnit(
            SqlConnection conn,
            AppTransactionUnitExDto unit,
            HashSet<string> tableNames,
            HashSet<string> viewNames)
        {
            CollectTableOrView(conn, unit.DataBaseTableName, tableNames, viewNames);
            var grandChildren = (unit.Children ?? new List<AppTransactionUnitExDto>())
                .Where(g => g != null && !string.IsNullOrWhiteSpace(g.DataBaseTableName))
                .ToList();
            foreach (var gc in grandChildren)
                CollectTableOrView(conn, gc.DataBaseTableName, tableNames, viewNames);

            string availableSourceTable = null;
            string selectedColumn = null;
            string sourceColumn = null;
            if (unit.AvailableSourceUnitId.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP 1 DataBaseTableName
FROM dbo.AppTransactionUnit
WHERE TransactionUnitID = @Id";
                    cmd.Parameters.AddWithValue("@Id", unit.AvailableSourceUnitId.Value);
                    availableSourceTable = cmd.ExecuteScalar() as string;
                }

                var mappedField = unit.AppTransactionFieldList?
                    .FirstOrDefault(f => f != null && f.MappingToAvailableSourceUnitTransactionFieldId.HasValue);
                if (mappedField != null)
                {
                    selectedColumn = mappedField.DataBaseFieldName;
                    sourceColumn = ResolveFieldTableColumn(conn, mappedField.MappingToAvailableSourceUnitTransactionFieldId.Value).Column;
                }
            }

            return new AppConfigPackChildUnitDto
            {
                TableName = unit.DataBaseTableName,
                DisplayName = unit.UnitDisplayName,
                GrandChildTableNames = grandChildren.Select(g => g.DataBaseTableName).ToList(),
                GrandChildUnits = grandChildren.Select(g => ExportChildUnit(conn, g, tableNames, viewNames)).ToList(),
                GridDisplayType = unit.EmGridViewDisplayType,
                IsReadOnly = unit.IsReadOnly,
                IsSynchToDatabaseTable = unit.IsSynchToDatabaseTable,
                IsDisableAddButton = unit.IsDisableAddButton,
                IsDisableDeleteButton = unit.IsDisableDeleteButton,
                AvailableSourceTableName = availableSourceTable,
                AvailableSelectSelectedColumn = selectedColumn,
                AvailableSelectSourceColumn = sourceColumn,
                LinkTargets = ExportUnitLinkTargets(conn, Convert.ToInt32(unit.Id))
            };
        }

        private static (string Table, string Column) ResolveFieldTableColumn(SqlConnection conn, int fieldId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 u.DataBaseTableName, f.DataBaseFieldName
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE f.TransactionFieldID = @Id";
                cmd.Parameters.AddWithValue("@Id", fieldId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1));
                    }
                }
            }

            return (null, null);
        }

        private static List<AppConfigPackLinkTargetDto> ExportUnitLinkTargets(SqlConnection conn, int unitId)
        {
            var raw = new List<(string Name, int? ActionType, int TxId, string SourceCol, string TargetCol, int? Sort, bool? IsPopup, int? Width, int? Height)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT NavigationActionName, ActionType, LinkTargetTransactionID, SourceColumn1, TargetColumn1,
       Sort, IsPopup, PopupWidth, PopupHeight
FROM dbo.AppFormLinkTarget
WHERE TransactionUnitID = @UnitId
ORDER BY ISNULL(Sort, 0), LinkTargetID";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(2))
                            continue;
                        raw.Add((
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            reader.GetInt32(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4),
                            reader.IsDBNull(5) ? (int?)null : Convert.ToInt32(reader.GetValue(5)),
                            reader.IsDBNull(6) ? (bool?)null : reader.GetBoolean(6),
                            reader.IsDBNull(7) ? (int?)null : Convert.ToInt32(reader.GetValue(7)),
                            reader.IsDBNull(8) ? (int?)null : Convert.ToInt32(reader.GetValue(8))));
                    }
                }
            }

            var list = new List<AppConfigPackLinkTargetDto>();
            foreach (var row in raw)
            {
                string integrationId = GetTransactionIntegrationId(conn, row.TxId);
                if (string.IsNullOrWhiteSpace(integrationId))
                    continue;

                string action = "Edit";
                if (row.ActionType == (int)EmAppLinkTargetActionType.Create)
                    action = "Create";
                else if (row.ActionType == (int)EmAppLinkTargetActionType.Delete)
                    action = "Delete";

                list.Add(new AppConfigPackLinkTargetDto
                {
                    Name = row.Name,
                    ActionType = action,
                    TransactionIntegrationId = integrationId,
                    SourceColumn = row.SourceCol,
                    TargetColumn = row.TargetCol,
                    Sort = row.Sort,
                    IsPopup = row.IsPopup,
                    PopupWidth = row.Width,
                    PopupHeight = row.Height
                });
            }

            return list.Count == 0 ? null : list;
        }

        private static void CollectTableOrView(
            SqlConnection conn,
            string name,
            HashSet<string> tableNames,
            HashSet<string> viewNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            if (IsView(conn, name, "dbo"))
                viewNames.Add(name.Trim());
            else
                tableNames.Add(name.Trim());
        }

        private static List<AppConfigPackFieldDto> ExportTransactionFields(SqlConnection conn, AppTransactionExDto hierarchy)
        {
            var fields = new List<AppConfigPackFieldDto>();
            var units = new List<AppTransactionUnitExDto>();
            foreach (var unit in hierarchy.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>())
            {
                CollectUnitsRecursive(unit, units);
            }

            foreach (var unit in units)
            {
                if (string.IsNullOrWhiteSpace(unit.DataBaseTableName) || unit.AppTransactionFieldList == null)
                    continue;

                foreach (var field in unit.AppTransactionFieldList)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.DataBaseFieldName))
                        continue;
                    if (field.IsPrimaryKey == true || field.IsLinkToParentPrimaryKey)
                        continue;

                    string matrixTable = null;
                    string matrixColumn = null;
                    if (field.MatrixForeignKeyFieldId.HasValue)
                    {
                        var matrix = ResolveFieldTableColumn(conn, field.MatrixForeignKeyFieldId.Value);
                        matrixTable = matrix.Table;
                        matrixColumn = matrix.Column;
                    }

                    string dependsOnTable = null;
                    string dependsOnColumn = null;
                    if (field.DdlparentLevelId.HasValue)
                    {
                        var parent = ResolveFieldTableColumn(conn, field.DdlparentLevelId.Value);
                        dependsOnTable = parent.Table;
                        dependsOnColumn = parent.Column;
                    }

                    fields.Add(new AppConfigPackFieldDto
                    {
                        TableName = unit.DataBaseTableName,
                        ColumnName = field.DataBaseFieldName,
                        DisplayName = field.DisplayName,
                        ControlType = field.ControlType,
                        EntityCode = string.IsNullOrWhiteSpace(field.DdlQueryText) ? GetEntityCodeById(conn, field.EntityId) : null,
                        IsVisible = field.IsVisible,
                        IsReadOnly = field.IsReadonly,
                        IsPivotRow = field.IsPivotRow,
                        IsPivotColumn = field.IsPivotColumn,
                        IsPivotValue = field.IsPivotValue,
                        MatrixSourceTable = matrixTable,
                        MatrixSourceColumn = matrixColumn,
                        DependsOnTable = dependsOnTable,
                        DependsOnColumn = dependsOnColumn,
                        CascadingRelationTable = field.CascadingRelationTable,
                        CascadingRelationSchemaOwner = field.CascadingRelationTableSchemaOwner,
                        CascadingParentKey = field.CascadingRelationTableParentKeyField,
                        CascadingChildKey = field.CascadingRelationTableChildKeyField,
                        SortOrder = field.SortOrder,
                        NbDecimal = field.Nbdecimal.HasValue && field.Nbdecimal.Value > 0 ? field.Nbdecimal : null,
                        DdlQueryText = string.IsNullOrWhiteSpace(field.DdlQueryText) ? null : field.DdlQueryText,
                        DdlQueryParameterColumns = ExportDdlQueryParameterColumns(conn, field.WhereClauseExpress)
                    });
                }
            }

            return fields;
        }

        private static List<string> ExportDdlQueryParameterColumns(SqlConnection conn, string whereClauseExpress)
        {
            if (string.IsNullOrWhiteSpace(whereClauseExpress))
                return null;
            var cols = new List<string>();
            foreach (var part in whereClauseExpress.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part.Trim(), out int fieldId))
                    continue;
                var loc = ResolveFieldTableColumn(conn, fieldId);
                if (string.IsNullOrWhiteSpace(loc.Table) || string.IsNullOrWhiteSpace(loc.Column))
                    continue;
                cols.Add(loc.Table + "." + loc.Column);
            }
            return cols.Count == 0 ? null : cols;
        }

        private static int? GetTransactionFormId(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT FormID FROM dbo.AppTransaction WHERE TransactionID = @Id";
                cmd.Parameters.AddWithValue("@Id", transactionId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void ApplyExportedLayoutTab(SqlConnection conn, int formId, AppConfigPackChildUnitDto child)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.TableName))
                return;
            child.LayoutTab = ExportUnitLayoutTab(conn, formId, child.TableName);
            foreach (var grand in child.GrandChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                ApplyExportedLayoutTab(conn, formId, grand);
        }

        private static string ExportUnitLayoutTab(SqlConnection conn, int formId, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
WITH walk AS (
    SELECT i.FormLayoutItemID, i.UIGridLayoutParentID, i.ParameterKeyValue, i.DisplayTitle, 0 AS Lvl
    FROM dbo.AppFormLayoutItem i
    INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = i.GridTransactionUnitID
    WHERE i.FormID = @FormId AND u.DataBaseTableName = @TableName
    UNION ALL
    SELECT p.FormLayoutItemID, p.UIGridLayoutParentID, p.ParameterKeyValue, p.DisplayTitle, w.Lvl + 1
    FROM dbo.AppFormLayoutItem p
    INNER JOIN walk w ON p.FormLayoutItemID = w.UIGridLayoutParentID
)
SELECT TOP 1 COALESCE(JSON_VALUE(ParameterKeyValue, '$.DisplayName'), DisplayTitle)
FROM walk
WHERE JSON_VALUE(ParameterKeyValue, '$.IsTab') = 'true'
ORDER BY Lvl";
                cmd.Parameters.AddWithValue("@FormId", formId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                var val = cmd.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
        }

        private static List<AppConfigPackCommandDto> ExportTransactionCommands(SqlConnection conn, int transactionId, int? formId)
        {
            var raw = new List<(int Id, string Name, int? ActionType, string Formula, string Sql, int? Order)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT WorkFlowActionID, Name, ActionType, FormulaExpression, NotificationMessage, ActionFlowOrder
FROM dbo.AppProjectWorkFlowAction
WHERE CommandTransactionID = @TxId
ORDER BY ISNULL(ActionFlowOrder, 9999), WorkFlowActionID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(1)))
                            continue;
                        raw.Add((
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4),
                            reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5)));
                    }
                }
            }

            if (raw.Count == 0)
                return null;

            var idToIntegration = raw.ToDictionary(r => r.Id, r => SlugCommandIntegrationId(r.Name));
            var list = new List<AppConfigPackCommandDto>();
            foreach (var row in raw)
            {
                AppActionAttributeDto attr = null;
                if (!string.IsNullOrWhiteSpace(row.Formula))
                {
                    try { attr = JsonConvert.DeserializeObject<AppActionAttributeDto>(row.Formula); }
                    catch { attr = null; }
                }

                var children = new List<string>();
                if (attr?.ChildActionList != null)
                {
                    foreach (var child in attr.ChildActionList.OrderBy(c => c.Sort ?? 0))
                    {
                        if (!child.CommandId.HasValue)
                            continue;
                        if (idToIntegration.TryGetValue(child.CommandId.Value, out string childKey))
                            children.Add(childKey);
                    }
                }

                string sql = null;
                if (row.ActionType == (int)EmAppTransactionCommandType.ExecuteSQLStatement
                    && !string.IsNullOrWhiteSpace(row.Sql))
                {
                    sql = RewriteRuntimeSqlTokensToPack(conn, transactionId, row.Sql);
                }

                list.Add(new AppConfigPackCommandDto
                {
                    IntegrationId = idToIntegration[row.Id],
                    Name = row.Name,
                    ActionType = row.ActionType ?? 0,
                    SqlStatement = sql,
                    ChildCommandIntegrationIds = children.Count == 0 ? null : children,
                    IsShowOnTopMenu = attr?.IsShowOnTopMenu,
                    LinkToUI = attr?.LinkToUI,
                    LayoutHostTable = formId.HasValue
                        ? ExportCommandLayoutHostTable(conn, formId.Value, row.Id)
                        : null
                });
            }

            return list;
        }

        private static string ExportCommandLayoutHostTable(SqlConnection conn, int formId, int actionId)
        {
            int? buttonParentId = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 UIGridLayoutParentID
FROM dbo.AppFormLayoutItem
WHERE FormID = @FormId
  AND JSON_VALUE(ParameterKeyValue, '$.WidgetDisplayType') = '106'
  AND JSON_VALUE(ParameterKeyValue, '$.CommandActionId') = @ActionId";
                cmd.Parameters.AddWithValue("@FormId", formId);
                cmd.Parameters.AddWithValue("@ActionId", actionId.ToString());
                var val = cmd.ExecuteScalar();
                buttonParentId = val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
            if (!buttonParentId.HasValue)
                return null;

            int? stackId;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT UIGridLayoutParentID FROM dbo.AppFormLayoutItem WHERE FormLayoutItemID = @Id";
                cmd.Parameters.AddWithValue("@Id", buttonParentId.Value);
                var val = cmd.ExecuteScalar();
                stackId = val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
            if (!stackId.HasValue)
                return null;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 u.DataBaseTableName
FROM dbo.AppFormLayoutItem rowItem
INNER JOIN dbo.AppFormLayoutItem grid ON grid.UIGridLayoutParentID = rowItem.FormLayoutItemID
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = grid.GridTransactionUnitID
WHERE rowItem.UIGridLayoutParentID = @StackId
  AND JSON_VALUE(grid.ParameterKeyValue, '$.WidgetDisplayType') = '6'
ORDER BY ISNULL(rowItem.FlowOrGridLayoutSortOrder, 9999), rowItem.FormLayoutItemID";
                cmd.Parameters.AddWithValue("@StackId", stackId.Value);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static void CollectUnitsRecursive(AppTransactionUnitExDto unit, List<AppTransactionUnitExDto> sink)
        {
            if (unit == null)
                return;
            sink.Add(unit);
            foreach (var child in unit.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
                CollectUnitsRecursive(child, sink);
        }

        private static AppConfigPackTableDto ExportOneTable(SqlConnection conn, int tenantDataSourceId, string tableName)
        {
            DatabaseTable schema = AppMetaDataBL.GetOneDatabaseTableSchema(tableName, tenantDataSourceId, "dbo");
            if (schema == null)
                return null;

            var table = new AppConfigPackTableDto
            {
                Name = tableName,
                SchemaOwner = "dbo",
                Description = schema.Description
            };

            foreach (var col in schema.Columns ?? new List<DatabaseColumn>())
            {
                table.Columns.Add(new AppConfigPackColumnDto
                {
                    Name = col.Name,
                    DataType = col.DbDataType,
                    Length = col.Length,
                    Precision = col.Precision,
                    Scale = col.Scale,
                    IsPrimaryKey = col.IsPrimaryKey,
                    IsNullable = col.Nullable,
                    IsAutoIncrement = col.IsAutoNumber,
                    DefaultValue = col.DefaultValue
                });
            }

            if (schema.ForeignKeys != null)
            {
                foreach (var fk in schema.ForeignKeys)
                {
                    if (fk == null || string.IsNullOrWhiteSpace(fk.RefersToTable) || fk.Columns == null || fk.Columns.Count == 0)
                        continue;
                    table.Relationships.Add(new AppConfigPackRelationshipDto
                    {
                        Type = "MANY_TO_ONE",
                        TargetTable = fk.RefersToTable,
                        ForeignKeyColumn = fk.Columns[0],
                        ReferencedColumn = string.IsNullOrWhiteSpace(fk.RefersToConstraint) ? fk.Columns[0] : null
                    });
                    if (table.Relationships[table.Relationships.Count - 1].ReferencedColumn == null)
                        table.Relationships[table.Relationships.Count - 1].ReferencedColumn = fk.Columns[0];
                }
            }

            return table;
        }

        private static AppConfigPackViewDto ExportOneView(SqlConnection conn, string viewName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT m.definition
FROM sys.sql_modules m
INNER JOIN sys.views v ON v.object_id = m.object_id
WHERE v.name = @Name AND SCHEMA_NAME(v.schema_id) = N'dbo'";
                cmd.Parameters.AddWithValue("@Name", viewName);
                var def = cmd.ExecuteScalar() as string;
                if (string.IsNullOrWhiteSpace(def))
                    return null;

                string sql = def.Trim();
                if (sql.StartsWith("CREATE VIEW", StringComparison.OrdinalIgnoreCase)
                    && sql.IndexOf("CREATE OR ALTER", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    sql = "CREATE OR ALTER VIEW" + sql.Substring("CREATE VIEW".Length);
                }

                return new AppConfigPackViewDto
                {
                    Name = viewName,
                    SchemaOwner = "dbo",
                    CreateOrAlterSql = sql
                };
            }
        }

        private static AppConfigPackTransactionGroupDto ExportTransactionGroup(
            SqlConnection conn,
            int saasApplicationId,
            Dictionary<int, string> txIdToIntegration)
        {
            if (txIdToIntegration.Count == 0)
                return null;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 g.TransactionGroupID, g.GroupName
FROM dbo.AppTransactionGroup g
INNER JOIN dbo.AppTransactionGroupItem i ON i.TransactionGroupID = g.TransactionGroupID
WHERE ISNULL(g.SaasApplicationID, 0) IN (0, @AppId)
  AND i.TransID IN (" + string.Join(",", txIdToIntegration.Keys.Select(id => id.ToString())) + @")
ORDER BY CASE WHEN g.SaasApplicationID = @AppId THEN 0 ELSE 1 END, g.TransactionGroupID";
                cmd.Parameters.AddWithValue("@AppId", saasApplicationId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    int groupId = reader.GetInt32(0);
                    string groupName = reader.IsDBNull(1) ? null : reader.GetString(1);
                    reader.Close();

                    var members = new List<string>();
                    string primary = null;
                    using (var itemCmd = conn.CreateCommand())
                    {
                        itemCmd.CommandText = @"
SELECT TransID, ISNULL(IsGroupSharedHeader, 0)
FROM dbo.AppTransactionGroupItem
WHERE TransactionGroupID = @GroupId
ORDER BY ISNULL(TransactionLayoutOrder, 0), TransID";
                        itemCmd.Parameters.AddWithValue("@GroupId", groupId);
                        using (var itemReader = itemCmd.ExecuteReader())
                        {
                            while (itemReader.Read())
                            {
                                if (itemReader.IsDBNull(0))
                                    continue;
                                int txId = itemReader.GetInt32(0);
                                if (!txIdToIntegration.TryGetValue(txId, out string integrationId))
                                    continue;
                                members.Add(integrationId);
                                if (!itemReader.IsDBNull(1) && itemReader.GetBoolean(1) && primary == null)
                                    primary = integrationId;
                            }
                        }
                    }

                    if (members.Count == 0)
                        return null;

                    return new AppConfigPackTransactionGroupDto
                    {
                        Name = groupName,
                        IntegrationId = SlugIntegrationId("TG_", groupName ?? "Group"),
                        PrimaryTransactionIntegrationId = primary ?? members[0],
                        MemberTransactionIntegrationIds = members
                    };
                }
            }
        }

        private static AppConfigPackSearchDto ExportOneSearch(
            SqlConnection conn,
            AppSearchDto searchDto,
            Dictionary<int, string> txIdToIntegration)
        {
            int searchId = Convert.ToInt32(searchDto.Id);
            AppSearchExDto full;
            try
            {
                full = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            }
            catch
            {
                return null;
            }

            if (full == null)
                return null;

            string integrationId = GetOrCreateSearchIntegrationId(conn, searchId, full.Name);
            var exported = new AppConfigPackSearchDto
            {
                IntegrationId = integrationId,
                Name = full.Name,
                Description = full.Description,
                UsageType = full.Type == (int)EmAppSearchUsageType.DataModelTemplate ? "DataModelTemplate" : "Management",
                AutoExecute = full.IsAutoExecute
            };

            if (full.DataSetId.HasValue)
            {
                try
                {
                    var dataSet = AppDataSetBL.RetrieveOneAppDataSetExDto(full.DataSetId.Value);
                    exported.DataSet = new AppConfigPackDataSetDto
                    {
                        Name = dataSet?.Name,
                        QueryText = dataSet?.QueryText
                    };
                }
                catch
                {
                    exported.DataSet = new AppConfigPackDataSetDto { Name = full.Name };
                }
            }

            if (full.AppSearchFieldList != null)
            {
                int sort = 10;
                foreach (var field in full.AppSearchFieldList.Where(f => f != null && !string.IsNullOrWhiteSpace(f.SysTableFiledPath)))
                {
                    exported.CriteriaFields.Add(new AppConfigPackCriteriaFieldDto
                    {
                        DisplayText = field.DisplayText,
                        SysTableFiledPath = field.SysTableFiledPath,
                        ControlType = field.ControlType,
                        EntityCode = GetEntityCodeById(conn, field.EntityId),
                        OperationId = field.OperationId,
                        PositionRow = field.PositionRow,
                        PositionColumn = field.PositionColumn,
                        IsVisible = field.IsVisible,
                        Sort = field.Sort ?? sort,
                        DefaultValue = field.DefaultValue
                    });
                    sort += 10;
                }
            }

            if (full.SearchViewId.HasValue)
            {
                var view = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(full.SearchViewId.Value);
                exported.SearchView = new AppConfigPackSearchViewDto
                {
                    Name = view?.Name,
                    IntegrationId = integrationId + "_View",
                    GridOutputMode = view?.GridOutputMode > 0 ? view.GridOutputMode : 1
                };

                if (view?.AppSearchViewFieldList != null)
                {
                    int sort = 10;
                    foreach (var field in view.AppSearchViewFieldList.Where(f => f != null && !string.IsNullOrWhiteSpace(f.SysTableFiledPath)))
                    {
                        exported.SearchView.Fields.Add(new AppConfigPackSearchViewFieldDto
                        {
                            DisplayText = field.DisplayText,
                            SysTableFiledPath = field.SysTableFiledPath,
                            ControlType = field.ControlType,
                            EntityCode = GetEntityCodeById(conn, field.EntityId),
                            IsTransRootId = field.IsTransRootId == true,
                            IsVisible = field.IsVisible,
                            Sort = field.Sort ?? sort
                        });
                        sort += 10;
                    }
                }

                exported.LinkTargets = ExportLinkTargets(conn, full.SearchViewId.Value, txIdToIntegration);
            }

            exported.Menu = ExportSearchMenu(conn, searchId, full.Name);
            return exported;
        }

        private static List<AppConfigPackLinkTargetDto> ExportLinkTargets(
            SqlConnection conn,
            int searchViewId,
            Dictionary<int, string> txIdToIntegration)
        {
            var raw = new List<(string Name, int? ActionType, int TxId, string SourceCol, int? Sort)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT NavigationActionName, ActionType, LinkTargetTransactionID, TargetColumn1, Sort
FROM dbo.AppFormLinkTarget
WHERE SearchViewID = @SearchViewId
ORDER BY ISNULL(Sort, 0), LinkTargetID";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(2))
                            continue;
                        raw.Add((
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            reader.GetInt32(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4))));
                    }
                }
            }

            var list = new List<AppConfigPackLinkTargetDto>();
            foreach (var row in raw)
            {
                string integrationId;
                if (!txIdToIntegration.TryGetValue(row.TxId, out integrationId))
                    integrationId = GetTransactionIntegrationId(conn, row.TxId);
                if (string.IsNullOrWhiteSpace(integrationId))
                    continue;

                string action = "Edit";
                if (row.ActionType == (int)EmAppLinkTargetActionType.Create)
                    action = "Create";
                else if (row.ActionType == (int)EmAppLinkTargetActionType.Delete)
                    action = "Delete";

                list.Add(new AppConfigPackLinkTargetDto
                {
                    Name = row.Name,
                    ActionType = action,
                    TransactionIntegrationId = integrationId,
                    SourceColumn = row.SourceCol,
                    Sort = row.Sort
                });
            }

            return list;
        }

        private static AppConfigPackMenuDto ExportSearchMenu(SqlConnection conn, int searchId, string searchName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 Name, Sort
FROM dbo.AppListMenu
WHERE RouteCode = N'MasterDataManagement' AND Link = @Link
ORDER BY MenuID";
                cmd.Parameters.AddWithValue("@Link", searchId.ToString());
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return new AppConfigPackMenuDto { RegisterInMainMenu = false };

                    return new AppConfigPackMenuDto
                    {
                        RegisterInMainMenu = true,
                        MenuTitle = reader.IsDBNull(0) ? searchName : reader.GetString(0),
                        MenuOrder = reader.IsDBNull(1) ? (int?)null : Convert.ToInt32(reader.GetValue(1))
                    };
                }
            }
        }

        private static string GetOrCreateTransactionIntegrationId(SqlConnection conn, int transactionId, string name)
        {
            string existing = GetTransactionIntegrationId(conn, transactionId);
            if (!string.IsNullOrWhiteSpace(existing))
                return existing;

            string generated = SlugIntegrationId("TX_", name ?? ("Transaction" + transactionId));
            SetIntegrationId(conn, "AppTransaction", "TransactionID", transactionId, generated);
            return generated;
        }

        private static string GetTransactionIntegrationId(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT IntegrationId FROM dbo.AppTransaction WHERE TransactionID = @Id";
                cmd.Parameters.AddWithValue("@Id", transactionId);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static string GetOrCreateSearchIntegrationId(SqlConnection conn, int searchId, string name)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT IntegrationId FROM dbo.AppSearch WHERE SearchID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchId);
                var existing = cmd.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(existing))
                    return existing;
            }

            string generated = SlugIntegrationId("Search_", name ?? ("Search" + searchId));
            SetIntegrationId(conn, "AppSearch", "SearchID", searchId, generated);
            return generated;
        }
    }
}
