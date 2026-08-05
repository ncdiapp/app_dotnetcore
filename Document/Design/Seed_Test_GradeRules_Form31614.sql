-- =============================================================================
-- Test data for ApplyGradeRuleSet against Grading form ProductReferenceId = 31614
-- (TchpPomSpecLine BodyPartId 142 / 143 / 144 as shown on screen)
--
-- ApplyGradeRuleSet does:
--   1. Load TchpGradeRule rows for @RuleSetId
--   2. For each NON-FIXED PomSpecLine on the StyleSpec, match BodyPart.Code
--   3. Generate per-size GradingDelta and MERGE into TchpGradeValue
--   4. Does NOT update the form grid by itself — Refresh after Command
--
-- Command DictOneToOneFields must supply:
--   RuleSetId   = @RuleSetId from this script
--   StyleSpecId = StyleSpecId for ProductReferenceId 31614
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ProductReferenceId INT = 31614;
DECLARE @StyleSpecId INT;
DECLARE @RuleSetId INT;
DECLARE @RuleSetName NVARCHAR(100) = N'Test Grade Rules — Form 31614';

SELECT @StyleSpecId = StyleSpecId
FROM dbo.TchpStyleSpec
WHERE ProductReferenceId = @ProductReferenceId;

IF @StyleSpecId IS NULL
BEGIN
    RAISERROR(N'No TchpStyleSpec for ProductReferenceId=%d. Import / create StyleSpec first.', 16, 1, @ProductReferenceId);
    RETURN;
END

PRINT N'StyleSpecId=' + CAST(@StyleSpecId AS NVARCHAR(20));

-- Show current POM lines + BodyPart.Code (rules must match Code, not BodyPartId)
SELECT
    psl.PomSpecLineId,
    psl.BodyPartId,
    bp.Code AS BodyPartCode,
    bp.Name AS BodyPartName,
    psl.BaseValue,
    psl.IsFixed,
    psl.GradeRuleSetId,
    psl.BodypartAliasName
FROM dbo.TchpPomSpecLine psl
INNER JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = psl.BodyPartId
WHERE psl.StyleSpecId = @StyleSpecId
ORDER BY ISNULL(psl.Sort, 0), psl.PomSpecLineId;

BEGIN TRAN;

-- 1) Grade rule set
IF NOT EXISTS (SELECT 1 FROM dbo.TchpGradeRuleSet WHERE GradeRuleSetName = @RuleSetName)
BEGIN
    INSERT INTO dbo.TchpGradeRuleSet (GradeRuleSetName, Description, Standard, IsActive, AppCreatedDate)
    VALUES (@RuleSetName, N'Test rules for ApplyGradeRuleSet on form 31614', N'CUSTOM', 1, GETDATE());
END

SELECT @RuleSetId = GradeRuleSetId
FROM dbo.TchpGradeRuleSet
WHERE GradeRuleSetName = @RuleSetName;

PRINT N'GradeRuleSetId=' + CAST(@RuleSetId AS NVARCHAR(20));

-- 2) Rules for body parts used on this form (142 / 143 / 144 preferred; fallback = all lines on this spec)
;WITH TargetParts AS (
    SELECT DISTINCT bp.BodyPartId, bp.Code
    FROM dbo.TchpPomSpecLine psl
    INNER JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = psl.BodyPartId
    WHERE psl.StyleSpecId = @StyleSpecId
      AND (
            psl.BodyPartId IN (142, 143, 144)
            OR NOT EXISTS (
                SELECT 1
                FROM dbo.TchpPomSpecLine x
                WHERE x.StyleSpecId = @StyleSpecId AND x.BodyPartId IN (142, 143, 144)
            )
      )
)
INSERT INTO dbo.TchpGradeRule (
    GradeRuleSetId, BodyPartCode, GradingPlusValue, GradingMinuValue, IsSymmetric, Sort, AppCreatedDate
)
SELECT
    @RuleSetId,
    tp.Code,
    CASE tp.BodyPartId
        WHEN 142 THEN 1.000  -- Waist extended
        WHEN 143 THEN 0.500  -- Front waistband
        WHEN 144 THEN 0.250  -- Waistband width
        ELSE 1.000
    END,
    CASE tp.BodyPartId
        WHEN 142 THEN 1.000
        WHEN 143 THEN 0.500
        WHEN 144 THEN 0.250
        ELSE 1.000
    END,
    1,
    ROW_NUMBER() OVER (ORDER BY tp.BodyPartId),
    GETDATE()
FROM TargetParts tp
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.TchpGradeRule r
    WHERE r.GradeRuleSetId = @RuleSetId
      AND r.BodyPartCode = tp.Code
);

-- 3) CRITICAL: Apply only processes IsFixed = 0. Screen currently has all Fixed checked.
UPDATE dbo.TchpPomSpecLine
SET IsFixed = 0,
    GradeRuleSetId = @RuleSetId,
    AppModifiedDate = GETDATE()
WHERE StyleSpecId = @StyleSpecId
  AND BodyPartId IN (142, 143, 144);

-- If those BodyPartIds are not present, unfix all lines on this spec for testing:
UPDATE dbo.TchpPomSpecLine
SET IsFixed = 0,
    GradeRuleSetId = ISNULL(GradeRuleSetId, @RuleSetId),
    AppModifiedDate = GETDATE()
WHERE StyleSpecId = @StyleSpecId
  AND NOT EXISTS (
      SELECT 1 FROM dbo.TchpPomSpecLine x
      WHERE x.StyleSpecId = @StyleSpecId AND x.BodyPartId IN (142, 143, 144)
  );

COMMIT;

-- Verify
SELECT GradeRuleSetId, GradeRuleSetName, Standard, IsActive
FROM dbo.TchpGradeRuleSet WHERE GradeRuleSetId = @RuleSetId;

SELECT GradeRuleId, BodyPartCode, GradingPlusValue, GradingMinuValue, IsSymmetric, Sort
FROM dbo.TchpGradeRule WHERE GradeRuleSetId = @RuleSetId
ORDER BY Sort;

SELECT
    psl.PomSpecLineId,
    psl.BodyPartId,
    bp.Code,
    psl.IsFixed,
    psl.GradeRuleSetId,
    psl.BaseValue,
    psl.BodypartAliasName
FROM dbo.TchpPomSpecLine psl
INNER JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = psl.BodyPartId
WHERE psl.StyleSpecId = @StyleSpecId
ORDER BY ISNULL(psl.Sort, 0);

PRINT N'=== Use these values in Command DictOneToOneFields ===';
PRINT N'RuleSetId=' + CAST(@RuleSetId AS NVARCHAR(20));
PRINT N'StyleSpecId=' + CAST(@StyleSpecId AS NVARCHAR(20));
PRINT N'Then click External Method ApplyGradeRuleSet and Refresh the form.';
GO
