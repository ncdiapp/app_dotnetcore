-- =============================================================================
-- Simple QC (QX1): SpecQCGrid -> Plm_SimpleQC + Plm_SimpleQCResult UNPIVOT
-- Size Index N = SizeOrder position in SizeRun (PLM GetDictSortSizeRelatedRotateSizeId).
-- Source grid: PLM_DW_Grid_SpecQCGrid_22  MaxSlot=20
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- StyleSpec.QcSelectedSizes <- QC Tab Selected_Size (pipe SizeRunSizeId)
IF COL_LENGTH(N'dbo.TchpStyleSpec', N'QcSelectedSizes') IS NULL
    ALTER TABLE dbo.TchpStyleSpec ADD QcSelectedSizes NVARCHAR(4000) NULL;

UPDATE ss SET
  ss.QcSelectedSizes = CONVERT(NVARCHAR(4000), q.Selected_Size_174),
  ss.AppModifiedDate = GETDATE()
FROM dbo.TchpStyleSpec ss
INNER JOIN [plmDW].dbo.PLM_DW_Tab_QC_4029 q ON TRY_CONVERT(INT, q.ProductReferenceID) = ss.StyleSpecId
WHERE NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(4000), q.Selected_Size_174))), N'''') IS NOT NULL;
PRINT N'TchpStyleSpec.QcSelectedSizes updated. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- Host POM rows -> Plm_SimpleQC
DELETE FROM dbo.[Plm_SimpleQCResult];
DELETE FROM dbo.[Plm_SimpleQC];
INSERT INTO dbo.[Plm_SimpleQC] (
  ReferenceId, CriticalPoint, BodyPartDetailIDWDimDetailID, Code, BodyPartName, BodyPartDesc, HowToMeasure, Tolerance, GradingBaseSize, Commtents, AddDesc, DimensionDetail, Dimension, NeedToApplyGradingRule, Sort
)
SELECT
  TRY_CONVERT(INT, g.ProductReferenceID) AS ReferenceId,
  CONVERT(NVARCHAR(4000), g.CriticalPoint_557) AS [CriticalPoint],
  CONVERT(NVARCHAR(4000), g.BodyPartDetailIDWDimDetailID_558) AS [BodyPartDetailIDWDimDetailID],
  CONVERT(NVARCHAR(4000), g.Code_559) AS [Code],
  CONVERT(NVARCHAR(4000), g.BodyPartName_560) AS [BodyPartName],
  CONVERT(NVARCHAR(4000), g.BodyPartDesc_561) AS [BodyPartDesc],
  CONVERT(NVARCHAR(4000), g.HowToMeasure_562) AS [HowToMeasure],
  CONVERT(NVARCHAR(4000), g.Tolerance_563) AS [Tolerance],
  CONVERT(NVARCHAR(4000), g.GradingBaseSize_564) AS [GradingBaseSize],
  CONVERT(NVARCHAR(4000), g.Commtents_625) AS [Commtents],
  CONVERT(NVARCHAR(4000), g.Add_Desc_626) AS [AddDesc],
  CONVERT(NVARCHAR(4000), g.DimensionDetail_646_FK_tblDimensionDetail) AS [DimensionDetail],
  CONVERT(NVARCHAR(4000), g.Dimension_649_FK_tblDimension) AS [Dimension],
  CONVERT(NVARCHAR(4000), g.NeedToApplyGradingRule_652) AS [NeedToApplyGradingRule],
  CONVERT(NVARCHAR(4000), g.Sort) AS [Sort]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
WHERE TRY_CONVERT(INT, g.ProductReferenceID) IS NOT NULL;
PRINT N'Plm_SimpleQC insert. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- Size rows -> Plm_SimpleQCResult (N = SizeOrder)
;WITH slot AS (
SELECT
  h.RowId AS ParentRowId,
  1 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize1_565) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize1_566) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference1_567) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash1_668) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading1_669) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron1_708) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading1_709) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron1_749) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  2 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize2_568) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize2_569) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference2_570) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash2_670) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading2_671) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron2_710) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading2_711) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron2_750) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  3 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize3_571) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize3_572) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference3_573) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash3_672) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading3_673) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron3_712) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading3_713) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron3_751) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  4 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize4_574) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize4_575) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference4_576) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash4_674) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading4_675) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron4_714) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading4_715) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron4_752) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  5 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize5_577) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize5_578) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference5_579) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash5_676) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading5_677) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron5_716) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading5_717) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron5_753) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  6 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize6_580) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize6_581) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference6_582) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash6_678) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading6_679) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron6_718) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading6_719) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron6_754) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  7 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize7_583) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize7_584) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference7_585) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash7_680) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading7_681) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron7_720) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading7_721) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron7_755) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  8 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize8_586) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize8_587) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference8_588) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash8_682) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading8_683) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron8_722) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading8_723) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron8_756) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  9 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize9_589) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize9_590) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference9_591) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash9_684) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading9_685) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron9_724) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading9_725) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron9_757) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  10 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize10_592) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize10_593) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference10_594) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash10_686) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading10_687) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron10_726) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading10_727) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron10_758) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  11 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize11_595) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize11_596) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference11_597) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash11_688) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading11_689) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron11_728) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading11_729) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron11_759) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  12 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize12_598) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize12_599) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference12_600) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash12_690) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading12_691) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron12_730) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading12_731) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron12_760) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  13 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize13_601) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize13_602) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference13_603) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash13_692) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading13_693) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron13_732) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading13_733) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron13_761) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  14 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize14_604) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize14_605) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference14_606) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash14_694) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading14_695) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron14_734) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading14_735) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron14_762) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  15 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize15_607) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize15_608) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference15_609) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash15_696) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading15_697) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron15_736) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading15_737) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron15_763) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  16 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize16_610) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize16_611) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference16_612) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash16_698) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading16_699) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron16_738) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading16_739) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron16_764) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  17 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize17_613) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize17_614) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference17_615) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash17_700) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading17_701) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron17_740) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading17_741) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron17_765) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  18 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize18_616) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize18_617) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference18_618) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash18_702) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading18_703) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron18_742) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading18_743) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron18_766) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  19 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize19_619) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize19_620) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference19_621) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash19_704) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading19_705) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron19_744) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading19_745) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron19_767) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
UNION ALL
SELECT
  h.RowId AS ParentRowId,
  20 AS SizeOrdinal,
  CONVERT(NVARCHAR(4000), g.GradingSize20_622) AS [GradingSize],
  CONVERT(NVARCHAR(4000), g.QCSize20_623) AS [QCSize],
  CONVERT(NVARCHAR(4000), g.Difference20_624) AS [Difference],
  CONVERT(NVARCHAR(4000), g.QCSizeBeforeWash20_706) AS [QCSizeBeforeWash],
  CONVERT(NVARCHAR(4000), g.DiffBeforeWashAndGrading20_707) AS [DiffBeforeWashAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterWashIron20_746) AS [QCAfterWashIron],
  CONVERT(NVARCHAR(4000), g.DiffAfterIronAndGrading20_747) AS [DiffAfterIronAndGrading],
  CONVERT(NVARCHAR(4000), g.QCAfterIron20_768) AS [QCAfterIron]
FROM [plmDW].dbo.PLM_DW_Grid_SpecQCGrid_22 g
INNER JOIN dbo.[Plm_SimpleQC] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
)
INSERT INTO dbo.[Plm_SimpleQCResult] (ParentRowId, SizeRunSizeId, GradingSize, QCSize, Difference, QCSizeBeforeWash, DiffBeforeWashAndGrading, QCAfterWashIron, DiffAfterIronAndGrading, QCAfterIron)
SELECT s.ParentRowId, sz.SizeRunSizeId,
  s.[GradingSize], s.[QCSize], s.[Difference], s.[QCSizeBeforeWash], s.[DiffBeforeWashAndGrading], s.[QCAfterWashIron], s.[DiffAfterIronAndGrading], s.[QCAfterIron]
FROM slot s
INNER JOIN dbo.[Plm_SimpleQC] h ON h.RowId = s.ParentRowId
INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = h.ReferenceId
INNER JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunId = ss.SizeRunId AND ISNULL(sz.SizeOrder, 0) = s.SizeOrdinal;
PRINT N'Plm_SimpleQCResult insert. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));

PRINT N'Simple QC import finished.';
GO