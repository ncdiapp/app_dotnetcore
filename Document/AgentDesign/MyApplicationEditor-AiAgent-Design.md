# My Application Editor — AI Agent Design

**Status:** **Multi-agent router locked (2026-11-16)** — supersedes 2026-11-10 “always `app-builder` only” for the Editor dock. See **§11**.  
**Date:** 2026-11-10; amended 2026-11-16  
**Host UI:** `AppReact/src/components/admin/MyApplicationEditor.tsx` and section children under `MyApplicationEditor/` (sidebar: Application Properties, Data Model Design, Entity List Of Value, Report & View, Import Config, …). Tab title pattern: `Config: {ApplicationName}`.  
**Related:**  
- [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) — Form/TX **Configurator** (`app-configurator` + `platform-form-config`); also Editor drill-in for TX/Template  
- [SearchViewConfig-AiAgent-Design.md](./SearchViewConfig-AiAgent-Design.md) — Search/View **Configurator** (`app-search-view-config`); Editor drill-in for Search  
- [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md) — Form **data** agent  
- [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md)

---

## 0. Dependency / revisit gate

This host embeds the **same** managed skill **`app-builder`** that Agent Management already defines. It does **not** introduce a fourth specialist skill.

**§0 status (2026-11-10): PASSED.** Managed `app-builder` is the skill this dock runs. Hardcoded `/app-builder-agent` remains short-term reference / fallback until day-to-day work moves here + Agent Management Test Run.

Still lock during implementation:

- HostContext fields and section→tool guidance overlay
- Refresh hooks per section
- Any host-specific tool exclusions
- Sync [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) when cloning the narrowed Form/Search agent

---

## 1. Why

**My Application Editor** is the application-level configuration shell. Admins create and maintain Form Models, List Edit models, entities, searches/views, DB design, workflow, and import packs for one `SaaSApplicationId`.

Today AI entry points for building are fragmented:

- Hardcoded **App Builder Agent** page (`/app-builder-agent`) — reference / fallback.
- Agent Management **`app-builder`** — product model; now §0-accepted.
- Form runtime: **`app-configurator`** (narrow TX/Search config) and **Form Agent** (FormData).

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
- Embedding the full graphic Form Designer **inside** the chat pane.

---

## 3. Which agent (locked — amended 2026-11-16)

**App / list depth** uses **`app-builder`** (Assistant). **Object drill-in** switches SkillSet — see **§11** (canonical). Short map:

| Depth / focus | Skill | Dock label |
|---------------|-------|------------|
| App section / list (no deep object) | `app-builder` | **Assistant** |
| Open Transaction / Form / TX Template | `app-configurator` | **Configurator** |
| Open Search / View | `app-search-view-config` | **Configurator** |
| Open Entity | `app-builder` (+ entity in HostContext) | **Assistant** |
| Runtime Form data | `app-transaction-form-agent` | **Assistant** (Form only) |

```
My Application Editor (Config: App) — ONE chrome, skill switches by depth (§11)
  ├── app depth     → Assistant → app-builder
  ├── TX / Template → Configurator → app-configurator
  └── Search / View → Configurator → app-search-view-config

Form runtime: Assistant = Form Agent; Configurator = app-configurator (same skill as Editor TX)
Search runtime: Configurator = app-search-view-config (same skill as Editor Search)
```

§4–§8 below still describe **app-builder / Assistant** host wiring; when drill-in is active, HostContext + session follow §11 instead of “always app-builder session”.

---

## 4. UX (locked decisions)

### Placement

- **Shell-level** dock on `MyApplicationEditor` (not only inside `DataModelDesign.tsx`).
- One persistent chat while switching left-nav sections; HostContext updates each turn with `section` + selection.
- **Tab label: Assistant** (not “Configurator” — that name is reserved for Form/Search `app-configurator`).

### Visibility

```
user can open My Application Editor / App Builder for this app
AND IsAiConfigured   // retrofit in final gate pass — see AppConfigurator §3.1
```

Do **not** require `EnableConfigurationMode` beyond whatever already gates entering the editor. Keep existing property name **`IsAiConfigured`** (no `IsAiModelReady`).

Optional collapse/expand; remember last open state in localStorage per user or per app.

### Session

- **Persist by `saasApplicationId`** — reopen editor for the same app resumes the same agent session (same pattern as wanting continuity across section switches).
- New app id → new session. Clearing/reset is an explicit user action (implementation: session store key includes skill + app id + user).

### After tool success

- Refresh the active section’s list/detail (e.g. Data Model Design grid `Refresh`) when the agent creates/updates/deletes a transaction or related object.
- Prefer structured tool results `{ ok, changed: [{ type, id }] }` so the host can refresh surgically.

### Relationship to hardcoded page

- Long term: this dock + Agent Management Test Run supersede reliance on hardcoded `/app-builder-agent`.
- Short term: keep hardcoded page as fallback.

### Opening ApplicationFormBuilder / deep editors (decision)

**Problem this answers:** after the agent creates or updates a Transaction, should chat also **launch** the existing ApplicationFormBuilder (graphic) popup, or only change metadata and refresh the grid?

| Option | Meaning |
|--------|---------|
| A. Metadata-only | Agent uses BuiltIn tools; host refreshes grid; admin opens Form Builder manually from the row. |
| B. Host open-editor | Agent may call a **host tool** (like Form Agent `open_linked_search`) that opens the **existing** ApplicationFormBuilder / Entity / Search editor popup for an id. Chat does not re-implement the designer. |
| C. In-chat designer | Rebuild layout UI inside the agent pane. **Out of scope.** |

**Locked recommendation: A + B.**

1. Default path: tools write metadata → `{ changed }` → section refresh (A).
2. When the user asks to “open / edit the form designer” (or after create, agent offers and user confirms): host tool `open_transaction_editor` / `open_entity_editor` / `open_search_editor` opens the **existing** popup (B).
3. Never C. Pixel-perfect layout stays in Design Layout / Form Builder UI; Form/Search Configurator handles in-context layout tweaks on the runtime form.

---

## 5. HostContext (sketch)

Sent every Assistant turn from the editor; inject into that turn’s system section only (Shared Agent pattern).

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

**Server rule:** when HostContext carries `saasApplicationId`, create/list tools that omit or mismatch app id use the host value (or reject mismatch). Prevents accidental cross-app writes.

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

Exact prompt wording per section: finalize when wiring the host overlay.

---

## 6. Tools

v1: **whatever healthy `app-builder` already subscribes** in Agent Management — no separate library only for this host.

Host-only helpers (recommended):

- Client interprets `{ ok, changed }` for refresh (preferred).
- Optional `refresh_editor_section` if server needs to signal explicitly.
- **`open_transaction_editor` / `open_entity_editor` / `open_search_editor`** — host tools; open existing popups (§4).

Do **not** subscribe `platform-form-host` here.

Import Config section may steer the agent toward `platform-app-config-pack` (validate / preview / execute) once that library is trusted — still HITL for execute.

---

## 7. Prompt notes

When embedding, append or inject a short **host overlay** (not a full second system prompt):

- You are inside My Application Editor for application `{name}` (`saasApplicationId=…`).
- Prefer operating on this application only.
- Current section is `{section}`; selected object is `{selection}` if any.
- After create/update, summarize ids so the UI can refresh.
- Destructive deletes and DDL require `ask_user`.
- To open the graphic form/entity/search editor, use the host open-* tools after confirm — do not claim you edited pixels in chat.

Keep the main editable prompt on the `app-builder` skill in Agent Management so Test Run and this dock stay aligned.

---

## 8. Implementation sketch

**Order (locked with Configurator doc):** Form Configurator first → **this Editor Assistant** → runtime Search Configurator → **`IsAiConfigured` gate retrofit last**.

1. Add right dock to `MyApplicationEditor.tsx` reusing `AgentUiChatHost` / Shared Agent chat (same building blocks as Form Agent panel). Tab label **Assistant**; skill `app-builder`.
2. Session key: user + `app-builder` + `saasApplicationId` (persist across editor opens).
3. Build HostContext from `menuId` / loaded application + `currentSection` + optional selection from child sections (start with Data Model Design row selection).
4. Wire refresh callbacks + host open-editor tools.
5. Visibility: can enter editor; fold in `IsAiConfigured` in the final gate pass.
6. Keep Form Configurator clone list aligned with the same builder baseline.

---

## 9. Open points (non-blocking)

1. How selection is reported from each section child (callback props vs shared editor context) — start with Data Model Design.
2. Exact session-store API (reuse GenericAgent session vs app-scoped key convention).
3. Whether localStorage also remembers dock open/closed per app.

---

## 10. Changelog

| Date | Change |
|------|--------|
| 2026-11-10 | Initial design: My Application Editor hosts managed `app-builder` with app-scoped HostContext; shell-level dock; no new skill; blocked on builder repair. |
| 2026-11-10 | §0 **passed**. Locked: tab **Assistant**; session per **`saasApplicationId`**; open existing editors via host tools (A+B); keep **`IsAiConfigured`** (no rename); build order Form Config → Editor → runtime Search → gate last. |
| 2026-11-16 | **§11 multi-agent router** locked (E1a–d, E3). App-builder only at app depth; TX/Template → `app-configurator`; Search → `app-search-view-config`. Canonical home for Editor topology (moved out of SearchViewConfig draft). |

---

## 11. Multi-agent router (canonical — 2026-11-16)

> Moved here from Search View Config draft. **This doc owns Editor topology.** Search View Config / App Configurator docs only link here.

### 11.1 Product intent

1. **App-level (list / section, no deep object):** `app-builder` manages the package for open `saasApplicationId`.
2. **Drill-in (opened a concrete object):** HostContext = that object; active agent **matches object type**.

Same right chrome; **agent + session follow selection depth**.

### 11.2 Topology

```
┌─ My Application Editor ─────────────────────────────────────┐
│ Left: sections / lists          Right: ONE chat chrome       │
│                                                              │
│  [no row open / app section]  →  App Builder Agent           │
│       skill: app-builder · label: Assistant                  │
│       scope: saasApplicationId                               │
│                                                              │
│  [open Transaction / Form]    →  App Configurator Agent      │
│       skill: app-configurator · label: Configurator          │
│       scope: transactionId (+ units/fields/formula/commands/ │
│              data transfer / link target / linked search…)   │
│                                                              │
│  [open Search / View]         →  Search View Config Agent    │
│       skill: app-search-view-config · label: Configurator    │
│       scope: searchId / searchViewId / dataSetId             │
│                                                              │
│  [open Entity]                →  app-builder (+ entity HC)   │
│  [open TX Template]           →  app-configurator (TX-shaped)│
│  [Import Config section]      →  app-builder (pack tools)    │
└──────────────────────────────────────────────────────────────┘
```

**Principles:** SkillSet = role + object class; Library = durable tools; Host + session = where. Same specialist SkillSet may run in Editor **and** Runtime — **not** a different SkillSet per host.

### 11.3 Agents & libraries

| # | SkillKey | When active | Job |
|---|----------|-------------|-----|
| 1 | `app-builder` | App / list | Global CRUD for this app |
| 2 | `app-configurator` | TX / Form / Template (Editor or runtime) | Durable Form/TX config — full Form Builder surface (see AppConfigurator §7) |
| 3 | `app-search-view-config` | Search / View (Editor or runtime) | Durable Search/View/Dataset/Criteria |
| 4 | `app-transaction-form-agent` | Runtime Form Assistant | FormData only |

| Library | Subscribed by |
|---------|---------------|
| Builder libs (`platform-transaction`, `platform-search` create/list, pack, …) | **`app-builder`** |
| **`platform-form-config`** | **`app-configurator` only** |
| **`platform-search-view-config`** | **`app-search-view-config` only** |
| `platform-form-host` | Form Agent only |

Do **not** merge form-config + search-view-config. **Do** share patterns (ask_user, Id preference, HostContext force ids).

### 11.4 Session & labels (E1 / E3 locked)

```
Depth = app     → app-builder
                 session ≈ app-builder:{user}:{saasApplicationId}
                 banner: Assistant · {App}

Depth = object  → skill by type
                 session ≈ {skill}:{user}:{objectType}:{objectId}
                 banner: Configurator · TX:… / Template:… / Search:…
```

| # | Decision |
|---|----------|
| **E1a** | App → `app-builder`; TX/Form → `app-configurator`; Search → `app-search-view-config` |
| **E1b** | Template open → `app-configurator`; Entity open → `app-builder` |
| **E1c** | v1 router on **Editor shell only**; Form Builder popup reuses same `app-configurator` session — no in-popup chrome |
| **E1d** | App = **Assistant**; object = **Configurator** (not everything “Configurator”) |
| **E3** | Separate session per skill + object; resume on return |

### 11.5 Application Form Builder popup (E1c detail)

Shell chat stays on My Application Editor. Opening Application Form Builder does **not** add a second dock in v1; TX focus keeps `app-configurator`. In-popup section router = later.

### 11.6 Overlap mitigations

| Overlap | Mitigation |
|---------|------------|
| Builder `create_search` vs Search Config update | Different skills; Search Config default no greenfield create |
| Builder structure vs Form Config field/layout/command | Builder: create/delete/structure; Form Config: open-TX durable detail |
| Runtime vs Editor Form Config | Same SkillSet + library; HostContext `editorKind` / `surface` |
