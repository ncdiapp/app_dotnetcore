-- ============================================================
-- AppAgentToolRegister — SkillKey = 'app-builder'
-- Generated from APP.BL/AIAgent/AppBuilderAgent/Plugins/
-- 34 tools across 12 plugin classes
-- ============================================================

DELETE FROM dbo.AppAgentToolRegister WHERE SkillKey = 'app-builder';

-- ============================================================
-- ApplicationManagerPlugin (4 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_app_package',
    'Step 1 — Create a new named application package in the platform. Returns the new SaasApplicationId (integer). Call this FIRST before creating tables, entities, or transactions. Pass the returned SaasApplicationId to all subsequent tools that require it.',
    '{"applicationName":{"type":"string","description":"Display name for the new application, e.g. Sales Order Management","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"CreateAppPackage"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'delete_application',
    'Permanently delete an application and ALL of its associated data: transactions (data models + forms), search views, entity data sources, and optionally the physical database tables. Find the application by name (case-insensitive). ALWAYS call propose_plan before this — deletion is irreversible. Returns a detailed report of everything that was deleted and any errors.',
    '{"applicationName":{"type":"string","description":"Name of the application to delete (case-insensitive match).","required":true},"dropDatabaseTables":{"type":"boolean","description":"Set true to also DROP the physical database tables. Default false — only removes AppAI configuration."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"DeleteApplication"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'add_transaction_to_menu',
    'Add a List or FolderList transaction (data model) to the application''s main navigation menu as a clickable item. Use this to expose a List Edit transaction in the left-side navigation. Looks up the transaction by name (case-insensitive), then adds it under the menu group. Only List and FolderList transaction types are supported (not MasterDetail). To find the correct transactionName use search_platform or get_existing_transactions first.',
    '{"transactionName":{"type":"string","description":"Name of the transaction to add (case-insensitive match against TransactionName).","required":true},"menuName":{"type":"string","description":"Display label shown in the navigation menu, e.g. Customers","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddTransactionToMenu"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'add_search_to_menu',
    'Step 6 — Add an existing search to the application navigation menu. MUST pass saasApplicationId from create_app_package so the item appears under the correct app.',
    '{"searchId":{"type":"integer","description":"The ID of the search or saved search to add to the menu","required":true},"menuName":{"type":"string","description":"Display label for the menu item, e.g. Employee List","required":true},"saasApplicationId":{"type":"integer","description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.","required":true},"isSavedSearch":{"type":"boolean","description":"Set true if searchId refers to a saved search; false for a regular search"}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin","MethodName":"AddSearchToMenu"}',
    1
);

-- ============================================================
-- DataQueryPlugin (3 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'execute_sql',
    'Execute a SQL SELECT query to verify tables were created or check data. Only SELECT is allowed.',
    '{"sql":{"type":"string","description":"A SQL SELECT statement. Must start with SELECT.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"ExecuteSql"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'insert_mockup_data',
    'Insert realistic sample/demo rows into a database table so the app has data to show. Call this AFTER create_application and create_search_view succeed. Only INSERT statements are allowed. Generate enough rows to demonstrate every dropdown, relationship, and field (typically 5-15 rows per table). Start with lookup/reference tables (no FK dependencies), then master rows, then child rows. Do NOT wrap in a transaction — execute each INSERT individually so partial success is preserved. Returns a count of rows inserted and any errors.',
    '{"tableName":{"type":"string","description":"Table name being populated (for labelling only)","required":true},"insertSql":{"type":"string","description":"One or more SQL INSERT statements separated by semicolons. Must only contain INSERT INTO statements.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"InsertMockupData"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'check_table_exists',
    'Check whether a specific database table exists. Returns true/false.',
    '{"tableName":{"type":"string","description":"Table name to check","required":true},"schemaOwner":{"type":"string","description":"Schema owner, default dbo"}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.DataQueryPlugin","MethodName":"CheckTableExists"}',
    1
);

-- ============================================================
-- EntityDataSourcePlugin (3 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'list_entity_data_sources',
    'List all existing Entity Data Sources (dropdown definitions) in the platform. Call this during explore_platform or before creating entities to avoid duplicates.',
    '{}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"ListEntityDataSources"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_entity_simple_list',
    'Step 3a — Create a Simple List Entity Data Source: a static dropdown with fixed items (e.g. Order Status: Approved / Rejected / Canceled). Use this for short, fixed-value lists that don''t come from a database table. Provide items as a comma-separated string: ''Item1,Item2,Item3''.',
    '{"entityCode":{"type":"string","description":"Name/code for this entity, e.g. Order Status","required":true},"items":{"type":"string","description":"Comma-separated list of item labels in order, e.g. Approved,Rejected,Canceled","required":true},"saasApplicationId":{"type":"integer","description":"The SaasApplicationId returned by create_app_package","required":true},"description":{"type":"string","description":"Optional description of this entity"}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntitySimpleList"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_entity_from_table',
    'Step 3b — Create a Database Table Entity Data Source: a dynamic dropdown backed by rows from an existing database table (e.g. a Customer table where CustomerId is the value and CustomerName is the display label). The table must already exist in the database before calling this.',
    '{"entityCode":{"type":"string","description":"Name/code for this entity, e.g. Customer List","required":true},"tableName":{"type":"string","description":"Database table name without schema, e.g. AppCustomer","required":true},"schemaOwner":{"type":"string","description":"Schema owner of the table, e.g. dbo","required":true},"identityField":{"type":"string","description":"Column name to use as the selected value (primary key), e.g. CustomerId","required":true},"displayField1":{"type":"string","description":"Primary display column shown to user, e.g. CustomerName","required":true},"saasApplicationId":{"type":"integer","description":"The SaasApplicationId returned by create_app_package","required":true},"displayField2":{"type":"string","description":"Optional secondary display column"},"displayField3":{"type":"string","description":"Optional tertiary display column"},"description":{"type":"string","description":"Optional description of this entity"}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.EntityDataSourcePlugin","MethodName":"CreateEntityFromTable"}',
    1
);

-- ============================================================
-- MemorySearchPlugin (1 tool)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'search_memory',
    'Search past build history, platform notes, and agent observations for entries that match the given keywords. Use this at the start of a session to recall what was built previously, or when you need to find historical context about a specific application or table.',
    '{"query":{"type":"string","description":"One or more keywords describing what you are looking for, e.g. application name, table name, topic.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.MemorySearchPlugin","MethodName":"SearchMemory"}',
    1
);

-- ============================================================
-- PlanConfirmPlugin (2 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'propose_plan',
    'REQUIRED GATE — call this BEFORE create_application or create_database_table. Presents a summary of what will be built to the user and waits for their approval. Returns {Confirmed:true} if the user approves — then proceed with building. Returns {Confirmed:false,Reason:...} if the user rejects — adjust the plan and call propose_plan again. Never skip this step for any operation that creates or modifies database tables.',
    '{"planSummary":{"type":"string","description":"Plain-text summary of what will be created: which tables, fields, relationships, and screens.","required":true},"tablesToCreate":{"type":"string","description":"Comma-separated list of database table names that will be physically created, e.g. Order,OrderItem,Customer."},"screensToCreate":{"type":"string","description":"Comma-separated list of transaction/screen names that will be generated, e.g. Order Management,Customer List."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ProposePlan"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'confirm_drop_tables',
    'Ask the user whether the physical database tables for a deleted application should be DROPped. Call this AFTER propose_plan confirms the deletion intent, but BEFORE calling delete_application. Pass the list of table names that would be dropped. Returns {DropTables:true} if the user wants the tables removed, {DropTables:false} to keep them. Pass the returned DropTables value as dropDatabaseTables to delete_application.',
    '{"tableNames":{"type":"string","description":"Comma-separated list of database table names that will be dropped if the user confirms.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ConfirmDropTables"}',
    1
);

-- ============================================================
-- PlatformExplorerPlugin (6 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'explore_platform',
    'Get a combined overview of existing application packages, transactions (data models), entity data sources, and database tables. Call this FIRST to understand what already exists before building anything.',
    '{}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ExplorePlatform"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'search_platform',
    'Search existing platform items by keyword: applications, transactions, entity data sources, and database tables whose name contains the query string (case-insensitive). Use this instead of explore_platform when you only need to find a specific item by name. Returns up to 20 matches per category.',
    '{"query":{"type":"string","description":"Keyword to search for — matched against names of apps, transactions, entities, and tables.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"SearchPlatform"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'list_applications',
    'Return the full child tree of every application package: transactions (with field count and table name) and search screens. Use this for modification requests — it tells you exactly what exists under each app so you can target the right transaction ID for update_transaction_field, set_field_entity, or delete_transaction. More detailed than explore_platform for modification work.',
    '{}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"ListApplications"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'get_database_tables',
    'List all tables and views in the target database.',
    '{}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetDatabaseTables"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'get_existing_transactions',
    'List all configured transaction units (data models / screens) in the application.',
    '{}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetExistingTransactions"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'get_transaction_details',
    'Get the full configuration of a transaction, including all units, fields, and search views.',
    '{"transactionId":{"type":"integer","description":"The transaction ID to inspect","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.PlatformExplorerPlugin","MethodName":"GetTransactionDetails"}',
    1
);

-- ============================================================
-- SchemaAlterPlugin (1 tool)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'alter_table',
    'Add, rename, or drop a column on an existing database table AND keep the AppAI data model in sync. alterSql must be a single ALTER TABLE statement, e.g. "ALTER TABLE [dbo].[Order] ADD Notes NVARCHAR(500) NULL". If transactionId is supplied the tool also adds a matching AppTransactionField to the unit so the form immediately shows the new field — pass newFieldJson to describe it. Returns {IsSuccess, TableName, AlterSql, FieldAdded} on success.',
    '{"tableName":{"type":"string","description":"The database table name to alter, e.g. Order or dbo.Order.","required":true},"alterSql":{"type":"string","description":"A single ALTER TABLE SQL statement. Must begin with ALTER TABLE.","required":true},"transactionId":{"type":"integer","description":"Transaction ID whose data model should be updated to match the schema change. Omit for DDL-only without platform sync."},"newFieldJson":{"type":"string","description":"JSON describing the new platform field when a column is being ADDED. Keys: displayName, controlType, entityId, isNullable, defaultValue. Omit when dropping or renaming."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaAlterPlugin","MethodName":"AlterTable"}',
    1
);

-- ============================================================
-- SchemaBuilderPlugin (2 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'get_table_schema',
    'Get column definitions (name, data type, nullable, primary key) for a specific database table.',
    '{"tableName":{"type":"string","description":"Table name to inspect","required":true},"schemaOwner":{"type":"string","description":"Schema owner, e.g. dbo"}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"GetTableSchema"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_database_table',
    'Execute a SQL CREATE TABLE statement to create a new table in the database. WARNING: Do NOT use this after create_application — create_application already creates all tables. Only use this when explicitly asked to create a specific table manually.',
    '{"createTableSql":{"type":"string","description":"The full CREATE TABLE SQL script","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"CreateDatabaseTable"}',
    1
);

-- ============================================================
-- SchemaDesignerPlugin (2 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'propose_schema',
    'Extract a database schema from natural-language requirements and present it to the user for review and optional inline editing BEFORE any DDL is executed. The user will see each table with its columns, data types, FK relationships, and a CREATE TABLE preview. They can rename columns, change types, remove unwanted columns, or reject with feedback. Returns {Confirmed:true, TableCount, Tables:[...names...]} when the user approves — the schema is stored internally. Returns {Confirmed:false, Feedback:''...''} when the user rejects — adjust requirements and call again. IMPORTANT: Call this EXACTLY ONCE per new application. When it returns {Confirmed:true}, immediately call execute_approved_schema with NO schemaJson argument — the schema is already stored. execute_approved_schema does NOT need a separate propose_plan call; propose_schema already served as the approval gate.',
    '{"requirements":{"type":"string","description":"Detailed natural-language description of the entities, fields, and relationships to build.","required":true},"appName":{"type":"string","description":"Application name used to prefix all table names, e.g. SalesOrder. Max 10 chars, letters/numbers only.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ProposeSchema"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'execute_approved_schema',
    'Execute the schema approved by propose_schema: create the physical database tables and AppTransaction hierarchy. Call this ONLY after propose_schema returns {Confirmed:true}. Do NOT pass schemaJson — the schema is stored internally. On partial failure (tables created but transactions failed), all newly created tables are automatically dropped and the error is reported. Returns {IsSuccess, TransactionId, TransactionName, TablesCreated[], LookupTables[], RolledBack[]}. IMPORTANT: LookupTables[] contains standalone reference tables excluded from the master-detail hierarchy. Their tables are ALREADY IN THE DATABASE — do not call propose_schema or execute_approved_schema again. For each table in LookupTables call ONLY: 1) create_entity_from_table 2) create_transaction_from_table 3) create_list_edit_form 4) create_search_view. propose_schema is called EXACTLY ONCE per application — never call it again after this point.',
    '{"saasApplicationId":{"type":"integer","description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.","required":true},"transactionName":{"type":"string","description":"Display name for the root transaction, e.g. Sales Order Management."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SchemaDesignerPlugin","MethodName":"ExecuteApprovedSchema"}',
    1
);

-- ============================================================
-- SearchBuilderPlugin (2 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_search',
    'Create a search/list screen backed by a SQL query. Internally creates: (1) a Dataset that holds the SQL query, (2) a SearchView grid with all query columns, (3) a Search that users navigate to. Returns SearchId, DataSetId, and SearchViewId on success. Call AFTER create_application so tables exist. Always pass SaasApplicationId to link the search to the correct application.',
    '{"name":{"type":"string","description":"Display name for the search screen, e.g. Order List.","required":true},"sqlQuery":{"type":"string","description":"SQL SELECT query that fetches the data to display, e.g. SELECT * FROM dbo.Order.","required":true},"saasApplicationId":{"type":"integer","description":"The SaasApplicationId returned by create_app_package.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"CreateSearch"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'list_searches',
    'List existing searches in the platform. Use this during exploration (Step 1) to see what searches already exist so you avoid creating duplicates.',
    '{"saasApplicationId":{"type":"integer","description":"Optional SaasApplicationId to filter searches by application."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"ListSearches"}',
    1
);

-- ============================================================
-- TransactionBuilderPlugin (5 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_application',
    'Step 2+4 — Build the database schema and transaction hierarchy from natural language requirements. Runs the full pipeline: AI extracts schema, creates physical tables, then creates the AppTransaction hierarchy with forms. PREFER propose_schema + execute_approved_schema for new builds — they let the user review the schema first. Use create_application only when skipping schema review is intentional. Call AFTER create_app_package and AFTER any create_entity_* calls. Pass entityMapJson to wire FK columns to entity dropdowns at creation time (eliminates the post-hoc set_field_entity pass). On partial failure (tables created but transactions failed) all newly created tables are automatically rolled back. Returns created table names, TransactionId(s), and success/error status.',
    '{"requirements":{"type":"string","description":"Detailed natural-language description of what to build, including entity names, fields, and relationships.","required":true},"saasApplicationId":{"type":"integer","description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.","required":true},"appName":{"type":"string","description":"Optional descriptive name for the root transaction, e.g. Sales Order Management"},"entityMapJson":{"type":"string","description":"JSON object mapping FK column names to EntityDataSource IDs — wires dropdowns at creation time. Obtain entity IDs from list_entity_data_sources."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateApplication"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_search_view',
    'Generate a default search/list navigation view for an existing transaction. Call this after create_application for each main transaction.',
    '{"transactionId":{"type":"integer","description":"The transaction ID to create the search view for","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateSearchView"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_transaction_from_table',
    'Create a single AppTransaction (data model + form) from a PRE-EXISTING database table. WARNING: ONLY use this for tables that existed BEFORE your current session (discovered via explore_platform). NEVER call this after create_application — create_application already creates all transactions. The table must already exist in the database.',
    '{"tableName":{"type":"string","description":"Name of the existing database table","required":true},"saasApplicationId":{"type":"integer","description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.","required":true},"schemaOwner":{"type":"string","description":"Schema owner, e.g. dbo. Leave empty to use the data source default."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateTransactionFromTable"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_hierarchy_from_tables',
    'RECOVERY TOOL — Create an AppTransaction hierarchy from tables that already exist in the database. Use this when create_application failed AFTER the tables were physically created (i.e. you can see the tables in explore_platform or check_table_exists confirms they exist). Provide the master (root) table name, child table names as a comma-separated list, and optionally grandChildMapJson to specify grandchild tables per child. FK relationships are auto-detected from the database schema.',
    '{"masterTableName":{"type":"string","description":"Name of the root/master table (the top-level parent)","required":true},"saasApplicationId":{"type":"integer","description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.","required":true},"childTableNames":{"type":"string","description":"Comma-separated names of child tables that belong under the master, e.g. OrderLine,OrderPayment"},"transactionName":{"type":"string","description":"Display name for the transaction, e.g. Sales Order Management. Defaults to master table name."},"schemaOwner":{"type":"string","description":"Schema owner, e.g. dbo"},"grandChildMapJson":{"type":"string","description":"JSON object mapping each child table name to its grandchild table names. Format: {ChildTable:[GrandChild1,GrandChild2]}. Only needed for 3-level hierarchies."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateHierarchyFromTables"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'create_list_edit_form',
    'Create a MasterDetail edit form linked to an existing List-type transaction. A List transaction (Data Model Type = 3. List) shows data in a grid. Calling this tool generates the corresponding edit/create/delete form, wires up the Create, Edit (Open), and Delete link-target actions automatically, and adds the list transaction to the application''s left navigation menu. Call AFTER create_transaction_from_table or create_hierarchy_from_tables when the resulting transaction is a List type. Returns the new MasterDetail TransactionId and FormId on success. NOTE: The nav menu entry is created automatically — do NOT call create_search_view separately for this transaction.',
    '{"listTransactionId":{"type":"integer","description":"TransactionId of the existing List-type transaction to add an edit form to.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionBuilderPlugin","MethodName":"CreateListEditForm"}',
    1
);

-- ============================================================
-- TransactionModifierPlugin (3 tools)
-- ============================================================

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'update_transaction_field',
    'Modify properties of an existing field on a transaction. Use this after create_application or when the user asks to rename a label, change a field to a dropdown, set a default value, or link a field to an entity. Pass only the properties you want to change in changesJson — omit unchanged ones. Supported keys: displayName (string), controlType (int), entityId (int), defaultValue (string). controlType values: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time. IMPORTANT: fieldId is unique and always preferred. fieldName alone is NOT unique across a hierarchy — the same column name can exist in the master and child units. Use get_transaction_details to find the exact fieldId before calling this tool. Returns the updated field state on success.',
    '{"transactionId":{"type":"integer","description":"ID of the transaction that owns the field.","required":true},"fieldId":{"type":"integer","description":"Unique numeric Id of the field to modify (preferred). Obtain from get_transaction_details."},"fieldName":{"type":"string","description":"Field name to match by DisplayName or DB column name. Only used when fieldId is not known."},"unitName":{"type":"string","description":"Unit name or table name to scope the fieldName search. Required when fieldName appears in more than one unit."},"changesJson":{"type":"string","description":"JSON object with the properties to change, e.g. displayName, controlType, entityId, defaultValue.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"UpdateTransactionField"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'set_field_entity',
    'Link a transaction field to an Entity Data Source so it renders as a dropdown (DDL). Use this AFTER create_application or create_transaction_from_table to wire up FK columns that should show as dropdowns. Automatically sets ControlType = 1 (DDL). Pass entityId = null to remove an existing entity link and revert to a plain text/numeric field. IMPORTANT: fieldId is unique and always preferred. Use get_transaction_details to find fieldId first.',
    '{"transactionId":{"type":"integer","description":"ID of the transaction that owns the field.","required":true},"fieldId":{"type":"integer","description":"Unique numeric Id of the field to link (preferred). Obtain from get_transaction_details."},"fieldName":{"type":"string","description":"Field name to match by DisplayName or DB column name. Only used when fieldId is not known."},"unitName":{"type":"string","description":"Unit name or table name to scope the fieldName search when the name is not unique across units."},"entityId":{"type":"integer","description":"ID of the Entity Data Source to link (from list_entity_data_sources). Pass null to remove the link."}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"SetFieldEntity"}',
    1
);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'app-builder',
    'delete_transaction',
    'Permanently delete a transaction and all its units, fields, forms, and search views. Use this when the user asks to remove a screen/module from the application. WARNING: This is irreversible. Always confirm with propose_plan before calling this. Does NOT drop the underlying database table — only removes the AppAI configuration.',
    '{"transactionId":{"type":"integer","description":"ID of the transaction to delete.","required":true}}',
    'BuiltIn',
    '{"TypeName":"App.BL.AppBuilderAgent.Plugins.TransactionModifierPlugin","MethodName":"DeleteTransaction"}',
    1
);
