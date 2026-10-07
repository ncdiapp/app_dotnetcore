using App.BL;

namespace AppAI.Web.Auth;

/// <summary>
/// Authenticates external MCP clients (Claude Desktop, ChatGPT Desktop, ...) on /mcp.
/// Each external user holds their own Integration token, sent in the IntergrationAccessToken header
/// (spelling is intentional: it matches the PLM / gateway source config).
/// On success the user's identity (user + tenant) is registered for the request and the raw token is kept
/// in HttpContext.Items so the gateway can forward it to downstream APIs on the caller's behalf.
/// </summary>
public class IntegrationTokenMiddleware
{
    public const string HeaderName = "IntergrationAccessToken";
    // Item keys are mirrored as literals in McpGateway.Services.McpCallerContext (cannot reference AppAI.Web).
    public const string ItemKey = "IntegrationAccessToken";
    public const string UserIdItemKey = "IntegrationUserId";
    public const string CompanyIdItemKey = "IntegrationCompanyId";

    private readonly RequestDelegate _next;

    public IntegrationTokenMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // SECURITY: header only. Query-string tokens leak via server logs.
        var token = context.Request.Headers[HeaderName].FirstOrDefault();

        int userId = 0, companyId = 0;
        if (string.IsNullOrWhiteSpace(token)
            || AppCacheManagerBL.GetAllCompnayAnoymouToken().Contains(token)
            || !AppSaasUserSessionMgtBL.TryRegisterIntegrationTokenIdentity(token, out userId, out companyId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"" + HeaderName + " required or invalid\"}");
            return;
        }

        context.Items[ItemKey] = token;
        // Server-derived identity the gateway uses to partition caches and audit; never taken from client headers.
        context.Items[UserIdItemKey] = userId;
        context.Items[CompanyIdItemKey] = companyId;
        await _next(context);
    }
}
