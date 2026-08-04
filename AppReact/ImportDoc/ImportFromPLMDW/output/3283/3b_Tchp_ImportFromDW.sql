-- =============================================================================
-- TechPack Tchp* import from plmDW (D1)
-- L2: TchpStyleSpec.ProductReferenceId -> Root.ReferenceId (Blueprint Link-to-Parent; no DB FK).
-- S1: SizeRun/BaseSize/UOM from Grading tab 4006 (PLM_DW_Tab_Grading_4006).
-- SpecFit ActualValue = COALESCE(ReviseN, SampleN). Comments tabs do not host Fit grid.
-- Prerequisites: Tchp foundation (ImportPlmPomAndGrading); Plm_* steps 1-3.
-- Size_Run=Size_Run_43_FK_tblSizeRun Base_Size=Base_Size_44_FK_tblSizeRunRotate Measure_Unit=Measure_Unit_58_FK_
-- SpecGrading=PLM_DW_Grid_SpecGradingGrid_10 SpecFit=PLM_DW_Grid_SpecFitGrid_5
-- =============================================================================
SET NOCOUNT ON;
DECLARE @DwDatabase NVARCHAR(128) = N'plmDW';
DECLARE @DwTwoPart NVARCHAR(260) = QUOTENAME(@DwDatabase) + N'.dbo';
DECLARE @sql NVARCHAR(MAX);

-- 1. TchpStyleSpec
SET @sql = N'
;WITH src AS (
  SELECT g.ProductReferenceID AS ProductReferenceId,
    TRY_CONVERT(INT, g.Size_Run_43_FK_tblSizeRun) AS SizeRunIdRaw,
    TRY_CONVERT(INT, g.Base_Size_44_FK_tblSizeRunRotate) AS BaseSizeRaw,
    CONVERT(NVARCHAR(50), g.Measure_Unit_58_FK_) AS MeasureUnitRaw
  FROM ' + @DwTwoPart + N'.PLM_DW_Tab_Grading_4006 g
  WHERE g.ProductReferenceID IS NOT NULL
)
MERGE dbo.TchpStyleSpec AS t
USING (
  SELECT s.ProductReferenceId,
    COALESCE(sr.SizeRunId, s.SizeRunIdRaw) AS SizeRunId,
    COALESCE(sz.SizeRunSizeId, s.BaseSizeRaw) AS BaseSizeDetailId,
    CASE WHEN UPPER(ISNULL(uom.EntityValue, N'')) LIKE N'%INCH%' THEN N'INCH' ELSE N'CM' END AS UnitOfMeasure
  FROM src s
  LEFT JOIN dbo.TchpSizeRun sr ON sr.SizeRunId = s.SizeRunIdRaw
  LEFT JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunSizeId = s.BaseSizeRaw
    OR (sz.SizeRunId = COALESCE(sr.SizeRunId, s.SizeRunIdRaw) AND sz.SizeRunSizeId = s.BaseSizeRaw)
  LEFT JOIN dbo.AppEntityInfo uom ON uom.EntityCode = N'UnitOfMeasure'
   AND uom.EntityKeyId = TRY_CONVERT(INT, s.MeasureUnitRaw)
  WHERE COALESCE(sr.SizeRunId, s.SizeRunIdRaw) IS NOT NULL
    AND COALESCE(sz.SizeRunSizeId, s.BaseSizeRaw) IS NOT NULL
) AS x ON x.ProductReferenceId = t.ProductReferenceId
WHEN MATCHED THEN UPDATE SET SizeRunId = x.SizeRunId, BaseSizeDetailId = x.BaseSizeDetailId,
  UnitOfMeasure = x.UnitOfMeasure, AppModifiedDate = GETDATE()
WHEN NOT MATCHED THEN INSERT (ProductReferenceId, SizeRunId, BaseSizeDetailId, UnitOfMeasure, AppCreatedDate)
VALUES (x.ProductReferenceId, x.SizeRunId, x.BaseSizeDetailId, x.UnitOfMeasure, GETDATE());
';
EXEC sp_executesql @sql;
PRINT N'TchpStyleSpec MERGE done.';

-- 2. TchpPomSpecLine
SET @sql = N'
INSERT INTO dbo.TchpPomSpecLine (StyleSpecId, BodyPartId, BaseValue, Tolerance, IsFixed, Sort, BodypartAliasName, AppCreatedDate)
SELECT ss.StyleSpecId, COALESCE(bp.BodyPartId, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart)),
  TRY_CONVERT(DECIMAL(10,3), g.GradingBaseSize_173), TRY_CONVERT(DECIMAL(10,3), g.Tolerance_172), CASE WHEN UPPER(ISNULL(CONVERT(NVARCHAR(20), g.NeedToApplyGradingRule_651), N'''')) IN (N''0'', N''N'', N''NO'', N''FALSE'') THEN 1 ELSE 0 END, g.Sort, CONVERT(NVARCHAR(50), g.BodyPartName_169), GETDATE()
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = g.ProductReferenceID
LEFT JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart)
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM dbo.TchpPomSpecLine pl
    WHERE pl.StyleSpecId = ss.StyleSpecId
      AND pl.BodyPartId = COALESCE(bp.BodyPartId, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart))
  );
';
EXEC sp_executesql @sql;
PRINT N'TchpPomSpecLine insert done.';

-- 3. TchpGradeValue
SET @sql = N'
;WITH unpvt AS (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  1 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize1_174) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize1_174) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  2 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize2_177) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize2_177) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  3 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize3_180) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize3_180) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  4 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize4_183) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize4_183) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  5 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize5_186) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize5_186) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  6 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize6_189) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize6_189) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  7 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize7_192) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize7_192) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  8 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize8_195) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize8_195) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  9 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize9_198) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize9_198) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  10 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize10_201) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize10_201) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  11 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize11_204) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize11_204) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  12 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize12_207) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize12_207) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  13 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize13_210) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize13_210) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  14 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize14_213) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize14_213) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  15 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize15_216) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize15_216) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  16 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize16_219) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize16_219) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  17 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize17_222) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize17_222) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  18 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize18_225) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize18_225) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  19 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize19_228) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize19_228) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  20 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize20_231) AS DeltaVal
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize20_231) IS NOT NULL
)
INSERT INTO dbo.TchpGradeValue (PomSpecLineId, SizeRunSizeId, GradingDelta, AppCreatedDate)
SELECT pl.PomSpecLineId, sz.SizeRunSizeId, u.DeltaVal, GETDATE()
FROM unpvt u
INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = u.ProductReferenceID
INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = u.BodyPartRaw
INNER JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunId = ss.SizeRunId AND ISNULL(sz.Sort, 0) = u.SizeOrdinal
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.TchpGradeValue gv
  WHERE gv.PomSpecLineId = pl.PomSpecLineId AND gv.SizeRunSizeId = sz.SizeRunSizeId
);
';
EXEC sp_executesql @sql;
PRINT N'TchpGradeValue insert done.';

-- 4. TchpFitRound + TchpFitMeasurement
SET @sql = N'
INSERT INTO dbo.TchpFitRound (StyleSpecId, RoundNumber, RoundType, RoundStatus, AppCreatedDate)
SELECT DISTINCT ss.StyleSpecId, r.RoundNumber, N'INTERNAL', N'PENDING', GETDATE()
FROM (
  SELECT DISTINCT ProductReferenceID, RoundNumber FROM (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  1 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise1_31, g.Sample1_30)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise1_31, g.Sample1_30)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  2 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise2_33, g.Sample2_32)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise2_33, g.Sample2_32)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  3 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise3_35, g.Sample3_34)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise3_35, g.Sample3_34)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  4 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise4_37, g.Sample4_36)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise4_37, g.Sample4_36)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  5 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise5_328, g.Sample5_326)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise5_328, g.Sample5_326)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  6 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise6_331, g.Sample6_329)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise6_331, g.Sample6_329)) IS NOT NULL
  ) x
) r
INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = r.ProductReferenceID
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.TchpFitRound fr
  WHERE fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = r.RoundNumber
);

;WITH meas AS (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  1 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise1_31, g.Sample1_30)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise1_31, g.Sample1_30)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  2 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise2_33, g.Sample2_32)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise2_33, g.Sample2_32)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  3 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise3_35, g.Sample3_34)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise3_35, g.Sample3_34)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  4 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise4_37, g.Sample4_36)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise4_37, g.Sample4_36)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  5 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise5_328, g.Sample5_326)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise5_328, g.Sample5_326)) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  6 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise6_331, g.Sample6_329)) AS ActualValue
FROM ' + @DwTwoPart + N'.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE(g.Revise6_331, g.Sample6_329)) IS NOT NULL
)
INSERT INTO dbo.TchpFitMeasurement (FitRoundId, PomSpecLineId, ActualValue, AppCreatedDate)
SELECT fr.FitRoundId, pl.PomSpecLineId, m.ActualValue, GETDATE()
FROM meas m
INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = m.ProductReferenceID
INNER JOIN dbo.TchpFitRound fr ON fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = m.RoundNumber
INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = m.BodyPartRaw
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.TchpFitMeasurement fm
  WHERE fm.FitRoundId = fr.FitRoundId AND fm.PomSpecLineId = pl.PomSpecLineId
);
';
EXEC sp_executesql @sql;
PRINT N'TchpFitRound / TchpFitMeasurement insert done.';

PRINT N'TechPack Tchp import batch finished.';
GO
