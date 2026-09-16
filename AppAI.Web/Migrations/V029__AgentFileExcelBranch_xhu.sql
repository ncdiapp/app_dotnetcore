-- V029: agent-files file_read / file_write — Excel (.xlsx/.xls) via GemBox; multi-sheet-safe write API.
-- Runtime code: AgentFilePlugin + GenericAgentExcelFileHelper.

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Read a file from the chat file area. Text: UTF-8 content. .xlsx/.xls: returns SheetNames and Sheets[{Name,Headers,Rows}] for all worksheets.',
    ParameterSchemaJson = N'{"path":{"type":"string","description":"Relative file path","required":true}}'
WHERE LibraryKey = N'agent-files' AND ToolName = N'file_read';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Write a file into the chat file area. Text: UTF-8 body. .xlsx/.xls: JSON content with mode append|overwrite_sheet|replace_file. overwrite_sheet updates named sheet(s) and keeps other sheets; replace_file recreates the whole workbook. Multi-sheet: {"mode":"overwrite_sheet","sheets":[{"name":"...","headers":[...],"rows":[[...]]}]}. Use real cell values in headers/rows — not markdown table text.',
    ParameterSchemaJson = N'{"path":{"type":"string","description":"Relative file path","required":true},"content":{"type":"string","description":"Text body, or Excel JSON {mode,sheet|sheets,headers,rows}","required":true}}'
WHERE LibraryKey = N'agent-files' AND ToolName = N'file_write';
GO
