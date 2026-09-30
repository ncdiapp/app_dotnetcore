using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using App.BL;
using DatabaseSchemaMrg;
using NLog;

namespace App.BL.AIAgent.GenericAgent
{
    public sealed class AppAgentChildMappingDto
    {
        public string ParentSkillKey { get; set; } = "";
        public string ChildSkillKey  { get; set; } = "";
        public int    SortOrder      { get; set; }
        public DateTime? CreatedAt   { get; set; }
        public DateTime? UpdatedAt   { get; set; }

        /// <summary>Optional display enrichment (not a table column).</summary>
        public string ChildDisplayName { get; set; }

        /// <summary>Optional display enrichment (not a table column).</summary>
        public string ChildExecutionMode { get; set; }

        /// <summary>Optional display enrichment (not a table column).</summary>
        public bool? ChildIsActive { get; set; }
    }

    public static class AppAgentChildMappingBL
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static bool TableExists(int dataSourceId)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                return TableExists(fixture);
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed); return false; }
        }

        public static bool TableExists(DatabaseFixture fixture)
        {
            if (fixture == null) return false;
            try
            {
                var dt = fixture.RetriveDataTable(
                    "SELECT 1 AS x FROM sys.tables WHERE name = N'AppAgentChildMapping' AND type = N'U'",
                    new List<DbParameter>());
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed); return false; }
        }

        public static List<AppAgentChildMappingDto> GetAll(int dataSourceId)
        {
            var list = new List<AppAgentChildMappingDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || !TableExists(fixture)) return list;
                var dt = fixture.RetriveDataTable(@"
SELECT m.ParentSkillKey, m.ChildSkillKey, m.SortOrder, m.CreatedAt, m.UpdatedAt,
       c.DisplayName AS ChildDisplayName, c.ExecutionMode AS ChildExecutionMode, c.IsActive AS ChildIsActive
FROM dbo.AppAgentChildMapping m
LEFT JOIN dbo.AppAgentSkillSet c ON c.SkillKey = m.ChildSkillKey
ORDER BY m.ParentSkillKey, m.SortOrder, m.ChildSkillKey",
                    new List<DbParameter>());
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
            }
            catch (Exception ex) { Log.Error(ex, nameof(GetAll)); }
            return list;
        }

        public static List<AppAgentChildMappingDto> GetChildren(int dataSourceId, string parentSkillKey)
        {
            var list = new List<AppAgentChildMappingDto>();
            if (string.IsNullOrWhiteSpace(parentSkillKey)) return list;
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || !TableExists(fixture)) return list;
                var dt = fixture.RetriveDataTable(@"
SELECT m.ParentSkillKey, m.ChildSkillKey, m.SortOrder, m.CreatedAt, m.UpdatedAt,
       c.DisplayName AS ChildDisplayName, c.ExecutionMode AS ChildExecutionMode, c.IsActive AS ChildIsActive
FROM dbo.AppAgentChildMapping m
LEFT JOIN dbo.AppAgentSkillSet c ON c.SkillKey = m.ChildSkillKey
WHERE m.ParentSkillKey = @ParentSkillKey
ORDER BY m.SortOrder, m.ChildSkillKey",
                    new List<DbParameter> { P(fixture, "@ParentSkillKey", parentSkillKey.Trim()) });
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
            }
            catch (Exception ex) { Log.Error(ex, nameof(GetChildren)); }
            return list;
        }

        public static List<string> GetUsedByParents(int dataSourceId, string childSkillKey)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(childSkillKey)) return list;
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || !TableExists(fixture)) return list;
                var dt = fixture.RetriveDataTable(@"
SELECT ParentSkillKey
FROM dbo.AppAgentChildMapping
WHERE ChildSkillKey = @ChildSkillKey
ORDER BY ParentSkillKey",
                    new List<DbParameter> { P(fixture, "@ChildSkillKey", childSkillKey.Trim()) });
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                {
                    var k = row["ParentSkillKey"] as string;
                    if (!string.IsNullOrWhiteSpace(k)) list.Add(k);
                }
            }
            catch (Exception ex) { Log.Error(ex, nameof(GetUsedByParents)); }
            return list;
        }

        public static bool IsRegisteredChild(int dataSourceId, string parentSkillKey, string childSkillKey)
        {
            if (string.IsNullOrWhiteSpace(parentSkillKey) || string.IsNullOrWhiteSpace(childSkillKey))
                return false;
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || !TableExists(fixture)) return false;
                var dt = fixture.RetriveDataTable(@"
SELECT 1 AS x FROM dbo.AppAgentChildMapping
WHERE ParentSkillKey = @ParentSkillKey AND ChildSkillKey = @ChildSkillKey",
                    new List<DbParameter>
                    {
                        P(fixture, "@ParentSkillKey", parentSkillKey.Trim()),
                        P(fixture, "@ChildSkillKey", childSkillKey.Trim()),
                    });
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception ex) { Log.Error(ex, nameof(IsRegisteredChild)); return false; }
        }

        /// <summary>
        /// Replace the full ordered Child-Agent list for an Orchestrator.
        /// </summary>
        public static bool SetChildren(int dataSourceId, string parentSkillKey, IList<AppAgentChildMappingDto> children, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(parentSkillKey))
            {
                error = "ParentSkillKey is required.";
                return false;
            }
            parentSkillKey = parentSkillKey.Trim();
            children ??= new List<AppAgentChildMappingDto>();

            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null)
                {
                    error = "Database fixture not available.";
                    return false;
                }
                if (!TableExists(fixture))
                {
                    error = "AppAgentChildMapping table is missing. Run migration V036.";
                    return false;
                }

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var ordered = new List<string>();
                // Preserve request list order (UI drag-sort). SortOrder values are rewritten 1..n.
                foreach (var c in children)
                {
                    var childKey = (c?.ChildSkillKey ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(childKey)) continue;
                    if (string.Equals(childKey, parentSkillKey, StringComparison.OrdinalIgnoreCase))
                    {
                        error = "An agent cannot be a Child-Agent of itself.";
                        return false;
                    }
                    if (!seen.Add(childKey)) continue;
                    ordered.Add(childKey);
                }

                foreach (var child in ordered)
                {
                    if (!SkillExists(fixture, child))
                    {
                        error = $"Child-Agent '{child}' does not exist.";
                        return false;
                    }
                }

                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentChildMapping WHERE ParentSkillKey = @ParentSkillKey",
                    new List<DbParameter> { P(fixture, "@ParentSkillKey", parentSkillKey) });

                for (int i = 0; i < ordered.Count; i++)
                {
                    fixture.ExecuteNonQueryResult(@"
INSERT INTO dbo.AppAgentChildMapping (ParentSkillKey, ChildSkillKey, SortOrder, CreatedAt, UpdatedAt)
VALUES (@ParentSkillKey, @ChildSkillKey, @SortOrder, SYSUTCDATETIME(), SYSUTCDATETIME())",
                        new List<DbParameter>
                        {
                            P(fixture, "@ParentSkillKey", parentSkillKey),
                            P(fixture, "@ChildSkillKey", ordered[i]),
                            P(fixture, "@SortOrder", i + 1),
                        });
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(SetChildren));
                error = ex.Message;
                return false;
            }
        }

        public static bool AddChildren(int dataSourceId, string parentSkillKey, IList<string> childSkillKeys, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(parentSkillKey))
            {
                error = "ParentSkillKey is required.";
                return false;
            }
            parentSkillKey = parentSkillKey.Trim();
            var existing = GetChildren(dataSourceId, parentSkillKey);
            var next = existing.Select(x => new AppAgentChildMappingDto
            {
                ParentSkillKey = parentSkillKey,
                ChildSkillKey = x.ChildSkillKey,
                SortOrder = x.SortOrder
            }).ToList();

            int maxSort = next.Count == 0 ? 0 : next.Max(x => x.SortOrder);
            var have = new HashSet<string>(next.Select(x => x.ChildSkillKey), StringComparer.OrdinalIgnoreCase);
            foreach (var raw in childSkillKeys ?? Array.Empty<string>())
            {
                var key = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key) || !have.Add(key)) continue;
                maxSort++;
                next.Add(new AppAgentChildMappingDto
                {
                    ParentSkillKey = parentSkillKey,
                    ChildSkillKey = key,
                    SortOrder = maxSort
                });
            }
            return SetChildren(dataSourceId, parentSkillKey, next, out error);
        }

        public static bool RemoveChild(int dataSourceId, string parentSkillKey, string childSkillKey, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(parentSkillKey) || string.IsNullOrWhiteSpace(childSkillKey))
            {
                error = "ParentSkillKey and ChildSkillKey are required.";
                return false;
            }
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || !TableExists(fixture))
                {
                    error = "AppAgentChildMapping table is missing.";
                    return false;
                }
                fixture.ExecuteNonQueryResult(@"
DELETE FROM dbo.AppAgentChildMapping
WHERE ParentSkillKey = @ParentSkillKey AND ChildSkillKey = @ChildSkillKey",
                    new List<DbParameter>
                    {
                        P(fixture, "@ParentSkillKey", parentSkillKey.Trim()),
                        P(fixture, "@ChildSkillKey", childSkillKey.Trim()),
                    });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(RemoveChild));
                error = ex.Message;
                return false;
            }
        }

        public static void DeleteLinksForParent(int dataSourceId, string parentSkillKey)
        {
            if (string.IsNullOrWhiteSpace(parentSkillKey)) return;
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null || !TableExists(fixture)) return;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentChildMapping WHERE ParentSkillKey = @ParentSkillKey",
                    new List<DbParameter> { P(fixture, "@ParentSkillKey", parentSkillKey.Trim()) });
            }
            catch (Exception ex) { Log.Error(ex, nameof(DeleteLinksForParent)); }
        }

        private static bool SkillExists(DatabaseFixture fixture, string skillKey)
        {
            var dt = fixture.RetriveDataTable(
                "SELECT 1 AS x FROM dbo.AppAgentSkillSet WHERE SkillKey = @SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey) });
            return dt != null && dt.Rows.Count > 0;
        }

        private static AppAgentChildMappingDto MapRow(DataRow row) => new AppAgentChildMappingDto
        {
            ParentSkillKey = row["ParentSkillKey"] as string ?? "",
            ChildSkillKey = row["ChildSkillKey"] as string ?? "",
            SortOrder = row["SortOrder"] == DBNull.Value ? 0 : Convert.ToInt32(row["SortOrder"]),
            CreatedAt = row.Table.Columns.Contains("CreatedAt") && row["CreatedAt"] != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(row["CreatedAt"]) : null,
            UpdatedAt = row.Table.Columns.Contains("UpdatedAt") && row["UpdatedAt"] != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(row["UpdatedAt"]) : null,
            ChildDisplayName = row.Table.Columns.Contains("ChildDisplayName")
                ? row["ChildDisplayName"] as string : null,
            ChildExecutionMode = row.Table.Columns.Contains("ChildExecutionMode")
                ? row["ChildExecutionMode"] as string : null,
            ChildIsActive = row.Table.Columns.Contains("ChildIsActive") && row["ChildIsActive"] != DBNull.Value
                ? (bool?)Convert.ToBoolean(row["ChildIsActive"]) : null,
        };

        private static DbParameter P(DatabaseFixture fixture, string name, object value)
        {
            var p = fixture.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
