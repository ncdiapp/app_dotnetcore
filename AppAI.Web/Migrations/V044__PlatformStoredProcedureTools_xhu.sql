-- V044: platform-database stored-procedure tools + data_render dataset_name (full columns).
-- Tools: stored_procedure_list | search | detail | execute
-- SOP: search/list → detail → execute; then data_render(ui=grid, dataset_name=…) for ALL columns.
-- Idempotent. Also migrates away from draft library key platform-stored-procedures if present.

-- ── Move draft library tools → platform-database (if an earlier draft ran) ──
IF EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-stored-procedures')
BEGIN
    UPDATE t
    SET LibraryKey = N'platform-database'
    FROM dbo.AppAgentLibraryTool t
    WHERE t.LibraryKey = N'platform-stored-procedures'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.AppAgentLibraryTool x
          WHERE x.LibraryKey = N'platform-database' AND x.ToolName = t.ToolName
      );

    DELETE FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-stored-procedures';
END
GO

-- Point subscriptions at platform-database; drop the draft library key.
IF EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE LibraryKey = N'platform-stored-procedures')
BEGIN
    INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
    SELECT s.SkillKey, N'platform-database'
    FROM dbo.AppAgentLibrarySubscription s
    WHERE s.LibraryKey = N'platform-stored-procedures'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.AppAgentLibrarySubscription x
          WHERE x.SkillKey = s.SkillKey AND x.LibraryKey = N'platform-database'
      );

    DELETE FROM dbo.AppAgentLibrarySubscription WHERE LibraryKey = N'platform-stored-procedures';
END
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE LibraryKey = N'platform-stored-procedures')
BEGIN
    UPDATE e
    SET LibraryKey = N'platform-database'
    FROM dbo.AppAgentToolExclusion e
    WHERE e.LibraryKey = N'platform-stored-procedures'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.AppAgentToolExclusion x
          WHERE x.SkillKey = e.SkillKey
            AND x.LibraryKey = N'platform-database'
            AND x.ToolName = e.ToolName
      );

    DELETE FROM dbo.AppAgentToolExclusion WHERE LibraryKey = N'platform-stored-procedures';
END
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-stored-procedures')
    DELETE FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-stored-procedures';
GO

-- Ensure platform-database library exists (seeded by V028).
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-database')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-database',
    N'platform',
    N'BuiltIn: Database, Query & Diagram',
    N'Physical database, SQL query, table DDL, stored procedures, and (later) DB view / ER diagram tools.',
    N'BuiltIn',
    1
);
GO

UPDATE dbo.AppAgentToolLibrary
SET Description = N'Physical database, SQL query, table DDL, stored procedures, and (later) DB view / ER diagram tools. Prefer stored_procedure_search → detail → execute for SP work; use execute_sql for ad-hoc SELECT.'
WHERE LibraryKey = N'platform-database';
GO

-- stored_procedure_list
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_list')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'stored_procedure_list',
    N'List stored procedures on a DataSource (paginated). Returns schema, name, fullName, short description — not full definitions. Prefer stored_procedure_search when looking for a specific capability (e.g. GETTAB). Optional dataSourceId = DataSourceRegisterId; omit to use the session default DS. Then call stored_procedure_detail before execute.',
    N'{"type":"object","properties":{"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"},"schema":{"type":"string","description":"Optional schema filter (e.g. dbo)"},"skip":{"type":"integer","description":"Offset (default 0)"},"take":{"type":"integer","description":"Page size (default 50, max 200)"}}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"List"}',
    1,
    60
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'List stored procedures on a DataSource (paginated). Returns schema, name, fullName, short description — not full definitions. Prefer stored_procedure_search when looking for a specific capability (e.g. GETTAB). Optional dataSourceId = DataSourceRegisterId; omit to use the session default DS. Then call stored_procedure_detail before execute.',
    ParameterSchemaJson = N'{"type":"object","properties":{"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"},"schema":{"type":"string","description":"Optional schema filter (e.g. dbo)"},"skip":{"type":"integer","description":"Offset (default 0)"},"take":{"type":"integer","description":"Page size (default 50, max 200)"}}}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"List"}',
    IsActive = 1,
    SortOrder = 60
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_list';
GO

-- stored_procedure_search
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_search')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'stored_procedure_search',
    N'Search stored procedures by keyword against name, schema, comments, and (if needed) definition text. Use this first for tasks like “GETTAB / tabid / referenceid”. Required: query. Optional dataSourceId. Then stored_procedure_detail → stored_procedure_execute.',
    N'{"type":"object","properties":{"query":{"type":"string","description":"Keyword (procedure name fragment or business term)"},"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"},"take":{"type":"integer","description":"Max hits (default 30, max 100)"}},"required":["query"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Search"}',
    1,
    61
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Search stored procedures by keyword against name, schema, comments, and (if needed) definition text. Use this first for tasks like “GETTAB / tabid / referenceid”. Required: query. Optional dataSourceId. Then stored_procedure_detail → stored_procedure_execute.',
    ParameterSchemaJson = N'{"type":"object","properties":{"query":{"type":"string","description":"Keyword (procedure name fragment or business term)"},"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"},"take":{"type":"integer","description":"Max hits (default 30, max 100)"}},"required":["query"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Search"}',
    IsActive = 1,
    SortOrder = 61
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_search';
GO

-- stored_procedure_detail
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_detail')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'stored_procedure_detail',
    N'Get one stored procedure: parameters (name/type/direction), description, usage hint, and FULL definition (OBJECT_DEFINITION / SHOW CREATE / ALL_SOURCE). Required: procedureName (optionally schema.Name). Call before stored_procedure_execute so arguments are correct.',
    N'{"type":"object","properties":{"procedureName":{"type":"string","description":"Procedure name or schema.name"},"schema":{"type":"string","description":"Optional schema if not embedded in procedureName"},"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"}},"required":["procedureName"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Detail"}',
    1,
    62
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Get one stored procedure: parameters (name/type/direction), description, usage hint, and FULL definition (OBJECT_DEFINITION / SHOW CREATE / ALL_SOURCE). Required: procedureName (optionally schema.Name). Call before stored_procedure_execute so arguments are correct.',
    ParameterSchemaJson = N'{"type":"object","properties":{"procedureName":{"type":"string","description":"Procedure name or schema.name"},"schema":{"type":"string","description":"Optional schema if not embedded in procedureName"},"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"}},"required":["procedureName"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Detail"}',
    IsActive = 1,
    SortOrder = 62
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_detail';
GO

DECLARE @spExecDesc NVARCHAR(MAX) = N'Execute a stored procedure on the chosen DataSource. Required: procedureName. Pass argsJson as a JSON object of parameter names to values (with or without @). Optional dataSourceId. Returns full rows including null cells + columnNames (capped row count). On success the result includes dataset_cached + dataset_name — call data_render with ui=grid and that dataset_name so the grid shows ALL columns (do not copy a subset into dataJson). Large narrative: use data_analyze. Allowed for Interactive and Deterministic agents.';
DECLARE @spExecSchema NVARCHAR(MAX) = N'{"type":"object","properties":{"procedureName":{"type":"string","description":"Procedure name or schema.name"},"argsJson":{"type":"string","description":"JSON object of SP arguments, e.g. {\"TabId\":123,\"ReferenceId\":1001}"},"schema":{"type":"string","description":"Optional schema"},"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"}},"required":["procedureName"]}';
DECLARE @spExecCfg NVARCHAR(MAX) = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Execute"}';

-- stored_procedure_execute
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_execute')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'stored_procedure_execute',
    @spExecDesc,
    @spExecSchema,
    N'BuiltIn',
    @spExecCfg,
    1,
    63
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Execute a stored procedure on the chosen DataSource. Required: procedureName. Pass argsJson as a JSON object of parameter names to values (with or without @). Optional dataSourceId. Returns full rows including null cells + columnNames (capped row count). On success the result includes dataset_cached + dataset_name — call data_render with ui=grid and that dataset_name so the grid shows ALL columns (do not copy a subset into dataJson). Large narrative: use data_analyze. Allowed for Interactive and Deterministic agents.',
    ParameterSchemaJson = N'{"type":"object","properties":{"procedureName":{"type":"string","description":"Procedure name or schema.name"},"argsJson":{"type":"string","description":"JSON object of SP arguments, e.g. {\"TabId\":123,\"ReferenceId\":1001}"},"schema":{"type":"string","description":"Optional schema"},"dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId; omit for session default"}},"required":["procedureName"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Execute"}',
    IsActive = 1,
    SortOrder = 63
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_execute';
GO

-- ── data_render: dataset_name loads full session-cached columns (incl. nulls) ──
UPDATE dbo.AppAgentLibraryTool
SET
    ToolDescription = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
IMPORTANT for grid/chart after stored_procedure_execute / execute_sql / any tool that returned dataset_cached=true:
  Prefer dataset_name=<that name> and ui=grid. Do NOT rebuild dataJson with a subset of columns — dataset_name loads EVERY column including nulls from the chat session cache.
For KPI / dashboard / analysis / overview / summary: prefer ONE call with ui=kpi_dashboard and blocksJson. Limits: kpi items<=12, chart blocks<=9, grid blocks<=3.
Pass dataJson only when there is no dataset_name (small hand-built payloads). Optional columnsJson for grid (ignored when dataset_name is set). chartConfigJson / actionsJson / metaJson / blocksJson as before.',
    ParameterSchemaJson = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"dataset_name":{"type":"string","description":"Preferred for grid/chart: session cache name from a prior tool result (dataset_cached / dataset_name). Loads ALL columns including nulls; do not also pass a column subset."},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Required only when dataset_name is omitted. JSON array of rows or card object. For kpi_dashboard optional summary / blocks fallback."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]. Ignored when dataset_name is set."},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,...}"}}'
WHERE LibraryKey = N'data-ui-render'
  AND ToolName = N'data_render';
GO

-- Prompt nudge for agents already subscribed to data-ui-render
DECLARE @hint NVARCHAR(MAX) = N'

## data_render full columns
After stored_procedure_execute / execute_sql (or any tool) returns dataset_cached and dataset_name: call data_render with ui=grid and dataset_name only. Never rebuild dataJson with fewer columns than the SP/SQL result.';

UPDATE s
SET SystemPrompt = CASE
    WHEN s.SystemPrompt IS NULL OR LTRIM(RTRIM(s.SystemPrompt)) = N'' THEN LTRIM(@hint)
    WHEN s.SystemPrompt LIKE N'%dataset_name only%' OR s.SystemPrompt LIKE N'%data_render full columns%' THEN s.SystemPrompt
    ELSE s.SystemPrompt + @hint
END
FROM dbo.AppAgentSkillSet s
INNER JOIN dbo.AppAgentLibrarySubscription sub
    ON sub.SkillKey = s.SkillKey AND sub.LibraryKey = N'data-ui-render';
GO
