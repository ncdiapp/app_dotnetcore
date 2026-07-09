# SearchTemplate 23702 — 1a - Style Search

**Phase B generated:** 2026-07-09  
**PLM:** `plm_live_20260602`  
**APP tenant:** `TenantDB_PLM27`

## User selections (Phase A confirm)

| Item | Choice |
|------|--------|
| JOIN plan | **B** — `Plm_Style_Header` (primary) LEFT JOIN `Plm_ReferenceBasicInfo` LEFT JOIN `Plm_Style_Summary` |
| Grid strategy | exclude |
| Reference scope | Style Header drives row set (no extra filter) |
| Missing view columns | ignore |
| Link target | **Transaction Group 1** (`01 - Apparel - 10 Colorways`) + primary tab `Tab_4251` |
| IntegrationId | `Search_1aStyleSearch` / `Search_1aStyleSearch_View` |
| Main menu | yes |

## Deliverable

- `1_PlmSearch_ImportBlueprint.json` — upload to future **Search Import** wizard (Phase D `ExecuteSearchBlueprintConfig`)

## Transaction group note

`AppFormLinkTarget.LinkTargetTransactionGroupId` is supported by the APP platform (opens full template group in FormMasterDetail).  
Current PLM migration BL does **not** yet implement Search Blueprint execute — JSON includes `linkTargetTransactionGroupId: 1` for BL to apply when built.

## Coverage summary

- Criteria: 15/16 mapped (Created By skipped)
- View: 17/24 mapped (7 ignored per user)
