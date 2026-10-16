namespace APP.OAuth;

/// <summary>Every public URL of the OAuth server, built once from <see cref="OAuthOptions.PublicBaseUrl"/>.</summary>
public sealed class OAuthUrls
{
    public const string ProtectedResourceDocumentPath = "/.well-known/oauth-protected-resource";
    public const string OpenIdConfigurationPath = "/.well-known/openid-configuration";
    public const string AuthorizationServerPath = "/.well-known/oauth-authorization-server";

    public OAuthUrls(OAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
            throw new InvalidOperationException("OAuth:PublicBaseUrl is required when OAuth is enabled (e.g. https://mcp.example.com/appai).");
        if (!Uri.TryCreate(options.PublicBaseUrl.Trim(), UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("OAuth:PublicBaseUrl must be an absolute URL without query or fragment.");
        if (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            throw new InvalidOperationException("OAuth:PublicBaseUrl must be https (http is allowed only for localhost).");

        // No trailing slash: clients compare the issuer string exactly.
        Issuer = uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath.TrimEnd('/');
        IssuerPath = uri.AbsolutePath.TrimEnd('/');
        McpPath = "/" + options.McpPath.Trim().Trim('/');
        Resource = Issuer + McpPath;
    }

    /// <summary>The OAuth issuer, e.g. https://host/appai (no trailing slash).</summary>
    public string Issuer { get; }

    /// <summary>Path part of the issuer ("/appai"), or empty when the app is at the host root.</summary>
    public string IssuerPath { get; }

    public string McpPath { get; }

    /// <summary>The MCP endpoint, the OAuth "resource" a token is issued for.</summary>
    public string Resource { get; }

    public string ProtectedResourceMetadata => Issuer + ProtectedResourceDocumentPath;
    public string AuthorizationEndpoint => Issuer + "/oauth/authorize";
    public string TokenEndpoint => Issuer + "/oauth/token";
    public string RevocationEndpoint => Issuer + "/oauth/revoke";
    public string RegistrationEndpoint => Issuer + "/oauth/register";

    /// <summary>
    /// Authorization-server metadata paths a client may probe, relative to the app: the plain forms (they also serve
    /// the "{issuer}/.well-known/..." suffix form) and, when the issuer has a path, the forms with the issuer path
    /// appended (RFC 8414 path insertion, reached through the root rewrite).
    /// </summary>
    public IReadOnlyList<string> ConfigurationPaths()
    {
        var paths = new List<string> { OpenIdConfigurationPath, AuthorizationServerPath };
        if (IssuerPath.Length > 0)
        {
            paths.Add(OpenIdConfigurationPath + IssuerPath);
            paths.Add(AuthorizationServerPath + IssuerPath);
        }
        return paths;
    }

    /// <summary>True when <paramref name="resourcePath"/> (the part after the document path) names the MCP resource.</summary>
    public bool IsResourcePath(string? resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return true;
        var requested = "/" + resourcePath.Trim('/');
        return string.Equals(requested, McpPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(requested, IssuerPath + McpPath, StringComparison.OrdinalIgnoreCase);
    }
}
