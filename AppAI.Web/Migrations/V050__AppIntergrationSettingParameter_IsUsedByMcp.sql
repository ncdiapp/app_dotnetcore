-- Nullable flag for a later MCP vs non-MCP response. Null means false until an editor saves the row.
-- Existing rows stay NULL. No default constraint.

IF COL_LENGTH(N'dbo.AppIntergrationSettingParameter', N'IsUsedByMcp') IS NULL
BEGIN
    ALTER TABLE dbo.AppIntergrationSettingParameter
        ADD IsUsedByMcp BIT NULL;
END
GO
