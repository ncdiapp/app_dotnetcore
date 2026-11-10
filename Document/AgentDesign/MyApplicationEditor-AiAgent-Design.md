# My Application Editor — AI Agent Design

**Status:** Direction agreed (2026-11-10). Implementation **blocked** until Agent Management `app-builder` is repaired (same gate as Form Configurator).  
**Date:** 2026-11-10  
**Host UI:** `AppReact/src/components/admin/MyApplicationEditor.tsx` and section children under `MyApplicationEditor/` (sidebar: Application Properties, Data Model Design, Entity List Of Value, Report & View, Import Config, …). Tab title pattern: `Config: {ApplicationName}`.  
**Related:**  
- [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) — Form/Search **runtime** narrow config (`app-configurator`)  
- [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md) — Form **data** agent  
- [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md)

---

## 0. Dependency / revisit gate

This host embeds the **same** managed skill **`app-builder`** that Agent Management already defines. It does **not** introduce a fourth specialist skill.

Until managed `app-builder` (prompt, selected tools, BuiltIn contracts) matches or approaches the hardcoded `/app-builder-agent` experience:

- Do not ship the editor dock as the primary builder UX.
- After `app-builder` is fixed, return here and lock: HostContext fields, section→tool guidance in prompt, refresh hooks per section, any tool exclusions specific to this host.

Also sync [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) when cloning the narrowed Form/Search agent from the healthy builder.

---

## 1. Why

**My Application Editor** is the application-level configuration shell. Admins create and maintain Form Models, List Edit models, entities, searches/views, DB design, workflow, and import packs for one `SaaSApplicationId`.

Today AI entry points for building are fragmented:

- Hardcoded **App Builder Agent** page (`/app-builder-agent`) — stronger behavior, not the managed skill.
- Agent Management **`app-builder`** — correct product model, currently weaker / error-prone.
- Form runtime will get **`app-configurator`** (narrow, current TX/Search only) and **Form Agent** (FormData only).

The Configuration shell needs an in-place agent that already knows **which application** is open and **which section / row** the admin is looking at — without leaving `Config: …` for a blank builder chat.

---

## 2. Goal / Non-goals

### Goal

In My Application Editor, provide a right-side AI dock that runs **App Builder Agent** (`SkillKey = app-builder`) with HostContext bound to the open application (and current section / selection). The agent can list, create, and modify application configuration objects for **that** package — the same capability surface as App Builder, but contextualized.

### Non-goals

- Using **Form Agent** / `platform-form-host` here (no FormData).
- Using **`app-configurator`** as the default agent on this page (too narrow; forbids greenfield create that Data Model Design needs).
- Inventing a new skill (e.g. `app-config-shell`) that duplicates `app-builder` tool subscriptions.
- Replacing Data Model Design grids, ApplicationFormBuilder popups, Entity editors, or Import Config UI (parallel enhancement).
- Implementing before §0 gate.

---

## 3. Which agent (locked direction)

| Choice | Verdict |
|--------|---------|
| Embed **`app-builder`** | **Yes** — application-wide create/configure is this agent’s job |
| New skill clone | **No** for v1 — one source of truth; Form/Search uses separate narrowed `app-configurator` |
| `app-configurator` on this page | **No** as primary — wrong scope |
| Form Agent | **No** |

```
My Application Editor (Config: App)
  └── dock → app-builder + HostContext(saasApplicationId, section, selection)

Form / Search runtime (EnableConfigurationMode + Admin)
  └── Configurator tab → app-configurator (narrow; cloned from healthy app-builder)

Form runtime Assistant
  └── app-transaction-form-agent (FormData only)
```

---

## 4. UX

### Placement

- **Shell-level** dock on `MyApplicationEditor` (not only inside `DataModelDesign.tsx`).
- One persistent chat while switching left-nav sections; HostContext updates each turn with `section` + selection.
- Tab label suggestion: **App Builder** (or **Assistant** if the shell has no other AI tab). Avoid calling it “Configurator” here to reduce confusion with Form’s Configurator tab.

### Visibility

- Editor is already an admin configuration surface. Align with **Admin** (and optionally `EnableConfigurationMode`) — exact flag parity to confirm at implement time; lean: same admin gate as opening My Applications / editor.
- Optional collapse/expand; remember last open state in localStorage per user or per app.

### After tool success

- Refresh the active section’s list/detail (e.g. Data Model Design grid `Refresh`) when the agent creates/updates/deletes a transaction or related object.
- Prefer structured tool results `{ ok, changed: [{ type, id }] }` so the host can refresh surgically.

### Relationship to hardcoded page

- Long term: this dock + Agent Management Test Run should supersede reliance on hardcoded `/app-builder-agent` for day-to-day work.
- Short term: keep hardcoded page until managed `app-builder` quality is acceptable.

---

## 5. HostContext (sketch)

Sent every Configurator/builder turn from the editor; inject into that turn’s system section only (Shared Agent pattern).

```json
{
  "surface": "myApplicationEditor",
  "mode": "appBuilder",
  "saasApplicationId": 12,
  "applicationName": "Simple CRM",
  "menuId": "…",
  "section": "DataModelDesign",
  "sectionId": 2,
  "selection": {
    "transactionId": 2270,
    "transactionName": "AI Working Tab",
    "organizedType": 1
  }
}
```

| Field | Role |
|-------|------|
| `saasApplicationId` | **Required.** Server-side default for create tools that take an app id. |
| `section` / `sectionId` | Prompt hint: prefer tools relevant to Data Model vs Entity vs Search vs Import. Map from `ApplicationBuilderSection` in `MyApplicationEditor.tsx`. |
| `selection` | Optional focused object when a grid row is selected. |

**Server rule (recommended):** when HostContext carries `saasApplicationId`, create/list tools that omit or mismatch app id use the host value (or reject mismatch). Prevents accidental cross-app writes.

Section code map (from editor today):

| sectionId | UI | Agent emphasis |
|-----------|-----|----------------|
| 1 | Application Properties | app package props |
| 2 | Data Model Design | transactions / hierarchy / list-edit |
| 17 | Data Model Template | templates |
| 15 | Entity List Of Value | entities |
| 12 | Report & View | searches / views |
| 18 | Import Config | AppConfigPack / import guidance |
| 9 | Database | schema tools (HITL) |
| 3 / workflow | Workflow | workflow tools when seeded |
| 11 | Menu Setting | menu attach tools |

Exact prompt wording per section: finalize after `app-builder` prompt is stable.

---

## 6. Tools

v1: **whatever healthy `app-builder` already subscribes** in Agent Management — no separate library only for this host.

Later optional host-only helpers (only if needed):

- `refresh_editor_section` — tell UI to reload current grid (or rely on client interpreting tool results).
- Stronger exclusions when `surface=myApplicationEditor` if some builder tools are unsafe in-tenant (product decision).

Do **not** subscribe `platform-form-host` here.

Import Config section may steer the agent toward `platform-app-config-pack` (validate / preview / execute) once that library is trusted — still HITL for execute.

---

## 7. Prompt notes (after builder fix)

When embedding, append or inject a short **host overlay** (not a full second system prompt):

- You are inside My Application Editor for application `{name}` (`saasApplicationId=…`).
- Prefer operating on this application only.
- Current section is `{section}`; selected object is `{selection}` if any.
- After create/update, summarize ids so the UI can refresh.
- Destructive deletes and DDL require `ask_user`.

Keep the main editable prompt on the `app-builder` skill in Agent Management so Test Run and this dock stay aligned.

---

## 8. Implementation sketch (after §0)

1. Stabilize `app-builder` in Agent Management.
2. Add right dock to `MyApplicationEditor.tsx` reusing `AgentUiChatHost` / Shared Agent chat (same building blocks as Form Agent panel).
3. Build HostContext from `menuId` / loaded application + `currentSection` + optional selection from child sections (start with Data Model Design row selection).
4. Wire refresh callbacks from dock → active section.
5. Soft-gate visibility (admin / settings).
6. Revisit this doc + Form Configurator clone list.

---

## 9. Open points

1. Dock label: **App Builder** vs **Assistant**.
2. Whether `EnableConfigurationMode` is required in addition to admin.
3. How selection is reported from each section child (callback props vs shared editor context).
4. Whether opening ApplicationFormBuilder popup from the agent is in scope vs metadata-only tools + user opens editor manually.
5. Session: one session per `saasApplicationId` vs new session every editor open.

---

## 10. Changelog

| Date | Change |
|------|--------|
| 2026-11-10 | Initial design: My Application Editor hosts managed `app-builder` with app-scoped HostContext; shell-level dock; no new skill; blocked on builder repair. |
