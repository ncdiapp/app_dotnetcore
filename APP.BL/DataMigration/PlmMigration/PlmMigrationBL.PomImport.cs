using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public const string StepPomImport = "PomImport";
        public const string JobTypePlmPomImport = "PlmPomImport";
        private const string PlmPomActionPreview = "PlmPomPreview";
        private const string PlmPomActionImport = "PlmPomImport";

        private const int PlmPomFolderType = 14;
        private const int PlmPomTemplateFolderType = 5;

        private const string PomBodyPartTableName = "Plm_PdmV2kBodyPart";
        private const string PomBodyTypeTableName = "Plm_pdmv2kBodyType";
        private const string PomBodyTypeDetailSourceTable = "pdmV2kBodyTypeDetail";
        private const string PomBodyTypeDetailTableName = "Plm_pdmV2kBodyTypeDetail";
        private const string PomSpecGradingSourceTable = "pdmV2kSpecBodyPartGrading";
        private const string PomSpecGradingTableName = "Plm_pdmV2kSpecBodyPartGrading";

        private const string PomTxIntegrationId = "PlmPom_BodyPart";
        private const string PomListSearchIntegrationId = "PlmPom_BodyPart_List";
        private const string PomFolderSearchIntegrationId = "PlmPom_BodyPart_FolderNav";

        // Tchp* integration IDs — do not reuse PlmPom_BodyType (legacy Plm_* Form Builder txs).
        private const string PomTemplateTxIntegrationId = "TchpPom_Template";
        private const string PomTemplateListSearchIntegrationId = "TchpPom_Template_List";
        private const string PomTemplateFolderSearchIntegrationId = "TchpPom_Template_FolderNav";

        private sealed class PomFolderRemapResult
        {
            public int ReadyToRemapCount { get; set; }
            public int UpdatedCount { get; set; }
            public int UnmappedCount { get; set; }
        }

        private sealed class PlmPomRootFolderRow
        {
            public int FolderId { get; set; }
            public string Name { get; set; }
        }

        private sealed class PomColumnRow
        {
            public string ColumnName { get; set; }
            public int SortOrder { get; set; }
        }

        private sealed class PomRootFolderResolution
        {
            public int? AppRootFolderId { get; set; }
            public string AppRootFolderName { get; set; }
            public List<PlmPomRootFolderPreviewDto> Roots { get; } = new List<PlmPomRootFolderPreviewDto>();
            public List<string> Warnings { get; } = new List<string>();
        }

        public static OperationCallResult<PlmPomImportPreviewDto> PreviewPlmPomImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmPomImportPreviewDto>
            {
                Object = new PlmPomImportPreviewDto()
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

                string tenantConn = GetTenantConnectionString();
                result.Object = BuildPlmPomImportPreview(session.PlmConnectionString.Trim(), tenantConn, sessionId.Value);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Pom_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Pom_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmPomImportExecuteResultDto> ExecutePlmPomImport(PlmPomImportExecuteRequestDto request)
        {
            var result = new OperationCallResult<PlmPomImportExecuteResultDto>
            {
                Object = new PlmPomImportExecuteResultDto()
            };

            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (request == null || !request.SessionId.HasValue || request.SessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, request.SessionId.Value, includeConnection: true);
                if (session == null || string.IsNullOrWhiteSpace(session.PlmConnectionString))
                    throw new InvalidOperationException("PLM connection is not available on this session.");
                if (!session.CompanyId.HasValue || session.CompanyId.Value <= 0)
                    throw new InvalidOperationException("Import session company is not set.");

                string tenantConn = GetTenantConnectionString();
                int? saasApplicationId = request.SaasApplicationId ?? session.SaasApplicationId;
                int tenantDataSourceId = GetTenantDataSourceId();

                result.Object = ExecutePlmPomImportCore(
                    session.PlmConnectionString.Trim(),
                    tenantConn,
                    request.SessionId.Value,
                    session.CompanyId.Value,
                    saasApplicationId,
                    tenantDataSourceId,
                    request.ImportJunctionTables,
                    request.ImportFoldersIfMissing);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Pom_Execute_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else
                {
                    WriteImportLog(fixture, request.SessionId.Value, null, StepPomImport, PlmPomActionImport, "Success",
                        null, null, null, null,
                        $"POM import complete. POM tx {result.Object.PomTransactionId}, POM Template tx {result.Object.PomTemplateTransactionId}.");
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Pom_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static PlmPomImportPreviewDto BuildPlmPomImportPreview(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId)
        {
            var preview = new PlmPomImportPreviewDto();
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                preview.HasBodyPartTable = TemplateTableExists(tenantConn, null, PomBodyPartTableName);
                preview.HasBodyTypeTable = TemplateTableExists(tenantConn, null, PomBodyTypeTableName);
                preview.HasTchpBodyPartTable = TemplateTableExists(tenantConn, null, TchpBodyPartTableName);
                preview.HasTchpPomTemplateTable = TemplateTableExists(tenantConn, null, TchpPomTemplateTableName);
                preview.HasTchpPomTemplatePartTable = TemplateTableExists(tenantConn, null, TchpPomTemplatePartTableName);

                if (!preview.HasBodyPartTable)
                {
                    preview.IsSuccess = false;
                    preview.ErrorMessage = "Tenant Plm_PdmV2kBodyPart was not found. Import PLM BodyPart table first (Entity Import).";
                    return preview;
                }

                if (!preview.HasTchpBodyPartTable || !preview.HasTchpPomTemplateTable || !preview.HasTchpPomTemplatePartTable)
                {
                    preview.IsSuccess = false;
                    preview.ErrorMessage =
                        "Tchp POM schema missing. Run Document/Design/POM_Grading_QC_NewSchema.sql "
                        + "(TchpBodyPart, TchpPomTemplate, TchpPomTemplatePart).";
                    return preview;
                }

                preview.BodyPartRowCount = CountTableRows(tenantConn, PomBodyPartTableName);
                preview.BodyTypeRowCount = preview.HasBodyTypeTable ? CountTableRows(tenantConn, PomBodyTypeTableName) : 0;
                preview.TchpBodyPartRowCount = CountTableRows(tenantConn, TchpBodyPartTableName);
                preview.TchpPomTemplateRowCount = CountTableRows(tenantConn, TchpPomTemplateTableName);
                preview.TchpPomTemplatePartRowCount = CountTableRows(tenantConn, TchpPomTemplatePartTableName);
                preview.HasBodyTypeDetailTable = preview.HasTchpPomTemplatePartTable;
                preview.BodyTypeDetailRowCount = preview.TchpPomTemplatePartRowCount;
                preview.HasSpecBodyPartGradingTable = TemplateTableExists(tenantConn, null, PomSpecGradingTableName);
                preview.SpecBodyPartGradingRowCount = preview.HasSpecBodyPartGradingTable
                    ? CountTableRows(tenantConn, PomSpecGradingTableName)
                    : 0;

                preview.ExistingPomTransactionId = FindTransactionIdByTable(tenantConn, null, PomBodyPartTableName, PomTxIntegrationId);
                preview.ExistingPomListSearchId = GetSearchIdByIntegrationId(tenantConn, null, PomListSearchIntegrationId);
                preview.ExistingPomFolderSearchId = GetSearchIdByIntegrationId(tenantConn, null, PomFolderSearchIntegrationId);
                preview.ExistingPomTemplateTransactionId = FindTransactionIdByTable(
                    tenantConn, null, TchpPomTemplateTableName, PomTemplateTxIntegrationId);
                preview.ExistingPomTemplateListSearchId = GetSearchIdByIntegrationId(tenantConn, null, PomTemplateListSearchIntegrationId);
                preview.ExistingPomTemplateFolderSearchId = GetSearchIdByIntegrationId(tenantConn, null, PomTemplateFolderSearchIntegrationId);

                var pomRemapPreview = PreviewPomFolderIdRemap(tenantConn, null, sessionId, PomBodyPartTableName, PlmPomFolderType);
                preview.PomFolderIdReadyToRemap = pomRemapPreview.ReadyToRemapCount;
                preview.PomFolderIdUnmappedCount = pomRemapPreview.UnmappedCount;
            }

            preview.BodyTypeDetailSourceRowCount = CountPlmSourceTableRows(plmConnectionString, PomBodyTypeDetailSourceTable);
            preview.SpecBodyPartGradingSourceRowCount = CountPlmSourceTableRows(plmConnectionString, PomSpecGradingSourceTable);

            var pomRoot = ResolvePomNavRootFolder(plmConnectionString, tenantConnectionString, sessionId, PlmPomFolderType);
            preview.PlmPomRootFolderCount = pomRoot.Roots.Count;
            preview.PlmPomRootFolders = pomRoot.Roots;
            preview.PomAppRootFolderId = pomRoot.AppRootFolderId;
            preview.PomAppRootFolderName = pomRoot.AppRootFolderName;
            preview.Warnings.AddRange(pomRoot.Warnings);

            var templateRoot = ResolvePomNavRootFolder(plmConnectionString, tenantConnectionString, sessionId, PlmPomTemplateFolderType);
            preview.PlmPomTemplateRootFolderCount = templateRoot.Roots.Count;
            preview.PlmPomTemplateRootFolders = templateRoot.Roots;
            preview.PomTemplateAppRootFolderId = templateRoot.AppRootFolderId;
            preview.PomTemplateAppRootFolderName = templateRoot.AppRootFolderName;
            preview.Warnings.AddRange(templateRoot.Warnings);
            preview.Warnings.Add(
                "POM Template editor uses TchpPomTemplate + TchpPomTemplatePart (no FolderID on Tchp rows; list search only).");

            preview.PlannedActions.Add("Upsert TchpBodyPart / TchpPomTemplate / TchpPomTemplatePart from PLM");
            preview.PlannedActions.Add("Remap FolderID on Plm_PdmV2kBodyPart via AppPlmFolderMap");
            if (preview.PomFolderIdReadyToRemap > 0)
                preview.PlannedActions.Add($"Remap {preview.PomFolderIdReadyToRemap} POM row(s) FolderID via AppPlmFolderMap");
            if (preview.PomFolderIdUnmappedCount > 0)
                preview.Warnings.Add($"Unmapped FolderID on POM: {preview.PomFolderIdUnmappedCount}.");
            preview.PlannedActions.Add("Ensure POM transaction, list search, and folder navigation (FolderType 14 / Plm BodyPart)");
            preview.PlannedActions.Add(
                "Ensure POM Template transaction with TchpPomTemplatePart child (TchpPomTemplate master)");
            if (preview.BodyTypeDetailSourceRowCount > 0)
                preview.PlannedActions.Add($"Import up to {preview.BodyTypeDetailSourceRowCount} Template Part row(s) from PLM BodyTypeDetail");

            preview.IsSuccess = true;
            return preview;
        }

        private static PlmPomImportExecuteResultDto ExecutePlmPomImportCore(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId,
            int companyId,
            int? saasApplicationId,
            int tenantDataSourceId,
            bool importJunctionTables,
            bool importFoldersIfMissing)
        {
            var executeResult = new PlmPomImportExecuteResultDto();
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                if (!TemplateTableExists(tenantConn, null, PomBodyPartTableName))
                    throw new InvalidOperationException("Tenant Plm_PdmV2kBodyPart was not found. Import PLM tables first.");

                RequireTchpPomSchema(tenantConn);

                if (importFoldersIfMissing)
                {
                    executeResult.FoldersImported = ImportPlmFolderTypes(
                        sessionId, companyId, plmConnectionString, tenantConnectionString,
                        new[] { PlmPomFolderType, PlmPomTemplateFolderType }, executeResult.Messages);
                }

                if (importJunctionTables)
                    ImportTchpPomTemplateMasterData(plmConnectionString, tenantConnectionString, executeResult);

                // Tchp tables must be in schema cache before CreateHierarchyTransactionFromTables.
                RefreshTenantTableSchemaCache(tenantDataSourceId);
                AppCacheManagerBL.RefreshOneTableCache(TchpBodyPartTableName, tenantDataSourceId, "dbo");
                AppCacheManagerBL.RefreshOneTableCache(TchpPomTemplateTableName, tenantDataSourceId, "dbo");
                AppCacheManagerBL.RefreshOneTableCache(TchpPomTemplatePartTableName, tenantDataSourceId, "dbo");

                var pomRemap = RemapImportedPomFolderIds(tenantConn, null, sessionId, PomBodyPartTableName, PlmPomFolderType);
                executeResult.PomFolderIdsRemapped = pomRemap.UpdatedCount;
                if (pomRemap.UpdatedCount > 0)
                    executeResult.Messages.Add($"Remapped FolderID on {pomRemap.UpdatedCount} POM row(s) to APP folder IDs.");
                if (pomRemap.UnmappedCount > 0)
                    executeResult.Messages.Add($"Warning: {pomRemap.UnmappedCount} POM row(s) still have unmapped FolderID.");

                var pomRoot = ResolvePomNavRootFolder(plmConnectionString, tenantConnectionString, sessionId, PlmPomFolderType);
                executeResult.Messages.AddRange(pomRoot.Warnings);
                var templateRoot = ResolvePomNavRootFolder(plmConnectionString, tenantConnectionString, sessionId, PlmPomTemplateFolderType);
                executeResult.Messages.AddRange(templateRoot.Warnings);

                int pomTransactionId = EnsurePomBodyPartTransaction(tenantConn, saasApplicationId, tenantDataSourceId, executeResult);
                executeResult.PomTransactionId = pomTransactionId;
                if (pomRoot.AppRootFolderId.HasValue)
                {
                    SetTransactionMgtRootFolderId(tenantConn, null, pomTransactionId, pomRoot.AppRootFolderId.Value);
                    executeResult.PomAppRootFolderId = pomRoot.AppRootFolderId;
                    executeResult.Messages.Add($"POM transaction MgtRootFolderID = {pomRoot.AppRootFolderId.Value}.");
                }

                var pomFormResult = AppDatabaseViewBL.EnsureTransactionDefaultFlexFormLayout(pomTransactionId, migrationFastPath: true, numberOfLayoutColumns: 4);
                if (!pomFormResult.IsSuccessful)
                {
                    throw new InvalidOperationException(pomFormResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? "Failed to generate POM form layout.");
                }

                executeResult.PomFormId = GetTransactionFormId(tenantConn, null, pomTransactionId);
                var pomColumns = LoadTableColumns(tenantConn, null, PomBodyPartTableName);
                executeResult.PomListSearchId = EnsurePomListSearch(
                    tenantConn, saasApplicationId, tenantDataSourceId, pomTransactionId,
                    PomListSearchIntegrationId, "POM Management", PomBodyPartTableName, "BodyPartID", pomColumns, executeResult);
                executeResult.PomFolderSearchId = EnsurePomFolderSearch(
                    tenantConn, saasApplicationId, tenantDataSourceId, pomTransactionId,
                    PomFolderSearchIntegrationId, "POM Management", PomBodyPartTableName, "BodyPartID", pomColumns, executeResult);
                if (pomRoot.AppRootFolderId.HasValue && executeResult.PomFolderSearchId > 0)
                {
                    ConfigurePomFolderNavigation(tenantConn, executeResult.PomFolderSearchId.Value, pomTransactionId, pomRoot.AppRootFolderId.Value, "POM", executeResult);
                }

                int templateTransactionId = EnsurePomTemplateTransaction(tenantConn, saasApplicationId, tenantDataSourceId, executeResult);
                executeResult.PomTemplateTransactionId = templateTransactionId;
                if (templateRoot.AppRootFolderId.HasValue)
                {
                    SetTransactionMgtRootFolderId(tenantConn, null, templateTransactionId, templateRoot.AppRootFolderId.Value);
                    executeResult.PomTemplateAppRootFolderId = templateRoot.AppRootFolderId;
                    executeResult.Messages.Add($"POM Template transaction MgtRootFolderID = {templateRoot.AppRootFolderId.Value}.");
                }

                var templateFormResult = AppDatabaseViewBL.EnsureTransactionDefaultFlexFormLayout(templateTransactionId, migrationFastPath: true, numberOfLayoutColumns: 4);
                if (!templateFormResult.IsSuccessful)
                {
                    throw new InvalidOperationException(templateFormResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? "Failed to generate POM Template form layout.");
                }

                executeResult.PomTemplateFormId = GetTransactionFormId(tenantConn, null, templateTransactionId);
                var templateColumns = LoadTableColumns(tenantConn, null, TchpPomTemplateTableName);
                executeResult.PomTemplateListSearchId = EnsurePomListSearch(
                    tenantConn, saasApplicationId, tenantDataSourceId, templateTransactionId,
                    PomTemplateListSearchIntegrationId, "POM Template Management", TchpPomTemplateTableName, "PomTemplateId", templateColumns, executeResult);
                // TchpPomTemplate has no FolderID — list search only (no folder-nav search).
                executeResult.Messages.Add("Skipped POM Template folder search (TchpPomTemplate has no FolderID).");

                RefreshTenantTableSchemaCache(tenantDataSourceId);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(pomTransactionId);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(templateTransactionId);
                AppCacheManagerBL.RefreshOneTableCache(PomBodyPartTableName, tenantDataSourceId, "dbo");
                AppCacheManagerBL.RefreshOneTableCache(TchpBodyPartTableName, tenantDataSourceId, "dbo");
                AppCacheManagerBL.RefreshOneTableCache(TchpPomTemplateTableName, tenantDataSourceId, "dbo");
                AppCacheManagerBL.RefreshOneTableCache(TchpPomTemplatePartTableName, tenantDataSourceId, "dbo");

                executeResult.IsSuccess = true;
                executeResult.Messages.Add("POM import configuration completed.");
            }

            return executeResult;
        }

        private static PomFolderRemapResult PreviewPomFolderIdRemap(
            SqlConnection tenantConn,
            SqlTransaction tran,
            int sessionId,
            string tableName,
            int plmFolderType)
        {
            var result = new PomFolderRemapResult();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $@"
SELECT
    SUM(CASE WHEN m.AppFolderId IS NOT NULL AND t.FolderID <> m.AppFolderId THEN 1 ELSE 0 END) AS ReadyToRemap,
    SUM(CASE WHEN t.FolderID IS NOT NULL AND m.AppFolderId IS NULL THEN 1 ELSE 0 END) AS Unmapped
FROM dbo.[{tableName}] t
OUTER APPLY (
    SELECT TOP 1 AppFolderId
    FROM dbo.AppPlmFolderMap
    WHERE PlmFolderId = t.FolderID
      AND PlmFolderType = @PlmFolderType
    ORDER BY CASE WHEN SessionId = @SessionId THEN 0 ELSE 1 END, LastSyncAt DESC, MapId DESC
) m;";
                cmd.Parameters.AddWithValue("@PlmFolderType", plmFolderType);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.ReadyToRemapCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                        result.UnmappedCount = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1));
                    }
                }
            }

            return result;
        }

        private static PomFolderRemapResult RemapImportedPomFolderIds(
            SqlConnection tenantConn,
            SqlTransaction tran,
            int sessionId,
            string tableName,
            int plmFolderType)
        {
            var result = RemapImportedPomFolderIdsCore(tenantConn, tran, sessionId, tableName, plmFolderType);
            result.UnmappedCount = CountUnmappedPomFolderIds(tenantConn, tran, sessionId, tableName, plmFolderType);
            return result;
        }

        private static PomFolderRemapResult RemapImportedPomFolderIdsCore(
            SqlConnection tenantConn,
            SqlTransaction tran,
            int sessionId,
            string tableName,
            int plmFolderType)
        {
            var result = new PomFolderRemapResult();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $@"
UPDATE t
SET t.FolderID = m.AppFolderId
FROM dbo.[{tableName}] t
INNER JOIN (
    SELECT PlmFolderId, AppFolderId,
           ROW_NUMBER() OVER (
               PARTITION BY PlmFolderId
               ORDER BY CASE WHEN SessionId = @SessionId THEN 0 ELSE 1 END, LastSyncAt DESC, MapId DESC
           ) AS rn
    FROM dbo.AppPlmFolderMap
    WHERE PlmFolderType = @PlmFolderType
) m ON m.PlmFolderId = t.FolderID AND m.rn = 1
WHERE t.FolderID IS NOT NULL
  AND t.FolderID <> m.AppFolderId;";
                cmd.Parameters.AddWithValue("@PlmFolderType", plmFolderType);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                result.UpdatedCount = cmd.ExecuteNonQuery();
            }

            return result;
        }

        private static int CountUnmappedPomFolderIds(
            SqlConnection tenantConn,
            SqlTransaction tran,
            int sessionId,
            string tableName,
            int plmFolderType)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $@"
SELECT COUNT(1)
FROM dbo.[{tableName}] t
WHERE t.FolderID IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.AppSEFolder f
      WHERE f.FolderID = t.FolderID
  )
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.AppPlmFolderMap m
      WHERE m.PlmFolderId = t.FolderID
        AND m.PlmFolderType = @PlmFolderType
  );";
                cmd.Parameters.AddWithValue("@PlmFolderType", plmFolderType);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static PomRootFolderResolution ResolvePomNavRootFolder(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId,
            int plmFolderType)
        {
            var resolution = new PomRootFolderResolution();
            var plmRoots = ReadPlmPomRootFolders(plmConnectionString, plmFolderType);
            if (plmRoots.Count == 0)
                return resolution;

            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                foreach (var plmRoot in plmRoots)
                {
                    int? appFolderId = ResolveAppFolderIdFromPlmFolder(tenantConn, sessionId, plmRoot.FolderId, plmFolderType);
                    string appFolderName = appFolderId.HasValue ? ReadAppFolderName(tenantConn, appFolderId.Value) : null;
                    resolution.Roots.Add(new PlmPomRootFolderPreviewDto
                    {
                        PlmFolderId = plmRoot.FolderId,
                        PlmFolderName = plmRoot.Name,
                        AppFolderId = appFolderId,
                        AppFolderName = appFolderName
                    });
                    if (!appFolderId.HasValue)
                    {
                        resolution.Warnings.Add(
                            $"PLM folder '{plmRoot.Name}' (FolderID {plmRoot.FolderId}, type {plmFolderType}) is not mapped. Run Folder Import or enable auto-import.");
                    }
                }

                if (plmRoots.Count == 1)
                {
                    var mapped = resolution.Roots.FirstOrDefault();
                    resolution.AppRootFolderId = mapped?.AppFolderId;
                    resolution.AppRootFolderName = mapped?.AppFolderName;
                }
                else if (plmRoots.Count > 1)
                {
                    var firstMapped = resolution.Roots.FirstOrDefault(r => r.AppFolderId.HasValue);
                    resolution.AppRootFolderId = firstMapped?.AppFolderId;
                    resolution.AppRootFolderName = firstMapped?.AppFolderName;
                    resolution.Warnings.Add($"Multiple PLM root folders for type {plmFolderType}; using first mapped folder for navigation root.");
                }
            }

            return resolution;
        }

        private static List<PlmPomRootFolderRow> ReadPlmPomRootFolders(string plmConnectionString, int folderType)
        {
            var rows = new List<PlmPomRootFolderRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT FolderID, Name
FROM dbo.pdmSEFolder
WHERE FolderType = @FolderType
  AND (ParentID IS NULL OR ParentID = 0)
ORDER BY FolderID;";
                    cmd.Parameters.AddWithValue("@FolderType", folderType);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new PlmPomRootFolderRow
                            {
                                FolderId = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? $"Folder_{reader.GetInt32(0)}" : reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private static int? ResolveAppFolderIdFromPlmFolder(SqlConnection tenantConn, int sessionId, int plmFolderId, int plmFolderType)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 AppFolderId
FROM dbo.AppPlmFolderMap
WHERE PlmFolderId = @PlmFolderId
  AND PlmFolderType = @PlmFolderType
ORDER BY CASE WHEN SessionId = @SessionId THEN 0 ELSE 1 END, LastSyncAt DESC, MapId DESC;";
                cmd.Parameters.AddWithValue("@PlmFolderId", plmFolderId);
                cmd.Parameters.AddWithValue("@PlmFolderType", plmFolderType);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int EnsurePomBodyPartTransaction(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            PlmPomImportExecuteResultDto executeResult)
        {
            return EnsureSingleTableTransaction(
                tenantConn, saasApplicationId, tenantDataSourceId,
                PomBodyPartTableName, "POM", PomTxIntegrationId, executeResult);
        }

        private static int EnsurePomTemplateTransaction(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            PlmPomImportExecuteResultDto executeResult)
        {
            RequireTchpPomSchema(tenantConn);

            int? existingId = FindTransactionIdByTable(tenantConn, null, TchpPomTemplateTableName, PomTemplateTxIntegrationId);
            if (existingId.HasValue)
            {
                SetIntegrationId(tenantConn, null, "AppTransaction", "TransactionID", existingId.Value, PomTemplateTxIntegrationId);
                UpdateTransactionShell(tenantConn, existingId.Value, "POM Template", saasApplicationId);

                if (TransactionHasChildTable(tenantConn, existingId.Value, TchpPomTemplatePartTableName))
                {
                    EnsurePomTemplateParentKeyLinks(tenantConn, existingId.Value, executeResult);
                    ApplyPomTemplateChildDisplayNames(tenantConn, existingId.Value);
                    executeResult.Messages.Add($"Reused existing POM Template transaction {existingId.Value}.");
                    return existingId.Value;
                }

                executeResult.Messages.Add(
                    $"Existing POM Template transaction {existingId.Value} is missing {TchpPomTemplatePartTableName} child — repairing.");
                RepairPomTemplateTransactionChildren(
                    tenantConn, existingId.Value, saasApplicationId, tenantDataSourceId, executeResult);
                return existingId.Value;
            }

            int transactionId = CreatePomTemplateHierarchyTransaction(tenantConn, saasApplicationId, tenantDataSourceId);
            SetIntegrationId(tenantConn, null, "AppTransaction", "TransactionID", transactionId, PomTemplateTxIntegrationId);
            EnsurePomTemplateParentKeyLinks(tenantConn, transactionId, executeResult);
            ApplyPomTemplateChildDisplayNames(tenantConn, transactionId);
            executeResult.Messages.Add(
                $"Created POM Template transaction {transactionId} with {TchpPomTemplatePartTableName} child grid.");
            return transactionId;
        }

        private static HierarchyTableSetupDto BuildPomTemplateHierarchySetup(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            string transactionName)
        {
            var childTables = new List<HierarchyChildTableDto>
            {
                new HierarchyChildTableDto
                {
                    TableName = TchpPomTemplatePartTableName,
                    GrandChildTableNames = new List<string>()
                }
            };

            return new HierarchyTableSetupDto
            {
                MasterTableName = TchpPomTemplateTableName,
                TransactionName = transactionName,
                DataSourceRegisterId = tenantDataSourceId,
                SchemaOwner = "dbo",
                SaasApplicationId = saasApplicationId,
                SiblingTableNames = new List<string>(),
                ChildTables = childTables
            };
        }

        private static int CreatePomTemplateHierarchyTransaction(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId)
        {
            var setup = BuildPomTemplateHierarchySetup(tenantConn, saasApplicationId, tenantDataSourceId, "POM Template");
            var saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(
                setup, isIgnoreValidation: true, skipPostSaveCacheSync: true);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to create POM Template transaction.");
            }

            int transactionId = Convert.ToInt32(saveResult.Object.Id);
            if (!TransactionHasChildTable(tenantConn, transactionId, TchpPomTemplatePartTableName))
            {
                throw new InvalidOperationException(
                    $"POM Template transaction {transactionId} was created without {TchpPomTemplatePartTableName} child. "
                    + "Schema cache likely still missing the table after import.");
            }

            return transactionId;
        }

        private static void RepairPomTemplateTransactionChildren(
            SqlConnection tenantConn,
            int existingTransactionId,
            int? saasApplicationId,
            int tenantDataSourceId,
            PlmPomImportExecuteResultDto executeResult)
        {
            int? existingRootId = GetRootTransactionUnitId(tenantConn, existingTransactionId);
            if (!existingRootId.HasValue)
                throw new InvalidOperationException($"POM Template transaction {existingTransactionId} has no root unit.");

            // Drop any incorrect legacy children (e.g. Plm_pdmV2kBodyTypeDetail) before attaching Tchp parts.
            DeleteNonRootTransactionUnits(tenantConn, existingTransactionId, existingRootId.Value);

            var setup = BuildPomTemplateHierarchySetup(
                tenantConn, saasApplicationId, tenantDataSourceId, "POM Template __repair_tmp");
            var saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(
                setup, isIgnoreValidation: true, skipPostSaveCacheSync: true);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to build temporary POM Template hierarchy for repair.");
            }

            int tempTxId = Convert.ToInt32(saveResult.Object.Id);
            int? tempRootId = GetRootTransactionUnitId(tenantConn, tempTxId);
            if (!tempRootId.HasValue)
                throw new InvalidOperationException($"Temporary POM Template transaction {tempTxId} has no root unit.");

            if (!TransactionHasChildTable(tenantConn, tempTxId, TchpPomTemplatePartTableName))
            {
                DeletePomTemplateTransactionShell(tenantConn, tempTxId);
                throw new InvalidOperationException(
                    $"Cannot repair POM Template: temporary hierarchy still missing {TchpPomTemplatePartTableName}. Refresh schema cache and retry.");
            }

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET TransactionID = @ExistingTxId,
    ParentTransactionUnitID = CASE
        WHEN ParentTransactionUnitID = @TempRootId THEN @ExistingRootId
        ELSE ParentTransactionUnitID
    END
WHERE TransactionID = @TempTxId
  AND TransactionUnitID <> @TempRootId;";
                cmd.Parameters.AddWithValue("@ExistingTxId", existingTransactionId);
                cmd.Parameters.AddWithValue("@ExistingRootId", existingRootId.Value);
                cmd.Parameters.AddWithValue("@TempTxId", tempTxId);
                cmd.Parameters.AddWithValue("@TempRootId", tempRootId.Value);
                int moved = cmd.ExecuteNonQuery();
                executeResult.Messages.Add($"Moved {moved} child unit(s) onto POM Template transaction {existingTransactionId}.");
            }

            DeletePomTemplateTransactionShell(tenantConn, tempTxId);
            EnsurePomTemplateParentKeyLinks(tenantConn, existingTransactionId, executeResult);
            ApplyPomTemplateChildDisplayNames(tenantConn, existingTransactionId);

            int? formId = GetTransactionFormId(tenantConn, null, existingTransactionId);
            if (formId.HasValue)
            {
                ClearPomFormLayoutItems(tenantConn, formId.Value);
                executeResult.Messages.Add(
                    $"Cleared form layout {formId.Value} so Template Part grid can be regenerated.");
            }
        }

        private static void DeleteNonRootTransactionUnits(SqlConnection tenantConn, int transactionId, int rootUnitId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
DELETE f
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.TransactionUnitID <> @RootUnitId;

DELETE FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId
  AND TransactionUnitID <> @RootUnitId;";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@RootUnitId", rootUnitId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void DeletePomTemplateTransactionShell(SqlConnection tenantConn, int transactionId)
        {
            int? formId = GetTransactionFormId(tenantConn, null, transactionId);
            if (formId.HasValue)
                ClearPomFormLayoutItems(tenantConn, formId.Value);

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransaction SET FormID = NULL WHERE TransactionID = @TxId;

DELETE f
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId;

DELETE FROM dbo.AppTransactionUnit WHERE TransactionID = @TxId;

IF @FormId IS NOT NULL
    DELETE FROM dbo.AppForm WHERE FormID = @FormId;

DELETE FROM dbo.AppTransaction WHERE TransactionID = @TxId;";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@FormId", (object)formId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ClearPomFormLayoutItems(SqlConnection tenantConn, int formId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppFormGridLayoutItemBindField')
BEGIN
    DELETE b
    FROM dbo.AppFormGridLayoutItemBindField b
    INNER JOIN dbo.AppFormLayoutItem i ON i.FormLayoutItemID = b.FormLayoutId
    WHERE i.FormID = @FormId;
END

UPDATE dbo.AppTransactionField
SET HostFormLayoutItemId = NULL
WHERE HostFormLayoutItemId IN (
    SELECT FormLayoutItemID FROM dbo.AppFormLayoutItem WHERE FormID = @FormId
);

DECLARE @guard int = 0;
WHILE @guard < 50 AND EXISTS (SELECT 1 FROM dbo.AppFormLayoutItem WHERE FormID = @FormId)
BEGIN
    SET @guard = @guard + 1;
    DELETE FROM dbo.AppFormLayoutItem
    WHERE FormID = @FormId
      AND FormLayoutItemID NOT IN (
          SELECT DISTINCT UigridLayoutParentId
          FROM dbo.AppFormLayoutItem
          WHERE FormID = @FormId
            AND UigridLayoutParentId IS NOT NULL
      );
END";
                cmd.Parameters.AddWithValue("@FormId", formId);
                cmd.ExecuteNonQuery();
            }
        }

        private static bool TransactionHasChildTable(SqlConnection tenantConn, int transactionId, string childTableName)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 1
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId
  AND DataBaseTableName = @TableName
  AND ParentTransactionUnitID IS NOT NULL
  AND ParentTransactionUnitID <> 0;";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", childTableName);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void EnsurePomTemplateParentKeyLinks(
            SqlConnection tenantConn,
            int transactionId,
            PlmPomImportExecuteResultDto executeResult)
        {
            // Tchp schema has real FKs, but still set LinkToParent explicitly for Form Builder runtime.
            int? rootUnitId = GetRootTransactionUnitId(tenantConn, transactionId);
            int? partUnitId = GetTransactionUnitIdByTableName(tenantConn, transactionId, TchpPomTemplatePartTableName);
            if (!rootUnitId.HasValue || !partUnitId.HasValue)
                return;

            EnsureChildUnitParent(tenantConn, partUnitId.Value, rootUnitId.Value);

            int? templatePkFieldId = ResolveTransactionFieldId(tenantConn, transactionId, rootUnitId, "PomTemplateId");
            if (!templatePkFieldId.HasValue)
                return;

            int updated = SetChildFieldLinkToParentPrimaryKey(
                tenantConn, partUnitId.Value, "PomTemplateId", templatePkFieldId.Value);
            if (updated > 0)
                executeResult.Messages.Add("Linked TchpPomTemplatePart.PomTemplateId → TchpPomTemplate.PomTemplateId.");
        }

        private static void ApplyPomTemplateChildDisplayNames(SqlConnection tenantConn, int transactionId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET UnitDisplayName = N'Template Parts'
WHERE TransactionID = @TxId
  AND DataBaseTableName = @PartTable;";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@PartTable", TchpPomTemplatePartTableName);
                cmd.ExecuteNonQuery();
            }
        }

        private static int EnsureSingleTableTransaction(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            string tableName,
            string transactionName,
            string integrationId,
            PlmPomImportExecuteResultDto executeResult)
        {
            int? existingId = FindTransactionIdByTable(tenantConn, null, tableName, integrationId);
            if (existingId.HasValue)
            {
                SetIntegrationId(tenantConn, null, "AppTransaction", "TransactionID", existingId.Value, integrationId);
                UpdateTransactionShell(tenantConn, existingId.Value, transactionName, saasApplicationId);
                executeResult.Messages.Add($"Reused existing {transactionName} transaction {existingId.Value}.");
                return existingId.Value;
            }

            var setup = new HierarchyTableSetupDto
            {
                MasterTableName = tableName,
                TransactionName = transactionName,
                DataSourceRegisterId = tenantDataSourceId,
                SchemaOwner = "dbo",
                SaasApplicationId = saasApplicationId,
                SiblingTableNames = new List<string>(),
                ChildTables = new List<HierarchyChildTableDto>()
            };

            var saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(setup, isIgnoreValidation: true, skipPostSaveCacheSync: true);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to create {transactionName} transaction.");
            }

            int transactionId = Convert.ToInt32(saveResult.Object.Id);
            SetIntegrationId(tenantConn, null, "AppTransaction", "TransactionID", transactionId, integrationId);
            executeResult.Messages.Add($"Created {transactionName} transaction {transactionId}.");
            return transactionId;
        }

        private static void UpdateTransactionShell(SqlConnection tenantConn, int transactionId, string name, int? saasApplicationId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransaction
SET TransactionName = @Name, Description = @Description, SaasApplicationID = @SaasApplicationId
WHERE TransactionID = @TransactionId";
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Description", name);
                cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? FindTransactionIdByTable(SqlConnection tenantConn, SqlTransaction tran, string tableName, string integrationId)
        {
            int? byIntegration = GetTransactionIdByIntegrationId(tenantConn, tran, integrationId);
            if (byIntegration.HasValue)
                return byIntegration;

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 t.TransactionID
FROM dbo.AppTransaction t
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionID = t.TransactionID
WHERE u.DataBaseTableName = @TableName
  AND u.ParentTransactionUnitID IS NULL
ORDER BY t.TransactionID;";
                cmd.Parameters.AddWithValue("@TableName", tableName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int EnsurePomListSearch(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            int transactionId,
            string integrationId,
            string searchName,
            string tableName,
            string rootIdColumn,
            List<PomColumnRow> columns,
            PlmPomImportExecuteResultDto executeResult)
        {
            int searchId = EnsureSearchShell(
                tenantConn, integrationId, searchName, (int)EmAppSearchUsageType.Management, saasApplicationId, autoExecute: true);

            string dataSetQuery = $"SELECT * FROM [dbo].[{tableName}]";
            int dataSetId = SaveSearchDataSet(searchId, searchName, dataSetQuery, tenantDataSourceId, saasApplicationId);
            var viewFields = BuildPomListViewFields(columns, rootIdColumn);
            int searchViewId = SaveSearchView(searchId, searchName, dataSetId, viewFields, gridOutputMode: 1);

            int? rootFieldId = GetSearchViewFieldId(tenantConn, null, searchViewId, rootIdColumn);
            if (!rootFieldId.HasValue)
                throw new InvalidOperationException($"{searchName} list view is missing {rootIdColumn} field.");

            ClearSearchViewFormLinkTargets(tenantConn, searchViewId);
            InsertPomFormLinkTarget(tenantConn, searchViewId, "Create", (int)EmAppLinkTargetActionType.Create, transactionId, rootFieldId.Value, rootIdColumn, 1);
            InsertPomFormLinkTarget(tenantConn, searchViewId, "Open", (int)EmAppLinkTargetActionType.Edit, transactionId, rootFieldId.Value, rootIdColumn, 2);
            InsertPomFormLinkTarget(tenantConn, searchViewId, "Delete", (int)EmAppLinkTargetActionType.Delete, transactionId, rootFieldId.Value, rootIdColumn, 3);

            var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(searchId, saasApplicationId, searchName, searchName);
            if (!menuResult.IsSuccessful && menuResult.ValidationResult?.HasErrors == true)
                executeResult.Messages.Add(menuResult.ValidationResult.Items?.FirstOrDefault()?.Message ?? $"{searchName} menu update failed.");
            else
                executeResult.Messages.Add($"{searchName} list search {searchId} configured.");

            EnsureTransactionQuickSearchNavigation(tenantConn, transactionId, searchId);
            return searchId;
        }

        private static int EnsurePomFolderSearch(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            int transactionId,
            string integrationId,
            string searchName,
            string tableName,
            string rootIdColumn,
            List<PomColumnRow> columns,
            PlmPomImportExecuteResultDto executeResult)
        {
            int searchId = EnsureSearchShell(
                tenantConn, integrationId, searchName, (int)EmAppSearchUsageType.DataModelTemplate, saasApplicationId, autoExecute: false);

            string dataSetQuery = $"SELECT * FROM [dbo].[{tableName}]";
            int dataSetId = SaveSearchDataSet(searchId, searchName, dataSetQuery, tenantDataSourceId, saasApplicationId);
            var viewFields = BuildPomFolderViewFields(columns, rootIdColumn);
            int searchViewId = SaveSearchView(searchId, searchName, dataSetId, viewFields, gridOutputMode: 1);

            int? rootFieldId = GetSearchViewFieldId(tenantConn, null, searchViewId, rootIdColumn);
            if (!rootFieldId.HasValue)
                throw new InvalidOperationException($"{searchName} folder view is missing {rootIdColumn} field.");

            ClearSearchViewFormLinkTargets(tenantConn, searchViewId);
            InsertPomFormLinkTarget(tenantConn, searchViewId, "Editor", (int)EmAppLinkTargetActionType.Edit, transactionId, rootFieldId.Value, rootIdColumn, 1);
            executeResult.Messages.Add($"{searchName} folder template search {searchId} configured.");
            return searchId;
        }

        private static void ConfigurePomFolderNavigation(
            SqlConnection tenantConn,
            int folderTemplateSearchId,
            int transactionId,
            int rootFolderId,
            string label,
            PlmPomImportExecuteResultDto executeResult)
        {
            int? searchViewId = GetSearchViewIdForSearch(tenantConn, folderTemplateSearchId);
            var config = new TemplateFolderNavigationConfigDto
            {
                TemplateSearchId = folderTemplateSearchId,
                HostTransactionId = transactionId,
                RootFolderId = rootFolderId,
                SearchViewId = searchViewId,
                IsEnableFolderSecurity = false
            };

            var saveResult = AppTransactionNavigationBL.SaveTemplateFolderNavigationConfig(config);
            if (!saveResult.IsSuccessful)
            {
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to configure {label} folder navigation.");
            }

            executeResult.Messages.Add($"{label} folder navigation configured with root folder {rootFolderId}.");
        }

        private static List<PomColumnRow> LoadTableColumns(SqlConnection tenantConn, SqlTransaction tran, string tableName)
        {
            var rows = new List<PomColumnRow>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT c.COLUMN_NAME, c.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = @TableName
ORDER BY c.ORDINAL_POSITION;";
                cmd.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new PomColumnRow
                        {
                            ColumnName = reader.GetString(0),
                            SortOrder = reader.GetInt32(1)
                        });
                    }
                }
            }

            return rows;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildPomListViewFields(List<PomColumnRow> columns, string rootIdColumn)
        {
            var fields = new ObservableSet<AppSearchViewFieldExDto>();
            int sort = 10;
            foreach (var column in columns)
            {
                var field = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = true,
                    SysTableFiledPath = column.ColumnName,
                    DisplayText = column.ColumnName,
                    ControlType = ResolvePomColumnControlType(column.ColumnName),
                    Sort = sort
                };
                if (string.Equals(column.ColumnName, rootIdColumn, StringComparison.OrdinalIgnoreCase))
                    field.IsTransRootId = true;
                fields.Add(field);
                sort += 10;
            }

            return fields;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildPomFolderViewFields(List<PomColumnRow> columns, string rootIdColumn)
        {
            var fields = new ObservableSet<AppSearchViewFieldExDto>();
            int sort = 1;
            var preferred = new[] { rootIdColumn, "Code", "BodyPartName", "BodyTypeName", "Description", "Tolerance", "FolderID" };
            foreach (var columnName in preferred)
            {
                if (!columns.Any(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var field = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = !string.Equals(columnName, "FolderID", StringComparison.OrdinalIgnoreCase),
                    SysTableFiledPath = columnName,
                    DisplayText = columnName,
                    ControlType = ResolvePomColumnControlType(columnName),
                    Sort = sort++
                };
                if (string.Equals(columnName, rootIdColumn, StringComparison.OrdinalIgnoreCase))
                    field.IsTransRootId = true;
                if (string.Equals(columnName, "FolderID", StringComparison.OrdinalIgnoreCase))
                    field.IsFileFoderId = true;
                fields.Add(field);
            }

            if (!fields.Any(f => string.Equals(f.SysTableFiledPath, "FolderID", StringComparison.OrdinalIgnoreCase))
                && columns.Any(c => string.Equals(c.ColumnName, "FolderID", StringComparison.OrdinalIgnoreCase)))
            {
                fields.Add(new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = false,
                    SysTableFiledPath = "FolderID",
                    DisplayText = "FolderID",
                    ControlType = (int)EmAppControlType.TextBox,
                    Sort = sort,
                    IsFileFoderId = true
                });
            }

            return fields;
        }

        private static int ResolvePomColumnControlType(string columnName)
        {
            if (string.Equals(columnName, "IsHight", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "IsImport", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "IsNeedToApplyGradingRule", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "CriticalPoint", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppControlType.CheckBox;
            return (int)EmAppControlType.TextBox;
        }

        private static void InsertPomFormLinkTarget(
            SqlConnection tenantConn,
            int searchViewId,
            string navigationActionName,
            int actionType,
            int linkTargetTransactionId,
            int sourceViewColumnId,
            string targetColumn,
            int sort)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppFormLinkTarget (
    SearchViewID,
    NavigationActionName,
    ActionType,
    LinkTargetTransactionID,
    LinkTargetUsageType,
    SourceColumnType,
    SourceViewColumnID1,
    TargetColumn1,
    Sort,
    IsPopup,
    PopupWidth,
    PopupHeight)
VALUES (
    @SearchViewId,
    @NavigationActionName,
    @ActionType,
    @LinkTargetTransactionId,
    @LinkTargetUsageType,
    @SourceColumnType,
    @SourceViewColumnId1,
    @TargetColumn1,
    @Sort,
    @IsPopup,
    @PopupWidth,
    @PopupHeight)";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@NavigationActionName", navigationActionName);
                cmd.Parameters.AddWithValue("@ActionType", actionType);
                cmd.Parameters.AddWithValue("@LinkTargetTransactionId", linkTargetTransactionId);
                cmd.Parameters.AddWithValue("@LinkTargetUsageType", (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);
                cmd.Parameters.AddWithValue("@SourceColumnType", (int)EmAppLinkTargetSourceColumnType.SearchViewField);
                cmd.Parameters.AddWithValue("@SourceViewColumnId1", sourceViewColumnId);
                cmd.Parameters.AddWithValue("@TargetColumn1", targetColumn);
                cmd.Parameters.AddWithValue("@Sort", sort);
                cmd.Parameters.AddWithValue("@IsPopup", true);
                cmd.Parameters.AddWithValue("@PopupWidth", 1200);
                cmd.Parameters.AddWithValue("@PopupHeight", 700);
                cmd.ExecuteNonQuery();
            }
        }

        private static int CountPlmSourceTableRows(string plmConnectionString, string tableName)
        {
            try
            {
                using (var conn = new SqlConnection(plmConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $@"
IF OBJECT_ID('dbo.{tableName}', 'U') IS NOT NULL
    SELECT COUNT(1) FROM dbo.[{tableName}]
ELSE
    SELECT 0;";
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}
