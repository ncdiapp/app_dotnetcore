-- V029: agent-files file_read / file_write describe Excel auto-branch (.xlsx/.xls via GemBox).
-- Code change is in AgentFilePlugin + GenericAgentExcelFileHelper; this updates LLM-facing tool text.


UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Read a file from the chat file area. Text: UTF-8. .xlsx/.xls: returns SheetNames + Sheets[{Name,Headers,Rows}] for ALL worksheets (GemBox).',
    ParameterSchemaJson = N'{"path":{"type":"string","description":"Relative file path","required":true}}'
WHERE LibraryKey = N'agent-files' AND ToolName = N'file_read';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Write a file. Text: UTF-8. Excel (.xlsx/.xls): JSON only. Modes: append (add rows), overwrite_sheet (replace ONE/MORE named sheets, KEEP other sheets), replace_file (wipe whole workbook — rare). Single: {"mode":"overwrite_sheet","sheet":"Japanese","headers":["A"],"rows":[["v"]]}. Multi: {"mode":"overwrite_sheet","sheets":[{"name":"Japanese","headers":[...],"rows":[...]},{"name":"Korean",...}]}. Never put user instructions or markdown tables into one cell. Never use replace_file to add sheets.',
    ParameterSchemaJson = N'{"path":{"type":"string","description":"Relative file path (.xlsx to use Excel)","required":true},"content":{"type":"string","description":"Excel: JSON {mode,sheet|sheets,headers,rows}. Text: body.","required":true}}'
WHERE LibraryKey = N'agent-files' AND ToolName = N'file_write';
GO
