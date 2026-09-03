using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APP.Framework.Plugin;

/// <summary>
/// Contract for ExternalDll agent tools.
/// Drop a DLL that implements this interface into the ExternalDllRepository folder,
/// register it in AppAgentToolRegister with ToolType='ExternalDll', and the agent
/// picks it up without recompile or redeploy.
///
/// ToolConfig JSON shape for ExternalDll:
///   { "AssemblyName": "Tenant.Reports.dll", "TypeName": "Tenant.Reports.ReportTool", "MethodName": "Run" }
///
/// ExternalDllToolExecutor loads the assembly via Assembly.LoadFrom, instantiates the
/// type, and calls ExecuteAsync with the LLM-supplied arguments and the current AgentContext.
/// </summary>
public interface IAgentTool
{
    /// <summary>
    /// Executes the tool and returns a plain-text result for the LLM.
    /// </summary>
    /// <param name="args">Key-value pairs from the LLM tool call arguments.</param>
    /// <param name="context">Runtime context: connection string, session id, skill key.</param>
    /// <param name="cancellationToken">Propagated from the agent session.</param>
    Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext                     context,
        CancellationToken                    cancellationToken);
}

/// <summary>
/// Runtime context passed to every IAgentTool.ExecuteAsync call.
/// Mirrors the fields available inside built-in plugin methods via AgentContext.
/// </summary>
public sealed class AgentToolContext
{
    /// <summary>Tenant database connection string.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Active agent session identifier.</summary>
    public string SessionId        { get; init; } = string.Empty;

    /// <summary>SkillKey that owns this tool (e.g. "app-builder").</summary>
    public string SkillKey         { get; init; } = string.Empty;

    /// <summary>Authenticated user id, sourced from the request identity.</summary>
    public int    UserId           { get; init; }

    /// <summary>Tenant/company id, sourced from the request identity.</summary>
    public int    CompanyId        { get; init; }
}
