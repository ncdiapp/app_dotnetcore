-- V065: remove the Skill-library columns and bind table added by the discarded V065.
-- Drops AppAISkill.OwnerUserId, Visibility, SourceType and dbo.AppAgentSkillSetSkill.
-- Idempotent: missing columns/table are skipped.
--
-- If a tenant already applied the OLD V065 (the ADD script), delete its tracking row
-- and run migrations again. Otherwise this file will not run:
--   DELETE FROM dbo._SchemaMigrations WHERE Version = N'V065__ReusableSkillLibrary_xhu';

IF OBJECT_ID(N'dbo.AppAgentSkillSetSkill', N'U') IS NOT NULL
    DROP TABLE dbo.AppAgentSkillSetSkill;
GO

DECLARE @sql nvarchar(max);

SELECT @sql = N'ALTER TABLE dbo.AppAISkill DROP CONSTRAINT ' + QUOTENAME(dc.name)
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = N'AppAISkill' AND c.name = N'Visibility';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

IF COL_LENGTH(N'dbo.AppAISkill', N'Visibility') IS NOT NULL
    ALTER TABLE dbo.AppAISkill DROP COLUMN Visibility;
GO

DECLARE @sql nvarchar(max);

SELECT @sql = N'ALTER TABLE dbo.AppAISkill DROP CONSTRAINT ' + QUOTENAME(dc.name)
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = N'AppAISkill' AND c.name = N'SourceType';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

IF COL_LENGTH(N'dbo.AppAISkill', N'SourceType') IS NOT NULL
    ALTER TABLE dbo.AppAISkill DROP COLUMN SourceType;
GO

DECLARE @sql nvarchar(max);

SELECT @sql = N'ALTER TABLE dbo.AppAISkill DROP CONSTRAINT ' + QUOTENAME(dc.name)
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = N'AppAISkill' AND c.name = N'OwnerUserId';
IF @sql IS NOT NULL EXEC sp_executesql @sql;

IF COL_LENGTH(N'dbo.AppAISkill', N'OwnerUserId') IS NOT NULL
    ALTER TABLE dbo.AppAISkill DROP COLUMN OwnerUserId;
GO
