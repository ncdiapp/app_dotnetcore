# SearchTemplate 28902 — 1b - Fabric Search

**Phase B generated:** 2026-07-10  
**PLM:** `plm_live_20260602`  
**APP tenant:** `TenantDB_PLM27`

## User selections (Phase A confirm)

| Item | Choice |
|------|--------|
| JOIN plan | **D** — `Plm_Fabric_Header` (primary) LEFT JOIN `Plm_ReferenceBasicInfo` + `Plm_Fabric_Info` + `Plm_Fabric_Cost` + `Plm_Attributes` |
| Grid strategy | exclude |
| Reference scope | FabricHeaderOnly |
| Missing fields | ignore (COO 103, Supplier 56) |
| Link target | Transaction Group **3** (`02 - Fabrics`) + `Tab_4258` |
| IntegrationId | `Search_28902` / `Search_28902_View` (PLM SearchTemplateId, not name text) |
| Main menu | yes |

## Deliverable

- `1_PlmSearch_ImportBlueprint.json` — upload to future **Search Import** wizard (Phase D `ExecuteSearchBlueprintConfig`)

## Coverage summary

- Criteria: 8/9 mapped (COO ignored)
- View: 19/20 mapped (Supplier ignored; 4 hidden PLM columns skipped)

## Column aliases (name collisions)

| Alias | Source | Why |
|-------|--------|-----|
| `Fabric_Mill_6790` | `Plm_Fabric_Info.Fabric_Mill` | Criteria entity Mill; Header also has text `Fabric_Mill` (view 3133) |
| `Total_Composition_7089` | `Plm_Attributes.Total_Composition` | Final Composition; Header has Original Composition as `Total_Composition` (4938) |

## Item Type default

PLM criteria Item Type: `operationId: 0`, `defaultValue: "3"` (read-only in PLM).
