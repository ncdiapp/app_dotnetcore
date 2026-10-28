# General Agent and Skill Library - Design

| | |
|---|---|
| **Status** | Replaces the 2026-10-26 AppAISkill / Available-Skills plan (S1-S4). That code is reverted. |
| **Date** | 2026-10-27 |
| **Language** | English (canonical) |
| **General agent key** | `__general_agent__` |
| **Skill store** | Existing `AppAISkill` + `AppAISkillRef` only |
| **Not used** | `AppAgentSkillSetSkill`, Private / Visibility / SourceType columns, Specialist to Skill binding |

The 2026-10-26 design bound skills onto Specialist agents (`Agent Management` -> Skills tab, `load_app_skill` / `save_as_app_skill` inside every agent). That model is withdrawn. Skills belong only to the **General Agent**.

Important naming note:

- **SharedAgentEngine runtime** means the shared code framework: chat sessions, files, tool calling, rendering, and streaming. During migration, the existing implementation types may retain the legacy `GenericAgent*` names for compatibility.
- **Specialist Agent** means an agent configured in `AppAgentSkillSet` and run by the SharedAgentEngine runtime.
- **General Agent** means the new built-in general chat agent. It is not a row in `AppAgentSkillSet`.

The name General Agent avoids confusion with the runtime implementation. `GenericAgent*` is a legacy code naming prefix and should not be used for new product-facing concepts.

---

## 1. Two kinds of product agent

| | Specialist Agent | General Agent |
|---|------------------|-----------------|
| Where it is configured | **Agent Management** (`AppAgentSkillSet`) | Not in Agent Management. No editable prompt, tool, limit, file, child-agent, or MCP configuration. |
| Runtime | Existing SharedAgentEngine runtime | Existing SharedAgentEngine runtime in General mode |
| Product role | Configured worker for a specific job | Built-in general entry point, like a ChatGPT-style work agent |
| Chat UI | Existing agent chat | Same family of UI: multiple chats, Thinking, tool activity, files, and results |
| Tools | Only configured private tools plus subscribed library tools, after exclusions | All active library tools and library MCP servers; no Specialist private tools |
| Skills | None. No skill tab, no skill tools, no skill index | Only this agent auto-loads and saves `AppAISkill` skills |
| Menu | Existing entries: configured agents, Agent Management, AI Skill Management | Hard-coded **first child** under the left-nav **AI Agent** menu |

Specialist agents stay the configured workers. General Agent is the unconfigured front door: it can use any library tool, pick skills from context, and hand a task to a Specialist when a configured agent is the better next step.

---

## 2. General Agent

### 2.1 Page

- Fixed page. Opened from the left menu **AI Agent**, as the **first submenu item**.
- The menu item is hard-coded in the sidebar. It is not a row created in Agent Management.
- It must not appear in Agent Management.
- It has no SkillKey editor, no System Prompt field, and no tool-subscription screen.
- The UI may reuse the existing Agent Chat components, but it passes the internal key `__general_agent__`.

### 2.2 Runtime identity

- Use fixed internal key `__general_agent__`.
- The key is allowed in chat/session/file APIs so existing `AppGenericAgentSession` storage can be reused.
- The key is **not** looked up in `AppAgentSkillSet`.
- No `AppAgentSkillSet` row is created for General Agent.
- `AppGenericAgentSession.SkillKey` may store `__general_agent__` so list/load/save/rename/delete chat behavior can use the existing session table.

### 2.3 Controller strategy

Do not create a full duplicate `GeneralAgentController` in v1.

Reuse the existing `SharedAgentController` endpoints for the large shared surface:

- run / poll / stream
- create, list, load, rename, clear, delete chats
- ask_user / plan confirmations
- chat files
- result render and export

Add General-specific branches behind `IsGeneralAgentKey(skillKey)` and General-specific endpoints only where needed, for example:

- draft skill from the current General chat
- save a reviewed skill draft
- create a hand-off to a Specialist chat

The implementation should avoid copying the full controller. If later General Agent grows a large API surface, move shared controller logic into services first, then split controllers.

### 2.4 Engine mode

The existing `SharedAgentEngine` should support a General mode when `skillKey == "__general_agent__"`:

- Do not call `AppAgentSkillSetBL.GetByKey`.
- Do not read `AppAgentSkillSet.SystemPrompt`.
- Do not use Specialist library subscriptions or tool exclusions.
- Use a code-owned General base system prompt.
- Load all active library tools and active library MCP servers.
- Exclude Specialist private tools.
- Keep platform safety checks, risky-tool policy, auth checks, and tool execution guards.
- Keep the existing streaming, tool activity, files, result rendering, HITL, and export behavior.

### 2.5 Base prompt

General Agent still needs instructions, but they are platform-owned:

- The v1 base prompt is hard-coded in code.
- It explains that General Agent should understand the user goal, select useful skills, choose tools, ask clarifying questions when needed, and propose hand-off only when a Specialist Agent is clearly better.
- The prompt is not editable in Agent Management.

---

## 3. Skills

### 3.1 Who they are for

- Only General Agent auto-loads and saves skills.
- A Specialist Agent has no link to skills: it does not list them, load them, or save them.
- Any signed-in user may create many skills.
- A skill is visible to every user. v1 has no owner, private/tenant, or permission check beyond existing AI Skill Management access.
- One General chat may use multiple skills at once. The user does not pick a skill chip list.

### 3.2 Store

Reuse `AppAISkill`:

- `Name`
- `Description`
- `SkillContent`
- `IsActive`

Reuse `AppAISkillRef` for text references.

Do not add Private columns. Do not add `AppAgentSkillSetSkill`.

`AI Skill Management` stays the manual screen: create, edit, and delete skills and their text references.

### 3.3 Skill content format

Skills generated by General Agent should use this structure:

```markdown
# When to use

# Do

# Tools

# Notes
```

- `# Do` must follow the **proven tool chain** from the saved chat (order, tool names, argument pattern). Do not add tools that never ran.
- `# Tools` lists only tools that appear in `# Do`.
- Avoid persona / `# Role` blocks and long SOP templates.
- Optional audit detail stays in `AppAISkillRef` `_source_context.md` (not loaded at runtime).

Evidence for drafting is built in `UniversalAgentWorkflowBL.BuildEvidence` from `MessagesJson` (`toolSteps`, user text, limits).

`Steps` in older docs maps to `# Do` in v1 save flow.

### 3.4 Use in a General chat

Before each General Agent run:

1. Load the active skill catalog: `SkillId`, `Name`, `Description`.
2. Ask the model to select 0-3 skills from the current user message and chat context.
3. Load the composed content for selected skills.
4. Inject selected skill content into this run's system context.

Composed content means:

- `SkillContent`
- plus `AppAISkillRef.FileContent`, ordered by `SortOrder`, `RefId`
- skipping any ref whose `FileName` starts with `_`

Loaded skill text is instructions for this chat run. It does not change Specialist configuration and does not change tool libraries.

### 3.5 Save as Skill

Only General Agent chat offers **Save as Skill**.

Specialist chat never shows Save as Skill.

Save as Skill is a reviewed flow:

1. User clicks Save as Skill on a General chat.
2. Backend builds a compact evidence package from:
   - the user request that led to the work
   - the assistant reply / final successful result
   - actual tool calls
   - tool arguments
   - tool result shape, including field names, not full row dumps
3. An LLM writes a draft `Name`, `Description`, and `SkillContent`.
4. The UI shows the draft in a modal for user review/edit.
5. User confirms save.
6. Persist a new `AppAISkill` row.
7. If the generated name already exists, append a suffix such as ` (2)`.
8. Optional audit text may be saved as an `AppAISkillRef` named `_source_context.md`.

The draft LLM must not paste the raw chat as the skill body. It must summarize the repeatable workflow as inputs, outputs, steps, tools, and constraints.

---

## 4. Tools

General Agent can call:

- all active `AppAgentLibraryTool` tools
- all active MCP servers that belong to tool libraries
- built-in platform tools that are normally available through the SharedAgentEngine runtime, when safe for General mode

General Agent must not call:

- Specialist private tools from `AppAgentToolRegister`
- tools that only exist on one Specialist configuration
- tools blocked by platform safety/auth checks

Specialist tool exclusions and per-agent subscriptions do not apply to General Agent.

---

## 5. Hand-off to a Specialist Agent

General Agent may propose a move to **one specific** Specialist Agent, meaning one active `AppAgentSkillSet` entry.

1. The model names that Specialist and explains why.
2. Nothing opens until the user agrees.
3. Agreement creates a **new** Specialist chat. It does not continue an existing Specialist chat.
4. The Specialist chat receives an AI-written summary of the task:
   - goal
   - decisions already made
   - relevant tool outcomes
   - known inputs/constraints
5. The raw General transcript is not used as the first user message.
6. After the jump, the chat is a normal Specialist chat: its own prompt and tools. It does not load skills.

### 5.1 Handoff storage

Do not add `AppGeneralAgentHandoff` in v1.

Both chats are already stored independently in `AppGenericAgentSession`.

Store lightweight hand-off metadata inside each chat's `MessagesJson`:

- General chat gets a hand-off marker with target Specialist key, target session key, and summary.
- Specialist chat gets initial summary content plus metadata with source General session key.

This is enough for v1 navigation and display. Add a hand-off table later only if we need audit/reporting/cross-chat query behavior.

Example General marker:

```json
{
  "role": "assistant",
  "content": "Handed off to DBA Agent.",
  "handoff": {
    "targetSkillKey": "dba-agent",
    "targetSessionKey": "00000000-0000-0000-0000-000000000000",
    "summary": "..."
  }
}
```

Example Specialist initial message metadata:

```json
{
  "role": "user",
  "content": "Transferred from General Agent.\n\nGoal:\n...",
  "sourceAgent": "GeneralAgent",
  "sourceSessionKey": "00000000-0000-0000-0000-000000000000",
  "handoffSummary": "..."
}
```

---

## 6. Out of scope for this revision

- Skill permissions, owners, or a private/tenant flag
- Binding skills onto a Specialist
- `load_app_skill` / `save_as_app_skill` inside Specialist runs
- An Agent Management Skills tab
- Auto-creating a Specialist from a skill
- A new hand-off table
- A duplicate full `GeneralAgentController`

---

## 7. Withdrawn implementation

The uncommitted S1-S4 code (draft-from-prompt on top of the old bind model, Available Skills tab, engine skill tools on every agent) is reverted.

`V065__ReusableSkillLibrary_xhu.sql` is a rollback script. It drops `AppAgentSkillSetSkill` and the columns `OwnerUserId`, `Visibility`, and `SourceType` when they exist. Tenants that already applied the old add-script must delete that version from `_SchemaMigrations` before migrations will run this file.

---

## 8. Acceptance

1. AI Agent menu opens General Agent first.
2. General Agent does not appear in Agent Management.
3. No `AppAgentSkillSet` row is created for `__general_agent__`.
4. General chats are stored under `AppGenericAgentSession.SkillKey = "__general_agent__"`.
5. General Agent can call active library tools that are not subscribed on any single Specialist.
6. General Agent does not call Specialist private tools.
7. General Agent can auto-select 0-3 active skills from context.
8. Skill composition skips `AppAISkillRef` rows whose `FileName` starts with `_`.
9. Save as Skill on a General chat creates one shared skill after user review.
10. The saved skill's steps match the tools and fields actually used.
11. A later General chat can load more than one skill from context without a user-picked skill list.
12. A Specialist chat never offers Save as Skill and never loads skills.
13. Hand-off asks the user, then opens a new Specialist chat seeded with a summary.
14. Hand-off linkage is stored in both chats' `MessagesJson` metadata in v1.
15. AI Skill Management can still create, edit, and delete skills by hand.

---

## 9. Implementation plan

### Phase 1 - General entry and session reuse

Goal: add the General Agent product surface while reusing the existing chat/session framework.

Tasks:

1. Add a shared constant for `__general_agent__`.
2. Add `IsGeneralAgentKey(skillKey)` helper in the backend layer used by controller/engine code.
3. Add a hard-coded **General Agent** menu item as the first child under **AI Agent**.
4. Add a General Agent route/page that reuses existing Agent Chat UI components.
5. Pass `__general_agent__` into chat create/list/load/run calls.
6. Ensure `SharedAgentController` accepts `__general_agent__` as a valid key without requiring an `AppAgentSkillSet` row.
7. Ensure General chats persist to `AppGenericAgentSession` with `SkillKey = "__general_agent__"`.

Done when:

- User can open General Agent from the AI Agent menu.
- User can create, list, rename, clear, delete, and reopen General chats.
- No General Agent row appears in Agent Management.

### Phase 2 - General engine mode

Goal: make the existing SharedAgentEngine runtime run without `AppAgentSkillSet` for General Agent.

Tasks:

1. Add a General mode branch in `SharedAgentEngine`.
2. Skip `AppAgentSkillSetBL.GetByKey` when skill key is `__general_agent__`.
3. Use a code-owned General base prompt.
4. Apply existing platform hard rules, tool preference rules, safety checks, and auth checks.
5. Keep streaming, tool activity, HITL, file access, rendering, and export behavior unchanged.
6. Make max history / recent window / max iterations use safe platform defaults in General mode.

Done when:

- A General chat can run without any `AppAgentSkillSet` row.
- Existing Specialist Agent behavior remains unchanged.
- A missing Specialist `SkillKey` still errors as before.

### Phase 3 - General tool resolver

Goal: give General Agent access to active library capabilities without Specialist subscriptions.

Tasks:

1. Add backend methods to load all active library tools.
2. Add backend methods to load all active MCP servers owned by active libraries.
3. Exclude Specialist private tools from General mode.
4. Do not apply Specialist library subscriptions.
5. Do not apply Specialist tool exclusions.
6. Keep risky-tool and permission checks before execution.
7. Deduplicate tool names with deterministic behavior if duplicate library tools exist.

Done when:

- General Agent can call a library tool that no Specialist has subscribed to.
- General Agent cannot see or call private tools registered only on one Specialist.
- Specialist Agents still only see their configured private/subscribed tools.

### Phase 4 - Skill selection and composition

Goal: let General Agent select reusable skills from context.

Tasks:

1. Add a skill catalog loader for active skills: `SkillId`, `Name`, `Description`.
2. Add model-assisted skill selection before a General run.
3. Limit selected skills to 0-3.
4. Load composed skill content for selected skills.
5. Update skill composition to skip `AppAISkillRef.FileName` values that start with `_`.
6. Inject selected skill content into the General run system context.
7. Record selected skill names/ids in tool activity or run metadata if practical for debugging.

Done when:

- General Agent can use zero skills when none are relevant.
- General Agent can load multiple relevant skills without a user-picked skill list.
- `_source_context.md` and other `_` refs are not injected into future runs.

### Phase 5 - Save as Skill

Goal: convert a successful General chat workflow into a reusable skill.

Tasks:

1. Show **Save as Skill** only in General Agent chat.
2. Hide Save as Skill in all Specialist chats.
3. Add backend endpoint to draft a skill from the current General chat.
4. Build the evidence package from user goal, final answer, tool calls, tool arguments, and result shapes.
5. Generate draft `Name`, `Description`, and `SkillContent`.
6. Use the required skill content structure: Role, Inputs, Steps, Tools, Outputs, Constraints.
7. Show a review/edit modal before saving.
8. Persist a new `AppAISkill` row after user confirmation.
9. Resolve duplicate names by appending a suffix such as ` (2)`.
10. Optionally save audit/source evidence as `_source_context.md`.

Done when:

- Save as Skill creates a new active skill.
- The saved skill describes the repeatable workflow, not a pasted transcript.
- A later General chat can select and use that saved skill.

### Phase 6 - Hand-off to Specialist

Goal: let General Agent move work to one configured Specialist when useful.

Tasks:

1. Reuse `GetActiveAgents` or equivalent to list active Specialist Agents.
2. Add a hand-off proposal UI that names one Specialist and explains why.
3. Require user confirmation before opening anything.
4. Create a new Specialist chat session after confirmation.
5. Generate a concise hand-off summary.
6. Seed the Specialist chat with the summary as the first user-visible message.
7. Add source/target metadata to both chats' `MessagesJson`.
8. Do not create `AppGeneralAgentHandoff` in v1.

Done when:

- General chat records the target Specialist session.
- Specialist chat records the source General session.
- Specialist chat behaves like a normal Specialist chat after hand-off.
- Specialist chat does not load General skills.

### Phase 7 - Regression cleanup

Goal: remove withdrawn Specialist-skill behavior and protect existing flows.

Tasks:

1. Ensure no Specialist UI exposes Save as Skill.
2. Ensure no Specialist runtime loads `AppAISkill` based on chat context.
3. Ensure Agent Management has no Skills tab from the withdrawn design.
4. Ensure rollback migration `V065__ReusableSkillLibrary_xhu.sql` remains the only DB action for withdrawn S1-S4 schema.
5. Keep AI Skill Management CRUD working.

Done when:

- Existing Specialist chat, Agent Management, and AI Skill Management flows still work.
- General-only features do not appear in Specialist screens.

---

## 10. Testing checklist

### 10.1 General entry

- [ ] AI Agent menu shows General Agent as the first child.
- [ ] General Agent route opens a chat UI.
- [ ] General Agent is not listed in Agent Management.
- [ ] There is no `AppAgentSkillSet` row for `__general_agent__`.

### 10.2 Chat session behavior

- [ ] Create a new General chat.
- [ ] Send a plain message and receive a response.
- [ ] Reopen the chat and verify transcript persists.
- [ ] Rename the chat.
- [ ] Clear the chat.
- [ ] Delete the chat.
- [ ] Verify rows use `AppGenericAgentSession.SkillKey = "__general_agent__"`.

### 10.3 Specialist regression

- [ ] Existing Specialist chat still runs with its configured prompt.
- [ ] Existing Specialist chat still uses subscribed library tools.
- [ ] Existing Specialist chat still respects private tools and exclusions.
- [ ] Missing/invalid Specialist `SkillKey` still errors.
- [ ] Save as Skill is not visible in Specialist chat.

### 10.4 General tools

- [ ] General Agent can call an active library tool not subscribed by any Specialist.
- [ ] General Agent can call an active library MCP tool.
- [ ] General Agent cannot call a Specialist private tool.
- [ ] Tool activity still records calls and results.
- [ ] Result render/export still works from General chat.

### 10.5 Skill selection

- [ ] With no relevant active skill, General Agent runs without loading a skill.
- [ ] With one relevant active skill, General Agent loads it.
- [ ] With multiple relevant active skills, General Agent can load up to 3.
- [ ] General Agent does not load more than 3 skills.
- [ ] `_` refs are skipped during composed prompt injection.

### 10.6 Save as Skill

- [ ] Save as Skill is visible in General chat.
- [ ] Draft generation uses actual user request, final answer, tool calls, args, and result field shapes.
- [ ] Draft content follows Role / Inputs / Steps / Tools / Outputs / Constraints.
- [ ] User can edit draft before saving.
- [ ] Confirm save creates a new `AppAISkill`.
- [ ] Duplicate name gets a suffix.
- [ ] Optional `_source_context.md` ref is saved but not injected later.

### 10.7 Handoff

- [ ] General Agent can propose one Specialist.
- [ ] Nothing opens until the user confirms.
- [ ] Confirming opens a new Specialist chat.
- [ ] Specialist chat starts with an AI-written summary.
- [ ] General chat stores target metadata in `MessagesJson`.
- [ ] Specialist chat stores source metadata in `MessagesJson`.
- [ ] Specialist chat does not load General skills after the jump.

### 10.8 AI Skill Management

- [ ] Existing AI Skill Management list works.
- [ ] Manual create/edit/delete still works.
- [ ] Skill refs still save and load.
- [ ] Existing non-General consumers of `AppAISkill` are not broken.
