-- merge file
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


-- V039: Library data-ui-render + tool data_render (non-blocking chat UI: grid/card/chart).
-- Subscribe per agent via AppAgentLibrarySubscription. Not system auto-inject.
-- Idempotent: safe to re-run.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'data-ui-render')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'data-ui-render',
    N'platform',
    N'Data UI Render',
    N'Present tabular, card, or chart data in Agent Chat (non-blocking). Subscribe per agent.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'data-ui-render' AND ToolName = N'data_render')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'data-ui-render',
    N'data_render',
    N'Present data in Agent Chat as an interactive UI panel. Non-blocking: returns immediately after the UI is pushed. ui=grid|card|chart. Pass rows as dataJson (JSON array). Optional columnsJson for grid, chartConfigJson for chart (type,xField,yField,groupBy,allowedTypes), actionsJson for toolbar buttons [{id,label}] that the user can click to send a follow-up turn, metaJson for title/subtitle/measure. Do not put large payloads only in FinalResponse — call this tool.',
    N'{"ui":{"type":"string","description":"grid | card | chart","required":true},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"JSON array of row objects (grid/chart) or one object / {fields:[{label,value}]} (card)","required":true},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]"},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,...}"}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentDataRenderPlugin","MethodName":"Render"}',
    1,
    0
);
GO
