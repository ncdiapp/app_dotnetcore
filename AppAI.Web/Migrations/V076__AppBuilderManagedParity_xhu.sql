-- V076: App Builder managed parity + split datasource/SP libraries + prompt efficiency (merged former V077).
-- 1) Split platform-database extras into platform-datasource / platform-stored-procedure
-- 2) Wipe app-builder agent-owned tools + library subscriptions/exclusions
-- 3) Re-subscribe app-builder to builder libraries; noise exclusions only
-- 4) Align SystemPrompt / ParameterSchemaJson / gate ToolDescriptions (efficient greenfield path)
-- Idempotent. Re-run after deleting Version from _SchemaMigrations if needed.
--   DELETE FROM dbo._SchemaMigrations WHERE Version = N'V076__AppBuilderManagedParity_xhu';
GO

-- ========== A) Library split ==========
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-datasource')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-datasource', N'platform',
    N'BuiltIn: Data Source Catalog',
    N'Tenant AppDataSourceRegister catalog: list registers and smoke-test connections. Never returns connection strings. Subscribe separately from platform-database (schema/SQL/DDL).',
    N'BuiltIn', 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-stored-procedure')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-stored-procedure', N'platform',
    N'BuiltIn: Stored Procedures',
    N'AI-published stored procedures (AppStoredProcedureRegister): list/search/detail/execute on a chosen DataSource. Prefer search → detail → execute. Subscribe separately from platform-database (ad-hoc SQL / DDL).',
    N'BuiltIn', 1);
GO

UPDATE dbo.AppAgentToolLibrary
SET Description = N'Physical database schema, ad-hoc SQL, table DDL, mockup data, and drop confirmation. DataSource catalog → platform-datasource. Registered SP tools → platform-stored-procedure.',
    LibraryName = N'BuiltIn: Database Schema & SQL'
WHERE LibraryKey = N'platform-database';
GO

INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder, TemplateContent, TemplateMetaJson, CallNotes)
SELECT N'platform-datasource', t.ToolName, t.ToolDescription, t.ParameterSchemaJson, t.ToolType, t.ToolConfig, t.IsActive, t.SortOrder, t.TemplateContent, t.TemplateMetaJson, t.CallNotes
FROM dbo.AppAgentLibraryTool t
WHERE t.LibraryKey = N'platform-database'
  AND t.ToolName IN (N'list_tenant_data_sources', N'test_data_source_connection')
  AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool x WHERE x.LibraryKey = N'platform-datasource' AND x.ToolName = t.ToolName);
GO
DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'platform-database'
  AND ToolName IN (N'list_tenant_data_sources', N'test_data_source_connection');
GO

INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder, TemplateContent, TemplateMetaJson, CallNotes)
SELECT N'platform-stored-procedure', t.ToolName, t.ToolDescription, t.ParameterSchemaJson, t.ToolType, t.ToolConfig, t.IsActive, t.SortOrder, t.TemplateContent, t.TemplateMetaJson, t.CallNotes
FROM dbo.AppAgentLibraryTool t
WHERE t.LibraryKey = N'platform-database'
  AND t.ToolName IN (N'stored_procedure_list', N'stored_procedure_search', N'stored_procedure_detail', N'stored_procedure_execute')
  AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool x WHERE x.LibraryKey = N'platform-stored-procedure' AND x.ToolName = t.ToolName);
GO
DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'platform-database'
  AND ToolName IN (N'stored_procedure_list', N'stored_procedure_search', N'stored_procedure_detail', N'stored_procedure_execute');
GO

INSERT INTO dbo.AppAgentToolExclusion (LibraryKey, ToolName, AgentID)
SELECT N'platform-datasource', e.ToolName, e.AgentID
FROM dbo.AppAgentToolExclusion e
WHERE e.LibraryKey = N'platform-database'
  AND e.ToolName IN (N'list_tenant_data_sources', N'test_data_source_connection')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentToolExclusion x
      WHERE x.AgentID = e.AgentID AND x.LibraryKey = N'platform-datasource' AND x.ToolName = e.ToolName);
GO
DELETE FROM dbo.AppAgentToolExclusion
WHERE LibraryKey = N'platform-database'
  AND ToolName IN (N'list_tenant_data_sources', N'test_data_source_connection');
GO

INSERT INTO dbo.AppAgentToolExclusion (LibraryKey, ToolName, AgentID)
SELECT N'platform-stored-procedure', e.ToolName, e.AgentID
FROM dbo.AppAgentToolExclusion e
WHERE e.LibraryKey = N'platform-database'
  AND e.ToolName IN (N'stored_procedure_list', N'stored_procedure_search', N'stored_procedure_detail', N'stored_procedure_execute')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentToolExclusion x
      WHERE x.AgentID = e.AgentID AND x.LibraryKey = N'platform-stored-procedure' AND x.ToolName = e.ToolName);
GO
DELETE FROM dbo.AppAgentToolExclusion
WHERE LibraryKey = N'platform-database'
  AND ToolName IN (N'stored_procedure_list', N'stored_procedure_search', N'stored_procedure_detail', N'stored_procedure_execute');
GO

INSERT INTO dbo.AppAgentLibrarySubscription (LibraryKey, AgentID)
SELECT DISTINCT N'platform-datasource', s.AgentID
FROM dbo.AppAgentLibrarySubscription s
WHERE s.LibraryKey = N'platform-database'
  AND EXISTS (
      SELECT 1 FROM (VALUES (N'list_tenant_data_sources'), (N'test_data_source_connection')) v(ToolName)
      WHERE NOT EXISTS (
          SELECT 1 FROM dbo.AppAgentToolExclusion e
          WHERE e.AgentID = s.AgentID AND e.LibraryKey = N'platform-datasource' AND e.ToolName = v.ToolName))
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = s.AgentID AND x.LibraryKey = N'platform-datasource');
GO

INSERT INTO dbo.AppAgentLibrarySubscription (LibraryKey, AgentID)
SELECT DISTINCT N'platform-stored-procedure', s.AgentID
FROM dbo.AppAgentLibrarySubscription s
WHERE s.LibraryKey = N'platform-database'
  AND EXISTS (
      SELECT 1 FROM (VALUES
          (N'stored_procedure_list'), (N'stored_procedure_search'),
          (N'stored_procedure_detail'), (N'stored_procedure_execute')) v(ToolName)
      WHERE NOT EXISTS (
          SELECT 1 FROM dbo.AppAgentToolExclusion e
          WHERE e.AgentID = s.AgentID AND e.LibraryKey = N'platform-stored-procedure' AND e.ToolName = v.ToolName))
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = s.AgentID AND x.LibraryKey = N'platform-stored-procedure');
GO

DELETE s
FROM dbo.AppAgentLibrarySubscription s
WHERE s.LibraryKey IN (N'platform-datasource', N'platform-stored-procedure')
  AND EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool t WHERE t.LibraryKey = s.LibraryKey AND t.IsActive = 1)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibraryTool t
      WHERE t.LibraryKey = s.LibraryKey AND t.IsActive = 1
        AND NOT EXISTS (
            SELECT 1 FROM dbo.AppAgentToolExclusion e
            WHERE e.AgentID = s.AgentID AND e.LibraryKey = s.LibraryKey AND e.ToolName = t.ToolName));
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
WHERE e.LibraryKey IN (N'platform-datasource', N'platform-stored-procedure')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription s
      WHERE s.AgentID = e.AgentID AND s.LibraryKey = e.LibraryKey);
GO

-- ========== B) Wipe app-builder agent tool/library config ==========
DELETE r
FROM dbo.AppAgentToolRegister r
WHERE r.SkillKey = N'app-builder';
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet k ON k.AgentID = e.AgentID
WHERE k.AgentCode = N'app-builder';
GO

DELETE s
FROM dbo.AppAgentLibrarySubscription s
INNER JOIN dbo.AppAgentSkillSet k ON k.AgentID = s.AgentID
WHERE k.AgentCode = N'app-builder';
GO

-- ========== C) Prompt + limits ==========
UPDATE dbo.AppAgentSkillSet
SET MaxToolResultChars = CASE WHEN ISNULL(MaxToolResultChars,0) < 10000 THEN 10000 ELSE MaxToolResultChars END,
    MaxIterations = CASE WHEN ISNULL(MaxIterations,0) < 40 THEN 40 ELSE MaxIterations END,
    CapabilityFlags = ISNULL(CapabilityFlags,0) | 31,
    SystemPrompt = N'You are AppBuilder AI, an intelligent agent embedded in the AppAI low-code/no-code platform.
Your job is to build complete business applications on behalf of the user.

━━━ PLATFORM CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Application Package: A top-level named container for all parts of one application.
  Its Id is called SaasApplicationId — thread it through every subsequent tool call.
- Entity Data Source: A dropdown list definition used in form fields.
  SimpleList   = fixed enumeration items coded at design time (never grows from user data).
  DatabaseTable = dynamic rows read from a real database table at runtime.
- Transaction: A data model + UI screen linked to one or more tables,
  with a form (data entry) and a search view (listing records).

━━━ EFFICIENCY RULES (GREENFIELD BUILDS) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When building a NEW application from a short user request ("create a simple X app"):
  ✗ Do NOT call get_existing_transactions, search_platform, or list_applications mid-build.
  ✗ Do NOT call list_applications just to summarize — use tool results you already have.
  ✗ Do NOT call search_memory unless you need historical context for this tenant.
  ✗ Do NOT call get_transaction_details or get_table_schema after execute_approved_schema when Fields / TableColumns are returned.
  ✓ Step 3 entities BEFORE Step 4 — then pass entityMapJson into execute_approved_schema.
  ✓ Wire EVERY enum/FK dropdown (no silent skip). Prefer TableName.ColumnName keys when names collide (e.g. Status).
  ✓ Follow execute_approved_schema NextStep / Fields / FKMappings / TableColumns exactly.
  ✓ One execute_sql statement per call (never batch with '';'').
  ✓ On execute_approved_schema failure with SchemaCleared/RecoveryInstruction: revise requirements and call propose_schema AGAIN — do NOT jump to create_application on the first failure.

━━━ STRICT WORKFLOW ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1 — EXPLORE (once)
  Call explore_platform for a compact overview.
  Optionally search_platform / search_memory only when looking up an EXISTING item by name.

Step 2 — CREATE APPLICATION PACKAGE
  Call create_app_package with the application name.
  Remember SaasApplicationId — pass it to ALL subsequent calls.
  Skip only if a matching application already exists from Step 1.

Step 3 — CREATE ENTITY DATA SOURCES  (REQUIRED before schema when dropdowns exist)
  MUST complete this BEFORE propose_schema / execute_approved_schema whenever Status/Type/Category/UoM (or similar) dropdowns are needed.
  ALWAYS use create_entity_simple_list for fixed enumerations (Status, Type, Priority, UoM, Yes/No).
  ALWAYS use create_entity_from_table for managed lookup tables (Customer, Category, Department, Product) that need their own screens.
  Remember each EntityId for entityMapJson.
  Always pass SaasApplicationId from Step 2.

Step 4 — DESIGN + BUILD DATABASE + TRANSACTIONS  (two-step schema review)

  Step 4a — PROPOSE SCHEMA:
    Call propose_schema(requirements, appName) BEFORE any DDL.
    appName = short alphanumeric code (max 10), e.g. Ord or Sales — NOT the full package display name.
    In requirements, use UNPREFIXED logical table names (Order, OrderItem) — the platform prefixes once.
    Do NOT invent a different short prefix in table names (avoid Xxx_Order when appName is Ord).
    If Confirmed:false — adjust and call propose_schema again.
    If Confirmed:true — proceed IMMEDIATELY to Step 4b.
    ✗ Do NOT call propose_plan before execute_approved_schema — propose_schema is the gate.
    After an execute failure that returns SchemaCleared=true, you MUST call propose_schema again with fixed requirements.

  Step 4b — EXECUTE APPROVED SCHEMA:
    Call execute_approved_schema(saasApplicationId, transactionName, entityMapJson).
    entityMapJson example after Step 3:
      {"Ord_Order.Status":4577,"Ord_OrderItem.Category":4578,"ProductId":4580}
    Keys may be ColumnName, TableName.ColumnName, UnitName.ColumnName, or field:{fieldId}.
    Returns TransactionId, LookupTables[], FKMappings[], Fields[], TableColumns[], EntityWiring[].
    If NextStep lists unbound enum fields, immediately set_field_entity(fieldId=…) for EACH — do not skip.
    If IsSuccess=false and SchemaCleared=true: follow RecoveryInstruction (re-propose). Do not use create_application until two failed propose/execute cycles or the user skips review.
    ✗ Do NOT call get_transaction_details when Fields[] is present.
    ✗ Do NOT call get_table_schema when TableColumns[] is present.

    For EACH table in LookupTables[]:
      a) create_entity_from_table(tableName, saasApplicationId) → note entityId
      b) For matching FKMappings rows, set_field_entity(transactionId, fieldId, entityId)
      c) create_transaction_from_table(tableName, saasApplicationId)
      d) create_list_edit_form(transactionId)  ← adds nav menu; do NOT also create_search_view on lookups
    ✗ Do NOT propose_schema / execute_approved_schema again for lookups.

  FALLBACK (only when user explicitly skips review):
    Call create_application (requires propose_plan first). Pass entityMapJson when possible.

Step 5 — MASTER SEARCH VIEW
  Call create_search_view for the master-detail TransactionId from execute_approved_schema.
  Do NOT call create_search_view for lookup list-edit screens (already handled by create_list_edit_form).

Step 5b — MOCKUP DATA
  insert_mockup_data once per table using TableColumns from execute_approved_schema: lookups first, then master, then child.
  Realistic domain values. Verify with one execute_sql SELECT per table when needed.
  ✗ Do NOT call get_table_schema for tables you just created unless a column is missing from TableColumns.

Step 6 — STANDALONE SEARCH / REPORT (optional)
  Call create_search only if the user asked for a report/search screen beyond transaction UIs.
  Otherwise skip.

Step 7 — MENU
  create_search_view / create_list_edit_form usually add menus.
  Use add_search_to_menu / add_transaction_to_menu only when auto-add did not happen.
  NEVER claim a menu item was added unless the tool returned IsSuccess=true.

Step 8 — SUMMARIZE
  Report application name, SaasApplicationId, tables, transaction IDs, entities, searches, menus —
  from prior tool results. Do not re-list the whole tenant via list_applications.

━━━ PLAN-BEFORE-BUILD RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Call propose_plan ONLY before:
  • create_application
  • create_database_table
  • create_hierarchy_from_tables (recovery)
  • delete / destructive flows (see DELETE APPLICATION)
✗ NOT before propose_schema or execute_approved_schema — propose_schema is the approval gate.

━━━ DELETE APPLICATION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to DELETE / REMOVE / CLEAN UP an entire application:
  1. Call list_applications to find the application and collect its table names.
  2. Call propose_plan — summarise what will be deleted.
  3. Call confirm_drop_tables — user decides DROP vs keep tables.
  4. Call delete_application(applicationName, dropDatabaseTables: <value from step 3>).
  5. Report the summary.

NEVER call delete_transaction individually when the user wants the whole app gone.
NEVER skip confirm_drop_tables.

━━━ ITERATIVE REFINEMENT ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to MODIFY an existing app (not build from scratch):
  1. Call explore_platform and/or get_transaction_details to find the target transaction AND the fieldId.
  2. Use update_transaction_field / set_field_entity with the fieldId — NOT just fieldName.
  3. Use delete_transaction to remove an unwanted screen (does NOT drop the DB table).
  4. Never call create_application, propose_schema, or execute_approved_schema for modification requests.

ADDING A NEW COLUMN TO AN EXISTING TABLE:
  Use alter_table — keeps database AND AppAI data model in sync.
  Pass transactionId and newFieldJson. NEVER use create_database_table to add columns.

FIELD IDENTIFICATION RULE — CRITICAL:
  fieldId is unique. ALWAYS prefer fieldId from get_transaction_details.
  If you only know fieldName, also pass unitName.

━━━ RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- ALWAYS call explore_platform once at the start of a greenfield build.
- ALWAYS pass SaasApplicationId to create_entity_*, create_application, create_transaction_from_table, create_search.
- NEVER call create_database_table or create_transaction_from_table after create_application succeeds for the same composition.
- create_transaction_from_table is for lookup/pre-existing tables (and LookupTables[] after execute_approved_schema).
- If a tool returns IsSuccess=false, read Error and adjust — never blind-retry the same call.

━━━ TWO RELATIONSHIP TYPES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
COMPOSITION (master-detail): child cannot exist without parent; FK in CHILD → parent PK.
  → Master-detail transaction from execute_approved_schema / create_application.
LOOKUP / REFERENCE: referenced entity has its own lifecycle; FK points OUTWARD.
  → create_entity_from_table + create_transaction_from_table + create_list_edit_form.
  → NEVER include lookup tables as child units in the master-detail hierarchy.

━━━ FK DIRECTION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FK in B → A PK: B is composition child of A.
FK in A → B PK: B is lookup referenced by A (DDL / EntityId on A).

━━━ LIST EDIT WORKFLOW (lookup tables) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  A) create_entity_from_table
  B) create_transaction_from_table
  C) create_list_edit_form  ← menu auto; skip create_search_view for lookups
  Wire FK dropdowns via FKMappings / set_field_entity / entityMapJson.

━━━ RECOVERY — tables exist but transaction missing ━━━━━━━━━━━━━━━━━━━━━━━
Do NOT call create_application again. Use create_hierarchy_from_tables with exact table names,
then create_search_view for the master transaction.

━━━ RESUME RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user says "Resume building...":
  ✗ Do NOT call explore_platform / propose_plan / propose_schema again if already done.
  Jump to the first incomplete step (often execute_approved_schema with stored schema).

━━━ MEMORY (RAG) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Call search_memory only when historical context helps. Skip for entirely new greenfield tasks.',
    IsActive = 1
WHERE AgentCode = N'app-builder';
GO

-- ========== D) Fresh library subscriptions (builder surface only) ==========
INSERT INTO dbo.AppAgentLibrarySubscription (LibraryKey, AgentID)
SELECT v.LibraryKey, s.AgentID
FROM dbo.AppAgentSkillSet s
CROSS JOIN (VALUES
  (N'platform-application'),
  (N'platform-transaction'),
  (N'platform-search'),
  (N'platform-database'),
  (N'platform-memory')
) v(LibraryKey)
WHERE s.AgentCode = N'app-builder'
  AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = v.LibraryKey)
  AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription x
    WHERE x.AgentID = s.AgentID AND x.LibraryKey = v.LibraryKey);
GO

-- Noise exclusions only (NOT get_database_schema)
INSERT INTO dbo.AppAgentToolExclusion (LibraryKey, ToolName, AgentID)
SELECT v.LibraryKey, v.ToolName, s.AgentID
FROM dbo.AppAgentSkillSet s
CROSS JOIN (VALUES
  (N'platform-application', N'list_tenant_saas_applications'),
  (N'platform-search', N'list_available_searches'),
  (N'platform-search', N'get_search_criteria')
) v(LibraryKey, ToolName)
WHERE s.AgentCode = N'app-builder'
  AND EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = s.AgentID AND x.LibraryKey = v.LibraryKey)
  AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentToolExclusion x
    WHERE x.AgentID = s.AgentID AND x.LibraryKey = v.LibraryKey AND x.ToolName = v.ToolName);
GO

-- ========== E) Gate tool descriptions (library) — efficiency-aligned ==========
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Approval gate for create_application, create_database_table, create_hierarchy_from_tables, and delete flows. Presents a plan and WAITS for ConfirmPlan. Do NOT call before propose_schema / execute_approved_schema — propose_schema is already the schema gate.'
WHERE LibraryKey = N'platform-application' AND ToolName = N'propose_plan';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Extract DB schema from requirements and WAIT for ConfirmSchema. Pass short appName code (max 10 alnum), UNPREFIXED table names. Returns {Confirmed:true} with schema stored — then execute_approved_schema with NO schemaJson. After execute SchemaCleared failure, call propose_schema AGAIN with fixed requirements.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'propose_schema';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Execute schema from propose_schema: create DB tables + master-detail AppTransaction. Pass saasApplicationId + entityMapJson (ColumnName or Table.Column or field:{id}). Returns Fields/TableColumns/EntityWiring. On failure clears stored schema and returns RecoveryInstruction — revise + propose_schema again; do not create_application on first failure.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'execute_approved_schema';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create a MasterDetail edit form linked to an existing List-type transaction. Only parameter: listTransactionId. Adds nav menu — do NOT also call create_search_view for the same lookup list.'
WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_list_edit_form';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Execute a single SQL SELECT. ONE statement per call — do not batch with '';''. Optional dataSourceId. Platform may COUNT first for row-limit gating.'
WHERE LibraryKey = N'platform-database' AND ToolName = N'execute_sql';
GO
-- ========== E2) Sync ToolRegister + ParameterSchemaJson for propose_plan / execute_sql ==========
UPDATE r
SET ToolDescription = t.ToolDescription
FROM dbo.AppAgentToolRegister r
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentCode = r.SkillKey
INNER JOIN dbo.AppAgentLibraryTool t ON t.ToolName = r.ToolName
WHERE s.AgentCode = N'app-builder'
  AND t.LibraryKey IN (N'platform-application', N'platform-transaction', N'platform-database')
  AND r.ToolName IN (N'propose_plan', N'propose_schema', N'execute_approved_schema', N'create_list_edit_form', N'execute_sql');
GO

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "sql": {"type":"string","description":"A single SELECT statement. Do not include multiple statements separated by semicolon.","required":true},
  "dataSourceId": {"type":"integer","description":"Optional DataSourceRegisterId; omit for session default.","required":false}
}'
WHERE LibraryKey = N'platform-database' AND ToolName = N'execute_sql';
GO

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "planSummary": {"type":"string","description":"Plain-text summary of what will be created or deleted.","required":true},
  "tablesToCreate": {"type":"string","description":"Comma-separated NEW table names (create flows).","required":false},
  "screensToCreate": {"type":"string","description":"Comma-separated transaction/screen names.","required":false}
}'
WHERE LibraryKey = N'platform-application' AND ToolName = N'propose_plan';
GO

-- ========== F) Backfill ParameterSchemaJson on library tools ==========
UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "planSummary": {"type":"string","description":"Plain-text summary of what will be created: which tables, fields, relationships, and screens.","required":true},
  "tablesToCreate": {"type":"string","description":"Comma-separated list of database table names that will be physically created."},
  "screensToCreate": {"type":"string","description":"Comma-separated list of transaction/screen names that will be generated."}
}'
WHERE ToolName=N'propose_plan'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "requirements": {"type":"string","description":"Detailed natural-language description of the entities, fields, and relationships to build.","required":true},
  "appName": {"type":"string","description":"Application name used to prefix table names once (e.g. SalesOrder). Pass UNPREFIXED logical table names in requirements. Max 10 chars.","required":true}
}'
WHERE ToolName=N'propose_schema'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "applicationName": {"type":"string","description":"Display name for the new application, e.g. Sales Order Management.","required":true}
}'
WHERE ToolName=N'create_app_package'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "entityCode": {"type":"string","description":"Name/code for this entity, e.g. Order Status.","required":true},
  "items": {"type":"string","description":"Comma-separated list of item labels in order.","required":true},
  "saasApplicationId": {"type":"integer","description":"The SaasApplicationId returned by create_app_package.","required":true},
  "description": {"type":"string","description":"Optional description of this entity."}
}'
WHERE ToolName=N'create_entity_simple_list'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "entityCode": {"type":"string","description":"Name/code for this entity.","required":true},
  "tableName": {"type":"string","description":"Database table name without schema.","required":true},
  "schemaOwner": {"type":"string","description":"Schema owner of the table, e.g. dbo.","required":true},
  "identityField": {"type":"string","description":"Column name used as selected value (PK).","required":true},
  "displayField1": {"type":"string","description":"Primary display column.","required":true},
  "saasApplicationId": {"type":"integer","description":"The SaasApplicationId returned by create_app_package.","required":true},
  "displayField2": {"type":"string","description":"Optional secondary display column."},
  "displayField3": {"type":"string","description":"Optional tertiary display column."},
  "description": {"type":"string","description":"Optional description."}
}'
WHERE ToolName=N'create_entity_from_table'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "listTransactionId": {"type":"integer","description":"TransactionId of the existing List-type transaction to add an edit form to.","required":true}
}'
WHERE ToolName=N'create_list_edit_form'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "tableName": {"type":"string","description":"Table name being populated.","required":true},
  "insertSql": {"type":"string","description":"One or more SQL INSERT statements separated by semicolons.","required":true}
}'
WHERE ToolName=N'insert_mockup_data'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "transactionId": {"type":"integer","description":"The transaction ID to create the search view for.","required":true}
}'
WHERE ToolName=N'create_search_view'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{}'
WHERE ToolName=N'explore_platform'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "query": {"type":"string","description":"Keyword to search for against apps, transactions, entities, and tables.","required":true}
}'
WHERE ToolName=N'search_platform'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "query": {"type":"string","description":"Keywords describing what to recall from memory.","required":true}
}'
WHERE ToolName=N'search_memory'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "transactionName": {"type":"string","description":"Transaction name (case-insensitive).","required":true},
  "menuName": {"type":"string","description":"Display label in the navigation menu.","required":true}
}'
WHERE ToolName=N'add_transaction_to_menu'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "tableNames": {"type":"string","description":"Comma-separated table names that will be dropped if confirmed.","required":true}
}'
WHERE ToolName=N'confirm_drop_tables'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "applicationName": {"type":"string","description":"Application name to delete.","required":true},
  "dropDatabaseTables": {"type":"boolean","description":"True to also DROP physical tables. Default false."}
}'
WHERE ToolName=N'delete_application'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "transactionId": {"type":"integer","description":"The transaction ID to inspect.","required":true}
}'
WHERE ToolName=N'get_transaction_details'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "createTableSql": {"type":"string","description":"The full CREATE TABLE SQL script.","required":true}
}'
WHERE ToolName=N'create_database_table'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "tableName": {"type":"string","description":"Table name to alter.","required":true},
  "alterSql": {"type":"string","description":"Single ALTER TABLE SQL statement.","required":true},
  "transactionId": {"type":"integer","description":"Transaction ID to keep in sync."},
  "newFieldJson": {"type":"string","description":"JSON for new platform field when adding a column."}
}'
WHERE ToolName=N'alter_table'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "transactionId": {"type":"integer","description":"Transaction that owns the field.","required":true},
  "fieldId": {"type":"integer","description":"Preferred unique field Id from get_transaction_details."},
  "fieldName": {"type":"string","description":"Field DisplayName or column name when fieldId unknown."},
  "unitName": {"type":"string","description":"Unit/table name to scope fieldName search."},
  "changesJson": {"type":"string","description":"JSON properties to change.","required":true}
}'
WHERE ToolName=N'update_transaction_field'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "transactionId": {"type":"integer","description":"Transaction that owns the field.","required":true},
  "fieldId": {"type":"integer","description":"Preferred unique field Id."},
  "fieldName": {"type":"string","description":"Field name when fieldId unknown."},
  "unitName": {"type":"string","description":"Unit/table name to scope fieldName search."},
  "entityId": {"type":"integer","description":"Entity Data Source Id to link."}
}'
WHERE ToolName=N'set_field_entity'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "transactionId": {"type":"integer","description":"Transaction Id to delete.","required":true}
}'
WHERE ToolName=N'delete_transaction'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "saasApplicationId": {"type":"integer","description":"Optional SaasApplicationId filter."}
}'
WHERE ToolName=N'list_searches'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "saasApplicationId": {"type":"integer","description":"REQUIRED — SaasApplicationId from create_app_package.","required":true},
  "transactionName": {"type":"string","description":"Display name for the root transaction."},
  "entityMapJson": {"type":"string","description":"JSON mapping fields to EntityDataSource IDs. Keys: ColumnName, TableName.ColumnName, UnitName.ColumnName, or field:{fieldId}. Create SimpleLists before execute and pass their EntityIds here."}
}'
WHERE ToolName=N'execute_approved_schema'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "requirements": {"type":"string","description":"Detailed natural-language description of what to build.","required":true},
  "saasApplicationId": {"type":"integer","description":"REQUIRED — SaasApplicationId from create_app_package.","required":true},
  "appName": {"type":"string","description":"Optional root transaction name."},
  "entityMapJson": {"type":"string","description":"JSON mapping FK columns to EntityDataSource IDs."}
}'
WHERE ToolName=N'create_application'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "tableName": {"type":"string","description":"Existing database table name.","required":true},
  "saasApplicationId": {"type":"integer","description":"REQUIRED — SaasApplicationId from create_app_package.","required":true},
  "schemaOwner": {"type":"string","description":"Schema owner, e.g. dbo."}
}'
WHERE ToolName=N'create_transaction_from_table'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "masterTable": {"type":"string","description":"Root/master table name.","required":true},
  "childTables": {"type":"string","description":"Comma-separated child table names."},
  "saasApplicationId": {"type":"integer","description":"REQUIRED — SaasApplicationId from create_app_package.","required":true},
  "transactionName": {"type":"string","description":"Display name for the transaction."},
  "schemaOwner": {"type":"string","description":"Schema owner, e.g. dbo."},
  "grandchildMapJson": {"type":"string","description":"JSON mapping child table to grandchild table names."}
}'
WHERE ToolName=N'create_hierarchy_from_tables'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "searchName": {"type":"string","description":"Display name for the search screen.","required":true},
  "sqlQuery": {"type":"string","description":"SQL SELECT for the search.","required":true},
  "saasApplicationId": {"type":"integer","description":"SaasApplicationId from create_app_package.","required":true}
}'
WHERE ToolName=N'create_search'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

UPDATE dbo.AppAgentLibraryTool
SET ParameterSchemaJson = N'{
  "searchId": {"type":"integer","description":"Search or saved-search Id.","required":true},
  "menuName": {"type":"string","description":"Menu display label.","required":true},
  "saasApplicationId": {"type":"integer","description":"SaasApplicationId from create_app_package.","required":true},
  "isSavedSearch": {"type":"boolean","description":"True if searchId is a saved search."}
}'
WHERE ToolName=N'add_search_to_menu'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

GO
