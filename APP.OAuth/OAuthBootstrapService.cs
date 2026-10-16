using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>
/// At startup makes sure the one scope clients request exists and allows the MCP URL as its token audience
/// (RFC 8707 resource). Without that row every authorize request is refused. Idempotent, and it also repairs the row
/// when OAuth:PublicBaseUrl or OAuth:McpPath changes. A failure (typically: the OAuth tables were not created yet)
/// is logged loudly but never stops the application from starting.
/// </summary>
public sealed class OAuthBootstrapService : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly OAuthUrls _urls;
    private readonly OAuthOptions _options;
    private readonly ILogger<OAuthBootstrapService> _log;

    public OAuthBootstrapService(IServiceScopeFactory scopes, OAuthUrls urls, IOptions<OAuthOptions> options, ILogger<OAuthBootstrapService> log)
    {
        _scopes = scopes;
        _urls = urls;
        _options = options.Value;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EnsureScopeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "OAuth could not prepare its '{Scope}' scope. Were the tables from Document/Design/MasterDB_OAuthTables.sql created in AppMasterDB? " +
                              "Sign-in from AI applications will not work until this succeeds (restart after fixing).", _options.Scope);
        }
    }

    /// <summary>Creates or repairs the scope. Public so it can be tested.</summary>
    public async Task EnsureScopeAsync(CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        var descriptor = new OpenIddictScopeDescriptor { Name = _options.Scope, DisplayName = "AppAI MCP gateway" };
        descriptor.Resources.Add(_urls.Resource);

        var existing = await manager.FindByNameAsync(_options.Scope, ct).ConfigureAwait(false);
        if (existing is null)
        {
            await manager.CreateAsync(descriptor, ct).ConfigureAwait(false);
            _log.LogInformation("OAuth scope '{Scope}' created for resource {Resource}", _options.Scope, _urls.Resource);
            return;
        }

        var current = await manager.GetResourcesAsync(existing, ct).ConfigureAwait(false);
        if (current.Length == 1 && string.Equals(current[0], _urls.Resource, StringComparison.Ordinal)) return;

        await manager.UpdateAsync(existing, descriptor, ct).ConfigureAwait(false);
        _log.LogWarning("OAuth scope '{Scope}' resource updated to {Resource}", _options.Scope, _urls.Resource);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
