using System;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.ProviderSchemaReaders
{
    static class SchemaReaderFactory
    {
        public static SchemaExtendedReader Create(string connectionString, string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentNullException("providerName", "providerName must not be empty");

            SchemaExtendedReader schemaReader = null;
            var type = ProviderToSqlType.Convert(providerName);
            switch (type)
            {
                case EmSqlType.Oracle:
                    schemaReader = new OracleSchemaReader(connectionString, providerName);
                    break;
                case EmSqlType.SqlServer:
                    schemaReader = new SqlAzureOrSqlServerSchemaReader(connectionString, providerName);
                    break;
                case EmSqlType.SqlServerCe:
                    schemaReader = new SqlServerCeSchemaReader(connectionString, providerName);
                    break;
                case EmSqlType.MySql:
                    schemaReader = new MySqlSchemaReader(connectionString, providerName);
                    break;
                case EmSqlType.PostgreSql:
                    schemaReader = new PostgreSqlSchemaReader(connectionString, providerName);
                    break;
                case EmSqlType.Db2:
                    schemaReader = new Db2SchemaReader(connectionString, providerName);
                    break;
                default:
                    //all the other types
                    if (providerName.Equals("Ingres.Client", StringComparison.OrdinalIgnoreCase))
                    {
                        schemaReader = new IngresSchemaReader(connectionString, providerName);
                    }
                    else if (providerName.Equals("iAnyWhere.Data.SQLAnyWhere", StringComparison.OrdinalIgnoreCase))
                    {
                        schemaReader = new SybaseAsaSchemaReader(connectionString, providerName);
                    }
                    else if (providerName.Equals("Sybase.Data.AseClient", StringComparison.OrdinalIgnoreCase))
                    {
                        schemaReader = new SybaseAseSchemaReader(connectionString, providerName);
                    }
                    else if (providerName.Equals("iAnyWhere.Data.UltraLite", StringComparison.OrdinalIgnoreCase))
                    {
                        schemaReader = new SybaseUltraLiteSchemaReader(connectionString, providerName);
                    }
#if NETFRAMEWORK
                    else if (providerName.Equals("System.Data.OleDb", StringComparison.OrdinalIgnoreCase))
                    {
                        schemaReader = new OleDbSchemaReader(connectionString, providerName);
                    }
#endif
                    else if (providerName.Equals("System.Data.VistaDB", StringComparison.OrdinalIgnoreCase))
                    {
                        schemaReader = new VistaDbSchemaReader(connectionString, providerName);
                    }

                    break;
            }
            if (schemaReader == null)
            {
                schemaReader = new SchemaExtendedReader(connectionString, providerName);
            }
            return schemaReader;
        }

        public static SchemaExtendedReader Create(string connectionString, EmSqlType sqlType)
        {
            SchemaExtendedReader schemaReader;

			string runningtimProvider = DatabaseSchema.GetProvideNameBySqlType(sqlType); 


			switch (sqlType)
			{//Oracle.DataAccess.Client
				case EmSqlType.Oracle:
                    schemaReader = new OracleSchemaReader(connectionString, runningtimProvider);
                    break;
                case EmSqlType.SqlServer:
                    schemaReader = new SqlAzureOrSqlServerSchemaReader(connectionString, runningtimProvider);
                    break;
                case EmSqlType.SqlServerCe:
                    schemaReader = new SqlServerCeSchemaReader(connectionString, runningtimProvider);
                    break;
                case EmSqlType.MySql:
                    schemaReader = new MySqlSchemaReader(connectionString, runningtimProvider);
                    break;
                case EmSqlType.PostgreSql:
                    schemaReader = new PostgreSqlSchemaReader(connectionString, runningtimProvider);
                    break;
                case EmSqlType.Db2:
                    schemaReader = new Db2SchemaReader(connectionString, runningtimProvider);
                    break;
                case EmSqlType.SQLite:
                    schemaReader = new SchemaExtendedReader(connectionString, runningtimProvider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("sqlType", "Not a recognized SqlType");
            }
			return schemaReader;

		}
    }
}
