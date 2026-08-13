-- =============================================================================
-- TechPack Tchp* import from plmDW (D1) â€” STATIC SQL (no dynamic @sql).
-- L2: TchpStyleSpec.StyleSpecId = Root.ReferenceId (no identity; sibling PK = parent PK).
-- S1: SizeRun/BaseSize/UOM from Grading tab 4006 ().
-- UOM: PLM tblUnitOfMeasure (not on tenant) -> CM|INCH; unmatched defaults to CM.
-- SpecFit ActualValue = SampleN only (PLM Meas N). ReviseN is Rev.Spec â€” do not COALESCE into ActualValue.
-- Blank-safe NULLIF on Sample; Comments tabs do not host Fit grid.
-- Prerequisites: Tchp foundation (ImportPlmPomAndGrading); Plm_* steps 1-3.
-- Size_Run= Base_Size= Measure_Unit=
-- SpecGrading= SpecFit= PlmUom=[plm_live_20260602].dbo.tblUnitOfMeasure
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'WARN: StyleSpec header skipped â€” Grading DW / Size_Run not resolved.';

PRINT N'WARN: PomSpecLine/GradeValue skipped â€” SpecGrading not resolved.';

PRINT N'WARN: FitRound/Measurement skipped â€” SpecFit not resolved.';

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

-- =============================================================================
-- F2: View_TchpPomSpecLine (FIT ROUND PomSpecLine DDL â€” BodyPartName + BaseValue/Tol)
-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql
-- =============================================================================
CREATE OR ALTER VIEW dbo.View_TchpPomSpecLine
AS
SELECT
    pl.PomSpecLineId,
    bp.BodyPartName,
    pl.StyleSpecId,
    pl.GradeRuleSetId,
    pl.BaseValue,
    pl.Tolerance,
    pl.IsFixed,
    pl.Sort,
    pl.BodypartAliasName
FROM dbo.TchpPomSpecLine AS pl
INNER JOIN dbo.TchpBodyPart AS bp
    ON bp.BodyPartId = pl.BodyPartId;
GO
PRINT N'View_TchpPomSpecLine created/altered.';
GO

-- =============================================================================
-- QX1: QcSelectedSizes + View_TchpSimpleQcSelectedSizes (Simple QC pivot domain)
-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql
-- =============================================================================
IF COL_LENGTH(N'dbo.TchpStyleSpec', N'QcSelectedSizes') IS NULL
BEGIN
    ALTER TABLE dbo.TchpStyleSpec ADD QcSelectedSizes NVARCHAR(4000) NULL;
    PRINT N'Added TchpStyleSpec.QcSelectedSizes';
END
GO

CREATE OR ALTER VIEW dbo.View_TchpSimpleQcSelectedSizes
AS
SELECT
    ss.StyleSpecId,
    ss.SizeRunId,
    srs.SizeRunSizeId,
    srs.SizeLabel,
    srs.SizeOrder,
    srs.IsActive,
    CASE
        WHEN NULLIF(LTRIM(RTRIM(ss.QcSelectedSizes)), N'') IS NULL THEN 1
        WHEN EXISTS (
            SELECT 1
            FROM STRING_SPLIT(REPLACE(ss.QcSelectedSizes, N'|', N','), N',') AS tok
            WHERE TRY_CONVERT(INT, LTRIM(RTRIM(tok.[value]))) = srs.SizeRunSizeId
        ) THEN 1
        ELSE 0
    END AS IsVisible
FROM dbo.TchpStyleSpec AS ss
INNER JOIN dbo.TchpSizeRunSize AS srs
    ON srs.SizeRunId = ss.SizeRunId
WHERE ISNULL(srs.IsActive, 1) = 1;
GO
PRINT N'View_TchpSimpleQcSelectedSizes created/altered.';
GO
