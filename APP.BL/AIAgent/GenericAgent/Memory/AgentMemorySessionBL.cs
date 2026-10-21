using System;
using System.Collections.Generic;
using System.Data;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    public sealed class MemoryChatCandidate
    {
        public string SessionKey         { get; init; } = "";
        public string SkillKey           { get; init; } = "";
        public int    UserId             { get; init; }
        public string MessagesJson       { get; init; } = "[]";
        public int    ExtractedTurnCount { get; init; }
    }

    public sealed class MemorySessionState
    {
        public int  ExtractedTurnCount { get; init; }
        public bool OptOut             { get; init; }
    }

    /// <summary>
    /// Extraction bookkeeping on dbo.AppGenericAgentSession: which chats are ready, claiming a chat so two
    /// workers never extract it at once, and recording how many turns have been processed.
    /// None of these statements touch UpdatedAt, so they never make a chat look "active" again.
    /// </summary>
    public static class AgentMemorySessionBL
    {
        private const int ClaimTimeoutMinutes = 15;
        public const int MaxRetries = 3;

        /// <summary>An active agent (alias k) has memory when it has the InjectMemory flag or subscribes to platform-memory.</summary>
        private static readonly string MemoryAgentCondition =
            $@"k.IsActive = 1 AND ((k.CapabilityFlags & {AgentMemoryInjector.InjectMemoryFlag}) <> 0
                OR EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription sub
                           WHERE sub.SkillKey = k.SkillKey AND sub.LibraryKey = N'{AgentMemoryInjector.MemoryLibraryKey}'))";

        /// <summary>Chats of memory-enabled agents that went quiet and have turns not yet extracted.</summary>
        public static List<MemoryChatCandidate> FindReady(int dataSourceId, int idleMinutes, int batch)
        {
            var list = new List<MemoryChatCandidate>();
            try
            {
                var fixture = AgentMemoryDb.Fixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    $@"SELECT TOP ({Math.Clamp(batch, 1, 200)}) s.SessionKey, s.SkillKey, s.UserId, s.MessagesJson, s.ExtractedTurnCount
                       FROM dbo.AppGenericAgentSession s
                       JOIN dbo.AppAgentSkillSet k ON k.SkillKey = s.SkillKey AND {MemoryAgentCondition}
                       WHERE s.MessagesJson <> N'[]' AND s.MemoryOptOut = 0 AND s.ExtractRetryCount < {MaxRetries}
                         AND s.UpdatedAt < DATEADD(MINUTE, -@Idle, GETUTCDATE())
                         AND (s.ExtractedAt IS NULL OR s.UpdatedAt > s.ExtractedAt)
                         AND (s.ExtractingAt IS NULL OR s.ExtractingAt < DATEADD(MINUTE, -{ClaimTimeoutMinutes}, GETUTCDATE()))
                       ORDER BY s.UpdatedAt",
                    AgentMemoryDb.Params(fixture, ("@Idle", idleMinutes)));
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new MemoryChatCandidate
                    {
                        SessionKey         = row["SessionKey"]?.ToString() ?? "",
                        SkillKey           = row["SkillKey"]?.ToString() ?? "",
                        UserId             = Convert.ToInt32(row["UserId"]),
                        MessagesJson       = row["MessagesJson"]?.ToString() ?? "[]",
                        ExtractedTurnCount = Convert.ToInt32(row["ExtractedTurnCount"])
                    });
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(FindReady));
            }
            return list;
        }

        /// <summary>True when this worker now owns the chat.</summary>
        public static bool TryClaim(int dataSourceId, string sessionKey) =>
            Exec(dataSourceId,
                $@"UPDATE dbo.AppGenericAgentSession SET ExtractingAt = GETUTCDATE()
                   WHERE SessionKey=@K AND (ExtractingAt IS NULL OR ExtractingAt < DATEADD(MINUTE, -{ClaimTimeoutMinutes}, GETUTCDATE()))",
                ("@K", sessionKey)) > 0;

        public static void Release(int dataSourceId, string sessionKey) =>
            Exec(dataSourceId, "UPDATE dbo.AppGenericAgentSession SET ExtractingAt = NULL WHERE SessionKey=@K", ("@K", sessionKey));

        public static void MarkDone(int dataSourceId, string sessionKey, int turnCount) =>
            Exec(dataSourceId,
                @"UPDATE dbo.AppGenericAgentSession
                  SET ExtractedAt = GETUTCDATE(), ExtractedTurnCount = @N, ExtractingAt = NULL, ExtractRetryCount = 0
                  WHERE SessionKey=@K",
                ("@N", turnCount), ("@K", sessionKey));

        /// <summary>
        /// Counts a failure. After <see cref="MaxRetries"/> the chat is marked extracted so it stops looping;
        /// new activity makes it eligible again.
        /// </summary>
        public static void MarkFailed(int dataSourceId, string sessionKey) =>
            Exec(dataSourceId,
                $@"UPDATE dbo.AppGenericAgentSession
                   SET ExtractingAt = NULL,
                       ExtractRetryCount = CASE WHEN ExtractRetryCount + 1 >= {MaxRetries} THEN 0 ELSE ExtractRetryCount + 1 END,
                       ExtractedAt = CASE WHEN ExtractRetryCount + 1 >= {MaxRetries} THEN GETUTCDATE() ELSE ExtractedAt END
                   WHERE SessionKey=@K",
                ("@K", sessionKey));

        public static MemorySessionState GetState(int dataSourceId, string sessionKey, string skillKey, int userId)
        {
            try
            {
                var fixture = AgentMemoryDb.Fixture(dataSourceId);
                if (fixture == null) return new MemorySessionState();
                var dt = fixture.RetriveDataTable(
                    "SELECT ExtractedTurnCount, MemoryOptOut FROM dbo.AppGenericAgentSession WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    AgentMemoryDb.Params(fixture, ("@K", sessionKey), ("@S", skillKey), ("@U", userId)));
                if (dt == null || dt.Rows.Count == 0) return new MemorySessionState();
                return new MemorySessionState
                {
                    ExtractedTurnCount = Convert.ToInt32(dt.Rows[0]["ExtractedTurnCount"]),
                    OptOut             = Convert.ToBoolean(dt.Rows[0]["MemoryOptOut"])
                };
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(GetState));
                return new MemorySessionState();
            }
        }

        public static bool SetOptOut(int dataSourceId, string sessionKey, string skillKey, int userId, bool optOut) =>
            Exec(dataSourceId,
                "UPDATE dbo.AppGenericAgentSession SET MemoryOptOut=@O WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                ("@O", optOut), ("@K", sessionKey), ("@S", skillKey), ("@U", userId)) > 0;

        /// <summary>The transcript was wiped (Clear conversation): forget how much of it had been processed.</summary>
        public static void ResetExtraction(int dataSourceId, string sessionKey, string skillKey, int userId) =>
            Exec(dataSourceId,
                @"UPDATE dbo.AppGenericAgentSession
                  SET ExtractedAt = NULL, ExtractedTurnCount = 0, ExtractingAt = NULL, ExtractRetryCount = 0
                  WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                ("@K", sessionKey), ("@S", skillKey), ("@U", userId));

        public static bool SkillUsesMemory(int dataSourceId, string skillKey)
        {
            try
            {
                var fixture = AgentMemoryDb.Fixture(dataSourceId);
                if (fixture == null) return false;
                var dt = fixture.RetriveDataTable(
                    $"SELECT k.SkillKey FROM dbo.AppAgentSkillSet k WHERE k.SkillKey=@S AND {MemoryAgentCondition}",
                    AgentMemoryDb.Params(fixture, ("@S", skillKey)));
                return dt != null && dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(SkillUsesMemory));
                return false;
            }
        }

        private static int Exec(int dataSourceId, string sql, params (string Name, object Value)[] ps)
        {
            try
            {
                var fixture = AgentMemoryDb.Fixture(dataSourceId);
                return fixture == null ? 0 : fixture.ExecuteNonQueryResult(sql, AgentMemoryDb.Params(fixture, ps));
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "AgentMemorySessionBL.Exec");
                return 0;
            }
        }
    }
}
