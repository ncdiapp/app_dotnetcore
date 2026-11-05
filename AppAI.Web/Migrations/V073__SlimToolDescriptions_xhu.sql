-- Short tool cards (LLM functionDeclarations) + CallNotes (attached only when a call fails/skips).

IF COL_LENGTH(N'dbo.AppAgentLibraryTool', N'CallNotes') IS NULL
    ALTER TABLE dbo.AppAgentLibraryTool ADD CallNotes NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH(N'dbo.AppAgentToolRegister', N'CallNotes') IS NULL
    ALTER TABLE dbo.AppAgentToolRegister ADD CallNotes NVARCHAR(MAX) NULL;
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Aggregate a cached dataset (count/sum/avg/min/max/distinct/top/distribution, optional group_by). Required: user_ask (the user analysis words), operation. Prefer dataset_name from a prior cache.',
    CallNotes = N'Analysis-like asks (analyze / group by / ranges / breakdown / multi-part metrics):
1) Call ask_user FIRST (contextKey=confirm_kpi_dashboard, mode=single_choice) — options kpi_dashboard vs fetch_display. Bias kpi_dashboard.
2) Call data_analyze ONLY after kpi_dashboard. Pass user_ask = the user words that request analysis.
3) Prefer operation=count|distribution with group_by for each named dimension. Do not use operation=summary on an already GROUP BY SQL result (TotalCount/Count) — render with yField=measure instead.
4) fetch_display: do not call this tool; use render_* / export_*.
Never invent analysis tables in FinalResponse when this tool exists.'
WHERE ToolName = N'data_analyze';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Aggregate a cached dataset (count/sum/avg/min/max/distinct/top/distribution, optional group_by). Required: user_ask (the user analysis words), operation. Prefer dataset_name from a prior cache.',
    CallNotes = N'Analysis-like asks: ask_user confirm_kpi_dashboard first (kpi_dashboard vs fetch_display). Call this tool only after kpi_dashboard. Prefer count|distribution + group_by. fetch_display: render/export only.'
WHERE ToolName = N'data_analyze';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Tables and columns for a DataSource (cached ~1h). Pass dataSourceId or omit for session default. Prefer get_table_schema for one known table.',
    CallNotes = N'Call once per DataSource when table/column names are unknown. If schema for this DataSource was already loaded in this chat, do not call again unless switching DB or the user asks to refresh. Prefer get_table_schema for a single known table.'
WHERE ToolName = N'get_database_schema';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Tables and columns for a DataSource (cached ~1h). Pass dataSourceId or omit for session default. Prefer get_table_schema for one known table.'
WHERE ToolName = N'get_database_schema';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Render interactive Result. Prefer dataset_name. layout=form|table|cards|kpi|chart|dashboard. specJson for kpi/chart/dashboard. Do not send HTML.',
    CallNotes = N'layout=form|table|cards freely. layout=kpi|chart|dashboard only after ask_user picks kpi_dashboard (contextKey=confirm_kpi_dashboard), then specJson. Prefer dataset_name. Do not send HTML. For cards/table field polish: prepare_ui_render_polish then cache_ui_render_polish (structure only).'
WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'render_interactive';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Render Result HTML (none-interactive). Prefer dataset_name. layout=form|table|cards|kpi|chart|dashboard. specJson for kpi/chart/dashboard. Do not send HTML.',
    CallNotes = N'layout=form|table|cards freely. layout=kpi|chart|dashboard only after ask_user picks kpi_dashboard (contextKey=confirm_kpi_dashboard), then specJson. Prefer dataset_name. Do not send HTML. For cards/table field polish: prepare_ui_render_polish then cache_ui_render_polish (structure only).'
WHERE LibraryKey = N'platform-ui-render' AND ToolName = N'render_none_interactive';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Deprecated alias of render_interactive / render_none_interactive. ui=grid|card|chart|kpi maps to layout table|cards|chart|kpi.',
    CallNotes = N'Deprecated. Prefer render_interactive or render_none_interactive. ui=grid→table, card→cards, chart→chart, kpi/kpi_dashboard→kpi|dashboard.'
WHERE ToolName = N'data_render';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Return templates, column schema, and up to 10 sample rows for Result polish. Structure only — do not merge rows into HTML.',
    CallNotes = N'Returns templates + ≤10 sample rows. Do not merge sample/full rows into HTML. Then cache_ui_render_polish (structure only) and render_* so GENERATE binds all N rows.'
WHERE ToolName = N'prepare_ui_render_polish';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Cache AiPolishedRenderSpec (templates + bindings, no rows). Then call render_interactive or render_none_interactive.',
    CallNotes = N'Structure only — never include result rows. After caching, call render_interactive or render_none_interactive with dataset_name.'
WHERE ToolName = N'cache_ui_render_polish';
GO
