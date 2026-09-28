-- CHILD worker: Image / Sketch import (Deterministic). TENANT seed -- NOT a Flyway migration.
-- SkillKey: plm-integration-image
-- RULE: new-tenant INSERT only. No UPDATE. Child IsActive=0 (hidden from left menu).
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-image')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'plm-integration-image',
    N'PLM Integration Image',
    N'Deterministic child: tblSketch to AppFile import; HITL owned by ROOT',
    N'# CHILD WORKER - SkillKey: plm-integration-image
# Parent ROOT: plm-integration-orchestrator

## Non-negotiable
1. ExecutionMode=Deterministic. Never call ask_user. Never STOP for HITL.
2. Missing sessionId: FinalResponse JSON ok=false + errors. Do not invent ids. Do not use connection strings.
3. On start read plm.integration.job and plm.integration.image.inputs. sessionId from inputs or job.
4. Honor PHASE=PREVIEW or PHASE=EXECUTE from the call message.
5. Final reply = compact JSON only: ok, skillKey, phase, sessionId, summary, counts, jobId, errors, nextHint.

## Tools (sessionId required)
- PREVIEW: preview_plm_sketch_import
- EXECUTE: execute_plm_sketch_import then poll get_plm_import_job until Completed/Failed/Cancelled
- Write plm.integration.image.outputs

## Domain
INSERT writes AppFile with FolderID NULL by design. Do not call folder placement from this child.
ROOT HARD gate: after this EXECUTE ok, ROOT must call plm-integration-folder PHASE=PLACEMENT in the same turn before marking wizard.image=done. Do not tell ROOT to advance to Color yet.

## nextHint
PREVIEW: Ask ROOT to confirm Proceed. EXECUTE ok: nextHint must say ROOT must run folder PHASE=PLACEMENT now (do not mark image=done until placement ok)
',
    3, 0, 22, 1,
    40000, 30000, 4000, 8,
    80, N'Deterministic', 1
);
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-image')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-image' AND LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-image', N'platform-multi-agent');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-image')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-image' AND LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-image', N'integration-plm-import');
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'
## HARD: after EXECUTE ok ROOT must PLACEMENT
EXECUTE ok does not mean image is done. FinalResponse nextHint must tell ROOT to call plm-integration-folder PHASE=PLACEMENT immediately. Do not suggest advancing to Color. Do not call folder placement from this child.
'
WHERE SkillKey = N'plm-integration-image'
  AND SystemPrompt NOT LIKE N'%HARD: after EXECUTE ok ROOT must PLACEMENT%';
GO
