-- V032: ask_user fieldsJson supports per-field select (DDL).
-- Platform HITL only — no PLM / AppPlmImportSession schema changes.

IF EXISTS (
    SELECT 1 FROM dbo.AppAgentLibraryTool
    WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user')
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Ask the user a structured question and wait for their answer (Interactive only). Use for Gate-0 / missing fields / menus. mode=text|single_choice|multi_choice. fieldsJson supports type=text|select; select options LookupItemDto [{id,display}]. Optionally merge answers into shared context via contextKey.',
    ParameterSchemaJson = N'{"type":"object","properties":{"prompt":{"type":"string","description":"Question shown to the user"},"mode":{"type":"string","description":"text | single_choice | multi_choice"},"fieldsJson":{"type":"string","description":"JSON array of {name,label,required?,type?,options?} — type=text|select; select options LookupItemDto [{id,display}]"},"optionsJson":{"type":"string","description":"JSON array of LookupItemDto {id,display} for choice modes"},"contextKey":{"type":"string","description":"Optional shared-context key to merge answers into"}},"required":["prompt"]}'
WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user';
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibraryTool
    WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'ask_user',
    N'Ask the user a structured question and wait for their answer (Interactive only). Use for Gate-0 / missing fields / menus. mode=text|single_choice|multi_choice. fieldsJson supports type=text|select; select options are LookupItemDto [{id,display}]. Optionally merge answers into shared context via contextKey.',
    N'{"type":"object","properties":{"prompt":{"type":"string","description":"Question shown to the user"},"mode":{"type":"string","description":"text | single_choice | multi_choice"},"fieldsJson":{"type":"string","description":"JSON array of {name,label,required?,type?,options?} — type=text|select; select options LookupItemDto [{id,display}]"},"optionsJson":{"type":"string","description":"JSON array of LookupItemDto {id,display} for choice modes"},"contextKey":{"type":"string","description":"Optional shared-context key to merge answers into"}},"required":["prompt"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentAskUserPlugin","MethodName":"AskUser"}',
    1,
    40
);
GO
