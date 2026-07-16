-- =============================================================================
-- 1_Tchp_Import_SizeRun.sql  | RunId: pilot1 | Decisions: S-A
-- Connect to: TenantDB_PLM27
-- Source: SourceERP (pdmEntity DataSourceFrom=2)
-- Filter: tblSizeRun.isVisibleInPLM = 1; rotates under those runs only
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpDb sysname = N'SourceERP';
DECLARE @Now datetime = GETDATE();

BEGIN TRAN;

DECLARE @sql nvarchar(max);

-- SizeRun
SET @sql = N'
SET IDENTITY_INSERT dbo.TchpSizeRun ON;

MERGE dbo.TchpSizeRun AS t
USING (
    SELECT
        sr.SizeRunId,
        LEFT(LTRIM(RTRIM(sr.SizeRunCode)), 50) AS SizeRunCode,
        LEFT(LTRIM(RTRIM(COALESCE(NULLIF(sr.Description, N''''), sr.SizeRunCode))), 100) AS SizeRunName,
        CAST(1 AS bit) AS IsActive
    FROM ' + QUOTENAME(@ErpDb) + N'.dbo.tblSizeRun AS sr
    WHERE ISNULL(sr.isVisibleInPLM, 1) = 1
) AS s
ON t.SizeRunId = s.SizeRunId
WHEN MATCHED THEN UPDATE SET
    SizeRunCode = s.SizeRunCode,
    SizeRunName = s.SizeRunName,
    IsActive = s.IsActive,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (SizeRunId, SizeRunCode, SizeRunName, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.SizeRunId, s.SizeRunCode, s.SizeRunName, s.IsActive, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpSizeRun OFF;
';
EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n1 int = (SELECT COUNT(*) FROM dbo.TchpSizeRun);
PRINT 'TchpSizeRun rows: ' + CAST(@n1 AS nvarchar(20));

-- SizeRunSize
SET @sql = N'
SET IDENTITY_INSERT dbo.TchpSizeRunSize ON;

MERGE dbo.TchpSizeRunSize AS t
USING (
    SELECT
        r.SizeRunRotateID AS SizeRunSizeId,
        r.SizeRunId,
        LEFT(LTRIM(RTRIM(COALESCE(NULLIF(r.SizeName, N''''), N''S'' + CAST(r.SizeRunRotateID AS nvarchar(20))))), 20) AS SizeLabel,
        ISNULL(r.SizeOrder, 0) AS SizeOrder,
        CAST(1 AS bit) AS IsActive
    FROM ' + QUOTENAME(@ErpDb) + N'.dbo.tblSizeRunRotate AS r
    INNER JOIN ' + QUOTENAME(@ErpDb) + N'.dbo.tblSizeRun AS sr
        ON sr.SizeRunId = r.SizeRunId
       AND ISNULL(sr.isVisibleInPLM, 1) = 1
) AS s
ON t.SizeRunSizeId = s.SizeRunSizeId
WHEN MATCHED THEN UPDATE SET
    SizeRunId = s.SizeRunId,
    SizeLabel = s.SizeLabel,
    SizeOrder = s.SizeOrder,
    IsActive = s.IsActive,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (SizeRunSizeId, SizeRunId, SizeLabel, SizeOrder, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.SizeRunSizeId, s.SizeRunId, s.SizeLabel, s.SizeOrder, s.IsActive, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpSizeRunSize OFF;
';
EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n2 int = (SELECT COUNT(*) FROM dbo.TchpSizeRunSize);
PRINT 'TchpSizeRunSize rows: ' + CAST(@n2 AS nvarchar(20));

COMMIT TRAN;
PRINT '1_Tchp_Import_SizeRun.sql DONE';
GO
