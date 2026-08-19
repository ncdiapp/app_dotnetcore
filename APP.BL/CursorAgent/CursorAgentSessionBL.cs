using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.CursorAgent
{
    public static class CursorAgentSessionBL
    {
        private const string CreateTableSql = @"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CursorAgentSession'
)
BEGIN
    CREATE TABLE dbo.CursorAgentSession (
        SessionGuid             NVARCHAR(50)   NOT NULL,
        CreatedAt               DATETIME       NOT NULL,
        UpdatedAt               DATETIME       NOT NULL,
        UserRequest             NVARCHAR(2000) NULL,
        Status                  NVARCHAR(20)   NOT NULL,
        CursorAgentId           NVARCHAR(80)   NULL,
        LatestRunId             NVARCHAR(80)   NULL,
        McpToken                NVARCHAR(80)   NULL,
        AppSessionId            NVARCHAR(100)  NULL,
        SaasApplicationId       INT            NULL,
        DataSourceRegisterId    INT            NULL,
        CreatedById             INT            NULL,
        WorkspaceRelativePath   NVARCHAR(400)  NULL,
        ConversationHistoryJson NVARCHAR(MAX)  NULL,
        IdentityJson            NVARCHAR(MAX)  NULL,
        PendingGateJson         NVARCHAR(MAX)  NULL,
        FinalResponse           NVARCHAR(4000) NULL,
        CONSTRAINT PK_CursorAgentSession PRIMARY KEY (SessionGuid)
    )
END";

        private const string MigrateIdentityJsonSql = @"
IF COL_LENGTH('dbo.CursorAgentSession', 'IdentityJson') IS NULL
    ALTER TABLE dbo.CursorAgentSession ADD IdentityJson NVARCHAR(MAX) NULL";

        private const string MigrateSkillKeySql = @"
IF COL_LENGTH('dbo.CursorAgentSession', 'SkillKey') IS NULL
    ALTER TABLE dbo.CursorAgentSession ADD SkillKey NVARCHAR(80) NULL";

        private static void EnsureSchema(DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            fixture.ExecuteNonQueryResult(CreateTableSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(MigrateIdentityJsonSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(MigrateSkillKeySql, new List<DbParameter>());
        }

        public static void SaveNew(CursorAgentSessionStore.SessionData session, string userRequest)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null || session == null) return;
                EnsureSchema(fixture);

                const string sql = @"
INSERT INTO dbo.CursorAgentSession
    (SessionGuid, CreatedAt, UpdatedAt, UserRequest, Status, CursorAgentId, LatestRunId, McpToken,
     AppSessionId, SaasApplicationId, DataSourceRegisterId, CreatedById, WorkspaceRelativePath,
     ConversationHistoryJson, IdentityJson, SkillKey)
VALUES
    (@SessionGuid, @CreatedAt, @UpdatedAt, @UserRequest, 'InProgress', @CursorAgentId, @LatestRunId, @McpToken,
     @AppSessionId, @SaasApplicationId, @DataSourceRegisterId, @CreatedById, @WorkspaceRelativePath,
     @ConversationHistoryJson, @IdentityJson, @SkillKey)";

                var now = DateTime.UtcNow;
                fixture.ExecuteNonQueryResult(sql, new List<DbParameter>
                {
                    P(fixture, "@SessionGuid", session.SessionId),
                    P(fixture, "@CreatedAt", now),
                    P(fixture, "@UpdatedAt", now),
                    P(fixture, "@UserRequest", Trunc(userRequest, 2000)),
                    P(fixture, "@CursorAgentId", session.CursorAgentId),
                    P(fixture, "@LatestRunId", session.LatestRunId),
                    P(fixture, "@McpToken", session.McpToken),
                    P(fixture, "@AppSessionId", session.AppSessionId),
                    P(fixture, "@SaasApplicationId", session.SaasApplicationId),
                    P(fixture, "@DataSourceRegisterId", session.DataSourceRegisterId),
                    P(fixture, "@CreatedById", session.CreatedById),
                    P(fixture, "@WorkspaceRelativePath", session.WorkspaceRelativePath),
                    P(fixture, "@ConversationHistoryJson", JsonConvert.SerializeObject(session.ConversationHistory ?? new List<CursorAgentMessageDto>())),
                    P(fixture, "@IdentityJson", session.IdentityJson ?? CursorAgentIdentity.Serialize(session.Identity)),
                    P(fixture, "@SkillKey", session.SkillKey)
                });
            }
            catch { }
        }

        public static void Update(CursorAgentSessionStore.SessionData session, string status, string finalResponse, CursorAgentGateEvent pendingGate)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null || session == null) return;
                EnsureSchema(fixture);

                const string sql = @"
UPDATE dbo.CursorAgentSession SET
    UpdatedAt = @UpdatedAt,
    Status = @Status,
    CursorAgentId = @CursorAgentId,
    LatestRunId = @LatestRunId,
    McpToken = @McpToken,
    SaasApplicationId = @SaasApplicationId,
    DataSourceRegisterId = @DataSourceRegisterId,
    SkillKey = @SkillKey,
    WorkspaceRelativePath = @WorkspaceRelativePath,
    ConversationHistoryJson = @ConversationHistoryJson,
    IdentityJson = @IdentityJson,
    PendingGateJson = @PendingGateJson,
    FinalResponse = @FinalResponse
WHERE SessionGuid = @SessionGuid";

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter>
                {
                    P(fixture, "@UpdatedAt", DateTime.UtcNow),
                    P(fixture, "@Status", status ?? "InProgress"),
                    P(fixture, "@CursorAgentId", session.CursorAgentId),
                    P(fixture, "@LatestRunId", session.LatestRunId),
                    P(fixture, "@McpToken", session.McpToken),
                    P(fixture, "@SaasApplicationId", session.SaasApplicationId),
                    P(fixture, "@DataSourceRegisterId", session.DataSourceRegisterId),
                    P(fixture, "@SkillKey", session.SkillKey),
                    P(fixture, "@WorkspaceRelativePath", session.WorkspaceRelativePath),
                    P(fixture, "@ConversationHistoryJson", JsonConvert.SerializeObject(session.ConversationHistory ?? new List<CursorAgentMessageDto>())),
                    P(fixture, "@IdentityJson", session.IdentityJson ?? CursorAgentIdentity.Serialize(session.Identity)),
                    P(fixture, "@PendingGateJson", pendingGate == null ? null : JsonConvert.SerializeObject(pendingGate)),
                    P(fixture, "@FinalResponse", Trunc(finalResponse, 4000)),
                    P(fixture, "@SessionGuid", session.SessionId)
                });
            }
            catch { }
        }

        public static List<CursorAgentSessionSummaryDto> ListRecent(int limit, int? createdById)
        {
            var list = new List<CursorAgentSessionSummaryDto>();
            try
            {
                var fixture = GetFixture();
                if (fixture == null) return list;
                EnsureSchema(fixture);

                var take = limit <= 0 ? 30 : Math.Min(limit, 100);
                var dt = fixture.RetriveDataTable(@"
SELECT TOP (@Take) SessionGuid, CreatedAt, UpdatedAt, UserRequest, Status, CursorAgentId,
       SaasApplicationId, DataSourceRegisterId, SkillKey, WorkspaceRelativePath, FinalResponse
FROM dbo.CursorAgentSession
WHERE (@CreatedById IS NULL OR CreatedById = @CreatedById)
ORDER BY UpdatedAt DESC",
                    new List<DbParameter>
                    {
                        P(fixture, "@Take", take),
                        P(fixture, "@CreatedById", createdById)
                    });
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new CursorAgentSessionSummaryDto
                    {
                        SessionGuid = row["SessionGuid"] as string,
                        CreatedAt = row["CreatedAt"] is DateTime c ? c : DateTime.MinValue,
                        UpdatedAt = row["UpdatedAt"] is DateTime u ? u : DateTime.MinValue,
                        UserRequest = row["UserRequest"] as string,
                        Status = row["Status"] as string,
                        CursorAgentId = row["CursorAgentId"] as string,
                        SaasApplicationId = row["SaasApplicationId"] is int sa ? sa : (int?)null,
                        DataSourceRegisterId = row["DataSourceRegisterId"] is int ds ? ds : (int?)null,
                        SkillKey = ColStr(row, "SkillKey"),
                        WorkspaceRelativePath = row["WorkspaceRelativePath"] as string,
                        FinalResponse = row["FinalResponse"] as string
                    });
                }
            }
            catch { }
            return list;
        }

        public static CursorAgentSessionFullDto Get(string sessionGuid)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null || string.IsNullOrWhiteSpace(sessionGuid)) return null;
                EnsureSchema(fixture);

                var dt = fixture.RetriveDataTable(@"
SELECT SessionGuid, CreatedAt, UpdatedAt, UserRequest, Status, CursorAgentId, LatestRunId,
       SaasApplicationId, DataSourceRegisterId, SkillKey, WorkspaceRelativePath,
       ConversationHistoryJson, PendingGateJson, FinalResponse
FROM dbo.CursorAgentSession WHERE SessionGuid = @SessionGuid",
                    new List<DbParameter> { P(fixture, "@SessionGuid", sessionGuid) });
                if (dt == null || dt.Rows.Count == 0) return null;
                var row = dt.Rows[0];
                var full = new CursorAgentSessionFullDto
                {
                    SessionGuid = row["SessionGuid"] as string,
                    CreatedAt = row["CreatedAt"] is DateTime c ? c : DateTime.MinValue,
                    UpdatedAt = row["UpdatedAt"] is DateTime u ? u : DateTime.MinValue,
                    UserRequest = row["UserRequest"] as string,
                    Status = row["Status"] as string,
                    CursorAgentId = row["CursorAgentId"] as string,
                    LatestRunId = row["LatestRunId"] as string,
                    SaasApplicationId = row["SaasApplicationId"] is int sa ? sa : (int?)null,
                    DataSourceRegisterId = row["DataSourceRegisterId"] is int ds ? ds : (int?)null,
                    SkillKey = ColStr(row, "SkillKey"),
                    WorkspaceRelativePath = row["WorkspaceRelativePath"] as string,
                    FinalResponse = row["FinalResponse"] as string,
                    PendingGateJson = row["PendingGateJson"] as string
                };
                var hist = row["ConversationHistoryJson"] as string;
                if (!string.IsNullOrWhiteSpace(hist))
                    full.ConversationHistory = JsonConvert.DeserializeObject<List<CursorAgentMessageDto>>(hist);
                return full;
            }
            catch { return null; }
        }

        public static CursorAgentSessionStore.SessionData HydrateLive(string sessionGuid)
        {
            try
            {
                var fixture = GetFixture();
                if (fixture == null || string.IsNullOrWhiteSpace(sessionGuid)) return null;
                EnsureSchema(fixture);

                var dt = fixture.RetriveDataTable(@"
SELECT SessionGuid, CursorAgentId, LatestRunId, McpToken, AppSessionId, SaasApplicationId,
       DataSourceRegisterId, SkillKey, CreatedById, WorkspaceRelativePath, ConversationHistoryJson, IdentityJson
FROM dbo.CursorAgentSession WHERE SessionGuid = @SessionGuid",
                    new List<DbParameter> { P(fixture, "@SessionGuid", sessionGuid) });
                if (dt == null || dt.Rows.Count == 0) return null;
                var row = dt.Rows[0];
                var data = new CursorAgentSessionStore.SessionData
                {
                    SessionId = row["SessionGuid"] as string,
                    CursorAgentId = row["CursorAgentId"] as string,
                    LatestRunId = row["LatestRunId"] as string,
                    McpToken = row["McpToken"] as string,
                    AppSessionId = row["AppSessionId"] as string,
                    SaasApplicationId = row["SaasApplicationId"] is int sa ? sa : (int?)null,
                    DataSourceRegisterId = row["DataSourceRegisterId"] is int ds ? ds : (int?)null,
                    SkillKey = ColStr(row, "SkillKey"),
                    CreatedById = row["CreatedById"] is int cb ? cb : (int?)null,
                    WorkspaceRelativePath = row["WorkspaceRelativePath"] as string,
                    IdentityJson = row.Table.Columns.Contains("IdentityJson") ? row["IdentityJson"] as string : null
                };
                data.Identity = CursorAgentIdentity.Deserialize(data.IdentityJson);
                CursorAgentSkillCatalogBL.ApplyToSession(data, data.SkillKey);
                if (data.CompanyId == null && data.Identity.HasValue && data.Identity.Value.CurrentWorkingCompanyId != null)
                    data.CompanyId = Convert.ToInt32(data.Identity.Value.CurrentWorkingCompanyId);
                var hist = row["ConversationHistoryJson"] as string;
                if (!string.IsNullOrWhiteSpace(hist))
                    data.ConversationHistory = JsonConvert.DeserializeObject<List<CursorAgentMessageDto>>(hist)
                        ?? new List<CursorAgentMessageDto>();
                CursorAgentSessionStore.AttachLive(data);
                return data;
            }
            catch { return null; }
        }

        private static DbParameter P(DatabaseSchemaMrg.DatabaseFixture fixture, string name, object value)
        {
            var p = fixture.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        private static DatabaseSchemaMrg.DatabaseFixture GetFixture()
        {
            var id = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (!id.HasValue) return null;
            return AppCacheManagerBL.GetOneDatabaseFixture(id.Value);
        }

        private static string Trunc(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        private static string ColStr(DataRow row, string column)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column)) return null;
            var v = row[column];
            return v == null || v == DBNull.Value ? null : v.ToString();
        }
    }
}
