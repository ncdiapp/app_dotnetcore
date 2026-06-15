using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Components.EntityConverter;
using System.Data;
using APP.Framework.Communication;
using System;


using APP.Framework;
namespace App.BL
{
    public static class AppSaasUserApplicationPackageBL
    {

        public static List<AppListMenuExDto> RetrieveAvailableApplicationPackages(bool? excludeChildMenu = false)
        {
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)
            {
                var list = AppTreeListMenuBL.RetrieveMgtAllFlatListMenuEntity(false, true);

                List<AppListMenuExDto> selectedPackages = RetrieveSelectedApplicationPackages(excludeChildMenu);
                List<AppListMenuExDto> availablePackages = ConvertListMenuEntityToApplicationPackageDtoList(list);

                foreach (var selectedPackage in selectedPackages)
                {
                    if (selectedPackage.GlobalGuid.HasValue)
                    {
                        var availablePackage = availablePackages.FirstOrDefault(o => o.GlobalGuid.HasValue && o.GlobalGuid.Value == selectedPackage.GlobalGuid.Value);

                        if (availablePackage != null)
                        {
                            availablePackage.IsPackageInstalled = true;
                            availablePackage.InstalledPackageUserDBMenuId = (int)selectedPackage.Id;
                        }
                    }
                }

                if (excludeChildMenu.HasValue && excludeChildMenu.Value)
                {
                    availablePackages.ForAll(o => o.AppListMenu_List = new ObservableSet<AppListMenuExDto>());
                }

                return availablePackages;
            }
            else
            {
                return new List<AppListMenuExDto>();
            }
        }

        public static List<AppMenuSimpleDto> GetSaasApplicationList(bool? excludeChildMenu = false)
        {
            List<AppMenuSimpleDto> applicationList = new List<AppMenuSimpleDto>();
            List<AppListMenuExDto> menuList = AppSaasUserApplicationPackageBL.RetrieveSelectedApplicationPackages(excludeChildMenu);

            foreach (var menuExDto in menuList)
            {
                var simpleDto = AppMenuSimpleDto.ConvertAppListMenuExDtoToAppMenuSimpleDto(menuExDto);
                applicationList.Add(simpleDto);
            }

            return applicationList;
        }

        public static List<AppListMenuExDto> RetrieveSelectedApplicationPackages(bool? excludeChildMenu = false)
        {
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            var toReturn = new List<AppListMenuExDto>();

            if (isAdmin)
            {
                var list = AppTreeListMenuBL.RetrieveMgtAllFlatListMenuEntity(false, false);
                toReturn = ConvertListMenuEntityToApplicationPackageDtoList(list);
            }
            else
            {
                ObservableSet<AppListMenuExDto> userMenuList = AppSecurityManagementBL.RetrieveUserTreeMenu();


                toReturn = userMenuList.ToList();
            }

            if (excludeChildMenu.HasValue && excludeChildMenu.Value)
            {
                toReturn.ForAll(o => o.AppListMenu_List = new ObservableSet<AppListMenuExDto>());
            }

            return toReturn;
        }

        //public static OperationCallResult<bool> InstallOneApplicationPackage(int? packageMenuId)
        //{
        //    OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
        //    ValidationResult validationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = validationResult;

        //    if (packageMenuId.HasValue)
        //    {
        //        List<AppListMenuExDto> availableAppPackages = RetrieveAvailableApplicationPackages();
        //        AppListMenuExDto packageMenuExDto = availableAppPackages.FirstOrDefault(o => ((int)o.Id == packageMenuId.Value));

        //        if (packageMenuExDto != null)
        //        {
        //            ObservableSet<AppListMenuExDto> sourceMenuTreeNode = new ObservableSet<AppListMenuExDto>() { packageMenuExDto };
        //            ObservableSet<AppListMenuExDto> flatMenuList = new ObservableSet<AppListMenuExDto>();
        //            AppTreeListMenuBL.ConvertMenuTreeToFlatList(sourceMenuTreeNode, flatMenuList);
        //            EntityCollection<AppApplicationAssetsItemEntity> sourceAssetsItemEntityList = RetrieveOneApplicationAppApplicationAssetsItemEntityList(packageMenuId.Value, true);

        //            Dictionary<Guid, Guid?> dictMenuGuidAndParentGuid = new Dictionary<Guid, Guid?>();

        //            flatMenuList.Where(o => !o.GlobalGuid.HasValue).ForAll(o => o.GlobalGuid = Guid.NewGuid());

        //            foreach (AppListMenuExDto menuExDto in flatMenuList)
        //            {
        //                dictMenuGuidAndParentGuid[menuExDto.GlobalGuid.Value] = null;

        //                if (menuExDto.ParentId.HasValue)
        //                {
        //                    var parentMenu = flatMenuList.FirstOrDefault(o => (int)o.Id == menuExDto.ParentId.Value);
        //                    if (parentMenu != null)
        //                    {
        //                        dictMenuGuidAndParentGuid[menuExDto.GlobalGuid.Value] = parentMenu.GlobalGuid;
        //                    }
        //                }
        //            }

        //            EntityCollection<AppListMenuEntity> needToSaveMenuEntities = new EntityCollection<AppListMenuEntity>();

        //            foreach (AppListMenuExDto menuExDto in flatMenuList)
        //            {
        //                menuExDto.Id = null;
        //                menuExDto.ParentId = null;
        //                menuExDto.AppCreatedByCompanyId = null;
        //                menuExDto.AppCreatedById = null;
        //                menuExDto.AppCreatedDate = null;
        //                menuExDto.AppModifiedById = null;
        //                menuExDto.AppModifiedDate = null;

        //                AppListMenuEntity newMenuEntity = new AppListMenuEntity();
        //                AppListMenuConverter.CopyDtoToEntity(newMenuEntity, menuExDto);
        //                newMenuEntity.IsNew = true;
        //                needToSaveMenuEntities.Add(newMenuEntity);
        //            }

        //            needToSaveMenuEntities.Where(o => o.GlobalGuid.HasValue).Select(o => o.GlobalGuid.Value).ToList();
        //            int? newInstallApplicationId = null;

        //            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //            {
        //                try
        //                {
        //                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
        //                    adapter.SaveEntityCollection(needToSaveMenuEntities);

        //                    EntityCollection<AppListMenuEntity> needToUpdateEntities = new EntityCollection<AppListMenuEntity>();
        //                    RelationPredicateBucket filter = new RelationPredicateBucket(AppListMenuFields.GlobalGuid == dictMenuGuidAndParentGuid.Keys.ToList());
        //                    adapter.FetchEntityCollection(needToUpdateEntities, filter);

        //                    Dictionary<Guid, int> dictMenuGuidAndMenuId = needToUpdateEntities.Where(o => o.GlobalGuid.HasValue).ToDictionary(o => o.GlobalGuid.Value, o => o.MenuId);

        //                    foreach (var menuEntity in needToUpdateEntities)
        //                    {
        //                        if (menuEntity.GlobalGuid.HasValue && dictMenuGuidAndParentGuid.ContainsKey(menuEntity.GlobalGuid.Value))
        //                        {
        //                            Guid? parentGuid = dictMenuGuidAndParentGuid[menuEntity.GlobalGuid.Value];

        //                            if (parentGuid.HasValue && dictMenuGuidAndMenuId.ContainsKey(parentGuid.Value))
        //                            {
        //                                int parentMenuId = dictMenuGuidAndMenuId[parentGuid.Value];

        //                                AppListMenuEntity entity = new AppListMenuEntity();
        //                                entity.ParentId = parentMenuId;
        //                                adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppListMenuFields.MenuId == menuEntity.MenuId));
        //                            }
        //                        }
        //                    }

                          

        //                    var rootMenu = needToUpdateEntities.FirstOrDefault(o => !o.ParentId.HasValue && o.LinkType == (int)EmAppListMenuLinkType.ApplicationPackage);
        //                    if (rootMenu != null)
        //                    {
        //                        newInstallApplicationId = rootMenu.MenuId;

        //                        EntityCollection<AppApplicationAssetsItemEntity> needToSaveItemEntityList = new EntityCollection<AppApplicationAssetsItemEntity>();

        //                        foreach (var sourceAssetItemEntity in sourceAssetsItemEntityList)
        //                        {
        //                            var newItemDto = AppApplicationAssetsItemConverter.ConvertEntityToDto(sourceAssetItemEntity);
        //                            newItemDto.Id = null;
        //                            AppApplicationAssetsItemEntity newItemEntity = new AppApplicationAssetsItemEntity();
        //                            AppApplicationAssetsItemConverter.CopyDtoToEntity(newItemEntity, newItemDto);
        //                            newItemEntity.ApplicationId = newInstallApplicationId;
        //                            needToSaveItemEntityList.Add(newItemEntity);
        //                        }

        //                        adapter.SaveEntityCollection(needToSaveItemEntityList);

        //                    }

        //                    adapter.Commit();
        //                }
        //                catch (ORMQueryExecutionException ex)
        //                {
        //                    adapter.Rollback();
        //                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_InstallOneApplicationPackage_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

        //                }


        //                if (!validationResult.HasErrors)
        //                {
        //                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_InstallOneApplicationPackage_Save_OK", ValidationItemType.Message, "Application Added."));
        //                    aOperationCallResult.Object = true;


                          
        //                }
        //            }

        //        }
        //    }


        //    return aOperationCallResult;
        //}



        public static OperationCallResult<bool> DeleteOneApplicationPackage(int? packageMenuId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (packageMenuId.HasValue)
            {
                List<AppListMenuExDto> availableAppPackages = RetrieveSelectedApplicationPackages();
                AppListMenuExDto packageMenuExDto = availableAppPackages.FirstOrDefault(o => ((int)o.Id == packageMenuId.Value));

                if (packageMenuExDto != null)
                {
                    ObservableSet<AppListMenuExDto> sourceMenuTreeNode = new ObservableSet<AppListMenuExDto>() { packageMenuExDto };
                    ObservableSet<AppListMenuExDto> flatMenuList = new ObservableSet<AppListMenuExDto>();
                    AppTreeListMenuBL.ConvertMenuTreeToFlatList(sourceMenuTreeNode, flatMenuList);

                    List<int> deleteMenuIdList = flatMenuList.Select(o => (int)o.Id).ToList();

                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.DeleteEntitiesDirectly(typeof(AppSecurityRegDomainListMenuEntity), new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.MenuId == deleteMenuIdList));
                            adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserListMenuEntity), new RelationPredicateBucket(AppSecurityUserListMenuFields.MenuId == deleteMenuIdList));
                            adapter.DeleteEntitiesDirectly(typeof(AppListMenuEntity), new RelationPredicateBucket(AppListMenuFields.MenuId == deleteMenuIdList));
                            adapter.Commit();

                            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_DeleteOneApplicationPackage_Save_OK", ValidationItemType.Message, "Delete Successful"));
                            aOperationCallResult.Object = true;
                        }
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_DeleteOneApplicationPackage_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }

                }
            }

            return aOperationCallResult;
        }


        public static List<AppApplicationAssetsItemExDto> RetrieveAppApplicationAssetsItemDtoListByType(int applicationId, int? emAppApplicationAssetsType)
        {
            var aDtoList = new List<AppApplicationAssetsItemExDto>();

            if (emAppApplicationAssetsType.HasValue)
            {
                EntityCollection<AppTransactionEntity> allTransactionEntityList = new EntityCollection<AppTransactionEntity>();
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    RelationPredicateBucket aFilter = new RelationPredicateBucket();
                    IPrefetchPath2 root = new PrefetchPath2(EntityType.AppTransactionEntity);
                    //root.Add(AppTransactionEntity.PrefetchPathAppForm);
                    root.Add(AppTransactionEntity.PrefetchPathAppForm_);
                    root.Add(AppTransactionEntity.PrefetchPathAppForm);
                    //adapter.FetchEntityCollection(allTransactionEntityList, aFilter, root);

                    IncludeFieldsList includeFied = new IncludeFieldsList();
                    includeFied.Add(AppTransactionFields.TransactionId);
                    includeFied.Add(AppTransactionFields.FormId);
                    includeFied.Add(AppTransactionFields.MasterWorkflowId);
                    includeFied.Add(AppTransactionFields.TransactionName);
                    includeFied.Add(AppTransactionFields.TransactionOrganizedType);
                    includeFied.Add(AppFormFields.LayoutType);
                    adapter.FetchEntityCollection(allTransactionEntityList, aFilter, 0, null, root, includeFied);


                }

                EntityCollection<AppApplicationAssetsItemEntity> list = RetrieveAppApplicationAssetsItemEntityListByType(applicationId, emAppApplicationAssetsType);

                foreach (var aEntity in list)
                {
                    var aDto = AppApplicationAssetsItemConverter.ConvertEntityToExDto(aEntity);

                    if (aEntity.FormId.HasValue && aEntity.AppForm != null)
                    {
                        aDto.ForeignAppFormExDto = AppFormConverter.ConvertEntityToExDto(aEntity.AppForm);

                        AppTransactionEntity transactionEntity = allTransactionEntityList.FirstOrDefault(o => o.FormId.HasValue && o.FormId.Value == aEntity.FormId.Value);

                        if (transactionEntity != null)
                        {
                            aDto.Name = transactionEntity.TransactionName;
                            aDto.TransactionId = transactionEntity.TransactionId;
                            aDto.ProjectWorkflowId = transactionEntity.MasterWorkflowId;
                        }

                    }

                    if (aEntity.TransactionId.HasValue && aEntity.AppTransaction != null)
                    {
                        aDto.ForeignAppTransactionExDto = AppTransactionConverter.ConvertEntityToExDto(aEntity.AppTransaction);
                        aDto.Name = aDto.ForeignAppTransactionExDto.TransactionName;
                        aDto.FormId = aDto.ForeignAppTransactionExDto.FormId;
                        aDto.ProjectWorkflowId = aDto.ForeignAppTransactionExDto.MasterWorkflowId;

                        AppTransactionEntity transactionEntity = allTransactionEntityList.FirstOrDefault(o => o.TransactionId == aEntity.TransactionId.Value);
                        if (transactionEntity.AppForm_ != null)
                        {
                            aDto.ForeignAppFormExDto = AppFormConverter.ConvertEntityToExDto(transactionEntity.AppForm_);
                        }

                        if (transactionEntity.PrintFormId.HasValue)
                        {
                            aDto.PrintFormId = transactionEntity.PrintFormId;
                            if (transactionEntity.AppForm != null)
                            {
                                aDto.ForeignAppPrintFormExDto = AppFormConverter.ConvertEntityToExDto(transactionEntity.AppForm);
                            }
                        }
                    }

                    if (aEntity.ProjectWorkflowId.HasValue && aEntity.AppProjectOrWorkFlow != null)
                    {
                        aDto.ForeignAppProjectOrWorkFlowExDto = AppProjectOrWorkFlowConverter.ConvertEntityToExDto(aEntity.AppProjectOrWorkFlow);

                        AppTransactionEntity transactionEntity = allTransactionEntityList.FirstOrDefault(o => o.MasterWorkflowId.HasValue && o.MasterWorkflowId.Value == aEntity.ProjectWorkflowId.Value);

                        if (transactionEntity != null)
                        {
                            aDto.Name = transactionEntity.TransactionName;
                            aDto.FormId = transactionEntity.FormId;
                            aDto.TransactionId = transactionEntity.TransactionId;

                            if (transactionEntity.AppForm != null)
                            {
                                aDto.ForeignAppFormExDto = AppFormConverter.ConvertEntityToExDto(transactionEntity.AppForm);
                            }
                        }
                    }

                    if (aEntity.AppSearch != null)
                    {
                        aDto.ForeignAppSearchExDto = AppSearchConverter.ConvertEntityToExDto(aEntity.AppSearch);
                    }

                    if (aEntity.AppDesktop != null)
                    {
                        aDto.ForeignAppDesktopExDto = AppDesktopConverter.ConvertEntityToExDto(aEntity.AppDesktop);
                    }

                    if (aEntity.AppReport != null)
                    {
                        aDto.ForeignAppReportExDto = AppReportConverter.ConvertEntityToExDto(aEntity.AppReport);
                    }

                    aDtoList.Add(aDto);
                }

            }
            return aDtoList;
        }
            

        internal static bool SaveNewApplicationAssetsItemExDto(AppApplicationAssetsItemExDto aDto)
        {
            bool isSaveSuccess = false;
            if (aDto.ApplicationId.HasValue)
            {
                AppApplicationAssetsItemEntity entity = new AppApplicationAssetsItemEntity();
                AppApplicationAssetsItemConverter.CopyDtoToEntity(entity, aDto);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(entity);

                        adapter.Commit();
                        isSaveSuccess = true;
                    }


                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        isSaveSuccess = false;
                    }
                }
            }

            return isSaveSuccess;
        }

        public static OperationCallResult<AppListMenuExDto> SaveOneSaasApplicationSetting(AppListMenuExDto appPackage)
        {
            OperationCallResult<AppListMenuExDto> aOperationCallResult = new OperationCallResult<AppListMenuExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;
                       
            validationResult.Merge(AppTreeListMenuBL.SaveAppListMenuExDto_ProcessDirtyAppListMenuExDto(appPackage));

            AppListMenuExDto toReturnMenuExDto = AppTreeListMenuBL.RetrieveOneAppListMenuExDto(appPackage.Id);

            //if (appPackage.InstalledTransactionList != null)
            //{
            //    List<AppApplicationAssetsItemExDto> orgAssetList = RetrieveAppApplicationAssetsItemDtoListByType((int)appPackage.Id, null);

            //    ObservableSet<AppApplicationAssetsItemExDto> aSet = new ObservableSet<AppApplicationAssetsItemExDto>();

            //    foreach (AppTransactionDto transactionDto in appPackage.InstalledTransactionList)
            //    {
            //        var existTransAssetItem = orgAssetList.FirstOrDefault(o => o.TransactionId.HasValue && o.TransactionId.Value == (int)transactionDto.Id);
            //        if (existTransAssetItem == null)
            //        {
            //            AppApplicationAssetsItemExDto assetItemExDto = new AppApplicationAssetsItemExDto();
            //            assetItemExDto.ApplicationId = (int)appPackage.Id;
            //            assetItemExDto.TransactionId = (int)transactionDto.Id;
            //            aSet.Add(assetItemExDto);
            //        }      

            //        if (transactionDto.FormId.HasValue)
            //        {
            //            var existFormAssetItem = orgAssetList.FirstOrDefault(o => o.FormId.HasValue && o.FormId.Value == transactionDto.FormId.Value);
            //            if (existFormAssetItem == null)
            //            {
            //                AppApplicationAssetsItemExDto assetItemExDto = new AppApplicationAssetsItemExDto();
            //                assetItemExDto.ApplicationId = (int)appPackage.Id;
            //                assetItemExDto.FormId = transactionDto.FormId.Value;
            //                aSet.Add(assetItemExDto);
            //            }
            //        }

            //        if (transactionDto.MasterWorkflowId.HasValue)
            //        {
            //            var existWorkflowAssetItem = orgAssetList.FirstOrDefault(o => o.ProjectWorkflowId.HasValue && o.ProjectWorkflowId.Value == transactionDto.MasterWorkflowId.Value);
            //            if (existWorkflowAssetItem == null)
            //            {
            //                AppApplicationAssetsItemExDto assetItemExDto = new AppApplicationAssetsItemExDto();
            //                assetItemExDto.ApplicationId = (int)appPackage.Id;
            //                assetItemExDto.ProjectWorkflowId = transactionDto.MasterWorkflowId.Value;
            //                aSet.Add(assetItemExDto);
            //            }
            //        }
            //    }


            //    validationResult.Merge(SaveAppApplicationAssetsItemDtoList(aSet, (int)appPackage.Id, null).ValidationResult);
            //}

            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                aOperationCallResult.Object = toReturnMenuExDto;                
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppApplicationAssetsItemExDto> SaveAppApplicationAssetsItemDtoList(ObservableSet<AppApplicationAssetsItemExDto> aSet, int applicationId, int? emAppApplicationAssetsType)
        {
            OperationCallResult<AppApplicationAssetsItemExDto> aOperationCallResult = new OperationCallResult<AppApplicationAssetsItemExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            var entityList = RetrieveAppApplicationAssetsItemEntityListByType(applicationId, emAppApplicationAssetsType);
            Dictionary<int, AppApplicationAssetsItemEntity> dictDbAppApplicationAssetsItem = entityList.ToDictionary(o => o.AssetsItemId, o => o);
            Dictionary<int, AppApplicationAssetsItemExDto> dictDbAppApplicationAssetsItemDto = aSet.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppApplicationAssetsItem.Keys.ToList();

            List<int> groupIdUi = dictDbAppApplicationAssetsItemDto.Keys.ToList();


            //Delete Id
            List<int> deletAppApplicationAssetsItemIDs = groupIdDbms.Except(groupIdUi).ToList();


            //new Entity
            foreach (var dto in aSet)
            {
                if (dto.IsNew)
                {
                    AppApplicationAssetsItemEntity aParentAppApplicationAssetsItemEntity = new AppApplicationAssetsItemEntity();

                    AppApplicationAssetsItemConverter.CopyDtoToEntity(aParentAppApplicationAssetsItemEntity, dto);

                    entityList.Add(aParentAppApplicationAssetsItemEntity);

                }
                else // update 
                {
                    if (dictDbAppApplicationAssetsItem.ContainsKey((int)dto.Id))
                    {
                        var entity = dictDbAppApplicationAssetsItem[(int)dto.Id];

                        AppApplicationAssetsItemConverter.CopyDtoToEntity(entity, dto);

                    }

                }


            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntityCollection(entityList);

                    adapter.DeleteEntitiesDirectly(typeof(AppApplicationAssetsItemEntity), new RelationPredicateBucket(AppApplicationAssetsItemFields.AssetsItemId == deletAppApplicationAssetsItemIDs));

                    adapter.Commit();
                    validationResult.Items.Add(new ValidationItem(typeof(AppApplicationAssetsItemDto), "App_AppApplicationAssetsItem_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppApplicationAssetsItemDto), "App_AppApplicationAssetsItem_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }


            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppApplicationAssetsItemDto), "App_AppApplicationAssetsItem_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveAppApplicationAssetsItemDtoListByType(applicationId, emAppApplicationAssetsType);
            }

            return aOperationCallResult;
        }

        public static AppApplicationDataManipulationDto RetrieveAppApplicationDataManipulations(int applicationId)
        {
            var allApplicationTransactions = AppTransactionBL.RetrieveSaasApplicationTransactionList(applicationId);

            AppApplicationDataManipulationDto toReturn = new AppApplicationDataManipulationDto();
            toReturn.ApplicationId = applicationId;
            toReturn.PublishedFormList = new List<AppFormExDto>();
            toReturn.ListEditTransactionList = allApplicationTransactions.Where(o => o.Id !=null && o.TransactionOrganizedType.HasValue && o.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List).ToList();
            toReturn.MasterDetailTransactionList = allApplicationTransactions.Where(o => o.Id != null && o.TransactionOrganizedType.HasValue && (o.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)).ToList();
            toReturn.FolderNavigationList = new List<AppTransactionNavigationExDto>();
            toReturn.SearchNavigationList = new List<AppTransactionNavigationExDto>();
            toReturn.AllSearchDtoList = AppSearchConfigBL.RetrieveAllAppSearchDto();
            toReturn.ApplicationSearchDtoList = AppSearchConfigBL.RetrieveSaasApplicationSearchList(applicationId);



            
                


            List<AppApplicationAssetsItemExDto> transactionAssetsList = RetrieveAppApplicationAssetsItemDtoListByType(applicationId, (int)EmAppApplicationAssetsType.Transaction);

            foreach (AppApplicationAssetsItemExDto assetsItemExDto in transactionAssetsList)
            {
                if (assetsItemExDto.ForeignAppTransactionExDto != null)
                {
                    if (assetsItemExDto.ForeignAppFormExDto != null)
                    {
                        toReturn.PublishedFormList.Add(assetsItemExDto.ForeignAppFormExDto);
                    }

                    var transactionExDto = assetsItemExDto.ForeignAppTransactionExDto;

                    //if (transactionExDto.TransactionOrganizedType.HasValue)
                    //{
                    //    if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    //    {
                    //        toReturn.ListEditTransactionList.Add(transactionExDto);
                    //    }
                    //    else if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    //    {
                    //        toReturn.MasterDetailTransactionList.Add(transactionExDto);
                    //    }
                    //}
                }
            }

          


            List<int> masterDetailTransactionIdList = toReturn.MasterDetailTransactionList.Select(o => (int)o.Id).ToList();

            toReturn.SearchNavigationList = AppTransactionNavigationBL.RetrieveSearchNavigationExDtoListBytransactionId(masterDetailTransactionIdList).ToList();

            var folderViewList = AppTransactionNavigationBL.RetrieveFolderViewListBytransactionId(masterDetailTransactionIdList).ToList();

            List<int> folderViewTransIds = new List<int>();

            foreach (var folderView in folderViewList)
            {
                if (folderView.TransactionId.HasValue)
                {
                    if (!folderViewTransIds.Contains(folderView.TransactionId.Value))
                    {
                        folderViewTransIds.Add(folderView.TransactionId.Value);
                        toReturn.FolderNavigationList.Add(folderView);
                    }
                }
            }

            return toReturn;
        }

        private static EntityCollection<AppApplicationAssetsItemEntity> RetrieveOneApplicationAppApplicationAssetsItemEntityList(int applicationId, bool isRetriveFromMasterDB)
        {
            EntityCollection<AppApplicationAssetsItemEntity> list = new EntityCollection<AppApplicationAssetsItemEntity>();
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppApplicationAssetsItemEntity);
            IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppApplicationAssetsItemFields.ApplicationId == applicationId);

            if (isRetriveFromMasterDB)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {              
                    adapter.FetchEntityCollection(list, aFilter, rootPath);
                }
            }
            else
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(list, aFilter, rootPath);
                }
            }          
         
            return list;
        }


        internal static EntityCollection<AppApplicationAssetsItemEntity> RetrieveAppApplicationAssetsItemEntityListByType(int applicationId, int? emAppApplicationAssetsType)
        {
            EntityCollection<AppApplicationAssetsItemEntity> list = new EntityCollection<AppApplicationAssetsItemEntity>();
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppApplicationAssetsItemEntity);
            IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppApplicationAssetsItemFields.ApplicationId == applicationId);

          
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    if (emAppApplicationAssetsType.HasValue)
                    {
                        if (emAppApplicationAssetsType.Value == (int)EmAppApplicationAssetsType.Form)
                        {
                            rootPath.Add(AppApplicationAssetsItemEntity.PrefetchPathAppForm);
                            aFilter.PredicateExpression.AddWithAnd(AppApplicationAssetsItemFields.FormId != System.DBNull.Value);
                        }
                        else if (emAppApplicationAssetsType.Value == (int)EmAppApplicationAssetsType.Transaction)
                        {
                            rootPath.Add(AppApplicationAssetsItemEntity.PrefetchPathAppTransaction);
                            aFilter.Relations.Add(AppApplicationAssetsItemEntity.Relations.AppTransactionEntityUsingTransactionId);
                            aFilter.PredicateExpression.AddWithAnd(AppApplicationAssetsItemFields.TransactionId != System.DBNull.Value);
                            //aFilter.PredicateExpression.AddWithAnd(AppTransactionFields.IsPhysicalModelTableCreated == true);
                        }
                        else if (emAppApplicationAssetsType.Value == (int)EmAppApplicationAssetsType.Workflow)
                        {
                            rootPath.Add(AppApplicationAssetsItemEntity.PrefetchPathAppProjectOrWorkFlow);
                            aFilter.PredicateExpression.AddWithAnd(AppApplicationAssetsItemFields.ProjectWorkflowId != System.DBNull.Value);
                        }
                        else if (emAppApplicationAssetsType.Value == (int)EmAppApplicationAssetsType.Search)
                        {
                            rootPath.Add(AppApplicationAssetsItemEntity.PrefetchPathAppSearch);
                            aFilter.PredicateExpression.AddWithAnd(AppApplicationAssetsItemFields.SearchId != System.DBNull.Value);
                        }
                        else if (emAppApplicationAssetsType.Value == (int)EmAppApplicationAssetsType.Report)
                        {
                            rootPath.Add(AppApplicationAssetsItemEntity.PrefetchPathAppReport);
                            aFilter.PredicateExpression.AddWithAnd(AppApplicationAssetsItemFields.ReportId != System.DBNull.Value);
                        }
                        else if (emAppApplicationAssetsType.Value == (int)EmAppApplicationAssetsType.Dashboard)
                        {
                            rootPath.Add(AppApplicationAssetsItemEntity.PrefetchPathAppDesktop);
                            aFilter.PredicateExpression.AddWithAnd(AppApplicationAssetsItemFields.DesktopId != System.DBNull.Value);
                        }

                    }

                    adapter.FetchEntityCollection(list, aFilter, rootPath);
                }               
          

            return list;
        }



        private static List<AppListMenuExDto> ConvertListMenuEntityToApplicationPackageDtoList(EntityCollection<AppListMenuEntity> list)
        {
            List<AppListMenuExDto> allMenuDtos = new List<AppListMenuExDto>();

            foreach (var o in list)
            {
                AppListMenuExDto aAppListMenuDto = AppListMenuConverter.ConvertEntityToExDto(o);

                if (!string.IsNullOrEmpty(aAppListMenuDto.IconName))
                {
                    aAppListMenuDto.ImageUrl = aAppListMenuDto.IconName;
                }
                else
                {
                    aAppListMenuDto.ImageUrl = "Upload to the Cloud-64.png";
                }

                if (string.IsNullOrEmpty(aAppListMenuDto.Description))
                {
                    aAppListMenuDto.Description = aAppListMenuDto.Name;
                }

                allMenuDtos.Add(aAppListMenuDto);
            }

            List<AppListMenuExDto> rootMenus = new List<AppListMenuExDto>();

            allMenuDtos.Where(f => !f.ParentId.HasValue && f.LinkType == (int)EmAppListMenuLinkType.ApplicationPackage).ForAll(o => rootMenus.Add(o));

            foreach (var aRootMenu in rootMenus)
            {
                AppTreeListMenuBL.ProcessChilds(allMenuDtos, aRootMenu);
            }

            return rootMenus;
        }

        public static OperationCallResult<int?> CreateMyNewApplicationPackage(AppListMenuExDto packageMenuExDto)
        {
            OperationCallResult<int?> aOperationCallResult = new OperationCallResult<int?>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (packageMenuExDto != null)
            {
                List<AppListMenuExDto> installedPackages = RetrieveSelectedApplicationPackages();

                int maxSort = 0;
                if (installedPackages != null && installedPackages.Count > 0)
                { 
                    maxSort = installedPackages.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value);
                }

                AppListMenuEntity newPackage = new AppListMenuEntity();
                AppListMenuConverter.CopyDtoToEntity(newPackage, packageMenuExDto);
                newPackage.IsNew = true;
                newPackage.Sort = maxSort + 1;
                newPackage.LinkType = (int)EmAppListMenuLinkType.ApplicationPackage;
                newPackage.GlobalGuid = Guid.NewGuid();
                newPackage.EmDeviceMenuShowMode = 3;
                newPackage.EmAppMenuItemCategory = (int)EmAppMenuItemCategory.ManagementPage;

                //AppListMenuEntity firstPage = new AppListMenuEntity();
                //firstPage.IsNew = true;
                //firstPage.Name = "My First Page";
                //firstPage.Description = "Description";
                //firstPage.Sort = 1;
                //firstPage.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                //firstPage.GlobalGuid = Guid.NewGuid();
                //newPackage.AppListMenu_.Add(firstPage);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(newPackage);

                        adapter.Commit();

                        AppListMenuUserAndDomainBL.SaveNewRoleMenusSecurity((int)EmAppBuiltInUserGroup.CompanyAdmin, new List<int>() { newPackage.MenuId });


                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_CreateMyNewApplicationPackage_Save_OK", ValidationItemType.Message, "Application Added."));
                        aOperationCallResult.Object = newPackage.MenuId;
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_CreateMyNewApplicationPackage_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    }
                }


            }


            return aOperationCallResult;
        }
    }
}
