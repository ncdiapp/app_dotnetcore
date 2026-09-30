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
