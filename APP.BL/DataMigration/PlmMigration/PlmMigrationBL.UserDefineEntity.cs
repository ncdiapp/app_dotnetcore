using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const string UdEntityStatusReady = "Ready";
        private const string UdEntityStatusSkipped = "Skipped";
        private const string UdEntityStatusBlocked = "Blocked";
        private const string UdEntityActionInsert = "Insert";
        private const string UdEntityActionUpdate = "Update";
        private const string UdAppTargetSimpleList = "SimpleValueList";
        private const string UdAppTargetWideTable = "SystemDefineTable";

        public const string PlmUserDefineActionPreview = "UserDefineEntityPreview";
        public const string PlmUserDefineActionImport = "UserDefineEntityImport";

        private sealed class PlmUdEntityRow
        {
            public int PlmEntityId { get; set; }
            public string PlmEntityCode { get; set; }
            public string TargetEntityCode { get; set; }
            public string TargetTableName { get; set; }
            public string Description { get; set; }
            public int ColumnCount { get; set; }
            public int TargetEntityType { get; set; }
            public int PlmRowCount { get; set; }
            public int ImportOrder { get; set; }
            public string ImportStatus { get; set; } = UdEntityStatusReady;
            public string ImportAction { get; set; }
            public string SkipReason { get; set; }
            public string AppTargetType =>
                TargetEntityType == (int)EmAppEntityType.SimpleValueList ? UdAppTargetSimpleList : UdAppTargetWideTable;
        }

        private sealed class PlmUdColumnRow
        {
            public int PlmEntityId { get; set; }
            public int UserDefineEntityColumnId { get; set; }
            public string ColumnName { get; set; }
            public string TargetSqlColumnName { get; set; }
            public int ColOrdinal { get; set; }
            public bool UsedByDropDownList { get; set; }
            public int? DisplayOrdinal { get; set; }
            public bool IsCodeColumn { get; set; }
            public bool IsDescColumn { get; set; }
            public int? UiControlType { get; set; }
            public int? FkEntityId { get; set; }
        }

        internal static PlmUserDefineEntityPreviewDto BuildUserDefineEntityPreview(
            string plmConnectionString,
            string dataSourceDiscoveryJson,
            string tenantConnectionString,
            int tenantDataSourceRegisterId,
            string entityWideTablePrefix)
        {
            var preview = new PlmUserDefineEntityPreviewDto();
            try
            {
                var staging = BuildUserDefineStaging(
                    plmConnectionString, tenantConnectionString, tenantDataSourceRegisterId, entityWideTablePrefix);
                preview.Entities = staging.Entities.Select(MapUserDefinePreviewItem).ToList();
                preview.ReadyCount = staging.Entities.Count(e => e.ImportStatus == UdEntityStatusReady);
                preview.SkippedCount = staging.Entities.Count(e => e.ImportStatus == UdEntityStatusSkipped);
                preview.BlockerCount = staging.Entities.Count(e => e.ImportStatus == UdEntityStatusBlocked);
                preview.Blockers = staging.Entities
                    .Where(e => e.ImportStatus == UdEntityStatusBlocked)
                    .Select(e => new PlmUserDefineEntityBlockerDto
                    {
                        PlmEntityId = e.PlmEntityId,
                        TargetEntityCode = e.TargetEntityCode,
                        TableName = e.TargetTableName,
                        Issue = e.SkipReason
                    })
                    .ToList();
                preview.IsSuccess = true;
                if (preview.Entities.Count == 0)
                    preview.ErrorMessage = "No User Define PLM entities (EntityType = 4) were found in pdmEntity.";
            }
            catch (Exception ex)
            {
                preview.IsSuccess = false;
                preview.ErrorMessage = ex.Message;
            }

            return preview;
        }

        internal static PlmUserDefineEntityImportResultDto ImportUserDefineEntities(
            string plmConnectionString,
            string tenantConnectionString,
            int tenantDataSourceRegisterId,
            int? saasApplicationId,
            string entityWideTablePrefix,
            PlmExportProgressCallback progressCallback)
        {
            var result = new PlmUserDefineEntityImportResultDto();
            var staging = BuildUserDefineStaging(
                plmConnectionString, tenantConnectionString, tenantDataSourceRegisterId, entityWideTablePrefix);

            result.SkippedCount = staging.Entities.Count(e => e.ImportStatus == UdEntityStatusSkipped);
            result.SkippedEntities = staging.Entities
                .Where(e => e.ImportStatus == UdEntityStatusSkipped)
                .Select(MapUserDefinePreviewItem)
                .ToList();
            result.Blockers = staging.Entities
                .Where(e => e.ImportStatus == UdEntityStatusBlocked)
                .Select(e => new PlmUserDefineEntityBlockerDto
                {
                    PlmEntityId = e.PlmEntityId,
                    TargetEntityCode = e.TargetEntityCode,
                    TableName = e.TargetTableName,
                    Issue = e.SkipReason
                })
                .ToList();

            if (result.Blockers.Count > 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Blockers found — resolve EntityCode / table conflicts before import.";
                return result;
            }

            var ready = staging.Entities
                .Where(e => e.ImportStatus == UdEntityStatusReady)
                .OrderBy(e => e.ImportOrder)
                .ThenBy(e => e.TargetEntityCode)
                .ToList();

            if (ready.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "No Ready entities to import.";
                return result;
            }

            string tenantDbName = GetTenantDatabaseName(tenantDataSourceRegisterId);
            var columnsByEntity = staging.Columns
                .GroupBy(c => c.PlmEntityId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.ColOrdinal).ToList());

            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();
                using (var tran = tenantConn.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < ready.Count; i++)
                        {
                            var entity = ready[i];
                            int percent = (int)Math.Round((i / (double)ready.Count) * 100);
                            progressCallback?.Invoke(percent, $"Importing {entity.TargetEntityCode}...");

                            var entityCols = columnsByEntity.TryGetValue(entity.PlmEntityId, out var cols)
                                ? cols
                                : new List<PlmUdColumnRow>();

                            int rows = ImportOneUserDefineEntity(
                                plmConn, tenantConn, tran, tenantDbName,
                                entity, entityCols, tenantDataSourceRegisterId, saasApplicationId);

                            result.RowsImported += rows;
                            if (entity.ImportAction == UdEntityActionUpdate)
                                result.UpdatedCount++;
                            else
                                result.InsertedCount++;
                        }

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                        return result;
                    }
                }
            }

            result.IsSuccess = true;
            progressCallback?.Invoke(100, "User Define entity import completed.");
            return result;
        }

        private sealed class PlmUdStagingResult
        {
            public List<PlmUdEntityRow> Entities { get; set; } = new List<PlmUdEntityRow>();
            public List<PlmUdColumnRow> Columns { get; set; } = new List<PlmUdColumnRow>();
        }

        private static PlmUdStagingResult BuildUserDefineStaging(
            string plmConnectionString,
            string tenantConnectionString,
            int tenantDataSourceRegisterId,
            string entityWideTablePrefix)
        {
            var result = new PlmUdStagingResult();
            string wideTablePrefix = SanitizeImportTablePrefix(entityWideTablePrefix, DefaultEntityWideTablePrefix);
            string tenantDbName = GetTenantDatabaseName(tenantDataSourceRegisterId);

            using (var plmConn = new SqlConnection(plmConnectionString))
            {
                plmConn.Open();
                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    e.EntityID,
    LEFT(LTRIM(RTRIM(e.EntityCode)), 200) AS EntityCode,
    LEFT(e.[Description], 500) AS [Description],
    ISNULL(cc.ColumnCount, 0) AS ColumnCount
FROM dbo.pdmEntity e
OUTER APPLY (
    SELECT COUNT(*) AS ColumnCount
    FROM dbo.pdmUserDefineEntityColumn c
    WHERE c.EntityID = e.EntityID
) cc
WHERE e.EntityType = 4
  AND ISNULL(e.IsRelationEntity, 0) = 0
ORDER BY e.EntityID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int columnCount = reader.GetInt32(3);
                            int targetType = columnCount <= 2
                                ? (int)EmAppEntityType.SimpleValueList
                                : (int)EmAppEntityType.SystemDefineTable;
                            string plmCode = reader.IsDBNull(1) ? null : reader.GetString(1);
                            string sanitized = SanitizeSqlIdentifier(plmCode, 100, "Entity_", reader.GetInt32(0));

                            result.Entities.Add(new PlmUdEntityRow
                            {
                                PlmEntityId = reader.GetInt32(0),
                                PlmEntityCode = plmCode,
                                TargetEntityCode = sanitized,
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                ColumnCount = columnCount,
                                TargetEntityType = targetType,
                                TargetTableName = targetType == (int)EmAppEntityType.SystemDefineTable
                                    ? Truncate(wideTablePrefix + sanitized, 100)
                                    : null
                            });
                        }
                    }
                }

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    c.EntityID,
    c.UserDefineEntityColumnID,
    LEFT(LTRIM(RTRIM(c.ColumnName)), 200) AS ColumnName,
    LEFT(LTRIM(RTRIM(
        CASE
            WHEN NULLIF(LTRIM(RTRIM(c.SystemTableColumnName)), '') IS NOT NULL THEN c.SystemTableColumnName
            ELSE c.ColumnName
        END
    )), 4000) AS TargetSqlColumnName,
    ISNULL(c.UsedByDropDownList, 0) AS UsedByDropDownList,
    c.UIControlType,
    c.FKEntityID
FROM dbo.pdmUserDefineEntityColumn c
INNER JOIN dbo.pdmEntity e ON e.EntityID = c.EntityID
WHERE e.EntityType = 4
  AND ISNULL(e.IsRelationEntity, 0) = 0
ORDER BY c.EntityID, ISNULL(c.DataRowSort, 9999), c.UserDefineEntityColumnID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        var ordinalByEntity = new Dictionary<int, int>();
                        while (reader.Read())
                        {
                            int entityId = reader.GetInt32(0);
                            if (!ordinalByEntity.ContainsKey(entityId))
                                ordinalByEntity[entityId] = 0;
                            ordinalByEntity[entityId]++;

                            result.Columns.Add(new PlmUdColumnRow
                            {
                                PlmEntityId = entityId,
                                UserDefineEntityColumnId = reader.GetInt32(1),
                                ColumnName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                TargetSqlColumnName = SanitizeSqlIdentifier(
                                    reader.IsDBNull(3) ? null : reader.GetString(3),
                                    128, "Col_", reader.GetInt32(1)),
                                ColOrdinal = ordinalByEntity[entityId],
                                UsedByDropDownList = !reader.IsDBNull(4) && reader.GetBoolean(4),
                                UiControlType = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                FkEntityId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6)
                            });
                        }
                    }
                }

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT r.EntityID, COUNT(*) AS Cnt
FROM dbo.pdmUserDefineEntityRow r
INNER JOIN dbo.pdmEntity e ON e.EntityID = r.EntityID
WHERE e.EntityType = 4 AND ISNULL(e.IsRelationEntity, 0) = 0
GROUP BY r.EntityID";
                    var rowCounts = new Dictionary<int, int>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            rowCounts[reader.GetInt32(0)] = reader.GetInt32(1);
                    }

                    foreach (var entity in result.Entities)
                    {
                        if (rowCounts.TryGetValue(entity.PlmEntityId, out int count))
                            entity.PlmRowCount = count;
                    }
                }
            }

            DedupeColumnNamesPerEntity(result.Columns);
            ApplyUserDefineColumnFlags(result.Entities, result.Columns);
            ApplyUserDefineEntityCodeRules(result.Entities, tenantConnectionString);
            SyncUserDefineTableNames(result.Entities);
            AssignUserDefineImportOrder(result.Entities, result.Columns);
            ApplyUserDefineValidation(result.Entities, result.Columns, tenantConnectionString, tenantDbName, wideTablePrefix);

            return result;
        }

        private static void DedupeColumnNamesPerEntity(List<PlmUdColumnRow> columns)
        {
            foreach (var group in columns.GroupBy(c => c.PlmEntityId))
            {
                var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var col in group.OrderBy(c => c.ColOrdinal))
                {
                    string name = col.TargetSqlColumnName;
                    if (!used.Add(name))
                    {
                        name = Truncate($"{name}_{col.UserDefineEntityColumnId}", 128);
                        col.TargetSqlColumnName = name;
                        used.Add(name);
                    }
                }
            }
        }

        private static void ApplyUserDefineColumnFlags(
            List<PlmUdEntityRow> entities,
            List<PlmUdColumnRow> columns)
        {
            var entityById = entities.ToDictionary(e => e.PlmEntityId);
            foreach (var group in columns.GroupBy(c => c.PlmEntityId))
            {
                if (!entityById.TryGetValue(group.Key, out var entity))
                    continue;

                int disp = 0;
                foreach (var col in group.Where(c => c.UsedByDropDownList).OrderBy(c => c.ColOrdinal))
                {
                    disp++;
                    if (disp <= 3)
                        col.DisplayOrdinal = disp;
                }

                if (entity.TargetEntityType == (int)EmAppEntityType.SimpleValueList)
                {
                    var first = group.FirstOrDefault(c => c.ColOrdinal == 1);
                    var second = group.FirstOrDefault(c => c.ColOrdinal == 2);
                    if (first != null) first.IsCodeColumn = true;
                    if (second != null) second.IsDescColumn = true;
                }
            }
        }

        private static void ApplyUserDefineEntityCodeRules(
            List<PlmUdEntityRow> entities,
            string tenantConnectionString)
        {
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                foreach (var entity in entities)
                {
                    if (EntityCodeExistsInApp(conn, entity.TargetEntityCode))
                        entity.TargetEntityCode = Truncate("Plm_" + entity.TargetEntityCode, 100);
                }
            }

            foreach (var group in entities
                         .GroupBy(e => e.TargetEntityCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Count() > 1))
            {
                foreach (var entity in group)
                    entity.TargetEntityCode = Truncate(
                        $"{entity.TargetEntityCode}_{entity.PlmEntityId}", 100);
            }
        }

        private static void SyncUserDefineTableNames(List<PlmUdEntityRow> entities)
        {
            // Physical table = {TablePrefix}Entity_{sanitized PLM code}. Do not re-derive from TargetEntityCode
            // after Plm_ duplicate prefix — EntityCode and TableName diverge by design.
            foreach (var group in entities
                         .Where(e => !string.IsNullOrWhiteSpace(e.TargetTableName))
                         .GroupBy(e => e.TargetTableName, StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Count() > 1))
            {
                foreach (var entity in group)
                    entity.TargetTableName = Truncate(
                        $"{entity.TargetTableName}_{entity.PlmEntityId}", 100);
            }
        }

        private static void AssignUserDefineImportOrder(
            List<PlmUdEntityRow> entities,
            List<PlmUdColumnRow> columns)
        {
            var entityIds = new HashSet<int>(entities.Select(e => e.PlmEntityId));
            var deps = columns
                .Where(c => c.FkEntityId is int fk && entityIds.Contains(fk))
                .Select(c => (Child: c.PlmEntityId, Parent: c.FkEntityId.Value))
                .Distinct()
                .ToList();

            var levels = entities.ToDictionary(e => e.PlmEntityId, _ => 0);
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var dep in deps)
                {
                    int newLevel = levels[dep.Parent] + 1;
                    if (levels[dep.Child] < newLevel)
                    {
                        levels[dep.Child] = newLevel;
                        changed = true;
                    }
                }
            }

            foreach (var entity in entities)
                entity.ImportOrder = levels[entity.PlmEntityId];
        }

        private static void ApplyUserDefineValidation(
            List<PlmUdEntityRow> entities,
            List<PlmUdColumnRow> columns,
            string tenantConnectionString,
            string tenantDbName,
            string entityWideTablePrefix)
        {
            foreach (var entity in entities.Where(e => e.ColumnCount == 0))
            {
                entity.ImportStatus = UdEntityStatusSkipped;
                entity.SkipReason = "No column definitions in pdmUserDefineEntityColumn";
            }

            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                foreach (var entity in entities.Where(e => e.ImportStatus == UdEntityStatusReady))
                {
                    if (HasEntityCodeConflict(conn, entity.TargetEntityCode, entity.PlmEntityId))
                    {
                        entity.ImportStatus = UdEntityStatusBlocked;
                        entity.SkipReason = "EntityCode already exists in AppEntityInfo";
                        continue;
                    }

                    entity.ImportAction = IntegrationIdExists(conn, entity.PlmEntityId)
                        ? UdEntityActionUpdate
                        : UdEntityActionInsert;

                    if (entity.ImportAction == UdEntityActionInsert
                        && entity.TargetEntityType == (int)EmAppEntityType.SystemDefineTable
                        && !string.IsNullOrWhiteSpace(entity.TargetTableName)
                        && TableExistsInDatabase(conn, tenantDbName, "dbo", entity.TargetTableName))
                    {
                        entity.ImportStatus = UdEntityStatusBlocked;
                        entity.SkipReason = $"Physical table {entityWideTablePrefix}* already exists";
                    }
                }
            }
        }

        private static int ImportOneUserDefineEntity(
            SqlConnection plmConn,
            SqlConnection tenantConn,
            SqlTransaction tran,
            string tenantDbName,
            PlmUdEntityRow entity,
            List<PlmUdColumnRow> entityCols,
            int tenantDataSourceRegisterId,
            int? saasApplicationId)
        {
            string disp1 = entityCols.FirstOrDefault(c => c.DisplayOrdinal == 1)?.TargetSqlColumnName;
            string disp2 = entityCols.FirstOrDefault(c => c.DisplayOrdinal == 2)?.TargetSqlColumnName;
            string disp3 = entityCols.FirstOrDefault(c => c.DisplayOrdinal == 3)?.TargetSqlColumnName;

            int entityInfoId;
            if (entity.ImportAction == UdEntityActionUpdate)
            {
                entityInfoId = GetEntityInfoIdByIntegrationId(tenantConn, tran, entity.PlmEntityId);
                UpdateUserDefineEntityInfo(
                    tenantConn, tran, entity, disp1, disp2, disp3, tenantDataSourceRegisterId, saasApplicationId);
            }
            else
            {
                entityInfoId = InsertUserDefineEntityInfo(
                    tenantConn, tran, entity, disp1, disp2, disp3, tenantDataSourceRegisterId, saasApplicationId);
            }

            if (entity.TargetEntityType == (int)EmAppEntityType.SimpleValueList)
                return ImportSimpleValueListData(plmConn, tenantConn, tran, entity, entityCols, entityInfoId);

            return ImportWideTableData(plmConn, tenantConn, tran, tenantDbName, entity, entityCols);
        }

        private static int InsertUserDefineEntityInfo(
            SqlConnection conn, SqlTransaction tran, PlmUdEntityRow entity,
            string disp1, string disp2, string disp3,
            int dataSourceFrom, int? saasApplicationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
INSERT INTO dbo.AppEntityInfo (
    EntityCode, [Description], EntityType, TableName,
    IdentityField, DisplayFiled1, DisplayFiled2, DisplayFiled3,
    DataSourceFrom, IsSystemDefine, SaasApplicationID, IntegrationId
)
OUTPUT INSERTED.EntityInfoID
VALUES (
    @EntityCode, @Description, @EntityType, @TableName,
    CASE WHEN @EntityType = @WideType THEN N'RowID' ELSE NULL END,
    @Disp1, @Disp2, @Disp3,
    @DataSourceFrom, NULL, @SaasApplicationId, @IntegrationId
)";
                AddUserDefineEntityInfoParameters(cmd, entity, disp1, disp2, disp3, dataSourceFrom, saasApplicationId);
                return (int)cmd.ExecuteScalar();
            }
        }

        private static void UpdateUserDefineEntityInfo(
            SqlConnection conn, SqlTransaction tran, PlmUdEntityRow entity,
            string disp1, string disp2, string disp3,
            int dataSourceFrom, int? saasApplicationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
UPDATE dbo.AppEntityInfo SET
    EntityCode = @EntityCode,
    [Description] = @Description,
    EntityType = @EntityType,
    TableName = @TableName,
    IdentityField = CASE WHEN @EntityType = @WideType THEN N'RowID' ELSE NULL END,
    DisplayFiled1 = @Disp1,
    DisplayFiled2 = @Disp2,
    DisplayFiled3 = @Disp3,
    DataSourceFrom = @DataSourceFrom,
    SaasApplicationID = @SaasApplicationId
WHERE IntegrationId = @IntegrationId";
                AddUserDefineEntityInfoParameters(cmd, entity, disp1, disp2, disp3, dataSourceFrom, saasApplicationId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddUserDefineEntityInfoParameters(
            SqlCommand cmd, PlmUdEntityRow entity,
            string disp1, string disp2, string disp3,
            int dataSourceFrom, int? saasApplicationId)
        {
            cmd.Parameters.AddWithValue("@EntityCode", (object)entity.TargetEntityCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityType", entity.TargetEntityType);
            cmd.Parameters.AddWithValue("@WideType", (int)EmAppEntityType.SystemDefineTable);
            cmd.Parameters.AddWithValue("@TableName", (object)entity.TargetTableName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Disp1", (object)disp1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Disp2", (object)disp2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Disp3", (object)disp3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DataSourceFrom", dataSourceFrom);
            cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IntegrationId", entity.PlmEntityId);
        }

        private static int GetEntityInfoIdByIntegrationId(SqlConnection conn, SqlTransaction tran, int plmEntityId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 EntityInfoID FROM dbo.AppEntityInfo WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", plmEntityId);
                object scalar = cmd.ExecuteScalar();
                if (scalar == null)
                    throw new InvalidOperationException($"AppEntityInfo not found for IntegrationId {plmEntityId}.");
                return Convert.ToInt32(scalar);
            }
        }

        private static int ImportSimpleValueListData(
            SqlConnection plmConn,
            SqlConnection tenantConn,
            SqlTransaction tran,
            PlmUdEntityRow entity,
            List<PlmUdColumnRow> entityCols,
            int entityInfoId)
        {
            var codeCol = entityCols.FirstOrDefault(c => c.IsCodeColumn);
            var descCol = entityCols.FirstOrDefault(c => c.IsDescColumn);

            using (var del = tenantConn.CreateCommand())
            {
                del.Transaction = tran;
                del.CommandText = "DELETE FROM dbo.AppEntitySimpleListValue WHERE EntityInfoID = @EntityInfoID";
                del.Parameters.AddWithValue("@EntityInfoID", entityInfoId);
                del.ExecuteNonQuery();
            }

            if (codeCol == null)
                return 0;

            string codeExpr = BuildValueExpression("cv1", codeCol.UiControlType);
            string descSelect = descCol != null
                ? $"LEFT(ISNULL({BuildValueExpression("cv2", descCol.UiControlType)}, N''), 500)"
                : "N''";

            string sql = $@"
SELECT
    r.RowID,
    LEFT(ISNULL({codeExpr}, N''), 100) AS Code,
    {descSelect} AS [Description]
FROM dbo.pdmUserDefineEntityRow r
LEFT JOIN dbo.pdmUserDefineEntityRowValue cv1
    ON cv1.RowID = r.RowID AND cv1.UserDefineEntityColumnID = @CodeColId
LEFT JOIN dbo.pdmUserDefineEntityRowValue cv2
    ON cv2.RowID = r.RowID AND cv2.UserDefineEntityColumnID = @DescColId
WHERE r.EntityID = @PlmEntityId";

            int count = 0;
            using (var readCmd = plmConn.CreateCommand())
            {
                readCmd.CommandText = sql;
                readCmd.Parameters.AddWithValue("@CodeColId", codeCol.UserDefineEntityColumnId);
                readCmd.Parameters.AddWithValue("@DescColId", (object)descCol?.UserDefineEntityColumnId ?? DBNull.Value);
                readCmd.Parameters.AddWithValue("@PlmEntityId", entity.PlmEntityId);

                using (var reader = readCmd.ExecuteReader())
                using (var ins = tenantConn.CreateCommand())
                {
                    ins.Transaction = tran;
                    ins.CommandText = @"
INSERT INTO dbo.AppEntitySimpleListValue (
    EntityInfoID, Sort, Code, [Description], InternalKey, AppCreatedByID, AppCreatedByCompanyID
)
VALUES (@EntityInfoID, @Sort, @Code, @Description, @InternalKey, NULL, NULL)";
                    var pEntityInfoId = ins.Parameters.Add("@EntityInfoID", SqlDbType.Int);
                    var pSort = ins.Parameters.Add("@Sort", SqlDbType.Int);
                    var pCode = ins.Parameters.Add("@Code", SqlDbType.NVarChar, 100);
                    var pDesc = ins.Parameters.Add("@Description", SqlDbType.NVarChar, 500);
                    var pKey = ins.Parameters.Add("@InternalKey", SqlDbType.Int);

                    while (reader.Read())
                    {
                        int rowId = reader.GetInt32(0);
                        string code = reader.IsDBNull(1) ? string.Empty : (reader.GetString(1) ?? string.Empty);
                        string desc = reader.IsDBNull(2) ? string.Empty : (reader.GetString(2) ?? string.Empty);

                        pEntityInfoId.Value = entityInfoId;
                        pSort.Value = rowId;
                        pCode.Value = code;
                        pDesc.Value = desc;
                        pKey.Value = rowId;
                        ins.ExecuteNonQuery();
                        count++;
                    }
                }
            }

            return count;
        }

        private static int ImportWideTableData(
            SqlConnection plmConn,
            SqlConnection tenantConn,
            SqlTransaction tran,
            string tenantDbName,
            PlmUdEntityRow entity,
            List<PlmUdColumnRow> entityCols)
        {
            if (string.IsNullOrWhiteSpace(entity.TargetTableName))
                return 0;

            if (TableExistsInDatabase(tenantConn, tenantDbName, "dbo", entity.TargetTableName, tran))
                TruncateUserDefineWideTable(tenantConn, tran, tenantDbName, entity.TargetTableName);
            else
                CreateUserDefineWideTable(tenantConn, tran, tenantDbName, entity.TargetTableName, entityCols);

            var selList = new StringBuilder("r.RowID");
            foreach (var col in entityCols.OrderBy(c => c.ColOrdinal))
            {
                selList.Append(", MAX(CASE WHEN rv.UserDefineEntityColumnID = ")
                    .Append(col.UserDefineEntityColumnId)
                    .Append(" THEN ")
                    .Append(BuildValueExpression("rv", col.UiControlType))
                    .Append(" END) AS [").Append(col.TargetSqlColumnName).Append(']');
            }

            string selectSql = $@"
SELECT {selList}
FROM dbo.pdmUserDefineEntityRow r
LEFT JOIN dbo.pdmUserDefineEntityRowValue rv ON rv.RowID = r.RowID
WHERE r.EntityID = @PlmEntityId
GROUP BY r.RowID";

            var table = new DataTable();
            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = selectSql;
                cmd.Parameters.AddWithValue("@PlmEntityId", entity.PlmEntityId);
                using (var reader = cmd.ExecuteReader())
                    table.Load(reader);
            }

            if (table.Rows.Count == 0)
                return 0;

            using (var bulk = new SqlBulkCopy(tenantConn, SqlBulkCopyOptions.Default, tran))
            {
                bulk.DestinationTableName = $"dbo.[{entity.TargetTableName}]";
                foreach (DataColumn column in table.Columns)
                    bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                bulk.WriteToServer(table);
            }

            return table.Rows.Count;
        }

        private static void CreateUserDefineWideTable(
            SqlConnection conn,
            SqlTransaction tran,
            string tenantDbName,
            string tableName,
            List<PlmUdColumnRow> entityCols)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE [{tenantDbName}].dbo.[{tableName}] (");
            sb.Append($"RowID int NOT NULL CONSTRAINT [PK_{tableName.Replace(".", "_")}] PRIMARY KEY");
            foreach (var col in entityCols.OrderBy(c => c.ColOrdinal))
                sb.Append($", [{col.TargetSqlColumnName}] {MapUiControlToSqlType(col.UiControlType)}");
            sb.Append(')');

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = sb.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        private static void TruncateUserDefineWideTable(
            SqlConnection conn, SqlTransaction tran, string tenantDbName, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $"TRUNCATE TABLE [{tenantDbName}].dbo.[{tableName}]";
                cmd.ExecuteNonQuery();
            }
        }

        private static string MapUiControlToSqlType(int? uiControlType)
        {
            if (uiControlType == 7) return "datetime NULL";
            if (uiControlType == 13) return "bit NULL";
            if (uiControlType == 1 || uiControlType == 20) return "int NULL";
            return "nvarchar(4000) NULL";
        }

        private static string BuildValueExpression(string alias, int? uiControlType)
        {
            if (uiControlType == 7)
                return $"CASE WHEN {alias}.ValueDate IS NOT NULL THEN {alias}.ValueDate ELSE TRY_CAST({alias}.ValueText AS datetime) END";
            if (uiControlType == 13)
                return $"CASE WHEN {alias}.ValueID IS NOT NULL THEN {alias}.ValueID WHEN {alias}.ValueText IN (N'1',N'true',N'True') THEN 1 WHEN {alias}.ValueText IN (N'0',N'false',N'False') THEN 0 ELSE TRY_CAST({alias}.ValueText AS bit) END";
            if (uiControlType == 1 || uiControlType == 20)
                return $"CASE WHEN {alias}.ValueID IS NOT NULL THEN {alias}.ValueID ELSE TRY_CAST({alias}.ValueText AS int) END";
            return $"CASE WHEN {alias}.ValueID IS NOT NULL THEN CAST({alias}.ValueID AS nvarchar(4000)) WHEN {alias}.ValueDate IS NOT NULL THEN CONVERT(nvarchar(30), {alias}.ValueDate, 121) ELSE {alias}.ValueText END";
        }

        private static bool EntityCodeExistsInApp(SqlConnection conn, string entityCode)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 1 FROM dbo.AppEntityInfo WHERE EntityCode = @EntityCode";
                cmd.Parameters.AddWithValue("@EntityCode", (object)entityCode ?? DBNull.Value);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static string SanitizeSqlIdentifier(string input, int maxLength, string fallbackPrefix, int fallbackId)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Truncate($"{fallbackPrefix}{fallbackId}", maxLength);

            var sb = new StringBuilder();
            foreach (char ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
                else if (sb.Length == 0 || sb[sb.Length - 1] != '_')
                    sb.Append('_');
            }

            string result = sb.ToString().Trim('_');
            if (result.Length == 0)
                result = $"{fallbackPrefix}{fallbackId}";
            if (char.IsDigit(result[0]))
                result = "T_" + result;
            return Truncate(result, maxLength);
        }

        private static PlmUserDefineEntityPreviewItemDto MapUserDefinePreviewItem(PlmUdEntityRow entity)
        {
            return new PlmUserDefineEntityPreviewItemDto
            {
                PlmEntityId = entity.PlmEntityId,
                PlmEntityCode = entity.PlmEntityCode,
                TargetEntityCode = entity.TargetEntityCode,
                Description = entity.Description,
                TableName = entity.TargetTableName,
                AppTargetType = entity.AppTargetType,
                ColumnCount = entity.ColumnCount,
                PlmRowCount = entity.PlmRowCount,
                ImportOrder = entity.ImportOrder,
                ImportStatus = entity.ImportStatus,
                ImportAction = entity.ImportAction,
                SkipReason = entity.SkipReason
            };
        }

        internal static void WriteUserDefineEntityIssuesToLog(
            DatabaseFixture fixture,
            int sessionId,
            int? jobId,
            string action,
            string status,
            IEnumerable<PlmUserDefineEntityPreviewItemDto> skipped,
            IEnumerable<PlmUserDefineEntityBlockerDto> blockers)
        {
            if (skipped != null)
            {
                foreach (var item in skipped)
                {
                    WriteImportLog(
                        fixture, sessionId, jobId, StepEntity, action, status,
                        item.TargetEntityCode, item.PlmEntityId.ToString(), null, null,
                        $"Skipped: {item.SkipReason}");
                }
            }

            if (blockers != null)
            {
                foreach (var blocker in blockers)
                {
                    WriteImportLog(
                        fixture, sessionId, jobId, StepEntity, action, status,
                        blocker.TargetEntityCode, blocker.PlmEntityId.ToString(), null, null,
                        $"Blocker: {blocker.Issue}");
                }
            }
        }
    }
}
