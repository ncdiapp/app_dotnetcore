
-- =============================================================================
-- 5_PlmDw_ImportBomColorwayGrandchild.sql - Template 3360
-- Pivot value columns from PLM pdmGridMetaColumn (DCU slot + DcucolumnId children).
-- =============================================================================

-- ----- BOM grid 3167 / block 5052 -> Artwork_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3360;
DECLARE @PlmTabId              INT           = 4246;
DECLARE @PlmGridId             INT           = 3167;
DECLARE @ProductGridBlockId    INT           = 5052;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Artwork_BOM_prod_3167';
DECLARE @HostAppTable          NVARCHAR(128) = N'Artwork_BOM_prod';
DECLARE @GrandchildAppTable    NVARCHAR(128) = N'Artwork_BOM_prodGrandColorway';
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'Colorway';
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'ParentRowId';
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7805 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7805_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image1_7809_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 2 AS SlotNo, 7806 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7806_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image2_7810_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 3 AS SlotNo, 7807 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7807_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image3_7811_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 4 AS SlotNo, 7808 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7808_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image4_7812_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 5 AS SlotNo, 7972 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7972_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image5_7973_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 6 AS SlotNo, 7974 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7974_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image6_7975_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 7 AS SlotNo, 7995 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7995_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image7_8014_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 8 AS SlotNo, 7996 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7996_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image8_8015_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 9 AS SlotNo, 7997 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7997_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image9_8016_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 10 AS SlotNo, 7998 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7998_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image10_8017_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 11 AS SlotNo, 7999 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_11_7999_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image11_8018_FK_tblSketch] AS INT) AS [ArtworkPhoto] UNION ALL SELECT 12 AS SlotNo, 8000 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_12_8000_FK_pdmRGBColor] AS INT) AS [ArtworkColor], TRY_CAST([dw].[Image12_8019_FK_tblSketch] AS INT) AS [ArtworkPhoto]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

-- Column name [TableName] must match PlmDw_ImportFromDW.sql #ImportLog (same SSMS session).
CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7805),
    (2, 7806),
    (3, 7807),
    (4, 7808),
    (5, 7972),
    (6, 7974),
    (7, 7995),
    (8, 7996),
    (9, 7997),
    (10, 7998),
    (11, 7999),
    (12, 8000);

SET @MappingTable    = @TablePrefix + N'FieldMapping';
SET @HostTable       = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF DB_ID(@PlmDatabase) IS NULL BEGIN RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase); RETURN; END
IF DB_ID(@DwDatabase) IS NULL BEGIN RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL BEGIN RAISERROR(N'Host table dbo.%s missing.', 16, 1, @HostTable); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL BEGIN RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable); RETURN; END
IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL BEGIN RAISERROR(N'PLM table pdmStyleColorWayMapping not found.', 16, 1); RETURN; END

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
    WHERE pt.[TemplateID] = @tid AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0 BEGIN RAISERROR(N'No references in import scope.', 16, 1); RETURN; END
PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;
INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, (SELECT COUNT(*) FROM #SlotMap));

DECLARE @CntDeleteGc INT = 0, @CntDwBom INT = 0, @CntHost INT = 0, @CntDwCells INT = 0, @CntInsertGc INT = 0, @CntSkipMap INT = 0, @CntSkipHost INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [Sort] INT NULL, [HostMatchRn] INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT dw.[ProductReferenceID], dw.[RowID], dw.[Sort],
            ROW_NUMBER() OVER (PARTITION BY dw.[ProductReferenceID] ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
    )
    INSERT INTO #DwBom SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';
    EXEC sp_executesql @sql;
    SET @CntDwBom = @@ROWCOUNT;

    CREATE TABLE #HostRanked ([RowId] INT NOT NULL PRIMARY KEY, [ReferenceId] INT NOT NULL, [HostMatchRn] INT NOT NULL);
    SET @sql = N'
    ;WITH h AS (
        SELECT h.[RowId], h.[ReferenceId],
            ROW_NUMBER() OVER (PARTITION BY h.[ReferenceId] ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';
    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    CREATE TABLE #DwCells (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [HostMatchRn] INT NOT NULL,
        [SlotNo] INT NOT NULL, [ColorWayGridColumnId] INT NOT NULL,
        [ArtworkColor] INT NULL,
        [ArtworkPhoto] INT NULL
    );

    SET @sql = N'
    INSERT INTO #DwCells ([ProductReferenceID], [RowID], [HostMatchRn], [SlotNo], [ColorWayGridColumnId], [ArtworkColor], [ArtworkPhoto])
    SELECT b.[ProductReferenceID], b.[RowID], b.[HostMatchRn], u.[SlotNo], u.[ColorWayGridColumnId], u.[ArtworkColor], u.[ArtworkPhoto]
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID] AND dw.[RowID] = b.[RowID]
    CROSS APPLY (' + @unionSql + N') AS u
    WHERE u.[ArtworkColor] IS NOT NULL OR u.[ArtworkPhoto] IS NOT NULL;';
    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        [ParentRowId],
        [Colorway],
        [ArtworkColor], [ArtworkPhoto]
    )
    SELECT hr.[RowId], m.[StyleColorID], c.[ArtworkColor], c.[ArtworkPhoto]
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';

    IF @DryRun = 0 EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID] AND hr.[HostMatchRn] = b.[HostMatchRn]
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

EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3169 / block 5084 -> Trim_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3360;
DECLARE @PlmTabId              INT           = 4256;
DECLARE @PlmGridId             INT           = 3169;
DECLARE @ProductGridBlockId    INT           = 5084;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Trim_BOM_prod_20_Colorways_3169';
DECLARE @HostAppTable          NVARCHAR(128) = N'Trim_BOM_prod';
DECLARE @GrandchildAppTable    NVARCHAR(128) = N'Trim_BOM_prodGrandColorway';
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'Colorway';
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'ParentRowId';
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7266 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7266_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 2 AS SlotNo, 7267 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7267_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 3 AS SlotNo, 7268 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7268_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 4 AS SlotNo, 7269 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7269_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 5 AS SlotNo, 7270 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7270_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 6 AS SlotNo, 7271 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7271_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 7 AS SlotNo, 7272 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7272_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 8 AS SlotNo, 7273 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7273_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 9 AS SlotNo, 7274 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7274_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 10 AS SlotNo, 7275 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7275_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 11 AS SlotNo, 7278 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_11_7278_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 12 AS SlotNo, 7279 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_12_7279_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 13 AS SlotNo, 7280 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_13_7280_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 14 AS SlotNo, 7281 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_14_7281_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 15 AS SlotNo, 7282 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_15_7282_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 16 AS SlotNo, 7283 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_16_7283_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 17 AS SlotNo, 7284 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_17_7284_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 18 AS SlotNo, 7285 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_18_7285_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 19 AS SlotNo, 7286 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_19_7286_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 20 AS SlotNo, 7287 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_20_7287_FK_pdmRGBColor] AS INT) AS [TrimColorway]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

-- Column name [TableName] must match PlmDw_ImportFromDW.sql #ImportLog (same SSMS session).
CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7266),
    (2, 7267),
    (3, 7268),
    (4, 7269),
    (5, 7270),
    (6, 7271),
    (7, 7272),
    (8, 7273),
    (9, 7274),
    (10, 7275),
    (11, 7278),
    (12, 7279),
    (13, 7280),
    (14, 7281),
    (15, 7282),
    (16, 7283),
    (17, 7284),
    (18, 7285),
    (19, 7286),
    (20, 7287);

SET @MappingTable    = @TablePrefix + N'FieldMapping';
SET @HostTable       = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF DB_ID(@PlmDatabase) IS NULL BEGIN RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase); RETURN; END
IF DB_ID(@DwDatabase) IS NULL BEGIN RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL BEGIN RAISERROR(N'Host table dbo.%s missing.', 16, 1, @HostTable); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL BEGIN RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable); RETURN; END
IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL BEGIN RAISERROR(N'PLM table pdmStyleColorWayMapping not found.', 16, 1); RETURN; END

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
    WHERE pt.[TemplateID] = @tid AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0 BEGIN RAISERROR(N'No references in import scope.', 16, 1); RETURN; END
PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;
INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, (SELECT COUNT(*) FROM #SlotMap));

DECLARE @CntDeleteGc INT = 0, @CntDwBom INT = 0, @CntHost INT = 0, @CntDwCells INT = 0, @CntInsertGc INT = 0, @CntSkipMap INT = 0, @CntSkipHost INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [Sort] INT NULL, [HostMatchRn] INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT dw.[ProductReferenceID], dw.[RowID], dw.[Sort],
            ROW_NUMBER() OVER (PARTITION BY dw.[ProductReferenceID] ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
    )
    INSERT INTO #DwBom SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';
    EXEC sp_executesql @sql;
    SET @CntDwBom = @@ROWCOUNT;

    CREATE TABLE #HostRanked ([RowId] INT NOT NULL PRIMARY KEY, [ReferenceId] INT NOT NULL, [HostMatchRn] INT NOT NULL);
    SET @sql = N'
    ;WITH h AS (
        SELECT h.[RowId], h.[ReferenceId],
            ROW_NUMBER() OVER (PARTITION BY h.[ReferenceId] ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';
    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    CREATE TABLE #DwCells (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [HostMatchRn] INT NOT NULL,
        [SlotNo] INT NOT NULL, [ColorWayGridColumnId] INT NOT NULL,
        [TrimColorway] INT NULL
    );

    SET @sql = N'
    INSERT INTO #DwCells ([ProductReferenceID], [RowID], [HostMatchRn], [SlotNo], [ColorWayGridColumnId], [TrimColorway])
    SELECT b.[ProductReferenceID], b.[RowID], b.[HostMatchRn], u.[SlotNo], u.[ColorWayGridColumnId], u.[TrimColorway]
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID] AND dw.[RowID] = b.[RowID]
    CROSS APPLY (' + @unionSql + N') AS u
    WHERE u.[TrimColorway] IS NOT NULL;';
    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        [ParentRowId],
        [Colorway],
        [TrimColorway]
    )
    SELECT hr.[RowId], m.[StyleColorID], c.[TrimColorway]
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';

    IF @DryRun = 0 EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID] AND hr.[HostMatchRn] = b.[HostMatchRn]
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

EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3170 / block 5085 -> Label_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3360;
DECLARE @PlmTabId              INT           = 4257;
DECLARE @PlmGridId             INT           = 3170;
DECLARE @ProductGridBlockId    INT           = 5085;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Label_BOM_prod_20_Colorways_3170';
DECLARE @HostAppTable          NVARCHAR(128) = N'Label_BOM_prod';
DECLARE @GrandchildAppTable    NVARCHAR(128) = N'Label_BOM_prodGrandColorway';
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'Colorway';
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'ParentRowId';
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7320 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7320_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_1_7312_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 2 AS SlotNo, 7321 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7321_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_2_7314_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 3 AS SlotNo, 7322 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7322_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_3_7316_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 4 AS SlotNo, 7323 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7323_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_4_7318_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 5 AS SlotNo, 7324 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7324_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 6 AS SlotNo, 7325 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7325_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 7 AS SlotNo, 7326 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7326_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 8 AS SlotNo, 7327 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7327_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 9 AS SlotNo, 7328 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7328_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 10 AS SlotNo, 7329 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7329_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 11 AS SlotNo, 7330 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_11_7330_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 12 AS SlotNo, 7331 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_12_7331_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 13 AS SlotNo, 7332 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_13_7332_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 14 AS SlotNo, 7333 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_14_7333_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 15 AS SlotNo, 7334 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_15_7334_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 16 AS SlotNo, 7335 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_16_7335_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 17 AS SlotNo, 7336 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_17_7336_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 18 AS SlotNo, 7337 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_18_7337_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 19 AS SlotNo, 7338 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_19_7338_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 20 AS SlotNo, 7339 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_20_7339_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

-- Column name [TableName] must match PlmDw_ImportFromDW.sql #ImportLog (same SSMS session).
CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7320),
    (2, 7321),
    (3, 7322),
    (4, 7323),
    (5, 7324),
    (6, 7325),
    (7, 7326),
    (8, 7327),
    (9, 7328),
    (10, 7329),
    (11, 7330),
    (12, 7331),
    (13, 7332),
    (14, 7333),
    (15, 7334),
    (16, 7335),
    (17, 7336),
    (18, 7337),
    (19, 7338),
    (20, 7339);

SET @MappingTable    = @TablePrefix + N'FieldMapping';
SET @HostTable       = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF DB_ID(@PlmDatabase) IS NULL BEGIN RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase); RETURN; END
IF DB_ID(@DwDatabase) IS NULL BEGIN RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL BEGIN RAISERROR(N'Host table dbo.%s missing.', 16, 1, @HostTable); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL BEGIN RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable); RETURN; END
IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL BEGIN RAISERROR(N'PLM table pdmStyleColorWayMapping not found.', 16, 1); RETURN; END

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
    WHERE pt.[TemplateID] = @tid AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0 BEGIN RAISERROR(N'No references in import scope.', 16, 1); RETURN; END
PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;
INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, (SELECT COUNT(*) FROM #SlotMap));

DECLARE @CntDeleteGc INT = 0, @CntDwBom INT = 0, @CntHost INT = 0, @CntDwCells INT = 0, @CntInsertGc INT = 0, @CntSkipMap INT = 0, @CntSkipHost INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [Sort] INT NULL, [HostMatchRn] INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT dw.[ProductReferenceID], dw.[RowID], dw.[Sort],
            ROW_NUMBER() OVER (PARTITION BY dw.[ProductReferenceID] ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
    )
    INSERT INTO #DwBom SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';
    EXEC sp_executesql @sql;
    SET @CntDwBom = @@ROWCOUNT;

    CREATE TABLE #HostRanked ([RowId] INT NOT NULL PRIMARY KEY, [ReferenceId] INT NOT NULL, [HostMatchRn] INT NOT NULL);
    SET @sql = N'
    ;WITH h AS (
        SELECT h.[RowId], h.[ReferenceId],
            ROW_NUMBER() OVER (PARTITION BY h.[ReferenceId] ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';
    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    CREATE TABLE #DwCells (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [HostMatchRn] INT NOT NULL,
        [SlotNo] INT NOT NULL, [ColorWayGridColumnId] INT NOT NULL,
        [LabelColorway] INT NULL,
        [LabelPhoto] INT NULL
    );

    SET @sql = N'
    INSERT INTO #DwCells ([ProductReferenceID], [RowID], [HostMatchRn], [SlotNo], [ColorWayGridColumnId], [LabelColorway], [LabelPhoto])
    SELECT b.[ProductReferenceID], b.[RowID], b.[HostMatchRn], u.[SlotNo], u.[ColorWayGridColumnId], u.[LabelColorway], u.[LabelPhoto]
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID] AND dw.[RowID] = b.[RowID]
    CROSS APPLY (' + @unionSql + N') AS u
    WHERE u.[LabelColorway] IS NOT NULL OR u.[LabelPhoto] IS NOT NULL;';
    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        [ParentRowId],
        [Colorway],
        [LabelColorway], [LabelPhoto]
    )
    SELECT hr.[RowId], m.[StyleColorID], c.[LabelColorway], c.[LabelPhoto]
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';

    IF @DryRun = 0 EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID] AND hr.[HostMatchRn] = b.[HostMatchRn]
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

EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3180 / block 5223 -> Trims_ApprovalGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3360;
DECLARE @PlmTabId              INT           = 4271;
DECLARE @PlmGridId             INT           = 3180;
DECLARE @ProductGridBlockId    INT           = 5223;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Trims_Approval_3180';
DECLARE @HostAppTable          NVARCHAR(128) = N'Trims_Approval';
DECLARE @GrandchildAppTable    NVARCHAR(128) = N'Trims_ApprovalGrandColorway';
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'Colorway';
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'ParentRowId';
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7700 AS ColorWayGridColumnId, TRY_CAST([dw].[Color_1_7700_FK_pdmRGBColor] AS INT) AS [ApprovalColor] UNION ALL SELECT 2 AS SlotNo, 7722 AS ColorWayGridColumnId, TRY_CAST([dw].[Color2_7722_FK_pdmRGBColor] AS INT) AS [ApprovalColor]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

-- Column name [TableName] must match PlmDw_ImportFromDW.sql #ImportLog (same SSMS session).
CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7700),
    (2, 7722);

SET @MappingTable    = @TablePrefix + N'FieldMapping';
SET @HostTable       = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF DB_ID(@PlmDatabase) IS NULL BEGIN RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase); RETURN; END
IF DB_ID(@DwDatabase) IS NULL BEGIN RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL BEGIN RAISERROR(N'Host table dbo.%s missing.', 16, 1, @HostTable); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL BEGIN RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable); RETURN; END
IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL BEGIN RAISERROR(N'PLM table pdmStyleColorWayMapping not found.', 16, 1); RETURN; END

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
    WHERE pt.[TemplateID] = @tid AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0 BEGIN RAISERROR(N'No references in import scope.', 16, 1); RETURN; END
PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;
INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, (SELECT COUNT(*) FROM #SlotMap));

DECLARE @CntDeleteGc INT = 0, @CntDwBom INT = 0, @CntHost INT = 0, @CntDwCells INT = 0, @CntInsertGc INT = 0, @CntSkipMap INT = 0, @CntSkipHost INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [Sort] INT NULL, [HostMatchRn] INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT dw.[ProductReferenceID], dw.[RowID], dw.[Sort],
            ROW_NUMBER() OVER (PARTITION BY dw.[ProductReferenceID] ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
    )
    INSERT INTO #DwBom SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';
    EXEC sp_executesql @sql;
    SET @CntDwBom = @@ROWCOUNT;

    CREATE TABLE #HostRanked ([RowId] INT NOT NULL PRIMARY KEY, [ReferenceId] INT NOT NULL, [HostMatchRn] INT NOT NULL);
    SET @sql = N'
    ;WITH h AS (
        SELECT h.[RowId], h.[ReferenceId],
            ROW_NUMBER() OVER (PARTITION BY h.[ReferenceId] ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';
    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    CREATE TABLE #DwCells (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [HostMatchRn] INT NOT NULL,
        [SlotNo] INT NOT NULL, [ColorWayGridColumnId] INT NOT NULL,
        [ApprovalColor] INT NULL
    );

    SET @sql = N'
    INSERT INTO #DwCells ([ProductReferenceID], [RowID], [HostMatchRn], [SlotNo], [ColorWayGridColumnId], [ApprovalColor])
    SELECT b.[ProductReferenceID], b.[RowID], b.[HostMatchRn], u.[SlotNo], u.[ColorWayGridColumnId], u.[ApprovalColor]
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID] AND dw.[RowID] = b.[RowID]
    CROSS APPLY (' + @unionSql + N') AS u
    WHERE u.[ApprovalColor] IS NOT NULL;';
    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        [ParentRowId],
        [Colorway],
        [ApprovalColor]
    )
    SELECT hr.[RowId], m.[StyleColorID], c.[ApprovalColor]
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';

    IF @DryRun = 0 EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID] AND hr.[HostMatchRn] = b.[HostMatchRn]
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

EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3183 / block 5083 -> Fabric_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3360;
DECLARE @PlmTabId              INT           = 4256;
DECLARE @PlmGridId             INT           = 3183;
DECLARE @ProductGridBlockId    INT           = 5083;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Fabric_BOM_prod_20_Colorways_3183';
DECLARE @HostAppTable          NVARCHAR(128) = N'Fabric_BOM_prod';
DECLARE @GrandchildAppTable    NVARCHAR(128) = N'Fabric_BOM_prodGrandColorway';
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'Colorway';
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'ParentRowId';
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7851 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7851_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status1_7852_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 2 AS SlotNo, 7853 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7853_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status2_7854_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 3 AS SlotNo, 7855 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7855_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status3_7856_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 4 AS SlotNo, 7857 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7857_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status4_7858_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 5 AS SlotNo, 7859 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7859_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status5_7860_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 6 AS SlotNo, 7861 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7861_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status6_7862_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 7 AS SlotNo, 7863 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7863_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status7_7864_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 8 AS SlotNo, 7865 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7865_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status8_7866_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 9 AS SlotNo, 7867 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7867_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status9_7868_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 10 AS SlotNo, 7869 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7869_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status10_7870_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 11 AS SlotNo, 7955 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_11_7955_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status11_7925_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 12 AS SlotNo, 7956 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_12_7956_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status12_7926_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 13 AS SlotNo, 7957 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_13_7957_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status13_7927_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 14 AS SlotNo, 7958 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_14_7958_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status14_7928_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 15 AS SlotNo, 7959 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_15_7959_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status15_7929_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 16 AS SlotNo, 7960 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_16_7960_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status16_7930_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 17 AS SlotNo, 7961 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_17_7961_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status17_7931_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 18 AS SlotNo, 7962 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_18_7962_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status18_7932_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 19 AS SlotNo, 7963 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_19_7963_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status19_7933_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 20 AS SlotNo, 7964 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_20_7964_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status20_7934_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

-- Column name [TableName] must match PlmDw_ImportFromDW.sql #ImportLog (same SSMS session).
CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7851),
    (2, 7853),
    (3, 7855),
    (4, 7857),
    (5, 7859),
    (6, 7861),
    (7, 7863),
    (8, 7865),
    (9, 7867),
    (10, 7869),
    (11, 7955),
    (12, 7956),
    (13, 7957),
    (14, 7958),
    (15, 7959),
    (16, 7960),
    (17, 7961),
    (18, 7962),
    (19, 7963),
    (20, 7964);

SET @MappingTable    = @TablePrefix + N'FieldMapping';
SET @HostTable       = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF DB_ID(@PlmDatabase) IS NULL BEGIN RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase); RETURN; END
IF DB_ID(@DwDatabase) IS NULL BEGIN RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL BEGIN RAISERROR(N'Host table dbo.%s missing.', 16, 1, @HostTable); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL BEGIN RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable); RETURN; END
IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL BEGIN RAISERROR(N'PLM table pdmStyleColorWayMapping not found.', 16, 1); RETURN; END

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
    WHERE pt.[TemplateID] = @tid AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0 BEGIN RAISERROR(N'No references in import scope.', 16, 1); RETURN; END
PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;
INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, (SELECT COUNT(*) FROM #SlotMap));

DECLARE @CntDeleteGc INT = 0, @CntDwBom INT = 0, @CntHost INT = 0, @CntDwCells INT = 0, @CntInsertGc INT = 0, @CntSkipMap INT = 0, @CntSkipHost INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [Sort] INT NULL, [HostMatchRn] INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT dw.[ProductReferenceID], dw.[RowID], dw.[Sort],
            ROW_NUMBER() OVER (PARTITION BY dw.[ProductReferenceID] ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
    )
    INSERT INTO #DwBom SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';
    EXEC sp_executesql @sql;
    SET @CntDwBom = @@ROWCOUNT;

    CREATE TABLE #HostRanked ([RowId] INT NOT NULL PRIMARY KEY, [ReferenceId] INT NOT NULL, [HostMatchRn] INT NOT NULL);
    SET @sql = N'
    ;WITH h AS (
        SELECT h.[RowId], h.[ReferenceId],
            ROW_NUMBER() OVER (PARTITION BY h.[ReferenceId] ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';
    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    CREATE TABLE #DwCells (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [HostMatchRn] INT NOT NULL,
        [SlotNo] INT NOT NULL, [ColorWayGridColumnId] INT NOT NULL,
        [FabricColorway] INT NULL,
        [FabricColorwayStatus] INT NULL
    );

    SET @sql = N'
    INSERT INTO #DwCells ([ProductReferenceID], [RowID], [HostMatchRn], [SlotNo], [ColorWayGridColumnId], [FabricColorway], [FabricColorwayStatus])
    SELECT b.[ProductReferenceID], b.[RowID], b.[HostMatchRn], u.[SlotNo], u.[ColorWayGridColumnId], u.[FabricColorway], u.[FabricColorwayStatus]
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID] AND dw.[RowID] = b.[RowID]
    CROSS APPLY (' + @unionSql + N') AS u
    WHERE u.[FabricColorway] IS NOT NULL OR u.[FabricColorwayStatus] IS NOT NULL;';
    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        [ParentRowId],
        [Colorway],
        [FabricColorway], [FabricColorwayStatus]
    )
    SELECT hr.[RowId], m.[StyleColorID], c.[FabricColorway], c.[FabricColorwayStatus]
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';

    IF @DryRun = 0 EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID] AND hr.[HostMatchRn] = b.[HostMatchRn]
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

EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';