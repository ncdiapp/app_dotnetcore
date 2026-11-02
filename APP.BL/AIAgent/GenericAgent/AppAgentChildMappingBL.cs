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
        public int    ParentAgentId  { get; set; }
        public int    ChildAgentId   { get; set; }
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
SELECT m.ParentAgentId, m.ChildAgentId, m.SortOrder, m.CreatedAt, m.UpdatedAt,
       p.AgentCode AS ParentSkillKey, c.AgentCode AS ChildSkillKey,
       c.DisplayName AS ChildDisplayName, c.ExecutionMode AS ChildExecutionMode, c.IsActive AS ChildIsActive
FROM dbo.AppAgentChildMapping m
LEFT JOIN dbo.AppAgentSkillSet p ON p.AgentID = m.ParentAgentId
LEFT JOIN dbo.AppAgentSkillSet c ON c.AgentID = m.ChildAgentId
ORDER BY p.AgentCode, m.SortOrder, c.AgentCode",
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
                var parentId = AppAgentLookup.IdByCode(fixture, parentSkillKey);
                if (parentId == null) return list;
                var dt = fixture.RetriveDataTable(@"
SELECT m.ParentAgentId, m.ChildAgentId, m.SortOrder, m.CreatedAt, m.UpdatedAt,
       p.AgentCode AS ParentSkillKey, c.AgentCode AS ChildSkillKey,
       c.DisplayName AS ChildDisplayName, c.ExecutionMode AS ChildExecutionMode, c.IsActive AS ChildIsActive
FROM dbo.AppAgentChildMapping m
LEFT JOIN dbo.AppAgentSkillSet p ON p.AgentID = m.ParentAgentId
LEFT JOIN dbo.AppAgentSkillSet c ON c.AgentID = m.ChildAgentId
WHERE m.ParentAgentId = @ParentAgentId
ORDER BY m.SortOrder, c.AgentCode",
                    new List<DbParameter> { P(fixture, "@ParentAgentId", parentId.Value) });
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
                var childId = AppAgentLookup.IdByCode(fixture, childSkillKey);
                if (childId == null) return list;
                var dt = fixture.RetriveDataTable(@"
SELECT p.AgentCode AS ParentSkillKey
FROM dbo.AppAgentChildMapping m
INNER JOIN dbo.AppAgentSkillSet p ON p.AgentID = m.ParentAgentId
WHERE m.ChildAgentId = @ChildAgentId
ORDER BY p.AgentCode",
                    new List<DbParameter> { P(fixture, "@ChildAgentId", childId.Value) });
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
                var parentId = AppAgentLookup.IdByCode(fixture, parentSkillKey);
                var childId = AppAgentLookup.IdByCode(fixture, childSkillKey);
                if (parentId == null || childId == null) return false;
                var dt = fixture.RetriveDataTable(@"
SELECT 1 AS x FROM dbo.AppAgentChildMapping
WHERE ParentAgentId = @ParentAgentId AND ChildAgentId = @ChildAgentId",
                    new List<DbParameter>
                    {
                        P(fixture, "@ParentAgentId", parentId.Value),
                        P(fixture, "@ChildAgentId", childId.Value),
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

                var parentId = AppAgentLookup.IdByCode(fixture, parentSkillKey);
                if (parentId == null)
                {
                    error = $"Orchestrator '{parentSkillKey}' does not exist.";
                    return false;
                }

                var childIds = new List<int>();
                foreach (var child in ordered)
                {
                    var id = AppAgentLookup.IdByCode(fixture, child);
                    if (id == null)
                    {
                        error = $"Child-Agent '{child}' does not exist.";
                        return false;
                    }
                    childIds.Add(id.Value);
                }

                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentChildMapping WHERE ParentAgentId = @ParentAgentId",
                    new List<DbParameter> { P(fixture, "@ParentAgentId", parentId.Value) });

                for (int i = 0; i < childIds.Count; i++)
                {
                    fixture.ExecuteNonQueryResult(@"
INSERT INTO dbo.AppAgentChildMapping (ParentAgentId, ChildAgentId, SortOrder, CreatedAt, UpdatedAt)
VALUES (@ParentAgentId, @ChildAgentId, @SortOrder, SYSUTCDATETIME(), SYSUTCDATETIME())",
                        new List<DbParameter>
                        {
                            P(fixture, "@ParentAgentId", parentId.Value),
                            P(fixture, "@ChildAgentId", childIds[i]),
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
                var parentId = AppAgentLookup.IdByCode(fixture, parentSkillKey);
                var childId = AppAgentLookup.IdByCode(fixture, childSkillKey);
                if (parentId == null || childId == null) return true;
                fixture.ExecuteNonQueryResult(@"
DELETE FROM dbo.AppAgentChildMapping
WHERE ParentAgentId = @ParentAgentId AND ChildAgentId = @ChildAgentId",
                    new List<DbParameter>
                    {
                        P(fixture, "@ParentAgentId", parentId.Value),
                        P(fixture, "@ChildAgentId", childId.Value),
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
                var parentId = AppAgentLookup.IdByCode(fixture, parentSkillKey);
                if (parentId == null) return;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentChildMapping WHERE ParentAgentId = @ParentAgentId",
                    new List<DbParameter> { P(fixture, "@ParentAgentId", parentId.Value) });
            }
            catch (Exception ex) { Log.Error(ex, nameof(DeleteLinksForParent)); }
        }

        private static AppAgentChildMappingDto MapRow(DataRow row) => new AppAgentChildMappingDto
        {
            ParentAgentId = row.Table.Columns.Contains("ParentAgentId") && row["ParentAgentId"] != DBNull.Value
                ? Convert.ToInt32(row["ParentAgentId"]) : 0,
            ChildAgentId = row.Table.Columns.Contains("ChildAgentId") && row["ChildAgentId"] != DBNull.Value
                ? Convert.ToInt32(row["ChildAgentId"]) : 0,
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
