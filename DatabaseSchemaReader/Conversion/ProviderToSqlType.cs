using System;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.Conversion
{
    /// <summary>
    /// Converts a provider invariant name to a SqlType
    /// </summary>
    public static class ProviderToSqlType
    {
        /// <summary>
        /// Converts the specified provider name to a <see cref="EmSqlType"/> or null if unknown.
        /// </summary>
        /// <param name="providerName">Name of the provider.</param>
        /// <returns></returns>
        public static EmSqlType? Convert(string providerName)
        {
            if (string.IsNullOrEmpty(providerName)) return null;

            if (providerName.Equals(SqlClientProviderName, StringComparison.OrdinalIgnoreCase))
                return EmSqlType.SqlServer;
            //if (providerName.Equals(SqlLiteFullName, StringComparison.OrdinalIgnoreCase))
            //    return EmSqlType.SQLite;
            if (providerName.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) != -1)
                return EmSqlType.Oracle;
            if (providerName.Equals(MySqlClienProviderName, StringComparison.OrdinalIgnoreCase))
                return EmSqlType.MySql;
            if (providerName.Equals("Devart.Data.MySql", StringComparison.OrdinalIgnoreCase))
                return EmSqlType.MySql;

            if (providerName.Equals("System.Data.SqlServerCe.4.0", StringComparison.OrdinalIgnoreCase))
                return EmSqlType.SqlServerCe;
            if (providerName.Equals("System.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
                return EmSqlType.SqlServer;
            if (providerName.Equals("Npgsql", StringComparison.OrdinalIgnoreCase) || 
                providerName.Equals("Devart.Data.PostgreSql", StringComparison.OrdinalIgnoreCase))
                return EmSqlType.PostgreSql;
            if (providerName.Equals("IBM.Data.DB2", StringComparison.OrdinalIgnoreCase))
                return EmSqlType.Db2;

            //could be something we don't have a direct syntax for
            return null;
        }



        private static readonly string SqlClientProviderName = "System.Data.SqlClient";
		private static readonly string MySqlClienProviderName = "MySql.Data.MySqlClient";
		





	}
}
