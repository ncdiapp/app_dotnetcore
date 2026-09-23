using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
    {
        private const string TchpGradeRuleSetTableName = "TchpGradeRuleSet";
        private const string TchpGradeRuleTableName = "TchpGradeRule";

        private sealed class TchpGradeRuleSetRow
        {
            public int GradeRuleSetId { get; set; }
            public string GradeRuleSetName { get; set; }
            public string Description { get; set; }
        }

        private sealed class TchpGradeRuleRow
        {
            public int GradeRuleId { get; set; }
            public int GradeRuleSetId { get; set; }
            public int BodyPartId { get; set; }
            public string BodyPartCode { get; set; }
            public decimal GradingPlusValue { get; set; }
            public decimal GradingMinuValue { get; set; }
            public bool IsSymmetric { get; set; }
            public short Sort { get; set; }
        }

        private static void FillGradeRulePreview(
            PlmPomImportPreviewDto preview,
            string plmConnectionString,
            string tenantConnectionString)
        {
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                EnsureTchpGradeRuleSchema(tenantConn);
                preview.TchpGradeRuleSetRowCount = TemplateTableExists(tenantConn, null, TchpGradeRuleSetTableName)
                    ? CountTableRows(tenantConn, TchpGradeRuleSetTableName) : 0;
                preview.TchpGradeRuleRowCount = TemplateTableExists(tenantConn, null, TchpGradeRuleTableName)
                    ? CountTableRows(tenantConn, TchpGradeRuleTableName) : 0;
            }

            try
            {
                var sets = ReadGradeRuleSetsFromPlm(plmConnectionString);
                var rules = ReadGradeRulesFromPlm(plmConnectionString);
                preview.PlannedGradeRuleSetCount = sets.Count;
                preview.PlannedGradeRuleCount = rules.Count;
            }
            catch (Exception ex)
            {
                preview.Warnings.Add("GradeRule source probe failed: " + ex.Message);
            }

            preview.PlannedActions.Add(
                "Upsert TchpGradeRuleSet / TchpGradeRule (G-B: one CUSTOM set per BodyType with ≥1 grading row)");
        }

        private static void ImportTchpGradeRules(
            string plmConnectionString,
            string tenantConnectionString,
            PlmPomImportExecuteResultDto executeResult)
        {
            var sets = ReadGradeRuleSetsFromPlm(plmConnectionString);
            var rules = ReadGradeRulesFromPlm(plmConnectionString);
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                EnsureTchpGradeRuleSchema(tenantConn);
                ApplyTchpBodyPartCodes(tenantConn, rules);
                rules = rules
                    .Where(r => !string.IsNullOrWhiteSpace(r.BodyPartCode))
                    .GroupBy(r => r.GradeRuleSetId + "\0" + r.BodyPartCode, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.GradeRuleId).First())
                    .ToList();
                executeResult.TchpGradeRuleSetRowsImported = UpsertTchpGradeRuleSets(tenantConn, sets);
                executeResult.TchpGradeRuleRowsImported = UpsertTchpGradeRules(tenantConn, rules);
                executeResult.SpecBodyPartGradingRowsImported = executeResult.TchpGradeRuleRowsImported;
                executeResult.Messages.Add(
                    "Upserted " + executeResult.TchpGradeRuleSetRowsImported + " GradeRuleSet and "
                    + executeResult.TchpGradeRuleRowsImported + " GradeRule row(s) (G-B).");
            }
        }

        private static List<TchpGradeRuleSetRow> ReadGradeRuleSetsFromPlm(string plmConnectionString)
        {
            var rows = new List<TchpGradeRuleSetRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                string bodyType = ResolvePlmTableName(conn, PlmBodyTypeSourceTable, "pdmv2kBodyType", "PdmV2kBodyType");
                string detail = ResolvePlmTableName(conn, PomBodyTypeDetailSourceTable, "pdmV2kBodyTypeDetail", "PdmV2kBodyTypeDetail");
                string grading = ResolvePlmTableName(conn, PomSpecGradingSourceTable, "pdmV2kSpecBodyPartGrading", "PdmV2kSpecBodyPartGrading");
                string nameCol = ColumnExists(conn, bodyType, "BodyTypeName") ? "BodyTypeName" : "Name";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT DISTINCT
    bt.BodyTypeID,
    bt.[" + nameCol + @"]
FROM dbo.[" + bodyType + @"] bt
INNER JOIN dbo.[" + detail + @"] d ON d.BodyTypeID = bt.BodyTypeID
INNER JOIN dbo.[" + grading + @"] g ON g.BodyTypeDetailID = d.BodyTypeDetailID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.IsDBNull(1) ? ("Template_" + id) : reader.GetString(1);
                            rows.Add(new TchpGradeRuleSetRow
                            {
                                GradeRuleSetId = id,
                                GradeRuleSetName = Truncate("Template: " + name.Trim(), 100),
                                Description = Truncate(
                                    "Imported from pdmV2kSpecBodyPartGrading for BodyTypeID=" + id, 800)
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static List<TchpGradeRuleRow> ReadGradeRulesFromPlm(string plmConnectionString)
        {
            var raw = new List<TchpGradeRuleRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                string bodyPart = ResolvePlmTableName(conn, PlmBodyPartSourceTable, "PdmV2kBodyPart", "pdmV2kBodyPart");
                string detail = ResolvePlmTableName(conn, PomBodyTypeDetailSourceTable, "pdmV2kBodyTypeDetail", "PdmV2kBodyTypeDetail");
                string grading = ResolvePlmTableName(conn, PomSpecGradingSourceTable, "pdmV2kSpecBodyPartGrading", "PdmV2kSpecBodyPartGrading");
                string pkCol = ColumnExists(conn, grading, "BodyPartGradingID")
                    ? "BodyPartGradingID"
                    : (ColumnExists(conn, grading, "BodyPartGradingId") ? "BodyPartGradingId" : "SpecBodyPartGradingID");
                string sortG = ColumnExists(conn, grading, "Sort") ? "g.Sort" : "NULL";
                string sortD = ColumnExists(conn, detail, "Sort") ? "d.Sort" : "NULL";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    g.[" + pkCol + @"],
    d.BodyTypeID,
    d.BodyPartID,
    bp.Code,
    ISNULL(g.GradingPlusValue, 0),
    ISNULL(g.GradingMinuValue, 0),
    COALESCE(" + sortG + ", " + sortD + @", 0)
FROM dbo.[" + grading + @"] g
INNER JOIN dbo.[" + detail + @"] d ON d.BodyTypeDetailID = g.BodyTypeDetailID
INNER JOIN dbo.[" + bodyPart + @"] bp ON bp.BodyPartID = d.BodyPartID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal plus = Convert.ToDecimal(reader.GetValue(4));
                            decimal minu = Convert.ToDecimal(reader.GetValue(5));
                            string code = reader.IsDBNull(3) ? null : reader.GetString(3);
                            raw.Add(new TchpGradeRuleRow
                            {
                                GradeRuleId = Convert.ToInt32(reader.GetValue(0)),
                                GradeRuleSetId = Convert.ToInt32(reader.GetValue(1)),
                                BodyPartId = Convert.ToInt32(reader.GetValue(2)),
                                BodyPartCode = string.IsNullOrWhiteSpace(code) ? null : Truncate(code.Trim(), 50),
                                GradingPlusValue = plus,
                                GradingMinuValue = minu,
                                IsSymmetric = plus == minu,
                                Sort = Convert.ToInt16(Math.Min(Convert.ToInt32(reader.GetValue(6)), short.MaxValue))
                            });
                        }
                    }
                }
            }

            return raw;
        }

        private static void ApplyTchpBodyPartCodes(SqlConnection tenantConn, List<TchpGradeRuleRow> rows)
        {
            if (rows.Count == 0 || !TemplateTableExists(tenantConn, null, TchpBodyPartTableName))
                return;

            var codes = new Dictionary<int, string>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT BodyPartId, Code FROM dbo.TchpBodyPart";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        codes[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
                }
            }

            foreach (var row in rows)
            {
                if (codes.TryGetValue(row.BodyPartId, out var remapped) && !string.IsNullOrWhiteSpace(remapped))
                    row.BodyPartCode = remapped;
            }
        }

        private static int UpsertTchpGradeRuleSets(SqlConnection tenantConn, List<TchpGradeRuleSetRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpGradeRuleSetImport", @"
CREATE TABLE #TchpGradeRuleSetImport (
    GradeRuleSetId INT NOT NULL PRIMARY KEY,
    GradeRuleSetName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(800) NULL
);");
            BulkInsert(tenantConn, "#TchpGradeRuleSetImport",
                rows.Select(r => new object[] { r.GradeRuleSetId, r.GradeRuleSetName, (object)r.Description ?? DBNull.Value }),
                new[] { "GradeRuleSetId", "GradeRuleSetName", "Description" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpGradeRuleSet ON;
MERGE dbo.TchpGradeRuleSet AS t
USING #TchpGradeRuleSetImport AS s ON t.GradeRuleSetId = s.GradeRuleSetId
WHEN MATCHED AND ISNULL(t.Standard, N'') <> N'ASTM' THEN UPDATE SET
    GradeRuleSetName = s.GradeRuleSetName,
    Description = s.Description,
    Standard = N'CUSTOM',
    IsActive = 1,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (GradeRuleSetId, GradeRuleSetName, Description, Standard, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.GradeRuleSetId, s.GradeRuleSetName, s.Description, N'CUSTOM', 1, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpGradeRuleSet OFF;
SELECT COUNT(*) FROM #TchpGradeRuleSetImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int UpsertTchpGradeRules(SqlConnection tenantConn, List<TchpGradeRuleRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpGradeRuleImport", @"
CREATE TABLE #TchpGradeRuleImport (
    GradeRuleId INT NOT NULL PRIMARY KEY,
    GradeRuleSetId INT NOT NULL,
    BodyPartCode NVARCHAR(50) NOT NULL,
    GradingPlusValue DECIMAL(10,3) NOT NULL,
    GradingMinuValue DECIMAL(10,3) NOT NULL,
    IsSymmetric BIT NOT NULL,
    Sort SMALLINT NULL
);");
            BulkInsert(tenantConn, "#TchpGradeRuleImport",
                rows.Select(r => new object[]
                {
                    r.GradeRuleId, r.GradeRuleSetId, r.BodyPartCode,
                    r.GradingPlusValue, r.GradingMinuValue, r.IsSymmetric, r.Sort
                }),
                new[] { "GradeRuleId", "GradeRuleSetId", "BodyPartCode", "GradingPlusValue", "GradingMinuValue", "IsSymmetric", "Sort" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpGradeRule ON;
MERGE dbo.TchpGradeRule AS t
USING #TchpGradeRuleImport AS s ON t.GradeRuleId = s.GradeRuleId
WHEN MATCHED THEN UPDATE SET
    GradeRuleSetId = s.GradeRuleSetId,
    BodyPartCode = s.BodyPartCode,
    GradingPlusValue = s.GradingPlusValue,
    GradingMinuValue = s.GradingMinuValue,
    IsSymmetric = s.IsSymmetric,
    Sort = s.Sort,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (GradeRuleId, GradeRuleSetId, BodyPartCode, GradingPlusValue, GradingMinuValue, IsSymmetric, Sort, AppCreatedDate, AppModifiedDate)
    VALUES (s.GradeRuleId, s.GradeRuleSetId, s.BodyPartCode, s.GradingPlusValue, s.GradingMinuValue, s.IsSymmetric, s.Sort, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpGradeRule OFF;
SELECT COUNT(*) FROM #TchpGradeRuleImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void EnsureTchpGradeRuleSchema(SqlConnection tenantConn)
        {
            ExecuteEnsureDdl(tenantConn, @"
IF OBJECT_ID(N'dbo.TchpGradeRuleSet', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpGradeRuleSet] (
        [GradeRuleSetId]        INT             IDENTITY(1,1)   NOT NULL,
        [GradeRuleSetName]      NVARCHAR(100)   NOT NULL,
        [Description]           NVARCHAR(800)   NULL,
        [Standard]              NVARCHAR(20)    NOT NULL CONSTRAINT DF_TchpGradeRuleSet_Standard DEFAULT ('CUSTOM'),
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpGradeRuleSet_IsActive DEFAULT (1),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpGradeRuleSet] PRIMARY KEY CLUSTERED ([GradeRuleSetId] ASC)
    );
END");
            ExecuteEnsureDdl(tenantConn, @"
IF OBJECT_ID(N'dbo.TchpGradeRule', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpGradeRule] (
        [GradeRuleId]           INT             IDENTITY(1,1)   NOT NULL,
        [GradeRuleSetId]        INT             NOT NULL,
        [BodyPartCode]          NVARCHAR(50)    NOT NULL,
        [GradingPlusValue]      DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpGradeRule_PlusValue DEFAULT (0),
        [GradingMinuValue]      DECIMAL(10,3)   NOT NULL CONSTRAINT DF_TchpGradeRule_MinuValue DEFAULT (0),
        [IsSymmetric]           BIT             NOT NULL CONSTRAINT DF_TchpGradeRule_IsSymmetric DEFAULT (1),
        [Sort]                  SMALLINT        NULL,
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpGradeRule] PRIMARY KEY CLUSTERED ([GradeRuleId] ASC),
        CONSTRAINT [FK_TchpGradeRule_TchpGradeRuleSet]
            FOREIGN KEY ([GradeRuleSetId]) REFERENCES [dbo].[TchpGradeRuleSet] ([GradeRuleSetId])
    );
END");
        }
    }
}
