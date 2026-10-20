using System.ComponentModel;
using ModelContextProtocol.Server;
using McpGateway.Services;

namespace McpGateway.MCP.Tools;

[McpServerToolType]
public class DataAnalysisTools
{
    [McpServerTool(Name = "data_analyze")]
    [Description(
        "GATED. Call ONLY when the user explicitly asks to analyze, aggregate, summarize metrics, or shape KPI/chart data. " +
        "Do NOT call for search, show, render, top-N display, or export. " +
        "Analyzes a cached dataset from a previous api_execute that returned a tabular ui_hint. " +
        "Dataset name format: '{app}_{operationId}'. If not cached, call api_execute first.")]
    public static async Task<string> AnalyzeAsync(
        IAnalysisService analysisService,
        [Description("Dataset name in format '{app}_{operationId}'. Underscores replace any non-alphanumeric chars.")]
        string dataset_name,
        [Description("Analysis operation: summary | count | sum | avg | min | max | distinct | top | distribution — must match the user ask")]
        string operation,
        [Description("Target column name. Required for: sum, avg, min, max, distinct, top, distribution.")]
        string? column = null,
        [Description("Group results by this column (optional, applies to: count, sum, avg, min, max).")]
        string? group_by = null,
        [Description("Filter substring — only rows containing this value are included.")]
        string? filter = null,
        [Description("Column to scope the filter to. If omitted, all columns are searched.")]
        string? filter_column = null,
        [Description("Maximum rows/groups to return (default 20).")]
        int limit = 20,
        [Description("Sort direction: asc | desc (default desc).")]
        string? order_by = null)
    {
        return await analysisService.AnalyzeAsync(
            dataset_name, operation, column, group_by, filter, filter_column, limit, order_by);
    }
}
