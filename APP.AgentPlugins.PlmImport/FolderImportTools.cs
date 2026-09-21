using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>ExternalDll wrapper: preview PLM folder tree import.</summary>
public sealed class PreviewFolderImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var result = PlmImportEngine.PreviewPlmFolderImport(sessionId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll wrapper: queue PLM folder import job. Poll with get_plm_import_job.</summary>
public sealed class ExecuteFolderImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var result = PlmImportEngine.ExecutePlmFolderImport(sessionId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll wrapper: preview folder placement (anchor / mapping).</summary>
public sealed class PreviewFolderPlacementTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var result = PlmImportEngine.PreviewPlmFolderPlacement(sessionId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll wrapper: queue folder placement job.</summary>
public sealed class ExecuteFolderPlacementTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var result = PlmImportEngine.ExecutePlmFolderPlacement(sessionId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
