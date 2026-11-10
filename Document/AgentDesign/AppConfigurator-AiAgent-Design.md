# App Configurator Agent — Design

**Status:** Requirements locked (2026-11-10). **§0 gate passed** — managed `app-builder` accepted as clone source. Ready for implementation planning / coding.  
**Date:** 2026-11-10 (decisions amended same day)  
**Parent (data path):** [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md)  
**Related:** [MyApplicationEditor-AiAgent-Design.md](./MyApplicationEditor-AiAgent-Design.md) (Config shell embeds full `app-builder`), [Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md), [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md)  
**Supersedes:** `TransactionForm-AiAgent-ConfigDesign.md` (deleted) — that draft put config tools on Form Agent; this design does **not**.

---

## 0. Dependency / revisit gate

Agent Management **App Builder Agent** (`SkillKey = app-builder`) is the **source of truth** for configuration-oriented tools (transaction, unit, field, search, entity, schema, menu, etc.).

**§0 status (2026-11-10): PASSED.** Managed `app-builder` (V076 prompt/tools + plugin hardening) is accepted as healthy enough to clone-and-narrow into `app-configurator`. Hardcoded `/app-builder-agent` remains a short-term reference / fallback, not a blocker.

Before / during Configurator implementation, still sync back here as tools land:

1. Concrete seeded tool list for `platform-form-config` (wrappers vs read-only reuse).
2. Default subscriptions / exclusions for `app-configurator`.
3. System prompt text (clone-and-narrow from current managed `app-builder` prompt).
4. HostContext fields the working tools actually need.

---

## 1. Why

Users configure forms today via **Configuration** (admin-only): Field Setting, Edit Data Model / Unit, Design Layout, Commands, Reload, etc.

They also have:

- **App Form Agent** (Assistant tab) — operates **business FormData** on the open form (`platform-form-host`).
- **App Builder Agent** (`app-builder`) — builds / configures applications in Agent Management or the dedicated builder page.

We need a third, scoped entry: chat that edits **configuration of the currently open Form or Search**, without mixing that into Form Agent, and without opening the full greenfield App Builder workflow.

---

## 2. Goal / Non-goals

### Goal

When tenant **Enable Configuration** is on and the user is Admin, Form and Search show the existing **Configuration** button. In that same visibility condition, the right dock gains a **Configurator** tab beside **Assistant**. That tab runs **App Configurator Agent** (`app-configurator`), scoped to the open screen’s Transaction / Search context — analogous to how Form Agent is scoped to FormData.

The agent can inspect and change durable app config for that context: Transaction, Unit, Fields, layout (position, width, height, span, visibility), Formula, Commands, Linked Search / Link Target, related Search / Entity / light DB work needed for those fields — after confirmation.

### Non-goals (v1)

- Putting configuration tools on **App Form Agent** / Assistant.
- Replacing Configuration menus, Field Setting drawer, Edit Unit, Design Layout UI (parallel enhancement only).
- Greenfield app build from the form dock (`create_application`, `create_hierarchy_from_tables`, full schema propose/execute as primary path).
- Deleting the open transaction / application from Configurator.
- Listing Configurator in the left menu / Chat Management (form/search-hosted only; Agent Management Test Run allowed).
- Making Configurator and Form Agent share one chat session.

---

## 3. Locked decisions (2026-11-10)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Agent identity | **New skill** `app-configurator` (display name **App Configurator Agent**). Clone-and-narrow from managed `app-builder`. Not the same skill as `app-builder`; not a Child-Agent shell. |
| 2 | Visibility | See §3.1 / §4. Tab **hidden** (not greyed) when gate fails. May coexist with Form Assistant. **Separate** chat session from Form Agent. |
| 3 | Scope | Current Form/Search binding + closely related objects. No greenfield / cross-app create; no delete TX/app. |
| 4 | Existing UI | **Parallel** — keep Configuration menu and field gears. |
| 5 | Write + refresh | Durable writes require **`ask_user`**. After success, host prompts **Reload** (preserve unsaved FormData when possible), matching today’s “Configuration has been changed…” flow. |
| 6 | Tools | New scoped library **`platform-form-config`** (server forces ids from HostContext). Optional **read-only** reuse of existing tools. Do **not** wholesale subscribe `platform-transaction` / `platform-search`. AppConfigPack = Phase 3 escape hatch. |
| 7 | Capability breadth (v1) | **Scaffold the full road**: Field Setting, Unit, Layout, Formula, Commands, Linked Search / Link Target, Entity attach/create, Search criteria/columns, light DB when needed. Polish edge cases iteratively. |
| 8 | Search surface | **Runtime Search** (`AppSearch`) Configurator — **after** Form Configurator and My Application Editor Assistant are accepted. Same gates/patterns as Form; not a separate product. |
| 9 | Field “remove” | **Prefer soft hide / deactivate** as default; hard metadata delete (+ optional column drop) only via explicit `ask_user`. See §13. |
| 10 | AI-off + Configuration-on | **Yes** — show **Configurator-only** dock for admins (Form Assistant still gated by form AI flags). |

### 3.1 Cross-cutting AI visibility: `IsAiConfigured`

**Product rule:** every UI that depends on an LLM / AI agent is **invisible** unless `UserContext.IsAiConfigured === true`.

| Item | Rule |
|------|------|
| Meaning | Tenant AI Config has a usable LLM for the default provider (today: `AIConfigSettingBL.IsConfigured()` — non-empty API key; may later also require resolvable model). |
| Login | Already set at login / `GetUserContextBySessionId` on `UserContext.IsAiConfigured`. **Never** send the raw API key. |
| Naming | **Keep `IsAiConfigured`** — do **not** introduce `IsAiModelReady`. |
| UI usage | `visible = IsAiConfigured && <surface-specific gates>`. If false, **hide** AI docks / agent tabs / “Generate with AI” (not greyed stubs). |
| Applies to | Form Assistant, Form/Search Configurator, My Application Editor Assistant, Agent Chat / menu agents that call LLM, DbGenie AI, Skill Test Run needing LLM, productized App Builder entry, etc. |
| Rollout | **Apply this gate last** (after Form Configurator + Editor Assistant work). Until then, new docks may follow surface-specific gates only; then retrofit `IsAiConfigured` across all AI surfaces in one pass. |

Configurator full gate (when gate work lands):

```
IsAiConfigured
AND EnableConfigurationMode
AND isAdminUser
AND NOT draft
```

Form Assistant (when gate work lands):

```
IsAiConfigured
AND IsEnableTransactionAiAgent
AND (other existing form AI flags)
```

---

## 4. Trigger & UX

### Visibility gate

Configuration button (unchanged product rule, still no LLM required):

```
EnableConfigurationMode === true
AND isAdminUser
AND NOT draft
```

Configurator tab / AI config dock (LLM required; full expression after gate pass):

```
IsAiConfigured
AND EnableConfigurationMode
AND isAdminUser
AND NOT draft
```

Sources today:

- Setting: `EnableConfigurationMode` (`adminPermissionHelper.isEnableConfigurationModeForUser`)
- Admin: `isAdminUserFromContext`
- AI ready: `UserContext.IsAiConfigured` (§3.1) — retrofit last
- UI: `FormMainMenus.renderConfigurationMenu`, List Edit, `AppSearch` (runtime Search = last slice)

When the Configurator gate passes:

1. Form / Search top-left **Configuration** button remains as today (still follows Configuration-only gate).
2. Right dock shows **Configurator** next to **Assistant** when each tab’s own gate passes.
3. When Configurator gate fails, Configurator tab is absent.
4. When Configuration is on but Form AI is off: open dock with **Configurator only** (decision #10); after gate pass also requires `IsAiConfigured`.

### Dock tabs (conceptual)

| Tab | Agent | Concern |
|-----|--------|---------|
| Conversation | (messages) | Human chat on the form, if enabled |
| Assistant | `app-transaction-form-agent` | FormData / actions on open form |
| Configurator | `app-configurator` | Durable config of open Form/Search context |

**Implement (Form first):** Admin + `EnableConfigurationMode` → ensure a right dock can open with at least Configurator; Assistant still gated by existing form AI flags. Fold in `IsAiConfigured` in the final gate pass.

### Session

- Separate `sessionId` / conversation from Form Agent.
- Do not list in left menu; deep-link without host shows “runs inside Form/Search with Configuration enabled” (same pattern as Form Agent).
- Reserve / protect `AgentCode` = `app-configurator` (no rename/delete; block collisions) like Form Agent.

---

## 5. Agent roles (split of responsibility)

```
┌─────────────────────────────────────────────────────────────┐
│ Form / Search runtime                                       │
│  Configuration button (admin + EnableConfigurationMode)     │
│  ┌──────────────┐  ┌────────────────┐                       │
│  │ Assistant    │  │ Configurator   │                       │
│  │ Form Agent   │  │ app-configurator│                      │
│  │ FormData     │  │ TX/Unit/Field/ │                       │
│  │ host tools   │  │ Layout/Formula │                       │
│  └──────────────┘  └────────────────┘                       │
└─────────────────────────────────────────────────────────────┘

Side menu / Agent Management / My Application Editor Assistant:
  app-builder  → full App Builder (greenfield + broad config); §0 passed — clone source for app-configurator
```

| Agent | SkillKey | Host | Writes |
|-------|----------|------|--------|
| App Form Agent | `app-transaction-form-agent` | Form dock Assistant | In-memory FormData; save via commands + confirm |
| App Configurator | `app-configurator` | Form/Search dock Configurator | DB metadata / layout / commands / etc. |
| App Builder | `app-builder` | Agent Management + hardcoded page | Full builder surface |

Configurator prompt must state explicitly: do not fill business fields; send the user to Assistant for FormData.

---

## 6. HostContext

Each Configurator turn sends **config-oriented** HostContext (not FormData values). Inject into that turn’s system section only (same engine pattern as Form Agent).

### Form host (sketch)

```json
{
  "surface": "form",
  "mode": "configurator",
  "transactionId": 1204,
  "transactionName": "Sales Order",
  "saasApplicationId": 12,
  "formKind": "master",
  "units": [
    {
      "unitId": 4401,
      "name": "Header",
      "table": "SO_Header",
      "fields": [
        { "fieldId": 9001, "name": "Status", "dbName": "Status", "controlType": "DropDown", "entityId": 55 }
      ]
    }
  ],
  "layoutHints": { "formId": 301, "hasFlexLayout": true },
  "commands": [{ "id": 1, "name": "Save" }],
  "linkedSearches": [{ "id": 88, "name": "Customer Lookup", "unitId": 4401 }],
  "linkTargets": []
}
```

### Search host (sketch)

```json
{
  "surface": "search",
  "mode": "configurator",
  "searchId": 501,
  "searchName": "Sales Order Search",
  "saasApplicationId": 12,
  "boundTransactionId": 1204
}
```

**Server rule:** mutate tools ignore client-supplied `transactionId` / `searchId` when HostContext is present; use host ids. Mismatched ids → `{ ok: false, error }`.

Catalog stays compact (ids, names, control types, layout item ids). Full DDL / formula text loaded via inspect tools on demand.

---

## 7. Tools

### 7.1 New library `platform-form-config`

`ToolType = BuiltIn`. Thin wrappers over existing BL (`AppTransactionBL`, form flex layout BL, command/linked-search BL, entity BL, etc.). Exact method list is **TBD after `app-builder` repair (§0)** — below is the intended capability map, not a frozen seed.

| Capability area | Example tool names (working titles) | Notes |
|-----------------|-------------------------------------|--------|
| Inspect | `get_current_transaction_config`, `get_unit_fields`, `get_field_setting`, `get_form_layout`, `get_commands`, `get_linked_searches` | Read current context |
| Transaction / Unit / Field | `update_field_setting`, `update_unit`, `add_field`, `remove_field` | Align with Field Setting + Edit Unit |
| Layout | `update_layout_item`, `move_layout_item`, `set_layout_span` | Position, width, height, visibility |
| Formula | `get_formula`, `set_formula` | HITL |
| Commands | `list_commands`, `upsert_command`, `delete_command` | HITL on delete |
| Linked Search / Link Target | `upsert_linked_search`, `upsert_link_target` | Wire existing or create related search under policy |
| Entity | `attach_entity_to_field`, `create_entity_for_field` | Prefer attach; create needs HITL |
| Search (when surface=search or linked) | `get_search_config`, `update_search_criteria`, `update_search_columns` | Scoped |
| Light DB | optional wrap of alter/add column with strong HITL | Prefer existing schema tools only after builder fix |
| Host | `request_form_reload` | Signal UI to reload metadata |

Dispatch path unchanged: `SharedAgentEngine` → `AppAgentToolEngine` → `BuiltInToolExecutor` → plugin methods (new `FormConfigPlugin` or split plugins — decide at implement time).

### 7.2 Do not wholesale subscribe

- `platform-transaction` (create/delete/greenfield)
- `platform-search` create-anywhere tools
- Destructive `platform-database` DDL as default write path

### 7.3 Optional read-only reuse

After `app-builder` tools are trustworthy, Configurator may subscribe selected **read** tools (e.g. `get_transaction_details`, `list_entity_data_sources`, `get_table_schema`) with exclusions on create/delete.

### 7.4 Phase 3

`platform-app-config-pack` validate / preview / execute for bulk/complex packs — not the daily “make Status a dropdown” path.

### 7.5 System tool

Interactive agents keep auto-injected **`ask_user`**. All durable config writes require it in v1.

---

## 8. Write → confirm → reload

```
User (Configurator chat)
  → inspect tools (optional)
  → ask_user (proposed config change)
  → platform-form-config mutate (DB write)
  → tool result { ok, changed: [...] }
  → host_tool or client signal: request reload
  → UI: "Configuration has been changed. Reload current form/search?"
  → Reload metadata; preserve FormData when possible
```

Align messaging with `FormMainMenus` refresh-after-config behavior.

Destructive or schema-changing steps: always HITL; prefer explicit deny in prompt for drop-table class operations.

---

## 9. Seed & protection (when implementing)

Upgrade script (after §0):

1. Insert skill `app-configurator` if not exists (do not overwrite admin-edited prompt on re-run).
2. Seed `platform-form-config` library + tools.
3. Subscribe libraries; apply exclusions.
4. Block rename/delete of `app-configurator` (parity with Form Agent).
5. Exclude from `GetActiveAgents` / left menu / Chat Management.
6. Do **not** add to General Agent allowlist by default.

Prompt starting point: copy fixed `app-builder` prompt, then rewrite workflow to:

- Current HostContext only
- Config inspect → propose → `ask_user` → write → reload
- Refuse FormData edits; refuse greenfield create/delete

Link from Transaction Graphic Editor / Search editor: “Configure Configurator Agent” → Agent Management `app-configurator` (optional; mirror Form Agent link).

---

## 10. Search surface (runtime Search — last)

**Scope:** runtime **`AppSearch`** (end-user / admin Search screen with Configuration button) — **not** My Application Editor “Report & View” section (that uses Editor Assistant / `app-builder`).

**Schedule:** after Form Configurator **and** My Application Editor Assistant are accepted.

- Configurator tab on Search right dock (or equivalent panel — discover UI hook then).
- HostContext `surface: "search"`.
- Tools focus on search criteria/columns/views and bound transaction linkage; full TX layout edits may open related TX context or tell user to open the form.
- Gate: same as Form Configurator (`IsAiConfigured` + Configuration rule) once the final gate pass is done.

---

## 11. Out of scope / deferred

- Replacing graphic layout designer for pixel-perfect drag inside chat.
- Headless FormData writes from Configurator.
- Per-transaction agent binding table.
- Auto-sync of this doc’s tool table from DB (manual update as tools land).
- Retiring hardcoded `/app-builder-agent` (tracked with My Application Editor dock; not a Configurator blocker).

---

## 12. Implementation order (locked)

1. **Form Configurator** — inventory builder tools → seed `app-configurator` + `platform-form-config` → React Form dock tab + HostContext + HITL/reload + prompt clone-and-narrow. Configurator-only dock when Form AI off. Accept Form slice first.
2. **My Application Editor Assistant** — shell dock, skill `app-builder`, HostContext, session per `saasApplicationId`, refresh + open-editor host tools. Accept Editor slice.
3. **Runtime Search Configurator** — same `app-configurator` on `AppSearch`; HostContext `surface: "search"`. Only after 1–2 accepted.
4. **`IsAiConfigured` gate pass (last)** — retrofit all AI-related windows to require `UserContext.IsAiConfigured` as the first visibility condition (Form Assistant, Form/Search Configurator, Editor Assistant, Agent Chat, DbGenie AI, etc.).
5. (Later) Phase 3 AppConfigPack; optional read-only library subscriptions; polish.

---

## 13. Field remove policy + remaining implement notes

### Field remove (locked recommendation)

| Action | When | HITL |
|--------|------|------|
| **Soft** — hide / deactivate on form (visibility / layout off; keep Field + column) | Default for “remove this field from the form” | `ask_user` once |
| **Hard** — delete field metadata from transaction | User explicitly asks to delete the field definition | `ask_user` with strong wording |
| **DDL** — drop / alter physical column | Only when user explicitly asks for schema change | Separate `ask_user`; prefer light-DB tools already used by builder |

Do not make hard delete the default synonym of “remove”.

### Still finalize at implement (non-blocking)

1. Exact `platform-form-config` method list / ParameterSchemaJson (from builder inventory).
2. How much layout mutate goes through flex-layout BL vs telling user to open Design Layout for pixel work.
3. Search dock host component parity with Form `FormSidePanel`.
4. Exact Search dock host component when runtime Search slice starts.

---

## 14. Changelog

| Date | Change |
|------|--------|
| 2026-11-10 | New design: separate `app-configurator`; Configuration-gate UX; scoped `platform-form-config`; parallel UI; HITL + reload. Explicit dependency on repairing managed `app-builder` before implement / before finalizing tool+prompt sections. Supersedes deleted Form-Agent-config draft. |
| 2026-11-10 | §0 **passed**. Locked: Configurator-only dock; full v1 capability scaffold; soft-hide default for field remove; **`IsAiConfigured`** (no `IsAiModelReady`); order = Form Config → Editor Assistant → runtime Search → gate retrofit last. |
| 2026-11-10 | **Form slice started:** V077 seed `app-configurator` + `platform-form-config`; `FormConfigPlugin`; Form/ListEdit Configurator tab; HostContext mode=configurator; `request_form_reload`. Layout/formula/command mutate tools still to expand. |
