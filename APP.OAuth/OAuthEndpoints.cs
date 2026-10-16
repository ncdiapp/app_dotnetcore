using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APP.OAuth;

/// <summary>The /oauth/authorize and /oauth/token endpoints (OpenIddict runs in passthrough mode).</summary>
public static class OAuthEndpoints
{
    private const string Scheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;

    public static IEndpointRouteBuilder MapAppOAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OAuthOptions>>().Value;
        if (!options.Enabled) return endpoints;

        endpoints.MapMethods("/oauth/authorize", new[] { "GET", "POST" }, AuthorizeAsync).ExcludeFromDescription();
        endpoints.MapPost("/oauth/token", TokenAsync).ExcludeFromDescription();
        if (options.RegistrationEnabled)
            endpoints.MapPost("/oauth/register", DynamicClientRegistration.HandleAsync).ExcludeFromDescription();
        return endpoints;
    }

    private static IResult Reject(string error, string description) =>
        Results.Forbid(new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }), new[] { Scheme });

    // ── /oauth/authorize ────────────────────────────────────────────────────────

    private static async Task<IResult> AuthorizeAsync(HttpContext http, OAuthUrls urls, IOptions<OAuthOptions> options,
        ConsentTicketService tickets, IOAuthIdentityBridge bridge, IOpenIddictApplicationManager apps,
        IOpenIddictAuthorizationManager authorizations, ILoggerFactory loggers)
    {
        var log = loggers.CreateLogger("APP.OAuth.Authorize");
        var request = http.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenIddict request is missing.");
        var query = http.Request.Query;

        if (query["consent_deny"] == "1")
        {
            log.LogInformation("OAuth consent denied for client {ClientId}", request.ClientId);
            return Reject(Errors.AccessDenied, "The user denied the request.");
        }

        var ticket = query["consent_ticket"].ToString();
        if (string.IsNullOrEmpty(ticket))
        {
            // Not approved yet: send the browser to the AppAI sign-in and consent page, carrying this exact request.
            if (request.HasPromptValue(PromptValues.None))
                return Reject(Errors.InteractionRequired, "The user must sign in and approve this request.");
            var page = options.Value.ConsentPageUrl + (options.Value.ConsentPageUrl.Contains('?') ? "&" : "?")
                + "authorize_url=" + Uri.EscapeDataString(urls.AuthorizationEndpoint + http.Request.QueryString.Value);
            return Results.Redirect(page);
        }

        var hash = ConsentTicketService.HashRequest(k => query.TryGetValue(k, out var v) ? v.ToString() : null);
        if (!tickets.TryValidate(ticket, hash, out var user))
        {
            log.LogWarning("OAuth consent ticket rejected (invalid, expired or for another request) for client {ClientId}", request.ClientId);
            return Reject(Errors.AccessDenied, "The approval expired or does not match this request. Start again.");
        }

        var app = await apps.FindByClientIdAsync(request.ClientId!, http.RequestAborted).ConfigureAwait(false);
        if (app is null) return Reject(Errors.InvalidClient, "Unknown client.");

        var allowed = new HashSet<string>(new[] { options.Value.Scope, Scopes.OfflineAccess });
        var scopes = request.GetScopes().Where(allowed.Contains).ToList();
        var expires = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenDays);
        var grantSession = bridge.CreateGrantSession(user.UserId, user.CompanyId, expires);

        var descriptor = new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = await apps.GetIdAsync(app, http.RequestAborted).ConfigureAwait(false),
            Status = Statuses.Valid,
            Subject = user.UserId.ToString(CultureInfo.InvariantCulture),
            Type = AuthorizationTypes.Permanent,
            CreationDate = DateTimeOffset.UtcNow
        };
        foreach (var scope in scopes) descriptor.Scopes.Add(scope);
        descriptor.Properties[OAuthClaims.CompanyId] = JsonSerializer.SerializeToElement(user.CompanyId);
        descriptor.Properties[OAuthClaims.GrantSession] = JsonSerializer.SerializeToElement(grantSession);
        var authorization = await authorizations.CreateAsync(descriptor, http.RequestAborted).ConfigureAwait(false);

        var identity = new ClaimsIdentity(Scheme, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.UserId.ToString(CultureInfo.InvariantCulture));
        identity.SetClaim(OAuthClaims.CompanyId, user.CompanyId.ToString(CultureInfo.InvariantCulture));
        identity.SetClaim(OAuthClaims.GrantSession, grantSession);
        identity.SetScopes(scopes);
        identity.SetResources(request.GetResources());
        identity.SetAuthorizationId(await authorizations.GetIdAsync(authorization, http.RequestAborted).ConfigureAwait(false));
        identity.SetDestinations(_ => new[] { Destinations.AccessToken });

        log.LogInformation("OAuth authorization granted: user {UserId} company {CompanyId} client {ClientId}", user.UserId, user.CompanyId, request.ClientId);
        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, Scheme);
    }

    // ── /oauth/token ────────────────────────────────────────────────────────────

    private static async Task<IResult> TokenAsync(HttpContext http, IOAuthIdentityBridge bridge, ILoggerFactory loggers)
    {
        var log = loggers.CreateLogger("APP.OAuth.Token");
        var request = http.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenIddict request is missing.");
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            return Reject(Errors.UnsupportedGrantType, "Only authorization_code and refresh_token are supported.");

        var result = await http.AuthenticateAsync(Scheme).ConfigureAwait(false);
        var principal = result.Principal;
        if (principal is null) return Reject(Errors.InvalidGrant, "The grant is not valid.");

        // The connection lives as long as its grant session: sign-out of the user, user deactivation, an admin
        // revoking the connection and the absolute lifetime all end it here, even if the refresh token is intact.
        if (!int.TryParse(principal.GetClaim(Claims.Subject), NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            || !int.TryParse(principal.GetClaim(OAuthClaims.CompanyId), NumberStyles.None, CultureInfo.InvariantCulture, out var companyId)
            || string.IsNullOrEmpty(principal.GetClaim(OAuthClaims.GrantSession))
            || !bridge.IsGrantSessionValid(principal.GetClaim(OAuthClaims.GrantSession)!, userId, companyId))
        {
            log.LogWarning("OAuth {Grant} refused: grant session ended or user inactive (client {ClientId})", request.GrantType, request.ClientId);
            return Reject(Errors.InvalidGrant, "This connection has ended. Sign in again from the AI application.");
        }

        principal.SetDestinations(_ => new[] { Destinations.AccessToken });
        log.LogInformation("OAuth {Grant} accepted: user {UserId} company {CompanyId} client {ClientId}", request.GrantType, userId, companyId, request.ClientId);
        return Results.SignIn(principal, properties: null, Scheme);
    }
}
