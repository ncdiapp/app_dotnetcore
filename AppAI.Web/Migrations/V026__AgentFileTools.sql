-- V026: BuiltIn file tools for Agent Management (GenericAgent FileTool).
-- Requires V025 (AppAgentLibraryTool). Library tools live in AppAgentLibraryTool, not AppAgentToolRegister.
-- Disk root: FileRepository/Company_{id}/AgentOutput/{sessionKey}/ (source/, output/, user folders).
-- {sessionKey} = AppGenericAgentSession.SessionKey (SkillKey:UserId or GUID), sanitized for the filesystem.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-files')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'agent-files',
    N'platform',
    N'Agent Files',
    N'Read/write files under FileRepository/Company_{id}/AgentOutput/{sessionKey}/ (source/, output/, and user folders).',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_list')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_list',
    N'List files and folders under the current chat file area (relative to AgentOutput/{sessionKey}/). Omit path or pass empty for the root.',
    N'{"path":{"type":"string","description":"Optional relative folder path. Empty = file area root."}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"List"}',
    1,
    0
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_read')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_read',
    N'Read a UTF-8 text file from the current chat file area. Path is relative to AgentOutput/{sessionKey}/.',
    N'{"path":{"type":"string","description":"Relative file path","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Read"}',
    1,
    1
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_write')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_write',
    N'Write a UTF-8 text file into the current chat file area (creates parent folders). Path is relative to AgentOutput/{sessionKey}/.',
    N'{"path":{"type":"string","description":"Relative file path","required":true},"content":{"type":"string","description":"File content","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Write"}',
    1,
    2
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_mkdir')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_mkdir',
    N'Create a folder (and parents) under the current chat file area.',
    N'{"path":{"type":"string","description":"Relative folder path","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Mkdir"}',
    1,
    3
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_delete')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_delete',
    N'Delete a file or folder (recursive) under the current chat file area.',
    N'{"path":{"type":"string","description":"Relative path to delete","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Delete"}',
    1,
    4
);
GO
