SET NOCOUNT ON;
-- Coverage helper: given tab IntegrationId + SubItem list vs unit columns + dataset columns from QueryText
PRINT '=== MU8 Style unit40 columns intersecting FM ===';
;WITH want AS (
  SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
  FROM dbo.Plm_FieldMapping WHERE PlmTabId=4251 AND FieldKind=N'TabField'
    AND PlmSubItemId IN (6,22,23,186,1,149,2,3,4,5,7171,10,11,3004,3006,146,147,148,47,56,103,7032,7035,7043,7045,6750,7121,7125,4938,4135,4136)
),
ids AS (SELECT v FROM (VALUES (6),(22),(23),(186),(1),(149),(2),(3),(4),(5),(7171),(10),(11),(3004),(3006),(146),(147),(148),(47),(56),(103),(7032),(7035),(7043),(7045),(6750),(7121),(7125),(4938),(4135),(4136),(1516),(1553),(1503)) AS x(v))
SELECT i.v AS SubItemId, w.AppColumnName, w.PlmControlType, w.PlmEntityId,
  CASE WHEN f.TransactionFieldID IS NULL THEN 0 ELSE 1 END AS OnUnit,
  f.TransactionFieldID, f.ControlType AS UnitCtl, f.EntityID AS UnitEnt
FROM ids i
LEFT JOIN want w ON w.PlmSubItemId=i.v
LEFT JOIN dbo.AppTransactionField f ON f.TransactionUnitID=40 AND f.DataBaseFieldName=w.AppColumnName
ORDER BY i.v;

PRINT '=== MU8 missing SubItems (no FM on 4251) ===';
SELECT v.SubItemId
FROM (VALUES (6),(22),(23),(186),(1),(149),(2),(3),(4),(5),(7171),(10),(11),(3004),(3006),(146),(147),(148),(47),(56),(103),(7032),(7035),(7043),(7045),(6750),(7121),(7125),(4938),(4135),(4136),(1516),(1553),(1503),(1474),(1477),(1490),(1491),(1501),(1502),(1504),(1539),(1540),(1541),(78),(76),(77),(79),(84),(85),(86),(81),(83),(94),(1523),(82),(95),(87),(88),(91),(92),(93),(80),(89),(90)) AS v(SubItemId)
-- wait wrong - use SI list from MU fields
WHERE 1=0;
