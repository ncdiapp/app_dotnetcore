# Mass Update View 111 — Trim Tracking MU

**Decision:** Option **B2** (HierarchicalListEdit + CreateNew ListEdit)  
**Generated:** 2026-07-13  
**Blueprint:** `3_PlmSearch_MassUpdateView_111.json`

## Target

| Item | Value |
|------|--------|
| APP Search | `Search_23902` (#19) |
| DataSet | #2095 (no 1:N join patch) |
| ListEdit | **CreateNew** `ListEdit_MU111_TrimTracking` |
| Root unit | `Plm_Trim_Header` PK `ReferenceId` |
| Child unit | `Plm_Trims_Tracking` PK `RowId` FK `ReferenceId` |
| MU SearchView | root PK map only |
| Default MU | **false** (PLM default remains MU 18) |

## ListEdit fields (approved — PLM MU Sort / IsHide)

**Root identity (always when present on unit):** Ref No. (`ReferenceId`), Article / Product Code, Description — other header columns stay hidden.

**Child (PLM Sort → SortOrder×10):**  
| PLM Sort | Column | Visible | Readonly |
|----------|--------|---------|----------|
| 1 | ID | yes | yes |
| 2 | Combo_Color | yes | no |
| 3 | Season | yes | no |
| 5 | Status | yes | no |
| 6 | Date | yes | no |
| 7 | Comment | yes | no |

All other Tracking columns hidden.

## Phase D

1. Open **PLM Data Import → PLM Search Import**
2. Upload `3_PlmSearch_MassUpdateView_111.json` (mode `MassUpdateViewAttach`)
3. Validate Preview → Execute  
   - Creates ListEdit first, then Mass Update View
4. Confirm display default View (#2086) unchanged

## Notes

- PLM MU is **inactive** (`IsActive=0`) but linked to SearchTemplate 23902.
- Do **not** join `Plm_Trims_Tracking` into the Search DataSet.
- Do **not** reuse/convert MasterDetail `Grid_3179` (#2273).
- Child FK `ReferenceId` must be **Link to Parent Primary Key** → root `ReferenceId` (BL now sets this from blueprint `fkColumn` after CreateHierarchy; schema FK auto-detect alone is insufficient for PLM tables).
- ListEdit field **ControlType / EntityId** come from blueprint `controlType` + `entityIntegrationId`, else **FieldMapping** (root SubItems + child GridColumns) by `AppTableName`/`AppColumnName`. CreateHierarchy alone defaults TextBox with no EntityId.
- **Hide/Show / Sort** follow PLM MU field list only (`isVisible = !(IsHide)`), **except root identity**: always show ReferenceId (+ Article/Product Code + Description when present on the unit). Non-MU other columns stay hidden but still get FieldMapping ControlType/EntityId.
- **UI:** FormListEdit must not fall back to `DictTransactionUnitIdFiledNameFiledID` when unit fields exist but are all hidden — that dumped every physical column without ControlType (first-layer Trim Tracking MU bug).

## Batch status (2026-07-13)

Already executed in APP as SearchView **#2105** / ListEdit **#2284**. Batch import: **verify only — do not duplicate**.
