-- V011__SkillSetMaxIterations.sql
-- Adds MaxIterations to AppAgentSkillSet so each skill set can configure
-- the maximum number of tool-call rounds before the agent stops.
-- Default 40 matches GenericAgentEngine's previous hard-coded constant.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.AppAgentSkillSet') AND name = 'MaxIterations'
)
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet
        ADD MaxIterations INT NOT NULL DEFAULT 40;
END
GO
