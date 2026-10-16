using System.Collections.Immutable;
using System.Text.Json;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APP.OAuth.Tests;

public sealed class AuthorizationTokenStoreTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _db;
    private readonly ApplicationStore _apps;
    private readonly AuthorizationStore _authz;
    private readonly TokenStore _tokens;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AuthorizationTokenStoreTests(LocalDbFixture db)
    {
        _db = db;
        _apps = new ApplicationStore(db.Options);
        _authz = new AuthorizationStore(db.Options);
        _tokens = new TokenStore(db.Options);
    }

    private async Task<OAuthApplication> NewApp()
    {
        var app = new OAuthApplication { ClientId = "c-" + Guid.NewGuid().ToString("N"), ClientType = "public" };
        await _apps.CreateAsync(app, Ct);
        return app;
    }

    private async Task<OAuthAuthorization> NewAuthz(OAuthApplication app, string subject = "42", string status = Statuses.Valid,
        string type = AuthorizationTypes.Permanent, string scopes = "[\"mcp\",\"offline_access\"]", DateTimeOffset? created = null)
    {
        var a = new OAuthAuthorization { ApplicationId = app.Id, Subject = subject, Status = status, Type = type, Scopes = scopes, CreationDate = created ?? DateTimeOffset.UtcNow };
        await _authz.CreateAsync(a, Ct);
        return a;
    }

    private async Task<OAuthToken> NewToken(OAuthApplication app, OAuthAuthorization? authz = null, string subject = "42",
        string status = Statuses.Valid, string type = TokenTypeHints.AccessToken, string? reference = null,
        DateTimeOffset? created = null, DateTimeOffset? expires = null)
    {
        var t = new OAuthToken
        {
            ApplicationId = app.Id, AuthorizationId = authz?.Id, Subject = subject, Status = status, Type = type,
            ReferenceId = reference ?? Guid.NewGuid().ToString("N"), CreationDate = created ?? DateTimeOffset.UtcNow,
            ExpirationDate = expires ?? DateTimeOffset.UtcNow.AddHours(1)
        };
        await _tokens.CreateAsync(t, Ct);
        return t;
    }

    private static async Task<List<T>> All<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }

    // ── authorizations ──────────────────────────────────────────────────────────

    [LocalDbFact]
    public async Task Authorization_Find_FiltersBySubjectClientStatusTypeAndScopes()
    {
        var app = await NewApp();
        var other = await NewApp();
        var match = await NewAuthz(app);
        await NewAuthz(app, subject: "99");
        await NewAuthz(app, status: Statuses.Revoked);
        await NewAuthz(other);

        Assert.Single(await All(_authz.FindAsync("42", app.Id, Statuses.Valid, AuthorizationTypes.Permanent, null, Ct)));
        Assert.Single(await All(_authz.FindAsync("42", app.Id, Statuses.Valid, null, ImmutableArray.Create("mcp"), Ct)));
        Assert.Single(await All(_authz.FindAsync("42", app.Id, Statuses.Valid, null, ImmutableArray.Create("mcp", "offline_access"), Ct)));
        Assert.Empty(await All(_authz.FindAsync("42", app.Id, Statuses.Valid, null, ImmutableArray.Create("mcp", "admin"), Ct)));
        Assert.Empty(await All(_authz.FindAsync("42", app.Id, Statuses.Valid, null, ImmutableArray.Create("MCP"), Ct)));
        Assert.Equal(match.Id, (await All(_authz.FindAsync("42", app.Id, Statuses.Valid, null, null, Ct))).Single().Id);
        Assert.Equal(3, (await All(_authz.FindByApplicationIdAsync(app.Id, Ct))).Count);
    }

    [LocalDbFact]
    public async Task Authorization_CompanyId_IsTakenFromProperties()
    {
        var app = await NewApp();
        var a = new OAuthAuthorization { ApplicationId = app.Id, Subject = "1", Status = Statuses.Valid, Type = AuthorizationTypes.Permanent,
            Properties = JsonSerializer.Serialize(new Dictionary<string, object> { ["company_id"] = 123 }) };
        await _authz.CreateAsync(a, Ct);

        Assert.Equal(123, (await _authz.FindByIdAsync(a.Id, Ct))!.CompanyId);
    }

    [LocalDbFact]
    public async Task Authorization_Delete_RemovesItsTokens()
    {
        var app = await NewApp();
        var a = await NewAuthz(app);
        var t = await NewToken(app, a);

        await _authz.DeleteAsync(a, Ct);

        Assert.Null(await _authz.FindByIdAsync(a.Id, Ct));
        Assert.Null(await _tokens.FindByIdAsync(t.Id, Ct));
    }

    [LocalDbFact]
    public async Task Authorization_Revoke_MarksRevoked_AndRotatesConcurrencyToken()
    {
        var app = await NewApp();
        var a = await NewAuthz(app, subject: "rev-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(1, await _authz.RevokeBySubjectAsync(a.Subject!, Ct));

        a.Status = Statuses.Inactive;   // stale token: a concurrent writer must now fail
        await Assert.ThrowsAsync<OpenIddictExceptions.ConcurrencyException>(async () => await _authz.UpdateAsync(a, Ct));
        Assert.Equal(Statuses.Revoked, (await _authz.FindByIdAsync(a.Id, Ct))!.Status);
    }

    [LocalDbFact]
    public async Task Authorization_Prune_RemovesOldInvalidAndOrphanAdHoc_KeepsOthers()
    {
        var app = await NewApp();
        var old = DateTimeOffset.UtcNow.AddDays(-30);
        var oldRevoked = await NewAuthz(app, status: Statuses.Revoked, created: old);
        var oldAdHocUnused = await NewAuthz(app, type: AuthorizationTypes.AdHoc, created: old);
        var oldValidPermanent = await NewAuthz(app, created: old);
        var freshRevoked = await NewAuthz(app, status: Statuses.Revoked);

        await _authz.PruneAsync(DateTimeOffset.UtcNow.AddDays(-7), Ct);

        Assert.Null(await _authz.FindByIdAsync(oldRevoked.Id, Ct));
        Assert.Null(await _authz.FindByIdAsync(oldAdHocUnused.Id, Ct));
        Assert.NotNull(await _authz.FindByIdAsync(oldValidPermanent.Id, Ct));
        Assert.NotNull(await _authz.FindByIdAsync(freshRevoked.Id, Ct));
    }

    // ── tokens ──────────────────────────────────────────────────────────────────

    [LocalDbFact]
    public async Task Token_RoundTrips_DatesInUtc_AndReferenceIdIsCaseSensitive()
    {
        var app = await NewApp();
        var reference = "AbC" + Guid.NewGuid().ToString("N");
        var created = new DateTimeOffset(2026, 10, 15, 12, 30, 45, 123, TimeSpan.FromHours(8));
        var t = await NewToken(app, reference: reference, created: created);

        var loaded = (await _tokens.FindByReferenceIdAsync(reference, Ct))!;

        Assert.Equal(t.Id, loaded.Id);
        Assert.Equal(created.UtcDateTime, loaded.CreationDate!.Value.UtcDateTime);
        Assert.Null(await _tokens.FindByReferenceIdAsync(reference.ToLowerInvariant(), Ct));
        Assert.Null(await _tokens.FindByReferenceIdAsync("does-not-exist", Ct));
    }

    [LocalDbFact]
    public async Task Token_Type_AcceptsOpenIddictUrnValues()
    {
        // Found by the end-to-end run: OpenIddict 7 stores token types as URNs, longer than the first column size.
        var app = await NewApp();
        const string urn = "urn:openiddict:params:oauth:token-type:authorization_code";
        var t = await NewToken(app, type: urn);
        var a = await NewAuthz(app, type: "urn:openiddict:params:oauth:token-type:permanent-authorization-with-a-long-name");

        Assert.Equal(urn, (await _tokens.FindByIdAsync(t.Id, Ct))!.Type);
        Assert.Equal(a.Type, (await _authz.FindByIdAsync(a.Id, Ct))!.Type);
    }

    [LocalDbFact]
    public async Task Token_ReferenceId_IsUnique()
    {
        var app = await NewApp();
        var reference = Guid.NewGuid().ToString("N");
        await NewToken(app, reference: reference);

        await Assert.ThrowsAnyAsync<Exception>(async () => await NewToken(app, reference: reference));
    }

    [LocalDbFact]
    public async Task Token_Find_FiltersAndByAuthorization()
    {
        var app = await NewApp();
        var a = await NewAuthz(app);
        await NewToken(app, a, type: TokenTypeHints.AccessToken);
        await NewToken(app, a, type: TokenTypeHints.RefreshToken);
        await NewToken(app, a, status: Statuses.Redeemed, type: TokenTypeHints.RefreshToken);

        Assert.Single(await All(_tokens.FindAsync("42", app.Id, Statuses.Valid, TokenTypeHints.RefreshToken, Ct)));
        Assert.Equal(3, (await All(_tokens.FindByAuthorizationIdAsync(a.Id, Ct))).Count);
        Assert.Equal(3, (await All(_tokens.FindBySubjectAsync("42", Ct))).Count(t => t.AuthorizationId == a.Id));
    }

    [LocalDbFact]
    public async Task Token_ConcurrentRedeem_ExactlyOneWins()
    {
        var app = await NewApp();
        var t = await NewToken(app, type: TokenTypeHints.RefreshToken);
        var one = (await _tokens.FindByIdAsync(t.Id, Ct))!;
        var two = (await _tokens.FindByIdAsync(t.Id, Ct))!;
        one.Status = Statuses.Redeemed; one.RedemptionDate = DateTimeOffset.UtcNow;
        two.Status = Statuses.Redeemed; two.RedemptionDate = DateTimeOffset.UtcNow;

        var results = await Task.WhenAll(Try(one), Try(two));

        Assert.Equal(1, results.Count(r => r));   // the second redemption of the same refresh token is rejected
        Assert.Equal(Statuses.Redeemed, (await _tokens.FindByIdAsync(t.Id, Ct))!.Status);

        async Task<bool> Try(OAuthToken token)
        {
            try { await _tokens.UpdateAsync(token, Ct); return true; }
            catch (OpenIddictExceptions.ConcurrencyException) { return false; }
        }
    }

    [LocalDbFact]
    public async Task Token_RevokeByAuthorization_RevokesOnlyThatChain()
    {
        var app = await NewApp();
        var chain = await NewAuthz(app);
        var otherChain = await NewAuthz(app);
        var inChain = await NewToken(app, chain);
        var notInChain = await NewToken(app, otherChain);

        Assert.Equal(1, await _tokens.RevokeByAuthorizationIdAsync(chain.Id, Ct));

        Assert.Equal(Statuses.Revoked, (await _tokens.FindByIdAsync(inChain.Id, Ct))!.Status);
        Assert.Equal(Statuses.Valid, (await _tokens.FindByIdAsync(notInChain.Id, Ct))!.Status);
    }

    [LocalDbFact]
    public async Task Token_Revoke_ByFilters_AndByApplication()
    {
        var app = await NewApp();
        var subject = "s-" + Guid.NewGuid().ToString("N");
        await NewToken(app, subject: subject, type: TokenTypeHints.AccessToken);
        await NewToken(app, subject: subject, type: TokenTypeHints.RefreshToken);

        Assert.Equal(1, await _tokens.RevokeAsync(subject, app.Id, Statuses.Valid, TokenTypeHints.AccessToken, Ct));
        Assert.Equal(1, (await All(_tokens.FindBySubjectAsync(subject, Ct))).Count(t => t.Status == Statuses.Revoked));

        Assert.Equal(2, await _tokens.RevokeBySubjectAsync(subject, Ct));   // matches both rows, including the revoked one
        Assert.All(await All(_tokens.FindBySubjectAsync(subject, Ct)), t => Assert.Equal(Statuses.Revoked, t.Status));

        Assert.Equal(2, await _tokens.RevokeByApplicationIdAsync(app.Id, Ct));
    }

    [LocalDbFact]
    public async Task Token_Prune_RemovesOldUnusable_KeepsFreshAndValid()
    {
        var app = await NewApp();
        var old = DateTimeOffset.UtcNow.AddDays(-30);
        var oldRedeemed = await NewToken(app, status: Statuses.Redeemed, created: old);
        var oldExpired = await NewToken(app, created: old, expires: DateTimeOffset.UtcNow.AddDays(-20));
        var oldStillValid = await NewToken(app, created: old, expires: DateTimeOffset.UtcNow.AddDays(5));
        var freshRedeemed = await NewToken(app, status: Statuses.Redeemed);
        var revokedAuthz = await NewAuthz(app, status: Statuses.Revoked, created: old);
        var oldOfRevokedAuthz = await NewToken(app, revokedAuthz, created: old, expires: DateTimeOffset.UtcNow.AddDays(5));

        await _tokens.PruneAsync(DateTimeOffset.UtcNow.AddDays(-7), Ct);

        Assert.Null(await _tokens.FindByIdAsync(oldRedeemed.Id, Ct));
        Assert.Null(await _tokens.FindByIdAsync(oldExpired.Id, Ct));
        Assert.Null(await _tokens.FindByIdAsync(oldOfRevokedAuthz.Id, Ct));
        Assert.NotNull(await _tokens.FindByIdAsync(oldStillValid.Id, Ct));
        Assert.NotNull(await _tokens.FindByIdAsync(freshRedeemed.Id, Ct));
    }

    [LocalDbFact]
    public async Task Token_LinqQueryOverloads_Throw_NotSupported()
    {
        await Assert.ThrowsAsync<NotSupportedException>(async () => await _tokens.GetAsync<int, int>((q, s) => q.Select(t => s), 1, Ct));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await _authz.CountAsync<int>(q => q.Select(a => 1), Ct));
    }
}
