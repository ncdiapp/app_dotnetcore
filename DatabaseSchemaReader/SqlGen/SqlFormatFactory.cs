using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.SqlGen
{
    static class SqlFormatFactory
    {
        public static ISqlFormatProvider Provider(EmSqlType sqlType)
        {
            switch (sqlType)
            {
                case EmSqlType.Oracle:
                    return new Oracle.SqlFormatProvider();
                case EmSqlType.MySql:
                    return new MySql.SqlFormatProvider();
                case EmSqlType.SQLite:
                    return new SqLite.SqlFormatProvider();
                case EmSqlType.PostgreSql:
                    return new PostgreSql.SqlFormatProvider();
                case EmSqlType.Db2:
                    return new Db2.SqlFormatProvider();
                case EmSqlType.SqlServerCe:
                    return new SqlServerCe.SqlServerCeFormatProvider();
                default:
                    return new SqlServer.SqlFormatProvider();
            }
        }
    }
}
