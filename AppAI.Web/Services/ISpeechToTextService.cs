namespace AppAI.Web.Services;

/// <summary>Transcribes recorded audio to text (Whisper).</summary>
public interface ISpeechToTextService
{
    /// <summary>
    /// Transcribe audio bytes. Language is auto-detected when <paramref name="language"/> is null/empty.
    /// </summary>
    Task<SpeechToTextResult> TranscribeAsync(
        byte[] audioBytes,
        string mimeType,
        string? language = null,
        CancellationToken ct = default);
}

public sealed class SpeechToTextResult
{
    public bool IsSuccess { get; init; }
    public string Sentence { get; init; } = string.Empty;
    public string? Error { get; init; }

    public static SpeechToTextResult Ok(string sentence) => new()
    {
        IsSuccess = true,
        Sentence = sentence ?? string.Empty,
    };

    public static SpeechToTextResult Fail(string error) => new()
    {
        IsSuccess = false,
        Error = error,
    };
}
