-- V093: app-configurator (platform-form-config) — Unit Editor field tools + Alter Table (DDL) for chain:
--   alter_table (ADD COLUMN) -> add_existing_table_fields -> request_form_reload
-- Also: create_transaction_field (Add Temp Field), remove_transaction_field (Remove Field).
-- Plugin: App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin
-- Idempotent: IF NOT EXISTS inserts; UPDATE descriptions/config for re-run.

DECLARE @Lib nvarchar(100) = N'platform-form-config';
DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

-- ── Register tools ───────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'create_transaction_field')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    @Lib, N'create_transaction_field',
    N'UI Add Temp Field: create IsTempVariable field on a unit (no DB column). Preview -> ask_user -> confirmed=true -> request_form_reload.',
    N'{"displayName":{"type":"string","required":true},"unitId":{"type":"integer"},"unitName":{"type":"string"},"controlType":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateTransactionField"}', 1, 46);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'add_existing_table_fields')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    @Lib, N'add_existing_table_fields',
    N'UI Add Table Existing Field: map existing table columns to fields. columnNamesJson = JSON array. Missing columns -> alter_table first. Preview -> ask_user -> confirmed=true -> request_form_reload.',
    N'{"columnNamesJson":{"type":"string","required":true},"unitId":{"type":"integer"},"unitName":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"AddExistingTableFields"}', 1, 47);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'remove_transaction_field')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    @Lib, N'remove_transaction_field',
    N'UI Remove Field: delete AppTransactionField definition only (no DROP COLUMN). Prefer set_field_visibility when unsure. Preview -> ask_user -> confirmed=true -> request_form_reload.',
    N'{"fieldId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"RemoveTransactionField"}', 1, 48);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'alter_table')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    @Lib, N'alter_table',
    N'UI Alter Table: single ALTER TABLE (prefer ADD COLUMN). DDL only. Then add_existing_table_fields for new columns. Preview -> ask_user -> confirmed=true.',
    N'{"tableName":{"type":"string","required":true},"alterSql":{"type":"string","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"AlterTable"}', 1, 49);

-- Keep descriptions/config in sync on re-run
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'UI Add Temp Field: create IsTempVariable field on a unit (no DB column). Preview -> ask_user -> confirmed=true -> request_form_reload.',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateTransactionField"}',
    IsActive = 1
WHERE LibraryKey = @Lib AND ToolName = N'create_transaction_field';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'UI Add Table Existing Field: map existing table columns to fields. columnNamesJson = JSON array. Missing columns -> alter_table first. Preview -> ask_user -> confirmed=true -> request_form_reload.',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"AddExistingTableFields"}',
    IsActive = 1
WHERE LibraryKey = @Lib AND ToolName = N'add_existing_table_fields';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'UI Remove Field: delete AppTransactionField definition only (no DROP COLUMN). Prefer set_field_visibility when unsure. Preview -> ask_user -> confirmed=true -> request_form_reload.',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"RemoveTransactionField"}',
    IsActive = 1
WHERE LibraryKey = @Lib AND ToolName = N'remove_transaction_field';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'UI Alter Table: single ALTER TABLE (prefer ADD COLUMN). DDL only. Then add_existing_table_fields for new columns. Preview -> ask_user -> confirmed=true.',
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"AlterTable"}',
    IsActive = 1
WHERE LibraryKey = @Lib AND ToolName = N'alter_table';
GO

-- ── Subscription: app-configurator must have platform-form-config (already used by V077+) ──
-- No new library row; tools live under existing LibraryKey.

-- ── System prompt: append unit-field chain (idempotent) ──────────────────────
-- Adds a short section; full prompt remains as set in V091.
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ UNIT FIELDS & TABLE CHAIN (V093) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Map UI buttons to tools (never say "ask a DBA" for these):
- Add Temp Field -> create_transaction_field(displayName, unitId?)
- Add Table Existing Field -> add_existing_table_fields(columnNamesJson, unitId?) — columns must exist on the table
- Remove Field -> remove_transaction_field(fieldId)
- Alter Table -> alter_table(tableName, alterSql) — ADD COLUMN only in normal use

Missing DB column chain:
  1) find_fields / get_unit_fields — confirm column not already a field
  2) ask_user — confirm ALTER TABLE SQL and column name/type
  3) alter_table(confirmed=true)
  4) add_existing_table_fields(columnNamesJson=["NewCol"], unitId, confirmed=true)
  5) request_form_reload

Temp staging (no DDL):
  create_transaction_field × N -> DT response mapping -> update_unit_formula -> request_form_reload
'
WHERE AgentCode = N'app-configurator'
  AND (SystemPrompt IS NULL OR SystemPrompt NOT LIKE N'%UNIT FIELDS & TABLE CHAIN (V093)%');
GO
