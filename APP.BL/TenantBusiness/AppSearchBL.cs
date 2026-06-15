using System.Collections.Generic;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Components.EntityConverter;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using System;
//using APP.Persistence.Common;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using RestSharp;

using Newtonsoft.Json.Linq;

using System.ComponentModel.Design;

using APP.Framework;
namespace App.BL
{
    public static class AppSearchBL
    {

        public static SearchDto RetrieveDefaultSearch(int? searchUsageType = null)
        {
            SearchDto toReturnSearchDto = null;

            // For all user Search
            EntityCollection<AppSearchSavedEntity> userSaveedSearchlist = AppSearchConfigBL.RetrieveAllCurrentUserSavedSearchEntity(searchUsageType);

            // user has saved search
            if (userSaveedSearchlist.Count > 0)
            {
                AppSearchSavedEntity defaultAppSearchSavedEntity = userSaveedSearchlist.Where(o => o.IsDefault.HasValue && o.IsDefault.Value).FirstOrDefault();

                if (defaultAppSearchSavedEntity == null)
                {

                    defaultAppSearchSavedEntity = userSaveedSearchlist.First();

                }

                toReturnSearchDto = AppSearchConfigBL.ConvertSavedSearchEntitySearchDto(defaultAppSearchSavedEntity);
            }
            else // from APPSecuritySysObjGrour to fiter product
            {

                var listUserAllAvaibleSearchEntity = AppSecurityManagementBL.RetrieveUserAllAvaibleSearchEntity(searchUsageType);
                if (listUserAllAvaibleSearchEntity.Count > 0)
                {
                    var appSearchEntity = listUserAllAvaibleSearchEntity[0];
                    appSearchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(appSearchEntity.SearchId);
                    toReturnSearchDto = AppSearchConfigBL.ConvertSearchEntitySearchDto(appSearchEntity);

                }


            }

            if (toReturnSearchDto != null)
            {
                if (toReturnSearchDto.WhereUsedSearchId.HasValue)
                {
                    toReturnSearchDto.EmbeddedChildSearchDto = RetrieveOneSearchDto(toReturnSearchDto.WhereUsedSearchId.Value, false, false);
                }
                AppCascadingSearchBL.SetupIntialCscadingSearchCretiaDataSource(toReturnSearchDto, false);
            }


            return toReturnSearchDto;


        }


        // user Search security
        public static RetrieveSearchesDto RetrieveSearches()
        {
            RetrieveSearchesDto result = new RetrieveSearchesDto();
            result.SavedSearches = AppSearchConfigBL.GetCurrentUserSavedSearchList();
            result.MySearches = AppSecurityManagementBL.GetCurrentUserSearchDefinitionDtoList();

            return result;
        }

        public static RetrieveSearchesDto RetrieveSearchesByUsageType(int? emSearchUsageType)
        {
            if (emSearchUsageType.HasValue)
            {
                RetrieveSearchesDto result = new RetrieveSearchesDto();
                result.SavedSearches = AppSearchConfigBL.GetCurrentUserSavedSearchList(emSearchUsageType);
                result.MySearches = AppSecurityManagementBL.GetCurrentUserSearchDefinitionDtoList(emSearchUsageType);

                return result;
            }
            else
            {
                return RetrieveSearches();
            }
        }


        private static string BuildFileFullTextSearchWhereCondition(string fullTextSearchKeyword)
        {
            string toReturn = string.Empty;

            if (!string.IsNullOrWhiteSpace(fullTextSearchKeyword))
            {
                string[] splitKeywordString = fullTextSearchKeyword.Trim().Split();

                splitKeywordString.ForAll(o => o.Replace("'", "''"));

                string keywordConditionStringFormat = @" FileID in (SELECT FileID from AppFile WHERE CONTAINS(*, ' {0} ') )";

                if (splitKeywordString.Length > 0)
                {
                    string containsKeywords = string.Empty;

                    foreach (string aKeyword in splitKeywordString)
                    {
                        if (!string.IsNullOrEmpty(aKeyword.Trim()))
                        {
                            if (containsKeywords.Length == 0)
                            {
                                containsKeywords = '"' + aKeyword.Trim() + '"';
                            }
                            else
                            {
                                containsKeywords += " AND " + '"' + aKeyword.Trim() + '"';
                            }
                        }
                    }

                    if (containsKeywords.Length > 0)
                    {
                        toReturn = string.Format(keywordConditionStringFormat, containsKeywords);
                    }
                }
            }

            return toReturn;
        }

        // will return initial fiedIDs 



        public static List<int> FullTextLatestVersionFileSearch(string keywords)
        {
            List<int> toReturn = new List<int>();

            if (!string.IsNullOrEmpty(keywords))
            {
                string[] splitKeywordString = keywords.Trim().Split();

                if (splitKeywordString.Length > 0)
                {
                    string containsKeywords = string.Empty;

                    foreach (string aKeyword in splitKeywordString)
                    {
                        if (!string.IsNullOrEmpty(aKeyword.Trim()))
                        {
                            if (containsKeywords.Length == 0)
                            {
                                containsKeywords = '"' + aKeyword.Trim() + '"';
                            }
                            else
                            {
                                containsKeywords += " AND  " + '"' + aKeyword.Trim() + '"';
                            }
                        }
                    }

                    if (containsKeywords.Length > 0)
                    {
                        string queryStringFormat = @" SELECT FileID FROM AppFile WHERE CONTAINS(FileContent, ' {0}')  And FileID in ( select  FileID  from [dbo].[App_FileView_Latest]  )  ";
                        string queryContain = string.Format(queryStringFormat, containsKeywords);

                        string queryInitFileIds = string.Format(@"select [InitialFileID]   FROM  [dbo].[App_FileView_Latest] where[FileID] IN({0} )", queryContain);

                        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                        {
                            toReturn = adapter.ExecuteDataTableRetrievalQuery(queryInitFileIds, new List<SqlParameter>())
                                .AsEnumerable().Select(o => (int)o[0]).ToList();


                        }
                    }
                }

            }


            return toReturn;
        }

        //public static List<AppFileDto> FullTextFileSearch(string keywords)
        //{
        //    List<AppFileDto> toReturn = new List<AppFileDto>();

        //    if (!string.IsNullOrEmpty(keywords))
        //    {
        //        string[] splitKeywordString = keywords.Trim().Split();

        //        if (splitKeywordString.Length > 0)
        //        {
        //            string containsKeywords = string.Empty;

        //            foreach (string aKeyword in splitKeywordString)
        //            {
        //                if (!string.IsNullOrEmpty(aKeyword.Trim()))
        //                {
        //                    if (containsKeywords.Length == 0)
        //                    {
        //                        containsKeywords = "\"" + aKeyword.Trim() + "\"";
        //                    }
        //                    else
        //                    {
        //                        containsKeywords += " or \"" + aKeyword.Trim() + "\"";
        //                    }
        //                }
        //            }

        //            if (containsKeywords.Length > 0)
        //            {
        //                string query = @" SELECT * FROM AppMessage WHERE CONTAINS(*, ' @keywordsValue ')";
        //                List<SqlParameter> lsitparamter = new List<SqlParameter>();
        //                lsitparamter.Add(new SqlParameter("@keywordsValue", containsKeywords));

        //                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //                {
        //                    DataTable result = adapter.ExecuteDataTableRetrievalQuery(query, new List<SqlParameter>());

        //                    if (result.Rows.Count > 0)
        //                    {
        //                        foreach (DataRow dataRow in result.Rows)
        //                        {
        //                            AppFileDto fileDto = new AppFileDto();

        //                            foreach (DataColumn col in result.Columns)
        //                            {
        //                                object value = dataRow[col];

        //                                if (col.ColumnName.ToLower() == "FileID".ToLower())
        //                                {
        //                                    fileDto.Id = ControlTypeValueConverter.ConvertValueToInt(value);
        //                                }
        //                                else if (col.ColumnName.ToLower() == "FileCode".ToLower())
        //                                {
        //                                    fileDto.FileCode = value.ToString();
        //                                }
        //                                else if (col.ColumnName.ToLower() == "Description".ToLower())
        //                                {
        //                                    fileDto.Description = value.ToString();
        //                                }
        //                                else if (col.ColumnName.ToLower() == "AppCreatedDate".ToLower())
        //                                {
        //                                    fileDto.AppCreatedDate = ControlTypeValueConverter.ConvertValueToDate(value);
        //                                }
        //                                else if (col.ColumnName.ToLower() == "AppCreatedByID".ToLower())
        //                                {
        //                                    fileDto.AppCreatedById = ControlTypeValueConverter.ConvertValueToInt(value);
        //                                }
        //                            }

        //                            toReturn.Add(fileDto);
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //    }


        //    return toReturn;
        //}

        //!!!!!
        public static SearchDto RetrieveOneSearchDto(int searchId, bool? isSavedSearch, bool needToCheckUserSecurity = true)
        {


            SearchDto toReturnSearchDto = null;
            if (isSavedSearch.HasValue && isSavedSearch.Value)
            {

                AppSearchSavedEntity appSearchSavedEntity = AppSearchConfigBL.RetrieveOneUserSavedSearchEntity(searchId);

                toReturnSearchDto = AppSearchConfigBL.ConvertSavedSearchEntitySearchDto(appSearchSavedEntity);
            }
            else
            {
                var appSearchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(searchId);

                toReturnSearchDto = AppSearchConfigBL.ConvertSearchEntitySearchDto(appSearchEntity);
            }





            if (toReturnSearchDto != null)
            {
                if (toReturnSearchDto.IsForPublicAcesss.HasValue && toReturnSearchDto.IsForPublicAcesss.Value)
                {
                    needToCheckUserSecurity = false;
                }

                if (needToCheckUserSecurity)
                {
                    List<int> accessibleSearchIds = AppSecurityManagementBL.GetCurrentUserAvailableSearchIds();
                    if (!accessibleSearchIds.Contains(searchId))
                    {
                        return new SearchDto() { Id = -1, Display = "Access Denied" };
                    }
                }


                if (toReturnSearchDto.WhereUsedSearchId.HasValue)
                {
                    toReturnSearchDto.EmbeddedChildSearchDto = RetrieveOneSearchDto(toReturnSearchDto.WhereUsedSearchId.Value, false, false);
                }

                AppCascadingSearchBL.SetupIntialCscadingSearchCretiaDataSource(toReturnSearchDto, false);


                PrepareSearchDtoLinkToTransactionsAndCommands(toReturnSearchDto);
            }


            return toReturnSearchDto;
        }





        public static ReferenceViewDto RetrieveOneReferenceViewDto(int viewId)
        {
            var appSearchViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(viewId);
            ReferenceViewDto referenceViewDto = AppSearchViewConfigBL.ConvertReverenceViewEntityToReferenceViewDto(appSearchViewEntity);



            return referenceViewDto;
        }


        public static SearchResultDto RetrieveSearchResult(SearchDto searchDto)
        {

            if (!searchDto.BlqueryId.HasValue)
            {
                return null;

            }

           


            PrepareSchedulerDataRangeCriterias(searchDto);

            SearchResultDto toReturn = new SearchResultDto();

            // remove IsSkipSearch

            searchDto.Criterias = searchDto.Criterias.Where(o => !(o.IsSkipSearch.HasValue && o.IsSkipSearch.Value)).ToList();


            searchDto.Criterias.ForAll(aCriteria =>
            {
                if (aCriteria.EmInternalCodeRegistration.HasValue && aCriteria.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.CalendarNavigationEndDate)
                {
                    if (aCriteria.Value != null && aCriteria.Value is DateTime)
                    {
                        aCriteria.Value = ((DateTime)aCriteria.Value).AddDays(1);
                    }
                }

                if (aCriteria.EmInternalCodeRegistration.HasValue && aCriteria.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.EsiteId)
                {
                    if (searchDto.CurrentEsiteId.HasValue)
                    {
                        aCriteria.Value = searchDto.CurrentEsiteId.Value;
                    }
                }

                if (aCriteria.EmInternalCodeRegistration.HasValue && aCriteria.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.EsiteMenuCategory)
                {
                    if (searchDto.EsiteMenuCategoryId.HasValue)
                    {
                        aCriteria.Value = searchDto.EsiteMenuCategoryId.Value;
                    }
                }


                if (searchDto.IsWorkflowLogSearch)
                {
                    if (aCriteria.EmInternalCodeRegistration.HasValue && aCriteria.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.WorkflowTransactionId)
                    {
                        if (searchDto.WorkflowTransactionId.HasValue)
                        {
                            aCriteria.Value = searchDto.WorkflowTransactionId.Value;
                        }
                    }

                    if (aCriteria.EmInternalCodeRegistration.HasValue && aCriteria.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.WorkflowTransactionRId)
                    {
                        if (!string.IsNullOrWhiteSpace(searchDto.WorkflowTransactionRId))
                        {
                            aCriteria.Value = searchDto.WorkflowTransactionRId;
                        }
                    }

                    if (aCriteria.EmInternalCodeRegistration.HasValue && aCriteria.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.WorkflowLogBatchNumber)
                    {
                        if (!string.IsNullOrWhiteSpace(searchDto.WorkflowLogBatchNumber))
                        {
                            aCriteria.Value = searchDto.WorkflowLogBatchNumber;
                        }
                    }
                }

            });           

                AppDataSetEntity aAppDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(searchDto.BlqueryId);
            if (aAppDataSetEntity.QueryType.Value == (int)EmAppDataServiceType.PluginWebApiCall)
            {

                //toReturn.SearchResultRowList = AppPluginClient.CallExternalSearchService(searchDto, aAppDataSetEntity);

                toReturn = CallExternalSearchService(searchDto, aAppDataSetEntity);

                if (toReturn.SearchResultRowList != null)
                {
                    AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(searchDto.ReferenceViewDefinitionDto.Id.ToString()));
                    List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                    Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

                    Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList = AppStaticDataSetSearchBL.GetViewEntityColumnLookupDict(viewEntity);
                    string specificTimeToken = ServerContext.Instance.CurrentUserTimeZoneKey;
                    foreach (var row in toReturn.SearchResultRowList)
                    {
                        foreach (var aColumn in viewEntity.AppSearchViewField)
                        {
                            if (row.DictViewColumnIDKeyValue.ContainsKey(aColumn.SearchViewFieldId))
                            {
                                object value = row.DictViewColumnIDKeyValue[aColumn.SearchViewFieldId];
                                object orgValue = value;
                                value = AppStaticDataSetSearchBL.PrepareOneSearchResultRowCellValue_PrepareDDLCellValue(viewEntity, dictColumnIdValuesList, aColumn, value);
                                AppStaticDataSetSearchBL.SetGanttViewRowProperties(viewEntity.ViewType, dictUserIdName, row, aColumn, value, orgValue, specificTimeToken);
                            }

                        }
                    }

                    if (viewEntity.ViewType == (int)EmAppViewType.DateAndTimeCalendarSelectorView)
                    {
                        AppStaticDataSetSearchBL.BulidDateAndTimeCalendarSelectorViewResult(searchDto.ReferenceViewDefinitionDto, toReturn);
                    }
                }

            }
            else if (aAppDataSetEntity.QueryType.Value == (int)EmAppDataServiceType.IntegrationWebApiCall)
            {
                toReturn = AppStaticDataSetSearchBL.GetIntegrationWebApiSearchResult(searchDto, searchDto.ReferenceViewDefinitionDto, aAppDataSetEntity);
            }
            else // statci
            {


                toReturn = AppStaticDataSetSearchBL.GetStaticSearchResult(searchDto, searchDto.ReferenceViewDefinitionDto);
            }


            if (searchDto.TopDataLimit.HasValue && searchDto.TopDataLimit.Value > 0)
            {
                toReturn.SearchResultRowList = toReturn.SearchResultRowList.Take(searchDto.TopDataLimit.Value).ToList();
            }


            if (searchDto.ReferenceViewDefinitionDto != null)
            {
                AppTransactionFormulaBL.CaculateOneSearchResult(toReturn, (int)searchDto.ReferenceViewDefinitionDto.Id);
            }
            // need to process EmREgistreCodeFiled


            ProcessSearchInternalCodeRegistrationField(searchDto, toReturn);

            AssignSchedulerViewResourceList(searchDto, toReturn);

           

            return toReturn;
        }


        private static SearchResultDto CallExternalSearchService(SearchDto searchDto, AppDataSetEntity aAppDataSetEntity)
        {
            //FileDto aFileDto = new WsCall.FileDto();
            string restResourceUri = aAppDataSetEntity.QueryText;
            searchDto.CurrentUserId = AppSecurityUserBL.CurrentUserId;

            string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint);

            string json = JsonConvert.SerializeObject(searchDto);

            var client = new RestClient(restEndPoint);


            var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Post);
            request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            //request.AddHeader("", ServerContext.Instance.CurrentSessionId.ToString());

            request.AddCookie(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString(), "/", "");

            var response = client.ExecuteAsync(request).GetAwaiter().GetResult();

            var result = JsonConvert.DeserializeObject<SearchResultDto>(response.Content);




            // need to convert Browser timezone

            return result;



        }

        private static void PrepareSchedulerDataRangeCriterias(SearchDto searchDto)
        {
            var schedulerBaseDateCriteria = searchDto.Criterias.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.SchedulerBaseDate);

            if (schedulerBaseDateCriteria != null)
            {
                ConvertOneDateCriteriaFromUTCToClient(schedulerBaseDateCriteria);

                DateTime? baseDate = ControlTypeValueConverter.ConvertValueToDate(schedulerBaseDateCriteria.Value);
                if (!baseDate.HasValue)
                {
                    baseDate = DateTime.Today;
                }
                searchDto.BaseDate = baseDate;


                //SearchCriteriaDto startDateCriteria = schedulerBaseDateCriteria.DeepCopy();
                //startDateCriteria.IsSkipSearch = false;
                //startDateCriteria.EmInternalCodeRegistration = (int)EmInternalCodeRegistration.CalendarNavigationStartDate;
                //startDateCriteria.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.GreaterThan;
                //startDateCriteria.Value = baseDate.Value.AddMonths(-1).AddDays(-1);


                //SearchCriteriaDto endDateCriteria = schedulerBaseDateCriteria.DeepCopy();
                //endDateCriteria.IsSkipSearch = false;
                //endDateCriteria.EmInternalCodeRegistration = (int)EmInternalCodeRegistration.CalendarNavigationEndDate;
                //endDateCriteria.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.LessThan;
                //endDateCriteria.Value = baseDate.Value.AddMonths(1).AddDays(1);

                //((List<SearchCriteriaDto>)searchDto.Criterias).Add(startDateCriteria);
                //((List<SearchCriteriaDto>)searchDto.Criterias).Add(endDateCriteria);

                //schedulerBaseDateCriteria.IsSkipSearch = true;
            }
        }

        private static void AssignSchedulerViewResourceList(SearchDto searchDto, SearchResultDto toReturn)
        {
            if (searchDto.ReferenceViewDefinitionDto != null && searchDto.ReferenceViewDefinitionDto.ViewType == (int)EmAppViewType.SchedulerView)
            {
                AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(searchDto.ReferenceViewDefinitionDto.Id.ToString()));

                var groupByColumn = viewEntity.AppSearchViewField.FirstOrDefault(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value && o.EntityId.HasValue);
                if (groupByColumn != null)
                {
                    List<LookupItemDto> lookupList = AppEntityInfoBL.GetLookupItemList(groupByColumn.EntityId.Value, string.Empty);
                    toReturn.SchedulerViewGroupByResources = lookupList;

                    if (groupByColumn.MappingSearchFieldId.HasValue)
                    {
                        var mapptingToCriteria = searchDto.Criterias.FirstOrDefault(o => o.SearcDCUID == groupByColumn.MappingSearchFieldId.Value);

                        if (mapptingToCriteria != null && mapptingToCriteria.Values != null && mapptingToCriteria.Values.Count > 0)
                        {
                            toReturn.SchedulerViewGroupByResources = new List<LookupItemDto>();

                            foreach (var lookupItem in lookupList)
                            {
                                var matchFiled = mapptingToCriteria.Values.FirstOrDefault(o => o.ToString().Trim().ToLower() == lookupItem.Id.ToString().Trim().ToLower());
                                if (matchFiled != null)
                                {
                                    toReturn.SchedulerViewGroupByResources.Add(lookupItem);
                                }
                            }
                        }
                    }
                }
            }
        }

        // all  InternalCodeRegistrationField will be process where
        private static void ProcessSearchInternalCodeRegistrationField(SearchDto searchDto, SearchResultDto toReturn)
        {
            var calendroNavStartDateFied = searchDto.Criterias.Where(o => o.EmInternalCodeRegistration.HasValue
                && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.CalendarNavigationStartDate).FirstOrDefault();

            if (calendroNavStartDateFied != null)
            {
                ConvertOneDateCriteriaFromUTCToClient(calendroNavStartDateFied);
                toReturn.StartDateTime = calendroNavStartDateFied.Value as DateTime?;
            }


            var calendroNavEndDateFied = searchDto.Criterias.Where(o => o.EmInternalCodeRegistration.HasValue
                && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.CalendarNavigationEndDate).FirstOrDefault();

            if (calendroNavEndDateFied != null)
            {
                ConvertOneDateCriteriaFromUTCToClient(calendroNavEndDateFied);
                toReturn.EndDateTime = calendroNavEndDateFied.Value as DateTime?;
            }



            if (searchDto.BaseDate.HasValue)
            {
                toReturn.BaseDate = searchDto.BaseDate.Value;

                int offsetToPrevSunday = (int)toReturn.BaseDate.Value.DayOfWeek;
                DateTime week1 = toReturn.BaseDate.Value.AddDays(-offsetToPrevSunday);
                DateTime week0 = week1.AddDays(-7);
                DateTime week2 = week1.AddDays(7);
                DateTime week3 = week1.AddDays(14);

                toReturn.StartDateTime = week0;
                toReturn.EndDateTime = week0.AddDays(7);

                toReturn.WeekStartDateList = new List<DateTime>();
                toReturn.WeekStartDateList.Add(week0);
                toReturn.WeekStartDateList.Add(week1);
                toReturn.WeekStartDateList.Add(week2);
                toReturn.WeekStartDateList.Add(week3);
            }
        }



        public static IEnumerable<StaticSearchResultRowJsonDto> RetrieveViewAllRecordResult(int viewID)
        {

            return AppStaticDataSetSearchBL.RetrieveViewAllRecordResult(viewID);



        }



        // if criteria contorl type == datetime, auto searialized to UTC
        // if criteria contorl type == date, need to convert to client time (trunkate time: 00:00:00)

        public static void ConvertSearchCriteriaDateFromUTCToClient(SearchDto searchDto)
        {
            foreach (var aCriteria in searchDto.Criterias)
            {
                if (aCriteria.Values != null && aCriteria.Values.Count > 0)
                {
                    if (aCriteria.CriteriaType == EmAppCriteriaType.Date)
                    {
                        if (aCriteria.ControlType.HasValue && aCriteria.ControlType.Value == (int)EmAppControlType.Date)
                        {
                            ConvertOneDateCriteriaFromUTCToClient(aCriteria);
                        }
                    }
                }
            }
        }

        public static void ConvertOneDateCriteriaFromUTCToClient(SearchCriteriaDto aCriteria)
        {
            for (int i = 0; i < aCriteria.Values.Count; i++)
            {
                DateTime? utcDatetime = ControlTypeValueConverter.ConvertValueToDate(aCriteria.Values[i]);
                if (utcDatetime.HasValue)
                {
                    var clientDateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(utcDatetime);
                    aCriteria.Values[i] = clientDateTime;
                }
            }
        }

        public static SearchDto RetrieveCurrentUserCalendarSearch(int? searchId)
        {
            SearchDto toReturnSearchDto = null;

            if (!searchId.HasValue)
            {
                int userType = AppSecurityUserBL.CurrentUserEntity.DomainId;

                if (userType == (int)EmAppUserType.SysAdmin || userType == (int)EmAppUserType.SaasCompanyAdmin)
                {
                    searchId = AppTenantSettingBL.GetIntValue(EmTenantSettings.AdminCalenarSearch);
                }
                else if (userType == (int)EmAppUserType.Employee)
                {
                    searchId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EmployeeCalendarSearch);
                }
                else if (userType == (int)EmAppUserType.Supplier)
                {
                    searchId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierCalendarSearch);
                }
                else if (userType == (int)EmAppUserType.Customer)
                {
                    searchId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerCalendarSearch);
                }
                else if (userType == (int)EmAppUserType.ClientAgent)
                {
                    searchId = AppTenantSettingBL.GetIntValue(EmTenantSettings.ClientAgentCalendarSearch);
                }
                else if (userType == (int)EmAppUserType.SupplierAgent)
                {
                    searchId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierAgentCalendarSearch);
                }
            }


            if (searchId.HasValue)
            {
                toReturnSearchDto = RetrieveOneSearchDto(searchId.Value, false, false);
            }


            if (toReturnSearchDto != null)
            {
                AppCascadingSearchBL.SetupIntialCscadingSearchCretiaDataSource(toReturnSearchDto, false);
            }


            return toReturnSearchDto;
        }

        public static SearchResultDto RetrieveUserCalendarSearchResult(SearchDto searchDto)
        {

            SearchResultDto toReturn = RetrieveSearchResult(searchDto);

            return toReturn;
        }

        //public static OperationCallResult<bool> ExecuteSearchViewLinkTargetBatchCommand(SearchDto searchDto)
        //{
        //    OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    if (searchDto.BatchExecuteLinkTargetId.HasValue)
        //    {

        //        AppFormLinkTargetDto linkTargetDto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(searchDto.BatchExecuteLinkTargetId.Value);

        //        if (linkTargetDto.ActionType == (int)EmAppLinkTargetActionType.ExecuteTransactionCommand &&
        //            linkTargetDto.OtherSettingsDto != null && linkTargetDto.OtherSettingsDto.IsStandAloneCommandAction
        //            && linkTargetDto.LinkTargetTransactionId.HasValue
        //            && linkTargetDto.OpennedFormAutoExecuteCommandId.HasValue
        //            && linkTargetDto.SourceViewColumnId1.HasValue)
        //        {

        //            SearchResultDto searchResult = AppSearchBL.RetrieveSearchResult(searchDto);

        //            if (searchResult != null && searchResult.SearchResultRowList != null)
        //            {
        //                foreach (StaticSearchResultRowJsonDto resultRowDto in searchResult.SearchResultRowList)
        //                {
        //                    resultRowDto.SelectedRowLinkTargetId = (int)linkTargetDto.Id;

        //                    if (linkTargetDto.SourceConditionViewColumnId.HasValue)
        //                    {
        //                        if (resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceConditionViewColumnId.Value] != null)
        //                        {
        //                            bool? conditionValue = ControlTypeValueConverter.ConvertValueToBoolean(resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceConditionViewColumnId.Value]);

        //                            if (conditionValue.HasValue && conditionValue.Value == false)
        //                            {
        //                                continue;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            continue;
        //                        }
        //                    }

        //                    string targetPkValue = ExecuteLinkTargetBatchCommand_GetOnResultRowTargetPkValue(linkTargetDto, resultRowDto);

        //                    if (!string.IsNullOrWhiteSpace(targetPkValue))
        //                    {
        //                        OperationCallResult<TransactionCommandActionResultDto> oneResult = AppTransactionCommandBL.ExecuteOneTransactionCommonadById(
        //                            linkTargetDto.OpennedFormAutoExecuteCommandId.Value,
        //                            linkTargetDto.LinkTargetTransactionId.Value,
        //                            targetPkValue, null, null, resultRowDto);

        //                        if (oneResult.ValidationResult.HasErrors)
        //                        {
        //                            string errorMsg = "API call failed on row key: " + targetPkValue + ".\n";
        //                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, errorMsg));
        //                            //aValidationResult.Merge(oneResult.ValidationResult);
        //                        }
        //                    }
        //                }

        //                if (!aValidationResult.HasErrors)
        //                {
        //                    aOperationCallResult.Object = true;
        //                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Success", ValidationItemType.Message, "Batch Command Execution Success."));
        //                }
        //                //aOperationCallResult.Object = true;
        //            }
        //            else
        //            {
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Invalid search result."));
        //            }



        //        }
        //        else
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Invalid LinkTarget."));
        //        }

        //    }
        //    else
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Invalid LinkTarget."));
        //    }

        //    return aOperationCallResult;
        //}

    

        private static void PrepareSearchDtoLinkToTransactionsAndCommands(SearchDto searchDto)
        {
            List<ReferenceViewDefinitionDto> searchViewList = AppSearchViewConfigBL.RetrieveUserViewsBySearchDefinition(new SearchDefinitionDto() { BlqueryId = searchDto.BlqueryId });

            List<AppFormLinkTargetDto> linkTargetList = new List<AppFormLinkTargetDto>();

            foreach (var viewDto in searchViewList)
            {
                linkTargetList.AddRange(LinkTragetBL.RetrieveOneSearchViewLinkTargetList(viewDto.Id, 1));
            }

            List<LookupItemDto> linkToTransactions = new List<LookupItemDto>();      

            var allTransactionDtoList = AppTransactionBL.RetrieveAllAppTransactionDto(false, 1, true);

            var transIdList = linkTargetList.Where(o => o.LinkTargetTransactionId.HasValue).Select(o => o.LinkTargetTransactionId.Value).Distinct().ToList();

            allTransactionDtoList.Where(o => transIdList.Contains((int)o.Id)).OrderBy(o => o.TransactionName)
                .ForAll(o => linkToTransactions.Add(new LookupItemDto() { Id = o.Id, Display = o.TransactionName }));
         
           
            List<LookupItemDto> linkToCommands = new List<LookupItemDto>();

            var commandIdList = linkTargetList.Where(o => o.LinkTargetTransactionId.HasValue && o.OpennedFormAutoExecuteCommandId.HasValue)
             .Select(o => o.OpennedFormAutoExecuteCommandId.Value).Distinct().ToList();

            AppTransactionCommandBL.RetrieveCommandListByIds(commandIdList).Where(o=>o.CommandTransactionId.HasValue).ForAll(o => linkToCommands.Add(
                new LookupItemDto() { Id = o.Id, Display = o.Name, UserDefinedValue1 = o.CommandTransactionId.Value.ToString() }));

            searchDto.LinkToTransactions = linkToTransactions;
            searchDto.LinkToCommands = linkToCommands;
        }
    }
}