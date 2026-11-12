-- V080: Formula + Form Layout BuiltIn tools for platform-form-config and platform-transaction.
-- Also refresh app-configurator system prompt with formula/layout sequences.
-- Force-enable the 4 new tools on app-configurator (form-config) and app-builder (transaction):
--   subscribe libraries + clear AppAgentToolExclusion rows for those tool names.

DECLARE @FormPlugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

UPDATE dbo.AppAgentToolLibrary
SET Description = N'Durable configuration of the open Form or Search (fields, visibility, entity links, formulas, form layout). Not FormData.'
WHERE LibraryKey = N'platform-form-config';

-- ═══════════════════════════════════════════════════════════════════════════
-- platform-form-config (HostContext-aware FormConfigPlugin)
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_unit_formulas')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_unit_formulas',
    N'List Calculation Validation Flow formulas for a unit (Assignment/Warning/Error/Aggregate/Subscribe). Prefer unitId. defaultValue is NOT a formula.',
    N'{"unitId":{"type":"integer","description":"Unit id (preferred)"},"unitName":{"type":"string","description":"Unit display/table name fallback"},"transactionId":{"type":"integer","description":"Required when multi-transaction HostContext"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetUnitFormulas"}', 1, 12);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'update_unit_formula')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'update_unit_formula',
    N'Add/update/delete unit formula. operationType: 1=Assignment 2=Warning 3=Error 4=Aggregate 5=Subscribe. Display-name expressions resolve to [DbName_Id]; ambiguous → needsDisambiguation (ask_user). Preview first, ask_user Yes/No, then confirmed=true. Optional dryRun. After success request_form_reload. Do NOT use defaultValue for Calculation Flow.',
    N'{"action":{"type":"string","description":"add|update|delete","required":true},"unitId":{"type":"integer"},"unitName":{"type":"string"},"formulaId":{"type":"integer"},"operationType":{"type":"integer","description":"1=Assignment 2=Warning 3=Error 4=Aggregate 5=Subscribe"},"expression":{"type":"string","description":"e.g. Description = Product Code + \" - \" + Nomenclature"},"assignToFieldId":{"type":"integer"},"assignToFieldName":{"type":"string"},"sourceFieldId":{"type":"integer","description":"Aggregate child field / Subscribe parent field"},"sourceFieldName":{"type":"string"},"aggregationType":{"type":"integer","description":"1=SUM 2=AVG 3=MIN 4=MAX 5=COUNT"},"warningMessage":{"type":"string"},"warningHighlightFieldId":{"type":"integer"},"confirmed":{"type":"boolean","description":"Must be true to persist"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"UpdateUnitFormula"}', 1, 13);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_form_layout')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_form_layout',
    N'Inspect Flex form layout tree (containers, fields, heights, collapsible, tabs) for the open transaction.',
    N'{"transactionId":{"type":"integer","description":"Required when multi-transaction HostContext"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetFormLayout"}', 1, 14);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'design_form_layout')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'design_form_layout',
    N'Generate or patch Flex form layout. mode=generate: N-column/default layout (REPLACES existing — ask_user + confirmed=true; optional backup/dryRun). mode=patch: patchesJson ops setHeight, moveField, setContainer, setColumns(alone→generate). After success request_form_reload.',
    N'{"mode":{"type":"string","description":"generate|patch","required":true},"nbColumns":{"type":"integer","description":"Column count for generate"},"patchesJson":{"type":"string","description":"JSON array of patch ops"},"confirmed":{"type":"boolean","description":"Must be true to persist"},"dryRun":{"type":"boolean"},"backup":{"type":"boolean","description":"Include current layout summary before overwrite"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"DesignFormLayout"}', 1, 15);

UPDATE dbo.AppAgentLibraryTool SET SortOrder = 12 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_unit_formulas';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 13 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'update_unit_formula';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 14 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_form_layout';
UPDATE dbo.AppAgentLibraryTool SET SortOrder = 15 WHERE LibraryKey = N'platform-form-config' AND ToolName = N'design_form_layout';
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- platform-transaction (explicit transactionId TransactionModifierPlugin)
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'get_unit_formulas')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-transaction', N'get_unit_formulas',
    N'List Calculation Validation Flow formulas for a unit (Assignment/Warning/Error/Aggregate/Subscribe). defaultValue is NOT a formula.',
    N'{"transactionId":{"type":"integer","required":true},"unitId":{"type":"integer"},"unitName":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"GetUnitFormulas"}', 1, 80);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'update_unit_formula')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-transaction', N'update_unit_formula',
    N'Add/update/delete unit formulas. operationType 1=Assignment 2=Warning 3=Error 4=Aggregate 5=Subscribe. Resolve display names; ask_user; confirmed=true to persist. Optional dryRun.',
    N'{"transactionId":{"type":"integer","required":true},"action":{"type":"string","required":true},"unitId":{"type":"integer"},"unitName":{"type":"string"},"formulaId":{"type":"integer"},"operationType":{"type":"integer"},"expression":{"type":"string"},"assignToFieldId":{"type":"integer"},"assignToFieldName":{"type":"string"},"sourceFieldId":{"type":"integer"},"sourceFieldName":{"type":"string"},"aggregationType":{"type":"integer"},"warningMessage":{"type":"string"},"warningHighlightFieldId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}}',
    N'BuiltIn', N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"UpdateUnitFormula"}', 1, 81);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'get_form_layout')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-transaction', N'get_form_layout',
    N'Inspect Flex form layout tree for a transaction FormId.',
    N'{"transactionId":{"type":"integer","required":true}}',
    N'BuiltIn', N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"GetFormLayout"}', 1, 82);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'design_form_layout')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-transaction', N'design_form_layout',
    N'Generate (N-col / default rebuild) or patch Flex layout. Overwrite needs confirmed=true; optional backup/dryRun. patch ops: setHeight, moveField, setContainer, setColumns.',
    N'{"transactionId":{"type":"integer","required":true},"mode":{"type":"string","required":true},"nbColumns":{"type":"integer"},"patchesJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"backup":{"type":"boolean"}}',
    N'BuiltIn', N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"DesignFormLayout"}', 1, 83);
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- Refresh app-configurator prompt (formula + layout sequences)
-- ═══════════════════════════════════════════════════════════════════════════

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = N'You are the App Configurator Agent on the open Form (or Search).
You change durable configuration only — Transaction / Unit / Field / Entity / Formula / Form Layout metadata for the screen in HostContextJson.
You do NOT fill business FormData. For data entry, tell the user to use the Assistant tab (App Form Agent).

━━━ 1. CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- HostContextJson (mode=configurator) — catalog of the open screen(s): transactionId / forms[], slim units, saasApplicationId.
  When multiTransaction=true (Template with HEADER+MAIN), HostContext.forms lists each open transaction.
  Write tools resolve transaction from fieldId when unique — never ask the user to leave multi-form mode.
- Durable write — saves metadata to the database (needs ask_user first).
- Soft hide — set_field_visibility(isVisible=false); preferred over deleting a field.
- fieldId — unique; always prefer fieldId from find_fields / get_unit_fields / get_current_transaction_config.
- Calculation Flow formula — Assignment/Warning/Error/Aggregate/Subscribe via get_unit_formulas / update_unit_formula.
  defaultValue is NOT a Calculation Flow formula — never claim you set a formula via update_field_setting.defaultValue.
- Form layout — Flex tree via get_form_layout / design_form_layout (generate N-col or patch).
- Entity Data Source — dropdown source. SimpleList = fixed items; DatabaseTable = rows from a table.
- controlType EmAppControlType (NO value 3): 1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label,
  13=CheckBox, 20=Numeric, 26=RichText, 27=DateTimeDetail, 34=Time, 37=ImageBinary, 38=AutoComplete,
  48=SearchAbleDDL, 50=ExternalImageUrl, 53=MultiSelectDDL. Multiline text = Memo=4 (never invent 3).

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

For durable-write confirmation (rename label, visibility, entity, controlType, create entity, formula, layout, etc.):
  ask_user(
    mode = "single_choice",
    ui = "button_group",
    prompt = short concrete question naming field + change,
    optionsJson = [{"id":"yes","display":"Yes"},{"id":"no","display":"No"}]
  )

When disambiguating multiple find_fields / formula field matches:
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
  find_fields / get_current_transaction_config / get_unit_fields / list_entity_data_sources / get_commands / get_linked_searches / get_unit_formulas / get_form_layout
  No ask_user / no reload if you did not write.

F) Rename label / change controlType / default / readonly / allowEmpty
  Same as default sequence; put only changed keys in changesJson:
  displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty
  NOTE: defaultValue ≠ Calculation Flow formula.

G) Set / change a Calculation Flow formula (Assignment / Warning / Error / Aggregate / Subscribe)
  1) find_fields / get_unit_fields — resolve fieldIds for assign-to and referenced fields
  2) get_unit_formulas(unitId) — see existing formulas
  3) update_unit_formula(... confirmed omitted or false) — tool resolves display names → [DbName_Id]
     → If needsDisambiguation: ask_user which fieldId, retry with explicit fieldIds
  4) Show resolvedExpression to user; ask_user Yes/No
  5) update_unit_formula(... same args, confirmed=true) → request_form_reload
  operationType: 1=Assignment, 2=Warning, 3=Error, 4=Aggregate (needs sourceFieldId + aggregationType 1..5),
  5=Subscribe (child assignToFieldId + parent sourceFieldId).
  Optional dryRun=true for preview without write.

H) Form layout — generate N-column / default, or patch
  1) get_form_layout — inspect current tree
  2) Generate (REPLACES existing layout):
     design_form_layout(mode=generate, nbColumns=N, backup=true) → needsConfirmation
     → ask_user Yes/No → design_form_layout(..., confirmed=true) → request_form_reload
     Optional dryRun=true to preview without write.
  3) Patch (move field / height / container props):
     design_form_layout(mode=patch, patchesJson=[...]) → ask_user → confirmed=true → request_form_reload
     Patch ops: setHeight({fieldId|layoutItemId|gridUnitId, height}), moveField({fieldId, beforeFieldId|afterFieldId}),
     setContainer({layoutItemId, label, isCollapsible, isDefaultCollapsed, isTab}),
     setColumns({nbColumns}) alone triggers generate rebuild.

━━━ 5. HARD RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Always ask_user (single_choice + optionsJson) before durable writes (field, entity, formula, layout overwrite).
✓ Prefer find_fields(name) over guessing unitId.
✓ Prefer fieldId; entity create uses HostContext.saasApplicationId.
✓ Prefer soft-hide over deleting field definitions.
✓ After every successful write (field, entity, formula, layout), call request_form_reload.
✓ Stay on HostContext transaction(s) only; fieldId resolves multi-form writes.
✓ Formula: resolve → ask_user on resolvedExpression → confirmed=true. Never use defaultValue as a formula.
✓ Layout generate overwrite: ask_user + confirmed=true; prefer backup=true.
✗ Do not edit FormData / call form-host tools (set_fields, update_grid, run_command, …).
✗ Do not create_app_package, propose_schema, execute_approved_schema, create_hierarchy,
  create_application, or delete the open transaction / application.
✗ Do not invent entityIds, fieldIds, unitIds, or saasApplicationId.
✗ Do not claim Reload happened unless request_form_reload returned ok.
✗ Do not tell the user to open single-transaction mode or leave Configurator to create SimpleList entities.
✗ Do not claim a formula was set unless update_unit_formula returned ok with confirmed=true.

━━━ 6. TOOL NOTES (match Description) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- find_fields(name) — locate fieldId / table / unit.
- list_entity_data_sources — list dropdown entities.
- create_entity_simple_list — fixed items SimpleList; then set_field_entity.
- create_entity_from_table — table-backed entity; then set_field_entity.
- set_field_entity — link/unlink entityId on a field (DDL).
- update_field_setting / set_field_visibility — other durable field settings (NOT Calculation Flow formulas).
- get_unit_formulas / update_unit_formula — Calculation Validation Flow formulas.
- get_form_layout / design_form_layout — Flex layout generate/patch.
- request_form_reload — host prompts Reload Current Form after the turn ends.
- ask_user — confirms always mode=single_choice + optionsJson Yes/No (or concrete choices).

When unsure which field or what to change, ask_user with single_choice options — do not guess.'
WHERE AgentCode = N'app-configurator';
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- Force-check the 4 new tools on both agents (subscribe + clear exclusions)
-- ═══════════════════════════════════════════════════════════════════════════

-- app-configurator → platform-form-config
INSERT INTO dbo.AppAgentLibrarySubscription (LibraryKey, AgentID)
SELECT N'platform-form-config', s.AgentID
FROM dbo.AppAgentSkillSet s
WHERE s.AgentCode = N'app-configurator'
  AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = N'platform-form-config')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = s.AgentID AND x.LibraryKey = N'platform-form-config');
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-configurator'
  AND e.LibraryKey = N'platform-form-config'
  AND e.ToolName IN (
      N'get_unit_formulas',
      N'update_unit_formula',
      N'get_form_layout',
      N'design_form_layout');
GO

-- app-builder → platform-transaction
INSERT INTO dbo.AppAgentLibrarySubscription (LibraryKey, AgentID)
SELECT N'platform-transaction', s.AgentID
FROM dbo.AppAgentSkillSet s
WHERE s.AgentCode = N'app-builder'
  AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = N'platform-transaction')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = s.AgentID AND x.LibraryKey = N'platform-transaction');
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-builder'
  AND e.LibraryKey = N'platform-transaction'
  AND e.ToolName IN (
      N'get_unit_formulas',
      N'update_unit_formula',
      N'get_form_layout',
      N'design_form_layout');
GO
