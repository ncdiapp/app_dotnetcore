using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        private static Dictionary<string, int> UpsertTransactions(
            AppConfigPackDto pack,
            int tenantDataSourceId,
            int? saasApplicationId,
            AppConfigPackExecuteResultDto executeResult)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var tx in pack.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId) || tx.UnitStructure == null)
                    continue;

                string integrationId = tx.IntegrationId.Trim();
                int transactionId;
                bool inserted;

                using (var conn = OpenTenantConnection())
                {
                    int? existing = GetTransactionIdByIntegrationId(conn, integrationId);
                    if (existing.HasValue)
                    {
                        transactionId = existing.Value;
                        inserted = false;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
UPDATE dbo.AppTransaction
SET TransactionName = @Name,
    Description = @Description,
    SaasApplicationID = COALESCE(@SaasApplicationId, SaasApplicationID),
    IsShowSaveButton = CASE WHEN @ShowSave IS NULL THEN IsShowSaveButton ELSE @ShowSave END,
    IsShowPrintButton = CASE WHEN @ShowPrint IS NULL THEN IsShowPrintButton ELSE @ShowPrint END,
    IsShowCalculateButton = CASE WHEN @ShowCalc IS NULL THEN IsShowCalculateButton ELSE @ShowCalc END,
    IsReadOnly = CASE WHEN @IsReadOnly IS NULL THEN IsReadOnly ELSE @IsReadOnly END,
    AppModifiedDate = GETDATE()
WHERE TransactionID = @Id";
                            cmd.Parameters.AddWithValue("@Name", TruncateName(tx.Name ?? integrationId, 200, integrationId));
                            cmd.Parameters.AddWithValue("@Description", (object)(tx.Description ?? tx.Name) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                            AddNullableBit(cmd, "@ShowSave", tx.IsShowSaveButton);
                            AddNullableBit(cmd, "@ShowPrint", tx.IsShowPrintButton);
                            AddNullableBit(cmd, "@ShowCalc", tx.IsShowCalculateButton);
                            AddNullableBit(cmd, "@IsReadOnly", tx.IsReadOnly);
                            cmd.Parameters.AddWithValue("@Id", transactionId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var setup = new HierarchyTableSetupDto
                        {
                            MasterTableName = tx.UnitStructure.RootTableName,
                            SiblingTableNames = MergeSiblingTableNames(tx.UnitStructure),
                            ChildTables = (tx.UnitStructure.ChildUnits ?? new List<AppConfigPackChildUnitDto>())
                                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.TableName))
                                .Select(c => new HierarchyChildTableDto
                                {
                                    TableName = c.TableName,
                                    GrandChildTableNames = MergeGrandChildTableNames(c)
                                })
                                .ToList(),
                            DataSourceRegisterId = tenantDataSourceId,
                            SchemaOwner = "dbo",
                            TransactionName = tx.Name,
                            SaasApplicationId = saasApplicationId
                        };

                        var saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(
                            setup, isIgnoreValidation: true, skipPostSaveCacheSync: false);
                        if (!saveResult.IsSuccessfulWithResult || saveResult.Object == null)
                        {
                            string msg = saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                                ?? $"Failed to create transaction '{integrationId}'.";
                            throw new InvalidOperationException(msg);
                        }

                        transactionId = Convert.ToInt32(saveResult.Object.Id);
                        SetIntegrationId(conn, "AppTransaction", "TransactionID", transactionId, integrationId);
                        inserted = true;
                        OverlayTransactionHeaderFlags(conn, transactionId, tx);
                    }

                    ApplyTransactionOrganizedType(conn, transactionId, tx);
                }

                OverlayTransactionFields(transactionId, tx);
                ApplyUnitOverlays(transactionId, tx);
                WireLogicalParentKeys(transactionId, tx, pack.Tables);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                UpsertTransactionCommands(transactionId, tx, applyLayoutHostButtons: false);
                ApplyTransactionFormLayout(transactionId, tx);
                if (!HasPortableFormLayout(tx))
                    ApplyCommandLayoutButtons(transactionId, tx);

                RegisterTransactionMainMenu(transactionId, tx, saasApplicationId, executeResult);

                map[integrationId] = transactionId;
                if (inserted)
                {
                    executeResult.TransactionsInserted++;
                    executeResult.Messages.Add($"Inserted transaction {transactionId} ({integrationId}).");
                }
                else
                {
                    executeResult.TransactionsUpdated++;
                    executeResult.Messages.Add($"Updated transaction {transactionId} ({integrationId}).");
                }
            }

            return map;
        }

        /// <summary>
        /// Pack organizedType: MasterDetail | List | ListEdit.
        /// Insert omit → leave CreateHierarchy default (MasterDetail).
        /// Update omit → leave existing. Explicit value always written.
        /// </summary>
        private static void ApplyTransactionOrganizedType(
            SqlConnection conn,
            int transactionId,
            AppConfigPackTransactionDto tx)
        {
            int? organizedType = ResolveOrganizedTypeId(tx?.OrganizedType);
            if (!organizedType.HasValue)
                return;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE dbo.AppTransaction SET TransactionOrganizedType = @Type, AppModifiedDate = GETDATE() WHERE TransactionID = @Id";
                cmd.Parameters.AddWithValue("@Type", organizedType.Value);
                cmd.Parameters.AddWithValue("@Id", transactionId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Returns enum id, or null when omit / unknown (unknown should be caught in Validate).</summary>
        internal static int? ResolveOrganizedTypeId(string organizedType)
        {
            if (string.IsNullOrWhiteSpace(organizedType))
                return null;
            if (string.Equals(organizedType, "List", StringComparison.OrdinalIgnoreCase)
                || string.Equals(organizedType, "ListEdit", StringComparison.OrdinalIgnoreCase))
                return (int)EmTransactionOrganizedType.List;
            if (string.Equals(organizedType, "MasterDetail", StringComparison.OrdinalIgnoreCase))
                return (int)EmTransactionOrganizedType.MasterDetail;
            return null;
        }

        internal static bool IsListOrganizedType(string organizedType)
        {
            int? id = ResolveOrganizedTypeId(organizedType);
            return id == (int)EmTransactionOrganizedType.List;
        }

        internal static string FormatOrganizedTypeName(int? organizedTypeId)
        {
            if (organizedTypeId == (int)EmTransactionOrganizedType.List)
                return "List";
            if (organizedTypeId == (int)EmTransactionOrganizedType.MasterDetail)
                return "MasterDetail";
            return organizedTypeId?.ToString();
        }

        private static void RegisterTransactionMainMenu(
            int transactionId,
            AppConfigPackTransactionDto tx,
            int? saasApplicationId,
            AppConfigPackExecuteResultDto executeResult)
        {
            if (tx?.Menu == null || !tx.Menu.RegisterInMainMenu)
                return;

            if (!IsListOrganizedType(tx.OrganizedType))
            {
                executeResult?.Messages.Add(
                    $"Skipped main menu for transaction {transactionId}: menu.registerInMainMenu requires organizedType List (ListEdit).");
                return;
            }

            if (!saasApplicationId.HasValue)
            {
                executeResult?.Messages.Add(
                    $"Skipped main menu for transaction {transactionId}: SaasApplicationId is required.");
                return;
            }

            string menuTitle = !string.IsNullOrWhiteSpace(tx.Menu.MenuTitle)
                ? tx.Menu.MenuTitle.Trim()
                : (tx.Name ?? tx.IntegrationId);

            var menuResult = AppTreeListMenuBL.AddListTransactionToMainMenu(transactionId, menuTitle);
            if (menuResult.ValidationResult != null && menuResult.ValidationResult.HasErrors)
            {
                executeResult?.Messages.Add(
                    menuResult.ValidationResult.Items?.FirstOrDefault()?.Message
                    ?? $"Menu registration failed for transaction {transactionId}.");
                return;
            }

            if (tx.Menu.MenuOrder.HasValue || !string.IsNullOrWhiteSpace(tx.Menu.MenuTitle))
            {
                using (var conn = OpenTenantConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppListMenu
SET Name = COALESCE(@Name, Name),
    Sort = CASE WHEN @Sort IS NULL THEN Sort ELSE @Sort END
WHERE RouteCode = N'FormListEdit' AND Link = @Link";
                    cmd.Parameters.AddWithValue("@Name", (object)menuTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Sort", (object)tx.Menu.MenuOrder ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Link", transactionId.ToString());
                    cmd.ExecuteNonQuery();
                }
            }

            executeResult?.Messages.Add(
                $"Registered ListEdit transaction {transactionId} on main menu as '{menuTitle}'.");
        }

        private static void OverlayTransactionFields(int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx.Fields == null || tx.Fields.Count == 0)
                return;

            using (var conn = OpenTenantConnection())
            {
                foreach (var field in tx.Fields
                    .Where(f => f != null && !string.IsNullOrWhiteSpace(f.ColumnName))
                    .OrderByDescending(f => f.IsPrimaryKey == true)
                    .ThenBy(f => f.IsLinkToParentPrimaryKey == true))
                {
                    int? entityId = ResolveEntityIdByCode(conn, field.EntityCode);
                    int? matrixFieldId = null;
                    if (!string.IsNullOrWhiteSpace(field.MatrixSourceTable)
                        && !string.IsNullOrWhiteSpace(field.MatrixSourceColumn))
                    {
                        matrixFieldId = GetTransactionFieldId(
                            conn, transactionId, field.MatrixSourceTable, field.MatrixSourceColumn);
                    }

                    int? dependsOnFieldId = null;
                    if (!string.IsNullOrWhiteSpace(field.DependsOnTable)
                        && !string.IsNullOrWhiteSpace(field.DependsOnColumn))
                    {
                        dependsOnFieldId = GetTransactionFieldId(
                            conn, transactionId, field.DependsOnTable, field.DependsOnColumn);
                        if (!dependsOnFieldId.HasValue)
                        {
                            throw new InvalidOperationException(
                                $"Depends-on field '{field.DependsOnTable}.{field.DependsOnColumn}' was not found for '{field.TableName}.{field.ColumnName}'.");
                        }
                    }

                    int? parentPkFieldId = null;
                    if (field.IsLinkToParentPrimaryKey == true && !string.IsNullOrWhiteSpace(field.TableName))
                    {
                        parentPkFieldId = GetParentPrimaryKeyFieldId(conn, transactionId, field.TableName);
                    }

                    bool hasQueryOverlay = field.DdlQueryText != null;
                    string whereClauseExpress = null;
                    if (hasQueryOverlay)
                    {
                        if (!string.IsNullOrWhiteSpace(field.DdlQueryText) && field.DdlQueryText.Length > 4000)
                        {
                            throw new InvalidOperationException(
                                $"ddlQueryText for '{field.TableName}.{field.ColumnName}' exceeds 4000 characters.");
                        }

                        if (!string.IsNullOrWhiteSpace(field.DdlQueryText)
                            && field.DdlQueryParameterColumns != null
                            && field.DdlQueryParameterColumns.Count > 0)
                        {
                            var paramIds = new List<string>();
                            foreach (var spec in field.DdlQueryParameterColumns)
                            {
                                if (string.IsNullOrWhiteSpace(spec))
                                    continue;
                                ParseTableColumnSpec(spec, field.TableName, out string paramTable, out string paramColumn);
                                int? paramFieldId = GetTransactionFieldId(conn, transactionId, paramTable, paramColumn);
                                if (!paramFieldId.HasValue)
                                {
                                    throw new InvalidOperationException(
                                        $"Query datasource parameter '{spec}' was not found for '{field.TableName}.{field.ColumnName}'.");
                                }
                                paramIds.Add(paramFieldId.Value.ToString());
                            }
                            whereClauseExpress = string.Join("|", paramIds);
                        }
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE f SET
    ControlType = COALESCE(@ControlType, f.ControlType),
    EntityId = CASE WHEN @HasQuery = 1 AND LEN(ISNULL(@DdlQueryText, N'')) > 0 THEN NULL ELSE COALESCE(@EntityId, f.EntityId) END,
    IsVisible = CASE WHEN @IsVisible IS NULL THEN f.IsVisible ELSE @IsVisible END,
    IsReadonly = CASE WHEN @IsReadOnly IS NULL THEN f.IsReadonly ELSE @IsReadOnly END,
    IsPrimaryKey = CASE WHEN @IsPrimaryKey IS NULL THEN f.IsPrimaryKey ELSE @IsPrimaryKey END,
    IsLinkToParentPrimaryKey = CASE WHEN @IsLinkToParent IS NULL THEN f.IsLinkToParentPrimaryKey ELSE @IsLinkToParent END,
    LinkToParentPrimaryKeyFieldID = COALESCE(@ParentPkFieldId, f.LinkToParentPrimaryKeyFieldID),
    IsPivotRow = CASE WHEN @IsPivotRow IS NULL THEN f.IsPivotRow ELSE @IsPivotRow END,
    IsPivotColumn = CASE WHEN @IsPivotColumn IS NULL THEN f.IsPivotColumn ELSE @IsPivotColumn END,
    IsPivotValue = CASE WHEN @IsPivotValue IS NULL THEN f.IsPivotValue ELSE @IsPivotValue END,
    MatrixForeignKeyFieldId = COALESCE(@MatrixFieldId, f.MatrixForeignKeyFieldId),
    DisplayName = COALESCE(@DisplayName, f.DisplayName),
    DDLParentLevelID = COALESCE(@DependsOnFieldId, f.DDLParentLevelID),
    CascadingRelationTable = COALESCE(@CascadingTable, f.CascadingRelationTable),
    CascadingRelationTableSchemaOwner = COALESCE(@CascadingSchema, f.CascadingRelationTableSchemaOwner),
    CascadingRelationTableParentKeyField = COALESCE(@CascadingParent, f.CascadingRelationTableParentKeyField),
    CascadingRelationTableChildKeyField = COALESCE(@CascadingChild, f.CascadingRelationTableChildKeyField),
    SortOrder = COALESCE(@SortOrder, f.SortOrder),
    NBDecimal = COALESCE(@NbDecimal, f.NBDecimal),
    DisplayWidth = CASE WHEN @DisplayWidth IS NULL THEN f.DisplayWidth ELSE @DisplayWidth END,
    DdlQueryText = CASE WHEN @HasQuery = 1 THEN @DdlQueryText ELSE f.DdlQueryText END,
    WhereClauseExpress = CASE WHEN @HasQuery = 1 THEN @WhereClause ELSE f.WhereClauseExpress END,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND f.DataBaseFieldName = @ColumnName
  AND (@TableName IS NULL OR u.DataBaseTableName = @TableName)";
                        cmd.Parameters.AddWithValue("@ControlType", (object)field.ControlType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntityId", (object)entityId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HasQuery", hasQueryOverlay ? 1 : 0);
                        cmd.Parameters.AddWithValue("@DdlQueryText",
                            hasQueryOverlay
                                ? (string.IsNullOrWhiteSpace(field.DdlQueryText) ? (object)DBNull.Value : field.DdlQueryText)
                                : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WhereClause",
                            hasQueryOverlay
                                ? (string.IsNullOrWhiteSpace(whereClauseExpress) ? (object)DBNull.Value : whereClauseExpress)
                                : (object)DBNull.Value);
                        AddNullableBit(cmd, "@IsVisible", field.IsVisible);
                        AddNullableBit(cmd, "@IsReadOnly", field.IsReadOnly);
                        AddNullableBit(cmd, "@IsPrimaryKey", field.IsPrimaryKey);
                        AddNullableBit(cmd, "@IsLinkToParent", field.IsLinkToParentPrimaryKey);
                        cmd.Parameters.AddWithValue("@ParentPkFieldId", (object)parentPkFieldId ?? DBNull.Value);
                        AddNullableBit(cmd, "@IsPivotRow", field.IsPivotRow);
                        AddNullableBit(cmd, "@IsPivotColumn", field.IsPivotColumn);
                        AddNullableBit(cmd, "@IsPivotValue", field.IsPivotValue);
                        cmd.Parameters.AddWithValue("@MatrixFieldId", (object)matrixFieldId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DisplayName", string.IsNullOrWhiteSpace(field.DisplayName) ? (object)DBNull.Value : field.DisplayName.Trim());
                        cmd.Parameters.AddWithValue("@DependsOnFieldId", (object)dependsOnFieldId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CascadingTable", string.IsNullOrWhiteSpace(field.CascadingRelationTable) ? (object)DBNull.Value : field.CascadingRelationTable.Trim());
                        cmd.Parameters.AddWithValue("@CascadingSchema", string.IsNullOrWhiteSpace(field.CascadingRelationSchemaOwner) ? (object)DBNull.Value : field.CascadingRelationSchemaOwner.Trim());
                        cmd.Parameters.AddWithValue("@CascadingParent", string.IsNullOrWhiteSpace(field.CascadingParentKey) ? (object)DBNull.Value : field.CascadingParentKey.Trim());
                        cmd.Parameters.AddWithValue("@CascadingChild", string.IsNullOrWhiteSpace(field.CascadingChildKey) ? (object)DBNull.Value : field.CascadingChildKey.Trim());
                        cmd.Parameters.AddWithValue("@SortOrder", (object)field.SortOrder ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NbDecimal", (object)field.NbDecimal ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DisplayWidth",
                            string.IsNullOrWhiteSpace(field.DisplayWidth) ? (object)DBNull.Value : field.DisplayWidth.Trim());
                        cmd.Parameters.AddWithValue("@TxId", transactionId);
                        cmd.Parameters.AddWithValue("@ColumnName", field.ColumnName.Trim());
                        cmd.Parameters.AddWithValue("@TableName", string.IsNullOrWhiteSpace(field.TableName) ? (object)DBNull.Value : field.TableName.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static List<string> MergeSiblingTableNames(AppConfigPackUnitStructureDto structure)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                if (seen.Add(name.Trim()))
                    names.Add(name.Trim());
            }

            foreach (var name in structure?.SiblingTableNames ?? new List<string>())
                Add(name);
            foreach (var sibling in structure?.SiblingUnits ?? new List<AppConfigPackSiblingUnitDto>())
                Add(sibling?.TableName);
            return names;
        }

        private static List<string> MergeGrandChildTableNames(AppConfigPackChildUnitDto child)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                if (seen.Add(name.Trim()))
                    names.Add(name.Trim());
            }

            foreach (var name in child?.GrandChildTableNames ?? new List<string>())
                Add(name);
            foreach (var grand in child?.GrandChildUnits ?? new List<AppConfigPackChildUnitDto>())
                Add(grand?.TableName);
            return names;
        }

        private static void ApplyUnitOverlays(int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx?.UnitStructure == null)
                return;

            using (var conn = OpenTenantConnection())
            {
                if (!string.IsNullOrWhiteSpace(tx.UnitStructure.RootDisplayName)
                    && !string.IsNullOrWhiteSpace(tx.UnitStructure.RootTableName))
                {
                    UpdateUnitDisplayName(conn, transactionId, tx.UnitStructure.RootTableName, tx.UnitStructure.RootDisplayName);
                }

                foreach (var sibling in tx.UnitStructure.SiblingUnits ?? Enumerable.Empty<AppConfigPackSiblingUnitDto>())
                {
                    if (sibling == null || string.IsNullOrWhiteSpace(sibling.TableName) || string.IsNullOrWhiteSpace(sibling.DisplayName))
                        continue;
                    UpdateUnitDisplayName(conn, transactionId, sibling.TableName, sibling.DisplayName);
                }

                foreach (var child in tx.UnitStructure.ChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                {
                    ApplyOneChildUnitOverlay(conn, transactionId, child);
                    foreach (var grand in child.GrandChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                        ApplyOneChildUnitOverlay(conn, transactionId, grand);
                }
            }
        }

        private static void ApplyOneChildUnitOverlay(SqlConnection conn, int transactionId, AppConfigPackChildUnitDto child)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.TableName))
                return;

            int? unitId = GetTransactionUnitId(conn, transactionId, child.TableName);
            if (!unitId.HasValue)
                throw new InvalidOperationException($"Transaction unit '{child.TableName}' was not found.");

            if (!string.IsNullOrWhiteSpace(child.DisplayName))
                UpdateUnitDisplayNameById(conn, unitId.Value, child.DisplayName.Trim());

            if (child.GridDisplayType.HasValue
                || child.IsReadOnly.HasValue
                || child.IsSynchToDatabaseTable.HasValue
                || child.IsDisableAddButton.HasValue
                || child.IsDisableDeleteButton.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit SET
    EmGridViewDisplayType = COALESCE(@DisplayType, EmGridViewDisplayType),
    IsReadOnly = CASE WHEN @IsReadOnly IS NULL THEN IsReadOnly ELSE @IsReadOnly END,
    IsSynchToDatabaseTable = CASE WHEN @IsSynch IS NULL THEN IsSynchToDatabaseTable ELSE @IsSynch END,
    IsDisableAddButton = CASE WHEN @DisableAdd IS NULL THEN IsDisableAddButton ELSE @DisableAdd END,
    IsDisableDeleteButton = CASE WHEN @DisableDelete IS NULL THEN IsDisableDeleteButton ELSE @DisableDelete END,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                    cmd.Parameters.AddWithValue("@DisplayType", (object)child.GridDisplayType ?? DBNull.Value);
                    AddNullableBit(cmd, "@IsReadOnly", child.IsReadOnly);
                    AddNullableBit(cmd, "@IsSynch", child.IsSynchToDatabaseTable);
                    AddNullableBit(cmd, "@DisableAdd", child.IsDisableAddButton);
                    AddNullableBit(cmd, "@DisableDelete", child.IsDisableDeleteButton);
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            if (string.IsNullOrWhiteSpace(child.AvailableSourceTableName))
                return;

            int? sourceUnitId = GetTransactionUnitId(conn, transactionId, child.AvailableSourceTableName);
            if (!sourceUnitId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Available source unit '{child.AvailableSourceTableName}' was not found for '{child.TableName}'.");
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET AvailableSourceUnitID = @SourceUnitId, AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                cmd.Parameters.AddWithValue("@SourceUnitId", sourceUnitId.Value);
                cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                cmd.ExecuteNonQuery();
            }

            using (var srcFlag = conn.CreateCommand())
            {
                srcFlag.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET IsUsedForLoadingAvailableSource = 1, AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @SourceUnitId";
                srcFlag.Parameters.AddWithValue("@SourceUnitId", sourceUnitId.Value);
                srcFlag.ExecuteNonQuery();
            }

            string selectedColumn = child.AvailableSelectSelectedColumn;
            if (string.IsNullOrWhiteSpace(selectedColumn))
            {
                throw new InvalidOperationException(
                    $"availableSelectSelectedColumn is required when availableSourceTableName is set on '{child.TableName}'.");
            }

            string sourceColumn = string.IsNullOrWhiteSpace(child.AvailableSelectSourceColumn)
                ? selectedColumn
                : child.AvailableSelectSourceColumn;
            int? selectedFieldId = GetTransactionFieldId(conn, transactionId, child.TableName, selectedColumn);
            int? sourceFieldId = GetTransactionFieldId(conn, transactionId, child.AvailableSourceTableName, sourceColumn);
            if (!selectedFieldId.HasValue || !sourceFieldId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Available Select mapping {child.TableName}.{selectedColumn} → {child.AvailableSourceTableName}.{sourceColumn} was not found.");
            }

            using (var mapCmd = conn.CreateCommand())
            {
                mapCmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET MappingToAvailableSourceUnitTransactionFieldID = @SourceFieldId, AppModifiedDate = GETDATE()
WHERE TransactionFieldID = @SelectedFieldId";
                mapCmd.Parameters.AddWithValue("@SourceFieldId", sourceFieldId.Value);
                mapCmd.Parameters.AddWithValue("@SelectedFieldId", selectedFieldId.Value);
                mapCmd.ExecuteNonQuery();
            }
        }

        internal static void ApplyTransactionChildLinkTargets(
            AppConfigPackDto pack,
            Dictionary<string, int> txIdsByIntegration)
        {
            foreach (var tx in pack?.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId) || tx.UnitStructure?.ChildUnits == null)
                    continue;
                if (!txIdsByIntegration.TryGetValue(tx.IntegrationId.Trim(), out int transactionId))
                    continue;

                using (var conn = OpenTenantConnection())
                {
                    foreach (var child in tx.UnitStructure.ChildUnits)
                    {
                        ApplyChildLinkTargets(conn, transactionId, child, txIdsByIntegration);
                        foreach (var grand in child?.GrandChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                            ApplyChildLinkTargets(conn, transactionId, grand, txIdsByIntegration);
                    }
                }
            }
        }

        private static void ApplyChildLinkTargets(
            SqlConnection conn,
            int transactionId,
            AppConfigPackChildUnitDto child,
            Dictionary<string, int> txIdsByIntegration)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.TableName) || child.LinkTargets == null)
                return;

            int? unitId = GetTransactionUnitId(conn, transactionId, child.TableName);
            if (!unitId.HasValue)
                throw new InvalidOperationException($"Transaction unit '{child.TableName}' was not found for link targets.");

            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM dbo.AppFormLinkTarget WHERE TransactionUnitID = @UnitId";
                del.Parameters.AddWithValue("@UnitId", unitId.Value);
                del.ExecuteNonQuery();
            }

            int sort = 10;
            foreach (var link in child.LinkTargets)
            {
                if (link == null || string.IsNullOrWhiteSpace(link.TransactionIntegrationId))
                    continue;

                int? targetTxId;
                if (!txIdsByIntegration.TryGetValue(link.TransactionIntegrationId.Trim(), out int mappedId))
                    targetTxId = GetTransactionIdByIntegrationId(conn, link.TransactionIntegrationId);
                else
                    targetTxId = mappedId;

                if (!targetTxId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Link target transaction '{link.TransactionIntegrationId}' was not found for unit '{child.TableName}'.");
                }

                string sourceColumn = string.IsNullOrWhiteSpace(link.SourceColumn) ? null : link.SourceColumn.Trim();
                if (string.IsNullOrWhiteSpace(sourceColumn))
                {
                    throw new InvalidOperationException(
                        $"Link target '{link.Name ?? link.ActionType}' on '{child.TableName}' is missing sourceColumn.");
                }

                string targetColumn = string.IsNullOrWhiteSpace(link.TargetColumn) ? sourceColumn : link.TargetColumn.Trim();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppFormLinkTarget (
    TransactionUnitID,
    NavigationActionName,
    ActionType,
    LinkTargetTransactionID,
    LinkTargetUsageType,
    SourceColumnType,
    SourceColumn1,
    TargetColumn1,
    Sort,
    IsPopup,
    PopupWidth,
    PopupHeight)
VALUES (
    @UnitId,
    @Name,
    @ActionType,
    @LinkTargetTransactionId,
    @UsageType,
    @SourceColumnType,
    @SourceColumn,
    @TargetColumn,
    @Sort,
    @IsPopup,
    @PopupWidth,
    @PopupHeight)";
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(link.Name) ? (link.ActionType ?? "Edit") : link.Name.Trim());
                    cmd.Parameters.AddWithValue("@ActionType", ResolveLinkTargetActionType(link.ActionType));
                    cmd.Parameters.AddWithValue("@LinkTargetTransactionId", targetTxId.Value);
                    cmd.Parameters.AddWithValue("@UsageType", (int)EmAppLinkTargetUsageType.TransactionUnitLinkToForm);
                    cmd.Parameters.AddWithValue("@SourceColumnType", (int)EmAppLinkTargetSourceColumnType.TransactionField);
                    cmd.Parameters.AddWithValue("@SourceColumn", sourceColumn);
                    cmd.Parameters.AddWithValue("@TargetColumn", targetColumn);
                    cmd.Parameters.AddWithValue("@Sort", link.Sort ?? sort);
                    cmd.Parameters.AddWithValue("@IsPopup", link.IsPopup ?? true);
                    cmd.Parameters.AddWithValue("@PopupWidth", link.PopupWidth ?? 1200);
                    cmd.Parameters.AddWithValue("@PopupHeight", link.PopupHeight ?? 700);
                    cmd.ExecuteNonQuery();
                }

                sort += 10;
            }
        }

        private static void UpdateUnitDisplayName(SqlConnection conn, int transactionId, string tableName, string displayName)
        {
            int? unitId = GetTransactionUnitId(conn, transactionId, tableName);
            if (!unitId.HasValue)
                throw new InvalidOperationException($"Transaction unit '{tableName}' was not found.");
            UpdateUnitDisplayNameById(conn, unitId.Value, displayName);
        }

        private static void UpdateUnitDisplayNameById(SqlConnection conn, int unitId, string displayName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET UnitDisplayName = @DisplayName, AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                cmd.Parameters.AddWithValue("@DisplayName", TruncateName(displayName, 200, displayName));
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? GetTransactionUnitId(SqlConnection conn, int transactionId, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId AND DataBaseTableName = @TableName
ORDER BY TransactionUnitID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetParentPrimaryKeyFieldId(SqlConnection conn, int transactionId, string childTableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 pk.TransactionFieldID
FROM dbo.AppTransactionUnit child
INNER JOIN dbo.AppTransactionUnit parent
    ON parent.TransactionUnitID = COALESCE(child.ParentTransactionUnitID,
        (SELECT TOP 1 r.TransactionUnitID
         FROM dbo.AppTransactionUnit r
         WHERE r.TransactionID = child.TransactionID
           AND r.ParentTransactionUnitID IS NULL
           AND ISNULL(r.IsMasterSiblingUnit, 0) = 0
         ORDER BY r.TransactionUnitID))
INNER JOIN dbo.AppTransactionField pk
    ON pk.TransactionUnitID = parent.TransactionUnitID
   AND pk.IsPrimaryKey = 1
WHERE child.TransactionID = @TxId
  AND child.DataBaseTableName = @TableName
ORDER BY pk.TransactionFieldID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", childTableName.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetTransactionFieldId(SqlConnection conn, int transactionId, string tableName, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 f.TransactionFieldID
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.DataBaseTableName = @TableName
  AND f.DataBaseFieldName = @ColumnName";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void EnsureDefaultForm(int transactionId)
        {
            var formResult = AppDatabaseViewBL.EnsureTransactionDefaultFlexFormLayout(
                transactionId, migrationFastPath: true, numberOfLayoutColumns: 4);
            if (formResult.ValidationResult != null && formResult.ValidationResult.HasErrors)
            {
                throw new InvalidOperationException(
                    formResult.ValidationResult.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to create default form for transaction {transactionId}.");
            }
        }

        private static void ApplyFormTabNames(int transactionId, AppConfigPackTransactionDto tx)
        {
            var tabs = new List<(string TableName, string TabName)>();
            foreach (var child in tx?.UnitStructure?.ChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
            {
                if (child == null || string.IsNullOrWhiteSpace(child.TableName) || string.IsNullOrWhiteSpace(child.LayoutTab))
                    continue;
                tabs.Add((child.TableName.Trim(), child.LayoutTab.Trim()));
            }
            if (tabs.Count == 0)
                return;

            using (var conn = OpenTenantConnection())
            {
                int? formId;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT FormID FROM dbo.AppTransaction WHERE TransactionID = @TxId";
                    cmd.Parameters.AddWithValue("@TxId", transactionId);
                    var val = cmd.ExecuteScalar();
                    formId = val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
                }
                if (!formId.HasValue)
                    return;

                foreach (var tab in tabs)
                {
                    int? unitId = GetTransactionUnitId(conn, transactionId, tab.TableName);
                    if (!unitId.HasValue)
                        continue;

                    int? tabItemId;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
WITH walk AS (
    SELECT FormLayoutItemID, UIGridLayoutParentID, ParameterKeyValue, 0 AS Lvl
    FROM dbo.AppFormLayoutItem
    WHERE FormID = @FormId AND GridTransactionUnitID = @UnitId
    UNION ALL
    SELECT p.FormLayoutItemID, p.UIGridLayoutParentID, p.ParameterKeyValue, w.Lvl + 1
    FROM dbo.AppFormLayoutItem p
    INNER JOIN walk w ON p.FormLayoutItemID = w.UIGridLayoutParentID
)
SELECT TOP 1 FormLayoutItemID
FROM walk
WHERE JSON_VALUE(ParameterKeyValue, '$.IsTab') = 'true'
ORDER BY Lvl";
                        cmd.Parameters.AddWithValue("@FormId", formId.Value);
                        cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                        var val = cmd.ExecuteScalar();
                        tabItemId = val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
                    }
                    if (!tabItemId.HasValue)
                        continue;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE dbo.AppFormLayoutItem
SET ParameterKeyValue = JSON_MODIFY(ParameterKeyValue, '$.DisplayName', @TabName),
    DisplayTitle = @TabName
WHERE FormLayoutItemID = @ItemId";
                        cmd.Parameters.AddWithValue("@TabName", tab.TabName);
                        cmd.Parameters.AddWithValue("@ItemId", tabItemId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void UpsertTransactionCommands(int transactionId, AppConfigPackTransactionDto tx, bool applyLayoutHostButtons = true)
        {
            var commands = (tx.Commands ?? new List<AppConfigPackCommandDto>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name))
                .ToList();
            if (commands.Count == 0)
                return;

            using (var conn = OpenTenantConnection())
            {
                var idByIntegration = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int order = 0;
                foreach (var command in commands)
                {
                    order++;
                    string sql = null;
                    if (command.ActionType == (int)EmAppTransactionCommandType.ExecuteSQLStatement
                        && !string.IsNullOrWhiteSpace(command.SqlStatement))
                    {
                        sql = RewritePackSqlTokensToRuntime(conn, transactionId, command.SqlStatement);
                    }

                    int actionId = UpsertOneCommand(conn, transactionId, command, sql, order);
                    if (!string.IsNullOrWhiteSpace(command.IntegrationId))
                        idByIntegration[command.IntegrationId.Trim()] = actionId;
                    idByIntegration[command.Name.Trim()] = actionId;
                }

                foreach (var command in commands)
                {
                    var childKeys = command.ChildCommandIntegrationIds ?? new List<string>();
                    if (childKeys.Count == 0 && command.ActionType != (int)EmAppTransactionCommandType.CompositionCommand)
                        continue;

                    if (!idByIntegration.TryGetValue(
                            string.IsNullOrWhiteSpace(command.IntegrationId) ? command.Name.Trim() : command.IntegrationId.Trim(),
                            out int parentId))
                        continue;

                    var children = new List<ChildTransactionCommandDto>();
                    int sort = 0;
                    foreach (var childKey in childKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
                    {
                        if (!idByIntegration.TryGetValue(childKey.Trim(), out int childId))
                        {
                            throw new InvalidOperationException(
                                $"Command '{command.Name}' child '{childKey}' was not found in this transaction's commands.");
                        }
                        sort++;
                        string childName = commands.FirstOrDefault(c =>
                            string.Equals(c.IntegrationId, childKey.Trim(), StringComparison.OrdinalIgnoreCase)
                            || string.Equals(c.Name, childKey.Trim(), StringComparison.OrdinalIgnoreCase))?.Name;
                        children.Add(new ChildTransactionCommandDto
                        {
                            Sort = sort,
                            CommandId = childId,
                            CommandDisplay = "User Defined: " + (childName ?? childKey.Trim()),
                            IsBatchCommand = false,
                            IsSkip = false
                        });
                    }

                    PatchCommandAttribute(conn, parentId, command, children);
                }

                foreach (var command in commands.Where(c => !string.IsNullOrWhiteSpace(c.LayoutHostTable)))
                {
                    if (!applyLayoutHostButtons)
                        break;
                    string key = string.IsNullOrWhiteSpace(command.IntegrationId) ? command.Name.Trim() : command.IntegrationId.Trim();
                    if (!idByIntegration.TryGetValue(key, out int actionId))
                        continue;
                    EnsureFormCommandButton(conn, transactionId, command.LayoutHostTable.Trim(), actionId, command.Name);
                }
            }

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
        }

        private static void ApplyCommandLayoutButtons(int transactionId, AppConfigPackTransactionDto tx)
        {
            using (var conn = OpenTenantConnection())
            {
                foreach (var command in (tx.Commands ?? new List<AppConfigPackCommandDto>())
                    .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.LayoutHostTable)))
                {
                    int? actionId = GetCommandIdByName(conn, transactionId, command.Name);
                    if (!actionId.HasValue)
                        continue;
                    EnsureFormCommandButton(conn, transactionId, command.LayoutHostTable.Trim(), actionId.Value, command.Name);
                }
            }
        }

        private static int UpsertOneCommand(
            SqlConnection conn,
            int transactionId,
            AppConfigPackCommandDto command,
            string sqlStatement,
            int actionFlowOrder)
        {
            int? existingId;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 WorkFlowActionID
FROM dbo.AppProjectWorkFlowAction
WHERE CommandTransactionID = @TxId AND Name = @Name
ORDER BY WorkFlowActionID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@Name", command.Name.Trim());
                var val = cmd.ExecuteScalar();
                existingId = val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }

            var attr = existingId.HasValue
                ? LoadCommandAttribute(conn, existingId.Value)
                : new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
            if (attr.ChildActionList == null)
                attr.ChildActionList = new List<ChildTransactionCommandDto>();
            attr.LinkToUI = command.LinkToUI;
            attr.IsShowOnTopMenu = command.IsShowOnTopMenu ?? false;
            attr.IsAutoExecuteOnFormOpen = attr.IsAutoExecuteOnFormOpen ?? false;
            string formula = JsonConvert.SerializeObject(attr);

            if (existingId.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppProjectWorkFlowAction SET
    ActionType = @ActionType,
    FormulaExpression = @Formula,
    NotificationMessage = CASE WHEN @HasSql = 1 THEN @Sql ELSE NotificationMessage END,
    ActionFlowOrder = @Order,
    AppModifiedDate = GETDATE()
WHERE WorkFlowActionID = @Id";
                    cmd.Parameters.AddWithValue("@ActionType", command.ActionType);
                    cmd.Parameters.AddWithValue("@Formula", formula);
                    cmd.Parameters.AddWithValue("@HasSql", sqlStatement != null ? 1 : 0);
                    cmd.Parameters.AddWithValue("@Sql", (object)sqlStatement ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Order", actionFlowOrder);
                    cmd.Parameters.AddWithValue("@Id", existingId.Value);
                    cmd.ExecuteNonQuery();
                }
                return existingId.Value;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppProjectWorkFlowAction
    (Name, Description, ActionType, FormulaExpression, NotificationMessage, RowIdentity, ActionGUID,
     ActionFlowOrder, CommandTransactionID, AppCreatedDate, AppModifiedDate)
VALUES
    (@Name, @Name, @ActionType, @Formula, @Sql, NEWID(), NEWID(),
     @Order, @TxId, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                cmd.Parameters.AddWithValue("@Name", TruncateName(command.Name.Trim(), 500, command.Name.Trim()));
                cmd.Parameters.AddWithValue("@ActionType", command.ActionType);
                cmd.Parameters.AddWithValue("@Formula", formula);
                cmd.Parameters.AddWithValue("@Sql", (object)sqlStatement ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Order", actionFlowOrder);
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static AppActionAttributeDto LoadCommandAttribute(SqlConnection conn, int actionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT FormulaExpression FROM dbo.AppProjectWorkFlowAction WHERE WorkFlowActionID = @Id";
                cmd.Parameters.AddWithValue("@Id", actionId);
                var json = cmd.ExecuteScalar() as string;
                if (string.IsNullOrWhiteSpace(json))
                    return new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
                try
                {
                    return JsonConvert.DeserializeObject<AppActionAttributeDto>(json)
                        ?? new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
                }
                catch
                {
                    return new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
                }
            }
        }

        private static void PatchCommandAttribute(
            SqlConnection conn,
            int actionId,
            AppConfigPackCommandDto command,
            List<ChildTransactionCommandDto> children)
        {
            var attr = LoadCommandAttribute(conn, actionId);
            attr.ChildActionList = children;
            attr.LinkToUI = command.LinkToUI;
            attr.IsShowOnTopMenu = command.IsShowOnTopMenu ?? attr.IsShowOnTopMenu ?? false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppProjectWorkFlowAction
SET FormulaExpression = @Formula, AppModifiedDate = GETDATE()
WHERE WorkFlowActionID = @Id";
                cmd.Parameters.AddWithValue("@Formula", JsonConvert.SerializeObject(attr));
                cmd.Parameters.AddWithValue("@Id", actionId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureFormCommandButton(
            SqlConnection conn,
            int transactionId,
            string hostTable,
            int actionId,
            string commandName)
        {
            int? formId;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT FormID FROM dbo.AppTransaction WHERE TransactionID = @TxId";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                var val = cmd.ExecuteScalar();
                formId = val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
            if (!formId.HasValue)
                return;

            int? unitId = GetTransactionUnitId(conn, transactionId, hostTable);
            if (!unitId.HasValue)
            {
                throw new InvalidOperationException(
                    $"layoutHostTable '{hostTable}' was not found for command '{commandName}'.");
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 FormLayoutItemID
FROM dbo.AppFormLayoutItem
WHERE FormID = @FormId
  AND JSON_VALUE(ParameterKeyValue, '$.CommandActionId') = @ActionId
  AND JSON_VALUE(ParameterKeyValue, '$.WidgetDisplayType') = '106'";
                cmd.Parameters.AddWithValue("@FormId", formId.Value);
                cmd.Parameters.AddWithValue("@ActionId", actionId.ToString());
                var existing = cmd.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return;
            }

            int? gridId = null;
            int? gridParentId = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 FormLayoutItemID, UIGridLayoutParentID
FROM dbo.AppFormLayoutItem
WHERE FormID = @FormId
  AND GridTransactionUnitID = @UnitId
  AND JSON_VALUE(ParameterKeyValue, '$.WidgetDisplayType') = '6'
ORDER BY FormLayoutItemID";
                cmd.Parameters.AddWithValue("@FormId", formId.Value);
                cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        gridId = reader.GetInt32(0);
                        gridParentId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                    }
                }
            }
            if (!gridId.HasValue || !gridParentId.HasValue)
                return;

            int stackId = gridParentId.Value;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT UIGridLayoutParentID, JSON_VALUE(ParameterKeyValue, '$.WidgetDisplayType')
FROM dbo.AppFormLayoutItem
WHERE FormLayoutItemID = @Id";
                cmd.Parameters.AddWithValue("@Id", gridParentId.Value);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string widget = reader.IsDBNull(1) ? null : reader.GetString(1);
                        if (widget == "101" && !reader.IsDBNull(0))
                            stackId = reader.GetInt32(0);
                    }
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppFormLayoutItem
SET FlowOrGridLayoutSortOrder = ISNULL(FlowOrGridLayoutSortOrder, 0) + 1
WHERE UIGridLayoutParentID = @StackId";
                cmd.Parameters.AddWithValue("@StackId", stackId);
                cmd.ExecuteNonQuery();
            }

            const string rowJson =
                "{\"WidgetDisplayType\":101,\"IsBindingToDataField\":false,\"IsTab\":false,\"CommandActionId\":null}";
            int rowId;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppFormLayoutItem
    (FormID, ParameterKeyValue, UIGridLayoutParentID, FlowOrGridLayoutSortOrder, AppCreatedDate, AppModifiedDate)
VALUES
    (@FormId, @Json, @ParentId, 1, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                cmd.Parameters.AddWithValue("@FormId", formId.Value);
                cmd.Parameters.AddWithValue("@Json", rowJson);
                cmd.Parameters.AddWithValue("@ParentId", stackId);
                rowId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            string buttonJson =
                "{\"ColSpanValue\":4,\"BackgroundColor\":\"#ffffff\",\"TextColor\":\"#000000\",\"DisplayName\":\"\"," +
                "\"WidgetDisplayType\":106,\"IsBindingToDataField\":false,\"CommandActionId\":" + actionId +
                ",\"IsShowSearchCriterias\":false,\"IsTab\":false}";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppFormLayoutItem
    (FormID, ParameterKeyValue, DisplayTitle, UIGridLayoutParentID, FlowOrGridLayoutSortOrder, AppCreatedDate, AppModifiedDate)
VALUES
    (@FormId, @Json, @Title, @ParentId, 1, GETDATE(), GETDATE());";
                cmd.Parameters.AddWithValue("@FormId", formId.Value);
                cmd.Parameters.AddWithValue("@Json", buttonJson);
                cmd.Parameters.AddWithValue("@Title", string.IsNullOrWhiteSpace(commandName) ? (object)DBNull.Value : commandName.Trim());
                cmd.Parameters.AddWithValue("@ParentId", rowId);
                cmd.ExecuteNonQuery();
            }
        }

        private static readonly Regex PackFieldTokenRegex = new Regex(@"\[TF:([^\]\s]+)\]", RegexOptions.Compiled);
        private static readonly Regex PackNameOnlyFieldTokenRegex = new Regex(@"\[TF_([A-Za-z][A-Za-z0-9_]*)\]", RegexOptions.Compiled);
        private static readonly Regex RuntimeFieldTokenRegex = new Regex(@"\[TF_(\d+)_([^\]]+)\]", RegexOptions.Compiled);

        internal static string RewritePackSqlTokensToRuntime(SqlConnection conn, int transactionId, string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;

            sql = PackFieldTokenRegex.Replace(sql, match =>
            {
                ParseTableColumnSpec(match.Groups[1].Value, null, out string table, out string column);
                int? fieldId = string.IsNullOrWhiteSpace(table)
                    ? GetTransactionFieldIdByColumn(conn, transactionId, column)
                    : GetTransactionFieldId(conn, transactionId, table, column);
                if (!fieldId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"SQL token '{match.Value}' did not match a transaction field.");
                }
                return $"[TF_{fieldId.Value}_{column}]";
            });

            return PackNameOnlyFieldTokenRegex.Replace(sql, match =>
            {
                string column = match.Groups[1].Value;
                int? fieldId = GetTransactionFieldIdByColumn(conn, transactionId, column);
                if (!fieldId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"SQL token '{match.Value}' did not match a transaction field.");
                }
                return $"[TF_{fieldId.Value}_{column}]";
            });
        }

        internal static string RewriteRuntimeSqlTokensToPack(SqlConnection conn, int transactionId, string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return sql;
            var fields = LoadTransactionFieldLookup(conn, transactionId);
            return RuntimeFieldTokenRegex.Replace(sql, match =>
            {
                int fieldId = int.Parse(match.Groups[1].Value);
                if (!fields.TryGetValue(fieldId, out var loc) || string.IsNullOrWhiteSpace(loc.Table) || string.IsNullOrWhiteSpace(loc.Column))
                    return match.Value;
                return $"[TF:{loc.Table}.{loc.Column}]";
            });
        }

        private static Dictionary<int, (string Table, string Column)> LoadTransactionFieldLookup(SqlConnection conn, int transactionId)
        {
            var map = new Dictionary<int, (string Table, string Column)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT f.TransactionFieldID, u.DataBaseTableName, f.DataBaseFieldName
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                            continue;
                        map[reader.GetInt32(0)] = (reader.GetString(1), reader.GetString(2));
                    }
                }
            }
            return map;
        }

        private static int? GetTransactionFieldIdByColumn(SqlConnection conn, int transactionId, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 f.TransactionFieldID
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId AND f.DataBaseFieldName = @ColumnName
ORDER BY CASE WHEN u.ParentTransactionUnitID IS NULL THEN 0 ELSE 1 END, f.TransactionFieldID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        internal static void ParseTableColumnSpec(string spec, string defaultTable, out string table, out string column)
        {
            table = defaultTable;
            column = spec?.Trim();
            if (string.IsNullOrWhiteSpace(column))
                return;
            int dot = column.LastIndexOf('.');
            if (dot <= 0 || dot >= column.Length - 1)
                return;
            table = column.Substring(0, dot).Trim();
            column = column.Substring(dot + 1).Trim();
        }

        internal static string SlugCommandIntegrationId(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            var sb = new StringBuilder("CMD_");
            bool capNext = true;
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(capNext ? char.ToUpperInvariant(c) : c);
                    capNext = false;
                }
                else
                {
                    capNext = true;
                }
            }
            return sb.ToString();
        }

        private static int? UpsertTransactionGroup(
            AppConfigPackDto pack,
            Dictionary<string, int> txIdsByIntegration,
            int? saasApplicationId)
        {
            var group = pack.TransactionGroup;
            if (group == null || string.IsNullOrWhiteSpace(group.Name))
                return null;

            var memberIds = new List<int>();
            var memberKeys = (group.MemberTransactionIntegrationIds != null && group.MemberTransactionIntegrationIds.Count > 0)
                ? group.MemberTransactionIntegrationIds
                : txIdsByIntegration.Keys.ToList();

            foreach (var key in memberKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                if (txIdsByIntegration.TryGetValue(key.Trim(), out int txId))
                    memberIds.Add(txId);
            }

            if (memberIds.Count == 0)
                return null;

            int? primaryTxId = null;
            if (!string.IsNullOrWhiteSpace(group.PrimaryTransactionIntegrationId)
                && txIdsByIntegration.TryGetValue(group.PrimaryTransactionIntegrationId.Trim(), out int primary))
            {
                primaryTxId = primary;
            }

            using (var conn = OpenTenantConnection())
            {
                int? groupId = GetTransactionGroupIdByName(conn, group.Name);
                if (!groupId.HasValue)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
INSERT INTO dbo.AppTransactionGroup (GroupName, Description, SaasApplicationID)
VALUES (@Name, @Description, @SaasApplicationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        cmd.Parameters.AddWithValue("@Name", TruncateName(group.Name, 100, group.Name));
                        cmd.Parameters.AddWithValue("@Description", (object)group.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                        groupId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                else
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE dbo.AppTransactionGroup
SET Description = @Description, SaasApplicationID = COALESCE(@SaasApplicationId, SaasApplicationID)
WHERE TransactionGroupID = @GroupId";
                        cmd.Parameters.AddWithValue("@Description", (object)group.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var del = conn.CreateCommand())
                {
                    del.CommandText = "DELETE FROM dbo.AppTransactionGroupItem WHERE TransactionGroupID = @GroupId";
                    del.Parameters.AddWithValue("@GroupId", groupId.Value);
                    del.ExecuteNonQuery();
                }

                int order = 0;
                foreach (int txId in memberIds.Distinct())
                {
                    order++;
                    int itemId = EnsureAppTransactionItemId(conn, txId);
                    bool isHeader = primaryTxId.HasValue && primaryTxId.Value == txId;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
INSERT INTO dbo.AppTransactionGroupItem (
    TransactionGroupID, TransactionItemID, TransactionLayoutOrder, TransID,
    IsGroupSharedHeader, IsCrossGroupSharedHeader)
VALUES (
    @GroupId, @TransactionItemId, @Order, @TransactionId,
    @IsHeader, @IsHeader)";
                        cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
                        cmd.Parameters.AddWithValue("@TransactionItemId", itemId);
                        cmd.Parameters.AddWithValue("@Order", order);
                        cmd.Parameters.AddWithValue("@TransactionId", txId);
                        cmd.Parameters.AddWithValue("@IsHeader", isHeader);
                        cmd.ExecuteNonQuery();
                    }
                }

                return groupId;
            }
        }

        private static int EnsureAppTransactionItemId(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 AppTransactionItemID
FROM dbo.AppTransactionItem
WHERE TransactionID = @TransactionId
ORDER BY AppTransactionItemID";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                var existing = cmd.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return Convert.ToInt32(existing);
            }

            string itemName = $"Transaction {transactionId}";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TransactionName FROM dbo.AppTransaction WHERE TransactionID = @TransactionId";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                var name = cmd.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(name))
                    itemName = name;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppTransactionItem (TransactionID, TransactionItemName, Description)
VALUES (@TransactionId, @Name, @Description);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Parameters.AddWithValue("@Name", itemName);
                cmd.Parameters.AddWithValue("@Description", itemName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void AttachApplicationAssets(
            int? saasApplicationId,
            List<int> transactionIds,
            AppConfigPackExecuteResultDto executeResult)
        {
            if (!saasApplicationId.HasValue || saasApplicationId.Value <= 0 || transactionIds == null || transactionIds.Count == 0)
                return;

            var importDto = new SaasApplicationSectionItemImportDto
            {
                ApplicationId = saasApplicationId.Value,
                SelectedItemIdList = transactionIds.Distinct().ToList()
            };
            var save = AppTransactionBL.ImportSaasApplicationTransactions(importDto);
            if (save.ValidationResult != null && save.ValidationResult.HasErrors)
            {
                executeResult.Messages.Add(
                    save.ValidationResult.Items?.FirstOrDefault()?.Message ?? "Failed to attach transactions to the application.");
            }
            else
            {
                executeResult.Messages.Add($"Attached {transactionIds.Distinct().Count()} transaction(s) to application {saasApplicationId.Value}.");
            }
        }

        private static void AddNullableBit(SqlCommand cmd, string name, bool? value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Bit);
            p.IsNullable = true;
            p.Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private sealed class PackUnitRow
        {
            public int UnitId { get; set; }
            public string TableName { get; set; }
            public int? ParentUnitId { get; set; }
            public bool IsSibling { get; set; }
        }

        /// <summary>
        /// After field/unit overlay: mark VIEW PKs, then wire child/sibling Link-To-Parent even when there is no DB FK.
        /// Same column name as parent PK wins; StyleSpecId ↔ ReferenceId is the product/spec alias.
        /// </summary>
        private static void WireLogicalParentKeys(
            int transactionId,
            AppConfigPackTransactionDto tx,
            List<AppConfigPackTableDto> tables)
        {
            using (var conn = OpenTenantConnection())
            {
                var units = LoadPackUnits(conn, transactionId);
                var root = units.FirstOrDefault(u => !u.ParentUnitId.HasValue && !u.IsSibling);
                if (root == null)
                    return;

                foreach (var field in tx.Fields ?? Enumerable.Empty<AppConfigPackFieldDto>())
                {
                    if (field?.IsPrimaryKey != true
                        || string.IsNullOrWhiteSpace(field.TableName)
                        || string.IsNullOrWhiteSpace(field.ColumnName))
                        continue;
                    SetFieldPrimaryKey(conn, transactionId, field.TableName.Trim(), field.ColumnName.Trim());
                }

                foreach (var unit in units)
                {
                    if (UnitHasFlag(conn, unit.UnitId, "IsPrimaryKey"))
                        continue;
                    int parentUnitId = ResolveParentUnitId(unit, root);
                    string parentPkCol = GetUnitPrimaryKeyColumn(conn, parentUnitId);
                    MarkHeuristicPrimaryKey(conn, unit.UnitId, parentPkCol);
                }

                foreach (var field in tx.Fields ?? Enumerable.Empty<AppConfigPackFieldDto>())
                {
                    if (field?.IsLinkToParentPrimaryKey != true
                        || string.IsNullOrWhiteSpace(field.TableName)
                        || string.IsNullOrWhiteSpace(field.ColumnName))
                        continue;

                    var unit = units.FirstOrDefault(u =>
                        string.Equals(u.TableName, field.TableName, StringComparison.OrdinalIgnoreCase));
                    if (unit == null)
                        continue;
                    int parentUnitId = ResolveParentUnitId(unit, root);
                    int? parentPkId = GetUnitPrimaryKeyFieldId(conn, parentUnitId);
                    if (!parentPkId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Parent primary key was not found for '{field.TableName}.{field.ColumnName}' (isLinkToParentPrimaryKey).");
                    }
                    SetFieldParentLink(conn, transactionId, field.TableName.Trim(), field.ColumnName.Trim(), parentPkId.Value);
                }

                var relationshipHints = BuildRelationshipHints(tables);
                foreach (var unit in units)
                {
                    if (unit.UnitId == root.UnitId)
                        continue;
                    if (UnitHasFlag(conn, unit.UnitId, "IsLinkToParentPrimaryKey"))
                        continue;

                    int parentUnitId = ResolveParentUnitId(unit, root);
                    int? parentPkId = GetUnitPrimaryKeyFieldId(conn, parentUnitId);
                    string parentPkCol = GetUnitPrimaryKeyColumn(conn, parentUnitId);
                    if (!parentPkId.HasValue || string.IsNullOrWhiteSpace(parentPkCol) || string.IsNullOrWhiteSpace(unit.TableName))
                        continue;

                    string childCol = FindLogicalChildLinkColumn(conn, unit, parentPkCol, parentUnitId, units, relationshipHints);
                    if (string.IsNullOrWhiteSpace(childCol))
                        continue;
                    SetFieldParentLink(conn, transactionId, unit.TableName, childCol, parentPkId.Value);
                }
            }
        }

        private static List<PackUnitRow> LoadPackUnits(SqlConnection conn, int transactionId)
        {
            var units = new List<PackUnitRow>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TransactionUnitID, DataBaseTableName, ParentTransactionUnitID, ISNULL(IsMasterSiblingUnit, 0)
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId
ORDER BY TransactionUnitID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        units.Add(new PackUnitRow
                        {
                            UnitId = reader.GetInt32(0),
                            TableName = reader.IsDBNull(1) ? null : reader.GetString(1),
                            ParentUnitId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            IsSibling = Convert.ToBoolean(reader.GetValue(3))
                        });
                    }
                }
            }
            return units;
        }

        private static int ResolveParentUnitId(PackUnitRow unit, PackUnitRow root)
        {
            if (unit.IsSibling || !unit.ParentUnitId.HasValue)
                return root.UnitId;
            return unit.ParentUnitId.Value;
        }

        private static Dictionary<string, List<AppConfigPackRelationshipDto>> BuildRelationshipHints(
            List<AppConfigPackTableDto> tables)
        {
            var map = new Dictionary<string, List<AppConfigPackRelationshipDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables ?? Enumerable.Empty<AppConfigPackTableDto>())
            {
                if (table == null || string.IsNullOrWhiteSpace(table.Name) || table.Relationships == null)
                    continue;
                map[table.Name.Trim()] = table.Relationships
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.ForeignKeyColumn))
                    .ToList();
            }
            return map;
        }

        private static bool UnitHasFlag(SqlConnection conn, int unitId, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT TOP 1 1 FROM dbo.AppTransactionField WHERE TransactionUnitID = @UnitId AND {columnName} = 1";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                var val = cmd.ExecuteScalar();
                return val != null && val != DBNull.Value;
            }
        }

        private static int? GetUnitPrimaryKeyFieldId(SqlConnection conn, int unitId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 TransactionFieldID
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId AND IsPrimaryKey = 1
ORDER BY TransactionFieldID";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetUnitPrimaryKeyColumn(SqlConnection conn, int unitId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 DataBaseFieldName
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId AND IsPrimaryKey = 1
ORDER BY TransactionFieldID";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? null : Convert.ToString(val);
            }
        }

        private static void SetFieldPrimaryKey(SqlConnection conn, int transactionId, string tableName, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE f SET
    IsPrimaryKey = 1,
    IsReadonly = 1,
    IsVisible = 0,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.DataBaseTableName = @TableName
  AND f.DataBaseFieldName = @ColumnName";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                cmd.ExecuteNonQuery();
            }
        }

        private static void MarkHeuristicPrimaryKey(SqlConnection conn, int unitId, string parentPkColumn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE f SET
    IsPrimaryKey = 1,
    IsReadonly = 1,
    IsVisible = 0,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
WHERE f.TransactionFieldID = (
    SELECT TOP 1 TransactionFieldID
    FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @UnitId
      AND RIGHT(DataBaseFieldName, 2) = 'Id'
      AND (@ParentPkCol IS NULL OR DataBaseFieldName <> @ParentPkCol)
    ORDER BY SortOrder, TransactionFieldID)";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.Parameters.AddWithValue("@ParentPkCol", (object)parentPkColumn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SetFieldParentLink(
            SqlConnection conn,
            int transactionId,
            string tableName,
            string columnName,
            int parentPkFieldId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE f SET
    IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @ParentPkFieldId,
    IsReadonly = 1,
    IsVisible = 0,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.DataBaseTableName = @TableName
  AND f.DataBaseFieldName = @ColumnName";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                cmd.Parameters.AddWithValue("@ParentPkFieldId", parentPkFieldId);
                cmd.ExecuteNonQuery();
            }
        }

        private static string FindLogicalChildLinkColumn(
            SqlConnection conn,
            PackUnitRow unit,
            string parentPkColumn,
            int parentUnitId,
            List<PackUnitRow> units,
            Dictionary<string, List<AppConfigPackRelationshipDto>> relationshipHints)
        {
            var childColumns = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DataBaseFieldName
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId
ORDER BY SortOrder, TransactionFieldID";
                cmd.Parameters.AddWithValue("@UnitId", unit.UnitId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            childColumns.Add(reader.GetString(0));
                    }
                }
            }

            string sameName = childColumns.FirstOrDefault(c =>
                c.Equals(parentPkColumn, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(sameName))
                return sameName;

            var parent = units.FirstOrDefault(u => u.UnitId == parentUnitId);
            if (parent != null
                && relationshipHints.TryGetValue(unit.TableName ?? string.Empty, out var rels))
            {
                var rel = rels.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.TargetTable)
                    && r.TargetTable.Equals(parent.TableName, StringComparison.OrdinalIgnoreCase)
                    && childColumns.Any(c => c.Equals(r.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase)));
                if (rel != null)
                    return childColumns.First(c => c.Equals(rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase));
            }

            string alias = AppTransactionBL.ResolveLogicalParentLinkAlias(parentPkColumn);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                string aliasMatch = childColumns.FirstOrDefault(c =>
                    c.Equals(alias, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(aliasMatch))
                    return aliasMatch;
            }

            return null;
        }
    }
}
