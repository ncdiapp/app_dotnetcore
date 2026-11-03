using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using App.BL;
using NLog;

namespace App.BL.TenantBusiness
{
    public sealed record AppGeneralAgentToolDto(string LibraryKey, string ToolName);

    public static class AppGeneralAgentToolBL
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static List<AppGeneralAgentToolDto> GetAll(int dataSourceId)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                return fixture == null ? new List<AppGeneralAgentToolDto>() : Load(fixture);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, nameof(GetAll));
                return new List<AppGeneralAgentToolDto>();
            }
        }

        /// <summary>
        /// Null when the table is missing (migration not applied). Empty set means nothing is exposed.
        /// </summary>
        public static HashSet<string> TryLoadKeySet(int dataSourceId)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return null;
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in Load(fixture))
                    set.Add(Key(row.LibraryKey, row.ToolName));
                return set;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "AppGeneralAgentTool not readable — General Agent keeps all tools until V068 is applied");
                return null;
            }
        }

        public static bool ReplaceAll(int dataSourceId, IEnumerable<AppGeneralAgentToolDto> items)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;
            fixture.ExecuteNonQueryResult("DELETE FROM dbo.AppGeneralAgentTool", new List<DbParameter>());
            foreach (var item in (items ?? Enumerable.Empty<AppGeneralAgentToolDto>())
                         .Where(i => !string.IsNullOrWhiteSpace(i?.LibraryKey) && !string.IsNullOrWhiteSpace(i?.ToolName))
                         .GroupBy(i => (i.LibraryKey.Trim().ToLowerInvariant(), i.ToolName.Trim().ToLowerInvariant()))
                         .Select(g => g.First()))
            {
                fixture.ExecuteNonQueryResult(
                    "INSERT INTO dbo.AppGeneralAgentTool (LibraryKey, ToolName) VALUES (@LibraryKey, @ToolName)",
                    new List<DbParameter>
                    {
                        P(fixture, "@LibraryKey", item.LibraryKey.Trim()),
                        P(fixture, "@ToolName", item.ToolName.Trim())
                    });
            }
            return true;
        }

        public static string Key(string libraryKey, string toolName) =>
            (libraryKey ?? "").Trim() + "\u0001" + (toolName ?? "").Trim();

        private static List<AppGeneralAgentToolDto> Load(DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(
                "SELECT LibraryKey, ToolName FROM dbo.AppGeneralAgentTool ORDER BY LibraryKey, ToolName",
                new List<DbParameter>());
            var result = new List<AppGeneralAgentToolDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new AppGeneralAgentToolDto(
                    row["LibraryKey"] as string ?? "",
                    row["ToolName"] as string ?? ""));
            return result;
        }

        private static DbParameter P(DatabaseSchemaMrg.DatabaseFixture f, string name, object value)
        {
            var p = f.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
