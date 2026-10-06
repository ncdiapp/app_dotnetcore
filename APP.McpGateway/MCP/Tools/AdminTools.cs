using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using McpGateway.Models;
using McpGateway.Services;

namespace McpGateway.MCP.Tools;

// Not exposed as MCP tools — management is handled via REST at /api/management
public class AdminTools
{
    /// <summary>
    /// Returns current runtime configuration status for the MCP Gateway.
    /// </summary>
    [McpServerTool(Name = "admin_get_config")]
    [Description("Returns the current runtime configuration of the MCP Gateway, including whether LLM enrichment is enabled, which provider is configured, and whether a runtime override is active.")]
    public static string GetConfig(IRuntimeConfigService runtimeConfig, IAuditService auditService)
    {
        auditService.Log(AuditCode.A006_PrivilegedAction,
            "Admin read gateway config",
            appSource: "gateway");

        var status = runtimeConfig.GetStatus();
        return JsonSerializer.Serialize(new
        {
            llmEnrichment = new
            {
                enabled = status.LlmEnrichmentEnabled,
                hasRuntimeOverride = status.HasRuntimeOverride,
                provider = status.Provider,
                cacheDirectory = status.CacheDirectory,
                maxEndpointsPerBatch = status.MaxEndpointsPerBatch
            },
            note = status.HasRuntimeOverride
                ? "A runtime override is active. The appsettings.json value has been updated."
                : "Using value from appsettings.json. Call admin_set_llm_enrichment to override at runtime."
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Enables or disables LLM enrichment at runtime without restarting the server.
    /// </summary>
    [McpServerTool(Name = "admin_set_llm_enrichment")]
    [Description("Enables or disables LLM enrichment at runtime. When enabled, endpoints are enriched with AI-generated descriptions on next load. Set reload=true to immediately reload all Swagger specs and apply enrichment. The setting is persisted to appsettings.json by default.")]
    public static async Task<string> SetLlmEnrichmentAsync(
        IRuntimeConfigService runtimeConfig,
        ISwaggerService swaggerService,
        IAuditService auditService,
        [Description("Set to true to enable LLM enrichment, false to disable it.")]
        bool enabled,
        [Description("If true, invalidates the Swagger cache so specs are reloaded and enrichment runs immediately. Default: true.")]
        bool reload = true)
    {
        await runtimeConfig.SetLlmEnrichmentEnabledAsync(enabled, persist: true);

        if (reload)
            await swaggerService.InvalidateCacheAsync();

        auditService.Log(AuditCode.A006_PrivilegedAction,
            $"Admin set LLM enrichment enabled={enabled} reload={reload}",
            appSource: "gateway",
            additionalContext: new Dictionary<string, object?> { ["Enabled"] = enabled, ["Reload"] = reload });

        var status = runtimeConfig.GetStatus();
        return JsonSerializer.Serialize(new
        {
            success = true,
            llmEnrichment = new
            {
                enabled = status.LlmEnrichmentEnabled,
                provider = status.Provider
            },
            cacheReloaded = reload,
            message = enabled
                ? reload
                    ? "LLM enrichment enabled and Swagger cache cleared. Endpoints will be enriched on next request."
                    : "LLM enrichment enabled. Call admin_set_llm_enrichment with reload=true or restart to apply enrichment."
                : "LLM enrichment disabled. Existing enriched data remains until next reload."
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
