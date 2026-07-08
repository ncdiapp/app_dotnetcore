
-- =============================================================================
-- 6_PlmDw_CleanupBomColorwayStaging.sql - Template 3360
-- Optional legacy cleanup: drops host Colorway_N / ImageN if present from older imports.
-- =============================================================================

-- ----- Host Artwork_BOM_prod -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- <<< USER SETTINGS â€” generator patches from dwTabImportConfig.json >>>
DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';
DECLARE @HostAppTable    NVARCHAR(128) = N'Artwork_BOM_prod';   -- e.g. Artwork_BOM_prod
DECLARE @PlmTabId        INT           = 4246;   -- host tab id (e.g. 4246) for Tab_{id} integration lookup
DECLARE @DryRun          BIT           = 0;
DECLARE @RequireGrandchildRows BIT     = 1;     -- refuse cleanup if grandchild empty

DECLARE @MappingTable    NVARCHAR(128);
DECLARE @HostTable       NVARCHAR(128);
DECLARE @GrandchildTable NVARCHAR(128);
DECLARE @TxIntegrationId NVARCHAR(64);
DECLARE @sql             NVARCHAR(MAX);
DECLARE @Cnt             INT;

IF @HostAppTable IS NULL OR LTRIM(RTRIM(@HostAppTable)) = N''
BEGIN
    RAISERROR(N'@HostAppTable is required.', 16, 1);
    RETURN;
END

IF @PlmTabId IS NULL
BEGIN
    RAISERROR(N'@PlmTabId is required.', 16, 1);
    RETURN;
END

SET @MappingTable = @TablePrefix + N'FieldMapping';
SET @HostTable    = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @HostAppTable + N'GrandColorway';
SET @TxIntegrationId = N'Tab_' + CAST(@PlmTabId AS NVARCHAR(20));

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Host table dbo.%s not found.', 16, 1, @HostTable);
    RETURN;
END

IF @RequireGrandchildRows = 1
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s not found. Run step 4 first.', 16, 1, @GrandchildTable);
        RETURN;
    END
    SET @sql = N'SELECT @cnt = COUNT(*) FROM dbo.' + QUOTENAME(@GrandchildTable) + N';';
    EXEC sp_executesql @sql, N'@cnt INT OUTPUT', @cnt = @Cnt OUTPUT;
    IF @Cnt = 0
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s is empty â€” aborting cleanup. Set @RequireGrandchildRows = 0 to force.', 16, 1, @GrandchildTable);
        RETURN;
    END
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- Delete DW slot mapping rows (not used by APP transaction after pivot import)
    SET @sql = N'
    DELETE FROM dbo.' + QUOTENAME(@MappingTable) + N'
    WHERE [AppTableName] = @hostTable
      AND [FieldKind] IN (N''BomColorwayDwSlot'', N''BomColorwaySlot'');';
    EXEC sp_executesql @sql, N'@hostTable NVARCHAR(128)', @hostTable = @HostTable;

    -- Remove staging fields from transaction metadata (columns may already be dropped)
    DELETE f
    FROM dbo.AppTransactionField AS f
    INNER JOIN dbo.AppTransactionUnit AS u ON u.TransactionUnitID = f.TransactionUnitID
    INNER JOIN dbo.AppTransaction AS t ON t.TransactionID = u.TransactionID
    WHERE u.DataBaseTableName = @HostTable
      AND t.IntegrationId = @TxIntegrationId
      AND (
            f.DataBaseFieldName LIKE N''Colorway[_]%'' ESCAPE N''\''
         OR f.DataBaseFieldName LIKE N''Image[0-9]%''
      );

    -- Drop staging columns from host (Colorway_N, ImageN)
    DECLARE @col SYSNAME;
    DECLARE col_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT c.name
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + @HostTable)
          AND (
                c.name LIKE N'Colorway[_]%' ESCAPE N'\'
             OR c.name LIKE N'Image[0-9]%'
          )
        ORDER BY c.column_id;

    OPEN col_cur;
    FETCH NEXT FROM col_cur INTO @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@HostTable) + N' DROP COLUMN ' + QUOTENAME(@col) + N';';
        IF @DryRun = 1
            PRINT N'[DryRun] ' + @sql;
        ELSE
            EXEC sp_executesql @sql;
        FETCH NEXT FROM col_cur INTO @col;
    END
    CLOSE col_cur;
    DEALLOCATE col_cur;

    IF @DryRun = 1
        ROLLBACK TRANSACTION;
    ELSE
        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'CleanupBomColorwayStaging failed for %s (line %d): %s', 16, 1, @HostTable, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

PRINT N'CleanupBomColorwayStaging completed for ' + @HostTable + N'.';
PRINT N'IMPORTANT: Refresh tenant schema cache after this script (restart AppAI.Web or run Blueprint Execute / Refresh Caches).';

GO

-- ----- Host Trim_BOM_prod -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- <<< USER SETTINGS â€” generator patches from dwTabImportConfig.json >>>
DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';
DECLARE @HostAppTable    NVARCHAR(128) = N'Trim_BOM_prod';   -- e.g. Artwork_BOM_prod
DECLARE @PlmTabId        INT           = 4256;   -- host tab id (e.g. 4246) for Tab_{id} integration lookup
DECLARE @DryRun          BIT           = 0;
DECLARE @RequireGrandchildRows BIT     = 1;     -- refuse cleanup if grandchild empty

DECLARE @MappingTable    NVARCHAR(128);
DECLARE @HostTable       NVARCHAR(128);
DECLARE @GrandchildTable NVARCHAR(128);
DECLARE @TxIntegrationId NVARCHAR(64);
DECLARE @sql             NVARCHAR(MAX);
DECLARE @Cnt             INT;

IF @HostAppTable IS NULL OR LTRIM(RTRIM(@HostAppTable)) = N''
BEGIN
    RAISERROR(N'@HostAppTable is required.', 16, 1);
    RETURN;
END

IF @PlmTabId IS NULL
BEGIN
    RAISERROR(N'@PlmTabId is required.', 16, 1);
    RETURN;
END

SET @MappingTable = @TablePrefix + N'FieldMapping';
SET @HostTable    = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @HostAppTable + N'GrandColorway';
SET @TxIntegrationId = N'Tab_' + CAST(@PlmTabId AS NVARCHAR(20));

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Host table dbo.%s not found.', 16, 1, @HostTable);
    RETURN;
END

IF @RequireGrandchildRows = 1
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s not found. Run step 4 first.', 16, 1, @GrandchildTable);
        RETURN;
    END
    SET @sql = N'SELECT @cnt = COUNT(*) FROM dbo.' + QUOTENAME(@GrandchildTable) + N';';
    EXEC sp_executesql @sql, N'@cnt INT OUTPUT', @cnt = @Cnt OUTPUT;
    IF @Cnt = 0
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s is empty â€” aborting cleanup. Set @RequireGrandchildRows = 0 to force.', 16, 1, @GrandchildTable);
        RETURN;
    END
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- Delete DW slot mapping rows (not used by APP transaction after pivot import)
    SET @sql = N'
    DELETE FROM dbo.' + QUOTENAME(@MappingTable) + N'
    WHERE [AppTableName] = @hostTable
      AND [FieldKind] IN (N''BomColorwayDwSlot'', N''BomColorwaySlot'');';
    EXEC sp_executesql @sql, N'@hostTable NVARCHAR(128)', @hostTable = @HostTable;

    -- Remove staging fields from transaction metadata (columns may already be dropped)
    DELETE f
    FROM dbo.AppTransactionField AS f
    INNER JOIN dbo.AppTransactionUnit AS u ON u.TransactionUnitID = f.TransactionUnitID
    INNER JOIN dbo.AppTransaction AS t ON t.TransactionID = u.TransactionID
    WHERE u.DataBaseTableName = @HostTable
      AND t.IntegrationId = @TxIntegrationId
      AND (
            f.DataBaseFieldName LIKE N''Colorway[_]%'' ESCAPE N''\''
         OR f.DataBaseFieldName LIKE N''Image[0-9]%''
      );

    -- Drop staging columns from host (Colorway_N, ImageN)
    DECLARE @col SYSNAME;
    DECLARE col_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT c.name
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + @HostTable)
          AND (
                c.name LIKE N'Colorway[_]%' ESCAPE N'\'
             OR c.name LIKE N'Image[0-9]%'
          )
        ORDER BY c.column_id;

    OPEN col_cur;
    FETCH NEXT FROM col_cur INTO @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@HostTable) + N' DROP COLUMN ' + QUOTENAME(@col) + N';';
        IF @DryRun = 1
            PRINT N'[DryRun] ' + @sql;
        ELSE
            EXEC sp_executesql @sql;
        FETCH NEXT FROM col_cur INTO @col;
    END
    CLOSE col_cur;
    DEALLOCATE col_cur;

    IF @DryRun = 1
        ROLLBACK TRANSACTION;
    ELSE
        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'CleanupBomColorwayStaging failed for %s (line %d): %s', 16, 1, @HostTable, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

PRINT N'CleanupBomColorwayStaging completed for ' + @HostTable + N'.';
PRINT N'IMPORTANT: Refresh tenant schema cache after this script (restart AppAI.Web or run Blueprint Execute / Refresh Caches).';

GO

-- ----- Host Label_BOM_prod -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- <<< USER SETTINGS â€” generator patches from dwTabImportConfig.json >>>
DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';
DECLARE @HostAppTable    NVARCHAR(128) = N'Label_BOM_prod';   -- e.g. Artwork_BOM_prod
DECLARE @PlmTabId        INT           = 4257;   -- host tab id (e.g. 4246) for Tab_{id} integration lookup
DECLARE @DryRun          BIT           = 0;
DECLARE @RequireGrandchildRows BIT     = 1;     -- refuse cleanup if grandchild empty

DECLARE @MappingTable    NVARCHAR(128);
DECLARE @HostTable       NVARCHAR(128);
DECLARE @GrandchildTable NVARCHAR(128);
DECLARE @TxIntegrationId NVARCHAR(64);
DECLARE @sql             NVARCHAR(MAX);
DECLARE @Cnt             INT;

IF @HostAppTable IS NULL OR LTRIM(RTRIM(@HostAppTable)) = N''
BEGIN
    RAISERROR(N'@HostAppTable is required.', 16, 1);
    RETURN;
END

IF @PlmTabId IS NULL
BEGIN
    RAISERROR(N'@PlmTabId is required.', 16, 1);
    RETURN;
END

SET @MappingTable = @TablePrefix + N'FieldMapping';
SET @HostTable    = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @HostAppTable + N'GrandColorway';
SET @TxIntegrationId = N'Tab_' + CAST(@PlmTabId AS NVARCHAR(20));

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Host table dbo.%s not found.', 16, 1, @HostTable);
    RETURN;
END

IF @RequireGrandchildRows = 1
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s not found. Run step 4 first.', 16, 1, @GrandchildTable);
        RETURN;
    END
    SET @sql = N'SELECT @cnt = COUNT(*) FROM dbo.' + QUOTENAME(@GrandchildTable) + N';';
    EXEC sp_executesql @sql, N'@cnt INT OUTPUT', @cnt = @Cnt OUTPUT;
    IF @Cnt = 0
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s is empty â€” aborting cleanup. Set @RequireGrandchildRows = 0 to force.', 16, 1, @GrandchildTable);
        RETURN;
    END
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- Delete DW slot mapping rows (not used by APP transaction after pivot import)
    SET @sql = N'
    DELETE FROM dbo.' + QUOTENAME(@MappingTable) + N'
    WHERE [AppTableName] = @hostTable
      AND [FieldKind] IN (N''BomColorwayDwSlot'', N''BomColorwaySlot'');';
    EXEC sp_executesql @sql, N'@hostTable NVARCHAR(128)', @hostTable = @HostTable;

    -- Remove staging fields from transaction metadata (columns may already be dropped)
    DELETE f
    FROM dbo.AppTransactionField AS f
    INNER JOIN dbo.AppTransactionUnit AS u ON u.TransactionUnitID = f.TransactionUnitID
    INNER JOIN dbo.AppTransaction AS t ON t.TransactionID = u.TransactionID
    WHERE u.DataBaseTableName = @HostTable
      AND t.IntegrationId = @TxIntegrationId
      AND (
            f.DataBaseFieldName LIKE N''Colorway[_]%'' ESCAPE N''\''
         OR f.DataBaseFieldName LIKE N''Image[0-9]%''
      );

    -- Drop staging columns from host (Colorway_N, ImageN)
    DECLARE @col SYSNAME;
    DECLARE col_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT c.name
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + @HostTable)
          AND (
                c.name LIKE N'Colorway[_]%' ESCAPE N'\'
             OR c.name LIKE N'Image[0-9]%'
          )
        ORDER BY c.column_id;

    OPEN col_cur;
    FETCH NEXT FROM col_cur INTO @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@HostTable) + N' DROP COLUMN ' + QUOTENAME(@col) + N';';
        IF @DryRun = 1
            PRINT N'[DryRun] ' + @sql;
        ELSE
            EXEC sp_executesql @sql;
        FETCH NEXT FROM col_cur INTO @col;
    END
    CLOSE col_cur;
    DEALLOCATE col_cur;

    IF @DryRun = 1
        ROLLBACK TRANSACTION;
    ELSE
        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'CleanupBomColorwayStaging failed for %s (line %d): %s', 16, 1, @HostTable, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

PRINT N'CleanupBomColorwayStaging completed for ' + @HostTable + N'.';
PRINT N'IMPORTANT: Refresh tenant schema cache after this script (restart AppAI.Web or run Blueprint Execute / Refresh Caches).';

GO

-- ----- Host Trims_Approval -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- <<< USER SETTINGS â€” generator patches from dwTabImportConfig.json >>>
DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';
DECLARE @HostAppTable    NVARCHAR(128) = N'Trims_Approval';   -- e.g. Artwork_BOM_prod
DECLARE @PlmTabId        INT           = 4271;   -- host tab id (e.g. 4246) for Tab_{id} integration lookup
DECLARE @DryRun          BIT           = 0;
DECLARE @RequireGrandchildRows BIT     = 1;     -- refuse cleanup if grandchild empty

DECLARE @MappingTable    NVARCHAR(128);
DECLARE @HostTable       NVARCHAR(128);
DECLARE @GrandchildTable NVARCHAR(128);
DECLARE @TxIntegrationId NVARCHAR(64);
DECLARE @sql             NVARCHAR(MAX);
DECLARE @Cnt             INT;

IF @HostAppTable IS NULL OR LTRIM(RTRIM(@HostAppTable)) = N''
BEGIN
    RAISERROR(N'@HostAppTable is required.', 16, 1);
    RETURN;
END

IF @PlmTabId IS NULL
BEGIN
    RAISERROR(N'@PlmTabId is required.', 16, 1);
    RETURN;
END

SET @MappingTable = @TablePrefix + N'FieldMapping';
SET @HostTable    = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @HostAppTable + N'GrandColorway';
SET @TxIntegrationId = N'Tab_' + CAST(@PlmTabId AS NVARCHAR(20));

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Host table dbo.%s not found.', 16, 1, @HostTable);
    RETURN;
END

IF @RequireGrandchildRows = 1
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s not found. Run step 4 first.', 16, 1, @GrandchildTable);
        RETURN;
    END
    SET @sql = N'SELECT @cnt = COUNT(*) FROM dbo.' + QUOTENAME(@GrandchildTable) + N';';
    EXEC sp_executesql @sql, N'@cnt INT OUTPUT', @cnt = @Cnt OUTPUT;
    IF @Cnt = 0
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s is empty â€” aborting cleanup. Set @RequireGrandchildRows = 0 to force.', 16, 1, @GrandchildTable);
        RETURN;
    END
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- Delete DW slot mapping rows (not used by APP transaction after pivot import)
    SET @sql = N'
    DELETE FROM dbo.' + QUOTENAME(@MappingTable) + N'
    WHERE [AppTableName] = @hostTable
      AND [FieldKind] IN (N''BomColorwayDwSlot'', N''BomColorwaySlot'');';
    EXEC sp_executesql @sql, N'@hostTable NVARCHAR(128)', @hostTable = @HostTable;

    -- Remove staging fields from transaction metadata (columns may already be dropped)
    DELETE f
    FROM dbo.AppTransactionField AS f
    INNER JOIN dbo.AppTransactionUnit AS u ON u.TransactionUnitID = f.TransactionUnitID
    INNER JOIN dbo.AppTransaction AS t ON t.TransactionID = u.TransactionID
    WHERE u.DataBaseTableName = @HostTable
      AND t.IntegrationId = @TxIntegrationId
      AND (
            f.DataBaseFieldName LIKE N''Colorway[_]%'' ESCAPE N''\''
         OR f.DataBaseFieldName LIKE N''Image[0-9]%''
      );

    -- Drop staging columns from host (Colorway_N, ImageN)
    DECLARE @col SYSNAME;
    DECLARE col_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT c.name
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + @HostTable)
          AND (
                c.name LIKE N'Colorway[_]%' ESCAPE N'\'
             OR c.name LIKE N'Image[0-9]%'
          )
        ORDER BY c.column_id;

    OPEN col_cur;
    FETCH NEXT FROM col_cur INTO @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@HostTable) + N' DROP COLUMN ' + QUOTENAME(@col) + N';';
        IF @DryRun = 1
            PRINT N'[DryRun] ' + @sql;
        ELSE
            EXEC sp_executesql @sql;
        FETCH NEXT FROM col_cur INTO @col;
    END
    CLOSE col_cur;
    DEALLOCATE col_cur;

    IF @DryRun = 1
        ROLLBACK TRANSACTION;
    ELSE
        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'CleanupBomColorwayStaging failed for %s (line %d): %s', 16, 1, @HostTable, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

PRINT N'CleanupBomColorwayStaging completed for ' + @HostTable + N'.';
PRINT N'IMPORTANT: Refresh tenant schema cache after this script (restart AppAI.Web or run Blueprint Execute / Refresh Caches).';

GO

-- ----- Host Fabric_BOM_prod -----

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- <<< USER SETTINGS â€” generator patches from dwTabImportConfig.json >>>
DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';
DECLARE @HostAppTable    NVARCHAR(128) = N'Fabric_BOM_prod';   -- e.g. Artwork_BOM_prod
DECLARE @PlmTabId        INT           = 4256;   -- host tab id (e.g. 4246) for Tab_{id} integration lookup
DECLARE @DryRun          BIT           = 0;
DECLARE @RequireGrandchildRows BIT     = 1;     -- refuse cleanup if grandchild empty

DECLARE @MappingTable    NVARCHAR(128);
DECLARE @HostTable       NVARCHAR(128);
DECLARE @GrandchildTable NVARCHAR(128);
DECLARE @TxIntegrationId NVARCHAR(64);
DECLARE @sql             NVARCHAR(MAX);
DECLARE @Cnt             INT;

IF @HostAppTable IS NULL OR LTRIM(RTRIM(@HostAppTable)) = N''
BEGIN
    RAISERROR(N'@HostAppTable is required.', 16, 1);
    RETURN;
END

IF @PlmTabId IS NULL
BEGIN
    RAISERROR(N'@PlmTabId is required.', 16, 1);
    RETURN;
END

SET @MappingTable = @TablePrefix + N'FieldMapping';
SET @HostTable    = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @HostAppTable + N'GrandColorway';
SET @TxIntegrationId = N'Tab_' + CAST(@PlmTabId AS NVARCHAR(20));

IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Host table dbo.%s not found.', 16, 1, @HostTable);
    RETURN;
END

IF @RequireGrandchildRows = 1
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s not found. Run step 4 first.', 16, 1, @GrandchildTable);
        RETURN;
    END
    SET @sql = N'SELECT @cnt = COUNT(*) FROM dbo.' + QUOTENAME(@GrandchildTable) + N';';
    EXEC sp_executesql @sql, N'@cnt INT OUTPUT', @cnt = @Cnt OUTPUT;
    IF @Cnt = 0
    BEGIN
        RAISERROR(N'Grandchild table dbo.%s is empty â€” aborting cleanup. Set @RequireGrandchildRows = 0 to force.', 16, 1, @GrandchildTable);
        RETURN;
    END
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- Delete DW slot mapping rows (not used by APP transaction after pivot import)
    SET @sql = N'
    DELETE FROM dbo.' + QUOTENAME(@MappingTable) + N'
    WHERE [AppTableName] = @hostTable
      AND [FieldKind] IN (N''BomColorwayDwSlot'', N''BomColorwaySlot'');';
    EXEC sp_executesql @sql, N'@hostTable NVARCHAR(128)', @hostTable = @HostTable;

    -- Remove staging fields from transaction metadata (columns may already be dropped)
    DELETE f
    FROM dbo.AppTransactionField AS f
    INNER JOIN dbo.AppTransactionUnit AS u ON u.TransactionUnitID = f.TransactionUnitID
    INNER JOIN dbo.AppTransaction AS t ON t.TransactionID = u.TransactionID
    WHERE u.DataBaseTableName = @HostTable
      AND t.IntegrationId = @TxIntegrationId
      AND (
            f.DataBaseFieldName LIKE N''Colorway[_]%'' ESCAPE N''\''
         OR f.DataBaseFieldName LIKE N''Image[0-9]%''
      );

    -- Drop staging columns from host (Colorway_N, ImageN)
    DECLARE @col SYSNAME;
    DECLARE col_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT c.name
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + @HostTable)
          AND (
                c.name LIKE N'Colorway[_]%' ESCAPE N'\'
             OR c.name LIKE N'Image[0-9]%'
          )
        ORDER BY c.column_id;

    OPEN col_cur;
    FETCH NEXT FROM col_cur INTO @col;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@HostTable) + N' DROP COLUMN ' + QUOTENAME(@col) + N';';
        IF @DryRun = 1
            PRINT N'[DryRun] ' + @sql;
        ELSE
            EXEC sp_executesql @sql;
        FETCH NEXT FROM col_cur INTO @col;
    END
    CLOSE col_cur;
    DEALLOCATE col_cur;

    IF @DryRun = 1
        ROLLBACK TRANSACTION;
    ELSE
        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'CleanupBomColorwayStaging failed for %s (line %d): %s', 16, 1, @HostTable, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

PRINT N'CleanupBomColorwayStaging completed for ' + @HostTable + N'.';
PRINT N'IMPORTANT: Refresh tenant schema cache after this script (restart AppAI.Web or run Blueprint Execute / Refresh Caches).';
