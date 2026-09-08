-- V020: Platform Built-in Library
-- Adds a shared 'platform' library with two BuiltIn tools that replace the old
-- InjectSchema (bit 32) and InjectMemory (bit 16) capability flag behaviours.
--
-- Before: engine injected DB schema / memory into system prompt at startup unconditionally.
-- After:  agent calls get_database_schema or load_memory_context on demand as BuiltIn tools.
-- Agents subscribe to this library via AppAgentLibrarySubscription.

-- ─────────────────────────────────────────────────────────────────────────────
-- Library entry in the 'platform' domain (already seeded in V018)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = 'platform-builtins')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    'platform-builtins',
    'platform',
    'Platform Built-in Tools',
    'Core BuiltIn tools available to any agent: live DB schema retrieval and AppBuilder memory context. Subscribe to this library to give an agent database awareness and cross-session memory recall.',
    'BuiltIn',
    1
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Tool 1: get_database_schema
-- Replaces CapabilityFlags bit 32 (InjectSchema).
-- Plugin: App.BL.AIAgent.GenericAgent.Plugins.SchemaContextPlugin.GetDatabaseSchema
-- Constructor: int? dataSourceId — injected by BuiltInToolExecutor from context.DataSourceId
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey = 'platform-builtins' AND ToolName = 'get_database_schema')
INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'platform-builtins',
    'get_database_schema',
    'Call this tool to retrieve the list of all database tables and their columns in the connected tenant database. Use it when the user asks about data structure, before writing any SQL, or when you need to find the right table or column name for a query. Do not guess table or column names — always call this first.',
    '{"properties":{},"required":[]}',
    'BuiltIn',
    '{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.SchemaContextPlugin","MethodName":"GetDatabaseSchema"}',
    1
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Tool 2: load_memory_context
-- Replaces CapabilityFlags bit 16 (InjectMemory) which was never wired.
-- Companion to search_memory (keyword search); this returns the full memory dump.
-- Plugin: App.BL.AIAgent.GenericAgent.Plugins.MemoryContextPlugin.LoadMemoryContext
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolRegister WHERE SkillKey = 'platform-builtins' AND ToolName = 'load_memory_context')
INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'platform-builtins',
    'load_memory_context',
    'Call this tool at the start of a session to recall what was built in previous sessions: existing tables, transactions, and prior user requests. Use it when the user references prior work or asks what currently exists in the platform. For targeted recall by keyword, use search_memory instead.',
    '{"properties":{},"required":[]}',
    'BuiltIn',
    '{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.MemoryContextPlugin","MethodName":"LoadMemoryContext"}',
    1
);
