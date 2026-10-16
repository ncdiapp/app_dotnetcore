using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace APP.OAuth;

/// <summary>Converts OpenIddict's immutable collections to and from the JSON text kept in the tables.</summary>
internal static class OAuthJson
{
    public static ImmutableArray<string> Array(string? json) =>
        string.IsNullOrEmpty(json) ? ImmutableArray<string>.Empty : JsonSerializer.Deserialize<ImmutableArray<string>>(json);

    public static string? Array(ImmutableArray<string> values) =>
        values.IsDefaultOrEmpty ? null : JsonSerializer.Serialize(values);

    public static ImmutableDictionary<string, JsonElement> Properties(string? json) =>
        string.IsNullOrEmpty(json)
            ? ImmutableDictionary<string, JsonElement>.Empty
            : JsonSerializer.Deserialize<ImmutableDictionary<string, JsonElement>>(json)!;

    public static string? Properties(ImmutableDictionary<string, JsonElement>? values) =>
        values is null || values.IsEmpty ? null : JsonSerializer.Serialize(values);

    public static ImmutableDictionary<CultureInfo, string> Cultures(string? json)
    {
        if (string.IsNullOrEmpty(json)) return ImmutableDictionary<CultureInfo, string>.Empty;
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        return raw.ToImmutableDictionary(k => CultureInfo.GetCultureInfo(k.Key), v => v.Value);
    }

    public static string? Cultures(ImmutableDictionary<CultureInfo, string>? values) =>
        values is null || values.IsEmpty ? null : JsonSerializer.Serialize(values.ToDictionary(k => k.Key.Name, v => v.Value));

    public static ImmutableDictionary<string, string> Settings(string? json) =>
        string.IsNullOrEmpty(json) ? ImmutableDictionary<string, string>.Empty : JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(json)!;

    public static string? Settings(ImmutableDictionary<string, string>? values) =>
        values is null || values.IsEmpty ? null : JsonSerializer.Serialize(values);

    public static JsonWebKeySet? KeySet(string? json) => string.IsNullOrEmpty(json) ? null : new JsonWebKeySet(json);

    public static string? KeySet(JsonWebKeySet? set) => set is null ? null : JsonSerializer.Serialize(set);

    /// <summary>The "company_id" property (number or numeric string) of a Properties JSON object, if any.</summary>
    public static int? CompanyId(string? propertiesJson)
    {
        if (string.IsNullOrEmpty(propertiesJson)) return null;
        using var doc = JsonDocument.Parse(propertiesJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("company_id", out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var s)) return s;
        return null;
    }
}
