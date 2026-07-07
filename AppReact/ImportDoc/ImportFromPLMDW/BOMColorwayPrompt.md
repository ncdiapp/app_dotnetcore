# BOM Colorway Pivot — merged into PROMPT.md

> **This prompt is deprecated.** BOM ProductDesignColor colorway pivot import is fully integrated into the main DW import pipeline.

**Use instead:** [`PROMPT.md`](./PROMPT.md) — see **§A8** (detection), **§B5–B6** (SQL steps 5–6), and **Execution order** (steps 1–6).

**Generator:** `source/_gen_plmdw_import_sql.ps1` (dot-sources `source/_gen_plmdw_bom_colorway.ps1`).

**Outputs (when detected):**

| Step | File |
|------|------|
| 4 | `output/{templateId}/4_PlmDw_ImportBlueprint.json` |
| 5 | `output/{templateId}/5_PlmDw_ImportBomColorwayGrandchild.sql` |
| 6 | `output/{templateId}/6_PlmDw_CleanupBomColorwayStaging.sql` |

**Example:** `output/3351/` — Grid 3167 / Tab 4246 → `Plm_Artwork_BOM_prodGrandColorway`.

**Legacy scripts** (`bomColorwayImportConfig.json`, `_gen_bom_colorway_import_sql.ps1`) are no longer used.
