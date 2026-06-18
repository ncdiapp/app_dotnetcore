using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private sealed class TabTransactionFormStatus
        {
            public int TransactionId { get; set; }
            public int? FormId { get; set; }
            public int LayoutItemCount { get; set; }
            public bool HasCompleteLayout => FormId.HasValue && LayoutItemCount > 0;
        }

        private sealed class TemplateSearchViewFieldSpec
        {
            public string ColumnName { get; set; }
            public string DisplayText { get; set; }
            public bool IsVisible { get; set; }
            public bool IsFolderId { get; set; }
            public bool IsTransRootId { get; set; }
            public int? ControlType { get; set; }
        }

        private static readonly TemplateSearchViewFieldSpec[] ReferenceBasicInfoViewFields =
        {
            new TemplateSearchViewFieldSpec { ColumnName = "ReferenceId", DisplayText = "Reference Id", IsVisible = false, IsTransRootId = true, ControlType = (int)EmAppControlType.TextBox },
            new TemplateSearchViewFieldSpec { ColumnName = "ReferenceCode", DisplayText = "Reference Code", IsVisible = true, ControlType = (int)EmAppControlType.TextBox },
            new TemplateSearchViewFieldSpec { ColumnName = "Description", DisplayText = "Description", IsVisible = true, ControlType = (int)EmAppControlType.TextBox },
            new TemplateSearchViewFieldSpec { ColumnName = "Description2", DisplayText = "Description 2", IsVisible = true, ControlType = (int)EmAppControlType.TextBox },
            new TemplateSearchViewFieldSpec { ColumnName = "Image", DisplayText = "Image", IsVisible = false, ControlType = (int)EmAppControlType.Image },
            new TemplateSearchViewFieldSpec { ColumnName = "FolderId", DisplayText = "Folder Id", IsVisible = false, IsFolderId = true, ControlType = (int)EmAppControlType.TextBox },
            new TemplateSearchViewFieldSpec { ColumnName = "MasterReferenceId", DisplayText = "Master Reference Id", IsVisible = false, ControlType = (int)EmAppControlType.TextBox },
        };

        private static void EnsureTabTransactionForms(
            SqlConnection conn,
            IEnumerable<PlmTemplateTabRow> tabs,
            PlmExportProgressCallback progressCallback)
        {
            var readyTabs = tabs
                .Where(t => t.ImportStatus != TemplateStatusSkipped)
                .GroupBy(t => t.TabId)
                .Select(g => g.First())
                .OrderBy(t => t.TabId)
                .ToList();

            var formStatusByTabId = LoadTabTransactionFormStatus(conn);
            int workTotal = readyTabs.Count(t =>
                !formStatusByTabId.TryGetValue(t.TabId, out var status) || !status.HasCompleteLayout);
            int workIndex = 0;

            foreach (var tab in readyTabs)
            {
                if (formStatusByTabId.TryGetValue(tab.TabId, out var status) && status.HasCompleteLayout)
                    continue;

                workIndex++;
                int pct = 78 + (int)(10.0 * workIndex / Math.Max(1, workTotal));
                progressCallback?.Invoke(pct, $"Generating form layout for tab {tab.TabName} ({workIndex}/{workTotal})…");

                int? transactionId = status != null
                    ? status.TransactionId
                    : GetTransactionIdByIntegrationId(conn, null, $"Tab_{tab.TabId}");
                if (!transactionId.HasValue)
                    throw new InvalidOperationException($"Transaction not found for tab {tab.TabId} ({tab.TabName}).");

                var formResult = AppDatabaseViewBL.EnsureTransactionDefaultFlexFormLayout(transactionId.Value, migrationFastPath: true);
                if (!formResult.IsSuccessful)
                {
                    string msg = formResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? $"Failed to generate form for tab {tab.TabId}.";
                    throw new InvalidOperationException(msg);
                }
            }
        }

        private static Dictionary<int, TabTransactionFormStatus> LoadTabTransactionFormStatus(SqlConnection conn)
        {
            var dict = new Dictionary<int, TabTransactionFormStatus>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT t.TransactionID, t.FormID, t.IntegrationId,
    (SELECT COUNT(1) FROM dbo.AppFormLayoutItem li WHERE li.FormID = t.FormID) AS LayoutItemCount
FROM dbo.AppTransaction t
WHERE t.IntegrationId LIKE 'Tab[_]%'";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string integrationId = reader.IsDBNull(2) ? null : reader.GetString(2);
                        if (string.IsNullOrWhiteSpace(integrationId) || !integrationId.StartsWith("Tab_", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!int.TryParse(integrationId.Substring(4), out int tabId))
                            continue;

                        dict[tabId] = new TabTransactionFormStatus
                        {
                            TransactionId = reader.GetInt32(0),
                            FormId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            LayoutItemCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                        };
                    }
                }
            }

            return dict;
        }

        private static void BuildCompleteDataModelTemplates(
            SqlConnection conn,
            string plmConnectionString,
            PlmTemplateHeaderRow template,
            IReadOnlyList<PlmTemplateTabRow> templateTabs,
            int? saasApplicationId,
            string rootTable,
            int tenantDataSourceId,
            PlmExportProgressCallback progressCallback,
            int templateIndex,
            int templateCount)
        {
            int pct = 88 + (int)(9.0 * templateIndex / Math.Max(1, templateCount));
            string templateName = template.TemplateName ?? $"Template_{template.TemplateId}";
            progressCallback?.Invoke(pct, $"Building Data Model Template {templateName} ({templateIndex}/{templateCount})…");

            int searchId = EnsureTemplateSearchShell(conn, template, saasApplicationId);
            string queryText = BuildReferenceBasicInfoDataSetQuery(rootTable);
            string dataSetName = TruncateDataSetName(templateName);

            AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            if (searchDto == null)
                throw new InvalidOperationException($"Template search {searchId} not found after upsert.");

            AppDataSetExDto dataSetDto = null;
            if (searchDto.DataSetId.HasValue)
            {
                dataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(searchDto.DataSetId.Value);
                dataSetDto.QueryText = queryText;
                dataSetDto.Name = dataSetName;
                dataSetDto.Description = template.Description ?? templateName;
                dataSetDto.IsModified = true;
            }
            else
            {
                dataSetDto = new AppDataSetExDto
                {
                    Name = dataSetName,
                    Description = template.Description ?? templateName,
                    QueryType = (int)EmAppDataServiceType.QueryText,
                    QueryText = queryText,
                    DataSourceFrom = tenantDataSourceId,
                    SaasApplicationId = saasApplicationId
                };
            }

            var dataSetSaveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
            if (!dataSetSaveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(dataSetSaveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to save dataset for template {template.TemplateId}.");
            }

            dataSetDto = dataSetSaveResult.Object;
            int dataSetId = Convert.ToInt32(dataSetDto.Id);

            AppSearchViewExDto searchViewDto;
            if (searchDto.SearchViewId.HasValue)
            {
                ClearSearchViewFields(conn, searchDto.SearchViewId.Value);
                searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchDto.SearchViewId.Value);
                searchViewDto.Name = dataSetName;
                searchViewDto.Description = template.Description ?? templateName;
                searchViewDto.DataSetId = dataSetId;
                searchViewDto.IsModified = true;
                searchViewDto.AppSearchViewFieldList = BuildTemplateSearchViewFields();
            }
            else
            {
                searchViewDto = new AppSearchViewExDto
                {
                    Name = dataSetName,
                    Description = template.Description ?? templateName,
                    DataSetId = dataSetId,
                    ViewType = (int)EmAppViewType.GridView,
                    AppSearchViewFieldList = BuildTemplateSearchViewFields()
                };
            }

            var searchViewSaveResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
            if (!searchViewSaveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(searchViewSaveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to save search view for template {template.TemplateId}.");
            }

            searchViewDto = searchViewSaveResult.Object;
            int searchViewId = Convert.ToInt32(searchViewDto.Id);

            searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            searchDto.DataSetId = dataSetId;
            searchDto.SearchViewId = searchViewId;
            searchDto.Type = (int)EmAppSearchUsageType.DataModelTemplate;
            searchDto.IsAutoExecute = false;
            searchDto.SaasApplicationId = saasApplicationId;
            searchDto.Name = templateName;
            searchDto.Description = template.Description ?? templateName;
            searchDto.IsModified = true;
            if (searchDto.AppSearchFieldList == null)
                searchDto.AppSearchFieldList = new ObservableSet<AppSearchFieldExDto>();

            var searchSaveResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
            if (!searchSaveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(searchSaveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to update template search {template.TemplateId}.");
            }

            ClearSearchViewFormLinkTargets(conn, searchViewId);
            SaveTemplateLinkTargets(searchViewDto, templateTabs, conn, plmConnectionString, template.TemplateId);
        }

        private static void AddTemplateSearchesToMainMenu(
            SqlConnection conn,
            IEnumerable<PlmTemplateHeaderRow> templates,
            int? saasApplicationId,
            PlmExportProgressCallback progressCallback)
        {
            var list = templates.ToList();
            int index = 0;
            foreach (var template in list)
            {
                index++;
                int pct = 97 + (int)(3.0 * index / Math.Max(1, list.Count));
                string templateName = template.TemplateName ?? $"Template_{template.TemplateId}";
                progressCallback?.Invoke(pct, $"Adding {templateName} to main menu ({index}/{list.Count})…");

                int? searchId = GetSearchIdByIntegrationId(conn, null, $"Template_{template.TemplateId}");
                if (!searchId.HasValue)
                    continue;

                var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(
                    searchId.Value, saasApplicationId, templateName, template.Description);
                if (!menuResult.IsSuccessful && menuResult.ValidationResult?.HasErrors == true)
                {
                    throw new InvalidOperationException(menuResult.ValidationResult.Items?.FirstOrDefault()?.Message
                        ?? $"Failed to add template {template.TemplateId} to menu.");
                }
            }
        }

        private static int EnsureTemplateSearchShell(SqlConnection conn, PlmTemplateHeaderRow template, int? saasApplicationId)
        {
            string integrationId = $"Template_{template.TemplateId}";
            int? searchId = GetSearchIdByIntegrationId(conn, null, integrationId);
            string templateName = template.TemplateName ?? $"Template_{template.TemplateId}";

            if (!searchId.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppSearch (Name, Description, Type, IsAutoExecute, SaasApplicationID, IntegrationId)
VALUES (@Name, @Description, @Type, @IsAutoExecute, @SaasApplicationId, @IntegrationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@Name", templateName);
                    cmd.Parameters.AddWithValue("@Description", (object)template.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Type", (int)EmAppSearchUsageType.DataModelTemplate);
                    cmd.Parameters.AddWithValue("@IsAutoExecute", false);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IntegrationId", integrationId);
                    searchId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppSearch SET
    Name = @Name,
    Description = @Description,
    SaasApplicationID = @SaasApplicationId
WHERE SearchId = @SearchId";
                    cmd.Parameters.AddWithValue("@Name", templateName);
                    cmd.Parameters.AddWithValue("@Description", (object)template.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return searchId.Value;
        }

        private static string BuildReferenceBasicInfoDataSetQuery(string rootTable)
        {
            return $"SELECT ReferenceId, ReferenceCode, Description, Description2, Image, FolderId, MasterReferenceId FROM dbo.[{rootTable}]";
        }

        private static string TruncateDataSetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "PLM Template";
            return name.Length <= 100 ? name : name.Substring(0, 100);
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildTemplateSearchViewFields()
        {
            var fields = new ObservableSet<AppSearchViewFieldExDto>();
            foreach (var spec in ReferenceBasicInfoViewFields)
            {
                var field = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = spec.IsVisible,
                    SysTableFiledPath = spec.ColumnName,
                    DisplayText = spec.DisplayText,
                    ControlType = spec.ControlType ?? (int)EmAppControlType.TextBox
                };
                if (spec.IsFolderId)
                {
                    field.IsFileFoderId = true;
                    field.IsVisible = false;
                }
                if (spec.IsTransRootId)
                    field.IsTransRootId = true;

                fields.Add(field);
            }

            return fields;
        }

        private static void ClearSearchViewFields(SqlConnection conn, int searchViewId)
        {
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
                cmd.CommandText = "DELETE FROM dbo.AppFormLinkTarget WHERE SearchViewID = @SearchViewId";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SaveTemplateLinkTargets(
            AppSearchViewExDto searchViewDto,
            IReadOnlyList<PlmTemplateTabRow> templateTabs,
            SqlConnection conn,
            string plmConnectionString,
            int templateId)
        {
            int searchViewId = Convert.ToInt32(searchViewDto.Id);
            var rootField = searchViewDto.AppSearchViewFieldList?
                .FirstOrDefault(f => string.Equals(f.SysTableFiledPath, "ReferenceId", StringComparison.OrdinalIgnoreCase));
            if (rootField?.Id == null)
                throw new InvalidOperationException("Template search view is missing ReferenceId field.");

            int sourceViewColumnId = Convert.ToInt32(rootField.Id);
            var plmTabHeaderFlags = LoadPlmTemplateTabHeaderFlags(plmConnectionString, templateId);
            if (plmTabHeaderFlags.Count == 0)
                return;

            var headerTabIds = new HashSet<int>(plmTabHeaderFlags.Where(kv => kv.Value).Select(kv => kv.Key));
            var tabNameById = templateTabs
                .Where(t => t.TemplateId == templateId)
                .GroupBy(t => t.TabId)
                .ToDictionary(g => g.Key, g => g.First().TabName);
            var tabSortById = templateTabs
                .Where(t => t.TemplateId == templateId)
                .GroupBy(t => t.TabId)
                .ToDictionary(g => g.Key, g => g.First().TabSort);

            int sortFallback = 0;
            PlmTemplateTabRow firstMainTab = null;

            foreach (int tabId in plmTabHeaderFlags.Keys.OrderBy(id => tabSortById.TryGetValue(id, out short? sort) ? sort ?? short.MaxValue : short.MaxValue).ThenBy(id => id))
            {
                if (!plmTabHeaderFlags.TryGetValue(tabId, out bool isTemplateHeader))
                    continue;

                sortFallback++;
                int? transactionId = GetTransactionIdByIntegrationId(conn, null, $"Tab_{tabId}");
                if (!transactionId.HasValue)
                    continue;

                if (!isTemplateHeader && firstMainTab == null)
                {
                    firstMainTab = new PlmTemplateTabRow
                    {
                        TabId = tabId,
                        TabName = tabNameById.TryGetValue(tabId, out string tabName) ? tabName : $"Tab_{tabId}"
                    };
                }

                string navigationName = tabNameById.TryGetValue(tabId, out string displayName) && !string.IsNullOrWhiteSpace(displayName)
                    ? displayName
                    : $"Tab_{tabId}";
                short? tabSort = tabSortById.TryGetValue(tabId, out short? sortValue) ? sortValue : null;

                InsertTemplateFormLinkTarget(
                    conn,
                    searchViewId,
                    navigationName,
                    (int)EmAppLinkTargetActionType.Edit,
                    transactionId.Value,
                    sourceViewColumnId,
                    tabSort ?? sortFallback,
                    BuildTemplateLinkTargetOtherSettingsJson(isTemplateHeader));
            }

            if (firstMainTab != null)
            {
                int? createTransactionId = GetTransactionIdByIntegrationId(conn, null, $"Tab_{firstMainTab.TabId}");
                if (createTransactionId.HasValue)
                {
                    InsertTemplateFormLinkTarget(
                        conn,
                        searchViewId,
                        "New",
                        (int)EmAppLinkTargetActionType.Create,
                        createTransactionId.Value,
                        sourceViewColumnId,
                        0,
                        BuildTemplateLinkTargetOtherSettingsJson(false));
                }
            }

            RepairTemplateLinkTargetItemTypes(conn, plmConnectionString, searchViewId, templateId);
        }

        private static void InsertTemplateFormLinkTarget(
            SqlConnection conn,
            int searchViewId,
            string navigationActionName,
            int actionType,
            int linkTargetTransactionId,
            int sourceViewColumnId,
            int sort,
            string otherSettings)
        {
            using (var cmd = conn.CreateCommand())
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
    PopupHeight,
    OtherSettings)
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
    @PopupHeight,
    @OtherSettings)";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@NavigationActionName", navigationActionName);
                cmd.Parameters.AddWithValue("@ActionType", actionType);
                cmd.Parameters.AddWithValue("@LinkTargetTransactionId", linkTargetTransactionId);
                cmd.Parameters.AddWithValue("@LinkTargetUsageType", (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);
                cmd.Parameters.AddWithValue("@SourceColumnType", (int)EmAppLinkTargetSourceColumnType.SearchViewField);
                cmd.Parameters.AddWithValue("@SourceViewColumnId1", sourceViewColumnId);
                cmd.Parameters.AddWithValue("@TargetColumn1", "ReferenceId");
                cmd.Parameters.AddWithValue("@Sort", sort);
                cmd.Parameters.AddWithValue("@IsPopup", true);
                cmd.Parameters.AddWithValue("@PopupWidth", 1200);
                cmd.Parameters.AddWithValue("@PopupHeight", 700);
                cmd.Parameters.AddWithValue("@OtherSettings", otherSettings);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Repairs TemplateItemType on edit link targets from PLM IsTemplateHeaderTab and removes orphan tab links.
        /// </summary>
        private static void RepairTemplateLinkTargetItemTypes(
            SqlConnection conn,
            string plmConnectionString,
            int searchViewId,
            int templateId)
        {
            var plmTabHeaderFlags = LoadPlmTemplateTabHeaderFlags(plmConnectionString, templateId);
            var allowedTabIds = new HashSet<int>(plmTabHeaderFlags.Keys);
            var rows = new List<(int LinkTargetId, int TabId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT lt.LinkTargetId, t.IntegrationId
FROM dbo.AppFormLinkTarget lt
INNER JOIN dbo.AppTransaction t ON t.TransactionId = lt.LinkTargetTransactionID
WHERE lt.SearchViewID = @SearchViewId
  AND lt.ActionType = @ActionType
  AND t.IntegrationId LIKE 'Tab[_]%' ESCAPE '\'";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@ActionType", (int)EmAppLinkTargetActionType.Edit);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string integrationId = reader.IsDBNull(1) ? null : reader.GetString(1);
                        if (string.IsNullOrWhiteSpace(integrationId)
                            || !integrationId.StartsWith("Tab_", StringComparison.OrdinalIgnoreCase)
                            || !int.TryParse(integrationId.Substring(4), out int tabId))
                        {
                            continue;
                        }

                        rows.Add((reader.GetInt32(0), tabId));
                    }
                }
            }

            foreach (var row in rows)
            {
                if (!allowedTabIds.Contains(row.TabId))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM dbo.AppFormLinkTarget WHERE LinkTargetId = @LinkTargetId";
                        cmd.Parameters.AddWithValue("@LinkTargetId", row.LinkTargetId);
                        cmd.ExecuteNonQuery();
                    }

                    continue;
                }

                bool isHeader = plmTabHeaderFlags.TryGetValue(row.TabId, out bool headerFlag) && headerFlag;
                string otherSettings = BuildTemplateLinkTargetOtherSettingsJson(isHeader);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppFormLinkTarget
SET OtherSettings = @OtherSettings,
    LinkTargetUsageType = @LinkTargetUsageType
WHERE LinkTargetId = @LinkTargetId";
                    cmd.Parameters.AddWithValue("@OtherSettings", otherSettings);
                    cmd.Parameters.AddWithValue("@LinkTargetUsageType", (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);
                    cmd.Parameters.AddWithValue("@LinkTargetId", row.LinkTargetId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string BuildTemplateLinkTargetOtherSettingsJson(bool isTemplateHeader)
        {
            var settings = new JObject
            {
                ["IsLinkToComsumeApiTransaction"] = false,
                ["UiId"] = Guid.NewGuid().ToString(),
                ["TemplateItemType"] = isTemplateHeader
                    ? (int)EmAppTransactionTemplateItemType.TemplateHeader
                    : (int)EmAppTransactionTemplateItemType.MainItem
            };
            return settings.ToString(Formatting.None);
        }
    }
}
