using System.Linq;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Collections.Specialized;


namespace APP.Framework.Collections
{
    /// <summary>
    ///   Represents an editable collection
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification="Designed to be applied only to a collection.")]
    public interface IEditableCollection : INotifyPropertyChanged, INotifyCollectionChanged, ICollection
    {
        /// <summary>
        ///   Determines whether this instance is modified.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is modified; otherwise, <c>false</c>.
        /// </returns>
        bool IsModified();
    }
}