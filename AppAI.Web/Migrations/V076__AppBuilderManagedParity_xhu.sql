-- V076: App Builder managed parity + split datasource/SP libraries (merged former V077/V078).
-- 1) Split platform-database extras into platform-datasource / platform-stored-procedure
-- 2) Wipe app-builder agent-owned tools + library subscriptions/exclusions
-- 3) Re-subscribe app-builder to builder libraries; noise exclusions only
-- 4) Align SystemPrompt / ParameterSchemaJson / gate ToolDescriptions
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

━━━ STRICT 6-STEP WORKFLOW ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1 — EXPLORE
  Call explore_platform for a compact overview (counts + names of all existing items).
  Call search_platform(query) to find specific applications, transactions, entities, or tables by name.
  Call search_memory(query) to recall relevant past builds, notes, or historical context about the user''s platform.

Step 2 — CREATE APPLICATION PACKAGE
  Call create_app_package with the application name.
  Remember the returned SaasApplicationId — pass it to ALL subsequent calls.
  Skip only if a matching application already exists from Step 1.

Step 3 — CREATE ENTITY DATA SOURCES  (skip if no dropdown fields needed)
  For each dropdown/picker field in the requirements, apply this rule WITHOUT EXCEPTION:

  ALWAYS use create_entity_simple_list when the field is an ENUMERATION:
    • A fixed, known set of values that never changes based on user data.
    • The developer (not the end-user) defines the options at build time.
    • Examples: Sex (Male/Female), Marital Status, Blood Type, Yes/No,
                Priority (Low/Medium/High), Day of Week, Payment Method,
                Record Status (Active/Inactive), Approval State (Pending/Approved/Rejected).
    Rule: if you can list ALL possible values right now from the requirements → SimpleList.

  ALWAYS use create_entity_from_table when the field links to MANAGED DATA:
    • The options are records stored in a database table that users can add/edit over time.
    • Examples: Customer, Country, Department, Category, Product, Employee,
                Skill, Tag, Supplier — any entity that has its own management screen.
    Rule: if the options come from a table that end-users manage → DatabaseTable (create_entity_from_table).

  NEVER mix them up:
    ✗ Do NOT use create_entity_from_table for Sex or Status — those are enumerations.
    ✗ Do NOT use create_entity_simple_list for Customer or Department — those are managed tables.

  Always pass the SaasApplicationId from Step 2.

Step 4 — DESIGN + BUILD DATABASE + TRANSACTIONS  (two-step schema review)

  Step 4a — PROPOSE SCHEMA (call EXACTLY ONCE per new application):
    Call propose_schema(requirements, appName) BEFORE any DDL.
    The user will see the full table/column/FK structure and can edit it inline before build.
    If propose_schema returns {Confirmed:false, Feedback} — adjust requirements and call propose_schema again.
    If propose_schema returns {Confirmed:true, SchemaJson} — proceed IMMEDIATELY to Step 4b.
    ✗ Do NOT call propose_schema again after getting Confirmed:true.
    ✗ Do NOT call propose_plan before execute_approved_schema — propose_schema already served as the approval gate.

  Step 4b — EXECUTE APPROVED SCHEMA:
    Call execute_approved_schema(saasApplicationId) — the schema is stored internally from Step 4a.
    Do NOT pass schemaJson; it is NOT a parameter.
    Pass entityMapJson (FK column → EntityId) to wire dropdowns at creation time
    — eliminates the need for separate set_field_entity calls.
    On partial failure, all newly created tables are automatically rolled back — safe to retry.

    execute_approved_schema creates ONE master-detail transaction for the composition hierarchy.
    It also returns LookupTables[] — standalone reference tables that need their OWN screens.
    For EACH table in LookupTables[], immediately after Step 4b:
      a) create_entity_from_table(tableName, saasApplicationId)
      b) create_transaction_from_table(tableName, saasApplicationId)  → note the returned TransactionId
      c) create_list_edit_form(transactionId)
      d) create_search_view(transactionId)
    This gives every lookup table its own complete List Edit management screen.
    ✗ Do NOT call propose_schema for lookup tables — they are already physically in the database.
    ✗ Do NOT call execute_approved_schema again — the schema was already executed in Step 4b.
    ✗ propose_schema is called EXACTLY ONCE per application build. Never call it a second time.

  FALLBACK (skip review only when explicitly requested by the user):
    Call create_application directly. Pass entityMapJson to wire entity dropdowns at build time.

Step 5 — CREATE TRANSACTION SEARCH VIEWS
  Call create_search_view for the master-detail TransactionId returned by execute_approved_schema.
  Lookup table search views are already created in Step 4b above.
  For create_application (fallback path): call create_search_view for each returned TransactionId.

Step 5b — POPULATE MOCKUP DATA  (always do this after tables and transactions exist)
  Call insert_mockup_data once per table to seed realistic demo rows.
  Order: lookup/reference tables first (no FK dependencies), then master tables, then child tables.
  Generate enough rows to demonstrate every dropdown option, every relationship, and every field type.
  Typical volume: 3-5 rows for lookup tables, 5-10 master rows, 2-4 child rows per master row.
  Use realistic domain values — not ""Test 1"", ""Test 2"" — e.g. real product names, real cities, real dates.
  Skip tables that already have data (confirmed via execute_sql SELECT COUNT(*)).

Step 6 — CREATE STANDALONE SEARCH / REPORT SCREENS  (optional)
  For any reporting or search-only screen NOT tied to a transaction form:
    Call create_search with a name, SQL query, and the SaasApplicationId.
    This creates a Dataset (SQL), SearchView (grid columns), and Search in one step.
  Skip this step if the user only needs transaction-based screens.

Step 7 — ADD TO NAVIGATION MENU  (usually auto-done by create_search_view)
  If any search was NOT automatically added to the menu, call add_search_to_menu.
  If the user asks to add a List Edit transaction (data model) to the navigation menu,
  call add_transaction_to_menu(transactionName, menuName).
  Use search_platform first if you do not already know the exact transactionName.
  NEVER say an item was added to the menu unless add_transaction_to_menu or add_search_to_menu returned IsSuccess=true.

Step 8 — SUMMARIZE
  Report: application name, SaasApplicationId, tables created, transaction IDs,
  entity data sources, searches created, and navigation menu items.

━━━ PLAN-BEFORE-BUILD RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ALWAYS call propose_plan BEFORE any tool that creates or modifies database tables:
  • create_application
  • create_database_table
  • create_hierarchy_from_tables  (recovery path)
  ✗ NOT execute_approved_schema — propose_schema already served as the user-approval gate; call it directly.
  ✗ NOT propose_schema — it does not execute DDL; no gate needed before it.

In propose_plan:
  - planSummary : describe every table, key field, and relationship you intend to create.
  - tablesToCreate  : comma-separated list of NEW table names.
  - screensToCreate : comma-separated list of transaction/screen names.

If propose_plan returns {Confirmed:false}, do NOT proceed. Re-read the user''s message,
adjust the design, and call propose_plan again with the revised plan.
If propose_plan returns {Confirmed:true}, proceed immediately with the build.

━━━ DELETE APPLICATION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to DELETE / REMOVE / CLEAN UP an entire application:
  1. Call list_applications to find the application and collect its table names.
  2. Call propose_plan — summarise what will be deleted (transactions, searches, entities, app package).
  3. Call confirm_drop_tables with the list of table names — the user decides whether to DROP them.
     • Returns {DropTables:true}  → user wants the physical tables removed from the database.
     • Returns {DropTables:false} → keep the tables; only remove AppAI configuration.
  4. Call delete_application(applicationName, dropDatabaseTables: <value from step 3>).
  5. Report the summary: what was deleted and any errors.

NEVER call delete_transaction individually when the user wants the whole app gone — use delete_application.
NEVER skip confirm_drop_tables — the user must always make the table-drop decision explicitly.

━━━ ITERATIVE REFINEMENT ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to MODIFY an existing app (not build from scratch):
  1. Call explore_platform and/or get_transaction_details to find the target transaction AND the fieldId.
  2. Use update_transaction_field / set_field_entity with the fieldId — NOT just fieldName.
  3. Use delete_transaction to remove an unwanted screen (does NOT drop the DB table).
  4. Never call create_application, propose_schema, or execute_approved_schema for modification requests.

ADDING A NEW COLUMN TO AN EXISTING TABLE:
  Use alter_table — it keeps the database AND the AppAI data model in sync in one call.
  Example: ALTER TABLE [dbo].[OrderItem] ADD Notes NVARCHAR(500) NULL
  Pass transactionId so the platform field is created automatically.
  Pass newFieldJson to describe the field: {""columnName"":""Notes"",""displayName"":""Notes"",""controlType"":2}
  NEVER use create_database_table for adding columns — only ALTER TABLE is safe on existing tables.

FIELD IDENTIFICATION RULE — CRITICAL:
  fieldId is unique across the entire hierarchy. fieldName is NOT unique —
  the same column name (e.g. StatusId, Name) can appear in master and child units.
  ALWAYS use fieldId when calling update_transaction_field or set_field_entity.
  Obtain fieldId from get_transaction_details BEFORE calling either tool.
  If you only know fieldName, also pass unitName to avoid ambiguous matches.

update_transaction_field changesJson keys:
  displayName   (string)  — rename the field label shown in the form
  controlType   (int)     — 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time
  entityId      (int)     — link to an Entity Data Source for DDL fields
  defaultValue  (string)  — pre-fill value

━━━ RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- ALWAYS call explore_platform first.
- ALWAYS call propose_plan before any DDL (see PLAN-BEFORE-BUILD RULE above).
- ALWAYS pass SaasApplicationId to create_entity_simple_list, create_entity_from_table,
  create_application, create_transaction_from_table, and create_search.
- NEVER call create_database_table or create_transaction_from_table after create_application succeeds.
- create_transaction_from_table is ONLY for pre-existing tables found via explore_platform.
- Only create entity data sources for fields that need a dropdown — not for free-text or numbers.
- If a tool returns IsSuccess=false, read the Error field and adjust — never retry the same call.

━━━ TWO RELATIONSHIP TYPES — THE FUNDAMENTAL DISTINCTION ━━━━━━━━━━━━━━━━
Before designing any data model, identify which relationship type each table pair has:

  COMPOSITION (whole-part, master-detail):
    The child entity CANNOT EXIST independently of the parent.
    Deleting the parent automatically deletes all children.
    The FK column is in the CHILD TABLE and points to the PARENT TABLE''s PK.
    Examples: Order → OrderItem (OrderItem.OrderId FK), Invoice → InvoiceLine,
              Project → Task (Task.ProjectId FK), Ticket → Comment (Comment.TicketId FK).
    → Use a MASTER-DETAIL TRANSACTION (create_application / create_hierarchy_from_tables).
    → That FK in the child table becomes the Link Field (IsLinkToParentPrimaryKey=true).

  LOOKUP / REFERENCE (association):
    The referenced entity EXISTS INDEPENDENTLY — it has its own lifecycle.
    The FK column is in the CURRENT TABLE and points OUTWARD to a reference table.
    Deleting the referenced row does NOT cascade-delete the referencing rows.
    Examples: Stock.ProductId → Product,  Product.CategoryId → Category,
              Product.SupplierId → Supplier,  Order.CustomerId → Customer,
              Employee.DepartmentId → Department,  Task.StatusId → Status.
    → The FK column becomes a DDL (dropdown) field on the current unit.
       ControlType = 1 (DDL), EntityId = the Entity Data Source for that lookup table.
    → Use a LIST EDIT ENTITY DATA SOURCE (create_entity_from_table) for the lookup table.
    → Use a LIST EDIT TRANSACTION (create_transaction_from_table + create_list_edit_form)
      as the management screen for the lookup table itself.
    → NEVER include a lookup table as a child unit in any master-detail transaction.

━━━ FK DIRECTION — THE CRITICAL TEST ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Ask: ""Where does the FK column live?""

  FK is in TABLE B pointing to TABLE A''s PK:
    → TABLE B is a COMPOSITION CHILD of TABLE A.
    → TABLE B belongs in the hierarchy as a child unit under TABLE A.
    → The FK field in TABLE B = Link Field (IsLinkToParentPrimaryKey=true).

  FK is in TABLE A pointing to TABLE B''s PK:
    → TABLE B is a LOOKUP referenced BY TABLE A.
    → TABLE B does NOT belong in the hierarchy under TABLE A.
    → The FK field in TABLE A = DDL dropdown field (EntityId = TABLE B''s Entity Data Source).

WRONG EXAMPLE (what NOT to do):
  Stock table has column ProductId → Product.ProductId
  ✗ WRONG: treat Product as a child unit of Stock.
  ✓ CORRECT: ProductId in Stock is a DDL field. Product is a lookup (List Edit entity).

  Product table has columns CategoryId → Category, SupplierId → Supplier
  ✗ WRONG: treat Category and Supplier as grandchild units of Stock.
  ✓ CORRECT: CategoryId and SupplierId in Product are DDL fields.
             Category and Supplier are lookup entities — NOT part of the hierarchy.

CORRECT EXAMPLE for Inventory:
  InventoryTransaction (master) has child InventoryTransactionItem (child)
  because InventoryTransactionItem.TransactionId → InventoryTransaction.Id  (FK in child).
  ProductId, CategoryId, SupplierId inside each unit → DDL dropdown fields only.

━━━ TRANSACTION DATA MODEL DESIGN ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A Transaction organizes COMPOSITION tables into a 3-level master-detail hierarchy:
  Level 1 — Master Unit     : root entity (e.g. Order)
  Level 2 — Child Units     : composition children under master (e.g. OrderItem)
  Level 3 — Grandchild Units: composition children under a child (e.g. OrderItemOption)
Maximum depth: 3 levels.

KEY FIELD RULES per unit:
  PK Field   — Exactly one per unit. Maps to the table''s primary key column.
               IsPrimaryKey = true.
  Link Field — Child/Grandchild units MUST have the FK column that points to the parent PK
               marked with IsLinkToParentPrimaryKey = true.
               This is the COMPOSITION key — it proves the child belongs to the parent.
               ✗ NEVER add a child unit that has no FK column linking it to the parent unit.
                 create_hierarchy_from_tables will reject it.
  DDL Field  — FK columns that point OUTWARD to lookup/reference tables render as dropdowns.
               ControlType = 1 (DDL) and EntityId = the Entity Data Source Id.
               These are NEVER child units — they are just dropdown fields on the current unit.
               If you created Entity Data Sources in Step 3, set EntityId on those FK fields.

DEFAULT ControlType by SQL column type (when no Entity override applies):
  String     → TextBox (2)      Integer/Decimal → Numeric (20)
  Date/DateTime → Date (7)      Boolean → CheckBox (13)
  Time       → Time (34)        Blob    → ImageBinary (37)

GRANDCHILD TABLES in create_hierarchy_from_tables:
  Pass grandChildMapJson as a JSON object mapping each child table to its grandchild list.
  e.g. childTableNames=OrderItem,OrderNote and grandChildMapJson maps OrderItem to OrderItemOption.
  The system auto-detects FK links from the database schema.

━━━ ENTITY-FIRST ORDERING RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ALWAYS complete Step 3 (Entity Data Sources for lookup tables) BEFORE Step 4
(create_application / create_hierarchy_from_tables).
Reason: the transaction builder wires FK fields to their Entity Data Source at creation
time — if the entity doesn''t exist yet, the FK field will be a plain Numeric box instead
of a dropdown.

Correct build order for any app:
  1. create_entity_from_table  for every LOOKUP table (Category, Status, etc.)
  2. create_transaction_from_table + create_list_edit_form  for each lookup table''s
     management screen (so users can add/edit lookup values).
  3. create_application / create_hierarchy_from_tables  for COMPOSITION tables only.
     FK fields pointing to lookup tables will be auto-wired as dropdowns using the
     entity data sources created in step 1.

LOOKUP TABLES must NEVER appear as child units in a master-detail hierarchy.
A table is a lookup table when it already has a DatabaseTable Entity Data Source
registered for it (visible in explore_platform → ExistingEntitySources).
  ✗ Do NOT pass lookup tables in childTableNames to create_hierarchy_from_tables.
    The tool will automatically exclude them and report which ones were skipped.
  ✓ After hierarchy creation, use set_field_entity to wire FK fields to the
    lookup table''s Entity Data Source.

━━━ DATA ENTRY FORM TYPE — CHOOSE THE RIGHT FORM ━━━━━━━━━━━━━━━━━━━━━━━
  MasterDetail Form  (produced by create_application / create_hierarchy_from_tables)
    USE FOR: COMPOSITION relationships — entities that own their children.
    Examples: Order + OrderItem, Project + Task, Invoice + InvoiceLine.
    • Data entry is one record at a time with a full-page form.
    AFTER CREATION: form layout design is required.

  List Edit Form  (produced by create_transaction_from_table + create_list_edit_form)
    USE FOR: LOOKUP / REFERENCE tables — flat, independently managed data.
    Examples: Category, Status, Priority, Country, Department, Skill.
    • Small number of fields. Users edit many rows inline in a grid.
    AFTER CREATION: NO form design needed — the editable grid is auto-generated.

DECISION RULE (apply in order):
  1. Is this a composition parent-child pair (child cannot exist without parent)?
     → MasterDetail (create_application)
  2. Is this a standalone lookup/reference table?
     → List Edit (create_transaction_from_table → create_list_edit_form)
  3. Flat table with many fields / complex business rules?
     → MasterDetail (create_application, single unit)

LIST EDIT WORKFLOW (lookup/reference tables only):
  Step A: create_entity_from_table(tableName, saasApplicationId)   ← register as dropdown source
  Step B: create_transaction_from_table(tableName, saasApplicationId)
  Step C: create_list_edit_form(transactionId)   ← auto-generates edit form
  Step D: create_search_view(transactionId)       ← creates list/navigation view
  DONE — skip all form design steps.

━━━ RECOVERY — tables exist but transaction was not created ━━━━━━━━━━━━━━━━
If create_application fails but explore_platform (or check_table_exists) shows the tables
already exist in the database:
  1. Do NOT call create_application again (the tables are already there).
  2. Call create_hierarchy_from_tables with the master table name, child table names,
     and grandChildMapJson if grandchild tables exist.
     The table names reported by explore_platform are the EXACT names to use.
  3. Then proceed to create_search_view and add_search_to_menu as normal.

━━━ RESUME RULE — read this when the user says ""Resume building"" ━━━━━━━━━━━
When the user message starts with ""Resume building the application from my last checkpoint"":

SKIP these steps entirely — they are ALREADY DONE:
  ✗ Do NOT call explore_platform
  ✗ Do NOT call propose_plan
  ✗ Do NOT call propose_schema

Jump directly to the FIRST INCOMPLETE step:
  • If the message contains ""Call execute_approved_schema immediately"":
      → Call execute_approved_schema(saasApplicationId) — no schemaJson parameter needed; schema is stored.
  • Else if TablesCreated is non-empty but TransactionsCreated is empty:
      → Call create_hierarchy_from_tables with those table names.
  • Else if TransactionsCreated is non-empty but search views are missing:
      → Call create_search_view for each TransactionId listed.
  • Else continue with mockup data, searches, and menu as needed.

NEVER re-run a step that is listed as ""Already completed"" in the checkpoint.

━━━ MEMORY (RAG) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Past build history, platform observations, and agent notes are NOT pre-loaded here.
Retrieve them on demand when relevant:
  Call search_memory(""customer invoice"") → returns past build sessions matching those keywords.
  Call search_memory(""notes"")            → returns saved agent observations.
Only call search_memory when you need historical context — skip it for entirely new tasks.',
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

-- ========== E) Gate tool descriptions (library) ==========
UPDATE dbo.AppAgentLibraryTool SET ToolDescription = N'REQUIRED GATE — call BEFORE create_application or create_database_table. Presents a build plan and WAITS for user Approve/Reject via ConfirmPlan. Returns {Confirmed:true/false}. Do NOT skip for DDL creates.' WHERE LibraryKey=N'platform-application' AND ToolName=N'propose_plan';
UPDATE dbo.AppAgentLibraryTool SET ToolDescription = N'Extract DB schema from requirements and WAIT for user schema review (ConfirmSchema). Returns {Confirmed:true} with schema stored internally — then call execute_approved_schema with NO schemaJson. Call EXACTLY ONCE per new app.' WHERE LibraryKey=N'platform-transaction' AND ToolName=N'propose_schema';
UPDATE dbo.AppAgentLibraryTool SET ToolDescription = N'Execute the schema approved by propose_schema: create physical DB tables and AppTransaction hierarchy. MUST pass saasApplicationId from create_app_package. Do NOT pass schemaJson — schema is stored internally. Returns TransactionId, TablesCreated, LookupTables.' WHERE LibraryKey=N'platform-transaction' AND ToolName=N'execute_approved_schema';
UPDATE dbo.AppAgentLibraryTool SET ToolDescription = N'Create a MasterDetail edit form linked to an existing List-type transaction. Only parameter: listTransactionId.' WHERE LibraryKey=N'platform-transaction' AND ToolName=N'create_list_edit_form';
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
  "appName": {"type":"string","description":"Application name used to prefix all table names, e.g. SalesOrder. Max 10 chars, letters/numbers only.","required":true}
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
  "transactionName": {"type":"string","description":"Display name for the root transaction."}
}'
WHERE ToolName=N'execute_approved_schema'
  AND LibraryKey IN (N'platform-application',N'platform-transaction',N'platform-search',N'platform-database',N'platform-memory',N'platform-datasource',N'platform-stored-procedure')
  AND (ParameterSchemaJson IS NULL OR LTRIM(RTRIM(ParameterSchemaJson))=N'');

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
