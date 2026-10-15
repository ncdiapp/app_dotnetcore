-- MCP document flag, plus API hint and usage notes.
-- Existing rows stay NULL. No default.
IF COL_LENGTH('dbo.AppOpenApiDocument', 'IsUsedByMcp') IS NULL
    ALTER TABLE dbo.AppOpenApiDocument ADD IsUsedByMcp BIT NULL;

IF COL_LENGTH('dbo.AppIntergrationSettingParameter', 'McpUiHint') IS NULL
    ALTER TABLE dbo.AppIntergrationSettingParameter ADD McpUiHint NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.AppIntergrationSettingParameter', 'McpUsageNotes') IS NULL
    ALTER TABLE dbo.AppIntergrationSettingParameter ADD McpUsageNotes NVARCHAR(MAX) NULL;
