using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif

using APP.Framework;
namespace App.BL
{
    public static class AppDesktopBL
    {
        public static readonly string App_DesktopEntity_Save_OK = "App_DesktopEntity_Save_OK";
        public static readonly string App_DesktopEntity_Save_Failed = "App_DesktopEntity_Save_Failed";
        public static readonly string App_DesktopEntityUILayout_Save_OK = "App_DesktopEntityUILayout_Save_OK";
        public static readonly string App_DesktopEntityUILayout_Save_Failed = "App_DesktopEntityUILayout_Save_Failed";
        public static readonly string App_DesktopEntity_Delete_Ok = "App_DesktopEntity_Delete_Ok";



        public static readonly string App_DesktopEntity_Delete_Failed = "App_DesktopEntity_Delete_Failed";

        public static List<AppDesktopDto> RetrieveAllAppDesktopDto(bool isIncludeUserDefaultDesktops = true)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDesktopEntity> list = new EntityCollection<AppDesktopEntity>();

                SortExpression expression = new SortExpression(AppDesktopFields.DesktopName | SortOperator.Ascending);
                adapter.FetchEntityCollection(list, null, 0, expression);

                var aDtoList = new List<AppDesktopDto>();

                foreach (var o in list)
                {
                    var dto = AppDesktopConverter.ConvertEntityToDto(o);
                    dto.IsGlobalDefault = dto.IsGlobalDefault.HasValue ? dto.IsGlobalDefault.Value : false;

                    if (dto.OtherSettingsDto == null)
                    {
                        dto.OtherSettingsDto = new AppDesktopOtherSettingsDto();
                    }

                    if (isIncludeUserDefaultDesktops)
                    {
                        aDtoList.Add(dto);
                    }
                    else
                    {
                        if (!(dto.OtherSettingsDto != null && dto.OtherSettingsDto.IsUserDesktop))
                        {
                            aDtoList.Add(dto);
                        }

                    }
                }

                aDtoList = aDtoList.OrderByDescending(o => o.IsGlobalDefault).ThenBy(o => o.OtherSettingsDto.IsUserDesktop).ThenBy(o => o.DesktopName).ToList();

                return aDtoList;
            }
        }

        public static List<AppDesktopDto> RetrieveSaasDesktopDtoList(int? applicationId)
        {
            List<AppDesktopDto> allDesktops = RetrieveAllAppDesktopDto().ToList();

            if (applicationId.HasValue)
            {
                return allDesktops.Where(o => o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value).ToList();
            }
            else
            {
                return allDesktops;
            }
        }

        public static AppDesktopExDto RetrieveOneAppDesktopExDto(object desktopId)
        {
            AppDesktopEntity aAppDesktopEntity = RetrieveOneAppDesktopEntity(desktopId);
            AppDesktopExDto adesktopDto = AppDesktopConverter.ConvertEntityToExDto(aAppDesktopEntity);

            Dictionary<int, AppListMenuExDto> dictListMenuIdAndDto = new Dictionary<int, AppListMenuExDto>();

            var menuList = AppSecurityManagementBL.RetrieveUserMenuFlatList();
            foreach (var menu in menuList)
            {
                dictListMenuIdAndDto.Add((int)menu.Id, menu);
            }


            foreach (var o in aAppDesktopEntity.AppDesktopItem)
            {
                AppDesktopItemExDto aAppDesktopKeyExDto = AppDesktopItemConverter.ConvertEntityToExDto(o);

                if (aAppDesktopKeyExDto.WidgetItemType.HasValue &&
                    (
                        aAppDesktopKeyExDto.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.InternalShortcut
                    || aAppDesktopKeyExDto.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.InternalPage
                    ))
                {
                    int? menuId = ControlTypeValueConverter.ConvertValueToInt(aAppDesktopKeyExDto.ParameterKeyValue);
                    if (menuId.HasValue && dictListMenuIdAndDto != null && dictListMenuIdAndDto.ContainsKey(menuId.Value))
                    {
                        aAppDesktopKeyExDto.LinkToListMenu = dictListMenuIdAndDto[menuId.Value];
                        if (aAppDesktopKeyExDto.LinkToListMenu != null && string.IsNullOrEmpty(aAppDesktopKeyExDto.LinkToListMenu.ImageUrl))
                        {
                            aAppDesktopKeyExDto.LinkToListMenu.ImageUrl = ImageLibraryDto.DefaultIconName64;
                        }
                    }
                }
                adesktopDto.AppDesktopItemList.Add(aAppDesktopKeyExDto);
            }


            return adesktopDto;
        }


        //public static void ConvertFlexDesktopItemsFromHierachyTreeToFlatList(AppDesktopExDto desktopExDto)
        //{
        //    List<AppDesktopItemExDto> orgDesktopItemList = desktopExDto.AppDesktopItemList.ToList();

        //    desktopExDto.AppDesktopItemList = new ObservableSet<AppDesktopItemExDto>();

        //    foreach (AppDesktopItemExDto desktopItemExDto in orgDesktopItemList.OrderBy(o => o.FlowOrGridLayoutSortOrder))
        //    {
        //        desktopExDto.AppDesktopItemList.Add(desktopItemExDto);

        //        ConvertFlexDesktopItemsFromFlatListToHierachyTree_ProcessChildItems(orgDesktopItemList, desktopItemExDto);

        //    }

        //}

        //private static void ConvertFlexDesktopItemsFromHierachyTreeToFlatList_ProcessChildItems(List<AppDesktopItemExDto> orgDesktopItemList, AppDesktopItemExDto desktopItemExDto)
        //{
        //    desktopItemExDto.ChildDesktopItems = new List<AppDesktopItemExDto>();

        //    foreach (AppDesktopItemExDto desktopItemExDto in orgDesktopItemList.OrderBy(o => o.FlowOrGridLayoutSortOrder))
        //    {
        //        desktopExDto.AppDesktopItemList.Add(desktopItemExDto);

        //        ConvertFlexDesktopItemsFromFlatListToHierachyTree_ProcessChildItems(orgDesktopItemList, desktopItemExDto);

        //    }
        //}


        //public static void ConvertFlexDesktopItemsFromFlatListToHierachyTree(AppDesktopExDto desktopExDto)
        //{
        //    List<AppDesktopItemExDto> orgDesktopItemList = desktopExDto.AppDesktopItemList.ToList();

        //    desktopExDto.AppDesktopItemList = new ObservableSet<AppDesktopItemExDto>();

        //    foreach (AppDesktopItemExDto desktopItemExDto in orgDesktopItemList.Where(o => !o.GridLayoutParentId.HasValue).OrderBy(o => o.FlowOrGridLayoutSortOrder))
        //    {
        //        desktopExDto.AppDesktopItemList.Add(desktopItemExDto);                

        //        ConvertFlexDesktopItemsFromFlatListToHierachyTree_ProcessChildItems(orgDesktopItemList, desktopItemExDto);

        //    }

        //}

        //private static void ConvertFlexDesktopItemsFromFlatListToHierachyTree_ProcessChildItems(List<AppDesktopItemExDto> orgDesktopItemList, AppDesktopItemExDto desktopItemExDto)
        //{
        //    desktopItemExDto.ChildDesktopItems = new List<AppDesktopItemExDto>();

        //    foreach (AppDesktopItemExDto childItemdto in orgDesktopItemList.Where(o => o.GridLayoutParentId.HasValue && o.GridLayoutParentId.Value == (int)desktopItemExDto.Id))
        //    {               
        //        desktopItemExDto.ChildDesktopItems.Add(childItemdto);

        //        ConvertFlexDesktopItemsFromFlatListToHierachyTree_ProcessChildItems(orgDesktopItemList, childItemdto);
        //    }
        //}

        public static AppDesktopEntity RetrieveOneAppDesktopEntity(object desktopId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppDesktopEntity desktopEntity = new AppDesktopEntity(int.Parse(desktopId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppDesktopEntity);




                rootPath.Add(AppDesktopEntity.PrefetchPathAppDesktopItem);

                adpater.FetchEntity(desktopEntity, rootPath);
                return desktopEntity;
            }
        }

        public static OperationCallResult<AppDesktopExDto> CreateUserDefaultDesktop()
        {
            OperationCallResult<AppDesktopExDto> aOperationCallResult = new OperationCallResult<AppDesktopExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            //if (!AppSecurityUserBL.CurrentUserEntity.DefaultDesktopId.HasValue)
            //{
            AppDesktopExDto aAppDesktopExDto = new AppDesktopExDto();
            aAppDesktopExDto.DesktopName = "My Default Dashboard";
            aAppDesktopExDto.LayoutType = (int)EmAppDesktopLayoutType.Flex;
            aAppDesktopExDto.OtherSettingsDto = new AppDesktopOtherSettingsDto();
            aAppDesktopExDto.OtherSettingsDto.IsUserDesktop = true;
            //aAppDesktopExDto.OtherSettingsDto.UserDesktopUserId = AppSecurityUserBL.CurrentUserId;
            aAppDesktopExDto.OtherSettingsDto.FlexLayoutItems = new List<AppDesktopItemExDto>();
            aAppDesktopExDto.OtherSettingsDto.DefaultNumberOfColumns = 4;

            var saveDesktopResult = SaveAppDesktopExDto(aAppDesktopExDto);

            aOperationCallResult.ValidationResult.Merge(saveDesktopResult.ValidationResult);

            if (saveDesktopResult.IsSuccessfulWithResult)
            {
                int desktopId = (int)saveDesktopResult.Object.Id;

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(AppSecurityUserBL.CurrentUserId);
                        userEntity.DefaultDesktopId = desktopId;

                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(userEntity);
                        adapter.Commit();

                        AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache(AppSecurityUserBL.CurrentUserId);
                    }

                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            return saveDesktopResult;
            //}
            //else
            //{
            //    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_CreateUserDefaultDesktop_Error", ValidationItemType.Error, "The user already has a default dashboard."));
            //}

            //return aOperationCallResult;
        }

        public static OperationCallResult<AppDesktopExDto> SaveAppDesktopExDto(AppDesktopExDto aAppDesktopExDto)
        {
            OperationCallResult<AppDesktopExDto> aOperationCallResult = new OperationCallResult<AppDesktopExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDesktopEntity aAppDesktopEntity;

            // prepare Data
            if (aAppDesktopExDto.IsNew)
            {
                aAppDesktopEntity = new AppDesktopEntity();
                AppDesktopConverter.CopyDtoToEntity(aAppDesktopEntity, aAppDesktopExDto);



                foreach (var templatefieldDto in aAppDesktopExDto.AppDesktopItemList)
                {
                    AppDesktopItemEntity aAppDesktopItemEntity = new AppDesktopItemEntity();
                    AppDesktopItemConverter.CopyDtoToEntity(aAppDesktopItemEntity, templatefieldDto);
                    aAppDesktopEntity.AppDesktopItem.Add(aAppDesktopItemEntity);
                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppDesktopEntity);
                        adapter.Commit();

                        aAppDesktopExDto.Id = aAppDesktopEntity.DesktopId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppDesktopExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppDesktopExDto(aAppDesktopExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppDesktopExDto(aAppDesktopExDto.Id);
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppDesktopExDto> SaveAsAppDesktopExDto(int dashboardId)
        {
            //OperationCallResult<AppDesktopExDto> aOperationCallResult = new OperationCallResult<AppDesktopExDto>();
            //ValidationResult aValidationResult = new ValidationResult();
            //aOperationCallResult.ValidationResult = aValidationResult;

            var dashboardExDto = RetrieveOneAppDesktopExDto(dashboardId);

            dashboardExDto.Id = null;
            dashboardExDto.DesktopName = dashboardExDto.DesktopName + " Save As";

            if (dashboardExDto.OtherSettingsDto == null)
            {
                dashboardExDto.OtherSettingsDto = new AppDesktopOtherSettingsDto();
            }

            dashboardExDto.OtherSettingsDto.IsUserDesktop = true;
            dashboardExDto.AppCreatedById = AppSecurityUserBL.CurrentUserId;
            dashboardExDto.AppCreatedDate = DateTime.Now;

            var saveResult = SaveAppDesktopExDto(dashboardExDto);


            return saveResult;
        }


        public static OperationCallResult<object> SetUserDashboardAsDefault(int desktopId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            var desktopDto = RetrieveOneAppDesktopExDto(desktopId);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(AppSecurityUserBL.CurrentUserId);
                    userEntity.DefaultDesktopId = desktopId;

                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(userEntity);
                    adapter.Commit();

                    AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache(AppSecurityUserBL.CurrentUserId);

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aOperationCallResult.Object = true;
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }


            return aOperationCallResult;
        }

        public static OperationCallResult<object> DeleteOneAppDesktop(object desktopId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppDesktopItemEntity), new RelationPredicateBucket(AppDesktopItemFields.DesktopId == desktopId));
                    adapter.DeleteEntitiesDirectly(typeof(AppDesktopEntity), new RelationPredicateBucket(AppDesktopFields.DesktopId == desktopId));
                    string message = StringLocalizer.Localize(App_DesktopEntity_Delete_Ok, "Desktop Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_DesktopEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_DesktopEntity_Delete_Failed, "Desktop Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_DesktopEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = desktopId;
            }

            return aOperationCallResult;
        }




        private static ValidationResult ProcessDirtyAppDesktopExDto(AppDesktopExDto aAppDesktopExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyFieldIds = aAppDesktopExDto.AppDesktopItemList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppDesktopEntity aAppDesktopEntity = RetrieveOneAppDesktopEntity(aAppDesktopExDto.Id);

            Dictionary<int, AppDesktopItemEntity> dictAppDesktopItemFromDbms = aAppDesktopEntity.AppDesktopItem.ToDictionary(o => o.DesktopItemId, o => o);

            AppDesktopConverter.CopyDtoToEntity(aAppDesktopEntity, aAppDesktopExDto);
            //  aAppDesktopEntity.ModifiedDate = System.DateTime.UtcNow;
            //  aAppDesktopEntity.ModifiedBy = (int)ServerContext.Instance.CurrentUid;

            //------- check  AppDesktopItem

            // new Items
            foreach (var aChildDto in aAppDesktopExDto.AppDesktopItemList.FindNewItems())
            {
                AppDesktopItemEntity aNewChildEntity = new AppDesktopItemEntity();
                AppDesktopItemConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppDesktopEntity.AppDesktopItem.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppDesktopExDto.AppDesktopItemList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppDesktopItemFromDbms.ContainsKey(dtoKey))
                {
                    AppDesktopItemConverter.CopyDtoToEntity(dictAppDesktopItemFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteFieldIDs = aAppDesktopExDto.AppDesktopItemList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppDesktopEntity);

                    // Need to delete AppDesktopItemFields
                    if (deleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppDesktopItemEntity), new RelationPredicateBucket(AppDesktopItemFields.DesktopItemId == deleteFieldIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDesktopEntity), "App_DesktopEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        public static bool CheckIfDesktopIsReadonlyForCurrentUser(object desktopId)
        {
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)
            {
                return false;
            }
            else
            {
                //List<AppDesktopDto> currentUserDesktop = RetrieveCurrnetUserDesktopDto().ToList();
                //var desktopDto = currentUserDesktop.FirstOrDefault(o => object.Equals(o.Id, desktopId));
                //if (desktopDto != null)
                //{
                //    return desktopDto.IsDesktopReadOnly;
                //}

                return true;
            }
        }


        public static void CheckIfDesktopItemsAreHiddenForCurrentUser(AppDesktopExDto aAppDesktopExDto)
        {
            bool isAdminUser = AppSecurityUserBL.IsAdminUser();
            if (!isAdminUser && aAppDesktopExDto != null)
            {
                RetrieveSearchesDto aRetrieveSearchesDto = AppSearchBL.RetrieveSearches();

                List<int> userAvailableSearchIds = aRetrieveSearchesDto.MySearches.Select(o => (int)o.Id).ToList();
                List<int> userAvailableSavedSearchIds = aRetrieveSearchesDto.SavedSearches.Select(o => (int)o.Id).ToList();
                List<int> userAvailableMenuIds = new List<int>();

                var userMenuList = AppSecurityManagementBL.RetrieveUserMenuFlatList();

                foreach (var menuDto in userMenuList)
                {
                    userAvailableMenuIds.Add((int)menuDto.Id);
                }

                foreach (AppDesktopItemExDto aAppDesktopItemExDto in aAppDesktopExDto.AppDesktopItemList)
                {
                    if (aAppDesktopItemExDto.WidgetItemType.HasValue)
                    {
                        if (aAppDesktopItemExDto.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.InternalPage
                            || aAppDesktopItemExDto.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.InternalShortcut)
                        {
                            CheckIfDesktopItemsAreHiddenForCurrentUser_CheckListMenuItems(aAppDesktopItemExDto, userAvailableMenuIds);
                        }
                        else if (aAppDesktopItemExDto.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.DirectiveWithParameters)
                        {
                            CheckIfDesktopItemsAreHiddenForCurrentUser_CheckSearchWidget(aAppDesktopItemExDto, userAvailableSearchIds, userAvailableSavedSearchIds);
                        }
                        else if (aAppDesktopItemExDto.WidgetItemType.Value == (int)EmAppDashboardWidgetItemType.SearchShortcut)
                        {
                            CheckIfDesktopItemsAreHiddenForCurrentUser_CheckSearchShortcut(aAppDesktopItemExDto, userAvailableSearchIds, userAvailableSavedSearchIds);
                        }
                    }
                }
            }
        }




        public static OperationCallResult<bool> AddOnePageToUserBookmark(AppListMenuDto menuDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDesktopExDto defaultDesktop = null;

            if (AppSecurityUserBL.CurrentUserEntity != null && AppSecurityUserBL.CurrentUserEntity.DefaultDesktopId.HasValue)
            {
                defaultDesktop = RetrieveOneAppDesktopExDto(AppSecurityUserBL.CurrentUserEntity.DefaultDesktopId.Value);
            }
            else
            {
                var createDefaultDesktopResult = CreateUserDefaultDesktop();

                if (createDefaultDesktopResult.IsSuccessfulWithResult)
                {
                    defaultDesktop = createDefaultDesktopResult.Object;
                }
                else
                {
                    aValidationResult.Merge(createDefaultDesktopResult.ValidationResult);
                }
            }

            if (defaultDesktop != null)
            {
                menuDto.Description = menuDto.RouteCode
                    + "_" + ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(menuDto.Link)
                    + "_" + ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(menuDto.Param1)
                    + "_" + ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(menuDto.Param2);

                if (defaultDesktop.UserBookmarkList.FirstOrDefault(o => o.Description == menuDto.Description) != null)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "AddOnePageToUserBookmark_Failed", ValidationItemType.Message, "Bookmark already exists."));
                }
                else
                {
                    int maxSort = 0;

                    if (defaultDesktop.UserBookmarkList.Where(o => o.Sort.HasValue).Count() > 0)
                    {
                        maxSort = defaultDesktop.UserBookmarkList.Where(o => o.Sort.HasValue).Max(o => o.Sort.Value);
                    }

                    menuDto.Sort = maxSort + 1;
                    defaultDesktop.UserBookmarkList.Add(menuDto);

                    var saveResult = SaveAppDesktopExDto(defaultDesktop);

                    if (saveResult.IsSuccessfulWithResult)
                    {
                        aOperationCallResult.Object = true;
                        aValidationResult.Items.Add(new ValidationItem(null, "AddOnePageToUserBookmark_Failed", ValidationItemType.Message, "Bookmark " + menuDto.Name + " has been created."));
                    }
                    else
                    {
                        aValidationResult.Merge(saveResult.ValidationResult);
                    }
                }                
            }


            return aOperationCallResult;
        }


        private static void CheckIfDesktopItemsAreHiddenForCurrentUser_CheckListMenuItems(AppDesktopItemExDto aAppDesktopItemExDto, List<int> userAvailableMenuIds)
        {
            int? menuId = ControlTypeValueConverter.ConvertValueToInt(aAppDesktopItemExDto.ParameterKeyValue);
            if (menuId.HasValue)
            {
                aAppDesktopItemExDto.IsDesktopItemHidden = !userAvailableMenuIds.Contains(menuId.Value);
            }
            else
            {
                aAppDesktopItemExDto.IsDesktopItemHidden = true;
            }
        }

        private static void CheckIfDesktopItemsAreHiddenForCurrentUser_CheckSearchShortcut(AppDesktopItemExDto aAppDesktopItemExDto, List<int> userAvailableSearchIds, List<int> userAvailableSavedSearchIds)
        {
            bool? isSavedSearch = ControlTypeValueConverter.ConvertValueToBoolean(aAppDesktopItemExDto.ParameterKeyValue);
            int? searchIdOrSavedSearchId = ControlTypeValueConverter.ConvertValueToInt(aAppDesktopItemExDto.DomElementTag);

            if (isSavedSearch.HasValue && searchIdOrSavedSearchId.HasValue)
            {
                if (isSavedSearch.Value)
                {
                    aAppDesktopItemExDto.IsDesktopItemHidden = !userAvailableSavedSearchIds.Contains(searchIdOrSavedSearchId.Value);
                }
                else
                {
                    aAppDesktopItemExDto.IsDesktopItemHidden = !userAvailableSearchIds.Contains(searchIdOrSavedSearchId.Value);
                }
            }
            else
            {
                aAppDesktopItemExDto.IsDesktopItemHidden = true;
            }
        }

        private static void CheckIfDesktopItemsAreHiddenForCurrentUser_CheckSearchWidget(AppDesktopItemExDto aAppDesktopItemExDto, List<int> userAvailableSearchIds, List<int> userAvailableSavedSearchIds)
        {
            if (aAppDesktopItemExDto.DomElementTag == "app-search" && !string.IsNullOrEmpty(aAppDesktopItemExDto.ParameterKeyValue))
            {
                if (!string.IsNullOrEmpty(aAppDesktopItemExDto.ParameterKeyValue))
                {
                    string[] parameters = aAppDesktopItemExDto.ParameterKeyValue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    bool? isSavedSearch = null;
                    int? searchIdOrSavedSearchId = null;

                    // Get ParameterKeyValue parameters: "initialsearchid=220 is-saved-search=false is-show-criterias=false"

                    foreach (string parameter in parameters)
                    {
                        if (parameter.Contains("initialsearchid="))
                        {
                            searchIdOrSavedSearchId = ControlTypeValueConverter.ConvertValueToInt(parameter.Substring("initialsearchid=".Length));
                        }
                        else if (parameter.Contains("is-saved-search="))
                        {
                            isSavedSearch = ControlTypeValueConverter.ConvertValueToBoolean(parameter.Substring("is-saved-search=".Length));
                        }
                    }

                    if (isSavedSearch.HasValue && searchIdOrSavedSearchId.HasValue)
                    {
                        if (isSavedSearch.Value)
                        {
                            aAppDesktopItemExDto.IsDesktopItemHidden = !userAvailableSavedSearchIds.Contains(searchIdOrSavedSearchId.Value);
                        }
                        else
                        {
                            aAppDesktopItemExDto.IsDesktopItemHidden = !userAvailableSearchIds.Contains(searchIdOrSavedSearchId.Value);
                        }
                    }
                    else
                    {
                        aAppDesktopItemExDto.IsDesktopItemHidden = true;
                    }
                }
                else
                {
                    aAppDesktopItemExDto.IsDesktopItemHidden = true;
                }
            }

        }


    }
}