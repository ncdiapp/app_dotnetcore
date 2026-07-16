-- =============================================================================
-- Probe: Active-only row counts (adjust @*ActiveCol after _plm_probe_tables.sql)
-- Against: PLM database (SizeRun section may be swapped to ERP three-part names)
-- Folder: ImportPlmPomAndGrading/source
-- =============================================================================
SET NOCOUNT ON;

/*
  Edit these after Phase A column discovery.
  Common PLM flags: IsActive, Active, IsDeleted (invert), Deleted.
*/
DECLARE @BodyPartActiveCol   sysname = N'IsActive';  -- or N'Active'
DECLARE @BodyTypeActiveCol   sysname = N'IsActive';
DECLARE @SizeRunActiveCol    sysname = N'IsActive';
DECLARE @SizeRotateActiveCol sysname = N'IsActive';

-- BodyPart table name may be PdmV2kBodyPart
DECLARE @BodyPartTable sysname = N'PdmV2kBodyPart';
DECLARE @BodyTypeTable sysname = N'pdmv2kBodyType'; -- confirm casing via probe

PRINT '--- Resolve actual table names via sys.tables before trusting defaults ---';

-- Existence-safe counts (dynamic when columns confirmed)
IF OBJECT_ID(N'dbo.PdmV2kBodyPart', N'U') IS NOT NULL
    SELECT N'PdmV2kBodyPart' AS Entity, COUNT(*) AS RowCountAll FROM dbo.PdmV2kBodyPart;

IF OBJECT_ID(N'dbo.pdmv2kBodyType', N'U') IS NOT NULL
    SELECT N'pdmv2kBodyType' AS Entity, COUNT(*) AS RowCountAll FROM dbo.pdmv2kBodyType;
ELSE IF OBJECT_ID(N'dbo.pdmV2kBodyType', N'U') IS NOT NULL
    SELECT N'pdmV2kBodyType' AS Entity, COUNT(*) AS RowCountAll FROM dbo.pdmV2kBodyType;

IF OBJECT_ID(N'dbo.pdmV2kBodyTypeDetail', N'U') IS NOT NULL
    SELECT N'pdmV2kBodyTypeDetail' AS Entity, COUNT(*) AS RowCountAll FROM dbo.pdmV2kBodyTypeDetail;

IF OBJECT_ID(N'dbo.pdmV2kSpecBodyPartGrading', N'U') IS NOT NULL
    SELECT N'pdmV2kSpecBodyPartGrading' AS Entity, COUNT(*) AS RowCountAll FROM dbo.pdmV2kSpecBodyPartGrading;

IF OBJECT_ID(N'dbo.tblSizeRun', N'U') IS NOT NULL
    SELECT N'tblSizeRun' AS Entity, COUNT(*) AS RowCountAll FROM dbo.tblSizeRun;

IF OBJECT_ID(N'dbo.tblSizeRunRotate', N'U') IS NOT NULL
    SELECT N'tblSizeRunRotate' AS Entity, COUNT(*) AS RowCountAll FROM dbo.tblSizeRunRotate;

/*
  SizeRun counts: run this script against the database resolved from pdmEntity.DataSourceFrom
  (see _plm_probe_sizerun_entity.sql), NOT necessarily the PLM catalog.

  After confirming active columns, replace with filtered counts, e.g.:
  SELECT COUNT(*) FROM dbo.tblSizeRun WHERE ISNULL(IsActive, 1) = 1;
*/
