using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Async methods for SchemaReader.
    /// </summary>
    public partial class SchemaReader : ISchemaReaderAsync
    {
        #region Async Connection Helpers

        /// <summary>
        /// Opens a connection asynchronously.
        /// </summary>
        protected async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var conn = Factory.CreateConnection();
            conn.ConnectionString = ConnectionString;
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return conn;
        }

        #endregion

        #region Async Schema Methods

        /// <summary>
        /// DataTable of all users asynchronously.
        /// </summary>
        public virtual async Task<DataTable> UsersAsync(CancellationToken cancellationToken = default)
        {
            string collection = UsersCollectionName;
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!SchemaCollectionExists(conn, collection))
                    return CreateDataTable(collection);
                return conn.GetSchema(collection);
            }
        }

        /// <summary>
        /// Gets the current user's schema owner asynchronously.
        /// </summary>
        public virtual async Task<string> CurrentUserSchemaOwnerAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = GetQueryCurrentUserSchemaOwner();
                var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result as string;
            }
        }

        /// <summary>
        /// DataTable of all tables asynchronously.
        /// </summary>
        public virtual async Task<DataTable> TablesAsync(CancellationToken cancellationToken = default)
        {
            string collectionName = TablesCollectionName;
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// Get all data for a specified table name asynchronously.
        /// </summary>
        public virtual async Task<DataSet> TableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var ds = new DataSet();
            ds.Locale = CultureInfo.InvariantCulture;
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                await LoadTableAsync(tableName, ds, connection, cancellationToken).ConfigureAwait(false);
                ds.Tables.Add(await PrimaryKeysAsync(tableName, connection, cancellationToken).ConfigureAwait(false));
                ds.Tables.Add(await ForeignKeysAsync(tableName, connection, cancellationToken).ConfigureAwait(false));
                ds.Tables.Add(await ForeignKeyColumnsAsync(tableName, connection, cancellationToken).ConfigureAwait(false));
            }
            return ds;
        }

        /// <summary>
        /// Loads the table columns, indexes and indexcolumns into a dataset asynchronously.
        /// </summary>
        protected async Task LoadTableAsync(string tableName, DataSet ds, DbConnection connection, CancellationToken cancellationToken = default)
        {
            var cols = await ColumnsAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            if (cols.Rows.Count == 0) return;
            ds.Tables.Add(cols);

            var indexes = await IndexesAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            ds.Tables.Add(indexes);
            var indexColumns = await IndexColumnsAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            if (indexColumns.TableName != indexes.TableName)
                ds.Tables.Add(indexColumns);
        }

        /// <summary>
        /// DataTable of all views asynchronously.
        /// </summary>
        public virtual async Task<DataTable> ViewsAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                string collectionName = ViewsCollectionName;
                if (!SchemaCollectionExists(conn, collectionName))
                    collectionName = TablesCollectionName;
                if (!SchemaCollectionExists(conn, collectionName))
                    return CreateDataTable(collectionName);
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        /// <summary>
        /// All the columns for a specific table asynchronously.
        /// </summary>
        public virtual async Task<DataTable> ColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ColumnsAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get the columns using GetSchema asynchronously.
        /// </summary>
        protected virtual Task<DataTable> ColumnsAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string[] restrictions = SchemaRestrictions.ForTable(connection, ColumnsCollectionName, tableName);
            return Task.FromResult(connection.GetSchema(ColumnsCollectionName, restrictions));
        }

        /// <summary>
        /// Gets the indexes asynchronously.
        /// </summary>
        public virtual async Task<DataTable> IndexesAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await IndexesAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the indexes asynchronously.
        /// </summary>
        protected virtual Task<DataTable> IndexesAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = IndexesCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
            {
                return Task.FromResult(CreateDataTable(collectionName));
            }

            return RunGetSchemaAsync(connection, collectionName, tableName, cancellationToken);
        }

        private Task<DataTable> RunGetSchemaAsync(DbConnection connection, string collectionName, string tableName, CancellationToken cancellationToken = default)
        {
            string[] restrictions = SchemaRestrictions.ForTable(connection, collectionName, tableName);
            try
            {
                return Task.FromResult(connection.GetSchema(collectionName, restrictions));
            }
            catch (DbException exception)
            {
                Console.WriteLine("Provider returned error for " + collectionName + ": " + exception.Message);
                return Task.FromResult(CreateDataTable(collectionName));
            }
            catch (SqlNullValueException exception)
            {
                Console.WriteLine("Provider returned error for " + collectionName + ": " + exception.Message);
                return Task.FromResult(CreateDataTable(collectionName));
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine("Provider returned error for " + collectionName + ": " + exception.Message);
                return Task.FromResult(CreateDataTable(collectionName));
            }
        }

        /// <summary>
        /// Gets the indexed columns asynchronously.
        /// </summary>
        public virtual async Task<DataTable> IndexColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await IndexColumnsAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task<DataTable> IndexColumnsAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = IndexColumnsCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
            {
                collectionName = IndexesCollectionName;
                if (!SchemaCollectionExists(connection, collectionName))
                    return Task.FromResult(CreateDataTable(collectionName));
            }

            return RunGetSchemaAsync(connection, collectionName, tableName, cancellationToken);
        }

        /// <summary>
        /// Gets the primary keys asynchronously.
        /// </summary>
        public virtual async Task<DataTable> PrimaryKeysAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await PrimaryKeysAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the primary keys asynchronously.
        /// </summary>
        protected virtual Task<DataTable> PrimaryKeysAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = PrimaryKeysCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(collectionName));

            string[] restrictions = SchemaRestrictions.ForTable(connection, collectionName, tableName);
            try
            {
                return Task.FromResult(connection.GetSchema(collectionName, restrictions));
            }
            catch (ArgumentException)
            {
                return Task.FromResult(CreateDataTable(collectionName));
            }
        }

        /// <summary>
        /// Finds the foreign keys asynchronously.
        /// </summary>
        public virtual async Task<DataTable> ForeignKeysAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ForeignKeysAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Finds the foreign keys asynchronously.
        /// </summary>
        protected virtual Task<DataTable> ForeignKeysAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = ForeignKeysCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
            {
                collectionName = "Foreign Keys";
                if (!SchemaCollectionExists(connection, collectionName))
                {
                    collectionName = "Foreign_Keys";
                    if (!SchemaCollectionExists(connection, collectionName))
                        return Task.FromResult(CreateDataTable(ForeignKeysCollectionName));
                }
            }
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(ForeignKeysCollectionName));

            string[] restrictions = SchemaRestrictions.ForTable(connection, collectionName, tableName);
            try
            {
                var dt = connection.GetSchema(collectionName, restrictions);
                dt.TableName = ForeignKeysCollectionName;
                return Task.FromResult(dt);
            }
            catch (ArgumentException)
            {
                return Task.FromResult(CreateDataTable(ForeignKeysCollectionName));
            }
        }

        /// <summary>
        /// Finds the foreign key columns asynchronously.
        /// </summary>
        public virtual async Task<DataTable> ForeignKeyColumnsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ForeignKeyColumnsAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Finds the foreign key columns asynchronously.
        /// </summary>
        protected virtual Task<DataTable> ForeignKeyColumnsAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = ForeignKeyColumnsCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(collectionName));

            string[] restrictions = SchemaRestrictions.ForTable(connection, collectionName, tableName);
            var dt = connection.GetSchema(collectionName, restrictions);
            if (dt.TableName != collectionName)
                dt.TableName = collectionName;
            return Task.FromResult(dt);
        }

        /// <summary>
        /// The Unique Key columns for a specific table asynchronously.
        /// </summary>
        public virtual async Task<DataTable> UniqueKeysAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await UniqueKeysAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The Unique Key columns for a specific table asynchronously.
        /// </summary>
        protected virtual Task<DataTable> UniqueKeysAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            return GenericCollectionAsync(UniqueKeysCollectionName, connection, tableName, cancellationToken);
        }

        /// <summary>
        /// The check constraints for a specific table asynchronously.
        /// </summary>
        public virtual async Task<DataTable> CheckConstraintsAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await CheckConstraintsAsync(tableName, connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The check constraints for a specific table asynchronously.
        /// </summary>
        protected virtual Task<DataTable> CheckConstraintsAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            return GenericCollectionAsync(CheckConstraintsCollectionName, connection, tableName, cancellationToken);
        }

        /// <summary>
        /// Gets the sequences asynchronously.
        /// </summary>
        public virtual async Task<DataTable> SequencesAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await SequencesAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the sequences asynchronously.
        /// </summary>
        protected virtual Task<DataTable> SequencesAsync(DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = SequencesCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
                collectionName = "Generators";
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(SequencesCollectionName));
            string[] restrictions = SchemaRestrictions.ForOwner(connection, collectionName);
            var dt = connection.GetSchema(collectionName, restrictions);
            dt.TableName = SequencesCollectionName;
            return Task.FromResult(dt);
        }

        /// <summary>
        /// Gets the triggers asynchronously.
        /// </summary>
        public virtual async Task<DataTable> TriggersAsync(string tableName, CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await TriggersAsync(tableName, conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the triggers asynchronously.
        /// </summary>
        protected virtual Task<DataTable> TriggersAsync(string tableName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            return GenericCollectionAsync(TriggersCollectionName, connection, tableName, cancellationToken);
        }

        /// <summary>
        /// Retrieve a generic collection asynchronously.
        /// </summary>
        protected Task<DataTable> GenericCollectionAsync(string collectionName, DbConnection connection, string tableName, CancellationToken cancellationToken = default)
        {
            if (SchemaCollectionExists(connection, collectionName))
            {
                return Task.FromResult(connection.GetSchema(collectionName, SchemaRestrictions.ForTable(connection, collectionName, tableName)));
            }
            return Task.FromResult(CreateDataTable(collectionName));
        }

        #endregion

        #region Async Sprocs

        /// <summary>
        /// Get all the functions asynchronously.
        /// </summary>
        public virtual async Task<DataTable> FunctionsAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await FunctionsAsync(conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the functions asynchronously.
        /// </summary>
        protected virtual Task<DataTable> FunctionsAsync(DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = FunctionsCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(collectionName));
            string[] restrictions = SchemaRestrictions.ForOwner(connection, collectionName);
            return Task.FromResult(connection.GetSchema(collectionName, restrictions));
        }

        /// <summary>
        /// Get all the stored procedures asynchronously.
        /// </summary>
        public virtual async Task<DataTable> StoredProceduresAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await StoredProceduresAsync(conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the stored procedures asynchronously.
        /// </summary>
        protected virtual Task<DataTable> StoredProceduresAsync(DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = ProceduresCollectionName;
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(collectionName));
            string[] restrictions = SchemaRestrictions.ForOwner(connection, collectionName);
            return Task.FromResult(connection.GetSchema(collectionName, restrictions));
        }

        /// <summary>
        /// Get all the arguments for a stored procedure asynchronously.
        /// </summary>
        public virtual async Task<DataTable> StoredProcedureArgumentsAsync(string storedProcedureName, CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await StoredProcedureArgumentsAsync(storedProcedureName, conn, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the arguments for a stored procedure asynchronously.
        /// </summary>
        protected virtual Task<DataTable> StoredProcedureArgumentsAsync(string storedProcedureName, DbConnection connection, CancellationToken cancellationToken = default)
        {
            string collectionName = ProcedureParametersCollectionName;
            if (!SchemaCollectionExists(connection, collectionName)) collectionName = "Arguments";
            if (ProviderType == EmSqlType.MySql) collectionName = "Procedure Parameters";
            else if (ProviderType == EmSqlType.Oracle) collectionName = "Arguments";
            if (!SchemaCollectionExists(connection, collectionName))
                return Task.FromResult(CreateDataTable(ProcedureParametersCollectionName));

            string[] restrictions = SchemaRestrictions.ForRoutine(connection, collectionName, storedProcedureName);
            var dt = connection.GetSchema(collectionName, restrictions);
            dt.TableName = ProcedureParametersCollectionName;
            return Task.FromResult(dt);
        }

        /// <summary>
        /// Get all the packages asynchronously.
        /// </summary>
        public virtual async Task<DataTable> PackagesAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                string collectionName = PackagesCollectionName;
                if (!SchemaCollectionExists(conn, collectionName))
                    return CreateDataTable(collectionName);
                string[] restrictions = SchemaRestrictions.ForOwner(conn, collectionName);
                return conn.GetSchema(collectionName, restrictions);
            }
        }

        #endregion

        #region Async MetadataCollections

        /// <summary>
        /// All the collections that are available via GetSchema asynchronously.
        /// </summary>
        public virtual async Task<DataTable> MetadataCollectionsAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                _metadata = MetadataCollections(conn);
                return _metadata;
            }
        }

        /// <summary>
        /// All the Datatypes in the database asynchronously.
        /// </summary>
        public virtual async Task<DataTable> DataTypesAsync(CancellationToken cancellationToken = default)
        {
            using (var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await DataTypesAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// All the Datatypes in the database asynchronously.
        /// </summary>
        protected virtual Task<DataTable> DataTypesAsync(DbConnection connection, CancellationToken cancellationToken = default)
        {
            try
            {
                return Task.FromResult(connection.GetSchema(DbMetaDataCollectionNames.DataTypes));
            }
            catch (NotSupportedException)
            {
                return Task.FromResult(CreateDataTable("DataTypes"));
            }
        }

        #endregion
    }
}
