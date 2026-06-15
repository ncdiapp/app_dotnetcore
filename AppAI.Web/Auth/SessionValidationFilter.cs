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

        // SECURITY: header-first (cross-domain XHR), then cookie.
        // Query-string tokens are intentionally excluded — they leak via server logs.
        var sessionId = request.Headers[ServerContext.CurrentUserSessionIdToken].FirstOrDefault()
                     ?? request.Cookies[ServerContext.CurrentUserSessionIdToken]
                     ?? string.Empty;

        var anonymousTokens = AppCacheManagerBL.GetAllCompnayAnoymouToken();
        if (anonymousTokens.Contains(sessionId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity(sessionId);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
