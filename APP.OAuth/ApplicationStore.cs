using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>OpenIddict application (client) store on dbo.AppOAuthApplication.</summary>
internal sealed class ApplicationStore : SqlStoreBase<OAuthApplication>, IOpenIddictApplicationStore<OAuthApplication>
{
    // Exact (binary) match: redirect URIs must never match case-insensitively.
    private const string UriMatch = "EXISTS (SELECT 1 FROM OPENJSON({0}) WHERE [value] = @uri COLLATE Latin1_General_100_BIN2)";

    public ApplicationStore(IOptions<OAuthDbOptions> options) : base(options) { }

    protected override string Table => "AppOAuthApplication";
    protected override string IdColumn => "ApplicationId";
    protected override string[] DataColumns { get; } =
    {
        "ClientId", "ClientSecret", "ApplicationType", "ClientType", "ConsentType", "DisplayName", "DisplayNames",
        "JsonWebKeySet", "Permissions", "PostLogoutRedirectUris", "Properties", "RedirectUris", "Requirements", "Settings"
    };

    protected override object?[] Values(OAuthApplication a) => new object?[]
    {
        a.ClientId, a.ClientSecret, a.ApplicationType, a.ClientType, a.ConsentType, a.DisplayName, a.DisplayNames,
        a.JsonWebKeySet, a.Permissions, a.PostLogoutRedirectUris, a.Properties, a.RedirectUris, a.Requirements, a.Settings
    };

    protected override void Read(SqlDataReader r, OAuthApplication a)
    {
        a.ClientId = S(r, "ClientId"); a.ClientSecret = S(r, "ClientSecret"); a.ApplicationType = S(r, "ApplicationType");
        a.ClientType = S(r, "ClientType"); a.ConsentType = S(r, "ConsentType"); a.DisplayName = S(r, "DisplayName");
        a.DisplayNames = S(r, "DisplayNames"); a.JsonWebKeySet = S(r, "JsonWebKeySet"); a.Permissions = S(r, "Permissions");
        a.PostLogoutRedirectUris = S(r, "PostLogoutRedirectUris"); a.Properties = S(r, "Properties");
        a.RedirectUris = S(r, "RedirectUris"); a.Requirements = S(r, "Requirements"); a.Settings = S(r, "Settings");
    }

    // ── lookups used by the authorize / token flows ────────────────────────────

    public async ValueTask<OAuthApplication?> FindByClientIdAsync(string identifier, CancellationToken ct) =>
        await QueryOneAsync("ClientId = @clientId", new[] { P("@clientId", identifier) }, ct).ConfigureAwait(false);

    public IAsyncEnumerable<OAuthApplication> FindByRedirectUriAsync(string uri, CancellationToken ct) =>
        Stream(QueryAsync(string.Format(UriMatch, "RedirectUris"), new[] { P("@uri", uri) }, ct), ct);

    public IAsyncEnumerable<OAuthApplication> FindByPostLogoutRedirectUriAsync(string uri, CancellationToken ct) =>
        Stream(QueryAsync(string.Format(UriMatch, "PostLogoutRedirectUris"), new[] { P("@uri", uri) }, ct), ct);

    // ── dynamic-registration housekeeping (property "dynamic": true set by POST /oauth/register) ──

    private const string DynamicFilter = "Properties LIKE N'%\"dynamic\":true%'";

    public async Task<long> CountDynamicAsync(CancellationToken ct) =>
        await ScalarLongAsync("SELECT COUNT_BIG(*) FROM dbo.AppOAuthApplication WHERE " + DynamicFilter, null, ct).ConfigureAwait(false);

    /// <summary>Deletes dynamically registered clients older than the threshold that nobody ever authorized.</summary>
    public async Task<long> DeleteUnusedDynamicAsync(DateTimeOffset olderThan, CancellationToken ct) =>
        await ExecAsync("DELETE FROM dbo.AppOAuthApplication WHERE " + DynamicFilter + " AND CreatedUtc < @olderThan " +
                        "AND NOT EXISTS (SELECT 1 FROM dbo.AppOAuthAuthorization a WHERE a.ApplicationId = dbo.AppOAuthApplication.ApplicationId)",
            new[] { P("@olderThan", olderThan) }, ct).ConfigureAwait(false);

    // ── LINQ-query overloads: not supported (see SqlStoreBase) ─────────────────

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<OAuthApplication>, IQueryable<TResult>> query, CancellationToken ct) =>
        throw QueryNotSupported(nameof(ApplicationStore), "CountAsync");

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<OAuthApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(ApplicationStore), "GetAsync");

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<OAuthApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(ApplicationStore), "ListAsync");

    // ── property accessors ─────────────────────────────────────────────────────

    public ValueTask<string?> GetIdAsync(OAuthApplication a, CancellationToken ct) => new(a.Id);
    public ValueTask<string?> GetClientIdAsync(OAuthApplication a, CancellationToken ct) => new(a.ClientId);
    public ValueTask<string?> GetClientSecretAsync(OAuthApplication a, CancellationToken ct) => new(a.ClientSecret);
    public ValueTask<string?> GetApplicationTypeAsync(OAuthApplication a, CancellationToken ct) => new(a.ApplicationType);
    public ValueTask<string?> GetClientTypeAsync(OAuthApplication a, CancellationToken ct) => new(a.ClientType);
    public ValueTask<string?> GetConsentTypeAsync(OAuthApplication a, CancellationToken ct) => new(a.ConsentType);
    public ValueTask<string?> GetDisplayNameAsync(OAuthApplication a, CancellationToken ct) => new(a.DisplayName);
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Cultures(a.DisplayNames));
    public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.KeySet(a.JsonWebKeySet));
    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Array(a.Permissions));
    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Array(a.PostLogoutRedirectUris));
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Properties(a.Properties));
    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Array(a.RedirectUris));
    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Array(a.Requirements));
    public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(OAuthApplication a, CancellationToken ct) => new(OAuthJson.Settings(a.Settings));

    public ValueTask SetClientIdAsync(OAuthApplication a, string? v, CancellationToken ct) { a.ClientId = v; return default; }
    public ValueTask SetClientSecretAsync(OAuthApplication a, string? v, CancellationToken ct) { a.ClientSecret = v; return default; }
    public ValueTask SetApplicationTypeAsync(OAuthApplication a, string? v, CancellationToken ct) { a.ApplicationType = v; return default; }
    public ValueTask SetClientTypeAsync(OAuthApplication a, string? v, CancellationToken ct) { a.ClientType = v; return default; }
    public ValueTask SetConsentTypeAsync(OAuthApplication a, string? v, CancellationToken ct) { a.ConsentType = v; return default; }
    public ValueTask SetDisplayNameAsync(OAuthApplication a, string? v, CancellationToken ct) { a.DisplayName = v; return default; }
    public ValueTask SetDisplayNamesAsync(OAuthApplication a, ImmutableDictionary<CultureInfo, string> v, CancellationToken ct) { a.DisplayNames = OAuthJson.Cultures(v); return default; }
    public ValueTask SetJsonWebKeySetAsync(OAuthApplication a, JsonWebKeySet? v, CancellationToken ct) { a.JsonWebKeySet = OAuthJson.KeySet(v); return default; }
    public ValueTask SetPermissionsAsync(OAuthApplication a, ImmutableArray<string> v, CancellationToken ct) { a.Permissions = OAuthJson.Array(v); return default; }
    public ValueTask SetPostLogoutRedirectUrisAsync(OAuthApplication a, ImmutableArray<string> v, CancellationToken ct) { a.PostLogoutRedirectUris = OAuthJson.Array(v); return default; }
    public ValueTask SetPropertiesAsync(OAuthApplication a, ImmutableDictionary<string, JsonElement> v, CancellationToken ct) { a.Properties = OAuthJson.Properties(v); return default; }
    public ValueTask SetRedirectUrisAsync(OAuthApplication a, ImmutableArray<string> v, CancellationToken ct) { a.RedirectUris = OAuthJson.Array(v); return default; }
    public ValueTask SetRequirementsAsync(OAuthApplication a, ImmutableArray<string> v, CancellationToken ct) { a.Requirements = OAuthJson.Array(v); return default; }
    public ValueTask SetSettingsAsync(OAuthApplication a, ImmutableDictionary<string, string> v, CancellationToken ct) { a.Settings = OAuthJson.Settings(v); return default; }
}
