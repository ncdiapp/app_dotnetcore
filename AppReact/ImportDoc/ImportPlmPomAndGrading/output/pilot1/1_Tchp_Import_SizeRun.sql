-- =============================================================================
-- 1_Tchp_Import_SizeRun.sql  | RunId: pilot1 | Decisions: S-A, D6
-- Connect to: TenantDB_PLM27
-- Source: SourceERP (pdmEntity DataSourceFrom=2)
-- Filter: tblSizeRun.isVisibleInPLM = 1; rotates under those runs only
-- After MERGE: remount AppEntity SizeRun/SizeRunDetail → Tchp*; delete Tchp* entities
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

-- =============================================================================
-- AppEntityInfo: repoint legacy SizeRun / SizeRunDetail to Tchp* tables.
-- Do NOT create EntityCode TchpSizeRun / TchpSizeRunSize.
-- Leave ALLSIZES / Sizes / SizeBreakDown on ERP unchanged.
-- =============================================================================
DECLARE @TenantDsId int =
(
    SELECT TOP (1) e.DataSourceFrom
    FROM dbo.AppEntityInfo AS e
    WHERE e.EntityCode IN (N'TchpSizeRun', N'TchpSizeRunSize')
      AND e.DataSourceFrom IS NOT NULL
    ORDER BY CASE e.EntityCode WHEN N'TchpSizeRun' THEN 0 ELSE 1 END
);

IF @TenantDsId IS NULL
BEGIN
    SELECT TOP (1) @TenantDsId = r.DataSourceID
    FROM AppMasterDB.dbo.AppDataSourceRegister AS r
    WHERE r.DatabaseName = DB_NAME()
      AND ISNULL(r.IsCompanyMasterDB, 0) = 1
    ORDER BY r.DataSourceID;
END

IF @TenantDsId IS NULL
    THROW 50001, N'Cannot resolve tenant DataSourceFrom for SizeRun entity remapping.', 1;

DECLARE @SizeRunEntityId int =
    (SELECT TOP (1) EntityInfoID FROM dbo.AppEntityInfo WHERE EntityCode = N'SizeRun' ORDER BY EntityInfoID);
DECLARE @SizeRunDetailEntityId int =
    (SELECT TOP (1) EntityInfoID FROM dbo.AppEntityInfo WHERE EntityCode = N'SizeRunDetail' ORDER BY EntityInfoID);
DECLARE @TchpSizeRunEntityId int =
    (SELECT TOP (1) EntityInfoID FROM dbo.AppEntityInfo WHERE EntityCode = N'TchpSizeRun' ORDER BY EntityInfoID);
DECLARE @TchpSizeRunSizeEntityId int =
    (SELECT TOP (1) EntityInfoID FROM dbo.AppEntityInfo WHERE EntityCode = N'TchpSizeRunSize' ORDER BY EntityInfoID);

IF @SizeRunEntityId IS NULL OR @SizeRunDetailEntityId IS NULL
    THROW 50002, N'Legacy AppEntityInfo SizeRun / SizeRunDetail not found.', 1;

-- Retarget fields that were bound to the temporary Tchp* entities.
IF @TchpSizeRunEntityId IS NOT NULL
BEGIN
    UPDATE dbo.AppTransactionField
    SET EntityId = @SizeRunEntityId
    WHERE EntityId = @TchpSizeRunEntityId;
    PRINT N'Repointed AppTransactionField from TchpSizeRun → SizeRun. Rows=' + CAST(@@ROWCOUNT AS nvarchar(20));
END

IF @TchpSizeRunSizeEntityId IS NOT NULL
BEGIN
    UPDATE dbo.AppTransactionField
    SET EntityId = @SizeRunDetailEntityId
    WHERE EntityId = @TchpSizeRunSizeEntityId;
    PRINT N'Repointed AppTransactionField from TchpSizeRunSize → SizeRunDetail. Rows=' + CAST(@@ROWCOUNT AS nvarchar(20));
END

UPDATE dbo.AppEntityInfo
SET
    TableName = N'TchpSizeRun',
    IdentityField = N'SizeRunId',
    DisplayFiled1 = N'SizeRunCode',
    DisplayFiled2 = N'SizeRunName',
    DisplayFiled3 = NULL,
    DataSourceFrom = @TenantDsId,
    SchemaOwner = N'dbo',
    QueryText = NULL,
    AppModifiedDate = @Now
WHERE EntityInfoID = @SizeRunEntityId;
PRINT N'Updated AppEntityInfo SizeRun → TchpSizeRun. DataSourceFrom=' + CAST(@TenantDsId AS nvarchar(20));

UPDATE dbo.AppEntityInfo
SET
    TableName = N'TchpSizeRunSize',
    IdentityField = N'SizeRunSizeId',
    DisplayFiled1 = N'SizeLabel',
    DisplayFiled2 = NULL,
    DisplayFiled3 = NULL,
    DataSourceFrom = @TenantDsId,
    SchemaOwner = N'dbo',
    QueryText = NULL,
    -- Keep SortByField=SizeOrder (column exists on TchpSizeRunSize)
    AppModifiedDate = @Now
WHERE EntityInfoID = @SizeRunDetailEntityId;
PRINT N'Updated AppEntityInfo SizeRunDetail → TchpSizeRunSize. DataSourceFrom=' + CAST(@TenantDsId AS nvarchar(20));

-- Remove temporary Tchp* entity rows (after field retarget).
DELETE FROM dbo.AppEntityInfo
WHERE EntityCode IN (N'TchpSizeRun', N'TchpSizeRunSize');
PRINT N'Deleted AppEntityInfo TchpSizeRun / TchpSizeRunSize. Rows=' + CAST(@@ROWCOUNT AS nvarchar(20));

COMMIT TRAN;
PRINT '1_Tchp_Import_SizeRun.sql DONE';
GO
