# App Configurator Agent — Design

**Status:** Form slice in progress. **§0 passed.** Editor hosts TX/Template drill-in on this skill ([MyApplicationEditor §11](./MyApplicationEditor-AiAgent-Design.md)). Runtime **Search** Configurator superseded by [SearchViewConfig-AiAgent-Design.md](./SearchViewConfig-AiAgent-Design.md) (`app-search-view-config`).  
**Date:** 2026-11-10; amended 2026-11-16  
**Parent (data path):** [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md)  
**Related:** [MyApplicationEditor-AiAgent-Design.md](./MyApplicationEditor-AiAgent-Design.md) §11, [SearchViewConfig-AiAgent-Design.md](./SearchViewConfig-AiAgent-Design.md), [Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md), [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [AppConfigurator-Formula-FormLayout-Tools.md](./AppConfigurator-Formula-FormLayout-Tools.md)  
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

When tenant **Enable Configuration** is on and the user is Admin, Form (and List Edit) show the existing **Configuration** button. In that same visibility condition, the right dock gains a **Configurator** tab beside **Assistant**. That tab runs **App Configurator Agent** (`app-configurator`), scoped to the open **Transaction** — analogous to how Form Agent is scoped to FormData. Same skill also runs when My Application Editor drills into a TX / TX Template.

The agent can inspect and change durable TX config aligned with Application Form Builder: Unit / Fields, Form Layout, Formula, **Operation Task (Commands) CRUD**, **Internal Data Transfer**, **Linked Search**, **Link Target**, Data Load, Conditional Lock/Hide, Entity attach/create, light DB when needed — after confirmation. **Search** criteria/columns/dataset updates belong to **`app-search-view-config`**, not this skill.

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
| 7 | Capability breadth | **Full Form Builder road in `platform-form-config`**: Field/Unit/Layout/Formula + **Commands CRUD** + **Data Transfer** + **Linked Search** + **Link Target** + Data Load / Condition Lock (+ Report/Navigation later). Implement tools for every area that needs agent mutate — see §7.1. Polish iteratively; do not leave Command/DT/Link as “UI only”. |
| 8 | Search surface | **Superseded (2026-11-16):** runtime + Editor Search Configurator = **`app-search-view-config`** ([SearchViewConfig](./SearchViewConfig-AiAgent-Design.md)). This skill stays **Form/TX/(Template) only**. |
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

### 7.1 Library `platform-form-config` — tool inventory (required)

`ToolType = BuiltIn`. Thin wrappers over existing BL (`AppTransactionBL`, `AppTransactionCommandBL`, DataTransfer BL, LinkTarget / LinkedSearch BL, flex layout, entity, …). HostContext forces `transactionId` (and template ids when Template open). All durable writes: preview → `ask_user` → `confirmed=true` → `request_form_reload`.

**Status column:** Done = seeded/plugin today; Need = must add for full Configurator (user 2026-11-16: Command / Data Transfer / Link Target all required).

#### A. Inspect / catalog

| Tool | Status | Notes |
|------|--------|-------|
| `get_current_transaction_config` | Done | Units + field summaries |
| `get_unit_fields` | Done | Prefer `unitId` |
| `find_fields` | Done | Cross-unit name search |
| `get_field_setting` | Done / expand | Full field props when needed |

#### B. Unit / Field

| Tool | Status | Notes |
|------|--------|-------|
| `update_field_setting` | Done | Label, controlType, defaultValue (≠ formula), flags… |
| `set_field_visibility` | Done | Soft hide |
| `set_field_entity` | Done | Attach entity to field |
| `create_entity_for_field` | Done | HITL create + wire |
| `update_unit` | Need | Unit display/options (align Unit Editor) |
| `add_field` / `remove_field` | Need | Soft-hide default for remove (§13) |

#### C. Form Layout

| Tool | Status | Notes |
|------|--------|-------|
| `get_form_layout` | Done | |
| `design_form_layout` | Done | patch / overwrite modes |

#### D. Formula (Calculation Validation Flow)

| Tool | Status | Notes |
|------|--------|-------|
| `get_unit_formulas` | Done | |
| `update_unit_formula` | Done | add/update/delete; op types 1–5 |

#### E. Operation Task — Transaction Command CRUD (**required**)

Maps Application Form Builder **Operation Task** (`TransactionCommandMgt` / `CommandEditor`). BL: `AppTransactionCommandBL.SaveOneTransactionCommandActionList` et al.

| Tool | Status | Notes |
|------|--------|-------|
| `get_commands` | Done | Compact: id, name, actionType |
| `get_command_detail` | Done (V084) | Full one command + ActionAttribute |
| `upsert_command` | Done (V084) | Create/update shell + optionsJson patches |
| `update_command_options` | Folded into upsert_command.optionsJson | Same tool |
| `delete_command` | Done (V084) | HITL |
| `reorder_commands` | Later | |

Do **not** execute runtime commands from Configurator (`run_command` stays Form Agent / `platform-form-host`).

#### F. Internal Data Transfer (**required**)

Maps **Internal Data Transfer Data Model** (`TransactionDataTransferSettingMgt`). BL: `AppTransactionDataTransferSettingBL`.

| Tool | Status | Notes |
|------|--------|-------|
| `get_data_transfers` | Done (V084) | List DT settings on open TX |
| `get_data_transfer_detail` | Done | Header + mappings + type hints (API params / request vs response) |
| `upsert_data_transfer` | Done | Header + **full-replace** `mappingsJson` (all types 1–4); `[]` clears; omit leaves mappings |
| `upsert_data_transfer_mappings` | Folded into upsert_data_transfer.mappingsJson | |
| `delete_data_transfer` | Done (V084) | HITL |

#### G. Linked Search (**required**)

Maps unit **Linked Search** UI. BL: `AppTransactionUnitLinkedSearchBL`. `get_linked_searches` exists today.

| Tool | Status | Notes |
|------|--------|-------|
| `get_linked_searches` | Done | List on units of open TX |
| `upsert_linked_search` | Done (V084) | Bind/create linked search on unit |
| `delete_linked_search` | Done (V084) | HITL |

Creating a brand-new Search elsewhere → `app-builder` / existing `create_search`; Configurator **binds** to open TX unit.

#### H. Link Target (**required**) — see also §7.6 taxonomy

Same table **`AppFormLinkTarget`** as Search View menus. Source = **TransactionUnit** (`EmAppLinkTargetSourceType.TransactionUnit = 3`).  
UI: `TransactionUnitLinkTargetEditor`. BL: `LinkTragetBL.SaveOneAppFormLinkTargetList`.

| Tool | Status | Notes |
|------|--------|-------|
| `get_link_targets` | Done (V084) | Prefer `unitId`; optional `usageType` |
| `get_link_target_detail` | Done (V084) | Full one row |
| `upsert_link_target` | Done (V084) | usageType default 101; ActionType Create/Edit/Delete/Preview/… |
| `delete_link_target` | Done (V084) | HITL by `linkTargetId` |

**Not the same as Linked Search (G):** Linked Search = `AppTransactionUnitLinkedSearch` (inline LOV/search on a field). Link Target = navigation **menus** (open Form / delete / preview…).

#### I. Data Load / Conditional Lock (Form Builder sections — include tools)

| Tool | Status | Notes |
|------|--------|-------|
| `get_data_loads` | Done | List on open TX; optional `unitId` |
| `get_data_load_detail` | Done | Header + condition/field mappings + dataset columns |
| `list_available_datasets` | Done | Pick `dataSetId` |
| `get_dataset_columns` | Done | Columns for mapping `dbColumnName` |
| `upsert_data_load` | Done | Header + `conditionMappingsJson` / `fieldMappingsJson` full-replace per side; `[]` clears |
| `delete_data_load` | Done | HITL |
| `get_message_templates` | Done | TX Message Templates (AppMessage scope 99) |
| `get_message_template_detail` | Done | |
| `upsert_message_template` | Done | Then bind via upsert_command 74 `messageTemplateId` |
| `delete_message_template` | Done | HITL |
| `get_condition_locks` | Need | Conditional lock/hide |
| `upsert_condition_lock` | Need | |
| `delete_condition_lock` | Need | HITL |

#### J. Later (same library, after E–I)

| Area | Tools (titles) | Notes |
|------|----------------|-------|
| Navigation | `get_transaction_navigation`, `upsert_…` | Form Builder Navigation |
| Report attach | `get_transaction_reports`, `upsert_…` | Bind reports to TX |
| Print form | inspect / light mutate | Prefer open designer host tool for pixels |
| Workflow | defer unless already on builder libs | |

#### K. Host

| Tool | Status | Notes |
|------|--------|-------|
| `request_form_reload` | Done | After every successful durable write |

#### L. Explicitly **not** in this library

| Concern | Where |
|---------|--------|
| Search basic / criteria / view columns / dataset update | **`platform-search-view-config`** / `app-search-view-config` |
| Greenfield create TX / Search / App | **`app-builder`** |
| FormData / run_command | **`platform-form-host`** / Form Agent |

Dispatch: `SharedAgentEngine` → `AppAgentToolEngine` → `BuiltInToolExecutor` → `FormConfigPlugin` (and/or split Command/DT plugins sharing HostContext helpers).

### 7.6 Link Target taxonomy (TX Unit + Search View — shared model)

One entity: **`AppFormLinkTarget`**. Owner is either a **Transaction Unit** or a **Search View**.

| Owner | `EmAppLinkTargetSourceType` | FK | Config agent |
|-------|----------------------------|-----|--------------|
| Transaction Unit | `TransactionUnit = 3` | `TransactionUnitId` | **`app-configurator`** / `platform-form-config` |
| Search View | `SearchView = 2` | `SearchViewId` | **`app-search-view-config`** / `platform-search-view-config` |

**`EmAppLinkTargetUsageType` (what you “link to”):**

| Value | Name | Typical owner | Destination fields |
|------:|------|---------------|--------------------|
| 1 | `SearchViewLinkToForm` | Search View | `LinkTargetTransactionId` (+ optional DataTransfer, field maps) |
| 2 | `SearchViewLinkToFormGroup` | Search View | `LinkTargetTransactionGroupId` |
| 3 | `SearchViewLinkToSearch` | Search View | `LinkTargetSearchId` (+ view/criteria maps) |
| 4 | `SearchViewLinkToReport` | Search View | report target fields |
| 5 | `SearchViewLinkSystemDefinedPage` | Search View (also TX “any page”) | `LinkTargetUrlOrRouteCode` / system page enum |
| 101 | `TransactionUnitLinkToForm` | Transaction Unit | `LinkTargetTransactionId` (+ source/target column maps) |

**`ActionType` (what the menu does)** — shared `EmAppLinkTargetActionType` (Search subset vs TX Unit subset in UI):

| Value | Meaning |
|------:|---------|
| 1 | Edit Form |
| 2 | Create Blank Form |
| 3 | Delete Form |
| 5 | Preview Form |
| 13 | Create From Existing / using current data |
| 17 | Execute Transaction Command (Search; needs command id in OtherSettings) |
| 6,11,12,14–16 | External / user / popup / copy-cut-paste (calendar etc.) |

**Also on Search View (related, not AppFormLinkTarget):** `AppViewLinkedSeaechOrUrl` — inline linked Search/URL cards (`AppViewLinkedSeaechOrUrlBL`). Separate tools on Search View Config: `get_view_linked_search_or_url` / `upsert_…` / `delete_…` (do not conflate with Link Target menus).

**Tool logic rules (both agents):**

1. HostContext forces owner: TX → unit must belong to open `transactionId`; Search → `searchViewId` must belong to open `searchId` (or explicit Test Run ids).
2. `upsert_*`: validate `usageType` vs owner (101 on TX; 1–5 on Search); require destination id for that usage; preview → `ask_user` → `confirmed=true`.
3. Column maps: prefer field/view-column **ids** (`SourceViewColumnId*`, `TargetSearchFieldId*`, `SourceColumn*` / `TargetColumn*` for TX).
4. After write: TX → `request_form_reload`; Search → `request_search_reload`.
5. Shared C# helper: `AppFormLinkTargetConfigService` (sourceType + sourceId + CRUD) used by both plugins.

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

## 10. Search surface — superseded

**2026-11-16:** Runtime Search + Editor Search drill-in use **`app-search-view-config`** / `platform-search-view-config` — see [SearchViewConfig-AiAgent-Design.md](./SearchViewConfig-AiAgent-Design.md).  
**Do not** put Search criteria/columns/dataset mutate tools on `app-configurator`. Linked Search **binding on a TX unit** remains here (§7.1 G).

---

## 11. Out of scope / deferred

- Replacing graphic layout designer for pixel-perfect drag inside chat.
- Headless FormData writes from Configurator.
- Per-transaction agent binding table.
- Auto-sync of this doc’s tool table from DB (manual update as tools land).
- Retiring hardcoded `/app-builder-agent` (tracked with My Application Editor dock; not a Configurator blocker).

---

## 12. Implementation order (amended 2026-11-16)

1. **Form Configurator expand** — finish §7.1 **Need** tools especially **E Commands**, **F Data Transfer**, **G Linked Search mutate**, **H Link Target** (then Data Load / Condition Lock). Prompt sequences + seed migrations.
2. **My Application Editor multi-agent dock** — §11 router: app → `app-builder`; TX/Template → `app-configurator`; Search → `app-search-view-config`.
3. **Search View Config Agent** — `platform-search-view-config` + runtime Search Configurator dock (parallelizable with 1 after patterns locked).
4. **`IsAiConfigured` gate pass (last)**.
5. (Later) Phase 3 AppConfigPack; Navigation/Report tools; polish.

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

1. ParameterSchemaJson for each §7.1 **Need** tool (Command options shapes are the largest).
2. How much layout mutate stays in chat vs “open Design Layout”.
3. Command `update_command_options` — one blob vs many typed tools per action type (prefer one upsert + typed common fields + optionsJson for remainder).

---

## 14. Changelog

| Date | Change |
|------|--------|
| 2026-11-10 | New design: separate `app-configurator`; Configuration-gate UX; scoped `platform-form-config`; parallel UI; HITL + reload. Explicit dependency on repairing managed `app-builder` before implement / before finalizing tool+prompt sections. Supersedes deleted Form-Agent-config draft. |
| 2026-11-10 | §0 **passed**. Locked: Configurator-only dock; full v1 capability scaffold; soft-hide default for field remove; **`IsAiConfigured`** (no `IsAiModelReady`); order = Form Config → Editor Assistant → runtime Search → gate retrofit last. |
| 2026-11-10 | **Form slice started:** V077 seed `app-configurator` + `platform-form-config`; `FormConfigPlugin`; Form/ListEdit Configurator tab; HostContext mode=configurator; `request_form_reload`. Layout/formula/command mutate tools still to expand. |
| 2026-11-16 | §7.1 full tool inventory: **Command CRUD**, **Data Transfer**, **Linked Search mutate**, **Link Target**, Data Load, Condition Lock marked required. §10 Search → `app-search-view-config`. Editor TX/Template drill-in via MyApplicationEditor §11. |
