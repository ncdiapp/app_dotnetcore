namespace APP.OAuth;

/// <summary>Base of the four OAuth rows. Id is the OpenIddict identifier; ConcurrencyToken is rotated on every write.</summary>
public abstract class OAuthEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("N");
}

/// <summary>dbo.AppOAuthApplication. Array/map fields hold JSON text, exactly as stored.</summary>
public class OAuthApplication : OAuthEntity
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ApplicationType { get; set; }
    public string? ClientType { get; set; }
    public string? ConsentType { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNames { get; set; }
    public string? JsonWebKeySet { get; set; }
    public string? Permissions { get; set; }
    public string? PostLogoutRedirectUris { get; set; }
    public string? Properties { get; set; }
    public string? RedirectUris { get; set; }
    public string? Requirements { get; set; }
    public string? Settings { get; set; }
}

/// <summary>dbo.AppOAuthScope.</summary>
public class OAuthScope : OAuthEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Descriptions { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNames { get; set; }
    public string? Properties { get; set; }
    public string? Resources { get; set; }
}

/// <summary>dbo.AppOAuthAuthorization. CompanyId is copied from the "company_id" property when the row is saved.</summary>
public class OAuthAuthorization : OAuthEntity
{
    public string? ApplicationId { get; set; }
    public int? CompanyId { get; set; }
    public string? Subject { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Scopes { get; set; }
    public string? Properties { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
}

/// <summary>dbo.AppOAuthToken. Payload and ReferenceId are already protected/hashed by OpenIddict.</summary>
public class OAuthToken : OAuthEntity
{
    public string? ApplicationId { get; set; }
    public string? AuthorizationId { get; set; }
    public int? CompanyId { get; set; }
    public string? Subject { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? ReferenceId { get; set; }
    public string? Payload { get; set; }
    public string? Properties { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset? RedemptionDate { get; set; }
}

/// <summary>Where the OAuth tables live (AppMasterDB connection string).</summary>
public sealed class OAuthDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
