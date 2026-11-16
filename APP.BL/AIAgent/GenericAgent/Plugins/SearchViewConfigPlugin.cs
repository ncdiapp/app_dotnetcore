using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Durable Search / View / Dataset / SearchView Link Target tools for app-search-view-config.
    /// </summary>
    public class SearchViewConfigPlugin
    {
        [AgentTool("get_search_config",
            "Inspect open Search: basic settings, bound View/Dataset ids, criteria summary, view columns summary, link-target counts.")]
        public string GetSearchConfig(
            AgentToolContext context,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!TryResolveSearchId(context, searchId, out var sid, out var err))
                return Fail(err);
            try
            {
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(sid);
                if (search == null) return Fail($"Search {sid} not found.");

                AppSearchViewExDto view = null;
                if (search.SearchViewId.HasValue)
                    view = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(search.SearchViewId.Value);

                AppDataSetExDto dataSet = null;
                if (search.DataSetId.HasValue)
                    dataSet = AppDataSetBL.RetrieveOneAppDataSetExDto(search.DataSetId.Value);

                var criteria = (search.AppSearchFieldList ?? Enumerable.Empty<AppSearchFieldExDto>())
                    .Select(f => new
                    {
                        searchFieldId = f.Id,
                        name = f.DisplayText,
                        path = f.SysTableFiledPath,
                        controlType = f.ControlType,
                        operationId = f.OperationId
                    }).ToList();

                var columns = (view?.AppSearchViewFieldList ?? Enumerable.Empty<AppSearchViewFieldExDto>())
                    .Select(c => new
                    {
                        searchViewFieldId = c.Id,
                        name = c.DisplayText,
                        path = c.SysTableFiledPath,
                        isVisible = c.IsVisible
                    }).ToList();

                int linkTargetCount = 0;
                if (search.SearchViewId.HasValue)
                {
                    linkTargetCount = LinkTragetBL
                        .RetrieveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.SearchView, search.SearchViewId.Value)
                        .Count;
                }

                return Ok(new
                {
                    searchId = search.Id,
                    searchName = search.Name,
                    description = search.Description,
                    isAutoExecute = search.IsAutoExecute,
                    searchViewId = search.SearchViewId,
                    searchViewName = view?.Name,
                    dataSetId = search.DataSetId,
                    dataSetName = dataSet?.Name,
                    dataSetQueryPreview = Truncate(dataSet?.QueryText, 400),
                    criteria,
                    viewColumns = columns,
                    linkTargetCount,
                    usageTypeGuide = new[]
                    {
                        new { usageType = 1, name = "LinkToForm", needs = "linkTargetTransactionId" },
                        new { usageType = 2, name = "LinkToFormGroup", needs = "linkTargetTransactionGroupId" },
                        new { usageType = 3, name = "LinkToSearch", needs = "linkTargetSearchId" },
                        new { usageType = 4, name = "LinkToReport", needs = "report target" },
                        new { usageType = 5, name = "SystemDefinedPage", needs = "linkTargetUrlOrRouteCode" }
                    }
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("update_search_basic_setting",
            "Update AppSearch root fields via changesJson (searchName, description, isAutoExecute, searchViewId, dataSetId). " +
            "Preview → ask_user → confirmed=true → request_search_reload.")]
        public string UpdateSearchBasicSetting(
            AgentToolContext context,
            [AgentParam("Partial JSON of Search root fields.", isRequired: true)] string changesJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (string.IsNullOrWhiteSpace(changesJson)) return Fail("changesJson is required.");
            if (!TryResolveSearchId(context, searchId, out var sid, out var err))
                return Fail(err);
            try
            {
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(sid);
                if (search == null) return Fail($"Search {sid} not found.");
                var changes = JObject.Parse(changesJson);

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        searchId = sid,
                        proposed = changes
                    });
                }

                ApplySearchRootChanges(search, changes);
                var result = AppSearchConfigBL.SaveAppSearchExDto(search);
                if (result?.ValidationResult?.HasErrors == true)
                    return Fail(ValidationMessages(result.ValidationResult));

                return Ok(new
                {
                    saved = true,
                    searchId = sid,
                    changed = new[] { new { type = "search", id = sid } },
                    NextStep = "Call request_search_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("update_search_criteria",
            "CRUD AppSearchField (criteria). op=add|update|delete. Prefer searchFieldId for update/delete. " +
            "changesJson for field props. Preview → ask_user → confirmed=true → request_search_reload.")]
        public string UpdateSearchCriteria(
            AgentToolContext context,
            [AgentParam("add | update | delete", isRequired: true)] string op = null,
            [AgentParam("Existing searchFieldId for update/delete.")] int? searchFieldId = null,
            [AgentParam("Field property patch JSON.")] string changesJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (string.IsNullOrWhiteSpace(op)) return Fail("op is required (add|update|delete).");
            if (!TryResolveSearchId(context, searchId, out var sid, out var err))
                return Fail(err);
            try
            {
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(sid);
                if (search == null) return Fail($"Search {sid} not found.");
                search.AppSearchFieldList ??= new ObservableSet<AppSearchFieldExDto>();
                var action = op.Trim().ToLowerInvariant();

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new { op = action, searchFieldId, changesJson }
                    });
                }

                if (action == "delete")
                {
                    if (!searchFieldId.HasValue) return Fail("searchFieldId required for delete.");
                    search.AppSearchFieldList.DeletedItemIds ??= new List<object>();
                    search.AppSearchFieldList.DeletedItemIds.Add(searchFieldId.Value);
                    // Also remove from items if present
                    var existing = search.AppSearchFieldList.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == searchFieldId.Value);
                    if (existing != null) search.AppSearchFieldList.Remove(existing);
                }
                else if (action == "add")
                {
                    var field = new AppSearchFieldExDto { SearchId = sid };
                    if (!string.IsNullOrWhiteSpace(changesJson))
                        JsonConvert.PopulateObject(changesJson, field);
                    search.AppSearchFieldList.Add(field);
                }
                else if (action == "update")
                {
                    if (!searchFieldId.HasValue) return Fail("searchFieldId required for update.");
                    var field = search.AppSearchFieldList.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == searchFieldId.Value);
                    if (field == null) return Fail($"searchFieldId {searchFieldId} not found.");
                    if (!string.IsNullOrWhiteSpace(changesJson))
                        JsonConvert.PopulateObject(changesJson, field);
                }
                else return Fail("op must be add|update|delete.");

                var result = AppSearchConfigBL.SaveAppSearchExDto(search);
                if (result?.ValidationResult?.HasErrors == true)
                    return Fail(ValidationMessages(result.ValidationResult));

                return Ok(new
                {
                    saved = true,
                    searchId = sid,
                    op = action,
                    searchFieldId,
                    NextStep = "Call request_search_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("update_search_view_columns",
            "Update/add/remove Search View result columns. op=add|update|delete. Prefer searchViewFieldId. " +
            "Preview → ask_user → confirmed=true → request_search_reload.")]
        public string UpdateSearchViewColumns(
            AgentToolContext context,
            [AgentParam("add | update | delete", isRequired: true)] string op = null,
            [AgentParam("Existing searchViewFieldId for update/delete.")] int? searchViewFieldId = null,
            [AgentParam("Column property patch JSON.")] string changesJson = null,
            [AgentParam("Override view id; defaults to Search.SearchViewId.")] int? searchViewId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (string.IsNullOrWhiteSpace(op)) return Fail("op is required.");
            if (!TryResolveSearchId(context, searchId, out var sid, out var err))
                return Fail(err);
            try
            {
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(sid);
                if (search == null) return Fail($"Search {sid} not found.");
                var viewId = searchViewId ?? search.SearchViewId;
                if (!viewId.HasValue) return Fail("Search has no SearchViewId.");
                if (searchViewId.HasValue && search.SearchViewId.HasValue && searchViewId.Value != search.SearchViewId.Value)
                    return Fail("searchViewId does not match host Search.");

                var view = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(viewId.Value);
                if (view == null) return Fail($"SearchView {viewId} not found.");
                view.AppSearchViewFieldList ??= new ObservableSet<AppSearchViewFieldExDto>();
                var action = op.Trim().ToLowerInvariant();

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new { op = action, searchViewFieldId, changesJson, searchViewId = viewId }
                    });
                }

                if (action == "delete")
                {
                    if (!searchViewFieldId.HasValue) return Fail("searchViewFieldId required for delete.");
                    view.AppSearchViewFieldList.DeletedItemIds ??= new List<object>();
                    view.AppSearchViewFieldList.DeletedItemIds.Add(searchViewFieldId.Value);
                    var existing = view.AppSearchViewFieldList.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == searchViewFieldId.Value);
                    if (existing != null) view.AppSearchViewFieldList.Remove(existing);
                }
                else if (action == "add")
                {
                    var col = new AppSearchViewFieldExDto { SearchViewId = viewId.Value };
                    if (!string.IsNullOrWhiteSpace(changesJson))
                        JsonConvert.PopulateObject(changesJson, col);
                    view.AppSearchViewFieldList.Add(col);
                }
                else if (action == "update")
                {
                    if (!searchViewFieldId.HasValue) return Fail("searchViewFieldId required for update.");
                    if (string.IsNullOrWhiteSpace(changesJson)) return Fail("changesJson required for update.");
                    var resultField = AppSearchViewConfigBL.SaveAppSearchViewFieldExDto(
                        PatchViewField(searchViewFieldId.Value, changesJson));
                    if (resultField?.ValidationResult?.HasErrors == true)
                        return Fail(ValidationMessages(resultField.ValidationResult));
                    return Ok(new
                    {
                        saved = true,
                        searchViewFieldId,
                        NextStep = "Call request_search_reload."
                    });
                }
                else return Fail("op must be add|update|delete.");

                var result = AppSearchViewConfigBL.SaveAppSearchViewExDto(view);
                if (result?.ValidationResult?.HasErrors == true)
                    return Fail(ValidationMessages(result.ValidationResult));

                return Ok(new
                {
                    saved = true,
                    searchViewId = viewId,
                    op = action,
                    searchViewFieldId,
                    NextStep = "Call request_search_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("update_dataset",
            "Update AppDataSet (QueryText, Name, …) via changesJson. Warn if Dataset is shared. " +
            "Optional syncViewColumns=true after SQL — still requires ask_user. Preview → confirmed=true → request_search_reload.")]
        public string UpdateDataset(
            AgentToolContext context,
            [AgentParam("Partial Dataset fields JSON.", isRequired: true)] string changesJson = null,
            [AgentParam("Override dataset id; defaults to Search.DataSetId.")] int? dataSetId = null,
            [AgentParam("If true after SQL change, attempt view-column sync (still HITL).")] bool? syncViewColumns = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (string.IsNullOrWhiteSpace(changesJson)) return Fail("changesJson is required.");
            if (!TryResolveSearchId(context, searchId, out var sid, out var err))
                return Fail(err);
            try
            {
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(sid);
                if (search == null) return Fail($"Search {sid} not found.");
                var dsId = dataSetId ?? search.DataSetId;
                if (!dsId.HasValue) return Fail("Search has no DataSetId.");
                if (dataSetId.HasValue && search.DataSetId.HasValue && dataSetId.Value != search.DataSetId.Value)
                    return Fail("dataSetId does not match host Search (shared Dataset edits must be explicit and confirmed).");

                var ds = AppDataSetBL.RetrieveOneAppDataSetExDto(dsId.Value);
                if (ds == null) return Fail($"DataSet {dsId} not found.");
                var changes = JObject.Parse(changesJson);

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. If Dataset may be shared across Searches, warn the user. ask_user then confirmed=true.",
                        dataSetId = dsId,
                        proposed = changes,
                        syncViewColumns
                    });
                }

                if (changes["name"] != null || changes["Name"] != null)
                    ds.Name = (changes["name"] ?? changes["Name"]).Value<string>();
                if (changes["queryText"] != null || changes["QueryText"] != null)
                    ds.QueryText = (changes["queryText"] ?? changes["QueryText"]).Value<string>();
                if (changes["description"] != null || changes["Description"] != null)
                    ds.Description = (changes["description"] ?? changes["Description"]).Value<string>();

                var result = AppDataSetBL.SaveOneAppDataSetEntityDto(ds);
                if (result?.ValidationResult?.HasErrors == true)
                    return Fail(ValidationMessages(result.ValidationResult));

                return Ok(new
                {
                    saved = true,
                    dataSetId = dsId,
                    syncViewColumns = syncViewColumns == true
                        ? "Requested — implementer/host should sync columns after reload if product supports it."
                        : "not requested",
                    NextStep = "Call request_search_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("get_search_view_link_targets",
            "List AppFormLinkTarget menus on the Search View (Link To Form / FormGroup / Search / Report / System page).")]
        public string GetSearchViewLinkTargets(
            AgentToolContext context,
            [AgentParam("Optional usageType filter 1–5.")] int? usageType = null,
            [AgentParam("Override view id.")] int? searchViewId = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!TryResolveSearchViewId(context, searchId, searchViewId, out var viewId, out var err))
                return Fail(err);
            return AppFormLinkTargetConfigService.List(
                (int)EmAppLinkTargetSourceType.SearchView, viewId, usageType);
        }

        [AgentTool("get_search_view_link_target_detail",
            "Full Search View Link Target detail.")]
        public string GetSearchViewLinkTargetDetail(
            AgentToolContext context,
            [AgentParam("LinkTarget id.", isRequired: true)] int? linkTargetId = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!linkTargetId.HasValue) return Fail("linkTargetId is required.");
            if (!TryResolveSearchId(context, searchId, out _, out var err))
                return Fail(err);
            return AppFormLinkTargetConfigService.Detail(linkTargetId.Value);
        }

        [AgentTool("upsert_search_view_link_target",
            "Create/update Search View Link Target. usageType: 1=Form 2=FormGroup 3=Search 4=Report 5=SystemPage. " +
            "Require matching destination id. Preview → ask_user → confirmed=true → request_search_reload.")]
        public string UpsertSearchViewLinkTarget(
            AgentToolContext context,
            [AgentParam("Existing linkTargetId for update.")] int? linkTargetId = null,
            [AgentParam("1=Form 2=FormGroup 3=Search 4=Report 5=SystemPage.", isRequired: true)] int? usageType = null,
            [AgentParam("ActionType 1=Edit 2=Create 3=Delete 5=Preview 13=CreateFromExisting 17=ExecuteCommand.")] int? actionType = null,
            [AgentParam("Menu caption.")] string navigationActionName = null,
            [AgentParam("Destination transaction.")] int? linkTargetTransactionId = null,
            [AgentParam("Destination transaction group.")] int? linkTargetTransactionGroupId = null,
            [AgentParam("Destination search.")] int? linkTargetSearchId = null,
            [AgentParam("System page route/URL.")] string linkTargetUrlOrRouteCode = null,
            [AgentParam("Optional Data Transfer id.")] int? dataTransferSettingId = null,
            [AgentParam("Sort.")] int? sort = null,
            [AgentParam("Popup.")] bool? isPopup = null,
            [AgentParam("Popup width.")] int? popupWidth = null,
            [AgentParam("Popup height.")] int? popupHeight = null,
            [AgentParam("Icon.")] string iconName = null,
            [AgentParam("Group name.")] string groupName = null,
            [AgentParam("Source column type.")] int? sourceColumnType = null,
            [AgentParam("Source view column id 1.")] int? sourceViewColumnId1 = null,
            [AgentParam("Source view column id 2.")] int? sourceViewColumnId2 = null,
            [AgentParam("Source view column id 3.")] int? sourceViewColumnId3 = null,
            [AgentParam("Target search field id 1.")] int? targetSearchFieldId1 = null,
            [AgentParam("Target search field id 2.")] int? targetSearchFieldId2 = null,
            [AgentParam("Target search field id 3.")] int? targetSearchFieldId3 = null,
            [AgentParam("Layout display mode (row vs top menu).")] int? layoutDisplayMode = null,
            [AgentParam("OtherSettings JSON.")] string otherSettingsJson = null,
            [AgentParam("Override view id.")] int? searchViewId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!usageType.HasValue) return Fail("usageType is required (1–5).");
            if (!TryResolveSearchViewId(context, searchId, searchViewId, out var viewId, out var err))
                return Fail(err);
            return AppFormLinkTargetConfigService.Upsert(
                (int)EmAppLinkTargetSourceType.SearchView, viewId, linkTargetId,
                usageType, actionType, navigationActionName,
                linkTargetTransactionId, linkTargetTransactionGroupId, linkTargetSearchId,
                linkTargetUrlOrRouteCode, dataTransferSettingId, sort, isPopup, popupWidth, popupHeight,
                iconName, groupName, sourceColumnType,
                null, null, null, null, null, null,
                sourceViewColumnId1, sourceViewColumnId2, sourceViewColumnId3,
                targetSearchFieldId1, targetSearchFieldId2, targetSearchFieldId3,
                layoutDisplayMode, otherSettingsJson, confirmed, dryRun);
        }

        [AgentTool("delete_search_view_link_target",
            "Delete Search View Link Target. Preview → ask_user → confirmed=true → request_search_reload.")]
        public string DeleteSearchViewLinkTarget(
            AgentToolContext context,
            [AgentParam("LinkTarget id.", isRequired: true)] int? linkTargetId = null,
            [AgentParam("Override view id.")] int? searchViewId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!linkTargetId.HasValue) return Fail("linkTargetId is required.");
            if (!TryResolveSearchViewId(context, searchId, searchViewId, out var viewId, out var err))
                return Fail(err);
            return AppFormLinkTargetConfigService.Delete(
                (int)EmAppLinkTargetSourceType.SearchView, viewId, linkTargetId.Value, confirmed, dryRun);
        }

        [AgentTool("get_view_linked_search_or_url",
            "List AppViewLinkedSeaechOrUrl (inline linked Search/URL cards — NOT Link Target menus).")]
        public string GetViewLinkedSearchOrUrl(
            AgentToolContext context,
            [AgentParam("Override view id.")] int? searchViewId = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!TryResolveSearchViewId(context, searchId, searchViewId, out var viewId, out var err))
                return Fail(err);
            try
            {
                var list = AppViewLinkedSeaechOrUrlBL.RetrieveOneAppViewLinkedSeaechOrUrlExDto(viewId)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.DisplayText,
                        linkTargetSearchId = x.LinkTargetSearchId,
                        linkTargetUrlOrRouteCode = x.LinkTargetUrlOrRouteCode,
                        layoutDisplayMode = x.LayoutDisplayMode
                    }).ToList();
                return Ok(new { searchViewId = viewId, linkedSearchOrUrls = list });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("upsert_view_linked_search_or_url",
            "Create/update inline AppViewLinkedSeaechOrUrl. Preview → ask_user → confirmed=true → request_search_reload.")]
        public string UpsertViewLinkedSearchOrUrl(
            AgentToolContext context,
            [AgentParam("Existing id for update.")] int? id = null,
            [AgentParam("Display name.")] string name = null,
            [AgentParam("Linked search id.")] int? linkTargetSearchId = null,
            [AgentParam("URL or route.")] string linkTargetUrlOrRouteCode = null,
            [AgentParam("Layout display mode.")] int? layoutDisplayMode = null,
            [AgentParam("Override view id.")] int? searchViewId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!TryResolveSearchViewId(context, searchId, searchViewId, out var viewId, out var err))
                return Fail(err);
            try
            {
                var set = new ObservableSet<AppViewLinkedSeaechOrUrlExDto>();
                var existingList = AppViewLinkedSeaechOrUrlBL.RetrieveOneAppViewLinkedSeaechOrUrlExDto(viewId);
                foreach (var e in existingList) set.Add(e);

                AppViewLinkedSeaechOrUrlExDto dto;
                if (id.HasValue && id.Value > 0)
                {
                    dto = set.FirstOrDefault(x => x.Id != null && Convert.ToInt32(x.Id) == id.Value);
                    if (dto == null) return Fail($"LinkedSearchOrUrl {id} not found.");
                }
                else
                {
                    dto = new AppViewLinkedSeaechOrUrlExDto { SearchViewId = viewId };
                    set.Add(dto);
                }

                if (name != null) dto.DisplayText = name;
                if (linkTargetSearchId.HasValue) dto.LinkTargetSearchId = linkTargetSearchId;
                if (linkTargetUrlOrRouteCode != null) dto.LinkTargetUrlOrRouteCode = linkTargetUrlOrRouteCode;
                if (layoutDisplayMode.HasValue) dto.LayoutDisplayMode = layoutDisplayMode;

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = dto.IsNew ? "create" : "update",
                            name = dto.DisplayText,
                            linkTargetSearchId = dto.LinkTargetSearchId,
                            linkTargetUrlOrRouteCode = dto.LinkTargetUrlOrRouteCode
                        }
                    });
                }

                var result = AppViewLinkedSeaechOrUrlBL.SaveAllAppViewLinkedSeaechOrUrlEntityDto(set, viewId);
                if (result?.ValidationResult?.HasErrors == true)
                    return Fail(ValidationMessages(result.ValidationResult));

                return Ok(new
                {
                    saved = true,
                    searchViewId = viewId,
                    NextStep = "Call request_search_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("delete_view_linked_search_or_url",
            "Delete inline AppViewLinkedSeaechOrUrl by omitting it from the saved set. Preview → ask_user → confirmed=true.")]
        public string DeleteViewLinkedSearchOrUrl(
            AgentToolContext context,
            [AgentParam("Id to delete.", isRequired: true)] int? id = null,
            [AgentParam("Override view id.")] int? searchViewId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required in Test Run without HostContext.")] int? searchId = null)
        {
            if (!id.HasValue) return Fail("id is required.");
            if (!TryResolveSearchViewId(context, searchId, searchViewId, out var viewId, out var err))
                return Fail(err);
            try
            {
                var existingList = AppViewLinkedSeaechOrUrlBL.RetrieveOneAppViewLinkedSeaechOrUrlExDto(viewId);
                var victim = existingList.FirstOrDefault(x => x.Id != null && Convert.ToInt32(x.Id) == id.Value);
                if (victim == null) return Fail($"LinkedSearchOrUrl {id} not found.");

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed = new { action = "delete", id, name = victim.DisplayText }
                    });
                }

                var set = new ObservableSet<AppViewLinkedSeaechOrUrlExDto>();
                foreach (var e in existingList.Where(x => x.Id == null || Convert.ToInt32(x.Id) != id.Value))
                    set.Add(e);

                var result = AppViewLinkedSeaechOrUrlBL.SaveAllAppViewLinkedSeaechOrUrlEntityDto(set, viewId);
                if (result?.ValidationResult?.HasErrors == true)
                    return Fail(ValidationMessages(result.ValidationResult));

                return Ok(new
                {
                    deleted = true,
                    id,
                    NextStep = "Call request_search_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("request_search_reload",
            "After successful Search/View/Dataset/LinkTarget writes, signal the host to prompt Reload current Search.")]
        public Task<string> RequestSearchReload(AgentToolContext context) =>
            AskHost(context, "request_search_reload", new { });

        static AppSearchViewFieldExDto PatchViewField(int searchViewFieldId, string changesJson)
        {
            var field = AppSearchViewConfigBL.RetrieveOneAppSearchViewFieldExDto(searchViewFieldId);
            if (field == null) throw new InvalidOperationException($"searchViewFieldId {searchViewFieldId} not found.");
            JsonConvert.PopulateObject(changesJson, field);
            return field;
        }

        static void ApplySearchRootChanges(AppSearchExDto search, JObject changes)
        {
            if (changes["searchName"] != null || changes["SearchName"] != null || changes["name"] != null || changes["Name"] != null)
                search.Name = (changes["searchName"] ?? changes["SearchName"] ?? changes["name"] ?? changes["Name"]).Value<string>();
            if (changes["description"] != null || changes["Description"] != null)
                search.Description = (changes["description"] ?? changes["Description"]).Value<string>();
            if (changes["isAutoExecute"] != null || changes["IsAutoExecute"] != null)
                search.IsAutoExecute = (changes["isAutoExecute"] ?? changes["IsAutoExecute"]).Value<bool>();
            if (changes["searchViewId"] != null || changes["SearchViewId"] != null)
                search.SearchViewId = (changes["searchViewId"] ?? changes["SearchViewId"]).Value<int?>();
            if (changes["dataSetId"] != null || changes["DataSetId"] != null)
                search.DataSetId = (changes["dataSetId"] ?? changes["DataSetId"]).Value<int?>();
        }

        static bool TryResolveSearchId(AgentToolContext context, int? clientSearchId, out int searchId, out string error)
        {
            searchId = 0;
            error = null;
            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            int? hostSearchId = null;
            if (!string.IsNullOrWhiteSpace(hostJson))
            {
                try
                {
                    var root = JToken.Parse(hostJson);
                    hostSearchId = (root["searchId"] ?? root["SearchId"])?.Value<int?>();
                }
                catch
                {
                    error = "Invalid HostContextJson.";
                    return false;
                }
            }

            if (hostSearchId.HasValue && hostSearchId.Value > 0)
            {
                if (clientSearchId.HasValue && clientSearchId.Value > 0 && clientSearchId.Value != hostSearchId.Value)
                {
                    error = $"searchId {clientSearchId} does not match HostContext searchId {hostSearchId}.";
                    return false;
                }
                searchId = hostSearchId.Value;
                return true;
            }

            if (clientSearchId.HasValue && clientSearchId.Value > 0)
            {
                searchId = clientSearchId.Value;
                return true;
            }

            error = "searchId is required (HostContext or explicit argument for Test Run).";
            return false;
        }

        static bool TryResolveSearchViewId(
            AgentToolContext context, int? clientSearchId, int? clientViewId, out int searchViewId, out string error)
        {
            searchViewId = 0;
            if (!TryResolveSearchId(context, clientSearchId, out var sid, out error))
                return false;

            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            int? hostViewId = null;
            if (!string.IsNullOrWhiteSpace(hostJson))
            {
                try
                {
                    var root = JToken.Parse(hostJson);
                    hostViewId = (root["searchViewId"] ?? root["SearchViewId"])?.Value<int?>();
                }
                catch { /* ignore */ }
            }

            if (clientViewId.HasValue && clientViewId.Value > 0)
            {
                if (hostViewId.HasValue && hostViewId.Value > 0 && clientViewId.Value != hostViewId.Value)
                {
                    error = $"searchViewId {clientViewId} does not match HostContext.";
                    return false;
                }
                searchViewId = clientViewId.Value;
                return true;
            }

            if (hostViewId.HasValue && hostViewId.Value > 0)
            {
                searchViewId = hostViewId.Value;
                return true;
            }

            var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(sid);
            if (search?.SearchViewId == null)
            {
                error = "Search has no SearchViewId.";
                return false;
            }
            searchViewId = search.SearchViewId.Value;
            return true;
        }

        static async Task<string> AskHost(AgentToolContext context, string toolName, object args)
        {
            var callbacks = AgentHitlBridge.Resolve(context?.ChatSessionKey);
            if (callbacks?.OnHostTool == null)
                return Fail("This tool only runs inside an open Search Configurator host.");

            var evt = new FormHostToolEvent
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                ArgsJson = JsonConvert.SerializeObject(args)
            };
            try
            {
                var result = await callbacks.OnHostTool(evt).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(result)
                    ? Fail("Empty host result.")
                    : result;
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max) + "…");

        static string ValidationMessages(APP.Framework.Validation.ValidationResult vr)
        {
            var msgs = vr?.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
            return msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save failed.";
        }

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
