-- ============================================================
-- AppAgentToolRegister_Create.sql
-- Phase 0 — Generic Agent Refactor
-- Creates AppAgentToolRegister table and seeds ~31 built-in tool rows.
-- ToolType='BuiltIn' rows reference C# plugin classes in APP.BL.
-- Run once per tenant DB. Idempotent (checks existence before INSERT).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppAgentToolRegister' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AppAgentToolRegister (
        ToolRegisterId      INT           IDENTITY(1,1) PRIMARY KEY,
        SkillKey            NVARCHAR(100) NOT NULL,
        ToolName            NVARCHAR(200) NOT NULL,     -- LLM-facing function name
        ToolDescription     NVARCHAR(MAX) NULL,         -- LLM-facing description
        ParameterSchemaJson NVARCHAR(MAX) NULL,         -- JSON Schema for LLM parameters
        ToolType            NVARCHAR(50)  NOT NULL DEFAULT 'BuiltIn',
        -- 'BuiltIn' | 'ExternalDll' | 'SqlQuery' | 'PowerShell' | 'HttpRest' | 'DynamicCSharp'
        ToolConfig          NVARCHAR(MAX) NULL,         -- JSON; shape varies by ToolType
        IsActive            BIT           NOT NULL DEFAULT 1
    );
END
GO

-- ----------------------------------------------------------------
-- Helper: only insert if the row does not already exist
-- ----------------------------------------------------------------

-- ================================================================
-- app-builder tools  (migrated from AppBuilderAgent plugin classes)
-- ================================================================

-- PlanConfirmPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='propose_plan')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'propose_plan',
    'REQUIRED GATE: presents a build plan summary to the user and blocks until they approve or reject. Returns {Confirmed:true/false}. Always call this before starting a new application build.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ProposePlan"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='confirm_drop_tables')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'confirm_drop_tables',
    'Asks the user whether to physically DROP database tables when deleting an application. Returns {DropTables:true/false}.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ConfirmDropTables"}'
);

-- SchemaDesignerPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='propose_schema')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'propose_schema',
    'Extracts a DB schema design from requirements via LLM and presents it to the user for review and editing before any DDL is executed. Returns {Confirmed:true/false, SchemaJson}.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ProposeSchema"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='execute_approved_schema')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'execute_approved_schema',
    'Executes the user-approved schema: creates physical DB tables and the AppTransaction hierarchy in one atomic step. Returns {IsSuccess, TransactionId, TablesCreated[], LookupTables[]}.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ExecuteApprovedSchema"}'
);

-- ApplicationManagerPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_app_package')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_app_package',
    'Creates a new named application package. Returns the new SaasApplicationId. Pass this Id to ALL subsequent tool calls.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"CreateAppPackage"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='delete_application')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'delete_application',
    'Permanently deletes an application and all its transactions, searches, and entity data sources. Optionally drops physical DB tables if confirmed.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"DeleteApplication"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='add_transaction_to_menu')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'add_transaction_to_menu',
    'Adds a List or FolderList transaction to the application navigation menu.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddTransactionToMenu"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='add_search_to_menu')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'add_search_to_menu',
    'Adds an existing search view to the application navigation menu.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddSearchToMenu"}'
);

-- PlatformExplorerPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='explore_platform')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'explore_platform',
    'Combined overview of the platform: applications, transactions, entity data sources, and database tables. Call this at the start of every session.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ExplorePlatform"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='search_platform')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'search_platform',
    'Keyword search across all apps, transactions, entity data sources, and database tables.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"SearchPlatform"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='list_applications')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'list_applications',
    'Full child tree of every application: transactions and searches.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ListApplications"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_database_tables')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_database_tables',
    'Lists all tables and views in the target database.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetDatabaseTables"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_existing_transactions')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_existing_transactions',
    'Lists all configured transaction units in the platform.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetExistingTransactions"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_transaction_details')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_transaction_details',
    'Full configuration of a specific transaction: units, fields, search views.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetTransactionDetails"}'
);

-- EntityDataSourcePlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='list_entity_data_sources')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'list_entity_data_sources',
    'Lists all existing Entity Data Sources (both SimpleList and DatabaseTable types).',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"ListEntityDataSources"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_entity_simple_list')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_entity_simple_list',
    'Creates a static dropdown Entity Data Source with fixed enumeration items (e.g. Sex, Status, Priority). Use for fixed sets that never grow from user data.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntitySimpleList"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_entity_from_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_entity_from_table',
    'Creates a dynamic dropdown Entity Data Source backed by a database table (e.g. Customer, Department, Country). Use for managed data that users add/edit over time.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntityFromTable"}'
);

-- TransactionBuilderPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_application')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_application',
    'Full pipeline: extracts schema from requirements, creates physical DB tables, creates AppTransaction hierarchy. Returns all created TransactionIds.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateApplication"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_search_view')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_search_view',
    'Generates a default search/list navigation view for a transaction.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateSearchView"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_transaction_from_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_transaction_from_table',
    'Creates a single AppTransaction configuration from a pre-existing database table.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateTransactionFromTable"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_hierarchy_from_tables')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_hierarchy_from_tables',
    'Recovery tool: rebuilds an AppTransaction hierarchy configuration from existing physical database tables when config is missing.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateHierarchyFromTables"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_list_edit_form')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_list_edit_form',
    'Creates a MasterDetail edit form linked to a List-type transaction and adds it to the navigation menu.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateListEditForm"}'
);

-- TransactionModifierPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='update_transaction_field')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'update_transaction_field',
    'Modifies properties of an existing transaction field: displayName, controlType, entityId, or defaultValue.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"UpdateTransactionField"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='set_field_entity')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'set_field_entity',
    'Links a transaction field to an Entity Data Source so it renders as a dropdown. Automatically sets ControlType=1.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"SetFieldEntity"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='delete_transaction')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'delete_transaction',
    'Permanently deletes a transaction unit (configuration only — does NOT drop the physical database table).',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"DeleteTransaction"}'
);

-- SchemaBuilderPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='get_table_schema')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'get_table_schema',
    'Gets column definitions (name, type, nullable, PKs, FKs) for a specific database table.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"GetTableSchema"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_database_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_database_table',
    'Executes a SQL CREATE TABLE statement against the target database.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"CreateDatabaseTable"}'
);

-- SchemaAlterPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='alter_table')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'alter_table',
    'Runs an ALTER TABLE statement AND keeps the AppAI platform data model in sync. Use this instead of raw DDL when modifying columns on a managed transaction table.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaAlterPlugin","MethodName":"AlterTable"}'
);

-- DataQueryPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='execute_sql')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'execute_sql',
    'Executes a SELECT query (SELECT only) to verify table structure or check existing data.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"ExecuteSql"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='insert_mockup_data')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'insert_mockup_data',
    'Inserts realistic sample rows into a table (INSERT statements only) for demo or testing purposes.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"InsertMockupData"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='check_table_exists')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'check_table_exists',
    'Checks whether a specific database table exists. Returns true/false.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"CheckTableExists"}'
);

-- SearchBuilderPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='create_search')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'create_search',
    'Creates a search screen (Dataset + SearchView + Search) backed by a SQL query.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"CreateSearch"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='list_searches')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'list_searches',
    'Lists existing search screens configured in the platform.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"ListSearches"}'
);

-- MemorySearchPlugin
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-builder' AND ToolName='search_memory')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-builder', 'search_memory',
    'RAG: searches past build history, platform notes, and agent observations by keyword. Call at session start and when the user references past decisions.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.MemorySearchPlugin","MethodName":"SearchMemory"}'
);

-- ================================================================
-- app-report tools  (migrated from AppReportAgent plugin classes)
-- ================================================================

-- ReportSearchPlugin — always available
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='list_available_searches')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-report', 'list_available_searches',
    'Lists all available search/report screens in the platform. Always call this first.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"ListAvailableSearches"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='get_search_criteria')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-report', 'get_search_criteria',
    'Gets the available filter criteria fields for a specific search screen. Always call before execute_report.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"GetSearchCriteria"}'
);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='execute_report')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig) VALUES (
    'app-report', 'execute_report',
    'Executes a search with optional filter criteria and a view type (grid/pivot/gantt). Returns the data result.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"ExecuteReport"}'
);

-- PlatformExplorerPlugin — available when DataSourceRegisterId is provided (GenericAgentEngine handles conditional loading)
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='list_applications')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig, IsActive) VALUES (
    'app-report', 'list_applications',
    'Full child tree of every application: transactions and searches. Used when building a new search.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ListApplications"}',
    0  -- inactive by default; enabled at runtime when DataSourceRegisterId is provided
);

-- SearchBuilderPlugin — available when DataSourceRegisterId is provided
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey='app-report' AND ToolName='create_search')
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, ToolDescription, ToolType, ToolConfig, IsActive) VALUES (
    'app-report', 'create_search',
    'Creates a new search screen backed by a SQL query. Fallback only — prefer existing searches.',
    'BuiltIn',
    '{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"CreateSearch"}',
    0  -- inactive by default; enabled at runtime when DataSourceRegisterId is provided
);
GO
