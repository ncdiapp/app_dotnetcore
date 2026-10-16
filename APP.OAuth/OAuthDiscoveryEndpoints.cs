using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace APP.OAuth;

public static class OAuthDiscoveryEndpoints
{
    /// <summary>
    /// Serves the protected-resource metadata (RFC 9728) that tells clients which authorization server issues tokens
    /// for /mcp. Public by design. The authorization-server metadata itself is served by OpenIddict.
    /// Does nothing when OAuth is disabled.
    /// </summary>
    public static IEndpointRouteBuilder MapAppOAuthDiscovery(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OAuthOptions>>().Value;
        if (!options.Enabled) return endpoints;

        var urls = new OAuthUrls(options);
        var document = new Dictionary<string, object>
        {
            ["resource"] = urls.Resource,
            ["authorization_servers"] = new[] { urls.Issuer },
            ["scopes_supported"] = new[] { options.Scope },
            ["bearer_methods_supported"] = new[] { "header" },
            ["resource_name"] = "AppAI MCP Gateway"
        };

        IResult Respond(string? resourcePath, HttpContext context)
        {
            if (!urls.IsResourcePath(resourcePath)) return Results.NotFound();
            context.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Json(document);
        }

        endpoints.MapGet(OAuthUrls.ProtectedResourceDocumentPath, (HttpContext c) => Respond(null, c)).ExcludeFromDescription();
        endpoints.MapGet(OAuthUrls.ProtectedResourceDocumentPath + "/{**resourcePath}",
            (string? resourcePath, HttpContext c) => Respond(resourcePath, c)).ExcludeFromDescription();
        return endpoints;
    }
}
