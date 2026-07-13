SET NOCOUNT ON;
PRINT '=== Grids 3162/3169/7/3084 ===';
SELECT GridID, GridName, GridType FROM dbo.pdmGrid WHERE GridID IN (7,3162,3169,3084,17);

PRINT '=== MU115 fields again ===';
SELECT f.Sort, f.SubItemID, f.GridColumnID, bsi.SubItemName, gmc.ColumnName, gmc.GridID
FROM dbo.pdmMassUpdateViewField f
LEFT JOIN dbo.pdmBlockSubItem bsi ON bsi.SubItemID=f.SubItemID
LEFT JOIN dbo.pdmGridMetaColumn gmc ON gmc.GridColumnID=f.GridColumnID
WHERE f.MassUpdateViewID=115 ORDER BY f.Sort;

PRINT '=== MU112 fields ===';
SELECT f.Sort, f.IsReadonly, f.SubItemID, f.GridColumnID, bsi.SubItemName, gmc.ColumnName
FROM dbo.pdmMassUpdateViewField f
LEFT JOIN dbo.pdmBlockSubItem bsi ON bsi.SubItemID=f.SubItemID
LEFT JOIN dbo.pdmGridMetaColumn gmc ON gmc.GridColumnID=f.GridColumnID
WHERE f.MassUpdateViewID=112 ORDER BY ISNULL(f.Sort,9999);
