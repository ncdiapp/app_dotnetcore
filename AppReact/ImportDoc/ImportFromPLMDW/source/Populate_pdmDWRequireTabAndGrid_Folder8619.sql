-- Populate pdmDWRequireTabAndGrid for all templates in FolderID 8619.
-- Rules:
--   1. Each Tab  -> TabID only (GridID / BlockID = NULL)
--   2. Each Grid block sub-item (ControlType = 6) -> TabID + BlockID + GridID
--   3. DISTINCT across templates (no duplicates)
--   4. Truncate before insert
--
-- Run against PLM database (e.g. plm_live_20260602).

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @FolderID INT = 8619;

BEGIN TRANSACTION;

TRUNCATE TABLE dbo.pdmDWRequireTabAndGrid;

-- Tab rows: TabID only
INSERT INTO dbo.pdmDWRequireTabAndGrid (TabID, GridID, BlockID)
SELECT DISTINCT
    tt.TabID,
    NULL,
    NULL
FROM dbo.pdmTemplateTab AS tt
INNER JOIN dbo.pdmTemplate AS t
    ON t.TemplateID = tt.TemplateID
WHERE t.FolderID = @FolderID;

DECLARE @TabRows INT = @@ROWCOUNT;

-- Grid rows: TabID + BlockID + GridID (grid block sub-items on each tab)
INSERT INTO dbo.pdmDWRequireTabAndGrid (TabID, GridID, BlockID)
SELECT DISTINCT
    tt.TabID,
    bsi.GridID,
    tb.BlockID
FROM dbo.pdmTemplateTab AS tt
INNER JOIN dbo.pdmTemplate AS t
    ON t.TemplateID = tt.TemplateID
INNER JOIN dbo.pdmTabBlock AS tb
    ON tb.TabID = tt.TabID
INNER JOIN dbo.pdmBlockSubItem AS bsi
    ON bsi.BlockID = tb.BlockID
WHERE t.FolderID = @FolderID
  AND bsi.ControlType = 6          -- Grid sub-item
  AND bsi.GridID IS NOT NULL;

DECLARE @GridRows INT = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT N'pdmDWRequireTabAndGrid populated for FolderID ' + CAST(@FolderID AS NVARCHAR(20));
PRINT N'  Tab rows (TabID only):   ' + CAST(@TabRows AS NVARCHAR(20));
PRINT N'  Grid rows (Tab+Block+Grid): ' + CAST(@GridRows AS NVARCHAR(20));
PRINT N'  Total:                   ' + CAST(@TabRows + @GridRows AS NVARCHAR(20));

-- Summary by template (informational)
SELECT
    t.TemplateID,
    t.TemplateName,
    COUNT(DISTINCT tt.TabID) AS TabCount,
    COUNT(DISTINCT CASE WHEN bsi.GridID IS NOT NULL THEN CAST(tt.TabID AS VARCHAR(20)) + N'|' + CAST(tb.BlockID AS VARCHAR(20)) + N'|' + CAST(bsi.GridID AS VARCHAR(20)) END) AS GridBlockCount
FROM dbo.pdmTemplate AS t
INNER JOIN dbo.pdmTemplateTab AS tt
    ON tt.TemplateID = t.TemplateID
LEFT JOIN dbo.pdmTabBlock AS tb
    ON tb.TabID = tt.TabID
LEFT JOIN dbo.pdmBlockSubItem AS bsi
    ON bsi.BlockID = tb.BlockID
   AND bsi.ControlType = 6
   AND bsi.GridID IS NOT NULL
WHERE t.FolderID = @FolderID
GROUP BY t.TemplateID, t.TemplateName
ORDER BY t.TemplateID;

-- Spot-check result table
SELECT
    CASE
        WHEN GridID IS NULL AND BlockID IS NULL THEN N'Tab'
        ELSE N'Grid'
    END AS RowType,
    TabID,
    BlockID,
    GridID
FROM dbo.pdmDWRequireTabAndGrid
ORDER BY TabID, CASE WHEN GridID IS NULL THEN 0 ELSE 1 END, BlockID, GridID;
