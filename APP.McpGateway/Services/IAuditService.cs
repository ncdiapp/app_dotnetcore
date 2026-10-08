using McpGateway.Models;

namespace McpGateway.Services;

public interface IAuditService
{
    /// <summary>Whether audit logging is currently active.</summary>
    bool IsEnabled { get; }

    /// <summary>Enable or disable audit logging at runtime. Pass persist=true to write to appsettings.json.</summary>
    Task SetEnabledAsync(bool enabled, bool persist = true);

    /// <summary>
    /// Emits a single audit event. Use from background tasks where no HttpContext is available.
    /// </summary>
    void Log(AuditEntry entry);

    /// <summary>
    /// Convenience overload — resolves SessionId, IpAddress, and CorrelationId from IHttpContextAccessor.
    /// Use from controllers and services running inside the HTTP request pipeline.
    /// </summary>
    void Log(AuditCode code, string action, bool success = true,
             string? appSource = null,
             string? httpMethod = null,
             string? resourcePath = null,
             int? httpStatus = null,
             string? errorMessage = null,
             Dictionary<string, object?>? additionalContext = null);
}
