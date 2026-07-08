# Batch OUTPUT — Folder 8619 templates (excluding 3351/3360)

Generated via `source/_gen_batch_folder8619_templates.ps1` (+ manual fix for `3361`).

| TemplateId | Name | Tabs (DW) | Grids | Steps 5–6 | Notes |
|-----------:|------|----------:|------:|:---------:|-------|
| 3348 | 02 - Fabrics | 6 | 11 | no | Missing DW Tab: 4214, 4215, 4217 — grids still imported where DW Grid exists |
| 3352 | 10 - Grading, Proto, SS and Fit | 8 | 2 | no | No `Article__22` — `referenceScope` uses Grading `Size_Run_43` (review if needed) |
| 3353 | 04 - Artwork | 1 | 1 | no | |
| 3354 | 03 - Labels & Hangtags | 2 | 5 | no | Missing DW Tab **4268** Tracking (grid Trims_Tracking still included) |
| 3355 | 05 - Packaging | 2 | 4 | no | Missing DW Tab **4268** |
| 3356 | 06 - Trims | 2 | 5 | no | Missing DW Tab **4268** |
| 3357 | 08 - Color Palette | 1 | 1 | no | Missing DW Tab **4255** Info — Colorways grid attached to Header |
| 3359 | 09 - Graphic Requests | 1 | 0 | no | `referenceScope` = `Request_Name_7154` (no Article) |
| 3361 | 07 - Sets | 4 | 12 | **yes** | Treated Header as template header; `Fabric_BOM_prod` shares 3351 name; Set Styles grid = `Set_Styles_Grid` |

## Config files

`source/dwTabImportConfig.{templateId}.json` for each id. Working `dwTabImportConfig.json` remains **3351**.

## Suggested run order (per template)

1. `1_PlmDw_Tables.sql`
2. `2_PlmDw_FieldMapping.sql`
3. `3_PlmDw_ImportFromDW.sql` (`@ImportMode=APPEND`, `@PlmTemplateId` set)
4. Phase D Execute Blueprint (`4_*.json`)
5–6. Only for **3361**

**Shared tables across templates:** `Plm_ProductDesignColorGrid`, SizeRun/Dimension, Publish-to-ERP price grids, etc. Use **APPEND**; FieldMapping is scoped per config — re-run that template’s `2_*.sql` before re-importing its data.
