using System;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.SqlGen
{
    static class DataTypeMappingFactory
    {
        public static DataTypeMapper DataTypeMapper(DatabaseTable databaseTable)
        {
            if (databaseTable == null) throw new ArgumentNullException("databaseTable", "databaseTable must not be null");
            var schema = databaseTable.DatabaseSchema;
            EmSqlType? type = EmSqlType.SqlServer;
            if (schema != null)
                type = ProviderToSqlType.Convert(schema.Provider);
            if (!type.HasValue) type = EmSqlType.SqlServer;
            return DataTypeMapper(type.Value);
        }

        public static DataTypeMapper DataTypeMapper(EmSqlType sqlType)
        {
            switch (sqlType)
            {
                case EmSqlType.SqlServer:
                    return new SqlServer.SqlServerDataTypeMapper();
                case EmSqlType.Oracle:
                    return new Oracle.OracleDataTypeMapper();
                case EmSqlType.MySql:
                    return new MySql.MySqlDataTypeMapper();
                case EmSqlType.SQLite:
                    return new SqLite.SqLiteDataTypeMapper();
                case EmSqlType.SqlServerCe:
                     return new SqlServerCe.SqlServerCeDataTypeMapper();
                case EmSqlType.PostgreSql:
                     return new PostgreSql.PostgreSqlDataTypeMapper();
                case EmSqlType.Db2:
                     return new Db2.Db2DataTypeMapper();
                default:
                    throw new ArgumentOutOfRangeException("sqlType");
            }
        }
    }
}
