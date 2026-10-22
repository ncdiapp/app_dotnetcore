using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// ChatSession-scoped AI polish cache (TemplateContent + AiPolishedRenderSpec).
    /// Process memory only; never written back to AppAgentLibraryTool (v1).
    /// </summary>
    public static class AgentAiPolishCacheBL
    {
        public const int SampleMaxRows = 10;

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AiPolishedRenderSpec>> Sessions
            = new(StringComparer.OrdinalIgnoreCase);

        public static string MakeKey(string datasetName, string layout, string promptHash = null)
        {
            var ds = string.IsNullOrWhiteSpace(datasetName) ? "_" : datasetName.Trim();
            var lay = string.IsNullOrWhiteSpace(layout) ? "table" : layout.Trim().ToLowerInvariant();
            var ph = string.IsNullOrWhiteSpace(promptHash) ? "default" : promptHash.Trim();
            return ds + "|" + lay + "|" + ph;
        }

        public static string HashPrompt(string userPromptOrContext)
        {
            if (string.IsNullOrWhiteSpace(userPromptOrContext))
                return "default";
            var bytes = Encoding.UTF8.GetBytes(userPromptOrContext.Trim());
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
        }

        public static void Put(string chatSessionKey, string datasetName, string layout, AiPolishedRenderSpec spec, string promptHash = null)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || spec == null) return;
            var bag = Sessions.GetOrAdd(chatSessionKey.Trim(),
                _ => new ConcurrentDictionary<string, AiPolishedRenderSpec>(StringComparer.OrdinalIgnoreCase));
            var key = MakeKey(datasetName, layout ?? spec.Layout, promptHash);
            if (string.IsNullOrWhiteSpace(spec.Layout))
                spec.Layout = layout;
            bag[key] = spec;
            // Also store under default prompt hash for render tools that omit promptHash
            var defaultKey = MakeKey(datasetName, layout ?? spec.Layout, null);
            if (!string.Equals(key, defaultKey, StringComparison.OrdinalIgnoreCase))
                bag[defaultKey] = spec;
        }

        public static bool TryGet(string chatSessionKey, string datasetName, string layout, out AiPolishedRenderSpec spec, string promptHash = null)
        {
            spec = null;
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            if (!Sessions.TryGetValue(chatSessionKey.Trim(), out var bag)) return false;
            var key = MakeKey(datasetName, layout, promptHash);
            if (bag.TryGetValue(key, out spec) && spec != null) return true;
            if (!string.IsNullOrWhiteSpace(promptHash))
            {
                var fallback = MakeKey(datasetName, layout, null);
                return bag.TryGetValue(fallback, out spec) && spec != null;
            }
            return false;
        }

        public static void InvalidateDataset(string chatSessionKey, string datasetName)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(datasetName)) return;
            if (!Sessions.TryGetValue(chatSessionKey.Trim(), out var bag)) return;
            var prefix = datasetName.Trim() + "|";
            foreach (var k in bag.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
                bag.TryRemove(k, out _);
        }

        public static void ClearSession(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            Sessions.TryRemove(chatSessionKey.Trim(), out _);
        }

        /// <summary>Distinct values for filter UI from full row array (capped).</summary>
        public static JObject BuildFilterOptions(JArray rows, AiPolishedRenderSpec spec)
        {
            var result = new JObject();
            if (rows == null || spec?.Bindings?.FilterFields == null) return result;
            foreach (var ff in spec.Bindings.FilterFields)
            {
                if (ff == null || string.IsNullOrWhiteSpace(ff.Name)) continue;
                if (string.Equals(ff.Control, "search", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ff.Name, "_search", StringComparison.OrdinalIgnoreCase))
                    continue;
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var list = new JArray();
                foreach (var token in rows.OfType<JObject>())
                {
                    var v = token[ff.Name]?.ToString();
                    if (string.IsNullOrWhiteSpace(v)) continue;
                    v = v.Trim();
                    if (!set.Add(v)) continue;
                    list.Add(v);
                    if (list.Count >= AiPolishedRenderSpec.MaxDistinctFilterValues) break;
                }
                result[ff.Name] = list;
            }
            return result;
        }
    }
}
