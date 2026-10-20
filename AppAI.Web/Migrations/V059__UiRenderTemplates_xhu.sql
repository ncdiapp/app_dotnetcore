-- V059: UI render templates on AppAgentLibraryTool; rename data-ui-render → platform-ui-render;
-- seed render_none_interactive / render_interactive (form|table|cards|kpi|chart|dashboard + specJson);
-- keep data_render as one-release alias; export layout + specJson; prompt nudges.

IF COL_LENGTH(N'dbo.AppAgentLibraryTool', N'TemplateContent') IS NULL
    ALTER TABLE dbo.AppAgentLibraryTool ADD TemplateContent NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH(N'dbo.AppAgentLibraryTool', N'TemplateMetaJson') IS NULL
    ALTER TABLE dbo.AppAgentLibraryTool ADD TemplateMetaJson NVARCHAR(MAX) NULL;
GO

-- Rename library data-ui-render → platform-ui-render
IF EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'data-ui-render')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-ui-render')
BEGIN
    INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
    SELECT N'platform-ui-render', DomainKey, N'Platform UI Render',
           N'Render analysis results in Agent Chat Result (iframe). C# merges templates; model does not write HTML.',
           ToolCategory, IsActive
    FROM dbo.AppAgentToolLibrary
    WHERE LibraryKey = N'data-ui-render';

    UPDATE dbo.AppAgentLibraryTool
    SET LibraryKey = N'platform-ui-render'
    WHERE LibraryKey = N'data-ui-render';

    UPDATE dbo.AppAgentLibrarySubscription
    SET LibraryKey = N'platform-ui-render'
    WHERE LibraryKey = N'data-ui-render';

    IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
        UPDATE dbo.AppAgentToolExclusion
        SET LibraryKey = N'platform-ui-render'
        WHERE LibraryKey = N'data-ui-render';

    IF OBJECT_ID(N'dbo.AppAgentToolCatalog', N'U') IS NOT NULL
        UPDATE dbo.AppAgentToolCatalog
        SET LibraryKey = N'platform-ui-render'
        WHERE LibraryKey = N'data-ui-render';

    DELETE FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'data-ui-render';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-ui-render')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-ui-render',
    N'platform',
    N'Platform UI Render',
    N'Render analysis results in Agent Chat Result (iframe). C# merges templates; model does not write HTML.',
    N'BuiltIn',
    1
);
GO

DECLARE @uiType nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentUiRenderPlugin';
DECLARE @renderSchema nvarchar(max) = N'{"layout":{"type":"string","description":"form | table | cards | kpi | chart | dashboard. Default table. For kpi|chart|dashboard: ONLY after ask_user confirm (contextKey=confirm_kpi_dashboard). Then optional data_analyze + specJson."},"dataset_name":{"type":"string","description":"Chat session cache name. Preferred."},"title":{"type":"string","description":"Optional title"},"content":{"type":"string","description":"Form JSON or row array when dataset_name is omitted."},"specJson":{"type":"string","description":"Required for layout kpi|chart|dashboard after user confirm. JSON: {title?, kpis:[{label,field,op}], chart:{type,xField,yField,title}, blocks:[{type:kpi|chart|table,...}]}. Render paints only."},"limit":{"type":"integer","description":"First N rows after cache is chosen"},"cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6"},"cardRows":{"type":"integer","description":"Card rows on one page. Default 3"},"actionsJson":{"type":"string","description":"Interactive only: toolbar actions [{id,label}]"}}';
DECLARE @tmpl nvarchar(max) = N'{"form":"{{code_default}}","table":"{{code_default}}","cards":"{{code_default}}","kpi":"{{code_default}}","chart":"{{code_default}}","dashboard":"{{code_default}}"}';
DECLARE @metaNone nvarchar(200) = N'{"mode":"NoneInteractive"}';
DECLARE @metaInter nvarchar(200) = N'{"mode":"Interactive"}';
DECLARE @aliasSchema nvarchar(max) = N'{"ui":{"type":"string","description":"Legacy: grid|card|chart|kpi|kpi_dashboard — mapped to layout","required":true},"layout":{"type":"string","description":"form|table|cards|kpi|chart|dashboard when not using ui"},"dataset_name":{"type":"string"},"title":{"type":"string"},"dataJson":{"type":"string"},"specJson":{"type":"string"},"blocksJson":{"type":"string","description":"Legacy kpi_dashboard blocks; wrapped into specJson"},"actionsJson":{"type":"string"},"limit":{"type":"integer"},"cardsPerRow":{"type":"integer"},"content":{"type":"string"}}';
DECLARE @aliasDesc nvarchar(max) = N'Deprecated alias. Prefer render_none_interactive or render_interactive. Maps ui=grid→table, card→cards, chart→chart, kpi/kpi_dashboard→kpi|dashboard. Pass specJson (or legacy blocksJson) for chart/dashboard.';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'render_none_interactive')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder, TemplateContent, TemplateMetaJson)
VALUES (
    N'platform-ui-render',
    N'render_none_interactive',
    N'Render a none-interactive HTML page in the Agent Chat Result iframe. C# merges the template with dataset_name. Prefer dataset_name. layout=form|table|cards|kpi|chart|dashboard. For kpi/chart/dashboard pass specJson. Do not send HTML.',
    @renderSchema,
    N'BuiltIn',
    N'{"TypeName":"' + @uiType + N'","MethodName":"RenderNoneInteractive"}',
    1, 0, @tmpl, @metaNone
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'render_interactive')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder, TemplateContent, TemplateMetaJson)
VALUES (
    N'platform-ui-render',
    N'render_interactive',
    N'Render an interactive HTML page in the Result iframe. Prefer dataset_name. layout=form|table|cards|kpi|chart|dashboard. Pass specJson for kpi/chart/dashboard. Pass actionsJson for buttons. Do not send HTML.',
    @renderSchema,
    N'BuiltIn',
    N'{"TypeName":"' + @uiType + N'","MethodName":"RenderInteractive"}',
    1, 1, @tmpl, @metaInter
);

-- Alias: update existing data_render to forward to AgentUiRenderPlugin.RenderAlias
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @aliasDesc,
    ParameterSchemaJson = @aliasSchema,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderAlias"}',
    TemplateContent = NULL,
    TemplateMetaJson = NULL,
    LibraryKey = N'platform-ui-render'
WHERE ToolName = N'data_render'
  AND (LibraryKey = N'platform-ui-render' OR LibraryKey = N'data-ui-render');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'data_render')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-ui-render',
    N'data_render',
    @aliasDesc,
    @aliasSchema,
    N'BuiltIn',
    N'{"TypeName":"' + @uiType + N'","MethodName":"RenderAlias"}',
    1, 9
);

-- Upsert schema/templates on render tools (idempotent if rows already existed)
UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @renderSchema,
    ToolDescription = N'Render a none-interactive HTML page in the Agent Chat Result iframe. C# merges the template with dataset_name. Prefer dataset_name. layout=form|table|cards|kpi|chart|dashboard. For kpi/chart/dashboard pass specJson. Do not send HTML.',
    TemplateContent = @tmpl,
    TemplateMetaJson = @metaNone,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderNoneInteractive"}'
WHERE LibraryKey = N'platform-ui-render'
  AND ToolName = N'render_none_interactive';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @renderSchema,
    ToolDescription = N'Render an interactive HTML page in the Result iframe. Prefer dataset_name. layout=form|table|cards|kpi|chart|dashboard. Pass specJson for kpi/chart/dashboard. Pass actionsJson for buttons. Do not send HTML.',
    TemplateContent = @tmpl,
    TemplateMetaJson = @metaInter,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderInteractive"}'
WHERE LibraryKey = N'platform-ui-render'
  AND ToolName = N'render_interactive';
GO

-- Append specJson to HTML/PDF/DOCX/PPTX export schemas when missing
UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = STUFF(ParameterSchemaJson, LEN(ParameterSchemaJson), 1,
    N',"specJson":{"type":"string","description":"Required when layout is kpi|chart|dashboard. Same shape as render tools."}}')
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_pdf', N'export_docx', N'export_pptx')
  AND ParameterSchemaJson IS NOT NULL
  AND ParameterSchemaJson NOT LIKE N'%"specJson"%'
  AND RIGHT(LTRIM(RTRIM(ParameterSchemaJson)), 1) = N'}';
GO

-- Broaden layout description on export tools
UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = REPLACE(ParameterSchemaJson,
    N'form (default), table, or cards',
    N'form (default), table, cards, kpi, chart, or dashboard')
WHERE LibraryKey = N'platform-document-export'
  AND ToolName LIKE N'export_%'
  AND ParameterSchemaJson LIKE N'%form (default), table, or cards%';
GO

-- Nudge agent prompts: prefer render_none_interactive over data_render
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(
    REPLACE(SystemPrompt,
        N'call data_render ONCE with that dataset_name',
        N'call render_none_interactive ONCE with that dataset_name (layout=table or cards)'),
    N'call data_render with ui=grid and dataset_name',
    N'call render_none_interactive with layout=table and dataset_name')
WHERE SystemPrompt LIKE N'%data_render%'
  AND SystemPrompt NOT LIKE N'%render_none_interactive%';
GO

-- Prompt nudge superseded by V060 "Data analysis intent — ask the user" (kpi_dashboard vs fetch_display).
GO

