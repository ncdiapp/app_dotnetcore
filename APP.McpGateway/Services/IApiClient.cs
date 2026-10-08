namespace McpGateway.Services;

/// <summary>
/// Generic HTTP client for forwarding requests to any configured API source (PLM, ERP, SFC, etc.).
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Executes an API call to the named source backend.
    /// </summary>
    /// <param name="appSource">Source name matching ApiSources:Sources[].Name (e.g., "PLM", "ERP", "SFC")</param>
    /// <param name="method">HTTP method (GET, POST, PUT, DELETE)</param>
    /// <param name="path">API path (relative to source BaseUrl)</param>
    /// <param name="queryParams">Query parameters</param>
    /// <param name="headers">Additional HTTP headers</param>
    /// <param name="body">Request body (for POST/PUT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ApiResponse> ExecuteAsync(
        string appSource,
        string method,
        string path,
        Dictionary<string, string>? queryParams = null,
        Dictionary<string, string>? headers = null,
        string? body = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from an API source.
/// </summary>
public record ApiResponse
{
    public required int StatusCode { get; init; }
    public required string Body { get; init; }
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
