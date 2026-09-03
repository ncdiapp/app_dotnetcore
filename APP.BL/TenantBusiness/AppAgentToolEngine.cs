using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.BL.TenantBusiness.AgentToolExecutors;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.TenantBusiness
{
    /// <summary>
    /// Delegates tool calls to the correct executor based on ToolType.
    /// ToolTypes: BuiltIn | ExternalDll | SqlQuery | PowerShell | HttpRest | DynamicCSharp
    /// </summary>
    public static class AppAgentToolEngine
    {
        /// <summary>
        /// Builds a name-keyed dictionary of async tool invokers from AppAgentToolRegister rows
        /// for the given skillKey. Each invoker takes (args, context, ct) → string result.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, string>, AgentToolContext, CancellationToken, Task<string>>>>
            BuildInvokersAsync(string skillKey, AgentToolContext context, CancellationToken ct)
        {
            var rows = AppAgentToolRegisterBL.GetBySkillKey(skillKey);
            var result = new Dictionary<string, Func<IReadOnlyDictionary<string, string>, AgentToolContext, CancellationToken, Task<string>>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var captured = row;
                result[captured.ToolName] = (args, ctx, token) => Dispatch(captured.ToolType, captured.ToolConfig, args, ctx, token);
            }

            await Task.CompletedTask.ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Dispatches a single tool call by ToolType.
        /// </summary>
        public static Task<string> Dispatch(
            string                             toolType,
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            return (toolType ?? "BuiltIn") switch
            {
                "BuiltIn"       => BuiltInToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
                "ExternalDll"   => ExternalDllToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
                "SqlQuery"      => SqlQueryToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
                "PowerShell"    => PowerShellToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
                "HttpRest"      => HttpRestToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
                "DynamicCSharp" => DynamicCSharpToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
                _               => Task.FromResult(JsonConvert.SerializeObject(new { Error = $"Unknown ToolType: {toolType}" }))
            };
        }
    }
}
