using APP.Components.Dto;
using APP.Framework.Communication;

namespace APP.Framework;

/// <summary>
/// Scoped ASP.NET Core implementation of IAppClientContext.
/// Phase 5: delegates every property to the legacy static ServerContext.Instance
///   so zero APP.BL call sites need to change yet.
/// Phase 6 goal: inject IHttpContextAccessor and resolve identity directly,
///   eliminating the static singleton entirely.
/// </summary>
public class HttpAppClientContext : IAppClientContext
{
    // TODO-PHASE6: replace with injected IHttpContextAccessor
    private static ServerContext Ctx => ServerContext.Instance;

    public object? CurrentUid => Ctx.CurrentUid;
    public object? CurrentCompanyId => Ctx.CurrentCompanyId;
    public int CurrentLoginUserType => Ctx.CurrentLoginUserType;
    public IClientIdentity? CurrentIdentity => Ctx.CurrnetClientIdentity;
    public object? CurrentSessionId => Ctx.CurrentSessionId;
    public string? CurrentUserTimeZoneKey => Ctx.CurrentUserTimeZoneKey;
    public string? CurrentUserDbConnectionString => Ctx.CurrentUserDbConnectionString;
    public string? CurrentUserDataBaseName => Ctx.CurrentUserDataBaseName;
    public int DataSourceId => Ctx.DataSourceId;
    public bool? IsCallingFromBrowser => Ctx.IsCallingFromBrowser;
    public CompanySettingDto CompanySettings => Ctx.CompanySettings;
}
