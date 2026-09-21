-- TENANT seed helper -- ensure platform-multi-agent tools (ask_user / call_agent / shared context).
-- Interactive ROOT needs ask_user. Workers need call_agent + shared context.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-multi-agent',
    N'platform',
    N'BuiltIn: Multi-Agent Coordination',
    N'Call other agents and share structured data across a workflow. Workers should use ExecutionMode=Deterministic.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'ask_user',
    N'Ask the user a structured question and wait for their answer (Interactive only). Use for Gate-0 / missing fields / menus. mode=text|single_choice|multi_choice. Optionally merge answers into shared context via contextKey.',
    N'{"type":"object","properties":{"prompt":{"type":"string","description":"Question shown to the user"},"mode":{"type":"string","description":"text | single_choice | multi_choice"},"fieldsJson":{"type":"string","description":"JSON array of {name,label,required?} for text answers"},"optionsJson":{"type":"string","description":"JSON array of {id,label} for choice modes"},"contextKey":{"type":"string","description":"Optional shared-context key to merge answers into"}},"required":["prompt"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentAskUserPlugin","MethodName":"AskUser"}',
    1,
    40
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'call_agent')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'call_agent',
    N'Invoke another agent by SkillKey in the same workflow (propagates WorkflowId). Target must be Deterministic.',
    N'{"type":"object","properties":{"targetSkillKey":{"type":"string"},"message":{"type":"string"}},"required":["targetSkillKey","message"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.AgentCallPlugin","MethodName":"CallAgent"}',
    1,
    10
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'write_shared_context')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'write_shared_context',
    N'Write a JSON value to the shared workflow blackboard (AppAgentSharedContext).',
    N'{"type":"object","properties":{"key":{"type":"string"},"valueJson":{"type":"string"}},"required":["key","valueJson"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.AgentSharedContextPlugin","MethodName":"WriteContext"}',
    1,
    20
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'read_shared_context')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'read_shared_context',
    N'Read a JSON value from the shared workflow blackboard. Returns {} if missing.',
    N'{"type":"object","properties":{"key":{"type":"string"}},"required":["key"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.AgentSharedContextPlugin","MethodName":"ReadContext"}',
    1,
    30
);
GO