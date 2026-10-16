-- ============================================================
-- MasterDB_OAuthTables.sql
-- Run ONCE against AppMasterDB via SSMS or sqlcmd (idempotent: every table and index is guarded on its own,
-- so re-running repairs a partly applied script).
-- Purpose: storage for the AppAI OAuth 2.1 authorization server (OpenIddict, MCP gateway sign-in for
--          Claude / ChatGPT users). Plan: Document/mcp/mcp-oauth-plan.md (sections 4.4, 7d).
--
-- Why master DB: tokens identify a user across tenants, like AppSecurityUserSession.
-- Why plain tables: no EF Core and no LLBLGen entity; APP.OAuth reads/writes them with parameterized ADO.NET.
-- Columns mirror the OpenIddict entity fields. Arrays/maps are JSON text (NVARCHAR(MAX)).
-- Subject is AppSecurityUser.UserId as text (no FK: OpenIddict treats the subject as an opaque string).
-- CompanyId is copied from the "company_id" property at creation so admins can list/revoke per company.
-- ClientSecret, ReferenceId and Payload hold values OpenIddict has already hashed or protected; the raw
-- access/refresh token string is never stored.
-- Deleting a client cascades to its authorizations and tokens. An authorization -> token link has no cascade
-- (SQL Server allows one cascade path), so APP.OAuth deletes an authorization's tokens first.
-- ============================================================

-- Filtered indexes below require these (sqlcmd defaults QUOTED_IDENTIFIER to OFF; SSMS to ON).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE AppMasterDB;
GO

-- ── AppOAuthApplication (registered OAuth clients: pre-registered and dynamically registered) ──
IF OBJECT_ID(N'dbo.AppOAuthApplication', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppOAuthApplication
    (
        ApplicationId          NVARCHAR(50)   NOT NULL,
        ClientId               NVARCHAR(200)  COLLATE Latin1_General_100_BIN2 NOT NULL,   -- case-sensitive
        ClientSecret           NVARCHAR(MAX)  NULL,
        ApplicationType        NVARCHAR(50)   NULL,
        ClientType             NVARCHAR(50)   NULL,
        ConsentType            NVARCHAR(50)   NULL,
        DisplayName            NVARCHAR(400)  NULL,
        DisplayNames           NVARCHAR(MAX)  NULL,   -- JSON {culture: text}
        JsonWebKeySet          NVARCHAR(MAX)  NULL,
        Permissions            NVARCHAR(MAX)  NULL,   -- JSON array
        PostLogoutRedirectUris NVARCHAR(MAX)  NULL,   -- JSON array
        Properties             NVARCHAR(MAX)  NULL,   -- JSON object
        RedirectUris           NVARCHAR(MAX)  NULL,   -- JSON array
        Requirements           NVARCHAR(MAX)  NULL,   -- JSON array
        Settings               NVARCHAR(MAX)  NULL,   -- JSON object
        CreatedUtc             DATETIME2      NOT NULL CONSTRAINT DF_AppOAuthApplication_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ConcurrencyToken       NVARCHAR(50)   NULL,
        CONSTRAINT PK_AppOAuthApplication PRIMARY KEY (ApplicationId)
    );
    PRINT 'Created AppOAuthApplication';
END
ELSE
    PRINT 'AppOAuthApplication already exists - skipped';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppOAuthApplication_ClientId' AND object_id = OBJECT_ID(N'dbo.AppOAuthApplication'))
    CREATE UNIQUE INDEX UX_AppOAuthApplication_ClientId ON dbo.AppOAuthApplication (ClientId);
GO

-- ── AppOAuthScope ────────────────────────────────────────────
IF OBJECT_ID(N'dbo.AppOAuthScope', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppOAuthScope
    (
        ScopeId          NVARCHAR(50)   NOT NULL,
        Name             NVARCHAR(200)  COLLATE Latin1_General_100_BIN2 NOT NULL,   -- case-sensitive
        Description      NVARCHAR(MAX)  NULL,
        Descriptions     NVARCHAR(MAX)  NULL,         -- JSON {culture: text}
        DisplayName      NVARCHAR(400)  NULL,
        DisplayNames     NVARCHAR(MAX)  NULL,         -- JSON {culture: text}
        Properties       NVARCHAR(MAX)  NULL,         -- JSON object
        Resources        NVARCHAR(MAX)  NULL,         -- JSON array of resource URIs (RFC 8707 audiences)
        ConcurrencyToken NVARCHAR(50)   NULL,
        CONSTRAINT PK_AppOAuthScope PRIMARY KEY (ScopeId)
    );
    PRINT 'Created AppOAuthScope';
END
ELSE
    PRINT 'AppOAuthScope already exists - skipped';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppOAuthScope_Name' AND object_id = OBJECT_ID(N'dbo.AppOAuthScope'))
    CREATE UNIQUE INDEX UX_AppOAuthScope_Name ON dbo.AppOAuthScope (Name);
GO

-- ── AppOAuthAuthorization (a user's consent for a client) ────
IF OBJECT_ID(N'dbo.AppOAuthAuthorization', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppOAuthAuthorization
    (
        AuthorizationId  NVARCHAR(50)   NOT NULL,
        ApplicationId    NVARCHAR(50)   NULL,
        CompanyId        INT            NULL,
        Subject          NVARCHAR(400)  NULL,
        Status           NVARCHAR(50)   NULL,
        Type             NVARCHAR(150)  NULL,         -- OpenIddict token types are URNs (~56 chars)
        Scopes           NVARCHAR(MAX)  NULL,         -- JSON array
        Properties       NVARCHAR(MAX)  NULL,         -- JSON object
        CreationDate     DATETIME2      NULL,
        ConcurrencyToken NVARCHAR(50)   NULL,
        CONSTRAINT PK_AppOAuthAuthorization PRIMARY KEY (AuthorizationId),
        CONSTRAINT FK_AppOAuthAuthorization_Application FOREIGN KEY (ApplicationId)
            REFERENCES dbo.AppOAuthApplication (ApplicationId) ON DELETE CASCADE
    );
    PRINT 'Created AppOAuthAuthorization';
END
ELSE
    PRINT 'AppOAuthAuthorization already exists - skipped';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthAuthorization_Lookup' AND object_id = OBJECT_ID(N'dbo.AppOAuthAuthorization'))
    CREATE INDEX IX_AppOAuthAuthorization_Lookup ON dbo.AppOAuthAuthorization (Subject, ApplicationId, Status, Type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthAuthorization_Application' AND object_id = OBJECT_ID(N'dbo.AppOAuthAuthorization'))
    CREATE INDEX IX_AppOAuthAuthorization_Application ON dbo.AppOAuthAuthorization (ApplicationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthAuthorization_Company' AND object_id = OBJECT_ID(N'dbo.AppOAuthAuthorization'))
    CREATE INDEX IX_AppOAuthAuthorization_Company ON dbo.AppOAuthAuthorization (CompanyId) WHERE CompanyId IS NOT NULL;
GO

-- ── AppOAuthToken (authorization codes, access tokens, refresh tokens) ──
IF OBJECT_ID(N'dbo.AppOAuthToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppOAuthToken
    (
        TokenId          NVARCHAR(50)   NOT NULL,
        ApplicationId    NVARCHAR(50)   NULL,
        AuthorizationId  NVARCHAR(50)   NULL,
        CompanyId        INT            NULL,
        Subject          NVARCHAR(400)  NULL,
        Status           NVARCHAR(50)   NULL,
        Type             NVARCHAR(150)  NULL,         -- OpenIddict token types are URNs (~56 chars)
        ReferenceId      NVARCHAR(100)  COLLATE Latin1_General_100_BIN2 NULL,   -- case-sensitive hashed lookup key of an opaque token
        Payload          NVARCHAR(MAX)  NULL,         -- protected token payload (never the raw token)
        Properties       NVARCHAR(MAX)  NULL,         -- JSON object
        CreationDate     DATETIME2      NULL,
        ExpirationDate   DATETIME2      NULL,
        RedemptionDate   DATETIME2      NULL,
        ConcurrencyToken NVARCHAR(50)   NULL,
        CONSTRAINT PK_AppOAuthToken PRIMARY KEY (TokenId),
        CONSTRAINT FK_AppOAuthToken_Application FOREIGN KEY (ApplicationId)
            REFERENCES dbo.AppOAuthApplication (ApplicationId) ON DELETE CASCADE,
        CONSTRAINT FK_AppOAuthToken_Authorization FOREIGN KEY (AuthorizationId)
            REFERENCES dbo.AppOAuthAuthorization (AuthorizationId)
    );
    PRINT 'Created AppOAuthToken';
END
ELSE
    PRINT 'AppOAuthToken already exists - skipped';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppOAuthToken_ReferenceId' AND object_id = OBJECT_ID(N'dbo.AppOAuthToken'))
    CREATE UNIQUE INDEX UX_AppOAuthToken_ReferenceId ON dbo.AppOAuthToken (ReferenceId) WHERE ReferenceId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthToken_Lookup' AND object_id = OBJECT_ID(N'dbo.AppOAuthToken'))
    CREATE INDEX IX_AppOAuthToken_Lookup ON dbo.AppOAuthToken (Subject, ApplicationId, Status, Type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthToken_Authorization' AND object_id = OBJECT_ID(N'dbo.AppOAuthToken'))
    CREATE INDEX IX_AppOAuthToken_Authorization ON dbo.AppOAuthToken (AuthorizationId) WHERE AuthorizationId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthToken_Application' AND object_id = OBJECT_ID(N'dbo.AppOAuthToken'))
    CREATE INDEX IX_AppOAuthToken_Application ON dbo.AppOAuthToken (ApplicationId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthToken_Expiration' AND object_id = OBJECT_ID(N'dbo.AppOAuthToken'))
    CREATE INDEX IX_AppOAuthToken_Expiration ON dbo.AppOAuthToken (ExpirationDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppOAuthToken_Company' AND object_id = OBJECT_ID(N'dbo.AppOAuthToken'))
    CREATE INDEX IX_AppOAuthToken_Company ON dbo.AppOAuthToken (CompanyId) WHERE CompanyId IS NOT NULL;
GO
