using System.Linq;


namespace APP.Framework
{
    /// <summary>
    ///   Represents the result of the set action of a property
    /// </summary>
    public class SetPropertyValueResult
    {
        /// <summary>
        ///   Gets the property name.
        /// </summary>
        public string PropertyName { get; internal set; }

        /// <summary>
        ///   Gets the old value of the property.
        /// </summary>
        public object OldValue { get; internal set; }

        /// <summary>
        ///   Gets the new value of the property.
        /// </summary>
        public object NewValue { get; internal set; }

        /// <summary>
        ///   Determine if the new value of the property was different.
        /// </summary>
        public bool PropertyHasChanged { get; internal set; }
    }
}