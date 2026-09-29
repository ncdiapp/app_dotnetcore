-- Seed Orchestrator → Child-Agent mappings for PLM Migration Multi-Agent.
-- TENANT seed — NOT a Flyway migration. Requires V036 AppAgentChildMapping + prior agent seeds.
-- Idempotent: INSERT only when the (Parent, Child) pair is missing. Updates SortOrder on re-run.
SET NOCOUNT ON;
GO

DECLARE @Parent NVARCHAR(100) = N'plm-integration-orchestrator';

IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NULL
BEGIN
    RAISERROR(N'AppAgentChildMapping missing — apply migration V036 first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = @Parent)
BEGIN
    RAISERROR(N'Orchestrator plm-integration-orchestrator not found — run 03_Seed first.', 16, 1);
    RETURN;
END

;WITH Wanted(ChildSkillKey, SortOrder) AS (
    SELECT * FROM (VALUES
        (N'plm-integration-import-dw', 1),
        (N'plm-integration-entity', 2),
        (N'plm-integration-folder', 3),
        (N'plm-integration-image', 4),
        (N'plm-integration-color', 5),
        (N'plm-integration-pom', 6),
        (N'plm-integration-search', 7),
        (N'plm-integration-massupdate', 8)
    ) AS v(ChildSkillKey, SortOrder)
)
MERGE dbo.AppAgentChildMapping AS t
USING (
    SELECT w.ChildSkillKey, w.SortOrder
    FROM Wanted w
    INNER JOIN dbo.AppAgentSkillSet s ON s.SkillKey = w.ChildSkillKey
) AS s
ON t.ParentSkillKey = @Parent AND t.ChildSkillKey = s.ChildSkillKey
WHEN MATCHED THEN
    UPDATE SET SortOrder = s.SortOrder, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (ParentSkillKey, ChildSkillKey, SortOrder, CreatedAt, UpdatedAt)
    VALUES (@Parent, s.ChildSkillKey, s.SortOrder, SYSUTCDATETIME(), SYSUTCDATETIME());

PRINT N'AppAgentChildMapping seed applied for plm-integration-orchestrator.';
GO
