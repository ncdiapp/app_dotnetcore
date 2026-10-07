using System.Collections.Concurrent;
using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>Loads the (AppSource, OperationId) pairs one user may call. Implemented by the host (tenant DB).</summary>
public interface IApiAccessProvider
{
    Task<IReadOnlyCollection<(string AppSource, string OperationId)>> LoadAllowedAsync(
        int companyId, int userId, CancellationToken cancellationToken);
}

/// <summary>
/// Decides which endpoints the authenticated external user may see and call. Deny by default: no authenticated
/// caller, an endpoint nobody granted, or a failure to load the grants all mean "not allowed".
/// </summary>
public interface IApiAccessPolicy
{
    Task<bool> IsAllowedAsync(string appSource, string operationId, CancellationToken cancellationToken = default);

    /// <summary>Keeps only the endpoints the caller may use, preserving order.</summary>
    Task<IReadOnlyList<T>> FilterAsync<T>(IEnumerable<T> items, Func<T, (string AppSource, string OperationId)> key,
        CancellationToken cancellationToken = default);

    /// <summary>Drops cached grants so admin changes apply immediately instead of after the cache TTL.</summary>
    void InvalidateAll();
}

public sealed class ApiAccessPolicy : IApiAccessPolicy
{
    private sealed record Entry(DateTime ExpiresUtc, HashSet<string> Allowed);

    private readonly IApiAccessProvider _provider;
    private readonly IMcpCallerContext _callerContext;
    private readonly ILogger<ApiAccessPolicy> _logger;
    private readonly TimeSpan _ttl;
    private readonly ConcurrentDictionary<(int CompanyId, int UserId), Entry> _cache = new();

    public ApiAccessPolicy(
        IApiAccessProvider provider,
        IMcpCallerContext callerContext,
        IConfiguration configuration,
        ILogger<ApiAccessPolicy> logger)
    {
        _provider = provider;
        _callerContext = callerContext;
        _logger = logger;
        _ttl = TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("Mcp:AccessCacheSeconds", 60)));
    }

    public async Task<bool> IsAllowedAsync(string appSource, string operationId, CancellationToken cancellationToken = default)
    {
        var allowed = await GetAllowedAsync(cancellationToken).ConfigureAwait(false);
        return allowed != null && allowed.Contains(Key(appSource, operationId));
    }

    public async Task<IReadOnlyList<T>> FilterAsync<T>(
        IEnumerable<T> items, Func<T, (string AppSource, string OperationId)> key, CancellationToken cancellationToken = default)
    {
        var allowed = await GetAllowedAsync(cancellationToken).ConfigureAwait(false);
        if (allowed == null) return [];

        return items.Where(i =>
        {
            var (source, operationId) = key(i);
            return allowed.Contains(Key(source, operationId));
        }).ToList();
    }

    public void InvalidateAll() => _cache.Clear();

    // null = nothing is allowed (no caller, or the grants could not be loaded).
    private async Task<HashSet<string>?> GetAllowedAsync(CancellationToken ct)
    {
        if (!_callerContext.TryGetIdentity(out var companyId, out var userId))
            return null;

        var cacheKey = (companyId, userId);
        if (_cache.TryGetValue(cacheKey, out var hit) && hit.ExpiresUtc > DateTime.UtcNow)
            return hit.Allowed;

        try
        {
            var loaded = await _provider.LoadAllowedAsync(companyId, userId, ct).ConfigureAwait(false);
            var set = new HashSet<string>(loaded.Select(l => Key(l.AppSource, l.OperationId)), StringComparer.OrdinalIgnoreCase);
            if (_ttl > TimeSpan.Zero)
                _cache[cacheKey] = new Entry(DateTime.UtcNow + _ttl, set);
            return set;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load API grants for company {Company} user {User}; denying access", companyId, userId);
            return null;
        }
    }

    private static string Key(string appSource, string operationId) => appSource + "\n" + operationId;
}
