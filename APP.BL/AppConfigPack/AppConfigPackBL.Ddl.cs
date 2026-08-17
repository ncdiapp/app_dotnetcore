using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using App.BL;
using APP.Components.EntityDto;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        private static void ApplyDdl(AppConfigPackDto pack, int tenantDataSourceId, AppConfigPackExecuteResultDto executeResult)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(tenantDataSourceId);
            var tables = pack.Tables ?? new List<AppConfigPackTableDto>();
            var views = pack.Views ?? new List<AppConfigPackViewDto>();

            foreach (var table in tables)
            {
                if (string.IsNullOrWhiteSpace(table?.Name) || table.Columns == null || table.Columns.Count == 0)
                    continue;

                string schema = string.IsNullOrWhiteSpace(table.SchemaOwner) ? "dbo" : table.SchemaOwner.Trim();
                bool existedBefore;
                using (var conn = OpenTenantConnection())
                {
                    existedBefore = ObjectExists(conn, table.Name, schema);
                }

                string createSql = BuildCreateTableSql(table, schema);
                fixture.ExecuteNonQueryResult(createSql, new List<DbParameter>());

                if (!existedBefore)
                {
                    executeResult.TablesCreated++;
                    executeResult.Messages.Add($"Created table {schema}.{table.Name}.");
                }

                int added = 0;
                using (var conn = OpenTenantConnection())
                {
                    foreach (var col in table.Columns.Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name)))
                    {
                        if (ColumnExists(conn, table.Name, col.Name, schema))
                            continue;

                        string alterSql = BuildAddColumnSql(schema, table.Name, col);
                        fixture.ExecuteNonQueryResult(alterSql, new List<DbParameter>());
                        added++;
                    }
                }

                if (added > 0)
                {
                    executeResult.ColumnsAdded += added;
                    executeResult.Messages.Add($"Added {added} column(s) to {schema}.{table.Name}.");
                }
            }

            foreach (var table in tables)
            {
                ApplyTableRelationships(fixture, table);
            }

            foreach (var view in views)
            {
                if (string.IsNullOrWhiteSpace(view?.Name) || string.IsNullOrWhiteSpace(view.CreateOrAlterSql))
                    continue;

                string sql = NormalizeViewSql(view);
                fixture.ExecuteNonQueryResult(sql, new List<DbParameter>());
                executeResult.ViewsApplied++;
                executeResult.Messages.Add($"Applied view {view.Name}.");
            }
        }

        private static string BuildCreateTableSql(AppConfigPackTableDto table, string schema)
        {
            var sb = new StringBuilder();
            string tableName = table.Name.Trim();
            sb.AppendLine($"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'{EscapeSqlLiteral(tableName)}' AND schema_id = SCHEMA_ID(N'{EscapeSqlLiteral(schema)}'))");
            sb.AppendLine("BEGIN");
            sb.AppendLine($"CREATE TABLE {QuoteIdent(schema)}.{QuoteIdent(tableName)} (");

            var defs = new List<string>();
            var pkCols = new List<string>();
            foreach (var col in table.Columns.Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name)))
            {
                defs.Add("    " + BuildColumnDefinition(col));
                if (col.IsPrimaryKey)
                    pkCols.Add(QuoteIdent(col.Name));
            }

            if (pkCols.Count > 0)
                defs.Add($"    CONSTRAINT [PK_{SanitizeIdent(tableName)}] PRIMARY KEY ({string.Join(", ", pkCols)})");

            sb.AppendLine(string.Join(",\r\n", defs));
            sb.AppendLine(");");
            sb.AppendLine("END");
            return sb.ToString();
        }

        private static string BuildAddColumnSql(string schema, string tableName, AppConfigPackColumnDto col)
        {
            return $@"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'{EscapeSqlLiteral(schema)}'
      AND TABLE_NAME = N'{EscapeSqlLiteral(tableName)}'
      AND COLUMN_NAME = N'{EscapeSqlLiteral(col.Name)}')
BEGIN
    ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(tableName)} ADD {BuildColumnDefinition(col)};
END";
        }

        private static string BuildColumnDefinition(AppConfigPackColumnDto column)
        {
            var sb = new StringBuilder();
            sb.Append(QuoteIdent(column.Name)).Append(' ');

            string dataType = string.IsNullOrWhiteSpace(column.DataType) ? "NVARCHAR" : column.DataType.Trim().ToUpperInvariant();
            if (dataType == "VARCHAR" || dataType == "NVARCHAR" || dataType == "CHAR" || dataType == "NCHAR")
            {
                int length = column.Length ?? 255;
                sb.Append(dataType).Append('(').Append(length == -1 ? "MAX" : length.ToString()).Append(')');
            }
            else if (dataType == "DECIMAL" || dataType == "NUMERIC")
            {
                sb.Append(dataType).Append('(')
                    .Append(column.Precision ?? 18).Append(',')
                    .Append(column.Scale ?? 2).Append(')');
            }
            else
            {
                sb.Append(dataType);
            }

            if (column.IsAutoIncrement)
                sb.Append(" IDENTITY(1,1)");

            sb.Append(column.IsNullable && !column.IsPrimaryKey ? " NULL" : " NOT NULL");

            if (!string.IsNullOrWhiteSpace(column.DefaultValue) && !column.IsAutoIncrement)
                sb.Append(" DEFAULT ").Append(column.DefaultValue.Trim());

            return sb.ToString();
        }

        private static void ApplyTableRelationships(DatabaseSchemaMrg.DatabaseFixture fixture, AppConfigPackTableDto table)
        {
            if (table?.Relationships == null || table.Relationships.Count == 0)
                return;

            string schema = string.IsNullOrWhiteSpace(table.SchemaOwner) ? "dbo" : table.SchemaOwner.Trim();
            foreach (var rel in table.Relationships)
            {
                if (string.IsNullOrWhiteSpace(rel?.TargetTable)
                    || string.IsNullOrWhiteSpace(rel.ForeignKeyColumn)
                    || string.IsNullOrWhiteSpace(rel.ReferencedColumn))
                    continue;

                string childTable = table.Name;
                string parentTable = rel.TargetTable;
                if (string.Equals(rel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase))
                {
                    childTable = rel.TargetTable;
                    parentTable = table.Name;
                }

                string constraint = $"FK_{SanitizeIdent(childTable)}_{SanitizeIdent(rel.ForeignKeyColumn)}";
                string sql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'{EscapeSqlLiteral(constraint)}')
AND COL_LENGTH(N'{EscapeSqlLiteral(schema)}.{EscapeSqlLiteral(childTable)}', N'{EscapeSqlLiteral(rel.ForeignKeyColumn)}') IS NOT NULL
BEGIN
    ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(childTable)}
    ADD CONSTRAINT {QuoteIdent(constraint)}
    FOREIGN KEY ({QuoteIdent(rel.ForeignKeyColumn)})
    REFERENCES {QuoteIdent(schema)}.{QuoteIdent(parentTable)} ({QuoteIdent(rel.ReferencedColumn)});
END";
                try
                {
                    fixture.ExecuteNonQueryResult(sql, new List<DbParameter>());
                }
                catch
                {
                    // FK is optional; missing parent table or existing incompatible data should not fail the pack.
                }
            }
        }

        private static string NormalizeViewSql(AppConfigPackViewDto view)
        {
            string sql = view.CreateOrAlterSql.Trim().TrimEnd(';');
            if (sql.StartsWith("CREATE OR ALTER VIEW", StringComparison.OrdinalIgnoreCase)
                || sql.StartsWith("CREATE VIEW", StringComparison.OrdinalIgnoreCase)
                || sql.StartsWith("ALTER VIEW", StringComparison.OrdinalIgnoreCase))
            {
                if (sql.StartsWith("CREATE VIEW", StringComparison.OrdinalIgnoreCase)
                    && sql.IndexOf("CREATE OR ALTER", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    sql = "CREATE OR ALTER VIEW" + sql.Substring("CREATE VIEW".Length);
                }

                return sql;
            }

            string schema = string.IsNullOrWhiteSpace(view.SchemaOwner) ? "dbo" : view.SchemaOwner.Trim();
            return $"CREATE OR ALTER VIEW {QuoteIdent(schema)}.{QuoteIdent(view.Name.Trim())} AS {sql}";
        }

        private static string EscapeSqlLiteral(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }
    }
}
