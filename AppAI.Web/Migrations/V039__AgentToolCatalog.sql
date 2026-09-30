-- V039: Unified, searchable tool catalog used by "AI Generate Agent Design".
-- One row per tool from every source (built-in / library / MCP / REST ...). Library tools are synced from
-- AppAgentLibraryTool; MCP tools are synced from the live server ("Sync tools").
-- Embedding is reserved for a later RAG phase and stays NULL for now.

IF OBJECT_ID(N'dbo.AppAgentToolCatalog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppAgentToolCatalog
    (
        CatalogId    INT IDENTITY(1,1) NOT NULL,
        Source       NVARCHAR(30)   NOT NULL,              -- builtin | httprest | sqlquery | mcp | ...
        LibraryKey   NVARCHAR(100)  NOT NULL,
        ToolName     NVARCHAR(200)  NOT NULL,
        Description  NVARCHAR(MAX)  NULL,
        InputSummary NVARCHAR(1000) NULL,                   -- parameter names only, * = required
        Risk         NVARCHAR(10)   NOT NULL CONSTRAINT DF_AppAgentToolCatalog_Risk DEFAULT N'read',  -- read | write | delete
        McpServerId  INT            NULL,                   -- set for Source = 'mcp'
        Embedding    VARBINARY(MAX) NULL,
        SyncedAt     DATETIME2      NOT NULL CONSTRAINT DF_AppAgentToolCatalog_SyncedAt DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AppAgentToolCatalog PRIMARY KEY (CatalogId)
    );

    CREATE UNIQUE INDEX UX_AppAgentToolCatalog_LibraryTool ON dbo.AppAgentToolCatalog (LibraryKey, ToolName);
    CREATE INDEX IX_AppAgentToolCatalog_McpServer ON dbo.AppAgentToolCatalog (McpServerId) WHERE McpServerId IS NOT NULL;
END
GO
