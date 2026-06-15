namespace AppAI.Web.Services;

/// <summary>
/// Extracts text from image bytes using a configured LLM vision provider.
/// Replaces the Tesseract dependency that is unavailable on .NET 10 without native binaries.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Returns the text content extracted from the image, or null if extraction fails.
    /// </summary>
    Task<string?> ExtractTextAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default);
}
