-- Card grid: 2x2 means two columns and two rows on each PDF, Word, and PowerPoint page.
UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = REPLACE(ParameterSchemaJson,
    N'"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. Card rows on one page. Default 3."}',
    N'"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. How many card rows are on one page. cardsPerRow is the columns. 2x2 means cardsPerRow=2 and cardRows=2, four cards on the page. One row of two cards is cardsPerRow=2 and cardRows=1. Default 3."}')
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_csv', N'export_excel', N'export_docx', N'export_pdf', N'export_pptx')
  AND ParameterSchemaJson NOT LIKE N'%2x2 means cardsPerRow=2 and cardRows=2%';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = CASE ToolName
    WHEN N'export_docx' THEN N'Write Word. Uses LibreOffice when it is installed, otherwise Open XML. Pass dataset_name for the full cache. For cards, 2x2 means cardsPerRow=2 and cardRows=2 (four cards on each page). Do not set cardRows=1 for 2x2.'
    WHEN N'export_pdf' THEN N'Write PDF by printing the built-in HTML template with Playwright. Pass dataset_name for the full cache. For cards, 2x2 means cardsPerRow=2 and cardRows=2 (four cards on each page). Do not set cardRows=1 for 2x2. To PDF an existing Word file, use convert_docx_to_pdf.'
    WHEN N'export_pptx' THEN N'Write PowerPoint with ShapeCrawler. For cards, 2x2 means cardsPerRow=2 and cardRows=2 (four cards on each slide). Do not set cardRows=1 for 2x2. Pass dataset_name for the full cache.'
    ELSE ToolDescription
END
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_docx', N'export_pdf', N'export_pptx');
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = REPLACE(ToolDescription,
    N'cardRows for PDF, Word, and PowerPoint (2 and 2 is four cards per page).',
    N'cardRows for PDF, Word, and PowerPoint. 2x2 means cardsPerRow=2 and cardRows=2, four cards on each page. Do not set cardRows=1 for 2x2.')
WHERE LibraryKey = N'data-ui-render' AND ToolName = N'data_render'
  AND ToolDescription LIKE N'%2 and 2 is four cards per page%';

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = REPLACE(ToolDescription,
    N'cardRows for PDF, Word, and PowerPoint (2 and 2 is four cards per page).',
    N'cardRows for PDF, Word, and PowerPoint. 2x2 means cardsPerRow=2 and cardRows=2, four cards on each page. Do not set cardRows=1 for 2x2.')
WHERE ToolName = N'data_render'
  AND ToolDescription LIKE N'%2 and 2 is four cards per page%';
GO
