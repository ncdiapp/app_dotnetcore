using APP.Framework.Communication;
using APP.Framework.Validation;
using App.BL.GenericAgent;
using AppAI.Web.Controllers.Base;
using AppAI.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

/// <summary>
/// Agent microphone voice input: browser MediaRecorder → Whisper / Gemini transcription.
/// Must inherit SecureBaseController so session registers tenant identity for AIConfig keys.
/// </summary>
[Route("webapi/[controller]/[action]")]
public sealed class VoiceInputController : SecureBaseController
{
    private readonly ISpeechToTextService _speechToText;

    public VoiceInputController(ISpeechToTextService speechToText)
    {
        _speechToText = speechToText;
    }

    /// <summary>
    /// Transcribe base64 audio to text. Language auto-detected.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(WhisperSpeechToTextService.MaxAudioBytes * 2)] // base64 overhead
    public async Task<OperationCallResult<VoiceTranscriptDto>> GetTextFromVoiceInput(
        [FromBody] VoiceInputRequest? req,
        CancellationToken ct)
    {
        var result = new OperationCallResult<VoiceTranscriptDto>();

        if (!AIConfigSettingBL.IsAiVoiceInputAvailable())
        {
            var detail = DiagnoseVoiceGate();
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(VoiceInputController), "VoiceInput_Disabled", ValidationItemType.Error,
                detail));
            return result;
        }

        if (req == null || string.IsNullOrWhiteSpace(req.AudioBase64))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(VoiceInputController), "VoiceInput_Required", ValidationItemType.Error,
                "AudioBase64 is required."));
            return result;
        }

        byte[] audioBytes;
        try
        {
            var b64 = StripDataUrl(req.AudioBase64);
            audioBytes = Convert.FromBase64String(b64);
        }
        catch (FormatException)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(VoiceInputController), "VoiceInput_BadBase64", ValidationItemType.Error,
                "AudioBase64 is not valid base64."));
            return result;
        }

        if (audioBytes.Length > WhisperSpeechToTextService.MaxAudioBytes)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(VoiceInputController), "VoiceInput_TooLarge", ValidationItemType.Error,
                "Audio exceeds the maximum allowed size."));
            return result;
        }

        var stt = await _speechToText.TranscribeAsync(
            audioBytes,
            req.MimeType ?? "audio/webm",
            language: null,
            ct).ConfigureAwait(false);

        if (!stt.IsSuccess)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(VoiceInputController), "VoiceInput_SttFailed", ValidationItemType.Error,
                stt.Error ?? "Transcription failed."));
            return result;
        }

        result.Object = new VoiceTranscriptDto { Sentence = stt.Sentence };
        return result;
    }

    private static string DiagnoseVoiceGate()
    {
        if (!AIConfigSettingBL.IsConfigured())
            return "AI is not configured for this tenant (default LLM API key missing).";
        if (!AIConfigSettingBL.IsVoiceInputEnabled())
            return "Voice input is disabled (AIConfigEnableVoiceInput).";
        if (!AIConfigSettingBL.IsVoiceTranscriptionConfigured())
            return "Configure an OpenAI API key (Whisper) or Gemini API key for voice transcription.";
        return "Voice input is not available for this tenant.";
    }

    private static string StripDataUrl(string audioBase64)
    {
        var s = audioBase64.Trim();
        var comma = s.IndexOf(',');
        if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            return s[(comma + 1)..];
        return s;
    }
}

public sealed class VoiceInputRequest
{
    public string? AudioBase64 { get; set; }
    public string? MimeType { get; set; }
}

public sealed class VoiceTranscriptDto
{
    public string Sentence { get; set; } = string.Empty;
}
