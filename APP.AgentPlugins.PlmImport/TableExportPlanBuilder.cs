using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Pure PLM-read plan for System Define table export (pdmEntity DataSourceFrom=1).
/// No ServerContext / tenant writes — Host owns session decrypt, jobs, and physical copy.
/// </summary>
public static class TableExportPlanBuilder
{
    private const string DefaultTablePrefix = "Plm_";
    private const string IssueMissingSourceTable = "MissingSourceTable";

    private sealed class EntityRef
    {
        public int EntityId { get; set; }
        public string EntityCode { get; set; }
        public string SchemaOwner { get; set; }
        public string TableName { get; set; }
    }

    private sealed class TableRef
    {
        public string SchemaOwner { get; set; }
        public string TableName { get; set; }
        public List<EntityRef> Entities { get; set; } = new List<EntityRef>();
        public int PlmEntityCount => Entities.Count;
    }

    public static PlmTableExportPlanDto Build(string plmConnectionString, string tablePrefix)
    {
        var plan = new PlmTableExportPlanDto();
        if (string.IsNullOrWhiteSpace(plmConnectionString))
        {
            plan.IsSuccess = false;
            plan.ErrorMessage = "plmConnectionString is required.";
            return plan;
        }

        try
        {
            string prefix = SanitizeTablePrefix(tablePrefix);
            var entityRefs = ReadEntityRefs(plmConnectionString.Trim());
            var tables = GroupByTable(entityRefs);

            using (var conn = new SqlConnection(plmConnectionString.Trim()))
            {
                conn.Open();
                foreach (var table in tables)
                {
                    bool exists = TableExists(conn, table.SchemaOwner, table.TableName);
                    string targetTable = ResolveTargetTableName(table.TableName, prefix);
                    plan.Tables.Add(new PlmTableExportPlanItemDto
                    {
                        SchemaOwner = table.SchemaOwner,
                        TableName = table.TableName,
                        TargetTableName = targetTable,
                        PlmEntityCount = table.PlmEntityCount,
                        SourceTableExists = exists,
                        Entities = table.Entities.Select(e => new PlmTableExportEntityRefDto
                        {
                            EntityId = e.EntityId,
                            EntityCode = e.EntityCode
                        }).ToList()
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

    private static List<EntityRef> ReadEntityRefs(string plmConnectionString)
    {
        var list = new List<EntityRef>();
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
                        list.Add(new EntityRef
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

    private static List<TableRef> GroupByTable(List<EntityRef> entityRefs)
    {
        return entityRefs
            .GroupBy(e => $"{e.SchemaOwner}\0{e.TableName}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                return new TableRef
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

    private static List<PlmTableExportIssueDto> BuildMissingSourceIssues(TableRef table)
    {
        string qualified = $"{table.SchemaOwner}.{table.TableName}";
        return table.Entities.Select(entity => new PlmTableExportIssueDto
        {
            EntityId = entity.EntityId,
            EntityCode = entity.EntityCode,
            SchemaOwner = table.SchemaOwner,
            TableName = table.TableName,
            IssueType = IssueMissingSourceTable,
            Message = $"Physical table {qualified} not found in PLM source database."
        }).ToList();
    }

    private static string FormatIssuesSummary(IEnumerable<PlmTableExportIssueDto> issues, string issueVerb)
    {
        var lines = issues.Select(issue =>
            $"{issue.SchemaOwner}.{issue.TableName}: EntityID={issue.EntityId}, EntityCode={issue.EntityCode} — {issueVerb}");
        return string.Join("; ", lines);
    }

    private static string SanitizeTablePrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultTablePrefix;

        var sb = new System.Text.StringBuilder();
        foreach (char ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
        }

        string result = sb.ToString();
        if (result.Length == 0)
            return DefaultTablePrefix;

        return result.Length <= 30 ? result : result.Substring(0, 30);
    }

    private static string ResolveTargetTableName(string sourceTableName, string tablePrefix)
    {
        if (string.IsNullOrWhiteSpace(sourceTableName))
            return null;

        string prefix = SanitizeTablePrefix(tablePrefix);
        string source = sourceTableName.Trim();
        string target = source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? source
            : prefix + source;
        return target.Length <= 100 ? target : target.Substring(0, 100);
    }
}
