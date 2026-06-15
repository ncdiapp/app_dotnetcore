/*
  Import PLM SystemDefineTable entities ONLY (pdmEntity.EntityType = 1).

  Creates AppEntityInfo metadata only (EmAppEntityType.SystemDefineTable = 1).
  Does NOT create physical tables or copy row data — tables must already exist
  in each APP datasource database (see ExportSourceDbTablesToNewDatabase.sql).

  PLM source:
    pdmEntity (EntityType = 1, IsRelationEntity = 0)
    pdmUserDefineEntityColumn (PK + display column definitions)

  PLM DataSourceFrom -> APP DataSourceRegister.Id:
    1 PLM     -> @AppDataSourceFromId_PLM_1
    2 ERP     -> @AppDataSourceFromId_ERP_2
    3 DataWS  -> @AppDataSourceFromId_DataWS_3
    4 OtherEx -> @AppDataSourceFromId_OtherEx_4
    (Multiple PLM sources may map to the same APP register Id / same physical DB.)
    NULL / 5 / 6 / other -> skipped (preview warning, not inserted)

  Mapping:
    EntityCode      = original PLM EntityCode (truncate 100); Plm_ if duplicate in batch
    TableName       = PLM SysTableName (no Plm_entity_ prefix)
    SchemaOwner     = PLM SchemaOwner, default dbo when blank
    IdentityField   = IsPrimaryKey column SystemTableColumnName (exactly one required)
    DisplayFiled1-3 = UsedByDropDownList columns by DataRowSort (SystemTableColumnName)
    DataSourceFrom  = mapped APP DataSourceRegister Id
    IntegrationId   = PLM EntityID

  WHERE TO RUN:
    Same SQL Server instance: @PlmDb, @AppDb, @AppMasterDb, and all datasource DBs.
    Connect to @AppDb (recommended) or any DB on the server before Execute.

  AFTER INSERT:
    Recycle IIS app pool (schema cache is not refreshed by direct SQL import).
*/

SET NOCOUNT ON;

/*
  EXECUTION CHECKLIST (before F5):
    [1] Set @PlmDb, @AppDb, @AppMasterDb, four @AppDataSourceFromId_* parameters.
    [2] Confirm AppDataSourceRegister rows exist in @AppMasterDb for those four Ids.
    [3] Confirm physical tables exist in each datasource DatabaseName (preview validates).
    [4] First run: @ExecuteInsert = 0 (preview). Then set to 1 to insert.
    [5] After successful insert: recycle IIS app pool before testing in the app.
*/

-- =============================================================================
-- LAYER 1: USER PARAMETERS
-- =============================================================================
DECLARE @PlmDb                        sysname = N'SourcePLM';
DECLARE @AppDb                        sysname = N'TenantDB_PLM';      -- AppEntityInfo writes
DECLARE @AppMasterDb                  sysname = N'AppMasterDB';       -- read AppDataSourceRegister only
DECLARE @AppDataSourceFromId_PLM_1    int     = 56;
DECLARE @AppDataSourceFromId_ERP_2    int     = 56;
DECLARE @AppDataSourceFromId_DataWS_3 int     = 56;
DECLARE @AppDataSourceFromId_OtherEx_4 int    = 56;
DECLARE @SaasApplicationID            int     = 1;
DECLARE @ExecuteInsert                bit     = 0;   -- 0 = preview, 1 = insert
-- =============================================================================

-- =============================================================================
-- LAYER 2: SETUP
-- =============================================================================
DECLARE @PlmEntityTbl       nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmEntity';
DECLARE @PlmColTbl          nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmUserDefineEntityColumn';
DECLARE @AppEntityInfo      nvarchar(261) = QUOTENAME(@AppDb) + N'.dbo.AppEntityInfo';
DECLARE @AppDataSourceReg   nvarchar(261) = QUOTENAME(@AppMasterDb) + N'.dbo.AppDataSourceRegister';
DECLARE @AppDbQuoted        nvarchar(261) = QUOTENAME(@AppDb);
DECLARE @AppMasterDbQuoted  nvarchar(261) = QUOTENAME(@AppMasterDb);
DECLARE @Collation          sysname;
DECLARE @C                  nvarchar(128);
DECLARE @Sql                nvarchar(max);
DECLARE @ErrMsg             nvarchar(4000);
DECLARE @HasBlocker         bit = 0;
DECLARE @PlmDsFrom          int;
DECLARE @DsDbName           sysname;

IF @PlmDb IS NULL OR LTRIM(RTRIM(@PlmDb)) = N''
   OR @AppDb IS NULL OR LTRIM(RTRIM(@AppDb)) = N''
   OR @AppMasterDb IS NULL OR LTRIM(RTRIM(@AppMasterDb)) = N''
BEGIN
    RAISERROR('Set @PlmDb, @AppDb, and @AppMasterDb before running.', 16, 1);
    RETURN;
END;

SELECT @Collation = collation_name FROM sys.databases WHERE name = @AppDb;
IF @Collation IS NULL
BEGIN
    RAISERROR('APP database not found or collation could not be resolved.', 16, 1);
    RETURN;
END;
SET @C = N' COLLATE ' + @Collation;

-- Ensure AppEntityInfo.IntegrationId exists
SET @Sql = N'
IF NOT EXISTS (
    SELECT 1
    FROM ' + @AppDbQuoted + N'.sys.columns AS c
    INNER JOIN ' + @AppDbQuoted + N'.sys.tables AS t ON t.object_id = c.object_id
    INNER JOIN ' + @AppDbQuoted + N'.sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE s.name = N''dbo'' AND t.name = N''AppEntityInfo'' AND c.name = N''IntegrationId''
)
BEGIN
    ALTER TABLE ' + @AppEntityInfo + N' ADD IntegrationId int NULL;
END;
';
EXEC sys.sp_executesql @Sql;

-- Resolve PLM DataSourceFrom (1-4) -> APP register Id + physical database name (read master only)
-- Drop legacy temp name from earlier script versions (same SSMS session)
IF OBJECT_ID('tempdb..#AppDataSourceDbMap') IS NOT NULL DROP TABLE #AppDataSourceDbMap;
IF OBJECT_ID('tempdb..#PlmSysDsRegisterMap') IS NOT NULL DROP TABLE #PlmSysDsRegisterMap;
CREATE TABLE #PlmSysDsRegisterMap (
    PlmDataSourceFrom    int     NOT NULL PRIMARY KEY,   -- PLM 1=PLM, 2=ERP, 3=DataWS, 4=OtherEx
    DataSourceRegisterId int     NOT NULL,
    DatabaseName         sysname NULL,
    IsRegisterResolved   bit     NOT NULL DEFAULT (0)
);

INSERT INTO #PlmSysDsRegisterMap (PlmDataSourceFrom, DataSourceRegisterId)
VALUES
    (1, @AppDataSourceFromId_PLM_1),
    (2, @AppDataSourceFromId_ERP_2),
    (3, @AppDataSourceFromId_DataWS_3),
    (4, @AppDataSourceFromId_OtherEx_4);

SET @Sql = N'
UPDATE m
SET DatabaseName = r.DatabaseName,
    IsRegisterResolved = 1
FROM #PlmSysDsRegisterMap AS m
INNER JOIN ' + @AppDataSourceReg + N' AS r
    ON r.DataSourceId = m.DataSourceRegisterId;
';
EXEC sys.sp_executesql @Sql;

IF EXISTS (
    SELECT 1 FROM #PlmSysDsRegisterMap
    WHERE IsRegisterResolved = 0 OR DatabaseName IS NULL OR LTRIM(RTRIM(DatabaseName)) = N''
)
BEGIN
    RAISERROR(
        'One or more @AppDataSourceFromId_* values not found in @AppMasterDb.dbo.AppDataSourceRegister. Check preview #PlmSysDsRegisterMap.',
        16, 1
    );
    RETURN;
END;

-- =============================================================================
-- LAYER 3: STAGING
-- =============================================================================
IF OBJECT_ID('tempdb..#PlmSysEntityMap') IS NOT NULL DROP TABLE #PlmSysEntityMap;
IF OBJECT_ID('tempdb..#PlmSysColumnMap') IS NOT NULL DROP TABLE #PlmSysColumnMap;

CREATE TABLE #PlmSysEntityMap
(
    PlmEntityID        int           NOT NULL PRIMARY KEY,
    PlmEntityCode      nvarchar(200) NOT NULL,
    TargetEntityCode   nvarchar(100) NOT NULL,
    [Description]      nvarchar(500) NULL,
    TableName          nvarchar(100) NOT NULL,
    SchemaOwner        nvarchar(50)  NOT NULL,
    PlmDataSourceFrom  int           NULL,
    AppDataSourceFrom  int           NULL,
    TargetDatabaseName sysname       NULL,
    IdentityField      nvarchar(128) NULL,
    DisplayFiled1      nvarchar(100) NULL,
    DisplayFiled2      nvarchar(100) NULL,
    DisplayFiled3      nvarchar(100) NULL,
    PkColumnCount      int           NOT NULL DEFAULT (0),
    DisplayColumnCount int           NOT NULL DEFAULT (0),
    ImportStatus       nvarchar(30)  NOT NULL DEFAULT (N'Ready'),
    SkipReason         nvarchar(200) NULL,
    PhysicalTableOk    bit           NOT NULL DEFAULT (0)
);

CREATE TABLE #PlmSysColumnMap
(
    PlmEntityID              int           NOT NULL,
    UserDefineEntityColumnID int           NOT NULL,
    SystemTableColumnName    nvarchar(128) NOT NULL,
    IsPrimaryKey             bit           NOT NULL DEFAULT (0),
    UsedByDropDownList       bit           NOT NULL DEFAULT (0),
    ColOrdinal               int           NOT NULL,
    DisplayOrdinal           int           NULL,
    PRIMARY KEY (PlmEntityID, UserDefineEntityColumnID)
);

-- Stage PLM SystemDefineTable headers
SET @Sql = N'
INSERT INTO #PlmSysEntityMap (
    PlmEntityID, PlmEntityCode, TargetEntityCode, [Description],
    TableName, SchemaOwner, PlmDataSourceFrom, AppDataSourceFrom
)
SELECT
    e.EntityID,
    e.EntityCode' + @C + N',
    LEFT(e.EntityCode, 100)' + @C + N',
    LEFT(e.[Description], 500)' + @C + N',
    LEFT(LTRIM(RTRIM(e.SysTableName)), 100)' + @C + N',
    LEFT(ISNULL(NULLIF(LTRIM(RTRIM(e.SchemaOwner)), N''''), N''dbo''), 50)' + @C + N',
    e.DataSourceFrom,
    CASE e.DataSourceFrom
        WHEN 1 THEN @IdPlm
        WHEN 2 THEN @IdErp
        WHEN 3 THEN @IdDataWS
        WHEN 4 THEN @IdOtherEx
        ELSE NULL
    END
FROM ' + @PlmEntityTbl + N' AS e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0;
';
EXEC sys.sp_executesql @Sql,
    N'@IdPlm int, @IdErp int, @IdDataWS int, @IdOtherEx int',
    @IdPlm = @AppDataSourceFromId_PLM_1,
    @IdErp = @AppDataSourceFromId_ERP_2,
    @IdDataWS = @AppDataSourceFromId_DataWS_3,
    @IdOtherEx = @AppDataSourceFromId_OtherEx_4;

-- Mark skipped: unsupported / missing DataSourceFrom
UPDATE m
SET ImportStatus = N'Skipped',
    SkipReason = CASE
        WHEN m.PlmDataSourceFrom IS NULL THEN N'DataSourceFrom is NULL'
        WHEN m.PlmDataSourceFrom IN (5, 6) THEN N'RestJson/RestXML not supported'
        ELSE N'Unsupported DataSourceFrom value'
    END
FROM #PlmSysEntityMap AS m
WHERE m.AppDataSourceFrom IS NULL;

-- Mark skipped: empty SysTableName
UPDATE m
SET ImportStatus = N'Skipped',
    SkipReason = N'SysTableName is empty'
FROM #PlmSysEntityMap AS m
WHERE m.ImportStatus = N'Ready'
  AND (m.TableName IS NULL OR LTRIM(RTRIM(m.TableName)) = N'');

-- Resolve target database name per entity (by PLM DataSourceFrom, not register Id alone)
UPDATE m
SET TargetDatabaseName = d.DatabaseName
FROM #PlmSysEntityMap AS m
INNER JOIN #PlmSysDsRegisterMap AS d ON d.PlmDataSourceFrom = m.PlmDataSourceFrom
WHERE m.ImportStatus = N'Ready';

-- Plm_ prefix on duplicate EntityCode within batch (first row keeps original code)
;WITH dupCode AS (
    SELECT PlmEntityID, TargetEntityCode,
        ROW_NUMBER() OVER (PARTITION BY TargetEntityCode ORDER BY PlmEntityID) AS rn
    FROM #PlmSysEntityMap
    WHERE ImportStatus = N'Ready'
)
UPDATE m
SET TargetEntityCode = LEFT(N'Plm_' + m.TargetEntityCode, 100)
FROM #PlmSysEntityMap AS m
INNER JOIN dupCode AS d ON d.PlmEntityID = m.PlmEntityID
WHERE d.rn > 1;

-- Stage columns (all columns — IsSimpleColumn not filtered)
SET @Sql = N'
INSERT INTO #PlmSysColumnMap (
    PlmEntityID, UserDefineEntityColumnID, SystemTableColumnName,
    IsPrimaryKey, UsedByDropDownList, ColOrdinal
)
SELECT
    c.EntityID,
    c.UserDefineEntityColumnID,
    LEFT(LTRIM(RTRIM(c.SystemTableColumnName)), 128)' + @C + N',
    ISNULL(c.IsPrimaryKey, 0),
    ISNULL(c.UsedByDropDownList, 0),
    ROW_NUMBER() OVER (
        PARTITION BY c.EntityID
        ORDER BY ISNULL(c.DataRowSort, 9999), c.UserDefineEntityColumnID
    )
FROM ' + @PlmColTbl + N' AS c
INNER JOIN #PlmSysEntityMap AS m ON m.PlmEntityID = c.EntityID
WHERE m.ImportStatus = N''Ready'';
';
EXEC sys.sp_executesql @Sql;

-- Display ordinals (UsedByDropDownList, max 3)
;WITH disp AS (
    SELECT PlmEntityID, UserDefineEntityColumnID,
        ROW_NUMBER() OVER (PARTITION BY PlmEntityID ORDER BY ColOrdinal) AS dispOrd
    FROM #PlmSysColumnMap
    WHERE UsedByDropDownList = 1
)
UPDATE c SET DisplayOrdinal = d.dispOrd
FROM #PlmSysColumnMap AS c
INNER JOIN disp AS d
    ON d.PlmEntityID = c.PlmEntityID AND d.UserDefineEntityColumnID = c.UserDefineEntityColumnID
WHERE d.dispOrd <= 3;

-- PK / display counts and field names on entity map
UPDATE m
SET PkColumnCount = ISNULL(pk.Cnt, 0),
    IdentityField = pk.PkColumnName
FROM #PlmSysEntityMap AS m
OUTER APPLY (
    SELECT COUNT(*) AS Cnt, MAX(SystemTableColumnName) AS PkColumnName
    FROM #PlmSysColumnMap AS c
    WHERE c.PlmEntityID = m.PlmEntityID AND c.IsPrimaryKey = 1
) AS pk;

UPDATE m
SET DisplayColumnCount = ISNULL(dc.Cnt, 0),
    DisplayFiled1 = dc.D1,
    DisplayFiled2 = dc.D2,
    DisplayFiled3 = dc.D3
FROM #PlmSysEntityMap AS m
OUTER APPLY (
    SELECT
        COUNT(*) AS Cnt,
        MAX(CASE WHEN DisplayOrdinal = 1 THEN SystemTableColumnName END) AS D1,
        MAX(CASE WHEN DisplayOrdinal = 2 THEN SystemTableColumnName END) AS D2,
        MAX(CASE WHEN DisplayOrdinal = 3 THEN SystemTableColumnName END) AS D3
    FROM #PlmSysColumnMap AS c
    WHERE c.PlmEntityID = m.PlmEntityID AND c.UsedByDropDownList = 1
) AS dc;

-- Mark validation failures on Ready rows
UPDATE m
SET ImportStatus = N'Skipped',
    SkipReason = CASE
        WHEN m.PkColumnCount <> 1 THEN N'Need exactly one PK column (IsPrimaryKey)'
        WHEN m.DisplayColumnCount < 1 THEN N'Need at least one UsedByDropDownList column'
        WHEN m.IdentityField IS NULL OR LTRIM(RTRIM(m.IdentityField)) = N'' THEN N'PK SystemTableColumnName is empty'
        ELSE N'Column validation failed'
    END
FROM #PlmSysEntityMap AS m
WHERE m.ImportStatus = N'Ready'
  AND (m.PkColumnCount <> 1 OR m.DisplayColumnCount < 1
       OR m.IdentityField IS NULL OR LTRIM(RTRIM(m.IdentityField)) = N'');

-- Physical table existence — one pass per PLM DataSourceFrom (same DB may appear more than once)
DECLARE ds_validate_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT PlmDataSourceFrom, DatabaseName FROM #PlmSysDsRegisterMap;

OPEN ds_validate_cursor;
FETCH NEXT FROM ds_validate_cursor INTO @PlmDsFrom, @DsDbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @DsDbName)
    BEGIN
        SET @Sql = N'
        UPDATE m
        SET PhysicalTableOk = 1
        FROM #PlmSysEntityMap AS m
        INNER JOIN ' + QUOTENAME(@DsDbName) + N'.sys.tables AS t
            ON t.name = m.TableName' + @C + N'
        INNER JOIN ' + QUOTENAME(@DsDbName) + N'.sys.schemas AS s
            ON s.schema_id = t.schema_id
           AND s.name = m.SchemaOwner' + @C + N'
        WHERE m.ImportStatus = N''Ready''
          AND m.PlmDataSourceFrom = @PlmDsFrom;
        ';
        EXEC sys.sp_executesql @Sql, N'@PlmDsFrom int', @PlmDsFrom = @PlmDsFrom;
    END;

    FETCH NEXT FROM ds_validate_cursor INTO @PlmDsFrom, @DsDbName;
END;

CLOSE ds_validate_cursor;
DEALLOCATE ds_validate_cursor;

UPDATE m
SET ImportStatus = N'Skipped',
    SkipReason = N'Physical table not found in datasource database'
FROM #PlmSysEntityMap AS m
WHERE m.ImportStatus = N'Ready'
  AND m.PhysicalTableOk = 0;

-- =============================================================================
-- LAYER 4: PREVIEW
-- =============================================================================
SELECT PlmDataSourceFrom, DataSourceRegisterId, DatabaseName, IsRegisterResolved
FROM #PlmSysDsRegisterMap
ORDER BY PlmDataSourceFrom;

SELECT
    m.TargetEntityCode,
    m.TableName,
    c.ColOrdinal,
    c.SystemTableColumnName,
    c.IsPrimaryKey,
    c.UsedByDropDownList,
    c.DisplayOrdinal
FROM #PlmSysColumnMap AS c
INNER JOIN #PlmSysEntityMap AS m ON m.PlmEntityID = c.PlmEntityID
WHERE m.ImportStatus = N'Ready'
ORDER BY m.TargetEntityCode, c.ColOrdinal;

-- Skipped summary (counts by reason)
SELECT ImportStatus, SkipReason, COUNT(*) AS EntityCount
FROM #PlmSysEntityMap
WHERE ImportStatus = N'Skipped'
GROUP BY ImportStatus, SkipReason
ORDER BY EntityCount DESC;

-- Skipped entities (detail — review before insert)
SELECT
    m.PlmEntityID,
    m.PlmEntityCode,
    m.TargetEntityCode,
    m.[Description],
    m.TableName,
    m.SchemaOwner,
    m.PlmDataSourceFrom,
    m.AppDataSourceFrom,
    m.TargetDatabaseName,
    m.PkColumnCount,
    m.DisplayColumnCount,
    m.IdentityField,
    m.DisplayFiled1,
    m.DisplayFiled2,
    m.DisplayFiled3,
    m.SkipReason
FROM #PlmSysEntityMap AS m
WHERE m.ImportStatus = N'Skipped'
ORDER BY m.SkipReason, m.PlmEntityCode;

-- Ready entities (will be inserted when @ExecuteInsert = 1)
SELECT
    m.PlmEntityID,
    m.PlmEntityCode,
    m.TargetEntityCode,
    m.[Description],
    m.TableName,
    m.SchemaOwner,
    m.PlmDataSourceFrom,
    m.AppDataSourceFrom,
    m.TargetDatabaseName,
    m.IdentityField,
    m.DisplayFiled1,
    m.DisplayFiled2,
    m.DisplayFiled3
FROM #PlmSysEntityMap AS m
WHERE m.ImportStatus = N'Ready'
ORDER BY m.TargetEntityCode;

DECLARE @ReadyCount int = (SELECT COUNT(*) FROM #PlmSysEntityMap WHERE ImportStatus = N'Ready');
DECLARE @SkippedCount int = (SELECT COUNT(*) FROM #PlmSysEntityMap WHERE ImportStatus = N'Skipped');
PRINT N'--- Import summary ---';
PRINT N'Ready to import: ' + CAST(@ReadyCount AS nvarchar(20));
PRINT N'Skipped:        ' + CAST(@SkippedCount AS nvarchar(20));
IF @SkippedCount > 0
    PRINT N'See skipped-entity detail result set above (SkipReason column).';

-- Blockers (non-empty = do not insert until fixed)
SET @Sql = N'
SELECT m.PlmEntityID, m.TargetEntityCode, m.TableName, m.TargetDatabaseName,
    N''EntityCode already exists in AppEntityInfo'' AS Issue
FROM #PlmSysEntityMap AS m
WHERE m.ImportStatus = N''Ready''
  AND EXISTS (
    SELECT 1 FROM ' + @AppEntityInfo + N' AS a
    WHERE a.EntityCode = m.TargetEntityCode' + @C + N'
);
';
EXEC sys.sp_executesql @Sql;

IF @ExecuteInsert = 0
BEGIN
    PRINT 'Preview only (@ExecuteInsert = 0). No rows inserted.';
    PRINT 'After insert: recycle IIS app pool before testing entities in the app.';
    RETURN;
END;

-- Pre-insert guard
SET @Sql = N'
SELECT @HasBlocker = CASE WHEN EXISTS (
    SELECT 1 FROM #PlmSysEntityMap AS m
    INNER JOIN ' + @AppEntityInfo + N' AS a ON a.EntityCode = m.TargetEntityCode' + @C + N'
    WHERE m.ImportStatus = N''Ready''
) THEN 1 ELSE 0 END;
';
EXEC sys.sp_executesql @Sql, N'@HasBlocker bit OUTPUT', @HasBlocker = @HasBlocker OUTPUT;

IF @HasBlocker = 1
BEGIN
    RAISERROR('Blockers found — TargetEntityCode already exists in AppEntityInfo.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM #PlmSysEntityMap WHERE ImportStatus = N'Ready')
BEGIN
    RAISERROR('No Ready entities to insert.', 16, 1);
    RETURN;
END;

-- =============================================================================
-- LAYER 5: INSERT AppEntityInfo
-- =============================================================================
BEGIN TRY
    BEGIN TRANSACTION;

    SET @Sql = N'
    INSERT INTO ' + @AppEntityInfo + N' (
        EntityCode, [Description], EntityType, TableName, SchemaOwner,
        IdentityField, DisplayFiled1, DisplayFiled2, DisplayFiled3,
        DataSourceFrom, IsSystemDefine, SaasApplicationID,
        AppCreatedByID, AppCreatedByCompanyID, IntegrationId
    )
    SELECT
        m.TargetEntityCode,
        m.[Description],
        1,
        m.TableName,
        m.SchemaOwner,
        m.IdentityField,
        m.DisplayFiled1,
        m.DisplayFiled2,
        m.DisplayFiled3,
        m.AppDataSourceFrom,
        NULL,
        @SaasApplicationID,
        NULL,
        NULL,
        m.PlmEntityID
    FROM #PlmSysEntityMap AS m
    WHERE m.ImportStatus = N''Ready'';
    ';
    EXEC sys.sp_executesql @Sql, N'@SaasApplicationID int', @SaasApplicationID = @SaasApplicationID;

    COMMIT TRANSACTION;

    SET @Sql = N'
    SELECT
        (SELECT COUNT(*) FROM #PlmSysEntityMap WHERE ImportStatus = N''Ready'') AS EntitiesImported,
        (SELECT COUNT(*) FROM #PlmSysEntityMap WHERE ImportStatus = N''Skipped'') AS EntitiesSkipped;
    ';
    EXEC sys.sp_executesql @Sql;

    SELECT
        m.PlmEntityID,
        m.PlmEntityCode,
        m.TargetEntityCode,
        m.TableName,
        m.PlmDataSourceFrom,
        m.TargetDatabaseName,
        m.SkipReason
    FROM #PlmSysEntityMap AS m
    WHERE m.ImportStatus = N'Skipped'
    ORDER BY m.SkipReason, m.PlmEntityCode;

    PRINT 'Insert complete. Recycle IIS app pool before testing entities in the app.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SET @ErrMsg = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
