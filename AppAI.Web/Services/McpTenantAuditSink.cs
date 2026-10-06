using System.Text.Json;
using App.BL.TenantBusiness;
using McpGateway.Models;
using McpGateway.Services;

namespace AppAI.Web.Services;

/// <summary>
/// Stores MCP gateway audit events in the caller's tenant database (dbo.AppMcpAuditLog, V040) through
/// McpAuditBL. Plugged into the gateway's QueuedAuditService, which does the queueing and batching.
/// </summary>
public sealed class McpTenantAuditSink : IAuditSink
{
    public object? CaptureTarget() => McpAuditBL.CaptureTarget();

    public Task WriteAsync(object target, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken)
    {
        var rows = entries.Select(e => new McpAuditRow(
            CreatedUtc:        e.Timestamp.UtcDateTime,
            EventCode:         e.Code.ToString(),
            Action:            e.Action,
            Success:           e.Success,
            CompanyId:         e.CompanyId,
            UserId:            e.UserId,
            McpSessionId:      e.SessionId,
            IpAddress:         e.IpAddress,
            CorrelationId:     e.CorrelationId,
            AppSource:         e.AppSource,
            HttpMethod:        e.HttpMethod,
            ResourcePath:      e.ResourcePath,
            HttpStatus:        e.HttpStatus,
            ErrorMessage:      e.ErrorMessage,
            AdditionalContext: e.AdditionalContext is { Count: > 0 } ? JsonSerializer.Serialize(e.AdditionalContext) : null))
            .ToList();

        return McpAuditBL.WriteAsync((McpAuditTarget)target, rows, cancellationToken);
    }
}
