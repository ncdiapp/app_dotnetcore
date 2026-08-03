using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const string TchpBodyPartTableName = "TchpBodyPart";
        private const string TchpPomTemplateTableName = "TchpPomTemplate";
        private const string TchpPomTemplatePartTableName = "TchpPomTemplatePart";

        private const string PlmBodyPartSourceTable = "pdmV2kBodyPart";
        private const string PlmBodyTypeSourceTable = "pdmv2kBodyType";

        private sealed class TchpBodyPartRow
        {
            public int BodyPartId { get; set; }
            public string Code { get; set; }
            public string BodyPartName { get; set; }
            public decimal? Tolerance { get; set; }
            public decimal GradingPlusValue { get; set; }
            public decimal GradingMinuValue { get; set; }
        }

        private sealed class TchpPomTemplateRow
        {
            public int PomTemplateId { get; set; }
            public string TemplateCode { get; set; }
            public string TemplateName { get; set; }
            public int? DefaultBaseSizeId { get; set; }
        }

        private sealed class TchpPomTemplatePartRow
        {
            public int PomTemplatePartId { get; set; }
            public int PomTemplateId { get; set; }
            public int BodyPartId { get; set; }
            public string BodypartAliasName { get; set; }
            public int Sort { get; set; }
        }

        private static void RequireTchpPomSchema(SqlConnection tenantConn)
        {
            if (!TemplateTableExists(tenantConn, null, TchpBodyPartTableName)
                || !TemplateTableExists(tenantConn, null, TchpPomTemplateTableName)
                || !TemplateTableExists(tenantConn, null, TchpPomTemplatePartTableName))
            {
                throw new InvalidOperationException(
                    "Tchp POM schema is missing. Run Document/Design/POM_Grading_QC_NewSchema.sql "
                    + $"(need {TchpBodyPartTableName}, {TchpPomTemplateTableName}, {TchpPomTemplatePartTableName}).");
            }
        }

        /// <summary>
        /// Upserts TchpBodyPart + TchpPomTemplate + TchpPomTemplatePart from PLM source tables.
        /// Preserves PLM PKs (IDENTITY_INSERT). Parts without matching BodyPart/Template are skipped.
        /// </summary>
        private static void ImportTchpPomTemplateMasterData(
            string plmConnectionString,
            string tenantConnectionString,
            PlmPomImportExecuteResultDto executeResult)
        {
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                RequireTchpPomSchema(tenantConn);

                var bodyParts = ReadPlmBodyPartsForTchp(plmConnectionString);
                NormalizeTchpBodyPartCodes(bodyParts);
                executeResult.TchpBodyPartRowsImported = UpsertTchpBodyParts(tenantConn, bodyParts);
                executeResult.Messages.Add(
                    $"Upserted {executeResult.TchpBodyPartRowsImported} row(s) into {TchpBodyPartTableName}.");

                var templates = ReadPlmBodyTypesForTchp(plmConnectionString);
                NormalizeTchpTemplateCodes(templates);
                NullInvalidDefaultBaseSizeIds(tenantConn, templates);
                executeResult.TchpPomTemplateRowsImported = UpsertTchpPomTemplates(tenantConn, templates);
                executeResult.Messages.Add(
                    $"Upserted {executeResult.TchpPomTemplateRowsImported} row(s) into {TchpPomTemplateTableName}.");

                var parts = ReadPlmBodyTypeDetailsForTchp(plmConnectionString);
                var bodyPartIds = new HashSet<int>(bodyParts.Select(b => b.BodyPartId));
                // Also accept BodyParts already in tenant (re-run without re-reading all PLM parts).
                foreach (var id in LoadExistingTchpBodyPartIds(tenantConn))
                    bodyPartIds.Add(id);
                var templateIds = new HashSet<int>(LoadExistingTchpPomTemplateIds(tenantConn));

                var validParts = parts
                    .Where(p => templateIds.Contains(p.PomTemplateId) && bodyPartIds.Contains(p.BodyPartId))
                    .ToList();
                int skipped = parts.Count - validParts.Count;
                if (skipped > 0)
                    executeResult.Messages.Add($"Skipped {skipped} BodyTypeDetail row(s) missing Template/BodyPart FK.");

                executeResult.TchpPomTemplatePartRowsImported = UpsertTchpPomTemplateParts(tenantConn, validParts);
                executeResult.Messages.Add(
                    $"Upserted {executeResult.TchpPomTemplatePartRowsImported} row(s) into {TchpPomTemplatePartTableName}.");

                // Keep legacy DTO field for UI that still shows BodyTypeDetailRowsImported.
                executeResult.BodyTypeDetailRowsImported = executeResult.TchpPomTemplatePartRowsImported;
            }
        }

        private static List<TchpBodyPartRow> ReadPlmBodyPartsForTchp(string plmConnectionString)
        {
            var rows = new List<TchpBodyPartRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                string table = ResolvePlmTableName(conn, PlmBodyPartSourceTable, "PdmV2kBodyPart", "pdmV2kBodyPart");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
SELECT BodyPartID,
       Code,
       BodyPartName,
       Tolerance,
       ISNULL(GradingPlusValue, 0),
       ISNULL(GradingMinuValue, 0)
FROM dbo.[{table}];";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new TchpBodyPartRow
                            {
                                BodyPartId = reader.GetInt32(0),
                                Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                                BodyPartName = reader.IsDBNull(2) ? $"BodyPart_{reader.GetInt32(0)}" : reader.GetString(2),
                                Tolerance = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3),
                                GradingPlusValue = reader.GetDecimal(4),
                                GradingMinuValue = reader.GetDecimal(5)
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static List<TchpPomTemplateRow> ReadPlmBodyTypesForTchp(string plmConnectionString)
        {
            var rows = new List<TchpPomTemplateRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                string table = ResolvePlmTableName(conn, PlmBodyTypeSourceTable, "pdmV2kBodyType", "PdmV2kBodyType");
                bool hasDefaultBase = ColumnExists(conn, table, "DefaultBaseSizeDetailID")
                    || ColumnExists(conn, table, "DefaultBaseSizeDetailId");
                string baseCol = ColumnExists(conn, table, "DefaultBaseSizeDetailID")
                    ? "DefaultBaseSizeDetailID"
                    : (ColumnExists(conn, table, "DefaultBaseSizeDetailId") ? "DefaultBaseSizeDetailId" : null);
                string nameCol = ColumnExists(conn, table, "BodyTypeName") ? "BodyTypeName" : "Name";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = hasDefaultBase && baseCol != null
                        ? $@"SELECT BodyTypeID, [{nameCol}], [{baseCol}] FROM dbo.[{table}];"
                        : $@"SELECT BodyTypeID, [{nameCol}], CAST(NULL AS INT) FROM dbo.[{table}];";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(1) ? $"Template_{reader.GetInt32(0)}" : reader.GetString(1);
                            rows.Add(new TchpPomTemplateRow
                            {
                                PomTemplateId = reader.GetInt32(0),
                                TemplateName = Truncate(name.Trim(), 100),
                                TemplateCode = null,
                                DefaultBaseSizeId = reader.IsDBNull(2) ? (int?)null : Convert.ToInt32(reader.GetValue(2))
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static List<TchpPomTemplatePartRow> ReadPlmBodyTypeDetailsForTchp(string plmConnectionString)
        {
            var rows = new List<TchpPomTemplatePartRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                string table = ResolvePlmTableName(conn, PomBodyTypeDetailSourceTable, "pdmV2kBodyTypeDetail", "PdmV2kBodyTypeDetail");
                string pkCol = ColumnExists(conn, table, "BodyTypeDetailID") ? "BodyTypeDetailID" : "BodyTypeDetailId";
                string aliasCol = ColumnExists(conn, table, "BodypartAliasName")
                    ? "BodypartAliasName"
                    : (ColumnExists(conn, table, "BodyPartAliasName") ? "BodyPartAliasName" : null);
                string sortCol = ColumnExists(conn, table, "Sort") ? "Sort" : null;

                using (var cmd = conn.CreateCommand())
                {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ").Append(pkCol).Append(", BodyTypeID, BodyPartID");
                    sb.Append(aliasCol != null ? $", [{aliasCol}]" : ", CAST(NULL AS NVARCHAR(50))");
                    sb.Append(sortCol != null ? $", ISNULL([{sortCol}], 0)" : ", 0");
                    sb.Append(" FROM dbo.[").Append(table).Append("];");
                    cmd.CommandText = sb.ToString();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new TchpPomTemplatePartRow
                            {
                                PomTemplatePartId = reader.GetInt32(0),
                                PomTemplateId = reader.GetInt32(1),
                                BodyPartId = reader.GetInt32(2),
                                BodypartAliasName = reader.IsDBNull(3) ? null : Truncate(reader.GetString(3), 50),
                                Sort = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4))
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static string ResolvePlmTableName(SqlConnection plmConn, params string[] candidates)
        {
            foreach (var name in candidates.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                if (TemplateTableExists(plmConn, null, name))
                    return name;
            }

            throw new InvalidOperationException(
                "PLM source table not found. Tried: " + string.Join(", ", candidates));
        }

        private static bool ColumnExists(SqlConnection conn, string tableName, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Table AND COLUMN_NAME = @Column";
                cmd.Parameters.AddWithValue("@Table", tableName);
                cmd.Parameters.AddWithValue("@Column", columnName);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void NormalizeTchpBodyPartCodes(List<TchpBodyPartRow> rows)
        {
            var codeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                string key = string.IsNullOrWhiteSpace(row.Code) ? string.Empty : row.Code.Trim();
                if (!codeCounts.ContainsKey(key))
                    codeCounts[key] = 0;
                codeCounts[key]++;
            }

            foreach (var row in rows)
            {
                string raw = string.IsNullOrWhiteSpace(row.Code) ? null : row.Code.Trim();
                string key = raw ?? string.Empty;
                int groupCnt = codeCounts[key];

                string code;
                if (string.IsNullOrWhiteSpace(raw))
                    code = "BP_" + row.BodyPartId;
                else if (groupCnt > 1)
                    code = Truncate(raw, 40) + "_" + row.BodyPartId;
                else
                    code = raw;

                row.Code = Truncate(code, 50);
                row.BodyPartName = Truncate(row.BodyPartName ?? ("BodyPart_" + row.BodyPartId), 100);
            }
        }

        private static void NormalizeTchpTemplateCodes(List<TchpPomTemplateRow> rows)
        {
            foreach (var row in rows)
                row.TemplateCode = NormalizeTemplateCode(row.TemplateName);

            var collisions = rows.GroupBy(r => r.TemplateCode, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();
            foreach (var row in collisions)
                row.TemplateCode = Truncate(row.TemplateCode, 40) + "_" + row.PomTemplateId;
        }

        private static string NormalizeTemplateCode(string templateName)
        {
            string upper = (templateName ?? string.Empty).ToUpperInvariant();
            string replaced = Regex.Replace(upper, @"[^A-Z0-9]+", "_");
            replaced = Regex.Replace(replaced, @"_+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(replaced))
                replaced = "TEMPLATE";
            return Truncate(replaced, 50);
        }

        private static void NullInvalidDefaultBaseSizeIds(SqlConnection tenantConn, List<TchpPomTemplateRow> rows)
        {
            if (!TemplateTableExists(tenantConn, null, "TchpSizeRunSize"))
            {
                foreach (var row in rows)
                    row.DefaultBaseSizeId = null;
                return;
            }

            var valid = new HashSet<int>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT SizeRunSizeId FROM dbo.TchpSizeRunSize";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        valid.Add(reader.GetInt32(0));
                }
            }

            foreach (var row in rows)
            {
                if (row.DefaultBaseSizeId.HasValue && !valid.Contains(row.DefaultBaseSizeId.Value))
                    row.DefaultBaseSizeId = null;
            }
        }

        private static HashSet<int> LoadExistingTchpBodyPartIds(SqlConnection tenantConn)
        {
            var ids = new HashSet<int>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT BodyPartId FROM dbo.TchpBodyPart";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ids.Add(reader.GetInt32(0));
                }
            }

            return ids;
        }

        private static HashSet<int> LoadExistingTchpPomTemplateIds(SqlConnection tenantConn)
        {
            var ids = new HashSet<int>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT PomTemplateId FROM dbo.TchpPomTemplate";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ids.Add(reader.GetInt32(0));
                }
            }

            return ids;
        }

        private static int UpsertTchpBodyParts(SqlConnection tenantConn, List<TchpBodyPartRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpBodyPartImport", @"
CREATE TABLE #TchpBodyPartImport (
    BodyPartId INT NOT NULL PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    BodyPartName NVARCHAR(100) NOT NULL,
    Tolerance DECIMAL(10,3) NULL,
    GradingPlusValue DECIMAL(10,3) NOT NULL,
    GradingMinuValue DECIMAL(10,3) NOT NULL
);");

            BulkInsert(tenantConn, "#TchpBodyPartImport", rows.Select(r => new object[]
            {
                r.BodyPartId, r.Code, r.BodyPartName, (object)r.Tolerance ?? DBNull.Value, r.GradingPlusValue, r.GradingMinuValue
            }), new[] { "BodyPartId", "Code", "BodyPartName", "Tolerance", "GradingPlusValue", "GradingMinuValue" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpBodyPart ON;
MERGE dbo.TchpBodyPart AS t
USING #TchpBodyPartImport AS s ON t.BodyPartId = s.BodyPartId
WHEN MATCHED THEN UPDATE SET
    Code = s.Code,
    BodyPartName = s.BodyPartName,
    Tolerance = s.Tolerance,
    GradingPlusValue = s.GradingPlusValue,
    GradingMinuValue = s.GradingMinuValue,
    IsActive = 1,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (BodyPartId, Code, BodyPartName, Tolerance, GradingPlusValue, GradingMinuValue, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.BodyPartId, s.Code, s.BodyPartName, s.Tolerance, s.GradingPlusValue, s.GradingMinuValue, 1, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpBodyPart OFF;
SELECT COUNT(*) FROM #TchpBodyPartImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int UpsertTchpPomTemplates(SqlConnection tenantConn, List<TchpPomTemplateRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpPomTemplateImport", @"
CREATE TABLE #TchpPomTemplateImport (
    PomTemplateId INT NOT NULL PRIMARY KEY,
    TemplateCode NVARCHAR(50) NOT NULL,
    TemplateName NVARCHAR(100) NOT NULL,
    DefaultBaseSizeId INT NULL
);");

            BulkInsert(tenantConn, "#TchpPomTemplateImport", rows.Select(r => new object[]
            {
                r.PomTemplateId, r.TemplateCode, r.TemplateName, (object)r.DefaultBaseSizeId ?? DBNull.Value
            }), new[] { "PomTemplateId", "TemplateCode", "TemplateName", "DefaultBaseSizeId" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpPomTemplate ON;
MERGE dbo.TchpPomTemplate AS t
USING #TchpPomTemplateImport AS s ON t.PomTemplateId = s.PomTemplateId
WHEN MATCHED THEN UPDATE SET
    TemplateCode = s.TemplateCode,
    TemplateName = s.TemplateName,
    DefaultBaseSizeId = s.DefaultBaseSizeId,
    IsActive = 1,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (PomTemplateId, TemplateCode, TemplateName, DefaultBaseSizeId, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.PomTemplateId, s.TemplateCode, s.TemplateName, s.DefaultBaseSizeId, 1, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpPomTemplate OFF;
SELECT COUNT(*) FROM #TchpPomTemplateImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int UpsertTchpPomTemplateParts(SqlConnection tenantConn, List<TchpPomTemplatePartRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpPomTemplatePartImport", @"
CREATE TABLE #TchpPomTemplatePartImport (
    PomTemplatePartId INT NOT NULL PRIMARY KEY,
    PomTemplateId INT NOT NULL,
    BodyPartId INT NOT NULL,
    BodypartAliasName NVARCHAR(50) NULL,
    Sort INT NOT NULL
);");

            BulkInsert(tenantConn, "#TchpPomTemplatePartImport", rows.Select(r => new object[]
            {
                r.PomTemplatePartId, r.PomTemplateId, r.BodyPartId, (object)r.BodypartAliasName ?? DBNull.Value, r.Sort
            }), new[] { "PomTemplatePartId", "PomTemplateId", "BodyPartId", "BodypartAliasName", "Sort" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpPomTemplatePart ON;
MERGE dbo.TchpPomTemplatePart AS t
USING #TchpPomTemplatePartImport AS s ON t.PomTemplatePartId = s.PomTemplatePartId
WHEN MATCHED THEN UPDATE SET
    PomTemplateId = s.PomTemplateId,
    BodyPartId = s.BodyPartId,
    BodypartAliasName = s.BodypartAliasName,
    Sort = s.Sort,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (PomTemplatePartId, PomTemplateId, BodyPartId, BodypartAliasName, Sort, AppCreatedDate, AppModifiedDate)
    VALUES (s.PomTemplatePartId, s.PomTemplateId, s.BodyPartId, s.BodypartAliasName, s.Sort, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpPomTemplatePart OFF;
SELECT COUNT(*) FROM #TchpPomTemplatePartImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void EnsureTempTable(SqlConnection conn, string tempName, string createSql)
        {
            using (var drop = conn.CreateCommand())
            {
                drop.CommandText = $"IF OBJECT_ID('tempdb..{tempName}') IS NOT NULL DROP TABLE {tempName};";
                drop.ExecuteNonQuery();
            }

            using (var create = conn.CreateCommand())
            {
                create.CommandText = createSql;
                create.ExecuteNonQuery();
            }
        }

        private static void BulkInsert(SqlConnection conn, string destinationTable, IEnumerable<object[]> rows, string[] columns)
        {
            var table = new DataTable();
            foreach (var col in columns)
                table.Columns.Add(col);

            foreach (var row in rows)
                table.Rows.Add(row);

            using (var bulk = new SqlBulkCopy(conn))
            {
                bulk.DestinationTableName = destinationTable;
                bulk.BatchSize = 5000;
                bulk.BulkCopyTimeout = 0;
                foreach (var col in columns)
                    bulk.ColumnMappings.Add(col, col);
                bulk.WriteToServer(table);
            }
        }
    }
}
