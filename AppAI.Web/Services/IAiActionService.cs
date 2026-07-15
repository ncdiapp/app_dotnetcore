namespace AppAI.Web.Services;

public record AiActionInput(string InputType, string? TextValue, byte[]? ImageBytes, string? MimeType);
public record AiActionRequest(List<AiActionInput> Inputs, string SkillName);
public record AiActionResult(string RawJson, string? Warnings);

public interface IAiActionService
{
    Task<AiActionResult?> ExecuteAsync(AiActionRequest request, CancellationToken ct = default);
}
