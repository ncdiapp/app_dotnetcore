namespace McpGateway.Services.LlmProviders;

/// <summary>
/// Abstraction over an LLM API provider (Anthropic, OpenAI, Azure OpenAI, Gemini, etc.).
/// Implementations handle auth, request format, and response parsing.
/// </summary>
public interface ILlmProvider
{
    /// <summary>Display name shown in logs (e.g., "Anthropic", "OpenAI").</summary>
    string ProviderName { get; }

    /// <summary>
    /// Sends a prompt and returns the text response.
    /// Returns null when the call fails — callers treat null as "skip enrichment for this batch".
    /// </summary>
    Task<string?> CompleteAsync(string prompt, CancellationToken ct = default);
}
