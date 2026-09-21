using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll tool: copy System Define PLM tables into tenant database.
///
/// ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.PlmImport.dll", "TypeName": "APP.AgentPlugins.PlmImport.TableExportTool" }
/// </summary>
public sealed class TableExportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("plmConnectionString", out var plm);
        args.TryGetValue("tenantConnectionString", out var tenant);
        args.TryGetValue("tablePrefix", out var tablePrefix);

        try
        {
            var result = TableExportExporter.Export(plm, tenant, tablePrefix, null);
            return Task.FromResult(JsonConvert.SerializeObject(result));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new PlmTableExportResultDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            }));
        }
    }
}
