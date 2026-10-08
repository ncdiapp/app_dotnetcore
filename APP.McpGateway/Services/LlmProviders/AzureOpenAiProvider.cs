using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services.LlmProviders;

/// <summary>
/// Calls an Azure AI Foundry / Azure OpenAI deployment.
/// Endpoint: POST {Endpoint}/openai/deployments/{DeploymentName}/chat/completions?api-version={ApiVersion}
/// </summary>
public class AzureOpenAiProvider : ILlmProvider
{
    private readonly AzureOpenAiProviderConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureOpenAiProvider> _logger;

    public string ProviderName => "AzureOpenAI";

    public AzureOpenAiProvider(
        IOptions<LlmEnrichmentSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<AzureOpenAiProvider> logger)
    {
        _config = settings.Value.AzureOpenAI;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var endpoint = _config.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{_config.DeploymentName}/chat/completions?api-version={_config.ApiVersion}";

        var body = JsonSerializer.Serialize(new
        {
            max_tokens = 8096,
            messages = new[] { new { role = "user", content = prompt } }
        });

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("api-key", _config.ApiKey);
            client.Timeout = TimeSpan.FromMinutes(3);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Azure OpenAI API {Status}: {Error}", (int)response.StatusCode,
                    err[..Math.Min(300, err.Length)]);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            // Same response shape as OpenAI
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI API call failed");
            return null;
        }
    }
}
