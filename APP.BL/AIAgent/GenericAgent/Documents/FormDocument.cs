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
                        row.Add(i < cells.Count ? CellOf(cells[i]) : new FormCell());
                }
                else if (rowToken is JObject map)
                {
                    foreach (var col in columns)
                        row.Add(CellOf(map[col]));
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
                var cell = CellOf(item["value"]);
                list.Add(new FormField
                {
                    Label = StringOf(item["label"]),
                    Value = cell.Text,
                    IsNumber = cell.IsNumber
                });
            }
            return list;
        }

        private static FormCell CellOf(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return new FormCell();
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                return new FormCell
                {
                    Text = ((JValue)token).ToString(CultureInfo.InvariantCulture),
                    IsNumber = true
                };
            }
            if (token.Type == JTokenType.Boolean)
                return new FormCell { Text = token.Value<bool>() ? "true" : "false" };
            if (token.Type == JTokenType.Date)
                return new FormCell { Text = token.Value<DateTime>().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
            if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
                return new FormCell { Text = token.ToString(Newtonsoft.Json.Formatting.None) };
            return new FormCell { Text = token.ToString() };
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
        public List<string> Columns { get; set; } = new List<string>();
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
        public byte[] ImageBytes { get; set; }
    }
}
