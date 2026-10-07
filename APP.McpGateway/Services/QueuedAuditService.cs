using System.Threading.Channels;
using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Audit service for the multi-tenant host. Events are stamped with the authenticated caller (company + user),
/// queued in a bounded channel and written in batches by a background task, so a request never waits on the
/// database. Overflow and write failures are counted and logged, never silent. The queue is drained on shutdown.
/// </summary>
public sealed class QueuedAuditService : IAuditService, IHostedService
{
    private readonly Channel<(object Target, AuditEntry Entry)> _channel;
    private readonly IAuditSink _sink;
    private readonly IMcpCallerContext _callerContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<QueuedAuditService> _logger;
    private readonly int _batchSize;
    private readonly TimeSpan _writeTimeout;

    private volatile bool _enabled;
    private long _dropped;
    private Task _writerLoop = Task.CompletedTask;

    public QueuedAuditService(
        IAuditSink sink,
        IMcpCallerContext callerContext,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<QueuedAuditService> logger)
    {
        _sink = sink;
        _callerContext = callerContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        _enabled = configuration.GetValue("Audit:Enabled", true);
        _batchSize = Math.Max(1, configuration.GetValue("Audit:BatchSize", 100));
        _writeTimeout = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Audit:WriteTimeoutSeconds", 30)));

        // Wait mode makes TryWrite return false when full, so overflow is detectable and counted.
        _channel = Channel.CreateBounded<(object, AuditEntry)>(new BoundedChannelOptions(
            Math.Max(1, configuration.GetValue("Audit:QueueCapacity", 10_000)))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public bool IsEnabled => _enabled;

    /// <summary>Events discarded because the queue was full.</summary>
    public long DroppedCount => Interlocked.Read(ref _dropped);

    /// <summary>Runtime switch only: it is never written back to appsettings.json.</summary>
    public Task SetEnabledAsync(bool enabled, bool persist = true)
    {
        _enabled = enabled;
        _logger.LogInformation("Audit runtime override set to {Enabled} (not persisted)", enabled);
        return Task.CompletedTask;
    }

    public void Log(AuditEntry entry)
    {
        if (!_enabled) return;

        var target = _sink.CaptureTarget();
        if (target == null)
        {
            _logger.LogWarning("Audit event {Code} not persisted: no tenant context on this thread", entry.Code);
            return;
        }

        if (_callerContext.TryGetIdentity(out var companyId, out var userId))
            entry = entry with { CompanyId = entry.CompanyId ?? companyId, UserId = entry.UserId ?? userId };

        if (!_channel.Writer.TryWrite((target, entry)))
        {
            var dropped = Interlocked.Increment(ref _dropped);
            if (dropped == 1 || dropped % 100 == 0)
                _logger.LogWarning("Audit queue is full or closed; {Dropped} event(s) discarded so far (latest {Code})", dropped, entry.Code);
        }
    }

    public void Log(AuditCode code, string action, bool success = true,
                    string? appSource = null,
                    string? httpMethod = null,
                    string? resourcePath = null,
                    int? httpStatus = null,
                    string? errorMessage = null,
                    Dictionary<string, object?>? additionalContext = null)
    {
        if (!_enabled) return;

        var ctx = _httpContextAccessor.HttpContext;

        Log(new AuditEntry
        {
            Code              = code,
            Action            = action,
            Success           = success,
            // Informational only: the client chooses this value; the trusted identity is CompanyId/UserId.
            SessionId         = ctx?.Request.Headers["Mcp-Session-Id"].FirstOrDefault(),
            IpAddress         = ctx?.Connection.RemoteIpAddress?.ToString(),
            CorrelationId     = ctx?.Request.Headers["X-Correlation-Id"].FirstOrDefault(),
            AppSource         = appSource,
            HttpMethod        = httpMethod,
            ResourcePath      = resourcePath,
            HttpStatus        = httpStatus,
            ErrorMessage      = errorMessage,
            AdditionalContext = additionalContext
        });
    }

    // Implemented as a plain IHostedService instead of a BackgroundService on purpose: BackgroundService cancels
    // its execute task as soon as the host stops, which can end it before it has drained the queue and lose events.
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _writerLoop = Task.Run(WriteLoopAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    // Completes the queue, then waits for the writer to flush everything already queued.
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        await _writerLoop.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    // Ends only when the channel is completed and fully drained.
    private async Task WriteLoopAsync()
    {
        var reader = _channel.Reader;

        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            var batch = new List<(object Target, AuditEntry Entry)>(_batchSize);
            while (batch.Count < _batchSize && reader.TryRead(out var item))
                batch.Add(item);

            foreach (var group in batch.GroupBy(b => b.Target))
                await WriteGroupAsync(group.Key, group.Select(g => g.Entry).ToList()).ConfigureAwait(false);
        }
    }

    private async Task WriteGroupAsync(object target, List<AuditEntry> entries)
    {
        using var cts = new CancellationTokenSource(_writeTimeout);
        try
        {
            await _sink.WriteAsync(target, entries, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist {Count} audit event(s); they are lost", entries.Count);
        }
    }
}
