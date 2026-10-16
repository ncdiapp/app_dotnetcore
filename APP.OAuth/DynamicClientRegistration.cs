using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APP.OAuth;

/// <summary>A validated RFC 7591 registration request.</summary>
public sealed record ClientRegistration(string Name, IReadOnlyList<string> RedirectUris, IReadOnlyList<string> Scopes);

/// <summary>
/// RFC 7591 dynamic client registration (POST /oauth/register). Registration is open to anonymous callers by design
/// (clients register themselves before any user signs in), so it only ever creates a public PKCE client that can
/// redirect to an allow-listed host, and is capped in number and rate limited by the host.
/// </summary>
public static class DynamicClientRegistration
{
    private const int MaxRedirectUris = 5;
    private const int MaxNameLength = 100;

    /// <summary>Checks the request body; returns null and sets <paramref name="error"/> (RFC 7591 error code + text) when invalid.</summary>
    public static ClientRegistration? Parse(JsonElement body, OAuthOptions options, out (string Code, string Text) error)
    {
        error = default;
        if (body.ValueKind != JsonValueKind.Object) { error = ("invalid_client_metadata", "The body must be a JSON object."); return null; }

        if (!body.TryGetProperty("redirect_uris", out var uris) || uris.ValueKind != JsonValueKind.Array
            || uris.GetArrayLength() is 0 or > MaxRedirectUris)
        { error = ("invalid_redirect_uri", "redirect_uris must list 1 to " + MaxRedirectUris + " URIs."); return null; }

        var redirects = new List<string>();
        foreach (var item in uris.EnumerateArray())
        {
            var text = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (text is null || !IsAllowedRedirect(text, options))
            { error = ("invalid_redirect_uri", "A redirect URI is not allowed: it must be https on an approved host, or http on localhost."); return null; }
            redirects.Add(text);
        }

        if (body.TryGetProperty("token_endpoint_auth_method", out var method) && method.GetString() != ClientAuthenticationMethods.None)
        { error = ("invalid_client_metadata", "Only token_endpoint_auth_method \"none\" (public client with PKCE) is supported."); return null; }
        if (!SubsetOf(body, "grant_types", new[] { GrantTypes.AuthorizationCode, GrantTypes.RefreshToken }))
        { error = ("invalid_client_metadata", "grant_types may only contain authorization_code and refresh_token."); return null; }
        if (!SubsetOf(body, "response_types", new[] { ResponseTypes.Code }))
        { error = ("invalid_client_metadata", "response_types may only contain code."); return null; }

        var allowedScopes = new HashSet<string>(new[] { options.Scope, Scopes.OfflineAccess });
        var scopes = new List<string>();
        if (body.TryGetProperty("scope", out var scope) && scope.ValueKind == JsonValueKind.String)
        {
            scopes.AddRange(scope.GetString()!.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (scopes.Any(s => !allowedScopes.Contains(s)))
            { error = ("invalid_client_metadata", "scope contains a scope this server does not offer."); return null; }
        }

        var name = body.TryGetProperty("client_name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? "" : "";
        name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (name.Length > MaxNameLength) name = name[..MaxNameLength];
        return new ClientRegistration(name.Length == 0 ? "Unnamed client" : name, redirects, scopes);
    }

    public static bool IsAllowedRedirect(string value, OAuthOptions options)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0) return false;
        if (uri.Scheme == Uri.UriSchemeHttps)
            return options.AllowedRedirectHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
        return uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;   // desktop tools redirect to a local port
    }

    private static bool SubsetOf(JsonElement body, string property, string[] allowed)
    {
        if (!body.TryGetProperty(property, out var value)) return true;
        if (value.ValueKind != JsonValueKind.Array) return false;
        return value.EnumerateArray().All(v => v.ValueKind == JsonValueKind.String && allowed.Contains(v.GetString()));
    }

    internal static OpenIddictApplicationDescriptor Describe(ClientRegistration registration, string clientId, string resource, string scope)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Explicit,
            DisplayName = registration.Name,
            Permissions =
            {
                Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken, Permissions.ResponseTypes.Code,
                Permissions.Prefixes.Scope + scope, Permissions.Prefixes.Scope + Scopes.OfflineAccess,
                Permissions.Prefixes.Resource + resource
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        foreach (var uri in registration.RedirectUris) descriptor.RedirectUris.Add(new Uri(uri));
        descriptor.Properties["dynamic"] = JsonSerializer.SerializeToElement(true);
        return descriptor;
    }

    internal static async Task<IResult> HandleAsync(HttpContext http, JsonElement body, IOpenIddictApplicationManager apps,
        OAuthClientAdmin clients, IOptions<OAuthOptions> options, OAuthUrls urls, ILoggerFactory loggers)
    {
        var log = loggers.CreateLogger("APP.OAuth.Register");
        var registration = Parse(body, options.Value, out var error);
        if (registration is null)
        {
            log.LogWarning("OAuth client registration refused: {Code} {Text}", error.Code, error.Text);
            return Results.Json(new { error = error.Code, error_description = error.Text }, statusCode: StatusCodes.Status400BadRequest);
        }

        if (await clients.CountDynamicAsync(http.RequestAborted).ConfigureAwait(false) >= options.Value.MaxDynamicClients)
        {
            log.LogError("OAuth client registration refused: limit of {Max} dynamic clients reached", options.Value.MaxDynamicClients);
            return Results.Json(new { error = "temporarily_unavailable", error_description = "Registration is full. Try again later." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var clientId = "dcr-" + Guid.NewGuid().ToString("N");
        await apps.CreateAsync(Describe(registration, clientId, urls.Resource, options.Value.Scope), http.RequestAborted).ConfigureAwait(false);
        log.LogInformation("OAuth client registered: {ClientId} name '{Name}' redirects {Redirects}", clientId, registration.Name, string.Join(", ", registration.RedirectUris));

        return Results.Json(new
        {
            client_id = clientId,
            client_id_issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            client_name = registration.Name,
            redirect_uris = registration.RedirectUris,
            token_endpoint_auth_method = ClientAuthenticationMethods.None,
            grant_types = new[] { GrantTypes.AuthorizationCode, GrantTypes.RefreshToken },
            response_types = new[] { ResponseTypes.Code },
            scope = string.Join(' ', new[] { options.Value.Scope, Scopes.OfflineAccess })
        }, statusCode: StatusCodes.Status201Created);
    }
}
