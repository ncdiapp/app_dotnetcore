-- V030: ask_user BuiltIn + AllowAgentFirstTurn + fields select + button_group UI.
-- Merges former V030/V031/V032/V033 into one script.
-- Idempotent: safe to re-run (IF NOT EXISTS / COL_LENGTH / upsert-style UPDATE+INSERT).
--
-- Tracking note (AppTenantMigrationRunnerBL): Version = full filename without .sql
-- (e.g. V030__AgentAskUserToolAndUI_xhu), NOT the numeric prefix alone.
-- Renaming/replacing this file with a different description segment creates a NEW
-- Version key and will run again on DBs that already applied an older V030_* name.

-- ─────────────────────────────────────────────────────────────────────────────
-- 1) AllowAgentFirstTurn column (former V031)
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

-- ─────────────────────────────────────────────────────────────────────────────
-- 2) ask_user tool — final schema (select fields + ui/layout button_group)
--    INSERT if missing; always UPDATE description/params when present.
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibraryTool
    WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent',
    N'ask_user',
    N'Ask the user a structured question and wait for their answer (Interactive only). Use for Gate-0 / missing fields / menus. mode=text|single_choice|multi_choice. For menus: mode=single_choice + optionsJson REQUIRED (never list choices only in prompt). ui=radio (select+Submit) | button_group (one-click select+submit). layout=vertical|horizontal for button_group. fieldsJson type=text|select. Optionally merge via contextKey.',
    N'{"type":"object","properties":{"prompt":{"type":"string","description":"Question shown to the user"},"mode":{"type":"string","description":"text | single_choice | multi_choice"},"fieldsJson":{"type":"string","description":"JSON array of {name,label,required?,type?,options?} — type=text|select; select options LookupItemDto [{id,display}]"},"optionsJson":{"type":"string","description":"JSON array of LookupItemDto {id,display} for choice modes"},"contextKey":{"type":"string","description":"Optional shared-context key to merge answers into"},"ui":{"type":"string","description":"radio (default) | button_group — button_group one-click select+submit; only for single_choice"},"layout":{"type":"string","description":"vertical (default) | horizontal — button_group only"}},"required":["prompt"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentAskUserPlugin","MethodName":"AskUser"}',
    1,
    40
);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Ask the user a structured question and wait for their answer (Interactive only). Use for Gate-0 / missing fields / menus. mode=text|single_choice|multi_choice. For menus: mode=single_choice + optionsJson REQUIRED (never list choices only in prompt). ui=radio (select+Submit) | button_group (one-click select+submit). layout=vertical|horizontal for button_group. fieldsJson type=text|select. Optionally merge via contextKey.',
    ParameterSchemaJson = N'{"type":"object","properties":{"prompt":{"type":"string","description":"Question shown to the user"},"mode":{"type":"string","description":"text | single_choice | multi_choice"},"fieldsJson":{"type":"string","description":"JSON array of {name,label,required?,type?,options?} — type=text|select; select options LookupItemDto [{id,display}]"},"optionsJson":{"type":"string","description":"JSON array of LookupItemDto {id,display} for choice modes"},"contextKey":{"type":"string","description":"Optional shared-context key to merge answers into"},"ui":{"type":"string","description":"radio (default) | button_group — button_group one-click select+submit; only for single_choice"},"layout":{"type":"string","description":"vertical (default) | horizontal — button_group only"}},"required":["prompt"]}',
    ToolType = N'BuiltIn',
    ToolConfig = N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentAskUserPlugin","MethodName":"AskUser"}',
    IsActive = 1,
    SortOrder = 40
WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user';
GO
