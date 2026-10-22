# AI Agent Result and Doc Export UI Layout Redesign

**Canonical design for Result panel chrome, Chat visual standards vs MCP Client, and AI polish of TemplateContent + field structure.**  
Date: 2026-10-22.

This document redesigns how App Agent Chat presents **Result** and **document export** so the product approaches the latest MCP Client UI, while keeping program-side GENERATE (Scriban / React hosts) for speed and token cost.

Merge engines, file formats, limits, and `data-role` Word rules remain in [`AI-Agent-Ui-Render-And-Document-Export.md`](AI-Agent-Ui-Render-And-Document-Export.md). Left-chat chrome details that are not superseded here remain in [`AI-Agent-Chat-UI.md`](AI-Agent-Chat-UI.md).

---

## Relationship to other design docs

| Document | Role after this redesign |
|----------|--------------------------|
| **This file** | Visual/layout standards (Chat + Result), AI polish pipeline, Result React chrome (Filter / Print / Export), field/column selection rules |
| [`AI-Agent-Ui-Render-And-Document-Export.md`](AI-Agent-Ui-Render-And-Document-Export.md) | Still canonical for Scriban merge, Playwright PDF, OpenXML Word, PPT shots, cache limits, tool names. **Partially superseded** for: “TemplateContent never enters LLM”, model-must-not-touch-templates, Result hosting chrome, card “show all fields” default |
| [`AI-Agent-Chat-UI.md`](AI-Agent-Chat-UI.md) | Composer/transcript motion basics. **Defers** MCP-aligned visual target and Result chrome to this file |

---

## Purpose and visual references

### Goal

Make App Agent Chat **look and behave** close to the MCP Client reference:

1. Cleaner left chat (spacing, type, chips, fewer in-thread export dumps).
2. Right Result as an **Interactive** gallery/grid with React toolbar: filters, Print, Export.
3. Cards show a **primary image** and a **small set of primary fields** with role-based typography — not every column as plain text.
4. Print / PDF uses a polished **none-interactive** template + GENERATE over the full dataset.
5. Keep our advantage: tools loop N rows in C#; the LLM does **not** paint 1000×500 HTML.

### Reference figures (conversation assets)

| Fig | Asset (approx.) | What to take from it |
|-----|-----------------|----------------------|
| 1 | MCP Client UI — chat + Card View Result | Left chat density; right Result filters + Print/Download/Copy; hero image + typed fields |
| 2 | Browser print preview — PLM Colorways Gallery | None-interactive 4×N gallery; page fit; ID/title/status hierarchy |
| 3 | Slack — MCP line-sheet PDF card | Earlier MCP demand: image + ID + name + status/date |
| 4 | Product demo — Blu AI Assist timeline grid | Dense professional grid; search + risk filter in Result chrome |
| 5 | App Agent Chat — stiff Card View | Current gap: all fields as text; image URLs as strings; exports in chat |

Paths under the Cursor project assets folder (filenames may vary by capture):

- `assets/image-8aa16d08-19bb-4012-a1f1-5034642003ae.png` — Fig 1 MCP Client
- `assets/image-8878e07e-ffd7-4984-93b2-9a3feec8c8a6.png` — Fig 2 print preview
- `assets/image-b7644770-9e7b-4b0f-9ca1-d4189c08c25a.png` — Fig 3 Slack MCP
- `assets/image-2606a91b-3a38-4cc2-9a92-b4d9c9e7eb78.png` — Fig 4 product demo
- `assets/image-11855322-5bff-4023-a54c-9a8a6f90d76e.png` — Fig 5 App Chat today

---

## Iron rules (mandatory)

These rules override older Goal #3 in the export design doc (“`TemplateContent` never enters the LLM context”).

### R1 — LLM never builds the Result page from data

Templates exist so the model can **edit** them and return polished TemplateContent **together with** selected field / column / filter **structure** for our GENERATE tools.

The model must **never** combine TemplateContent with Result rows to produce HTML or pages.  
Feeding a template into the polish step is **not** permission to render data into that template inside the LLM.

### R2 — Sample only for polish

At most **10** sample rows (plus field names, labels, and light type hints) may enter the polish prompt. The full dataset must not.

### R3 — Full N-row merge stays in GENERATE

`UiTemplateMerge` (Scriban), Interactive React hosts, and export engines loop / bind the full `AgentDataAnalyzeCacheBL` rows. Same family as today.

### R4 — Session cache only (v1)

Polished TemplateContent and `AiPolishedRenderSpec` are **session-scoped**. They are **not** written back to company library `AppAgentLibraryTool.TemplateContent` in v1.

### R5 — Structure, not row payloads, in bindings

Bindings list **field names**, roles, and layout hints only. They must not embed result row JSON for the full set.

### Naming

Continue to use **none-interactive** / `NoneInteractive` and **interactive** / `Interactive`. Never use `static` in tool names or user-facing copy.

---

## Locked product decisions

| # | Decision |
|---|----------|
| 1 | AI polish output = **session-scoped cache only** |
| 2 | Filter / Print / Export live in **React Result chrome**, not inside TemplateContent HTML |
| 3 | This independent design document is the UI-layout source of truth for the redesign |
| 4 | Iron rules R1–R5 above |
| 5 | LLM emits **polished TemplateContent + field/column structure only**; C# GENERATE loops full result rows |

### Defaults

| Topic | Default |
|-------|---------|
| Polish sample size | **10** rows |
| Polish cache key | `chatSessionId` + `dataset_name` + `layout` + prompt-hash; invalidate when analysis cache for that dataset is replaced |
| Filter distinct values | Computed in C#/React from **full cache** after AI picks field names; if distinct count is huge, expose at most **50** values per filter field to the UI (LLM does not need the full distinct list) |
| “All columns” | When the user prompt or an explicit tool flag requires it, select **all** columns; otherwise select an important subset that fits one page / panel |

---

## MCP Client vs App Agent — gap analysis

### Left chat

| Area | MCP Client (target feel) | App today (`GenericAgentChat`, Chat UI design) | Required improvement |
|------|--------------------------|------------------------------------------------|----------------------|
| Density / whitespace | Clean transcript column; restrained bubbles | Busier: long export link lists, many suggestion pills | Tighten vertical rhythm; move exports to Result chrome; fewer competing CTAs in-thread |
| Typography | Clear title / body / meta hierarchy | Closer but uneven vs Result | Align type scale with MCP-like roles via `useTheme()` tokens |
| Artifact chip | Compact “Open in panel” / “Showing in panel” | `RenderChip` exists; captions often busier | Match MCP: title + one fact line + Open/Showing; accent when open |
| Tool chain | Collapsible “N tools used” | Partially present | Keep; de-emphasize large monospace dumps |
| Composer | Rounded; optional attach/mic in MCP | Send/Stop + focus shimmer; no attach | Keep our scope; polish radius and placeholder (“Ask {skill}…” / “Ask Blue…”) |
| Colors | Blue accent, soft greys | Theme tokens | Match **roles** (accent ID, status success/danger) — do not hardcode MCP hex |

### Right Result

| Area | MCP Client | App today | Required improvement |
|------|------------|-----------|----------------------|
| Card body | Hero image + few primary fields; typed styles | All fields as plain text; image URLs as strings | AI field pick + role-styled TemplateContent; render images as `<img>` |
| Toolbar | Print / Save PDF, Download, Copy HTML | Export mostly via chat tool links | React chrome: Print primary; Export menu → existing `export_*` |
| Filters | Search + multi-select status pills; “N of M” | Sparse (e.g. dashboard dim filter only) | AI-chosen filter fields → React controls over cached rows |
| Print | None-interactive gallery page | PDF via Playwright merge | Print uses **session polished none-interactive** template + GENERATE |

### Approach comparison (why hybrid)

| | MCP Client | App Agent (today) | App Agent (this redesign) |
|--|------------|-------------------|---------------------------|
| Who paints UI | LLM generates large HTML from template + data | C# GENERATE from library TemplateContent + all fields | LLM **polishes** template + **selects structure**; C# GENERATE binds all rows |
| Token / time | High | Low | Moderate polish once; GENERATE stays cheap |
| Visual quality | High | Stiff / cluttered | Target MCP-like without LLM page builds |
| Print | From generated artifact | From none-interactive merge | From polished none-interactive + GENERATE |

---

## Target architecture

```
Library TemplateContent (interactive + none-interactive)
+ ≤10 sample rows + field schema
+ user prompt / conversation context
+ render tool I/O contracts
        │
        ▼
   AI polish step  ──►  FORBIDDEN: emit full-page HTML for N rows
        │
        ▼
 Session cache:
   • polished TemplateContent (interactive + none-interactive)
   • AiPolishedRenderSpec (bindings, filters, columns, layoutHints)
        │
        ├──────────────────────────────┐
        ▼                              ▼
 render_interactive              render_none_interactive
 + React host                    Scriban merge (full N rows)
        │                              │
        ▼                              ▼
 Result body                     Print / export_pdf / docx / pptx / html
        ▲
        │
 React Result chrome
 (Filter, Print, Export)  ← filterFields from AiPolishedRenderSpec
```

**Hard boundary:** full `AgentDataAnalyzeCache` rows flow only into GENERATE / export. They do not flow into the polish LLM call except as the capped sample.

---

## AiPolishedRenderSpec (structure, not data)

Stable session / `ui_render` payload. Name: **`AiPolishedRenderSpec`**.

Bindings list **field names only** — never full result row arrays.

```json
{
  "version": 1,
  "layout": "cards",
  "title": "PLM Style Colorways Gallery",
  "templateContentInteractive": { "cards": "<article data-role=\"card\">…</article>" },
  "templateContentNoneInteractive": { "cards": "<article data-role=\"card\">…</article>" },
  "bindings": {
    "primaryImageField": "Image1Link",
    "fields": [
      { "name": "Product_Code", "role": "idLink", "typographyHint": "accent-semibold" },
      { "name": "Description", "role": "title", "typographyHint": "body" },
      { "name": "ColorName", "role": "colorSwatch", "typographyHint": "meta" },
      { "name": "StyleStatus", "role": "statusPill", "typographyHint": "status" },
      { "name": "Season", "role": "meta", "typographyHint": "muted-small" }
    ],
    "columns": [],
    "filterFields": [
      { "name": "StyleStatus", "control": "multiSelect" },
      { "name": "_search", "control": "search", "searchFields": ["Product_Code", "Description"] }
    ],
    "chart": null,
    "kpi": null
  },
  "layoutHints": {
    "cardsPerRow": 4,
    "cardRows": 4,
    "statusColorMap": {
      "Approved": "success",
      "Dropped": "danger",
      "Hold": "muted",
      "In Development": "info"
    }
  },
  "selectAllColumns": false
}
```

### Field roles (cards)

| Role | Typical render |
|------|----------------|
| `primaryImage` / `primaryImageField` | Large top image; never raw URL text |
| `idLink` | Accent, semibold; primary identifier |
| `title` | Body weight; product/style name |
| `colorSwatch` | Small circle + label when value looks like a color |
| `statusPill` | Pill with success/danger/muted/info from `statusColorMap` |
| `meta` | Muted small footer (season, category, date) |
| `body` | Default text |

Target **4–7** display fields besides the image. Do not dump dozens of columns onto a card.

### Grid / table

- `bindings.columns`: ordered list of field names.
- Default: important columns that fit one page / panel width.
- If user asks for all columns, or `selectAllColumns: true`: include all (subject to existing column cap in the export design doc).

### Filters

- AI chooses which fields become filter controls (`search`, `multiSelect`, optionally `singleSelect`).
- Distinct values for pills are derived from the **full cache** in the host after polish — not invented by the LLM as a closed world unless sample-only is enough for tiny sets.
- Cap distinct values shown at **50** per field.

### Chart / KPI

Align measure/dimension picks with existing `specJson` shapes from the export/render design. Polish may refine which fields feed charts/KPIs; GENERATE still binds data.

### Polish trigger

Run polish when layout needs visual selection: `cards`, `table`/`grid`, `kpi`, `chart`, `dashboard`.

Cheap-path / skip polish when:

- Layout is a trivial `form` with an already-small field set, and
- No user request implies gallery/grid curation,

or when a valid session polish cache entry already matches the key.

---

## Result React chrome (Interactive default)

Hosted beside chat (Result / Files / Tool Activity tabs as today). Chrome is **React**, not TemplateContent.

### Top bar

- Title from polish / render title
- Count: filtered vs total (e.g. `57 of 57 colorways`)
- Maximize / close (existing behavior)

### Filter row

- Search input when `filterFields` includes `search`
- Multi-select pills / toggles for categorical fields (MCP “All / Approved / Dropped / …”)
- Filtering is **client-side** (or host-side) over cached rows; regenerating HTML for every keystroke is not required if the Interactive host binds a row collection

### Actions

| Control | Behavior |
|---------|----------|
| **Print** (primary) | Uses session **polished none-interactive** TemplateContent + GENERATE / `export_pdf` over full (or currently intended export) cache. Does **not** screenshot Interactive chrome or filter UI |
| **Export** (secondary menu) | PDF, Word, PowerPoint, HTML, Excel — same engines as [`AgentDocumentPlugin`](../../APP.BL/AIAgent/GenericAgent/Plugins/AgentDocumentPlugin.cs) |
| Optional Copy HTML / Download gallery | Same merge source as HTML export when product wants MCP parity |

### Body

GENERATE output for the layout (cards, grid, chart, KPI, dashboard stack). Interactive TemplateContent supplies control LAYOUTs; React hosts mount grids/charts as already designed.

### Print vs Interactive

| Mode | Template source | Filters in page |
|------|-----------------|-----------------|
| Interactive Result | Polished interactive TemplateContent + React chrome filters | Yes (React) |
| Print / PDF / most exports | Polished **none-interactive** TemplateContent + Scriban over rows | No (static gallery/table) |

---

## Layout-specific rules

### Card view

- Exactly one **primary image field** when an image-like field exists (name/url/content-type heuristics + sample values).
- Primary fields only; role-based CSS in TemplateContent; keep `data-role` for editable Word.
- Interactive: React filters + Print/Export chrome.
- None-interactive: print-ready grid (e.g. 4×4 per page via `cardsPerRow` / `cardRows`).

### Grid / table

- AI selects columns; default fit-one-page; all columns when demanded.
- Toolbar same chrome pattern (search may map to quick filter; risk/status filters as in demo fig 4).

### KPI / dashboard

- Each block (card list, grid, chart, small KPI cards) uses the same polish → GENERATE logic.
- Blocks are composed into one dashboard Result page.
- Outer page uses the same Print / Export chrome.
- Platform collapse/expand for stacked controls stays **outside** TemplateContent (existing rule).

### Form

- Prefer existing form TemplateContent; polish optional when field count is large (show primary fields first).

---

## Export

Engines **unchanged** in responsibility:

- Scriban merge, agent-ui CSS inline, Playwright PDF, OpenXML Word from `data-role`, PPT Playwright shots, Excel/CSV fixed tables — see export design doc.

**Change of template source:**

1. If session has polished none-interactive TemplateContent + bindings for that `dataset_name` + `layout`, merge uses those.
2. Else fall back to library `TemplateContent` / code defaults (current behavior).

Export may be started from:

- Result React chrome, or
- Chat `export_*` tool calls,

both must use the same merge preference order.

In-thread lists of export file URLs should be reduced over time; prefer “exported — open Files / use Export in Result”.

---

## Chat UI redesign checklist (left pane)

Implement in a later coding phase; standards:

1. Reduce in-thread export URL clutter; prefer Result Export / Files tab.
2. Chip parity with MCP ArtifactCard (title, one fact, Open/Showing).
3. Tighten header / transcript / composer spacing to MCP density.
4. Suggestion / follow-up buttons secondary — do not dominate the turn.
5. Tool chain collapsed by default after completion; keep “N tools used”.
6. Theme-token colors for accent IDs and status in Result templates (not hardcoded MCP palette).
7. Placeholder copy closer to MCP (“Ask …”) without requiring attach/mic.

Code anchors today: [`GenericAgentChat.tsx`](../../AppReact/src/components/aiskill/GenericAgentChat.tsx), [`DataRenderPanel.tsx`](../../AppReact/src/components/aiskill/dataRender/DataRenderPanel.tsx), [`UiTemplateMerge.cs`](../../APP.BL/AIAgent/GenericAgent/Documents/UiTemplateMerge.cs).

---

## What the model may and must not do (updated)

| Actor | May do | Must not do |
|-------|--------|-------------|
| Model (polish) | Edit TemplateContent for the layout; emit `AiPolishedRenderSpec` field/column/filter structure; set layout hints; honor “all columns” from user intent | Merge Result rows into HTML; emit N-card pages; write library templates; send full cache into context |
| Model (render/export tools) | `dataset_name`, `layout`, `title`, limits, `cardsPerRow` / `cardRows`, `actionsJson`, `specJson`, export paths; reference that polish cache should be used | Paste fonts/colors as ad-hoc page CSS outside TemplateContent polish; invent full HTML artifacts like MCP |
| C# GENERATE | Load session polish or library template; bind full cache; encode; sanitize; emit HTML/files | Ask the model to fill row cells into the page |
| React Result chrome | Filters, Print, Export, count, panel chrome | Embed Print/Export/Filter **inside** TemplateContent HTML as the primary design |

---

## Non-goals (this redesign)

- Writing polished templates back to the company library (v1)
- LLM generating full HTML for all result rows
- Primary Filter / Print / Export UI embedded inside iframe TemplateContent
- Changing Excel/CSV fixed-table approach
- Copying MCP hardcoded hex / Inter-only / Tailwind CDN rules into the SPA

---

## Implementation roadmap

| Phase | Work | Status (2026-10-22) |
|-------|------|---------------------|
| 1 | This design document + cross-links on existing docs | Done |
| 2 | Session polish cache + `AiPolishedRenderSpec` + render tool inputs / wire-up | Done — `AgentAiPolishCacheBL`, `prepare_ui_render_polish` / `cache_ui_render_polish`, heuristic polish, `render_*` params, migration `V064` |
| 3 | React Result chrome (Filter, Print, Export) | Done — `ResultChrome.tsx`, `ExportResult` API |
| 4 | Default TemplateContent role styles for cards (image + typed fields) | Done — `UiDefaultViewTemplates.CardsNoneInteractive`, `agent-cards.css`, React `DataRenderCard` roles |
| 5 | Chat chrome cleanup pass (left pane checklist) | Done — chip caption, Ask placeholder, fewer export links, quieter follow-ups |
| 6 | KPI / dashboard composition polish | Done — polish on kpi/dashboard SpecJson; dashboard grid uses polish columns |

Apply DB migration `V064__AiRenderPolish_xhu.sql` so polish tools are registered. Unit tests: `AiPolishedRenderSpecTests` + Documents suite.

---

## Supersession summary

| Old statement (export design) | New statement |
|------------------------------|---------------|
| `TemplateContent` never enters the LLM context | TemplateContent **may** enter a **dedicated polish** step with ≤10 sample rows |
| Model must not paste template bodies | Model **may** return polished TemplateContent **only** as polish output for tools — never as a data-bound page |
| Cards show available fields from merge defaults | Cards show **AI-selected** primary fields + image; full field dump is wrong by default |
| Export links primarily from chat tools | Result React chrome is the primary Print/Export surface |

Merge/export **engines** and numeric **limits** in [`AI-Agent-Ui-Render-And-Document-Export.md`](AI-Agent-Ui-Render-And-Document-Export.md) remain in force unless a future change explicitly updates them.

---

## Revision: Result visual fidelity & print/export (2026-10-22b)

Product decisions after live Result/Print/Export review vs MCP Client:

| Decision | Choice |
|----------|--------|
| Interactive card host | **TemplateContent / `cardsHtml` GENERATE only** — do **not** use React `DataRenderCard` as the primary Result path (that path is deprecated for Agent Result) |
| Image URL remap (PLM `srv-mite/.../requestsketchimage.aspx` → `/api/resources/...`) | **Deferred** — skip for this revision; track separately |
| Follow-up chips | Only after an **analysis** path this turn |
| Print UX | Hidden iframe → system print dialog; **no** visible blank tab |
| Page grid (2×2 / 4×4) | Must apply to Print and Export PDF/DOCX/PPT (and print-oriented HTML) |

### Findings (root causes)

1. **Card look vs MCP** — Library TemplateContent may still be stub/`code_default`; role CSS exists in code defaults but Result twin often lacks MCP hierarchy. Fix by upgrading **both** interactive and none-interactive `cards` TemplateContent shells + `agent-cards.css`, still GENERATE via Scriban (not React cards).
2. **Follow-ups on show/search** — `FollowupsPromptHint` injects whenever `data_analyze` is subscribed and explicitly allows followups after `render_*`. UI shows any ```followups``` fence. Fix prompt + UI gate (no chips unless this turn called `data_analyze` or confirmed KPI path).
3. **Print temp tab** — `ResultChrome` used `window.open` + `document.write` + `print()`. Fix: off-screen iframe.
4. **2×2 / 4×4 ignored** — (a) `ExportResult` passed `cardsPerRow=0, cardRows=0`; (b) heuristic polish defaulted LayoutHints to 4×4 and **overrode** tool args; (c) `MergeCards` did not use `WriteFittedCards` for print when TemplateContent present / code_default called `WriteCards(..., forPrint: false)`; (d) Print read screen twin `htmlPath`, not fitted paged HTML.
5. **Images** — Raw `Image1Link` from PLM views is often legacy `http://srv-mite/ootb_blank_plms/requestsketchimage.aspx?SketchID=` (data from SQL, not invented by polish). Working App URLs are `/api/resources/Company_{id}/Image/regular/{guid}` from Search/AppFile mapping. **Out of scope this revision.**

### Implementation checklist (this revision)

1. Page grid: tool/user `cardsPerRow`/`cardRows`/`pageGrid` **win over** polish defaults; pass them through Result chrome → `ExportResult` → document plugin; Print regenerates fitted HTML (or export twin) with same grid.
2. `MergeCards` / export: when `forPrint`, use fitted page breaks (R×C per page) even with TemplateContent (page-break wrapper or fitted path + role styles).
3. Print: hidden iframe; HTML = fitted none-interactive merge (same as PDF source), not unfitted Result twin.
4. Follow-ups: tighten engine hint; UI suppress unless analysis tools this turn.
5. TemplateContent cards shell + CSS closer to MCP (hero/id/status/meta) via GENERATE — **no** React card list as Result body.
6. Image remap: deferred.

### Acceptance tests

- [ ] User asks card view **2×2** (2 rows × 2 columns per page): Print preview and Export PDF/DOCX/PPT each page has **exactly 4** cards (2×2). **4×4** → 16 cards per page.
- [ ] Print: no visible blank browser tab; system print dialog appears.
- [ ] Show/search/card (no `data_analyze` this turn): **no** FollowupChips.
- [ ] Analysis turn (`data_analyze` or KPI confirm): followups may appear.
- [ ] Interactive Result cards still from **TemplateContent/`cardsHtml`**, not `DataRenderCard` list as primary host.
- [ ] Image remap not required to pass this revision (may still break on legacy PLM URLs).

### Implemented (2026-10-22b code)

- `ExportResult` + Result chrome pass `cardsPerRow` / `cardRows` / `pageGrid`; document plugin always applies grid for cards (incl. HTML).
- Print: `ExportResult` HTML → hidden iframe print (no `window.open` tab); screen twin `HtmlPath` not used for print paging.
- Follow-ups: engine hint + UI gate on `data_analyze` / KPI confirm this turn.
- Result cards host: TemplateContent/`cardsHtml` only (React `DataRenderCard` path removed for Agent Result).
- Image remap: deferred.
