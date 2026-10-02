-- V042: Library data-analyze + BuiltIn tool data_analyze (session cache + aggregations).
-- Presence of tool data_analyze enables runtime followups inject.
-- Tabular results from SqlQuery / HttpRest / BuiltIn / MCP auto-cache into ChatSession.
-- data-ui-render / data_render left unchanged.
-- If a draft previously added SuggestFollowups on AppAgentSkillSet, drop it.

IF COL_LENGTH('dbo.AppAgentSkillSet', 'SuggestFollowups') IS NOT NULL
BEGIN
    DECLARE @df sysname =
        (SELECT dc.name
         FROM sys.default_constraints dc
         INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
         WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AppAgentSkillSet')
           AND c.name = N'SuggestFollowups');
    IF @df IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AppAgentSkillSet DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE dbo.AppAgentSkillSet DROP COLUMN SuggestFollowups;
END
GO

-- Rename draft key agent-chat-ux → data-analyze if present (FK-safe)
IF EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-chat-ux')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'data-analyze')
BEGIN
    INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
    SELECT N'data-analyze', DomainKey, N'Data Analyze',
           N'Analysis tools: data_analyze aggregations over ChatSession-cached tabular query/API/MCP results. Pair with data-ui-render for visuals.',
           ToolCategory, IsActive
    FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-chat-ux';

    UPDATE dbo.AppAgentLibrarySubscription SET LibraryKey = N'data-analyze' WHERE LibraryKey = N'agent-chat-ux';
    DELETE FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-chat-ux';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'data-analyze')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'data-analyze',
    N'platform',
    N'Data Analyze',
    N'Analysis tools: data_analyze aggregations over ChatSession-cached tabular query/API/MCP results. Pair with data-ui-render for visuals.',
    N'BuiltIn',
    1
);
GO

UPDATE dbo.AppAgentToolLibrary
SET LibraryName = N'Data Analyze',
    Description = N'Analysis tools: data_analyze aggregations over ChatSession-cached tabular query/API/MCP results. Pair with data-ui-render for visuals.',
    DomainKey = N'platform',
    ToolCategory = N'BuiltIn',
    IsActive = 1
WHERE LibraryKey = N'data-analyze';
GO

DECLARE @desc NVARCHAR(MAX) = N'Analyze a ChatSession-cached tabular dataset (or pass dataJson for small inline arrays). Prefer dataset_name from a prior query/API/MCP tool result that returned dataset_cached. Operations: summary|count|sum|avg|min|max|distinct|top|distribution. Optional: column, group_by, filter, filter_column, limit, order_by (asc|desc). Returns text + structured result JSON for narration and data_render. Do not re-fetch large tables; do not dump raw rows into FinalResponse.';
DECLARE @schema NVARCHAR(MAX) = N'{"operation":{"type":"string","description":"summary | count | sum | avg | min | max | distinct | top | distribution","required":true},"dataset_name":{"type":"string","description":"Cached dataset name from a prior tool envelope"},"dataJson":{"type":"string","description":"Optional inline JSON array / {Rows|data:[...]} when no cache yet"},"column":{"type":"string","description":"Target column for sum/avg/min/max/distinct/top/distribution"},"group_by":{"type":"string","description":"Group key for count/sum/avg/min/max"},"filter":{"type":"string","description":"Substring filter"},"filter_column":{"type":"string","description":"Column to apply filter to"},"limit":{"type":"string","description":"Max groups/rows to return (default 20)"},"order_by":{"type":"string","description":"asc | desc"}}';
DECLARE @cfg NVARCHAR(MAX) = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentDataAnalyzePlugin","MethodName":"Analyze"}';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'data-analyze' AND ToolName = N'data_analyze')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'data-analyze',
    N'data_analyze',
    @desc,
    @schema,
    N'BuiltIn',
    @cfg,
    1,
    0
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Analyze a ChatSession-cached tabular dataset (or pass dataJson for small inline arrays). Prefer dataset_name from a prior query/API/MCP tool result that returned dataset_cached. Operations: summary|count|sum|avg|min|max|distinct|top|distribution. Optional: column, group_by, filter, filter_column, limit, order_by (asc|desc). Returns text + structured result JSON for narration and data_render. Do not re-fetch large tables; do not dump raw rows into FinalResponse.',
    ParameterSchemaJson = N'{"operation":{"type":"string","description":"summary | count | sum | avg | min | max | distinct | top | distribution","required":true},"dataset_name":{"type":"string","description":"Cached dataset name from a prior tool envelope"},"dataJson":{"type":"string","description":"Optional inline JSON array / {Rows|data:[...]} when no cache yet"},"column":{"type":"string","description":"Target column for sum/avg/min/max/distinct/top/distribution"},"group_by":{"type":"string","description":"Group key for count/sum/avg/min/max"},"filter":{"type":"string","description":"Substring filter"},"filter_column":{"type":"string","description":"Column to apply filter to"},"limit":{"type":"string","description":"Max groups/rows to return (default 20)"},"order_by":{"type":"string","description":"asc | desc"}}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentDataAnalyzePlugin","MethodName":"Analyze"}',
    IsActive = 1
WHERE LibraryKey = N'data-analyze'
  AND ToolName = N'data_analyze';
GO
