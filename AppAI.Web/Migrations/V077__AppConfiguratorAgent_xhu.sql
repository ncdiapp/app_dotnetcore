-- V077: App Configurator Agent (app-configurator) + platform-form-config tools.
-- IF NOT EXISTS only. Re-run does not overwrite prompt or subscriptions.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-form-config')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-form-config',
    N'platform',
    N'BuiltIn: Form/Search Configurator',
    N'Durable configuration of the open Form or Search (fields, visibility, entity links). Not FormData.',
    N'BuiltIn',
    1
);
GO

DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_current_transaction_config')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_current_transaction_config',
    N'Inspect the open form transaction configuration (units, fields). Uses HostContext transactionId.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetCurrentTransactionConfig"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_unit_fields')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_unit_fields',
    N'List fields for one unit. Prefer unitId from get_current_transaction_config.',
    N'{"unitId":{"type":"integer","description":"Unit id"},"unitName":{"type":"string","description":"Unit display or table name fallback"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetUnitFields"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'update_field_setting')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'update_field_setting',
    N'Update durable field settings. changesJson keys: displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty. Prefer fieldId. ask_user first; then request_form_reload.',
    N'{"fieldId":{"type":"integer","description":"Field id (preferred)","required":true},"changesJson":{"type":"string","description":"JSON object of properties to change","required":true},"fieldName":{"type":"string","description":"Fallback when fieldId unknown"},"unitName":{"type":"string","description":"Unit scope for fieldName"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"UpdateFieldSetting"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'set_field_entity')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'set_field_entity',
    N'Link a field to an Entity data source (DDL). Prefer fieldId. ask_user first; then request_form_reload.',
    N'{"fieldId":{"type":"integer","description":"Field id","required":true},"entityId":{"type":"integer","description":"Entity id; omit/null to unlink"},"fieldName":{"type":"string"},"unitName":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"SetFieldEntity"}', 1, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'set_field_visibility')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'set_field_visibility',
    N'Soft-hide or show a field (IsVisible). Prefer over deleting. ask_user first; then request_form_reload.',
    N'{"fieldId":{"type":"integer","description":"Field id","required":true},"isVisible":{"type":"boolean","description":"true=show false=hide","required":true},"fieldName":{"type":"string"},"unitName":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"SetFieldVisibility"}', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_commands')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_commands',
    N'List commands on the open transaction.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetCommands"}', 1, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_linked_searches')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_linked_searches',
    N'List Linked Search configs on units of the open transaction.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetLinkedSearches"}', 1, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'request_form_reload')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'request_form_reload',
    N'Ask the form host to prompt Reload after configuration changes. Call after successful writes.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"RequestFormReload"}', 1, 7);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-configurator')
INSERT INTO dbo.AppAgentSkillSet
    (AgentCode, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'app-configurator',
    N'App Configurator Agent',
    N'Configures the open Form/Search metadata. Not listed in the left menu.',
    N'You are the App Configurator Agent. You change durable configuration of the currently open Form or Search.
Use HostContextJson (mode=configurator). Call get_current_transaction_config or get_unit_fields before edits.
Always ask_user before durable writes. Prefer fieldId. Prefer set_field_visibility (soft hide) over deleting fields.
After a successful write, call request_form_reload.
Do not edit business FormData — tell the user to use the Assistant (App Form Agent).
Do not create applications, run propose_schema/execute_approved_schema, create_hierarchy, or delete the open transaction/application.
controlType: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL, 34=Time.',
    3, 1, 6, 1,
    80000, 60000, 8000, 10,
    40, N'Interactive', 1
);
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-configurator';

IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-form-config')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-form-config');
GO
