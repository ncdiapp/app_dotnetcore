-- V087: app-configurator — Data Load CRUD + Data Transfer mapping/types + Message Template CRUD.
-- Merged former V087 + V088. Idempotent: IF NOT EXISTS inserts; always UPDATE descriptions;
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
