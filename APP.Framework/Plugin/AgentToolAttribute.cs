using System;

namespace APP.Framework.Plugin;

/// <summary>
/// Marks a public method as an AI-callable tool.
/// Replaces the old [AgentFunction] attribute from App.BL.AppBuilderAgent namespace.
/// Used by BuiltInToolExecutor to reflect methods from plugin classes at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AgentToolAttribute : Attribute
{
    public string Name        { get; }
    public string Description { get; }

    public AgentToolAttribute(string name, string description)
    {
        Name        = name;
        Description = description;
    }
}

/// <summary>
/// Describes a method parameter so the LLM knows what value to supply.
/// Replaces the old [AgentParam] attribute from App.BL.AppBuilderAgent namespace.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class AgentParamAttribute : Attribute
{
    public string Description { get; }
    public bool   IsRequired  { get; }

    public AgentParamAttribute(string description, bool isRequired = false)
    {
        Description = description;
        IsRequired  = isRequired;
    }
}
