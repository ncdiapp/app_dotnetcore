-- Phase A probe: PLM source DB — one TemplateId (set @TemplateId before running).
-- Returns: template header, tab list (Sort, IsTemplateHeaderTab), product reference count.

SET NOCOUNT ON;

DECLARE @TemplateId INT = NULL;  -- <<< SET before run, e.g. 42

IF @TemplateId IS NULL
BEGIN
    RAISERROR(N'Set @TemplateId before running _plm_probe_template.sql', 16, 1);
    RETURN;
END

PRINT '=== Template ===';
SELECT
    t.TemplateID,
    t.TemplateName,
    t.Description,
    t.FolderId
FROM dbo.pdmTemplate t
WHERE t.TemplateID = @TemplateId;

IF @@ROWCOUNT = 0
BEGIN
    RAISERROR(N'TemplateID %d not found in pdmTemplate.', 16, 1, @TemplateId);
    RETURN;
END

PRINT '=== Template tabs (pdmTemplateTab + pdmTab) ===';
SELECT
    tt.TabID,
    tab.TabName,
    tt.Sort AS TabSort,
    tab.IsTemplateHeaderTab,
    tab.IsMasterReferenceHeaderTab,
    tab.IsAllowProductTabCopy
FROM dbo.pdmTemplateTab tt
INNER JOIN dbo.pdmTab tab ON tab.TabID = tt.TabID
WHERE tt.TemplateID = @TemplateId
ORDER BY tt.Sort, tt.TabID;

PRINT '=== Product references in template (import scope) ===';
SELECT COUNT(DISTINCT pt.ProductReferenceID) AS DistinctProductReferenceCount
FROM dbo.pdmProductTemplate pt
WHERE pt.TemplateID = @TemplateId
  AND pt.ProductReferenceID IS NOT NULL;

PRINT '=== Sample ProductReferenceIDs (top 20) ===';
SELECT TOP 20 pt.ProductReferenceID
FROM dbo.pdmProductTemplate pt
WHERE pt.TemplateID = @TemplateId
  AND pt.ProductReferenceID IS NOT NULL
ORDER BY pt.ProductReferenceID;
