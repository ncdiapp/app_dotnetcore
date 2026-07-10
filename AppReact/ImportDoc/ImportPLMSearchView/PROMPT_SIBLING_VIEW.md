# PLM Search — Sibling View Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportPLMSearchView/`  
> **Purpose:** Add another PLM `ReferenceView` to an APP Search that was **already** imported via [`PROMPT.md`](PROMPT.md).  
> **Does NOT replace** the main first-import prompt. Do **not** edit `PROMPT.md` while running this file.  
> **Architecture:** [`source/MULTI_VIEW_COVERAGE.md`](source/MULTI_VIEW_COVERAGE.md)

---

## User input (required)

```text
1. PLM connection string
2. APP tenant connection string
3. PLM SearchTemplateId — already imported once (e.g. 1)
4. PLM ReferenceViewId — the ADDITIONAL view to import (e.g. 2)
```

Optional:

| Parameter | Default |
|-----------|---------|
| APP `Search.IntegrationId` | Resolve from prior import / naming convention (e.g. `Search_Fabric`) |
| `@TablePrefix` | `Plm_` |

### Gate 0 — missing input → ask only

If the user only `@` this file without **all four** required items → **STOP** and ask. Do not probe.

### Example session

```text
按 AppReact/ImportDoc/ImportPLMSearchView/PROMPT_SIBLING_VIEW.md 执行。

PLM connection string: ...
APP tenant connection string: ...
PLM SearchTemplateId: 1
PLM ReferenceViewId: 2
APP Search IntegrationId: Search_Fabric   (optional)
```

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Prerequisite** | Main [`PROMPT.md`](PROMPT.md) import for this `SearchTemplateId` must already exist in APP. |
| **No server code** | Deliverables are SQL + JSON under this folder only (same as main Prompt). |
| **FieldMapping is truth** | Resolve columns only via `{prefix}FieldMapping`. |
| **Option A join limit** | Enriching the existing DataSet may **only** add SELECT columns and/or **first-level 1:1** `LEFT OUTER JOIN` tables (same `ReferenceId` / header key). **Forbidden:** 1:N grid tables or any join that multiplies row count. |
| **Cross-grain → B** | If the new View needs header×grid row multiplication → recommend **Option B** (new DataSet + new Search). |
| **Option B IntegrationId** | New Search: `Search_{Name}_V{ViewId}`. New View: `Search_{Name}_V{ViewId}_View`. |
| **Do not change default View** | Option A adds a **sibling** `AppSearchView` on the same DataSet; keep the Search’s existing default `SearchViewId` unless the user explicitly asks to switch default. |

---

## Flow

```text
Gate 0 → APP probe (Search exists? View already imported?)
      → PLM probe (View columns) + FieldMapping
      → Analyze same-DataSet coverage
      → STOP: report + user chooses A / B / C
      → Phase B blueprint(s)
      → Phase D (SearchImportStep / BL APIs)
```

---

## Phase A1 — APP state probe

Locate APP Search:

1. If user gave `IntegrationId` → `AppSearch` by that id.  
2. Else try convention from prior README / `Search_{sanitizedName}`.  
3. If not found → **STOP**: run main `PROMPT.md` first for this `SearchTemplateId`.

Load:

- `SearchID`, `DataSetId`, current DataSet `QueryText` / tables / columns  
- Existing Views on that DataSet (`GetAllSearchViewForOneDataSet` equivalent SQL)  
- Whether View for this `ReferenceViewId` was already imported (match by name, README note, or `output/{searchTemplateId}/` sibling blueprint)

| Result | Action |
|--------|--------|
| Search missing | STOP — main Prompt first |
| Sibling View already present | STOP — nothing to do |
| Search OK, View not imported | Continue A2 |

Suggested SQL sketch (adapt to tenant):

```sql
-- Resolve search
SELECT SearchID, Name, IntegrationId, DataSetId, SearchViewId
FROM dbo.AppSearch
WHERE IntegrationId = @IntegrationId;

-- Sibling views on same DataSet
SELECT SearchViewID, Name, DataSetID, ViewType
FROM dbo.AppSearchView
WHERE DataSetID = @DataSetId
ORDER BY SearchViewID;
```

Also re-read `output/{SearchTemplateId}/README.md` and any `2_PlmSearch_SiblingView_*.json` if present.

---

## Phase A2 — PLM View + FieldMapping

Against PLM:

- `pdmReferenceView` for `@ReferenceViewId`  
- `pdmReferenceViewColumn` (+ SubItem / GridMeta enrichment)  
- Confirm View belongs to same RefTxType family as the original Search (warn if not)

Against APP:

- Resolve each column via FieldMapping (`_app_probe_fieldmapping.sql` pattern)  
- Classify each field vs **current** DataSet:

| Class | Meaning |
|-------|---------|
| `Covered` | Column already in DataSet SELECT |
| `AddColumn` | Table already joined; only add to SELECT |
| `AddOneToOneLeftJoin` | Need one more **1:1** sibling table via `LEFT OUTER JOIN` on header/`ReferenceId` |
| `RequiresOneToN` | Needs grid / row multiplication → **cannot** use Option A |
| `Unmapped` | No FieldMapping — list for ignore / stop / import Template first |

---

## Phase A3 — Analysis report (STOP — wait for user)

Present exactly this shape (fill numbers/lists):

```text
Sibling View import analysis
============================
PLM SearchTemplateId: {id}  ({name})
PLM ReferenceViewId:  {viewId}  ({viewName})
APP Search:           #{searchId}  IntegrationId={...}
APP DataSet:          #{dataSetId}
  Current tables:     [...]
  Current column count: N

Field coverage for View {viewId}:
  Covered:              n  → [optional short list]
  AddColumn:            n  → [table.column ...]
  AddOneToOneLeftJoin:  n  → [Plm_Xxx ON ReferenceId ...]
  RequiresOneToN:       n  → [MUST force Option B if > 0]
  Unmapped:             n  → [...]

Row-grain assessment: Header1to1 | HeaderTimesGrid | Custom
Recommended: Option A | Option B

Choose one:
[ ] A — Enrich existing DataSet (AddColumn + allowed 1:1 LEFT JOINs only),
        then create sibling AppSearchView on the SAME Search / DataSet
[ ] B — New DataSet + new Search IntegrationId = Search_{Name}_V{ViewId}
[ ] C — Cancel / fix FieldMapping / import missing Template first
```

Do **not** generate Phase B files until the user selects A, B, or C.

---

## Phase B — After user decision

### Option A — Enrich DataSet + sibling View

Outputs under `output/{SearchTemplateId}/`:

```text
2_PlmSearch_SiblingView_{ReferenceViewId}.json
README_Sibling_{ReferenceViewId}.md   (optional)
```

Blueprint contents (minimum):

- `mode`: `SiblingViewEnrichDataSet`  
- `source.searchTemplateId`, `source.referenceViewId`  
- `target.appSearchIntegrationId`, `target.appSearchId`, `target.appDataSetId`  
- `dataSetPatch`: `{ addColumns[], addLeftJoins[] }` — **only 1:1 LEFT JOINs**  
- `searchView`: name, suggested IntegrationId label (documentation), `viewType`, `fields[]`  
- `linkTargets`: copy from default View unless user overrides  

**Do not** rewrite criteria. **Do not** create a second Search.

Phase D (BL — implemented):

1. Open **PLM Data Import → PLM Search Import**  
2. Upload `2_PlmSearch_SiblingView_{ViewId}.json` (mode auto-detected)  
3. Validate Preview → Execute  

APIs: `LoadSearchSiblingViewBlueprint`, `ValidateSearchSiblingViewBlueprint`, `PreviewSearchSiblingViewConfig`, `ExecuteSearchSiblingViewConfig`.

Option B files use the **main** Search Import Execute path (`1_PlmSearch_ImportBlueprint_V{ViewId}.json`).

### Option B — New DataSet + new Search

- New Search IntegrationId: **`Search_{Name}_V{ViewId}`**  
- New View IntegrationId: **`Search_{Name}_V{ViewId}_View`**  
- Generate a **full** search blueprint (same schema as main Prompt’s `1_PlmSearch_ImportBlueprint.json`) into:

```text
output/{SearchTemplateId}/1_PlmSearch_ImportBlueprint_V{ViewId}.json
```

- Synthesize DataSet for this View’s grain (may include 1:N if that is the point of the View).  
- Criteria: optionally clone from the original APP Search field list (ask user).  
- Menu: do not auto-register unless user asks; prefer Linked Search / second menu item.

User then runs existing Phase D / `SearchImportStep` on that file (same as main import).

### Option C

STOP. List blockers only.

---

## Agent checklist

```text
[ ] Gate 0: PLM + APP + SearchTemplateId + ReferenceViewId
[ ] APP Search exists (else STOP → main PROMPT.md)
[ ] Sibling View not already imported
[ ] Probe PLM View columns + FieldMapping
[ ] Classify Covered / AddColumn / AddOneToOneLeftJoin / RequiresOneToN / Unmapped
[ ] If RequiresOneToN > 0 → recommend B
[ ] STOP with A/B/C report — wait
[ ] A → 2_PlmSearch_SiblingView_{ViewId}.json (+ DataSet patch, no new Search)
[ ] B → 1_PlmSearch_ImportBlueprint_V{ViewId}.json with Search_{Name}_V{ViewId}
[ ] No 1:N joins in Option A dataSetPatch
```

---

## Out of scope

- Editing main [`PROMPT.md`](PROMPT.md) behavior  
- Changing DwBlueprint / `includeInSearch`  
- Implementing C# Execute in this Prompt run (document gap; warn user)  
- Forcing one Search onto two DataSets  

---

## Related

- [`source/MULTI_VIEW_COVERAGE.md`](source/MULTI_VIEW_COVERAGE.md)  
- [`PROMPT.md`](PROMPT.md) — first import  
- Probe helpers: `source/_plm_probe_search.sql`, `source/_app_probe_fieldmapping.sql`, `source/_app_probe_search_context.sql`
