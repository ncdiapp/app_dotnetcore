-- Read-only: analyze SubItem overlap across FABRIC DW Tab tables
SET NOCOUNT ON;

DECLARE @Tabs TABLE (DwTable SYSNAME, TabId INT);
INSERT INTO @Tabs VALUES
 (N'PLM_DW_Tab_Fabric_Header_4258', 4258),
 (N'PLM_DW_Tab_Fabric_Info_4212', 4212),
 (N'PLM_DW_Tab_Attributes_4213', 4213),
 (N'PLM_DW_Tab_Fabric_Cost_4270', 4270),
 (N'PLM_DW_Tab_Fabric_Policy_4274', 4274),
 (N'PLM_DW_Tab_Testing____Compliance_4219', 4219);

;WITH cols AS (
    SELECT t.DwTable, t.TabId, c.COLUMN_NAME, c.DATA_TYPE, c.ORDINAL_POSITION
    FROM @Tabs t
    JOIN INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.DwTable
    WHERE c.COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')
),
parsed AS (
    SELECT *,
        CASE
            WHEN COLUMN_NAME LIKE N'%[_]FK[_]%' THEN
                TRY_CAST(
                    RIGHT(LEFT(COLUMN_NAME, CHARINDEX(N'_FK_', COLUMN_NAME + N'_FK_') - 1),
                         CHARINDEX(N'_', REVERSE(LEFT(COLUMN_NAME, CHARINDEX(N'_FK_', COLUMN_NAME + N'_FK_') - 1)) + N'_') - 1)
                    AS INT)
            ELSE
                TRY_CAST(
                    RIGHT(COLUMN_NAME,
                         CHARINDEX(N'_', REVERSE(COLUMN_NAME) + N'_') - 1)
                    AS INT)
        END AS SubItemId,
        CASE WHEN COLUMN_NAME LIKE N'%[_]FK[_]%' THEN 1 ELSE 0 END AS IsFkColumn
    FROM cols
)
SELECT DwTable, TabId, COUNT(*) AS DataColumnCount,
       COUNT(DISTINCT SubItemId) AS DistinctSubItemIds
FROM parsed
WHERE SubItemId IS NOT NULL
GROUP BY DwTable, TabId
ORDER BY TabId;

-- SubItems appearing in multiple tabs
;WITH cols AS (
    SELECT t.DwTable, t.TabId, c.COLUMN_NAME
    FROM @Tabs t
    JOIN INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.DwTable
    WHERE c.COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')
),
parsed AS (
    SELECT DwTable, TabId, COLUMN_NAME,
        CASE
            WHEN COLUMN_NAME LIKE N'%[_]FK[_]%' THEN
                TRY_CAST(
                    RIGHT(LEFT(COLUMN_NAME, CHARINDEX(N'_FK_', COLUMN_NAME + N'_FK_') - 1),
                         CHARINDEX(N'_', REVERSE(LEFT(COLUMN_NAME, CHARINDEX(N'_FK_', COLUMN_NAME + N'_FK_') - 1)) + N'_') - 1)
                    AS INT)
            ELSE
                TRY_CAST(RIGHT(COLUMN_NAME, CHARINDEX(N'_', REVERSE(COLUMN_NAME) + N'_') - 1) AS INT)
        END AS SubItemId
    FROM cols
),
bySub AS (
    SELECT SubItemId,
           STRING_AGG(CAST(TabId AS NVARCHAR(10)), N',') WITHIN GROUP (ORDER BY TabId) AS TabIds,
           COUNT(DISTINCT TabId) AS TabCount
    FROM parsed
    WHERE SubItemId IS NOT NULL
    GROUP BY SubItemId
)
SELECT TOP 30 SubItemId, TabCount, TabIds
FROM bySub
WHERE TabCount > 1
ORDER BY TabCount DESC, SubItemId;

-- Header-only vs Info-only SubItem counts (4258 vs 4212)
;WITH cols AS (
    SELECT t.TabId, c.COLUMN_NAME
    FROM @Tabs t
    JOIN INFORMATION_SCHEMA.COLUMNS c ON c.TABLE_NAME = t.DwTable
    WHERE t.TabId IN (4258, 4212)
      AND c.COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')
),
parsed AS (
    SELECT TabId,
        CASE
            WHEN COLUMN_NAME LIKE N'%[_]FK[_]%' THEN
                TRY_CAST(
                    RIGHT(LEFT(COLUMN_NAME, CHARINDEX(N'_FK_', COLUMN_NAME + N'_FK_') - 1),
                         CHARINDEX(N'_', REVERSE(LEFT(COLUMN_NAME, CHARINDEX(N'_FK_', COLUMN_NAME + N'_FK_') - 1)) + N'_') - 1)
                    AS INT)
            ELSE
                TRY_CAST(RIGHT(COLUMN_NAME, CHARINDEX(N'_', REVERSE(COLUMN_NAME) + N'_') - 1) AS INT)
        END AS SubItemId
    FROM cols
),
h AS (SELECT DISTINCT SubItemId FROM parsed WHERE TabId = 4258 AND SubItemId IS NOT NULL),
i AS (SELECT DISTINCT SubItemId FROM parsed WHERE TabId = 4212 AND SubItemId IS NOT NULL)
SELECT
    (SELECT COUNT(*) FROM h) AS HeaderSubItems,
    (SELECT COUNT(*) FROM i) AS InfoSubItems,
    (SELECT COUNT(*) FROM h INNER JOIN i x ON x.SubItemId = h.SubItemId) AS SharedSubItems,
    (SELECT COUNT(*) FROM h WHERE NOT EXISTS (SELECT 1 FROM i x WHERE x.SubItemId = h.SubItemId)) AS HeaderOnly,
    (SELECT COUNT(*) FROM i WHERE NOT EXISTS (SELECT 1 FROM h x WHERE x.SubItemId = i.SubItemId)) AS InfoOnly;
