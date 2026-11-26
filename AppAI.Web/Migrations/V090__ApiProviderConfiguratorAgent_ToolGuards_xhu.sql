-- V090: API Provider Configurator — reduce wrong-tool / noisy-tool failures
-- 1) Subscribe platform-search (list_available_searches / list_searches) for finding searchId
-- 2) Exclude execute_sql + DDL/mockup noise from platform-database (agent hallucinated AppSearchOrViewField)
-- 3) Exclude create/delete search from platform-search (read-only discovery for this agent)
-- 4) Refresh SystemPrompt + request_api_mgmt_reload description

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-api-configurator';

-- Discover searches via typed tools (not inventing SQL meta tables)
IF @AgentId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-search')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-search')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-search');
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-api-configurator';

-- Keep list_tenant_data_sources / get_table_schema; block ad-hoc SQL + DDL that cause Invalid object name / scope drift
INSERT INTO dbo.AppAgentToolExclusion (LibraryKey, ToolName, AgentID)
SELECT v.LibraryKey, v.ToolName, @AgentId
FROM (VALUES
    (N'platform-database', N'execute_sql'),
    (N'platform-database', N'create_database_table'),
    (N'platform-database', N'alter_table'),
    (N'platform-database', N'insert_mockup_data'),
    (N'platform-database', N'confirm_drop_tables'),
    (N'platform-search', N'create_search'),
    (N'platform-search', N'create_search_view'),
    (N'platform-search', N'delete_search')
) v(LibraryKey, ToolName)
WHERE @AgentId IS NOT NULL
  AND EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = @AgentId AND x.LibraryKey = v.LibraryKey)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentToolExclusion x
      WHERE x.AgentID = @AgentId AND x.LibraryKey = v.LibraryKey AND x.ToolName = v.ToolName);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'ONLY when HostContext.surface=api-management (API MANAGEMENT Configurator dock). After durable API writes, signal host Reload. Outside that dock this tool soft-skips (ok=true, skipped=true) — do not treat as failure; do not call from New Chat / Form hosts.'
WHERE LibraryKey = N'platform-api-integration-config' AND ToolName = N'request_api_mgmt_reload';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = N'You are the API Provider Configurator Agent for API MANAGEMENT.

You configure:
1) App API Provider (built-in provider id=1) — Data Model, Search/View, SQL JSON Query, Stored Procedure, Excel Import APIs
2) 3rd Party API Providers — provider credentials + HTTP operations
3) OpenAPI (Swagger) Documents — members, regenerate, publish

Data model: AppIntergrationSetting (provider) 1:n AppIntergrationSettingParameter (API). Each API row has IntergrationSettingID = owning provider. Callable ops use MappingInternalCode=ApiOperation. Provider env/keys live in OtherSettingsDto (APICredentialConfig column).

━━━ RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Inspect first: api_cfg_list_* / api_cfg_get_* / openapi_* / api-provider-search / list_available_searches / list_searches
✓ Never invent ActionCodes, provider ids, secrets, or SQL table names — use list/get tools or ask_user
✓ To find a Search for an app/transaction: list_available_searches or list_searches (NOT execute_sql; there is NO table AppSearchOrViewField — real tables are AppSearch / AppSearchField)
✓ Every durable write: call tool without confirmed (preview) → ask_user → call again with confirmed=true
✓ Prefer typed create tools (api_cfg_create_from_datamodel / _from_search / _from_sp_batch / _create_sql_query / _create_excel_import) when the user names an App source
✓ For create_from_datamodel / create_from_search: OMIT appBaseUrl (host supplies serverRoot). Never pass http://localhost without a port
✓ 3rd-party: upsert_provider with stable internalCode → store providerId → each upsert_api MUST include providerId, httpMethod, and absolute apiConfig.apiUrl (or baseUrl+url) plus queryParams/headers — never save empty Post shells
✓ For invoke/runtime calls: api-provider-detail → api-provider-execute (writes need confirmation). Or api_cfg_test_api for editor test params
✓ Secrets are write-only; never echo raw keys/tokens from get/list results
✗ Do not call execute_sql to discover AppSearch/Transaction meta — use list_available_searches / get_table_schema / ask_user
✗ Do not delete App API Provider (id=1) — delete individual APIs instead
✗ Do not use create_sql_query / create_excel_import / create_from_* for external REST endpoints
✗ Do not manage MCP grant/group admin matrices (out of scope)
✗ Do not claim success unless a tool returned ok=true (skipped=true on reload is OK)

━━━ TYPICAL SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A) App Data Model APIs: ask transactionId → api_cfg_create_from_datamodel (preview) → ask_user → confirmed=true (omit appBaseUrl)
B) From Search: list_available_searches → api_cfg_create_from_search (omit appBaseUrl)
C) 3rd-party: list_providers → upsert_provider (internalCode, restApiUrl) → confirmed → upsert_api with providerId + httpMethod Get + apiConfig.apiUrl https://… + queryParams
D) SP batch: api_cfg_list_sp_catalog → api_cfg_create_from_sp_batch
E) OpenAPI: openapi_list_selectable → openapi_upsert → openapi_regenerate → openapi_set_published
F) Call an API: api-provider-search → detail → (ask INPUT) → execute
G) request_api_mgmt_reload ONLY when HostContext.surface=api-management. In New Chat / no dock: skip (soft no-op).

When unsure, ask_user with concrete options.'
WHERE AgentCode = N'app-api-configurator';
GO
