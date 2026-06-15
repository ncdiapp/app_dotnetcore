using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Async interface for reading database schema metadata.
    /// </summary>
    public interface ISchemaReaderAsync
    {
        /// <summary>
        /// Gets all users asynchronously.
        /// </summary>
        Task<DataTable> UsersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current user's schema owner asynchronously.
        /// </summary>
        Task<string> CurrentUserSchemaOwnerAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tables asynchronously.
        /// </summary>
        Task<DataTable> TablesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific table's schema asynchronously.
        /// </summary>
        Task<DataSet> TableAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all views asynchronously.
        /// </summary>
        Task<DataTable> ViewsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all columns for a table asynchronously.
        /// </summary>
        Task<DataTable> ColumnsAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets indexes for a table asynchronously.
        /// </summary>
        Task<DataTable> IndexesAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets index columns for a table asynchronously.
        /// </summary>
        Task<DataTable> IndexColumnsAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets primary keys for a table asynchronously.
        /// </summary>
        Task<DataTable> PrimaryKeysAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets foreign keys for a table asynchronously.
        /// </summary>
        Task<DataTable> ForeignKeysAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets foreign key columns for a table asynchronously.
        /// </summary>
        Task<DataTable> ForeignKeyColumnsAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets unique keys for a table asynchronously.
        /// </summary>
        Task<DataTable> UniqueKeysAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets check constraints for a table asynchronously.
        /// </summary>
        Task<DataTable> CheckConstraintsAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets sequences asynchronously.
        /// </summary>
        Task<DataTable> SequencesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets triggers for a table asynchronously.
        /// </summary>
        Task<DataTable> TriggersAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all functions asynchronously.
        /// </summary>
        Task<DataTable> FunctionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all stored procedures asynchronously.
        /// </summary>
        Task<DataTable> StoredProceduresAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets stored procedure arguments asynchronously.
        /// </summary>
        Task<DataTable> StoredProcedureArgumentsAsync(string storedProcedureName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets packages asynchronously.
        /// </summary>
        Task<DataTable> PackagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets metadata collections asynchronously.
        /// </summary>
        Task<DataTable> MetadataCollectionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets data types asynchronously.
        /// </summary>
        Task<DataTable> DataTypesAsync(CancellationToken cancellationToken = default);
    }
}
