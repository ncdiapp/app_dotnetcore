using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;

namespace APP.OAuth;

public static class OAuthStoreExtensions
{
    /// <summary>
    /// Registers the ADO.NET stores and tells OpenIddict to use them. Call before AddOpenIddict().AddCore(...)
    /// finishes: pass the same builder to <see cref="UseAppOAuthStores"/>.
    /// </summary>
    public static IServiceCollection AddAppOAuthStores(this IServiceCollection services, string masterDbConnectionString)
    {
        services.Configure<OAuthDbOptions>(o => o.ConnectionString = masterDbConnectionString);
        services.TryAddSingleton<IOpenIddictApplicationStore<OAuthApplication>, ApplicationStore>();
        services.TryAddSingleton<IOpenIddictAuthorizationStore<OAuthAuthorization>, AuthorizationStore>();
        services.TryAddSingleton<IOpenIddictScopeStore<OAuthScope>, ScopeStore>();
        services.TryAddSingleton<IOpenIddictTokenStore<OAuthToken>, TokenStore>();
        return services;
    }
}
