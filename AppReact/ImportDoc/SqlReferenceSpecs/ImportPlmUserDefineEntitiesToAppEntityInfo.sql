/*
  Import PLM UserDefineTable entities ONLY (pdmEntity.EntityType = 4, EmEntityType.UserDefineTable).

  OUT OF SCOPE (future scripts):
    PLM EmEntityType.SystemDefineTable (1), PDMEnum (3), RelationFK (5), etc.

  PLM source (EAV):
    pdmEntity / pdmUserDefineEntityColumn / pdmUserDefineEntityRow / pdmUserDefineEntityRowValue

  APP targets (by PLM UserDefineTable column count):
    <= 2 columns  -> EmAppEntityType.SimpleValueList (4)
                     AppEntityInfo + AppEntitySimpleListValue
                     col1 -> Code, col2 -> Description (by DataRowSort); RowID -> InternalKey & Sort
    >  2 columns  -> EmAppEntityType.SystemDefineTable (1)
                     CREATE TABLE Plm_entity_{EntityCode} in @AppDb
                     AppEntityInfo.TableName = physical table name
                     Data from row/value tables (EAV pivot); RowID = PK

  Naming:
    AppEntityInfo.EntityCode  = sanitized PLM EntityCode (A-Z, a-z, 0-9, _ only; Plm_ if duplicate)
    Physical table (>2 cols)  = Plm_entity_{sanitized EntityCode} (no spaces or punctuation)
    Column names            = sanitized SystemTableColumnName / ColumnName (deduped per table)
    AppEntityInfo.Description = original PLM Description (human-readable, unchanged)
    AppEntityInfo.IntegrationId = original PLM EntityID (int; column added if missing)

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
            pdmEntity, pdmUserDefineEntityColumn,
            pdmUserDefineEntityRow, pdmUserDefineEntityRowValue
      WRITE to @AppDb   (@AppDb parameter below)
            AppEntityInfo, AppEntitySimpleListValue
            CREATE TABLE dbo.Plm_entity_* (SystemDefineTable entities only)

    Temp tables (#PlmUdEntityMap, etc.) live in tempdb for your session only.
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
-- @PlmDb = source (read only).  @AppDb = target (all inserts / CREATE TABLE).
-- SSMS "current database" is irrelevant; these two names drive cross-DB access.
-- =============================================================================
DECLARE @PlmDb               sysname = N'SourcePLM';       -- PLM source DB (read)
DECLARE @AppDb               sysname = N'TenantDB_PLM';   -- APP tenant DB (write)
DECLARE @DataSourceFrom      int     = 56;
DECLARE @SaasApplicationID   int     = 1;
DECLARE @ExecuteInsert       bit     = 0;   -- 0 = preview only, 1 = insert
DECLARE @PhysicalTablePrefix nvarchar(20) = N'Plm_entity_';
-- =============================================================================

-- =============================================================================
-- LAYER 2: SETUP — three-part names [@PlmDb].dbo.* (read) and [@AppDb].dbo.* (write)
-- Collation taken from @AppDb (sys.databases).
-- =============================================================================
DECLARE @PlmEntityTbl      nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmEntity';
DECLARE @PlmColTbl         nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmUserDefineEntityColumn';
DECLARE @PlmRowTbl         nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmUserDefineEntityRow';
DECLARE @PlmValTbl         nvarchar(261) = QUOTENAME(@PlmDb) + N'.dbo.pdmUserDefineEntityRowValue';
DECLARE @AppEntityInfo     nvarchar(261) = QUOTENAME(@AppDb) + N'.dbo.AppEntityInfo';
DECLARE @AppSimpleList     nvarchar(261) = QUOTENAME(@AppDb) + N'.dbo.AppEntitySimpleListValue';
DECLARE @AppDbQuoted       nvarchar(261) = QUOTENAME(@AppDb);
DECLARE @Collation         sysname;
DECLARE @C                 nvarchar(128);
DECLARE @Sql               nvarchar(max);
DECLARE @ErrMsg            nvarchar(4000);
DECLARE @HasBlocker        bit = 0;

IF @PlmDb IS NULL OR LTRIM(RTRIM(@PlmDb)) = N''
   OR @AppDb IS NULL OR LTRIM(RTRIM(@AppDb)) = N''
BEGIN
    RAISERROR('Set @PlmDb and @AppDb before running.', 16, 1);
    RETURN;
END;

SELECT @Collation = collation_name FROM sys.databases WHERE name = @AppDb;
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

-- Sanitize helper variables (LAYER 3b — safe SQL identifiers for APP table/column/entity codes)
DECLARE @SanIn     nvarchar(4000);
DECLARE @SanOut    nvarchar(4000);
DECLARE @SanPos    int;
DECLARE @SanCh     nchar(1);
DECLARE @SanMaxLen int;
DECLARE @SanId     int;
DECLARE @EntPlmId    int;
DECLARE @EntType     int;

-- =============================================================================
-- LAYER 3: STAGING (outer batch)
-- =============================================================================
IF OBJECT_ID('tempdb..#PlmUdEntityMap') IS NOT NULL DROP TABLE #PlmUdEntityMap;
IF OBJECT_ID('tempdb..#PlmUdColumnMap') IS NOT NULL DROP TABLE #PlmUdColumnMap;

CREATE TABLE #PlmUdEntityMap
(
    PlmEntityID       int           NOT NULL PRIMARY KEY,
    PlmEntityCode     nvarchar(200) NOT NULL,
    TargetEntityCode  nvarchar(100) NOT NULL,   -- AppEntityInfo.EntityCode
    TargetTableName   nvarchar(100) NULL,       -- AppEntityInfo.TableName (SystemDefineTable only): Plm_entity_*
    [Description]     nvarchar(500) NULL,
    ColumnCount       int           NOT NULL,
    TargetEntityType  int           NOT NULL,   -- 4 = SimpleValueList, 1 = SystemDefineTable
    PlmRowCount       int           NOT NULL DEFAULT (0),
    ImportOrder       int           NOT NULL DEFAULT (0)
);

CREATE TABLE #PlmUdColumnMap
(
    PlmEntityID               int           NOT NULL,
    UserDefineEntityColumnID  int           NOT NULL,
    ColumnName                nvarchar(200) NOT NULL,
    TargetSqlColumnName       nvarchar(128) NOT NULL,
    ColOrdinal                int           NOT NULL,
    UsedByDropDownList        bit           NOT NULL,
    DisplayOrdinal            int           NULL,
    IsCodeColumn              bit           NOT NULL DEFAULT (0),
    IsDescColumn              bit           NOT NULL DEFAULT (0),
    UIControlType             int           NULL,
    FKEntityID                int           NULL,
    PRIMARY KEY (PlmEntityID, UserDefineEntityColumnID)
);

-- =============================================================================
-- LAYER 4: STAGE — PLM UserDefineTable only (EntityType = 4); names sanitized in 4b
-- =============================================================================
SET @Sql = N'
INSERT INTO #PlmUdEntityMap (
    PlmEntityID, PlmEntityCode, TargetEntityCode, TargetTableName,
    [Description], ColumnCount, TargetEntityType
)
SELECT
    e.EntityID,
    e.EntityCode' + @C + N',
    N'''',
    NULL,
    LEFT(e.[Description], 500)' + @C + N',
    ISNULL(cc.ColumnCount, 0),
    CASE WHEN ISNULL(cc.ColumnCount, 0) <= 2 THEN 4 ELSE 1 END
FROM ' + @PlmEntityTbl + N' AS e
OUTER APPLY (
    SELECT COUNT(*) AS ColumnCount
    FROM ' + @PlmColTbl + N' AS c
    WHERE c.EntityID = e.EntityID
) AS cc
WHERE e.EntityType = 4
  AND ISNULL(e.IsRelationEntity, 0) = 0;
';
EXEC sys.sp_executesql @Sql;

-- LAYER 4b: Sanitize TargetEntityCode and TargetTableName (A-Z, a-z, 0-9, _ only)
DECLARE entity_sanitize_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT PlmEntityID, PlmEntityCode, TargetEntityType FROM #PlmUdEntityMap;

OPEN entity_sanitize_cursor;
FETCH NEXT FROM entity_sanitize_cursor INTO @EntPlmId, @SanIn, @EntType;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SanMaxLen = 100;
    SET @SanOut = N'';
    SET @SanPos = 1;
    WHILE @SanPos <= LEN(@SanIn)
    BEGIN
        SET @SanCh = SUBSTRING(@SanIn, @SanPos, 1);
        IF @SanCh LIKE N'[A-Za-z0-9]'
            SET @SanOut = @SanOut + @SanCh;
        ELSE IF @SanOut = N'' OR RIGHT(@SanOut, 1) <> N'_'
            SET @SanOut = @SanOut + N'_';
        SET @SanPos += 1;
    END;
    WHILE LEN(@SanOut) > 0 AND RIGHT(@SanOut, 1) = N'_' SET @SanOut = LEFT(@SanOut, LEN(@SanOut) - 1);
    WHILE LEN(@SanOut) > 0 AND LEFT(@SanOut, 1) = N'_' SET @SanOut = SUBSTRING(@SanOut, 2, 4000);
    IF @SanOut = N'' SET @SanOut = N'Entity_' + CAST(@EntPlmId AS nvarchar(20));
    IF LEFT(@SanOut, 1) LIKE N'[0-9]' SET @SanOut = N'T_' + @SanOut;
    SET @SanOut = LEFT(@SanOut, 100);

    UPDATE #PlmUdEntityMap
    SET TargetEntityCode = @SanOut,
        TargetTableName = CASE
            WHEN @EntType = 1 THEN LEFT(@PhysicalTablePrefix + @SanOut, 100)
            ELSE NULL
        END
    WHERE PlmEntityID = @EntPlmId;

    FETCH NEXT FROM entity_sanitize_cursor INTO @EntPlmId, @SanIn, @EntType;
END;

CLOSE entity_sanitize_cursor;
DEALLOCATE entity_sanitize_cursor;

-- Plm_ prefix if EntityCode already in APP; append _EntityID if duplicate within this import batch
SET @Sql = N'
UPDATE m
SET TargetEntityCode = LEFT(N''Plm_'' + m.TargetEntityCode, 100)
FROM #PlmUdEntityMap AS m
WHERE EXISTS (
    SELECT 1 FROM ' + @AppEntityInfo + N' AS a
    WHERE a.EntityCode = m.TargetEntityCode' + @C + N'
);
';
EXEC sys.sp_executesql @Sql;

;WITH dupEntity AS (
    SELECT TargetEntityCode
    FROM #PlmUdEntityMap
    GROUP BY TargetEntityCode
    HAVING COUNT(*) > 1
)
UPDATE m
SET TargetEntityCode = LEFT(m.TargetEntityCode + N'_' + CAST(m.PlmEntityID AS nvarchar(20)), 100)
FROM #PlmUdEntityMap AS m
INNER JOIN dupEntity AS d ON d.TargetEntityCode = m.TargetEntityCode;

-- Re-sync table names after EntityCode dedupe (SystemDefineTable only)
UPDATE m
SET TargetTableName = LEFT(@PhysicalTablePrefix + m.TargetEntityCode, 100)
FROM #PlmUdEntityMap AS m
WHERE m.TargetEntityType = 1;

;WITH dupTable AS (
    SELECT TargetTableName
    FROM #PlmUdEntityMap
    WHERE TargetTableName IS NOT NULL
    GROUP BY TargetTableName
    HAVING COUNT(*) > 1
)
UPDATE m
SET TargetTableName = LEFT(m.TargetTableName + N'_' + CAST(m.PlmEntityID AS nvarchar(20)), 100)
FROM #PlmUdEntityMap AS m
INNER JOIN dupTable AS d ON d.TargetTableName = m.TargetTableName;

SET @Sql = N'
UPDATE m
SET PlmRowCount = rc.Cnt
FROM #PlmUdEntityMap AS m
INNER JOIN (
    SELECT r.EntityID, COUNT(*) AS Cnt
    FROM ' + @PlmRowTbl + N' AS r
    GROUP BY r.EntityID
) AS rc ON rc.EntityID = m.PlmEntityID;
';
EXEC sys.sp_executesql @Sql;

-- =============================================================================
-- LAYER 5: STAGE columns (raw names; sanitized in 5b)
-- =============================================================================
SET @Sql = N'
INSERT INTO #PlmUdColumnMap (
    PlmEntityID, UserDefineEntityColumnID, ColumnName, TargetSqlColumnName,
    ColOrdinal, UsedByDropDownList, UIControlType, FKEntityID
)
SELECT
    c.EntityID,
    c.UserDefineEntityColumnID,
    c.ColumnName' + @C + N',
    LEFT(
        CASE
            WHEN NULLIF(LTRIM(RTRIM(c.SystemTableColumnName)), N'''') IS NOT NULL
            THEN c.SystemTableColumnName
            ELSE c.ColumnName
        END,
        4000
    )' + @C + N',
    ROW_NUMBER() OVER (
        PARTITION BY c.EntityID
        ORDER BY ISNULL(c.DataRowSort, 9999), c.UserDefineEntityColumnID
    ),
    ISNULL(c.UsedByDropDownList, 0),
    c.UIControlType,
    c.FKEntityID
FROM ' + @PlmColTbl + N' AS c
INNER JOIN #PlmUdEntityMap AS m ON m.PlmEntityID = c.EntityID;
';
EXEC sys.sp_executesql @Sql;

-- LAYER 5b: Sanitize column names (same rules as entity codes; max 128 chars)
DECLARE col_sanitize_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT PlmEntityID, UserDefineEntityColumnID, TargetSqlColumnName
    FROM #PlmUdColumnMap;

OPEN col_sanitize_cursor;
FETCH NEXT FROM col_sanitize_cursor INTO @EntPlmId, @SanId, @SanIn;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SanMaxLen = 128;
    SET @SanOut = N'';
    SET @SanPos = 1;
    WHILE @SanPos <= LEN(@SanIn)
    BEGIN
        SET @SanCh = SUBSTRING(@SanIn, @SanPos, 1);
        IF @SanCh LIKE N'[A-Za-z0-9]'
            SET @SanOut = @SanOut + @SanCh;
        ELSE IF @SanOut = N'' OR RIGHT(@SanOut, 1) <> N'_'
            SET @SanOut = @SanOut + N'_';
        SET @SanPos += 1;
    END;
    WHILE LEN(@SanOut) > 0 AND RIGHT(@SanOut, 1) = N'_' SET @SanOut = LEFT(@SanOut, LEN(@SanOut) - 1);
    WHILE LEN(@SanOut) > 0 AND LEFT(@SanOut, 1) = N'_' SET @SanOut = SUBSTRING(@SanOut, 2, 4000);
    IF @SanOut = N'' SET @SanOut = N'Col_' + CAST(@SanId AS nvarchar(20));
    IF LEFT(@SanOut, 1) LIKE N'[0-9]' SET @SanOut = N'C_' + @SanOut;
    SET @SanOut = LEFT(@SanOut, 128);

    UPDATE #PlmUdColumnMap
    SET TargetSqlColumnName = @SanOut
    WHERE PlmEntityID = @EntPlmId AND UserDefineEntityColumnID = @SanId;

    FETCH NEXT FROM col_sanitize_cursor INTO @EntPlmId, @SanId, @SanIn;
END;

CLOSE col_sanitize_cursor;
DEALLOCATE col_sanitize_cursor;

-- Dedupe column names within each entity (append _ColumnID if collision)
;WITH colDup AS (
    SELECT PlmEntityID, TargetSqlColumnName,
        ROW_NUMBER() OVER (
            PARTITION BY PlmEntityID, TargetSqlColumnName
            ORDER BY UserDefineEntityColumnID
        ) AS rn,
        UserDefineEntityColumnID
    FROM #PlmUdColumnMap
)
UPDATE c
SET TargetSqlColumnName = LEFT(c.TargetSqlColumnName + N'_' + CAST(c.UserDefineEntityColumnID AS nvarchar(20)), 128)
FROM #PlmUdColumnMap AS c
INNER JOIN colDup AS d
    ON d.PlmEntityID = c.PlmEntityID
   AND d.UserDefineEntityColumnID = c.UserDefineEntityColumnID
WHERE d.rn > 1;

-- =============================================================================
-- LAYER 5c: Display / code column flags (after sanitized names)
-- =============================================================================
;WITH disp AS (
    SELECT PlmEntityID, UserDefineEntityColumnID,
        ROW_NUMBER() OVER (PARTITION BY PlmEntityID ORDER BY ColOrdinal) AS dispOrd
    FROM #PlmUdColumnMap
    WHERE UsedByDropDownList = 1
)
UPDATE c SET DisplayOrdinal = d.dispOrd
FROM #PlmUdColumnMap AS c
INNER JOIN disp AS d ON d.PlmEntityID = c.PlmEntityID AND d.UserDefineEntityColumnID = c.UserDefineEntityColumnID
WHERE d.dispOrd <= 3;

UPDATE c SET IsCodeColumn = 1
FROM #PlmUdColumnMap AS c
INNER JOIN #PlmUdEntityMap AS m ON m.PlmEntityID = c.PlmEntityID
WHERE m.TargetEntityType = 4 AND c.ColOrdinal = 1;

UPDATE c SET IsDescColumn = 1
FROM #PlmUdColumnMap AS c
INNER JOIN #PlmUdEntityMap AS m ON m.PlmEntityID = c.PlmEntityID
WHERE m.TargetEntityType = 4 AND c.ColOrdinal = 2;

-- =============================================================================
-- LAYER 6: Dependency order (FK to other PLM UserDefineTable entities in this import)
-- =============================================================================
IF OBJECT_ID('tempdb..#PlmUdDep') IS NOT NULL DROP TABLE #PlmUdDep;
CREATE TABLE #PlmUdDep (ChildEntityID int NOT NULL, ParentEntityID int NOT NULL);

INSERT INTO #PlmUdDep (ChildEntityID, ParentEntityID)
SELECT DISTINCT c.PlmEntityID, c.FKEntityID
FROM #PlmUdColumnMap AS c
INNER JOIN #PlmUdEntityMap AS fk ON fk.PlmEntityID = c.FKEntityID
WHERE c.FKEntityID IS NOT NULL;

;WITH ord AS (
    SELECT m.PlmEntityID, 0 AS lvl
    FROM #PlmUdEntityMap AS m
    WHERE NOT EXISTS (SELECT 1 FROM #PlmUdDep AS d WHERE d.ChildEntityID = m.PlmEntityID)
    UNION ALL
    SELECT d.ChildEntityID, o.lvl + 1
    FROM #PlmUdDep AS d
    INNER JOIN ord AS o ON o.PlmEntityID = d.ParentEntityID
)
UPDATE m SET ImportOrder = x.lvl
FROM #PlmUdEntityMap AS m
INNER JOIN (SELECT PlmEntityID, MAX(lvl) AS lvl FROM ord GROUP BY PlmEntityID) AS x ON x.PlmEntityID = m.PlmEntityID;

UPDATE #PlmUdEntityMap SET ImportOrder = 0 WHERE ImportOrder IS NULL;

-- =============================================================================
-- LAYER 7: PREVIEW
-- =============================================================================
SELECT
    m.PlmEntityID,
    m.PlmEntityCode,
    m.TargetEntityCode,
    m.TargetTableName,
    m.[Description],
    m.ColumnCount,
    CASE m.TargetEntityType WHEN 4 THEN N'SimpleValueList' WHEN 1 THEN N'SystemDefineTable' END AS AppTargetType,
    m.PlmRowCount,
    m.ImportOrder,
    CASE
        WHEN m.PlmEntityCode <> m.TargetEntityCode THEN
            CASE
                WHEN m.TargetEntityCode LIKE N'Plm[_]%' THEN N'Plm_ prefix (duplicate in APP) + name sanitized'
                WHEN m.TargetEntityCode LIKE N'%[_]' + CAST(m.PlmEntityID AS nvarchar(20)) THEN N'Name sanitized + dedupe suffix'
                ELSE N'Name sanitized (spaces/punctuation -> _)'
            END
        ELSE N''
    END AS Note
FROM #PlmUdEntityMap AS m
ORDER BY m.ImportOrder, m.TargetEntityCode;

SELECT
    m.TargetEntityCode,
    m.TargetTableName,
    c.ColOrdinal,
    c.TargetSqlColumnName,
    c.IsCodeColumn,
    c.IsDescColumn,
    c.DisplayOrdinal,
    c.FKEntityID
FROM #PlmUdColumnMap AS c
INNER JOIN #PlmUdEntityMap AS m ON m.PlmEntityID = c.PlmEntityID
ORDER BY m.ImportOrder, m.TargetEntityCode, c.ColOrdinal;

-- Blockers
SET @Sql = N'
SELECT m.PlmEntityID, m.TargetEntityCode, N''AppEntityInfo.EntityCode already exists'' AS Issue
FROM #PlmUdEntityMap AS m
WHERE EXISTS (
    SELECT 1 FROM ' + @AppEntityInfo + N' AS a WHERE a.EntityCode = m.TargetEntityCode' + @C + N'
)
UNION ALL
SELECT m.PlmEntityID, m.TargetTableName, N''Physical table Plm_entity_* already exists'' AS Issue
FROM #PlmUdEntityMap AS m
WHERE m.TargetEntityType = 1
  AND m.TargetTableName IS NOT NULL
  AND EXISTS (
    SELECT 1
    FROM ' + @AppDbQuoted + N'.sys.tables AS t
    INNER JOIN ' + @AppDbQuoted + N'.sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE s.name = N''dbo'' AND t.name = m.TargetTableName' + @C + N'
);
';
EXEC sys.sp_executesql @Sql;

IF @ExecuteInsert = 0
BEGIN
    PRINT 'Preview only (@ExecuteInsert = 0). No rows inserted.';
    RETURN;
END;

SET @Sql = N'
SELECT @HasBlocker = CASE
    WHEN EXISTS (
        SELECT 1 FROM #PlmUdEntityMap AS m
        INNER JOIN ' + @AppEntityInfo + N' AS a ON a.EntityCode = m.TargetEntityCode' + @C + N'
    )
    OR EXISTS (
        SELECT 1 FROM #PlmUdEntityMap AS m
        INNER JOIN ' + @AppDbQuoted + N'.sys.tables AS t ON t.name = m.TargetTableName' + @C + N'
        INNER JOIN ' + @AppDbQuoted + N'.sys.schemas AS s ON s.schema_id = t.schema_id AND s.name = N''dbo''
        WHERE m.TargetEntityType = 1 AND m.TargetTableName IS NOT NULL
    )
    THEN 1 ELSE 0 END;
';
EXEC sys.sp_executesql @Sql, N'@HasBlocker bit OUTPUT', @HasBlocker = @HasBlocker OUTPUT;

IF @HasBlocker = 1
BEGIN
    RAISERROR('Blockers found — fix preview blocker result set before insert.', 16, 1);
    RETURN;
END;

-- =============================================================================
-- LAYER 8: INSERT (per entity, dependency order)
-- =============================================================================
DECLARE
    @CurEntityID       int,
    @CurEntityCode     nvarchar(100),
    @CurTableName      nvarchar(100),
    @CurDesc           nvarchar(500),
    @CurTargetType     int,
    @CurDisp1          nvarchar(100),
    @CurDisp2          nvarchar(100),
    @CurDisp3          nvarchar(100),
    @CurCodeColId      int,
    @CurDescColId      int,
    @EntityInfoID      int,
    @CreateSql         nvarchar(max),
    @InsertSql         nvarchar(max),
    @ColList           nvarchar(max),
    @SelList           nvarchar(max),
    @ColName           nvarchar(128),
    @ColId             int,
    @SqlType           nvarchar(100),
    @UIType            int,
    @ValExpr           nvarchar(max),
    @ColOrd            int,
    @MaxColOrd         int;

DECLARE entity_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT PlmEntityID, TargetEntityCode, TargetTableName, [Description], TargetEntityType
    FROM #PlmUdEntityMap
    ORDER BY ImportOrder, TargetEntityCode;

BEGIN TRY
    OPEN entity_cursor;
    FETCH NEXT FROM entity_cursor INTO @CurEntityID, @CurEntityCode, @CurTableName, @CurDesc, @CurTargetType;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @CurDisp1 = NULL; SET @CurDisp2 = NULL; SET @CurDisp3 = NULL;
        SELECT @CurDisp1 = TargetSqlColumnName FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID AND DisplayOrdinal = 1;
        SELECT @CurDisp2 = TargetSqlColumnName FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID AND DisplayOrdinal = 2;
        SELECT @CurDisp3 = TargetSqlColumnName FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID AND DisplayOrdinal = 3;
        SELECT @CurCodeColId = UserDefineEntityColumnID FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID AND IsCodeColumn = 1;
        SELECT @CurDescColId = UserDefineEntityColumnID FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID AND IsDescColumn = 1;

        -- 8a. AppEntityInfo
        SET @Sql = N'
        INSERT INTO ' + @AppEntityInfo + N' (
            EntityCode, [Description], EntityType, TableName,
            IdentityField, DisplayFiled1, DisplayFiled2, DisplayFiled3,
            DataSourceFrom, IsSystemDefine, SaasApplicationID,
            AppCreatedByID, AppCreatedByCompanyID, IntegrationId
        )
        VALUES (
            @EntityCode' + @C + N',
            @Description' + @C + N',
            @EntityType,
            @TableName' + @C + N',
            CASE WHEN @EntityType = 1 THEN N''RowID'' ELSE NULL END,
            @Disp1' + @C + N', @Disp2' + @C + N', @Disp3' + @C + N',
            @DataSourceFrom, NULL, @SaasApplicationID, NULL, NULL, @PlmEntityID
        );
        SET @EntityInfoID = SCOPE_IDENTITY();
        ';
        EXEC sys.sp_executesql @Sql,
            N'@EntityCode nvarchar(100), @Description nvarchar(500), @EntityType int, @TableName nvarchar(100),
              @Disp1 nvarchar(100), @Disp2 nvarchar(100), @Disp3 nvarchar(100),
              @DataSourceFrom int, @SaasApplicationID int, @PlmEntityID int, @EntityInfoID int OUTPUT',
            @EntityCode = @CurEntityCode, @Description = @CurDesc, @EntityType = @CurTargetType,
            @TableName = @CurTableName,
            @Disp1 = @CurDisp1, @Disp2 = @CurDisp2, @Disp3 = @CurDisp3,
            @DataSourceFrom = @DataSourceFrom, @SaasApplicationID = @SaasApplicationID,
            @PlmEntityID = @CurEntityID,
            @EntityInfoID = @EntityInfoID OUTPUT;

        IF @CurTargetType = 4
        BEGIN
            -- 8b. SimpleValueList — Code + Description from PLM UserDefineTable columns
            IF @CurCodeColId IS NOT NULL
            BEGIN
                SET @Sql = N'
                INSERT INTO ' + @AppSimpleList + N' (
                    EntityInfoID, Sort, Code, [Description], InternalKey,
                    AppCreatedByID, AppCreatedByCompanyID
                )
                SELECT
                    @EntityInfoID,
                    r.RowID,
                    LEFT(
                        CASE
                            WHEN cv1.ValueID IS NOT NULL THEN CAST(cv1.ValueID AS nvarchar(100))
                            WHEN cv1.ValueDate IS NOT NULL THEN CONVERT(nvarchar(30), cv1.ValueDate, 121)
                            ELSE ISNULL(cv1.ValueText, N'''')
                        END, 100
                    )' + @C + N',
                    LEFT(
                        CASE
                            WHEN @DescColId IS NULL THEN N''''
                            WHEN cv2.ValueID IS NOT NULL THEN CAST(cv2.ValueID AS nvarchar(100))
                            WHEN cv2.ValueDate IS NOT NULL THEN CONVERT(nvarchar(30), cv2.ValueDate, 121)
                            ELSE ISNULL(cv2.ValueText, N'''')
                        END, 500
                    )' + @C + N',
                    r.RowID, NULL, NULL
                FROM ' + @PlmRowTbl + N' AS r
                LEFT JOIN ' + @PlmValTbl + N' AS cv1 ON cv1.RowID = r.RowID AND cv1.UserDefineEntityColumnID = @CodeColId
                LEFT JOIN ' + @PlmValTbl + N' AS cv2 ON cv2.RowID = r.RowID AND cv2.UserDefineEntityColumnID = @DescColId
                WHERE r.EntityID = @PlmEntityID;
                ';
                EXEC sys.sp_executesql @Sql,
                    N'@EntityInfoID int, @PlmEntityID int, @CodeColId int, @DescColId int',
                    @EntityInfoID = @EntityInfoID, @PlmEntityID = @CurEntityID,
                    @CodeColId = @CurCodeColId, @DescColId = @CurDescColId;
            END;
        END
        ELSE IF @CurTargetType = 1 AND @CurTableName IS NOT NULL
        BEGIN
            -- 8c. SystemDefineTable — CREATE Plm_entity_* table + load from row/value EAV
            SET @CreateSql = N'CREATE TABLE ' + @AppDbQuoted + N'.dbo.' + QUOTENAME(@CurTableName) + N' ('
                + N'RowID int NOT NULL CONSTRAINT ' + QUOTENAME(N'PK_' + LEFT(REPLACE(@CurTableName, N'.', N'_'), 100)) + N' PRIMARY KEY';
            SET @ColList = N'RowID';
            SET @SelList = N'r.RowID';
            SET @ColOrd = 1;
            SELECT @MaxColOrd = MAX(ColOrdinal) FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID;

            WHILE @ColOrd <= ISNULL(@MaxColOrd, 0)
            BEGIN
                SELECT @ColId = UserDefineEntityColumnID, @ColName = TargetSqlColumnName, @UIType = UIControlType
                FROM #PlmUdColumnMap WHERE PlmEntityID = @CurEntityID AND ColOrdinal = @ColOrd;

                SET @SqlType = CASE
                    WHEN @UIType = 7 THEN N'datetime NULL'
                    WHEN @UIType = 13 THEN N'bit NULL'
                    WHEN @UIType IN (1, 20) THEN N'int NULL'
                    ELSE N'nvarchar(4000) NULL'
                END;

                SET @ValExpr = CASE
                    WHEN @UIType = 7 THEN N'CASE WHEN rv.ValueDate IS NOT NULL THEN rv.ValueDate ELSE TRY_CAST(rv.ValueText AS datetime) END'
                    WHEN @UIType = 13 THEN N'CASE WHEN rv.ValueID IS NOT NULL THEN rv.ValueID WHEN rv.ValueText IN (N''1'',N''true'',N''True'') THEN 1 WHEN rv.ValueText IN (N''0'',N''false'',N''False'') THEN 0 ELSE TRY_CAST(rv.ValueText AS bit) END'
                    WHEN @UIType IN (1, 20) THEN N'CASE WHEN rv.ValueID IS NOT NULL THEN rv.ValueID ELSE TRY_CAST(rv.ValueText AS int) END'
                    ELSE N'CASE WHEN rv.ValueID IS NOT NULL THEN CAST(rv.ValueID AS nvarchar(4000)) WHEN rv.ValueDate IS NOT NULL THEN CONVERT(nvarchar(30), rv.ValueDate, 121) ELSE rv.ValueText END'
                END;

                SET @CreateSql = @CreateSql + N',' + QUOTENAME(@ColName) + N' ' + @SqlType;
                SET @ColList = @ColList + N',' + QUOTENAME(@ColName);
                SET @SelList = @SelList + N', MAX(CASE WHEN rv.UserDefineEntityColumnID = '
                    + CAST(@ColId AS nvarchar(20)) + N' THEN ' + @ValExpr + N' END)';
                SET @ColOrd += 1;
            END;

            SET @CreateSql = @CreateSql + N');';
            EXEC sys.sp_executesql @CreateSql;

            SET @InsertSql = N'
            INSERT INTO ' + @AppDbQuoted + N'.dbo.' + QUOTENAME(@CurTableName) + N' (' + @ColList + N')
            SELECT ' + @SelList + N'
            FROM ' + @PlmRowTbl + N' AS r
            LEFT JOIN ' + @PlmValTbl + N' AS rv ON rv.RowID = r.RowID
            WHERE r.EntityID = @PlmEntityID
            GROUP BY r.RowID;
            ';
            EXEC sys.sp_executesql @InsertSql, N'@PlmEntityID int', @PlmEntityID = @CurEntityID;
        END;

        FETCH NEXT FROM entity_cursor INTO @CurEntityID, @CurEntityCode, @CurTableName, @CurDesc, @CurTargetType;
    END;

    CLOSE entity_cursor;
    DEALLOCATE entity_cursor;

    SELECT
        SUM(CASE WHEN TargetEntityType = 4 THEN 1 ELSE 0 END) AS SimpleValueListEntities,
        SUM(CASE WHEN TargetEntityType = 1 THEN 1 ELSE 0 END) AS SystemDefineTableEntities,
        SUM(PlmRowCount) AS TotalPlmRows
    FROM #PlmUdEntityMap;

END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'entity_cursor') >= 0
    BEGIN CLOSE entity_cursor; DEALLOCATE entity_cursor; END;
    SET @ErrMsg = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
