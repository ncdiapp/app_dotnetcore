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
//    

using APP.Framework;
namespace App.BL
{
    public static class AppTreeListMenuBL
    {

        private static readonly string _mySearchMenuGroupName = "My Search";
        private static readonly string _fileManagementMenuGroupName = "File Management";
        private static readonly string _imageManagementGroupName = "Image Management";


        public static readonly string AppMasterDBConnectionString = AppConfig.GetConnectionString("AppMasterDBConnectionString") ?? string.Empty;

        public static ObservableSet<AppListMenuExDto> RetrieveNoneMgtUserTreeMenu(int? siteId, int? siteMenuCategory)
        {

            EntityCollection<AppListMenuEntity> userMeneEntityList = new EntityCollection<AppListMenuEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppListMenuFields.EsiteId == siteId &  AppListMenuFields.EmAppMenuItemCategory == siteMenuCategory );
                adapter.FetchEntityCollection(userMeneEntityList, aFilter);
            }


            var allTreeMenus = ConvertMenuEnityListToHairarchyList(null, false, userMeneEntityList);



            return allTreeMenus;
        }

        public static ObservableSet<AppListMenuExDto> RetrieveListMenuHairarchyDto(bool isWebPageMenu = false, int? rootMenuId = null, bool isFilterDisabledMenu = false)
        {
            EntityCollection<AppListMenuEntity> list = RetrieveMgtAllFlatListMenuEntity(isWebPageMenu, false);

            ObservableSet<AppListMenuExDto> rootMenus = ConvertMenuEnityListToHairarchyList(rootMenuId, isFilterDisabledMenu, list);

            return rootMenus;

        }

        internal static ObservableSet<AppListMenuExDto> ConvertMenuEnityListToHairarchyList(int? rootMenuId, bool isFilterDisabledMenu, EntityCollection<AppListMenuEntity> list)
        {
            var allMenuItems = new ObservableSet<AppListMenuExDto>();

            foreach (var o in list)
            {
                if (isFilterDisabledMenu)
                {
                    if (o.EmDeviceMenuShowMode.HasValue && o.EmDeviceMenuShowMode.Value == (int)EmAppDeviceMenuShowMode.Disable)
                    {
                        continue;
                    }
                }

                AppListMenuExDto aAppListMenuDto = AppListMenuConverter.ConvertEntityToExDto(o);
                if (!string.IsNullOrEmpty(aAppListMenuDto.IconName))
                {

                    aAppListMenuDto.ImageUrl = aAppListMenuDto.IconName;

                }
                else
                {
                    aAppListMenuDto.ImageUrl = "Upload to the Cloud-64.png";
                }

                allMenuItems.Add(aAppListMenuDto);
            }

            ObservableSet<AppListMenuExDto> rootMenus = new ObservableSet<AppListMenuExDto>();

            allMenuItems.Where(f => !f.ParentId.HasValue).ForAll(o =>
            {
                if (rootMenuId.HasValue)
                {
                    if ((int)o.Id == rootMenuId.Value)
                    {
                        rootMenus.Add(o);
                    }
                }
                else
                {
                    rootMenus.Add(o);
                }

            });

            foreach (var rootMenu in rootMenus)
            {
                rootMenu.MenuPath = rootMenu.Name;
                ProcessChilds(allMenuItems, rootMenu);
            }

            return rootMenus;
        }

        private static AppListMenuEntity RetrieveOneAppListMenuEntity(object menuId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppListMenuEntity entity = new AppListMenuEntity(int.Parse(menuId.ToString()));
                adpater.FetchEntity(entity);
                return entity;
            }
        }


        public static AppListMenuExDto RetrieveOneAppListMenuExDto(object menuId)
        {
            AppListMenuEntity entity = RetrieveOneAppListMenuEntity(menuId);
            AppListMenuExDto aExDto = AppListMenuConverter.ConvertEntityToExDto(entity);

            return aExDto;
        }


        public static OperationCallResult<AppListMenuExDto> SaveOneAppListMenuTreeNode(AppListMenuExDto aAppListMenuExDto)
        {
            OperationCallResult<AppListMenuExDto> aOperationCallResult = new OperationCallResult<AppListMenuExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppListMenuEntity aAppListMenuEntity;
                     

            // Save New
            if (aAppListMenuExDto.IsNew)
            {
                aAppListMenuEntity = new AppListMenuEntity();
                AppListMenuConverter.CopyDtoToEntity(aAppListMenuEntity, aAppListMenuExDto);

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppListMenuEntity);
                        adapter.Commit();

                        aAppListMenuExDto.Id = aAppListMenuEntity.MenuId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                        AppListMenuUserAndDomainBL.SaveNewRoleMenusSecurity((int)EmAppBuiltInUserGroup.CompanyAdmin, new List<int>() { aAppListMenuEntity.MenuId });
                                              
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            // Save Dirty
            else if (aAppListMenuExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(SaveAppListMenuExDto_ProcessDirtyAppListMenuExDto(aAppListMenuExDto));
            }

            // if no any errors, refresh from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppListMenuExDto(aAppListMenuExDto.Id);
            }

            return aOperationCallResult;
        }





        public static OperationCallResult<AppListMenuExDto> SaveAllTreeListMenuDto(ObservableSet<AppListMenuExDto> aSet, bool isWebPageMenu = false)
        {
            OperationCallResult<AppListMenuExDto> aOperationCallResult = new OperationCallResult<AppListMenuExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            ObservableSet<AppListMenuExDto> menuList = new ObservableSet<AppListMenuExDto>();

            ConvertMenuTreeToFlatList(aSet, menuList);

            menuList.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(SaveAppListMenuExDto_ProcessDirtyAppListMenuExDto(o)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                aOperationCallResult.ObjectList = RetrieveListMenuHairarchyDto(isWebPageMenu);
            }

            return aOperationCallResult;


        }


        public static OperationCallResult<object> AddSearchToMainMenu(int? searchOrSavedSearchId, string menuName, bool isSavedSearch = false)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (searchOrSavedSearchId.HasValue)
            {
                ObservableSet<AppListMenuExDto> menuTree = RetrieveListMenuHairarchyDto();
                AppListMenuExDto mySearchMenuGroup = menuTree.FirstOrDefault(o => o.Description == _mySearchMenuGroupName && !o.ParentId.HasValue && o.LinkType == (int)EmAppListMenuLinkType.SystemPage);

                if (mySearchMenuGroup == null)
                {
                    var newMenuGroup = new AppListMenuExDto();
                    newMenuGroup.Name = _mySearchMenuGroupName;
                    newMenuGroup.Description = _mySearchMenuGroupName;
                    newMenuGroup.LinkType = (int)EmAppListMenuLinkType.SystemPage;

                    if (menuTree.Where(o => o.Sort.HasValue).Count() == 0)
                    {
                        newMenuGroup.Sort = 10;
                    }
                    else
                    {
                        newMenuGroup.Sort = menuTree.Where(o => o.Sort.HasValue).Max(p => p.Sort.Value) + 10;
                    }
                    newMenuGroup.AppListMenu_List = new ObservableSet<AppListMenuExDto>();

                    OperationCallResult<AppListMenuExDto> saveMySearchGroupMenuResult = SaveOneAppListMenuTreeNode(newMenuGroup);

                    if (saveMySearchGroupMenuResult.IsSuccessfulWithResult)
                    {
                        mySearchMenuGroup = saveMySearchGroupMenuResult.Object;
                    }
                    else
                    {
                        aValidationResult.Merge(saveMySearchGroupMenuResult.ValidationResult);
                    }

                }

                if (!aValidationResult.HasErrors && mySearchMenuGroup != null && mySearchMenuGroup.Id != null)
                {

                    AppListMenuExDto newSearchMenu = new AppListMenuExDto();

                    var existingSearchMenus = mySearchMenuGroup.AppListMenu_List.Where(o => o.Sort.HasValue).ToList();
                    if (existingSearchMenus.Count() == 0)
                    {
                        newSearchMenu.Sort = 10;
                    }
                    else
                    {
                        newSearchMenu.Sort = existingSearchMenus.Max(p => p.Sort.Value) + 10;
                    }

                    newSearchMenu.Name = menuName;
                    newSearchMenu.RouteCode = ListMenus.MasterDataManagement;
                    newSearchMenu.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                    newSearchMenu.Link = searchOrSavedSearchId.Value.ToString();

                    if (!isSavedSearch)
                    {
                        newSearchMenu.Link = searchOrSavedSearchId.Value.ToString();
                    }
                    else
                    {
                        newSearchMenu.Link = searchOrSavedSearchId.Value.ToString() + StringHelper.UnderscoreToken + 1.ToString();
                    }


                    newSearchMenu.ParentId = (int)mySearchMenuGroup.Id;


                    OperationCallResult<AppListMenuExDto> saveNewSearchMenuResult = SaveOneAppListMenuTreeNode(newSearchMenu);

                    if (saveNewSearchMenuResult.IsSuccessfulWithResult)
                    {
                        aOperationCallResult.Object = true;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Menu Successfully"));
                    }
                    else
                    {
                        aValidationResult.Merge(saveNewSearchMenuResult.ValidationResult);
                    }

                }
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<object> AddListTransactionToMainMenu(int? transactionId, string menuName)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (transactionId.HasValue)
            {
                // New menu should always attach directly under the current transaction's application root.
                AppTransactionEntity transactionEntity = AppTransactionBL.RetrieveOneAppTransactionEntity(transactionId.Value);
                if (transactionEntity == null || !transactionEntity.SaasApplicationId.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(
                        typeof(AppListMenuEntity),
                        "App_ListMenuEntity_CannotFindTransactionApplicationId_Error",
                        ValidationItemType.Error,
                        "Cannot find application id for the current transaction."));
                    return aOperationCallResult;
                }

                int applicationId = transactionEntity.SaasApplicationId.Value;

                ObservableSet<AppListMenuExDto> menuTree = RetrieveListMenuHairarchyDto(false, applicationId);
                AppListMenuExDto applicationRootMenu = menuTree.FirstOrDefault();
                if (applicationRootMenu == null || applicationRootMenu.Id == null)
                {
                    aValidationResult.Items.Add(new ValidationItem(
                        typeof(AppListMenuEntity),
                        "App_ListMenuEntity_CannotFindApplicationMenuRoot_Error",
                        ValidationItemType.Error,
                        "Cannot find main menu root for the current application."));
                    return aOperationCallResult;
                }

                string linkValue = transactionId.Value.ToString();

                string routeCode;
              
                routeCode = ListMenus.FormListEdit;
               

                // Don't create duplicate menu node under the same parent:
                // match (RouteCode, Link) under ParentId=applicationId.
                ObservableSet<AppListMenuExDto> allChildren = applicationRootMenu.AppListMenu_List ?? new ObservableSet<AppListMenuExDto>();
                bool alreadyExists = allChildren.Any(o => o.RouteCode == routeCode && o.Link == linkValue);
                if (alreadyExists)
                {
                    aValidationResult.Items.Add(new ValidationItem(
                        typeof(AppListMenuEntity),
                        "App_ListMenuEntity_MenuAlreadyExists_Warning",
                        ValidationItemType.Warning,
                        "Main menu shortcut already exists for this transaction."));
                    return aOperationCallResult;
                }

                AppListMenuExDto newSearchMenu = new AppListMenuExDto();

                var existingSiblings = allChildren
                    .Where(o => o.Sort.HasValue)
                    .ToList();

                newSearchMenu.Sort = existingSiblings.Count == 0 ? 10 : existingSiblings.Max(p => p.Sort.Value) + 10;

                newSearchMenu.Name = menuName;
                
                // Match standard main-menu shortcut behavior.
                newSearchMenu.EmDeviceMenuShowMode = 3;
                newSearchMenu.EmAppMenuItemCategory = 1;

                newSearchMenu.RouteCode = routeCode;

                newSearchMenu.LinkType = (int)EmAppListMenuLinkType.SystemPage;
                newSearchMenu.Link = linkValue;

                newSearchMenu.ParentId = applicationId;

                OperationCallResult<AppListMenuExDto> saveNewSearchMenuResult = SaveOneAppListMenuTreeNode(newSearchMenu);

                if (saveNewSearchMenuResult.IsSuccessfulWithResult)
                {
                    aOperationCallResult.Object = true;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Menu Successfully"));
                }
                else
                {
                    aValidationResult.Merge(saveNewSearchMenuResult.ValidationResult);
                }
            }

            return aOperationCallResult;
        }




        //internal static ValidationResult CopyListMenuStructure(object sourceMenuId, object targetMenuId)
        //{
        //    ValidationResult aValidationResult = new ValidationResult();

        //    AppListMenuEntity sourceEntity = RetrieveOneAppListMenuEntity(sourceMenuId);


        //    EntityCollection<AppListMenuEntity> needCopyEntities = new EntityCollection<AppListMenuEntity>();
        //    EntityCollection<AppListMenuEntity> allFolderEntity = PdmSecurityManagementBL.RetrieveCurrentUserAllFolderEntity((EmFolderType)sourceEntity.FolderType);

        //    List<FolderCopyHelper> folderCopyHelperList = new List<FolderCopyHelper>();
        //    folderCopyHelperList.Add(new FolderCopyHelper(sourceEntity.FolderId, targetEntity.FolderId));

        //    if (sourceEntity.FolderType == targetEntity.FolderType)
        //    {
        //        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //        {
        //            try
        //            {
        //                adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

        //                //organize the need copy entities
        //                needCopyEntities.Add(sourceEntity);

        //                if (isCopyChildFolderStructure)
        //                {
        //                    RetrieveChildFolders(sourceEntity, allFolderEntity, needCopyEntities);
        //                }

        //                //get all the folderId and parentId
        //                needCopyEntities.ForAll(o =>
        //                {
        //                    if (!folderCopyHelperList.Any(obj => obj.FolderId == o.FolderId))
        //                    {
        //                        folderCopyHelperList.Add(new FolderCopyHelper(o.FolderId, o.ParentId.Value));
        //                    }
        //                });

        //                //execute copy

        //                Dictionary<int, List<PdmSefolderResourceDto>> dictSourceFolderIdAndSecurityList = new Dictionary<int, List<PdmSefolderResourceDto>>();

        //                if (isCopyFolderWithSecurity)
        //                {
        //                    List<int> folderIdList = needCopyEntities.Select(o => o.FolderId).ToList();
        //                    ObservableSet<PdmSefolderResourceDto> sourceFolderSecurityList = RetrieveMultiplePdmSefolderSecurity(folderIdList.ToArray());

        //                    foreach (PdmSefolderResourceDto aResourceDto in sourceFolderSecurityList)
        //                    {
        //                        if (dictSourceFolderIdAndSecurityList.ContainsKey(aResourceDto.FolderId))
        //                        {
        //                            dictSourceFolderIdAndSecurityList[aResourceDto.FolderId].Add(aResourceDto);
        //                        }
        //                        else
        //                        {
        //                            dictSourceFolderIdAndSecurityList.Add(aResourceDto.FolderId, new List<PdmSefolderResourceDto>());
        //                            dictSourceFolderIdAndSecurityList[aResourceDto.FolderId].Add(aResourceDto);
        //                        }
        //                    }

        //                }

        //                needCopyEntities.ForAll(o =>
        //                {
        //                    CopyPdmSeFolder(o, folderCopyHelperList, adapter, newFolderNamePostFix, isCopyFolderWithSecurity, dictSourceFolderIdAndSecurityList);
        //                });

        //                // needCopyEntities

        //                adapter.Commit();
        //            }

        //            // Entity Logical Validation Exception
        //            catch (ORMEntityValidationException ex)
        //            {
        //                adapter.Rollback();
        //                aValidationResult.AddItem(typeof(AppListMenuEntity), "plm_AppListMenuEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString());
        //            }

        //            // Database FK Exception .......
        //            catch (ORMQueryExecutionException ex)
        //            {
        //                adapter.Rollback();
        //                aValidationResult.AddItem(typeof(AppListMenuEntity), "plm_AppListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString());
        //            }
        //        }

        //        return aValidationResult;
        //    }

        //    else
        //    {
        //        const string referMsg = "Cannot copy the folder into another folder of different type.";
        //        aValidationResult.AddItem(typeof(AppListMenuEntity), "plm_AppListMenuEntity_CannotCopyDifferntTypeOfFolders", ValidationItemType.Error, referMsg);

        //        return aValidationResult;
        //    }
        //}


        private static AppListMenuExDto[] GetChilds(IEnumerable<AppListMenuExDto> allListMenuItems, AppListMenuExDto appListMenuExDto)
        {
            return allListMenuItems.Where(f => f.ParentId == (int)appListMenuExDto.Id).ToArray();
        }

        internal static void ProcessChilds(IEnumerable<AppListMenuExDto> allListMenuItems, AppListMenuExDto appListMenuExDto)
        {
            ObservableSet<AppListMenuExDto> children = new ObservableSet<AppListMenuExDto>();

            GetChilds(allListMenuItems, appListMenuExDto).OrderBy(f => f.Name).ForAll(o => { 
                children.Add(o);
                o.MenuPath = appListMenuExDto.MenuPath + " / " + o.Name;
            });

            if (!children.IsEmpty())
            {
                appListMenuExDto.AppListMenu_List = children;
                appListMenuExDto.AppListMenu_List.ForAll(c => ProcessChilds(allListMenuItems, c));

            }
        }

        internal static void ConvertMenuTreeToFlatList(ObservableSet<AppListMenuExDto> menuTree, ObservableSet<AppListMenuExDto> menuList)
        {
            if (menuTree != null && menuList != null)
            {
                foreach (var aMenuDto in menuTree)
                {
                    menuList.Add(aMenuDto);
                    ConvertMenuTreeToFlatList(aMenuDto.AppListMenu_List, menuList);
                }
            }
        }

        internal static ValidationResult SaveAppListMenuExDto_ProcessDirtyAppListMenuExDto(AppListMenuExDto aAppListMenuExDto)
        {
            int folderId = int.Parse(aAppListMenuExDto.Id.ToString());

            ValidationResult aValidationResult = new ValidationResult();

            AppListMenuEntity aAppListMenuEntity = RetrieveOneAppListMenuEntity(folderId);

            AppListMenuConverter.CopyDtoToEntity(aAppListMenuEntity, aAppListMenuExDto);
                        
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppListMenuEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<object> DeleteOneAppListMenuTreeNode(object menuId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    var folderEntity = RetrieveOneAppListMenuEntity(menuId);

                    var childCollection = new EntityCollection<AppListMenuEntity>();
                    IRelationPredicateBucket childFilter = new RelationPredicateBucket(AppListMenuFields.ParentId == menuId);
                    adapter.FetchEntityCollection(childCollection, childFilter);

                    if (childCollection.Count > 0)
                    {
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_thismenuhaschildmenus", ValidationItemType.Error, "This Menu Has Child Menus."));
                        return aOperationCallResult;
                    }

                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityRegDomainListMenuEntity), new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.MenuId == menuId));
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserListMenuEntity), new RelationPredicateBucket(AppSecurityUserListMenuFields.MenuId == menuId));
                    adapter.DeleteEntitiesDirectly(typeof(AppListMenuEntity), new RelationPredicateBucket(AppListMenuFields.MenuId == menuId));
                    adapter.Commit();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Delete_OK", ValidationItemType.Message, "Delete Successful"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aOperationCallResult.ValidationResult.HasErrors)
                {
                    aOperationCallResult.Object = menuId;
                }
            }

            return aOperationCallResult;
        }

        internal static EntityCollection<AppListMenuEntity> RetrieveMgtAllFlatListMenuEntity(bool isWebPageMenu, bool isRetriveFromMasterDB)
        {
            // isRetriveFromMasterDB=true means tenant's master DB (IsCompanyMasterDb=true).
            // For SysAdmin, CurrentUserDbConnectionString == AppMasterDBConnectionString (set at login).
            // Never use the raw AppMasterDBConnectionString here — AppListMenu does not exist in the hosting company DB.
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                return RetrieveMgtAllFlatListMenuEntityByAdapter(isWebPageMenu, adapter);
            }
        }

        private static EntityCollection<AppListMenuEntity> RetrieveMgtAllFlatListMenuEntityByAdapter(bool isWebPageMenu, DataAccessAdapter adapter)
        {
            EntityCollection<AppListMenuEntity> list = new EntityCollection<AppListMenuEntity>();
            SortClause aSortClause = AppListMenuFields.Sort | SortOperator.Ascending;// new SortClause(AppListMenuFields.Sort | SortOperator.Ascending);

            IPrefetchPath2 root = new PrefetchPath2(EntityType.AppListMenuEntity);
            root.Add(AppListMenuEntity.PrefetchPathAppListMenu_).Sorter.Add(aSortClause);

            IRelationPredicateBucket aFilter = new RelationPredicateBucket();
            if (isWebPageMenu)
            {
                aFilter.PredicateExpression.AddWithAnd(AppListMenuFields.LinkType == (int)EmAppListMenuLinkType.WebPage);
            }
            else
            {
                aFilter.PredicateExpression.AddWithAnd(AppListMenuFields.LinkType != (int)EmAppListMenuLinkType.WebPage);
            }

            aFilter.PredicateExpression.AddWithAnd(AppListMenuFields.EmAppMenuItemCategory == (int)EmAppMenuItemCategory.ManagementPage);
            adapter.FetchEntityCollection(list, aFilter, 0, new SortExpression(aSortClause), root);

            return list;
        }

    }
}
