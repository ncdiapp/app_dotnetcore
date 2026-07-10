# Sibling View 12 — 3a - Styles (Card)

**Option:** A (enrich DataSet + sibling View)  
**Generated:** 2026-07-10  
**Target Search:** `#17` `Search_1aStyleSearch` / DataSet `#2087`

## Decision

Same header grain as default Grid View. All 4 PLM Card columns already in DataSet SELECT → **no QueryText change**.

| PLM column | APP column | Class |
|------------|------------|-------|
| Sketch | `Sketch` | Covered |
| Article # | `Article` | Covered |
| Description | `Description_23` | Covered |
| Season | `Season_3` | Covered |

Plus APP-only `ReferenceId` (root / link target).

## Deliverable

- `2_PlmSearch_SiblingView_12.json`

## Phase D

1. Open **PLM Data Import → PLM Search Import**
2. Upload `2_PlmSearch_SiblingView_12.json` (mode auto-detected as sibling)
3. Validate Preview → Execute

Result: sibling `AppSearchView` **3a - Styles (Card)** with `ViewType=CardView` on the same Search; default View stays Grid.

## After import (optional)

Custom card layout: Search View Editor → select Card View → **Layout**.
