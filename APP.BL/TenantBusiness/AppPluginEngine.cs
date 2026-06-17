using System;
using System.IO;
using System.Reflection;
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

    /// <summary>
    /// Loads a plugin assembly, resolves the target type, and invokes the named operation.
    /// </summary>
    /// <typeparam name="TResult">Expected return type (cast from object? after invocation).</typeparam>
    /// <param name="assemblyName">DLL filename without extension (e.g. "APP.TechPack").</param>
    /// <param name="typeName">Fully-qualified class name.</param>
    /// <param name="methodName">Method name; used for IAppPlugin dispatch and static reflection.</param>
    /// <param name="input">Input passed to the plugin (typically AppMasterDetailDto).</param>
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

        // ── IAppPlugin path ────────────────────────────────────────────────────
        if (typeof(IAppPlugin).IsAssignableFrom(type))
        {
            var plugin = Activator.CreateInstance(type) as IAppPlugin
                ?? throw new InvalidOperationException(
                    $"Could not instantiate plugin type '{typeName}'. Ensure it has a public parameterless constructor.");

            return plugin.Execute(input, context) as TResult
                ?? throw new InvalidOperationException(
                    $"Plugin '{typeName}.Execute' returned null or a type incompatible with '{typeof(TResult).Name}'.");
        }

        // ── Static method fallback (legacy) ────────────────────────────────────
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"No public static method '{methodName}' found on '{typeName}' in '{assemblyName}'.");

        return method.Invoke(null, [input]) as TResult
            ?? throw new InvalidOperationException(
                $"Static plugin method '{typeName}.{methodName}' returned null or a type incompatible with '{typeof(TResult).Name}'.");
    }
}
