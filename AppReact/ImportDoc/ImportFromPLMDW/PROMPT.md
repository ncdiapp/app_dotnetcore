# PLM Data Warehouse → APP Template Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportFromPLMDW/`  
> **Outputs (after Phase B):** `output/PlmDw_Tables.sql`, `output/PlmDw_FieldMapping.sql`, `output/PlmDw_ImportFromDW.sql`, `output/PlmDw_ImportBlueprint.json`  
> **Phase D (BL TOOLS):** `PlmMigration/ExecuteDwBlueprintConfig` — consumes physical tables + FieldMapping + Blueprint to create Transaction / Form / Search / navigation.
> **Applies to:** any PLM **Template** (not a single product type). APP table names come from DW metadata, not from fixed names in this prompt.

---

## User input (required — two items)

**The user must supply both in the same message** (or in a follow-up before any DW work):

```text
1. DW connection string
   Server=...\Database=plmDW;...

2. TabIds to import
   comma-separated PLM Tab IDs, e.g. 4258,4212,4213,4270,4274,4219
```

Optional (defaults if omitted): `@TablePrefix` = `Plm_`, `@RootTableSuffix` = `ReferenceBasicInfo`, pilot `ReferenceId` for smoke test.

**Never hardcode template or product names** (e.g. Fabric) in generated **file names**. APP table names inside SQL are derived per template from DW table names (see §A3).

### Gate 0 — missing input → ask user, do nothing else

If the user **only** references this file (e.g. `@PROMPT.md`) and does **not** include **both** a connection string **and** TabIds in that message:

1. **STOP immediately.** Do **not** run `sqlcmd`, probe SQL, Phase A analysis, or Phase B generation.
2. **Ask the user** for the two required items (use the template under §Example session message).
3. **Do not** treat any of the following as user input:
   - The example connection string or TabIds in §Example session message
   - `source/dwTabImportConfig.example.json`
   - `source/dwTabImportConfig.json` (working file from a **previous** run — not valid until Phase B after user confirms Phase A)
   - Values inferred from other folders (e.g. `ImportDoc/Temp/`) or from prior chat unless the user repeats them in the current request

**Wrong:** user sends only `@PROMPT.md` → agent connects to `PC3B\...` and probes TabIds from the doc.  
**Right:** user sends only `@PROMPT.md` → agent replies asking for connection string + TabIds, then waits.

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Gate 0** | No connection string **and** TabIds from the user → **ask only**; no DW access, no Phase A/B (see §Gate 0). |
| **Two phases** | **Phase A:** DW analysis + APP table proposal + **Blueprint draft** → **STOP for user confirmation**. **Phase B:** generate SQL + Blueprint JSON **after** confirm. **Phase D:** BL TOOLS apply Blueprint to APP config (separate step). |
| **plmDW is truth** | Column names, SubItem IDs, TabIds from DW — not legacy PLM exports. |
| **1 Tab → 1 APP table** | Tab wide tables only for tabs with `PLM_DW_Tab_*_{TabId}`. Grid-only tabs → `PLM_DW_Grid_*`. |
| **Mapping drives import** | `{prefix}FieldMapping` stores `DwTableName` + `DwColumnName` per APP column. |
| **Prefix is parameter** | `@TablePrefix` in all three SQL scripts (default `Plm_`). |

---

## Phase A — Discovery & analysis (STOP after)

### A1. Parse inputs

**Prerequisite:** Gate 0 passed — user supplied connection string **and** TabIds in the current session.

From connection string: `@SqlServer`, `@DwDatabase`. Auth: prefer `sqlcmd -E`; do not commit passwords.

Build `#TabInput(TabId)` from the **user-provided** TabId list only (not from example JSON or repo config files).

### A2. Resolve DW objects per TabId

```sql
-- Tab wide table (0 or 1 per TabId)
SELECT t.name FROM sys.tables t
WHERE t.name LIKE N'PLM_DW_Tab[_]%'
  AND t.name LIKE N'%\_' + CAST(@TabId AS NVARCHAR(20)) ESCAPE N'\';
```

| Match count | Meaning |
|-------------|---------|
| 1 | Tab wide table → 1:1 APP table |
| 0 | Grid-only or missing → find `PLM_DW_Grid_*` in Phase A; ask user |
| >1 | Error — ambiguous TabId |

Probe helper: `source/_dw_probe_by_tabids.sql` (populate `#TabInput` first).

List all grids: `SELECT name FROM sys.tables WHERE name LIKE 'PLM_DW_Grid_%'`.

### A3. Derive APP logical table names

From DW tab table name `PLM_DW_Tab_{Segment}_{TabId}`:

- **APP table name** = `{Segment}` (middle part), e.g. `Fabric_Header`, `Attributes`, `Testing____Compliance`
- From DW grid `PLM_DW_Grid_{Segment}_{GridMetaId}`:
- **APP grid table name** = `{Segment}` (e.g. `ProductDesignColorGrid`)

Present TabId inventory to user:

| TabId | DwTableName | APP table | Columns | Type |
|-------|-------------|-----------|---------|------|

### A4. DW column naming

```
{Name}_{SubItemId}  |  {Name}__{SubItemId}  |  {Name}_{SubItemId}_FK_{target}
```

System columns (not mapped): Tab → `TabID`, `ProductReferenceID`; Grid → `ProductReferenceID`, `BlockID`, `GridID`, `RowID`, `RowValueGUID`, `Sort`.

### A5. SubItem sharing (among TabIds in user list)

When **two tab wide tables overlap** (common: a “header” tab and a richer “info” tab):

- Report shared / tab-A-only / tab-B-only SubItem counts
- **Recommend:** store shared SubItems in **one** APP table only; secondary tab keeps **only exclusive** SubItems (`excludeSubItemsFromDwTable` in config)

When tabs are independent (no shared SubItemIds), each APP table gets **all** DW columns.

Run overlap SQL ad hoc or extend `_dw_probe_by_tabids.sql` for the specific tab pair the user’s TabIds imply — **do not assume header/info names**; detect by SubItem intersection.

### A6. Normalize / denormalize proposal

Present scoped to **user TabIds only**:

| APP object | Rule |
|----------|------|
| `{prefix}ReferenceBasicInfo` | `ReferenceId` = `ProductReferenceID`; `ReferenceCode` from user-chosen reference-scope column on one tab (propose default: code/article column on primary tab) |
| Each tab wide table | All columns, or exclusive SubItems if overlap rule applies |
| Each grid | Business columns + `RowId` / `ReferenceId` / `Sort` |
| Grids without Tab wide table | No tab DDL; grid table only |

### A7. Confirmation checklist — **STOP**

Ask user to confirm (adapt wording to actual tab names):

1. TabId → APP table mapping  
2. Overlap / exclusive SubItem split (if any)  
3. Grid ↔ TabId associations  
4. Reference-scope DW table + column for `ReferenceCode`  
5. Skip tabs/grids with no DW source  
6. Mapping table schema (§Phase B3)  
7. `@TablePrefix` default `Plm_` OK?  
8. **Transaction Group** name and `integrationId` (Blueprint)  
9. **Per TabId → Transaction** unit structure (Root / Sibling / Child) and `fieldPolicy`  
10. **Blueprint field counts** per Transaction vs FieldMapping rows (shared columns must not duplicate on secondary tab)

After user confirms Phase A, record decisions in `source/dwTabImportConfig.json` (structure per `dwTabImportConfig.example.json` — include `blueprint` node — **do not** copy example TabIds/server without user input).

---

## Phase B — Generate SQL (after user confirms)

### B1. Write `source/dwTabImportConfig.json`

```json
{
  "importTabIds": [ ... ],
  "sqlServer": "...",
  "dwDatabase": "plmDW",
  "tablePrefixDefault": "Plm_",
  "rootTableSuffix": "ReferenceBasicInfo",
  "referenceScope": {
    "dwTable": "PLM_DW_Tab_...",
    "dwColumn": "...",
    "plmTabId": 0,
    "plmSubItemId": 0
  },
  "tabs": [
    { "appTable": "...", "dwTable": "...", "tabId": 0, "mode": "all" },
    { "appTable": "...", "dwTable": "...", "tabId": 0,
      "mode": "excludeSubItemsFromDwTable", "excludeSubItemsFromDwTable": "..." }
  ],
  "grids": [
    { "appTable": "...", "dwTable": "PLM_DW_Grid_...", "gridSubItemId": 0, "gridId": 0 }
  ],
  "blueprint": {
    "templateName": "...",
    "transactionGroupName": "...",
    "transactionGroupIntegrationId": "TG_...",
    "searchName": "... References",
    "searchIntegrationId": "Search_...",
    "folderName": "..."
  }
}
```

- `appTable` = logical name from DW segment (no template prefix in name)  
- `mode`: `all` | `excludeSubItemsFromDwTable`  
- `gridSubItemId` = PLM grid container SubItem on tab; `gridId` = DW grid metadata id  

### B2. Run generator

```powershell
powershell -File AppReact/ImportDoc/ImportFromPLMDW/source/_gen_plmdw_import_sql.ps1
```

Requires `source/dwTabImportConfig.json` (not committed with secrets; example is `dwTabImportConfig.example.json`).

**Produces in `output/`:**

| File | Content |
|------|---------|
| `output/PlmDw_Tables.sql` | `{prefix}ReferenceBasicInfo` + tab/grid tables from config |
| `output/PlmDw_FieldMapping.sql` | `{prefix}FieldMapping` DDL + seed |
| `output/PlmDw_ImportFromDW.sql` | Copied from `source/PlmDw_ImportFromDW.sql` template |
| `output/PlmDw_ImportBlueprint.json` | Transaction / Form / Search **configuration plan** for BL TOOLS |
| `output/PlmDw_ImportBlueprint.sql` | Optional tenant table `{prefix}ImportBlueprint` + JSON seed |

Generator details:
- Reads `INFORMATION_SCHEMA` from plmDW  
- APP column names: strip `_SubItemId` / `_FK_*`; suffix `_SubItemId` on collisions  
- Mapping DELETE scoped to **tables in config only** (no `LIKE Fabric_%`)  
- INSERT values use doubled quotes inside `SET @sql = N'...'` → `N''@P@...''`  

### B3. `{prefix}FieldMapping` schema

`AppTableName`, `AppColumnName`, `DwTableName`, `DwColumnName`, `PlmTabId`, `PlmSubItemId`, `PlmGridSubItemId`, `PlmGridId`, `PlmMetaColumnId`, `PlmBlockId`, `DwFkTarget`, `FieldKind` (`TabField` | `GridColumn` | `ReferenceField`), `PlmControlType`, `PlmEntityId`, `DwDataType`.

### B3b. `PlmDw_ImportBlueprint.json`

Describes Transaction Group, per-Tab Transaction unit structure (`RootPlusMasterSibling`), `fieldPolicy` (`AllMappedColumns` | `ExclusiveSubItemsOnly`), grid bindings, field UI metadata (`blueprintFields`), and Search/View/navigation targets. Generated from `dwTabImportConfig.json` + DW column probe. BL TOOLS: `PlmMigration/ValidateDwImportBlueprint`, `PreviewDwBlueprintConfig`, `ExecuteDwBlueprintConfig`.

### B4. `output/PlmDw_ImportFromDW.sql`

Template maintained at `source/PlmDw_ImportFromDW.sql`; copied into `output/` when the generator runs. Mapping-driven import. Parameters: `@TablePrefix`, `@RootTableSuffix`, `@DwDatabase`, `@ImportMode`, `@ReferenceIdList`, `@DryRun`.

Reference list source = `referenceScope.dwTable` via `ReferenceField` mapping (not hardcoded).

---

## Execution order (APP tenant DB)

Run scripts from **`output/`**:

```text
1. output/PlmDw_Tables.sql
2. output/PlmDw_FieldMapping.sql
3. output/PlmDw_ImportFromDW.sql
4. (optional) output/PlmDw_ImportBlueprint.sql — store Blueprint JSON in tenant DB
```

## Phase D — APP configuration (BL TOOLS)

After physical tables are populated, open **PLM Data Import → Step 3 DW Blueprint** in the app, or call the API directly:

1. Upload `output/PlmDw_ImportBlueprint.json` (or **Load from tenant DB** if `PlmDw_ImportBlueprint.sql` was run)
2. **Validate & Preview** — runs `ValidateDwImportBlueprint` + `PreviewDwBlueprintConfig`
3. **Execute Insert** or **Execute Update** — `ExecuteDwBlueprintConfig`

API equivalents: `POST webapi/PlmMigration/ValidateDwImportBlueprint`, `PreviewDwBlueprintConfig`, `ExecuteDwBlueprintConfig`.

---

## Folder layout

```text
ImportFromPLMDW/
  PROMPT.md
  output/                           ← deliverables (generated each run)
    PlmDw_Tables.sql
    PlmDw_FieldMapping.sql
    PlmDw_ImportFromDW.sql
  source/
    dwTabImportConfig.example.json
    dwTabImportConfig.json          ← Phase B working config
    _gen_plmdw_import_sql.ps1       ← writes to ../output/
    PlmDw_ImportFromDW.sql          ← import template
    _dw_probe_by_tabids.sql
```

**If `source/` is deleted:** PROMPT does **not** auto-recreate it. Agent must restore from repo or rewrite tools from §Phase B.

---

## Agent checklist

```text
[ ] Gate 0: user provided connection string + TabIds? If not → ask and STOP
[ ] Parse connection string + TabIds (from user message only)
[ ] Resolve PLM_DW_Tab_* / PLM_DW_Grid_* per TabId
[ ] SubItem overlap analysis for tab pairs in scope
[ ] Propose APP tables (names from DW, not hardcoded template)
[ ] Phase A checklist → WAIT FOR USER
[ ] Write dwTabImportConfig.json
[ ] Run _gen_plmdw_import_sql.ps1 → output/PlmDw_*.sql (3 files)
[ ] Verify output/PlmDw_FieldMapping.sql quoting + row counts
[ ] Optional: pilot output/PlmDw_ImportFromDW.sql with one ReferenceId
```

---

## Example session message

**Illustration only — not defaults.** The agent must not use these values unless the user pastes them (or equivalent) in their message.

**Insufficient** (agent must ask for the two required items):

```text
@AppReact/ImportDoc/ImportFromPLMDW/PROMPT.md
```

**Sufficient** (agent may start Gate 0 → Phase A):

```text
按 AppReact/ImportDoc/ImportFromPLMDW/PROMPT.md 执行。

DW connection string:
  Server=PC3B\MSSQLSERVER01;Database=plmDW;Trusted_Connection=True;

TabIds to import:
  4258,4212,4213,4270,4274,4219
```

(Example TabIds happen to be one fabric template; another template will have different TabIds, DW segments, and APP table names.)

---

## Out of scope

- Template Import Wizard / auto transaction builder  
- `PlmBlockId` backfill  
- C# import service  
- Full production load without explicit user request  
