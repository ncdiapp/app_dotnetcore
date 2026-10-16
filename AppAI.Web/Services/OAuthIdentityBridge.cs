using App.BL;
using APP.Components.Dto;
using APP.Framework;
using APP.OAuth;

namespace AppAI.Web.Services;

/// <summary>
/// Connects the OAuth server (APP.OAuth) to AppAI's own login and session code. The signed-in user comes from the
/// identity the session filter registered; grant sessions live in the master-DB session table (AppOAuthGrantSessionBL).
/// </summary>
public sealed class OAuthIdentityBridge : IOAuthIdentityBridge
{
    /// <summary>
    /// True when the current request is a real interactive login. A grant session (Integration type) is what an AI
    /// client ends up acting through, so it must never be able to approve connections or manage clients.
    /// </summary>
    public static bool IsInteractiveSession()
    {
        var sessionId = ServerContext.Instance.CurrentSessionId?.ToString();
        var session = string.IsNullOrWhiteSpace(sessionId) ? null : AppSecurityUserSessionBL.GetSessionEntityBySessionID(sessionId);
        return session != null
            && session.EmExternalSigninType != (int)EmAppExternalLoginType.Integration
            && session.EmExternalSigninType != (int)EmAppExternalLoginType.Anonymous;
    }

    public OAuthWebUser? GetCurrentWebUser()
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity == null || !AppOAuthGrantSessionBL.IsPersonAccount(identity.CurrentLoginUserType)) return null;

        var userId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentUid);
        var companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
        if (userId is not > 0 || companyId is not > 0) return null;   // SysAdmin has no company: it cannot connect AI apps
        if (!IsInteractiveSession()) return null;

        var name = AppOAuthGrantSessionBL.GetLoginName(userId.Value);
        return new OAuthWebUser(userId.Value, companyId.Value, string.IsNullOrWhiteSpace(name) ? "user " + userId : name);
    }

    public string CreateGrantSession(int userId, int companyId, DateTimeOffset expiresUtc) =>
        AppOAuthGrantSessionBL.Create(userId, companyId, expiresUtc.UtcDateTime);

    public void DeleteGrantSession(string grantSessionId) => AppOAuthGrantSessionBL.Delete(grantSessionId);

    public bool IsGrantSessionValid(string grantSessionId, int userId, int companyId) =>
        AppOAuthGrantSessionBL.IsValid(grantSessionId, userId, companyId);

    public bool TryRegisterGrantIdentity(string grantSessionId, int userId, int companyId) =>
        AppOAuthGrantSessionBL.TryRegister(grantSessionId, userId, companyId);
}
