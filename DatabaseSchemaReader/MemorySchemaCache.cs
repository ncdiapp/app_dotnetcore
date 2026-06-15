using System.Collections.Generic;
using System.Data;

namespace DatabaseSchemaMrg
{
    /// <summary>In-memory implementation of <see cref="ISchemaCache"/>.</summary>
    public sealed class MemorySchemaCache : ISchemaCache
    {
        private readonly Dictionary<string, DataTable> _store = new Dictionary<string, DataTable>(System.StringComparer.Ordinal);

        public DataTable? Get(string key)
            => _store.TryGetValue(key, out var dt) ? dt : null;

        public void Set(string key, DataTable data)
            => _store[key] = data;

        public void Invalidate()
            => _store.Clear();
    }
}
