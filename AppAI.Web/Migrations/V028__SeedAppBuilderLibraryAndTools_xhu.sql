-- V028: Seed AppAgentToolLibrary then AppAgentLibraryTool, then retire old keys.
-- Split former platform-builtins / platform-queries BuiltIns by App Configuration area.
-- LibraryKey prefix: platform-   LibraryName prefix: BuiltIn:  (SqlQuery/HttpRest keep their own names).
-- Idempotent IF NOT EXISTS. execute_report is not seeded.
-- Empty shells: platform-workflow, platform-dashboard, platform-report.
-- Retires LibraryKey platform-builtins and platform-queries (FK-safe: tools first, then library).
-- Requires AppAgentToolDomain: platform, database-queries, external-rest (V018).

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. AppAgentToolLibrary
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-application')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-application',
    N'platform',
    N'BuiltIn: Application & Menu',
    N'Create/delete application packages, add items to the navigation menu, and explore existing apps. Subscribe to give an agent application-level configuration.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-transaction')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-transaction',
    N'platform',
    N'BuiltIn: Transaction & Form',
    N'Data model, form, and entity (list-of-value) configuration: create transactions from schema or tables, edit fields, and wire dropdowns.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-search')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-search',
    N'platform',
    N'BuiltIn: Search, View & Dataset',
    N'Search, search-view, and dataset configuration. List existing searches and create search screens backed by SQL.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-database')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-database',
    N'platform',
    N'BuiltIn: Database, Query & Diagram',
    N'Physical database, SQL query, table DDL, and (later) DB view / ER diagram tools. Use instead of the old platform-queries BuiltIn dump.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-workflow')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-workflow',
    N'platform',
    N'BuiltIn: Workflow Automation',
    N'Workflow automation configuration tools. Placeholder — no tools seeded yet.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-dashboard')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-dashboard',
    N'platform',
    N'BuiltIn: Dashboard',
    N'Dashboard configuration tools. Placeholder — no tools seeded yet.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-report')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-report',
    N'platform',
    N'BuiltIn: Report Runtime',
    N'Report execution tools (e.g. execute_report). Placeholder — execute_report is not seeded yet.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-memory')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-memory',
    N'platform',
    N'BuiltIn: Agent Memory',
    N'Cross-session memory recall for agents: load prior build context or search memory by keyword.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-multi-agent',
    N'platform',
    N'BuiltIn: Multi-Agent Coordination',
    N'Call other agents and share structured data across a workflow. Subscribe the orchestrator and all worker agents. Workers should use ExecutionMode=Deterministic.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-files')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'agent-files',
    N'platform',
    N'BuiltIn: Workspace Files',
    N'Read/write files under FileRepository/Company_{id}/AgentOutput/{sessionKey}/ (source/, output/, and user folders).',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'agent-scripts')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'agent-scripts',
    N'platform',
    N'BuiltIn: Workspace Scripts',
    N'Run user-provided scripts under FileRepository/Company_{id}/AgentOutput/{sessionKey}/source/ (sandboxed). Subscribe per agent; upload scripts via Files.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'sql-tenant-settings')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'sql-tenant-settings',
    N'database-queries',
    N'SQL: Tenant Settings',
    N'SqlQuery tools that read AppTenantSetting key/value configuration for the current tenant.',
    N'SqlQuery',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'demo-rest-api')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'demo-rest-api',
    N'external-rest',
    N'Demo REST API Tools',
    N'Example HttpRest tools showing the correct ToolConfig shape for calling external REST APIs. Replace the URL and headers with your real endpoint.',
    N'HttpRest',
    1
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. AppAgentLibraryTool
-- ─────────────────────────────────────────────────────────────────────────────

-- platform-application.propose_plan
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'propose_plan')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'propose_plan',
    N'REQUIRED GATE: presents a build plan summary to the user and blocks until they approve or reject. Returns {Confirmed:true/false}. Always call this before starting a new application build.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ProposePlan"}',
    1,
    0
);
GO

-- platform-application.explore_platform
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'explore_platform')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'explore_platform',
    N'Combined overview of the platform: applications, transactions, entity data sources, and database tables. Call this at the start of every session.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ExplorePlatform"}',
    1,
    1
);
GO

-- platform-application.search_platform
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'search_platform')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'search_platform',
    N'Keyword search across all apps, transactions, entity data sources, and database tables.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"SearchPlatform"}',
    1,
    2
);
GO

-- platform-application.list_applications
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'list_applications')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'list_applications',
    N'Full child tree of every application: transactions and searches.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ListApplications"}',
    1,
    3
);
GO

-- platform-application.create_app_package
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'create_app_package')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'create_app_package',
    N'Creates a new named application package. Returns the new SaasApplicationId. Pass this Id to ALL subsequent tool calls.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"CreateAppPackage"}',
    1,
    4
);
GO

-- platform-application.delete_application
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'delete_application')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'delete_application',
    N'Permanently deletes an application and all its transactions, searches, and entity data sources. Optionally drops physical DB tables if confirmed.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"DeleteApplication"}',
    1,
    5
);
GO

-- platform-application.add_transaction_to_menu
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'add_transaction_to_menu')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'add_transaction_to_menu',
    N'Adds a List or FolderList transaction to the application navigation menu.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddTransactionToMenu"}',
    1,
    6
);
GO

-- platform-application.add_search_to_menu
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'add_search_to_menu')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'add_search_to_menu',
    N'Step 6 — Add an existing search to the application navigation menu. MUST pass saasApplicationId from create_app_package so the item appears under the correct app.',
    N'{
  "searchId":          {"type":"integer", "description":"The ID of the search or saved search to add to the menu.", "required":true},
  "menuName":          {"type":"string",  "description":"Display label for the menu item, e.g. Employee List.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Places the item under the correct app in the sidebar.", "required":true},
  "isSavedSearch":     {"type":"boolean", "description":"Set true if searchId refers to a saved search; false for a regular search. Default false."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddSearchToMenu"}',
    1,
    7
);
GO

-- platform-transaction.propose_schema
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'propose_schema')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'propose_schema',
    N'Extracts a DB schema design from requirements via LLM and presents it to the user for review before any DDL is executed. Returns {Confirmed:true/false, SchemaJson}.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ProposeSchema"}',
    1,
    0
);
GO

-- platform-transaction.execute_approved_schema
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'execute_approved_schema')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'execute_approved_schema',
    N'Execute the schema approved by propose_schema: create physical DB tables and AppTransaction hierarchy. MUST pass saasApplicationId from create_app_package. Returns TransactionId, TablesCreated, LookupTables.',
    N'{
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results. Without this the transaction has no application parent.", "required":true},
  "transactionName":   {"type":"string",  "description":"Display name for the root transaction, e.g. Sales Order Management."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ExecuteApprovedSchema"}',
    1,
    1
);
GO

-- platform-transaction.create_application
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_application')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'create_application',
    N'Step 2+4 — Build the database schema and transaction hierarchy from natural language requirements. MUST have saasApplicationId from create_app_package — call that first. Returns created table names, TransactionId(s), and success/error status.',
    N'{
  "requirements":      {"type":"string",  "description":"Detailed natural-language description of what to build, including entity names, fields, and relationships.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results. Call create_app_package first if you do not have this value.", "required":true},
  "appName":           {"type":"string",  "description":"Optional descriptive name for the root transaction, e.g. Sales Order Management"},
  "entityMapJson":     {"type":"string",  "description":"JSON object mapping FK column names to EntityDataSource IDs — wires dropdowns at creation time."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateApplication"}',
    1,
    2
);
GO

-- platform-transaction.create_transaction_from_table
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_transaction_from_table')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'create_transaction_from_table',
    N'Create a single AppTransaction (data model + form) from a pre-existing database table. MUST pass saasApplicationId from create_app_package.',
    N'{
  "tableName":         {"type":"string",  "description":"Name of the existing database table.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.", "required":true},
  "schemaOwner":       {"type":"string",  "description":"Schema owner, e.g. dbo. Leave empty for the data source default."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateTransactionFromTable"}',
    1,
    3
);
GO

-- platform-transaction.create_hierarchy_from_tables
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_hierarchy_from_tables')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'create_hierarchy_from_tables',
    N'RECOVERY TOOL — Rebuild an AppTransaction hierarchy from tables that already exist in the database. MUST pass saasApplicationId from create_app_package.',
    N'{
  "masterTableName":   {"type":"string",  "description":"Name of the root/master table (the top-level parent).", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.", "required":true},
  "childTableNames":   {"type":"string",  "description":"Comma-separated names of child tables, e.g. OrderLine,OrderPayment"},
  "transactionName":   {"type":"string",  "description":"Display name for the transaction. Defaults to master table name."},
  "schemaOwner":       {"type":"string",  "description":"Schema owner, e.g. dbo"},
  "grandChildMapJson": {"type":"string",  "description":"JSON object mapping each child table name to its grandchild table names. Format: {ChildTable:[GrandChild1,GrandChild2]}"}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateHierarchyFromTables"}',
    1,
    4
);
GO

-- platform-transaction.create_list_edit_form
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_list_edit_form')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'create_list_edit_form',
    N'Creates a MasterDetail edit form linked to a List-type transaction and adds it to the navigation menu.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateListEditForm"}',
    1,
    5
);
GO

-- platform-transaction.get_existing_transactions
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'get_existing_transactions')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'get_existing_transactions',
    N'Lists all configured transaction units in the platform.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetExistingTransactions"}',
    1,
    6
);
GO

-- platform-transaction.get_transaction_details
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'get_transaction_details')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'get_transaction_details',
    N'Full configuration of a specific transaction: units, fields, search views.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetTransactionDetails"}',
    1,
    7
);
GO

-- platform-transaction.update_transaction_field
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'update_transaction_field')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'update_transaction_field',
    N'Modifies properties of an existing transaction field: displayName, controlType, entityId, or defaultValue.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"UpdateTransactionField"}',
    1,
    8
);
GO

-- platform-transaction.set_field_entity
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'set_field_entity')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'set_field_entity',
    N'Links a transaction field to an Entity Data Source so it renders as a dropdown. Automatically sets ControlType=1.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"SetFieldEntity"}',
    1,
    9
);
GO

-- platform-transaction.delete_transaction
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'delete_transaction')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'delete_transaction',
    N'Permanently deletes a transaction unit (configuration only — does NOT drop the physical database table).',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"DeleteTransaction"}',
    1,
    10
);
GO

-- platform-transaction.list_entity_data_sources
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'list_entity_data_sources')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'list_entity_data_sources',
    N'Lists all existing Entity Data Sources (both SimpleList and DatabaseTable types).',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"ListEntityDataSources"}',
    1,
    11
);
GO

-- platform-transaction.create_entity_simple_list
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_entity_simple_list')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'create_entity_simple_list',
    N'Creates a static dropdown Entity Data Source with fixed enumeration items (e.g. Sex, Status, Priority). Use for fixed sets that never grow from user data.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntitySimpleList"}',
    1,
    12
);
GO

-- platform-transaction.create_entity_from_table
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-transaction' AND ToolName = N'create_entity_from_table')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-transaction',
    N'create_entity_from_table',
    N'Creates a dynamic dropdown Entity Data Source backed by a database table (e.g. Customer, Department, Country). Use for managed data that users add/edit over time.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntityFromTable"}',
    1,
    13
);
GO

-- platform-search.list_available_searches
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search' AND ToolName = N'list_available_searches')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-search',
    N'list_available_searches',
    N'Lists all available search/report screens in the platform. Always call this first.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"ListAvailableSearches"}',
    1,
    0
);
GO

-- platform-search.list_searches
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search' AND ToolName = N'list_searches')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-search',
    N'list_searches',
    N'Lists existing search screens configured in the platform.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"ListSearches"}',
    1,
    1
);
GO

-- platform-search.get_search_criteria
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search' AND ToolName = N'get_search_criteria')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-search',
    N'get_search_criteria',
    N'Gets the available filter criteria fields for a specific search screen. Always call before execute_report.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"APP.BL.AppReportAgent.Plugins.ReportSearchPlugin","MethodName":"GetSearchCriteria"}',
    1,
    2
);
GO

-- platform-search.create_search
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search' AND ToolName = N'create_search')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-search',
    N'create_search',
    N'Create a search/list screen backed by a SQL query. MUST pass saasApplicationId from create_app_package. Returns SearchId, DataSetId, SearchViewId.',
    N'{
  "name":              {"type":"string",  "description":"Display name for the search screen, e.g. Employee List.", "required":true},
  "sqlQuery":          {"type":"string",  "description":"SQL SELECT query that fetches the data to display.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.", "required":true}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"CreateSearch"}',
    1,
    3
);
GO

-- platform-search.create_search_view
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search' AND ToolName = N'create_search_view')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-search',
    N'create_search_view',
    N'Generates a default search/list navigation view for a transaction.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateSearchView"}',
    1,
    4
);
GO

-- platform-database.get_database_schema
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'get_database_schema')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'get_database_schema',
    N'Call this tool to retrieve tables and columns for a registered DataSource. Pass dataSourceId for PLM / plmDW / another DB; omit or 0 to use the session default. Do not guess table or column names — call this (or get_table_schema) first.',
    N'{
  "properties":{
    "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
  },
  "required":[]
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.SchemaContextPlugin","MethodName":"GetDatabaseSchema"}',
    1,
    0
);
GO

-- platform-database.get_database_tables
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'get_database_tables')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'get_database_tables',
    N'List all tables and views in the target database. Pass dataSourceId to target a registered DataSource; omit or 0 for session default.',
    N'{
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetDatabaseTables"}',
    1,
    1
);
GO

-- platform-database.get_table_schema
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'get_table_schema')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'get_table_schema',
    N'Get column definitions (name, data type, nullable, primary key) for a specific database table. Pass dataSourceId to target a registered DataSource; omit or 0 for session default.',
    N'{
  "tableName":{"type":"string","description":"Table name to inspect","required":true},
  "schemaOwner":{"type":"string","description":"Schema owner, e.g. dbo"},
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"GetTableSchema"}',
    1,
    2
);
GO

-- platform-database.check_table_exists
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'check_table_exists')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'check_table_exists',
    N'Check whether a specific database table exists. Returns true/false. Pass dataSourceId to target a registered DataSource; omit or 0 for session default.',
    N'{
  "tableName":{"type":"string","description":"Table name to check","required":true},
  "schemaOwner":{"type":"string","description":"Schema owner, default dbo"},
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"CheckTableExists"}',
    1,
    3
);
GO

-- platform-database.execute_sql
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'execute_sql')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'execute_sql',
    N'Execute a SQL SELECT query against a registered DataSource. Only SELECT is allowed. Pass dataSourceId to target PLM / plmDW / another registered DB; omit or 0 to use the session default data source.',
    N'{
  "sql":{"type":"string","description":"A SQL SELECT statement. Must start with SELECT.","required":true},
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"ExecuteSql"}',
    1,
    4
);
GO

-- platform-database.create_database_table
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'create_database_table')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'create_database_table',
    N'Executes a SQL CREATE TABLE statement against the target database.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"CreateDatabaseTable"}',
    1,
    5
);
GO

-- platform-database.alter_table
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'alter_table')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'alter_table',
    N'Runs an ALTER TABLE statement AND keeps the AppAI platform data model in sync. Use this instead of raw DDL when modifying columns on a managed transaction table.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaAlterPlugin","MethodName":"AlterTable"}',
    1,
    6
);
GO

-- platform-database.insert_mockup_data
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'insert_mockup_data')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'insert_mockup_data',
    N'Inserts realistic sample rows into a table (INSERT statements only) for demo or testing purposes.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"InsertMockupData"}',
    1,
    7
);
GO

-- platform-database.confirm_drop_tables
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'confirm_drop_tables')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'confirm_drop_tables',
    N'Asks the user whether to physically DROP database tables when deleting an application. Returns {DropTables:true/false}.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ConfirmDropTables"}',
    1,
    8
);
GO

-- platform-memory.load_memory_context
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-memory' AND ToolName = N'load_memory_context')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-memory',
    N'load_memory_context',
    N'Call this tool at the start of a session to recall what was built in previous sessions: existing tables, transactions, and prior user requests. Use it when the user references prior work or asks what currently exists in the platform. For targeted recall by keyword, use search_memory instead.',
    N'{"properties":{},"required":[]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.MemoryContextPlugin","MethodName":"LoadMemoryContext"}',
    1,
    0
);
GO

-- platform-memory.search_memory
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-memory' AND ToolName = N'search_memory')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-memory',
    N'search_memory',
    N'RAG: searches past build history, platform notes, and agent observations by keyword. Call at session start and when the user references past decisions.',
    NULL,
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.MemorySearchPlugin","MethodName":"SearchMemory"}',
    1,
    1
);
GO

-- platform-multi-agent.call_agent
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'call_agent')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'call_agent',
    N'Invoke another agent by its SkillKey and wait for its response. Use this to delegate subtasks to specialised agents (e.g. sales-order-agent, ws-manager-agent). The called agent runs headlessly and returns its final answer as a string. Share data with the called agent via write_shared_context before calling it.',
    N'{"type":"object","properties":{"targetSkillKey":{"type":"string","description":"The SkillKey of the agent to invoke (must have ExecutionMode=Deterministic)"},"message":{"type":"string","description":"The instruction or question to send to the target agent"}},"required":["targetSkillKey","message"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.AgentCallPlugin","MethodName":"CallAgent"}',
    1,
    0
);
GO

-- platform-multi-agent.write_shared_context
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'write_shared_context')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'write_shared_context',
    N'Write a JSON value to the shared workflow blackboard under the given key. All agents in the same workflow (sharing the same WorkflowId) can read this value via read_shared_context. Use this to pass structured data — production lot details, order IDs, quantities — from the orchestrator to worker agents before calling them.',
    N'{"type":"object","properties":{"key":{"type":"string","description":"The context key (e.g. production-lot, order-summary)"},"valueJson":{"type":"string","description":"A JSON string containing the data to store"}},"required":["key","valueJson"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.AgentSharedContextPlugin","MethodName":"WriteContext"}',
    1,
    1
);
GO

-- platform-multi-agent.read_shared_context
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'read_shared_context')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'read_shared_context',
    N'Read a JSON value from the shared workflow blackboard by key. Returns the JSON string written by write_shared_context, or an empty object {} if the key does not exist. Worker agents call this at the start of their run to receive production data, order details, or any context the orchestrator prepared for them.',
    N'{"type":"object","properties":{"key":{"type":"string","description":"The context key to read (e.g. production-lot, order-summary)"}},"required":["key"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.AgentSharedContextPlugin","MethodName":"ReadContext"}',
    1,
    2
);
GO

-- agent-files.file_list
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_list')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_list',
    N'List files and folders under the current chat file area (relative to AgentOutput/{sessionKey}/). Omit path or pass empty for the root.',
    N'{"path":{"type":"string","description":"Optional relative folder path. Empty = file area root."}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"List"}',
    1,
    0
);
GO

-- agent-files.file_read
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_read')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_read',
    N'Read a file from the chat file area (relative to AgentOutput/{sessionKey}/). Text files return UTF-8 content. .xlsx/.xls return tabular JSON (sheet, headers, rows) via GemBox — not binary.',
    N'{"path":{"type":"string","description":"Relative file path","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Read"}',
    1,
    1
);
GO

-- agent-files.file_write
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_write')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_write',
    N'Write a file into the chat file area. Text: UTF-8 body. .xlsx/.xls: pass JSON {"mode":"append|overwrite","sheet":"Log","headers":["A","B"],"rows":[["v1","v2"]]} or plain/CSV lines (plain one-line log becomes a Message column). Creates real Excel via GemBox — do not write binary as text.',
    N'{"path":{"type":"string","description":"Relative file path","required":true},"content":{"type":"string","description":"Text body, or for Excel: JSON {mode,sheet,headers,rows} or CSV/TSV/plain log lines","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Write"}',
    1,
    2
);
GO

-- agent-files.file_mkdir
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_mkdir')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_mkdir',
    N'Create a folder (and parents) under the current chat file area.',
    N'{"path":{"type":"string","description":"Relative folder path","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Mkdir"}',
    1,
    3
);
GO

-- agent-files.file_delete
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'file_delete')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'file_delete',
    N'Delete a file or folder (recursive) under the current chat file area.',
    N'{"path":{"type":"string","description":"Relative path to delete","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"Delete"}',
    1,
    4
);
GO

-- agent-scripts.run_agent_script
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-scripts' AND ToolName = N'run_agent_script')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-scripts',
    N'run_agent_script',
    N'Run a script file from the current chat AgentOutput area (default: only under source/, .ps1). Use after writing config files. Example for PLM DW Phase B: relativePath=source/_gen_plmdw_import_sql.ps1. Returns exit code, log tails, and output/ file sizes. Do not invent stub SQL/JSON — call this tool instead.',
    N'{"relativePath":{"type":"string","description":"Relative path under AgentOutput/{sessionKey}/, e.g. source/_gen_plmdw_import_sql.ps1","required":true}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentScriptPlugin","MethodName":"Run","AllowedPathPrefixes":["source/"],"AllowedExtensions":[".ps1"],"MaxTimeoutSeconds":1200,"InjectDataSourcesFromConfig":"source/dwTabImportConfig.json"}',
    1,
    0
);
GO

-- agent-scripts.validate_agent_outputs
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-scripts' AND ToolName = N'validate_agent_outputs')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-scripts',
    N'validate_agent_outputs',
    N'Check deliverable file sizes under a folder (default output/). Pass minSizeByFileNameJson as a JSON object of fileName→minBytes.',
    N'{"relativeDir":{"type":"string","description":"Folder under AgentOutput (default output/)"},"minSizeByFileNameJson":{"type":"string","description":"JSON map of file name to minimum SizeBytes, e.g. {\"1_PlmDw_Tables.sql\":400000,\"4_PlmDw_ImportBlueprint.json\":500000}"}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentScriptPlugin","MethodName":"ValidateOutputs"}',
    1,
    1
);
GO

-- sql-tenant-settings.get_tenant_settings
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'sql-tenant-settings' AND ToolName = N'get_tenant_settings')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'sql-tenant-settings',
    N'get_tenant_settings',
    N'Call this tool when the user asks about platform configuration, application settings, or tenant settings. Returns key-value configuration pairs for the current tenant. Filter by category keyword if the user names a specific area.',
    N'{"properties":{"category":{"description":"Optional keyword to filter setting keys (e.g. ''AI'', ''Email'', ''Theme''). Leave empty to return all settings.","type":"string"}},"required":[]}',
    N'SqlQuery',
    N'{"SqlBody":"SELECT SettingKey, SettingValue FROM dbo.AppTenantSetting WHERE IsActive=1 AND (@category IS NULL OR @category = '''' OR SettingKey LIKE ''%'' + @category + ''%'') ORDER BY SettingKey","ReturnType":"json"}',
    1,
    0
);
GO

-- demo-rest-api.get_post_by_id
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'demo-rest-api' AND ToolName = N'get_post_by_id')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'demo-rest-api',
    N'get_post_by_id',
    N'Call this tool when the user asks to retrieve a post or article by its numeric ID. Returns the post title and body from the demo API. Replace with your real API endpoint and authentication headers.',
    N'{"properties":{"postId":{"description":"The numeric ID of the post to retrieve","type":"string"}},"required":["postId"]}',
    N'HttpRest',
    N'{"Url":"https://jsonplaceholder.typicode.com/posts/{postId}","Method":"GET","Headers":{}}',
    1,
    0
);
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Retire old LibraryKey values: platform-builtins, platform-queries
-- FK order:
--   AppAgentLibraryTool.LibraryKey      -> AppAgentToolLibrary  (NO ACTION)
--     DELETE tools on old keys before DELETE library.
--   AppAgentLibrarySubscription.LibraryKey -> AppAgentToolLibrary  (ON DELETE CASCADE)
--     copy subscriptions to new keys first; leftover rows fall with the library.
-- ─────────────────────────────────────────────────────────────────────────────

-- 3a. Keep LibraryName in sync when the row already existed (INSERT skipped)
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Application & Menu',            ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-application';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Transaction & Form',            ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-transaction';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Search, View & Dataset',        ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-search';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Database, Query & Diagram',     ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-database';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Workflow Automation',           ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-workflow';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Dashboard',                     ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-dashboard';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Report Runtime',                ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-report';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Agent Memory',                  ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-memory';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Multi-Agent Coordination',      ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'platform-multi-agent';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Workspace Files',               ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'agent-files';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'BuiltIn: Workspace Scripts',             ToolCategory = N'BuiltIn',  DomainKey = N'platform'          WHERE LibraryKey = N'agent-scripts';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'SQL: Tenant Settings',                   ToolCategory = N'SqlQuery', DomainKey = N'database-queries'  WHERE LibraryKey = N'sql-tenant-settings';
UPDATE dbo.AppAgentToolLibrary SET LibraryName = N'Demo REST API Tools',                    ToolCategory = N'HttpRest', DomainKey = N'external-rest'     WHERE LibraryKey = N'demo-rest-api';
GO

-- 3b. Copy subscriptions off retired keys onto the split libraries
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
SELECT DISTINCT s.SkillKey, m.NewLibraryKey
FROM dbo.AppAgentLibrarySubscription s
INNER JOIN (VALUES
    (N'platform-builtins', N'platform-application'),
    (N'platform-builtins', N'platform-transaction'),
    (N'platform-builtins', N'platform-search'),
    (N'platform-builtins', N'platform-database'),
    (N'platform-builtins', N'platform-memory'),
    (N'platform-queries',  N'platform-database'),
    (N'platform-queries',  N'sql-tenant-settings')
) m(OldLibraryKey, NewLibraryKey) ON s.LibraryKey = m.OldLibraryKey
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription x
    WHERE x.SkillKey = s.SkillKey AND x.LibraryKey = m.NewLibraryKey
);
GO

-- 3c. Remove tools on retired keys (required before DELETE library: NO ACTION FK)
DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey IN (N'platform-builtins', N'platform-queries');
GO

-- 3d. MCP rows keyed by library SkillKey (same order as AppAgentToolLibraryBL.DeleteLibrary)
IF OBJECT_ID(N'dbo.AppAgentMcpServer', N'U') IS NOT NULL
DELETE FROM dbo.AppAgentMcpServer
WHERE SkillKey IN (N'platform-builtins', N'platform-queries');
GO

-- 3e. Leftover AppAgentToolRegister rows from V018/V020 (SkillKey used as LibraryKey)
DELETE FROM dbo.AppAgentToolRegister
WHERE SkillKey IN (N'platform-builtins', N'platform-queries');
GO

-- 3f. Drop retired libraries last (subscription leftovers CASCADE)
DELETE FROM dbo.AppAgentToolLibrary
WHERE LibraryKey IN (N'platform-builtins', N'platform-queries');
GO

