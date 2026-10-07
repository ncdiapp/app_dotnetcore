using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services.LlmProviders;

/// <summary>
/// Calls the Anthropic Messages API (Claude models).
/// Endpoint: POST https://api.anthropic.com/v1/messages
/// </summary>
public class AnthropicProvider : ILlmProvider
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private readonly AnthropicProviderConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicProvider> _logger;

    public string ProviderName => "Anthropic";

    public AnthropicProvider(
        IOptions<LlmEnrichmentSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicProvider> logger)
    {
        _config = settings.Value.Anthropic;
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
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _config.ApiKey);
            client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", ApiVersion);
            client.Timeout = TimeSpan.FromMinutes(3);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(ApiUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Anthropic API {Status}: {Error}", (int)response.StatusCode,
                    err[..Math.Min(300, err.Length)]);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("content", out var contentArray)
                || contentArray.ValueKind != JsonValueKind.Array
                || contentArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("Anthropic response missing 'content' array. Body: {Body}",
                    json[..Math.Min(500, json.Length)]);
                return null;
            }

            var firstBlock = contentArray[0];
            if (!firstBlock.TryGetProperty("text", out var textEl))
            {
                var blockType = firstBlock.TryGetProperty("type", out var t) ? t.GetString() : "unknown";
                _logger.LogWarning("Anthropic content[0] has no 'text' field (type='{BlockType}')", blockType);
                return null;
            }

            return textEl.GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic API call failed");
            return null;
        }
    }
}
