-- Re-runnable counts for RunId: pilot1
-- PLM: plm_live_20260602 | ERP (SizeRun): SourceERP | Target: TenantDB_PLM27
-- Do NOT store passwords in this file.

-- === SizeRun resolution (pdmEntity) ===
-- SizeRun EntityID=10, SizeRunDetail EntityID=63, DataSourceFrom=2 (ERP)

-- ERP: SizeRun (active filter per preview: isVisibleInPLM = 1)
SELECT N'SizeRun_visible' AS Metric, COUNT(*) AS Cnt
FROM SourceERP.dbo.tblSizeRun WHERE ISNULL(isVisibleInPLM, 1) = 1;

SELECT N'SizeRun_all' AS Metric, COUNT(*) AS Cnt
FROM SourceERP.dbo.tblSizeRun;

-- ERP: SizeRunRotate (only sizes under visible runs)
SELECT N'SizeRotate_visibleRuns' AS Metric, COUNT(*) AS Cnt
FROM SourceERP.dbo.tblSizeRunRotate r
INNER JOIN SourceERP.dbo.tblSizeRun sr ON sr.SizeRunId = r.SizeRunId
WHERE ISNULL(sr.isVisibleInPLM, 1) = 1;

-- PLM: POM
SELECT N'BodyPart_all' AS Metric, COUNT(*) AS Cnt FROM plm_live_20260602.dbo.pdmV2kBodyPart;
SELECT N'BodyType_all' AS Metric, COUNT(*) AS Cnt FROM plm_live_20260602.dbo.pdmv2kBodyType;
SELECT N'BodyTypeDetail_all' AS Metric, COUNT(*) AS Cnt FROM plm_live_20260602.dbo.pdmV2kBodyTypeDetail;
SELECT N'SpecGrading_all' AS Metric, COUNT(*) AS Cnt FROM plm_live_20260602.dbo.pdmV2kSpecBodyPartGrading;

-- Data quality
SELECT N'BodyPart_nullCode' AS Metric, COUNT(*) AS Cnt
FROM plm_live_20260602.dbo.pdmV2kBodyPart
WHERE Code IS NULL OR LTRIM(RTRIM(Code)) = N'';

SELECT N'BodyPart_dupCodeGroups' AS Metric, COUNT(*) AS Cnt
FROM (
    SELECT Code FROM plm_live_20260602.dbo.pdmV2kBodyPart
    WHERE Code IS NOT NULL AND LTRIM(RTRIM(Code)) <> N''
    GROUP BY Code HAVING COUNT(*) > 1
) x;

-- G-B join
SELECT N'Grading_joinable' AS Metric, COUNT(*) AS Cnt
FROM plm_live_20260602.dbo.pdmV2kSpecBodyPartGrading g
INNER JOIN plm_live_20260602.dbo.pdmV2kBodyTypeDetail d ON d.BodyTypeDetailID = g.BodyTypeDetailID;

-- Target (TenantDB_PLM27)
SELECT N'TchpSizeRun' AS Metric, COUNT(*) AS Cnt FROM TenantDB_PLM27.dbo.TchpSizeRun;
SELECT N'TchpBodyPart' AS Metric, COUNT(*) AS Cnt FROM TenantDB_PLM27.dbo.TchpBodyPart;
SELECT N'TchpGradeRuleSet_seed' AS Metric, COUNT(*) AS Cnt FROM TenantDB_PLM27.dbo.TchpGradeRuleSet;
