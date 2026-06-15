
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
namespace App.BL
{
    public static class AppSecurityAuthenticationBL

    {

        private static readonly string AuthenticationMode = (AppConfig.Get("AuthenticationMode") ?? string.Empty).ToUpperInvariant();


        public readonly static DataTable LoginFailedLog = new DataTable();
        public readonly static int AllowAttemptTimes = 5;
        private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;
        static AppSecurityAuthenticationBL()
        {
            LoginFailedLog.Columns.Add(new DataColumn("LoginName", typeof(string)));
            //LoginFailedLog.Columns.Add(new DataColumn("FailedCount", typeof(int)));
            LoginFailedLog.Columns.Add(new DataColumn("FailedLogTimeStamp", typeof(DateTime)));

        }


        // for MGT login
        public static UserContext Authenticate(string username, string password)
        {
            // Defualt 
            UserContext aUserContext = new UserContext();
            aUserContext.LoginFailedErroMessage = "Cannot find your  Account";
            aUserContext.IsLoginFailed = true;


            AppSecurityUserEntity aMasterDbAppSecurityUserEntity = null;
            EmAppAuthenticationResult emAppAuthenticationResult = VerifyUserAccount(username, password, ref aMasterDbAppSecurityUserEntity);

            if (emAppAuthenticationResult != EmAppAuthenticationResult.LoginSucceful)
            {
                aUserContext = ProcessLoginFailedUserContext(username, aUserContext, aMasterDbAppSecurityUserEntity, emAppAuthenticationResult);
            }
            else if (emAppAuthenticationResult == EmAppAuthenticationResult.LoginSucceful)
            {
                var faieldRows = LoginFailedLog.AsEnumerable().Where(o => o["LoginName"].ToString() == username).ToList();
                foreach (var row in faieldRows)
                {
                    LoginFailedLog.Rows.Remove(row);
                }

                var availableCompnay = AppSecurityManagementBL.RetrieveUserAvailableCompaniesFromMasterDB(aMasterDbAppSecurityUserEntity.UserId);

                aUserContext.IsLoginFailed = false;
                aUserContext.LoginFailedErroMessage = "";
                aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;
                aUserContext.AvailableCompnay = availableCompnay;
                aUserContext.SessionId = System.Guid.NewGuid().ToString();
                aUserContext.DisplayName = aMasterDbAppSecurityUserEntity.UserName;
                aUserContext.DocumentId = aMasterDbAppSecurityUserEntity.DocumentId;
                aUserContext.DomainId = aMasterDbAppSecurityUserEntity.DomainId;

                // Default :  the First company will be save to the session !!!
                // Only one company, no need to select company
                // for mutiple company, user need to select whcih one to work
                if (availableCompnay.Count == 1)
                {
                    string ecryptCompanyId = availableCompnay[0].Id as string;
                    aUserContext.ServerSideCurrentCompnayId = AppSaasAccountUserBL.DecryptCompanyIdString(ecryptCompanyId);
                    AppSecurityUserSessionBL.CreateNewAppSecurityUserSession(aUserContext);
                }
            }

            return aUserContext;
        }

        // why need  AuthenticateEStore ??? ( how to get the compnayID, from previous session?
        // Before Login, use anoymous context (GetExternalUserContext), which has the company Id

        public static UserContext AuthenticateEStore(string username, string password)
        {
            // Defualt 
            UserContext aUserContext = new UserContext();
            aUserContext.LoginFailedErroMessage = "Cannot find your  Account";
            aUserContext.IsLoginFailed = true;


            AppSecurityUserEntity aMasterDbAppSecurityUserEntity = null;
            EmAppAuthenticationResult emAppAuthenticationResult = VerifyUserAccount(username, password, ref aMasterDbAppSecurityUserEntity);

            if (emAppAuthenticationResult != EmAppAuthenticationResult.LoginSucceful)
            {
                aUserContext = ProcessLoginFailedUserContext(username, aUserContext, aMasterDbAppSecurityUserEntity, emAppAuthenticationResult);
            }
            else if (emAppAuthenticationResult == EmAppAuthenticationResult.LoginSucceful)
            {
                var faieldRows = LoginFailedLog.AsEnumerable().Where(o => o["LoginName"].ToString() == username).ToList();
                foreach (var row in faieldRows)
                {
                    LoginFailedLog.Rows.Remove(row);
                }

                bool isUserOnEStoreUserDB = AppSecurityUserBL.RetrieveSimpleAppSecurityUserEntityList(new List<int>() { aMasterDbAppSecurityUserEntity.UserId })
                    .Count > 0;

                if (isUserOnEStoreUserDB)
                {
                    aUserContext.IsLoginFailed = false;
                    aUserContext.LoginFailedErroMessage = "";
                    aUserContext.AvailableCompnay = new List<LookupItemDto>();
                    aUserContext.SessionId = System.Guid.NewGuid().ToString();
                    aUserContext.DisplayName = aMasterDbAppSecurityUserEntity.UserName;
                    aUserContext.ServerSideCurrentCompnayId = ServerContext.Instance.CurrentCompanyId as int?;
                    aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;
                    aUserContext.DomainId = aMasterDbAppSecurityUserEntity.DomainId;
                    aUserContext.DocumentId = aMasterDbAppSecurityUserEntity.DocumentId;
                    aUserContext.Email = aMasterDbAppSecurityUserEntity.Email;

                    if (aUserContext.ServerSideCurrentCompnayId.HasValue)
                    {
                        // For anoymous user, need to ise compnay applicaon Tokey;
                        int? compnayId = aUserContext.ServerSideCurrentCompnayId;
                        int? userId = aMasterDbAppSecurityUserEntity.UserId;
                        UpdateUserContextDomainUserType(aUserContext, compnayId, userId);
                    }

                    AppSecurityUserSessionBL.CreateNewAppSecurityUserSession(aUserContext);
                    
                }
                else
                {
                    aUserContext = new UserContext();
                    aUserContext.LoginFailedType = (int)EmAppAuthenticationResult.UserNotLinkedToEStoreUserDB;
                    aUserContext.TempToken = AppSaasAccountUserBL.EncryptCompanyIdString(aMasterDbAppSecurityUserEntity.UserId);
                    aUserContext.LoginFailedErroMessage = "You account is not linked to current EStore.";
                    aUserContext.IsLoginFailed = true;

                }
            }

            return aUserContext;
        }

        internal static void UpdateUserContextDomainUserType(UserContext aUserContext, int? compnayId, int? userId)
        {
            var partnerEntity = AppCacheManagerBL.GetCurrentUserWorkingCompanyBinessPartner(compnayId.Value, userId.Value);
            if (partnerEntity != null)
            {
                aUserContext.BusinessUserId = partnerEntity.AppBusinessPartnerId;
                if (partnerEntity.PartnerType.HasValue)
                {
                    aUserContext.DomainId = partnerEntity.PartnerType.Value;
                }
            }
        }

        public static UserContext CreateUserContextAndSessionFromExistUserDto(AppSecurityUserDto userDto)
        {
            // Defualt 
            UserContext aUserContext = new UserContext();
            aUserContext.LoginFailedErroMessage = "Cannot find your Account";
            aUserContext.IsLoginFailed = true;

            if (userDto != null)
            {
                aUserContext.IsLoginFailed = false;
                aUserContext.LoginFailedErroMessage = "";
                aUserContext.AvailableCompnay = new List<LookupItemDto>();
                aUserContext.SessionId = System.Guid.NewGuid().ToString();
                aUserContext.DisplayName = userDto.UserName;
                aUserContext.ServerSideCurrentCompnayId = ServerContext.Instance.CurrentCompanyId as int?;
                aUserContext.UserId = userDto.Id;
                aUserContext.EmExternalSigninType = userDto.EmExternalSigninType;
                aUserContext.DocumentId = userDto.DocumentId;
                //aUserContext.ExternalAcessToken = userDto.ExternalAcessToken;

                var partnerEntity = AppCacheManagerBL.GetCurrentUserWorkingCompanyBinessPartner(aUserContext.ServerSideCurrentCompnayId.Value, (int)aUserContext.UserId);
                if (partnerEntity != null)
                {
                    aUserContext.BusinessUserId = partnerEntity.AppBusinessPartnerId;
                    if (partnerEntity.PartnerType.HasValue)
                    {
                        aUserContext.DomainId = partnerEntity.PartnerType.Value;
                    }
                }

                AppSecurityUserSessionBL.CreateNewAppSecurityUserSession(aUserContext);
            }

            return aUserContext;
        }

        private static UserContext ProcessLoginFailedUserContext(string username, UserContext aUserContext, AppSecurityUserEntity aMasterDbAppSecurityUserEntity, EmAppAuthenticationResult emAppAuthenticationResult)
        {
            if (emAppAuthenticationResult == EmAppAuthenticationResult.NotFound)
            {

                aUserContext = new UserContext();
                aUserContext.LoginFailedErroMessage = "Cannot find your  Account  ";
                aUserContext.IsLoginFailed = true;
            }

            if (emAppAuthenticationResult == EmAppAuthenticationResult.SaasUserRegisterNotComplete)
            {

                aUserContext = new UserContext();
                aUserContext.LoginFailedErroMessage = "You did not complete the registration process for this user ID. ";
                aUserContext.IsLoginFailed = true;
                aUserContext.LoginFailedType = (int)EmAppAuthenticationResult.SaasUserRegisterNotComplete;
                aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;
            }


            else if (emAppAuthenticationResult == EmAppAuthenticationResult.InActive)
            {
                // aUserContext.LoginFailedErroMessage = "User is not active. Please contact your administrator.";
                aUserContext = new UserContext();
                aUserContext.LoginFailedErroMessage = "Your account is locked.";
                aUserContext.IsLoginFailed = true;
                aUserContext.LoginFailedType = (int)EmAppAuthenticationResult.InActive;
                aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;
            }

            else if (emAppAuthenticationResult == EmAppAuthenticationResult.LockedByTooManyWrongPassword)
            {
                // aUserContext.LoginFailedErroMessage = "User is not active. Please contact your administrator.";
                aUserContext = new UserContext();
                aUserContext.LoginFailedErroMessage = "Your account is locked, too many times sign in."; 
                aUserContext.IsLoginFailed = true;
                aUserContext.LoginFailedType = (int)EmAppAuthenticationResult.InActive;
                aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;
            }

            else if (emAppAuthenticationResult == EmAppAuthenticationResult.NewUserNotActivedByEmail)
            {
                // aUserContext.LoginFailedErroMessage = "User is not active. Please contact your administrator.";
                aUserContext = new UserContext();
                aUserContext.LoginFailedErroMessage = "Please confirm your account via email first!";
                aUserContext.IsLoginFailed = true;
                aUserContext.LoginFailedType = (int)EmAppAuthenticationResult.InActive;
                aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;
            }

            else if (emAppAuthenticationResult == EmAppAuthenticationResult.InvalidPassword)
            {

                aUserContext = new UserContext();
                aUserContext.LoginFailedErroMessage = "Wrong password ";
                aUserContext.IsLoginFailed = true;


                DataRow row = LoginFailedLog.NewRow();
                LoginFailedLog.Rows.Add(row);

                row["LoginName"] = username;
                row["FailedLogTimeStamp"] = System.DateTime.Now;

                int trycount = LoginFailedLog.AsEnumerable().Where(o => o["LoginName"].ToString() == username).Count();

                if (trycount > AllowAttemptTimes)
                {
                    aUserContext.LoginFailedErroMessage = "Your account is locked, too many times sign in.";
                    aUserContext.IsLoginFailed = true;
                    aUserContext.LoginFailedType = (int)EmAppAuthenticationResult.InActive;
                    aUserContext.UserId = aMasterDbAppSecurityUserEntity.UserId;

                    using (DataAccessAdapter adpater = new DataAccessAdapter(MasterConnStr))
                    {
                        aMasterDbAppSecurityUserEntity.Addomain = EmAppAuthenticationResult.LockedByTooManyWrongPassword.ToString();
                        aMasterDbAppSecurityUserEntity.IsActive = false;
                        adpater.UpdateEntitiesDirectly(aMasterDbAppSecurityUserEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == aMasterDbAppSecurityUserEntity.UserId));
                    }
                }

            }

            return aUserContext;
        }


        // 1 cannot find the user
        // 2: find the user but password not mahtc
        // 3: find the user mathc oreect  int.MinValue
        private static EmAppAuthenticationResult VerifyUserAccount(string LoginName, string password, ref AppSecurityUserEntity aAppSecurityUserEntity)
        {

            aAppSecurityUserEntity = GetUserAccountbyLogName(LoginName);



            // cannot find the user
            if (aAppSecurityUserEntity == null)
            {
                return EmAppAuthenticationResult.NotFound;
            }
            else // user found
            {



                bool isMasteruser = aAppSecurityUserEntity.IsBuiltIntUser.HasValue && aAppSecurityUserEntity.IsBuiltIntUser.Value;
                bool isRegisterComple = aAppSecurityUserEntity.IsRegisterCompleted.HasValue && aAppSecurityUserEntity.IsRegisterCompleted.Value;
                bool isCompnayLink = aAppSecurityUserEntity.MyOwnCompnanyId.HasValue;


                if (!isMasteruser && (!(isRegisterComple || isCompnayLink)))
                {
                    return EmAppAuthenticationResult.SaasUserRegisterNotComplete;
                }


                if (!isRegisterComple && aAppSecurityUserEntity.DomainId == (int)EmAppUserType.SaasCompanyAdmin)
                {
                    return EmAppAuthenticationResult.SaasUserRegisterNotComplete;

                }
                else if (!aAppSecurityUserEntity.IsActive)
                {
                    if (aAppSecurityUserEntity.Addomain == EmAppAuthenticationResult.LockedByTooManyWrongPassword.ToString())
                    {
                        return EmAppAuthenticationResult.LockedByTooManyWrongPassword;
                    }
                    else if (aAppSecurityUserEntity.Addomain == EmAppAuthenticationResult.NewUserNotActivedByEmail.ToString())
                    {
                        return EmAppAuthenticationResult.NewUserNotActivedByEmail;
                    }
                    else
                    {
                        return EmAppAuthenticationResult.InActive;
                    }
                }                
                else
                {
                    bool isUserAllowAccessCurrentDB = CheckIfUserAllowAccessCurrentDB(aAppSecurityUserEntity);

                    if (!isUserAllowAccessCurrentDB)
                    {
                        return EmAppAuthenticationResult.AccessDenied;
                    }
                    else
                    {
                        // hashpassword hash
                        if (AppSecurityPasswordHashBL.ValidatePassword(password, aAppSecurityUserEntity.Password))
                        {
                            return EmAppAuthenticationResult.LoginSucceful;
                        }
                        else
                        {
                            return EmAppAuthenticationResult.InvalidPassword;
                        }


                        //if (password == aAppSecurityUserEntity.DecrypedPassword)
                        //{
                        //	return EmAppAuthenticationResult.LoginSucceful;
                        //}
                        //else
                        //{
                        //	return EmAppAuthenticationResult.InvalidPassword;
                        //}

                    }

                }


            }


        }

        private static bool CheckIfUserAllowAccessCurrentDB(AppSecurityUserEntity aAppSecurityUserEntity)
        {
            bool isAllowAccess = false;

            if (ServerContext.Instance.CompanySettings != null && ServerContext.Instance.CompanySettings.CompanyId.HasValue &&
                    aAppSecurityUserEntity.MyOwnCompnanyId.HasValue && aAppSecurityUserEntity.MyOwnCompnanyId.Value != ServerContext.Instance.CompanySettings.CompanyId.Value
                    && !string.IsNullOrWhiteSpace(ServerContext.Instance.CurrentUserDataBaseName))
            {
                AppDataSourceRegisterEntity aAppDataSourceRegisterEntity = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(aAppSecurityUserEntity.MyOwnCompnanyId.Value);
                if (aAppDataSourceRegisterEntity.DatabaseName.ToLower() == ServerContext.Instance.CurrentUserDataBaseName.ToLower())
                {
                    isAllowAccess = true;
                }

            }
            else
            {
                isAllowAccess = true;
            }

            return isAllowAccess;
        }

        private static  bool IsValidEmail(string email)
        {
            try
            {
                // var addr = new System.Net.Mail.MailAddress(email);
                // return addr.Address == email;

                EmailAddressAttribute emailAddressAttribute = new EmailAddressAttribute();
                return emailAddressAttribute.IsValid(email);
            }
            catch
            {
                return false;
            }
        }

        internal static AppSecurityUserEntity GetUserAccountbyLogName(string LoginName)
        {
            // need to use master DB for authentication 
            using (DataAccessAdapter dapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {


                RelationPredicateBucket filterBucket;

                if(IsValidEmail(LoginName))
                {
                    filterBucket = new RelationPredicateBucket(AppSecurityUserFields.Email == LoginName);
                }
                else
                {
                    filterBucket = new RelationPredicateBucket(AppSecurityUserFields.LoginName == LoginName);
                }
             


                //IncludeFieldsList includeFieldsList = new IncludeFieldsList();
                //includeFieldsList.Add(AppSecurityUserFields.UserId);
                //includeFieldsList.Add(AppSecurityUserFields.LoginName);
                //includeFieldsList.Add(AppSecurityUserFields.Password);
                //includeFieldsList.Add(AppSecurityUserFields.IsActive);

                EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();
                dapter.FetchEntityCollection(users, filterBucket);

                if (users.Count > 0)
                {
                    AppSecurityUserEntity aAppSecurityUserEntity = users[0];

                    return aAppSecurityUserEntity;
                }
                else  // LogFailed
                {
                    //HttpContext.Current.Request.UserHostAddress
                    return null;
                }
            }


        }
    }
}