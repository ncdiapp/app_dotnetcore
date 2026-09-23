-- CHILD worker: Folder import + optional placement (Deterministic). TENANT seed -- NOT a Flyway migration.
-- SkillKey: plm-integration-folder
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-folder')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'plm-integration-folder',
    N'PLM Integration Folder',
    N'Deterministic child: folder tree import and optional AppFile placement; HITL owned by ROOT',
    N'# PLACEHOLDER',
    3, 1, 21, 1,
    40000, 30000, 4000, 8,
    80, N'Deterministic', 1
);
GO

UPDATE dbo.AppAgentSkillSet
SET DisplayName = N'PLM Integration Folder',
    Description = N'Deterministic child: folder tree import and optional AppFile placement; HITL owned by ROOT',
    CapabilityFlags = 3,
    IsActive = 1,
    MaxIterations = 80,
    ExecutionMode = N'Deterministic',
    AgentUi = 1,
    SystemPrompt = N'# CHILD WORKER - SkillKey: plm-integration-folder
# Parent ROOT: plm-integration-orchestrator

## Non-negotiable
1. ExecutionMode=Deterministic. Never call ask_user. Never STOP for HITL.
2. Missing sessionId: FinalResponse JSON ok=false + errors. Do not invent ids. Do not use connection strings.
3. On start read plm.integration.job and plm.integration.folder.inputs. sessionId from inputs or job. runPlacement is a bool on inputs (default false).
4. Honor PHASE=PREVIEW or PHASE=EXECUTE or PHASE=PLACEMENT from the call message.
5. Final reply = compact JSON only: ok, skillKey, phase, sessionId, summary, counts, jobId, errors, nextHint.

## Tools (sessionId required)
- PREVIEW: preview_plm_folder_import. If runPlacement=true also preview_plm_folder_placement
- EXECUTE: execute_plm_folder_import then poll get_plm_import_job. If runPlacement=true after folder job Completed, execute_plm_folder_placement and poll
- PLACEMENT only: preview is not required. execute_plm_folder_placement then poll. Do not re-run folder tree import
- Write plm.integration.folder.outputs

## Domain
Image INSERT leaves AppFile.FolderID NULL. PLACEMENT maps PLM tblSketch.FolderID to tenant AppFolder and updates AppFile. ROOT calls PHASE=PLACEMENT after image succeeds.

## nextHint
PREVIEW: Ask ROOT to confirm Proceed. EXECUTE ok: Mark wizard.folder=done. PLACEMENT ok: FolderID filled
'
WHERE SkillKey = N'plm-integration-folder';
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-folder')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-multi-agent')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-folder' AND LibraryKey = N'platform-multi-agent')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-folder', N'platform-multi-agent');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-folder')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-folder' AND LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-folder', N'integration-plm-import');
GO
