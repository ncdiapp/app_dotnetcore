-- =============================================================================
-- PLM Mass Update View probe
-- Folder: AppReact/ImportDoc/ImportPLMSearchView/source/
-- Used by: PROMPT_MASSUPDATE_VIEW.md
--
-- Set @SearchTemplateId and @MassUpdateViewId, then run against PLM database.
-- =============================================================================

DECLARE @SearchTemplateId INT = 28902;   -- REQUIRED
DECLARE @MassUpdateViewId INT = 9;       -- REQUIRED

SET NOCOUNT ON;

-- ---------------------------------------------------------------------------
-- §0  Catalog (Mass Update related tables)
-- ---------------------------------------------------------------------------
PRINT '=== §0 Mass Update related tables ===';
SELECT t.name AS TableName
FROM sys.tables t
WHERE t.name LIKE N'%MassUpdate%'
   OR t.name LIKE N'%SearchTemplateReferenceView%'
ORDER BY t.name;

-- ---------------------------------------------------------------------------
-- §1  Search template shell (incl. default MU)
-- ---------------------------------------------------------------------------
PRINT '=== §1 Search template ===';
SELECT
    st.SearchTemplateID,
    st.Name,
    st.ReferenceViewID,
    st.BLQueryID,
    st.DefaultMassUpdateViewId
FROM dbo.pdmSearchTemplate st
WHERE st.SearchTemplateID = @SearchTemplateId;

PRINT '=== §1b Junction rows (display views + mass update links) ===';
IF OBJECT_ID(N'dbo.pdmSearchTemplateReferenceView', N'U') IS NOT NULL
BEGIN
    SELECT
        strv.SearchTemplateViewID,
        strv.SearchTemplateID,
        strv.ReferenceViewID,
        strv.ReferenceIDViewFilterColumnID,
        strv.MassUpdateViewID
    FROM dbo.pdmSearchTemplateReferenceView strv
    WHERE strv.SearchTemplateID = @SearchTemplateId
    ORDER BY strv.SearchTemplateViewID;
END

-- ---------------------------------------------------------------------------
-- §2  Mass Update View shell
-- ---------------------------------------------------------------------------
PRINT '=== §2 pdmMassUpdateView shell ===';
IF OBJECT_ID(N'dbo.pdmMassUpdateView', N'U') IS NOT NULL
BEGIN
    SELECT *
    FROM dbo.pdmMassUpdateView mu
    WHERE mu.MassUpdateViewID = @MassUpdateViewId;
END
ELSE
    PRINT 'pdmMassUpdateView not found';

PRINT '=== §2c Link check: this MU on this SearchTemplate? ===';
DECLARE @IsDefault BIT = 0;
DECLARE @IsLinked BIT = 0;

IF EXISTS (
    SELECT 1 FROM dbo.pdmSearchTemplate
    WHERE SearchTemplateID = @SearchTemplateId
      AND DefaultMassUpdateViewId = @MassUpdateViewId
)
    SET @IsDefault = 1;

IF OBJECT_ID(N'dbo.pdmSearchTemplateReferenceView', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM dbo.pdmSearchTemplateReferenceView
    WHERE SearchTemplateID = @SearchTemplateId
      AND MassUpdateViewID = @MassUpdateViewId
)
    SET @IsLinked = 1;

SELECT
    @MassUpdateViewId AS MassUpdateViewId,
    @IsDefault AS IsDefaultMassUpdateView,
    @IsLinked AS IsLinkedViaJunction,
    CASE WHEN @IsDefault = 1 OR @IsLinked = 1 THEN N'OK' ELSE N'WARN: not linked to this SearchTemplate' END AS LinkStatus;

-- ---------------------------------------------------------------------------
-- §3  Mass Update fields
-- ---------------------------------------------------------------------------
PRINT '=== §3 pdmMassUpdateViewField ===';
IF OBJECT_ID(N'dbo.pdmMassUpdateViewField', N'U') IS NOT NULL
BEGIN
    SELECT *
    FROM dbo.pdmMassUpdateViewField f
    WHERE f.MassUpdateViewID = @MassUpdateViewId
    ORDER BY f.Sort, f.MassUpdateViewFieldID;
END
ELSE
    PRINT 'pdmMassUpdateViewField not found';

-- ---------------------------------------------------------------------------
-- §3b Field enrichment — SubItem
-- ---------------------------------------------------------------------------
PRINT '=== §3b Fields → SubItem enrichment ===';
IF OBJECT_ID(N'dbo.pdmMassUpdateViewField', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.pdmBlockSubItem', N'U') IS NOT NULL
BEGIN
    SELECT
        f.MassUpdateViewFieldID,
        f.Sort,
        f.IsReadonly,
        f.SubItemID,
        f.GridColumnID,
        si.Name AS SubItemName,
        si.ControlType AS SubItemControlType,
        si.EntityID AS SubItemEntityId,
        si.FriendName,
        si.DisplayName
    FROM dbo.pdmMassUpdateViewField f
    LEFT JOIN dbo.pdmBlockSubItem si ON si.BlockSubItemID = f.SubItemID
    WHERE f.MassUpdateViewID = @MassUpdateViewId
      AND f.SubItemID IS NOT NULL
    ORDER BY f.Sort, f.MassUpdateViewFieldID;
END

-- ---------------------------------------------------------------------------
-- §3c Field enrichment — GridColumn / Meta
-- ---------------------------------------------------------------------------
PRINT '=== §3c Fields → GridColumn enrichment ===';
IF OBJECT_ID(N'dbo.pdmMassUpdateViewField', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.pdmGridMetaColumn', N'U') IS NOT NULL
BEGIN
    SELECT
        f.MassUpdateViewFieldID,
        f.Sort,
        f.IsReadonly,
        f.SubItemID,
        f.GridColumnID,
        gmc.Name AS GridMetaColumnName,
        gmc.ColumnTypeId,
        gmc.EntityID AS GridEntityId
    FROM dbo.pdmMassUpdateViewField f
    LEFT JOIN dbo.pdmGridMetaColumn gmc ON gmc.GridMetaColumnID = f.GridColumnID
    WHERE f.MassUpdateViewID = @MassUpdateViewId
      AND f.GridColumnID IS NOT NULL
    ORDER BY f.Sort, f.MassUpdateViewFieldID;
END

-- ---------------------------------------------------------------------------
-- §4  Wanted ids for APP FieldMapping probe
-- ---------------------------------------------------------------------------
PRINT '=== §4 Wanted SubItem / MetaColumn ids (paste into APP probe) ===';
IF OBJECT_ID(N'dbo.pdmMassUpdateViewField', N'U') IS NOT NULL
BEGIN
    SELECT DISTINCT f.SubItemID AS WantedSubItemId
    FROM dbo.pdmMassUpdateViewField f
    WHERE f.MassUpdateViewID = @MassUpdateViewId
      AND f.SubItemID IS NOT NULL;

    SELECT DISTINCT f.GridColumnID AS WantedMetaColumnId
    FROM dbo.pdmMassUpdateViewField f
    WHERE f.MassUpdateViewID = @MassUpdateViewId
      AND f.GridColumnID IS NOT NULL;
END

-- ---------------------------------------------------------------------------
-- §5  Optional: security / calc (inventory only — out of import scope)
-- ---------------------------------------------------------------------------
PRINT '=== §5 Optional related (inventory) ===';
IF OBJECT_ID(N'dbo.pdmMassUpdateViewMember', N'U') IS NOT NULL
BEGIN
    SELECT COUNT(*) AS MemberCount
    FROM dbo.pdmMassUpdateViewMember
    WHERE MassUpdateViewID = @MassUpdateViewId;
END

IF OBJECT_ID(N'dbo.pdmMassUpdateViewCalFlow', N'U') IS NOT NULL
BEGIN
    SELECT COUNT(*) AS CalFlowCount
    FROM dbo.pdmMassUpdateViewCalFlow
    WHERE MassUpdateViewID = @MassUpdateViewId;
END

PRINT '=== Done Mass Update probe ===';
