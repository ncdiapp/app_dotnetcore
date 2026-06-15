using System.Data;

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Per-session cache for bulk-loaded schema DataTables. Wire up via
    /// <see cref="DatabaseFixture.Cache"/> to avoid re-querying the database
    /// when AllTables() / AllStoredProcedures() is called more than once.
    /// </summary>
    public interface ISchemaCache
    {
        DataTable? Get(string key);
        void Set(string key, DataTable data);
        void Invalidate();
    }
}
