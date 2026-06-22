-- Phase A probe: populate #TabInput (TabId INT) with user TabIds before running.
-- Example:
--   CREATE TABLE #TabInput (TabId INT PRIMARY KEY);
--   INSERT #TabInput VALUES (4258),(4212),(4213);

SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#TabInput') IS NULL
BEGIN
    RAISERROR(N'Create and fill #TabInput (TabId) first.', 16, 1);
    RETURN;
END

SELECT
    i.TabId,
    t.name AS DwTableName,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c
     WHERE c.TABLE_NAME = t.name
       AND c.COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')) AS DataColumnCount
FROM #TabInput i
LEFT JOIN sys.tables t
    ON t.name LIKE N'PLM_DW_Tab[_]%'
   AND t.name LIKE N'%\_' + CAST(i.TabId AS NVARCHAR(20)) ESCAPE N'\'
ORDER BY i.TabId;

-- Grids (agent maps to TabIds in Phase A)
SELECT name AS DwGridTable FROM sys.tables
WHERE name LIKE N'PLM_DW_Grid[_]%'
ORDER BY name;
