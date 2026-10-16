using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APP.OAuth;

/// <summary>
/// Authenticates the bearer credential of a /mcp request when it is not an Integration token: an OAuth access token
/// issued by this server (checked by OpenIddict for signature/reference, expiry and audience, then for the mcp scope)
/// or an in-app agent run token. Success registers the user's identity for the request. A failure is just "no".
/// </summary>
public sealed class OAuthBearerAuthenticator
{
    private readonly AgentTokenService _agentTokens;
    private readonly IOAuthIdentityBridge _bridge;
    private readonly OAuthOptions _options;
    private readonly ILogger<OAuthBearerAuthenticator> _log;

    public OAuthBearerAuthenticator(AgentTokenService agentTokens, IOAuthIdentityBridge bridge,
        IOptions<OAuthOptions> options, ILogger<OAuthBearerAuthenticator> log)
    {
        _agentTokens = agentTokens;
        _bridge = bridge;
        _options = options.Value;
        _log = log;
    }

    /// <param name="onAuthenticated">Receives the user, company and the grant session id (what downstream AppAI calls carry as the user's session).</param>
    public async Task<bool> TryAuthenticateAsync(HttpContext http, string bearerToken, Action<int, int, string> onAuthenticated)
    {
        if (bearerToken.StartsWith(OAuthClaims.AgentTokenPrefix, StringComparison.Ordinal))
        {
            if (!_agentTokens.TryAuthenticate(bearerToken, out var agentUser, out var agentCompany, out var agentSession)) return false;
            onAuthenticated(agentUser, agentCompany, agentSession);
            return true;
        }

        // OpenIddict reads the same Authorization: Bearer header; it rejects unknown, expired, revoked and
        // wrong-audience tokens.
        var result = await http.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme).ConfigureAwait(false);
        var principal = result.Principal;
        if (!result.Succeeded || principal is null) return false;

        if (!principal.HasScope(_options.Scope)
            || !int.TryParse(principal.GetClaim(Claims.Subject), NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            || !int.TryParse(principal.GetClaim(OAuthClaims.CompanyId), NumberStyles.None, CultureInfo.InvariantCulture, out var companyId)
            || string.IsNullOrEmpty(principal.GetClaim(OAuthClaims.GrantSession)))
        {
            _log.LogWarning("OAuth access token refused: missing scope or identity claims");
            return false;
        }

        if (!_bridge.TryRegisterGrantIdentity(principal.GetClaim(OAuthClaims.GrantSession)!, userId, companyId))
        {
            _log.LogWarning("OAuth access token refused: grant session ended or user inactive (user {UserId}, company {CompanyId})", userId, companyId);
            return false;
        }

        onAuthenticated(userId, companyId, principal.GetClaim(OAuthClaims.GrantSession)!);
        return true;
    }
}
