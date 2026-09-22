using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Framework;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness.AgentToolExecutors
{
    /// <summary>
    /// Loads an external DLL and calls an IAgentTool implementation.
    /// ToolConfig: {"AssemblyName":"Tenant.Custom.dll","TypeName":"Tenant.Custom.MyTool"}
    /// DLL folder: AppConfig "Agent.ExternalDllRepo", else {BaseDirectory}/AgentPlugins
    /// (Command plugins use ExternalDllRepository — keep Agent DLLs in AgentPlugins unless configured).
    /// Restores ServerContext identity like BuiltIn so host BL calls work on background threads.
    /// </summary>
    public static class ExternalDllToolExecutor
    {
        private static readonly string ExternalDllRepo =
            AppConfig.Get("Agent.ExternalDllRepo")
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AgentPlugins");

        public static async Task<string> ExecuteAsync(
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            var cfg = ParseConfig(toolConfig);
            if (string.IsNullOrWhiteSpace(cfg.AssemblyName))
                return JsonConvert.SerializeObject(new { Error = "ExternalDll ToolConfig requires AssemblyName." });
            if (string.IsNullOrWhiteSpace(cfg.TypeName))
                return JsonConvert.SerializeObject(new { Error = "ExternalDll ToolConfig requires TypeName." });

            var asmPath = Path.Combine(ExternalDllRepo, cfg.AssemblyName);
            if (!File.Exists(asmPath))
                return JsonConvert.SerializeObject(new { Error = $"Assembly not found: {asmPath}" });

            Assembly asm;
            try { asm = Assembly.LoadFrom(asmPath); }
            catch (Exception ex)
            { return JsonConvert.SerializeObject(new { Error = $"Failed to load {cfg.AssemblyName}: {ex.Message}" }); }

            var type = asm.GetType(cfg.TypeName, throwOnError: false, ignoreCase: true)
                       ?? asm.GetType(cfg.TypeName);
            if (type == null)
                return JsonConvert.SerializeObject(new { Error = $"Type not found: {cfg.TypeName}" });

            object instance;
            try { instance = Activator.CreateInstance(type); }
            catch (Exception ex)
            { return JsonConvert.SerializeObject(new { Error = $"Cannot instantiate {cfg.TypeName}: {ex.Message}" }); }

            if (instance is not IAgentTool tool)
                return JsonConvert.SerializeObject(new { Error = $"{cfg.TypeName} does not implement IAgentTool." });

            // Same identity restore as BuiltInToolExecutor — agent tools often run after HTTP flush.
            if (!string.IsNullOrEmpty(context.ConnectionString) && !string.IsNullOrEmpty(context.DatabaseName))
            {
                ServerContext.OverrideThreadIdentity(new AppClientIdentity
                {
                    UserId                        = context.UserId,
                    CurrentWorkingCompanyId       = context.CompanyId,
                    CurrentUserDbConnectionString = context.ConnectionString,
                    CurrentUserDataBaseName       = context.DatabaseName,
                    SessionId                     = context.UserSessionId,
                    DataSourceId                  = context.DataSourceId,
                    CurrentLoginUserType          = context.LoginUserType
                });
            }

            try
            {
                return await tool.ExecuteAsync(args, context, ct).ConfigureAwait(false);
            }
            finally
            {
                ServerContext.OverrideThreadIdentity(null);
            }
        }

        private static (string AssemblyName, string TypeName) ParseConfig(string toolConfig)
        {
            try
            {
                var obj = JObject.Parse(toolConfig ?? "{}");
                return (
                    obj["AssemblyName"]?.ToString() ?? "",
                    obj["TypeName"]?.ToString() ?? "");
            }
            catch { return ("", ""); }
        }
    }
}
