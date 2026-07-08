
-- =============================================================================
-- 5_PlmDw_ImportBomColorwayGrandchild.sql - Template 3351
-- Pivot value columns from PLM pdmGridMetaColumn (DCU slot + DcucolumnId children).
-- =============================================================================

-- ----- BOM grid 3161 / block 3974 -> Fabric_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3351;
DECLARE @PlmTabId              INT           = 4229;
DECLARE @PlmGridId             INT           = 3161;
DECLARE @ProductGridBlockId    INT           = 3974;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Fabric_BOM_prod_10_Colorways_3161';
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
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7053 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7053_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status1_7604_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 2 AS SlotNo, 7054 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7054_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status2_7672_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 3 AS SlotNo, 7055 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7055_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status3_7673_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 4 AS SlotNo, 7196 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7196_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status4_7674_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 5 AS SlotNo, 7197 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7197_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status5_7675_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 6 AS SlotNo, 7198 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7198_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status6_7676_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 7 AS SlotNo, 7199 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7199_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status7_7677_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 8 AS SlotNo, 7200 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7200_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status8_7678_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 9 AS SlotNo, 7201 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7201_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status9_7679_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus] UNION ALL SELECT 10 AS SlotNo, 7202 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7202_FK_pdmRGBColor] AS INT) AS [FabricColorway], TRY_CAST([dw].[Status10_7680_FK_PLM_DW_UD_Rallye_Status_3667] AS INT) AS [FabricColorwayStatus]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [Detail]    NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7053),
    (2, 7054),
    (3, 7055),
    (4, 7196),
    (5, 7197),
    (6, 7198),
    (7, 7199),
    (8, 7200),
    (9, 7201),
    (10, 7202);

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
        ' + (SqlBracketName ParentRowId) + N',
        ' + (SqlBracketName Colorway) + N',
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

EXEC (N'SELECT [Step], [Detail], [RowCount] FROM #ImportLog ORDER BY [Step], [Detail];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3162 / block 3975 -> Trim_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3351;
DECLARE @PlmTabId              INT           = 4229;
DECLARE @PlmGridId             INT           = 3162;
DECLARE @ProductGridBlockId    INT           = 3975;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Trim_BOM_prod_10_Colorways_3162';
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
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7079 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7079_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 2 AS SlotNo, 7080 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7080_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 3 AS SlotNo, 7081 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7081_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 4 AS SlotNo, 7084 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7084_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 5 AS SlotNo, 7085 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7085_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 6 AS SlotNo, 7089 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7089_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 7 AS SlotNo, 7090 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7090_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 8 AS SlotNo, 7095 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7095_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 9 AS SlotNo, 7096 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7096_FK_pdmRGBColor] AS INT) AS [TrimColorway] UNION ALL SELECT 10 AS SlotNo, 7097 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7097_FK_pdmRGBColor] AS INT) AS [TrimColorway]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [Detail]    NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7079),
    (2, 7080),
    (3, 7081),
    (4, 7084),
    (5, 7085),
    (6, 7089),
    (7, 7090),
    (8, 7095),
    (9, 7096),
    (10, 7097);

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
        ' + (SqlBracketName ParentRowId) + N',
        ' + (SqlBracketName Colorway) + N',
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

EXEC (N'SELECT [Step], [Detail], [RowCount] FROM #ImportLog ORDER BY [Step], [Detail];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3163 / block 4037 -> Label_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3351;
DECLARE @PlmTabId              INT           = 4230;
DECLARE @PlmGridId             INT           = 3163;
DECLARE @ProductGridBlockId    INT           = 4037;
DECLARE @DwGridTable           NVARCHAR(256) = N'PLM_DW_Grid_Label_BOM_prod_10_Colorways_3163';
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
DECLARE @unionSql              NVARCHAR(MAX) = N'SELECT 1 AS SlotNo, 7340 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_1_7340_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_1_7205_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 2 AS SlotNo, 7341 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_2_7341_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_2_7207_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 3 AS SlotNo, 7342 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_3_7342_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_3_7209_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 4 AS SlotNo, 7343 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_4_7343_FK_pdmRGBColor] AS INT) AS [LabelColorway], TRY_CAST([dw].[Image_4_7211_FK_tblSketch] AS INT) AS [LabelPhoto] UNION ALL SELECT 5 AS SlotNo, 7344 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_5_7344_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 6 AS SlotNo, 7345 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_6_7345_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 7 AS SlotNo, 7346 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_7_7346_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 8 AS SlotNo, 7347 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_8_7347_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 9 AS SlotNo, 7348 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_9_7348_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto] UNION ALL SELECT 10 AS SlotNo, 7349 AS ColorWayGridColumnId, TRY_CAST([dw].[Colorway_10_7349_FK_pdmRGBColor] AS INT) AS [LabelColorway], CAST(NULL AS INT) AS [LabelPhoto]';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [Detail]    NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    (1, 7340),
    (2, 7341),
    (3, 7342),
    (4, 7343),
    (5, 7344),
    (6, 7345),
    (7, 7346),
    (8, 7347),
    (9, 7348),
    (10, 7349);

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
        ' + (SqlBracketName ParentRowId) + N',
        ' + (SqlBracketName Colorway) + N',
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

EXEC (N'SELECT [Step], [Detail], [RowCount] FROM #ImportLog ORDER BY [Step], [Detail];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3167 / block 5052 -> Artwork_BOM_prodGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3351;
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

CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [Detail]    NVARCHAR(256) NULL,
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
        ' + (SqlBracketName ParentRowId) + N',
        ' + (SqlBracketName Colorway) + N',
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

EXEC (N'SELECT [Step], [Detail], [RowCount] FROM #ImportLog ORDER BY [Step], [Detail];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
GO

-- ----- BOM grid 3180 / block 5223 -> Trims_ApprovalGrandColorway -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'Plm_';
DECLARE @PlmDatabase           NVARCHAR(128) = N'plm_live_20260602';
DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';
DECLARE @PlmTemplateId         INT           = 3351;
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

CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [Detail]    NVARCHAR(256) NULL,
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
        ' + (SqlBracketName ParentRowId) + N',
        ' + (SqlBracketName Colorway) + N',
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

EXEC (N'SELECT [Step], [Detail], [RowCount] FROM #ImportLog ORDER BY [Step], [Detail];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';