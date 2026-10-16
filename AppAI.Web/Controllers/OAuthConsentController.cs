using App.BL;
using APP.Framework;
using APP.OAuth;
using AppAI.Web.Controllers.Base;
using McpGateway.Models;
using McpGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

/// <summary>
/// The server side of the AppAI "connect an AI application" page (/mcp-authorize) and of a user's own list of
/// connections. Requires a normal signed-in session. Excluded from the API catalogue the MCP gateway can expose.
/// Everything is re-validated here: the page only relays the authorize URL an AI client opened.
/// </summary>
[Route("webapi/[controller]/[action]")]
public class OAuthConsentController : SecureBaseController
{
    private readonly IServiceProvider _services;
    private readonly IOAuthIdentityBridge _bridge;
    private readonly IAuditService _audit;

    public OAuthConsentController(IServiceProvider services, IOAuthIdentityBridge bridge, IAuditService audit)
    {
        _services = services;
        _bridge = bridge;
        _audit = audit;
    }

    public sealed class AuthorizeRequest
    {
        public string AuthorizeUrl { get; set; } = string.Empty;
    }

    /// <summary>What the page shows before the user decides: which application, where it returns, what it gets.</summary>
    [HttpPost]
    public async Task<IActionResult> Describe([FromBody] AuthorizeRequest request, CancellationToken ct)
    {
        var (user, consent, failure) = Resolve();
        if (failure != null) return failure;
        try
        {
            var info = await consent!.DescribeAsync(request.AuthorizeUrl, ct);
            return Ok(new { info.ClientName, info.RedirectHost, info.Scopes, info.Resource, user!.UserName, user.CompanyId });
        }
        catch (OAuthConsentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>The user approves: returns the URL the browser must open next (it carries the consent ticket).</summary>
    [HttpPost]
    public async Task<IActionResult> Approve([FromBody] AuthorizeRequest request, CancellationToken ct)
    {
        var (user, consent, failure) = Resolve();
        if (failure != null) return failure;
        try
        {
            var redirectUrl = await consent!.ApproveAsync(user!, request.AuthorizeUrl, ct);
            Audit("User approved an AI application connection (OAuth consent)", user!);
            return Ok(new { redirectUrl });
        }
        catch (OAuthConsentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>The user declines: returns the URL that sends the AI application back with access_denied.</summary>
    [HttpPost]
    public async Task<IActionResult> Deny([FromBody] AuthorizeRequest request, CancellationToken ct)
    {
        var (user, consent, failure) = Resolve();
        if (failure != null) return failure;
        try
        {
            var redirectUrl = await consent!.DenyAsync(request.AuthorizeUrl, ct);
            Audit("User declined an AI application connection (OAuth consent)", user!);
            return Ok(new { redirectUrl });
        }
        catch (OAuthConsentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>The AI applications the signed-in user has connected.</summary>
    [HttpGet]
    public async Task<IActionResult> MyConnections(CancellationToken ct)
    {
        var (user, admin, failure) = ResolveAdmin();
        if (failure != null) return failure;
        var rows = await admin!.ListAsync(user!.CompanyId, user.UserId, ct);
        return Ok(rows.Select(c => new { c.AuthorizationId, c.ClientName, c.CreatedUtc }));
    }

    /// <summary>The user disconnects one of their own AI applications.</summary>
    [HttpPost]
    public async Task<IActionResult> RevokeMyConnection([FromQuery] string id, CancellationToken ct)
    {
        var (user, admin, failure) = ResolveAdmin();
        if (failure != null) return failure;
        var revoked = await admin!.RevokeAsync(id, user!.CompanyId, user.UserId, ct);
        if (revoked) Audit("User disconnected an AI application", user);
        return Ok(new { revoked });
    }

    private (OAuthWebUser? User, OAuthConsentService? Consent, IActionResult? Failure) Resolve()
    {
        var consent = _services.GetService<OAuthConsentService>();
        if (consent == null) return (null, null, NotFound());   // OAuth is not enabled
        var user = _bridge.GetCurrentWebUser();
        return user == null ? (null, null, StatusCode(StatusCodes.Status403Forbidden, new { error = "Only a signed-in company user can connect an AI application." }))
                            : (user, consent, null);
    }

    private (OAuthWebUser? User, OAuthConnectionAdmin? Admin, IActionResult? Failure) ResolveAdmin()
    {
        var admin = _services.GetService<OAuthConnectionAdmin>();
        if (admin == null) return (null, null, NotFound());
        var user = _bridge.GetCurrentWebUser();
        return user == null ? (null, null, StatusCode(StatusCodes.Status403Forbidden, new { error = "Only a signed-in company user can manage connections." }))
                            : (user, admin, null);
    }

    private void Audit(string action, OAuthWebUser user) =>
        _audit.Log(new AuditEntry
        {
            Code = AuditCode.A006_PrivilegedAction,
            Action = action,
            AppSource = "OAuth",
            ResourcePath = "webapi/OAuthConsent",
            UserId = user.UserId,
            CompanyId = user.CompanyId
        });
}
