using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Loads and merges Swagger specs from multiple API sources (PLM, ERP, SFC, etc.).
/// All sources are fetched in parallel on first use and merged into a single indexed corpus.
/// All lookups are O(1) via in-memory indexes built once at load time.
/// </summary>
public class SwaggerService : ISwaggerService
{
    private readonly MultiSourceApiSettings _settings;
    private readonly ILogger<SwaggerService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILlmEnrichmentService _enrichmentService;
    private readonly ISemanticSearchService _semanticSearch;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    // Primary cache
    private List<SwaggerEndpoint>? _cachedEndpoints;
    private string? _cachedRawSpec;   // first source only (legacy compat)
    private string? _cachedBaseUrl;   // first source only (legacy compat)

    // O(1) lookup indexes built at parse time
    private Dictionary<string, SwaggerEndpoint>? _byOperationId;
    private Dictionary<string, List<SwaggerEndpoint>>? _byTag;
    private Dictionary<string, List<SwaggerEndpoint>>? _byApp;
    private IReadOnlyList<string>? _sortedTags;
    private IReadOnlyList<string>? _sortedApps;
    private IReadOnlyList<(string Tag, int Count)>? _tagSummary;

    // Inverted index: token → set of endpoint list indices
    private Dictionary<string, HashSet<int>>? _invertedIndex;

    private static readonly HashSet<string> _httpMethods = new(StringComparer.OrdinalIgnoreCase)
        { "get", "post", "put", "delete", "patch", "head", "options", "trace" };

    public SwaggerService(
        IOptions<MultiSourceApiSettings> settings,
        ILogger<SwaggerService> logger,
        IHttpClientFactory httpClientFactory,
        ILlmEnrichmentService enrichmentService,
        ISemanticSearchService semanticSearch)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _enrichmentService = enrichmentService;
        _semanticSearch = semanticSearch;
    }

    // ── Public interface ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SwaggerEndpoint>> GetAllEndpointsAsync()
    {
        await EnsureLoadedAsync();
        return _cachedEndpoints!;
    }

    public async Task<IReadOnlyList<SwaggerEndpoint>> GetEndpointsByTagAsync(string tag)
    {
        await EnsureLoadedAsync();
        return _byTag!.TryGetValue(tag, out var list) ? list : [];
    }

    public async Task<IReadOnlyList<SwaggerEndpoint>> GetEndpointsByAppAsync(string appSource)
    {
        await EnsureLoadedAsync();
        return _byApp!.TryGetValue(appSource, out var list) ? list : [];
    }

    public async Task<SwaggerEndpoint?> GetEndpointByOperationIdAsync(string operationId)
    {
        await EnsureLoadedAsync();
        _byOperationId!.TryGetValue(operationId, out var ep);
        return ep;
    }

    public async Task<IReadOnlyList<SwaggerEndpoint>> SearchEndpointsAsync(string keyword)
    {
        await EnsureLoadedAsync();

        var tokens = Tokenize(keyword).ToArray();
        if (tokens.Length == 0) return [];

        HashSet<int>? matchingIndices = null;
        foreach (var token in tokens)
        {
            if (!_invertedIndex!.TryGetValue(token, out var hits))
                return [];

            if (matchingIndices == null)
                matchingIndices = new HashSet<int>(hits);
            else
                matchingIndices.IntersectWith(hits);
        }

        if (matchingIndices == null || matchingIndices.Count == 0) return [];

        return matchingIndices
            .Select(i => _cachedEndpoints![i])
            .OrderBy(e => e.AppSource)
            .ThenBy(e => e.Tag)
            .ThenBy(e => e.OperationId)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetAllTagsAsync()
    {
        await EnsureLoadedAsync();
        return _sortedTags!;
    }

    public async Task<IReadOnlyList<string>> GetAllAppSourcesAsync()
    {
        await EnsureLoadedAsync();
        return _sortedApps!;
    }

    public async Task<IReadOnlyList<(string Tag, int Count)>> GetTagSummaryAsync()
    {
        await EnsureLoadedAsync();
        return _tagSummary!;
    }

    public async Task<string> GetRawSpecificationAsync()
    {
        await EnsureLoadedAsync();
        return _cachedRawSpec!;
    }

    public async Task<string?> GetBaseUrlAsync()
    {
        await EnsureLoadedAsync();
        return _cachedBaseUrl;
    }

    // ── Load & index ────────────────────────────────────────────────────────

    private async Task EnsureLoadedAsync()
    {
        if (_cachedEndpoints != null) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedEndpoints != null) return;

            var sources = _settings.Sources;
            if (sources.Count == 0)
            {
                _logger.LogWarning("No API sources configured in ApiSources:Sources");
                _cachedEndpoints = [];
                BuildIndexes(_cachedEndpoints);
                return;
            }

            // Load all sources in parallel
            var tasks = sources.Select(s => LoadAndParseSourceAsync(s)).ToArray();
            var results = await Task.WhenAll(tasks);

            _cachedEndpoints = results.SelectMany(r => r).ToList();
            BuildIndexes(_cachedEndpoints);

            // Build semantic vector index per source in parallel (no-op when embedding provider is disabled)
            await Task.WhenAll(sources.Zip(results, (s, r) => _semanticSearch.IndexAsync(s.Name, r)));

            var sourceSummary = sources.Zip(results, (s, r) => $"{s.Name}({r.Count})");
            _logger.LogInformation(
                "Loaded {Total} endpoints from {SourceCount} sources: {Sources}",
                _cachedEndpoints.Count,
                sources.Count,
                string.Join(", ", sourceSummary));
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task InvalidateCacheAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            _cachedEndpoints = null;
            _cachedRawSpec = null;
            _cachedBaseUrl = null;
            _byOperationId = null;
            _byTag = null;
            _byApp = null;
            _sortedTags = null;
            _sortedApps = null;
            _tagSummary = null;
            _invertedIndex = null;
            _logger.LogInformation("SwaggerService cache invalidated — will reload on next request");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private void BuildIndexes(List<SwaggerEndpoint> endpoints)
    {
        // O(1) operationId lookup
        _byOperationId = new Dictionary<string, SwaggerEndpoint>(endpoints.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var ep in endpoints)
            _byOperationId.TryAdd(ep.OperationId, ep);

        // O(1) tag lookup
        _byTag = endpoints
            .Where(e => e.Tag != null)
            .GroupBy(e => e.Tag!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // O(1) app lookup
        _byApp = endpoints
            .GroupBy(e => e.AppSource, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        _sortedTags = _byTag.Keys.OrderBy(t => t).ToList();
        _sortedApps = _byApp.Keys.OrderBy(a => a).ToList();

        _tagSummary = _sortedTags
            .Select(t => (Tag: t, Count: _byTag[t].Count))
            .ToList();

        _invertedIndex = BuildInvertedIndex(endpoints);
    }

    private static Dictionary<string, HashSet<int>> BuildInvertedIndex(List<SwaggerEndpoint> endpoints)
    {
        var index = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < endpoints.Count; i++)
        {
            var e = endpoints[i];
            var text = $"{e.AppSource} {e.Path} {e.OperationId} {e.Summary} {e.Description} {e.Tag}";

            foreach (var token in Tokenize(text))
            {
                if (!index.TryGetValue(token, out var set))
                    index[token] = set = [];
                set.Add(i);
            }
        }

        return index;
    }

    private static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        foreach (var part in text.Split([' ', '/', '_', '-', '.', '(', ')', '{', '}'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Length < 2) continue;

            yield return part;

            // Also split CamelCase so "FabricRetrieve" → ["Fabric", "Retrieve"]
            foreach (var word in SplitCamelCase(part))
            {
                if (word.Length >= 2 && !string.Equals(word, part, StringComparison.OrdinalIgnoreCase))
                    yield return word;
            }
        }
    }

    /// <summary>
    /// Resolves a JSON Schema element: if it's a $ref, follows it through
    /// #/components/schemas and expands properties recursively (max 3 levels).
    /// </summary>
    private static string ResolveSchemaToJson(JsonElement schemaEl, JsonElement rootDoc, int depth = 0)
    {
        const int maxDepth = 3;
        if (depth >= maxDepth) return schemaEl.ToString();

        // Follow $ref
        if (schemaEl.TryGetProperty("$ref", out var refEl))
        {
            var refPath = refEl.GetString();
            if (refPath?.StartsWith("#/") == true)
            {
                var current = rootDoc;
                foreach (var part in refPath[2..].Split('/'))
                {
                    if (!current.TryGetProperty(part, out current))
                        return schemaEl.ToString();
                }
                return ResolveSchemaToJson(current, rootDoc, depth + 1);
            }
            return schemaEl.ToString();
        }

        // If the schema has properties, resolve any $ref within each property
        if (schemaEl.ValueKind == JsonValueKind.Object &&
            schemaEl.TryGetProperty("properties", out var props))
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();

            foreach (var member in schemaEl.EnumerateObject())
            {
                if (member.Name == "properties")
                {
                    writer.WritePropertyName("properties");
                    writer.WriteStartObject();
                    foreach (var p in props.EnumerateObject())
                    {
                        writer.WritePropertyName(p.Name);
                        var resolved = ResolveSchemaToJson(p.Value, rootDoc, depth + 1);
                        using var pd = JsonDocument.Parse(resolved);
                        pd.RootElement.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WritePropertyName(member.Name);
                    member.Value.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        return schemaEl.ToString();
    }

    private static IEnumerable<string> SplitCamelCase(string text)
    {
        int start = 0;
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) && char.IsLower(text[i - 1]))
            {
                yield return text[start..i];
                start = i;
            }
        }
        if (start < text.Length)
            yield return text[start..];
    }

    // ── Per-source loading ──────────────────────────────────────────────────

    private async Task<List<SwaggerEndpoint>> LoadAndParseSourceAsync(ApiSourceConfig source)
    {
        try
        {
            _logger.LogInformation("Loading Swagger spec for source '{Name}'", source.Name);
            var json = await LoadSpecAsync(source);

            // Store first source's raw spec and base URL for legacy compat
            if (_cachedRawSpec == null)
            {
                _cachedRawSpec = json;
            }

            var endpoints = ParseSwaggerSpec(json, source);

            // Overlay LLM-enriched agent descriptions when enrichment is enabled.
            // The enrichment service returns an empty dict instantly when disabled.
            var enriched = await _enrichmentService.EnrichAsync(source.Name, json, endpoints);
            if (enriched.Count > 0)
            {
                endpoints = endpoints
                    .Select(ep => enriched.TryGetValue(ep.OperationId, out var rich)
                        ? ep with
                        {
                            AgentInstructions = string.IsNullOrEmpty(rich.AgentDescription)
                                ? ep.AgentInstructions
                                : rich.AgentDescription,
                            PromptHelp = rich.PromptHelp ?? ep.PromptHelp
                        }
                        : ep)
                    .ToList();
            }

            return endpoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Swagger spec for source '{Name}' — skipping", source.Name);
            return [];
        }
    }

    private async Task<string> LoadSpecAsync(ApiSourceConfig source)
    {
        var path = source.SwaggerJsonPath;

        if (Uri.TryCreate(path, UriKind.Absolute, out var fullUri) && fullUri.Scheme.StartsWith("http"))
        {
            _logger.LogInformation("Fetching '{Name}' spec from {Url}", source.Name, fullUri);
            return await FetchFromUrlAsync(fullUri.ToString(), source);
        }

        var localPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(Directory.GetCurrentDirectory(), path);

        if (File.Exists(localPath))
        {
            _logger.LogInformation("Loading '{Name}' spec from local file {Path}", source.Name, localPath);
            return await File.ReadAllTextAsync(localPath);
        }

        var baseUrl = source.BaseUrl.TrimEnd('/');
        var relPath = path.TrimStart('/');
        var url = $"{baseUrl}/{relPath}";
        _logger.LogInformation("Fetching '{Name}' spec from {Url}", source.Name, url);
        return await FetchFromUrlAsync(url, source);
    }

    private async Task<string> FetchFromUrlAsync(string url, ApiSourceConfig source)
    {
        // Use the named client so the per-source resilience pipeline applies to spec fetches too.
        using var client = _httpClientFactory.CreateClient(source.Name);

        if (!string.IsNullOrEmpty(source.AccessTokenValue))
            client.DefaultRequestHeaders.TryAddWithoutValidation(source.AccessTokenHeaderName, source.AccessTokenValue);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private List<SwaggerEndpoint> ParseSwaggerSpec(string json, ApiSourceConfig source)
    {
        var endpoints = new List<SwaggerEndpoint>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Capture base URL from first source
        if (_cachedBaseUrl == null &&
            root.TryGetProperty("servers", out var servers) &&
            servers.GetArrayLength() > 0 &&
            servers[0].TryGetProperty("url", out var url))
        {
            _cachedBaseUrl = url.GetString();
        }

        if (!root.TryGetProperty("paths", out var paths))
        {
            _logger.LogWarning("No paths found in '{Name}' spec", source.Name);
            return endpoints;
        }

        foreach (var path in paths.EnumerateObject())
        {
            var pathString = path.Name;

            foreach (var method in path.Value.EnumerateObject())
            {
                // Skip non-operation keys (e.g., path-level "parameters" in OpenAPI 3.x)
                if (!_httpMethods.Contains(method.Name)) continue;

                var methodName = method.Name.ToUpperInvariant();
                var operation = method.Value;

                var rawOperationId = operation.TryGetProperty("operationId", out var opId)
                    ? opId.GetString() ?? $"{methodName}_{pathString}"
                    : $"{methodName}_{pathString}";
                var operationId = rawOperationId.EndsWith("Async", StringComparison.Ordinal)
                    ? rawOperationId[..^5]
                    : rawOperationId;

                var summary = operation.TryGetProperty("summary", out var sum) ? sum.GetString() : null;
                var description = operation.TryGetProperty("description", out var desc) ? desc.GetString() : null;

                string? tag = null;
                if (operation.TryGetProperty("tags", out var tags) && tags.GetArrayLength() > 0)
                    tag = tags[0].GetString();

                var parameters = new List<EndpointParameter>();
                if (operation.TryGetProperty("parameters", out var paramArray))
                {
                    foreach (var param in paramArray.EnumerateArray())
                    {
                        if (!param.TryGetProperty("name", out var nameEl) || !param.TryGetProperty("in", out var inEl))
                        {
                            _logger.LogWarning("Skipping malformed parameter (missing 'name' or 'in') in '{Name}' spec path {Path}", source.Name, pathString);
                            continue;
                        }
                        var paramName = nameEl.GetString()!;
                        var paramIn = inEl.GetString()!;

                        string? paramType = null;
                        if (param.TryGetProperty("schema", out var schema) &&
                            schema.TryGetProperty("type", out var type))
                            paramType = type.GetString();

                        var paramDesc = param.TryGetProperty("description", out var pd) ? pd.GetString() : null;
                        var required = param.TryGetProperty("required", out var req) && req.GetBoolean();
                        var example = param.TryGetProperty("example", out var ex) ? ex.ToString() : null;

                        parameters.Add(new EndpointParameter
                        {
                            Name = paramName,
                            In = paramIn,
                            Type = paramType,
                            Description = paramDesc,
                            Required = required,
                            Example = example
                        });
                    }
                }

                bool requiresBody = false;
                string? requestBodySchema = null;
                string? requestBodyExample = null;
                if (operation.TryGetProperty("requestBody", out var reqBody))
                {
                    requiresBody = reqBody.TryGetProperty("required", out var rbReq) && rbReq.GetBoolean();
                    if (reqBody.TryGetProperty("content", out var content))
                    {
                        foreach (var contentType in content.EnumerateObject())
                        {
                            var ct = contentType.Value;

                            if (ct.TryGetProperty("schema", out var schemaRef))
                            {
                                var resolved = ResolveSchemaToJson(schemaRef, root);
                                requestBodySchema = resolved;

                                // Pull example from schema-level "example" if present
                                if (requestBodyExample == null)
                                {
                                    try
                                    {
                                        using var sd = JsonDocument.Parse(resolved);
                                        if (sd.RootElement.TryGetProperty("example", out var schEx))
                                            requestBodyExample = schEx.ToString();
                                    }
                                    catch (Exception parseEx) { _logger.LogDebug(parseEx, "Failed to parse schema example JSON for endpoint '{OperationId}'", operationId); }
                                }
                            }

                            // content[type].example (single)
                            if (requestBodyExample == null && ct.TryGetProperty("example", out var ex))
                                requestBodyExample = ex.ToString();

                            // content[type].examples (named map) — take first value
                            if (requestBodyExample == null && ct.TryGetProperty("examples", out var exMap))
                            {
                                foreach (var named in exMap.EnumerateObject())
                                {
                                    if (named.Value.TryGetProperty("value", out var val))
                                    {
                                        requestBodyExample = val.ToString();
                                        break;
                                    }
                                }
                            }

                            if (requestBodySchema != null) break;
                        }
                    }
                }

                endpoints.Add(new SwaggerEndpoint
                {
                    AppSource = source.Name,
                    Path = pathString,
                    Method = methodName,
                    OperationId = operationId,
                    Summary = summary,
                    Description = description,
                    Tag = tag,
                    Parameters = parameters,
                    RequiresRequestBody = requiresBody,
                    RequestBodySchema = requestBodySchema,
                    RequestBodyExample = requestBodyExample
                });
            }
        }

        return endpoints;
    }

    // ── Hybrid search ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<HybridSearchResult>> HybridSearchAsync(
        string intent, string? appSource = null, int topK = 5, CancellationToken ct = default)
    {
        await EnsureLoadedAsync();

        // RAG candidates — returns [] when embedding provider is not configured
        var semanticHits = await _semanticSearch.SearchAsync(intent, appSource, topK * 2, ct);
        var semanticScore = semanticHits.ToDictionary(
            r => r.Endpoint.OperationId,
            r => r.Score,
            StringComparer.OrdinalIgnoreCase);

        // Inverted-index candidates (keyword exact/partial match)
        var keywordHits = await SearchEndpointsAsync(intent);
        if (!string.IsNullOrEmpty(appSource))
            keywordHits = keywordHits
                .Where(e => string.Equals(e.AppSource, appSource, StringComparison.OrdinalIgnoreCase))
                .ToList();
        var exactMatchIds = keywordHits
            .Select(e => e.OperationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Union all candidate operationIds
        var allIds = new HashSet<string>(semanticScore.Keys, StringComparer.OrdinalIgnoreCase);
        allIds.UnionWith(exactMatchIds);

        if (allIds.Count == 0) return [];

        // Score = cosine similarity + bonus for exact keyword match
        const float exactMatchBonus = 0.3f;

        return allIds
            .Select(id =>
            {
                if (!_byOperationId!.TryGetValue(id, out var ep)) return null;
                var sem = semanticScore.TryGetValue(id, out var s) ? s : 0f;
                var exact = exactMatchIds.Contains(id);
                return new HybridSearchResult(ep, sem + (exact ? exactMatchBonus : 0f), exact);
            })
            .Where(r => r != null)
            .Select(r => r!)
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }
}
