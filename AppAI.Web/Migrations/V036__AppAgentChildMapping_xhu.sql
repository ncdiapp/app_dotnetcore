-- merge file
-- V036: Auth settings for AppAgentMcpServer (streamable-http).
--   BearerTokenEnvVar : NAME of an env var whose value is sent as "Authorization: Bearer <value>"
--   Headers           : JSON object of static headers            {"HeaderName":"value"}
--   HeadersFromEnv    : JSON object of header -> env var name    {"HeaderName":"ENV_VAR_NAME"}

IF COL_LENGTH(N'dbo.AppAgentMcpServer', N'BearerTokenEnvVar') IS NULL
    ALTER TABLE dbo.AppAgentMcpServer ADD BearerTokenEnvVar NVARCHAR(200) NULL;
GO

IF COL_LENGTH(N'dbo.AppAgentMcpServer', N'Headers') IS NULL
    ALTER TABLE dbo.AppAgentMcpServer ADD Headers NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH(N'dbo.AppAgentMcpServer', N'HeadersFromEnv') IS NULL
    ALTER TABLE dbo.AppAgentMcpServer ADD HeadersFromEnv NVARCHAR(MAX) NULL;
GO

-- V036: Orchestrator → Child-Agent mapping (many-to-many; no Parent FK on Child).
-- Any agent may be a Child-Agent of multiple Orchestrators.
-- Idempotent: safe to re-run.
--
-- Tracking note (AppTenantMigrationRunnerBL): Version = full filename without .sql
-- (e.g. V036__AppAgentChildMapping_xhu).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppAgentChildMapping' AND type = N'U')
BEGIN
    CREATE TABLE dbo.AppAgentChildMapping (
        ParentSkillKey NVARCHAR(100) NOT NULL,
        ChildSkillKey  NVARCHAR(100) NOT NULL,
        SortOrder      INT           NOT NULL CONSTRAINT DF_AppAgentChildMapping_SortOrder DEFAULT (0),
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_AppAgentChildMapping_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt      DATETIME2     NOT NULL CONSTRAINT DF_AppAgentChildMapping_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AppAgentChildMapping PRIMARY KEY (ParentSkillKey, ChildSkillKey),
        CONSTRAINT CK_AppAgentChildMapping_NotSelf CHECK (ParentSkillKey <> ChildSkillKey),
        CONSTRAINT FK_AppAgentChildMapping_Parent
            FOREIGN KEY (ParentSkillKey) REFERENCES dbo.AppAgentSkillSet (SkillKey),
        CONSTRAINT FK_AppAgentChildMapping_Child
            FOREIGN KEY (ChildSkillKey) REFERENCES dbo.AppAgentSkillSet (SkillKey)
    );

    CREATE INDEX IX_AppAgentChildMapping_ChildSkillKey
        ON dbo.AppAgentChildMapping (ChildSkillKey);
END
GO
