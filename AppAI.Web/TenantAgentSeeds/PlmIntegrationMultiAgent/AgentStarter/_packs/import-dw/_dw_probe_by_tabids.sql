-- Phase A probe: populate #TabInput (TabId INT) from PLM pdmTemplateTab (NOT user-typed TabIds).
-- Run source/_plm_probe_template.sql first, then insert TabIDs into #TabInput.

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
