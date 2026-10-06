using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services.LlmProviders;

/// <summary>
/// Calls the Google Gemini generateContent API.
/// Endpoint: POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={ApiKey}
/// </summary>
public class GeminiProvider : ILlmProvider
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private readonly GeminiProviderConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiProvider> _logger;

    public string ProviderName => "Gemini";

    public GeminiProvider(
        IOptions<LlmEnrichmentSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<GeminiProvider> logger)
    {
        _config = settings.Value.Gemini;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/{_config.Model}:generateContent?key={_config.ApiKey}";

        var body = JsonSerializer.Serialize(new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new { maxOutputTokens = 8096 }
        });

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Gemini API {Status}: {Error}", (int)response.StatusCode,
                    err[..Math.Min(300, err.Length)]);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini API call failed");
            return null;
        }
    }
}
