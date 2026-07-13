SET NOCOUNT ON;
PRINT '=== MU8 SI alt tables ===';
SELECT PlmSubItemId, AppTableName, AppColumnName, PlmTabId
FROM dbo.Plm_FieldMapping
WHERE PlmSubItemId IN (47,56,103,146,147,148,4135,4136,4938,1516) AND FieldKind=N'TabField'
ORDER BY PlmSubItemId, PlmTabId;

PRINT '=== MU9 SI alt (Fabric Info 4212) ===';
SELECT PlmSubItemId, AppTableName, AppColumnName, PlmTabId
FROM dbo.Plm_FieldMapping
WHERE PlmSubItemId IN (6,3,4,5,56,103,181,3127,3789) AND (PlmTabId=4212 OR AppTableName LIKE N'%Fabric%')
ORDER BY PlmSubItemId, PlmTabId;

PRINT '=== MU18 SI alt ===';
SELECT PlmSubItemId, AppTableName, AppColumnName, PlmTabId
FROM dbo.Plm_FieldMapping
WHERE PlmSubItemId IN (181,3153,3810,7111) AND FieldKind=N'TabField'
ORDER BY PlmSubItemId, PlmTabId;

PRINT '=== Entity IntegrationIds sample ===';
SELECT TOP 5 COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME LIKE N'%Entity%' AND TABLE_NAME NOT LIKE N'%Log%' ORDER BY TABLE_NAME;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE N'%AppEntity%' OR TABLE_NAME LIKE N'Entity%';
