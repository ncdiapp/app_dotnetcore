using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;

namespace App.BL
{
    public static class AppSecurityManagementBL
    {
        private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;



        //

        #region --- user menu security control

        public static List<LookupItemDto> RetrieveCurrentUserCompany()
        {
            return RetrieveUserAvailableCompaniesFromMasterDB((int)ServerContext.Instance.CurrentUid);

        }



        public static ObservableSet<AppListMenuExDto> RetrieveUserMenu()
        {
            ObservableSet<AppListMenuExDto> userMenuList = new ObservableSet<AppListMenuExDto>();

            bool isAdmin = AppSecurityUserBL.IsAdminUser();


            if (isAdmin)
            {

                userMenuList = AppListMenuBL.RetrieveAllAppListMenuEntityDto();
            }
            else
            {
                //return  AppListMenuBL.RetrieveAllAppListMenuEntityDto();

                //userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserMenu(AppSecurityUserBL.CurrentUserEntity.UserId, EmAppMenuRegisterType.User);
                //if (userMenuList.Count == 0)
                //{
                //    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserMenu(AppSecurityUserBL.CurrentUserEntity.DomainId, EmAppMenuRegisterType.Organization);
                //}
                //if (userMenuList.Count == 0)
                //{
                //    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserMenu(AppSecurityUserBL.CurrentUserEntity.DomainId, EmAppMenuRegisterType.RegionDomain);
                //}
                //if (userMenuList.Count == 0)
                //{
                //    userMenuList = AppListMenuBL.RetrieveAllAppListMenuEntityDto();
                //}


                if (userMenuList.Count == 0)
                {

                    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserMenu(AppSecurityUserBL.CurrentGroupIds.ToList(), EmAppMenuRegisterType.Role);
                }

                if (userMenuList.Count == 0)
                {
                    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserMenu(new List<int>() { AppSecurityUserBL.CurrentUserEntity.DomainId }, EmAppMenuRegisterType.RegionDomain);
                }

            }

            foreach (var menuGroup in userMenuList)
            {
                menuGroup.Name = AppLocalizeSystemLableBL.GetMenuLabel(menuGroup.Id, menuGroup.Name);
                foreach (var childMenu in menuGroup.AppListMenu_List)
                {
                    childMenu.Name = AppLocalizeSystemLableBL.GetMenuLabel(childMenu.Id, childMenu.Name);
                }
            }

            return userMenuList;
        }


        public static ObservableSet<AppListMenuExDto> RetrieveUserTreeMenu()
        {
            ObservableSet<AppListMenuExDto> userMenuList = new ObservableSet<AppListMenuExDto>();

            bool isSystemAdmin = ServerContext.Instance.CurrentUid != null && AppSecurityUserBL.CurrentUserEntity?.IsInSysAdminDomain == true;


            if (isSystemAdmin)
            {

                userMenuList = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, null, true);

            }
            else
            {

                //userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserTreeMenu(new List<int>() { AppSecurityUserBL.CurrentUserEntity.UserId }, EmAppMenuRegisterType.User);

                //if (userMenuList.Count == 0)
                //{
                //    if (!(AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Supplier 
                //        || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Customer
                //        || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.ClientAgent))
                //    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserTreeMenu(AppSecurityUserBL.CurrentUserEntity.OrganizationId, EmAppMenuRegisterType.Organization);
                //}

                if (userMenuList.Count == 0)
                {

                    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserTreeMenu(AppSecurityUserBL.CurrentGroupIds.ToList(), EmAppMenuRegisterType.Role);
                }

                if (userMenuList.Count == 0)
                {
                    userMenuList = AppListMenuUserAndDomainBL.RetrieveDomainOrUserTreeMenu(new List<int>() { AppSecurityUserBL.CurrentUserEntity.DomainId }, EmAppMenuRegisterType.RegionDomain);
                }


            }

            AssignMenuTreeLanguageKey(userMenuList);

            return userMenuList;
        }

        public static ObservableSet<AppListMenuExDto> RetrieveUserMenuFlatList()
        {
            ObservableSet<AppListMenuExDto> flatMenuList = new ObservableSet<AppListMenuExDto>();

            ObservableSet<AppListMenuExDto> userTreeMenuList = RetrieveUserTreeMenu();
            AddMenuTreeToFlatMenuList(flatMenuList, userTreeMenuList);

            return flatMenuList;
        }



        public static List<LookupItemDto> RetrieveUserAvailableCompaniesFromMasterDB(int userId)
        {
            List<LookupItemDto> toReturn = new List<LookupItemDto>();

            AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserSimpleEntity(userId);

            if (userEntity != null)
            {
                if (userEntity.IsBuiltIntUser.HasValue && userEntity.IsBuiltIntUser.Value)
                {
                    LookupItemDto companyLookup = new LookupItemDto()
                    {
                        Id = AppSaasAccountUserBL.EncryptCompanyIdString(1),
                        Display = "Host Company",
                        ItemType = (int)EmAppSaasUserAvailableCompanyType.MyCompany
                    };
                    toReturn.Add(companyLookup);
                }
                else
                {
                    // My onw compnay ..
                    if (userEntity.MyOwnCompnanyId.HasValue)
                    {
                        int myCompanyId = userEntity.MyOwnCompnanyId.Value;
                        var myCompany = AppCompanyBL.RetrieveOneAppCompanyEntityFromMasterDB(myCompanyId);

                        LookupItemDto companyLookup = new LookupItemDto()
                        {
                            Id = AppSaasAccountUserBL.EncryptCompanyIdString(myCompanyId),
                            Display = myCompany.Code + " (My Company)",
                            ColorCode = myCompany.Code,
                            ItemType = (int)EmAppSaasUserAvailableCompanyType.MyCompany
                        };
                        toReturn.Add(companyLookup);
                    }


                    // check  partner compnay!!, all invte compnay must register in Master DB as weel !!!!
                    // During login, tenant DB context is not yet available; skip partner lookup gracefully.
                    List<AppBusinessPartnerInviteUserEntity> businessPartnerInviteList;
                    try
                    {
                        businessPartnerInviteList = AppSaasAccountUserBL.RetrieveAppBusinessPartnerInviteUserEntityListByUserId(userId);
                    }
                    catch (InvalidOperationException)
                    {
                        businessPartnerInviteList = new List<AppBusinessPartnerInviteUserEntity>();
                    }

                    List<int> parternerCompanyIds = businessPartnerInviteList.Where(o => o.AppCompanyId.HasValue).Select(o => o.AppCompanyId.Value).Distinct().ToList();

                    var parternerCompanyEntityList = AppCompanyBL.RetrieveCompanyEntityListByIdsFromMasterDB(parternerCompanyIds);

                    foreach (var companyEntity in parternerCompanyEntityList)
                    {
                        LookupItemDto companyLookup = new LookupItemDto()
                        {
                            Id = AppSaasAccountUserBL.EncryptCompanyIdString(companyEntity.AppCompanyId),
                            Display = companyEntity.Code,
                            ColorCode = companyEntity.Code,
                            ItemType = (int)EmAppSaasUserAvailableCompanyType.BusinessParternerCompany
                        };
                        toReturn.Add(companyLookup);
                    }
                }
            }


            return toReturn;
        }


        public static List<LookupItemDto> RetrieveEmployeeUserExternalMappingAccountLookupItemList()
        {
            List<LookupItemDto> toReturn = null;

            int? mappingToExEmployee_EntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EmployeeEntity);

            if (mappingToExEmployee_EntityId.HasValue)
            {
                toReturn = AppEntityInfoBL.GetLookupItemList(mappingToExEmployee_EntityId.Value, string.Empty);
                toReturn.ForAll(o => o.Id = o.Id.ToString());

            }

            return toReturn;
        }


        private static void AssignMenuTreeLanguageKey(ObservableSet<AppListMenuExDto> userMenuList)
        {
            foreach (var aMenu in userMenuList)
            {
                aMenu.Name = AppLocalizeSystemLableBL.GetMenuLabel(aMenu.Id, aMenu.Name);
                AssignMenuTreeLanguageKey(aMenu.AppListMenu_List);
            }
        }


        private static void AddMenuTreeToFlatMenuList(ObservableSet<AppListMenuExDto> flatMenuList, ObservableSet<AppListMenuExDto> userTreeMenuList)
        {
            if (flatMenuList != null && userTreeMenuList != null && userTreeMenuList.Count > 0)
            {
                foreach (var menuDto in userTreeMenuList)
                {
                    flatMenuList.Add(menuDto);
                    AddMenuTreeToFlatMenuList(flatMenuList, menuDto.AppListMenu_List);
                }
            }
        }
        #endregion

        #region ---   Search  and view control control

        public static List<int> GetCurrentUserAvailableSearchIds()
        {
            List<int> aList = RetrieveUserAllAvaibleSearchEntity().Select(o => o.SearchId).ToList();



            return aList;
        }

        internal static List<SearchDefinitionDto> GetCurrentUserSearchDefinitionDtoList(int? searchUsageType = null)
        {
            List<SearchDefinitionDto> aList = new List<SearchDefinitionDto>();



            foreach (var searchTempalte in RetrieveUserAllAvaibleSearchEntity(searchUsageType))
            {
                SearchDefinitionDto aSearchDto = new SearchDefinitionDto();
                aSearchDto.Id = searchTempalte.SearchId;
                aSearchDto.Display = searchTempalte.Name.ToString();
                aSearchDto.IsSavedSearch = false;
                aSearchDto.SearchType = searchTempalte.Type;

                aSearchDto.Display = AppLocalizeSystemLableBL.GetSearchLabel(aSearchDto.Id, aSearchDto.Display);

                aList.Add(aSearchDto);
            }

            return aList;
        }

        internal static EntityCollection<AppSearchEntity> RetrieveUserAllAvaibleSearchEntity(int? searchUsageType = null)
        {
            EntityCollection<AppSearchEntity> toReturn = new EntityCollection<AppSearchEntity>();

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)
            {

                return AppSearchConfigBL.RetrieveAllSearchEntity(searchUsageType);

            }
            else
            {







                EntityCollection<AppSearchEntity> availableOrgSearchlist = new EntityCollection<AppSearchEntity>();
                EntityCollection<AppSearchEntity> restrictUserRoleSearchList = new EntityCollection<AppSearchEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket();
                    filter.Relations.Add(AppSearchEntity.Relations.AppSecuritySysObjGroupUserEntityUsingSearchId);

                    if (searchUsageType.HasValue)
                    {
                        filter.PredicateExpression.AddWithAnd(AppSearchFields.Type == searchUsageType.Value);
                    }



                    adapter.FetchEntityCollection(availableOrgSearchlist, filter, 0, null);
                }

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {

                    try
                    {
                        RelationPredicateBucket filter = new RelationPredicateBucket();
                        filter.Relations.Add(AppSearchEntity.Relations.AppSecuritySysObjGroupUserEntityUsingSearchId);
                        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.IsIgnoreFilterBy == DBNull.Value &
                            (AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds));
                        filter.PredicateExpression.AddWithOr(AppSearchFields.IsForPublicAcesss == true);
                        adapter.FetchEntityCollection(restrictUserRoleSearchList, filter, 0, null);
                    }
                    catch (Exception ex)
                    {

                    }

                }

                List<int> acceccibleSearchIds = restrictUserRoleSearchList.Select(o => o.SearchId).ToList();


                foreach (var anEntity in availableOrgSearchlist)
                {
                    if (acceccibleSearchIds.Contains(anEntity.SearchId))
                    {
                        toReturn.Add(anEntity);
                    }
                }






                return toReturn;

            }
        }


        internal static List<int> RetrieveUserIgnoreFilterByCurrentUserSearchIds()
        {
            List<int> toReturn = new List<int>();


            bool isAdmin = AppSecurityUserBL.IsAdminUser();


            if (!isAdmin)
            {

                EntityCollection<AppSearchEntity> ignoreFilterSearchList = new EntityCollection<AppSearchEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket();
                    filter.Relations.Add(AppSearchEntity.Relations.AppSecuritySysObjGroupUserEntityUsingSearchId);

                    if (ServerContext.Instance.CurrentUid != null)
                    {
                        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.IsIgnoreFilterBy != DBNull.Value &
                            (AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds));
                    }

                    adapter.FetchEntityCollection(ignoreFilterSearchList, filter, 0, null);
                }

                toReturn = ignoreFilterSearchList.Select(o => o.SearchId).Distinct().ToList();


            }




            return toReturn;


        }



        // Transcation Security control
        #endregion

        #region  -- Transcation Security control

        public static void ProcessCurrentUserTransactionAllSecurity(AppTransactionExDto appTransactionExDto, object rootStringPkId)
        {
            ProcessCurrentUserTransactionLevelSecurity(appTransactionExDto);


            if (appTransactionExDto.IsAllowAccess)
            {
                if (!(appTransactionExDto.OtherOptions != null && appTransactionExDto.OtherOptions.IsApiIntegrationTransaction))
                {
                    object formDataCreatedById = null;
                    if (rootStringPkId != null && rootStringPkId.ToString() != string.Empty && rootStringPkId.ToString() != "null")
                    {

                        string pkFileName = appTransactionExDto.RootMasterUnit.PrimaryKeyDbfieldList[0];
                        var pkfiled = appTransactionExDto.RootMasterUnit.AppTransactionFieldList.Where(o => o.DataBaseFieldName == pkFileName).First();

                        object rooKeyVale = ControlTypeValueConverter.ConvertValueToObject(rootStringPkId, pkfiled.ControlType);

                        if (rooKeyVale != null)
                        {
                            Dictionary<string, object> rootOneToOneFields = new Dictionary<string, object>();

                            rootOneToOneFields[pkFileName] = rooKeyVale;

                            // for IsReadOnly  appTransactionExDto, need redeing EmSystemDbTrackField.AppCreatedByID
                            if (appTransactionExDto.IsReadOnly.HasValue && appTransactionExDto.IsReadOnly.Value )
                            {

                            }
                            else
                            {
                                DataRow row = AppDbHelerBL.RetriveOneDataRowWtihPrimayKeyFromDataBaseTable(rootOneToOneFields, appTransactionExDto.RootMasterUnit);
                                if (row != null && row.Table != null && row.Table.Columns != null &&
                                    row.Table.Columns.Contains(EmSystemDbTrackField.AppCreatedByID.ToString()))
                                {
                                    formDataCreatedById = row[EmSystemDbTrackField.AppCreatedByID.ToString()].ToString();
                                }

                            }

                         
                        }





                    }


                    ProcessCurrentUserTransactionUnitSecurity(appTransactionExDto, formDataCreatedById);
                    ProcessCurrentUserTransactionFieldSecurity(appTransactionExDto, formDataCreatedById);
                }


            }

        }

        // Transaction level security 

        private static void ProcessCurrentUserTransactionLevelSecurity(AppTransactionExDto appTransactionExDto)
        {
            var transactionId = appTransactionExDto.Id;
            bool isAdmin = AppSecurityUserBL.IsAdminUser();



            if (isAdmin || appTransactionExDto.IsForPublicAcesss.HasValue && appTransactionExDto.IsForPublicAcesss.Value)
            {
                appTransactionExDto.IsAllowAccess = true;


            }
            else
            {


                CheckTransactionSecurity(appTransactionExDto);

            }

            if (appTransactionExDto.IsAllowAccess)
            {
                if (isAdmin)
                {
                    appTransactionExDto.RestrictedTransactionUserActionList = new List<int>();
                    appTransactionExDto.RestrictedTransactionCommandIdList = new List<int>();
                }

                else
                {
                    appTransactionExDto.RestrictedTransactionUserActionList = GetCurrentUserRestrictedTransactionActionList(appTransactionExDto.Id);
                    appTransactionExDto.RestrictedTransactionCommandIdList = GetCurrentUserRestrictedTransactionCommandIdList(appTransactionExDto);
                }
            }



        }


        private static void CheckTransactionSecurity(AppTransactionExDto appTransactionExDto)
        {
            object transactionId = appTransactionExDto.Id;
            bool isUserOrRoleAllowAccess = false;

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> accessibleUserRoleObjList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.TransactionId == transactionId);
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId
                    | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds);
                adapter.FetchEntityCollection(accessibleUserRoleObjList, filter, 0, null);

                if (accessibleUserRoleObjList.Count > 0)
                {
                    isUserOrRoleAllowAccess = true;
                }
            }


            appTransactionExDto.IsAllowAccess = isUserOrRoleAllowAccess;

            if (appTransactionExDto.IsForPublicAcesss.HasValue && appTransactionExDto.IsForPublicAcesss.Value)
            {
                appTransactionExDto.IsAllowAccess = true;
            }

            //if (isUserOrRoleReadOnly)
            //{
            //    appTransactionExDto.IsReadOnly = true;
            //}

        }

        private static void CheckUserTypeTransactionSecurity(AppTransactionExDto appTransactionExDto, int userType)
        {
            bool isuserTypeAllowAccess = false;
            object transactionId = appTransactionExDto.Id;


            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.TransactionId == transactionId);
                filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.EmUserType == userType);
                adapter.FetchEntityCollection(availableOrgSysObjlist, filter, 0, null);

                isuserTypeAllowAccess = availableOrgSysObjlist.Count > 0;
            }


            //bool isUserTypeInvisible = false;
            //bool isUserTypeReadOnly = false;
            bool isUserOrRoleAllowAccess = false;

            if (isuserTypeAllowAccess)
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    EntityCollection<AppSecuritySysObjGroupUserEntity> accessibleUserTypeObjList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.TransactionId == transactionId);
                    filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionCode == userType
                        );
                    adapter.FetchEntityCollection(accessibleUserTypeObjList, filter, 0, null);

                    if (accessibleUserTypeObjList.Count > 0)
                    {
                        isUserOrRoleAllowAccess = true;
                    }
                }
            }

            appTransactionExDto.IsAllowAccess = isuserTypeAllowAccess && isUserOrRoleAllowAccess;

            //if (isUserTypeReadOnly)
            //{
            //    appTransactionExDto.IsReadOnly = true;
            //}


        }
        private static List<int> GetCurrentUserRestrictedTransactionActionList(object transactionId)
        {
            List<int> restrictedTransactionActionList = new List<int>();

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (!isAdmin)


            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> restrictUserRoleObjList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket();
                    filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionId == transactionId);
                    filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId
                        | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds);
                    adapter.FetchEntityCollection(restrictUserRoleObjList, filter, 0, null);
                }

                restrictedTransactionActionList = restrictUserRoleObjList.Where(o => o.UserActionTransactionCode.HasValue).Select(o => o.UserActionTransactionCode.Value).Distinct().ToList();

            }

            return restrictedTransactionActionList;
        }

        private static List<int> GetCurrentUserRestrictedTransactionCommandIdList(AppTransactionExDto transactionExDto)
        {
            object transactionId = transactionExDto.Id;

            List<int> restrictedTransactionCommandIdList = new List<int>();

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (!isAdmin)
            {
                if (transactionExDto.CommandActionList != null && transactionExDto.CommandActionList.Count > 0)
                {
                    int[] commandIds = transactionExDto.CommandActionList.Select(o => (int)o.Id).ToArray();

                    EntityCollection<AppSecuritySysObjGroupUserEntity> restrictUserRoleObjList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                    using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                    {

                        RelationPredicateBucket filter = new RelationPredicateBucket();
                        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.CommandId == commandIds);
                        filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId
                            | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds);
                        adapter.FetchEntityCollection(restrictUserRoleObjList, filter, 0, null);
                    }

                    restrictedTransactionCommandIdList = restrictUserRoleObjList.Where(o => o.CommandId.HasValue).Select(o => o.CommandId.Value).Distinct().ToList();
                }
            }

            return restrictedTransactionCommandIdList;
        }


        // unit table security
        private static void ProcessCurrentUserTransactionUnitSecurity(AppTransactionExDto appTransactionExDto, object formDataCreatedById)
        {
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)

            {
                if (appTransactionExDto != null && appTransactionExDto.DictAllTransactionUnitIdExDto != null)
                {
                    foreach (var unitDto in appTransactionExDto.DictAllTransactionUnitIdExDto.Values)
                    {
                        unitDto.IsFormLayoutVisible = true;
                        unitDto.IsReadOnly = false;
                    }
                }

                return;
            }

            else
            {
                var transactionUnitIds = appTransactionExDto.DictAllTransactionUnitIdExDto.Values.Select(o => (int)o.Id).ToArray();

                var filter = new RelationPredicateBucket
                   (
                      (
                      AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserId
                      | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds
                      ) & AppSecuritySysObjGroupUserFields.TransactionUnitId == transactionUnitIds
                  );

                ProcessTransactionUnitSecurityWithFilter(appTransactionExDto, filter);


            }

            if (formDataCreatedById != null)
            {
                // need to check currecnt creater(owner) User runing time 

                List<int> currentUserUnitIds = appTransactionExDto.DictAllTransactionUnitIdExDto.Values
                    .Where(o => o.IsExclusiveForOwner.HasValue && o.IsExclusiveForOwner.Value)
                    .Select(o => (int)o.Id).ToList();


                if (!currentUserUnitIds.IsEmpty())
                {


                    if (formDataCreatedById.ToString() == ServerContext.Instance.CurrentUid.ToString())
                    {
                        foreach (int unitId in currentUserUnitIds)
                        {
                            var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                            unitDto.IsReadOnly = false;

                        }



                    }
                    else
                    {
                        foreach (int unitId in currentUserUnitIds)
                        {
                            var fieldDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                            fieldDto.IsReadOnly = true;

                        }

                    }
                }


            }
        }

        private static void ProcessTransactionUnitSecurityWithFilter(AppTransactionExDto appTransactionExDto, RelationPredicateBucket filter)
        {


            if (appTransactionExDto != null && appTransactionExDto.DictAllTransactionUnitIdExDto != null)
            {
                //var unitIds = appTransactionExDto.DictAllTransactionUnitIdExDto.Values.Select(o => (int)o.Id).ToArray();

                var transactionUnitUserRights = RetrieveTransactionUnitSecurityEntityWithFilter(filter);

                foreach (var unitDto in appTransactionExDto.DictAllTransactionUnitIdExDto.Values)
                {
                    unitDto.IsFormLayoutVisible = true;
                    unitDto.IsReadOnly = false;

                    if (transactionUnitUserRights.Count > 0)
                    {
                        foreach (AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity in transactionUnitUserRights)
                        {
                            if (aAppSecuritySysObjGroupUserEntity.TransactionUnitId.HasValue && aAppSecuritySysObjGroupUserEntity.TransactionUnitId.Value == (int)unitDto.Id)
                            {


                                if (aAppSecuritySysObjGroupUserEntity.IsInVisible.HasValue && aAppSecuritySysObjGroupUserEntity.IsInVisible.Value)
                                {
                                    unitDto.IsFormLayoutVisible = false;
                                }

                                else if (aAppSecuritySysObjGroupUserEntity.IsUnSaveAble.HasValue && aAppSecuritySysObjGroupUserEntity.IsUnSaveAble.Value)
                                {
                                    unitDto.IsReadOnly = true;
                                }
                            }
                        }
                    }

                    if (appTransactionExDto.IsReadOnly.HasValue && appTransactionExDto.IsReadOnly.Value)
                    {
                        unitDto.IsReadOnly = true;
                    }

                    if (!(unitDto.IsFormLayoutVisible.HasValue && !unitDto.IsFormLayoutVisible.Value) && !(unitDto.IsReadOnly.HasValue && unitDto.IsReadOnly.Value))
                    {
                        unitDto.RestrictedTransactionUnitUserActionList = GetCurrentUserRestrictedTransactionUnitActionList(unitDto.Id);
                    }

                }
            }
        }



        private static EntityCollection<AppSecuritySysObjGroupUserEntity> RetrieveTransactionUnitSecurityEntityWithFilter(RelationPredicateBucket filter)
        {

            EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)

            {
                return list;
            }

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {



                ResultsetFields resultsetFields = new ResultsetFields(3);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.TransactionUnitId, 0);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.IsInVisible, 1);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.IsUnSaveAble, 2);




                DataTable dynamicList = new DataTable();
                adapter.FetchTypedList(resultsetFields, dynamicList, filter, 0, null, false);

                foreach (DataRow row in dynamicList.Rows)
                {

                    AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity = new AppSecuritySysObjGroupUserEntity();
                    aAppSecuritySysObjGroupUserEntity.TransactionUnitId = row[0] as int?;
                    aAppSecuritySysObjGroupUserEntity.IsInVisible = row[1] as bool?;
                    aAppSecuritySysObjGroupUserEntity.IsUnSaveAble = row[2] as bool?;
                    list.Add(aAppSecuritySysObjGroupUserEntity);

                }
            }

            return list;



        }

        private static List<int> GetCurrentUserRestrictedTransactionUnitActionList(object transactionUnitId)
        {
            List<int> restrictedTransactionUnitActionList = new List<int>();

            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (!isAdmin)


            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> restrictUserRoleObjList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket();
                    filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionUnitId == transactionUnitId);
                    filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId
                        | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds);
                    adapter.FetchEntityCollection(restrictUserRoleObjList, filter, 0, null);
                }

                restrictedTransactionUnitActionList = restrictUserRoleObjList.Where(o => o.UserActionTransactionUnitCode.HasValue).Select(o => o.UserActionTransactionUnitCode.Value).Distinct().ToList();

            }

            return restrictedTransactionUnitActionList;
        }




        // column level , Filed levle  security

        private static void ProcessCurrentUserTransactionFieldSecurity(AppTransactionExDto appTransactionExDto, object formDataCreatedById)
        {
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)
            {
                if (appTransactionExDto != null && appTransactionExDto.DictAllTransactionField != null)
                {

                    foreach (int transactionFieldId in appTransactionExDto.DictAllTransactionField.Keys)
                    {
                        var fieldDto = appTransactionExDto.DictAllTransactionField[transactionFieldId];
                        if (fieldDto != null)
                        {
                            fieldDto.IsFormLayoutVisible = fieldDto.IsVisible;
                            fieldDto.IsFormLayoutReadOnly = fieldDto.IsReadonly;
                        }
                    }
                }
                return;
            }

            else
            {
                ProcessUserSystemObjectsecurity(appTransactionExDto);

                if (formDataCreatedById != null)
                {
                    // need to check currecnt creater(owner) User runing time 

                    List<int> currentUserFieldFieldIds = appTransactionExDto.DictAllTransactionField.Values
                        .Where(o => o.IsFieldExclusiveForOwner.HasValue && o.IsFieldExclusiveForOwner.Value)
                        .Select(o => (int)o.Id).ToList();


                    if (!currentUserFieldFieldIds.IsEmpty())
                    {


                        if (formDataCreatedById.ToString() == ServerContext.Instance.CurrentUid.ToString())
                        {
                            foreach (int fieldId in currentUserFieldFieldIds)
                            {
                                var fieldDto = appTransactionExDto.DictAllTransactionField[fieldId];

                                fieldDto.IsFormLayoutReadOnly = fieldDto.IsReadonly;
                            }



                        }
                        else
                        {
                            foreach (int fieldId in currentUserFieldFieldIds)
                            {
                                var fieldDto = appTransactionExDto.DictAllTransactionField[fieldId];

                                fieldDto.IsFormLayoutReadOnly = true;

                            }

                        }
                    }


                }

            }
        }

        private static void ProcessUserSystemObjectsecurity(AppTransactionExDto appTransactionExDto)
        {
            List<int> transactionFieldIds = appTransactionExDto.DictAllTransactionField.Select(o => (int)o.Key).ToList();




            RelationPredicateBucket filter = new RelationPredicateBucket
                (
                   (
                   AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserId
                   | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds
                   ) & AppSecuritySysObjGroupUserFields.TransactionFieldId == transactionFieldIds
               );

            var transactionFieldUserRights = RetrieveTransactionFieldSecurityWithFilter(transactionFieldIds.ToArray(), filter);

            SetupTransactionFieldSecurity(appTransactionExDto, transactionFieldUserRights);


        }

        private static EntityCollection<AppSecuritySysObjGroupUserEntity> RetrieveTransactionFieldSecurityWithFilter(int[] transactionFieldIds, RelationPredicateBucket filter)
        {

            EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();



            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {



                ResultsetFields resultsetFields = new ResultsetFields(4);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.TransactionFieldId, 0);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.IsInVisible, 1);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.IsUnSaveAble, 2);
                resultsetFields.DefineField(AppSecuritySysObjGroupUserFields.IsNeedSpecailEditPrivilege, 3);





                DataTable dynamicList = new DataTable();
                adapter.FetchTypedList(resultsetFields, dynamicList, filter, 0, null, false);

                foreach (DataRow row in dynamicList.Rows)
                {

                    AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity = new AppSecuritySysObjGroupUserEntity();
                    aAppSecuritySysObjGroupUserEntity.TransactionFieldId = row[0] as int?;
                    aAppSecuritySysObjGroupUserEntity.IsInVisible = row[1] as bool?;
                    aAppSecuritySysObjGroupUserEntity.IsUnSaveAble = row[2] as bool?;
                    aAppSecuritySysObjGroupUserEntity.IsNeedSpecailEditPrivilege = row[3] as bool?;

                    list.Add(aAppSecuritySysObjGroupUserEntity);

                }
            }

            return list;



        }

        private static void SetupTransactionFieldSecurity(AppTransactionExDto appTransactionExDto, EntityCollection<AppSecuritySysObjGroupUserEntity> transactionFieldUserRights)
        {

            appTransactionExDto.SepcialEditPermissionTransFieldIdList = new List<int>();

            if (appTransactionExDto != null && appTransactionExDto.DictAllTransactionField != null)
            {

                foreach (int transactionFieldId in appTransactionExDto.DictAllTransactionField.Keys)
                {
                    var transactionFieldDto = appTransactionExDto.DictAllTransactionField[transactionFieldId];

                    transactionFieldDto.IsFormLayoutReadOnly = transactionFieldDto.IsReadonly;
                    transactionFieldDto.IsFormLayoutVisible = transactionFieldDto.IsVisible;

                    if (transactionFieldUserRights.Count > 0)
                    {

                        foreach (AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity in transactionFieldUserRights)
                        {
                            if (aAppSecuritySysObjGroupUserEntity.TransactionFieldId.HasValue && aAppSecuritySysObjGroupUserEntity.TransactionFieldId.Value == (int)transactionFieldId)
                            {


                                transactionFieldDto.IsFormLayoutVisible = transactionFieldDto.IsVisible;

                                if (aAppSecuritySysObjGroupUserEntity.IsInVisible.HasValue && aAppSecuritySysObjGroupUserEntity.IsInVisible.Value)
                                {
                                    transactionFieldDto.IsFormLayoutVisible = false;
                                }

                                else if (aAppSecuritySysObjGroupUserEntity.IsUnSaveAble.HasValue && aAppSecuritySysObjGroupUserEntity.IsUnSaveAble.Value)
                                {
                                    transactionFieldDto.IsFormLayoutReadOnly = true;
                                }


                            }

                        }
                    }

                    if (appTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(transactionFieldDto.TransactionUnitId.ToString()))
                    {
                        var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[transactionFieldDto.TransactionUnitId.ToString()];
                        if (unitDto != null)
                        {
                            if (unitDto.IsReadOnly.HasValue && unitDto.IsReadOnly.Value)
                            {
                                transactionFieldDto.IsFormLayoutReadOnly = true;
                            }

                            if (unitDto.IsFormLayoutVisible.HasValue && !unitDto.IsFormLayoutVisible.Value)
                            {
                                transactionFieldDto.IsFormLayoutVisible = false;
                            }
                        }
                    }

                    // last step  oervrid specail right 
                    if (transactionFieldUserRights.Count > 0)
                    {

                        foreach (AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity in transactionFieldUserRights)
                        {
                            if (aAppSecuritySysObjGroupUserEntity.TransactionFieldId.HasValue && aAppSecuritySysObjGroupUserEntity.TransactionFieldId.Value == (int)transactionFieldId)
                            {




                                if (aAppSecuritySysObjGroupUserEntity.IsNeedSpecailEditPrivilege.HasValue && aAppSecuritySysObjGroupUserEntity.IsNeedSpecailEditPrivilege.Value)
                                {
                                    transactionFieldDto.IsFormLayoutVisible = true;
                                    transactionFieldDto.IsFormLayoutReadOnly = false;
                                    appTransactionExDto.SepcialEditPermissionTransFieldIdList.Add((int)transactionFieldDto.Id);

                                }




                            }

                        }
                    }
                }
            }
        }


        #endregion

        #region ---- Dashboard security

        public static List<int> GetCurrentUserAvailableDesktopIds()
        {
            List<int> aList = RetrieveCurrnetUserDesktopDto().Select(o => (int)o.Id).ToList();

            return aList;
        }

        public static List<AppDesktopDto> GetOrganizationDesktopListByOrganizationId(int organizationId)
        {
            var desktopList = new List<AppDesktopDto>();

            var desktopids = new List<int>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                IncludeFieldsList includefiled = new IncludeFieldsList();
                includefiled.Add(AppSecuritySysObjGroupUserFields.DesktopId);

                EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.DesktopId != DBNull.Value);
                filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.OrganizationId == organizationId);
                adapter.FetchEntityCollection(availableOrgSysObjlist, includefiled, filter);

                desktopids = availableOrgSysObjlist.Select(o => o.DesktopId.Value).ToList();
            }

            // need to remove restriction Dashboard           


            desktopList = RetrieveAppDesktoWtihFilter(desktopids).OrderBy(o => o.DesktopName).ToList();

            return desktopList;

        }

        public static List<AppDesktopDto> RetrieveCurrnetUserDesktopDto()
        {
            return RetrieveDesktopDtoListByUserEntity(AppSecurityUserBL.CurrentUserEntity);
        }

        public static List<AppDesktopDto> RetrieveDesktopDtoListByUserId(int userId)
        {
            var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId);
            return RetrieveDesktopDtoListByUserEntity(userEntity);
        }

        public static List<AppDesktopDto> RetrieveDesktopDtoListByRoleId(int roleId)
        {
            List<AppDesktopDto> desktopList = new List<AppDesktopDto>();

            var roleEntity = AppSecurityGroupBL.RetrieveOneAppSecurityGroupEntity(roleId);

            if (roleEntity != null)
            {
                var desktopids = new List<int>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    IncludeFieldsList includefiled = new IncludeFieldsList();
                    includefiled.Add(AppSecuritySysObjGroupUserFields.DesktopId);

                    EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.DesktopId != DBNull.Value & AppSecuritySysObjGroupUserFields.GroupId == roleId);

                    adapter.FetchEntityCollection(availableOrgSysObjlist, includefiled, filter);



                    desktopids = availableOrgSysObjlist.Select(o => o.DesktopId.Value).ToList();

                }


                // need to remove restriction Dashboard
                desktopList = RetrieveAppDesktoWtihFilter(desktopids).OrderBy(o => o.DesktopName).ToList();
            }

            return desktopList;
        }

        private static List<AppDesktopDto> RetrieveDesktopDtoListByUserEntity(AppSecurityUserEntity userEntity)
        {
            List<AppDesktopDto> desktopList = new List<AppDesktopDto>();

            if (userEntity != null)
            {
                bool isAdmin = AppSecurityUserBL.IsAdminUser();
                int userId = AppSecurityUserBL.CurrentUserId;

                if (isAdmin)
                {


                    desktopList = AppDesktopBL.RetrieveAllAppDesktopDto(true).ToList();

                    desktopList = desktopList.Where(o => !(o.IsGlobalDefault.HasValue && o.IsGlobalDefault.Value && (int)o.Id != (int)EmAppUserType.Employee)).ToList();
                }

                else
                {

                    desktopList = GetDesktopListByUserEntity(userEntity, true).ToList();



                }

                if (desktopList.Count > 0)
                {
                    desktopList = desktopList.Where(o => !(o.OtherSettingsDto != null && o.OtherSettingsDto.IsUserDesktop && o.AppCreatedById.HasValue && o.AppCreatedById.Value != userId)).ToList();

                    var userDesktop = desktopList.Where(o => o.OtherSettingsDto != null && o.OtherSettingsDto.IsUserDesktop).ToList();

                    var domainDefaultDesktop = desktopList.Where(o => o.IsGlobalDefault.HasValue && o.IsGlobalDefault.Value).ToList();

                    var otherDesktop = desktopList.Where(o => !(o.OtherSettingsDto != null && o.OtherSettingsDto.IsUserDesktop) && !(o.IsGlobalDefault.HasValue && o.IsGlobalDefault.Value)).OrderBy(o => o.DesktopName).ToList();


                    desktopList = new List<AppDesktopDto>();

                    desktopList.AddRange(userDesktop);
                    desktopList.AddRange(domainDefaultDesktop);
                    desktopList.AddRange(otherDesktop);
                }

            }

            return desktopList;
        }


        private static ObservableSet<AppDesktopDto> GetDesktopListByUserEntity(AppSecurityUserEntity userEntity, bool isIncludeDesktopCreatedByCurrentUser = true)
        {
            var toReturn = new ObservableSet<AppDesktopDto>();

            if (userEntity != null)
            {
                var desktopids = new List<int>();


                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {

                    IncludeFieldsList includefiled = new IncludeFieldsList();
                    includefiled.Add(AppSecuritySysObjGroupUserFields.DesktopId);

                    EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.DesktopId != DBNull.Value
                        & (AppSecuritySysObjGroupUserFields.UserId == userEntity.UserId | AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds));

                    adapter.FetchEntityCollection(availableOrgSysObjlist, includefiled, filter);

                    desktopids = availableOrgSysObjlist.Select(o => o.DesktopId.Value).Distinct().ToList();

                }

                // need to remove restriction Dashboard


                toReturn = RetrieveAppDesktoWtihFilter(desktopids, isIncludeDesktopCreatedByCurrentUser);


            }

            return toReturn;

        }

        private static ObservableSet<AppDesktopDto> RetrieveAppDesktoWtihFilter(List<int> desktopids, bool isIncludeDesktopCreatedByCurrentUser = true)
        {
            var aDtoList = new ObservableSet<AppDesktopDto>();
            EntityCollection<AppDesktopEntity> list = new EntityCollection<AppDesktopEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {


                int currentUserType = AppSecurityUserBL.CurrentUserEntity.DomainId;

                if (currentUserType == (int)EmAppUserType.Employee
                    || currentUserType == (int)EmAppUserType.Customer
                    || currentUserType == (int)EmAppUserType.Supplier
                    || currentUserType == (int)EmAppUserType.ClientAgent
                    || currentUserType == (int)EmAppUserType.SupplierAgent)
                {
                    int desktopId = currentUserType;
                    desktopids.Add(desktopId);
                }

                desktopids = desktopids.Distinct().ToList();

                if (desktopids.Count > 0)
                {
                    if (isIncludeDesktopCreatedByCurrentUser)
                    {
                        adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppDesktopFields.DesktopId == desktopids | AppDesktopFields.AppCreatedById == AppSecurityUserBL.CurrentUserEntity.UserId));
                    }
                    else
                    {
                        adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppDesktopFields.DesktopId == desktopids));
                    }



                    foreach (var o in list)
                    {
                        AppDesktopDto deskTopDto = AppDesktopConverter.ConvertEntityToDto(o);
                        aDtoList.Add(deskTopDto);

                    }
                }

                return aDtoList;


            }
        }


        #endregion





        #region ---- Report security

        public static List<int> GetCurrentUserAvailableReportIds()
        {
            List<int> aList = RetrieveCurrnetUserReportDto().Select(o => (int)o.Id).ToList();

            return aList;
        }

        public static List<AppReportExDto> RetrieveCurrnetUserReportDto()
        {
            List<AppReportExDto> userReportList = new List<AppReportExDto>();
            bool isAdmin = AppSecurityUserBL.IsAdminUser();

            if (isAdmin)
            {
                userReportList = AppReportBL.RetrieveAllAppReportEntityDto().ToList();
            }
            else
            {
                userReportList = GetCurrentNoneAdminUserReportSecurity();
            }

            userReportList = userReportList.Where(o => o.IsActive.HasValue && o.IsActive.Value).ToList();

            return userReportList;
        }

        private static List<AppReportExDto> GetCurrentNoneAdminUserReportSecurity()
        {
            var toReturn = new List<AppReportExDto>();

            var ids = new List<int>();


            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    IncludeFieldsList includefiled = new IncludeFieldsList();
                    includefiled.Add(AppSecuritySysObjGroupUserFields.ReportId);

                    EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.ReportId != DBNull.Value & (
                         AppSecuritySysObjGroupUserFields.UserId == AppSecurityUserBL.CurrentUserEntity.UserId |
                        AppSecuritySysObjGroupUserFields.GroupId == AppSecurityUserBL.CurrentGroupIds
                        ));

                    adapter.FetchEntityCollection(availableOrgSysObjlist, includefiled, filter);

                    ids = availableOrgSysObjlist.Select(o => o.ReportId.Value).ToList();
                }
                catch (Exception ex)
                {
                
                }
            }




            // need to remove restriction Report
            if (ids.Count > 0)
            {

                RelationPredicateBucket deskTopFilterfilter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.ReportId == ids);

                toReturn = RetrieveAppReportWtihFilter(deskTopFilterfilter);

            }


            return toReturn;

        }

        private static List<AppReportExDto> RetrieveAppReportWtihFilter(RelationPredicateBucket filter)
        {
            var aDtoList = new List<AppReportExDto>();
            EntityCollection<AppReportEntity> list = new EntityCollection<AppReportEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {

                EntityCollection<AppSecuritySysObjGroupUserEntity> availableOrgSysObjlist = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                adapter.FetchEntityCollection(availableOrgSysObjlist, filter);

                var ids = availableOrgSysObjlist.Select(o => o.ReportId).ToArray();

                adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppReportFields.ReportId == ids));


                foreach (var o in list)
                {
                    AppReportExDto aDto = AppReportConverter.ConvertEntityToExDto(o);
                    aDtoList.Add(aDto);
                }


                return aDtoList;

            }
        }


        #endregion

    }
}