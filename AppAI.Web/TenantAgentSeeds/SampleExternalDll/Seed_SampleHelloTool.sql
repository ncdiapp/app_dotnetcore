-- TENANT / DEMO seed — NOT a schema migration.
-- Registers sample ExternalDll tool (APP.AgentPlugins.Sample.HelloTool).
-- Prerequisite: APP.AgentPlugins.Sample.dll is in {WebRoot}/AgentPlugins/
--
--   sqlcmd -S <server> -d <TenantDB> -E -i Seed_SampleHelloTool.sql

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'sample-agent-plugins')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'sample-agent-plugins',
    N'platform',
    N'Sample: ExternalDll Hello',
    N'Demo library for APP.AgentPlugins.Sample (IAgentTool). Safe to delete after verifying ExternalDll.',
    N'ExternalDll',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'sample-agent-plugins' AND ToolName = N'sample_hello')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'sample-agent-plugins',
    N'sample_hello',
    N'Sample ExternalDll tool. Returns a greeting JSON with tenant echo fields. Args: name (string).',
    N'{"type":"object","properties":{"name":{"type":"string","description":"Name to greet"}},"required":["name"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.Sample.dll","TypeName":"APP.AgentPlugins.Sample.HelloTool"}',
    1,
    10
);
GO

-- Optional: subscribe a demo agent if present (adjust SkillKey as needed).
-- IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'your-skill-key')
-- AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE SkillKey = N'your-skill-key' AND LibraryKey = N'sample-agent-plugins')
-- INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
-- VALUES (N'your-skill-key', N'sample-agent-plugins');
-- GO
