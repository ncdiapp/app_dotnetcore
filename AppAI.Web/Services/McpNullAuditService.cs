using McpGateway.Models;
using McpGateway.Services;

namespace AppAI.Web.Services;

/// <summary>
/// No-op IAuditService — registered when ConnectionStrings:AuditDb is empty.
/// </summary>
public class McpNullAuditService : IAuditService
{
    public bool IsEnabled => false;

    public Task SetEnabledAsync(bool enabled, bool persist = true)
        => Task.CompletedTask;

    public void Log(AuditEntry entry) { }

    public void Log(AuditCode code, string action, bool success = true,
        string? appSource = null,
        string? httpMethod = null,
        string? resourcePath = null,
        int? httpStatus = null,
        string? errorMessage = null,
        Dictionary<string, object?>? additionalContext = null) { }
}
