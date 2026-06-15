using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.AppBuilderAgent;
using APP.Components.Dto;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;

namespace App.BL.QueryAgent
{
    /// <summary>
    /// Provides get_table_schema and list_selected_tables tools for the Query AI Agent.
    /// </summary>
    public class QueryDesignerPlugin
    {
        private readonly int? _dataSourceId;
        private readonly List<string> _selectedTables;

        public QueryDesignerPlugin(int? dataSourceId, List<string> selectedTables)
        {
            _dataSourceId   = dataSourceId;
            _selectedTables = selectedTables ?? new List<string>();
        }

        [AgentFunction("get_database_dialect",
            "Returns the database platform and SQL dialect for this data source (e.g. SQL Server/T-SQL, MySQL, Oracle/PL-SQL). " +
            "Call this first so you know which syntax to use.")]
        public string GetDatabaseDialect()
        {
            try
            {
                if (!_dataSourceId.HasValue)
                    return JsonConvert.SerializeObject(new { Dialect = "SqlServer", DisplayName = "Microsoft SQL Server (T-SQL)", Note = "Default — no data source configured." });

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(_dataSourceId.Value);
                if (fixture == null)
                    return JsonConvert.SerializeObject(new { Dialect = "SqlServer", DisplayName = "Microsoft SQL Server (T-SQL)" });

                string dialect, displayName, syntaxNotes;
                switch (fixture.SqlServerType)
                {
                    case DatabaseSchemaMrg.DataSchema.EmSqlType.Oracle:
                        dialect     = "Oracle";
                        displayName = "Oracle Database (PL/SQL)";
                        syntaxNotes = "Use ROWNUM or FETCH FIRST n ROWS ONLY for row limits. Use NVL() instead of ISNULL(). Use SYSDATE for current date. Identifiers are unquoted or use double-quotes. Hierarchical queries: CONNECT BY PRIOR. Sequences for auto-increment.";
                        break;
                    case DatabaseSchemaMrg.DataSchema.EmSqlType.MySql:
                        dialect     = "MySQL";
                        displayName = "MySQL (MySQL SQL)";
                        syntaxNotes = "Use LIMIT n for row limits. Use IFNULL() instead of ISNULL(). Use NOW() for current date. Use backtick identifiers (`table_name`). STR_TO_DATE() for date parsing. GROUP_CONCAT() for string aggregation.";
                        break;
                    default:
                        dialect     = "SqlServer";
                        displayName = "Microsoft SQL Server (T-SQL)";
                        syntaxNotes = "Use TOP n or OFFSET/FETCH for row limits. Use ISNULL()/COALESCE(). Use GETDATE() for current date. Use [bracket] identifiers. STRING_AGG() for string aggregation. CROSS APPLY / OUTER APPLY supported.";
                        break;
                }

                return JsonConvert.SerializeObject(new
                {
                    Dialect     = dialect,
                    DisplayName = displayName,
                    SyntaxNotes = syntaxNotes,
                    SchemaOwner = fixture.CurrentOwner
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Dialect = "SqlServer", DisplayName = "Microsoft SQL Server (T-SQL)", Error = ex.Message });
            }
        }

        [AgentFunction("list_selected_tables",
            "List the tables/views the user has selected to work with. Call this after get_database_dialect to see which tables are in scope.")]
        public string ListSelectedTables()
        {
            try
            {
                if (!_selectedTables.Any())
                    return JsonConvert.SerializeObject(new
                    {
                        Message = "No tables selected. The user may want to query any table in the data source.",
                        AvailableCount = 0
                    });

                return JsonConvert.SerializeObject(new
                {
                    SelectedTables = _selectedTables,
                    Count          = _selectedTables.Count,
                    Message        = "Call get_table_schema for each table you need to understand its columns."
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        [AgentFunction("get_table_schema",
            "Get the column names and data types for a specific table or view. " +
            "Call this before writing a query to understand the available columns.")]
        public string GetTableSchema(
            [AgentParam("The table or view name to look up", true)]
            string tableName,
            [AgentParam("Optional schema owner (e.g. 'dbo'). Leave empty if unknown.")]
            string schemaOwner = null)
        {
            try
            {
                if (!_dataSourceId.HasValue)
                    return JsonConvert.SerializeObject(new { Error = "No data source configured." });

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(_dataSourceId.Value);
                if (fixture == null)
                    return JsonConvert.SerializeObject(new { Error = $"Data source {_dataSourceId} not found." });

                // Try exact table match first, then partial, then views
                var dbTable = fixture.Table(tableName)
                    ?? fixture.AllTables().FirstOrDefault(t =>
                        t.Name.IndexOf(tableName, StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? (DatabaseTable)fixture.AllViews().FirstOrDefault(v =>
                        string.Equals(v.Name, tableName, StringComparison.OrdinalIgnoreCase))
                    ?? (DatabaseTable)fixture.AllViews().FirstOrDefault(v =>
                        v.Name.IndexOf(tableName, StringComparison.OrdinalIgnoreCase) >= 0);

                if (dbTable == null)
                    return JsonConvert.SerializeObject(new
                    {
                        Error = $"Table or view '{tableName}' not found in data source {_dataSourceId}. " +
                                "Call list_selected_tables to see what tables are available."
                    });

                var cols = dbTable.Columns?
                    .Select(c => new
                    {
                        Name     = c.Name,
                        DataType = c.DbDataType ?? "unknown"
                    })
                    .ToList();

                bool isView = dbTable is DatabaseView;

                return JsonConvert.SerializeObject(new
                {
                    TableName   = dbTable.Name,
                    SchemaOwner = dbTable.SchemaOwner,
                    IsView      = isView,
                    Columns     = cols,
                    FullName    = string.IsNullOrEmpty(dbTable.SchemaOwner)
                                    ? $"[{dbTable.Name}]"
                                    : $"[{dbTable.SchemaOwner}].[{dbTable.Name}]"
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        [AgentFunction("get_table_relationships",
            "Get foreign key relationships for a table — which columns reference other tables and vice versa. " +
            "Use this to understand how to JOIN tables correctly.")]
        public string GetTableRelationships(
            [AgentParam("The table name to get relationships for", true)]
            string tableName)
        {
            try
            {
                if (!_dataSourceId.HasValue)
                    return JsonConvert.SerializeObject(new { Error = "No data source configured." });

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(_dataSourceId.Value);
                if (fixture == null)
                    return JsonConvert.SerializeObject(new { Error = $"Data source {_dataSourceId} not found." });

                var dbTable = fixture.Table(tableName)
                    ?? fixture.AllTables().FirstOrDefault(t =>
                        t.Name.IndexOf(tableName, StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? (DatabaseTable)fixture.AllViews().FirstOrDefault(v =>
                        string.Equals(v.Name, tableName, StringComparison.OrdinalIgnoreCase));

                if (dbTable == null)
                    return JsonConvert.SerializeObject(new { Error = $"Table '{tableName}' not found." });

                // Outgoing FKs: columns in this table that reference another table
                var outgoingFks = dbTable.Columns?
                    .Where(c => c.IsForeignKey && !string.IsNullOrEmpty(c.ForeignKeyTableName))
                    .Select(c => new
                    {
                        Column          = c.Name,
                        ReferencesTable = c.ForeignKeyTableName,
                        JoinHint        = $"JOIN [{c.ForeignKeyTableName}] ON [{dbTable.Name}].[{c.Name}] = [{c.ForeignKeyTableName}].[Id]"
                    })
                    .ToList();

                // Incoming FKs: other tables whose columns reference this table
                var incomingFks = fixture.AllTables()
                    .Where(t => !string.Equals(t.Name, dbTable.Name, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(t => (t.Columns ?? new System.Collections.Generic.List<DatabaseSchemaMrg.DataSchema.DatabaseColumn>())
                        .Where(c => c.IsForeignKey &&
                                    string.Equals(c.ForeignKeyTableName, dbTable.Name, StringComparison.OrdinalIgnoreCase))
                        .Select(c => new
                        {
                            FromTable  = t.Name,
                            FromColumn = c.Name,
                            JoinHint   = $"JOIN [{t.Name}] ON [{t.Name}].[{c.Name}] = [{dbTable.Name}].[Id]"
                        }))
                    .ToList();

                return JsonConvert.SerializeObject(new
                {
                    TableName   = dbTable.Name,
                    OutgoingFKs = outgoingFks,   // this table references these tables
                    IncomingFKs = incomingFks,   // other tables reference this table
                    Note        = "JoinHint uses 'Id' as a placeholder — adjust to the actual PK column name from get_table_schema."
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }
    }
}
