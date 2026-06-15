using DatabaseSchemaMrg.DataSchema;
using MySqlConnector;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
#if NETFRAMEWORK
using System.Data.SqlClient;
using System.Web;
#elif NET6_0_OR_GREATER
using Microsoft.Data.SqlClient;
#endif

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Uses <see cref="DatabaseSchemaMrg.SchemaReader"/> to read database schema into schema objects (rather than DataTables).
    /// </summary>
    /// <remarks>
    /// Either load independent objects (list of Tables, StoredProcedures), fuller information (a Table with all Columns, constraints...), or full database schemas (<see cref="ReadAll"/>: all tables, views, stored procedures with all information; the DatabaseSchema object will hook up the relationships). Obviously the fuller versions will be slow on moderate to large databases.
    /// </remarks>
    public partial class DatabaseFixture
    {
        // 3306
        //Base object	   SqlClient object	OleDb Object
        //DbConnection	SqlConnection	OleDbConnection
        //DbCommand	  SqlCommand	OleDbCommand
        //DbParameter 	SqlParameter	OleDbParameter
        //DbConnectionStringBuilder	SqlConnectionStringBuilder	OleDbConnection StringBuilder
        //DbProviderFactory	SqlClientFactory	OleDbFactory

        private static readonly string[] DataTableColumnDataType = new string[] {
            "String" ,

            "TinyInt",
            "Int16",

            "Int32",

            "Int64",




            "Decimal",
            "Double",
            "Single",

            "DateTime",

            "Boolean",

            "Guid",

            "ByteArray",





        };
        public DbConnection OpenConnection()
        {
            var conn = DbProviderFactory.CreateConnection();
            conn.ConnectionString = this.ConnectionString;
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            return conn;
        }

        public static DataTable GetServerInstallPorvider()
        {
            return Utilities.FactoryTools.Providers();
        }

        public DbParameter CreateParameter(string paramName)
        {
            DbParameter aDbParameter = DbProviderFactory.CreateParameter();
            aDbParameter.ParameterName = GetProviderParameter(paramName);
            return aDbParameter;

        }

        /// <summary>
        /// Creates a new database transaction.
        /// IMPORTANT: Caller must dispose BOTH the transaction AND the connection (via transaction.Connection) when done.
        /// Example: using (var tx = CreateDbTransaction()) { ... tx.Commit(); tx.Connection?.Close(); }
        /// </summary>
        public DbTransaction CreateDbTransaction(IsolationLevel? isolationLevel = IsolationLevel.ReadCommitted)
        {
            var connection = DbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            connection.Open();

            return connection.BeginTransaction(isolationLevel.Value);

        }

        /// <summary>
        /// Creates a new database transaction and outputs the connection for proper disposal.
        /// Use this overload to ensure proper resource cleanup.
        /// </summary>
        public DbTransaction CreateDbTransaction(out DbConnection connection, IsolationLevel? isolationLevel = IsolationLevel.ReadCommitted)
        {
            connection = DbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            connection.Open();

            return connection.BeginTransaction(isolationLevel.Value);

        }


        //
        private String GetProviderParameter(string paramName)
        {
            string prefix = "";
            if (SqlServerType.Value == EmSqlType.SqlServer || SqlServerType.Value == EmSqlType.MySql)
            {
                // need to remove  "@"
                if (paramName[0] != '@')
                {
                    prefix = "@";

                }



            }

            //The parameter is preceded by an '@' symbol to indicate it is to be treated as a parameter.
            //https://dev.mysql.com/doc/connector-net/en/connector-net-tutorials-parameters.html 
            //else if (SqlServerType.Value == EmSqlType.MySql)
            //	prefix = "?";

            // https://stackoverflow.com/questions/11048910/oraclecommand-sql-parameters-binding and with respect to oracle use :
            else if (SqlServerType.Value == EmSqlType.Oracle)
            {
                if (paramName[0] != ':')
                {
                    prefix = ":";

                }

            }



            return prefix + paramName;
        }
        public DataTable RetriveDataTableWithErrorHandle(string query, List<DbParameter> parameterList, out string returnErrorMsg)
        {
            returnErrorMsg = "";

            query = ReformatQuery(query);

            //open a connection

            try
            {
                DataTable dt = new DataTable();
                using (DbConnection conn = this.DbProviderFactory.CreateConnection())
                {
                    conn.ConnectionString = this.ConnectionString;
                    conn.Open();



                    using (DbDataAdapter adapter = DbProviderFactory.CreateDataAdapter())
                    {
                        using (var dbcmd = conn.CreateCommand())
                        {

                            dbcmd.CommandType = CommandType.Text;
                            dbcmd.CommandText = query;
                            dbcmd.Parameters.AddRange(parameterList.ToArray());


                            adapter.SelectCommand = dbcmd;

                            adapter.Fill(dt);



                        }
                    }


                }

                return dt;
            }
            catch (WebException ex)
            {
                returnErrorMsg = ex.Message;
                return null;
            }
        }


        public DataTable RetriveDataTable(string query, List<DbParameter> parameterList)
        {
            string returnErrorMsg;
            return RetriveDataTableWithErrorHandle(query, parameterList, out returnErrorMsg);
        }

        public void BulkCopyDataTable(DataTable dataTable, string tableName)
        {
            if (SqlServerType == EmSqlType.SqlServer)
            {
#if NETFRAMEWORK || NET6_0_OR_GREATER
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.BulkCopyTimeout = 300; // Set timeout to 5 minutes
                        bulkCopy.DestinationTableName = tableName;
                        bulkCopy.WriteToServer(dataTable);
                    }
                }
#endif
            }
            else if (SqlServerType == EmSqlType.MySql)
            {
                BulkInsertMySQL(dataTable, tableName);
            }
        }

        private void BulkInsertMySQL(DataTable table, string tableName)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                using (MySqlTransaction tran = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = tran;
                        cmd.CommandText = $"SELECT * FROM " + tableName + " limit 0";

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.UpdateBatchSize = 10000;
                            using (MySqlCommandBuilder cb = new MySqlCommandBuilder(adapter))
                            {
                                cb.SetAllValues = true;
                                adapter.Update(table);
                                tran.Commit();
                            }
                        };
                    }
                }
            }
        }

        public DataTable RetriveStorProcDataTable(string storeProceName, List<DbParameter> parameterList)
        {
            //open a connection
            DataTable dt = new DataTable();
            using (DbConnection conn = this.DbProviderFactory.CreateConnection())
            {
                conn.ConnectionString = this.ConnectionString;
                conn.Open();

                using (var dbcmd = conn.CreateCommand())
                {
                    dbcmd.CommandType = CommandType.StoredProcedure;
                    dbcmd.CommandText = storeProceName;
                    dbcmd.Parameters.AddRange(parameterList.ToArray());
                    using (var dbrdr = dbcmd.ExecuteReader())
                    {
                        dt.Load(dbrdr);
                    }

                    return dt;
                }
            }
        }

        public bool IsQueryContainsKeyword(string queryText, string keyword)
        {
            string pattern = @"\b" + keyword + @"\b(?!\w)";

            return Regex.IsMatch(queryText, pattern, RegexOptions.IgnoreCase);
        }

        public string ValidateQueryText(string queryText)
        {
            string errorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                try
                {


                    if (this.SqlServerType == EmSqlType.SqlServer || this.SqlServerType == EmSqlType.Oracle)
                    {
                        if (IsQueryContainsKeyword(queryText, "WHERE"))
                        {
                            //queryText = Regex.Replace(queryText, "WHERE", "WHERE  1=2 AND", RegexOptions.IgnoreCase);

                            string pattern = @"\bwhere\b(?!\w)";
                            queryText = Regex.Replace(queryText, pattern, "WHERE 1=2 AND", RegexOptions.IgnoreCase);

                        }
                        else if (queryText.IndexOf("GROUP BY", StringComparison.InvariantCultureIgnoreCase) != -1)
                        {


                            queryText = Regex.Replace(queryText, "GROUP BY", " WHERE 1=2 GROUP BY", RegexOptions.IgnoreCase);
                        }
                        else if (queryText.IndexOf("ORDER BY", StringComparison.InvariantCultureIgnoreCase) != -1)
                        {


                            queryText = Regex.Replace(queryText, "ORDER BY", " WHERE 1=2 ORDER BY", RegexOptions.IgnoreCase);
                        }
                        else
                        {
                            queryText = queryText + " WHERE  1=2";
                        }
                    }

                    else if (this.SqlServerType == EmSqlType.MySql)
                    {
                        if (IsQueryContainsKeyword(queryText, "WHERE"))
                        {

                            //queryText = Regex.Replace(queryText, "WHERE", "WHERE  false AND", RegexOptions.IgnoreCase);

                            string pattern = @"\bwhere\b(?!\w)";
                            queryText = Regex.Replace(queryText, pattern, "WHERE  false AND", RegexOptions.IgnoreCase);

                        }
                        else if (queryText.IndexOf("GROUP BY", StringComparison.InvariantCultureIgnoreCase) != -1)
                        {


                            queryText = Regex.Replace(queryText, "GROUP BY", "  WHERE  false GROUP BY", RegexOptions.IgnoreCase);
                        }
                        else if (queryText.IndexOf("ORDER BY", StringComparison.InvariantCultureIgnoreCase) != -1)
                        {


                            queryText = Regex.Replace(queryText, "ORDER BY", "  WHERE  false ORDER BY", RegexOptions.IgnoreCase);
                        }
                        else if (IsQueryContainsKeyword(queryText, "limit"))
                        {
                            string pattern = @"\blimit\b(?!\w)";
                            queryText = Regex.Replace(queryText, pattern, "  WHERE  false  limit", RegexOptions.IgnoreCase);
                        }
                        else
                        {
                            queryText = queryText + " WHERE  false";
                        }

                        //	SELECT
                        //	select_list
                        //FROM
                        //	table_name
                        //ORDER BY

                        //	sort_expression
                        //LIMIT offset, row_count;
                    }






                    this.ExecuteNonQueryResult(queryText, new List<System.Data.Common.DbParameter>());

                }
                catch (Exception ex)
                {
                    //errorMessage = StringLocalizer.Localize("App_InvalidQueryText", "Invalid Query Text");

                    //if (!string.IsNullOrWhiteSpace(ex.Message))
                    //{
                    errorMessage = ex.ToString();
                    //}

                }
            }

            return errorMessage;
        }


        public string MatchSQLServerQuerySyntax(string queryString)
        {
            // need to clear specail token, only need to amtch MS sql server
            // queryString = queryString.Replace("`", "");

            char[] tmpBuffer = queryString.ToCharArray();
            int counter = 0;
            for (int i = 0; i < tmpBuffer.Length; i++)
            {
                char a = tmpBuffer[i];


                if (a == '`')
                {
                    counter++;
                    if (counter % 2 == 1)
                    {
                        tmpBuffer[i] = '[';
                    }
                    else
                    {
                        tmpBuffer[i] = ']';
                    }

                }
            }


            queryString = new string(tmpBuffer);




            // need to get schema
            if (queryString.Contains("*"))
            {
                var result = GetQuerySchemeColumnNameDataType(queryString);

                string allColumn = result.Keys.Aggregate((i, j) => i + "," + j);

                queryString = queryString.Replace("*", $@" {allColumn}  ");

            }

            return queryString;
        }

        public Dictionary<string, string> GetQuerySchemeColumnNameDataType(string query)
        {
            query = ReformatQuery(query);

            string whereClauseAlwasyFalse = " where 1= -1";

            if (this.SqlServerType == EmSqlType.MySql)
            {
                whereClauseAlwasyFalse = "  where false ";
            }


            Dictionary<string, string> dictColumnNameDataType = new Dictionary<string, string>();

            string queryMetaSchme = string.Format(" Select * from ( {0}  ) as subQuery {1} ", query, whereClauseAlwasyFalse);

            DataTable schemaTable = null;

            using (var con = this.OpenConnection())
            {

                //string newQuery = SetupTopQuery(this.SqlServerType.Value, 1, query);
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = queryMetaSchme;
                    using (var rd = cmd.ExecuteReader())
                    {
                        schemaTable = rd.GetSchemaTable();
                    }
                }
            }




            var allColumns = schemaTable.Columns;
            // columns
            foreach (DataRow dataRow in schemaTable.Rows)
            {
                string columnName = dataRow[SchemeDataTableColumnName.ColumnName].ToString();
                string dataType = dataRow[SchemeDataTableColumnName.DataType].ToString();

                dataType = Regex.Replace(dataType, "System.", string.Empty, RegexOptions.IgnoreCase);

                if (dataType == "Byte[]")
                {
                    dataType = "ByteArray";
                }

                if (dataType == "Byte")
                {
                    dataType = "TinyInt";
                }

                if (!DataTableColumnDataType.Contains(dataType))
                {
                    dataType = "Unknown";
                }

                if (!dictColumnNameDataType.ContainsKey(columnName))
                {
                    dictColumnNameDataType.Add(columnName, dataType);
                }
            }

            return dictColumnNameDataType;
        }

        public KeyValuePair<string, string> GetOneTableOneFkCoumnReferenceParentTables(string currentTableLe, string currentfKeyName)
        {


            string queryToExecute = "";


            if (SqlServerType == EmSqlType.SqlServer)
            {

                string whereClause = "";

                whereClause = $@"where tab1.name  = '{currentTableLe}' AND    col1.name ='{currentfKeyName}' ";




                queryToExecute = $@"SELECT  obj.name AS Contraint_Name,    sch.name AS [SchemaName],    tab1.name AS [CURRENT_TABLE],    col1.name AS [CURRENT_COLUMN],   
							tab2.name AS [REFERENCED_TABLE],    col2.name AS [REFERENCED_COLUMN]
                            FROM sys.foreign_key_columns fkc
                            INNER JOIN sys.objects obj    ON obj.object_id = fkc.constraint_object_id
                            INNER JOIN sys.tables tab1    ON tab1.object_id = fkc.parent_object_id
                            INNER JOIN sys.schemas sch    ON tab1.schema_id = sch.schema_id
                            INNER JOIN sys.columns col1    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                            INNER JOIN sys.tables tab2    ON tab2.object_id = fkc.referenced_object_id
                            INNER JOIN sys.columns col2    ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id  {whereClause} ";


            }

            else if (SqlServerType == EmSqlType.MySql)
            {

                string whereClause = "";

                whereClause = $@"  WHERE
                                          REFERENCED_TABLE_SCHEMA = '{CurrentOwner}' AND
                                           TABLE_NAME = '{currentTableLe}'  and    COLUMN_NAME='{currentfKeyName}' ";



                queryToExecute = $@"SELECT 
                                         CONSTRAINT_NAME as Contraint_Name,
                                          '{CurrentOwner}' as SchemaName,

                                          REFERENCED_TABLE_NAME as REFERENCED_TABLE ,
                                          REFERENCED_COLUMN_NAME as  REFERENCED_COLUMN,
                                            TABLE_NAME as CURRENT_TABLE,
                                          COLUMN_NAME as CURRENT_COLUMN
                                        FROM
                                          INFORMATION_SCHEMA.KEY_COLUMN_USAGE {whereClause};
                                        ";


            }

            else if (SqlServerType == EmSqlType.Oracle)
            {

                string whereClause = $@"   WHERE c.constraint_type = 'R'
                                       AND a.table_name ='{currentTableLe}'  AND  a.column_name='{currentfKeyName}";





                queryToExecute = $@"SELECT a.table_name as CURRENT_TABLE, a.column_name as CURRENT_COLUMN, a.constraint_name as Contraint_Name, c.owner, 
                                           -- referenced pk
                                           c.r_owner, r_table_name as REFERENCED_TABLE , c_pk.constraint_name r_pk as REFERENCED_COLUMN

                                      FROM all_cons_columns a
                                      JOIN all_constraints c ON a.owner = c.owner
                                                            AND a.constraint_name = c.constraint_name
                                      JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
                                                               AND c.r_constraint_name = c_pk.constraint_name
									 {whereClause}	 ";



            }
            var resultDatatable = RetriveDataTable(queryToExecute, new List<DbParameter>());

            if (resultDatatable.Rows.Count > 0)
            {
                return new KeyValuePair<string, string>(resultDatatable.Rows[0]["REFERENCED_TABLE"] as string, resultDatatable.Rows[0]["REFERENCED_COLUMN"] as string);

            }
            else
            {
                return new KeyValuePair<string, string>();
            }





        }

        public Dictionary<string, List<DatabaseFkMappingDto>> GetMutipleTableReferenceParentTables(List<string> currentTableList)
        {

            return GetMutipleTableRelatedTables(currentTableList, true);


        }

        public Dictionary<string, List<DatabaseFkMappingDto>> GetMutipleTableReferencedChildTables(List<string> currentTableList)
        {

            return GetMutipleTableRelatedTables(currentTableList, false);


        }

        private Dictionary<string, List<DatabaseFkMappingDto>> GetMutipleTableRelatedTables(List<string> currentTableList, bool isReferenceTable)
        {

            Dictionary<string, List<DatabaseFkMappingDto>> toReturn = new Dictionary<string, List<DatabaseFkMappingDto>>();
            if (currentTableList == null || currentTableList.Count == 0)
            {
                return toReturn;
            }

            currentTableList = currentTableList.Where(o => !string.IsNullOrEmpty(o))
                .Select(o => $@"'{o}'")
                    .Distinct().ToList();

            if (currentTableList.Count == 0)
            {
                return toReturn;
            }

            string pramCurrentTables = currentTableList.Aggregate((i, j) => $@" {i},{j}  ");

            string queryToExecute = "";


            if (SqlServerType == EmSqlType.SqlServer)
            {

                string whereClause = "";
                if (isReferenceTable)
                {
                    whereClause = $@"where tab1.name  in ({pramCurrentTables}) ";


                }
                else // it is referenced tables ( child table)
                {
                    whereClause = $@"where tab2.name  in ({pramCurrentTables}) ";
                }

                queryToExecute = $@"SELECT  obj.name AS Contraint_Name,    sch.name AS [SchemaName],    tab1.name AS [CURRENT_TABLE],    col1.name AS [CURRENT_COLUMN],   
							tab2.name AS [REFERENCED_TABLE],    col2.name AS [REFERENCED_COLUMN]
                            FROM sys.foreign_key_columns fkc
                            INNER JOIN sys.objects obj    ON obj.object_id = fkc.constraint_object_id
                            INNER JOIN sys.tables tab1    ON tab1.object_id = fkc.parent_object_id
                            INNER JOIN sys.schemas sch    ON tab1.schema_id = sch.schema_id
                            INNER JOIN sys.columns col1    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                            INNER JOIN sys.tables tab2    ON tab2.object_id = fkc.referenced_object_id
                            INNER JOIN sys.columns col2    ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id  {whereClause} ";







            }

            else if (SqlServerType == EmSqlType.MySql)
            {

                string whereClause = "";
                if (isReferenceTable)
                {
                    whereClause = $@"  WHERE
                                          REFERENCED_TABLE_SCHEMA = '{CurrentOwner}' AND
                                           TABLE_NAME in ({pramCurrentTables}) ";


                }
                else // it is referenced tables ( child table)
                {
                    whereClause = $@"  WHERE
                                          REFERENCED_TABLE_SCHEMA = '{CurrentOwner}' AND
                                           REFERENCED_TABLE_NAME in ({pramCurrentTables}) ";
                }

                queryToExecute = $@"SELECT 
                                         CONSTRAINT_NAME as Contraint_Name,
                                          '{CurrentOwner}' as SchemaName,

                                          REFERENCED_TABLE_NAME as REFERENCED_TABLE ,
                                          REFERENCED_COLUMN_NAME as  REFERENCED_COLUMN,
                                            TABLE_NAME as CURRENT_TABLE,
                                          COLUMN_NAME as CURRENT_COLUMN
                                        FROM
                                          INFORMATION_SCHEMA.KEY_COLUMN_USAGE {whereClause};
                                        ";


            }

            else if (SqlServerType == EmSqlType.Oracle)
            {
                string whereClause = "";
                if (isReferenceTable)
                {
                    whereClause = $@"   WHERE c.constraint_type = 'R'
                                       AND a.table_name in ({pramCurrentTables}) ";


                }
                else // it is referenced tables ( child table)
                {
                    whereClause = $@"   WHERE c.constraint_type = 'R'
                                       AND r_table_name in ({pramCurrentTables}) ";
                }

                queryToExecute = $@"SELECT a.table_name as CURRENT_TABLE, a.column_name as CURRENT_COLUMN, a.constraint_name as Contraint_Name, c.owner, 
                                           -- referenced pk
                                           c.r_owner, r_table_name as REFERENCED_TABLE , c_pk.constraint_name r_pk as REFERENCED_COLUMN

                                      FROM all_cons_columns a
                                      JOIN all_constraints c ON a.owner = c.owner
                                                            AND a.constraint_name = c.constraint_name
                                      JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
                                                               AND c.r_constraint_name = c_pk.constraint_name
									 {whereClause}	 ";



            }


            if (isReferenceTable)
            {
                var dt = RetriveDataTable(queryToExecute, new List<DbParameter>());
                toReturn = DataTableToEnumerable(dt)
                .GroupBy(o => o["CURRENT_TABLE"].ToString())
                .ToDictionary(o => o.Key, g =>
                {
                    List<DatabaseFkMappingDto> fkDtoList = new List<DatabaseFkMappingDto>();

                    foreach (var r in g.ToList())
                    {
                        DatabaseFkMappingDto fkDto = new DatabaseFkMappingDto();
                        fkDto.ChildTableName = r["CURRENT_TABLE"].ToString();
                        fkDto.ChildTableFkColumnName = r["CURRENT_COLUMN"].ToString();
                        fkDto.ParentTableName = r["REFERENCED_TABLE"].ToString();
                        fkDto.ParentTablePkColumnName = r["REFERENCED_COLUMN"].ToString();

                        if (fkDtoList.FirstOrDefault(o => o.ParentTableName == fkDto.ParentTableName) == null)
                        {
                            fkDtoList.Add(fkDto);
                        }
                    }

                    return fkDtoList;
                });
            }
            else
            {
                var dt = RetriveDataTable(queryToExecute, new List<DbParameter>());
                toReturn = DataTableToEnumerable(dt)
                    .GroupBy(o => o["REFERENCED_TABLE"].ToString())
                    .ToDictionary(o => o.Key, g =>
                    {
                        List<DatabaseFkMappingDto> fkDtoList = new List<DatabaseFkMappingDto>();

                        foreach (var r in g.ToList())
                        {
                            DatabaseFkMappingDto fkDto = new DatabaseFkMappingDto();
                            fkDto.ChildTableName = r["CURRENT_TABLE"].ToString();
                            fkDto.ChildTableFkColumnName = r["CURRENT_COLUMN"].ToString();
                            fkDto.ParentTableName = r["REFERENCED_TABLE"].ToString();
                            fkDto.ParentTablePkColumnName = r["REFERENCED_COLUMN"].ToString();

                            if (fkDtoList.FirstOrDefault(o => o.ChildTableName == fkDto.ChildTableName) == null)
                            {
                                fkDtoList.Add(fkDto);
                            }
                        }

                        return fkDtoList;
                    });
            }




            return toReturn;


        }

        public DataTable GetTablesForeighKeyConstrain(List<string> listTable)
        {

            DataTable toReturn = new DataTable();
            if (listTable == null || listTable.Count == 0)
            {
                return toReturn;
            }

            listTable = listTable.Where(o => !string.IsNullOrEmpty(o))
                .Select(o => $@"'{o}'")
                    .Distinct().ToList();

            string pramTables = listTable.Aggregate((i, j) => $@" {i},{j}  ");
            string queryToExecute = "";


            if (SqlServerType == EmSqlType.SqlServer)
            {
                queryToExecute = $@"SELECT  obj.name AS Contraint_Name,    sch.name AS [SchemaName],    tab1.name AS [CURRENT_TABLE],    col1.name AS [CURRENT_COLUMN],   
							tab2.name AS [REFERENCED_TABLE],    col2.name AS [REFERENCED_COLUMN]
                            FROM sys.foreign_key_columns fkc
                            INNER JOIN sys.objects obj    ON obj.object_id = fkc.constraint_object_id
                            INNER JOIN sys.tables tab1    ON tab1.object_id = fkc.parent_object_id
                            INNER JOIN sys.schemas sch    ON tab1.schema_id = sch.schema_id
                            INNER JOIN sys.columns col1    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                            INNER JOIN sys.tables tab2    ON tab2.object_id = fkc.referenced_object_id
                            INNER JOIN sys.columns col2    ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id

                             where   tab2.name in (" + pramTables + ") " +
                        " and   tab1.name in (" + pramTables + ")";


            }

            else if (SqlServerType == EmSqlType.MySql)
            {
                queryToExecute = $@"SELECT 
                                         CONSTRAINT_NAME as Contraint_Name,
                                          '{CurrentOwner}' as SchemaName,

                                          REFERENCED_TABLE_NAME as REFERENCED_TABLE ,
                                          REFERENCED_COLUMN_NAME as  REFERENCED_COLUMN,
                                            TABLE_NAME as CURRENT_TABLE,
                                          COLUMN_NAME as CURRENT_COLUMN
                                        FROM
                                          INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                                        WHERE
                                          REFERENCED_TABLE_SCHEMA = '{CurrentOwner}' AND
                                           TABLE_NAME in ({pramTables})
                                           and 
                                            REFERENCED_TABLE_NAME in ({pramTables})";


            }

            else if (SqlServerType == EmSqlType.Oracle)
            {
                queryToExecute = $@"SELECT a.table_name as CURRENT_TABLE, a.column_name as CURRENT_COLUMN, a.constraint_name as Contraint_Name, c.owner, 
                                           -- referenced pk
                                           c.r_owner, c_pk.table_name r_table_name as REFERENCED_TABLE , c_pk.constraint_name r_pk as REFERENCED_COLUMN

                                      FROM all_cons_columns a
                                      JOIN all_constraints c ON a.owner = c.owner
                                                            AND a.constraint_name = c.constraint_name
                                      JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
                                                               AND c.r_constraint_name = c_pk.constraint_name
                                     WHERE c.constraint_type = 'R'
                                       AND a.table_name  in  ({pramTables})
                                       AND c_pk.table_name r_table_name  in  ({pramTables})

                                         ";
            }
            toReturn = RetriveDataTable(queryToExecute, new List<DbParameter>());
            return toReturn;


        }

        public static string SetupTopQuery(EmSqlType sqlType, int? topNbValues, string inputQuery)
        {
            if (sqlType == EmSqlType.SqlServer)
            {
                string top = " TOP " + topNbValues.Value.ToString();

                inputQuery = Regex.Replace(inputQuery, "SELECT", " SELECT " + top, RegexOptions.IgnoreCase);
                //"TOP " + topNbValues.Value.ToString() + " "
            }
            else if (sqlType == EmSqlType.Oracle)
            {
                inputQuery = inputQuery + " ROWNUM <= " + topNbValues.Value.ToString();
            }

            else if (sqlType == EmSqlType.MySql)
            {
                inputQuery = inputQuery + " LIMIT  " + topNbValues.Value.ToString();
            }

            return inputQuery;
        }

        public static DataTable RetriveDataTable(DbConnection connWithOpenState, string query, List<DbParameter> parameterList)
        {
            //open a connection
            DataTable dt = new DataTable();
            return FillDataTable(connWithOpenState, query, parameterList, dt);
        }


        public static DataTable FillDataTable(DbConnection connWithOpenState, string query, List<DbParameter> parameterList, DataTable dt)
        {
            using (var dbcmd = connWithOpenState.CreateCommand())
            {
                //Oracle.DataAccess.Client only binds first parameter match unless BindByName=true
                //so we violate LiskovSP (in reflection to avoid dependency on ODP)

                var type = dbcmd.GetType(); ;
                var bind = dbcmd.GetType().GetProperty("BindByName");

                if (dbcmd.GetType().GetProperty("BindByName") != null)
                {
                    dbcmd.GetType().GetProperty("BindByName").SetValue(dbcmd, true, null);
                }


                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;
                dbcmd.Parameters.AddRange(parameterList.ToArray());

                FillDataTable(dt, dbcmd);

                return dt;
            }
        }

        public static void FillDataTable(DataTable dt, DbCommand dbcmd)
        {
            if (dbcmd.GetType().GetProperty("BindByName") != null)
            {
                dbcmd.GetType().GetProperty("BindByName").SetValue(dbcmd, true, null);
            }
            using (var reader = dbcmd.ExecuteReader())
            {
                dt.Load(reader);
            }
        }

        public object RetriveScalar(string query, List<DbParameter> parameterList)
        {
            query = ReformatQuery(query);

            using (DbConnection conn = this.DbProviderFactory.CreateConnection())
            {
                conn.ConnectionString = this.ConnectionString;
                conn.Open();
                return ExecuteScalar(query, parameterList, conn);
            }


        }

        private bool isMySqlQueryForJson(string query)
        {
            query = query.ToLower();

            if (query.Contains("JSON_CONTAINS".ToLower())
                || query.Contains("JSON_EXTRACT".ToLower())
                || query.Contains("->".ToLower())
                || query.Contains("JSON_KEYS".ToLower())
                || query.Contains("JSON_OVERLAPS".ToLower())
                || query.Contains("JSON_SEARCH".ToLower())
                || query.Contains("JSON_VALUE".ToLower())
                || query.Contains("MEMBER OF".ToLower())
                || query.Contains("AS JSON".ToLower())
                || query.Contains("JSON_OBJECT".ToLower())
                || query.Contains("JSON_ARRAY".ToLower())
                || query.Contains("JSON_ARRAYAGG".ToLower())
                || query.Contains("JSON_TABLE".ToLower())

                )
            {
                return true;
            }

            return false;
        }

        public string ReformatQuery(string query)
        {
            if (SqlServerType == EmSqlType.MySql)
            {
                if (isMySqlQueryForJson(query) == false)
                {
                    query = query.Replace("[", "`").Replace("]", "`");
                }
            }
            else if (SqlServerType == EmSqlType.Oracle)
            {
                query = query.Replace("[", @"""").Replace("]", @"""");
            }

            return query;
        }

        public object ExecuteTransScalar(string query, List<DbParameter> parameterList, DbTransaction trans)
        {

            // trans.Connection.
            query = ReformatQuery(query);


            var conn = trans.Connection;

            SetParamterNullAsDbNull(parameterList);

            using (var dbcmd = conn.CreateCommand())
            {
                dbcmd.Transaction = trans;

                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;

                Array parameters = parameterList.GroupBy(o => o.ParameterName).ToDictionary(o => o.Key, g => g.ToList().First()).Select(o => o.Value).ToArray();
                dbcmd.Parameters.AddRange(parameters);

                return dbcmd.ExecuteScalar();
            }
        }

        private static void SetParamterNullAsDbNull(List<DbParameter> parameterList)
        {
            foreach (var param in parameterList)
            {
                if (param.Value == null)
                {
                    param.Value = DBNull.Value;
                }
            }
        }

        /// <summary>
        /// Converts a DataTable to an IEnumerable of DataRow.
        /// This is a cross-platform replacement for DataTableExtensions.AsEnumerable().
        /// </summary>
        private static IEnumerable<DataRow> DataTableToEnumerable(DataTable table)
        {
            if (table == null)
                yield break;
            foreach (DataRow row in table.Rows)
            {
                yield return row;
            }
        }

        private object ExecuteScalar(string query, List<DbParameter> parameterList, DbConnection conn)
        {
            query = ReformatQuery(query);
            SetParamterNullAsDbNull(parameterList);
            using (var dbcmd = conn.CreateCommand())
            {
                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;
                dbcmd.Parameters.AddRange(parameterList.ToArray());

                return dbcmd.ExecuteScalar();
            }
        }


        public bool IsColumnExist(string tabalName, string columnName)
        {
            if (IsTableExist(tabalName))
            {
                string query = "";
                if (SqlServerType == EmSqlType.MySql)
                {
                    query = $@"SHOW COLUMNS FROM {tabalName} LIKE '{columnName}'";
                }
                else if (SqlServerType == EmSqlType.SqlServer)
                {
                    query = $@"select COL_LENGTH ('{tabalName}', '{columnName}')";
                }
                else
                {
                    return false;
                }

                object result = GetQueryResultObject(query);

                if (result != null)
                {
                    if (string.IsNullOrWhiteSpace(result.ToString()))
                    {
                        // MSSQL result == {}
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    // MYSQL result == null
                    return false;
                }

            }

            return false;
        }


        public bool IsTableExist(string tabalName)
        {
            string query = "";
            if (SqlServerType == EmSqlType.MySql)
            {
                query = $@"SELECT * 
                FROM information_schema.tables
                WHERE table_schema = '{this.CurrentOwner}' 
                    AND table_name = '{tabalName}'
                LIMIT 1";
            }
            else if (SqlServerType == EmSqlType.SqlServer)
            {
                query = $@"SELECT *
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = '{this.CurrentOwner}'
                AND TABLE_NAME = '{tabalName}'";
            }
            else
            {
                return false;
            }

            object result = GetQueryResultObject(query);

            if (result != null)
            {
                if (string.IsNullOrWhiteSpace(result.ToString()))
                {
                    // MSSQL result == {}
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                // MYSQL result == null
                return false;
            }

        }

        public object GetQueryResultObject(string query)
        {
            using (DbConnection conn = this.DbProviderFactory.CreateConnection())
            {
                conn.ConnectionString = this.ConnectionString;
                conn.Open();

                using (var dbcmd = conn.CreateCommand())
                {
                    dbcmd.CommandType = CommandType.Text;
                    dbcmd.CommandText = query;


                    object result = dbcmd.ExecuteScalar();
                    return result;
                }
            }
        }
        /// <summary>
        ///  return number of row affect
        /// </summary>
        /// <param name="currentTable"></param>
        /// <param name="currentTableFkCoumn"></param>
        /// <param name="currentKeyColumn"></param>
        /// <param name="referenceTableName"></param>
        /// <param name="referencePkColumnName"></param>
        /// <returns></returns>
        public int SetForeceUpdateCurrentTableForeighKeyAsNull(string currentTable, string currentTableFkCoumn, string currentKeyColumn, string referenceTableName, string referencePkColumnName)
        {
            string removeNotExstingIdInReference = $@"
                                   update [{this.CurrentOwner}].[{currentTable}]
                                 set [{currentTableFkCoumn}]=null 
 
                                 where  [{currentTableFkCoumn}]  not in (
 
                                 select [{referencePkColumnName}] from [{CurrentOwner}].[{referenceTableName}]
                                 ) and [{currentKeyColumn}] > 0  ;


                           ";


            return ExecuteNonQueryResult(removeNotExstingIdInReference, new List<DbParameter>());


        }

        public int ExecuteNonQueryResult(string query, List<DbParameter> parameterList)
        {
            query = ReformatQuery(query);

            SetParamterNullAsDbNull(parameterList);
            using (DbConnection conn = this.DbProviderFactory.CreateConnection())
            {
                conn.ConnectionString = this.ConnectionString;
                conn.Open();

                using (var dbcmd = conn.CreateCommand())
                {
                    dbcmd.CommandType = CommandType.Text;
                    dbcmd.CommandText = query;
                    dbcmd.Parameters.AddRange(parameterList.ToArray());

                    return dbcmd.ExecuteNonQuery();
                }
            }
        }

    }


    internal static class SchemeDataTableColumnName
    {
        public static readonly string ColumnName = "ColumnName";
        public static readonly string DataType = "DataType";
        public static readonly string ColumnSize = "ColumnSize";
        public static readonly string NumericPrecision = "NumericPrecision";
        public static readonly string NumericScale = "NumericScale";
        public static readonly string IsKey = "IsKey";
        public static readonly string AllowDBNull = "AllowDBNull";


    }

}