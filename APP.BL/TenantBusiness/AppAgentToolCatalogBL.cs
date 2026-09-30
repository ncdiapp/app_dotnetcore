using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using App.BL;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentToolCatalogDto(
        string Source,
        string LibraryKey,
        string ToolName,
        string Description,
        string InputSummary,
        string Risk,
        int?   McpServerId = null);

    // Unified tool catalog (table AppAgentToolCatalog, V039) read by "AI Generate Agent Design".
    public static class AppAgentToolCatalogBL
    {
        public static List<AppAgentToolCatalogDto> GetAll(int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentToolCatalogDto>();
            return MapAll(fixture.RetriveDataTable(SelectSql + " ORDER BY LibraryKey, ToolName", new List<DbParameter>()));
        }

        public static List<AppAgentToolCatalogDto> GetByLibraries(int dataSourceId, IEnumerable<string> libraryKeys)
        {
            var keys = new HashSet<string>((libraryKeys ?? Enumerable.Empty<string>()).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()),
                StringComparer.OrdinalIgnoreCase);
            return GetAll(dataSourceId).Where(t => keys.Contains(t.LibraryKey)).ToList();
        }

        // Active MCP servers that have no catalog rows yet (never synced) — the AI cannot pick their tools.
        public static List<string> GetUnsyncedMcpServers(int dataSourceId)
        {
            var result = new List<string>();
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return result;
            var dt = fixture.RetriveDataTable(@"
SELECT m.ServerName, m.SkillKey
FROM dbo.AppAgentMcpServer m
WHERE m.IsActive=1 AND m.ServerType='streamable-http'
  AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolCatalog c WHERE c.McpServerId=m.McpServerId)", new List<DbParameter>());
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add($"{row["ServerName"]} (library {row["SkillKey"]})");
            return result;
        }

        // Brings the non-MCP catalog rows in line with AppAgentLibraryTool. Only changed rows are written.
        // Returns the number of rows inserted/updated/deleted.
        public static int SyncLibraryTools(int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return 0;

            var libDt = fixture.RetriveDataTable(
                @"SELECT LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType
                  FROM dbo.AppAgentLibraryTool WHERE IsActive=1", new List<DbParameter>());
            var wanted = new Dictionary<string, AppAgentToolCatalogDto>(StringComparer.OrdinalIgnoreCase);
            if (libDt != null)
                foreach (DataRow r in libDt.Rows)
                {
                    var lib  = r["LibraryKey"] as string ?? "";
                    var name = r["ToolName"] as string ?? "";
                    wanted[Key(lib, name)] = new AppAgentToolCatalogDto(
                        Source:       (r["ToolType"] as string ?? "tool").ToLowerInvariant(),
                        LibraryKey:   lib,
                        ToolName:     name,
                        Description:  r["ToolDescription"] as string ?? "",
                        InputSummary: ToolRiskGuesser.SummarizeSchema(r["ParameterSchemaJson"] as string),
                        Risk:         ToolRiskGuesser.Guess(name));
                }

            var existing = MapAll(fixture.RetriveDataTable(SelectSql + " WHERE Source<>'mcp'", new List<DbParameter>()))
                .ToDictionary(t => Key(t.LibraryKey, t.ToolName), t => t, StringComparer.OrdinalIgnoreCase);

            int changed = 0;
            foreach (var old in existing.Values.Where(e => !wanted.ContainsKey(Key(e.LibraryKey, e.ToolName))))
            {
                fixture.ExecuteNonQueryResult("DELETE FROM dbo.AppAgentToolCatalog WHERE LibraryKey=@L AND ToolName=@T AND Source<>'mcp'",
                    new List<DbParameter> { P(fixture, "@L", old.LibraryKey), P(fixture, "@T", old.ToolName) });
                changed++;
            }
            foreach (var w in wanted.Values)
            {
                if (existing.TryGetValue(Key(w.LibraryKey, w.ToolName), out var cur))
                {
                    if (cur.Source == w.Source && cur.Description == w.Description && cur.InputSummary == w.InputSummary && cur.Risk == w.Risk) continue;
                    fixture.ExecuteNonQueryResult(
                        @"UPDATE dbo.AppAgentToolCatalog SET Source=@Source, Description=@Description, InputSummary=@InputSummary,
                            Risk=@Risk, SyncedAt=GETUTCDATE() WHERE LibraryKey=@L AND ToolName=@T",
                        CatalogParams(fixture, w));
                }
                else
                {
                    fixture.ExecuteNonQueryResult(
                        @"INSERT INTO dbo.AppAgentToolCatalog (Source,LibraryKey,ToolName,Description,InputSummary,Risk)
                          VALUES (@Source,@L,@T,@Description,@InputSummary,@Risk)",
                        CatalogParams(fixture, w));
                }
                changed++;
            }
            return changed;
        }

        // Replaces the catalog rows of one MCP server with the tools it reports now.
        public static int ReplaceMcpServerTools(int dataSourceId, int mcpServerId, IEnumerable<AppAgentToolCatalogDto> tools)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return 0;

            fixture.ExecuteNonQueryResult("DELETE FROM dbo.AppAgentToolCatalog WHERE McpServerId=@Id",
                new List<DbParameter> { P(fixture, "@Id", mcpServerId) });

            int count = 0;
            foreach (var t in tools)
            {
                // A tool with the same name already catalogued in this library by another source/server: replace it.
                fixture.ExecuteNonQueryResult("DELETE FROM dbo.AppAgentToolCatalog WHERE LibraryKey=@L AND ToolName=@T",
                    new List<DbParameter> { P(fixture, "@L", t.LibraryKey), P(fixture, "@T", t.ToolName) });
                var ps = CatalogParams(fixture, t);
                ps.Add(P(fixture, "@McpServerId", mcpServerId));
                fixture.ExecuteNonQueryResult(
                    @"INSERT INTO dbo.AppAgentToolCatalog (Source,LibraryKey,ToolName,Description,InputSummary,Risk,McpServerId)
                      VALUES ('mcp',@L,@T,@Description,@InputSummary,@Risk,@McpServerId)", ps);
                count++;
            }
            return count;
        }

        private const string SelectSql =
            "SELECT Source, LibraryKey, ToolName, Description, InputSummary, Risk, McpServerId FROM dbo.AppAgentToolCatalog";

        private static string Key(string lib, string tool) => lib + "\u0001" + tool;

        private static List<AppAgentToolCatalogDto> MapAll(DataTable dt)
        {
            var result = new List<AppAgentToolCatalogDto>();
            if (dt == null) return result;
            foreach (DataRow r in dt.Rows)
                result.Add(new AppAgentToolCatalogDto(
                    Source:       r["Source"] as string ?? "",
                    LibraryKey:   r["LibraryKey"] as string ?? "",
                    ToolName:     r["ToolName"] as string ?? "",
                    Description:  r["Description"] as string ?? "",
                    InputSummary: r["InputSummary"] as string ?? "",
                    Risk:         r["Risk"] as string ?? "read",
                    McpServerId:  r["McpServerId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["McpServerId"])));
            return result;
        }

        private static List<DbParameter> CatalogParams(DatabaseSchemaMrg.DatabaseFixture f, AppAgentToolCatalogDto d) =>
            new List<DbParameter>
            {
                P(f, "@Source",       d.Source ?? ""),
                P(f, "@L",            d.LibraryKey),
                P(f, "@T",            d.ToolName),
                P(f, "@Description",  d.Description ?? ""),
                P(f, "@InputSummary", d.InputSummary ?? ""),
                P(f, "@Risk",         string.IsNullOrEmpty(d.Risk) ? "read" : d.Risk)
            };

        private static DbParameter P(DatabaseSchemaMrg.DatabaseFixture f, string name, object value)
        {
            var p = f.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
