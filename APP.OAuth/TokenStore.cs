using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>
/// OpenIddict token store on dbo.AppOAuthToken: authorization codes, access tokens and refresh tokens.
/// Opaque (reference) tokens are looked up by ReferenceId on every /mcp call, so that lookup is indexed and exact.
/// </summary>
internal sealed class TokenStore : SqlStoreBase<OAuthToken>, IOpenIddictTokenStore<OAuthToken>
{
    public TokenStore(IOptions<OAuthDbOptions> options) : base(options) { }

    protected override string Table => "AppOAuthToken";
    protected override string IdColumn => "TokenId";
    protected override string[] DataColumns { get; } =
    {
        "ApplicationId", "AuthorizationId", "CompanyId", "Subject", "Status", "Type", "ReferenceId", "Payload",
        "Properties", "CreationDate", "ExpirationDate", "RedemptionDate"
    };

    protected override object?[] Values(OAuthToken t) => new object?[]
    {
        t.ApplicationId, t.AuthorizationId, t.CompanyId, t.Subject, t.Status, t.Type, t.ReferenceId, t.Payload,
        t.Properties, t.CreationDate, t.ExpirationDate, t.RedemptionDate
    };

    protected override void Read(SqlDataReader r, OAuthToken t)
    {
        t.ApplicationId = S(r, "ApplicationId"); t.AuthorizationId = S(r, "AuthorizationId"); t.CompanyId = I(r, "CompanyId");
        t.Subject = S(r, "Subject"); t.Status = S(r, "Status"); t.Type = S(r, "Type"); t.ReferenceId = S(r, "ReferenceId");
        t.Payload = S(r, "Payload"); t.Properties = S(r, "Properties"); t.CreationDate = D(r, "CreationDate");
        t.ExpirationDate = D(r, "ExpirationDate"); t.RedemptionDate = D(r, "RedemptionDate");
    }

    public override ValueTask CreateAsync(OAuthToken t, CancellationToken ct)
    {
        t.CompanyId ??= OAuthJson.CompanyId(t.Properties);
        return base.CreateAsync(t, ct);
    }

    // ── lookups ────────────────────────────────────────────────────────────────

    public async ValueTask<OAuthToken?> FindByReferenceIdAsync(string identifier, CancellationToken ct) =>
        await QueryOneAsync("ReferenceId = @ref", new[] { P("@ref", identifier) }, ct).ConfigureAwait(false);

    public IAsyncEnumerable<OAuthToken> FindAsync(string? subject, string? client, string? status, string? type, CancellationToken ct)
    {
        var ps = new List<SqlParameter>();
        return Stream(QueryAsync(Filter(ps, subject, client, status, type), ps, ct), ct);
    }

    public IAsyncEnumerable<OAuthToken> FindByApplicationIdAsync(string identifier, CancellationToken ct) =>
        Stream(QueryAsync("ApplicationId = @id", new[] { P("@id", identifier) }, ct), ct);

    public IAsyncEnumerable<OAuthToken> FindByAuthorizationIdAsync(string identifier, CancellationToken ct) =>
        Stream(QueryAsync("AuthorizationId = @id", new[] { P("@id", identifier) }, ct), ct);

    public IAsyncEnumerable<OAuthToken> FindBySubjectAsync(string subject, CancellationToken ct) =>
        Stream(QueryAsync("Subject = @subject", new[] { P("@subject", subject) }, ct), ct);

    // ── revoke / prune ─────────────────────────────────────────────────────────

    public async ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken ct)
    {
        var ps = new List<SqlParameter>();
        return await RevokeWhereAsync(Filter(ps, subject, client, status, type), ps, ct).ConfigureAwait(false);
    }

    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken ct) =>
        await RevokeWhereAsync("ApplicationId = @id", new[] { P("@id", identifier) }, ct).ConfigureAwait(false);

    public async ValueTask<long> RevokeByAuthorizationIdAsync(string identifier, CancellationToken ct) =>
        await RevokeWhereAsync("AuthorizationId = @id", new[] { P("@id", identifier) }, ct).ConfigureAwait(false);

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken ct) =>
        await RevokeWhereAsync("Subject = @subject", new[] { P("@subject", subject) }, ct).ConfigureAwait(false);

    // Old tokens that are no longer usable: not inactive/valid, expired, or belonging to an authorization that is
    // no longer valid. Run on a schedule (OpenIddict only provides the call).
    public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken ct) =>
        await ExecAsync(
            "DELETE t FROM dbo.AppOAuthToken t LEFT JOIN dbo.AppOAuthAuthorization a ON a.AuthorizationId = t.AuthorizationId " +
            "WHERE t.CreationDate < @threshold AND (" +
            "t.Status NOT IN (N'" + OpenIddictConstants.Statuses.Inactive + "', N'" + OpenIddictConstants.Statuses.Valid + "') " +
            "OR (a.AuthorizationId IS NOT NULL AND a.Status <> N'" + OpenIddictConstants.Statuses.Valid + "') " +
            "OR t.ExpirationDate < @now)",
            new[] { P("@threshold", threshold), P("@now", DateTimeOffset.UtcNow) }, ct).ConfigureAwait(false);

    // ── LINQ-query overloads: not supported (see SqlStoreBase) ─────────────────

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<OAuthToken>, IQueryable<TResult>> query, CancellationToken ct) =>
        throw QueryNotSupported(nameof(TokenStore), "CountAsync");

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<OAuthToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(TokenStore), "GetAsync");

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<OAuthToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(TokenStore), "ListAsync");

    // ── property accessors ─────────────────────────────────────────────────────

    public ValueTask<string?> GetIdAsync(OAuthToken t, CancellationToken ct) => new(t.Id);
    public ValueTask<string?> GetApplicationIdAsync(OAuthToken t, CancellationToken ct) => new(t.ApplicationId);
    public ValueTask<string?> GetAuthorizationIdAsync(OAuthToken t, CancellationToken ct) => new(t.AuthorizationId);
    public ValueTask<DateTimeOffset?> GetCreationDateAsync(OAuthToken t, CancellationToken ct) => new(t.CreationDate);
    public ValueTask<DateTimeOffset?> GetExpirationDateAsync(OAuthToken t, CancellationToken ct) => new(t.ExpirationDate);
    public ValueTask<string?> GetPayloadAsync(OAuthToken t, CancellationToken ct) => new(t.Payload);
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(OAuthToken t, CancellationToken ct) => new(OAuthJson.Properties(t.Properties));
    public ValueTask<DateTimeOffset?> GetRedemptionDateAsync(OAuthToken t, CancellationToken ct) => new(t.RedemptionDate);
    public ValueTask<string?> GetReferenceIdAsync(OAuthToken t, CancellationToken ct) => new(t.ReferenceId);
    public ValueTask<string?> GetStatusAsync(OAuthToken t, CancellationToken ct) => new(t.Status);
    public ValueTask<string?> GetSubjectAsync(OAuthToken t, CancellationToken ct) => new(t.Subject);
    public ValueTask<string?> GetTypeAsync(OAuthToken t, CancellationToken ct) => new(t.Type);

    public ValueTask SetApplicationIdAsync(OAuthToken t, string? v, CancellationToken ct) { t.ApplicationId = v; return default; }
    public ValueTask SetAuthorizationIdAsync(OAuthToken t, string? v, CancellationToken ct) { t.AuthorizationId = v; return default; }
    public ValueTask SetCreationDateAsync(OAuthToken t, DateTimeOffset? v, CancellationToken ct) { t.CreationDate = v; return default; }
    public ValueTask SetExpirationDateAsync(OAuthToken t, DateTimeOffset? v, CancellationToken ct) { t.ExpirationDate = v; return default; }
    public ValueTask SetPayloadAsync(OAuthToken t, string? v, CancellationToken ct) { t.Payload = v; return default; }
    public ValueTask SetPropertiesAsync(OAuthToken t, ImmutableDictionary<string, JsonElement> v, CancellationToken ct) { t.Properties = OAuthJson.Properties(v); return default; }
    public ValueTask SetRedemptionDateAsync(OAuthToken t, DateTimeOffset? v, CancellationToken ct) { t.RedemptionDate = v; return default; }
    public ValueTask SetReferenceIdAsync(OAuthToken t, string? v, CancellationToken ct) { t.ReferenceId = v; return default; }
    public ValueTask SetStatusAsync(OAuthToken t, string? v, CancellationToken ct) { t.Status = v; return default; }
    public ValueTask SetSubjectAsync(OAuthToken t, string? v, CancellationToken ct) { t.Subject = v; return default; }
    public ValueTask SetTypeAsync(OAuthToken t, string? v, CancellationToken ct) { t.Type = v; return default; }
}
