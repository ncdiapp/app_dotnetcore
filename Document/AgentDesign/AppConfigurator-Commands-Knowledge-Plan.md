# App Configurator — Commands Classification, Editor UI & Knowledge Plan

**Status:** P0–P6 landed (taxonomy, picker, KB, Help UI, AgentStarter seed + V085 upsert/SystemPrompt index). Optional P7 `list_command_types` not started.  
**Date:** 2026-11-17  
**Parent:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) §7.1 E (Data Model Operation CRUD)  
**Related:** V084 command tools (`get_commands`, `get_command_detail`, `upsert_command`, `delete_command`); UI list [`transactionCommandActionTypes.ts`](../../AppReact/src/components/transaction/transactionCommandActionTypes.ts); runtime `AppTransactionCommandBL`  
**Libraries:** `platform-form-config` (`app-configurator`)

---

## 1. Why

Data Model Operation (formerly Command / Operation Task) exposes a **flat, A–Z sorted** Operation Type dropdown. Users struggle to find the right type among dozens of peers. Configurator agents can CRUD operations via tools, but **lack a structured catalog and field-level knowledge** of what each type and shared setting means, where it is stored, and how runtime uses it.

This plan covers:

1. **Locked taxonomy** for UI-offered command types only (not the full server enum).
2. **Searchable tree Operation Type picker** + derived category path (not persisted).
3. **User help + agent knowledge base**, authored from full FE+BE analysis.
4. **How the KB is exposed** in the UI and fed to `app-configurator`.

---

## 2. Goal / Non-goals

### Goal

| # | Outcome |
|---|--------|
| 1 | Shared taxonomy module: L1 → optional L2 → leaf `ActionType` Id + friendly Display |
| 2 | Command Editor Operation Type control: searchable tree; Composition L1 auto-selects type `200` |
| 3 | Derived read-only **category breadcrumb** under Operation Type (from Id → path; not stored) |
| 4 | Authoritative English help/KB: cross-cutting settings + all **32** leaf types (UI → storage → runtime) |
| 5 | In-app Help / per-type help; optional prefab prompts into Configurator chat |
| 6 | Feed KB to agent via `AgentStarter/app-configurator/` + short SystemPrompt index |

### Non-goals (first pass)

- Renaming leaf Display strings (keep Angular-parity friendly names).
- Resurrecting unused / non-UI server enum members into the picker.
- Changing command **runtime** execution semantics.
- Chinese copies under `Document/` (Chinese review copies stay outside `Document/`, e.g. chat only).
- Shipping `list_command_types` tool in phase 1 (optional later if upsert quality needs it).

---

## 3. Document placement

| Artifact | Path | Language |
|----------|------|----------|
| **This design / plan (canonical)** | `Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md` | English |
| **User + agent KB body** | `Document/AgentDesign/AppConfigurator-Commands-UserGuide.md` | English |
| **UI Help fetch** | `AppReact/public/help/commands-user-guide.md` | English |
| **Agent file feed (seed)** | `AppAI.Web/AgentStarterSeeds/app-configurator/` → copy to `FileRepository/Company_{id}/AgentStarter/app-configurator/` | English |
| Chinese reading draft | **Not** under `Document/` | Human review only |

---

## 4. Source of truth for Operation Types

| Layer | Source | Role |
|-------|--------|------|
| **UI catalog** | `AppReact/src/components/transaction/transactionCommandActionTypes.ts` | Friendly Display + Id lists (Master–Detail, List Edit, Workflow embedded) |
| **Internal code** | `EmAppTransactionCommandType` in `APP.Components.Dto/AppEnums.cs` | Persisted `ActionType` |
| **Command Mgt filter** | `TransactionCommandMgt.tsx` | Drops **51** / **55**; this plan also drops **70** from new picks |

Do **not** drive the Operation Type picker from `useEnumValues('EmAppTransactionCommandType')` — that exposes unused server members and raw enum names.

### Excluded from new picker / taxonomy

| Id | Display | Reason |
|----|---------|--------|
| 51 | UI: Open Form Creation Window | Already filtered in Command Management |
| 55 | UI: Open Form Edit Window | Already filtered in Command Management |
| 70 | Create User | No longer offered in taxonomy (existing rows may still resolve Display via Id→label map) |

Server enum members never in any UI list (e.g. `CallExternalDllMethod` 52, `StartProject` 101) stay out of taxonomy and KB chapters.

**Catalog size:** **32** leaf types (36 Master–Detail Angular entries − 51 − 55 − 70).

---

## 5. Locked taxonomy

Leaf **Display** strings remain today’s UI names. Only grouping labels are new. Categories are **derived** from `ActionType`; nothing category-related is stored on the command row.

### 5.1 L1 rules

| L1 Id | Label | L2? | Behavior |
|-------|--------|-----|----------|
| `ui` | **UI Operations** | None — flat leaves | Client-side window / navigation / print |
| `data` | **Backend Operations** | Three L2 groups (no orphan L1 leaves) | Server pipeline steps |
| `apiIntegration` | **API, Plugin & External Process** | None (4 leaves) | API / plugin / external method / external exe |
| `messaging` | **Messaging** | None (4 leaves) | Email / SMS to form fields |
| `composition` | **Composition** | None | Selecting L1 **immediately sets** `ActionType = 200` |

### 5.2 L1 = `ui` — UI Operations

- UI: Close Form Window (54)
- UI: Navigate To Search (53)
- UI: Print (48)
- UI: Print From Message Template (74)
- UI: Save As (47)

### 5.3 L1 = `data` — Backend Operations

| L2 Id | L2 Label | Types |
|-------|----------|--------|
| `formPersistence` | **Form Persistence & Validation** | Save (49), Refresh (50), Validate Required Fields (205) |
| `formDataProcessing` | **Form Data Processing** | Exec DataModel All Formula (44), Execute Command Formula (57), Execute All DataLoad (45), Generate Matrix (46), Execute SQL Statement (42), Execute Data Transfer (43) |
| `importFileGeneration` | **Import & File Generation** | Import Json / Excel / Rest / DB-to-DB (66, 67, 71, 72), Multi Json / Excel (76, 77), Download File (73), Convert Xml↔Json (81, 82) |

**Form Data Processing** = in-form data work: formulas, SQL-driven updates, data load, data transfer — not file import and not window chrome.

**Form Persistence under Data (not UI):** Composition can sequence calculate → Save → message from DB values; Save / Refresh / Validate are **server command steps**, not window chrome.

### 5.4 L1 = `apiIntegration` — API, Plugin & External Process

- Call API Operation (59)
- Call Plugin Web Api (40)
- External Method MasterDetail (38)
- Execute External Exe Process (68)

### 5.5 L1 = `messaging` — Messaging

- Send Message: To Form Field UserId (60)
- Send Message: To Form Field Email Address (61)
- Send Message: To Form Field Partner (62)
- Send SMS: To Form Field Phone Number (63)

### 5.6 L1 = `composition` — Composition

Selecting this category sets **Composition Command (200)** immediately (single leaf or category click with `autoActionTypeId: 200`).

### 5.7 Context filters

Same taxonomy tree; filter leaves by allowed Id sets:

| Context | Allowed set |
|---------|-------------|
| Application Form Builder (Master–Detail) | Full 32 |
| List Edit | Subset today: 43, 49, 50, 200 (keep parity; still show path for those Ids) |
| Workflow embedded editor | Master–Detail minus UI:\* types (existing embedded list) |

---

## 6. Taxonomy module (implementation)

**Suggested file:** `AppReact/src/components/transaction/transactionCommandTypeTaxonomy.ts`  
(Keep Display maps in `transactionCommandActionTypes.ts`; taxonomy imports Display by Id.)

```ts
type CommandTypeLeaf = { kind: 'type'; actionTypeId: number; display: string };

type CommandTypeNode =
  | {
      kind: 'category';
      id: string;
      label: string;
      children: CommandTypeNode[];
      autoActionTypeId?: number; // Composition → 200
    }
  | { kind: 'group'; id: string; label: string; children: CommandTypeLeaf[] }
  | CommandTypeLeaf;
```

**Helpers:**

- `getCommandTypeTaxonomyTree(context)` → filtered tree
- `getCategoryPath(actionTypeId) → { l1Label, l2Label? }`
- `getCommandTypeLeaf(actionTypeId)` / search matcher (Display + ancestor labels)

**Optional L2 rule (engine):** show a `group` only when it has ≥ 1 leaf after context filter; if a future group would have a single leaf and product wants flatten, promote leaf under L1. Current catalog has no L1-only leaves under `data`.

---

## 7. Operation Type picker UI

### 7.1 Control

Replace the Operation Type `<select>` in  
`AppReact/src/components/transaction/ApplicationFormBuilder/CommandEditorPart/CommandBasicOptions.tsx`  
with a **searchable tree combobox / popover**:

- Theme tokens; keyboard: filter, arrows, Enter, Esc.
- Trigger shows **current leaf Display** only (e.g. `Execute SQL Statement`).
- Popover: search on top + scrollable tree (L1 → optional L2 → leaves).
- **Composition:** click L1 (or its only leaf) → set `ActionType = 200` and close.

Apply the same control wherever the same Operation Type list is edited (e.g. workflow embedded command panel), with context-filtered trees.

### 7.2 Derived category display (not stored)

Do **not** use two labeled Category / Subcategory inputs that look editable.

**Locked pattern:** under Operation Type, one muted **breadcrumb** derived from `ActionType`:

- `Backend Operations / Form Data Processing` (Category field shown only when L2 exists)
- Composition / UI / Messaging / API — no Category field (L1-only)

Inside the open picker, show the same path for the highlighted node. Optional help affordance on that line (see §9).

Persisted field remains **`ActionType` only**.

---

## 8. Help / KB — analysis commitment

Before publishing user help or agent KB, **fully analyze frontend + backend** for every shared editor setting and every in-taxonomy `ActionType`. Document from code, not guesswork.

| Layer | Extract |
|-------|---------|
| **UI** | Fields, checkboxes, buttons, sections; show/hide; create defaults; editor validation |
| **Persistence** | `AppActionAttributeDto` / columns vs `FormulaExpression` JSON; `NotificationMessage`, `Description`, `ActionType`, `ActionFlowOrder`, condition FK, etc. |
| **Save path** | APIs / BL that persist the command list; serialization |
| **Runtime** | Triggers (form menu, layout button, auto-on-open, composition child, workflow); FE-only vs `AppTransactionCommandBL`; condition; batch; result/log shaping |
| **Side effects** | DB writes, messages, windows, reload, child open-form flags, etc. |
| **Agent CRUD** | `upsert_command` / `optionsJson` field mapping |

**Method:** `CommandEditor.tsx` + `CommandEditorPart/*` → attribute DTO / `AppProjectWorkFlowActionConverter` → `AppTransactionCommandBL` (+ type-specific BLs) → `FormMainMenus` / form-load auto-exec. Cross-check Angular only when React is unclear.

**Quality bar:** a human or agent can configure a command and predict runtime behavior without reading source; agent can answer what a checkbox does, where it is stored, and when it runs.

### 8.1 KB content outline

1. Taxonomy and when to use each L1 / L2  
2. **Cross-cutting settings** — Public/Private (`LinkToUI`), Visible on Form Menu, Auto Execute On Form Open, Execute Condition, Flow Order, Command Status Log, Execution Result Display, Batch Command Setting, Notes, etc.  
3. **Per-type chapters** (32 leaves) — purpose, editor fields, storage, execution, prerequisites, pitfalls  
4. Composition / Message link **master → child** rules and defaults  
5. Menu / security filtering as implemented  

Suggested KB file name (later):  
`Document/AgentDesign/AppConfigurator-Commands-UserGuide.md`  
(optional splits under `Document/AgentDesign/Commands/`).

---

## 9. Help entry points (UI)

| Entry | Behavior |
|-------|----------|
| Data Model Operation **Help** | Opens full Operation user guide |
| Operation Type row **?** | Opens section for current type (or Composition category) |
| Prefab **Ask Configurator** | Posts a fixed prompt into Configurator chat (e.g. “Explain configuration for Execute SQL Statement”) so the agent answers from the KB |

---

## 10. Feeding the Configurator agent

| Step | Mechanism |
|------|-----------|
| 1 | Author English KB under `Document/AgentDesign/` |
| 2 | Sync/copy into **`AgentStarter/app-configurator/`** so the agent can `list_agent_files` / `read_agent_file` |
| 3 | Seed a **short SystemPrompt index** (L1/L2 + “read command help before `upsert_command`”) via migration pattern (V077 / V084 style). Do **not** dump the full enum essay into the prompt |
| 4 | Optional later: `list_command_types` BuiltIn returning taxonomy JSON (Id, display, path) |

Configurator must **not** execute runtime commands (`run_command` stays Form Agent / `platform-form-host`).

---

## 11. Implementation phases

| Phase | Work | Exit criteria |
|-------|------|----------------|
| **P0** | This plan doc in `Document/AgentDesign/` | Done when file lands and taxonomy matches §5 |
| **P1** | Taxonomy module + path helpers | Unit-testable Id → path; context filters; 70 excluded from new picks |
| **P2** | Searchable tree picker + breadcrumb in `CommandBasicOptions` (+ workflow host if shared) | Parity Display; Composition auto-200; theme/keyboard usable |
| **P3** | Deep FE+BE analysis notes (shared + 32 types) | Structured notes covering table in §8 |
| **P4** | User/agent KB markdown from notes | Quality bar in §8 |
| **P5** | Help / ? / prefab Ask Configurator entry points | Linked to KB anchors |
| **P6** | AgentStarter sync + SystemPrompt index seed | Agent can answer category/type help from files |
| **P7** (optional) | `list_command_types` tool | Upsert uses correct Ids without guesswork |

---

## 12. Key code anchors

| Area | Path |
|------|------|
| UI type lists | `AppReact/src/components/transaction/transactionCommandActionTypes.ts` |
| Command list / tabs | `.../ApplicationFormBuilder/TransactionCommandMgt.tsx` |
| Editor shell | `.../ApplicationFormBuilder/CommandEditor.tsx` |
| Basic options (Operation Type) | `.../CommandEditorPart/CommandBasicOptions.tsx` |
| Shared settings sections | `CommandStatusLog`, `CommandExecutionResultDisplay`, `BatchCommandSettingSection`, `CompositionCommandSection`, `NoteSection`, … |
| Enum | `APP.Components.Dto/AppEnums.cs` → `EmAppTransactionCommandType` |
| Attribute DTO | `APP.Components.Dto/UserDefine/AppTrans/AppActionAttributeDto.cs` |
| Attribute serialize | `APP.Components.EntityConverter/.../AppProjectWorkFlowActionConverter.cs` |
| Runtime | `APP.BL/TenantBusiness/AppTransactionCommandBL.cs` |
| Auto-exec on open | `APP.BL/TenantBusiness/AppMasterDetailFormDataLoadBL.cs` |
| Form menu trigger | `AppReact/src/components/formMgt/FormMasterDetail/FormMainMenus.tsx` |
| Agent tools | V084 / `platform-form-config` / `FormConfigPlugin` |

---

## 13. Change log

| Date | Note |
|------|------|
| 2026-11-17 | Initial plan: taxonomy (UI / Data+L2 / API & Integration / Messaging / Composition), tree picker + breadcrumb, FE+BE KB commitment, AgentStarter + SystemPrompt feed. Exclude 51, 55, 70 from new picks. |
| 2026-11-17 | P0 doc written. P1: `transactionCommandTypeTaxonomy.ts`. P2: `CommandOperationTypeTreePicker` in `CommandBasicOptions`; Command Mgt filters 51/55/70 via shared exclude set. |
| 2026-11-17 | Bug: Configurator `upsert_command` agents used Link Target `actionType=1` (Edit) as command ActionType → UI showed Unknown type (1). Fixed tool copy + server allow-list validation + create defaults (LinkToUI, FlowOrder). |
| 2026-11-17 | P3–P6: `AppConfigurator-Commands-UserGuide.md`; `public/help/commands-user-guide.md` + `CommandHelpModal`; prefab Ask Configurator; `AgentStarterSeeds/app-configurator/`. **V085** (merged former V085+V086): upsert_command ActionType clarify + SystemPrompt commands index (`commands-knowledge.md`). |
| 2026-11-17 | Product rename: UI/help/agent copy **Command / Operation Task → Data Model Operation** (short **Operation**). Tool names / code identifiers unchanged. |
