using System.ComponentModel;
using System.Linq;


namespace APP.Framework
{
    /// <summary>
    ///   Represents an editable object with tracking information
    /// </summary>
    public interface IEditableObject : IIdentifiable, INotifyPropertyChanged
    {
        /// <summary>
        ///   Gets the is new.
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        ///   Determines whether any related entities are modified (Including the current instance).
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any related entities are modified; otherwise, <c>false</c>.
        /// </returns>
        bool IsRelatedEntitiesModified();

		object NewItemTrackGuiId { get; set; }
    }
}