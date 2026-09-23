-- CHILD worker: Color import (Deterministic). TENANT seed -- NOT a Flyway migration.
-- SkillKey: plm-integration-color
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-color')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'plm-integration-color',
    N'PLM Integration Color',
    N'Deterministic child: PLM RGB color import; HITL owned by ROOT',
    N'# PLACEHOLDER',
    3, 1, 23, 1,
    40000, 30000, 4000, 8,
    40, N'Deterministic', 1
);
GO

UPDATE dbo.AppAgentSkillSet
SET DisplayName = N'PLM Integration Color',
    Description = N'Deterministic child: PLM RGB color import; HITL owned by ROOT',
    CapabilityFlags = 3,
    IsActive = 1,
    MaxIterations = 40,
    ExecutionMode = N'Deterministic',
    AgentUi = 1,
    SystemPrompt = N'# CHILD WORKER - SkillKey: plm-integration-color
# Parent ROOT: plm-integration-orchestrator

## Non-negotiable
1. ExecutionMode=Deterministic. Never call ask_user. Never STOP for HITL.
2. Missing sessionId: FinalResponse JSON ok=false + errors. Do not invent ids. Do not use connection strings.
3. On start read plm.integration.job and plm.integration.color.inputs. sessionId from inputs or job. Optional saasApplicationId from inputs or job.
4. Honor PHASE=PREVIEW or PHASE=EXECUTE from the call message.
5. Final reply = compact JSON only: ok, skillKey, phase, sessionId, summary, counts, jobId, errors, nextHint.

## Tools (sessionId required)
- PREVIEW: preview_plm_color_import
- EXECUTE: execute_plm_color_import (sync). Pass saasApplicationId when present. If a jobId is returned, poll get_plm_import_job
- Write plm.integration.color.outputs

## nextHint
PREVIEW: Ask ROOT to confirm Proceed. EXECUTE ok: Mark wizard.color=done
'
WHERE SkillKey = N'plm-integration-color';
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-color')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-color' AND LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-color', N'platform-multi-agent');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-color')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-color' AND LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-color', N'integration-plm-import');
GO
