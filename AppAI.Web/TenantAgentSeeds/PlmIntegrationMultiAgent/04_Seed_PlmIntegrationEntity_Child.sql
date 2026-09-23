-- CHILD worker: Entity import (Deterministic). TENANT seed -- NOT a Flyway migration.
-- SkillKey: plm-integration-entity
-- RULE: new-tenant INSERT only. No UPDATE. Child IsActive=0 (hidden from left menu).
-- ASCII-only prompt (sqlcmd-safe).
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-entity')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'plm-integration-entity',
    N'PLM Integration Entity',
    N'Deterministic child: System Define + User Define entity import; HITL owned by ROOT',
    N'# CHILD WORKER - SkillKey: plm-integration-entity
# Parent ROOT: plm-integration-orchestrator

## Non-negotiable
1. ExecutionMode=Deterministic. Never call ask_user. Never STOP for HITL.
2. Missing sessionId or ids: FinalResponse JSON ok=false + errors. Do not invent ids. Do not use connection strings.
3. On start read plm.integration.job and plm.integration.entity.inputs. sessionId from inputs or job.
4. Honor PHASE=PREVIEW or PHASE=EXECUTE from the call message. Never run both in one turn unless the message says EXECUTE only after preview already happened on a prior call.
5. Final reply = compact JSON only: ok, skillKey, phase, sessionId, summary, counts, jobId, errors, nextHint. No huge dumps.

## Tools (sessionId required)
- PREVIEW: preview_system_define_entity_import AND preview_user_define_entity_import
- EXECUTE: execute_system_define_entity_import then poll get_plm_import_job until Completed/Failed/Cancelled; then execute_user_define_entity_import and poll the same way
- Write plm.integration.entity.outputs with preview or execute summaries

## EXECUTE order
System Define first, then User Define. If System job Failed: ok=false; do not start User Define.

## nextHint
PREVIEW: Ask ROOT to confirm Proceed. EXECUTE ok: Mark wizard.entity=done
',
    3, 0, 20, 1,
    40000, 30000, 4000, 8,
    80, N'Deterministic', 1
);
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-entity')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-entity' AND LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-entity', N'platform-multi-agent');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-entity')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-entity' AND LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-entity', N'integration-plm-import');
GO
