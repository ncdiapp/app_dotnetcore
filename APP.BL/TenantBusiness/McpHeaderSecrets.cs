using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace App.BL.TenantBusiness
{
    /// <summary>
    /// Static MCP headers (AppAgentMcpServer.Headers) can hold tokens. They are encrypted at rest and never sent back to
    /// the browser: the UI receives <see cref="Mask"/> for every value, and a masked value that comes back on save
    /// keeps the stored value for that header.
    /// </summary>
    public static class McpHeaderSecrets
    {
        public const string Mask = "********";

        // stored (AES:… or legacy plain JSON) -> plain JSON object text; "" when empty
        public static string Unprotect(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return "";
            return AppConnectionStringEncryptionBL.Decrypt(stored);
        }

        // plain JSON -> value for the database
        public static string Protect(string plainJson) =>
            string.IsNullOrWhiteSpace(plainJson) ? "" : AppConnectionStringEncryptionBL.Encrypt(plainJson);

        // For API responses: same header names, every value replaced by the mask.
        public static string MaskForClient(string stored)
        {
            var map = Parse(Unprotect(stored));
            if (map.Count == 0) return "";
            foreach (var key in new List<string>(map.Keys)) map[key] = Mask;
            return JsonConvert.SerializeObject(map);
        }

        // Fills masked values from the stored headers; masked values with no stored counterpart are dropped.
        public static string MergeMasked(string incomingJson, string stored)
        {
            var incoming = Parse(incomingJson);
            if (incoming.Count == 0) return "";
            var existing = Parse(Unprotect(stored));

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in incoming)
            {
                if (kv.Value == Mask)
                {
                    if (existing.TryGetValue(kv.Key, out var old)) result[kv.Key] = old;
                }
                else result[kv.Key] = kv.Value;
            }
            return result.Count == 0 ? "" : JsonConvert.SerializeObject(result);
        }

        public static Dictionary<string, string> Parse(string json)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json)) return map;
            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (parsed != null)
                    foreach (var kv in parsed)
                        if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null) map[kv.Key.Trim()] = kv.Value;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "MCP header JSON is invalid");
            }
            return map;
        }
    }
}
