using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn data_analyze — gated aggregations over ChatSession-cached tabular data.
    /// Call only when the user asked to analyze/aggregate/shape metrics — not for show/render/export.
    /// </summary>
    public class AgentDataAnalyzePlugin
    {
        public Task<string> Analyze(
            AgentToolContext context,
            string operation,
            string user_ask,
            string dataset_name = null,
            string dataJson = null,
            string column = null,
            string group_by = null,
            string filter = null,
            string filter_column = null,
            string limit = null,
            string order_by = null)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ChatSessionKey is required for data_analyze."
                }));
            }

            var gate = ValidateUserAsk(user_ask);
            if (gate != null)
                return Task.FromResult(JsonConvert.SerializeObject(new { ok = false, error = gate, skipped = true }));

            if (context.IsDeterministic != true)
            {
                if (AgentUiPathConfirmBL.IsFetchDisplayOnly(context.ChatSessionKey))
                {
                    return Task.FromResult(JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        skipped = true,
                        error =
                            "User chose fetch-and-display only. Do not call data_analyze. " +
                            "Use render_none_interactive / export_* for the data the user asked to show."
                    }));
                }
                if (!AgentUiPathConfirmBL.IsKpiDashboardConfirmed(context.ChatSessionKey))
                {
                    return Task.FromResult(JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        skipped = true,
                        error =
                            "Analysis-like work needs user confirmation first. Call ask_user with contextKey='" +
                            AgentUiPathConfirmBL.ContextKey + "', mode=single_choice, prompt='" +
                            AgentUiPathConfirmBL.AskUserPromptHint() + "', optionsJson=" +
                            AgentUiPathConfirmBL.AskUserOptionsJson() + ". " +
                            "After kpi_dashboard: call data_analyze (count/distribution) then render layout=dashboard with kpis+chart. " +
                            "After fetch_display: only fetch and show/export — no data_analyze."
                    }));
                }
            }

            if (string.IsNullOrWhiteSpace(operation))
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "operation is required: summary|count|sum|avg|min|max|distinct|top|distribution"
                }));
            }

            int lim = 20;
            if (!string.IsNullOrWhiteSpace(limit)
                && int.TryParse(limit.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                && parsed > 0)
                lim = Math.Min(parsed, 500);

            AgentCachedDataset dataset;

            if (!string.IsNullOrWhiteSpace(dataset_name))
            {
                dataset = AgentDataAnalyzeCacheBL.Get(context.ChatSessionKey, dataset_name.Trim());
                if (dataset == null)
                {
                    var names = AgentDataAnalyzeCacheBL.ListNames(context.ChatSessionKey);
                    return Task.FromResult(JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"Dataset '{dataset_name}' not found in this chat session. " +
                                (names.Count == 0
                                    ? "Run a query/API/MCP tool that returns tabular rows first (auto-cached)."
                                    : $"Available: {string.Join(", ", names)}")
                    }));
                }
            }
            else if (!string.IsNullOrWhiteSpace(dataJson))
            {
                try
                {
                    if (!AgentDataAnalyzeHook.TryExtractRowArray(dataJson, out var rows) || rows == null)
                    {
                        var token = JToken.Parse(dataJson);
                        rows = token as JArray;
                    }
                    if (rows == null || rows.Count == 0)
                    {
                        return Task.FromResult(JsonConvert.SerializeObject(new
                        {
                            ok = false,
                            error = "dataJson must be a JSON array of row objects (or {Rows|data: [...]})."
                        }));
                    }
                    dataset = AgentDataAnalyzeCacheBL.PutNamed(
                        context.ChatSessionKey, "inline_data", rows, "dataJson");
                }
                catch (Exception ex)
                {
                    return Task.FromResult(JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "Invalid dataJson: " + ex.Message
                    }));
                }
            }
            else
            {
                var names = AgentDataAnalyzeCacheBL.ListNames(context.ChatSessionKey);
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "Provide dataset_name (preferred) or dataJson. " +
                            (names.Count == 0
                                ? "No cached datasets in this chat yet."
                                : $"Cached: {string.Join(", ", names)}")
                }));
            }

            var json = AgentDataAnalyzeService.Analyze(
                dataset, operation, column, group_by, filter, filter_column, lim, order_by);
            return Task.FromResult(json);
        }

        /// <summary>
        /// Returns an error message when the call is not justified by user intent; null when OK.
        /// </summary>
        public static string ValidateUserAsk(string userAsk)
        {
            if (string.IsNullOrWhiteSpace(userAsk) || userAsk.Trim().Length < 4)
            {
                return "data_analyze is gated. Pass user_ask with the user's words that request analysis/aggregation/KPI/chart shaping. " +
                       "If the user only asked to search, show, render, or export rows, do not call this tool — use render_* / export_* with limit.";
            }

            var s = userAsk.Trim().ToLowerInvariant();
            if (LooksLikeShowOrExportOnly(s))
            {
                return "user_ask looks like show/render/export (or top-N display), not analysis. " +
                       "Use render_none_interactive / export_* with dataset_name and limit. Do not call data_analyze.";
            }

            return null;
        }

        private static bool LooksLikeShowOrExportOnly(string s)
        {
            if (HasAnalyzeIntent(s))
                return false;

            if (Regex.IsMatch(s, @"\b(export|render|powerpoint|pptx|docx|pdf|excel|xlsx|html|card view|cards per|per row|per page)\b"))
                return true;

            // "top 20 results" / "first N" without analyze words = display slice
            if (Regex.IsMatch(s, @"\b(top|first)\s+\d+\b") || Regex.IsMatch(s, @"\blimit\s*=?\s*\d+\b"))
                return true;

            if (Regex.IsMatch(s, @"\b(show|display|list|search|grid)\b")
                && !HasAnalyzeIntent(s))
                return true;

            return false;
        }

        private static bool HasAnalyzeIntent(string s)
        {
            return Regex.IsMatch(s,
                @"\b(analy[sz]|aggregat|summar|average|\bavg\b|\bsum\b|total|kpi|dashboard|distribution|break\s*down|breakdown|group(?:ed)?\s*by|how many|metric|trend|chart|histogram|percentile|correlate)\b")
                || Regex.IsMatch(s, @"\bby\s+[a-z][a-z0-9_ /-]{1,40}\b");
        }

    }
}
