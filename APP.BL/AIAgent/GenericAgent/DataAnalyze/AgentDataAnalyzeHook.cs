using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.AIAgent.GenericAgent.Documents;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.DataAnalyze
{
    /// <summary>
    /// Intercepts tabular tool results (SqlQuery / HttpRest / BuiltIn / MCP / …).
    /// <para>
    /// <b>Token rule:</b> tools (cache / render / analyze) keep full rows up to
    /// <see cref="AgentDataAnalyzeCacheBL.MaxCachedRows"/>. What the LLM sees is capped at
    /// <see cref="AgentDataAnalyzeCacheBL.LlmPreviewMaxRows"/> (envelope + sample).
    /// </para>
    /// </summary>
    public static class AgentDataAnalyzeHook
    {
        private static readonly HashSet<string> SkipToolNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "data_analyze", "data_render", "render_none_interactive", "render_interactive", "ask_user",
            "propose_plan", "call_agent", "write_shared_context", "read_shared_context"
        };

        public static string MaybeCacheAndRewrite(
            AgentToolContext context,
            string toolType,
            string toolName,
            string result,
            IReadOnlyDictionary<string, string> toolArgs = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(result))
                    return result;
                if (!string.IsNullOrWhiteSpace(toolName) && SkipToolNames.Contains(toolName.Trim()))
                    return result;
                if (string.Equals(toolType, "PromptHint", StringComparison.OrdinalIgnoreCase))
                    return result;

                if (!TryExtractRowArray(result, out var rows) || rows == null || rows.Count == 0)
                    return result;

                // No chat session → cannot cache for tools; still protect the LLM with a 20-row preview.
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return TruncateRowsForLlm(result, rows);

                var displayLimit = AgentDisplayRowLimitBL.ResolveForCache(
                    context.ChatSessionKey,
                    toolName ?? toolType ?? "tool",
                    toolArgs,
                    result);

                var ds = AgentDataAnalyzeCacheBL.Put(
                    context.ChatSessionKey,
                    toolName ?? toolType ?? "tool",
                    rows,
                    displayLimit);

                // LLM path: ≤20 rows → annotate and keep rows; >20 → envelope + sample only.
                if (ds.Rows.Count <= AgentDataAnalyzeCacheBL.LlmPreviewMaxRows)
                {
                    try
                    {
                        var token = JToken.Parse(result);
                        if (token is JObject obj)
                        {
                            ApplyCacheAnnotations(obj, ds);
                            // Ensure Rows in the LLM payload matches cache (already ≤ preview).
                            if (obj["Rows"] != null || obj["rows"] != null)
                                ReplaceRowArray(obj, TakeSample(rows, ds.Rows.Count));
                            return obj.ToString(Formatting.None);
                        }
                    }
                    catch { /* fall through */ }
                    return result;
                }

                return BuildLlmEnvelope(ds, toolName ?? toolType, rows);
            }
            catch
            {
                return result;
            }
        }

        /// <summary>LLM-facing envelope: totals + columns + first N sample rows. Full data is in cache.</summary>
        public static string BuildLlmEnvelope(AgentCachedDataset ds, string sourceTool, JArray sourceRows)
        {
            var sample = TakeSample(sourceRows, AgentDataAnalyzeCacheBL.LlmPreviewMaxRows);
            var displayLimit = ds.DisplayRowLimit;
            var displayRows = displayLimit is > 0
                ? Math.Min(ds.Rows.Count, displayLimit.Value)
                : ds.Rows.Count;
            return JsonConvert.SerializeObject(new
            {
                ok = true,
                dataset_cached = true,
                dataset_name = ds.Name,
                cache_scope = "chat_session",
                source_tool = sourceTool,
                rows = ds.Rows.Count,
                totalRows = ds.Rows.Count,
                display_row_limit = displayLimit,
                display_rows = displayRows,
                truncated_for_llm = true,
                llm_preview_rows = sample.Count,
                sample_rows = sample,
                columns = ds.Columns.Select(c => c.Name).ToArray(),
                next_hint = BuildNextHint(ds)
            }, Formatting.None);
        }

        /// <summary>
        /// When there is no ChatSessionKey, still cap Rows in the JSON for the LLM.
        /// Preserves RowCount / total when present.
        /// </summary>
        public static string TruncateRowsForLlm(string result, JArray rows)
        {
            if (rows == null || rows.Count <= AgentDataAnalyzeCacheBL.LlmPreviewMaxRows)
                return result;
            try
            {
                var token = JToken.Parse(result);
                if (token is JArray)
                {
                    return TakeSample(rows, AgentDataAnalyzeCacheBL.LlmPreviewMaxRows).ToString(Formatting.None);
                }
                if (token is JObject obj)
                {
                    if (obj["RowCount"] == null && obj["rowCount"] == null && obj["totalRows"] == null)
                        obj["RowCount"] = rows.Count;
                    obj["ReturnedRows"] = AgentDataAnalyzeCacheBL.LlmPreviewMaxRows;
                    obj["truncated_for_llm"] = true;
                    obj["llm_preview_rows"] = AgentDataAnalyzeCacheBL.LlmPreviewMaxRows;
                    ReplaceRowArray(obj, TakeSample(rows, AgentDataAnalyzeCacheBL.LlmPreviewMaxRows));
                    return obj.ToString(Formatting.None);
                }
            }
            catch { /* keep original */ }
            return result;
        }

        private static void ApplyCacheAnnotations(JObject obj, AgentCachedDataset ds)
        {
            obj["dataset_cached"] = true;
            obj["dataset_name"] = ds.Name;
            obj["next_hint"] = BuildNextHint(ds);
        }

        private static string BuildNextHint(AgentCachedDataset ds)
        {
            var displayNote = ds.DisplayRowLimit is > 0
                ? $" Render limit={ds.DisplayRowLimit.Value} (render_interactive limit={ds.DisplayRowLimit.Value}; tell user {Math.Min(ds.Rows.Count, ds.DisplayRowLimit.Value)} rows shown)."
                : "";
            return
                $"Dataset cached as '{ds.Name}' ({ds.Rows.Count} rows — full data for tools; LLM only sees up to {AgentDataAnalyzeCacheBL.LlmPreviewMaxRows} sample rows).{displayNote} " +
                "MUST for analysis-like asks (group-by / ranges / multi-part metrics): ask_user contextKey=confirm_kpi_dashboard FIRST (bias kpi_dashboard). " +
                "After kpi_dashboard: data_analyze (count|distribution|group_by) on raw caches for each user dimension, then render_interactive layout=dashboard. " +
                "If SQL already GROUP BY'd with TotalCount|Count, skip redundant summary — render with chart.yField=measure. " +
                "Do NOT answer with FinalResponse tables alone. Grid: render_interactive dataset_name='" + ds.Name + "' with limit matching display_row_limit when set. " +
                "fetch_display: render table|cards only, no data_analyze.";
        }

        private static JArray TakeSample(JArray rows, int n)
        {
            var sample = new JArray();
            if (rows == null || n <= 0) return sample;
            var take = Math.Min(n, rows.Count);
            for (int i = 0; i < take; i++)
                sample.Add(rows[i]);
            return sample;
        }

        private static void ReplaceRowArray(JObject obj, JArray sample)
        {
            foreach (var key in new[] { "Rows", "rows", "data", "Data", "items", "Items", "result", "Result", "records", "Records" })
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var child) && child is JArray)
                {
                    obj[key] = sample;
                    return;
                }
                if (child is JObject nested)
                {
                    foreach (var key2 in new[] { "Rows", "rows", "data", "Data", "items", "Items" })
                    {
                        if (nested.TryGetValue(key2, StringComparison.OrdinalIgnoreCase, out var inner) && inner is JArray)
                        {
                            nested[key2] = sample;
                            return;
                        }
                    }
                }
            }
            obj["Rows"] = sample;
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

                // Prefer the largest object-array — small metadata lists (e.g. column defs) must not
                // win over the real 600-row grid when both appear under different keys.
                JArray best = null;
                foreach (var key in new[] { "Rows", "rows", "data", "Data", "items", "Items", "result", "Result", "records", "Records" })
                {
                    if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var child)
                        && child is JArray arr
                        && LooksLikeRowArray(arr)
                        && (best == null || arr.Count > best.Count))
                    {
                        best = arr;
                    }
                    if (child is JObject nested)
                    {
                        foreach (var key2 in new[] { "Rows", "rows", "data", "Data", "items", "Items" })
                        {
                            if (nested.TryGetValue(key2, StringComparison.OrdinalIgnoreCase, out var inner)
                                && inner is JArray arr2
                                && LooksLikeRowArray(arr2)
                                && (best == null || arr2.Count > best.Count))
                            {
                                best = arr2;
                            }
                        }
                    }
                }

                if (best != null)
                {
                    rows = best;
                    return true;
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
            int objCount = 0;
            int check = Math.Min(arr.Count, 8);
            for (int i = 0; i < check; i++)
            {
                if (arr[i] is JObject) objCount++;
            }
            return objCount > 0 && objCount * 2 >= check;
        }
    }
}
