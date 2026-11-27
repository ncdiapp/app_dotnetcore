-- V094:
-- 1) Clarify api_cfg_upsert_api ToolDescription (PascalCase httpMethod, JSON object headers, {{Env.VarName}} syntax).
-- 2) Option C for app-configurator ONLY: subscribe platform-api-integration-config and exclude 19 non-core tools,
--    exposing only the 8 core API config tools:
--      api_cfg_list_providers, api_cfg_list_apis, api_cfg_get_api,
--      api_cfg_upsert_provider, api_cfg_upsert_api,
--      api_cfg_create_from_datamodel, api_cfg_create_from_search, api_cfg_capture_sample
-- 3) Append concise API Config guidance to app-configurator SystemPrompt (idempotent).

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create or update one API operation. dtoJson: actionCode, description|actionDescription (UI Description column), httpMethod (Get|Post|Put|Delete), providerId, apiConfig{apiUrl|baseUrl+url, method, headers:{"Key":"Val"}, queryParams:{"k":"v"}, pathParams:{"k":"v"}} (use {{Env.VarName}} for provider env vars), jsonSampleData. 3rd-party HTTP REQUIRES providerId (>1) AND absolute apiUrl (or baseUrl+url) AND httpMethod — empty shells are rejected. Prefer typed create_* for Data Model/Search/SP/Excel. Preview -> ask_user -> confirmed=true.'
WHERE LibraryKey = N'platform-api-integration-config' AND ToolName = N'api_cfg_upsert_api';
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-configurator';

IF @AgentId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-api-integration-config')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-api-integration-config')
BEGIN
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey)
    VALUES (@AgentId, N'platform-api-integration-config');
END
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-configurator';

-- Exclude 19 non-core tools so app-configurator only receives the 8 core API config tools
INSERT INTO dbo.AppAgentToolExclusion (LibraryKey, ToolName, AgentID)
SELECT N'platform-api-integration-config', v.ToolName, @AgentId
FROM (VALUES
    (N'api_cfg_api_where_used'),
    (N'api_cfg_batch_delete_apis'),
    (N'api_cfg_create_excel_import'),
    (N'api_cfg_create_from_sp_batch'),
    (N'api_cfg_create_sql_query'),
    (N'api_cfg_create_tx_simple_query'),
    (N'api_cfg_delete_api'),
    (N'api_cfg_delete_provider'),
    (N'api_cfg_get_provider'),
    (N'api_cfg_list_sp_catalog'),
    (N'api_cfg_test_api'),
    (N'openapi_delete'),
    (N'openapi_get'),
    (N'openapi_list'),
    (N'openapi_list_selectable'),
    (N'openapi_regenerate'),
    (N'openapi_set_published'),
    (N'openapi_upsert'),
    (N'request_api_mgmt_reload')
) v(ToolName)
WHERE @AgentId IS NOT NULL
  AND EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = @AgentId AND x.LibraryKey = N'platform-api-integration-config')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentToolExclusion x
      WHERE x.AgentID = @AgentId
        AND x.LibraryKey = N'platform-api-integration-config'
        AND x.ToolName = v.ToolName);
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ API CONFIG (CORE, V094) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
You can also configure App built-in and 3rd-party APIs directly when a form/trigger needs them:
- Inspect first: api_cfg_list_providers, api_cfg_list_apis, api_cfg_get_api
- Built-in App APIs (providerId=1): prefer api_cfg_create_from_datamodel / api_cfg_create_from_search
- 3rd-party APIs (providerId>1):
  1) api_cfg_upsert_provider (put tokens/secrets in environmentVariables, never hardcoded in URL)
  2) api_cfg_upsert_api with httpMethod in PascalCase (Get|Post|Put|Delete), apiConfig.apiUrl,
     apiConfig.headers as JSON object {"HeaderName":"{{Env.TokenVar}}"} (always {{Env.VarName}} prefix),
     and jsonSampleData for Post/Put payloads
  3) After saving a 3rd-party API, offer/run api_cfg_capture_sample(idsJson=[id], confirmed=true) to capture response sample & schema before wiring Data Trigger mappings
- Always preview -> ask_user -> confirmed=true for write tools.
'
WHERE AgentCode = N'app-configurator'
  AND (SystemPrompt IS NULL OR SystemPrompt NOT LIKE N'%API CONFIG (CORE, V094)%');
GO
