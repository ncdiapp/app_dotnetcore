# Mass Update View 114 — Delete Colors from Styles

**Decision:** Option **B2** (HierarchicalListEdit + CreateNew ListEdit)  
**Generated:** 2026-07-13  
**Blueprint:** `3_PlmSearch_MassUpdateView_114.json`

## Target

| Item | Value |
|------|--------|
| APP Search | `Search_1aStyleSearch` (#17) |
| DataSet | #2087 (no 1:N join patch) |
| ListEdit | **CreateNew** `ListEdit_MU114_DeleteColors` |
| Root unit | `Plm_Style_Header` PK `ReferenceId` |
| Child unit | `Plm_ProductDesignColorGrid` PK `RowId` FK `ReferenceId` |
| MU SearchView | root PK map only |
| Default MU | **false** |

## ListEdit fields (approved)

**Root:** ReferenceId, Article #, Name  

**Child:** RowId, ReferenceId, Color (entity 79), Color_Code, Name, Description, ReferenceCode, ReferenceName  
(Color_Code / Name / Description / ReferenceCode / ReferenceName marked read-only from grid meta)

## Phase D

1. Open **PLM Data Import → PLM Search Import**
2. Upload `3_PlmSearch_MassUpdateView_114.json` (mode `MassUpdateViewAttach`)
3. Validate Preview → Execute  
   - Creates ListEdit first, then Mass Update View
4. Confirm display default View (#2083) unchanged

## Notes

- PLM MU is **inactive** (`IsActive=0`) but linked to SearchTemplate 23702.
- Purpose is delete colorway rows from styles — `isAllowDeleteRow=true`.
- Do **not** join `Plm_ProductDesignColorGrid` into the Search DataSet.
