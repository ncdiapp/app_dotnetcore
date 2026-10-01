-- TENANT upgrade — NOT a Flyway migration.
-- Run once on tenants that already have PLM Integration agents, after Flyway V040
-- (platform BuiltIn catalog tools) has been applied.
-- New tenants: prefer RUN_ALL seeds only; this script is for existing DBs.

-- Domain: plm-integration
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolDomain WHERE DomainKey = N'plm-integration')
INSERT INTO dbo.AppAgentToolDomain (DomainKey, DomainName, Description, SortOrder, IsActive)
VALUES (
    N'plm-integration',
    N'PLM Integration',
    N'PLM data import / migration agent libraries (ExternalDll and related).',
    5,
    1
);
GO

-- Move library into plm-integration when present
IF EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
UPDATE dbo.AppAgentToolLibrary
SET DomainKey = N'plm-integration',
    LibraryName = N'PLM Integration Import',
    Description = N'PLM Data Import tools (session/wizard, Entity/Image/Folder/Color/POM/DW/Search). Catalog list/test tools: platform-database / platform-application.'
WHERE LibraryKey = N'integration-plm-import';
GO

-- Remove PLM ExternalDll duplicates (platform BuiltIns own these names)
DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'integration-plm-import'
  AND ToolName IN (
      N'list_tenant_data_sources',
      N'list_tenant_saas_applications',
      N'test_plm_connection',
      N'test_data_source_connection'
  );
GO

-- Rename tool in existing agent prompts
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt, N'test_plm_connection', N'test_data_source_connection')
WHERE SystemPrompt LIKE N'%test_plm_connection%';
GO

-- Orchestrator subscriptions to platform catalog libraries
IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-orchestrator')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-database')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-orchestrator' AND LibraryKey = N'platform-database')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-orchestrator', N'platform-database');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-orchestrator')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-application')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-orchestrator' AND LibraryKey = N'platform-application')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-orchestrator', N'platform-application');
GO
