namespace APP.Framework.Plugin;

/// <summary>
/// Contract for all APP platform plugins.
///
/// A plugin class implements this interface and is registered in AppExternalMethodRegister.
/// The platform instantiates the class and calls Execute — no static methods required.
///
/// The input/output are typed as object so this interface carries no dependency on
/// specific DTO assemblies (AppMasterDetailDto, etc.). The platform engine casts the
/// result to the expected concrete type after invocation.
///
/// Multi-method plugins dispatch internally using context.MethodName, which is sourced
/// from the AppExternalMethodRegister.MethodName column.
/// </summary>
public interface IAppPlugin
{
    object? Execute(object? input, PluginContext context);
}
