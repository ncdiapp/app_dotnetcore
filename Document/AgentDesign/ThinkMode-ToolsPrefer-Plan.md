# Think Mode · Tool I/O Detail · Tools-Prefer — Design & Feasibility Plan

**Status:** Items 1–3 implemented (2026-10-20)  
**Date:** 2026-10-20  
**Scope:** Items 1–3 from product request. Item 4 (AppAISkill from user prompt) is a separate design doc.

---

## 1. Goals (product language)

| # | Goal | Runtime default |
|---|------|-----------------|
| 1 | Think panel + Tools panel show **real I/O**: SQL text; search / analyze / UI-render / export show **input & output JSON**. Each Think step is **expandable** for detail. | Detail always available when Think is on; Tools panel always shows I/O |
| 2 | Agent setting: **Reasoning loop (Think)** checkbox. Think costs tokens → **default OFF**. Debug ON; production OFF. | Off |
| 3 | All agents **prefer tools** for **any** task when a tool exists. **Hard rule** — engine always injects; cannot be disabled in SkillSet UI. | Always on (runtime Instructions append) |

---

## 2. Current state (feasibility baseline)

### 2.1 Think Mode (already shipped, needs refinement)

| Piece | Today | Gap vs goal |
|-------|--------|-------------|
| Backend `AgentStepFilter` | Emits `thinking` + `tool_call` / `tool_result` | Think lines are causal but **short**; full SQL / JSON live only in Tool Activity `Details` |
| `AgentThinkNarrator` | Intent / outcome from args + truncated result | Not exposed as expandable “detail” payload on thinking steps |
| Chat `ThinkModePanel` | Lists thinking segments; no per-step expand with I/O | Need foldable step + optional detail block |
| Tool Activity (right) | Expandable Args / Result | Often truncated (`MaxToolResultChars`, filter 2k/4k); SQL may be buried in JSON blob — need **labeled SQL** and pretty JSON |
| Skill setting | No Think toggle | Need DB + UI + engine gate |

**Feasibility:** High. Mostly UI + richer `AgentStepEvent.Details` on `thinking` steps + a skill flag. No new infra.

### 2.2 Agent settings surface

- Table: `AppAgentSkillSet` (`APP.Components.Dto` → `AppAgentSkillSetDto`)
- UI: `AgentSkillSetManagement.tsx` (Execution Mode, Active, Agent Starts Chat First)
- Flags today: `CapabilityFlags` bitmask (`StreamTokens`, `MultiTurn`, `PlanGate`, …) — see Architecture §4
- Engine already loads skill row at run start (`GenericAgentEngine.RunAsync`)

**Feasibility:** High. Prefer a **dedicated bit** or **bool column** (see §4.2).

### 2.3 Tools-prefer for data / render / export

| Capability | Existing tool | LLM temptation today |
|------------|---------------|----------------------|
| Aggregate / GROUP BY / SUM / count | `data_analyze` (gated, session cache) | Model invents tables in markdown |
| Fetch rows | `execute_sql`, `api_provider_execute`, … | Model paraphrases instead of re-query |
| Large row fetch | `execute_sql` (COUNT first; ≤250 auto-all; else `ask_user` with count-based options) | Model invents TOP 100/1000 or omits TOP to silently load MaxCachedRows |
| Result UI | `render_interactive` / `render_none_interactive` | Model dumps HTML/markdown “dashboard” |
| Export | `export_*` / document plugins | Model pastes CSV in chat |

**Feasibility:** Medium–High for **policy** (SystemPrompt + library inject hooks already exist, e.g. `AgentDataAnalyzeHook`).  
**Hard enforcement** (block final answers that look like invented aggregates) is Medium — easy to false-positive; phase 2.

---

## 3. Item 1 — Think + Tools: SQL & JSON I/O, expandable steps

### 3.1 Event contract (backward compatible)

Extend usage of existing `AgentStepEvent` (no DTO rename required):

| Field | `tool_call` / `tool_result` (Tools panel) | `thinking` (Think panel) |
|-------|-------------------------------------------|---------------------------|
| `Description` | Short title (tool name / label) | One-line causal summary (keep current narrator style) |
| `Details` | Full **structured** text for expand | Optional **detail** block (SQL, key args, key result excerpt) |

**Recommended `Details` format (text, not a new SSE type):**

```text
## Input
```json
{ ... normalized args ... }
```

## SQL
```sql
SELECT ...
```

## Output
```json
{ ... truncated result summary ... }
```
```

Rules:

- **SQL tools** (`execute_sql`, etc.): always surface `## SQL` with the exact statement (from args), plus Output row-count / error.
- **Search / analyze / UI-render / export**: always `## Input` + `## Output` JSON (pretty, size-capped).
- Cap sizes: Input ≤ 4k, Output ≤ 4k, SQL ≤ 8k (configurable; align with `MaxToolResultChars` where sensible).
- Tools panel continues to pair call/result; formatting helper shared with Think detail.

### 3.2 Backend changes

| Component | Change |
|-----------|--------|
| `AgentThinkNarrator` | Add `BuildThinkDetail(tool, args, result, ok)` → markdown/sections for expand |
| `AgentStepFilter` | On thinking events, set `Details = BuildThinkDetail(...)`; keep `Description` short |
| Tool result path | Normalize args: extract `sql`/`query` to top-level SQL section; leave other args as JSON |
| Truncation | Single helper `ToolIoFormatter` used by filter (avoid duplicate pretty-print in React) |

### 3.3 Frontend changes

| Surface | Change |
|---------|--------|
| `ThinkModePanel` | Each step: title row (chevron) + collapsed one-liner; expand shows `Details` (SQL/JSON) in `<pre>` |
| Tool Activity | Same formatter: if Details contains `## SQL` / `## Input`, render labeled sections (not only raw dump) |
| Persistence | Store thinking `Description` (+ optional Details) on chat message / session JSON so reload keeps expand content |

### 3.4 UX sketch (Think)

```
▼ Thinking · 6 steps                          [live]
  ▼ #2 Need API for style search — catalog keyword "style"
      ## Input
      { "query": "style search" }
      ## Output
      { "items": [ { "name": "StyleSearchApi" }, ... ] }
  ▶ #3 Schema check for grouping columns…
  ▼ #6 KPI/dashboard gate — ask user before render
      ## Input
      { "contextKey": "confirm_kpi_dashboard", "prompt": "..." }
```

### 3.5 Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Token / payload size of SSE events | Caps; Think OFF by default (§4) |
| Secrets in SQL/JSON shown in UI | Reuse existing redact patterns if any; never log full payloads to NLog at Info |
| Duplicate huge blobs in Think + Tools | Same string reference in memory; UI can collapse by default |

---

## 4. Item 2 — Reasoning loop (Think) checkbox (default OFF)

### 4.1 Product rules

- **Label:** `Reasoning loop (Think)`  
- **Help:** “Emits step-by-step reasoning into chat. Uses extra tokens. Leave off in production; enable for debugging.”  
- **Default:** unchecked / `false` for new and existing agents (migration must not turn ON).  
- When **OFF**: no `thinking` SSE events; no Think panel; tool loop unchanged; Tools panel still shows tool I/O.  
- When **ON**: current Think behavior + Item 1 detail.

### 4.2 Storage options (choose one)

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| **A. New bit** `AgentCapabilityFlags.ReasoningLoop = 256` (or next free) | Fits existing checkbox UX pattern; no migration column | Bitmask clutter; PRD must document | **Preferred** if UI already maps flags to checkboxes |
| **B. New column** `EnableThinkMode BIT NOT NULL DEFAULT 0` | Explicit, easy query | Migration + DTO + BL | Prefer if flags UI is incomplete for new bits |
| **C. AgentUi / JSON blob** | No enum change | Opaque, hard to filter | Avoid |

**Decision proposal:** Option **A** if `AgentSkillSetManagement` already has a CapabilityFlags checkbox group; else **B** for clarity next to Execution Mode.

Check UI: today only Active + Agent Starts Chat First are prominent; CapabilityFlags may be wizard-only. **Safer path = Option B** `EnableThinkMode` next to Execution Mode (same row as Active).

Migration sketch:

```sql
-- V06x__AgentSkillSet_EnableThinkMode_xhu.sql
IF COL_LENGTH('dbo.AppAgentSkillSet', 'EnableThinkMode') IS NULL
  ALTER TABLE dbo.AppAgentSkillSet ADD EnableThinkMode BIT NOT NULL CONSTRAINT DF_AppAgentSkillSet_EnableThinkMode DEFAULT 0;
```

### 4.3 Engine gate

```text
skill.EnableThinkMode == true
  → emit thinking steps (narrator + detail)
  → OnIntermediateMessage thinking prose
else
  → skip all Type=thinking Fire()
  → still emit tool_call / tool_result
```

Pass flag into `AgentStepFilter` constructor (or static AsyncLocal / context) so filter does not need DB.

### 4.4 UI

- `AgentSkillSetManagement.tsx`: checkbox under Execution Mode  
- DTO / BL / save-load wired  
- Optional: chat header badge “Think on” when skill has it enabled (debug affordance)

### 4.5 Cost note

Think roughly doubles step events and can add intermediate model prose. Default OFF keeps production token use aligned with today’s Tools-only sidebar.

---

## 5. Item 3 — Tools Prefer (**hard rule — written into the engine**)

### 5.1 Principle (non-negotiable)

```
User intent → if any tool can do it → MUST call tool → LLM only narrates
❌ User intent → LLM fabricates data / UI / files / aggregates in FinalResponse
```

Applies to **any** task (not only analyze/export): fetch, SQL, API, search, aggregate, render, export, and every other tool-backed capability.

### 5.2 Implementation (done / locked)

| Mechanism | Detail |
|-----------|--------|
| **Constant** | `GenericAgentEngine.ToolsPreferHardRule` |
| **Inject** | Always appended to `Instructions` on every run: `(skillSet.SystemPrompt ?? "") + ToolsPreferHardRule` |
| **Not in DB** | Not stored on `AppAgentSkillSet`; Skill UI cannot clear or override it |
| **Scope** | All agents (Interactive + Deterministic) |

Optional later (not required for “写死”):

| Phase | Mechanism | Notes |
|-------|-----------|--------|
| P1 | Stronger per-tool descriptions | Reinforce the same rule |
| P2 | Soft FinalResponse heuristic warning | Detect invented tables/HTML when no matching tool ran |
| P3 | Forced tool routing / hard fail | Planner redesign; out of scope for v1 |

### 5.3 Tool coverage (examples)

| Intent | Prefer tool |
|--------|-------------|
| Analyze / GROUP BY / SUM | `data_analyze` / SQL |
| Large / unbounded row fetch | `execute_sql` (COUNT) → maybe `ask_user(confirm_row_limit)` → re-fetch |
| Result UI | `render_interactive` / `render_none_interactive` |
| Export | `export_*` / document tools |
| API / search | `api_provider_*` |
| Anything else with a matching tool | That tool |

If no suitable tool is registered, the model must **say so** — not invent the artifact.

---

## 6. Implementation plan (after doc approval)

### Phase A — Item 2 (gate) + Item 1 (I/O)  [~2–3 days]

1. Migration `EnableThinkMode` (default 0) + DTO/BL/API  
2. Skill UI checkbox  
3. Engine + `AgentStepFilter` respect flag  
4. `ToolIoFormatter` + thinking `Details`  
5. `ThinkModePanel` expandable steps; Tools panel labeled SQL/JSON  

### Phase B — Item 3  ✅ hard rule in engine

1. `ToolsPreferHardRule` always appended in `GenericAgentEngine` (not a SkillSet migration)  
2. Optional later: tighten tool descriptions; UserGuide note  

### Phase C — verify

1. Skill with Think OFF: no Thinking panel; Tools still show SQL/JSON  
2. Skill with Think ON: expandable causal steps + detail  
3. Style search scenario: analyze via `data_analyze`, UI via `render_*`, not invented markdown tables  
4. Removing Tools Prefer text from SkillSet SystemPrompt still leaves the hard rule in Instructions  

---

## 7. Acceptance criteria

1. Think step expands to show SQL and/or Input/Output JSON for search, analyze, UI-render, export.  
2. Tools panel shows the same class of detail (SQL text clearly labeled).  
3. New/existing agents have Think **off** unless admin checks Reasoning loop.  
4. With Think off, no thinking SSE / no Think panel; tokens for thinking path ≈ 0.  
5. **Tools Prefer** is always present in runtime Instructions for every agent; cannot be turned off in admin UI.

---

## 8. Open questions

1. Store Think detail in DB chat history or only live session / tab cache? (Proposal: persist Description + Details in session message JSON when saving.)  
2. Cap Output at 4k — enough for debug?  
3. CapabilityFlags vs `EnableThinkMode` column — confirm Option B with team.  
4. Should Deterministic mode allow Think? (Proposal: yes if checkbox on — useful for batch debug.)

---

## 9. Out of scope here

- Item 4: summarizing user prompts into `AppAISkill` / configuring skills → see `AppAISkill-FromUserPrompt-Design.md`  
- Hard LLM suppression (Phase C P3)  
- Embedding-based tool RAG (Architecture §12 backlog)
