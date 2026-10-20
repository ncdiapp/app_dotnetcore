# Agent UI render and document export

**Canonical design, test standard, and implementation summary.**  
Date: 2026-10-20. Supersedes the former drafts `AI-Agent-Document-Export-Tools-V1.md`, `…-V2.md`, and `…-V3.md` (those files are removed). Chat chrome (header, composer, chips, motion) stays in [`AI-Agent-Chat-UI.md`](AI-Agent-Chat-UI.md); that file defers Result hosting to this document.

This document covers both:

1. **UI render** — Agent Chat Result panel shows C#-merged HTML in an **iframe** (`render_none_interactive` / `render_interactive`).
2. **Document export / read** — Same none-interactive templates drive HTML / PDF / Word / PowerPoint; Excel and CSV use a fixed style table.

The model does **not** send HTML, fonts, or colors, and does **not** write the Result page. C# fills templates from the session cache.

---

## Summary

| Concern | Decision |
|---------|----------|
| Who builds the page / file body | **C#** Scriban merge from `AppAgentLibraryTool.TemplateContent` |
| Result host | **iframe** (`srcDoc` / blob); CSS/JS isolated from the main app |
| Templates store | `TemplateContent` + `TemplateMetaJson` on **render** library tool rows (MCP-like `content`, program-filled) |
| Export templates | **Shared** with `render_none_interactive` — not on each `export_*` tool |
| Tool names | `render_none_interactive`, `render_interactive` — **never** use `static` |
| Modes | `NoneInteractive` \| `Interactive` |
| Layouts (shipped) | `form`, `table`, `cards` |
| Layouts (next) | `kpi`, `chart`, `dashboard` — MCP-like look in Result |
| Result CSS/JS | **Allowed inside the iframe** (Tailwind / Chart.js / other). Isolated from the host app. Prefer **vendored / prebuilt** assets, not runtime CDN. |
| Export charts | When needed, **rasterize chart(s) to PNG** and embed in DOC/PDF/PPT (and HTML export as `<img>`). Not required for Excel/CSV. |
| PDF | Playwright on merged HTML |
| Word | LibreOffice HTML → docx (chart images embedded) |
| PowerPoint | ShapeCrawler; styles from `data-role`; chart images as pictures |
| Excel / CSV | Fixed style table; ignore HTML templates; charts become numbers/tables only |
| Legacy | `data_render` alias one release, then delete; React `DataRenderPanel` removed after cutover |

```
dataset_name / form tree
        │
        ▼
 AgentDataAnalyzeCacheBL
        │
        ▼
 Template: render_none_interactive | render_interactive
   .TemplateContent[layout]
        │
        ▼
 C# merge (Scriban + encode + safety)
        │
        ├──► SSE ui_render → Result iframe
        └──► export_html / export_pdf / export_docx / export_pptx
              (always none-interactive template for that layout)
```

---

## Goals

1. Analysis view → render tool (`dataset_name`, `layout`, …) → C# merge → Result iframe.
2. Export → `export_*` with same cache + `layout` → **same none-interactive merge** → file matches Result as far as each engine allows.
3. Model never receives or emits large HTML/CSS/JS as tool args; `TemplateContent` never enters the LLM context.
4. Merged HTML can stand alone later (download / open outside the app).

### AI vs program

| Actor | May do | Must not do |
|-------|--------|-------------|
| Model | `dataset_name`, `layout`, `title`, `limit`, `cardsPerRow`, `cardRows`, `actionsJson`, export paths | Paste HTML, fonts, colors, template bodies |
| C# | Load template, bind cache, encode, sanitize, emit HTML / files | Ask the model to fill the template |
| React host | iframe, sandbox, postMessage, chips | Wijmo/AG Grid/Recharts inside Result |

### Naming rule (mandatory)

Do **not** use `static` in tool names, mode enums, or user-facing copy. Use **none-interactive** / `NoneInteractive` and **interactive** / `Interactive`.

---

## Limits

| What | Cap |
|------|-----|
| Session cache (`AgentDataAnalyzeCacheBL`) | 50000 data rows |
| Render / export | 50000 data rows |
| Columns | 50 |
| Rows returned to the model on read | 500 |
| Section depth drawn | 3 |
| PDF text returned to the model | ~12000 characters |
| One file | 20 MB |
| Inline HTML on SSE (suggested) | 512 KB; above → session file + `htmlPath` |

Over 50000 data rows: error, no file, no Result HTML. Cache keeps first 50000 and marks truncated. Restart drops process memory cache.

Playwright needs Chromium installed. LibreOffice (`DocumentExport:SofficePath`) for docx paths. Each `soffice` call uses its own temp profile; cleaned up on success or failure.

---

## Input, files, and cache

The model sends a form tree, `dataset_name`, or `sourcePath`. Prefer `dataset_name` so the file and Result get full cached rows, not a truncated paste.

Files: `FileRepository/Company_{id}/AgentOutput/{session}/`. Bare name → `output/` with `_yyyyMMdd_HHmmss` before extension. Reject `..`.

`dataset_name` loads `AgentDataAnalyzeCacheBL` for this chat; wins over `content`. Tool does not re-run the query.

`layout`: `form` (default for form trees), `table`, or `cards`. `cardsPerRow` 1–6 (default 1). `cardRows` default 3 (PDF/Word/PPT page grid). CSV is one flat row per record. Excel ignores `cardRows`.

### Form tree (layout `form`)

```json
{
  "title": "Sales Order",
  "fields": [
    { "label": "Order No", "value": "SO-1" },
    { "label": "Customer", "value": "Acme" }
  ],
  "sections": [
    {
      "title": "Lines",
      "tables": [
        { "columns": ["Item", "Qty", "Amount"], "rows": [["Widget", 2, 20]] }
      ]
    }
  ],
  "notes": "Ship this week."
}
```

`rows` may be objects. If `columns` is omitted, use the first object's keys. Cells: string, number, bool, ISO-8601 date, or null. Nesting: depth 1 title, depth 2 section, deeper as depth 3.

Export may pass `path` plus exactly one of `content` or `sourcePath` (`.json`, `.csv`, `.xlsx`, `.html` already in session).

### Row choice (every export / render that binds tabular data)

- `dataset_name` → full cache.
- `content` / `records` that look like a flat table or card list, match one cached dataset on key values, and have fewer rows → replace with that dataset’s full rows (at least half of passed keys hit).
- Two matching datasets and no `dataset_name` → error, no file / no panel.
- Multi-section form that is not a single table → written as given.
- Remembered Result / card panel is a display slice; does not override a larger matching cache. Explicit `limit` applies after cache choice.
- `rowCount` = rows written or shown.

### Preview

`preview` true on export: return merged HTML, do not write the office file. That HTML is what `export_pdf` prints (with `PrintCss`). It does not match LibreOffice Word, docx-to-pdf, or PowerPoint beyond template/`data-role` subset.

`export_html` / email body: tables and inline styles only — no `<style>` block, no flex/grid/float. Preview and Playwright PDF may add `PrintCss` on the same body; `PrintCss` is not part of the email body.

---

## Templates on the library tool row

```sql
ALTER TABLE dbo.AppAgentLibraryTool ADD
  TemplateContent   NVARCHAR(MAX) NULL,
  TemplateMetaJson  NVARCHAR(MAX) NULL;
```

| Column | Use |
|--------|-----|
| `TemplateContent` | JSON map layout → Scriban HTML. Keys: `form`, `table`, `cards`. Only on render tools. |
| `TemplateMetaJson` | `{ "mode": "NoneInteractive" \| "Interactive", "printCss": "...", "sampleJson": "..." }` |
| `ToolConfig` | BuiltIn dispatch only (`TypeName`, `MethodName`). No HTML. |
| `ParameterSchemaJson` | LLM args only. No template bodies. |

```json
{
  "form": "<!DOCTYPE html>… {{ title }} …",
  "table": "<!DOCTYPE html>…",
  "cards": "<!DOCTYPE html>…"
}
```

Rules:

- `render_none_interactive` and `render_interactive` must have `TemplateContent` after seed.
- Every `export_*` / `read_*` / `convert_*` row: both columns **NULL**.
- Runtime prefers DB; empty DB → code default for that layout (deploy safety).
- Tenant may edit the two columns (company customization). Visual template editor / copy-Restore table: **later**, out of this build.
- Missing layout key → `{ ok: false, error }` — no file, no panel.

### Scriban / safety

- Data only: title, fields, sections, columns, rows, cards, meta.
- No `include`, no arbitrary method calls. Values HTML-encoded.
- Keep `data-role` markers: `title`, `label`, `value`, `grid`, section roles (read_html, PPT style copy, LibreOffice round-trip).
- **NoneInteractive:** no interactive handlers from **data**; chart scripts may run only if the layout is chart/dashboard and the template ships them (or use server-rendered SVG/PNG for none-interactive). Prefer static SVG/PNG for print fidelity.
- **Interactive:** scripts from **shipped template / vendored bundles** only; still reject `javascript:` in data-bound attrs.
- **CSS in iframe (adopted):** Result HTML **may** include Tailwind utility CSS and other stylesheets **scoped to the iframe**. Host app CSS/JS must not leak in. Do **not** rely on `cdn.tailwindcss.com` or jsDelivr at runtime — vendor files under e.g. `wwwroot/agent-ui/` or compile used classes into `PrintCss` / `agent-ui.css` at build/seed time.

### Style table (built-in default)

Font Arial. Inline on `data-role` nodes so PowerPoint can copy them.

| Level | Size | Weight | Align | Color | Extra |
|-------|------|--------|-------|-------|-------|
| Document title | 18pt | bold | center | `#1a1a1a` | blank row after |
| Section | 13pt | bold | left | `#1a1a1a` on `#f0f2f4` | blank row before |
| Nested section | 12pt | bold | left | `#1a1a1a` | blank row before |
| Field label | 10pt | bold | left | `#555555` | |
| Field value | 10pt | normal | left | `#1a1a1a` | |
| Table header | 10pt | bold | left | `#ffffff` on `#3d4f5f` | repeat on PDF pages |
| Table cell | 10pt | normal | left | `#1a1a1a`, border `#dddddd` | numbers right |
| Notes | 10pt | normal | left | `#333333` | blank row before |

Excel title: 13pt bold, centered. Excel/CSV do not read HTML templates. Field rows: two label/value pairs per row.

---

## Layouts: table, cards, images

| Layout | Data | Page |
|--------|------|------|
| `form` | Form tree above | One document |
| `table` | Columns + rows | One grid; header repeats on page break where format supports it |
| `cards` | One record per card | `cardsPerRow`; PDF/Word/PPT also `cardRows` |

### Cards

| Format | Parameters | Placement |
|--------|------------|-----------|
| HTML / Result | `cardsPerRow` | Flowing grid; no page size |
| Excel | `cardsPerRow` | Sheet rows; no page size |
| PDF, Word | `cardRows`, `cardsPerRow` | Page = `cardRows × cardsPerRow` |
| PowerPoint | `cardRows`, `cardsPerRow` | One slide = one page |
| CSV | — | One flat row per record |

Image fields: `fileId`, app image URL, or `http(s)` image URL. Model does not send bytes. HTML/PDF/Word/Result: data-URI `<img>` when bytes load. Excel: anchored picture. PPT: picture on card. Failures listed in `missingImages`; file/panel still produced.

### Charts / KPI / dashboard

**Shipped today:** `form` / `table` / `cards` only. Legacy `data_render` `chart` / `kpi_dashboard` still map to `table` / `cards` with `mappedFrom`.

**Adopted for the next slice** (MCP-like Result look):

| layout | Result iframe | Export |
|--------|---------------|--------|
| `kpi` | KPI card row(s), Tailwind-styled | Same numbers as form/table fields; no chart image |
| `chart` | One primary chart (+ optional small table) | Chart → **PNG** embedded; table as today |
| `dashboard` | KPIs + 1–N charts + optional grid | Each chart → PNG; grids/tables as today |

**Export chart images (adopted):** C# may render chart HTML (or SVG) to PNG (Playwright screenshot of a chart fragment, or equivalent) and inject `data-URI` / file images into the merged HTML before LibreOffice / Playwright PDF / ShapeCrawler. Failures go to `missingImages`; the file is still written. Cap charts per document (suggest ≤9, same as old dashboard chart block cap).

**Still not AI-written HTML:** model / `data_analyze` pass `dataset_name` + small **spec**; C# fills templates and draws only.

### Next slice — locked decisions (2026-10-20)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Spec | **Model or `data_analyze` passes a small spec** (`kpiFields`, `xField`, `yField`, `chartType`, dashboard `blocks`). Render does not invent the chart meaning. Optional weak inference only if spec is omitted and columns are obvious — never required for success. |
| 2 | Aggregation | **`data_analyze` (or prior tool) writes a shaped cached dataset** (already aggregated / top-N). **`render_*` only paints** that dataset + spec. No heavy group-by inside render. |
| 3 | Assets | Vendor **prebuilt `agent-ui.css`** (Tailwind compiled or hand CSS) + **Chart.js** (or equiv) under `wwwroot/agent-ui/`. Iframe loads local URLs or inlined critical CSS. **No runtime CDN.** Goal: iframe looks MCP-like. |
| 4 | Dual path | **One template family**, bind flag `forExport` / mode: Result interactive = live chart script; export / none-interactive print = **PNG (or static SVG) placeholders** filled by C# before write. |
| 5 | Rasterize | Reuse **Playwright** (same stack as `export_pdf`) to screenshot chart fragments → PNG → embed. Cap ≤9 charts/doc; timeout; failures → `missingImages`, file still written. Limit concurrency so chart jobs do not starve PDF. |

### Spec shape (next slice)

Passed as tool arg `specJson` (string) and/or stored on the cached dataset meta. Illustrative:

```json
{
  "title": "Sales overview",
  "kpis": [
    { "label": "Total Sales", "field": "Amount", "op": "sum" },
    { "label": "Orders", "field": "OrderId", "op": "count" }
  ],
  "chart": {
    "type": "bar",
    "xField": "Region",
    "yField": "Amount",
    "title": "By region"
  },
  "blocks": [
    { "type": "kpi" },
    { "type": "chart", "chartType": "bar", "xField": "Month", "yField": "Amount" },
    { "type": "table", "limit": 20 }
  ]
}
```

For `layout=kpi` use `kpis` only. For `layout=chart` use `chart` (+ optional table from same dataset). For `layout=dashboard` use `blocks` (≤12 KPI items, ≤9 charts, ≤3 tables — align old dashboard caps).

**Pipeline:** query/SP → cache raw → `data_analyze` writes e.g. `dataset_name=sales_by_region` + optional spec on meta → `render_none_interactive` / `render_interactive` with `layout=chart|kpi|dashboard` + `dataset_name` + `specJson`.

### Implementation order (next build)

1. Spec DTO + `specJson` on render tools; validate and ack errors naming missing fields.  
2. `wwwroot/agent-ui/` assets + seed TemplateContent keys `kpi` / `chart` / `dashboard`.  
3. Result merge (interactive Chart.js in iframe).  
4. Export path: `forExport` → Playwright chart PNG → inject → existing HTML/PDF/DOCX/PPTX writers.  
5. Alias: `ui=chart`→`layout=chart`, `kpi_dashboard`→`layout=dashboard`; prompt/hook updates.  
6. Fixture tests: one Sales-style analyze→render→export with at least one embedded chart image.

**Still out of next slice:** pixel-identical Result vs Word; Excel chart objects; Wijmo in iframe; runtime CDN; template visual editor.

---

## Library: `platform-ui-render`

Rename from `data-ui-render` (update subscriptions / exclusions in the same migration).

| Tool | Method | Mode | Template |
|------|--------|------|----------|
| `render_none_interactive` | `AgentUiRenderPlugin.RenderNoneInteractive` | `NoneInteractive` | this row |
| `render_interactive` | `AgentUiRenderPlugin.RenderInteractive` | `Interactive` | this row |
| `data_render` | alias → NoneInteractive (or Interactive if `actionsJson`) | — | one release, then delete |

Shared params: `layout`, `dataset_name`, `title`, `limit`, `cardsPerRow`, `cardRows`, `content`, **`specJson`** (required for reliable `kpi`/`chart`/`dashboard`; optional elsewhere). Interactive adds `actionsJson` (`{ Id, Label }[]`).

Ack to model: `{ ok, mode, layout, title, rowCount, truncated }`. Full HTML on SSE (`ui_render`), not in tool text.

---

## Library: `platform-document-export`

| | |
|--|--|
| LibraryKey | `platform-document-export` |
| Class | `App.BL.AIAgent.GenericAgent.Plugins.AgentDocumentPlugin` |
| Subscribe | Not automatic. Any one enabled tool opens the Files tab. |

| Tool | Role |
|------|------|
| `export_html` / `read_html` | Merged none-interactive HTML. Email: no `<style>`. Read via `data-role`. |
| `export_csv` / `read_csv` | UTF-8 BOM, RFC 4180. No template. |
| `export_excel` / `read_excel` | Fixed style table. `.xls` read via GemBox. No template. |
| `export_docx` / `read_docx` | LibreOffice HTML→docx; read docx→HTML→`data-role`. |
| `export_pdf` | Playwright on merged HTML (+ PrintCss). |
| `convert_docx_to_pdf` | LibreOffice on existing session `.docx` only (no re-merge). |
| `read_pdf` | PdfPig plain text. |
| `export_pptx` / `read_pptx` | ShapeCrawler; styles from `data-role`. |

HTML/PDF/DOCX/PPTX merge loads **`render_none_interactive.TemplateContent[layout]`** (+ that row’s `printCss` when needed). Export tool rows keep template columns NULL.

Export returns `{ ok, relativePath, rowCount, bytes, engine, template }`. `engine`: `playwright` \| `libreoffice` \| `shapecrawler`. Read returns tree, data rows previewed to 500. Failures: `{ ok: false, error }`. Tool result never includes file body.

### PDF and Word

- `export_pdf` → `AppReportPdfService.RenderHtmlToPdfAsync`. A4, portrait, 15mm, backgrounds on.
- `export_docx` → same merged HTML via LibreOffice. `page-break-after` from `cardRows` where LibreOffice understands it.
- `convert_docx_to_pdf` → existing `.docx` only; not a data export path.
- `read_docx` → LibreOffice to HTML → `HtmlForm.Read` / `data-role`.
- `read_pdf` → PdfPig, capped at `DocumentStyles.PdfTextCap`.

GemBox is **not** an export write engine (package may remain for `.xls` read and other features).

### PowerPoint

`PptxForm` + ShapeCrawler. Title slide, then section slides. Grids split after `DocumentStyles.PptRowsPerSlide`. Cards use `cardsPerRow` / `cardRows`. Copy inline styles from merged template `data-role` nodes. Do not add Aspose.Slides / Syncfusion / Spire.

### Print vs Result

| Scenario | Behavior |
|----------|----------|
| Result = `render_none_interactive` | Export same `layout` → identical merge body (PDF adds PrintCss) |
| Result = `render_interactive` | Export still uses **none-interactive** template for that `layout` |
| Do not | Store print templates on `export_html` or other export tools |

---

## Result iframe (Chat)

1. SSE **`ui_render`**: `{ html | htmlPath, mode, title, layout, rowCount, truncated, renderId }`. Persist on the assistant message.
2. Chip opens Result iframe; one open at a time; width via `agentRenderPanelPct`.
3. Sandbox: none-interactive **without** `allow-scripts`; interactive `allow-scripts allow-same-origin`. Prefer `srcDoc`.
4. Interactive postMessage:

```json
{ "type": "agent_ui_action", "renderId": "…", "actionId": "…", "selection": { } }
```

Host validates source → user turn with prefix **`[ui_render_action]`** (legacy `[data_render_action]` accepted during compatibility, then removed).

5. Eight-row rule for small none-interactive previews in transcript (see Chat-UI); large → chips only. No Wijmo remount.
6. No Tailwind CDN; no host theme leakage into iframe. MCP Client: copy ArtifactPanel layout/motion only — not AI-written artifact HTML.

---

## Compatibility

1. Ship both render tools + seeded templates.
2. Keep `data_render` alias one release; update prompts/hooks to `render_none_interactive` / `render_interactive`. Alias map: `grid`→`table`, `card`→`cards`, `chart`→`chart`, `kpi`→`kpi`, `kpi_dashboard`→`dashboard` (+ `mappedFrom`).
3. Old messages without HTML: chip “Re-run to view in Result” (no Wijmo fallback required after cutover).
4. Next release: delete `data_render`, `AgentDataRenderPlugin` surface, React `dataRender/*` renderers.

---

## Implementation checklist

### A. Docs

- [x] This canonical document
- [x] `AI-Agent-Chat-UI.md` points here for Result iframe
- [x] Remove former V1 / V2 / V3 draft files

### B. Database

- [x] `TemplateContent`, `TemplateMetaJson` on `AppAgentLibraryTool` (`V059`)
- [x] Rename `data-ui-render` → `platform-ui-render`
- [x] Seed `render_none_interactive`, `render_interactive` (schema, ToolConfig, TemplateContent form/table/cards/kpi/chart/dashboard + specJson, TemplateMetaJson)
- [x] `data_render` alias one release
- [x] BL/DTO read-write new columns (admin editor optional phase 2)

### C. Merge engine

- [x] Loader: `render_none_interactive` → TemplateContent JSON → layout (`UiTemplateMerge`; `{{code_default}}` → `HtmlForm`)
- [x] HTML merge + `data-role` via `HtmlForm` (custom Scriban bodies later when TemplateContent is not code_default)
- [x] None-interactive sanitizer; interactive keeps template scripts + postMessage bridge
- [x] Wire `AgentDocumentPlugin` HTML/PDF/DOCX/PPTX to this loader (code fallback if DB empty)
- [ ] Unit tests: markers, no script in none-interactive, layout miss, **render body === export_html body** for same fixture

### D. Render plugin + SSE

- [x] `AgentUiRenderPlugin.RenderNoneInteractive` / `RenderInteractive`
- [x] `data_render` alias
- [x] SSE `ui_render`; persist html / htmlPath
- [x] Large HTML → `AgentOutput/{session}/render/{renderId}.html`

### E. React Result host

- [x] iframe `srcDoc` + sandbox by mode (`UiRenderFrame.tsx`)
- [x] postMessage → `[ui_render_action]`
- [x] Chips bind `ui_render`; HTML uses iframe (legacy DataJson still uses `DataRenderPanel`)

### F. Prompts / hooks

- [x] Replace “call data_render” with render tools; prefer `dataset_name`

### G. Out of this build (shipped slice)

- Template visual editor, company copy/Restore table
- `kpi` / `chart` / `dashboard` **code** (decisions locked; implement as next build §H)
- Pixel-identical interactive Result vs Word
- Wijmo inside iframe; runtime Tailwind/Chart CDN
- SQL inside tools; pivot; logos; screenshot of chat widgets; changing `file_write`

### H. Next build (kpi / chart / dashboard) — checklist

- [x] Lock decisions (spec from model/analyze; shaped dataset; vendored assets; forExport; Playwright PNG)
- [x] Spec DTO + `specJson` on render tools (`UiRenderSpec`, `AgentUiRenderPlugin`)
- [x] Vendor `wwwroot/agent-ui/agent-ui.css` (inline `AgentUiCss` for srcDoc; C# SVG charts — no Chart.js CDN)
- [x] Seed `TemplateContent` keys `kpi`, `chart`, `dashboard` (`V059`, `{{code_default}}` → `UiDashboardHtml`)
- [x] Result merge (SVG charts in iframe via `UiDashboardHtml`)
- [x] Export: `forExport` → `ChartRasterize` Playwright PNG → embed; `missingImages` on failure
- [x] Alias + prompt updates (`chart` → chart, `kpi_dashboard` → dashboard; hooks/V059)
- [x] Unit tests: `UiDashboardHtmlTests` (KPI/chart/dashboard + alias). Full Playwright rasterize fixture optional in CI.

---

## Code map

| Piece | Location |
|-------|----------|
| Render plugin | `APP.BL/.../Plugins/AgentUiRenderPlugin.cs` (new) |
| Document export | `APP.BL/.../Plugins/AgentDocumentPlugin.cs` |
| Documents | `APP.BL/.../Documents/` (`HtmlForm`, `FormDocument`, `UiTemplateMerge`, `UiRenderSpec`, `UiDashboardHtml`, `ChartRasterize`, `PptxForm`, …) |
| Agent UI CSS | `AppAI.Web/wwwroot/agent-ui/agent-ui.css` (srcDoc uses inline `AgentUiCss`) |
| Cache | `APP.BL/.../DataAnalyze/AgentDataAnalyzeCacheBL.cs` |
| Files | `APP.BL/.../GenericAgentFileBL.cs` |
| Library tools | `APP.BL/TenantBusiness/AppAgentLibraryToolBL.cs` |
| SSE | `AppAI.Web/Controllers/GenericAgentController.cs` |
| Chat | `AppReact/src/components/aiskill/GenericAgentChat.tsx` |
| Result iframe | `AppReact/src/components/aiskill/` (e.g. `UiRenderFrame.tsx`) |
| Migrations | `AppAI.Web/Migrations/V0xx__…_xhu.sql` |

```
JSON or dataset_name → cache row rules → FormDocument
  ├─ render_none_interactive.TemplateContent[layout] → merged HTML
  │    ├─ SSE ui_render → iframe
  │    ├─ export_html     inline styles only
  │    ├─ preview / pdf   + PrintCss → RenderHtmlToPdfAsync
  │    ├─ export_docx     soffice HTML → docx
  │    └─ export_pptx     data-role → PptxForm
  ├─ convert_docx_to_pdf  soffice docx → pdf
  ├─ read_docx / read_pdf / read_html
  ├─ ExcelForm / CsvForm  (fixed style; no HTML template)
  └─ render_interactive   same engine + Interactive template → iframe + postMessage
```

Do not add ClosedXML, EPPlus, NPOI, QuestPDF, iText, Aspose, Pico.css. Do not rewrite the report engine. Do not remove GemBox packages used elsewhere.

| Format | Write | Read |
|--------|-------|------|
| HTML | Merged template; email no `<style>` | AngleSharp `data-role` |
| CSV | Our code | Our code |
| Excel `.xlsx` | Open XML fixed style | Open XML |
| Excel `.xls` | — | GemBox.Spreadsheet |
| Word | LibreOffice HTML→docx | LibreOffice→HTML→`data-role` |
| PDF from data | Playwright | PdfPig |
| PDF from Word | LibreOffice convert | PdfPig |
| PowerPoint | ShapeCrawler | ShapeCrawler |

---

## Test standard (checks)

Use these as acceptance criteria for the build and regressions.

### Templates and merge

- [ ] Seeded `form` / `table` / `cards` on `render_none_interactive` produce `data-role` markers and V1 style table look (18pt centered title, two fields per row, section above grid).
- [ ] Same fixture + `layout=table`: `render_none_interactive` HTML body **equals** `export_html` body (before PrintCss).
- [ ] Missing layout key → error, no side effects.
- [ ] `TemplateContent` never appears in tool definitions sent to the model.
- [ ] Export tool rows have NULL template columns.

### None-interactive / interactive

- [ ] None-interactive iframe: script injected via data cannot run.
- [ ] Interactive: button click → one host user message with `actionId` + selection.
- [ ] Export from an interactive Result still uses none-interactive template (no buttons/scripts in Word/PDF).

### Cache and limits

- [ ] Short `content` matching one cache writes/shows every cached row; `rowCount` matches.
- [ ] Two matching datasets, no `dataset_name` → error, no file/panel.
- [ ] 50001 data rows → error, no partial file, no panel HTML.
- [ ] `../` rejected; tool result has no file body.

### Engines

- [ ] Email HTML: no `<style>` / flex / grid / float. Preview/PDF may include PrintCss.
- [ ] Unit tests assert merged HTML markers without launching Chromium.
- [ ] LibreOffice tests skip when `soffice` missing; when present, docx round-trips via `read_docx`.
- [ ] `convert_docx_to_pdf` does not call Playwright.
- [ ] `read_pdf` uses PdfPig on a small fixture.
- [ ] Existing GemBox **write** tests are not the export path.

### Chat / compatibility

- [ ] `ui_render` opens Result iframe; chip restore works after reload (panel closed on reload is OK per Chat-UI).
- [ ] `data_render` alias still works during the compatibility release.
- [ ] Agent with no document-export tools does not see those tools or the Files tab.

### Committed defaults (override only by explicit decision)

1. SSE event name: `ui_render`.
2. Chart/KPI/dashboard next build: **fully locked** — spec from model/`data_analyze`; analyze shapes dataset; render paints only; vendored iframe assets; dual `forExport` path; Playwright chart→PNG. See “Next slice — locked decisions”.
3. HTML > 512 KB → session file + `htmlPath`.
4. Library rename `data-ui-render` → `platform-ui-render` in the same migration.
5. Interactive host action prefix: `[ui_render_action]` (replaces legacy `[data_render_action]` at cutover).
6. No TemplateContent admin editor in this build — SQL seed (and direct DB edit if needed) only.
7. No runtime CDN for Tailwind/Chart.js — vendor or precompile into `wwwroot/agent-ui/` / `PrintCss`.

---

## Out of scope

SQL inside render/export tools. Pivot, merged input cells, logos, per-call colors. Replacing the report engine. Changing `file_write`. Pixel-identical interactive Result vs Word. LibreOffice reproducing all Tailwind flex/grid. Template visual editor / dedicated template table (this build uses tool columns). GemBox as HTML/PDF/DOCX write engine. Embedding Wijmo in the iframe. Runtime CDN stylesheets/scripts. Excel native chart objects.
