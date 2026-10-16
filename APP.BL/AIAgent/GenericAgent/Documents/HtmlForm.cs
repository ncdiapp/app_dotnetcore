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
                    var css = cell.IsNumber ? DocumentStyles.NumberCss : DocumentStyles.CellCss;
                    sb.Append("<td");
                    if (cell.IsNumber) sb.Append(" data-num=\"1\"");
                    sb.Append(" style=\"").Append(css).Append("\">");
                    sb.Append(WebUtility.HtmlEncode(cell.Text ?? ""));
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></td></tr>");
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

        public static string WriteCards(CardSheet sheet)
        {
            var perRow = CardSheet.ClampPerRow(sheet.CardsPerRow);
            var imageWidth = perRow <= 1 ? 320 : perRow == 2 ? 220 : 160;
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>");
            sb.Append("<body style=\"margin:0;padding:0;background-color:#f4f6f8;\">");
            sb.Append("<table data-doc=\"cards\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" ");
            sb.Append("style=\"width:100%;border-collapse:separate;border-spacing:12px 0;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;\">");
            if (!string.IsNullOrEmpty(sheet.Title))
            {
                sb.Append("<tr><td colspan=\"");
                sb.Append(perRow.ToString(CultureInfo.InvariantCulture));
                sb.Append("\" align=\"center\" style=\"padding:18px 16px 6px 16px;font-family:Arial,Helvetica,sans-serif;font-size:20px;line-height:26px;font-weight:bold;color:#111827;\">");
                sb.Append(WebUtility.HtmlEncode(sheet.Title));
                sb.Append("</td></tr>");
            }
            if (sheet.Cards.Count == 0)
            {
                sb.Append("<tr><td style=\"padding:16px;font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#6b7280;\">No records.</td></tr>");
            }
            for (var i = 0; i < sheet.Cards.Count; i += perRow)
            {
                var slice = new List<List<CardField>>();
                for (var c = 0; c < perRow; c++)
                    slice.Add(i + c < sheet.Cards.Count ? sheet.Cards[i + c] : null);
                var images = new List<List<CardField>>();
                var texts = new List<List<CardField>>();
                var imageCount = 0;
                var textCount = 0;
                foreach (var card in slice)
                {
                    var pics = new List<CardField>();
                    var lines = new List<CardField>();
                    if (card != null)
                    {
                        foreach (var field in card)
                        {
                            if (field.IsImage || (field.ImageBytes != null && field.ImageBytes.Length > 0))
                                pics.Add(field);
                            else
                                lines.Add(field);
                        }
                    }
                    images.Add(pics);
                    texts.Add(lines);
                    imageCount = Math.Max(imageCount, pics.Count);
                    textCount = Math.Max(textCount, lines.Count);
                }
                var last = imageCount + textCount - 1;
                var rowIndex = 0;
                for (var p = 0; p < imageCount; p++)
                {
                    sb.Append("<tr>");
                    for (var c = 0; c < perRow; c++)
                    {
                        if (slice[c] == null)
                            continue;
                        OpenCardCell(sb, perRow, rowIndex == 0, rowIndex == last);
                        var field = p < images[c].Count ? images[c][p] : null;
                        sb.Append("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;border-collapse:collapse;\"><tr><td style=\"padding:12px 14px 4px 14px;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:14px;color:#6b7280;\">");
                        sb.Append(WebUtility.HtmlEncode(field == null ? "" : PrettyLabel(field.Label)));
                        sb.Append("</td></tr></table>");
                        sb.Append("<table width=\"100%\" height=\"180\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"height:180px;border-collapse:collapse;background-color:#f3f4f6;\"><tr><td align=\"center\" valign=\"middle\" height=\"180\" bgcolor=\"#f3f4f6\" style=\"height:180px;background-color:#f3f4f6;text-align:center;vertical-align:middle;\">");
                        if (field != null)
                            AppendCardImage(sb, field, imageWidth);
                        sb.Append("</td></tr></table>");
                        sb.Append("</td>");
                    }
                    sb.Append("</tr>");
                    rowIndex++;
                }
                for (var t = 0; t < textCount; t++)
                {
                    sb.Append("<tr>");
                    for (var c = 0; c < perRow; c++)
                    {
                        if (slice[c] == null)
                            continue;
                        OpenCardCell(sb, perRow, rowIndex == 0, rowIndex == last);
                        sb.Append("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;border-collapse:collapse;\"><tr>");
                        var field = t < texts[c].Count ? texts[c][t] : null;
                        var value = field == null || string.IsNullOrWhiteSpace(field.Value) ? "\u2014" : field.Value;
                        sb.Append("<td valign=\"top\" width=\"108\" height=\"36\" style=\"width:108px;height:36px;padding:6px 12px 6px 14px;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:16px;color:#6b7280;vertical-align:top;\">");
                        sb.Append(WebUtility.HtmlEncode(field == null ? "" : PrettyLabel(field.Label)));
                        sb.Append("</td><td valign=\"top\" height=\"36\" style=\"height:36px;padding:6px 14px 6px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:18px;color:#111827;vertical-align:top;\">");
                        sb.Append(WebUtility.HtmlEncode(value));
                        sb.Append("</td></tr></table></td>");
                    }
                    sb.Append("</tr>");
                    rowIndex++;
                }
                if (imageCount + textCount > 0)
                    sb.Append("<tr><td colspan=\"").Append(perRow.ToString(CultureInfo.InvariantCulture)).Append("\" height=\"12\" style=\"height:12px;font-size:0;line-height:0;\">&nbsp;</td></tr>");
            }
            sb.Append("</table></body></html>");
            return sb.ToString();
        }

        private static void OpenCardCell(StringBuilder sb, int perRow, bool first, bool last)
        {
            sb.Append("<td valign=\"top\" width=\"");
            sb.Append((100 / perRow).ToString(CultureInfo.InvariantCulture));
            sb.Append("%\" bgcolor=\"#ffffff\" style=\"width:");
            sb.Append((100 / perRow).ToString(CultureInfo.InvariantCulture));
            sb.Append("%;background-color:#ffffff;border-left:1px solid #e6e8eb;border-right:1px solid #e6e8eb;");
            if (first)
                sb.Append("border-top:1px solid #e6e8eb;");
            if (last)
                sb.Append("border-bottom:1px solid #e6e8eb;");
            sb.Append("vertical-align:top;\">");
        }

        private static void AppendCardImage(StringBuilder sb, CardField field, int imageWidth)
        {
            string src = null;
            if (field.ImageBytes != null && field.ImageBytes.Length > 0)
                src = "data:" + CardImageMime(field.ImageBytes) + ";base64," + Convert.ToBase64String(field.ImageBytes);
            else if (CardSheet.IsImageValue(field.Value))
                src = field.Value;
            if (string.IsNullOrEmpty(src))
            {
                sb.Append("<span style=\"font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#9ca3af;\">\u2014</span>");
                return;
            }
            sb.Append("<img src=\"");
            sb.Append(WebUtility.HtmlEncode(src));
            sb.Append("\" alt=\"");
            sb.Append(WebUtility.HtmlEncode(PrettyLabel(field.Label)));
            sb.Append("\" width=\"");
            sb.Append(imageWidth.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" height=\"148\" style=\"border:0;width:");
            sb.Append(imageWidth.ToString(CultureInfo.InvariantCulture));
            sb.Append("px;height:148px;object-fit:contain;\" />");
        }

        private static string PrettyLabel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            var parts = name.Trim().Replace('-', ' ').Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                if (part.Length == 1)
                    sb.Append(char.ToUpperInvariant(part[0]));
                else
                    sb.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
            }
            return sb.ToString();
        }

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
