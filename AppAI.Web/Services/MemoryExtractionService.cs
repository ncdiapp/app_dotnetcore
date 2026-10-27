using App.BL.AIAgent.GenericAgent.Memory;

namespace AppAI.Web.Services;

/// <summary>
/// Periodic extraction of agent memory (L2 facts, L3 episodes) from idle chats. Configuration section AgentMemory:
///   Mode            Off (default) | DryRun | Preview | On
///   IntervalMinutes how often a round runs (default 10)
///   IdleMinutes     a chat must be quiet this long before it is extracted (default 30)
///   BatchPerTenant  chats per tenant per round (default 20)
///   MinNewTurns     skip chats with fewer new user/assistant text turns (default 4)
/// Mode is applied in the constructor, so the on-"Clear conversation" extraction follows it even when Off here.
/// Same shape as OAuthMaintenanceService: delayed start, PeriodicTimer, failures logged and retried next round.
/// </summary>
public sealed class MemoryExtractionService : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

    public MemoryExtractionService(IConfiguration configuration)
    {
        var section = configuration.GetSection("AgentMemory");
        var mode = Enum.TryParse<AgentMemoryMode>(section["Mode"], true, out var parsed) ? parsed : AgentMemoryMode.Off;
        AgentMemoryJob.Configure(new AgentMemoryJobOptions
        {
            Mode           = mode,
            IdleMinutes    = Positive(section["IdleMinutes"], 30),
            BatchPerTenant = Positive(section["BatchPerTenant"], 20),
            MinNewTurns    = Positive(section["MinNewTurns"], 4)
        });
        _interval = TimeSpan.FromMinutes(Positive(section["IntervalMinutes"], 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (AgentMemoryJob.Current.Mode == AgentMemoryMode.Off)
        {
            _log.Info("[MemoryJob] AgentMemory:Mode is Off; periodic extraction disabled");
            return;
        }
        _log.Info("[MemoryJob] mode={0} every {1} min", AgentMemoryJob.Current.Mode, _interval.TotalMinutes);

        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(_interval);
        do
        {
            try { await AgentMemoryJob.RunOnceAsync(AgentMemoryJob.Current, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _log.Error(ex, "[MemoryJob] round failed; it will be retried on the next round"); }
        }
        while (await WaitAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { return false; }
    }

    private static int Positive(string? raw, int fallback) => int.TryParse(raw, out var n) && n > 0 ? n : fallback;
}
