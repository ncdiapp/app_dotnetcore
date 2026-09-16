-- V029: agent-files file_read / file_write describe Excel auto-branch (.xlsx/.xls via GemBox).
-- Code change is in AgentFilePlugin + GenericAgentExcelFileHelper; this updates LLM-facing tool text.

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Read a file from the chat file area (relative to AgentOutput/{sessionKey}/). Text files return UTF-8 content. .xlsx/.xls return tabular JSON (sheet, headers, rows) via GemBox — not binary.',
    ParameterSchemaJson = N'{"path":{"type":"string","description":"Relative file path","required":true}}'
WHERE LibraryKey = N'agent-files' AND ToolName = N'file_read';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Write a file into the chat file area. Text: UTF-8 body. .xlsx/.xls: pass JSON {"mode":"append|overwrite","sheet":"Log","headers":["A","B"],"rows":[["v1","v2"]]} or plain/CSV lines (plain one-line log becomes a Message column). Creates real Excel via GemBox — do not write binary as text.',
    ParameterSchemaJson = N'{"path":{"type":"string","description":"Relative file path","required":true},"content":{"type":"string","description":"Text body, or for Excel: JSON {mode,sheet,headers,rows} or CSV/TSV/plain log lines","required":true}}'
WHERE LibraryKey = N'agent-files' AND ToolName = N'file_write';
GO
