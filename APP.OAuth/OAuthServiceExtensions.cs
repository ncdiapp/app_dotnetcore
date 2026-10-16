using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace APP.OAuth;

public static class OAuthServiceExtensions
{
    /// <summary>
    /// Registers the OAuth settings and the 401 challenge builder (always, they are harmless), and, only when
    /// OAuth:Enabled is true, the stores and the OpenIddict server. Existing behaviour is unchanged when disabled.
    /// </summary>
    public static IServiceCollection AddAppOAuth(this IServiceCollection services, IConfiguration configuration,
        string masterDbConnectionString, bool isDevelopment)
    {
        services.Configure<OAuthOptions>(configuration.GetSection(OAuthOptions.SectionName));
        services.TryAddSingleton<OAuthChallenge>();

        var options = configuration.GetSection(OAuthOptions.SectionName).Get<OAuthOptions>() ?? new OAuthOptions();
        if (!options.Enabled) return services;

        var urls = new OAuthUrls(options);
        if (!Uri.TryCreate(options.ConsentPageUrl, UriKind.Absolute, out var consentPage)
            || !(consentPage.Scheme == Uri.UriSchemeHttps || (consentPage.Scheme == Uri.UriSchemeHttp && consentPage.IsLoopback)))
            throw new InvalidOperationException("OAuth:ConsentPageUrl is required when OAuth is enabled: the https URL of the AppAI /mcp-authorize page.");
        services.AddAppOAuthStores(masterDbConnectionString);

        services.AddOpenIddict()
            .AddCore(core => core
                .SetDefaultApplicationEntity<OAuthApplication>()
                .SetDefaultAuthorizationEntity<OAuthAuthorization>()
                .SetDefaultScopeEntity<OAuthScope>()
                .SetDefaultTokenEntity<OAuthToken>())
            .AddServer(server => ConfigureServer(server, options, urls, isDevelopment))
            .AddValidation(validation =>
            {
                validation.UseLocalServer();
                validation.UseAspNetCore();
                validation.AddAudiences(urls.Resource);   // a token issued for another resource is refused
            });

        // The host must also register an IOAuthIdentityBridge (singleton) over its session code.
        services.AddDataProtection();
        services.AddSingleton(urls);
        services.AddSingleton<ConsentTicketService>();
        services.AddSingleton<AgentTokenService>();
        services.AddSingleton<OAuthBearerAuthenticator>();
        services.AddScoped<OAuthConsentService>();
        services.AddScoped<OAuthConnectionAdmin>();
        services.AddScoped<OAuthClientAdmin>();
        services.AddHostedService<OAuthMaintenanceService>();
        services.AddHostedService<OAuthBootstrapService>();

        return services;
    }

    private static void ConfigureServer(OpenIddictServerBuilder server, OAuthOptions options, OAuthUrls urls, bool isDevelopment)
    {
        server.SetIssuer(new Uri(urls.Issuer + "/"));

        server.SetConfigurationEndpointUris(urls.ConfigurationPaths().Select(p => p.TrimStart('/')).ToArray())
              .SetAuthorizationEndpointUris("oauth/authorize")
              .SetTokenEndpointUris("oauth/token")
              .SetRevocationEndpointUris("oauth/revoke");

        server.AllowAuthorizationCodeFlow().AllowRefreshTokenFlow().RequireProofKeyForCodeExchange();
        server.Configure(o => o.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain));   // S256 only
        server.UseReferenceAccessTokens().UseReferenceRefreshTokens();
        server.SetAccessTokenLifetime(TimeSpan.FromMinutes(options.AccessTokenMinutes))
              .SetRefreshTokenLifetime(TimeSpan.FromDays(options.RefreshTokenDays))
              .SetRefreshTokenReuseLeeway(TimeSpan.Zero);   // a used refresh token can never be replayed

        server.RegisterScopes(Scopes.OfflineAccess, options.Scope);
        server.RegisterResources(urls.Resource);

        server.UseAspNetCore()
              .EnableAuthorizationEndpointPassthrough()
              .EnableTokenEndpointPassthrough();
        if (options.TlsTerminatedUpstream || isDevelopment)
            server.UseAspNetCore().DisableTransportSecurityRequirement();

        // Metadata Claude needs: public PKCE clients authenticate with "none"; registration only once implemented.
        server.AddEventHandler<HandleConfigurationRequestContext>(handler => handler.UseInlineHandler(context =>
        {
            context.Metadata["issuer"] = urls.Issuer;   // exact issuer string, no trailing slash (must equal authorization_servers)
            context.TokenEndpointAuthenticationMethods.Add(ClientAuthenticationMethods.None);
            if (options.RegistrationEnabled)
                context.Metadata["registration_endpoint"] = urls.RegistrationEndpoint;
            return default;
        }).SetOrder(int.MaxValue - 100_000));   // after OpenIddict fills in its own values, so the issuer stays exact

        ConfigureKeys(server, options, isDevelopment);
    }

    private static void ConfigureKeys(OpenIddictServerBuilder server, OAuthOptions options, bool isDevelopment)
    {
        if (isDevelopment && string.IsNullOrWhiteSpace(options.SigningCertificateThumbprint))
        {
            server.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
            return;
        }
        if (string.IsNullOrWhiteSpace(options.SigningCertificateThumbprint) || string.IsNullOrWhiteSpace(options.EncryptionCertificateThumbprint))
            throw new InvalidOperationException(
                "OAuth:SigningCertificateThumbprint and OAuth:EncryptionCertificateThumbprint are required outside Development.");

        server.AddSigningCertificate(FindCertificate(options.SigningCertificateThumbprint))
              .AddEncryptionCertificate(FindCertificate(options.EncryptionCertificateThumbprint));
    }

    private static X509Certificate2 FindCertificate(string thumbprint)
    {
        foreach (var location in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);
            var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint.Replace(" ", string.Empty), validOnly: false);
            if (found.Count > 0) return found[0];
        }
        throw new InvalidOperationException("OAuth certificate with thumbprint " + thumbprint + " was not found in the LocalMachine or CurrentUser store.");
    }
}
