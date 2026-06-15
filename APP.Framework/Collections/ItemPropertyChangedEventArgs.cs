using System.ComponentModel;
using System.Linq;


namespace APP.Framework.Collections
{
    /// <summary>
    ///   Provides data for the ItemPropertyChanged event
    /// </summary>
    public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ItemPropertyChangedEventArgs" /> class.
        /// </summary>
        /// <param name = "item">The item.</param>
        /// <param name = "propertyName">Name of the property.</param>
        public ItemPropertyChangedEventArgs(object item, string propertyName)
            : base(propertyName)
        {
            Item = item;
        }

        /// <summary>
        ///   Gets the item.
        /// </summary>
        public object Item { get; private set; }
    }
}