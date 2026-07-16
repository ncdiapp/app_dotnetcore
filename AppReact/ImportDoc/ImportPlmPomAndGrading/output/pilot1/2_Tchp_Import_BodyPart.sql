-- =============================================================================
-- 2_Tchp_Import_BodyPart.sql  | RunId: pilot1 | Decisions: B-A, all 540
-- Connect to: TenantDB_PLM27
-- Source: plm_live_20260602.dbo.pdmV2kBodyPart
-- Code: empty -> BP_{id}; duplicate Code groups -> Code_{BodyPartID} (trunc 50)
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PlmDb sysname = N'plm_live_20260602';
DECLARE @Now datetime = GETDATE();

BEGIN TRAN;

DECLARE @sql nvarchar(max) = N'
SET IDENTITY_INSERT dbo.TchpBodyPart ON;

;WITH src AS (
    SELECT
        bp.BodyPartID,
        LTRIM(RTRIM(bp.Code)) AS RawCode,
        LEFT(LTRIM(RTRIM(bp.BodyPartName)), 100) AS BodyPartName,
        bp.Tolerance,
        ISNULL(bp.GradingPlusValue, 0) AS GradingPlusValue,
        ISNULL(bp.GradingMinuValue, 0) AS GradingMinuValue,
        COUNT(*) OVER (
            PARTITION BY CASE
                WHEN bp.Code IS NULL OR LTRIM(RTRIM(bp.Code)) = N'''' THEN NULL
                ELSE LTRIM(RTRIM(bp.Code))
            END
        ) AS CodeGroupCnt
    FROM ' + QUOTENAME(@PlmDb) + N'.dbo.pdmV2kBodyPart AS bp
),
mapped AS (
    SELECT
        BodyPartID AS BodyPartId,
        LEFT(
            CASE
                WHEN RawCode IS NULL OR RawCode = N'''' THEN
                    N''BP_'' + CAST(BodyPartID AS nvarchar(20))
                WHEN CodeGroupCnt > 1 THEN
                    LEFT(RawCode, 40) + N''_'' + CAST(BodyPartID AS nvarchar(20))
                ELSE
                    RawCode
            END
        , 50) AS Code,
        BodyPartName,
        Tolerance,
        GradingPlusValue,
        GradingMinuValue,
        CAST(1 AS bit) AS IsActive
    FROM src
)
MERGE dbo.TchpBodyPart AS t
USING mapped AS s
ON t.BodyPartId = s.BodyPartId
WHEN MATCHED THEN UPDATE SET
    Code = s.Code,
    BodyPartName = s.BodyPartName,
    Tolerance = s.Tolerance,
    GradingPlusValue = s.GradingPlusValue,
    GradingMinuValue = s.GradingMinuValue,
    IsActive = s.IsActive,
    AppModifiedDate = @Now
WHEN NOT MATCHED BY TARGET THEN INSERT
    (BodyPartId, Code, BodyPartName, Tolerance, GradingPlusValue, GradingMinuValue, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.BodyPartId, s.Code, s.BodyPartName, s.Tolerance, s.GradingPlusValue, s.GradingMinuValue, s.IsActive, @Now, @Now);

SET IDENTITY_INSERT dbo.TchpBodyPart OFF;
';

EXEC sp_executesql @sql, N'@Now datetime', @Now = @Now;

DECLARE @n int = (SELECT COUNT(*) FROM dbo.TchpBodyPart);
PRINT 'TchpBodyPart rows: ' + CAST(@n AS nvarchar(20));
PRINT '2_Tchp_Import_BodyPart.sql DONE';

COMMIT TRAN;
GO
