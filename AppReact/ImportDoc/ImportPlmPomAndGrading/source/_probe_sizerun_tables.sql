-- =============================================================================
-- Probe: Physical SizeRun tables on the RESOLVED database
-- Against: database chosen by pdmEntity.DataSourceFrom (PLM or ERP) — NOT guessed
-- Run AFTER _plm_probe_sizerun_entity.sql
-- Folder: ImportPlmPomAndGrading/source
-- =============================================================================
SET NOCOUNT ON;

/*
  Default table names from pdmEntity.SysTableName (usually tblSizeRun / tblSizeRunRotate).
  Override @SizeRunTable / @SizeDetailTable if probe shows different SysTableName.
*/
DECLARE @SizeRunTable sysname = N'tblSizeRun';
DECLARE @SizeDetailTable sysname = N'tblSizeRunRotate';

SELECT
    @SizeRunTable AS ObjectName,
    CASE WHEN OBJECT_ID(N'dbo.' + @SizeRunTable, N'U') IS NULL THEN 0 ELSE 1 END AS ExistsInDb
UNION ALL
SELECT
    @SizeDetailTable,
    CASE WHEN OBJECT_ID(N'dbo.' + @SizeDetailTable, N'U') IS NULL THEN 0 ELSE 1 END;

IF OBJECT_ID(N'dbo.' + @SizeRunTable, N'U') IS NOT NULL
BEGIN
    SELECT c.column_id, c.name AS ColumnName, ty.name AS TypeName, c.is_nullable, c.is_identity
    FROM sys.columns c
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.' + @SizeRunTable)
    ORDER BY c.column_id;

    SELECT TOP (5) * FROM dbo.tblSizeRun ORDER BY 1;
END

IF OBJECT_ID(N'dbo.' + @SizeDetailTable, N'U') IS NOT NULL
BEGIN
    SELECT c.column_id, c.name AS ColumnName, ty.name AS TypeName, c.is_nullable, c.is_identity
    FROM sys.columns c
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.' + @SizeDetailTable)
    ORDER BY c.column_id;

    DECLARE @sql nvarchar(200) = N'SELECT TOP (5) * FROM dbo.' + QUOTENAME(@SizeDetailTable) + N' ORDER BY 1';
    EXEC sp_executesql @sql;
END
