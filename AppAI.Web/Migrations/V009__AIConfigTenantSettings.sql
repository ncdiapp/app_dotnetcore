-- V009__AIConfigTenantSettings.sql
-- Adds per-tenant LLM provider configuration rows to AppTenantSetting.
-- Each provider has its own API key so tenants can keep multiple providers configured.
-- AIConfigSettingBL reads these first; no appsettings.json fallback.
-- Idempotent: skips INSERT if the row already exists.
--
-- SetupCode prefix: AIConfig
-- Enum range: EmTenantSettings 3201–3207 (AppEnums.cs)
-- UsageType: 2=Text, 4=Select, 5=Password/Secret

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigProvider', 'Gemini', 'Active LLM Provider (OpenAI / Gemini / Anthropic)', 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigOpenAIApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigOpenAIApiKey', '', 'OpenAI API Key', 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigGeminiApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigGeminiApiKey', '', 'Gemini API Key', 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigAnthropicApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigAnthropicApiKey', '', 'Anthropic API Key', 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigOpenAIModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigOpenAIModel', 'gpt-4o', 'OpenAI Model Name', 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigGeminiModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigGeminiModel', 'gemini-2.0-flash', 'Gemini Model Name', 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigAnthropicModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType)
    VALUES ('AIConfigAnthropicModel', 'claude-3-5-sonnet-20241022', 'Anthropic Model Name', 4);
