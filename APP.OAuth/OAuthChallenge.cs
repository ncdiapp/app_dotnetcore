using Microsoft.Extensions.Options;

namespace APP.OAuth;

/// <summary>
/// Builds the WWW-Authenticate challenge for a rejected /mcp request. With OAuth enabled it carries the
/// resource_metadata pointer Claude and ChatGPT need to start sign-in (they ignore a challenge without it);
/// without OAuth it is the plain Bearer challenge.
/// </summary>
public sealed class OAuthChallenge
{
    private readonly OAuthUrls? _urls;
    private readonly string _scope;

    public OAuthChallenge(IOptions<OAuthOptions> options)
    {
        var o = options.Value;
        _scope = o.Scope;
        _urls = o.Enabled ? new OAuthUrls(o) : null;
    }

    /// <param name="credentialRejected">True when a token was sent and refused (RFC 6750 error="invalid_token").</param>
    public string Header(bool credentialRejected)
    {
        var parts = new List<string>();
        if (credentialRejected) parts.Add("error=\"invalid_token\"");
        if (_urls != null)
        {
            parts.Add("resource_metadata=\"" + _urls.ProtectedResourceMetadata + "\"");
            parts.Add("scope=\"" + _scope + "\"");
        }
        return parts.Count == 0 ? "Bearer" : "Bearer " + string.Join(", ", parts);
    }
}
