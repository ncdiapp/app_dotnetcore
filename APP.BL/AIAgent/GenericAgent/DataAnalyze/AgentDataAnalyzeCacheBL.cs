using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.DataAnalyze
{
    /// <summary>
    /// ChatSession-scoped tabular cache for data_analyze.
    /// Keyed by ChatSessionKey + dataset name. Process-lifetime memory only.
    /// </summary>
    public static class AgentDataAnalyzeCacheBL
    {
        /// <summary>Max rows kept in session cache for tools (render / analyze / export).</summary>
        public const int MaxCachedRows = 50000;

        /// <summary>
        /// Max rows exposed to the LLM in tool_result text. Full data stays in cache for tools.
        /// Rule: tools = unlimited (up to <see cref="MaxCachedRows"/>); LLM = this preview cap.
        /// </summary>
        public const int LlmPreviewMaxRows = 20;

        /// <summary>When extracted row count &gt; <see cref="LlmPreviewMaxRows"/>, replace tool_result with a cache envelope (+ sample).</summary>
        public const int AutoEnvelopeMinRows = LlmPreviewMaxRows + 1;

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentCachedDataset>> Sessions
            = new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, int> NameCounters
            = new(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, CardRenderSnapshot> CardRenders
            = new(StringComparer.OrdinalIgnoreCase);

        public static AgentCachedDataset Put(string chatSessionKey, string sourceTool, JArray rows, int? displayRowLimit = null)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                throw new ArgumentException("ChatSessionKey is required for data analyze cache.", nameof(chatSessionKey));

            var name = NextDatasetName(chatSessionKey, sourceTool);
            var ds = BuildDataset(name, rows, sourceTool ?? "", displayRowLimit);
            var bag = Sessions.GetOrAdd(chatSessionKey.Trim(), _ => new ConcurrentDictionary<string, AgentCachedDataset>(StringComparer.OrdinalIgnoreCase));
            bag[name] = ds;
            if (displayRowLimit is > 0)
                Documents.AgentDisplayRowLimitBL.SetDatasetDisplayLimit(chatSessionKey, name, displayRowLimit);
            AgentAiPolishCacheBL.InvalidateDataset(chatSessionKey, name);
            return ds;
        }

        public static AgentCachedDataset PutNamed(string chatSessionKey, string datasetName, JArray rows, string sourceTool = "", int? displayRowLimit = null)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                throw new ArgumentException("ChatSessionKey is required for data analyze cache.", nameof(chatSessionKey));
            if (string.IsNullOrWhiteSpace(datasetName))
                throw new ArgumentException("datasetName is required.", nameof(datasetName));

            var name = SanitizeName(datasetName);
            var ds = BuildDataset(name, rows, sourceTool ?? "", displayRowLimit);
            var bag = Sessions.GetOrAdd(chatSessionKey.Trim(), _ => new ConcurrentDictionary<string, AgentCachedDataset>(StringComparer.OrdinalIgnoreCase));
            bag[name] = ds;
            if (displayRowLimit is > 0)
                Documents.AgentDisplayRowLimitBL.SetDatasetDisplayLimit(chatSessionKey, name, displayRowLimit);
            AgentAiPolishCacheBL.InvalidateDataset(chatSessionKey, name);
            return ds;
        }

        public static int? GetDisplayRowLimit(string chatSessionKey, string datasetName)
        {
            var ds = Get(chatSessionKey, datasetName);
            if (ds?.DisplayRowLimit is > 0) return ds.DisplayRowLimit;
            if (Documents.AgentDisplayRowLimitBL.TryGetDatasetDisplayLimit(chatSessionKey, datasetName, out var n))
                return n;
            return null;
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
        /// The model often copies a short preview or drops image URLs when it rebuilds dataJson.
        /// Put the cached row back, including Sketch and other columns it omitted.
        /// A short renamed summary (few rows, few fields) is replaced by the whole latest dataset when <paramref name="expandShortSummary"/> is true.
        /// </summary>
        public static bool TryRestoreRows(string chatSessionKey, JArray inline, bool expandShortSummary, out JArray rows, out string datasetName)
        {
            rows = null;
            datasetName = null;
            var ds = Latest(chatSessionKey);
            if (ds?.Rows == null || ds.Rows.Count == 0 || ds.Columns == null || ds.Columns.Count == 0)
                return false;
            if (!TryExportDataJson(chatSessionKey, ds.Name, out var dataJson, out _, out _))
                return false;

            var cached = JArray.Parse(dataJson);
            var cachedRows = cached.OfType<JObject>().ToList();
            if (cachedRows.Count == 0)
                return false;

            var inlineRows = inline?.OfType<JObject>().ToList() ?? new List<JObject>();
            if (inlineRows.Count == 0)
            {
                if (!expandShortSummary)
                    return false;
                rows = cached;
                datasetName = ds.Name;
                return true;
            }

            string inlineKey = null;
            string cacheKey = null;
            var bestHits = 0;
            foreach (var prop in inlineRows[0].Properties())
            {
                var values = inlineRows
                    .Select(o => o[prop.Name]?.ToString() ?? "")
                    .Where(v => v.Length > 0)
                    .ToList();
                if (values.Count == 0)
                    continue;
                foreach (var column in ds.Columns)
                {
                    var set = new HashSet<string>(
                        cachedRows.Select(o => o[column.Name]?.ToString() ?? ""),
                        StringComparer.OrdinalIgnoreCase);
                    var hits = values.Count(v => set.Contains(v));
                    if (hits > bestHits)
                    {
                        bestHits = hits;
                        inlineKey = prop.Name;
                        cacheKey = column.Name;
                    }
                }
            }

            var matched = bestHits >= Math.Max(1, inlineRows.Count / 2);
            var lossy = IsLossy(inlineRows, cachedRows, matched, inlineKey, cacheKey, ds.Columns.Count);
            if (!lossy || !matched)
                return false;
            _ = expandShortSummary;

            var map = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in cachedRows)
            {
                var key = row[cacheKey]?.ToString() ?? "";
                if (key.Length > 0 && !map.ContainsKey(key))
                    map[key] = row;
            }

            var restored = new JArray();
            foreach (var row in inlineRows)
            {
                var key = row[inlineKey]?.ToString() ?? "";
                restored.Add(key.Length > 0 && map.TryGetValue(key, out var full) ? full.DeepClone() : row);
            }
            rows = restored;
            datasetName = ds.Name;
            return true;
        }

        /// <summary>Most recently cached non-empty dataset for this chat session.</summary>
        public static AgentCachedDataset Latest(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                return null;
            if (!Sessions.TryGetValue(chatSessionKey.Trim(), out var bag))
                return null;
            return bag.Values
                .OrderByDescending(d => d.CachedAt)
                .FirstOrDefault(d => d.Rows != null && d.Rows.Count > 0);
        }

        private static bool IsLossy(List<JObject> inlineRows, List<JObject> cachedRows, bool matched, string inlineKey, string cacheKey, int cacheColumnCount)
        {
            var inlineColumnCount = inlineRows[0].Properties().Count();
            if (inlineColumnCount + 1 < cacheColumnCount)
                return true;
            if (!matched || string.IsNullOrEmpty(inlineKey) || string.IsNullOrEmpty(cacheKey))
                return false;

            var map = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in cachedRows)
            {
                var key = row[cacheKey]?.ToString() ?? "";
                if (key.Length > 0 && !map.ContainsKey(key))
                    map[key] = row;
            }

            foreach (var row in inlineRows)
            {
                var key = row[inlineKey]?.ToString() ?? "";
                if (key.Length == 0 || !map.TryGetValue(key, out var full))
                    continue;
                foreach (var prop in full.Properties())
                {
                    var cachedText = prop.Value?.Type == JTokenType.Null ? "" : prop.Value?.ToString() ?? "";
                    if (cachedText.Length == 0)
                        continue;
                    var inlineProp = row.Properties().FirstOrDefault(p => string.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase));
                    if (inlineProp == null)
                        return true;
                    var inlineText = inlineProp.Value?.ToString() ?? "";
                    if (LooksLikeImage(cachedText) && !LooksLikeImage(inlineText))
                        return true;
                }
            }
            return false;
        }

        private static bool LooksLikeImage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var text = value.Trim();
            return text.IndexOf("/api/resources/", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("/api/files/", StringComparison.OrdinalIgnoreCase) >= 0
                || text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
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
            var key = chatSessionKey.Trim();
            Sessions.TryRemove(key, out _);
            NameCounters.TryRemove(key, out _);
            CardRenders.TryRemove(key, out _);
            AgentAiPolishCacheBL.ClearSession(key);
        }

        /// <summary>The card panel last shown in this chat. Export uses it instead of a rewritten summary.</summary>
        public static void RememberCardRender(string chatSessionKey, JArray rows, string title)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || rows == null)
                return;
            CardRenders[chatSessionKey.Trim()] = new CardRenderSnapshot
            {
                Title = title ?? "",
                Rows = (JArray)rows.DeepClone()
            };
        }

        public static bool TryGetCardRender(string chatSessionKey, out JArray rows, out string title)
        {
            rows = null;
            title = null;
            if (string.IsNullOrWhiteSpace(chatSessionKey))
                return false;
            if (!CardRenders.TryGetValue(chatSessionKey.Trim(), out var shot) || shot?.Rows == null || shot.Rows.Count == 0)
                return false;
            rows = (JArray)shot.Rows.DeepClone();
            title = shot.Title;
            return true;
        }

        public static JArray TakeFirst(JArray rows, int limit)
        {
            if (rows == null || limit <= 0 || rows.Count <= limit)
                return rows;
            var slice = new JArray();
            for (var i = 0; i < limit; i++)
                slice.Add(rows[i]);
            return slice;
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

        public static AgentCachedDataset BuildDataset(string name, JArray dataArray, string sourceTool, int? displayRowLimit = null)
        {
            if (dataArray == null || dataArray.Count == 0)
                return new AgentCachedDataset { Name = name, CachedAt = DateTime.UtcNow, SourceTool = sourceTool, DisplayRowLimit = displayRowLimit };

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
                DisplayRowLimit = displayRowLimit,
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

        private sealed class CardRenderSnapshot
        {
            public string Title;
            public JArray Rows;
        }
    }
}
