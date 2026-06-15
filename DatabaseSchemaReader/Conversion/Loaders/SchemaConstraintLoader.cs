using System;
using System.Collections.Generic;
using System.Data;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.Conversion.Loaders
{
    /// <summary>
    /// Loads and converts the dataTable (wrapping the Converter). Hides all/byTable logic.
    /// </summary>
    class SchemaConstraintLoader
    {
        private readonly SchemaExtendedReader _sr;
        private readonly SchemaConstraintConverter _pkConverter;
        private readonly SchemaConstraintConverter _fkConverter;
        private readonly ForeignKeyColumnConverter _fkColumnConverter;
        private readonly SchemaConstraintConverter _ukConverter;
        private readonly SchemaConstraintConverter _ckConverter;
        private readonly SchemaConstraintConverter _dfConverter;

        private readonly bool _noPks;
        private readonly bool _noFks;

        public SchemaConstraintLoader(SchemaExtendedReader schemaReader, ISchemaCache? cache = null)
        {
            _sr = schemaReader;
            var pks = CacheLoad(cache, "PrimaryKeys", () => _sr.PrimaryKeys(null));
            _noPks = (pks.Rows.Count == 0);
            if (!_noPks)
            {
                _pkConverter = new SchemaConstraintConverter(pks, ConstraintType.PrimaryKey);
            }
            var fks = CacheLoad(cache, "ForeignKeys", () => _sr.ForeignKeys(null));
            _noFks = (fks.Rows.Count == 0);
            if (!_noFks)
            {
                _fkConverter = new SchemaConstraintConverter(fks, ConstraintType.ForeignKey);
            }
            var fkcols = CacheLoad(cache, "ForeignKeyColumns", () => _sr.ForeignKeyColumns(null));
            _fkColumnConverter = new ForeignKeyColumnConverter(fkcols);

            var uks = CacheLoad(cache, "UniqueKeys", () => _sr.UniqueKeys(null));
            _ukConverter = new SchemaConstraintConverter(uks, ConstraintType.UniqueKey);
            var cks = CacheLoad(cache, "CheckConstraints", () => _sr.CheckConstraints(null));
            _ckConverter = new SchemaConstraintConverter(cks, ConstraintType.Check);

            var dfs = CacheLoad(cache, "DefaultConstraints", () => _sr.DefaultConstraints(null));
            _dfConverter = new SchemaConstraintConverter(dfs, ConstraintType.Default);
        }

        private static DataTable CacheLoad(ISchemaCache? cache, string key, Func<DataTable> loader)
        {
            if (cache == null) return loader();
            var cached = cache.Get(key);
            if (cached != null) return cached;
            var dt = loader();
            cache.Set(key, dt);
            return dt;
        }

        private IList<DatabaseConstraint> PrimaryKeys(string tableName, string schemaName)
        {
            if (!_noPks)
            {
                //we have preloaded
                return _pkConverter.Constraints(tableName, schemaName);
            }
            var constraints = _sr.PrimaryKeys(tableName);
            var converter = new SchemaConstraintConverter(constraints, ConstraintType.PrimaryKey);
            return converter.Constraints();
        }


        private IList<DatabaseConstraint> ForeignKeys(string tableName, string schemaName)
        {
            IList<DatabaseConstraint> fks;
            if (!_noFks)
            {
                //we have preloaded
                fks = _fkConverter.Constraints(tableName, schemaName);
                _fkColumnConverter.AddForeignKeyColumns(fks);
                return fks;
            }
            var constraints = _sr.ForeignKeys(tableName);
            var converter = new SchemaConstraintConverter(constraints, ConstraintType.ForeignKey);
            fks = converter.Constraints();
            var cols = _sr.ForeignKeyColumns(tableName);
            var colConverter = new ForeignKeyColumnConverter(cols);
            colConverter.AddForeignKeyColumns(fks);
            return fks;
        }

        public IList<DatabaseConstraint> Load(string tableName, string schemaName, ConstraintType constraintType)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException("tableName", "must have tableName");

            switch (constraintType)
            {
                case ConstraintType.PrimaryKey:
                    return PrimaryKeys(tableName, schemaName);

                case ConstraintType.ForeignKey:
                    return ForeignKeys(tableName, schemaName);

                case ConstraintType.UniqueKey:
                    return _ukConverter.Constraints(tableName, schemaName);

                case ConstraintType.Check:
                    return _ckConverter.Constraints(tableName, schemaName);

                case ConstraintType.Default:
                    return _dfConverter.Constraints(tableName, schemaName);

                default:
                    throw new ArgumentOutOfRangeException("constraintType");
            }
        }

    }
}
