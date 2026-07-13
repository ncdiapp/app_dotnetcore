SET NOCOUNT ON;
PRINT '=== MU20 Label unit 98 ===';
;WITH mu AS (
  SELECT * FROM (VALUES
    (6),(22),(23),(186),(1),(141),(149),(2),(3),(4),(5),(3128),(3810),(6885),(7333),(103),
    (6688),(7339),(7335),(7342),(7337),(7344)
  ) AS t(SubItemId)
),
fm AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4247 AND FieldKind=N'TabField'
)
SELECT m.SubItemId, fm.AppColumnName, fm.PlmControlType, fm.PlmEntityId,
  CASE WHEN fm.PlmSubItemId IS NULL THEN N'UNMAPPED' ELSE N'mapped' END AS FmStatus,
  f.TransactionFieldID, CASE WHEN f.TransactionFieldID IS NULL THEN 0 ELSE 1 END AS OnUnit
FROM mu m
LEFT JOIN fm ON fm.PlmSubItemId=m.SubItemId
LEFT JOIN dbo.AppTransactionField f ON f.TransactionUnitID=98 AND f.DataBaseFieldName=fm.AppColumnName
ORDER BY CASE WHEN fm.PlmSubItemId IS NULL THEN 0 ELSE 1 END, m.SubItemId;

PRINT '=== MU22 Pack unit 109 ===';
;WITH mu AS (
  SELECT * FROM (VALUES
    (6),(22),(23),(186),(1),(149),(2),(3),(4),(5),(3128),(3810),(56),(3789),(103),(181),(3153),(7111)
  ) AS t(SubItemId)
),
fm AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4249 AND FieldKind=N'TabField'
)
SELECT m.SubItemId, fm.AppColumnName, fm.PlmControlType, fm.PlmEntityId,
  CASE WHEN fm.PlmSubItemId IS NULL THEN N'UNMAPPED' ELSE N'mapped' END AS FmStatus,
  f.TransactionFieldID, CASE WHEN f.TransactionFieldID IS NULL THEN 0 ELSE 1 END AS OnUnit
FROM mu m
LEFT JOIN fm ON fm.PlmSubItemId=m.SubItemId
LEFT JOIN dbo.AppTransactionField f ON f.TransactionUnitID=109 AND f.DataBaseFieldName=fm.AppColumnName
ORDER BY CASE WHEN fm.PlmSubItemId IS NULL THEN 0 ELSE 1 END, m.SubItemId;

PRINT '=== MU22 alt FM any tab for unmapped SIs ===';
SELECT PlmSubItemId, AppTableName, AppColumnName, PlmTabId, FieldKind
FROM dbo.Plm_FieldMapping
WHERE PlmSubItemId IN (3810,56,3789,181,3153,7111)
ORDER BY PlmSubItemId, PlmTabId;

PRINT '=== MU49 split Style+Publish ===';
;WITH mu AS (
  SELECT * FROM (VALUES
    (6),(3105),(22),(3919),(23),(121),(186),(1),(2),(103),(48),(3),(4),(5),(47),(102),(141),(142),
    (11),(10),(4133),(4134),(4135),(4136),(111),(4137),(3923),(143),(144),(145),(56),(3916),
    (146),(147),(148),(104),(3914),(3915),(3920),(4184)
  ) AS t(SubItemId)
),
fmStyle AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4251 AND FieldKind=N'TabField'
),
fmPub AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4272 AND FieldKind=N'TabField'
)
SELECT m.SubItemId,
  COALESCE(fs.AppColumnName, fp.AppColumnName) AS AppColumn,
  COALESCE(fs.PlmControlType, fp.PlmControlType) AS Ctl,
  COALESCE(fs.PlmEntityId, fp.PlmEntityId) AS Ent,
  CASE WHEN fs.PlmSubItemId IS NOT NULL THEN N'Tab_4251'
       WHEN fp.PlmSubItemId IS NOT NULL THEN N'Tab_4272'
       ELSE N'UNMAPPED' END AS SourceTab,
  CASE WHEN fsh.TransactionFieldID IS NOT NULL OR fpu.TransactionFieldID IS NOT NULL OR fsty.TransactionFieldID IS NOT NULL THEN 1 ELSE 0 END AS OnPubTxAnyUnit,
  COALESCE(fsty.TransactionFieldID, fpu.TransactionFieldID, fsh.TransactionFieldID) AS FieldId
FROM mu m
LEFT JOIN fmStyle fs ON fs.PlmSubItemId=m.SubItemId
LEFT JOIN fmPub fp ON fp.PlmSubItemId=m.SubItemId
LEFT JOIN dbo.AppTransactionField fsty ON fsty.TransactionUnitID=49 AND fsty.DataBaseFieldName=COALESCE(fs.AppColumnName,fp.AppColumnName)
LEFT JOIN dbo.AppTransactionField fpu ON fpu.TransactionUnitID=50 AND fpu.DataBaseFieldName=COALESCE(fs.AppColumnName,fp.AppColumnName)
LEFT JOIN dbo.AppTransactionField fsh ON fsh.TransactionUnitID=40 AND fsh.DataBaseFieldName=COALESCE(fs.AppColumnName,fp.AppColumnName)
ORDER BY CASE WHEN COALESCE(fs.PlmSubItemId,fp.PlmSubItemId) IS NULL THEN 0 ELSE 1 END, m.SubItemId;
