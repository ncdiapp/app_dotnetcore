using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    public static class AppGenericAgentSessionBL
    {
        private static string MakeKey(string skillKey, int userId) =>
            $"{skillKey}:{userId}";

        public static List<JObject> LoadSession(string skillKey, int userId)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0) return null;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return null;

                var dt = fixture.RetriveDataTable(
                    "SELECT MessagesJson FROM dbo.AppGenericAgentSession WHERE SessionKey=@K",
                    new List<DbParameter> { P(fixture, "@K", MakeKey(skillKey, userId)) });

                if (dt == null || dt.Rows.Count == 0) return null;
                var json = dt.Rows[0]["MessagesJson"]?.ToString();
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonConvert.DeserializeObject<List<JObject>>(json);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(LoadSession));
                return null;
            }
        }

        public static void SaveSession(string skillKey, int userId, int dataSourceId, List<JObject> messages)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0 || messages == null) return;
            try
            {
                var fixture = dataSourceId > 0
                    ? AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId)
                    : GetFixture();
                if (fixture == null) return;

                var key  = MakeKey(skillKey, userId);
                var json = JsonConvert.SerializeObject(messages);

                const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppGenericAgentSession WHERE SessionKey=@K)
    UPDATE dbo.AppGenericAgentSession SET MessagesJson=@J, SkillKey=@S, UserId=@U, UpdatedAt=GETUTCDATE() WHERE SessionKey=@K
ELSE
    INSERT INTO dbo.AppGenericAgentSession (SessionKey, SkillKey, UserId, MessagesJson, UpdatedAt)
    VALUES (@K, @S, @U, @J, GETUTCDATE())";

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter>
                {
                    P(fixture, "@K", key),
                    P(fixture, "@J", json),
                    P(fixture, "@S", skillKey),
                    P(fixture, "@U", userId)
                });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(SaveSession));
            }
        }

        public static void DeleteSession(string skillKey, int userId)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0) return;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppGenericAgentSession WHERE SessionKey=@K",
                    new List<DbParameter> { P(fixture, "@K", MakeKey(skillKey, userId)) });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(DeleteSession));
            }
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
