using System;
using System.Collections.Generic;
using System.Data.Common;
using App.BL;
using DatabaseSchemaMrg;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>Resolves AppAgentSkillSet.AgentID from the public AgentCode (API SkillKey).</summary>
    public static class AppAgentLookup
    {
        public static int? IdByCode(DatabaseFixture fixture, string agentCode)
        {
            if (fixture == null || string.IsNullOrWhiteSpace(agentCode)) return null;
            try
            {
                var p = fixture.CreateParameter("@C");
                p.Value = agentCode.Trim();
                var dt = fixture.RetriveDataTable(
                    "SELECT AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode=@C",
                    new List<DbParameter> { p });
                if (dt == null || dt.Rows.Count == 0) return null;
                return Convert.ToInt32(dt.Rows[0]["AgentID"]);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Specialist chats store AppAgentSkillSet.AgentID.
        /// Built-in General Agent is not a SkillSet row; session AgentID is NULL.
        /// </summary>
        public static bool TryResolveSessionAgentId(DatabaseFixture fixture, string skillKey, out int? agentId)
        {
            agentId = null;
            if (fixture == null || string.IsNullOrWhiteSpace(skillKey)) return false;
            if (GeneralAgentConstants.IsGeneralAgentKey(skillKey)) return true;
            agentId = IdByCode(fixture, skillKey);
            return agentId.HasValue;
        }

        public static int? IdByCode(int dataSourceId, string agentCode)
        {
            if (dataSourceId <= 0) return null;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            return IdByCode(fixture, agentCode);
        }

        public static string CodeById(DatabaseFixture fixture, int agentId)
        {
            if (fixture == null || agentId <= 0) return "";
            try
            {
                var p = fixture.CreateParameter("@Id");
                p.Value = agentId;
                var dt = fixture.RetriveDataTable(
                    "SELECT AgentCode FROM dbo.AppAgentSkillSet WHERE AgentID=@Id",
                    new List<DbParameter> { p });
                if (dt == null || dt.Rows.Count == 0) return "";
                return dt.Rows[0]["AgentCode"] as string ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
