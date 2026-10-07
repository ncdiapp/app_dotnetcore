using System.Text.Json;
using McpGateway.Models;

namespace McpGateway.Services;

public interface IDataAnalysisCacheService
{
    /// <summary>
    /// Builds and caches a dataset from the API response JSON array for the authenticated caller.
    /// isGlobal=true: disk + memory cache shared by the users of the caller's company only.
    /// isGlobal=false: memory-only cache private to the calling user.
    /// Throws UnauthorizedAccessException when there is no authenticated caller.
    /// </summary>
    Task<CachedDataset> SyncFromJsonAsync(string app, string operationId, JsonElement jsonDataArray, bool isGlobal = false);

    /// <summary>
    /// Returns the dataset from the caller's company-global cache, else the caller's private cache.
    /// Returns null if not found, or if the name is not a name this service generates.
    /// </summary>
    Task<CachedDataset?> GetCachedDatasetAsync(string datasetName);
}

public interface IAnalysisService
{
    Task<string> AnalyzeAsync(string datasetName, string operation,
        string? column = null, string? groupBy = null,
        string? filter = null, string? filterColumn = null,
        int limit = 20, string? orderBy = null);
}
