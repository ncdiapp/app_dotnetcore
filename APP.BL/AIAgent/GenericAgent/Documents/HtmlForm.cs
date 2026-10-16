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
    }
}
