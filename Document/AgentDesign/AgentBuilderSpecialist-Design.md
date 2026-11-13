# Agent Instruction Designer — Design

**Status:** Approved for implementation (2026-11-13). Locked: UX pattern A; UI coexist with AI / AI Edit in v1; identity `agent-instruction-designer`; **new requirements win** on prompt shape / knowledge / capability (§3.1); **§15 implementation locks** (host_tool draft bridge, unsaved-new rule, right split panel).  
**Date:** 2026-11-12 (approved 2026-11-13)  
**Related:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) (specialist design template), [HowAppBuilderAGenericAgent_Example_AppBuilderAgent.txt](./HowAppBuilderAGenericAgent_Example_AppBuilderAgent.txt) (methodology to encode — **canonical** for Instruction shape), [GeneralAgent-And-SkillLibrary.md](./GeneralAgent-And-SkillLibrary.md), [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [AppAISkill-FromUserPrompt-Design.md](./AppAISkill-FromUserPrompt-Design.md)  
**Supersedes:** none (does **not** adopt GenerateAgentDesign’s four-H2 output as the durable Instruction shape)

---

## 0. Dependency / revisit gate

Before implementing:

1. Agent Management editor already supports draft SystemPrompt + library subscriptions / exclusions / private BuiltIns (current UI).
2. SharedAgentEngine can host Interactive specialists with HostContext (Form Configurator / Form Agent patterns).
3. Tool catalog sync (`AppAgentToolCatalogBL` / V039+) is available for shortlist/search (same data `GenerateAgentDesign` uses today).
4. Agreement that **methodology** for authoring agents lives in this specialist’s editable SkillSet — not only in Cursor chats or hardcoded controller meta-prompts.
5. Conflict rule locked: see **§3.1** (new demand wins over legacy button behavior).

§0 is **passed**; product accepted this doc on 2026-11-13 — implement per §13 + §15 locks.

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

Generate’s metaPrompt (~lines 637–700) asks for JSON with four H2s (Role / Workflow / Rules / Output Format) plus RecommendedLibraryKeys / RecommendedBuiltInToolNames / RecommendedTools. Edit’s metaPrompt (~805–809) only rewrites prompt text and tends to **preserve** whatever structure is already there.

Neither call uses SharedAgentEngine tool-calling. Improving “how we design agents” requires editing C# strings, not feeding an in-app agent.

**Legacy note:** §1 describes what the buttons do **today**. It is **not** the Instruction shape or authoring method the Designer must copy. Where Generate/Edit conflict with §3.1 / §9, the Designer follows the new rules; the buttons may keep emitting the old shape until retired.

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

- **Removing** the AI / AI Edit **UI buttons** in v1 (they stay as legacy fast path — **coexist in the toolbar**). This does **not** mean the Designer must match their output shape or keep their four-H2 / preserve-structure rules.
- Editing Child-Agent / Files / Limits tabs as primary scope (Limits may get a soft suggestion only, e.g. MaxToolResultChars).
- Silent DB Upsert of the target SkillSet without user Save.
- Listing this specialist in the left menu / Chat Management as a general chat agent.
- Using this agent as a side effect to rewrite App Builder / Configurator / Form Agent product seeds without an explicit user request on that draft.
- Replacing AI Skill Management `DraftFromPrompt` (skills ≠ SkillSets).
- Treating GenerateAgentDesign’s four-H2 JSON schema as the durable Instruction format (superseded by §9.2).

---

## 3. Locked decisions (2026-11-12)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Identity | SkillSet **`agent-instruction-designer`**, display name **Agent Instruction Designer**. Scope = SystemPrompt **and** Tools (name leads with Instruction; prompt must state Tools are in scope). |
| 2 | UX | **Pattern A:** interactive chat panel inside Agent Management while editing an agent (Configurator-style host), not a one-shot modal. |
| 3 | Rollout | **Toolbar coexist** with AI / AI Edit in v1. **Authority** for how to author agents = Designer SkillSet (§3.1). Deprecate buttons only after §9.8 + dogfood. |
| 4 | Host | Agent Management editor only (`editItem` open). Not form dock / not left-menu agent. |
| 5 | Write model | Authoring tools **stage** prompt + subscription/exclusion changes into the **open draft** (UI state). Durable persist = existing **Save**. Large/destructive diffs require **`ask_user`**. |
| 6 | Visibility | Exclude from `GetActiveAgents` / left menu / Chat Management. **Test Run** in Agent Management allowed. |
| 7 | Methodology / Instruction shape | HowAppBuilder + §9.2 blocks are the source of truth (scenarios → subscribe tools → per-scenario sequences → tune via AgentLoop → harden). **Not** Generate’s four H2s. |
| 8 | AI gate | Respect `UserContext.IsAiConfigured` when that gate pass lands (same product rule as other AI surfaces). |
| 9 | Superset + migrate | Designer is a **functional superset** of AI + AI Edit (§9.7). Useful hardcoded knowledge migrates via §9.8 into seed (ask_user细则 mandatory). Legacy rules that conflict with §3.1 are **dropped**, not migrated. |
| 10 | One designer, many chats | There is **one** SkillSet `agent-instruction-designer`. Each edit-target gets its own **chat session**, not its own Designer SkillSet row (§5.1). |
| 11 | Improve-the-designer loop | Make Designer better by editing **its** SkillSet (prompt / authoring tools / descriptions) from dogfood evidence — not by forking a Designer per target (§5.2). |
| 12 | Draft bridge | FormHost-style **`onHostTool`**: all `get_draft_*` / `apply_*_to_draft` are host tools; catalog `search_*` / `list_*` / `get_library_tools` / validate / suggest are server BuiltIns (§15). |
| 13 | Unsaved new target | Prompt + library subscription / exclusion draft only until first Save; **no** private BuiltIn UpsertTool until AgentCode exists (mirror Generate warning). |
| 14 | Panel chrome | **Right split** beside the open editor (editor left, Designer chat right). Not slide-over / not dock tab in v1. |

### 3.1 Precedence — new demand wins over legacy buttons

When old button behavior and this design disagree, **this design wins** for Instruction Designer (and for any future replace of the buttons).

| Conflict | Legacy (AI / AI Edit) | New (Designer) — wins |
|----------|----------------------|------------------------|
| Instruction shape | Four H2 Role/Workflow/Rules/Output | §9.2 HowAppBuilder blocks (concepts → hardest scenario sequence → other scenarios → shared bans) |
| Edit default | Preserve existing structure/formatting | Preserve only if already §9.2-aligned **or** user explicitly asks to keep format; otherwise **reshape** to §9.2 |
| Tools on edit | AI Edit cannot change tools | Refine may update subscriptions/exclusions when scenarios require it |
| Catalog in context | Dump shortlist into one-shot metaPrompt | Authoring tools fetch/search catalog on demand |
| I/O | One-shot JSON or raw prompt string | Multi-turn AgentLoop + propose/apply + `ask_user` |
| Knowledge home | C# metaPrompt strings | Designer SkillSet seed (§9.8); C# remains only for legacy buttons until retire |
| Toolbar | Two buttons only | Buttons stay in v1; Designer is the **capability + knowledge** path |

**Coexist** means: both UIs available; legacy path may still produce old-shaped prompts. It does **not** mean Designer must imitate Generate/Edit output.

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

### 5.1 Session model — one Designer agent, one chat per edit-target

**No:** editing agent X does **not** create a new SkillSet row that is “a Designer object for X”.  
**Yes:** opening Instruction Designer while editing X opens (or resumes) a **chat session** whose HostContext.target = X.

| Concept | What it is | How many |
|---------|------------|----------|
| Designer SkillSet | `agent-instruction-designer` definition (its SystemPrompt + authoring tools) | **One** per tenant (seeded / protected) |
| Target draft | The agent row being edited in Agent Management (`app-configurator`, `app-builder`, …) | One open editor at a time in the UI |
| Chat session | Conversation with the Designer about that target (`sessionId` / ChatSessionKey) | **One stable session per (user + targetAgentCode)** (and a distinct key for unsaved-new drafts, e.g. user + `new:{clientDraftId}`) |
| Test Run session | Running the **target** agent | Separate from Designer chat |

Rules:

1. Switching the left-list selection from agent A to agent B switches the Designer panel to B’s session (HostContext retargets). Do not mix A’s transcript into B.
2. Designer chat ≠ Test Run of the target; different sessionIds.
3. Closing the panel may keep the session for resume; deleting the target agent can GC or orphan that Designer session (implement detail).
4. The Designer never becomes “owned by” the target — it only **authors** the target draft via apply-to-draft.

```
SkillSet: agent-instruction-designer  (single definition)
    │
    ├─ chat session S1  HostContext.target = app-configurator
    ├─ chat session S2  HostContext.target = app-builder
    └─ chat session S3  HostContext.target = new:draft-17
```

### 5.2 How the Designer gets better over time (iteration loop)

Goal: the more we use it (and learn in Cursor), the more accurate/efficient **the same** `agent-instruction-designer` becomes — without spawning per-target Designer clones.

| Layer | What to improve | When |
|-------|-----------------|------|
| **A. Designer SystemPrompt** | §9 methodology, ask_user contracts, anti-patterns, scenario templates | After dogfood: wrong Instruction shape, weak bans, missing ask_user detail → add **one** hard line (same repair map as §9.4, applied to the Designer itself) |
| **B. Authoring tools** | `search_tool_catalog`, propose/apply, validate alignment, Descriptions / ParameterSchema | Wrong tool picks, invented names, bad apply payloads → fix tool/plugin, not a novel in the prompt |
| **C. Catalog quality** | Library tool Descriptions the Designer reads when planning | Target agents fail because Descriptions lie → fix catalog/plugin (Designer then plans better for everyone) |
| **D. Capture evidence** | Note acceptance scripts, successful tool sequences, known gaps (HowAppBuilder step 6) — store in Designer prompt appendix or internal notes doc linked from seed | After a scenario is proven |
| **E. Do not** | Fork a Designer SkillSet per target; only patch C# `GenerateAgentDesign` metaPrompt; tune chat tone while tool sequence is wrong | — |

**Operating cadence (product):**

1. Use Designer on a real target → Save → Test Run the **target** → read AgentLoop.
2. Classify failure (§9.4): subscription/Description vs Designer prompt vs authoring plugin.
3. Fix **one class**; prefer updating `agent-instruction-designer` SkillSet in Agent Management (eat our own dogfood: open Designer on the Designer itself only for careful meta-edits, or edit its prompt tab directly).
4. Re-run the same acceptance script; keep a short changelog on the Designer SkillSet description or this design doc §16.
5. Periodically fold Cursor-side learnings into A/B — that is how in-app AI capability rises.

Optional later (not v1 blockers): memory/vector recall of past successful designs; “Save as pattern” snippets — only if they feed the **single** Designer SkillSet, not per-target clones.

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

Reuse the **draft staging** idea from Generate’s “Use this” (local dirty → user Save). Do **not** reuse Generate’s required four-H2 / JSON payload shape.

1. Specialist proposes SystemPrompt and/or tool changes (via tools + `ask_user` when large). Prompt content follows §9.2.
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
| Agent Instruction Designer | `agent-instruction-designer` | **Authoritative** path to design **other** agents’ Instruction + Tools |
| AI Generate / AI Edit | (controller one-shots) | Legacy fast path; toolbar coexist in v1; not the design authority |
| App Builder / Configurator / Form | their codes | Domain workers; may be **targets** of the designer |

---

## 7. Tools

### 7.1 New library `platform-agent-authoring`

`ToolType = BuiltIn`. Thin plugins over catalog BL + host draft bridge.

**Host vs server (locked):** dirty `editItem` lives in React — server plugins must not read saved SkillSet as truth for inspect/apply.

| Area | Tool titles | Where |
|------|-------------|--------|
| Inspect draft | `get_draft_agent`, `get_draft_system_prompt`, `get_draft_subscriptions` | **Host** (`onHostTool`) |
| Catalog | `search_tool_catalog`, `list_libraries`, `get_library_tools` | **Server** (`AgentAuthoringPlugin` + catalog BL) |
| Propose | `propose_system_prompt`, `propose_tool_plan` | **Server** (normalize / return proposal JSON for ask_user; no mutate) |
| Apply-to-draft | `apply_system_prompt_to_draft`, `apply_subscriptions_to_draft`, `apply_exclusions_to_draft` | **Host** (`onHostTool`); marks dirty; no DB Upsert |
| Validate | `validate_prompt_tool_alignment` | **Server** (args = prompt + subscription snapshot from prior host inspect) |
| Soft limits | `suggest_limit_tweaks` | **Server** (e.g. MaxToolResultChars when heavy DB tools appear) |

Dispatch: SharedAgentEngine → BuiltInToolExecutor → `AgentAuthoringPlugin` (host tools call `AskHost` like `FormHostPlugin`).

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

Instruction Designer **must** follow this method when creating or editing a target agent’s SystemPrompt + Tools. Do **not** paste a long essay. Do **not** treat GenerateAgentDesign’s four-H2 schema as the target format (§3.1). If a greenfield draft briefly uses Role/Workflow/Rules/Output labels, it still **must** satisfy §9.2 (scenario tool sequences + shared bans); otherwise rewrite before apply.

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
- Emitting Generate-style four H2s (Role / Workflow / Rules / Output) **without** §9.2 scenario tool sequences (legacy button habit — reject).
- Blindly “preserving structure” when the current prompt is a non-§9.2 essay (legacy Edit habit — reshape unless user forbids).
- Naming tools that are not in the catalog / not subscribed.
- Subscribing whole large libraries when three tools would do.
- Optimizing chat tone while the tool sequence is still wrong.
- Editing prompt and subscriptions and plugin advice in the same apply without user confirmation.

### 9.6 Seed content for `agent-instruction-designer` itself

The designer’s **own** SystemPrompt (seed) must teach §9.1–§9.5, the **functional superset** in §9.7, and every item on the **hardcoded knowledge migration checklist** in §9.8, plus:

- Host = Agent Management draft; Apply-to-draft then user Save.
- `ask_user` before large overwrites of the **target** draft.
- Tools job is equal to Instruction job (despite the display name).
- Point at HowAppBuilder as the method; use App Builder / Configurator only as **examples** of scenario lists, not as paste sources.

When we learn better authoring patterns in Cursor, **update this specialist’s SkillSet** (and this §9 if the method changes) — do not only patch `GenerateAgentDesign` / `EditSystemPrompt` metaPrompts in C#.

### 9.7 Functional superset (vs AI / AI Edit)

Instruction Designer is a **functional superset** of the two one-shot buttons: it must cover their **jobs** (create Instruction, refine Instruction, pick tools), and more. v1 **toolbar-coexists** with the buttons. “Parity” means job coverage under **§3.1 / §9.2**, not identical four-H2 JSON output.

| Capability | AI (Generate) | AI Edit | Instruction Designer (required) |
|------------|---------------|---------|----------------------------------|
| Create SystemPrompt from a description | Yes | No | Yes |
| Edit / refine existing SystemPrompt from an instruction | No (full regen only) | Yes | Yes |
| Recommend / select libraries + tools | Yes | No | Yes |
| Ground picks in real tool catalog | Yes (injected into metaPrompt) | No | Yes (via authoring tools) |
| Multi-turn refine + HowAppBuilder scenario method | No | No | Yes (extra) |
| Stage into draft → user Save | Yes | Yes | Yes |

Do **not** ship a Designer that only edits prose and cannot plan tools, or only greenfields and cannot refine.

### 9.8 Hardcoded knowledge migration checklist

Today, valuable authoring rules live only in controller metaPrompts (`AgentSkillSetController.GenerateAgentDesign` / `EditSystemPrompt`). Those strings are **not** the long-term source of truth. Before first seed of `agent-instruction-designer`, migrate the knowledge below into the designer’s **SkillSet SystemPrompt** (and/or tool Descriptions). Implementers check each row off.

**From GenerateAgentDesign metaPrompt (must migrate):**

| # | Knowledge | Seed duty |
|---|-----------|-----------|
| G1 | Do **not** recommend/register `ask_user` as a private BuiltIn; Interactive agents get it at runtime | Designer never puts `ask_user` in RecommendedBuiltIns / private tools |
| G2 | **ask_user contract — forms:** prefer one `ask_user` with `mode=text` + non-empty `fieldsJson`; fields are `{name,label,required?,type?,options?}`; types **only** `text` \| `select` \| `radio`; DDL → `type=select` + options; radio → `type=radio` + options (**never** select for radio); never collect multi-field forms only in chat prose | When writing target Instruction that collects input, paste concrete `fieldsJson` shapes into that prompt’s steps/rules |
| G3 | **ask_user contract — standalone choices:** `mode=single_choice` / `multi_choice` + non-empty `optionsJson` `[{id,display},...]` + explicit `ui` (`dropdown` / `radio` / `button_group`); never list choices only in FinalResponse text | Same — encode into target SystemPrompt when scenario needs confirm/menus |
| G4 | Workflow steps that collect input must show concrete mode/fieldsJson/optionsJson+ui — not vague “ask the user” | Enforce in §9.3 checklist |
| G5 | Target Rules must restate: MUST use ask_user for those flows; radio≠select; no numbered choices only in prose | Enforce when proposing Rules / shared bans |
| G6 | Pick **fewest** catalog tools; never invent LibraryKey/ToolName; no duplicate recommendations | Catalog search tools + validate alignment |
| G7 | Before [write]/[delete], Workflow includes confirmation (`ask_user` or catalog `propose_plan` if present); Rules say so | Default for mutating target agents |
| G8 | BuiltIns only when no library tool covers the need (0–5); still never `ask_user` | Same |
| G9 | ~~Four H2 Role/Workflow/Rules/Output as mandatory~~ | **Do not migrate as a requirement.** Superseded by §9.2. (Legacy Generate still may emit this until button retire.) |

**From EditSystemPrompt metaPrompt:**

| # | Knowledge | Seed duty |
|---|-----------|-----------|
| E1 | Apply the user’s edit instruction to the **current** prompt | Support refine-in-place, not only full regenerate — **migrate** |
| E2 | Return / propose **only** the edited prompt body (no preamble) when applying | `apply_system_prompt_to_draft` gets clean text — **migrate** |
| E3 | ~~Always preserve structure/formatting~~ | **Do not migrate as default.** New rule: preserve only if already §9.2-aligned or user explicitly asks; else reshape to §9.2 (§3.1) |

**Not migrated as-is (intentionally replaced by new design):**

- Injecting the entire shortlist catalog into a one-shot metaPrompt → `search_tool_catalog` / `list_libraries`.
- JSON-only one-shot response schema as the only I/O → propose/apply tools + chat + designer-host `ask_user`.
- Mandatory four-H2 Instruction shape (old G9).
- Unconditional “preserve formatting” on edit (old E3).

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
| AI (GenerateAgentDesign) | Yes (toolbar) | Legacy fast path; may still emit four-H2 prompts — **not** design authority |
| AI Edit (EditSystemPrompt) | Yes (toolbar) | Legacy fast path; preserve-structure habit — **not** design authority |
| Instruction Designer panel | Yes (new) | **Authority:** functional superset (§9.7); §9.8 knowledge in SkillSet; §3.1 precedence |

v1 does **not** rewrite controller metaPrompts to match §9.2 (avoid dual maintenance). Buttons stay usable; quality/method bar is on the Designer. Retire or thin-wrap buttons only after §9.8 seeded + dogfood shows parity-or-better **under the new rules** (not “same as Generate JSON”).

---

## 12. Out of scope / deferred

- Embedding-based tool RAG (keep keyword shortlist initially; same swap point as Generate).
- Automatically deleting/rewriting controller metaPrompts on deploy (knowledge is migrated via §9.8 checklist into seed; C# paths stay until explicit retire).
- Child-agent graph design UI.
- Teaching the designer to edit MCP server sync configuration.
- Pixel-perfect panel layout (reuse existing agent chat components where possible).

---

## 13. Implementation order (later)

1. Lock this design (review).
2. HostContext store + Agent Management chat panel shell + entry button (coexist).
3. `platform-agent-authoring` inspect/propose/apply-to-draft tools + host bridge.
4. Seed migration SkillSet + library; menu exclusion + code protection.
5. Designer SystemPrompt v1: HowAppBuilder (§9.1–§9.5) + superset (§9.7) + **complete §9.8 migration checklist** (especially ask_user G2–G5).
6. Dogfood: author/refine a known agent via the panel. Success = §9.2 shape + correct tool plan + ask_user contracts — **not** matching Generate’s four-H2 JSON. Compare to buttons only for coverage (can Designer do the job), not for identical output.
7. Optional: deprecate or rehome one-shots (only after §9.8 + dogfood under new rules).

---

## 14. Acceptance (design-level)

- User can open Instruction Designer while editing an agent without losing the draft editor.
- Designer can propose a SystemPrompt and a tool plan grounded in the real catalog.
- Proposed SystemPrompt follows §9.2 (concepts → hardest scenario sequence → other scenarios → shared bans); four-H2-only essays fail acceptance.
- Designer can perform both Generate-like (greenfield prompt+tools) and Edit-like (refine existing prompt, including tool updates) flows (§9.7).
- Seed includes §9.8 migrate rows (G1–G8, E1–E2, ask_user G2–G5); does **not** require old G9 / unconditional E3.
- Apply updates local draft only; Save persists.
- AI / AI Edit buttons still open and run in v1 (legacy); no requirement that their output equals Designer’s.
- Specialist does not appear in left-menu agent list.
- Improving the designer’s SkillSet changes future authoring behavior without editing C# metaPrompts.

---

## 15. Implementation locks (was open questions)

| # | Topic | Locked decision (2026-11-13) |
|---|--------|------------------------------|
| 1 | Panel chrome | **Right split** beside the open editor while `editItem` is open. |
| 2 | Apply / inspect bridge | FormHost-style **`onHostTool`** only. Do **not** use “return draft patch for panel to apply” as the v1 path. |
| 3 | Unsaved new agent | Prompt + library / exclusion **draft only** until first Save. **No** private BuiltIn `UpsertTool` until `AgentCode` exists (same spirit as Generate’s warning). |
| 4 | Session GC on delete | Non-blocking for v1 start: orphan Designer chats may remain; GC / cleanup can follow. |

---

## 16. Changelog

| Date | Change |
|------|--------|
| 2026-11-12 | Initial design: interactive Agent Instruction Designer (`agent-instruction-designer`); coexist with AI / AI Edit; draft-staging write model; `platform-agent-authoring`; HowAppBuilder methodology; analysis of GenerateAgentDesign / EditSystemPrompt. |
| 2026-11-12 | Expanded §9 from HowAppBuilder reference: operating loop, required prompt block structure, checklist, AgentLoop repair map, anti-patterns, seed duties for the designer itself. |
| 2026-11-12 | Added §9.7 functional superset vs AI/AI Edit; §9.8 hardcoded knowledge migration checklist (ask_user contracts G2–G5 mandatory in seed). |
| 2026-11-12 | Conflict pass: §3.1 new-demand-wins; HowAppBuilder §9.2 beats four-H2; E3 preserve no longer default; coexist = toolbar only; G9 not migrated as requirement; dogfood/acceptance aligned. |
| 2026-11-12 | Locked §5.1 one Designer SkillSet + chat-per-target; §5.2 improve-the-designer loop (prompt/tools/catalog dogfood). |
| 2026-11-13 | **Approved for implementation.** Locked §15: host_tool draft bridge, unsaved-new BuiltIn rule, right-split panel; §3 rows 12–14; §7.1 host vs server table. |
