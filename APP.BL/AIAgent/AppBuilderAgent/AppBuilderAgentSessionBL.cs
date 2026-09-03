using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent
{
    // ─────────────────────────────────────────────────────────────────────────
    // Lightweight DTO — no external dependency, lives in the BL assembly
    // ─────────────────────────────────────────────────────────────────────────

    public class AgentSessionRecord
    {
        public string   SessionGuid             { get; set; }
        public DateTime CreatedAt               { get; set; }
        public DateTime UpdatedAt               { get; set; }
        public string   UserRequest             { get; set; }
        /// <summary>"InProgress" | "Completed" | "Failed"</summary>
        public string   Status                  { get; set; }
        public string   CurrentStep             { get; set; }
        public int?     CreatedById             { get; set; }
        public AgentCheckpointDto Checkpoint    { get; set; }
        public List<AppBuilderAgentMessageDto> ConversationHistory { get; set; }
        public string   FinalResponse           { get; set; }
    }

    /// <summary>
    /// DB-backed persistence for AppBuilder AI Agent sessions.
    ///
    /// Stores each agent run in the master database so sessions survive server
    /// restarts, can be resumed after interruption, and are visible in build history.
    ///
    /// Table is auto-created on first use — no migration script required.
    ///
    /// All methods swallow exceptions so a DB failure never crashes the agent.
    /// </summary>
    public static class AppBuilderAgentSessionBL
    {
        // ── Table DDL ────────────────────────────────────────────────────────

        private const string CreateTableSql = @"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppBuilderAgentSession'
)
BEGIN
    CREATE TABLE dbo.AppBuilderAgentSession (
        SessionGuid             NVARCHAR(50)   NOT NULL,
        CreatedAt               DATETIME       NOT NULL,
        UpdatedAt               DATETIME       NOT NULL,
        UserRequest             NVARCHAR(2000) NULL,
        Status                  NVARCHAR(20)   NOT NULL,
        CurrentStep             NVARCHAR(200)  NULL,
        CreatedById             INT            NULL,
        CheckpointJson          NVARCHAR(MAX)  NULL,
        ConversationHistoryJson NVARCHAR(MAX)  NULL,
        FinalResponse           NVARCHAR(4000) NULL,
        CONSTRAINT PK_AppBuilderAgentSession PRIMARY KEY (SessionGuid)
    )
END";

        // Migration: add CreatedById to existing tables that pre-date this column
        private const string MigrateCreatedByIdSql = @"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppBuilderAgentSession' AND COLUMN_NAME = 'CreatedById'
)
BEGIN
    ALTER TABLE dbo.AppBuilderAgentSession ADD CreatedById INT NULL
END";

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Insert a new session record as "InProgress".</summary>
        public static void SaveSession(string sessionGuid, string userRequest, int? createdById = null)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return;

                var now = DateTime.UtcNow;
                const string sql = @"
INSERT INTO dbo.AppBuilderAgentSession
    (SessionGuid, CreatedAt, UpdatedAt, UserRequest, Status, CreatedById)
VALUES
    (@SessionGuid, @CreatedAt, @UpdatedAt, @UserRequest, 'InProgress', @CreatedById)";

                var p1 = fixture.CreateParameter("@SessionGuid");  p1.Value = sessionGuid;
                var p2 = fixture.CreateParameter("@CreatedAt");    p2.Value = now;
                var p3 = fixture.CreateParameter("@UpdatedAt");    p3.Value = now;
                var p4 = fixture.CreateParameter("@UserRequest");  p4.Value = Truncate(userRequest, 2000) ?? (object)DBNull.Value;
                var p5 = fixture.CreateParameter("@CreatedById");  p5.Value = createdById.HasValue ? (object)createdById.Value : DBNull.Value;

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter> { p1, p2, p3, p4, p5 });
            }
            catch { /* never crash the agent */ }
        }

        /// <summary>Update status, checkpoint, conversation history, and final response.</summary>
        public static void UpdateSession(
            string sessionGuid,
            string status,
            AgentCheckpointDto checkpoint,
            List<AppBuilderAgentMessageDto> conversationHistory,
            string finalResponse,
            string currentStep = null)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return;

                var checkpointJson  = checkpoint           != null ? JsonConvert.SerializeObject(checkpoint)           : null;
                var conversationJson = conversationHistory != null ? JsonConvert.SerializeObject(conversationHistory) : null;

                const string sql = @"
UPDATE dbo.AppBuilderAgentSession SET
    UpdatedAt               = @UpdatedAt,
    Status                  = @Status,
    CurrentStep             = @CurrentStep,
    CheckpointJson          = @CheckpointJson,
    ConversationHistoryJson = @ConversationHistoryJson,
    FinalResponse           = @FinalResponse
WHERE SessionGuid = @SessionGuid";

                var p1 = fixture.CreateParameter("@UpdatedAt");               p1.Value = DateTime.UtcNow;
                var p2 = fixture.CreateParameter("@Status");                  p2.Value = status ?? "InProgress";
                var p3 = fixture.CreateParameter("@CurrentStep");             p3.Value = (object)currentStep ?? DBNull.Value;
                var p4 = fixture.CreateParameter("@CheckpointJson");          p4.Value = (object)checkpointJson ?? DBNull.Value;
                var p5 = fixture.CreateParameter("@ConversationHistoryJson"); p5.Value = (object)conversationJson ?? DBNull.Value;
                var p6 = fixture.CreateParameter("@FinalResponse");           p6.Value = (object)Truncate(finalResponse, 4000) ?? DBNull.Value;
                var p7 = fixture.CreateParameter("@SessionGuid");             p7.Value = sessionGuid;

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter> { p1, p2, p3, p4, p5, p6, p7 });
            }
            catch { }
        }

        /// <summary>
        /// Return the N most recent sessions for a specific user (summary — no full conversation history).
        /// Used by GET /RecentSessions to populate build history in the React UI.
        /// </summary>
        public static List<AgentSessionRecord> GetRecentSessions(int limit = 20, int? createdById = null)
        {
            var list = new List<AgentSessionRecord>();
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return list;

                EnsureTable(fixture);

                var p1 = fixture.CreateParameter("@Limit");       p1.Value = limit;
                var p2 = fixture.CreateParameter("@CreatedById"); p2.Value = createdById.HasValue ? (object)createdById.Value : DBNull.Value;

                var dt = fixture.RetriveDataTable(
                    @"SELECT TOP (@Limit) SessionGuid, CreatedAt, UpdatedAt,
                        UserRequest, Status, CurrentStep, CreatedById, CheckpointJson, FinalResponse
                      FROM dbo.AppBuilderAgentSession
                      WHERE (@CreatedById IS NULL OR CreatedById = @CreatedById)
                      ORDER BY UpdatedAt DESC",
                    new List<DbParameter> { p1, p2 });

                if (dt == null) return list;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    AgentCheckpointDto cp = null;
                    var cpJson = row["CheckpointJson"] as string;
                    if (!string.IsNullOrWhiteSpace(cpJson))
                        try { cp = JsonConvert.DeserializeObject<AgentCheckpointDto>(cpJson); } catch { }

                    list.Add(new AgentSessionRecord
                    {
                        SessionGuid   = row["SessionGuid"]  as string,
                        CreatedAt     = row["CreatedAt"]    is DateTime c  ? c  : DateTime.MinValue,
                        UpdatedAt     = row["UpdatedAt"]    is DateTime u  ? u  : DateTime.MinValue,
                        UserRequest   = row["UserRequest"]  as string,
                        Status        = row["Status"]       as string,
                        CurrentStep   = row["CurrentStep"]  as string,
                        CreatedById   = row["CreatedById"]  is int cb ? cb : (int?)null,
                        FinalResponse = row["FinalResponse"] as string,
                        Checkpoint    = cp
                    });
                }
            }
            catch { }
            return list;
        }

        /// <summary>
        /// Return a single session including full conversation history (for resume).
        /// </summary>
        public static AgentSessionRecord GetSession(string sessionGuid)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return null;

                var p = fixture.CreateParameter("@SessionGuid"); p.Value = sessionGuid;
                var dt = fixture.RetriveDataTable(
                    @"SELECT SessionGuid, CreatedAt, UpdatedAt, UserRequest, Status, CurrentStep,
                        CreatedById, CheckpointJson, ConversationHistoryJson, FinalResponse
                      FROM dbo.AppBuilderAgentSession
                      WHERE SessionGuid = @SessionGuid",
                    new List<DbParameter> { p });

                if (dt == null || dt.Rows.Count == 0) return null;

                var row = dt.Rows[0];

                AgentCheckpointDto cp = null;
                var cpJson = row["CheckpointJson"] as string;
                if (!string.IsNullOrWhiteSpace(cpJson))
                    try { cp = JsonConvert.DeserializeObject<AgentCheckpointDto>(cpJson); } catch { }

                List<AppBuilderAgentMessageDto> history = null;
                var histJson = row["ConversationHistoryJson"] as string;
                if (!string.IsNullOrWhiteSpace(histJson))
                    try { history = JsonConvert.DeserializeObject<List<AppBuilderAgentMessageDto>>(histJson); } catch { }

                return new AgentSessionRecord
                {
                    SessionGuid         = row["SessionGuid"]  as string,
                    CreatedAt           = row["CreatedAt"]    is DateTime c  ? c  : DateTime.MinValue,
                    UpdatedAt           = row["UpdatedAt"]    is DateTime u  ? u  : DateTime.MinValue,
                    UserRequest         = row["UserRequest"]  as string,
                    Status              = row["Status"]       as string,
                    CurrentStep         = row["CurrentStep"]  as string,
                    CreatedById         = row["CreatedById"]  is int cb ? cb : (int?)null,
                    FinalResponse       = row["FinalResponse"] as string,
                    Checkpoint          = cp,
                    ConversationHistory = history
                };
            }
            catch { return null; }
        }

        /// <summary>Delete a session record by GUID. Returns true if a row was deleted.</summary>
        public static bool DeleteSession(string sessionGuid)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return false;

                var p = fixture.CreateParameter("@SessionGuid"); p.Value = sessionGuid;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppBuilderAgentSession WHERE SessionGuid = @SessionGuid",
                    new List<DbParameter> { p });
                return true;
            }
            catch { return false; }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static void EnsureTable(DatabaseFixture fixture)
        {
            try
            {
                if (fixture == null) return;
                fixture.ExecuteNonQueryResult(CreateTableSql, new List<DbParameter>());
                fixture.ExecuteNonQueryResult(MigrateCreatedByIdSql, new List<DbParameter>());
            }
            catch { }
        }

        private static DatabaseFixture GetFixture()
        {
            try
            {
                var dsId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                if (dsId == null) return null;
                return AppCacheManagerBL.GetOneDatabaseFixture(dsId.Value);
            }
            catch { return null; }
        }

        private static string Truncate(string s, int max) =>
            s == null ? null : s.Length > max ? s.Substring(0, max) : s;
    }
}
