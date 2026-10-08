namespace McpGateway.Services;

public interface IEmbeddingProvider
{
    /// <summary>True when the provider has credentials and is ready to serve embeddings.</summary>
    bool IsAvailable { get; }

    /// <summary>Returns one embedding vector per input text, or null on failure / when unavailable.</summary>
    Task<float[][]?> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}
