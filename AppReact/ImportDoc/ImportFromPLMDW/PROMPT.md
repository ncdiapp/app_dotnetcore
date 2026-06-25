# PLM Data Warehouse → APP Template Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportFromPLMDW/`  
> **Outputs (after Phase B):** `output/{templateId}/PlmDw_Tables.sql`, `output/{templateId}/PlmDw_FieldMapping.sql`, `output/{templateId}/PlmDw_ImportFromDW.sql`, `output/{templateId}/PlmDw_ImportBlueprint.json` (e.g. `output/3351/` for TemplateId 3351)  
> **Phase D (BL TOOLS):** `PlmMigration/ExecuteDwBlueprintConfig` — consumes physical tables + FieldMapping + Blueprint to create Transaction / Form / Search / navigation.
> **Applies to:** any PLM **Template** (not a single product type). APP table names come from DW metadata, not from fixed names in this prompt.

---

## User input (required — three items)

**The user must supply all three in the same message** (or in a follow-up before any probe work):

```text
1. PLM connection string (source DB — pdmTemplate, pdmTemplateTab, pdmProductTemplate)
   Server=...\Database=PLM;...

2. plmDW connection string
   Server=...\Database=plmDW;...

3. TemplateId to import — exactly ONE integer
   e.g. 12
```

Optional (defaults if omitted): `@TablePrefix` = `Plm_`, `@RootTableSuffix` = `ReferenceBasicInfo`, pilot `@ReferenceIdList` for smoke test.

**TabIds are NOT user input.** Agent loads the tab list from PLM (`pdmTemplateTab` + `pdmTab`) for the given `TemplateId`, then probes plmDW per tab.

**Never hardcode template or product names** in generated **file names**. APP table names inside SQL are derived per template from DW metadata (see §A3). Transaction Group name defaults from `pdmTemplate.TemplateName` (user confirms in Phase A).

### Gate 0 — missing input → ask user, do nothing else

If the user **only** references this file (e.g. `@PROMPT.md`) and does **not** include **all three** required items in that message:

1. **STOP immediately.** Do **not** run `sqlcmd`, probe SQL, Phase A analysis, or Phase B generation.
2. **Ask the user** for PLM connection string + plmDW connection string + **one** TemplateId (see §Example session message).
3. **Do not** treat any of the following as user input:
   - Example connection strings or TemplateId in §Example session message
   - `source/dwTabImportConfig.example.json`
   - `source/dwTabImportConfig.json` (working file from a **previous** run — not valid until Phase B after user confirms Phase A)
   - TabId lists from prior chats, example JSON, or other folders unless the user repeats TemplateId + connections in the current request

**Wrong:** user sends only `@PROMPT.md` → agent connects and guesses TemplateId / TabIds from a prior Fabric run.  
**Right:** user sends only `@PROMPT.md` → agent asks for the three required items, then waits.

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Gate 0** | No PLM + plmDW connection strings **and** one TemplateId from the user → **ask only**; no probe, no Phase A/B (see §Gate 0). |
| **No server code** | **Never** edit C# / `.csproj` / React WebAPI / BL under `APP.BL`, `AppAI.Web`, etc. **Never** run `dotnet build`, `msbuild`, or any compile step. Deliverables are **SQL + JSON + PowerShell in this folder only**. If the workflow appears to need a code change → **STOP immediately**, explain the gap, and **warn the user** — do not patch the repo. |
| **Two phases** | **Phase A:** DW analysis + APP table proposal + **Blueprint draft** → **STOP for user confirmation**. **Phase B:** generate SQL + Blueprint JSON **after** confirm. **Phase D:** BL TOOLS apply Blueprint to APP config (separate step; user runs in app). |
| **plmDW is truth** | Column names, SubItem IDs, TabIds from DW — not legacy PLM exports. |
| **1 Tab → 1 APP table** | Tab wide tables only for tabs with `PLM_DW_Tab_*_{TabId}`. Grid-only tabs → `PLM_DW_Grid_*`. |
| **Mapping drives import** | `{prefix}FieldMapping` stores `DwTableName` + `DwColumnName` per APP column. |
| **Prefix is parameter** | `@TablePrefix` in all three SQL scripts (default `Plm_`). |

---

## Phase A — Discovery & analysis (STOP after)

### A1. Parse inputs

**Prerequisite:** Gate 0 passed — user supplied **PLM connection string**, **plmDW connection string**, and **one TemplateId**.

From connection strings: `@PlmSqlServer`, `@PlmDatabase`, `@DwSqlServer`, `@DwDatabase`. Auth: `sqlcmd -E` or env `PLM_DW_SQL_USER` / `PLM_DW_SQL_PASSWORD`; **do not commit passwords**.

### A1b. Load template + tab list from PLM (authoritative)

Run `source/_plm_probe_template.sql` with `@TemplateId` set, against **PLM** database.

Equivalent query (same rules as `PlmMigrationBL.LoadPlmTemplateTabs`):

```sql
SELECT t.TemplateID, t.TemplateName, t.Description,
       tt.TabID, tab.TabName, tt.Sort,
       tab.IsTemplateHeaderTab, tab.IsMasterReferenceHeaderTab
FROM dbo.pdmTemplate t
INNER JOIN dbo.pdmTemplateTab tt ON tt.TemplateID = t.TemplateID
INNER JOIN dbo.pdmTab tab ON tab.TabID = tt.TabID
WHERE t.TemplateID = @TemplateId
ORDER BY tt.Sort, tt.TabID;
```

Record:

| Field | Use |
|-------|-----|
| `TemplateName` | Default Transaction Group / Search / folder name (user confirms) |
| `TabID`, `TabName`, `Sort` | Tab inventory order; Blueprint transaction order |
| `IsTemplateHeaderTab` | Template Header tab(s); `referenceScope` candidate; Search link `TemplateItemType` (see §Phase D **Warning**) |
| `IsMasterReferenceHeaderTab` | Prefer for `ReferenceCode` / root scope when multiple header flags |

**Import data scope** (product references belonging to this template):

```sql
SELECT DISTINCT ProductReferenceID
FROM dbo.pdmProductTemplate
WHERE TemplateID = @TemplateId AND ProductReferenceID IS NOT NULL;
```

Build `#TabInput(TabId)` from **PLM query result only** — not from user-typed TabIds.

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

Present TabId inventory to user (merge PLM + DW):

| TabId | TabName (PLM) | Sort | IsTemplateHeader | DwTableName | APP table | Columns | Type |
|-------|---------------|------|------------------|-------------|-----------|---------|------|

### A4. DW column naming

```
{Name}_{SubItemId}  |  {Name}__{SubItemId}  |  {Name}_{SubItemId}_FK_{target}
```

System columns (not mapped): Tab → `TabID`, `ProductReferenceID`; Grid → `ProductReferenceID`, `BlockID`, `GridID`, `RowID`, `RowValueGUID`, `Sort`.

### A5. SubItem sharing (among this template's tabs)

When **two tab wide tables overlap** (common: `IsTemplateHeaderTab` tab + a richer info tab):

- Report shared / tab-A-only / tab-B-only SubItem counts
- **Recommend:** shared SubItems on the **Template Header** APP table; secondary tab `excludeSubItemsFromDwTable` → header DW table
- **Fabric Info style:** if user confirms, secondary transaction = Root + (Header sibling + Info sibling) — see prior `excludeSubItemsFromDwTable` pattern

Detect overlap by SubItem intersection — **do not assume** names; `IsTemplateHeaderTab` hints which tab is primary.

### A6. Normalize / denormalize proposal

Present scoped to **user TabIds only**:

| APP object | Rule |
|----------|------|
| `{prefix}ReferenceBasicInfo` | `ReferenceId` = `ProductReferenceID`; scope = `pdmProductTemplate` for this `TemplateId`; `ReferenceCode` from header tab (§A1b) |
| Each tab wide table | All columns, or exclusive SubItems if overlap rule applies |
| Each grid | Business columns + `RowId` / `ReferenceId` / `Sort` |
| Grids without Tab wide table | No tab DDL; grid table only |

### A7. Confirmation checklist — **STOP**

Ask user to confirm:

1. **TemplateId** + `TemplateName` → Transaction Group / Search names  
2. TabId → APP table mapping (all tabs from PLM for this template)  
3. **IsTemplateHeaderTab** tab(s) → `referenceScope` DW table + column  
4. Overlap / exclusive SubItem split (if any)  
5. Grid ↔ TabId associations (grid-only tabs: no `PLM_DW_Tab_*`)  
6. Skip tabs/grids with no DW source  
7. `@TablePrefix` default `Plm_` OK?  
8. **`@ImportMode`** — default **`APPEND`** when tenant may already have rows from another template; `REPLACE` only for full reload of scoped refs  
9. Per TabId → Transaction unit structure (Root / Sibling / dual-sibling for info tabs)  
10. **Existing transactions** — optional tenant probe: `AppTransaction.IntegrationId = 'Tab_{TabId}'`; mark `importStatus: "Skipped"` in config for tabs that already exist (Phase D Insert also skips automatically)  
11. Blueprint field counts per Transaction vs FieldMapping rows  

After user confirms Phase A, record in `source/dwTabImportConfig.json` (see §B1) — include `plmTemplateId`, `plmDatabase`, `plmTemplate` metadata, and per-tab `tabSort`, `isTemplateHeaderTab`, `importStatus`.

---

## Phase B — Generate SQL (after user confirms)

### B1. Write `source/dwTabImportConfig.json`

```json
{
  "plmTemplateId": 0,
  "plmSqlServer": "...",
  "plmDatabase": "PLM",
  "sqlServer": "...",
  "dwDatabase": "plmDW",
  "importTabIds": [ "... from PLM pdmTemplateTab ..." ],
  "tablePrefixDefault": "Plm_",
  "rootTableSuffix": "ReferenceBasicInfo",
  "plmTemplate": {
    "templateId": 0,
    "templateName": "...",
    "templateHeaderTabIds": [ 0 ]
  },
  "referenceScope": {
    "dwTable": "PLM_DW_Tab_...",
    "dwColumn": "...",
    "plmTabId": 0,
    "plmSubItemId": 0
  },
  "tabs": [
    {
      "appTable": "...",
      "dwTable": "...",
      "tabId": 0,
      "plmTabName": "...",
      "tabSort": 1,
      "isTemplateHeaderTab": false,
      "importStatus": "Ready",
      "mode": "all"
    }
  ],
  "grids": [ ... ],
  "blueprint": {
    "transactionGroupName": "...",
    "transactionGroupIntegrationId": "TG_...",
    "searchName": "... References",
    "searchIntegrationId": "Search_...",
    "folderName": "..."
  }
}
```

- `importTabIds` / `tabs` — **derived from PLM** for `plmTemplateId`, not typed by user  
- `tabSort`, `isTemplateHeaderTab` — copied from PLM probe  
- `importStatus`: `Ready` | `Skipped` (existing `Tab_{id}` transaction — optional; Insert mode skips anyway)  
- `mode`: `all` | `excludeSubItemsFromDwTable`  

### B2. Run generator

```powershell
powershell -File AppReact/ImportDoc/ImportFromPLMDW/source/_gen_plmdw_import_sql.ps1
```

Requires `source/dwTabImportConfig.json` (not committed with secrets; example is `dwTabImportConfig.example.json`).

**Produces in `output/{templateId}/`** (subfolder named from `plmTemplateId` in config):

| File | Content |
|------|---------|
| `output/{templateId}/PlmDw_Tables.sql` | `{prefix}ReferenceBasicInfo` + tab/grid tables from config |
| `output/{templateId}/PlmDw_FieldMapping.sql` | `{prefix}FieldMapping` DDL + seed |
| `output/{templateId}/PlmDw_ImportFromDW.sql` | Copied from `source/PlmDw_ImportFromDW.sql` template |
| `output/{templateId}/PlmDw_ImportBlueprint.json` | Transaction / Form / Search **configuration plan** for BL TOOLS |
| `output/{templateId}/PlmDw_ImportBlueprint.sql` | Optional tenant table `{prefix}ImportBlueprint` + JSON seed |

Generator details:
- Reads `INFORMATION_SCHEMA` from plmDW  
- Reads PLM `pdmBlockSubItem` (`ControlType`, `EntityId`) and `pdmGridMetaColumn` for grid columns — `plmEntityId` matches tenant `AppEntityInfo.IntegrationId`  
- Visibility (`isVisible`) is resolved differently for tab fields vs grid columns:
  - **Tab fields (block sub-items)** — visible only when **both** layers pass:
    1. Layer 1 `pdmTabBlockSubItemExtraInfo.Visible = 1` (keyed `TabID + SubItemID`; `AliasName` → `displayLabel`), AND
    2. Layer 2 placed on the **Tab Design** layout (`pdmTabLayout` → `pdmTabLayoutItem` → `pdmTabLayoutSubitem`, keyed `TabID + SubItemID`).
  - **Grid columns** — visibility is **not** in `pdmTabBlockSubItemExtraInfo`. It is controlled at tab level by `pdmTabGridMetaColumn.Visible = 1` (keyed `TabID + GridColumnID`; `pdmTabGridMetaColumn.AliasName` → `displayLabel`). `pdmGridMetaColumn.Hidden` is only the grid-wide default and is overridden by the tab-level row.
  - Anything not matching the rule above → `isVisible: false`.
- APP column names: strip `_SubItemId` / `_FK_*`; suffix `_SubItemId` on collisions  
- Mapping DELETE scoped to **tables in config only** (no `LIKE Fabric_%`)  
- INSERT values use doubled quotes inside `SET @sql = N'...'` → `N''@P@...''`  

### B3. `{prefix}FieldMapping` schema

`AppTableName`, `AppColumnName`, `DwTableName`, `DwColumnName`, `PlmTabId`, `PlmSubItemId`, `PlmGridSubItemId`, `PlmGridId`, `PlmMetaColumnId`, `PlmBlockId`, `DwFkTarget`, `FieldKind` (`TabField` | `GridColumn` | `ReferenceField`), `PlmControlType`, `PlmEntityId`, `DwDataType`.

### B3b. `PlmDw_ImportBlueprint.json`

Describes Transaction Group, per-Tab Transaction unit structure (`RootPlusMasterSibling`), `fieldPolicy` (`AllMappedColumns` | `ExclusiveSubItemsOnly`), grid bindings, field UI metadata (`blueprintFields`: `plmControlType`, `plmEntityId` / `entityIntegrationId`, `displayLabel`, `isVisible` from PLM), and Search/View/navigation targets. Generated from `dwTabImportConfig.json` + DW column probe + PLM sub-item/grid/extra-info metadata. BL TOOLS: `PlmMigration/ValidateDwImportBlueprint`, `PreviewDwBlueprintConfig`, `ExecuteDwBlueprintConfig`. On Execute, BL maps PLM control type → `AppTransactionField.ControlType` and resolves `plmEntityId` → tenant `AppEntityInfo.EntityInfoID` via `IntegrationId`.

### B4. `output/{templateId}/PlmDw_ImportFromDW.sql`

Template: `source/PlmDw_ImportFromDW.sql`. Generator patches `@DwDatabase`, `@PlmDatabase`, `@PlmTemplateId` from config.

**Reference scope (required):** when `@PlmTemplateId` is set, `#RefFilter` = distinct `ProductReferenceID` from `pdmProductTemplate` for that template, **intersected** with rows present on the `referenceScope` DW tab table.

```sql
SELECT ProductReferenceID FROM dbo.pdmProductTemplate WHERE TemplateID = @PlmTemplateId;
```

**Incremental data import (second+ template):** default `@ImportMode = 'APPEND'`. For each target table, INSERT only where `ReferenceId` **not already** in that APP table — shared physical tables keep prior template rows; new template adds only new references. Use `REPLACE` only to delete+reload scoped refs on all mapped tables.

Parameters: `@TablePrefix`, `@RootTableSuffix`, `@DwDatabase`, `@PlmDatabase`, `@PlmTemplateId`, `@ImportMode`, `@ReferenceIdList`, `@DryRun`.

---

## Execution order (APP tenant DB)

Run scripts from **`output/{templateId}/`** (e.g. `output/3351/`):

```text
1. output/{templateId}/PlmDw_Tables.sql
2. output/{templateId}/PlmDw_FieldMapping.sql
3. output/{templateId}/PlmDw_ImportFromDW.sql
4. (optional) output/{templateId}/PlmDw_ImportBlueprint.sql — store Blueprint JSON in tenant DB
```

## Phase D — APP configuration (BL TOOLS)

After physical tables are populated, open **PLM Data Import → Step 3 DW Blueprint** in the app, or call the API directly:

1. Upload `output/{templateId}/PlmDw_ImportBlueprint.json` (or **Load from tenant DB** if `PlmDw_ImportBlueprint.sql` was run)
2. **Validate & Preview** — runs `ValidateDwImportBlueprint` + `PreviewDwBlueprintConfig`
3. **Execute Insert** or **Execute Update** — `ExecuteDwBlueprintConfig`

API equivalents: `POST webapi/PlmMigration/ValidateDwImportBlueprint`, `PreviewDwBlueprintConfig`, `ExecuteDwBlueprintConfig`.

**Agent scope:** Phase D is executed by the **user in the running app**. The agent generates files and instructions only — no server deployment during PROMPT runtime.

**BL (Phase D):** `SaveDwBlueprintLinkTargets` reads `plmTemplate.templateHeaderTabIds` and per-transaction `isTemplateHeaderTab` / `plmTabSort` from Blueprint JSON — same `TemplateItemType` behavior as legacy Template Import (`TemplateHeader` vs `MainItem`). **New** action targets the first non-header tab.

**Warning (keep in Phase A checklist):** Search link `TemplateItemType` is **only** correct when Blueprint JSON includes header metadata from the PLM probe (`templateHeaderTabIds`, per-tab `isTemplateHeaderTab`, `plmTabSort`). If Phase B omits these fields, BL falls back to **all MainItem** and **New** may target the wrong tab. Agent must verify generated `PlmDw_ImportBlueprint.json` before user runs Execute. Re-run Execute **Update** (or rebuild Search View) after fixing Blueprint. Any further BL gap (e.g. `RepairTemplateLinkTargetItemTypes` against live PLM) → **STOP and warn user** — do not patch CS during PROMPT runtime unless user explicitly authorizes a flow rewrite (as in this session).

---

## Incremental import — second template when some TabIds / tables already exist

Triggered when user imports **another `TemplateId`** after a prior DW import (same tenant, usually same `@TablePrefix`).

### What the user provides (same Gate 0)

PLM connection + plmDW connection + **new** TemplateId only. Tab list comes from PLM again.

### Phase A — detect overlap with tenant (optional sqlcmd on tenant DB)

| Check | Action |
|-------|--------|
| `Tab_{TabId}` exists | Blueprint `importStatus: "Skipped"` optional; Phase D **Insert** skips existing integrationIds |
| APP table exists | `PlmDw_Tables.sql` — `ALTER ADD` new columns only |
| `{prefix}FieldMapping` rows | Scoped DELETE per config tables only |
| Same `ReferenceId` in shared table | `PlmDw_ImportFromDW.sql` with `@ImportMode='APPEND'` — **no duplicate rows per table** |

### Phase D — transactions

| Goal | Mode |
|------|------|
| New tabs only | **Insert** — existing `Tab_{id}` skipped automatically |
| Refresh existing tab layout | **Update** |
| Same TabId across templates | **One** `Tab_{id}` transaction shared — do not create duplicate |

**Transaction Group:** include **all** TabIds that should stay in the menu (this template + prior), or use separate `transactionGroupIntegrationId` per template.

---

## Folder layout

```text
ImportFromPLMDW/
  PROMPT.md
  output/                           ← deliverables root
    {templateId}/                   ← one subfolder per plmTemplateId (e.g. 3351/)
      PlmDw_Tables.sql
      PlmDw_FieldMapping.sql
      PlmDw_ImportFromDW.sql
      PlmDw_ImportBlueprint.json
      PlmDw_ImportBlueprint.sql
  source/
    dwTabImportConfig.example.json
    dwTabImportConfig.json          ← Phase B working config
    _gen_plmdw_import_sql.ps1       ← writes to ../output/{templateId}/
    _plm_probe_template.sql         ← PLM: template + tabs + ref count
    PlmDw_ImportFromDW.sql          ← import template
    _dw_probe_by_tabids.sql         ← plmDW: tab/grid probe (fill #TabInput from PLM)
```

**If `source/` is deleted:** PROMPT does **not** auto-recreate it. Agent must restore from repo or rewrite tools from §Phase B.

---

## Agent checklist

```text
[ ] Gate 0: PLM + plmDW connections + one TemplateId? If not → ask and STOP
[ ] No server code: no .cs edits, no dotnet build — if needed → STOP and warn user
[ ] Run _plm_probe_template.sql → TemplateName, tabs, Sort, IsTemplateHeaderTab
[ ] Build #TabInput from PLM tabs; run _dw_probe_by_tabids.sql on plmDW
[ ] SubItem overlap analysis among template tabs
[ ] Propose referenceScope on IsTemplateHeaderTab (or IsMasterReferenceHeaderTab) tab
[ ] Phase A checklist → WAIT FOR USER
[ ] Write dwTabImportConfig.json (plmTemplateId + PLM tab metadata)
[ ] Run _gen_plmdw_import_sql.ps1 → output/{templateId}/PlmDw_*.sql + Blueprint
[ ] Verify PlmDw_ImportFromDW.sql has @PlmTemplateId + APPEND default
[ ] Optional: pilot import with @ReferenceIdList
```

---

## Example session message

**Illustration only — not defaults.** The agent must not use these values unless the user pastes them (or equivalent) in their message.

**Insufficient** (agent must ask for the three required items):

```text
@AppReact/ImportDoc/ImportFromPLMDW/PROMPT.md
```

**Sufficient** (agent may start Gate 0 → Phase A):

```text
按 AppReact/ImportDoc/ImportFromPLMDW/PROMPT.md 执行。

PLM connection string:
  Data Source=PC3B\MSSQLSERVER01;Initial Catalog=PLM;User ID=sa;Password=...

plmDW connection string:
  Data Source=PC3B\MSSQLSERVER01;Initial Catalog=plmDW;User ID=sa;Password=...

TemplateId to import:
  42
```

Agent loads TabIds from `pdmTemplateTab` for TemplateId 42 — user does **not** list TabIds.

---

## Out of scope

- Template Import Wizard / auto transaction builder  
- `PlmBlockId` backfill  
- **Any C# / WebAPI / `dotnet build` changes** (see **No server code** hard rule)  
- Full production load without explicit user request  
