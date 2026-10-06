using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Enriches parsed Swagger endpoints with LLM-generated agent instructions.
/// Uses a SHA-256-keyed disk cache so the LLM is only called when the spec changes.
/// </summary>
public interface ILlmEnrichmentService
{
    /// <summary>
    /// Returns a dictionary of enriched data keyed by operationId.
    /// Returns an empty dictionary when enrichment is disabled or the API key is missing.
    /// </summary>
    /// <param name="appName">Source name used as the cache file prefix (e.g., "PLM").</param>
    /// <param name="swaggerJson">Raw Swagger/OpenAPI JSON — hashed to detect changes.</param>
    /// <param name="endpoints">Parsed endpoints for this source.</param>
    Task<IReadOnlyDictionary<string, EnrichedEndpointData>> EnrichAsync(
        string appName,
        string swaggerJson,
        IReadOnlyList<SwaggerEndpoint> endpoints);
}
