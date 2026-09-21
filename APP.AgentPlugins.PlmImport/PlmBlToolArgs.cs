using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>Shared arg parsing / JSON for ExternalDll tools that wrap PlmImportEngine.</summary>
internal static class PlmBlToolArgs
{
    public static int? ParseInt(IReadOnlyDictionary<string, string> args, string key)
    {
        if (args == null || !args.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;
        if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        return null;
    }

    public static bool ParseBool(IReadOnlyDictionary<string, string> args, string key, bool defaultValue = false)
    {
        if (args == null || !args.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        raw = raw.Trim();
        if (bool.TryParse(raw, out var b))
            return b;
        if (raw == "1" || string.Equals(raw, "yes", System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (raw == "0" || string.Equals(raw, "no", System.StringComparison.OrdinalIgnoreCase))
            return false;
        return defaultValue;
    }

    public static string? GetString(IReadOnlyDictionary<string, string> args, string key)
    {
        if (args == null || !args.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;
        return raw.Trim();
    }

    public static string Serialize(object? value) =>
        JsonConvert.SerializeObject(value);
}
