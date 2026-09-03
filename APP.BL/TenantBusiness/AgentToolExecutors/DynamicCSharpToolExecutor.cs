using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness.AgentToolExecutors
{
    /// <summary>
    /// Evaluates a C# code snippet at runtime using reflection against
    /// Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.
    /// ToolConfig: {"Code":"return args[\"param1\"] + \" done\";"}
    /// Globals: args (Dictionary<string,string>), context (AgentToolContext)
    /// </summary>
    public static class DynamicCSharpToolExecutor
    {
        private static readonly string[] AllowedImports =
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Newtonsoft.Json"
        };

        public static async Task<string> ExecuteAsync(
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            var code = ParseCode(toolConfig);
            if (string.IsNullOrWhiteSpace(code))
                return JsonConvert.SerializeObject(new { Error = "DynamicCSharp ToolConfig requires Code." });

            // Locate CSharpScript via reflection to avoid hard compile dependency
            var scriptType = Type.GetType(
                "Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript, Microsoft.CodeAnalysis.CSharp.Scripting");
            if (scriptType == null)
                return JsonConvert.SerializeObject(new { Error = "Microsoft.CodeAnalysis.CSharp.Scripting is not available." });

            try
            {
                var globalsType = typeof(ScriptGlobals);
                var globals = new ScriptGlobals
                {
                    args    = new Dictionary<string, string>(args ?? new Dictionary<string, string>()),
                    context = context
                };

                // ScriptOptions with imports
                var optionsType = Type.GetType("Microsoft.CodeAnalysis.Scripting.ScriptOptions, Microsoft.CodeAnalysis.Scripting");
                var defaultOpts = optionsType?.GetProperty("Default", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                object options = defaultOpts;
                if (defaultOpts != null)
                {
                    var withImports = optionsType.GetMethod("AddImports", new[] { typeof(string[]) });
                    options = withImports?.Invoke(defaultOpts, new object[] { AllowedImports }) ?? defaultOpts;
                }

                // EvaluateAsync<string>(code, options, globals, globalsType, ct)
                var evalMethod = scriptType.GetMethod("EvaluateAsync",
                    new[] { typeof(string), optionsType, typeof(object), typeof(Type), typeof(CancellationToken) });
                if (evalMethod == null)
                    return JsonConvert.SerializeObject(new { Error = "CSharpScript.EvaluateAsync not found." });

                var genericEval = evalMethod.MakeGenericMethod(typeof(string));
                var task = (Task<string>)genericEval.Invoke(null, new[] { code, options, globals, globalsType, ct });
                return await task.ConfigureAwait(false) ?? "";
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                return JsonConvert.SerializeObject(new { Error = inner.Message });
            }
        }

        private static string ParseCode(string toolConfig)
        {
            try { return JObject.Parse(toolConfig ?? "{}")["Code"]?.ToString() ?? ""; }
            catch { return ""; }
        }
    }

    public sealed class ScriptGlobals
    {
        public Dictionary<string, string> args    { get; set; }
        public AgentToolContext           context { get; set; }
    }
}
