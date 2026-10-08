using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using McpGateway.Models;
using McpGateway.Services;

namespace McpGateway.Controllers;

/// <summary>
/// REST management API for the MCP Gateway.
/// Exposes runtime configuration and cache controls — not accessible from MCP tools.
/// </summary>
// DISABLED: this API has no authentication (the old X-Api-Key guard never covered /api/management) and can
// rewrite appsettings.json. [NonController] keeps MVC from routing it until it is replaced by an admin-only
// management controller in AppAI.Web (see Document/mcp/mcp-gateway-merge-plan.md, phases 3-4).
[NonController]
[ApiController]
[Route("api/management")]
[Produces("application/json")]
public class McpServerManagerController : ControllerBase
{
    private readonly IRuntimeConfigService _runtimeConfig;
    private readonly ISwaggerService _swaggerService;
    private readonly MultiSourceApiSettings _sourceSettings;
    private readonly IAuditService _auditService;

    public McpServerManagerController(
        IRuntimeConfigService runtimeConfig,
        ISwaggerService swaggerService,
        IOptions<MultiSourceApiSettings> sourceSettings,
        IAuditService auditService)
    {
        _runtimeConfig = runtimeConfig;
        _swaggerService = swaggerService;
        _sourceSettings = sourceSettings.Value;
        _auditService = auditService;
    }

    /// <summary>
    /// Returns the current runtime configuration of the MCP Gateway.
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ConfigResponse), 200)]
    public IActionResult GetConfig()
    {
        _auditService.Log(AuditCode.A006_PrivilegedAction,
            "REST admin read gateway config",
            appSource: "gateway");

        var status = _runtimeConfig.GetStatus();
        var sources = _sourceSettings.Sources.Select(s => new
        {
            s.Name,
            s.BaseUrl,
            s.SwaggerJsonPath,
            s.TimeoutSeconds
        }).ToList();

        return Ok(new ConfigResponse(
            LlmEnrichment: new LlmEnrichmentStatus(
                Enabled: status.LlmEnrichmentEnabled,
                HasRuntimeOverride: status.HasRuntimeOverride,
                Provider: status.Provider,
                CacheDirectory: status.CacheDirectory,
                MaxEndpointsPerBatch: status.MaxEndpointsPerBatch
            ),
            AuditEnabled: _auditService.IsEnabled,
            StatelessMode: _sourceSettings.StatelessMode,
            Sources: sources.Cast<object>().ToList()
        ));
    }

    /// <summary>
    /// Configures LLM enrichment at runtime without restarting the server.
    /// </summary>
    [HttpPut("llm-enrichment")]
    [ProducesResponseType(typeof(LlmEnrichmentUpdateResponse), 200)]
    public async Task<IActionResult> SetLlmEnrichment([FromBody] SetLlmEnrichmentRequest request)
    {
        await _runtimeConfig.SetLlmEnrichmentEnabledAsync(request.Enabled, persist: request.Persist);

        if (request.InvalidateCache)
            await _swaggerService.InvalidateCacheAsync();

        _auditService.Log(AuditCode.A006_PrivilegedAction,
            $"REST admin set LLM enrichment enabled={request.Enabled} persist={request.Persist} invalidateCache={request.InvalidateCache}",
            appSource: "gateway",
            additionalContext: new Dictionary<string, object?>
            {
                ["Enabled"]         = request.Enabled,
                ["Persist"]         = request.Persist,
                ["InvalidateCache"] = request.InvalidateCache
            });

        var status = _runtimeConfig.GetStatus();
        return Ok(new LlmEnrichmentUpdateResponse(
            Enabled: status.LlmEnrichmentEnabled,
            Provider: status.Provider,
            CacheInvalidated: request.InvalidateCache,
            Persisted: request.Persist
        ));
    }

    /// <summary>
    /// Lists all configured API sources.
    /// </summary>
    [HttpGet("sources")]
    [ProducesResponseType(200)]
    public IActionResult GetSources()
    {
        var sources = _sourceSettings.Sources.Select(s => new
        {
            s.Name,
            s.BaseUrl,
            s.SwaggerJsonPath,
            s.TimeoutSeconds,
            s.ForwardCallerToken,
            GlobalDatasetCount = s.GlobalDatasetOperationIds.Count
        });

        return Ok(new { total = _sourceSettings.Sources.Count, sources });
    }

    /// <summary>
    /// Invalidates the Swagger spec cache, forcing a fresh fetch on next use.
    /// Call this after updating an API's endpoints or Swagger spec.
    /// </summary>
    [HttpPost("cache/invalidate")]
    [ProducesResponseType(typeof(CacheInvalidateResponse), 200)]
    public async Task<IActionResult> InvalidateCache([FromQuery] string? source = null)
    {
        // ISwaggerService.InvalidateCacheAsync clears all sources.
        // If a specific source is requested, validate it exists first.
        if (!string.IsNullOrEmpty(source))
        {
            var exists = _sourceSettings.Sources
                .Any(s => string.Equals(s.Name, source, StringComparison.OrdinalIgnoreCase));

            if (!exists)
                return NotFound(new { error = $"Source '{source}' not found. Available: {string.Join(", ", _sourceSettings.Sources.Select(s => s.Name))}" });
        }

        await _swaggerService.InvalidateCacheAsync();

        _auditService.Log(AuditCode.A006_PrivilegedAction,
            $"REST admin invalidated cache scope={source ?? "all"}",
            appSource: "gateway");
        _auditService.Log(AuditCode.A012_DataStructureChange,
            $"Swagger spec cache invalidated scope={source ?? "all"}",
            appSource: "gateway",
            additionalContext: new Dictionary<string, object?> { ["Scope"] = source ?? "all" });

        return Ok(new CacheInvalidateResponse(
            Invalidated: true,
            Scope: string.IsNullOrEmpty(source) ? "all" : source,
            Message: string.IsNullOrEmpty(source)
                ? "All source caches cleared. Specs will be reloaded on next request."
                : $"Cache cleared for source '{source}'. Spec will be reloaded on next request."
        ));
    }

    /// <summary>
    /// Returns whether audit logging is currently enabled.
    /// </summary>
    [HttpGet("audit")]
    [ProducesResponseType(200)]
    public IActionResult GetAuditStatus() =>
        Ok(new { enabled = _auditService.IsEnabled });

    /// <summary>
    /// Enables or disables audit logging at runtime without restarting the server.
    /// </summary>
    [HttpPut("audit")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SetAudit([FromBody] SetAuditRequest request)
    {
        await _auditService.SetEnabledAsync(request.Enabled, persist: request.Persist);

        // Emit A006 only when re-enabling (no point logging into a just-disabled audit system)
        if (request.Enabled)
            _auditService.Log(AuditCode.A006_PrivilegedAction,
                $"REST admin set audit enabled={request.Enabled}",
                appSource: "gateway");

        return Ok(new { enabled = _auditService.IsEnabled, persisted = request.Persist });
    }
}

// ── Request / Response models ──────────────────────────────────────────────

public record SetLlmEnrichmentRequest(
    bool Enabled,
    bool InvalidateCache = true,
    bool Persist = true);

public record SetAuditRequest(bool Enabled, bool Persist = true);

public record ConfigResponse(
    LlmEnrichmentStatus LlmEnrichment,
    bool AuditEnabled,
    bool StatelessMode,
    List<object> Sources);

public record LlmEnrichmentStatus(
    bool Enabled,
    bool HasRuntimeOverride,
    string Provider,
    string CacheDirectory,
    int MaxEndpointsPerBatch);

public record LlmEnrichmentUpdateResponse(
    bool Enabled,
    string Provider,
    bool CacheInvalidated,
    bool Persisted);

public record CacheInvalidateResponse(
    bool Invalidated,
    string Scope,
    string Message);
