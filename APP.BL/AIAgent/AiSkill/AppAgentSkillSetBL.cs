using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;

namespace App.BL.AIAgent.AiSkill
{
    public sealed record AppAgentSkillSetDto(
        string SkillKey,
        string DisplayName,
        string Description,
        string SystemPrompt,
        int    CapabilityFlags,
        bool   IsActive,
        int    SortOrder,
        int    Version,
        int    MaxHistoryTokens,
        int    SummarizeThreshold,
        int    MaxToolResultChars,
        int    RecentWindowSize,
        int    MaxIterations);

    public static class AppAgentSkillSetBL
    {
        private const string SelectCols = @"
            SkillKey, DisplayName, Description, SystemPrompt,
            CapabilityFlags, IsActive, SortOrder, Version,
            MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize, MaxIterations";

        public static List<AppAgentSkillSetDto> GetAll()
        {
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentSkillSetDto>();

            var dt = fixture.RetriveDataTable(
                $"SELECT {SelectCols} FROM dbo.AppAgentSkillSet WHERE IsActive=1 ORDER BY SortOrder",
                new List<DbParameter>());

            return MapAll(dt);
        }

        public static AppAgentSkillSetDto GetByKey(string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return null;
            var fixture = GetFixture();
            if (fixture == null) return null;
            return GetByKey(skillKey, fixture);
        }

        public static AppAgentSkillSetDto GetByKey(string skillKey, int dataSourceId)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return null;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return null;
            return GetByKey(skillKey, fixture);
        }

        private static AppAgentSkillSetDto GetByKey(string skillKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(
                $"SELECT {SelectCols} FROM dbo.AppAgentSkillSet WHERE SkillKey=@SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey.Trim()) });

            if (dt == null || dt.Rows.Count == 0) return null;
            return Map(dt.Rows[0]);
        }

        public static void Upsert(AppAgentSkillSetDto dto)
        {
            if (dto == null) return;
            var fixture = GetFixture();
            if (fixture == null) return;

            const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey=@SkillKey)
    UPDATE dbo.AppAgentSkillSet SET
        DisplayName=@DisplayName, Description=@Description, SystemPrompt=@SystemPrompt,
        CapabilityFlags=@CapabilityFlags, IsActive=@IsActive, SortOrder=@SortOrder,
        Version=@Version, MaxHistoryTokens=@MaxHistoryTokens, SummarizeThreshold=@SummarizeThreshold,
        MaxToolResultChars=@MaxToolResultChars, RecentWindowSize=@RecentWindowSize,
        MaxIterations=@MaxIterations
    WHERE SkillKey=@SkillKey
ELSE
    INSERT INTO dbo.AppAgentSkillSet
        (SkillKey,DisplayName,Description,SystemPrompt,CapabilityFlags,IsActive,SortOrder,
         Version,MaxHistoryTokens,SummarizeThreshold,MaxToolResultChars,RecentWindowSize,MaxIterations)
    VALUES
        (@SkillKey,@DisplayName,@Description,@SystemPrompt,@CapabilityFlags,@IsActive,@SortOrder,
         @Version,@MaxHistoryTokens,@SummarizeThreshold,@MaxToolResultChars,@RecentWindowSize,@MaxIterations)";

            fixture.ExecuteNonQueryResult(sql, Params(fixture, dto));
        }

        public static bool Delete(string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return false;
            var fixture = GetFixture();
            if (fixture == null) return false;

            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentSkillSet WHERE SkillKey=@SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey.Trim()) });
            return true;
        }

        private static List<AppAgentSkillSetDto> MapAll(DataTable dt)
        {
            var result = new List<AppAgentSkillSetDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(Map(row));
            return result;
        }

        private static AppAgentSkillSetDto Map(DataRow row)
        {
            return new AppAgentSkillSetDto(
                SkillKey:           ColStr(row, "SkillKey"),
                DisplayName:        ColStr(row, "DisplayName"),
                Description:        ColStr(row, "Description"),
                SystemPrompt:       ColStr(row, "SystemPrompt"),
                CapabilityFlags:    ColInt(row, "CapabilityFlags"),
                IsActive:           ColBool(row, "IsActive"),
                SortOrder:          ColInt(row, "SortOrder"),
                Version:            ColInt(row, "Version"),
                MaxHistoryTokens:   ColInt(row, "MaxHistoryTokens"),
                SummarizeThreshold: ColInt(row, "SummarizeThreshold"),
                MaxToolResultChars: ColInt(row, "MaxToolResultChars"),
                RecentWindowSize:   ColInt(row, "RecentWindowSize"),
                MaxIterations:      ColIntSafe(row, "MaxIterations", 40));
        }

        private static List<DbParameter> Params(DatabaseSchemaMrg.DatabaseFixture f, AppAgentSkillSetDto d)
        {
            return new List<DbParameter>
            {
                P(f, "@SkillKey",            d.SkillKey),
                P(f, "@DisplayName",         d.DisplayName),
                P(f, "@Description",         d.Description),
                P(f, "@SystemPrompt",        d.SystemPrompt),
                P(f, "@CapabilityFlags",     d.CapabilityFlags),
                P(f, "@IsActive",            d.IsActive),
                P(f, "@SortOrder",           d.SortOrder),
                P(f, "@Version",             d.Version),
                P(f, "@MaxHistoryTokens",    d.MaxHistoryTokens),
                P(f, "@SummarizeThreshold",  d.SummarizeThreshold),
                P(f, "@MaxToolResultChars",  d.MaxToolResultChars),
                P(f, "@RecentWindowSize",    d.RecentWindowSize),
                P(f, "@MaxIterations",       d.MaxIterations > 0 ? d.MaxIterations : 40)
            };
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

        private static string ColStr(DataRow row, string col) => row[col] as string ?? "";
        private static int    ColInt(DataRow row, string col)  => row[col] == DBNull.Value ? 0 : Convert.ToInt32(row[col]);
        private static bool   ColBool(DataRow row, string col) => row[col] != DBNull.Value && Convert.ToBoolean(row[col]);
        // Safe read for columns that may not exist yet in older DBs (before V011 migration)
        private static int    ColIntSafe(DataRow row, string col, int defaultVal)
        {
            try { return row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToInt32(row[col]) : defaultVal; }
            catch { return defaultVal; }
        }
    }
}
