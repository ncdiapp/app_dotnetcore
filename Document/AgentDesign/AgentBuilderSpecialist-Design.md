# Agent Instruction Designer — Design

**Status:** Requirements draft (2026-11-12). Locked: UX pattern A + Coexist with AI / AI Edit; identity `agent-instruction-designer`. Not yet implementation-ready until this doc is reviewed.  
**Date:** 2026-11-12  
**Related:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) (specialist design template), [HowAppBuilderAGenericAgent_Example_AppBuilderAgent.txt](./HowAppBuilderAGenericAgent_Example_AppBuilderAgent.txt) (methodology to encode), [GeneralAgent-And-SkillLibrary.md](./GeneralAgent-And-SkillLibrary.md), [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [AppAISkill-FromUserPrompt-Design.md](./AppAISkill-FromUserPrompt-Design.md)  
**Supersedes:** none

---

## 0. Dependency / revisit gate

Before implementing:

1. Agent Management editor already supports draft SystemPrompt + library subscriptions / exclusions / private BuiltIns (current UI).
2. SharedAgentEngine can host Interactive specialists with HostContext (Form Configurator / Form Agent patterns).
3. Tool catalog sync (`AppAgentToolCatalogBL` / V039+) is available for shortlist/search (same data `GenerateAgentDesign` uses today).
4. Agreement that **methodology** for authoring agents lives in this specialist’s editable SkillSet — not only in Cursor chats or hardcoded controller meta-prompts.

§0 is **passed for design**; implement when product accepts this doc.

---

## 1. Why — and what the current AI buttons do

### 1.1 Product gap

We can make agents work by tuning SystemPrompt and Registered Tools in Cursor / Agent Management by hand. That produces usable agents, but it does **not** raise the app’s durable AI capability: the “how to correctly build an agent” knowledge stays outside the product.

Today’s **AI** and **AI Edit** buttons on Agent Instruction are convenient one-shots. Their meta-prompts are **hardcoded** in the controller. They cannot be improved as a managed agent, cannot run a multi-turn AgentLoop with authoring tools, and (for Edit) cannot optimize tool selection or method steps.

### 1.2 Current buttons — analysis

Both live on Agent Setting → Prompt → “Agent Instruction” toolbar in [`AgentSkillSetManagement.tsx`](../../AppReact/src/components/aiskill/AgentSkillSetManagement.tsx).

| Button | Icon | API | Behavior | Limitation |
|--------|------|-----|----------|------------|
| **AI** | wand | `GenerateAgentDesign` | User describes the agent → one LLM call → SystemPrompt + recommended libraries / BuiltIns / tools → review checkboxes → “Use this” writes **local draft** | Hardcoded metaPrompt; keyword shortlist (max 300); no AgentLoop; not a SkillSet we can feed |
| **AI Edit** | pencil | `EditSystemPrompt` | User gives an edit instruction + current prompt → one LLM call → edited prompt string → Apply to draft | Hardcoded editor metaPrompt; **no** tool catalog; cannot change subscriptions |

**Code path:**

```
AgentSkillSetManagement.tsx
  → agentSkillSetSvc.ts (GenerateAgentDesign / EditSystemPrompt)
  → AgentSkillSetController.cs (inline metaPrompt)
  → LLMProviderHelper.CallLLMAsync
  → Apply dirties editItem; user must Save (UpsertSkillSet + subscriptions/exclusions)
```

Generate’s metaPrompt (~lines 637–700) asks for JSON with Role / Workflow / Rules / Output Format plus RecommendedLibraryKeys / RecommendedBuiltInToolNames / RecommendedTools. Edit’s metaPrompt (~805–809) only rewrites prompt text.

Neither call uses SharedAgentEngine tool-calling. Improving “how we design agents” requires editing C# strings, not feeding an in-app agent.

### 1.3 Desired shift

Encode the HowAppBuilder methodology into a **managed specialist** (`agent-instruction-designer`) whose own SystemPrompt + tools we improve over time as we learn how to build good agents — so the app’s AI gets smarter at agent authoring.

---

## 2. Goal / Non-goals

### Goal

When a user creates or edits an agent in **Agent Management**, they can open an interactive chat with **Agent Instruction Designer**. That specialist helps author and refine:

- **SystemPrompt** (Agent Instruction) — scenarios, tool sequences, hard rules
- **Tools selection** — library subscriptions, exclusions, private BuiltIns where needed

using SharedAgentEngine + authoring tools. Changes stage into the **open editor draft**; durable persist remains the existing **Save**.

### Non-goals (v1)

- Replacing or deleting the existing **AI** / **AI Edit** one-shot buttons (they **coexist**).
- Editing Child-Agent / Files / Limits tabs as primary scope (Limits may get a soft suggestion only, e.g. MaxToolResultChars).
- Silent DB Upsert of the target SkillSet without user Save.
- Listing this specialist in the left menu / Chat Management as a general chat agent.
- Using this agent as a side effect to rewrite App Builder / Configurator / Form Agent product seeds without an explicit user request on that draft.
- Replacing AI Skill Management `DraftFromPrompt` (skills ≠ SkillSets).

---

## 3. Locked decisions (2026-11-12)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Identity | SkillSet **`agent-instruction-designer`**, display name **Agent Instruction Designer**. Scope = SystemPrompt **and** Tools (name leads with Instruction; prompt must state Tools are in scope). |
| 2 | UX | **Pattern A:** interactive chat panel inside Agent Management while editing an agent (Configurator-style host), not a one-shot modal. |
| 3 | Rollout | **Coexist** with AI / AI Edit. Deprecate/replace those only after this specialist is proven. |
| 4 | Host | Agent Management editor only (`editItem` open). Not form dock / not left-menu agent. |
| 5 | Write model | Authoring tools **stage** prompt + subscription/exclusion changes into the **open draft** (UI state). Durable persist = existing **Save**. Large/destructive diffs require **`ask_user`**. |
| 6 | Visibility | Exclude from `GetActiveAgents` / left menu / Chat Management. **Test Run** in Agent Management allowed. |
| 7 | Methodology | HowAppBuilder steps are the source of truth for the specialist’s SystemPrompt (scenarios → subscribe tools → per-scenario sequences → tune via AgentLoop → harden). |
| 8 | AI gate | Respect `UserContext.IsAiConfigured` when that gate pass lands (same product rule as other AI surfaces). |

---

## 4. Naming

### Locked

| Field | Value |
|-------|--------|
| AgentCode | `agent-instruction-designer` |
| Display name | Agent Instruction Designer |

### Alternatives considered

| Candidate | AgentCode sketch | Notes |
|-----------|------------------|--------|
| Agent Instruction Designer *(locked)* | `agent-instruction-designer` | UI-aligned (“Agent Instruction”) |
| Agent Designer Agent | `agent-designer` | Shorter; less tied to Instruction label |
| Agent Author Agent | `agent-author` | Emphasizes writing the definition |
| Agent Instruction Agent | `agent-instruction` | Earlier temp name |
| Agent Design Coach | `agent-design-coach` | Methodology / teaching |
| Prompt & Tools Coach | `prompt-tools-coach` | Explicit dual job; longer |
| SkillSet Designer | `skillset-designer` | Matches entity name |
| Meta-Agent Builder | `meta-agent-builder` | Accurate but jargon-heavy |

---

## 5. Trigger & UX

### Entry

On the Agent Instruction toolbar, **next to** AI / AI Edit, add an entry (working label: **Design with Agent** or **Instruction Designer**).

- Opens a side/right **chat panel** bound to the agent currently being edited.
- Does not close or replace the Prompt / Tools editors; user can see draft update after Apply.
- Separate `sessionId` from Test Run and from any other agent chat.

### Visibility

```
IsAiConfigured   (when gate retrofit lands)
AND user is editing an agent in Agent Management
AND agent-instruction-designer skill exists / active
```

When the specialist SkillSet is missing, hide the entry (or show a one-line “not seeded” — prefer hide).

### HostContextJson (sketch)

Each turn injects a compact catalog of the **target draft** (not FormData):

```json
{
  "surface": "agent-management",
  "mode": "instruction-designer",
  "targetAgentCode": "app-configurator",
  "targetDisplayName": "App Configurator Agent",
  "isNew": false,
  "dirty": true,
  "systemPromptChars": 4200,
  "systemPromptPreview": "You are the App Configurator Agent...",
  "subscribedLibraryKeys": ["platform-form-config"],
  "excludedTools": [],
  "privateBuiltInToolNames": [],
  "executionMode": "Interactive",
  "limits": { "maxToolResultChars": 8000 }
}
```

Full prompt text and full tool catalogs are loaded via **inspect / search tools** on demand — do not dump entire catalogs into every turn’s HostContext.

**Server rule:** mutate-to-draft tools bind to the host’s target draft / chat session key. Client-supplied `targetAgentCode` must match HostContext or the tool returns `{ ok: false, error }`.

### Apply semantics

Mirror today’s Generate “Use this”:

1. Specialist proposes SystemPrompt and/or tool changes (via tools + `ask_user` when large).
2. Host applies accepted proposal into `editItem` + subscription/exclusion local state (marks dirty).
3. User clicks **Save** for UpsertSkillSet / SetSubscriptions / SetToolExclusions (existing path).

---

## 6. Agent roles

```
Agent Management — editing SkillSet X
  ┌─────────────────────────────┐  ┌──────────────────────────────────┐
  │ Prompt / Tools editor       │  │ Instruction Designer panel       │
  │ (human edit + AI / AI Edit) │  │ agent-instruction-designer       │
  │ draft of X                  │◄─│ authors prompt + tools for X     │
  └─────────────────────────────┘  └──────────────────────────────────┘

Product agents (not replaced by this designer):
  app-builder, app-configurator, app-transaction-form-agent, __general_agent__
```

| Agent | SkillKey | Job |
|-------|----------|-----|
| Agent Instruction Designer | `agent-instruction-designer` | Help design **other** agents’ Instruction + Tools |
| AI Generate / AI Edit | (controller one-shots) | Fast path; coexist in v1 |
| App Builder / Configurator / Form | their codes | Domain workers; may be **targets** of the designer |

---

## 7. Tools

### 7.1 New library `platform-agent-authoring`

`ToolType = BuiltIn`. Thin plugins over catalog BL + host draft bridge (exact method names at implement time).

| Area | Working tool titles | Notes |
|------|---------------------|--------|
| Inspect draft | `get_draft_agent`, `get_draft_system_prompt`, `get_draft_subscriptions` | Current target from HostContext |
| Catalog | `search_tool_catalog`, `list_libraries`, `get_library_tools` | Reuse catalog sync; shortlist like Generate |
| Propose | `propose_system_prompt`, `propose_tool_plan` | Return structured proposal for review |
| Apply-to-draft | `apply_system_prompt_to_draft`, `apply_subscriptions_to_draft`, `apply_exclusions_to_draft` | Host UI state only; HITL for large diffs |
| Validate | `validate_prompt_tool_alignment` | Prompt mentions vs subscribed tools; warn mismatches |
| Soft limits | `suggest_limit_tweaks` | e.g. MaxToolResultChars when heavy DB tools appear |

Dispatch: SharedAgentEngine → BuiltInToolExecutor → new plugin (e.g. `AgentAuthoringPlugin`).

### 7.2 Do not

- Call `UpsertSkillSet` / `SetSubscriptions` directly from tools in v1 (bypass user Save).
- Recommend or register `ask_user` as a private BuiltIn (runtime-injected for Interactive).
- Invent library keys or tool names not in catalog.

### 7.3 System tool

Interactive agents keep auto-injected **`ask_user`**. Require it before overwriting a long existing prompt or changing many subscriptions.

---

## 8. Write → confirm → Apply → Save

```
User (Instruction Designer chat)
  → inspect draft / search catalog
  → propose prompt sections and/or tool plan
  → ask_user (when overwrite / large tool set)
  → apply_*_to_draft (host updates editItem)
  → user reviews Prompt + Tools tabs
  → Save (existing UpsertSkillSet + subscriptions/exclusions)
```

Optional later: “Save & Test Run” shortcut — not required for v1.

---

## 9. How to write SystemPrompt (Agent Instruction)

**Canonical reference:** [HowAppBuilderAGenericAgent_Example_AppBuilderAgent.txt](./HowAppBuilderAGenericAgent_Example_AppBuilderAgent.txt)  
（App Builder Agent 如何调到可用 — 在 Agent Management 里配置 Generic Agent 的对照方法。）

Instruction Designer **must** follow this method when creating or editing a target agent’s SystemPrompt + Tools. Do **not** paste a long essay, do **not** invent a free-form “Role / Workflow / Rules / Output Format” wall unless it still satisfies the blocks below. The GenerateAgentDesign four-H2 schema may be used as a **thin starter** for greenfield only; the durable shape is the HowAppBuilder block structure.

### 9.1 Operating loop (designer must run these steps)

| Step | What | Why |
|------|------|-----|
| 1. List scenarios | Agent does more than one job. Write the full list first (example from App Builder: new app, alter tables, change existing Transaction, Search-only, delete app, gated confirm…). | Prompt without scenarios becomes vague prose. |
| 2. Subscribe tools | Only tools the scenarios will actually call. Unused tools confuse the model — do not subscribe “just in case”. | Tool list is part of the agent definition, not decoration. |
| 3. Per scenario, extract three things | (a) Success **tool order** (exact ToolName + which return fields to read next). (b) Short **hard rules** (allowed / forbidden). (c) **Domain forks** if any (example: master-detail docs vs lookup reference tables; where FKs live). | This is the material that becomes prompt sections. |
| 4. Write one SystemPrompt (one document, with blocks) | See §9.2. | Models read prompt + tool Descriptions together — they must not contradict. |
| 5. Tune one scenario at a time | Fixed acceptance script per scenario; run hardest first. Judge by **AgentLoop tool sequence**, not chat bubbles. | See §9.4 repair map. |
| 6. Harden | Freeze prompt + subscriptions; record scripts, successful tool-sequence signatures, known gaps. | Makes the agent shippable and re-tunable later. |

### 9.2 Required SystemPrompt block structure

Write **one** SystemPrompt with clear blocks (headings or numbered sections). Order:

1. **Concepts (short)** — a few lines: who the agent is, durable vs ephemeral work, HostContext / key nouns if any. No novel.
2. **Default / hardest scenario — numbered tool sequence** — put the primary or hardest path first (HowAppBuilder put「新建应用」first and made it work first). Each step names the **exact ToolName** and what to look at in the return value before the next step.
3. **Other scenarios — short sequences** — each scenario a small numbered list; same discipline (tool names + return cues).
4. **Shared bans / shared allows** — one place for cross-scenario hard rules (confirm before write/delete, never invent tool names, etc.).

**Alignment rule (critical):** wording in the step text for “when to call / what to read next” must stay consistent with that tool’s **library Description**. The model sees both. If they conflict, fix Description or prompt — do not leave both.

### 9.3 What good Instruction looks like (checklist for propose/apply)

Instruction Designer should self-check before `apply_system_prompt_to_draft`:

- [ ] Every supported scenario appears (or is explicitly out of scope).
- [ ] Every ToolName in the prompt is subscribed (or runtime-injected like `ask_user`).
- [ ] No subscribed tools that no scenario uses (unless user explicitly kept them).
- [ ] Hardest scenario has a full numbered sequence, not a paragraph summary.
- [ ] Domain forks are explicit where the same user intent branches.
- [ ] Shared bans are short and hard (“MUST / NEVER”), not essays.
- [ ] Prompt steps do not contradict tool Descriptions.
- [ ] Write/delete paths include confirmation (`ask_user` / equivalent) where risk requires it.

### 9.4 Repair map (when Test Run / AgentLoop fails)

Same rules as HowAppBuilder — designer should recommend the **smallest** fix class:

| Symptom | Fix where |
|---------|-----------|
| Wrong tool / bad parameters | Subscription set or tool **Description** (and ParameterSchema), not a long prompt rewrite |
| Wrong order / weak ban | Add **one** prompt sentence; do not write a short essay |
| Lying / wrong-shaped returns | **Plugin** / tool implementation |
| Same class of error twice+ | Prefer **plugin hard-block** over more prompt text |

Tune **one class per round**; compare the new run. Always judge by tool sequence in the AgentLoop log.

### 9.5 Anti-patterns (designer must refuse or rewrite)

- Dumping hardcoded App Builder / Configurator prose wholesale without scenario extraction.
- Only four generic H2s (Role / Workflow / Rules / Output) with no per-scenario tool sequences.
- Naming tools that are not in the catalog / not subscribed.
- Subscribing whole large libraries when three tools would do.
- Optimizing chat tone while the tool sequence is still wrong.
- Editing prompt and subscriptions and plugin advice in the same apply without user confirmation.

### 9.6 Seed content for `agent-instruction-designer` itself

The designer’s **own** SystemPrompt (seed) must teach §9.1–§9.5 plus:

- Host = Agent Management draft; Apply-to-draft then user Save.
- `ask_user` before large overwrites.
- Tools job is equal to Instruction job (despite the display name).
- Point at HowAppBuilder as the method; use App Builder / Configurator only as **examples** of scenario lists, not as paste sources.

When we learn better authoring patterns in Cursor, **update this specialist’s SkillSet** (and this §9 if the method changes) — do not only patch `GenerateAgentDesign` metaPrompt in C#.

---


## 10. Seed & protection (when implementing)

1. Insert SkillSet `agent-instruction-designer` **IF NOT EXISTS** (do not overwrite admin-edited prompt on re-run).
2. Seed library `platform-agent-authoring` + BuiltIn tools.
3. Subscribe the designer to that library (minimal surface).
4. Protect AgentCode: no rename/delete; block collisions (parity with Form Agent / Configurator).
5. Exclude from `GetActiveAgents` / left menu / Chat Management.
6. Do not add to General Agent allowlist by default.

---

## 11. Coexistence with AI / AI Edit

| Path | Keep in v1? | Role |
|------|-------------|------|
| AI (GenerateAgentDesign) | Yes | Fast one-shot greenfield design |
| AI Edit (EditSystemPrompt) | Yes | Fast one-shot prompt tweak |
| Instruction Designer panel | Yes (new) | Multi-turn, tool-aware, improvable SkillSet |

After acceptance criteria (below) are met, a later decision may retire or thin-wrap the one-shots behind the same specialist.

---

## 12. Out of scope / deferred

- Embedding-based tool RAG (keep keyword shortlist initially; same swap point as Generate).
- Auto-migrate hardcoded controller metaPrompts into the SkillSet.
- Child-agent graph design UI.
- Teaching the designer to edit MCP server sync configuration.
- Pixel-perfect panel layout (reuse existing agent chat components where possible).

---

## 13. Implementation order (later)

1. Lock this design (review).
2. HostContext store + Agent Management chat panel shell + entry button (coexist).
3. `platform-agent-authoring` inspect/propose/apply-to-draft tools + host bridge.
4. Seed migration SkillSet + library; menu exclusion + code protection.
5. Designer SystemPrompt v1 (HowAppBuilder methodology).
6. Dogfood: author/refine a known agent (e.g. configurator-like) via the panel; compare quality vs AI / AI Edit.
7. Optional: deprecate or rehome one-shots.

---

## 14. Acceptance (design-level)

- User can open Instruction Designer while editing an agent without losing the draft editor.
- Designer can propose a SystemPrompt and a tool plan grounded in the real catalog.
- Proposed SystemPrompt follows §9 block structure (concepts → hardest scenario sequence → other scenarios → shared bans), not only generic H2 essay.
- Apply updates local draft only; Save persists.
- AI and AI Edit still work unchanged.
- Specialist does not appear in left-menu agent list.
- Improving the designer’s SkillSet in Agent Management changes future authoring behavior (no C# metaPrompt edit required for methodology tweaks).

---

## 15. Open questions (non-blocking for this draft)

1. Exact panel chrome: split view vs slide-over vs dock tab.
2. Whether apply tools push events to React via SignalR/host callback vs returning a structured “draft patch” the panel applies.
3. Whether targeting an **unsaved new** agent (no AgentCode yet) allows BuiltIn UpsertTool or only library subscriptions until first Save (mirror Generate’s current warning).

---

## 16. Changelog

| Date | Change |
|------|--------|
| 2026-11-12 | Initial design: interactive Agent Instruction Designer (`agent-instruction-designer`); coexist with AI / AI Edit; draft-staging write model; `platform-agent-authoring`; HowAppBuilder methodology; analysis of GenerateAgentDesign / EditSystemPrompt. |
| 2026-11-12 | Expanded §9 from HowAppBuilder reference: operating loop, required prompt block structure, checklist, AgentLoop repair map, anti-patterns, seed duties for the designer itself. |
