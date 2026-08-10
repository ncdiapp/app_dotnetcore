-- =============================================================================
-- TechPack Tchp* import from plmDW (D1) â€” STATIC SQL (no dynamic @sql).
-- L2: TchpStyleSpec.StyleSpecId = Root.ReferenceId (no identity; sibling PK = parent PK).
-- S1: SizeRun/BaseSize/UOM from Grading tab 4006 (PLM_DW_Tab_Grading_4006).
-- UOM: PLM tblUnitOfMeasure (not on tenant) -> CM|INCH; unmatched defaults to CM.
-- SpecFit ActualValue = SampleN only (PLM Meas N). ReviseN is Rev.Spec — do not COALESCE into ActualValue.
-- Blank-safe NULLIF on Sample; Comments tabs do not host Fit grid.
-- Prerequisites: Tchp foundation (ImportPlmPomAndGrading); Plm_* steps 1-3.
-- Size_Run=Size_Run_43_FK_tblSizeRun Base_Size=Base_Size_44_FK_tblSizeRunRotate Measure_Unit=Measure_Unit_58_FK_
-- SpecGrading=PLM_DW_Grid_SpecGradingGrid_10 SpecFit=PLM_DW_Grid_SpecFitGrid_5 PlmUom=[plm_live_20260602].dbo.tblUnitOfMeasure
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- 1. TchpStyleSpec (StyleSpecId = ProductReferenceID = ReferenceId)
;WITH src AS (
  SELECT TRY_CONVERT(INT, g.ProductReferenceID) AS StyleSpecId,
    TRY_CONVERT(INT, g.Size_Run_43_FK_tblSizeRun) AS SizeRunIdRaw,
    TRY_CONVERT(INT, g.Base_Size_44_FK_tblSizeRunRotate) AS BaseSizeRaw,
    CONVERT(NVARCHAR(50), g.Measure_Unit_58_FK_) AS MeasureUnitRaw
  FROM [plmDW].dbo.PLM_DW_Tab_Grading_4006 g
  WHERE TRY_CONVERT(INT, g.ProductReferenceID) IS NOT NULL
)
MERGE dbo.TchpStyleSpec AS t
USING (
  SELECT s.StyleSpecId,
    COALESCE(sr.SizeRunId, s.SizeRunIdRaw) AS SizeRunId,
    COALESCE(sz.SizeRunSizeId, s.BaseSizeRaw) AS BaseSizeDetailId,
    CASE
      WHEN UPPER(ISNULL(uom.Unit_Measure, N'')) LIKE N'%INCH%' THEN N'INCH'
      WHEN UPPER(ISNULL(uom.Unit_Measure, N'')) = N'IN' THEN N'INCH'
      WHEN UPPER(ISNULL(uom.Description, N'')) LIKE N'%INCH%' THEN N'INCH'
      ELSE N'CM'
    END AS UnitOfMeasure
  FROM src s
  LEFT JOIN dbo.TchpSizeRun sr ON sr.SizeRunId = s.SizeRunIdRaw
  LEFT JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunSizeId = s.BaseSizeRaw
    OR (sz.SizeRunId = COALESCE(sr.SizeRunId, s.SizeRunIdRaw) AND sz.SizeRunSizeId = s.BaseSizeRaw)
  LEFT JOIN [plm_live_20260602].dbo.tblUnitOfMeasure uom ON uom.Unit_Id = TRY_CONVERT(INT, s.MeasureUnitRaw)
  WHERE COALESCE(sr.SizeRunId, s.SizeRunIdRaw) IS NOT NULL
    AND COALESCE(sz.SizeRunSizeId, s.BaseSizeRaw) IS NOT NULL
) AS x ON x.StyleSpecId = t.StyleSpecId
WHEN MATCHED THEN UPDATE SET SizeRunId = x.SizeRunId, BaseSizeDetailId = x.BaseSizeDetailId,
  UnitOfMeasure = x.UnitOfMeasure, AppModifiedDate = GETDATE()
WHEN NOT MATCHED THEN INSERT (StyleSpecId, SizeRunId, BaseSizeDetailId, UnitOfMeasure, AppCreatedDate)
VALUES (x.StyleSpecId, x.SizeRunId, x.BaseSizeDetailId, x.UnitOfMeasure, GETDATE());
PRINT N'TchpStyleSpec MERGE done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 2. TchpPomSpecLine
INSERT INTO dbo.TchpPomSpecLine (StyleSpecId, BodyPartId, BaseValue, Tolerance, IsFixed, Sort, BodypartAliasName, AppCreatedDate)
SELECT ss.StyleSpecId, COALESCE(bp.BodyPartId, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart)),
  TRY_CONVERT(DECIMAL(10,3), g.GradingBaseSize_173), TRY_CONVERT(DECIMAL(10,3), g.Tolerance_172), CASE WHEN UPPER(ISNULL(CONVERT(NVARCHAR(20), g.NeedToApplyGradingRule_651), N'')) IN (N'0', N'N', N'NO', N'FALSE') THEN 1 ELSE 0 END, g.Sort, CONVERT(NVARCHAR(50), g.BodyPartName_169), GETDATE()
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, g.ProductReferenceID)
LEFT JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart)
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM dbo.TchpPomSpecLine pl
    WHERE pl.StyleSpecId = ss.StyleSpecId
      AND pl.BodyPartId = COALESCE(bp.BodyPartId, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart))
  );
PRINT N'TchpPomSpecLine insert done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 3. TchpGradeValue
;WITH unpvt AS (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  1 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize1_174) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize1_174) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  2 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize2_177) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize2_177) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  3 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize3_180) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize3_180) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  4 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize4_183) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize4_183) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  5 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize5_186) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize5_186) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  6 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize6_189) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize6_189) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  7 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize7_192) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize7_192) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  8 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize8_195) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize8_195) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  9 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize9_198) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize9_198) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  10 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize10_201) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize10_201) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  11 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize11_204) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize11_204) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  12 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize12_207) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize12_207) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  13 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize13_210) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize13_210) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  14 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize14_213) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize14_213) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  15 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize15_216) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize15_216) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  16 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize16_219) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize16_219) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  17 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize17_222) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize17_222) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  18 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize18_225) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize18_225) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  19 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize19_228) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize19_228) IS NOT NULL
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_167_FK_PdmV2kBodyPart) AS BodyPartRaw,
  20 AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.GradingSize20_231) AS DeltaVal
FROM [plmDW].dbo.PLM_DW_Grid_SpecGradingGrid_10 g
WHERE TRY_CONVERT(DECIMAL(10,3), g.GradingSize20_231) IS NOT NULL
)
INSERT INTO dbo.TchpGradeValue (PomSpecLineId, SizeRunSizeId, GradingDelta, AppCreatedDate)
SELECT pl.PomSpecLineId, sz.SizeRunSizeId, u.DeltaVal, GETDATE()
FROM unpvt u
INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, u.ProductReferenceID)
INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = u.BodyPartRaw
INNER JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunId = ss.SizeRunId AND ISNULL(sz.SizeOrder, 0) = u.SizeOrdinal
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.TchpGradeValue gv
  WHERE gv.PomSpecLineId = pl.PomSpecLineId AND gv.SizeRunSizeId = sz.SizeRunSizeId
);
PRINT N'TchpGradeValue insert done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 4. TchpFitRound + TchpFitMeasurement (R1: RoundNumber = N from SampleN/ReviseN)
-- ActualValue = SampleN (PLM Meas); ReviseN = Rev.Spec — not COALESCE'd into ActualValue.
INSERT INTO dbo.TchpFitRound (StyleSpecId, RoundNumber, RoundType, RoundStatus, AppCreatedDate)
SELECT DISTINCT ss.StyleSpecId, r.RoundNumber, N'FIT', N'PENDING', GETDATE()
FROM (
  SELECT DISTINCT ProductReferenceID, RoundNumber FROM (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  1 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample1_30)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample1_30)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise1_31)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  2 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample2_32)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample2_32)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise2_33)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  3 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample3_34)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample3_34)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise3_35)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  4 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample4_36)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample4_36)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise4_37)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  5 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample5_326)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample5_326)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise5_328)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  6 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample6_329)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample6_329)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise6_331)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  11 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample11_377)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample11_377)), N'')) IS NOT NULL
  ) x
) r
INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, r.ProductReferenceID)
WHERE NOT EXISTS (
  SELECT 1 FROM dbo.TchpFitRound fr
  WHERE fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = r.RoundNumber
);
PRINT N'TchpFitRound insert done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

;WITH meas AS (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  1 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample1_30)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample1_30)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise1_31)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  2 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample2_32)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample2_32)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise2_33)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  3 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample3_34)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample3_34)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise3_35)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  4 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample4_36)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample4_36)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise4_37)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  5 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample5_326)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample5_326)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise5_328)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  6 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample6_329)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample6_329)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise6_331)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  11 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample11_377)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample11_377)), N'')) IS NOT NULL
)
INSERT INTO dbo.TchpFitMeasurement (FitRoundId, PomSpecLineId, ActualValue, AppCreatedDate)
SELECT fr.FitRoundId, pl.PomSpecLineId, m.ActualValue, GETDATE()
FROM meas m
INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, m.ProductReferenceID)
INNER JOIN dbo.TchpFitRound fr ON fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = m.RoundNumber
INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = m.BodyPartRaw
WHERE m.ActualValue IS NOT NULL
  AND NOT EXISTS (
  SELECT 1 FROM dbo.TchpFitMeasurement fm
  WHERE fm.FitRoundId = fr.FitRoundId AND fm.PomSpecLineId = pl.PomSpecLineId
);
PRINT N'TchpFitMeasurement insert done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

;WITH meas AS (
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  1 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample1_30)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample1_30)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise1_31)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  2 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample2_32)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample2_32)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise2_33)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  3 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample3_34)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample3_34)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise3_35)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  4 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample4_36)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample4_36)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise4_37)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  5 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample5_326)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample5_326)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise5_328)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  6 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample6_329)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND (TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample6_329)), N'')) IS NOT NULL OR TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Revise6_331)), N'')) IS NOT NULL)
UNION ALL
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) AS BodyPartRaw,
  11 AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample11_377)), N'')) AS ActualValue
FROM [plmDW].dbo.PLM_DW_Grid_SpecFitGrid_5 g
WHERE TRY_CONVERT(INT, g.BodyPartDetailIDWDimDetailID_28) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.Sample11_377)), N'')) IS NOT NULL
)
UPDATE fm
SET fm.ActualValue = m.ActualValue
FROM dbo.TchpFitMeasurement fm
INNER JOIN dbo.TchpFitRound fr ON fr.FitRoundId = fm.FitRoundId
INNER JOIN dbo.TchpPomSpecLine pl ON pl.PomSpecLineId = fm.PomSpecLineId
INNER JOIN meas m ON TRY_CONVERT(INT, m.ProductReferenceID) = fr.StyleSpecId
  AND m.RoundNumber = fr.RoundNumber
  AND m.BodyPartRaw = pl.BodyPartId
WHERE m.ActualValue IS NOT NULL
  AND (fm.ActualValue IS NULL OR fm.ActualValue <> m.ActualValue);
PRINT N'TchpFitMeasurement update done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 4b. FX1 skeleton Plm_FitRoundInfo (FitRoundId = TchpFitRound.FitRoundId)
IF OBJECT_ID(N'dbo.Plm_FitRoundInfo', N'U') IS NOT NULL
BEGIN
  INSERT INTO dbo.Plm_FitRoundInfo (FitRoundId, StyleSpecId, AppCreatedDate)
  SELECT fr.FitRoundId, fr.StyleSpecId, GETDATE()
  FROM dbo.TchpFitRound fr
  WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Plm_FitRoundInfo i WHERE i.FitRoundId = fr.FitRoundId
  );
  PRINT N'Plm_FitRoundInfo skeleton insert done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));
END
ELSE
  PRINT N'WARN: Plm_FitRoundInfo missing - run step 1_ tables before 3b.';

PRINT N'TechPack Tchp import batch finished.';
GO

-- =============================================================================
-- V1: View_TchpStyleActiveSizeRunSizes (Grading ROOT read-only SizeRunSizes child)
-- IsVisible: Dimension filter AND VisibleSizes whitelist (pipe-delimited SizeRunSizeId).
-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql
-- Run this script BEFORE Phase D Blueprint Execute.
-- CREATE VIEW must be first statement in its batch (GO above required).
-- =============================================================================
CREATE OR ALTER VIEW dbo.View_TchpStyleActiveSizeRunSizes
AS
SELECT
    ss.StyleSpecId,
    ss.SizeRunId,
    srs.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive,
    CASE
        WHEN (
            CASE
                WHEN NOT EXISTS (
                    SELECT 1
                    FROM dbo.TchpStyleSpecDimension AS ssd
                    WHERE ssd.StyleSpecId = ss.StyleSpecId
                ) THEN 1
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.TchpSizeRunDimension AS srd
                    INNER JOIN dbo.TchpStyleSpecDimension AS ssd
                        ON ssd.StyleSpecId = ss.StyleSpecId
                       AND ssd.DimensionCode = srd.DimensionCode
                       AND (
                            ssd.IsActive = 1
                            OR NOT EXISTS (
                                SELECT 1
                                FROM dbo.TchpStyleSpecDimension AS x
                                WHERE x.StyleSpecId = ss.StyleSpecId
                                  AND x.IsActive = 1
                            )
                       )
                    WHERE srd.SizeRunSizeId = srs.SizeRunSizeId
                ) THEN 1
                ELSE 0
            END
        ) = 0 THEN 0
        WHEN NULLIF(LTRIM(RTRIM(ss.VisibleSizes)), N'') IS NULL THEN 1
        WHEN EXISTS (
            SELECT 1
            FROM STRING_SPLIT(REPLACE(ss.VisibleSizes, N'|', N','), N',') AS tok
            WHERE TRY_CONVERT(INT, LTRIM(RTRIM(tok.[value]))) = srs.SizeRunSizeId
        ) THEN 1
        ELSE 0
    END AS IsVisible
FROM dbo.TchpStyleSpec AS ss
INNER JOIN dbo.TchpSizeRunSize AS srs
    ON srs.SizeRunId = ss.SizeRunId
WHERE ISNULL(srs.IsActive, 1) = 1;
GO
PRINT N'View_TchpStyleActiveSizeRunSizes created/altered.';
GO

-- =============================================================================
-- View_TchpSizeRunSize_DefaultDimension: size + first DimensionCode
-- One row per SizeRunSizeId; if multiple DimensionCodes, first by SortOrder.
-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql
-- =============================================================================
IF OBJECT_ID(N'dbo.View_TchpSizeRunSize', N'V') IS NOT NULL
    DROP VIEW dbo.View_TchpSizeRunSize; -- rename: old short name
GO
CREATE OR ALTER VIEW dbo.View_TchpSizeRunSize_DefaultDimension
AS
SELECT
    srs.SizeRunSizeId,
    srs.SizeRunId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive,
    ISNULL(dim.DimensionCode, N'') AS DimensionCode
FROM dbo.TchpSizeRunSize AS srs
OUTER APPLY (
    SELECT TOP (1)
        srd.DimensionCode
    FROM dbo.TchpSizeRunDimension AS srd
    WHERE srd.SizeRunSizeId = srs.SizeRunSizeId
    ORDER BY srd.SortOrder ASC, srd.SizeRunDimensionId ASC
) AS dim;
GO
PRINT N'View_TchpSizeRunSize_DefaultDimension created/altered.';
GO

-- =============================================================================
-- F3: View_TchpFitMeasurementByPom (FIT SUMMARY POM Ã— Round pivot, read-only)
-- ChildUnitPivotColumns: IsPivotColumn=RoundNumber, IsPivotValue=ActualValue.
-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql
-- =============================================================================
CREATE OR ALTER VIEW dbo.View_TchpFitMeasurementByPom
AS
SELECT
    fm.FitMeasurementId,
    fm.PomSpecLineId,
    pl.StyleSpecId,
    fr.FitRoundId,
    fr.RoundNumber,
    fr.RoundType,
    CONCAT(N'Fit ', fr.RoundNumber) AS RoundLabel,
    fm.ActualValue
FROM dbo.TchpFitMeasurement AS fm
INNER JOIN dbo.TchpFitRound AS fr
    ON fr.FitRoundId = fm.FitRoundId
INNER JOIN dbo.TchpPomSpecLine AS pl
    ON pl.PomSpecLineId = fm.PomSpecLineId;
GO
PRINT N'View_TchpFitMeasurementByPom created/altered.';
GO
