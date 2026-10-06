using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Where audit events are finally stored. The gateway cannot reference the host's business layer, so the host
/// (AppAI.Web) supplies the implementation, which writes into the caller's tenant database.
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Called on the request thread while the caller's identity is registered. Returns an opaque destination
    /// (e.g. the tenant connection) the background writer will use later, or null when the event cannot be stored.
    /// Must support value equality so events for the same destination are written together.
    /// </summary>
    object? CaptureTarget();

    /// <summary>Stores a batch for one destination. Throws on failure; the writer logs and moves on.</summary>
    Task WriteAsync(object target, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken);
}
