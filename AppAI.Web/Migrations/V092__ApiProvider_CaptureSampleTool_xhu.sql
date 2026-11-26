-- V092: api_cfg_capture_sample (UI Send Request) + SystemPrompt SOP for post-create multi_choice capture
-- Design: Document/AgentDesign/ApiProvider-AiAgent-Design.md

DECLARE @Lib nvarchar(100) = N'platform-api-integration-config';
DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.ApiIntegrationConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_capture_sample')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    @Lib,
    N'api_cfg_capture_sample',
    N'UI Send Request: live HTTP, persist response sample + schema. GET→JsonSampleData/JsonSchema; POST/PUT needs samplePayloadJson or JsonSampleData→PostResponseDto. After creating 3rd-party APIs: ask_user multi_choice then confirmed=true (id or idsJson). Not api_cfg_test_api.',
    N'{"type":"object","properties":{"settingParameterId":{"type":"integer"},"actionCode":{"type":"string"},"idsJson":{"type":"string","description":"JSON array of settingParameterIds"},"samplePayloadJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}}}',
    N'BuiltIn',
    N'{"TypeName":"' + @Plugin + N'","MethodName":"CaptureSample"}',
    1,
    39
);

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'UI Send Request: live HTTP, persist response sample + schema. GET→JsonSampleData/JsonSchema; POST/PUT needs samplePayloadJson or JsonSampleData→PostResponseDto. After creating 3rd-party APIs: ask_user multi_choice then confirmed=true (id or idsJson). Not api_cfg_test_api.',
    ParameterSchemaJson = N'{"type":"object","properties":{"settingParameterId":{"type":"integer"},"actionCode":{"type":"string"},"idsJson":{"type":"string","description":"JSON array of settingParameterIds"},"samplePayloadJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}}}',
    ToolType = N'BuiltIn',
    IsActive = 1,
    SortOrder = 39,
    ToolConfig = N'{"TypeName":"' + @Plugin + N'","MethodName":"CaptureSample"}'
WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_capture_sample';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Legacy/staging test (ExecuteOneOperationWithTestParameters). For 3rd-party Response/Payload structure use api_cfg_capture_sample (UI Send Request).'
WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_test_api';
GO

-- Keep ToolConfig CASE map in sync when V089-style refresh is re-run elsewhere; patch MethodName here too.
UPDATE dbo.AppAgentLibraryTool
SET ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiIntegrationConfigPlugin","MethodName":"CaptureSample"}'
WHERE LibraryKey = N'platform-api-integration-config' AND ToolName = N'api_cfg_capture_sample';
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
✓ After creating or updating 3rd-party HTTP APIs: ask_user mode=multi_choice listing each new/changed API (id=settingParameterId, display=ActionCode + method). Then api_cfg_capture_sample confirmed=true with idsJson=[selected ids] (or one settingParameterId). This is the UI Send Request — fills Response/Payload structure. POST/PUT: set JsonSampleData on upsert or pass samplePayloadJson
✓ Prefer api_cfg_capture_sample over api_cfg_test_api for Response/Payload structure
✓ For invoke/runtime calls: api-provider-detail → api-provider-execute (writes need confirmation)
✓ Secrets are write-only; never echo raw keys/tokens from get/list results
✗ Do not call execute_sql to discover AppSearch/Transaction meta — use list_available_searches / get_table_schema / ask_user
✗ Do not delete App API Provider (id=1) — delete individual APIs instead
✗ Do not use create_sql_query / create_excel_import / create_from_* for external REST endpoints
✗ Do not manage MCP grant/group admin matrices (out of scope)
✗ Do not claim success unless a tool returned ok=true (skipped=true on reload is OK)
✗ Do not skip capture_sample for 3rd-party APIs that need schema downstream unless the user declines the multi_choice (or selects none)

━━━ TYPICAL SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A) App Data Model APIs: ask transactionId → api_cfg_create_from_datamodel (preview) → ask_user → confirmed=true (omit appBaseUrl)
B) From Search: list_available_searches → api_cfg_create_from_search (omit appBaseUrl)
C) 3rd-party: list_providers → upsert_provider → confirmed → upsert_api(s) → ask_user multi_choice which APIs to Send Request → api_cfg_capture_sample confirmed=true idsJson=[…]
D) SP batch: api_cfg_list_sp_catalog → api_cfg_create_from_sp_batch
E) OpenAPI: openapi_list_selectable → openapi_upsert → openapi_regenerate → openapi_set_published
F) Call an API: api-provider-search → detail → (ask INPUT) → execute
G) request_api_mgmt_reload ONLY when HostContext.surface=api-management. In New Chat / no dock: skip (soft no-op).

When unsure, ask_user with concrete options.'
WHERE AgentCode = N'app-api-configurator';
GO
