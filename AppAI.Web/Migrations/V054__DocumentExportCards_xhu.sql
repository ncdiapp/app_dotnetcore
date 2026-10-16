-- V054: document export accepts the chat dataset and card layout.
-- dataset_name reads the session cache. layout=cards uses cardsPerRow.
-- PDF, Word, and PowerPoint also use cardRows (card rows per page).

DECLARE @schema nvarchar(max) = N'{"path":{"type":"string","description":"File name or session-relative path. A bare name is saved under output/. Extension is added when missing. The saved name adds _yyyyMMdd_HHmmss before the extension. Tell the user the relativePath from the result.","required":true},"content":{"type":"string","description":"Form JSON, or card records. Ignored when dataset_name is set."},"sourcePath":{"type":"string","description":"Session file (.json, .csv, .xlsx, .html) used when dataset_name and content are omitted."},"dataset_name":{"type":"string","description":"Chat session cache name from a query, API, or data_render. Loads every cached row. Wins over content."},"layout":{"type":"string","description":"form (default), table, or cards. cards is one record per card, same rows as data_render ui=card."},"records":{"type":"string","description":"JSON array of card records when dataset_name is omitted."},"preview":{"type":"boolean","description":"True only when the user wants to see the document before export. Returns HTML and does not write the office file."},"cardsPerRow":{"type":"integer","description":"Cards sharing one row. 1-6, default 1. Same value as data_render cardsPerRow. HTML and Excel use only this."},"cardRows":{"type":"integer","description":"PDF, Word, and PowerPoint only. Card rows on one page. Default 3. Example 3 and cardsPerRow 2 is 6 cards per page. Ignored by HTML and Excel."}}';

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = @schema,
    ToolDescription = CASE ToolName
        WHEN N'export_html' THEN N'Write email-safe HTML. layout=form is the transaction form. layout=cards places cardsPerRow cards on each row, with image fields as img. Pass dataset_name to export the full chat cache.'
        WHEN N'export_csv' THEN N'Write UTF-8 CSV. layout=cards becomes one flat row per card. Pass dataset_name to export the full chat cache.'
        WHEN N'export_excel' THEN N'Write one .xlsx sheet. layout=form is the form grid. layout=cards places cardsPerRow cards on each sheet row. Pass dataset_name to export the full chat cache.'
        WHEN N'export_docx' THEN N'Write Word. layout=cards uses cardRows by cardsPerRow on each page. Pass dataset_name to export the full chat cache.'
        WHEN N'export_pdf' THEN N'Write PDF from the same model as Word. layout=cards uses cardRows by cardsPerRow on each page. Pass dataset_name to export the full chat cache.'
        WHEN N'export_pptx' THEN N'Write PowerPoint. layout=cards uses one slide per page of cardRows by cardsPerRow. Pass dataset_name to export the full chat cache.'
        ELSE ToolDescription
    END
WHERE LibraryKey = N'platform-document-export'
  AND ToolName IN (N'export_html', N'export_csv', N'export_excel', N'export_docx', N'export_pdf', N'export_pptx');
GO
