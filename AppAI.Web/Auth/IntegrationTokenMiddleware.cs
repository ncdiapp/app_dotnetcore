using App.BL;
using APP.OAuth;
using McpGateway.Services;

namespace AppAI.Web.Auth;

/// <summary>
/// Authenticates external MCP clients (Claude Desktop, ChatGPT Desktop, in-house agents, ...) on /mcp. Three credentials:
///  - an Integration token (a service account's session id) in the IntergrationAccessToken header or as
///    "Authorization: Bearer" (see McpCredentialReader);
///  - an OAuth access token issued by this server to a signed-in user ("Authorization: Bearer", OAuth enabled);
///  - a short-lived agent run token minted for a signed-in user's agent run ("Authorization: Bearer aat....").
/// On success the user's identity (user + tenant) is registered for the request. HttpContext.Items carries the id the
/// gateway forwards to this instance's own API as the caller's session: the Integration token itself, or the grant
/// session behind an OAuth or agent token (an OAuth token means nothing to /webapi). A rejected request gets a 401
/// whose WWW-Authenticate points OAuth clients at the resource metadata when OAuth is enabled (see OAuthChallenge).
/// </summary>
public class IntegrationTokenMiddleware
{
    public const string HeaderName = McpCredentialReader.IntegrationHeaderName;
    // Item keys are mirrored as literals in McpGateway.Services.McpCallerContext (cannot reference AppAI.Web).
    public const string ItemKey = "IntegrationAccessToken";
    public const string UserIdItemKey = "IntegrationUserId";
    public const string CompanyIdItemKey = "IntegrationCompanyId";

    private readonly RequestDelegate _next;

    public IntegrationTokenMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, OAuthChallenge challenge, IServiceProvider services,
        ILogger<IntegrationTokenMiddleware> log)
    {
        // SECURITY: headers only. Query-string tokens leak via server logs.
        var source = McpCredentialReader.TryRead(context.Request.Headers, out var token);

        int userId = 0, companyId = 0;
        var forwardedId = token;
        var authenticated = false;

        // Integration session ids are GUIDs; OAuth reference tokens and agent tokens never are, so a bearer that is a
        // GUID goes straight to the Integration check without an OAuth token lookup.
        if (source == McpCredentialSource.BearerAuthorization && !Guid.TryParse(token, out _)
            && services.GetService<OAuthBearerAuthenticator>() is { } oauth)
        {
            authenticated = await oauth.TryAuthenticateAsync(context, token, (user, company, session) =>
            {
                userId = user;
                companyId = company;
                forwardedId = session;
            });
        }
        else if (source != McpCredentialSource.None
                 && !AppCacheManagerBL.GetAllCompnayAnoymouToken().Contains(token)
                 && AppSaasUserSessionMgtBL.TryRegisterIntegrationTokenIdentity(token, out userId, out companyId))
        {
            authenticated = true;
        }

        if (!authenticated)
        {
            // Never log the credential itself.
            log.LogWarning("MCP request rejected: credential {Credential} from {Remote}", source, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            // RFC 6750: a request with no credential gets a bare challenge, a rejected one gets invalid_token.
            context.Response.Headers.WWWAuthenticate = challenge.Header(credentialRejected: source != McpCredentialSource.None);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"" + HeaderName + " or Authorization: Bearer token required or invalid\"}");
            return;
        }

        context.Items[ItemKey] = forwardedId;
        // Server-derived identity the gateway uses to partition caches and audit; never taken from client headers.
        context.Items[UserIdItemKey] = userId;
        context.Items[CompanyIdItemKey] = companyId;
        await _next(context);
    }
}
