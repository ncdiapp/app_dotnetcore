-- V087: app-configurator Data Load/DT/Message Template + App Builder Matrix child unit.
-- Merged former V087 + V088 (matrix). Idempotent: IF NOT EXISTS inserts; UPDATE descriptions;
-- clear exclusions; SystemPrompt appends guarded by NOT LIKE / stub REPLACE.

DECLARE @FormPlugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

-- ═══════════════════════════════════════════════════════════════════════════
-- A) Register Data Load tools (IF NOT EXISTS)
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_loads')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_data_loads', N'', N'{"unitId":{"type":"integer"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetDataLoads"}', 1, 40);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_load_detail')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_data_load_detail', N'', N'{"dataLoadId":{"type":"integer","required":true},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetDataLoadDetail"}', 1, 41);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'list_available_datasets')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'list_available_datasets', N'', N'{"nameContains":{"type":"string"},"take":{"type":"integer"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"ListAvailableDatasets"}', 1, 42);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_dataset_columns')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_dataset_columns', N'', N'{"dataSetId":{"type":"integer","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetDatasetColumns"}', 1, 43);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_data_load')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'upsert_data_load', N'',
    N'{"dataLoadId":{"type":"integer"},"unitId":{"type":"integer"},"loadName":{"type":"string"},"description":{"type":"string"},"dataSetId":{"type":"integer"},"loadOrder":{"type":"integer"},"isAutoExecutedWhenOpenEditForm":{"type":"boolean"},"isAutoExecuteBeforeInitialCascading":{"type":"boolean"},"conditionMappingsJson":{"type":"string"},"fieldMappingsJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"UpsertDataLoad"}', 1, 44);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_data_load')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'delete_data_load', N'',
    N'{"dataLoadId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"DeleteDataLoad"}', 1, 45);
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- B) Register Message Template tools (IF NOT EXISTS) — former V088
-- ═══════════════════════════════════════════════════════════════════════════
DECLARE @FormPlugin2 nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_message_templates')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_message_templates', N'',
    N'{"nameContains":{"type":"string"},"take":{"type":"integer"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin2 + N'","MethodName":"GetMessageTemplates"}', 1, 50);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_message_template_detail')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_message_template_detail', N'',
    N'{"messageTemplateId":{"type":"integer","required":true},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin2 + N'","MethodName":"GetMessageTemplateDetail"}', 1, 51);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_message_template')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'upsert_message_template', N'',
    N'{"messageTemplateId":{"type":"integer"},"name":{"type":"string"},"subject":{"type":"string"},"body":{"type":"string"},"toList":{"type":"string"},"ccList":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin2 + N'","MethodName":"UpsertMessageTemplate"}', 1, 52);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_message_template')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'delete_message_template', N'',
    N'{"messageTemplateId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin2 + N'","MethodName":"DeleteMessageTemplate"}', 1, 53);
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- C) Always refresh tool descriptions
-- ═══════════════════════════════════════════════════════════════════════════
UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'List Transaction Data Load rules on the open transaction. Optional unitId filters to one unit. Returns dataLoadId, loadName, dataSetId, loadOrder, auto-exec flags. Next: get_data_load_detail or upsert_data_load.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_loads';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'One Data Load: header + conditionMappings + fieldMappings + dataSetColumns (when DataSetId set). condition = where/join keys; field = dataset column → form field. Use find_fields for transactionFieldId; get_dataset_columns / detail columns for dbColumnName.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_load_detail';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'List tenant AppDataSet rows for Data Load dataSetId. Optional nameContains + take (default 50, max 200). Never invent dataSetId — pick from this list. Then get_dataset_columns or upsert_data_load.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'list_available_datasets';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Query columns for one dataset. Use columnId / display as dbColumnName in conditionMappingsJson and fieldMappingsJson.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_dataset_columns';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update Transaction Data Load (Form Builder Data Load section — not Operation Task 45 Execute All DataLoad). Create requires unitId + loadName + dataSetId (from list_available_datasets). loadOrder (UI Sort/Load Order) optional on create — defaults to max existing + 1. Also optional: isAutoExecutedWhenOpenEditForm, isAutoExecuteBeforeInitialCascading. conditionMappingsJson=[{transactionFieldId,dbColumnName,whereClause}] full-replace for conditions; fieldMappingsJson=[{transactionFieldId,dbColumnName}] full-replace for value maps; [] clears that side; omit leaves that side. Preview→ask_user→confirmed=true→request_form_reload. Runtime run = separate command actionType 45.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_data_load';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Delete one Transaction Data Load rule (and its field mappings). Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_data_load';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'List Internal Data Transfer settings on the open transaction (dataTransferId, description, transferTypeId, destinationTransactionId, mappingCount). Next: get_data_transfer_detail before editing mappings.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_transfers';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'One Data Transfer: header + mappings + mappingHints by type. Types: 1=FromDataModelToMessageTemplate, 2=FromDataModelToDataModel, 3=FromApiToDataModel, 4=FromDataModelToApi. For type 4 returns targetApiInputParameters after internalCode=API op id is set. Read mappingHints.rowShape before upsert_data_transfer.mappingsJson.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_transfer_detail';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update Internal Data Transfer (Form Builder Internal Data Transfer — not Operation Task 43 Execute Data Transfer). Types: 1=ToMessageTemplate, 2=DM→DM (default on create), 3=Api→DM, 4=DM→Api. Header: description, internalCode (API op id for 3/4), transferTypeId, destinationTransactionId (DM target for 2/3). mappingsJson=FULL REPLACE of AppTransactionSaveAsMapping: [{sourceFieldId,targetFieldId,name,jsonPropertyPathName,isBlankTargetField,mappingUnitId}]; [] clears; omit leaves mappings. Type1: sourceFieldId+name(token). Type2: sourceFieldId+targetFieldId. Type3: jsonPropertyPathName+targetFieldId. Type4: name+sourceFieldId (API params) and/or jsonPropertyPathName+sourceFieldId (payload) and/or isBlankTargetField=true+jsonPropertyPathName+targetFieldId (response). Preview→ask_user→confirmed=true→request_form_reload. Runtime run = command actionType 43 + DataTransferSettingId.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_data_transfer';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Delete one Internal Data Transfer setting (and mappings). Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_data_transfer';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'List transaction Message Templates (AppMessage MessgaeScopeType=99). Returns messageTemplateId, name (Bcclist), subject, bodyPreview. Next: get_message_template_detail or upsert_command actionType 74 with optionsJson.messageTemplateId. Never invent template ids; never put template body in notificationMessage for type 74.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_message_templates';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'One Message Template detail: name/Bcclist, subject, body, to/cc. Body may use find_fields → messageToken ([TF_{fieldId}_{dbName}]).'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_message_template_detail';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update transaction Message Template (AppMessage scope MessageTemplate). Create: subject and/or name (name→Bcclist picker label), body (HTML/text + messageToken). Returns messageTemplateId. Then upsert_command(commandId, actionType 74, optionsJson:{messageTemplateId}). Do NOT store body in command notificationMessage. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_message_template';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Delete a transaction Message Template. Preview→ask_user→confirmed=true→request_form_reload. Rebind or clear commands that still reference the id.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_message_template';

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create/update Data Model Operation. actionType=EmAppTransactionCommandType (NOT Link Target 1/2). Common: 50=Refresh, 49=Save, 42=SQL, 43=Data Transfer, 59=Call API, 57=Cmd Formula, 53=Navigate Search, 74=Print From Message Template, 200=Composition, 60/61/62=Message, 205=Validate Required. Type 74→optionsJson.messageTemplateId from get_message_templates / upsert_message_template (top-level MessageTemplateId column — NOT notificationMessage, NOT ActionAttribute). Type 42 SQL→notificationMessage+sqlToken. Type 57→optionsJson.executionFormulaUI+formulaToken. Type 53→optionsJson.linkedSearchId. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- D) Force-enable on app-configurator
-- ═══════════════════════════════════════════════════════════════════════════
DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-configurator'
  AND e.LibraryKey = N'platform-form-config'
  AND e.ToolName IN (
      N'get_data_loads', N'get_data_load_detail', N'list_available_datasets', N'get_dataset_columns',
      N'upsert_data_load', N'delete_data_load',
      N'get_data_transfers', N'get_data_transfer_detail', N'upsert_data_transfer', N'delete_data_transfer',
      N'get_message_templates', N'get_message_template_detail',
      N'upsert_message_template', N'delete_message_template');
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- E) SystemPrompt — Data Load / Data Transfer
-- ═══════════════════════════════════════════════════════════════════════════
DECLARE @OldStub nvarchar(max) = N'
=== DATA LOAD / DATA TRANSFER ===
Data Load: list_available_datasets → get_dataset_columns → upsert_data_load(unitId,loadName,dataSetId,conditionMappingsJson,fieldMappingsJson).
conditionMappingsJson / fieldMappingsJson are full-replace per side; [] clears that side.
Data Transfer: get_data_transfers → get_data_transfer_detail (read mappingHints) → upsert_data_transfer.
mappingsJson is full replace for all types 1–4; [] clears. Type 4 (DM→Api): internalCode = API operation id.
Always preview → ask_user → confirmed=true → request_form_reload.
';

DECLARE @NewSection nvarchar(max) = N'
=== DATA LOAD (CRUD) / DATA TRANSFER (types 1–4) ===
These configure Form Builder rules on the open transaction — NOT runtime FormData.

--- Data Load (AppTransactionDataLoad) ---
Hard flow (create):
1) get_unit_fields / find_fields — pick unitId + transactionFieldIds.
2) list_available_datasets — real dataSetId (never invent).
3) get_dataset_columns(dataSetId) — dbColumnName values.
4) upsert_data_load(unitId, loadName, dataSetId, conditionMappingsJson?, fieldMappingsJson?) preview→ask_user→confirmed=true.
5) request_form_reload.

Mapping JSON (full-replace per side; [] clears that side; omit = leave):
- conditionMappingsJson: [{transactionFieldId, dbColumnName, whereClause}]  (IsConditionMapping)
- fieldMappingsJson: [{transactionFieldId, dbColumnName}]  (value load)

Inspect: get_data_loads → get_data_load_detail.
Delete: delete_data_load (HITL).
Do NOT confuse with Operation Task actionType 45 (Execute All DataLoad) — that runs existing rules; upsert_data_load defines them.

--- Internal Data Transfer (AppTransactionDataTransferSetting) ---
Hard flow:
1) get_data_transfers (or create via upsert_data_transfer header first).
2) get_data_transfer_detail — read mappingHints + targetApiInputParameters (type 4).
3) upsert_data_transfer with mappingsJson full replace (or [] to clear).
4) request_form_reload.

transferTypeId:
1 = FromDataModelToMessageTemplate — sourceFieldId + name (template token); internalCode may id template
2 = FromDataModelToDataModel (default create) — sourceFieldId + targetFieldId; destinationTransactionId required
3 = FromApiToDataModel — jsonPropertyPathName + targetFieldId; internalCode = API operation id; destinationTransactionId = target DM
4 = FromDataModelToApi — internalCode = API operation id; mix rows: (name+sourceFieldId) API params, (jsonPropertyPathName+sourceFieldId) payload, (isBlankTargetField=true + jsonPropertyPathName + targetFieldId) response

mappingsJson = FULL REPLACE. Omit to leave mappings. Never invent field ids — use find_fields.
Do NOT confuse with Operation Task actionType 43 (Execute Data Transfer) — that runs a setting; upsert_data_transfer defines it (then set DataTransferSettingId on the command).
';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt, @OldStub, N'')
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt LIKE N'%=== DATA LOAD / DATA TRANSFER ===%';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + @NewSection
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%DATA LOAD (CRUD) / DATA TRANSFER (types 1–4) ===%';
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- F) SystemPrompt — Message Template + Print From Template (74) — former V088
-- ═══════════════════════════════════════════════════════════════════════════
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

=== MESSAGE TEMPLATE + PRINT FROM TEMPLATE (74) ===
Message Template = AppMessage with MessgaeScopeType=MessageTemplate on the open transaction (Message Management / picker).

Hard flow for UI: Print From Message Template:
1) find_fields — messageToken for body placeholders ([TF_{fieldId}_{dbName}]).
2) upsert_message_template(name?, subject, body) preview→ask_user→confirmed=true — returns messageTemplateId.
   Or get_message_templates to reuse an existing id.
3) upsert_command(commandId, actionType=74, optionsJson:{messageTemplateId:<id>}) confirmed=true.
4) request_form_reload — Operation editor Message Template must show the name, not (None).

Forbidden:
- Putting template body into notificationMessage for type 74 (UI stays None; body is not the picker binding).
- Inventing messageTemplateId / using ActionAttribute.MessageTemplateId (wrong storage — use top-level MessageTemplateId via optionsJson.messageTemplateId).
'
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%MESSAGE TEMPLATE + PRINT FROM TEMPLATE (74) ===%';
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- G) App Builder — Dynamic Matrix as Root CHILD unit (former V088)
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibraryTool
    WHERE LibraryKey = N'platform-transaction' AND ToolName = N'configure_matrix_unit')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'configure_matrix_unit',
    N'Dedicated Matrix WRITE tool. MatrixForeignKeyFieldId→Source KEY DDL (ColorId); shared master Entity (~80%); command 46. Preview→confirmed=true. Then inspect_matrix_unit.',
    N'{"transactionId":{"type":"integer"},"matrixUnitId":{"type":"integer"},"matrixUnitNameOrTable":{"type":"string"},"matrixKeyUnitIdsCsv":{"type":"string"},"matrixKeyTableNamesCsv":{"type":"string"},"createGenerateMatrixCommand":{"type":"boolean"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"ConfigureMatrixUnit"}',
    1,
    25
);
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Dedicated Matrix WRITE tool. Wires Matrix KEY: MatrixForeignKeyFieldId→Source KEY DDL (ColorId), ControlType=DDL, EntityId shared with Source (master Color/Size Entity ~80%, not Source grid table); command 46. Generate uses Source row KEY IDs; display from Entity. Preview→confirmed=true. Then inspect_matrix_unit.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'configure_matrix_unit';
GO

IF NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibraryTool
    WHERE LibraryKey = N'platform-transaction' AND ToolName = N'inspect_matrix_unit')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'inspect_matrix_unit',
    N'Read-only Matrix check: Matrix KEY→Source KEY DDL, shared EntityId, command 46, issues. Call before claiming ready.',
    N'{"transactionId":{"type":"integer"},"matrixUnitId":{"type":"integer"},"matrixUnitNameOrTable":{"type":"string"}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"InspectMatrixUnit"}',
    1,
    26
);
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Read-only Matrix check: IsMatrixUnit, Matrix KEY→Source KEY DDL, shared EntityId, ControlType, command 46, issues (Source PK wiring / Entity mismatch). Call before claiming ready.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'inspect_matrix_unit';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Modify field displayName/controlType/entityId/defaultValue only. FORBIDDEN: matrixForeignKeyFieldId/isMatrixUnit — use configure_matrix_unit. Prefer fieldId from get_transaction_details.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'update_transaction_field';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Execute approved schema. Matrix: Root children; KEY columns {DimShort}Id; auto-wires IsMatrixUnit + MatrixForeignKeyFieldId→Source KEY DDL + shared EntityId. FAILS if MatrixConfig.applied=false. Then inspect_matrix_unit.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'execute_approved_schema';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Extract schema. Dynamic matrix: Root + Child dims + matrix. Matrix KEY columns {DimShort}Id same as Source KEY; Source+Matrix share master DDL Entity (Color/Size). Never grandchild; never call Root children siblings.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'propose_schema';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ DYNAMIC MATRIX + CHILD UNIT TERMINOLOGY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Sibling Unit = ONLY a root-level peer of the Root Unit. NEVER call Root''s child units siblings.
When user wants a dynamic matrix form: Root + N Child units under that Root (e.g. 2 dimension grids + 1 matrix child).
Dimension grids AND matrix are ALL Root children (isLookup=false). Matrix is NEVER a grandchild.
propose_schema JSON: matrix table isMatrix=true + matrixKeyTables=[ExactFullDimTable1,ExactFullDimTable2]; Root ONE_TO_MANY to each.

MATRIX KEY FIELD (required for Generate Matrix):
On Matrix unit, each combination-key column is a Matrix KEY field.
Set MatrixForeignKeyFieldId = Source KEY DDL field Id (ColorId) whose ROW VALUES feed Cartesian product — not Source PK.
Source KEY + Matrix KEY share master DDL Entity (~80%). create_entity_from_table on MASTER Color/Size, not small Source grid.
Use configure_matrix_unit: IsMatrixUnit + MatrixForeignKeyFieldId + ControlType=DDL + shared EntityId + command 46.
execute_approved_schema FAILS if MatrixConfig.applied=false — fix and retry; do not pretend success.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt NOT LIKE N'%DYNAMIC MATRIX + CHILD UNIT TERMINOLOGY%';
GO

-- If older short matrix prompt already applied, append KEY-field explain once.
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

MATRIX KEY FIELD (required for Generate Matrix):
On Matrix unit, each combination-key column is a Matrix KEY field.
Set MatrixForeignKeyFieldId = Source KEY DDL field (ColorId), not Source PK. Shared master Entity for display (~80%).
configure_matrix_unit sets ControlType=DDL + copies/shares EntityId with Source KEY.
execute_approved_schema FAILS if MatrixConfig.applied=false — fix and retry.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt LIKE N'%DYNAMIC MATRIX + CHILD UNIT TERMINOLOGY%'
  AND SystemPrompt NOT LIKE N'%MATRIX KEY FIELD (required for Generate Matrix)%';
GO

-- Append DDL/EntityId note if KEY section exists but DDL note missing.
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
Matrix KEY UI: ControlType must be DDL/AutoComplete/SearchAbleDDL; configure_matrix_unit sets ControlType=DDL + EntityId when entity exists.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt LIKE N'%MATRIX KEY FIELD (required for Generate Matrix)%'
  AND SystemPrompt NOT LIKE N'%ControlType must be DDL%';
GO

-- Harden: dedicated tools + shared Entity + inspect
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
MATRIX HARDENING: Only configure_matrix_unit may set IsMatrixUnit/MatrixForeignKeyFieldId. MatrixForeignKeyFieldId→Source KEY DDL (ColorId), not Source PK. Source KEY + Matrix KEY share master DDL Entity (~80%; Color/Size lists) — create_entity_from_table on MASTER list, not small Source grid table. Call inspect_matrix_unit before claiming ready.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt LIKE N'%DYNAMIC MATRIX + CHILD UNIT TERMINOLOGY%'
  AND SystemPrompt NOT LIKE N'%MATRIX HARDENING%';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
MATRIX SHARED DDL ENTITY (~80%): Source KEY and Matrix KEY use the SAME EntityId (master Color/Size). Generate Matrix takes KEY IDs from Source grid rows; display from Entity. Rare (~20%): Entity from Source grid table.
Source Color grid: ProductColorId=PK (NOT Matrix KEY), ProductId=Link FK, ColorId=DDL Matrix KEY. Wire MatrixForeignKeyFieldId→ColorId only.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt LIKE N'%MATRIX HARDENING%'
  AND SystemPrompt NOT LIKE N'%MATRIX SHARED DDL ENTITY%';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
SOURCE GRID PK ≠ MATRIX KEY: Source auto-increment PK (ProductColorId) is meaningless for Generate Matrix. Always add a separate DDL KEY field (ColorId) on Source; Matrix KEY points at that DDL field.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt LIKE N'%MATRIX SHARED DDL ENTITY%'
  AND SystemPrompt NOT LIKE N'%SOURCE GRID PK ≠ MATRIX KEY%';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
MATRIX SCHEMA LOOP FIX: propose_schema auto-adds Source identity PK + parent Link + DDL KEY and Matrix KEY columns before review. After Confirmed:true NEVER call propose_schema again — even if MatrixConfig.applied=false (PARTIAL). Use configure_matrix_unit / alter_table / inspect_matrix_unit only.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt LIKE N'%SOURCE GRID PK ≠ MATRIX KEY%'
  AND SystemPrompt NOT LIKE N'%MATRIX SCHEMA LOOP FIX%';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Execute approved schema. saasApplicationId: use HostContext when in My Application Editor (forced). Matrix: auto-normalizes KEY columns. If Matrix PARTIAL: keep TransactionId — DO NOT propose_schema; call configure_matrix_unit.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'execute_approved_schema';
GO

UPDATE dbo.AppAgentLibraryTool SET ToolDescription =
N'Create app package ONLY for greenfield. If HostContext.saasApplicationId is set (My Application Editor), returns that Id and creates nothing new — pass it to all subsequent tools.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_app_package';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
HOST APP REUSE: When HostContext.saasApplicationId is present, never create a new Application package. New transaction = under that SaasApplicationId. create_app_package returns the host Id only.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt IS NOT NULL
  AND SystemPrompt NOT LIKE N'%HOST APP REUSE%';
GO
