using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>
/// Hourly housekeeping: removes expired and unusable tokens and authorizations (OpenIddict only provides the call),
/// and dynamically registered clients that nobody used. Failures are logged and retried on the next round.
/// </summary>
public sealed class OAuthMaintenanceService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly OAuthOptions _options;
    private readonly ILogger<OAuthMaintenanceService> _log;

    public OAuthMaintenanceService(IServiceScopeFactory scopes, IOptions<OAuthOptions> options, ILogger<OAuthMaintenanceService> log)
    {
        _scopes = scopes;
        _options = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the application finish starting before the first round.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await WaitAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { return false; }
    }

    /// <summary>One housekeeping round. Public so it can be run on demand and tested.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var threshold = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, _options.RefreshTokenDays));
            var tokens = await scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>().PruneAsync(threshold, ct).ConfigureAwait(false);
            var authorizations = await scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>().PruneAsync(threshold, ct).ConfigureAwait(false);
            var clients = await scope.ServiceProvider.GetRequiredService<OAuthClientAdmin>()
                .DeleteUnusedDynamicAsync(DateTimeOffset.UtcNow.AddDays(-Math.Max(1, _options.DynamicClientRetentionDays)), ct).ConfigureAwait(false);
            _log.LogInformation("OAuth maintenance: pruned {Tokens} tokens, {Authorizations} authorizations, {Clients} unused dynamic clients",
                tokens, authorizations, clients);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "OAuth maintenance round failed; it will be retried on the next round");
        }
    }
}
