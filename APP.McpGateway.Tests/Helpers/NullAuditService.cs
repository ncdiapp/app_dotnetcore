using McpGateway.Models;
using McpGateway.Services;

namespace McpGateway.Tests.Helpers;

internal sealed class NullAuditService : IAuditService
{
    public static readonly NullAuditService Instance = new();
    public bool IsEnabled => true;
    public Task SetEnabledAsync(bool enabled, bool persist = true) => Task.CompletedTask;
    public void Log(AuditEntry entry) { }
    public void Log(AuditCode code, string action, bool success = true,
                    string? appSource = null, string? httpMethod = null,
                    string? resourcePath = null, int? httpStatus = null,
                    string? errorMessage = null,
                    Dictionary<string, object?>? additionalContext = null) { }
}
