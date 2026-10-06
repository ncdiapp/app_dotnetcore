using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.DataAnalyze
{
    /// <summary>
    /// ChatSession-scoped tabular cache for data_analyze.
    /// Keyed by ChatSessionKey + dataset name. Process-lifetime memory only.
    /// </summary>
    public static class AgentDataAnalyzeCacheBL
    {
        public const int MaxCachedRows = 5000;
        /// <summary>When extracted row count &gt;= this, replace tool result with a cache envelope.</summary>
        public const int AutoEnvelopeMinRows = 15;

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentCachedDataset>> Sessions
            = new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, int> NameCounters
            = new(StringComparer.OrdinalIgnoreCase);

        public static AgentCachedDataset Put(string chatSessionKey, string sourceTool, JArray rows)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                throw new ArgumentException("ChatSessionKey is required for data analyze cache.", nameof(chatSessionKey));

            var name = NextDatasetName(chatSessionKey, sourceTool);
            var ds = BuildDataset(name, rows, sourceTool ?? "");
            var bag = Sessions.GetOrAdd(chatSessionKey.Trim(), _ => new ConcurrentDictionary<string, AgentCachedDataset>(StringComparer.OrdinalIgnoreCase));
            bag[name] = ds;
            return ds;
        }

        public static AgentCachedDataset PutNamed(string chatSessionKey, string datasetName, JArray rows, string sourceTool = "")
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                throw new ArgumentException("ChatSessionKey is required for data analyze cache.", nameof(chatSessionKey));
            if (string.IsNullOrWhiteSpace(datasetName))
                throw new ArgumentException("datasetName is required.", nameof(datasetName));

            var name = SanitizeName(datasetName);
            var ds = BuildDataset(name, rows, sourceTool ?? "");
            var bag = Sessions.GetOrAdd(chatSessionKey.Trim(), _ => new ConcurrentDictionary<string, AgentCachedDataset>(StringComparer.OrdinalIgnoreCase));
            bag[name] = ds;
            return ds;
        }

        public static AgentCachedDataset Get(string chatSessionKey, string datasetName)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(datasetName))
                return null;
            if (!Sessions.TryGetValue(chatSessionKey.Trim(), out var bag))
                return null;
            return bag.TryGetValue(SanitizeName(datasetName), out var ds) ? ds : null;
        }

        public static IReadOnlyList<string> ListNames(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                return Array.Empty<string>();
            if (!Sessions.TryGetValue(chatSessionKey.Trim(), out var bag))
                return Array.Empty<string>();
            return bag.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Rebuild row objects (including empty/null cells) for data_render so the UI
        /// gets every column from the session cache — not an LLM-curated subset.
        /// </summary>
        public static bool TryExportDataJson(string chatSessionKey, string datasetName, out string dataJson, out string columnsJson, out int rowCount)
        {
            dataJson = null;
            columnsJson = null;
            rowCount = 0;
            var ds = Get(chatSessionKey, datasetName);
            if (ds == null || ds.Columns == null || ds.Columns.Count == 0)
                return false;

            var arr = new JArray();
            foreach (var cells in ds.Rows ?? new List<string[]>())
            {
                var obj = new JObject();
                for (int i = 0; i < ds.Columns.Count; i++)
                {
                    var name = ds.Columns[i].Name;
                    var cell = (cells != null && i < cells.Length) ? cells[i] : null;
                    if (string.IsNullOrEmpty(cell))
                        obj[name] = JValue.CreateNull();
                    else
                        obj[name] = cell;
                }
                arr.Add(obj);
            }

            var cols = new JArray();
            foreach (var c in ds.Columns)
            {
                cols.Add(new JObject
                {
                    ["field"] = c.Name,
                    ["header"] = c.Name,
                    ["dataType"] = c.Type == AgentColumnType.Numeric ? "number"
                        : c.Type == AgentColumnType.Date ? "date" : "string"
                });
            }

            dataJson = arr.ToString(Newtonsoft.Json.Formatting.None);
            columnsJson = cols.ToString(Newtonsoft.Json.Formatting.None);
            rowCount = arr.Count;
            return true;
        }

        public static void ClearSession(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            Sessions.TryRemove(chatSessionKey.Trim(), out _);
            NameCounters.TryRemove(chatSessionKey.Trim(), out _);
        }

        private static string NextDatasetName(string chatSessionKey, string sourceTool)
        {
            var baseName = SanitizeName(string.IsNullOrWhiteSpace(sourceTool) ? "dataset" : sourceTool);
            var n = NameCounters.AddOrUpdate(chatSessionKey.Trim(), 1, (_, v) => v + 1);
            return n <= 1 ? baseName : $"{baseName}_{n}";
        }

        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "dataset";
            var s = Regex.Replace(raw.Trim(), @"[^a-zA-Z0-9_\-]+", "_");
            if (s.Length > 80) s = s.Substring(0, 80);
            return string.IsNullOrEmpty(s) ? "dataset" : s;
        }

        public static AgentCachedDataset BuildDataset(string name, JArray dataArray, string sourceTool)
        {
            if (dataArray == null || dataArray.Count == 0)
                return new AgentCachedDataset { Name = name, CachedAt = DateTime.UtcNow, SourceTool = sourceTool };

            var take = Math.Min(dataArray.Count, MaxCachedRows);
            var headerList = new List<string>();
            var headerSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < take; i++)
            {
                if (dataArray[i] is not JObject o) continue;
                foreach (var prop in o.Properties())
                {
                    if (headerSeen.Add(prop.Name))
                        headerList.Add(prop.Name);
                }
            }
            if (headerList.Count == 0)
                return new AgentCachedDataset { Name = name, CachedAt = DateTime.UtcNow, SourceTool = sourceTool };

            var headers = headerList;
            int colCount = headers.Count;
            var colTypes = new AgentColumnType[colCount];
            var typeKnown = new bool[colCount];
            Array.Fill(colTypes, AgentColumnType.Text);

            for (int r = 0; r < take; r++)
            {
                if (dataArray[r] is not JObject row) continue;
                bool allKnown = true;
                for (int i = 0; i < colCount; i++)
                {
                    if (typeKnown[i]) continue;
                    allKnown = false;
                    var val = row[headers[i]];
                    if (val == null || val.Type == JTokenType.Null) continue;
                    colTypes[i] = InferType(val);
                    typeKnown[i] = true;
                }
                if (allKnown) break;
            }

            var columns = headers.Select((h, i) => new AgentColumnDefinition { Index = i, Name = h, Type = colTypes[i] }).ToList();
            var rows = new List<string[]>(take);
            var numericLists = colTypes.Select(t => t == AgentColumnType.Numeric ? new List<double?>() : null).ToArray();
            var dateLists = colTypes.Select(t => t == AgentColumnType.Date ? new List<double?>() : null).ToArray();

            for (int r = 0; r < take; r++)
            {
                if (dataArray[r] is not JObject row) continue;
                var cells = new string[colCount];
                for (int i = 0; i < colCount; i++)
                {
                    var val = row[headers[i]];
                    if (val == null || val.Type == JTokenType.Null)
                    {
                        cells[i] = "";
                        numericLists[i]?.Add(null);
                        dateLists[i]?.Add(null);
                        continue;
                    }
                    cells[i] = CellString(val);
                    if (numericLists[i] != null)
                        numericLists[i].Add(TryDouble(val));
                    if (dateLists[i] != null)
                        dateLists[i].Add(TryOaDate(val));
                }
                rows.Add(cells);
            }

            return new AgentCachedDataset
            {
                Name = name,
                CachedAt = DateTime.UtcNow,
                SourceTool = sourceTool,
                Columns = columns,
                Rows = rows,
                NumericColumns = numericLists
                    .Select((l, i) => (i, l))
                    .Where(x => x.l != null)
                    .ToDictionary(x => x.i, x => x.l.ToArray()),
                DateColumns = dateLists
                    .Select((l, i) => (i, l))
                    .Where(x => x.l != null)
                    .ToDictionary(x => x.i, x => x.l.ToArray())
            };
        }

        private static AgentColumnType InferType(JToken val)
        {
            if (val.Type == JTokenType.Integer || val.Type == JTokenType.Float)
                return AgentColumnType.Numeric;
            if (val.Type == JTokenType.String
                && DateTime.TryParse(val.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                return AgentColumnType.Date;
            if (val.Type == JTokenType.String
                && double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                return AgentColumnType.Numeric;
            return AgentColumnType.Text;
        }

        private static string CellString(JToken val) => val.Type switch
        {
            JTokenType.String => val.ToString(),
            JTokenType.Boolean => val.ToString().ToLowerInvariant(),
            JTokenType.Null => "",
            _ => val.ToString(Newtonsoft.Json.Formatting.None)
        };

        private static double? TryDouble(JToken val)
        {
            if (val.Type == JTokenType.Integer || val.Type == JTokenType.Float)
                return val.Value<double>();
            if (val.Type == JTokenType.String
                && double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }

        private static double? TryOaDate(JToken val)
        {
            if (val.Type == JTokenType.String
                && DateTime.TryParse(val.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToOADate();
            return null;
        }
    }
}
