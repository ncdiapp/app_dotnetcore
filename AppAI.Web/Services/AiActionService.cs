using System.Net.Http.Headers;
using System.Text;
using App.BL.AppMgr.AiSkill;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Newtonsoft.Json.Linq;

namespace AppAI.Web.Services;

/// <summary>
/// Generic AI Action engine. Resolves the prompt from an AppAISkill record,
/// builds a multi-modal message (text + image blocks), calls the configured
/// LLM provider, and returns the raw JSON string the skill produces.
/// </summary>
public sealed class AiActionService : IAiActionService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AiActionService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AiActionResult?> ExecuteAsync(AiActionRequest request, CancellationToken ct = default)
    {
        var dsId = AppAISkillBL.GetDefaultDataSourceId();
        if (!dsId.HasValue) return null;

        var skill = AppAISkillBL.GetSkillByName(dsId.Value, request.SkillName);
        if (skill == null) return null;

        var systemPrompt = AppAISkillBL.GetComposedSkillPrompt(dsId.Value, skill.SkillId);
        if (string.IsNullOrWhiteSpace(systemPrompt)) return null;

        var provider = LLMProviderHelper.GetConfiguredProvider();
        var apiKey   = LLMProviderHelper.GetConfiguredApiKey();
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var raw = provider switch
        {
            EmLLMProvider.OpenAI    => await CallOpenAIAsync(apiKey, systemPrompt, request.Inputs, ct),
            EmLLMProvider.Anthropic => await CallAnthropicAsync(apiKey, systemPrompt, request.Inputs, ct),
            EmLLMProvider.Gemini    => await CallGeminiAsync(apiKey, systemPrompt, request.Inputs, ct),
            _                       => null
        };

        if (raw == null) return null;

        raw = StripMarkdownFences(raw);

        string? warnings = null;
        try
        {
            var parsed = JToken.Parse(raw);
            warnings = (parsed is JObject obj)
                ? obj["warnings"]?.ToString() is { Length: > 0 } w ? w : null
                : null;
        }
        catch { /* non-object JSON — leave warnings null */ }

        return new AiActionResult(raw, warnings);
    }

    private async Task<string?> CallOpenAIAsync(
        string apiKey, string systemPrompt, List<AiActionInput> inputs, CancellationToken ct)
    {
        var payload = new
        {
            model       = LLMProviderHelper.OpenAIDefaultModel,
            max_tokens  = 4096,
            messages    = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = BuildOpenAIBlocks(inputs) }
            }
        };

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var resp = await client.PostAsync(
            "https://api.openai.com/v1/chat/completions", JsonBody(payload), ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        return json["choices"]?[0]?["message"]?["content"]?.ToString();
    }

    private async Task<string?> CallAnthropicAsync(
        string apiKey, string systemPrompt, List<AiActionInput> inputs, CancellationToken ct)
    {
        var payload = new
        {
            model      = LLMProviderHelper.AnthropicDefaultModel,
            max_tokens = 4096,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = BuildAnthropicBlocks(inputs) } }
        };

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        var resp = await client.PostAsync(
            "https://api.anthropic.com/v1/messages", JsonBody(payload), ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        return json["content"]?[0]?["text"]?.ToString();
    }

    private async Task<string?> CallGeminiAsync(
        string apiKey, string systemPrompt, List<AiActionInput> inputs, CancellationToken ct)
    {
        // Gemini: system prompt goes in system_instruction, user inputs in contents
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents           = new[] { new { parts = BuildGeminiParts(inputs).ToArray() } }
        };

        using var client = _httpClientFactory.CreateClient();
        var model = LLMProviderHelper.GeminiDefaultModel;
        var url   = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var resp  = await client.PostAsync(url, JsonBody(payload), ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
    }

    private static List<object> BuildAnthropicBlocks(List<AiActionInput> inputs)
    {
        var blocks = new List<object>();
        foreach (var input in inputs)
        {
            if (input.InputType == "image" && input.ImageBytes != null)
                blocks.Add(new { type = "image", source = new { type = "base64", media_type = input.MimeType ?? "image/jpeg", data = Convert.ToBase64String(input.ImageBytes) } });
            else if (!string.IsNullOrWhiteSpace(input.TextValue))
                blocks.Add(new { type = "text", text = input.TextValue });
        }
        if (blocks.Count == 0) blocks.Add(new { type = "text", text = "Analyze the provided content." });
        return blocks;
    }

    private static List<object> BuildOpenAIBlocks(List<AiActionInput> inputs)
    {
        var blocks = new List<object>();
        foreach (var input in inputs)
        {
            if (input.InputType == "image" && input.ImageBytes != null)
                blocks.Add(new { type = "image_url", image_url = new { url = $"data:{input.MimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(input.ImageBytes)}" } });
            else if (!string.IsNullOrWhiteSpace(input.TextValue))
                blocks.Add(new { type = "text", text = input.TextValue });
        }
        if (blocks.Count == 0) blocks.Add(new { type = "text", text = "Analyze the provided content." });
        return blocks;
    }

    private static List<object> BuildGeminiParts(List<AiActionInput> inputs)
    {
        var parts = new List<object>();
        foreach (var input in inputs)
        {
            if (input.InputType == "image" && input.ImageBytes != null)
                parts.Add(new { inline_data = new { mime_type = input.MimeType ?? "image/jpeg", data = Convert.ToBase64String(input.ImageBytes) } });
            else if (!string.IsNullOrWhiteSpace(input.TextValue))
                parts.Add(new { text = input.TextValue });
        }
        if (parts.Count == 0) parts.Add(new { text = "Analyze the provided content." });
        return parts;
    }

    private static string StripMarkdownFences(string text)
    {
        var s = text.Trim();
        if (!s.StartsWith("```")) return s;
        var nl = s.IndexOf('\n');
        if (nl >= 0) s = s[(nl + 1)..];
        if (s.EndsWith("```")) s = s[..^3].TrimEnd();
        return s.Trim();
    }

    private static StringContent JsonBody(object payload)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
