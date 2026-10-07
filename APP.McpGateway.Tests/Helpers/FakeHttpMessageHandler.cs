using System.Net;
using System.Text;

namespace McpGateway.Tests.Helpers;

/// <summary>
/// In-process HTTP handler that intercepts requests without real network I/O.
/// Accepts either a synchronous or asynchronous delegate.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _factory;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) =>
        _factory = req => Task.FromResult(factory(req));

    public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) =>
        _factory = factory;

    /// <summary>Convenience factory: returns a JSON 200 (or given status) for every request.</summary>
    public static FakeHttpMessageHandler ReturnsJson(
        string json,
        HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        _factory(request);
}
