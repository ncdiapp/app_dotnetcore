using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Async methods for SchemaExtendedReader.
    /// </summary>
    internal partial class SchemaExtendedReader
    {
        /// <summary>
        /// Get all data for a specified table name asynchronously.
        /// </summary>
        public override async Task<DataSet> TableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var ds = new DataSet();
            ds.Locale = CultureInfo.InvariantCulture;
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                await LoadTableAsync(tableName, ds, conn, cancellationToken).ConfigureAwait(false);
                if (ds.Tables.Count == 0) return null;
                if (string.IsNullOrEmpty(Owner))
                {
                    Owner = SchemaConverter.FindSchema(ds.Tables[ColumnsCollectionName]);
                }

                var pkDataTable = await PrimaryKeysAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
                pkDataTable.TableName = PrimaryKeysCollectionName;
                ds.Tables.Add(pkDataTable);

                var fkDataTable = await ForeignKeysAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
                fkDataTable.TableName = ForeignKeyColumnsCollectionName;
                ds.Tables.Add(fkDataTable);

                var ukDataTable = await UniqueKeysAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
                ukDataTable.TableName = UniqueKeysCollectionName;
                ds.Tables.Add(ukDataTable);

                var chDataTable = await CheckConstraintsAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
                chDataTable.TableName = CheckConstraintsCollectionName;
                ds.Tables.Add(chDataTable);

                var idnDataTable = await IdentityColumnsAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
                idnDataTable.TableName = IdentityColumnsCollectionName;
                ds.Tables.Add(idnDataTable);

                var compDataTable = await ComputedColumnsAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
                compDataTable.TableName = ComputedColumnsCollectionName;
                ds.Tables.Add(compDataTable);
            }
            return ds;
        }

        /// <summary>
        /// Finds the column identities asynchronously.
        /// </summary>
        public virtual async Task<DataTable> IdentityColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await IdentityColumnsAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Finds the column identities asynchronously.
        /// </summary>
        protected virtual Task<DataTable> IdentityColumnsAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDataTable(IdentityColumnsCollectionName));
        }

        /// <summary>
        /// Finds the table descriptions asynchronously.
        /// </summary>
        public virtual Task<DataTable> TableDescriptionAsync(string tableName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDataTable(TableDescriptionCollectionName));
        }

        /// <summary>
        /// Finds the column descriptions asynchronously.
        /// </summary>
        public virtual Task<DataTable> ColumnDescriptionAsync(string tableName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDataTable(ColumnDescriptionCollectionName));
        }

        /// <summary>
        /// Gets the triggers asynchronously.
        /// </summary>
        protected override Task<DataTable> TriggersAsync(string tableName, DbConnection conn, CancellationToken cancellationToken = default)
        {
            return GenericCollectionAsync(TriggersCollectionName, conn, tableName, cancellationToken);
        }

        /// <summary>
        /// Gets procedure source asynchronously.
        /// </summary>
        public virtual Task<DataTable> ProcedureSourceAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDataTable(ProcedureSourceCollectionName));
        }

        /// <summary>
        /// Get the columns of a view asynchronously.
        /// </summary>
        public virtual async Task<DataTable> ViewColumnsAsync(string viewName, CancellationToken cancellationToken = default)
        {
            var dt = await ColumnsAsync(viewName, cancellationToken).ConfigureAwait(false);
            dt.TableName = ViewColumnsCollectionName;
            return dt;
        }

        /// <summary>
        /// Gets all the "computed" columns asynchronously.
        /// </summary>
        public virtual async Task<DataTable> ComputedColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ComputedColumnsAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets all the "computed" columns asynchronously.
        /// </summary>
        protected virtual Task<DataTable> ComputedColumnsAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDataTable(ComputedColumnsCollectionName));
        }

        /// <summary>
        /// Gets the database version string asynchronously.
        /// </summary>
        public virtual async Task<string> ServerVersionAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ServerVersionAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the database version string asynchronously.
        /// </summary>
        protected virtual Task<string> ServerVersionAsync(DbConnection connection, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(connection.ServerVersion);
        }

        /// <summary>
        /// Gets default constraints asynchronously.
        /// </summary>
        public virtual Task<DataTable> DefaultConstraintsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDataTable(DefaultConstraintCollectionName));
        }

        /// <summary>
        /// Executes a command for a table and returns result as DataTable asynchronously.
        /// </summary>
        protected virtual async Task<DataTable> CommandForTableAsync(string tableName, DbConnection conn, string collectionName, string sqlCommand, CancellationToken cancellationToken = default)
        {
            DataTable dt = CreateDataTable(collectionName);

            if (conn.State == ConnectionState.Closed)
            {
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            using (var selectCommand = conn.CreateCommand())
            {
                selectCommand.CommandText = sqlCommand;
                AddTableNameSchemaParameters(selectCommand, tableName);

                try
                {
                    using (var dr = await selectCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        dt.Load(dr);
                    }
                }
                catch (DbException e)
                {
                    Console.WriteLine(e);
                }
                return dt;
            }
        }
    }
}
