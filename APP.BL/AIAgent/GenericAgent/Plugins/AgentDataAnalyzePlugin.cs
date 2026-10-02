using System;
using System.Globalization;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn data_analyze — deterministic aggregations over ChatSession-cached tabular data
    /// (or inline dataJson). Pair with data_render for UI; do not dump large tables into FinalResponse.
    /// </summary>
    public class AgentDataAnalyzePlugin
    {
        public Task<string> Analyze(
            AgentToolContext context,
            string operation,
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
    }
}
