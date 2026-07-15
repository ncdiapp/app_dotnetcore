using AppAI.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Endpoints;

/// <summary>
/// Generic AI Action Engine endpoint.
/// Any AI capability is delivered by pairing this endpoint with an AppAISkill prompt
/// and a CommandActionButton configured in Form Builder — no additional code needed
/// per feature.
/// </summary>
public static class AiActionEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/ai/action", async (
            [FromBody] AiActionHttpRequest req,
            IAiActionService svc,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.SkillName))
                return Results.BadRequest("SkillName is required.");

            var inputs = req.Inputs.Select(i => new AiActionInput(
                i.InputType,
                i.TextValue,
                i.ImageBase64 is { Length: > 0 } b64 ? Convert.FromBase64String(b64) : null,
                i.MimeType)).ToList();

            var result = await svc.ExecuteAsync(new AiActionRequest(inputs, req.SkillName), ct);

            return result is null
                ? Results.Problem("AI action failed — check skill name and provider config.")
                : Results.Ok(result);
        });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record AiActionHttpInputDto(
    string InputType,       // "text" | "image"
    string? TextValue,
    string? ImageBase64,
    string? MimeType);

public record AiActionHttpRequest(
    List<AiActionHttpInputDto> Inputs,
    string SkillName);
