using App.BL.TenantBusiness;
using McpGateway.Services;

namespace AppAI.Web.Services;

/// <summary>
/// Supplies the gateway's access policy with the APIs the calling user may use: catalogued + enabled in
/// dbo.AppMcpExposedApi and granted to one of the user's security groups (AppSecurityGroupMember).
/// Must be called while the caller's identity is registered (inside an authenticated /mcp request).
/// </summary>
public sealed class McpApiAccessProvider : IApiAccessProvider
{
    public async Task<IReadOnlyCollection<(string AppSource, string OperationId)>> LoadAllowedAsync(
        int companyId, int userId, CancellationToken cancellationToken)
    {
        var allowed = await McpApiAccessBL.GetAllowedForUserAsync(userId, cancellationToken);
        return allowed.Select(a => (a.AppSource, a.OperationId)).ToList();
    }
}
