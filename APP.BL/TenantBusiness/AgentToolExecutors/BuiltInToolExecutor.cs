using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    /// Resolves [AgentTool]-decorated methods by TypeName+MethodName from ToolConfig JSON.
    /// ToolConfig: {"TypeName":"APP.BL.AppBuilderAgent.Plugins.PlanConfirmPlugin","MethodName":"ProposePlan"}
    /// </summary>
    public static class BuiltInToolExecutor
    {
        private static readonly Assembly BlAssembly = typeof(BuiltInToolExecutor).Assembly;

        // Process-lifetime cache: TypeName::MethodName → (Type, MethodInfo)
        // Avoids repeated assembly scanning (AppDomain.GetAssemblies) on every tool call.
        private static readonly ConcurrentDictionary<string, (Type Type, MethodInfo Method)?> _methodCache
            = new(StringComparer.Ordinal);

        public static async Task<string> ExecuteAsync(
            string                              toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext                    context,
            CancellationToken                  ct,
            Dictionary<string, object>?        instancePool = null)
        {
            var cfg      = ParseConfig(toolConfig);
            var cacheKey = $"{cfg.TypeName}::{cfg.MethodName}";
            var cached   = _methodCache.GetOrAdd(cacheKey, _ => ResolveTypeAndMethod(cfg.TypeName, cfg.MethodName));

            if (cached == null)
                return JsonConvert.SerializeObject(new { Error = $"BuiltIn type or method not found: {cfg.TypeName}.{cfg.MethodName}" });

            var type   = cached.Value.Type;
            var method = cached.Value.Method;

            object instance = null;
            if (!method.IsStatic)
            {
                var poolKey = type.FullName ?? cfg.TypeName;
                if (instancePool != null && instancePool.TryGetValue(poolKey, out var pooled))
                    instance = pooled;
                else
                {
                    instance = TryCreateInstance(type, context);
                    if (instancePool != null) instancePool[poolKey] = instance;
                }
            }

            var paramValues = BuildParams(method, args, context, ct);

            // Restore tenant identity on this thread — needed because agent tools run inside
            // Task.Run after the HTTP response has been flushed, so IHttpContextAccessor returns
            // null and ServerContext.CurrnetClientIdentity would otherwise throw.
            if (!string.IsNullOrEmpty(context.ConnectionString) && !string.IsNullOrEmpty(context.DatabaseName))
            {
                ServerContext.OverrideThreadIdentity(new AppClientIdentity
                {
                    UserId                        = context.UserId,
                    CurrentWorkingCompanyId       = context.CompanyId,
                    CurrentUserDbConnectionString = context.ConnectionString,
                    CurrentUserDataBaseName       = context.DatabaseName,
                    SessionId                     = context.UserSessionId
                });
            }

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
            finally
            {
                ServerContext.OverrideThreadIdentity(null);
            }
        }

        private static object TryCreateInstance(Type type, AgentToolContext context)
        {
            // 1. True parameterless constructor
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor != null) return Activator.CreateInstance(type);

            // 2. Constructor that takes a single AppClientIdentity
            var identityType = typeof(AppClientIdentity);
            var identityCtor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == 1 &&
                                     c.GetParameters()[0].ParameterType.IsAssignableFrom(identityType));
            if (identityCtor != null)
                return identityCtor.Invoke(new object[] { default(AppClientIdentity) });

            // 3. Constructor where ALL parameters have default values
            //    e.g. (int? dataSourceId = null) or (int? dataSourceId = null, string schemaOwner = "dbo")
            //    If the first parameter is named "dataSourceId" and is int?, inject from context.
            var allDefaultCtor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length > 0 &&
                                     c.GetParameters().All(p => p.HasDefaultValue));
            if (allDefaultCtor != null)
            {
                var ps = allDefaultCtor.GetParameters();
                var invokeArgs = ps.Select(p => p.DefaultValue).ToArray<object>();
                if (ps[0].Name == "dataSourceId" &&
                    (ps[0].ParameterType == typeof(int?) || ps[0].ParameterType == typeof(int)) &&
                    context.DataSourceId > 0)
                {
                    invokeArgs[0] = (int?)context.DataSourceId;
                }
                return allDefaultCtor.Invoke(invokeArgs);
            }

            // 4. Constructor whose first parameter is a Func/delegate (callback passed as null)
            //    and all remaining parameters have default values
            //    e.g. (Func<AgentPlanEvent, Task<bool>> onPlanReady, int? dataSourceId = null)
            var funcFirstCtor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length >= 1 &&
                                     typeof(Delegate).IsAssignableFrom(c.GetParameters()[0].ParameterType) &&
                                     c.GetParameters().Skip(1).All(p => p.HasDefaultValue));
            if (funcFirstCtor != null)
            {
                var ps = funcFirstCtor.GetParameters();
                var invokeArgs = new object[ps.Length];
                invokeArgs[0] = null; // null callback — plugin will not invoke the gate
                for (var i = 1; i < ps.Length; i++)
                {
                    if (ps[i].Name == "dataSourceId" &&
                        (ps[i].ParameterType == typeof(int?) || ps[i].ParameterType == typeof(int)) &&
                        context.DataSourceId > 0)
                        invokeArgs[i] = (int?)context.DataSourceId;
                    else
                        invokeArgs[i] = ps[i].DefaultValue;
                }
                return funcFirstCtor.Invoke(invokeArgs);
            }

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

        private static (Type Type, MethodInfo Method)? ResolveTypeAndMethod(string typeName, string methodName)
        {
            var type = FindType(typeName);
            if (type == null) return null;
            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);
            return method != null ? (type, method) : null;
        }

        private static Type FindType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;
            var t = Type.GetType(typeName);
            if (t != null) return t;
            t = BlAssembly.GetType(typeName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }
            // Case-insensitive fallback — catches TypeName casing mismatches in DB seed data
            t = BlAssembly.GetType(typeName, throwOnError: false, ignoreCase: true);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName, throwOnError: false, ignoreCase: true);
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
