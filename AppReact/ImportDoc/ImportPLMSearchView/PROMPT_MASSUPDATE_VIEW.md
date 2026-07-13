# PLM Search — Mass Update View Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportPLMSearchView/`  
> **Purpose:** Import one PLM `pdmMassUpdateView` onto an APP Search that was **already** imported via [`PROMPT.md`](PROMPT.md).  
> **Does NOT replace** the main first-import or sibling-view prompts. Do **not** edit `PROMPT.md` / `PROMPT_SIBLING_VIEW.md` while running this file.  
> **Not a Sibling ReferenceView:** Mass Update is a **separate** PLM object (`pdmMassUpdateView`), linked via `pdmSearchTemplateReferenceView.MassUpdateViewID` / `pdmSearchTemplate.DefaultMassUpdateViewId`.

---

## User input (required)

```text
1. PLM connection string
2. APP tenant connection string
3. PLM SearchTemplateId — already imported once (e.g. 28902)
4. PLM MassUpdateViewId — the Mass Update to import (e.g. 9)
```

Optional:

| Parameter | Default |
|-----------|---------|
| APP `Search.IntegrationId` | Resolve from prior import / naming (e.g. `Search_28902`) |
| `@TablePrefix` | `Plm_` |
| `setAsDefaultMassUpdateView` | `true` if this id equals PLM `DefaultMassUpdateViewId`; else ask |

### Gate 0 — missing input → ask only

If the user only `@` this file without **all four** required items → **STOP** and ask. Do not probe.

### Example session

```text
按 AppReact/ImportDoc/ImportPLMSearchView/PROMPT_MASSUPDATE_VIEW.md 执行。

PLM connection string: ...
APP tenant connection string: ...
PLM SearchTemplateId: 28902
PLM MassUpdateViewId: 9
APP Search IntegrationId: Search_28902   (optional)
```

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Prerequisite** | Main [`PROMPT.md`](PROMPT.md) import for this `SearchTemplateId` must already exist in APP (`AppSearch` + DataSet + at least one View). Template DW + `{prefix}FieldMapping` must exist. |
| **No server code in this Prompt run** | Deliverables are SQL + JSON under this folder only. If Phase D BL / Import Tool support is missing → generate Blueprint anyway, **STOP** with clear “BL gap” note (same pattern as early Search import). |
| **FieldMapping is truth** | Resolve SubItem / GridColumn → APP column **only** via `{prefix}FieldMapping`. Never invent column names. |
| **Mass Update ≠ display View** | Do **not** treat this as another `pdmReferenceView` sibling. Do **not** use `PROMPT_SIBLING_VIEW.md` Option A/B for this object. |
| **PK required (Mode A)** | Single-table APP mass update **must** map the update Unit’s primary key field (`MassUpdateTransactionFieldId`). |
| **Option A join limit** | Enriching DataSet for missing MU columns may only add SELECT columns and/or **first-level 1:1** `LEFT OUTER JOIN`. **Forbidden:** 1:N grid tables that multiply rows. |
| **Do not change default display View** | Adding a Mass Update View must **not** replace `AppSearch.SearchViewId` (display default) unless the user explicitly asks. |
| **Prefix is parameter** | All APP table names respect `@TablePrefix`. |

---

## Conceptual map (agent must use this)

### PLM (source)

```text
pdmSearchTemplate
  ├─ ReferenceViewID              → display grid (already imported by PROMPT.md)
  ├─ DefaultMassUpdateViewId      → default mass-update definition
  └─ pdmSearchTemplateReferenceView
        ├─ ReferenceViewID        → extra display views (PROMPT_SIBLING_VIEW)
        └─ MassUpdateViewID       → THIS prompt

pdmMassUpdateView
  ├─ UpdateType   1=TabField  2=RegularGrid  3=DynamicMatrix
  ├─ MainTabId / GridBlockId
  └─ pdmMassUpdateViewField  (SubItemId | GridColumnId, Sort, IsReadonly, …)
```

### APP (target)

| PLM | APP |
|-----|-----|
| `pdmMassUpdateView` | New `AppSearchView` with `IsMassUpdateView = true` (preferred), same DataSet as the Search |
| `UpdateType` TabField (flat header fields) | **Mode A** `SingleTableUpdate` — MasterDetail Transaction + Unit |
| `UpdateType` RegularGrid / Matrix (1:N) | **Mode B** `HierarchicalTableUpdate` — ListEdit Transaction (root PK only). **Find existing ListEdit or create one** (user confirms structure) |
| Field SubItem / GridColumn | `AppSearchViewField.MassUpdateTransactionFieldId` → Transaction field (Mode A) or ListEdit unit fields (Mode B create) |
| MainTab / update table | `UpdateTransctionId` + `UpdateBaseTranscationUnitId` |
| Default Mass Update | Document / set Search’s default Mass Update view when supported |

Runtime type is **derived** from Transaction (`MasterDetail` → SingleTable; `List` → Hierarchical) — see `AppSearchViewConfigBL` / `EmAppMassUpdateViewType`.

---

## Flow

```text
Gate 0 → APP probe (Search exists? MU View already imported?)
      → PLM probe (MassUpdateView shell + fields) + FieldMapping + Transaction resolve
      → If Mode B likely: ListEdit discovery (existing candidates OR propose new structure)
      → STOP: report + user chooses A / B / C
            (+ confirm Transaction/Unit, or ListEdit pick / approve ListEdit create structure)
      → Phase B blueprint JSON (Mode B may embed listEditCreate)
      → Phase D (PLM Import Tool — create ListEdit if needed, then Mass Update View)
```

---

## Phase A1 — APP state probe

Locate APP Search:

1. If user gave `IntegrationId` → `AppSearch` by that id.  
2. Else try `output/{SearchTemplateId}/README.md` / convention `Search_{SearchTemplateId}`.  
3. If not found → **STOP**: run main `PROMPT.md` first.

Load:

- `SearchID`, `DataSetId`, current DataSet `QueryText` / tables / columns  
- Existing Views on that DataSet (name, `IsMassUpdateView`, `UpdateTransctionId`)  
- Candidate **MasterDetail** Transactions: `Tab_{MainTabId}` and FieldMapping-backed tables → `AppTransaction` / units  
- Candidate **ListEdit** Transactions: `TransactionOrganizedType = 3` (`List`) whose root/child units touch the MU header / grid tables  
- Whether this MassUpdateView was already imported (name match, `IsMassUpdateView`, or prior `3_PlmSearch_MassUpdateView_*.json`)

| Result | Action |
|--------|--------|
| Search missing | STOP — main Prompt first |
| Mass Update View already present | STOP — nothing to do (or ask re-import / update) |
| Search OK, MU not imported | Continue A2 |

Suggested SQL sketch:

```sql
SELECT SearchID, Name, IntegrationId, DataSetId, SearchViewId
FROM dbo.AppSearch
WHERE IntegrationId = @IntegrationId;

SELECT SearchViewID, Name, DataSetID, ViewType,
       IsMassUpdateView, UpdateTransctionID, UpdateBaseTranscationUnitID,
       IsAllowAddRow, IsAllowDeleteRow, IsAllowUpdateRow
FROM dbo.AppSearchView
WHERE DataSetID = @DataSetId
ORDER BY SearchViewID;

-- MasterDetail candidates (Mode A)
SELECT TransactionID, Name, IntegrationId, TransactionOrganizedType
FROM dbo.AppTransaction
WHERE IntegrationId LIKE N'Tab_%' OR Name LIKE N'%Header%';

-- ListEdit candidates (Mode B) — OrganizedType List = 3
SELECT t.TransactionID, t.Name, t.IntegrationId, t.TransactionOrganizedType
FROM dbo.AppTransaction t
WHERE t.TransactionOrganizedType = 3
ORDER BY t.Name;

-- Units for a ListEdit (after picking candidates)
SELECT u.TransactionUnitID, u.UnitDisplayName, u.ParentTransactionUnitID, u.SysTableName
FROM dbo.AppTransactionUnit u
WHERE u.TransactionID = @ListEditTransactionId
ORDER BY u.TransactionUnitID;
```

Also re-read `output/{SearchTemplateId}/README.md` and any existing Mass Update blueprints.

---

## Phase A2 — PLM Mass Update + FieldMapping

Against **PLM**, run [`source/_plm_probe_massupdate.sql`](source/_plm_probe_massupdate.sql) with `@SearchTemplateId` + `@MassUpdateViewId` (or equivalent ad-hoc SQL):

| Check | Detail |
|-------|--------|
| Shell | `pdmMassUpdateView`: Name, UpdateType, MainTabId, GridBlockId, Freeze, Max rows, flags |
| Link | Confirm View is attached to this SearchTemplate (`pdmSearchTemplateReferenceView` and/or `DefaultMassUpdateViewId`) — **warn** if not linked but user still wants import |
| Fields | `pdmMassUpdateViewField`: SubItemId / GridColumnId, Sort, IsReadonly, display names |
| Enrich | SubItem → Tab / Entity; GridColumn → GridMeta / Entity (same pattern as search view probe §4b) |

Against **APP**:

- Resolve each field via FieldMapping (`_app_probe_fieldmapping.sql` pattern — populate wanted SubItems / MetaColumns from MU fields).  
- Resolve **update Transaction + Unit** (Mode A):
  - Prefer Transaction for `Tab_{MainTabId}` / header table from FieldMapping.  
  - Unit = table that owns the majority of updatable MU fields (or MainTab’s header unit).  
  - PK column of that Unit must be resolvable (usually `ReferenceId` or table PK).  
- Resolve **ListEdit** (Mode B — when UpdateType is RegularGrid/DynamicMatrix or RequiresOneToN > 0):
  1. Query existing `AppTransaction` where `TransactionOrganizedType = 3` (List).  
  2. Score candidates whose root/child `SysTableName` match MU header / grid tables (FieldMapping).  
  3. Present ranked list to user (**B1**).  
  4. If **none** match (or user rejects all) → draft a **CreateNew** ListEdit structure from FieldMapping + MU fields (root = header grain, child = 1:N grid table(s), fields from MU). Present for **explicit user approval** before Phase B (**B2**).  
  5. Never emit `listEditCreate.action=CreateNew` without that approval.  
- Classify each MU field vs **current** DataSet **and** vs Transaction fields:

| Class | Meaning |
|-------|---------|
| `MappedToTxnField` | FieldMapping + matching `AppTransactionField` on chosen Unit → can set `MassUpdateTransactionFieldId` |
| `CoveredInDataSet` | Column already in DataSet SELECT (needed for grid display/edit) |
| `AddColumn` | Table already joined; add to SELECT only |
| `AddOneToOneLeftJoin` | Need one more **1:1** sibling via `LEFT OUTER JOIN` on header/`ReferenceId` |
| `RequiresOneToN` | Grid / row multiplication → cannot stay on Mode A DataSet enrich |
| `Unmapped` | No FieldMapping — ignore / stop / import Template first |
| `ReadonlySkip` | PLM `IsReadonly` — include in View as non-updatable (no MU field map) or skip |

---

## Phase A3 — Analysis report (STOP — wait for user)

Present exactly this shape:

```text
Mass Update View import analysis
================================
PLM SearchTemplateId: {id}  ({name})
PLM MassUpdateViewId: {muId}  ({muName})
PLM UpdateType:       TabField | RegularGrid | DynamicMatrix  ({n})
PLM MainTabId / GridBlockId: {…}
PLM DefaultMassUpdateViewId: {id}  → this view is default? yes|no

APP Search:           #{searchId}  IntegrationId={...}
APP DataSet:          #{dataSetId}
  Current tables:     [...]
  Current column count: N

Recommended APP mode: SingleTableUpdate | HierarchicalTableUpdate | Blocked
Proposed Update Transaction: #{id} / IntegrationId={...}  (OrganizedType=MasterDetail|List)
Proposed Update Unit:        #{id} / {unitName} / table={Plm_Xxx}
PK field for mapping:        {column} → TransactionFieldId={…}

Field coverage for MassUpdateView {muId}:
  MappedToTxnField:     n  → [short list]
  CoveredInDataSet:     n
  AddColumn:            n  → [table.column ...]
  AddOneToOneLeftJoin:  n  → [Plm_Xxx ON ReferenceId ...]
  RequiresOneToN:       n  → [forces B; ListEdit path]
  Unmapped:             n  → [...]
  ReadonlySkip:         n

--- ListEdit (required when recommending B) ---
Existing ListEdit candidates (TransactionOrganizedType=List):
  [0] none found
  [1] #{id} IntegrationId=... Name=... root={Plm_Xxx} children=[{Plm_Yyy}]  matchScore=...
  [2] ...

If creating new ListEdit, proposed structure (CONFIRM before Phase B):
  IntegrationId: ListEdit_MU{muId}_{ShortName}
  Name:          {PLM MU name} ListEdit
  Root unit:     table={Plm_Header}  pk={ReferenceId}  fields=[PK + optional header display cols]
  Child unit(s): table={Plm_Grid_...} fk={ReferenceId}  fields=[MU GridColumn/SubItem mapped cols]
  Notes:         DynamicMatrix → warn limited support; propose RegularGrid-shaped child if possible

Choose one:
[ ] A — SingleTableAttach: enrich DataSet if needed (1:1 only), create Mass Update AppSearchView
        (IsMassUpdateView=true, map fields + PK, UpdateTransctionId/Unit)
[ ] B — HierarchicalListEdit: map root PK only on Mass Update View
        then confirm ListEdit:
        [ ] B1 — Use existing ListEdit #{id} / IntegrationId=...
        [ ] B2 — Create NEW ListEdit (approve structure above) — include listEditCreate in OUTPUT
[ ] C — Cancel / fix FieldMapping / import missing Template / reject proposed ListEdit structure
```

Decision guidance (agent recommendation):

| PLM UpdateType | Prefer |
|----------------|--------|
| **TabField** and majority fields on one header Unit | **A** |
| **RegularGrid** / **DynamicMatrix** / RequiresOneToN | **B** — always run ListEdit discovery |
| **B** + matching ListEdit found | Recommend **B1** (user must confirm which) |
| **B** + no ListEdit | Propose **B2** structure from FieldMapping + MU fields; **STOP for structure confirm** — do **not** invent without user OK |
| Mixed header + grid | Prefer **A** for header-only subset, or **B** if grid is the point of the MU |
| Unmapped grid table / no PK-FK path | **C** |

Do **not** generate Phase B files until the user selects A / B1 / B2 / C.  
For **B2**, do **not** generate until the user explicitly approves (or amends) the proposed ListEdit unit tree + field list.

---

## Phase B — After user decision

### Option A — SingleTableAttach (primary path)

Outputs under `output/{SearchTemplateId}/`:

```text
3_PlmSearch_MassUpdateView_{MassUpdateViewId}.json
README_MassUpdate_{MassUpdateViewId}.md   (optional)
```

Blueprint contents (minimum) — schema reference: [`source/9_PlmSearch_MassUpdateView.example.json`](source/9_PlmSearch_MassUpdateView.example.json):

- `mode`: **`MassUpdateViewAttach`**  
- `source`: `searchTemplateId`, `massUpdateViewId`, `updateType`, `mainTabId`, `tablePrefix`  
- `target`: `appSearchIntegrationId`, `appSearchId`, `appDataSetId`  
- `dataSetPatch`: `{ resultingQueryText?, addColumns[], addLeftJoins[] }` — **only 1:1 LEFT JOINs** (same rules as Sibling Option A)  
- `massUpdate`:
  - `appMode`: `SingleTableUpdate`
  - `updateTransactionIntegrationId` / `updateTransactionId`
  - `updateBaseTransactionUnitId` / unit IntegrationId if available
  - `isAllowAddRow` / `isAllowDeleteRow` / `isAllowAdvancedUpdate` (default true/true/true unless PLM implies otherwise)
  - `setAsDefaultMassUpdateView`: bool
- `searchView`: name (PLM MU name), `integrationIdLabel` e.g. `Search_{Name}_MU{Id}_View`, `viewType`: `GridView`, `isMassUpdateView`: true, `fields[]`  
- Each field: display, `sysTableFiledPath`, controlType, `isVisible`, `sort`, and when updatable:  
  `massUpdateTransactionFieldId` **or** resolvable `massUpdateTransactionFieldIntegrationId` / `databaseFieldName` on the Unit  
- **Must include PK** field with mass-update mapping  
- `coverage` summary counts  

**Do not** rewrite Search criteria. **Do not** create a second Search. **Do not** change display default View.

Phase D (Import Tool — **implemented**):

1. Open **PLM Data Import → PLM Search Import**  
2. Upload `3_PlmSearch_MassUpdateView_{MassUpdateViewId}.json` (mode `MassUpdateViewAttach` auto-detected)  
3. Validate Preview → Execute  

APIs: `LoadSearchMassUpdateViewBlueprint`, `ValidateSearchMassUpdateViewBlueprint`, `PreviewSearchMassUpdateViewConfig`, `ExecuteSearchMassUpdateViewConfig`.

Execute must:

1. Optionally patch DataSet QueryText (1:1 only).  
2. Create `AppSearchView` with `IsMassUpdateView=true`, `UpdateTransctionId`, `UpdateBaseTranscationUnitId`, allow flags.  
3. Create `AppSearchViewField` rows; set `MassUpdateTransactionFieldId` for updatable columns (incl. PK).  
4. Leave `AppSearch.SearchViewId` (display) unchanged.  
5. If `setAsDefaultMassUpdateView` — note in execute messages (manual assign may still be needed).

### Option B — HierarchicalListEdit

Outputs under `output/{SearchTemplateId}/` (same primary file; optional README):

```text
3_PlmSearch_MassUpdateView_{MassUpdateViewId}.json
README_MassUpdate_{MassUpdateViewId}.md   (optional — include approved ListEdit structure)
```

Schema reference: [`source/9b_PlmSearch_MassUpdateView_ListEdit.example.json`](source/9b_PlmSearch_MassUpdateView_ListEdit.example.json)

Common:

- `mode`: **`MassUpdateViewAttach`**
- `massUpdate.appMode`: **`HierarchicalTableUpdate`**
- Mass Update `searchView` fields: map **root PK only** to ListEdit **root unit PK** (`MassUpdateTransactionFieldId` / `massUpdateDatabaseFieldName`)
- Other PLM MU columns belong on the **ListEdit** units/forms (not full SearchView MU maps)
- DataSet enrich only if root PK / filter columns missing (**1:1** only)

#### B1 — Use existing ListEdit

- `listEditCreate.action`: **`UseExisting`**
- `listEditCreate.existingTransactionId` / `existingIntegrationId` = user-confirmed ListEdit
- `massUpdate.updateTransactionIntegrationId` points at that ListEdit
- Do **not** invent units; only attach Mass Update View

#### B2 — Create new ListEdit (user-approved structure)

**Gate:** User must have approved the proposed unit tree + field list in Phase A3.

Embed in the same blueprint (so Import Tool can create ListEdit then Mass Update in one Execute, or two-step if UI prefers):

- `listEditCreate.action`: **`CreateNew`**
- `listEditCreate.create`:
  - `integrationId`: `ListEdit_MU{MassUpdateViewId}_{ShortName}`
  - `name`, `transactionOrganizedType`: **`List`** (`EmTransactionOrganizedType.List = 3`)
  - `tenantDataSourceRegisterId` / `saasApplicationId` (same conventions as Search import)
  - `unitStructure`:
    - **root**: header / parent table (usually same grain as Search result PK, e.g. `Plm_*_Header` or `Plm_ReferenceBasicInfo`) — include PK field
    - **children**: 1:N grid table(s) from MU `GridColumnId` / FieldMapping — FK to root PK
  - `fields[]` per unit: from MU fields via FieldMapping (`appTableName`, `appColumnName`, controlType, visible, sort). Readonly MU fields → visible non-editable on ListEdit as appropriate
- `massUpdate.updateTransactionIntegrationId` = the **new** ListEdit IntegrationId (same as `listEditCreate.create.integrationId`)

**Do not** silently change an existing MasterDetail `Tab_*` into List. Always create a **dedicated** ListEdit IntegrationId for B2.

Phase D (Import Tool — **implemented**):

1. Upload `3_PlmSearch_MassUpdateView_{Id}.json`
2. If `listEditCreate.action = CreateNew` → create ListEdit Transaction + units **first** (hierarchy from tables, then `TransactionOrganizedType = List`)
3. Create Mass Update `AppSearchView` (`IsMassUpdateView=true`, `UpdateTransctionId` = ListEdit, root-PK field map only)
4. Leave display default View unchanged

Execute API: `ExecuteSearchMassUpdateViewConfig` (same Validate/Preview pair as Mode A).

### Option C

STOP. List blockers only (missing FieldMapping, no resolvable header/grid tables, user rejected ListEdit structure, Matrix cannot be modeled, etc.).

---

## Agent checklist

```text
[ ] Gate 0: PLM + APP + SearchTemplateId + MassUpdateViewId
[ ] APP Search exists (else STOP → main PROMPT.md)
[ ] Mass Update View not already imported
[ ] Probe pdmMassUpdateView + fields (+ link to SearchTemplate)
[ ] FieldMapping resolve + MasterDetail / ListEdit candidates
[ ] Classify MappedToTxnField / DataSet / AddColumn / 1:1 join / 1:N / Unmapped
[ ] If B likely: list existing ListEdits; if none, draft CreateNew structure
[ ] Recommend A / B1 / B2 / C
[ ] STOP — wait for user confirm (Transaction/Unit OR ListEdit pick OR ListEdit structure)
[ ] A / B1 / B2 → 3_PlmSearch_MassUpdateView_{Id}.json (mode MassUpdateViewAttach)
[ ] B2 includes listEditCreate.action=CreateNew with approved unitStructure + fields
[ ] No 1:N joins in dataSetPatch
[ ] Mode A includes Unit PK mass-update mapping
[ ] Mode B SearchView maps root PK only
[ ] Do not change display default SearchViewId
[ ] If Phase D fails → fix Blueprint / report BL error; do not invent columns
```

---

## Out of scope

- Editing [`PROMPT.md`](PROMPT.md) or [`PROMPT_SIBLING_VIEW.md`](PROMPT_SIBLING_VIEW.md) behavior  
- Changing DwBlueprint / `includeInSearch`  
- Importing `pdmMassUpdateViewMember` security or `pdmMassUpdateViewCalFlow` (document as later / manual)  
- Creating ListEdit **without** user confirmation of structure  
- Converting existing MasterDetail `Tab_*` Transactions into ListEdit  
- Full DynamicMatrix pivot UI parity (propose best-effort child unit; warn gaps)  
- Implementing C# / React Import Tool wiring **inside** a Prompt run (Phase D already lives in `PlmMigrationBL.SearchMassUpdateView` + Search Import step; Prompt only generates JSON)  
- Treating Mass Update as a Sibling display ReferenceView  

---

## Related

- [`PROMPT.md`](PROMPT.md) — first Search import (prerequisite)  
- [`PROMPT_SIBLING_VIEW.md`](PROMPT_SIBLING_VIEW.md) — extra **display** ReferenceViews only  
- Probe: [`source/_plm_probe_massupdate.sql`](source/_plm_probe_massupdate.sql), [`source/_app_probe_fieldmapping.sql`](source/_app_probe_fieldmapping.sql), [`source/_app_probe_search_context.sql`](source/_app_probe_search_context.sql)  
- Blueprint examples: [`source/9_PlmSearch_MassUpdateView.example.json`](source/9_PlmSearch_MassUpdateView.example.json) (Mode A), [`source/9b_PlmSearch_MassUpdateView_ListEdit.example.json`](source/9b_PlmSearch_MassUpdateView_ListEdit.example.json) (Mode B)  
- Phase D BL: `APP.BL/DataMigration/PlmMigration/PlmMigrationBL.SearchMassUpdateView.cs`  
- APP config UI notes: SearchViewEditor Mass Update Setting Notes (Single table vs Hierarchical ListEdit)  
- APP save: `AppTransactionDataMassUpdateBL.SaveMassUpdateResult` / ListEdit: `AppListEditFormDataLoadBL`
