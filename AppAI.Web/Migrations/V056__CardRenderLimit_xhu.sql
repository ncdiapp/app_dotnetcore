-- V056: card view and card export take limit so "top 20" is 20 rows, not the whole cache.
-- PDF layout=cards uses that panel. Do not rewrite the rows into content.

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
When a tool result includes dataset_cached and dataset_name, call data_render ONCE with that dataset_name. Use ui=card and cardsPerRow (1-6) when the user asked for cards. When the user asked for the top N, also pass limit=N. The panel shows the first N cached rows, not the whole result. Do NOT copy rows into dataJson — image columns such as Sketch are dropped.
Pass dataJson only when there is no dataset_name.
To export that card panel, call export_pdf / export_docx / export_pptx / export_excel / export_html with layout=cards, the same cardsPerRow, the same limit, and cardRows for PDF, Word, and PowerPoint (2 and 2 is four cards per page). Do not pass content.
For KPI / dashboard requests: one call with ui=kpi_dashboard and blocksJson. Limits: kpi items<=12, chart blocks<=9, grid blocks<=3.',
    ParameterSchemaJson = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"dataset_name":{"type":"string","description":"Session cache name from a prior tool result (dataset_cached / dataset_name). Loads every column, including image fields. Do not also pass dataJson."},"limit":{"type":"integer","description":"First N rows only. Required when the user asked for the top N, for example top 20 means limit=20. Omit to show the whole cache."},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Only when dataset_name is omitted. JSON array of rows (one card per row) or one card object."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]. Ignored when dataset_name is set."},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,cardsPerRow}. cardsPerRow 1-6, default 1."},"cardsPerRow":{"type":"integer","description":"Card view only. How many cards share one row. Default 1. Max 6."}}'
WHERE LibraryKey = N'data-ui-render'
  AND ToolName = N'data_render';
GO

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
When a tool result includes dataset_cached and dataset_name, call data_render ONCE with that dataset_name. Use ui=card and cardsPerRow (1-6) when the user asked for cards. When the user asked for the top N, also pass limit=N. The panel shows the first N cached rows, not the whole result. Do NOT copy rows into dataJson — image columns such as Sketch are dropped.
Pass dataJson only when there is no dataset_name.
To export that card panel, call export_pdf / export_docx / export_pptx / export_excel / export_html with layout=cards, the same cardsPerRow, the same limit, and cardRows for PDF, Word, and PowerPoint (2 and 2 is four cards per page). Do not pass content.
For KPI / dashboard requests: one call with ui=kpi_dashboard and blocksJson. Limits: kpi items<=12, chart blocks<=9, grid blocks<=3.',
    ParameterSchemaJson = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"dataset_name":{"type":"string","description":"Session cache name from a prior tool result (dataset_cached / dataset_name). Loads every column, including image fields. Do not also pass dataJson."},"limit":{"type":"integer","description":"First N rows only. Required when the user asked for the top N, for example top 20 means limit=20. Omit to show the whole cache."},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Only when dataset_name is omitted. JSON array of rows (one card per row) or one card object."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]. Ignored when dataset_name is set."},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,cardsPerRow}. cardsPerRow 1-6, default 1."},"cardsPerRow":{"type":"integer","description":"Card view only. How many cards share one row. Default 1. Max 6."}}'
WHERE ToolName = N'data_render'
  AND ToolType = N'BuiltIn';
GO

DECLARE @exportSchema nvarchar(max) = N'{"path":{"type":"string","description":"File name or session-relative path. A bare name is saved under output/. Extension is added when missing. The saved name adds _yyyyMMdd_HHmmss before the extension. Tell the user the relativePath from the result.","required":true},"content":{"type":"string","description":"Form JSON. For layout=cards, omit this. The file uses the card panel already shown in this chat."},"sourcePath":{"type":"string","description":"Session file used when there is no card panel and dataset_name is omitted."},"dataset_name":{"type":"string","description":"Chat session cache name. For layout=cards, omit it when a card panel was just rendered; that panel is exported. Pass it with limit to slice the cache."},"layout":{"type":"string","description":"form (default), table, or cards. cards is the chat card panel: one record per card, including image fields."},"records":{"type":"string","description":"Do not pass for layout=cards. The card panel is used."},"preview":{"type":"boolean","description":"True only when the user wants to see the document before export. Returns HTML and does not write the office file."},"cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6. For a PDF page of 2x2, pass 2."},"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. Card rows on one page. For 2x2, pass 2. Default 3. Ignored by HTML and Excel."},"limit":{"type":"integer","description":"First N cards. Pass the same limit as data_render. Top 20 means limit=20."}}';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @exportSchema,
    ToolDescription = CASE ToolName
        WHEN N'export_html' THEN N'Write email-safe HTML. layout=cards exports the chat card panel (or dataset_name with limit). Pass cardsPerRow. Do not rewrite rows into content.'
        WHEN N'export_csv' THEN N'Write UTF-8 CSV. layout=cards is one flat row per card from the chat card panel. Pass limit for top N. Do not rewrite rows into content.'
        WHEN N'export_excel' THEN N'Write one .xlsx sheet. layout=cards exports the chat card panel with cardsPerRow and limit. Do not rewrite rows into content.'
        WHEN N'export_docx' THEN N'Write Word. layout=cards uses the chat card panel, cardRows by cardsPerRow on each page, and limit for top N. Do not rewrite rows into content.'
        WHEN N'export_pdf' THEN N'Write PDF. layout=cards uses the chat card panel, not a rewritten summary. cardRows and cardsPerRow set the page (2 and 2 is four cards per page). Pass the same limit as data_render. Do not pass content.'
        WHEN N'export_pptx' THEN N'Write PowerPoint. layout=cards uses the chat card panel, one slide per page of cardRows by cardsPerRow, and limit for top N. Do not rewrite rows into content.'
        ELSE ToolDescription
    END
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_csv', N'export_excel', N'export_docx', N'export_pdf', N'export_pptx');
GO
