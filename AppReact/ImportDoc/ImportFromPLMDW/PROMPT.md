# PLM Data Warehouse → APP Template Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportFromPLMDW/`  
> **Outputs (after Phase B):** `output/{templateId}/1_PlmDw_Tables.sql` … `4_PlmDw_ImportBlueprint.json` (e.g. `output/3351/` for TemplateId 3351). Steps 5–6 are emitted only when BOM ProductDesignColor colorway grids are detected.  
> **Phase D (BL TOOLS):** `PlmMigration/ExecuteDwBlueprintConfig` — consumes physical tables + FieldMapping + Blueprint to create Transaction / Form / Search / navigation.
> **Applies to:** any PLM **Template** (not a single product type). APP table names come from DW metadata, not from fixed names in this prompt.
> **Runtimes:** This file is used both in **Cursor IDE (local)** and in **App Cloud Agent** (Cursor VM + `appai` MCP). Phase **B0** branches by runtime — do not apply App MCP/artifact rules when running in the IDE.

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
| **No server code** | **Default:** deliverables are **SQL + JSON + PowerShell in this folder only** — no C# / WebAPI edits, no `dotnet build`. **Exception (BOM colorway pivot):** `PlmMigrationBL` pivot/hierarchy support in `APP.BL` is required for Phase D; already in repo. Any *other* BL gap → **STOP**, explain, warn user. |
| **Two phases** | **Phase A:** DW analysis + APP table proposal + **Blueprint draft** → **STOP for user confirmation**. **Phase B:** generate SQL + Blueprint JSON **after** confirm. **Phase D:** BL TOOLS apply Blueprint to APP config (separate step; user runs in app). |
| **plmDW is truth** | Column names, SubItem IDs, TabIds from DW — not legacy PLM exports. |
| **1 Tab → 1 sibling table + N grid tables** | Tab wide table (`PLM_DW_Tab_*_{TabId}`) = the tab's regular sub-items → **sibling** (PK `ReferenceId`). Each materialized grid sub-item (`PLM_DW_Grid_*`) = a **grid table** (PK `RowId` identity). A tab with both yields 1 sibling + 1 grid table per grid; the tab table is never a child. Grid-only tabs (no DW Tab table): true PLM `parentPlmTabId` or orphan `Grid_{id}` as **Root+Child** — never Master Sibling. **Exception FX1 (Fit family):** see §TechPack Fit — do not emit `Plm_Fit_1`…`Plm_Fit_N`; fold into `Plm_FitSummary` + `Plm_FitRoundInfo` + `TchpFitRound` / `TchpFitMeasurement`. **Exception QX1 (Simple QC):** see §TechPack Simple QC — do not emit flat `Plm_SpecQCGrid` size slots; emit `Plm_SimpleQC` + `Plm_SimpleQCResult` (+ size VIEW). |
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
| Each tab wide table (**sibling** — default) | 1:1 with root. PK = `[ReferenceId]` (NOT identity — value comes from import). Holds the tab's **regular sub-items**. All columns, or exclusive SubItems if overlap rule applies |
| Each grid (**child**) | 1:many under root. PK = `[RowId] INT IDENTITY` + `[ReferenceId]` FK + `[Sort]`. One grid table per materialized `PLM_DW_Grid_*` |
| Grids without Tab wide table | No tab DDL; grid table only |
| Tab wide table (**child** — override only) | Optional `unitType: "child"`: PK = `[{appTable}Id] INT IDENTITY`; `[ReferenceId]` plain FK. Not the default |

#### Tab wide table → sibling; grid sub-items → separate grid tables (PK rule)

A PLM tab can contain **regular sub-items** and/or **grid sub-items** (`ControlType = 6`). They map to **different** APP tables:

- **Regular sub-items** → the tab's **wide DW table** (`PLM_DW_Tab_*_{TabId}`) → **always one `sibling` unit**:
  - PK = `[ReferenceId]` (1:1 with root; value comes from import, **not** an identity);
  - placed in `unitStructure.siblingUnits`.
- **Each grid sub-item** → its **own grid DW table** (`PLM_DW_Grid_{Segment}_{GridMetaId}`) → a separate **grid table**:
  - PK = `[RowId] INT IDENTITY(1,1)` (DB-filled, **not** imported) + `[ReferenceId] INT NOT NULL` FK to root + `[Sort]` (1:many under root);
  - placed in `unitStructure.childUnits` / `gridBindings`.
  - **Only grids that exist as `PLM_DW_Grid_*` tables are imported.** A tab may host many grid sub-items but only those materialized in plmDW become tables.
- **Therefore:** a tab that hosts **both** regular and grid sub-items produces **1 sibling table** (regular sub-items) **plus 1 grid table per materialized grid** (each with `RowId` identity PK). **The tab wide table itself is NEVER a child** — hosting a Grid sub-item does **not** turn the tab table into a child unit.
- **Override (optional):** set `unitType: "child"` or `unitType: "sibling"` on a tab in the config to force the kind. Omit `unitType` → tab wide table defaults to **`sibling`**.

#### Grid-only PLM tabs (no `PLM_DW_Tab_*`) — required rules

Some PLM tabs host **only** a grid sub-item: ExtraInfo/layout places the grid on Tab X, but plmDW has **no** `PLM_DW_Tab_*_{TabId}` — only `PLM_DW_Grid_*_{GridId}`.

**Resolve the true parent TabId from PLM** (not from DW table list, not by guessing the template header):

```sql
-- Authoritative grid → tab placement
SELECT e.TabID, t.TabName, bs.GridID, bs.SubItemID, bs.SubItemName
FROM dbo.pdmTabBlockSubItemExtraInfo e
JOIN dbo.pdmBlockSubItem bs ON bs.SubItemID = e.SubItemID
JOIN dbo.pdmTab t ON t.TabID = e.TabID
WHERE bs.ControlType = 6 AND bs.GridID = @GridId AND e.Visible = 1;
-- Prefer the TabID that also appears on this Template's pdmTemplateTab.
```

| Rule | Detail |
|------|--------|
| **`parentPlmTabId` = true PLM TabId** | Always the Tab that hosts the grid in PLM (e.g. 3171→4215, 3181→4217, 3179→4268). **Never** substitute the template header / Fabric Header / a random sibling tab. |
| **Prefer attach to imported Tab** | If that parent TabId is in `importTabIds` / `tabs` (has a DW tab wide table), set `parentPlmTabId` + `transactionIntegrationId: "Tab_{parentTabId}"` so the grid becomes a **child** of that Tab Transaction. |
| **Grid-only parent not in this template's DW tabs** | Parent Tab has no `PLM_DW_Tab_*` (or Tab is out of import scope). Options (pick one, tell user in Phase A): **(A)** set `parentPlmTabId: null`, `attachToRoot: true`, `transactionIntegrationId: "Grid_{gridId}"` → BL creates standalone **`Grid_{id}`** Transaction = **Root + Child** (grid table under root; **never** Master Sibling); **(B)** skip the grid and list it for a later import that owns the parent Tab. |
| **Do not invent wrong parents** | Wrong: hang Fabric Approvals Tracker (PLM Tab 4215) under Fabric Header 4258 just because 4258 is the header. Right: `parentPlmTabId: 4215` or orphan `Grid_3171` with Root+Child. |
| **Shared grids (e.g. Grid_7 ProductDesignColorGrid)** | When this template's tab hosts it, set `parentPlmTabId` to **that** tab. Do **not** create a second standalone `Grid_7` with `parentPlmTabId: null` if another template already attached Grid_7 as a child — Insert skips existing IntegrationIds; orphan `Grid_7` is only for templates that have no hosting tab in scope (rare). |

**BL TOOLS behavior (Phase D):** `AttachOrphanGridTransactions` builds orphan grids as **Root + Child** (`AppTransaction.IntegrationId = Grid_{plmGridId}`). Re-run **Update** / **Repair** on the Blueprint to fix existing transactions that were wrongly created as Root + Master Sibling.

**Re-import note:** tab wide tables keep `[ReferenceId]` as PK — re-running `1_PlmDw_Tables.sql` does **not** rewrite the PK. Drop the existing table (or `ALTER` PK manually) before re-running only if you set an explicit `unitType: "child"` override.

### A7. Confirmation checklist — **STOP**

Ask user to confirm:

1. **TemplateId** + `TemplateName` → Transaction Group / Search names  
2. TabId → APP table mapping (all tabs from PLM for this template)  
3. **IsTemplateHeaderTab** tab(s) → `referenceScope` DW table + column  
4. Overlap / exclusive SubItem split (if any)  
5. Grid ↔ TabId associations — **true PLM parent from ExtraInfo** (grid-only tabs: no `PLM_DW_Tab_*`); never invent parent = template header; orphan = Root+Child `Grid_{id}` only when parent Tab is out of scope (see §A6 *Grid-only PLM tabs*)  
6. Skip tabs/grids with no DW source  
7. `@TablePrefix` default `Plm_` OK?  
8. **`@ImportMode`** — default **`APPEND`** when tenant may already have rows from another template; `REPLACE` only for full reload of scoped refs  
9. Per TabId → Transaction unit structure: tab wide table = **sibling** (regular sub-items); each materialized grid = a **grid/child table** (PK `RowId` identity). Tab table is child only with explicit `unitType: "child"` override. Orphan `Grid_*` txs = **Root + Child**, never Root + Master Sibling.  
10. **Existing transactions** — optional tenant probe: `AppTransaction.IntegrationId = 'Tab_{TabId}'` or `'Grid_{GridId}'`; mark `importStatus: "Skipped"` in config for tabs that already exist (Phase D Insert also skips automatically). Wrong-unit orphan grids → re-Execute **Update/Repair** after TOOLS fix.  
11. Blueprint field counts per Transaction vs FieldMapping rows  
12. **BOM colorway grids** (if any): auto-detected `ProductDesignColor` DCU columns → grandchild `{HostAppTable}GrandColorway`; **no** `Colorway_N`/`ImageN` on host APP table (DW slot mapping only)  

After user confirms Phase A, record in `source/dwTabImportConfig.json` (see §B1) — include `plmTemplateId`, `plmDatabase`, `plmTemplate` metadata, and per-tab `tabSort`, `isTemplateHeaderTab`, `importStatus`.

### A8. BOM ProductDesignColor colorway grids (auto-detect)

Some BOM grids expose **wide** colorway slots in **DW only** (`Colorway_1` … `Colorway_N`, paired `Image1` … `ImageN`). These are **not** materialized as columns on the host APP table (`Plm_{HostAppTable}`). Step 5 UNPIVOTs directly from the DW grid table into grandchild rows.

| Signal | Source |
|--------|--------|
| DCU colorway key columns | `pdmGridMetaColumn.IsDCUForProductGridRef = 1` AND `DCUColumnBlockID` → `pdmBlock.InternalCode = 'ProductDesignColor'` |
| Host grid / tab / block | `pdmBlockSubItem.ControlType = 6` AND `GridID` → `PdmTabBlock` |
| Pivot source grid | `ProductDesignColorGrid` (pivot key column `Color`) |
| Image columns | Paired by slot index (`Colorway_N` + `ImageN`); no `DCUColumnBlockID` on Image cols |

**Transaction layout:** grandchild pivot table `{HostAppTable}GrandColorway` under the **same Tab Transaction** as the host BOM grid (host child → grandchild pivot). Physical columns: `RowId`, `ParentRowId` (FK → host `RowId`), `Colorway`, pivot value columns — **no `ReferenceId`**. Host APP table has **only** normal BOM columns (no `Colorway_N` / `ImageN`). Grandchild `AppTransactionField` control types come from PLM `pdmGridMetaColumn` (DDL + `EntityId`, Image, etc.) via Blueprint Execute.

**FieldKind values:** `BomColorwayDwSlot` (DW wide-slot mapping only — **not** APP columns) | `GrandchildPivot` (normalized pivot storage). `FieldKind` column is `NVARCHAR(32)`.

**Grandchild pivot value column names (Phase A — user confirms before generate):**

PLM BOM grids use **wide slot columns** (`Colorway1`…`Colorway20`, `Image1`…`Image20` in PLM; `Colorway_1` / `Image1` in DW). After UNPIVOT, each slot maps into **one normalized column per business role** — slot numbers are **not** kept in grandchild column names.

| Business role | PLM wide columns | Meaning | Default grandchild name |
|---------------|------------------|---------|-------------------------|
| `SlotColorValue` | `ColorwayN` (DCU key column) | Artwork color selected for that colorway cell (FK `pdmRGBColor`) | `ArtworkColor` |
| `SlotChildImage` | `ImageN` (`MasterDcucolumnId` → ColorwayN) | Artwork sketch/image for that colorway | `ArtworkPhoto` |

The pivot-key column **`Colorway`** (FK `pdmRGBColor`, from `pdmStyleColorWayMapping.StyleColorID`) is separate — do not reuse the name `Colorway` for pivot value columns.

**Phase A checklist — ask user:**

1. Report detected BOM colorway grid(s) and the role mapping above (generator prints this when run).
2. Confirm grandchild pivot value names, or provide overrides in `dwTabImportConfig.json`:

```json
"bomColorwayPivotColumnNames": {
  "3167": ["ArtworkColor", "ArtworkPhoto"]
}
```

Array order matches pivot value roles per slot (slot-1 template: color value, then image, then any additional child columns).

---

## Phase B — Generate SQL (after user confirms)

### B0. Where to put deliverables (detect runtime first)

This PROMPT is used in **two** places. **Detect which one you are in, then follow only that branch.**

| Signal | Runtime |
|--------|---------|
| MCP server **`appai`** is available (`list_workspace_files` / `sync_cloud_artifacts` work) | **App Cloud Agent** (Cursor cloud VM + AppAI) |
| No `appai` MCP; you edit files in the user’s repo / workspace on disk | **Cursor IDE (local)** |

#### B0-IDE — Cursor IDE (local)

- Write files **directly** under this folder tree (relative to `ImportFromPLMDW/`):
  - `output/{templateId}/1_PlmDw_Tables.sql` … `4_…` (and `5_`/`6_` when needed)
  - `source/dwTabImportConfig.json`
  - `scripts/…` when applicable
- **Do not** call `write_workspace_file`, `append_workspace_file`, `sync_cloud_artifacts`, or depend on App Workspace.
- **Do not** invent App Workspace / `FileRepository` URLs — the user opens files in the IDE or Explorer.
- Success = files exist on disk at those paths with expected sizes.

#### B0-APP — App Cloud Agent (VM + `appai` MCP)

**Same delivery path as generated images.** Do **not** use `write_workspace_file` / `append_workspace_file` (disabled).

**Phase split (mandatory on App Cloud):**

| Phase | Do | Do not |
|-------|-----|--------|
| **A** | Probe PLM + plmDW via MCP; draft `source/dwTabImportConfig.json`; **STOP** for user confirm | Generate `output/{templateId}/` in the same run |
| **B** | After user confirms: patch official templates → six files under `output/{templateId}/` | Start Phase B before confirm |

1. Official generators/templates are **seeded** into workspace `source/` at session start. Confirm with `list_workspace_files`.
2. **Producer:** patch `source/PlmDw_*.sql` and official templates from probe data — **never** `gen_plmdw_*.py`, `build_sql_cache.py`, or `sql_cache.json`.
3. **MCP probes:** `run_select` returns **summary only** (counts + sample rows). Full rows stay on App server. **Do not** write `source/mcp_results/*.json` or sync probe JSON via artifacts.
   - Batch queries (one sub-item meta query, one grid meta query) — not one `INFORMATION_SCHEMA` file per DW table.
4. Place **deliverables only** under Cursor artifacts, e.g. `/opt/cursor/artifacts/output/3351/1_PlmDw_Tables.sql`.
5. Call **`sync_cloud_artifacts`**, then **`list_workspace_files`**, and report `RelativePath` + `SizeBytes`.
6. User downloads via App **Workspace → Download**.

**Do not sync to Workspace:** `mcp_results/`, `sql_cache.json`, `build_sql_cache.py`, `artifacts/bin/sqlcmd`, or other probe caches.

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
- `unitType`: **optional override only.** Tab wide tables default to **`sibling`** (regular sub-items, 1:1); grids are always separate grid tables (`RowId` identity PK). Set `child` / `sibling` to force a tab table's kind. See §A6 *Tab wide table → sibling; grid sub-items → separate grid tables*.  

### B2. Run generator

**Cursor IDE (local):**

```powershell
powershell -File AppReact/ImportDoc/ImportFromPLMDW/source/_gen_plmdw_import_sql.ps1
```

Requires `source/dwTabImportConfig.json`. The script uses `sqlcmd` against `sqlServer` / `dwDatabase` / `plmDatabase` from that config.

**App Cloud Agent:**

1. Use seeded workspace files under `source/` (same script + `PlmDw_*.sql` templates).
2. **Do not expect `sqlcmd` on the VM to reach customer PLM/DW.** Prefer MCP `run_select` / `get_table_schema` on App DataSources for every probe the PS generator would run.
3. Produce the same six deliverables (structure/content parity with IDE). Forbidden as final producer: ad-hoc `gen_plmdw_*.py` short scripts.
4. Publish via Cursor artifacts + `sync_cloud_artifacts` (see B0-APP). AppAI validates size + `blueprintFields` and marks the run incomplete if checks fail.

**Produces in `output/{templateId}/`** (subfolder named from `plmTemplateId` in config):

| File | Content |
|------|---------|
| `1_PlmDw_Tables.sql` | `{prefix}ReferenceBasicInfo` + tab/grid tables + grandchild colorway tables (when detected) |
| `2_PlmDw_FieldMapping.sql` | `{prefix}FieldMapping` DDL + seed |
| `3_PlmDw_ImportFromDW.sql` | DW → APP flat import (host/grid/tab tables; **excludes** `BomColorwayDwSlot`) |
| `4_PlmDw_ImportBlueprint.json` | Transaction / Form / Search plan + `bomColorwayPivotBindings` for Phase D |
| `5_PlmDw_ImportBomColorwayGrandchild.sql` | **When BOM colorway grids detected:** UNPIVOT DW slots → grandchild rows |
| `6_PlmDw_CleanupBomColorwayStaging.sql` | **Optional legacy:** drop host `Colorway_N`/`ImageN` if an older import created them |

Generator details:
- Reads `INFORMATION_SCHEMA` from plmDW  
- Reads PLM `pdmBlockSubItem` (`ControlType`, `EntityId`) and `pdmGridMetaColumn` for grid columns — `plmEntityId` matches tenant `AppEntityInfo.IntegrationId`  
- Visibility (`isVisible`) is resolved differently for tab fields vs grid columns:
  - **Tab fields (block sub-items)** — visible only when **both** layers pass:
    1. Layer 1 `pdmTabBlockSubItemExtraInfo.Visible = 1` (keyed `TabID + SubItemID`), AND
    2. Layer 2 placed on the **Tab Design** layout (`pdmTabLayout` → `pdmTabLayoutItem` → `pdmTabLayoutSubitem`, keyed `TabID + SubItemID`).
  - **`displayLabel` (Transaction field Display Name)** — for **all** SubItems and Grid columns:
    1. Prefer tab-level Alias (`pdmTabBlockSubItemExtraInfo.AliasName` for tab fields; `pdmTabGridMetaColumn.AliasName` for grid columns) when non-empty,
    2. Else use PLM name (`pdmBlockSubItem.SubItemName` / `pdmGridMetaColumn.ColumnName`),
    3. Never fall back to the generated App column name (e.g. `How_to_Measure_3848`) when a PLM name exists.
  - **Grid columns** — visibility is **not** in `pdmTabBlockSubItemExtraInfo`. It is controlled at tab level by `pdmTabGridMetaColumn.Visible = 1` (keyed `TabID + GridColumnID`). `pdmGridMetaColumn.Hidden` is only the grid-wide default and is overridden by the tab-level row.
  - **Simple QC size-measure stems** (`GradingSize` / `QCSize` / `Difference` / wash / iron on `Plm_SimpleQCResult`) — fold size slots `Stem1…N` into one APP field. Stem **visible** if any slot has `pdmTabGridMetaColumn.Visible=1` for the QC Tab; DisplayName = first non-empty tab `AliasName` (trimmed). Hidden stems are omitted from `pivotValueFields` / `IsPivotValue`. Do **not** hardcode Size/Meas/Delta — each QC Tab / PLM database can differ.
  - **Grid-only / orphan grids** — the generator **must** load `pdmTabGridMetaColumn` for the **true PLM hosting TabId(s)** of each grid (from ExtraInfo / `parentPlmTabId`), even when that Tab has no `PLM_DW_Tab_*` and is **not** in `importTabIds`. If `parentPlmTabId` is null or wrong (e.g. template header), lookup fails and Blueprint marks every column `isVisible: false` → Phase D hides all child-grid fields. Fallback: any hosting tab with `Visible=1` for that `GridColumnID`. BL also falls back to “show all mapped columns” when the visible set is empty for a grid unit.
  - Anything not matching the rule above → `isVisible: false`.
- APP column names: strip `_SubItemId` / `_FK_*`; suffix `_SubItemId` on collisions  
- Mapping DELETE scoped to **tables in config only** (no `LIKE Fabric_%`)  
- INSERT values use doubled quotes inside `SET @sql = N'...'` → `N''@P@...''`  
- **BOM colorway:** `_gen_plmdw_bom_colorway.ps1` (dot-sourced) probes PLM, appends grandchild DDL/field rows, emits steps 5–6, and adds `bomColorwayPivotBindings` to step-4 Blueprint JSON  

### B3. `{prefix}FieldMapping` schema

`AppTableName`, `AppColumnName`, `DwTableName`, `DwColumnName`, `PlmTabId`, `PlmSubItemId`, `PlmGridSubItemId`, `PlmGridId`, `PlmMetaColumnId`, `PlmBlockId`, `DwFkTarget`, `FieldKind` (`TabField` | `GridColumn` | `ReferenceField` | `BomColorwayDwSlot` | `GrandchildPivot`) — **`FieldKind` is `NVARCHAR(32)`** (auto-widened on existing tables), `PlmControlType`, `PlmEntityId`, `DwDataType`.

### B3b. `4_PlmDw_ImportBlueprint.json`

Describes Transaction Group, per-Tab Transaction unit structure (`RootPlusMasterSibling` for tab wide tables — the default; `RootPlusChild` only for `unitType: "child"` override tabs — child tab table goes in `unitStructure.childUnits`; grids always land in `gridBindings` / `childUnits`), `fieldPolicy` (`AllMappedColumns` | `ExclusiveSubItemsOnly`), grid bindings, field UI metadata (`blueprintFields`: `plmControlType`, `plmEntityId` / `entityIntegrationId`, `displayLabel`, `isVisible` from PLM), Search/View/navigation targets, and **`bomColorwayPivotBindings`** (host/grandchild/source table names, pivot column keys, staging column patterns). Generated from `dwTabImportConfig.json` + DW column probe + PLM sub-item/grid/extra-info metadata + BOM colorway probe. BL TOOLS: `PlmMigration/ValidateDwImportBlueprint`, `PreviewDwBlueprintConfig`, `ExecuteDwBlueprintConfig`. On Execute, BL maps PLM control type → `AppTransactionField.ControlType`, resolves `plmEntityId` → tenant `AppEntityInfo.EntityInfoID` via `IntegrationId`, and applies pivot bindings (`ApplyBomColorwayPivotBindingsSql` — hides/deletes host staging fields, configures grandchild `EmGridViewDisplayType=7`).

**Orphan / grid-only grids** (`parentPlmTabId` null or parent Tab not in this Blueprint's `transactions`): BL `AttachOrphanGridTransactions` creates `AppTransaction.IntegrationId = Grid_{plmGridId}` with unit structure **Root (`ReferenceBasicInfo`) + Child (grid table, `RowId` PK)** — never Master Sibling. `transactionIntegrationId` for orphans must be `Grid_{id}` (generator default when parent is null). Do **not** set `transactionIntegrationId` to a `Tab_*` unless that Tab is actually in the Blueprint plan.

### B4. `3_PlmDw_ImportFromDW.sql`

Template: `source/PlmDw_ImportFromDW.sql`. Generator patches `@DwDatabase`, `@PlmDatabase`, `@PlmTemplateId` from config, and injects `#Targets` filter = **this config's tab/grid AppTables only** (same set as step 2 scoped DELETE, excluding root). That prevents residual `{prefix}FieldMapping` rows from a prior template from being imported under the wrong `@PlmTemplateId`.

**Reference scope (required):** when `@PlmTemplateId` is set, `#RefFilter` = distinct `ProductReferenceID` from `pdmProductTemplate` for that template, **intersected** with rows present on the `referenceScope` DW tab table.

```sql
SELECT ProductReferenceID FROM dbo.pdmProductTemplate WHERE TemplateID = @PlmTemplateId;
```

**Incremental data import (second+ template):** default `@ImportMode = 'APPEND'`. For each target table, INSERT only where `ReferenceId` **not already** in that APP table — shared physical tables keep prior template rows; new template adds only new references. Use `REPLACE` only to delete+reload scoped refs on all mapped tables.

Parameters: `@TablePrefix`, `@RootTableSuffix`, `@DwDatabase`, `@PlmDatabase`, `@PlmTemplateId`, `@ImportMode`, `@ReferenceIdList`, `@DryRun`.

### B5. `5_PlmDw_ImportBomColorwayGrandchild.sql` (when detected)

Template: `source/PlmDw_ImportBomColorwayGrandchild.sql`. UNPIVOTs **DW** `Colorway_N` / `ImageN` via `pdmStyleColorWayMapping` into `{HostAppTable}GrandColorway` rows. **Prerequisite:** steps 1–3 completed; step 4 Execute completed; `ProductDesignColorGrid` imported for pivot headers. **FieldMapping:** slot lookup reads `FieldKind = BomColorwayDwSlot` rows from step 2 (legacy `BomColorwaySlot` still supported).

### B6. `6_PlmDw_CleanupBomColorwayStaging.sql` (optional — legacy DBs only)

Template: `source/PlmDw_CleanupBomColorwayStaging.sql`. For databases that **already** have host `Colorway_N`/`ImageN` columns from an older pipeline. **Fresh imports do not need step 6.**

---

## Execution order (APP tenant DB)

Run scripts from **`output/{templateId}/`** (e.g. `output/3351/`):

```text
1. output/{templateId}/1_PlmDw_Tables.sql
2. output/{templateId}/2_PlmDw_FieldMapping.sql
3. output/{templateId}/3_PlmDw_ImportFromDW.sql
4. output/{templateId}/4_PlmDw_ImportBlueprint.json — Phase D Validate & Execute
5. output/{templateId}/5_PlmDw_ImportBomColorwayGrandchild.sql   (when BOM colorway grids detected)
6. output/{templateId}/6_PlmDw_CleanupBomColorwayStaging.sql   (optional — legacy host staging columns only)
```

**Order when BOM colorway is present:** steps 1–3 → **step 4 Execute** (or Execute + **Refresh Caches**) → step 5 (grandchild data). Step 6 only if upgrading an old tenant DB that still has host staging columns.

## Phase D — APP configuration (BL TOOLS)

After physical tables are populated (steps 1–3), open **PLM Data Import → Step 3 DW Blueprint** in the app, or call the API directly:

1. Upload `output/{templateId}/4_PlmDw_ImportBlueprint.json`
2. **Validate & Preview** — runs `ValidateDwImportBlueprint` + `PreviewDwBlueprintConfig`
3. **Execute Insert** or **Execute Update** — `ExecuteDwBlueprintConfig`

API equivalents: `POST webapi/PlmMigration/ValidateDwImportBlueprint`, `PreviewDwBlueprintConfig`, `ExecuteDwBlueprintConfig`.

**Agent scope:** Phase D is executed by the **user in the running app**. The agent generates files and instructions only — no server deployment during PROMPT runtime.

**BL (Phase D):** `SaveDwBlueprintLinkTargets` reads `plmTemplate.templateHeaderTabIds` and per-transaction `isTemplateHeaderTab` / `plmTabSort` from Blueprint JSON — same `TemplateItemType` behavior as legacy Template Import (`TemplateHeader` vs `MainItem`). **New** action targets the first non-header tab.

**Warning (keep in Phase A checklist):** Search link `TemplateItemType` is **only** correct when Blueprint JSON includes header metadata from the PLM probe (`templateHeaderTabIds`, per-tab `isTemplateHeaderTab`, `plmTabSort`). If Phase B omits these fields, BL falls back to **all MainItem** and **New** may target the wrong tab. Agent must verify generated `4_PlmDw_ImportBlueprint.json` before user runs Execute. Re-run Execute **Update** (or rebuild Search View) after fixing Blueprint. Any further BL gap (e.g. `RepairTemplateLinkTargetItemTypes` against live PLM) → **STOP and warn user** — do not patch CS during PROMPT runtime unless user explicitly authorizes a flow rewrite (as in this session).

---

## Incremental import — second template when some TabIds / tables already exist

Triggered when user imports **another `TemplateId`** after a prior DW import (same tenant, usually same `@TablePrefix`).

### What the user provides (same Gate 0)

PLM connection + plmDW connection + **new** TemplateId only. Tab list comes from PLM again.

### Phase A — detect overlap with tenant (optional sqlcmd on tenant DB)

| Check | Action |
|-------|--------|
| `Tab_{TabId}` exists | Blueprint `importStatus: "Skipped"` optional; Phase D **Insert** skips existing integrationIds |
| APP table exists | `1_PlmDw_Tables.sql` — `ALTER ADD` new columns only |
| `{prefix}FieldMapping` rows | Scoped DELETE per config tables only |
| Same `ReferenceId` in shared table | `3_PlmDw_ImportFromDW.sql` with `@ImportMode='APPEND'` — **no duplicate rows per table** |
| Residual FieldMapping from prior template | Step 3 `#Targets` is filtered to **this config's** AppTables only — does not import leftover tables |

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
  BOMColorwayPrompt.md              ← stub; see PROMPT.md §A8
  output/                           ← deliverables root
    {templateId}/                   ← one subfolder per plmTemplateId (e.g. 3351/)
      1_PlmDw_Tables.sql
      2_PlmDw_FieldMapping.sql
      3_PlmDw_ImportFromDW.sql
      4_PlmDw_ImportBlueprint.json
      5_PlmDw_ImportBomColorwayGrandchild.sql   (when BOM colorway detected)
      6_PlmDw_CleanupBomColorwayStaging.sql     (when BOM colorway detected)
  source/
    dwTabImportConfig.example.json
    dwTabImportConfig.json          ← Phase B working config
    _gen_plmdw_import_sql.ps1       ← writes to ../output/{templateId}/
    _gen_plmdw_bom_colorway.ps1     ← BOM colorway probe + steps 5–6
    _plm_probe_template.sql         ← PLM: template + tabs + ref count
    PlmDw_ImportFromDW.sql          ← import template (step 3)
    PlmDw_ImportBomColorwayGrandchild.sql
    PlmDw_CleanupBomColorwayStaging.sql
    _dw_probe_by_tabids.sql         ← plmDW: tab/grid probe (fill #TabInput from PLM)
```

**If `source/` is deleted:** PROMPT does **not** auto-recreate it. Agent must restore from repo or rewrite tools from §Phase B.

---

## Agent checklist

```text
[ ] Gate 0: PLM + plmDW connections + one TemplateId? If not → ask and STOP
[ ] BOM colorway: report auto-detected grids (§A8) in Phase A checklist
[ ] Run _plm_probe_template.sql → TemplateName, tabs, Sort, IsTemplateHeaderTab
[ ] Build #TabInput from PLM tabs; run _dw_probe_by_tabids.sql on plmDW
[ ] SubItem overlap analysis among template tabs
[ ] Propose referenceScope on IsTemplateHeaderTab (or IsMasterReferenceHeaderTab) tab
[ ] Phase A checklist → WAIT FOR USER
[ ] Write dwTabImportConfig.json (plmTemplateId + PLM tab metadata)
[ ] Run _gen_plmdw_import_sql.ps1 → output/{templateId}/1_…6_ files
[ ] Verify 3_PlmDw_ImportFromDW.sql has @PlmTemplateId + APPEND default
[ ] Verify 4_PlmDw_ImportBlueprint.json includes bomColorwayPivotBindings when steps 5–6 exist
[ ] TechPack: run 3b (includes View_TchpStyleActiveSizeRunSizes + View_TchpFitMeasurementByPom) before Phase D
[ ] TechPack Grading: SizeRunSizes view child + GradeValue pivot (P1) + golden fields (G1) + BaseSize cascade (S2)
[ ] TechPack Fit: FX1 tables (slim FitSummary + FitRoundInfo) + F2 SUMMARY/ROUND TX + F3 read-only POM×Round pivot
[ ] F2 Fit Round measurement UX: View_TchpPomSpecLine + PomSpecLine entity + Init/Tol/Diff temp + Calc button
[ ] Optional: pilot import with @ReferenceIdList
```

---

## TechPack (Grading / Fit / QC) — locked decisions (Template 3283+)

When `dwTabImportConfig` includes a `techPack` block (α bindings):

| Decision | Choice |
|----------|--------|
| Flat SpecFit / SpecGrading grids | **A** — replace with `Tchp*` long-term; do **not** emit `Plm_SpecFitGrid` / `Plm_SpecGradingGrid` |
| StyleSpec count | **A** — one `TchpStyleSpec` per product (shared by Grading + Fit + **Simple QC**) |
| Fit rounds | `TchpFitRound.RoundNumber` = 1,2,3,4… |
| Blueprint wiring | **α** — explicit `techPack.bindings` per `plmTabId` |
| SizeRun / BaseSize / UOM | **S1** — from Grading DW only → `TchpStyleSpec` columns; stripped from `Plm_*` |
| Import scope | **D1** — step `3b_Tchp_ImportFromDW.sql` writes Tchp now |
| StyleSpec unit kind | **Sibling**; `StyleSpecId` = non-identity PK = `Root.ReferenceId` |
| Link without DB FK | **L2** — sibling `StyleSpecId` → Root.`ReferenceId`; children **attachToRoot** with `StyleSpecId` → Root.`ReferenceId` |
| SizeRunSizes grid | **V1** — **Grading tab only**: ROOT child on `View_TchpStyleActiveSizeRunSizes`. Link `StyleSpecId` → Root.`ReferenceId`. **Not on Form layout** (pivot column source only). Do **not** add to Fit tabs. |
| GradeValue pivot | **P1** — `TchpGradeValue.EmGridViewDisplayType = ChildUnitPivotColumns (7)`. `SizeRunSizeId` = IsPivotColumn + `MatrixForeignKeyFieldId` → View.`SizeRunSizeId`. `GradingDelta` = IsPivotValue. `MatrixKeyTransactionFieldId` → View.`IsVisible` (DimensionCode filter). |
| BaseSize cascade | **S2** — `TchpStyleSpec.BaseSizeDetailId` Depend On DDL = `SizeRunId`; entities `SizeRun` / `SizeRunDetail`; **RelationalTable** cascade: `CascadingRelationTable=TchpSizeRunSize`, Schema=`dbo`, ParentKey=`SizeRunId`, ChildKey=`SizeRunSizeId` (not only `DDLParentLevelID`) |
| Grading field golden | **G1** — see §TechPack Grading golden field template (widths / sort / entities). `IsFixed` stays TextBox; `GradeRuleSetId` → DDL `TchpGradeRuleSet`; `UnitOfMeasure` stays TextBox (+ Entity ok). **`VisibleSizes` is Grading-only** (with V1 SizeRunSizes view) — do **not** add to Fit Summary / Fit Round StyleSpec. |
| SpecFit ActualValue | **`SampleN` only** (PLM **Meas N**) → `TchpFitMeasurement.ActualValue`. **`ReviseN` = Rev.Spec N** (revised target) — do **not** `COALESCE(Revise, Sample)` into ActualValue. Blank-safe `NULLIF(trim(Sample),'')`. Round discovery may still use Sample **or** Revise so a round with only Rev.Spec still creates `TchpFitRound`. |
| Fit RoundNumber source | **R1** — digit **N** in SpecFit columns `SampleN` / `ReviseN` (not Tab Sort). PLM Tab names (“Fit 1”, “Fit 2”, …) use the same N. |
| FIT import exception | **FX1** — Fit-family tabs do **not** follow “1 Tab → 1 sibling”. See §TechPack Fit (FX1 / F2 / F3). |
| Fit transactions | **F2** — one **FIT SUMMARY** master TX + one **FIT ROUND** child TX (Child Unit Link Target). No per-round TX / no `Plm_Fit_1`…`Plm_Fit_8`. |
| Fit Summary aggregate grid | **F3** — read-only: Child `TchpPomSpecLine` + Grandchild `View_TchpFitMeasurementByPom` + `ChildUnitPivotColumns` (RoundNumber). |
| POM_Template / Spec_Selected_Size | Stay on `Plm_Grading` / slim `Plm_FitSummary` (round-agnostic blocks only) |

Phase D (`ExecuteDwBlueprintConfig`) applies L2 after `CreateHierarchyTransactionFromTables`: sibling + child `IsLinkToParentPrimaryKey` to Root only (never reparent under StyleSpec sibling). Then: EnsureMissing child units (Update), `IsReadOnly` flags for V1/F3 view children, StyleSpec SizeRun/BaseSize DDL + S2 cascade, **G1 golden field template**, **P1 GradeValue pivot**, **F3 FitMeasurement pivot** (`techPackGradeValuePivotBindings` / Fit pivot bindings or auto-detect from ChildUnitDefs).

**V1 / F3 view DDL** (keep identical in both places):

1. `Document/Design/POM_Grading_QC_NewSchema.sql`
2. Emitted in `output/{templateId}/3b_Tchp_ImportFromDW.sql` (`CREATE OR ALTER VIEW` after a `GO`) when `techPack` present

Run **3b before Phase D** so views exist when Blueprint Validate/Execute resolves `View_TchpStyleActiveSizeRunSizes` and `View_TchpFitMeasurementByPom`.

### TechPack Fit — FX1 import exception + F2/F3 units (locked)

**Scope of exception:** tabs in `techPack.bindings` whose `role` is Fit-family (`FitSummary`, `Fit1`…`FitN`, `PP1`…, `TOP`, …) **and** their Comments companion tabs. All other tabs keep **1 Tab → 1 sibling**.

**Does not apply to:** Grading, BOM, Header, non-Fit template tabs.

#### FX1 — APP tables (instead of one sibling per Fit tab)

| PLM source | APP target | Notes |
|------------|------------|-------|
| Fit Summary Tab — blocks **not** tied to a round | Sibling `Plm_FitSummary` (slim) | Round-specific SubItems (Fit1 Date, Fit2 Status, …) **out** of this table |
| Fit1…N / PP… / Comments — per-round non-grid SubItems | Sibling `Plm_FitRoundInfo` (1:1 with `TchpFitRound`) | **Semantic normalize** to shared columns. Prefer **Fit N Tab + Fit N Comments Tab** as source (not Fit Summary flattened columns). Map SubItem → column by round N from Tab name / `roundSources` / `commentSources`. Calc columns (`blankdate_calc_*`, `dateisblank_calc_*`, `setdate_calc_*`, `patternstate_IB_*`, SampleStatusState CB, …) **are imported**. Shared Spec*/SizeRun/BaseSize/Measure_Unit stay on Fit Summary / StyleSpec. |
| SpecFit Grid `SampleN`/`ReviseN` | `TchpFitRound` + `TchpFitMeasurement` | Do **not** emit `Plm_SpecFitGrid`. Create round rows for each N that has data; **`RoundType` = Sample \| PP \| Top** from PLM Fit block (`FitN`→Sample, `PPn`→PP, `TOPn`→Top). Config: `fitRoundTypeByRoundNumber` / `fitDefaultRoundType` / bindings `role` |
| — | `TchpFitRound` | PK `FitRoundId` + `StyleSpecId` + **`RoundNumber`** + **`RoundType`**. Workflow columns may remain on table for APP; **PLM-imported** round header fields live **only** on `Plm_FitRoundInfo` (do not sync into TchpFitRound) |
| Comments tabs | Field source for `Plm_FitRoundInfo` only | **No** separate Comments transaction |

**`techPack.fitRoundInfo` config**

| Key | Purpose |
|-----|---------|
| `appTable` | Logical name (`FitRoundInfo` → `Plm_FitRoundInfo`) |
| `semanticColumnsFile` | External JSON (e.g. `fitRoundInfo.semanticColumns.{templateId}.json`) **or** inline `semanticColumns[]` |
| `semanticColumns[].appColumn` / `sqlType` / `entityCode` / `controlType` / `displayName` / `sortOrder` | APP column + Entity/DDL wiring |
| `roundSources[{roundNumber,dwTable,dwColumns[]}]` | Prefer round-specific DW cols; `COALESCE` when shared+specific (e.g. Approve_Date) |
| `commentSources[...]` | FitComment / FitCommentImage / CommentDate from Comments tabs |

Template **3283** maps Fit1–4 only; **PROMPT + file shape must work for FitN / PP / TOP** on other PLM DBs (add more `roundSources` entries). Step **1_** DDL adds columns; step **3b** fills by `TchpFitRound.RoundNumber`; Blueprint `ApplyTechPackFitRoundInfoGoldenFieldTemplate` wires Entity/DDL + Form layout on **FIT ROUND** sibling.

#### F2 — Transaction shape

**Master — FIT SUMMARY**

| Unit | Table | Kind |
|------|-------|------|
| Root | `Plm_ReferenceBasicInfo` (or template root) | Master |
| Sibling | `TchpStyleSpec` | Shared StyleSpec — SizeRun / BaseSize / UOM only; **no `VisibleSizes`** (Grading/V1 only) |
| Sibling | `Plm_FitSummary` | Slim Summary blocks |
| Child | `TchpFitRound` | One row per round; Link Target → FIT ROUND TX |
| Child (F3, optional on Form) | `TchpPomSpecLine` | All POMs for StyleSpec |
| Grandchild (F3) | `View_TchpFitMeasurementByPom` | Pivot measurements; **IsReadOnly** |
| Child (pivot domain, Form omit) | `TchpFitRound` or thin round list view | RoundNumber column domain for F3 (same pattern as V1 sizes) |

**Child TX — FIT ROUND** (opened via Child Unit Link Target from SUMMARY)

| Unit | Table | Kind |
|------|-------|------|
| Root | `TchpFitRound` | This round |
| Sibling | `Plm_FitRoundInfo` | Normalized PLM round header (`FitRoundId` PK/FK) |
| Child | `TchpFitMeasurement` | POM actuals for this round only |

Legacy `fitRoundNumberFilter` on **separate Fit1–4 transactions** is **retired** under F2 (one ROUND TX; filter = current `FitRoundId`).

#### F2 — Fit Round measurement UX golden template (locked)

Goal: on **TX_FitRound**, child grid `TchpFitMeasurement` shows POM label + Init/Tol from spec + Actual + computed Diff, with **Calculation** button enabled. Matches architecture §7.2 (`difference = actual − spec`); Init Value is the subscribed **BaseValue** snapshot from the POM line (not a DB column on `TchpFitMeasurement`).

**1) View + Entity (system define)**

| Object | Value |
|--------|-------|
| View | `dbo.View_TchpPomSpecLine` — `TchpPomSpecLine` ⋈ `TchpBodyPart` → `PomSpecLineId`, `BodyPartName`, `StyleSpecId`, `BaseValue`, `Tolerance`, `IsFixed`, `Sort`, `BodypartAliasName`, … |
| EntityCode | `PomSpecLine` |
| EntityType | SystemDefineTable (1) |
| TableName | `View_TchpPomSpecLine` |
| IdentityField | `PomSpecLineId` |
| DisplayFiled1 | `BodyPartName` |
| OtherSettings | `SortByField = Sort` |

Keep view DDL identical in `POM_Grading_QC_NewSchema.sql` and `3b_Tchp_ImportFromDW.sql`.

**2) `TchpFitMeasurement` unit fields**

| Sort | DbName (stable) | Store | Control | Notes |
|------|-----------------|-------|---------|-------|
| 30 | `PomSpecLineId` | DatabaseTable | DDL (1) | Entity = `PomSpecLine` |
| 35 | `InitValue` | **TemporaryField** (`IsTempVariable=1`) | Numeric (20), NBDecimal=4 | Subscribe from PomSpecLine DDL: `MasterEntityFieldlID` → PomSpecLineId field, `InnerEntitySubscribeFiled` = `BaseValue` |
| 36 | `Tol` | TemporaryField | Numeric (20), NBDecimal=4 | Same master; `InnerEntitySubscribeFiled` = `Tolerance` |
| 40 | `ActualValue` | DatabaseTable | Numeric (20), NBDecimal=4 | PLM Meas / user entry |
| 60 | `Diff` | TemporaryField | Numeric (20), NBDecimal=4 | Formula result |

PK / link columns (`FitMeasurementId`, `FitRoundId`) stay hidden.

**3) Formula (unit = Fit Measurement)**

| Setting | Value |
|---------|-------|
| OperationType | Assignment (1) |
| FormulaName | `FitDiff_ActualMinusInit` |
| Expression | `transactionfieldid_{DiffId} = transactionfieldid_{ActualValueId} - transactionfieldid_{InitValueId}` |
| CaculationFlowSort | 1 |

**4) Transaction flag**

`AppTransaction.IsShowCalculateButton = 1` on **TX_FitRound** (Calculation toolbar button).

**5) Phase D / Blueprint**

`ApplyTechPackFitRoundMeasurementGoldenFieldTemplate` (after FitRoundInfo golden): ensure view entity, temp fields + subscribe, Diff assignment formula, Calculate button. Form layout still via Form Design Reset & Auto Design (do not insert orphan `AppFormLayoutItem`).

Optional later: filter PomSpecLine DDL to current round’s `StyleSpecId` (not required for MVP if grid rows are already seeded by import).

#### F3 — Read-only summary pivot (feasibility confirmed)

Same pattern as P1 GradeValue ↔ `View_TchpStyleActiveSizeRunSizes`:

| Setting | Value |
|---------|-------|
| Child | `TchpPomSpecLine` (StyleSpecId → Root.ReferenceId); user-facing POM list |
| Grandchild | `View_TchpFitMeasurementByPom` |
| Unit `EmGridViewDisplayType` | 7 ChildUnitPivotColumns |
| IsPivotColumn | `RoundNumber` (MatrixFK → FitRound list unit / RoundNumber) |
| IsPivotValue | `ActualValue` |
| IsReadOnly | **required** on PomSpecLine (in this TX) + view grandchild; disable add/delete |
| Form | Show POM + pivoted Fit columns; omit pure pivot-domain unit if unused on layout |

**View_TchpFitMeasurementByPom** (keep in sync with schema + 3b):

```sql
CREATE OR ALTER VIEW dbo.View_TchpFitMeasurementByPom
AS
SELECT
    fm.FitMeasurementId,
    fm.PomSpecLineId,
    pl.StyleSpecId,
    fr.FitRoundId,
    fr.RoundNumber,
    fr.RoundType,
    CONCAT(N'Fit ', fr.RoundNumber) AS RoundLabel,
    fm.ActualValue
FROM dbo.TchpFitMeasurement AS fm
INNER JOIN dbo.TchpFitRound AS fr ON fr.FitRoundId = fm.FitRoundId
INNER JOIN dbo.TchpPomSpecLine AS pl ON pl.PomSpecLineId = fm.PomSpecLineId;
```

Writable Fit edits stay on **FIT ROUND** TX (`TchpFitMeasurement` under that round). SUMMARY F3 is display-only.

#### Round alignment (R1) — import sketch

```
SpecFit DW columns SampleN / ReviseN
        │
        ▼  RoundNumber = N
TchpFitRound (StyleSpecId, RoundNumber, RoundType)
        │
        ▼  SampleN (Meas) → ActualValue; ReviseN = Rev.Spec (not ActualValue)
TchpFitMeasurement (FitRoundId, PomSpecLineId ← BodyPart match)

PLM Tab "Fit N" / "Fit N Comments" non-grid SubItems
        │
        ▼  RoundNumber = N (from Tab name digit = same N as SampleN)
Plm_FitRoundInfo row for that FitRoundId (semantic columns)
```

### TechPack Grading golden field template (G1)

Apply on tabs that have `TchpPomSpecLine` (+ GradeValue / SizeRunSizes view):

**TchpStyleSpec**

| Field | ControlType | Entity | Width | Sort | Visible | Notes |
|-------|-------------|--------|-------|------|---------|-------|
| SizeRunId | DDL (1) | SizeRun | 100 | 20 | 1 | |
| BaseSizeDetailId | DDL (1) | SizeRunDetail | 100 | 30 | 1 | `DDLParentLevelID` = SizeRunId |
| UnitOfMeasure | TextBox (2) | UnitOfMeasure | 100 | 40 | 1 | keep TextBox |
| VisibleSizes | **MultiSelectDDL (53)** | SizeRunDetail | 200 | 45 | 1 | **Grading TX only** (V1). `DDLParentLevelID` = SizeRunId; RelationalTable cascade to `TchpSizeRunSize`; nvarchar pipe-delimited **SizeRunSizeId**; NULL/empty = show all Dimension-visible sizes. **Omit on Fit Summary / Fit Round.** |

**TchpPomSpecLine**

| Field | ControlType | Entity | Width | Sort | Visible | Notes |
|-------|-------------|--------|-------|------|---------|-------|
| Sort | TextBox | — | 100 | 25 | 1 | `GroupByLevel=1` |
| BodyPartId | DDL | TchpBodyPart | **200** | 30 | 1 | |
| BodypartAliasName | TextBox | — | **200** | 35 | 1 | |
| GradeRuleSetId | DDL | TchpGradeRuleSet | **150** | 40 | 1 | |
| BaseValue | (schema) | — | **150** | 50 | 1 | |
| Tolerance | (schema) | — | **150** | 60 | 1 | |
| IsFixed | TextBox (2) | — | **150** | 70 | 1 | do **not** force CheckBox |

**TchpGradeValue (P1)**

| Setting | Value |
|---------|-------|
| Unit `EmGridViewDisplayType` | 7 ChildUnitPivotColumns |
| SizeRunSizeId | IsPivotColumn; MatrixFK → View.SizeRunSizeId; MatrixKey → View.IsVisible; DDL SizeRunDetail; width 150 |
| GradingDelta | IsPivotValue; width 150 |
| MatrixKey | View.`IsVisible` (selected DimensionCode) |

**View_TchpStyleActiveSizeRunSizes**

| Setting | Value |
|---------|-------|
| Parent | ROOT; Link StyleSpecId → ReferenceId |
| **Is Read-Only** | **必选** `IsReadOnly=1`；并 `IsDisableAddButton=1` / `IsDisableDeleteButton=1`（Phase D 对 `View_TchpStyleActiveSizeRunSizes` **强制**写入，不依赖 JSON 标志） |
| Columns | SizeRunSizeId, SizeLabel, SizeOrder, IsActive, **IsVisible** (Dimension AND `TchpStyleSpec.VisibleSizes` whitelist of SizeRunSizeId; empty VisibleSizes = no extra filter) |
| Visible fields | SizeRunSizeId, SizeLabel, SizeOrder (`GroupByLevel=1` on SizeOrder); IsVisible hidden (MatrixKey only) |
| Form layout | **omit** (not a user-facing grid) |

Blueprint JSON array: `techPackGradeValuePivotBindings` (generator emits for `role=Grading` when View + PomSpecLine present).

### TechPack Simple QC — QX1 import exception (locked)

**Scope:** PLM Spec QC import is **Simple QC** only. When IMPORT detects a **Spec QC** system block / `SpecQCGrid` on the current tab (or `techPack.bindings` `role=SimpleQC` / `systemBlockGrids` `role=SpecQC`), take the Simple QC path — do **not** emit a flat `Plm_SpecQCGrid` with `QCSize1`…`QCSize20` columns.

QC Order / Garment tables and transactions are a **separate APP configuration** (not imported from PLM). This path must **not** create or alter them.

#### QX1 — APP tables

| PLM source | APP target | Notes |
|------------|------------|-------|
| QC Tab — non-grid SubItems (comments, color, etc.) | Sibling `Plm_{Tab…}` (auto name from TAB) | Same as normal tab sibling DDL/MAPPING |
| QC Tab Size Selector (checked sizes) | **`TchpStyleSpec.QcSelectedSizes` only** | Source of truth = PLM **`PdmProductQcSize`** (`ProductReferenceID` + QC `TabID`) → pipe-delimited **SizeRunRotateID** (= APP `SizeRunSizeId`). **Do not** use DW Tab `Selected_Size` — that column is the **full Size Run list**, not the checkbox selection. **Do not** emit `Plm_{Tab}.SelectedSizes` |
| QC Tab Size_Run / Base_Size / Measure_Unit | `TchpStyleSpec` only | **S1** strip from `Plm_*`; on Simple QC TX these StyleSpec fields are **IsReadOnly** (filled by Grading import / shared StyleSpec) |
| SpecQCGrid — size-**independent** columns | Child `Plm_SimpleQC` | One POM / CriticalPoint row; PK `RowId`; FK `ReferenceId` |
| SpecQCGrid — size-**dependent** columns `*{N}` | Grandchild `Plm_SimpleQCResult` | One row per `(SimpleQC RowId, SizeRunSizeId)`; strip trailing size index from column names |
| — | Child `View_TchpSimpleQcSelectedSizes` | Pivot column domain (Form omit); link `StyleSpecId` → Root.`ReferenceId`; **IsReadOnly** |

#### QX1 — Transaction shape: **Simple QC**

| Unit | Table | Kind |
|------|-------|------|
| Root | `Plm_ReferenceBasicInfo` (or template root) | Master |
| Sibling | `TchpStyleSpec` | Shared; SizeRun / BaseSize / UOM **read-only** on this TX; **`QcSelectedSizes` MultiSelectDDL (53)** on Form (same cascade as Grading `VisibleSizes`: Entity SizeRunDetail, parent SizeRunId) |
| Sibling | `Plm_{QC Tab}` | Tab non-grid fields only — **no** Sizes / SelectedSizes |
| Child (pivot domain) | `View_TchpSimpleQcSelectedSizes` | Like V1 SizeRunSizes; Form **omit** (pivot column domain only; not a user-facing grid) |
| Child | `Plm_SimpleQC` | POM list |
| Grandchild | `Plm_SimpleQCResult` | `EmGridViewDisplayType = ChildUnitPivotColumns (7)`; pivot by Selected QC sizes |

#### QX1 — `Plm_SimpleQC` columns (SIZE-independent)

| PLM SpecQCGrid InternalCode / stem | APP Column |
|------------------------------------|------------|
| CriticalPoint | CriticalPoint |
| BodyPartDetailIDWDimDetailID | BodyPartDetailIDWDimDetailID |
| Code | Code |
| BodyPartName | BodyPartName |
| BodyPartDesc | BodyPartDesc |
| HowToMeasure | HowToMeasure |
| Tolerance | Tolerance |
| GradingBaseSize | GradingBaseSize |
| Commtents | Commtents |
| Add.Desc / Add_Desc / AddDesc | **AddDesc** |
| DimensionDetail | DimensionDetail |
| Dimension | Dimension |
| NeedToApplyGradingRule | NeedToApplyGradingRule |

Plus `RowId` (identity PK), `ReferenceId`, `Sort` when present on DW.

#### QX1 — `Plm_SimpleQCResult` columns (SIZE-dependent; single series)

PLM has slots `1…MaxiumGradingSizeCounter` (typically 20). **Not** dual-series (`QCSize11` is size slot 11, not a second measure for size 1).

| PLM (Index=N) | APP Column |
|---------------|------------|
| GradingSize**N** | GradingSize |
| QCSize**N** | QCSize |
| Difference**N** | Difference |
| QCSizeBeforeWash**N** | QCSizeBeforeWash |
| DiffBeforeWashAndGrading**N** | DiffBeforeWashAndGrading |
| QCAfterWashIron**N** | QCAfterWashIron |
| DiffAfterIronAndGrading**N** | DiffAfterIronAndGrading |
| QCAfterIron**N** | QCAfterIron |

Plus: `RowId`, `ParentRowId` → `Plm_SimpleQC.RowId`, **`SizeRunSizeId`** (required).

#### QX1 — Size Index N → `SizeRunSizeId` (from PLM source)

Locked from `PomHelper.GetDictSortSizeRelatedRotateSizeId` + `SpecBlockControlHelper.SetupQCSizeDisplay`:

1. Load SizeRun sizes ordered by **`SizeOrder`** (`TblSizeRunRotate` / `TchpSizeRunSize`).
2. Column suffix **N** = **1-based position** in that full list (`QCSize{N}` ↔ sizes[N−1]).
3. **Selected** sizes (`PdmProductQcSize` rows for the QC TabId) only control **column visibility** in PLM — they do **not** renumber slots (unselected sizes keep their N; columns hide). DW Tab `Selected_Size` is the Size Run member list and must **not** be copied to `QcSelectedSizes`.

Import UNPIVOT:

```
SizeRunId = QC Tab Size_Run (or TchpStyleSpec.SizeRunId)
sizes = TchpSizeRunSize WHERE SizeRunId ORDER BY SizeOrder
FOR N = 1 .. Max:
  IF sizes[N-1] exists:
    INSERT Plm_SimpleQCResult (…, SizeRunSizeId = sizes[N-1].SizeRunSizeId,
      GradingSize = GradingSizeN, QCSize = QCSizeN, …)
```

Pivot UI: Matrix column domain = `View_TchpSimpleQcSelectedSizes.SizeRunSizeId`; MatrixKey = `IsVisible` (QcSelectedSizes whitelist; empty = show all SizeRun sizes).

**Size-related measure columns** (GradingSize / QCSize / Difference / wash / iron): visibility and **AliasName** come from PLM **`pdmTabGridMetaColumn`** for the QC Tab + SpecQCGrid (`TabID + GridColumnID`). Size slots `1…N` of the same stem are folded to one APP field: stem is **visible** if **any** slot has `Visible=1`; DisplayName = first non-empty `AliasName` (trimmed), e.g. `Size` / `Meas` / `Delta`. Hidden stems are not `IsPivotValue`. Different QC tabs / PLM databases can show a different measure set.

Phase D: StyleSpec SizeRun/BaseSize/UOM **IsReadOnly** on Simple QC TX; **`QcSelectedSizes` MultiSelectDDL (53)** (same cascade as Grading VisibleSizes); strip leftover Plm_* Sizes fields; omit `View_TchpSimpleQcSelectedSizes` from Form layout; apply Simple QC pivot bindings (mirror P1 GradeValue ↔ size view); apply PLM tab grid **measure visibility + alias**.

#### QX1 — `View_TchpSimpleQcSelectedSizes`

Keep identical in `POM_Grading_QC_NewSchema.sql` and emitted `3b_Tchp_ImportFromDW.sql` when Simple QC present:

- Columns: `SizeRunSizeId`, `SizeLabel`, `SizeOrder`, `StyleSpecId`, `IsVisible`
- `IsVisible`: `TchpStyleSpec.QcSelectedSizes` pipe/comma whitelist of SizeRunSizeId; NULL/empty = all sizes visible (same semantics as Grading `VisibleSizes`)
- Does **not** use Grading `VisibleSizes`

#### QX1 — config / generator keys

| Key | Purpose |
|-----|---------|
| `techPack.systemBlockGrids[]` `role=SpecQC` | Skip flat Plm_* SpecQCGrid DDL; source for UNPIVOT |
| `techPack.bindings[]` `role=SimpleQC` | TX unit tree: StyleSpec + Plm tab sibling + size view + SimpleQC/Result |
| `techPackSimpleQcPivotBindings` | Blueprint Phase D: Result unit → ChildUnitPivotColumns; **`pivotValueFields`** = stems with any `pdmTabGridMetaColumn.Visible=1` on this QC Tab; **`pivotValueLabels`** = tab `AliasName` |

Phase D: StyleSpec SizeRun/BaseSize/UOM **IsReadOnly** on Simple QC TX; **`QcSelectedSizes` MultiSelectDDL (53)** (same cascade as Grading VisibleSizes); strip leftover Plm_* Sizes fields; omit `View_TchpSimpleQcSelectedSizes` from Form layout; apply Simple QC pivot bindings (mirror P1 GradeValue ↔ size view); apply PLM tab grid **measure visibility + alias**.
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
- **Unrelated C# / WebAPI changes** during PROMPT runs (BOM pivot BL is already in repo)  
- Full production load without explicit user request  
