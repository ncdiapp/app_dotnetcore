using System;
using System.Linq;


namespace APP.Framework
{
    /// <summary>
    ///   Marker attribute use to detect an editable object
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class EditableMemberAttribute : Attribute
    {
    }
}