-- V062: Interactive KPI/dashboard (prefer render_interactive) + LLM chart.type selection.
-- Result: interactive Wijmo; export/PDF: render_none_interactive / export_* (SVG).
-- Chart types: bar | hbar | line | scatter | pie | donut | area — LLM picks default per chart.

DECLARE @chartGuide nvarchar(max) = N'AVAILABLE chart.type values: bar | hbar | line | scatter | pie | donut | area. For EACH chart, set type from the data meaning (do not leave blank and do not force every chart to bar). Guidance: few categories (2-5) share-of-total → pie or donut; many categories or long labels → hbar; time/period/season trend → line or area; two numeric measures correlation → scatter; simple category counts → bar. Prefer specJson.charts:[{type,xField,yField,title},...] when multiple dimensions; otherwise chart:{type,xField,yField,title}.';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Render Result. Prefer dataset_name. layout=form|table|cards freely. layout=kpi|chart|dashboard ONLY after ask_user picks kpi_dashboard. Prefer render_interactive for Result exploration; export/PDF use render_none_interactive or export_* (SVG). Pass specJson. '
    + @chartGuide
    + N' Do not send HTML.',
    ParameterSchemaJson = N'{"layout":{"type":"string","description":"form | table | cards | kpi | chart | dashboard. After kpi_dashboard: prefer layout=dashboard + render_interactive."},"dataset_name":{"type":"string","description":"Chat session cache name. Preferred."},"title":{"type":"string","description":"Optional title"},"content":{"type":"string","description":"Form JSON or row array when dataset_name is omitted."},"specJson":{"type":"string","description":"Required for kpi|chart|dashboard. {title?, kpis:[{label,field,op}], chart:{type,xField,yField,title}, charts:[{type,xField,yField,title}], blocks:[...]}. chart.type REQUIRED per chart when possible. AVAILABLE types: bar|hbar|line|scatter|pie|donut|area. Pick type from data meaning: pie/donut for few share categories; hbar for many/long labels; line/area for time; scatter for two numerics; bar for simple counts. op: count|distinct|sum|avg."},"limit":{"type":"integer","description":"First N rows after cache is chosen"},"cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6"},"cardRows":{"type":"integer","description":"Card rows on one page. Default 3"},"actionsJson":{"type":"string","description":"Interactive only: toolbar actions [{id,label}]"}}'
WHERE LibraryKey = N'platform-ui-render'
  AND ToolName IN (N'render_none_interactive', N'render_interactive');
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

## Interactive vs export for KPI/dashboard
After kpi_dashboard is confirmed: prefer render_interactive with layout=dashboard (or kpi/chart), dataset_name, and specJson — Result embeds a Wijmo page (grid filter/sort, chart type toggles). For export_pdf/docx/html or a static snapshot, use render_none_interactive or export_* with the same layout/specJson (SVG charts, no CDN). Do not invent HTML.'
WHERE IsActive = 1
  AND (SystemPrompt LIKE N'%After kpi_dashboard is confirmed%' OR SystemPrompt LIKE N'%Data analysis intent — ask the user%')
  AND SystemPrompt NOT LIKE N'%Interactive vs export for KPI/dashboard%';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

## Chart type selection (KPI / dashboard)
When building specJson for layout=kpi|chart|dashboard, set chart.type (or charts[].type) for each visualization. Available types: bar, hbar, line, scatter, pie, donut, area.
Choose the default type from the chart meaning and rough data shape — not always bar. Examples: classification with ~3-6 values → pie/donut; size range with long labels or many buckets → hbar; season/month over time → line; plain category counts → bar. User can still switch types in the Result UI.'
WHERE IsActive = 1
  AND (SystemPrompt LIKE N'%Interactive vs export for KPI/dashboard%' OR SystemPrompt LIKE N'%After kpi_dashboard is confirmed%')
  AND SystemPrompt NOT LIKE N'%Chart type selection (KPI / dashboard)%';
GO
