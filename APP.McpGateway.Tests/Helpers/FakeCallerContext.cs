using McpGateway.Services;

namespace McpGateway.Tests.Helpers;

/// <summary>Mutable stand-in for the authenticated /mcp caller; Authenticated=false models "no caller".</summary>
internal sealed class FakeCallerContext : IMcpCallerContext
{
    public string? Token { get; set; }
    public int CompanyId { get; set; }
    public int UserId { get; set; }

    public static FakeCallerContext For(int companyId, int userId, string? token = "caller-token") =>
        new() { CompanyId = companyId, UserId = userId, Token = token };

    public static FakeCallerContext Anonymous() => new();

    public bool TryGetIdentity(out int companyId, out int userId)
    {
        companyId = CompanyId;
        userId = UserId;
        return CompanyId > 0 && UserId > 0;
    }

    public (int CompanyId, int UserId) RequireIdentity() =>
        TryGetIdentity(out var c, out var u) ? (c, u) : throw new UnauthorizedAccessException();
}
