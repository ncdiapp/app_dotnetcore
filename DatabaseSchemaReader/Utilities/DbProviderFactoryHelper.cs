using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DatabaseSchemaMrg.Utilities
{
    /// <summary>
    /// Helper class for DbProviderFactory that handles differences between .NET Framework and .NET Core.
    /// </summary>
    public static class DbProviderFactoryHelper
    {
        private static DbProviderFactory _manualProviderFactory;
        private static readonly object _lock = new object();

#if NET6_0_OR_GREATER
        private static bool _defaultProvidersRegistered = false;
#endif

        /// <summary>
        /// Gets a DbProviderFactory for the specified provider name.
        /// </summary>
        /// <param name="providerName">Name of the provider (e.g., "System.Data.SqlClient").</param>
        /// <returns>The DbProviderFactory for the specified provider.</returns>
        public static DbProviderFactory GetFactory(string providerName)
        {
            // Check for manual override first
            if (_manualProviderFactory != null)
                return _manualProviderFactory;

            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentNullException(nameof(providerName));

#if NET6_0_OR_GREATER
            EnsureDefaultProvidersRegistered();
#endif
            return DbProviderFactories.GetFactory(providerName);
        }

        /// <summary>
        /// Adds a manual factory override. Call this before creating the DatabaseReader or SchemaReader.
        /// </summary>
        /// <param name="factory">The factory to use for all provider requests.</param>
        public static void AddFactory(DbProviderFactory factory)
        {
            _manualProviderFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Clears the manual factory override.
        /// </summary>
        public static void ClearFactory()
        {
            _manualProviderFactory = null;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Registers a provider factory for use in .NET Core.
        /// </summary>
        /// <param name="providerName">The provider invariant name.</param>
        /// <param name="factory">The provider factory instance.</param>
        public static void RegisterProvider(string providerName, DbProviderFactory factory)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentNullException(nameof(providerName));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (_lock)
            {
                if (!DbProviderFactories.TryGetFactory(providerName, out _))
                {
                    DbProviderFactories.RegisterFactory(providerName, factory);
                }
            }
        }

        /// <summary>
        /// Ensures default providers are registered for common database types.
        /// </summary>
        private static void EnsureDefaultProvidersRegistered()
        {
            if (_defaultProvidersRegistered)
                return;

            lock (_lock)
            {
                if (_defaultProvidersRegistered)
                    return;

                // Register common providers by loading them dynamically
                TryRegisterProviderByType("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient");
                TryRegisterProviderByType("System.Data.SqlClient", "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient");
                TryRegisterProviderByType("MySql.Data.MySqlClient", "MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data");
                TryRegisterProviderByType("Oracle.ManagedDataAccess.Client", "Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess.Core");
                TryRegisterProviderByType("Npgsql", "Npgsql.NpgsqlFactory, Npgsql");
                TryRegisterProviderByType("IBM.Data.DB2", "IBM.Data.DB2.Core.DB2Factory, IBM.Data.DB2.Core");
                TryRegisterProviderByType("Microsoft.Data.Sqlite", "Microsoft.Data.Sqlite.SqliteFactory, Microsoft.Data.Sqlite");

                _defaultProvidersRegistered = true;
            }
        }

        /// <summary>
        /// Attempts to register a provider by loading its factory type.
        /// </summary>
        private static void TryRegisterProviderByType(string providerName, string factoryTypeName)
        {
            try
            {
                // Skip if already registered
                if (DbProviderFactories.TryGetFactory(providerName, out _))
                    return;

                var factoryType = Type.GetType(factoryTypeName, throwOnError: false);
                if (factoryType != null)
                {
                    var instanceField = factoryType.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (instanceField != null)
                    {
                        var factory = instanceField.GetValue(null) as DbProviderFactory;
                        if (factory != null)
                        {
                            DbProviderFactories.RegisterFactory(providerName, factory);
                        }
                    }
                }
            }
            catch
            {
                // Provider assembly not available, skip
            }
        }
#endif

        /// <summary>
        /// Gets a DataTable listing all registered providers.
        /// </summary>
        /// <returns>A DataTable with provider information.</returns>
        public static DataTable GetProviders()
        {
#if NET6_0_OR_GREATER
            EnsureDefaultProvidersRegistered();
#endif
            return DbProviderFactories.GetFactoryClasses();
        }
    }
}
