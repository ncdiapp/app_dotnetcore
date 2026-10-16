-- V052: agent-documents library. Form-shaped export and read for HTML, CSV, Excel, Word, PDF, PowerPoint.
-- Subscribe an agent separately. These rows do not attach themselves.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-documents')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'agent-documents',
    N'platform',
    N'Agent Documents',
    N'Write and read form-shaped documents (HTML, CSV, Excel, Word, PDF, PowerPoint) from a title, fields, and sections. The model does not send HTML or fonts.',
    N'BuiltIn',
    1
);
GO

DECLARE @type nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentDocumentPlugin';
DECLARE @exportSchema nvarchar(max) = N'{"path":{"type":"string","description":"Session-relative file path. Extension is added when missing.","required":true},"content":{"type":"string","description":"Form JSON: title, fields[{label,value}], sections[{title,fields,tables[{columns,rows}],sections}], notes. Pass this or sourcePath."},"sourcePath":{"type":"string","description":"Session file (.json, .csv, .xlsx, .html) to export instead of content."}}';
DECLARE @readSchema nvarchar(200) = N'{"path":{"type":"string","description":"Session-relative file path.","required":true}}';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'export_html')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'export_html',
    N'Write an email-safe HTML file (tables and inline styles only, no flex) laid out like a transaction form: title, field pairs, sections, grids.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportHtml"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'read_html')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'read_html',
    N'Read an HTML form this library wrote. Returns the form tree. Preview is capped at 500 data rows.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadHtml"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'export_csv')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'export_csv',
    N'Write UTF-8 CSV (BOM) for the form values. No fonts. A one-cell row is a title or section name.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportCsv"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'read_csv')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'read_csv',
    N'Read a CSV this library wrote back into a form tree.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadCsv"}', 1, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'export_excel')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'export_excel',
    N'Write one .xlsx worksheet laid out as a form: merged title, two field pairs per row, section bands, grids. Open XML, not the GemBox row cap.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportExcel"}', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'read_excel')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'read_excel',
    N'Read an .xlsx form this library wrote. .xls is read with GemBox as a single grid.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadExcel"}', 1, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'export_docx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'export_docx',
    N'Write a Word file with the same heading sizes, field pairs, and grids as the HTML form.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportDocx"}', 1, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'read_docx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'read_docx',
    N'Read a Word form this library wrote.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadDocx"}', 1, 7);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'export_pdf')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'export_pdf',
    N'Write a PDF from the same Word layout (title, field pairs, sections, grids).',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportPdf"}', 1, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'read_pdf')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'read_pdf',
    N'Read plain text and a page count from a PDF. Does not rebuild tables.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadPdf"}', 1, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'export_pptx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'export_pptx',
    N'Write a PowerPoint file: a title slide, then one slide per section. Grids split after 12 rows.',
    @exportSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ExportPptx"}', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-documents' AND ToolName = N'read_pptx')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'agent-documents', N'read_pptx',
    N'Read titles, text, and tables from a PowerPoint file. Pictures and charts are skipped.',
    @readSchema, N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ReadPptx"}', 1, 11);
GO
