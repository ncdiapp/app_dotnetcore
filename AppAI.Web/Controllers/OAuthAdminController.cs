using App.BL;
using APP.Components.Dto;
using APP.Framework;
using APP.OAuth;
using AppAI.Web.Controllers.Base;
using AppAI.Web.Services;
using McpGateway.Models;
using McpGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

/// <summary>
/// Administration of AI-application connections. A company admin sees and ends the connections of their own company's
/// users; the host SysAdmin manages the OAuth clients (which AI applications may ask users to sign in). Requires an
/// interactive login: a grant session (what an AI client acts through) is refused. Excluded from the MCP catalogue.
/// </summary>
[Route("webapi/[controller]/[action]")]
public class OAuthAdminController : SecureBaseController
{
    private readonly IServiceProvider _services;
    private readonly IAuditService _audit;

    public OAuthAdminController(IServiceProvider services, IAuditService audit)
    {
        _services = services;
        _audit = audit;
    }

    public sealed class CreateClientRequest
    {
        public string Name { get; set; } = string.Empty;
        public List<string> RedirectUris { get; set; } = new();
    }

    // ── company admin: connections of this company ─────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Connections(CancellationToken ct)
    {
        var admin = _services.GetService<OAuthConnectionAdmin>();
        if (admin == null) return NotFound();
        if (!TryCompanyAdmin(out var companyId, out var denied)) return denied!;

        var rows = await admin.ListAsync(companyId, null, ct);
        return Ok(rows.Select(c => new
        {
            c.AuthorizationId, c.ClientName, c.UserId, UserName = UserName(c.UserId), c.CreatedUtc
        }));
    }

    [HttpPost]
    public async Task<IActionResult> RevokeConnection([FromQuery] string id, CancellationToken ct)
    {
        var admin = _services.GetService<OAuthConnectionAdmin>();
        if (admin == null) return NotFound();
        if (!TryCompanyAdmin(out var companyId, out var denied)) return denied!;

        var revoked = await admin.RevokeAsync(id, companyId, null, ct);
        if (revoked) Audit("Admin ended an AI application connection", companyId);
        return Ok(new { revoked });
    }

    // ── host SysAdmin: OAuth clients ───────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Clients(CancellationToken ct)
    {
        var clients = _services.GetService<OAuthClientAdmin>();
        if (clients == null) return NotFound();
        if (!IsSysAdmin(out var denied)) return denied!;
        return Ok(await clients.ListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request, CancellationToken ct)
    {
        var clients = _services.GetService<OAuthClientAdmin>();
        if (clients == null) return NotFound();
        if (!IsSysAdmin(out var denied)) return denied!;
        try
        {
            var clientId = await clients.CreateAsync(request.Name, request.RedirectUris, ct);
            Audit("SysAdmin registered an OAuth client", null);
            return Ok(new { clientId });
        }
        catch (OAuthConsentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteClient([FromQuery] string clientId, CancellationToken ct)
    {
        var clients = _services.GetService<OAuthClientAdmin>();
        if (clients == null) return NotFound();
        if (!IsSysAdmin(out var denied)) return denied!;
        var deleted = await clients.DeleteAsync(clientId, ct);
        if (deleted) Audit("SysAdmin deleted an OAuth client", null);
        return Ok(new { deleted });
    }

    // ── guards ─────────────────────────────────────────────────────────────────

    private bool TryCompanyAdmin(out int companyId, out IActionResult? denied)
    {
        companyId = 0;
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        var company = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
        if (identity == null || identity.CurrentLoginUserType != (int)EmAppUserType.SaasCompanyAdmin || company is not > 0
            || !OAuthIdentityBridge.IsInteractiveSession())
        {
            denied = StatusCode(StatusCodes.Status403Forbidden, new { error = "Company administrators only." });
            return false;
        }
        companyId = company.Value;
        denied = null;
        return true;
    }

    private bool IsSysAdmin(out IActionResult? denied)
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity == null || identity.CurrentLoginUserType != (int)EmAppUserType.SysAdmin || !OAuthIdentityBridge.IsInteractiveSession())
        {
            denied = StatusCode(StatusCodes.Status403Forbidden, new { error = "System administrators only." });
            return false;
        }
        denied = null;
        return true;
    }

    private static string UserName(int userId) =>
        AppOAuthGrantSessionBL.GetLoginName(userId) ?? ("user " + userId);

    private void Audit(string action, int? companyId) =>
        _audit.Log(new AuditEntry
        {
            Code = AuditCode.A006_PrivilegedAction,
            Action = action,
            AppSource = "OAuth",
            ResourcePath = "webapi/OAuthAdmin",
            UserId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentUid),
            CompanyId = companyId
        });
}
