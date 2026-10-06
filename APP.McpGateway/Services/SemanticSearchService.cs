using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Builds a flat in-memory cosine-similarity index over endpoint embedding vectors.
/// Vectors are persisted to disk (keyed by SHA-256 of the embedding texts) so that
/// subsequent restarts skip the embedding API call when the spec hasn't changed.
/// </summary>
public class SemanticSearchService : ISemanticSearchService
{
    private readonly IEmbeddingProvider _provider;
    private readonly SemanticSearchSettings _settings;
    private readonly ILogger<SemanticSearchService> _logger;

    private readonly object _lock = new();
    private readonly Dictionary<string, List<(SwaggerEndpoint ep, float[] vector)>> _byApp
        = new(StringComparer.OrdinalIgnoreCase);
    private List<(SwaggerEndpoint ep, float[] vector)> _allIndexed = [];

    public bool IsAvailable => _provider.IsAvailable;

    public SemanticSearchService(
        IEmbeddingProvider embeddingProvider,
        IOptions<SemanticSearchSettings> settings,
        ILogger<SemanticSearchService> logger)
    {
        _provider = embeddingProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task IndexAsync(string appSource, IReadOnlyList<SwaggerEndpoint> endpoints, CancellationToken ct = default)
    {
        if (!_provider.IsAvailable || endpoints.Count == 0) return;

        var texts = endpoints.Select(BuildEmbeddingText).ToList();
        var hash = ComputeHash(texts);
        var cacheFile = GetCacheFilePath(appSource, hash);

        float[][]? vectors = await TryLoadCacheAsync(cacheFile, _logger);

        if (vectors == null)
        {
            _logger.LogInformation("Building semantic index for '{App}' ({Count} endpoints)...", appSource, endpoints.Count);
            vectors = await _provider.GetEmbeddingsAsync(texts, ct);
            if (vectors == null)
            {
                _logger.LogWarning("Embedding provider returned null for '{App}' — semantic search unavailable for this source", appSource);
                return;
            }
            await SaveCacheAsync(cacheFile, vectors);
            _logger.LogInformation("Semantic index built and cached for '{App}'", appSource);
        }
        else
        {
            _logger.LogInformation("Loaded semantic index from cache for '{App}' ({Count} vectors)", appSource, vectors.Length);
        }

        var pairs = endpoints
            .Zip(vectors, (ep, vec) => (ep, vec))
            .ToList();

        lock (_lock)
        {
            _byApp[appSource] = pairs;
            _allIndexed = _byApp.Values.SelectMany(x => x).ToList();
        }
    }

    public async Task<IReadOnlyList<(SwaggerEndpoint Endpoint, float Score)>> SearchAsync(
        string intent, string? appSource = null, int topK = 5, CancellationToken ct = default)
    {
        if (!_provider.IsAvailable) return [];

        var queryVecs = await _provider.GetEmbeddingsAsync([intent], ct);
        if (queryVecs == null || queryVecs.Length == 0) return [];

        var q = queryVecs[0];

        List<(SwaggerEndpoint ep, float[] vector)> searchSpace;
        lock (_lock)
        {
            searchSpace = (!string.IsNullOrEmpty(appSource) && _byApp.TryGetValue(appSource, out var appList))
                ? new List<(SwaggerEndpoint, float[])>(appList)
                : new List<(SwaggerEndpoint, float[])>(_allIndexed);
        }

        if (searchSpace.Count == 0) return [];

        return searchSpace
            .Select(item => (item.ep, CosineSimilarity(q, item.vector)))
            .OrderByDescending(x => x.Item2)
            .Take(topK)
            .ToList();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string BuildEmbeddingText(SwaggerEndpoint ep)
    {
        var paramNames = ep.Parameters.Count > 0
            ? string.Join(" ", ep.Parameters.Select(p => p.Name))
            : "";
        return $"{ep.OperationId} {ep.Summary} {ep.Tag} {paramNames}".Trim();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        var denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return denom < 1e-8f ? 0f : dot / denom;
    }

    private static string ComputeHash(IReadOnlyList<string> texts)
    {
        var combined = string.Join("\n", texts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes)[..16];
    }

    private string GetCacheFilePath(string appSource, string hash)
    {
        var dir = Path.IsPathRooted(_settings.CacheDirectory)
            ? _settings.CacheDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), _settings.CacheDirectory);
        return Path.Combine(dir, $"{appSource}_embeddings_{hash}.json");
    }

    private static async Task<float[][]?> TryLoadCacheAsync(string path, ILogger<SemanticSearchService> logger)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("vectors");
            var result = new float[data.GetArrayLength()][];
            int i = 0;
            foreach (var row in data.EnumerateArray())
            {
                var vec = new float[row.GetArrayLength()];
                int j = 0;
                foreach (var val in row.EnumerateArray())
                    vec[j++] = val.GetSingle();
                result[i++] = vec;
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load embedding cache from '{Path}' — will rebuild", path);
            return null;
        }
    }

    private async Task SaveCacheAsync(string path, float[][] vectors)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(new { vectors });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save embedding cache to {Path}", path);
        }
    }
}
