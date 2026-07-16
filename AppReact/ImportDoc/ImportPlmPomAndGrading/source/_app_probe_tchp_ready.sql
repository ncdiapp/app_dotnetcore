-- =============================================================================
-- Probe: Tenant APP — Tchp target tables ready?
-- Against: Tenant APP database
-- Folder: ImportPlmPomAndGrading/source
-- =============================================================================
SET NOCOUNT ON;

DECLARE @Need TABLE (TableName sysname PRIMARY KEY);
INSERT INTO @Need (TableName) VALUES
    (N'TchpSizeRun'),
    (N'TchpSizeRunSize'),
    (N'TchpBodyPart'),
    (N'TchpPomTemplate'),
    (N'TchpPomTemplatePart'),
    (N'TchpGradeRuleSet'),
    (N'TchpGradeRule');

SELECT
    n.TableName,
    CASE WHEN OBJECT_ID(N'dbo.' + n.TableName, N'U') IS NULL THEN 0 ELSE 1 END AS ExistsInDb,
    CASE
        WHEN OBJECT_ID(N'dbo.' + n.TableName, N'U') IS NULL THEN NULL
        ELSE (SELECT SUM(p.rows)
              FROM sys.partitions p
              WHERE p.object_id = OBJECT_ID(N'dbo.' + n.TableName)
                AND p.index_id IN (0, 1))
    END AS ApproxRowCount
FROM @Need n
ORDER BY n.TableName;

-- PK overlap risk sample (run only if tables exist)
IF OBJECT_ID(N'dbo.TchpBodyPart', N'U') IS NOT NULL
    SELECT N'TchpBodyPart' AS T, COUNT(*) AS Cnt FROM dbo.TchpBodyPart;

IF OBJECT_ID(N'dbo.TchpPomTemplate', N'U') IS NOT NULL
    SELECT N'TchpPomTemplate' AS T, COUNT(*) AS Cnt FROM dbo.TchpPomTemplate;

IF OBJECT_ID(N'dbo.TchpSizeRun', N'U') IS NOT NULL
    SELECT N'TchpSizeRun' AS T, COUNT(*) AS Cnt FROM dbo.TchpSizeRun;
