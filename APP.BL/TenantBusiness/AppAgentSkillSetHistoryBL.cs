using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;
using App.BL.AIAgent.GenericAgent;
using DatabaseSchemaMrg;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentSkillSetHistoryDto(
        int      HistoryId,
        string   SkillKey,
        string   SystemPrompt,
        DateTime SavedAt,
        string?  SavedBy);

    public static class AppAgentSkillSetHistoryBL
    {
        private const int MaxHistory = 10;

        public static List<AppAgentSkillSetHistoryDto> GetRecent(int dataSourceId, string skillKey, int limit = 5)
        {
            var result = new List<AppAgentSkillSetHistoryDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return result;
                var agentId = AppAgentLookup.IdByCode(fixture, skillKey);
                if (agentId == null) return result;

                var p = new List<DbParameter> { P(fixture, "@id", agentId.Value) };
                var dt = fixture.RetriveDataTable(
                    $"SELECT TOP {limit} h.HistoryId, k.AgentCode AS SkillKey, h.SystemPrompt, h.SavedAt, h.SavedBy " +
                    "FROM dbo.AppAgentSkillSetHistory h " +
                    "INNER JOIN dbo.AppAgentSkillSet k ON k.AgentID = h.AgentID " +
                    "WHERE h.AgentID = @id " +
                    "ORDER BY h.SavedAt DESC",
                    p);
                if (dt == null) return result;
                foreach (DataRow row in dt.Rows)
                    result.Add(new AppAgentSkillSetHistoryDto(
                        HistoryId:    Convert.ToInt32(row["HistoryId"]),
                        SkillKey:     row["SkillKey"]     as string ?? "",
                        SystemPrompt: row["SystemPrompt"] as string ?? "",
                        SavedAt:      Convert.ToDateTime(row["SavedAt"]),
                        SavedBy:      row["SavedBy"] == DBNull.Value ? null : row["SavedBy"] as string));
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(GetRecent));
            }
            return result;
        }

        public static void Insert(int dataSourceId, string skillKey, string systemPrompt, string? savedBy = null)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || string.IsNullOrWhiteSpace(systemPrompt)) return;
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return;
                var agentId = AppAgentLookup.IdByCode(fixture, skillKey);
                if (agentId == null) return;

                fixture.ExecuteNonQueryResult(
                    "INSERT INTO dbo.AppAgentSkillSetHistory (AgentID, SystemPrompt, SavedBy) " +
                    "VALUES (@id, @sp, @by)",
                    new List<DbParameter>
                    {
                        P(fixture, "@id", agentId.Value),
                        P(fixture, "@sp", systemPrompt),
                        P(fixture, "@by", (object?)savedBy ?? DBNull.Value)
                    });

                Prune(fixture, agentId.Value);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(Insert));
            }
        }

        private static void Prune(DatabaseFixture fixture, int agentId)
        {
            try
            {
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentSkillSetHistory WHERE HistoryId IN (" +
                    "  SELECT HistoryId FROM dbo.AppAgentSkillSetHistory WHERE AgentID = @id " +
                    "  ORDER BY SavedAt DESC OFFSET @keep ROWS)",
                    new List<DbParameter>
                    {
                        P(fixture, "@id",   agentId),
                        P(fixture, "@keep", MaxHistory)
                    });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(Prune));
            }
        }

        private static DbParameter P(DatabaseFixture fixture, string name, object value)
        {
            var p = fixture.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
