using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll tool: User Define entity import preview.
///
/// ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.PlmImport.dll", "TypeName": "APP.AgentPlugins.PlmImport.UserDefineEntityPreviewTool" }
/// </summary>
public sealed class UserDefineEntityPreviewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("plmConnectionString", out var plm);
        args.TryGetValue("tenantConnectionString", out var tenant);
        args.TryGetValue("tenantDatabaseName", out var tenantDatabaseName);
        args.TryGetValue("entityWideTablePrefix", out var entityWideTablePrefix);

        try
        {
            var preview = UserDefineEntityPreviewBuilder.Build(plm, tenant, tenantDatabaseName, entityWideTablePrefix);
            return Task.FromResult(JsonConvert.SerializeObject(preview));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new PlmUserDefineEntityPreviewDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            }));
        }
    }
}
