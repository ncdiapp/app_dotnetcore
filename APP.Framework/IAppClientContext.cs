using APP.Components.Dto;
using APP.Framework.Communication;

namespace APP.Framework;

/// <summary>
/// Per-request client context. Replaces direct reads from the static ServerContext.Instance
/// singleton, which is unsafe under .NET 10's concurrent async pipeline.
///
/// Phase 5: backed by HttpAppClientContext which delegates to ServerContext.Instance.
/// Phase 6 goal: inject IHttpContextAccessor directly into HttpAppClientContext so the
///   static singleton is no longer needed at all.
///
/// Registration: services.AddScoped&lt;IAppClientContext, HttpAppClientContext&gt;()
/// </summary>
public interface IAppClientContext
{
    /// <summary>Current user's PK (int or Guid depending on tenant config).</summary>
    object? CurrentUid { get; }

    /// <summary>Current user's active company.</summary>
    object? CurrentCompanyId { get; }

    /// <summary>Login user type (0 = internal, 1 = external, etc.).</summary>
    int CurrentLoginUserType { get; }

    /// <summary>Full resolved identity for the current request.</summary>
    IClientIdentity? CurrentIdentity { get; }

    /// <summary>Session token for the current request.</summary>
    object? CurrentSessionId { get; }

    /// <summary>IANA time zone key for the current user (e.g. "Asia/Hong_Kong").</summary>
    string? CurrentUserTimeZoneKey { get; }

    /// <summary>Tenant database connection string for the current user.</summary>
    string? CurrentUserDbConnectionString { get; }

    /// <summary>Tenant database name for the current user.</summary>
    string? CurrentUserDataBaseName { get; }

    /// <summary>Registered data source ID for the current tenant.</summary>
    int DataSourceId { get; }

    /// <summary>True if request originated from a browser (vs. server-to-server).</summary>
    bool? IsCallingFromBrowser { get; }

    /// <summary>Company-level feature flags and labels for the current session.</summary>
    CompanySettingDto CompanySettings { get; }
}
