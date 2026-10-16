using McpGateway.Services;
using Microsoft.AspNetCore.Http;

namespace McpGateway.Tests.Services;

public sealed class McpCredentialReaderTests
{
    private static IHeaderDictionary Headers(params (string Name, string Value)[] items)
    {
        var headers = new HeaderDictionary();
        foreach (var (name, value) in items) headers.Append(name, value);
        return headers;
    }

    [Fact]
    public void IntegrationHeader_IsRead()
    {
        var source = McpCredentialReader.TryRead(Headers(("IntergrationAccessToken", "abc")), out var token);

        Assert.Equal(McpCredentialSource.IntegrationHeader, source);
        Assert.Equal("abc", token);
    }

    [Fact]
    public void BearerAuthorization_IsRead()
    {
        var source = McpCredentialReader.TryRead(Headers(("Authorization", "Bearer abc")), out var token);

        Assert.Equal(McpCredentialSource.BearerAuthorization, source);
        Assert.Equal("abc", token);
    }

    [Theory]
    [InlineData("bearer abc")]
    [InlineData("BEARER abc")]
    [InlineData("  Bearer   abc  ")]
    public void BearerScheme_IsCaseInsensitiveAndTrimmed(string value)
    {
        var source = McpCredentialReader.TryRead(Headers(("Authorization", value)), out var token);

        Assert.Equal(McpCredentialSource.BearerAuthorization, source);
        Assert.Equal("abc", token);
    }

    [Fact]
    public void BothHeaders_IntegrationHeaderWins()
    {
        var source = McpCredentialReader.TryRead(
            Headers(("IntergrationAccessToken", "from-integration"), ("Authorization", "Bearer from-bearer")), out var token);

        Assert.Equal(McpCredentialSource.IntegrationHeader, source);
        Assert.Equal("from-integration", token);
    }

    [Fact]
    public void BlankIntegrationHeader_FallsBackToBearer()
    {
        var source = McpCredentialReader.TryRead(
            Headers(("IntergrationAccessToken", "   "), ("Authorization", "Bearer abc")), out var token);

        Assert.Equal(McpCredentialSource.BearerAuthorization, source);
        Assert.Equal("abc", token);
    }

    [Theory]
    [InlineData("Basic YWJjOmRlZg==")]
    [InlineData("Negotiate abc")]
    [InlineData("abc")]
    [InlineData("Bearer")]
    [InlineData("Bearer    ")]
    [InlineData("Bearerabc")]
    public void NonBearerOrEmptyAuthorization_IsNotACredential(string value)
    {
        var source = McpCredentialReader.TryRead(Headers(("Authorization", value)), out var token);

        Assert.Equal(McpCredentialSource.None, source);
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void NoHeaders_ReturnsNone()
    {
        var source = McpCredentialReader.TryRead(Headers(), out var token);

        Assert.Equal(McpCredentialSource.None, source);
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void QueryStringIsNeverRead()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?IntergrationAccessToken=abc&access_token=abc&token=abc");

        var source = McpCredentialReader.TryRead(context.Request.Headers, out var token);

        Assert.Equal(McpCredentialSource.None, source);
        Assert.Equal(string.Empty, token);
    }
}
