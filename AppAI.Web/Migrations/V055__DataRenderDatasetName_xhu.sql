-- V055: V053 replaced the data_render schema and dropped dataset_name.
-- Card view and PDF export then received a rewritten row subset, so Sketch and the other fields were lost.
-- dataset_name is required again. cardsPerRow stays.

DECLARE @desc nvarchar(max) = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
When a tool result includes dataset_cached and dataset_name, call data_render ONCE with that dataset_name. Use ui=card and cardsPerRow (1-6) when the user asked for cards. Do NOT copy rows into dataJson — image columns such as Sketch are dropped and the cards lose their pictures. dataset_name loads every cached row and column.
Pass dataJson only when there is no dataset_name.
To export that card view, call export_pdf / export_docx / export_pptx / export_excel / export_html with the same dataset_name, layout=cards, the same cardsPerRow, and cardRows for PDF, Word, and PowerPoint (2 and 2 is four cards per page). Do not rewrite the rows into content.
For KPI / dashboard requests: one call with ui=kpi_dashboard and blocksJson. Limits: kpi items<=12, chart blocks<=9, grid blocks<=3.';

DECLARE @schema nvarchar(max) = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"dataset_name":{"type":"string","description":"Session cache name from a prior tool result (dataset_cached / dataset_name). Required for search and query results. Loads every row and column, including image fields. Do not also pass dataJson."},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Only when dataset_name is omitted. JSON array of rows (one card per row) or one card object."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]. Ignored when dataset_name is set."},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,cardsPerRow}. cardsPerRow 1-6, default 1."},"cardsPerRow":{"type":"integer","description":"Card view only. How many cards share one row. Default 1. Max 6. Same value is passed to export as cardsPerRow."}}';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @desc,
    ParameterSchemaJson = @schema
WHERE LibraryKey = N'data-ui-render'
  AND ToolName = N'data_render';
GO

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
When a tool result includes dataset_cached and dataset_name, call data_render ONCE with that dataset_name. Use ui=card and cardsPerRow (1-6) when the user asked for cards. Do NOT copy rows into dataJson — image columns such as Sketch are dropped and the cards lose their pictures. dataset_name loads every cached row and column.
Pass dataJson only when there is no dataset_name.
To export that card view, call export_pdf / export_docx / export_pptx / export_excel / export_html with the same dataset_name, layout=cards, the same cardsPerRow, and cardRows for PDF, Word, and PowerPoint (2 and 2 is four cards per page). Do not rewrite the rows into content.
For KPI / dashboard requests: one call with ui=kpi_dashboard and blocksJson. Limits: kpi items<=12, chart blocks<=9, grid blocks<=3.',
    ParameterSchemaJson = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"dataset_name":{"type":"string","description":"Session cache name from a prior tool result (dataset_cached / dataset_name). Required for search and query results. Loads every row and column, including image fields. Do not also pass dataJson."},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Only when dataset_name is omitted. JSON array of rows (one card per row) or one card object."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]. Ignored when dataset_name is set."},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,cardsPerRow}. cardsPerRow 1-6, default 1."},"cardsPerRow":{"type":"integer","description":"Card view only. How many cards share one row. Default 1. Max 6. Same value is passed to export as cardsPerRow."}}'
WHERE ToolName = N'data_render'
  AND ToolType = N'BuiltIn';
GO
