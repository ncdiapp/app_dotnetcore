SET NOCOUNT ON;
PRINT '=== Entity IntegrationId by EntityInfoID ===';
SELECT EntityInfoID, IntegrationId, EntityCode, LEFT(Description,40) AS Descr
FROM dbo.AppEntityInfo
WHERE EntityInfoID IN (1,4,5,6,7,8,9,10,11,12,42,59,67,68,79,84,92,93,95,3461,3462,3501,3521,3708,4507,4721,4757,4772,4790)
ORDER BY EntityInfoID;

PRINT '=== Trim BOM FM sample for ProductReference ===';
SELECT PlmMetaColumnId, AppTableName, AppColumnName, PlmControlType, PlmEntityId, PlmGridId
FROM dbo.Plm_FieldMapping
WHERE AppTableName=N'Plm_Trim_BOM_prod' AND (
  AppColumnName LIKE N'%Trim%' OR AppColumnName LIKE N'%Product%' OR AppColumnName LIKE N'%Desc%' OR AppColumnName LIKE N'%Type%' OR PlmMetaColumnId IN (7068,7072,7360,7074)
)
ORDER BY PlmMetaColumnId;

PRINT '=== Selling retail unit46 fields ===';
SELECT f.TransactionFieldID, f.DataBaseFieldName, f.ControlType, f.EntityID, f.IsPrimaryKey, f.IsLinkToParentPrimaryKey
FROM dbo.AppTransactionField f
WHERE f.TransactionUnitID=46
  AND (f.DataBaseFieldName IN (N'RowId',N'ReferenceId',N'Selling_Currency',N'Fob_Cost',N'VIP_Price',N'Selling_Price',N'Retail') OR f.IsPrimaryKey=1 OR f.IsLinkToParentPrimaryKey=1);

PRINT '=== Color Entity on Grid_7 (4507 vs 79) ===';
SELECT f.DataBaseFieldName, f.ControlType, f.EntityID FROM dbo.AppTransactionField f WHERE f.TransactionUnitID=126 AND f.DataBaseFieldName=N'Color';
