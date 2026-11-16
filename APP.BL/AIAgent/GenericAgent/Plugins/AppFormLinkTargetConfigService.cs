using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Shared AppFormLinkTarget CRUD for Transaction Unit (Form Config) and Search View (Search View Config).
    /// See AppConfigurator §7.6.
    /// </summary>
    public static class AppFormLinkTargetConfigService
    {
        public static string List(
            int sourceType,
            int sourceId,
            int? usageType = null)
        {
            try
            {
                var list = LinkTragetBL.RetrieveOneAppFormLinkTargetList(sourceType, sourceId)
                    .Where(x => !usageType.HasValue || x.LinkTargetUsageType == usageType.Value)
                    .OrderBy(x => x.Sort ?? int.MaxValue)
                    .Select(Summarize)
                    .ToList();
                return Ok(new
                {
                    sourceType,
                    sourceTypeName = SourceTypeName(sourceType),
                    sourceId,
                    usageType,
                    linkTargets = list,
                    usageTypeGuide = UsageTypeGuide(sourceType)
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string Detail(int linkTargetId)
        {
            try
            {
                var dto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(linkTargetId);
                if (dto == null) return Fail($"LinkTarget {linkTargetId} not found.");
                return Ok(new { linkTarget = DetailObject(dto) });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string Upsert(
            int sourceType,
            int sourceId,
            int? linkTargetId,
            int? usageType,
            int? actionType,
            string navigationActionName,
            int? linkTargetTransactionId,
            int? linkTargetTransactionGroupId,
            int? linkTargetSearchId,
            string linkTargetUrlOrRouteCode,
            int? dataTransferSettingId,
            int? sort,
            bool? isPopup,
            int? popupWidth,
            int? popupHeight,
            string iconName,
            string groupName,
            int? sourceColumnType,
            string sourceColumn1,
            string sourceColumn2,
            string sourceColumn3,
            string targetColumn1,
            string targetColumn2,
            string targetColumn3,
            int? sourceViewColumnId1,
            int? sourceViewColumnId2,
            int? sourceViewColumnId3,
            int? targetSearchFieldId1,
            int? targetSearchFieldId2,
            int? targetSearchFieldId3,
            int? layoutDisplayMode,
            string otherSettingsJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                var resolvedUsage = usageType
                    ?? (sourceType == (int)EmAppLinkTargetSourceType.TransactionUnit
                        ? (int)EmAppLinkTargetUsageType.TransactionUnitLinkToForm
                        : (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);

                var usageErr = ValidateUsageForSource(sourceType, resolvedUsage);
                if (usageErr != null) return Fail(usageErr);

                AppFormLinkTargetDto dto;
                if (linkTargetId.HasValue && linkTargetId.Value > 0)
                {
                    dto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(linkTargetId.Value);
                    if (dto == null) return Fail($"LinkTarget {linkTargetId} not found.");
                    if (!OwnerMatches(dto, sourceType, sourceId))
                        return Fail("linkTargetId does not belong to this source.");
                }
                else
                {
                    dto = new AppFormLinkTargetDto();
                    if (sourceType == (int)EmAppLinkTargetSourceType.SearchView)
                        dto.SearchViewId = sourceId;
                    else
                        dto.TransactionUnitId = sourceId;
                }

                ApplyUpsertFields(dto, resolvedUsage, actionType, navigationActionName,
                    linkTargetTransactionId, linkTargetTransactionGroupId, linkTargetSearchId,
                    linkTargetUrlOrRouteCode, dataTransferSettingId, sort, isPopup, popupWidth, popupHeight,
                    iconName, groupName, sourceColumnType,
                    sourceColumn1, sourceColumn2, sourceColumn3,
                    targetColumn1, targetColumn2, targetColumn3,
                    sourceViewColumnId1, sourceViewColumnId2, sourceViewColumnId3,
                    targetSearchFieldId1, targetSearchFieldId2, targetSearchFieldId3,
                    layoutDisplayMode, otherSettingsJson);

                var destErr = ValidateDestination(dto);
                if (destErr != null) return Fail(destErr);

                var preview = new
                {
                    action = dto.IsNew ? "create" : "update",
                    sourceType,
                    sourceId,
                    linkTarget = DetailObject(dto)
                };

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. Call again with confirmed=true after ask_user.",
                        proposed = preview
                    });
                }

                var set = new ObservableSet<AppFormLinkTargetDto>();
                set.Add(dto);
                var result = LinkTragetBL.SaveOneAppFormLinkTargetList(sourceType, sourceId, set);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save LinkTarget failed.");
                }

                var saved = result?.ObjectList?.OrderByDescending(x => x.Id).FirstOrDefault()
                            ?? result?.ObjectList?.FirstOrDefault();
                return Ok(new
                {
                    saved = true,
                    linkTargetId = saved?.Id,
                    linkTarget = saved != null ? DetailObject(saved) : null,
                    changed = new[] { new { type = "linkTarget", id = saved?.Id } },
                    NextStep = sourceType == (int)EmAppLinkTargetSourceType.SearchView
                        ? "Call request_search_reload."
                        : "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string Delete(int sourceType, int sourceId, int linkTargetId, bool? confirmed, bool? dryRun)
        {
            try
            {
                var dto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(linkTargetId);
                if (dto == null) return Fail($"LinkTarget {linkTargetId} not found.");
                if (!OwnerMatches(dto, sourceType, sourceId))
                    return Fail("linkTargetId does not belong to this source.");

                var preview = new { action = "delete", linkTarget = DetailObject(dto) };
                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. Call again with confirmed=true after ask_user.",
                        proposed = preview
                    });
                }

                var set = new ObservableSet<AppFormLinkTargetDto>();
                set.DeletedItemIds = new List<object> { linkTargetId };
                var result = LinkTragetBL.SaveOneAppFormLinkTargetList(sourceType, sourceId, set);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Delete LinkTarget failed.");
                }

                return Ok(new
                {
                    deleted = true,
                    linkTargetId,
                    changed = new[] { new { type = "linkTarget", id = linkTargetId } },
                    NextStep = sourceType == (int)EmAppLinkTargetSourceType.SearchView
                        ? "Call request_search_reload."
                        : "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static void ApplyUpsertFields(
            AppFormLinkTargetDto dto,
            int usageType,
            int? actionType,
            string navigationActionName,
            int? linkTargetTransactionId,
            int? linkTargetTransactionGroupId,
            int? linkTargetSearchId,
            string linkTargetUrlOrRouteCode,
            int? dataTransferSettingId,
            int? sort,
            bool? isPopup,
            int? popupWidth,
            int? popupHeight,
            string iconName,
            string groupName,
            int? sourceColumnType,
            string sourceColumn1,
            string sourceColumn2,
            string sourceColumn3,
            string targetColumn1,
            string targetColumn2,
            string targetColumn3,
            int? sourceViewColumnId1,
            int? sourceViewColumnId2,
            int? sourceViewColumnId3,
            int? targetSearchFieldId1,
            int? targetSearchFieldId2,
            int? targetSearchFieldId3,
            int? layoutDisplayMode,
            string otherSettingsJson)
        {
            dto.LinkTargetUsageType = usageType;
            if (actionType.HasValue) dto.ActionType = actionType;
            if (navigationActionName != null) dto.NavigationActionName = navigationActionName;
            if (string.IsNullOrWhiteSpace(dto.NavigationActionName) && actionType.HasValue)
                dto.NavigationActionName = DefaultNavName(actionType.Value);

            if (linkTargetTransactionId.HasValue) dto.LinkTargetTransactionId = linkTargetTransactionId;
            if (linkTargetTransactionGroupId.HasValue) dto.LinkTargetTransactionGroupId = linkTargetTransactionGroupId;
            if (linkTargetSearchId.HasValue) dto.LinkTargetSearchId = linkTargetSearchId;
            if (linkTargetUrlOrRouteCode != null) dto.LinkTargetUrlOrRouteCode = linkTargetUrlOrRouteCode;
            if (dataTransferSettingId.HasValue) dto.DataTransferSettingId = dataTransferSettingId;
            if (sort.HasValue) dto.Sort = sort;
            if (isPopup.HasValue) dto.IsPopup = isPopup;
            if (popupWidth.HasValue) dto.PopupWidth = popupWidth;
            if (popupHeight.HasValue) dto.PopupHeight = popupHeight;
            if (iconName != null) dto.IconName = iconName;
            if (groupName != null) dto.GroupName = groupName;
            if (sourceColumnType.HasValue) dto.SourceColumnType = sourceColumnType;
            if (sourceColumn1 != null) dto.SourceColumn1 = sourceColumn1;
            if (sourceColumn2 != null) dto.SourceColumn2 = sourceColumn2;
            if (sourceColumn3 != null) dto.SourceColumn3 = sourceColumn3;
            if (targetColumn1 != null) dto.TargetColumn1 = targetColumn1;
            if (targetColumn2 != null) dto.TargetColumn2 = targetColumn2;
            if (targetColumn3 != null) dto.TargetColumn3 = targetColumn3;
            if (sourceViewColumnId1.HasValue) dto.SourceViewColumnId1 = sourceViewColumnId1;
            if (sourceViewColumnId2.HasValue) dto.SourceViewColumnId2 = sourceViewColumnId2;
            if (sourceViewColumnId3.HasValue) dto.SourceViewColumnId3 = sourceViewColumnId3;
            if (targetSearchFieldId1.HasValue) dto.TargetSearchFieldId1 = targetSearchFieldId1;
            if (targetSearchFieldId2.HasValue) dto.TargetSearchFieldId2 = targetSearchFieldId2;
            if (targetSearchFieldId3.HasValue) dto.TargetSearchFieldId3 = targetSearchFieldId3;
            if (layoutDisplayMode.HasValue) dto.LayoutDisplayMode = layoutDisplayMode;
            if (!string.IsNullOrWhiteSpace(otherSettingsJson))
            {
                try
                {
                    // Keep raw JSON string on DTO; BL also uses OtherSettingsDto when present.
                    dto.OtherSettings = otherSettingsJson;
                    dto.OtherSettingsDto = JsonConvert.DeserializeObject<AppFormLinkTargetOtherSettingsDto>(otherSettingsJson);
                }
                catch
                {
                    dto.OtherSettings = otherSettingsJson;
                }
            }
        }

        static string ValidateUsageForSource(int sourceType, int usageType)
        {
            if (sourceType == (int)EmAppLinkTargetSourceType.TransactionUnit)
            {
                if (usageType != (int)EmAppLinkTargetUsageType.TransactionUnitLinkToForm
                    && usageType != (int)EmAppLinkTargetUsageType.SearchViewLinkSystemDefinedPage)
                    return "Transaction Unit Link Target usageType should be 101 (LinkToForm) or 5 (SystemDefinedPage/route).";
                return null;
            }

            if (sourceType == (int)EmAppLinkTargetSourceType.SearchView)
            {
                if (usageType < 1 || usageType > 5)
                    return "Search View Link Target usageType must be 1=Form, 2=FormGroup, 3=Search, 4=Report, 5=SystemPage.";
                return null;
            }

            return "Unsupported sourceType. Use 2=SearchView or 3=TransactionUnit.";
        }

        static string ValidateDestination(AppFormLinkTargetDto dto)
        {
            var u = dto.LinkTargetUsageType ?? 0;
            switch (u)
            {
                case (int)EmAppLinkTargetUsageType.SearchViewLinkToForm:
                case (int)EmAppLinkTargetUsageType.TransactionUnitLinkToForm:
                    if (!dto.LinkTargetTransactionId.HasValue)
                        return "linkTargetTransactionId is required for LinkToForm.";
                    break;
                case (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup:
                    if (!dto.LinkTargetTransactionGroupId.HasValue)
                        return "linkTargetTransactionGroupId is required for LinkToFormGroup.";
                    break;
                case (int)EmAppLinkTargetUsageType.SearchViewLinkToSearch:
                    if (!dto.LinkTargetSearchId.HasValue)
                        return "linkTargetSearchId is required for LinkToSearch.";
                    break;
                case (int)EmAppLinkTargetUsageType.SearchViewLinkSystemDefinedPage:
                    if (string.IsNullOrWhiteSpace(dto.LinkTargetUrlOrRouteCode)
                        && !dto.LinkTargetTransactionId.HasValue)
                        return "linkTargetUrlOrRouteCode (or transaction) required for SystemDefinedPage.";
                    break;
            }

            if (!dto.ActionType.HasValue)
                return "actionType is required (1=Edit 2=Create 3=Delete 5=Preview 13=CreateFromExisting …).";
            return null;
        }

        static bool OwnerMatches(AppFormLinkTargetDto dto, int sourceType, int sourceId)
        {
            if (sourceType == (int)EmAppLinkTargetSourceType.SearchView)
                return dto.SearchViewId.HasValue && dto.SearchViewId.Value == sourceId;
            if (sourceType == (int)EmAppLinkTargetSourceType.TransactionUnit)
                return dto.TransactionUnitId.HasValue && dto.TransactionUnitId.Value == sourceId;
            return false;
        }

        static string DefaultNavName(int actionType)
        {
            return actionType switch
            {
                (int)EmAppLinkTargetActionType.Edit => "Open",
                (int)EmAppLinkTargetActionType.Create => "Create",
                (int)EmAppLinkTargetActionType.Delete => "Delete",
                (int)EmAppLinkTargetActionType.Preview => "Preview",
                (int)EmAppLinkTargetActionType.CreateFromExistingItem => "Create From Existing",
                (int)EmAppLinkTargetActionType.ExecuteTransactionCommand => "Execute Command",
                _ => "Menu"
            };
        }

        static object Summarize(AppFormLinkTargetDto d) => new
        {
            linkTargetId = d.Id,
            usageType = d.LinkTargetUsageType,
            usageTypeName = UsageName(d.LinkTargetUsageType),
            actionType = d.ActionType,
            navigationActionName = d.NavigationActionName,
            linkTargetTransactionId = d.LinkTargetTransactionId,
            linkTargetTransactionGroupId = d.LinkTargetTransactionGroupId,
            linkTargetSearchId = d.LinkTargetSearchId,
            linkTargetUrlOrRouteCode = d.LinkTargetUrlOrRouteCode,
            dataTransferSettingId = d.DataTransferSettingId,
            sort = d.Sort,
            isPopup = d.IsPopup,
            searchViewId = d.SearchViewId,
            transactionUnitId = d.TransactionUnitId
        };

        static object DetailObject(AppFormLinkTargetDto d) => new
        {
            linkTargetId = d.Id,
            usageType = d.LinkTargetUsageType,
            usageTypeName = UsageName(d.LinkTargetUsageType),
            actionType = d.ActionType,
            navigationActionName = d.NavigationActionName,
            linkTargetTransactionId = d.LinkTargetTransactionId,
            linkTargetTransactionGroupId = d.LinkTargetTransactionGroupId,
            linkTargetSearchId = d.LinkTargetSearchId,
            linkTargetUrlOrRouteCode = d.LinkTargetUrlOrRouteCode,
            dataTransferSettingId = d.DataTransferSettingId,
            sort = d.Sort,
            isPopup = d.IsPopup,
            popupWidth = d.PopupWidth,
            popupHeight = d.PopupHeight,
            iconName = d.IconName,
            groupName = d.GroupName,
            sourceColumnType = d.SourceColumnType,
            sourceColumn1 = d.SourceColumn1,
            sourceColumn2 = d.SourceColumn2,
            sourceColumn3 = d.SourceColumn3,
            targetColumn1 = d.TargetColumn1,
            targetColumn2 = d.TargetColumn2,
            targetColumn3 = d.TargetColumn3,
            sourceViewColumnId1 = d.SourceViewColumnId1,
            sourceViewColumnId2 = d.SourceViewColumnId2,
            sourceViewColumnId3 = d.SourceViewColumnId3,
            targetSearchFieldId1 = d.TargetSearchFieldId1,
            targetSearchFieldId2 = d.TargetSearchFieldId2,
            targetSearchFieldId3 = d.TargetSearchFieldId3,
            layoutDisplayMode = d.LayoutDisplayMode,
            rowDisplayDbField = d.RowDisplayDbField,
            rowDisplayViewColumnId = d.RowDisplayViewColumnId,
            conditionTransFieldId = d.ConditionTransFieldId,
            sourceConditionColumn = d.SourceConditionColumn,
            sourceConditionViewColumnId = d.SourceConditionViewColumnId,
            conditionWarningMessage = d.ConditionWarningMessage,
            opennedFormAutoExecuteCommandId = d.OpennedFormAutoExecuteCommandId,
            isReadonly = d.IsReadonly,
            otherSettings = d.OtherSettings,
            searchViewId = d.SearchViewId,
            transactionUnitId = d.TransactionUnitId
        };

        static string UsageName(int? u) => u switch
        {
            1 => "SearchViewLinkToForm",
            2 => "SearchViewLinkToFormGroup",
            3 => "SearchViewLinkToSearch",
            4 => "SearchViewLinkToReport",
            5 => "SearchViewLinkSystemDefinedPage",
            101 => "TransactionUnitLinkToForm",
            _ => u?.ToString() ?? ""
        };

        static object UsageTypeGuide(int sourceType)
        {
            if (sourceType == (int)EmAppLinkTargetSourceType.TransactionUnit)
            {
                return new[]
                {
                    new { usageType = 101, name = "TransactionUnitLinkToForm", needs = "linkTargetTransactionId" },
                    new { usageType = 5, name = "SystemDefinedPage/route", needs = "linkTargetUrlOrRouteCode" }
                };
            }

            return new[]
            {
                new { usageType = 1, name = "LinkToForm", needs = "linkTargetTransactionId" },
                new { usageType = 2, name = "LinkToFormGroup", needs = "linkTargetTransactionGroupId" },
                new { usageType = 3, name = "LinkToSearch", needs = "linkTargetSearchId" },
                new { usageType = 4, name = "LinkToReport", needs = "report target" },
                new { usageType = 5, name = "SystemDefinedPage", needs = "linkTargetUrlOrRouteCode" }
            };
        }

        static string SourceTypeName(int sourceType) => sourceType switch
        {
            (int)EmAppLinkTargetSourceType.SearchView => "SearchView",
            (int)EmAppLinkTargetSourceType.TransactionUnit => "TransactionUnit",
            _ => sourceType.ToString()
        };

        static string Ok(object payload)
        {
            var jo = JObject.FromObject(payload);
            jo["ok"] = true;
            jo["IsSuccess"] = true;
            return jo.ToString(Formatting.Indented);
        }

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });
    }
}
