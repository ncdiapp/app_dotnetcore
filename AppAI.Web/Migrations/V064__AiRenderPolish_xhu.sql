-- V064: AI polish tools + render schema params (session AiPolishedRenderSpec).
-- See Document/Design/AI-Agent-Result-And-Doc-Export-UI-Layout-Redesign.md

DECLARE @polishType nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentUiPolishPlugin';

DECLARE @prepareDesc nvarchar(max) = N'Prepare AI polish inputs for Result render. Returns library TemplateContent (interactive + none-interactive) for the layout, column schema, and at most 10 sample rows. Iron rule: do NOT merge sample/full rows into HTML; return structure only via cache_ui_render_polish / aiPolishedRenderSpec. Then call render_interactive or render_none_interactive so GENERATE binds all N rows.';

DECLARE @prepareSchema nvarchar(max) = N'{
  "dataset_name":{"type":"string","description":"Cached dataset name. Preferred."},
  "layout":{"type":"string","description":"form | table | cards | kpi | chart | dashboard. Default cards."},
  "user_prompt":{"type":"string","description":"Optional user intent quote for promptHash and field selection."}
}';

DECLARE @cacheDesc nvarchar(max) = N'Cache session AiPolishedRenderSpec (polished TemplateContent fragments + field/column/filter bindings). Structure only — never include result rows. Does not write company library TemplateContent. After caching, call render_* with dataset_name.';

DECLARE @cacheSchema nvarchar(max) = N'{
  "aiPolishedRenderSpec":{"type":"string","description":"JSON AiPolishedRenderSpec: version, layout, title, templateContentInteractive?, templateContentNoneInteractive?, bindings{primaryImageField,fields[{name,role,typographyHint}],columns,filterFields}, layoutHints, selectAllColumns. No rows/data/records.","required":true},
  "dataset_name":{"type":"string","description":"Cached dataset name"},
  "layout":{"type":"string","description":"layout key"},
  "promptHash":{"type":"string","description":"From prepare_ui_render_polish when user_prompt was set"}
}';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'prepare_ui_render_polish')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-ui-render',
    N'prepare_ui_render_polish',
    @prepareDesc,
    @prepareSchema,
    N'BuiltIn',
    N'{"TypeName":"' + @polishType + N'","MethodName":"PrepareUiRenderPolish"}',
    1, 2
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'cache_ui_render_polish')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-ui-render',
    N'cache_ui_render_polish',
    @cacheDesc,
    @cacheSchema,
    N'BuiltIn',
    N'{"TypeName":"' + @polishType + N'","MethodName":"CacheUiRenderPolish"}',
    1, 3
);

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @prepareDesc,
    ParameterSchemaJson = @prepareSchema,
    ToolConfig = N'{"TypeName":"' + @polishType + N'","MethodName":"PrepareUiRenderPolish"}',
    IsActive = 1
WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'prepare_ui_render_polish';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @cacheDesc,
    ParameterSchemaJson = @cacheSchema,
    ToolConfig = N'{"TypeName":"' + @polishType + N'","MethodName":"CacheUiRenderPolish"}',
    IsActive = 1
WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'cache_ui_render_polish';
GO

DECLARE @uiType nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentUiRenderPlugin';
DECLARE @renderSchema nvarchar(max) = N'{
  "layout":{"type":"string","description":"form | table | cards | kpi | chart | dashboard. Default table. layout=kpi|chart|dashboard ONLY after ask_user option kpi_dashboard."},
  "dataset_name":{"type":"string","description":"Chat session cache name. Preferred."},
  "title":{"type":"string","description":"Optional title"},
  "content":{"type":"string","description":"Form JSON or row array when dataset_name is omitted."},
  "specJson":{"type":"string","description":"Required for layout kpi|chart|dashboard after kpi_dashboard confirm."},
  "limit":{"type":"integer","description":"First N rows after cache is chosen"},
  "cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6"},
  "cardRows":{"type":"integer","description":"Card rows on one page. Default 3"},
  "actionsJson":{"type":"string","description":"Interactive only: toolbar actions [{id,label}]"},
  "aiPolishedRenderSpec":{"type":"string","description":"Optional AiPolishedRenderSpec JSON (structure only). Prefer prepare_ui_render_polish + cache_ui_render_polish. Never include result rows. GENERATE binds full dataset."},
  "selectAllColumns":{"type":"boolean","description":"true only when user asked for all columns"},
  "promptHash":{"type":"string","description":"From prepare_ui_render_polish"}
}';

DECLARE @renderDescNone nvarchar(max) = N'Render Result none-interactive HTML. Prefer dataset_name. For cards/table: call prepare_ui_render_polish then cache_ui_render_polish (or pass aiPolishedRenderSpec) so GENERATE uses selected fields — do not paint HTML from data. layout=kpi|chart|dashboard ONLY after ask_user picks kpi_dashboard.';

DECLARE @renderDescInter nvarchar(max) = N'Render interactive Result. Prefer dataset_name. For cards/table: polish field structure first (prepare_ui_render_polish / cache_ui_render_polish or aiPolishedRenderSpec). Do not merge rows into HTML in the model. layout=kpi|chart|dashboard ONLY after ask_user picks kpi_dashboard.';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @renderSchema,
    ToolDescription = @renderDescNone,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderNoneInteractive"}'
WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'render_none_interactive';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @renderSchema,
    ToolDescription = @renderDescInter,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderInteractive"}'
WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'render_interactive';
GO

-- Mirror BuiltIn register rows if present
UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Prepare AI polish inputs (templates + ≤10 sample rows). Structure-only response.',
    ParameterSchemaJson = N'{"dataset_name":{"type":"string"},"layout":{"type":"string"},"user_prompt":{"type":"string"}}',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentUiPolishPlugin","MethodName":"PrepareUiRenderPolish"}'
WHERE ToolName = N'prepare_ui_render_polish' AND ToolType = N'BuiltIn';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Cache session AiPolishedRenderSpec (no result rows).',
    ParameterSchemaJson = N'{"aiPolishedRenderSpec":{"type":"string","required":true},"dataset_name":{"type":"string"},"layout":{"type":"string"},"promptHash":{"type":"string"}}',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentUiPolishPlugin","MethodName":"CacheUiRenderPolish"}'
WHERE ToolName = N'cache_ui_render_polish' AND ToolType = N'BuiltIn';
GO
