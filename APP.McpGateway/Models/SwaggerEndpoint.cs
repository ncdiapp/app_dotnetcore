namespace McpGateway.Models;

/// <summary>
/// Represents a parsed API endpoint from a Swagger/OpenAPI specification.
/// </summary>
public record SwaggerEndpoint
{
    public required string Path { get; init; }
    public required string Method { get; init; }
    public required string OperationId { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public string? Tag { get; init; }
    public List<EndpointParameter> Parameters { get; init; } = [];
    public string? RequestBodySchema { get; init; }
    public string? RequestBodyExample { get; init; }
    public bool RequiresRequestBody { get; init; }
    public string AppSource { get; init; } = "";

    // ── LLM-enriched fields (populated when LlmEnrichment:Enabled = true) ──

    /// <summary>
    /// LLM-generated 3-4 sentence agent instruction covering when, context, caution, and format.
    /// Null when enrichment is disabled or the endpoint was not enriched.
    /// </summary>
    public string? AgentInstructions { get; init; }

    /// <summary>
    /// LLM-generated system note for the agent: edge cases, call ordering, rate limits.
    /// Null when enrichment is disabled or the endpoint was not enriched.
    /// </summary>
    public string? PromptHelp { get; init; }
}

/// <summary>
/// Represents a parameter for an API endpoint.
/// </summary>
public record EndpointParameter
{
    public required string Name { get; init; }
    public required string In { get; init; } // query, header, path
    public string? Type { get; init; }
    public string? Description { get; init; }
    public bool Required { get; init; }
    public string? Example { get; init; }
}

/// <summary>
/// Configuration for a single named API source (PLM, ERP, SFC, etc.).
/// </summary>
public class ApiSourceConfig
{
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string SwaggerJsonPath { get; set; } = "swagger.json";
    /// <summary>
    /// Service credential used ONLY to download the Swagger spec at discovery time (no caller exists then).
    /// It is never sent on API calls, so calls can never run as a shared identity.
    /// </summary>
    public string? AccessTokenValue { get; set; }
    public string AccessTokenHeaderName { get; set; } = "IntergrationAccessToken";

    /// <summary>
    /// When true, the calling external user's IntergrationAccessToken is forwarded in
    /// <see cref="AccessTokenHeaderName"/> on every call to this source. Enable only for sources that are this
    /// AppAI instance (they recognise the token); never for third-party hosts, which would just receive a credential.
    /// </summary>
    public bool ForwardCallerToken { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// OperationIds whose API responses are shared by all users of ONE company (e.g. country list, product statuses).
    /// They are cached on first call, in a per-company on-disk + in-memory cache (never across companies).
    /// All other DataAnalysis responses use a per-user memory-only cache.
    /// </summary>
    public List<string> GlobalDatasetOperationIds { get; set; } = [];
}

/// <summary>
/// Result of a hybrid (RAG + inverted-index) endpoint search.
/// </summary>
public record HybridSearchResult(
    SwaggerEndpoint Endpoint,
    float Score,
    bool ExactMatch);

/// <summary>
/// Top-level multi-source configuration. Bound to the "ApiSources" section.
/// </summary>
public class MultiSourceApiSettings
{
    public const string SectionName = "ApiSources";

    public List<ApiSourceConfig> Sources { get; set; } = [];
    public bool StatelessMode { get; set; } = false;
}
