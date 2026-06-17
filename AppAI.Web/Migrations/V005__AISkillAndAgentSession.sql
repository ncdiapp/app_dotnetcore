-- ============================================================
-- V005: Add AI Skill and Agent Session tables to tenant DB
--
-- AppAISkill, AppAISkillRef, AppBuilderAgentSession move from
-- AppMasterDB to each tenant DB so skills and agent history are
-- scoped per tenant.
--
-- BL change: AppAISkillBL.GetDefaultDataSourceId() and
-- AppBuilderAgentSessionBL.GetFixture() now use
-- AppDataSourceRegisterBL.GetDefaultDataSourceRegId()
-- (ServerContext.DataSourceId) instead of MasterDataSourceRegisterId.
-- ============================================================

-- ── AppAISkill ───────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppAISkill'
)
BEGIN
    CREATE TABLE dbo.AppAISkill (
        SkillId       INT            NOT NULL IDENTITY(1,1),
        Name          NVARCHAR(200)  NOT NULL,
        Description   NVARCHAR(500)  NULL,
        SkillContent  NVARCHAR(MAX)  NULL,
        IsActive      BIT            NOT NULL CONSTRAINT DF_AppAISkill_IsActive DEFAULT (1),
        CreatedDate   DATETIME       NOT NULL CONSTRAINT DF_AppAISkill_CreatedDate DEFAULT (GETDATE()),
        UpdatedDate   DATETIME       NULL,
        CONSTRAINT PK_AppAISkill PRIMARY KEY (SkillId)
    );

    CREATE UNIQUE INDEX UX_AppAISkill_Name ON dbo.AppAISkill (Name);
END
GO

-- ── AppAISkillRef ────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppAISkillRef'
)
BEGIN
    CREATE TABLE dbo.AppAISkillRef (
        RefId         INT            NOT NULL IDENTITY(1,1),
        SkillId       INT            NOT NULL,
        FileName      NVARCHAR(500)  NOT NULL,
        FileContent   NVARCHAR(MAX)  NULL,
        SortOrder     INT            NOT NULL CONSTRAINT DF_AppAISkillRef_SortOrder DEFAULT (0),
        CreatedDate   DATETIME       NOT NULL CONSTRAINT DF_AppAISkillRef_CreatedDate DEFAULT (GETDATE()),
        CONSTRAINT PK_AppAISkillRef PRIMARY KEY (RefId),
        CONSTRAINT FK_AppAISkillRef_AppAISkill FOREIGN KEY (SkillId)
            REFERENCES dbo.AppAISkill (SkillId) ON DELETE CASCADE
    );
END
GO

-- ── AppBuilderAgentSession ───────────────────────────────────
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
    );
END
GO

-- ── Migrate existing rows from AppMasterDB (run once per tenant if needed) ──
-- If AppAISkill rows exist in master and should be seeded into this tenant DB,
-- use a linked server or SSMS import. No automatic data migration here.
-- ============================================================
