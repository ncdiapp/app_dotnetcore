-- V016: Add ExecutionMode to AppAgentSkillSet.
-- Controls whether the agent pauses at PlanGate/SchemaGate for user approval (Interactive)
-- or auto-approves both gates and runs to completion without blocking (Deterministic).
-- Default: Interactive (current behavior preserved for all existing skill sets).

ALTER TABLE dbo.AppAgentSkillSet
    ADD ExecutionMode VARCHAR(20) NOT NULL DEFAULT 'Interactive';
GO
