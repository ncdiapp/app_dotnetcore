# SearchTemplate 28902 — Mass Update View 9

**Phase B generated:** 2026-07-13  
**Mode:** A — `MassUpdateViewAttach` / `SingleTableUpdate`  
**PLM:** `plm_live_20260602` · MU `10- Fabric MU` (UpdateType=TabField)  
**APP:** `TenantDB_PLM27` · Search `Search_28902` (#18) · DataSet #2091

## Selections

| Item | Choice |
|------|--------|
| Option | **A** — SingleTableAttach |
| Update Transaction | `Tab_4258` (#2266) Fabric Header |
| Update Unit | #85 `Plm_Fabric_Header` (PK `ReferenceId` #2723) |
| PLM MainTabId | 3998 (no APP Tab) → use Tab_4258 by FieldMapping |
| Default MU | `setAsDefaultMassUpdateView=true` |
| LinkTargets | copy from display View #2085 |

## Coverage

- Mapped to txn field (incl. PK): 11 updatable Header fields
- Display-only (Fabric_Info / readonly): Sketch, Season, Collection, Group, Print/Solid, Article #, Description
- DataSet AddColumn: 8 (no new joins)
- Unmapped omitted: Supplier (56), Supplier Number (3789), Country Of Origin (103), Created Date (181)

## Deliverable

- `3_PlmSearch_MassUpdateView_9.json`

## Phase D

1. Open **PLM Data Import → PLM Search Import**
2. Upload `3_PlmSearch_MassUpdateView_9.json` (mode `MassUpdateViewAttach`)
3. Validate Preview → Execute
4. Confirm display default View (#2085) unchanged; MU View created with `IsMassUpdateView=true`
