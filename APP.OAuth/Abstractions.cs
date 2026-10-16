namespace APP.OAuth;

/// <summary>A signed-in AppAI web user who may grant an AI client access.</summary>
public sealed record OAuthWebUser(int UserId, int CompanyId, string UserName);

/// <summary>
/// What the OAuth server needs from AppAI's own login/session code. Implemented by the host (AppAI.Web) on top of the
/// master-DB session table, so this library never references the business layer. Every method is synchronous because
/// the session code is.
/// </summary>
public interface IOAuthIdentityBridge
{
    /// <summary>
    /// The signed-in user of the current request (the session filter has registered the identity), or null when the
    /// caller is not signed in or may not use OAuth (SysAdmin, service or anonymous accounts, no company).
    /// </summary>
    OAuthWebUser? GetCurrentWebUser();

    /// <summary>
    /// Creates a session row that represents one OAuth grant (or one agent run) and returns its id. It never touches
    /// the user's other sessions and its expiry is not slid by normal request activity.
    /// </summary>
    string CreateGrantSession(int userId, int companyId, DateTimeOffset expiresUtc);

    void DeleteGrantSession(string grantSessionId);

    /// <summary>True when the grant session exists, is unexpired, belongs to that user and company, and the user is still active.</summary>
    bool IsGrantSessionValid(string grantSessionId, int userId, int companyId);

    /// <summary>Same checks as <see cref="IsGrantSessionValid"/>, then registers the user's identity for the current request.</summary>
    bool TryRegisterGrantIdentity(string grantSessionId, int userId, int companyId);
}

/// <summary>The caller of an OAuth consent API sent something invalid (shown to the user as a message).</summary>
public sealed class OAuthConsentException : Exception
{
    public OAuthConsentException(string message) : base(message) { }
}

/// <summary>Custom claim and property names shared by the authorize, token and validation code.</summary>
public static class OAuthClaims
{
    public const string CompanyId = "company_id";
    public const string GrantSession = "grant_session";

    /// <summary>Prefix of the stateless short-lived token an in-app agent run uses (not an OpenIddict token).</summary>
    public const string AgentTokenPrefix = "aat.";
}
