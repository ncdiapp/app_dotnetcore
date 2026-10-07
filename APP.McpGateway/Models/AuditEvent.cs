namespace McpGateway.Models;

public enum AuditCode
{
    // ── Implemented ──────────────────────────────────────────────────────────
    A001_DataChange          = 1,
    A002_DataAccess          = 2,
    A003_AccountCreation     = 3,
    A006_PrivilegedAction    = 6,
    A007_DataDeletion        = 7,
    A012_DataStructureChange = 12,
    A017_SessionTimeout      = 17,
    A018_Logon               = 18,
    A019_Logoff              = 19,
    A021_ApiCrudAction       = 21,
    A022_AccessDenied        = 22,

    // ── Future (requires RBAC / user-management infrastructure) ─────────────
    A004_AccountModification          = 4,
    A005_AccountDeactivation          = 5,
    A008_PrivilegeAddedToRole         = 8,
    A009_PrivilegeRemovedFromRole     = 9,
    A010_RoleDeletion                 = 10,
    A011_RoleCreation                 = 11,
    A013_DataStructureCreation        = 13,
    A014_DataStructurePrivilegeChange = 14,
    A015_RoleAddedToUser              = 15,
    A016_RoleRemovedFromUser          = 16,
    A020_AccountEnabled               = 20,
}

public record AuditEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public required AuditCode Code  { get; init; }
    public required string Action   { get; init; }
    public bool Success             { get; init; } = true;
    /// <summary>Authenticated caller, filled in by the audit service from the validated token (not client-controlled).</summary>
    public int?    CompanyId        { get; init; }
    public int?    UserId           { get; init; }
    /// <summary>Client-supplied Mcp-Session-Id; informational only, never an identity.</summary>
    public string? SessionId        { get; init; }
    public string? IpAddress        { get; init; }
    public string? CorrelationId    { get; init; }
    public string? AppSource        { get; init; }
    public string? HttpMethod       { get; init; }
    public string? ResourcePath     { get; init; }
    public int?    HttpStatus       { get; init; }
    public string? ErrorMessage     { get; init; }
    public Dictionary<string, object?>? AdditionalContext { get; init; }
}
