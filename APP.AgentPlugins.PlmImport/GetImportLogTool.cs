using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll: list AppPlmImportLog rows for a session.
/// ToolConfig: { "AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.GetImportLogTool" }
/// </summary>
public sealed class GetImportLogTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        var result = PlmImportEngine.GetImportLog(sessionId, targetCompanyId);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
