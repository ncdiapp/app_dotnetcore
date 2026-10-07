using System.Text.Json;
using McpGateway.Models;
using Microsoft.Extensions.Options;

namespace McpGateway.Services;

/// <summary>
/// Two cache scopes, both derived from the authenticated caller (never from client headers):
///   company-global: reference data shared by the users of ONE company; disk-backed in {cacheDir}/{companyId}/.
///   per-user: memory-only, keyed {companyId}:{userId}:{name}.
/// A request with no authenticated caller throws instead of falling back to a shared bucket.
/// </summary>
public class DataAnalysisCacheService : IDataAnalysisCacheService
{
    private readonly ILogger<DataAnalysisCacheService> _logger;
    private readonly IMcpCallerContext _callerContext;
    private readonly int _maxRows;
    private readonly string _cacheDir;

    // Company-global: keyed {companyId}:{name}
    private readonly Dictionary<string, CachedDataset> _globalCache = new();

    // Per-user: memory-only, keyed {companyId}:{userId}:{name}
    private readonly Dictionary<string, CachedDataset> _sessionCache = new();

    private readonly SemaphoreSlim _lock = new(1, 1);

    public DataAnalysisCacheService(
        ILogger<DataAnalysisCacheService> logger,
        IMcpCallerContext callerContext,
        IOptions<DataAnalysisSettings> settings)
    {
        _logger              = logger;
        _callerContext       = callerContext;
        _maxRows             = settings.Value.MaxRows;
        _cacheDir            = Path.IsPathRooted(settings.Value.CacheDirectory)
            ? settings.Value.CacheDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), settings.Value.CacheDirectory);
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<CachedDataset> SyncFromJsonAsync(string app, string operationId, JsonElement jsonDataArray, bool isGlobal = false)
    {
        var (companyId, userId) = _callerContext.RequireIdentity();
        var name = BuildDatasetName(app, operationId);

        await _lock.WaitAsync();
        try
        {
            var dataset = BuildDataset(name, jsonDataArray, _maxRows);

            if (isGlobal)
            {
                // Write atomically: write to a .tmp file then rename so a crash mid-write
                // never leaves a corrupt .json file that gets loaded on the next restart.
                var filePath = GlobalFilePath(companyId, name);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                var tmpPath  = filePath + ".tmp";
                await File.WriteAllTextAsync(tmpPath, jsonDataArray.GetRawText());
                File.Move(tmpPath, filePath, overwrite: true);
                dataset.FilePath = filePath;
                _globalCache[GlobalKey(companyId, name)] = dataset;
                _logger.LogInformation("DataAnalysis [company {Company} global]: cached {Name} — {Rows} rows, {Cols} columns",
                    companyId, name, dataset.Rows.Count, dataset.Columns.Count);
            }
            else
            {
                _sessionCache[UserKey(companyId, userId, name)] = dataset;
                _logger.LogInformation("DataAnalysis [company {Company} user {User}]: cached {Name} — {Rows} rows, {Cols} columns",
                    companyId, userId, name, dataset.Rows.Count, dataset.Columns.Count);
            }

            return dataset;
        }
        finally { _lock.Release(); }
    }

    public async Task<CachedDataset?> GetCachedDatasetAsync(string datasetName)
    {
        var (companyId, userId) = _callerContext.RequireIdentity();

        // The name comes from the model/client and ends up in a file path: only names we generate are valid.
        if (!IsSafeDatasetName(datasetName)) return null;

        // The caller's own company-global data first, then the caller's private datasets.
        var global = await GetGlobalDatasetAsync(companyId, datasetName);
        if (global != null) return global;

        await _lock.WaitAsync();
        try
        {
            return _sessionCache.TryGetValue(UserKey(companyId, userId, datasetName), out var session) ? session : null;
        }
        finally { _lock.Release(); }
    }

    private async Task<CachedDataset?> GetGlobalDatasetAsync(int companyId, string datasetName)
    {
        var key = GlobalKey(companyId, datasetName);

        await _lock.WaitAsync();
        try
        {
            if (_globalCache.TryGetValue(key, out var hit))
                return hit;
        }
        finally { _lock.Release(); }

        // Try loading from disk (survives service restarts)
        var filePath = GlobalFilePath(companyId, datasetName);
        if (!File.Exists(filePath)) return null;

        var json    = await File.ReadAllTextAsync(filePath);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var loaded  = BuildDataset(datasetName, element, _maxRows);
        loaded.FilePath = filePath;

        await _lock.WaitAsync();
        try { _globalCache[key] = loaded; }
        finally { _lock.Release(); }

        _logger.LogInformation("DataAnalysis [company {Company} global]: loaded {Name} from disk — {Rows} rows",
            companyId, datasetName, loaded.Rows.Count);
        return loaded;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string GlobalKey(int companyId, string name) => $"{companyId}:{name}";

    private static string UserKey(int companyId, int userId, string name) => $"{companyId}:{userId}:{name}";

    private string GlobalFilePath(int companyId, string name) =>
        Path.Combine(_cacheDir, companyId.ToString(), name + ".json");

    /// <summary>True only for names BuildDatasetName could have produced (no separators or dots).</summary>
    internal static bool IsSafeDatasetName(string name) =>
        !string.IsNullOrEmpty(name) && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

    private static CachedDataset BuildDataset(string name, JsonElement dataArray, int maxRows)
    {
        if (dataArray.ValueKind != JsonValueKind.Array || dataArray.GetArrayLength() == 0)
            return new CachedDataset { Name = name, CachedAt = DateTime.UtcNow };

        int totalRows = dataArray.GetArrayLength();
        if (totalRows > maxRows)
            throw new InvalidOperationException(
                $"Dataset '{name}' has {totalRows} rows, exceeding the limit of {maxRows}. " +
                $"Increase DataAnalysis:MaxRows in appsettings.json if needed.");

        var first = dataArray[0];
        if (first.ValueKind != JsonValueKind.Object)
            return new CachedDataset { Name = name, CachedAt = DateTime.UtcNow };

        var headers  = first.EnumerateObject().Select(p => p.Name).ToList();
        int colCount = headers.Count;

        // Pass 1: resolve column type from the first non-null JsonValueKind per column
        var colTypes  = new ColumnType[colCount];
        var typeKnown = new bool[colCount];
        Array.Fill(colTypes, ColumnType.Text);

        foreach (var row in dataArray.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object) continue;
            bool allKnown = true;
            for (int i = 0; i < colCount; i++)
            {
                if (typeKnown[i]) continue;
                allKnown = false;
                if (!row.TryGetProperty(headers[i], out var val) || val.ValueKind == JsonValueKind.Null) continue;
                colTypes[i] = val.ValueKind == JsonValueKind.Number ? ColumnType.Numeric
                    : val.ValueKind == JsonValueKind.String && DateTime.TryParse(val.GetString(), out _) ? ColumnType.Date
                    : ColumnType.Text;
                typeKnown[i] = true;
            }
            if (allKnown) break;
        }

        var columns      = headers.Select((h, i) => new ColumnDefinition { Index = i, Name = h, Type = colTypes[i] }).ToList();
        var rows         = new List<string[]>(totalRows);
        var numericLists = colTypes.Select((t, i) => t == ColumnType.Numeric ? new List<double?>() : null).ToArray();
        var dateLists    = colTypes.Select((t, i) => t == ColumnType.Date    ? new List<double?>() : null).ToArray();

        // Pass 2: build row store and columnar stores in one sweep
        foreach (var row in dataArray.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object) continue;
            var cells = new string[colCount];
            for (int i = 0; i < colCount; i++)
            {
                if (!row.TryGetProperty(headers[i], out var val))
                {
                    cells[i] = "";
                    numericLists[i]?.Add(null);
                    dateLists[i]?.Add(null);
                    continue;
                }
                cells[i] = CellString(val);
                if (numericLists[i] != null)
                    numericLists[i]!.Add(val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d) ? d : null);
                if (dateLists[i] != null)
                    dateLists[i]!.Add(val.ValueKind == JsonValueKind.String && DateTime.TryParse(val.GetString(), out var dt) ? dt.ToOADate() : null);
            }
            rows.Add(cells);
        }

        return new CachedDataset
        {
            Name           = name,
            CachedAt       = DateTime.UtcNow,
            FilePath       = "",
            Columns        = columns,
            Rows           = rows,
            NumericColumns = numericLists.Select((l, i) => (i, l)).Where(x => x.l != null).ToDictionary(x => x.i, x => x.l!.ToArray()),
            DateColumns    = dateLists.Select((l, i) => (i, l)).Where(x => x.l != null).ToDictionary(x => x.i, x => x.l!.ToArray())
        };
    }

    private static string CellString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        JsonValueKind.Null   => "",
        _                    => el.GetRawText()
    };

    internal static string BuildDatasetName(string app, string operationId)
    {
        var raw = $"{app}_{operationId}";
        return string.Concat(raw.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));
    }
}
