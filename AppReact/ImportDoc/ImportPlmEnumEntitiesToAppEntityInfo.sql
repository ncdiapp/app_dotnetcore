/*
  Import PLM enum entities (pdmEntity.EntityType = 3) into APP:
    AppEntityInfo              (EntityType = 4, SimpleValueList)
    AppEntitySimpleListValue

  Mapping:
    PLM EntityCode     -> App EntityCode (truncate 100; prefix Plm_ if duplicate)
    PLM Description    -> App Description
    PLM EnumKey        -> App InternalKey, Sort
    PLM EnumValue      -> App Code (truncate 100)
    App Description    -> blank on value rows
    PLM EntityID       -> AppEntityInfo.IntegrationId (int; column added if missing)

  Execution flow (same session, same SQL Server):
    [1] Parameters & setup (DB names, APP collation)
    [2] Staging table #PlmEnumEntityMap (outer batch — must persist across sp_executesql calls)
    [3] Stage PLM enum headers into #PlmEnumEntityMap (cross-database read)
    [4] Preview result sets (always run — review before insert)
    [5] Optional early exit when @ExecuteInsert = 0
    [6] Blocker check — abort insert if target EntityCode already exists in APP
    [7] Insert into APP (transaction): AppEntityInfo, then AppEntitySimpleListValue
    [8] Post-insert summary or rollback on error

  Note: PLM and APP may use different collations; @C applies APP collation on cross-DB string ops.

  WHERE TO RUN (which database to connect in SSMS / Azure Data Studio):
    -------------------------------------------------------------------------
    This is a CROSS-DATABASE script. Both @PlmDb and @AppDb must be on the
    SAME SQL Server instance.

    Which database should you select in the connection dropdown before Execute?
      -> ANY database on that server is OK (master, @PlmDb, or @AppDb).
      -> Recommended: connect to @AppDb (tenant DB) so you see writes in the
         same context; or master if you prefer a neutral context.
      -> The "current database" does NOT limit where data is read/written.
         All access uses three-part names: [DatabaseName].dbo.TableName.

    Data flow:
      READ  from @PlmDb  (@PlmDb parameter below)
            pdmEntity (EntityType = 3), pdmEntityEnumValue
      WRITE to @AppDb   (@AppDb parameter below)
            AppEntityInfo, AppEntitySimpleListValue

    Temp tables (#PlmEnumEntityMap, etc.) live in tempdb for your session only.
    -------------------------------------------------------------------------
*/

SET NOCOUNT ON;

/*
  EXECUTION CHECKLIST (before F5):
    [1] Connected to the correct SQL Server (not a wrong environment).
    [2] Set @PlmDb and @AppDb in LAYER 1 to your real database names.
    [3] Current database in SSMS toolbar can be anything on that server.
    [4] First run: @ExecuteInsert = 0 (preview). Then set to 1 to insert.
*/

-- =============================================================================
-- LAYER 1: USER PARAMETERS
-- @PlmDb = source (read only).  @AppDb = target (all inserts).
-- SSMS "current database" is irrelevant; these two names drive cross-DB access.
-- =============================================================================
DECLARE @PlmDb             sysname = N'SourcePLM';       -- PLM source DB (read)
DECLARE @AppDb             sysname = N'TenantDB_PLM';   -- APP tenant DB (write)
DECLARE @DataSourceFrom    int     = 56;               -- AppEntityInfo.DataSourceFrom (DataSourceRegister Id)
DECLARE @SaasApplicationID int     = 1;                -- AppEntityInfo.SaasApplicationID (optional package/menu Id)
DECLARE @ExecuteInsert     bit     = 1;                -- 0 = preview only (no APP writes), 1 = insert
-- =============================================================================

-- =============================================================================
-- LAYER 2: SETUP — three-part names [@PlmDb].dbo.* (read) and [@AppDb].dbo.* (write)
-- Collation taken from @AppDb (sys.databases).
-- =============================================================================
DECLARE @PlmEntity       nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmEntity';
DECLARE @PlmEnumValue    nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmEntityEnumValue';
DECLARE @AppEntityInfo   nvarchar(261) = QUOTENAME(@AppDb) + N'.dbo.AppEntityInfo';
DECLARE @AppSimpleList   nvarchar(261) = QUOTENAME(@AppDb) + N'.dbo.AppEntitySimpleListValue';
DECLARE @AppDbQuoted     nvarchar(261) = QUOTENAME(@AppDb);
DECLARE @Collation       sysname;
DECLARE @C               nvarchar(128);   -- appended to PLM strings, e.g. ' COLLATE Latin1_General_CI_AS'
DECLARE @Sql             nvarchar(max);
DECLARE @ErrMsg          nvarchar(4000);
DECLARE @HasBlocker      bit = 0;

IF @PlmDb IS NULL OR LTRIM(RTRIM(@PlmDb)) = N''
   OR @AppDb IS NULL OR LTRIM(RTRIM(@AppDb)) = N''
BEGIN
    RAISERROR('Set @PlmDb and @AppDb before running.', 16, 1);
    RETURN;
END;

-- Use APP database collation for all cross-database string comparisons and inserts.
SELECT @Collation = collation_name
FROM sys.databases
WHERE name = @AppDb;

IF @Collation IS NULL
BEGIN
    RAISERROR('APP database not found or collation could not be resolved.', 16, 1);
    RETURN;
END;

SET @C = N' COLLATE ' + @Collation;

-- Ensure AppEntityInfo.IntegrationId exists (stores original PLM EntityID)
SET @Sql = N'
IF NOT EXISTS (
    SELECT 1
    FROM ' + @AppDbQuoted + N'.sys.columns AS c
    INNER JOIN ' + @AppDbQuoted + N'.sys.tables AS t ON t.object_id = c.object_id
    INNER JOIN ' + @AppDbQuoted + N'.sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE s.name = N''dbo''
      AND t.name = N''AppEntityInfo''
      AND c.name = N''IntegrationId''
)
BEGIN
    ALTER TABLE ' + @AppEntityInfo + N' ADD IntegrationId int NULL;
END;
';
EXEC sys.sp_executesql @Sql;

-- =============================================================================
-- LAYER 3: STAGING TABLE — session-scoped map (PLM EntityID -> target APP EntityCode)
-- Created in the OUTER batch so it survives multiple sp_executesql calls.
-- Do NOT create #temp inside sp_executesql; SQL Server drops it when that batch ends.
-- =============================================================================
IF OBJECT_ID('tempdb..#PlmEnumEntityMap') IS NOT NULL
    DROP TABLE #PlmEnumEntityMap;

CREATE TABLE #PlmEnumEntityMap
(
    PlmEntityID      int            NOT NULL PRIMARY KEY,  -- legacy PLM EntityID (reference only)
    PlmEntityCode    nvarchar(200)  NOT NULL,             -- original PLM EntityCode
    TargetEntityCode nvarchar(100)  NOT NULL,             -- resolved APP EntityCode (may have Plm_ prefix)
    [Description]    nvarchar(500)  NULL                  -- -> AppEntityInfo.Description
);

-- =============================================================================
-- LAYER 4: STAGE DATA — read PLM enum entities (EntityType = 3) into #PlmEnumEntityMap
-- EntityCode rules:
--   - truncate to 100 chars
--   - if truncated code already exists in APP AppEntityInfo, prefix with Plm_
-- =============================================================================
SET @Sql = N'
INSERT INTO #PlmEnumEntityMap (PlmEntityID, PlmEntityCode, TargetEntityCode, [Description])
SELECT
    e.EntityID,
    e.EntityCode' + @C + N',
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM ' + @AppEntityInfo + N' AS a
            WHERE a.EntityCode = LEFT(e.EntityCode, 100)' + @C + N'
        )
        THEN LEFT(N''Plm_'' + e.EntityCode, 100)' + @C + N'
        ELSE LEFT(e.EntityCode, 100)' + @C + N'
    END,
    LEFT(e.[Description], 500)' + @C + N'
FROM ' + @PlmEntity + N' AS e
WHERE e.EntityType = 3;
';

EXEC sys.sp_executesql @Sql;

-- =============================================================================
-- LAYER 5: PREVIEW — result set 1 of 3 (entity headers)
-- Review TargetEntityCode, Plm_ prefix notes, and value counts before insert.
-- =============================================================================
SET @Sql = N'
SELECT
    m.PlmEntityID,
    m.PlmEntityCode,
    m.TargetEntityCode,
    m.[Description] AS EntityDescription,
    CASE
        WHEN m.TargetEntityCode <> LEFT(m.PlmEntityCode, 100) THEN N''Plm_ prefix (duplicate EntityCode)''
        WHEN LEN(m.PlmEntityCode) > 100 THEN N''EntityCode truncated''
        ELSE N''''
    END AS Note,
    (
        SELECT COUNT(*)
        FROM ' + @PlmEnumValue + N' AS ev
        WHERE ev.EntityID = m.PlmEntityID
    ) AS ValueCount
FROM #PlmEnumEntityMap AS m
ORDER BY m.TargetEntityCode;
';
EXEC sys.sp_executesql @Sql;

-- =============================================================================
-- LAYER 5: PREVIEW — result set 2 of 3 (enum values, first 500 rows)
-- Shows how pdmEntityEnumValue rows will map to AppEntitySimpleListValue.
-- =============================================================================
SET @Sql = N'
SELECT TOP (500)
    m.TargetEntityCode,
    ev.EnumKey,
    LEFT(ev.EnumValue, 100)' + @C + N' AS TargetCode,           -- -> Code (truncated)
    CAST(N'''' AS nvarchar(500))' + @C + N' AS TargetDescription, -- always blank
    ev.EnumKey AS TargetInternalKey,                            -- -> InternalKey
    ev.EnumKey AS TargetSort,                                   -- -> Sort
    CASE WHEN LEN(ev.EnumValue) > 100 THEN N''EnumValue truncated for Code'' ELSE N'''' END AS Note
FROM #PlmEnumEntityMap AS m
INNER JOIN ' + @PlmEnumValue + N' AS ev
    ON ev.EntityID = m.PlmEntityID
ORDER BY m.TargetEntityCode, ev.EnumKey;
';
EXEC sys.sp_executesql @Sql;

-- =============================================================================
-- LAYER 5: PREVIEW — result set 3 of 3 (blockers)
-- Non-empty result = insert would fail or duplicate; fix before @ExecuteInsert = 1.
-- Typical causes: re-run script, or Plm_ prefixed code also already exists in APP.
-- =============================================================================
SET @Sql = N'
SELECT
    m.PlmEntityID,
    m.PlmEntityCode,
    m.TargetEntityCode,
    N''Target EntityCode already exists in AppEntityInfo'' AS Issue
FROM #PlmEnumEntityMap AS m
WHERE EXISTS (
    SELECT 1
    FROM ' + @AppEntityInfo + N' AS a
    WHERE a.EntityCode = m.TargetEntityCode' + @C + N'
);
';
EXEC sys.sp_executesql @Sql;

-- =============================================================================
-- LAYER 6: PREVIEW-ONLY EXIT — stop here when @ExecuteInsert = 0
-- =============================================================================
IF @ExecuteInsert = 0
BEGIN
    PRINT 'Preview only (@ExecuteInsert = 0). No rows inserted.';
    RETURN;
END;

-- =============================================================================
-- LAYER 7: PRE-INSERT GUARD — hard stop if any blocker rows exist
-- =============================================================================
SET @Sql = N'
SELECT @HasBlocker = CASE WHEN EXISTS (
    SELECT 1
    FROM #PlmEnumEntityMap AS m
    INNER JOIN ' + @AppEntityInfo + N' AS a
        ON a.EntityCode = m.TargetEntityCode' + @C + N'
) THEN 1 ELSE 0 END;
';

EXEC sys.sp_executesql @Sql, N'@HasBlocker bit OUTPUT', @HasBlocker = @HasBlocker OUTPUT;

IF @HasBlocker = 1
BEGIN
    RAISERROR('One or more TargetEntityCode values already exist in AppEntityInfo. Fix blockers above or delete existing rows before insert.', 16, 1);
    RETURN;
END;

-- =============================================================================
-- LAYER 8: INSERT INTO APP (single transaction)
-- Step 8a: AppEntityInfo headers (EntityType = 4 SimpleValueList)
-- Step 8b: AppEntitySimpleListValue rows (join new EntityInfoID by TargetEntityCode)
-- =============================================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- Step 8a: insert entity headers
    SET @Sql = N'
    INSERT INTO ' + @AppEntityInfo + N'
    (
        EntityCode,
        [Description],
        EntityType,
        DataSourceFrom,
        IsSystemDefine,
        SaasApplicationID,
        AppCreatedByID,
        AppCreatedByCompanyID,
        IntegrationId
    )
    SELECT
        m.TargetEntityCode,
        m.[Description],
        4,                      -- SimpleValueList (PLM enum entity)
        @DataSourceFrom,
        NULL,                   -- user-defined entity
        @SaasApplicationID,
        NULL,
        NULL,
        m.PlmEntityID           -- original PLM EntityID
    FROM #PlmEnumEntityMap AS m;
    ';
    EXEC sys.sp_executesql
        @Sql,
        N'@DataSourceFrom int, @SaasApplicationID int',
        @DataSourceFrom = @DataSourceFrom,
        @SaasApplicationID = @SaasApplicationID;

    -- Step 8b: insert enum values (requires Step 8a — EntityInfoID must exist)
    SET @Sql = N'
    INSERT INTO ' + @AppSimpleList + N'
    (
        EntityInfoID,
        Sort,
        Code,
        [Description],
        InternalKey,
        AppCreatedByID,
        AppCreatedByCompanyID
    )
    SELECT
        ai.EntityInfoID,
        ev.EnumKey,                         -- Sort
        LEFT(ev.EnumValue, 100)' + @C + N', -- Code
        N'''' ' + @C + N',                  -- Description (blank)
        ev.EnumKey,                         -- InternalKey
        NULL,
        NULL
    FROM #PlmEnumEntityMap AS m
    INNER JOIN ' + @AppEntityInfo + N' AS ai
        ON ai.EntityCode = m.TargetEntityCode' + @C + N'
    INNER JOIN ' + @PlmEnumValue + N' AS ev
        ON ev.EntityID = m.PlmEntityID;
    ';
    EXEC sys.sp_executesql @Sql;

    COMMIT TRANSACTION;

    -- =============================================================================
    -- LAYER 9: POST-INSERT SUMMARY
    -- =============================================================================
    SET @Sql = N'
    SELECT
        (SELECT COUNT(*) FROM #PlmEnumEntityMap) AS EntitiesImported,
        COUNT(*) AS ValuesImported
    FROM ' + @AppSimpleList + N' AS v
    INNER JOIN ' + @AppEntityInfo + N' AS ai
        ON ai.EntityInfoID = v.EntityInfoID
    INNER JOIN #PlmEnumEntityMap AS m
        ON m.TargetEntityCode = ai.EntityCode' + @C + N';
    ';
    EXEC sys.sp_executesql @Sql;
END TRY
BEGIN CATCH
    -- Roll back both AppEntityInfo and AppEntitySimpleListValue if either step fails.
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SET @ErrMsg = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
