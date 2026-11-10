# App Configurator Agent — Design

**Status:** Requirements locked (2026-11-10). Implementation **blocked** until Agent Management `app-builder` is repaired and its prompt / tools / BuiltIn definitions are trusted enough to clone from.  
**Date:** 2026-11-10  
**Parent (data path):** [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md)  
**Related:** [MyApplicationEditor-AiAgent-Design.md](./MyApplicationEditor-AiAgent-Design.md) (Config shell embeds full `app-builder`), [Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md), [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md)  
**Supersedes:** `TransactionForm-AiAgent-ConfigDesign.md` (deleted) — that draft put config tools on Form Agent; this design does **not**.

---

## 0. Dependency / revisit gate (read first)

Agent Management already has **App Builder Agent** (`SkillKey = app-builder`). It is the intended **source of truth** for configuration-oriented tools (transaction, unit, field, search, entity, schema, menu, etc.).

**Current reality:** that managed `app-builder` still fails often and behaves worse than the **hardcoded** App Builder Agent UI (`/app-builder-agent`). Until it is fixed, this Configurator design must stay a **draft target**, not an implementation checklist to execute blindly.

Before implementing Configurator (or when revisiting this doc), complete and then sync back here:

1. Repair Agent Management **`app-builder`**: system prompt, selected libraries/tools, exclusions, limits.
2. Stabilize each **configuration BuiltIn** it uses (schema, descriptions, `ToolConfig` TypeName/MethodName, parameter contracts, HITL).
3. Re-run end-to-end App Builder flows in Agent Management Test Run until quality is acceptable vs hardcoded.
4. **Return to this document** and update:
   - Seeded tool list for `platform-form-config` (which wrappers exist vs reuse read-only tools)
   - Default subscriptions / exclusions for `app-configurator`
   - System prompt text (clone-and-narrow from the fixed `app-builder` prompt)
   - Any HostContext fields the working tools actually need

Do **not** treat today’s broken `app-builder` prompt/tool set as the final Configurator surface.

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
- Implementing before `app-builder` (Agent Management) is repaired (§0).

---

## 3. Locked decisions (2026-11-10)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Agent identity | **New skill** `app-configurator` (display name **App Configurator Agent**). Clone-and-narrow from managed `app-builder` once that agent is healthy. Not the same skill as `app-builder`; not a Child-Agent shell. |
| 2 | Visibility | Same gate as Configuration button. Tab **hidden** (not greyed) when gate fails. May coexist with Assistant. **Separate** chat session from Form Agent. |
| 3 | Scope | Current Form/Search binding + closely related objects. No greenfield / cross-app create; no delete TX/app. |
| 4 | Existing UI | **Parallel** — keep Configuration menu and field gears. |
| 5 | Write + refresh | Durable writes require **`ask_user`**. After success, host prompts **Reload** (preserve unsaved FormData when possible), matching today’s “Configuration has been changed…” flow. |
| 6 | Tools | New scoped library **`platform-form-config`** (server forces ids from HostContext). Optional **read-only** reuse of existing tools. Do **not** wholesale subscribe `platform-transaction` / `platform-search`. AppConfigPack = Phase 3 escape hatch. |

---

## 4. Trigger & UX

### Visibility gate (existing platform rule)

Reuse the same gate as the Configuration button:

```
EnableConfigurationMode === true   // tenant Application Setting (DictAppSetup)
AND isAdminUser                    // adminPermissionHelper / Angular parity
AND NOT draft                      // form draft hides Configuration today
```

Sources today:

- Setting: `EnableConfigurationMode` (`adminPermissionHelper.isEnableConfigurationModeForUser`)
- Admin: `isAdminUserFromContext`
- UI: `FormMainMenus.renderConfigurationMenu`, List Edit, `AppSearch`

When the gate passes:

1. Form / Search top-left **Configuration** button remains as today.
2. Right dock shows **Configurator** next to **Assistant** (when AI / side panel is present).
3. When the gate fails, Configurator tab is absent — same as no Configuration button.

### Dock tabs (conceptual)

| Tab | Agent | Concern |
|-----|--------|---------|
| Conversation | (messages) | Human chat on the form, if enabled |
| Assistant | `app-transaction-form-agent` | FormData / actions on open form |
| Configurator | `app-configurator` | Durable config of open Form/Search context |

Configurator does not require Transaction Graphic Editor “Enable AI Agent” if product later wants config-only admins; **v1 recommendation:** show Configurator whenever the Configuration gate passes **and** the form/search already hosts the right dock (AI and/or Communication). Exact coupling to `IsEnableTransactionAiAgent` is an implementation detail to confirm at build time — prefer: if dock exists for Assistant, add Configurator under the same gate; if AI is off but Configuration is on, still show a dock with Configurator-only (admin config without data agent).

**Recommendation to implement:** Admin + `EnableConfigurationMode` → ensure a right dock can open with at least Configurator; Assistant still gated by existing AI flags.

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

Side menu / Agent Management:
  app-builder  → full App Builder (greenfield + broad config)
                 MUST be fixed before cloning into app-configurator (§0)
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

## 10. Search surface

v1 includes Search when Configuration is visible:

- Configurator tab on Search right dock (or equivalent panel if Search AI dock differs — confirm UI hook at implement time).
- HostContext `surface: "search"`.
- Tools focus on search criteria/columns/views and bound transaction linkage; full TX layout edits may open related TX context or tell user to open the form.

Exact Search dock component parity with Form is an implementation discovery item; behavior gate is the same Configuration visibility rule.

---

## 11. Out of scope / deferred

- Fixing hardcoded vs managed App Builder parity (prerequisite project, tracked outside this file except §0).
- Replacing graphic layout designer for pixel-perfect drag.
- Headless FormData writes from Configurator.
- Per-transaction agent binding table.
- Auto-sync of this doc’s tool table from DB (manual update after builder fix).

---

## 12. Implementation order (after §0 gate)

1. Stabilize Agent Management `app-builder` (prompt, tools, BuiltIn contracts) and update §7 with concrete tool names/schemas.
2. React: Configurator tab under Configuration visibility gate; separate session; HostContext builder for form (+ search).
3. Seed `app-configurator` + `platform-form-config` + protections.
4. Plugin BL wrappers + HITL + reload signal.
5. Prompt polish; Agent Management “Configure Configurator” link.
6. (Later) Phase 3 AppConfigPack; optional read-only library subscriptions.

---

## 13. Open points (resolve at implement / after builder fix)

1. If AI Agent is off but Configuration is on: always show Configurator-only dock? (**Lean yes** for admins.)
2. Final list of `platform-form-config` methods after inventory of working `app-builder` tools.
3. How much layout mutate goes through flex-layout BL vs reopening Design Layout UI.
4. Search dock host component and whether Search shares Form’s `FormSidePanel` patterns.
5. Whether soft-delete / deactivate of fields is allowed vs hard remove in v1.

---

## 14. Changelog

| Date | Change |
|------|--------|
| 2026-11-10 | New design: separate `app-configurator`; Configuration-gate UX; scoped `platform-form-config`; parallel UI; HITL + reload. Explicit dependency on repairing managed `app-builder` before implement / before finalizing tool+prompt sections. Supersedes deleted Form-Agent-config draft. |
