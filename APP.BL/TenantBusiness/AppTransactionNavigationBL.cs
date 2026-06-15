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


using APP.Framework;
namespace App.BL
{
    public static class AppTransactionNavigationBL
    {

        public static AppTransactionNavigationExDto RetrieveOneTransactionDefaultNavigationDto(int? transactionId, bool isFolderNavigation = false)
        {
            AppTransactionExDto transDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            var menuData = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, transDto.SaasApplicationId).ToList();

            var rootMenu = menuData[0];

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionNavigationEntity> entityList = new EntityCollection<AppTransactionNavigationEntity>();

                PrefetchPath2 path = new PrefetchPath2(EntityType.AppTransactionNavigationEntity);
                path.Add(AppTransactionNavigationEntity.PrefetchPathAppSearchView);
                path.Add(AppTransactionNavigationEntity.PrefetchPathAppSearch);

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionNavigationFields.TransactionId == transactionId);

                if (isFolderNavigation)
                {
                    filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.FolderViewId != DBNull.Value);
                }
                else
                {
                    filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.QuickSearchId != DBNull.Value);
                }


                adapter.FetchEntityCollection(entityList, filter, 0, null, path);



                if (entityList.Count > 0)
                {
                    AppTransactionNavigationEntity defaultNavigationEntity = entityList.FirstOrDefault(o => o.IsDefaultView.HasValue && o.IsDefaultView.Value);

                    if (defaultNavigationEntity == null)
                    {
                        defaultNavigationEntity = entityList.FirstOrDefault();
                    }

                    AppTransactionNavigationExDto toReturn = AppTransactionNavigationConverter.ConvertEntityToExDto(defaultNavigationEntity);

                    toReturn.FolderViewDtoList = new List<AppSearchViewDto>();


                    if (!isFolderNavigation && toReturn.QuickSearchId.HasValue)
                    {
                        toReturn.NavigationType = (int)EmAppTransNavigationType.SearchNavigation;

                        var existingMenu = FindMenuInChildLevel(rootMenu, "MasterDataManagement", toReturn.QuickSearchId.Value.ToString());

                        if (existingMenu != null)
                        {
                            toReturn.DefaultMenuDto = existingMenu;
                        }


                        AppSearchEntity searchEntity = defaultNavigationEntity.AppSearch;

                        if (defaultNavigationEntity.AppSearch != null)
                        {
                            toReturn.DefaultSearchDto = AppSearchConverter.ConvertEntityToDto(defaultNavigationEntity.AppSearch);
                        }

                    }
                    else if (isFolderNavigation && toReturn.FolderViewId.HasValue)
                    {
                        toReturn.NavigationType = (int)EmAppTransNavigationType.FolderNavigation;

                        AppListMenuExDto existingMenu = FindMenuInChildLevel(rootMenu, "FolderNavigation", transactionId.Value.ToString());

                        if (existingMenu != null)
                        {
                            toReturn.DefaultMenuDto = existingMenu;
                        }

                        foreach (var navigationEntity in entityList.Where(o => o.FolderViewId.HasValue))
                        {
                            if (navigationEntity.AppSearchView != null)
                            {
                                AppSearchViewEntity searchViewEntity = navigationEntity.AppSearchView;

                                AppSearchViewDto searchViewDto = AppSearchViewConverter.ConvertEntityToDto(searchViewEntity);

                                if (navigationEntity.IsDefaultView.HasValue)
                                {
                                    searchViewDto.IsDefaultView = navigationEntity.IsDefaultView.Value;
                                    toReturn.DefaultFolderViewDto = searchViewDto;
                                }
                                else
                                {
                                    searchViewDto.IsDefaultView = false;

                                }

                                toReturn.FolderViewDtoList.Add(searchViewDto);
                            }
                        }
                    }




                    return toReturn;
                }
            }


            return null;
        }


        public static OperationCallResult<bool> DeleteOneTransactionAllNavigations(object transactionId)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionNavigationEntity), new RelationPredicateBucket(AppTransactionNavigationFields.TransactionId == transactionId));

                    adapter.Commit();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_AppTransactionNavigation_Save_OK", ValidationItemType.Message, "Delete Successful"));
                    operationCallResult.Object = true;
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_AppTransactionNavigation_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return operationCallResult;
        }

        public static List<AppSearchViewDto> RetrieveFolderSearchViewList(object transactionId)
        {
            List<AppSearchViewDto> aDtoList = new List<AppSearchViewDto>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionNavigationEntity> entityList = new EntityCollection<AppTransactionNavigationEntity>();

                IncludeFieldsList includFiled = new IncludeFieldsList();
                includFiled.Add(AppTransactionNavigationFields.IsDefaultView);
                includFiled.Add(AppTransactionNavigationFields.FolderViewId);

                PrefetchPath2 path = new PrefetchPath2(EntityType.AppTransactionNavigationEntity);
                path.Add(AppTransactionNavigationEntity.PrefetchPathAppSearchView);


                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionNavigationFields.TransactionId == transactionId);
                filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.QuickSearchId == DBNull.Value);


                adapter.FetchEntityCollection(entityList, filter, 0, null, path, includFiled);

                foreach (var AppTransactionNavigationEntity in entityList)
                {

                    AppSearchViewEntity viewEntity = AppTransactionNavigationEntity.AppSearchView;

                    AppSearchViewDto viewDto = AppSearchViewConverter.ConvertEntityToDto(viewEntity);

                    if (AppTransactionNavigationEntity.IsDefaultView.HasValue)
                    {
                        viewDto.IsDefaultView = AppTransactionNavigationEntity.IsDefaultView.Value;
                    }
                    else
                    {
                        viewDto.IsDefaultView = false;

                    }



                    aDtoList.Add(viewDto);

                }
                return aDtoList;
            }
        }


        public static ObservableSet<AppTransactionNavigationExDto> RetrieveFolderViewListBytransactionId(List<int> transactionIdList)
        {

            EntityCollection<AppTransactionNavigationEntity> entityList = GetTransactionNavigationEntityList(transactionIdList);


            var aDtoList = new ObservableSet<AppTransactionNavigationExDto>();
            foreach (var AppTransactionNavigationEntity in entityList)
            {
                AppTransactionNavigationExDto aAppTransactionNavigationExDto = AppTransactionNavigationConverter.ConvertEntityToExDto(AppTransactionNavigationEntity);


                aDtoList.Add(aAppTransactionNavigationExDto);

            }


            return aDtoList;
        }

        public static EntityCollection<AppTransactionNavigationEntity> GetTransactionNavigationEntityList(List<int> transactionIdList)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionNavigationEntity> entityList = new EntityCollection<AppTransactionNavigationEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionNavigationFields.TransactionId == transactionIdList);
                filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.QuickSearchId == DBNull.Value);


                adapter.FetchEntityCollection(entityList, filter);

                return entityList;

            }
        }

        public static ObservableSet<AppTransactionNavigationExDto> RetrieveSearchNavigationExDtoListBytransactionId(List<int> transactionIdList)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionNavigationEntity> entityList = new EntityCollection<AppTransactionNavigationEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionNavigationFields.TransactionId == transactionIdList);
                filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.QuickSearchId != DBNull.Value);


                adapter.FetchEntityCollection(entityList, filter);


                var aDtoList = new ObservableSet<AppTransactionNavigationExDto>();
                foreach (var AppTransactionNavigationEntity in entityList)
                {
                    AppTransactionNavigationExDto aAppTransactionNavigationExDto = AppTransactionNavigationConverter.ConvertEntityToExDto(AppTransactionNavigationEntity);


                    aDtoList.Add(aAppTransactionNavigationExDto);

                }

                return aDtoList;
            }
        }


        public static AppTransactionNavigationEntity RetrieveOneAppTransactionNavigationEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionNavigationEntity aAppTransactionNavigationEntity = new AppTransactionNavigationEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppTransactionNavigationEntity);
                return aAppTransactionNavigationEntity;
            }
        }

        public static OperationCallResult<AppTransactionNavigationExDto> SaveFolderViewNavigationListExDto(ObservableSet<AppTransactionNavigationExDto> aSet, int transactionId, int? rootFolderId, bool isEnableFolderSecurity)
        {
            OperationCallResult<AppTransactionNavigationExDto> aOperationCallResult = new OperationCallResult<AppTransactionNavigationExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            validationResult.Merge(UpdateTransactionMgtRootFolderId(transactionId, rootFolderId, isEnableFolderSecurity));

            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveFolderViewListBytransactionId(new List<int>() { transactionId });
            }

            return aOperationCallResult;

        }

        public static OperationCallResult<AppTransactionNavigationExDto> SaveQuickSearchNavigationListExDto(ObservableSet<AppTransactionNavigationExDto> aSet, int transactionId)
        {
            OperationCallResult<AppTransactionNavigationExDto> aOperationCallResult = new OperationCallResult<AppTransactionNavigationExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveSearchNavigationExDtoListBytransactionId(new List<int>() { transactionId });
            }

            return aOperationCallResult;

        }


        private static ValidationResult ProcessNewDto(AppTransactionNavigationExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionNavigationEntity aAppTransactionNavigationEntity = new AppTransactionNavigationEntity();
            AppTransactionNavigationConverter.CopyDtoToEntity(aAppTransactionNavigationEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionNavigationEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionNavigationEntity), "plm_AppTransactionNavigationEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionNavigationEntity), "plm_AppTransactionNavigationEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTransactionNavigationExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTransactionNavigationEntity aAppTransactionNavigationEntity = RetrieveOneAppTransactionNavigationEntity(aDto.Id);

            AppTransactionNavigationConverter.CopyDtoToEntity(aAppTransactionNavigationEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionNavigationEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionNavigationEntity), "plm_AppTransactionNavigationEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionNavigationEntity), "plm_AppTransactionNavigationEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDeleteDto(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppTransactionNavigationEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionNavigationEntity), new RelationPredicateBucket(AppTransactionNavigationFields.TransNavigationId == Id));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionNavigationEntity), "plm_AppTransactionNavigationEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }


        private static ValidationResult UpdateTransactionMgtRootFolderId(int transactionId, int? rootFolderId, bool isEnableFolderSecurity)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    AppTransactionEntity appTransactionEntity = new AppTransactionEntity();
                    appTransactionEntity.MgtRootFolderId = rootFolderId;
                    appTransactionEntity.IsEnableFolderSecurity = isEnableFolderSecurity;

                    adapter.UpdateEntitiesDirectly(appTransactionEntity, new RelationPredicateBucket(AppTransactionFields.TransactionId == transactionId));
                    adapter.Commit();

                    AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_AppTransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }


        private static AppListMenuExDto FindMenuInChildLevel(AppListMenuExDto rootMenu, string routeCode, string link)
        {
            AppListMenuExDto menuFound = null;

            if (rootMenu.AppListMenu_List != null && rootMenu.AppListMenu_List.Count > 0)
            {
                for (int i = 0; i < rootMenu.AppListMenu_List.Count; i++)
                {
                    var aMenu = rootMenu.AppListMenu_List.ToList()[i];

                    if (aMenu.RouteCode == routeCode && aMenu.Link == link)
                    {
                        menuFound = aMenu;
                    }
                    else
                    {
                        menuFound = FindMenuInChildLevel(aMenu, routeCode, link);
                    }

                    if (menuFound != null)
                    {
                        break;
                    }
                }
            }

            return menuFound;
        }


        public static TemplateFolderNavigationConfigResultDto RetrieveTemplateFolderNavigationConfig(int? templateSearchId)
        {
            var result = new TemplateFolderNavigationConfigResultDto
            {
                TemplateSearchId = templateSearchId,
                IsConfigured = false,
            };

            if (!templateSearchId.HasValue)
            {
                return result;
            }

            AppSearchExDto templateSearch = AppSearchConfigBL.RetrieveOneAppSearchExDto(templateSearchId.Value);
            if (templateSearch == null)
            {
                return result;
            }

            result.TemplateSearchName = templateSearch.Name;
            result.SearchViewId = templateSearch.SearchViewId;

            if (!templateSearch.SearchViewId.HasValue)
            {
                return result;
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionNavigationEntity> entityList = new EntityCollection<AppTransactionNavigationEntity>();
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionNavigationFields.FolderViewId == templateSearch.SearchViewId.Value);
                filter.PredicateExpression.AddWithAnd(AppTransactionNavigationFields.QuickSearchId == DBNull.Value);
                adapter.FetchEntityCollection(entityList, filter);

                if (entityList.Count > 0)
                {
                    var navEntity = entityList.FirstOrDefault(o => o.IsDefaultView.HasValue && o.IsDefaultView.Value) ?? entityList.FirstOrDefault();
                    if (navEntity != null)
                    {
                        result.IsConfigured = true;
                        result.HostTransactionId = navEntity.TransactionId;
                        if (navEntity.TransactionId.HasValue)
                        {
                            var hostTrans = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(navEntity.TransactionId);
                            result.RootFolderId = hostTrans?.MgtRootFolderId;
                            result.IsEnableFolderSecurity = hostTrans?.IsEnableFolderSecurity ?? false;
                        }
                    }
                }
            }

            return result;
        }


        public static OperationCallResult<TemplateFolderNavigationConfigResultDto> SaveTemplateFolderNavigationConfig(TemplateFolderNavigationConfigDto configDto)
        {
            OperationCallResult<TemplateFolderNavigationConfigResultDto> operationCallResult = new OperationCallResult<TemplateFolderNavigationConfigResultDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (configDto == null || !configDto.TemplateSearchId.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_TemplateRequired", ValidationItemType.Error, "Template is required."));
                return operationCallResult;
            }

            if (!configDto.HostTransactionId.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_HostRequired", ValidationItemType.Error, "Host Transaction is required."));
                return operationCallResult;
            }

            if (!configDto.RootFolderId.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_RootFolderRequired", ValidationItemType.Error, "Root Folder is required."));
                return operationCallResult;
            }

            AppSearchExDto templateSearch = AppSearchConfigBL.RetrieveOneAppSearchExDto(configDto.TemplateSearchId.Value);
            if (templateSearch == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_TemplateNotFound", ValidationItemType.Error, "Template not found."));
                return operationCallResult;
            }

            if (templateSearch.Type != (int)EmAppSearchUsageType.DataModelTemplate)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_InvalidTemplateType", ValidationItemType.Error, "Search is not a Data Model Template."));
                return operationCallResult;
            }

            int? searchViewId = configDto.SearchViewId ?? templateSearch.SearchViewId;
            if (!searchViewId.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_SearchViewRequired", ValidationItemType.Error, "Template default Search View is required."));
                return operationCallResult;
            }

            if (!templateSearch.DataSetId.HasValue)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_DataSetRequired", ValidationItemType.Error, "Template Dataset is required."));
                return operationCallResult;
            }

            AppSearchViewExDto searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchViewId.Value);
            if (searchViewDto == null || searchViewDto.AppSearchViewFieldList == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_SearchViewNotFound", ValidationItemType.Error, "Template Search View not found."));
                return operationCallResult;
            }

            if (searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.IsFileFoderId.HasValue && o.IsFileFoderId.Value) == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_FolderFieldRequired", ValidationItemType.Error, "Template Search View must include a Folder Id field (IsFileFoderId)."));
                return operationCallResult;
            }

            var linkTargets = LinkTragetBL.RetrieveOneSearchViewLinkTargetList(searchViewId.Value, (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);
            bool hasMainEdit = linkTargets.Any(o =>
                o.ActionType == (int)EmAppLinkTargetActionType.Edit
                && (o.OtherSettingsDto == null || o.OtherSettingsDto.TemplateItemType != (int)EmAppTransactionTemplateItemType.TemplateHeader));
            if (!hasMainEdit)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_EditLinkRequired", ValidationItemType.Error, "Template Search View must have at least one Main Item Edit link target."));
                return operationCallResult;
            }

            AppTransactionExDto hostTrans = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(configDto.HostTransactionId);
            if (hostTrans == null)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionExDto), "TemplateFolderNavigation_HostNotFound", ValidationItemType.Error, "Host Transaction not found."));
                return operationCallResult;
            }

            if (hostTrans.TransactionOrganizedType != (int)EmTransactionOrganizedType.MasterDetail)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionExDto), "TemplateFolderNavigation_HostMustBeForm", ValidationItemType.Error, "Host Transaction must be a Form Model (Master Detail)."));
                return operationCallResult;
            }

            if (validationResult.HasErrors)
            {
                return operationCallResult;
            }

            ObservableSet<AppTransactionNavigationExDto> navSet = RetrieveFolderViewListBytransactionId(new List<int>() { configDto.HostTransactionId.Value });
            bool foundExisting = false;
            foreach (var nav in navSet)
            {
                if (nav.FolderViewId == searchViewId.Value)
                {
                    nav.IsDefaultView = true;
                    nav.IsModified = true;
                    foundExisting = true;
                }
                else if (nav.IsDefaultView.HasValue && nav.IsDefaultView.Value)
                {
                    nav.IsDefaultView = false;
                    nav.IsModified = true;
                }
            }

            if (!foundExisting)
            {
                navSet.Add(new AppTransactionNavigationExDto()
                {
                    TransactionId = configDto.HostTransactionId,
                    FolderViewId = searchViewId.Value,
                    IsDefaultView = true,
                });
            }

            var saveResult = SaveFolderViewNavigationListExDto(navSet, configDto.HostTransactionId.Value, configDto.RootFolderId, configDto.IsEnableFolderSecurity);
            validationResult.Merge(saveResult.ValidationResult);

            if (!validationResult.HasErrors)
            {
                SaveOrUpdateFolderNavigationAppMenu(validationResult, configDto.HostTransactionId.Value, templateSearch.SaasApplicationId, templateSearch.Name, templateSearch.Description);
                operationCallResult.Object = RetrieveTemplateFolderNavigationConfig(configDto.TemplateSearchId);
                operationCallResult.Object.IsConfigured = true;
            }

            return operationCallResult;
        }


        public static FolderNavigationRuntimeContextDto ResolveFolderNavigationRuntimeContext(int? transactionId)
        {
            var result = new FolderNavigationRuntimeContextDto
            {
                HostTransactionId = transactionId,
                IsTemplateMode = false,
            };

            if (!transactionId.HasValue)
            {
                return result;
            }

            var defaultNav = RetrieveOneTransactionDefaultNavigationDto(transactionId, true);
            if (defaultNav?.DefaultFolderViewDto?.Id == null)
            {
                return result;
            }

            int? folderViewId = ControlTypeValueConverter.ConvertValueToInt(defaultNav.DefaultFolderViewDto.Id);
            if (!folderViewId.HasValue)
            {
                return result;
            }
            result.DefaultFolderViewId = folderViewId;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSearchEntity> searchList = new EntityCollection<AppSearchEntity>();
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppSearchFields.SearchViewId == folderViewId.Value);
                filter.PredicateExpression.AddWithAnd(AppSearchFields.Type == (int)EmAppSearchUsageType.DataModelTemplate);
                adapter.FetchEntityCollection(searchList, filter);

                if (searchList.Count > 0)
                {
                    var searchEntity = searchList.FirstOrDefault();
                    result.IsTemplateMode = true;
                    result.TemplateSearchId = searchEntity.SearchId;
                    result.TemplateSearchName = searchEntity.Name;
                }
            }

            return result;
        }


        private static void SaveOrUpdateFolderNavigationAppMenu(ValidationResult validationResult, int hostTransactionId, int? applicationId, string menuName, string menuDescription)
        {
            if (!applicationId.HasValue)
            {
                return;
            }

            var menuData = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, applicationId).ToList();
            var rootMenu = menuData[0];
            string link = hostTransactionId.ToString();
            AppListMenuExDto existingMenu = FindMenuInChildLevel(rootMenu, "FolderNavigation", link);

            if (existingMenu != null)
            {
                existingMenu.Name = menuName;
                existingMenu.Description = menuDescription;
                existingMenu.IsModified = true;
                var updateResult = AppTreeListMenuBL.SaveOneAppListMenuTreeNode(existingMenu);
                if (!updateResult.IsSuccessful)
                {
                    validationResult.Merge(updateResult.ValidationResult);
                }
                else
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_MenuUpdated", ValidationItemType.Message,
                        "App menu updated. \n" + "Menu path: " + existingMenu.MenuPath));
                }
                return;
            }

            int maxSort = 0;
            if (rootMenu.AppListMenu_List != null)
            {
                foreach (var aMenu in rootMenu.AppListMenu_List)
                {
                    if (aMenu.Sort.HasValue && aMenu.Sort > maxSort)
                    {
                        maxSort = aMenu.Sort.Value;
                    }
                }
            }

            AppListMenuExDto menuDto = new AppListMenuExDto();
            menuDto.EmAppMenuItemCategory = 1;
            menuDto.EmDeviceMenuShowMode = 3;
            menuDto.LinkType = 1;
            menuDto.RouteCode = "FolderNavigation";
            menuDto.IconName = "";
            menuDto.Sort = maxSort + 1;
            menuDto.Name = menuName;
            menuDto.Description = menuDescription;
            menuDto.Link = link;
            menuDto.ParentId = (int)rootMenu.Id;

            var createMenuResult = AppTreeListMenuBL.SaveOneAppListMenuTreeNode(menuDto);
            if (createMenuResult.IsSuccessfulWithResult)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "TemplateFolderNavigation_MenuCreated", ValidationItemType.Message,
                    "New App menu created. \n" + "Menu path: " + rootMenu.Name + " / " + menuName));
            }
            else
            {
                validationResult.Merge(createMenuResult.ValidationResult);
            }
        }

    }


}