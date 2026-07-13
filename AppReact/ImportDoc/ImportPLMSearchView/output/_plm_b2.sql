SET NOCOUNT ON;
PRINT '=== MU B2 shells ===';
SELECT MassUpdateViewID, Name, UpdateType, MainTabID, GridBlockID, GridID, IsActive
FROM dbo.pdmMassUpdateView WHERE MassUpdateViewID IN (114,115,112,116);

PRINT '=== Meta cols ===';
SELECT GridColumnID, ColumnName, ColumnTypeID, EntityID, GridID
FROM dbo.pdmGridMetaColumn WHERE GridColumnID IN (7068,7072,7360,7074,44,635,372,45,470,471,7976,5483,8051,8048,5511,5512);

PRINT '=== Grid names ===';
SELECT DISTINCT gmc.GridID, g.Name
FROM dbo.pdmGridMetaColumn gmc
LEFT JOIN dbo.pdmGrid g ON g.GridID = gmc.GridID
WHERE gmc.GridColumnID IN (7068,7072,7360,7074,44,635);

PRINT '=== GridBlock 3975/17/3690 ===';
SELECT GridBlockID, Name, GridID FROM dbo.pdmGridBlock WHERE GridBlockID IN (3975,17,3690,5207);
