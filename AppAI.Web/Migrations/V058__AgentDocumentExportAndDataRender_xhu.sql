-- Merged V050, V053–V057, V059, V060.
-- Final tool text only. Earlier drafts of data_render and the export tools are not repeated.
-- Card page layout is generic (cardsPerRow, cardRows, optional pageGrid / cardsPerPage) — no fixed 2x2 example in tool text.
-- Safe to run after those scripts: inserts are skipped when the row exists, then descriptions are updated.

IF EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-documents')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-document-export')
BEGIN
    INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
    SELECT N'platform-document-export', DomainKey, N'Platform Document Export', Description, ToolCategory, IsActive
    FROM dbo.AppAgentToolLibrary
    WHERE LibraryKey = N'agent-documents';

    UPDATE dbo.AppAgentLibraryTool
    SET LibraryKey = N'platform-document-export'
    WHERE LibraryKey = N'agent-documents';

    UPDATE dbo.AppAgentLibrarySubscription
    SET LibraryKey = N'platform-document-export'
    WHERE LibraryKey = N'agent-documents';

    IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
        UPDATE dbo.AppAgentToolExclusion
        SET LibraryKey = N'platform-document-export'
        WHERE LibraryKey = N'agent-documents';

    IF OBJECT_ID(N'dbo.AppAgentToolCatalog', N'U') IS NOT NULL
        UPDATE dbo.AppAgentToolCatalog
        SET LibraryKey = N'platform-document-export'
        WHERE LibraryKey = N'agent-documents';

    DELETE FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-documents';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-document-export')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-document-export',
    N'platform',
    N'Platform Document Export',
    N'Write and read form-shaped documents (HTML, CSV, Excel, Word, PDF, PowerPoint) from a title, fields, and sections. The model does not send HTML or fonts.',
    N'BuiltIn',
    1
);
GO

DECLARE @type nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentDocumentPlugin';
DECLARE @exportSchema nvarchar(max) = N'{"path":{"type":"string","description":"File name or session-relative path. A bare name is saved under output/. Extension is added when missing. The saved name adds _yyyyMMdd_HHmmss before the extension. Tell the user the relativePath from the result.","required":true},"content":{"type":"string","description":"Form JSON. Omit when dataset_name is set. Do not paste a short sample of a cached dataset."},"sourcePath":{"type":"string","description":"Session file used when dataset_name and content are omitted."},"dataset_name":{"type":"string","description":"Chat session cache name. Loads every cached row. Wins over content and over the card panel. Pass this instead of copying rows."},"layout":{"type":"string","description":"form (default), table, or cards."},"records":{"type":"string","description":"Card records when dataset_name is omitted. Do not pass a short copy of a cached dataset."},"preview":{"type":"boolean","description":"True only when the user wants to see the document before export. Returns HTML and does not write the office file."},"cardsPerRow":{"type":"integer","description":"Cards sharing one row (columns). 1-6."},"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. Card rows on one page. Default 3. Prefer pageGrid when the user names columns×rows."},"pageGrid":{"type":"string","description":"PDF, Word, and PowerPoint cards. Optional columns×rows string (e.g. 3x2). Wins over cardsPerRow and cardRows. HTML uses cardsPerRow only."},"cardsPerPage":{"type":"integer","description":"PDF, Word, and PowerPoint. Optional total cards per page; derives cardRows from cardsPerRow. Wins over cardRows."},"limit":{"type":"integer","description":"First N rows after the cache is chosen. Omit to write the whole cache."}}';
DECLARE @readSchema nvarchar(200) = N'{"path":{"type":"string","description":"Session-relative file path.","required":true}}';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'export_html')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'export_html',
    N'Write email-safe HTML from the built-in template. Tables and inline styles, no style block. Pass dataset_name for the full cache. Do not paste rows into content.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportHtml"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_html')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'read_html',
    N'Read an HTML form this library wrote. Returns the form tree. Preview is capped at 500 data rows.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadHtml"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'export_csv')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'export_csv',
    N'Write UTF-8 CSV. No template. Pass dataset_name for the full cache.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportCsv"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_csv')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'read_csv',
    N'Read a CSV this library wrote back into a form tree.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadCsv"}', 1, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'export_excel')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'export_excel',
    N'Write one .xlsx sheet with the fixed style table. No template. Pass dataset_name for the full cache.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportExcel"}', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_excel')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'read_excel',
    N'Read an .xlsx form this library wrote. .xls is read with GemBox as a single grid.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadExcel"}', 1, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'export_docx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'export_docx',
    N'Write Word by converting the built-in HTML template with LibreOffice. Pass dataset_name for the full cache. Do not paste rows into content.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportDocx"}', 1, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_docx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'read_docx',
    N'Read a Word file this library wrote. LibreOffice converts it back to HTML and the form tree is read from data-role.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadDocx"}', 1, 7);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'export_pdf')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'export_pdf',
    N'Write PDF by printing the built-in HTML template with Playwright. Pass dataset_name for the full cache. To PDF an existing Word file, use convert_docx_to_pdf.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportPdf"}', 1, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_pdf')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'read_pdf',
    N'Read plain text from a PDF with PdfPig. Text is capped. Does not return the form tree.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadPdf"}', 1, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'export_pptx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'export_pptx',
    N'Write PowerPoint with ShapeCrawler. Font and color follow the built-in template. Pass dataset_name for the full cache.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportPptx"}', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_pptx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-document-export', N'read_pptx',
    N'Read titles, text, and tables from a PowerPoint file. Pictures and charts are skipped.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadPptx"}', 1, 11);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-document-export' AND ToolName = N'convert_docx_to_pdf')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-document-export',
    N'convert_docx_to_pdf',
    N'Convert an existing session Word file to PDF with LibreOffice. Use this only when the input is already a .docx. A data export to PDF is export_pdf.',
    N'{"sourcePath":{"type":"string","description":"Session-relative .docx path.","required":true},"path":{"type":"string","description":"PDF file name. A bare name is saved under output/."}}',
    N'BuiltIn',
    N'{"TypeName":"' + @type + N'","MethodName":"ConvertDocxToPdf"}',
    1,
    20
);

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @exportSchema,
    ToolDescription = CASE ToolName
        WHEN N'export_html' THEN N'Write email-safe HTML from the built-in template. Tables and inline styles, no style block. Pass dataset_name for the full cache. Do not paste rows into content.'
        WHEN N'export_csv' THEN N'Write UTF-8 CSV. No template. Pass dataset_name for the full cache.'
        WHEN N'export_excel' THEN N'Write one .xlsx sheet with the fixed style table. No template. Pass dataset_name for the full cache.'
        WHEN N'export_docx' THEN N'Write Word. Uses LibreOffice when installed, otherwise Open XML. Pass dataset_name for the full cache. For layout=cards set cardsPerRow and cardRows (or pageGrid) to match the page the user asked for. Do not paste rows into content.'
        WHEN N'export_pdf' THEN N'Write PDF by printing the built-in HTML template with Playwright. Pass dataset_name for the full cache. For layout=cards set cardsPerRow and cardRows (or pageGrid) to match the page the user asked for. To PDF an existing Word file, use convert_docx_to_pdf.'
        WHEN N'export_pptx' THEN N'Write PowerPoint with ShapeCrawler. Font and color follow the built-in template. Pass dataset_name for the full cache. For layout=cards set cardsPerRow and cardRows (or pageGrid) to match the slide the user asked for.'
        ELSE ToolDescription
    END
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_csv', N'export_excel', N'export_docx', N'export_pdf', N'export_pptx');

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Read a Word file this library wrote. LibreOffice converts it back to HTML and the form tree is read from data-role.'
WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_docx';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Read plain text from a PDF with PdfPig. Text is capped. Does not return the form tree.'
WHERE LibraryKey = N'platform-document-export' AND ToolName = N'read_pdf';
GO

DECLARE @renderDesc nvarchar(max) = N'Present data in Agent Chat as an interactive UI panel. Non-blocking. ui=grid|card|chart|kpi_dashboard.
When a tool result includes dataset_cached and dataset_name, call data_render ONCE with that dataset_name. Use ui=card and cardsPerRow (1-6) when the user asked for cards. When the user asked for the top N, also pass limit=N. The panel shows the first N cached rows, not the whole result. Do NOT copy rows into dataJson — image columns such as Sketch are dropped.
Pass dataJson only when there is no dataset_name.
To export that card panel, call export_pdf / export_docx / export_pptx / export_excel / export_html with layout=cards, the same cardsPerRow, the same limit, and cardRows (or pageGrid) for PDF, Word, and PowerPoint so the page matches what the user asked for. Do not pass content.
For KPI / dashboard requests: one call with ui=kpi_dashboard and blocksJson. Limits: kpi items<=12, chart blocks<=9, grid blocks<=3.';

DECLARE @renderSchema nvarchar(max) = N'{"ui":{"type":"string","description":"grid | card | chart | kpi_dashboard","required":true},"dataset_name":{"type":"string","description":"Session cache name from a prior tool result (dataset_cached / dataset_name). Loads every column, including image fields. Do not also pass dataJson."},"limit":{"type":"integer","description":"First N rows only. Required when the user asked for the top N, for example top 20 means limit=20. Omit to show the whole cache."},"title":{"type":"string","description":"Optional panel title"},"dataJson":{"type":"string","description":"Only when dataset_name is omitted. JSON array of rows (one card per row) or one card object."},"blocksJson":{"type":"string","description":"Required for kpi_dashboard. JSON array of blocks: markdown|kpi|chart|grid|card."},"columnsJson":{"type":"string","description":"Optional grid columns [{field,header,dataType,width,hide}]. Ignored when dataset_name is set."},"chartConfigJson":{"type":"string","description":"Optional chart config {type,xField,yField,groupBy,allowedTypes}"},"actionsJson":{"type":"string","description":"Optional panel toolbar actions [{id,label}]"},"metaJson":{"type":"string","description":"Optional meta {subtitle,measure,total,cardsPerRow}. cardsPerRow 1-6, default 1."},"cardsPerRow":{"type":"integer","description":"Card view only. How many cards share one row. Default 1. Max 6."}}';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = @renderDesc,
    ParameterSchemaJson = @renderSchema
WHERE LibraryKey = N'data-ui-render'
  AND ToolName = N'data_render';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = @renderDesc,
    ParameterSchemaJson = @renderSchema
WHERE ToolName = N'data_render'
  AND ToolType = N'BuiltIn';
GO
