SET NOCOUNT ON;
PRINT '=== Trim BOM unit20 fields for MU115 cols ===';
SELECT f.TransactionFieldID, f.DataBaseFieldName, f.ControlType, f.EntityID, f.IsPrimaryKey, f.IsLinkToParentPrimaryKey
FROM dbo.AppTransactionField f
WHERE f.TransactionUnitID=20
  AND (f.DataBaseFieldName IN (N'RowId',N'ReferenceId',N'ProductReferenceID',N'Trim_Code',N'Trim_Type',N'Description')
       OR f.IsPrimaryKey=1 OR f.IsLinkToParentPrimaryKey=1)
ORDER BY f.SortOrder;

PRINT '=== Tab_4229 unit tree ===';
SELECT u.TransactionUnitID, u.UnitDisplayName, u.DataBaseTableName, u.ParentTransactionUnitID
FROM dbo.AppTransactionUnit u WHERE u.TransactionID=2251 ORDER BY u.TransactionUnitID;

PRINT '=== MU112 SI FM on Style Header 4251 ===';
SELECT PlmSubItemId, AppColumnName, PlmControlType, PlmEntityId
FROM dbo.Plm_FieldMapping WHERE PlmTabId=4251 AND FieldKind=N'TabField'
  AND PlmSubItemId IN (1,22,2,10,121,4938,6887,6805,5082,5234);

PRINT '=== MU112 SI FM any ===';
SELECT PlmSubItemId, AppTableName, AppColumnName, PlmTabId
FROM dbo.Plm_FieldMapping WHERE PlmSubItemId IN (5082,5234,6887,6805,4938) ORDER BY PlmSubItemId, PlmTabId;

PRINT '=== Pack DS Collection token ===';
DECLARE @q nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2096);
SELECT CASE WHEN CHARINDEX(N'Collection',@q)>0 THEN 1 ELSE 0 END AS HasCollection,
       CASE WHEN CHARINDEX(N'[hdr].[Collection]',@q)>0 THEN 1 ELSE 0 END AS HasHdrCollection;
