# PLM BOM Grid Colorway Cells → APP Grandchild Pivot Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportFromPLMDW/`  
> **Companion:** `PROMPT.md` (DW flat grid + tab import). This prompt covers the **second step**: UNPIVOT BOM `Colorway_N` / `Image_N` wide slots into **ChildUnitPivotColumns** grandchild storage.  
> **Outputs:** `output/{templateId}/PlmDw_ImportBomColorwayGrandchild.sql`  
> **Example:** `output/3351/PlmDw_ImportBomColorwayGrandchild.sql` (Artwork BOM Grid 3167 / Block 5052)

---

## When to use this prompt

Use after the standard DW import (`PlmDw_ImportFromDW.sql`) has created:

1. **Host** child grid rows (e.g. `Plm_Artwork_BOM_prod`) — one row per BOM line  
2. **Source** colorway grid rows (e.g. `Plm_ProductDesignColorGrid`) — for pivot column headers  
3. **Grandchild** physical table exists (e.g. `Plm_ArtworkGrandColorway`) with pivot fields configured in APP Transaction Designer  

This prompt does **not** create transactions/units/fields — only imports **cell values** into an existing grandchild table.

---

## User input (required)

**Minimum for Phase A probe:**

```text
1. PLM connection string (pdmStyleColorWayMapping, pdmGridMetaColumn, pdmProductTemplate)
2. plmDW connection string
3. APP tenant connection string (physical tables + AppTransactionUnit/Field)
4. TemplateId (e.g. 3351)
5. BOM grid identity (all three):
   - PlmTabId        (e.g. 4246)
   - PlmGridId       (e.g. 3167)
   - ProductGridBlockId (e.g. 5052)  ← from PLM block hosting the grid
6. APP table mapping (host + grandchild physical table names, without or with prefix — be consistent):
   - HostAppTable       (e.g. Artwork_BOM_prod)
   - GrandchildAppTable (e.g. ArtworkGrandColorway)
```

**Optional (defaults if omitted):**

| Parameter | Default |
|-----------|---------|
| `@TablePrefix` | `Plm_` |
| `@ImportMode` | `APPEND` |
| `@ReferenceIdList` | all refs in `pdmProductTemplate` for TemplateId |
| `@DryRun` | `0` |
| Grandchild column names | `ParentRowId`, `Colorway`, `ArtworkColor`, `ArtworkPhoto` |

**If multiple BOM grids exist in one APP transaction**, also provide **one** of:

- APP `TransactionID` + `HostUnitId` + `GrandchildUnitId` (verified in tenant DB), **or**
- Explicit `HostAppTable` + `GrandchildAppTable` pair (unique within tenant)

`PlmTabId` alone is **not** enough when several BOM child+grandchild pairs share a transaction.

### Gate 0 — missing input → ask user, do nothing else

If the user references only `@BOMColorwayPrompt.md` without PLM + plmDW + **tenant** connections and the six required identifiers above → **STOP and ask**.

---

## Hard rules

| Rule | Detail |
|------|--------|
| **No server code** | Deliverables are **SQL + JSON + PowerShell in this folder only**. Never edit C# / React / BL. |
| **Mapping is explicit** | Column key = `pdmStyleColorWayMapping.StyleColorID` (pdmRGBColor id). **Not** slot position / Sort alone. |
| **Host must exist first** | Grandchild `ParentRowId` → host `RowId`. Run `PlmDw_ImportFromDW.sql` for the host grid before this script. |
| **Skip empty slots** | Do not insert when both DW `Colorway_N` and `Image_N` are NULL. |
| **Skip unmapped slots** | Do not insert when no `pdmStyleColorWayMapping` row for `(ProductReferenceID, ProductGridBlockID, ColorWayGridColumnID)`. |
| **plmDW + FieldMapping** | Slot → DW column names come from tenant `{prefix}FieldMapping` (`Colorway_N`, `ImageN`, `PlmMetaColumnId`). |

---

## PLM data model (authoritative)

### Wide BOM grid (plmDW)

Each materialized grid `PLM_DW_Grid_{Segment}_{GridId}` stores colorway cells as **fixed slots**:

| DW column pattern | Meaning |
|-------------------|---------|
| `Colorway_N_{metaColumnId}_FK_pdmRGBColor` | Artwork color chosen for colorway slot N (cell value) |
| `ImageN_{metaColumnId}_FK_tblSketch` | Artwork photo for slot N |

`PlmMetaColumnId` in FieldMapping = `ColorWayGridColumnID` in mapping table.

### Explicit colorway column mapping (PLM live DB)

Table: **`pdmStyleColorWayMapping`**

| Column | Role |
|--------|------|
| `ProductReferenceID` | Style |
| `ProductGridBlockID` | BOM block (e.g. **5052**) |
| `ColorWayGridColumnID` | BOM grid meta column id for slot N |
| `StyleColorID` | **Column header key** — pdmRGBColor id ("Lt Grey") |

Populated via PLM UI popup; BL: `PdmStyleColorWayMappingBL.cs`. Initial backfill script: `V2K.PLM.SQLUpdater/.../2026_03_18_ColorwayMapping_Init.sql` (Sort-based seed only — runtime uses explicit table).

### APP pivot storage (tenant DB)

| Layer | Typical table | Pivot role |
|-------|---------------|------------|
| Host child | `Plm_{BomGrid}` | BOM line rows |
| Source child | `Plm_ProductDesignColorGrid` | Column domain (`Color` field) |
| Grandchild | `Plm_{Grandchild}` | `Colorway` = StyleColorID, `ArtworkColor` / `ArtworkPhoto` = cell values |

Grandchild unit: `EmGridViewDisplayType = 7` (ChildUnitPivotColumns).  
Pivot column field: `IsPivotColumn = 1`, `MatrixForeignKeyFieldId` → Source unit `Color` field.

---

## Phase A — Discovery (STOP for user confirmation)

### A1. Probe PLM BOM colorway columns

```sql
-- Colorway slots on grid (SpecialColumnType = ProductGridKeyDCUColumn, DCU → ProductDesignColor)
SELECT g.GridID, g.GridColumnID, g.ColumnName, g.ColumnOrder, g.EntityID
FROM dbo.pdmGridMetaColumn g
WHERE g.GridID = @PlmGridId
  AND g.SpecialColumnType = 3  -- ProductGridKeyDCUColumn — verify enum in source if needed
ORDER BY g.ColumnOrder;
```

Confirm `ProductGridBlockId` from `pdmBlock` / tab layout for Tab `@PlmTabId`.

### A2. Probe mapping coverage

```sql
SELECT COUNT(*) AS MappingRows,
       COUNT(DISTINCT ProductReferenceID) AS RefCount
FROM dbo.pdmStyleColorWayMapping
WHERE ProductGridBlockID = @ProductGridBlockId;
```

Sample one reference:

```sql
SELECT ColorWayGridColumnID, StyleColorID
FROM dbo.pdmStyleColorWayMapping
WHERE ProductReferenceID = @SampleRef AND ProductGridBlockID = @ProductGridBlockId
ORDER BY ColorWayGridColumnID;
```

### A3. Probe DW + FieldMapping slots

On **tenant DB**:

```sql
SELECT AppColumnName, DwColumnName, PlmMetaColumnId
FROM dbo.Plm_FieldMapping
WHERE AppTableName = @HostTable
  AND PlmGridId = @PlmGridId
  AND AppColumnName LIKE 'Colorway[_]%'
ORDER BY PlmMetaColumnId;
```

On **plmDW**: confirm `PLM_DW_Grid_*_{GridId}` exists.

### A4. Resolve APP units (tenant DB)

When transaction has **one** pivot grandchild:

```sql
SELECT u.TransactionUnitID, u.DataBaseTableName, u.ParentTransactionUnitId,
       u.EmGridViewDisplayType, t.TransactionID, t.TransactionName, t.IntegrationId
FROM dbo.AppTransactionUnit u
JOIN dbo.AppTransaction t ON t.TransactionID = u.TransactionID
WHERE u.EmGridViewDisplayType = 7;
```

For **multiple** pivot pairs in same transaction, filter by `DataBaseTableName` or user-supplied unit ids.

Verify grandchild fields:

```sql
SELECT DataBaseFieldName, IsPivotColumn, IsPivotValue, MatrixForeignKeyFieldId
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @GrandchildUnitId;
```

### A5. Confirmation checklist — **STOP**

Ask user to confirm:

1. `ProductGridBlockId` + `PlmGridId` + DW table name  
2. Host / Grandchild / Source table names  
3. Grandchild column names (`Colorway`, `ArtworkColor`, `ArtworkPhoto`, `ParentRowId`)  
4. `StyleColorID` = pivot column key (pdmRGBColor)  
5. Host row match strategy: `ReferenceId` + `ROW_NUMBER` ordered by `Sort`, `RowID` / `RowId`  
6. `@ImportMode` APPEND vs REPLACE  
7. Pilot `@ReferenceIdList` for smoke test  

Record in `source/bomColorwayImportConfig.json`.

---

## Phase B — Generate SQL

### B1. Write `source/bomColorwayImportConfig.json`

See `source/bomColorwayImportConfig.example.json`.

### B2. Run generator

```powershell
powershell -File AppReact/ImportDoc/ImportFromPLMDW/source/_gen_bom_colorway_import_sql.ps1
```

Produces: `output/{templateId}/PlmDw_ImportBomColorwayGrandchild.sql`

### B3. Script behavior summary

1. Build `#RefFilter` from `pdmProductTemplate` (+ optional pilot list)  
2. Load `#SlotMap` from `{prefix}FieldMapping` (`Colorway_N` ↔ `ImageN` ↔ `PlmMetaColumnId`)  
3. Stage DW BOM rows; rank for host match  
4. Stage host rows; rank within `ReferenceId`  
5. **UNPIVOT** slots via dynamic `CROSS APPLY` (skip empty cells)  
6. Join `pdmStyleColorWayMapping` → `StyleColorID` as `Colorway` key  
7. Join host on `ReferenceId` + rank → `ParentRowId`  
8. `INSERT` into grandchild table (`APPEND` skips existing `ParentRowId` + `Colorway`)  

Log steps: `DW_BOM_ROWS`, `HOST_ROWS`, `DW_CELLS`, `INSERT_GRANDCHILD`, `SKIP_NO_MAPPING`, `SKIP_NO_HOST`.

---

## Execution order (APP tenant database)

```text
1. output/{templateId}/PlmDw_Tables.sql
2. output/{templateId}/PlmDw_FieldMapping.sql
3. output/{templateId}/PlmDw_ImportFromDW.sql     ← host + source grids
4. (APP) Execute pivot transaction blueprint / manual unit config
5. output/{templateId}/PlmDw_ImportBomColorwayGrandchild.sql   ← this prompt
```

**Run step 5 on the tenant database** (e.g. `TenantDB_PLM23`), not AppMasterDB.

Recommended first run:

```sql
DECLARE @DryRun = 1;
DECLARE @ReferenceIdList = N'31614';  -- one style with BOM + mapping
```

Then set `@DryRun = 0` for production.

---

## Multi BOM grid transactions

One APP transaction may contain several `(Host, Grandchild, Source)` triples. Each PLM BOM grid is distinguished by:

| PLM | APP |
|-----|-----|
| `ProductGridBlockID` | Host table (one grid → one host unit/table) |
| `PlmGridId` | FieldMapping `PlmGridId` filter |
| `pdmStyleColorWayMapping` rows | Scoped per block id |

Generate **one** `PlmDw_ImportBomColorwayGrandchild.sql` per BOM grid pair (or one script with parameters per grid). Do not assume `Tab_{TabId}` IntegrationId — pivot transactions may have `IntegrationId IS NULL`.

---

## Agent checklist

```text
[ ] Gate 0: PLM + plmDW + tenant connections + TemplateId + TabId + GridId + BlockId + host/grandchild tables?
[ ] Phase A: pdmStyleColorWayMapping exists and has rows for block
[ ] Phase A: FieldMapping Colorway_N slots match grid meta columns
[ ] Phase A: APP grandchild unit EmGridViewDisplayType=7, pivot fields verified
[ ] User confirmed checklist
[ ] bomColorwayImportConfig.json written
[ ] Generator run → output/{templateId}/PlmDw_ImportBomColorwayGrandchild.sql
[ ] DryRun with pilot ReferenceIdList; review SKIP_NO_HOST / SKIP_NO_MAPPING counts
```

---

## Example session message

```text
按 AppReact/ImportDoc/ImportFromPLMDW/BOMColorwayPrompt.md 生成 BOM colorway grandchild 导入 SQL。

PLM: Server=PC3B\MSSQLSERVER01;Database=plm_live_20260602;User ID=sa;Password=...
plmDW: Server=PC3B\MSSQLSERVER01;Database=PLMDW;User ID=sa;Password=...
Tenant: Server=PC3B\MSSQLSERVER01;Database=TenantDB_PLM23;User ID=sa;Password=...

TemplateId: 3351
PlmTabId: 4246
PlmGridId: 3167
ProductGridBlockId: 5052
HostAppTable: Artwork_BOM_prod
GrandchildAppTable: ArtworkGrandColorway
```

---

## Out of scope

- Creating APP transactions / pivot unit configuration (manual or separate blueprint)  
- Importing non-colorway BOM columns (handled by `PlmDw_ImportFromDW.sql`)  
- Any C# / WebAPI changes  
- PLM → APP automatic unit discovery without tenant DB probe  

---

## Reference (3351 / Artwork BOM)

| Item | Value |
|------|-------|
| TemplateId | 3351 |
| TabId | 4246 (Artworks) |
| GridId | 3167 |
| BlockId | 5052 |
| DW table | `PLM_DW_Grid_Artwork_BOM_prod_3167` |
| Host | `Plm_Artwork_BOM_prod` |
| Grandchild | `Plm_ArtworkGrandColorway` |
| Source | `Plm_ProductDesignColorGrid` |
| Pivot transaction | TransactionID 15 (IntegrationId NULL) |
| Units | Host 34, Grandchild 36, Source 37 |

PLM mapping source: `C:\Dev\PLM3\PLM\Com.Visual2000.BL\ProductReference\PdmStyleColorWayMappingBL.cs`
