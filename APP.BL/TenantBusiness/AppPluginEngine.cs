using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;
using APP.Framework.Plugin;

namespace App.BL;

/// <summary>
/// Central runtime loader for all APP platform plugins.
///
/// Loading strategy (in order):
///   1. IAppPlugin — the plugin class implements IAppPlugin; the engine instantiates it
///      and calls Execute(input, context). MethodName is carried in PluginContext so the
///      plugin can dispatch internally. This is the preferred path for all new modules.
///
///   2. Static method reflection fallback — for legacy plugins that pre-date IAppPlugin.
///      The engine looks up the static method by name and invokes it with [input] as the
///      parameter array. These plugins have no access to PluginContext.
///
/// DLLs are loaded from ExternalDllRepository\ relative to the AppDomain base.
/// Assembly.LoadFrom keeps the loaded assembly in the AppDomain for the process lifetime;
/// subsequent calls to the same DLL reuse the cached assembly.
/// </summary>
public static class AppPluginEngine
{
    public static readonly string DllRoot =
        AppDomain.CurrentDomain.BaseDirectory + @"ExternalDllRepository\";

    private static readonly HashSet<string> IgnoredMethodNames = new(StringComparer.Ordinal)
    {
        "Execute", "Equals", "GetHashCode", "GetType", "ToString", "Finalize", "MemberwiseClone"
    };

    /// <summary>
    /// Loads a plugin assembly, resolves the target type, and invokes the named operation.
    /// </summary>
    public static TResult Invoke<TResult>(
        string assemblyName, string typeName, string methodName, object? input)
        where TResult : class
    {
        var context = PluginContext.FromServerContext(methodName);

        var path = Path.Combine(DllRoot, assemblyName + ".dll");
        var assembly = Assembly.LoadFrom(path);

        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException(
                $"Plugin type '{typeName}' not found in assembly '{assemblyName}'.");

        if (typeof(IAppPlugin).IsAssignableFrom(type))
        {
            var plugin = Activator.CreateInstance(type) as IAppPlugin
                ?? throw new InvalidOperationException(
                    $"Could not instantiate plugin type '{typeName}'. Ensure it has a public parameterless constructor.");

            return plugin.Execute(input, context) as TResult
                ?? throw new InvalidOperationException(
                    $"Plugin '{typeName}.Execute' returned null or a type incompatible with '{typeof(TResult).Name}'.");
        }

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"No public static method '{methodName}' found on '{typeName}' in '{assemblyName}'.");

        return method.Invoke(null, [input]) as TResult
            ?? throw new InvalidOperationException(
                $"Static plugin method '{typeName}.{methodName}' returned null or a type incompatible with '{typeof(TResult).Name}'.");
    }

    /// <summary>
    /// Scans ExternalDllRepository\*.dll and returns registerable method candidates
    /// as AppExternalMethodRegisterDto (same shape as the register table).
    /// </summary>
    public static List<AppExternalMethodRegisterDto> DiscoverAvailableMethods()
    {
        var results = new List<AppExternalMethodRegisterDto>();

        if (!Directory.Exists(DllRoot))
        {
            return results;
        }

        foreach (var dllPath in Directory.GetFiles(DllRoot, "*.dll"))
        {
            try
            {
                DiscoverFromAssembly(dllPath, results);
            }
            catch
            {
                // Skip unloadable / non-plugin DLLs (native, wrong TFM, missing deps).
            }
        }

        return results
            .GroupBy(o => $"{o.AssemblyName}|{o.TypeName}|{o.MethodName}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(o => o.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.TypeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.MethodName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void DiscoverFromAssembly(string dllPath, List<AppExternalMethodRegisterDto> results)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(dllPath);
        var assembly = Assembly.LoadFrom(dllPath);

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type == null || type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            if (typeof(IAppPlugin).IsAssignableFrom(type))
            {
                DiscoverIAppPluginMethods(assemblyName, type, results);
                continue;
            }

            DiscoverLegacyStaticMethods(assemblyName, type, results);
        }
    }

    private static void DiscoverIAppPluginMethods(
        string assemblyName, Type type, List<AppExternalMethodRegisterDto> results)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var method in type.GetMethods(flags))
        {
            if (IgnoredMethodNames.Contains(method.Name) || method.IsSpecialName)
            {
                continue;
            }

            if (!IsPluginOperationReturnType(method.ReturnType))
            {
                continue;
            }

            results.Add(CreateCandidate(
                assemblyName,
                type.FullName ?? type.Name,
                method.Name,
                BuildInputParameterListForIAppPlugin(method)));
        }
    }

    private static void DiscoverLegacyStaticMethods(
        string assemblyName, Type type, List<AppExternalMethodRegisterDto> results)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (IgnoredMethodNames.Contains(method.Name) || method.IsSpecialName)
            {
                continue;
            }

            if (!IsPluginOperationReturnType(method.ReturnType) && method.ReturnType != typeof(DataTable))
            {
                continue;
            }

            results.Add(CreateCandidate(
                assemblyName,
                type.FullName ?? type.Name,
                method.Name,
                BuildInputParameterListFromSignature(method)));
        }
    }

    private static AppExternalMethodRegisterDto CreateCandidate(
        string assemblyName, string typeName, string methodName, string inputParameterList)
    {
        return new AppExternalMethodRegisterDto
        {
            MethodDisplayName = HumanizeMethodName(methodName),
            AssemblyName = assemblyName,
            TypeName = typeName,
            MethodName = methodName,
            InputParameterList = inputParameterList
        };
    }

    private static bool IsPluginOperationReturnType(Type returnType)
    {
        if (returnType == typeof(DataTable))
        {
            return true;
        }

        var name = returnType.IsGenericType
            ? returnType.GetGenericTypeDefinition().Name
            : returnType.Name;

        return name.StartsWith("OperationCallResult", StringComparison.Ordinal);
    }

    private static string BuildInputParameterListForIAppPlugin(MethodInfo method)
    {
        var parameters = method.GetParameters();
        foreach (var p in parameters)
        {
            if (string.Equals(p.ParameterType.Name, "AppMasterDetailDto", StringComparison.Ordinal)
                || (p.ParameterType.FullName?.Contains("AppMasterDetailDto", StringComparison.Ordinal) ?? false))
            {
                return "AppMasterDetailDto";
            }
        }

        return parameters.Length == 0
            ? "AppMasterDetailDto"
            : BuildInputParameterListFromSignature(method);
    }

    private static string BuildInputParameterListFromSignature(MethodInfo method)
    {
        var names = method.GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToArray();
        return names.Length == 0 ? string.Empty : string.Join("|", names);
    }

    private static string HumanizeMethodName(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return methodName;
        }

        return Regex.Replace(methodName, "([a-z])([A-Z])", "$1 $2");
    }
}
