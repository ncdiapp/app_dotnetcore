using System.Diagnostics.CodeAnalysis;

namespace APP.Framework.Communication
{
    /// <summary>
    /// Provide the client identity of the application
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IClientIdentityProvider<T>
        where T : struct, IClientIdentity
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Namespace")]
        string Namespace { get; }

        /// <summary>
        /// Provides the identity.
        /// </summary>
        /// <returns></returns>
        T? ProvideIdentity();

        /// <summary>
        /// Serializes the identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        string SerializeIdentity(T identity);

        /// <summary>
        /// Deserializes the identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns></returns>
        T DeserializeIdentity(string identity);

#if !SILVERLIGHT

        /// <summary>
        /// Registers the identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        void RegisterIdentity(T? identity);

#endif
    }
}