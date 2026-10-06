using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AppAI.Web.Services;

/// <summary>
/// OpenAPI document of AppAI's own controllers. The MCP gateway indexes it as the "AppAI" API source, and admins
/// then choose which operations external MCP users may call (dbo.AppMcpExposedApi). Generating the document
/// exposes nothing by itself: an operation is reachable through MCP only after an admin grants it to a group.
/// </summary>
public static class McpSwaggerSetup
{
    // Controllers that must never be offered to external MCP users, even to an admin choosing what to expose.
    private static readonly HashSet<string> ExcludedControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "McpManagement"
    };

    public static void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "AppAI API",
            Version     = "v1",
            Description = "AppAI REST API. Operations are exposed to external MCP clients only when granted to a security group."
        });

        options.CustomOperationIds(BaseOperationId);
        options.DocInclusionPredicate((_, api) => IsOfferable(api));
        // Same verb + path declared twice (overloads): keep the first rather than failing the whole document.
        options.ResolveConflictingActions(apis => apis.First());
        // Many DTOs share a short name across namespaces.
        options.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace('+', '.'));
        options.DocumentFilter<UniqueOperationIdFilter>();
    }

    // An action without an explicit [HttpGet]/[HttpPost]/... accepts any verb and cannot be described as one
    // operation (Swagger throws and the whole document fails), so it is not offered. Add a verb attribute to expose it.
    internal static bool IsOfferable(ApiDescription api) =>
        api.HttpMethod != null
        && (api.ActionDescriptor is not ControllerActionDescriptor cad || !ExcludedControllers.Contains(cad.ControllerName));

    internal static string BaseOperationId(ApiDescription api)
    {
        var routeValues = api.ActionDescriptor.RouteValues;
        routeValues.TryGetValue("controller", out var controller);
        routeValues.TryGetValue("action", out var action);
        return $"{controller}_{action}";
    }

    /// <summary>
    /// The gateway indexes operations by operationId and silently keeps only the first of two equal ids, which would
    /// make a grant ambiguous. Guarantees uniqueness by appending the HTTP method, then a counter, to repeats.
    /// Also drops a trailing "Async" because the gateway strips it when indexing, which could create new clashes.
    /// </summary>
    internal sealed class UniqueOperationIdFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in swaggerDoc.Paths.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var (method, operation) in path.Value.Operations.OrderBy(o => o.Key))
            {
                var id = operation.OperationId ?? "Operation";
                if (id.EndsWith("Async", StringComparison.Ordinal)) id = id[..^5];

                var candidate = id;
                if (!seen.Add(candidate))
                {
                    candidate = $"{id}_{method.ToString().ToUpperInvariant()}";
                    for (var n = 2; !seen.Add(candidate); n++)
                        candidate = $"{id}_{method.ToString().ToUpperInvariant()}{n}";
                }

                operation.OperationId = candidate;
            }
        }
    }
}
