using System.ComponentModel;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using McpGateway.Models;
using McpGateway.Services;

namespace McpGateway.MCP.Tools;

/// <summary>
/// MCP tools for executing API calls against any configured source (PLM, ERP, SFC, etc.).
/// Use summarize_api or search_endpoints first to find the right operationId.
/// </summary>
[McpServerToolType]
public class ApiTools
{
    [McpServerTool(Name = "api_execute")]
    [Description("Executes any API endpoint by operationId. Automatically determines the HTTP method and routes to the correct app source (PLM, ERP, SFC) based on which system the endpoint belongs to.")]
    public static async Task<string> ExecuteByOperationIdAsync(
        IApiClient apiClient,
        ISwaggerService swaggerService,
        IDataAnalysisCacheService analysisCacheService,
        IOptions<MultiSourceApiSettings> apiSettings,
        IApiAccessPolicy accessPolicy,
        IAuditService auditService,
        ILogger<ApiTools> logger,
        [Description("The operationId of the endpoint to execute (from list_endpoints or search_endpoints)")]
        string operationId,
        [Description("Query parameters as JSON object. Optional.")]
        string? queryParamsJson = null,
        [Description("Request body as JSON string (for POST/PUT). Optional.")]
        string? body = null,
        [Description("Additional headers as JSON object. Optional.")]
        string? headersJson = null)
    {
        var endpoint = await swaggerService.GetEndpointByOperationIdAsync(operationId);

        // A denied endpoint is reported exactly like a missing one, so callers cannot probe which operationIds exist.
        var allowed = endpoint != null && await accessPolicy.IsAllowedAsync(endpoint.AppSource, endpoint.OperationId);
        if (endpoint != null && !allowed)
        {
            auditService.Log(AuditCode.A022_AccessDenied,
                $"Denied api_execute {endpoint.AppSource}/{endpoint.OperationId}",
                success: false,
                appSource: endpoint.AppSource,
                httpMethod: endpoint.Method,
                resourcePath: endpoint.Path);
        }

        if (endpoint == null || !allowed)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Endpoint with operationId '{operationId}' not found",
                suggestion = "Use list_endpoints or search_endpoints to find valid operationIds"
            });
        }

        var response = await apiClient.ExecuteAsync(
            endpoint.AppSource,
            endpoint.Method,
            endpoint.Path,
            ParseJsonToDict(queryParamsJson, logger),
            ParseJsonToDict(headersJson, logger),
            body);

        // Intercept DataAnalysis responses before generic formatting
        if (response.IsSuccess)
        {
            try
            {
                var jsonBody = JsonSerializer.Deserialize<JsonElement>(response.Body);
                if (IsDataAnalysisHint(jsonBody)
                    && jsonBody.TryGetProperty("data", out var dataEl)
                    && dataEl.ValueKind == JsonValueKind.Array)
                {
                    bool isGlobal = IsGlobalDataset(apiSettings.Value, endpoint.AppSource, operationId);
                    return await FormatDataAnalysisResponseAsync(
                        analysisCacheService, logger, response.StatusCode,
                        endpoint.AppSource, endpoint.OperationId, jsonBody, dataEl, isGlobal);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "DataAnalysis hint check failed for '{OperationId}' — using standard formatting", operationId);
            }
        }

        return FormatResponse(response, endpoint.AppSource, endpoint.OperationId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsGlobalDataset(MultiSourceApiSettings settings, string? appSource, string operationId)
    {
        var source = settings.Sources.FirstOrDefault(s =>
            s.Name.Equals(appSource, StringComparison.OrdinalIgnoreCase));
        return source?.GlobalDatasetOperationIds.Contains(operationId, StringComparer.OrdinalIgnoreCase) == true;
    }

    // Tabular ui_hints whose responses contain a 'data' array suitable for data_analyze.
    // FlexGrid(4), SelectorGrid(1), PivotTable(8), ChartView(7) come from the PLM REST API.
    // DataAnalysis(9) is the synthetic value written by this gateway on cache responses.
    private static readonly HashSet<string> _tabularHintStrings =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "DataAnalysis", "FlexGrid", "SelectorGrid", "PivotTable", "ChartView"
        };

    private static readonly HashSet<int> _tabularHintInts = new()
    {
        (int)EmAiUiHint.DataAnalysis,   // 9
        (int)EmAiUiHint.FlexGrid,       // 4
        (int)EmAiUiHint.SelectorGrid,   // 1
        (int)EmAiUiHint.PivotTable,     // 8
        (int)EmAiUiHint.ChartView,      // 7
    };

    private static bool IsDataAnalysisHint(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object) return false;
        foreach (var propName in new[] { "ui_hint", "ui-hint" })
        {
            if (!body.TryGetProperty(propName, out var hint)) continue;
            if (hint.ValueKind == JsonValueKind.String)
                return _tabularHintStrings.Contains(hint.GetString() ?? "");
            if (hint.ValueKind == JsonValueKind.Number && hint.TryGetInt32(out var n))
                return _tabularHintInts.Contains(n);
        }
        return false;
    }

    private static async Task<string> FormatDataAnalysisResponseAsync(
        IDataAnalysisCacheService cacheService,
        ILogger<ApiTools> logger,
        int statusCode, string? app, string? operationId,
        JsonElement fullBody, JsonElement dataArray, bool isGlobal = false)
    {
        try
        {
            var dataset = await cacheService.SyncFromJsonAsync(app ?? "", operationId ?? "", dataArray, isGlobal);
            var result = new
            {
                success        = true,
                statusCode,
                app,
                operationId,
                ui_hint        = "DataAnalysis",
                dataset_cached = true,
                dataset_name   = dataset.Name,
                cache_scope    = isGlobal ? "global" : "session",
                rows           = dataset.Rows.Count,
                columns        = dataset.Columns.Select(c => c.Name).ToArray(),
                analysis_hint  = $"Dataset cached. Use data_analyze tool with dataset_name='{dataset.Name}' to analyze."
            };
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DataAnalysis cache failed for '{App}/{OperationId}' — returning raw payload", app, operationId);
            return FlattenUiPayloadObject(statusCode, app, operationId, fullBody);
        }
    }

    private static Dictionary<string, string>? ParseJsonToDict(string? json, ILogger<ApiTools>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            return dict?.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Could not parse JSON parameters '{Json}' — parameters will be omitted", json);
            return null;
        }
    }

    private static string FormatResponse(ApiResponse response, string? app = null, string? operationId = null)
    {
        object result;

        if (response.IsSuccess)
        {
            try
            {
                var jsonBody = JsonSerializer.Deserialize<JsonElement>(response.Body);

                // Only flatten responses that look like our UI payload contract:
                // a JSON object with BOTH `ui_hint` (or `ui-hint`) and `data` at the root level.
                if (jsonBody.ValueKind == JsonValueKind.Object
                    && (jsonBody.TryGetProperty("ui_hint", out _) || jsonBody.TryGetProperty("ui-hint", out _))
                    && jsonBody.TryGetProperty("data", out _))
                {
                    return FlattenUiPayloadObject(response.StatusCode, app, operationId, jsonBody);
                }

                result = new { success = true, statusCode = response.StatusCode, app, operationId, data = jsonBody };
            }
            catch
            {
                result = new { success = true, statusCode = response.StatusCode, app, operationId, data = response.Body };
            }
        }
        else
        {
            result = new { success = false, statusCode = response.StatusCode, app, operationId, error = response.ErrorMessage, body = response.Body };
        }

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string FlattenUiPayloadObject(int statusCode, string? app, string? operationId, JsonElement backendObject)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteBoolean("success", true);
            writer.WriteNumber("statusCode", statusCode);

            if (!string.IsNullOrWhiteSpace(app))
                writer.WriteString("app", app);

            if (!string.IsNullOrWhiteSpace(operationId))
                writer.WriteString("operationId", operationId);

            foreach (var prop in backendObject.EnumerateObject())
            {
                // Prevent backend from overriding our envelope keys.
                if (prop.NameEquals("success")
                    || prop.NameEquals("statusCode")
                    || prop.NameEquals("app")
                    || prop.NameEquals("operationId"))
                {
                    continue;
                }

                writer.WritePropertyName(prop.Name);
                prop.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
