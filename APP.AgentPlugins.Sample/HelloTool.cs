using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.Sample;

/// <summary>
/// Minimal ExternalDll sample for GenericAgent / AppAgentToolEngine.
///
/// Deploy: APP.AgentPlugins.Sample.dll → {WebRoot}/AgentPlugins/
/// Register ToolType=ExternalDll with ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.Sample.dll", "TypeName": "APP.AgentPlugins.Sample.HelloTool" }
/// ToolName example: sample_hello
/// </summary>
public sealed class HelloTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("name", out var name);
        if (string.IsNullOrWhiteSpace(name))
            name = "world";

        var payload = new
        {
            ok = true,
            message = $"Hello, {name.Trim()}!",
            echo = new
            {
                companyId = context.CompanyId,
                dataSourceId = context.DataSourceId,
                userId = context.UserId,
                skillKey = context.SkillKey,
                workflowId = context.WorkflowId
            }
        };

        return Task.FromResult(JsonConvert.SerializeObject(payload));
    }
}
