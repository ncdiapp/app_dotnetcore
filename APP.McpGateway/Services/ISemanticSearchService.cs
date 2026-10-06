using McpGateway.Models;

namespace McpGateway.Services;

public interface ISemanticSearchService
{
    /// <summary>True when an embedding provider is configured and ready.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Builds (or restores from cache) the embedding vector index for all endpoints in one app source.
    /// Safe to call in parallel for different app sources.
    /// </summary>
    Task IndexAsync(string appSource, IReadOnlyList<SwaggerEndpoint> endpoints, CancellationToken ct = default);

    /// <summary>
    /// Returns the top-K endpoints closest to <paramref name="intent"/> by cosine similarity.
    /// Returns an empty list when the provider is unavailable or the index is empty.
    /// </summary>
    Task<IReadOnlyList<(SwaggerEndpoint Endpoint, float Score)>> SearchAsync(
        string intent, string? appSource = null, int topK = 5, CancellationToken ct = default);
}
