using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    public class GenericAgentSessionDetailDto
    {
        public string        SessionKey { get; set; }
        public string        SkillKey   { get; set; }
        /// <summary>Derived from first user message — not a DB column.</summary>
        public string        Title      { get; set; }
        public List<JObject> Messages   { get; set; }
    }

    /// <summary>
    /// Persistence for GenericAgent conversations in dbo.AppGenericAgentSession
    /// (SessionKey, SkillKey, UserId, MessagesJson, UpdatedAt — no schema change).
    /// SessionKey holds either:
    ///   - "{SkillKey}:{UserId}" for Agent Management test RUN (fixed session)
    ///   - a new GUID for New Chat based on an agent
    /// </summary>
    public static class AppGenericAgentSessionBL
    {
        public static string MakeFixedKey(string skillKey, int userId) =>
            $"{skillKey}:{userId}";

        public static bool IsFixedKey(string sessionKey, string skillKey, int userId) =>
            string.Equals(sessionKey, MakeFixedKey(skillKey, userId), StringComparison.Ordinal);

        public static string CreateChat(string skillKey, int userId, int dataSourceId)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0) return null;
            try
            {
                var fixture = Fixture(dataSourceId);
                if (fixture == null) return null;

                var key = Guid.NewGuid().ToString("D");
                const string sql = @"
INSERT INTO dbo.AppGenericAgentSession (SessionKey, SkillKey, UserId, MessagesJson, UpdatedAt)
VALUES (@K, @S, @U, N'[]', GETUTCDATE())";

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter>
                {
                    P(fixture, "@K", key),
                    P(fixture, "@S", skillKey.Trim()),
                    P(fixture, "@U", userId)
                });
                return key;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(CreateChat));
                return null;
            }
        }

        public static List<GenericAgentSessionSummaryDto> ListChats(string skillKey, int userId, int take = 50)
        {
            var list = new List<GenericAgentSessionSummaryDto>();
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0) return list;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return list;

                if (take <= 0 || take > 200) take = 50;
                var fixedKey = MakeFixedKey(skillKey.Trim(), userId);

                var dt = fixture.RetriveDataTable(
                    @"SELECT TOP (@Take) SessionKey, SkillKey, MessagesJson, UpdatedAt
                      FROM dbo.AppGenericAgentSession
                      WHERE SkillKey=@SkillKey AND UserId=@UserId
                      ORDER BY UpdatedAt DESC",
                    new List<DbParameter>
                    {
                        P(fixture, "@Take", take),
                        P(fixture, "@SkillKey", skillKey.Trim()),
                        P(fixture, "@UserId", userId)
                    });

                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                {
                    var key = row["SessionKey"] as string;
                    var messages = ParseMessages(row["MessagesJson"]?.ToString());
                    list.Add(new GenericAgentSessionSummaryDto
                    {
                        SessionKey = key,
                        SkillKey   = row["SkillKey"] as string,
                        Title      = DeriveTitle(messages),
                        UpdatedAt  = ColDt(row, "UpdatedAt"),
                        IsFixedTestSession = string.Equals(key, fixedKey, StringComparison.Ordinal)
                    });
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(ListChats));
            }
            return list;
        }

        public static GenericAgentSessionDetailDto LoadBySessionKey(string sessionKey, string skillKey, int userId)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return null;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return null;

                var dt = fixture.RetriveDataTable(
                    @"SELECT SessionKey, SkillKey, MessagesJson
                      FROM dbo.AppGenericAgentSession
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    new List<DbParameter>
                    {
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });

                if (dt == null || dt.Rows.Count == 0) return null;
                var row = dt.Rows[0];
                var messages = ParseMessages(row["MessagesJson"]?.ToString());
                return new GenericAgentSessionDetailDto
                {
                    SessionKey = row["SessionKey"] as string,
                    SkillKey   = row["SkillKey"] as string,
                    Title      = DeriveTitle(messages),
                    Messages   = messages
                };
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(LoadBySessionKey));
                return null;
            }
        }

        public static List<JObject> LoadFixedSession(string skillKey, int userId)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0) return null;
            var detail = LoadBySessionKey(MakeFixedKey(skillKey.Trim(), userId), skillKey.Trim(), userId);
            return detail?.Messages;
        }

        public static void SaveSession(
            string skillKey,
            int userId,
            int dataSourceId,
            List<JObject> messages,
            string sessionKey = null)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || userId <= 0 || messages == null) return;
            try
            {
                var fixture = Fixture(dataSourceId);
                if (fixture == null) return;

                var key = string.IsNullOrWhiteSpace(sessionKey)
                    ? MakeFixedKey(skillKey.Trim(), userId)
                    : sessionKey.Trim();
                var json = JsonConvert.SerializeObject(messages);

                if (!IsFixedKey(key, skillKey.Trim(), userId))
                {
                    var existing = LoadBySessionKey(key, skillKey.Trim(), userId);
                    if (existing == null) return;
                }

                const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppGenericAgentSession WHERE SessionKey=@K)
    UPDATE dbo.AppGenericAgentSession
    SET MessagesJson=@J, SkillKey=@S, UserId=@U, UpdatedAt=GETUTCDATE()
    WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U
ELSE
    INSERT INTO dbo.AppGenericAgentSession (SessionKey, SkillKey, UserId, MessagesJson, UpdatedAt)
    VALUES (@K, @S, @U, @J, GETUTCDATE())";

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter>
                {
                    P(fixture, "@K", key),
                    P(fixture, "@J", json),
                    P(fixture, "@S", skillKey.Trim()),
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
            DeleteBySessionKey(MakeFixedKey(skillKey.Trim(), userId), skillKey.Trim(), userId);
        }

        public static bool DeleteBySessionKey(string sessionKey, string skillKey, int userId)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return false;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return false;
                var n = fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppGenericAgentSession WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    new List<DbParameter>
                    {
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });
                return n > 0;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(DeleteBySessionKey));
                return false;
            }
        }

        public static List<JObject> LoadSession(string skillKey, int userId) =>
            LoadFixedSession(skillKey, userId);

        private static List<JObject> ParseMessages(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<JObject>();
            return JsonConvert.DeserializeObject<List<JObject>>(json) ?? new List<JObject>();
        }

        private static string DeriveTitle(List<JObject> messages)
        {
            if (messages == null) return null;
            foreach (var m in messages)
            {
                if (m == null) continue;
                var role = m["role"]?.ToString();
                if (!string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)) continue;
                var content = m["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(content)) continue;
                content = content.Trim().Replace("\r", " ").Replace("\n", " ");
                return content.Length > 80 ? content.Substring(0, 80) : content;
            }
            return null;
        }

        private static DatabaseSchemaMrg.DatabaseFixture Fixture(int dataSourceId) =>
            dataSourceId > 0
                ? AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId)
                : GetFixture();

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

        private static DateTime ColDt(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return default;
            return row[col] is DateTime dt ? dt : default;
        }
    }
}
