# App Configurator — Formula & Form Layout Tools

**Status:** Implemented (2026-11-12). Seed: `V080__FormConfigFormulaLayoutTools_xhu.sql`.  
**Date:** 2026-11-12  
**Parent:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md)  
**Related:** [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md), Formula Editor UI (`TransactionFormulaEditor.tsx`), Form Design UI (`FormDesign.tsx`)  
**Libraries:** `platform-form-config` (app-configurator), `platform-transaction` (app-builder)  
**Code:** `FormConfigFormulaLayoutService.cs` → wrapped by `FormConfigPlugin` / `TransactionModifierPlugin`

---

## 1. Why

Configurator could change field settings, visibility, and Entity DDL links, but **not**:

- Calculation Validation Flow formulas (Assignment / Warning / Error / Aggregate / Subscribe)
- Flex Form Design layout (generate N-column layout, move fields, heights, containers / tabs)

Agents then misused `update_field_setting.defaultValue` as a “formula,” which never appears in Formula Editor.

This sub-doc covers the **tool enhancement** that closes those gaps on both Configurator and App Builder libraries.

---

## 2. Goal / Non-goals

### Goal

Give agents durable BuiltIns to:

1. **List / add / update / delete** unit formulas with display-name resolution + `ask_user` confirmation.
2. **Inspect / generate / patch** Flex form layout with overwrite confirmation and optional `backup` / `dryRun`.

Same capability on:

| Agent | Library | Plugin |
|-------|---------|--------|
| `app-configurator` | `platform-form-config` | `FormConfigPlugin` (HostContext `transactionId`) |
| `app-builder` | `platform-transaction` | `TransactionModifierPlugin` (explicit `transactionId`) |

Shared logic lives in `FormConfigFormulaLayoutService`.

### Non-goals (v1)

- SQL Scalar / SQL Tuple / Lead Assignment formula types (only Assignment, Warning, Error, Aggregate, Subscribe).
- Full Form Design parity (drag canvas, arbitrary nested rebuild beyond generate + listed patch ops).
- Search View formulas (transaction unit formulas only).
- Writing FormData / runtime calculation execution.

---

## 3. Tools (4)

| Tool | Read/Write | Purpose |
|------|------------|---------|
| `get_unit_formulas` | Read | List formulas for a unit (incl. reconstructed Aggregate / Subscribe). |
| `update_unit_formula` | Write | `add` / `update` / `delete` with resolve → confirm → persist. |
| `get_form_layout` | Read | Summarize Flex layout tree (containers, fields, heights, tabs). |
| `design_form_layout` | Write | `mode=generate` or `mode=patch`. |

After every successful write: Configurator must call `request_form_reload`.

---

## 4. Formula tools

### 4.1 Storage model (critical)

- Table: `AppTransactionUnitFormula`.
- **`TransactionUnitId` is required** — there is no “null UnitId = root” formula.
- **Root-level formulas** (fields on Root Master **or** Master Sibling units) must be stored on the **True Root Master Unit** (`ParentTransactionUnitId` null and `IsMasterSiblingUnit` false).
- Formula Editor main grid only renders `AppTransactionUnitList[0]` (true root) formulas.
- Master Sibling fields appear in root **Assign To** via UI merge, but sibling-unit formula rows **do not** show on the root grid.

| Field location | Formula owner unit |
|----------------|--------------------|
| Root Master field | True Root Master Unit |
| Master Sibling field (`[Table.DbName_Id]` tokens) | **True Root Master Unit** (not the sibling id) |
| Child / grandchild grid field | That child / grandchild unit |

`update_unit_formula` remaps owner unit accordingly and removes duplicate Assignments left on sibling units.

### 4.2 Operation types (`EmAppFormularType`)

| Value | Name | Persist how |
|------:|------|-------------|
| 1 | Assignment | Formula row: `[Left_Id] = expr` |
| 2 | BooleanExpressionWarning | Formula row + `WarningMessage` |
| 3 | BooleanExpressionError | Formula row + `WarningMessage` |
| 4 | Aggregate (SubscribeFromGridColumnAggregation) | Field cross-relation + `AppTransactionFieldAggFunction` (not a durable formula row) |
| 5 | Subscribe (SubscribeFromParentLevelField) | Field `ChildUnitSubscribeParentFieldId` |

`defaultValue` on a field is **not** a Calculation Flow formula.

### 4.3 Expression resolution

1. User / agent may pass approximate **display names**  
   e.g. `Description = Product Code + " - " + Nomenclature`.
2. Tool resolves to FormulaDisplayName tokens  
   e.g. `[Description2_124] = [Article_117] + " - " + [Description_118]`  
   (sibling fields use `[Table.DbName_Id]`).
3. On save, BL `InFormat` converts tokens → `transactionfieldid_{id}` in DB.
4. Ambiguous names → `needsDisambiguation` + candidates → agent `ask_user` → retry with `fieldId`s.
5. Preview (`confirmed` omitted/false or `dryRun=true`) returns `resolvedExpression` → agent `ask_user` Yes/No → call again with `confirmed=true`.

### 4.4 `update_unit_formula` parameters

| Param | Notes |
|-------|--------|
| `action` | `add` \| `update` \| `delete` |
| `unitId` / `unitName` | Hint; owner remapped for root-level fields |
| `operationType` | 1–5 (default Assignment) |
| `expression` | Required for 1–3 |
| `assignToFieldId` / `assignToFieldName` | Prefer id |
| `sourceFieldId` / `sourceFieldName` | Aggregate child column / Subscribe parent field |
| `aggregationType` | 1=SUM 2=AVG 3=MIN 4=MAX 5=COUNT (also 6/7) |
| `warningMessage` | Warning / Error text |
| `confirmed` | Must be `true` to persist |
| `dryRun` | Preview only |

### 4.5 Agent sequence (formula)

```
find_fields / get_unit_fields
  → get_unit_formulas(unitId)          # inspect
  → update_unit_formula(..., confirmed omitted)   # resolve / preview
  → [needsDisambiguation?] ask_user pick fieldId → retry
  → ask_user Yes/No on resolvedExpression
  → update_unit_formula(..., confirmed=true)
  → request_form_reload
  → verify in Formula Editor (Refresh)
```

---

## 5. Form layout tools

### 5.1 Storage model

| Layer | What |
|-------|------|
| DB | `AppForm` + `AppFormLayoutItem` |
| Tree | Parent via host ids / `UigridLayoutParentId`; order via `FlowOrGridLayoutSortOrder` |
| Attrs | `ParameterKeyValue` ↔ `AppFormDomAttributeDto` |
| Type | Prefer Flex (`EmAppFormLayoutType.Flex = 4`) |

Common DomAttribute props used by tools:

- Columns: `DefaultNbColumns`, `ColSpanValue`
- Height: `HeightValue`, `IsUnlimitedHeight` (Memo / Image / Grid)
- Container: `DisplayName` (label), `IsCollapsible`, `IsDefaultCollapsed`, `IsTab`, `WidgetDisplayType` (Section / TabContainer / LayoutRow / …)

BL entry points:

- Load: `AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto`
- Generate: `BuildAppFormDefaultLayout(formId, nbColumns)` then `SaveAppFormFlexLayoutExDto`
- Clear: `AppFormBL.ResetFormLayout` (leaf-safe delete) before regenerate when items exist

### 5.2 `design_form_layout`

| Mode | Behavior |
|------|----------|
| `generate` | Build default Flex layout; optional `nbColumns`. **Replaces** existing layout → requires `confirmed=true`. Optional `backup=true` (summary in response), `dryRun=true`. |
| `patch` | Apply `patchesJson` ops on current tree, then save. |

**Patch ops (v1):**

| `op` | Args | Effect |
|------|------|--------|
| `setHeight` | `fieldId` \| `layoutItemId` \| `gridUnitId`, `height`, optional `isUnlimitedHeight` | Memo / Image / Grid height |
| `moveField` | `fieldId`, `beforeFieldId` \| `afterFieldId` | Reorder field rows among siblings |
| `setContainer` | `layoutItemId` / `hostId`, `label`, `isCollapsible`, `isDefaultCollapsed`, `isTab`, optional `widgetDisplayType` | Stack / Tab label & collapse |
| `setColumns` | `nbColumns` | Alone → treated as `generate` rebuild |

Changing 2-col → 3-col on a hand-tuned tree is **generate** (full rebuild), not a surgical column redistribute.

### 5.3 Agent sequence (layout)

```
get_form_layout
  → design_form_layout(mode=generate, nbColumns=N, backup=true)   # needsConfirmation
  → ask_user Yes/No (overwrite)
  → design_form_layout(..., confirmed=true)
  → request_form_reload
```

Patch path: same confirm gate with `patchesJson`.

---

## 6. Seed / agent enablement (V080)

Migration: `AppAI.Web/Migrations/V080__FormConfigFormulaLayoutTools_xhu.sql`

1. Insert 4 tools into `platform-form-config` and `platform-transaction`.
2. Refresh `app-configurator` SystemPrompt (formula + layout sequences; forbid defaultValue-as-formula).
3. Force-enable:
   - `app-configurator` → subscribe `platform-form-config`; delete exclusions for the 4 tools.
   - `app-builder` → subscribe `platform-transaction`; delete exclusions for the 4 tools.

Selection model: library subscription + `AppAgentToolExclusion` (opt-out). Clearing exclusions = tools checked in Agent Management UI.

---

## 7. Hard rules (agents)

1. Never treat `defaultValue` as Calculation Flow formula.
2. Always `ask_user` (single_choice + optionsJson) before durable formula / layout writes.
3. Formula: show `resolvedExpression` before `confirmed=true`.
4. Layout generate overwrite: `ask_user` + `confirmed=true`; prefer `backup=true`.
5. After write: `request_form_reload` (Configurator); do not claim Reload unless tool returned ok.
6. Root-level / sibling-field formulas → Root Master UnitId (tool remaps; agents should still prefer true root `unitId` when listing).

---

## 8. Verification checklist

- [ ] Formula appears in DB under **Root Master** `TransactionUnitId` (not sibling).
- [ ] Formula Editor Refresh shows Assignment row (not only Grid placeholders).
- [ ] Expression OutFormat shows `[DbName_Id]` / `[Table.DbName_Id]` tokens.
- [ ] Aggregate / Subscribe appear after reload (reconstructed from field flags).
- [ ] `design_form_layout` generate with existing layout blocked until `confirmed=true`.
- [ ] Patch `setHeight` / `moveField` / `setContainer` visible after Reload.
- [ ] Both agents show 4 tools checked under their libraries after V080.

---

## 9. Known pitfalls

| Symptom | Likely cause |
|---------|----------------|
| DB has formula; Formula Editor empty | Formula stored on **Master Sibling** unitId; Editor only shows Root Master list |
| Agent says “formula set”; Editor unchanged | Wrote `defaultValue` via `update_field_setting` |
| Tokens look like `[Table.Field_Id]` | Master Sibling field — correct FormulaDisplayName shape |
| Editor shows formula; runtime no new value | (1) After save, DB must store `transactionfieldid_{id}` (InFormat). Sibling `[Table.Field_id]` is editor display only. (2) React runs formulas on **Calculate** / **Save**, not every keystroke. |
| `Expression Is Invalid, On Unit [{0}] Formular Sort [{1}]` | `IsValidFormulaExpression` requires `[...]` field tokens (or `transactionfieldid_`). Do **not** strip brackets before `SaveAppTransactionUnitFormulaSetDto` validates — keep display tokens; InFormat converts after validate. |
| Generate wiped custom layout | Expected for `mode=generate`; use `backup` + confirm; prefer `patch` for surgical edits |
| Aggregate not in `AppTransactionUnitFormula` | By design — stored on field AggFunction / cross-relation |

---

## 10. File map

| Area | Path |
|------|------|
| Shared service | `APP.BL/AIAgent/GenericAgent/Plugins/FormConfigFormulaLayoutService.cs` |
| Configurator wrappers | `APP.BL/AIAgent/GenericAgent/Plugins/FormConfigPlugin.cs` |
| Builder wrappers | `APP.BL/AIAgent/AppBuilderAgent/Plugins/TransactionModifierPlugin.cs` |
| Formula BL | `APP.BL/TenantBusiness/AppTransactionFormulaSetupBL.cs` |
| Layout BL | `APP.BL/TenantBusiness/AppFormFlexLayoutBL.cs`, `AppFormBL.ResetFormLayout` |
| Formula UI | `AppReact/.../TransactionFormulaEditor.tsx` |
| Form Design UI | `AppReact/.../formMgt/FormDesign.tsx` |
| Seed | `AppAI.Web/Migrations/V080__FormConfigFormulaLayoutTools_xhu.sql` |
