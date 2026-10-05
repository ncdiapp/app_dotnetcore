-- Phase A/B probe: PLM Tab Design layout for one or more TabIds.
-- Used by _gen_plmdw_import_sql.ps1 to emit portable formLayout (non-grid fields).
-- Set @TabIdList before run, e.g. N'4258,4272'

SET NOCOUNT ON;

DECLARE @TabIdList NVARCHAR(MAX) = NULL; -- <<< SET e.g. N'4258,4272'

IF @TabIdList IS NULL OR LTRIM(RTRIM(@TabIdList)) = N''
BEGIN
    RAISERROR(N'Set @TabIdList (comma-separated TabIDs) before running _plm_probe_tab_layout.sql', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#TabIds') IS NOT NULL DROP TABLE #TabIds;
CREATE TABLE #TabIds (TabID INT NOT NULL PRIMARY KEY);

INSERT INTO #TabIds (TabID)
SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
FROM STRING_SPLIT(@TabIdList, N',')
WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

PRINT N'=== pdmTabLayout cells (RowIndex / ColumnIndex / spans) ===';
SELECT
    l.TabID,
    l.LayoutID,
    l.RowIndex,
    l.ColumnIndex,
    ISNULL(l.RowSpan, 1) AS RowSpan,
    ISNULL(l.ColumnSpan, 1) AS ColumnSpan,
    l.Style
FROM dbo.pdmTabLayout l
INNER JOIN #TabIds t ON t.TabID = l.TabID
ORDER BY l.TabID, l.RowIndex, l.ColumnIndex, l.LayoutID;

PRINT N'=== pdmTabLayoutItem (Blocks in cells) ===';
SELECT
    l.TabID,
    li.LayoutItemID,
    li.LayoutID,
    li.BlockID,
    b.Name AS BlockName,
    ISNULL(li.Sort, 0) AS BlockSort,
    li.LabelText,
    ISNULL(li.IsChildTableContainer, 0) AS IsChildTableContainer
FROM dbo.pdmTabLayout l
INNER JOIN #TabIds t ON t.TabID = l.TabID
INNER JOIN dbo.pdmTabLayoutItem li ON li.LayoutID = l.LayoutID
LEFT JOIN dbo.pdmBlock b ON b.BlockID = li.BlockID
ORDER BY l.TabID, l.RowIndex, l.ColumnIndex, ISNULL(li.Sort, 0), li.LayoutItemID;

PRINT N'=== pdmTabLayoutSubitem (fields / grids in blocks) ===';
SELECT
    l.TabID,
    ls.LayoutSubitemID,
    ls.LayoutItemID,
    ls.SubItemID,
    ISNULL(ls.Sort, 0) AS SubItemSort,
    bsi.SubItemName,
    bsi.ControlType,
    bsi.GridID,
    ls.ImageOrMemoWidth,
    ls.ImageOrMemoHight
FROM dbo.pdmTabLayout l
INNER JOIN #TabIds t ON t.TabID = l.TabID
INNER JOIN dbo.pdmTabLayoutItem li ON li.LayoutID = l.LayoutID
INNER JOIN dbo.pdmTabLayoutSubitem ls ON ls.LayoutItemID = li.LayoutItemID
LEFT JOIN dbo.pdmBlockSubItem bsi ON bsi.SubItemID = ls.SubItemID
ORDER BY l.TabID, li.LayoutItemID, ISNULL(ls.Sort, 0), ls.LayoutSubitemID;
