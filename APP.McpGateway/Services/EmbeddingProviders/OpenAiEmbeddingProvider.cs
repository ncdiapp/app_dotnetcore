using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services.EmbeddingProviders;

/// <summary>
/// Calls the OpenAI embeddings endpoint.
/// Endpoint: POST https://api.openai.com/v1/embeddings
/// </summary>
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly OpenAiEmbeddingConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;

    public bool IsAvailable => !string.IsNullOrEmpty(_config.ApiKey);

    public OpenAiEmbeddingProvider(
        IOptions<SemanticSearchSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiEmbeddingProvider> logger)
    {
        _config = settings.Value.OpenAI;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<float[][]?> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (!IsAvailable || texts.Count == 0) return null;

        var model = string.IsNullOrEmpty(_config.Model) ? "text-embedding-ada-002" : _config.Model;
        var body = JsonSerializer.Serialize(new { input = texts, model });

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_config.ApiKey}");
            client.Timeout = TimeSpan.FromMinutes(2);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/embeddings", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenAI Embeddings {Status}: {Error}",
                    (int)response.StatusCode, err[..Math.Min(300, err.Length)]);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            return ParseEmbeddingsResponse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI Embeddings call failed");
            return null;
        }
    }

    private static float[][]? ParseEmbeddingsResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        var result = new float[data.GetArrayLength()][];
        int i = 0;
        foreach (var item in data.EnumerateArray())
        {
            var embedding = item.GetProperty("embedding");
            var vec = new float[embedding.GetArrayLength()];
            int j = 0;
            foreach (var val in embedding.EnumerateArray())
                vec[j++] = val.GetSingle();
            result[i++] = vec;
        }
        return result;
    }
}
