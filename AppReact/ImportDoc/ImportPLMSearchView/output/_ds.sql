SET NOCOUNT ON;
PRINT '=== DataSets ===';
SELECT d.DataSetID, d.Name, d.BaseTableName,
  LEFT(REPLACE(REPLACE(ISNULL(d.QueryText,N''),CHAR(13),N' '),CHAR(10),N' '),220) AS QLine
FROM dbo.AppDataSet d
WHERE d.DataSetID IN (2087,2095,2096,2097,2098,2091,2109)
ORDER BY d.DataSetID;

PRINT '=== Searches ===';
SELECT s.SearchID, s.IntegrationId, s.Name, s.DataSetID
FROM dbo.AppSearch s WHERE s.SearchID IN (17,18,19,20,21,22,33) OR s.IntegrationId IN (17,18,19,20,21,22,33)
ORDER BY s.SearchID;
