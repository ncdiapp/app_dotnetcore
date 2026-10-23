using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using AngleSharp.Html.Parser;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Email-safe HTML: tables and inline styles only. No flex, grid, or stylesheet block.
    /// </summary>
    public static class HtmlForm
    {
        public static string Write(FormDocument doc)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>");
            sb.Append("<table data-doc=\"form\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ");
            sb.Append("style=\"width:100%;border-collapse:collapse;font-family:Arial,Helvetica,sans-serif;\">");
            if (!string.IsNullOrEmpty(doc.Title))
            {
                CellRow(sb, "title", 1, DocumentStyles.TitleCss, doc.Title);
                Blank(sb);
            }
            Fields(sb, doc.Fields);
            foreach (var section in doc.Sections)
                Section(sb, section);
            if (!string.IsNullOrEmpty(doc.Notes))
            {
                Blank(sb);
                CellRow(sb, "notes", 0, DocumentStyles.NotesCss, doc.Notes);
            }
            sb.Append("</table></body></html>");
            return sb.ToString();
        }

        public static FormDocument Read(string html)
        {
            var parser = new HtmlParser();
            var page = parser.ParseDocument(html ?? "");
            var root = page.QuerySelector("table[data-doc=form]");
            if (root == null)
                throw new System.InvalidOperationException("HTML has no form table.");

            var doc = new FormDocument();
            FormSection current = null;
            FormSection parent = null;
            foreach (var tr in DirectRows(root))
            {
                var td = tr.Children.FirstOrDefault(c => c.LocalName == "td");
                if (td == null) continue;
                var role = td.GetAttribute("data-role") ?? "";
                if (role == "blank") continue;
                if (role == "title")
                {
                    doc.Title = td.TextContent.Trim();
                    continue;
                }
                if (role == "notes")
                {
                    doc.Notes = td.TextContent.Trim();
                    continue;
                }
                if (role == "section")
                {
                    var level = 2;
                    var raw = td.GetAttribute("data-level");
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                        level = n;
                    var section = new FormSection { Title = td.TextContent.Trim(), Level = level };
                    if (level >= 3 && parent != null)
                        parent.Sections.Add(section);
                    else
                    {
                        doc.Sections.Add(section);
                        parent = section;
                    }
                    current = section;
                    continue;
                }
                var nested = td.Children.FirstOrDefault(c => c.LocalName == "table");
                if (nested == null) continue;
                var kind = nested.GetAttribute("data-role") ?? "";
                if (kind == "fields")
                {
                    var target = current == null ? doc.Fields : current.Fields;
                    ReadFields(nested, target);
                }
                else if (kind == "grid")
                {
                    var section = current ?? new FormSection { Level = 2 };
                    if (current == null)
                    {
                        current = section;
                        doc.Sections.Add(section);
                    }
                    section.Tables.Add(ReadGrid(nested));
                }
            }
            return doc;
        }

        private static void Section(StringBuilder sb, FormSection section)
        {
            if (!string.IsNullOrEmpty(section.Title))
            {
                Blank(sb);
                var css = section.Level >= 3 ? DocumentStyles.NestedCss : DocumentStyles.SectionCss;
                CellRow(sb, "section", section.Level, css, section.Title);
            }
            Fields(sb, section.Fields);
            foreach (var table in section.Tables)
                Grid(sb, table);
            foreach (var child in section.Sections)
                Section(sb, child);
        }

        private static void Fields(StringBuilder sb, List<FormField> fields)
        {
            if (fields == null || fields.Count == 0) return;
            sb.Append("<tr><td style=\"padding:0;\">");
            sb.Append("<table data-role=\"fields\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;border-collapse:collapse;\">");
            for (var i = 0; i < fields.Count; i += 2)
            {
                sb.Append("<tr>");
                Pair(sb, fields[i]);
                if (i + 1 < fields.Count)
                    Pair(sb, fields[i + 1]);
                else
                {
                    sb.Append("<td data-role=\"label\" style=\"").Append(DocumentStyles.LabelCss).Append("\"></td>");
                    sb.Append("<td data-role=\"value\" style=\"").Append(DocumentStyles.ValueCss).Append("\"></td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></td></tr>");
        }

        private static void Pair(StringBuilder sb, FormField field)
        {
            sb.Append("<td data-role=\"label\" style=\"").Append(DocumentStyles.LabelCss).Append("\">");
            sb.Append(WebUtility.HtmlEncode(field.Label ?? ""));
            sb.Append("</td><td data-role=\"value\" style=\"").Append(DocumentStyles.ValueCss).Append("\">");
            sb.Append(WebUtility.HtmlEncode(field.Value ?? ""));
            sb.Append("</td>");
        }

        private static void Grid(StringBuilder sb, FormTable table)
        {
            if (!string.IsNullOrEmpty(table.Title))
                CellRow(sb, "section", 3, DocumentStyles.NestedCss, table.Title);
            sb.Append("<tr><td style=\"padding:0;\">");
            sb.Append("<table data-role=\"grid\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;border-collapse:collapse;\">");
            sb.Append("<tr>");
            foreach (var col in table.Columns)
            {
                sb.Append("<th style=\"").Append(DocumentStyles.HeaderCss).Append("\">");
                sb.Append(WebUtility.HtmlEncode(col ?? ""));
                sb.Append("</th>");
            }
            sb.Append("</tr>");
            foreach (var row in table.Rows)
            {
                sb.Append("<tr>");
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var cell = i < row.Count ? row[i] : new FormCell();
                    var colName = i < table.Columns.Count ? table.Columns[i] : null;
                    AppendGridCell(sb, cell, colName);
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></td></tr>");
        }

        private static void AppendGridCell(StringBuilder sb, FormCell cell, string columnName = null)
        {
            cell ??= new FormCell();
            var text = cell.Text ?? "";
            var isImage = cell.IsImage
                          || (cell.ImageBytes != null && cell.ImageBytes.Length > 0)
                          || CardSheet.IsRenderableImage(text, columnName, cell.IsImage);
            var css = cell.IsNumber && !isImage ? DocumentStyles.NumberCss : DocumentStyles.CellCss;
            sb.Append("<td");
            if (cell.IsNumber && !isImage) sb.Append(" data-num=\"1\"");
            if (isImage) sb.Append(" data-role=\"image\"");
            sb.Append(" style=\"").Append(css).Append("\">");
            if (isImage)
                AppendGridImage(sb, cell, text.Trim().Trim('"', '\''));
            else
                sb.Append(WebUtility.HtmlEncode(FormatGridCellText(text)));
            sb.Append("</td>");
        }

        /// <summary>ISO date/datetime strings → short date for print/export grids.</summary>
        private static string FormatGridCellText(string text)
            => CardSheet.FormatDisplayValue(text);

        private static void AppendGridImage(StringBuilder sb, FormCell cell, string text)
        {
            string src = null;
            if (cell.ImageBytes != null && cell.ImageBytes.Length > 0)
                src = "data:" + CardImageMime(cell.ImageBytes) + ";base64," + Convert.ToBase64String(cell.ImageBytes);
            else if (!string.IsNullOrWhiteSpace(text))
                src = text.Trim();
            if (string.IsNullOrEmpty(src))
            {
                sb.Append("&nbsp;");
                return;
            }
            sb.Append("<img src=\"");
            sb.Append(WebUtility.HtmlEncode(src));
            sb.Append("\" alt=\"\" style=\"border:0;max-width:120px;max-height:80px;width:auto;height:auto;object-fit:contain;display:block;\" />");
        }

        private static void CellRow(StringBuilder sb, string role, int level, string css, string text)
        {
            sb.Append("<tr><td data-role=\"").Append(role).Append("\"");
            if (level > 0)
                sb.Append(" data-level=\"").Append(level.ToString(CultureInfo.InvariantCulture)).Append("\"");
            sb.Append(" style=\"").Append(css).Append("\">");
            sb.Append(WebUtility.HtmlEncode(text ?? ""));
            sb.Append("</td></tr>");
        }

        private static void Blank(StringBuilder sb)
        {
            sb.Append("<tr><td data-role=\"blank\" style=\"").Append(DocumentStyles.BlankCss).Append("\">&nbsp;</td></tr>");
        }

        private static System.Collections.Generic.IEnumerable<AngleSharp.Dom.IElement> DirectRows(AngleSharp.Dom.IElement table)
        {
            foreach (var child in table.Children)
            {
                if (child.LocalName == "tr")
                    yield return child;
                else if (child.LocalName == "tbody" || child.LocalName == "thead")
                {
                    foreach (var tr in child.Children)
                    {
                        if (tr.LocalName == "tr")
                            yield return tr;
                    }
                }
            }
        }

        private static void ReadFields(AngleSharp.Dom.IElement table, List<FormField> target)
        {
            foreach (var tr in DirectRows(table))
            {
                var cells = tr.Children.Where(c => c.LocalName == "td").ToList();
                for (var i = 0; i + 1 < cells.Count; i += 2)
                {
                    var label = cells[i].TextContent.Trim();
                    var value = cells[i + 1].TextContent.Trim();
                    if (label.Length == 0 && value.Length == 0) continue;
                    target.Add(new FormField { Label = label, Value = value });
                }
            }
        }

        private static FormTable ReadGrid(AngleSharp.Dom.IElement table)
        {
            var grid = new FormTable();
            var first = true;
            foreach (var tr in DirectRows(table))
            {
                if (first)
                {
                    foreach (var th in tr.Children.Where(c => c.LocalName == "th" || c.LocalName == "td"))
                        grid.Columns.Add(th.TextContent.Trim());
                    first = false;
                    continue;
                }
                var row = new List<FormCell>();
                foreach (var td in tr.Children.Where(c => c.LocalName == "td"))
                {
                    row.Add(new FormCell
                    {
                        Text = td.TextContent.Trim(),
                        IsNumber = td.GetAttribute("data-num") == "1"
                    });
                }
                if (row.Count > 0)
                    grid.Rows.Add(row);
            }
            return grid;
        }

        /// <summary>Same body as email HTML, plus print CSS for preview and Playwright PDF.</summary>
        public static string ForPrint(string html)
            => InjectStyle(html, DocumentStyles.PdfPrintCss);

        /// <summary>KPI/chart/dashboard PDF/DOCX/PPTX: landscape + grid fonts fitted to page width.</summary>
        public static string ForDashboardPrint(string html)
            => InjectStyle(html, DocumentStyles.DashboardPrintCss + UiDashboardHtml.BuildGridPageFitCss(html));

        private static string InjectStyle(string html, string css)
        {
            var page = html ?? "";
            var tag = "<style>" + css + "</style>";
            var head = page.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            if (head >= 0)
                return page.Insert(head + 6, tag);
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" + tag + "</head><body>" + page + "</body></html>";
        }

        public static string WriteCards(CardSheet sheet)
            => WriteCards(sheet, false);

        public static string WriteCards(CardSheet sheet, bool forPrint)
        {
            if (forPrint)
                return WriteFittedCards(sheet);
            var perRow = CardSheet.ClampPerRow(sheet.CardsPerRow);
            var imageWidth = perRow <= 1 ? 320 : perRow == 2 ? 220 : 160;
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>");
            sb.Append("<body style=\"margin:0;padding:12px;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;\">");
            if (!string.IsNullOrEmpty(sheet.Title))
            {
                sb.Append("<div data-role=\"title\" style=\"padding:6px 4px 14px 4px;text-align:center;font-size:20px;line-height:26px;font-weight:bold;color:#111827;\">");
                sb.Append(WebUtility.HtmlEncode(sheet.Title));
                sb.Append("</div>");
            }
            // One self-contained card per cell (not field-rows spanning the grid — that looked like zebra mini-tables).
            // max-width + table-layout keep the sheet inside the Result pane (avoids forcing chat splitter open).
            sb.Append("<div style=\"max-width:100%;overflow-x:auto;box-sizing:border-box;\">");
            sb.Append("<table data-doc=\"cards\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ");
            sb.Append("style=\"width:100%;max-width:100%;table-layout:fixed;border-collapse:separate;border-spacing:12px 12px;background-color:#f4f6f8;box-sizing:border-box;\">");
            if (sheet.Cards.Count == 0)
            {
                sb.Append("<tr><td style=\"padding:16px;font-size:13px;color:#6b7280;\">No records.</td></tr>");
            }
            var colPctScreen = (100.0 / perRow).ToString("0.####", CultureInfo.InvariantCulture) + "%";
            for (var i = 0; i < sheet.Cards.Count; i += perRow)
            {
                sb.Append("<tr>");
                for (var c = 0; c < perRow; c++)
                {
                    var idx = i + c;
                    if (idx >= sheet.Cards.Count)
                    {
                        sb.Append("<td width=\"").Append(colPctScreen)
                            .Append("\" style=\"width:").Append(colPctScreen)
                            .Append(";\"></td>");
                        continue;
                    }
                    sb.Append("<td valign=\"top\" width=\"").Append(colPctScreen)
                        .Append("\" data-role=\"card\" style=\"width:")
                        .Append(colPctScreen)
                        .Append(";max-width:").Append(colPctScreen)
                        .Append(";overflow:hidden;word-break:break-word;background:#ffffff;border:1px solid #e6e8eb;border-radius:8px;padding:12px 14px;vertical-align:top;box-sizing:border-box;\">");
                    AppendScreenCardBody(sb, sheet.Cards[idx], imageWidth);
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></div></body></html>");
            return sb.ToString();
        }

        /// <summary>All fields for one card inside a single cell (Result / screen).</summary>
        private static void AppendScreenCardBody(StringBuilder sb, List<CardField> card, int imageWidth)
        {
            if (card == null || card.Count == 0)
            {
                sb.Append("<div style=\"font-size:12px;color:#9ca3af;\">\u2014</div>");
                return;
            }
            foreach (var field in card)
            {
                if (field == null) continue;
                if (field.ImageBytes != null && field.ImageBytes.Length > 0
                    || CardSheet.IsRenderableImage(field.Value, field.Label, field.IsImage))
                {
                    sb.Append("<div style=\"margin:0 0 10px 0;\">");
                    sb.Append("<div style=\"font-size:11px;line-height:14px;color:#6b7280;margin:0 0 4px 0;\">");
                    sb.Append(WebUtility.HtmlEncode(PrettyLabel(field.Label)));
                    sb.Append("</div>");
                    sb.Append("<div style=\"background:#f3f4f6;border-radius:4px;min-height:120px;display:flex;align-items:center;justify-content:center;padding:8px;\">");
                    AppendCardImage(sb, field, imageWidth);
                    sb.Append("</div></div>");
                    continue;
                }
                var value = string.IsNullOrWhiteSpace(field.Value) ? "\u2014" : field.Value;
                sb.Append("<div style=\"display:flex;gap:8px;margin:0 0 6px 0;align-items:flex-start;\">");
                sb.Append("<div style=\"flex:0 0 108px;width:108px;font-size:11px;line-height:16px;color:#6b7280;\">");
                sb.Append(WebUtility.HtmlEncode(PrettyLabel(field.Label)));
                sb.Append("</div>");
                sb.Append("<div style=\"flex:1 1 auto;font-size:12px;line-height:16px;color:#111827;font-weight:500;\">");
                sb.Append(WebUtility.HtmlEncode(value));
                sb.Append("</div></div>");
            }
        }

        /// <summary>
        /// One printed page holds cardRows by cardsPerRow. Each card is given an equal slice of the A4 content box.
        /// </summary>
        private static string WriteFittedCards(CardSheet sheet)
        {
            var perRow = CardSheet.ClampPerRow(sheet.CardsPerRow);
            var pageRows = CardSheet.ClampPageRows(sheet.CardRows);
            var pageSize = Math.Max(1, pageRows * perRow);
            var pages = Math.Max(1, (int)Math.Ceiling(sheet.Cards.Count / (double)pageSize));
            if (sheet.Cards.Count == 0) pages = 1;
            var pageH = CardPageFit.PageHeightMm;
            // Empty title (e.g. layout=page Card section) → no title band; more room for cards.
            var titleH = string.IsNullOrEmpty(sheet.Title) ? 0 : CardPageFit.TitleMm;
            // Print/PDF: catalog on white paper (screen Result keeps grey board via CSS).
            var gutter = 1.8;
            var rowH = (pageH - titleH) / pageRows;
            // Equal column % (avoid int 100/3=33 → 99% right gutter).
            var colPct = (100.0 / perRow).ToString("0.####", CultureInfo.InvariantCulture) + "%";
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
            // width:100% fills the printable content box (browser @page margins). Fixed 180mm left a right gutter.
            sb.Append("<style>@page{size:A4 portrait;margin:10mm}");
            sb.Append("html,body{margin:0;padding:0;width:100%;max-width:100%;background:#ffffff;box-sizing:border-box}");
            sb.Append("*,*:before,*:after{box-sizing:border-box}");
            sb.Append(".card-page{width:100%;max-width:100%;height:").Append(Mm(pageH));
            sb.Append(";overflow:hidden;page-break-after:always;break-after:page;background:#ffffff;}");
            sb.Append(".card-page>table{width:100%!important;max-width:100%;table-layout:fixed}");
            sb.Append(".card-page table,.card-page tr,.card-page td{page-break-inside:auto;break-inside:auto;}");
            sb.Append(".mcp-card-shell{background:#fff;border:1px solid #e5e7eb;border-radius:6px;overflow:hidden;");
            sb.Append("height:100%;width:100%;box-sizing:border-box;}");
            sb.Append(".mcp-card-hero{background:#f3f4f6;overflow:hidden;text-align:center;vertical-align:middle;position:relative;}");
            sb.Append(".mcp-card-hero-blur{display:none!important;}");
            sb.Append(".mcp-card-hero-fg,.mcp-card-hero img{width:100%;height:100%;object-fit:cover;object-position:center center;");
            sb.Append("border:0;display:block;background:#f3f4f6;}");
            sb.Append(".mcp-card-body{overflow:hidden;background:#fff;box-sizing:border-box;}");
            sb.Append(".card-page-num{position:absolute;top:2mm;right:3mm;font-size:8px;line-height:10px;color:#9ca3af;font-weight:normal;}");
            sb.Append(".card-page:last-child{page-break-after:auto!important;break-after:auto!important}");
            sb.Append("</style></head>");
            sb.Append("<body style=\"margin:0;padding:0;width:100%;background-color:#ffffff;\">");
            var pageTitle = sheet.Title ?? "";
            for (var p = 0; p < pages; p++)
            {
                if (p > 0)
                    sb.Append("<div style=\"page-break-before:always\"></div>");
                sb.Append("<div class=\"card-page\" style=\"position:relative;width:100%;\">");
                if (pages > 1)
                {
                    sb.Append("<div class=\"card-page-num\">");
                    sb.Append((p + 1).ToString(CultureInfo.InvariantCulture));
                    sb.Append("/");
                    sb.Append(pages.ToString(CultureInfo.InvariantCulture));
                    sb.Append("</div>");
                }
                sb.Append("<table data-doc=\"cards\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;");
                sb.Append("height:");
                sb.Append(Mm(pageH));
                sb.Append(";border-collapse:separate;border-spacing:").Append(Mm(gutter)).Append(" ").Append(Mm(gutter));
                sb.Append(";table-layout:fixed;background-color:#ffffff;font-family:Arial,Helvetica,sans-serif;\">");
                if (titleH > 0)
                {
                    sb.Append("<tr style=\"height:").Append(Mm(titleH)).Append("\"><td colspan=\"").Append(perRow.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" data-role=\"title\" align=\"center\" height=\"").Append(Mm(titleH));
                    sb.Append("\" style=\"height:").Append(Mm(titleH)).Append(";padding:0 4px;font-size:14px;line-height:16px;font-weight:bold;color:#111827;vertical-align:middle;background:#ffffff;\">");
                    sb.Append(WebUtility.HtmlEncode(pageTitle));
                    sb.Append("</td></tr>");
                }
                var cellH = Math.Max(10, rowH - gutter);
                for (var r = 0; r < pageRows; r++)
                {
                    sb.Append("<tr style=\"height:").Append(Mm(rowH)).Append("\">");
                    for (var c = 0; c < perRow; c++)
                    {
                        var index = p * pageSize + r * perRow + c;
                        var card = index < sheet.Cards.Count ? sheet.Cards[index] : null;
                        // Outer td = transparent board cell; radius lives on inner .mcp-card-shell.
                        sb.Append("<td valign=\"top\" width=\"").Append(colPct);
                        sb.Append("\" height=\"").Append(Mm(cellH));
                        sb.Append("\" style=\"width:").Append(colPct);
                        sb.Append(";height:").Append(Mm(cellH));
                        sb.Append(";background:transparent;border:0;vertical-align:top;padding:0;\">");
                        sb.Append("<div class=\"mcp-card-shell\" style=\"width:100%;height:").Append(Mm(cellH)).Append(";\">");
                        if (card != null)
                            AppendFittedMcpCard(sb, card, cellH, perRow);
                        sb.Append("</div></td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table></div>");
            }
            sb.Append("</body></html>");
            return sb.ToString();
        }

        /// <summary>Print/Export card: fixed hero+body bands (A1–A4), MCP roles, rounded inner shell.</summary>
        private static void AppendFittedMcpCard(StringBuilder sb, List<CardField> card, double rowMm, int perRow)
        {
            CardField image = null;
            CardField id = null;
            CardField title = null;
            CardField status = null;
            CardField color = null;
            var metas = new List<CardField>();
            foreach (var field in card ?? new List<CardField>())
            {
                if (field == null) continue;
                var role = (field.Role ?? "").Trim().ToLowerInvariant();
                if (field.IsImage || (field.ImageBytes != null && field.ImageBytes.Length > 0)
                    || role == "primaryimage"
                    || CardSheet.IsRenderableImage(field.Value, field.Label, field.IsImage))
                {
                    // Prefer any hero with bytes or a URL (PLM /image/300 needs Label / IsImage flag).
                    if (image == null && ((field.ImageBytes != null && field.ImageBytes.Length > 0)
                        || CardSheet.IsRenderableImage(field.Value, field.Label, field.IsImage)
                        || (role == "primaryimage" && CardSheet.IsUrlValue(field.Value))))
                        image = field;
                    continue;
                }
                if (role is "idlink" or "id")
                {
                    if (id == null && !string.IsNullOrWhiteSpace(field.Value)) id = field;
                }
                else if (role == "title")
                {
                    if (title == null && !string.IsNullOrWhiteSpace(field.Value)) title = field;
                }
                else if (role is "statuspill" or "status")
                {
                    if (status == null && !string.IsNullOrWhiteSpace(field.Value)) status = field;
                }
                else if (role is "colorswatch" or "color")
                {
                    if (color == null && !string.IsNullOrWhiteSpace(field.Value)) color = field;
                }
                else if (role == "meta" && !string.IsNullOrWhiteSpace(field.Value))
                    metas.Add(field);
            }

            // Slightly smaller hero → more room for catalog text (MCP-like).
            const double heroRatio = 0.50;
            var imageBand = Math.Max(14, rowMm * heroRatio);
            var textBand = Math.Max(8, rowMm - imageBand);
            sb.Append("<table width=\"100%\" height=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;height:100%;border-collapse:collapse;table-layout:fixed;\">");
            sb.Append("<tr style=\"height:").Append(Mm(imageBand)).Append("\"><td class=\"mcp-card-hero\" align=\"center\" valign=\"middle\" height=\"");
            sb.Append(Mm(imageBand)).Append("\" style=\"height:").Append(Mm(imageBand));
            sb.Append(";max-height:").Append(Mm(imageBand));
            sb.Append(";background-color:#f3f4f6;text-align:center;vertical-align:middle;padding:0;overflow:hidden;\">");
            if (image != null)
                AppendCardImageFixedBox(sb, image, imageBand);
            sb.Append("</td></tr>");
            sb.Append("<tr style=\"height:").Append(Mm(textBand)).Append("\"><td class=\"mcp-card-body\" valign=\"top\" style=\"height:");
            sb.Append(Mm(textBand)).Append(";max-height:").Append(Mm(textBand));
            // Gap between hero image and text (MCP catalog breathing room).
            sb.Append(";padding:10px 7px 5px 7px;vertical-align:top;background:#ffffff;overflow:hidden;\">");
            if (id != null)
            {
                sb.Append("<div data-role=\"idLink\" style=\"font-size:12px;line-height:14px;font-weight:700;color:#2563eb;margin:0 0 2px 0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;\">");
                sb.Append(WebUtility.HtmlEncode(CardSheet.FormatDisplayValue(id.Value)));
                sb.Append("</div>");
            }
            if (title != null)
            {
                sb.Append("<div data-role=\"title\" style=\"font-size:11px;line-height:13px;font-weight:500;color:#111827;margin:0 0 2px 0;max-height:26px;overflow:hidden;\">");
                sb.Append(WebUtility.HtmlEncode(CardSheet.FormatDisplayValue(title.Value)));
                sb.Append("</div>");
            }
            if (color != null)
            {
                var swatch = ResolveColorSwatchHex(color.Value);
                sb.Append("<div data-role=\"colorSwatch\" style=\"font-size:9px;line-height:12px;color:#374151;margin:0 0 2px 0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;\">");
                sb.Append("<span style=\"display:inline-block;width:8px;height:8px;border-radius:999px;background:");
                sb.Append(swatch);
                sb.Append(";border:1px solid #d1d5db;margin-right:4px;vertical-align:middle;\"></span>");
                sb.Append(WebUtility.HtmlEncode(CardSheet.FormatDisplayValue(color.Value)));
                sb.Append("</div>");
            }
            if (status != null && !CardSheet.LooksLikeDateValue(status.Value))
            {
                var (bg, fg) = StatusPillColors(status.Value);
                sb.Append("<div data-role=\"statusPill\" style=\"display:inline-block;font-size:9px;line-height:11px;font-weight:600;padding:1px 6px;border-radius:999px;background:");
                sb.Append(bg);
                sb.Append(";color:");
                sb.Append(fg);
                sb.Append(";margin:1px 0;max-width:100%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;\">");
                sb.Append(WebUtility.HtmlEncode(status.Value));
                sb.Append("</div>");
            }
            else if (status != null && CardSheet.LooksLikeDateValue(status.Value))
                metas.Insert(0, status);
            foreach (var m in metas)
            {
                sb.Append("<div data-role=\"meta\" style=\"font-size:9px;line-height:11px;color:#6b7280;margin:1px 0 0 0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;\">");
                if (m.ShowLabel)
                {
                    var lab = !string.IsNullOrWhiteSpace(m.DisplayLabel) ? m.DisplayLabel : PrettyLabel(m.Label);
                    if (!string.IsNullOrWhiteSpace(lab))
                    {
                        sb.Append("<span style=\"font-weight:600;color:#9ca3af;margin-right:3px;\">");
                        sb.Append(WebUtility.HtmlEncode(lab));
                        sb.Append("</span>");
                    }
                }
                sb.Append(WebUtility.HtmlEncode(CardSheet.FormatDisplayValue(m.Value)));
                sb.Append("</div>");
            }
            sb.Append("</td></tr></table>");
            _ = perRow;
        }

        private static (string Bg, string Fg) StatusPillColors(string status)
        {
            var s = (status ?? "").Trim().ToLowerInvariant();
            if (s.IndexOf("drop", StringComparison.Ordinal) >= 0
                || s.IndexOf("reject", StringComparison.Ordinal) >= 0)
                return ("#fee2e2", "#991b1b");
            if (s.IndexOf("hold", StringComparison.Ordinal) >= 0)
                return ("#f3f4f6", "#4b5563");
            if (s.IndexOf("sampling", StringComparison.Ordinal) >= 0
                || s.IndexOf("in development", StringComparison.Ordinal) >= 0
                || s.IndexOf("indevelopment", StringComparison.Ordinal) >= 0)
                return ("#dbeafe", "#1e40af");
            if (s.IndexOf("approv", StringComparison.Ordinal) >= 0)
                return ("#dcfce7", "#166534");
            return ("#dcfce7", "#166534");
        }

        /// <summary>Hex for print color dot: #rrggbb, common name, else soft blue.</summary>
        private static string ResolveColorSwatchHex(string value)
        {
            var t = (value ?? "").Trim();
            if (t.Length >= 4 && t[0] == '#')
            {
                var hex = t.Length >= 7 ? t.Substring(0, 7) : t;
                if (System.Text.RegularExpressions.Regex.IsMatch(hex, @"^#[0-9A-Fa-f]{3,8}$"))
                    return hex.Length == 4
                        ? "#" + hex[1] + hex[1] + hex[2] + hex[2] + hex[3] + hex[3]
                        : hex.Substring(0, Math.Min(7, hex.Length));
            }
            switch (t.ToLowerInvariant())
            {
                case "black": return "#111827";
                case "white": return "#f9fafb";
                case "red": return "#ef4444";
                case "blue": return "#3b82f6";
                case "green": return "#22c55e";
                case "yellow": return "#eab308";
                case "orange": return "#f97316";
                case "purple": return "#a855f7";
                case "pink": return "#ec4899";
                case "brown": return "#92400e";
                case "grey":
                case "gray": return "#9ca3af";
                case "navy": return "#1e3a8a";
                case "beige": return "#d6c6a8";
                case "tan": return "#d2b48c";
                case "olive": return "#6b8e23";
                case "khaki": return "#c3b091";
                default: return "#60a5fa";
            }
        }

        /// <summary>Fixed hero: object-fit cover center crop (matches screen agent-cards.css).</summary>
        private static void AppendCardImageFixedBox(StringBuilder sb, CardField field, double boxHeightMm)
        {
            string src = null;
            if (field?.ImageBytes != null && field.ImageBytes.Length > 0)
                src = "data:" + CardImageMime(field.ImageBytes) + ";base64," + Convert.ToBase64String(field.ImageBytes);
            else if (CardSheet.IsRenderableImage(field?.Value, field?.Label, field?.IsImage == true)
                     || (field != null && CardSheet.IsUrlValue(field.Value)))
                src = (field.Value ?? "").Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(src)) return;
            var h = Mm(boxHeightMm);
            sb.Append("<img class=\"mcp-card-hero-fg\" src=\"");
            sb.Append(WebUtility.HtmlEncode(src));
            sb.Append("\" alt=\"\" style=\"border:0;width:100%;height:").Append(h);
            sb.Append(";max-height:").Append(h);
            sb.Append(";object-fit:cover;object-position:center center;display:block;\" />");
        }

        private static string Mm(double value)
            => value.ToString("0.##", CultureInfo.InvariantCulture) + "mm";

        private static void AppendCardImage(StringBuilder sb, CardField field, int imageWidth)
            => AppendCardImage(sb, field, imageWidth, 0);

        private static void AppendCardImage(StringBuilder sb, CardField field, int imageWidth, double maxHeightMm)
        {
            string src = null;
            if (field.ImageBytes != null && field.ImageBytes.Length > 0)
                src = "data:" + CardImageMime(field.ImageBytes) + ";base64," + Convert.ToBase64String(field.ImageBytes);
            else if (CardSheet.IsRenderableImage(field.Value, field.Label, field.IsImage)
                     || CardSheet.IsUrlValue(field.Value))
                src = (field.Value ?? "").Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(src))
                return; // blank hero — never broken-image chrome or em dash
            sb.Append("<img src=\"");
            sb.Append(WebUtility.HtmlEncode(src));
            sb.Append("\" alt=\"");
            sb.Append(WebUtility.HtmlEncode(PrettyLabel(field.Label)));
            sb.Append("\"");
            if (maxHeightMm > 0)
            {
                sb.Append(" style=\"border:0;max-width:100%;max-height:");
                sb.Append(Mm(maxHeightMm));
                sb.Append(";width:auto;height:auto;object-fit:contain;\" />");
                return;
            }
            sb.Append(" width=\"");
            sb.Append(imageWidth.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" height=\"148\" style=\"border:0;width:");
            sb.Append(imageWidth.ToString(CultureInfo.InvariantCulture));
            sb.Append("px;height:148px;object-fit:contain;\" />");
        }

        private static string PrettyLabel(string name) => CardSheet.PrettyFieldLabel(name);

        private static string CardImageMime(byte[] bytes)
        {
            if (bytes != null && bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes != null && bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
            if (bytes != null && bytes.Length >= 3 && bytes[0] == (byte)'G' && bytes[1] == (byte)'I') return "image/gif";
            if (bytes != null && bytes.Length >= 2 && bytes[0] == (byte)'B' && bytes[1] == (byte)'M') return "image/bmp";
            return "application/octet-stream";
        }
    }
}
