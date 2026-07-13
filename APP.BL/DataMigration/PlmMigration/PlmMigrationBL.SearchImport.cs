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
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public const string StepSearchImport = "SearchImport";

        private const string SearchImportActionInsert = "Insert";
        private const string SearchImportActionUpdate = "Update";

        public static OperationCallResult<PlmSearchImportBlueprintDto> LoadSearchImportBlueprint(PlmSearchImportLoadRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchImportBlueprintDto>();
            try
            {
                RequirePlmMigrationAdmin();
                if (request == null || string.IsNullOrWhiteSpace(request.BlueprintJson))
                    throw new ArgumentException("BlueprintJson is required.");

                var blueprint = JsonConvert.DeserializeObject<PlmSearchImportBlueprintDto>(request.BlueprintJson);
                if (blueprint == null)
                    throw new InvalidOperationException("Search blueprint JSON could not be deserialized.");

                if (blueprint.SchemaVersion <= 0)
                    blueprint.SchemaVersion = 1;

                result.Object = blueprint;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchImport_Load_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportValidationDto> ValidateSearchImportBlueprint(PlmSearchImportBlueprintDto blueprint)
        {
            var result = new OperationCallResult<PlmSearchImportValidationDto>
            {
                Object = new PlmSearchImportValidationDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                string tenantConn = GetTenantConnectionString();
                ValidateSearchImportBlueprintInternal(blueprint, tenantConn, result.Object);
                result.Object.IsValid = result.Object.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Object.Errors.Add(ex.Message);
                result.Object.IsValid = false;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchImport_Validate_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportPreviewDto> PreviewSearchBlueprintConfig(PlmSearchImportBlueprintDto blueprint)
        {
            var result = new OperationCallResult<PlmSearchImportPreviewDto>
            {
                Object = new PlmSearchImportPreviewDto { IsSuccess = true }
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (blueprint == null)
                    throw new ArgumentException("Blueprint is required.");

                string tenantConn = GetTenantConnectionString();
                result.Object.Items = BuildSearchImportPreviewItems(blueprint, tenantConn);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchImport_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportExecuteResultDto> ExecuteSearchBlueprintConfig(PlmSearchImportExecuteRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchImportExecuteResultDto>
            {
                Object = new PlmSearchImportExecuteResultDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (request?.Blueprint == null)
                    throw new ArgumentException("Blueprint is required.");

                string tenantConn = GetTenantConnectionString();
                var validation = new PlmSearchImportValidationDto();
                ValidateSearchImportBlueprintInternal(request.Blueprint, tenantConn, validation);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                int tenantDataSourceId = request.Blueprint.DataSet?.TenantDataSourceRegisterId
                    ?? GetTenantDataSourceId();
                int? saasApplicationId = request.SaasApplicationId
                    ?? request.Blueprint.Search?.SaasApplicationId;

                result.Object = ExecuteSearchBlueprintConfigCore(
                    request.Blueprint, tenantConn, tenantDataSourceId, saasApplicationId);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchImport_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void ValidateSearchImportBlueprintInternal(
            PlmSearchImportBlueprintDto blueprint,
            string tenantConn,
            PlmSearchImportValidationDto validation)
        {
            if (blueprint == null)
            {
                validation.Errors.Add("Blueprint is required.");
                return;
            }

            if (blueprint.SchemaVersion <= 0)
                validation.Warnings.Add("SchemaVersion missing — defaulting to 1 at execute time.");

            if (blueprint.Search == null || string.IsNullOrWhiteSpace(blueprint.Search.IntegrationId))
                validation.Errors.Add("search.integrationId is required.");

            if (blueprint.DataSet == null || string.IsNullOrWhiteSpace(blueprint.DataSet.QueryText))
                validation.Errors.Add("dataSet.queryText is required.");

            if (blueprint.SearchView == null || string.IsNullOrWhiteSpace(blueprint.SearchView.IntegrationId))
                validation.Errors.Add("searchView.integrationId is required.");

            if (blueprint.SearchView?.Fields == null || blueprint.SearchView.Fields.Count == 0)
                validation.Errors.Add("searchView.fields must contain at least one column.");

            if (!blueprint.SearchView.Fields.Any(f => f.IsTransRootId))
                validation.Errors.Add("searchView.fields must include one field with isTransRootId=true (typically ReferenceId).");

            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                string primaryTable = blueprint.DataSet?.PrimaryTableName
                    ?? blueprint.Source?.PrimaryTableName
                    ?? blueprint.DataSet?.RootTableName;
                if (!string.IsNullOrWhiteSpace(primaryTable) && !TemplateTableExists(conn, null, primaryTable))
                    validation.Errors.Add($"Primary table dbo.[{primaryTable}] does not exist in tenant database.");

                foreach (var join in EnumerateJoinTables(blueprint))
                {
                    if (!string.IsNullOrWhiteSpace(join) && !TemplateTableExists(conn, null, join))
                        validation.Errors.Add($"JOIN table dbo.[{join}] does not exist in tenant database.");
                }

                foreach (var link in blueprint.LinkTargets ?? Enumerable.Empty<PlmSearchImportLinkTargetDto>())
                {
                    if (string.IsNullOrWhiteSpace(link.TransactionIntegrationId))
                        continue;

                    int? txId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                    if (!txId.HasValue)
                        validation.Errors.Add($"Link target transaction '{link.TransactionIntegrationId}' was not found.");
                }

                int? groupId = ResolveLinkTargetTransactionGroupId(conn, blueprint);
                if (groupId.HasValue)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1 FROM dbo.AppTransactionGroup WHERE TransactionGroupID = @GroupId";
                        cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
                        if (cmd.ExecuteScalar() == null)
                            validation.Errors.Add($"Transaction group id {groupId.Value} was not found.");
                    }
                }
            }

            if ((blueprint.CriteriaFields?.Count ?? 0) == 0)
                validation.Warnings.Add("No criteriaFields in blueprint — search will have an empty criteria panel.");

            if (blueprint.UnmappedPlmFields?.Count > 0)
                validation.Warnings.Add($"{blueprint.UnmappedPlmFields.Count} PLM field(s) were intentionally unmapped.");
        }

        private static IEnumerable<string> EnumerateJoinTables(PlmSearchImportBlueprintDto blueprint)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string primary = blueprint.DataSet?.PrimaryTableName
                ?? blueprint.Source?.PrimaryTableName;
            if (!string.IsNullOrWhiteSpace(primary))
                tables.Add(primary);

            if (!string.IsNullOrWhiteSpace(blueprint.DataSet?.RootTableName))
                tables.Add(blueprint.DataSet.RootTableName);

            // Infer from query text aliases is fragile — use known plan tables when present.
            if (blueprint.JoinPlan?.Label != null)
            {
                foreach (var known in new[] { "Plm_ReferenceBasicInfo", "Plm_Style_Header", "Plm_Style_Summary" })
                {
                    if (blueprint.DataSet?.QueryText?.IndexOf(known, StringComparison.OrdinalIgnoreCase) >= 0)
                        tables.Add(known);
                }
            }

            return tables;
        }

        private static List<PlmSearchImportPreviewItemDto> BuildSearchImportPreviewItems(
            PlmSearchImportBlueprintDto blueprint,
            string tenantConn)
        {
            var items = new List<PlmSearchImportPreviewItemDto>();
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();

                string searchIntegrationId = blueprint.Search?.IntegrationId;
                int? searchId = GetSearchIdByIntegrationId(conn, null, searchIntegrationId);
                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "Search",
                    Name = blueprint.Search?.Name ?? searchIntegrationId,
                    IntegrationId = searchIntegrationId,
                    Action = searchId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = searchId,
                    Detail = $"Criteria fields: {blueprint.CriteriaFields?.Count ?? 0}"
                });

                int? dataSetId = null;
                if (searchId.HasValue)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DataSetId FROM dbo.AppSearch WHERE SearchID = @SearchId";
                        cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                        var val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            dataSetId = Convert.ToInt32(val);
                    }
                }

                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "DataSet",
                    Name = blueprint.DataSet?.Name ?? "Search DataSet",
                    IntegrationId = null,
                    Action = dataSetId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = dataSetId,
                    Detail = $"Primary: {blueprint.DataSet?.PrimaryTableName ?? blueprint.Source?.PrimaryTableName}"
                });

                int? searchViewId = searchId.HasValue
                    ? GetSearchViewIdForSearch(conn, searchId.Value)
                    : null;
                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "SearchView",
                    Name = blueprint.SearchView?.Name ?? blueprint.SearchView?.IntegrationId,
                    IntegrationId = blueprint.SearchView?.IntegrationId,
                    Action = searchViewId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = searchViewId,
                    Detail = $"View fields: {blueprint.SearchView?.Fields?.Count ?? 0}"
                });

                int? groupId = ResolveLinkTargetTransactionGroupId(conn, blueprint);
                if (groupId.HasValue)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "TransactionGroup",
                        Name = blueprint.TransactionGroup?.GroupName ?? $"Group {groupId}",
                        IntegrationId = null,
                        Action = SearchImportActionUpdate,
                        ExistingId = groupId,
                        Detail = "Open link will use LinkTargetTransactionGroupId"
                    });
                }

                foreach (var link in blueprint.LinkTargets ?? Enumerable.Empty<PlmSearchImportLinkTargetDto>())
                {
                    int? txId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "LinkTarget",
                        Name = link.Name,
                        IntegrationId = link.TransactionIntegrationId,
                        Action = SearchImportActionInsert,
                        ExistingId = txId,
                        Detail = link.ActionType
                    });
                }

                if (blueprint.Menu?.RegisterInMainMenu == true)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "Menu",
                        Name = blueprint.Menu.MenuTitle ?? blueprint.Search?.Name,
                        IntegrationId = searchIntegrationId,
                        Action = searchId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                        ExistingId = searchId,
                        Detail = "Register in application main menu"
                    });
                }
            }

            return items;
        }

        private static PlmSearchImportExecuteResultDto ExecuteSearchBlueprintConfigCore(
            PlmSearchImportBlueprintDto blueprint,
            string tenantConn,
            int tenantDataSourceId,
            int? saasApplicationId)
        {
            var executeResult = new PlmSearchImportExecuteResultDto { IsSuccess = true };
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();

                string searchIntegrationId = blueprint.Search.IntegrationId;
                string searchName = blueprint.Search.Name ?? blueprint.Source?.PlmSearchName ?? "PLM Search";
                int searchType = ResolveSearchUsageType(blueprint.Search.UsageType);
                bool autoExecute = blueprint.Search.AutoExecute;

                int searchId = EnsureSearchShell(
                    conn, searchIntegrationId, searchName, searchType, saasApplicationId, autoExecute);
                executeResult.SearchId = searchId;
                executeResult.Messages.Add($"Search {searchId} ({searchIntegrationId}) ready.");

                string dataSetName = blueprint.DataSet.Name ?? searchName;
                int dataSetId = SaveSearchDataSet(searchId, dataSetName, blueprint.DataSet.QueryText, tenantDataSourceId, saasApplicationId);
                executeResult.DataSetId = dataSetId;
                executeResult.Messages.Add($"DataSet {dataSetId} saved.");

                var viewFields = BuildSearchImportViewFields(conn, blueprint.SearchView?.Fields);
                string viewName = blueprint.SearchView.Name ?? searchName;
                int gridOutputMode = blueprint.SearchView.GridOutputMode > 0 ? blueprint.SearchView.GridOutputMode : 1;
                int searchViewId = SaveSearchView(searchId, viewName, dataSetId, viewFields, gridOutputMode);
                executeResult.SearchViewId = searchViewId;
                executeResult.Messages.Add($"Search view {searchViewId} saved with {viewFields.Count} field(s).");

                ClearSearchCriteriaFields(conn, searchId);
                SaveSearchCriteriaFields(searchId, blueprint.CriteriaFields, conn);
                executeResult.Messages.Add($"Saved {blueprint.CriteriaFields?.Count ?? 0} criteria field(s).");

                string rootColumn = blueprint.SearchView.Fields?
                    .FirstOrDefault(f => f.IsTransRootId)?.SysTableFiledPath ?? "ReferenceId";
                int? rootFieldId = GetSearchViewFieldId(conn, null, searchViewId, rootColumn);
                if (!rootFieldId.HasValue)
                    throw new InvalidOperationException($"Search view is missing root column '{rootColumn}'.");

                ClearSearchViewFormLinkTargets(conn, searchViewId);
                int? transactionGroupId = ResolveLinkTargetTransactionGroupId(conn, blueprint);
                foreach (var link in blueprint.LinkTargets ?? Enumerable.Empty<PlmSearchImportLinkTargetDto>())
                {
                    int? transactionId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                    if (!transactionId.HasValue)
                        throw new InvalidOperationException($"Transaction '{link.TransactionIntegrationId}' not found for link target '{link.Name}'.");

                    int actionType = ResolveLinkTargetActionType(link.ActionType);
                    int sort = link.Sort ?? 1;
                    int? groupIdForLink = link.LinkTargetTransactionGroupId ?? transactionGroupId;
                    InsertSearchFormLinkTarget(
                        conn,
                        searchViewId,
                        link.Name,
                        actionType,
                        transactionId.Value,
                        rootFieldId.Value,
                        link.SourceColumn ?? rootColumn,
                        sort,
                        groupIdForLink);
                }
                executeResult.Messages.Add($"Configured {blueprint.LinkTargets?.Count ?? 0} link target(s).");

                if (blueprint.Menu?.RegisterInMainMenu == true)
                {
                    string menuTitle = blueprint.Menu.MenuTitle ?? searchName;
                    var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(
                        searchId, saasApplicationId, menuTitle, menuTitle);
                    if (!menuResult.IsSuccessful && menuResult.ValidationResult?.HasErrors == true)
                    {
                        executeResult.Messages.Add(menuResult.ValidationResult.Items?.FirstOrDefault()?.Message
                            ?? "Main menu registration reported errors.");
                    }
                    else
                    {
                        executeResult.Messages.Add($"Registered '{menuTitle}' in main menu.");
                    }
                }
            }

            return executeResult;
        }

        private static int ResolveSearchUsageType(string usageType)
        {
            if (string.Equals(usageType, "DataModelTemplate", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppSearchUsageType.DataModelTemplate;
            return (int)EmAppSearchUsageType.Management;
        }

        private static int ResolveLinkTargetActionType(string actionType)
        {
            if (string.Equals(actionType, "Create", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppLinkTargetActionType.Create;
            if (string.Equals(actionType, "Delete", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppLinkTargetActionType.Delete;
            return (int)EmAppLinkTargetActionType.Edit;
        }

        private static int? ResolveLinkTargetTransactionGroupId(SqlConnection conn, PlmSearchImportBlueprintDto blueprint)
        {
            if (blueprint.TransactionGroup?.TransactionGroupId is int groupId && groupId > 0)
                return groupId;

            int? fromLink = blueprint.LinkTargets?
                .Select(l => l.LinkTargetTransactionGroupId)
                .FirstOrDefault(id => id.HasValue && id.Value > 0);
            if (fromLink.HasValue)
                return fromLink;

            if (!string.IsNullOrWhiteSpace(blueprint.TransactionGroup?.GroupName))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT TOP 1 TransactionGroupID FROM dbo.AppTransactionGroup WHERE GroupName = @Name";
                    cmd.Parameters.AddWithValue("@Name", blueprint.TransactionGroup.GroupName);
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        return Convert.ToInt32(val);
                }
            }

            return null;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildSearchImportViewFields(
            SqlConnection conn,
            List<PlmSearchImportSearchViewFieldDto> fields)
        {
            var result = new ObservableSet<AppSearchViewFieldExDto>();
            if (fields == null)
                return result;

            foreach (var field in fields.OrderBy(f => f.Sort ?? int.MaxValue))
            {
                if (string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;

                var dto = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = field.IsVisible,
                    SysTableFiledPath = field.SysTableFiledPath,
                    DisplayText = string.IsNullOrWhiteSpace(field.DisplayText) ? field.SysTableFiledPath : field.DisplayText,
                    ControlType = field.ControlType ?? (int)EmAppControlType.TextBox,
                    Sort = field.Sort
                };
                if (field.IsTransRootId)
                    dto.IsTransRootId = true;

                int? entityId = ResolveEntityInfoId(conn, field.EntityIntegrationId);
                if (entityId.HasValue)
                    dto.EntityId = entityId;

                result.Add(dto);
            }

            return result;
        }

        private static void SaveSearchCriteriaFields(
            int searchId,
            List<PlmSearchImportCriteriaFieldDto> criteriaFields,
            SqlConnection conn)
        {
            if (criteriaFields == null || criteriaFields.Count == 0)
                return;

            AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            searchDto.AppSearchFieldList = new ObservableSet<AppSearchFieldExDto>();

            foreach (var field in criteriaFields.OrderBy(f => f.Sort ?? int.MaxValue))
            {
                if (string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;

                var dto = new AppSearchFieldExDto
                {
                    IsModified = true,
                    IsVisible = field.IsVisible,
                    IsReadOnly = false,
                    IsAllowMultipleSelect = false,
                    SysTableFiledPath = field.SysTableFiledPath,
                    DisplayText = string.IsNullOrWhiteSpace(field.DisplayText) ? field.SysTableFiledPath : field.DisplayText,
                    ControlType = field.ControlType ?? (int)EmAppControlType.TextBox,
                    OperationId = field.OperationId,
                    PositionRow = field.PositionRow,
                    PositionColumn = field.PositionColumn,
                    Sort = field.Sort,
                    DefaultValue = field.DefaultValue
                };

                int? entityId = ResolveEntityInfoId(conn, field.EntityIntegrationId);
                if (entityId.HasValue)
                    dto.EntityId = entityId;

                searchDto.AppSearchFieldList.Add(dto);
            }

            searchDto.IsModified = true;
            var saveResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to save search criteria fields.");
            }
        }

        private static int? ResolveEntityInfoId(SqlConnection conn, string entityIntegrationId)
        {
            if (string.IsNullOrWhiteSpace(entityIntegrationId))
                return null;

            if (!int.TryParse(entityIntegrationId.Trim(), out int plmEntityId))
                return null;

            return GetAppEntityInfoIdByPlmEntityId(conn, null, plmEntityId);
        }

        private static void ClearSearchCriteriaFields(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.AppSearchField WHERE SearchID = @SearchId";
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertSearchFormLinkTarget(
            SqlConnection tenantConn,
            int searchViewId,
            string navigationActionName,
            int actionType,
            int linkTargetTransactionId,
            int sourceViewColumnId,
            string targetColumn,
            int sort,
            int? linkTargetTransactionGroupId,
            int? linkTargetUsageType = null)
        {
            // Group id ⇒ Business Template / Form Group (UsageType 2); otherwise Link To Form (1).
            int usageType = linkTargetUsageType
                ?? (linkTargetTransactionGroupId.HasValue && linkTargetTransactionGroupId.Value > 0
                    ? (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup
                    : (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppFormLinkTarget (
    SearchViewID,
    NavigationActionName,
    ActionType,
    LinkTargetTransactionID,
    LinkTargetTransactionGroupID,
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
    @LinkTargetTransactionGroupId,
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
                cmd.Parameters.AddWithValue("@LinkTargetTransactionGroupId", (object)linkTargetTransactionGroupId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LinkTargetUsageType", usageType);
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
    }
}
