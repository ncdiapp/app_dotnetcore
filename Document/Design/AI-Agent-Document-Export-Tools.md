# Agent document export and read

This document specifies agent tools that turn structured data into files, so the model does not spend tokens drawing Excel, PDF, Word, or slides.

The data looks like a transaction form: a title, label/value fields, sections, and grids. The tool picks the font, size, color, alignment, and blank lines from the level of each part. The same tree can be written as HTML, CSV, Excel, Word, PDF, or PowerPoint, and read back.

How to use it: subscribe the agent to the library `agent-documents`. In chat, the model calls `export_html`, `export_excel`, or the matching tool, and passes the form JSON (or a path to a file already in the session). The tool saves the file under the chat folder and returns the path, row count, and byte size. The user downloads it from the files panel. `read_html`, `read_excel`, and the other read tools load a file this library wrote and return the form again. `read_pdf` returns text only.

The model does not send HTML, fonts, or colors. One walker assigns a level to each node. Each writer applies the same style table for that level.

Status: implemented. Library seed is `V052__AgentDocumentTools.sql`. Date: 2026-10-15. Update this file when the plan changes.

`file_write` / `file_read` stay for plain text and the current Excel branch. Tool results are capped (`MaxToolResultChars`, default about 4000), so the file body is never returned.

## Input

Same shape as a transaction form: a title, header fields, sections, and grids inside sections. Nesting is the outline. Depth 1 is the document title, depth 2 is a section, depth 3 and below render as depth 3.

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

`rows` may be objects. If `columns` is omitted, use the first object's keys. Cells are string, number, bool, ISO-8601 date, or null. Limits: 5000 data rows, 50 columns, section depth rendered only to 3. Over the limit, return an error and write no file. Read preview to the model is 500 rows.

Shared export arguments: `path` (session-relative) and `sourcePath` (one of `content` or `sourcePath`). `sourcePath` may be this JSON, or a `.csv` / `.xlsx` that becomes a single table under the title.

## Style table

Fixed. The model cannot override it. Font is Arial. Spacing is inserted by the writer, from the level, the way a Markdown viewer styles a heading without a blank line in the source.

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

Field rows hold two pairs: label, value, label, value. That is the transaction-form column pair, done with cells.

## Writers

One tree, one style table, a writer per format. Do not style Word or PDF by hoping an HTML import keeps CSS.

| Format | How the layout is applied |
|---|---|
| HTML | One outer table. Blocks are `<tr><td>`. Field pairs and grids are nested tables. Every font, size, color, align, and padding is an inline `style` on `td` / `th`. No `<style>` block, no class-based layout, no `display`, no flex, no grid, no float. This is what email and rich-text editors keep. |
| Word | GemBox.Document. Paragraph and table-cell formatting uses the sizes and colors above. Not an HTML round-trip. |
| PDF | Save that Word model with GemBox.Document. PDF matches Word. |
| Excel | One worksheet, not one sheet per grid. A fixed grid of 4 columns. Title and section rows are merged across. Field pairs occupy the four cells. Each grid sits below its section with a filled header row and borders. A short empty row is the blank line. Open XML write, so the GemBox free cap (about 5 sheets / 150 rows) does not apply. Do not flip global `UseOpenXml`. |
| CSV | Values only. A section name on its own row, then the grid. No fonts. |
| PowerPoint | ShapeCrawler. One title slide, then one slide per section: section title, field pairs as a table, grid as a table. Split a grid after about 12 rows onto the next slide. |

`read_html` (AngleSharp) walks the tables and `data-level` markers we write, and rebuilds the tree. Email clients may strip those markers; round-trip is for files we wrote.

`read_docx` maps Heading-like paragraphs and tables back to sections, fields, and grids.

`read_excel` treats a merged bold row as a title or section, a label/value pattern as fields, and a filled header plus rows as a grid.

`read_pdf` returns `pageCount` and plain text, capped at about 12000 characters. No table rebuild.

## PowerPoint library

Use [ShapeCrawler](https://github.com/ShapeCrawler/ShapeCrawler) (NuGet `ShapeCrawler`, MIT). It reads and writes `.pptx` on top of `DocumentFormat.OpenXml`, which `APP.BL` already references. No new license.

GemBox.Presentation would match the other GemBox products, but this repo has no Presentation license key. Do not add Aspose.Slides, Syncfusion, or Spire.

Slides we did not author: read title text, text frames, and tables. Ignore pictures and charts.

## What we reuse

| Piece | Use |
|---|---|
| `AppAgentToolEngine`, BuiltIn | Library tools. Not an external DLL |
| `GenericAgentFileBL` | `FileRepository/Company_{id}/AgentOutput/{session}/`. Max 20 MB. Reject `..` |
| `GemBox.Document` | Word and PDF from the style table. License already in `appsettings.json` |
| Open XML | Excel write |
| AngleSharp (already in `APP.BL`) | Read our HTML |
| ShapeCrawler | Read and write pptx |

Leave the report engine (Playwright) alone. Do not add ClosedXML, EPPlus, NPOI, QuestPDF, iText, Aspose, or Pico.css.

## Library and tools

| | |
|---|---|
| LibraryKey | `agent-documents` |
| Class | `App.BL.AIAgent.GenericAgent.Plugins.AgentDocumentPlugin` |
| ToolType | `BuiltIn` |
| Seed | New migration into `AppAgentToolLibrary` and `AppAgentLibraryTool`. No auto-subscribe |

| Tool | Role |
|---|---|
| `export_html` / `read_html` | Email-safe table HTML |
| `export_csv` / `read_csv` | UTF-8 with BOM, RFC 4180 |
| `export_excel` / `read_excel` | Form layout on one sheet. Read `.xls` via GemBox |
| `export_docx` / `read_docx` | Same style table |
| `export_pdf` | From the Word model |
| `read_pdf` | Plain text |
| `export_pptx` / `read_pptx` | ShapeCrawler |

Return `{ ok, relativePath, rowCount, bytes }` on export. On read, return the tree with data rows previewed. Failures are `{ ok: false, error }`.

## Code

```
JSON tree → FormDocument (levels assigned here)
  ├─ HtmlForm    inline styles, tr/td only
  ├─ CsvForm
  ├─ ExcelForm   Open XML, 4-column form grid
  ├─ WordForm    GemBox.Document → docx and pdf
  └─ PptxForm    ShapeCrawler
```

| File | Role |
|---|---|
| `APP.BL/.../Documents/FormDocument.cs` | Parse, validate, assign levels |
| `APP.BL/.../Documents/DocumentStyles.cs` | The style table |
| `APP.BL/.../Documents/HtmlForm.cs` | Email-safe HTML |
| `APP.BL/.../Documents/ExcelForm.cs` | Form layout |
| `APP.BL/.../Documents/WordForm.cs` | docx and pdf |
| `APP.BL/.../Documents/PptxForm.cs` | pptx via ShapeCrawler |
| `APP.BL/.../Documents/CsvForm.cs` | CSV |
| `APP.BL/.../Plugins/AgentDocumentPlugin.cs` | Paths and disk |
| `AppAI.Web/Migrations/V052__AgentDocumentTools.sql` | Seed |

## Order of work

1. `FormDocument`, `DocumentStyles`, and tests (pairs, nested sections, numeric align, over-limit).
2. HTML. Open in a browser and paste into a rich-text box. Confirm no flex and no class layout.
3. Word and PDF from `WordWriter`. Compare heading size and field pairs with the HTML.
4. Excel on one sheet: title, two field pairs on a row, grid under the section.
5. CSV round-trip, including commas, quotes, and non-ASCII in Excel.
6. ShapeCrawler write and read of a two-section deck.
7. Migration. One subscribed test agent. Download each format from the files panel.

## Checks

- HTML source is tables and inline styles only.
- The same tree produces a centered 18pt title in HTML, Word, PDF, and Excel.
- Two fields share one row. A section sits above its grid.
- 5001 data rows: error, no partial file.
- Tool result has no file body. `../` is rejected.
- An agent that does not subscribe does not see the tools.

## Out of scope

SQL inside the tool. Charts, pivot, merged input cells, logos, images, and per-call colors. Embedding the file in the chat. Replacing the report engine. Changing `file_write`.
