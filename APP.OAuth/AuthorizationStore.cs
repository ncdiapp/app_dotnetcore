using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>OpenIddict authorization (user consent) store on dbo.AppOAuthAuthorization.</summary>
internal sealed class AuthorizationStore : SqlStoreBase<OAuthAuthorization>, IOpenIddictAuthorizationStore<OAuthAuthorization>
{
    public AuthorizationStore(IOptions<OAuthDbOptions> options) : base(options) { }

    protected override string Table => "AppOAuthAuthorization";
    protected override string IdColumn => "AuthorizationId";
    protected override string[] DataColumns { get; } =
        { "ApplicationId", "CompanyId", "Subject", "Status", "Type", "Scopes", "Properties", "CreationDate" };

    protected override object?[] Values(OAuthAuthorization a) =>
        new object?[] { a.ApplicationId, a.CompanyId, a.Subject, a.Status, a.Type, a.Scopes, a.Properties, a.CreationDate };

    protected override void Read(SqlDataReader r, OAuthAuthorization a)
    {
        a.ApplicationId = S(r, "ApplicationId"); a.CompanyId = I(r, "CompanyId"); a.Subject = S(r, "Subject");
        a.Status = S(r, "Status"); a.Type = S(r, "Type"); a.Scopes = S(r, "Scopes");
        a.Properties = S(r, "Properties"); a.CreationDate = D(r, "CreationDate");
    }

    public override ValueTask CreateAsync(OAuthAuthorization a, CancellationToken ct)
    {
        a.CompanyId ??= OAuthJson.CompanyId(a.Properties);
        return base.CreateAsync(a, ct);
    }

    // Authorization -> token has no cascade in the schema, so remove the tokens first, in one transaction.
    public override async ValueTask DeleteAsync(OAuthAuthorization a, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct).ConfigureAwait(false);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (var tokens = Command(c, "DELETE FROM dbo.AppOAuthToken WHERE AuthorizationId = @id", new[] { P("@id", a.Id) }, tx))
            await tokens.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        int deleted;
        await using (var row = Command(c, "DELETE FROM dbo.AppOAuthAuthorization WHERE AuthorizationId = @id AND ConcurrencyToken = @token",
                         new[] { P("@id", a.Id), P("@token", a.ConcurrencyToken) }, tx))
            deleted = await row.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (deleted == 0)
            throw new OpenIddictExceptions.ConcurrencyException("AppOAuthAuthorization " + a.Id + " was modified or deleted by another request.");
        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    // ── lookups ────────────────────────────────────────────────────────────────

    public IAsyncEnumerable<OAuthAuthorization> FindAsync(string? subject, string? client, string? status, string? type,
        ImmutableArray<string>? scopes, CancellationToken ct)
    {
        var ps = new List<SqlParameter>();
        var where = Filter(ps, subject, client, status, type);
        if (scopes is { IsDefaultOrEmpty: false } required)
            for (var i = 0; i < required.Length; i++)
            {
                where += " AND EXISTS (SELECT 1 FROM OPENJSON(Scopes) WHERE [value] = @s" + i + " COLLATE Latin1_General_100_BIN2)";
                ps.Add(P("@s" + i, required[i]));
            }
        return Stream(QueryAsync(where, ps, ct), ct);
    }

    public IAsyncEnumerable<OAuthAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken ct) =>
        Stream(QueryAsync("ApplicationId = @id", new[] { P("@id", identifier) }, ct), ct);

    public IAsyncEnumerable<OAuthAuthorization> FindBySubjectAsync(string subject, CancellationToken ct) =>
        Stream(QueryAsync("Subject = @subject", new[] { P("@subject", subject) }, ct), ct);

    // ── admin listing: valid connections, optionally for one company and/or one user ──

    public async Task<List<OAuthConnection>> ListConnectionsAsync(int? companyId, int? userId, CancellationToken ct)
    {
        const string sql =
            "SELECT a.AuthorizationId, ap.ClientId, ap.DisplayName, a.Subject, a.CompanyId, a.CreationDate, a.Scopes, a.Properties " +
            "FROM dbo.AppOAuthAuthorization a JOIN dbo.AppOAuthApplication ap ON ap.ApplicationId = a.ApplicationId " +
            "WHERE a.Status = N'valid' AND (@company IS NULL OR a.CompanyId = @company) AND (@subject IS NULL OR a.Subject = @subject) " +
            "ORDER BY a.CreationDate DESC";

        await using var c = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = Command(c, sql, new[] { P("@company", companyId), P("@subject", userId?.ToString(System.Globalization.CultureInfo.InvariantCulture)) });
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var rows = new List<OAuthConnection>();
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            if (!int.TryParse(S(r, "Subject"), out var subject)) continue;
            rows.Add(new OAuthConnection(S(r, "AuthorizationId")!, S(r, "ClientId") ?? "", S(r, "DisplayName") ?? S(r, "ClientId") ?? "",
                subject, I(r, "CompanyId") ?? 0, D(r, "CreationDate"), S(r, "Scopes") ?? "[]", GrantSessionOf(S(r, "Properties"))));
        }
        return rows;
    }

    private static string? GrantSessionOf(string? propertiesJson)
    {
        if (string.IsNullOrEmpty(propertiesJson)) return null;
        var props = OAuthJson.Properties(propertiesJson);
        return props.TryGetValue(OAuthClaims.GrantSession, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    // ── revoke / prune ─────────────────────────────────────────────────────────

    public async ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken ct)
    {
        var ps = new List<SqlParameter>();
        return await RevokeWhereAsync(Filter(ps, subject, client, status, type), ps, ct).ConfigureAwait(false);
    }

    public async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken ct) =>
        await RevokeWhereAsync("ApplicationId = @id", new[] { P("@id", identifier) }, ct).ConfigureAwait(false);

    public async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken ct) =>
        await RevokeWhereAsync("Subject = @subject", new[] { P("@subject", subject) }, ct).ConfigureAwait(false);

    // Old authorizations that are no longer valid, or ad-hoc ones nothing refers to. Authorizations that still
    // have tokens are kept (the token prune removes tokens of invalid authorizations first).
    public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken ct) =>
        await ExecAsync(
            "DELETE FROM dbo.AppOAuthAuthorization WHERE CreationDate < @threshold " +
            "AND (Status <> N'" + OpenIddictConstants.Statuses.Valid + "' OR Type = N'" + OpenIddictConstants.AuthorizationTypes.AdHoc + "') " +
            "AND NOT EXISTS (SELECT 1 FROM dbo.AppOAuthToken t WHERE t.AuthorizationId = dbo.AppOAuthAuthorization.AuthorizationId)",
            new[] { P("@threshold", threshold) }, ct).ConfigureAwait(false);

    // ── LINQ-query overloads: not supported (see SqlStoreBase) ─────────────────

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<OAuthAuthorization>, IQueryable<TResult>> query, CancellationToken ct) =>
        throw QueryNotSupported(nameof(AuthorizationStore), "CountAsync");

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<OAuthAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(AuthorizationStore), "GetAsync");

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<OAuthAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(AuthorizationStore), "ListAsync");

    // ── property accessors ─────────────────────────────────────────────────────

    public ValueTask<string?> GetIdAsync(OAuthAuthorization a, CancellationToken ct) => new(a.Id);
    public ValueTask<string?> GetApplicationIdAsync(OAuthAuthorization a, CancellationToken ct) => new(a.ApplicationId);
    public ValueTask<DateTimeOffset?> GetCreationDateAsync(OAuthAuthorization a, CancellationToken ct) => new(a.CreationDate);
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(OAuthAuthorization a, CancellationToken ct) => new(OAuthJson.Properties(a.Properties));
    public ValueTask<ImmutableArray<string>> GetScopesAsync(OAuthAuthorization a, CancellationToken ct) => new(OAuthJson.Array(a.Scopes));
    public ValueTask<string?> GetStatusAsync(OAuthAuthorization a, CancellationToken ct) => new(a.Status);
    public ValueTask<string?> GetSubjectAsync(OAuthAuthorization a, CancellationToken ct) => new(a.Subject);
    public ValueTask<string?> GetTypeAsync(OAuthAuthorization a, CancellationToken ct) => new(a.Type);

    public ValueTask SetApplicationIdAsync(OAuthAuthorization a, string? v, CancellationToken ct) { a.ApplicationId = v; return default; }
    public ValueTask SetCreationDateAsync(OAuthAuthorization a, DateTimeOffset? v, CancellationToken ct) { a.CreationDate = v; return default; }
    public ValueTask SetPropertiesAsync(OAuthAuthorization a, ImmutableDictionary<string, JsonElement> v, CancellationToken ct) { a.Properties = OAuthJson.Properties(v); return default; }
    public ValueTask SetScopesAsync(OAuthAuthorization a, ImmutableArray<string> v, CancellationToken ct) { a.Scopes = OAuthJson.Array(v); return default; }
    public ValueTask SetStatusAsync(OAuthAuthorization a, string? v, CancellationToken ct) { a.Status = v; return default; }
    public ValueTask SetSubjectAsync(OAuthAuthorization a, string? v, CancellationToken ct) { a.Subject = v; return default; }
    public ValueTask SetTypeAsync(OAuthAuthorization a, string? v, CancellationToken ct) { a.Type = v; return default; }
}
