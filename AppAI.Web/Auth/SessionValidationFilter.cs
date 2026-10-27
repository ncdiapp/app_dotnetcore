using System;
using System.Linq;
using App.BL;
using APP.Framework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppAI.Web.Auth;

/// <summary>
/// Replaces SecureBaseWebApiController.Initialize() from .NET 4.8.
/// Reads the session token from the request header (preferred) or cookie,
/// rejects anonymous tokens, then registers the caller's identity in ServerContext.
/// Apply via [ServiceFilter(typeof(SessionValidationFilter))] on secure controllers.
/// </summary>
public class SessionValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        // Result iframe loads AgentUiInteractiveHtml?token=… without session headers.
        // Token TTL gate is enforced inside the action — skip session filter here.
        if (IsTokenGatedAgentUiHtml(context))
            return;

        // A blank CurrentUserSessionId header must not replace a session already present on the cookie.
        var headerSessionId = request.Headers[ServerContext.CurrentUserSessionIdToken].FirstOrDefault();
        var sessionId = string.IsNullOrWhiteSpace(headerSessionId)
            ? (request.Cookies[ServerContext.CurrentUserSessionIdToken] ?? string.Empty)
            : headerSessionId;

        var anonymousTokens = AppCacheManagerBL.GetAllCompnayAnoymouToken();
        if (anonymousTokens.Contains(sessionId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity(sessionId);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static bool IsTokenGatedAgentUiHtml(ActionExecutingContext context)
    {
        if (context.ActionDescriptor?.RouteValues == null) return false;
        if (!context.ActionDescriptor.RouteValues.TryGetValue("action", out var action))
            return false;
        if (!string.Equals(action, "AgentUiInteractiveHtml", StringComparison.OrdinalIgnoreCase))
            return false;
        var token = context.HttpContext.Request.Query["token"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(token);
    }
}
