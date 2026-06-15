using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Async version of IDatabaseReader for reading database schema into schema objects.
    /// </summary>
    public interface IDatabaseReaderAsync : IDatabaseReader
    {
        /// <summary>
        /// Gets all of the schema in one call asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The complete database schema.</returns>
        Task<DatabaseSchema> ReadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the users (specifically for Oracle) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of database users.</returns>
        Task<IList<DatabaseUser>> AllUsersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tables (just names, no columns) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of tables with names only.</returns>
        Task<IList<DatabaseTable>> TableListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all tables (plus constraints, indexes and triggers) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of tables with full metadata.</returns>
        Task<IList<DatabaseTable>> AllTablesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all views asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of database views.</returns>
        Task<IList<DatabaseView>> AllViewsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific table by name asynchronously.
        /// </summary>
        /// <param name="tableName">Name of the table. Oracle names can be case sensitive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The table with full metadata.</returns>
        Task<DatabaseTable> TableAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all stored procedures (no arguments, for Oracle no packages) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of stored procedures.</returns>
        Task<IList<DatabaseStoredProcedure>> StoredProcedureListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all stored procedures (and functions) with their arguments asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of stored procedures with arguments.</returns>
        Task<IList<DatabaseStoredProcedure>> AllStoredProceduresAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all datatypes (and updates columns/arguments if already loaded) asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of data types.</returns>
        Task<IList<DataType>> DataTypesAsync(CancellationToken cancellationToken = default);
    }
}
