-- CHILD worker: POM import (Deterministic). TENANT seed -- NOT a Flyway migration.
-- SkillKey: plm-integration-pom
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-pom')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'plm-integration-pom',
    N'PLM Integration POM',
    N'Deterministic child: POM / body-part import; HITL owned by ROOT',
    N'# PLACEHOLDER',
    3, 1, 24, 1,
    40000, 30000, 4000, 8,
    40, N'Deterministic', 1
);
GO

UPDATE dbo.AppAgentSkillSet
SET DisplayName = N'PLM Integration POM',
    Description = N'Deterministic child: POM / body-part import; HITL owned by ROOT',
    CapabilityFlags = 3,
    IsActive = 1,
    MaxIterations = 40,
    ExecutionMode = N'Deterministic',
    AgentUi = 1,
    SystemPrompt = N'# CHILD WORKER - SkillKey: plm-integration-pom
# Parent ROOT: plm-integration-orchestrator

## Non-negotiable
1. ExecutionMode=Deterministic. Never call ask_user. Never STOP for HITL.
2. Missing sessionId: FinalResponse JSON ok=false + errors. Do not invent ids. Do not use connection strings.
3. On start read plm.integration.job and plm.integration.pom.inputs. sessionId from inputs or job. Optional saasApplicationId from inputs or job.
4. Honor PHASE=PREVIEW or PHASE=EXECUTE from the call message.
5. Final reply = compact JSON only: ok, skillKey, phase, sessionId, summary, counts, jobId, errors, nextHint.

## Tools (sessionId required)
- PREVIEW: preview_plm_pom_import
- EXECUTE: execute_plm_pom_import (sync). Pass saasApplicationId when present. Default importJunctionTables=true and importFoldersIfMissing=true unless inputs override. If a jobId is returned, poll get_plm_import_job
- Write plm.integration.pom.outputs

## nextHint
PREVIEW: Ask ROOT to confirm Proceed. EXECUTE ok: Mark wizard.pom=done
'
WHERE SkillKey = N'plm-integration-pom';
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-pom')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-pom' AND LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-pom', N'platform-multi-agent');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-pom')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-pom' AND LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-pom', N'integration-plm-import');
GO
