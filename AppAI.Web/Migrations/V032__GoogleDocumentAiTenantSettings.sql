-- V032: Tenant-level non-secret Google Document AI settings.
-- Do not store GOOGLE_APPLICATION_CREDENTIALS or service-account private keys here.
-- The credential is host-level ADC, Secret Manager, or an attached workload identity.

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAIProjectId')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAIProjectId', N'', N'Google Cloud project ID used by the tenant PDF extractor.', 1, N'Google Setting', N'Document AI');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAILocation')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAILocation', N'us', N'Document AI processor location.', 1, N'Google Setting', N'Document AI');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAIProcessorId')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAIProcessorId', N'', N'Document AI processor ID.', 1, N'Google Setting', N'Document AI');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAIBucket')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAIBucket', N'', N'Temporary Google Cloud Storage bucket used for Document AI batch processing.', 1, N'Google Setting', N'Document AI');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAIPollTimeoutMinutes')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAIPollTimeoutMinutes', N'90', N'Maximum Document AI batch polling time in minutes.', 1, N'Google Setting', N'Document AI');

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'GoogleDocumentAICredentialJson')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (N'GoogleDocumentAICredentialJson', N'', N'Encrypted Google service-account JSON for this tenant. Enter through Tenant Application Settings; the value is encrypted before persistence.', 1, N'Google Setting', N'Document AI');
