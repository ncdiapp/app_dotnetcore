-- V024: Optional dataSourceId on read-only BuiltIn DB tools.
-- Empty/0 keeps session default (AgentToolContext.DataSourceId). Non-zero targets that DataSourceRegisterId.

-- execute_sql
UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Execute a SQL SELECT query against a registered DataSource. Only SELECT is allowed. Pass dataSourceId to target PLM / plmDW / another registered DB; omit or 0 to use the session default data source.',
    ParameterSchemaJson = N'{
  "sql":{"type":"string","description":"A SQL SELECT statement. Must start with SELECT.","required":true},
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}'
WHERE ToolName = N'execute_sql' AND ToolType = N'BuiltIn';

-- check_table_exists
UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Check whether a specific database table exists. Returns true/false. Pass dataSourceId to target a registered DataSource; omit or 0 for session default.',
    ParameterSchemaJson = N'{
  "tableName":{"type":"string","description":"Table name to check","required":true},
  "schemaOwner":{"type":"string","description":"Schema owner, default dbo"},
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}'
WHERE ToolName = N'check_table_exists' AND ToolType = N'BuiltIn';

-- get_table_schema
UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Get column definitions (name, data type, nullable, primary key) for a specific database table. Pass dataSourceId to target a registered DataSource; omit or 0 for session default.',
    ParameterSchemaJson = N'{
  "tableName":{"type":"string","description":"Table name to inspect","required":true},
  "schemaOwner":{"type":"string","description":"Schema owner, e.g. dbo"},
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}'
WHERE ToolName = N'get_table_schema' AND ToolType = N'BuiltIn';

-- get_database_tables
UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'List all tables and views in the target database. Pass dataSourceId to target a registered DataSource; omit or 0 for session default.',
    ParameterSchemaJson = N'{
  "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
}'
WHERE ToolName = N'get_database_tables' AND ToolType = N'BuiltIn';

-- get_database_schema (platform-builtins)
UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Call this tool to retrieve tables and columns for a registered DataSource. Pass dataSourceId for PLM / plmDW / another DB; omit or 0 to use the session default. Do not guess table or column names — call this (or get_table_schema) first.',
    ParameterSchemaJson = N'{
  "properties":{
    "dataSourceId":{"type":"integer","description":"Optional DataSourceRegisterId. Omit or 0 = session default data source."}
  },
  "required":[]
}'
WHERE ToolName = N'get_database_schema' AND ToolType = N'BuiltIn';
GO
