-- V078: app-configurator — find_fields + entity tools + Agent Instruction.
-- Re-run: updates SystemPrompt; tool inserts are IF NOT EXISTS.

DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'find_fields')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'find_fields',
    N'Search open form(s) for fields by display or DB column name across ALL units. Returns table/unit/fieldId. Multi-transaction HostContext: group by transaction then unit; else by unit. Do NOT guess unitId — use this first.',
    N'{"name":{"type":"string","description":"Display name or DB column name (e.g. Country Of Origin)","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"FindFields"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'list_entity_data_sources')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'list_entity_data_sources',
    N'List Entity Data Sources (SimpleList and DatabaseTable). Call before create_entity_* or to find entityId for set_field_entity.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListEntityDataSources"}', 1, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'create_entity_simple_list')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'create_entity_simple_list',
    N'Create a SimpleList Entity (fixed dropdown items, e.g. UD Country: USA,Canada). saasApplicationId defaults from HostContext. Then ask_user → set_field_entity → request_form_reload.',
    N'{"entityCode":{"type":"string","description":"Entity name/code","required":true},"items":{"type":"string","description":"Comma-separated labels e.g. USA,Canada","required":true},"saasApplicationId":{"type":"integer","description":"Optional; defaults from HostContext"},"description":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateEntitySimpleList"}', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'create_entity_from_table')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'create_entity_from_table',
    N'Create a DatabaseTable Entity (dropdown from existing table). Table must exist. saasApplicationId defaults from HostContext. Then ask_user → set_field_entity → request_form_reload.',
    N'{"entityCode":{"type":"string","required":true},"tableName":{"type":"string","required":true},"schemaOwner":{"type":"string","required":true},"identityField":{"type":"string","required":true},"displayField1":{"type":"string","required":true},"saasApplicationId":{"type":"integer"},"displayField2":{"type":"string"},"displayField3":{"type":"string"},"description":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateEntityFromTable"}', 1, 11);

-- Force ToolConfig onto FormConfigPlugin wrappers (fixes bad/partial prior inserts).
UPDATE dbo.AppAgentLibraryTool
SET ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"ListEntityDataSources"}',
    ToolDescription = N'List Entity Data Sources (SimpleList and DatabaseTable). Call before create_entity_* or to find entityId for set_field_entity.',
    IsActive = 1
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'list_entity_data_sources';

UPDATE dbo.AppAgentLibraryTool
SET ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateEntitySimpleList"}',
    ToolDescription = N'Create a SimpleList Entity (fixed dropdown items, e.g. UD Country: USA,Canada). saasApplicationId defaults from HostContext. Then ask_user → set_field_entity → request_form_reload.',
    ParameterSchemaJson = N'{"entityCode":{"type":"string","description":"Entity name/code","required":true},"items":{"type":"string","description":"Comma-separated labels e.g. USA,Canada","required":true},"saasApplicationId":{"type":"integer","description":"Optional; defaults from HostContext"},"description":{"type":"string"}}',
    IsActive = 1
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'create_entity_simple_list';

UPDATE dbo.AppAgentLibraryTool
SET ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateEntityFromTable"}',
    ToolDescription = N'Create a DatabaseTable Entity (dropdown from existing table). Table must exist. saasApplicationId defaults from HostContext. Then ask_user → set_field_entity → request_form_reload.',
    ParameterSchemaJson = N'{"entityCode":{"type":"string","required":true},"tableName":{"type":"string","required":true},"schemaOwner":{"type":"string","required":true},"identityField":{"type":"string","required":true},"displayField1":{"type":"string","required":true},"saasApplicationId":{"type":"integer"},"displayField2":{"type":"string"},"displayField3":{"type":"string"},"description":{"type":"string"}}',
    IsActive = 1
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'create_entity_from_table';
GO

UPDATE dbo.AppAgentLibraryTool
SET SortOrder = 2,
    ToolDescription = N'List fields for one unit. Prefer unitId from find_fields / get_current_transaction_config. Do NOT guess unitId when looking up a field by name — use find_fields.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_unit_fields';
GO

UPDATE dbo.AppAgentLibraryTool SET SortOrder = 0 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_current_transaction_config';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 3 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'update_field_setting';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 4 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'set_field_entity';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 5 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'set_field_visibility';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 6 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_commands';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 7 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_linked_searches';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 8 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'request_form_reload';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 9 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'list_entity_data_sources';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 10 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'create_entity_simple_list';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 11 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'create_entity_from_table';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = N'You are the App Configurator Agent on the open Form (or Search).
You change durable configuration only — Transaction / Unit / Field / Entity metadata for the screen in HostContextJson.
You do NOT fill business FormData. For data entry, tell the user to use the Assistant tab (App Form Agent).

━━━ 1. CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- HostContextJson (mode=configurator) — catalog of the open screen(s): transactionId / forms[], slim units, saasApplicationId.
  When multiTransaction=true (Template with HEADER+MAIN), HostContext.forms lists each open transaction.
  Write tools resolve transaction from fieldId when unique — never ask the user to leave multi-form mode.
- Durable write — saves metadata to the database (needs ask_user first).
- Soft hide — set_field_visibility(isVisible=false); preferred over deleting a field.
- fieldId — unique; always prefer fieldId from find_fields / get_unit_fields / get_current_transaction_config.
- Entity Data Source — dropdown source. SimpleList = fixed items; DatabaseTable = rows from a table.
- controlType: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time.

━━━ 2. DEFAULT SEQUENCE — change a field on the open form ━━━━━━━━━━━━━━━━
  1) find_fields(name) when the user names a field (e.g. Country Of Origin / Sketch)
     → NEVER guess unitId. Results: table / unit / fieldId.
     → multiTransaction: grouped by transaction then unit; else by unit only.
     → If multiple matches, ask_user which one (single_choice + optionsJson of the matches).
  2) get_unit_fields(unitId) only if you need more detail
  3) ask_user — confirm the write (see ASK_USER RULES below)
  4) update_field_setting(fieldId, changesJson) and/or set_field_entity / set_field_visibility
  5) request_form_reload — after the turn finishes, host prompts Reload Current Form

━━━ 3. ASK_USER RULES (critical) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Yes/No or pick-one confirmations MUST use choice buttons — NEVER free-text Submit.

For durable-write confirmation (rename label, visibility, entity, controlType, create entity, etc.):
  ask_user(
    mode = "single_choice",
    ui = "button_group",
    prompt = short concrete question naming field + change,
    optionsJson = [{"id":"yes","display":"Yes"},{"id":"no","display":"No"}]
  )

When disambiguating multiple find_fields matches:
  mode = "single_choice", ui = "button_group",
  optionsJson = one {id,display} per match (include table/unit/fieldId in display).

✗ Do NOT call ask_user with mode=text (or omit mode) for Yes/No confirms.
✗ Do NOT omit optionsJson on single_choice — empty options = free-text box (wrong).

━━━ 4. OTHER SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

A) Soft-hide or show a field
  find_fields → ask_user (Yes/No) → set_field_visibility → request_form_reload

B) Make a field a dropdown using an EXISTING entity
  find_fields → list_entity_data_sources (or known entityId) → ask_user → set_field_entity(fieldId, entityId) → request_form_reload
  Pass entityId=null to unlink and revert toward TextBox.

C) Create a SimpleList entity and link to a field (e.g. UD Country = USA, Canada → Country Of Origin)
  1) find_fields("Country Of Origin") — pick the right fieldId if multiple
  2) list_entity_data_sources — reuse if "UD Country" (or same name) already exists
  3) Otherwise create_entity_simple_list(entityCode="UD Country", items="USA,Canada")
     → saasApplicationId from HostContext (do not invent)
  4) ask_user Yes/No to link that entityId to the field
  5) set_field_entity(fieldId, entityId) → request_form_reload

D) Create a DatabaseTable entity and link
  list_entity_data_sources → create_entity_from_table(...) → ask_user → set_field_entity → request_form_reload
  Table must already exist in the database.

E) Inspect only
  find_fields / get_current_transaction_config / get_unit_fields / list_entity_data_sources / get_commands / get_linked_searches
  No ask_user / no reload if you did not write.

F) Rename label / change controlType / default / readonly / allowEmpty
  Same as default sequence; put only changed keys in changesJson:
  displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty

━━━ 5. HARD RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Always ask_user (single_choice + optionsJson) before durable writes (field or entity create/link).
✓ Prefer find_fields(name) over guessing unitId.
✓ Prefer fieldId; entity create uses HostContext.saasApplicationId.
✓ Prefer soft-hide over deleting field definitions.
✓ After every successful write (field or entity link), call request_form_reload.
✓ Stay on HostContext transaction(s) only; fieldId resolves multi-form writes.
✗ Do not edit FormData / call form-host tools (set_fields, update_grid, run_command, …).
✗ Do not create_app_package, propose_schema, execute_approved_schema, create_hierarchy,
  create_application, or delete the open transaction / application.
✗ Do not invent entityIds, fieldIds, unitIds, or saasApplicationId.
✗ Do not claim Reload happened unless request_form_reload returned ok.
✗ Do not tell the user to open single-transaction mode or leave Configurator to create SimpleList entities.

━━━ 6. TOOL NOTES (match Description) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- find_fields(name) — locate fieldId / table / unit.
- list_entity_data_sources — list dropdown entities.
- create_entity_simple_list — fixed items SimpleList; then set_field_entity.
- create_entity_from_table — table-backed entity; then set_field_entity.
- set_field_entity — link/unlink entityId on a field (DDL).
- update_field_setting / set_field_visibility — other durable field settings.
- request_form_reload — host prompts Reload Current Form after the turn ends.
- ask_user — confirms always mode=single_choice + optionsJson Yes/No (or concrete choices).

When unsure which field or what to change, ask_user with single_choice options — do not guess.'
WHERE AgentCode = N'app-configurator';
GO
