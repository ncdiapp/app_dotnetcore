using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness.AgentToolExecutors
{
    /// <summary>
    /// Resolves [AgentTool]-decorated methods by TypeName+MethodName from ToolConfig JSON.
    /// ToolConfig: {"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ProposePlan"}
    /// </summary>
    public static class BuiltInToolExecutor
    {
        private static readonly Assembly BlAssembly = typeof(BuiltInToolExecutor).Assembly;

        public static async Task<string> ExecuteAsync(
            string                             toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct)
        {
            var cfg = ParseConfig(toolConfig);
            var type = ResolveType(cfg.TypeName);
            if (type == null)
                return JsonConvert.SerializeObject(new { Error = $"BuiltIn type not found: {cfg.TypeName}" });

            var method = type.GetMethod(cfg.MethodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (method == null)
                return JsonConvert.SerializeObject(new { Error = $"Method not found: {cfg.MethodName} on {cfg.TypeName}" });

            object instance = null;
            if (!method.IsStatic)
                instance = TryCreateInstance(type, context);

            var paramValues = BuildParams(method, args, context, ct);

            try
            {
                object returnValue;
                if (method.ReturnType == typeof(Task<string>))
                    returnValue = await ((Task<string>)method.Invoke(instance, paramValues)).ConfigureAwait(false);
                else if (method.ReturnType == typeof(Task))
                {
                    await ((Task)method.Invoke(instance, paramValues)).ConfigureAwait(false);
                    returnValue = "";
                }
                else
                    returnValue = method.Invoke(instance, paramValues);

                if (returnValue is string s) return s;
                return JsonConvert.SerializeObject(returnValue);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                return JsonConvert.SerializeObject(new { Error = tie.InnerException.Message });
            }
        }

        private static object TryCreateInstance(Type type, AgentToolContext context)
        {
            // Try parameterless constructor first
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor != null) return Activator.CreateInstance(type);

            // Try constructor that takes AppClientIdentity
            var identityType = typeof(AppClientIdentity);
            var identityCtor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == 1 &&
                                     c.GetParameters()[0].ParameterType.IsAssignableFrom(identityType));
            if (identityCtor != null)
                return identityCtor.Invoke(new object[] { default(AppClientIdentity) });

            // Try constructor that takes a single Func<> (plan/schema gate callbacks)
            var funcCtor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == 1 &&
                                     c.GetParameters()[0].ParameterType.IsSubclassOf(typeof(Delegate)));
            if (funcCtor != null)
                return funcCtor.Invoke(new object[] { null });

            return Activator.CreateInstance(type);
        }

        private static object[] BuildParams(MethodInfo method, IReadOnlyDictionary<string, string> args, AgentToolContext context, CancellationToken ct)
        {
            var parameters = method.GetParameters();
            var values = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (param.ParameterType == typeof(CancellationToken))
                {
                    values[i] = ct;
                    continue;
                }

                if (param.ParameterType == typeof(AgentToolContext))
                {
                    values[i] = context;
                    continue;
                }

                if (param.ParameterType == typeof(AppClientIdentity))
                {
                    values[i] = default(AppClientIdentity);
                    continue;
                }

                if (args != null && args.TryGetValue(param.Name, out var raw))
                {
                    values[i] = ConvertArg(raw, param.ParameterType);
                    continue;
                }

                values[i] = param.HasDefaultValue ? param.DefaultValue : GetDefault(param.ParameterType);
            }

            return values;
        }

        private static object ConvertArg(string raw, Type targetType)
        {
            if (raw == null) return GetDefault(targetType);
            if (targetType == typeof(string)) return raw;
            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.TryParse(raw, out int i) ? (object)i : null;
            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.TryParse(raw, out bool b) ? (object)b : null;
            if (targetType == typeof(long) || targetType == typeof(long?))
                return long.TryParse(raw, out long l) ? (object)l : null;
            try { return JsonConvert.DeserializeObject(raw, targetType); } catch { return GetDefault(targetType); }
        }

        private static object GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;
            var t = Type.GetType(typeName);
            if (t != null) return t;
            t = BlAssembly.GetType(typeName);
            if (t != null) return t;
            // Scan all loaded assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }
            return null;
        }

        private static (string TypeName, string MethodName) ParseConfig(string toolConfig)
        {
            try
            {
                var obj = JObject.Parse(toolConfig ?? "{}");
                return (
                    obj["TypeName"]?.ToString() ?? "",
                    obj["MethodName"]?.ToString() ?? "");
            }
            catch
            {
                return ("", "");
            }
        }
    }
}
