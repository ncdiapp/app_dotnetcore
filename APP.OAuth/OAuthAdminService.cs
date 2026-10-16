using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APP.OAuth;

/// <summary>One AI application a user has connected to AppAI.</summary>
public sealed record OAuthConnection(string AuthorizationId, string ClientId, string ClientName, int UserId, int CompanyId,
    DateTimeOffset? CreatedUtc, string ScopesJson, string? GrantSession);

/// <summary>An OAuth client (an AI application that may ask users to sign in).</summary>
public sealed record OAuthClientInfo(string ClientId, string Name, IReadOnlyList<string> RedirectUris, bool IsDynamic);

/// <summary>Lists and ends the connections users made. A revoked connection stops working at once.</summary>
public sealed class OAuthConnectionAdmin
{
    private readonly IOpenIddictAuthorizationStore<OAuthAuthorization> _store;
    private readonly IOpenIddictAuthorizationManager _authorizations;
    private readonly IOpenIddictTokenManager _tokens;
    private readonly IOAuthIdentityBridge _bridge;
    private readonly ILogger<OAuthConnectionAdmin> _log;

    public OAuthConnectionAdmin(IOpenIddictAuthorizationStore<OAuthAuthorization> store, IOpenIddictAuthorizationManager authorizations,
        IOpenIddictTokenManager tokens, IOAuthIdentityBridge bridge, ILogger<OAuthConnectionAdmin> log)
    {
        _store = store;
        _authorizations = authorizations;
        _tokens = tokens;
        _bridge = bridge;
        _log = log;
    }

    /// <param name="companyId">Only this company's connections (company admin), or null for all (not exposed to company admins).</param>
    /// <param name="userId">Only this user's connections (a user managing their own), or null.</param>
    public Task<List<OAuthConnection>> ListAsync(int? companyId, int? userId, CancellationToken ct) =>
        ((AuthorizationStore)_store).ListConnectionsAsync(companyId, userId, ct);

    /// <summary>Ends one connection if it belongs to the given company/user scope. Returns false when it is not in scope.</summary>
    public async Task<bool> RevokeAsync(string authorizationId, int? companyId, int? userId, CancellationToken ct)
    {
        var connection = (await ListAsync(companyId, userId, ct).ConfigureAwait(false)).FirstOrDefault(c => c.AuthorizationId == authorizationId);
        if (connection is null) return false;

        await _tokens.RevokeByAuthorizationIdAsync(authorizationId, ct).ConfigureAwait(false);
        var authorization = await _authorizations.FindByIdAsync(authorizationId, ct).ConfigureAwait(false);
        if (authorization is not null) await _authorizations.TryRevokeAsync(authorization, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(connection.GrantSession)) _bridge.DeleteGrantSession(connection.GrantSession);

        _log.LogInformation("OAuth connection revoked: authorization {AuthorizationId} user {UserId} company {CompanyId} client {ClientId}",
            authorizationId, connection.UserId, connection.CompanyId, connection.ClientId);
        return true;
    }
}

/// <summary>Registers, lists and removes OAuth clients, and the housekeeping of dynamically registered ones.</summary>
public sealed class OAuthClientAdmin
{
    private readonly IOpenIddictApplicationStore<OAuthApplication> _store;
    private readonly IOpenIddictApplicationManager _apps;
    private readonly OAuthUrls _urls;
    private readonly string _scope;
    private readonly ILogger<OAuthClientAdmin> _log;

    public OAuthClientAdmin(IOpenIddictApplicationStore<OAuthApplication> store, IOpenIddictApplicationManager apps,
        OAuthUrls urls, Microsoft.Extensions.Options.IOptions<OAuthOptions> options, ILogger<OAuthClientAdmin> log)
    {
        _store = store;
        _apps = apps;
        _urls = urls;
        _scope = options.Value.Scope;
        _log = log;
    }

    public Task<long> CountDynamicAsync(CancellationToken ct) => ((ApplicationStore)_store).CountDynamicAsync(ct);

    public Task<long> DeleteUnusedDynamicAsync(DateTimeOffset olderThan, CancellationToken ct) =>
        ((ApplicationStore)_store).DeleteUnusedDynamicAsync(olderThan, ct);

    public async Task<List<OAuthClientInfo>> ListAsync(CancellationToken ct)
    {
        var result = new List<OAuthClientInfo>();
        await foreach (var app in _store.ListAsync(null, null, ct).ConfigureAwait(false))
        {
            var props = OAuthJson.Properties(app.Properties);
            var dynamic = props.TryGetValue("dynamic", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.True;
            result.Add(new OAuthClientInfo(app.ClientId ?? "", app.DisplayName ?? app.ClientId ?? "", OAuthJson.Array(app.RedirectUris), dynamic));
        }
        return result.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Pre-registers a public PKCE client (the "use your own OAuth client" setting in Claude). Returns its client id.</summary>
    public async Task<string> CreateAsync(string name, IReadOnlyList<string> redirectUris, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new OAuthConsentException("The name is required (100 characters at most).");
        if (redirectUris.Count is 0 or > 10) throw new OAuthConsentException("Give between 1 and 10 redirect URIs.");
        foreach (var uri in redirectUris)
        {
            // An administrator may register any https address (their own application) or a loopback address.
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Fragment.Length > 0 || parsed.UserInfo.Length > 0
                || !(parsed.Scheme == Uri.UriSchemeHttps || (parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback)))
                throw new OAuthConsentException("Redirect URI not allowed: " + uri + " (https, or http on localhost).");
        }

        var clientId = "app-" + Guid.NewGuid().ToString("N");
        var descriptor = DynamicClientRegistration.Describe(new ClientRegistration(name.Trim(), redirectUris, Array.Empty<string>()),
            clientId, _urls.Resource, _scope);
        descriptor.Properties.Remove("dynamic");   // pre-registered clients are never cleaned up automatically
        await _apps.CreateAsync(descriptor, ct).ConfigureAwait(false);
        _log.LogInformation("OAuth client pre-registered: {ClientId} '{Name}'", clientId, name);
        return clientId;
    }

    public async Task<bool> DeleteAsync(string clientId, CancellationToken ct)
    {
        var app = await _apps.FindByClientIdAsync(clientId, ct).ConfigureAwait(false);
        if (app is null) return false;
        await _apps.DeleteAsync(app, ct).ConfigureAwait(false);   // cascades to its authorizations and tokens
        _log.LogInformation("OAuth client deleted: {ClientId}", clientId);
        return true;
    }
}
