-- LLM / Cursor / provider-routing rows for AppTenantSetting.
-- Prefer running AppAI.Web/Migrations/V017__AppTenantSettingCategoryAndAiProviders.sql
-- on existing tenant DBs (adds Category/SubCategory, renames provider, inserts Cursor keys).
-- This script is a manual fallback for greenfield / one-off tenant DBs.
--
-- Category = N'AI Settings'
-- UsageType: 4=Text, 5=Password/Secret

IF COL_LENGTH('dbo.AppTenantSetting', 'Category') IS NULL
    ALTER TABLE dbo.AppTenantSetting ADD Category NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.AppTenantSetting', 'SubCategory') IS NULL
    ALTER TABLE dbo.AppTenantSetting ADD SubCategory NVARCHAR(4000) NULL;
GO

IF EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigProvider')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigDefaultProvider')
    UPDATE dbo.AppTenantSetting SET SetupCode = 'AIConfigDefaultProvider' WHERE SetupCode = 'AIConfigProvider';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigDefaultProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigDefaultProvider', 'Gemini', 'Default LLM Provider (OpenAI / Gemini / Anthropic)', 4, N'AI Settings', N'Provider Routing');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigImageProcessProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigImageProcessProvider', 'Gemini', 'Image / OCR / Vision LLM Provider', 4, N'AI Settings', N'Provider Routing');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigIntegrationProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigIntegrationProvider', 'Cursor', 'Integration engine (Cursor / Cloud / OpenAI / Gemini / Anthropic)', 4, N'AI Settings', N'Provider Routing');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigOpenAIApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigOpenAIApiKey', '', 'OpenAI API Key', 5, N'AI Settings', N'OpenAI Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigGeminiApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigGeminiApiKey', '', 'Gemini API Key', 5, N'AI Settings', N'Gemini Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigAnthropicApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigAnthropicApiKey', '', 'Anthropic API Key', 5, N'AI Settings', N'Anthropic Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigOpenAIModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigOpenAIModel', 'gpt-4o', 'OpenAI Model Name', 4, N'AI Settings', N'OpenAI Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigGeminiModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigGeminiModel', 'gemini-2.0-flash', 'Gemini Model Name', 4, N'AI Settings', N'Gemini Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigAnthropicModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigAnthropicModel', 'claude-3-5-sonnet-20241022', 'Anthropic Model Name', 4, N'AI Settings', N'Anthropic Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigCursorApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigCursorApiKey', 'crsr_b7cf4a429f1fbca6cbc31602763bb4c9d4122e05b67b3851bfd3b9a65056cd7f', 'Cursor API Key', 5, N'AI Settings', N'Cursor Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigCursorModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigCursorModel', 'auto', 'Cursor Model Id', 4, N'AI Settings', N'Cursor Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigCursorMcpPublicBaseUrl')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigCursorMcpPublicBaseUrl', 'https://intensive-dealers-spreading-nascar.trycloudflare.com/appai', 'Cursor MCP Public Base URL', 4, N'AI Settings', N'Cursor Settings');
GO
