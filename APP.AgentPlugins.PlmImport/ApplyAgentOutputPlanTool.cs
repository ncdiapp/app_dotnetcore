using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll: run the whole executionPlan in BL (SQL files then blueprint).
/// The LLM must not walk steps itself — this tool is the single APPLY entry.
/// </summary>
public sealed class ApplyAgentOutputPlanTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = PlmImportEngine.ApplyAgentOutputPlan(
                context,
                PlmBlToolArgs.GetString(args, "outputsContextKey"),
                PlmBlToolArgs.GetString(args, "planJson"),
                PlmBlToolArgs.ParseInt(args, "sessionId"),
                PlmBlToolArgs.ParseInt(args, "saasApplicationId"),
                PlmBlToolArgs.GetString(args, "requiredDataSourceIds"),
                PlmBlToolArgs.GetString(args, "mode"));
            return Task.FromResult(PlmBlToolArgs.Serialize(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(PlmBlToolArgs.Serialize(new
            {
                Ok = false,
                Error = ex.Message,
                Planned = 0,
                Executed = 0
            }));
        }
    }
}
