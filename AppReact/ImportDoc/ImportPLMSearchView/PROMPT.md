# PLM Search / Search View → APP Search Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportPLMSearchView/`  
> **Outputs (after Phase B):** `output/{searchTemplateId}/1_PlmSearch_ImportBlueprint.json`  
> **Phase D (BL TOOLS — future):** `PlmMigration/ExecuteSearchBlueprintConfig` — consumes Blueprint JSON + tenant FieldMapping to create `AppSearch` / `AppSearchField` / `AppDataSet` / `AppSearchView` / link targets.  
> **Prerequisite:** Template DW import already completed (`Plm_*` tables + `{prefix}FieldMapping` populated).  
> **Does NOT modify:** DwBlueprint / `includeInSearch` on template blueprint — Search import is a **separate** lifecycle.

---

## User input (required — three items)

**The user must supply all three in the same message** (or in a follow-up before any probe work):

```text
1. PLM connection string (source DB — `pdmSearchTemplate`, `pdmSearchTemplateDCU`, `pdmReferenceView`, …)
   Server=...\Database=PLM;...

2. APP tenant connection string (FieldMapping + transactions)
   Server=...\Database=AppTenant;...

3. PLM SearchTemplateId — exactly ONE integer (`pdmSearchTemplate.SearchTemplateID`)
   e.g. 23702
```

Optional (defaults if omitted):

| Parameter | Default |
|-----------|---------|
| `@TablePrefix` | `Plm_` |
| `primaryTableName` | Agent proposes in Phase A (often a header sibling table, e.g. `Plm_Style_Header`; user may require it as JOIN driver) |
| `gridColumnStrategy` | `exclude` (Phase A user may override) |
| `saasApplicationId` | null |
| `tenantDataSourceRegisterId` | first default from `AppDataSourceRegister` |

**Join plans are NOT user input at Gate 0.** Agent derives JOIN candidates from PLM probe + FieldMapping, ranks them, presents **3–10 semantic options** (or fewer if only 2–3 are meaningful), then **STOPS for user selection**.

### Gate 0 — missing input → ask user, do nothing else

If the user **only** references this file (e.g. `@PROMPT.md`) and does **not** include **all three** required items:

1. **STOP immediately.** Do **not** run `sqlcmd`, probe SQL, Phase A analysis, or Phase B generation.
2. **Ask the user** for PLM connection + APP tenant connection + **one** SearchTemplateId.
3. **Do not** treat example JSON, prior chat SearchIds, or `ImportFromPLMDW` TemplateIds as user input.

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Gate 0** | No PLM + APP connections **and** one SearchTemplateId → **ask only**; no probe. |
| **No server code** | Deliverables are **SQL + JSON in this folder only** — no C# / WebAPI edits during PROMPT runs. BL gap → **STOP**, explain, warn user. |
| **Two phases** | **Phase A:** probe + coverage + JOIN plans → **STOP for user confirmation**. **Phase B:** generate Blueprint JSON after confirm. **Phase D:** user runs BL in app (future `SearchImportStep`). |
| **FieldMapping is truth** | APP column resolution goes through `{prefix}FieldMapping` (`PlmSubItemId`, `PlmMetaColumnId`). Never invent column names. |
| **Do not copy PLM BlQuery SQL** | `pdmBlQuery.SqlText` is **reference only**. APP `DataSet.QueryText` is **synthesized** from selected JOIN plan + FieldMapping. |
| **Criteria ≠ View** | Score JOIN plans separately for criteria coverage and view coverage. One plan may not fit both — report explicitly. |
| **Grid 1:N warning** | `FieldKind = GridColumn` tables are **1:N**. Default `gridColumnStrategy = exclude`. Other options: `scalar` (subquery), `accept-1N` (user accepts row multiplication). |
| **Prefix is parameter** | All APP table names respect `@TablePrefix`. |
| **Cleanup temp probes** | Probe SQL under `source/` is permanent. Scratch dumps written during a run (`output/_probe_*`, ad-hoc `.sql`/`.txt` under `output/` root — not under `output/{SearchTemplateId}/`) must be **deleted** when the run finishes (Phase B done, cancel/stop after probes, or Gate abort after any probes). Do not leave them for the user. |

---

## Folder layout

```text
ImportPLMSearchView/
  PROMPT.md                              ← first Search import (this file)
  PROMPT_SIBLING_VIEW.md                 ← extra display ReferenceView
  PROMPT_MASSUPDATE_VIEW.md              ← Mass Update View (after Search exists)
  source/
    _plm_probe_search.sql              ← PLM: Search / Parameter / ViewColumn / tab affinity
    _plm_probe_massupdate.sql          ← PLM: MassUpdateView + fields
    _app_probe_fieldmapping.sql        ← APP: FieldMapping inventory + SubItem/MetaColumn resolve
    _app_probe_search_context.sql      ← APP: transactions, data sources, existing searches
    plmSearchImportConfig.example.json ← Phase B working config (agent-written)
    7_PlmSearch_ImportBlueprint.example.json  ← schema reference + example
    8_PlmSearch_SiblingView.example.json
    9_PlmSearch_MassUpdateView.example.json
    9b_PlmSearch_MassUpdateView_ListEdit.example.json
  output/
    {searchTemplateId}/                ← one subfolder per SearchTemplateID (e.g. 23702/)
      1_PlmSearch_ImportBlueprint.json
      2_PlmSearch_SiblingView_{viewId}.json      (optional)
      3_PlmSearch_MassUpdateView_{muId}.json     (optional)
      README.md                        ← optional: Phase A notes + user selections
```

**If `source/` is deleted:** restore from repo before running PROMPT.

---

## Phase A — Discovery & analysis (STOP after)

### A1. Parse inputs

From connection strings: `@PlmSqlServer`, `@PlmDatabase`, `@AppSqlServer`, `@AppDatabase`, `@SearchId`.  
Auth: `sqlcmd -E` or env credentials; **do not commit passwords**.

Verify `{prefix}FieldMapping` exists on APP DB. If missing → **STOP** — user must run Template DW import first (`ImportFromPLMDW`).

### A2. PLM probe

Run `source/_plm_probe_search.sql` with `@SearchTemplateId` against **PLM** database.

**Verified PLM tables (plm_live):**

| Table | Role |
|-------|------|
| `pdmSearchTemplate` | Search shell — `SearchTemplateID`, `Name`, `ReferenceViewID`, `BLQueryID` |
| `pdmSearchTemplateDCU` | Criteria (upper panel) — `SubitemID`, `DisplayText`, `ControlType`, `OperationID`, `DefaultValue`, layout |
| `pdmReferenceView` | View shell — linked via `ReferenceViewID` |
| `pdmReferenceViewColumn` | Grid columns — `SubItemID`, `GridColumnID`, `Sort`, `IsVisible` |
| `pdmBLQuery` | Optional SQL body — often **NULL**; synthesize DataSet instead |

Record from probe output:

| Section | Use |
|---------|-----|
| §1 Search template | Name, `ReferenceViewID`, `BLQueryID` |
| §2 DCU criteria | Criteria fields + tab affinity (§2b) |
| §4 View columns | Result columns + grid/sub-item enrichment (§4b) |
| §5 BLQuery | **Reference only** — do not copy |
| §6 Tab affinity | JOIN plan ranking |

User may require **primary table ≠ ReferenceBasicInfo** (e.g. `FROM Plm_Style_Header` with LEFT JOINs) — honor in Phase B `dataSet.queryText`.

### A3. APP FieldMapping probe

Run `source/_app_probe_fieldmapping.sql` on **APP tenant** DB.

Populate `#WantedSubItems` and `#WantedMetaColumns` from PLM probe §2/§4, re-run resolve sections.

Run `source/_app_probe_search_context.sql` for transactions (`Tab_{id}`) and data sources.

### A4. Coverage report (mandatory before JOIN plans)

For **each** PLM criteria parameter and view column, classify:

| Status | Meaning |
|--------|---------|
| **Mapped** | Exactly one `AppTableName` + `AppColumnName` in FieldMapping |
| **Ambiguous** | Same `PlmSubItemId` → multiple APP tables (user must pick per JOIN plan) |
| **Missing** | No FieldMapping row — list `PlmSubItemId` / `PlmMetaColumnId`; suggest which Template to import |
| **Grid** | `FieldKind = GridColumn` — subject to `gridColumnStrategy` |
| **BuiltIn** | ReferenceId / ReferenceCode — map to `Plm_ReferenceBasicInfo` even without SubItem |

**If criteria mapped < 50%** → warn user strongly; JOIN plans may not be useful until more templates are imported.

Output template:

```text
=== Coverage ===
Criteria: 12 total — 10 mapped, 1 ambiguous, 1 missing
View:     15 total — 12 mapped, 0 ambiguous, 1 missing, 2 grid

Missing:
  - SubItemId 9999 (Season filter) — not in FieldMapping; import Template 3356?

Ambiguous:
  - SubItemId 1234 → Plm_Style_Header | Plm_Style_Alt
```

### A5. JOIN plan generation (core Phase A deliverable)

Build candidate APP table sets that can supply all **mapped** fields.

**Cardinality rules:**

| FieldKind | JOIN role | Default |
|-----------|-----------|---------|
| `ReferenceField`, root columns | `root` | `Plm_ReferenceBasicInfo` |
| `TabField` on tab wide table | `sibling` | `INNER JOIN ON ReferenceId` (1:1) |
| `GridColumn` | `grid` | **exclude** unless user picks `scalar` or `accept-1N` |

**Scoring heuristics** (apply explicitly, show score breakdown):

1. **Criteria coverage** (weight 35%) — % of criteria fields resolvable from tables in plan  
2. **View coverage** (weight 35%) — % of view fields resolvable  
3. **Tab affinity** (weight 15%) — % of fields whose `PlmTabId` matches a table in plan  
4. **Header tab bonus** (weight 10%) — includes table mapped to `IsTemplateHeaderTab`  
5. **Simplicity** (weight 5%) — fewer tables = higher  

**Prune combinatorics:**

- For each `PlmSubItemId`, pick **at most one** APP table per plan  
- Do not include grid tables when `gridColumnStrategy = exclude`  
- Prefer plans where majority of fields share the same dominant `PlmTabId`

**Output 3–10 plans** (or fewer with explanation). Example:

```text
Option A (score 92): Plm_ReferenceBasicInfo + Plm_Style_Header
  Criteria 12/12 | View 13/15 (2 grid excluded) | Tab 4250 affinity 85%
  Semantic: Style master list, 1 Reference = 1 row

Option B (score 78): Plm_ReferenceBasicInfo + Plm_Style_Header + Plm_Style_Details
  Criteria 12/12 | View 14/15 | more columns, extra JOIN

Option C (score 45): ... includes Plm_Bom_Grid — accept-1N only
```

### A6. User confirmation checklist (STOP here)

Present and **wait**:

```text
[ ] Select JOIN plan: A / B / C / ...
[ ] Grid column strategy: exclude | scalar | accept-1N
[ ] Reference scope: AllReferences | ByTemplateIds [list] | CustomFilter
[ ] Missing fields: ignore | stop and import Template ____ first
[ ] Link target transaction: Tab_{id} for Open/Create
[ ] Search IntegrationId: Search_{Name} (confirm no collision)
[ ] Register in main menu? yes/no
```

Do **not** generate Phase B files until user confirms.

---

## Phase B — Generate Blueprint JSON (after user confirm)

1. Write `source/plmSearchImportConfig.json` (working config — not committed with secrets).  
2. Synthesize `dataSet.queryText` from selected JOIN plan:
   - `SELECT` all resolved view columns + criteria columns needed  
   - `FROM` root table  
   - `INNER JOIN` siblings on `ReferenceId`  
   - Grid `scalar`: `(SELECT TOP 1 col FROM grid WHERE grid.ReferenceId = root.ReferenceId)`  
   - Add `WHERE` from `referenceScope` if user specified  
3. Build `fieldResolution[]` — one entry per PLM field with resolved APP column + control metadata from FieldMapping (`PlmControlType`, `PlmEntityId`).  
4. Split into `criteriaFields[]` and `searchView.fields[]`.  
5. Map PLM default operators → APP `operationId` (see §Operator mapping).  
6. Write `output/{searchId}/1_PlmSearch_ImportBlueprint.json`.  
7. Optional: `output/{searchId}/README.md` with Phase A notes.

**Schema reference:** `source/7_PlmSearch_ImportBlueprint.example.json`

### Operator mapping (PLM → APP)

PLM `pdmSearchTemplateDCU.OperationID` uses the **same numeric values** as APP `EmAppCriteriaOperatorType` (stored on `AppSearchField.OperationID`). Copy PLM `OperationID` as-is into blueprint `operationId`. Do **not** use `EmAppWijmoOperator` (grid filter enum — different numbering).

| PLM / UI hint | APP `operationId` | `EmAppCriteriaOperatorType` |
|---------------|-------------------|-----------------------------|
| = / Equals | **0** | Equals |
| Is null | 1 | Null |
| Is not null | 2 | NotNull |
| > | 3 | GreaterThan |
| >= | 4 | GreaterThanOrEquals |
| < | 5 | LessThan |
| <= | 6 | LessThanOrEquals |
| Like / Contains | **7** | Like |
| Is empty | 8 | NullOrEmpty |
| Is not empty | 9 | NotNullOrEmpty |
| Starts with | 10 | StartWith |
| Ends with | 11 | EndWith |
| In | 12 | In |
| <> / Not equal | 13 | NotEqual |
| Between | 14 | Between |

Also copy PLM `pdmSearchTemplateDCU.DefaultValue` → blueprint `defaultValue` (string; omit or `null` when empty). Phase D writes both to `AppSearchField`.

### ControlType / EntityId

- Use `PlmControlType` and `PlmEntityId` from FieldMapping row for resolved column  
- `entityIntegrationId` = string of `PlmEntityId` (matches `AppEntityInfo.IntegrationId`)  
- `IsTransRootId = true` on `ReferenceId` view field when linking to transactions  
- Image/sketch columns: `ControlType = 5` (Image) when `PlmControlType = 5`

---

## Phase D — APP configuration (BL TOOLS — future)

After Blueprint JSON is generated:

1. Open **PLM Data Import → Search Import** step (`SearchImportStep.tsx` — future), or call API  
2. Upload `output/{searchId}/1_PlmSearch_ImportBlueprint.json`  
3. **Validate & Preview** — `ValidateSearchImportBlueprint` + `PreviewSearchBlueprintConfig`  
4. **Execute Insert / Update** — `ExecuteSearchBlueprintConfig`

**Agent scope:** Phase D is executed by the **user in the running app**. Agent generates files only.

**BL responsibilities (when implemented):**

- Resolve `entityIntegrationId` → `AppEntityInfo.EntityInfoID`  
- `AppSearchConfigBL.SaveAppSearchExDto` with `criteriaFields`  
- `AppSearchViewConfigBL.SaveAppSearchViewExDto` with view fields  
- `AppFormLinkTarget` for Open/Create/Delete  
- `AppDatabaseViewBL.AddSearchToApplicationMainMenu` when `menu.registerInMainMenu`  
- Idempotent update by `integrationId`

Reference implementation pattern: `PlmMigrationBL.PomImport.cs` (`EnsurePomListSearch`, `BuildPomListViewFields`, `SaveSearchView`).

---

## Agent checklist

```text
[ ] Gate 0: PLM + APP connections + one SearchId? If not → ask and STOP
[ ] Verify Plm_FieldMapping exists on APP DB
[ ] Run _plm_probe_search.sql → record Search name, BlQueryId, parameters, view columns
[ ] Run _app_probe_fieldmapping.sql with #WantedSubItems / #WantedMetaColumns
[ ] Run _app_probe_search_context.sql → Tab_{id} transactions, data sources
[ ] Build coverage report (mapped / ambiguous / missing / grid)
[ ] Generate 3–10 JOIN plans with scores — WAIT FOR USER
[ ] User selects plan + grid strategy + reference scope + link targets
[ ] Write plmSearchImportConfig.json
[ ] Generate output/{searchId}/1_PlmSearch_ImportBlueprint.json
[ ] Verify queryText uses only tables/columns from FieldMapping
[ ] Verify integrationIds unique vs existing AppSearch
[ ] Delete output/_probe_* (and other output-root scratch dumps) before ending the run
```

---

## Example session message

**Insufficient:**

```text
@AppReact/ImportDoc/ImportPLMSearchView/PROMPT.md
```

**Sufficient:**

```text
按 AppReact/ImportDoc/ImportPLMSearchView/PROMPT.md 执行。

PLM connection string:
  Data Source=PC3B\MSSQLSERVER01;Initial Catalog=PLM;...

APP tenant connection string:
  Data Source=PC3B\MSSQLSERVER01;Initial Catalog=AppTenant;...

PLM SearchTemplateId:
  23702
```

---

## Out of scope

- Modifying DwBlueprint / template `includeInSearch`  
- Copying PLM BlQuery SQL verbatim  
- Saved searches / user views / pivot views (phase 2+)  
- PLM row-level security filters  
- Physical data import (tables already imported via `ImportFromPLMDW`)  
- C# / React implementation (separate tasks: `ExecuteSearchBlueprintConfig`, `SearchImportStep.tsx`)

---

## Related docs

- Template DW import: `AppReact/ImportDoc/ImportFromPLMDW/PROMPT.md`  
- Sibling display View: [`PROMPT_SIBLING_VIEW.md`](PROMPT_SIBLING_VIEW.md)  
- Mass Update View: [`PROMPT_MASSUPDATE_VIEW.md`](PROMPT_MASSUPDATE_VIEW.md)  
- POM search import (BL reference): `APP.BL/DataMigration/PlmMigration/PlmMigrationBL.PomImport.cs`
