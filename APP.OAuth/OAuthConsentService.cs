using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APP.OAuth;

/// <summary>What the consent page shows the user before they approve.</summary>
public sealed record ConsentRequestInfo(string ClientName, string RedirectHost, IReadOnlyList<string> Scopes, string Resource);

/// <summary>
/// The server side of the consent page. The page sends the authorize URL the AI client opened; this service re-validates
/// it (never trusting the page), describes it, and turns the user's decision into the URL the browser must open next.
/// </summary>
public sealed class OAuthConsentService
{
    private readonly IOpenIddictApplicationManager _apps;
    private readonly ConsentTicketService _tickets;
    private readonly OAuthOptions _options;
    private readonly OAuthUrls _urls;

    public OAuthConsentService(IOpenIddictApplicationManager apps, ConsentTicketService tickets, IOptions<OAuthOptions> options)
    {
        _apps = apps;
        _tickets = tickets;
        _options = options.Value;
        _urls = new OAuthUrls(_options);
    }

    public async Task<ConsentRequestInfo> DescribeAsync(string authorizeUrl, CancellationToken ct)
    {
        var (_, info) = await ValidateAsync(authorizeUrl, ct).ConfigureAwait(false);
        return info;
    }

    /// <summary>Returns the authorize URL with a consent ticket for <paramref name="user"/> appended.</summary>
    public async Task<string> ApproveAsync(OAuthWebUser user, string authorizeUrl, CancellationToken ct)
    {
        var (query, _) = await ValidateAsync(authorizeUrl, ct).ConfigureAwait(false);
        var ticket = _tickets.Create(user, ConsentTicketService.HashRequest(k => query.TryGetValue(k, out var v) ? v.ToString() : null));
        return authorizeUrl + "&consent_ticket=" + Uri.EscapeDataString(ticket);
    }

    /// <summary>Returns the authorize URL that makes the server send the client back with access_denied.</summary>
    public async Task<string> DenyAsync(string authorizeUrl, CancellationToken ct)
    {
        await ValidateAsync(authorizeUrl, ct).ConfigureAwait(false);
        return authorizeUrl + "&consent_deny=1";
    }

    private async Task<(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> Query, ConsentRequestInfo Info)> ValidateAsync(
        string authorizeUrl, CancellationToken ct)
    {
        const string bad = "This sign-in request is not valid. Start again from the AI application.";
        var prefix = _urls.AuthorizationEndpoint + "?";
        if (string.IsNullOrEmpty(authorizeUrl) || !authorizeUrl.StartsWith(prefix, StringComparison.Ordinal))
            throw new OAuthConsentException(bad);

        var query = QueryHelpers.ParseQuery(authorizeUrl.Substring(prefix.Length));
        string? One(string key) => query.TryGetValue(key, out var v) && v.Count == 1 ? v[0] : null;

        // The consent parameters are ours alone: a client-supplied one would pre-approve the request.
        if (query.ContainsKey("consent_ticket") || query.ContainsKey("consent_deny")) throw new OAuthConsentException(bad);

        var clientId = One("client_id");
        var redirectUri = One("redirect_uri");
        var resource = One("resource");
        if (clientId is null || redirectUri is null || One("response_type") != "code"
            || One("code_challenge") is null || One("code_challenge_method") != CodeChallengeMethods.Sha256
            || !string.Equals(resource, _urls.Resource, StringComparison.Ordinal))
            throw new OAuthConsentException(bad);

        var app = await _apps.FindByClientIdAsync(clientId, ct).ConfigureAwait(false);
        if (app is null) throw new OAuthConsentException(bad);
        var registered = await _apps.GetRedirectUrisAsync(app, ct).ConfigureAwait(false);
        if (!registered.Contains(redirectUri, StringComparer.Ordinal)) throw new OAuthConsentException(bad);

        var allowed = new HashSet<string>(new[] { _options.Scope, Scopes.OfflineAccess });
        var scopes = (One("scope") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (scopes.Length == 0 || scopes.Any(s => !allowed.Contains(s))) throw new OAuthConsentException(bad);

        var name = await _apps.GetDisplayNameAsync(app, ct).ConfigureAwait(false);
        var host = new Uri(redirectUri).Host;
        return (query, new ConsentRequestInfo(string.IsNullOrWhiteSpace(name) ? clientId : name!, host, scopes, resource!));
    }
}
