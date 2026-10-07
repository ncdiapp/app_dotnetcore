-- V045: platform-api-provider library + tools
-- Tools: api-provider-search | api-provider-detail | api-provider-execute
-- SOP: search → detail → (ask_user if write) → execute
-- Covers App API Provider (Id=1) + all 3rd-party API providers in API Management.
-- Idempotent.

-- ── Library ──
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-api-provider')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-api-provider',
    N'platform',
    N'BuiltIn: API Provider (App + 3rd-party)',
    N'Search, inspect, and execute published APIs from API Management (App API Provider and third-party providers). Prefer api-provider-search → api-provider-detail → api-provider-execute. For writes, confirm with the user (ask_user / confirmed=true). Stored-procedure tools on platform-database remain available when subscribed — use whichever library the skill references.',
    N'BuiltIn',
    1
);
GO

UPDATE dbo.AppAgentToolLibrary
SET LibraryName = N'BuiltIn: API Provider (App + 3rd-party)',
    Description = N'Search, inspect, and execute published APIs from API Management (App API Provider and third-party providers). Prefer api-provider-search → api-provider-detail → api-provider-execute. For writes, confirm with the user (ask_user / confirmed=true). Stored-procedure tools on platform-database remain available when subscribed — use whichever library the skill references.',
    ToolCategory = N'BuiltIn',
    IsActive = 1,
    DomainKey = N'platform'
WHERE LibraryKey = N'platform-api-provider';
GO

-- api-provider-search
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-api-provider' AND ToolName = N'api-provider-search')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-api-provider',
    N'api-provider-search',
    N'Search published APIs in API Management (App API Provider + all 3rd-party). Matches ActionCode, description, provider name, input parameter names, and sample/output keys. Required: query (user need / API name / business term). Optional providerName to filter when the user names a provider. Then call api-provider-detail with actionCode.',
    N'{"type":"object","properties":{"query":{"type":"string","description":"Keyword: API name, description term, parameter, or output field"},"providerName":{"type":"string","description":"Optional provider name filter when user specifies a provider"},"take":{"type":"integer","description":"Max hits (default 30, max 100)"}},"required":["query"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Search"}',
    1,
    10
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Search published APIs in API Management (App API Provider + all 3rd-party). Matches ActionCode, description, provider name, input parameter names, and sample/output keys. Required: query (user need / API name / business term). Optional providerName to filter when the user names a provider. Then call api-provider-detail with actionCode.',
    ParameterSchemaJson = N'{"type":"object","properties":{"query":{"type":"string","description":"Keyword: API name, description term, parameter, or output field"},"providerName":{"type":"string","description":"Optional provider name filter when user specifies a provider"},"take":{"type":"integer","description":"Max hits (default 30, max 100)"}},"required":["query"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Search"}',
    IsActive = 1,
    SortOrder = 10
WHERE LibraryKey = N'platform-api-provider' AND ToolName = N'api-provider-search';
GO

-- api-provider-detail
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-api-provider' AND ToolName = N'api-provider-detail')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-api-provider',
    N'api-provider-detail',
    N'Get one published API by actionCode: description, HTTP method, URL, input parameters (with defaults), sanitized config (no credentials), sample response, schema. Check requiresUserConfirm before execute. Required: actionCode from api-provider-search.',
    N'{"type":"object","properties":{"actionCode":{"type":"string","description":"API ActionCode (route key under /webapi/DataIntegration/{actionCode})"}},"required":["actionCode"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Detail"}',
    1,
    11
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Get one published API by actionCode: description, HTTP method, URL, input parameters (with defaults), sanitized config (no credentials), sample response, schema. Check requiresUserConfirm before execute. Required: actionCode from api-provider-search.',
    ParameterSchemaJson = N'{"type":"object","properties":{"actionCode":{"type":"string","description":"API ActionCode (route key under /webapi/DataIntegration/{actionCode})"}},"required":["actionCode"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Detail"}',
    IsActive = 1,
    SortOrder = 11
WHERE LibraryKey = N'platform-api-provider' AND ToolName = N'api-provider-detail';
GO

DECLARE @execDesc NVARCHAR(MAX) = N'Execute a published API via DataIntegration (same as API Management Send Request). Required: actionCode. Pass bodyJson as the POST JSON payload (object string), e.g. {"args":{"tabid":3992}} or a flat args object. Read APIs run immediately. Write/mutating APIs require user confirmation: Interactive agents trigger ask_user automatically; or pass confirmed=true after the user approved. Do not invent credentials — platform uses stored provider config. Returns response JSON (may be truncated).';
DECLARE @execSchema NVARCHAR(MAX) = N'{"type":"object","properties":{"actionCode":{"type":"string","description":"API ActionCode"},"bodyJson":{"type":"string","description":"JSON object payload for POST (or query fields for GET), e.g. {\"args\":{\"tabid\":1}}"},"confirmed":{"type":"boolean","description":"Required true for write APIs after user approval (Deterministic), or to skip ask_user when already approved"}},"required":["actionCode"]}';
DECLARE @execCfg NVARCHAR(MAX) = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Execute"}';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-api-provider' AND ToolName = N'api-provider-execute')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-api-provider',
    N'api-provider-execute',
    @execDesc,
    @execSchema,
    N'BuiltIn',
    @execCfg,
    1,
    12
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Execute a published API via DataIntegration (same as API Management Send Request). Required: actionCode. Pass bodyJson as the POST JSON payload (object string), e.g. {"args":{"tabid":3992}} or a flat args object. Read APIs run immediately. Write/mutating APIs require user confirmation: Interactive agents trigger ask_user automatically; or pass confirmed=true after the user approved. Do not invent credentials — platform uses stored provider config. Returns response JSON (may be truncated).',
    ParameterSchemaJson = N'{"type":"object","properties":{"actionCode":{"type":"string","description":"API ActionCode"},"bodyJson":{"type":"string","description":"JSON object payload for POST (or query fields for GET), e.g. {\"args\":{\"tabid\":1}}"},"confirmed":{"type":"boolean","description":"Required true for write APIs after user approval (Deterministic), or to skip ask_user when already approved"}},"required":["actionCode"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Execute"}',
    IsActive = 1,
    SortOrder = 12
WHERE LibraryKey = N'platform-api-provider' AND ToolName = N'api-provider-execute';
GO
