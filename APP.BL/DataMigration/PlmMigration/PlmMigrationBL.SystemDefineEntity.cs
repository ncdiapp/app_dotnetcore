using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using App.BL;
using APP.Components.EntityDto;
using APP.Components.Dto;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const string SysEntityStatusReady = "Ready";
        private const string SysEntityStatusSkipped = "Skipped";
        private const string SysEntityStatusBlocked = "Blocked";
        private const string SysEntityActionInsert = "Insert";
        private const string SysEntityActionUpdate = "Update";

        private sealed class PlmSysDsRegisterMap
        {
            public int PlmDataSourceFrom { get; set; }
            public int DataSourceRegisterId { get; set; }
            public string DatabaseName { get; set; }
            public bool IsRegisterResolved { get; set; }
        }

        private sealed class PlmSysColumnRow
        {
            public int PlmEntityId { get; set; }
            public int UserDefineEntityColumnId { get; set; }
            public string SystemTableColumnName { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool UsedByDropDownList { get; set; }
            public int ColOrdinal { get; set; }
            public int? DisplayOrdinal { get; set; }
        }

        private sealed class PlmSysEntityRow
        {
            public int PlmEntityId { get; set; }
            public string PlmEntityCode { get; set; }
            public string TargetEntityCode { get; set; }
            public string Description { get; set; }
            public string TableName { get; set; }
            public string SchemaOwner { get; set; }
            public int? PlmDataSourceFrom { get; set; }
            public int? AppDataSourceFrom { get; set; }
            public string TargetDatabaseName { get; set; }
            public string IdentityField { get; set; }
            public string DisplayFiled1 { get; set; }
            public string DisplayFiled2 { get; set; }
            public string DisplayFiled3 { get; set; }
            public int PkColumnCount { get; set; }
            public int DisplayColumnCount { get; set; }
            public string ImportStatus { get; set; } = SysEntityStatusReady;
            public string ImportAction { get; set; }
            public string SkipReason { get; set; }
            public bool PhysicalTableOk { get; set; }
        }

        internal static PlmSystemDefineEntityPreviewDto BuildSystemDefineEntityPreview(
            string plmConnectionString,
            string dataSourceDiscoveryJson,
            string tenantConnectionString,
            string tablePrefix)
        {
            var preview = new PlmSystemDefineEntityPreviewDto();
            try
            {
                var discovery = ParseDataSourceDiscovery(dataSourceDiscoveryJson);
                var registerMaps = BuildPlmSysDsRegisterMap(discovery);
                preview.DataSourceMaps = registerMaps.Select(MapRegisterDto).ToList();

                string registerError = ValidateRegisterMaps(registerMaps, discovery);
                if (registerError != null)
                {
                    preview.IsSuccess = false;
                    preview.ErrorMessage = registerError;
                    return preview;
                }

                var staging = BuildPlmSysEntityStaging(plmConnectionString, registerMaps, tablePrefix);
                ValidatePhysicalTables(staging, registerMaps, tenantConnectionString);
                ApplyEntityCodeBlockers(staging, tenantConnectionString);
                AssignImportActions(staging, tenantConnectionString);

                preview.Entities = staging.Select(MapEntityPreviewItem).ToList();
                preview.ReadyCount = staging.Count(e => e.ImportStatus == SysEntityStatusReady);
                preview.SkippedCount = staging.Count(e => e.ImportStatus == SysEntityStatusSkipped);
                preview.BlockerCount = staging.Count(e => e.ImportStatus == SysEntityStatusBlocked);
                preview.Blockers = staging
                    .Where(e => e.ImportStatus == SysEntityStatusBlocked)
                    .Select(e => new PlmSystemDefineEntityBlockerDto
                    {
                        PlmEntityId = e.PlmEntityId,
                        TargetEntityCode = e.TargetEntityCode,
                        TableName = e.TableName,
                        TargetDatabaseName = e.TargetDatabaseName,
                        Issue = e.SkipReason
                    })
                    .ToList();

                preview.IsSuccess = true;
                if (preview.Entities.Count == 0)
                    preview.ErrorMessage = "No System Define PLM entities (EntityType = 1) were found in pdmEntity.";
            }
            catch (Exception ex)
            {
                preview.IsSuccess = false;
                preview.ErrorMessage = ex.Message;
            }

            return preview;
        }

        internal static PlmSystemDefineEntityImportResultDto ImportSystemDefineEntities(
            string plmConnectionString,
            string dataSourceDiscoveryJson,
            string tenantConnectionString,
            int? saasApplicationId,
            string tablePrefix,
            PlmExportProgressCallback progressCallback)
        {
            var result = new PlmSystemDefineEntityImportResultDto();
            var discovery = ParseDataSourceDiscovery(dataSourceDiscoveryJson);
            var registerMaps = BuildPlmSysDsRegisterMap(discovery);
            string registerError = ValidateRegisterMaps(registerMaps, discovery);
            if (registerError != null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = registerError;
                return result;
            }

            var staging = BuildPlmSysEntityStaging(plmConnectionString, registerMaps, tablePrefix);
            ValidatePhysicalTables(staging, registerMaps, tenantConnectionString);
            ApplyEntityCodeBlockers(staging, tenantConnectionString);
            AssignImportActions(staging, tenantConnectionString);

            result.SkippedCount = staging.Count(e => e.ImportStatus == SysEntityStatusSkipped);
            result.SkippedEntities = staging
                .Where(e => e.ImportStatus == SysEntityStatusSkipped)
                .Select(MapEntityPreviewItem)
                .ToList();
            result.Blockers = staging
                .Where(e => e.ImportStatus == SysEntityStatusBlocked)
                .Select(e => new PlmSystemDefineEntityBlockerDto
                {
                    PlmEntityId = e.PlmEntityId,
                    TargetEntityCode = e.TargetEntityCode,
                    TableName = e.TableName,
                    TargetDatabaseName = e.TargetDatabaseName,
                    Issue = e.SkipReason
                })
                .ToList();

            if (result.Blockers.Count > 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Blockers found — resolve EntityCode conflicts before importing.";
                return result;
            }

            var ready = staging.Where(e => e.ImportStatus == SysEntityStatusReady).ToList();
            if (ready.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "No Ready entities to import.";
                return result;
            }

            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < ready.Count; i++)
                        {
                            var entity = ready[i];
                            int percent = (int)Math.Round((i / (double)ready.Count) * 100);
                            progressCallback?.Invoke(percent,
                                $"Importing entity metadata {entity.TargetEntityCode}...");

                            if (entity.ImportAction == SysEntityActionUpdate)
                            {
                                UpdateSystemDefineEntity(conn, tran, entity, saasApplicationId);
                                result.UpdatedCount++;
                            }
                            else
                            {
                                InsertSystemDefineEntity(conn, tran, entity, saasApplicationId);
                                result.InsertedCount++;
                            }
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
            progressCallback?.Invoke(100, "Entity metadata import completed.");
            return result;
        }

        private static PlmDiscoverDataSourcesResultDto ParseDataSourceDiscovery(string dataSourceDiscoveryJson)
        {
            if (string.IsNullOrWhiteSpace(dataSourceDiscoveryJson))
                throw new InvalidOperationException("Data source discovery is missing on this session. Run Connect & Discover first.");

            var discovery = JsonConvert.DeserializeObject<PlmDiscoverDataSourcesResultDto>(dataSourceDiscoveryJson);
            if (discovery?.DataSources == null || discovery.DataSources.Count == 0)
                throw new InvalidOperationException("Data source discovery is empty. Run Connect & Discover first.");

            return discovery;
        }

        /// <summary>
        /// Only registers that discovery actually assigned must resolve. Skipped sources (B10: no PLM connection string) are OK.
        /// PLM (DSF=1) must always resolve to company master.
        /// </summary>
        private static string ValidateRegisterMaps(
            List<PlmSysDsRegisterMap> maps,
            PlmDiscoverDataSourcesResultDto discovery)
        {
            foreach (int plmFrom in new[] { 1, 2, 3, 4 })
            {
                var map = maps.First(m => m.PlmDataSourceFrom == plmFrom);
                var item = discovery.DataSources.FirstOrDefault(d => d.DataSourceFrom == plmFrom);
                bool mustResolve = plmFrom == 1
                    || (item?.RegisteredDataSourceId is int registerId && registerId > 0);

                if (!mustResolve)
                    continue;

                if (!map.IsRegisterResolved || string.IsNullOrWhiteSpace(map.DatabaseName))
                {
                    string name = GetPlmDataSourceFromName(plmFrom);
                    return plmFrom == 1
                        ? "Company Master database register could not be resolved for PLM (DataSourceFrom = 1)."
                        : $"Data source register for {name} could not be resolved in AppDataSourceRegister.";
                }
            }

            return null;
        }

        private static List<PlmSysDsRegisterMap> BuildPlmSysDsRegisterMap(PlmDiscoverDataSourcesResultDto discovery)
        {
            var maps = new List<PlmSysDsRegisterMap>();
            foreach (int plmFrom in new[] { 1, 2, 3, 4 })
            {
                var item = discovery.DataSources.FirstOrDefault(d => d.DataSourceFrom == plmFrom);
                var map = new PlmSysDsRegisterMap { PlmDataSourceFrom = plmFrom };
                if (item?.RegisteredDataSourceId is int registerId && registerId > 0)
                {
                    map.DataSourceRegisterId = registerId;
                    var register = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(registerId);
                    if (register != null && !string.IsNullOrWhiteSpace(register.DatabaseName))
                    {
                        map.DatabaseName = register.DatabaseName.Trim();
                        map.IsRegisterResolved = true;
                    }
                }

                maps.Add(map);
            }

            return maps;
        }

        private static List<PlmSysEntityRow> BuildPlmSysEntityStaging(
            string plmConnectionString,
            List<PlmSysDsRegisterMap> registerMaps,
            string tablePrefix)
        {
            var registerByPlmFrom = registerMaps.ToDictionary(m => m.PlmDataSourceFrom);
            var entities = new List<PlmSysEntityRow>();
            var columns = new List<PlmSysColumnRow>();

            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    e.EntityID,
    LEFT(LTRIM(RTRIM(e.EntityCode)), 200) AS EntityCode,
    LEFT(e.[Description], 500) AS [Description],
    LEFT(LTRIM(RTRIM(e.SysTableName)), 100) AS TableName,
    LEFT(ISNULL(NULLIF(LTRIM(RTRIM(e.SchemaOwner)), ''), 'dbo'), 50) AS SchemaOwner,
    e.DataSourceFrom
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0
ORDER BY e.EntityID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int? plmDsFrom = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                            int? appDsFrom = null;
                            if (plmDsFrom is int ds && registerByPlmFrom.TryGetValue(ds, out var reg))
                                appDsFrom = reg.DataSourceRegisterId;

                            entities.Add(new PlmSysEntityRow
                            {
                                PlmEntityId = reader.GetInt32(0),
                                PlmEntityCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                                TargetEntityCode = Truncate(reader.IsDBNull(1) ? null : reader.GetString(1), 100),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                TableName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                SchemaOwner = reader.IsDBNull(4) ? "dbo" : reader.GetString(4),
                                PlmDataSourceFrom = plmDsFrom,
                                AppDataSourceFrom = appDsFrom
                            });
                        }
                    }
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    c.EntityID,
    c.UserDefineEntityColumnID,
    LEFT(LTRIM(RTRIM(c.SystemTableColumnName)), 128) AS SystemTableColumnName,
    ISNULL(c.IsPrimaryKey, 0) AS IsPrimaryKey,
    ISNULL(c.UsedByDropDownList, 0) AS UsedByDropDownList,
    ISNULL(c.DataRowSort, 9999) AS DataRowSort
FROM dbo.pdmUserDefineEntityColumn c
INNER JOIN dbo.pdmEntity e ON e.EntityID = c.EntityID
WHERE e.EntityType = 1
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

                            columns.Add(new PlmSysColumnRow
                            {
                                PlmEntityId = entityId,
                                UserDefineEntityColumnId = reader.GetInt32(1),
                                SystemTableColumnName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                IsPrimaryKey = !reader.IsDBNull(3) && reader.GetBoolean(3),
                                UsedByDropDownList = !reader.IsDBNull(4) && reader.GetBoolean(4),
                                ColOrdinal = ordinalByEntity[entityId]
                            });
                        }
                    }
                }
            }

            MarkUnsupportedDataSources(entities);
            MarkEmptyTableNames(entities);
            ApplySystemDefineTablePrefix(entities, tablePrefix);
            ResolveTargetDatabaseNames(entities, registerByPlmFrom);
            ApplyDuplicateEntityCodePrefix(entities);

            var entityById = entities.ToDictionary(e => e.PlmEntityId);
            var columnsByEntity = columns
                .Where(c => entityById.ContainsKey(c.PlmEntityId))
                .GroupBy(c => c.PlmEntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady))
            {
                if (!columnsByEntity.TryGetValue(entity.PlmEntityId, out var entityCols))
                    entityCols = new List<PlmSysColumnRow>();

                AssignDisplayOrdinals(entityCols);
                ApplyColumnValidation(entity, entityCols);
            }

            return entities;
        }

        private static void MarkUnsupportedDataSources(List<PlmSysEntityRow> entities)
        {
            foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady))
            {
                if (!entity.AppDataSourceFrom.HasValue)
                {
                    entity.ImportStatus = SysEntityStatusSkipped;
                    entity.SkipReason = entity.PlmDataSourceFrom switch
                    {
                        null => "DataSourceFrom is NULL",
                        2 => "ERP data source not registered (skipped or not configured in PLM)",
                        3 => "DataWS data source not registered (no connection in PLM)",
                        4 => "OtherEx data source not registered (no connection in PLM)",
                        5 => "RestJson not supported",
                        6 => "RestXML not supported",
                        _ => "Data source not registered in AppDataSourceRegister"
                    };
                }
            }
        }

        private static void MarkEmptyTableNames(List<PlmSysEntityRow> entities)
        {
            foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady))
            {
                if (string.IsNullOrWhiteSpace(entity.TableName))
                {
                    entity.ImportStatus = SysEntityStatusSkipped;
                    entity.SkipReason = "SysTableName is empty";
                }
            }
        }

        private static void ApplySystemDefineTablePrefix(List<PlmSysEntityRow> entities, string tablePrefix)
        {
            foreach (var entity in entities.Where(e =>
                         e.ImportStatus == SysEntityStatusReady && e.PlmDataSourceFrom == 1))
            {
                entity.TableName = ResolveSystemDefineTargetTableName(entity.TableName, tablePrefix);
            }
        }

        private static void ResolveTargetDatabaseNames(
            List<PlmSysEntityRow> entities,
            Dictionary<int, PlmSysDsRegisterMap> registerByPlmFrom)
        {
            foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady))
            {
                if (entity.PlmDataSourceFrom is int ds && registerByPlmFrom.TryGetValue(ds, out var reg))
                    entity.TargetDatabaseName = reg.DatabaseName;
            }
        }

        private static void ApplyDuplicateEntityCodePrefix(List<PlmSysEntityRow> entities)
        {
            var ready = entities.Where(e => e.ImportStatus == SysEntityStatusReady).ToList();
            var groups = ready
                .GroupBy(e => e.TargetEntityCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                foreach (var entity in group.OrderBy(e => e.PlmEntityId).Skip(1))
                    entity.TargetEntityCode = Truncate("Plm_" + entity.TargetEntityCode, 100);
            }
        }

        private static List<PlmSysColumnRow> GetEligibleDisplayColumns(List<PlmSysColumnRow> entityCols)
        {
            return entityCols
                .Where(c => c.UsedByDropDownList && !c.IsPrimaryKey)
                .OrderBy(c => c.ColOrdinal)
                .ToList();
        }

        private static void AssignDisplayOrdinals(List<PlmSysColumnRow> entityCols)
        {
            int disp = 0;
            foreach (var col in GetEligibleDisplayColumns(entityCols))
            {
                disp++;
                if (disp <= 3)
                    col.DisplayOrdinal = disp;
            }
        }

        private static void ApplyColumnValidation(PlmSysEntityRow entity, List<PlmSysColumnRow> entityCols)
        {
            var pkCols = entityCols.Where(c => c.IsPrimaryKey).ToList();
            var displayCols = GetEligibleDisplayColumns(entityCols);

            entity.PkColumnCount = pkCols.Count;
            entity.DisplayColumnCount = displayCols.Count;
            entity.IdentityField = pkCols.Count == 1 ? pkCols[0].SystemTableColumnName : null;
            entity.DisplayFiled1 = displayCols.ElementAtOrDefault(0)?.SystemTableColumnName;
            entity.DisplayFiled2 = displayCols.ElementAtOrDefault(1)?.SystemTableColumnName;
            entity.DisplayFiled3 = displayCols.ElementAtOrDefault(2)?.SystemTableColumnName;

            if (entity.PkColumnCount != 1
                || entity.DisplayColumnCount < 1
                || string.IsNullOrWhiteSpace(entity.IdentityField))
            {
                entity.ImportStatus = SysEntityStatusSkipped;
                entity.SkipReason = entity.PkColumnCount != 1
                    ? "Need exactly one PK column (IsPrimaryKey)"
                    : entity.DisplayColumnCount < 1
                        ? "Need at least one UsedByDropDownList column"
                        : "PK SystemTableColumnName is empty";
            }
        }

        private static void ValidatePhysicalTables(
            List<PlmSysEntityRow> entities,
            List<PlmSysDsRegisterMap> registerMaps,
            string tenantConnectionString)
        {
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                foreach (var map in registerMaps)
                {
                    if (string.IsNullOrWhiteSpace(map.DatabaseName))
                        continue;

                    string dbName = SanitizeDatabaseName(map.DatabaseName);
                    foreach (var entity in entities.Where(e =>
                                 e.ImportStatus == SysEntityStatusReady
                                 && e.PlmDataSourceFrom == map.PlmDataSourceFrom))
                    {
                        if (TableExistsInDatabase(conn, dbName, entity.SchemaOwner, entity.TableName))
                            entity.PhysicalTableOk = true;
                    }
                }
            }

            foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady && !e.PhysicalTableOk))
            {
                entity.ImportStatus = SysEntityStatusSkipped;
                entity.SkipReason = "Physical table not found in datasource database";
            }
        }

        private static void ApplyEntityCodeBlockers(List<PlmSysEntityRow> entities, string tenantConnectionString)
        {
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady))
                {
                    if (HasEntityCodeConflict(conn, entity.TargetEntityCode, entity.PlmEntityId))
                    {
                        entity.ImportStatus = SysEntityStatusBlocked;
                        entity.SkipReason = "EntityCode already exists in AppEntityInfo";
                    }
                }
            }
        }

        private static void AssignImportActions(List<PlmSysEntityRow> entities, string tenantConnectionString)
        {
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                foreach (var entity in entities.Where(e => e.ImportStatus == SysEntityStatusReady))
                {
                    entity.ImportAction = IntegrationIdExists(conn, entity.PlmEntityId)
                        ? SysEntityActionUpdate
                        : SysEntityActionInsert;
                }
            }
        }

        private static bool IntegrationIdExists(SqlConnection conn, int plmEntityId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 1
FROM dbo.AppEntityInfo
WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", plmEntityId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool HasEntityCodeConflict(SqlConnection conn, string targetEntityCode, int plmEntityId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 1
FROM dbo.AppEntityInfo
WHERE EntityCode = @EntityCode
  AND (IntegrationId IS NULL OR IntegrationId <> @IntegrationId)";
                cmd.Parameters.AddWithValue("@EntityCode", (object)targetEntityCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IntegrationId", plmEntityId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void InsertSystemDefineEntity(
            SqlConnection conn, SqlTransaction tran, PlmSysEntityRow entity, int? saasApplicationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
INSERT INTO dbo.AppEntityInfo (
    EntityCode, [Description], EntityType, TableName, SchemaOwner,
    IdentityField, DisplayFiled1, DisplayFiled2, DisplayFiled3,
    DataSourceFrom, IsSystemDefine, SaasApplicationID, IntegrationId
)
VALUES (
    @EntityCode, @Description, @EntityType, @TableName, @SchemaOwner,
    @IdentityField, @DisplayFiled1, @DisplayFiled2, @DisplayFiled3,
    @DataSourceFrom, NULL, @SaasApplicationId, @IntegrationId
)";
                AddSystemDefineEntityParameters(cmd, entity, saasApplicationId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateSystemDefineEntity(
            SqlConnection conn, SqlTransaction tran, PlmSysEntityRow entity, int? saasApplicationId)
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
    SchemaOwner = @SchemaOwner,
    IdentityField = @IdentityField,
    DisplayFiled1 = @DisplayFiled1,
    DisplayFiled2 = @DisplayFiled2,
    DisplayFiled3 = @DisplayFiled3,
    DataSourceFrom = @DataSourceFrom,
    SaasApplicationID = @SaasApplicationId
WHERE IntegrationId = @IntegrationId";
                AddSystemDefineEntityParameters(cmd, entity, saasApplicationId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddSystemDefineEntityParameters(
            SqlCommand cmd, PlmSysEntityRow entity, int? saasApplicationId)
        {
            cmd.Parameters.AddWithValue("@EntityCode", (object)entity.TargetEntityCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityType", (int)EmAppEntityType.SystemDefineTable);
            cmd.Parameters.AddWithValue("@TableName", (object)entity.TableName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SchemaOwner", (object)entity.SchemaOwner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IdentityField", (object)entity.IdentityField ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DisplayFiled1", (object)entity.DisplayFiled1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DisplayFiled2", (object)entity.DisplayFiled2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DisplayFiled3", (object)entity.DisplayFiled3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DataSourceFrom", (object)entity.AppDataSourceFrom ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IntegrationId", entity.PlmEntityId);
        }

        private static bool TableExistsInDatabase(
            SqlConnection conn, string databaseName, string schema, string table, SqlTransaction tran = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $@"
SELECT 1
FROM [{databaseName}].sys.tables AS t
INNER JOIN [{databaseName}].sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = @Schema AND t.name = @Table";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", table);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static string SanitizeDatabaseName(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name is required.");

            string name = databaseName.Trim();
            if (name.IndexOfAny(new[] { '[', ']', ';', '\'', '"', ' ' }) >= 0)
                throw new ArgumentException($"Invalid database name: {databaseName}");

            return name;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength);
        }

        private static PlmSystemDefineDataSourceMapDto MapRegisterDto(PlmSysDsRegisterMap map)
        {
            return new PlmSystemDefineDataSourceMapDto
            {
                PlmDataSourceFrom = map.PlmDataSourceFrom,
                DataSourceRegisterId = map.DataSourceRegisterId,
                DatabaseName = map.DatabaseName,
                IsRegisterResolved = map.IsRegisterResolved
            };
        }

        private static PlmSystemDefineEntityPreviewItemDto MapEntityPreviewItem(PlmSysEntityRow entity)
        {
            return new PlmSystemDefineEntityPreviewItemDto
            {
                PlmEntityId = entity.PlmEntityId,
                PlmEntityCode = entity.PlmEntityCode,
                TargetEntityCode = entity.TargetEntityCode,
                Description = entity.Description,
                TableName = entity.TableName,
                SchemaOwner = entity.SchemaOwner,
                PlmDataSourceFrom = entity.PlmDataSourceFrom,
                AppDataSourceFrom = entity.AppDataSourceFrom,
                TargetDatabaseName = entity.TargetDatabaseName,
                IdentityField = entity.IdentityField,
                DisplayFiled1 = entity.DisplayFiled1,
                DisplayFiled2 = entity.DisplayFiled2,
                DisplayFiled3 = entity.DisplayFiled3,
                ImportStatus = entity.ImportStatus,
                ImportAction = entity.ImportAction,
                SkipReason = entity.SkipReason
            };
        }

        internal static void WriteSystemDefineEntityIssuesToLog(
            DatabaseFixture fixture,
            int sessionId,
            int? jobId,
            string action,
            string status,
            IEnumerable<PlmSystemDefineEntityPreviewItemDto> skipped,
            IEnumerable<PlmSystemDefineEntityBlockerDto> blockers)
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
