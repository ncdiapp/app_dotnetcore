using System;
using System.Collections.Generic;
using System.Data;
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
        public const string StepColorImport = "ColorImport";
        public const string JobTypePlmColorImport = "PlmColorImport";
        private const string PlmColorActionPreview = "PlmColorPreview";
        private const string PlmColorActionImport = "PlmColorImport";

        private const string ColorRgbTableName = "Plm_pdmRGBColor";
        private const string ColorRootFolderName = "Color Management";
        private const int PlmColorFolderType = 2;

        private const string ColorTxIntegrationId = "PlmColor_RGB";
        private const string ColorListSearchIntegrationId = "PlmColor_RGB_List";
        private const string ColorFolderTemplateSearchIntegrationId = "PlmColor_RGB_FolderTemplate";
        private const string ColorAppFolderIdFieldName = "AppFolderId";

        private const string ColorRootStrategyNone = "NoPlmColorRoots";
        private const string ColorRootStrategySingle = "SinglePlmColorRoot";
        private const string ColorRootStrategyMulti = "MultiPlmColorRootWrapper";

        private sealed class PlmColorRootFolderRow
        {
            public int FolderId { get; set; }
            public string Name { get; set; }
        }

        private sealed class ColorRootFolderResolution
        {
            public string Strategy { get; set; }
            public int? AppRootFolderId { get; set; }
            public string AppRootFolderName { get; set; }
            public List<PlmColorRootFolderPreviewDto> Roots { get; } = new List<PlmColorRootFolderPreviewDto>();
            public List<string> Warnings { get; } = new List<string>();
        }

        private sealed class ColorColumnRow
        {
            public string ColumnName { get; set; }
            public int SortOrder { get; set; }
        }

        public static OperationCallResult<PlmColorImportPreviewDto> PreviewPlmColorImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmColorImportPreviewDto>
            {
                Object = new PlmColorImportPreviewDto()
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
                result.Object = BuildPlmColorImportPreview(session.PlmConnectionString.Trim(), tenantConn, sessionId.Value);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Color_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Color_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmColorImportExecuteResultDto> ExecutePlmColorImport(PlmColorImportExecuteRequestDto request)
        {
            var result = new OperationCallResult<PlmColorImportExecuteResultDto>
            {
                Object = new PlmColorImportExecuteResultDto()
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

                string tenantConn = GetTenantConnectionString();
                int? saasApplicationId = request.SaasApplicationId ?? session.SaasApplicationId;
                int tenantDataSourceId = GetTenantDataSourceId();

                result.Object = ExecutePlmColorImportCore(
                    session.PlmConnectionString.Trim(),
                    tenantConn,
                    request.SessionId.Value,
                    saasApplicationId,
                    tenantDataSourceId);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Color_Execute_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else
                {
                    WriteImportLog(fixture, request.SessionId.Value, null, StepColorImport, PlmColorActionImport, "Success",
                        null, null, null, null,
                        $"Color import complete. Transaction {result.Object.TransactionId}, list search {result.Object.ListSearchId}, folder template {result.Object.FolderTemplateSearchId}.");
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Color_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static PlmColorImportPreviewDto BuildPlmColorImportPreview(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId)
        {
            var preview = new PlmColorImportPreviewDto();
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                preview.HasRgbColorTable = TemplateTableExists(tenantConn, null, ColorRgbTableName);
                if (!preview.HasRgbColorTable)
                {
                    preview.IsSuccess = false;
                    preview.ErrorMessage = $"Tenant table dbo.{ColorRgbTableName} was not found. Import PLM tables first.";
                    return preview;
                }

                preview.RgbColorRowCount = CountTableRows(tenantConn, ColorRgbTableName);
                preview.ColorGroupDetailRowCount = TableExists(tenantConn, ColorGroupDetailTable)
                    ? CountTableRows(tenantConn, ColorGroupDetailTable)
                    : 0;

                preview.ExistingTransactionId = FindRgbColorTransactionId(tenantConn, null);
                preview.ExistingListSearchId = GetSearchIdByIntegrationId(tenantConn, null, ColorListSearchIntegrationId);
                preview.ExistingFolderTemplateSearchId = GetSearchIdByIntegrationId(tenantConn, null, ColorFolderTemplateSearchIntegrationId);
            }

            var rootResolution = ResolveColorNavRootFolder(plmConnectionString, tenantConnectionString, sessionId, applyChanges: false);
            preview.PlmColorRootFolderCount = rootResolution.Roots.Count;
            preview.RootFolderStrategy = rootResolution.Strategy;
            preview.ResolvedAppRootFolderId = rootResolution.AppRootFolderId;
            preview.ResolvedAppRootFolderName = rootResolution.AppRootFolderName;
            preview.PlmColorRootFolders = rootResolution.Roots;
            preview.Warnings.AddRange(rootResolution.Warnings);

            preview.PlannedActions.Add("Ensure RGB Color transaction and form");
            preview.PlannedActions.Add("Create/update RGB Color list search (MasterDataManagement menu)");
            preview.PlannedActions.Add("Create/update Color Management folder search template and folder navigation menu");
            if (preview.PlmColorRootFolderCount == 0)
                preview.PlannedActions.Add("Skip folder root setup (no PLM color root folders)");
            else if (string.Equals(rootResolution.Strategy, ColorRootStrategySingle, StringComparison.Ordinal))
                preview.PlannedActions.Add($"Use mapped APP folder {rootResolution.AppRootFolderId} as folder navigation root");
            else if (string.Equals(rootResolution.Strategy, ColorRootStrategyMulti, StringComparison.Ordinal))
                preview.PlannedActions.Add($"Ensure wrapper folder '{ColorRootFolderName}' and reparent imported color roots under it");

            preview.IsSuccess = true;
            return preview;
        }

        private static PlmColorImportExecuteResultDto ExecutePlmColorImportCore(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId,
            int? saasApplicationId,
            int tenantDataSourceId)
        {
            var executeResult = new PlmColorImportExecuteResultDto();
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                if (!TemplateTableExists(tenantConn, null, ColorRgbTableName))
                    throw new InvalidOperationException($"Tenant table dbo.{ColorRgbTableName} was not found.");

                var rootResolution = ResolveColorNavRootFolder(plmConnectionString, tenantConnectionString, sessionId, applyChanges: true);
                executeResult.RootFolderStrategy = rootResolution.Strategy;
                executeResult.AppRootFolderId = rootResolution.AppRootFolderId;
                executeResult.Messages.AddRange(rootResolution.Warnings);

                int transactionId = EnsureRgbColorTransaction(tenantConn, saasApplicationId, tenantDataSourceId, executeResult);
                executeResult.TransactionId = transactionId;

                if (rootResolution.AppRootFolderId.HasValue)
                {
                    SetTransactionMgtRootFolderId(tenantConn, null, transactionId, rootResolution.AppRootFolderId.Value);
                    executeResult.Messages.Add($"Set transaction MgtRootFolderID = {rootResolution.AppRootFolderId.Value}.");
                }

                var formResult = AppDatabaseViewBL.EnsureTransactionDefaultFlexFormLayout(transactionId, migrationFastPath: true, numberOfLayoutColumns: 4);
                if (!formResult.IsSuccessful)
                {
                    throw new InvalidOperationException(formResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? "Failed to generate RGB Color form layout.");
                }

                executeResult.FormId = GetTransactionFormId(tenantConn, null, transactionId);
                var columns = LoadRgbColorColumns(tenantConn, null);

                int listSearchId = EnsureRgbColorListSearch(tenantConn, saasApplicationId, tenantDataSourceId, transactionId, columns, executeResult);
                executeResult.ListSearchId = listSearchId;

                int folderTemplateSearchId = EnsureRgbColorFolderTemplateSearch(
                    tenantConn, saasApplicationId, tenantDataSourceId, transactionId, columns, executeResult);
                executeResult.FolderTemplateSearchId = folderTemplateSearchId;

                if (rootResolution.AppRootFolderId.HasValue && folderTemplateSearchId > 0)
                {
                    ConfigureColorFolderNavigation(tenantConn, folderTemplateSearchId, transactionId, rootResolution.AppRootFolderId.Value, executeResult);
                }

                RefreshTenantTableSchemaCache(tenantDataSourceId);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                AppCacheManagerBL.RefreshOneTableCache(ColorRgbTableName, tenantDataSourceId, "dbo");

                executeResult.IsSuccess = true;
                executeResult.Messages.Add("Color import configuration completed.");
            }

            return executeResult;
        }

        private static ColorRootFolderResolution ResolveColorNavRootFolder(
            string plmConnectionString,
            string tenantConnectionString,
            int sessionId,
            bool applyChanges)
        {
            var resolution = new ColorRootFolderResolution { Strategy = ColorRootStrategyNone };
            var plmRoots = ReadPlmColorRootFolders(plmConnectionString);
            if (plmRoots.Count == 0)
                return resolution;

            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();
                foreach (var plmRoot in plmRoots)
                {
                    int? appFolderId = ResolveAppFolderIdFromPlmFolder(tenantConn, sessionId, plmRoot.FolderId);
                    string appFolderName = appFolderId.HasValue ? ReadAppFolderName(tenantConn, appFolderId.Value) : null;
                    resolution.Roots.Add(new PlmColorRootFolderPreviewDto
                    {
                        PlmFolderId = plmRoot.FolderId,
                        PlmFolderName = plmRoot.Name,
                        AppFolderId = appFolderId,
                        AppFolderName = appFolderName
                    });
                    if (!appFolderId.HasValue)
                    {
                        resolution.Warnings.Add(
                            $"PLM color root '{plmRoot.Name}' (FolderID {plmRoot.FolderId}) is not mapped in AppPlmFolderMap. Run Folder Import first.");
                    }
                }

                if (plmRoots.Count == 1)
                {
                    resolution.Strategy = ColorRootStrategySingle;
                    var mapped = resolution.Roots.FirstOrDefault();
                    resolution.AppRootFolderId = mapped?.AppFolderId;
                    resolution.AppRootFolderName = mapped?.AppFolderName;
                    if (!resolution.AppRootFolderId.HasValue)
                        resolution.Warnings.Add("Single PLM color root exists but APP mapping is missing; folder navigation root will not be set.");
                    return resolution;
                }

                resolution.Strategy = ColorRootStrategyMulti;
                int wrapperFolderId = applyChanges
                    ? EnsureColorManagementWrapperFolder(tenantConn)
                    : FindColorManagementWrapperFolderId(tenantConn);

                if (!applyChanges && wrapperFolderId <= 0)
                {
                    resolution.Warnings.Add($"Wrapper folder '{ColorRootFolderName}' will be created during execute.");
                }

                if (applyChanges)
                {
                    wrapperFolderId = EnsureColorManagementWrapperFolder(tenantConn);
                    foreach (var root in resolution.Roots.Where(r => r.AppFolderId.HasValue))
                    {
                        ReparentAppFolder(tenantConn, root.AppFolderId.Value, wrapperFolderId);
                    }
                }

                resolution.AppRootFolderId = wrapperFolderId > 0 ? wrapperFolderId : (int?)null;
                resolution.AppRootFolderName = wrapperFolderId > 0 ? ReadAppFolderName(tenantConn, wrapperFolderId) : ColorRootFolderName;
                return resolution;
            }
        }

        private static List<PlmColorRootFolderRow> ReadPlmColorRootFolders(string plmConnectionString)
        {
            var rows = new List<PlmColorRootFolderRow>();
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
                    cmd.Parameters.AddWithValue("@FolderType", PlmColorFolderType);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new PlmColorRootFolderRow
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

        private static int? ResolveAppFolderIdFromPlmFolder(SqlConnection tenantConn, int sessionId, int plmFolderId)
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
                cmd.Parameters.AddWithValue("@PlmFolderType", PlmColorFolderType);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int FindColorManagementWrapperFolderId(SqlConnection tenantConn)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 FolderID
FROM dbo.AppSEFolder
WHERE Name = @Name AND (ParentID IS NULL OR ParentID = 0)
ORDER BY FolderID;";
                cmd.Parameters.AddWithValue("@Name", ColorRootFolderName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? 0 : Convert.ToInt32(val);
            }
        }

        private static int EnsureColorManagementWrapperFolder(SqlConnection tenantConn)
        {
            int existingId = FindColorManagementWrapperFolderId(tenantConn);
            if (existingId > 0)
                return existingId;

            int? userId = AppSecurityUserBL.CurrentUserId;
            object companyId = APP.Framework.ServerContext.Instance.CurrentCompanyId;
            var now = DateTime.UtcNow;
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppSEFolder
    (FolderType, Name, Description, ParentID, IsSystemFolder, TransactionID,
     AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID, AppCreatedByCompanyID)
VALUES
    (1, @Name, @Description, NULL, 0, NULL,
     @UserId, @CreatedDate, @ModifiedDate, @UserId, @CompanyId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                cmd.Parameters.AddWithValue("@Name", ColorRootFolderName);
                cmd.Parameters.AddWithValue("@Description", "PLM color folder navigation root");
                cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedDate", now);
                cmd.Parameters.AddWithValue("@ModifiedDate", now);
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void ReparentAppFolder(SqlConnection tenantConn, int appFolderId, int parentFolderId)
        {
            if (appFolderId == parentFolderId)
                return;

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppSEFolder
SET ParentID = @ParentId,
    AppModifiedDate = @ModifiedDate
WHERE FolderID = @FolderId
  AND ISNULL(ParentID, -1) <> @ParentId;";
                cmd.Parameters.AddWithValue("@ParentId", parentFolderId);
                cmd.Parameters.AddWithValue("@FolderId", appFolderId);
                cmd.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);
                cmd.ExecuteNonQuery();
            }
        }

        private static string ReadAppFolderName(SqlConnection tenantConn, int folderId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT Name FROM dbo.AppSEFolder WHERE FolderID = @FolderId";
                cmd.Parameters.AddWithValue("@FolderId", folderId);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static int EnsureRgbColorTransaction(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            PlmColorImportExecuteResultDto executeResult)
        {
            int? existingId = FindRgbColorTransactionId(tenantConn, null);
            if (existingId.HasValue)
            {
                SetIntegrationId(tenantConn, null, "AppTransaction", "TransactionID", existingId.Value, ColorTxIntegrationId);
                using (var cmd = tenantConn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppTransaction
SET TransactionName = @Name, Description = @Description, SaasApplicationID = @SaasApplicationId
WHERE TransactionID = @TransactionId";
                    cmd.Parameters.AddWithValue("@Name", "RGB Color");
                    cmd.Parameters.AddWithValue("@Description", "RGB Color");
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TransactionId", existingId.Value);
                    cmd.ExecuteNonQuery();
                }

                executeResult.Messages.Add($"Reused existing RGB Color transaction {existingId.Value}.");
                return existingId.Value;
            }

            var setup = new HierarchyTableSetupDto
            {
                MasterTableName = ColorRgbTableName,
                TransactionName = "RGB Color",
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
                    ?? "Failed to create RGB Color transaction.");
            }

            int transactionId = Convert.ToInt32(saveResult.Object.Id);
            SetIntegrationId(tenantConn, null, "AppTransaction", "TransactionID", transactionId, ColorTxIntegrationId);
            executeResult.Messages.Add($"Created RGB Color transaction {transactionId}.");
            return transactionId;
        }

        private static int? FindRgbColorTransactionId(SqlConnection tenantConn, SqlTransaction tran)
        {
            int? byIntegration = GetTransactionIdByIntegrationId(tenantConn, tran, ColorTxIntegrationId);
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
                cmd.Parameters.AddWithValue("@TableName", ColorRgbTableName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void SetTransactionMgtRootFolderId(SqlConnection tenantConn, SqlTransaction tran, int transactionId, int rootFolderId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
UPDATE dbo.AppTransaction
SET MgtRootFolderID = @RootFolderId
WHERE TransactionID = @TransactionId";
                cmd.Parameters.AddWithValue("@RootFolderId", rootFolderId);
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? GetTransactionFormId(SqlConnection tenantConn, SqlTransaction tran, int transactionId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT FormID FROM dbo.AppTransaction WHERE TransactionID = @TransactionId";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static List<ColorColumnRow> LoadRgbColorColumns(SqlConnection tenantConn, SqlTransaction tran)
        {
            var rows = new List<ColorColumnRow>();
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT c.COLUMN_NAME, c.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = @TableName
ORDER BY c.ORDINAL_POSITION;";
                cmd.Parameters.AddWithValue("@TableName", ColorRgbTableName);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new ColorColumnRow
                        {
                            ColumnName = reader.GetString(0),
                            SortOrder = reader.GetInt32(1)
                        });
                    }
                }
            }

            return rows;
        }

        private static int EnsureRgbColorListSearch(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            int transactionId,
            List<ColorColumnRow> columns,
            PlmColorImportExecuteResultDto executeResult)
        {
            int searchId = EnsureSearchShell(
                tenantConn,
                ColorListSearchIntegrationId,
                "RGB Color",
                (int)EmAppSearchUsageType.Management,
                saasApplicationId,
                autoExecute: true);

            string dataSetQuery = $"SELECT * FROM [dbo].[{ColorRgbTableName}]";
            int dataSetId = SaveSearchDataSet(searchId, "RGB Color", dataSetQuery, tenantDataSourceId, saasApplicationId);

            var viewFields = BuildRgbListViewFields(columns);
            int searchViewId = SaveSearchView(searchId, "RGB Color", dataSetId, viewFields, gridOutputMode: 1);
            executeResult.ListSearchViewId = searchViewId;

            int? rootFieldId = GetSearchViewFieldId(tenantConn, null, searchViewId, "RGBColorID");
            if (!rootFieldId.HasValue)
                throw new InvalidOperationException("RGB Color list view is missing RGBColorID field.");

            ClearSearchViewFormLinkTargets(tenantConn, searchViewId);
            InsertColorFormLinkTarget(tenantConn, searchViewId, "Create", (int)EmAppLinkTargetActionType.Create, transactionId, rootFieldId.Value, "RGBColorID", 1);
            InsertColorFormLinkTarget(tenantConn, searchViewId, "Open", (int)EmAppLinkTargetActionType.Edit, transactionId, rootFieldId.Value, "RGBColorID", 2);
            InsertColorFormLinkTarget(tenantConn, searchViewId, "Delete", (int)EmAppLinkTargetActionType.Delete, transactionId, rootFieldId.Value, "RGBColorID", 3);

            var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(searchId, saasApplicationId, "RGB Color", "RGB Color");
            if (!menuResult.IsSuccessful && menuResult.ValidationResult?.HasErrors == true)
                executeResult.Messages.Add(menuResult.ValidationResult.Items?.FirstOrDefault()?.Message ?? "RGB Color menu update failed.");
            else
                executeResult.Messages.Add($"RGB Color list search {searchId} configured.");

            EnsureTransactionQuickSearchNavigation(tenantConn, transactionId, searchId);
            return searchId;
        }

        private static int EnsureRgbColorFolderTemplateSearch(
            SqlConnection tenantConn,
            int? saasApplicationId,
            int tenantDataSourceId,
            int transactionId,
            List<ColorColumnRow> columns,
            PlmColorImportExecuteResultDto executeResult)
        {
            int searchId = EnsureSearchShell(
                tenantConn,
                ColorFolderTemplateSearchIntegrationId,
                "Color Management",
                (int)EmAppSearchUsageType.DataModelTemplate,
                saasApplicationId,
                autoExecute: false);

            string dataSetQuery = $@"
SELECT Plm_pdmRGBColor.*, Plm_pdmColorGroupDetail.AppFolderId
FROM dbo.{ColorGroupDetailTable}
LEFT OUTER JOIN dbo.{ColorRgbTableName}
    ON {ColorGroupDetailTable}.RGBColorID = {ColorRgbTableName}.RGBColorID";

            int dataSetId = SaveSearchDataSet(searchId, "Color Management", dataSetQuery, tenantDataSourceId, saasApplicationId);
            var viewFields = BuildRgbFolderViewFields(columns);
            int searchViewId = SaveSearchView(searchId, "Color Management", dataSetId, viewFields, gridOutputMode: 1);
            executeResult.FolderSearchViewId = searchViewId;

            int? rootFieldId = GetSearchViewFieldId(tenantConn, null, searchViewId, "RGBColorID");
            if (!rootFieldId.HasValue)
                throw new InvalidOperationException("Color Management folder view is missing RGBColorID field.");

            ClearSearchViewFormLinkTargets(tenantConn, searchViewId);
            InsertColorFormLinkTarget(tenantConn, searchViewId, "Color Editor", (int)EmAppLinkTargetActionType.Edit, transactionId, rootFieldId.Value, "RGBColorID", 1);

            executeResult.Messages.Add($"Color Management folder template search {searchId} configured.");
            return searchId;
        }

        private static void ConfigureColorFolderNavigation(
            SqlConnection tenantConn,
            int folderTemplateSearchId,
            int transactionId,
            int rootFolderId,
            PlmColorImportExecuteResultDto executeResult)
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
                    ?? "Failed to configure Color folder navigation.");
            }

            executeResult.Messages.Add($"Folder navigation configured with root folder {rootFolderId}.");
        }

        private static int EnsureSearchShell(
            SqlConnection tenantConn,
            string integrationId,
            string name,
            int searchType,
            int? saasApplicationId,
            bool autoExecute)
        {
            int? searchId = GetSearchIdByIntegrationId(tenantConn, null, integrationId);
            if (!searchId.HasValue)
            {
                using (var cmd = tenantConn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppSearch (Name, Description, Type, IsAutoExecute, SaasApplicationID, IntegrationId)
VALUES (@Name, @Description, @Type, @IsAutoExecute, @SaasApplicationId, @IntegrationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Description", name);
                    cmd.Parameters.AddWithValue("@Type", searchType);
                    cmd.Parameters.AddWithValue("@IsAutoExecute", autoExecute);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IntegrationId", integrationId);
                    searchId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                using (var cmd = tenantConn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppSearch
SET Name = @Name, Description = @Description, Type = @Type, IsAutoExecute = @IsAutoExecute, SaasApplicationID = @SaasApplicationId
WHERE SearchID = @SearchId";
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Description", name);
                    cmd.Parameters.AddWithValue("@Type", searchType);
                    cmd.Parameters.AddWithValue("@IsAutoExecute", autoExecute);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return searchId.Value;
        }

        private static int SaveSearchDataSet(int searchId, string name, string queryText, int tenantDataSourceId, int? saasApplicationId)
        {
            AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            AppDataSetExDto dataSetDto;
            if (searchDto.DataSetId.HasValue)
            {
                dataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(searchDto.DataSetId.Value);
                dataSetDto.QueryText = queryText;
                dataSetDto.Name = TruncateDataSetName(name);
                dataSetDto.Description = name;
                dataSetDto.IsModified = true;
            }
            else
            {
                dataSetDto = new AppDataSetExDto
                {
                    Name = TruncateDataSetName(name),
                    Description = name,
                    QueryType = (int)EmAppDataServiceType.QueryText,
                    QueryText = queryText,
                    DataSourceFrom = tenantDataSourceId,
                    SaasApplicationId = saasApplicationId
                };
            }

            var saveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
            if (!saveResult.IsSuccessfulWithResult)
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to save color dataset.");

            return Convert.ToInt32(saveResult.Object.Id);
        }

        private static int SaveSearchView(
            int searchId,
            string name,
            int dataSetId,
            ObservableSet<AppSearchViewFieldExDto> viewFields,
            int gridOutputMode)
        {
            AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            AppSearchViewExDto searchViewDto;
            if (searchDto.SearchViewId.HasValue)
            {
                ClearSearchViewFieldsOnConnection(searchDto.SearchViewId.Value);
                searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchDto.SearchViewId.Value);
                searchViewDto.Name = name;
                searchViewDto.Description = name;
                searchViewDto.DataSetId = dataSetId;
                searchViewDto.GridOutputMode = gridOutputMode;
                searchViewDto.ViewType = (int)EmAppViewType.GridView;
                searchViewDto.IsModified = true;
                searchViewDto.AppSearchViewFieldList = viewFields;
            }
            else
            {
                searchViewDto = new AppSearchViewExDto
                {
                    Name = name,
                    Description = name,
                    DataSetId = dataSetId,
                    GridOutputMode = gridOutputMode,
                    ViewType = (int)EmAppViewType.GridView,
                    AppSearchViewFieldList = viewFields
                };
            }

            var saveViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
            if (!saveViewResult.IsSuccessfulWithResult)
                throw new InvalidOperationException(saveViewResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to save color search view.");

            int searchViewId = Convert.ToInt32(saveViewResult.Object.Id);
            searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            searchDto.DataSetId = dataSetId;
            searchDto.SearchViewId = searchViewId;
            searchDto.IsModified = true;
            var saveSearchResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
            if (!saveSearchResult.IsSuccessfulWithResult)
                throw new InvalidOperationException(saveSearchResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to update color search.");

            return searchViewId;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildRgbListViewFields(List<ColorColumnRow> columns)
        {
            var fields = new ObservableSet<AppSearchViewFieldExDto>();
            int sort = 10;
            foreach (var column in columns)
            {
                fields.Add(new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = true,
                    SysTableFiledPath = column.ColumnName,
                    DisplayText = column.ColumnName,
                    ControlType = ResolveRgbColumnControlType(column.ColumnName),
                    Sort = sort
                });
                sort += 10;
            }

            return fields;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildRgbFolderViewFields(List<ColorColumnRow> columns)
        {
            var fields = new ObservableSet<AppSearchViewFieldExDto>();
            int sort = 1;
            foreach (var columnName in new[] { "RGBColorID", "Name", "Description", "Code", "IsActive", "RGB", "SwatchColor" })
            {
                if (!columns.Any(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                    && !string.Equals(columnName, "AppFolderId", StringComparison.OrdinalIgnoreCase))
                    continue;

                var field = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = true,
                    SysTableFiledPath = columnName,
                    DisplayText = columnName,
                    ControlType = ResolveRgbColumnControlType(columnName),
                    Sort = sort++
                };
                if (string.Equals(columnName, "RGBColorID", StringComparison.OrdinalIgnoreCase))
                    field.IsTransRootId = true;
                fields.Add(field);
            }

            fields.Add(new AppSearchViewFieldExDto
            {
                IsModified = true,
                IsVisible = false,
                SysTableFiledPath = ColorAppFolderIdFieldName,
                DisplayText = ColorAppFolderIdFieldName,
                ControlType = (int)EmAppControlType.TextBox,
                Sort = sort,
                IsFileFoderId = true
            });

            return fields;
        }

        private static int ResolveRgbColumnControlType(string columnName)
        {
            if (string.Equals(columnName, "SwatchColor", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppControlType.RGBColorDisplay;
            if (string.Equals(columnName, "IsActive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "IsPublishToERP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "IsReadOnly", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppControlType.CheckBox;
            if (columnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "Red", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "Green", StringComparison.OrdinalIgnoreCase)
                || string.Equals(columnName, "Blue", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppControlType.TextBox;
            return (int)EmAppControlType.TextBox;
        }

        private static void InsertColorFormLinkTarget(
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

        private static void EnsureTransactionQuickSearchNavigation(SqlConnection tenantConn, int transactionId, int quickSearchId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
IF NOT EXISTS (
    SELECT 1 FROM dbo.AppTransactionNavigation
    WHERE TransactionID = @TransactionId AND QuickSearchID = @QuickSearchId)
BEGIN
    INSERT INTO dbo.AppTransactionNavigation (TransactionID, QuickSearchID, IsDefaultView)
    VALUES (@TransactionId, @QuickSearchId, 0);
END";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Parameters.AddWithValue("@QuickSearchId", quickSearchId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? GetSearchViewFieldId(SqlConnection tenantConn, SqlTransaction tran, int searchViewId, string columnName)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 SearchViewFieldID
FROM dbo.AppSearchViewField
WHERE SearchViewID = @SearchViewId AND SysTableFiledPath = @ColumnName
ORDER BY SearchViewFieldID;";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetSearchViewIdForSearch(SqlConnection tenantConn, int searchId)
        {
            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = "SELECT SearchViewID FROM dbo.AppSearch WHERE SearchID = @SearchId";
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void ClearSearchViewFieldsOnConnection(int searchViewId)
        {
            string tenantConn = GetTenantConnectionString();
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                ClearSearchViewFields(conn, searchViewId);
            }
        }

        private static int CountTableRows(SqlConnection conn, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT COUNT(1) FROM dbo.[{tableName}]";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
