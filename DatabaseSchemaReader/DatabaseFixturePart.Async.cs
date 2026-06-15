using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
#if NETFRAMEWORK
using System.Data.SqlClient;
#elif NET6_0_OR_GREATER
using Microsoft.Data.SqlClient;
#endif

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Async methods for DatabaseFixturePart.
    /// </summary>
    public partial class DatabaseFixture
    {
        /// <summary>
        /// Opens a connection asynchronously.
        /// </summary>
        public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = DbProviderFactory.CreateConnection();
            conn.ConnectionString = this.ConnectionString;
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return conn;
        }

        /// <summary>
        /// Retrieves a DataTable using a query asynchronously.
        /// </summary>
        public async Task<DataTable> RetriveDataTableAsync(string query, List<DbParameter> parameterList, CancellationToken cancellationToken = default)
        {
            var result = await RetriveDataTableWithErrorHandleAsync(query, parameterList, cancellationToken).ConfigureAwait(false);
            return result.Result;
        }

        /// <summary>
        /// Retrieves a DataTable using a query asynchronously with error handling.
        /// </summary>
        public async Task<(DataTable Result, string ErrorMessage)> RetriveDataTableWithErrorHandleAsync(string query, List<DbParameter> parameterList, CancellationToken cancellationToken = default)
        {
            query = ReformatQuery(query);

            try
            {
                DataTable dt = new DataTable();
                using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
                using (var dbcmd = conn.CreateCommand())
                {
                    dbcmd.CommandType = CommandType.Text;
                    dbcmd.CommandText = query;
                    dbcmd.Parameters.AddRange(parameterList.ToArray());

                    using (var reader = await dbcmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        dt.Load(reader);
                    }
                }
                return (dt, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// Bulk copies a DataTable to a table asynchronously.
        /// </summary>
        public async Task BulkCopyDataTableAsync(DataTable dataTable, string tableName, CancellationToken cancellationToken = default)
        {
            if (SqlServerType == DataSchema.EmSqlType.SqlServer)
            {
#if NETFRAMEWORK || NET6_0_OR_GREATER
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                    using (var bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.BulkCopyTimeout = 300;
                        bulkCopy.DestinationTableName = tableName;
                        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);
                    }
                }
#endif
            }
            else if (SqlServerType == DataSchema.EmSqlType.MySql)
            {
                await BulkInsertMySQLAsync(dataTable, tableName, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task BulkInsertMySQLAsync(DataTable table, string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                // Use synchronous transaction methods for broader compatibility
                using (var tran = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (var cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = tran;
                        cmd.CommandText = $"SELECT * FROM {tableName} limit 0";

                        using (var adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.UpdateBatchSize = 10000;
                            using (var cb = new MySqlCommandBuilder(adapter))
                            {
                                cb.SetAllValues = true;
                                adapter.Update(table);
                                tran.Commit();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves data using a stored procedure asynchronously.
        /// </summary>
        public async Task<DataTable> RetriveStorProcDataTableAsync(string storeProceName, List<DbParameter> parameterList, CancellationToken cancellationToken = default)
        {
            DataTable dt = new DataTable();
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var dbcmd = conn.CreateCommand())
            {
                dbcmd.CommandType = CommandType.StoredProcedure;
                dbcmd.CommandText = storeProceName;
                dbcmd.Parameters.AddRange(parameterList.ToArray());

                using (var reader = await dbcmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    dt.Load(reader);
                }
                return dt;
            }
        }

        /// <summary>
        /// Retrieves a scalar value asynchronously.
        /// </summary>
        public async Task<object> RetriveScalarAsync(string query, List<DbParameter> parameterList, CancellationToken cancellationToken = default)
        {
            query = ReformatQuery(query);

            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteScalarAsync(query, parameterList, conn, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<object> ExecuteScalarAsync(string query, List<DbParameter> parameterList, DbConnection conn, CancellationToken cancellationToken = default)
        {
            query = ReformatQuery(query);
            SetParamterNullAsDbNull(parameterList);

            using (var dbcmd = conn.CreateCommand())
            {
                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;
                dbcmd.Parameters.AddRange(parameterList.ToArray());

                return await dbcmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes a query that returns the result object asynchronously.
        /// </summary>
        public async Task<object> GetQueryResultObjectAsync(string query, CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var dbcmd = conn.CreateCommand())
            {
                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;

                return await dbcmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes a non-query command asynchronously.
        /// </summary>
        public async Task<int> ExecuteNonQueryResultAsync(string query, List<DbParameter> parameterList, CancellationToken cancellationToken = default)
        {
            query = ReformatQuery(query);
            SetParamterNullAsDbNull(parameterList);

            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var dbcmd = conn.CreateCommand())
            {
                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;
                dbcmd.Parameters.AddRange(parameterList.ToArray());

                return await dbcmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks if a table exists asynchronously.
        /// </summary>
        public async Task<bool> IsTableExistAsync(string tableName, CancellationToken cancellationToken = default)
        {
            string query = "";
            if (SqlServerType == DataSchema.EmSqlType.MySql)
            {
                query = $@"SELECT *
                FROM information_schema.tables
                WHERE table_schema = '{this.CurrentOwner}'
                    AND table_name = '{tableName}'
                LIMIT 1";
            }
            else if (SqlServerType == DataSchema.EmSqlType.SqlServer)
            {
                query = $@"SELECT *
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = '{this.CurrentOwner}'
                AND TABLE_NAME = '{tableName}'";
            }
            else
            {
                return false;
            }

            var result = await GetQueryResultObjectAsync(query, cancellationToken).ConfigureAwait(false);
            return result != null && !string.IsNullOrWhiteSpace(result.ToString());
        }

        /// <summary>
        /// Checks if a column exists asynchronously.
        /// </summary>
        public async Task<bool> IsColumnExistAsync(string tableName, string columnName, CancellationToken cancellationToken = default)
        {
            if (!await IsTableExistAsync(tableName, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            string query = "";
            if (SqlServerType == DataSchema.EmSqlType.MySql)
            {
                query = $@"SHOW COLUMNS FROM {tableName} LIKE '{columnName}'";
            }
            else if (SqlServerType == DataSchema.EmSqlType.SqlServer)
            {
                query = $@"select COL_LENGTH ('{tableName}', '{columnName}')";
            }
            else
            {
                return false;
            }

            var result = await GetQueryResultObjectAsync(query, cancellationToken).ConfigureAwait(false);
            return result != null && !string.IsNullOrWhiteSpace(result.ToString());
        }

        /// <summary>
        /// Fills a DataTable using a static connection asynchronously.
        /// </summary>
        public static async Task<DataTable> RetriveDataTableAsync(DbConnection connWithOpenState, string query, List<DbParameter> parameterList, CancellationToken cancellationToken = default)
        {
            DataTable dt = new DataTable();
            return await FillDataTableAsync(connWithOpenState, query, parameterList, dt, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Fills a DataTable using a static connection asynchronously.
        /// </summary>
        public static async Task<DataTable> FillDataTableAsync(DbConnection connWithOpenState, string query, List<DbParameter> parameterList, DataTable dt, CancellationToken cancellationToken = default)
        {
            using (var dbcmd = connWithOpenState.CreateCommand())
            {
                if (dbcmd.GetType().GetProperty("BindByName") != null)
                {
                    dbcmd.GetType().GetProperty("BindByName").SetValue(dbcmd, true, null);
                }

                dbcmd.CommandType = CommandType.Text;
                dbcmd.CommandText = query;
                dbcmd.Parameters.AddRange(parameterList.ToArray());

                await FillDataTableAsync(dt, dbcmd, cancellationToken).ConfigureAwait(false);

                return dt;
            }
        }

        /// <summary>
        /// Fills a DataTable using a command asynchronously.
        /// </summary>
        public static async Task FillDataTableAsync(DataTable dt, DbCommand dbcmd, CancellationToken cancellationToken = default)
        {
            if (dbcmd.GetType().GetProperty("BindByName") != null)
            {
                dbcmd.GetType().GetProperty("BindByName").SetValue(dbcmd, true, null);
            }
            using (var reader = await dbcmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                dt.Load(reader);
            }
        }
    }
}
