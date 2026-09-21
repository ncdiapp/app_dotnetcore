using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll tool: build System Define table-export plan from raw PLM connection + tablePrefix.
/// Host BuiltIn <c>preview_plm_table_export_plan</c> resolves session then calls this via bridge.
///
/// ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.PlmImport.dll", "TypeName": "APP.AgentPlugins.PlmImport.TableExportPlanTool" }
/// </summary>
public sealed class TableExportPlanTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("plmConnectionString", out var plm);
        args.TryGetValue("tablePrefix", out var tablePrefix);

        try
        {
            var plan = TableExportPlanBuilder.Build(plm, tablePrefix);
            return Task.FromResult(JsonConvert.SerializeObject(plan));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new APP.Components.EntityDto.PlmTableExportPlanDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            }));
        }
    }
}
