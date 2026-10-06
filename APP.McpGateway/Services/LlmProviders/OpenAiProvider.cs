using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services.LlmProviders;

/// <summary>
/// Calls the OpenAI Chat Completions API (ChatGPT models).
/// Endpoint: POST https://api.openai.com/v1/chat/completions
/// </summary>
public class OpenAiProvider : ILlmProvider
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    private readonly OpenAiProviderConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiProvider> _logger;

    public string ProviderName => "OpenAI";

    public OpenAiProvider(
        IOptions<LlmEnrichmentSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiProvider> logger)
    {
        _config = settings.Value.OpenAI;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = _config.Model,
            max_tokens = 8096,
            messages = new[] { new { role = "user", content = prompt } }
        });

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_config.ApiKey}");
            client.Timeout = TimeSpan.FromMinutes(3);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(ApiUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenAI API {Status}: {Error}", (int)response.StatusCode,
                    err[..Math.Min(300, err.Length)]);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI API call failed");
            return null;
        }
    }
}
