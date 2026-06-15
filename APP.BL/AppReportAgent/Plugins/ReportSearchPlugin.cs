using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using App.BL;
using App.BL.AppBuilderAgent;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppReportAgent.Plugins
{
    /// <summary>
    /// Agent plugin — discovers and executes existing search/report screens.
    ///
    /// Three tools exposed to the LLM:
    ///   list_available_searches  — lists all management searches by name/id
    ///   get_search_criteria      — shows filter fields for a chosen search
    ///   execute_report           — applies criteria and runs the search; stores result
    /// </summary>
    public class ReportSearchPlugin
    {
        /// <summary>
        /// Set by execute_report; read by AppReportAgentBL after the agentic loop completes.
        /// </summary>
        public AgentReportResultEvent LastReportResult { get; private set; }

        // ──────────────────────────────────────────────────────────────────────

        [AgentFunction("list_available_searches",
            "List all available search / report screens so you can find one that matches the user's intent. " +
            "Returns [{Id, Display}]. Call this first before any other tool.")]
        public string ListAvailableSearches()
        {
            try
            {
                var dto = AppSearchBL.RetrieveSearchesByUsageType(1);
                var all = new List<object>();

                if (dto?.MySearches != null)
                    foreach (var s in dto.MySearches)
                        if (s.Id.HasValue)
                            all.Add(new { Id = s.Id, Display = s.Display });

                if (dto?.SavedSearches != null)
                    foreach (var s in dto.SavedSearches)
                        if (s.Id.HasValue && all.All(x => ((dynamic)x).Id != s.Id))
                            all.Add(new { Id = s.Id, Display = s.Display });

                return JsonConvert.SerializeObject(all);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────────────────

        [AgentFunction("get_search_criteria",
            "Get the filter criteria fields for a specific search. " +
            "Returns [{Id, Display, ControlType}] so you can map user intent to filter values. " +
            "ControlType: 2=TextBox, 7=Date, 20=Numeric, 1=DDL(dropdown), 13=CheckBox.")]
        public string GetSearchCriteria(
            [AgentParam("The Id of the search returned by list_available_searches.", true)]
            int search_id)
        {
            try
            {
                var searchDto = AppSearchBL.RetrieveOneSearchDto(search_id, false);
                if (searchDto == null)
                    return JsonConvert.SerializeObject(new { Error = $"Search {search_id} not found." });

                var criterias = searchDto.Criterias?.Select(c => new
                {
                    Id          = c.SearcDCUID,
                    Display     = c.Display,
                    ControlType = c.ControlType
                });

                return JsonConvert.SerializeObject(new
                {
                    SearchId   = searchDto.Id,
                    SearchName = searchDto.Display,
                    Criterias  = criterias
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────────────────

        [AgentFunction("execute_report",
            "Execute a search/report with optional filter criteria and a preferred view type. " +
            "criteria_values_json is a JSON object mapping criteriaId (int) to value (string), e.g. {\"123\":\"Open\",\"456\":\"2024-01-01\"}. " +
            "Pass an empty object {} to run with default/no filters. " +
            "view_type: 'grid' (default), 'pivot', or 'gantt'.")]
        public string ExecuteReport(
            [AgentParam("The Id of the search to execute.", true)]
            int search_id,

            [AgentParam("JSON object mapping criteria Id to value string, e.g. {\"123\":\"Active\"}. Pass {} for no filters.", false)]
            string criteria_values_json = "{}",

            [AgentParam("View type: 'grid', 'pivot', or 'gantt'. Default is 'grid'.", false)]
            string view_type = "grid")
        {
            try
            {
                // 1. Load search
                var searchDto = AppSearchBL.RetrieveOneSearchDto(search_id, false);
                if (searchDto == null)
                    return JsonConvert.SerializeObject(new { Error = $"Search {search_id} not found." });

                // 2. Apply criteria values
                if (!string.IsNullOrWhiteSpace(criteria_values_json) && criteria_values_json != "{}")
                {
                    try
                    {
                        var criteriaMap = JObject.Parse(criteria_values_json);
                        if (searchDto.Criterias != null)
                        {
                            foreach (var criteria in searchDto.Criterias)
                            {
                                JToken val;
                                if (criteriaMap.TryGetValue(criteria.SearcDCUID.ToString(),
                                    StringComparison.OrdinalIgnoreCase, out val))
                                {
                                    var strVal = val.ToString();
                                    if (!string.IsNullOrEmpty(strVal))
                                    {
                                        criteria.Values = new ObservableCollection<object> { strVal };
                                    }
                                }
                            }
                        }
                    }
                    catch { /* ignore malformed criteria json — proceed with defaults */ }
                }

                // 3. Map view type string to ViewType int
                int viewTypeInt = 1; // GridView default
                var vt = (view_type ?? "grid").ToLower().Trim();
                if (vt == "pivot")   viewTypeInt = 4;
                else if (vt == "gantt") viewTypeInt = 3;

                // 4. Set the view reference on the searchDto
                var viewDto = searchDto.DefaultView;
                if (viewDto != null)
                    viewDto.ViewType = viewTypeInt;

                // 5. Execute
                AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(searchDto);
                var result = AppSearchBL.RetrieveSearchResult(searchDto);

                var rows = result?.SearchResultRowList?.Cast<object>().ToList()
                           ?? new List<object>();

                // 6. Store result for the done callback
                LastReportResult = new AgentReportResultEvent
                {
                    SearchDefinitionId = search_id,
                    SearchName         = searchDto.Display ?? $"Search {search_id}",
                    ViewType           = vt == "pivot" ? "pivot" : vt == "gantt" ? "gantt" : "grid",
                    SearchDto          = searchDto,
                    ViewDto            = viewDto,
                    SearchResultRows   = rows,
                    RowCount           = rows.Count,
                    Timestamp          = DateTime.UtcNow
                };

                // 7. Return summary to the LLM
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess  = true,
                    SearchId   = search_id,
                    SearchName = searchDto.Display,
                    RowCount   = rows.Count,
                    ViewType   = LastReportResult.ViewType,
                    Message    = $"Found {rows.Count} result(s) for '{searchDto.Display}'."
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }
    }
}
