using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using McpGateway.Models;
using McpGateway.Services;

namespace McpGateway.MCP.Tools;

/// <summary>
/// MCP tools for querying and exploring multi-source Swagger/OpenAPI specifications.
/// Supports PLM, ERP, SFC, and any other configured API sources.
/// </summary>
[McpServerToolType]
public class SwaggerTools
{
    /// <summary>
    /// Gets detailed information about a specific endpoint including parameters and request body schema.
    /// </summary>
    [McpServerTool(Name = "get_endpoint_details")]
    [Description("Gets full details for a specific API endpoint including app source, parameters, request body schema, and examples. Use the operationId from list_endpoints or search_endpoints.")]
    public static async Task<string> GetEndpointDetailsAsync(
        ISwaggerService swaggerService,
        IApiAccessPolicy accessPolicy,
        IAuditService auditService,
        [Description("The operationId of the endpoint (e.g., 'DataExchange_CreateStyle3dInfo')")]
        string operationId)
    {
        var endpoint = await swaggerService.GetEndpointByOperationIdAsync(operationId);

        // A denied endpoint is reported exactly like a missing one, so callers cannot probe which operationIds exist.
        var allowed = endpoint != null && await accessPolicy.IsAllowedAsync(endpoint.AppSource, endpoint.OperationId);
        if (endpoint != null && !allowed)
        {
            auditService.Log(AuditCode.A022_AccessDenied,
                $"Denied get_endpoint_details {endpoint.AppSource}/{endpoint.OperationId}",
                success: false,
                appSource: endpoint.AppSource,
                resourcePath: endpoint.Path);
        }

        if (endpoint == null || !allowed)
        {
            return JsonSerializer.Serialize(new { error = $"Endpoint with operationId '{operationId}' not found" });
        }

        var result = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["AppSource"]          = endpoint.AppSource,
            ["OperationId"]        = endpoint.OperationId,
            ["Method"]             = endpoint.Method,
            ["Path"]               = endpoint.Path,
            ["Summary"]            = endpoint.Summary,
            ["Description"]        = endpoint.Description,
            ["Tag"]                = endpoint.Tag,
            ["Parameters"]         = endpoint.Parameters,
            ["RequiresRequestBody"]= endpoint.RequiresRequestBody,
            ["RequestBodySchema"]  = endpoint.RequestBodySchema,
        };

        if (!string.IsNullOrEmpty(endpoint.RequestBodyExample))
            result["RequestBodyExample"] = endpoint.RequestBodyExample;

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Hybrid RAG search: combines semantic embedding similarity with keyword matching.
    /// </summary>
    [McpServerTool(Name = "semantic_search_endpoints")]
    [Description("Searches for API endpoints using natural-language intent. Combines semantic (embedding) similarity with keyword matching (hybrid RAG). Better than search_endpoints for synonyms and paraphrasing — e.g. 'show purchase history for a customer' finds GET /orders/customer/{id}. Returns top-N endpoint summaries; call get_endpoint_details on the chosen operationId for full parameters.")]
    public static async Task<string> SemanticSearchEndpointsAsync(
        ISwaggerService swaggerService,
        IApiAccessPolicy accessPolicy,
        [Description("Natural-language description of what you want to do (e.g. 'get purchase history for customer', 'create a new style', 'update inventory quantity')")]
        string intent,
        [Description("Optional app source to scope the search (e.g. 'PLM', 'ERP', 'SFC'). Leave empty to search all sources.")]
        string? app = null,
        [Description("Maximum number of results to return. Default: 5, max: 50.")]
        int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(intent))
            return JsonSerializer.Serialize(new { error = "Intent cannot be empty" });

        topK = Math.Clamp(topK, 1, 50);

        // Over-fetch, then keep only what the caller may use: filtering after the cut would starve the result list.
        var ranked = await swaggerService.HybridSearchAsync(intent, app, 200);
        var results = (await accessPolicy.FilterAsync(ranked, r => (r.Endpoint.AppSource, r.Endpoint.OperationId)))
            .Take(topK).ToList();

        var endpoints = results.Select(r => new
        {
            r.Endpoint.AppSource,
            r.Endpoint.OperationId,
            r.Endpoint.Method,
            r.Endpoint.Path,
            r.Endpoint.Summary,
            r.Endpoint.Tag,
            score = Math.Round(r.Score, 3),
            exactMatch = r.ExactMatch
        });

        return JsonSerializer.Serialize(new
        {
            intent,
            filterApp = app ?? "all",
            total = results.Count,
            hint = "Call get_endpoint_details with the chosen operationId to see full parameters before executing.",
            endpoints
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Browse endpoints by app source and/or tag with offset pagination.
    /// </summary>
    [McpServerTool(Name = "browse_endpoints")]
    [Description("Browse API endpoints filtered by app source and/or tag, with limit/offset pagination. Use this when semantic_search_endpoints doesn't find the right endpoint — systematically explore a tag's endpoints page by page. Returns summaries only; call get_endpoint_details for full parameters.")]
    public static async Task<string> BrowseEndpointsAsync(
        ISwaggerService swaggerService,
        IApiAccessPolicy accessPolicy,
        [Description("App source to filter by (e.g. 'PLM', 'ERP', 'SFC'). Leave empty for all sources.")]
        string? app = null,
        [Description("Tag/category to filter by. Leave empty to browse all tags in the chosen app.")]
        string? tag = null,
        [Description("Maximum number of results to return per page. Default: 50, max: 200.")]
        int limit = 50,
        [Description("Number of results to skip for pagination. Default: 0.")]
        int offset = 0)
    {
        limit  = Math.Clamp(limit, 1, 200);
        offset = Math.Max(offset, 0);

        IReadOnlyList<SwaggerEndpoint> all;
        if (!string.IsNullOrWhiteSpace(tag))
            all = await swaggerService.GetEndpointsByTagAsync(tag);
        else if (!string.IsNullOrWhiteSpace(app))
            all = await swaggerService.GetEndpointsByAppAsync(app);
        else
            all = await swaggerService.GetAllEndpointsAsync();

        // Apply app filter when browsing by tag (tag index is cross-source)
        if (!string.IsNullOrWhiteSpace(app) && !string.IsNullOrWhiteSpace(tag))
            all = all.Where(e => string.Equals(e.AppSource, app, StringComparison.OrdinalIgnoreCase)).ToList();

        all = await accessPolicy.FilterAsync(all, e => (e.AppSource, e.OperationId));

        var page = all.Skip(offset).Take(limit).Select(e => new
        {
            e.AppSource,
            e.OperationId,
            e.Method,
            e.Path,
            e.Summary,
            e.Tag
        });

        return JsonSerializer.Serialize(new
        {
            filterApp   = app    ?? "all",
            filterTag   = tag    ?? "all",
            totalCount  = all.Count,
            offset,
            limit,
            returned    = page.Count(),
            hasMore     = offset + limit < all.Count,
            nextOffset  = offset + limit < all.Count ? offset + limit : (int?)null,
            hint        = all.Count > limit + offset
                ? $"More results available. Call again with offset={offset + limit} to continue."
                : "All results returned.",
            endpoints   = page
        }, new JsonSerializerOptions { WriteIndented = true });
    }

}
