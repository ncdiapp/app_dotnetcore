SET NOCOUNT ON;
PRINT '=== Tx shells ===';
SELECT t.TransactionID, t.IntegrationId, t.TransactionName,
       u.TransactionUnitID, u.UnitDisplayName, u.DataBaseTableName, u.ParentTransactionUnitID
FROM dbo.AppTransaction t
LEFT JOIN dbo.AppTransactionUnit u ON u.TransactionID=t.TransactionID
WHERE t.IntegrationId IN (N'Tab_4251',N'Tab_4252',N'Tab_4249',N'Tab_4247',N'Tab_4272',N'Tab_4258',N'Grid_7',N'ListEdit_MU111_TrimTracking')
   OR t.TransactionID IN (2256,2276,2274,2271,2257,2266,2279,2284)
ORDER BY t.TransactionID, CASE WHEN u.ParentTransactionUnitID IS NULL THEN 0 ELSE 1 END, u.TransactionUnitID;

PRINT '=== PK fields ===';
SELECT t.TransactionID, t.IntegrationId, u.DataBaseTableName, f.TransactionFieldID, f.DataBaseFieldName, f.ControlType, f.EntityID, f.IsPrimaryKey
FROM dbo.AppTransaction t
JOIN dbo.AppTransactionUnit u ON u.TransactionID=t.TransactionID
JOIN dbo.AppTransactionField f ON f.TransactionUnitID=u.TransactionUnitID
WHERE t.IntegrationId IN (N'Tab_4251',N'Tab_4252',N'Tab_4249',N'Tab_4247',N'Tab_4272',N'Tab_4258')
  AND (f.DataBaseFieldName=N'ReferenceId' OR f.IsPrimaryKey=1 OR f.DataBaseFieldName LIKE N'%Id')
  AND (f.IsPrimaryKey=1 OR f.DataBaseFieldName IN (N'ReferenceId',N'RowId',N'ParentReferenceId'))
ORDER BY t.TransactionID, f.TransactionFieldID;
