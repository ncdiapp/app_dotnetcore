-- V082: Agent Instruction Designer (agent-instruction-designer) + platform-agent-authoring tools.
-- Includes draft inspect/apply, catalog, and debug target Test Run (MessagesJson/toolSteps).
-- IF NOT EXISTS only for library/tools/SkillSet. Re-run does not overwrite prompt or subscriptions.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-agent-authoring')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-agent-authoring',
    N'platform',
    N'BuiltIn: Agent Instruction Authoring',
    N'Inspect/apply the open Agent Management draft, search the tool catalog, and debug target Test Run toolSteps. Host tools stage draft only; user Save persists.',
    N'BuiltIn',
    1
);
GO

DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentAuthoringPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'get_draft_agent')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'get_draft_agent',
    N'Inspect the open Agent Management draft (code, name, mode, limits, subscription summary). Full prompt via get_draft_system_prompt.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetDraftAgent"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'get_draft_system_prompt')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'get_draft_system_prompt',
    N'Return the full SystemPrompt text of the open draft (not the HostContext preview).',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetDraftSystemPrompt"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'get_draft_subscriptions')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'get_draft_subscriptions',
    N'Return subscribed library keys, tool exclusions, and whether the draft has an AgentCode yet.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetDraftSubscriptions"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'list_libraries')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'list_libraries',
    N'List active tool libraries (LibraryKey, name, description, category). Use before subscribing.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListLibraries"}', 1, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'get_library_tools')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'get_library_tools',
    N'List tools in one library (names + short descriptions). Prefer exact LibraryKey from list_libraries.',
    N'{"libraryKey":{"type":"string","description":"LibraryKey","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetLibraryTools"}', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'search_tool_catalog')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'search_tool_catalog',
    N'Keyword-shortlist tools from the synced catalog for a use-case description. Never invent ToolNames.',
    N'{"query":{"type":"string","description":"Scenarios / keywords","required":true},"max":{"type":"integer","description":"Max tools (default 80, max 300)"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"SearchToolCatalog"}', 1, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'propose_system_prompt')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'propose_system_prompt',
    N'Normalize a proposed SystemPrompt for ask_user review. Does not apply. Prefer scenario tool sequences (§9.2), not four-H2 Role/Workflow essays.',
    N'{"systemPrompt":{"type":"string","description":"Full proposed SystemPrompt","required":true},"summary":{"type":"string","description":"Short change note"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ProposeSystemPrompt"}', 1, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'propose_tool_plan')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'propose_tool_plan',
    N'Normalize a tool plan (libraries + optional exclusions) for ask_user. Does not apply. Never include ask_user as a private BuiltIn.',
    N'{"libraryKeysJson":{"type":"string","description":"JSON array of LibraryKey","required":true},"exclusionsJson":{"type":"string","description":"JSON [{libraryKey,toolName}]"},"privateBuiltInNamesJson":{"type":"string","description":"JSON ToolName array (0-5; never ask_user)"},"reason":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ProposeToolPlan"}', 1, 7);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'apply_system_prompt_to_draft')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'apply_system_prompt_to_draft',
    N'Stage a full SystemPrompt into the open editor draft. Does not Save. Use ask_user before large overwrites. Pass clean prompt body only.',
    N'{"systemPrompt":{"type":"string","description":"Full SystemPrompt body only","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ApplySystemPromptToDraft"}', 1, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'apply_subscriptions_to_draft')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'apply_subscriptions_to_draft',
    N'Stage library subscriptions on the open draft (replaces the subscribed set). Does not Save.',
    N'{"libraryKeysJson":{"type":"string","description":"JSON array of LibraryKey","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ApplySubscriptionsToDraft"}', 1, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'apply_exclusions_to_draft')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'apply_exclusions_to_draft',
    N'Stage per-tool exclusions on the open draft. Does not Save.',
    N'{"exclusionsJson":{"type":"string","description":"JSON [{libraryKey,toolName}]","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ApplyExclusionsToDraft"}', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'validate_prompt_tool_alignment')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'validate_prompt_tool_alignment',
    N'Warn when ToolNames mentioned in the prompt are missing from the subscribed set. Pass snapshots from get_draft_*.',
    N'{"systemPrompt":{"type":"string","required":true},"libraryKeysJson":{"type":"string"},"exclusionsJson":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ValidatePromptToolAlignment"}', 1, 11);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'suggest_limit_tweaks')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'suggest_limit_tweaks',
    N'Suggest MaxToolResultChars when heavy DB tools (execute_sql / stored_procedure_execute) appear.',
    N'{"currentMaxToolResultChars":{"type":"integer"},"toolNamesJson":{"type":"string"},"systemPrompt":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"SuggestLimitTweaks"}', 1, 12);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'list_target_chats')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'list_target_chats',
    N'List recent chats for the OPEN edit target (current user). Prefer Test Run (isFixedTestSession) when debugging Agent Management runs.',
    N'{"take":{"type":"integer","description":"Max chats (default 10, max 30)"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListTargetChats"}', 1, 13);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'get_target_last_run')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'get_target_last_run',
    N'Load the target agent recent user→assistant turn(s) with ordered toolSteps from MessagesJson. Default takeTurns=3 (last few rounds). Judge by tool sequences, not chat tone.',
    N'{"sessionKey":{"type":"string","description":"Optional SessionKey from list_target_chats"},"takeTurns":{"type":"integer","description":"How many recent turns (default 3, max 10)"},"turnIndex":{"type":"integer","description":"Skip this many newest turns (default 0)"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetTargetLastRun"}', 1, 14);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'explain_run_vs_instruction')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-agent-authoring', N'explain_run_vs_instruction',
    N'Compare recent target turn(s) tool sequences to draft Instruction/subscriptions; suggest §9.4 fix class. Default takeTurns=3. Pass systemPrompt + libraryKeysJson from get_draft_* when possible.',
    N'{"sessionKey":{"type":"string"},"systemPrompt":{"type":"string"},"libraryKeysJson":{"type":"string"},"exclusionsJson":{"type":"string"},"takeTurns":{"type":"integer","description":"Recent turns to analyze (default 3, max 10)"},"turnIndex":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ExplainRunVsInstruction"}', 1, 15);

-- Re-run safe: refresh debug tool contracts if an older V082 already inserted them.
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Load the target agent recent user→assistant turn(s) with ordered toolSteps from MessagesJson. Default takeTurns=3 (last few rounds). Judge by tool sequences, not chat tone.',
    ParameterSchemaJson = N'{"sessionKey":{"type":"string","description":"Optional SessionKey from list_target_chats"},"takeTurns":{"type":"integer","description":"How many recent turns (default 3, max 10)"},"turnIndex":{"type":"integer","description":"Skip this many newest turns (default 0)"}}'
WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'get_target_last_run';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Compare recent target turn(s) tool sequences to draft Instruction/subscriptions; suggest §9.4 fix class. Default takeTurns=3. Pass systemPrompt + libraryKeysJson from get_draft_* when possible.',
    ParameterSchemaJson = N'{"sessionKey":{"type":"string"},"systemPrompt":{"type":"string"},"libraryKeysJson":{"type":"string"},"exclusionsJson":{"type":"string"},"takeTurns":{"type":"integer","description":"Recent turns to analyze (default 3, max 10)"},"turnIndex":{"type":"integer"}}'
WHERE LibraryKey = N'platform-agent-authoring' AND ToolName = N'explain_run_vs_instruction';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE AgentCode = N'agent-instruction-designer')
INSERT INTO dbo.AppAgentSkillSet
    (AgentCode, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'agent-instruction-designer',
    N'Agent Instruction Designer',
    N'Authors other agents'' SystemPrompt + Tools in Agent Management. Not listed in the left menu.',
    N'You are the Agent Instruction Designer on Agent Management.
You author the OPEN draft target in HostContextJson (mode=instruction-designer): SystemPrompt (Agent Instruction) AND Tools (library subscriptions / exclusions).
Apply-to-draft stages UI state only — the human clicks Save. You do NOT UpsertSkillSet yourself.

━━━ 1. CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Target draft — the agent being edited (targetAgentCode / isNew). HostContext has a preview; full prompt via get_draft_system_prompt.
- Instruction shape — HowAppBuilder §9.2: Concepts → hardest scenario numbered tool sequence → other scenarios → shared bans. NOT GenerateAgentDesign four-H2 Role/Workflow/Rules/Output.
- Tools job = Instruction job — pick fewest real catalog tools; never invent LibraryKey/ToolName.
- ask_user — runtime-injected for Interactive agents. NEVER recommend/register ask_user as a private BuiltIn.
- Unsaved new agents — stage prompt + subscriptions/exclusions only; private BuiltIns need AgentCode (Save first).
- Target Test Run evidence — MessagesJson toolSteps via get_target_last_run (current user + target only). Default takeTurns=3 (last few rounds). Judge by tool sequences across turns, not chat bubbles. No NLog file scrape.

━━━ 2. DEFAULT SEQUENCE — design or reshape Instruction + Tools ━━━━━━━━━━━
  1) get_draft_agent + get_draft_system_prompt + get_draft_subscriptions
  2) List scenarios the target must support (write the list; do not skip)
  3) search_tool_catalog / list_libraries / get_library_tools — ground picks in catalog
  4) Draft §9.2 SystemPrompt: concepts; hardest scenario full numbered ToolName sequence + return cues; other scenarios; shared MUST/NEVER bans
  5) propose_system_prompt + propose_tool_plan → ask_user (large overwrite / many subscriptions)
  6) apply_system_prompt_to_draft + apply_subscriptions_to_draft (+ apply_exclusions_to_draft if needed)
  7) validate_prompt_tool_alignment; suggest_limit_tweaks when heavy DB tools appear
  8) Tell the user to review Prompt/Tools tabs and click Save; then Test Run the TARGET (not this chat)

━━━ 3. OTHER SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

A) Greenfield from a short description
  Same as default; create scenarios first; subscribe only tools those scenarios call.

B) Refine existing Instruction (Edit-like)
  get_draft_*; apply user edit intent. Preserve format ONLY if already §9.2-aligned OR user asks to keep format; otherwise reshape to §9.2. May update subscriptions when scenarios require it.

C) Tools-only adjust
  get_draft_subscriptions → catalog search → ask_user → apply_subscriptions/exclusions → validate.

D) Inspect only
  get_draft_* / list_libraries / search_tool_catalog — no apply.

E) Debug why Test Run / target result looks wrong
  1) get_draft_system_prompt + get_draft_subscriptions (optional but preferred)
  2) list_target_chats (optional) or get_target_last_run(takeTurns=3) — last few rounds, not only the newest turn; raise takeTurns up to 10 if needed
  3) explain_run_vs_instruction(takeTurns=3) with that prompt + libraryKeysJson — watch repeatedFailedTools
  4) Apply ONE §9.4 fix class (subscription/Description vs one prompt sentence vs plugin hard-block) — ask_user before large overwrite
  5) User Saves and re-runs Test on the TARGET; compare the new toolSequence window

━━━ 4. HARD RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ ask_user before overwriting a long existing prompt or changing many subscriptions.
✓ Every ToolName in the prompt must be subscribed (or ask_user runtime-injected).
✓ Fewest tools; no inventing names; no duplicate library recommendations.
✓ Write/delete paths in TARGET Instruction include confirmation (ask_user or catalog propose_plan if present).
✓ When TARGET collects input, encode concrete ask_user contracts in that prompt:
  - Forms: mode=text + fieldsJson [{name,label,required?,type?,options?}]; types ONLY text|select|radio; DDL→select+options; radio→radio+options (NEVER select for radio).
  - Standalone choices: mode=single_choice|multi_choice + optionsJson [{id,display}] + ui (dropdown|radio|button_group). Never list choices only in FinalResponse prose.
✓ Private BuiltIns only when no library tool covers the need (0–5); still never ask_user.
✓ When user asks why a result happened: call get_target_last_run / explain_run_vs_instruction (default last 3 turns) before rewriting prose.
✗ Do not emit four-H2 Role/Workflow/Rules/Output without scenario tool sequences.
✗ Do not blindly preserve non-§9.2 essay structure.
✗ Do not call UpsertSkillSet / claim Save happened.
✗ Do not appear as a left-menu general chat agent; stay on this host draft.
✗ Do not dump whole libraries when three tools would do.
✗ Do not optimize chat tone while the tool sequence is still wrong.
✗ Do not invent SQL against AppGenericAgentSession — use list_target_chats / get_target_last_run.

━━━ 5. TOOL NOTES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- get_draft_* / apply_*_to_draft — host only; empty host = not in Agent Management Designer.
- search_tool_catalog — keyword shortlist; prefer exact names from results.
- propose_* — review helpers; still ask_user then apply_*.
- validate_prompt_tool_alignment — fix mismatches before finishing.
- suggest_limit_tweaks — advisory; user can set Limits tab (MaxToolResultChars).
- list_target_chats / get_target_last_run / explain_run_vs_instruction — current user + HostContext.targetAgentCode only; toolSteps from DB; takeTurns defaults to 3.

When unsure which scenario or tool, ask_user with concrete options — do not guess catalog names.',
    3, 1, 7, 1,
    80000, 60000, 8000, 10,
    40, N'Interactive', 1
);
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'agent-instruction-designer';

IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-agent-authoring')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-agent-authoring');

-- Upgrade stock designer prompt that already has debug tools but not takeTurns multi-turn window.
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
    N'2) list_target_chats (optional) or get_target_last_run (default = newest chat for this user+target)
  3) explain_run_vs_instruction with that prompt + libraryKeysJson',
    N'2) list_target_chats (optional) or get_target_last_run(takeTurns=3) — last few rounds, not only the newest turn; raise takeTurns up to 10 if needed
  3) explain_run_vs_instruction(takeTurns=3) with that prompt + libraryKeysJson — watch repeatedFailedTools')
WHERE AgentCode = N'agent-instruction-designer'
  AND SystemPrompt LIKE N'%get_target_last_run%'
  AND SystemPrompt NOT LIKE N'%takeTurns%';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
    N'- Target Test Run evidence — MessagesJson toolSteps via get_target_last_run (current user + target only). Judge by tool sequence, not chat bubbles. No NLog file scrape.',
    N'- Target Test Run evidence — MessagesJson toolSteps via get_target_last_run (current user + target only). Default takeTurns=3 (last few rounds). Judge by tool sequences across turns, not chat bubbles. No NLog file scrape.')
WHERE AgentCode = N'agent-instruction-designer'
  AND SystemPrompt LIKE N'%get_target_last_run%'
  AND SystemPrompt NOT LIKE N'%Default takeTurns=3%';
GO
