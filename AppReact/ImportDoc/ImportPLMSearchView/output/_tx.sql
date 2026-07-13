SET NOCOUNT ON;
PRINT '=== Tx by IntegrationId ===';
SELECT t.IntegrationId, t.TransactionID, t.TransactionName,
       u.TransactionUnitID, u.UnitDisplayName, u.DataBaseTableName, u.ParentTransactionUnitID, u.IntegrationId AS UnitIntId
FROM dbo.AppTransaction t
LEFT JOIN dbo.AppTransactionUnit u ON u.TransactionID=t.TransactionID
WHERE t.IntegrationId IN (2256,2276,2274,2271,2257,2266,2279,2284,2277,2275,2272,2262)
ORDER BY t.IntegrationId, CASE WHEN u.ParentTransactionUnitID IS NULL THEN 0 ELSE 1 END, u.TransactionUnitID;

PRINT '=== PK ReferenceId on those units ===';
SELECT t.IntegrationId, t.TransactionName, u.DataBaseTableName, f.TransactionFieldID, f.DataBaseFieldName, f.ControlType, f.EntityID, f.IsPrimaryKey, f.IsVisible, f.IsReadonly
FROM dbo.AppTransaction t
JOIN dbo.AppTransactionUnit u ON u.TransactionID=t.TransactionID
JOIN dbo.AppTransactionField f ON f.TransactionUnitID=u.TransactionUnitID
WHERE t.IntegrationId IN (2256,2276,2274,2271,2257,2266)
  AND (f.DataBaseFieldName='ReferenceId' OR f.IsPrimaryKey=1)
ORDER BY t.IntegrationId, u.TransactionUnitID, f.TransactionFieldID;
