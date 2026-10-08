using System.Text;
using McpGateway.Models;

namespace McpGateway.Services;

public class AnalysisService : IAnalysisService
{
    private readonly IDataAnalysisCacheService _cache;

    public AnalysisService(IDataAnalysisCacheService cache)
    {
        _cache = cache;
    }

    public async Task<string> AnalyzeAsync(string datasetName, string operation,
        string? column = null, string? groupBy = null,
        string? filter = null, string? filterColumn = null,
        int limit = 20, string? orderBy = null)
    {
        var dataset = await _cache.GetCachedDatasetAsync(datasetName);
        if (dataset == null)
            return $"Dataset '{datasetName}' not found. Call api_execute with the corresponding operationId first to cache it.";

        // Validate filter_column early so the error reaches Claude instead of silently scanning all rows
        if (!string.IsNullOrEmpty(filterColumn) && ColIdx(dataset, filterColumn) < 0)
            return $"filter_column '{filterColumn}' not found. Available columns: {string.Join(", ", dataset.Columns.Select(c => c.Name))}";

        var indices = ApplyFilter(dataset, filter, filterColumn);

        return operation.ToLowerInvariant() switch
        {
            "summary"      => BuildSummary(dataset, indices),
            "count"        => BuildCount(dataset, indices, groupBy, limit, orderBy),
            "sum"          => BuildAggregate(dataset, indices, column, "sum",  groupBy, limit, orderBy),
            "avg"          => BuildAggregate(dataset, indices, column, "avg",  groupBy, limit, orderBy),
            "min"          => BuildAggregate(dataset, indices, column, "min",  groupBy, limit, orderBy),
            "max"          => BuildAggregate(dataset, indices, column, "max",  groupBy, limit, orderBy),
            "distinct"     => BuildDistinct(dataset, indices, column, limit),
            "top"          => BuildTop(dataset, indices, column, limit, orderBy),
            "distribution" => BuildDistribution(dataset, indices, column, limit),
            _              => $"Unknown operation '{operation}'. Valid: summary, count, sum, avg, min, max, distinct, top, distribution"
        };
    }

    // ── filter ───────────────────────────────────────────────────────────────

    private static List<int> ApplyFilter(CachedDataset ds, string? filter, string? filterColumn)
    {
        if (string.IsNullOrEmpty(filter))
            return Enumerable.Range(0, ds.Rows.Count).ToList();

        if (!string.IsNullOrEmpty(filterColumn))
        {
            int col = ColIdx(ds, filterColumn);
            if (col < 0) return Enumerable.Range(0, ds.Rows.Count).ToList();
            return ds.Rows
                .Select((row, i) => (row, i))
                .Where(t => t.row.Length > col && t.row[col].Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.i).ToList();
        }

        return ds.Rows
            .Select((row, i) => (row, i))
            .Where(t => t.row.Any(cell => cell.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.i).ToList();
    }

    private static int ColIdx(CachedDataset ds, string name)
        => ds.Columns.FindIndex(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string Cell(CachedDataset ds, int rowIdx, int colIdx)
        => ds.Rows[rowIdx].Length > colIdx ? ds.Rows[rowIdx][colIdx] : "";

    // ── summary ──────────────────────────────────────────────────────────────

    private static string BuildSummary(CachedDataset ds, List<int> indices)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Dataset : {ds.Name}");
        sb.AppendLine($"Rows    : {indices.Count} (total cached: {ds.Rows.Count})");
        sb.AppendLine($"Cached  : {ds.CachedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("Columns:");
        foreach (var col in ds.Columns)
        {
            var distinct = indices.Select(i => Cell(ds, i, col.Index)).Distinct().Count();
            sb.AppendLine($"  [{col.Index}] {col.Name} ({col.Type}) — {distinct} distinct values");
        }
        return sb.ToString();
    }

    // ── count ─────────────────────────────────────────────────────────────────

    private static string BuildCount(CachedDataset ds, List<int> indices, string? groupBy, int limit, string? orderBy)
    {
        if (string.IsNullOrEmpty(groupBy))
            return $"Count: {indices.Count}";

        int gc = ColIdx(ds, groupBy);
        if (gc < 0) return $"Column '{groupBy}' not found.";

        var counts = new Dictionary<string, int>();
        foreach (var i in indices)
        {
            var key = Cell(ds, i, gc);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        return RenderKvResult($"Count by {groupBy}", counts, limit, orderBy);
    }

    // ── aggregate (sum / avg / min / max) ─────────────────────────────────────

    private static string BuildAggregate(CachedDataset ds, List<int> indices,
        string? column, string op, string? groupBy, int limit, string? orderBy)
    {
        if (string.IsNullOrEmpty(column))
            return $"Operation '{op}' requires a 'column' parameter.";

        int ci = ColIdx(ds, column);
        if (ci < 0) return $"Column '{column}' not found.";
        if (!ds.NumericColumns.TryGetValue(ci, out var nums))
            return $"Column '{column}' is not numeric.";

        if (string.IsNullOrEmpty(groupBy))
        {
            var vals = indices.Select(i => nums[i]).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (vals.Count == 0) return $"{op}({column}): no numeric values";
            var r = Agg(op, vals);
            return $"{op}({column}): {r:G}  ({vals.Count} rows)";
        }

        int gc = ColIdx(ds, groupBy);
        if (gc < 0) return $"Column '{groupBy}' not found.";

        var groups = new Dictionary<string, List<double>>();
        foreach (var i in indices)
        {
            var key = Cell(ds, i, gc);
            if (!groups.ContainsKey(key)) groups[key] = [];
            if (nums[i].HasValue) groups[key].Add(nums[i]!.Value);
        }

        var results = groups.ToDictionary(kv => kv.Key, kv => Agg(op, kv.Value));
        return RenderKvResult($"{op}({column}) by {groupBy}", results, limit, orderBy);
    }

    private static double Agg(string op, List<double> v) => op switch
    {
        "sum" => v.Count > 0 ? v.Sum()     : 0,
        "avg" => v.Count > 0 ? v.Average() : 0,
        "min" => v.Count > 0 ? v.Min()     : 0,
        "max" => v.Count > 0 ? v.Max()     : 0,
        _     => 0
    };

    // ── distinct ──────────────────────────────────────────────────────────────

    private static string BuildDistinct(CachedDataset ds, List<int> indices, string? column, int limit)
    {
        if (string.IsNullOrEmpty(column)) return "Operation 'distinct' requires a 'column' parameter.";
        int ci = ColIdx(ds, column);
        if (ci < 0) return $"Column '{column}' not found.";

        var values = indices.Select(i => Cell(ds, i, ci)).Distinct().OrderBy(v => v).Take(limit).ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"Distinct '{column}' ({values.Count} shown):");
        foreach (var v in values) sb.AppendLine($"  {v}");
        return sb.ToString();
    }

    // ── top ───────────────────────────────────────────────────────────────────

    private static string BuildTop(CachedDataset ds, List<int> indices, string? column, int limit, string? orderBy)
    {
        if (string.IsNullOrEmpty(column)) return "Operation 'top' requires a 'column' parameter.";
        int ci = ColIdx(ds, column);
        if (ci < 0) return $"Column '{column}' not found.";

        bool asc = orderBy?.ToLowerInvariant() == "asc";

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
        // Always include the sort column even if it falls outside the first 5
        var displayCols = ds.Columns.Take(5).ToList();
        if (!displayCols.Any(c => c.Index == ci))
            displayCols.Add(ds.Columns[ci]);

        var sb = new StringBuilder();
        sb.AppendLine($"Top {top.Count} by '{column}' ({(asc ? "asc" : "desc")}):");
        sb.AppendLine("  " + string.Join(" | ", displayCols.Select(c => c.Name.PadRight(15))));
        foreach (var i in top)
        {
            var cells = displayCols.Select(c => Cell(ds, i, c.Index).PadRight(15));
            sb.AppendLine("  " + string.Join(" | ", cells));
        }
        return sb.ToString();
    }

    // ── distribution ─────────────────────────────────────────────────────────

    private static string BuildDistribution(CachedDataset ds, List<int> indices, string? column, int limit)
    {
        if (string.IsNullOrEmpty(column)) return "Operation 'distribution' requires a 'column' parameter.";
        int ci = ColIdx(ds, column);
        if (ci < 0) return $"Column '{column}' not found.";

        var counts = new Dictionary<string, int>();
        foreach (var i in indices)
        {
            var key = Cell(ds, i, ci);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        int total = indices.Count;
        var sb = new StringBuilder();
        sb.AppendLine($"Distribution of '{column}' ({total} rows):");
        foreach (var kv in counts.OrderByDescending(kv => kv.Value).Take(limit))
        {
            double pct = total > 0 ? kv.Value * 100.0 / total : 0;
            sb.AppendLine($"  {kv.Key}: {kv.Value} ({pct:F1}%)");
        }
        return sb.ToString();
    }

    // ── shared render helper ─────────────────────────────────────────────────

    private static string RenderKvResult<T>(string header, Dictionary<string, T> data, int limit, string? orderBy)
        where T : IComparable<T>
    {
        var sorted = orderBy?.ToLowerInvariant() == "asc"
            ? data.OrderBy(kv => kv.Value)
            : (IEnumerable<KeyValuePair<string, T>>)data.OrderByDescending(kv => kv.Value);

        var sb = new StringBuilder();
        sb.AppendLine($"{header}:");
        foreach (var kv in sorted.Take(limit))
            sb.AppendLine($"  {kv.Key}: {kv.Value}");
        return sb.ToString();
    }
}
