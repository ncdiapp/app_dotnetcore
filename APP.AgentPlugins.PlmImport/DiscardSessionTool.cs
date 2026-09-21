using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll: discard (complete) the PLM import session.
/// ToolConfig: { "AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.DiscardSessionTool" }
/// </summary>
public sealed class DiscardSessionTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        var result = PlmImportEngine.DiscardImportSession(sessionId, targetCompanyId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
