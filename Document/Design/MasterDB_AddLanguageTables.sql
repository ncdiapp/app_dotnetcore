-- ============================================================
-- MasterDB_AddLanguageTables.sql
-- Run ONCE against AppMasterDB via SSMS.
-- Purpose: Add AppLanguage, AppLanguageKey, AppSysLabelLanguage
--          to AppMasterDB so SysAdmin can use the multi-language
--          feature.  Tenant DBs already have these tables via
--          V001__InitialTenantSchema.sql.
--
-- NOTE: AppSysLabelLanguage FK to AppListMenu is intentionally
--       omitted here — AppListMenu does not exist in AppMasterDB.
-- ============================================================

USE AppMasterDB;
GO

-- ── AppLanguage ──────────────────────────────────────────────
IF OBJECT_ID(N'dbo.AppLanguage', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppLanguage] (
        [LanguageId]            INT            IDENTITY(1,1) NOT NULL,
        [Name]                  NVARCHAR(50)   NOT NULL,
        [Description]           NVARCHAR(50)   NULL,
        [IsDefault]             BIT            NOT NULL CONSTRAINT DF_AppLanguage_IsDefault DEFAULT (0),
        [DefaultCultureInfoCode] NVARCHAR(50)  NULL,
        [SystemTimeStamp]       ROWVERSION     NULL,
        [AppCreatedById]        INT            NULL,
        [AppCreatedDate]        DATETIME       NULL,
        [AppModifiedDate]       DATETIME       NULL,
        [AppModifiedById]       INT            NULL,
        [AppCreatedByCompanyId] INT            NULL,
        CONSTRAINT [PK_AppLanguage] PRIMARY KEY CLUSTERED ([LanguageId] ASC)
    );
    PRINT 'Created AppLanguage';
END
ELSE
    PRINT 'AppLanguage already exists — skipped';
GO

-- ── AppLanguageKey ───────────────────────────────────────────
IF OBJECT_ID(N'dbo.AppLanguageKey', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppLanguageKey] (
        [LanguageKeyId]         INT             IDENTITY(1,1) NOT NULL,
        [ResourceKey]           NVARCHAR(400)   NOT NULL,
        [LanguageId]            INT             NOT NULL,
        [ApplicationId]         INT             NULL,
        [Value]                 NVARCHAR(1000)  NOT NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_AppLanguageKey] PRIMARY KEY CLUSTERED ([LanguageKeyId] ASC),
        CONSTRAINT [FK_AppLanguageKey_AppLanguage]
            FOREIGN KEY ([LanguageId]) REFERENCES [dbo].[AppLanguage] ([LanguageId])
    );
    PRINT 'Created AppLanguageKey';
END
ELSE
    PRINT 'AppLanguageKey already exists — skipped';
GO

-- ── AppSysLabelLanguage ──────────────────────────────────────
-- FK to AppListMenu omitted: AppListMenu is tenant-only.
IF OBJECT_ID(N'dbo.AppSysLabelLanguage', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppSysLabelLanguage] (
        [SysLableLanguageID]             INT            IDENTITY(1,1) NOT NULL,
        [LanguageID]                     INT            NULL,
        [MenuID]                         INT            NULL,
        [TransactionFieldID]             INT            NULL,
        [FormID]                         INT            NULL,
        [LanguageText]                   NVARCHAR(500)  NULL,
        [TransactionUnitLinkedSearchId]  INT            NULL,
        [LinkTargetID]                   INT            NULL,
        [SearchViewFieldID]              INT            NULL,
        [SearchFieldID]                  INT            NULL,
        [TransactionUnitID]              INT            NULL,
        [SearchViewID]                   INT            NULL,
        [SearchID]                       INT            NULL,
        [AppCreatedByID]                 INT            NULL,
        [AppCreatedDate]                 DATETIME       NULL,
        [AppModifiedDate]                DATETIME       NULL,
        [AppModifiedByID]                INT            NULL,
        [AppCreatedByCompanyID]          INT            NULL,
        [ApplicationID]                  INT            NULL,
        CONSTRAINT [PK_AppSysLabelLanguage] PRIMARY KEY CLUSTERED ([SysLableLanguageID] ASC)
    );
    PRINT 'Created AppSysLabelLanguage';
END
ELSE
    PRINT 'AppSysLabelLanguage already exists — skipped';
GO

-- ── Seed: default English language for SysAdmin ─────────────
IF NOT EXISTS (SELECT 1 FROM [dbo].[AppLanguage] WHERE [IsDefault] = 1)
BEGIN
    INSERT INTO [dbo].[AppLanguage]
        ([Name], [Description], [IsDefault], [DefaultCultureInfoCode], [AppCreatedDate])
    VALUES
        ('English', 'English (Default)', 1, 'en-US', GETUTCDATE());
    PRINT 'Seeded default English language';
END
ELSE
    PRINT 'Default language already exists — seed skipped';
GO
