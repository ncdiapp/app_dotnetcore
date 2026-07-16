-- =============================================================================
-- Probe: Resolve SizeRun / SizeRunDetail physical database via pdmEntity
-- Against: PLM database ONLY
-- Folder: ImportPlmPomAndGrading/source
--
-- Authoritative rule (do NOT infer from table existence on PLM):
--   pdmEntity.DataSourceFrom  1 = PLM,  2 = ERP  (3 = DataWS, 4 = OtherEx)
-- Connection for DataSourceFrom > 1: dbo.pdmDataSource.ConnectionString
-- Reference: APP.BL PlmMigrationBL.GetPlmDataSourceFromName / ReadPdmDataSourceRows
-- =============================================================================
SET NOCOUNT ON;

-- 1) SizeRun-related system entities
SELECT
    e.EntityID,
    e.EntityCode,
    e.[Description],
    e.SysTableName,
    e.SchemaOwner,
    e.DataSourceFrom,
    CASE e.DataSourceFrom
        WHEN 1 THEN N'PLM'
        WHEN 2 THEN N'ERP'
        WHEN 3 THEN N'DataWS'
        WHEN 4 THEN N'OtherEx'
        ELSE N'Source_' + CAST(e.DataSourceFrom AS NVARCHAR(10))
    END AS DataSourceFromName,
    e.EntityType,
    e.IsRelationEntity
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0
  AND e.EntityCode IN (N'SizeRun', N'SizeRunDetail')
ORDER BY e.EntityCode, e.EntityID;

-- 2) All system entities (optional — verify EntityCode spelling if above returns 0 rows)
SELECT
    e.EntityID,
    e.EntityCode,
    e.SysTableName,
    e.DataSourceFrom
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0
  AND (
        e.EntityCode LIKE N'%Size%Run%'
     OR e.SysTableName IN (N'tblSizeRun', N'tblSizeRunRotate')
      )
ORDER BY e.EntityCode;

-- 3) Registered data source connections (PLM stores ERP connection here)
IF OBJECT_ID(N'dbo.pdmDataSource', N'U') IS NOT NULL
BEGIN
    SELECT
        ds.DataSourceFrom,
        CASE ds.DataSourceFrom
            WHEN 1 THEN N'PLM'
            WHEN 2 THEN N'ERP'
            WHEN 3 THEN N'DataWS'
            WHEN 4 THEN N'OtherEx'
            ELSE N'Source_' + CAST(ds.DataSourceFrom AS NVARCHAR(10))
        END AS DataSourceFromName,
        ds.DataSourceName,
        -- Do NOT paste ConnectionString into git; Preview uses this locally only
        CASE
            WHEN ds.ConnectionString IS NULL OR LTRIM(RTRIM(ds.ConnectionString)) = N'' THEN 0
            ELSE 1
        END AS HasConnectionString,
        LEFT(ds.ConnectionString, 80) AS ConnectionStringPreview
    FROM dbo.pdmDataSource ds
    WHERE ds.DataSourceFrom IN (1, 2, 3, 4)
    ORDER BY ds.DataSourceFrom;
END
ELSE
    PRINT 'pdmDataSource not found — user must supply ERP connection override if DataSourceFrom = 2';

/*
  Phase A agent steps after this script:
  1. For SizeRun entity: if DataSourceFrom = 1 → run _probe_sizerun_tables.sql on PLM
  2. For SizeRun entity: if DataSourceFrom = 2 → connect using pdmDataSource row 2 (or user ERP string)
  3. Repeat for SizeRunDetail if DataSourceFrom differs
  4. Record resolved InitialCatalog + SysTableName in output/{runId}/0_Preview_Report.md
*/
