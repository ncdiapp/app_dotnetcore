using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Multi-source HTTP client. Routes each request to the correct backend
/// by matching appSource to ApiSources:Sources[].Name.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly IReadOnlyDictionary<string, ApiSourceConfig> _sources;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string CallerTokenHeader = "IntergrationAccessToken";

    private readonly IMcpCallerContext _callerContext;
    private readonly ILogger<ApiClient> _logger;
    private readonly IAuditService _auditService;

    public ApiClient(
        IOptions<MultiSourceApiSettings> settings,
        IHttpClientFactory httpClientFactory,
        IMcpCallerContext callerContext,
        ILogger<ApiClient> logger,
        IAuditService auditService)
    {
        _httpClientFactory = httpClientFactory;
        _callerContext = callerContext;
        _logger = logger;
        _auditService = auditService;

        _sources = settings.Value.Sources
            .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ApiResponse> ExecuteAsync(
        string appSource,
        string method,
        string path,
        Dictionary<string, string>? queryParams = null,
        Dictionary<string, string>? headers = null,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        if (!_sources.TryGetValue(appSource, out var source))
        {
            var available = string.Join(", ", _sources.Keys);
            return new ApiResponse
            {
                StatusCode = 400,
                Body = "",
                IsSuccess = false,
                ErrorMessage = $"Unknown app source '{appSource}'. Configured sources: {available}"
            };
        }

        // The caller's own token is forwarded; there is no shared or stored credential for calls.
        string? authToken = null;
        if (source.ForwardCallerToken)
        {
            authToken = GetCallerToken();
            if (string.IsNullOrEmpty(authToken))
            {
                return new ApiResponse
                {
                    StatusCode = 401,
                    Body = JsonSerializer.Serialize(new { error = $"{CallerTokenHeader} required for {appSource}" }),
                    IsSuccess = false,
                    ErrorMessage = "Caller token missing"
                };
            }
        }

        try
        {
            var url = BuildUrl(source.BaseUrl, path, queryParams);

            // Use the named client so the per-source resilience pipeline (retry + circuit breaker)
            // is applied. Timeout is enforced by the Polly pipeline, not HttpClient.Timeout.
            using var client = _httpClientFactory.CreateClient(source.Name);

            using var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.TryAddWithoutValidation(source.AccessTokenHeaderName, authToken);

            if (headers != null)
                foreach (var (key, value) in headers)
                    request.Headers.TryAddWithoutValidation(key, value);

            if (!string.IsNullOrEmpty(body))
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            _logger.LogInformation("[{App}] {Method} {Url}", appSource, method, url);

            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var httpStatus = (int)response.StatusCode;
            var isSuccess = response.IsSuccessStatusCode;
            var methodUpper = method.ToUpperInvariant();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                _logger.LogWarning("[{App}] 401 received — token expired, revoked or not valid for this source", appSource);

            // A021 — catch-all for all API calls
            _auditService.Log(AuditCode.A021_ApiCrudAction,
                $"API {method} {appSource}{path}",
                success: isSuccess,
                appSource: appSource,
                httpMethod: method,
                resourcePath: path,
                httpStatus: httpStatus,
                errorMessage: isSuccess ? null : response.ReasonPhrase);

            // Method-specific codes
            if (methodUpper == "GET")
                _auditService.Log(AuditCode.A002_DataAccess,
                    $"Data access via GET {appSource}{path}",
                    success: isSuccess,
                    appSource: appSource,
                    httpMethod: method,
                    resourcePath: path,
                    httpStatus: httpStatus,
                    errorMessage: isSuccess ? null : response.ReasonPhrase);
            else if (methodUpper is "POST" or "PUT")
                _auditService.Log(AuditCode.A001_DataChange,
                    $"Data change via {method} {appSource}{path}",
                    success: isSuccess,
                    appSource: appSource,
                    httpMethod: method,
                    resourcePath: path,
                    httpStatus: httpStatus,
                    errorMessage: isSuccess ? null : response.ReasonPhrase);
            else if (methodUpper == "DELETE")
                _auditService.Log(AuditCode.A007_DataDeletion,
                    $"Data deletion via DELETE {appSource}{path}",
                    success: isSuccess,
                    appSource: appSource,
                    httpMethod: method,
                    resourcePath: path,
                    httpStatus: httpStatus,
                    errorMessage: isSuccess ? null : response.ReasonPhrase);

            return new ApiResponse
            {
                StatusCode = httpStatus,
                Body = responseBody,
                IsSuccess = isSuccess,
                ErrorMessage = isSuccess ? null : response.ReasonPhrase
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{App}] Error executing {Method} {Path}", appSource, method, path);

            _auditService.Log(AuditCode.A021_ApiCrudAction,
                $"API {method} {appSource}{path} threw exception",
                success: false,
                appSource: appSource,
                httpMethod: method,
                resourcePath: path,
                httpStatus: 500,
                errorMessage: ex.Message);

            return new ApiResponse
            {
                StatusCode = 500,
                Body = "",
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string? GetCallerToken() => _callerContext.Token;

    private static string BuildUrl(string baseUrl, string path, Dictionary<string, string>? queryParams)
    {
        var fullUrl = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

        if (queryParams == null || queryParams.Count == 0)
            return fullUrl;

        var queryString = string.Join("&",
            queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{fullUrl}?{queryString}";
    }
}
