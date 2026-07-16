-- =============================================================================
-- Probe: SpecBodyPartGrading → BodyTypeDetail → BodyType → BodyPart join path (G-B)
-- Against: PLM database
-- Folder: ImportPlmPomAndGrading/source
-- =============================================================================
SET NOCOUNT ON;

/*
  Goal: discover FK column names so Phase B can build GradeRule rows:
    BodyType (active) → Detail → SpecBodyPartGrading → BodyPart.Code
    GradeRuleSetId := BodyTypeID
*/

-- 1) Column names on the three tables
SELECT o.name AS TableName, c.name AS ColumnName, c.is_identity, ty.name AS TypeName
FROM sys.objects o
INNER JOIN sys.columns c ON c.object_id = o.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE o.type = 'U'
  AND o.name IN (N'pdmV2kBodyTypeDetail', N'pdmV2kSpecBodyPartGrading', N'PdmV2kBodyPart', N'pdmv2kBodyType', N'pdmV2kBodyType')
ORDER BY o.name, c.column_id;

-- 2) Sample join — ADJUST column names after step 1
-- Common patterns (verify against probe):
--   Detail: BodyTypeDetailID, BodyTypeID, BodyPartID, Sort
--   Grading: BodyTypeDetailID (or SpecBodyPartGradingID), GradingPlusValue, GradingMinuValue

IF OBJECT_ID(N'dbo.pdmV2kBodyTypeDetail', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.pdmV2kSpecBodyPartGrading', N'U') IS NOT NULL
BEGIN
    -- Peek grading columns
    SELECT TOP (20) g.*
    FROM dbo.pdmV2kSpecBodyPartGrading AS g;

    SELECT TOP (20) d.*
    FROM dbo.pdmV2kBodyTypeDetail AS d;
END

/*
  After columns are known, agent writes a validated join into Preview, e.g.:

  SELECT
      bt.BodyTypeID,
      bt.BodyTypeName,
      bp.Code AS BodyPartCode,
      g.GradingPlusValue,
      g.GradingMinuValue   -- or GradingMinusValue
  FROM dbo.pdmv2kBodyType bt
  INNER JOIN dbo.pdmV2kBodyTypeDetail d ON d.BodyTypeID = bt.BodyTypeID
  INNER JOIN dbo.pdmV2kSpecBodyPartGrading g ON g.BodyTypeDetailID = d.BodyTypeDetailID
  INNER JOIN dbo.PdmV2kBodyPart bp ON bp.BodyPartID = d.BodyPartID
  WHERE ISNULL(bt.IsActive, 1) = 1
    AND ISNULL(bp.IsActive, 1) = 1;
*/
