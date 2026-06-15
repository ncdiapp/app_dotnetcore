namespace APP.Framework
{
    /// <summary>
    /// Represents a key of an enum with a localized value and description
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumLookup<T>
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public T Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the value description.
        /// </summary>
        /// <value>
        /// The value description.
        /// </value>
        public string ValueDescription { get; set; }
    }
}