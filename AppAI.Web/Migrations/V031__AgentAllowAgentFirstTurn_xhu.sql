-- V031: AllowAgentFirstTurn on AppAgentSkillSet.
-- When 1 (and ExecutionMode=Interactive), empty chat fires hidden [session_start]
-- so the agent may speak first / ask_user per SystemPrompt.
-- Default 0: agent waits for the user (no auto greeting).
-- UI label: "Agent speaks first"

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
