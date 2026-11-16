using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Agent tools for creating Datasets, Search Views, and Searches.
    ///
    /// Flow: Dataset (SQL query) → SearchView (columns + viewType) → Search (+ optional criteria).
    /// </summary>
    public class SearchBuilderPlugin
    {
        private readonly int? _dataSourceId;

        public SearchBuilderPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("create_search",
            "Create a search/list screen backed by a SQL query. " +
            "Creates: (1) Dataset, (2) SearchView with all query columns, (3) Search. " +
            "Optional criteriaFields (comma-separated SQL column/alias names) creates Filters/Criteria. " +
            "Optional viewType sets representation: GridView|CardView|PivotView|CalendarView|ChartView " +
            "(or enum int). Default GridView. " +
            "Returns SearchId, DataSetId, SearchViewId, CriteriaCreated, ViewType. " +
            "Always pass saasApplicationId. Call AFTER tables exist.")]
        public string CreateSearch(
            [AgentParam("Display name for the search screen, e.g. 'Order List'.", isRequired: true)]
            string name,
            [AgentParam("SQL SELECT query that fetches the data to display, e.g. SELECT * FROM dbo.Order.", isRequired: true)]
            string sqlQuery,
            [AgentParam("The SaasApplicationId returned by create_app_package.", isRequired: true)]
            int saasApplicationId,
            [AgentParam("Optional comma-separated dataset column names/aliases to create as Filters/Criteria, e.g. 'Customer,Title'.")]
            string criteriaFields = null,
            [AgentParam("Optional view type: GridView (default), CardView, PivotView, CalendarView, ChartView, or EmAppViewType int.")]
            string viewType = null)
        {
            try
            {
                var validErr = AppBuilderValidation.ValidateSaasApplicationId(saasApplicationId);
                if (validErr != null) return validErr;

                if (!TryResolveViewType(viewType, out int resolvedViewType, out string viewTypeName, out string viewTypeError))
                {
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = viewTypeError });
                }

                // ── Step 1: Dataset ───────────────────────────────────────────
                var datasetDto = new AppDataSetExDto
                {
                    Name = name,
                    QueryText = sqlQuery,
                    QueryType = 1, // EmAppDataServiceType.QueryText
                    DataSourceFrom = _dataSourceId,
                    SaasApplicationId = saasApplicationId
                };

                var datasetResult = AppDataSetBL.SaveOneAppDataSetEntityDto(datasetDto);
                if (datasetResult.ValidationResult.HasErrors)
                {
                    var errors = string.Join("; ",
                        datasetResult.ValidationResult.Items.Select(i => i.Message));
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = "Dataset creation failed: " + errors
                    });
                }

                int dataSetId = (int)datasetResult.Object.Id;
                var columns = AppDataSetBL.RetrieveQueryColumnList(dataSetId) ?? new List<LookupItemDto>();

                // ── Step 2: Criteria from optional criteriaFields ─────────────
                var criteriaResult = BuildCriteriaFields(criteriaFields, columns);
                if (!string.IsNullOrEmpty(criteriaResult.Error))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = criteriaResult.Error,
                        DataSetId = dataSetId,
                        AvailableColumns = columns.Select(c => c.Id?.ToString()).ToList()
                    }, Formatting.Indented);
                }

                // ── Step 3: Search (auto-creates SearchView as Grid with all cols)
                var searchDto = new AppSearchExDto
                {
                    Name = name,
                    Type = 0,
                    IsAutoExecute = true,
                    DataSetId = dataSetId,
                    SaasApplicationId = saasApplicationId,
                    NeedToCreateDefaultViewWithAllDataSetColumns = true,
                    AppSearchFieldList = criteriaResult.Fields
                };

                var searchResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
                if (searchResult.ValidationResult.HasErrors)
                {
                    var errors = string.Join("; ",
                        searchResult.ValidationResult.Items.Select(i => i.Message));
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = "Search creation failed: " + errors,
                        DataSetId = dataSetId
                    });
                }

                int searchId = (int)searchResult.Object.Id;
                int? searchViewId = searchResult.Object.SearchViewId;

                // ── Step 4: Apply non-default ViewType on the created SearchView
                string viewTypeApplied = "GridView";
                int viewTypeAppliedInt = (int)EmAppViewType.GridView;
                if (searchViewId.HasValue && resolvedViewType != (int)EmAppViewType.GridView)
                {
                    var view = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchViewId.Value);
                    if (view == null)
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Error = $"Search created (Id={searchId}) but SearchView {searchViewId} not found for viewType update.",
                            SearchId = searchId,
                            DataSetId = dataSetId,
                            SearchViewId = searchViewId,
                            CriteriaCreated = criteriaResult.CreatedNames
                        }, Formatting.Indented);
                    }

                    view.ViewType = resolvedViewType;
                    var viewSave = AppSearchViewConfigBL.SaveAppSearchViewExDto(view);
                    if (viewSave?.ValidationResult?.HasErrors == true)
                    {
                        var errors = string.Join("; ", viewSave.ValidationResult.Items.Select(i => i.Message));
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Error = "Search created but viewType update failed: " + errors,
                            SearchId = searchId,
                            DataSetId = dataSetId,
                            SearchViewId = searchViewId,
                            CriteriaCreated = criteriaResult.CreatedNames
                        }, Formatting.Indented);
                    }

                    viewTypeApplied = viewTypeName;
                    viewTypeAppliedInt = resolvedViewType;
                }
                else
                {
                    viewTypeApplied = viewTypeName;
                    viewTypeAppliedInt = resolvedViewType;
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = true,
                    SearchId = searchId,
                    SearchName = name,
                    DataSetId = dataSetId,
                    SearchViewId = searchViewId,
                    ViewType = viewTypeApplied,
                    ViewTypeId = viewTypeAppliedInt,
                    CriteriaCreated = criteriaResult.CreatedNames,
                    CriteriaCount = criteriaResult.CreatedNames.Count,
                    Info = criteriaResult.CreatedNames.Count == 0 && string.IsNullOrWhiteSpace(criteriaFields)
                        ? "No criteriaFields passed — Filters tab is empty by design."
                        : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [AgentTool("delete_search",
            "Permanently delete a standalone Search. " +
            "deleteView/deleteDataSet default true mean 'delete IF unshared' — View/Dataset are kept when other Searches " +
            "(or SearchViews) still reference them. Also removes sidebar shortcuts when possible. " +
            "Does NOT drop database tables. Irreversible — prefer ask_user / propose_plan before calling.")]
        public string DeleteSearch(
            [AgentParam("Search id to delete.", isRequired: true)]
            int searchId,
            [AgentParam("Delete default SearchView only if no other Search uses it. Default true.")]
            bool deleteView = true,
            [AgentParam("Delete Dataset only if no other Search/SearchView uses it. Default true.")]
            bool deleteDataSet = true,
            [AgentParam("Remove sidebar menu shortcuts for this Search. Default true.")]
            bool removeFromMenu = true)
        {
            try
            {
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
                if (search == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        ok = false,
                        Error = $"Search {searchId} not found."
                    }, Formatting.Indented);
                }

                string searchName = search.Name;
                int? dataSetId = search.DataSetId;
                int? searchViewId = search.SearchViewId;
                int? saasApplicationId = search.SaasApplicationId;

                var menuRemovedIds = new List<object>();
                if (removeFromMenu && saasApplicationId.HasValue && saasApplicationId.Value > 0)
                {
                    var menuResult = AppTreeListMenuBL.RemoveSearchFromApplicationMenu(
                        searchId, saasApplicationId.Value, isSavedSearch: false);
                    if (menuResult?.Object is System.Collections.IEnumerable removed)
                    {
                        foreach (var id in removed)
                            if (id != null) menuRemovedIds.Add(id);
                    }
                    var savedMenu = AppTreeListMenuBL.RemoveSearchFromApplicationMenu(
                        searchId, saasApplicationId.Value, isSavedSearch: true);
                    if (savedMenu?.Object is System.Collections.IEnumerable removedSaved)
                    {
                        foreach (var id in removedSaved)
                            if (id != null) menuRemovedIds.Add(id);
                    }
                }

                var result = AppSearchConfigBL.DeleteAppSearch(
                    searchId,
                    isDeleteDefaultView: deleteView,
                    isDeleteDataSet: deleteDataSet);

                bool success = result?.ValidationResult?.HasErrors == false;
                var messages = result?.ValidationResult?.Items?.Select(i => i.Message).ToList()
                               ?? new List<string>();
                var warnings = result?.ValidationResult?.Items?
                    .Where(i => i.ItemType == APP.Framework.Validation.ValidationItemType.Warning)
                    .Select(i => i.Message).ToList() ?? new List<string>();

                if (!success)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        ok = false,
                        searchId,
                        Error = messages.Count > 0 ? string.Join("; ", messages) : "Delete failed",
                        menuRemovedIds
                    }, Formatting.Indented);
                }

                // Parse cascade details from BL result Object
                bool deletedView = false;
                bool deletedDataSet = false;
                string viewKeptReason = null;
                string dataSetKeptReason = null;
                if (result?.Object != null)
                {
                    var json = JsonConvert.SerializeObject(result.Object);
                    var jo = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json);
                    deletedView = jo?.Value<bool?>("DeletedView") == true;
                    deletedDataSet = jo?.Value<bool?>("DeletedDataSet") == true;
                    viewKeptReason = jo?.Value<string>("ViewKeptReason");
                    dataSetKeptReason = jo?.Value<string>("DataSetKeptReason");
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = true,
                    ok = true,
                    searchId,
                    deletedName = searchName,
                    searchViewId,
                    dataSetId,
                    deletedView,
                    deletedDataSet,
                    viewKeptReason,
                    dataSetKeptReason,
                    menuRemovedIds,
                    Warnings = warnings.Count > 0 ? warnings : null,
                    Info = $"Deleted Search '{searchName}' (Id={searchId})"
                           + (deletedView ? $"; deleted View {searchViewId}" : $"; View kept ({viewKeptReason ?? "n/a"})")
                           + (deletedDataSet ? $"; deleted Dataset {dataSetId}" : $"; Dataset kept ({dataSetKeptReason ?? "n/a"})")
                           + "."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = ex.Message });
            }
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("list_searches",
            "List existing searches in the platform. " +
            "Use this during exploration (Step 1) to see what searches already exist " +
            "so you avoid creating duplicates.")]
        public string ListSearches(
            [AgentParam("Optional SaasApplicationId to filter searches by application.")]
            int? saasApplicationId = null)
        {
            try
            {
                List<AppSearchDto> list;
                if (saasApplicationId.HasValue)
                    list = AppSearchConfigBL.RetrieveSaasApplicationSearchList(saasApplicationId);
                else
                    list = AppSearchConfigBL.RetrieveAllAppSearchDto();

                var items = list.Select(s => new
                {
                    SearchId = s.Id,
                    Name = s.Name,
                    DataSetId = s.DataSetId,
                    s.SaasApplicationId
                }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = true,
                    Count = items.Count,
                    Searches = items
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        private sealed class CriteriaBuildResult
        {
            public ObservableSet<AppSearchFieldExDto> Fields { get; set; } = new ObservableSet<AppSearchFieldExDto>();
            public List<string> CreatedNames { get; set; } = new List<string>();
            public string Error { get; set; }
        }

        private static CriteriaBuildResult BuildCriteriaFields(string criteriaFields, List<LookupItemDto> columns)
        {
            var result = new CriteriaBuildResult();
            if (string.IsNullOrWhiteSpace(criteriaFields))
                return result;

            var requested = criteriaFields
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requested.Count == 0)
                return result;

            var unmatched = new List<string>();
            int sort = 0;
            foreach (var req in requested)
            {
                var col = columns.FirstOrDefault(c =>
                {
                    var id = c.Id?.ToString() ?? "";
                    var display = c.Display ?? "";
                    return id.Equals(req, StringComparison.OrdinalIgnoreCase)
                           || display.Equals(req, StringComparison.OrdinalIgnoreCase);
                });

                if (col == null)
                {
                    unmatched.Add(req);
                    continue;
                }

                sort++;
                string colName = col.Id?.ToString() ?? req;
                // 4 columns per row (matches SearchEditor filter layout)
                int linear = sort - 1;
                result.Fields.Add(new AppSearchFieldExDto
                {
                    Sort = sort,
                    SysTableFiledPath = colName,
                    DisplayText = string.IsNullOrWhiteSpace(col.Display) ? colName : col.Display.Split('|')[0].Trim(),
                    ControlType = (int)EmAppControlType.TextBox,
                    OperationId = 0,
                    DefaultValue = "",
                    PositionRow = (linear / 4) + 1,
                    PositionColumn = (linear % 4) + 1,
                    IsVisible = true,
                    IsReadOnly = false,
                    IsAutoPopulate = false
                });
                result.CreatedNames.Add(colName);
            }

            if (unmatched.Count > 0)
            {
                result.Error =
                    "criteriaFields not found in dataset columns: " + string.Join(", ", unmatched) +
                    ". Available: " + string.Join(", ", columns.Select(c => c.Id?.ToString()).Where(s => !string.IsNullOrEmpty(s)));
            }

            return result;
        }

        private static bool TryResolveViewType(string viewType, out int viewTypeId, out string viewTypeName, out string error)
        {
            viewTypeId = (int)EmAppViewType.GridView;
            viewTypeName = nameof(EmAppViewType.GridView);
            error = null;

            if (string.IsNullOrWhiteSpace(viewType))
                return true;

            var raw = viewType.Trim();
            if (int.TryParse(raw, out int asInt))
            {
                if (!Enum.IsDefined(typeof(EmAppViewType), asInt))
                {
                    error = $"Unknown viewType int={asInt}. Use GridView/CardView or EmAppViewType value.";
                    return false;
                }
                viewTypeId = asInt;
                viewTypeName = Enum.GetName(typeof(EmAppViewType), asInt) ?? asInt.ToString();
                return true;
            }

            // Accept "Card", "CardView", "card view"
            var normalized = raw.Replace(" ", "").Replace("_", "");
            if (normalized.Equals("Card", StringComparison.OrdinalIgnoreCase))
                normalized = "CardView";
            if (normalized.Equals("Grid", StringComparison.OrdinalIgnoreCase))
                normalized = "GridView";

            foreach (EmAppViewType vt in Enum.GetValues(typeof(EmAppViewType)))
            {
                if (vt.ToString().Equals(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    viewTypeId = (int)vt;
                    viewTypeName = vt.ToString();
                    return true;
                }
            }

            error = $"Unknown viewType '{viewType}'. Use GridView, CardView, PivotView, CalendarView, ChartView, or EmAppViewType int.";
            return false;
        }
    }
}
