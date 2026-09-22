SET NOCOUNT ON;
PRINT '=== Libraries ===';
SELECT LibraryKey, IsActive FROM dbo.AppAgentToolLibrary
WHERE LibraryKey IN (
  N'platform-multi-agent', N'integration-plm-import',
  N'agent-files', N'agent-scripts', N'platform-database', N'platform-application',
  N'platform-transaction', N'platform-search', N'platform-memory')
ORDER BY LibraryKey;

PRINT '=== Gate-0 + wizard tools (must exist) ===';
SELECT ToolName, ToolType, IsActive FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'integration-plm-import'
  AND ToolName IN (
    N'list_tenant_data_sources', N'list_tenant_saas_applications', N'ensure_techpack_schema',
    N'test_plm_connection', N'save_plm_import_session',
    N'update_plm_wizard_progress', N'get_plm_wizard_progress')
ORDER BY ToolName;

PRINT '=== integration-plm-import tool count ===';
SELECT COUNT(*) AS ToolCount FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import';

PRINT '=== Agents ===';
SELECT SkillKey, DisplayName, ExecutionMode, IsActive,
       AllowAgentFirstTurn,
       LEN(SystemPrompt) AS PromptLen,
       CASE WHEN SystemPrompt LIKE N'%list_tenant_saas_applications%' THEN 1 ELSE 0 END AS HasSaasAppToolInPrompt,
       CASE WHEN SystemPrompt LIKE N'%WIZARD CATALOG%' THEN 1 ELSE 0 END AS HasWizardCatalog,
       CASE WHEN SystemPrompt LIKE N'%get_plm_wizard_progress%' THEN 1 ELSE 0 END AS HasWizardResume,
       CASE WHEN SystemPrompt LIKE N'%update_plm_wizard_progress%' THEN 1 ELSE 0 END AS HasWizardPersist
FROM dbo.AppAgentSkillSet
WHERE SkillKey IN (N'plm-integration-orchestrator', N'plm-integration-import-dw')
ORDER BY SkillKey;

PRINT '=== Subscriptions ===';
SELECT SkillKey, LibraryKey FROM dbo.AppAgentLibrarySubscription
WHERE SkillKey IN (N'plm-integration-orchestrator', N'plm-integration-import-dw')
ORDER BY SkillKey, LibraryKey;

PRINT '=== Shared context table ===';
SELECT CASE WHEN OBJECT_ID(N'dbo.AppAgentSharedContext', N'U') IS NULL THEN 'MISSING' ELSE 'OK' END AS AppAgentSharedContext;
GO
