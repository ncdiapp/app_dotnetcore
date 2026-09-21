using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using App.BL;
using APP.AgentPlugins.PlmImport;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
    {
        public const string JobTypePlmSketchImport = "PlmSketchImport";
        private const string PlmSketchActionPreview = "PlmSketchPreview";
        private const string PlmSketchActionImport = "PlmSketchImport";

        public static OperationCallResult<PlmSketchImportPreviewDto> PreviewPlmSketchImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmSketchImportPreviewDto>
            {
                Object = new PlmSketchImportPreviewDto()
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
                result.Object = SketchImportPreviewBuilder.Build(
                    session.PlmConnectionString.Trim(), tenantConn);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmImportEngine), "Plm_Sketch_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else
                {
                    WriteImportLog(fixture, sessionId.Value, null, StepOtherData, PlmSketchActionPreview, "Success",
                        null, null, result.Object.ReadyToImportCount, null,
                        $"Sketch preview complete. Ready {result.Object.ReadyToImportCount}, existing {result.Object.ExistingAppFileCount}.");
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportEngine), "Plm_Sketch_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecutePlmSketchImport(int? sessionId)
        {
            return StartPlmSketchImportJob(sessionId);
        }

        public static OperationCallResult<PlmImportJobDto> StartPlmSketchImportJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypePlmSketchImport,
                    "Queued PLM tblSketch to AppFile import.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepOtherData, PlmSketchActionImport, "Running",
                    null, null, null, null, "PLM sketch import job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunPlmSketchImportJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportEngine), "Plm_Sketch_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void RunPlmSketchImportJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var importResult = ImportPlmSketchesToAppFile(
                context.PlmConnectionString,
                context.TenantConnectionString,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Sketch import cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(importResult);
            if (!importResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "Sketch import failed.",
                    resultJson: resultJson, errorMessage: importResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepOtherData,
                    PlmSketchActionImport, "Failed", null, null, null, null, importResult.ErrorMessage);
                return;
            }

            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "Sketch import completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepOtherData,
                PlmSketchActionImport, "Success", null, null, importResult.InsertedCount, null,
                $"Inserted {importResult.InsertedCount}, skipped existing {importResult.SkippedExistingCount}, skipped missing binary {importResult.SkippedMissingBinaryCount}, failed {importResult.FailedCount}.");
        }

        private static PlmSketchImportResultDto ImportPlmSketchesToAppFile(
            string plmConnectionString,
            string tenantConnectionString,
            Action<int, string> progressCallback)
        {
            var result = new PlmSketchImportResultDto();
            string imageRoot = AppCompanyBL.GetMyCompanyImagePath();
            EnsureAppImageFolders(imageRoot);

            string stagingRoot = Path.Combine(
                Path.GetTempPath(),
                "AppAI_PlmSketchStaging",
                Guid.NewGuid().ToString("N"));

            try
            {
                int[] skipIds;
                using (var tenantConn = new SqlConnection(tenantConnectionString))
                {
                    tenantConn.Open();
                    skipIds = new List<int>(LoadExistingAppFileIds(tenantConn)).ToArray();
                }

                // Same-assembly exporters (formerly ExternalDll bridge).
                var manifest = SketchImportExporter.Export(
                    plmConnectionString,
                    stagingRoot,
                    skipIds,
                    (percent, message) => progressCallback?.Invoke(percent, message));

                if (!manifest.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = manifest.ErrorMessage ?? "Sketch export from ExternalDll failed.";
                    result.SourceSketchCount = manifest.SourceSketchCount;
                    result.SkippedExistingCount = manifest.SkippedExistingCount;
                    result.SkippedMissingBinaryCount = manifest.SkippedMissingBinaryCount;
                    return result;
                }

                result.SourceSketchCount = manifest.SourceSketchCount;
                result.SkippedExistingCount = manifest.SkippedExistingCount;
                result.SkippedMissingBinaryCount = manifest.SkippedMissingBinaryCount;

                using (var tenantConn = new SqlConnection(tenantConnectionString))
                {
                    tenantConn.Open();
                    SetAppFileIdentityInsert(tenantConn, true);
                    try
                    {
                        int total = manifest.Items?.Count ?? 0;
                        int processed = 0;
                        foreach (var item in manifest.Items ?? new List<PlmSketchStagingItemDto>())
                        {
                            processed++;
                            if (processed == 1 || processed % 50 == 0 || processed == total)
                            {
                                int percent = total == 0
                                    ? 100
                                    : Math.Min(99, 50 + (int)Math.Round(processed * 49.0 / total));
                                progressCallback?.Invoke(percent, $"Writing AppFile {processed} of {total}...");
                            }

                            try
                            {
                                InsertSketchStagingAsAppFile(tenantConn, imageRoot, item);
                                result.InsertedCount++;
                                if (item.IsImage)
                                    result.ImageInsertedCount++;
                                else
                                    result.FileInsertedCount++;
                            }
                            catch (Exception ex)
                            {
                                result.FailedCount++;
                                if (result.Errors.Count < 100)
                                    result.Errors.Add($"SketchID {item.SketchId}: {ex.Message}");
                            }
                        }
                    }
                    finally
                    {
                        SetAppFileIdentityInsert(tenantConn, false);
                    }

                    ReseedAppFileIdentity(tenantConn);
                }

                result.IsSuccess = result.FailedCount == 0;
                if (!result.IsSuccess)
                    result.ErrorMessage = $"{result.FailedCount} sketch row(s) failed. See Errors for details.";

                progressCallback?.Invoke(100, "Sketch import completed.");
                return result;
            }
            finally
            {
                TryDeleteDirectory(stagingRoot);
            }
        }

        private static void InsertSketchStagingAsAppFile(
            SqlConnection tenantConn,
            string imageRoot,
            PlmSketchStagingItemDto item)
        {
            string originalPath = null;
            string thumbnailPath = null;
            string regularPath = null;
            byte[] fileContent = null;

            if (item.IsImage)
            {
                if (!string.IsNullOrWhiteSpace(item.OriginalStagingPath) && File.Exists(item.OriginalStagingPath))
                    originalPath = WriteAppFileBytes(imageRoot, DocumentInfoDto.ImageOriginalSizeLocation,
                        File.ReadAllBytes(item.OriginalStagingPath));
                if (!string.IsNullOrWhiteSpace(item.ThumbnailStagingPath) && File.Exists(item.ThumbnailStagingPath))
                    thumbnailPath = WriteAppFileBytes(imageRoot, DocumentInfoDto.ImageThumbnailLocation,
                        File.ReadAllBytes(item.ThumbnailStagingPath));
                if (!string.IsNullOrWhiteSpace(item.RegularStagingPath) && File.Exists(item.RegularStagingPath))
                    regularPath = WriteAppFileBytes(imageRoot, DocumentInfoDto.ImageRegularSizeLocation,
                        File.ReadAllBytes(item.RegularStagingPath));
            }
            else if (!string.IsNullOrWhiteSpace(item.ContentStagingPath) && File.Exists(item.ContentStagingPath))
            {
                fileContent = File.ReadAllBytes(item.ContentStagingPath);
            }

            string fileCode = !string.IsNullOrWhiteSpace(item.SketchCode)
                ? item.SketchCode
                : $"Sketch_{item.SketchId}{item.Extension}";
            fileCode = TruncateString(fileCode, 200);

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppFile
    (FileID, FileCode, Description, FolderID, FileType, Extension,
     OriginalFilePath, ThumbnailFilePath, RegularImageFilepath, FileContent,
     InitialFileID, AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID, AppCreatedByCompanyID)
VALUES
    (@FileID, @FileCode, @Description, NULL, @FileType, @Extension,
     @OriginalFilePath, @ThumbnailFilePath, @RegularImageFilepath, @FileContent,
     @InitialFileID, @UserId, @CreatedDate, @ModifiedDate, @UserId, @CompanyId);";
                cmd.Parameters.AddWithValue("@FileID", item.SketchId);
                cmd.Parameters.AddWithValue("@FileCode", (object)fileCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object)item.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FileType", item.FileType);
                cmd.Parameters.AddWithValue("@Extension", (object)item.Extension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OriginalFilePath", (object)originalPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ThumbnailFilePath", (object)thumbnailPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RegularImageFilepath", (object)regularPath ?? DBNull.Value);
                cmd.Parameters.Add("@FileContent", SqlDbType.VarBinary, -1).Value = (object)fileContent ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@InitialFileID", DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object)AppSecurityUserBL.CurrentUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedDate", (object)item.CreatedDate ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@ModifiedDate", (object)item.ModifyDate ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@CompanyId", APP.Framework.ServerContext.Instance.CurrentCompanyId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
            try { Directory.Delete(path, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        private static HashSet<int> LoadExistingAppFileIds(SqlConnection tenantConn)
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

        private static void EnsureAppImageFolders(string imageRoot)
        {
            Directory.CreateDirectory(imageRoot + DocumentInfoDto.ImageOriginalSizeLocation);
            Directory.CreateDirectory(imageRoot + DocumentInfoDto.ImageRegularSizeLocation);
            Directory.CreateDirectory(imageRoot + DocumentInfoDto.ImageThumbnailLocation);
        }

        private static string WriteAppFileBytes(string imageRoot, string relativeFolder, byte[] bytes)
        {
            string relativePath = relativeFolder + Guid.NewGuid().ToString("N");
            string absolutePath = imageRoot + relativePath;
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(absolutePath, bytes);
            return relativePath;
        }

        private static void SetAppFileIdentityInsert(SqlConnection conn, bool enabled)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = enabled
                    ? "SET IDENTITY_INSERT dbo.AppFile ON;"
                    : "SET IDENTITY_INSERT dbo.AppFile OFF;";
                cmd.ExecuteNonQuery();
            }
        }

        private static void ReseedAppFileIdentity(SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
DECLARE @MaxId INT;
SELECT @MaxId = ISNULL(MAX(FileID), 0) FROM dbo.AppFile;
DBCC CHECKIDENT ('dbo.AppFile', RESEED, @MaxId) WITH NO_INFOMSGS;";
                cmd.ExecuteNonQuery();
            }
        }

        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
