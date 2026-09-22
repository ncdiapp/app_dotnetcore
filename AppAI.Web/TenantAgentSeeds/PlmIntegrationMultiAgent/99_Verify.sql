SET NOCOUNT ON;
PRINT '=== Libraries ===';
SELECT LibraryKey, IsActive FROM dbo.AppAgentToolLibrary
WHERE LibraryKey IN (
  N'platform-multi-agent', N'integration-plm-import',
  N'agent-files', N'agent-scripts', N'platform-database', N'platform-application',
  N'platform-transaction', N'platform-search', N'platform-memory')
ORDER BY LibraryKey;

PRINT '=== platform-multi-agent tools ===';
SELECT ToolName, ToolType, IsActive FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'platform-multi-agent' ORDER BY SortOrder, ToolName;

PRINT '=== integration-plm-import tool count ===';
SELECT COUNT(*) AS ToolCount FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import';

PRINT '=== Agents ===';
SELECT SkillKey, DisplayName, ExecutionMode, IsActive,
       AllowAgentFirstTurn,
       LEN(SystemPrompt) AS PromptLen
FROM dbo.AppAgentSkillSet
WHERE SkillKey IN (N'plm-integration-orchestrator', N'plm-integration-import-dw')
ORDER BY SkillKey;

PRINT '=== Subscriptions ===';
SELECT SkillKey, LibraryKey FROM dbo.AppAgentLibrarySubscription
WHERE SkillKey IN (N'plm-integration-orchestrator', N'plm-integration-import-dw')
ORDER BY SkillKey, LibraryKey;

PRINT '=== Shared context table ===';
SELECT CASE WHEN OBJECT_ID(N'dbo.AppAgentSharedContext', N'U') IS NULL THEN 'MISSING' ELSE 'OK' END AS AppAgentSharedContext;
