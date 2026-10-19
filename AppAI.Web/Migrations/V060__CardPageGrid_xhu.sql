-- pageGrid copies the user's layout. 2x2 is two columns and two rows, four cards on the page.
DECLARE @grid nvarchar(max) = N',"pageGrid":{"type":"string","description":"PDF, Word, and PowerPoint cards. Copy the user layout, for example 2x2. 2x2 means two columns and two rows, four cards on that page. 2 cards per page is 2x1. This wins over cardsPerRow and cardRows. HTML uses cardsPerRow only."},"cardsPerPage":{"type":"integer","description":"PDF, Word, and PowerPoint. How many cards are on one page. 4 with cardsPerRow=2 is two rows. Wins over cardRows. Do not set cardRows=1 when cardsPerPage is 4."}';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = REPLACE(ParameterSchemaJson,
    N'"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. How many card rows are on one page. cardsPerRow is the columns. 2x2 means cardsPerRow=2 and cardRows=2, four cards on the page. One row of two cards is cardsPerRow=2 and cardRows=1. Default 3."}',
    N'"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. How many card rows are on one page. Prefer pageGrid. 2x2 means cardsPerRow=2 and cardRows=2. Default 3."}' + @grid)
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_csv', N'export_excel', N'export_docx', N'export_pdf', N'export_pptx')
  AND ParameterSchemaJson NOT LIKE N'%"pageGrid"%'
  AND ParameterSchemaJson LIKE N'%2x2 means cardsPerRow=2 and cardRows=2, four cards on the page%';
GO

DECLARE @grid nvarchar(max) = N',"pageGrid":{"type":"string","description":"PDF, Word, and PowerPoint cards. Copy the user layout, for example 2x2. 2x2 means two columns and two rows, four cards on that page. 2 cards per page is 2x1. This wins over cardsPerRow and cardRows. HTML uses cardsPerRow only."},"cardsPerPage":{"type":"integer","description":"PDF, Word, and PowerPoint. How many cards are on one page. 4 with cardsPerRow=2 is two rows. Wins over cardRows. Do not set cardRows=1 when cardsPerPage is 4."}';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = REPLACE(ParameterSchemaJson,
    N'"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. Card rows on one page. Default 3."}',
    N'"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. How many card rows are on one page. Prefer pageGrid. Default 3."}' + @grid)
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_csv', N'export_excel', N'export_docx', N'export_pdf', N'export_pptx')
  AND ParameterSchemaJson NOT LIKE N'%"pageGrid"%'
  AND ParameterSchemaJson LIKE N'%Card rows on one page. Default 3.%';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = CASE ToolName
    WHEN N'export_docx' THEN N'Write Word. Card pages keep the requested rows and columns. For 2x2 pass pageGrid=2x2 (two columns, two rows, four cards on each page). Do not pass cardRows=1 for 2x2. Pass dataset_name for the full cache.'
    WHEN N'export_pdf' THEN N'Write PDF. Card pages keep the requested rows and columns. For 2x2 pass pageGrid=2x2 (two columns, two rows, four cards on each page). Do not pass cardRows=1 for 2x2. Pass dataset_name for the full cache.'
    WHEN N'export_pptx' THEN N'Write PowerPoint. For 2 cards per page pass pageGrid=2x1. For 2x2 pass pageGrid=2x2. Pass dataset_name for the full cache.'
    ELSE ToolDescription
END
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_docx', N'export_pdf', N'export_pptx');
GO
