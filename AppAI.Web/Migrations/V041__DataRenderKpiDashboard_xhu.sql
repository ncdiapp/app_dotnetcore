-- V041: data_render ui=kpi_dashboard (blocksJson composite dashboard).
-- Idempotent UPDATE of library + tool metadata; prompt hint for subscribed agents.

UPDATE dbo.AppAgentToolLibrary
SET Description = N'Present grid/card/chart or a composite kpi_dashboard in Agent Chat (non-blocking). Trigger kpi_dashboard when the user asks for KPI, dashboard, analysis, analyze, overview, summary, or similar. Subscribe per agent.'
WHERE LibraryKey = N'data-ui-render';
GO

UPDATE dbo.AppAgentLibraryTool
SET
    ToolDescription = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
For KPI / dashboard / analysis / analyze / overview / summary requests: prefer ONE call with ui=kpi_dashboard and blocksJson (array). Model chooses how many markdown/kpi/chart/grid/card blocks (limits: kpi items<=12, chart blocks<=9, grid blocks<=3). Panel actionsJson and per-block actions allowed. Short FinalResponse text OK in addition to dashboard narrative. Single table/chart may still use ui=grid|chart.
Pass rows as dataJson for grid/card/chart. For kpi_dashboard pass blocksJson: [{type,id?,title?,content?,items?,data?,columns?,chartConfig?,actions?,meta?}]. type=markdown|kpi|chart|grid|card.',
    ParameterSchemaJson = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Required for grid/card/chart. JSON array of rows or card object. For kpi_dashboard optional summary / blocks fallback."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card. kpi.items[{label,value,hint}], chart/grid use data+chartConfig/columns, optional per-block actions[{id,label}]."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]"},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,...}"}}'
WHERE LibraryKey = N'data-ui-render'
  AND ToolName = N'data_render';
GO

-- Prompt guidance for agents already subscribed to data-ui-render
DECLARE @hint NVARCHAR(MAX) = N'

## data_render / KPI dashboard
When the user asks for KPI, dashboard, analysis, analyze, overview, summary, or similar insight views: call data_render once with ui=kpi_dashboard and blocksJson. Choose markdown + kpi cards + charts + optional grids as needed (do not ask the user for layout). Limits: at most 12 kpi items, 9 chart blocks, 3 grid blocks per dashboard. You may also write a short FinalResponse narrative. For a single plain table or chart, ui=grid or ui=chart is fine. Do not dump large result tables only as text.';
 
UPDATE s
SET SystemPrompt = CASE
    WHEN s.SystemPrompt IS NULL OR LTRIM(RTRIM(s.SystemPrompt)) = N'' THEN LTRIM(@hint)
    WHEN s.SystemPrompt LIKE N'%ui=kpi_dashboard%' THEN s.SystemPrompt
    ELSE s.SystemPrompt + @hint
END
FROM dbo.AppAgentSkillSet s
INNER JOIN dbo.AppAgentLibrarySubscription sub
    ON sub.SkillKey = s.SkillKey AND sub.LibraryKey = N'data-ui-render';
GO
