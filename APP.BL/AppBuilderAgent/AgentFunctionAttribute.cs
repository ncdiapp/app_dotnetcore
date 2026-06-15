using System;

namespace App.BL.AppBuilderAgent
{
    /// <summary>
    /// Marks a public method as an AI-callable tool.
    /// Replaces Microsoft.SemanticKernel's [KernelFunction] — no external dependency needed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AgentFunctionAttribute : Attribute
    {
        public string Name        { get; }
        public string Description { get; }

        public AgentFunctionAttribute(string name, string description)
        {
            Name        = name;
            Description = description;
        }
    }

    /// <summary>
    /// Describes a method parameter so the LLM knows what value to supply.
    /// Replaces [Description] on parameters.
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
}
