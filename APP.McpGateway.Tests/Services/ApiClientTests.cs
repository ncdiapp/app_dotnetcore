using System.Net;
using McpGateway.Models;
using McpGateway.Services;
using McpGateway.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpGateway.Tests.Services;

public class ApiClientTests
{
    private const string HeaderName = "IntergrationAccessToken";

    // ── Factory helpers ───────────────────────────────────────────────────────

    private static ApiClient CreateClient(
        List<ApiSourceConfig> sources,
        HttpMessageHandler? handler = null,
        IMcpCallerContext? caller = null)
    {
        var settings = Options.Create(new MultiSourceApiSettings { Sources = sources });

        var factory = new Mock<IHttpClientFactory>();
        if (handler != null)
            factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                   .Returns(() => new HttpClient(handler));

        return new ApiClient(
            settings,
            factory.Object,
            caller ?? FakeCallerContext.For(1, 10),
            NullLogger<ApiClient>.Instance,
            NullAuditService.Instance);
    }

    private static ApiSourceConfig Source(bool forward = false, string? staticToken = null) =>
        new()
        {
            Name = "PLM",
            BaseUrl = "https://api.example.com",
            AccessTokenValue = staticToken,
            AccessTokenHeaderName = HeaderName,
            ForwardCallerToken = forward
        };

    private static FakeHttpMessageHandler CaptureHeader(Action<string?> capture) =>
        new(req =>
        {
            capture(req.Headers.TryGetValues(HeaderName, out var vals) ? vals.First() : null);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

    // ── Tests: source resolution ──────────────────────────────────────────────

    [Fact]
    public async Task Unknown_source_returns_400_with_descriptive_error()
    {
        var client = CreateClient([]);

        var result = await client.ExecuteAsync("UNKNOWN", "GET", "/api/test");

        Assert.Equal(400, result.StatusCode);
        Assert.False(result.IsSuccess);
        Assert.Contains("UNKNOWN", result.ErrorMessage);
    }

    [Fact]
    public async Task Unknown_source_error_lists_configured_sources()
    {
        var client = CreateClient([Source()]);

        var result = await client.ExecuteAsync("ERP", "GET", "/api/test");

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("PLM", result.ErrorMessage);
    }

    // ── Tests: caller token forwarding ────────────────────────────────────────

    [Fact]
    public async Task Forwarding_source_sends_the_callers_token_in_the_configured_header()
    {
        string? captured = null;
        var client = CreateClient([Source(forward: true)], CaptureHeader(t => captured = t),
            FakeCallerContext.For(1, 10, "user-10-token"));

        var result = await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.True(result.IsSuccess);
        Assert.Equal("user-10-token", captured);
    }

    [Fact]
    public async Task Two_callers_each_send_only_their_own_token()
    {
        var seen = new List<string?>();
        var handler = CaptureHeader(seen.Add);
        var caller = FakeCallerContext.For(1, 10, "token-A");
        var client = CreateClient([Source(forward: true)], handler, caller);

        await client.ExecuteAsync("PLM", "GET", "/api/items");
        caller.UserId = 11;
        caller.Token = "token-B";
        await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.Equal(["token-A", "token-B"], seen);
    }

    [Fact]
    public async Task Forwarding_source_without_a_caller_token_returns_401_and_makes_no_call()
    {
        var called = false;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            called = true;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = CreateClient([Source(forward: true)], handler, FakeCallerContext.Anonymous());

        var result = await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.Equal(401, result.StatusCode);
        Assert.False(result.IsSuccess);
        Assert.False(called);
    }

    [Fact]
    public async Task Non_forwarding_source_never_receives_the_callers_token()
    {
        string? captured = "unset";
        var client = CreateClient([Source(forward: false)], CaptureHeader(t => captured = t),
            FakeCallerContext.For(1, 10, "user-10-token"));

        await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.Null(captured);
    }

    [Fact]
    public async Task Static_access_token_is_not_sent_on_api_calls()
    {
        string? captured = "unset";
        var client = CreateClient([Source(forward: false, staticToken: "shared-service-token")],
            CaptureHeader(t => captured = t));

        await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.Null(captured);
    }

    // ── Tests: URL building ───────────────────────────────────────────────────

    [Fact]
    public async Task Request_url_is_base_url_plus_path()
    {
        Uri? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var client = CreateClient([Source()], handler);
        await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.Equal("https://api.example.com/api/items", captured!.ToString());
    }

    [Fact]
    public async Task Query_params_are_appended_and_encoded()
    {
        Uri? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var client = CreateClient([Source()], handler);
        await client.ExecuteAsync("PLM", "GET", "/api/items",
            queryParams: new Dictionary<string, string>
            {
                ["id"] = "42",
                ["q"] = "hello world"
            });

        Assert.NotNull(captured);
        Assert.StartsWith("https://api.example.com/api/items?", captured!.ToString());
        Assert.Contains("id=42", captured.Query);
        Assert.Contains("hello%20world", captured.Query);
    }

    [Fact]
    public async Task Base_url_trailing_slash_is_normalised()
    {
        Uri? captured = null;
        var handler = new FakeHttpMessageHandler(req =>
        {
            captured = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var source = Source();
        source.BaseUrl = "https://api.example.com/";
        var client = CreateClient([source], handler);
        await client.ExecuteAsync("PLM", "GET", "/api/items");

        Assert.Equal("https://api.example.com/api/items", captured!.ToString());
    }

    // ── Tests: response handling ──────────────────────────────────────────────

    [Fact]
    public async Task Successful_response_body_is_returned()
    {
        var handler = FakeHttpMessageHandler.ReturnsJson("{\"result\":\"ok\"}");
        var client = CreateClient([Source()], handler);

        var result = await client.ExecuteAsync("PLM", "GET", "/api/test");

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("ok", result.Body);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Non_success_response_returns_error_message()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Internal Server Error",
                Content = new StringContent("{}")
            });

        var client = CreateClient([Source()], handler);
        var result = await client.ExecuteAsync("PLM", "GET", "/api/test");

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.StatusCode);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task Backend_401_is_passed_through()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}") });
        var client = CreateClient([Source(forward: true)], handler);

        var result = await client.ExecuteAsync("PLM", "GET", "/api/test");

        Assert.Equal(401, result.StatusCode);
        Assert.False(result.IsSuccess);
    }

    // ── Tests: request body ───────────────────────────────────────────────────

    [Fact]
    public async Task Request_body_is_sent_as_json()
    {
        string? capturedBody = null;
        string? capturedContentType = null;
        var handler = new FakeHttpMessageHandler(async (HttpRequestMessage req) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            capturedContentType = req.Content.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var client = CreateClient([Source()], handler);
        await client.ExecuteAsync("PLM", "POST", "/api/orders", body: "{\"name\":\"test\"}");

        Assert.Equal("{\"name\":\"test\"}", capturedBody);
        Assert.Equal("application/json", capturedContentType);
    }
}
