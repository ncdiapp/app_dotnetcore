using McpGateway.Models;

namespace McpGateway.Services;


/// <summary>
/// Service for parsing and querying Swagger/OpenAPI specifications.
/// </summary>
public interface ISwaggerService
{
    /// <summary>
    /// Gets all endpoints from the loaded Swagger specification.
    /// </summary>
    Task<IReadOnlyList<SwaggerEndpoint>> GetAllEndpointsAsync();

    /// <summary>
    /// Gets endpoints filtered by tag.
    /// </summary>
    Task<IReadOnlyList<SwaggerEndpoint>> GetEndpointsByTagAsync(string tag);

    /// <summary>
    /// Gets a specific endpoint by operation ID.
    /// </summary>
    Task<SwaggerEndpoint?> GetEndpointByOperationIdAsync(string operationId);

    /// <summary>
    /// Searches endpoints by keyword in path, summary, or description.
    /// </summary>
    Task<IReadOnlyList<SwaggerEndpoint>> SearchEndpointsAsync(string keyword);

    /// <summary>
    /// Gets all unique tags from the specification.
    /// </summary>
    Task<IReadOnlyList<string>> GetAllTagsAsync();

    /// <summary>
    /// Gets the raw Swagger JSON specification.
    /// </summary>
    Task<string> GetRawSpecificationAsync();

    /// <summary>
    /// Gets the API base URL from the specification.
    /// </summary>
    Task<string?> GetBaseUrlAsync();

    /// <summary>
    /// Gets a summary of all tags with their endpoint counts.
    /// </summary>
    Task<IReadOnlyList<(string Tag, int Count)>> GetTagSummaryAsync();

    /// <summary>
    /// Gets all known app source names (e.g., "PLM", "ERP", "SFC").
    /// </summary>
    Task<IReadOnlyList<string>> GetAllAppSourcesAsync();

    /// <summary>
    /// Gets endpoints filtered by app source name.
    /// </summary>
    Task<IReadOnlyList<SwaggerEndpoint>> GetEndpointsByAppAsync(string appSource);

    /// <summary>
    /// Hybrid search: combines semantic (RAG) similarity with inverted-index keyword matching.
    /// Falls back to keyword-only when the embedding provider is not configured.
    /// </summary>
    Task<IReadOnlyList<HybridSearchResult>> HybridSearchAsync(
        string intent, string? appSource = null, int topK = 5, CancellationToken ct = default);

    /// <summary>
    /// Clears all in-memory caches so the next call re-fetches specs and re-runs enrichment.
    /// Use after toggling LlmEnrichment:Enabled to pick up the new setting.
    /// </summary>
    Task InvalidateCacheAsync();
}
