-- V081: Tenant switch for AI agent microphone / Whisper voice input.
-- Default SetupValue = 1 (enabled). Missing row is also treated as enabled in AIConfigSettingBL.

IF NOT EXISTS (SELECT 1 FROM dbo.AppTenantSetting WHERE SetupCode = N'AIConfigEnableVoiceInput')
INSERT INTO dbo.AppTenantSetting (SetupCode, SetupValue, Description, UsageType, Category, SubCategory)
VALUES (
    N'AIConfigEnableVoiceInput',
    N'1',
    N'Enable microphone voice input for AI agents (Whisper STT). Requires OpenAI API key.',
    4,
    N'AI Config',
    N'Voice Input'
);
GO
