SET NOCOUNT ON;
PRINT '=== APP tables for grid 3162 style trim bom ===';
SELECT DISTINCT AppTableName, PlmGridId, FieldKind FROM dbo.Plm_FieldMapping WHERE PlmGridId IN (3162,3169) OR AppTableName LIKE N'%Trim_BOM%';
PRINT '=== Any tx with Trim_BOM ===';
SELECT t.TransactionID, t.IntegrationId, t.TransactionName, u.TransactionUnitID, u.DataBaseTableName, u.ParentTransactionUnitID
FROM dbo.AppTransactionUnit u
JOIN dbo.AppTransaction t ON t.TransactionID=u.TransactionID
WHERE u.DataBaseTableName LIKE N'%Trim_BOM%' OR u.DataBaseTableName LIKE N'%Selling_and_Retail%';
PRINT '=== Style header unit40 has Article for MU112/115 root field ===';
SELECT TransactionFieldID, DataBaseFieldName, ControlType, EntityID, IsReadonly
FROM dbo.AppTransactionField WHERE TransactionUnitID=40 AND DataBaseFieldName IN (N'Article',N'Classification',N'Product_Type_2',N'Size_Range_10',N'Long_Description',N'Total_Composition',N'Garment_Factory',N'Manufacturer_COO');
