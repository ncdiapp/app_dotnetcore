# PLM Data Warehouse → APP Template Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportFromPLMDW/`  
> **Outputs (after Phase B):** `output/PlmDw_Tables.sql`, `output/PlmDw_FieldMapping.sql`, `output/PlmDw_ImportFromDW.sql`  
> **Applies to:** any PLM **Template** (not a single product type). APP table names come from DW metadata, not from fixed names in this prompt.

---

## User input (only two items)

```text
1. DW connection string
   Server=...\Database=plmDW;...

2. TabIds to import
   comma-separated PLM Tab IDs, e.g. 4258,4212,4213,4270,4274,4219
```

Optional (defaults if omitted): `@TablePrefix` = `Plm_`, `@RootTableSuffix` = `ReferenceBasicInfo`, pilot `ReferenceId` for smoke test.

**Never hardcode template or product names** (e.g. Fabric) in generated **file names**. APP table names inside SQL are derived per template from DW table names (see §2.2).

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Two phases** | **Phase A:** DW analysis + APP table proposal → **STOP for user confirmation**. **Phase B:** generate SQL **after** confirm. |
| **plmDW is truth** | Column names, SubItem IDs, TabIds from DW — not legacy PLM exports. |
| **1 Tab → 1 APP table** | Tab wide tables only for tabs with `PLM_DW_Tab_*_{TabId}`. Grid-only tabs → `PLM_DW_Grid_*`. |
| **Mapping drives import** | `{prefix}FieldMapping` stores `DwTableName` + `DwColumnName` per APP column. |
| **Prefix is parameter** | `@TablePrefix` in all three SQL scripts (default `Plm_`). |

---

## Phase A — Discovery & analysis (STOP after)

### A1. Parse inputs

From connection string: `@SqlServer`, `@DwDatabase`. Auth: prefer `sqlcmd -E`; do not commit passwords.

Build `#TabInput(TabId)` from user TabId list.

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

Record decisions in `source/dwTabImportConfig.json` (copy from `dwTabImportConfig.example.json`).

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
  ]
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

Generator details:
- Reads `INFORMATION_SCHEMA` from plmDW  
- APP column names: strip `_SubItemId` / `_FK_*`; suffix `_SubItemId` on collisions  
- Mapping DELETE scoped to **tables in config only** (no `LIKE Fabric_%`)  
- INSERT values use doubled quotes inside `SET @sql = N'...'` → `N''@P@...''`  

### B3. `{prefix}FieldMapping` schema

`AppTableName`, `AppColumnName`, `DwTableName`, `DwColumnName`, `PlmTabId`, `PlmSubItemId`, `PlmGridSubItemId`, `PlmGridId`, `PlmMetaColumnId`, `PlmBlockId`, `DwFkTarget`, `FieldKind` (`TabField` | `GridColumn` | `ReferenceField`).

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
```

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
[ ] Parse connection string + TabIds
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
