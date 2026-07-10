# Output folder

Generated deliverables land here after **Phase B**.

## Main import ([`../PROMPT.md`](../PROMPT.md))

```text
output/{searchTemplateId}/
  1_PlmSearch_ImportBlueprint.json
  README.md   (optional — Phase A notes)
```

Example: `output/23702/1_PlmSearch_ImportBlueprint.json`.

### Batch 2026-07-10 (auto, no confirm)

| Status | SearchTemplateId | Name / note |
|--------|------------------|-------------|
| OK | 23902 | 1c - Trim Search → `Search_23902` |
| OK | 24002 | 2c - Packaging Search → `Search_24002` |
| OK | 24102 | 2b - Label Search → `Search_24102` |
| OK | 25402 | 4a - Publish To Erp Status → `Search_25402` |
| SKIP | 26202 | Vendor Request — no Request Header table / SubItem 3865 unmapped |
| OK | 28902 | 1b - Fabric Search → `Search_28902` |
| OK | 30206 | 3b - Artwork Search → `Search_30206` |
| OK | 30207 | 3a - Color Palette Search → `Search_30207` |
| OK | 30213–30221 | Style category searches (Bottoms/Shirts/…) → `Search_{id}` |
| OK | 30223 / 30233 | Robert's Board-10/20 (many colorway grid cols excluded) |
| SKIP | 30224 | Trims Approval — GridColumn-only Tracking/Approval |
| OK | 30228 | Betty's Requests → `Search_30228` |
| OK | 30229 | 3c - Fabric Costing → `Search_30229` |
| OK | 30231 | Recap by Fabric → `Search_30231` |

Defaults: JOIN AUTO (header primary + LEFT siblings), `gridColumnStrategy=exclude`, missing=ignore, IntegrationId=`Search_{plmId}`, menu=yes.  
Detail: `_batch_data/batch_summary.json`. All generated `queryText` validated on `TenantDB_PLM27`.

## Sibling View import ([`../PROMPT_SIBLING_VIEW.md`](../PROMPT_SIBLING_VIEW.md))

After an existing Search import, adding another PLM View:

```text
output/{searchTemplateId}/
  2_PlmSearch_SiblingView_{referenceViewId}.json     ← Option A (enrich DataSet + sibling View)
  1_PlmSearch_ImportBlueprint_V{referenceViewId}.json ← Option B (new Search Search_{Name}_V{ViewId})
  README_Sibling_{referenceViewId}.md                 (optional)
```

Architecture: [`../source/MULTI_VIEW_COVERAGE.md`](../source/MULTI_VIEW_COVERAGE.md)

Do not commit connection strings or tenant secrets.
