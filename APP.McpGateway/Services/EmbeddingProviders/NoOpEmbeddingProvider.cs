namespace McpGateway.Services.EmbeddingProviders;

/// <summary>
/// Stub embedding provider used when the configured LLM provider doesn't support embeddings
/// (e.g., Anthropic, Gemini). Semantic search is silently disabled.
/// </summary>
public class NoOpEmbeddingProvider : IEmbeddingProvider
{
    public bool IsAvailable => false;

    public Task<float[][]?> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
        => Task.FromResult<float[][]?>(null);
}
