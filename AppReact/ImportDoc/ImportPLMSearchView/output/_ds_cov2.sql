SET NOCOUNT ON;
PRINT '=== DS column presence ===';
DECLARE @q nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2087);
SELECT col, CASE WHEN CHARINDEX(col,@q)>0 THEN 1 ELSE 0 END AS InDs
FROM (VALUES
 (N'Division_186'),(N'Gender_7171'),(N'CC_7032'),(N'Details_7035'),(N'sketch_id'),(N'ddl'),(N'Inseam'),(N'Inseam_txt'),(N'Collection_txt'),
 (N'Sketch'),(N'Article'),(N'Description_23'),(N'Classification'),(N'Style_Status'),(N'Sample_Status'),(N'Composition')
) v(col);
DECLARE @q9 nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2091);
SELECT col, CASE WHEN CHARINDEX(col,@q9)>0 THEN 1 ELSE 0 END AS InDs
FROM (VALUES
 (N'Fabric_Name'),(N'Division_186'),(N'Designer_1'),(N'Designer_2'),(N'Composition'),(N'Product_Type'),(N'ProductTypeGroup'),(N'Fabric_Mill'),(N'Raw_Material_Status')
) v(col);
DECLARE @q18 nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2095);
SELECT N'2095' ds, col, CASE WHEN CHARINDEX(col,@q18)>0 THEN 1 ELSE 0 END InDs FROM (VALUES
 (N'Sketch'),(N'Cost_1'),(N'Cost_2'),(N'Cost_3'),(N'Supplier_1'),(N'Supplier_2'),(N'USD_for_7339'),(N'USD_for_7342'),(N'USD_for_7344'),(N'Raw_Material_Status'),(N'Classification'),(N'Season'),(N'Collection'),(N'Group')
) v(col);
DECLARE @q20 nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2097);
DECLARE @q22 nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2096);
DECLARE @q49 nvarchar(max)=(SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID=2098);
SELECT N'2097' ds, col, CASE WHEN CHARINDEX(col,@q20)>0 THEN 1 ELSE 0 END InDs FROM (VALUES
 (N'Material'),(N'Brand'),(N'Cost_1'),(N'Supplier_1'),(N'Raw_Material_Status'),(N'Sketch'),(N'USD_for_7339'),(N'Classification'),(N'Season')
) v(col)
UNION ALL SELECT N'2096', col, CASE WHEN CHARINDEX(col,@q22)>0 THEN 1 ELSE 0 END FROM (VALUES
 (N'Sketch'),(N'Raw_Material_Status'),(N'Classification'),(N'Season'),(N'Collection'),(N'Group')
) v(col)
UNION ALL SELECT N'2098', col, CASE WHEN CHARINDEX(col,@q49)>0 THEN 1 ELSE 0 END FROM (VALUES
 (N'Publish_to_ERP'),(N'Published_to_ERP'),(N'Publish_Failed_to_ERP'),(N'Long_Description'),(N'Division_186'),(N'Notes'),(N'Price_By'),(N'Publish_to_ERP_Message'),(N'Size_Range_10'),(N'Dimension_11'),(N'Group'),(N'Product_Type_2')
) v(col);
