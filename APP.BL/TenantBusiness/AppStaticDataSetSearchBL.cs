//using APP.Persistence.Common;
using APP.LBL.EntityClasses;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using DatabaseSchemaMrg.DataSchema;
using System.Text.RegularExpressions;
using DatabaseSchemaMrg;
using System.Data.Common;
using System;
using System.Text;
using APP.LBL.DatabaseSpecific;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

   
using APP.Components.EntityConverter;

using APP.Framework;
namespace App.BL
{
    public static class AppStaticDataSetSearchBL
    {
        public static readonly string EventNamePropertyName = "EventName";
        public static readonly string EventBodyPropertyName = "EventBody";
        public static readonly string EventStartDatePropertyName = "EventStartDate";
        public static readonly string EventEndDatePropertyName = "EventEndDate";

        public static readonly string EventActualStartDatePropertyName = "EventActualStartDate";
        public static readonly string EventActualEndDatePropertyName = "EventActualEndDate";

        public static readonly string EventCompletePercentagePropertyName = "EventCompletePercentage";


        public static readonly string EventTypePropertyName = "EventType";
        public static readonly string EventCompletStagePropertyName = "EventCompletStage";
        public static readonly string EventStatusPropertyName = "EventStatus";

        public static readonly string EventUserIdPropertyName = "EventUserId";
        public static readonly string EventDescription1PropertyName = "EventDescription1";
        public static readonly string EventDescription2PropertyName = "EventDescription2";
        public static readonly string EventGroupByIdPropertyName = "EventGroupById";

        public static readonly string EventDateIdPropertyName = "EventDateId";
        public static readonly string EventTransactionIdPropertyName = "EventTransactionId";
        public static readonly string EventTransactionRIdPropertyName = "EventTransactionRId";

        public static readonly string EventColorIdPropertyName = "EventColorId";

        public static readonly string AppModifiedByIDColumnName = "AppModifiedByID";
        public static readonly string AppModifiedDateColumnName = "AppModifiedDate";


        public static readonly List<string> CalendarViewRequiredFieldList = new List<string>()
        {
            EventNamePropertyName,
            EventBodyPropertyName,
            EventStartDatePropertyName,
            EventEndDatePropertyName,
        };

        public static readonly List<string> GanttViewRequiredFieldList = new List<string>()
        {
            EventNamePropertyName,
            EventBodyPropertyName,
            EventStartDatePropertyName,
            EventEndDatePropertyName,
            EventActualStartDatePropertyName,
            EventActualEndDatePropertyName,
            EventCompletePercentagePropertyName,
        };

        public static readonly List<string> SchedulerViewRequiredFieldList = new List<string>()
        {
            EventNamePropertyName,
            EventBodyPropertyName,
            EventStartDatePropertyName,
            EventEndDatePropertyName,
        };


        public static IEnumerable<StaticSearchResultRowJsonDto> GetTransctionFolderViewList(int? folderId, int? searchViewId, int? transactionId)
        {
            var toReturn = new List<StaticSearchResultRowJsonDto>();

            AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(searchViewId);
            List<AppSearchViewFieldEntity> selectViewColumnEntitys = viewEntity.AppSearchViewField.Where(o => o.OrderByLevel.HasValue).ToList();

            if (viewEntity.AppDataSet == null || (string.IsNullOrEmpty(viewEntity.AppDataSet.QueryText)))
            {
                return toReturn;
            }



            //
            var dataTableResult = RetriveFolderSearchViewDataTable(folderId, viewEntity, transactionId);

            dataTableResult = SortSelectViewColumnDataTable(selectViewColumnEntitys, dataTableResult);

            List<StaticSearchResultRowJsonDto> rows = ConvertSearchResultToJsonRow(viewEntity, dataTableResult, null);

            return rows;


        }



        public static IEnumerable<StaticSearchResultRowJsonDto> GetFileFolderCategoryFileViewList(int? searchViewId, EmAppFileFolderCategory emAppFileFolderCategory, int? transactionId, string fullTextSearch = "")
        {
            var toReturn = new List<StaticSearchResultRowJsonDto>();

            AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(searchViewId);
            List<AppSearchViewFieldEntity> selectViewColumnEntitys = viewEntity.AppSearchViewField.Where(o => o.OrderByLevel.HasValue).ToList();

            if (viewEntity.AppDataSet == null || (string.IsNullOrEmpty(viewEntity.AppDataSet.QueryText)))
            {
                return toReturn;
            }




            //
            var dataTableResult = RetriveFileFolderCategorySearchViewDataTable(viewEntity, emAppFileFolderCategory, transactionId, fullTextSearch);

            if (emAppFileFolderCategory == EmAppFileFolderCategory.MyRecentlyFiles)
            {
                if (dataTableResult.Rows.Count > 0)
                {
                    DataView dv = dataTableResult.DefaultView;
                    var col_Sort = viewEntity.AppSearchViewField.FirstOrDefault(o => o.SysTableFiledPath.ToLower() == "AppModifiedDate".ToLower());

                    if (col_Sort == null)
                    {
                        col_Sort = viewEntity.AppSearchViewField.FirstOrDefault(o => o.SysTableFiledPath.ToLower() == "FileID".ToLower());
                    }

                    if (col_Sort != null)
                    {
                        dv.Sort = string.Format("{0} {1}", col_Sort.SysTableFiledPath, "desc");
                        DataTable sortedDT = dv.ToTable();

                        dataTableResult = dv.ToTable();
                    }
                }
            }
            else
            {
                dataTableResult = SortSelectViewColumnDataTable(selectViewColumnEntitys, dataTableResult);
            }

            List<StaticSearchResultRowJsonDto> rows = ConvertSearchResultToJsonRow(viewEntity, dataTableResult, null);

            return rows;


        }

        public static string ParseSelectFiledWithStarQuery(string orgQuery, List<string> includingSelectFields, List<string> whereClauseFields, EmSqlType sqlType, bool isUseOrClause = false, int? topNbValues = null)
        {
            string includingField = string.Empty;
            if (includingSelectFields == null || includingSelectFields.Count == 0)
            {
                includingField = " * ";
            }

            foreach (string orgselectFields in includingSelectFields)
            {
                string dbFields = AppMetaDataBL.GetQulifiedTableFiledName(orgselectFields, sqlType);
                includingField = includingField + dbFields + ",";
            }

            //  remove  last ,
            includingField = includingField.Substring(0, includingField.Length - 1);

            string whereClasue = string.Empty;

            if (whereClauseFields.Count > 0)
            {
                whereClasue = SqlQuery.WHERE;

                foreach (string whereField in whereClauseFields)
                {
                    if (isUseOrClause)
                    {
                        whereClasue = whereClasue + whereField + SqlQuery.OR;
                    }
                    else
                    {
                        whereClasue = whereClasue + whereField + SqlQuery.AND;
                    }
                }

                int lastIndexofAnd = whereClasue.LastIndexOf(SqlQuery.AND);

                if (isUseOrClause)
                {
                    lastIndexofAnd = whereClasue.LastIndexOf(SqlQuery.OR);
                }

                whereClasue = whereClasue.Substring(0, lastIndexofAnd);
            }

            //TraceLevel: Enum: off-0 Error-1 Warning-2  Info-3  Verbose-4   : Output all debugging and tracing messages.
            string aQuery = SqlQuery.SELECT + includingField + SqlQuery.FROM + " ( " + orgQuery + " )  as  DynTable " + whereClasue;

            if (topNbValues.HasValue && topNbValues.Value > 0)
            {
                aQuery = DatabaseFixture.SetupTopQuery(sqlType, topNbValues, aQuery);

            }

            return aQuery;
        }

        internal static IEnumerable<StaticSearchResultRowJsonDto> GetTriggerExecutionStaticSearchResult(SearchDto searchDto, ReferenceViewDefinitionDto referenceViewDefinitionDto)
        {
            var toReturn = new List<StaticSearchResultRowJsonDto>();


            //SearchCriteriaDto triggerExecutionCriteriaDto = searchDto.CurrentTriggerExecutionCriteria;


            //AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(referenceViewDefinitionDto.Id.ToString()));


            //List<AppSearchViewFieldEntity> selectViewColumnEntitys = viewEntity.AppSearchViewField.Where(o => o.OrderByLevel.HasValue).ToList();


            //if (viewEntity.AppDataSet == null || (!viewEntity.AppDataSet.BaseDataSetId.HasValue && string.IsNullOrEmpty(viewEntity.AppDataSet.QueryText)))
            //{
            //	return toReturn;
            //}

            ////



            //var dataTableResult = RetriveMasterDataSetDataTable(searchDto, viewEntity);


            //dataTableResult = SortSeleteDataTable(selectViewColumnEntitys, dataTableResult);

            //if (viewEntity.AppDataSet.BaseDataSetId.HasValue)
            //{
            //	dataTableResult = ExtractChildDataSet(viewEntity, dataTableResult);
            //}


            //List<StaticSearchResultRowJsonDto> searchResultRows = ConvertSearchResultToJsonRow(viewEntity, dataTableResult);


            //if (viewEntity.UpdateTransctionId.HasValue)
            //{
            //	AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(viewEntity.UpdateTransctionId);

            //	if (aAppTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List) // it is list eidt ( two level update)
            //	{

            //		StaticSearchResultRowJsonDto aMassUpdateStaticSearchResultRowJsonDto = new APP.Components.EntityDto.StaticSearchResultRowJsonDto();

            //		List<object> rootIdList = searchResultRows.Select(o => o.DictViewColumnIDKeyValue.First().Value).ToList();

            //		//searchResultRows[]

            //		AppListDataDto appListDataDto = AppListEditFormDataLoadBL.GetListEditFormData(viewEntity.UpdateTransctionId.Value, rootIdList);
            //		appListDataDto.MassUpdateRootIdList = rootIdList;

            //		aMassUpdateStaticSearchResultRowJsonDto.MassUpdateAppListDataDto = appListDataDto;

            //		searchResultRows = new List<StaticSearchResultRowJsonDto>();

            //		searchResultRows.Add(aMassUpdateStaticSearchResultRowJsonDto);



            //	}


            //}

            //return searchResultRows;

            return toReturn;
        }

        internal static SearchResultDto GetStaticSearchResult(SearchDto searchDto, ReferenceViewDefinitionDto referenceViewDefinitionDto)
        {





            SearchResultDto toReturn = new SearchResultDto();
            if (searchDto != null && referenceViewDefinitionDto != null)
            {
                AppSearchViewEntity rootViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(referenceViewDefinitionDto.Id.ToString()));
                toReturn.SearchResultRowList = new List<StaticSearchResultRowJsonDto>();



                if (rootViewEntity.AppDataSet == null || (!rootViewEntity.AppDataSet.BaseDataSetId.HasValue && string.IsNullOrEmpty(rootViewEntity.AppDataSet.QueryText)))
                {
                    return toReturn;
                }

                //
                List<StaticSearchResultRowJsonDto> rootViewSearchResultRows = ExcuteSearchDtoWithViewEntity(searchDto, rootViewEntity);

                toReturn.SearchResultRowList = rootViewSearchResultRows;

                BuildSearchViewByType(searchDto, referenceViewDefinitionDto, rootViewEntity, toReturn);

                if (searchDto.WhereUsedSearchId.HasValue)
                {
                    SearchDto embeddedChildSearchDto = AppSearchBL.RetrieveOneSearchDto(searchDto.WhereUsedSearchId.Value, false, false);
                    ReferenceViewDto referenceViewDto = embeddedChildSearchDto.DefaultView;

                    AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(embeddedChildSearchDto);
                    embeddedChildSearchDto.ReferenceViewDefinitionDto = embeddedChildSearchDto.DefaultView;
                    toReturn.ChildSearchResultDto = AppSearchBL.RetrieveSearchResult(embeddedChildSearchDto);
                }


                // Only apply for list edit massupdate . for massupdate --mastedetail, no need t
                if (rootViewEntity.UpdateTransctionId.HasValue)
                {
                    ProcessViewMassUpdateTrascation(searchDto, rootViewEntity, toReturn);

                }
            }
            return toReturn;
        }


        internal static SearchResultDto GetIntegrationWebApiSearchResult(SearchDto searchDto, ReferenceViewDefinitionDto referenceViewDefinitionDto, AppDataSetEntity dataSetEntity)
        {
            AppSearchViewEntity rootViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(int.Parse(referenceViewDefinitionDto.Id.ToString()));

            //SearchResultDto toReturn = new SearchResultDto();
            //toReturn.SearchResultRowList = new List<StaticSearchResultRowJsonDto>();

            int? apiOperationId = ControlTypeValueConverter.ConvertValueToInt(dataSetEntity.QueryText);
            string rootArrayJsonNodePath = dataSetEntity.BaseTableName;

            SearchResultDto toReturn = ExcuteIntegrationApiSearch(searchDto, rootViewEntity, apiOperationId, rootArrayJsonNodePath, dataSetEntity);

            //toReturn.SearchResultRowList = searchResultRows;

            BuildSearchViewByType(searchDto, referenceViewDefinitionDto, rootViewEntity, toReturn);

            // Only apply for list edit massupdate . for massupdate --mastedetail, no need t
            if (rootViewEntity.UpdateTransctionId.HasValue)
            {
                ProcessViewMassUpdateTrascation(searchDto, rootViewEntity, toReturn);

            }

            return toReturn;

        }


        internal static void BulidDateAndTimeCalendarSelectorViewResult(ReferenceViewDefinitionDto referenceViewDefinitionDto, SearchResultDto toReturn)
        {
            var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(referenceViewDefinitionDto.Id);
            var groupByDateField = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value);

            if (groupByDateField != null)
            {
                Dictionary<int, List<StaticSearchResultRowJsonDto>> dictDateIdAndTimeResultList = new Dictionary<int, List<StaticSearchResultRowJsonDto>>();

                foreach (var aResultRow in toReturn.SearchResultRowList)
                {
                    DateTime? groupByDate = ControlTypeValueConverter.ConvertValueToDate(aResultRow.DictViewColumnIDKeyValue[(int)groupByDateField.Id]);

                    if (groupByDate.HasValue)
                    {
                        int? dateId = ControlTypeValueConverter.ConvertValueToInt(groupByDate.Value.ToString("yyyyMMdd"));
                        if (dateId.HasValue)
                        {
                            if (!dictDateIdAndTimeResultList.ContainsKey(dateId.Value))
                            {
                                dictDateIdAndTimeResultList.Add(dateId.Value, new List<StaticSearchResultRowJsonDto>());
                            }

                            dictDateIdAndTimeResultList[dateId.Value].Add(aResultRow);
                        }
                    }
                }

                toReturn.DictDateIdAndResultRowList = dictDateIdAndTimeResultList;

                //var newResultRowList = new List<StaticSearchResultRowJsonDto>();                

                //foreach (int dateId in dictDateIdAndTimeResultList.Keys)
                //{
                //    StaticSearchResultRowJsonDto keyDateRow = new StaticSearchResultRowJsonDto();
                //    keyDateRow.EventDateId = dateId;
                //    keyDateRow.Children = dictDateIdAndTimeResultList[dateId];
                //    keyDateRow.EventStartDate = ControlTypeValueConverter.ConvertValueToDate(keyDateRow.Children.First().DictViewColumnIDKeyValue[(int)groupByDateField.Id]);
                //    keyDateRow.DictViewColumnIDKeyValue = new Dictionary<int, object>();
                //    keyDateRow.DictViewColumnIDKeyValue[(int)groupByDateField.Id] = keyDateRow.Children.First().DictViewColumnIDKeyValue[(int)groupByDateField.Id];

                //    newResultRowList.Add(keyDateRow);
                //}

                toReturn.SearchResultRowList = new List<StaticSearchResultRowJsonDto>();
            }
        }

        internal static void PrepareClusterChildViewTransformdResult(ReferenceViewDefinitionDto referenceViewDefinitionDto, SearchResultDto toReturn)
        {
            toReturn.DictTransformedResultSet = new Dictionary<string, List<StaticSearchResultRowJsonDto>>();

            var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(referenceViewDefinitionDto.Id);

            if (searchViewDto.OtherSettingsDto != null && searchViewDto.OtherSettingsDto.ClusterChildViewItemList != null)
            {
                foreach (var childViewItem in searchViewDto.OtherSettingsDto.ClusterChildViewItemList)
                {
                    if (childViewItem.ViewType.HasValue)
                    {
                        if (childViewItem.ViewType.Value == (int)EmAppViewType.ChartView)
                        {
                            if (childViewItem.ChartType.HasValue && childViewItem.ChartType.Value == (int)EmAppChartViewType.TreeMap)
                            {
                                PrepareClusterChildViewTransformdResult_TreeMapChartView(toReturn, childViewItem);

                            }
                        }
                    }
                }

            }


        }

        private static void PrepareClusterChildViewTransformdResult_TreeMapChartView(SearchResultDto toReturn, AppDesktopItemExDto childViewItem)
        {
            List<StaticSearchResultRowJsonDto> transformedItems = new List<StaticSearchResultRowJsonDto>();

            toReturn.DictTransformedResultSet.Add(childViewItem.UiId, transformedItems);

            var treeNodeColumn = childViewItem.AppSearchViewFieldList.FirstOrDefault(o => o.IsUserDefined1.HasValue && o.IsUserDefined1.Value);
            var parentNodeColumn = childViewItem.AppSearchViewFieldList.FirstOrDefault(o => o.IsUserDefined2.HasValue && o.IsUserDefined2.Value);

            if (treeNodeColumn != null && parentNodeColumn != null)
            {
                int? treeNodeColumnId = ControlTypeValueConverter.ConvertValueToInt(treeNodeColumn.Id);
                int? parentNodeColumnId = ControlTypeValueConverter.ConvertValueToInt(parentNodeColumn.Id);

                if (treeNodeColumnId.HasValue && parentNodeColumnId.HasValue)

                    toReturn.SearchResultRowList.Where(f => f.DictViewColumnIDKeyValue[parentNodeColumnId.Value] == null).ForAll(o =>
                    {
                        transformedItems.Add(o);
                    });

                foreach (var rootItem in transformedItems)
                {
                    ProcessChilds(toReturn.SearchResultRowList, rootItem, treeNodeColumnId.Value, parentNodeColumnId.Value);
                }
            }


        }

        private static void ProcessViewMassUpdateTrascation(SearchDto searchDto, AppSearchViewEntity rootViewEntity, SearchResultDto toReturn)
        {
            List<StaticSearchResultRowJsonDto> rootViewSearchResultRows = toReturn.SearchResultRowList.ToList();

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootViewEntity.UpdateTransctionId);

            if (aAppTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List) // it is list eidt ( two level update)
            {

                //viewEntity.AppSearchViewField.Where(o => o.OrderByLevel.HasValue).ToList();


                var dictmassUpdateViewPullOneToOneFields = new Dictionary<string, object>();
                foreach (var viewFiedcolmEntity in rootViewEntity.AppSearchViewField)
                {
                    if (viewFiedcolmEntity.PullCriteriaAsDefaultValueSearchFieldId.HasValue)
                    {
                        int searchFiedId = viewFiedcolmEntity.PullCriteriaAsDefaultValueSearchFieldId.Value;
                        if (viewFiedcolmEntity.MassUpdateTransactionFieldId.HasValue)
                        {
                            AppTransactionFieldExDto transactionFieldExDto = aAppTransactionExDto.DictAllTransactionField[viewFiedcolmEntity.MassUpdateTransactionFieldId.Value];

                            var cretiaDto = searchDto.Criterias.Where(o => o.SearcDCUID == searchFiedId).FirstOrDefault();

                            dictmassUpdateViewPullOneToOneFields[transactionFieldExDto.DataBaseFieldName] = cretiaDto.Value;
                        }

                    }
                }

                string transactionJsonData = null;

                if (toReturn.ApiOperationId.HasValue)
                {
                    AppTransactionExDto listEditTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootViewEntity.UpdateTransctionId.Value);

                    if (listEditTransactionExDto != null && listEditTransactionExDto.OtherOptions != null
                        && listEditTransactionExDto.OtherOptions.IsApiIntegrationTransaction
                        && listEditTransactionExDto.BaseApiConfigDto != null)
                    {
                        int transactionApiOperationId = (int)listEditTransactionExDto.BaseApiConfigDto.Id;

                        if (transactionApiOperationId == toReturn.ApiOperationId.Value)
                        {
                            transactionJsonData = toReturn.ApiResponseJsonData;
                        }
                    }
                }

                AppListDataDto appListDataDto = new AppListDataDto();

                if (transactionJsonData != null)
                {
                    appListDataDto = AppListEditFormDataLoadBL.GetListEditFormData(rootViewEntity.UpdateTransctionId.Value, null, transactionJsonData);
                }
                else
                {
                    List<object> rootIdList = rootViewSearchResultRows.Select(o => o.DictViewColumnIDKeyValue.First().Value).ToList();

                    //searchResultRows[]

                    if (!rootIdList.IsEmpty())
                    {
                        appListDataDto = AppListEditFormDataLoadBL.GetListEditFormData(rootViewEntity.UpdateTransctionId.Value, rootIdList, null);
                    }
                    else
                    {
                        appListDataDto.EditCloneAppChildDataDto = AppListEditFormDataLoadBL.GetListCloneEditRow(aAppTransactionExDto);
                        appListDataDto.ListData = new List<AppChildDataDto>();
                        appListDataDto.TransactionId = rootViewEntity.UpdateTransctionId.Value;
                    }

                    appListDataDto.MassUpdateRootIdList = rootIdList;
                }



                appListDataDto.IsMassUpdate = true;
                appListDataDto.MassUpdateViewId = rootViewEntity.SearchViewId;

                var editCloneAppChildDataDtoOneToOne = appListDataDto.EditCloneAppChildDataDto.DictOneToOneFields;

                foreach (var dbField in dictmassUpdateViewPullOneToOneFields.Keys)
                {
                    if (editCloneAppChildDataDtoOneToOne.ContainsKey(dbField))
                    {
                        editCloneAppChildDataDtoOneToOne[dbField] = dictmassUpdateViewPullOneToOneFields[dbField];

                    }
                }

                // For list eidt , the first Row is used 
                toReturn.MassUpdateAppListDataDto = appListDataDto;

            }
        }

        private static void PorcessHierarchyMasterDetailView(AppSearchViewEntity viewEntity, List<StaticSearchResultRowJsonDto> rootViewSearchResultRows)
        {
            // get child view 

            var childViewEntityList = AppSearchViewConfigBL.RetrieveHierarchyMasterDetailViewChildViewEntityList(viewEntity.SearchViewId);

            Dictionary<int, Dictionary<int, AppSearchViewFieldEntity>> dictChildViewIdDictChildViewFiedJoinToFieldEntity = new Dictionary<int, Dictionary<int, AppSearchViewFieldEntity>>();

            Dictionary<int, List<int>> dictChildViewIdFiedJoinToParentFiedId = new Dictionary<int, List<int>>();

            foreach (var childViewEntity in childViewEntityList)
            {
                var dictChildViewFiedIdJoinToFieldEntity = childViewEntity.AppSearchViewField.Where(o => o.JoinToParentViewFieldId.HasValue)
                     .ToDictionary(o => o.SearchViewFieldId, o => o);

                dictChildViewIdDictChildViewFiedJoinToFieldEntity[childViewEntity.SearchViewId] = dictChildViewFiedIdJoinToFieldEntity;
            }

            Dictionary<int, Dictionary<string, List<StaticSearchResultRowJsonDto>>> dictChildViewDictJoinKeySearchResult = new Dictionary<int, Dictionary<string, List<StaticSearchResultRowJsonDto>>>();
            // filter each child dataset by parentJoinFiled Id
            foreach (var childViewEntity in childViewEntityList)
            {
                if (childViewEntity.FilterSearchId.HasValue)
                {
                    int searchFilterId = childViewEntity.FilterSearchId.Value;

                    var appSearchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(searchFilterId);
                    SearchDto childSearchDto = AppSearchConfigBL.GetSearchDtoSetupDefaultSearchCretia(appSearchEntity);

                    var dictChildViewFiedJoinToFieldEntity = dictChildViewIdDictChildViewFiedJoinToFieldEntity[childViewEntity.SearchViewId];

                    var dictTableFiedPathSearchViewField = dictChildViewFiedJoinToFieldEntity.Values.ToDictionary(o => o.SysTableFiledPath, o => o.SearchViewFieldId);


                    List<int> joinToParentViewFieldIdList = dictChildViewFiedJoinToFieldEntity.Values.Select(o => o.JoinToParentViewFieldId.Value).ToList();

                    dictChildViewIdFiedJoinToParentFiedId[childViewEntity.SearchViewId] = joinToParentViewFieldIdList;

                    foreach (var criteriaDto in childSearchDto.Criterias)
                    {
                        string sysTableFiledPath = criteriaDto.SysTableFiledPath;
                        if (dictTableFiedPathSearchViewField.ContainsKey(sysTableFiledPath))
                        {
                            int searchViewFieldId = dictTableFiedPathSearchViewField[sysTableFiledPath];
                            int JoinToParentFiedId = dictChildViewFiedJoinToFieldEntity[searchViewFieldId].JoinToParentViewFieldId.Value;

                            var listRooeListValeId = rootViewSearchResultRows.Select(o => o.DictViewColumnIDKeyValue[JoinToParentFiedId]).Distinct();



                            //criteriaDto.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.In;

                            criteriaDto.CriteriaOperator = CriteriaOperators.In;

                            criteriaDto.Values = new System.Collections.ObjectModel.ObservableCollection<object>(listRooeListValeId);

                        }

                    }


                    List<StaticSearchResultRowJsonDto> childViewSearchResult = ExcuteSearchDtoWithViewEntity(childSearchDto, childViewEntity);

                    //  Dictionary<int, int> dictChildViewFiedJoinToId = dictChildViewFiedJoinToFieldEntity.ToDictionary(o => o.Key, o => o.Value.JoinToParentViewFieldId.Value);

                    Dictionary<string, List<StaticSearchResultRowJsonDto>> dictchildViewSearchResult = childViewSearchResult.GroupBy(r => new NTuple<object>(from columnId in dictChildViewFiedJoinToFieldEntity.Keys select r[columnId]))
                      .ToDictionary(
                       g => g.Key.Values.
                       Select(o => o.ToString()).Aggregate((i, v) => i + "_" + v),
                       g => g.ToList()

                      );



                    dictChildViewDictJoinKeySearchResult[childViewEntity.SearchViewId] = dictchildViewSearchResult;
                }

            }





            foreach (var rootTesultRowDto in rootViewSearchResultRows)
            {
                rootTesultRowDto.DictViewIdAndChildRowList = new Dictionary<int, List<StaticSearchResultRowJsonDto>>();

                foreach (int childViewId in dictChildViewDictJoinKeySearchResult.Keys)
                {
                    if (dictChildViewIdDictChildViewFiedJoinToFieldEntity.ContainsKey(childViewId))
                    {

                        Dictionary<string, List<StaticSearchResultRowJsonDto>> dictchildViewSearchResult = dictChildViewDictJoinKeySearchResult[childViewId];
                        List<int> joinToParentFiedIds = dictChildViewIdFiedJoinToParentFiedId[childViewId];

                        List<string> jointValueList = new List<string>();
                        foreach (int parentId in joinToParentFiedIds)
                        {
                            jointValueList.Add(rootTesultRowDto[parentId].ToString());

                        }
                        string combineKey = jointValueList.Aggregate((i, j) => i + "_" + j);
                        if (dictchildViewSearchResult.ContainsKey(combineKey))
                        {
                            rootTesultRowDto.DictViewIdAndChildRowList[childViewId] = dictchildViewSearchResult[combineKey];
                        }

                    }

                }


                //var childRow1 = new StaticSearchResultRowJsonDto() { DictViewColumnIDKeyValue = new Dictionary<int, object>() };
                //var childRow2 = new StaticSearchResultRowJsonDto() { DictViewColumnIDKeyValue = new Dictionary<int, object>() };
                //childRow1.DictViewColumnIDKeyValue.Add(14885, "AAA");
                //childRow2.DictViewColumnIDKeyValue.Add(14885, "BBB");

                //childViewRows.Add(childRow1);
                //childViewRows.Add(childRow2);

                //int childViewId = 7377;
                //rootTesultRowDto.DictViewIdAndChildRowList.Add(childViewId, childViewRows);
            }
        }

        private static List<StaticSearchResultRowJsonDto> ExcuteSearchDtoWithViewEntity(SearchDto searchDto, AppSearchViewEntity viewEntity)
        {
            List<AppSearchViewFieldEntity> orderbyViewColumnEntityList = viewEntity.AppSearchViewField.Where(o => o.OrderByLevel.HasValue).ToList();

            var dataTableResult = RetriveMasterDataSetDataTable(searchDto, viewEntity);

            dataTableResult = SortSelectViewColumnDataTable(orderbyViewColumnEntityList, dataTableResult);

            if (viewEntity.AppDataSet.BaseDataSetId.HasValue)
            {
                dataTableResult = ExtractChildDataSet(viewEntity, dataTableResult);
            }


            // for regular search vuew, massupdate view -- MasterDetail ,  massupdate view -- List eidt , need to get all search resutl first !!
            List<StaticSearchResultRowJsonDto> rootViewSearchResultRows = ConvertSearchResultToJsonRow(viewEntity, dataTableResult, searchDto);
            return rootViewSearchResultRows;
        }

        private static DataTable SortSelectViewColumnDataTable(List<AppSearchViewFieldEntity> selectViewColumnEntitys, DataTable toRetrun)
        {
            if (toRetrun == null || toRetrun.Rows.Count == 0)
                return new DataTable();

            DataView dv = toRetrun.DefaultView;
            var orderColumn = selectViewColumnEntitys.Where(o => o.OrderByLevel.HasValue).OrderBy(o => o.OrderByLevel.Value).ToList();
            string sortBy = "";
            foreach (var colun in orderColumn)
            {
                string desc = "";
                if (colun.IsDescOrder.HasValue && colun.IsDescOrder.Value)
                {
                    desc = "desc";
                }
                sortBy = sortBy + string.Format("{0} {1},", colun.SysTableFiledPath, desc); ;

            }

            if (sortBy != "")
            {
                sortBy = sortBy.Substring(0, sortBy.Length - 1);

                dv.Sort = sortBy;
                DataTable sortedDT = dv.ToTable();
                return sortedDT;
            }
            else
            {
                return toRetrun;
            }

        }
        private static DataTable ExtractChildDataSet(AppSearchViewEntity viewEntity, DataTable dataTableResult)
        {
            DataTable extractResult = new DataTable();

            // need to process Extract View 
            // it is extract view
            //https://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
            if (viewEntity.AppDataSet.BaseDataSetId.HasValue)
            {
                var childDataSetEntity = AppDataSetExtractViewConfigBL.RetrieveOneAppDataSetEntity(viewEntity.DataSetId);



                List<string> extractByColumn = childDataSetEntity


                    .AppDateSetDataExtractView.Select(o => o.DbfiledName).ToList();

                //dt2.Columns.a
                foreach (string exColum in extractByColumn)
                {

                }


                List<string> groupByColumn = childDataSetEntity.AppDateSetDataExtractView.Where(o => o.IsGroup.HasValue && o.IsGroup.Value).Select(o => o.DbfiledName).ToList();

                List<AppDateSetDataExtractViewEntity> aggByColumn = childDataSetEntity.AppDateSetDataExtractView.Where(o => o.AggFunction.HasValue).ToList();

                var groupedDataRow = dataTableResult.AsEnumerable().GroupBy(dataRow =>
                {
                    string sb = "";

                    foreach (String column in groupByColumn)
                    {
                        sb = sb + string.Format("{0}_", dataRow[column]);

                    }
                    return sb;
                    //return "";

                });

                extractResult = dataTableResult.Clone();

                foreach (var groupRow in groupedDataRow)
                {
                    DataRow row = extractResult.NewRow();
                    extractResult.Rows.Add(row);
                    DataRow firstRow = groupRow.First();

                    foreach (string gcolumn in groupByColumn)
                    {
                        row[gcolumn] = firstRow[gcolumn];

                    }

                    foreach (var aggColumnEntity in aggByColumn)
                    {
                        string dbFieldname = aggColumnEntity.DbfiledName;
                        int aggType = aggColumnEntity.AggFunction.Value;

                        if (aggType == (int)EmAppAggregationFunctionType.AVG)
                        {
                            row[dbFieldname] = groupRow.Average(o => ControlTypeValueConverter.ConvertValueToDecimalWithDefautZero(o[dbFieldname]));

                        }
                        else if (aggType == (int)EmAppAggregationFunctionType.SUM)
                        {
                            row[dbFieldname] = groupRow.Sum(o => ControlTypeValueConverter.ConvertValueToDecimalWithDefautZero(o[dbFieldname]));

                        }

                        else if (aggType == (int)EmAppAggregationFunctionType.Max)
                        {
                            row[dbFieldname] = groupRow.Max(o => ControlTypeValueConverter.ConvertValueToDecimalWithDefautZero(o[dbFieldname]));

                        }
                        else if (aggType == (int)EmAppAggregationFunctionType.Min)
                        {
                            row[dbFieldname] = groupRow.Min(o => ControlTypeValueConverter.ConvertValueToDecimalWithDefautZero(o[dbFieldname]));

                        }

                        else if (aggType == (int)EmAppAggregationFunctionType.ConcatenateString)
                        {
                            row[dbFieldname] = groupRow.Select(o => o[dbFieldname] as string).Aggregate((i, j) => i + "," + j);

                        }

                        else if (aggType == (int)EmAppAggregationFunctionType.RowCount)
                        {
                            row[dbFieldname] = groupRow.Count();

                        }

                        else if (aggType == (int)EmAppAggregationFunctionType.BooleanSum)
                        {
                            row[dbFieldname] = "true";//groupRow.Count();

                        }

                    }

                }

            }

            return extractResult;
        }




        private static DataTable RetriveFileFolderCategorySearchViewDataTable(AppSearchViewEntity viewEntity, EmAppFileFolderCategory emAppFileFolderCategory, int? transactionId, string fullTextSearch)
        {

            DataTable toReturn = new DataTable();

            var fielIdFied = viewEntity.AppSearchViewField.Where(o => o.IsTransRootId.HasValue && o.IsTransRootId.Value).FirstOrDefault();

            if (fielIdFied != null)
            {




                //paramterList.Add(parameter);
                int[] fieldIds = null;

                int[] recycleBinfieldIds = AppTransactionRecycleBL.GetAllTransactionRecycleBinRootValueIds(transactionId);

                if (emAppFileFolderCategory == EmAppFileFolderCategory.MyRecentlyFiles)
                {

                    fieldIds = AppFileCollaborationBL.GetMyRecentlyFilesFileIds();

                    if (!recycleBinfieldIds.IsEmpty())
                    {
                        fieldIds = fieldIds.Except(recycleBinfieldIds).ToArray();

                    }
                }
                else if (emAppFileFolderCategory == EmAppFileFolderCategory.Favorites)
                {

                    fieldIds = AppFileCollaborationBL.GetMyFavouriteFileIds();

                    if (!recycleBinfieldIds.IsEmpty())
                    {
                        fieldIds = fieldIds.Except(recycleBinfieldIds).ToArray();

                    }
                }

                else if (emAppFileFolderCategory == EmAppFileFolderCategory.SharedToMe)
                {

                    fieldIds = AppFileCollaborationBL.GetSharedToMeFileIds();

                    if (!recycleBinfieldIds.IsEmpty())
                    {
                        fieldIds = fieldIds.Except(recycleBinfieldIds).ToArray();

                    }
                }
                else if (emAppFileFolderCategory == EmAppFileFolderCategory.ShareToOthers)
                {


                    fieldIds = AppFileCollaborationBL.GetShareToOthersFileIds();

                    if (!recycleBinfieldIds.IsEmpty())
                    {
                        fieldIds = fieldIds.Except(recycleBinfieldIds).ToArray();

                    }
                }

                else if (emAppFileFolderCategory == EmAppFileFolderCategory.MyRecycleBin)
                {

                    fieldIds = AppTransactionRecycleBL.GetCurrentUserRecycleBinRootValueIds(transactionId);
                }

                if (!fieldIds.IsEmpty())
                {

                    if (!string.IsNullOrWhiteSpace(fullTextSearch))
                    {
                        var fullTextFiledIdList = AppSearchBL.FullTextLatestVersionFileSearch(fullTextSearch);

                        fieldIds = fieldIds.Intersect(fullTextFiledIdList).ToArray();

                    }


                    if (!fieldIds.IsEmpty())
                    {


                        return RetrieveFileViewWithFileIds(viewEntity, fieldIds);
                    }



                }



            }




            return new DataTable();






        }
        // it is root fileIds
        internal static DataTable RetrieveFileViewWithFileIds(AppSearchViewEntity viewEntity, int[] fileIds)
        {
            var fielIdFied = viewEntity.AppSearchViewField.Where(o => o.IsTransRootId.HasValue && o.IsTransRootId.Value).FirstOrDefault();

            if (fielIdFied == null)
            {
                return new DataTable();

            }

            int dataSetId = viewEntity.DataSetId.Value;

            var aAppDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(dataSetId);
            var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

            if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType == (int)EmAppDataServiceType.QueryText)
            {
                string query = viewEntity.AppDataSet.QueryText;
                string qulifiedTableFiledName = ResolveQueryQualifiedFieldName(query, fielIdFied.SysTableFiledPath, databaseFixture.SqlServerType.Value);

                var paramterList = new List<System.Data.Common.DbParameter>();

                // No where clause
                if (query.IndexOf("WHERE", StringComparison.InvariantCultureIgnoreCase) == -1)
                {
                    string inCause = " WHERE" + DBInteractionBase.GenerateColumnInClauseWithAndCondition(fileIds, qulifiedTableFiledName, false);
                    query = query + inCause;
                }
                else
                {
                    string inCause = DBInteractionBase.GenerateColumnInClauseWithAndCondition(fileIds, qulifiedTableFiledName, true);
                    query = query + inCause;

                }


                return databaseFixture.RetriveDataTable(query, paramterList);//new DBInteractionBase(connectInfo).RetriveDataTable(query);

            }

            return new DataTable();
        }

        private static string ResolveQueryQualifiedFieldName(string queryText, string sysTableFieldPath, EmSqlType sqlServerType)
        {
            if (string.IsNullOrWhiteSpace(sysTableFieldPath))
            {
                return AppMetaDataBL.GetQulifiedTableFiledName(sysTableFieldPath, sqlServerType);
            }

            int dotIndex = sysTableFieldPath.IndexOf('.');
            if (dotIndex > 0 && dotIndex < sysTableFieldPath.Length - 1)
            {
                string tablePart = sysTableFieldPath.Substring(0, dotIndex);
                string columnPart = sysTableFieldPath.Substring(dotIndex + 1);
                return AppMetaDataBL.GetQulifiedTableFiledName(tablePart, sqlServerType)
                    + "." + AppMetaDataBL.GetQulifiedTableFiledName(columnPart, sqlServerType);
            }

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                string pattern = $@"(?:\[[^\]]+\]|[^\s\.,\[\]]+)\.{Regex.Escape(sysTableFieldPath)}\b";
                Match match = Regex.Match(queryText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Value;
                }
            }

            return AppMetaDataBL.GetQulifiedTableFiledName(sysTableFieldPath, sqlServerType);
        }

        internal static Dictionary<int, int> CountFolderContentByFolderView(int transactionId)
        {
            var dict = new Dictionary<int, int>();

            var defaultNav = AppTransactionNavigationBL.RetrieveOneTransactionDefaultNavigationDto(transactionId, true);
            if (defaultNav?.FolderViewId.HasValue != true)
            {
                return dict;
            }

            AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(defaultNav.FolderViewId);
            if (viewEntity?.AppDataSet == null || string.IsNullOrWhiteSpace(viewEntity.AppDataSet.QueryText))
            {
                return dict;
            }

            var folderIdField = viewEntity.AppSearchViewField?
                .FirstOrDefault(o => o.IsFileFoderId.HasValue && o.IsFileFoderId.Value);
            if (folderIdField == null || string.IsNullOrWhiteSpace(folderIdField.SysTableFiledPath))
            {
                return dict;
            }

            if (!viewEntity.DataSetId.HasValue)
            {
                return dict;
            }

            var dataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(viewEntity.DataSetId.Value);
            if (dataSetDto?.DataSourceFrom == null)
            {
                return dict;
            }

            var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSetDto.DataSourceFrom.Value);
            if (databaseFixture?.SqlServerType == null)
            {
                return dict;
            }

            string baseQuery = viewEntity.AppDataSet.QueryText.Trim().TrimEnd(';');
            string folderColumnName = GetFolderViewColumnName(folderIdField.SysTableFiledPath);

            var transRootField = viewEntity.AppSearchViewField?
                .FirstOrDefault(o => o.IsTransRootId.HasValue && o.IsTransRootId.Value);
            string recycleBinFilter = string.Empty;
            if (transRootField != null && !string.IsNullOrWhiteSpace(transRootField.SysTableFiledPath))
            {
                string rootColumnName = GetFolderViewColumnName(transRootField.SysTableFiledPath);
                recycleBinFilter = $@"
  AND Src.[{rootColumnName}] NOT IN (
      SELECT RootKeyValueID FROM AppTrascationRecycleBin WHERE TranscationID = {transactionId}
  )";
            }

            string query = $@"
SELECT Src.[{folderColumnName}] AS FolderID, COUNT(*) AS ContentCount
FROM (
{baseQuery}
) AS Src
WHERE Src.[{folderColumnName}] IS NOT NULL{recycleBinFilter}
GROUP BY Src.[{folderColumnName}]";

            try
            {
                DataTable dt = databaseFixture.RetriveDataTable(query, new List<System.Data.Common.DbParameter>());
                foreach (DataRow dataRow in dt.Rows)
                {
                    int? folderId = ControlTypeValueConverter.ConvertValueToInt(dataRow["FolderID"].ToString());
                    int? contentCount = ControlTypeValueConverter.ConvertValueToInt(dataRow["ContentCount"].ToString());
                    if (folderId.HasValue && contentCount.HasValue)
                    {
                        dict[folderId.Value] = contentCount.Value;
                    }
                }
            }
            catch (Exception)
            {
                dict.Clear();
            }

            return dict;
        }

        private static string GetFolderViewColumnName(string sysTableFieldPath)
        {
            if (string.IsNullOrWhiteSpace(sysTableFieldPath))
            {
                return sysTableFieldPath;
            }

            int dotIndex = sysTableFieldPath.LastIndexOf('.');
            return dotIndex >= 0 && dotIndex < sysTableFieldPath.Length - 1
                ? sysTableFieldPath.Substring(dotIndex + 1)
                : sysTableFieldPath;
        }

        internal static DataTable RetriveFolderSearchViewDataTable(int? folderId, AppSearchViewEntity viewEntity, int? transactionId)
        {

            DataTable toReturn = new DataTable();

            var folderIdFied = viewEntity.AppSearchViewField.Where(o => o.IsFileFoderId.HasValue && o.IsFileFoderId.Value).FirstOrDefault();

            var IsTransRootIdFied = viewEntity.AppSearchViewField.Where(o => o.IsTransRootId.HasValue && o.IsTransRootId.Value).FirstOrDefault();

            if (folderIdFied != null)
            {

                if (!viewEntity.DataSetId.HasValue)
                {
                    return new DataTable();
                }

                int dataSetId = viewEntity.DataSetId.Value;

                var aAppDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(dataSetId);
                if (aAppDataSetExDto?.DataSourceFrom == null)
                {
                    return new DataTable();
                }

                var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);
                if (databaseFixture?.SqlServerType == null)
                {
                    return new DataTable();
                }

                List<SqlParameter> sqlparamterList = new List<SqlParameter>();
                var parameterFolderId = new SqlParameter("@folderId", folderId);
                sqlparamterList.Add(parameterFolderId);


                if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType == (int)EmAppDataServiceType.QueryText)
                {
                    string query = viewEntity.AppDataSet.QueryText;
                    string quliFolderIdFieldName = ResolveQueryQualifiedFieldName(query, folderIdFied.SysTableFiledPath, databaseFixture.SqlServerType.Value);

                    // No where clause
                    if (query.IndexOf("WHERE", StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        query = query + string.Format(" WHERE {0}={1}", quliFolderIdFieldName, parameterFolderId.ParameterName);
                    }
                    else
                    {
                        query = query + string.Format(" AND  ( {0}={1} ) ", quliFolderIdFieldName, parameterFolderId.ParameterName);

                    }

                    if (transactionId.HasValue && IsTransRootIdFied != null)
                    {
                        string qulFielIdFieldName = ResolveQueryQualifiedFieldName(query, IsTransRootIdFied.SysTableFiledPath, databaseFixture.SqlServerType.Value);

                        string notInREcycle = " SELECT RootKeyValueID FROM AppTrascationRecycleBin WHERE  TranscationID=" + transactionId;

                        query = query + string.Format(" AND {0} NOT IN ({1})", qulFielIdFieldName, notInREcycle);




                    }

                    //return databaseFixture.RetriveDataTable(query, paramterList);//new DBInteractionBase(connectInfo).RetriveDataTable(query);

                    using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        DataTable dt = new DataTable();
                        dt = adpater.ExecuteDataTableRetrievalQuery(query, sqlparamterList);

                        return dt;

                    }

                }
            }




            return new DataTable();






        }


        internal static DataTable RetriveFolderSearchViewDataTable(List<int> fileIds, AppSearchViewEntity viewEntity)
        {

            DataTable toReturn = new DataTable();

            var folderIdFied = viewEntity.AppSearchViewField.Where(o => o.IsFileFoderId.HasValue && o.IsFileFoderId.Value).FirstOrDefault();

            if (folderIdFied != null)
            {

                int dataSetId = viewEntity.DataSetId.Value;

                var aAppDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(dataSetId);
                var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

                var paramterList = new List<System.Data.Common.DbParameter>();

                DbParameter parameter = databaseFixture.CreateParameter("folderId");
                //	parameter.Value = folderId;

                paramterList.Add(parameter);


                if (aAppDataSetExDto.QueryType.HasValue && aAppDataSetExDto.QueryType == (int)EmAppDataServiceType.QueryText)
                {
                    string query = viewEntity.AppDataSet.QueryText;
                    string qulifiedTableFiledName = ResolveQueryQualifiedFieldName(query, folderIdFied.SysTableFiledPath, databaseFixture.SqlServerType.Value);

                    // No where clause
                    if (query.IndexOf("WHERE", StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        query = query + string.Format(" WHERE {0}={1}", qulifiedTableFiledName, parameter.ParameterName);
                    }
                    else
                    {
                        query = query + string.Format(" AND  ( {0}={1} ) ", qulifiedTableFiledName, parameter.ParameterName);

                    }


                    return databaseFixture.RetriveDataTable(query, paramterList);//new DBInteractionBase(connectInfo).RetriveDataTable(query);

                }
            }




            return new DataTable();






        }

        internal static IEnumerable<StaticSearchResultRowJsonDto> RetrieveViewAllRecordResult(int viewID)
        {

            AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(viewID);
            DataTable result = GetMasterDatasetwDataTableFromViewEntity(viewEntity);

            //if(viewEntity.DataSetId )



            return ConvertSearchResultToJsonRow(viewEntity, result, null);



            // return resultList;
        }

        internal static DataTable GetMasterDatasetwDataTableFromViewEntity(AppSearchViewEntity viewEntity)
        {
            var selectViewColumns = viewEntity.AppSearchViewField.Where(o => (!string.IsNullOrEmpty(o.SysTableFiledPath))).Select(o => o.SysTableFiledPath).Distinct().ToList();
            if (selectViewColumns.Count == 0)
            {
                return new DataTable();
            }
            else
            {

                // Assume it is Master Data Source( Based Data Source)
                AppDataSetEntity masterDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(viewEntity.DataSetId);


                // need to override
                if (masterDataSetEntity.BaseDataSetId.HasValue)
                {
                    masterDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(masterDataSetEntity.BaseDataSetId);

                    // Get All Base View Column if it is extract date set
                    selectViewColumns = AppDataSetBL.RetrieveQueryColumnList(masterDataSetEntity.DataSetId).Select(o => o.Id.ToString()).ToList();
                }
                // AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(masterDataSetEntity.DataSourceFrom.Value);



                string query = ParseSelectFiledWithStarQuery(masterDataSetEntity.QueryText, selectViewColumns, new List<string>(), fixture.SqlServerType.Value);

                return fixture.RetriveDataTable(query, new List<System.Data.Common.DbParameter>());

            }
        }




        internal static List<StaticSearchResultRowJsonDto> ConvertSearchResultToJsonRow(AppSearchViewEntity viewEntity, DataTable dataTableResult, SearchDto searchDto)
        {
            Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList = GetViewEntityColumnLookupDict(viewEntity);

            List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
            Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

            //View Link target referecen

            // default
            string specificTimeToken = ServerContext.Instance.CurrentUserTimeZoneKey;
            if (searchDto != null)
            {
                SearchCriteriaDto searchCriteriaDto = searchDto.Criterias
                    .FirstOrDefault(o => o.CriteriaSubType.HasValue && o.CriteriaSubType.Value == (int)EmAppSearchFieldSubControlType.TimeZoneTokenInput);
                if (searchCriteriaDto != null && searchCriteriaDto.Value != null)
                {
                    specificTimeToken = searchCriteriaDto.Value.ToString();
                }
            }
            // need to override  specificTimeToken from searchDto serchCretia

            List<StaticSearchResultRowJsonDto> rows = new List<StaticSearchResultRowJsonDto>();
            foreach (DataRow aTableRow in dataTableResult.Rows)
            {
                StaticSearchResultRowJsonDto row = new StaticSearchResultRowJsonDto();
                rows.Add(row);
                foreach (var aColumn in viewEntity.AppSearchViewField.Where(o => !(o.IsCalulationField.HasValue && o.IsCalulationField.Value)))
                {
                    string columnName = aColumn.SysTableFiledPath;
                    int columnViewId = aColumn.SearchViewFieldId;

                    object value = ControlTypeValueConverter.ConvertValueToObject(aTableRow[columnName], aColumn.ControlType.Value);
                    object orgValue = value;


                    //if (linkTargetColumnIds.Contains(columnViewId))
                    //{
                    //    row.DictLinkTargetParameterValue.Add(columnViewId, value);
                    //}

                    // if it is not mass udpate 
                    value = PrepareOneSearchResultRowCellValue(viewEntity, dictColumnIdValuesList, aColumn, value, specificTimeToken);

                    row.DictViewColumnIDKeyValue.Add(columnViewId, value);
                    SetGanttViewRowProperties(viewEntity.ViewType, dictUserIdName, row, aColumn, value, orgValue, specificTimeToken);
                }

                if (viewEntity.ViewType == (int)EmAppViewType.ClusterAnalysisView)
                {
                    var viewDto = AppSearchViewConverter.ConvertEntityToDto(viewEntity);

                    if (viewDto.OtherSettingsDto != null && viewDto.OtherSettingsDto.ClusterChildViewItemList != null)
                    {
                        viewDto.OtherSettingsDto.ClusterChildViewItemList.Where(o => o.ViewType.HasValue && (o.ViewType.Value == (int)EmAppViewType.CalendarView || o.ViewType.Value == (int)EmAppViewType.GanttView || o.ViewType.Value == (int)EmAppViewType.SchedulerView))



                        .ForAll(clusterChildViewItemDto =>
                        {
                            if (row.DictClusterChildViewUiIdAndResultRowJsonDto == null)
                            {
                                row.DictClusterChildViewUiIdAndResultRowJsonDto = new Dictionary<string, StaticSearchResultRowJsonDto>();
                            }

                            StaticSearchResultRowJsonDto clusterChildResult = new StaticSearchResultRowJsonDto();
                            clusterChildResult.DictViewColumnIDKeyValue = row.DictViewColumnIDKeyValue;

                            row.DictClusterChildViewUiIdAndResultRowJsonDto.Add(clusterChildViewItemDto.UiId, clusterChildResult);



                            //foreach (var aColumn in clusterChildViewItemDto.ClusterViewItemColumns)
                            //{
                            //    var foundField = viewEntity.AppSearchViewField.FirstOrDefault(o => 
                            //        o.SysTableFiledPath == aColumn.SysTableFiledPath && o.DisplayText == aColumn.DisplayName && o.ControlType.HasValue && o.ControlType.Value == aColumn.ControlType);

                            //    if (foundField != null)
                            //    {
                            //        object orgValue = ControlTypeValueConverter.ConvertValueToObject(aTableRow[foundField.SysTableFiledPath], aColumn.ControlType);
                            //        object value = row.DictViewColumnIDKeyValue[foundField.SearchViewFieldId];

                            //        SetGanttViewRowProperties(clusterChildViewItemDto.ViewType.Value, dictUserIdName, clusterChildResult, foundField, value, orgValue, specificTimeToken);
                            //    }


                            //}

                            foreach (var aColumn in clusterChildViewItemDto.AppSearchViewFieldList)
                            {


                                var foundField = viewEntity.AppSearchViewField.FirstOrDefault(o =>
                                    o.SysTableFiledPath == aColumn.SysTableFiledPath && o.DisplayText == aColumn.DisplayText && aColumn.ControlType.HasValue && o.ControlType.HasValue && o.ControlType.Value == aColumn.ControlType.Value);

                                if (foundField != null)
                                {
                                    object orgValue = ControlTypeValueConverter.ConvertValueToObject(aTableRow[foundField.SysTableFiledPath], aColumn.ControlType.Value);
                                    object value = row.DictViewColumnIDKeyValue[foundField.SearchViewFieldId];

                                    SetGanttViewRowProperties(clusterChildViewItemDto.ViewType.Value, dictUserIdName, clusterChildResult, foundField, value, orgValue, specificTimeToken);
                                }


                            }
                        });
                    }
                }
            }





            PopulateSearchResultThumbnailUrls(viewEntity, rows);

            return rows;
        }

        /// <summary>
        /// Fills DictThumbnailUrl for Image columns using one batch AppFile lookup per search result.
        /// </summary>
        internal static void PopulateSearchResultThumbnailUrls(AppSearchViewEntity viewEntity, List<StaticSearchResultRowJsonDto> rows)
        {
            if (viewEntity?.AppSearchViewField == null || rows == null || rows.Count == 0)
                return;

            var imageColumns = viewEntity.AppSearchViewField
                .Where(f => f.ControlType.HasValue && f.ControlType.Value == (int)EmAppControlType.Image)
                .ToList();
            if (imageColumns.Count == 0)
                return;

            var fileIds = new HashSet<int>();
            foreach (StaticSearchResultRowJsonDto row in rows)
            {
                foreach (var column in imageColumns)
                {
                    if (!row.DictViewColumnIDKeyValue.TryGetValue(column.SearchViewFieldId, out object? raw) || raw == null)
                        continue;

                    if (int.TryParse(raw.ToString(), out int fileId) && fileId > 0)
                        fileIds.Add(fileId);
                }
            }

            if (fileIds.Count == 0)
                return;

            Dictionary<int, string> urlByFileId = AppFileBL.RetrieveThumbnailResourceUrlsByFileIds(fileIds);
            if (urlByFileId.Count == 0)
                return;

            foreach (StaticSearchResultRowJsonDto row in rows)
            {
                foreach (var column in imageColumns)
                {
                    if (!row.DictViewColumnIDKeyValue.TryGetValue(column.SearchViewFieldId, out object? raw) || raw == null)
                        continue;

                    if (!int.TryParse(raw.ToString(), out int fileId) || fileId <= 0)
                        continue;

                    if (urlByFileId.TryGetValue(fileId, out string? url) && !string.IsNullOrWhiteSpace(url))
                        row.DictThumbnailUrl[column.SearchViewFieldId] = url;
                }
            }
        }

        internal static object PrepareOneSearchResultRowCellValue(AppSearchViewEntity viewEntity,
            Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, AppSearchViewFieldEntity aColumn, object value, string specificTimeToken)
        {
            value = PrepareOneSearchResultRowCellValue_PrepareDDLCellValue(viewEntity, dictColumnIdValuesList, aColumn, value);

            if (aColumn.ControlType.Value == (int)EmAppControlType.CheckBox)
            {
                if (value == null)
                {
                    value = false;
                }
            }

            //if (value != null && (aColumn.ControlType.Value == (int)EmAppControlType.DateTimeDetail))
            //{

            //    value = TimeZoneHelper.ConvertUTCToClientDateTime((value as DateTime?), specificTimeToken);





            //}
            bool isNeedToConvertDateToClientTime = !(viewEntity.IsDisableClientTimeConvert.HasValue && viewEntity.IsDisableClientTimeConvert.Value);

            if (value != null &&
                    (aColumn.ControlType.Value == (int)EmAppControlType.DateTimeDetail
                        ||
                        ((viewEntity.ViewType == (int)EmAppViewType.CalendarView || viewEntity.ViewType == (int)EmAppViewType.SchedulerView || viewEntity.ViewType == (int)EmAppViewType.GanttView)
                            && isNeedToConvertDateToClientTime &&
                            (aColumn.DisplayText == EventStartDatePropertyName
                            || aColumn.DisplayText == EventEndDatePropertyName
                            || aColumn.DisplayText == EventActualStartDatePropertyName
                            || aColumn.DisplayText == EventActualEndDatePropertyName)
                        )
                    )
                )
            {
                DateTime? aDateTime = ControlTypeValueConverter.ConvertValueToDate(value);

                value = TimeZoneHelper.ConvertUTCToClientDateTime(aDateTime, specificTimeToken);

            }

            return value;
        }

        internal static object PrepareOneSearchResultRowCellValue_PrepareDDLCellValue(AppSearchViewEntity viewEntity, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, AppSearchViewFieldEntity aColumn, object value)
        {
            if (!(viewEntity.IsMassUpdateView.HasValue && viewEntity.IsMassUpdateView.Value))
            {
                if (value != null && aColumn.ControlType.Value == (int)EmAppControlType.DDL 
                    || aColumn.ControlType.Value == (int)EmAppControlType.AutoComplete
                    || aColumn.ControlType.Value == (int)EmAppControlType.SearchAbleDDL)
                {
                    if (aColumn.EntityId.HasValue)
                    {
                        int viewColumnId = aColumn.SearchViewFieldId;
                        if (dictColumnIdValuesList.ContainsKey(viewColumnId))
                        {
                            Dictionary<string, string> aDict = dictColumnIdValuesList[viewColumnId];
                            string key = value.ToString();
                            if (aDict.ContainsKey(key))
                            {
                                value = aDict[key];
                            }

                            else
                            {
                                value = string.Empty;
                            }
                        }
                        else
                        {
                            value = string.Empty;
                        }
                    }
                }


            }

            return value;
        }

        public static void SetGanttViewRowProperties(int viewType, Dictionary<int, string> dictUserIdName, StaticSearchResultRowJsonDto row, AppSearchViewFieldEntity aColumn, object value, object orgValue, string specificTimeToken)
        {
            // bool isNeedToConvertDateToClientTime = !(viewEntity.IsDisableClientTimeConvert.HasValue && viewEntity.IsDisableClientTimeConvert.Value);

            if ((viewType == (int)EmAppViewType.CalendarView
                                    || viewType == (int)EmAppViewType.GanttView
                                    || viewType == (int)EmAppViewType.SchedulerView) && value != null)
            {
                string displayText = aColumn.DisplayText;

                if (displayText.Trim().ToLower() == EventNamePropertyName.ToLower())
                {
                    row.EventName = value.ToString();
                }
                else if (displayText.Trim().ToLower() == EventBodyPropertyName.ToLower())
                {
                    row.EventBody = value.ToString();
                }
                else if (displayText.Trim().ToLower() == EventStartDatePropertyName.ToLower())
                {
                    row.EventStartDate = ControlTypeValueConverter.ConvertValueToDate(value);

                    //if (isNeedToConvertDateToClientTime)
                    //{                       
                    //    row.EventStartDate = TimeZoneHelper.ConvertUTCToClientDateTime(row.EventStartDate, specificTimeToken);
                    //}

                    if (row.EventStartDate.HasValue)
                    {
                        row.EventStartDateString = row.EventStartDate.Value.ToString("yyyy'-'MM'-'dd HH':'mm':'ss'Z'"); // 2007-07-21 15:17:20Z                                
                    }
                }

                else if (displayText.Trim().ToLower() == EventEndDatePropertyName.ToLower())
                {
                    row.EventEndDate = ControlTypeValueConverter.ConvertValueToDate(value);

                    //if (isNeedToConvertDateToClientTime)
                    //{
                    //    row.EventEndDate = TimeZoneHelper.ConvertUTCToClientDateTime(row.EventEndDate, specificTimeToken);
                    //}

                    if (row.EventEndDate.HasValue)
                    {
                        row.EventEndDateString = row.EventEndDate.Value.ToString("yyyy'-'MM'-'dd HH':'mm':'ss'Z'"); // 2007-07-21 15:17:20Z                                
                    }
                }
                else if (displayText.Trim().ToLower() == EventActualStartDatePropertyName.ToLower())
                {
                    row.EventActualStartDate = ControlTypeValueConverter.ConvertValueToDate(value);

                    //if (isNeedToConvertDateToClientTime)
                    //{
                    //    row.EventActualStartDate = TimeZoneHelper.ConvertUTCToClientDateTime(row.EventActualStartDate, specificTimeToken);
                    //}

                    if (row.EventActualStartDate.HasValue)
                    {
                        row.EventActualStartDateString = row.EventActualStartDate.Value.ToString("yyyy'-'MM'-'dd HH':'mm':'ss'Z'"); // 2007-07-21 15:17:20Z                                
                    }
                }

                else if (displayText.Trim().ToLower() == EventActualEndDatePropertyName.ToLower())
                {
                    row.EventActualEndDate = ControlTypeValueConverter.ConvertValueToDate(value);

                    //if (isNeedToConvertDateToClientTime)
                    //{
                    //    row.EventActualEndDate = TimeZoneHelper.ConvertUTCToClientDateTime(row.EventActualEndDate, specificTimeToken);
                    //}

                    if (row.EventActualEndDate.HasValue)
                    {
                        row.EventActualEndDateString = row.EventActualEndDate.Value.ToString("yyyy'-'MM'-'dd HH':'mm':'ss'Z'"); // 2007-07-21 15:17:20Z                                
                    }
                }
                else if (displayText.Trim().ToLower() == EventCompletePercentagePropertyName.ToLower())
                {
                    double? percentage = ControlTypeValueConverter.ConvertValueToDouble(value);
                    row.EventCompletePercentage = percentage.HasValue ? percentage.Value : 0;
                }

                else if (displayText.Trim().ToLower() == EventTypePropertyName.ToLower())
                {
                    row.EventType = ControlTypeValueConverter.ConvertValueToInt(value);

                    if (!row.EventType.HasValue)
                    {
                        row.EventType = (int)EmAppCalendarViewEventType.Unspecified;
                        row.EventTypeDisplay = "Other";
                    }
                    else
                    {
                        row.EventTypeDisplay = ((EmAppCalendarViewEventType)row.EventType.Value).ToString();
                    }

                    row.EventTypeString = ((EmAppCalendarViewEventType)row.EventType.Value).ToString();
                }
                else if (displayText.Trim().ToLower() == EventCompletStagePropertyName.ToLower())
                {
                    row.EventCompletStage = ControlTypeValueConverter.ConvertValueToInt(value);

                    if (!row.EventCompletStage.HasValue)
                    {
                        if (viewType == (int)EmAppViewType.CalendarView || viewType == (int)EmAppViewType.SchedulerView)
                        {
                            row.EventCompletStage = (int)EmAppCalendarViewEventCompletStage.NotCompleted;
                        }
                        else if (viewType == (int)EmAppViewType.GanttView)
                        {
                            row.EventCompletStage = (int)EmAppProjectStage.Planning;
                        }
                    }

                    if (viewType == (int)EmAppViewType.CalendarView || viewType == (int)EmAppViewType.SchedulerView)
                    {
                        row.EventCompletStageString = ((EmAppCalendarViewEventCompletStage)row.EventCompletStage.Value).ToString();
                    }
                    else if (viewType == (int)EmAppViewType.GanttView)
                    {
                        row.EventCompletStageString = ((EmAppProjectStage)row.EventCompletStage.Value).ToString();
                    }

                }
                else if (displayText.Trim().ToLower() == EventStatusPropertyName.ToLower())
                {
                    row.EventStatus = ControlTypeValueConverter.ConvertValueToInt(value);

                    if (!row.EventStatus.HasValue)
                    {
                        if (viewType == (int)EmAppViewType.GanttView)
                        {
                            row.EventStatus = (int)EmAppProjectTaskStatus.NotAvailable;
                        }
                    }

                    if (viewType == (int)EmAppViewType.GanttView)
                    {
                        row.EventStatusString = ((EmAppProjectTaskStatus)row.EventStatus.Value).ToString();
                    }
                }
                else if (displayText.Trim().ToLower() == EventUserIdPropertyName.ToLower())
                {
                    row.EventUserId = ControlTypeValueConverter.ConvertValueToInt(orgValue);

                    if (row.EventUserId.HasValue && dictUserIdName.ContainsKey(row.EventUserId.Value))
                    {
                        row.EventUserDisplay = dictUserIdName[row.EventUserId.Value];
                    }
                }
                else if (displayText.Trim().ToLower() == EventDescription1PropertyName.ToLower())
                {
                    row.EventDescription1 = value.ToString();

                }
                else if (displayText.Trim().ToLower() == EventDescription2PropertyName.ToLower())
                {
                    row.EventDescription2 = value.ToString();

                }
                else if (displayText.Trim().ToLower() == EventGroupByIdPropertyName.ToLower())
                {
                    row.EventGroupById = orgValue;

                }

                else if (displayText.Trim().ToLower() == EventDateIdPropertyName.ToLower())
                {
                    row.EventDateId = ControlTypeValueConverter.ConvertValueToInt(orgValue);
                }
                else if (displayText.Trim().ToLower() == EventTransactionIdPropertyName.ToLower())
                {
                    row.EventTransactionId = ControlTypeValueConverter.ConvertValueToInt(orgValue); ;
                }
                else if (displayText.Trim().ToLower() == EventTransactionRIdPropertyName.ToLower())
                {
                    row.EventTransactionRId = orgValue;
                }
                else if (displayText.Trim().ToLower() == EventColorIdPropertyName.ToLower())
                {
                    row.EventColorId = value.ToString();
                }


            }
        }

        internal static DataTable RetriveMasterDataSetDataTable(SearchDto searchDto, AppSearchViewEntity viewEntity)
        {
            bool isQuickSearchWithOrSearch = searchDto.SearchType == (int)EmAppSearchUsageType.QuickSearch;
            bool isMyLastModifiedItemSearch = searchDto.SearchType == (int)EmAppSearchUsageType.MyLastModify;

            var selectViewColumns = viewEntity.AppSearchViewField.Where(o => (!string.IsNullOrEmpty(o.SysTableFiledPath)) && !(o.IsCalulationField.HasValue && o.IsCalulationField.Value)).Select(o => o.SysTableFiledPath).Distinct().ToList();
            if (selectViewColumns.Count == 0)
            {
                return new DataTable();
            }

            if (isQuickSearchWithOrSearch && string.IsNullOrEmpty(searchDto.QuickSearchValueText))
            {
                return new DataTable();
            }

            // Assume it is Master Data Source( Based Data Source)
            var masterDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(viewEntity.DataSetId);


            int? masterDataSetId = masterDataSetExDto.BaseDataSetId;
            // need to override
            if (masterDataSetId.HasValue)
            {
                masterDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(masterDataSetId);

                // Get All Base View Column if it is extract date set
                selectViewColumns = AppDataSetBL.RetrieveQueryColumnList(masterDataSetId.Value).Select(o => o.Id.ToString()).ToList();
            }





            var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(masterDataSetExDto.DataSourceFrom.Value);

            if (masterDataSetExDto.QueryType.HasValue && masterDataSetExDto.QueryType == (int)EmAppDataServiceType.QueryText)
            {
                string whereclause = string.Empty;
                List<string> condtionString = new List<string>();

                if (isMyLastModifiedItemSearch)
                {
                    condtionString.Add(AppModifiedByIDColumnName + "=" + AppSecurityUserBL.CurrentUserEntity.UserId.ToString());
                }
                else if (isQuickSearchWithOrSearch)
                {
                    foreach (SearchCriteriaDto searchCriteriaDto in searchDto.Criterias)
                    {
                        searchCriteriaDto.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.Like;
                        searchCriteriaDto.Values.Add(searchDto.QuickSearchValueText);

                        //searchCriteriaDto.SearcDCUID

                        string dbQulifiedFiedname = AppMetaDataBL.GetQulifiedTableFiledName(searchCriteriaDto.SysTableFiledPath, databaseFixture.SqlServerType.Value);

                        string condition = new SearchCriteriaSqlTranslator(dbQulifiedFiedname, searchCriteriaDto).ComputeCondition();

                        if (condition != string.Empty)
                        {
                            condtionString.Add(condition);
                        }

                    }
                }
                else
                {
                    foreach (SearchCriteriaDto searchCriteriaDto in searchDto.Criterias)
                    {

                        if (searchCriteriaDto.CriteriaSubType.HasValue && searchCriteriaDto.CriteriaSubType.Value == (int)EmAppSearchFieldSubControlType.TimeZoneTokenInput)
                        {
                            continue;
                        }


                        string dbQulifiedFiedname = AppMetaDataBL.GetQulifiedTableFiledName(searchCriteriaDto.SysTableFiledPath, databaseFixture.SqlServerType.Value);

                        string condition = new SearchCriteriaSqlTranslator(dbQulifiedFiedname, searchCriteriaDto).ComputeCondition();

                        // need to override Date >= <=
                        if (searchCriteriaDto.EmInternalCodeRegistration.HasValue &&
                            searchCriteriaDto.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.UptoUtcTodayDateDiff
                          )
                        {
                            string operaType = "=";
                            var opratorType = searchCriteriaDto.CriteriaOperator.OperatorType;

                            if (opratorType == EmAppCriteriaOperatorType.GreaterThan)
                            {
                                operaType = " > ";
                            }
                            else if (opratorType == EmAppCriteriaOperatorType.GreaterThanOrEquals)
                            {
                                operaType = " >= ";

                            }
                            else if (opratorType == EmAppCriteriaOperatorType.LessThan)
                            {
                                operaType = " < ";
                            }
                            else if (opratorType == EmAppCriteriaOperatorType.LessThanOrEquals)
                            {
                                operaType = " <= ";
                            }

                            string defaultDiffDay = "1";
                            if (!string.IsNullOrWhiteSpace(searchCriteriaDto.OriginalDefaultValue))
                            {
                                defaultDiffDay = searchCriteriaDto.OriginalDefaultValue;
                            }

                            //Regex.Match(content, @"\b(where|orderby|top)\b");
                            condition = string.Format(@"  DATEDIFF( day ,  {0},GETUTCDATE()) {1} {2}", dbQulifiedFiedname, operaType, defaultDiffDay);
                        }
                        else
                        {

                            condition = PorcessDateEquals(searchCriteriaDto, dbQulifiedFiedname, condition);
                        }


                        if (condition != string.Empty)
                        {
                            condtionString.Add(condition);
                        }
                    }
                }

                // filter 
                // need to check security

                if (!viewEntity.NoSecurity)
                {
                    var searchExdto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchDto.Id);

                    bool isAdmin = AppSecurityUserBL.IsAdminUser();

                    if (!isAdmin)
                    {
                        // check specific currnet user user security 
                        if (!string.IsNullOrWhiteSpace(searchExdto.FilterByCurrentUserMappingField))
                        {
                            //List<int> ignoreFileterSearchIds = AppSecurityManagementBL.RetrieveUserIgnoreFilterByCurrentUserSearchIds();


                            //if (!ignoreFileterSearchIds.Contains((int)searchExdto.Id)) // need to filter by current uses
                            //{
                            condtionString.Add(searchExdto.FilterByCurrentUserMappingField + "=" + AppSecurityUserBL.CurrentUserEntity.UserId);
                            // }
                        }
                        else // check parter filter
                        {
                            AppComBusinessPartnerFilterBL.SetupBusinessPartnerFilterCondition(viewEntity, condtionString);
                        }



                    }
                }

                //?????
                string query = ParseSelectFiledWithStarQuery(masterDataSetExDto.QueryText, selectViewColumns, condtionString, databaseFixture.SqlServerType.Value, isQuickSearchWithOrSearch, searchDto.TopNbResult);

                if (isMyLastModifiedItemSearch)
                {
                    query = query + "Order by" + AppModifiedDateColumnName + " DESC";
                }
                return databaseFixture.RetriveDataTable(query, new List<System.Data.Common.DbParameter>());//new DBInteractionBase(connectInfo).RetriveDataTable(query);

            }
            else if (masterDataSetExDto.QueryType.HasValue && masterDataSetExDto.QueryType == (int)EmAppDataServiceType.StoredProcedure)// it is  stor proce
            {
                if (searchDto.Id == null)
                {

                }

                AppSearchEntity searchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(searchDto.Id);

                var dictParamterIdInSearch = searchEntity.AppSearchParameter.ToDictionary(o => o.ParameterId.Value, o => o);

                var dictSearchFiedIdCriteria = searchDto.Criterias.ToDictionary(o => o.SearcDCUID, o => o);

                List<SearchCriteriaDto> storeProcSearchCriteriaList = new List<SearchCriteriaDto>();

                foreach (var dataSetparamter in masterDataSetExDto.AppDataSetParameterList)
                {
                    int dataSetparamterId = (int)dataSetparamter.Id;

                    if (dictParamterIdInSearch.ContainsKey(dataSetparamterId))
                    {

                        var aAppSearchParameterEntity = dictParamterIdInSearch[dataSetparamterId];

                        //First level overide
                        if (!string.IsNullOrWhiteSpace(aAppSearchParameterEntity.DefaultValue))
                        {
                            dataSetparamter.SqlParameterValue = aAppSearchParameterEntity.DefaultValue;
                        }

                        int searchField = aAppSearchParameterEntity.SearchFieldId.Value;

                        //Second level overide
                        if (dictSearchFiedIdCriteria.ContainsKey(searchField))
                        {
                            SearchCriteriaDto aSearchCriteriaDto = dictSearchFiedIdCriteria[searchField];
                            if (aSearchCriteriaDto.CriteriaOperator != null && aSearchCriteriaDto.CriteriaOperator.OperatorType.HasValue
                                && aSearchCriteriaDto.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Equals)
                            {
                                if (aSearchCriteriaDto.Value != null)
                                {
                                    dataSetparamter.SqlParameterValue = aSearchCriteriaDto.Value;
                                    storeProcSearchCriteriaList.Add(aSearchCriteriaDto);

                                }

                            }

                        }

                    }

                }



                DataTable dt = AppDataSetBL.CallDataSetStoreProcedure(masterDataSetExDto);


                List<KeyValuePair<string, SearchCriteriaDto>> mapingRefViewColumnToSearchCreatia = new List<KeyValuePair<string, SearchCriteriaDto>>();

                foreach (SearchCriteriaDto searchCriteriaDto in searchDto.Criterias)
                {


                    if (string.IsNullOrWhiteSpace(searchCriteriaDto.SysTableFiledPath))
                        continue;

                    var sharedParameterBetweenSearchAndDataSet = storeProcSearchCriteriaList.FirstOrDefault(o => o.SearcDCUID == searchCriteriaDto.SearcDCUID);
                    if (sharedParameterBetweenSearchAndDataSet == null)
                    {

                        KeyValuePair<string, SearchCriteriaDto> filter = new KeyValuePair<string, SearchCriteriaDto>(searchCriteriaDto.SysTableFiledPath, searchCriteriaDto);
                        mapingRefViewColumnToSearchCreatia.Add(filter);
                    }


                }

                // need to fitler Dt by the 


                return FilterGridResultView(mapingRefViewColumnToSearchCreatia, dt);



            }
            else if (masterDataSetExDto.QueryType.HasValue && masterDataSetExDto.QueryType == (int)EmAppDataServiceType.PluginWebApiCall)// it is  PluginWebApiCall
            {

            }

            return new DataTable();
        }

        private static string PorcessDateEquals(SearchCriteriaDto searchCriteriaDto, string dbQulifiedFiedname, string condition)
        {
            if (searchCriteriaDto.CriteriaType == EmAppCriteriaType.Date
                && searchCriteriaDto.CriteriaOperator != null && searchCriteriaDto.CriteriaOperator.OperatorType.HasValue
                && searchCriteriaDto.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Equals)
            {
                if (!searchCriteriaDto.Values.IsEmpty())
                {
                    DateTime? startValue = ControlTypeValueConverter.ConvertValueToDate(searchCriteriaDto.Values[0]);
                    if (startValue.HasValue)
                    {
                        if (searchCriteriaDto.ControlType.HasValue && searchCriteriaDto.ControlType.Value == (int)EmAppControlType.DateTimeDetail)
                        {
                            var startValue_Date_Client = ClientTimeZoneHelper.ConvertUTCToClientDateTime(startValue.Value).Date;
                            var startValue_Utc = ClientTimeZoneHelper.ConvertClientToUTCDateTime(startValue_Date_Client);
                            startValue = startValue_Utc;
                        }


                        DateTime endtValue = startValue.Value.AddDays(1);

                        SearchCriteriaDto aSearchCriteriaDto1 = new SearchCriteriaDto();
                        //aSearchCriteriaDto1
                        aSearchCriteriaDto1.CriteriaType = EmAppCriteriaType.Date;
                        aSearchCriteriaDto1.SearcDCUID = searchCriteriaDto.SearcDCUID;
                        aSearchCriteriaDto1.Values = new System.Collections.ObjectModel.ObservableCollection<object>();
                        aSearchCriteriaDto1.Values.Add(startValue);
                        aSearchCriteriaDto1.CriteriaOperator = new CriteriaOperatorDto();
                        aSearchCriteriaDto1.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.GreaterThanOrEquals;

                        aSearchCriteriaDto1.CriteriaOperator.IsEditorRequired = true;

                        // need to remove this onme
                        aSearchCriteriaDto1.CriteriaOperator.IsMultipleValuesAllowed = true;






                        string condition1 = new SearchCriteriaSqlTranslator(dbQulifiedFiedname, aSearchCriteriaDto1).ComputeCondition();


                        SearchCriteriaDto aSearchCriteriaDto2 = new SearchCriteriaDto();
                        aSearchCriteriaDto2.CriteriaType = EmAppCriteriaType.Date;
                        aSearchCriteriaDto2.SearcDCUID = searchCriteriaDto.SearcDCUID;
                        aSearchCriteriaDto2.Values = new System.Collections.ObjectModel.ObservableCollection<object>();
                        aSearchCriteriaDto2.Values.Add(endtValue);
                        aSearchCriteriaDto2.CriteriaOperator = new CriteriaOperatorDto();
                        aSearchCriteriaDto2.CriteriaOperator.OperatorType = EmAppCriteriaOperatorType.LessThan;
                        aSearchCriteriaDto2.CriteriaOperator.IsEditorRequired = true;

                        // need to remove this onme
                        aSearchCriteriaDto2.CriteriaOperator.IsMultipleValuesAllowed = true;

                        string condition2 = new SearchCriteriaSqlTranslator(dbQulifiedFiedname, aSearchCriteriaDto2).ComputeCondition();

                        condition = string.Format("( ({0}) AND ({1}) ) ", condition1, condition2);



                    }
                }

            }

            return condition;
        }

        internal static DataTable FilterGridResultView(List<KeyValuePair<string, SearchCriteriaDto>> mapingRefViewColumnToSearchCreatia, DataTable originalDataTable)
        {
            string filterString = string.Empty;

            foreach (var pair in mapingRefViewColumnToSearchCreatia)
            {
                string columnName = "[" + pair.Key + "]";
                SearchCriteriaDto staticFieldDto = pair.Value;

                if (staticFieldDto.CriteriaType == EmAppCriteriaType.Entity || staticFieldDto.CriteriaType == EmAppCriteriaType.Media)
                {
                    string condition = new SearchCriteriaSqlTranslator(columnName, staticFieldDto).ComputeDataTableSelectCondition();
                    if (condition != string.Empty)
                    {
                        filterString = filterString + condition + " AND ";
                    }
                }
                else if (staticFieldDto.CriteriaType == EmAppCriteriaType.Text || staticFieldDto.CriteriaType == EmAppCriteriaType.Boolean)
                {
                    string condition = new SearchCriteriaSqlTranslator(columnName, staticFieldDto).ComputeDataTableSelectCondition();
                    if (condition != string.Empty)
                    {
                        filterString = filterString + condition + " AND ";
                    }
                }
                else if (staticFieldDto.CriteriaType == EmAppCriteriaType.Date)
                {
                    string condition = new SearchCriteriaSqlTranslator(columnName, staticFieldDto).ComputeDataTableSelectCondition();
                    if (condition != string.Empty)
                    {
                        filterString = filterString + condition + " AND ";
                    }
                }

                else if (staticFieldDto.CriteriaType == EmAppCriteriaType.Numeric)
                {
                    string condition = new SearchCriteriaSqlTranslator(columnName, staticFieldDto).ComputeDataTableSelectCondition();
                    if (condition != string.Empty)
                    {
                        filterString = filterString + condition + " AND ";
                    }
                }
            }

            // must use Qulified Column !"["+aGridMetaColumn.DCUColumnID.ToString() + "]
            // DataRow[] aOneRows = aFkColumnTAble.Select("[" + aGridMetaColumn.DCUColumnID.ToString() + "]=" + valueID);
            // "(823=17)"
            if (filterString != string.Empty)
            {
                filterString = filterString.Substring(0, filterString.Length - 4).Trim();
                DataTable aNewDataTable = originalDataTable.Clone();

                try
                {
                    var filterResult = originalDataTable.Select(filterString);
                    foreach (DataRow afilterRow in filterResult)
                    {
                        aNewDataTable.ImportRow(afilterRow);
                        // aNewDataTable.Rows.Add(afilterRow);
                    }
                    return aNewDataTable;
                }
                catch
                {
                    return originalDataTable;

                }

            }
            else
            {

                return originalDataTable;
            }

            //   return new DataTable();
        }


        internal static Dictionary<int, Dictionary<string, string>> GetViewEntityColumnLookupDict(AppSearchViewEntity viewEntity)
        {
            Dictionary<int, int> dictColumnIdEntityId = viewEntity.AppSearchViewField.Where(o => o.EntityId.HasValue).ToDictionary(o => o.SearchViewFieldId, o => o.EntityId.Value);

            Dictionary<int, List<LookupItemDto>> dictEntityIdValuesList = new Dictionary<int, List<LookupItemDto>>();

            foreach (int entityId in dictColumnIdEntityId.Values)
            {
                if (!dictEntityIdValuesList.ContainsKey(entityId))
                    dictEntityIdValuesList.Add(entityId, AppEntityInfoBL.GetLookupItemList(entityId, ""));
            }

            Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList = new Dictionary<int, Dictionary<string, string>>();
            foreach (int columnId in dictColumnIdEntityId.Keys)
            {
                int entityId = dictColumnIdEntityId[columnId];
                dictColumnIdValuesList.Add(columnId, dictEntityIdValuesList[entityId].ToDictionary(o => o.Id.ToString(), o => o.Display));
            }
            return dictColumnIdValuesList;
        }

        //private static List<string> SetViewLinkTargetColumnIds(AppSearchViewEntity viewEntity)
        //{
        //    List<string> linkTargetColumnIds = new List<string>();
        //    foreach (var linkTarget in viewEntity.AppFormLinkTarget)
        //    {
        //        if (!string.IsNullOrEmpty(linkTarget.SourceColumn1))
        //        {
        //            linkTargetColumnIds.Add(linkTarget.SourceColumn1);
        //        }
        //        if (!string.IsNullOrEmpty(linkTarget.SourceColumn2))
        //        {
        //            linkTargetColumnIds.Add(linkTarget.SourceColumn3);
        //        }
        //        if (!string.IsNullOrEmpty(linkTarget.SourceColumn3))
        //        {
        //            linkTargetColumnIds.Add(linkTarget.SourceColumn3);
        //        }
        //    }
        //    return linkTargetColumnIds;
        //}

        private static void BuildFlatDataSetTreeViewResult(AppSearchViewExDto treeSearchViewExDto, SearchResultDto searchResultDto)
        {
            List<StaticSearchResultRowJsonDto> searchResultRows = searchResultDto.SearchResultRowList.ToList();

            AppCatalogueTreeDto appCatalogueTreeDto = new AppCatalogueTreeDto();
            appCatalogueTreeDto.Id = appCatalogueTreeDto.TreeViewEntityId = (int)treeSearchViewExDto.Id;
            appCatalogueTreeDto.Name = treeSearchViewExDto.Name;
            appCatalogueTreeDto.UiId = Guid.NewGuid().ToString();

            //AppDataSetExDto aAppDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(treeSearchViewExDto.DataSetId);

            //DataTable dataTable = new DataTable();
            //using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            //{
            //    dataTable = adpater.ExecuteDataTableRetrievalQuery(aAppDataSetExDto.QueryText, new List<System.Data.SqlClient.SqlParameter>());

            //}


            var dictLevelTree = AppCatalogueForEshopTreeV4BL.GetDictEshopTreeViewOrCardViewLevelNodeList(treeSearchViewExDto);

            List<int> levelNodeIdColumnName = new List<int>();
            foreach (int levelKey in dictLevelTree.Keys)
            {
                int? columnId_NodeId;
                int? columnId_NodeDisplay;
                int? columnId_NodeImage;
                int? columnId_NodeSort;



                AppCatalogueForEshopTreeV4BL.GetTreeViewNodeSpecialColumnId(dictLevelTree, levelKey, out columnId_NodeId, out columnId_NodeDisplay, out columnId_NodeImage, out columnId_NodeSort);



                if (columnId_NodeId.HasValue)
                {
                    levelNodeIdColumnName.Add(columnId_NodeId.Value);

                    // root level from 1
                    if (levelNodeIdColumnName.Count == 1)
                    {
                        AppCatalogueForEshopTreeV4BL.FirstlevelTree(appCatalogueTreeDto, searchResultRows, columnId_NodeId, columnId_NodeDisplay, columnId_NodeImage, columnId_NodeSort);
                    }
                    else if (levelNodeIdColumnName.Count == 2)
                    {
                        AppCatalogueForEshopTreeV4BL.SecondLevelTree(appCatalogueTreeDto, searchResultRows, levelNodeIdColumnName, columnId_NodeId, columnId_NodeDisplay, columnId_NodeImage, columnId_NodeSort);
                    }
                    else if (levelNodeIdColumnName.Count == 3)
                    {
                        AppCatalogueForEshopTreeV4BL.ThirdLevelTree(appCatalogueTreeDto, searchResultRows, levelNodeIdColumnName, columnId_NodeId, columnId_NodeDisplay, columnId_NodeImage, columnId_NodeSort);
                    }
                    else if (levelNodeIdColumnName.Count == 4)
                    {
                        AppCatalogueForEshopTreeV4BL.FourthLevelTree(appCatalogueTreeDto, searchResultRows, levelNodeIdColumnName, columnId_NodeId, columnId_NodeDisplay, columnId_NodeImage, columnId_NodeSort);
                    }
                    else if (levelNodeIdColumnName.Count == 5)
                    {
                        AppCatalogueForEshopTreeV4BL.FivelevelTree(appCatalogueTreeDto, searchResultRows, levelNodeIdColumnName, columnId_NodeId, columnId_NodeDisplay, columnId_NodeImage, columnId_NodeSort);
                    }
                }

            }

            List<StaticSearchResultRowJsonDto> newSearchResultRowList = new List<StaticSearchResultRowJsonDto>();

            var rootRow = new StaticSearchResultRowJsonDto();
            rootRow.DictViewColumnIDKeyValue = new Dictionary<int, object>();
            rootRow.BaseAppCatalogueTreeDto = new BaseAppCatalogueTreeDto();
            rootRow.BaseAppCatalogueTreeDto.Name = "All";
            rootRow.BaseAppCatalogueTreeDto.UiId = appCatalogueTreeDto.UiId;

            List<StaticSearchResultRowJsonDto> levelOneRows = new List<StaticSearchResultRowJsonDto>();
            rootRow.Children = levelOneRows;
            newSearchResultRowList.Add(rootRow);

            if (appCatalogueTreeDto.Children != null)
            {
                foreach (AppCatalogueTreeDto folder in appCatalogueTreeDto.Children)
                {
                    var resultRowDto = ConvertOneCatalogueTreeDtoToSearchResultRowJsonDto(folder, null, treeSearchViewExDto, searchResultDto);

                    if (resultRowDto.BaseAppCatalogueTreeDto != null && resultRowDto.BaseAppCatalogueTreeDto.Id != null
                        && !string.IsNullOrWhiteSpace(resultRowDto.BaseAppCatalogueTreeDto.Id.ToString()))
                    {
                        levelOneRows.Add(resultRowDto);
                    }
                }
            }

            searchResultDto.SearchResultRowList = newSearchResultRowList;

            BuildFlatDataSetTreeViewChildSearchResult(treeSearchViewExDto, searchResultDto);
        }

        private static void BuildFlatDataSetTreeViewChildSearchResult(AppSearchViewExDto searchViewDto, SearchResultDto searchResultDto)
        {
            if (searchViewDto != null && searchResultDto != null)
            {
                if (searchViewDto.AppViewLinkedSeaechOrUrlList != null && searchViewDto.AppViewLinkedSeaechOrUrlList.Count > 0)
                {
                    var linkedSearchDto = searchViewDto.AppViewLinkedSeaechOrUrlList.FirstOrDefault();

                    if (linkedSearchDto.LinkTargetSearchId.HasValue)
                    {
                        SearchDto searchDto = AppSearchBL.RetrieveOneSearchDto(linkedSearchDto.LinkTargetSearchId.Value, false, false);
                        ReferenceViewDto referenceViewDto = searchDto.DefaultView;

                        if (searchResultDto.DefaultSelectedRowDto != null)
                        {
                            if (linkedSearchDto.SourceViewColumnId1.HasValue && linkedSearchDto.TargetSearchFieldId1.HasValue)
                            {
                                var criteria1 = searchDto.Criterias.FirstOrDefault(o => o.SearcDCUID == linkedSearchDto.TargetSearchFieldId1.Value);
                                if (criteria1 != null)
                                {
                                    criteria1.Value = searchResultDto.DefaultSelectedRowDto.DictViewColumnIDKeyValue[linkedSearchDto.SourceViewColumnId1.Value];
                                }
                            }

                            if (linkedSearchDto.SourceViewColumnId2.HasValue && linkedSearchDto.TargetSearchFieldId2.HasValue)
                            {
                                var criteria2 = searchDto.Criterias.FirstOrDefault(o => o.SearcDCUID == linkedSearchDto.TargetSearchFieldId2.Value);
                                if (criteria2 != null)
                                {
                                    criteria2.Value = searchResultDto.DefaultSelectedRowDto.DictViewColumnIDKeyValue[linkedSearchDto.SourceViewColumnId2.Value];
                                }
                            }

                            if (linkedSearchDto.SourceViewColumnId3.HasValue && linkedSearchDto.TargetSearchFieldId3.HasValue)
                            {
                                var criteria3 = searchDto.Criterias.FirstOrDefault(o => o.SearcDCUID == linkedSearchDto.TargetSearchFieldId3.Value);
                                if (criteria3 != null)
                                {
                                    criteria3.Value = searchResultDto.DefaultSelectedRowDto.DictViewColumnIDKeyValue[linkedSearchDto.SourceViewColumnId3.Value];
                                }
                            }
                        }

                        AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(searchDto);
                        searchDto.ReferenceViewDefinitionDto = searchDto.DefaultView;
                        searchResultDto.ChildSearchResultDto = AppSearchBL.RetrieveSearchResult(searchDto);
                    }


                }
            }
        }

        private static StaticSearchResultRowJsonDto ConvertOneCatalogueTreeDtoToSearchResultRowJsonDto(AppCatalogueTreeDto folder, StaticSearchResultRowJsonDto parentRow, AppSearchViewExDto treeSearchViewExDto, SearchResultDto searchResultDto)
        {
            StaticSearchResultRowJsonDto resultRow = new StaticSearchResultRowJsonDto();
            List<StaticSearchResultRowJsonDto> children = new List<StaticSearchResultRowJsonDto>();

            resultRow.BaseAppCatalogueTreeDto = folder;

            if (parentRow != null)
            {
                resultRow.DictViewColumnIDKeyValue = parentRow.DictViewColumnIDKeyValue.DeepCopy();
            }
            else
            {
                resultRow.DictViewColumnIDKeyValue = new Dictionary<int, object>();
            }


            if (folder.columnId_NodeId.HasValue)
            {
                resultRow.DictViewColumnIDKeyValue[folder.columnId_NodeId.Value] = folder.Id;

                var nodeId_ViewFieldDto = treeSearchViewExDto.AppSearchViewFieldList.FirstOrDefault(o => o.Id.ToString() == folder.columnId_NodeId.Value.ToString());
                if (nodeId_ViewFieldDto != null)
                {
                    if (nodeId_ViewFieldDto.RowNumber.HasValue) // if has default selected node id value
                    {
                        int? nodeId = ControlTypeValueConverter.ConvertValueToInt(folder.Id);

                        if (nodeId.HasValue && nodeId.Value == nodeId_ViewFieldDto.RowNumber.Value)
                        {
                            resultRow.IsSelected = true;
                            searchResultDto.DefaultSelectedRowDto = resultRow;

                            if (parentRow != null)
                            {
                                parentRow.IsSelected = false;
                            }

                        }
                    }
                }

            }

            if (folder.columnId_NodeDisplay.HasValue)
            {
                resultRow.DictViewColumnIDKeyValue[folder.columnId_NodeDisplay.Value] = folder.Name;
            }

            if (folder.columnId_NodeImage.HasValue)
            {
                resultRow.DictViewColumnIDKeyValue[folder.columnId_NodeImage.Value] = folder.ImageId;
            }

            if (folder.Children != null)
            {
                foreach (var childFolder in folder.Children)
                {
                    StaticSearchResultRowJsonDto childResultRowDto = ConvertOneCatalogueTreeDtoToSearchResultRowJsonDto(childFolder, resultRow, treeSearchViewExDto, searchResultDto);

                    if (childResultRowDto.BaseAppCatalogueTreeDto != null && childResultRowDto.BaseAppCatalogueTreeDto.Id != null
                        && !string.IsNullOrWhiteSpace(childResultRowDto.BaseAppCatalogueTreeDto.Id.ToString()))
                    {
                        children.Add(childResultRowDto);
                    }

                    
                }
            }


            resultRow.Children = children;

            return resultRow;
        }

        private static List<StaticSearchResultRowJsonDto> BuildHierachyTreeViewResult(AppSearchViewEntity viewEntity, List<StaticSearchResultRowJsonDto> searchResultRows)
        {
            List<StaticSearchResultRowJsonDto> rootItems = new List<StaticSearchResultRowJsonDto>();

            var treeNodeColumn = viewEntity.AppSearchViewField.FirstOrDefault(o => o.IsUserDefined1.HasValue && o.IsUserDefined1.Value);
            var parentNodeColumn = viewEntity.AppSearchViewField.FirstOrDefault(o => o.IsUserDefined2.HasValue && o.IsUserDefined2.Value);
            if (treeNodeColumn != null && parentNodeColumn != null)
            {
                int treeNodeColumnId = treeNodeColumn.SearchViewFieldId;
                int parentNodeColumnId = parentNodeColumn.SearchViewFieldId;



                searchResultRows.Where(f => f.DictViewColumnIDKeyValue[parentNodeColumnId] == null).ForAll(o =>
                {
                    rootItems.Add(o);
                });

                foreach (var rootItem in rootItems)
                {
                    ProcessChilds(searchResultRows, rootItem, treeNodeColumnId, parentNodeColumnId);
                }


            }

            return rootItems;
        }

        private static void BuildEShopCardViewResult(SearchDto searchDto, AppSearchViewExDto aAppSearchViewExDto, SearchResultDto searchResultDto)
        {


            AppEshopCatalogViewDto eshopCatalogViewDto = new AppEshopCatalogViewDto();
            Dictionary<int, List<LookupItemDto>> dictOptionLevel = eshopCatalogViewDto.DictOptionLevel = new Dictionary<int, List<LookupItemDto>>();
            Dictionary<int, string> dictOptionDisplay = eshopCatalogViewDto.DictOptionDisplay = new Dictionary<int, string>();

            AppCatalogueForEshopTreeV4BL.BulidEshopCardViewOrDetailViewOptions(searchDto, aAppSearchViewExDto, searchResultDto, dictOptionLevel, dictOptionDisplay, null);
            searchResultDto.EshopCatalogViewDto = eshopCatalogViewDto;

            bool isWithOptionFilter = searchDto.DictFilterOptionLevelAndLookupList != null;

            if (isWithOptionFilter)
            {
                Dictionary<int, List<LookupItemDto>> dictFilterOptionLevelAndLookupList = searchDto.DictFilterOptionLevelAndLookupList;
                AppCatalogueForEshopTreeV4BL.FilterEshopCardViewResultRowsWithLevelOptions(aAppSearchViewExDto, searchResultDto, dictFilterOptionLevelAndLookupList);
            }

            AppCatalogueForEshopTreeV4BL.BuildEShopCardViewResultRows(aAppSearchViewExDto, searchResultDto);
        }

        private static void BuildEShopProductDetailViewResult(AppSearchViewExDto aAppSearchViewExDto, SearchResultDto searchResultDto)
        {
            AppEshopCatalogCardDetailDto aAppEshopCatalogCardDetailDto = new AppEshopCatalogCardDetailDto();

            List<StaticSearchResultRowJsonDto> orgResultRows = searchResultDto.SearchResultRowList.ToList();
            List<StaticSearchResultRowJsonDto> groupedRows = new List<StaticSearchResultRowJsonDto>();




            if (orgResultRows.Count > 0)
            {
                groupedRows.Add(orgResultRows[0]);
            }

            var dictOptionLevelLookup = new Dictionary<int, List<LookupItemDto>>();
            var dictOptionDisplay = new Dictionary<int, string>();
            var dictOptionAndDto = new Dictionary<int, AppEshopCatalogLevelOptionDto>();

            AppCatalogueForEshopTreeV4BL.BulidEshopCardViewOrDetailViewOptions(null, aAppSearchViewExDto, searchResultDto, dictOptionLevelLookup, dictOptionDisplay, dictOptionAndDto);

            aAppEshopCatalogCardDetailDto.DictOptionLevelLookup = dictOptionLevelLookup;
            aAppEshopCatalogCardDetailDto.OrgDictOptionLevelLookup = dictOptionLevelLookup.DeepCopy();
            aAppEshopCatalogCardDetailDto.DictOptionLable = dictOptionDisplay;
            aAppEshopCatalogCardDetailDto.DictOptionAndDto = dictOptionAndDto;
            aAppEshopCatalogCardDetailDto.DictOptionLevelSelectedValue = new Dictionary<int, object>(); ;

            var dictOptionLevelSelectedValue = aAppEshopCatalogCardDetailDto.DictOptionLevelSelectedValue;




            // Image and Display
            // AppSearchViewFieldExDto ImageNode = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartX.HasValue && o.IsMapToChartX.Value).FirstOrDefault();
            List<AppSearchViewFieldExDto> DisplayColumnList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartY.HasValue && o.IsMapToChartY.Value).OrderBy(o => o.Sort).ToList();



            aAppEshopCatalogCardDetailDto.ProductDisplay = new Dictionary<string, List<string>>();

            aAppEshopCatalogCardDetailDto.ImageUrl = new List<string>();

            foreach (AppSearchViewFieldExDto DisplayNode in DisplayColumnList)
            {
                List<string> displayList = new List<string>();
                foreach (var resultRow in orgResultRows)
                {
                    displayList.Add(ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(resultRow.DictViewColumnIDKeyValue[(int)DisplayNode.Id]));
                }

                aAppEshopCatalogCardDetailDto.ProductDisplay.Add(DisplayNode.DisplayText, displayList.Distinct().ToList());
            }



            // matrix key and logical 

            Dictionary<int, List<AppSearchViewFieldExDto>> dictOptionLevelSearchField = AppCatalogueForEshopTreeV4BL.GetDictEshopTreeViewOrCardViewLevelNodeList(aAppSearchViewExDto);
            Dictionary<int, AppSearchViewFieldExDto> dictOptionLevelTreeNodeId = new Dictionary<int, AppSearchViewFieldExDto>();
            Dictionary<int, AppSearchViewFieldExDto> dictOptionLevelTreeNodeDisplay = new Dictionary<int, AppSearchViewFieldExDto>();

            foreach (int levelKey in dictOptionLevelSearchField.Keys)
            {

                AppSearchViewFieldExDto earchViewFieldExDto = dictOptionLevelSearchField[levelKey].FirstOrDefault(o => o.IsTreeNodeId.HasValue && o.IsTreeNodeId.Value);
                dictOptionLevelTreeNodeId.Add(levelKey, earchViewFieldExDto);

                AppSearchViewFieldExDto aAppSearchViewFieldExDto = dictOptionLevelSearchField[levelKey].FirstOrDefault(o => o.IsTreeNodeDisplay.HasValue && o.IsTreeNodeDisplay.Value);
                dictOptionLevelTreeNodeDisplay.Add(levelKey, aAppSearchViewFieldExDto);

            }


            AppSearchViewFieldExDto detailKeyField = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value).FirstOrDefault();
            AppSearchViewFieldExDto skuSearchFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsMapToChartX.HasValue && o.IsMapToChartX.Value).FirstOrDefault();
            AppSearchViewFieldExDto priceSearchFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsFilterByCurrentUser.HasValue && o.IsFilterByCurrentUser.Value).FirstOrDefault();
            AppSearchViewFieldExDto availableQtyFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsUserDefined1.HasValue && o.IsUserDefined1.Value).FirstOrDefault();

            AppSearchViewFieldExDto viewFieldDto_IsSkuHaveGrandChildrenBindingKey = aAppSearchViewExDto.AppSearchViewFieldList.FirstOrDefault(o => o.IsUserDefined4.HasValue && o.IsUserDefined4.Value);
            bool isHaveMultiSelectOption = dictOptionAndDto.Values.FirstOrDefault(o => o.IsMultipleSelection) != null;
            aAppEshopCatalogCardDetailDto.IsProductHaveMultiSelectOption = isHaveMultiSelectOption;
            AppSearchViewFieldExDto selectQtyFieldExDto = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsUserDefined2.HasValue && o.IsUserDefined2.Value).FirstOrDefault();


            List<AppSearchViewFieldExDto> skuDisplayFieldList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsTreeNodeDesc.HasValue && o.IsTreeNodeDesc.Value).ToList();

            List<AppSearchViewFieldExDto> skuImageFiedList = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsTreeNodeImageUrl.HasValue && o.IsTreeNodeImageUrl.Value).ToList();




            Dictionary<string, string> DictMatrixConbineKeySku = new Dictionary<string, string>();
            Dictionary<string, string> DictSkuMatrixDisplay = new Dictionary<string, string>();

            Dictionary<string, decimal> DictSkuPrice = new Dictionary<string, decimal>();
            Dictionary<string, decimal> DictSkuSelectedQty = new Dictionary<string, decimal>();

            Dictionary<string, string> DictSkuDetailId = new Dictionary<string, string>();

            Dictionary<string, Dictionary<string, string>> DictSkuDescription = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> DictSkuImageUrl = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, AppEshopBagItemDto> DictSkuAppEshopBagItem = new Dictionary<string, AppEshopBagItemDto>();
            Dictionary<string, StaticSearchResultRowJsonDto> DictSkuAndResultRowJsonDto = new Dictionary<string, StaticSearchResultRowJsonDto>();




            if (orgResultRows.Count > 0)
            {
                var defaultRow = orgResultRows[0];
                foreach (int levelKey in dictOptionLevelTreeNodeId.Keys)
                {
                    var viewFieldDto = dictOptionLevelTreeNodeId[levelKey];

                    bool isMultiSelectOption = viewFieldDto.IsUserDefined3.HasValue && viewFieldDto.IsUserDefined3.Value;

                    var defaultValueString = defaultRow.DictViewColumnIDKeyValue[(int)viewFieldDto.Id].ToString();

                    if (isMultiSelectOption)
                    {
                        List<string> multiSelectOptionValue = new List<string>() { defaultValueString };
                        dictOptionLevelSelectedValue[levelKey] = multiSelectOptionValue;

                        var foundLookupItemDto = dictOptionLevelLookup[levelKey].FirstOrDefault(o => o.Id.ToString() == defaultValueString);
                        if (foundLookupItemDto != null)
                        {
                            foundLookupItemDto.IsChecked = true;
                        }
                    }
                    else
                    {
                        dictOptionLevelSelectedValue[levelKey] = defaultValueString;
                    }
                }
            }





            foreach (var resultRow in orgResultRows)
            {
                string combineKey = string.Empty;
                foreach (int levelKey in dictOptionLevelTreeNodeId.Keys)
                {
                    combineKey = combineKey + resultRow.DictViewColumnIDKeyValue[(int)dictOptionLevelTreeNodeId[levelKey].Id].ToString() + "_";
                }

                if (combineKey != string.Empty)
                {
                    combineKey = combineKey.Substring(0, combineKey.Length - 1);
                }



                string skuValue = resultRow.DictViewColumnIDKeyValue[(int)skuSearchFieldExDto.Id].ToString();
                string detailID = resultRow.DictViewColumnIDKeyValue[(int)detailKeyField.Id].ToString();

                // fro bagitem display 
                string combineDisplay = string.Empty;

                foreach (int levelKey in dictOptionLevelTreeNodeDisplay.Keys)
                {
                    int viewFieldId = (int)dictOptionLevelTreeNodeDisplay[levelKey].Id;
                    combineDisplay = combineDisplay + resultRow.DictViewColumnIDKeyValue[viewFieldId].ToString() + ",";


                }

                if (combineDisplay != string.Empty)
                {
                    combineDisplay = combineDisplay.Substring(0, combineDisplay.Length - 1);
                    combineDisplay = "(" + combineDisplay + ")";

                }

                DictSkuMatrixDisplay.Add(skuValue, combineDisplay);




                decimal price = ControlTypeValueConverter.ConvertValueToDecimalWithDefautZero(resultRow.DictViewColumnIDKeyValue[(int)priceSearchFieldExDto.Id]);
                decimal selectedQty = 1;

                if (selectQtyFieldExDto != null)
                {
                    aAppEshopCatalogCardDetailDto.IsSelectedQtyReadOnly = true;
                    decimal? qty = ControlTypeValueConverter.ConvertValueToDecimal(resultRow.DictViewColumnIDKeyValue[(int)selectQtyFieldExDto.Id]);

                    if (qty.HasValue && qty.Value > 0)
                    {
                        selectedQty = qty.Value;
                    }
                }


                DictMatrixConbineKeySku.Add(combineKey, skuValue);
                DictSkuPrice.Add(skuValue, price);
                DictSkuSelectedQty.Add(skuValue, selectedQty);
                DictSkuDetailId.Add(skuValue, detailID);

                Dictionary<string, string> dictDisplay = new Dictionary<string, string>();
                foreach (AppSearchViewFieldExDto displayFiled in skuDisplayFieldList)
                {
                    object displayValue = resultRow.DictViewColumnIDKeyValue[(int)displayFiled.Id];
                    dictDisplay.Add(displayFiled.DisplayText, ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(displayValue));

                }

                DictSkuDescription.Add(skuValue, dictDisplay);


                Dictionary<string, string> dictImage = new Dictionary<string, string>();
                foreach (AppSearchViewFieldExDto disimageFiled in skuImageFiedList)
                {
                    object iamgeValue = resultRow.DictViewColumnIDKeyValue[(int)disimageFiled.Id];
                    dictImage.Add(disimageFiled.DisplayText, ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(iamgeValue));

                }



                DictSkuImageUrl.Add(skuValue, dictImage);
                DictSkuAndResultRowJsonDto.Add(skuValue, resultRow);

                searchResultDto.SearchResultRowList = groupedRows;
            }


            aAppEshopCatalogCardDetailDto.DictMatrixKeySku = DictMatrixConbineKeySku;
            aAppEshopCatalogCardDetailDto.DictSkuPrice = DictSkuPrice;
            aAppEshopCatalogCardDetailDto.DictSkuSelectedQty = DictSkuSelectedQty;
            aAppEshopCatalogCardDetailDto.DictSkuDetailId = DictSkuDetailId;
            aAppEshopCatalogCardDetailDto.DictSkuDescription = DictSkuDescription;
            aAppEshopCatalogCardDetailDto.DictSkuImageUrl = DictSkuImageUrl;
            //DictSkuAppEshopBagItem
            foreach (string sku in DictSkuDetailId.Keys)
            {
                AppEshopBagItemDto aAppEshopBagItemDto = new AppEshopBagItemDto();
                aAppEshopBagItemDto.SkuNo = sku;
                aAppEshopBagItemDto.DetailId = DictSkuDetailId[sku];
                aAppEshopBagItemDto.ImageUrl = DictSkuImageUrl[sku].Values.FirstOrDefault();
                aAppEshopBagItemDto.Price = DictSkuPrice[sku];
                aAppEshopBagItemDto.SelectedQuantity = DictSkuSelectedQty[sku];
                aAppEshopBagItemDto.IsSelectedQtyReadOnly = aAppEshopCatalogCardDetailDto.IsSelectedQtyReadOnly;

                List<string> allDisplay = new List<string>();

                foreach (List<string> listdisplay in aAppEshopCatalogCardDetailDto.ProductDisplay.Values)
                {
                    allDisplay.AddRange(listdisplay);
                }



                allDisplay = allDisplay.Select(o => o.Trim()).Distinct().ToList();

                aAppEshopBagItemDto.ProductDisplay = allDisplay.Aggregate((o, next) => o + "," + next);

                aAppEshopBagItemDto.DictMaxtrixDisplay = DictSkuMatrixDisplay[sku];

                aAppEshopBagItemDto.DictSkuDisplay = DictSkuDescription[sku];

                //  aAppEshopBagItemDto.Price = DictSkuPrice[sku];
                //  aAppEshopBagItemDto.Quantity = 0;
                //  aAppEshopBagItemDto.Weight = 0;

                aAppEshopBagItemDto.SkuFieldName = skuSearchFieldExDto.SysTableFiledPath;
                aAppEshopBagItemDto.PriceFieldName = priceSearchFieldExDto.SysTableFiledPath;
                //aAppEshopBagItemDto.SelectedQtyFieldName = selectQtyFieldExDto.SysTableFiledPath;

                aAppEshopBagItemDto.ProductDetaiViewMapUnitId = aAppSearchViewExDto.ProductDetaiViewMapUnitId;
                aAppEshopBagItemDto.TransactionId = aAppSearchViewExDto.TransactionId;

                aAppEshopBagItemDto.SearchViewId = ControlTypeValueConverter.ConvertValueToInt(aAppSearchViewExDto.Id);

                //     aAppEshopBagItemDto.QtyFieldName = 

                var resultRow = DictSkuAndResultRowJsonDto[sku];

                if (isHaveMultiSelectOption && viewFieldDto_IsSkuHaveGrandChildrenBindingKey != null)
                {
                    bool? isSkuHaveGrandChildren = ControlTypeValueConverter.ConvertValueToBoolean(resultRow.DictViewColumnIDKeyValue[(int)viewFieldDto_IsSkuHaveGrandChildrenBindingKey.Id]);
                    aAppEshopBagItemDto.IsSkuHaveGrandChildren = isSkuHaveGrandChildren.HasValue && isSkuHaveGrandChildren.Value;
                }

                aAppEshopBagItemDto.DictOneToOneFields = new Dictionary<string, object>();

                foreach (var column in aAppSearchViewExDto.AppSearchViewFieldList)
                {
                    if (!aAppEshopBagItemDto.DictOneToOneFields.ContainsKey(column.SysTableFiledPath))
                    {
                        aAppEshopBagItemDto.DictOneToOneFields.Add(column.SysTableFiledPath, resultRow.DictViewColumnIDKeyValue[(int)column.Id]);
                    }

                }

                DictSkuAppEshopBagItem.Add(sku, aAppEshopBagItemDto);

            }


            aAppEshopCatalogCardDetailDto.DictSkuAppEshopBagItem = DictSkuAppEshopBagItem;


            aAppEshopCatalogCardDetailDto.ChangedOptionLevel = 1;

            aAppEshopCatalogCardDetailDto = AppCatalogueForEshopTreeV4BL.GetCardDetailOptionCascadingResult(aAppEshopCatalogCardDetailDto);

            groupedRows[0].EshopCatalogCardDetailDto = aAppEshopCatalogCardDetailDto;
        }

        private static StaticSearchResultRowJsonDto[] GetChilds(IEnumerable<StaticSearchResultRowJsonDto> allItems, StaticSearchResultRowJsonDto aItem, int treeNodeColumnId, int parentNodeColumnId)
        {
            return allItems.Where(f => (aItem.DictViewColumnIDKeyValue[treeNodeColumnId] != null)
                && (object.Equals(f.DictViewColumnIDKeyValue[parentNodeColumnId], aItem.DictViewColumnIDKeyValue[treeNodeColumnId])))
                .ToArray();
        }

        internal static void ProcessChilds(IEnumerable<StaticSearchResultRowJsonDto> allItems, StaticSearchResultRowJsonDto aItem, int treeNodeColumnId, int parentNodeColumnId)
        {
            List<StaticSearchResultRowJsonDto> children = new List<StaticSearchResultRowJsonDto>();

            GetChilds(allItems, aItem, treeNodeColumnId, parentNodeColumnId).ForAll(o => children.Add(o));

            if (!children.IsEmpty())
            {
                aItem.Children = children;
                aItem.Children.ForAll(c => ProcessChilds(allItems, c, treeNodeColumnId, parentNodeColumnId));

            }
        }

        private static void BuildSearchViewByType(SearchDto searchDto, ReferenceViewDefinitionDto referenceViewDefinitionDto, AppSearchViewEntity rootViewEntity, SearchResultDto toReturn)
        {
            List<StaticSearchResultRowJsonDto> rootViewSearchResultRows = toReturn.SearchResultRowList.ToList();

            if (rootViewEntity.ViewType == (int)EmAppViewType.HierarchyMasterDetailView)
            {
                PorcessHierarchyMasterDetailView(rootViewEntity, rootViewSearchResultRows);
            }
            else if (rootViewEntity.ViewType == (int)EmAppViewType.FlatDataSetTreeView)
            {
                var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(referenceViewDefinitionDto.Id);
                BuildFlatDataSetTreeViewResult(searchViewDto, toReturn);


            }
            else if (rootViewEntity.ViewType == (int)EmAppViewType.RecursiveDataSetTreeView
                || (rootViewEntity.ViewType == (int)EmAppViewType.ChartView && rootViewEntity.ChartType.HasValue && rootViewEntity.ChartType.Value == (int)EmAppChartViewType.TreeMap))
            {
                toReturn.SearchResultRowList = BuildHierachyTreeViewResult(rootViewEntity, rootViewSearchResultRows);
            }
            else if (rootViewEntity.ViewType == (int)EmAppViewType.EShopCardView)
            {
                var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(referenceViewDefinitionDto.Id);
                BuildEShopCardViewResult(searchDto, searchViewDto, toReturn);
            }
            else if (rootViewEntity.ViewType == (int)EmAppViewType.EShopProductDetailView)
            {
                var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(referenceViewDefinitionDto.Id);
                BuildEShopProductDetailViewResult(searchViewDto, toReturn);
            }


            else if (rootViewEntity.ViewType == (int)EmAppViewType.CalendarView
                                   || rootViewEntity.ViewType == (int)EmAppViewType.GanttView
                                   || rootViewEntity.ViewType == (int)EmAppViewType.SchedulerView)
            {
                toReturn.DcitTransactionIdAndCommandList = new Dictionary<int, List<AppProjectWorkFlowActionExDto>>();

                foreach (var row in rootViewSearchResultRows)
                {
                    if (row.EventTransactionId.HasValue && !toReturn.DcitTransactionIdAndCommandList.ContainsKey(row.EventTransactionId.Value))
                    {
                        var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(row.EventTransactionId.Value);
                        var commandList = transactionExDto.CommandActionList.Where(o => o.ActionAttribute.IsShowOnSearchViewEventOptionMenu.HasValue && o.ActionAttribute.IsShowOnSearchViewEventOptionMenu.Value).ToList();

                        toReturn.DcitTransactionIdAndCommandList.Add(row.EventTransactionId.Value, commandList);
                    }
                }
            }
            else if (rootViewEntity.ViewType == (int)EmAppViewType.DateAndTimeCalendarSelectorView)
            {
                BulidDateAndTimeCalendarSelectorViewResult(referenceViewDefinitionDto, toReturn);
            }
            else if (rootViewEntity.ViewType == (int)EmAppViewType.ClusterAnalysisView)
            {
                PrepareClusterChildViewTransformdResult(referenceViewDefinitionDto, toReturn);

            }
        }



        private static SearchResultDto ExcuteIntegrationApiSearch(SearchDto searchDto, AppSearchViewEntity viewEntity, int? apiOperationId, string rootArrayJsonNodePath, AppDataSetEntity dataSetEntity)
        {
            SearchResultDto searchResultDto = new SearchResultDto();
            List<StaticSearchResultRowJsonDto> searchResultRows = new List<StaticSearchResultRowJsonDto>();
            searchResultDto.SearchResultRowList = searchResultRows;

            if (apiOperationId.HasValue)
            {
                searchResultDto.ApiOperationId = apiOperationId;
                searchResultDto.ApiResponseJsonData = "";

                var apiConfigDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiOperationId.Value);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                Dictionary<string, string> queryParams = new Dictionary<string, string>();
                Dictionary<string, string> pathParams = new Dictionary<string, string>();
                Dictionary<string, string> environmentVairables = new Dictionary<string, string>();

                foreach (SearchCriteriaDto searchCriteriaDto in searchDto.Criterias)
                {
                    if (searchCriteriaDto.Values == null
                       || searchCriteriaDto.Values.Count <= 0
                       || searchCriteriaDto.Values.Cast<object>().All(o => o == null)
                       || (searchCriteriaDto.CriteriaType == EmAppCriteriaType.Boolean
                           && searchCriteriaDto.Values.Cast<object>().All(o => object.Equals(o, -1))))
                    {

                    }
                    else
                    {
                        if (searchCriteriaDto.Values.Count > 0)
                        {
                            string stringValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(searchCriteriaDto.Values.First());

                            if (!string.IsNullOrWhiteSpace(stringValue))
                            {
                                pathParams[searchCriteriaDto.SysTableFiledPath] = stringValue;
                                queryParams[searchCriteriaDto.SysTableFiledPath] = stringValue;
                            }
                        }
                    }
                }


                List<string> lstResponse = AppMasterDetailApiFormDataLoadBL.GetDataFromIntegrationSettingApiCallAsync(apiOperationId.Value, headers, queryParams, pathParams, environmentVairables).Result;


                if (lstResponse.Count > 0)
                {
                    string jsonData = lstResponse[0];

                    searchResultDto.ApiResponseJsonData = jsonData;

                    var jObj = JToken.Parse(jsonData);

                    JToken arrayJtokenArray = null;
                    var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(apiConfigDto.JsonSchema);

                    if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray)
                    {
                        rootNodeDto = rootNodeDto.Children[0];
                        rootNodeDto.Display = "[ ]";
                    }


                    if (!string.IsNullOrWhiteSpace(rootArrayJsonNodePath))
                    {
                        arrayJtokenArray = jObj.SelectToken(rootArrayJsonNodePath);

                        string[] levelPathList = rootArrayJsonNodePath.Split(new string[] { "." }, StringSplitOptions.None);
                        var actualRootNode = AppDataSetBL.FindChildNodeDtoFromPathLevel(rootNodeDto, levelPathList, 0);

                        if (actualRootNode != null)
                        {
                            rootNodeDto = actualRootNode;
                        }
                    }
                    else
                    {
                        arrayJtokenArray = (JToken)jObj;
                    }

                    List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                    Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);
                    Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList = GetViewEntityColumnLookupDict(viewEntity);

                    // default
                    string specificTimeToken = ServerContext.Instance.CurrentUserTimeZoneKey;
                    if (searchDto != null)
                    {
                        SearchCriteriaDto searchCriteriaDto = searchDto.Criterias
                            .FirstOrDefault(o => o.CriteriaSubType.HasValue && o.CriteriaSubType.Value == (int)EmAppSearchFieldSubControlType.TimeZoneTokenInput);
                        if (searchCriteriaDto != null && searchCriteriaDto.Value != null)
                        {
                            specificTimeToken = searchCriteriaDto.Value.ToString();
                        }
                    }

                    if (dataSetEntity.UsageTypeId.HasValue && dataSetEntity.UsageTypeId.Value == (int)EmAppDataSetUsageType.ConvertSimpleObjectToList)
                    {
                        ConvertSimpleJsonObjToSearchResultRows(viewEntity, searchResultRows, arrayJtokenArray, rootNodeDto);
                    }
                    else
                    {
                        if (arrayJtokenArray.Type == JTokenType.Array)
                        {
                            foreach (JToken rowJtoken in arrayJtokenArray.Children())
                            {
                                AddSearchResultRowFromApiJotken(searchResultRows, viewEntity, dictUserIdName, dictColumnIdValuesList, specificTimeToken, rowJtoken, rootNodeDto);
                            }
                        }
                        else if (arrayJtokenArray.Type == JTokenType.Object)
                        {
                            JToken rowJtoken = arrayJtokenArray;
                            AddSearchResultRowFromApiJotken(searchResultRows, viewEntity, dictUserIdName, dictColumnIdValuesList, specificTimeToken, rowJtoken, rootNodeDto);
                        }
                    }


                    List<StaticSearchResultRowJsonDto> allNeedToAddRowList = new List<StaticSearchResultRowJsonDto>();

                    foreach (var baseRowDto in searchResultRows)
                    {
                        List<StaticSearchResultRowJsonDto> aBaseRowMergedRowList = new List<StaticSearchResultRowJsonDto>();
                        aBaseRowMergedRowList.Add(baseRowDto);
                        aBaseRowMergedRowList = ProcessMergedRowsFromBaseRowAndChildRows(baseRowDto, aBaseRowMergedRowList);
                        allNeedToAddRowList.AddRange(aBaseRowMergedRowList);
                    }

                    searchResultRows = allNeedToAddRowList;
                }
            }

            return searchResultDto;
        }

        private static void ConvertSimpleJsonObjToSearchResultRows(AppSearchViewEntity viewEntity, List<StaticSearchResultRowJsonDto> searchResultRows, JToken arrayJtokenArray, ApiDataStructureNodeDto rootNodeDto)
        {
            if (arrayJtokenArray.Type == JTokenType.Object)
            {
                var keyColumn = viewEntity.AppSearchViewField.FirstOrDefault(o => o.SysTableFiledPath == "Key");
                var valueColumn = viewEntity.AppSearchViewField.FirstOrDefault(o => o.SysTableFiledPath == "Value");


                if (keyColumn != null && valueColumn != null)
                {
                    foreach (var childNodeDto in rootNodeDto.Children.Where(o => !o.IsArray && !o.IsObject))
                    {
                        var childNode = arrayJtokenArray.SelectToken(childNodeDto.Name);

                        if (childNode != null && childNode.Type != JTokenType.Array && childNode.Type != JTokenType.Object)
                        {
                            var row = new StaticSearchResultRowJsonDto();

                            string key = childNodeDto.Name;
                            string value = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(childNode);

                            row.DictViewColumnIDKeyValue.Add(keyColumn.SearchViewFieldId, key);
                            row.DictViewColumnIDKeyValue.Add(valueColumn.SearchViewFieldId, value);

                            searchResultRows.Add(row);
                        }
                    }
                }
            }
        }

        private static List<StaticSearchResultRowJsonDto> ProcessMergedRowsFromBaseRowAndChildRows(StaticSearchResultRowJsonDto baseRowDto, List<StaticSearchResultRowJsonDto> mergedRows_from_baseRowAndChildRows)
        {
            if (baseRowDto.DictUdKeyAndChildRowList != null && baseRowDto.DictUdKeyAndChildRowList.Keys.Count > 0)
            {
                foreach (string childKey in baseRowDto.DictUdKeyAndChildRowList.Keys)
                {
                    List<StaticSearchResultRowJsonDto> childBaseRowList = baseRowDto.DictUdKeyAndChildRowList[childKey];

                    if (childBaseRowList.Count > 0)
                    {
                        List<StaticSearchResultRowJsonDto> allChildMergedRowList = new List<StaticSearchResultRowJsonDto>();

                        foreach (var childBaseRow in childBaseRowList)
                        {
                            List<StaticSearchResultRowJsonDto> aChildMergedRowList = new List<StaticSearchResultRowJsonDto>();
                            aChildMergedRowList.Add(childBaseRow);
                            aChildMergedRowList = ProcessMergedRowsFromBaseRowAndChildRows(childBaseRow, aChildMergedRowList);
                            allChildMergedRowList.AddRange(aChildMergedRowList);
                        }

                        List<StaticSearchResultRowJsonDto> newParentMergedRowList = new List<StaticSearchResultRowJsonDto>();

                        foreach (var orgParentRow in mergedRows_from_baseRowAndChildRows)
                        {
                            foreach (var childRow in allChildMergedRowList)
                            {
                                var aNewMergedRow = orgParentRow.DeepCopy();

                                foreach (int columnId in childRow.DictViewColumnIDKeyValue.Keys)
                                {
                                    var cellValue = childRow.DictViewColumnIDKeyValue[columnId];

                                    if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                                    {
                                        aNewMergedRow.DictViewColumnIDKeyValue[columnId] = cellValue;
                                    }
                                }

                                newParentMergedRowList.Add(aNewMergedRow);
                            }
                        }

                        mergedRows_from_baseRowAndChildRows = newParentMergedRowList;
                    }
                }
            }

            return mergedRows_from_baseRowAndChildRows;
        }

        private static void AddSearchResultRowFromApiJotken(List<StaticSearchResultRowJsonDto> searchResultRows, AppSearchViewEntity viewEntity, Dictionary<int, string> dictUserIdName, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, string specificTimeToken, JToken jtoken, ApiDataStructureNodeDto structureNodeDto)
        {
            var row = new StaticSearchResultRowJsonDto();
            ProcessApiSearchResult_ChildNodeValues(viewEntity, jtoken, structureNodeDto, row, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
            searchResultRows.Add(row);

        }

        private static void ProcessApiSearchResult_ChildNodeValues(AppSearchViewEntity viewEntity, JToken jtoken, ApiDataStructureNodeDto structureNodeDto, StaticSearchResultRowJsonDto searchResultRow, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, Dictionary<int, string> dictUserIdName, string specificTimeToken)
        {
            ProcessApiSearchResult_ChildNodeValues_PropertyNodes(viewEntity, jtoken, structureNodeDto, searchResultRow, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
            ProcessApiSearchResult_ChildNodeValues_ObjectNodes(viewEntity, jtoken, structureNodeDto, searchResultRow, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
            ProcessApiSearchResult_ChildNodeValues_ArrayNodes(viewEntity, jtoken, structureNodeDto, searchResultRow, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
        }

        private static void ProcessApiSearchResult_ChildNodeValues_PropertyNodes(AppSearchViewEntity viewEntity, JToken jtoken, ApiDataStructureNodeDto structureNodeDto, StaticSearchResultRowJsonDto searchResultRow, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, Dictionary<int, string> dictUserIdName, string specificTimeToken)
        {
            foreach (var childNodeDto in structureNodeDto.Children.Where(o => !o.IsArray && !o.IsObject))
            {
                InitStructureNodeJsonPath(structureNodeDto, childNodeDto);
                var childJToken = jtoken.SelectToken(childNodeDto.Name);

                if (childJToken == null && childNodeDto.Name.StartsWith("@"))
                {
                    try
                    {
                        childJToken = jtoken[childNodeDto.Name];
                    }
                    catch (Exception ex)
                    { 
                    
                    }
                }

                if (childJToken != null)
                {
                    var viewFieldEntity = viewEntity.AppSearchViewField.FirstOrDefault(o => o.SysTableFiledPath == childNodeDto.NodePath);
                    ProcessApiSearchResult_OneViewFieldValue(viewEntity, dictColumnIdValuesList, childJToken, searchResultRow, childNodeDto, viewFieldEntity, dictUserIdName, specificTimeToken);
                }
            }
        }

        private static void ProcessApiSearchResult_ChildNodeValues_ObjectNodes(AppSearchViewEntity viewEntity, JToken jtoken, ApiDataStructureNodeDto structureNodeDto, StaticSearchResultRowJsonDto searchResultRow, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, Dictionary<int, string> dictUserIdName, string specificTimeToken)
        {
            foreach (var childNodeDto in structureNodeDto.Children.Where(o => o.IsObject))
            {
                InitStructureNodeJsonPath(structureNodeDto, childNodeDto);
                var childJToken = jtoken.SelectToken(childNodeDto.Name);

                if (childJToken != null)
                {
                    if (viewEntity.AppSearchViewField.Where(o => o.SysTableFiledPath.StartsWith(childNodeDto.NodePath)).Count() > 0)
                    {
                        ProcessApiSearchResult_ChildNodeValues(viewEntity, childJToken, childNodeDto, searchResultRow, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
                    }
                }
            }
        }

        private static void ProcessApiSearchResult_ChildNodeValues_ArrayNodes(AppSearchViewEntity viewEntity, JToken jtoken, ApiDataStructureNodeDto structureNodeDto, StaticSearchResultRowJsonDto searchResultRow, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, Dictionary<int, string> dictUserIdName, string specificTimeToken)
        {
            if (searchResultRow.DictUdKeyAndChildRowList == null)
            {
                searchResultRow.DictUdKeyAndChildRowList = new Dictionary<string, List<StaticSearchResultRowJsonDto>>();
            }


            foreach (var childNodeDto in structureNodeDto.Children.Where(o => o.IsArray))
            {
                InitStructureNodeJsonPath(structureNodeDto, childNodeDto);
                var childJToken = jtoken.SelectToken(childNodeDto.Name);

                if (childJToken != null)
                {
                    if (viewEntity.AppSearchViewField.Where(o => o.SysTableFiledPath.StartsWith(childNodeDto.NodePath)).Count() > 0)
                    {
                        ProcessApiSearchResult_ChildArrayNode(viewEntity, childJToken, childNodeDto, searchResultRow, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
                    }
                }
            }
        }


        private static void InitStructureNodeJsonPath(ApiDataStructureNodeDto parentNode, ApiDataStructureNodeDto nodeDto)
        {
            if (!string.IsNullOrWhiteSpace(parentNode.NodePath))
            {
                nodeDto.NodePath = parentNode.NodePath + "." + nodeDto.Name;
            }
            else
            {
                nodeDto.NodePath = nodeDto.Name;
            }
        }

        private static void ProcessApiSearchResult_OneViewFieldValue(AppSearchViewEntity viewEntity, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, JToken jtoken, StaticSearchResultRowJsonDto row, ApiDataStructureNodeDto childNodeDto, AppSearchViewFieldEntity viewFieldEntity, Dictionary<int, string> dictUserIdName, string specificTimeToken)
        {
            if (viewFieldEntity != null)
            {
                int viewFieldId = viewFieldEntity.SearchViewFieldId;

                object value = jtoken;
                object orgValue = value;
                value = ControlTypeValueConverter.ConvertValueToObject(value, viewFieldEntity.ControlType.Value);
                value = PrepareOneSearchResultRowCellValue(viewEntity, dictColumnIdValuesList, viewFieldEntity, value, specificTimeToken);
                row.DictViewColumnIDKeyValue.Add(viewFieldId, value);
                SetGanttViewRowProperties(viewEntity.ViewType, dictUserIdName, row, viewFieldEntity, value, orgValue, specificTimeToken);
            }
        }

        private static void ProcessApiSearchResult_ChildArrayNode(AppSearchViewEntity viewEntity, JToken jToken, ApiDataStructureNodeDto structureNodeDto, StaticSearchResultRowJsonDto searchResultRow, Dictionary<int, Dictionary<string, string>> dictColumnIdValuesList, Dictionary<int, string> dictUserIdName, string specificTimeToken)
        {
            List<StaticSearchResultRowJsonDto> childRowList = new List<StaticSearchResultRowJsonDto>();
            searchResultRow.DictUdKeyAndChildRowList.Add(structureNodeDto.NodePath, childRowList);

            foreach (var childRowJToken in jToken.Children())
            {
                var childRow = new StaticSearchResultRowJsonDto();
                childRowList.Add(childRow);
                ProcessApiSearchResult_ChildNodeValues(viewEntity, childRowJToken, structureNodeDto, childRow, dictColumnIdValuesList, dictUserIdName, specificTimeToken);
            }
        }
    }
}