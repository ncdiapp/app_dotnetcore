namespace McpGateway.Services;

/// <summary>
/// Identity of the external user behind the current /mcp request. Populated by AppAI.Web's
/// IntegrationTokenMiddleware after the IntergrationAccessToken has been validated; nothing here is
/// read from client-controlled headers such as Mcp-Session-Id.
/// </summary>
public interface IMcpCallerContext
{
    /// <summary>The validated IntergrationAccessToken, or null outside an authenticated /mcp request.</summary>
    string? Token { get; }

    /// <summary>Tenant and user of the caller; false when there is no authenticated caller.</summary>
    bool TryGetIdentity(out int companyId, out int userId);

    /// <summary>Same as TryGetIdentity but throws, so cache and audit code can never fall back to a shared bucket.</summary>
    (int CompanyId, int UserId) RequireIdentity();
}

public sealed class McpCallerContext : IMcpCallerContext
{
    // Keys must match AppAI.Web.Auth.IntegrationTokenMiddleware (AppAI.Web cannot be referenced from here).
    private const string TokenKey = "IntegrationAccessToken";
    private const string UserIdKey = "IntegrationUserId";
    private const string CompanyIdKey = "IntegrationCompanyId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public McpCallerContext(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string? Token => _httpContextAccessor.HttpContext?.Items[TokenKey] as string;

    public bool TryGetIdentity(out int companyId, out int userId)
    {
        var items = _httpContextAccessor.HttpContext?.Items;
        if (items?[CompanyIdKey] is int c && items[UserIdKey] is int u && c > 0 && u > 0)
        {
            companyId = c;
            userId = u;
            return true;
        }

        companyId = 0;
        userId = 0;
        return false;
    }

    public (int CompanyId, int UserId) RequireIdentity() =>
        TryGetIdentity(out var companyId, out var userId)
            ? (companyId, userId)
            : throw new UnauthorizedAccessException("No authenticated MCP caller for this request.");
}
