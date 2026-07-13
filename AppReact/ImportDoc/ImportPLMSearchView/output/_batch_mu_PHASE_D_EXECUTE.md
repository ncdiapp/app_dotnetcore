# Phase D — Batch Mass Update Execute Checklist

Blueprints are ready under `output/{SearchTemplateId}/`.  
**HTTP Execute requires authenticated PLM Import session** (`RequirePlmMigrationAdmin`).

## How to Execute

1. Open App → **PLM Data Import → PLM Search Import** (Mass Update attach mode).
2. For each file below: upload JSON → **Validate** → **Execute**.
3. Or call (while logged in):
   - `POST /appai/webapi/PlmMigration/ValidateSearchMassUpdateViewBlueprint`
   - `POST /appai/webapi/PlmMigration/ExecuteSearchMassUpdateViewConfig` body `{ "Blueprint": <json>, "SaasApplicationId": 5602 }`

## Order (TabField A, then RegularGrid B2)

| # | File | Mode | Default MU? | Notes |
|--:|------|------|-------------|-------|
| 1 | `23702/3_PlmSearch_MassUpdateView_8.json` | A | no | Style |
| 2 | `28902/3_PlmSearch_MassUpdateView_9.json` | A | **yes** | Fabric |
| 3 | `23902/3_PlmSearch_MassUpdateView_18.json` | A | **yes** | Trim |
| 4 | `24102/3_PlmSearch_MassUpdateView_20.json` | A | **yes** | Label |
| 5 | `24002/3_PlmSearch_MassUpdateView_22.json` | A | **yes** | Packaging |
| 6 | `25402/3_PlmSearch_MassUpdateView_49.json` | A | **yes** | Publish ERP (Style unit only) |
| 7 | `23702/3_PlmSearch_MassUpdateView_114.json` | B2 | no | Delete Colors ListEdit |
| 8 | `23702/3_PlmSearch_MassUpdateView_115.json` | B2 | no | Trim BOM ListEdit |
| 9 | `30223/3_PlmSearch_MassUpdateView_112.json` | B2 | no | Robert's Board |
| 10 | `30223/3_PlmSearch_MassUpdateView_116.json` | B2 | no | Robert's Board Price |

## Skip / verify-only

| Item | Status |
|------|--------|
| MU **111** Trim Tracking | **Already in APP** View #2105 / ListEdit #2284 — do not re-create |
| MU **48**, **79** | DynamicMatrix — skipped (`25402/README_MassUpdate_Matrix_SKIP.md`) |

## Post-execute verify SQL (TenantDB_PLM27)

```sql
SELECT sv.SearchViewID, sv.Name, sv.IsMassUpdateView, sv.UpdateTransctionID, s.IntegrationId AS SearchIntegrationId
FROM dbo.AppSearchView sv
JOIN dbo.AppSearch s ON s.SearchID = (
  SELECT TOP 1 s2.SearchID FROM dbo.AppSearch s2
  WHERE s2.SearchViewID = sv.SearchViewID OR EXISTS (
    SELECT 1 FROM dbo.AppSearchView x WHERE x.SearchViewID = sv.SearchViewID
  )
)
WHERE ISNULL(sv.IsMassUpdateView,0)=1
ORDER BY sv.SearchViewID;

-- Better: find MU views by name / Integration pattern if AppSearchView has IntegrationId
SELECT SearchViewID, Name, IsMassUpdateView, UpdateTransctionID
FROM dbo.AppSearchView WHERE ISNULL(IsMassUpdateView,0)=1 ORDER BY SearchViewID;

SELECT TransactionID, IntegrationId, TransactionName
FROM dbo.AppTransaction
WHERE IntegrationId LIKE N'ListEdit_MU%'
ORDER BY IntegrationId;

-- Display default View must be unchanged:
SELECT SearchID, IntegrationId, SearchViewID FROM dbo.AppSearch
WHERE IntegrationId IN (
  N'Search_1aStyleSearch',N'Search_23902',N'Search_24002',N'Search_24102',
  N'Search_25402',N'Search_28902',N'Search_30223');
```

Expected after full Execute: **~10 new MU views** (+ existing 111) and ListEdits `ListEdit_MU114_*`, `MU115_*`, `MU112_*`, `MU116_*` (plus existing `MU111_*`).

## Verify snapshot (2026-07-13, pre-Execute)

```
AppSearchView IsMassUpdateView=1 → only #2105 Trim Tracking MU (MU111)
ListEdit_MU% → only ListEdit_MU111_TrimTracking (#2284)
AppSearch display SearchViewID unchanged for the 7 in-scope searches
```

**Status:** Blueprints + checklist ready. Execute requires logged-in SaasCompanyAdmin/SysAdmin (`plm27admin` or equivalent) via PLM Search Import UI or authenticated API. Agent HTTP call returns Unauthorized without session — run steps 1–10 above after login, then re-run verify SQL.
