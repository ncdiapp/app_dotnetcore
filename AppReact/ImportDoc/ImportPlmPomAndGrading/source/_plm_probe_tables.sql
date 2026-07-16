-- =============================================================================
-- Probe: PLM POM-related tables exist + column inventory
-- Against: PLM database
-- Folder: ImportPlmPomAndGrading/source
--
-- SizeRun / SizeRunRotate: do NOT use this script to pick the database.
-- Use _plm_probe_sizerun_entity.sql (pdmEntity.DataSourceFrom 1=PLM, 2=ERP).
-- =============================================================================
SET NOCOUNT ON;

DECLARE @Tables TABLE (ObjectName sysname PRIMARY KEY);
INSERT INTO @Tables (ObjectName) VALUES
    (N'PdmV2kBodyPart'),
    (N'pdmv2kBodyType'),
    (N'pdmV2kBodyTypeDetail'),
    (N'pdmV2kSpecBodyPartGrading');

SELECT
    t.ObjectName,
    CASE WHEN o.object_id IS NULL THEN 0 ELSE 1 END AS ExistsInDb,
    o.type_desc
FROM @Tables t
LEFT JOIN sys.objects o
    ON o.name = t.ObjectName
   AND o.schema_id = SCHEMA_ID(N'dbo')
   AND o.type IN ('U', 'V');

-- Columns for objects that exist
SELECT
    s.name AS SchemaName,
    o.name AS TableName,
    c.column_id,
    c.name AS ColumnName,
    ty.name AS TypeName,
    c.max_length,
    c.is_nullable,
    c.is_identity
FROM sys.objects o
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
INNER JOIN sys.columns c ON c.object_id = o.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE o.type IN ('U', 'V')
  AND o.name IN (
        N'PdmV2kBodyPart', N'pdmv2kBodyType', N'pdmV2kBodyType',
        N'pdmV2kBodyTypeDetail', N'pdmV2kSpecBodyPartGrading'
      )
ORDER BY o.name, c.column_id;
