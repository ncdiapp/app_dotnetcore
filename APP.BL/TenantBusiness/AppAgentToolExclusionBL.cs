using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using App.BL;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentToolExclusionDto(string LibraryKey, string ToolName);

    // Per-agent "do not use" list for tools inside subscribed libraries (table AppAgentToolExclusion, V038).
    public static class AppAgentToolExclusionBL
    {
        public static List<AppAgentToolExclusionDto> GetBySkillKey(string skillKey)
        {
            var fixture = GetFixture();
            return fixture == null ? new List<AppAgentToolExclusionDto>() : Load(skillKey, fixture);
        }

        public static List<AppAgentToolExclusionDto> GetBySkillKey(string skillKey, int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            return fixture == null ? new List<AppAgentToolExclusionDto>() : Load(skillKey, fixture);
        }

        // Used at run time: a failure (e.g. V038 not applied yet) must not stop the agent, so it is logged
        // and treated as "nothing excluded".
        public static List<AppAgentToolExclusionDto> GetBySkillKey(string skillKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            try { return Load(skillKey, fixture); }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Could not read AppAgentToolExclusion for {0}", skillKey);
                return new List<AppAgentToolExclusionDto>();
            }
        }

        public static HashSet<string> ExcludedNamesForLibrary(IEnumerable<AppAgentToolExclusionDto> all, string libraryKey) =>
            new HashSet<string>(
                all.Where(e => string.Equals(e.LibraryKey, libraryKey, StringComparison.OrdinalIgnoreCase)).Select(e => e.ToolName),
                StringComparer.OrdinalIgnoreCase);

        // Replaces the agent's whole exclusion list.
        public static bool ReplaceForSkill(string skillKey, IEnumerable<AppAgentToolExclusionDto> items, int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            return fixture != null && Replace(skillKey, items, fixture);
        }

        private static bool Replace(string skillKey, IEnumerable<AppAgentToolExclusionDto> items, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return false;

            skillKey = skillKey.Trim();
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentToolExclusion WHERE SkillKey=@SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey) });

            foreach (var item in (items ?? Enumerable.Empty<AppAgentToolExclusionDto>())
                         .Where(i => !string.IsNullOrWhiteSpace(i?.LibraryKey) && !string.IsNullOrWhiteSpace(i?.ToolName))
                         .GroupBy(i => (i.LibraryKey.Trim().ToLowerInvariant(), i.ToolName.Trim().ToLowerInvariant()))
                         .Select(g => g.First()))
            {
                fixture.ExecuteNonQueryResult(
                    "INSERT INTO dbo.AppAgentToolExclusion (SkillKey,LibraryKey,ToolName) VALUES (@SkillKey,@LibraryKey,@ToolName)",
                    new List<DbParameter>
                    {
                        P(fixture, "@SkillKey", skillKey),
                        P(fixture, "@LibraryKey", item.LibraryKey.Trim()),
                        P(fixture, "@ToolName", item.ToolName.Trim())
                    });
            }
            return true;
        }

        private static List<AppAgentToolExclusionDto> Load(string skillKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var result = new List<AppAgentToolExclusionDto>();
            if (string.IsNullOrWhiteSpace(skillKey)) return result;

            var dt = fixture.RetriveDataTable(
                "SELECT LibraryKey,ToolName FROM dbo.AppAgentToolExclusion WHERE SkillKey=@SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey.Trim()) });
            if (dt == null) return result;
            foreach (System.Data.DataRow row in dt.Rows)
                result.Add(new AppAgentToolExclusionDto(row["LibraryKey"] as string ?? "", row["ToolName"] as string ?? ""));
            return result;
        }

        private static DatabaseSchemaMrg.DatabaseFixture GetFixture()
        {
            var id = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (!id.HasValue) return null;
            return AppCacheManagerBL.GetOneDatabaseFixture(id.Value);
        }

        private static DbParameter P(DatabaseSchemaMrg.DatabaseFixture f, string name, object value)
        {
            var p = f.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
