using Serilog.Context;

namespace McpGateway.Middleware;

/// <summary>
/// Reads X-Correlation-Id from the incoming request (or generates a new one),
/// pushes it into the Serilog LogContext so every log entry for this request
/// includes a CorrelationId field, and echoes it back in the response header.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
