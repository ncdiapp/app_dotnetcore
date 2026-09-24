-- =============================================================================
-- Phase A probe: PLM source DB — one SearchTemplateId (set before running).
-- Folder: ImportPLMSearchView/source/
-- Verified schema: plm_live_20260602
--   pdmSearchTemplate, pdmSearchTemplateDCU (criteria),
--   pdmReferenceView / pdmReferenceViewColumn (grid view),
--   pdmBLQuery (optional SQL — often NULL for template searches)
-- =============================================================================

SET NOCOUNT ON;

DECLARE @SearchTemplateId INT = NULL;  -- <<< SET before run, e.g. 23702

IF @SearchTemplateId IS NULL
BEGIN
    RAISERROR(N'Set @SearchTemplateId before running _plm_probe_search.sql', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- §0  Schema catalog (fallback when sections below fail on other PLM versions)
-- ---------------------------------------------------------------------------
PRINT '=== §0 Catalog: search-related tables ===';
SELECT t.name AS TableName
FROM sys.tables t
WHERE t.name LIKE N'%Search%'
   OR t.name LIKE N'%BLQuery%'
   OR t.name LIKE N'%BlQuery%'
   OR t.name LIKE N'%ReferenceView%'
ORDER BY t.name;

-- ---------------------------------------------------------------------------
-- §1  Search template shell (pdmSearchTemplate)
-- ---------------------------------------------------------------------------
PRINT '=== §1 Search template shell ===';
IF OBJECT_ID(N'dbo.pdmSearchTemplate', N'U') IS NULL
BEGIN
    RAISERROR(N'TABLE NOT FOUND: dbo.pdmSearchTemplate', 16, 1);
    RETURN;
END

SELECT
    st.SearchTemplateID,
    st.Name,
    st.Description,
    st.Type,
    st.BLQueryID,
    st.ReferenceViewID,
    st.OutputTabID,
    st.IsAutoExecute,
    st.IsDefault,
    st.IsCascadingSearch
FROM dbo.pdmSearchTemplate st
WHERE st.SearchTemplateID = @SearchTemplateId;

IF @@ROWCOUNT = 0
BEGIN
    RAISERROR(N'SearchTemplateID %d not found in pdmSearchTemplate.', 16, 1, @SearchTemplateId);
    RETURN;
END

DECLARE @ReferenceViewId INT;
DECLARE @BlQueryId INT;

SELECT
    @ReferenceViewId = st.ReferenceViewID,
    @BlQueryId = st.BLQueryID
FROM dbo.pdmSearchTemplate st
WHERE st.SearchTemplateID = @SearchTemplateId;

-- ---------------------------------------------------------------------------
-- §2  Criteria fields (pdmSearchTemplateDCU — search upper panel)
-- ---------------------------------------------------------------------------
PRINT '=== §2 Criteria (pdmSearchTemplateDCU) ===';
SELECT
    dcu.SearchTemplateDCUID,
    dcu.SubitemID,
    dcu.GridColumnID,
    dcu.DisplayText,
    dcu.ControlType,
    dcu.EntityID,
    dcu.OperationID,
    dcu.PositionRow,
    dcu.PositionColumn,
    dcu.Sort,
    dcu.IsVisible,
    dcu.IsReadOnly,
    dcu.DefaultValue,
    dcu.SysTableFiledPath
FROM dbo.pdmSearchTemplateDCU dcu
WHERE dcu.SearchTemplateID = @SearchTemplateId
  AND dcu.IsActive = 1
ORDER BY dcu.PositionRow, dcu.PositionColumn, dcu.Sort, dcu.SearchTemplateDCUID;

PRINT '=== §2b Criteria → pdmBlockSubItem enrichment ===';
SELECT
    dcu.SearchTemplateDCUID,
    dcu.DisplayText,
    dcu.SubitemID,
    bsi.BlockID,
    bsi.ControlType AS SubItemControlType,
    bsi.EntityId AS SubItemEntityId,
    tb.TabID,
    tab.TabName,
    tab.IsTemplateHeaderTab
FROM dbo.pdmSearchTemplateDCU dcu
LEFT JOIN dbo.pdmBlockSubItem bsi ON bsi.SubItemID = dcu.SubitemID
LEFT JOIN dbo.pdmTabBlock tb ON tb.BlockID = bsi.BlockID
LEFT JOIN dbo.pdmTab tab ON tab.TabID = tb.TabID
WHERE dcu.SearchTemplateID = @SearchTemplateId
  AND dcu.IsActive = 1
ORDER BY dcu.PositionRow, dcu.PositionColumn;

-- ---------------------------------------------------------------------------
-- §3  Reference view shell (pdmReferenceView)
-- ---------------------------------------------------------------------------
PRINT '=== §3 Reference view shell ===';
IF @ReferenceViewId IS NOT NULL
BEGIN
    SELECT rv.ReferenceViewID, rv.Name, rv.ViewType, rv.BLQueryID
    FROM dbo.pdmReferenceView rv
    WHERE rv.ReferenceViewID = @ReferenceViewId;
END
ELSE
    PRINT 'ReferenceViewID is NULL on search template — check pdmSearchTemplateReferenceView';

PRINT '=== §3b Extra views (pdmSearchTemplateReferenceView) ===';
IF OBJECT_ID(N'dbo.pdmSearchTemplateReferenceView', N'U') IS NOT NULL
BEGIN
    SELECT * FROM dbo.pdmSearchTemplateReferenceView
    WHERE SearchTemplateID = @SearchTemplateId;
END

-- ---------------------------------------------------------------------------
-- §4  View columns (pdmReferenceViewColumn — grid result panel)
-- ---------------------------------------------------------------------------
PRINT '=== §4 View columns (pdmReferenceViewColumn) ===';
IF @ReferenceViewId IS NOT NULL
BEGIN
    SELECT
        rvc.ReferenceViewColumnID,
        rvc.SubItemID,
        rvc.GridColumnID,
        rvc.DisplayText,
        rvc.ControlType,
        rvc.EntityID,
        rvc.Sort,
        rvc.IsVisible,
        rvc.SysTableFiledPath
    FROM dbo.pdmReferenceViewColumn rvc
    WHERE rvc.ReferenceViewID = @ReferenceViewId
    ORDER BY rvc.Sort, rvc.ReferenceViewColumnID;
END

PRINT '=== §4b View columns → grid meta / sub-item enrichment ===';
IF @ReferenceViewId IS NOT NULL
BEGIN
    SELECT
        rvc.ReferenceViewColumnID,
        rvc.DisplayText,
        rvc.SubItemID,
        rvc.GridColumnID,
        gmc.ColumnName AS GridMetaColumnName,
        gmc.ColumnTypeId,
        gmc.EntityId AS GridEntityId,
        bsi.ControlType AS SubItemControlType,
        bsi.EntityId AS SubItemEntityId,
        tb.TabID,
        tab.TabName,
        tab.IsTemplateHeaderTab
    FROM dbo.pdmReferenceViewColumn rvc
    LEFT JOIN dbo.pdmGridMetaColumn gmc ON gmc.GridColumnID = rvc.GridColumnID
    LEFT JOIN dbo.pdmBlockSubItem bsi ON bsi.SubItemID = rvc.SubItemID
    LEFT JOIN dbo.pdmTabBlock tb ON tb.BlockID = bsi.BlockID
    LEFT JOIN dbo.pdmTab tab ON tab.TabID = tb.TabID
    WHERE rvc.ReferenceViewID = @ReferenceViewId
      AND rvc.IsVisible = 1
    ORDER BY rvc.Sort;
END

-- ---------------------------------------------------------------------------
-- §5  BLQuery SQL text (reference only — do not copy verbatim to APP)
-- ---------------------------------------------------------------------------
PRINT '=== §5 BLQuery SQL preview (reference only) ===';
IF @BlQueryId IS NOT NULL AND OBJECT_ID(N'dbo.pdmBLQuery', N'U') IS NOT NULL
BEGIN
    SELECT BLQueryID, QueryName, LEFT(CAST(QueryText AS NVARCHAR(MAX)), 4000) AS QueryText_Preview
    FROM dbo.pdmBLQuery WHERE BLQueryID = @BlQueryId;
END
ELSE
    PRINT 'No BLQueryID on search template (synthesize DataSet from FieldMapping JOIN plan)';

-- ---------------------------------------------------------------------------
-- §6  Tab affinity summary (distinct SubItems used by this search)
-- ---------------------------------------------------------------------------
PRINT '=== §6 Tab affinity (SubItem → Tab) ===';
;WITH SearchSubItems AS (
    SELECT DISTINCT SubitemID AS SubItemId
    FROM dbo.pdmSearchTemplateDCU
    WHERE SearchTemplateID = @SearchTemplateId AND IsActive = 1 AND SubitemID IS NOT NULL
    UNION
    SELECT DISTINCT rvc.SubItemID
    FROM dbo.pdmReferenceViewColumn rvc
    WHERE rvc.ReferenceViewID = @ReferenceViewId AND rvc.SubItemID IS NOT NULL AND rvc.IsVisible = 1
)
SELECT
    si.SubItemId,
    tb.TabID,
    tab.TabName,
    tab.IsTemplateHeaderTab,
    tab.IsMasterReferenceHeaderTab,
    bsi.ControlType,
    bsi.EntityId
FROM SearchSubItems si
INNER JOIN dbo.pdmBlockSubItem bsi ON bsi.SubItemID = si.SubItemId
INNER JOIN dbo.pdmTabBlock tb ON tb.BlockID = bsi.BlockID
INNER JOIN dbo.pdmTab tab ON tab.TabID = tb.TabID
ORDER BY tab.TabName, si.SubItemId;

PRINT '=== Probe complete (SearchTemplateId ' + CAST(@SearchTemplateId AS NVARCHAR(20)) + ') ===';
