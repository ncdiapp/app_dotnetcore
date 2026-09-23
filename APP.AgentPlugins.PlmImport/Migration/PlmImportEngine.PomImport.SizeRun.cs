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
        private const string TchpSizeRunTableName = "TchpSizeRun";
        private const string TchpSizeRunSizeTableName = "TchpSizeRunSize";

        private sealed class SizeRunSourceResolution
        {
            public int? DataSourceFrom { get; set; }
            public string DataSourceFromName { get; set; }
            public string SizeRunTable { get; set; } = "tblSizeRun";
            public string SizeRunDetailTable { get; set; } = "tblSizeRunRotate";
            public string ConnectionString { get; set; }
            public string Error { get; set; }
        }

        private sealed class TchpSizeRunRow
        {
            public int SizeRunId { get; set; }
            public string SizeRunCode { get; set; }
            public string SizeRunName { get; set; }
        }

        private sealed class TchpSizeRunSizeRow
        {
            public int SizeRunSizeId { get; set; }
            public int SizeRunId { get; set; }
            public string SizeLabel { get; set; }
            public int SizeOrder { get; set; }
        }

        private static SizeRunSourceResolution ResolveSizeRunSource(
            string plmConnectionString,
            string erpConnectionString)
        {
            var resolved = new SizeRunSourceResolution();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    LEFT(LTRIM(RTRIM(e.EntityCode)), 100),
    e.DataSourceFrom,
    LEFT(LTRIM(RTRIM(e.SysTableName)), 100)
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND ISNULL(e.IsRelationEntity, 0) = 0
  AND e.EntityCode IN (N'SizeRun', N'SizeRunDetail')
ORDER BY e.EntityCode, e.EntityID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string code = reader.IsDBNull(0) ? null : reader.GetString(0);
                            int? dsf = reader.IsDBNull(1) ? (int?)null : Convert.ToInt32(reader.GetValue(1));
                            string table = reader.IsDBNull(2) ? null : reader.GetString(2);
                            if (string.Equals(code, "SizeRun", StringComparison.OrdinalIgnoreCase))
                            {
                                resolved.DataSourceFrom = dsf;
                                if (!string.IsNullOrWhiteSpace(table))
                                    resolved.SizeRunTable = table.Trim();
                            }
                            else if (string.Equals(code, "SizeRunDetail", StringComparison.OrdinalIgnoreCase)
                                     && !string.IsNullOrWhiteSpace(table))
                            {
                                resolved.SizeRunDetailTable = table.Trim();
                            }
                        }
                    }
                }
            }

            resolved.DataSourceFromName = resolved.DataSourceFrom.HasValue
                ? GetPlmDataSourceFromName(resolved.DataSourceFrom.Value)
                : null;

            if (resolved.DataSourceFrom == 2)
            {
                if (string.IsNullOrWhiteSpace(erpConnectionString))
                {
                    resolved.Error = "SizeRun DataSourceFrom=2 (ERP) but no ERP DataSourceRegisterId is set on the session.";
                    return resolved;
                }
                resolved.ConnectionString = erpConnectionString;
                return resolved;
            }

            resolved.ConnectionString = plmConnectionString;
            if (!resolved.DataSourceFrom.HasValue)
                resolved.Error = "pdmEntity SizeRun / SizeRunDetail was not found; defaulting to PLM tblSizeRun.";
            return resolved;
        }

        private static void FillSizeRunPreview(
            PlmPomImportPreviewDto preview,
            string plmConnectionString,
            string tenantConnectionString,
            string erpConnectionString)
        {
            var source = ResolveSizeRunSource(plmConnectionString, erpConnectionString);
            preview.SizeRunDataSourceFrom = source.DataSourceFrom;
            preview.SizeRunDataSourceFromName = source.DataSourceFromName;
            preview.SizeRunTableName = source.SizeRunTable;
            preview.SizeRunDetailTableName = source.SizeRunDetailTable;
            if (!string.IsNullOrWhiteSpace(source.Error))
                preview.Warnings.Add(source.Error);

            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                EnsureTchpSizeRunSchema(tenantConn);
                preview.TchpSizeRunRowCount = TemplateTableExists(tenantConn, null, TchpSizeRunTableName)
                    ? CountTableRows(tenantConn, TchpSizeRunTableName) : 0;
                preview.TchpSizeRunSizeRowCount = TemplateTableExists(tenantConn, null, TchpSizeRunSizeTableName)
                    ? CountTableRows(tenantConn, TchpSizeRunSizeTableName) : 0;
                preview.SizeRunEntityExists = EntityCodeExists(tenantConn, "SizeRun");
                preview.SizeRunDetailEntityExists = EntityCodeExists(tenantConn, "SizeRunDetail");
            }

            if (string.IsNullOrWhiteSpace(source.ConnectionString))
                return;

            try
            {
                using (var src = new SqlConnection(source.ConnectionString))
                {
                    src.Open();
                    if (!TemplateTableExists(src, null, source.SizeRunTable))
                    {
                        preview.Warnings.Add("SizeRun source table " + source.SizeRunTable + " was not found.");
                        return;
                    }

                    bool hasVisible = ColumnExists(src, source.SizeRunTable, "isVisibleInPLM");
                    preview.SizeRunSourceRowCount = CountTableRows(src, source.SizeRunTable);
                    preview.SizeRunVisibleRowCount = hasVisible
                        ? CountWhere(src, source.SizeRunTable, "ISNULL(isVisibleInPLM, 1) = 1")
                        : preview.SizeRunSourceRowCount;

                    if (TemplateTableExists(src, null, source.SizeRunDetailTable))
                    {
                        preview.SizeRunSizeSourceRowCount = hasVisible
                            ? CountJoinedVisibleSizes(src, source.SizeRunTable, source.SizeRunDetailTable)
                            : CountTableRows(src, source.SizeRunDetailTable);
                    }
                }
            }
            catch (Exception ex)
            {
                preview.Warnings.Add("SizeRun source probe failed: " + ex.Message);
            }

            if (!preview.SizeRunEntityExists || !preview.SizeRunDetailEntityExists)
                preview.Warnings.Add("AppEntityInfo SizeRun / SizeRunDetail missing — remount (D6) will be skipped until Entity import creates them.");

            preview.PlannedActions.Add(
                "Upsert TchpSizeRun / TchpSizeRunSize from " + (source.DataSourceFromName ?? "PLM")
                + " " + source.SizeRunTable + " (visible filter when isVisibleInPLM exists)");
            preview.PlannedActions.Add("Remount AppEntity SizeRun / SizeRunDetail onto Tchp* (do not create TchpSizeRun entity codes)");
        }

        private static void ImportTchpSizeRunMasterData(
            string plmConnectionString,
            string tenantConnectionString,
            string erpConnectionString,
            int tenantDataSourceId,
            PlmPomImportExecuteResultDto executeResult)
        {
            var source = ResolveSizeRunSource(plmConnectionString, erpConnectionString);
            if (!string.IsNullOrWhiteSpace(source.Error) && string.IsNullOrWhiteSpace(source.ConnectionString))
            {
                executeResult.Messages.Add(source.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(source.ConnectionString))
            {
                executeResult.Messages.Add("SizeRun source connection is not available.");
                return;
            }

            var runs = ReadSizeRunsForTchp(source);
            var sizes = ReadSizeRunSizesForTchp(source);
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                EnsureTchpSizeRunSchema(tenantConn);
                executeResult.TchpSizeRunRowsImported = UpsertTchpSizeRuns(tenantConn, runs);
                executeResult.TchpSizeRunSizeRowsImported = UpsertTchpSizeRunSizes(tenantConn, sizes);
                executeResult.Messages.Add(
                    "Upserted " + executeResult.TchpSizeRunRowsImported + " TchpSizeRun and "
                    + executeResult.TchpSizeRunSizeRowsImported + " TchpSizeRunSize row(s) from "
                    + (source.DataSourceFromName ?? "PLM") + ".");

                executeResult.SizeRunEntitiesRemounted = RemountSizeRunEntities(tenantConn, tenantDataSourceId, executeResult);
            }
        }

        private static List<TchpSizeRunRow> ReadSizeRunsForTchp(SizeRunSourceResolution source)
        {
            var rows = new List<TchpSizeRunRow>();
            using (var conn = new SqlConnection(source.ConnectionString))
            {
                conn.Open();
                string table = ResolvePlmTableName(conn, source.SizeRunTable, "tblSizeRun");
                bool hasVisible = ColumnExists(conn, table, "isVisibleInPLM");
                string nameCol = ColumnExists(conn, table, "SizeRunName")
                    ? "SizeRunName"
                    : (ColumnExists(conn, table, "Description") ? "Description" : "SizeRunCode");
                string where = hasVisible ? " WHERE ISNULL(isVisibleInPLM, 1) = 1" : "";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT SizeRunId, SizeRunCode, [" + nameCol + "] FROM dbo.[" + table + "]" + where;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string code = reader.IsDBNull(1) ? ("SR_" + reader.GetInt32(0)) : reader.GetString(1);
                            string name = reader.IsDBNull(2) ? code : reader.GetString(2);
                            rows.Add(new TchpSizeRunRow
                            {
                                SizeRunId = reader.GetInt32(0),
                                SizeRunCode = Truncate(code.Trim(), 50),
                                SizeRunName = Truncate((string.IsNullOrWhiteSpace(name) ? code : name).Trim(), 100)
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static List<TchpSizeRunSizeRow> ReadSizeRunSizesForTchp(SizeRunSourceResolution source)
        {
            var rows = new List<TchpSizeRunSizeRow>();
            using (var conn = new SqlConnection(source.ConnectionString))
            {
                conn.Open();
                string runTable = ResolvePlmTableName(conn, source.SizeRunTable, "tblSizeRun");
                string sizeTable = ResolvePlmTableName(conn, source.SizeRunDetailTable, "tblSizeRunRotate");
                bool hasVisible = ColumnExists(conn, runTable, "isVisibleInPLM");
                string orderCol = ColumnExists(conn, sizeTable, "SizeOrder")
                    ? "SizeOrder"
                    : (ColumnExists(conn, sizeTable, "Sort") ? "Sort" : null);
                string nameCol = ColumnExists(conn, sizeTable, "SizeName") ? "SizeName" : "SizeLabel";
                string pkCol = ColumnExists(conn, sizeTable, "SizeRunRotateID")
                    ? "SizeRunRotateID"
                    : (ColumnExists(conn, sizeTable, "SizeRunSizeId") ? "SizeRunSizeId" : "SizeRunRotateId");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT r.[" + pkCol + @"], r.SizeRunId, r.[" + nameCol + @"], "
                        + (orderCol != null ? "ISNULL(r.[" + orderCol + "], 0)" : "0") + @"
FROM dbo.[" + sizeTable + @"] r
INNER JOIN dbo.[" + runTable + @"] sr ON sr.SizeRunId = r.SizeRunId"
                        + (hasVisible ? " AND ISNULL(sr.isVisibleInPLM, 1) = 1" : "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = Convert.ToInt32(reader.GetValue(0));
                            string label = reader.IsDBNull(2) ? ("S" + id) : reader.GetString(2);
                            if (string.IsNullOrWhiteSpace(label))
                                label = "S" + id;
                            rows.Add(new TchpSizeRunSizeRow
                            {
                                SizeRunSizeId = id,
                                SizeRunId = Convert.ToInt32(reader.GetValue(1)),
                                SizeLabel = Truncate(label.Trim(), 20),
                                SizeOrder = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3))
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static int UpsertTchpSizeRuns(SqlConnection tenantConn, List<TchpSizeRunRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpSizeRunImport", @"
CREATE TABLE #TchpSizeRunImport (
    SizeRunId INT NOT NULL PRIMARY KEY,
    SizeRunCode NVARCHAR(50) NOT NULL,
    SizeRunName NVARCHAR(100) NOT NULL
);");
            BulkInsert(tenantConn, "#TchpSizeRunImport",
                rows.Select(r => new object[] { r.SizeRunId, r.SizeRunCode, r.SizeRunName }),
                new[] { "SizeRunId", "SizeRunCode", "SizeRunName" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpSizeRun ON;
MERGE dbo.TchpSizeRun AS t
USING #TchpSizeRunImport AS s ON t.SizeRunId = s.SizeRunId
WHEN MATCHED THEN UPDATE SET
    SizeRunCode = s.SizeRunCode,
    SizeRunName = s.SizeRunName,
    IsActive = 1,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (SizeRunId, SizeRunCode, SizeRunName, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.SizeRunId, s.SizeRunCode, s.SizeRunName, 1, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpSizeRun OFF;
SELECT COUNT(*) FROM #TchpSizeRunImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int UpsertTchpSizeRunSizes(SqlConnection tenantConn, List<TchpSizeRunSizeRow> rows)
        {
            if (rows.Count == 0)
                return 0;

            EnsureTempTable(tenantConn, "#TchpSizeRunSizeImport", @"
CREATE TABLE #TchpSizeRunSizeImport (
    SizeRunSizeId INT NOT NULL PRIMARY KEY,
    SizeRunId INT NOT NULL,
    SizeLabel NVARCHAR(20) NOT NULL,
    SizeOrder INT NOT NULL
);");
            BulkInsert(tenantConn, "#TchpSizeRunSizeImport",
                rows.Select(r => new object[] { r.SizeRunSizeId, r.SizeRunId, r.SizeLabel, r.SizeOrder }),
                new[] { "SizeRunSizeId", "SizeRunId", "SizeLabel", "SizeOrder" });

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SET IDENTITY_INSERT dbo.TchpSizeRunSize ON;
MERGE dbo.TchpSizeRunSize AS t
USING #TchpSizeRunSizeImport AS s ON t.SizeRunSizeId = s.SizeRunSizeId
WHEN MATCHED THEN UPDATE SET
    SizeRunId = s.SizeRunId,
    SizeLabel = s.SizeLabel,
    SizeOrder = s.SizeOrder,
    IsActive = 1,
    AppModifiedDate = GETDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (SizeRunSizeId, SizeRunId, SizeLabel, SizeOrder, IsActive, AppCreatedDate, AppModifiedDate)
    VALUES (s.SizeRunSizeId, s.SizeRunId, s.SizeLabel, s.SizeOrder, 1, GETDATE(), GETDATE());
SET IDENTITY_INSERT dbo.TchpSizeRunSize OFF;
SELECT COUNT(*) FROM #TchpSizeRunSizeImport;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static bool RemountSizeRunEntities(
            SqlConnection tenantConn,
            int tenantDataSourceId,
            PlmPomImportExecuteResultDto executeResult)
        {
            int? sizeRunId = GetEntityInfoIdByCode(tenantConn, "SizeRun");
            int? sizeRunDetailId = GetEntityInfoIdByCode(tenantConn, "SizeRunDetail");
            if (!sizeRunId.HasValue || !sizeRunDetailId.HasValue)
            {
                executeResult.Messages.Add("Skipped SizeRun entity remount — SizeRun / SizeRunDetail AppEntityInfo rows are missing.");
                return false;
            }

            int? tchpRunId = GetEntityInfoIdByCode(tenantConn, "TchpSizeRun");
            int? tchpSizeId = GetEntityInfoIdByCode(tenantConn, "TchpSizeRunSize");
            if (tchpRunId.HasValue)
            {
                using (var cmd = tenantConn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE dbo.AppTransactionField SET EntityId = @ToId WHERE EntityId = @FromId";
                    cmd.Parameters.AddWithValue("@ToId", sizeRunId.Value);
                    cmd.Parameters.AddWithValue("@FromId", tchpRunId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            if (tchpSizeId.HasValue)
            {
                using (var cmd = tenantConn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE dbo.AppTransactionField SET EntityId = @ToId WHERE EntityId = @FromId";
                    cmd.Parameters.AddWithValue("@ToId", sizeRunDetailId.Value);
                    cmd.Parameters.AddWithValue("@FromId", tchpSizeId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppEntityInfo SET
    TableName = N'TchpSizeRun',
    IdentityField = N'SizeRunId',
    DisplayFiled1 = N'SizeRunCode',
    DisplayFiled2 = N'SizeRunName',
    DisplayFiled3 = NULL,
    DataSourceFrom = @Ds,
    SchemaOwner = N'dbo',
    QueryText = NULL,
    AppModifiedDate = GETDATE()
WHERE EntityInfoID = @Id";
                cmd.Parameters.AddWithValue("@Ds", tenantDataSourceId);
                cmd.Parameters.AddWithValue("@Id", sizeRunId.Value);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppEntityInfo SET
    TableName = N'TchpSizeRunSize',
    IdentityField = N'SizeRunSizeId',
    DisplayFiled1 = N'SizeLabel',
    DisplayFiled2 = NULL,
    DisplayFiled3 = NULL,
    DataSourceFrom = @Ds,
    SchemaOwner = N'dbo',
    QueryText = NULL,
    AppModifiedDate = GETDATE()
WHERE EntityInfoID = @Id";
                cmd.Parameters.AddWithValue("@Ds", tenantDataSourceId);
                cmd.Parameters.AddWithValue("@Id", sizeRunDetailId.Value);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.AppEntityInfo WHERE EntityCode IN (N'TchpSizeRun', N'TchpSizeRunSize')";
                int deleted = cmd.ExecuteNonQuery();
                executeResult.Messages.Add(
                    "Remounted SizeRun / SizeRunDetail onto Tchp*. Deleted " + deleted + " TchpSizeRun* entity row(s).");
            }

            return true;
        }

        private static void EnsureTchpSizeRunSchema(SqlConnection tenantConn)
        {
            ExecuteEnsureDdl(tenantConn, @"
IF OBJECT_ID(N'dbo.TchpSizeRun', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpSizeRun] (
        [SizeRunId]             INT             IDENTITY(1,1)   NOT NULL,
        [SizeRunCode]           NVARCHAR(50)    NOT NULL,
        [SizeRunName]           NVARCHAR(100)   NOT NULL,
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpSizeRun_IsActive DEFAULT (1),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpSizeRun] PRIMARY KEY CLUSTERED ([SizeRunId] ASC),
        CONSTRAINT [UQ_TchpSizeRun_Code] UNIQUE ([SizeRunCode])
    );
END");
            ExecuteEnsureDdl(tenantConn, @"
IF OBJECT_ID(N'dbo.TchpSizeRunSize', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TchpSizeRunSize] (
        [SizeRunSizeId]         INT             IDENTITY(1,1)   NOT NULL,
        [SizeRunId]             INT             NOT NULL,
        [SizeLabel]             NVARCHAR(20)    NOT NULL,
        [SizeOrder]             INT             NOT NULL CONSTRAINT DF_TchpSizeRunSize_Order DEFAULT (0),
        [IsActive]              BIT             NOT NULL CONSTRAINT DF_TchpSizeRunSize_IsActive DEFAULT (1),
        [SystemTimeStamp]       ROWVERSION      NULL,
        [AppCreatedById]        INT             NULL,
        [AppCreatedDate]        DATETIME        NULL,
        [AppModifiedDate]       DATETIME        NULL,
        [AppModifiedById]       INT             NULL,
        [AppCreatedByCompanyId] INT             NULL,
        CONSTRAINT [PK_TchpSizeRunSize] PRIMARY KEY CLUSTERED ([SizeRunSizeId] ASC),
        CONSTRAINT [FK_TchpSizeRunSize_TchpSizeRun]
            FOREIGN KEY ([SizeRunId]) REFERENCES [dbo].[TchpSizeRun] ([SizeRunId]),
        CONSTRAINT [UQ_TchpSizeRunSize_RunLabel] UNIQUE ([SizeRunId], [SizeLabel])
    );
END");
        }

        private static bool EntityCodeExists(SqlConnection conn, string entityCode)
        {
            return GetEntityInfoIdByCode(conn, entityCode).HasValue;
        }

        private static int? GetEntityInfoIdByCode(SqlConnection conn, string entityCode)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 EntityInfoID FROM dbo.AppEntityInfo
WHERE EntityCode = @Code ORDER BY EntityInfoID";
                cmd.Parameters.AddWithValue("@Code", entityCode);
                var value = cmd.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static int CountWhere(SqlConnection conn, string tableName, string whereSql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.[" + tableName + "] WHERE " + whereSql;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int CountJoinedVisibleSizes(SqlConnection conn, string runTable, string sizeTable)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COUNT(*)
FROM dbo.[" + sizeTable + @"] r
INNER JOIN dbo.[" + runTable + @"] sr ON sr.SizeRunId = r.SizeRunId
WHERE ISNULL(sr.isVisibleInPLM, 1) = 1";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
