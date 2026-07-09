using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private sealed class PlmExportEntityRef
        {
            public int EntityId { get; set; }
            public string EntityCode { get; set; }
            public string SchemaOwner { get; set; }
            public string TableName { get; set; }
        }

        private sealed class PlmExportTableRef
        {
            public string SchemaOwner { get; set; }
            public string TableName { get; set; }
            public List<PlmExportEntityRef> Entities { get; set; } = new List<PlmExportEntityRef>();

            public int PlmEntityCount => Entities.Count;
        }

        private sealed class SqlColumnDefinition
        {
            public string Name { get; set; }
            public string SqlType { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
        }

        public delegate void PlmExportProgressCallback(int progressPercent, string message);

        internal static PlmTableExportPlanDto BuildPlmTableExportPlan(string plmConnectionString, string tablePrefix)
        {
            var plan = new PlmTableExportPlanDto();
            try
            {
                var entityRefs = ReadPlmExportEntityRefs(plmConnectionString);
                var tables = GroupEntityRefsByTable(entityRefs);

                using (var conn = new SqlConnection(plmConnectionString))
                {
                    conn.Open();
                    foreach (var table in tables)
                    {
                        bool exists = TableExists(conn, table.SchemaOwner, table.TableName);
                        string targetTable = ResolveSystemDefineTargetTableName(table.TableName, tablePrefix);
                        plan.Tables.Add(new PlmTableExportPlanItemDto
                        {
                            SchemaOwner = table.SchemaOwner,
                            TableName = table.TableName,
                            TargetTableName = targetTable,
                            PlmEntityCount = table.PlmEntityCount,
                            SourceTableExists = exists,
                            Entities = table.Entities.Select(MapEntityRefDto).ToList()
                        });

                        if (!exists)
                            plan.Issues.AddRange(BuildMissingSourceIssues(table));
                    }
                }

                if (plan.Tables.Count == 0)
                {
                    plan.IsSuccess = false;
                    plan.ErrorMessage = "No System Define PLM tables (DataSourceFrom = 1) were found in pdmEntity.";
                    return plan;
                }

                plan.MissingSourceTableCount = plan.Tables.Count(t => !t.SourceTableExists);
                if (plan.Issues.Count > 0)
                    plan.ErrorMessage = FormatIssuesSummary(plan.Issues, "missing in PLM source");

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
            string tablePrefix,
            PlmExportProgressCallback progressCallback)
        {
            var exportResult = new PlmTableExportResultDto();
            var entityRefs = ReadPlmExportEntityRefs(plmConnectionString);
            var tables = GroupEntityRefsByTable(entityRefs);
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

                var missingTables = tables.Where(t => !TableExists(sourceConn, t.SchemaOwner, t.TableName)).ToList();
                foreach (var missing in missingTables)
                    exportResult.Issues.AddRange(BuildMissingSourceIssues(missing));

                var exportableTables = tables
                    .Where(t => TableExists(sourceConn, t.SchemaOwner, t.TableName))
                    .ToList();

                if (exportableTables.Count == 0)
                {
                    exportResult.IsSuccess = false;
                    exportResult.ErrorMessage = exportResult.Issues.Count > 0
                        ? FormatIssuesSummary(exportResult.Issues, "missing in PLM source")
                        : "No referenced PLM tables exist in the source database.";
                    return exportResult;
                }

                for (int i = 0; i < exportableTables.Count; i++)
                {
                    var table = exportableTables[i];
                    string targetTable = ResolveSystemDefineTargetTableName(table.TableName, tablePrefix);
                    int percent = (int)Math.Round((i / (double)exportableTables.Count) * 100);
                    progressCallback?.Invoke(percent,
                        $"Importing {table.SchemaOwner}.{table.TableName} → {table.SchemaOwner}.{targetTable}...");

                    var itemResult = new PlmTableExportResultItemDto
                    {
                        SchemaOwner = table.SchemaOwner,
                        TableName = table.TableName,
                        TargetTableName = targetTable,
                        Entities = table.Entities.Select(MapEntityRefDto).ToList()
                    };

                    try
                    {
                        int rows = CopyTableWithPrimaryKey(
                            sourceConn, targetConn, table.SchemaOwner, table.TableName, targetTable);
                        itemResult.IsSuccess = true;
                        itemResult.RowsCopied = rows;
                    }
                    catch (Exception ex)
                    {
                        itemResult.IsSuccess = false;
                        itemResult.ErrorMessage = ex.Message;
                        var failIssues = BuildExportFailedIssues(table, ex.Message);
                        exportResult.Issues.AddRange(failIssues);
                        exportResult.Tables.Add(itemResult);
                        exportResult.IsSuccess = false;
                        exportResult.ErrorMessage = FormatIssuesSummary(failIssues, "export failed");
                        return exportResult;
                    }

                    exportResult.Tables.Add(itemResult);
                }
            }

            exportResult.IsSuccess = true;
            progressCallback?.Invoke(100, "Import completed.");
            return exportResult;
        }

        internal static void WritePlmTableExportIssuesToLog(
            DatabaseFixture fixture,
            int sessionId,
            int? jobId,
            string action,
            string status,
            IEnumerable<PlmTableExportIssueDto> issues)
        {
            if (issues == null) return;
            foreach (var issue in issues)
            {
                WriteImportLog(
                    fixture,
                    sessionId,
                    jobId,
                    StepEntity,
                    action,
                    status,
                    $"{issue.SchemaOwner}.{issue.TableName}",
                    issue.EntityId.ToString(),
                    null,
                    null,
                    $"{issue.IssueType}: EntityCode={issue.EntityCode}. {issue.Message}");
            }
        }

        private static PlmTableExportEntityRefDto MapEntityRefDto(PlmExportEntityRef entity)
        {
            return new PlmTableExportEntityRefDto
            {
                EntityId = entity.EntityId,
                EntityCode = entity.EntityCode
            };
        }

        private static List<PlmExportTableRef> GroupEntityRefsByTable(List<PlmExportEntityRef> entityRefs)
        {
            return entityRefs
                .GroupBy(e => $"{e.SchemaOwner}\0{e.TableName}", StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    return new PlmExportTableRef
                    {
                        SchemaOwner = first.SchemaOwner,
                        TableName = first.TableName,
                        Entities = g.OrderBy(e => e.EntityId).ToList()
                    };
                })
                .OrderBy(t => t.SchemaOwner)
                .ThenBy(t => t.TableName)
                .ToList();
        }

        private static List<PlmTableExportIssueDto> BuildMissingSourceIssues(PlmExportTableRef table)
        {
            string qualified = $"{table.SchemaOwner}.{table.TableName}";
            return table.Entities.Select(entity => new PlmTableExportIssueDto
            {
                EntityId = entity.EntityId,
                EntityCode = entity.EntityCode,
                SchemaOwner = table.SchemaOwner,
                TableName = table.TableName,
                IssueType = PlmExportIssueMissingSourceTable,
                Message = $"Physical table {qualified} not found in PLM source database."
            }).ToList();
        }

        private static List<PlmTableExportIssueDto> BuildExportFailedIssues(PlmExportTableRef table, string errorMessage)
        {
            string qualified = $"{table.SchemaOwner}.{table.TableName}";
            return table.Entities.Select(entity => new PlmTableExportIssueDto
            {
                EntityId = entity.EntityId,
                EntityCode = entity.EntityCode,
                SchemaOwner = table.SchemaOwner,
                TableName = table.TableName,
                IssueType = PlmExportIssueExportFailed,
                Message = $"Export failed for {qualified}: {errorMessage}"
            }).ToList();
        }

        private static string FormatIssuesSummary(IEnumerable<PlmTableExportIssueDto> issues, string issueVerb)
        {
            var lines = issues.Select(issue =>
                $"{issue.SchemaOwner}.{issue.TableName}: EntityID={issue.EntityId}, EntityCode={issue.EntityCode} — {issueVerb}");
            return string.Join("; ", lines);
        }

        private static List<PlmExportEntityRef> ReadPlmExportEntityRefs(string plmConnectionString)
        {
            var list = new List<PlmExportEntityRef>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    e.EntityID,
    LEFT(LTRIM(RTRIM(e.EntityCode)), 100) AS EntityCode,
    LEFT(ISNULL(NULLIF(LTRIM(RTRIM(e.SchemaOwner)), ''), 'dbo'), 50) AS SchemaOwner,
    LEFT(LTRIM(RTRIM(e.SysTableName)), 100) AS TableName
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0
  AND e.DataSourceFrom = 1
  AND e.SysTableName IS NOT NULL
  AND LTRIM(RTRIM(e.SysTableName)) <> ''
ORDER BY SchemaOwner, TableName, e.EntityID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PlmExportEntityRef
                            {
                                EntityId = reader.GetInt32(0),
                                EntityCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                                SchemaOwner = reader.GetString(2),
                                TableName = reader.GetString(3)
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
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @Schema AND t.name = @Table";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", table);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static int CopyTableWithPrimaryKey(
            SqlConnection source, SqlConnection target, string schema, string sourceTable, string targetTable)
        {
            var columns = ReadColumnDefinitions(source, schema, sourceTable);
            if (columns.Count == 0)
                throw new InvalidOperationException("No columns found.");

            var pkColumns = ReadPrimaryKeyColumns(source, schema, sourceTable);
            EnsureSchemaExists(target, schema);
            DropTableIfExists(target, schema, targetTable);
            CreateTable(target, schema, targetTable, columns, pkColumns);

            using (var cmd = source.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM " + Qualify(schema, sourceTable);
                using (var reader = cmd.ExecuteReader())
                using (var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock, null))
                {
                    bulk.DestinationTableName = $"{schema}.{targetTable}";
                    bulk.BatchSize = 5000;
                    bulk.BulkCopyTimeout = 0;
                    foreach (var col in columns)
                        bulk.ColumnMappings.Add(col.Name, col.Name);
                    bulk.WriteToServer(reader);
                }
            }

            using (var countCmd = target.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM " + Qualify(schema, targetTable);
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

        internal static int CopyPlmSourceTableToTenant(
            string plmConnectionString,
            string tenantConnectionString,
            string sourceTableName,
            string targetTableName)
        {
            using (var sourceConn = new SqlConnection(plmConnectionString))
            using (var targetConn = new SqlConnection(tenantConnectionString))
            {
                sourceConn.Open();
                targetConn.Open();
                if (!TableExists(sourceConn, "dbo", sourceTableName))
                    throw new InvalidOperationException($"PLM table dbo.{sourceTableName} was not found.");

                return CopyTableWithPrimaryKey(sourceConn, targetConn, "dbo", sourceTableName, targetTableName);
            }
        }
    }
}
