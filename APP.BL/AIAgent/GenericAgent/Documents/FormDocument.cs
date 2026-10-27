using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    public sealed class FormDocument
    {
        public string Title { get; set; } = "";
        public List<FormField> Fields { get; set; } = new List<FormField>();
        public List<FormSection> Sections { get; set; } = new List<FormSection>();
        public string Notes { get; set; } = "";

        public int DataRowCount()
        {
            var n = 0;
            foreach (var section in Sections)
                n += section.DataRowCount();
            return n;
        }

        public int ColumnSpan()
        {
            var n = 4;
            foreach (var section in Sections)
                n = Math.Max(n, section.ColumnSpan());
            return n;
        }

        public static FormDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("content is empty.");
            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("content is not JSON. " + ex.Message);
            }
            if (token is not JObject obj)
                throw new InvalidOperationException("content must be a JSON object.");
            var doc = ParseObject(obj);
            if (string.IsNullOrWhiteSpace(doc.Title)
                && doc.Fields.Count == 0
                && doc.Sections.Count == 0
                && string.IsNullOrWhiteSpace(doc.Notes))
                throw new InvalidOperationException("content has no title, fields, sections, or notes.");
            if (doc.DataRowCount() > DocumentStyles.MaxDataRows)
                throw new InvalidOperationException("Row limit is " + DocumentStyles.MaxDataRows + ".");
            return doc;
        }

        public static FormDocument FromTable(string title, IList<string> columns, IList<List<FormCell>> rows)
        {
            var doc = new FormDocument
            {
                Title = title ?? "",
                Sections =
                {
                    new FormSection
                    {
                        Title = "",
                        Level = 2,
                        Tables =
                        {
                            new FormTable
                            {
                                Columns = columns?.ToList() ?? new List<string>(),
                                Rows = rows?.Select(r => r.ToList()).ToList() ?? new List<List<FormCell>>()
                            }
                        }
                    }
                }
            };
            if (doc.DataRowCount() > DocumentStyles.MaxDataRows)
                throw new InvalidOperationException("Row limit is " + DocumentStyles.MaxDataRows + ".");
            return doc;
        }

        /// <summary>
        /// Keep only named grid columns (AI polish bindings.columns). Order follows <paramref name="keepColumns"/>.
        /// </summary>
        public FormDocument ProjectColumns(IList<string> keepColumns)
        {
            if (keepColumns == null || keepColumns.Count == 0) return this;
            FormTable table = null;
            foreach (var sec in Sections)
            {
                if (sec?.Tables == null) continue;
                foreach (var t in sec.Tables)
                {
                    if (t != null) { table = t; break; }
                }
                if (table != null) break;
            }
            if (table?.Columns == null || table.Columns.Count == 0) return this;

            var indexes = new List<int>();
            foreach (var name in keepColumns)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    if (string.Equals(table.Columns[i], name, StringComparison.OrdinalIgnoreCase)
                        && !indexes.Contains(i))
                    {
                        indexes.Add(i);
                        break;
                    }
                }
            }
            if (indexes.Count == 0 || indexes.Count == table.Columns.Count) return this;

            var newCols = indexes.Select(i => table.Columns[i]).ToList();
            List<string> newHeaders = null;
            List<string> newFormats = null;
            if (table.DisplayHeaders != null && table.DisplayHeaders.Count == table.Columns.Count)
                newHeaders = indexes.Select(i => table.DisplayHeaders[i]).ToList();
            if (table.Formats != null && table.Formats.Count == table.Columns.Count)
                newFormats = indexes.Select(i => table.Formats[i]).ToList();
            var newRows = new List<List<FormCell>>();
            if (table.Rows != null)
            {
                foreach (var row in table.Rows)
                {
                    var cells = new List<FormCell>();
                    foreach (var i in indexes)
                        cells.Add(i < (row?.Count ?? 0) ? row[i] : new FormCell());
                    newRows.Add(cells);
                }
            }
            var projected = FromTable(Title, newCols, newRows);
            var dest = FirstGridTable(projected);
            if (dest != null)
            {
                dest.DisplayHeaders = newHeaders;
                dest.Formats = newFormats;
            }
            return projected;
        }

        /// <summary>
        /// Apply LLM short headers + cell formats onto the first grid (field keys stay in Columns).
        /// </summary>
        public void ApplyColumnPolish(IList<AiPolishColumnBinding> polishColumns)
        {
            if (polishColumns == null || polishColumns.Count == 0) return;
            var table = FirstGridTable(this);
            if (table?.Columns == null || table.Columns.Count == 0) return;

            var headers = new List<string>(table.Columns.Count);
            var formats = new List<string>(table.Columns.Count);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var field = table.Columns[i];
                var hit = polishColumns.FirstOrDefault(c =>
                    c != null && string.Equals(c.Field, field, StringComparison.OrdinalIgnoreCase));
                headers.Add(!string.IsNullOrWhiteSpace(hit?.Header) ? hit.Header.Trim() : field);
                formats.Add(AiPolishedRenderSpec.NormalizeColumnFormat(hit?.Format));
            }
            table.DisplayHeaders = headers;
            table.Formats = formats;
        }

        private static FormTable FirstGridTable(FormDocument doc)
        {
            if (doc?.Sections == null) return null;
            foreach (var sec in doc.Sections)
            {
                if (sec?.Tables == null) continue;
                foreach (var t in sec.Tables)
                {
                    if (t?.Columns != null && t.Columns.Count > 0)
                        return t;
                }
            }
            return null;
        }

        /// <summary>Column count of the first grid table, or 0.</summary>
        public int FirstGridColumnCount()
        {
            foreach (var sec in Sections)
            {
                if (sec?.Tables == null) continue;
                foreach (var t in sec.Tables)
                {
                    if (t?.Columns != null && t.Columns.Count > 0)
                        return t.Columns.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// Copy with each grid limited to <paramref name="maxRows"/> data rows.
        /// </summary>
        public FormDocument Preview(int maxRows, out int previewRowCount, out bool truncated)
        {
            var left = maxRows;
            var copy = new FormDocument { Title = Title, Notes = Notes, Fields = Fields.ToList() };
            previewRowCount = 0;
            foreach (var section in Sections)
            {
                var next = section.Preview(ref left, ref previewRowCount);
                copy.Sections.Add(next);
            }
            truncated = previewRowCount < DataRowCount();
            return copy;
        }

        private static FormDocument ParseObject(JObject obj)
        {
            var doc = new FormDocument
            {
                Title = StringOf(obj["title"]),
                Notes = StringOf(obj["notes"])
            };
            doc.Fields = ParseFields(obj["fields"]);
            doc.Sections = ParseSections(obj["sections"], 2);
            if (obj["tables"] is JArray rootTables)
            {
                var holder = new FormSection { Title = "", Level = 2, Tables = ParseTables(rootTables) };
                if (holder.Tables.Count > 0)
                    doc.Sections.Insert(0, holder);
            }
            if (obj["columns"] != null || obj["rows"] != null)
            {
                var table = ParseTable(obj);
                doc.Sections.Add(new FormSection { Title = "", Level = 2, Tables = { table } });
            }
            return doc;
        }

        private static List<FormSection> ParseSections(JToken token, int level)
        {
            var list = new List<FormSection>();
            if (token is not JArray arr) return list;
            foreach (var item in arr.OfType<JObject>())
            {
                var renderLevel = Math.Min(level, DocumentStyles.MaxSectionLevel);
                var section = new FormSection
                {
                    Title = StringOf(item["title"]),
                    Level = renderLevel,
                    Fields = ParseFields(item["fields"]),
                    Tables = item["tables"] is JArray tables ? ParseTables(tables) : new List<FormTable>(),
                    Sections = ParseSections(item["sections"], level + 1)
                };
                if (item["columns"] != null || item["rows"] != null)
                    section.Tables.Add(ParseTable(item));
                list.Add(section);
            }
            return list;
        }

        private static List<FormTable> ParseTables(JArray arr)
        {
            var list = new List<FormTable>();
            foreach (var item in arr.OfType<JObject>())
                list.Add(ParseTable(item));
            return list;
        }

        private static FormTable ParseTable(JObject obj)
        {
            var rowsToken = obj["rows"] as JArray ?? new JArray();
            List<string> columns;
            if (obj["columns"] is JArray colArr)
                columns = colArr.Select(c => c?.ToString() ?? "").ToList();
            else if (rowsToken.First is JObject firstObj)
                columns = firstObj.Properties().Select(p => p.Name).ToList();
            else
                columns = new List<string>();

            if (columns.Count > DocumentStyles.MaxColumns)
                throw new InvalidOperationException("Column limit is " + DocumentStyles.MaxColumns + ".");

            var table = new FormTable { Title = StringOf(obj["title"]), Columns = columns };
            foreach (var rowToken in rowsToken)
            {
                var row = new List<FormCell>();
                if (rowToken is JArray cells)
                {
                    for (var i = 0; i < columns.Count; i++)
                    {
                        var colName = i < columns.Count ? columns[i] : null;
                        row.Add(i < cells.Count ? CellOf(cells[i], colName) : new FormCell());
                    }
                }
                else if (rowToken is JObject map)
                {
                    foreach (var col in columns)
                        row.Add(CellOf(map[col], col));
                }
                else
                {
                    continue;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private static List<FormField> ParseFields(JToken token)
        {
            var list = new List<FormField>();
            if (token is not JArray arr) return list;
            foreach (var item in arr.OfType<JObject>())
            {
                var label = StringOf(item["label"]);
                var cell = CellOf(item["value"], label);
                list.Add(new FormField
                {
                    Label = label,
                    Value = cell.Text,
                    IsNumber = cell.IsNumber
                });
            }
            return list;
        }

        private static FormCell CellOf(JToken token, string columnName = null)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return new FormCell();
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                var numText = ((JValue)token).ToString(CultureInfo.InvariantCulture);
                return new FormCell
                {
                    Text = numText,
                    IsNumber = !CardSheet.IsImageValue(numText, columnName),
                    IsImage = CardSheet.IsImageValue(numText, columnName)
                };
            }
            if (token.Type == JTokenType.Boolean)
                return new FormCell { Text = token.Value<bool>() ? "true" : "false" };
            if (token.Type == JTokenType.Date)
                return new FormCell { Text = token.Value<DateTime>().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
            if (token.Type == JTokenType.Object)
            {
                var o = (JObject)token;
                var url = o["url"]?.ToString() ?? o["Url"]?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                    return new FormCell { Text = url, IsImage = true };
                var fileId = o["fileId"]?.ToString() ?? o["FileId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(fileId))
                    return new FormCell { Text = fileId, IsImage = true };
                return new FormCell { Text = token.ToString(Newtonsoft.Json.Formatting.None) };
            }
            if (token.Type == JTokenType.Array)
                return new FormCell { Text = token.ToString(Newtonsoft.Json.Formatting.None) };
            var text = token.ToString();
            // Column/field name + URL (or known app/file/extension URL) → image cell for print/export.
            return new FormCell { Text = text, IsImage = CardSheet.IsImageValue(text, columnName) };
        }

        private static string StringOf(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";
            return token.ToString();
        }
    }

    public sealed class FormSection
    {
        public string Title { get; set; } = "";
        public int Level { get; set; } = 2;
        public List<FormField> Fields { get; set; } = new List<FormField>();
        public List<FormTable> Tables { get; set; } = new List<FormTable>();
        public List<FormSection> Sections { get; set; } = new List<FormSection>();

        public int DataRowCount()
        {
            var n = Tables.Sum(t => t.Rows.Count);
            foreach (var child in Sections)
                n += child.DataRowCount();
            return n;
        }

        public int ColumnSpan()
        {
            var n = 4;
            foreach (var table in Tables)
                n = Math.Max(n, Math.Max(table.Columns.Count, 1));
            foreach (var child in Sections)
                n = Math.Max(n, child.ColumnSpan());
            return n;
        }

        public FormSection Preview(ref int left, ref int previewRowCount)
        {
            var copy = new FormSection
            {
                Title = Title,
                Level = Level,
                Fields = Fields.ToList()
            };
            foreach (var table in Tables)
            {
                var take = Math.Min(left, table.Rows.Count);
                left -= take;
                previewRowCount += take;
                copy.Tables.Add(new FormTable
                {
                    Title = table.Title,
                    Columns = table.Columns.ToList(),
                    DisplayHeaders = table.DisplayHeaders?.ToList(),
                    Formats = table.Formats?.ToList(),
                    Rows = table.Rows.Take(take).Select(r => r.ToList()).ToList()
                });
            }
            foreach (var child in Sections)
                copy.Sections.Add(child.Preview(ref left, ref previewRowCount));
            return copy;
        }
    }

    public sealed class FormTable
    {
        public string Title { get; set; } = "";
        /// <summary>Field / binding keys (stable).</summary>
        public List<string> Columns { get; set; } = new List<string>();
        /// <summary>Optional LLM short headers parallel to Columns; null → use Columns.</summary>
        public List<string> DisplayHeaders { get; set; }
        /// <summary>Optional cell formats parallel to Columns (delta|percent|date|…).</summary>
        public List<string> Formats { get; set; }
        public List<List<FormCell>> Rows { get; set; } = new List<List<FormCell>>();
    }

    public sealed class FormField
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public bool IsNumber { get; set; }
    }

    public sealed class FormCell
    {
        public string Text { get; set; } = "";
        public bool IsNumber { get; set; }
        /// <summary>When true (or Text looks like an image URL), HTML grid writes an img tag.</summary>
        public bool IsImage { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
