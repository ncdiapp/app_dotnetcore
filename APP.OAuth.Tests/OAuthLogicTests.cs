using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace APP.OAuth.Tests;

public sealed class ConsentTicketTests
{
    private static readonly OAuthWebUser Alice = new(42, 7, "alice");

    private static string Hash(string state = "s", string challenge = "c") => ConsentTicketService.HashRequest(k => k switch
    {
        "client_id" => "client", "redirect_uri" => "https://claude.ai/cb", "scope" => "mcp", "resource" => "https://h/appai/mcp",
        "code_challenge" => challenge, "state" => state, _ => null
    });

    [Fact]
    public void Ticket_RoundTrips_ForTheSameRequest()
    {
        var svc = new ConsentTicketService(new EphemeralDataProtectionProvider());
        var ticket = svc.Create(Alice, Hash());

        Assert.True(svc.TryValidate(ticket, Hash(), out var user));
        Assert.Equal(Alice, user);
    }

    [Theory]
    [InlineData("other-state", "c")]
    [InlineData("s", "other-challenge")]
    public void Ticket_IsBoundToTheExactRequest(string state, string challenge)
    {
        var svc = new ConsentTicketService(new EphemeralDataProtectionProvider());
        var ticket = svc.Create(Alice, Hash());

        Assert.False(svc.TryValidate(ticket, Hash(state, challenge), out _));
    }

    [Fact]
    public void Ticket_FromAnotherKeyRing_OrTampered_OrEmpty_IsRefused()
    {
        var svc = new ConsentTicketService(new EphemeralDataProtectionProvider());
        var ticket = svc.Create(Alice, Hash());
        var other = new ConsentTicketService(new EphemeralDataProtectionProvider());

        Assert.False(other.TryValidate(ticket, Hash(), out _));
        Assert.False(svc.TryValidate(ticket + "x", Hash(), out _));
        Assert.False(svc.TryValidate(ticket[..^4], Hash(), out _));
        Assert.False(svc.TryValidate("", Hash(), out _));
        Assert.False(svc.TryValidate(null, Hash(), out _));
        Assert.False(svc.TryValidate("Zm9yZ2Vk", Hash(), out _));
    }

    [Fact]
    public void HashRequest_ChangesWithEveryBoundValue_AndIgnoresOthers()
    {
        string? H(string key, string value) => ConsentTicketService.HashRequest(k => k == key ? value : "x");
        var baseline = ConsentTicketService.HashRequest(_ => "x");

        foreach (var key in new[] { "client_id", "redirect_uri", "scope", "resource", "code_challenge", "state" })
            Assert.NotEqual(baseline, H(key, "different"));
        Assert.Equal(baseline, ConsentTicketService.HashRequest(k => new[] { "client_id", "redirect_uri", "scope", "resource", "code_challenge", "state" }.Contains(k) ? "x" : "ignored"));
    }
}

public sealed class DynamicClientRegistrationTests
{
    private static readonly OAuthOptions Opts = new();   // default allow-list: claude.ai, claude.com, chatgpt.com, chat.openai.com

    private static ClientRegistration? Parse(string json, out (string Code, string Text) error) =>
        DynamicClientRegistration.Parse(JsonDocument.Parse(json).RootElement, Opts, out error);

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback", true)]
    [InlineData("https://CLAUDE.AI/cb", true)]
    [InlineData("https://chatgpt.com/connector/oauth/abc", true)]
    [InlineData("http://localhost:3118/callback", true)]
    [InlineData("http://127.0.0.1/callback", true)]
    [InlineData("http://[::1]:8080/cb", true)]
    [InlineData("https://evil.example/cb", false)]
    [InlineData("https://claude.ai.evil.example/cb", false)]
    [InlineData("https://sub.claude.ai/cb", false)]            // exact hosts only
    [InlineData("http://claude.ai/cb", false)]                 // http only on loopback
    [InlineData("http://localhost.evil.example/cb", false)]
    [InlineData("https://claude.ai/cb#frag", false)]
    [InlineData("https://user:pw@claude.ai/cb", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("custom-scheme://callback", false)]
    [InlineData("/relative", false)]
    [InlineData("", false)]
    public void Redirect_IsAllowedOnlyForApprovedHostsOrLoopback(string uri, bool expected) =>
        Assert.Equal(expected, DynamicClientRegistration.IsAllowedRedirect(uri, Opts));

    [Fact]
    public void ValidRequest_IsAccepted_AndNamesAreCleaned()
    {
        var r = Parse("{\"client_name\":\"  Claude\\u0007 Desktop \",\"redirect_uris\":[\"https://claude.ai/cb\"],\"token_endpoint_auth_method\":\"none\"," +
                      "\"grant_types\":[\"authorization_code\",\"refresh_token\"],\"response_types\":[\"code\"],\"scope\":\"mcp offline_access\"}", out _);

        Assert.NotNull(r);
        Assert.Equal("Claude Desktop", r!.Name);
        Assert.Equal(new[] { "mcp", "offline_access" }, r.Scopes);
    }

    [Fact]
    public void MissingName_GetsADefault_AndLongNamesAreCut()
    {
        Assert.Equal("Unnamed client", Parse("{\"redirect_uris\":[\"https://claude.ai/cb\"]}", out _)!.Name);
        Assert.Equal(100, Parse("{\"client_name\":\"" + new string('a', 500) + "\",\"redirect_uris\":[\"https://claude.ai/cb\"]}", out _)!.Name.Length);
    }

    [Theory]
    [InlineData("{}", "invalid_redirect_uri")]
    [InlineData("{\"redirect_uris\":[]}", "invalid_redirect_uri")]
    [InlineData("{\"redirect_uris\":\"https://claude.ai/cb\"}", "invalid_redirect_uri")]
    [InlineData("{\"redirect_uris\":[1]}", "invalid_redirect_uri")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/1\",\"https://claude.ai/2\",\"https://claude.ai/3\",\"https://claude.ai/4\",\"https://claude.ai/5\",\"https://claude.ai/6\"]}", "invalid_redirect_uri")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/cb\",\"https://evil.example/cb\"]}", "invalid_redirect_uri")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/cb\"],\"token_endpoint_auth_method\":\"client_secret_basic\"}", "invalid_client_metadata")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/cb\"],\"grant_types\":[\"client_credentials\"]}", "invalid_client_metadata")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/cb\"],\"grant_types\":\"authorization_code\"}", "invalid_client_metadata")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/cb\"],\"response_types\":[\"token\"]}", "invalid_client_metadata")]
    [InlineData("{\"redirect_uris\":[\"https://claude.ai/cb\"],\"scope\":\"mcp admin\"}", "invalid_client_metadata")]
    [InlineData("[1,2]", "invalid_client_metadata")]
    public void InvalidRequests_AreRefused_WithTheRightErrorCode(string json, string code)
    {
        Assert.Null(Parse(json, out var error));
        Assert.Equal(code, error.Code);
    }
}

public sealed class AgentTokenTests
{
    private sealed class Bridge : IOAuthIdentityBridge
    {
        public readonly Dictionary<string, (int User, int Company, DateTimeOffset Expires)> Sessions = new();
        public bool RejectRegistration;
        public OAuthWebUser? GetCurrentWebUser() => null;
        public string CreateGrantSession(int userId, int companyId, DateTimeOffset expiresUtc)
        { var id = Guid.NewGuid().ToString(); Sessions[id] = (userId, companyId, expiresUtc); return id; }
        public void DeleteGrantSession(string id) => Sessions.Remove(id);
        public bool IsGrantSessionValid(string id, int userId, int companyId) =>
            Sessions.TryGetValue(id, out var s) && s.User == userId && s.Company == companyId && s.Expires > DateTimeOffset.UtcNow;
        public bool TryRegisterGrantIdentity(string id, int userId, int companyId) => !RejectRegistration && IsGrantSessionValid(id, userId, companyId);
    }

    private static (AgentTokenService Svc, Bridge Bridge) Create(int minutes = 60)
    {
        var bridge = new Bridge();
        return (new AgentTokenService(new EphemeralDataProtectionProvider(), bridge, Options.Create(new OAuthOptions { AgentTokenMinutes = minutes })), bridge);
    }

    [Fact]
    public void MintedToken_AuthenticatesAsTheUser_AndHasThePrefix()
    {
        var (svc, _) = Create();
        var token = svc.Mint(new OAuthWebUser(42, 7, "alice"));

        Assert.StartsWith("aat.", token);
        Assert.True(svc.TryAuthenticate(token, out var user, out var company, out var session));
        Assert.Equal((42, 7), (user, company));
        Assert.False(string.IsNullOrEmpty(session));
    }

    [Fact]
    public void Token_IsUnusableWhenTamperedForeignOrNotAnAgentToken()
    {
        var (svc, _) = Create();
        var token = svc.Mint(new OAuthWebUser(42, 7, "alice"));
        var (other, _) = Create();

        Assert.False(svc.TryAuthenticate(token + "x", out _, out _, out _));
        Assert.False(svc.TryAuthenticate(token[..^3], out _, out _, out _));
        Assert.False(other.TryAuthenticate(token, out _, out _, out _));
        Assert.False(svc.TryAuthenticate(token.Substring(4), out _, out _, out _));   // prefix removed
        Assert.False(svc.TryAuthenticate("aat.", out _, out _, out _));
        Assert.False(svc.TryAuthenticate("some-oauth-token", out _, out _, out _));
    }

    [Fact]
    public void Token_DiesWithItsSession()
    {
        var (svc, bridge) = Create();
        var token = svc.Mint(new OAuthWebUser(42, 7, "alice"));

        bridge.Sessions.Clear();   // sign-out, user deactivated, or the run's session cleaned up
        Assert.False(svc.TryAuthenticate(token, out _, out _, out _));
    }

    [Fact]
    public void Token_IsRefused_WhenTheHostRejectsTheIdentity()
    {
        var (svc, bridge) = Create();
        var token = svc.Mint(new OAuthWebUser(42, 7, "alice"));

        bridge.RejectRegistration = true;
        Assert.False(svc.TryAuthenticate(token, out _, out _, out _));
    }

    [Theory]
    [InlineData(null, 30)]
    [InlineData(1000, 30)]     // asking for more than the configured maximum is capped
    [InlineData(5, 5)]
    [InlineData(0, 1)]         // never below one minute
    public void SessionLifetime_FollowsTheClampedTokenLifetime(int? requested, int expectedMinutes)
    {
        var (svc, bridge) = Create(minutes: 30);
        svc.Mint(new OAuthWebUser(1, 1, "u"), requested);

        var expires = bridge.Sessions.Values.Single().Expires;
        Assert.InRange((expires - DateTimeOffset.UtcNow).TotalMinutes, expectedMinutes, expectedMinutes + 1.1);   // + the one-minute grace
    }
}

public sealed class OAuthJsonTests
{
    [Theory]
    [InlineData("{\"company_id\":123}", 123)]
    [InlineData("{\"company_id\":\"45\"}", 45)]
    [InlineData("{\"other\":1}", null)]
    [InlineData("{\"company_id\":\"abc\"}", null)]
    [InlineData("{\"company_id\":-5}", -5)]
    [InlineData("[1]", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void CompanyId_IsReadFromProperties(string? json, int? expected) =>
        Assert.Equal(expected, OAuthJson.CompanyId(json));
}
