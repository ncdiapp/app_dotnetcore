using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll tool: System Define entity import preview.
/// Host resolves data source maps then calls via bridge.
///
/// ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.PlmImport.dll", "TypeName": "APP.AgentPlugins.PlmImport.SystemDefineEntityPreviewTool" }
/// </summary>
public sealed class SystemDefineEntityPreviewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("plmConnectionString", out var plm);
        args.TryGetValue("tenantConnectionString", out var tenant);
        args.TryGetValue("tablePrefix", out var tablePrefix);
        args.TryGetValue("dataSourceMapsJson", out var dataSourceMapsJson);

        try
        {
            var preview = SystemDefineEntityPreviewBuilder.Build(plm, tenant, tablePrefix, dataSourceMapsJson);
            return Task.FromResult(JsonConvert.SerializeObject(preview));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new PlmSystemDefineEntityPreviewDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            }));
        }
    }
}
