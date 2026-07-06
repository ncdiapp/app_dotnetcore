using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const string PlmFolderPlacementActionPreview = "PlmFolderPlacementPreview";
        private const string PlmFolderPlacementActionExecute = "PlmFolderPlacement";

        public static OperationCallResult<PlmFolderPlacementPreviewDto> PreviewPlmFolderPlacement(int? sessionId)
        {
            var result = new OperationCallResult<PlmFolderPlacementPreviewDto>
            {
                Object = new PlmFolderPlacementPreviewDto()
            };

            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null || string.IsNullOrWhiteSpace(session.PlmConnectionString))
                    throw new InvalidOperationException("PLM connection is not available on this session.");

                var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(GetTenantDataSourceId());
                if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
                    throw new InvalidOperationException("Tenant database connection is not available.");

                string tenantConn = AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);
                var prefixes = ResolveImportPrefixes(session.StepStateJson);
                string rootTable = prefixes.TablePrefix + "ReferenceBasicInfo";

                result.Object = BuildPlmFolderPlacementPreview(
                    sessionId.Value,
                    session.PlmConnectionString.Trim(),
                    tenantConn,
                    rootTable);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_FolderPlacement_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else
                {
                    WriteImportLog(fixture, sessionId.Value, null, StepFolderImport, PlmFolderPlacementActionPreview, "Success",
                        null, null, null, null, "PLM folder placement preview completed.");
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_FolderPlacement_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecutePlmFolderPlacement(int? sessionId)
        {
            return StartPlmFolderPlacementJob(sessionId);
        }

        public static OperationCallResult<PlmImportJobDto> StartPlmFolderPlacementJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypePlmFolderPlacement,
                    "Queued PLM folder placement for products, colors, and images.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepFolderImport, PlmFolderPlacementActionExecute, "Running",
                    null, null, null, null, "PLM folder placement job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunPlmFolderPlacementJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_FolderPlacement_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void RunPlmFolderPlacementJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var session = LoadSessionById(fixture, context.SessionId, includeConnection: false);
            if (session == null)
                throw new InvalidOperationException("Import session not found.");

            var prefixes = ResolveImportPrefixes(session.StepStateJson);
            string rootTable = prefixes.TablePrefix + "ReferenceBasicInfo";

            var placementResult = ApplyPlmFolderPlacement(
                context.SessionId,
                context.PlmConnectionString,
                context.TenantConnectionString,
                rootTable,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Folder placement cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(placementResult);
            if (!placementResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "Folder placement failed.",
                    resultJson: resultJson, errorMessage: placementResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepFolderImport,
                    PlmFolderPlacementActionExecute, "Failed", null, null, null, null, placementResult.ErrorMessage);
                return;
            }

            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "Folder placement completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepFolderImport,
                PlmFolderPlacementActionExecute, "Success", null, null,
                placementResult.ProductsUpdated + placementResult.ColorDetailsUpdated + placementResult.AppFilesUpdated,
                null,
                $"Products {placementResult.ProductsUpdated}, color links {placementResult.ColorDetailsUpdated}, images {placementResult.AppFilesUpdated}.");
        }

        private static PlmFolderPlacementPreviewDto BuildPlmFolderPlacementPreview(
            int sessionId,
            string plmConnectionString,
            string tenantConnectionString,
            string rootTable)
        {
            var preview = new PlmFolderPlacementPreviewDto { IsSuccess = true };
            EnsurePlmFolderImportTables(tenantConnectionString);

            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();
                EnsurePlmSeFolderSchema(plmConn);

                preview.ProductReadyCount = CountProductPlacementReady(plmConn, tenantConn, sessionId, rootTable, out int productMissingMap);
                preview.ProductMissingFolderMapCount = productMissingMap;

                preview.ColorDetailReadyCount = CountColorDetailPlacementReady(tenantConn, sessionId, out int colorMissingMap);
                preview.ColorDetailMissingFolderMapCount = colorMissingMap;

                preview.ImageReadyCount = CountImagePlacementReady(plmConn, tenantConn, sessionId, out int imageMissingMap);
                preview.ImageMissingFolderMapCount = imageMissingMap;

                if (preview.ProductMissingFolderMapCount > 0)
                    preview.Warnings.Add($"{preview.ProductMissingFolderMapCount} product(s) have PLM folders without AppPlmFolderMap entries.");
                if (preview.ColorDetailMissingFolderMapCount > 0)
                    preview.Warnings.Add($"{preview.ColorDetailMissingFolderMapCount} color-folder link(s) are missing folder mappings.");
                if (preview.ImageMissingFolderMapCount > 0)
                    preview.Warnings.Add($"{preview.ImageMissingFolderMapCount} sketch(s) have PLM folders without AppPlmFolderMap entries.");
            }

            return preview;
        }

        private static PlmFolderPlacementResultDto ApplyPlmFolderPlacement(
            int sessionId,
            string plmConnectionString,
            string tenantConnectionString,
            string rootTable,
            Action<int, string> progressCallback)
        {
            var result = new PlmFolderPlacementResultDto { IsSuccess = true };

            progressCallback?.Invoke(10, "Placing products into folders…");
            result.ProductsUpdated = PlaceProductFolders(plmConnectionString, tenantConnectionString, sessionId, rootTable, result);

            progressCallback?.Invoke(45, "Placing color-folder links…");
            result.ColorDetailsUpdated = PlaceColorDetailFolders(tenantConnectionString, sessionId, result);

            progressCallback?.Invoke(75, "Placing images into folders…");
            result.AppFilesUpdated = PlaceImageFolders(plmConnectionString, tenantConnectionString, sessionId, result);

            if (result.Errors.Count > 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = string.Join("; ", result.Errors.Take(5));
            }

            progressCallback?.Invoke(100, "Folder placement finished.");
            return result;
        }

        private static int CountProductPlacementReady(
            SqlConnection plmConn,
            SqlConnection tenantConn,
            int sessionId,
            string rootTable,
            out int missingMapCount)
        {
            missingMapCount = 0;
            if (!TableExists(tenantConn, rootTable))
                return 0;

            int ready = 0;
            var productFolderPairs = new List<(int ReferenceId, int PlmFolderId)>();

            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT p.ProductReferenceID, p.FolderID
FROM dbo.pdmProduct p
WHERE p.FolderID IS NOT NULL;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        productFolderPairs.Add((reader.GetInt32(0), reader.GetInt32(1)));
                }
            }

            var referenceIds = LoadReferenceIds(tenantConn, rootTable);
            var maps = LoadFolderMapByPlmId(tenantConn, sessionId, 1);

            foreach (var pair in productFolderPairs)
            {
                if (!referenceIds.Contains(pair.ReferenceId))
                    continue;
                if (maps.ContainsKey(pair.PlmFolderId))
                    ready++;
                else
                    missingMapCount++;
            }

            return ready;
        }

        private static int CountColorDetailPlacementReady(SqlConnection tenantConn, int sessionId, out int missingMapCount)
        {
            missingMapCount = 0;
            if (!TableExists(tenantConn, ColorGroupDetailTable))
                return 0;

            int ready = 0;
            var maps = LoadFolderMapByPlmId(tenantConn, sessionId, 2);

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = $@"
SELECT PlmFolderId
FROM dbo.{ColorGroupDetailTable}
WHERE AppFolderId IS NULL;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int plmFolderId = reader.GetInt32(0);
                        if (maps.ContainsKey(plmFolderId))
                            ready++;
                        else
                            missingMapCount++;
                    }
                }
            }

            return ready;
        }

        private static int CountImagePlacementReady(
            SqlConnection plmConn,
            SqlConnection tenantConn,
            int sessionId,
            out int missingMapCount)
        {
            missingMapCount = 0;
            int ready = 0;
            var maps = LoadFolderMapByPlmId(tenantConn, sessionId, 7);
            var appFileIds = LoadAppFileIds(tenantConn);

            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT s.SketchID, s.FolderID
FROM dbo.tblSketch s
WHERE s.FolderID IS NOT NULL;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int sketchId = reader.GetInt32(0);
                        int plmFolderId = reader.GetInt32(1);
                        if (!appFileIds.Contains(sketchId))
                            continue;
                        if (maps.ContainsKey(plmFolderId))
                            ready++;
                        else
                            missingMapCount++;
                    }
                }
            }

            return ready;
        }

        private static int PlaceProductFolders(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId,
            string rootTable,
            PlmFolderPlacementResultDto result)
        {
            if (!TableExistsOnConnection(tenantConnectionString, rootTable))
                return 0;

            int updated = 0;
            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();

                var maps = LoadFolderMapByPlmId(tenantConn, sessionId, 1);
                var referenceIds = LoadReferenceIds(tenantConn, rootTable);

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT p.ProductReferenceID, p.FolderID
FROM dbo.pdmProduct p
WHERE p.FolderID IS NOT NULL;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int referenceId = reader.GetInt32(0);
                            int plmFolderId = reader.GetInt32(1);
                            if (!referenceIds.Contains(referenceId))
                                continue;
                            if (!maps.TryGetValue(plmFolderId, out int appFolderId))
                            {
                                result.SkippedNoMapping++;
                                continue;
                            }

                            if (UpdateReferenceFolderId(tenantConn, rootTable, referenceId, appFolderId))
                                updated++;
                        }
                    }
                }
            }

            return updated;
        }

        private static int PlaceColorDetailFolders(
            string tenantConnectionString,
            int sessionId,
            PlmFolderPlacementResultDto result)
        {
            if (!TableExistsOnConnection(tenantConnectionString, ColorGroupDetailTable))
                return 0;

            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();

                using (var skipCmd = conn.CreateCommand())
                {
                    skipCmd.CommandText = $@"
SELECT COUNT(*)
FROM dbo.{ColorGroupDetailTable} d
WHERE d.AppFolderId IS NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.AppPlmFolderMap m
      WHERE m.SessionId = @SessionId
        AND m.PlmFolderType = 2
        AND m.PlmFolderId = d.PlmFolderId);";
                    skipCmd.Parameters.AddWithValue("@SessionId", sessionId);
                    result.SkippedNoMapping += Convert.ToInt32(skipCmd.ExecuteScalar());
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
UPDATE d
SET d.AppFolderId = m.AppFolderId
FROM dbo.{ColorGroupDetailTable} d
INNER JOIN dbo.AppPlmFolderMap m
    ON m.PlmFolderId = d.PlmFolderId
   AND m.SessionId = @SessionId
   AND m.PlmFolderType = 2
WHERE d.AppFolderId IS NULL;";
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        private static int PlaceImageFolders(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId,
            PlmFolderPlacementResultDto result)
        {
            int updated = 0;
            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();

                var maps = LoadFolderMapByPlmId(tenantConn, sessionId, 7);
                var appFileIds = LoadAppFileIds(tenantConn);

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT s.SketchID, s.FolderID
FROM dbo.tblSketch s
WHERE s.FolderID IS NOT NULL;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int sketchId = reader.GetInt32(0);
                            int plmFolderId = reader.GetInt32(1);
                            if (!appFileIds.Contains(sketchId))
                                continue;
                            if (!maps.TryGetValue(plmFolderId, out int appFolderId))
                            {
                                result.SkippedNoMapping++;
                                continue;
                            }

                            using (var updateCmd = tenantConn.CreateCommand())
                            {
                                updateCmd.CommandText = @"
UPDATE dbo.AppFile
SET FolderID = @FolderId
WHERE FileID = @FileId AND (FolderID IS NULL OR FolderID <> @FolderId);";
                                updateCmd.Parameters.AddWithValue("@FolderId", appFolderId);
                                updateCmd.Parameters.AddWithValue("@FileId", sketchId);
                                updated += updateCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            return updated;
        }

        private static Dictionary<int, int> LoadFolderMapByPlmId(SqlConnection tenantConn, int sessionId, int plmFolderType)
        {
            var maps = new Dictionary<int, int>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT PlmFolderId, AppFolderId
FROM dbo.AppPlmFolderMap
WHERE SessionId = @SessionId AND PlmFolderType = @PlmFolderType;";
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                cmd.Parameters.AddWithValue("@PlmFolderType", plmFolderType);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        maps[reader.GetInt32(0)] = reader.GetInt32(1);
                }
            }

            return maps;
        }

        private static HashSet<int> LoadReferenceIds(SqlConnection tenantConn, string rootTable)
        {
            var ids = new HashSet<int>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = $"SELECT ReferenceId FROM dbo.[{rootTable.Replace("]", "]]")}];";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ids.Add(reader.GetInt32(0));
                }
            }

            return ids;
        }

        private static HashSet<int> LoadAppFileIds(SqlConnection tenantConn)
        {
            var ids = new HashSet<int>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT FileID FROM dbo.AppFile;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ids.Add(reader.GetInt32(0));
                }
            }

            return ids;
        }

        private static bool UpdateReferenceFolderId(SqlConnection tenantConn, string rootTable, int referenceId, int appFolderId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = $@"
UPDATE dbo.[{rootTable.Replace("]", "]]")}]
SET FolderId = @FolderId
WHERE ReferenceId = @ReferenceId AND (FolderId IS NULL OR FolderId <> @FolderId);";
                cmd.Parameters.AddWithValue("@FolderId", appFolderId);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static bool TableExists(SqlConnection conn, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName;";
                cmd.Parameters.AddWithValue("@TableName", tableName);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static bool TableExistsOnConnection(string connectionString, string tableName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return TableExists(conn, tableName);
            }
        }
    }
}
