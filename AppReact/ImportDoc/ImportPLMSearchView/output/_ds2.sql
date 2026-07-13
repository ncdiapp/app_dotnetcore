SET NOCOUNT ON;
PRINT '=== Searches ===';
SELECT s.SearchID, s.IntegrationId, s.Name, s.DataSetID
FROM dbo.AppSearch s
WHERE s.IntegrationId IN (N'Search_1aStyleSearch',N'Search_28902',N'Search_23902',N'Search_24002',N'Search_24102',N'Search_25402',N'Search_30223')
ORDER BY s.SearchID;

PRINT '=== DS FROM table aliases (extract FROM section start) ===';
SELECT d.DataSetID, d.Name,
  SUBSTRING(d.QueryText, NULLIF(CHARINDEX(N'FROM', d.QueryText),0), 350) AS FromClause
FROM dbo.AppDataSet d WHERE d.DataSetID IN (2087,2095,2096,2097,2098,2091,2109);
