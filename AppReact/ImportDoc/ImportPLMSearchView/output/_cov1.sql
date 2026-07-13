SET NOCOUNT ON;
PRINT '=== MU8 coverage Tab_4251 unit 40 ===';
;WITH mu AS (
  SELECT * FROM (VALUES
    (6,1),(22,1),(23,1),(186,0),(1,0),(149,0),(2,0),(3,0),(4,0),(5,0),(7171,0),(10,0),(11,0),(3004,0),(3006,0),
    (146,0),(147,0),(148,0),(47,0),(56,0),(103,0),(7032,0),(7035,0),(7043,0),(7045,0),(6750,0),(7121,0),(7125,0),
    (4938,0),(4135,0),(4136,0)
  ) AS t(SubItemId,IsReadonly)
),
fm AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4251 AND FieldKind=N'TabField'
)
SELECT m.SubItemId, m.IsReadonly, fm.AppColumnName, fm.PlmControlType, fm.PlmEntityId,
  CASE WHEN fm.PlmSubItemId IS NULL THEN N'UNMAPPED' ELSE N'mapped' END AS FmStatus,
  f.TransactionFieldID, CASE WHEN f.TransactionFieldID IS NULL THEN 0 ELSE 1 END AS OnUnit
FROM mu m
LEFT JOIN fm ON fm.PlmSubItemId=m.SubItemId
LEFT JOIN dbo.AppTransactionField f ON f.TransactionUnitID=40 AND f.DataBaseFieldName=fm.AppColumnName
ORDER BY CASE WHEN fm.PlmSubItemId IS NULL THEN 0 ELSE 1 END, m.SubItemId;

PRINT '=== MU9 coverage Tab_4258 unit 85 ===';
;WITH mu AS (
  SELECT * FROM (VALUES
    (6,1),(22,1),(23,1),(5333,0),(186,0),(1,0),(149,0),(2,0),(3,0),(4,0),(5,0),(3127,0),(3133,0),(3128,0),
    (47,0),(56,0),(3789,0),(103,0),(181,1),(7202,0),(7201,0)
  ) AS t(SubItemId,IsReadonly)
),
fm AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4258 AND FieldKind=N'TabField'
)
SELECT m.SubItemId, fm.AppColumnName, fm.PlmControlType, fm.PlmEntityId,
  CASE WHEN fm.PlmSubItemId IS NULL THEN N'UNMAPPED' ELSE N'mapped' END AS FmStatus,
  f.TransactionFieldID, CASE WHEN f.TransactionFieldID IS NULL THEN 0 ELSE 1 END AS OnUnit
FROM mu m
LEFT JOIN fm ON fm.PlmSubItemId=m.SubItemId
LEFT JOIN dbo.AppTransactionField f ON f.TransactionUnitID=85 AND f.DataBaseFieldName=fm.AppColumnName
ORDER BY CASE WHEN fm.PlmSubItemId IS NULL THEN 0 ELSE 1 END, m.SubItemId;

PRINT '=== MU18 coverage Tab_4252 unit 117 ===';
;WITH mu AS (
  SELECT * FROM (VALUES
    (6),(22),(23),(186),(1),(149),(2),(3),(4),(5),(3128),(3810),(6885),(7333),(103),(181),(3153),(7111),
    (6688),(7339),(7335),(7342),(7337),(7344)
  ) AS t(SubItemId)
),
fm AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4252 AND FieldKind=N'TabField'
)
SELECT m.SubItemId, fm.AppColumnName, fm.PlmControlType, fm.PlmEntityId,
  CASE WHEN fm.PlmSubItemId IS NULL THEN N'UNMAPPED' ELSE N'mapped' END AS FmStatus,
  f.TransactionFieldID, CASE WHEN f.TransactionFieldID IS NULL THEN 0 ELSE 1 END AS OnUnit
FROM mu m
LEFT JOIN fm ON fm.PlmSubItemId=m.SubItemId
LEFT JOIN dbo.AppTransactionField f ON f.TransactionUnitID=117 AND f.DataBaseFieldName=fm.AppColumnName
ORDER BY CASE WHEN fm.PlmSubItemId IS NULL THEN 0 ELSE 1 END, m.SubItemId;
