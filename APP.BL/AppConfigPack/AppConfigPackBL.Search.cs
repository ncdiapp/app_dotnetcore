using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        /// <summary>Public Search upsert — prefer <see cref="StepUpsertSearches"/>.</summary>
        public static void UpsertSearches(
            AppConfigPackDto pack,
            int tenantDataSourceId,
            int? saasApplicationId,
            Dictionary<string, int> txIdsByIntegration,
            int? transactionGroupId,
            AppConfigPackExecuteResultDto executeResult)
        {
            var searchIds = new List<int>();

            foreach (var search in pack.Searches ?? Enumerable.Empty<AppConfigPackSearchDto>())
            {
                if (search == null || string.IsNullOrWhiteSpace(search.IntegrationId))
                    continue;

                string integrationId = search.IntegrationId.Trim();
                string searchName = search.Name ?? integrationId;
                int usageType = ResolveSearchUsageType(search.UsageType);
                bool inserted;

                using (var conn = OpenTenantConnection())
                {
                    int? existing = GetSearchIdByIntegrationId(conn, integrationId);
                    inserted = !existing.HasValue;
                    int searchId = EnsureSearchShell(
                        conn, integrationId, searchName, search.Description, usageType, saasApplicationId, search.AutoExecute);

                    string dataSetName = search.DataSet?.Name ?? searchName;
                    int dataSetId = SaveSearchDataSet(
                        searchId, dataSetName, search.DataSet?.QueryText, tenantDataSourceId, saasApplicationId);

                    var viewFields = BuildSearchViewFields(conn, search.SearchView?.Fields);
                    string viewName = search.SearchView?.Name ?? searchName;
                    int gridOutputMode = search.SearchView != null && search.SearchView.GridOutputMode > 0
                        ? search.SearchView.GridOutputMode
                        : 1;
                    int searchViewId = SaveSearchView(conn, searchId, viewName, dataSetId, viewFields, gridOutputMode);

                    ClearSearchCriteriaFields(conn, searchId);
                    SaveSearchCriteriaFields(searchId, search.CriteriaFields, conn);

                    string rootColumn = search.SearchView?.Fields?
                        .FirstOrDefault(f => f != null && f.IsTransRootId)?.SysTableFiledPath ?? "Id";
                    int? rootFieldId = GetSearchViewFieldId(conn, searchViewId, rootColumn);
                    if (!rootFieldId.HasValue)
                        throw new InvalidOperationException(
                            $"Search '{integrationId}' view is missing root column '{rootColumn}'.");

                    ClearSearchViewFormLinkTargets(conn, searchViewId);
                    foreach (var link in search.LinkTargets ?? Enumerable.Empty<AppConfigPackLinkTargetDto>())
                    {
                        if (string.IsNullOrWhiteSpace(link?.TransactionIntegrationId))
                            continue;

                        int? transactionId;
                        if (!txIdsByIntegration.TryGetValue(link.TransactionIntegrationId.Trim(), out int mappedId))
                            transactionId = GetTransactionIdByIntegrationId(conn, link.TransactionIntegrationId);
                        else
                            transactionId = mappedId;

                        if (!transactionId.HasValue)
                            throw new InvalidOperationException(
                                $"Link target transaction '{link.TransactionIntegrationId}' was not found for search '{integrationId}'.");

                        int resolvedUsage = ResolveSearchLinkUsageType(link, transactionGroupId);
                        int? groupIdForLink = resolvedUsage == (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup
                            ? transactionGroupId
                            : null;

                        InsertSearchFormLinkTarget(
                            conn,
                            searchViewId,
                            string.IsNullOrWhiteSpace(link.Name) ? (link.ActionType ?? "Edit") : link.Name,
                            ResolveLinkTargetActionType(link.ActionType),
                            transactionId.Value,
                            rootFieldId.Value,
                            string.IsNullOrWhiteSpace(link.SourceColumn) ? rootColumn : link.SourceColumn.Trim(),
                            link.Sort ?? 1,
                            groupIdForLink,
                            resolvedUsage,
                            link.TemplateItemType);
                    }

                    if (usageType == (int)EmAppSearchUsageType.DataModelTemplate)
                    {
                        EnsureDataModelTemplateItemLinkTargets(
                            conn,
                            searchViewId,
                            rootFieldId.Value,
                            rootColumn,
                            transactionGroupId,
                            pack.TransactionGroup,
                            txIdsByIntegration);
                    }

                    if (search.Menu?.RegisterInMainMenu == true && saasApplicationId.HasValue)
                    {
                        string menuTitle = search.Menu.MenuTitle ?? searchName;
                        var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(
                            searchId, saasApplicationId, menuTitle, menuTitle);
                        if (menuResult.ValidationResult != null && menuResult.ValidationResult.HasErrors)
                        {
                            executeResult.Messages.Add(
                                menuResult.ValidationResult.Items?.FirstOrDefault()?.Message
                                ?? $"Menu registration failed for search {searchId}.");
                        }
                    }

                    searchIds.Add(searchId);
                    if (inserted)
                    {
                        executeResult.SearchesInserted++;
                        executeResult.Messages.Add($"Inserted search {searchId} ({integrationId}).");
                    }
                    else
                    {
                        executeResult.SearchesUpdated++;
                        executeResult.Messages.Add($"Updated search {searchId} ({integrationId}).");
                    }
                }
            }

            AttachSearchAssets(saasApplicationId, searchIds, executeResult);
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

        private static int EnsureSearchShell(
            SqlConnection conn,
            string integrationId,
            string name,
            string description,
            int searchType,
            int? saasApplicationId,
            bool autoExecute)
        {
            int? searchId = GetSearchIdByIntegrationId(conn, integrationId);
            if (!searchId.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppSearch (Name, Description, Type, IsAutoExecute, SaasApplicationID, IntegrationId)
VALUES (@Name, @Description, @Type, @IsAutoExecute, @SaasApplicationId, @IntegrationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@Name", TruncateName(name, 50, integrationId));
                    cmd.Parameters.AddWithValue("@Description", (object)(description ?? name) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Type", searchType);
                    cmd.Parameters.AddWithValue("@IsAutoExecute", autoExecute);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IntegrationId", TruncateName(integrationId, 100, integrationId));
                    searchId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppSearch
SET Name = @Name, Description = @Description, Type = @Type, IsAutoExecute = @IsAutoExecute,
    SaasApplicationID = COALESCE(@SaasApplicationId, SaasApplicationID)
WHERE SearchID = @SearchId";
                    cmd.Parameters.AddWithValue("@Name", TruncateName(name, 50, integrationId));
                    cmd.Parameters.AddWithValue("@Description", (object)(description ?? name) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Type", searchType);
                    cmd.Parameters.AddWithValue("@IsAutoExecute", autoExecute);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return searchId.Value;
        }

        private static int SaveSearchDataSet(
            int searchId,
            string name,
            string queryText,
            int tenantDataSourceId,
            int? saasApplicationId)
        {
            AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            AppDataSetExDto dataSetDto;
            if (searchDto.DataSetId.HasValue)
            {
                dataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(searchDto.DataSetId.Value);
                dataSetDto.QueryText = queryText;
                dataSetDto.Name = TruncateName(name, 100, "Search");
                dataSetDto.Description = name;
                dataSetDto.IsModified = true;
            }
            else
            {
                dataSetDto = new AppDataSetExDto
                {
                    Name = TruncateName(name, 100, "Search"),
                    Description = name,
                    QueryType = (int)EmAppDataServiceType.QueryText,
                    QueryText = queryText,
                    DataSourceFrom = tenantDataSourceId,
                    SaasApplicationId = saasApplicationId
                };
            }

            var saveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
            if (!saveResult.IsSuccessfulWithResult)
                throw new InvalidOperationException(
                    saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to save search dataset.");

            return Convert.ToInt32(saveResult.Object.Id);
        }

        private static int SaveSearchView(
            SqlConnection conn,
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
                ClearSearchViewFields(conn, searchDto.SearchViewId.Value);
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
                throw new InvalidOperationException(
                    saveViewResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to save search view.");

            int searchViewId = Convert.ToInt32(saveViewResult.Object.Id);
            searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            searchDto.DataSetId = dataSetId;
            searchDto.SearchViewId = searchViewId;
            searchDto.IsModified = true;
            var saveSearchResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
            if (!saveSearchResult.IsSuccessfulWithResult)
                throw new InvalidOperationException(
                    saveSearchResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to update search.");

            return searchViewId;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildSearchViewFields(
            SqlConnection conn,
            List<AppConfigPackSearchViewFieldDto> fields)
        {
            var result = new ObservableSet<AppSearchViewFieldExDto>();
            if (fields == null)
                return result;

            int sort = 10;
            foreach (var field in fields.Where(f => f != null && !string.IsNullOrWhiteSpace(f.SysTableFiledPath)))
            {
                var dto = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = field.IsVisible,
                    SysTableFiledPath = field.SysTableFiledPath.Trim(),
                    DisplayText = string.IsNullOrWhiteSpace(field.DisplayText) ? field.SysTableFiledPath : field.DisplayText,
                    ControlType = field.ControlType ?? (int)EmAppControlType.TextBox,
                    IsTransRootId = field.IsTransRootId,
                    Sort = field.Sort ?? sort
                };
                int? entityId = ResolveEntityIdByCode(conn, field.EntityCode);
                if (entityId.HasValue)
                    dto.EntityId = entityId;
                result.Add(dto);
                sort += 10;
            }

            return result;
        }

        private static void SaveSearchCriteriaFields(
            int searchId,
            List<AppConfigPackCriteriaFieldDto> criteriaFields,
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
                    SysTableFiledPath = field.SysTableFiledPath.Trim(),
                    DisplayText = string.IsNullOrWhiteSpace(field.DisplayText) ? field.SysTableFiledPath : field.DisplayText,
                    ControlType = field.ControlType ?? (int)EmAppControlType.TextBox,
                    OperationId = field.OperationId,
                    PositionRow = field.PositionRow,
                    PositionColumn = field.PositionColumn,
                    Sort = field.Sort,
                    DefaultValue = field.DefaultValue
                };
                int? entityId = ResolveEntityIdByCode(conn, field.EntityCode);
                if (entityId.HasValue)
                    dto.EntityId = entityId;
                searchDto.AppSearchFieldList.Add(dto);
            }

            searchDto.IsModified = true;
            var saveResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(
                    saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to save search criteria fields.");
            }
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

        private static void ClearSearchViewFields(SqlConnection conn, int searchViewId)
        {
            ClearSearchViewFormLinkTargets(conn, searchViewId);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.AppSearchViewField WHERE SearchViewID = @SearchViewId";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ClearSearchViewFormLinkTargets(SqlConnection conn, int searchViewId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
DELETE FROM dbo.AppFormLinkTarget
WHERE SearchViewID = @SearchViewId
   OR SourceViewColumnID1 IN (SELECT SearchViewFieldID FROM dbo.AppSearchViewField WHERE SearchViewID = @SearchViewId)
   OR SourceViewColumnID2 IN (SELECT SearchViewFieldID FROM dbo.AppSearchViewField WHERE SearchViewID = @SearchViewId)
   OR SourceViewColumnID3 IN (SELECT SearchViewFieldID FROM dbo.AppSearchViewField WHERE SearchViewID = @SearchViewId);";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? GetSearchViewFieldId(SqlConnection conn, int searchViewId, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 SearchViewFieldID
FROM dbo.AppSearchViewField
WHERE SearchViewID = @SearchViewId AND SysTableFiledPath = @ColumnName";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int ResolveSearchLinkUsageType(AppConfigPackLinkTargetDto link, int? transactionGroupId)
        {
            if (string.Equals(link?.UsageType, "FormGroup", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup;
            if (string.Equals(link?.UsageType, "Form", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppLinkTargetUsageType.SearchViewLinkToForm;
            if (link?.TemplateItemType != null)
                return (int)EmAppLinkTargetUsageType.SearchViewLinkToForm;
            if (transactionGroupId.HasValue && transactionGroupId.Value > 0)
                return (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup;
            return (int)EmAppLinkTargetUsageType.SearchViewLinkToForm;
        }

        private static bool SearchViewHasTemplateItemLinks(SqlConnection conn, int searchViewId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 1
FROM dbo.AppFormLinkTarget
WHERE SearchViewID = @SearchViewId
  AND LinkTargetUsageType = @UsageForm
  AND OtherSettings LIKE N'%TemplateItemType%'";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@UsageForm", (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);
                return cmd.ExecuteScalar() != null;
            }
        }

        /// <summary>
        /// Data Model Template editor / Form Group read Usage=1 links with TemplateItemType.
        /// Pack Search often only has the FormGroup Edit open-entry; synthesize Main/Shared from the group.
        /// </summary>
        private static void EnsureDataModelTemplateItemLinkTargets(
            SqlConnection conn,
            int searchViewId,
            int sourceViewColumnId,
            string targetColumn,
            int? transactionGroupId,
            AppConfigPackTransactionGroupDto group,
            Dictionary<string, int> txIdsByIntegration)
        {
            if (SearchViewHasTemplateItemLinks(conn, searchViewId))
                return;

            var headerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in group?.HeaderTransactionIntegrationIds ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(key))
                    headerKeys.Add(key.Trim());
            }

            var members = new List<(int TxId, string Name, int Sort, bool IsHeader)>();
            var memberKeys = group?.MemberTransactionIntegrationIds;
            if (memberKeys != null && memberKeys.Count > 0)
            {
                int sort = 0;
                foreach (var key in memberKeys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        continue;
                    string trimmed = key.Trim();
                    int? txId;
                    if (txIdsByIntegration != null && txIdsByIntegration.TryGetValue(trimmed, out int mapped))
                        txId = mapped;
                    else
                        txId = GetTransactionIdByIntegrationId(conn, trimmed);
                    if (!txId.HasValue)
                        continue;
                    sort++;
                    members.Add((txId.Value, GetTransactionName(conn, txId.Value) ?? trimmed, sort, headerKeys.Contains(trimmed)));
                }
            }
            else if (transactionGroupId.HasValue && transactionGroupId.Value > 0)
            {
                members = LoadTransactionGroupTemplateMembers(conn, transactionGroupId.Value);
            }

            if (members.Count == 0)
                return;

            int? firstMainTxId = null;
            foreach (var member in members)
            {
                if (!member.IsHeader && firstMainTxId == null)
                    firstMainTxId = member.TxId;

                int templateItemType = member.IsHeader
                    ? (int)EmAppTransactionTemplateItemType.TemplateHeader
                    : (int)EmAppTransactionTemplateItemType.MainItem;
                InsertSearchFormLinkTarget(
                    conn,
                    searchViewId,
                    member.Name,
                    (int)EmAppLinkTargetActionType.Edit,
                    member.TxId,
                    sourceViewColumnId,
                    targetColumn,
                    member.Sort,
                    null,
                    (int)EmAppLinkTargetUsageType.SearchViewLinkToForm,
                    templateItemType);
            }

            if (firstMainTxId.HasValue)
            {
                InsertSearchFormLinkTarget(
                    conn,
                    searchViewId,
                    "New",
                    (int)EmAppLinkTargetActionType.Create,
                    firstMainTxId.Value,
                    sourceViewColumnId,
                    targetColumn,
                    0,
                    null,
                    (int)EmAppLinkTargetUsageType.SearchViewLinkToForm,
                    (int)EmAppTransactionTemplateItemType.MainItem);
            }
        }

        private static List<(int TxId, string Name, int Sort, bool IsHeader)> LoadTransactionGroupTemplateMembers(
            SqlConnection conn,
            int transactionGroupId)
        {
            var members = new List<(int TxId, string Name, int Sort, bool IsHeader)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT gi.TransID, t.TransactionName, ISNULL(gi.TransactionLayoutOrder, 0),
       CAST(CASE WHEN ISNULL(gi.IsGroupSharedHeader, 0) = 1 OR ISNULL(gi.IsCrossGroupSharedHeader, 0) = 1 THEN 1 ELSE 0 END AS bit)
FROM dbo.AppTransactionGroupItem gi
INNER JOIN dbo.AppTransaction t ON t.TransactionID = gi.TransID
WHERE gi.TransactionGroupID = @GroupId
ORDER BY ISNULL(gi.TransactionLayoutOrder, 0), gi.TransID";
                cmd.Parameters.AddWithValue("@GroupId", transactionGroupId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                            continue;
                        members.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            Convert.ToInt32(reader.GetValue(2)),
                            !reader.IsDBNull(3) && reader.GetBoolean(3)));
                    }
                }
            }

            return members;
        }

        private static string GetTransactionName(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TransactionName FROM dbo.AppTransaction WHERE TransactionID = @Id";
                cmd.Parameters.AddWithValue("@Id", transactionId);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static void InsertSearchFormLinkTarget(
            SqlConnection conn,
            int searchViewId,
            string navigationActionName,
            int actionType,
            int linkTargetTransactionId,
            int sourceViewColumnId,
            string targetColumn,
            int sort,
            int? linkTargetTransactionGroupId,
            int? explicitUsageType = null,
            int? templateItemType = null)
        {
            int usageType = explicitUsageType
                ?? (linkTargetTransactionGroupId.HasValue && linkTargetTransactionGroupId.Value > 0
                    ? (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup
                    : (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);

            string otherSettings = templateItemType.HasValue
                ? "{\"IsLinkToComsumeApiTransaction\":false,\"UiId\":\""
                    + Guid.NewGuid().ToString()
                    + "\",\"TemplateItemType\":"
                    + templateItemType.Value
                    + "}"
                : null;

            using (var cmd = conn.CreateCommand())
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
    PopupHeight,
    OtherSettings)
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
    @PopupHeight,
    @OtherSettings)";
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
                cmd.Parameters.AddWithValue("@OtherSettings", (object)otherSettings ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AttachSearchAssets(
            int? saasApplicationId,
            List<int> searchIds,
            AppConfigPackExecuteResultDto executeResult)
        {
            if (!saasApplicationId.HasValue || saasApplicationId.Value <= 0 || searchIds == null || searchIds.Count == 0)
                return;

            var existing = AppSaasUserApplicationPackageBL.RetrieveAppApplicationAssetsItemDtoListByType(
                saasApplicationId.Value, (int)EmAppApplicationAssetsType.Search);
            var needToSave = new ObservableSet<AppApplicationAssetsItemExDto>();
            foreach (int searchId in searchIds.Distinct())
            {
                var found = existing.FirstOrDefault(o => o.SearchId.HasValue && o.SearchId.Value == searchId);
                if (found != null)
                    needToSave.Add(found);
                else
                {
                    needToSave.Add(new AppApplicationAssetsItemExDto
                    {
                        ApplicationId = saasApplicationId.Value,
                        SearchId = searchId
                    });
                }
            }

            var save = AppSaasUserApplicationPackageBL.SaveAppApplicationAssetsItemDtoList(
                needToSave, saasApplicationId.Value, (int)EmAppApplicationAssetsType.Search);
            if (save.ValidationResult != null && save.ValidationResult.HasErrors)
            {
                executeResult.Messages.Add(
                    save.ValidationResult.Items?.FirstOrDefault()?.Message ?? "Failed to attach searches to the application.");
            }
            else
            {
                executeResult.Messages.Add($"Attached {searchIds.Distinct().Count()} search(es) to application {saasApplicationId.Value}.");
            }
        }
    }
}
