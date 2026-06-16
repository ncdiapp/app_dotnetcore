/*
  Export selected PLM / external reference tables (and two views) from a source
  database into a new target database on the SAME SQL Server instance.

  WHAT IT DOES
    - De-duplicates the table list you provided (56 unique objects).
    - For each object that exists in @SourceDb:
        * Base tables  -> SELECT * INTO (structure + data; identity preserved)
        * Views        -> materialized as tables with the same name (data snapshot)
    - Optional: create @TargetDb, drop existing target objects before copy.
    - Preview mode (@ExecuteCopy = 0) prints the SQL without running copies.

  LIMITATIONS (by design — keep script simple and portable)
    - Does NOT copy indexes, FKs, triggers, or extended properties.
    - Views become regular tables in the target (standalone export DB use case).
    - Cross-database SELECT INTO requires both DBs on one server.

  WHERE TO RUN
    Connect to any database on the server (master recommended for CREATE DATABASE).
    All access uses three-part names: [DatabaseName].dbo.ObjectName.

  EXECUTION CHECKLIST
    [1] Set @SourceDb and @TargetDb below.
    [2] Run with @ExecuteCopy = 0 first (preview / validation).
    [3] Set @ExecuteCopy = 1 to perform the export.
*/

SET NOCOUNT ON;

-- =============================================================================
-- LAYER 1: USER PARAMETERS
-- =============================================================================
DECLARE @SourceDb        sysname = N'ImportationsRallye_Erp_20260602';   -- huge third-party source DB
DECLARE @TargetDb        sysname = N'SourceERP'; -- new smaller export DB
DECLARE @CreateTargetDb  bit     = 1;   -- 1 = CREATE DATABASE if missing
DECLARE @DropExisting    bit     = 1;   -- 1 = DROP TABLE in target before copy
DECLARE @ExecuteCopy     bit     = 1;   -- 0 = preview only, 1 = execute copy
-- =============================================================================

-- =============================================================================
-- LAYER 2: OBJECT LIST (de-duplicated from your request)
-- =============================================================================
DECLARE @Objects TABLE (
    Seq         int          NOT NULL IDENTITY(1, 1) PRIMARY KEY,
    ObjectName  sysname      NOT NULL,
    UNIQUE (ObjectName)
);

INSERT INTO @Objects (ObjectName) VALUES
    (N'tblProductClass'),
    (N'tblProductType'),
    (N'tblCompanyDivision'),
    (N'tblCollection'),
    (N'tblGroup'),
    (N'tblSellingPeriod'),
    (N'tblDimension'),
    (N'tblSizeRun'),
    (N'tblComposition'),
    (N'tblVendor'),
    (N'tblSystemProductStatusType'),
    (N'tblEmployee'),
    (N'tblAgent'),
    (N'tblCountry'),
    (N'tblSizeRunRotate'),
    (N'vwCustomer'),
    (N'tblCareInstruction'),
    (N'tblContentLabel'),
    (N'tblUnitOfMeasure'),
    (N'tblCurrency'),
    (N'tblCompositionType'),
    (N'tblFiber'),
    (N'tblCieWarehouse'),
    (N'tblDimensionDetail'),
    (N'tblPackage'),
    (N'tblHTS'),
    (N'tblComponent'),
    (N'tblProductCatalog'),
    (N'tblLabel'),
    (N'tblProductClassGroup'),
    (N'tblColorNRF'),
    (N'tblRoyalty'),
    (N'tblRoyaltyItem'),
    (N'tblPersonType'),
    (N'tblCaNo'),
    (N'tblRoyaltyDetail'),
    (N'tblSalesType'),
    (N'tblProductionCycle'),
    (N'tblTerm'),
    (N'tblShipmentTerm'),
    (N'tblShipVia'),
    (N'tblTax'),
    (N'tblSystemSubjectDetail'),
    (N'tblPOInstruction'),
    (N'tblCustomerShipTo'),
    (N'tblCompanySetup'),
    (N'tblAgentGroup'),
    (N'tblContainerType'),
    (N'tblPrintLabel'),
    (N'tblReason'),
    (N'tblDepartment'),
    (N'tblCommission'),
    (N'tblCustomer'),
    (N'tblLocationStorageType'),
    (N'tblComponentGroup'),
    (N'View_tblGender');

IF OBJECT_ID(N'tempdb..#Objects') IS NOT NULL
    DROP TABLE #Objects;

SELECT Seq, ObjectName INTO #Objects FROM @Objects;

-- =============================================================================
-- LAYER 3: VALIDATION
-- =============================================================================
IF DB_ID(@SourceDb) IS NULL
BEGIN
    RAISERROR(N'Source database [%s] was not found on this server.', 16, 1, @SourceDb);
    RETURN;
END;

IF DB_ID(@TargetDb) IS NULL
BEGIN
    IF @CreateTargetDb = 1
    BEGIN
        DECLARE @CreateDbSql nvarchar(max) =
            N'CREATE DATABASE ' + QUOTENAME(@TargetDb) + N';';

        PRINT N'-- Target database will be created: ' + @TargetDb;
        IF @ExecuteCopy = 1
            EXEC sys.sp_executesql @CreateDbSql;
        ELSE
            PRINT @CreateDbSql;
    END
    ELSE IF @ExecuteCopy = 1
    BEGIN
        RAISERROR(N'Target database [%s] does not exist. Set @CreateTargetDb = 1 or create it manually.', 16, 1, @TargetDb);
        RETURN;
    END;
END;

-- =============================================================================
-- LAYER 4: PRE-FLIGHT — report missing objects in source
-- =============================================================================
DECLARE @SourceObjects sysname = QUOTENAME(@SourceDb) + N'.sys.objects';

IF OBJECT_ID(N'tempdb..#SourceObjectStatus') IS NOT NULL
    DROP TABLE #SourceObjectStatus;

CREATE TABLE #SourceObjectStatus (
    ObjectName  sysname NOT NULL,
    ObjectType  char(2) NULL
);

DECLARE @SourceSchemas sysname = QUOTENAME(@SourceDb) + N'.sys.schemas';

DECLARE @PreflightSql nvarchar(max) = N'
INSERT INTO #SourceObjectStatus (ObjectName, ObjectType)
SELECT o.ObjectName, so.type
FROM #Objects o
LEFT JOIN ' + @SourceObjects + N' so
    ON so.name = o.ObjectName
   AND so.schema_id = (SELECT schema_id FROM ' + @SourceSchemas + N' WHERE name = N''dbo'')
   AND so.type IN (N''U'', N''V'');';

EXEC sys.sp_executesql @PreflightSql;

PRINT N'';
PRINT N'========== PRE-FLIGHT: objects in source ==========';

SELECT
    o.Seq,
    o.ObjectName,
    CASE
        WHEN s.ObjectType IS NULL THEN N'MISSING in source'
        WHEN s.ObjectType = N'U' THEN N'TABLE'
        WHEN s.ObjectType = N'V' THEN N'VIEW (will materialize as table)'
        ELSE N'UNSUPPORTED type ' + s.ObjectType
    END AS SourceStatus
FROM @Objects o
LEFT JOIN #SourceObjectStatus s ON s.ObjectName = o.ObjectName
ORDER BY o.Seq;

IF EXISTS (SELECT 1 FROM #SourceObjectStatus WHERE ObjectType IS NULL)
BEGIN
    PRINT N'';
    PRINT N'WARNING: One or more requested objects were not found in '
        + QUOTENAME(@SourceDb) + N'.dbo. They will be skipped.';
END;

-- =============================================================================
-- LAYER 5: COPY LOOP
-- =============================================================================
PRINT N'';
PRINT N'========== COPY PLAN ==========';

DECLARE
    @ObjectName   sysname,
    @SourceObject nvarchar(517),
    @TargetObject nvarchar(517),
    @ObjectType   char(2),
    @Sql          nvarchar(max),
  @RowCount     bigint,
    @StartedAt    datetime2(3),
    @EndedAt      datetime2(3);

DECLARE obj_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT
        o.ObjectName,
        s.ObjectType
    FROM @Objects o
    INNER JOIN #SourceObjectStatus s
        ON s.ObjectName = o.ObjectName
       AND s.ObjectType IN (N'U', N'V')
    ORDER BY o.Seq;

OPEN obj_cur;
FETCH NEXT FROM obj_cur INTO @ObjectName, @ObjectType;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SourceObject = QUOTENAME(@SourceDb) + N'.dbo.' + QUOTENAME(@ObjectName);
    SET @TargetObject = QUOTENAME(@TargetDb) + N'.dbo.' + QUOTENAME(@ObjectName);

    IF @ExecuteCopy = 1 AND DB_ID(@TargetDb) IS NULL
    BEGIN
        RAISERROR(N'Target database [%s] is not available for copy.', 16, 1, @TargetDb);
        RETURN;
    END;

    IF @DropExisting = 1
    BEGIN
        SET @Sql = N'
IF OBJECT_ID(N''' + @TargetDb + N'.dbo.' + @ObjectName + N''', N''U'') IS NOT NULL
    DROP TABLE ' + @TargetObject + N';';

        IF @ExecuteCopy = 1
            EXEC sys.sp_executesql @Sql;
        ELSE
            PRINT @Sql;
    END;

    SET @Sql = N'SELECT * INTO ' + @TargetObject + N' FROM ' + @SourceObject + N';';

    PRINT N'';
    PRINT N'-- ' + @ObjectName
        + CASE WHEN @ObjectType = N'V' THEN N' (view -> table)' ELSE N'' END;

    IF @ExecuteCopy = 1
    BEGIN
        SET @StartedAt = SYSDATETIME();
        BEGIN TRY
            EXEC sys.sp_executesql @Sql;

            SET @Sql = N'SELECT @rc = COUNT_BIG(1) FROM ' + @TargetObject + N';';
            EXEC sys.sp_executesql @Sql, N'@rc bigint OUTPUT', @rc = @RowCount OUTPUT;

            SET @EndedAt = SYSDATETIME();
            PRINT N'OK  rows=' + CAST(@RowCount AS nvarchar(30))
                + N'  elapsed_ms=' + CAST(DATEDIFF(millisecond, @StartedAt, @EndedAt) AS nvarchar(20));
        END TRY
        BEGIN CATCH
            PRINT N'ERROR on ' + @ObjectName + N': ' + ERROR_MESSAGE();
        END CATCH;
    END
    ELSE
        PRINT @Sql;

    FETCH NEXT FROM obj_cur INTO @ObjectName, @ObjectType;
END;

CLOSE obj_cur;
DEALLOCATE obj_cur;

-- =============================================================================
-- LAYER 6: POST-COPY SUMMARY
-- =============================================================================
IF @ExecuteCopy = 1
BEGIN
    PRINT N'';
    PRINT N'========== TARGET SUMMARY ==========';

    DECLARE @TargetSchemas sysname = QUOTENAME(@TargetDb) + N'.sys.schemas';

    DECLARE @SummarySql nvarchar(max) = N'
SELECT
    o.ObjectName,
    ISNULL(SUM(p.rows), 0) AS ApproxRowCount
FROM #Objects o
INNER JOIN ' + QUOTENAME(@TargetDb) + N'.sys.tables st
    ON st.name = o.ObjectName
   AND st.schema_id = (SELECT schema_id FROM ' + @TargetSchemas + N' WHERE name = N''dbo'')
LEFT JOIN ' + QUOTENAME(@TargetDb) + N'.sys.partitions p
    ON p.object_id = st.object_id
   AND p.index_id IN (0, 1)
GROUP BY o.Seq, o.ObjectName
ORDER BY o.Seq;';

    EXEC sys.sp_executesql @SummarySql;
END
ELSE
BEGIN
    PRINT N'';
    PRINT N'Preview complete. Set @ExecuteCopy = 1 to run the export.';
END;
