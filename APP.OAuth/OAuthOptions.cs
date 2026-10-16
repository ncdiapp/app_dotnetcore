namespace APP.OAuth;

/// <summary>
/// Settings of the AppAI OAuth server ("OAuth" section of appsettings). The whole feature is off unless
/// <see cref="Enabled"/> is true, so adding this code changes nothing until it is configured.
/// </summary>
public sealed class OAuthOptions
{
    public const string SectionName = "OAuth";

    public bool Enabled { get; set; }

    /// <summary>
    /// Public HTTPS address of the app including its path base, e.g. https://mcp.example.com/appai. Used as the
    /// OAuth issuer and to build every metadata URL. Required when enabled; http is accepted only for localhost.
    /// Not derived from the request, because behind a proxy the request host is not the public one.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Path of the MCP endpoint under <see cref="PublicBaseUrl"/> (the OAuth "resource").</summary>
    public string McpPath { get; set; } = "/mcp";

    /// <summary>The one scope clients request to use the MCP gateway.</summary>
    public string Scope { get; set; } = "mcp";

    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>
    /// True when TLS ends at a reverse proxy, so the app itself sees plain http. OpenIddict refuses non-https
    /// requests by default. Use together with trusted forwarded-header handling.
    /// </summary>
    public bool TlsTerminatedUpstream { get; set; }

    /// <summary>Serve POST /oauth/register and advertise it in the metadata (RFC 7591 dynamic client registration).</summary>
    public bool RegistrationEnabled { get; set; }

    /// <summary>
    /// Absolute URL of the AppAI web page that signs the user in and asks for consent (the React route
    /// /mcp-authorize). /oauth/authorize sends unauthenticated browsers there. Required when enabled.
    /// </summary>
    public string ConsentPageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Hosts that dynamically registered clients may redirect to (https only), plus loopback http for desktop
    /// tools. Registration with any other redirect URI is refused.
    /// </summary>
    public string[] AllowedRedirectHosts { get; set; } = { "claude.ai", "claude.com", "chatgpt.com", "chat.openai.com" };

    /// <summary>Upper bound on dynamically registered clients, so open registration cannot fill the table.</summary>
    public int MaxDynamicClients { get; set; } = 5000;

    /// <summary>A dynamically registered client that nobody authorized within this many days is deleted.</summary>
    public int DynamicClientRetentionDays { get; set; } = 7;

    /// <summary>Lifetime of the token an in-app agent run uses to call the gateway as the signed-in user.</summary>
    public int AgentTokenMinutes { get; set; } = 60;

    /// <summary>
    /// Exact MCP server URLs (as registered for the agent) that receive the signed-in user's own token on every call
    /// instead of their stored static headers. Empty means no agent uses user tokens.
    /// </summary>
    public string[] AgentTokenServerUrls { get; set; } = Array.Empty<string>();

    /// <summary>Certificate thumbprints (certificate store) for token signing and encryption. Required outside Development.</summary>
    public string? SigningCertificateThumbprint { get; set; }
    public string? EncryptionCertificateThumbprint { get; set; }
}
