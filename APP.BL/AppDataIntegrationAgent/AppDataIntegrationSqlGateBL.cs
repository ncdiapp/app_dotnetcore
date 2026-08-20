using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.BL;
using Newtonsoft.Json;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationSqlGateBL
    {
        public enum SqlKind
        {
            Select,
            Insert,
            Update,
            Delete,
            AlterAdd,
            CreateTable,
            Forbidden
        }

        public class SqlClassification
        {
            public SqlKind Kind { get; set; }
            public string Normalized { get; set; }
            public string Reason { get; set; }
            public bool IsReadOnly => Kind == SqlKind.Select;
            public bool Allowed => Kind != SqlKind.Forbidden;
        }

        public static SqlClassification Classify(string sql)
        {
            var result = new SqlClassification { Normalized = sql ?? "", Kind = SqlKind.Forbidden };
            if (string.IsNullOrWhiteSpace(sql))
            {
                result.Reason = "SQL is empty.";
                return result;
            }

            var stripped = StripComments(sql).Trim();
            result.Normalized = stripped;
            if (stripped.IndexOf(';') >= 0 && stripped.TrimEnd().TrimEnd(';').Contains(";"))
            {
                result.Reason = "Multiple SQL statements are not allowed.";
                return result;
            }

            var one = stripped.Trim().TrimEnd(';').Trim();
            var head = Regex.Replace(one, @"\s+", " ");
            var upper = head.ToUpperInvariant();

            if (ContainsForbidden(upper))
            {
                result.Reason = "Statement contains a forbidden keyword (DROP/TRUNCATE/EXEC/etc.).";
                return result;
            }

            if (upper.StartsWith("SELECT ") || upper.StartsWith("WITH "))
            {
                result.Kind = SqlKind.Select;
                return result;
            }
            if (upper.StartsWith("INSERT "))
            {
                result.Kind = SqlKind.Insert;
                return result;
            }
            if (upper.StartsWith("UPDATE "))
            {
                result.Kind = SqlKind.Update;
                return result;
            }
            if (upper.StartsWith("DELETE "))
            {
                result.Kind = SqlKind.Delete;
                return result;
            }
            if (upper.StartsWith("CREATE TABLE "))
            {
                result.Kind = SqlKind.CreateTable;
                return result;
            }
            if (upper.StartsWith("ALTER TABLE ") && Regex.IsMatch(upper, @"\bADD\b") && !upper.Contains(" DROP "))
            {
                result.Kind = SqlKind.AlterAdd;
                return result;
            }

            result.Reason = "Only SELECT, INSERT, UPDATE, DELETE, CREATE TABLE, and ALTER TABLE ... ADD are allowed.";
            return result;
        }

        public static string RunSelect(int dataSourceRegisterId, string sql, int rowLimit)
        {
            var classified = Classify(sql);
            if (classified.Kind != SqlKind.Select)
                throw new InvalidOperationException(classified.Reason ?? "Not a SELECT.");

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);
            var limited = WrapTop(classified.Normalized, rowLimit);
            var dt = fixture.RetriveDataTable(limited, new List<DbParameter>());
            return SerializeTable(dt, rowLimit);
        }

        public static string ExecuteWrite(int dataSourceRegisterId, string sql)
        {
            var classified = Classify(sql);
            if (!classified.Allowed || classified.IsReadOnly)
                throw new InvalidOperationException(classified.Reason ?? "Write SQL not allowed.");

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);
            fixture.ExecuteNonQueryResult(classified.Normalized, new List<DbParameter>());
            return JsonConvert.SerializeObject(new
            {
                Ok = true,
                Kind = classified.Kind.ToString(),
                Message = "Statement executed."
            });
        }

        public static string GetTableSchema(int dataSourceRegisterId, string schemaOwner, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName is required.");

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);
            var schema = string.IsNullOrWhiteSpace(schemaOwner) ? "dbo" : schemaOwner.Trim();
            const string sql = @"
SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE,
       c.IS_NULLABLE, c.COLUMN_DEFAULT,
       CASE WHEN pk.COLUMN_NAME IS NULL THEN 0 ELSE 1 END AS IsPrimaryKey
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
       AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON pk.TABLE_SCHEMA = c.TABLE_SCHEMA AND pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @Table
ORDER BY c.ORDINAL_POSITION";

            var p1 = fixture.CreateParameter("@Schema"); p1.Value = schema;
            var p2 = fixture.CreateParameter("@Table"); p2.Value = tableName.Trim();
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { p1, p2 });
            return SerializeTable(dt, 500);
        }

        private static bool ContainsForbidden(string upper)
        {
            if (upper.Contains(" DROP COLUMN") || upper.Contains(" DROP CONSTRAINT") || upper.Contains("DROP TABLE")
                || upper.Contains("DROP VIEW") || upper.Contains("DROP INDEX") || upper.Contains("TRUNCATE TABLE"))
                return true;
            if (upper.Contains(" EXEC ") || upper.Contains(" EXECUTE ") || upper.Contains(" EXEC(") || upper.Contains(" EXECUTE(") || upper.Contains(" XP_"))
                return true;
            return false;
        }

        private static string StripComments(string sql)
        {
            var noBlock = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            var lines = noBlock.Split('\n').Select(l =>
            {
                var idx = l.IndexOf("--", StringComparison.Ordinal);
                return idx >= 0 ? l.Substring(0, idx) : l;
            });
            return string.Join("\n", lines);
        }

        private static string WrapTop(string sql, int rowLimit)
        {
            var one = sql.Trim().TrimEnd(';');
            if (Regex.IsMatch(one, @"^\s*SELECT\s+TOP\s+", RegexOptions.IgnoreCase))
                return one;
            if (Regex.IsMatch(one, @"^\s*SELECT\s+", RegexOptions.IgnoreCase))
                return Regex.Replace(one, @"^\s*SELECT\s+", "SELECT TOP (" + rowLimit + ") ", RegexOptions.IgnoreCase);
            return one;
        }

        private static string SerializeTable(DataTable dt, int rowLimit)
        {
            if (dt == null) return JsonConvert.SerializeObject(new { Rows = new object[0], RowCount = 0 });
            var rows = new List<Dictionary<string, object>>();
            var take = Math.Min(dt.Rows.Count, rowLimit);
            for (var i = 0; i < take; i++)
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn col in dt.Columns)
                {
                    var v = dt.Rows[i][col];
                    dict[col.ColumnName] = v == DBNull.Value ? null : v;
                }
                rows.Add(dict);
            }
            return JsonConvert.SerializeObject(new { RowCount = dt.Rows.Count, Returned = rows.Count, Rows = rows });
        }
    }
}
