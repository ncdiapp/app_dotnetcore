# Agent document export and read

This document specifies agent tools that turn structured data into files, so the model does not spend tokens drawing Excel, PDF, Word, or slides.

Part 1 writes a transaction form: a title, label/value fields, sections, and grids. The tool picks the font, size, color, alignment, and blank lines from the level. The same tree is written as HTML, CSV, Excel, Word, PDF, or PowerPoint, and read back.

Part 2 adds a plain table layout, a card page, images, and an optional document preview. Chat widgets stay as they are, including Wijmo FlexGrid and charts. The file follows our layout. Export writes the file immediately unless the user asks to preview first, or the agent sets `preview` on the call. That preview is the document, and the downloaded file matches it.

How to use it: enable any tool from the library `platform-document-export` on the agent. One enabled tool opens the chat Files tab. The model calls `export_html`, `export_excel`, or the matching tool, and passes the form JSON or a path to a file already in the session. A bare file name is saved under the chat folder's `output/` directory. A path that already includes a folder is kept. The saved name adds `_yyyyMMdd_HHmmss` before the extension. The tool returns the path, the row count, and the byte size. Names in the reply that match a file from that turn are download links. `read_html`, `read_excel`, and the other read tools load a file this library wrote and return the form again. `read_pdf` returns text only.

The model does not send HTML, fonts, or colors.

Status: Part 1 implemented. Session-cache export and Part 2 are agreed, not built. Seed: `AppAI.Web/Migrations/V050__AgentDocumentExportTools_xhu.sql`. Date: 2026-10-16. Update this file when the plan changes.

`file_write` / `file_read` stay for plain text and the older Excel branch. The tool result is capped by the agent's `MaxToolResultChars`, so the file body is never returned.

## Limits

| What | Cap |
|---|---|
| Session cache (`AgentDataAnalyzeCacheBL`) | 50000 data rows |
| Chat grid (`data_render`) | 50000 data rows |
| Export (`DocumentStyles.MaxDataRows`) | 50000 data rows |
| Columns | 50 |
| Rows returned to the model on read | 500 |
| Section depth drawn | 3 |
| PDF text returned to the model | about 12000 characters |
| One file | 20 MB |

Over 50000 data rows, export returns an error and writes no file. The session cache keeps the first 50000 and marks the result truncated. The 500-row read preview is only what the model sees. It is not the grid and not the file.

The cache is process memory for the current chat session. A restart drops it.

## Part 1 — form export

### Input

Same shape as a transaction form. Nesting is the outline. Depth 1 is the document title, depth 2 is a section, depth 3 and below render as depth 3.

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

`rows` may be objects. If `columns` is omitted, use the first object's keys. Cells are string, number, bool, ISO-8601 date, or null.

Pass `path` plus exactly one of `content` or `sourcePath`. `sourcePath` may be `.json`, `.csv`, `.xlsx`, or `.html` already in the session. A `.csv` or `.xlsx` becomes one table under the title.

### Where the file goes

`GenericAgentFileBL` writes `FileRepository/Company_{id}/AgentOutput/{session}/`. Reject `..`. A bare export name is placed in `output/`. Example: `hello_world.docx` becomes `output/hello_world_20261016_093405.docx`.

### Style table

Fixed. The model cannot override it. Font is Arial. Spacing is inserted by the writer, from the level.

| Level | Size | Weight | Align | Color | Extra |
|---|---|---|---|---|---|
| Document title | 18pt | bold | center | `#1a1a1a` | one blank row after |
| Section | 13pt | bold | left | `#1a1a1a` on `#f0f2f4` | blank row before |
| Nested section | 12pt | bold | left | `#1a1a1a` | blank row before |
| Field label | 10pt | bold | left | `#555555` | |
| Field value | 10pt | normal | left | `#1a1a1a` | |
| Table header | 10pt | bold | left | `#ffffff` on `#3d4f5f` | |
| Table cell | 10pt | normal | left | `#1a1a1a`, border `#dddddd` | numbers align right |
| Notes | 10pt | normal | left | `#333333` | blank row before |

Excel title text is 13pt bold, centered. HTML, Word, and PDF keep the 18pt title. Excel sets every data row's height from the font size and the wrapped line count so text is not clipped.

Field rows hold two pairs: label, value, label, value.

### Writers

One tree, one style table, a writer per format. Word and PDF are not an HTML import.

| Format | How the layout is applied |
|---|---|
| HTML | One outer table. Blocks are `<tr><td>`. Field pairs and grids are nested tables. Font, size, color, align, and padding are inline styles on `td` / `th`. No `<style>` block, no class layout, no `display`, no flex, no grid, no float. Email and rich-text editors keep this. |
| Word | GemBox.Document. Paragraph and table-cell formatting uses the sizes and colors above. |
| PDF | Save that Word model with GemBox.Document. PDF matches Word. |
| Excel | One worksheet. A grid at least 4 columns wide. Title and section rows are merged across. Field pairs occupy the four cells. Each grid sits below its section. Open XML write. Do not flip global `UseOpenXml`. |
| CSV | Values only. A section name on its own row, then the grid. No fonts. |
| PowerPoint | ShapeCrawler. One title slide, then one slide per section. Split a grid after about 12 rows onto the next slide. |

`read_html` (AngleSharp) walks the tables and `data-level` markers we write. Email clients may strip those markers. Round-trip is for files we wrote.

`read_docx` maps heading-like paragraphs and tables back to sections, fields, and grids.

`read_excel` uses the style index: title, section, label/value, header, cell, notes. `.xls` is read with GemBox.Spreadsheet.

`read_pdf` returns `pageCount` and plain text. No table rebuild.

### Library

| | |
|---|---|
| LibraryKey | `platform-document-export` |
| Display name | Platform Document Export |
| Class | `App.BL.AIAgent.GenericAgent.Plugins.AgentDocumentPlugin` |
| ToolType | `BuiltIn` |
| Subscribe | Not automatic. Any one enabled tool in this library opens the Files tab. |

| Tool | Role |
|---|---|
| `export_html` / `read_html` | Email-safe table HTML |
| `export_csv` / `read_csv` | UTF-8 with BOM, RFC 4180 |
| `export_excel` / `read_excel` | Form layout on one sheet. Read `.xls` via GemBox |
| `export_docx` / `read_docx` | Same style table |
| `export_pdf` | From the Word model |
| `read_pdf` | Plain text |
| `export_pptx` / `read_pptx` | ShapeCrawler |

Export returns `{ ok, relativePath, rowCount, bytes }`. Read returns the tree, with data rows previewed to 500. Failures are `{ ok: false, error }`.

### Checks

- HTML source is tables and inline styles only.
- HTML, Word, and PDF use a centered 18pt title. Excel title text is 13pt bold, centered, and row heights cover the text.
- Two fields share one row. A section sits above its grid.
- 50001 data rows: error, no partial file.
- Tool result has no file body. `../` is rejected.
- An agent with no tool from this library does not see the tools or the Files tab.

### Out of scope for Part 1

SQL inside the tool. Pivot, merged input cells, logos, and per-call colors. Replacing the report engine. Changing `file_write`. Copying a chat widget into the file.

## Export from the chat cache

Agreed. Not built. Do this before Part 2 layouts. Part 1 calls that pass `content` or `sourcePath` keep working.

A query, stored procedure, or API tool that returns a table is stored by `AgentDataAnalyzeCacheBL` on the chat session. The model receives a short envelope: `dataset_cached`, `dataset_name`, row count, and column names. It does not receive every row. `data_render` already loads that name for the grid.

Export must load the same cache. Otherwise the model pastes the truncated envelope and the file drops rows.

Add `dataset_name` on each export tool. When it is set, read `AgentDataAnalyzeCacheBL` for this `ChatSessionKey` and write those rows. If `content` is also passed, ignore it. An unknown name returns an error and writes no file. The file contains every cached row, up to 50000. When the cache itself was truncated, the result says so.

```json
{ "path": "orders.xlsx", "dataset_name": "sql_query_2" }
```

The cache row objects become one table under the document title. `dataset_name` is not a Search id and not a FlexGrid on the screen. The tool does not re-run the query.

## Part 2 — table, cards, images

Agreed. Not built.

The chat screen and the file are two views of the same data. Wijmo FlexGrid, charts, and later widgets stay in the chat. Do not replace them, and do not turn their pixels into the file. The model fills data and parameters. It still does not send fonts, colors, or HTML.

### Preview

Default is no preview. The tool writes the file.

Set `preview` true only when the user asks to see the document before export, or the agent sets that parameter. Then the tool does not write the office file. It builds the email-safe HTML for that layout. If that HTML fits in the tool result, return it for the chat to show. If it is longer than `MaxToolResultChars`, save it under `output/` and return the path. The chat loads that HTML. A later export call with `preview` omitted writes the file from the same input. The file matches this HTML. The binary file is not embedded in the chat.

### Layouts

| Layout | What the data is | Page |
|---|---|---|
| `form` | Part 1 tree | One document |
| `table` | Columns and rows | One grid. Repeat the header when a format breaks a page |
| `cards` | One record per card | `rows` by `columns` cards on each page |

`layout` defaults to `form`, so current calls stay valid. A `dataset_name` on a `table` export is the cache from the previous section.

### Cards

`rows` and `columns` are the card grid for one page. The agent passes them from the user, or from a Search card view it has already read. Omitted values default to 2 and 2. Cards on a page = `rows × columns`. Page count = record count divided by that, rounded up.

Each card has the same slots: an image, a title, and field lines. The slot list is fixed for the call. Records only fill those slots. Search Card View on screen stays the existing component.

HTML, Word, and PDF: one page is one table of `rows` by `columns` cards. PowerPoint: one slide is one page. Excel: one sheet, each page in its own block. CSV: one flat row per record, no card grid.

### Images

A slot may be `{ "fileId": 123 }` or `{ "url": "https://..." }`. The model does not send image bytes. The tool resolves `fileId` in this application, or fetches the URL, once per export.

| Format | Picture |
|---|---|
| HTML | `<img>` with the resolved source. Email uses a reachable URL, or the image inlined when the client allows it |
| Word, PDF | GemBox picture |
| PowerPoint | ShapeCrawler picture |
| Excel | Picture anchored at the cell |
| CSV | The `fileId` or URL as text |

Missing or oversized images leave the slot empty. The tool result names the slot. Do not fail the whole file for one image.

### Charts

A chart on screen stays the chat widget. Export writes the chart's data as a `table` layout, from `dataset_name` when the query was cached. A picture is included only when the caller passes an image slot. That picture is not required to match the on-screen chart.

### Part 2 input

```json
{
  "layout": "cards",
  "preview": false,
  "rows": 2,
  "columns": 3,
  "card": ["image", "title", "Price", "City"],
  "records": [
    {
      "image": { "fileId": 123 },
      "title": "Widget",
      "Price": 20,
      "City": "Boston"
    }
  ]
}
```

For `layout: "table"`, `columns` is the header list and `rows` is the data. For `layout: "cards"`, `rows` and `columns` are the page shape. The tool tells them apart by `layout`.

### Part 2 out of scope

Reading Search or a chart control from inside the tool. The agent passes the records and the page shape. Screenshot of FlexGrid or of a chart. Editable Excel charts. Per-call fonts and colors. Changing the on-screen card, grid, or chart components.

## Code

```
JSON or dataset_name → FormDocument
  ├─ HtmlForm    inline styles, tr/td only
  ├─ CsvForm
  ├─ ExcelForm   Open XML
  ├─ WordForm    GemBox.Document → docx and pdf
  └─ PptxForm    ShapeCrawler
```

Cache reads go through `APP.BL/AIAgent/GenericAgent/DataAnalyze/AgentDataAnalyzeCacheBL.cs`. The grid reads it from `APP.BL/AIAgent/GenericAgent/Plugins/AgentDataRenderPlugin.cs`.

PowerPoint uses [ShapeCrawler](https://github.com/ShapeCrawler/ShapeCrawler) (NuGet `ShapeCrawler`, MIT) on `DocumentFormat.OpenXml`. This repo has no GemBox.Presentation license. Do not add Aspose.Slides, Syncfusion, or Spire. Slides we did not author: read title text, text frames, and tables.

Leave the report engine (Playwright) alone. Do not add ClosedXML, EPPlus, NPOI, QuestPDF, iText, Aspose, or Pico.css.

## Export libraries

| Format | Write | Read | File |
|---|---|---|---|
| HTML | Our code | AngleSharp | `APP.BL/AIAgent/GenericAgent/Documents/HtmlForm.cs` |
| CSV | Our code | Our code | `APP.BL/AIAgent/GenericAgent/Documents/CsvForm.cs` |
| Excel `.xlsx` | DocumentFormat.OpenXml | DocumentFormat.OpenXml | `APP.BL/AIAgent/GenericAgent/Documents/ExcelForm.cs` |
| Excel `.xls` | — | GemBox.Spreadsheet | `APP.BL/AIAgent/GenericAgent/Documents/ExcelForm.cs` |
| Word | GemBox.Document | GemBox.Document | `APP.BL/AIAgent/GenericAgent/Documents/WordForm.cs` |
| PDF | GemBox.Document | GemBox.Document | `APP.BL/AIAgent/GenericAgent/Documents/WordForm.cs` |
| PowerPoint | ShapeCrawler | ShapeCrawler | `APP.BL/AIAgent/GenericAgent/Documents/PptxForm.cs` |

Calls enter `APP.BL/AIAgent/GenericAgent/Plugins/AgentDocumentPlugin.cs`. Files are saved by `APP.BL/AIAgent/GenericAgent/GenericAgentFileBL.cs`. Styles live in `APP.BL/AIAgent/GenericAgent/Documents/DocumentStyles.cs`. The tree is `APP.BL/AIAgent/GenericAgent/Documents/FormDocument.cs`.
