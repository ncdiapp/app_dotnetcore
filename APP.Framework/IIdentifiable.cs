using System.Linq;


namespace APP.Framework
{
    /// <summary>
    ///   Define the contract of a class that have an unique identifier
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        ///   Gets the id.
        /// </summary>
        /// <value>The id.</value>
        object Id { get; }
    }
}