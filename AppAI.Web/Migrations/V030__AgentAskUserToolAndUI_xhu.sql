-- V030: AllowAgentFirstTurn for Interactive orchestrators.
-- ask_user is NOT a library tool — GenericAgentEngine auto-injects it for Interactive agents.
-- Idempotent: safe to re-run (COL_LENGTH / UPDATE).
--
-- Tracking note (AppTenantMigrationRunnerBL): Version = full filename without .sql
-- (e.g. V030__AgentAskUserToolAndUI_xhu), NOT the numeric prefix alone.
-- Renaming/replacing this file with a different description segment creates a NEW
-- Version key and will run again on DBs that already applied an older V030_* name.

-- ─────────────────────────────────────────────────────────────────────────────
-- AllowAgentFirstTurn column (former V031)
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.AppAgentSkillSet', 'AllowAgentFirstTurn') IS NULL
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet
        ADD AllowAgentFirstTurn BIT NOT NULL
            CONSTRAINT DF_AppAgentSkillSet_AllowAgentFirstTurn DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.AppAgentSkillSet', 'AllowAgentFirstTurn') IS NOT NULL
BEGIN
    UPDATE dbo.AppAgentSkillSet
    SET AllowAgentFirstTurn = 1
    WHERE SkillKey IN (
        N'plm-integration-orchestrator',
        N'app-config-pack-orchestrator'
    );
END
GO
