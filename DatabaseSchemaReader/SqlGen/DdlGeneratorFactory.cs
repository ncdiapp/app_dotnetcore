using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.SqlGen
{
    /// <summary>
    /// Generate Ddl
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ddl")]
    public class DdlGeneratorFactory
    {
        private readonly EmSqlType _sqlType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DdlGeneratorFactory"/> class.
        /// </summary>
        /// <param name="sqlType">Type of the SQL.</param>
        public DdlGeneratorFactory(EmSqlType sqlType)
        {
            _sqlType = sqlType;
        }

        /// <summary>
        /// Creates a table DDL generator.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        public ITableGenerator TableGenerator(DatabaseTable table)
        {
            switch (_sqlType)
            {
                case EmSqlType.SqlServer:
                    return new SqlServer.MsSqlTableGenerator(table);
                case EmSqlType.Oracle:
                    return new Oracle.OracleTableGenerator(table);
                case EmSqlType.MySql:
                    return new MySql.MysqlTableGenerator(table);
                case EmSqlType.SQLite:
                    return new SqLite.SqlLiteTableGenerator(table);
                case EmSqlType.SqlServerCe:
                    return new SqlServerCe.SqlCeTableGenerator(table);
                case EmSqlType.PostgreSql:
                    return new PostgreSql.PostGreTableGenerator(table);
                case EmSqlType.Db2:
                    return new Db2.DB2TableGenerator(table);
            }
            return null;
        }

        /// <summary>
        /// Creates a DDL generator for all tables.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        public ITablesGenerator AllTablesGenerator(DatabaseSchema schema)
        {
            switch (_sqlType)
            {
                case EmSqlType.SqlServer:
                    return new SqlServer.TablesGenerator(schema);
                case EmSqlType.Oracle:
                    return new Oracle.TablesGenerator(schema);
                case EmSqlType.MySql:
                    return new MySql.TablesGenerator(schema);
                case EmSqlType.SQLite:
                    return new SqLite.TablesGenerator(schema);
                case EmSqlType.SqlServerCe:
                    return new SqlServerCe.TablesGenerator(schema);
                case EmSqlType.PostgreSql:
                    return new PostgreSql.TablesGenerator(schema);
                case EmSqlType.Db2:
                    return new Db2.TablesGenerator(schema);
            }
            return null;
        }

        /// <summary>
        /// Creates a stored procedure generator for a table.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        public IProcedureGenerator ProcedureGenerator(DatabaseTable table)
        {
            switch (_sqlType)
            {
                case EmSqlType.SqlServer:
                    return new SqlServer.ProcedureGenerator(table);
                case EmSqlType.Oracle:
                    return new Oracle.ProcedureGenerator(table);
                case EmSqlType.MySql:
                    return new MySql.ProcedureGenerator(table);
                case EmSqlType.SQLite:
                    return null; //no stored procedures in SqlLite
                case EmSqlType.SqlServerCe:
                    return null; //no stored procedures in SqlServerCE
                case EmSqlType.PostgreSql:
                    return null; //for now
                case EmSqlType.Db2:
                    return new Db2.ProcedureGenerator(table);
            }
            return null;
        }

        /// <summary>
        /// Internal method to find constraint writer
        /// </summary>
        /// <param name="databaseTable">The database table.</param>
        /// <returns></returns>
        internal ConstraintWriterBase ConstraintWriter(DatabaseTable databaseTable)
        {
            switch (_sqlType)
            {
                case EmSqlType.SqlServer:
                    return new SqlServer.ConstraintWriter(databaseTable);
                case EmSqlType.Oracle:
                    return new Oracle.ConstraintWriter(databaseTable);
                case EmSqlType.MySql:
                    return new MySql.ConstraintWriter(databaseTable);
                case EmSqlType.SQLite:
                    return null; //can't alter constraints after creating table
                case EmSqlType.SqlServerCe:
                    return new SqlServer.ConstraintWriter(databaseTable);
                case EmSqlType.PostgreSql:
                    return new PostgreSql.ConstraintWriter(databaseTable);
                case EmSqlType.Db2:
                    return new Db2.ConstraintWriter(databaseTable);
            }
            return null;
        }

        /// <summary>
        /// Creates a migration generator (Create Tables, add/alter/drop columns)
        /// </summary>
        /// <returns></returns>
        public IMigrationGenerator MigrationGenerator()
        {
            switch (_sqlType)
            {
                case EmSqlType.SqlServer:
                    return new SqlServer.SqlServerMigrationGenerator();
                case EmSqlType.Oracle:
                    return new Oracle.OracleMigrationGenerator();
                case EmSqlType.MySql:
                    return new MySql.MySqlMigrationGenerator();
                case EmSqlType.SQLite:
                    return new SqLite.SqLiteMigrationGenerator();
                case EmSqlType.SqlServerCe:
                    return new SqlServerCe.SqlServerCeMigrationGenerator();
                case EmSqlType.PostgreSql:
                    return new PostgreSql.PostgreSqlMigrationGenerator();
                case EmSqlType.Db2:
                    return new Db2.Db2MigrationGenerator();
            }
            return null;
        }
    }
}
