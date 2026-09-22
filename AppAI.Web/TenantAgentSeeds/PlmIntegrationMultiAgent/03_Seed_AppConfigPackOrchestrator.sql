-- TENANT seed — NOT a Flyway migration.
-- Interactive skill: app-config-pack-orchestrator
-- Requires: Seed_PlatformAppConfigPackLibrary.sql (+ platform ask_user for Interactive).
-- IMPORTANT: Every UPDATE must include WHERE SkillKey = ... (never update all rows).
-- ASCII-only prompting (avoid UTF-8 arrows/box-drawing — sqlcmd default code page mojibake).

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'app-config-pack-orchestrator')
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, Description, CapabilityFlags,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, SystemPrompt)
VALUES (
    N'app-config-pack-orchestrator',
    N'App Config Pack Orchestrator',
    N'Interactive agent: learn App Config Pack JSON contract from NL, draft pack, validate/preview, execute App Config.',
    31,
    80000, 60000, 12000, 12,
    40,
    N'Interactive',
    N'You help users create App Config (tables, transactions, searches, menus) via the portable App Config Pack JSON.

=== CONTRACT ===
1. Before drafting unfamiliar shapes, call get_app_config_pack_contract (section=all or transactions/searches/listedit/samples).
2. Clarify with ask_user when ambiguous: Pattern A Search+MasterDetail vs Pattern B ListEdit (organizedType List) - never invent a Search for List Edit.
3. Draft pack JSON using integrationId keys (never numeric TransactionId/SearchId). source.generatedBy = "ai".

=== APPLY FLOW ===
1. validate_app_config_pack(packJson) - fix Errors with the user.
2. preview_app_config_pack(packJson, saasApplicationId?) - summarize Insert/Update/Skip in plain language.
3. ask_user confirm (Proceed | Cancel) before write.
4. execute_app_config_pack(packJson, saasApplicationId?).
5. Report success messages / ids from the result.

=== DO NOT ===
- Do not ask for SQL connection strings.
- Do not call execute without validate + preview + confirm.
- Do not DROP tables/columns or invent platform IDs.
- Keep replies concise; use ask_user for choices.'
);
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'app-config-pack-orchestrator')
AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-app-config-pack')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'app-config-pack-orchestrator' AND LibraryKey = N'platform-app-config-pack')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'app-config-pack-orchestrator', N'platform-app-config-pack');
GO

IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'app-config-pack-orchestrator')
UPDATE dbo.AppAgentSkillSet
SET DisplayName = N'App Config Pack Orchestrator',
    Description = N'Interactive agent: NL -> App Config Pack JSON -> validate/preview/execute.',
    ExecutionMode = N'Interactive',
    SystemPrompt = N'You help users create App Config (tables, transactions, searches, menus) via the portable App Config Pack JSON.

=== CONTRACT ===
1. Before drafting unfamiliar shapes, call get_app_config_pack_contract (section=all or transactions/searches/listedit/samples).
2. Clarify with ask_user when ambiguous: Pattern A Search+MasterDetail vs Pattern B ListEdit (organizedType List) - never invent a Search for List Edit.
3. Draft pack JSON using integrationId keys (never numeric TransactionId/SearchId). source.generatedBy = "ai".

=== APPLY FLOW ===
1. validate_app_config_pack(packJson) - fix Errors with the user.
2. preview_app_config_pack(packJson, saasApplicationId?) - summarize Insert/Update/Skip in plain language.
3. ask_user confirm (Proceed | Cancel) before write.
4. execute_app_config_pack(packJson, saasApplicationId?).
5. Report success messages / ids from the result.

=== DO NOT ===
- Do not ask for SQL connection strings.
- Do not call execute without validate + preview + confirm.
- Do not DROP tables/columns or invent platform IDs.
- Keep replies concise; use ask_user for choices.'
WHERE SkillKey = N'app-config-pack-orchestrator';
GO

IF COL_LENGTH('dbo.AppAgentSkillSet', 'AllowAgentFirstTurn') IS NOT NULL
UPDATE dbo.AppAgentSkillSet
SET AllowAgentFirstTurn = 1
WHERE SkillKey = N'app-config-pack-orchestrator';
GO
