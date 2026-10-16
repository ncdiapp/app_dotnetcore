using System.Collections.Immutable;
using OpenIddict.Abstractions;

namespace APP.OAuth.Tests;

public sealed class ApplicationScopeStoreTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _db;
    private readonly ApplicationStore _apps;
    private readonly ScopeStore _scopes;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public ApplicationScopeStoreTests(LocalDbFixture db)
    {
        _db = db;
        _apps = new ApplicationStore(db.Options);
        _scopes = new ScopeStore(db.Options);
    }

    private static string Unique(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N");

    private static OAuthApplication NewApp(string clientId, params string[] redirects) => new()
    {
        ClientId = clientId,
        ClientType = "public",
        RedirectUris = OAuthJson.Array(redirects.ToImmutableArray()),
        Permissions = OAuthJson.Array(ImmutableArray.Create("ept:token", "gt:authorization_code"))
    };

    private static async Task<List<T>> All<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }

    // ── applications ────────────────────────────────────────────────────────────

    [LocalDbFact]
    public async Task Application_RoundTrips_AllFields()
    {
        var app = NewApp(Unique("rt"), "https://claude.ai/api/mcp/auth_callback");
        app.DisplayName = "Claude é"; app.ConsentType = "explicit"; app.Settings = "{\"a\":\"b\"}";
        await _apps.CreateAsync(app, Ct);

        var loaded = await _apps.FindByIdAsync(app.Id, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(app.ClientId, loaded!.ClientId);
        Assert.Equal("Claude é", loaded.DisplayName);
        Assert.Equal(app.RedirectUris, loaded.RedirectUris);
        Assert.Equal(app.Permissions, loaded.Permissions);
        Assert.Equal(app.Settings, loaded.Settings);
        Assert.Equal(app.ConcurrencyToken, loaded.ConcurrencyToken);
    }

    [LocalDbFact]
    public async Task ClientId_IsCaseSensitive_AndUnique()
    {
        var id = Unique("Case");
        await _apps.CreateAsync(NewApp(id), Ct);
        await _apps.CreateAsync(NewApp(id.ToLowerInvariant()), Ct);   // different case = different client

        Assert.Equal(id, (await _apps.FindByClientIdAsync(id, Ct))!.ClientId);
        Assert.Equal(id.ToLowerInvariant(), (await _apps.FindByClientIdAsync(id.ToLowerInvariant(), Ct))!.ClientId);
        Assert.Null(await _apps.FindByClientIdAsync(id.ToUpperInvariant(), Ct));
        await Assert.ThrowsAnyAsync<Exception>(async () => await _apps.CreateAsync(NewApp(id), Ct));   // same id twice
    }

    [LocalDbFact]
    public async Task FindByRedirectUri_IsExact()
    {
        var app = NewApp(Unique("uri"), "https://claude.ai/api/mcp/auth_callback", "http://localhost/callback");
        await _apps.CreateAsync(app, Ct);

        Assert.Contains(await All(_apps.FindByRedirectUriAsync("http://localhost/callback", Ct)), a => a.Id == app.Id);
        Assert.Empty(await All(_apps.FindByRedirectUriAsync("HTTPS://claude.ai/api/mcp/auth_callback", Ct)));
        Assert.Empty(await All(_apps.FindByRedirectUriAsync("https://claude.ai/api/mcp/auth_callback/", Ct)));
        Assert.Empty(await All(_apps.FindByRedirectUriAsync("https://evil.example/cb", Ct)));
    }

    [LocalDbFact]
    public async Task Update_WithStaleToken_ThrowsConcurrency()
    {
        var app = NewApp(Unique("conc"));
        await _apps.CreateAsync(app, Ct);
        var first = (await _apps.FindByIdAsync(app.Id, Ct))!;
        var second = (await _apps.FindByIdAsync(app.Id, Ct))!;

        first.DisplayName = "one";
        await _apps.UpdateAsync(first, Ct);
        second.DisplayName = "two";

        await Assert.ThrowsAsync<OpenIddictExceptions.ConcurrencyException>(async () => await _apps.UpdateAsync(second, Ct));
        Assert.Equal("one", (await _apps.FindByIdAsync(app.Id, Ct))!.DisplayName);
    }

    [LocalDbFact]
    public async Task Update_RotatesToken_SoSameInstanceCanUpdateAgain()
    {
        var app = NewApp(Unique("rot"));
        await _apps.CreateAsync(app, Ct);
        var before = app.ConcurrencyToken;

        app.DisplayName = "x";
        await _apps.UpdateAsync(app, Ct);
        app.DisplayName = "y";
        await _apps.UpdateAsync(app, Ct);

        Assert.NotEqual(before, app.ConcurrencyToken);
        Assert.Equal("y", (await _apps.FindByIdAsync(app.Id, Ct))!.DisplayName);
    }

    [LocalDbFact]
    public async Task DeletingApplication_CascadesToAuthorizationsAndTokens()
    {
        var app = NewApp(Unique("cascade"));
        await _apps.CreateAsync(app, Ct);
        var authz = new OAuthAuthorization { ApplicationId = app.Id, Subject = "7", Status = "valid", Type = "permanent" };
        await new AuthorizationStore(_db.Options).CreateAsync(authz, Ct);
        await new TokenStore(_db.Options).CreateAsync(new OAuthToken { ApplicationId = app.Id, AuthorizationId = authz.Id, Subject = "7", Status = "valid", Type = "access_token" }, Ct);

        await _apps.DeleteAsync(app, Ct);

        Assert.Equal(0, _db.Scalar<int>("SELECT COUNT(*) FROM dbo.AppOAuthAuthorization WHERE ApplicationId = '" + app.Id + "'"));
        Assert.Equal(0, _db.Scalar<int>("SELECT COUNT(*) FROM dbo.AppOAuthToken WHERE ApplicationId = '" + app.Id + "'"));
    }

    [LocalDbFact]
    public async Task ListAndCount_Work()
    {
        for (var i = 0; i < 3; i++) await _apps.CreateAsync(NewApp(Unique("list")), Ct);

        var count = await _apps.CountAsync(Ct);
        var page = await All(_apps.ListAsync(2, 0, Ct));

        Assert.True(count >= 3);
        Assert.Equal(2, page.Count);
    }

    // ── scopes ──────────────────────────────────────────────────────────────────

    [LocalDbFact]
    public async Task Scope_FindByName_ByNames_AndByResource()
    {
        var name = Unique("mcp");
        var resource = "https://host/" + Unique("res");
        await _scopes.CreateAsync(new OAuthScope { Name = name, Resources = OAuthJson.Array(ImmutableArray.Create(resource)) }, Ct);
        await _scopes.CreateAsync(new OAuthScope { Name = name + "-b" }, Ct);

        Assert.Equal(name, (await _scopes.FindByNameAsync(name, Ct))!.Name);
        Assert.Null(await _scopes.FindByNameAsync(name.ToUpperInvariant(), Ct));
        Assert.Equal(2, (await All(_scopes.FindByNamesAsync(ImmutableArray.Create(name, name + "-b", "nope"), Ct))).Count);
        Assert.Empty(await All(_scopes.FindByNamesAsync(ImmutableArray<string>.Empty, Ct)));
        Assert.Single(await All(_scopes.FindByResourceAsync(resource, Ct)));
        Assert.Empty(await All(_scopes.FindByResourceAsync(resource.ToUpperInvariant(), Ct)));
    }

    // ── LINQ-query overloads are deliberately unsupported ──────────────────────

    [LocalDbFact]
    public async Task LinqQueryOverloads_Throw_NotSupported()
    {
        await Assert.ThrowsAsync<NotSupportedException>(async () => await _apps.CountAsync<int>(q => q.Select(a => 1), Ct));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await _apps.GetAsync<int, int>((q, s) => q.Select(a => s), 1, Ct));
        await Assert.ThrowsAsync<NotSupportedException>(async () => await All(_scopes.ListAsync<int, int>((q, s) => q.Select(a => s), 1, Ct)));
    }
}
