using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using APP.Components.EntityDto;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private sealed class PlmExportTableRef
        {
            public string SchemaOwner { get; set; }
            public string TableName { get; set; }
            public int PlmEntityCount { get; set; }
        }

        private sealed class SqlColumnDefinition
        {
            public string Name { get; set; }
            public string SqlType { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
        }

        public delegate void PlmExportProgressCallback(int progressPercent, string message);

        internal static PlmTableExportPlanDto BuildPlmTableExportPlan(string plmConnectionString)
        {
            var plan = new PlmTableExportPlanDto();
            try
            {
                var tables = ReadPlmExportTableRefs(plmConnectionString);
                using (var conn = new SqlConnection(plmConnectionString))
                {
                    conn.Open();
                    foreach (var table in tables)
                    {
                        bool exists = TableExists(conn, table.SchemaOwner, table.TableName);
                        plan.Tables.Add(new PlmTableExportPlanItemDto
                        {
                            SchemaOwner = table.SchemaOwner,
                            TableName = table.TableName,
                            PlmEntityCount = table.PlmEntityCount,
                            SourceTableExists = exists
                        });
                    }
                }

                if (plan.Tables.Count == 0)
                {
                    plan.IsSuccess = false;
                    plan.ErrorMessage = "No System Define PLM tables (DataSourceFrom = 1) were found in pdmEntity.";
                    return plan;
                }

                if (plan.Tables.Any(t => !t.SourceTableExists))
                {
                    plan.IsSuccess = false;
                    plan.ErrorMessage = "One or more referenced PLM tables do not exist in the source database.";
                    return plan;
                }

                plan.IsSuccess = true;
            }
            catch (Exception ex)
            {
                plan.IsSuccess = false;
                plan.ErrorMessage = ex.Message;
            }

            return plan;
        }

        internal static PlmTableExportResultDto ExportPlmTablesToTenant(
            string plmConnectionString,
            string tenantConnectionString,
            PlmExportProgressCallback progressCallback)
        {
            var exportResult = new PlmTableExportResultDto();
            var tables = ReadPlmExportTableRefs(plmConnectionString);
            if (tables.Count == 0)
            {
                exportResult.IsSuccess = false;
                exportResult.ErrorMessage = "No PLM tables to export.";
                return exportResult;
            }

            using (var sourceConn = new SqlConnection(plmConnectionString))
            using (var targetConn = new SqlConnection(tenantConnectionString))
            {
                sourceConn.Open();
                targetConn.Open();

                for (int i = 0; i < tables.Count; i++)
                {
                    var table = tables[i];
                    int percent = (int)Math.Round((i / (double)tables.Count) * 100);
                    progressCallback?.Invoke(percent, $"Exporting {table.SchemaOwner}.{table.TableName}...");

                    var itemResult = new PlmTableExportResultItemDto
                    {
                        SchemaOwner = table.SchemaOwner,
                        TableName = table.TableName
                    };

                    try
                    {
                        if (!TableExists(sourceConn, table.SchemaOwner, table.TableName))
                            throw new InvalidOperationException("Source table does not exist.");

                        int rows = CopyTableWithPrimaryKey(sourceConn, targetConn, table.SchemaOwner, table.TableName);
                        itemResult.IsSuccess = true;
                        itemResult.RowsCopied = rows;
                    }
                    catch (Exception ex)
                    {
                        itemResult.IsSuccess = false;
                        itemResult.ErrorMessage = ex.Message;
                        exportResult.Tables.Add(itemResult);
                        exportResult.IsSuccess = false;
                        exportResult.ErrorMessage =
                            $"Export failed at {table.SchemaOwner}.{table.TableName}: {ex.Message}";
                        return exportResult;
                    }

                    exportResult.Tables.Add(itemResult);
                }
            }

            exportResult.IsSuccess = true;
            progressCallback?.Invoke(100, "Export completed.");
            return exportResult;
        }

        private static List<PlmExportTableRef> ReadPlmExportTableRefs(string plmConnectionString)
        {
            var list = new List<PlmExportTableRef>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    LEFT(ISNULL(NULLIF(LTRIM(RTRIM(e.SchemaOwner)), ''), 'dbo'), 50) AS SchemaOwner,
    LEFT(LTRIM(RTRIM(e.SysTableName)), 100) AS TableName,
    COUNT(*) AS PlmEntityCount
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0
  AND e.DataSourceFrom = 1
  AND e.SysTableName IS NOT NULL
  AND LTRIM(RTRIM(e.SysTableName)) <> ''
GROUP BY
    LEFT(ISNULL(NULLIF(LTRIM(RTRIM(e.SchemaOwner)), ''), 'dbo'), 50),
    LEFT(LTRIM(RTRIM(e.SysTableName)), 100)
ORDER BY SchemaOwner, TableName";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PlmExportTableRef
                            {
                                SchemaOwner = reader.GetString(0),
                                TableName = reader.GetString(1),
                                PlmEntityCount = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }
            return list;
        }

        private static bool TableExists(SqlConnection conn, string schema, string table)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 1
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table AND TABLE_TYPE = 'BASE TABLE'";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", table);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static int CopyTableWithPrimaryKey(SqlConnection source, SqlConnection target, string schema, string table)
        {
            var columns = ReadColumnDefinitions(source, schema, table);
            if (columns.Count == 0)
                throw new InvalidOperationException("No columns found.");

            var pkColumns = ReadPrimaryKeyColumns(source, schema, table);
            EnsureSchemaExists(target, schema);
            DropTableIfExists(target, schema, table);
            CreateTable(target, schema, table, columns, pkColumns);

            string sourceQualified = Qualify(schema, table);
            string selectSql = "SELECT * FROM " + sourceQualified;

            using (var cmd = source.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM " + sourceQualified;
                using (var reader = cmd.ExecuteReader())
                using (var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock, null))
                {
                    bulk.DestinationTableName = $"{schema}.{table}";
                    bulk.BatchSize = 5000;
                    bulk.BulkCopyTimeout = 0;
                    foreach (var col in columns)
                        bulk.ColumnMappings.Add(col.Name, col.Name);
                    bulk.WriteToServer(reader);
                }
            }

            using (var countCmd = target.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM " + Qualify(schema, table);
                return Convert.ToInt32(countCmd.ExecuteScalar());
            }
        }

        private static List<SqlColumnDefinition> ReadColumnDefinitions(SqlConnection conn, string schema, string table)
        {
            var list = new List<SqlColumnDefinition>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    c.IS_NULLABLE,
    COLUMNPROPERTY(OBJECT_ID(QUOTENAME(@Schema) + '.' + QUOTENAME(@Table)), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @Table
ORDER BY c.ORDINAL_POSITION";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", table);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SqlColumnDefinition
                        {
                            Name = reader.GetString(0),
                            SqlType = BuildSqlType(
                                reader.GetString(1),
                                reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                reader.IsDBNull(3) ? (byte?)null : reader.GetByte(3),
                                reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)),
                            IsNullable = string.Equals(reader.GetString(5), "YES", StringComparison.OrdinalIgnoreCase),
                            IsIdentity = !reader.IsDBNull(6) && Convert.ToInt32(reader.GetValue(6)) == 1
                        });
                    }
                }
            }
            return list;
        }

        private static List<string> ReadPrimaryKeyColumns(SqlConnection conn, string schema, string table)
        {
            var list = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT c.name
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 1
INNER JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ic.column_id
WHERE s.name = @Schema AND t.name = @Table
ORDER BY ic.key_ordinal";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", table);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(reader.GetString(0));
                }
            }
            return list;
        }

        private static string BuildSqlType(string dataType, int? charLen, byte? precision, int? scale)
        {
            switch (dataType.ToLowerInvariant())
            {
                case "nvarchar":
                case "varchar":
                case "nchar":
                case "char":
                case "binary":
                case "varbinary":
                    if (charLen == -1) return $"{dataType}(max)";
                    return $"{dataType}({charLen ?? 1})";
                case "decimal":
                case "numeric":
                    return $"{dataType}({precision ?? 18},{scale ?? 0})";
                case "datetime2":
                case "datetimeoffset":
                case "time":
                    return scale.HasValue ? $"{dataType}({scale.Value})" : dataType;
                default:
                    return dataType;
            }
        }

        private static void EnsureSchemaExists(SqlConnection target, string schema)
        {
            if (string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
                return;

            using (var cmd = target.CreateCommand())
            {
                cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @Schema)
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE SCHEMA ' + QUOTENAME(@Schema);
    EXEC sp_executesql @sql;
END";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DropTableIfExists(SqlConnection target, string schema, string table)
        {
            using (var cmd = target.CreateCommand())
            {
                cmd.CommandText = @"
IF OBJECT_ID(QUOTENAME(@Schema) + '.' + QUOTENAME(@Table), 'U') IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'DROP TABLE ' + QUOTENAME(@Schema) + '.' + QUOTENAME(@Table);
    EXEC sp_executesql @sql;
END";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", table);
                cmd.ExecuteNonQuery();
            }
        }

        private static void CreateTable(
            SqlConnection target, string schema, string table,
            List<SqlColumnDefinition> columns, List<string> pkColumns)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE ").Append(Qualify(schema, table)).Append(" (");

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                if (i > 0) sb.Append(',');
                sb.Append('[').Append(col.Name).Append("] ").Append(col.SqlType);
                if (col.IsIdentity) sb.Append(" IDENTITY(1,1)");
                sb.Append(col.IsNullable ? " NULL" : " NOT NULL");
            }

            if (pkColumns.Count > 0)
            {
                sb.Append(", CONSTRAINT [PK_")
                    .Append(table)
                    .Append("_Export] PRIMARY KEY (");
                sb.Append(string.Join(", ", pkColumns.Select(c => "[" + c + "]")));
                sb.Append(')');
            }

            sb.Append(')');

            using (var cmd = target.CreateCommand())
            {
                cmd.CommandText = sb.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        private static string Qualify(string schema, string table)
        {
            return "[" + schema.Replace("]", "]]") + "].[" + table.Replace("]", "]]") + "]";
        }
    }
}
