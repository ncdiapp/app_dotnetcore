-- =============================================================================
-- Phase A probe: APP tenant DB — FieldMapping + table inventory for Search import.
-- Folder: ImportPLMSearchView/source/
-- Prerequisite: Template DW import already ran (Plm_* tables + FieldMapping exist).
-- =============================================================================

SET NOCOUNT ON;

DECLARE @TablePrefix NVARCHAR(32) = N'Plm_';  -- <<< USER SETTING (must match DW import)
DECLARE @MappingTable NVARCHAR(128) = @TablePrefix + N'FieldMapping';

PRINT '=== FieldMapping table exists? ===';
SELECT
    OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') AS MappingObjectId,
    @MappingTable AS MappingTableName;

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NULL
BEGIN
    RAISERROR(N'FieldMapping table %s not found. Run Template DW import first.', 16, 1, @MappingTable);
    RETURN;
END

PRINT '=== APP tables with FieldMapping rows (by AppTableName) ===';
DECLARE @sql NVARCHAR(MAX) = N'
SELECT
    fm.AppTableName,
    COUNT(*) AS ColumnCount,
    COUNT(DISTINCT fm.PlmTabId) AS DistinctTabIds,
    COUNT(DISTINCT fm.PlmSubItemId) AS DistinctSubItemIds,
    COUNT(DISTINCT fm.PlmMetaColumnId) AS DistinctMetaColumnIds,
    SUM(CASE WHEN fm.FieldKind = N''TabField'' THEN 1 ELSE 0 END) AS TabFieldCount,
    SUM(CASE WHEN fm.FieldKind = N''GridColumn'' THEN 1 ELSE 0 END) AS GridColumnCount,
    SUM(CASE WHEN fm.FieldKind = N''ReferenceField'' THEN 1 ELSE 0 END) AS ReferenceFieldCount
FROM dbo.' + QUOTENAME(@MappingTable) + N' fm
GROUP BY fm.AppTableName
ORDER BY fm.AppTableName;';
EXEC sp_executesql @sql;

PRINT '=== TabId → AppTableName (sibling tables) ===';
SET @sql = N'
SELECT DISTINCT
    fm.PlmTabId,
    fm.AppTableName,
    fm.FieldKind
FROM dbo.' + QUOTENAME(@MappingTable) + N' fm
WHERE fm.PlmTabId IS NOT NULL
  AND fm.FieldKind IN (N''TabField'', N''ReferenceField'')
ORDER BY fm.PlmTabId, fm.AppTableName;';
EXEC sp_executesql @sql;

PRINT '=== SubItemId → candidate App tables (ambiguous mappings) ===';
SET @sql = N'
SELECT
    fm.PlmSubItemId,
    COUNT(DISTINCT fm.AppTableName) AS AppTableCount,
    STRING_AGG(CAST(fm.AppTableName AS NVARCHAR(MAX)), N'', '') AS AppTables
FROM dbo.' + QUOTENAME(@MappingTable) + N' fm
WHERE fm.PlmSubItemId IS NOT NULL
GROUP BY fm.PlmSubItemId
HAVING COUNT(DISTINCT fm.AppTableName) > 1
ORDER BY fm.PlmSubItemId;';
EXEC sp_executesql @sql;

PRINT '=== MetaColumnId → App column (grid columns) ===';
SET @sql = N'
SELECT
    fm.PlmMetaColumnId,
    fm.AppTableName,
    fm.AppColumnName,
    fm.PlmTabId,
    fm.PlmGridId,
    fm.PlmControlType,
    fm.PlmEntityId,
    fm.FieldKind
FROM dbo.' + QUOTENAME(@MappingTable) + N' fm
WHERE fm.PlmMetaColumnId IS NOT NULL
ORDER BY fm.PlmMetaColumnId, fm.AppTableName;';
EXEC sp_executesql @sql;

PRINT '=== Root table columns (ReferenceBasicInfo) ===';
DECLARE @RootTable NVARCHAR(128) = @TablePrefix + N'ReferenceBasicInfo';
IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NOT NULL
BEGIN
    SET @sql = N'
SELECT c.COLUMN_NAME, c.DATA_TYPE, c.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = N''dbo'' AND c.TABLE_NAME = @RootTable
ORDER BY c.ORDINAL_POSITION;';
    EXEC sp_executesql @sql, N'@RootTable NVARCHAR(128)', @RootTable = @RootTable;
END
ELSE
    PRINT 'Root table not found: ' + @RootTable;

PRINT '=== Resolve SubItemIds (paste list from PLM probe §6) ===';
-- Agent: populate #WantedSubItems from PLM probe output before running this block.
IF OBJECT_ID(N'tempdb..#WantedSubItems') IS NOT NULL DROP TABLE #WantedSubItems;
CREATE TABLE #WantedSubItems (SubItemId INT NOT NULL PRIMARY KEY);
-- INSERT INTO #WantedSubItems VALUES (1234), (5678);  -- <<< AGENT fills from PLM probe

IF EXISTS (SELECT 1 FROM #WantedSubItems)
BEGIN
    SET @sql = N'
SELECT
    w.SubItemId,
    fm.AppTableName,
    fm.AppColumnName,
    fm.PlmTabId,
    fm.FieldKind,
    fm.PlmControlType,
    fm.PlmEntityId
FROM #WantedSubItems w
LEFT JOIN dbo.' + QUOTENAME(@MappingTable) + N' fm
    ON fm.PlmSubItemId = w.SubItemId
ORDER BY w.SubItemId, fm.AppTableName;';
    EXEC sp_executesql @sql;
END
ELSE
    PRINT 'SKIP: #WantedSubItems empty — agent fills after PLM probe.';

PRINT '=== Resolve MetaColumnIds (paste list from PLM probe §4) ===';
IF OBJECT_ID(N'tempdb..#WantedMetaColumns') IS NOT NULL DROP TABLE #WantedMetaColumns;
CREATE TABLE #WantedMetaColumns (MetaColumnId INT NOT NULL PRIMARY KEY);
-- INSERT INTO #WantedMetaColumns VALUES (44), (55);  -- <<< AGENT fills from PLM probe

IF EXISTS (SELECT 1 FROM #WantedMetaColumns)
BEGIN
    SET @sql = N'
SELECT
    w.MetaColumnId,
    fm.AppTableName,
    fm.AppColumnName,
    fm.PlmTabId,
    fm.PlmGridId,
    fm.FieldKind,
    fm.PlmControlType,
    fm.PlmEntityId
FROM #WantedMetaColumns w
LEFT JOIN dbo.' + QUOTENAME(@MappingTable) + N' fm
    ON fm.PlmMetaColumnId = w.MetaColumnId
ORDER BY w.MetaColumnId, fm.AppTableName;';
    EXEC sp_executesql @sql;
END
ELSE
    PRINT 'SKIP: #WantedMetaColumns empty — agent fills after PLM probe.';

PRINT '=== FieldMapping probe complete ===';
