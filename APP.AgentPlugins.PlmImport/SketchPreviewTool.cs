using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll tool: build sketch-import preview from raw connection strings.
/// Host BuiltIn <c>preview_plm_sketch_import</c> resolves session then calls this via Dispatch.
///
/// ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.PlmImport.dll", "TypeName": "APP.AgentPlugins.PlmImport.SketchPreviewTool" }
/// </summary>
public sealed class SketchPreviewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("plmConnectionString", out var plm);
        args.TryGetValue("tenantConnectionString", out var tenant);

        try
        {
            var preview = SketchImportPreviewBuilder.Build(plm, tenant);
            return Task.FromResult(JsonConvert.SerializeObject(preview));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new APP.Components.EntityDto.PlmSketchImportPreviewDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            }));
        }
    }
}
