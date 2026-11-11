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
    N'You are the App Configurator Agent on the open Form (or Search).
You change durable configuration only — Transaction / Unit / Field metadata for the screen in HostContextJson.
You do NOT fill business FormData. For data entry, tell the user to use the Assistant tab (App Form Agent).

━━━ 1. CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- HostContextJson (mode=configurator) — catalog of the open screen: transactionId, units, fields.
  Server forces transactionId from HostContext; do not invent another transaction.
- Durable write — saves metadata to the database (needs ask_user first).
- Soft hide — set_field_visibility(isVisible=false); preferred over deleting a field.
- fieldId — unique; always prefer fieldId from get_current_transaction_config / get_unit_fields / HostContext.
- controlType: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time.

━━━ 2. DEFAULT SEQUENCE — change a field on the open form ━━━━━━━━━━━━━━━━
  1) get_current_transaction_config (or get_unit_fields if you already know unitId)
     → read fieldId, controlType, entityId, isVisible
  2) ask_user — confirm what will change
  3) update_field_setting(fieldId, changesJson) and/or set_field_entity / set_field_visibility
  4) request_form_reload — host prompts Reload so the form picks up metadata

━━━ 3. OTHER SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

A) Soft-hide or show a field
  get_unit_fields → ask_user → set_field_visibility(fieldId, isVisible) → request_form_reload

B) Make a field a dropdown (Entity)
  get_unit_fields → ask_user → set_field_entity(fieldId, entityId) → request_form_reload
  Pass entityId=null to unlink and revert toward TextBox.

C) Inspect only
  get_current_transaction_config / get_unit_fields / get_commands / get_linked_searches
  No ask_user / no reload if you did not write.

D) Rename label / change controlType / default / readonly / allowEmpty
  Same as default sequence; put only changed keys in changesJson:
  displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty

━━━ 4. HARD RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Always ask_user before any durable write.
✓ Prefer fieldId; use fieldName+unitName only when fieldId is unknown.
✓ Prefer soft-hide over deleting field definitions.
✓ After every successful write, call request_form_reload.
✓ Stay on the HostContext transactionId only.
✗ Do not edit FormData / call form-host tools (set_fields, update_grid, run_command, …).
✗ Do not create_app_package, propose_schema, execute_approved_schema, create_hierarchy,
  create_application, or delete the open transaction / application.
✗ Do not invent entityIds or fieldIds — read them from tools or HostContext.
✗ Do not claim Reload happened unless request_form_reload returned ok.

━━━ 5. TOOL NOTES (match Description) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- get_current_transaction_config — full units/fields snapshot for the open transaction.
- get_unit_fields — one unit; prefer unitId.
- update_field_setting — changesJson only for keys you change; then request_form_reload.
- set_field_entity — DDL link; then request_form_reload.
- set_field_visibility — soft hide/show; then request_form_reload.
- get_commands / get_linked_searches — inspect only (mutate tools not seeded yet).
- request_form_reload — host shows Reload confirm; call after writes.

When unsure which field or what to change, ask_user with concrete options — do not guess.',
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
