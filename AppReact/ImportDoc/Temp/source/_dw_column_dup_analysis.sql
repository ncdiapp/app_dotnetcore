SET NOCOUNT ON;
-- Duplicate base names within each DW tab after stripping SubItemId and _FK_
;WITH tabs AS (
    SELECT name AS DwTable FROM sys.tables WHERE name IN (
        'PLM_DW_Tab_Fabric_Header_4258','PLM_DW_Tab_Fabric_Info_4212','PLM_DW_Tab_Attributes_4213',
        'PLM_DW_Tab_Fabric_Cost_4270','PLM_DW_Tab_Fabric_Policy_4274','PLM_DW_Tab_Testing____Compliance_4219',
        'PLM_DW_Grid_ProductDesignColorGrid_7')
),
cols AS (
    SELECT t.DwTable, c.COLUMN_NAME,
        LEFT(CASE WHEN c.COLUMN_NAME LIKE '%_FK_%' 
            THEN LEFT(c.COLUMN_NAME, CHARINDEX('_FK_', c.COLUMN_NAME+'_FK_')-1) 
            ELSE c.COLUMN_NAME END,
            LEN(CASE WHEN c.COLUMN_NAME LIKE '%_FK_%' 
            THEN LEFT(c.COLUMN_NAME, CHARINDEX('_FK_', c.COLUMN_NAME+'_FK_')-1) 
            ELSE c.COLUMN_NAME END) 
            - CHARINDEX('_', REVERSE(CASE WHEN c.COLUMN_NAME LIKE '%_FK_%' 
            THEN LEFT(c.COLUMN_NAME, CHARINDEX('_FK_', c.COLUMN_NAME+'_FK_')-1) 
            ELSE c.COLUMN_NAME END) + '_') + 1) AS BaseName
    FROM tabs t
    JOIN INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.DwTable
    WHERE c.COLUMN_NAME NOT IN ('TabID','ProductReferenceID','BlockID','GridID','RowID','RowValueGUID','Sort')
),
dup AS (
    SELECT DwTable, BaseName, COUNT(*) AS Cnt
    FROM cols GROUP BY DwTable, BaseName HAVING COUNT(*) > 1
)
SELECT d.DwTable, d.BaseName, d.Cnt,
    STRING_AGG(c.COLUMN_NAME, N' | ') WITHIN GROUP (ORDER BY c.COLUMN_NAME) AS DwColumns
FROM dup d
JOIN cols c ON c.DwTable = d.DwTable AND c.BaseName = d.BaseName
GROUP BY d.DwTable, d.BaseName, d.Cnt
ORDER BY d.DwTable, d.Cnt DESC;
