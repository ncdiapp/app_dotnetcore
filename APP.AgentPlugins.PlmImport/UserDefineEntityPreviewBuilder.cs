using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Pure PLM-read + tenant validation for User Define entity import preview (EntityType = 4).
/// </summary>
public static class UserDefineEntityPreviewBuilder
{
    private const string DefaultEntityWideTablePrefix = "Plm_Entity_";
    private const string StatusReady = "Ready";
    private const string StatusSkipped = "Skipped";
    private const string StatusBlocked = "Blocked";
    private const string ActionInsert = "Insert";
    private const string ActionUpdate = "Update";
    private const string AppTargetSimpleList = "SimpleValueList";
    private const string AppTargetWideTable = "SystemDefineTable";

    private sealed class EntityRow
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
        public string ImportStatus { get; set; } = StatusReady;
        public string ImportAction { get; set; }
        public string SkipReason { get; set; }
        public string AppTargetType =>
            TargetEntityType == (int)EmAppEntityType.SimpleValueList ? AppTargetSimpleList : AppTargetWideTable;
    }

    private sealed class ColumnRow
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

    private sealed class StagingResult
    {
        public List<EntityRow> Entities { get; set; } = new List<EntityRow>();
        public List<ColumnRow> Columns { get; set; } = new List<ColumnRow>();
    }

    public static PlmUserDefineEntityPreviewDto Build(
        string plmConnectionString,
        string tenantConnectionString,
        string tenantDatabaseName,
        string entityWideTablePrefix)
    {
        var preview = new PlmUserDefineEntityPreviewDto();
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
        if (string.IsNullOrWhiteSpace(tenantDatabaseName))
        {
            preview.IsSuccess = false;
            preview.ErrorMessage = "tenantDatabaseName is required.";
            return preview;
        }

        try
        {
            var staging = BuildStaging(
                plmConnectionString.Trim(),
                tenantConnectionString.Trim(),
                SanitizeDatabaseName(tenantDatabaseName.Trim()),
                entityWideTablePrefix);

            preview.Entities = staging.Entities.Select(MapPreviewItem).ToList();
            preview.ReadyCount = staging.Entities.Count(e => e.ImportStatus == StatusReady);
            preview.SkippedCount = staging.Entities.Count(e => e.ImportStatus == StatusSkipped);
            preview.BlockerCount = staging.Entities.Count(e => e.ImportStatus == StatusBlocked);
            preview.Blockers = staging.Entities
                .Where(e => e.ImportStatus == StatusBlocked)
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

    private static StagingResult BuildStaging(
        string plmConnectionString,
        string tenantConnectionString,
        string tenantDbName,
        string entityWideTablePrefix)
    {
        var result = new StagingResult();
        string wideTablePrefix = SanitizeImportTablePrefix(entityWideTablePrefix, DefaultEntityWideTablePrefix);

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

                        result.Entities.Add(new EntityRow
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

                        result.Columns.Add(new ColumnRow
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
        ApplyColumnFlags(result.Entities, result.Columns);
        ApplyEntityCodeRules(result.Entities, tenantConnectionString);
        SyncTableNames(result.Entities);
        AssignImportOrder(result.Entities, result.Columns);
        ApplyValidation(result.Entities, result.Columns, tenantConnectionString, tenantDbName, wideTablePrefix);

        return result;
    }

    private static void DedupeColumnNamesPerEntity(List<ColumnRow> columns)
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

    private static void ApplyColumnFlags(List<EntityRow> entities, List<ColumnRow> columns)
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

    private static void ApplyEntityCodeRules(List<EntityRow> entities, string tenantConnectionString)
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
                entity.TargetEntityCode = Truncate($"{entity.TargetEntityCode}_{entity.PlmEntityId}", 100);
        }
    }

    private static void SyncTableNames(List<EntityRow> entities)
    {
        foreach (var group in entities
                     .Where(e => !string.IsNullOrWhiteSpace(e.TargetTableName))
                     .GroupBy(e => e.TargetTableName, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            foreach (var entity in group)
                entity.TargetTableName = Truncate($"{entity.TargetTableName}_{entity.PlmEntityId}", 100);
        }
    }

    private static void AssignImportOrder(List<EntityRow> entities, List<ColumnRow> columns)
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

    private static void ApplyValidation(
        List<EntityRow> entities,
        List<ColumnRow> columns,
        string tenantConnectionString,
        string tenantDbName,
        string entityWideTablePrefix)
    {
        foreach (var entity in entities.Where(e => e.ColumnCount == 0))
        {
            entity.ImportStatus = StatusSkipped;
            entity.SkipReason = "No column definitions in pdmUserDefineEntityColumn";
        }

        using (var conn = new SqlConnection(tenantConnectionString))
        {
            conn.Open();
            foreach (var entity in entities.Where(e => e.ImportStatus == StatusReady))
            {
                if (HasEntityCodeConflict(conn, entity.TargetEntityCode, entity.PlmEntityId))
                {
                    entity.ImportStatus = StatusBlocked;
                    entity.SkipReason = "EntityCode already exists in AppEntityInfo";
                    continue;
                }

                entity.ImportAction = IntegrationIdExists(conn, entity.PlmEntityId)
                    ? ActionUpdate
                    : ActionInsert;

                if (entity.ImportAction == ActionInsert
                    && entity.TargetEntityType == (int)EmAppEntityType.SystemDefineTable
                    && !string.IsNullOrWhiteSpace(entity.TargetTableName)
                    && TableExistsInDatabase(conn, tenantDbName, "dbo", entity.TargetTableName))
                {
                    entity.ImportStatus = StatusBlocked;
                    entity.SkipReason = $"Physical table {entityWideTablePrefix}* already exists";
                }
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

    private static bool EntityCodeExistsInApp(SqlConnection conn, string entityCode)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT TOP 1 1 FROM dbo.AppEntityInfo WHERE EntityCode = @EntityCode";
            cmd.Parameters.AddWithValue("@EntityCode", (object)entityCode ?? DBNull.Value);
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

    private static string SanitizeImportTablePrefix(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(fallback))
            fallback = DefaultEntityWideTablePrefix;

        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var sb = new StringBuilder();
        foreach (char ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
        }

        string result = sb.ToString();
        if (result.Length == 0)
            return fallback;

        return result.Length <= 30 ? result : result.Substring(0, 30);
    }

    private static string SanitizeDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("tenantDatabaseName is required.");

        string name = databaseName.Trim();
        if (name.IndexOfAny(new[] { '[', ']', ';', '\'', '"', ' ' }) >= 0)
            throw new ArgumentException($"Invalid database name: {databaseName}");

        return name;
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

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength);
    }

    private static PlmUserDefineEntityPreviewItemDto MapPreviewItem(EntityRow entity)
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
}
