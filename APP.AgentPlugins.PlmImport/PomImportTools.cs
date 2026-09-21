using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Components.EntityDto;
using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>ExternalDll wrapper: preview PLM POM / body-part import.</summary>
public sealed class PreviewPomImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var result = PlmImportEngine.PreviewPlmPomImport(sessionId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll wrapper: execute PLM POM import (sync).</summary>
public sealed class ExecutePomImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmPomImportExecuteRequestDto
        {
            SessionId = PlmBlToolArgs.ParseInt(args, "sessionId"),
            SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId"),
            ImportJunctionTables = PlmBlToolArgs.ParseBool(args, "importJunctionTables", true),
            ImportFoldersIfMissing = PlmBlToolArgs.ParseBool(args, "importFoldersIfMissing", true)
        };
        var result = PlmImportEngine.ExecutePlmPomImport(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
