-- V027: BuiltIn agent script tools (sandboxed run under AgentOutput/{sessionKey}/source/).
-- Requires V025 (AppAgentLibraryTool) and V026 (agent-files) patterns.
-- Domain: platform. Library: agent-scripts. Tool: run_agent_script (+ optional validate_agent_outputs).

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-scripts')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'agent-scripts',
    N'platform',
    N'Agent Scripts',
    N'Run user-provided scripts under FileRepository/Company_{id}/AgentOutput/{sessionKey}/source/ (sandboxed). Subscribe per agent; upload scripts via Files.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-scripts' AND ToolName = N'run_agent_script')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-scripts',
    N'run_agent_script',
    N'Run a script file from the current chat AgentOutput area (default: only under source/, .ps1). Use after writing config files. Example for PLM DW Phase B: relativePath=source/_gen_plmdw_import_sql.ps1. Returns exit code, log tails, and output/ file sizes. Do not invent stub SQL/JSON — call this tool instead.',
    N'{"relativePath":{"type":"string","description":"Relative path under AgentOutput/{sessionKey}/, e.g. source/_gen_plmdw_import_sql.ps1","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentScriptPlugin","MethodName":"Run","AllowedPathPrefixes":["source/"],"AllowedExtensions":[".ps1"],"MaxTimeoutSeconds":1200,"InjectDataSourcesFromConfig":"source/dwTabImportConfig.json"}',
    1,
    0
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-scripts' AND ToolName = N'validate_agent_outputs')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-scripts',
    N'validate_agent_outputs',
    N'Check deliverable file sizes under a folder (default output/). Pass minSizeByFileNameJson as a JSON object of fileName→minBytes.',
    N'{"relativeDir":{"type":"string","description":"Folder under AgentOutput (default output/)"},"minSizeByFileNameJson":{"type":"string","description":"JSON map of file name to minimum SizeBytes, e.g. {\"1_PlmDw_Tables.sql\":400000,\"4_PlmDw_ImportBlueprint.json\":500000}"}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentScriptPlugin","MethodName":"ValidateOutputs"}',
    1,
    1
);
GO
