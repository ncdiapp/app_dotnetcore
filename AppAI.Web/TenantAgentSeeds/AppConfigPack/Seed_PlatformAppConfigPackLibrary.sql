-- TENANT seed — NOT a Flyway migration.
-- Library: platform-app-config-pack (BuiltIn tools wrapping AppConfigPackBL)
-- Skill: app-config-pack-orchestrator (Interactive NL → JSON → Execute)
--
-- sqlcmd -S <server> -d <TenantDB> -E -i Seed_PlatformAppConfigPackLibrary.sql
-- then Seed_AppConfigPackOrchestrator.sql

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-app-config-pack')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-app-config-pack',
    N'platform',
    N'App Config Pack',
    N'BuiltIn tools to learn the App Config Pack JSON contract and validate/preview/execute packs into App Config (DDL/TX/Search/Menu).',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-app-config-pack' AND ToolName = N'get_app_config_pack_contract')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-app-config-pack',
    N'get_app_config_pack_contract',
    N'Return the fixed App Config Pack JSON contract (PROMPT.md). Call before drafting pack JSON from natural language. Optional section: overview|tables|transactions|searches|listedit|samples|pipeline|all.',
    N'{"type":"object","properties":{"section":{"type":"string","description":"overview|tables|transactions|searches|listedit|samples|pipeline|all"}}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AppConfigPackPlugin","MethodName":"GetAppConfigPackContract"}',
    1,
    10
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-app-config-pack' AND ToolName = N'validate_app_config_pack')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-app-config-pack',
    N'validate_app_config_pack',
    N'Validate App Config Pack JSON. Returns IsValid, Errors, Warnings. Fix errors before preview/execute.',
    N'{"type":"object","properties":{"packJson":{"type":"string","description":"Full AppConfigPack JSON"}},"required":["packJson"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AppConfigPackPlugin","MethodName":"ValidateAppConfigPack"}',
    1,
    20
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-app-config-pack' AND ToolName = N'preview_app_config_pack')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-app-config-pack',
    N'preview_app_config_pack',
    N'Preview what Execute would Insert/Update/Skip (tables, transactions, searches). Does not write. Pass optional saasApplicationId.',
    N'{"type":"object","properties":{"packJson":{"type":"string"},"saasApplicationId":{"type":"integer"}},"required":["packJson"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AppConfigPackPlugin","MethodName":"PreviewAppConfigPack"}',
    1,
    30
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-app-config-pack' AND ToolName = N'execute_app_config_pack')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-app-config-pack',
    N'execute_app_config_pack',
    N'Apply App Config Pack JSON (DDL + TX + Search + Menu). ALWAYS validate + preview + ask_user confirm before calling. Pass packJson and optional saasApplicationId.',
    N'{"type":"object","properties":{"packJson":{"type":"string"},"saasApplicationId":{"type":"integer"}},"required":["packJson"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AppConfigPackPlugin","MethodName":"ExecuteAppConfigPack"}',
    1,
    40
);
GO
