-- V046: Audit trail for the MCP gateway (external users on Claude Desktop / ChatGPT Desktop etc.).
-- One row per audited gateway event, written in the caller's tenant DB so each company only ever holds its own events.
-- Replaces the gateway's old standalone AuditLog table, which had no user or company and a single global database.
-- UserId refers to AppMasterDB.dbo.AppSecurityUser (no FK across databases). The access token is never stored.

IF OBJECT_ID(N'dbo.AppMcpAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppMcpAuditLog
    (
        AuditId           BIGINT IDENTITY(1,1) NOT NULL,
        CreatedUtc        DATETIME2      NOT NULL CONSTRAINT DF_AppMcpAuditLog_CreatedUtc DEFAULT SYSUTCDATETIME(),
        EventCode         NVARCHAR(40)   NOT NULL,              -- e.g. A021_ApiCrudAction
        Action            NVARCHAR(500)  NOT NULL,
        Success           BIT            NOT NULL,
        CompanyId         INT            NULL,
        UserId            INT            NULL,
        McpSessionId      NVARCHAR(100)  NULL,                  -- client-supplied Mcp-Session-Id, informational only
        IpAddress         NVARCHAR(64)   NULL,
        CorrelationId     NVARCHAR(100)  NULL,
        AppSource         NVARCHAR(100)  NULL,
        HttpMethod        NVARCHAR(10)   NULL,
        ResourcePath      NVARCHAR(1000) NULL,
        HttpStatus        INT            NULL,
        ErrorMessage      NVARCHAR(2000) NULL,
        AdditionalContext NVARCHAR(MAX)  NULL,                  -- JSON
        CONSTRAINT PK_AppMcpAuditLog PRIMARY KEY (AuditId)
    );

    CREATE INDEX IX_AppMcpAuditLog_CreatedUtc ON dbo.AppMcpAuditLog (CreatedUtc DESC);
    CREATE INDEX IX_AppMcpAuditLog_User ON dbo.AppMcpAuditLog (UserId, CreatedUtc DESC) WHERE UserId IS NOT NULL;
END
GO
