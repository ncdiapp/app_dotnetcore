using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

public sealed class PreviewTableExportPlanTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.PreviewPlmTableExportPlan(sessionId)));
    }
}

public sealed class ExecuteTableExportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.StartPlmTableExportJob(sessionId)));
    }
}

public sealed class PreviewSystemDefineEntityImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.PreviewSystemDefineEntityImport(sessionId)));
    }
}

public sealed class ExecuteSystemDefineEntityImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.ExecuteSystemDefineEntityImport(sessionId)));
    }
}

public sealed class PreviewUserDefineEntityImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.PreviewUserDefineEntityImport(sessionId)));
    }
}

public sealed class ExecuteUserDefineEntityImportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.ExecuteUserDefineEntityImport(sessionId)));
    }
}
