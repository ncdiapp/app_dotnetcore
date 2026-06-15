using System.Net.Http.Headers;
using System.Text;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace AppAI.Web.Services;

/// <summary>
/// OCR implementation that sends the image to the configured LLM vision provider
/// (OpenAI gpt-4o, Anthropic Claude, or Gemini) and returns the extracted text.
/// </summary>
public sealed class LLMOcrService : IOcrService
{
    private const string OcrPrompt = "Extract all text visible in this image. Return only the extracted text with no explanations or additional commentary.";

    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public LLMOcrService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> ExtractTextAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default)
    {
        var provider = LLMProviderHelper.GetConfiguredProvider();
        var apiKey = LLMProviderHelper.GetConfiguredApiKey();
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var base64 = Convert.ToBase64String(imageBytes);

        return provider switch
        {
            EmLLMProvider.OpenAI => await CallOpenAIVisionAsync(apiKey, base64, mimeType, ct),
            EmLLMProvider.Anthropic => await CallAnthropicVisionAsync(apiKey, base64, mimeType, ct),
            EmLLMProvider.Gemini => await CallGeminiVisionAsync(apiKey, base64, mimeType, ct),
            _ => null
        };
    }

    private async Task<string?> CallOpenAIVisionAsync(string apiKey, string base64, string mimeType, CancellationToken ct)
    {
        var model = _config["DbaGenieOpenAIModel"] ?? "gpt-4o";
        var payload = new
        {
            model,
            max_tokens = 2048,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64}" } },
                        new { type = "text", text = OcrPrompt }
                    }
                }
            }
        };

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var response = await client.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            JsonContent(payload), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) return null;
        var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        return json["choices"]?[0]?["message"]?["content"]?.ToString();
    }

    private async Task<string?> CallAnthropicVisionAsync(string apiKey, string base64, string mimeType, CancellationToken ct)
    {
        var model = _config["DbaGenieAnthropicModel"] ?? "claude-3-5-sonnet-20241022";
        var payload = new
        {
            model,
            max_tokens = 2048,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image", source = new { type = "base64", media_type = mimeType, data = base64 } },
                        new { type = "text", text = OcrPrompt }
                    }
                }
            }
        };

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        var response = await client.PostAsync(
            "https://api.anthropic.com/v1/messages",
            JsonContent(payload), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) return null;
        var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        return json["content"]?[0]?["text"]?.ToString();
    }

    private async Task<string?> CallGeminiVisionAsync(string apiKey, string base64, string mimeType, CancellationToken ct)
    {
        var model = _config["DbaGenieGeminiModel"] ?? "gemini-1.5-pro";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inline_data = new { mime_type = mimeType, data = base64 } },
                        new { text = OcrPrompt }
                    }
                }
            }
        };

        using var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var response = await client.PostAsync(url, JsonContent(payload), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) return null;
        var json = JObject.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
    }

    private static StringContent JsonContent(object payload)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
