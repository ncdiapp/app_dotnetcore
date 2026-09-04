-- V015: Two fixes for the app-builder Generic Agent tool registry.
--
-- Fix 1: ParseKernelParameters (GenericAgentEngine.cs) was changed to handle the
--        flat ParameterSchemaJson format. This migration updates the 4 tools whose
--        saasApplicationId was NOT marked "required":true so the LLM now sees it
--        as a mandatory parameter and always passes it.
--
-- Fix 2: add_search_to_menu was updated by V013 but lost the saasApplicationId
--        required flag and lost its isSavedSearch parameter. Restored here.
--
-- Root cause: The LLM called create_application, execute_approved_schema,
--             create_transaction_from_table, create_hierarchy_from_tables without
--             saasApplicationId (or passed 5602 from explore_platform results).
--             The code guards blocked those calls, leaving zero DB records even
--             though the agent reported "successfully built."
-- ============================================================

-- create_application: add "required":true to saasApplicationId
UPDATE dbo.AppAgentToolRegister
SET ParameterSchemaJson = N'{
  "requirements":      {"type":"string",  "description":"Detailed natural-language description of what to build, including entity names, fields, and relationships.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results. Call create_app_package first if you do not have this value.", "required":true},
  "appName":           {"type":"string",  "description":"Optional descriptive name for the root transaction, e.g. Sales Order Management"},
  "entityMapJson":     {"type":"string",  "description":"JSON object mapping FK column names to EntityDataSource IDs — wires dropdowns at creation time."}
}',
    ToolDescription = 'Step 2+4 — Build the database schema and transaction hierarchy from natural language requirements. MUST have saasApplicationId from create_app_package — call that first. Returns created table names, TransactionId(s), and success/error status.'
WHERE SkillKey = 'app-builder' AND ToolName = 'create_application';
GO

-- execute_approved_schema: add "required":true to saasApplicationId
UPDATE dbo.AppAgentToolRegister
SET ParameterSchemaJson = N'{
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results. Without this the transaction has no application parent.", "required":true},
  "transactionName":   {"type":"string",  "description":"Display name for the root transaction, e.g. Sales Order Management."}
}',
    ToolDescription = 'Execute the schema approved by propose_schema: create physical DB tables and AppTransaction hierarchy. MUST pass saasApplicationId from create_app_package. Returns TransactionId, TablesCreated, LookupTables.'
WHERE SkillKey = 'app-builder' AND ToolName = 'execute_approved_schema';
GO

-- create_transaction_from_table: add "required":true to saasApplicationId
UPDATE dbo.AppAgentToolRegister
SET ParameterSchemaJson = N'{
  "tableName":         {"type":"string",  "description":"Name of the existing database table.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.", "required":true},
  "schemaOwner":       {"type":"string",  "description":"Schema owner, e.g. dbo. Leave empty for the data source default."}
}',
    ToolDescription = 'Create a single AppTransaction (data model + form) from a pre-existing database table. MUST pass saasApplicationId from create_app_package.'
WHERE SkillKey = 'app-builder' AND ToolName = 'create_transaction_from_table';
GO

-- create_hierarchy_from_tables: add "required":true to saasApplicationId
UPDATE dbo.AppAgentToolRegister
SET ParameterSchemaJson = N'{
  "masterTableName":   {"type":"string",  "description":"Name of the root/master table (the top-level parent).", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.", "required":true},
  "childTableNames":   {"type":"string",  "description":"Comma-separated names of child tables, e.g. OrderLine,OrderPayment"},
  "transactionName":   {"type":"string",  "description":"Display name for the transaction. Defaults to master table name."},
  "schemaOwner":       {"type":"string",  "description":"Schema owner, e.g. dbo"},
  "grandChildMapJson": {"type":"string",  "description":"JSON object mapping each child table name to its grandchild table names. Format: {ChildTable:[GrandChild1,GrandChild2]}"}
}',
    ToolDescription = 'RECOVERY TOOL — Rebuild an AppTransaction hierarchy from tables that already exist in the database. MUST pass saasApplicationId from create_app_package.'
WHERE SkillKey = 'app-builder' AND ToolName = 'create_hierarchy_from_tables';
GO

-- add_search_to_menu: restore correct flat schema (V013 used wrong format and dropped isSavedSearch)
UPDATE dbo.AppAgentToolRegister
SET ParameterSchemaJson = N'{
  "searchId":          {"type":"integer", "description":"The ID of the search or saved search to add to the menu.", "required":true},
  "menuName":          {"type":"string",  "description":"Display label for the menu item, e.g. Employee List.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Places the item under the correct app in the sidebar.", "required":true},
  "isSavedSearch":     {"type":"boolean", "description":"Set true if searchId refers to a saved search; false for a regular search. Default false."}
}',
    ToolDescription = 'Step 6 — Add an existing search to the application navigation menu. MUST pass saasApplicationId from create_app_package so the item appears under the correct app.'
WHERE SkillKey = 'app-builder' AND ToolName = 'add_search_to_menu';
GO

-- create_search: saasApplicationId is already required=true in the seed, but strengthen the description
UPDATE dbo.AppAgentToolRegister
SET ParameterSchemaJson = N'{
  "name":              {"type":"string",  "description":"Display name for the search screen, e.g. Employee List.", "required":true},
  "sqlQuery":          {"type":"string",  "description":"SQL SELECT query that fetches the data to display.", "required":true},
  "saasApplicationId": {"type":"integer", "description":"REQUIRED — The SaasApplicationId returned by create_app_package. Never use an ID from explore_platform results.", "required":true}
}',
    ToolDescription = 'Create a search/list screen backed by a SQL query. MUST pass saasApplicationId from create_app_package. Returns SearchId, DataSetId, SearchViewId.'
WHERE SkillKey = 'app-builder' AND ToolName = 'create_search';
GO
