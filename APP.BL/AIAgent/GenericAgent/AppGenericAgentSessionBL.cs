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
        /// <summary>DisplayTitle if renamed; otherwise first real user message.</summary>
        public string        Title      { get; set; }
        public List<JObject> Messages   { get; set; }
    }

    /// <summary>
    /// Persistence for GenericAgent conversations in dbo.AppGenericAgentSession
    /// (SessionKey, SkillKey, UserId, MessagesJson, DisplayTitle, UpdatedAt).
    /// SessionKey holds either:
    ///   - "{SkillKey}:{UserId}" for Agent Management test RUN (fixed session)
    ///   - a new GUID for New Chat based on an agent
    /// </summary>
    public static class AppGenericAgentSessionBL
    {
        private const int AutoTitleMax = 80;
        private const int RenameTitleMax = 200;

        private const string EnsureDisplayTitleSql = @"
IF COL_LENGTH('dbo.AppGenericAgentSession', 'DisplayTitle') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD DisplayTitle NVARCHAR(200) NULL";

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
                EnsureSchema(fixture);

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
                EnsureSchema(fixture);

                if (take <= 0 || take > 200) take = 50;
                var fixedKey = MakeFixedKey(skillKey.Trim(), userId);

                var dt = fixture.RetriveDataTable(
                    @"SELECT TOP (@Take) SessionKey, SkillKey, MessagesJson, DisplayTitle, UpdatedAt
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
                    list.Add(new GenericAgentSessionSummaryDto
                    {
                        SessionKey = key,
                        SkillKey   = row["SkillKey"] as string,
                        Title      = ResolveTitle(row),
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
                EnsureSchema(fixture);

                var dt = fixture.RetriveDataTable(
                    @"SELECT SessionKey, SkillKey, MessagesJson, DisplayTitle
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
                    Title      = ResolveTitle(row),
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

        /// <summary>
        /// HITL gate: append or replace the pending assistant. Never overwrite prior turns
        /// with the empty request.Messages from a long [session_start] run.
        /// </summary>
        public static void PersistAskUserPending(
            string skillKey,
            int userId,
            int dataSourceId,
            string sessionKey,
            string assistantContent,
            JObject pendingAskUser,
            string runSessionId,
            IList<JObject> toolSteps = null)
        {
            if (string.IsNullOrWhiteSpace(sessionKey)) return;
            var list = CloneMessages(LoadBySessionKey(sessionKey, skillKey, userId)?.Messages);
            StripEphemeralMarkers(list);

            var assistant = new JObject
            {
                ["role"] = "assistant",
                ["content"] = assistantContent ?? "",
                ["pendingAskUser"] = pendingAskUser,
                ["runSessionId"] = runSessionId
            };
            if (toolSteps != null && toolSteps.Count > 0)
                assistant["toolSteps"] = new JArray(toolSteps);

            if (IsPendingAskAssistant(Last(list)))
                list[list.Count - 1] = assistant;
            else
                list.Add(assistant);

            SaveSession(skillKey, userId, dataSourceId, list, sessionKey);
        }

        /// <summary>Finalize the pending ask and append the user's answer (same Chat, next line).</summary>
        public static void PersistAskUserAnswer(
            string skillKey,
            int userId,
            int dataSourceId,
            string sessionKey,
            string answerText)
        {
            if (string.IsNullOrWhiteSpace(sessionKey)) return;
            var list = CloneMessages(LoadBySessionKey(sessionKey, skillKey, userId)?.Messages);
            StripEphemeralMarkers(list);

            var last = Last(list);
            if (IsPendingAskAssistant(last))
            {
                var prompt = last["pendingAskUser"]?["Prompt"]?.ToString()
                    ?? last["pendingAskUser"]?["prompt"]?.ToString();
                var content = last.Value<string>("content");
                if (string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(prompt))
                    last["content"] = prompt;
                last.Remove("pendingAskUser");
                last.Remove("PendingAskUser");
            }

            if (!string.IsNullOrWhiteSpace(answerText)
                && !IsSameUserContent(Last(list), answerText))
            {
                list.Add(new JObject { ["role"] = "user", ["content"] = answerText });
            }

            SaveSession(skillKey, userId, dataSourceId, list, sessionKey);
        }

        /// <summary>
        /// End of a Run: append this turn onto MessagesJson already on disk.
        /// Do not replace the transcript with request.Messages (often [] on session_start).
        /// </summary>
        public static void PersistTurnDone(
            string skillKey,
            int userId,
            int dataSourceId,
            string sessionKey,
            string userMessage,
            bool isSessionStart,
            string assistantContent,
            IList<JObject> toolSteps = null)
        {
            if (string.IsNullOrWhiteSpace(sessionKey)) return;
            var list = CloneMessages(LoadBySessionKey(sessionKey, skillKey, userId)?.Messages);
            StripEphemeralMarkers(list);

            if (!isSessionStart && !string.IsNullOrWhiteSpace(userMessage)
                && !IsSameUserContent(Last(list), userMessage))
            {
                list.Add(new JObject { ["role"] = "user", ["content"] = userMessage });
            }

            var assistant = new JObject
            {
                ["role"] = "assistant",
                ["content"] = assistantContent ?? ""
            };
            if (toolSteps != null && toolSteps.Count > 0)
                assistant["toolSteps"] = new JArray(toolSteps);

            if (IsPendingAskAssistant(Last(list)))
                list[list.Count - 1] = assistant;
            else
                list.Add(assistant);

            SaveSession(skillKey, userId, dataSourceId, list, sessionKey);
        }

        public static string FormatAskUserAnswer(JObject pendingAskUser, AgentAskUserResponse response)
        {
            if (response == null) return "(empty)";
            if (response.Cancelled) return "(cancelled)";

            var mode = (pendingAskUser?["Mode"] ?? pendingAskUser?["mode"])?.ToString()?.ToLowerInvariant();
            if (mode == "single_choice" || mode == "multi_choice")
            {
                var labels = new List<string>();
                var options = pendingAskUser?["Options"] ?? pendingAskUser?["options"] as JToken;
                foreach (var id in response.SelectedIds ?? new List<string>())
                {
                    string label = id;
                    if (options is JArray arr)
                    {
                        foreach (var opt in arr)
                        {
                            var optId = (opt?["Id"] ?? opt?["id"])?.ToString();
                            if (string.Equals(optId, id, StringComparison.Ordinal))
                            {
                                label = (opt?["Display"] ?? opt?["display"])?.ToString() ?? id;
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(label)) labels.Add(label);
                }
                return labels.Count > 0 ? string.Join(", ", labels) : "(no selection)";
            }

            var fields = pendingAskUser?["Fields"] ?? pendingAskUser?["fields"];
            if (fields is JArray fieldArr && fieldArr.Count > 0 && response.Answers != null && response.Answers.Count > 0)
            {
                var lines = new List<string>();
                foreach (var field in fieldArr)
                {
                    var name = (field?["Name"] ?? field?["name"])?.ToString();
                    var label = (field?["Label"] ?? field?["label"])?.ToString() ?? name;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    response.Answers.TryGetValue(name, out var raw);
                    lines.Add($"{label}: {(string.IsNullOrWhiteSpace(raw) ? "(empty)" : raw)}");
                }
                if (lines.Count > 0) return string.Join("\n", lines);
            }

            return string.IsNullOrWhiteSpace(response.FreeText) ? "(empty)" : response.FreeText.Trim();
        }

        private static List<JObject> CloneMessages(List<JObject> source)
        {
            var list = new List<JObject>();
            if (source == null) return list;
            foreach (var m in source)
            {
                if (m != null) list.Add((JObject)m.DeepClone());
            }
            return list;
        }

        private static void StripEphemeralMarkers(List<JObject> list)
        {
            if (list == null) return;
            list.RemoveAll(m =>
            {
                if (m == null) return true;
                var role = m.Value<string>("role");
                var content = m.Value<string>("content");
                return string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(content, "[run_in_progress]", StringComparison.Ordinal);
            });
        }

        private static JObject Last(List<JObject> list) =>
            list == null || list.Count == 0 ? null : list[list.Count - 1];

        private static bool IsAssistant(JObject m) =>
            m != null && string.Equals(m.Value<string>("role"), "assistant", StringComparison.OrdinalIgnoreCase);

        private static bool IsPendingAskAssistant(JObject m) =>
            IsAssistant(m) && (m["pendingAskUser"] != null || m["PendingAskUser"] != null);

        private static bool IsSameUserContent(JObject m, string content)
        {
            if (m == null || string.IsNullOrWhiteSpace(content)) return false;
            if (!string.Equals(m.Value<string>("role"), "user", StringComparison.OrdinalIgnoreCase))
                return false;
            return string.Equals(m.Value<string>("content")?.Trim(), content.Trim(), StringComparison.Ordinal);
        /// Removes persisted in-progress markers whose in-memory run was lost during an
        /// application restart. The completed chat messages are preserved.
        /// </summary>
        public static void ClearStaleRunState(
            string skillKey,
            int userId,
            int dataSourceId,
            string sessionKey)
        {
            var detail = LoadBySessionKey(sessionKey, skillKey, userId);
            if (detail?.Messages == null) return;

            var changed = false;
            var cleaned = new List<JObject>();
            foreach (var message in detail.Messages)
            {
                if (message == null) continue;
                var content = message.Value<string>("content");
                if (string.Equals(message.Value<string>("role"), "system", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(content, "[run_in_progress]", StringComparison.Ordinal))
                {
                    changed = true;
                    continue;
                }

                var copy = (JObject)message.DeepClone();
                if (copy["runSessionId"] != null)
                {
                    copy.Remove("runSessionId");
                    changed = true;
                }
                if (copy["pendingAskUser"] != null || copy["PendingAskUser"] != null)
                {
                    copy.Remove("pendingAskUser");
                    copy.Remove("PendingAskUser");
                    changed = true;
                }
                cleaned.Add(copy);
            }

            if (changed)
                SaveSession(skillKey, userId, dataSourceId, cleaned, sessionKey);
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
                EnsureSchema(fixture);

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

        /// <summary>Wipe MessagesJson for a chat but keep the SessionKey row (Clear conversation).</summary>
        public static bool ClearBySessionKey(string sessionKey, string skillKey, int userId)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return false;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return false;
                var n = fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppGenericAgentSession
                      SET MessagesJson=N'[]', UpdatedAt=GETUTCDATE()
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
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
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(ClearBySessionKey));
                return false;
            }
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

        /// <summary>Manual rename. Sets DisplayTitle and later auto-title will not overwrite it.</summary>
        public static bool Rename(string sessionKey, string skillKey, int userId, string title)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return false;
            var next = NormalizeTitle(title, RenameTitleMax);
            if (string.IsNullOrWhiteSpace(next)) return false;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return false;
                EnsureSchema(fixture);
                var n = fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppGenericAgentSession
                      SET DisplayTitle=@T, UpdatedAt=GETUTCDATE()
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    new List<DbParameter>
                    {
                        P(fixture, "@T", next),
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });
                return n > 0;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(Rename));
                return false;
            }
        }

        /// <summary>
        /// First real user message becomes DisplayTitle only when it is still empty.
        /// Prefers the first persisted user turn so later "Continue" does not steal the name.
        /// </summary>
        public static void TryAutoTitle(string sessionKey, string skillKey, int userId, string incomingUserMessage)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return;
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return;
                EnsureSchema(fixture);

                var dt = fixture.RetriveDataTable(
                    @"SELECT DisplayTitle, MessagesJson
                      FROM dbo.AppGenericAgentSession
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    new List<DbParameter>
                    {
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });
                if (dt == null || dt.Rows.Count == 0) return;
                var row = dt.Rows[0];
                if (!string.IsNullOrWhiteSpace(ColStr(row, "DisplayTitle"))) return;

                var title = DeriveTitle(ParseMessages(row["MessagesJson"]?.ToString()))
                    ?? NormalizeTitle(incomingUserMessage, AutoTitleMax);
                if (string.IsNullOrWhiteSpace(title)) return;

                fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppGenericAgentSession
                      SET DisplayTitle=@T
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U
                        AND (DisplayTitle IS NULL OR LTRIM(RTRIM(DisplayTitle)) = N'')",
                    new List<DbParameter>
                    {
                        P(fixture, "@T", title),
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(TryAutoTitle));
            }
        }

        private static List<JObject> ParseMessages(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<JObject>();
            return JsonConvert.DeserializeObject<List<JObject>>(json) ?? new List<JObject>();
        }

        private static string ResolveTitle(DataRow row)
        {
            var display = ColStr(row, "DisplayTitle");
            if (!string.IsNullOrWhiteSpace(display)) return display.Trim();
            return DeriveTitle(ParseMessages(row["MessagesJson"]?.ToString()));
        }

        private static string DeriveTitle(List<JObject> messages)
        {
            if (messages == null) return null;
            foreach (var m in messages)
            {
                if (m == null) continue;
                var role = m["role"]?.ToString();
                if (!string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)) continue;
                var content = ExtractPlainText(m["content"]);
                var title = NormalizeTitle(content, AutoTitleMax);
                if (!string.IsNullOrWhiteSpace(title)) return title;
            }
            return null;
        }

        private static string ExtractPlainText(JToken content)
        {
            if (content == null || content.Type == JTokenType.Null) return null;
            if (content.Type == JTokenType.String) return content.ToString();
            if (content.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)content)
                {
                    if (item == null) continue;
                    var text = item.Type == JTokenType.String
                        ? item.ToString()
                        : item["text"]?.ToString() ?? item["content"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
                return null;
            }
            var s = content.Type == JTokenType.Object ? content["text"]?.ToString() : content.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static string NormalizeTitle(string raw, int max)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var content = raw.Trim().Replace("\r", " ").Replace("\n", " ");
            while (content.Contains("  ")) content = content.Replace("  ", " ");
            if (IsIgnoredUserText(content)) return null;
            return Trunc(content, max);
        }

        private static bool IsIgnoredUserText(string content) =>
            string.Equals(content, "[session_start]", StringComparison.Ordinal)
            || string.Equals(content, "[run_in_progress]", StringComparison.Ordinal);

        private static void EnsureSchema(DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            if (fixture == null) return;
            try { fixture.ExecuteNonQueryResult(EnsureDisplayTitleSql, new List<DbParameter>()); }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(EnsureSchema));
            }
        }

        private static string Trunc(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || max <= 0) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        private static string ColStr(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return null;
            var v = row[column];
            return v == null || v == DBNull.Value ? null : v.ToString();
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
