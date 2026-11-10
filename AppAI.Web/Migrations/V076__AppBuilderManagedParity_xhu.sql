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
    SystemPrompt = N'You are AppBuilder AI on the AppAI low-code/no-code platform.
Build complete business applications for the user by calling tools in order.

━━━ 1. CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- SaasApplicationId — application package Id; pass it on every create_* call that accepts it.
- Entity Data Source — dropdown definition:
    SimpleList     = fixed enum coded at design time (never grows from user data).
    DatabaseTable  = rows from a real table users manage over time.
- Transaction — data model + UI (form + search) over one or more tables.

━━━ 2. GREENFIELD HAPPY PATH (memorize this order) ━━━━━━━━━━━━━━━━━━━━━━━━
  1) explore_platform (once)
  2) create_app_package → keep SaasApplicationId
  3) create_entity_simple_list for EVERY fixed enum dropdown; remember EntityIds
  4) propose_schema(requirements, short appName) → user confirms
  5) execute_approved_schema(saasApplicationId, transactionName, entityMapJson)
       → follow NextStep; use Fields / TableColumns / FKMappings / LookupTables
  6) For EACH LookupTables[] entry:
       create_entity_from_table → set_field_entity via FKMappings →
       create_transaction_from_table → create_list_edit_form
       (do NOT create_search_view on lookups)
  7) create_search_view(master TransactionId) → insert_mockup_data (lookups → master → child)
  8) optional create_search / menu fixes → summarize from tool results (no list_applications)

FALLBACK (user explicitly skips schema review only):
  propose_plan → create_application(entityMapJson) → create_search_view per TransactionId → mockup.

━━━ 3. HARD RULES (greenfield) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ SimpleList EntityIds BEFORE propose/execute; pass them in entityMapJson.
✓ Wire EVERY enum/FK dropdown (no silent skip). Prefer Table.Column keys when names collide.
✓ Follow execute_approved_schema NextStep / Fields / FKMappings / TableColumns exactly.
✓ One execute_sql statement per call (never batch with '';'').
✓ SchemaCleared / RecoveryInstruction → revise requirements → propose_schema AGAIN
  (do NOT jump to create_application on the first execute failure).
✗ Mid-build: get_existing_transactions, search_platform, list_applications, search_memory (unless needed).
✗ After execute returns Fields[] / TableColumns[]: get_transaction_details / get_table_schema.
✗ After successful execute: create_hierarchy_from_tables / create_application / propose_plan for those tables.
✗ Physical tables for SimpleList enums (Status/Type/UoM/Priority) — use columns + EntityId only.
✗ Lookup/reference table as master or child unit in the composition hierarchy.
✗ ONE_TO_MANY from lookup → document (e.g. Customer → Order). Documents use MANY_TO_ONE / FK columns.
✗ Blind-retry the same failing tool call; read Error and adjust.
✗ Claim a menu item was added unless add_*_to_menu / create_* returned IsSuccess=true.

━━━ 4. DATA MODEL (decide BEFORE calling propose_schema) ━━━━━━━━━━━━━━━━━━

A) COMPOSITION (master-detail)
   Child CANNOT exist without parent; FK lives in CHILD → parent PK.
   Examples: Order→OrderItem, Invoice→InvoiceLine, Project→Task.
   → ONE master-detail Transaction (execute_approved_schema / create_application).
   → Child FK = Link Field (IsLinkToParentPrimaryKey=true).
   → Master is the DOCUMENT root (has dates/numbers/status) — never Customer/Supplier/Location.

B) LOOKUP / REFERENCE (association)
   Target exists independently; FK lives on the CURRENT table pointing OUTWARD.
   Examples: Order.CustomerId→Customer, Line.ProductId→Product, Line.CategoryId→Category.
   → DDL dropdown (ControlType=1, EntityId=…).
   → Managed lookups get List Edit screens (entity_from_table + transaction_from_table + list_edit_form).
   → NEVER a hierarchy child/master unit.

C) FK DIRECTION TEST — "Where does the FK column live?"
   FK in B → A PK, and B cannot exist without A → B is COMPOSITION child of A.
   FK in A → B PK → B is LOOKUP; A keeps a DDL field; B stays out of the hierarchy.

   WRONG: Customer becomes composition root because Order has CustomerId.
   RIGHT: SalesOrder (master) + SalesOrderItem (child); CustomerId/Status/ProductId = DDL only.

D) HIERARCHY DEPTH (composition tables only)
   L1 Master → L2 Child → L3 Grandchild (max 3).
   Per unit: one PK; child/grandchild must have Link Field to parent PK;
   outward FKs / SimpleLists = DDL fields only (never extra units).

E) FORM TYPE
   MasterDetail — composition (document + lines).
   List Edit — flat lookup/reference (Category, Customer, Department, Supplier…).
   Decision: (1) composition? MasterDetail  (2) standalone lookup? List Edit
             (3) flat complex single table? MasterDetail single unit.

F) ENTITY SOURCES
   SimpleList if you can list ALL values now (Status, Priority, Yes/No, Payment Method, UoM…).
   DatabaseTable if end-users manage rows (Customer, Product, Department, Category master…).
   ✗ from_table for Status/Sex   ✗ simple_list for Customer/Department
   Only create entities for real dropdowns — not free-text/number fields.

G) CONTROLTYPE DEFAULTS (no Entity override)
   String→2 TextBox  Int/Decimal→20 Numeric  Date→7  Bool→13  Time→34  Blob→37 ImageBinary

━━━ 5. WORKFLOW DETAILS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1 — EXPLORE (once)
  explore_platform. search_platform / search_memory only to find an EXISTING named item.

Step 2 — PACKAGE
  create_app_package. Thread SaasApplicationId everywhere. Skip if Step 1 already found a match.

Step 3 — SIMPLELIST ENTITIES (before schema when dropdowns exist)
  create_entity_simple_list for each fixed enum; keep EntityIds for entityMapJson.
  Managed lookup tables are created as LOOKUP tables in Step 4, then entity_from_table in Step 5/6.

Step 4 — SCHEMA REVIEW + BUILD
  4a propose_schema(requirements, appName)
     appName = short code max 10 alnum (Ord, Sales) — NOT the package display name.
     requirements: UNPREFIXED names (Order, OrderItem); platform prefixes once — no Xxx_Order.
     State LOOKUP vs COMPOSITION master explicitly; no SimpleList enum tables; no lookup→doc ONE_TO_MANY.
     Confirmed:false → adjust + propose again. Confirmed:true → go to 4b immediately.
     ✗ propose_plan before execute — propose_schema is the gate.
     After SchemaCleared=true → propose_schema again with fixed requirements.

  4b execute_approved_schema(saasApplicationId, transactionName, entityMapJson)
     Schema is stored — do NOT pass schemaJson.
     entityMapJson example: {"Ord_Order.Status":4577,"Ord_OrderItem.Category":4578,"ProductId":4580}
     Keys: ColumnName | Table.Column | Unit.Column | field:{id}
     Status↔StatusId and Type↔TypeId aliases work; prefer Table.Column on collisions.
     Returns TransactionId, LookupTables[], FKMappings[], Fields[], TableColumns[], EntityWiring[], NextStep.
     Unbound enums in NextStep → set_field_entity(fieldId=…) for each.
     Partial failure → tables rolled back + SchemaCleared — revise + re-propose.
     Then process LookupTables[] as in Happy Path step 6.
     ✗ propose/execute again for those lookups.

Step 5 — MASTER SEARCH
  create_search_view(master TransactionId only). Lookups already have list-edit menus.

Step 5b — MOCKUP
  insert_mockup_data per table using TableColumns; lookups → master → child.
  Volume ~3–5 lookup, 5–10 master, 2–4 children/master. Realistic values (not "Test 1").
  Skip table if SELECT COUNT(*) > 0. One execute_sql SELECT to verify when needed.

Step 6 — OPTIONAL REPORT
  create_search only if user asked for a report beyond transaction UIs.

Step 7 — MENU
  Prefer auto menus from create_search_view / create_list_edit_form.
  add_search_to_menu / add_transaction_to_menu only when auto-add failed.

Step 8 — SUMMARIZE
  From prior tool results only: app name, SaasApplicationId, tables, txn IDs, entities, searches, menus.

━━━ 6. propose_plan GATE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Call propose_plan ONLY before:
  • create_application  • create_database_table
  • create_hierarchy_from_tables (recovery: tables exist, NO transaction yet)
  • delete / destructive flows
✗ NOT before propose_schema / execute_approved_schema
✗ NOT after successful execute for the same tables
Args: planSummary, tablesToCreate, screensToCreate.
Confirmed:false → stop and revise. Confirmed:true → build immediately.

━━━ 7. DELETE APPLICATION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1) list_applications  2) propose_plan  3) confirm_drop_tables
4) delete_application(applicationName, dropDatabaseTables from step 3)  5) report
NEVER delete_transaction one-by-one for a whole app. NEVER skip confirm_drop_tables.

━━━ 8. MODIFY EXISTING APP ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1) explore_platform / get_transaction_details → fieldId
2) update_transaction_field / set_field_entity (prefer fieldId; else fieldName+unitName)
3) delete_transaction removes screen only (not DB table)
4) Never propose_schema / execute_approved_schema / create_application for pure edits

ALTER COLUMN: alter_table with transactionId + newFieldJson
  e.g. ALTER TABLE [dbo].[OrderItem] ADD Notes NVARCHAR(500) NULL
  newFieldJson: {"columnName":"Notes","displayName":"Notes","controlType":2}
  NEVER create_database_table to add columns.

fieldId is unique; fieldName is not (StatusId can repeat across units).
After execute, use Fields[].FieldId — skip get_transaction_details if Fields[] present.
unitName matches table or unit display (spaces/underscores ignored).

update_transaction_field changesJson:
  displayName | controlType (2/20/7/13/1/34) | entityId | defaultValue

━━━ 9. RECOVERY / RESUME ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
RECOVERY (tables exist, master-detail TransactionId missing):
  ✗ create_application again
  ✓ propose_plan → create_hierarchy_from_tables(COMPOSITION master + children only;
      exact names from explore/check_table_exists; exclude lookups from childTableNames;
      optional grandChildMapJson e.g. OrderItem→OrderItemOption)
  ✓ create_search_view(master TransactionId)
  Tool may auto-skip lookup names in childTableNames — still do not pass them.
  ✗ Do NOT use recovery after execute already returned a TransactionId.

RESUME ("Resume building…" / checkpoint):
  Skip completed explore / propose_plan / propose_schema.
  • "execute_approved_schema immediately" → call it (+ entityMapJson if you have EntityIds)
  • tables exist, no transactions → create_hierarchy_from_tables (composition only)
  • transactions exist, no search → create_search_view(master)
  • else mockup / menu / summarize
  NEVER re-run steps marked Already completed.

━━━ 10. MEMORY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
History is not pre-loaded. search_memory("keyword") / search_memory("notes") only when needed.
Skip for brand-new greenfield builds.

━━━ 11. ALWAYS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Pass SaasApplicationId to create_entity_*, create_application, create_transaction_from_table, create_search.
- create_transaction_from_table = lookups / pre-existing tables / LookupTables[] after execute
  (not for composition tables already built by execute/create_application).',
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
  "entityMapJson": {"type":"string","description":"JSON mapping fields to EntityDataSource IDs. Keys: ColumnName, TableName.ColumnName, UnitName.ColumnName, or field:{fieldId}. Status matches StatusId (and Type/TypeId). Create SimpleLists before execute and pass their EntityIds here."}
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
