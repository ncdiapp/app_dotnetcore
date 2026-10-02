using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.DataAnalyze
{
    /// <summary>
    /// Deterministic tabular analysis over <see cref="AgentCachedDataset"/>.
    /// Operations aligned with the MCP gateway AnalysisService reference (ported, not linked).
    /// </summary>
    public static class AgentDataAnalyzeService
    {
        public static string Analyze(
            AgentCachedDataset dataset,
            string operation,
            string column = null,
            string groupBy = null,
            string filter = null,
            string filterColumn = null,
            int limit = 20,
            string orderBy = null)
        {
            if (dataset == null)
                return JsonConvert.SerializeObject(new { ok = false, error = "Dataset not found." });

            if (!string.IsNullOrEmpty(filterColumn) && ColIdx(dataset, filterColumn) < 0)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = $"filter_column '{filterColumn}' not found. Available: {string.Join(", ", dataset.Columns.Select(c => c.Name))}"
                });
            }

            if (limit <= 0) limit = 20;
            var indices = ApplyFilter(dataset, filter, filterColumn);
            var op = (operation ?? "").Trim().ToLowerInvariant();

            string text;
            JToken result;
            switch (op)
            {
                case "summary":
                    (text, result) = BuildSummary(dataset, indices);
                    break;
                case "count":
                    (text, result) = BuildCount(dataset, indices, groupBy, limit, orderBy);
                    break;
                case "sum":
                case "avg":
                case "min":
                case "max":
                    (text, result) = BuildAggregate(dataset, indices, column, op, groupBy, limit, orderBy);
                    break;
                case "distinct":
                    (text, result) = BuildDistinct(dataset, indices, column, limit);
                    break;
                case "top":
                    (text, result) = BuildTop(dataset, indices, column, limit, orderBy);
                    break;
                case "distribution":
                    (text, result) = BuildDistribution(dataset, indices, column, limit);
                    break;
                default:
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"Unknown operation '{operation}'. Valid: summary, count, sum, avg, min, max, distinct, top, distribution"
                    });
            }

            return JsonConvert.SerializeObject(new
            {
                ok = true,
                dataset_name = dataset.Name,
                operation = op,
                text,
                result,
                columns = dataset.Columns.Select(c => new { c.Name, type = c.Type.ToString().ToLowerInvariant() }).ToList()
            }, Formatting.None);
        }

        private static List<int> ApplyFilter(AgentCachedDataset ds, string filter, string filterColumn)
        {
            if (string.IsNullOrEmpty(filter))
                return Enumerable.Range(0, ds.Rows.Count).ToList();

            if (!string.IsNullOrEmpty(filterColumn))
            {
                int col = ColIdx(ds, filterColumn);
                if (col < 0) return Enumerable.Range(0, ds.Rows.Count).ToList();
                return ds.Rows
                    .Select((row, i) => (row, i))
                    .Where(t => t.row.Length > col && t.row[col].IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(t => t.i).ToList();
            }

            return ds.Rows
                .Select((row, i) => (row, i))
                .Where(t => t.row.Any(cell => cell != null && cell.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(t => t.i).ToList();
        }

        private static int ColIdx(AgentCachedDataset ds, string name)
            => ds.Columns.FindIndex(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private static string Cell(AgentCachedDataset ds, int rowIdx, int colIdx)
            => ds.Rows[rowIdx].Length > colIdx ? ds.Rows[rowIdx][colIdx] : "";

        private static (string text, JToken result) BuildSummary(AgentCachedDataset ds, List<int> indices)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Dataset : {ds.Name}");
            sb.AppendLine($"Rows    : {indices.Count} (cached: {ds.Rows.Count})");
            sb.AppendLine($"Source  : {ds.SourceTool}");
            sb.AppendLine($"Cached  : {ds.CachedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("Columns:");
            var cols = new JArray();
            foreach (var col in ds.Columns)
            {
                var distinct = indices.Select(i => Cell(ds, i, col.Index)).Distinct().Count();
                sb.AppendLine($"  [{col.Index}] {col.Name} ({col.Type}) — {distinct} distinct");
                cols.Add(new JObject
                {
                    ["name"] = col.Name,
                    ["type"] = col.Type.ToString().ToLowerInvariant(),
                    ["distinct"] = distinct
                });
            }
            return (sb.ToString().TrimEnd(), new JObject
            {
                ["rowCount"] = indices.Count,
                ["cachedRows"] = ds.Rows.Count,
                ["columns"] = cols
            });
        }

        private static (string text, JToken result) BuildCount(
            AgentCachedDataset ds, List<int> indices, string groupBy, int limit, string orderBy)
        {
            if (string.IsNullOrEmpty(groupBy))
            {
                var t = $"Count: {indices.Count}";
                return (t, new JObject { ["count"] = indices.Count });
            }

            int gc = ColIdx(ds, groupBy);
            if (gc < 0) return ($"Column '{groupBy}' not found.", new JObject { ["error"] = $"Column '{groupBy}' not found." });

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var i in indices)
            {
                var key = Cell(ds, i, gc);
                counts[key] = counts.TryGetValue(key, out var c) ? c + 1 : 1;
            }

            var text = RenderKvResult($"Count by {groupBy}", counts, limit, orderBy);
            var arr = ToKvArray(counts, limit, orderBy, "key", "count");
            return (text, arr);
        }

        private static (string text, JToken result) BuildAggregate(
            AgentCachedDataset ds, List<int> indices, string column, string op, string groupBy, int limit, string orderBy)
        {
            if (string.IsNullOrEmpty(column))
                return ($"Operation '{op}' requires a 'column' parameter.", new JObject { ["error"] = "column required" });

            int ci = ColIdx(ds, column);
            if (ci < 0) return ($"Column '{column}' not found.", new JObject { ["error"] = $"Column '{column}' not found." });
            if (!ds.NumericColumns.TryGetValue(ci, out var nums))
                return ($"Column '{column}' is not numeric.", new JObject { ["error"] = $"Column '{column}' is not numeric." });

            if (string.IsNullOrEmpty(groupBy))
            {
                var vals = indices.Select(i => nums[i]).Where(v => v.HasValue).Select(v => v.Value).ToList();
                if (vals.Count == 0)
                    return ($"{op}({column}): no numeric values", new JObject { ["value"] = null, ["n"] = 0 });
                var r = Agg(op, vals);
                return ($"{op}({column}): {r:G}  ({vals.Count} rows)", new JObject { ["value"] = r, ["n"] = vals.Count });
            }

            int gc = ColIdx(ds, groupBy);
            if (gc < 0) return ($"Column '{groupBy}' not found.", new JObject { ["error"] = $"Column '{groupBy}' not found." });

            var groups = new Dictionary<string, List<double>>(StringComparer.Ordinal);
            foreach (var i in indices)
            {
                var key = Cell(ds, i, gc);
                if (!groups.ContainsKey(key)) groups[key] = new List<double>();
                if (nums[i].HasValue) groups[key].Add(nums[i].Value);
            }

            var results = groups.ToDictionary(kv => kv.Key, kv => Agg(op, kv.Value));
            var text = RenderKvResult($"{op}({column}) by {groupBy}", results, limit, orderBy);
            return (text, ToKvArray(results, limit, orderBy, "key", "value"));
        }

        private static double Agg(string op, List<double> v) => op switch
        {
            "sum" => v.Count > 0 ? v.Sum() : 0,
            "avg" => v.Count > 0 ? v.Average() : 0,
            "min" => v.Count > 0 ? v.Min() : 0,
            "max" => v.Count > 0 ? v.Max() : 0,
            _ => 0
        };

        private static (string text, JToken result) BuildDistinct(
            AgentCachedDataset ds, List<int> indices, string column, int limit)
        {
            if (string.IsNullOrEmpty(column))
                return ("Operation 'distinct' requires a 'column' parameter.", new JObject { ["error"] = "column required" });
            int ci = ColIdx(ds, column);
            if (ci < 0) return ($"Column '{column}' not found.", new JObject { ["error"] = $"Column '{column}' not found." });

            var values = indices.Select(i => Cell(ds, i, ci)).Distinct().OrderBy(v => v).Take(limit).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"Distinct '{column}' ({values.Count} shown):");
            foreach (var v in values) sb.AppendLine($"  {v}");
            return (sb.ToString().TrimEnd(), new JArray(values.Select(v => (JToken)v)));
        }

        private static (string text, JToken result) BuildTop(
            AgentCachedDataset ds, List<int> indices, string column, int limit, string orderBy)
        {
            if (string.IsNullOrEmpty(column))
                return ("Operation 'top' requires a 'column' parameter.", new JObject { ["error"] = "column required" });
            int ci = ColIdx(ds, column);
            if (ci < 0) return ($"Column '{column}' not found.", new JObject { ["error"] = $"Column '{column}' not found." });

            bool asc = string.Equals(orderBy, "asc", StringComparison.OrdinalIgnoreCase);
            IEnumerable<int> sorted;
            if (ds.NumericColumns.TryGetValue(ci, out var nums))
                sorted = asc
                    ? indices.OrderBy(i => nums[i] ?? double.MinValue)
                    : indices.OrderByDescending(i => nums[i] ?? double.MinValue);
            else
                sorted = asc
                    ? indices.OrderBy(i => Cell(ds, i, ci))
                    : indices.OrderByDescending(i => Cell(ds, i, ci));

            var top = sorted.Take(limit).ToList();
            var displayCols = ds.Columns.Take(5).ToList();
            if (!displayCols.Any(c => c.Index == ci))
                displayCols.Add(ds.Columns[ci]);

            var sb = new StringBuilder();
            sb.AppendLine($"Top {top.Count} by '{column}' ({(asc ? "asc" : "desc")}):");
            sb.AppendLine("  " + string.Join(" | ", displayCols.Select(c => c.Name.PadRight(15))));
            var arr = new JArray();
            foreach (var i in top)
            {
                var cells = displayCols.Select(c => Cell(ds, i, c.Index).PadRight(15));
                sb.AppendLine("  " + string.Join(" | ", cells));
                var obj = new JObject();
                foreach (var c in displayCols)
                    obj[c.Name] = Cell(ds, i, c.Index);
                arr.Add(obj);
            }
            return (sb.ToString().TrimEnd(), arr);
        }

        private static (string text, JToken result) BuildDistribution(
            AgentCachedDataset ds, List<int> indices, string column, int limit)
        {
            if (string.IsNullOrEmpty(column))
                return ("Operation 'distribution' requires a 'column' parameter.", new JObject { ["error"] = "column required" });
            int ci = ColIdx(ds, column);
            if (ci < 0) return ($"Column '{column}' not found.", new JObject { ["error"] = $"Column '{column}' not found." });

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var i in indices)
            {
                var key = Cell(ds, i, ci);
                counts[key] = counts.TryGetValue(key, out var c) ? c + 1 : 1;
            }

            int total = indices.Count;
            var sb = new StringBuilder();
            sb.AppendLine($"Distribution of '{column}' ({total} rows):");
            var arr = new JArray();
            foreach (var kv in counts.OrderByDescending(kv => kv.Value).Take(limit))
            {
                double pct = total > 0 ? kv.Value * 100.0 / total : 0;
                sb.AppendLine($"  {kv.Key}: {kv.Value} ({pct:F1}%)");
                arr.Add(new JObject { ["key"] = kv.Key, ["count"] = kv.Value, ["pct"] = Math.Round(pct, 1) });
            }
            return (sb.ToString().TrimEnd(), arr);
        }

        private static string RenderKvResult<T>(string header, Dictionary<string, T> data, int limit, string orderBy)
            where T : IComparable<T>
        {
            IEnumerable<KeyValuePair<string, T>> sorted = string.Equals(orderBy, "asc", StringComparison.OrdinalIgnoreCase)
                ? data.OrderBy(kv => kv.Value)
                : data.OrderByDescending(kv => kv.Value);

            var sb = new StringBuilder();
            sb.AppendLine($"{header}:");
            foreach (var kv in sorted.Take(limit))
                sb.AppendLine($"  {kv.Key}: {kv.Value}");
            return sb.ToString().TrimEnd();
        }

        private static JArray ToKvArray<T>(Dictionary<string, T> data, int limit, string orderBy, string keyName, string valueName)
            where T : IComparable<T>
        {
            IEnumerable<KeyValuePair<string, T>> sorted = string.Equals(orderBy, "asc", StringComparison.OrdinalIgnoreCase)
                ? data.OrderBy(kv => kv.Value)
                : data.OrderByDescending(kv => kv.Value);
            var arr = new JArray();
            foreach (var kv in sorted.Take(limit))
                arr.Add(new JObject { [keyName] = kv.Key, [valueName] = JToken.FromObject(kv.Value) });
            return arr;
        }
    }
}
