using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using Newtonsoft.Json;
//using Microsoft.Office.Core;
//using System.Management.Automation;

using APP.Framework;
namespace App.BL
{
    public static class AppSearchViewConfigBL
    {
        public static AppSearchViewEntity RetrieveOneAppSearchViewEntity(object searchViewId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchViewEntity aEntity = new AppSearchViewEntity(int.Parse(searchViewId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchViewEntity);

                rootPath.Add(AppSearchViewEntity.PrefetchPathAppSearchViewField);
                rootPath.Add(AppSearchViewEntity.PrefetchPathAppFormLinkTarget);
                rootPath.Add(AppSearchViewEntity.PrefetchPathAppViewLinkedSeaechOrUrl);
                rootPath.Add(AppSearchViewEntity.PrefetchPathAppDataSet);

                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }





        public static AppSearchViewEntity RetrieveOneAppSearchViewEntityWithEntityCode(string entityCodeInfo)
        {

            EntityCollection<AppSearchViewEntity> userSearchViewEntitylist = new EntityCollection<AppSearchViewEntity>();


            RelationPredicateBucket filter = new RelationPredicateBucket(AppSearchViewFields.EntityInternalCode == entityCodeInfo);

            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchViewEntity);

            rootPath.Add(AppSearchViewEntity.PrefetchPathAppSearchViewField);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppFormLinkTarget);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppDataSet);


            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {


                adpater.FetchEntityCollection(userSearchViewEntitylist, filter, rootPath);

            }

            if (userSearchViewEntitylist.Count > 0)
            {
                return userSearchViewEntitylist[0];
            }
            else
            {
                return null;
            }
        }




        internal static EntityCollection<AppSearchViewEntity> RetrieveUserAllAvaibleSearchViewEntity()
        {
            return AppSearchViewConfigBL.RetrieveAllAppSearchViewEntity();

            //bool isAdmin = AppSecurityUserBL.IsAdminUser();


            //if (isAdmin)
            //{
            //    return AppSearchViewConfigBL.RetrieveAllAppSearchViewEntity();
            //}
            //else //// from APPSecuritySysObjGrour to fiter product
            //{
            //    EntityCollection<AppSearchViewEntity> availableOrgSearchViewlist = new EntityCollection<AppSearchViewEntity>();
            //    EntityCollection<AppSearchViewEntity> restrictUserRoleSearchViewList = new EntityCollection<AppSearchViewEntity>();

            //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            //    {
            //        RelationPredicateBucket filter = new RelationPredicateBucket();
            //        filter.Relations.Add(AppSearchViewEntity.Relations.AppSecuritySysObjGroupUserEntityUsingSearchViewId);

            //        int? currentUserOrganizationId = AppSecurityUserBL.CurrentUserEntity.OrganizationId;
            //        if (currentUserOrganizationId.HasValue)
            //        {
            //            filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.OrganizationId == currentUserOrganizationId.Value);
            //        }

            //        adapter.FetchEntityCollection(availableOrgSearchViewlist, filter, 0, null);
            //    }

            //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            //    {

            //        RelationPredicateBucket filter = new RelationPredicateBucket();
            //        filter.Relations.Add(AppSearchViewEntity.Relations.AppSecuritySysObjGroupUserEntityUsingSearchViewId);
            //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds);
            //        adapter.FetchEntityCollection(restrictUserRoleSearchViewList, filter, 0, null);
            //    }

            //    List<int> restrictSearchIds = restrictUserRoleSearchViewList.Select(o => o.SearchViewId).ToList();
            //    EntityCollection<AppSearchViewEntity> toReturn = new EntityCollection<AppSearchViewEntity>();

            //    foreach (var anEntity in availableOrgSearchViewlist)
            //    {
            //        if (!restrictSearchIds.Contains(anEntity.SearchViewId))
            //        {
            //            toReturn.Add(anEntity);
            //        }
            //    }

            //    return toReturn;
            //}



        }


        public static List<ReferenceViewDefinitionDto> RetrieveUserViewsBySearchDefinition(SearchDefinitionDto searchDefinitionDto)
        {
            List<ReferenceViewDefinitionDto> views = new List<ReferenceViewDefinitionDto>();

            EntityCollection<AppSearchViewEntity> aPdmReferenceViewCollection = RetrieveUserAllAvaibleSearchViewEntity();

            if (searchDefinitionDto.BlqueryId.HasValue)
            {
                List<int> dataSetIds = AppDataSetBL.RetrieveChildDataSetIDListByBaseDataSetId(searchDefinitionDto.BlqueryId.Value);
                dataSetIds.Add(searchDefinitionDto.BlqueryId.Value);

                foreach (var aView in aPdmReferenceViewCollection)
                {
                    if (aView.DataSetId.HasValue && dataSetIds.Contains(aView.DataSetId.Value))
                    {
                        ReferenceViewDefinitionDto aReferenceViewDefinitionDto = ConvertPdmReferenceViewEntityToReferenceViewDefinitionDto(aView);
                        aReferenceViewDefinitionDto.Display = AppLocalizeSystemLableBL.GetSearchViewLabel(aReferenceViewDefinitionDto.Id, aReferenceViewDefinitionDto.Display);
                        views.Add(aReferenceViewDefinitionDto);
                    }
                }
            }




            return views;
        }

        internal static ReferenceViewDefinitionDto ConvertPdmReferenceViewEntityToReferenceViewDefinitionDto(AppSearchViewEntity searchViewEntity)
        {
            ReferenceViewDefinitionDto aReferenceViewDefinitionDto = new ReferenceViewDefinitionDto();
            aReferenceViewDefinitionDto.Id = searchViewEntity.SearchViewId;
            aReferenceViewDefinitionDto.Display = searchViewEntity.Name.ToString();
            aReferenceViewDefinitionDto.Description = searchViewEntity.Description.ToString();
            aReferenceViewDefinitionDto.PivotOrChartSetting = searchViewEntity.PivotOrChartSetting.ToString();

            aReferenceViewDefinitionDto.AppRestResourceUriDisplay = searchViewEntity.AppRestResourceUriDisplay;
            aReferenceViewDefinitionDto.AppRestResourceUri = searchViewEntity.AppRestResourceUri;

            aReferenceViewDefinitionDto.ViewType = searchViewEntity.ViewType;
            aReferenceViewDefinitionDto.ChartType = searchViewEntity.ChartType;
            aReferenceViewDefinitionDto.IsMassUpdate = false;
            aReferenceViewDefinitionDto.BlqueryId = searchViewEntity.DataSetId;
            aReferenceViewDefinitionDto.IsMassUpdate = searchViewEntity.IsMassUpdateView.HasValue && searchViewEntity.IsMassUpdateView.Value;
            aReferenceViewDefinitionDto.MassUpdateTransactionId = searchViewEntity.UpdateTransctionId;

            aReferenceViewDefinitionDto.IsAllowAddRow = searchViewEntity.IsAllowAddRow.HasValue && searchViewEntity.IsAllowAddRow.Value;
            aReferenceViewDefinitionDto.IsAllowDeleteRow = searchViewEntity.IsAllowDeleteRow.HasValue && searchViewEntity.IsAllowDeleteRow.Value;
            aReferenceViewDefinitionDto.IsAllowAdvancedUpdate = searchViewEntity.IsAllowUpdateRow.HasValue && searchViewEntity.IsAllowUpdateRow.Value;

            if (searchViewEntity.UpdateTransctionId.HasValue)
            {
                //var transcationDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppSearchViewEntity.UpdateTransctionId);

                AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(searchViewEntity.UpdateTransctionId);
                if (aAppTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                {
                    aReferenceViewDefinitionDto.MassUpdateViewType = (int)EmAppMassUpdateViewType.HierarchicalTableUpdate;

                }
                else
                {
                    aReferenceViewDefinitionDto.MassUpdateViewType = (int)EmAppMassUpdateViewType.SingleTableUpdate;

                }


            }





            return aReferenceViewDefinitionDto;
        }


        internal static ReferenceViewDto ConvertReverenceViewEntityToReferenceViewDto(AppSearchViewEntity searchViewEntity)
        {
            var viewDto = new ReferenceViewDto();

            viewDto.Id = searchViewEntity.SearchViewId;
            viewDto.Display = searchViewEntity.Name.ToString();
            viewDto.Description = searchViewEntity.Description;

            viewDto.AppRestResourceUriDisplay = searchViewEntity.AppRestResourceUriDisplay;
            viewDto.AppRestResourceUri = searchViewEntity.AppRestResourceUri;

            viewDto.ViewType = searchViewEntity.ViewType;
            viewDto.ChartType = searchViewEntity.ChartType;
            // for  static view,need to setup BL query data set
            viewDto.BlqueryId = searchViewEntity.DataSetId;
            viewDto.IsMassUpdate = searchViewEntity.IsMassUpdateView.HasValue && searchViewEntity.IsMassUpdateView.Value;
            viewDto.MassUpdateTransactionId = searchViewEntity.UpdateTransctionId;

            viewDto.CanlendarDefaultViewMode = searchViewEntity.CanlendarDefaultViewMode;
            viewDto.IsEnableCalendarMonthView = searchViewEntity.IsEnableCalendarMonthView.HasValue && searchViewEntity.IsEnableCalendarMonthView.Value;
            viewDto.IsEnableCalendarWeekView = searchViewEntity.IsEnableCalendarWeekView.HasValue && searchViewEntity.IsEnableCalendarWeekView.Value;
            viewDto.IsEnableCalendarDayView = searchViewEntity.IsEnableCalendarDayView.HasValue && searchViewEntity.IsEnableCalendarDayView.Value;
            viewDto.IsEnableCalendarNavigator = searchViewEntity.IsEnableCalendarNavigator.HasValue && searchViewEntity.IsEnableCalendarNavigator.Value;
            //viewDto.IsDisableClientTimeConvert = searchViewEntity.IsDisableClientTimeConvert;
            viewDto.GridOutputMode = searchViewEntity.GridOutputMode;

            viewDto.IsMassUpdate = searchViewEntity.IsMassUpdateView.HasValue && searchViewEntity.IsMassUpdateView.Value;
            viewDto.Options = searchViewEntity.Options;
            viewDto.CollapseGroupsToLevel = searchViewEntity.RowPerPage;

            if (!string.IsNullOrWhiteSpace(searchViewEntity.PivotOrChartSetting))
            {
                try
                {
                    viewDto.OtherSettingsDto = JsonConvert.DeserializeObject<AppSearchViewOtherSettingsDto>(searchViewEntity.PivotOrChartSetting);
                }
                catch
                {
                    viewDto.OtherSettingsDto = new AppSearchViewOtherSettingsDto();
                }
            }
            else
            {
                viewDto.OtherSettingsDto = new AppSearchViewOtherSettingsDto();
            }

            if (searchViewEntity.UpdateTransctionId.HasValue)
            {
                //var transcationDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppSearchViewEntity.UpdateTransctionId);

                AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(searchViewEntity.UpdateTransctionId);
                if (aAppTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                {
                    viewDto.MassUpdateViewType = (int)EmAppMassUpdateViewType.HierarchicalTableUpdate;

                }
                else
                {
                    viewDto.MassUpdateViewType = (int)EmAppMassUpdateViewType.SingleTableUpdate;

                }


            }


            viewDto.FreezeColumnCount = searchViewEntity.NbFrozenColumn;
            viewDto.PivotOrChartSetting = searchViewEntity.PivotOrChartSetting;
            //PivotOrChartSetting
            viewDto.WhereUsedDefaultViewId = searchViewEntity.WhereUsedDefaultViewId;
            viewDto.HierachyParentViewId = searchViewEntity.HierachyParentViewId;
            ConvertReferenceViewColumn(searchViewEntity, viewDto);

            ConvertReferenceViewLinkTarget(searchViewEntity, viewDto);

            ConvertReferenceViewLinkedSearch(searchViewEntity, viewDto);

            //  view.ReportPrintDtoList = PdmTemplateReportPrintBL.RetrieveReferenceViewPrintDto((int)view.Id);

            //   public static ObservableSet<PdmTemplateReportPrintDto> RetrieveAllPdmTemplateReportPrintForOneTemplateDto(int referenceViewId)


            var folderIdColumn = viewDto.Columns.FirstOrDefault(o => o.IsFileFoderId.HasValue && o.IsFileFoderId.Value);
            var transRootIdColumn = viewDto.Columns.FirstOrDefault(o => o.IsTransRootId.HasValue && o.IsTransRootId.Value);
            var fileCodeColumn = viewDto.Columns.FirstOrDefault(o => o.SysTableFiledPath.ToLower() == "filecode");

            if (folderIdColumn != null)
            {
                viewDto.FolderIdColumnId = ControlTypeValueConverter.ConvertValueToInt(folderIdColumn.Id);
            }

            if (transRootIdColumn != null)
            {
                viewDto.TransRootIdColumnId = ControlTypeValueConverter.ConvertValueToInt(transRootIdColumn.Id);
            }

            if (fileCodeColumn != null)
            {
                viewDto.FileCodeColumnId = ControlTypeValueConverter.ConvertValueToInt(fileCodeColumn.Id);
            }

            if (searchViewEntity.ViewType == (int)EmAppViewType.PivotView)
            {
                viewDto.PivotRows = new ObservableCollection<ReferenceViewColumnDto>(viewDto.Columns.Where(o => o.IsPivotRowField.HasValue && o.IsPivotRowField.Value).OrderBy(o => o.Sort).ToList());
                viewDto.PivotColumns = new ObservableCollection<ReferenceViewColumnDto>(viewDto.Columns.Where(o => o.IsPivotColumnField.HasValue && o.IsPivotColumnField.Value).OrderBy(o => o.Sort).ToList());
                viewDto.PivotAggregationFields = new ObservableCollection<ReferenceViewColumnDto>(viewDto.Columns.Where(o => o.IsPivotAggregationField.HasValue && o.IsPivotAggregationField.Value).OrderBy(o => o.Sort).ToList());
                viewDto.PivotDefaultAggregation = searchViewEntity.ChartType;
            }
            else if (searchViewEntity.ViewType == (int)EmAppViewType.SchedulerView)
            {
                //var groupByColumn = viewDto.Columns.FirstOrDefault(o=>o.IsGroupBy.HasValue && o.IsGroupBy.Value && o.EntityId.HasValue);
                //if (groupByColumn != null)
                //{
                //    viewDto.SchedulerViewGroupByResources = AppEntityInfoBL.GetLookupItemList(groupByColumn.EntityId.Value, string.Empty);
                //}              
            }

            if (searchViewEntity.ViewType == (int)EmAppViewType.HierarchyMasterDetailView)
            {
                var childViewEntityList = AppSearchViewConfigBL.RetrieveHierarchyMasterDetailViewChildViewEntityList(searchViewEntity.SearchViewId);

                viewDto.ChildReferenceViewDtoList = new List<ReferenceViewDto>();

                foreach (var childviewEntity in childViewEntityList)
                {
                    viewDto.ChildReferenceViewDtoList.Add(AppSearchViewConfigBL.ConvertReverenceViewEntityToReferenceViewDto(childviewEntity));
                }
            }



            return viewDto;
        }

        private static void ConvertReferenceViewLinkTarget(AppSearchViewEntity referenceViewEntity, ReferenceViewDto view)
        {
            view.AppFormLinkTargetList = new List<AppFormLinkTargetDto>();

            foreach (var viewTargetEntity in referenceViewEntity.AppFormLinkTarget)
            {
                view.AppFormLinkTargetList.Add(AppFormLinkTargetConverter.ConvertEntityToDto(viewTargetEntity));
            }
        }

        private static void ConvertReferenceViewLinkedSearch(AppSearchViewEntity referenceViewEntity, ReferenceViewDto view)
        {
            view.AppViewLinkedSeaechOrUrlDtoList = new List<AppViewLinkedSeaechOrUrlDto>();

            //bool isBuiltInLinkedSearch = false;

            if (referenceViewEntity.ViewType == (int)EmAppViewType.FlatDataSetTreeView)
            {
                AppSearchViewExDto appSearchViewExDto = AppSearchViewConverter.ConvertEntityToExDto(referenceViewEntity);

                if (appSearchViewExDto.OtherSettingsDto != null && appSearchViewExDto.OtherSettingsDto.EshopCategorySearchMapping != null)
                {
                    //isBuiltInLinkedSearch = true;
                    var linkedSearchDto = appSearchViewExDto.OtherSettingsDto.EshopCategorySearchMapping;
                    linkedSearchDto.Id = linkedSearchDto.UiId;
                    view.AppViewLinkedSeaechOrUrlDtoList.Add(linkedSearchDto);
                }
            }


            foreach (var entity in referenceViewEntity.AppViewLinkedSeaechOrUrl)
            {
                view.AppViewLinkedSeaechOrUrlDtoList.Add(AppViewLinkedSeaechOrUrlConverter.ConvertEntityToDto(entity));
            }

        }

        private static void ConvertReferenceViewColumn(AppSearchViewEntity referenceViewEntity, ReferenceViewDto view)
        {
            view.Columns = new List<ReferenceViewColumnDto>();



            var allEntitIds = referenceViewEntity.AppSearchViewField.Where(o => o.EntityId.HasValue).Select(o => o.EntityId.Value).Distinct().ToList();

            Dictionary<int, List<LookupItemDto>> dictEntityIdValuesList = new Dictionary<int, List<LookupItemDto>>();

            foreach (int entityId in allEntitIds)
            {
                dictEntityIdValuesList.Add(entityId, AppEntityInfoBL.GetLookupItemList(entityId, ""));
            }

            view.DictEntityLookupItemDto = dictEntityIdValuesList;


            foreach (var aBColumn in referenceViewEntity.AppSearchViewField)
            {
                ReferenceViewColumnDto aColumnDto = ConvertViewEntityToViewDto(view, aBColumn);

                view.Columns.Add(aColumnDto);
            }

            view.GroupByFieldList = referenceViewEntity.AppSearchViewField.Where(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value && o.GroupByLevel.HasValue).OrderBy(o => o.GroupByLevel.Value).Select(o => o.SearchViewFieldId.ToString()).ToList();


            if (referenceViewEntity.ViewType == (int)EmAppViewType.ClusterAnalysisView)
            {
                ConvertReferenceViewColumn_PrepareClusterAnalysisView(referenceViewEntity, view);
            }
        }

        private static void ConvertReferenceViewColumn_PrepareClusterAnalysisView(AppSearchViewEntity referenceViewEntity, ReferenceViewDto view)
        {
            if (view.OtherSettingsDto != null && view.OtherSettingsDto.ClusterChildViewItemList != null)
            {
                foreach (var clusterChildViewItem in view.OtherSettingsDto.ClusterChildViewItemList)
                {
                    clusterChildViewItem.MasterClusterViewId = referenceViewEntity.SearchViewId;
                    clusterChildViewItem.ClusterViewItemColumns = new List<ReferenceViewColumnDto>();

                    if (clusterChildViewItem.AppSearchViewFieldList == null)
                    {
                        clusterChildViewItem.AppSearchViewFieldList = new List<AppSearchViewFieldExDto>();
                    }

                    foreach (var searchViewFieldDto in clusterChildViewItem.AppSearchViewFieldList)
                    {
                        AppSearchViewFieldEntity fieldEntity = referenceViewEntity.AppSearchViewField.FirstOrDefault(o => o.SysTableFiledPath == searchViewFieldDto.SysTableFiledPath && o.ControlType == searchViewFieldDto.ControlType);

                        if (fieldEntity != null)
                        {
                            ReferenceViewColumnDto aColumnDto = ConvertViewEntityToViewDto(view, fieldEntity);
                            aColumnDto.Sort = searchViewFieldDto.Sort;
                            aColumnDto.AggregationFunctionType = searchViewFieldDto.AggregationFunctionType;

                            if (clusterChildViewItem.ViewType.HasValue && clusterChildViewItem.ViewType.Value == (int)EmAppViewType.ChartView)
                            {
                                aColumnDto.IsMapToChartX = searchViewFieldDto.IsMapToChartX;
                                aColumnDto.IsMapToChartY = searchViewFieldDto.IsMapToChartY;
                            }
                            else if (clusterChildViewItem.ViewType.HasValue && clusterChildViewItem.ViewType.Value == (int)EmAppViewType.PivotView)
                            {
                                aColumnDto.IsPivotRowField = searchViewFieldDto.IsUserDefined1;
                                aColumnDto.IsPivotColumnField = searchViewFieldDto.IsUserDefined2;
                                aColumnDto.IsPivotAggregationField = searchViewFieldDto.IsUserDefined3;


                            }




                            clusterChildViewItem.ClusterViewItemColumns.Add(aColumnDto);
                        }
                    }

                    AppDesktopItemExDto layoutItemWidget = FindLayoutWidgetFromClusterAyalisysView(view.OtherSettingsDto.FlexLayoutItems, clusterChildViewItem.UiId);

                    if (layoutItemWidget != null)
                    {
                        layoutItemWidget.MasterClusterViewId = clusterChildViewItem.MasterClusterViewId;
                        layoutItemWidget.ClusterViewItemColumns = clusterChildViewItem.ClusterViewItemColumns;
                    }
                }
            }
        }

        private static AppDesktopItemExDto FindLayoutWidgetFromClusterAyalisysView(List<AppDesktopItemExDto> flexLayoutItems, string uiId)
        {
            if (flexLayoutItems != null)
            {
                foreach (var layoutItem in flexLayoutItems)
                {
                    var foundItem = FindLayoutWidgetFromClusterAyalisysView_ProcessChildLayoutItem(layoutItem, uiId);
                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
            }

            return null;
        }

        private static AppDesktopItemExDto FindLayoutWidgetFromClusterAyalisysView_ProcessChildLayoutItem(AppDesktopItemExDto layoutItem, string uiId)
        {
            if (layoutItem.ChildDesktopItems != null && layoutItem.ChildDesktopItems.Count > 0)
            {
                foreach (AppDesktopItemExDto childLayoutItem in layoutItem.ChildDesktopItems)
                {
                    var foundItem = FindLayoutWidgetFromClusterAyalisysView_ProcessChildLayoutItem(childLayoutItem, uiId);
                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
            }
            else
            {
                if (layoutItem.DesktopWidget != null && layoutItem.DesktopWidget.WidgetItemType.HasValue
                    && layoutItem.DesktopWidget.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.ClusterAnalysisViewItem)
                {
                    if (uiId == layoutItem.DesktopWidget.UiId)
                    {
                        return layoutItem.DesktopWidget;
                    }

                }
            }

            return null;
        }

        private static ReferenceViewColumnDto ConvertViewEntityToViewDto(ReferenceViewDto view, AppSearchViewFieldEntity aBColumn)
        {


            ReferenceViewColumnDto aColumnDto = new ReferenceViewColumnDto();

            aColumnDto.IsUpdatable = view.IsMassUpdate && aBColumn.MassUpdateTransactionFieldId.HasValue;

            // default type;
            aColumnDto.ColumnType = EmAppViewColumnType.Data;
            aColumnDto.DataType = typeof(string).FullName;
            aColumnDto.EntityId = aBColumn.EntityId;
            aColumnDto.IsCalulationField = aBColumn.IsCalulationField;
            aColumnDto.IsMapToChartX = aBColumn.IsMapToChartX;
            aColumnDto.IsMapToChartY = aBColumn.IsMapToChartY;
            aColumnDto.ChartYmappingOrder = aBColumn.ChartYmappingOrder;
            aColumnDto.Width = aBColumn.Width;

            aColumnDto.TreeLevel = aBColumn.TreeLevel;
            aColumnDto.IsTreeNodeId = aBColumn.IsTreeNodeId.HasValue && aBColumn.IsTreeNodeId.Value;
            aColumnDto.IsTreeNodeDisplay = aBColumn.IsTreeNodeDisplay.HasValue && aBColumn.IsTreeNodeDisplay.Value;

            aColumnDto.IsVisible = aBColumn.IsVisible;
            aColumnDto.Sort = aBColumn.Sort;

            aColumnDto.RowNumber = aBColumn.RowNumber;
            aColumnDto.ColumnNumber = aBColumn.ColumnNumber;

            aColumnDto.IsGroupBy = aBColumn.IsGroupBy;
            aColumnDto.GroupByLevel = aBColumn.GroupByLevel;
            aColumnDto.AggregationFunctionType = aBColumn.AggregationFunctionType;
            aColumnDto.ControlType = (int)EmAppControlType.TextBox;
            if (aBColumn.ControlType.HasValue)
            {
                aColumnDto.ControlType = aBColumn.ControlType.Value;
            }

            aColumnDto.IsTransRootId = aBColumn.IsTransRootId;
            aColumnDto.IsFileFoderId = aBColumn.IsFileFoderId;


            aColumnDto.Id = aBColumn.SearchViewFieldId;



            aColumnDto.Name = aBColumn.SysTableFiledPath;

            aColumnDto.SysTableFiledPath = aBColumn.SysTableFiledPath;
            aColumnDto.MappingSearchFieldId = aBColumn.MappingSearchFieldId;


            SetupViewColumnControlTypeAndDataType(aColumnDto, aColumnDto.ControlType, aColumnDto.EntityId, view.ViewType);

            if (aColumnDto.ControlType == (int)EmAppViewColumnType.DDL && !view.IsMassUpdate)
            {
                aColumnDto.ColumnType = EmAppViewColumnType.Data;
                aColumnDto.DataType = typeof(string).FullName;
                aColumnDto.ControlType = (int)EmAppControlType.TextBox;
            }



            if (!string.IsNullOrEmpty(aBColumn.DisplayText))
            {
                aColumnDto.Name = aBColumn.DisplayText.ToString();
            }

            aColumnDto.DisplayName = AppLocalizeSystemLableBL.GetSearchViewFieldLabel(aColumnDto.Id, aColumnDto.Name);

            if (view.ViewType == (int)EmAppViewType.PivotView)
            {
                aColumnDto.IsPivotRowField = aBColumn.IsUserDefined1;
                aColumnDto.IsPivotColumnField = aBColumn.IsUserDefined2;
                aColumnDto.IsPivotAggregationField = aBColumn.IsUserDefined3;

                aColumnDto.AggregationFunctionType = aBColumn.AggregationFunctionType;
            }


            aColumnDto.IsUserDefined1 = aBColumn.IsUserDefined1.HasValue && aBColumn.IsUserDefined1.Value;
            aColumnDto.IsUserDefined2 = aBColumn.IsUserDefined2.HasValue && aBColumn.IsUserDefined2.Value;
            aColumnDto.IsUserDefined3 = aBColumn.IsUserDefined3.HasValue && aBColumn.IsUserDefined3.Value;
            aColumnDto.IsUserDefined4 = aBColumn.IsUserDefined4.HasValue && aBColumn.IsUserDefined4.Value;

            if (view.ViewType == (int)EmAppViewType.EShopCardView || view.ViewType == (int)EmAppViewType.EShopProductDetailView)
            {
                aColumnDto.IsEshopPriceColumn = aBColumn.IsFilterByCurrentUser.HasValue && aBColumn.IsFilterByCurrentUser.Value;
            }


            aColumnDto.EmInternalCodeRegistration = aBColumn.EmInternalCodeRegistration;


            return aColumnDto;
        }




        internal static void SetupViewColumnControlTypeAndDataType(ReferenceViewColumnDto aDto, int controlType, int? aEntityId, int viewType)
        {
            aDto.ControlType = controlType;
            if (controlType == (int)EmAppControlType.Memo

               || controlType == (int)EmAppControlType.TextBox

               || controlType == (int)EmAppControlType.AutoGeneration

                )
            {
                aDto.ColumnType = EmAppViewColumnType.Data;
                aDto.DataType = typeof(string).FullName;
            }

            else if (controlType == (int)EmAppControlType.DDL || controlType == (int)EmAppControlType.AutoComplete || controlType == (int)EmAppControlType.SearchAbleDDL)
            {
                aDto.ColumnType = EmAppViewColumnType.DDL;
                aDto.DataType = typeof(int).FullName;

                aDto.EntityId = aEntityId;
            }

            else if (controlType == (int)EmAppControlType.CheckBox)
            {
                aDto.ColumnType = EmAppViewColumnType.Data;
                aDto.DataType = typeof(bool?).FullName;
            }
            else if (controlType == (int)EmAppControlType.Date)
            {
                aDto.ColumnType = EmAppViewColumnType.Data;
                aDto.DataType = typeof(DateTime).FullName;
            }
            else if (controlType == (int)EmAppControlType.Numeric

                )
            {
                aDto.ColumnType = EmAppViewColumnType.Data;
                aDto.DataType = typeof(double).FullName;
            }

            else if (controlType == (int)EmAppControlType.Image)
            {
                aDto.ColumnType = EmAppViewColumnType.Image;
                aDto.DataType = typeof(string).FullName;
            }
        }



        internal static List<AppSearchViewExDto> RetrievAllTreeViewEntityList()
        {

            List<AppSearchViewExDto> toReturnList = new List<AppSearchViewExDto>();
            EntityCollection<AppSearchViewEntity> userSearchViewEntitylist = new EntityCollection<AppSearchViewEntity>();
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchViewEntity);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppSearchViewField);
            RelationPredicateBucket filter = new RelationPredicateBucket(AppSearchViewFields.ViewType == (int)EmAppViewType.FlatDataSetTreeView);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(userSearchViewEntitylist, filter, rootPath);
            }

            foreach (var entityView in userSearchViewEntitylist)
            {

                toReturnList.Add(ConvertOneEntityToExDto(entityView));

            }



            return toReturnList;
        }



        public static AppSearchViewExDto RetrieveOneAppSearchViewExDto(object SearchViewId)
        {
            AppSearchViewEntity aAppSearchViewEntity = RetrieveOneAppSearchViewEntity(SearchViewId);

            AppSearchViewExDto aAppSearchViewDto = ConvertOneEntityToExDto(aAppSearchViewEntity);

            if (aAppSearchViewDto.ViewType == (int)EmAppViewType.FlatDataSetTreeView
                && aAppSearchViewDto.OtherSettingsDto != null
                && aAppSearchViewDto.OtherSettingsDto.EshopCategorySearchMapping != null
                && aAppSearchViewDto.OtherSettingsDto.EshopCategorySearchMapping.LinkTargetSearchId.HasValue)
            {
                aAppSearchViewDto.EshopCardViewSearchId = aAppSearchViewDto.OtherSettingsDto.EshopCategorySearchMapping.LinkTargetSearchId.Value;
            }


            if (aAppSearchViewDto.ViewType == (int)EmAppViewType.HierarchyMasterDetailView)
            {
                aAppSearchViewDto.ChildViewExDtoList = RetrieveHierarchyMasterDetailViewChildViewExDtoList(SearchViewId);
            }

            return aAppSearchViewDto;
        }

        internal static List<AppSearchViewExDto> RetrieveHierarchyMasterDetailViewChildViewExDtoList(object masterSearchViewId)
        {
            List<AppSearchViewExDto> childSearchViewExDtoList = new List<AppSearchViewExDto>();

            EntityCollection<AppSearchViewEntity> userSearchViewEntitylist = RetrieveHierarchyMasterDetailViewChildViewEntityList(masterSearchViewId);

            foreach (var entityView in userSearchViewEntitylist)
            {
                childSearchViewExDtoList.Add(ConvertOneEntityToExDto(entityView));
            }



            return childSearchViewExDtoList;
        }



        internal static EntityCollection<AppSearchViewEntity> RetrieveHierarchyMasterDetailViewChildViewEntityList(object masterSearchViewId)
        {
            EntityCollection<AppSearchViewEntity> userSearchViewEntitylist = new EntityCollection<AppSearchViewEntity>();
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchViewEntity);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppSearchViewField);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppFormLinkTarget);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppViewLinkedSeaechOrUrl);
            rootPath.Add(AppSearchViewEntity.PrefetchPathAppDataSet);

            RelationPredicateBucket filter = new RelationPredicateBucket(AppSearchViewFields.HierachyParentViewId == (int)masterSearchViewId);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(userSearchViewEntitylist, filter, rootPath);
            }

            return userSearchViewEntitylist;
        }

        internal static AppSearchViewExDto ConvertOneEntityToExDto(AppSearchViewEntity aAppSearchViewEntity)
        {
            AppSearchViewExDto aAppSearchViewDto = AppSearchViewConverter.ConvertEntityToExDto(aAppSearchViewEntity);


            foreach (var o in aAppSearchViewEntity.AppSearchViewField)
            {
                AppSearchViewFieldExDto aAppSearchViewFieldExDto = AppSearchViewFieldConverter.ConvertEntityToExDto(o);

                PrepareOneSearchViewFieldWebSiteDesignExpression(aAppSearchViewFieldExDto);

                aAppSearchViewDto.AppSearchViewFieldList.Add(aAppSearchViewFieldExDto);
            }

            foreach (var o in aAppSearchViewEntity.AppViewLinkedSeaechOrUrl.OrderBy(o => o.Sort))
            {
                AppViewLinkedSeaechOrUrlExDto dto = AppViewLinkedSeaechOrUrlConverter.ConvertEntityToExDto(o);
                aAppSearchViewDto.AppViewLinkedSeaechOrUrlList.Add(dto);
            }

            foreach (var o in aAppSearchViewEntity.AppFormLinkTarget.OrderBy(o => o.Sort))
            {
                AppFormLinkTargetExDto dto = AppFormLinkTargetConverter.ConvertEntityToExDto(o);
                aAppSearchViewDto.AppFormLinkTargetList.Add(dto);
            }


            if (aAppSearchViewDto.ViewType == (int)EmAppViewType.FlatDataSetTreeView)
            {
                InitEshopLinkedToSearchIds(aAppSearchViewDto);

            }

            return aAppSearchViewDto;
        }

        private static void InitEshopLinkedToSearchIds(AppSearchViewExDto aAppSearchViewDto)
        {
            var defaultLinkedSearch = aAppSearchViewDto.AppViewLinkedSeaechOrUrlList.FirstOrDefault(o => o.LinkTargetSearchId.HasValue);
            if (defaultLinkedSearch != null)
            {
                aAppSearchViewDto.EshopCardViewSearchId = defaultLinkedSearch.LinkTargetSearchId.Value;

                var linkedToSearchExDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(defaultLinkedSearch.LinkTargetSearchId.Value);

                if (linkedToSearchExDto.SearchViewId.HasValue)
                {
                    var linkedToSearchViewExDto = RetrieveOneAppSearchViewExDto(linkedToSearchExDto.SearchViewId);

                    if (linkedToSearchViewExDto.ViewType == (int)EmAppViewType.EShopCardView)
                    {
                        var productDetailSearchExDto = linkedToSearchViewExDto.AppViewLinkedSeaechOrUrlList.FirstOrDefault(o => o.LinkTargetSearchId.HasValue);
                        if (productDetailSearchExDto != null)
                        {
                            aAppSearchViewDto.EshopProductDetailViewSearchId = productDetailSearchExDto.LinkTargetSearchId.Value;
                        }
                    }
                }

            }
        }

        private static void PrepareOneSearchViewFieldWebSiteDesignExpression(AppSearchViewFieldExDto aAppSearchViewFieldExDto)
        {
            aAppSearchViewFieldExDto.Expression = new Dictionary<string, string>();

            aAppSearchViewFieldExDto.Expression[EmAppSearchViewFieldExpressionType.ViewFieldId.ToString()] = aAppSearchViewFieldExDto.Id.ToString();
            aAppSearchViewFieldExDto.Expression[EmAppSearchViewFieldExpressionType.SearchViewId.ToString()] = aAppSearchViewFieldExDto.SearchViewId.ToString();

            if (!string.IsNullOrEmpty(aAppSearchViewFieldExDto.DisplayText))
            {
                aAppSearchViewFieldExDto.Expression[EmAppSearchViewFieldExpressionType.Name.ToString()] = aAppSearchViewFieldExDto.DisplayText;
            }
            else
            {
                aAppSearchViewFieldExDto.Expression[EmAppSearchViewFieldExpressionType.Name.ToString()] = aAppSearchViewFieldExDto.SysTableFiledPath;
            }

            aAppSearchViewFieldExDto.Expression[EmAppSearchViewFieldExpressionType.ValueBinding.ToString()] = "aResultItem.DictViewColumnIDKeyValue['" + aAppSearchViewFieldExDto.Id.ToString() + "']";

            if (aAppSearchViewFieldExDto.EntityId.HasValue)
            {
                aAppSearchViewFieldExDto.Expression[EmAppSearchViewFieldExpressionType.EntityId.ToString()] = aAppSearchViewFieldExDto.EntityId.Value.ToString();
            }

        }

        public static ObservableSet<AppSearchViewDto> RetrieveAllAppSearchViewDto()
        {
            EntityCollection<AppSearchViewEntity> list = RetrieveAllAppSearchViewEntity();

            var aDtoList = new ObservableSet<AppSearchViewDto>();

            foreach (var o in list)
            {
                aDtoList.Add(AppSearchViewConverter.ConvertEntityToDto(o));
            }

            return aDtoList;

        }


        public static ObservableSet<AppSearchViewDto> RetrieveAllSearchViewDtoByViewType(int? viewType = null)
        {
            EntityCollection<AppSearchViewEntity> list = new EntityCollection<AppSearchViewEntity>();

            IRelationPredicateBucket filter = null;

            if (viewType.HasValue)
            {
                filter = new RelationPredicateBucket(AppSearchViewFields.ViewType == viewType.Value);
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, filter);
            }


            var aDtoList = new ObservableSet<AppSearchViewDto>();

            foreach (var o in list)
            {
                aDtoList.Add(AppSearchViewConverter.ConvertEntityToDto(o));
            }

            return aDtoList;

        }

        public static EntityCollection<AppSearchViewEntity> RetrieveAllAppSearchViewEntity()
        {
            EntityCollection<AppSearchViewEntity> list = new EntityCollection<AppSearchViewEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, null);
            }
            return list;
        }


        public static OperationCallResult<AppSearchViewExDto> SaveAsSearchView(object searchViewId)
        {

            var viewExdto = RetrieveOneAppSearchViewExDto(searchViewId);

            viewExdto.Id = null;
            viewExdto.Name = "Copy From " + viewExdto.Name;

            return SaveAppSearchViewExDto(viewExdto);




        }

        public static OperationCallResult<AppSearchViewExDto> SaveAppSearchViewExDto(AppSearchViewExDto aAppSearchViewExDto)
        {
            OperationCallResult<AppSearchViewExDto> aOperationCallResult = new OperationCallResult<AppSearchViewExDto>();

            PrepaireDayPilotTypeSearchViewFieldsBeforeSave(aAppSearchViewExDto);

            var aValidationResult = aAppSearchViewExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchViewEntity aAppSearchViewEntity;

            foreach (var searchViewFieldDto in aAppSearchViewExDto.AppSearchViewFieldList)
            {
                if (string.IsNullOrEmpty(searchViewFieldDto.DisplayText))
                {
                    searchViewFieldDto.DisplayText = searchViewFieldDto.SysTableFiledPath;
                }
                if (searchViewFieldDto.ControlType.HasValue && searchViewFieldDto.ControlType.Value != (int)EmAppControlType.DDL 
                    && searchViewFieldDto.ControlType.Value != (int)EmAppControlType.AutoComplete
                     && searchViewFieldDto.ControlType.Value != (int)EmAppControlType.SearchAbleDDL)
                {
                    searchViewFieldDto.EntityId = null;
                }

                if (!searchViewFieldDto.Sort.HasValue)
                {
                    int? maxSort = aAppSearchViewExDto.AppSearchViewFieldList.Max(o => o.Sort);
                    maxSort = maxSort.HasValue ? maxSort.Value : 0;
                    searchViewFieldDto.Sort = maxSort.Value + 10;
                }
            }

            if (aAppSearchViewExDto.ViewType == (int)EmAppViewType.ClusterAnalysisView)
            {
                if (aAppSearchViewExDto.OtherSettingsDto != null)
                {
                    PrepareClusterChildViewItems(aAppSearchViewExDto);

                    if (aAppSearchViewExDto.IsUpdateClusterMainViewItemSource)
                    {
                        SynchronizClusterItemSourceToChildViewItemFields(aAppSearchViewExDto);
                    }
                    else
                    {
                        PrepareClusterChildViewFieldList(aAppSearchViewExDto);
                    }
                }
            }

            // prepare Data
            if (aAppSearchViewExDto.IsNew)
            {
                aAppSearchViewEntity = new AppSearchViewEntity();
                AppSearchViewConverter.CopyDtoToEntity(aAppSearchViewEntity, aAppSearchViewExDto);


                foreach (var searchTemplateSubitemDto in aAppSearchViewExDto.AppSearchViewFieldList)
                {
                    AppSearchViewFieldEntity aAppSearchViewFieldEntity = new AppSearchViewFieldEntity();
                    AppSearchViewFieldConverter.CopyDtoToEntity(aAppSearchViewFieldEntity, searchTemplateSubitemDto);
                    aAppSearchViewEntity.AppSearchViewField.Add(aAppSearchViewFieldEntity);
                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppSearchViewEntity);
                        adapter.Commit();

                        aAppSearchViewExDto.Id = aAppSearchViewEntity.SearchViewId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewExDto), "app_SearchViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }


                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewExDto), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppSearchViewExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppSearchViewExDto(aAppSearchViewExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                // need remove Da

                object serchViewId = aAppSearchViewExDto.Id;
                RemoveViewNoneExsitDateFilePath(aValidationResult, serchViewId);

                aOperationCallResult.Object = RetrieveOneAppSearchViewExDto(aAppSearchViewExDto.Id);
            }

            return aOperationCallResult;
        }


        public static AppSearchViewFieldEntity RetrieveOneAppSearchViewFieldEntity(object searchViewFieldId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchViewFieldEntity aEntity = new AppSearchViewFieldEntity(int.Parse(searchViewFieldId.ToString()));


                adpater.FetchEntity(aEntity);
                return aEntity;
            }
        }


        public static AppSearchViewFieldExDto RetrieveOneAppSearchViewFieldExDto(object searchViewFieldId)
        {
            AppSearchViewFieldEntity aAppSearchViewFieldEntity = RetrieveOneAppSearchViewFieldEntity(searchViewFieldId);

            AppSearchViewFieldExDto aAppSearchViewFieldDto = AppSearchViewFieldConverter.ConvertEntityToExDto(aAppSearchViewFieldEntity);


            return aAppSearchViewFieldDto;
        }


        public static OperationCallResult<AppSearchViewFieldExDto> SaveAppSearchViewFieldExDto(AppSearchViewFieldExDto aAppSearchViewFieldExDto)
        {
            OperationCallResult<AppSearchViewFieldExDto> aOperationCallResult = new OperationCallResult<AppSearchViewFieldExDto>();

            var aValidationResult = aAppSearchViewFieldExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchViewFieldEntity aAppSearchViewFieldEntity;


            if (string.IsNullOrEmpty(aAppSearchViewFieldExDto.DisplayText))
            {
                aAppSearchViewFieldExDto.DisplayText = aAppSearchViewFieldExDto.SysTableFiledPath;
            }

            if (aAppSearchViewFieldExDto.ControlType.HasValue
                && aAppSearchViewFieldExDto.ControlType.Value != (int)EmAppControlType.DDL
                && aAppSearchViewFieldExDto.ControlType.Value != (int)EmAppControlType.AutoComplete
                && aAppSearchViewFieldExDto.ControlType.Value != (int)EmAppControlType.SearchAbleDDL)
            {
                aAppSearchViewFieldExDto.EntityId = null;
            }


            // prepare Data
            if (aAppSearchViewFieldExDto.IsNew)
            {
                aAppSearchViewFieldEntity = new AppSearchViewFieldEntity();
                AppSearchViewFieldConverter.CopyDtoToEntity(aAppSearchViewFieldEntity, aAppSearchViewFieldExDto);


                if (!aValidationResult.HasErrors)
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSearchViewFieldEntity);
                            adapter.Commit();

                            aAppSearchViewFieldExDto.Id = aAppSearchViewFieldEntity.SearchViewFieldId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewFieldExDto), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }


                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewFieldExDto), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
            }

            else if (aAppSearchViewFieldExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppSearchViewFieldExDto(aAppSearchViewFieldExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSearchViewFieldExDto(aAppSearchViewFieldExDto.Id);
            }

            return aOperationCallResult;
        }


        private static ValidationResult ProcessDirtyAppSearchViewFieldExDto(AppSearchViewFieldExDto aAppSearchViewFieldExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppSearchViewFieldEntity aAppSearchViewFieldEntity = RetrieveOneAppSearchViewFieldEntity(aAppSearchViewFieldExDto.Id);

            AppSearchViewFieldConverter.CopyDtoToEntity(aAppSearchViewFieldEntity, aAppSearchViewFieldExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSearchViewFieldEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewFieldEntity), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewFieldEntity), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        private static void PrepareClusterChildViewItems(AppSearchViewExDto aAppSearchViewExDto)
        {
            aAppSearchViewExDto.OtherSettingsDto.ClusterChildViewItemList = new List<AppDesktopItemExDto>();

            if (aAppSearchViewExDto.OtherSettingsDto.FlexLayoutItems != null)
            {
                foreach (AppDesktopItemExDto layoutItem in aAppSearchViewExDto.OtherSettingsDto.FlexLayoutItems)
                {
                    PrepareClusterChildViewItems_ProcessChildLayoutItem(layoutItem, aAppSearchViewExDto);
                }
            }

            foreach (var viewField in aAppSearchViewExDto.AppSearchViewFieldList)
            {
                if (string.IsNullOrWhiteSpace(viewField.UiId))
                {
                    viewField.UiId = ExtensionMethodhelper.RandomId();
                }
            }


        }

        private static void SynchronizClusterItemSourceToChildViewItemFields(AppSearchViewExDto aAppSearchViewExDto)
        {
            foreach (var mainViewFieldDto in aAppSearchViewExDto.AppSearchViewFieldList)
            {
                if (mainViewFieldDto.Id != null)
                {
                    foreach (AppDesktopItemExDto clusterChildViewItem in aAppSearchViewExDto.OtherSettingsDto.ClusterChildViewItemList)
                    {
                        var matchChildViewField = clusterChildViewItem.AppSearchViewFieldList.FirstOrDefault(o => o.Id != null && (int)o.Id == (int)mainViewFieldDto.Id);

                        if (matchChildViewField != null)
                        {
                            matchChildViewField.SysTableFiledPath = mainViewFieldDto.SysTableFiledPath;
                            matchChildViewField.DisplayText = mainViewFieldDto.DisplayText;
                            matchChildViewField.ControlType = mainViewFieldDto.ControlType;
                            matchChildViewField.EntityId = mainViewFieldDto.EntityId;
                            matchChildViewField.IsCalulationField = mainViewFieldDto.IsCalulationField;
                        }
                    }
                }
            }
        }

        private static void PrepareClusterChildViewFieldList(AppSearchViewExDto aAppSearchViewExDto)
        {

            aAppSearchViewExDto.AppSearchViewFieldList = new ObservableSet<AppSearchViewFieldExDto>();

            if (aAppSearchViewExDto.Id != null)
            {
                var orgViewDto = RetrieveOneAppSearchViewExDto(aAppSearchViewExDto.Id);
                aAppSearchViewExDto.AppSearchViewFieldList = orgViewDto.AppSearchViewFieldList;
            }


            List<int> needToKeepExistViewFieldIds = aAppSearchViewExDto.AppSearchViewFieldList.Where(o => o.IsCalulationField.HasValue && o.IsCalulationField.Value).Select(o => (int)o.Id).ToList();

            foreach (AppDesktopItemExDto clusterChildViewItem in aAppSearchViewExDto.OtherSettingsDto.ClusterChildViewItemList)
            {
                if (clusterChildViewItem.ViewType.HasValue)
                {
                    if (clusterChildViewItem.ViewType.Value == (int)EmAppViewType.ChartView)
                    {
                        clusterChildViewItem.AppSearchViewFieldList = new List<AppSearchViewFieldExDto>();

                        if (!string.IsNullOrWhiteSpace(clusterChildViewItem.AynalysisViewFieldX))
                        {
                            var existingField = aAppSearchViewExDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == clusterChildViewItem.AynalysisViewFieldX);

                            if (existingField == null)
                            {
                                var newField = new AppSearchViewFieldExDto()
                                {
                                    SysTableFiledPath = clusterChildViewItem.AynalysisViewFieldX,
                                    DisplayText = clusterChildViewItem.AynalysisViewFieldX,
                                    ControlType = (int)EmAppControlType.TextBox,
                                    IsVisible = true,
                                    IsMapToChartX = true,
                                    UiId = ExtensionMethodhelper.RandomId(),
                                };

                                aAppSearchViewExDto.AppSearchViewFieldList.Add(newField);

                                clusterChildViewItem.AppSearchViewFieldList.Add(newField.DeepCopy());
                            }
                            else
                            {
                                if (existingField.Id != null)
                                {
                                    needToKeepExistViewFieldIds.Add((int)existingField.Id);
                                }

                                var field = existingField.DeepCopy();
                                field.IsMapToChartX = true;
                                field.IsMapToChartY = false;

                                clusterChildViewItem.AppSearchViewFieldList.Add(field);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(clusterChildViewItem.AynalysisViewFieldY))
                        {
                            var existingField = aAppSearchViewExDto.AppSearchViewFieldList.FirstOrDefault(
                                o => o.SysTableFiledPath == clusterChildViewItem.AynalysisViewFieldY && o.ControlType == (int)EmAppControlType.Numeric);

                            if (existingField == null)
                            {
                                var newField = new AppSearchViewFieldExDto()
                                {
                                    SysTableFiledPath = clusterChildViewItem.AynalysisViewFieldY,
                                    DisplayText = clusterChildViewItem.AynalysisViewFieldY,
                                    ControlType = (int)EmAppControlType.Numeric,
                                    IsVisible = true,
                                    IsMapToChartY = true,
                                    UiId = ExtensionMethodhelper.RandomId(),
                                };

                                aAppSearchViewExDto.AppSearchViewFieldList.Add(newField); clusterChildViewItem.AppSearchViewFieldList.Add(newField);
                            }
                            else
                            {
                                if (existingField.Id != null)
                                {
                                    needToKeepExistViewFieldIds.Add((int)existingField.Id);
                                }

                                var field = existingField.DeepCopy();
                                field.IsMapToChartX = false;
                                field.IsMapToChartY = true;

                                clusterChildViewItem.AppSearchViewFieldList.Add(field);
                            }
                        }

                        if (clusterChildViewItem.ChartType.HasValue && clusterChildViewItem.ChartType.Value == (int)EmAppChartViewType.TreeMap)
                        {
                            if (!string.IsNullOrWhiteSpace(clusterChildViewItem.AynalysisViewFieldNodeId))
                            {
                                var existingField = aAppSearchViewExDto.AppSearchViewFieldList
                                    .FirstOrDefault(o => o.SysTableFiledPath == clusterChildViewItem.AynalysisViewFieldNodeId && o.ControlType == (int)EmAppControlType.Numeric);

                                if (existingField == null)
                                {
                                    var newField = new AppSearchViewFieldExDto()
                                    {
                                        SysTableFiledPath = clusterChildViewItem.AynalysisViewFieldNodeId,
                                        DisplayText = clusterChildViewItem.AynalysisViewFieldNodeId,
                                        ControlType = (int)EmAppControlType.Numeric,
                                        IsVisible = true,
                                        IsUserDefined1 = true,
                                        UiId = ExtensionMethodhelper.RandomId(),
                                    };

                                    aAppSearchViewExDto.AppSearchViewFieldList.Add(newField);

                                    clusterChildViewItem.AppSearchViewFieldList.Add(newField.DeepCopy());
                                }
                                else
                                {
                                    if (existingField.Id != null)
                                    {
                                        needToKeepExistViewFieldIds.Add((int)existingField.Id);
                                    }

                                    var field = existingField.DeepCopy();
                                    field.IsUserDefined1 = true;

                                    clusterChildViewItem.AppSearchViewFieldList.Add(field);
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(clusterChildViewItem.AynalysisViewFieldParentNodeId))
                            {
                                var existingField = aAppSearchViewExDto.AppSearchViewFieldList
                                    .FirstOrDefault(o => o.SysTableFiledPath == clusterChildViewItem.AynalysisViewFieldParentNodeId && o.ControlType == (int)EmAppControlType.Numeric);

                                if (existingField == null)
                                {
                                    var newField = new AppSearchViewFieldExDto()
                                    {
                                        SysTableFiledPath = clusterChildViewItem.AynalysisViewFieldParentNodeId,
                                        DisplayText = clusterChildViewItem.AynalysisViewFieldParentNodeId,
                                        ControlType = (int)EmAppControlType.Numeric,
                                        IsVisible = true,
                                        IsUserDefined2 = true,
                                        UiId = ExtensionMethodhelper.RandomId(),
                                    };

                                    aAppSearchViewExDto.AppSearchViewFieldList.Add(newField);

                                    clusterChildViewItem.AppSearchViewFieldList.Add(newField.DeepCopy());
                                }
                                else
                                {
                                    if (existingField.Id != null)
                                    {
                                        needToKeepExistViewFieldIds.Add((int)existingField.Id);
                                    }

                                    var field = existingField.DeepCopy();
                                    field.IsUserDefined2 = true;

                                    clusterChildViewItem.AppSearchViewFieldList.Add(field);
                                }
                            }
                        }

                    }
                    //else if (clusterChildViewItem.ViewType.Value == (int)EmAppViewType.PivotView)
                    //{

                    //}
                    else
                    {
                        if (clusterChildViewItem.AppSearchViewFieldList != null)
                        {
                            foreach (var viewFiledDto in clusterChildViewItem.AppSearchViewFieldList)
                            {
                                if (string.IsNullOrWhiteSpace(viewFiledDto.DisplayText))
                                {
                                    viewFiledDto.DisplayText = viewFiledDto.SysTableFiledPath;
                                }

                                var existingField = aAppSearchViewExDto.AppSearchViewFieldList.FirstOrDefault(
                                    o =>
                                    {
                                        if (o.SysTableFiledPath == viewFiledDto.SysTableFiledPath && o.ControlType == viewFiledDto.ControlType)
                                        {
                                            if (o.ControlType == (int)EmAppControlType.DDL)
                                            {
                                                if (!(o.EntityId.HasValue && viewFiledDto.EntityId.HasValue && o.EntityId.Value == viewFiledDto.EntityId.Value))
                                                {
                                                    return false;
                                                }
                                            }

                                            if (!string.IsNullOrWhiteSpace(viewFiledDto.DisplayText))
                                            {
                                                if (viewFiledDto.DisplayText != o.DisplayText)
                                                {
                                                    return false;
                                                }
                                            }

                                            return true;

                                        }

                                        return false;
                                    });

                                if (existingField == null)
                                {
                                    aAppSearchViewExDto.AppSearchViewFieldList.Add(viewFiledDto);
                                }
                                else
                                {
                                    if (existingField.Id != null)
                                    {
                                        needToKeepExistViewFieldIds.Add((int)existingField.Id);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            aAppSearchViewExDto.AppSearchViewFieldList.DeletedItemIds = aAppSearchViewExDto.AppSearchViewFieldList
                .Where(o => o.Id != null && !needToKeepExistViewFieldIds.Contains((int)o.Id)).Select(o => o.Id).ToList<object>();

            //var fieldList = aAppSearchViewExDto.AppSearchViewFieldList.OrderBy(o => o.Sort).ToList();
            //for (int i = 0; i < fieldList.Count; i++)
            //{
            //    var fieldDto = fieldList[i];
            //    fieldDto.Sort = i + 1;
            //}

        }

        private static void PrepareClusterChildViewItems_ProcessChildLayoutItem(AppDesktopItemExDto layoutItem, AppSearchViewExDto aAppSearchViewExDto)
        {
            if (layoutItem.ChildDesktopItems != null && layoutItem.ChildDesktopItems.Count > 0)
            {
                foreach (AppDesktopItemExDto childLayoutItem in layoutItem.ChildDesktopItems)
                {
                    PrepareClusterChildViewItems_ProcessChildLayoutItem(childLayoutItem, aAppSearchViewExDto);
                }
            }
            else
            {
                if (layoutItem.DesktopWidget != null && layoutItem.DesktopWidget.WidgetItemType.HasValue
                    && layoutItem.DesktopWidget.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.ClusterAnalysisViewItem)
                {
                    if (string.IsNullOrWhiteSpace(layoutItem.DesktopWidget.UiId))
                    {
                        layoutItem.DesktopWidget.UiId = ExtensionMethodhelper.RandomId();
                    }

                    layoutItem.DesktopWidget.MasterClusterViewId = ControlTypeValueConverter.ConvertValueToInt(aAppSearchViewExDto.Id);

                    aAppSearchViewExDto.OtherSettingsDto.ClusterChildViewItemList.Add(layoutItem.DesktopWidget);
                }
            }
        }

        private static void RemoveViewNoneExsitDateFilePath(ValidationResult aValidationResult, object serchViewId)
        {
            AppSearchViewEntity adbAppSearchViewEntity = RetrieveOneAppSearchViewEntity(serchViewId);
            if (adbAppSearchViewEntity.DataSetId.HasValue)
            {


                //Id : coumnName display: datatype
                List<String> dataSetColumnList = AppDataSetBL.RetrieveQueryColumnList(adbAppSearchViewEntity.DataSetId.Value).Select(o => o.Id.ToString()).ToList();



                var needTodeleteFiedIds = new List<int>();
                var dictFiedIdSystempath = adbAppSearchViewEntity.AppSearchViewField.Where(o => !(o.IsCalulationField.HasValue && o.IsCalulationField.Value))
                    .ToDictionary(o => o.SearchViewFieldId, o => o.SysTableFiledPath);

                foreach (var pair in dictFiedIdSystempath)
                {
                    if (!dataSetColumnList.Contains(pair.Value))
                    {
                        needTodeleteFiedIds.Add(pair.Key);
                    }
                }




                if (needTodeleteFiedIds.Count > 0)
                {

                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.DeleteEntitiesDirectly(typeof(AppSearchViewFieldEntity), new RelationPredicateBucket(AppSearchViewFieldFields.SearchViewFieldId == needTodeleteFiedIds));
                            adapter.Commit();

                        }


                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewExDto), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }

                }




            }
        }

        private static void PrepaireDayPilotTypeSearchViewFieldsBeforeSave(AppSearchViewExDto aAppSearchViewExDto)
        {
            if (aAppSearchViewExDto != null &&
                                (aAppSearchViewExDto.ViewType == (int)EmAppViewType.CalendarView
                                || aAppSearchViewExDto.ViewType == (int)EmAppViewType.GanttView
                                || aAppSearchViewExDto.ViewType == (int)EmAppViewType.SchedulerView
                            ))
            {
                RemoveDayPilotTypeSearchViewNotRequiredEmptyFields(aAppSearchViewExDto);
                AddAllDataSetColumnToSearchViewField(aAppSearchViewExDto);
            }
        }

        internal static void AddAllDataSetColumnToSearchViewField(AppSearchViewExDto aAppSearchViewExDto, bool visible = false)
        {
            List<LookupItemDto> dataSetColumns = AppDataSetBL.RetrieveQueryColumnList(aAppSearchViewExDto.DataSetId.Value);

            AppSearchViewExDto orgSearchViewExDto = null;
            if (aAppSearchViewExDto.Id != null)
            {
                orgSearchViewExDto = RetrieveOneAppSearchViewExDto(aAppSearchViewExDto.Id);

            }

            foreach (LookupItemDto aDatasetColumn in dataSetColumns)
            {
                string colName = aDatasetColumn.Id.ToString();

                if (orgSearchViewExDto == null ||
                   orgSearchViewExDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath.ToLower() == colName.ToLower()) == null)
                {
                    AppSearchViewFieldExDto newSearchViewField = new AppSearchViewFieldExDto();
                    newSearchViewField.SysTableFiledPath = colName;
                    newSearchViewField.DisplayText = colName;
                    newSearchViewField.IsVisible = visible;
                    newSearchViewField.IsGroupBy = false;
                    newSearchViewField.IsDescOrder = false;
                    newSearchViewField.ControlType = 2;
                    newSearchViewField.EntityId = null;
                    newSearchViewField.DataType = null;
                    newSearchViewField.ProductDetaiMapTransFiledId = null;
                    newSearchViewField.IsMapToChartX = false;
                    newSearchViewField.IsMapToChartY = false;
                    newSearchViewField.IsFileFoderId = false;
                    newSearchViewField.IsTransRootId = false;
                    newSearchViewField.IsUserDefined1 = false;
                    newSearchViewField.IsUserDefined2 = false;
                    newSearchViewField.IsUserDefined3 = false;
                    newSearchViewField.IsUserDefined4 = false;
                    aAppSearchViewExDto.AppSearchViewFieldList.Add(newSearchViewField);
                }

            }
        }

        private static void RemoveDayPilotTypeSearchViewNotRequiredEmptyFields(AppSearchViewExDto aAppSearchViewExDto)
        {
            List<AppSearchViewFieldExDto> needToRemoveViewFieldList = new List<AppSearchViewFieldExDto>();

            foreach (var aViewField in aAppSearchViewExDto.AppSearchViewFieldList)
            {
                if (aAppSearchViewExDto.ViewType == (int)EmAppViewType.CalendarView)
                {
                    if (!AppStaticDataSetSearchBL.CalendarViewRequiredFieldList.Contains(aViewField.DisplayText))
                    {
                        if (string.IsNullOrWhiteSpace(aViewField.SysTableFiledPath))
                        {
                            needToRemoveViewFieldList.Add(aViewField);
                        }
                    }
                }
                else if (aAppSearchViewExDto.ViewType == (int)EmAppViewType.GanttView)
                {
                    if (!AppStaticDataSetSearchBL.GanttViewRequiredFieldList.Contains(aViewField.DisplayText))
                    {
                        if (string.IsNullOrWhiteSpace(aViewField.SysTableFiledPath))
                        {
                            needToRemoveViewFieldList.Add(aViewField);
                        }
                    }
                }
                else if (aAppSearchViewExDto.ViewType == (int)EmAppViewType.SchedulerView)
                {
                    if (!AppStaticDataSetSearchBL.SchedulerViewRequiredFieldList.Contains(aViewField.DisplayText))
                    {
                        if (string.IsNullOrWhiteSpace(aViewField.SysTableFiledPath))
                        {
                            needToRemoveViewFieldList.Add(aViewField);
                        }
                    }
                }
            }

            needToRemoveViewFieldList.ForEach(o => aAppSearchViewExDto.AppSearchViewFieldList.Remove(o));
        }

        private static ValidationResult ProcessDirtyAppSearchViewExDto(AppSearchViewExDto aAppSearchViewExDto)
        {



            ValidationResult aValidationResult = new ValidationResult();

            // int[] deleteAppSearchViewFieldIDs = aAppSearchViewExDto.AppSearchViewFieldList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppSearchViewEntity aAppSearchViewEntity = RetrieveOneAppSearchViewEntity(aAppSearchViewExDto.Id);

            //if (aAppSearchViewExDto.ViewType == (int)EmAppViewType.ClusterAnalysisView)
            //{
            //    aAppSearchViewExDto.AppSearchViewFieldList.DeletedItemIds = aAppSearchViewEntity.AppSearchViewField.Select(o => (object)o.SearchViewFieldId).ToList();
            //}

            Dictionary<int, AppSearchViewFieldEntity> dictAppSearchViewFieldFromDbms = aAppSearchViewEntity.AppSearchViewField.ToDictionary(o => o.SearchViewFieldId, o => o);

            AppSearchViewConverter.CopyDtoToEntity(aAppSearchViewEntity, aAppSearchViewExDto);

            // new Items
            foreach (AppSearchViewFieldDto aChildDto in aAppSearchViewExDto.AppSearchViewFieldList.FindNewItems())
            {
                AppSearchViewFieldEntity aNewChildEntity = new AppSearchViewFieldEntity();
                AppSearchViewFieldConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppSearchViewEntity.AppSearchViewField.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppSearchViewExDto.AppSearchViewFieldList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppSearchViewFieldFromDbms.ContainsKey(dtoKey))
                {
                    AppSearchViewFieldConverter.CopyDtoToEntity(dictAppSearchViewFieldFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteAppSearchViewFieldIDs = new int[0];

            var deletedFieldIds = aAppSearchViewExDto.AppSearchViewFieldList.FindDeletedItemIds();

            if (deletedFieldIds != null)
            {
                deleteAppSearchViewFieldIDs = deletedFieldIds.Cast<int>().ToArray();
            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSearchViewEntity);

                    // Need to delete SearchTemplate subitems
                    if (deleteAppSearchViewFieldIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppSearchViewFieldEntity), new RelationPredicateBucket(AppSearchViewFieldFields.SearchViewFieldId == deleteAppSearchViewFieldIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewEntity), "app_SearchViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewEntity), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }




        //Delete a AppSearchView
        public static OperationCallResult<object> DeleteAppSearchView(object searchViewId, bool needToResetSearchDefultView = true)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string referMsg = string.Empty;

            List<AppSearchViewEntity> siblingViewList = GetSiblingSearchViewList(searchViewId);
            AppSearchViewEntity aAppSearchViewEntity = siblingViewList.FirstOrDefault();
            int? defaultSearchViewId = aAppSearchViewEntity == null ? null : aAppSearchViewEntity.SearchViewId as int?;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");


                    if (needToResetSearchDefultView)
                    {
                        string updateToSearchViewID = "null";
                        if (defaultSearchViewId.HasValue)
                        {
                            updateToSearchViewID = defaultSearchViewId.ToString();
                        }
                        //ALTER TABLE AppSearch ALTER COLUMN SearchViewID INT NULL
                        string setSearchViewIdNull = string.Format(@"update AppSearch set SearchViewID = " + updateToSearchViewID + " where SearchViewID = @searchViewId");
                        List<SqlParameter> paramter = new List<SqlParameter>();
                        paramter.Add(new SqlParameter("@searchViewId", searchViewId));
                        adapter.ExecuteScalarQuery(setSearchViewIdNull, paramter);
                    }

                    adapter.DeleteEntitiesDirectly(typeof(AppViewLinkedSeaechOrUrlEntity), new RelationPredicateBucket(AppViewLinkedSeaechOrUrlFields.SearchViewId == searchViewId));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.SearchViewId == searchViewId));
                    adapter.DeleteEntitiesDirectly(typeof(AppSearchViewFieldEntity), new RelationPredicateBucket(AppSearchViewFieldFields.SearchViewId == searchViewId));
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.SearchViewId == searchViewId));
                    adapter.DeleteEntitiesDirectly(typeof(AppSearchViewEntity), new RelationPredicateBucket(AppSearchViewFields.SearchViewId == searchViewId));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSearchViewEntity), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Object = searchViewId;
                }
            }
            return aValidationResult;
        }

        private static List<AppSearchViewEntity> GetSiblingSearchViewList(object searchViewId)
        {
            int searchViewIdInt = int.Parse(searchViewId.ToString());
            AppSearchViewEntity appSearchViewEntity = new AppSearchViewEntity(searchViewIdInt);


            List<AppSearchViewEntity> siblingViewList = new List<AppSearchViewEntity>();
            EntityCollection<AppSearchViewEntity> relatedSearchViewList = null;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {


                adapter.FetchEntity(appSearchViewEntity);
                int? dataSetId = appSearchViewEntity.DataSetId;
                if (dataSetId.HasValue)
                {
                    relatedSearchViewList = AppDataSetBL.GetAllSearchViewForOneDataSet(dataSetId.Value);
                }

            }

            if (!relatedSearchViewList.IsEmpty())
            {
                siblingViewList = relatedSearchViewList.Where(o => o.SearchViewId != searchViewIdInt).ToList();
            }

            return siblingViewList;
        }

        public static ObservableSet<AppViewFiledSearchFiledMappingExDto> RetrieveAppViewFiledSearchFiledMappingBySearchViewId(object searchViewId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppViewFiledSearchFiledMappingEntity> entityList = new EntityCollection<AppViewFiledSearchFiledMappingEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppViewFiledSearchFiledMappingFields.SearchViewId == searchViewId);

                adapter.FetchEntityCollection(entityList, filter);


                var aDtoList = new ObservableSet<AppViewFiledSearchFiledMappingExDto>();
                foreach (var AppViewFiledSearchFiledMappingEntity in entityList)
                {
                    AppViewFiledSearchFiledMappingExDto aAppViewFiledSearchFiledMappingExDto = AppViewFiledSearchFiledMappingConverter.ConvertEntityToExDto(AppViewFiledSearchFiledMappingEntity);


                    aDtoList.Add(aAppViewFiledSearchFiledMappingExDto);

                }
                return aDtoList;
            }
        }






        public static OperationCallResult<AppViewFiledSearchFiledMappingExDto> SaveAllAppViewFiledSearchFiledMappingExDto(ObservableSet<AppViewFiledSearchFiledMappingExDto> aSet, int searchViewId)
        {
            OperationCallResult<AppViewFiledSearchFiledMappingExDto> aOperationCallResult = new OperationCallResult<AppViewFiledSearchFiledMappingExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (AppViewFiledSearchFiledMappingExDto aAppViewFiledSearchFiledMappingExDto in aSet)
            {
                validationResult.Merge(aAppViewFiledSearchFiledMappingExDto.ValidateDto());
            }

            if (validationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = validationResult;
                return aOperationCallResult;
            }
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppViewFiledSearchFiledMappingEntity), new RelationPredicateBucket(AppViewFiledSearchFiledMappingFields.SearchViewId == searchViewId));

                    foreach (AppViewFiledSearchFiledMappingExDto aAppViewFiledSearchFiledMappingExDto in aSet)
                    {
                        AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity = new AppViewFiledSearchFiledMappingEntity();
                        AppViewFiledSearchFiledMappingConverter.CopyDtoToEntity(aAppViewFiledSearchFiledMappingEntity, aAppViewFiledSearchFiledMappingExDto);
                        aAppViewFiledSearchFiledMappingEntity.SearchViewId = searchViewId;
                        adapter.SaveEntity(aAppViewFiledSearchFiledMappingEntity, false, true);
                    }

                    adapter.Commit();
                    validationResult.Items.Add(new ValidationItem(typeof(AppViewFiledSearchFiledMappingEntity), "plm_AppViewFiledSearchFiledMapping_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppViewFiledSearchFiledMappingEntity), "plm_AppViewFiledSearchFiledMapping_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppViewFiledSearchFiledMappingEntity), "plm_AppViewFiledSearchFiledMapping_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }



            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAppViewFiledSearchFiledMappingBySearchViewId(searchViewId);
            }

            return aOperationCallResult;

        }








    }
}