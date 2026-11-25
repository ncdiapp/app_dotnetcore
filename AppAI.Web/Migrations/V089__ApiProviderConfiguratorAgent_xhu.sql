-- V089: API Provider Configurator Agent (app-api-configurator)
-- Library: platform-api-integration-config (FULL CRUD for API MANAGEMENT + request_api_mgmt_reload)
-- Subscribes: platform-api-integration-config + platform-api-provider
--            + platform-database / platform-application when present (data sources / apps)
-- IF NOT EXISTS only. Re-run does not overwrite prompt or subscriptions.
-- Design: Document/AgentDesign/ApiProvider-AiAgent-Design.md

-- ── Library ──
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-api-integration-config')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-api-integration-config',
    N'platform',
    N'BuiltIn: API Management Config',
    N'Configure App API Provider, 3rd-party API providers, and OpenAPI documents. Durable writes require ask_user then confirmed=true. Runtime invoke stays on platform-api-provider.',
    N'BuiltIn',
    1
);
GO

UPDATE dbo.AppAgentToolLibrary
SET LibraryName = N'BuiltIn: API Management Config',
    Description = N'Configure App API Provider, 3rd-party API providers, and OpenAPI documents. Durable writes require ask_user then confirmed=true. Runtime invoke stays on platform-api-provider.',
    ToolCategory = N'BuiltIn',
    IsActive = 1,
    DomainKey = N'platform'
WHERE LibraryKey = N'platform-api-integration-config';
GO

DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.ApiIntegrationConfigPlugin';
DECLARE @Lib nvarchar(100) = N'platform-api-integration-config';

-- Helper pattern: insert if missing, then refresh description/schema/config (idempotent update)
-- Providers
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_list_providers')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_list_providers',
    N'List App API Provider (id=1) and all 3rd-party providers. Secrets redacted.',
    N'{"type":"object","properties":{"includeApis":{"type":"boolean"},"includeAppBuiltIn":{"type":"boolean"}}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListProviders"}', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_get_provider')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_get_provider',
    N'Get one provider by providerId. Includes API list. Secrets redacted.',
    N'{"type":"object","properties":{"providerId":{"type":"integer"}},"required":["providerId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetProvider"}', 1, 11);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_upsert_provider')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_upsert_provider',
    N'Create/update 3rd-party provider. dtoJson: name, internalCode, restApiUrl, apicredentialConfig (write-only), environmentVariables. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"providerId":{"type":"integer"},"dtoJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["dtoJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"UpsertProvider"}', 1, 12);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_delete_provider')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_delete_provider',
    N'Delete 3rd-party provider. Forbidden for App provider id=1. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"providerId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["providerId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"DeleteProvider"}', 1, 13);

-- APIs
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_list_apis')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_list_apis',
    N'List API operations. Filter by providerId and/or query.',
    N'{"type":"object","properties":{"providerId":{"type":"integer"},"query":{"type":"string"},"take":{"type":"integer"}}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListApis"}', 1, 20);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_get_api')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_get_api',
    N'Get one API by settingParameterId or actionCode. Config sanitized.',
    N'{"type":"object","properties":{"settingParameterId":{"type":"integer"},"actionCode":{"type":"string"}}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetApi"}', 1, 21);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_upsert_api')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_upsert_api',
    N'Create/update Standard or 3rd-party HTTP API via dtoJson. Prefer typed create_* for Data Model/Search/SP. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"settingParameterId":{"type":"integer"},"dtoJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["dtoJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"UpsertApi"}', 1, 22);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_delete_api')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_delete_api',
    N'Delete one API by settingParameterId. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"settingParameterId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["settingParameterId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"DeleteApi"}', 1, 23);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_batch_delete_apis')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_batch_delete_apis',
    N'Batch delete APIs. idsJson = [1,2,3]. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"idsJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["idsJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"BatchDeleteApis"}', 1, 24);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_create_from_datamodel')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_create_from_datamodel',
    N'Ensure App Data Model GET/POST APIs for transactionId. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"transactionId":{"type":"integer"},"appBaseUrl":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["transactionId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateFromDataModel"}', 1, 30);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_create_from_search')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_create_from_search',
    N'Ensure App Report/View API for searchId. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"searchId":{"type":"integer"},"appBaseUrl":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["searchId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateFromSearch"}', 1, 31);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_create_sql_query')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_create_sql_query',
    N'Create/update SQL JSON Query API (IsSimpleQuery). dtoJson: actionCode, jsonQuery, dataSourceId, …. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"dtoJson":{"type":"string"},"settingParameterId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["dtoJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateSqlQuery"}', 1, 32);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_create_tx_simple_query')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_create_tx_simple_query',
    N'Generate Transaction Provide Simple Query API. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"transactionId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["transactionId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateTxSimpleQuery"}', 1, 33);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_list_sp_catalog')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_list_sp_catalog',
    N'List stored procedures for SP API builder on a data source.',
    N'{"type":"object","properties":{"dataSourceId":{"type":"integer"},"schema":{"type":"string"},"take":{"type":"integer"}},"required":["dataSourceId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListSpCatalog"}', 1, 34);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_create_from_sp_batch')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_create_from_sp_batch',
    N'Batch-create Stored Procedure APIs. requestJson = SpApiCreateRequest. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"requestJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["requestJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateFromSpBatch"}', 1, 35);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_create_excel_import')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_create_excel_import',
    N'Create/update Excel Import Update Data API. Requires apiConfig.excelDataImportDataSetId. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"dtoJson":{"type":"string"},"settingParameterId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["dtoJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"CreateExcelImport"}', 1, 36);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_test_api')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_test_api',
    N'Test API with saved test parameters (Send Request). Mutating: ask_user then confirmed=true. Or isSimulate=true.',
    N'{"type":"object","properties":{"settingParameterId":{"type":"integer"},"actionCode":{"type":"string"},"isSimulate":{"type":"boolean"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"TestApi"}', 1, 40);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'api_cfg_api_where_used')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'api_cfg_api_where_used',
    N'Find Transactions and Searches that reference an API.',
    N'{"type":"object","properties":{"settingParameterId":{"type":"integer"}},"required":["settingParameterId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ApiWhereUsed"}', 1, 41);

-- OpenAPI
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_list')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_list',
    N'List OpenAPI (Swagger) documents.',
    N'{"type":"object","properties":{}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiList"}', 1, 50);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_get')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_get',
    N'Get one OpenAPI document. includeJson for truncated OpenApiJson.',
    N'{"type":"object","properties":{"documentId":{"type":"integer"},"includeJson":{"type":"boolean"}}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiGet"}', 1, 51);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_list_selectable')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_list_selectable',
    N'List ActionCodes selectable for an OpenAPI document.',
    N'{"type":"object","properties":{"mcpOnly":{"type":"boolean"}}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiListSelectable"}', 1, 52);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_upsert')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_upsert',
    N'Create/update OpenAPI document. dtoJson: id?, name, code, version, description, isUsedByMcp, actionCodes[]. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"dtoJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["dtoJson"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiUpsert"}', 1, 53);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_delete')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_delete',
    N'Delete OpenAPI document. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"documentId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["documentId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiDelete"}', 1, 54);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_set_published')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_set_published',
    N'Publish or unpublish OpenAPI document. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"documentId":{"type":"integer"},"published":{"type":"boolean"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["documentId","published"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiSetPublished"}', 1, 55);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'openapi_regenerate')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'openapi_regenerate',
    N'Regenerate OpenAPI JSON from members. Preview → ask_user → confirmed=true.',
    N'{"type":"object","properties":{"documentId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"}},"required":["documentId"]}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenApiRegenerate"}', 1, 56);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = @Lib AND ToolName = N'request_api_mgmt_reload')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@Lib, N'request_api_mgmt_reload',
    N'After successful API Management writes, signal the host page to prompt Reload. Only works inside API MANAGEMENT Configurator dock.',
    N'{"type":"object","properties":{}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"RequestApiMgmtReload"}', 1, 60);
GO

-- Refresh ToolConfig TypeName on all tools in this library (safe re-run)
UPDATE dbo.AppAgentLibraryTool
SET ToolType = N'BuiltIn',
    IsActive = 1,
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiIntegrationConfigPlugin","MethodName":"' +
        CASE ToolName
            WHEN N'api_cfg_list_providers' THEN N'ListProviders'
            WHEN N'api_cfg_get_provider' THEN N'GetProvider'
            WHEN N'api_cfg_upsert_provider' THEN N'UpsertProvider'
            WHEN N'api_cfg_delete_provider' THEN N'DeleteProvider'
            WHEN N'api_cfg_list_apis' THEN N'ListApis'
            WHEN N'api_cfg_get_api' THEN N'GetApi'
            WHEN N'api_cfg_upsert_api' THEN N'UpsertApi'
            WHEN N'api_cfg_delete_api' THEN N'DeleteApi'
            WHEN N'api_cfg_batch_delete_apis' THEN N'BatchDeleteApis'
            WHEN N'api_cfg_create_from_datamodel' THEN N'CreateFromDataModel'
            WHEN N'api_cfg_create_from_search' THEN N'CreateFromSearch'
            WHEN N'api_cfg_create_sql_query' THEN N'CreateSqlQuery'
            WHEN N'api_cfg_create_tx_simple_query' THEN N'CreateTxSimpleQuery'
            WHEN N'api_cfg_list_sp_catalog' THEN N'ListSpCatalog'
            WHEN N'api_cfg_create_from_sp_batch' THEN N'CreateFromSpBatch'
            WHEN N'api_cfg_create_excel_import' THEN N'CreateExcelImport'
            WHEN N'api_cfg_test_api' THEN N'TestApi'
            WHEN N'api_cfg_api_where_used' THEN N'ApiWhereUsed'
            WHEN N'openapi_list' THEN N'OpenApiList'
            WHEN N'openapi_get' THEN N'OpenApiGet'
            WHEN N'openapi_list_selectable' THEN N'OpenApiListSelectable'
            WHEN N'openapi_upsert' THEN N'OpenApiUpsert'
            WHEN N'openapi_delete' THEN N'OpenApiDelete'
            WHEN N'openapi_set_published' THEN N'OpenApiSetPublished'
            WHEN N'openapi_regenerate' THEN N'OpenApiRegenerate'
            WHEN N'request_api_mgmt_reload' THEN N'RequestApiMgmtReload'
            ELSE N'ListProviders'
        END + N'"}'
WHERE LibraryKey = N'platform-api-integration-config';
GO

-- ── SkillSet ──
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-api-configurator')
INSERT INTO dbo.AppAgentSkillSet
    (AgentCode, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'app-api-configurator',
    N'API Provider Configurator Agent',
    N'Configures API MANAGEMENT: App API Provider, 3rd-party providers, and OpenAPI documents. Listed under AI Agent.',
    N'You are the API Provider Configurator Agent for API MANAGEMENT.

You configure:
1) App API Provider (built-in provider id=1) — Data Model, Search/View, SQL JSON Query, Stored Procedure, Excel Import APIs
2) 3rd Party API Providers — provider credentials + HTTP operations
3) OpenAPI (Swagger) Documents — members, regenerate, publish

━━━ RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Inspect first: api_cfg_list_* / api_cfg_get_* / openapi_* / api-provider-search
✓ Never invent ActionCodes, provider ids, or secrets — read tools or ask_user
✓ Every durable write: call tool without confirmed (preview) → ask_user → call again with confirmed=true
✓ Prefer typed create tools (api_cfg_create_from_datamodel / _from_search / _from_sp_batch / _create_sql_query / _create_excel_import) when the user names a source
✓ For invoke/runtime calls: api-provider-detail → api-provider-execute (writes need confirmation). Or api_cfg_test_api for editor test params
✓ Secrets are write-only; never echo raw keys/tokens from get/list results
✗ Do not delete App API Provider (id=1) — delete individual APIs instead
✗ Do not manage MCP grant/group admin matrices (out of scope)
✗ Do not claim success unless a tool returned ok=true

━━━ TYPICAL SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A) App Data Model APIs: ask transactionId → api_cfg_create_from_datamodel (preview) → ask_user → confirmed=true
B) From Search: api_cfg_create_from_search
C) 3rd-party: api_cfg_upsert_provider → api_cfg_upsert_api (method/url/headers)
D) SP batch: api_cfg_list_sp_catalog → api_cfg_create_from_sp_batch
E) OpenAPI: openapi_list_selectable → openapi_upsert → openapi_regenerate → openapi_set_published
F) Call an API: api-provider-search → detail → (ask INPUT) → execute
G) When hosted in an API MANAGEMENT Configurator dock, after successful durable writes call request_api_mgmt_reload so the page can Reload.

When unsure, ask_user with concrete options.',
    3, 1, 20, 1,
    80000, 60000, 8000, 10,
    40, N'Interactive', 1
);
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-api-configurator';

IF @AgentId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-api-integration-config')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-api-integration-config')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-api-integration-config');

IF @AgentId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-api-provider')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-api-provider')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-api-provider');

-- list_tenant_data_sources lives on platform-database; list_tenant_saas_applications on platform-application (no platform-tenant-catalog library).
IF @AgentId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-database')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-database')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-database');

IF @AgentId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-application')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-application')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-application');
GO
