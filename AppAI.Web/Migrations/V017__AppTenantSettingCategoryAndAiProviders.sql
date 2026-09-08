-- V017__AppTenantSettingCategoryAndAiProviders.sql
-- 1) Add Category / SubCategory columns
-- 2) Copy Description → Category; map known numeric category codes to labels
-- 3) Set AI Config category + subcategories; rename AIConfigProvider → AIConfigDefaultProvider
-- 4) Insert ImageProcess / Integration providers + AIConfigCursorApiKey / AIConfigCursorModel / AIConfigCursorMcpPublicBaseUrl
-- Idempotent for re-runs on existing tenant DBs.

-- ============================================================
-- 1. Columns
-- ============================================================
IF COL_LENGTH('dbo.AppTenantSetting', 'Category') IS NULL
    ALTER TABLE dbo.AppTenantSetting ADD Category NVARCHAR(4000) NULL;

IF COL_LENGTH('dbo.AppTenantSetting', 'SubCategory') IS NULL
    ALTER TABLE dbo.AppTenantSetting ADD SubCategory NVARCHAR(4000) NULL;
GO

-- ============================================================
-- 2. Copy Description → Category (only where Category still empty)
-- ============================================================
UPDATE dbo.AppTenantSetting
SET Category = Description
WHERE Category IS NULL OR LTRIM(RTRIM(Category)) = '';
GO

-- Map EmAppApplicationSettingCategory numeric codes → human labels
UPDATE dbo.AppTenantSetting SET Category = N'Server Setting' WHERE LTRIM(RTRIM(Category)) = N'1';
UPDATE dbo.AppTenantSetting SET Category = N'File System' WHERE LTRIM(RTRIM(Category)) = N'2';
UPDATE dbo.AppTenantSetting SET Category = N'Email System' WHERE LTRIM(RTRIM(Category)) = N'3';
UPDATE dbo.AppTenantSetting SET Category = N'UI Layout' WHERE LTRIM(RTRIM(Category)) = N'4';
UPDATE dbo.AppTenantSetting SET Category = N'E Shop Page Setting' WHERE LTRIM(RTRIM(Category)) = N'5';
UPDATE dbo.AppTenantSetting SET Category = N'File Folder Setting' WHERE LTRIM(RTRIM(Category)) = N'6';
UPDATE dbo.AppTenantSetting SET Category = N'Mobile Setting' WHERE LTRIM(RTRIM(Category)) = N'8';
UPDATE dbo.AppTenantSetting SET Category = N'Google Setting' WHERE LTRIM(RTRIM(Category)) = N'9';
UPDATE dbo.AppTenantSetting SET Category = N'Calendar Setting By User Type' WHERE LTRIM(RTRIM(Category)) = N'10';
UPDATE dbo.AppTenantSetting SET Category = N'User Profile Setting By User Type' WHERE LTRIM(RTRIM(Category)) = N'11';
UPDATE dbo.AppTenantSetting SET Category = N'Security Filter Entity By User Type' WHERE LTRIM(RTRIM(Category)) = N'12';
UPDATE dbo.AppTenantSetting SET Category = N'Default Login Page By User Type' WHERE LTRIM(RTRIM(Category)) = N'13';
UPDATE dbo.AppTenantSetting SET Category = N'Partner User And Partner Mapping Relation' WHERE LTRIM(RTRIM(Category)) = N'14';
UPDATE dbo.AppTenantSetting SET Category = N'Transfer Partner User Register Info To Partner Extend Table Mapping' WHERE LTRIM(RTRIM(Category)) = N'15';
UPDATE dbo.AppTenantSetting SET Category = N'Figma' WHERE LTRIM(RTRIM(Category)) = N'16';
UPDATE dbo.AppTenantSetting SET Category = N'General Setting' WHERE LTRIM(RTRIM(Category)) = N'100';
GO

-- ============================================================
-- 3. Rename AIConfigProvider → AIConfigDefaultProvider
-- ============================================================
IF EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigProvider')
   AND NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigDefaultProvider')
BEGIN
    UPDATE dbo.AppTenantSetting
    SET SetupCode = 'AIConfigDefaultProvider'
    WHERE SetupCode = 'AIConfigProvider';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigProvider')
   AND EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigDefaultProvider')
BEGIN
    -- Prefer existing DefaultProvider row; drop legacy duplicate
    DELETE FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigProvider';
END
GO

-- Ensure DefaultProvider row exists
IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigDefaultProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigDefaultProvider', 'Gemini', 'Default LLM Provider (OpenAI / Gemini / Anthropic)', 4, N'AI Config', N'Provider Routing');
GO

-- ============================================================
-- 4. AI category / subcategory for existing + new AI keys
-- ============================================================
UPDATE dbo.AppTenantSetting
SET Category = N'AI Config', SubCategory = N'Provider Routing'
WHERE SetupCode IN (
    'AIConfigDefaultProvider',
    'AIConfigImageProcessProvider',
    'AIConfigIntegrationProvider'
);

UPDATE dbo.AppTenantSetting
SET Category = N'AI Config', SubCategory = N'OpenAI Settings'
WHERE SetupCode IN ('AIConfigOpenAIApiKey', 'AIConfigOpenAIModel');

UPDATE dbo.AppTenantSetting
SET Category = N'AI Config', SubCategory = N'Gemini Settings'
WHERE SetupCode IN ('AIConfigGeminiApiKey', 'AIConfigGeminiModel');

UPDATE dbo.AppTenantSetting
SET Category = N'AI Config', SubCategory = N'Anthropic Settings'
WHERE SetupCode IN ('AIConfigAnthropicApiKey', 'AIConfigAnthropicModel');

UPDATE dbo.AppTenantSetting
SET Category = N'AI Config', SubCategory = N'Cursor Settings'
WHERE SetupCode IN ('AIConfigCursorApiKey', 'AIConfigCursorModel', 'AIConfigCursorMcpPublicBaseUrl');
GO

-- Also regroup any leftover AIConfig* rows that still have free-text Description-as-category
UPDATE dbo.AppTenantSetting
SET Category = N'AI Config'
WHERE SetupCode LIKE 'AIConfig%'
  AND (Category IS NULL OR Category NOT IN (N'AI Config'));
GO

-- Normalize UsageType for provider keys to Text(4)
UPDATE dbo.AppTenantSetting SET UsageType = 4
WHERE SetupCode IN (
    'AIConfigDefaultProvider',
    'AIConfigImageProcessProvider',
    'AIConfigIntegrationProvider',
    'AIConfigCursorModel'
);
GO

-- ============================================================
-- 5. Insert new keys (new AIConfig* names only — no legacy Cursor* rename)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigImageProcessProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigImageProcessProvider', 'Gemini', 'Image / OCR / Vision LLM Provider (OpenAI / Gemini / Anthropic)', 4, N'AI Config', N'Provider Routing');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigIntegrationProvider')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigIntegrationProvider', 'Cursor', 'Integration engine (Cursor / Cloud / OpenAI / Gemini / Anthropic)', 4, N'AI Config', N'Provider Routing');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigCursorApiKey')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigCursorApiKey', 'crsr_b7cf4a429f1fbca6cbc31602763bb4c9d4122e05b67b3851bfd3b9a65056cd7f', 'Cursor API Key', 5, N'AI Config', N'Cursor Settings');

-- Seed previous appsettings default when row exists but value is empty
UPDATE dbo.AppTenantSetting
SET SetupValue = N'crsr_b7cf4a429f1fbca6cbc31602763bb4c9d4122e05b67b3851bfd3b9a65056cd7f',
    Category = N'AI Config',
    SubCategory = N'Cursor Settings'
WHERE SetupCode = 'AIConfigCursorApiKey'
  AND (SetupValue IS NULL OR LTRIM(RTRIM(SetupValue)) = '');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigCursorModel')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigCursorModel', 'auto', 'Cursor Model Id', 4, N'AI Config', N'Cursor Settings');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = 'AIConfigCursorMcpPublicBaseUrl')
    INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
    VALUES ('AIConfigCursorMcpPublicBaseUrl', 'https://intensive-dealers-spreading-nascar.trycloudflare.com/appai', 'Cursor MCP Public Base URL', 4, N'AI Config', N'Cursor Settings');
ELSE
    UPDATE dbo.AppTenantSetting
    SET Category = N'AI Config',
        SubCategory = N'Cursor Settings'
    WHERE SetupCode = 'AIConfigCursorMcpPublicBaseUrl'
      AND (Category IS NULL OR Category <> N'AI Config' OR SubCategory IS NULL OR SubCategory <> N'Cursor Settings');
GO
