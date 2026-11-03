using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;
using App.BL.TenantBusiness;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;
using NLog;

namespace App.BL.AIAgent.GenericAgent
{
    public static class AppAgentSkillSetBL
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string SelectColsBase = @"
            AgentID,AgentCode AS SkillKey,DisplayName,Description,SystemPrompt,CapabilityFlags,IsActive,SortOrder,Version,
            MaxHistoryTokens,SummarizeThreshold,MaxToolResultChars,RecentWindowSize,MaxIterations,ExecutionMode,AgentUi";

        public static List<AppAgentSkillSetDto> GetAllSkillSets(int dataSourceId)
        {
            var list = new List<AppAgentSkillSetDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    $"SELECT {SelectCols(fixture)} FROM dbo.AppAgentSkillSet ORDER BY SortOrder,AgentCode",
                    new List<DbParameter>());
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
                EnrichChildCounts(fixture, list);
            }
            catch (Exception ex) { Log.Error(ex, nameof(GetAllSkillSets)); }
            return list;
        }

        public static List<AppAgentSkillSetDto> GetTemplates(int dataSourceId)
        {
            var list = new List<AppAgentSkillSetDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    $"SELECT {SelectCols(fixture)} FROM dbo.AppAgentSkillSet WHERE AgentCode LIKE 'tmpl-%' AND IsActive = 0 ORDER BY SortOrder,AgentCode",
                    new List<DbParameter>());
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
            }
            catch (Exception ex) { Log.Error(ex, nameof(GetTemplates)); }
            return list;
        }

        public static bool UpsertSkillSet(int dataSourceId, AppAgentSkillSetDto dto)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;

                var code = (dto.AgentCode ?? "").Trim();
                if (string.IsNullOrWhiteSpace(code)) return false;

                // Snapshot current SystemPrompt into history (update by AgentID only)
                string existingSql = dto.AgentId > 0
                    ? "SELECT SystemPrompt FROM dbo.AppAgentSkillSet WHERE AgentID = @AgentID"
                    : "SELECT SystemPrompt FROM dbo.AppAgentSkillSet WHERE 1=0";
                var existingParams = new List<DbParameter>();
                if (dto.AgentId > 0)
                    existingParams.Add(BuildParam(fixture, "@AgentID", dto.AgentId));
                var existing = fixture.RetriveDataTable(existingSql, existingParams);
                if (existing != null && existing.Rows.Count > 0)
                {
                    var currentPrompt = existing.Rows[0]["SystemPrompt"] as string ?? "";
                    var newPrompt     = dto.SystemPrompt ?? "";
                    if (!string.Equals(currentPrompt, newPrompt, StringComparison.Ordinal))
                        AppAgentSkillSetHistoryBL.Insert(dataSourceId, code, currentPrompt);
                }

                var hasFirstTurn = HasColumn(fixture, "AllowAgentFirstTurn");
                var hasThink     = HasColumn(fixture, "EnableThinkMode");
                var updateExtra  = "";
                var insertCols   = "";
                var insertVals   = "";
                if (hasFirstTurn)
                {
                    updateExtra += ", AllowAgentFirstTurn=@AllowAgentFirstTurn";
                    insertCols  += ",AllowAgentFirstTurn";
                    insertVals  += ",@AllowAgentFirstTurn";
                }
                if (hasThink)
                {
                    updateExtra += ", EnableThinkMode=@EnableThinkMode";
                    insertCols  += ",EnableThinkMode";
                    insertVals  += ",@EnableThinkMode";
                }

                var sql = $@"
IF @AgentID > 0 AND EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE AgentID = @AgentID)
    UPDATE dbo.AppAgentSkillSet SET
        AgentCode=@AgentCode, DisplayName=@DisplayName, Description=@Description, SystemPrompt=@SystemPrompt,
        CapabilityFlags=@CapabilityFlags, IsActive=@IsActive, SortOrder=@SortOrder,
        Version=@Version, MaxHistoryTokens=@MaxHistoryTokens, SummarizeThreshold=@SummarizeThreshold,
        MaxToolResultChars=@MaxToolResultChars, RecentWindowSize=@RecentWindowSize,
        MaxIterations=@MaxIterations, ExecutionMode=@ExecutionMode, AgentUi=@AgentUi{updateExtra}
    WHERE AgentID = @AgentID
ELSE IF @AgentID <= 0
    INSERT INTO dbo.AppAgentSkillSet (AgentCode,DisplayName,Description,SystemPrompt,CapabilityFlags,IsActive,SortOrder,Version,MaxHistoryTokens,SummarizeThreshold,MaxToolResultChars,RecentWindowSize,MaxIterations,ExecutionMode,AgentUi{insertCols})
    VALUES (@AgentCode,@DisplayName,@Description,@SystemPrompt,@CapabilityFlags,@IsActive,@SortOrder,@Version,@MaxHistoryTokens,@SummarizeThreshold,@MaxToolResultChars,@RecentWindowSize,@MaxIterations,@ExecutionMode,@AgentUi{insertVals})";
                fixture.ExecuteNonQueryResult(sql, BuildParams(fixture, dto, hasFirstTurn, hasThink));
                return true;
            }
            catch (Exception ex) { Log.Error(ex, nameof(UpsertSkillSet)); return false; }
        }

        public static bool DeleteSkillSet(int dataSourceId, string skillKey)
        {
            return TryDeleteSkillSet(dataSourceId, skillKey, out _);
        }

        /// <summary>
        /// Deletes an agent. Blocks when the agent is registered as someone else's Child-Agent.
        /// When deleting an Orchestrator, removes its AppAgentChildMapping rows first (Child agents kept).
        /// </summary>
        public static bool TryDeleteSkillSet(int dataSourceId, string skillKey, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(skillKey))
            {
                error = "SkillKey is required.";
                return false;
            }
            skillKey = skillKey.Trim();
            if (GeneralAgentConstants.IsGeneralAgentKey(skillKey))
            {
                error = "General Agent is built in and cannot be deleted.";
                return false;
            }
            try
            {
                var usedBy = AppAgentChildMappingBL.GetUsedByParents(dataSourceId, skillKey);
                if (usedBy.Count > 0)
                {
                    error = $"Cannot delete '{skillKey}': it is a Child-Agent of: {string.Join(", ", usedBy)}. Remove it from those Orchestrators first.";
                    return false;
                }

                // Drop Orchestrator → Child links; keep Child-Agent skill rows.
                AppAgentChildMappingBL.DeleteLinksForParent(dataSourceId, skillKey);

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null)
                {
                    error = "Database fixture not available.";
                    return false;
                }
                var p = fixture.CreateParameter("@SkillKey"); p.Value = skillKey;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentSkillSet WHERE AgentCode = @SkillKey",
                    new List<DbParameter> { p });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(TryDeleteSkillSet));
                error = ex.Message;
                return false;
            }
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

        private static string SelectCols(DatabaseFixture fixture)
        {
            var cols = SelectColsBase;
            if (HasColumn(fixture, "AllowAgentFirstTurn")) cols += ",AllowAgentFirstTurn";
            if (HasColumn(fixture, "EnableThinkMode")) cols += ",EnableThinkMode";
            return cols;
        }

        private static bool HasColumn(DatabaseFixture fixture, string columnName)
        {
            if (columnName != "AllowAgentFirstTurn" && columnName != "EnableThinkMode")
                return false;
            try
            {
                var dt = fixture.RetriveDataTable(
                    $"SELECT 1 AS x FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AppAgentSkillSet') AND name = N'{columnName}'",
                    new List<DbParameter>());
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed); return false; }
        }

        private static AppAgentSkillSetDto MapRow(DataRow row) => new AppAgentSkillSetDto
        {
            AgentId            = ColInt(row, "AgentID", 0),
            AgentCode          = row["SkillKey"] as string ?? "",
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
            MaxIterations      = ColInt(row, "MaxIterations", 40),
            ExecutionMode      = ColStr(row, "ExecutionMode", "Interactive"),
            AgentUi            = ColInt(row, "AgentUi", 1),
            AllowAgentFirstTurn = ColBool(row, "AllowAgentFirstTurn", false),
            EnableThinkMode     = ColBool(row, "EnableThinkMode", false),
            ChildCount = 0,
            UsedByCount = 0,
        };

        private static void EnrichChildCounts(DatabaseFixture fixture, List<AppAgentSkillSetDto> list)
        {
            if (fixture == null || list == null || list.Count == 0) return;
            if (!AppAgentChildMappingBL.TableExists(fixture)) return;
            try
            {
                var dt = fixture.RetriveDataTable(
                    "SELECT ParentAgentId, ChildAgentId FROM dbo.AppAgentChildMapping",
                    new List<DbParameter>());
                if (dt == null || dt.Rows.Count == 0) return;

                var childCount = new Dictionary<int, int>();
                var usedByCount = new Dictionary<int, int>();
                foreach (DataRow row in dt.Rows)
                {
                    var parent = Convert.ToInt32(row["ParentAgentId"]);
                    var child = Convert.ToInt32(row["ChildAgentId"]);
                    childCount[parent] = childCount.TryGetValue(parent, out var pc) ? pc + 1 : 1;
                    usedByCount[child] = usedByCount.TryGetValue(child, out var uc) ? uc + 1 : 1;
                }
                foreach (var dto in list)
                {
                    if (childCount.TryGetValue(dto.AgentId, out var cc)) dto.ChildCount = cc;
                    if (usedByCount.TryGetValue(dto.AgentId, out var ub)) dto.UsedByCount = ub;
                }
            }
            catch (Exception ex) { Log.Warn(ex, nameof(EnrichChildCounts)); }
        }

        private static int ColInt(DataRow row, string col, int fallback)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return fallback;
            return Convert.ToInt32(row[col]);
        }

        private static string ColStr(DataRow row, string col, string fallback)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return fallback;
            var s = row[col] as string;
            return string.IsNullOrWhiteSpace(s) ? fallback : s;
        }

        private static bool ColBool(DataRow row, string col, bool fallback)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return fallback;
            return Convert.ToBoolean(row[col]);
        }

        private static DbParameter BuildParam(DatabaseFixture fixture, string name, object value)
        {
            var p = fixture.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        private static List<DbParameter> BuildParams(
            DatabaseFixture fixture,
            AppAgentSkillSetDto dto,
            bool includeAllowAgentFirstTurn,
            bool includeEnableThinkMode)
        {
            var p = new List<DbParameter>();
            void Add(string n, object v) { var x = fixture.CreateParameter(n); x.Value = v ?? DBNull.Value; p.Add(x); }
            Add("@AgentID",            dto.AgentId);
            Add("@AgentCode",          (dto.AgentCode ?? "").Trim());
            Add("@DisplayName",        string.IsNullOrEmpty(dto.DisplayName) ? (dto.AgentCode ?? "") : dto.DisplayName);
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
            Add("@ExecutionMode",      string.IsNullOrWhiteSpace(dto.ExecutionMode) ? "Interactive" : dto.ExecutionMode);
            Add("@AgentUi",            dto.AgentUi > 0 ? dto.AgentUi : 1);
            if (includeAllowAgentFirstTurn)
                Add("@AllowAgentFirstTurn", dto.AllowAgentFirstTurn);
            if (includeEnableThinkMode)
                Add("@EnableThinkMode", dto.EnableThinkMode);
            return p;
        }
    }
}

