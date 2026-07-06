using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const string PlmFolderActionPreview = "PlmFolderPreview";
        private const string PlmFolderActionImport = "PlmFolderImport";
        private const string ColorGroupDetailTable = "Plm_pdmColorGroupDetail";

        private static readonly int[] DefaultPlmFolderTypes = { 1, 2, 7 };

        private sealed class PlmSeFolderRow
        {
            public int FolderId { get; set; }
            public int FolderType { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int? ParentId { get; set; }
            public bool IsSystemFolder { get; set; }
        }

        private sealed class FolderImportScope
        {
            public int PlmFolderType { get; set; }
            public string PlmFolderTypeName { get; set; }
            public int AppFolderType { get; set; }
            public int? AppTransactionId { get; set; }
            public int? AppAnchorFolderId { get; set; }
        }

        public static OperationCallResult<PlmFolderImportPreviewDto> PreviewPlmFolderImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmFolderImportPreviewDto>
            {
                Object = new PlmFolderImportPreviewDto()
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
                result.Object = BuildPlmFolderImportPreview(
                    sessionId.Value,
                    session.PlmConnectionString.Trim(),
                    tenantConn);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Folder_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else
                {
                    WriteImportLog(fixture, sessionId.Value, null, StepFolderImport, PlmFolderActionPreview, "Success",
                        null, null, result.Object.Scopes?.Sum(s => s.ToCreateCount), null,
                        "PLM folder import preview completed.");
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Folder_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecutePlmFolderImport(int? sessionId)
        {
            return StartPlmFolderImportJob(sessionId);
        }

        public static OperationCallResult<PlmImportJobDto> StartPlmFolderImportJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypePlmFolderImport,
                    "Queued PLM folder tree and color-folder link import.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepFolderImport, PlmFolderActionImport, "Running",
                    null, null, null, null, "PLM folder import job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunPlmFolderImportJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Folder_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void RunPlmFolderImportJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var session = LoadSessionById(fixture, context.SessionId, includeConnection: false);
            if (session == null)
                throw new InvalidOperationException("Import session not found.");

            if (!session.CompanyId.HasValue || session.CompanyId.Value <= 0)
                throw new InvalidOperationException("Import session company is not set.");

            var importResult = ImportPlmFoldersAndColorLinks(
                context.SessionId,
                session.CompanyId.Value,
                context.PlmConnectionString,
                context.TenantConnectionString,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Folder import cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(importResult);
            if (!importResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "Folder import failed.",
                    resultJson: resultJson, errorMessage: importResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepFolderImport,
                    PlmFolderActionImport, "Failed", null, null, null, null, importResult.ErrorMessage);
                return;
            }

            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "Folder import completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepFolderImport,
                PlmFolderActionImport, "Success", null, null,
                importResult.FoldersCreated + importResult.ColorDetailsInserted, null,
                $"Folders created {importResult.FoldersCreated}, mappings {importResult.MappingsWritten}, color links inserted {importResult.ColorDetailsInserted}.");
        }

        private static PlmFolderImportPreviewDto BuildPlmFolderImportPreview(
            int sessionId,
            string plmConnectionString,
            string tenantConnectionString)
        {
            var preview = new PlmFolderImportPreviewDto { IsSuccess = true };
            EnsurePlmFolderImportTables(tenantConnectionString);

            var scopes = ResolveFolderImportScopes(tenantConnectionString);
            var existingMaps = LoadExistingFolderMapCounts(tenantConnectionString, sessionId);
            var allFolders = ReadPlmFolders(plmConnectionString, DefaultPlmFolderTypes);
            var foldersByType = allFolders.GroupBy(f => f.FolderType).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var scope in scopes)
            {
                var scopePreview = new PlmFolderImportScopePreviewDto
                {
                    PlmFolderType = scope.PlmFolderType,
                    PlmFolderTypeName = scope.PlmFolderTypeName,
                    AppTransactionId = scope.AppTransactionId,
                    AppAnchorFolderId = scope.AppAnchorFolderId
                };

                if (!foldersByType.TryGetValue(scope.PlmFolderType, out var rows) || rows.Count == 0)
                {
                    preview.Scopes.Add(scopePreview);
                    continue;
                }

                scopePreview.TotalPlmFolders = rows.Count;
                scopePreview.ExistingMappedFolders = rows.Count(r => existingMaps.ContainsKey(r.FolderId));
                scopePreview.ToCreateCount = scopePreview.TotalPlmFolders - scopePreview.ExistingMappedFolders;

                var idSet = new HashSet<int>(rows.Select(r => r.FolderId));
                scopePreview.MissingParentCount = rows.Count(r =>
                    r.ParentId.HasValue && r.ParentId.Value > 0 && !idSet.Contains(r.ParentId.Value));

                if (scopePreview.MissingParentCount > 0)
                {
                    preview.Warnings.Add(
                        $"{scope.PlmFolderTypeName}: {scopePreview.MissingParentCount} folder(s) reference a parent outside this folder type tree.");
                }

                preview.Scopes.Add(scopePreview);
            }

            CountColorGroupDetailPreview(plmConnectionString, tenantConnectionString, preview);
            return preview;
        }

        private static PlmFolderImportResultDto ImportPlmFoldersAndColorLinks(
            int sessionId,
            int companyId,
            string plmConnectionString,
            string tenantConnectionString,
            Action<int, string> progressCallback)
        {
            var result = new PlmFolderImportResultDto { IsSuccess = true };
            EnsurePlmFolderImportTables(tenantConnectionString);

            var scopes = ResolveFolderImportScopes(tenantConnectionString);
            var allFolders = ReadPlmFolders(plmConnectionString, DefaultPlmFolderTypes);
            var foldersByType = allFolders.GroupBy(f => f.FolderType).ToDictionary(g => g.Key, g => g.ToList());
            var existingMaps = LoadExistingFolderMaps(tenantConnectionString, sessionId);

            int scopeIndex = 0;
            int totalScopes = scopes.Count;
            foreach (var scope in scopes)
            {
                scopeIndex++;
                if (!foldersByType.TryGetValue(scope.PlmFolderType, out var rows) || rows.Count == 0)
                    continue;

                progressCallback?.Invoke(
                    (int)(20.0 * scopeIndex / Math.Max(1, totalScopes)),
                    $"Importing {scope.PlmFolderTypeName} folders ({scopeIndex}/{totalScopes})…");

                var ordered = TopologicalSortFolders(rows);
                var plmIdSet = new HashSet<int>(rows.Select(r => r.FolderId));
                var plmToApp = new Dictionary<int, int>();

                foreach (var existing in existingMaps.Where(m => m.PlmFolderType == scope.PlmFolderType))
                {
                    plmToApp[existing.PlmFolderId] = existing.AppFolderId;
                    result.FoldersSkippedExisting++;
                }

                int rowIndex = 0;
                foreach (var row in ordered)
                {
                    rowIndex++;
                    if (plmToApp.ContainsKey(row.FolderId))
                        continue;

                    if (rowIndex % 25 == 0)
                    {
                        int pct = 20 + (int)(50.0 * scopeIndex / Math.Max(1, totalScopes) * rowIndex / Math.Max(1, ordered.Count));
                        progressCallback?.Invoke(pct, $"Creating {scope.PlmFolderTypeName} folder {rowIndex}/{ordered.Count}…");
                    }

                    int? appParentId = ResolveAppParentFolderId(row, scope, plmIdSet, plmToApp);
                    try
                    {
                        int appFolderId = InsertAppFolder(tenantConnectionString, row, scope, appParentId);
                        plmToApp[row.FolderId] = appFolderId;
                        UpsertFolderMap(tenantConnectionString, sessionId, companyId, row, scope, appFolderId, appParentId);
                        result.FoldersCreated++;
                        result.MappingsWritten++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Folder {row.FolderId} ({row.Name}): {ex.Message}");
                    }
                }
            }

            progressCallback?.Invoke(75, "Importing color-folder links (pdmColorGroupDetail)…");
            ImportColorGroupDetails(plmConnectionString, tenantConnectionString, result, progressCallback);

            if (result.Errors.Count > 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = string.Join("; ", result.Errors.Take(5));
            }

            progressCallback?.Invoke(100, "Folder import finished.");
            return result;
        }

        private static void ImportColorGroupDetails(
            string plmConnectionString,
            string tenantConnectionString,
            PlmFolderImportResultDto result,
            Action<int, string> progressCallback)
        {
            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();
                EnsurePlmColorGroupDetailSchema(plmConn);

                using (var readCmd = plmConn.CreateCommand())
                {
                    readCmd.CommandText = @"
SELECT ColorGroupDetailID, ColorGroupID, RGBColorID, FolderID
FROM dbo.pdmColorGroupDetail
WHERE FolderID IS NOT NULL
ORDER BY ColorGroupDetailID;";

                    using (var reader = readCmd.ExecuteReader())
                    {
                        int rowNum = 0;
                        while (reader.Read())
                        {
                            rowNum++;
                            if (rowNum % 500 == 0)
                                progressCallback?.Invoke(75 + (int)(20.0 * rowNum / 9287.0), $"Importing color-folder links ({rowNum})…");

                            int detailId = reader.GetInt32(0);
                            int? colorGroupId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
                            int rgbColorId = reader.GetInt32(2);
                            int plmFolderId = reader.GetInt32(3);

                            if (ColorGroupDetailExists(tenantConn, detailId))
                            {
                                result.ColorDetailsSkipped++;
                                continue;
                            }

                            InsertColorGroupDetail(tenantConn, detailId, colorGroupId, rgbColorId, plmFolderId);
                            result.ColorDetailsInserted++;
                        }
                    }
                }
            }
        }

        private static void CountColorGroupDetailPreview(
            string plmConnectionString,
            string tenantConnectionString,
            PlmFolderImportPreviewDto preview)
        {
            using (var plmConn = new SqlConnection(plmConnectionString))
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                plmConn.Open();
                tenantConn.Open();
                EnsurePlmColorGroupDetailSchema(plmConn);

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM dbo.pdmColorGroupDetail WHERE FolderID IS NOT NULL;";
                    preview.ColorDetailSourceCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = tenantConn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(*) FROM dbo.{ColorGroupDetailTable};";
                    preview.ColorDetailExistingCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                preview.ColorDetailReadyToImport = Math.Max(
                    0,
                    preview.ColorDetailSourceCount - preview.ColorDetailExistingCount);
            }
        }

        private static void EnsurePlmFolderImportTables(string tenantConnectionString)
        {
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                EnsurePlmImportSchema();
            }
        }

        private static void EnsurePlmColorGroupDetailSchema(SqlConnection plmConn)
        {
            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'pdmColorGroupDetail';";
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("PLM table dbo.pdmColorGroupDetail was not found.");
            }
        }

        private static List<FolderImportScope> ResolveFolderImportScopes(string tenantConnectionString)
        {
            int? fileTransactionId = null;
            int? fileRootFolderId = null;

            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP 1 TransactionID, MgtRootFolderID
FROM dbo.AppTransaction
WHERE EmAppTransBusinessType = @FileType
ORDER BY TransactionID;";
                    cmd.Parameters.AddWithValue("@FileType", (int)EmAppTransBusinessType.File);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fileTransactionId = reader.GetInt32(0);
                            if (!reader.IsDBNull(1))
                                fileRootFolderId = reader.GetInt32(1);
                        }
                    }
                }
            }

            return new List<FolderImportScope>
            {
                new FolderImportScope
                {
                    PlmFolderType = 1,
                    PlmFolderTypeName = "Product",
                    AppFolderType = (int)EmAppTransBusinessType.FormData,
                    AppTransactionId = null,
                    AppAnchorFolderId = null
                },
                new FolderImportScope
                {
                    PlmFolderType = 2,
                    PlmFolderTypeName = "Color",
                    AppFolderType = (int)EmAppTransBusinessType.FormData,
                    AppTransactionId = null,
                    AppAnchorFolderId = null
                },
                new FolderImportScope
                {
                    PlmFolderType = 7,
                    PlmFolderTypeName = "Sketch",
                    AppFolderType = (int)EmAppTransBusinessType.File,
                    AppTransactionId = fileTransactionId,
                    AppAnchorFolderId = fileRootFolderId
                }
            };
        }

        private static List<PlmSeFolderRow> ReadPlmFolders(string plmConnectionString, IEnumerable<int> folderTypes)
        {
            var typeList = folderTypes?.Distinct().ToList() ?? new List<int>();
            if (typeList.Count == 0)
                return new List<PlmSeFolderRow>();

            var rows = new List<PlmSeFolderRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                EnsurePlmSeFolderSchema(conn);

                string inClause = string.Join(",", typeList);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
SELECT FolderID, FolderType, Name, Description, ParentID, IsSystemFolder
FROM dbo.pdmSEFolder
WHERE FolderType IN ({inClause})
ORDER BY FolderType, ParentID, FolderID;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new PlmSeFolderRow
                            {
                                FolderId = reader.GetInt32(0),
                                FolderType = reader.GetInt32(1),
                                Name = reader.IsDBNull(2) ? $"Folder_{reader.GetInt32(0)}" : reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                ParentId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                IsSystemFolder = !reader.IsDBNull(5) && reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static void EnsurePlmSeFolderSchema(SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'pdmSEFolder';";
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("PLM table dbo.pdmSEFolder was not found.");
            }
        }

        private static List<PlmSeFolderRow> TopologicalSortFolders(List<PlmSeFolderRow> rows)
        {
            var byId = rows.ToDictionary(r => r.FolderId);
            var result = new List<PlmSeFolderRow>();
            var visited = new HashSet<int>();
            var visiting = new HashSet<int>();

            void Visit(PlmSeFolderRow node)
            {
                if (visited.Contains(node.FolderId))
                    return;
                if (visiting.Contains(node.FolderId))
                    return;

                visiting.Add(node.FolderId);
                if (node.ParentId.HasValue && byId.TryGetValue(node.ParentId.Value, out var parent))
                    Visit(parent);
                visiting.Remove(node.FolderId);
                visited.Add(node.FolderId);
                result.Add(node);
            }

            foreach (var row in rows.OrderBy(r => r.ParentId ?? 0).ThenBy(r => r.FolderId))
                Visit(row);

            return result;
        }

        private static int? ResolveAppParentFolderId(
            PlmSeFolderRow row,
            FolderImportScope scope,
            HashSet<int> plmIdSet,
            Dictionary<int, int> plmToApp)
        {
            if (!row.ParentId.HasValue || row.ParentId.Value <= 0)
                return scope.AppAnchorFolderId;

            if (plmToApp.TryGetValue(row.ParentId.Value, out int mappedParent))
                return mappedParent;

            if (!plmIdSet.Contains(row.ParentId.Value))
                return scope.AppAnchorFolderId;

            return scope.AppAnchorFolderId;
        }

        private static int InsertAppFolder(
            string tenantConnectionString,
            PlmSeFolderRow row,
            FolderImportScope scope,
            int? appParentId)
        {
            string folderName = TruncateString(row.Name, 200);
            int? userId = AppSecurityUserBL.CurrentUserId;
            object companyId = APP.Framework.ServerContext.Instance.CurrentCompanyId;
            var now = DateTime.UtcNow;

            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppSEFolder
    (FolderType, Name, Description, ParentID, IsSystemFolder, TransactionID,
     AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID, AppCreatedByCompanyID)
VALUES
    (@FolderType, @Name, @Description, @ParentID, @IsSystemFolder, @TransactionID,
     @UserId, @CreatedDate, @ModifiedDate, @UserId, @CompanyId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@FolderType", scope.AppFolderType);
                    cmd.Parameters.AddWithValue("@Name", folderName);
                    cmd.Parameters.AddWithValue("@Description", (object)TruncateString(row.Description, 500) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ParentID", (object)appParentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsSystemFolder", row.IsSystemFolder);
                    cmd.Parameters.AddWithValue("@TransactionID", (object)scope.AppTransactionId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedDate", now);
                    cmd.Parameters.AddWithValue("@ModifiedDate", now);
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private static void UpsertFolderMap(
            string tenantConnectionString,
            int sessionId,
            int companyId,
            PlmSeFolderRow row,
            FolderImportScope scope,
            int appFolderId,
            int? appParentId)
        {
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.AppPlmFolderMap WHERE SessionId = @SessionId AND PlmFolderId = @PlmFolderId)
BEGIN
    UPDATE dbo.AppPlmFolderMap SET
        AppFolderId = @AppFolderId,
        AppName = @AppName,
        PlmParentId = @PlmParentId,
        LastSyncAt = @LastSyncAt
    WHERE SessionId = @SessionId AND PlmFolderId = @PlmFolderId;
END
ELSE
BEGIN
    INSERT INTO dbo.AppPlmFolderMap
        (SessionId, CompanyId, PlmFolderId, AppFolderId, PlmFolderType, AppTransactionId, AppFolderType,
         PlmParentId, PlmName, AppName, LastSyncAt)
    VALUES
        (@SessionId, @CompanyId, @PlmFolderId, @AppFolderId, @PlmFolderType, @AppTransactionId, @AppFolderType,
         @PlmParentId, @PlmName, @AppName, @LastSyncAt);
END";
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@PlmFolderId", row.FolderId);
                    cmd.Parameters.AddWithValue("@AppFolderId", appFolderId);
                    cmd.Parameters.AddWithValue("@PlmFolderType", scope.PlmFolderType);
                    cmd.Parameters.AddWithValue("@AppTransactionId", (object)scope.AppTransactionId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AppFolderType", scope.AppFolderType);
                    cmd.Parameters.AddWithValue("@PlmParentId", (object)row.ParentId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PlmName", TruncateString(row.Name, 200));
                    cmd.Parameters.AddWithValue("@AppName", TruncateString(row.Name, 200));
                    cmd.Parameters.AddWithValue("@LastSyncAt", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private sealed class ExistingFolderMap
        {
            public int PlmFolderId { get; set; }
            public int AppFolderId { get; set; }
            public int PlmFolderType { get; set; }
        }

        private static Dictionary<int, int> LoadExistingFolderMapCounts(string tenantConnectionString, int sessionId)
        {
            return LoadExistingFolderMaps(tenantConnectionString, sessionId)
                .ToDictionary(m => m.PlmFolderId, m => m.AppFolderId);
        }

        private static List<ExistingFolderMap> LoadExistingFolderMaps(string tenantConnectionString, int sessionId)
        {
            var maps = new List<ExistingFolderMap>();
            using (var conn = new SqlConnection(tenantConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT PlmFolderId, AppFolderId, PlmFolderType
FROM dbo.AppPlmFolderMap
WHERE SessionId = @SessionId;";
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            maps.Add(new ExistingFolderMap
                            {
                                PlmFolderId = reader.GetInt32(0),
                                AppFolderId = reader.GetInt32(1),
                                PlmFolderType = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }

            return maps;
        }

        private static bool ColorGroupDetailExists(SqlConnection tenantConn, int detailId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM dbo.{ColorGroupDetailTable} WHERE ColorGroupDetailID = @Id;";
                cmd.Parameters.AddWithValue("@Id", detailId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void InsertColorGroupDetail(
            SqlConnection tenantConn,
            int detailId,
            int? colorGroupId,
            int rgbColorId,
            int plmFolderId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = $@"
INSERT INTO dbo.{ColorGroupDetailTable}
    (ColorGroupDetailID, ColorGroupID, RGBColorID, PlmFolderId, AppFolderId)
VALUES
    (@DetailId, @ColorGroupId, @RgbColorId, @PlmFolderId, NULL);";
                cmd.Parameters.AddWithValue("@DetailId", detailId);
                cmd.Parameters.AddWithValue("@ColorGroupId", (object)colorGroupId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RgbColorId", rgbColorId);
                cmd.Parameters.AddWithValue("@PlmFolderId", plmFolderId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
