-- V043: Soften get_database_schema tool description — prefer reuse within a chat;
-- platform also caches by DataSourceId and injects a session hint (AgentSchemaCacheBL).

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Retrieve tables and columns for a registered DataSource (cached ~1h per DataSourceId). Pass dataSourceId for PLM / plmDW / another DB; omit or 0 for session default. Call once per database when you do not yet know table/column names. If schema for this DataSource was already loaded earlier in this chat (see system hint), do NOT call again unless switching DB or the user asks to refresh. Prefer get_table_schema for a single known table.'
WHERE LibraryKey = N'platform-database'
  AND ToolName = N'get_database_schema';
GO

UPDATE dbo.AppAgentToolRegister
SET ToolDescription = N'Retrieve tables and columns for a registered DataSource (cached ~1h per DataSourceId). Pass dataSourceId for PLM / plmDW / another DB; omit or 0 for session default. Call once per database when you do not yet know table/column names. If schema for this DataSource was already loaded earlier in this chat (see system hint), do NOT call again unless switching DB or the user asks to refresh. Prefer get_table_schema for a single known table.'
WHERE ToolName = N'get_database_schema'
  AND ToolType = N'BuiltIn';
GO
