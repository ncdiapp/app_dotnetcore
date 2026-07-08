-- =============================================================================
-- PLM BOM Grid Colorway cells -> APP Grandchild (ChildUnitPivotColumns storage)
-- Template: source/PlmDw_ImportBomColorwayGrandchild.sql
-- Deliverable: output/{templateId}/5_PlmDw_ImportBomColorwayGrandchild.sql
--
-- UNPIVOTs PLM DW wide Colorway_N / Image_N slots into normalized grandchild rows.
-- Column key (Colorway) = pdmStyleColorWayMapping.StyleColorID (explicit popup mapping).
-- Cell values = DW Colorway_N (ArtworkColor) + Image_N (ArtworkPhoto).
--
-- EXECUTION (on TENANT database -- host + grandchild physical tables):
--   Prerequisites:
--     1. 1_PlmDw_Tables.sql
--     2. 2_PlmDw_FieldMapping.sql
--     3. 3_PlmDw_ImportFromDW.sql  (host BOM rows must exist)
--     4. 4_PlmDw_ImportBlueprint.json + Phase D Execute
--     Source colorway grid imported (e.g. Plm_ProductDesignColorGrid) for pivot column headers
--   Then run this script (step 5 in pipeline).
--   Step 6 (legacy cleanup) only needed for databases created before BomColorwayDwSlot change.
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- <<< USER SETTINGS -- generator patches from bomColorwayImportConfig.json >>>
DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'PLM';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = NULL;
DECLARE @PlmTabId              INT           = NULL;
DECLARE @PlmGridId             INT           = NULL;
DECLARE @ProductGridBlockId    INT           = NULL;
DECLARE @DwGridTable           NVARCHAR(256) = NULL;   -- e.g. PLM_DW_Grid_Artwork_BOM_prod_3167
DECLARE @HostAppTable          NVARCHAR(128) = NULL;   -- e.g. Artwork_BOM_prod (prefix applied)
DECLARE @GrandchildAppTable    NVARCHAR(128) = NULL;   -- e.g. ArtworkGrandColorway
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'Colorway';       -- IsPivotColumn field on grandchild unit
DECLARE @GcArtworkColorColumn  NVARCHAR(128) = N'ArtworkColor';   -- IsPivotValue
DECLARE @GcArtworkPhotoColumn  NVARCHAR(128) = N'ArtworkPhoto';   -- IsPivotValue (nullable)
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'ParentRowId';     -- FK -> host RowId
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';         -- APPEND | REPLACE
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;              -- optional pilot e.g. N'1536,2001'
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @Step                  NVARCHAR(128);

-- Drop leftover temp tables from a prior run in the same SSMS connection
IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter (
    [ReferenceId] INT NOT NULL PRIMARY KEY
);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    [ColorwayDwColumn]       NVARCHAR(256) NOT NULL,
    [ImageDwColumn]          NVARCHAR(256) NULL,
    PRIMARY KEY ([SlotNo])
);

SET @MappingTable   = @TablePrefix + N'FieldMapping';
SET @HostTable      = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF @PlmTemplateId IS NULL OR @PlmGridId IS NULL OR @ProductGridBlockId IS NULL
   OR @DwGridTable IS NULL OR @HostAppTable IS NULL OR @GrandchildAppTable IS NULL
BEGIN
    RAISERROR(N'Required parameters missing: @PlmTemplateId, @PlmGridId, @ProductGridBlockId, @DwGridTable, @HostAppTable, @GrandchildAppTable.', 16, 1);
    RETURN;
END

IF DB_ID(@PlmDatabase) IS NULL
BEGIN
    RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase);
    RETURN;
END

IF DB_ID(@DwDatabase) IS NULL
BEGIN
    RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase);
    RETURN;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Host table dbo.%s missing. Run PlmDw_Tables.sql + PlmDw_ImportFromDW.sql first.', 16, 1, @HostTable);
    RETURN;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable);
    RETURN;
END

IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL
BEGIN
    RAISERROR(N'PLM table %s.dbo.pdmStyleColorWayMapping not found.', 16, 1, @PlmDatabase);
    RETURN;
END

-- Build #RefFilter (template scope)
IF @ReferenceIdList IS NOT NULL AND LTRIM(RTRIM(@ReferenceIdList)) <> N''
BEGIN
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT TRY_CAST(LTRIM(RTRIM([value])) AS INT)
    FROM STRING_SPLIT(@ReferenceIdList, N',')
    WHERE TRY_CAST(LTRIM(RTRIM([value])) AS INT) IS NOT NULL;
END
ELSE
BEGIN
    SET @sql = N'
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT pt.[ProductReferenceID]
    FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmProductTemplate] AS pt
    WHERE pt.[TemplateID] = @tid
      AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0
BEGIN
    RAISERROR(N'No references in import scope.', 16, 1);
    RETURN;
END

PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;

-- Slot map from tenant FieldMapping (DW Colorway_N / ImageN columns — not APP host table columns).
-- FieldKind: BomColorwayDwSlot (current) or legacy BomColorwaySlot / GridColumn.
SET @sql = N'
INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId], [ColorwayDwColumn], [ImageDwColumn])
SELECT
    TRY_CAST(REPLACE(cw.[AppColumnName], N''Colorway_'', N'''') AS INT) AS SlotNo,
    cw.[PlmMetaColumnId],
    cw.[DwColumnName],
    img.[DwColumnName]
FROM dbo.' + QUOTENAME(@MappingTable) + N' AS cw
LEFT JOIN dbo.' + QUOTENAME(@MappingTable) + N' AS img
    ON img.[AppTableName] = cw.[AppTableName]
   AND img.[PlmGridId] = cw.[PlmGridId]
   AND img.[AppColumnName] = N''Image''
        + TRY_CAST(REPLACE(cw.[AppColumnName], N''Colorway_'', N'''') AS NVARCHAR(10))
WHERE cw.[AppTableName] = @hostApp
  AND cw.[PlmGridId] = @gridId
  AND cw.[FieldKind] IN (N''BomColorwayDwSlot'', N''BomColorwaySlot'', N''GridColumn'')
  AND cw.[AppColumnName] LIKE N''Colorway[_]%''
  AND cw.[PlmMetaColumnId] IS NOT NULL
  AND TRY_CAST(REPLACE(cw.[AppColumnName], N''Colorway_'', N'''') AS INT) IS NOT NULL;';

EXEC sp_executesql @sql,
    N'@hostApp nvarchar(128), @gridId int',
    @hostApp = @HostTable, @gridId = @PlmGridId;

SELECT @RowCnt = COUNT(*) FROM #SlotMap;
IF @RowCnt = 0
BEGIN
    RAISERROR(N'No Colorway_N slot columns found in %s for PlmGridId=%d.', 16, 1, @MappingTable, @PlmGridId);
    RETURN;
END

INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, @RowCnt);

DECLARE @CntDeleteGc   INT = 0;
DECLARE @CntDwBom      INT = 0;
DECLARE @CntHost       INT = 0;
DECLARE @CntDwCells    INT = 0;
DECLARE @CntInsertGc   INT = 0;
DECLARE @CntSkipMap    INT = 0;
DECLARE @CntSkipHost   INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    -- REPLACE: remove existing grandchild rows for scoped host rows
    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc
        FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    -- Staging: DW BOM rows in scope
    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL,
        [RowID]              INT NOT NULL,
        [Sort]               INT NULL,
        [HostMatchRn]        INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT
            dw.[ProductReferenceID],
            dw.[RowID],
            dw.[Sort],
            ROW_NUMBER() OVER (
                PARTITION BY dw.[ProductReferenceID]
                ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]
            ) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
        WHERE dw.[GridID] = @gridId OR @gridId IS NULL
    )
    INSERT INTO #DwBom ([ProductReferenceID], [RowID], [Sort], [HostMatchRn])
    SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';

    EXEC sp_executesql @sql, N'@gridId int', @gridId = @PlmGridId;
    SET @CntDwBom = @@ROWCOUNT;

    -- Staging: host rows with match rank
    CREATE TABLE #HostRanked (
        [RowId]        INT NOT NULL PRIMARY KEY,
        [ReferenceId]  INT NOT NULL,
        [HostMatchRn]  INT NOT NULL
    );

    SET @sql = N'
    ;WITH h AS (
        SELECT
            h.[RowId],
            h.[ReferenceId],
            ROW_NUMBER() OVER (
                PARTITION BY h.[ReferenceId]
                ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]
            ) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked ([RowId], [ReferenceId], [HostMatchRn])
    SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';

    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    -- Build dynamic UNPIVOT (one SELECT per slot -> UNION ALL)
    DECLARE @unionSql NVARCHAR(MAX) = N'';
    SELECT @unionSql = @unionSql
        + CASE WHEN @unionSql <> N'' THEN N' UNION ALL ' ELSE N'' END
        + N'SELECT '
        + CAST(s.[SlotNo] AS NVARCHAR(10)) + N' AS SlotNo, '
        + CAST(s.[ColorWayGridColumnId] AS NVARCHAR(20)) + N' AS ColorWayGridColumnId, '
        + N'TRY_CAST(dw.' + QUOTENAME(s.[ColorwayDwColumn]) + N' AS INT) AS ArtworkColor, '
        + CASE WHEN s.[ImageDwColumn] IS NOT NULL
               THEN N'TRY_CAST(dw.' + QUOTENAME(s.[ImageDwColumn]) + N' AS INT)'
               ELSE N'CAST(NULL AS INT)' END
        + N' AS ArtworkPhoto'
    FROM #SlotMap AS s
    ORDER BY s.[SlotNo];

    CREATE TABLE #DwCells (
        [ProductReferenceID]     INT NOT NULL,
        [RowID]                  INT NOT NULL,
        [HostMatchRn]            INT NOT NULL,
        [SlotNo]                 INT NOT NULL,
        [ColorWayGridColumnId]   INT NOT NULL,
        [ArtworkColor]           INT NULL,
        [ArtworkPhoto]           INT NULL
    );

    SET @sql = N'
    INSERT INTO #DwCells (
        [ProductReferenceID], [RowID], [HostMatchRn], [SlotNo],
        [ColorWayGridColumnId], [ArtworkColor], [ArtworkPhoto]
    )
    SELECT
        b.[ProductReferenceID],
        b.[RowID],
        b.[HostMatchRn],
        u.[SlotNo],
        u.[ColorWayGridColumnId],
        u.[ArtworkColor],
        u.[ArtworkPhoto]
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID]
       AND dw.[RowID] = b.[RowID]
    CROSS APPLY (
        ' + @unionSql + N'
    ) AS u
    WHERE u.[ArtworkColor] IS NOT NULL OR u.[ArtworkPhoto] IS NOT NULL;';

    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    -- Count rows to insert (also used for DryRun preview)
    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr
        ON hr.[ReferenceId] = c.[ProductReferenceID]
       AND hr.[HostMatchRn] = c.[HostMatchRn]'
    + CASE WHEN @ImportMode = N'APPEND' THEN N'
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    )' ELSE N'' END + N';';

    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    -- Insert grandchild rows (join explicit colorway mapping + host match)
    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        ' + QUOTENAME(@GcParentLinkColumn) + N',
        ' + QUOTENAME(@GcColorwayColumn) + N',
        ' + QUOTENAME(@GcArtworkColorColumn) + N',
        ' + QUOTENAME(@GcArtworkPhotoColumn) + N'
    )
    SELECT
        hr.[RowId],
        m.[StyleColorID],
        c.[ArtworkColor],
        c.[ArtworkPhoto]
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr
        ON hr.[ReferenceId] = c.[ProductReferenceID]
       AND hr.[HostMatchRn] = c.[HostMatchRn]'
    + CASE WHEN @ImportMode = N'APPEND' THEN N'
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    )' ELSE N'' END + N';';

    IF @DryRun = 0
        EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    -- Log cells skipped: value present but no mapping row
    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1
        FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    -- Log DW rows with no matching host row
    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID]
          AND hr.[HostMatchRn] = b.[HostMatchRn]
    );';
    EXEC sp_executesql @sql, N'@cnt int OUTPUT', @cnt = @CntSkipHost OUTPUT;

    IF @DryRun = 1 ROLLBACK TRANSACTION;
    ELSE COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'PlmDw_ImportBomColorwayGrandchild failed (line %d): %s', 16, 1, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

IF @ImportMode = N'REPLACE'
    INSERT INTO #ImportLog VALUES (N'DELETE_GRANDCHILD', @GrandchildTable, @CntDeleteGc);
INSERT INTO #ImportLog VALUES (N'DW_BOM_ROWS', @DwGridTable, @CntDwBom);
INSERT INTO #ImportLog VALUES (N'HOST_ROWS', @HostTable, @CntHost);
INSERT INTO #ImportLog VALUES (N'DW_CELLS', N'non-empty slots', @CntDwCells);
INSERT INTO #ImportLog VALUES (N'INSERT_GRANDCHILD', @GrandchildTable, @CntInsertGc);
INSERT INTO #ImportLog VALUES (N'SKIP_NO_MAPPING', N'cells with value but no pdmStyleColorWayMapping', @CntSkipMap);
INSERT INTO #ImportLog VALUES (N'SKIP_NO_HOST', N'DW BOM rows without host match', @CntSkipHost);

-- Dynamic SELECT: compiled at EXEC time (after DROP+CREATE), so a leftover
-- old-schema #ImportLog in the same session cannot break compile-time binding.
EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
