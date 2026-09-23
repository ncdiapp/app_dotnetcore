-- V033: Add the encrypted per-tenant Google Document AI credential setting.
-- Required when each tenant uses a different Google service account.

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAICredentialJson')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAICredentialJson', N'', N'Server-side credential file path for this tenant, relative to Google:DocumentAI:CredentialRoot.', 1, N'Google Setting', N'Document AI');
