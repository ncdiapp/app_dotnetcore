# Mass Update View 8 — 01- Style Mass Update View

**Decision:** Option **A** (SingleTableAttach) — **no ListEdit**  
**Generated:** 2026-07-13  
**Blueprint:** `3_PlmSearch_MassUpdateView_8.json`

## Target

| Item | Value |
|------|--------|
| APP Search | resolve by `Search_1aStyleSearch` (tenant-safe; not hardcoded SearchId) |
| DataSet | #2087 fallback via `appDataSetId` if `AppSearch.DataSetID` is null |
| Update Transaction | `Tab_4251` Style Header (by IntegrationId) |
| Update Unit | `Plm_Style_Header` (by table name) |
| PK | `ReferenceId` |
| Default MU | **false** |

## Phase D

1. **Rebuild / restart** the APP backend so Mass Update BL fixes are loaded
2. Open **PLM Data Import → PLM Search Import**
3. Re-upload `3_PlmSearch_MassUpdateView_8.json`
4. Validate Preview → Execute

Confirm you are logged into the **TenantDB_PLM27** company (`Search_1aStyleSearch` must exist).

Display default View is left unchanged.

## Skipped / unmapped

Supplier, shipping dates, CancelByDate, Country Of Origin, Weight/Total Composition TD — FieldMapping gaps or non–Style Header tables. Composition shown read-only from `Plm_Style_Summary`.
