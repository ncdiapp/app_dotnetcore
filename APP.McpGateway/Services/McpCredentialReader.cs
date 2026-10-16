using Microsoft.Net.Http.Headers;

namespace McpGateway.Services;

/// <summary>Which request header supplied the MCP credential.</summary>
public enum McpCredentialSource
{
    None,
    IntegrationHeader,
    BearerAuthorization
}

/// <summary>
/// Reads the caller's MCP credential from a request. Headers only: a token in the query string is never read
/// (it would end up in server logs). IntergrationAccessToken wins when both headers are present.
/// </summary>
public static class McpCredentialReader
{
    // Spelling is intentional: it matches the rest of the codebase and the Integration token admin screen.
    public const string IntegrationHeaderName = "IntergrationAccessToken";

    private const string BearerPrefix = "Bearer ";

    public static McpCredentialSource TryRead(IHeaderDictionary headers, out string token)
    {
        var integration = headers[IntegrationHeaderName].FirstOrDefault()?.Trim();
        if (!string.IsNullOrEmpty(integration))
        {
            token = integration;
            return McpCredentialSource.IntegrationHeader;
        }

        // Only the Bearer scheme is a token; Basic, Negotiate etc. are somebody else's credential.
        var authorization = headers[HeaderNames.Authorization].FirstOrDefault()?.Trim();
        if (authorization != null && authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var bearer = authorization.Substring(BearerPrefix.Length).Trim();
            if (bearer.Length > 0)
            {
                token = bearer;
                return McpCredentialSource.BearerAuthorization;
            }
        }

        token = string.Empty;
        return McpCredentialSource.None;
    }
}
