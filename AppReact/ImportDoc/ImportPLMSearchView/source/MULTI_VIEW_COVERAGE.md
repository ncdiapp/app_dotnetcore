# APP coverage of PLM「一 Search 多 View」

> **Does not replace** the main first-import prompt.  
> First import: [`../PROMPT.md`](../PROMPT.md)  
> Add another view to an already-imported Search: [`../PROMPT_SIBLING_VIEW.md`](../PROMPT_SIBLING_VIEW.md)

## Model difference

| | PLM | APP |
|--|-----|-----|
| Criteria | On Search | `AppSearch` + `AppSearchField` bound to `DataSetId` |
| Switch View | Any View of same RefTxType | View columns must come from **that** DataSet |
| Row grain | Can change by switching View (1 row ↔ N rows) | **Row grain = DataSet SQL** — cannot change by View alone |

APP already supports multiple Views on one DataSet (sibling Views; Search UI View dropdown).

## Rule (one sentence)

**Same row grain → 1 DataSet + 1 Search + N Views. Different row grain → N DataSets + N Searches (`Search_{Name}_V{ViewId}`).**

### Same grain (Grid vs Image Card)

1. Run main [`../PROMPT.md`](../PROMPT.md) once (default View).  
2. Run [`../PROMPT_SIBLING_VIEW.md`](../PROMPT_SIBLING_VIEW.md) for each extra View.  
3. Agent analyzes whether the existing DataSet can cover the new View.  
4. User chooses:
   - **A** — Enrich DataSet (add columns and/or **1:1** `LEFT OUTER JOIN` only), then add sibling View on the **same** Search  
   - **B** — New DataSet + new Search (`Search_{Name}_V{ViewId}`)

**Hard limit on A:** never add 1:N grid / row-multiplying joins to “force” a sibling onto the first DataSet.

### Cross grain (Fabric list vs Fabric×Color)

Must be Option **B** (or a fresh main Prompt run with a different JOIN / `accept-1N`). Do not enrich the header DataSet with Color grid joins.

## Fabric checklist

| Step | Object | How |
|------|--------|-----|
| 1 | DataSet_Fabric + Search_Fabric + View (default) | Main `../PROMPT.md` |
| 2 | View Image Card (same grain) | `../PROMPT_SIBLING_VIEW.md` → usually Option A |
| 3 | DataSet + Search for Fabric×Color | Sibling Option B, or main Prompt with color JOIN |
| 4 | LinkTargets | Same Fabric Transaction Group; Color suite may prefer Colorway tab |

## Anti-patterns

- One UNION / fat DataSet mixing header grain and header×grid grain  
- One APP Search bound to two DataSets  
- Duplicating a full Search for every same-grain layout View  

## Related

- Sibling import agent prompt: [`../PROMPT_SIBLING_VIEW.md`](../PROMPT_SIBLING_VIEW.md)  
- Main import: [`../PROMPT.md`](../PROMPT.md)  
- Sibling Views by DataSet: `AppDataSetBL.GetAllSearchViewForOneDataSet`  
- Search UI switch: `AppReact/src/components/search/AppSearch.tsx`
