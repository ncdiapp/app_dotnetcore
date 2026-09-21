using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

public sealed class PreviewSketchImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.PreviewPlmSketchImport(sessionId)));
    }
}

public sealed class ExecuteSketchImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.ExecutePlmSketchImport(sessionId)));
    }
}

public sealed class GetPlmImportJobTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var jobId = PlmBlToolArgs.ParseInt(args, "jobId") ?? 0;
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.GetImportJob(jobId)));
    }
}

public sealed class CancelPlmImportJobTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var jobId = PlmBlToolArgs.ParseInt(args, "jobId") ?? 0;
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.CancelImportJob(jobId)));
    }
}
