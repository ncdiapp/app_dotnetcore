-- =============================================================================
-- CopyTenantAppSetting.sql
-- TENANT utility — NOT a Flyway migration.
--
-- Run against the TARGET tenant DB. Copies ALL rows from dbo.AppTenantSetting
-- in the SOURCE tenant DB into the current DB (upsert by SetupCode).
--
-- PARAMETER: set @SrcDb to the source database name (e.g. TenantDB_PLM32).
--
-- Example (sqlcmd):
--   sqlcmd -S Server\Instance -d TenantDB_PLM34 -E -v SrcDb="TenantDB_PLM32" -i CopyTenantAppSetting.sql
-- Or edit @SrcDb below and run in SSMS on the target DB.
--
-- Notes:
--   - Cross-DB SELECT requires both DBs on the same SQL Server instance.
--   - Upserts SetupValue / Description / EntityId / UsageType / Category / SubCategory.
--   - Does NOT delete target keys that are missing on the source.
--   - Includes AI keys (AIConfig*ApiKey) — treat as sensitive.
-- =============================================================================

SET NOCOUNT ON;

-- >>> PARAMETER: source tenant database name <<<
-- SSMS: edit the value below, run this script while connected to the TARGET DB.
-- sqlcmd: sqlcmd -S Server -d TargetDb -E -v SrcDb="TenantDB_PLM32" -i CopyTenantAppSetting.sql
DECLARE @SrcDb SYSNAME = N'TenantDB_PLM32';

-- If launched with sqlcmd -v SrcDb="...", prefer that value.
IF N'$(SrcDb)' NOT LIKE N'$(%' AND LEN(LTRIM(RTRIM(N'$(SrcDb)'))) > 0
    SET @SrcDb = N'$(SrcDb)';

IF @SrcDb IS NULL OR LTRIM(RTRIM(@SrcDb)) = N''
BEGIN
    RAISERROR(N'@SrcDb is required. Set DECLARE @SrcDb or sqlcmd -v SrcDb="SourceTenantDb".', 16, 1);
    RETURN;
END;

IF DB_ID(@SrcDb) IS NULL
BEGIN
    RAISERROR(N'Source database [%s] does not exist on this server.', 16, 1, @SrcDb);
    RETURN;
END;

IF OBJECT_ID(N'dbo.AppTenantSetting', N'U') IS NULL
BEGIN
    RAISERROR(N'Target dbo.AppTenantSetting does not exist. Run structure migrate first.', 16, 1);
    RETURN;
END;

DECLARE @srcObj NVARCHAR(512) = QUOTENAME(@SrcDb) + N'.dbo.AppTenantSetting';
IF OBJECT_ID(@srcObj, N'U') IS NULL
BEGIN
    RAISERROR(N'Source [%s].dbo.AppTenantSetting does not exist.', 16, 1, @SrcDb);
    RETURN;
END;

PRINT N'Copy AppTenantSetting from [' + @SrcDb + N'] -> [' + DB_NAME() + N'] ...';

DECLARE @sql NVARCHAR(MAX) = N'
MERGE dbo.AppTenantSetting AS t
USING (
    SELECT
        s.SetupCode,
        s.SetupValue,
        s.Description,
        s.EntityId,
        s.UsageType,
        ' + CASE WHEN COL_LENGTH(@SrcDb + N'.dbo.AppTenantSetting', 'Category') IS NOT NULL
                 THEN N's.Category' ELSE N'CAST(NULL AS NVARCHAR(4000))' END + N' AS Category,
        ' + CASE WHEN COL_LENGTH(@SrcDb + N'.dbo.AppTenantSetting', 'SubCategory') IS NOT NULL
                 THEN N's.SubCategory' ELSE N'CAST(NULL AS NVARCHAR(4000))' END + N' AS SubCategory
    FROM ' + QUOTENAME(@SrcDb) + N'.dbo.AppTenantSetting AS s
) AS src
ON t.SetupCode = src.SetupCode
WHEN MATCHED THEN
    UPDATE SET
        t.SetupValue  = src.SetupValue,
        t.Description = src.Description,
        t.EntityId    = src.EntityId,
        t.UsageType   = src.UsageType'
+ CASE WHEN COL_LENGTH(N'dbo.AppTenantSetting', 'Category') IS NOT NULL
       THEN N',
        t.Category    = src.Category,
        t.SubCategory = src.SubCategory'
       ELSE N'' END
+ N'
WHEN NOT MATCHED BY TARGET THEN
    INSERT (SetupCode, SetupValue, Description, EntityId, UsageType'
+ CASE WHEN COL_LENGTH(N'dbo.AppTenantSetting', 'Category') IS NOT NULL
       THEN N', Category, SubCategory'
       ELSE N'' END
+ N')
    VALUES (src.SetupCode, src.SetupValue, src.Description, src.EntityId, src.UsageType'
+ CASE WHEN COL_LENGTH(N'dbo.AppTenantSetting', 'Category') IS NOT NULL
       THEN N', src.Category, src.SubCategory'
       ELSE N'' END
+ N');
';

EXEC sys.sp_executesql @sql;

DECLARE @cnt INT = (SELECT COUNT(*) FROM dbo.AppTenantSetting);
PRINT N'Done. Target AppTenantSetting row count = ' + CAST(@cnt AS NVARCHAR(20));

-- Spot-check AI settings (values masked)
SELECT SetupCode,
       CASE
           WHEN SetupCode LIKE N'%ApiKey%' AND NULLIF(LTRIM(RTRIM(SetupValue)), N'') IS NOT NULL
               THEN N'(set, len=' + CAST(LEN(SetupValue) AS NVARCHAR(10)) + N')'
           ELSE SetupValue
       END AS SetupValuePreview,
       Category, SubCategory
FROM dbo.AppTenantSetting
WHERE SetupCode LIKE N'AIConfig%'
ORDER BY SetupCode;
GO
