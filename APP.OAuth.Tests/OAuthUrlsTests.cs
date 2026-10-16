using Microsoft.Extensions.Options;

namespace APP.OAuth.Tests;

public sealed class OAuthUrlsTests
{
    private static OAuthOptions Opts(string baseUrl, string mcp = "/mcp") =>
        new() { Enabled = true, PublicBaseUrl = baseUrl, McpPath = mcp };

    [Theory]
    [InlineData("https://mcp.example.com/appai", "https://mcp.example.com/appai")]
    [InlineData("https://mcp.example.com/appai/", "https://mcp.example.com/appai")]
    [InlineData("  https://mcp.example.com/appai/  ", "https://mcp.example.com/appai")]
    [InlineData("https://mcp.example.com", "https://mcp.example.com")]
    [InlineData("https://mcp.example.com/", "https://mcp.example.com")]
    [InlineData("http://localhost:5098/appai", "http://localhost:5098/appai")]
    [InlineData("http://127.0.0.1:52740/appai", "http://127.0.0.1:52740/appai")]
    public void Issuer_HasNoTrailingSlash(string input, string expected) =>
        Assert.Equal(expected, new OAuthUrls(Opts(input)).Issuer);

    [Fact]
    public void Urls_AreBuiltFromTheIssuer()
    {
        var u = new OAuthUrls(Opts("https://mcp.example.com/appai"));

        Assert.Equal("https://mcp.example.com/appai/mcp", u.Resource);
        Assert.Equal("https://mcp.example.com/appai/.well-known/oauth-protected-resource", u.ProtectedResourceMetadata);
        Assert.Equal("https://mcp.example.com/appai/oauth/authorize", u.AuthorizationEndpoint);
        Assert.Equal("https://mcp.example.com/appai/oauth/token", u.TokenEndpoint);
        Assert.Equal("https://mcp.example.com/appai/oauth/revoke", u.RevocationEndpoint);
        Assert.Equal("https://mcp.example.com/appai/oauth/register", u.RegistrationEndpoint);
        Assert.Equal("/appai", u.IssuerPath);
    }

    [Theory]
    [InlineData("mcp")]
    [InlineData("/mcp")]
    [InlineData("/mcp/")]
    public void McpPath_IsNormalised(string mcpPath) =>
        Assert.Equal("https://h.example.com/appai/mcp", new OAuthUrls(Opts("https://h.example.com/appai", mcpPath)).Resource);

    [Fact]
    public void ConfigurationPaths_IncludeIssuerPathForms_WhenAppIsUnderAPath()
    {
        var paths = new OAuthUrls(Opts("https://h.example.com/appai")).ConfigurationPaths();

        Assert.Equal(new[]
        {
            "/.well-known/openid-configuration",
            "/.well-known/oauth-authorization-server",
            "/.well-known/openid-configuration/appai",
            "/.well-known/oauth-authorization-server/appai"
        }, paths);
    }

    [Fact]
    public void ConfigurationPaths_AreJustThePlainForms_WhenAppIsAtTheRoot() =>
        Assert.Equal(2, new OAuthUrls(Opts("https://h.example.com")).ConfigurationPaths().Count);

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("mcp", true)]
    [InlineData("/mcp/", true)]
    [InlineData("appai/mcp", true)]
    [InlineData("APPAI/MCP", true)]
    [InlineData("other", false)]
    [InlineData("appai/other", false)]
    [InlineData("mcp/extra", false)]
    public void IsResourcePath_OnlyAcceptsTheMcpResource(string? path, bool expected) =>
        Assert.Equal(expected, new OAuthUrls(Opts("https://h.example.com/appai")).IsResourcePath(path));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("/appai")]
    [InlineData("http://mcp.example.com/appai")]          // plain http only for localhost
    [InlineData("ftp://mcp.example.com/appai")]
    [InlineData("https://mcp.example.com/appai?x=1")]
    [InlineData("https://mcp.example.com/appai#frag")]
    public void InvalidPublicBaseUrl_IsRejected(string baseUrl) =>
        Assert.Throws<InvalidOperationException>(() => new OAuthUrls(Opts(baseUrl)));

    // ── challenge header ────────────────────────────────────────────────────────

    private static OAuthChallenge Challenge(bool enabled) => new(Options.Create(new OAuthOptions
    {
        Enabled = enabled, PublicBaseUrl = enabled ? "https://mcp.example.com/appai" : "", Scope = "mcp"
    }));

    [Fact]
    public void Challenge_WhenDisabled_IsThePlainBearerChallenge()
    {
        Assert.Equal("Bearer", Challenge(false).Header(credentialRejected: false));
        Assert.Equal("Bearer error=\"invalid_token\"", Challenge(false).Header(credentialRejected: true));
    }

    [Fact]
    public void Challenge_WhenEnabled_PointsClientsAtTheResourceMetadata()
    {
        const string pointer = "resource_metadata=\"https://mcp.example.com/appai/.well-known/oauth-protected-resource\"";

        Assert.Equal("Bearer " + pointer + ", scope=\"mcp\"", Challenge(true).Header(credentialRejected: false));
        Assert.Equal("Bearer error=\"invalid_token\", " + pointer + ", scope=\"mcp\"", Challenge(true).Header(credentialRejected: true));
    }

    [Fact]
    public void Challenge_Enabled_WithoutPublicBaseUrl_FailsAtStartup() =>
        Assert.Throws<InvalidOperationException>(() => new OAuthChallenge(Options.Create(new OAuthOptions { Enabled = true })));
}
