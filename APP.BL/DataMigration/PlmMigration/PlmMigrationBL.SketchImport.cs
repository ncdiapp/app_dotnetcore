using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
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
                result.Object = BuildPlmSketchImportPreview(session.PlmConnectionString.Trim(), tenantConn);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Sketch_Preview_Error", ValidationItemType.Error,
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
                    typeof(PlmMigrationBL), "Plm_Sketch_Preview_Error", ValidationItemType.Error, ex.Message));
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
                    typeof(PlmMigrationBL), "Plm_Sketch_Execute_Error", ValidationItemType.Error, ex.Message));
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

        private static PlmSketchImportPreviewDto BuildPlmSketchImportPreview(
            string plmConnectionString,
            string tenantConnectionString)
        {
            var preview = new PlmSketchImportPreviewDto();
            var sourceIds = new List<int>();

            using (var plmConn = new SqlConnection(plmConnectionString))
            {
                plmConn.Open();
                EnsurePlmSketchSchema(plmConn);

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT
    SketchID,
    Extension,
    ImageType,
    CASE WHEN SketchImage IS NOT NULL OR Thumbnail IS NOT NULL OR OriginalImage IS NOT NULL THEN 1 ELSE 0 END AS HasBinary
FROM dbo.tblSketch
ORDER BY SketchID;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int sketchId = reader.GetInt32(0);
                            preview.SourceSketchCount++;

                            bool hasBinary = reader.GetInt32(3) == 1;
                            if (!hasBinary)
                            {
                                preview.MissingBinaryCount++;
                                continue;
                            }

                            sourceIds.Add(sketchId);
                            preview.SourceWithBinaryCount++;
                            string extension = NormalizeExtension(reader.IsDBNull(1) ? null : reader.GetString(1),
                                reader.IsDBNull(2) ? (int?)null : Convert.ToInt32(reader["ImageType"]));
                            var docType = DocumentHelper.GetDocumentTypeByExtensionName(extension);
                            if (IsAppImageType(docType))
                                preview.ImageCount++;
                            else
                                preview.FileCount++;
                        }
                    }
                }
            }

            preview.ExistingAppFileCount = CountExistingAppFiles(tenantConnectionString, sourceIds);
            preview.ReadyToImportCount = Math.Max(0, preview.SourceWithBinaryCount - preview.ExistingAppFileCount);
            if (preview.MissingBinaryCount > 0)
                preview.Warnings.Add($"{preview.MissingBinaryCount} tblSketch row(s) have no SketchImage, Thumbnail, or OriginalImage binary.");
            if (preview.ExistingAppFileCount > 0)
                preview.Warnings.Add($"{preview.ExistingAppFileCount} AppFile row(s) already use the same FileID and will be skipped.");

            preview.IsSuccess = true;
            return preview;
        }

        private static PlmSketchImportResultDto ImportPlmSketchesToAppFile(
            string plmConnectionString,
            string tenantConnectionString,
            Action<int, string> progressCallback)
        {
            var result = new PlmSketchImportResultDto();
            string imageRoot = AppCompanyBL.GetMyCompanyImagePath();
            EnsureAppImageFolders(imageRoot);

            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();
                EnsurePlmSketchSchema(plmConn);

                var existingIds = LoadExistingAppFileIds(tenantConn);
                int total = CountSourceSketches(plmConn);
                result.SourceSketchCount = total;

                SetAppFileIdentityInsert(tenantConn, true);
                try
                {
                    using (var cmd = plmConn.CreateCommand())
                    {
                        cmd.CommandTimeout = 0;
                        cmd.CommandText = @"
SELECT
    SketchID,
    SketchCode,
    SketchImage,
    Thumbnail,
    ImageType,
    OriginalImage,
    CreatedDate,
    ModifyDate,
    Description,
    Extension
FROM dbo.tblSketch
ORDER BY SketchID;";

                        using (var reader = cmd.ExecuteReader())
                        {
                            int processed = 0;
                            while (reader.Read())
                            {
                                processed++;
                                if (processed == 1 || processed % 50 == 0)
                                {
                                    int percent = total == 0 ? 100 : Math.Min(99, (int)Math.Round(processed * 100.0 / total));
                                    progressCallback?.Invoke(percent, $"Importing sketch {processed} of {total}...");
                                }

                                int sketchId = reader.GetInt32(0);
                                if (existingIds.Contains(sketchId))
                                {
                                    result.SkippedExistingCount++;
                                    continue;
                                }

                                try
                                {
                                    var row = ReadSketchRow(reader);
                                    if (!row.HasAnyBinary)
                                    {
                                        result.SkippedMissingBinaryCount++;
                                        continue;
                                    }

                                    InsertSketchAsAppFile(tenantConn, imageRoot, row);
                                    existingIds.Add(sketchId);
                                    result.InsertedCount++;
                                    if (row.IsImage)
                                        result.ImageInsertedCount++;
                                    else
                                        result.FileInsertedCount++;
                                }
                                catch (Exception ex)
                                {
                                    result.FailedCount++;
                                    if (result.Errors.Count < 100)
                                        result.Errors.Add($"SketchID {sketchId}: {ex.Message}");
                                }
                            }
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

        private sealed class PlmSketchRow
        {
            public int SketchId { get; set; }
            public string SketchCode { get; set; }
            public byte[] SketchImage { get; set; }
            public byte[] Thumbnail { get; set; }
            public byte[] OriginalImage { get; set; }
            public DateTime? CreatedDate { get; set; }
            public DateTime? ModifyDate { get; set; }
            public string Description { get; set; }
            public string Extension { get; set; }
            public int FileType { get; set; }
            public bool IsImage { get; set; }
            public bool HasAnyBinary => SketchImage != null || Thumbnail != null || OriginalImage != null;
        }

        private static PlmSketchRow ReadSketchRow(SqlDataReader reader)
        {
            string extension = NormalizeExtension(
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader["ImageType"]));
            var docType = DocumentHelper.GetDocumentTypeByExtensionName(extension);

            return new PlmSketchRow
            {
                SketchId = reader.GetInt32(0),
                SketchCode = TruncateString(reader.IsDBNull(1) ? null : reader.GetString(1), 200),
                SketchImage = reader.IsDBNull(2) ? null : (byte[])reader["SketchImage"],
                Thumbnail = reader.IsDBNull(3) ? null : (byte[])reader["Thumbnail"],
                OriginalImage = reader.IsDBNull(5) ? null : (byte[])reader["OriginalImage"],
                CreatedDate = reader.IsDBNull(6) ? (DateTime?)null : Convert.ToDateTime(reader["CreatedDate"]),
                ModifyDate = reader.IsDBNull(7) ? (DateTime?)null : Convert.ToDateTime(reader["ModifyDate"]),
                Description = TruncateString(reader.IsDBNull(8) ? null : reader.GetString(8), 100),
                Extension = extension,
                FileType = (int)docType,
                IsImage = IsAppImageType(docType)
            };
        }

        private static void InsertSketchAsAppFile(SqlConnection tenantConn, string imageRoot, PlmSketchRow row)
        {
            string originalPath = null;
            string thumbnailPath = null;
            string regularPath = null;
            byte[] fileContent = null;

            if (row.IsImage)
            {
                if (row.OriginalImage != null)
                    originalPath = WriteAppFileBytes(imageRoot, DocumentInfoDto.ImageOriginalSizeLocation, row.OriginalImage);
                if (row.Thumbnail != null)
                    thumbnailPath = WriteAppFileBytes(imageRoot, DocumentInfoDto.ImageThumbnailLocation, row.Thumbnail);
                if (row.SketchImage != null)
                    regularPath = WriteAppFileBytes(imageRoot, DocumentInfoDto.ImageRegularSizeLocation, row.SketchImage);
            }
            else
            {
                fileContent = row.OriginalImage ?? row.SketchImage ?? row.Thumbnail;
            }

            string fileCode = !string.IsNullOrWhiteSpace(row.SketchCode)
                ? row.SketchCode
                : $"Sketch_{row.SketchId}{row.Extension}";
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
                cmd.Parameters.AddWithValue("@FileID", row.SketchId);
                cmd.Parameters.AddWithValue("@FileCode", (object)fileCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", (object)row.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FileType", row.FileType);
                cmd.Parameters.AddWithValue("@Extension", (object)row.Extension ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OriginalFilePath", (object)originalPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ThumbnailFilePath", (object)thumbnailPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RegularImageFilepath", (object)regularPath ?? DBNull.Value);
                cmd.Parameters.Add("@FileContent", SqlDbType.VarBinary, -1).Value = (object)fileContent ?? DBNull.Value;
                // Original files must have InitialFileID NULL so App_FileView (WHERE InitialFileID IS NULL) shows them.
                // A non-null InitialFileID marks a row as a version of another file and hides it from the file list.
                cmd.Parameters.AddWithValue("@InitialFileID", DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", (object)AppSecurityUserBL.CurrentUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedDate", (object)row.CreatedDate ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@ModifiedDate", (object)row.ModifyDate ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@CompanyId", APP.Framework.ServerContext.Instance.CurrentCompanyId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsurePlmSketchSchema(SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSketch';";
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("PLM table dbo.tblSketch was not found.");
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'tblSketch'
  AND COLUMN_NAME IN ('SketchID', 'SketchCode', 'SketchImage', 'Thumbnail', 'OriginalImage', 'Extension');";
                if (Convert.ToInt32(cmd.ExecuteScalar()) < 6)
                    throw new InvalidOperationException("PLM dbo.tblSketch is missing one or more required columns.");
            }
        }

        private static int CountSourceSketches(SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.tblSketch;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
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

        private static int CountExistingAppFiles(string tenantConnectionString, List<int> sourceIds)
        {
            if (sourceIds.Count == 0) return 0;

            int count = 0;
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                for (int offset = 0; offset < sourceIds.Count; offset += 1000)
                {
                    int take = Math.Min(1000, sourceIds.Count - offset);
                    using (var cmd = conn.CreateCommand())
                    {
                        var names = new List<string>();
                        for (int i = 0; i < take; i++)
                        {
                            string name = "@p" + i;
                            names.Add(name);
                            cmd.Parameters.AddWithValue(name, sourceIds[offset + i]);
                        }

                        cmd.CommandText = $"SELECT COUNT(*) FROM dbo.AppFile WHERE FileID IN ({string.Join(",", names)});";
                        count += Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }

            return count;
        }

        private static bool IsAppImageType(EmAppDocumentType docType)
        {
            return docType == EmAppDocumentType.JPG
                   || docType == EmAppDocumentType.GIF
                   || docType == EmAppDocumentType.PNG
                   || docType == EmAppDocumentType.BMP
                   || docType == EmAppDocumentType.TIF;
        }

        private static string NormalizeExtension(string extension, int? imageType)
        {
            string ext = string.IsNullOrWhiteSpace(extension) ? null : extension.Trim();
            if (string.IsNullOrWhiteSpace(ext) && imageType.HasValue)
            {
                switch (imageType.Value)
                {
                    case (int)EmAppDocumentType.JPG:
                        ext = ".jpg";
                        break;
                    case (int)EmAppDocumentType.GIF:
                        ext = ".gif";
                        break;
                    case (int)EmAppDocumentType.BMP:
                        ext = ".bmp";
                        break;
                    case (int)EmAppDocumentType.TIF:
                        ext = ".tif";
                        break;
                    case (int)EmAppDocumentType.PNG:
                        ext = ".png";
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(ext))
                return string.Empty;

            if (!ext.StartsWith(".", StringComparison.Ordinal))
                ext = "." + ext;

            return TruncateString(ext, 50);
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
