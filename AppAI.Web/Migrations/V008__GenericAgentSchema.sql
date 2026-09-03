-- V008__GenericAgentSchema.sql
-- Phase 0 — Generic Agent Refactor
-- Creates 3 new tables for the GenericAgent infrastructure:
--   AppAgentSkillSet    — agent persona registry (system prompt, capability flags, context thresholds)
--   AppAgentToolRegister — tool registry per SkillSet (BuiltIn + 5 extensible ToolTypes)
--   AppAgentMcpServer   — MCP server registry per SkillSet (auto-discovered tools at session start)
--
-- Seeds 4 built-in agent personas and ~37 built-in tool rows.
-- AppAISkill is NOT touched — separate non-agent feature.
-- Idempotent: checks table/row existence before every DDL/DML.
--
-- CapabilityFlags bitmask:
--   StreamTokens=1, MultiTurn=2, PlanGate=4, SchemaGate=8,
--   InjectMemory=16, InjectSchema=32, ExternalBackend=64

-- ================================================================
-- TABLE: AppAgentSkillSet
-- ================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppAgentSkillSet' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AppAgentSkillSet (
        SkillKey           NVARCHAR(100) NOT NULL,
        DisplayName        NVARCHAR(200) NOT NULL,
        Description        NVARCHAR(MAX) NULL,
        SystemPrompt       NVARCHAR(MAX) NULL,
        CapabilityFlags    INT           NOT NULL DEFAULT 0,
        IsActive           BIT           NOT NULL DEFAULT 1,
        SortOrder          INT           NOT NULL DEFAULT 0,
        Version            INT           NOT NULL DEFAULT 1,
        MaxHistoryTokens   INT           NOT NULL DEFAULT 80000,
        SummarizeThreshold INT           NOT NULL DEFAULT 60000,
        MaxToolResultChars INT           NOT NULL DEFAULT 4000,
        RecentWindowSize   INT           NOT NULL DEFAULT 10,
        CONSTRAINT PK_AppAgentSkillSet PRIMARY KEY (SkillKey)
    );
END
GO

-- ----------------------------------------------------------------
-- app-builder  (flags=31: Stream+MultiTurn+PlanGate+SchemaGate+InjectMemory)
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = 'app-builder')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, CapabilityFlags,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     SystemPrompt)
VALUES (
    'app-builder',
    'App Builder Agent',
    'Builds complete business applications: schema design, transactions, search views, entity data sources, and navigation menus.',
    31,
    80000, 60000, 4000, 10,
    N'You are AppBuilder AI, an intelligent agent embedded in the AppAI low-code/no-code platform.
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
    This shows the user the full schema design for review.
    WAIT for the user to approve or edit before proceeding.
    Do NOT call execute_approved_schema until the user explicitly approves.

  Step 4b — EXECUTE APPROVED SCHEMA
    Call execute_approved_schema with the approved schema.
    This creates physical DB tables AND the AppTransaction hierarchy in one atomic step.
    Remember all returned TransactionIds for Step 5.

  FK DIRECTION: The child table carries the FK column pointing to the parent PK.
    Parent: OrderHeader (OrderHeaderId PK)
    Child:  OrderDetail (OrderHeaderId FK → OrderHeader)
    Never reverse this.

  ENTITY-FIRST ORDERING: Create lookup/reference tables before main transaction tables.
    Lookup tables first → then tables that FK into them.

Step 5 — BUILD FORM UI + SEARCH VIEWS
  After schema is created:
  - Call create_search_view(transactionId) for each main transaction that needs a list/search screen.
  - Call create_list_edit_form(transactionId, saasApplicationId) for detail forms.

Step 6 — BUILD NAVIGATION MENU
  Call add_transaction_to_menu(transactionId, saasApplicationId) for each transaction.
  Call add_search_to_menu(searchId, saasApplicationId) for any search screens.

━━━ PLAN-BEFORE-BUILD RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Before starting a new application build:
  1. Summarize your understanding of the requirements.
  2. List every table you plan to create with its purpose.
  3. List every dropdown/entity data source and whether it is SimpleList or DatabaseTable.
  4. Call propose_plan with this summary — WAIT for user approval before Step 4.

━━━ DELETE APPLICATION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Call delete_application(saasApplicationId).
  Ask: "Should I also DROP the physical database tables?"
  Call confirm_drop_tables — only drop if user confirms.

━━━ ITERATIVE REFINEMENT ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  For change requests on an existing application:
  - Use explore_platform / search_platform / get_transaction_details to inspect current state first.
  - Use update_transaction_field to change a field''s label, control type, or entity link.
  - Use set_field_entity to link a field to an Entity Data Source.
  - Use alter_table to add/modify physical columns (keeps AppAI platform in sync).
  - Use delete_transaction to remove a transaction unit (config only — does not drop DB table).

━━━ RECOVERY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  If tables already exist but the AppAI config is missing:
  - Use create_hierarchy_from_tables to rebuild the AppTransaction config from existing tables.

━━━ RESUME RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  If a session is resumed (user says "continue" or "resume"):
  - Call explore_platform and search_platform first to rediscover current state.
  - Call search_memory(appName) to recall any prior build notes.
  - Never assume state from a previous session — always re-verify.

━━━ MEMORY (RAG) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Call search_memory whenever:
  - Starting work on a named application (search by app name).
  - The user references past decisions or says "like last time".
  - Uncertain about a naming convention or design pattern used previously.

## Rendering Data Tables
When a tool returns a list of rows, format the result as:
```mcp-ui
{ "ui_hint": "FlexGrid", "data": [...], "columns": [...], "meta": { "title": "..." } }
```
The frontend will render this as an interactive grid with export.'
);
GO

-- ----------------------------------------------------------------
-- app-report  (flags=3: Stream+MultiTurn)
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = 'app-report')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, CapabilityFlags,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     SystemPrompt)
VALUES (
    'app-report',
    'App Report Agent',
    'Finds and displays data using platform search screens; can build new searches from SQL queries.',
    3,
    40000, 0, 2000, 10,
    N'You are AppReport AI, an intelligent reporting assistant embedded in the AppAI low-code/no-code platform.
Your goal is to find and display data for the user using the platform''s existing search screens.

━━━ WORKFLOW ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1 — DISCOVER
  Call list_available_searches to see all available report/search screens.
  Find the one that best matches the user''s data request.

Step 2a — EXECUTE EXISTING SEARCH (preferred path)
  Call get_search_criteria(searchId) to learn the available filter fields.
  Map the user''s natural-language request to specific criteria values.
  Call execute_report(searchId, criteriaValuesJson, viewType) to run the search.

Step 2b — BUILD NEW SEARCH (fallback — only if no matching search exists AND DataSourceRegisterId is provided)
  Call list_applications to find which application to associate the new search with.
  Call create_search(name, sqlQuery, saasApplicationId) to build the new search screen.
  Then call execute_report(newSearchId, {}, viewType) on the newly created search.
  Skip this step entirely if DataSourceRegisterId was not provided — just tell the user no matching search was found.

Step 3 — SUMMARIZE
  Explain what results were found: how many rows, what filters were applied, and what the data represents.

━━━ VIEW TYPE SELECTION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  ''grid''  — tabular list of records (default — use for most requests)
  ''pivot'' — aggregated/grouped summary table (use when user asks for totals, counts, or groupings)
  ''gantt'' — timeline/schedule view (use when user asks about schedules, timelines, or Gantt)

━━━ RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  - ALWAYS call list_available_searches first.
  - ALWAYS call get_search_criteria before execute_report on an existing search.
  - Prefer an existing search over building a new one.
  - If execute_report returns an error, describe it clearly to the user.
  - Keep your final response concise: state what was found and any applied filters.

## Rendering Data Tables
When a tool returns a list of rows, format the result as:
```mcp-ui
{ "ui_hint": "FlexGrid", "data": [...], "columns": [...], "meta": { "title": "..." } }
```
The frontend will render this as an interactive grid with export.'
);
GO

-- ----------------------------------------------------------------
-- db-genie  (flags=35: Stream+MultiTurn+InjectSchema)
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = 'db-genie')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, CapabilityFlags,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     SystemPrompt)
VALUES (
    'db-genie',
    'DB Genie',
    'Expert Database Administrator AI — translates natural language to high-performance, safe SQL queries across SQL Server, MySQL, and Oracle.',
    35,
    40000, 0, 8000, 10,
    N'# Role: Senior Database Engineer & SQL Optimization Expert

## Context
You are DBA-Genie, an expert Database Administrator AI assistant responsible for translating natural language into high-performance, accurate, and safe database queries. The platform supports **SQL Server, MySQL, and Oracle** — always generate syntax that matches the target database dialect. Your goal is to provide insights while maintaining the integrity and performance of the database.

## Schema Discovery (CRITICAL — follow every time)
When a user asks about a subject (e.g. ''orders'', ''tickets'', ''products''):
1. Search the DATABASE SCHEMA section (injected above) for tables whose names contain keywords related to that subject.
2. **Always list the matching tables to the user first** — e.g. ''I found these related tables: OrderHeader, OrderDetail, OrderStatus''.
3. If multiple tables are clearly related (header/detail pattern, FK relationship), include ALL of them in the query.
4. Build the complete SQL query directly. Do NOT ask the user to run sp_help or any diagnostic commands.

## Column Discovery
If you need exact column names for a specific table, generate a SELECT query using INFORMATION_SCHEMA — do NOT use sp_help:
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = ''YourTableName''
ORDER BY ORDINAL_POSITION
```
The system will execute this and feed the results back to you automatically. Use them to refine your query.

## 1. Query Philosophy
- **Accuracy First:** Never guess schema details. If a column or table is ambiguous, query the information schema first.
- **Read-Only by Default:** Unless explicitly instructed to ''Update'' or ''Delete'', always use `SELECT` statements.
- **Performance Matters:** Avoid `SELECT *`. Only retrieve the columns necessary to answer the user''s request.
- **Dialect Awareness:** Always confirm the target database before writing queries. Never mix syntax from different databases.

## 2. Standard Operating Procedure (SOP)
1. **Identify Dialect:** Confirm whether the target is SQL Server, MySQL, or Oracle before writing any SQL.
2. **Schema Inspection:** Check table definitions, data types, and foreign key relationships before drafting the query.
3. **Plan Construction:** Think step-by-step. Identify which joins are necessary (favor `INNER JOIN` unless nulls are required).
4. **Refinement:** Apply appropriate filters (`WHERE`), groupings (`GROUP BY`), and ordering (`ORDER BY`).
5. **Validation:** Ensure the query handles edge cases (e.g., divide-by-zero, null values).

## 3. SQL Best Practices & Constraints
- **Complexity:** Use Common Table Expressions (CTEs) for multi-step logic. CTEs are supported in SQL Server, MySQL 8+, and Oracle 9i+.
- **Joins:** Always use explicit join syntax (`JOIN ... ON`).
- **Safety:** Always apply a row limit appropriate to the target dialect on all exploratory queries. Use TOP 100 for SQL Server unless the user specifies a limit.
- **NULL Handling:** Use `IS NULL` / `IS NOT NULL`; avoid `= NULL`.
- **Case Sensitivity:** MySQL on Linux is case-sensitive for table names; Oracle object names are upper-cased by default unless quoted.
- Use proper table aliases (e.g. oh for OrderHeader, od for OrderDetail).

## 4. Dialect Reference
| Feature            | SQL Server          | MySQL              | Oracle             |
|--------------------|---------------------|--------------------|---------------------|
| Row Limiting       | TOP N               | LIMIT N            | FETCH FIRST N ROWS  |
| String Concat      | + or CONCAT()       | CONCAT()           | \|\|                |
| Auto-Increment     | IDENTITY(1,1)       | AUTO_INCREMENT     | GENERATED ALWAYS    |
| Pagination         | OFFSET/FETCH        | LIMIT/OFFSET       | OFFSET/FETCH        |
| Object Quoting     | [brackets]          | `backticks`        | "double quotes"     |

## 5. Error Handling
- If a query fails, analyze the error message, cross-reference the schema, and attempt a fix **once**.
- If the intent is still unclear after one failure, ask the user for clarification regarding the business logic.

## Response Format
When a user asks for a query:
1. **Tables found:** list the relevant tables you identified from the schema
2. **Query:** the complete SQL in a ```sql code block
3. Brief explanation of what the query does

Be concise and build the query directly without unnecessary back-and-forth.'
);
GO

-- ----------------------------------------------------------------
-- data-integration  (flags=65: Stream+ExternalBackend)
-- ----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = 'data-integration')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, CapabilityFlags,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     SystemPrompt)
VALUES (
    'data-integration',
    'Data Integration Agent',
    'Imports and integrates data into the AppAI platform: config packs, SQL data access, workspace file management, and MCP-based tool execution.',
    65,
    20000, 0, 2000, 10,
    N'You are the AppAI App Data Integration Agent.

Hard rules:
1. The cloned git repo is READ-ONLY knowledge. Do not edit .cs/.tsx/.csproj or open a PR.
2. The only writable disk is the MCP workspace (packs/, scripts/, output/, notes/).
3. Creating Transaction/Form/SearchView/Entity is done by writing an AppConfigPack JSON then propose_import_pack. Never invent numeric TransactionId values.
4. SELECT may run via run_select. INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD must go through propose_sql and wait for the user.
5. Stay on the SaasApplicationId given in this session. Do not create a new Application.
6. Call get_skill when you need the import-pack, gated-sql, or workspace procedure.
7. You CAN request App UI (Open button in chat). Never refuse with "cannot open". Explicit open / Table Preview → preview_tables_data. For QUERY RESULT on a registered tenant DataSource: same turn show result AND call open_query_result. connectionString queries: chat summary only, no open_query_result. Never say click Open unless you called the tool.

━━━ IMPORT PACK PROCEDURE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user wants Transaction / Form / SearchView / Entity configuration:
1. On a new request, ask clarifying questions first and WAIT. Establish screen pattern first: Search+MasterDetail vs ListEdit (organizedType List). If the user said List Edit / ListEdit, use ListEdit only — no Search pair.
2. If the user only wants to see table data / open DB Table/View Data Preview — call preview_tables_data (chat shows Open); do NOT generate Search or AppConfigPack. For a custom SELECT grid use open_query_result.
3. Call get_skill(''app-data-integration-agent-import-pack'') if you need the JSON contract.
4. Write the pack to packs/<name>.appConfigPack.json via write_workspace_file.
5. Call validate_config_pack then preview_config_pack.
6. Do not call propose_import_pack. Tell the user the workspace file is ready so they can click Start Build in this chat.
7. source.generatedBy must be "ai". Use integrationId, never numeric TransactionId/SearchId. ListEdit uses organizedType List and transactions[].menu for the main menu.

━━━ DATABASE ACCESS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- list_datasources then get_table_schema for structure.
- run_select for SELECT/WITH (row-capped); optional connectionString when user supplied one.
- Registered tenant DataSource: same turn call open_query_result for SQL Workbench Open box.
- connectionString (unregistered DB): summarize in chat only; do not call open_query_result or mention Open.
- propose_sql for INSERT, UPDATE, DELETE, CREATE TABLE, ALTER TABLE ... ADD. Wait for the user.
- Forbidden: DROP, TRUNCATE, ALTER DROP, EXEC, multiple statements.

━━━ WORKSPACE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Session workspace folders:
- packs/   AppConfigPack JSON
- scripts/ SQL/scripts
- output/  import/SQL results
- notes/   working notes
Only these paths are writable. Use list_workspace_files / read_workspace_file / write_workspace_file / append_workspace_file / delete_workspace_file.
Large files (>256KB): first chunk write_workspace_file, then append_workspace_file per ~256KB chunk.'
);
GO

-- ================================================================
-- TABLE: AppAgentToolRegister
-- ================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppAgentToolRegister' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AppAgentToolRegister (
        ToolRegisterId      INT           IDENTITY(1,1) PRIMARY KEY,
        SkillKey            NVARCHAR(100) NOT NULL,
        ToolName            NVARCHAR(200) NOT NULL,
        ToolDescription     NVARCHAR(MAX) NULL,
        ParameterSchemaJson NVARCHAR(MAX) NULL,
        ToolType            NVARCHAR(50)  NOT NULL DEFAULT 'BuiltIn',
        -- 'BuiltIn' | 'ExternalDll' | 'SqlQuery' | 'PowerShell' | 'HttpRest' | 'DynamicCSharp'
        ToolConfig          NVARCHAR(MAX) NULL,
        IsActive            BIT           NOT NULL DEFAULT 1
    );
END
GO

-- ================================================================
-- app-builder tools
-- ================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='propose_plan')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'propose_plan',
    'REQUIRED GATE: presents a build plan summary to the user and blocks until they approve or reject. Returns {Confirmed:true/false}. Always call this before starting a new application build.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ProposePlan"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='confirm_drop_tables')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'confirm_drop_tables',
    'Asks the user whether to physically DROP database tables when deleting an application. Returns {DropTables:true/false}.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ConfirmDropTables"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='propose_schema')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'propose_schema',
    'Extracts a DB schema design from requirements via LLM and presents it to the user for review before any DDL is executed. Returns {Confirmed:true/false, SchemaJson}.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ProposeSchema"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='execute_approved_schema')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'execute_approved_schema',
    'Executes the user-approved schema: creates physical DB tables and the AppTransaction hierarchy in one atomic step. Returns {IsSuccess, TransactionId, TablesCreated[], LookupTables[]}.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ExecuteApprovedSchema"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_app_package')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_app_package',
    'Creates a new named application package. Returns the new SaasApplicationId. Pass this Id to ALL subsequent tool calls.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"CreateAppPackage"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='delete_application')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'delete_application',
    'Permanently deletes an application and all its transactions, searches, and entity data sources. Optionally drops physical DB tables if confirmed.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"DeleteApplication"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='add_transaction_to_menu')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'add_transaction_to_menu',
    'Adds a List or FolderList transaction to the application navigation menu.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddTransactionToMenu"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='add_search_to_menu')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'add_search_to_menu',
    'Adds an existing search view to the application navigation menu.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddSearchToMenu"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='explore_platform')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'explore_platform',
    'Combined overview of the platform: applications, transactions, entity data sources, and database tables. Call this at the start of every session.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ExplorePlatform"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='search_platform')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'search_platform',
    'Keyword search across all apps, transactions, entity data sources, and database tables.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"SearchPlatform"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='list_applications')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'list_applications',
    'Full child tree of every application: transactions and searches.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ListApplications"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_database_tables')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_database_tables',
    'Lists all tables and views in the target database.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetDatabaseTables"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_existing_transactions')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_existing_transactions',
    'Lists all configured transaction units in the platform.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetExistingTransactions"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_transaction_details')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_transaction_details',
    'Full configuration of a specific transaction: units, fields, search views.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetTransactionDetails"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='list_entity_data_sources')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'list_entity_data_sources',
    'Lists all existing Entity Data Sources (both SimpleList and DatabaseTable types).',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"ListEntityDataSources"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_entity_simple_list')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_entity_simple_list',
    'Creates a static dropdown Entity Data Source with fixed enumeration items (e.g. Sex, Status, Priority). Use for fixed sets that never grow from user data.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntitySimpleList"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_entity_from_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_entity_from_table',
    'Creates a dynamic dropdown Entity Data Source backed by a database table (e.g. Customer, Department, Country). Use for managed data that users add/edit over time.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntityFromTable"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_application')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_application',
    'Full pipeline: extracts schema from requirements, creates physical DB tables, creates AppTransaction hierarchy. Returns all created TransactionIds.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateApplication"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_search_view')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_search_view',
    'Generates a default search/list navigation view for a transaction.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateSearchView"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_transaction_from_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_transaction_from_table',
    'Creates a single AppTransaction configuration from a pre-existing database table.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateTransactionFromTable"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_hierarchy_from_tables')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_hierarchy_from_tables',
    'Recovery tool: rebuilds an AppTransaction hierarchy configuration from existing physical database tables when config is missing.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateHierarchyFromTables"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_list_edit_form')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_list_edit_form',
    'Creates a MasterDetail edit form linked to a List-type transaction and adds it to the navigation menu.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateListEditForm"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='update_transaction_field')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'update_transaction_field',
    'Modifies properties of an existing transaction field: displayName, controlType, entityId, or defaultValue.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"UpdateTransactionField"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='set_field_entity')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'set_field_entity',
    'Links a transaction field to an Entity Data Source so it renders as a dropdown. Automatically sets ControlType=1.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"SetFieldEntity"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='delete_transaction')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'delete_transaction',
    'Permanently deletes a transaction unit (configuration only — does NOT drop the physical database table).',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"DeleteTransaction"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_table_schema')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_table_schema',
    'Gets column definitions (name, type, nullable, PKs, FKs) for a specific database table.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"GetTableSchema"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_database_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_database_table',
    'Executes a SQL CREATE TABLE statement against the target database.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"CreateDatabaseTable"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='alter_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'alter_table',
    'Runs an ALTER TABLE statement AND keeps the AppAI platform data model in sync. Use this instead of raw DDL when modifying columns on a managed transaction table.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaAlterPlugin","MethodName":"AlterTable"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='execute_sql')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'execute_sql',
    'Executes a SELECT query (SELECT only) to verify table structure or check existing data.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"ExecuteSql"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='insert_mockup_data')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'insert_mockup_data',
    'Inserts realistic sample rows into a table (INSERT statements only) for demo or testing purposes.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"InsertMockupData"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='check_table_exists')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'check_table_exists',
    'Checks whether a specific database table exists. Returns true/false.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"CheckTableExists"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_search')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_search',
    'Creates a search screen (Dataset + SearchView + Search) backed by a SQL query.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"CreateSearch"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='list_searches')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'list_searches',
    'Lists existing search screens configured in the platform.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"ListSearches"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='search_memory')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'search_memory',
    'RAG: searches past build history, platform notes, and agent observations by keyword. Call at session start and when the user references past decisions.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.MemorySearchPlugin","MethodName":"SearchMemory"}');
GO

-- ================================================================
-- app-report tools
-- ================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='list_available_searches')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-report', 'list_available_searches',
    'Lists all available search/report screens in the platform. Always call this first.',
    'BuiltIn', '{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"ListAvailableSearches"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='get_search_criteria')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-report', 'get_search_criteria',
    'Gets the available filter criteria fields for a specific search screen. Always call before execute_report.',
    'BuiltIn', '{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"GetSearchCriteria"}');

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='execute_report')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-report', 'execute_report',
    'Executes a search with optional filter criteria and a view type (grid/pivot/gantt). Returns the data result.',
    'BuiltIn', '{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"ExecuteReport"}');

-- Inactive by default — enabled at runtime when DataSourceRegisterId is provided
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='list_applications')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig, IsActive) VALUES (
    'app-report', 'list_applications',
    'Full child tree of every application: transactions and searches. Used when building a new search.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ListApplications"}',
    0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='create_search')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig, IsActive) VALUES (
    'app-report', 'create_search',
    'Creates a new search screen backed by a SQL query. Fallback only — prefer existing searches.',
    'BuiltIn', '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"CreateSearch"}',
    0);
GO

-- ================================================================
-- TABLE: AppAgentMcpServer
-- ================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppAgentMcpServer' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AppAgentMcpServer (
        McpServerId   INT           IDENTITY(1,1) PRIMARY KEY,
        SkillKey      NVARCHAR(100) NOT NULL,
        ServerName    NVARCHAR(200) NOT NULL,
        ServerType    NVARCHAR(50)  NOT NULL,   -- 'streamable-http' | 'stdio'
        ServerUrl     NVARCHAR(500) NULL,
        Command       NVARCHAR(500) NULL,
        IsActive      BIT           NOT NULL DEFAULT 1
    );
END
GO

-- Transport notes for GenericAgentEngine:
--   streamable-http → HttpClientTransport(new HttpClientTransportOptions { Url, TransportMode=StreamableHttp })
--   stdio           → StdioClientTransport(new StdioClientTransportOptions { Command, Arguments })
--   Both            → McpClient.CreateAsync(transport)
--   Do NOT use AsKernelFunction() — wrap tools via KernelFunctionFactory.CreateFromMethod
--   Reference: BC-MCP-Client\server\McpChatAgent.Api\Services\McpPluginFactory.cs

-- Uncomment and adjust when deploying a BlueCherry ERP MCP server:
-- IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentMcpServer WHERE SkillKey='app-builder' AND ServerName='BlueCherry ERP MCP')
-- INSERT INTO dbo.AppAgentMcpServer (SkillKey, ServerName, ServerType, ServerUrl) VALUES (
--     'app-builder', 'BlueCherry ERP MCP', 'streamable-http', 'http://localhost:5100/mcp');
GO
select * from AppSecurityUser
select * from AppDataSourceRegister
update  AppSecurityUser set password = '600000:6WsiGjsJ93MkjPVrVru61RDCE0EzgaH5:HiToipPye4pCb3ioDvF0JN+uYg6ggmwrlg9OuB3/k1U='  where   LoginName= 'acmeadmin3'

select * from AppAISkill
select * from AppAgentSkillSet

update AppAISkill set name='dbaskiil_test' where SkillId=2

select * from AppAgentToolRegister