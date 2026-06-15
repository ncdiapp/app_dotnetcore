using System.ComponentModel;
using System.Linq;


namespace APP.Framework
{
    /// <summary>
    ///   Provides data for the System.ComponentModel.INotifyPropertyChanged.PropertyChanged
    ///   event with the old and new value if applicable.
    /// </summary>
    public class ObservablePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ObservablePropertyChangedEventArgs" /> class.
        /// </summary>
        /// <param name = "result">The result.</param>
        public ObservablePropertyChangedEventArgs(SetPropertyValueResult result)
            : base(result.PropertyName)
        {
            ArgumentValidator.IsNotNull("result", result);
            SetResult = result;
        }

        /// <summary>
        ///   Gets the result of the set of the property.
        /// </summary>
        public SetPropertyValueResult SetResult { get; private set; }
    }
}