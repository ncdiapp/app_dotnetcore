using System;
using System.Linq;


namespace APP.Framework.Globalization
{
    /// <summary>
    ///   Contains all data required to localize a string
    /// </summary>
    public class LocalizeStringEventArgs : EventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "LocalizeStringEventArgs" /> class.
        /// </summary>
        /// <param name = "key">The key.</param>
        /// <exception cref = "System.ArgumentNullException">Raised when the key is null</exception>
        public LocalizeStringEventArgs(string key)
        {
            ArgumentValidator.IsNotNullOrEmpty("key", key);
            Key = key;
            DefaultValue = key;
            Value = key;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LocalizeStringEventArgs" /> class.
        /// </summary>
        /// <param name = "key">The key.</param>
        /// <param name = "defaultValue">The default value.</param>
        /// <exception cref = "System.ArgumentNullException">Raised when the key is null</exception>
        public LocalizeStringEventArgs(string key, string defaultValue)
        {
            ArgumentValidator.IsNotNullOrEmpty("key", key);
            Key = key;
            DefaultValue = defaultValue;
            Value = defaultValue;
        }

        /// <summary>
        ///   Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; private set; }

        /// <summary>
        ///   Gets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public string DefaultValue { get; private set; }

        /// <summary>
        ///   Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }
    }
}