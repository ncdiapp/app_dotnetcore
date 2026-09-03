using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness.AgentToolExecutors
{
    /// <summary>
    /// Executes a PowerShell script using reflection against System.Management.Automation.
    /// ToolConfig: {"Script":"Write-Host $param1"}
    /// Args are injected as $argName variables before execution.
    /// Only available when System.Management.Automation is loaded at runtime.
    /// </summary>
    public static class PowerShellToolExecutor
    {
        public static Task<string> ExecuteAsync(
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            var script = ParseScript(toolConfig);
            if (string.IsNullOrWhiteSpace(script))
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "PowerShell ToolConfig requires Script." }));

            try
            {
                return Task.FromResult(RunScript(script, args));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = ex.Message }));
            }
        }

        private static string RunScript(string script, IReadOnlyDictionary<string, string> args)
        {
            var psType = Type.GetType("System.Management.Automation.PowerShell, System.Management.Automation");
            if (psType == null)
                return JsonConvert.SerializeObject(new { Error = "System.Management.Automation is not available on this host." });

            var createMethod = psType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (createMethod == null)
                return JsonConvert.SerializeObject(new { Error = "PowerShell.Create() not found." });

            using var psRaw = (IDisposable)createMethod.Invoke(null, null);

            // Inject variables via Runspace.SessionStateProxy
            if (args != null)
            {
                var runspaceProp = psType.GetProperty("Runspace");
                var runspace = runspaceProp?.GetValue(psRaw);
                if (runspace != null)
                {
                    var proxyProp = runspace.GetType().GetProperty("SessionStateProxy");
                    var proxy = proxyProp?.GetValue(runspace);
                    if (proxy != null)
                    {
                        var setVar = proxy.GetType().GetMethod("SetVariable", new[] { typeof(string), typeof(object) });
                        if (setVar != null)
                            foreach (var kv in args)
                                setVar.Invoke(proxy, new object[] { kv.Key, kv.Value });
                    }
                }
            }

            var addScript = psType.GetMethod("AddScript", new[] { typeof(string) });
            addScript?.Invoke(psRaw, new object[] { script });

            var invoke = psType.GetMethod("Invoke", Type.EmptyTypes);
            var output = invoke?.Invoke(psRaw, null) as System.Collections.IEnumerable;

            var sb = new StringBuilder();
            if (output != null)
                foreach (var item in output)
                    if (item != null) sb.AppendLine(item.ToString());

            return sb.ToString().TrimEnd();
        }

        private static string ParseScript(string toolConfig)
        {
            try { return JObject.Parse(toolConfig ?? "{}")["Script"]?.ToString() ?? ""; }
            catch { return ""; }
        }
    }
}
