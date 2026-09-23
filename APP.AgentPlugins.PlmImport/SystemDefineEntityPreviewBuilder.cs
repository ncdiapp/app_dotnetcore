using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Pure PLM-read + tenant validation for System Define entity import preview.
/// Host resolves AppDataSourceRegister maps and passes dataSourceMapsJson.
/// </summary>
public static class SystemDefineEntityPreviewBuilder
{
    private const string DefaultTablePrefix = "Plm_";
    private const string StatusReady = "Ready";
    private const string StatusSkipped = "Skipped";
    private const string StatusBlocked = "Blocked";
    private const string ActionInsert = "Insert";
    private const string ActionUpdate = "Update";

    private sealed class RegisterMap
    {
        public int PlmDataSourceFrom { get; set; }
        public int DataSourceRegisterId { get; set; }
        public string DatabaseName { get; set; }
        public bool IsRegisterResolved { get; set; }
    }

    private sealed class ColumnRow
    {
        public int PlmEntityId { get; set; }
        public int UserDefineEntityColumnId { get; set; }
        public string SystemTableColumnName { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool UsedByDropDownList { get; set; }
        public int ColOrdinal { get; set; }
        public int? DisplayOrdinal { get; set; }
    }

    private sealed class EntityRow
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
        public string ImportStatus { get; set; } = StatusReady;
        public string ImportAction { get; set; }
        public string SkipReason { get; set; }
        public bool PhysicalTableOk { get; set; }
    }

    public static PlmSystemDefineEntityPreviewDto Build(
        string plmConnectionString,
        string tenantConnectionString,
        string tablePrefix,
        string dataSourceMapsJson)
    {
        var preview = new PlmSystemDefineEntityPreviewDto();
        if (string.IsNullOrWhiteSpace(plmConnectionString))
        {
            preview.IsSuccess = false;
            preview.ErrorMessage = "plmConnectionString is required.";
            return preview;
        }
        if (string.IsNullOrWhiteSpace(tenantConnectionString))
        {
            preview.IsSuccess = false;
            preview.ErrorMessage = "tenantConnectionString is required.";
            return preview;
        }

        try
        {
            var registerMaps = ParseDataSourceMaps(dataSourceMapsJson);
            preview.DataSourceMaps = registerMaps;

            string registerError = ValidateRegisterMaps(registerMaps);
            if (registerError != null)
            {
                preview.IsSuccess = false;
                preview.ErrorMessage = registerError;
                return preview;
            }

            var staging = BuildStaging(plmConnectionString.Trim(), registerMaps, tablePrefix);
            ValidatePhysicalTables(staging, registerMaps, tenantConnectionString.Trim());
            ApplyEntityCodeBlockers(staging, tenantConnectionString.Trim());
            AssignImportActions(staging, tenantConnectionString.Trim());

            preview.Entities = staging.Select(MapEntityPreviewItem).ToList();
            preview.ReadyCount = staging.Count(e => e.ImportStatus == StatusReady);
            preview.SkippedCount = staging.Count(e => e.ImportStatus == StatusSkipped);
            preview.BlockerCount = staging.Count(e => e.ImportStatus == StatusBlocked);
            preview.Blockers = staging
                .Where(e => e.ImportStatus == StatusBlocked)
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

    private static List<PlmSystemDefineDataSourceMapDto> ParseDataSourceMaps(string dataSourceMapsJson)
    {
        if (string.IsNullOrWhiteSpace(dataSourceMapsJson))
            throw new InvalidOperationException("dataSourceMapsJson is required.");

        var maps = JsonConvert.DeserializeObject<List<PlmSystemDefineDataSourceMapDto>>(dataSourceMapsJson);
        if (maps == null || maps.Count == 0)
            throw new InvalidOperationException("dataSourceMapsJson is empty or invalid.");

        return maps;
    }

    private static string ValidateRegisterMaps(List<PlmSystemDefineDataSourceMapDto> maps)
    {
        var plmMap = maps.FirstOrDefault(m => m.PlmDataSourceFrom == 1);
        if (plmMap == null || !plmMap.IsRegisterResolved || string.IsNullOrWhiteSpace(plmMap.DatabaseName))
            return "Company Master database register could not be resolved for PLM (DataSourceFrom = 1).";

        return null;
    }

    private static List<EntityRow> BuildStaging(
        string plmConnectionString,
        List<PlmSystemDefineDataSourceMapDto> registerMaps,
        string tablePrefix)
    {
        var registerByPlmFrom = registerMaps.ToDictionary(m => m.PlmDataSourceFrom, m => new RegisterMap
        {
            PlmDataSourceFrom = m.PlmDataSourceFrom,
            DataSourceRegisterId = m.DataSourceRegisterId,
            DatabaseName = m.DatabaseName,
            IsRegisterResolved = m.IsRegisterResolved
        });

        var entities = new List<EntityRow>();
        var columns = new List<ColumnRow>();

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

                        entities.Add(new EntityRow
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

                        columns.Add(new ColumnRow
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

        foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
        {
            if (!columnsByEntity.TryGetValue(entity.PlmEntityId, out var entityCols))
                entityCols = new List<ColumnRow>();

            AssignDisplayOrdinals(entityCols);
            ApplyColumnValidation(entity, entityCols);
        }

        return entities;
    }

    private static void MarkUnsupportedDataSources(List<EntityRow> entities)
    {
        foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
        {
            if (!entity.AppDataSourceFrom.HasValue || entity.AppDataSourceFrom.Value <= 0)
            {
                entity.ImportStatus = StatusSkipped;
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

    private static void MarkEmptyTableNames(List<EntityRow> entities)
    {
        foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
        {
            if (string.IsNullOrWhiteSpace(entity.TableName))
            {
                entity.ImportStatus = StatusSkipped;
                entity.SkipReason = "SysTableName is empty";
            }
        }
    }

    private static void ApplySystemDefineTablePrefix(List<EntityRow> entities, string tablePrefix)
    {
        foreach (var entity in entities.Where(e =>
                     e.ImportStatus == StatusReady && e.PlmDataSourceFrom == 1))
        {
            entity.TableName = ResolveTargetTableName(entity.TableName, tablePrefix);
        }
    }

    private static void ResolveTargetDatabaseNames(
        List<EntityRow> entities,
        Dictionary<int, RegisterMap> registerByPlmFrom)
    {
        foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
        {
            if (entity.PlmDataSourceFrom is int ds && registerByPlmFrom.TryGetValue(ds, out var reg))
                entity.TargetDatabaseName = reg.DatabaseName;
        }
    }

    private static void ApplyDuplicateEntityCodePrefix(List<EntityRow> entities)
    {
        var ready = entities.Where(e => e.ImportStatus == StatusReady).ToList();
        var groups = ready
            .GroupBy(e => e.TargetEntityCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            foreach (var entity in group.OrderBy(e => e.PlmEntityId).Skip(1))
                entity.TargetEntityCode = Truncate("Plm_" + entity.TargetEntityCode, 100);
        }
    }

    private static List<ColumnRow> GetEligibleDisplayColumns(List<ColumnRow> entityCols)
    {
        return entityCols
            .Where(c => c.UsedByDropDownList && !c.IsPrimaryKey)
            .OrderBy(c => c.ColOrdinal)
            .ToList();
    }

    private static void AssignDisplayOrdinals(List<ColumnRow> entityCols)
    {
        int disp = 0;
        foreach (var col in GetEligibleDisplayColumns(entityCols))
        {
            disp++;
            if (disp <= 3)
                col.DisplayOrdinal = disp;
        }
    }

    private static void ApplyColumnValidation(EntityRow entity, List<ColumnRow> entityCols)
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
            entity.ImportStatus = StatusSkipped;
            entity.SkipReason = entity.PkColumnCount != 1
                ? "Need exactly one PK column (IsPrimaryKey)"
                : entity.DisplayColumnCount < 1
                    ? "Need at least one UsedByDropDownList column"
                    : "PK SystemTableColumnName is empty";
        }
    }

    private static void ValidatePhysicalTables(
        List<EntityRow> entities,
        List<PlmSystemDefineDataSourceMapDto> registerMaps,
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
                             e.ImportStatus == StatusReady
                             && e.PlmDataSourceFrom == map.PlmDataSourceFrom))
                {
                    if (TableExistsInDatabase(conn, dbName, entity.SchemaOwner, entity.TableName))
                        entity.PhysicalTableOk = true;
                }
            }
        }

        foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady && !e.PhysicalTableOk))
        {
            if (entity.PlmDataSourceFrom == 1)
            {
                entity.SkipReason = "Physical table will be created by table export";
                continue;
            }

            entity.ImportStatus = StatusSkipped;
            entity.SkipReason = "Physical table not found in datasource database";
        }
    }

    private static void ApplyEntityCodeBlockers(List<EntityRow> entities, string tenantConnectionString)
    {
        using (var conn = new SqlConnection(tenantConnectionString))
        {
            conn.Open();
            foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
            {
                if (HasEntityCodeConflict(conn, entity.TargetEntityCode, entity.PlmEntityId))
                {
                    entity.ImportStatus = StatusBlocked;
                    entity.SkipReason = "EntityCode already exists in AppEntityInfo";
                }
            }
        }
    }

    private static void AssignImportActions(List<EntityRow> entities, string tenantConnectionString)
    {
        using (var conn = new SqlConnection(tenantConnectionString))
        {
            conn.Open();
            foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
            {
                entity.ImportAction = IntegrationIdExists(conn, entity.PlmEntityId)
                    ? ActionUpdate
                    : ActionInsert;
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

    private static bool TableExistsInDatabase(SqlConnection conn, string databaseName, string schema, string table)
    {
        using (var cmd = conn.CreateCommand())
        {
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

    private static string SanitizeTablePrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultTablePrefix;

        var sb = new StringBuilder();
        foreach (char ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
        }

        string result = sb.ToString();
        if (result.Length == 0)
            return DefaultTablePrefix;

        return result.Length <= 30 ? result : result.Substring(0, 30);
    }

    private static string ResolveTargetTableName(string sourceTableName, string tablePrefix)
    {
        if (string.IsNullOrWhiteSpace(sourceTableName))
            return null;

        string prefix = SanitizeTablePrefix(tablePrefix);
        string source = sourceTableName.Trim();
        string target = source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? source
            : prefix + source;
        return target.Length <= 100 ? target : target.Substring(0, 100);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength);
    }

    private static PlmSystemDefineEntityPreviewItemDto MapEntityPreviewItem(EntityRow entity)
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
}
