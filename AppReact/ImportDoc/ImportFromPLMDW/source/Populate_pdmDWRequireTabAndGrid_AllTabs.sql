-- Populate pdmDWRequireTabAndGrid for ALL tabs in PLM.
-- Rules:
--   1. Each Tab  -> TabID only (GridID / BlockID = NULL)
--   2. Each Grid block sub-item (ControlType = 6) -> TabID + BlockID + GridID
--      (GRIDBLOCKID = pdmTabBlock.BlockID, the block that hosts the grid)
--   3. DISTINCT (no duplicates)
--   4. Truncate before insert (full reload)
--
-- Run against PLM database (e.g. plm_live_20260602).

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

TRUNCATE TABLE dbo.pdmDWRequireTabAndGrid;

-- 1) Tab rows: TabID only
INSERT INTO dbo.pdmDWRequireTabAndGrid (TabID, GridID, BlockID)
SELECT DISTINCT
    t.TabID,
    NULL,
    NULL
FROM dbo.pdmTab AS t;

DECLARE @TabRows INT = @@ROWCOUNT;

-- 2) Grid rows: TabID + GridID + BlockID (grid block sub-items on each tab)
INSERT INTO dbo.pdmDWRequireTabAndGrid (TabID, GridID, BlockID)
SELECT DISTINCT
    tb.TabID,
    bsi.GridID,
    tb.BlockID
FROM dbo.pdmTabBlock AS tb
INNER JOIN dbo.pdmBlockSubItem AS bsi
    ON bsi.BlockID = tb.BlockID
WHERE bsi.ControlType = 6          -- Grid sub-item
  AND bsi.GridID IS NOT NULL;

DECLARE @GridRows INT = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT N'pdmDWRequireTabAndGrid populated for ALL tabs';
PRINT N'  Tab rows (TabID only):      ' + CAST(@TabRows AS NVARCHAR(20));
PRINT N'  Grid rows (Tab+Grid+Block): ' + CAST(@GridRows AS NVARCHAR(20));
PRINT N'  Total:                      ' + CAST(@TabRows + @GridRows AS NVARCHAR(20));

-- Spot-check
SELECT
    CASE
        WHEN GridID IS NULL AND BlockID IS NULL THEN N'Tab'
        ELSE N'Grid'
    END AS RowType,
    TabID,
    GridID,
    BlockID
FROM dbo.pdmDWRequireTabAndGrid
ORDER BY TabID, CASE WHEN GridID IS NULL THEN 0 ELSE 1 END, BlockID, GridID;
