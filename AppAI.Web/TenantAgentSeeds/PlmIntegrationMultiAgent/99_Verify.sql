SET NOCOUNT ON;
PRINT '=== Libraries ===';
SELECT LibraryKey, IsActive FROM dbo.AppAgentToolLibrary
WHERE LibraryKey IN (
  N'platform-multi-agent', N'integration-plm-import',
  N'agent-files', N'agent-scripts', N'platform-database', N'platform-application',
  N'platform-transaction', N'platform-search', N'platform-memory')
ORDER BY LibraryKey;

PRINT '=== Gate-0 + wizard + path-based apply tools (must exist) ===';
SELECT ToolName, ToolType, IsActive FROM dbo.AppAgentLibraryTool
WHERE (
    LibraryKey = N'integration-plm-import'
    AND ToolName IN (
      N'list_tenant_data_sources', N'list_tenant_saas_applications', N'ensure_techpack_schema',
      N'test_plm_connection', N'save_plm_import_session',
      N'update_plm_wizard_progress', N'get_plm_wizard_progress',
      N'preview_dw_blueprint_from_file', N'execute_dw_blueprint_from_file',
      N'apply_agent_output_plan',
      N'preview_search_blueprint_config', N'execute_search_blueprint_config',
      N'preview_search_sibling_view', N'execute_search_sibling_view',
      N'preview_search_massupdate_view', N'execute_search_massupdate_view')
  )
  OR (LibraryKey = N'agent-files' AND ToolName = N'execute_agent_sql_file')
ORDER BY LibraryKey, ToolName;

PRINT '=== integration-plm-import tool count ===';
SELECT COUNT(*) AS ToolCount FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import';

PRINT '=== Agents (ROOT IsActive=1; children IsActive=0) ===';
SELECT SkillKey, DisplayName, ExecutionMode, IsActive,
       AllowAgentFirstTurn,
       LEN(SystemPrompt) AS PromptLen,
       CASE WHEN SystemPrompt LIKE N'%list_tenant_saas_applications%' THEN 1 ELSE 0 END AS HasSaasAppToolInPrompt,
       CASE WHEN SystemPrompt LIKE N'%WIZARD CATALOG%' THEN 1 ELSE 0 END AS HasWizardCatalog,
       CASE WHEN SystemPrompt LIKE N'%get_plm_wizard_progress%' THEN 1 ELSE 0 END AS HasWizardResume,
       CASE WHEN SystemPrompt LIKE N'%update_plm_wizard_progress%' THEN 1 ELSE 0 END AS HasWizardPersist,
       CASE WHEN SystemPrompt LIKE N'%Sibling SearchView%' THEN 1 ELSE 0 END AS HasLegacySiblingMenu,
       CASE WHEN SystemPrompt LIKE N'%plm-integration-entity%' THEN 1 ELSE 0 END AS HasEntityChild,
       CASE WHEN SystemPrompt LIKE N'%Never run those preview/execute tools on ROOT%' THEN 1 ELSE 0 END AS HasNoLocalFallback,
       CASE WHEN SystemPrompt LIKE N'%PHASE=APPLY%' THEN 1 ELSE 0 END AS HasPhaseApply,
       CASE WHEN SystemPrompt LIKE N'%pendingApplyIds%' THEN 1 ELSE 0 END AS HasPendingApply,
       CASE WHEN SystemPrompt LIKE N'%plm-integration-search%' THEN 1 ELSE 0 END AS HasSearchChild,
       CASE WHEN SystemPrompt LIKE N'%plm-integration-massupdate%' THEN 1 ELSE 0 END AS HasMassUpdateChild,
       CASE WHEN SystemPrompt LIKE N'%ROOT local until Wave 2%' THEN 1 ELSE 0 END AS HasLegacySearchLocal
FROM dbo.AppAgentSkillSet
WHERE SkillKey IN (
  N'plm-integration-orchestrator', N'plm-integration-import-dw',
  N'plm-integration-entity', N'plm-integration-folder', N'plm-integration-image',
  N'plm-integration-color', N'plm-integration-pom',
  N'plm-integration-search', N'plm-integration-massupdate')
ORDER BY SkillKey;

PRINT '=== Active flags (expect ROOT=1, children=0) ===';
SELECT SkillKey, IsActive FROM dbo.AppAgentSkillSet
WHERE SkillKey IN (
  N'plm-integration-orchestrator', N'plm-integration-import-dw',
  N'plm-integration-entity', N'plm-integration-folder', N'plm-integration-image',
  N'plm-integration-color', N'plm-integration-pom',
  N'plm-integration-search', N'plm-integration-massupdate')
ORDER BY CASE WHEN SkillKey = N'plm-integration-orchestrator' THEN 0 ELSE 1 END, SkillKey;

PRINT '=== Subscriptions ===';
SELECT SkillKey, LibraryKey FROM dbo.AppAgentLibrarySubscription
WHERE SkillKey IN (
  N'plm-integration-orchestrator', N'plm-integration-import-dw',
  N'plm-integration-entity', N'plm-integration-folder', N'plm-integration-image',
  N'plm-integration-color', N'plm-integration-pom',
  N'plm-integration-search', N'plm-integration-massupdate')
ORDER BY SkillKey, LibraryKey;

PRINT '=== Shared context table (platform; wizard durable scope = ChatSessionKey) ===';
SELECT CASE WHEN OBJECT_ID(N'dbo.AppAgentSharedContext', N'U') IS NULL THEN 'MISSING' ELSE 'OK' END AS AppAgentSharedContext;
GO
