-- V061: After kpi_dashboard confirm, shape metrics then render KPIs + charts (not raw table-only).

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Render Result iframe HTML. Prefer dataset_name. layout=form|table|cards freely. layout=kpi|chart|dashboard ONLY after ask_user picks kpi_dashboard (contextKey=confirm_kpi_dashboard). Then: data_analyze (count / distribution / group_by on the dimensions the user named) and/or render with specJson — kpis use op=count|distinct|sum|avg; chart uses xField + yField (or yField=count on raw rows). Prefer layout=dashboard with kpis + chart (not table-only). Do not send HTML.',
    ParameterSchemaJson = N'{"layout":{"type":"string","description":"form | table | cards | kpi | chart | dashboard. Default table. layout=kpi|chart|dashboard ONLY after ask_user option kpi_dashboard. After confirm: prefer dashboard with kpis+chart; categorical KPIs use op=distinct or count; chart yField=count groups raw rows by xField."},"dataset_name":{"type":"string","description":"Chat session cache name. Preferred."},"title":{"type":"string","description":"Optional title"},"content":{"type":"string","description":"Form JSON or row array when dataset_name is omitted. For chart from data_analyze distribution, pass shaped rows [{key,count}] as content with chart xField=key yField=count."},"specJson":{"type":"string","description":"Required for layout kpi|chart|dashboard. {title?, kpis:[{label,field,op}], chart:{type,xField,yField,title}, blocks:[{type:kpi|chart|table,...}]}. op: count|distinct|sum|avg|min|max. yField omit or count = row count per x category."},"limit":{"type":"integer","description":"First N rows after cache is chosen"},"cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6"},"cardRows":{"type":"integer","description":"Card rows on one page. Default 3"},"actionsJson":{"type":"string","description":"Interactive only: toolbar actions [{id,label}]"}}'
WHERE LibraryKey = N'platform-ui-render'
  AND ToolName IN (N'render_none_interactive', N'render_interactive');
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

## After kpi_dashboard is confirmed
1) Call data_analyze for count and for distribution/group_by on each dimension the user named.
2) Render once with layout=dashboard (or kpi), dataset_name (or shaped content), and specJson that includes kpis AND at least one chart. KPI op=count for totals; op=distinct for category fields; do not use sum on text dimensions. Chart: xField=dimension, yField=count (works on raw rows) or pass distribution rows with xField=key yField=count. Do not stop at a raw data table with empty KPI cards.'
WHERE IsActive = 1
  AND (SystemPrompt LIKE N'%Data analysis intent — ask the user%' OR SystemPrompt LIKE N'%render_none_interactive%')
  AND SystemPrompt NOT LIKE N'%After kpi_dashboard is confirmed%';
GO
