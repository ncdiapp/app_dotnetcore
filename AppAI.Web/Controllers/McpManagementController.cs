using System.Net;
using App.BL;
using App.BL.TenantBusiness;
using APP.Components.Dto;
using APP.Framework;
using AppAI.Web.Controllers.Base;
using McpGateway.Models;
using McpGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppAI.Web.Controllers;

/// <summary>
/// Company-admin API that controls which APIs external MCP users (Claude Desktop, ChatGPT Desktop, ...) may use.
/// An operation is reachable through MCP only when it is catalogued and enabled here AND granted to one of the
/// caller's security groups (AppSecurityGroup). Tokens themselves are managed by the Integration Tokens screen.
/// </summary>
[Route("webapi/[controller]/[action]")]
public class McpManagementController : SecureBaseController
{
    private readonly ISwaggerService _swagger;
    private readonly IApiAccessPolicy _policy;
    private readonly IAuditService _audit;
    private readonly MultiSourceApiSettings _sources;

    public McpManagementController(
        ISwaggerService swagger,
        IApiAccessPolicy policy,
        IAuditService audit,
        IOptions<MultiSourceApiSettings> sources)
    {
        _swagger = swagger;
        _policy = policy;
        _audit = audit;
        _sources = sources.Value;
    }

    public sealed class SaveExposedApiRequest
    {
        public string AppSource { get; set; }
        public string OperationId { get; set; }
        public bool IsEnabled { get; set; }
        public List<int> GroupIds { get; set; } = new();
    }

    /// <summary>Every operation in the gateway's API index merged with its exposure state.</summary>
    [HttpGet]
    public async Task<object> GetExposableApis(CancellationToken ct)
    {
        RequireCompanyAdmin();

        var catalog = (await McpApiAccessBL.GetExposedApisAsync(ct))
            .ToDictionary(c => c.AppSource + "\n" + c.OperationId, StringComparer.OrdinalIgnoreCase);
        var forwards = _sources.Sources.ToDictionary(s => s.Name, s => s.ForwardCallerToken, StringComparer.OrdinalIgnoreCase);

        var apis = new List<object>();
        foreach (var e in await _swagger.GetAllEndpointsAsync())
        {
            catalog.Remove(e.AppSource + "\n" + e.OperationId, out var state);
            apis.Add(new
            {
                e.AppSource,
                e.OperationId,
                HttpMethod = e.Method,
                ApiPath = e.Path,
                e.Summary,
                e.Tag,
                InSpec = true,
                ForwardsCallerToken = forwards.TryGetValue(e.AppSource, out var f) && f,
                ExposedApiId = state?.ExposedApiId ?? 0,
                IsEnabled = state?.IsEnabled ?? false,
                GroupIds = state?.GroupIds ?? new List<int>()
            });
        }

        // Catalogued earlier but no longer in the spec (renamed/removed): shown so an admin can clean them up.
        foreach (var stale in catalog.Values)
        {
            apis.Add(new
            {
                stale.AppSource,
                stale.OperationId,
                stale.HttpMethod,
                stale.ApiPath,
                stale.Summary,
                Tag = (string)null,
                InSpec = false,
                ForwardsCallerToken = false,
                stale.ExposedApiId,
                stale.IsEnabled,
                stale.GroupIds
            });
        }

        return apis;
    }

    [HttpGet]
    public object GetSecurityGroups()
    {
        RequireCompanyAdmin();

        return AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.SecurityGroup)
            .Select(g => new { GroupId = g.Id, g.GroupName })
            .OrderBy(g => g.GroupName)
            .ToList();
    }

    /// <summary>Enables/disables an operation and replaces the security groups allowed to call it.</summary>
    [HttpPost]
    public async Task<object> SaveExposedApi([FromBody] SaveExposedApiRequest request, CancellationToken ct)
    {
        RequireCompanyAdmin();
        if (request == null) throw new BadHttpRequestException("Request body is required", (int)HttpStatusCode.BadRequest);

        // Take the descriptive fields from the server's own spec: only operations that exist there can be exposed.
        var endpoint = await _swagger.GetEndpointByOperationIdAsync(request.OperationId ?? "");
        if (endpoint == null || !string.Equals(endpoint.AppSource, request.AppSource, StringComparison.OrdinalIgnoreCase))
            throw new BadHttpRequestException("Unknown operation", (int)HttpStatusCode.BadRequest);

        int id;
        try
        {
            id = await McpApiAccessBL.SaveExposedApiAsync(new McpExposedApiDto
            {
                AppSource = endpoint.AppSource,
                OperationId = endpoint.OperationId,
                HttpMethod = endpoint.Method,
                ApiPath = endpoint.Path,
                Summary = endpoint.Summary,
                IsEnabled = request.IsEnabled,
                GroupIds = request.GroupIds ?? new List<int>()
            }, CurrentUserId() ?? 0, ct);
        }
        catch (ArgumentException ex)
        {
            throw new BadHttpRequestException(ex.Message, (int)HttpStatusCode.BadRequest);
        }

        _policy.InvalidateAll();
        AuditAdminChange($"Set MCP exposure of {endpoint.AppSource}/{endpoint.OperationId}: enabled={request.IsEnabled}, groups=[{string.Join(",", request.GroupIds ?? new List<int>())}]",
            endpoint.AppSource, endpoint.OperationId);

        return new { ExposedApiId = id };
    }

    [HttpPost]
    public async Task<object> DeleteExposedApi(int exposedApiId, CancellationToken ct)
    {
        RequireCompanyAdmin();

        await McpApiAccessBL.DeleteExposedApiAsync(exposedApiId, ct);
        _policy.InvalidateAll();
        AuditAdminChange($"Removed MCP exposure entry {exposedApiId}", null, exposedApiId.ToString());

        return new { Deleted = true };
    }

    /// <summary>Preview for an admin: exactly which operations a given user can reach through MCP right now.</summary>
    [HttpGet]
    public async Task<object> GetUserEffectiveApis(int userId, CancellationToken ct)
    {
        RequireCompanyAdmin();

        var allowed = await McpApiAccessBL.GetAllowedForUserAsync(userId, ct);
        return allowed.OrderBy(a => a.AppSource).ThenBy(a => a.OperationId)
            .Select(a => new { a.AppSource, a.OperationId }).ToList();
    }

    /// <summary>Re-reads the API specs so new or changed operations show up in the catalogue.</summary>
    [HttpPost]
    public async Task<object> RefreshApiCatalog()
    {
        RequireCompanyAdmin();

        await _swagger.InvalidateCacheAsync();
        return new { Refreshed = true };
    }

    // Tenant company admins only: the exposure tables live in the tenant DB, which a SysAdmin session has no access to.
    private static void RequireCompanyAdmin()
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity == null)
            throw new BadHttpRequestException("Unauthorized", (int)HttpStatusCode.Unauthorized);
        if (identity.CurrentLoginUserType != (int)EmAppUserType.SaasCompanyAdmin)
            throw new BadHttpRequestException("Forbidden", (int)HttpStatusCode.Forbidden);
    }

    private static int? CurrentUserId() => ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentUid);

    private void AuditAdminChange(string action, string appSource, string resource) =>
        _audit.Log(new AuditEntry
        {
            Code = AuditCode.A006_PrivilegedAction,
            Action = action,
            AppSource = appSource,
            ResourcePath = resource,
            UserId = CurrentUserId(),
            CompanyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId)
        });
}
