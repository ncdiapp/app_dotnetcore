using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Values only. A one-cell row is a title or section name, then a grid.
    /// </summary>
    public static class CsvForm
    {
        public static byte[] Write(FormDocument doc)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(doc.Title))
                sb.AppendLine(Escape(doc.Title));
            WriteFields(sb, doc.Fields);
            foreach (var section in doc.Sections)
                WriteSection(sb, section);
            if (!string.IsNullOrEmpty(doc.Notes))
            {
                sb.AppendLine(Escape("Notes"));
                sb.AppendLine(Escape(doc.Notes));
            }
            var preamble = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
            return bytes;
        }

        public static FormDocument Read(byte[] bytes)
        {
            var text = Decode(bytes);
            var rows = Parse(text);
            var doc = new FormDocument();
            var i = 0;
            if (i < rows.Count && rows[i].Count == 1 && !IsLabelHeader(rows[i]))
            {
                doc.Title = rows[i][0];
                i++;
            }
            FormSection current = null;
            while (i < rows.Count)
            {
                var row = rows[i];
                if (row.Count == 0) { i++; continue; }
                if (row.Count == 1 && string.Equals(row[0], "Notes", StringComparison.Ordinal) && i + 1 < rows.Count)
                {
                    doc.Notes = rows[i + 1].Count > 0 ? rows[i + 1][0] : "";
                    break;
                }
                if (IsLabelHeader(row))
                {
                    i++;
                    var fields = current == null ? doc.Fields : current.Fields;
                    while (i < rows.Count && rows[i].Count >= 2 && !IsLabelHeader(rows[i]) && rows[i].Count != 1)
                    {
                        fields.Add(new FormField { Label = rows[i][0], Value = rows[i][1] });
                        i++;
                    }
                    continue;
                }
                if (row.Count == 1)
                {
                    current = new FormSection { Title = row[0], Level = current == null ? 2 : 3 };
                    doc.Sections.Add(current);
                    i++;
                    continue;
                }
                var section = current ?? new FormSection { Level = 2 };
                if (current == null)
                {
                    current = section;
                    doc.Sections.Add(section);
                }
                var table = new FormTable { Columns = new List<string>(row) };
                i++;
                while (i < rows.Count && rows[i].Count > 1)
                {
                    var cells = new List<FormCell>();
                    for (var c = 0; c < table.Columns.Count; c++)
                    {
                        var textCell = c < rows[i].Count ? rows[i][c] : "";
                        cells.Add(new FormCell
                        {
                            Text = textCell,
                            IsNumber = IsNumber(textCell)
                        });
                    }
                    table.Rows.Add(cells);
                    i++;
                }
                section.Tables.Add(table);
            }
            return doc;
        }

        private static void WriteSection(StringBuilder sb, FormSection section)
        {
            if (!string.IsNullOrEmpty(section.Title))
                sb.AppendLine(Escape(section.Title));
            WriteFields(sb, section.Fields);
            foreach (var table in section.Tables)
            {
                if (!string.IsNullOrEmpty(table.Title))
                    sb.AppendLine(Escape(table.Title));
                sb.AppendLine(string.Join(",", table.Columns.ConvertAll(Escape)));
                foreach (var row in table.Rows)
                {
                    var parts = new List<string>();
                    for (var c = 0; c < table.Columns.Count; c++)
                        parts.Add(Escape(c < row.Count ? row[c].Text : ""));
                    sb.AppendLine(string.Join(",", parts));
                }
            }
            foreach (var child in section.Sections)
                WriteSection(sb, child);
        }

        private static void WriteFields(StringBuilder sb, List<FormField> fields)
        {
            if (fields == null || fields.Count == 0) return;
            sb.AppendLine("Label,Value");
            foreach (var field in fields)
                sb.AppendLine(Escape(field.Label) + "," + Escape(field.Value));
        }

        private static bool IsLabelHeader(List<string> row)
        {
            return row.Count >= 2
                && string.Equals(row[0], "Label", StringComparison.Ordinal)
                && string.Equals(row[1], "Value", StringComparison.Ordinal);
        }

        private static bool IsNumber(string text)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        public static string Escape(string value)
        {
            var s = value ?? "";
            var quote = s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!quote) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static string Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            return Encoding.UTF8.GetString(bytes);
        }

        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < (text ?? "").Length; i++)
            {
                var ch = text[i];
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else quoted = false;
                    }
                    else cell.Append(ch);
                    continue;
                }
                if (ch == '"') { quoted = true; continue; }
                if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); continue; }
                if (ch == '\r') continue;
                if (ch == '\n')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    if (!(row.Count == 1 && row[0].Length == 0))
                        rows.Add(row);
                    row = new List<string>();
                    continue;
                }
                cell.Append(ch);
            }
            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }
            return rows;
        }
    }
}
