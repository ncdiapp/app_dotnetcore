# Batch Mass Update Inventory (junction × APP-imported Search)

**Generated:** 2026-07-13  
**Source:** `pdmSearchTemplateReferenceView` where `MassUpdateViewID IS NOT NULL`  
**Filter:** APP tenant already has matching Search

## Scope table

| PLM Template | APP Search | MU Id | MU Name | UpdateType | Default? | Action |
|---|---|---:|---|---|---|---|
| 23702 | `Search_1aStyleSearch` #17 DS 2087 | 8 | 01- Style Mass Update View | TabField | no | A — blueprint exists, execute |
| 23702 | same | 114 | Delete Colors from Styles | RegularGrid | no | B2 — blueprint exists, refresh+execute |
| 23702 | same | 115 | Trim BOM MU | RegularGrid | no | B2 — generate+execute |
| 23902 | `Search_23902` #19 DS 2095 | 18 | 11- Trim Mass Update View | TabField | **yes** | A — generate+execute |
| 23902 | same | 111 | Trim Tracking MU | RegularGrid | no | **DONE** APP View #2105 / ListEdit #2284 — verify only |
| 24002 | `Search_24002` #20 DS 2096 | 22 | 12- Packaging Mass Update View | TabField | **yes** | A — generate+execute |
| 24102 | `Search_24102` #21 DS 2097 | 20 | 13- Label Mass Update View | TabField | **yes** | A — generate+execute |
| 25402 | `Search_25402` #22 DS 2098 | 49 | Publish to ERP | TabField | **yes** | A — generate+execute |
| 25402 | same | 48 | Publish Special Pricing… | DynamicMatrix | no | **SKIP** Matrix |
| 25402 | same | 79 | Update IPC Codes | DynamicMatrix | no | **SKIP** Matrix |
| 28902 | `Search_28902` #18 DS 2091 | 9 | 10- Fabric MU | TabField | **yes** | A — blueprint exists, execute |
| 30223 | `Search_30223` #33 DS 2109 | 112 | Robert's Board MU | RegularGrid | no | B2 — generate+execute |
| 30223 | same | 116 | Robert's Board Price MU | RegularGrid | no | B2 — generate+execute |

## Out of scope

Junction templates with MU but **no** APP Search import: 25002, 25502, 25602, 26402, 26502, 26702, 27202, 27602, 27702, 27902, 28402, 28602, 30202, 30208, 30210, 30222, 30230, …

## Decision defaults

- TabField → Option A `SingleTableAttach`
- RegularGrid → Option B2 CreateNew ListEdit
- DynamicMatrix → skip
- `setAsDefaultMassUpdateView` = PLM Default match

## Deliverables

- Phase D checklist: [`_batch_mu_PHASE_D_EXECUTE.md`](_batch_mu_PHASE_D_EXECUTE.md)
- Blueprints: MU 8/9/18/20/22/49/114/115/112/116 under `output/{templateId}/`
- APP so far: only MU **111** executed (View #2105). Remaining need Import UI Execute (API requires admin auth).
- Matrix 48/79: `25402/README_MassUpdate_Matrix_SKIP.md`
