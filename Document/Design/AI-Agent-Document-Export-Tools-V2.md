# Agent document export and read (V2)

This file replaces `AI-Agent-Document-Export-Tools-V1.md`. V1 is the 2026-10-16 design and is left unchanged. Revised: 2026-10-19.

Part 1 and Part 2 still describe the form tree, the chat cache, cards, and images. Those rules stay. This version changes how HTML, PDF, Word, and PowerPoint get their layout, and how export chooses rows from the chat cache.

## What changed from V1

- HTML, PDF, Word, and PowerPoint share one merged template. With no user copy, that template is the built-in default in code. Excel and CSV stay on the fixed style table.
- The default template is a Scriban document, one per layout (`form`, `table`, `cards`). It renders to the same tables, sizes, and colors as today's `HtmlForm`, and it keeps the same `data-role` markers (`title`, `label`, `value`, `grid`, and the section roles). It is not a paste of already-rendered HTML.
- `export_pdf` prints that merged HTML with Playwright only. There is no `pdfVia`.
- `export_docx` converts that same HTML to docx with LibreOffice only. There is no `docxVia`. Word follows the template inside LibreOffice's CSS: tables, inline sizes, colors, borders, and simple images.
- LibreOffice PDF is only `convert_docx_to_pdf`. The input is an existing session `.docx`. That PDF matches the Word file. It does not merge the template again.
- GemBox is not an export engine. The package stays because other features still use it, including `.xls` read. The free 20-paragraph cap does not apply to these exports. The Open XML fixed style-table writer is not an export path.
- PowerPoint stays ShapeCrawler. Slide geometry stays in `PptxForm`. Font, size, color, background, and border come from the template's `data-role` nodes.
- A short `content` that matches a cached dataset is replaced by that dataset's full rows, so the file matches the chat grid.
- `read_docx` converts the docx back to HTML with LibreOffice and parses `data-role`. `read_pdf` uses PdfPig.
- This slice does not add a `template` tool argument, a template table, or an editor. Every HTML, PDF, Word, and PowerPoint export uses the code default.

## This slice and the later editor

This slice ships the engines, the code default template, the cache row fix, `convert_docx_to_pdf`, PdfPig, and the LibreOffice docx reader.

The template table, company copies, and the editor stay designed below and are not built in this slice. Tools do not take `template` until a company copy can exist.

## Limits

Unchanged from V1. Session cache, chat grid, and export stop at 50000 data rows. Columns stop at 50. Read preview is 500 rows. Section depth drawn is 3. PDF text returned to the model is about 12000 characters. One file is 20 MB. Over 50000 data rows, export returns an error and writes no file.

Playwright starts a Chromium process for each PDF. LibreOffice starts `soffice` for each Word file and for each docx-to-pdf or docx-to-html read. Both calls have a timeout. Large tables stay inside the same caps. The server must already have the Playwright Chromium install and LibreOffice.

## Input, files, and cache

The model sends the form tree, `dataset_name`, or `sourcePath`. It does not send HTML, fonts, or colors. Tool text tells the model to pass `dataset_name` and not to paste rows. Files go under `FileRepository/Company_{id}/AgentOutput/{session}/`. A bare name is saved in `output/` with `_yyyyMMdd_HHmmss` before the extension. `..` is rejected.

`dataset_name` loads `AgentDataAnalyzeCacheBL` for this chat. It wins over `content` and over the remembered card panel. The tool does not re-run the query.

`layout` is `form` (default), `table`, or `cards`. `cardsPerRow` is 1–6, default 1. `cardRows` defaults to 3. Cards on one page are `cardRows × cardsPerRow`. CSV stays one flat row per record. Excel ignores `cardRows`.

The chat grid can show dozens of rows while today's Excel, Word, or PowerPoint file has only a few. The grid is `data_render` from the session cache. Export often receives `content` the model copied. `Load` parses that `content` as the whole form. `TryRestoreRows` only fills missing columns on the rows already passed. It does not add the rest of the cached rows.

Row choice for every export format:

- `dataset_name` loads that dataset's full cache.
- `content` or `records` that are a flat table or a card list, and that match one cached dataset on key values while containing fewer rows, are replaced by that dataset's full rows. A match means at least half of the passed key values hit that dataset.
- If two datasets match and `dataset_name` is omitted, the tool returns an error and writes no file.
- A multi-section form that is not a single table is written as given.
- The remembered card panel from `data_render` is a display slice. It does not override a larger matching cache. An explicit `limit` slices only after the cache is chosen.
- `rowCount` in the result is the number of rows written.

## Preview and email HTML

`preview` true returns the merged HTML and does not write the office file. That HTML is what `export_pdf` prints. It matches the Playwright PDF, including `PrintCss`. It does not match the LibreOffice Word file, the docx-to-pdf file, or the PowerPoint file. Those follow the template only as far as the engine sections below.

`export_html` stays tables and inline styles: no `<style>` block, no flex, no grid, no float. Preview and the Playwright PDF add `PrintCss` on that same body. `PrintCss` is not part of the email body.

## PDF and Word

`export_pdf` always calls `AppReportPdfService.RenderHtmlToPdfAsync` on the merged HTML. Page setup is A4, portrait, 15mm margins, backgrounds on. The report engine is not rewritten. A failure returns an error and writes no file.

`export_docx` always converts that same HTML with LibreOffice to docx. `page-break-after` is written into the merged HTML from `cardRows`. LibreOffice applies it only where its HTML import understands that CSS.

`convert_docx_to_pdf` takes `sourcePath`, a session `.docx`. `soffice` converts that file to PDF and does not merge a template. The result names the Word file as the source. Use this tool only to turn an existing Word file into PDF. A data export to PDF is `export_pdf`. The tool needs a migration after `V056` and a `ParameterSchemaJson` row. Tool arguments come from that schema, not from the C# signature alone.

`DocumentExport:SofficePath` is the LibreOffice program. The Windows default is `C:\Program Files\LibreOffice\program\soffice.exe`. Linux is typically `/usr/bin/soffice`. Each convert gets its own user profile directory so two calls do not lock the same profile. The profile and other temp files are deleted when the call ends, including when it fails or times out. Missing `soffice`, a timeout, or a non-zero exit returns an error and writes no file.

`read_pdf` uses PdfPig for page count and plain text, capped at `DocumentStyles.PdfTextCap`. Both the Playwright PDF and the docx-to-pdf file use that reader.

`read_docx` converts the docx to HTML with LibreOffice, then `HtmlForm.Read` parses `data-role`. GemBox style names `FormTitle` and `FormSection` are not on these files. A user-edited template is not required to round-trip. The built-in default is.

A data-URI picture in the card HTML is checked on a sample card through LibreOffice. A picture that does not survive is listed in `missingImages`. The file is still written.

## PowerPoint

The engine stays `APP.BL/AIAgent/GenericAgent/Documents/PptxForm.cs` and ShapeCrawler. One title slide, then one slide per section. Grids split after `DocumentStyles.PptRowsPerSlide` rows. Cards use `cardsPerRow` and `cardRows`. Pictures stay on the card.

ShapeCrawler does not layout HTML. Before drawing, read the merged template with AngleSharp and copy inline style from the `data-role` nodes: font family, font size, bold, text color, background, and border. A later user copy that only changes those values changes the slides. `flex`, `grid`, and a rewritten card structure do not change slide geometry. `read_pptx` still reads the shapes.

## User templates

Not built in this slice. No table and no editor. The rules below apply when that work starts.

The MCP client has no template editor. Copy only its preview idea: show the merged page in an iframe `srcDoc`. Do not bake rows into the page, and do not load Tailwind from a CDN.

The report template in this repo is the closer storage pattern: HTML in `NVARCHAR(MAX)`, sample JSON, and the same HTML sent on to a converter. Report tokens are a flat dictionary. This form tree is nested, so the binder is Scriban.

Each layout has one built-in default template, owned by code, not copied per company. Its body is the Scriban document described above. The default is read-only in the editor.

New is not a blank page. The code copies that layout's current default body into a company row. The user edits the copy. Restore copies the current code default again, not the body from the day the copy was made. There is no empty template.

The user edits two fragments: the document shell, and one card or one row. The engine repeats the fragment, fills `cardRows` and `cardsPerRow`, and writes `page-break-after` into the merged HTML. Save compiles Scriban and refuses the save when the shell or the fragment placeholder is missing. The error names the missing piece.

Scriban receives only the document data: title, fields, sections, columns, rows, cards. No `include`. No arbitrary method calls. Values are HTML-encoded. `<script>`, event-handler attributes, and `javascript:` URLs are rejected.

Tailwind classes may be added on a user copy. Save compiles the used classes with the AppReact Tailwind CLI into `PrintCss`. The class list is passed as data to the compiler, not interpolated into a shell command. Preview and PDF embed `PrintCss`. They do not request `cdn.tailwindcss.com`. The default template does not need Tailwind. A copy that uses `flex`, `grid`, or `object-fit` changes the Playwright PDF only. LibreOffice Word and ShapeCrawler slides keep the subset described above.

The editor shows the copy on the left and a virtual-data preview on the right. The preview iframe is `srcDoc` with a sandbox that does not allow scripts. `SampleJson` is stored on the template, starts as the Sales Order fixture in `FormDocumentTests`, and may be edited. A card sample includes one small image.

Columns: `TemplateId`, `Name`, `Layout`, `HtmlBody`, `PrintCss`, `SampleJson`, `IsBuiltIn`, `CopiedFromTemplateId`. `IsBuiltIn` rows are global and code-owned. Copies are company rows.

When copies exist, `export_html`, `export_pdf`, `export_docx`, and `export_pptx` take `template`, a template name. Omitted means the built-in default for `layout`. If the template's layout is not the call's layout, the tool returns an error and writes no file. Excel and CSV ignore `template`. `convert_docx_to_pdf` ignores `template` because its input is the Word file.

## Style table

The built-in default template uses the V1 style table. Font is Arial. These values are inline styles on the `data-role` nodes so PowerPoint can copy them.

| Level | Size | Weight | Align | Color | Extra |
|---|---|---|---|---|---|
| Document title | 18pt | bold | center | `#1a1a1a` | one blank row after |
| Section | 13pt | bold | left | `#1a1a1a` on `#f0f2f4` | blank row before |
| Nested section | 12pt | bold | left | `#1a1a1a` | blank row before |
| Field label | 10pt | bold | left | `#555555` | |
| Field value | 10pt | normal | left | `#1a1a1a` | |
| Table header | 10pt | bold | left | `#ffffff` on `#3d4f5f` | repeat on PDF pages |
| Table cell | 10pt | normal | left | `#1a1a1a`, border `#dddddd` | numbers align right |
| Notes | 10pt | normal | left | `#333333` | blank row before |

Excel title text stays 13pt bold, centered. Excel and CSV do not read the template. Field rows hold two pairs: label, value, label, value.

## Images and charts

Excel anchors a picture on the cell. PowerPoint places a picture on the card. HTML, the default template, the Playwright PDF, and the LibreOffice Word path use a data-URI `<img>` when the bytes were loaded. A picture that cannot be read, or that LibreOffice drops, is listed in `missingImages`. The file is still written.

A chart on screen stays the chat widget. Export writes the chart's data as a `table` layout.

## Library

| | |
|---|---|
| LibraryKey | `platform-document-export` |
| Display name | Platform Document Export |
| Class | `App.BL.AIAgent.GenericAgent.Plugins.AgentDocumentPlugin` |
| ToolType | `BuiltIn` |
| Subscribe | Not automatic. Any one enabled tool in this library opens the Files tab. |

| Tool | Role |
|---|---|
| `export_html` / `read_html` | Merged template HTML. Email body has no `<style>` block. Read round-trips the built-in layout by `data-role`. |
| `export_csv` / `read_csv` | UTF-8 with BOM, RFC 4180. No template. |
| `export_excel` / `read_excel` | Fixed style table. Read `.xls` via GemBox. No template. |
| `export_docx` / `read_docx` | LibreOffice HTML to docx. Read is LibreOffice docx to HTML, then `data-role`. |
| `export_pdf` | Playwright on the merged HTML. |
| `convert_docx_to_pdf` | LibreOffice converts an existing session `.docx` to PDF. |
| `read_pdf` | Plain text via PdfPig. |
| `export_pptx` / `read_pptx` | ShapeCrawler. Styles from template `data-role`. Read still reads shapes. |

Export returns `{ ok, relativePath, rowCount, bytes, engine, template }`. `engine` is `playwright`, `libreoffice`, or `shapecrawler`. In this slice `template` is the code default name. `convert_docx_to_pdf` also names the source Word file. Read returns the tree, with data rows previewed to 500. Failures are `{ ok: false, error }`.

## Checks

- HTML, PDF, Word, and PowerPoint with no user copy use the built-in default for that layout. Excel and CSV use the fixed style table.
- Default template render matches the V1 table layout: centered 18pt title, two fields on a row, section above its grid, and the `data-role` markers.
- Email HTML has no `<style>` block. Preview HTML and the Playwright PDF include `PrintCss`.
- A short `content` that matches one cache writes every cached row. A multi-section form that does not match is left as given. Two matching datasets and no `dataset_name` write no file. `rowCount` equals the rows in the file.
- Unit tests assert the merged HTML and `data-role` markers. They do not launch Chromium.
- LibreOffice tests return early when `soffice` is missing. A present `soffice` produces a docx, and `read_docx` returns the built-in tree. A timeout or non-zero exit writes no file, and the temp profile is gone.
- `read_pdf` uses PdfPig on a small file.
- `convert_docx_to_pdf` converts that docx and does not call Playwright.
- 50001 data rows: error, no partial file.
- Tool result has no file body. `../` is rejected.
- Existing GemBox write tests in `FormDocumentTests` are not the export path.

## Code

```
JSON or dataset_name → full cache when the row rule says so → FormDocument
  ├─ code default Scriban → merged HTML with data-role
  │    ├─ export_html     inline styles only
  │    ├─ preview / pdf   same body plus PrintCss → RenderHtmlToPdfAsync
  │    ├─ export_docx     soffice HTML → docx
  │    └─ export_pptx     data-role styles → PptxForm / ShapeCrawler
  ├─ convert_docx_to_pdf  soffice docx → pdf
  ├─ read_docx            soffice docx → HTML → HtmlForm.Read
  ├─ read_pdf             PdfPig
  ├─ ExcelForm            Open XML, fixed style table
  └─ CsvForm
```

Calls enter `APP.BL/AIAgent/GenericAgent/Plugins/AgentDocumentPlugin.cs`. Files are saved by `GenericAgentFileBL`. The tree is `FormDocument.cs`. Cache reads go through `AgentDataAnalyzeCacheBL`.

PowerPoint uses ShapeCrawler on `DocumentFormat.OpenXml`. Do not add Aspose.Slides, Syncfusion, or Spire. Do not add ClosedXML, EPPlus, NPOI, QuestPDF, iText, Aspose, or Pico.css. Do not rewrite the report engine. Do not remove the GemBox packages.

| Format | Write | Read |
|---|---|---|
| HTML | Merged template, no `<style>` block on email | AngleSharp `data-role`, built-in layout |
| CSV | Our code | Our code |
| Excel `.xlsx` | DocumentFormat.OpenXml | DocumentFormat.OpenXml |
| Excel `.xls` | — | GemBox.Spreadsheet |
| Word | LibreOffice HTML to docx | LibreOffice to HTML, then `data-role` |
| PDF from data | Playwright | PdfPig |
| PDF from Word | LibreOffice `convert_docx_to_pdf` | PdfPig |
| PowerPoint | ShapeCrawler, styles from `data-role` | ShapeCrawler |

## Out of scope

SQL inside the tool. Pivot, merged input cells, and logos. Replacing the report engine. Changing `file_write`. Copying a chat widget or a chart screenshot into the file. Making LibreOffice or ShapeCrawler reproduce Tailwind `flex` or `grid`. Pixel-identical Word and Playwright PDF. A template table, company copies, and a template editor in this slice. GemBox as an export engine. An Open XML style-table Word writer.
