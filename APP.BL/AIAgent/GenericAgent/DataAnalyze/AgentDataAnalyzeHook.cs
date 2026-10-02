using System;
using System.Collections.Generic;
using System.Linq;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.DataAnalyze
{
    /// <summary>
    /// Intercepts tabular tool results (SqlQuery / HttpRest / BuiltIn / MCP / …),
    /// caches them under ChatSessionKey, and optionally rewrites large payloads to an envelope.
    /// </summary>
    public static class AgentDataAnalyzeHook
    {
        private static readonly HashSet<string> SkipToolNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "data_analyze", "data_render", "ask_user",
            "propose_plan", "call_agent", "write_shared_context", "read_shared_context"
        };

        public static string MaybeCacheAndRewrite(AgentToolContext context, string toolType, string toolName, string result)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return result;
                if (string.IsNullOrWhiteSpace(result))
                    return result;
                if (!string.IsNullOrWhiteSpace(toolName) && SkipToolNames.Contains(toolName.Trim()))
                    return result;
                if (string.Equals(toolType, "PromptHint", StringComparison.OrdinalIgnoreCase))
                    return result;

                if (!TryExtractRowArray(result, out var rows) || rows == null || rows.Count == 0)
                    return result;

                // Tiny results: leave for the model; still optional to cache for analyze continuity.
                var ds = AgentDataAnalyzeCacheBL.Put(context.ChatSessionKey, toolName ?? toolType ?? "tool", rows);
                if (rows.Count < AgentDataAnalyzeCacheBL.AutoEnvelopeMinRows)
                {
                    // Annotate without stripping — model can still see small tables.
                    try
                    {
                        var token = JToken.Parse(result);
                        if (token is JObject obj)
                        {
                            obj["dataset_cached"] = true;
                            obj["dataset_name"] = ds.Name;
                            obj["analysis_hint"] =
                                $"Dataset cached as '{ds.Name}'. Use data_analyze(dataset_name='{ds.Name}', operation=...) for aggregations.";
                            return obj.ToString(Formatting.None);
                        }
                    }
                    catch { /* keep original */ }
                    return result;
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    dataset_cached = true,
                    dataset_name = ds.Name,
                    cache_scope = "chat_session",
                    source_tool = toolName ?? toolType,
                    rows = ds.Rows.Count,
                    truncated = rows.Count > AgentDataAnalyzeCacheBL.MaxCachedRows,
                    columns = ds.Columns.Select(c => c.Name).ToArray(),
                    analysis_hint =
                        $"Dataset cached. Use data_analyze with dataset_name='{ds.Name}' " +
                        $"(operations: summary|count|sum|avg|min|max|distinct|top|distribution). " +
                        "Then call data_render for visuals. Do not re-fetch the full table."
                }, Formatting.None);
            }
            catch
            {
                return result;
            }
        }

        /// <summary>
        /// Best-effort extract of a JSON array of row objects from common App tool shapes.
        /// </summary>
        public static bool TryExtractRowArray(string json, out JArray rows)
        {
            rows = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            var trimmed = json.Trim();
            if (trimmed.Length == 0 || trimmed[0] is not ('{' or '[')) return false;

            try
            {
                var token = JToken.Parse(trimmed);
                if (token is JArray rootArr && LooksLikeRowArray(rootArr))
                {
                    rows = rootArr;
                    return true;
                }

                if (token is not JObject obj) return false;

                // Prefer well-known keys used by SqlQuery / APIs / MCP.
                foreach (var key in new[] { "Rows", "rows", "data", "Data", "items", "Items", "result", "Result", "records", "Records" })
                {
                    if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var child)
                        && child is JArray arr
                        && LooksLikeRowArray(arr))
                    {
                        rows = arr;
                        return true;
                    }
                    // One-level nest: { result: { data: [...] } }
                    if (child is JObject nested)
                    {
                        foreach (var key2 in new[] { "Rows", "rows", "data", "Data", "items", "Items" })
                        {
                            if (nested.TryGetValue(key2, StringComparison.OrdinalIgnoreCase, out var inner)
                                && inner is JArray arr2
                                && LooksLikeRowArray(arr2))
                            {
                                rows = arr2;
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeRowArray(JArray arr)
        {
            if (arr == null || arr.Count == 0) return false;
            // Require at least one object row (tabular). Pure scalar arrays are not datasets.
            int objCount = 0;
            int check = Math.Min(arr.Count, 8);
            for (int i = 0; i < check; i++)
            {
                if (arr[i] is JObject) objCount++;
            }
            return objCount > 0 && objCount * 2 >= check; // majority objects in sample
        }
    }
}
