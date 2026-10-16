using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace APP.OAuth;

/// <summary>
/// Short-lived token an in-app agent run uses to call the MCP gateway as the user who started the run, instead of a
/// shared static credential (which would run every user's agent with one identity). Stateless (Data Protection, so
/// every node must share the key ring); expires with the run; backed by a grant session that ends with it.
/// </summary>
public sealed class AgentTokenService
{
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IOAuthIdentityBridge _bridge;
    private readonly int _maxMinutes;

    public AgentTokenService(IDataProtectionProvider provider, IOAuthIdentityBridge bridge, IOptions<OAuthOptions> options)
    {
        _protector = provider.CreateProtector("AppAI.OAuth.AgentToken.v1").ToTimeLimitedDataProtector();
        _bridge = bridge;
        _maxMinutes = Math.Clamp(options.Value.AgentTokenMinutes, 1, 240);
    }

    private sealed record Payload(int UserId, int CompanyId, string GrantSession);

    /// <summary>Mints a token for <paramref name="user"/> that works for at most <paramref name="minutes"/> (capped by configuration).</summary>
    public string Mint(OAuthWebUser user, int? minutes = null)
    {
        var lifetime = TimeSpan.FromMinutes(Math.Clamp(minutes ?? _maxMinutes, 1, _maxMinutes));
        // The session outlives the token by a minute so a request in flight at expiry still resolves cleanly.
        var session = _bridge.CreateGrantSession(user.UserId, user.CompanyId, DateTimeOffset.UtcNow.Add(lifetime).AddMinutes(1));
        var payload = JsonSerializer.Serialize(new Payload(user.UserId, user.CompanyId, session));
        return OAuthClaims.AgentTokenPrefix + _protector.Protect(payload, lifetime);
    }

    /// <summary>Validates the token and registers the user's identity for the current request.</summary>
    public bool TryAuthenticate(string token, out int userId, out int companyId, out string grantSession)
    {
        userId = companyId = 0;
        grantSession = string.Empty;
        if (!token.StartsWith(OAuthClaims.AgentTokenPrefix, StringComparison.Ordinal)) return false;
        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(token.Substring(OAuthClaims.AgentTokenPrefix.Length)));
            if (payload is null || payload.UserId <= 0 || payload.CompanyId <= 0 || string.IsNullOrEmpty(payload.GrantSession)) return false;
            if (!_bridge.TryRegisterGrantIdentity(payload.GrantSession, payload.UserId, payload.CompanyId)) return false;
            userId = payload.UserId;
            companyId = payload.CompanyId;
            grantSession = payload.GrantSession;
            return true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;   // expired, forged or protected with another key
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
