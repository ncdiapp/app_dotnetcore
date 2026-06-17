using APP.Components.Dto;

namespace APP.Framework.Plugin;

/// <summary>
/// Per-request context injected by the platform into every plugin invocation.
/// Plugins receive this instead of directly casting ServerContext — they need no
/// knowledge of the platform identity infrastructure.
/// </summary>
public sealed class PluginContext
{
    /// <summary>Tenant database connection string, plain text at runtime.</summary>
    public string ConnectionString { get; }

    /// <summary>
    /// The registered method name from AppExternalMethodRegister.MethodName.
    /// Multi-method plugin classes switch on this to dispatch internally.
    /// </summary>
    public string MethodName { get; }

    public PluginContext(string connectionString, string methodName)
    {
        ConnectionString = connectionString;
        MethodName = methodName;
    }

    /// <summary>
    /// Creates a PluginContext from the current request's ServerContext.
    /// Called by AppPluginEngine — plugins never need to call this directly.
    /// </summary>
    public static PluginContext FromServerContext(string methodName)
    {
        var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
        return new PluginContext(
            identity?.CurrentUserDbConnectionString ?? string.Empty,
            methodName);
    }
}
