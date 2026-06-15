using System;
using System.Data;
using System.Data.Common;

namespace DatabaseSchemaMrg.Utilities
{
    /// <summary>
    /// Tools to help with DbProviderFactory.
    /// This class delegates to DbProviderFactoryHelper for cross-platform compatibility.
    /// </summary>
    public static class FactoryTools
    {
        /// <summary>
        /// Finds the factory for the specified provider name.
        /// </summary>
        /// <param name="providerName">Name of the provider.</param>
        /// <returns>The DbProviderFactory for the specified provider.</returns>
        public static DbProviderFactory GetFactory(string providerName)
        {
            return DbProviderFactoryHelper.GetFactory(providerName);
        }

        /// <summary>
        /// Adds an existing factory. Call this before creating the DatabaseReader or SchemaReader.
        /// Use with care - this sets a global override!
        /// </summary>
        /// <param name="factory">The factory to use for all provider requests.</param>
        /// <exception cref="System.ArgumentNullException">factory is null</exception>
        public static void AddFactory(DbProviderFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            DbProviderFactoryHelper.AddFactory(factory);
        }

        /// <summary>
        /// Clears the manual factory override.
        /// </summary>
        public static void ClearFactory()
        {
            DbProviderFactoryHelper.ClearFactory();
        }

        /// <summary>
        /// List of all the valid Providers. Use the ProviderInvariantName to fill ProviderName property.
        /// </summary>
        /// <returns>A DataTable with provider information.</returns>
        public static DataTable Providers()
        {
            return DbProviderFactoryHelper.GetProviders();
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Registers a provider factory for use in .NET Standard.
        /// </summary>
        /// <param name="providerName">The provider invariant name.</param>
        /// <param name="factory">The provider factory instance.</param>
        public static void RegisterProvider(string providerName, DbProviderFactory factory)
        {
            DbProviderFactoryHelper.RegisterProvider(providerName, factory);
        }
#endif
    }
}
