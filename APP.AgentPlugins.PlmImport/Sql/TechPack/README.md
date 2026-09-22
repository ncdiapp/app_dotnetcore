# TechPack SQL (product assets)

Runtime DDL for `ensure_techpack_schema` (embedded into `APP.AgentPlugins.PlmImport.dll`).

| File | When |
|---|---|
| `POM_Grading_QC_NewSchema.sql` | Always (full Tchp* tables + views) |
| `POM_Grading_QC_InspectionAddon.sql` | Only if `includeInspectionAddon=true` |

**Edit these files** for product schema changes. Do not depend on `Document/Design/` drafts.
