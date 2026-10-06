namespace McpGateway.Services;

/// <summary>
/// Provides runtime-toggleable configuration overrides that take effect without restart.
/// Overrides are applied on top of appsettings.json values and can be persisted back to disk.
/// </summary>
public interface IRuntimeConfigService
{
    /// <summary>Runtime override for LlmEnrichment:Enabled. Null means "use appsettings value".</summary>
    bool? LlmEnrichmentEnabledOverride { get; }

    /// <summary>Returns the effective LlmEnrichment enabled state (runtime override takes precedence).</summary>
    bool IsLlmEnrichmentEnabled();

    /// <summary>Sets the runtime override and optionally persists it to appsettings.json.</summary>
    Task SetLlmEnrichmentEnabledAsync(bool enabled, bool persist = true);

    /// <summary>Returns a status snapshot for admin inspection.</summary>
    RuntimeConfigStatus GetStatus();
}

public record RuntimeConfigStatus(
    bool LlmEnrichmentEnabled,
    bool HasRuntimeOverride,
    string Provider,
    string CacheDirectory,
    int MaxEndpointsPerBatch);
