using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpGateway.Models;
using McpGateway.Services.LlmProviders;

namespace McpGateway.Services;

/// <summary>
/// Enriches parsed Swagger endpoints with LLM-generated agent descriptions.
/// Delegates the actual LLM call to the configured <see cref="ILlmProvider"/>.
/// Uses a SHA-256-keyed disk cache so the provider is only called when the spec changes.
/// </summary>
public class LlmEnrichmentService : ILlmEnrichmentService
{
    private readonly LlmEnrichmentSettings _settings;
    private readonly IRuntimeConfigService _runtimeConfig;
    private readonly ILlmProvider _provider;
    private readonly ILogger<LlmEnrichmentService> _logger;

    // Provider-agnostic master prompt — the structure is the same regardless of which LLM processes it.
    private const string MasterPrompt =
        """
        Role: You are an Expert AI Systems Architect specializing in the Model Context Protocol (MCP).
        Your task is to transform the provided API endpoint documentation into high-precision MCP Tool Definitions.

        Goal: For every endpoint provided, generate one JSON object with exactly these fields:
          - operationId: The exact operationId as given — do not change it.
          - name: An action-oriented snake_case string (e.g., get_inventory_status).
          - description: A detailed 3-4 sentence "Agent Instruction" that covers:
              (1) The "When" — which user intent or business action triggers this tool.
              (2) Business context and logical prerequisites (e.g., token required, parent record must exist).
              (3) The "Caution" — whether this is destructive (POST/PUT/DELETE) or has side effects.
              (4) The "Format" — any data pre-processing the agent must do (dates → ISO-8601, enums, etc.).
          - inputSchema: A strict JSON Schema object mapping all query, path, and body parameters,
              each with a clear description and enums where values are constrained.
          - prompt_help: A "System Note" for the agent covering edge cases, mandatory call ordering
              (e.g., "Must call auth_login first"), pagination patterns, or response-parsing hints.

        Return ONLY a valid JSON array — no prose, no markdown code fences, no commentary.
        Every element must include all five fields above.
        """;

    public LlmEnrichmentService(
        IOptions<LlmEnrichmentSettings> settings,
        IRuntimeConfigService runtimeConfig,
        ILlmProvider provider,
        ILogger<LlmEnrichmentService> logger)
    {
        _settings = settings.Value;
        _runtimeConfig = runtimeConfig;
        _provider = provider;
        _logger = logger;
    }

    // ── Public interface ────────────────────────────────────────────────────

    public async Task<IReadOnlyDictionary<string, EnrichedEndpointData>> EnrichAsync(
        string appName,
        string swaggerJson,
        IReadOnlyList<SwaggerEndpoint> endpoints)
    {
        if (!_runtimeConfig.IsLlmEnrichmentEnabled())
        {
            _logger.LogDebug("LLM enrichment disabled — skipping '{App}'", appName);
            return new Dictionary<string, EnrichedEndpointData>();
        }

        var hash = ComputeSha256(swaggerJson);
        var cacheFile = BuildCachePath(appName, hash);

        if (File.Exists(cacheFile))
        {
            _logger.LogInformation("LLM enrichment cache hit for '{App}' via {Provider} (sha256 prefix: {Hash})",
                appName, _provider.ProviderName, hash[..12]);
            return await LoadCacheAsync(cacheFile);
        }

        _logger.LogInformation(
            "LLM enrichment cache miss for '{App}' — calling {Provider} ({Count} endpoints, batches of {BatchSize})",
            appName, _provider.ProviderName, endpoints.Count, _settings.MaxEndpointsPerBatch);

        var enriched = await EnrichInBatchesAsync(appName, endpoints);
        await SaveCacheAsync(cacheFile, enriched);

        _logger.LogInformation(
            "LLM enrichment complete for '{App}' via {Provider}: {Count}/{Total} endpoints enriched",
            appName, _provider.ProviderName, enriched.Count, endpoints.Count);

        return enriched;
    }

    // ── Batching ────────────────────────────────────────────────────────────

    private async Task<Dictionary<string, EnrichedEndpointData>> EnrichInBatchesAsync(
        string appName,
        IReadOnlyList<SwaggerEndpoint> endpoints)
    {
        var result = new Dictionary<string, EnrichedEndpointData>(StringComparer.OrdinalIgnoreCase);
        var batchSize = Math.Max(1, _settings.MaxEndpointsPerBatch);

        var batches = endpoints
            .Select((ep, i) => (ep, batch: i / batchSize))
            .GroupBy(x => x.batch)
            .Select(g => g.Select(x => x.ep).ToList())
            .ToList();

        for (int i = 0; i < batches.Count; i++)
        {
            _logger.LogDebug("Enriching '{App}' batch {Num}/{Total} ({Count} endpoints)",
                appName, i + 1, batches.Count, batches[i].Count);

            var batchResult = await CallProviderForBatchAsync(appName, batches[i]);
            foreach (var (key, value) in batchResult)
                result[key] = value;

            if (i < batches.Count - 1)
                await Task.Delay(350); // be polite to rate limits
        }

        return result;
    }

    // ── Provider call ───────────────────────────────────────────────────────

    private async Task<Dictionary<string, EnrichedEndpointData>> CallProviderForBatchAsync(
        string appName,
        IReadOnlyList<SwaggerEndpoint> batch)
    {
        // Send a compact endpoint summary — not the full swagger JSON — to minimise token usage
        var summaries = batch.Select(ep => new
        {
            operationId = ep.OperationId,
            method = ep.Method,
            path = ep.Path,
            tag = ep.Tag,
            summary = ep.Summary,
            description = ep.Description,
            parameters = ep.Parameters.Select(p => new
            {
                name = p.Name,
                @in = p.In,
                type = p.Type,
                description = p.Description,
                required = p.Required
            }),
            requiresRequestBody = ep.RequiresRequestBody,
            requestBodySchema = ep.RequestBodySchema
        });

        var prompt = $"{MasterPrompt}\n\nApp system: {appName}\n\nEndpoints:\n{JsonSerializer.Serialize(summaries)}";

        var text = await _provider.CompleteAsync(prompt);
        if (text is null)
            return [];

        return ParseProviderResponse(text);
    }

    // ── Response parsing ────────────────────────────────────────────────────

    private Dictionary<string, EnrichedEndpointData> ParseProviderResponse(string text)
    {
        var jsonArray = ExtractJsonArray(text);
        if (jsonArray is null)
        {
            _logger.LogWarning("{Provider} response did not contain a JSON array: {Preview}",
                _provider.ProviderName, text[..Math.Min(300, text.Length)]);
            return [];
        }

        var result = new Dictionary<string, EnrichedEndpointData>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var arr = JsonDocument.Parse(jsonArray);
            foreach (var item in arr.RootElement.EnumerateArray())
            {
                var operationId = item.TryGetProperty("operationId", out var opId)
                    ? opId.GetString()
                    : null;

                if (string.IsNullOrEmpty(operationId))
                {
                    _logger.LogDebug("{Provider} returned an item with no operationId — skipping",
                        _provider.ProviderName);
                    continue;
                }

                result[operationId] = new EnrichedEndpointData
                {
                    OperationId = operationId,
                    Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                    AgentDescription = item.TryGetProperty("description", out var desc)
                        ? desc.GetString() ?? "" : "",
                    PromptHelp = item.TryGetProperty("prompt_help", out var ph)
                        ? ph.GetString() : null,
                    InputSchemaJson = item.TryGetProperty("inputSchema", out var schema)
                        ? schema.ToString() : null
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "{Provider} enrichment response could not be deserialized",
                _provider.ProviderName);
        }

        return result;
    }

    /// <summary>Extracts the first complete [...] block from text that may contain prose.</summary>
    private static string? ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    // ── Disk cache ──────────────────────────────────────────────────────────

    private string BuildCachePath(string appName, string hash)
    {
        var dir = Path.IsPathRooted(_settings.CacheDirectory)
            ? _settings.CacheDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), _settings.CacheDirectory);

        Directory.CreateDirectory(dir);

        var safeName = new string(appName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return Path.Combine(dir, $"{safeName}-{hash[..16]}.json");
    }

    private async Task<Dictionary<string, EnrichedEndpointData>> LoadCacheAsync(string cacheFile)
    {
        try
        {
            var json = await File.ReadAllTextAsync(cacheFile);
            var list = JsonSerializer.Deserialize<List<EnrichedEndpointData>>(json) ?? [];
            return list.ToDictionary(x => x.OperationId, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load enrichment cache '{File}' — will re-enrich", cacheFile);
            return [];
        }
    }

    private async Task SaveCacheAsync(string cacheFile, Dictionary<string, EnrichedEndpointData> enriched)
    {
        try
        {
            var json = JsonSerializer.Serialize(
                enriched.Values.ToList(),
                new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(cacheFile, json);
            _logger.LogInformation("Saved enrichment cache: {File}", cacheFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save enrichment cache '{File}'", cacheFile);
        }
    }

    // ── Utilities ───────────────────────────────────────────────────────────

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

}
