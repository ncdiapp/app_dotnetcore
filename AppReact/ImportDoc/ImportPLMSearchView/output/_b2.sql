SET NOCOUNT ON;
PRINT '=== B2 Grid_7 units / color grid ===';
SELECT u.TransactionUnitID, u.UnitDisplayName, u.DataBaseTableName, u.ParentTransactionUnitID,
  (SELECT COUNT(*) FROM dbo.AppTransactionField f WHERE f.TransactionUnitID=u.TransactionUnitID) AS FieldCnt
FROM dbo.AppTransactionUnit u WHERE u.TransactionID=2279;

PRINT '=== Grid_7 color unit fields for MU114 metas ===';
SELECT f.TransactionFieldID, f.DataBaseFieldName, f.ControlType, f.EntityID, f.IsPrimaryKey, f.IsLinkToParentPrimaryKey
FROM dbo.AppTransactionField f
WHERE f.TransactionUnitID=126
  AND (f.DataBaseFieldName IN (N'RowId',N'ReferenceId',N'Color',N'Color_Code',N'Name',N'Description',N'ReferenceCode',N'ReferenceName',N'Active',
       N'CAN_Qty_Color',N'USA_Qty_Color',N'CAN_Qty_Total',N'USA_Qty_Total',N'CAN_PO_s',N'CAN_ETA',N'USA_PO_s',N'USA_ETA')
       OR f.IsPrimaryKey=1 OR f.IsLinkToParentPrimaryKey=1)
ORDER BY f.SortOrder, f.TransactionFieldID;

PRINT '=== Selling Retail grid units (MU116) ===';
SELECT t.TransactionID, t.IntegrationId, t.TransactionName, u.TransactionUnitID, u.DataBaseTableName, u.ParentTransactionUnitID
FROM dbo.AppTransaction t
JOIN dbo.AppTransactionUnit u ON u.TransactionID=t.TransactionID
WHERE u.DataBaseTableName=N'Plm_Selling_and_Retail_by_Style_Currency'
   OR t.IntegrationId LIKE N'%3084%' OR t.IntegrationId LIKE N'%Selling%';

PRINT '=== Trim BOM tables / grids ===';
SELECT DISTINCT AppTableName, PlmGridId, FieldKind, COUNT(*) AS Cnt
FROM dbo.Plm_FieldMapping
WHERE AppTableName LIKE N'%Trim%BOM%' OR AppTableName LIKE N'%Trims%'
GROUP BY AppTableName, PlmGridId, FieldKind
ORDER BY AppTableName;

PRINT '=== Existing ListEdits ===';
SELECT TransactionID, IntegrationId, TransactionName FROM dbo.AppTransaction
WHERE IntegrationId LIKE N'ListEdit%' OR TransactionName LIKE N'%Mass Update%' OR TransactionName LIKE N'%MU%'
ORDER BY TransactionID;
