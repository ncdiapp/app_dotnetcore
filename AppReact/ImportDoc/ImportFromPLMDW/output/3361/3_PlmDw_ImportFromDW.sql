-- =============================================================================
-- PLM DW â†’ APP product data import (template: source/PlmDw_ImportFromDW.sql)
-- Deliverable copy: output/PlmDw_ImportFromDW.sql (see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. 1_PlmDw_Tables.sql
--   2. 2_PlmDw_FieldMapping.sql
--   3. 3_PlmDw_ImportFromDW.sql    (this file)
--   4. 4_PlmDw_ImportBlueprint.json + Phase D Execute
--   5. 5_PlmDw_ImportBomColorwayGrandchild.sql  (when BOM colorway grids detected)
--   6. 6_PlmDw_CleanupBomColorwayStaging.sql
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix       NVARCHAR(32)  = N'Plm_';               -- <<< USER SETTING
DECLARE @RootTableSuffix   NVARCHAR(128) = N'ReferenceBasicInfo'; -- <<< USER SETTING
DECLARE @DwDatabase        NVARCHAR(128) = N'plmDW';               -- <<< USER SETTING (plmDW)
DECLARE @PlmDatabase       NVARCHAR(128) = N'plm_live_20260602';                -- <<< USER SETTING (PLM source DB for pdmProductTemplate)
DECLARE @PlmTemplateId     INT           = 3361;                   -- <<< USER SETTING â€” scope refs: pdmProductTemplate.TemplateID
DECLARE @ImportMode        NVARCHAR(16)  = N'APPEND';             -- REPLACE | APPEND (APPEND skips existing ReferenceId per table)
DECLARE @ReferenceIdList   NVARCHAR(MAX) = NULL;                   -- optional pilot, e.g. N'1536,2001'
DECLARE @DryRun            BIT           = 0;

DECLARE @MappingTable      NVARCHAR(128);
DECLARE @RootTable         NVARCHAR(128);
DECLARE @RefSourceDwTable  NVARCHAR(256);
DECLARE @RefCodeDwColumn   NVARCHAR(256);
DECLARE @sql               NVARCHAR(MAX);
DECLARE @InsertCols        NVARCHAR(MAX);
DECLARE @SelectExprs       NVARCHAR(MAX);
DECLARE @RowCnt            INT;
DECLARE @AppTableName      NVARCHAR(128);
DECLARE @DwTableName       NVARCHAR(256);
DECLARE @FieldKind         NVARCHAR(16);
DECLARE @GridIdFilter      INT;
DECLARE @Step              NVARCHAR(128);

-- Drop leftover temp tables from a prior run in the same SSMS/sqlcmd connection
IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#Targets') IS NOT NULL DROP TABLE #Targets;

CREATE TABLE #ImportLog (
    [Step] NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(128) NULL,
    [RowCount] INT NOT NULL
);
CREATE TABLE #RefFilter ( [ReferenceId] INT NOT NULL PRIMARY KEY );

SET @MappingTable = @TablePrefix + N'FieldMapping';
SET @RootTable    = @TablePrefix + @RootTableSuffix;

IF DB_ID(@DwDatabase) IS NULL
BEGIN
    RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase);
    RETURN;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Mapping table dbo.%s missing. Run PlmDw_FieldMapping.sql first.', 16, 1, @MappingTable);
    RETURN;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NULL
BEGIN
    RAISERROR(N'Root table dbo.%s missing. Run PlmDw_Tables.sql first.', 16, 1, @RootTable);
    RETURN;
END

SET @sql = N'
SELECT @t = [DwTableName], @c = [DwColumnName]
FROM dbo.' + QUOTENAME(@MappingTable) + N'
WHERE [AppTableName] = @root
  AND [AppColumnName] = N''ReferenceCode''
  AND [FieldKind] = N''ReferenceField'';';
EXEC sp_executesql @sql,
    N'@root nvarchar(128), @t nvarchar(256) OUTPUT, @c nvarchar(256) OUTPUT',
    @root = @RootTable, @t = @RefSourceDwTable OUTPUT, @c = @RefCodeDwColumn OUTPUT;

IF @RefSourceDwTable IS NULL OR @RefCodeDwColumn IS NULL
BEGIN
    RAISERROR(N'ReferenceField mapping for ReferenceCode not found in %s.', 16, 1, @MappingTable);
    RETURN;
END

DECLARE @RefScopeObjId INT;
DECLARE @FullRefScopeDw NVARCHAR(512) = @DwDatabase + N'.dbo.' + @RefSourceDwTable;
SET @sql = N'SELECT @oid = OBJECT_ID(@full, N''U'');';
EXEC sp_executesql @sql, N'@full nvarchar(512), @oid int OUTPUT', @full = @FullRefScopeDw, @oid = @RefScopeObjId OUTPUT;
IF @RefScopeObjId IS NULL
BEGIN
    RAISERROR(N'Reference scope DW table %s not found.', 16, 1, @FullRefScopeDw);
    RETURN;
END

IF @ReferenceIdList IS NOT NULL AND LTRIM(RTRIM(@ReferenceIdList)) <> N''
BEGIN
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT TRY_CAST(LTRIM(RTRIM([value])) AS INT)
    FROM STRING_SPLIT(@ReferenceIdList, N',')
    WHERE TRY_CAST(LTRIM(RTRIM([value])) AS INT) IS NOT NULL;

    IF @PlmTemplateId IS NOT NULL AND @PlmTemplateId > 0 AND DB_ID(@PlmDatabase) IS NOT NULL
    BEGIN
        SET @sql = N'
        DELETE rf
        FROM #RefFilter rf
        WHERE NOT EXISTS (
            SELECT 1
            FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmProductTemplate] pt
            WHERE pt.[TemplateID] = @tid
              AND pt.[ProductReferenceID] = rf.[ReferenceId]
        );';
        EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
    END
END
ELSE IF @PlmTemplateId IS NOT NULL AND @PlmTemplateId > 0 AND DB_ID(@PlmDatabase) IS NOT NULL
BEGIN
    SET @sql = N'
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT pt.[ProductReferenceID]
    FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmProductTemplate] AS pt
    WHERE pt.[TemplateID] = @tid
      AND pt.[ProductReferenceID] IS NOT NULL
      AND EXISTS (
          SELECT 1
          FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@RefSourceDwTable) + N' AS dw
          WHERE dw.[ProductReferenceID] = pt.[ProductReferenceID]
      );';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END
ELSE
BEGIN
    SET @sql = N'
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT dw.[ProductReferenceID]
    FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@RefSourceDwTable) + N' AS dw;';
    EXEC sp_executesql @sql;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0
BEGIN
    RAISERROR(N'No references to import.', 16, 1);
    RETURN;
END

PRINT N'Import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;

CREATE TABLE #Targets (
    [AppTableName] NVARCHAR(128) NOT NULL PRIMARY KEY,
    [FieldKind] NVARCHAR(16) NOT NULL,
    [DwTableName] NVARCHAR(256) NOT NULL,
    [GridIdFilter] INT NULL
);

SET @sql = N'
INSERT INTO #Targets ([AppTableName], [FieldKind], [DwTableName], [GridIdFilter])
SELECT
    m.[AppTableName],
    CASE WHEN MAX(CASE WHEN m.[FieldKind] = N''GridColumn'' THEN 1 ELSE 0 END) = 1
         THEN N''GridColumn'' ELSE N''TabField'' END,
    MIN(m.[DwTableName]),
    MAX(m.[PlmGridId])
FROM dbo.' + QUOTENAME(@MappingTable) + N' AS m
WHERE m.[FieldKind] IN (N''TabField'', N''GridColumn'')
GROUP BY m.[AppTableName];';
EXEC sp_executesql @sql;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        DECLARE @DelTables TABLE ([Ord] INT IDENTITY(1,1), [TableName] NVARCHAR(128));
        INSERT INTO @DelTables ([TableName])
        SELECT [AppTableName] FROM #Targets WHERE [FieldKind] = N'GridColumn'
        UNION ALL
        SELECT [AppTableName] FROM #Targets WHERE [FieldKind] = N'TabField';

        DECLARE @DelName NVARCHAR(128);
        DECLARE del_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT [TableName] FROM @DelTables ORDER BY [Ord];
        OPEN del_cur;
        FETCH NEXT FROM del_cur INTO @DelName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF OBJECT_ID(N'dbo.' + QUOTENAME(@DelName), N'U') IS NOT NULL
            BEGIN
                SET @sql = N'
                DELETE tgt FROM dbo.' + QUOTENAME(@DelName) + N' AS tgt
                WHERE EXISTS (SELECT 1 FROM #RefFilter rf WHERE rf.[ReferenceId] = tgt.[ReferenceId]);';
                IF @DryRun = 0 EXEC sp_executesql @sql;
                SET @RowCnt = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
                INSERT INTO #ImportLog VALUES (N'DELETE', @DelName, @RowCnt);
            END
            FETCH NEXT FROM del_cur INTO @DelName;
        END
        CLOSE del_cur; DEALLOCATE del_cur;

        SET @sql = N'
        DELETE r FROM dbo.' + QUOTENAME(@RootTable) + N' AS r
        WHERE EXISTS (SELECT 1 FROM #RefFilter rf WHERE rf.[ReferenceId] = r.[ReferenceId]);';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        INSERT INTO #ImportLog VALUES (N'DELETE', @RootTable, CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END);
    END

    SET @Step = N'INSERT ReferenceBasicInfo';
    SET @sql = N'
    SET IDENTITY_INSERT dbo.' + QUOTENAME(@RootTable) + N' ON;
    INSERT INTO dbo.' + QUOTENAME(@RootTable) + N' (
        [ReferenceId],[ReferenceCode],[MasterReferenceId],[FolderId],
        [AppCreatedByID],[AppCreatedDate],[AppModifiedByID],[AppModifiedDate])
    SELECT rf.[ReferenceId], src.[' + REPLACE(@RefCodeDwColumn, N']', N']]') + N'],
        NULL,NULL,NULL,GETDATE(),NULL,NULL
    FROM #RefFilter rf
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@RefSourceDwTable) + N' src
        ON src.[ProductReferenceID] = rf.[ReferenceId]
    ' + CASE WHEN @ImportMode = N'APPEND' THEN N'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@RootTable) + N' r WHERE r.[ReferenceId]=rf.[ReferenceId])' ELSE N'' END + N';
    SET IDENTITY_INSERT dbo.' + QUOTENAME(@RootTable) + N' OFF;';
    IF @DryRun = 0 EXEC sp_executesql @sql;
    INSERT INTO #ImportLog VALUES (@Step, @RootTable, CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END);

    DECLARE imp_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT [AppTableName],[FieldKind],[DwTableName],[GridIdFilter]
        FROM #Targets
        ORDER BY CASE [FieldKind] WHEN N'TabField' THEN 1 ELSE 2 END, [AppTableName];
    OPEN imp_cur;
    FETCH NEXT FROM imp_cur INTO @AppTableName, @FieldKind, @DwTableName, @GridIdFilter;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID(N'dbo.' + QUOTENAME(@AppTableName), N'U') IS NULL
        BEGIN
            PRINT N'Skipped (missing): ' + @AppTableName;
            FETCH NEXT FROM imp_cur INTO @AppTableName, @FieldKind, @DwTableName, @GridIdFilter;
            CONTINUE;
        END

        SET @sql = N'
        SELECT @ic = STRING_AGG(CAST(QUOTENAME(m.[AppColumnName]) AS NVARCHAR(MAX)), N'','')
                WITHIN GROUP (ORDER BY m.[AppColumnName]),
            @sc = STRING_AGG(CAST(
                CASE
                    WHEN ty.[name] IN (N''decimal'',N''numeric'') THEN
                        N''TRY_CAST(dw.''+QUOTENAME(m.[DwColumnName])+N'' AS ''+ty.[name]
                        +N''(''+CAST(c.[precision] AS NVARCHAR(10))+N'',''+CAST(c.[scale] AS NVARCHAR(10))+N''))''
                    WHEN ty.[name] IN (N''int'',N''bigint'',N''smallint'',N''datetime'',N''datetime2'',N''date'') THEN
                        N''TRY_CAST(dw.''+QUOTENAME(m.[DwColumnName])+N'' AS ''+ty.[name]+N'')''
                    WHEN ty.[name]=N''bit'' THEN
                        N''CASE WHEN TRY_CAST(dw.''+QUOTENAME(m.[DwColumnName])+N'' AS int)=1
                            OR TRY_CAST(dw.''+QUOTENAME(m.[DwColumnName])+N'' AS nvarchar(50)) IN (N''''1'''',N''''true'''',N''''Y'''')
                         THEN CONVERT(bit,1) ELSE CONVERT(bit,0) END''
                    ELSE N''dw.''+QUOTENAME(m.[DwColumnName])
                END AS NVARCHAR(MAX)), N'','')
                WITHIN GROUP (ORDER BY m.[AppColumnName])
        FROM dbo.' + QUOTENAME(@MappingTable) + N' m
        INNER JOIN sys.columns c ON c.object_id=OBJECT_ID(N''dbo.' + REPLACE(@AppTableName, N'''', N'''''') + N''') AND c.name=m.[AppColumnName]
        INNER JOIN sys.types ty ON ty.user_type_id=c.user_type_id
        WHERE m.[AppTableName]=@app AND m.[FieldKind]=@kind;';
        EXEC sp_executesql @sql, N'@app nvarchar(128),@kind nvarchar(16),@ic nvarchar(max) OUTPUT,@sc nvarchar(max) OUTPUT',
            @app=@AppTableName,@kind=@FieldKind,@ic=@InsertCols OUTPUT,@sc=@SelectExprs OUTPUT;

        IF @InsertCols IS NULL OR @SelectExprs IS NULL
        BEGIN
            FETCH NEXT FROM imp_cur INTO @AppTableName, @FieldKind, @DwTableName, @GridIdFilter;
            CONTINUE;
        END

        IF @FieldKind = N'TabField'
        BEGIN
            SET @Step = N'INSERT Tab';
            SET @sql = N'INSERT INTO dbo.' + QUOTENAME(@AppTableName) + N' ([ReferenceId],'+@InsertCols+N')
            SELECT dw.[ProductReferenceID],'+@SelectExprs+N'
            FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwTableName) + N' dw
            INNER JOIN #RefFilter rf ON rf.[ReferenceId]=dw.[ProductReferenceID]'
            + CASE WHEN @ImportMode=N'APPEND' THEN N'
            WHERE NOT EXISTS (SELECT 1 FROM dbo.'+QUOTENAME(@AppTableName)+N' t WHERE t.[ReferenceId]=dw.[ProductReferenceID])' ELSE N'' END + N';';
        END
        ELSE
        BEGIN
            SET @Step = N'INSERT Grid';
            SET @sql = N'INSERT INTO dbo.' + QUOTENAME(@AppTableName) + N' ([ReferenceId],[Sort],'+@InsertCols+N')
            SELECT dw.[ProductReferenceID],dw.[Sort],'+@SelectExprs+N'
            FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwTableName) + N' dw
            INNER JOIN #RefFilter rf ON rf.[ReferenceId]=dw.[ProductReferenceID]'
            + CASE WHEN @GridIdFilter IS NOT NULL THEN N' WHERE dw.[GridID]='+CAST(@GridIdFilter AS NVARCHAR(20)) ELSE N'' END
            + CASE WHEN @ImportMode=N'APPEND' THEN
                CASE WHEN @GridIdFilter IS NOT NULL THEN N' AND' ELSE N' WHERE' END
                +N' NOT EXISTS (SELECT 1 FROM dbo.'+QUOTENAME(@AppTableName)+N' t WHERE t.[ReferenceId]=dw.[ProductReferenceID])' ELSE N'' END + N';';
        END

        IF @DryRun = 0 EXEC sp_executesql @sql;
        INSERT INTO #ImportLog VALUES (@Step, @AppTableName, CASE WHEN @DryRun=0 THEN @@ROWCOUNT ELSE 0 END);
        FETCH NEXT FROM imp_cur INTO @AppTableName, @FieldKind, @DwTableName, @GridIdFilter;
    END
    CLOSE imp_cur; DEALLOCATE imp_cur;

    IF @DryRun = 1 ROLLBACK TRANSACTION; ELSE COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'PlmDw_ImportFromDW failed (line %d): %s', 16, 1, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

-- EXEC avoids compile-time bind to a prior #ImportLog shape (e.g. after step 5 in same SSMS session).
EXEC (N'SELECT [Step],[TableName],[RowCount] FROM #ImportLog ORDER BY [Step],[TableName];');
PRINT N'PlmDw_ImportFromDW completed.';

