using System.Net.Http.Headers;
using System.Text;
using App.BL.GenericAgent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppAI.Web.Services;

/// <summary>
/// Speech-to-text: prefer OpenAI Whisper; fall back to Gemini multimodal audio.
/// Omits Whisper <c>language</c> so the model auto-detects (multi-language).
/// </summary>
public sealed class WhisperSpeechToTextService : ISpeechToTextService
{
    public const int MaxAudioBytes = 25 * 1024 * 1024;
    private const string WhisperModel = "whisper-1";
    private const string TranscriptionsUrl = "https://api.openai.com/v1/audio/transcriptions";
    private const string TranscribePrompt =
        "Transcribe this audio to text. Return only the spoken words with no preamble or commentary.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhisperSpeechToTextService> _logger;

    public WhisperSpeechToTextService(
        IHttpClientFactory httpClientFactory,
        ILogger<WhisperSpeechToTextService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SpeechToTextResult> TranscribeAsync(
        byte[] audioBytes,
        string mimeType,
        string? language = null,
        CancellationToken ct = default)
    {
        if (audioBytes == null || audioBytes.Length == 0)
            return SpeechToTextResult.Fail("Audio is empty.");

        if (audioBytes.Length > MaxAudioBytes)
            return SpeechToTextResult.Fail($"Audio exceeds the {MaxAudioBytes / (1024 * 1024)} MB limit.");

        if (!AIConfigSettingBL.IsAiVoiceInputAvailable())
            return SpeechToTextResult.Fail("Voice input is not enabled for this tenant, or no STT API key is configured.");

        var contentType = NormalizeMime(mimeType);
        var openAiKey = AIConfigSettingBL.GetOpenAIApiKey();
        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            var whisper = await TranscribeWithWhisperAsync(openAiKey, audioBytes, contentType, language, ct)
                .ConfigureAwait(false);
            if (whisper.IsSuccess) return whisper;
            _logger.LogWarning("Whisper failed ({Error}); trying Gemini if available.", whisper.Error);
        }

        var geminiKey = AIConfigSettingBL.GetGeminiApiKey();
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            return await TranscribeWithGeminiAsync(geminiKey, audioBytes, contentType, ct).ConfigureAwait(false);
        }

        return SpeechToTextResult.Fail(
            string.IsNullOrWhiteSpace(openAiKey)
                ? "Configure an OpenAI or Gemini API key for voice transcription."
                : "Voice transcription failed and no Gemini fallback key is configured.");
    }

    private async Task<SpeechToTextResult> TranscribeWithWhisperAsync(
        string apiKey,
        byte[] audioBytes,
        string contentType,
        string? language,
        CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.Timeout = TimeSpan.FromMinutes(2);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(WhisperModel), "model");
            form.Add(new StringContent("json"), "response_format");
            if (!string.IsNullOrWhiteSpace(language))
                form.Add(new StringContent(language.Trim()), "language");

            var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(audioContent, "file", FileNameForMime(contentType));

            using var response = await client.PostAsync(TranscriptionsUrl, form, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Whisper transcription failed ({Status}): {Body}", (int)response.StatusCode, Truncate(body, 500));
                return SpeechToTextResult.Fail(ParseOpenAiError(body) ?? $"Whisper failed ({(int)response.StatusCode}).");
            }

            var text = JObject.Parse(body)["text"]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return SpeechToTextResult.Fail("No speech was recognized.");
            return SpeechToTextResult.Ok(text);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper transcription error");
            return SpeechToTextResult.Fail($"Whisper error: {ex.Message}");
        }
    }

    private async Task<SpeechToTextResult> TranscribeWithGeminiAsync(
        string apiKey,
        byte[] audioBytes,
        string contentType,
        CancellationToken ct)
    {
        try
        {
            var model = AIConfigSettingBL.GetGeminiModel();
            var base64 = Convert.ToBase64String(audioBytes);
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inline_data = new { mime_type = contentType, data = base64 } },
                            new { text = TranscribePrompt }
                        }
                    }
                }
            };

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            using var response = await client.PostAsync(
                url,
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
                ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini transcription failed ({Status}): {Body}", (int)response.StatusCode, Truncate(body, 500));
                return SpeechToTextResult.Fail($"Gemini transcription failed ({(int)response.StatusCode}).");
            }

            var text = JObject.Parse(body)["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim()
                       ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return SpeechToTextResult.Fail("No speech was recognized.");
            return SpeechToTextResult.Ok(text);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini transcription error");
            return SpeechToTextResult.Fail($"Gemini transcription error: {ex.Message}");
        }
    }

    private static string NormalizeMime(string? mimeType)
    {
        var m = (mimeType ?? string.Empty).Trim().ToLowerInvariant().Split(';')[0].Trim();
        if (string.IsNullOrEmpty(m)) return "audio/webm";
        if (m is "audio/wav" or "audio/wave" or "audio/x-wav") return "audio/wav";
        if (m.StartsWith("audio/", StringComparison.Ordinal)) return m;
        return "audio/webm";
    }

    private static string FileNameForMime(string mime) => mime switch
    {
        "audio/wav" or "audio/wave" or "audio/x-wav" => "recording.wav",
        "audio/mpeg" or "audio/mp3" => "recording.mp3",
        "audio/mp4" or "audio/m4a" => "recording.m4a",
        "audio/ogg" => "recording.ogg",
        "audio/flac" => "recording.flac",
        _ => "recording.webm",
    };

    private static string? ParseOpenAiError(string body)
    {
        try
        {
            var msg = JObject.Parse(body)["error"]?["message"]?.ToString();
            return string.IsNullOrWhiteSpace(msg) ? null : msg;
        }
        catch
        {
            return null;
        }
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
}
