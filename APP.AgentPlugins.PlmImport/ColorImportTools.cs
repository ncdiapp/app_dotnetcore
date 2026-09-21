using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Components.EntityDto;
using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>ExternalDll wrapper: preview PLM color (RGB) import.</summary>
public sealed class PreviewColorImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var result = PlmImportEngine.PreviewPlmColorImport(sessionId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll wrapper: execute PLM color import (sync).</summary>
public sealed class ExecuteColorImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmColorImportExecuteRequestDto
        {
            SessionId = PlmBlToolArgs.ParseInt(args, "sessionId"),
            SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId")
        };
        var result = PlmImportEngine.ExecutePlmColorImport(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
