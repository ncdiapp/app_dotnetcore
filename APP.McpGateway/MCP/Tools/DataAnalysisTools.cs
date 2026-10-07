using System.ComponentModel;
using ModelContextProtocol.Server;
using McpGateway.Services;

namespace McpGateway.MCP.Tools;

[McpServerToolType]
public class DataAnalysisTools
{
    [McpServerTool(Name = "data_analyze")]
    [Description(
        "Analyzes a cached dataset from a previous api_execute call that returned a tabular ui_hint " +
        "(FlexGrid, SelectorGrid, PivotTable, ChartView). " +
        "Dataset name format: '{app}_{operationId}' (e.g. 'PLM_DataExchange_list_sales_orders'). " +
        "Use semantic_search_endpoints to find the operationId. " +
        "If the dataset is not yet cached, call api_execute first — it will auto-cache any tabular response.")]
    public static async Task<string> AnalyzeAsync(
        IAnalysisService analysisService,
        [Description("Dataset name in format '{app}_{operationId}'. Underscores replace any non-alphanumeric chars.")]
        string dataset_name,
        [Description("Analysis operation: summary | count | sum | avg | min | max | distinct | top | distribution")]
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
