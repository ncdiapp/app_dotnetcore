-- =============================================================================
-- 5_Tchp_Import_Validate.sql  | RunId: pilot1
-- Connect to: TenantDB_PLM27 (same server as PLM + ERP)
-- =============================================================================
SET NOCOUNT ON;

DECLARE @PlmDb sysname = N'plm_live_20260602';
DECLARE @ErpDb sysname = N'SourceERP';

PRINT '=== Target counts ===';
SELECT N'TchpSizeRun' AS T, COUNT(*) AS Cnt FROM dbo.TchpSizeRun
UNION ALL SELECT N'TchpSizeRunSize', COUNT(*) FROM dbo.TchpSizeRunSize
UNION ALL SELECT N'TchpBodyPart', COUNT(*) FROM dbo.TchpBodyPart
UNION ALL SELECT N'TchpPomTemplate', COUNT(*) FROM dbo.TchpPomTemplate
UNION ALL SELECT N'TchpPomTemplatePart', COUNT(*) FROM dbo.TchpPomTemplatePart
UNION ALL SELECT N'TchpGradeRuleSet', COUNT(*) FROM dbo.TchpGradeRuleSet
UNION ALL SELECT N'TchpGradeRule', COUNT(*) FROM dbo.TchpGradeRule
UNION ALL SELECT N'TchpGradeRuleSet_CUSTOM', COUNT(*) FROM dbo.TchpGradeRuleSet WHERE Standard = N'CUSTOM'
UNION ALL SELECT N'TchpGradeRule_CUSTOM', COUNT(*) FROM dbo.TchpGradeRule WHERE GradeRuleSetId >= 17;

PRINT '=== Expected vs actual ===';
SELECT N'SizeRun_expected_visible' AS Metric,
    (SELECT COUNT(*) FROM SourceERP.dbo.tblSizeRun WHERE ISNULL(isVisibleInPLM,1)=1) AS Expected,
    (SELECT COUNT(*) FROM dbo.TchpSizeRun) AS Actual;

SELECT N'SizeRotate_expected_visible' AS Metric,
    (SELECT COUNT(*) FROM SourceERP.dbo.tblSizeRunRotate r
     INNER JOIN SourceERP.dbo.tblSizeRun sr ON sr.SizeRunId=r.SizeRunId AND ISNULL(sr.isVisibleInPLM,1)=1) AS Expected,
    (SELECT COUNT(*) FROM dbo.TchpSizeRunSize) AS Actual;

SELECT N'BodyPart_expected' AS Metric,
    (SELECT COUNT(*) FROM plm_live_20260602.dbo.pdmV2kBodyPart) AS Expected,
    (SELECT COUNT(*) FROM dbo.TchpBodyPart) AS Actual;

SELECT N'Template_expected' AS Metric,
    (SELECT COUNT(*) FROM plm_live_20260602.dbo.pdmv2kBodyType) AS Expected,
    (SELECT COUNT(*) FROM dbo.TchpPomTemplate) AS Actual;

SELECT N'TemplatePart_expected' AS Metric,
    (SELECT COUNT(*) FROM plm_live_20260602.dbo.pdmV2kBodyTypeDetail) AS Expected,
    (SELECT COUNT(*) FROM dbo.TchpPomTemplatePart) AS Actual;

PRINT '=== Orphans / integrity ===';
SELECT N'Orphan_TemplatePart_BodyPart' AS Issue, COUNT(*) AS Cnt
FROM dbo.TchpPomTemplatePart p
LEFT JOIN dbo.TchpBodyPart b ON b.BodyPartId = p.BodyPartId
WHERE b.BodyPartId IS NULL;

SELECT N'Orphan_Template_DefaultBaseSize' AS Issue, COUNT(*) AS Cnt
FROM dbo.TchpPomTemplate t
WHERE t.DefaultBaseSizeId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.TchpSizeRunSize s WHERE s.SizeRunSizeId = t.DefaultBaseSizeId);

SELECT N'GradeRule_Code_not_in_BodyPart' AS Issue, COUNT(*) AS Cnt
FROM dbo.TchpGradeRule g
WHERE g.GradeRuleSetId >= 17
  AND NOT EXISTS (SELECT 1 FROM dbo.TchpBodyPart b WHERE b.Code = g.BodyPartCode);

SELECT N'Dup_BodyPart_Code' AS Issue, COUNT(*) AS Cnt
FROM (SELECT Code FROM dbo.TchpBodyPart GROUP BY Code HAVING COUNT(*)>1) x;

PRINT '=== Templates DefaultBaseSizeId (NULL = not in S-A visible sizes) ===';
SELECT PomTemplateId, TemplateCode, TemplateName, DefaultBaseSizeId
FROM dbo.TchpPomTemplate
ORDER BY PomTemplateId;

PRINT '5_Tchp_Import_Validate.sql DONE';
GO
