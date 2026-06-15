using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Agent tools for creating Datasets, Search Views, and Searches.
    ///
    /// Flow: Dataset (SQL query) → SearchView (grid columns) → Search (entry point + search fields)
    /// The create_search tool handles all three steps at once.
    /// </summary>
    public class SearchBuilderPlugin
    {
        private readonly int? _dataSourceId;

        public SearchBuilderPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        // ─────────────────────────────────────────────────────────────────────
        // create_search — all-in-one: Dataset + SearchView + Search
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("create_search",
            "Create a search/list screen backed by a SQL query. " +
            "Internally creates: (1) a Dataset that holds the SQL query, " +
            "(2) a SearchView grid with all query columns, " +
            "(3) a Search that users navigate to. " +
            "Returns SearchId, DataSetId, and SearchViewId on success. " +
            "Call AFTER create_application so tables exist. " +
            "Always pass SaasApplicationId to link the search to the correct application.")]
        public string CreateSearch(
            [AgentParam("Display name for the search screen, e.g. 'Order List'.", isRequired: true)]
            string name,
            [AgentParam("SQL SELECT query that fetches the data to display, e.g. SELECT * FROM dbo.Order.", isRequired: true)]
            string sqlQuery,
            [AgentParam("The SaasApplicationId returned by create_app_package.", isRequired: true)]
            int saasApplicationId)
        {
            try
            {
                // ── Step 1: Dataset ───────────────────────────────────────────
                var datasetDto = new AppDataSetExDto
                {
                    Name           = name,
                    QueryText      = sqlQuery,
                    QueryType      = 1, // EmAppDataServiceType.QueryText
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

                // ── Step 2 + 3: Search (auto-creates SearchView) ─────────────
                var searchDto = new AppSearchExDto
                {
                    Name                                    = name,
                    Type                                    = 0, // default search
                    IsAutoExecute                           = true,
                    DataSetId                               = dataSetId,
                    SaasApplicationId                       = saasApplicationId,
                    NeedToCreateDefaultViewWithAllDataSetColumns = true
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

                int searchId     = (int)searchResult.Object.Id;
                int? searchViewId = searchResult.Object.SearchViewId;

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess    = true,
                    SearchId     = searchId,
                    SearchName   = name,
                    DataSetId    = dataSetId,
                    SearchViewId = searchViewId
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    Error     = ex.Message
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // list_searches — explore existing searches
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("list_searches",
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
                    SearchId   = s.Id,
                    Name       = s.Name,
                    DataSetId  = s.DataSetId,
                    s.SaasApplicationId
                }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = true,
                    Count     = items.Count,
                    Searches  = items
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }
    }
}
