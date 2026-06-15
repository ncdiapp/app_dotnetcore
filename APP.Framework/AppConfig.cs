#if NETFRAMEWORK
using System.Configuration;
#else
using Microsoft.Extensions.Configuration;
#endif

namespace APP.Framework;

/// <summary>
/// Static configuration bridge.
/// .NET 4.8  — reads from System.Configuration.ConfigurationManager.AppSettings.
/// .NET 10   — reads from IConfiguration set by AppConfig.SetConfiguration() at startup.
///             Dot-notation keys auto-translate to IConfiguration colon notation
///             (e.g. "Agent.MaxIterations" → "Agent:MaxIterations").
/// Registration: call AppConfig.SetConfiguration(app.Configuration) in Program.cs before app.Run().
/// </summary>
public static class AppConfig
{
#if !NETFRAMEWORK
    private static IConfiguration? _configuration;

    public static void SetConfiguration(IConfiguration configuration)
        => _configuration = configuration;
#endif

    public static string? Get(string key)
    {
#if NETFRAMEWORK
        return ConfigurationManager.AppSettings[key];
#else
        return _configuration?[key.Replace('.', ':')];
#endif
    }

    public static string? GetConnectionString(string name)
    {
#if NETFRAMEWORK
        return ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
#else
        return _configuration?.GetConnectionString(name);
#endif
    }
}
