using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;

namespace App.BL.AIAgent.GenericAgent
{
    public static class AppAgentSkillSetBL
    {
        public static List<AppAgentSkillSetDto> GetAllSkillSets(int dataSourceId)
        {
            var list = new List<AppAgentSkillSetDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    "SELECT SkillKey,DisplayName,Description,SystemPrompt,CapabilityFlags,IsActive,SortOrder,Version,MaxHistoryTokens,SummarizeThreshold,MaxToolResultChars,RecentWindowSize,MaxIterations FROM dbo.AppAgentSkillSet ORDER BY SortOrder,SkillKey",
                    new List<DbParameter>());
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("GetAllSkillSets error: " + ex); }
            return list;
        }

        public static bool UpsertSkillSet(int dataSourceId, AppAgentSkillSetDto dto)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;
                const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = @SkillKey)
    UPDATE dbo.AppAgentSkillSet SET
        DisplayName=@DisplayName, Description=@Description, SystemPrompt=@SystemPrompt,
        CapabilityFlags=@CapabilityFlags, IsActive=@IsActive, SortOrder=@SortOrder,
        Version=@Version, MaxHistoryTokens=@MaxHistoryTokens, SummarizeThreshold=@SummarizeThreshold,
        MaxToolResultChars=@MaxToolResultChars, RecentWindowSize=@RecentWindowSize,
        MaxIterations=@MaxIterations
    WHERE SkillKey = @SkillKey
ELSE
    INSERT INTO dbo.AppAgentSkillSet (SkillKey,DisplayName,Description,SystemPrompt,CapabilityFlags,IsActive,SortOrder,Version,MaxHistoryTokens,SummarizeThreshold,MaxToolResultChars,RecentWindowSize,MaxIterations)
    VALUES (@SkillKey,@DisplayName,@Description,@SystemPrompt,@CapabilityFlags,@IsActive,@SortOrder,@Version,@MaxHistoryTokens,@SummarizeThreshold,@MaxToolResultChars,@RecentWindowSize,@MaxIterations)";
                fixture.ExecuteNonQueryResult(sql, BuildParams(fixture, dto));
                return true;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("UpsertSkillSet error: " + ex); return false; }
        }

        public static bool DeleteSkillSet(int dataSourceId, string skillKey)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;
                var p = fixture.CreateParameter("@SkillKey"); p.Value = skillKey;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentSkillSet WHERE SkillKey = @SkillKey",
                    new List<DbParameter> { p });
                return true;
            }
            catch { return false; }
        }

        public static (string ConnStr, int RowCount) GetDebugInfo(int dataSourceId)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return ("(null fixture)", -1);
                var connStr = fixture.ConnectionString ?? "(no conn)";
                var dt = fixture.RetriveDataTable(
                    "SELECT COUNT(*) AS Cnt FROM dbo.AppAgentSkillSet",
                    new List<DbParameter>());
                var count = dt != null && dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["Cnt"]) : -1;
                return (connStr, count);
            }
            catch (Exception ex) { return ("ERR: " + ex.Message, -1); }
        }

        private static AppAgentSkillSetDto MapRow(DataRow row) => new AppAgentSkillSetDto
        {
            SkillKey           = row["SkillKey"] as string ?? "",
            DisplayName        = row["DisplayName"] as string ?? "",
            Description        = row["Description"] as string ?? "",
            SystemPrompt       = row["SystemPrompt"] as string ?? "",
            CapabilityFlags    = Convert.ToInt32(row["CapabilityFlags"]),
            IsActive           = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
            SortOrder          = row["SortOrder"] == DBNull.Value ? 0 : Convert.ToInt32(row["SortOrder"]),
            Version            = row["Version"] == DBNull.Value ? 1 : Convert.ToInt32(row["Version"]),
            MaxHistoryTokens   = row["MaxHistoryTokens"] == DBNull.Value ? 80000 : Convert.ToInt32(row["MaxHistoryTokens"]),
            SummarizeThreshold = row["SummarizeThreshold"] == DBNull.Value ? 60000 : Convert.ToInt32(row["SummarizeThreshold"]),
            MaxToolResultChars = row["MaxToolResultChars"] == DBNull.Value ? 4000 : Convert.ToInt32(row["MaxToolResultChars"]),
            RecentWindowSize   = row["RecentWindowSize"] == DBNull.Value ? 10 : Convert.ToInt32(row["RecentWindowSize"]),
            MaxIterations      = row.Table.Columns.Contains("MaxIterations") && row["MaxIterations"] != DBNull.Value ? Convert.ToInt32(row["MaxIterations"]) : 40,
        };

        private static List<DbParameter> BuildParams(DatabaseFixture fixture, AppAgentSkillSetDto dto)
        {
            var p = new List<DbParameter>();
            void Add(string n, object v) { var x = fixture.CreateParameter(n); x.Value = v ?? DBNull.Value; p.Add(x); }
            Add("@SkillKey",           dto.SkillKey);
            Add("@DisplayName",        string.IsNullOrEmpty(dto.DisplayName) ? dto.SkillKey : dto.DisplayName);
            Add("@Description",        (object)dto.Description ?? DBNull.Value);
            Add("@SystemPrompt",       (object)dto.SystemPrompt ?? DBNull.Value);
            Add("@CapabilityFlags",    dto.CapabilityFlags);
            Add("@IsActive",           dto.IsActive);
            Add("@SortOrder",          dto.SortOrder);
            Add("@Version",            dto.Version > 0 ? dto.Version : 1);
            Add("@MaxHistoryTokens",   dto.MaxHistoryTokens > 0 ? dto.MaxHistoryTokens : 80000);
            Add("@SummarizeThreshold", dto.SummarizeThreshold > 0 ? dto.SummarizeThreshold : 60000);
            Add("@MaxToolResultChars", dto.MaxToolResultChars > 0 ? dto.MaxToolResultChars : 4000);
            Add("@RecentWindowSize",   dto.RecentWindowSize > 0 ? dto.RecentWindowSize : 10);
            Add("@MaxIterations",      dto.MaxIterations > 0 ? dto.MaxIterations : 40);
            return p;
        }
    }
}
