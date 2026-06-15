using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.Conversion.Loaders;
using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg
{
    /// <summary>
    /// Async methods for DatabaseFixture.
    /// </summary>
    public partial class DatabaseFixture : IDatabaseReaderAsync
    {
        /// <summary>
        /// Gets all of the schema in one call asynchronously.
        /// </summary>
        public async Task<DatabaseSchema> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            _fixUp = false;

            await DataTypesAsync(cancellationToken).ConfigureAwait(false);
            await AllTablesAsync(cancellationToken).ConfigureAwait(false);

            // Oracle extra - sequences
            DatabaseSchema.Sequences.Clear();
            var sequences = await _schemaReader.SequencesAsync(cancellationToken).ConfigureAwait(false);
            DatabaseSchema.Sequences.AddRange(SchemaProcedureConverter.Sequences(sequences));

            _fixUp = true;
            UpdateReferences();

            return _databaseSchema;
        }

        /// <summary>
        /// Gets the users (specifically for Oracle) asynchronously.
        /// </summary>
        public async Task<IList<DatabaseUser>> AllUsersAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<DatabaseUser>();
            DataTable dt = await _schemaReader.UsersAsync(cancellationToken).ConfigureAwait(false);

            string key = "user_name";
            if (!dt.Columns.Contains(key)) key = "name";
            if (!dt.Columns.Contains(key)) key = "username";

            foreach (DataRow row in dt.Rows)
            {
                var u = new DatabaseUser();
                u.Name = row[key].ToString();
                list.Add(u);
            }
            DatabaseSchema.Users.Clear();
            DatabaseSchema.Users.AddRange(list);
            return list;
        }

        /// <summary>
        /// Gets all tables (just names, no columns) asynchronously.
        /// </summary>
        public async Task<IList<DatabaseTable>> TableListAsync(CancellationToken cancellationToken = default)
        {
            DataTable dt = await _schemaReader.TablesAsync(cancellationToken).ConfigureAwait(false);
            return SchemaConverter.Tables(dt);
        }

        /// <summary>
        /// Gets all tables (plus constraints, indexes and triggers) asynchronously.
        /// </summary>
        public async Task<IList<DatabaseTable>> AllTablesAsync(CancellationToken cancellationToken = default)
        {
            DataTable tabs = await _schemaReader.TablesAsync(cancellationToken).ConfigureAwait(false);

            var columnLoader = new ColumnLoader(_schemaReader, Cache);
            var constraintLoader = new SchemaConstraintLoader(_schemaReader, Cache);
            var indexLoader = new IndexLoader(_schemaReader, Cache);

            DataTable ids = await _schemaReader.IdentityColumnsAsync(null, cancellationToken).ConfigureAwait(false);
            DataTable computeds = await _schemaReader.ComputedColumnsAsync(null, cancellationToken).ConfigureAwait(false);

            var tableDescriptions = new TableDescriptionConverter(await _schemaReader.TableDescriptionAsync(null, cancellationToken).ConfigureAwait(false));
            var columnDescriptions = new ColumnDescriptionConverter(await _schemaReader.ColumnDescriptionAsync(null, cancellationToken).ConfigureAwait(false));

            DataTable triggers = await _schemaReader.TriggersAsync(null, cancellationToken).ConfigureAwait(false);
            var triggerConverter = new TriggerConverter(triggers);

            var tables = SchemaConverter.Tables(tabs);
            var tableFilter = Exclusions.TableFilter;
            if (tableFilter != null)
            {
                tables.RemoveAll(t => tableFilter.Exclude(t.Name));
            }
            tables.Sort(delegate (DatabaseTable t1, DatabaseTable t2)
            {
                return string.Compare(t1.Name, t2.Name, StringComparison.OrdinalIgnoreCase);
            });

            foreach (DatabaseTable table in tables)
            {
                var tableName = table.Name;
                var schemaName = table.SchemaOwner;
                table.Description = tableDescriptions.FindDescription(table.SchemaOwner, tableName);

                var databaseColumns = columnLoader.Load(tableName, schemaName);
                table.Columns.AddRange(databaseColumns);

                columnDescriptions.AddDescriptions(table);

                var pkConstraints = constraintLoader.Load(tableName, schemaName, ConstraintType.PrimaryKey);
                PrimaryKeyLogic.AddPrimaryKey(table, pkConstraints);

                var fks = constraintLoader.Load(tableName, schemaName, ConstraintType.ForeignKey);
                table.AddConstraints(fks);

                table.AddConstraints(constraintLoader.Load(tableName, schemaName, ConstraintType.UniqueKey));
                table.AddConstraints(constraintLoader.Load(tableName, schemaName, ConstraintType.Check));

                var defaultContrain = constraintLoader.Load(tableName, schemaName, ConstraintType.Default);
                table.AddConstraints(defaultContrain);

                indexLoader.AddIndexes(table);

                SchemaConstraintConverter.AddIdentity(ids, table);
                SchemaConstraintConverter.AddComputed(computeds, table);

                table.Triggers.Clear();
                table.Triggers.AddRange(triggerConverter.Triggers(tableName));
                _schemaReader.PostProcessing(table);
            }

            DatabaseSchema.Tables.Clear();
            DatabaseSchema.Tables.AddRange(tables);
            UpdateReferences();

            if (DatabaseSchema.DataTypes.Count > 0)
                DatabaseSchemaFixer.UpdateDataTypes(DatabaseSchema);

            _schemaReader.PostProcessing(DatabaseSchema);

            return tables;
        }

        /// <summary>
        /// Gets all views asynchronously.
        /// </summary>
        public async Task<IList<DatabaseView>> AllViewsAsync(CancellationToken cancellationToken = default)
        {
            DataTable dt = await _schemaReader.ViewsAsync(cancellationToken).ConfigureAwait(false);
            List<DatabaseView> views = SchemaConverter.Views(dt);
            var viewFilter = Exclusions.ViewFilter;
            if (viewFilter != null)
            {
                views.RemoveAll(v => viewFilter.Exclude(v.Name));
            }

            var columnLoader = new ViewColumnLoader(_schemaReader);
            foreach (DatabaseView v in views)
            {
                v.Columns.AddRange(columnLoader.Load(v.Name, v.SchemaOwner));
            }
            DatabaseSchema.Views.Clear();
            DatabaseSchema.Views.AddRange(views);
            return views;
        }

        /// <summary>
        /// Gets a specific table by name asynchronously.
        /// </summary>
        public async Task<DatabaseTable> TableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

            var schemaOwner = _schemaReader.Owner;
            DatabaseTable table;

            using (DataSet ds = await _schemaReader.TableAsync(tableName, cancellationToken).ConfigureAwait(false))
            {
                if (ds == null) return null;
                if (ds.Tables.Count == 0) return null;

                table = DatabaseSchema.FindTableByName(tableName, schemaOwner);
                if (table == null)
                {
                    table = new DatabaseTable();
                    DatabaseSchema.Tables.Add(table);
                }
                table.Name = tableName;
                table.SchemaOwner = schemaOwner;

                table.Columns.Clear();
                var columnConverter = new ColumnConverter(ds.Tables[_schemaReader.ColumnsCollectionName]);
                var databaseColumns = columnConverter.Columns(tableName, schemaOwner).ToList();
                if (!databaseColumns.Any())
                {
                    databaseColumns = columnConverter.Columns().ToList();
                    var first = databaseColumns.FirstOrDefault();
                    if (first != null)
                    {
                        table.SchemaOwner = schemaOwner = first.SchemaOwner;
                    }
                    databaseColumns = columnConverter.Columns(tableName, schemaOwner).ToList();
                }
                table.Columns.AddRange(databaseColumns);

                if (ds.Tables.Contains(_schemaReader.PrimaryKeysCollectionName))
                {
                    var converter = new SchemaConstraintConverter(ds.Tables[_schemaReader.PrimaryKeysCollectionName], ConstraintType.PrimaryKey);
                    var pkConstraints = converter.Constraints();
                    PrimaryKeyLogic.AddPrimaryKey(table, pkConstraints);
                }
                if (ds.Tables.Contains(_schemaReader.ForeignKeysCollectionName))
                {
                    var converter = new SchemaConstraintConverter(ds.Tables[_schemaReader.ForeignKeysCollectionName], ConstraintType.ForeignKey);
                    table.AddConstraints(converter.Constraints());
                }
                if (ds.Tables.Contains(_schemaReader.ForeignKeyColumnsCollectionName))
                {
                    var fkConverter = new ForeignKeyColumnConverter(ds.Tables[_schemaReader.ForeignKeyColumnsCollectionName]);
                    fkConverter.AddForeignKeyColumns(table.ForeignKeys);
                }

                if (ds.Tables.Contains(_schemaReader.UniqueKeysCollectionName))
                {
                    var converter = new SchemaConstraintConverter(ds.Tables[_schemaReader.UniqueKeysCollectionName], ConstraintType.UniqueKey);
                    table.AddConstraints(converter.Constraints());
                }

                var indexConverter = new IndexConverter(ds.Tables[_schemaReader.IndexColumnsCollectionName], null);
                table.Indexes.AddRange(indexConverter.Indexes(tableName, schemaOwner));

                if (ds.Tables.Contains(_schemaReader.IdentityColumnsCollectionName))
                    SchemaConstraintConverter.AddIdentity(ds.Tables[_schemaReader.IdentityColumnsCollectionName], table);

                _schemaReader.PostProcessing(table);
            }

            if (DatabaseSchema.DataTypes.Count > 0)
                DatabaseSchemaFixer.UpdateDataTypes(DatabaseSchema);
            _schemaReader.PostProcessing(DatabaseSchema);

            return table;
        }

        /// <summary>
        /// Gets all stored procedures (no arguments, for Oracle no packages) asynchronously.
        /// </summary>
        public async Task<IList<DatabaseStoredProcedure>> StoredProcedureListAsync(CancellationToken cancellationToken = default)
        {
            DataTable dt = await _schemaReader.StoredProceduresAsync(cancellationToken).ConfigureAwait(false);
            SchemaProcedureConverter.StoredProcedures(DatabaseSchema, dt);
            return DatabaseSchema.StoredProcedures;
        }

        /// <summary>
        /// Gets all stored procedures (and functions) with their arguments asynchronously.
        /// </summary>
        public async Task<IList<DatabaseStoredProcedure>> AllStoredProceduresAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                DataTable functions = await _schemaReader.FunctionsAsync(cancellationToken).ConfigureAwait(false);
                DatabaseSchema.Functions.Clear();
                DatabaseSchema.Functions.AddRange(SchemaProcedureConverter.Functions(functions));
            }
            catch (DbException ex)
            {
                Debug.WriteLine("Cannot read functions - database security may prevent access to DDL\n" + ex.Message);
                throw;
            }

            DataTable dt = await _schemaReader.StoredProceduresAsync(cancellationToken).ConfigureAwait(false);
            SchemaProcedureConverter.StoredProcedures(DatabaseSchema, dt);
            var procFilter = Exclusions.StoredProcedureFilter;
            if (procFilter != null)
            {
                DatabaseSchema.StoredProcedures.RemoveAll(p => procFilter.Exclude(p.Name));
            }

            DatabaseSchema.Packages.Clear();
            DatabaseSchema.Packages.AddRange(SchemaProcedureConverter.Packages(await _schemaReader.PackagesAsync(cancellationToken).ConfigureAwait(false)));
            var packFilter = Exclusions.PackageFilter;
            if (packFilter != null)
            {
                DatabaseSchema.Packages.RemoveAll(p => packFilter.Exclude(p.Name));
            }

            DataTable args = await _schemaReader.StoredProcedureArgumentsAsync(null, cancellationToken).ConfigureAwait(false);

            var converter = new SchemaProcedureConverter();
            converter.PackageFilter = Exclusions.PackageFilter;
            converter.StoredProcedureFilter = Exclusions.StoredProcedureFilter;

            if (args.Rows.Count == 0)
            {
                foreach (var sproc in DatabaseSchema.StoredProcedures)
                {
                    args = await _schemaReader.StoredProcedureArgumentsAsync(sproc.Name, cancellationToken).ConfigureAwait(false);
                    converter.UpdateArguments(DatabaseSchema, args);
                }

                foreach (var function in DatabaseSchema.Functions)
                {
                    args = await _schemaReader.StoredProcedureArgumentsAsync(function.Name, cancellationToken).ConfigureAwait(false);
                    converter.UpdateArguments(DatabaseSchema, args);
                }
            }

            converter.UpdateArguments(DatabaseSchema, args);
            foreach (var function in DatabaseSchema.Functions)
            {
                function.CheckArgumentsForReturnType();
            }

            DataTable srcs = await _schemaReader.ProcedureSourceAsync(null, cancellationToken).ConfigureAwait(false);
            SchemaSourceConverter.AddSources(DatabaseSchema, srcs);

            UpdateReferences();

            return DatabaseSchema.StoredProcedures;
        }

        /// <summary>
        /// Gets all datatypes (and updates columns/arguments if already loaded) asynchronously.
        /// </summary>
        public async Task<IList<DataType>> DataTypesAsync(CancellationToken cancellationToken = default)
        {
            List<DataType> list = SchemaConverter.DataTypes(await _schemaReader.DataTypesAsync(cancellationToken).ConfigureAwait(false));
            if (list.Count == 0) list = _schemaReader.SchemaDataTypes();
            DatabaseSchema.DataTypes.Clear();
            DatabaseSchema.DataTypes.AddRange(list);
            DatabaseSchemaFixer.UpdateDataTypes(DatabaseSchema);
            return list;
        }
    }
}
