-- =============================================================================
-- 4_Tchp_Import_GradeRules.sql  | RunId: pilot1 | D1 = G-B
-- Connect to: TenantDB_PLM27
-- One RuleSet per BodyType (GradeRuleSetId = BodyTypeID)
-- Rules from pdmV2kSpecBodyPartGrading; BodyPartCode = remapped TchpBodyPart.Code (B-A)
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PlmDb sysname = N'plm_live_20260602';
DECLARE @Now datetime = GETDATE();

BEGIN TRAN;

DECLARE @sql nvarchar(max);

-- RuleSets (templates that have >= 1 grading row)
SET @sql = N'
SET IDENTITY_INSERT dbo.TchpGradeRuleSet ON;

MERGE dbo.TchpGradeRuleSet AS t
USING (
    SELECT DISTINCT
        bt.BodyTypeID AS GradeRuleSetId,
        LEFT(N''Template: '' + LTRIM(RTRIM(bt.BodyTypeName)), 100) AS GradeRuleSetName,
        LEFT(N''Imported from pdmV2kSpecBodyPartGrading for BodyTypeID='' + CAST(bt.BodyTypeID AS nvarchar(20)), 800) AS Description,
        N''CUSTOM'' AS Standard,
        CAST(1 AS bit) AS IsActive
    FROM ' + QUOTENAME(@PlmDb) + N'.dbo.pdmv2kBodyType AS bt
    INNER JOIN ' + QUOTENAME(@PlmDb) + N'.dbo.pdmV2kBodyTypeDetail AS d ON d.BodyTypeID = bt.BodyTypeID
    INNER JOIN ' + QUOTENAME(@PlmDb) + N'.dbo.pdmV2kSpecBodyPartGrading AS g ON g.BodyTypeDetailID = d.BodyTypeDetailID
) AS s
ON t.GradeRuleSetId = s.GradeRuleSetId
WHEN MATCHED THEN UPDATE SET
    GradeRuleSetName = s.GradeRuleSetName,
    Description = s.Description,
    Standard = s.Standard,
    IsActive = s.IsActive,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (GradeRuleSetId, GradeRuleSetName, Description, Standard, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.GradeRuleSetId, s.GradeRuleSetName, s.Description, s.Standard, s.IsActive, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpGradeRuleSet OFF;
';
EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n1 int = (SELECT COUNT(*) FROM dbo.TchpGradeRuleSet);
PRINT 'TchpGradeRuleSet rows (incl ASTM seed): ' + CAST(@n1 AS nvarchar(20));

-- Rules: use already-imported TchpBodyPart.Code so B-A remapping is respected
SET @sql = N'
SET IDENTITY_INSERT dbo.TchpGradeRule ON;

;WITH ranked AS (
    SELECT
        g.BodyPartGradingID AS GradeRuleId,
        d.BodyTypeID AS GradeRuleSetId,
        bp.Code AS BodyPartCode,
        ISNULL(g.GradingPlusValue, 0) AS GradingPlusValue,
        ISNULL(g.GradingMinuValue, 0) AS GradingMinuValue,
        CASE WHEN ISNULL(g.GradingPlusValue, 0) = ISNULL(g.GradingMinuValue, 0) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsSymmetric,
        COALESCE(g.Sort, d.Sort, 0) AS Sort,
        ROW_NUMBER() OVER (
            PARTITION BY d.BodyTypeID, bp.Code
            ORDER BY g.BodyPartGradingID DESC
        ) AS rn
    FROM ' + QUOTENAME(@PlmDb) + N'.dbo.pdmV2kSpecBodyPartGrading AS g
    INNER JOIN ' + QUOTENAME(@PlmDb) + N'.dbo.pdmV2kBodyTypeDetail AS d ON d.BodyTypeDetailID = g.BodyTypeDetailID
    INNER JOIN dbo.TchpBodyPart AS bp ON bp.BodyPartId = d.BodyPartID
    INNER JOIN dbo.TchpGradeRuleSet AS rs ON rs.GradeRuleSetId = d.BodyTypeID
)
MERGE dbo.TchpGradeRule AS t
USING (SELECT * FROM ranked WHERE rn = 1) AS s
ON t.GradeRuleId = s.GradeRuleId
WHEN MATCHED THEN UPDATE SET
    GradeRuleSetId = s.GradeRuleSetId,
    BodyPartCode = s.BodyPartCode,
    GradingPlusValue = s.GradingPlusValue,
    GradingMinuValue = s.GradingMinuValue,
    IsSymmetric = s.IsSymmetric,
    Sort = s.Sort,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (GradeRuleId, GradeRuleSetId, BodyPartCode, GradingPlusValue, GradingMinuValue, IsSymmetric, Sort, AppCreatedDate, AppModifiedDate)
    VALUES (s.GradeRuleId, s.GradeRuleSetId, s.BodyPartCode, s.GradingPlusValue, s.GradingMinuValue, s.IsSymmetric, s.Sort, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpGradeRule OFF;
';
EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n2 int = (SELECT COUNT(*) FROM dbo.TchpGradeRule);
DECLARE @n3 int = (SELECT COUNT(*) FROM dbo.TchpGradeRule WHERE GradeRuleSetId >= 17);
PRINT 'TchpGradeRule rows (incl ASTM seed): ' + CAST(@n2 AS nvarchar(20));
PRINT 'Imported custom rules (SetId >= 17): ' + CAST(@n3 AS nvarchar(20));

COMMIT TRAN;
PRINT '4_Tchp_Import_GradeRules.sql DONE';
GO
