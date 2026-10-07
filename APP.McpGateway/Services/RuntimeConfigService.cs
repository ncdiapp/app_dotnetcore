using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using McpGateway.Models;

namespace McpGateway.Services;

/// <summary>
/// Singleton that holds in-memory configuration overrides and can persist them to appsettings.json.
/// </summary>
public class RuntimeConfigService : IRuntimeConfigService
{
    private readonly LlmEnrichmentSettings _settings;
    private readonly ILogger<RuntimeConfigService> _logger;
    private readonly string _appsettingsPath;

    private bool? _llmEnrichmentEnabledOverride;

    public RuntimeConfigService(
        IOptions<LlmEnrichmentSettings> settings,
        ILogger<RuntimeConfigService> logger,
        IHostEnvironment env)
    {
        _settings = settings.Value;
        _logger = logger;
        _appsettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
    }

    public bool? LlmEnrichmentEnabledOverride => _llmEnrichmentEnabledOverride;

    public bool IsLlmEnrichmentEnabled() =>
        _llmEnrichmentEnabledOverride ?? _settings.Enabled;

    public async Task SetLlmEnrichmentEnabledAsync(bool enabled, bool persist = true)
    {
        _llmEnrichmentEnabledOverride = enabled;
        _logger.LogInformation("LlmEnrichment runtime override set to {Enabled}", enabled);

        if (persist)
            await PersistToAppsettingsAsync(enabled);
    }

    public RuntimeConfigStatus GetStatus() => new(
        LlmEnrichmentEnabled: IsLlmEnrichmentEnabled(),
        HasRuntimeOverride: _llmEnrichmentEnabledOverride.HasValue,
        Provider: _settings.Provider,
        CacheDirectory: _settings.CacheDirectory,
        MaxEndpointsPerBatch: _settings.MaxEndpointsPerBatch);

    private async Task PersistToAppsettingsAsync(bool enabled)
    {
        if (!File.Exists(_appsettingsPath))
        {
            _logger.LogWarning("appsettings.json not found at {Path} — runtime override is in-memory only", _appsettingsPath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_appsettingsPath);
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                _logger.LogWarning("Failed to parse appsettings.json — runtime override is in-memory only");
                return;
            }

            node["LlmEnrichment"] ??= new JsonObject();
            node["LlmEnrichment"]!["Enabled"] = enabled;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_appsettingsPath, node.ToJsonString(options));
            _logger.LogInformation("Persisted LlmEnrichment:Enabled={Enabled} to {Path}", enabled, _appsettingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist LlmEnrichment setting to appsettings.json — runtime override is active but not saved");
        }
    }
}
