-- V060: Analysis-like intent → ask_user (KPI/Dashboard batch vs fetch-and-display only).
-- data_analyze and layout=kpi|chart|dashboard only after user picks kpi_dashboard.

DECLARE @desc nvarchar(max) = N'Run aggregations on a ChatSession-cached dataset (count/sum/avg/… with optional group_by).
BEFORE calling this tool when the user question looks like data analysis: call ask_user (contextKey=confirm_kpi_dashboard, mode=single_choice) with options kpi_dashboard (KPI/Dashboard batch analysis) vs fetch_display (only fetch and show the data they asked for).
Call data_analyze ONLY after the user picks kpi_dashboard. If they pick fetch_display, do not call this tool — use render_*/export_* instead.
Do NOT invent analysis via export_csv/read_csv. Do NOT call for plain search/show/export without analysis intent.
Required: user_ask = quote of the user words that request analysis. Prefer dataset_name from dataset_cached. Operations: summary|count|sum|avg|min|max|distinct|top|distribution. Optional: column, group_by, filter, filter_column, limit, order_by.';

DECLARE @schema nvarchar(max) = N'{
  "user_ask":{"type":"string","description":"Required. Quote the user words that request analysis. Show/export-only text is rejected.","required":true},
  "operation":{"type":"string","description":"summary | count | sum | avg | min | max | distinct | top | distribution","required":true},
  "dataset_name":{"type":"string","description":"Cached dataset name from a prior tool envelope"},
  "dataJson":{"type":"string","description":"Optional inline JSON when no cache yet"},
  "column":{"type":"string","description":"Target column for sum/avg/min/max/distinct/top/distribution"},
  "group_by":{"type":"string","description":"Group key for count/sum/avg/min/max"},
  "filter":{"type":"string","description":"Substring filter"},
  "filter_column":{"type":"string","description":"Column to apply filter to"},
  "limit":{"type":"string","description":"Max groups/rows returned (default 20)"},
  "order_by":{"type":"string","description":"asc | desc"}
}';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @desc,
    ParameterSchemaJson = @schema
WHERE LibraryKey = N'data-analyze'
  AND ToolName = N'data_analyze';

UPDATE dbo.AppAgentToolLibrary
SET Description = N'data_analyze after user confirms KPI/Dashboard path (ask_user: kpi_dashboard vs fetch_display).'
WHERE LibraryKey = N'data-analyze';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = @desc,
    ParameterSchemaJson = @schema
WHERE ToolName = N'data_analyze'
  AND ToolType = N'BuiltIn';
GO

DECLARE @uiType nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentUiRenderPlugin';
DECLARE @renderSchema nvarchar(max) = N'{"layout":{"type":"string","description":"form | table | cards | kpi | chart | dashboard. Default table. layout=kpi|chart|dashboard ONLY after ask_user option kpi_dashboard (contextKey=confirm_kpi_dashboard)."},"dataset_name":{"type":"string","description":"Chat session cache name. Preferred."},"title":{"type":"string","description":"Optional title"},"content":{"type":"string","description":"Form JSON or row array when dataset_name is omitted."},"specJson":{"type":"string","description":"Required for layout kpi|chart|dashboard after kpi_dashboard confirm."},"limit":{"type":"integer","description":"First N rows after cache is chosen"},"cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6"},"cardRows":{"type":"integer","description":"Card rows on one page. Default 3"},"actionsJson":{"type":"string","description":"Interactive only: toolbar actions [{id,label}]"}}';
DECLARE @tmpl nvarchar(max) = N'{"form":"{{code_default}}","table":"{{code_default}}","cards":"{{code_default}}","kpi":"{{code_default}}","chart":"{{code_default}}","dashboard":"{{code_default}}"}';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @renderSchema,
    ToolDescription = N'Render Result iframe HTML. Prefer dataset_name. layout=form|table|cards freely. layout=kpi|chart|dashboard ONLY after ask_user picks kpi_dashboard (contextKey=confirm_kpi_dashboard), then specJson. Do not send HTML.',
    TemplateContent = @tmpl,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderNoneInteractive"}'
WHERE LibraryKey = N'platform-ui-render'
  AND ToolName = N'render_none_interactive';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @renderSchema,
    ToolDescription = N'Render interactive Result iframe. layout=form|table|cards freely. layout=kpi|chart|dashboard ONLY after ask_user picks kpi_dashboard. Do not send HTML.',
    TemplateContent = @tmpl,
    ToolConfig = N'{"TypeName":"' + @uiType + N'","MethodName":"RenderInteractive"}'
WHERE LibraryKey = N'platform-ui-render'
  AND ToolName = N'render_interactive';
GO

-- Strip older analyze/KPI prompt blocks then append the current rule once
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
    N'## KPI / chart / dashboard — ask first
If you believe a KPI, chart, or dashboard would help but the user did not already confirm that path: call ask_user (contextKey=confirm_kpi_dashboard, mode=single_choice) with a short proposal and options Yes (build KPI/dashboard) / No (use table or cards). Only after Yes may you call data_analyze or render/export with layout=kpi|chart|dashboard. Never invent a KPI/dashboard without that confirmation. Plain search/show/export stays on layout=table|cards.',
    N'')
WHERE SystemPrompt LIKE N'%## KPI / chart / dashboard — ask first%';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
    N'## Analyze vs KPI/dashboard
When the user asks to analyze / break down / group by fields: AFTER the dataset is cached, call data_analyze (group_by each column the user named). Do not invent analysis with export_csv/read_csv or free-form tables only.
When you want a Result KPI/chart/dashboard visual (or the user asked for one vaguely): ask_user first (contextKey=confirm_kpi_dashboard, Yes/No). Only after Yes use layout=kpi|chart|dashboard. Ordinary data_analyze breakdowns do not need that confirm.',
    N'')
WHERE SystemPrompt LIKE N'%## Analyze vs KPI/dashboard%';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
    N'## render kpi / chart / dashboard
If you believe KPI/chart/dashboard would help (or the user asked vaguely for insight visuals): call ask_user first (contextKey=confirm_kpi_dashboard, Yes / No). Only after Yes: optional data_analyze to shape, then render_none_interactive with layout=kpi|chart|dashboard, dataset_name, and specJson. Do not invent HTML. For plain table/cards show or export, use render/export with limit — do not call data_analyze just for top-N.',
    N'')
WHERE SystemPrompt LIKE N'%## render kpi / chart / dashboard%';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

## Data analysis intent — ask the user
You decide from the user message whether it looks like data analysis (analyze, breakdown, group by, metrics, compare, KPI, dashboard, insight, etc.).
If it does: call ask_user (contextKey=confirm_kpi_dashboard, mode=single_choice) BEFORE data_analyze or layout=kpi|chart|dashboard. Prompt: ask how to proceed. Options: kpi_dashboard = KPI/Dashboard batch analysis; fetch_display = only fetch and show the data they asked for.
- After kpi_dashboard: you may data_analyze and render/export kpi|chart|dashboard.
- After fetch_display: only search/fetch + render/export table|cards as requested — do not data_analyze and do not invent a dashboard.
If the message is clearly show/search/export only (no analysis intent), skip ask_user and do fetch + render/export.'
WHERE IsActive = 1
  AND (SystemPrompt LIKE N'%render_none_interactive%' OR SystemPrompt LIKE N'%data_analyze%' OR SystemPrompt LIKE N'%data_render%')
  AND SystemPrompt NOT LIKE N'%Data analysis intent — ask the user%';
GO
