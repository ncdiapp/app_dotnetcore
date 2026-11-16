# Search View Config Agent — Design

**Status:** Requirements draft (2026-11-16). Locked: identity, tool names, LibraryKey **`platform-search-view-config`**, hosts=both, criteria CRUD, dataset sync, reload, tab **Configurator**. Editor multi-agent router lives in [MyApplicationEditor-AiAgent-Design.md](./MyApplicationEditor-AiAgent-Design.md) **§11** (not this doc).  
**Date:** 2026-11-16  
**Related:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md), [MyApplicationEditor-AiAgent-Design.md](./MyApplicationEditor-AiAgent-Design.md) §11, [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [AgentBuilderSpecialist-Design.md](./AgentBuilderSpecialist-Design.md)  
**Relationship to App Configurator:** Separate SkillSet. Form/TX durable config stays `app-configurator`; Search durable config is this agent. Editor when Search is open → this skill (MyApplicationEditor §11).

---

## 0. Dependency / revisit gate

Before implementing:

1. Form Configurator (`app-configurator` + `platform-form-config`) patterns accepted (HostContext, ask_user, reload).
2. BL already supports durable Search/View/Dataset/Criteria writes (`AppSearchConfigBL`, `AppSearchViewConfigBL`, `AppDataSetBL`) — no new persistence layer.
3. Agreement that **Search config tools** live in a dedicated library (not wholesale `platform-search` create tools; not Form’s `platform-form-config`).
4. Product accepts **separate** SkillSet from `app-configurator` (this doc).

§0 is **passed for design**; Editor router locked in MyApplicationEditor §11 — Search View Config coding can proceed (runtime Search dock and/or library first).

---

## 1. Why

Users configure Search today via Search Configuration UI (name, View, Dataset/SQL, criteria fields, result columns).

Agents today can **`create_search`** (and list/execute) via `platform-search`, but **cannot update** an existing Search:

| Need | Agent today |
|------|-------------|
| Rename Search | ✗ |
| Switch / rebind SearchView | ✗ |
| Change Dataset / SQL | ✗ |
| Change Search Fields (Criterias) | ✗ |
| Change View result columns | ✗ |

App Configurator covers **Form** durable config. Search needs the same class of specialist — scoped to the open Search (and its View / Dataset / Criteria), with HITL + reload — not mixed into Form Configurator or greenfield App Builder.

---

## 2. Goal / Non-goals

### Goal

A managed specialist **Search View Config Agent** that, while the user is on a Search (runtime and/or Editor drill-in — MyApplicationEditor §11), can inspect and **update**:

- Search basic settings (name, flags, SearchView / DataSet **binding**)
- Search criteria (`AppSearchField`)
- Search View columns (`AppSearchViewField`)
- Dataset definition (`AppDataSet` — SQL / DataSource / name)

using SharedAgentEngine + scoped BuiltIns. Writes go to DB via existing config BL; UI reloads Search metadata after confirm (Form Configurator pattern).

### Non-goals (v1)

- Replacing Search Configuration screens (parallel enhancement only).
- Greenfield `create_search` / create-anywhere Dataset as the primary job (may *call* existing create tools later under policy — default no).
- Form Transaction / Unit / Field / Layout config (that remains `app-configurator`).
- Listing this agent in the left menu / Chat Management as a general chat agent (Agent Management Test Run allowed).
- User criteria **presets** (`AppSearchSaved`) as primary scope.
- Pixel-perfect grid designer inside chat.

---

## 3. Locked decisions (2026-11-16)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Identity | **New SkillSet** — display name **Search View Config Agent**. AgentCode **`app-search-view-config`**. Not `app-configurator`; not `app-builder`. |
| 2 | Role split | Form durable config → `app-configurator`. Search durable config → **this agent**. Editor drill-in: see MyApplicationEditor §11. |
| 3 | Tools (v1 names locked) | See §7.1. |
| 4 | Write model | Durable DB write via config BL. **`ask_user` before every durable write**. After success → **`request_search_reload`**. |
| 5 | Id preference | Prefer **searchId / searchViewId / dataSetId / searchFieldId / searchViewFieldId**. |
| 6 | Dataset tool | **`update_dataset`**. Shared-Dataset HITL warning. After SQL change: **optional sync view columns + ask_user** (not silent always-regenerate). |
| 7 | View columns tool | **`update_search_view_columns`**. |
| 8 | Basic settings tool | **`update_search_basic_setting`**. |
| 9 | Hosts v1 | **Both**: runtime `AppSearch` **and** Application / Form Builder editor surfaces that edit Search/View. |
| 10 | UI tab label | **Configurator** (same word as Form Configurator tab; host page distinguishes which specialist runs). |
| 11 | Criteria | **Full CRUD** in v1 (create / update / delete Search Fields — not update-only). Exact tool split §7.1. |
| 12 | Menu | Exclude from `GetActiveAgents` / left menu / Chat Management. Protect AgentCode. |
| 13 | Test Run (no host) | Mutate tools require **explicit `searchId`** (and related ids as needed). See §13 glossary. |
| 14 | Reload host tool | **`request_search_reload`**. |

### Locked tool names (do not rename without doc update)

| ToolName | Object |
|----------|--------|
| `get_search_config` | Inspect Search + bound View/Dataset + criteria/columns summary |
| `update_search_basic_setting` | `AppSearch` root: name, description, isAutoExecute, searchViewId, dataSetId, … |
| `update_search_criteria` | `AppSearchField` CRUD (Criterias) — may be one tool with `op` or split add/update/remove; capability locked to CRUD |
| `update_search_view_columns` | `AppSearchViewField` (result columns; extend to CRUD if needed in same slice) |
| `update_dataset` | `AppDataSet` (QueryText, DataSourceFrom, Name, …) + optional sync-columns flag |
| `get_search_view_link_targets` | `AppFormLinkTarget` on SearchView (`SourceType=SearchView`) — Link To Form / FormGroup / Search / Report / System page |
| `get_search_view_link_target_detail` | Full one Link Target |
| `upsert_search_view_link_target` | CRUD create/update; `usageType` 1–5; destinations per AppConfigurator §7.6 |
| `delete_search_view_link_target` | HITL delete by `linkTargetId` |
| `get_view_linked_search_or_url` | `AppViewLinkedSeaechOrUrl` (inline linked search/URL — **not** Link Target menus) |
| `upsert_view_linked_search_or_url` | Create/update inline linked search/URL |
| `delete_view_linked_search_or_url` | HITL |
| `request_search_reload` | Host: prompt Reload current Search |

| 15 | LibraryKey | **`platform-search-view-config`** (new). Create/list/run stay on existing `platform-search`. Optional read subscribe `list_searches` later. |

---

## 4. Naming

### Locked

| Field | Value |
|-------|--------|
| Display name | Search View Config Agent |
| AgentCode | `app-search-view-config` |
| LibraryKey | `platform-search-view-config` |
| Plugin | `SearchViewConfigPlugin` |
| Tab label | **Configurator** |

---

## 5. Trigger & UX

**Surfaces (locked: both):**

1. **Runtime `AppSearch`** — Configurator tab / dock (parity with Form Configurator).
2. **Editor** — My Application Editor when Search/View is open (chrome: MyApplicationEditor §11).

- Separate session from Form Assistant / Form Configurator / App Builder chats.
- HostContext `surface: "search"`, `mode: "search-view-config"`, with searchId / searchViewId / dataSetId.

**Editor multi-agent chat:** see [MyApplicationEditor §11](./MyApplicationEditor-AiAgent-Design.md).

### HostContextJson (sketch)

```json
{
  "surface": "search",
  "mode": "search-view-config",
  "searchId": 501,
  "searchName": "Sales Order Search",
  "searchViewId": 902,
  "dataSetId": 301,
  "saasApplicationId": 12
}
```

Full field/column lists via **`get_search_config`** (and later find_* tools) — do not dump entire criteria/column catalogs into every turn.

**Server rule:** when HostContext is present, mutate tools bind to host `searchId` / default view / dataset; client-supplied ids must match or return `{ ok: false, error }`. When running from Agent Management Test Run without host, require explicit ids.

---

## 6. Agent roles

```
Search runtime / Search editor
  ┌─────────────────────────────┐  ┌──────────────────────────────────┐
  │ Search Configuration UI     │  │ Search View Config panel         │
  │ (human)                     │  │ app-search-view-config           │
  │                             │◄─│ authors Search / View / Dataset  │
  └─────────────────────────────┘  └──────────────────────────────────┘

Not this agent:
  app-configurator  — Form (Transaction/Unit/Field/Layout/…)
  app-builder       — greenfield / broad builder
  platform-search create_search — create-only (legacy path; not design authority for update)
```

| Agent | SkillKey (provisional) | Job |
|-------|------------------------|-----|
| Search View Config Agent | `app-search-view-config` | Durable config of open Search + View + Criteria + Dataset |
| App Configurator | `app-configurator` | Durable config of open Form |
| App Builder | `app-builder` | Broad build; not Search-update specialist |

---

## 7. Tools

### 7.1 Library (provisional key) + locked tools

`ToolType = BuiltIn`. Thin plugins over:

- `AppSearchConfigBL` (`RetrieveOneAppSearchExDto`, `SaveAppSearchExDto`, `SaveAppSearchFieldExDto`, …)
- `AppSearchViewConfigBL` (`SaveAppSearchViewExDto`, `SaveAppSearchViewFieldExDto`, …)
- `AppDataSetBL` (`SaveOneAppDataSetEntityDto`, …)

| Tool | Behavior (intent) | BL |
|------|-------------------|-----|
| `get_search_config` | Return Search root + bound DataSet/View ids/names + criteria list (id/path/op/control) + view columns summary | Retrieve ExDtos |
| `update_search_basic_setting` | `changesJson` partial keys on Search root only (name, description, isAutoExecute, searchViewId, dataSetId, sync-view-dataset flag if needed) | `SaveAppSearchExDto` |
| `update_search_criteria` | **CRUD** Criterias: prefer `searchFieldId` + `changesJson` for update; create/delete via `op` or sibling tools (implementer chooses; capability locked) | Field list / single-field save |
| `update_search_view_columns` | Prefer `searchViewFieldId` + `changesJson`; CRUD for columns as needed in same slice | View field save |
| `update_dataset` | Prefer `dataSetId` + `changesJson`. Warn shared Dataset. Optional `syncViewColumns` after SQL — **ask_user** | `SaveOneAppDataSetEntityDto` (+ view sync if flagged) |
| `request_search_reload` | Host prompts Reload | Host tool |

Dispatch: SharedAgentEngine → BuiltInToolExecutor → `SearchViewConfigPlugin` (name TBD).

### 7.2 Do not (v1)

- Wholesale subscribe `platform-search` create tools as the update path.
- Put these tools on `platform-form-config` / Form Configurator.
- Unrestricted DDL / drop-table from this agent.
- Silent writes without `ask_user`.

### 7.3 System tool

Interactive: auto-injected **`ask_user`**. Required before every durable write; Dataset SQL changes need explicit impact confirmation.

### 7.4 Data model reminder

```
AppDataSet ←── AppSearch.DataSetId
AppSearchView ←── AppSearch.SearchViewId
AppSearchField     = criteria (filters)
AppSearchViewField = result columns
```

---

## 8. Write → confirm → Reload

```
User (Search View Config chat)
  → get_search_config
  → propose change
  → ask_user
  → update_search_basic_setting | update_search_criteria | update_search_view_columns | update_dataset
  → request_*_reload (host)
  → UI: Configuration changed — Reload current Search?
```

Align copy with Form Configurator reload confirm.

---

## 9. Seed & protection (when implementing)

1. Insert SkillSet **IF NOT EXISTS** (do not overwrite admin-edited prompt on re-run).
2. Seed library + BuiltIn tools.
3. Subscribe agent to that library only (minimal surface).
4. Protect AgentCode; exclude from `GetActiveAgents` / left menu / Chat Management.
5. Prompt: HostContext-only (or explicit ids in Test Run); inspect → ask_user → write → reload; refuse FormData / Form layout work.

---

## 10. Acceptance (design-level)

- Agent can rename Search via `update_search_basic_setting`.
- Agent can rebind SearchView / DataSet via basic setting (with ask_user).
- Agent can change criteria via `update_search_criteria` (by searchFieldId).
- Agent can change result columns via `update_search_view_columns` (by searchViewFieldId).
- Agent can change SQL/DataSource via `update_dataset` with shared-Dataset warning.
- No left-menu listing; Test Run in Agent Management allowed.
- Form Configurator remains Form-only for durable Form metadata.

---

## 11. Out of scope / deferred

- Creating new SearchView from chat (may follow).
- Entity create for criteria DDL (attach existing Entity first).
- Embedding-based field find (add `find_search_fields` if name ambiguity hurts).
- Merging runtime Search + Editor hosts in one prompt without HostContext discrimination.

---

## 12. Implementation order (later)

1. ~~Editor router~~ locked — MyApplicationEditor §11.
2. Constants + protection + menu exclusion.
3. Library + plugin tools (inspect → basic → criteria → view columns → dataset).
4. React host panel + HostContext + reload.
5. Seed SkillSet + SystemPrompt (§9.2-style scenarios for Search).
6. Dogfood on a real Search: rename, criteria tweak, column hide, SQL change + reload.

---

## 13. Glossary of prior questions (answered / explained)

| # | Topic | Resolution |
|---|--------|------------|
| Q1 AgentCode | Locked `app-search-view-config` |
| Q2 LibraryKey | **Locked B** — `platform-search-view-config` (§14) |
| Q3 Hosts | Locked **both** runtime + editor |
| Q4 Test Run without HostContext | **Locked:** require explicit ids |
| Q5 Criteria | Locked **full CRUD** |
| Q6 Dataset→columns sync | Locked **optional flag + ask_user** |
| Q7 Reload | Locked `request_search_reload` |
| Q8 list_searches subscribe | **Locked default Yes** — subscribe read-only `list_searches` / `list_available_searches` for Test Run discovery; no create/update from `platform-search` |
| Q9 Tab label | Locked **Configurator** |
| Q10 Supersede AppConfigurator §10 | **Done** — Search → `app-search-view-config` |

---

## 14. LibraryKey — locked

**Locked B:** mutate tools live in **`platform-search-view-config`**.  
Existing **`platform-search`** keeps create/list/run. This agent may later optionally subscribe read-only `list_searches`.

---

## 15. Editor topology — pointer only

**Canonical:** [MyApplicationEditor-AiAgent-Design.md](./MyApplicationEditor-AiAgent-Design.md) **§11**.

This Search View Config doc must **not** own App Editor multi-agent design. When Editor opens a Search/View → skill **`app-search-view-config`** (Configurator). App depth → `app-builder`; TX/Template → `app-configurator`.

---

## 16. Remaining before coding

1. ~~Editor router E1~~ done — MyApplicationEditor §11.  
2. Footnote AppConfigurator §10 (Search surface → this agent, not `app-configurator`).  
3. Implement `platform-search-view-config` tools + seed (runtime Search dock can host first).

---

## 17. Changelog

| Date | Change |
|------|--------|
| 2026-11-16 | Initial draft: Search View Config Agent; locked tool names; open questions. |
| 2026-11-16 | Locked hosts/CRUD/reload/tab; LibraryKey analysis; Editor chat analysis. |
| 2026-11-16 | Locked LibraryKey **`platform-search-view-config`**. Rewrote §15: App-level `app-builder` + object-matched specialists; agents/libraries/Form Config share analysis; E1a–d open. |
| 2026-11-16 | E1a–d + E3 locked: Template→`app-configurator`, Entity→`app-builder`; Form Builder popup shell-only v1; labels Assistant@app / Configurator@object; session per skill+object. |
| 2026-11-16 | **Doc split:** Editor topology moved to MyApplicationEditor §11; this §15 is pointer-only. AppConfigurator gains full Command / Data Transfer / Link Target tool inventory. |
