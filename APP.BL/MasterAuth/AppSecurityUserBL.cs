using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;

using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
#if NETFRAMEWORK
using System.Web;
#endif
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Framework;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using System.ComponentModel.Design;


namespace App.BL
{
    public static class AppSecurityUserBL
    {
        private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;

        // when  user  edit is done, need to refresh the  processCache

        private static readonly string AuthenticationMode = (AppConfig.Get("AuthenticationMode") ?? string.Empty).ToUpperInvariant();
        public static readonly string AppSuperuser = "AppSuperUser";// it is buint in user 

        public static bool IsAdminUser()
        {
            try
            {
                // Read CurrentLoginUserType directly from the registered identity rather than going
                // through CurrentUserEntity, which can return null if the user entity cache misses
                // (e.g. for SysAdmin users whose prefetch paths don't exist in the master DB).
                var identity = ServerContext.Instance.CurrnetClientIdentity;
                if (identity == null) return false;
                int loginType = identity.CurrentLoginUserType;
                return loginType == (int)EmAppUserType.SysAdmin || loginType == (int)EmAppUserType.SaasCompanyAdmin;
            }
            catch
            {
                return false;
            }

        }

        public static AppSecurityUserEntity CurrentUserEntity
        {
            get
            {
                //int uid = (int)ServerContext.Instance.CurrentUid;

                //int uid = 1;

                if (ServerContext.Instance.CurrentUid != null)
                {
                    int uid = (int)ServerContext.Instance.CurrentUid;

                    AppSecurityUserEntity userEntity = AppCacheManagerBL.GetOneAppSecurityUserEntityFromCache(uid);

                    if (userEntity == null) return null;

                    //!!! overrride  DomainId is used to identify user type  EmAppUserType
                    //userEntity.DomainId = ServerContext.Instance.CurrentLoginUserType;

                    return userEntity;
                }

                return null;

            }
        }


        public static bool IsPartnerUser
        {
            get
            {
                bool isPartnerUser = false;

                try
                {
                    isPartnerUser = CurrentUserEntity.DomainId == 3 || CurrentUserEntity.DomainId == 4 || CurrentUserEntity.DomainId == 5;
                }
                catch (Exception ex)
                {

                }

                return isPartnerUser;
            }
        }

        public static int CurrentUserId
        {
            get
            {
                return (int)ServerContext.Instance.CurrentUid;
            }
        }


        public static int[] CurrentGroupIds
        {
            get
            {
                List<int> toReturn = new List<int>();

                try
                {



                    List<int> currentGroupIds = AppSecurityUserBL.CurrentUserEntity.AppSecurityGroupMember.Select(o => o.GroupId).ToList();
                    toReturn.AddRange(currentGroupIds);

                    var buitGenericGroupEntiy = AppSecurityGroupBL.GetGenericBuiltInRole();
                    if (buitGenericGroupEntiy != null)
                    {
                        toReturn.Add(buitGenericGroupEntiy.GroupId);

                    }

                    var buitSpecificGroupEntiy = AppSecurityGroupBL.GetSpecificBuiltInRole();
                    if (buitSpecificGroupEntiy != null)
                    {
                        toReturn.Add(buitSpecificGroupEntiy.GroupId);

                    }


                    var buitAlluserGroupEntiy = AppSecurityGroupBL.GetGenericBuiltInAllUserRole();
                    if (buitAlluserGroupEntiy != null)
                    {
                        toReturn.Add(buitAlluserGroupEntiy.GroupId);

                    }
                }
                catch (Exception ex)
                {
                }




                return toReturn.ToArray();
            }
        }


      

        public static AppSecurityUserEntity RetrieveOneAppSecurityUserEntity(object userId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
            {


                AppSecurityUserEntity userEntity = new AppSecurityUserEntity(int.Parse(userId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);

                //rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityRegDomain);

               // rootPath.Add(AppSecurityUserEntity.PrefetchPathAppComOrganization_);

             

                adpater.FetchEntity(userEntity, rootPath);
                return userEntity;
            }
        }
     

        public static IEnumerable<AppSecurityUserEntity> RetrieveAppSecurityUserEntity(object groupId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityGroupMemberFields.GroupId == groupId);
                filter.Relations.Add(AppSecurityUserEntity.Relations.AppSecurityGroupMemberEntityUsingUserId);

                adpater.FetchEntityCollection(users, filter);
                return users;
            }
        }


        public static AppSecurityUserEntity RetrieveOneAppSecurityUserSimpleEntity(object userId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
            {

                AppSecurityUserEntity userEntity = new AppSecurityUserEntity(int.Parse(userId.ToString()));
                adpater.FetchEntity(userEntity);
                return userEntity;
            }
        }
        internal static List<AppSecurityUserDto> RetrieveAllMasterDBUsersByUserId(List<int> userIdList)
        {
            List<AppSecurityUserDto> toReturn = new List<AppSecurityUserDto>();

            if (userIdList != null && userIdList.Count > 0)
            {
                EntityCollection<AppSecurityUserEntity> userEntityList = new EntityCollection<AppSecurityUserEntity>();
                using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.UserId == userIdList);

                    adpater.FetchEntityCollection(userEntityList, filter);
                }


                foreach (var auserEntity in userEntityList)
                {
                    var dto = AppSecurityUserConverter.ConvertEntityToDto(auserEntity);
                    toReturn.Add(dto);
                }
            }

            return toReturn;
        }

        public static AppSecurityUserExDto RetrieveMasterDBUserLoginInfo(object userId)
        {
            AppSecurityUserEntity userEntity = RetrieveOneAppSecurityUserSimpleEntity(userId);
            AppSecurityUserExDto toReturn = AppSecurityUserConverter.ConvertEntityToExDto(userEntity);
            toReturn.Password = "password";
            toReturn.ConfirmPassword = toReturn.Password;
            return toReturn;
        }

        public static OperationCallResult<AppSecurityUserExDto> UpdateMasterDBUserLoginInfo(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = aAppSecurityUserExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            // prepare Data
            if (!aAppSecurityUserExDto.IsNew)
            {
                aValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(aAppSecurityUserExDto.LoginName, (int?)aAppSecurityUserExDto.Id));

                if (aValidationResult.HasErrors)
                {
                    return aOperationCallResult;
                }

                aValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(aAppSecurityUserExDto.Email, (int?)aAppSecurityUserExDto.Id));

                if (aValidationResult.HasErrors)
                {
                    return aOperationCallResult;
                }



                AppSecurityUserEntity userEntity = RetrieveOneAppSecurityUserSimpleEntity((int)aAppSecurityUserExDto.Id);
                userEntity.UserName = aAppSecurityUserExDto.UserName;
                userEntity.LoginName = aAppSecurityUserExDto.LoginName;
                userEntity.Email = aAppSecurityUserExDto.Email;
                userEntity.LanguageId = aAppSecurityUserExDto.LanguageId;
                userEntity.CultureInfoCode = aAppSecurityUserExDto.CultureInfoCode;
                userEntity.TimeZoneInfoToken = aAppSecurityUserExDto.TimeZoneInfoToken;
                userEntity.IsActive = aAppSecurityUserExDto.IsActive;
                userEntity.MenuSetting = aAppSecurityUserExDto.MenuSetting;
                userEntity.MappingExternalEmployeeAccountId = aAppSecurityUserExDto.MappingExternalEmployeeAccountId;

                //userEntity_FromUserDB.UserName = userEntity_FromMasterDB.UserName;
                //userEntity_FromUserDB.LoginName = userEntity_FromMasterDB.LoginName;
                //userEntity_FromUserDB.Password = userEntity_FromMasterDB.Password;
                //userEntity_FromUserDB.Email = userEntity_FromMasterDB.Email;
                //userEntity_FromUserDB.IsBuiltIntUser = userEntity_FromMasterDB.IsBuiltIntUser;
                //userEntity_FromUserDB.IsActive = userEntity_FromMasterDB.IsActive;
                //userEntity_FromUserDB.IsDeleted = userEntity_FromMasterDB.IsDeleted;
                //userEntity_FromUserDB.IsRegisterCompleted = userEntity_FromMasterDB.IsRegisterCompleted;
                //userEntity_FromUserDB.RefreshToken = userEntity_FromMasterDB.RefreshToken;


                if (aAppSecurityUserExDto.Password != "password")
                {
                    userEntity.Password = AppSecurityPasswordHashBL.HashPassword(aAppSecurityUserExDto.Password);
                }

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(userEntity);
                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }

                if (!aValidationResult.HasErrors)
                {
                    int userId = (int)aAppSecurityUserExDto.Id;
                    var userDto = RetrieveMasterDBUserLoginInfo(aAppSecurityUserExDto.Id);

                    AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache(userId);
                    // ResetDictAllUserDtoOneUserById is NOT called here because it queries
                    // AppSecurityGroupMember which was moved to the tenant DB.

                    aOperationCallResult.Object = userDto;

                    //if (!userDto.DefaultVendorRequestFolderId.HasValue)
                    //{
                    //    ProcessCreateUserDeaultFileFolder(userDto, aValidationResult);
                    //}
                }
            }



            return aOperationCallResult;
        }


        public static OperationCallResult<AppSecurityUserExDto> UpdateSaasUserLoginInfo(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = UpdateMasterDBUserLoginInfo(aAppSecurityUserExDto);

            if (aOperationCallResult.IsSuccessfulWithResult)
            {
                aOperationCallResult.ValidationResult = new ValidationResult();

                AppSecurityUserEntity userEntity = RetrieveOneAppSecurityUserEntity((int)aAppSecurityUserExDto.Id);
                userEntity.UserName = aAppSecurityUserExDto.UserName;
                userEntity.LoginName = aAppSecurityUserExDto.LoginName;
                userEntity.Email = aAppSecurityUserExDto.Email;
                userEntity.LanguageId = aAppSecurityUserExDto.LanguageId;
                userEntity.CultureInfoCode = aAppSecurityUserExDto.CultureInfoCode;
                userEntity.TimeZoneInfoToken = aAppSecurityUserExDto.TimeZoneInfoToken;
                userEntity.IsActive = aAppSecurityUserExDto.IsActive;
                userEntity.MenuSetting = aAppSecurityUserExDto.MenuSetting;
                userEntity.MappingExternalEmployeeAccountId = aAppSecurityUserExDto.MappingExternalEmployeeAccountId;

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(userEntity);
                        adapter.Commit();
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                        AppSecurityUserEntity currentyuserEntity = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource((int)aAppSecurityUserExDto.Id);
                        if (currentyuserEntity != null)
                        {
                            currentyuserEntity.UserName = aAppSecurityUserExDto.UserName;
                            currentyuserEntity.LoginName = aAppSecurityUserExDto.LoginName;
                            currentyuserEntity.Email = aAppSecurityUserExDto.Email;
                            currentyuserEntity.LanguageId = aAppSecurityUserExDto.LanguageId;
                            currentyuserEntity.CultureInfoCode = aAppSecurityUserExDto.CultureInfoCode;
                            currentyuserEntity.TimeZoneInfoToken = aAppSecurityUserExDto.TimeZoneInfoToken;
                            currentyuserEntity.IsActive = aAppSecurityUserExDto.IsActive;
                            currentyuserEntity.MenuSetting = aAppSecurityUserExDto.MenuSetting;
                            currentyuserEntity.MappingExternalEmployeeAccountId = aAppSecurityUserExDto.MappingExternalEmployeeAccountId;

                            AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache(currentyuserEntity.UserId);
                            ResetDictAllUserDtoOneUserById(currentyuserEntity.UserId);
                        }
                    }

                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            return aOperationCallResult;

        }




        public static EntityCollection<AppSecurityUserEntity> RetrieveSimpleAppSecurityUserEntityList(List<int> userIds)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.UserId == userIds);

                adpater.FetchEntityCollection(users, filter);
                return users;
            }
        }



        public static EntityCollection<AppSecurityUserEntity> GetUserEntityListbyUserName(string username)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.UserName == username);

                adpater.FetchEntityCollection(users, filter);
                return users;
            }
        }


        public static UserContext GetUserContextBySessionId(string sessionId)
        {
            UserContext aUserContext = InitUserContext();

            AppSecurityUserSessionEntity sessionEntity = AppSecurityUserSessionBL.GetSessionEntityBySessionID(sessionId);

            if (sessionEntity != null)
            {

                aUserContext.IsLoginFailed = false;
                aUserContext.LoginFailedErroMessage = "";

                aUserContext = SetupUserContextWithSessionEntity(sessionEntity);

                aUserContext.SessionId = sessionId;





                return aUserContext;
            }

            else
            {

                return null;

            }
            // need to keep same user context 



        }


        private static UserContext InitUserContext()
        {
            UserContext aUserContext = new UserContext();
            aUserContext.IsLoginFailed = true;
            aUserContext.LoginFailedErroMessage = "Invalid credential.";
            return aUserContext;
        }



        private static UserContext SetupUserContextWithSessionEntity(AppSecurityUserSessionEntity sessionEntity)
        {

            var aUser = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(sessionEntity.UserId);

            if (!aUser.LanguageId.HasValue)
            {
                AppLanguageEntity aDefaultlan = AppLanguageBL.RetrieveDefaultAppLanguageEntity();
                aUser.LanguageId = aDefaultlan.LanguageId;
            }

            IEnumerable<LookupItemDto> timeZonesList = RetrieveTimeZones();

            UserContext aUserContext = new UserContext()
            {
                DisplayName = aUser.UserName.ToString(),
                Email = aUser.Email.ToString(),
                DocumentId = aUser.DocumentId,
                LanguageId = aUser.LanguageId,
                UserId = aUser.UserId,
                SessionId = Guid.NewGuid().ToString(),
                CultureInfoCode = aUser.CultureInfoCode,
                //TimeZoneKey = aUser.TimeZoneInfoToken,
                IsInSysAdminDomain = aUser.IsInSysAdminDomain,
                UserCategory = (aUser.IsBuiltIntUser.HasValue && aUser.IsBuiltIntUser.Value) ? (int)EmAppUserCategory.BuiltInUser : (int)EmAppUserCategory.SaasUser,
              
                DomainId = aUser.DomainId,
                DefaultFileFolderId = aUser.DefaultVendorRequestFolderId,
                //  ResourceId = aUser.ResourceId,
                //CatalogueId = aUser.CatalogueId,
                CloseTimeout = ServerContext.Instance.CloseTimeout,
                OpenTimeout = ServerContext.Instance.OpenTimeout,
                ReceiveTimeout = ServerContext.Instance.ReceiveTimeout,
                SendTimeout = ServerContext.Instance.SendTimeout,
                ThemeId = aUser.MenuSetting,
                DefaultDesktopId = aUser.DefaultDesktopId,
                IsLoginFailed = false

            };

            // overiide UserType  DomainId = aUser.DomainId,
            AppSecurityAuthenticationBL.UpdateUserContextDomainUserType(aUserContext, sessionEntity.AppCreatedByCompanyId, aUser.UserId);



            //var timezoneLookup = timeZonesList.FirstOrDefault(o => o.Id.ToString() == aUser.TimeZoneInfoToken);
            //if (timezoneLookup != null)
            //{
            //    aUserContext.TimeZoneDisplay = timezoneLookup.Display;
            //}


            aUserContext.TimeZoneDisplay = aUser.AdloginName;

            try
            {
                AppSaasUserSessionMgtBL.RegisterUserIdentityTotheSystem(sessionEntity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RegisterUserIdentityTotheSystem failed: " + ex.Message);
            }

            aUserContext.DictAppSetup = AppTenantSettingBL.GetOrLoadCache(sessionEntity?.AppCreatedByCompanyId ?? 0);



            return aUserContext;
        }

        private static AppSecurityUserEntity DoAuthentication(string userName, string password)
        {
            AppSecurityUserEntity auser = null;


            if (AuthenticationMode == EmAppAuthenticationMode.PLM.ToString().ToUpperInvariant())
            {
                auser = APpAuthentication(userName, password);
            }
            else if (AuthenticationMode == EmAppAuthenticationMode.AD.ToString().ToUpperInvariant())
            {
                auser = ADAuthentication(userName, password);
            }

            else if (AuthenticationMode == EmAppAuthenticationMode.Mix.ToString().ToUpperInvariant())
            {
                auser = MixedAuthentication(userName, password);
            }

            string fromHost = string.Empty;
#if NETFRAMEWORK
            // TODO-PHASE4: Replace with IHttpContextAccessor
            if (HttpContext.Current != null)
            {
                fromHost = "From Host:" + HttpContext.Current.Request.UserHostAddress;
            }
#endif





            return auser;
        }

        private static void LogEventWrite(string userName, AppSecurityUserEntity auser, string fromHost)
        {
            string eventSource = ".Net  Application";
            string eventLog = "Application";

            if (!EventLog.SourceExists(eventSource))
                EventLog.CreateEventSource(eventSource, eventLog);


            if (auser == null)
            {
                string sEvent = userName + ": Failure login " + fromHost;
                EventLog.WriteEntry(eventSource, sEvent, EventLogEntryType.Error, 999);

            }
            else // Successful login
            {
                string sEvent = userName + ": Success login  " + fromHost;
                EventLog.WriteEntry(eventSource, sEvent, EventLogEntryType.SuccessAudit, 999);



            }

        }



        private static AppSecurityUserEntity APpAuthentication(string userName, string password)
        {
            using (DataAccessAdapter dapter = new DataAccessAdapter(MasterConnStr))
            {
                password = EnDeCrypt.Encrypt(password, userName);

                RelationPredicateBucket filterBucket = new RelationPredicateBucket();
                IPredicateExpression predicate = new PredicateExpression();
                predicate.Add(AppSecurityUserFields.LoginName == userName);
                predicate.AddWithAnd(AppSecurityUserFields.Password == password);
                filterBucket.PredicateExpression.Add(predicate);

                IncludeFieldsList includeFieldsList = new IncludeFieldsList();
                includeFieldsList.Add(AppSecurityUserFields.UserId);

                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                dapter.FetchEntityCollection(users, includeFieldsList, filterBucket);

                if (users.Count > 0)
                {
                    AppSecurityUserEntity aAppSecurityUserEntity = users[0];

                    return AppCacheManagerBL.GetOneAppSecurityUserEntityFromCache(aAppSecurityUserEntity.UserId);
                }
                else  // LogFailed
                {
                    //HttpContext.Current.Request.UserHostAddress
                    return null;
                }
            }
        }


        private static AppSecurityUserEntity ADAuthentication(string userName, string password)
        {
            string aUserPrincipalName = string.Empty;

            try
            {
                if (ADHelp.AuthenticateUser(userName, password, out aUserPrincipalName))
                {
                    // find PLM user by UserPrincipalName something like abc@v2k.com
                    return RetriveUserByADLoginName(aUserPrincipalName);

                }
                else
                {
                    return null;
                }
            }
            catch
            { // Ad server is not setup weel

                return null;

            }

        }

        private static AppSecurityUserEntity MixedAuthentication(string userName, string password)
        {
            // first try applcaiton itself
            var aUser = APpAuthentication(userName, password);

            if (aUser == null)
            {
                // try AD Logn
                aUser = ADAuthentication(userName, password);
            }

            return aUser;

        }
        private static AppSecurityUserEntity RetriveUserByADLoginName(string aUserPrincipalName)
        {


            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                RelationPredicateBucket filterBucket = new RelationPredicateBucket();
                IPredicateExpression predicate = new PredicateExpression();

                // need to add column ADUserPrincipalName in AppSEcurityuser Table
                //predicate.Add(AppSecurityUserFields.ADUserPrincipalName == aUserPrincipalName);

                filterBucket.PredicateExpression.Add(predicate);

                IncludeFieldsList includeFieldsList = new IncludeFieldsList();
                includeFieldsList.Add(AppSecurityUserFields.UserId);

                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                adapter.FetchEntityCollection(users, includeFieldsList, filterBucket);

                if (users.Count > 0)
                {
                    AppSecurityUserEntity aAppSecurityUserEntity = users[0];

                    return AppCacheManagerBL.GetOneAppSecurityUserEntityFromCache(aAppSecurityUserEntity.UserId);
                }


            }

            return null;
        }



        internal static AppSecurityUserEntity RetrieveOneAppSecurityUserEntityByLoginName(string loginName)
        {
            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                adapter.FetchEntityCollection(users, new RelationPredicateBucket(new PredicateExpression(AppSecurityUserFields.LoginName == loginName)));
            }

            return users.FirstOrDefault();
        }

        public static EntityCollection<AppSecurityUserEntity> RetrieveAllSystemBuiltinUserEntity()
        {
            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);

                RelationPredicateBucket filter = new RelationPredicateBucket();
                filter.PredicateExpression.AddWithAnd(AppSecurityUserFields.IsBuiltIntUser == true);


                adapter.FetchEntityCollection(list, filter, rootPath);
            }
            return list;
        }

        public static ObservableSet<AppSecurityUserDto> RetrieveAllSystemBuiltinUserDto()
        {
            EntityCollection<AppSecurityUserEntity> list = RetrieveAllSystemBuiltinUserEntity();


            List<LookupItemDto> groupLookupItems = new List<LookupItemDto>();
            AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.SecurityGroup).ForAll(aGroupDto =>
            {
                LookupItemDto lookupDto = new LookupItemDto();
                lookupDto.Id = aGroupDto.Id;
                lookupDto.Display = aGroupDto.GroupName;
                groupLookupItems.Add(lookupDto);
            });


            var aDtoList = new ObservableSet<AppSecurityUserDto>();
            foreach (var o in list)
            {
                AppSecurityUserDto aUserDto = AppSecurityUserConverter.ConvertEntityToDto(o);
                aDtoList.Add(aUserDto);

                List<int> groupids = o.AppSecurityGroupMember.Select(g => g.GroupId).ToList();
                aUserDto.UserGroups = groupLookupItems.Where(dg => groupids.Contains((int)dg.Id)).ToList();
                aUserDto.UserGroupString = EntityHelper.ConvertLookupListToString(aUserDto.UserGroups);
            }

            return aDtoList;
        }


        public static EntityCollection<AppSecurityUserEntity> RetrieveAllAppSecurityUserEntity(RelationPredicateBucket filter = null)
        {
            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);






                rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityRegDomain);

              

                rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityUserContact);
                adapter.FetchEntityCollection(list, filter, rootPath);
            }
            return list;
        }


        //public static ObservableSet<AppSecurityUserDto> BusinessPartnerUserList(int partnerId)
        //{

        //    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.BelongToBusinessPartnerId == partnerId);
        //    EntityCollection<AppSecurityUserEntity> list = RetrieveAllAppSecurityUserEntity(filter);



        //    //List<LookupItemDto> groupLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityGroup.ToString(), "");

        //    ObservableSet<AppSecurityUserDto> aDtoList = ConvertUserEntityListToExDto(list);

        //    return aDtoList;
        //}

        public static ObservableSet<AppSecurityUserDto> RetrieveAppSecurityUserDtoByCompanyId(int companyId)
        {
            // AppCreatedByCompanyId is the owner company set at user creation time (provisioning or manual).
            var filter = new RelationPredicateBucket(
                AppSecurityUserFields.AppCreatedByCompanyId == companyId);
            var list = RetrieveAllAppSecurityUserEntity(filter);
            return ConvertUserEntityListToExDto(list);
        }

        /// <summary>
        /// Retrieves users belonging to a company using only master DB — safe to call from SysAdmin context.
        /// Skips tenant-level group/domain lookups that would require a tenant DB connection.
        /// </summary>
        public static List<AppSecurityUserDto> RetrieveSimpleUserDtoListByCompanyId(int companyId)
        {
            var filter = new RelationPredicateBucket(
                AppSecurityUserFields.AppCreatedByCompanyId == companyId);
            var list = new EntityCollection<AppSecurityUserEntity>();
            using (var adapter = new DataAccessAdapter(MasterConnStr))
            {
                adapter.FetchEntityCollection(list, filter);
            }
            return list.Select(e => AppSecurityUserConverter.ConvertEntityToDto(e)).ToList();
        }

        public static ObservableSet<AppSecurityUserDto> RetrieveAllAppSecurityUserDto(bool isExcludeSystemTokenUser = false)
        {
            RelationPredicateBucket filter = null;

            if (isExcludeSystemTokenUser)
            {
                filter = new RelationPredicateBucket(
                    AppSecurityUserFields.DomainId != (int)EmAppUserType.Integration
                    & AppSecurityUserFields.DomainId != (int)EmAppUserType.CompanyAnonymousUser
                    & AppSecurityUserFields.DomainId != (int)EmAppUserType.CompanyWinScheduleUser
                    );
            }

            EntityCollection<AppSecurityUserEntity> list = RetrieveAllAppSecurityUserEntity(filter);



            //List<LookupItemDto> groupLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityGroup.ToString(), "");

            ObservableSet<AppSecurityUserDto> aDtoList = ConvertUserEntityListToExDto(list);

            return aDtoList;
        }

        public static List<AppSecurityUserSimpleDto> RetrieveAllSimpleUserDto()
        {

            List<AppSecurityUserSimpleDto> toReturn = new List<AppSecurityUserSimpleDto>();

            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);
                adapter.FetchEntityCollection(list, null, rootPath);
            }

            list.Where(o => o.DomainId < (int)EmAppUserType.AllUser)
                .ForAll(o => toReturn.Add(new AppSecurityUserSimpleDto()
                {
                    Id = o.UserId,
                    UserName = o.UserName,
                    Email = o.Email,
                    DomainId = o.DomainId
                }));

            return toReturn;
        }

        public static List<AppSecurityUserDto> RetrieveAllIntegrationTokenDto()
        {
            EntityCollection<AppSecurityUserEntity> list = RetrieveAllIntegrationTokenEntity();

            List<AppSecurityUserDto> aDtoList = new List<AppSecurityUserDto>();
            foreach (var o in list)
            {
                AppSecurityUserDto aUserDto = AppSecurityUserConverter.ConvertEntityToDto(o);
                aDtoList.Add(aUserDto);

                var sessionEntity = o.AppSecurityUserSession.FirstOrDefault();

                if (sessionEntity != null)
                {
                    var sessionDto = AppSecurityUserSessionConverter.ConvertEntityToDto(sessionEntity);

                    aUserDto.AccessToken = sessionDto.SessionId;
                    aUserDto.TokenExpirationDate = sessionDto.ExpirationDate;
                }
            }

            return aDtoList;
        }

        public static AppSecurityUserExDto RetrieveOneIntegrationTokenDto(object userId)
        {
            AppSecurityUserEntity aAppSecurityUserEntity = RetrieveOneIntegrationTokenEntity(userId);

            AppSecurityUserExDto aUserDto = AppSecurityUserConverter.ConvertEntityToExDto(aAppSecurityUserEntity);

            var sessionEntity = aAppSecurityUserEntity.AppSecurityUserSession.FirstOrDefault();

            if (sessionEntity != null)
            {
                var sessionDto = AppSecurityUserSessionConverter.ConvertEntityToDto(sessionEntity);

                aUserDto.AccessToken = sessionDto.SessionId;
                aUserDto.TokenExpirationDate = sessionDto.ExpirationDate;
            }

            return aUserDto;
        }

        public static OperationCallResult<AppSecurityUserExDto> SaveOneIntegrationTokenExDto(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserEntity aAppSecurityUserEntity;

            bool isApplicationNameUnique = IsUserNameUnique(aAppSecurityUserExDto.LoginName, (int?)aAppSecurityUserExDto.Id);

            if (isApplicationNameUnique)
            {

                aAppSecurityUserExDto.DomainId = (int)EmAppUserType.Integration;
                aAppSecurityUserExDto.Email = "";
                aAppSecurityUserExDto.LoginName = Guid.NewGuid().ToString();
                aAppSecurityUserExDto.Password = Guid.NewGuid().ToString();

                // prepare Data
                if (aAppSecurityUserExDto.IsNew)
                {
                    aAppSecurityUserEntity = new AppSecurityUserEntity();
                    // New user                 

                    AppSecurityUserConverter.CopyDtoToEntity(aAppSecurityUserEntity, aAppSecurityUserExDto);
                    aAppSecurityUserEntity.Password = aAppSecurityUserExDto.Password;
                    aAppSecurityUserEntity.AppCreatedDate = System.DateTime.UtcNow;
                    aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;
                    aAppSecurityUserEntity.CultureInfoCode = CurrentUserEntity.CultureInfoCode;
                    aAppSecurityUserEntity.TimeZoneInfoToken = CurrentUserEntity.TimeZoneInfoToken;
                    aAppSecurityUserEntity.LanguageId = CurrentUserEntity.LanguageId;
                    aAppSecurityUserEntity.GlobalGuid = new Guid();
                    aAppSecurityUserEntity.IsRegisterCompleted = true;

                    using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSecurityUserEntity);

                            adapter.Commit();
                            aAppSecurityUserExDto.Id = aAppSecurityUserEntity.UserId;

                            ResetIntegrationTokenSession(aAppSecurityUserExDto, aValidationResult);

                            if (aValidationResult.HasErrors)
                            {
                                adapter.Rollback();
                            }
                        }

                        // Entity Logical Validation Exception
                        catch (ORMEntityValidationException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "plm_AppSecurityUserEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                        }
                        // Database FK Exception .......
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "plm_AppSecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
                else
                {
                    aValidationResult.Merge(ProcessDirtyIntegrationTokenExDto(aAppSecurityUserExDto));
                }
            }
            else
            {
                aValidationResult.AddItem(null, "plm_AppSecurityUserEntity_ApplicationNameAlreadyExist", ValidationItemType.Error, "Application Name Already Exist");
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "plm_AppSecurityUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                int userId = (int)aAppSecurityUserExDto.Id;
                AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache(userId);
             
                var aUserDto = RetrieveOneIntegrationTokenDto(aAppSecurityUserExDto.Id);

                aOperationCallResult.Object = aUserDto;

            }

            return aOperationCallResult;
        }



        private static void ResetIntegrationTokenSession(AppSecurityUserExDto aAppSecurityUserExDto, ValidationResult aValidationResult)
        {
            AppSecurityUserSessionEntity aAppSecurityUserSessionEntity = new AppSecurityUserSessionEntity();
            aAppSecurityUserSessionEntity.UserId = (int)aAppSecurityUserExDto.Id;
            aAppSecurityUserSessionEntity.AppCreatedDate = System.DateTime.UtcNow;
            aAppSecurityUserSessionEntity.ExpirationDate = aAppSecurityUserExDto.TokenExpirationDate;
            aAppSecurityUserSessionEntity.ApplicationType = 1;
            aAppSecurityUserSessionEntity.AppCreatedByCompanyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

            //string sessionId = EnDeCrypt.Encrypt(aAppSecurityUserExDto.LoginName + aAppSecurityUserExDto.Password, aAppSecurityUserSessionEntity.AppCreatedDate.Value.ToString("s"));
            //sessionId = AppSecurityPasswordHashBL.HashPassword(sessionId);

            string sessionId = Guid.NewGuid().ToString();

            aAppSecurityUserSessionEntity.SessionId = sessionId;


            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(AppSecurityUserSessionFields.UserId == (int)aAppSecurityUserExDto.Id));
                    adapter.SaveEntity(aAppSecurityUserSessionEntity);

                    adapter.Commit();


                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_Save_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }

        private static ValidationResult ProcessDirtyIntegrationTokenExDto(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppSecurityUserEntity aAppSecurityUserEntity = RetrieveOneAppSecurityUserEntity(aAppSecurityUserExDto.Id);

            aAppSecurityUserExDto.LoginName = aAppSecurityUserEntity.LoginName;
            aAppSecurityUserExDto.Password = aAppSecurityUserEntity.Password;

            AppSecurityUserConverter.CopyDtoToEntity(aAppSecurityUserEntity, aAppSecurityUserExDto);
            aAppSecurityUserEntity.Password = aAppSecurityUserExDto.Password;
            aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityUserEntity);

                    adapter.Commit();

                    ResetIntegrationTokenSession(aAppSecurityUserExDto, aValidationResult);

                    if (aValidationResult.HasErrors)
                    {
                        adapter.Rollback();
                    }
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "plm_AppSecurityUserEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "plm_AppSecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static EntityCollection<AppSecurityUserEntity> RetrieveAllIntegrationTokenEntity()
        {
            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.DomainId == (int)EmAppUserType.Integration);
                rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityUserSession);

                adapter.FetchEntityCollection(list, filter, rootPath);
            }
            return list;
        }

        public static AppSecurityUserEntity RetrieveOneIntegrationTokenEntity(object userId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
            {
                AppSecurityUserEntity userEntity = new AppSecurityUserEntity(int.Parse(userId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);

                rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityUserSession);



                adpater.FetchEntity(userEntity, rootPath);
                return userEntity;
            }
        }

        private static ObservableSet<AppSecurityUserDto> ConvertUserEntityListToExDto(EntityCollection<AppSecurityUserEntity> list)
        {
            List<LookupItemDto> groupLookupItems = new List<LookupItemDto>();
            AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.SecurityGroup).ForAll(aGroupDto =>
            {
                LookupItemDto lookupDto = new LookupItemDto();
                lookupDto.Id = aGroupDto.Id;
                lookupDto.Display = aGroupDto.GroupName;
                groupLookupItems.Add(lookupDto);
            });

            List<LookupItemDto> projectTeamLookupItems = new List<LookupItemDto>();
            AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.ProjectTeam).ForAll(aGroupDto =>
            {
                LookupItemDto lookupDto = new LookupItemDto();
                lookupDto.Id = aGroupDto.Id;
                lookupDto.Display = aGroupDto.GroupName;
                projectTeamLookupItems.Add(lookupDto);
            });

            List<LookupItemDto> contactGroups = new List<LookupItemDto>();
            AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.ContactGroup).ForAll(aGroupDto =>
            {
                LookupItemDto lookupDto = new LookupItemDto();
                lookupDto.Id = aGroupDto.Id;
                lookupDto.Display = aGroupDto.GroupName;
                contactGroups.Add(lookupDto);
            });



            List<LookupItemDto> DomainLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityRegDomain.ToString(), "");



            //Dictionary<int, AppComOrganizationDto> dictOrganization = AppComOrgBL.GetAppComOrganizationDtoFlatList().ToDictionary(o => (int)o.Id, o => o);
            Dictionary<int, List<AppBusinessPartnerDto>> dictUserIdAndPartnerDtoList = GetDictPartnerUserIdAndPartnerDtoList();

            var aDtoList = new ObservableSet<AppSecurityUserDto>();
            foreach (var o in list)
            {
                AppSecurityUserDto aUserDto = AppSecurityUserConverter.ConvertEntityToDto(o);
                aDtoList.Add(aUserDto);

                var aDomainLookUpItem = DomainLookupItems.FirstOrDefault(p => (int)p.Id == aUserDto.DomainId);
                if (aDomainLookUpItem != null)
                {
                    aUserDto.DomainDispaly = aDomainLookUpItem.Display;
                }

                List<int> groupids = o.AppSecurityGroupMember.Select(g => g.GroupId).ToList();
                aUserDto.UserGroups = groupLookupItems.Where(dg => groupids.Contains((int)dg.Id)).ToList();
                aUserDto.UserGroupString = EntityHelper.ConvertLookupListToString(aUserDto.UserGroups);

                aUserDto.UserProjectTeams = projectTeamLookupItems.Where(dg => groupids.Contains((int)dg.Id)).ToList();
                aUserDto.UserProjectTeamString = EntityHelper.ConvertLookupListToString(aUserDto.UserProjectTeams);

                aUserDto.UserContactGroups = contactGroups.Where(g => groupids.Contains((int)g.Id)).ToList();


                if (o.AppSecurityUserContact != null)
                {
                    aUserDto.UserContactEmails = o.AppSecurityUserContact
                        .Where(contact => contact.ContactType.HasValue && contact.ContactType == (int)EmAppUserContactType.Email && !string.IsNullOrWhiteSpace(contact.ContactFormat))
                        .Select(c => c.ContactFormat).ToList();
                }

                PrepareUserPathString(null, dictUserIdAndPartnerDtoList, aUserDto);
            }

            return aDtoList;
        }



        private static Dictionary<int, AppSecurityUserDto> _dictAllUserDto;

        public static Dictionary<int, AppSecurityUserDto> DictAllUserDto
        {
            get
            {
                if (_dictAllUserDto == null)
                {
                    _dictAllUserDto = RetrieveAllAppSecurityUserDto().ToDictionary(p => (int)p.Id, p => p);
                }

                return _dictAllUserDto;
            }
        }

        public static void ResetDictAllUserDto()
        {
            _dictAllUserDto = RetrieveAllAppSecurityUserDto().ToDictionary(p => (int)p.Id, p => p);
        }

        public static void ResetDictAllUserDtoOneUserById(int userId)
        {
            if (_dictAllUserDto == null)
            {
                _dictAllUserDto = RetrieveAllAppSecurityUserDto().ToDictionary(p => (int)p.Id, p => p);
            }
            else
            {
                if (_dictAllUserDto.ContainsKey(userId))
                {
                    _dictAllUserDto[userId] = RetrieveOneAppSecurityUserExDto(userId);
                }
                else
                {
                    _dictAllUserDto.Add(userId, RetrieveOneAppSecurityUserExDto(userId));
                }
            }

        }

        public static Dictionary<int, string> RetrieveUsersEmails(List<int> userIds)
        {
            Dictionary<int, string> dictionaryUserMails = new Dictionary<int, string>();

            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.UserId == userIds);

                adapter.FetchEntityCollection(list, filter);
            }

            foreach (AppSecurityUserEntity userEntity in list)
            {
                dictionaryUserMails.Add(userEntity.UserId, userEntity.Email);
            }

            return dictionaryUserMails;
        }

        public static Dictionary<int, string> RetrieveUsersName(List<int> userIds)
        {
            Dictionary<int, string> dictionaryUserMails = new Dictionary<int, string>();

            EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.UserId == userIds);

                adapter.FetchEntityCollection(list, filter);
            }

            foreach (AppSecurityUserEntity userEntity in list)
            {
                dictionaryUserMails.Add(userEntity.UserId, userEntity.UserName);
            }

            return dictionaryUserMails;
        }






        public static AppSecurityUserExDto RetrieveOneAppSecurityUserExDtoByLoginName(string loginName)
        {
            AppSecurityUserEntity aAppSecurityUserEntity = RetrieveOneAppSecurityUserEntityByLoginName(loginName);

            AppSecurityUserExDto aUserDto = null;

            if (aAppSecurityUserEntity != null)
            {
                aUserDto = RetrieveUserRelatedExDtos(aAppSecurityUserEntity);
            }

            return aUserDto;
        }


        public static AppSecurityUserExDto RetrieveOneAppSecurityUserExDto(object userId)
        {
            AppSecurityUserEntity aAppSecurityUserEntity = RetrieveOneAppSecurityUserEntity(userId);



            AppSecurityUserExDto aUserDto = RetrieveUserRelatedExDtos(aAppSecurityUserEntity);

            if (aUserDto != null)
            {
                if (aUserDto.DomainId == (int)EmAppUserType.Supplier
                    || aUserDto.DomainId == (int)EmAppUserType.Customer
                    || aUserDto.DomainId == (int)EmAppUserType.ClientAgent
                    || aUserDto.DomainId == (int)EmAppUserType.SupplierAgent)
                {
                    AppBusinessPartnerDto partnerDto = AppBusinessPartnerBL.GetCurrentCompanyPartnerDtoByUserId((int)aUserDto.Id);

                    if (partnerDto != null)
                    {
                        aUserDto.PartnerDtoInCurrentCompany = partnerDto;
                    }
                }
            }



            return aUserDto;
        }





        public static AppSecurityUserExDto RetrieveUserExDtoByDomainBusinessUser(object domainId, object businessUserId)
        {

            if (domainId != null && businessUserId != null)
            {
                EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);

                    RelationPredicateBucket filter = new RelationPredicateBucket(
                        AppSecurityUserFields.LoginName != AppSecurityUserBL.AppSuperuser
                        & AppSecurityUserFields.DomainId == domainId
                       );

                    rootPath.Add(AppSecurityUserEntity.PrefetchPathAppSecurityRegDomain);

                   
    
                    adapter.FetchEntityCollection(list, filter, rootPath);
                }

                if (list.Count > 0)
                {
                    return RetrieveOneAppSecurityUserExDto(list.First().UserId);
                }

            }

            return null;
        }

        public static AppSecurityUserEntity FindUserEntityByLoginName(string loginName)
        {
            if (!string.IsNullOrWhiteSpace(loginName))
            {
                PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.LoginName == loginName);

                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
                }

                if (users.Count > 0)
                {
                    return users.First();
                }
            }

            return null;
        }

        //public static AppSecurityUserEntity FindUserEntityByEmailAddress_OnMasterDB(string emailAddress)
        //{
        //    if (!string.IsNullOrWhiteSpace(emailAddress))
        //    {
        //        PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.Email == emailAddress);

        //        EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

        //        using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
        //        {
        //            adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
        //        }

        //        if (users.Count > 0)
        //        {
        //            return users.First();
        //        }
        //    }

        //    return null;
        //}

        //public static AppSecurityUserEntity FindUserEntityByEmailAddress_OnUserDB(string emailAddress)
        //{
        //    if (!string.IsNullOrWhiteSpace(emailAddress))
        //    {
        //        PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.Email == emailAddress);

        //        EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

        //        using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
        //        {
        //            adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
        //        }

        //        if (users.Count > 0)
        //        {
        //            return users.First();
        //        }
        //    }

        //    return null;
        //}

        public static AppSecurityUserEntity FindUserEntityByEmailAddress(string emailAddress)
        {
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.Email == emailAddress);

                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
                }

                if (users.Count > 0)
                {
                    return users.First();
                }
            }

            return null;
        }

        private static AppSecurityUserExDto RetrieveUserRelatedExDtos(AppSecurityUserEntity aAppSecurityUserEntity)
        {
            AppSecurityUserExDto aUserDto = AppSecurityUserConverter.ConvertEntityToExDto(aAppSecurityUserEntity);

            if (aAppSecurityUserEntity.AppSecurityRegDomain != null)
            {
                aUserDto.ForeignAppSecurityRegDomainExDto = AppSecurityRegDomainConverter.ConvertEntityToExDto(aAppSecurityUserEntity.AppSecurityRegDomain);

            }




            foreach (var o in aAppSecurityUserEntity.AppSecurityGroupMember)
            {
                AppSecurityGroupMemberExDto aAppSecurityGroupMemberExDto = AppSecurityGroupMemberConverter.ConvertEntityToExDto(o);
                aUserDto.AppSecurityGroupMemberList.Add(aAppSecurityGroupMemberExDto);
            }

            if (aUserDto.OrganizationId.HasValue && aUserDto.MyOwnCompnanyId.HasValue)
            {
                //var organizationDto = AppComOrgBL.GetAppComOrganizationDtoFlatList(aUserDto.MyOwnCompnanyId.Value).FirstOrDefault(o => (int)o.Id == aUserDto.OrganizationId.Value);
                //if (organizationDto != null)
                //{
                //    aUserDto.OrganizationPath = organizationDto.PathName;
                //}
            }


            if (aAppSecurityUserEntity.DomainId == (int)EmAppUserType.SysAdmin || aAppSecurityUserEntity.DomainId == (int)EmAppUserType.SaasCompanyAdmin)
            {
                aUserDto.UserTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.AdminUserTransaction);
            }
            else if (aAppSecurityUserEntity.DomainId == (int)EmAppUserType.Employee)
            {
                aUserDto.UserTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EmployeeUserTransaction);
            }
            else if (aAppSecurityUserEntity.DomainId == (int)EmAppUserType.Supplier)
            {
                aUserDto.UserTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierUserTransaction);
            }
            else if (aAppSecurityUserEntity.DomainId == (int)EmAppUserType.Customer)
            {
                aUserDto.UserTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerUserTransaction);
            }
            else if (aAppSecurityUserEntity.DomainId == (int)EmAppUserType.ClientAgent)
            {
                aUserDto.UserTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.ClientAgentUserTransaction);
            }
            else if (aAppSecurityUserEntity.DomainId == (int)EmAppUserType.SupplierAgent)
            {
                aUserDto.UserTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierAgentUserTransaction);
            }

            return aUserDto;
        }


        public static ValidationResult CheckNewUserLoginNameAndEmailConfilict(AppSecurityUserExDto appSecurityUserExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (!string.IsNullOrWhiteSpace(appSecurityUserExDto.Email))
            {
                aValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(appSecurityUserExDto.Email, null));
                if (aValidationResult.HasErrors)
                {
                    return aValidationResult;
                }
            }

            if (!string.IsNullOrWhiteSpace(appSecurityUserExDto.LoginName))
            {
                aValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(appSecurityUserExDto.LoginName, null));
                if (aValidationResult.HasErrors)
                {
                    return aValidationResult;
                }
            }

            return aValidationResult;
        }

        public static bool IsUserNameUnique(string username, int? userId)
        {
            PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.UserName == username);

            if (userId.HasValue)
            {
                predicateExpression.Add(AppSecurityUserFields.UserId != userId);
            }

            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
            }

            return users.Count == 0;
        }

        public static ValidationResult IsUserLoginNameUnique(string login, int? userId)
        {
            ValidationResult validationResult = new ValidationResult();
            PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.LoginName == login);

            if (userId.HasValue)
            {
                predicateExpression.AddWithAnd(AppSecurityUserFields.UserId != userId);
            }

            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
            }

            if (users.Count > 0)
            {
                validationResult.AddItem(null, "app_SecurityUserEntity_LoginNameAlreadyExist", ValidationItemType.Error, "Login Name Already Exist");
            }

            return validationResult;
        }


        public static ValidationResult IsUserEmailUnique(string email, int? userId)
        {
            ValidationResult validationResult = new ValidationResult();
            PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.Email == email);

            if (userId.HasValue)
            {
                predicateExpression.AddWithAnd(AppSecurityUserFields.UserId != userId);
            }

            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
            }

            if (users.Count > 0)
            {
                validationResult.AddItem(null, "app_SecurityUserEntity_UserEmailAlreadyExist", ValidationItemType.Error, "User Email Already Exist");
            }

            return validationResult;
        }


        //public static AppSecurityUserEntity FindFirstUserEntityByEmail(string email)
        //{
        //    EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

        //    if (!string.IsNullOrWhiteSpace(email))
        //    {
        //        using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
        //        {
        //            PredicateExpression predicateExpression = new PredicateExpression(AppSecurityUserFields.Email == email);
        //            adapter.FetchEntityCollection(users, new RelationPredicateBucket(predicateExpression));
        //        }
        //    }

        //    return users.FirstOrDefault();
        //}

        public static int? GetOrCreateUserFileFolderId(int? userId)
        {
            int? fileFolderId = null;

            if (userId.HasValue)
            {
                var userDto = RetrieveOneAppSecurityUserExDto(userId);

                if (!userDto.DefaultVendorRequestFolderId.HasValue)
                {
                    OperationCallResult<AppSefolderExDto> createFolderResult = AppSecurityUserBL.CreateUserFileFolder(userDto);

                    AppSefolderExDto folderDto = createFolderResult.Object;

                    if (folderDto != null && folderDto.Id != null)
                    {
                        fileFolderId = (int)folderDto.Id;
                    }
                }
                else
                {
                    fileFolderId = userDto.DefaultVendorRequestFolderId;
                }
            }

            return fileFolderId;
        }

        public static AppSecurityUserExDto GetUserInfoWithAutoGenerateFileFolderId(int? userId)
        {
            if (userId.HasValue)
            {
                GetOrCreateUserFileFolderId(userId);
                var userDto = RetrieveOneAppSecurityUserExDto(userId);

                return userDto;
            }

            return null;
        }

        public static OperationCallResult<AppSecurityUserExDto> SaveAppSecurityUserExDto(AppSecurityUserExDto aAppSecurityUserExDto, string newpassowrd = "")
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            // bool isExternalSelfReg = aAppSecurityUserExDto.IsExternalUserSelfRegistration.HasValue && aAppSecurityUserExDto.IsExternalUserSelfRegistration.Value;


            //if (aAppSecurityUserExDto.IsNew && isExternalSelfReg)
            //{
            //    //??????
            //    int? domainId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EshopUserDomain);
            //    aAppSecurityUserExDto.DomainId = domainId.Value;
            //    if (!aAppSecurityUserExDto.LanguageId.HasValue)
            //    {
            //        int defaultLanguageId = AppLanguageBL.RetrieveDefaultAppLanguageEntity().LanguageId;
            //        aAppSecurityUserExDto.LanguageId = defaultLanguageId;
            //    }
            //    if (string.IsNullOrEmpty(aAppSecurityUserExDto.TimeZoneInfoToken))
            //    {

            //    }
            //}

            var aValidationResult = aAppSecurityUserExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserEntity aAppSecurityUserEntity;


            aValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(aAppSecurityUserExDto.LoginName, (int?)aAppSecurityUserExDto.Id));

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            aValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(aAppSecurityUserExDto.Email, (int?)aAppSecurityUserExDto.Id));

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }


            //if (!aAppSecurityUserExDto.DefaultVendorRequestFolderId.HasValue)
            //{
            //    OperationCallResult<AppSefolderExDto> saveFolderResult = CreateUserFileFolder(aAppSecurityUserExDto.UserName);

            //    if (saveFolderResult.ValidationResult.HasErrors)
            //    {
            //        aValidationResult.Merge(saveFolderResult.ValidationResult);
            //        aOperationCallResult.ValidationResult = aValidationResult;
            //        return aOperationCallResult;
            //    }
            //    else
            //    {
            //        aAppSecurityUserExDto.DefaultVendorRequestFolderId = ControlTypeValueConverter.ConvertValueToInt(saveFolderResult.Object.Id);
            //    }
            //}

            // prepare Data
            if (aAppSecurityUserExDto.IsNew)
            {
                aAppSecurityUserEntity = new AppSecurityUserEntity();
                AppSecurityUserConverter.CopyDtoToEntity(aAppSecurityUserEntity, aAppSecurityUserExDto);
                aAppSecurityUserEntity.AppCreatedDate = System.DateTime.UtcNow;
                aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;

                foreach (var aAppSecurityGroupMemberDto in aAppSecurityUserExDto.AppSecurityGroupMemberList)
                {
                    AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity = new AppSecurityGroupMemberEntity();
                    AppSecurityGroupMemberConverter.CopyDtoToEntity(aAppSecurityGroupMemberEntity, aAppSecurityGroupMemberDto);
                    aAppSecurityUserEntity.AppSecurityGroupMember.Add(aAppSecurityGroupMemberEntity);
                }



                string insertDomainUserEntity = string.Empty;
                string getlastid = string.Empty;


                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    try
                    {

                        int? userId = null;
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppSecurityUserEntity);

                        userId = aAppSecurityUserEntity.UserId;







                        adapter.Commit();
                        aAppSecurityUserExDto.Id = userId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                        SendUserNameAndPassword(aAppSecurityUserExDto.LoginName, aAppSecurityUserExDto.Email, newpassowrd);
                    }


                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else if (aAppSecurityUserExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppSecurityUserExDto(aAppSecurityUserExDto));
            }


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                ProcessMessageTemplates(aAppSecurityUserExDto, aValidationResult);



                int userId = (int)aAppSecurityUserExDto.Id;
                AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache(userId);
                ResetDictAllUserDtoOneUserById(userId);

                var ToReturnUserObj = RetrieveOneAppSecurityUserExDto(aAppSecurityUserExDto.Id);

                //if (!aAppSecurityUserExDto.DefaultVendorRequestFolderId.HasValue)
                //{
                //    ProcessCreateUserDeaultFileFolder(ToReturnUserObj, aValidationResult);
                //}

                aOperationCallResult.Object = ToReturnUserObj;
                ResetDictAllUserDto();

            }

            return aOperationCallResult;
        }
        private static void ProcessMessageTemplates(AppSecurityUserExDto aAppSecurityUserExDto, ValidationResult aValidationResult)
        {

        }

        private static void ProcessCreateUserDeaultFileFolder(AppSecurityUserExDto aAppSecurityUserExDto, ValidationResult aValidationResult)
        {
            if (aAppSecurityUserExDto != null && !aAppSecurityUserExDto.IsNew && !aAppSecurityUserExDto.DefaultVendorRequestFolderId.HasValue)
            {
                OperationCallResult<AppSefolderExDto> createFolderResult = CreateUserFileFolder(aAppSecurityUserExDto);

                if (createFolderResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_DefaultFileFolderCreationFailed", ValidationItemType.Warning, "Default file folder creation failed."));
                }
                else
                {
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_DefaultFileFolderCreatedSuccessfully", ValidationItemType.Message, "Default file folder created successfully."));
                }
            }
        }

        public static OperationCallResult<AppSefolderExDto> CreateUserFileFolder(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            OperationCallResult<AppSefolderExDto> toReturn = new OperationCallResult<AppSefolderExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            toReturn.ValidationResult = aValidationResult;

            if (aAppSecurityUserExDto != null && !aAppSecurityUserExDto.IsNew && !aAppSecurityUserExDto.DefaultVendorRequestFolderId.HasValue)
            {
                int? usersListRootFolderId = AppTenantSettingBL.GetIntValue(EmTenantSettings.UsersFolderId);

                if (usersListRootFolderId.HasValue)
                {
                    AppSefolderExDto userFolder = new AppSefolderExDto();
                    userFolder.ParentId = usersListRootFolderId;
                    userFolder.Name = aAppSecurityUserExDto.UserName;
                    userFolder.FolderType = (int)EmAppTransBusinessType.File;

                    AppSefolderEntity aAppSefolderEntity = new AppSefolderEntity();
                    AppSefolderConverter.CopyDtoToEntity(aAppSefolderEntity, userFolder);

                    aAppSefolderEntity.AppCreatedById = (int)aAppSecurityUserExDto.Id;

                    using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSefolderEntity);
                            adapter.Commit();



                        }
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_CreateUserFileFolder_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }

                    using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                    {
                        try
                        {
                            AppSecurityUserEntity updateUserEntity = new AppSecurityUserEntity();
                            updateUserEntity.DefaultVendorRequestFolderId = aAppSefolderEntity.FolderId;
                            adapter.UpdateEntitiesDirectly(updateUserEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == (int)aAppSecurityUserExDto.Id));

                            //aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_DefaultFileFolderCreatedSuccessfully", ValidationItemType.Message, "User default file folder created successfully."));

                            AppCacheManagerBL.ResetMasterDBAppSecurityUserEntityCache((int)aAppSecurityUserExDto.Id);
                            aAppSecurityUserExDto = RetrieveOneAppSecurityUserExDto(aAppSecurityUserExDto.Id);


                        }
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                        }
                    }

                    toReturn.Object = AppSeFolderBL.RetrieveOneAppSefolderExDto(aAppSefolderEntity.FolderId);


                }
            }
            return toReturn;
        }



        private static ValidationResult ProcessDirtyAppSecurityUserExDto(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            // need to vlidate not null fields
            //foreach (string manp in AppSecurityUserExDto.MandatoryProperties)
            //{
            //    object a = typeof(AppSecurityUserExDto).GetProperty(manp).GetValue(aAppSecurityUserExDto,null);
            //    // aAppSecurityUserExDto.

            //}


            int[] dirtyGroupMemberIds = aAppSecurityUserExDto.AppSecurityGroupMemberList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppSecurityUserEntity aAppSecurityUserEntity = RetrieveOneAppSecurityUserEntity(aAppSecurityUserExDto.Id);



            Dictionary<int, AppSecurityGroupMemberEntity> dictAppSecurityGroupMemberFromDbms = aAppSecurityUserEntity.AppSecurityGroupMember.ToDictionary(o => o.RoleMemberId, o => o);


            //if (aAppSecurityUserExDto.Password == "password")
            //{
            //    aAppSecurityUserExDto.Password = aAppSecurityUserExDto.ConfirmPassword = aAppSecurityUserEntity.Password;
            //}

            string newPassword = aAppSecurityUserExDto.Password;

            if (aAppSecurityUserExDto.IsBuiltIntUser.HasValue && aAppSecurityUserExDto.IsBuiltIntUser.Value)
            {
                if (newPassword != "password")
                {
                    aAppSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(aAppSecurityUserExDto.Password);
                }
                else
                {
                    aAppSecurityUserExDto.Password = aAppSecurityUserEntity.Password;
                }
            }
            else
            {
                aAppSecurityUserExDto.Password = aAppSecurityUserEntity.Password;
            }




            AppSecurityUserConverter.CopyDtoToEntity(aAppSecurityUserEntity, aAppSecurityUserExDto);

            aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;

            //------- check  Group  member

            // new Items
            foreach (AppSecurityGroupMemberExDto aChildDto in aAppSecurityUserExDto.AppSecurityGroupMemberList.FindNewItems())
            {
                AppSecurityGroupMemberEntity aNewChildEntity = new AppSecurityGroupMemberEntity();
                AppSecurityGroupMemberConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppSecurityUserEntity.AppSecurityGroupMember.Add(aNewChildEntity);
            }

            // Dirty items, only the update item remove from dbms, no need to update that itmes
            foreach (var modifyitem in aAppSecurityUserExDto.AppSecurityGroupMemberList.FindModifiedItems())
            {
                if (!modifyitem.IsNew)
                {
                    int dtoKey = int.Parse(modifyitem.Id.ToString());
                    if (dictAppSecurityGroupMemberFromDbms.ContainsKey(dtoKey))
                    {
                        AppSecurityGroupMemberConverter.CopyDtoToEntity(dictAppSecurityGroupMemberFromDbms[dtoKey], modifyitem);
                    }
                }
            }

            // deletedIDs
            int[] deletSecurityGroupMemberIDs = aAppSecurityUserExDto.AppSecurityGroupMemberList.FindDeletedItemIds().Cast<int>().ToArray();


            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityUserEntity);

                    //1: need to delete deletdmSecurityDivUserGroupMemberIDs


                    // seurity group memeber id

                    if (deletSecurityGroupMemberIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityGroupMemberEntity), new RelationPredicateBucket(AppSecurityGroupMemberFields.RoleMemberId == deletSecurityGroupMemberIDs));
                    }

                    adapter.Commit();
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                if (!(aAppSecurityUserExDto.IsBuiltIntUser.HasValue && aAppSecurityUserExDto.IsBuiltIntUser.Value))
                {
                    aAppSecurityUserExDto.Password = aAppSecurityUserExDto.ConfirmPassword = newPassword;
                    var masterDbUpdateResult = UpdateMasterDBUserLoginInfo(aAppSecurityUserExDto);

                    if (!masterDbUpdateResult.IsSuccessful)
                    {
                        aValidationResult.Merge(masterDbUpdateResult.ValidationResult);
                    }

                    aAppSecurityUserExDto.Password = aAppSecurityUserExDto.ConfirmPassword = "password";
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
            }

            return aValidationResult;
        }

        // Delete a User
        public static OperationCallResult<object> DeleteAppSecurityUser(object userId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();
            string referMsg = string.Empty;

            var userEntity = RetrieveOneAppSecurityUserEntity(userId);

            if (userEntity != null)
            {
                bool isBuiltInUser = userEntity.IsBuiltIntUser.HasValue && userEntity.IsBuiltIntUser.Value;
                if (!isBuiltInUser)
                {
                    using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(AppSecurityUserSessionFields.UserId == userId));
                            adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserEntity), new RelationPredicateBucket(AppSecurityUserFields.UserId == userId));
                            adapter.Commit();
                        }

                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }

                        // if no any errors
                        if (!aValidationResult.ValidationResult.HasErrors)
                        {
                            aValidationResult.Object = userId;
                        }
                    }
                }

                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(AppSecurityUserSessionFields.UserId == userId));
                            adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserEntity), new RelationPredicateBucket(AppSecurityUserFields.UserId == userId));
                            adapter.Commit();
                            aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "app_SecurityUserEntity_Delete_Ok", ValidationItemType.Message, "Delete Successful"));
                        }

                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }

                        // if no any errors
                        if (!aValidationResult.ValidationResult.HasErrors)
                        {
                            aValidationResult.Object = userId;
                        }
                    }

                }
            }
            else
            {
                aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "app_SecurityUserEntity_UserDoesNotExistsError", ValidationItemType.Error, "User does not exist."));
            }

            return aValidationResult;
        }

        public static IEnumerable<LookupItemDto> RetrieveTimeZones()
        {
            List<LookupItemDto> timeZoneLookups = new List<LookupItemDto>();

            foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
            {
                timeZoneLookups.Add(new LookupItemDto { Id = zone.Id, Display = zone.DisplayName });
            }

            return timeZoneLookups;
        }

        public static List<LookupItemDto> GetCultroInfos()
        {
            List<LookupItemDto> cultroInfoLookups = new List<LookupItemDto>();

            foreach (var key in AppSecurityUserDto.DictCultroInfo.Keys)
            {
                cultroInfoLookups.Add(new LookupItemDto { Id = key, Display = AppSecurityUserDto.DictCultroInfo[key] });
            }

            return cultroInfoLookups;
        }






        //public static string ConvertLookupListToString(List<LookupItemDto> lookUpList)
        //{

        //    if (lookUpList == null)
        //    {
        //        return string.Empty;
        //    }

        //    //return string.Join(",", list.Select(o => o.Display));

        //    return string.Join(", ", lookUpList.Select(o =>
        //    {
        //        if (o == null || o.Display == null)
        //        {
        //            return "";
        //        }

        //        var index = o.Display.IndexOf("|");

        //        if (index > 0)
        //        {
        //            return o.Display.Substring(0, index);
        //        }
        //        else
        //        {
        //            return o.Display;
        //        }
        //    }));
        //}



        public static UserContext SendUserNameAndPassword(string loginName, string toEmailAddress, string newPassword = "")
        {

            UserContext aUserContext = new UserContext();

            //  AppSecurityUserEntity auser = DoAuthentication(userName, password);

            if (loginName.IsEmpty() || toEmailAddress.IsEmpty())
            {

                aUserContext.IsLoginFailed = true;
                aUserContext.LoginFailedErroMessage = "Invalid Login name or Passwrod";

                return aUserContext;

            }

            else
            {
                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
                {

                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.LoginName == loginName);
                    filter.PredicateExpression.Add(AppSecurityUserFields.Email == toEmailAddress);

                    adpater.FetchEntityCollection(users, filter);

                }

                if (users.Count > 0)
                {
                    var userEntity = users[0];

                    aUserContext.IsLoginFailed = false;
                    // aUserContext.TimeZoneDisplay = users[0].DecrypedPassword;

                    if (string.IsNullOrWhiteSpace(newPassword))
                    {
                        string passpwrod = Guid.NewGuid().ToString();


                        string hasssavepwad = AppSecurityPasswordHashBL.HashPassword(passpwrod);

                        userEntity.Password = hasssavepwad;
                        using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                        {


                            adapter.UpdateEntitiesDirectly(userEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == userEntity.UserId));




                        }

                        newPassword = passpwrod;

                    }




                    string appHostUrl = string.Empty;
#if NETFRAMEWORK
                    if (HttpContext.Current != null)
                    {
                        appHostUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + HttpContext.Current.Request.ApplicationPath;
                        appHostUrl = appHostUrl.Trim();
                        if (!appHostUrl.EndsWith("/"))
                        {
                            appHostUrl = appHostUrl + "/";
                        }

                    }
#endif


                    // need to update user password

                    string messageBody = string.Format(@" The following is the detail of your account <br>" + System.Environment.NewLine +
                                @"URL: {0} <br> " + System.Environment.NewLine +
                                "Username: {1} <br> " + System.Environment.NewLine +
                                "Temprary Password: {2} <br> ", appHostUrl, loginName, newPassword);


                    EmailHelper.SmtpEamilSend(toEmailAddress, appHostUrl + " account info ", messageBody);
                    aUserContext.LoginFailedErroMessage = "Please check your Inbox and change the password asap";

                    return aUserContext;

                }
                else
                {

                    aUserContext.IsLoginFailed = false;
                    aUserContext.LoginFailedErroMessage = "Cannot find the user";

                    return aUserContext;

                }

            }

        }


        //public static AppSecurityUserEntity RetrievePLMSSystemEmailReceiver()
        //{
        //    int? systemUserId = AppTenantSettingBL.GetIntValue(EmTenantSettings.AppSystemEmailReceiver);
        //    if (systemUserId.HasValue)
        //    {
        //        return AppCacheManagerBL.GetOneAppSecurityUserEntityFromCache(systemUserId.Value);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}


        public static List<AppSecurityUserDto> GetCompanyUserDtoList(int companyId)
        {
            RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserFields.MyOwnCompnanyId == companyId);
            EntityCollection<AppSecurityUserEntity> list = RetrieveAllAppSecurityUserEntity(filter);
            List<AppSecurityUserDto> aDtoList = ConvertUserEntityListToExDto(list).ToList();

            return aDtoList;
        }

        public static int[] CurrentContactGroupIds
        {
            get
            {
                int uid = (int)ServerContext.Instance.CurrentUid;

                List<int> ContactGroupIds = AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType((int)EmAppGroupUsage.ContactGroup)
                    .Select(o => (int)o.Id).ToList();

                return CurrentUserEntity.AppSecurityGroupMember.Where(o => ContactGroupIds.Contains(o.GroupId)).Select(o => o.GroupId).ToArray();
            }
        }

        public static List<AppSecurityUserSimpleDto> RetrieveCurrentUserAvailableEmailToUsers()
        {
            List<AppSecurityUserSimpleDto> toReturn = new List<AppSecurityUserSimpleDto>();

            try
            {
                List<AppSecurityUserDto> userList = DictAllUserDto.Values.OrderBy(o => o.UserName).ToList();

                bool isCurrentUserPartner = false;
                Dictionary<int, List<AppBusinessPartnerDto>> dictUserIdAndPartnerDtoList = null;
                List<int> currentUsrePartnerIdList = null;


                if (CurrentUserEntity.DomainId == (int)EmAppUserType.Supplier
                            || CurrentUserEntity.DomainId == (int)EmAppUserType.Customer
                            || CurrentUserEntity.DomainId == (int)EmAppUserType.ClientAgent
                            || CurrentUserEntity.DomainId == (int)EmAppUserType.SupplierAgent)
                {
                    isCurrentUserPartner = true;
                    dictUserIdAndPartnerDtoList = GetDictPartnerUserIdAndPartnerDtoList();

                    if (dictUserIdAndPartnerDtoList.ContainsKey(CurrentUserEntity.UserId))
                    {
                        currentUsrePartnerIdList = dictUserIdAndPartnerDtoList[CurrentUserEntity.UserId].Select(o => (int)o.Id).ToList();
                    }
                }



                // need to remove password display 
                foreach (var userDto in userList)
                {
                    if (!(userDto.IsBuiltIntUser.HasValue && userDto.IsBuiltIntUser.Value))
                    {



                        userDto.UserGroupString = EntityHelper.ConvertLookupListToString(userDto.UserGroups);
                        userDto.UserGroups = userDto.UserGroups.Where(o => CurrentContactGroupIds.Contains((int)o.Id)).ToList();

                        AppSecurityUserSimpleDto userObj = new AppSecurityUserSimpleDto();

                        userObj.Id = userDto.Id;
                        userObj.UserName = userDto.UserName;
                        //userObj.DocumentId = userDto.DocumentId;
                        userObj.Email = userDto.Email;
                        //userObj.UserGroups = userDto.UserGroups;
                        userObj.UserContactEmails = userDto.UserContactEmails;
                        userObj.UserContactGroups = userDto.UserContactGroups;

                        if (!string.IsNullOrWhiteSpace(userDto.Email) && CurrentUserEntity != null)
                        {
                            bool isTargetUserPartner = userDto.DomainId == (int)EmAppUserType.Supplier
                                || userDto.DomainId == (int)EmAppUserType.Customer
                                || userDto.DomainId == (int)EmAppUserType.ClientAgent
                                || userDto.DomainId == (int)EmAppUserType.SupplierAgent;

                            if (isCurrentUserPartner && isTargetUserPartner)
                            {
                                if (currentUsrePartnerIdList != null && currentUsrePartnerIdList.Count > 0)
                                {
                                    if (dictUserIdAndPartnerDtoList.ContainsKey((int)userDto.Id))
                                    {
                                        List<int> targetUsrePartnerIdList = dictUserIdAndPartnerDtoList[(int)userDto.Id].Select(o => (int)o.Id).ToList();

                                        foreach (int targetUserPartnerId in targetUsrePartnerIdList)
                                        {
                                            if (currentUsrePartnerIdList.Contains(targetUserPartnerId))
                                            {
                                                toReturn.Add(userObj);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            else
                            {
                                toReturn.Add(userObj);
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {

            }

            return toReturn;
        }

        private static void PrepareUserPathString(Dictionary<int, AppComOrganizationDto> dictOrganization, Dictionary<int, List<AppBusinessPartnerDto>> dictUserIdAndPartnerDtoList, AppSecurityUserDto aUserDto)
        {
            if (aUserDto.DomainId == (int)EmAppUserType.Employee)
            {
                if (dictOrganization != null && aUserDto.OrganizationId.HasValue && dictOrganization.ContainsKey(aUserDto.OrganizationId.Value))
                {
                    aUserDto.OrganizationPath = dictOrganization[aUserDto.OrganizationId.Value].PathName;
                }
            }
            else if (aUserDto.DomainId == (int)EmAppUserType.Customer
                || aUserDto.DomainId == (int)EmAppUserType.Supplier
                || aUserDto.DomainId == (int)EmAppUserType.ClientAgent
                || aUserDto.DomainId == (int)EmAppUserType.SupplierAgent)
            {
                if (dictUserIdAndPartnerDtoList.ContainsKey((int)aUserDto.Id))
                {
                    aUserDto.OrganizationPath = string.Empty;
                    dictUserIdAndPartnerDtoList[(int)aUserDto.Id].ForAll(o => aUserDto.OrganizationPath += (o.Code + ", "));
                    if (aUserDto.OrganizationPath.EndsWith(", "))
                    {
                        aUserDto.OrganizationPath = aUserDto.OrganizationPath.Substring(0, aUserDto.OrganizationPath.Length - 2);
                    }
                }
            }
        }

        private static Dictionary<int, List<AppBusinessPartnerDto>> GetDictPartnerUserIdAndPartnerDtoList()
        {
            Dictionary<int, List<AppBusinessPartnerDto>> dictUserIdAndPartnerDtoList = new Dictionary<int, List<AppBusinessPartnerDto>>();

            var partnerEntities = new EntityCollection<AppBusinessPartnerEntity>();
            var invitedUserEntities = new EntityCollection<AppBusinessPartnerInviteUserEntity>();

            using (DataAccessAdapter adapater = new DataAccessAdapter(MasterConnStr))
            {
                adapater.FetchEntityCollection(partnerEntities, null);
                adapater.FetchEntityCollection(invitedUserEntities, null);
            }

            foreach (var aInvitedUserEntity in invitedUserEntities)
            {
                if (aInvitedUserEntity.UserId.HasValue && aInvitedUserEntity.AppBusinessPartnerId.HasValue)
                {
                    int userId = aInvitedUserEntity.UserId.Value;
                    int partnerId = aInvitedUserEntity.AppBusinessPartnerId.Value;
                    AppBusinessPartnerEntity partnerEntity = partnerEntities.FirstOrDefault(o => o.AppBusinessPartnerId == partnerId);

                    if (partnerEntity != null)
                    {
                        if (!dictUserIdAndPartnerDtoList.ContainsKey(userId))
                        {
                            dictUserIdAndPartnerDtoList.Add(userId, new List<AppBusinessPartnerDto>());
                        }

                        List<AppBusinessPartnerDto> partnerDtoList = dictUserIdAndPartnerDtoList[userId];

                        if (partnerDtoList.FirstOrDefault(o => (int)o.Id == partnerId) == null)
                        {
                            partnerDtoList.Add(AppBusinessPartnerConverter.ConvertEntityToDto(partnerEntity));
                        }
                    }

                }

            }

            return dictUserIdAndPartnerDtoList;
        }
    }
}