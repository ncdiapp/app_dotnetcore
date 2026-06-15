using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Framework;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
#if NETFRAMEWORK
using System.Management.Automation;
using System.Management.Automation.Runspaces;
#endif
using System.IO;

#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif

using DatabaseSchemaMrg;
using System.Diagnostics;

using System.Text.RegularExpressions;
using System.ComponentModel.Design;
using System.Data.SqlClient;
    
using System.Security.AccessControl;

namespace App.BL
{

    public static class AppSaasAccountUserBL
    {
        public static readonly string DefaultEncryptSaltKey = "AD3F0E16-B227-4A87-89AC-A4367E13E531";

        public static readonly string AppMasterDBConnectionString = AppConfig.GetConnectionString("AppMasterDBConnectionString") ?? string.Empty;
        public static readonly string HostCompanyDbName = new SqlConnectionStringBuilder(AppMasterDBConnectionString).InitialCatalog;
        public static readonly string StandAloneWebAppFolderName = "WebApp";
        public static readonly string StandAloneWebAppCompanyFolderPrefix = "Comp_";

        public static OperationCallResult<AppSecurityUserExDto> RegisterNewSaasAccountWithUserCompanyDB(AppSecurityUserExDto appSecurityUserExDto)
        {
            appSecurityUserExDto.ApplicationCode = Regex.Replace(appSecurityUserExDto.Email.Replace(" ", "").Replace("@", "_").Replace(".", "_"), @"[^0-9a-zA-Z]+", "_");

            appSecurityUserExDto.AdloginName = appSecurityUserExDto.ApplicationCode;
            //appSecurityUserExDto.Addomain = appSecurityUserExDto.CompanyName;

            if (appSecurityUserExDto.IsNeedActiveSaasUserByEmail)
            {
                return RegisterNewSaasAccountWaitForEmailConfirmation(appSecurityUserExDto);
            }
            else
            {
                return RegisterNewSaasAccountWithoutEmailConfirmation(appSecurityUserExDto);
            }
        }


        public static OperationCallResult<AppSecurityUserExDto> RegisterNewSaasAccountWithoutEmailConfirmation(AppSecurityUserExDto appSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = ValidateNewSaasAccountUserExDto(appSecurityUserExDto);
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
            appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;


            SaveSaasNewUser(appSecurityUserExDto, aValidationResult);


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo((int)appSecurityUserExDto.Id);


                string userGuidStr = userDto.GlobalGuid.ToString();
                Guid userGuid = new Guid(userGuidStr);
                OperationCallResult<AppSecurityUserExDto> completeRegistrationResult = AppSaasAccountUserBL.ConfirmNewSaasAccountEmailAndCreateUserCompanyDB(userGuid);

                return completeRegistrationResult;
            }
            else
            {
                return aOperationCallResult;
            }
        }



        public static OperationCallResult<AppSecurityUserExDto> RegisterNewSaasAccountWaitForEmailConfirmation(AppSecurityUserExDto appSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = ValidateNewSaasAccountUserExDto(appSecurityUserExDto);
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aValidationResult.HasErrors)
            {

                return aOperationCallResult;
            }

            appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
            appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;


            SaveSaasNewUser(appSecurityUserExDto, aValidationResult);


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo((int)appSecurityUserExDto.Id);

                SendSaasAccountRegisterConfirmationRequestEmail(userDto);

                aValidationResult.AddItem(null, "activeEmailMessage", ValidationItemType.Message, "Please check your mail to activate your account.");

                aOperationCallResult.Object = userDto;

            }

            return aOperationCallResult;
        }



        public static OperationCallResult<AppSecurityUserExDto> ConfirmNewSaasAccountEmailAndCreateUserCompanyDB(Guid userGuid, bool setUserActive = true, int? companyId = null, int? orgnizationId = null)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserEntity userEntity = RetrieveOneAppSecurityUserEntityByGuid(userGuid);

            if (userEntity != null)
            {
                if (!(userEntity.IsRegisterCompleted.HasValue && userEntity.IsRegisterCompleted.Value))
                {

                    AppSecurityUserExDto aNewSecurityUserExDto = AppSecurityUserConverter.ConvertEntityToExDto(userEntity);

                    aNewSecurityUserExDto.ApplicationCode = aNewSecurityUserExDto.AdloginName;
                    //aNewSecurityUserExDto.CompanyName = aNewSecurityUserExDto.Addomain;

                    bool isPostProcessSuccess = false;

                    isPostProcessSuccess = CreateNewCompanyAndRestoreUserDBForRegistedUser(aNewSecurityUserExDto, aValidationResult);

                    if (isPostProcessSuccess)
                    {
                        SetUserRegisterCompleted(setUserActive, aValidationResult, userEntity.UserId);

                        string appName = aNewSecurityUserExDto.ApplicationCode;
                        string companyIdStr = aNewSecurityUserExDto.MyOwnCompnanyId.Value.ToString();

                        var createAppValidationResult = CreateUserWebApplication(appName, companyIdStr);

                        if (createAppValidationResult.HasErrors)
                        {
                            aValidationResult.Merge(createAppValidationResult);
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UserRegister_CreateUserCompanyDatabaseFailed", ValidationItemType.Error, "Prepare User Data Failed."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_UserAlreadyActived_Message", ValidationItemType.Message, "User had already been actived. You don't need to active it again."));
                }

                if (!aValidationResult.HasErrors)
                {
                    var saasUserDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(userEntity.UserId);
                    aOperationCallResult.Object = saasUserDto;
                    //SendAccountCreatedConfirmationMessage(saasUserDto);
                }
                else // need to clear up by user guid !!! and let user registger
                {

                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_CannotFindUser_Error", ValidationItemType.Message, "System cannot find this user."));

                //// need to clear up by user guid !!!
            }

            return aOperationCallResult;
        }

        private static ValidationResult CreateUserWebApplication(string appName, string companyId)
        {
            ValidationResult validationResult = new ValidationResult();
            string mgtPhsyicalPath = AppDomain.CurrentDomain.BaseDirectory;

            //I:\DevTest\App\PlmApplication\FileRepository\Company_4040\WebSite\Site_1
            string defaultSitePhsyicalPath = mgtPhsyicalPath + "FileRepository\\Company_" + companyId + "\\WebSite\\Site_1";

            string appPoolName = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationPoolName);
            string iisWebSiteName = AppSystemSettingBL.GetStringValue(EmSystemSettings.IISWebSiteName);
            string mgtPath = appName + "/MGT";

            try
            {
                IISHelper.CreateApplicatoin(iisWebSiteName, appName, defaultSitePhsyicalPath, appPoolName);
                //IISHelper.CreateApplicatoin(iisWebSiteName, appName, mgtPhsyicalPath, appPoolName);
                IISHelper.CreateApplicatoin(iisWebSiteName, mgtPath, mgtPhsyicalPath, appPoolName);
            }
            catch (Exception ex)
            {

                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "Create_Web_Application_Error", ValidationItemType.Error, ex.ToString()));
            }

            return validationResult;
        }

        public static OperationCallResult<AppSecurityUserExDto> SelfCompanyUserRegister(AppSecurityUserExDto appSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = ValidateUserExDto(appSecurityUserExDto);
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
            appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;


            SaveSaasNewUser(appSecurityUserExDto, aValidationResult);


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo((int)appSecurityUserExDto.Id);

                SendSaasUserRegisterConfirmationRequestEmail(userDto);

                aOperationCallResult.Object = userDto;
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppSecurityUserExDto> ResendSaasUserSelfRegistrationConfirmationEmailToNewAddress(AppSecurityUserExDto appSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = ValidateUserExDto(appSecurityUserExDto);
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
            appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;

            aValidationResult.Merge(AppSecurityUserBL.UpdateMasterDBUserLoginInfo(appSecurityUserExDto).ValidationResult);

            if (!aValidationResult.HasErrors)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo((int)appSecurityUserExDto.Id);

                SendSaasUserRegisterConfirmationRequestEmail(userDto);


                aOperationCallResult.Object = userDto;
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppSecurityUserExDto> QuickCreateSaasUser(AppSecurityUserExDto appSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = ValidateUserExDto(appSecurityUserExDto);
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (appSecurityUserExDto.InvitedByPartnerId.HasValue
                    && (appSecurityUserExDto.DomainId == (int)EmAppUserType.Supplier
                    || appSecurityUserExDto.DomainId == (int)EmAppUserType.Customer
                    || appSecurityUserExDto.DomainId == (int)EmAppUserType.ClientAgent
                    || appSecurityUserExDto.DomainId == (int)EmAppUserType.SupplierAgent))
            {
                appSecurityUserExDto.IsQuickRegistration = true;
                appSecurityUserExDto.IsNeedActivePartnerUserByEmail = false;
                appSecurityUserExDto.NewUserPartnerType = appSecurityUserExDto.DomainId;
                appSecurityUserExDto.MyOwnCompnanyId = null;
                int? partnerId = appSecurityUserExDto.InvitedByPartnerId;
                OperationCallResult<AppSecurityUserExDto> saveUserResult = AppSaasAccountUserBL.CreateUserForExistingPartner(appSecurityUserExDto, partnerId.Value);

                if (saveUserResult.IsSuccessfulWithResult)
                {
                    appSecurityUserExDto = saveUserResult.Object;
                }
                else
                {
                    aOperationCallResult.ValidationResult = saveUserResult.ValidationResult;
                }
            }
            else
            {
                appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
                appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;


                if (appSecurityUserExDto.MyOwnCompnanyId.HasValue)
                {
                    Guid newUserGuid = Guid.NewGuid();
                    int? orgnizationid = appSecurityUserExDto.OrganizationId;
                    appSecurityUserExDto.OrganizationId = null;

                    appSecurityUserExDto.IsActive = true;
                    appSecurityUserExDto.IsRegisterCompleted = true;

                    var savedUserEntity = SaveSaasNewUser(appSecurityUserExDto, aValidationResult, newUserGuid);

                    //if (!aValidationResult.HasErrors)
                    //{
                    //    var completeRegisterResult = ConfirmNewSaasUserEmailAndCompleteRegistration(newUserGuid, true, appSecurityUserExDto.MyOwnCompnanyId, orgnizationid).ValidationResult;
                    //    aValidationResult.Merge(completeRegisterResult);
                    //}
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UserRegister_CreateUserFailed", ValidationItemType.Error, "Create User Failed."));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo((int)appSecurityUserExDto.Id);
                aOperationCallResult.Object = userDto;
            }

            return aOperationCallResult;
        }


        public static AppSecurityUserExDto GetExistingUserFromInvitationInfoWithEmailAddress(int invitingCompanyId, int invitationId)
        {
            AppSecurityUserInvitationEntity invitationEntity = AppSaasAccountUserBL.RetrieveOneAppSecurityUserInvitationEntityByCompany(invitingCompanyId, invitationId);
            AppSecurityUserEntity existUserEntity = AppSaasAccountUserBL.RetrieveInvitedBusinessPartnerUserEntityByEmailAddrees(invitationEntity);

            if (existUserEntity != null)
            {
                AppSecurityUserExDto aAppSecurityUserExDto = AppSecurityUserConverter.ConvertEntityToExDto(existUserEntity);
                return aAppSecurityUserExDto;
            }

            return null;
        }
        /// <summary>
        ///  
        /// after invitation get pass, need to log invited user and company to Master Db
        ///  Master DB: only store UserId and AppcompnayID(inviting company)
        ///for User dB, need to populate UserID ,parternID, apppcompnayId
        /// </summary>
        /// <param name="aNewSecurityUserExDto"></param>
        /// <param name="invitingCompanyIdFromEmail"></param>
        /// <param name="invitationId"></param>
        /// <returns></returns>

        public static bool LinkExistingUserToBusinessParternCompnay(AppSecurityUserExDto aNewSecurityUserExDto, int invitingCompanyIdFromEmail, int? businessPartnerId)
        {
            bool isSuccess = false;

            if (businessPartnerId.HasValue)
            {
                isSuccess = RegisterInvitingUserAndCompnay((int)aNewSecurityUserExDto.Id, invitingCompanyIdFromEmail, businessPartnerId);

            }

            return isSuccess;
        }

        public static int? GetBusinessPartnerIdFromInvitationId(int invitationId, int invitingCompanyId)
        {
            AppDataSourceRegisterEntity aAppDataSourceRegisterEntity = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(invitingCompanyId);
            string userDBConnString = aAppDataSourceRegisterEntity.ConnectionString;
            string Userdbname = aAppDataSourceRegisterEntity.DatabaseName;

            AppSecurityUserInvitationEntity invitationEntity = new AppSecurityUserInvitationEntity(invitationId);

            if (!string.IsNullOrWhiteSpace(userDBConnString) && !string.IsNullOrWhiteSpace(Userdbname))
            {
                using (var adpater = new DataAccessAdapter(userDBConnString))
                {
                    try
                    {
                        adpater.FetchEntity(invitationEntity);
                        return invitationEntity.InvitingBusinessPartnerId;
                    }
                    catch (Exception ex)
                    {
                        adpater.Rollback();
                    }

                }
            }

            return null;
        }


        public static OperationCallResult<AppSecurityUserExDto> CreateUserForExistingPartner(AppSecurityUserExDto appSecurityUserExDto, int? partnerId, bool isInPartnerAdminRole = false)
        {
            return CreateSaasPartnerEndUser(appSecurityUserExDto, partnerId, isInPartnerAdminRole);
        }

        public static OperationCallResult<AppSecurityUserExDto> CreateNewPartnerAndDefaultUser(AppSecurityUserExDto appSecurityUserExDto, bool isInPartnerAdminRole = false)
        {
            return CreateSaasPartnerEndUser(appSecurityUserExDto, null, isInPartnerAdminRole);
        }

        // Create Partner, User, InvitedUser, UDUserTableEntity(example: StudentTable)
        private static OperationCallResult<AppSecurityUserExDto> CreateSaasPartnerEndUser(AppSecurityUserExDto appSecurityUserExDto, int? partnerId = null, bool isInPartnerAdminRole = false)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
            appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;

            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

            if (companyId.HasValue)
            {
                // Validate LoginName Or Email Already Exists
                var aValidationResult = AppSecurityUserBL.CheckNewUserLoginNameAndEmailConfilict(appSecurityUserExDto);
                if (aValidationResult.HasErrors)
                {
                    aOperationCallResult.ValidationResult = aValidationResult;
                    return aOperationCallResult;
                }

                // Validate UserDto
                aValidationResult = appSecurityUserExDto.ValidateDto();
                if (aValidationResult.HasErrors)
                {
                    aOperationCallResult.ValidationResult = aValidationResult;
                    return aOperationCallResult;
                }


                // 1. Get Or Create Partner
                AppBusinessPartnerExDto partnerExDto = null;
                if (partnerId.HasValue)
                {
                    partnerExDto = AppBusinessPartnerBL.RetrieveOneAppBusinessPartnerExDto(partnerId.Value);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(appSecurityUserExDto.NewUserPartnerName))
                    {
                        appSecurityUserExDto.NewUserPartnerName = appSecurityUserExDto.LoginName;
                    }

                    partnerExDto = CreatePartnerFromUserDto(appSecurityUserExDto.NewUserPartnerName, appSecurityUserExDto.NewUserPartnerType, companyId, aValidationResult);
                    isInPartnerAdminRole = true;
                }

                if (aValidationResult.HasErrors || partnerExDto == null)
                {
                    return aOperationCallResult;
                }


                // 2. Create User On DB
                var savedUserEntity = SaveSaasNewUser(appSecurityUserExDto, aValidationResult);
                if (aValidationResult.HasErrors)
                {
                    return aOperationCallResult;
                }


                // 3. CompleteInvitedSaasBusinessParternerUserRegistration   
                //      Link User To Partner: Add AppBusinessPartnerInviteUser On MasterDB and User DB                
                //      Send Email

                int? needToAssignPartnerAdminRoleType = null;

                if (isInPartnerAdminRole)
                {
                    needToAssignPartnerAdminRoleType = appSecurityUserExDto.NewUserPartnerType;
                }

                var saveUserResult = CompleteInvitedSaasBusinessParternerUserRegistration(savedUserEntity.UserId, companyId.Value, (int)partnerExDto.Id, appSecurityUserExDto.IsNeedActivePartnerUserByEmail, appSecurityUserExDto.PostEmailActivationRedirectUrl, appSecurityUserExDto.IsQuickRegistration, appSecurityUserExDto.LogoImgUrl, appSecurityUserExDto.MessageTempalte, appSecurityUserExDto.MessageTempalteId, needToAssignPartnerAdminRoleType);

                if (saveUserResult.IsSuccessfulWithResult)
                {
                    aOperationCallResult.Object = saveUserResult.Object;
                    AppSecurityUserBL.ResetDictAllUserDto();
                }
                else
                {
                    aValidationResult = saveUserResult.ValidationResult;
                    return aOperationCallResult;
                }
            }

            return aOperationCallResult;
        }

        //public static OperationCallResult<AppSecurityUserExDto> LinkExistMasterDBUserToEStore(int userId, int? partnerId = null, string postEmailActivationRedirectUrl = "")
        //{
        //    OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
        //    AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserSimpleEntity(userId);

        //    if (companyId.HasValue)
        //    {
        //        if (!partnerId.HasValue)
        //        {
        //            // 1. Create Partner On User DB
        //            AppBusinessPartnerExDto newPartnerExDto = CreatePartnerFormUserDto(userEntity.LoginName, (int)EmAppUserType.Customer, companyId, aValidationResult);
        //            if (aValidationResult.HasErrors || newPartnerExDto == null)
        //            {
        //                return aOperationCallResult;
        //            }
        //            else
        //            {
        //                partnerId = (int)newPartnerExDto.Id;
        //            }
        //        }

        //        // 3. CompleteInvitedSaasBusinessParternerUserRegistration                
        //        //      Copy MasterDB User To UserDB
        //        //      Link User To Partner: Add AppBusinessPartnerInviteUser On MasterDB and User DB                
        //        //      Send Email

        //        var saveUserResult = CompleteInvitedSaasBusinessParternerUserRegistration(userId, companyId.Value, partnerId, false, postEmailActivationRedirectUrl, false);

        //        if (saveUserResult.IsSuccessfulWithResult)
        //        {
        //            aOperationCallResult.Object = saveUserResult.Object;
        //        }
        //        else
        //        {
        //            aValidationResult = saveUserResult.ValidationResult;
        //            return aOperationCallResult;
        //        }
        //    }

        //    return aOperationCallResult;
        //}

        internal static AppBusinessPartnerExDto CreatePartnerFromUserDto(string partnerCode, int? partnerType, int? companyId, ValidationResult aValidationResult)
        {
            AppBusinessPartnerExDto newPartner = new AppBusinessPartnerExDto();
            newPartner.Code = partnerCode;
            newPartner.ShortName = partnerCode;
            newPartner.FullName = partnerCode;
            newPartner.PartnerType = partnerType;
            newPartner.AppCompanyId = companyId;
            newPartner.AppCreatedByCompanyId = companyId;



            var savePartnerResult = AppBusinessPartnerBL.SaveOneAppBusinessPartnerExDto(newPartner);

            if (savePartnerResult.IsSuccessfulWithResult)
            {
                return savePartnerResult.Object;
            }
            else
            {
                if (aValidationResult != null)
                {
                    aValidationResult.Merge(savePartnerResult.ValidationResult);
                }
                return null;
            }
        }

        //private static void SaveAppBusinessPartnerInviteUserToUserDB(int userId, int invitingCompanyIdFromEmail, int? businessPartnerId, AppDataSourceRegisterEntity aAppDataSourceRegisterEntity)
        //{
        //    using (var adpater = new DataAccessAdapter(aAppDataSourceRegisterEntity.ConnectionString, aAppDataSourceRegisterEntity.DatabaseName))
        //    {
        //        AppBusinessPartnerInviteUserEntity userDBBusinessPartnerInviteUserExDto = new AppBusinessPartnerInviteUserEntity();
        //        // appBusinessPartnerInviteUserExDto.AppBusinessPartnerId = businessPartnerId.Value;
        //        userDBBusinessPartnerInviteUserExDto.UserId = userId;
        //        userDBBusinessPartnerInviteUserExDto.AppCompanyId = invitingCompanyIdFromEmail;
        //        userDBBusinessPartnerInviteUserExDto.AppBusinessPartnerId = businessPartnerId;

        //        adpater.SaveEntity(userDBBusinessPartnerInviteUserExDto);
        //    }

        //    //  adpater.Commit();
        //}

        internal static void CopyMasterDBUserToUserCompanyDatabaseForNoneExistUser(AppSecurityUserExDto aMasterSecurityUserExDto, AppDataSourceRegisterEntity aAppDataSourceRegisterEntity, int? businessPartnerId = null)
        {
            using (var adpater = new DataAccessAdapter(AppConnectionStringEncryptionBL.Decrypt(aAppDataSourceRegisterEntity.ConnectionString)))
            {
                EntityCollection<AppSecurityUserEntity> existUsers = new EntityCollection<AppSecurityUserEntity>();
                adpater.FetchEntityCollection(existUsers, new RelationPredicateBucket(new PredicateExpression(AppSecurityUserFields.UserId == (int)aMasterSecurityUserExDto.Id)));

                if (existUsers.Count == 0)
                {

                    //aMasterSecurityUserExDto.DomainId = (int)EmAppUserType.Unknow;
                    CopyMasterDBUserToUserCompanyDatabaseByAdapter(adpater, aMasterSecurityUserExDto, null, null, businessPartnerId);

                }

            }

        }

        private static bool RegisterInvitingUserAndCompnay(int newUserId, int invitingCompanyIdFromEmail, int? businessPartnerId)
        {

            bool isSuccess = false;

            List<AppBusinessPartnerInviteUserEntity> inviteUserEntityList = RetrieveAppBusinessPartnerInviteUserEntityListByUserId(newUserId);

            var existEntity = inviteUserEntityList.FirstOrDefault(o =>
                o.AppCreatedByCompanyId.HasValue && o.AppCreatedByCompanyId.Value == invitingCompanyIdFromEmail
                && o.AppBusinessPartnerId.HasValue && o.AppBusinessPartnerId.Value == businessPartnerId.Value
                && o.UserId.HasValue && o.UserId.Value == newUserId);
            if (existEntity == null)
            {
                var partnerEntity = AppBusinessPartnerBL.RetrieveOneAppBusinessPartnerEntity(businessPartnerId.Value);
                AppBusinessPartnerInviteUserExDto appBusinessPartnerInviteUserExDto = new AppBusinessPartnerInviteUserExDto();
                appBusinessPartnerInviteUserExDto.AppBusinessPartnerId  = businessPartnerId.Value;
                appBusinessPartnerInviteUserExDto.UserId                = newUserId;
                appBusinessPartnerInviteUserExDto.AppCreatedByCompanyId = invitingCompanyIdFromEmail;
                appBusinessPartnerInviteUserExDto.AppCompanyId          = invitingCompanyIdFromEmail; // backward compat
                appBusinessPartnerInviteUserExDto.EmInvitedUserType     = partnerEntity?.PartnerType;

                OperationCallResult<AppBusinessPartnerInviteUserExDto> saveInviteRelationResult = SaveOneAppBusinessPartnerInviteUserEntityExDto(appBusinessPartnerInviteUserExDto);

                if (saveInviteRelationResult.IsSuccessful)
                {
                    isSuccess = true;
                }
            }
            else
            {
                isSuccess = true;
            }

            return isSuccess;

        }

        public static List<AppCompanyDto> GetAccessibleCompaniesForUser(int userId)
        {
            var result = new List<AppCompanyDto>();

            var userEntity = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(userId);
            if (userEntity?.MyOwnCompnanyId.HasValue == true)
            {
                var homeCompany = AppCompanyBL.RetrieveOneSaasCompanyDto(userEntity.MyOwnCompnanyId.Value);
                if (homeCompany != null) result.Add(homeCompany);
            }

            // AppBusinessPartnerInviteUser now lives in each tenant DB (V006).
            // Query every registered tenant data source for this user's invite records.
            var accessibleCompanyIds = new HashSet<int>(result.Select(c => (int)c.Id));
            var allDataSources = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity();
            foreach (var ds in allDataSources)
            {
                if (!(ds.IsCompanyMasterDb.HasValue && ds.IsCompanyMasterDb.Value)) continue;
                try
                {
                    var connStr = AppConnectionStringEncryptionBL.Decrypt(ds.ConnectionString);
                    var tenantInvites = new EntityCollection<AppBusinessPartnerInviteUserEntity>();
                    using (var adapter = new DataAccessAdapter(connStr))
                    {
                        adapter.FetchEntityCollection(tenantInvites,
                            new RelationPredicateBucket(
                                AppBusinessPartnerInviteUserFields.UserId == userId
                                & AppBusinessPartnerInviteUserFields.EmInvitedUserType != DBNull.Value));
                    }
                    foreach (var invite in tenantInvites)
                    {
                        if (!invite.AppCreatedByCompanyId.HasValue) continue;
                        int cid = invite.AppCreatedByCompanyId.Value;
                        if (accessibleCompanyIds.Contains(cid)) continue;
                        var company = AppCompanyBL.RetrieveOneSaasCompanyDto(cid);
                        if (company != null)
                        {
                            result.Add(company);
                            accessibleCompanyIds.Add(cid);
                        }
                    }
                }
                catch { /* skip unreachable tenant DBs */ }
            }

            return result;
        }

        public static OperationCallResult<AppSecurityUserExDto> CreateNewUserForInvitedBusinessPartner(AppSecurityUserExDto appSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            var aValidationResult = ValidateUserExDto(appSecurityUserExDto);
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            appSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(appSecurityUserExDto.Password);
            appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;

            // it is not Invited User compnay, need to set AppCreatedByCompanyId =null
            appSecurityUserExDto.MyOwnCompnanyId = null;




            if (appSecurityUserExDto.InvitationId.HasValue && appSecurityUserExDto.InvitedByCompanyId.HasValue)
            {

                var savedUserEntity = SaveSaasNewUser(appSecurityUserExDto, aValidationResult);

                if (!aValidationResult.HasErrors)
                {

                    int? businessPartnerId = GetBusinessPartnerIdFromInvitationId(appSecurityUserExDto.InvitationId.Value, appSecurityUserExDto.InvitedByCompanyId.Value);

                    var completeRegisterResult = CompleteInvitedSaasBusinessParternerUserRegistration(savedUserEntity.UserId, appSecurityUserExDto.InvitedByCompanyId.Value, businessPartnerId).ValidationResult;

                    aValidationResult.Merge(completeRegisterResult);
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UserRegister_CreateUserFailed", ValidationItemType.Error, "Create User Failed."));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo((int)appSecurityUserExDto.Id);



                aOperationCallResult.Object = userDto;
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppSecurityUserExDto> ConfirmNewSaasUserEmailAndCompleteRegistration(Guid userGuid, bool setUserActive = true, int? companyId = null, int? orgnizationId = null)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserEntity userEntity = RetrieveOneAppSecurityUserEntityByGuid(userGuid);

            if (userEntity != null)
            {
                if (!(userEntity.IsRegisterCompleted.HasValue && userEntity.IsRegisterCompleted.Value))
                {
                    SetUserRegisterCompleted(setUserActive, aValidationResult, userEntity.UserId);
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_UserAlreadyActived_Message", ValidationItemType.Message, "User had already been actived. You don't need to active it again."));
                }

                if (!aValidationResult.HasErrors)
                {
                    var saasUserDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(userEntity.UserId);
                    aOperationCallResult.Object = saasUserDto;
                    SendAccountCreatedConfirmationMessage(saasUserDto);
                }
                else // need to clear up by user guid !!! and let user registger
                {

                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_CannotFindUser_Error", ValidationItemType.Message, "System cannot find this user."));

                //// need to clear up by user guid !!!
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppSecurityUserExDto> ActiveLockedSaasUser(AppSecurityUserExDto appSecurityUserExDto)
        {
            if (appSecurityUserExDto != null && appSecurityUserExDto.Id != null)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo(appSecurityUserExDto.Id);

                userDto.Password = appSecurityUserExDto.Password;
                userDto.ConfirmPassword = userDto.Password;
                userDto.IsActive = true;

                return AppSecurityUserBL.UpdateMasterDBUserLoginInfo(userDto);
            }

            return null;
        }


        public static OperationCallResult<AppSecurityUserExDto> SetUserAsActive(object userId)
        {
            if (userId != null)
            {
                var userDto = AppSecurityUserBL.RetrieveMasterDBUserLoginInfo(userId);
                userDto.IsActive = true;
                return AppSecurityUserBL.UpdateMasterDBUserLoginInfo(userDto);
            }

            return null;
        }



        public static void SendSaasAccountRegisterConfirmationRequestEmail(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            if (aAppSecurityUserExDto != null && !(aAppSecurityUserExDto.IsRegisterCompleted.HasValue && aAppSecurityUserExDto.IsRegisterCompleted.Value))
            {
                string userGuidStr = aAppSecurityUserExDto.GlobalGuid.ToString();
                string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
                string paramStr = EmAppGlobalServiceAction.ConfirmNewSaasAccountEmailAndCreateUserCompanyDB.ToString()
                    + "|" + userGuidStr
                    + "|" + sentDateTicksStr;

                string paramStr_encrypted = EncryptParamString(paramStr);
                //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);



                AppMessageDto messageDto = new AppMessageDto();
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
                messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
                messageDto.Subject = "Welcome to App Builder! Activate Your Account and Get Started";
                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
                string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;

                //messageDto.Message = "<a target='_blank' href='" + url + "' style=''>" + "Please Verify Your Account With This Link" + "</a>";

                string messageBodyText = GetAccountActivationEmailTemplate();

                messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.RegistrationUserName.ToString() + "]", aAppSecurityUserExDto.UserName);
                messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.UserRegisterActivationUrl.ToString() + "]", url);

                messageDto.Message = messageBodyText;

                messageDto.ToList = aAppSecurityUserExDto.Email + ";";
                //messageDto.IsForceUseGlobalSetting = true;
                AppMessageBL.SaveOneAppMessageDto(messageDto);
            }
        }


        public static void SendSaasUserRegisterConfirmationRequestEmail(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            if (aAppSecurityUserExDto != null && !(aAppSecurityUserExDto.IsRegisterCompleted.HasValue && aAppSecurityUserExDto.IsRegisterCompleted.Value))
            {
                string userGuidStr = aAppSecurityUserExDto.GlobalGuid.ToString();
                string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
                string paramStr = EmAppGlobalServiceAction.ConfirmNewSaasUserEmailAndCompleteRegistration.ToString()
                    + "|" + userGuidStr
                    + "|" + sentDateTicksStr;

                string paramStr_encrypted = EncryptParamString(paramStr);
                //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);



                AppMessageDto messageDto = new AppMessageDto();
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
                messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
                messageDto.Subject = "Please Verify Your Account";
                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
                string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;

                messageDto.Message = "<a target='_blank' href='" + url + "' style=''>" + "Please Verify Your Account With This Link" + "</a>";
                messageDto.ToList = aAppSecurityUserExDto.Email + ";";
                //messageDto.IsForceUseGlobalSetting = true;
                AppMessageBL.SaveOneAppMessageDto(messageDto);
            }
        }

        public static OperationCallResult<object> SendUserAccountUnlockEmail(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            if (aAppSecurityUserExDto != null && !(aAppSecurityUserExDto.IsActive))
            {
                string userGuidStr = aAppSecurityUserExDto.GlobalGuid.ToString();
                string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
                string paramStr = EmAppGlobalServiceAction.UnlockSaasUserAccountByEmail.ToString()
                    + "|" + userGuidStr
                    + "|" + sentDateTicksStr;

                if (!string.IsNullOrWhiteSpace(aAppSecurityUserExDto.PostEmailActivationRedirectUrl))
                {
                    paramStr += "|" + aAppSecurityUserExDto.PostEmailActivationRedirectUrl.Trim()
                        + "?email=" + aAppSecurityUserExDto.Email;

                }

                string paramStr_encrypted = EncryptParamString(paramStr);
                //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);



                AppMessageDto messageDto = new AppMessageDto();
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
                messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
                messageDto.Subject = "Activate Your Account";
                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
                string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;
                messageDto.Message = "<a target='_blank' href='" + url + "' style=''>" + "Please Activate Your Account With This Link" + "</a>";
                //messageDto.Message = "Welcome to Fit Concierge! A couple more simple steps until you've completed registration. <br /><a target='_blank' href='" + url + "' style=''>" + "Please click this link to sign in using your email and password. " + "</a>";
                messageDto.ToList = aAppSecurityUserExDto.Email + ";";
                //messageDto.IsForceUseGlobalSetting = true;
                return AppMessageBL.SaveOneAppMessageDto(messageDto);
            }

            return null;
        }

        public static void SendEsitePartnerUserAccountActivationEmail(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            if (aAppSecurityUserExDto != null && !(aAppSecurityUserExDto.IsActive) && !string.IsNullOrWhiteSpace(aAppSecurityUserExDto.PostEmailActivationRedirectUrl))
            {
                string userGuidStr = aAppSecurityUserExDto.GlobalGuid.ToString();
                string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
                string paramStr = EmAppGlobalServiceAction.ActiveEsitePartnerUserByEmail.ToString()
                    + "|" + userGuidStr
                    + "|" + sentDateTicksStr
                    + "|" + (aAppSecurityUserExDto.PostEmailActivationRedirectUrl);// + "?email=" + aAppSecurityUserExDto.Email;



                string paramStr_encrypted = EncryptParamString(paramStr);
                //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);



                AppMessageDto messageDto = new AppMessageDto();
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
                messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
                messageDto.Subject = "Account Activation";
                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
                string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;

                var companyEntity = AppCompanyBL.RetrieveOneAppCompanyEntity(aAppSecurityUserExDto.AppCreatedByCompanyId);
                string companyName = companyEntity.Code;

                //messageDto.Message = "<div style='padding:20px;'>"
                //                   + "<div style='width:100%;padding:20px 0px;'><img src='https://www.fit-concierge.com/images/logo4.jpg'/></div>"
                //                   + "Welcome to Fit Concierge! A couple more simple steps until you've completed registration. <br /><a target='_blank' href='" + url + "' style=''>" + "Please click this link to sign in using your email and password. " + "</a>";


                if (aAppSecurityUserExDto.MessageTempalteId.HasValue)
                {
                    var messageTemplate = AppMessageBL.RetrieveOneAppMessageEntity(aAppSecurityUserExDto.MessageTempalteId.Value);

                    //messageDto.Message = string.Format(messageTemplate.Message, aAppSecurityUserExDto.UserName, url);
                    messageDto.Message = messageTemplate.Message;
                    messageDto.Message = messageDto.Message.Replace("[" + EmAppMessagePlaceHolderToken.RegistrationUserName.ToString() + "]", aAppSecurityUserExDto.UserName);
                    messageDto.Message = messageDto.Message.Replace("[" + EmAppMessagePlaceHolderToken.UserRegisterActivationUrl.ToString() + "]", url);
                }
                else if (!string.IsNullOrWhiteSpace(aAppSecurityUserExDto.MessageTempalte))
                {
                    //messageDto.Message = string.Format(aAppSecurityUserExDto.MessageTempalte, aAppSecurityUserExDto.LogoImgUrl, url);
                    messageDto.Message = aAppSecurityUserExDto.MessageTempalte;
                    messageDto.Message = messageDto.Message.Replace("[" + EmAppMessagePlaceHolderToken.RegistrationUserName.ToString() + "]", aAppSecurityUserExDto.UserName);
                    messageDto.Message = messageDto.Message.Replace("[" + EmAppMessagePlaceHolderToken.UserRegisterActivationUrl.ToString() + "]", url);
                }
                else
                {

                    //messageDto.Message = "You received an invitation from company " + companyEntity.Code + " " + companyEntity.FullName + "<br />"
                    //    + "<a target='_blank' href='" + url + "' style=''>" + "To Join the Company As A " + ((EmAppUserType)(userInvitationExDto.EmUserType.Value)).ToString() + ": Click This Link" + "</a>";


                    string messageBodyText = GetPartnerInvitationEmailTemplate();

                    messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.RegistrationUserName.ToString() + "]", ((EmAppUserType)(aAppSecurityUserExDto.DomainId)).ToString());
                    messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.UserRegisterActivationUrl.ToString() + "]", url);
                    messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.CurrentCompanyName.ToString() + "]", companyName);

                    messageDto.Message = messageBodyText;

                }




                messageDto.ToList = aAppSecurityUserExDto.Email + ";";
                //messageDto.IsForceUseGlobalSetting = true;
                AppMessageBL.SaveOneAppMessageDto(messageDto);
            }
        }


        public static OperationCallResult<object> SendUserPasswordRetrieveEmail(List<AppSecurityUserExDto> userDtoList)
        {
            if (userDtoList != null && userDtoList.Count > 0)
            {
                AppMessageDto messageDto = new AppMessageDto();
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
                messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
                messageDto.ToList = userDtoList.First().Email + ";";
                //messageDto.IsForceUseGlobalSetting = true;
                messageDto.Subject = "Retrieve Your Password";
                messageDto.Message = "";

                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

                foreach (var aAppSecurityUserExDto in userDtoList)
                {
                    string userGuidStr = aAppSecurityUserExDto.GlobalGuid.ToString();
                    string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
                    string paramStr = EmAppGlobalServiceAction.UserPasswordRetrieve.ToString()
                        + "|" + userGuidStr
                        + "|" + sentDateTicksStr;

                    string paramStr_encrypted = EncryptParamString(paramStr);
                    //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);                                       

                    string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;
                    messageDto.Message += "<a target='_blank' href='" + url + "' style=''>" + "Click Here To Retrieve The Password For User: " + aAppSecurityUserExDto.UserName + ".</a><br /><br />";
                }

                return AppMessageBL.SaveOneAppMessageDto(messageDto);
            }

            return null;
        }

        //public static OperationCallResult<object> SendUserPasswordRetrieveEmailByLoginNameOrEmail(string loginNameOrEmail, string logoImgUrl)
        //{
        //    OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    AppSecurityUserEntity userEntity = AppSecurityAuthenticationBL.GetUserAccountbyLogName(loginNameOrEmail);

        //    if (userEntity == null)
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "SendUserPasswordRetrieveEmailByLoginNameOrEmail_ThisUserDoesNotExist", ValidationItemType.Error, "This user does not exist."));
        //        return aOperationCallResult;
        //    }
        //    else // user found
        //    {
        //        AppMessageDto messageDto = new AppMessageDto();
        //        messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
        //        messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
        //        messageDto.ToList = userEntity.Email + ";";
        //        //messageDto.IsForceUseGlobalSetting = true;
        //        messageDto.Subject = "Retrieve Your Password";
        //        messageDto.Message = "";

        //        string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

        //        string userGuidStr = userEntity.GlobalGuid.ToString();
        //        string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
        //        string paramStr = EmAppGlobalServiceAction.UserPasswordRetrieve.ToString()
        //            + "|" + userGuidStr
        //            + "|" + sentDateTicksStr;

        //        string paramStr_encrypted = EncryptParamString(paramStr);
        //        //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);                                       

        //        string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;
        //        messageDto.Message += "<a target='_blank' href='" + url + "' style=''>" + "Click Here To Retrieve The Password For User: " + userEntity.UserName + ".</a><br /><br />";


        //        return AppMessageBL.SaveOneAppMessageDto(messageDto);
        //    }
        //}


        //public static OperationCallResult<object> SendEsiteUserPasswordRetrieveEmailByLoginNameOrEmail(string loginNameOrEmail, string eSiteBaseUrl, string logoImgUrl)
        //{
        //    OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    AppSecurityUserEntity userEntity = AppSecurityAuthenticationBL.GetUserAccountbyLogName(loginNameOrEmail);

        //    if (string.IsNullOrWhiteSpace(eSiteBaseUrl))
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "SendUserPasswordRetrieveEmailByLoginNameOrEmail_WrongSiteUrl", ValidationItemType.Error, "Wrong site url."));
        //        return aOperationCallResult;
        //    }

        //    if (userEntity == null)
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "SendUserPasswordRetrieveEmailByLoginNameOrEmail_ThisUserDoesNotExist", ValidationItemType.Error, "This user does not exist."));
        //        return aOperationCallResult;
        //    }

        //    AppMessageDto messageDto = new AppMessageDto();
        //    messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
        //    messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
        //    messageDto.ToList = userEntity.Email + ";";
        //    messageDto.IsForceUseGlobalSetting = true;
        //    messageDto.Subject = "Retrieve Your Password";
        //    messageDto.Message = "";


        //    string userGuidStr = userEntity.GlobalGuid.ToString();
        //    string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
        //    string paramStr = userGuidStr
        //        + "|" + sentDateTicksStr;

        //    string paramStr_encrypted = EncryptParamString(paramStr);
        //    paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted);

        //    string url = eSiteBaseUrl + "#/main/UserPasswordRetrieve/" + "?paramstr=" + paramStr_encrypted;
        //    messageDto.Message += "<a target='_blank' href='" + url + "' style=''>" + "Click Here To Retrieve The Password For User: " + userEntity.UserName + ".</a><br /><br />";


        //    return AppMessageBL.SaveOneAppMessageDto(messageDto);


        //}

        public static OperationCallResult<object> SendEsiteUserPasswordRetrieveEmailByLoginNameOrEmail(string loginNameOrEmail, string postEmailActivationRedirectUrl, string logoImgUrl, string messageTempalte, int? callinfFrom)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserEntity userEntity = AppSecurityAuthenticationBL.GetUserAccountbyLogName(loginNameOrEmail);

            if (userEntity == null)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "SendUserPasswordRetrieveEmailByLoginNameOrEmail_ThisUserDoesNotExist", ValidationItemType.Error, "This user does not exist."));
                return aOperationCallResult;
            }
            else // user found
            {
                AppMessageDto messageDto = new AppMessageDto();
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
                messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
                messageDto.ToList = userEntity.Email + ";";
                //messageDto.IsForceUseGlobalSetting = true;
                messageDto.Subject = "Retrieve Your Password";
                messageDto.Message = "";

                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

                string userGuidStr = userEntity.GlobalGuid.ToString();
                string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
                string paramStr =
                    EmAppGlobalServiceAction.UserPasswordRetrieve.ToString()
                    + "|" + userGuidStr
                    + "|" + sentDateTicksStr;

                if (!string.IsNullOrWhiteSpace(postEmailActivationRedirectUrl))
                {
                    paramStr += "|" + postEmailActivationRedirectUrl.Trim() + "?email=" + loginNameOrEmail;
                }

                string paramStr_encrypted = EncryptParamString(paramStr);
                //paramStr_encrypted = APP.Persistence.Common.StringUtil.UrlEncode(paramStr_encrypted); 
                // if call from mobile                                      

                string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;

                string secondaryUrl = url;

                if (callinfFrom.HasValue && callinfFrom.Value == (int)EmClientAgentCallingFrom.MobileApp)
                {
                    url = applicationUrl + string.Format("/MobileDeepLink.html?servicename={0}&email={1}&DeepLinkType={2}", paramStr_encrypted, loginNameOrEmail, "paswordReset");
                }

                if (!string.IsNullOrWhiteSpace(messageTempalte))
                {
                    messageDto.Message += string.Format(messageTempalte, logoImgUrl, url, url, secondaryUrl, secondaryUrl);
                }
                else
                {
                    messageDto.Message += "<a target='_blank' href='" + url + "' style=''>" + "Click Here To Retrieve The Password For User: " + userEntity.UserName + ".</a><br /><br />";
                }



                var returnObject = AppMessageBL.SaveOneAppMessageDto(messageDto);
                returnObject.Object = paramStr_encrypted;

                return returnObject;

            }
        }


        public static OperationCallResult<AppSecurityUserExDto> CompleteInvitedSaasBusinessParternerUserRegistration(int userId, int invitedByCompanyId, int? businessPartnerId, bool isNeedActiveByEmail = false, string postEmailActivationRedirectUrl = "", bool isQuickRegistration = false, string logoImgUrl = "", string messageTemplate = "", int? messageTemplateId = null, int? needToAssignPartnerAdminRoleType = null)
        {
            OperationCallResult<AppSecurityUserExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserSimpleEntity(userId);
            if (businessPartnerId.HasValue)
            {
                if (userEntity != null)
                {

                    AppSecurityUserExDto aAppSecurityUserExDto = AppSecurityUserConverter.ConvertEntityToExDto(userEntity);

                    bool isPostProcessSuccess = LinkExistingUserToBusinessParternCompnay(aAppSecurityUserExDto, invitedByCompanyId, businessPartnerId);

                    if (isPostProcessSuccess)
                    {
                        using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
                        {

                            try
                            {
                                if (isNeedActiveByEmail)
                                {
                                    userEntity.IsActive = false;
                                    userEntity.Addomain = EmAppAuthenticationResult.NewUserNotActivedByEmail.ToString();
                                }
                                else
                                {
                                    userEntity.IsActive = true;
                                }

                                userEntity.IsRegisterCompleted = true;

                                adapter.UpdateEntitiesDirectly(userEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == userEntity.UserId));
                                adapter.Commit();
                            }
                            // Database FK Exception .......
                            catch (ORMQueryExecutionException ex)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_AppTransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                                adapter.Rollback();
                            }
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UserRegister_CreateUserCompanyDatabaseFailed", ValidationItemType.Error, "Add Invited User Failed."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_UserAlreadyActived_Message", ValidationItemType.Message, "User had already been actived. You don't need to active it again."));
                }

                if (!aValidationResult.HasErrors)
                {
                    var saasUserDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(userEntity.UserId);
                    saasUserDto.LogoImgUrl = logoImgUrl;
                    saasUserDto.MessageTempalte = messageTemplate;
                    saasUserDto.MessageTempalteId = messageTemplateId;


                    if (needToAssignPartnerAdminRoleType.HasValue)
                    {
                        if (saasUserDto.AppSecurityGroupMemberList.Count == 0)
                        {
                            if (needToAssignPartnerAdminRoleType.Value == (int)EmAppUserType.Customer)
                            {
                                saasUserDto.AppSecurityGroupMemberList.Add(new AppSecurityGroupMemberExDto()
                                {
                                    GroupId = (int)EmAppSecurityGroupInernalCode.CustomerAdmin,
                                    UserId = (int)saasUserDto.Id
                                });
                            }
                            else if (needToAssignPartnerAdminRoleType.Value == (int)EmAppUserType.Supplier)
                            {
                                saasUserDto.AppSecurityGroupMemberList.Add(new AppSecurityGroupMemberExDto()
                                {
                                    GroupId = (int)EmAppSecurityGroupInernalCode.SupplierAdmin,
                                    UserId = (int)saasUserDto.Id
                                });
                            }
                            else if (needToAssignPartnerAdminRoleType.Value == (int)EmAppUserType.ClientAgent)
                            {
                                saasUserDto.AppSecurityGroupMemberList.Add(new AppSecurityGroupMemberExDto()
                                {
                                    GroupId = (int)EmAppSecurityGroupInernalCode.ClientAgentAdmin,
                                    UserId = (int)saasUserDto.Id
                                });
                            }
                            else if (needToAssignPartnerAdminRoleType.Value == (int)EmAppUserType.SupplierAgent)
                            {
                                saasUserDto.AppSecurityGroupMemberList.Add(new AppSecurityGroupMemberExDto()
                                {
                                    GroupId = (int)EmAppSecurityGroupInernalCode.SupplierAgentAdmin,
                                    UserId = (int)saasUserDto.Id
                                });
                            }

                            if (saasUserDto.AppSecurityGroupMemberList.Count > 0)
                            {
                                saasUserDto.Password = "password";
                                saasUserDto.ConfirmPassword = "password";

                                var userRoleUssaveResult = AppSecurityUserBL.SaveAppSecurityUserExDto(saasUserDto);

                                if (!userRoleUssaveResult.IsSuccessful)
                                {
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UserRegister_CreateUserCompanyDatabaseFailed", ValidationItemType.Error, "Assign user to partner admin role faild."));
                                }
                            }
                        }


                    }
                    saasUserDto.NewBusinessAccountId = businessPartnerId.Value;
                    aOperationCallResult.Object = saasUserDto;


                    if (saasUserDto.IsActive)
                    {
                        if (!isQuickRegistration)
                        {
                            saasUserDto.PostEmailActivationRedirectUrl = postEmailActivationRedirectUrl;
                            SendAccountCreatedConfirmationMessage(saasUserDto);
                        }
                    }
                    else
                    {
                        if (isNeedActiveByEmail && !string.IsNullOrWhiteSpace(postEmailActivationRedirectUrl))
                        {
                            saasUserDto.PostEmailActivationRedirectUrl = postEmailActivationRedirectUrl;
                            SendEsitePartnerUserAccountActivationEmail(saasUserDto);
                        }
                        else
                        {
                            SendUserAccountUnlockEmail(saasUserDto);
                        }
                    }
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_CannotFindBusinessPartner_Error", ValidationItemType.Message, "System cannot find this business partner."));
            }

            return aOperationCallResult;
        }



        //public static void AddInvitedBusinessPartnerUserToCompany(int? invitingCompanyId, int? invitationId)
        //{
        //    string connString = "";
        //    string dbname = "";

        //    AppSaasAccountUserBL.GetCompanyDBConnection(invitingCompanyId, ref connString, ref dbname);

        //    if (!string.IsNullOrWhiteSpace(connString) && !string.IsNullOrWhiteSpace(dbname))
        //    {
        //        using (var adpater = new DataAccessAdapter(connString, dbname))
        //        {
        //            try
        //            {
        //                AppSecurityUserInvitationEntity invitationEntity = new AppSecurityUserInvitationEntity(invitationId.Value);
        //                adpater.FetchEntity(invitationEntity);

        //                if (invitationEntity != null
        //                    && invitationEntity.EmUserType.HasValue && !string.IsNullOrWhiteSpace(invitationEntity.InvitedUserEmail))
        //                {
        //                    var existUserEntity = RetrieveOneAppSecurityUserEntityByEmail(invitationEntity.InvitedUserEmail);

        //                    if (existUserEntity != null)
        //                    {

        //                    }
        //                    else
        //                    {

        //                    }
        //                }

        //            }
        //            catch (Exception ex)
        //            {
        //                adpater.Rollback();
        //            }
        //        }
        //    }
        //}

        // Delete a User
        public static OperationCallResult<object> DeleteAppSaasAccountUser(object userId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();
            string referMsg = string.Empty;
            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
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

            return aValidationResult;
        }


        public static AppBusinessPartnerInviteUserExDto RetrieveOneAppBusinessPartnerInviteUserExDto(object id)
        {

            AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity = RetrieveOneAppBusinessPartnerInviteUserEntity(id);
            AppBusinessPartnerInviteUserExDto aAppBusinessPartnerInviteUserExDto = AppBusinessPartnerInviteUserConverter.ConvertEntityToExDto(aAppBusinessPartnerInviteUserEntity);
            return aAppBusinessPartnerInviteUserExDto;
        }

        public static AppSecurityUserExDto RetrieveOneAppSecurityUserExDtoByGuid(Guid userGuid)
        {
            AppSecurityUserEntity entity = RetrieveOneAppSecurityUserEntityByGuid(userGuid);
            if (entity != null)
            {
                return AppSecurityUserConverter.ConvertEntityToExDto(entity);
            }

            return null;
        }

        public static AppSecurityUserExDto RetrieveOneAppSecurityUserExDtoByEmail(string email)
        {
            AppSecurityUserEntity entity = RetrieveOneAppSecurityUserEntityByEmail(email);
            if (entity != null)
            {
                return AppSecurityUserConverter.ConvertEntityToExDto(entity);
            }

            return null;
        }

        public static List<AppSecurityUserExDto> RetrieveAppSecurityUserExDtoListByEmail(string email, string loginName = "")
        {
            List<AppSecurityUserExDto> toReturn = new List<AppSecurityUserExDto>();
            List<AppSecurityUserEntity> entityList = RetrieveAppSecurityUserEntityListByEmail(email, loginName);
            if (entityList != null)
            {
                foreach (var entity in entityList)
                {
                    var dto = AppSecurityUserConverter.ConvertEntityToExDto(entity);
                    toReturn.Add(dto);
                }

            }

            return toReturn;
        }


        public static List<AppSecurityUserInvitationDto> RetrievePendingUserInvitationList(int? businessPartnerId)
        {
            List<AppSecurityUserInvitationDto> toReturn = new List<AppSecurityUserInvitationDto>();

            EntityCollection<AppSecurityUserInvitationEntity> entityList = new EntityCollection<AppSecurityUserInvitationEntity>();
            RelationPredicateBucket filter = null;

            if (businessPartnerId.HasValue)
            {
                filter = new RelationPredicateBucket(AppSecurityUserInvitationFields.InvitingBusinessPartnerId == businessPartnerId.Value);
            }

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                adapter.FetchEntityCollection(entityList, filter);
            }

            foreach (var entity in entityList)
            {
                AppSecurityUserInvitationDto dto = AppSecurityUserInvitationConverter.ConvertEntityToDto(entity);
                toReturn.Add(dto);
            }

            return toReturn;
        }

        public static AppSecurityUserInvitationEntity RetrieveOneAppSecurityUserInvitationEntity(object invitationId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                AppSecurityUserInvitationEntity invitationEntity = new AppSecurityUserInvitationEntity(int.Parse(invitationId.ToString()));
                adpater.FetchEntity(invitationEntity);
                return invitationEntity;
            }

        }

        public static OperationCallResult<AppSecurityUserInvitationExDto> CreateSaasBusinessPaternerUserInvitation(AppSecurityUserInvitationExDto userInvitationExDto)
        {
            OperationCallResult<AppSecurityUserInvitationExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserInvitationExDto>();

            userInvitationExDto.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;




            var aValidationResult = userInvitationExDto.ValidateDto();

            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(userInvitationExDto.InvitedUserEmail, null));

            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();

            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity = new AppSecurityUserInvitationEntity();

            AppSecurityUserInvitationConverter.CopyDtoToEntity(aAppSecurityUserInvitationEntity, userInvitationExDto);

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityUserInvitationEntity);
                    adapter.Commit();

                    userInvitationExDto.Id = aAppSecurityUserInvitationEntity.InvitationId; ;

                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserInvitationEntity), "app_AppSecurityUserInvitationEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                var sendValidationResult = SendBusinessPaternerUserInvitationMessage(userInvitationExDto);

                if (!sendValidationResult.HasErrors)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserInvitationEntity), "app_AppSecurityUserInvitationEntity_Save_OK", ValidationItemType.Message, "An invitation email with account activation link has been sent to " + userInvitationExDto.InvitedUserEmail + "."));
                }
                else
                {
                    aValidationResult.Merge(sendValidationResult);
                }

            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppBusinessPartnerInviteUserExDto> SaveOneAppBusinessPartnerInviteUserEntityExDto(AppBusinessPartnerInviteUserExDto aAppBusinessPartnerInviteUserExDto)
        {
            OperationCallResult<AppBusinessPartnerInviteUserExDto> aOperationCallResult = new OperationCallResult<AppBusinessPartnerInviteUserExDto>();

            var aValidationResult = aAppBusinessPartnerInviteUserExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;




            if (aAppBusinessPartnerInviteUserExDto.IsNew)
            {
                validationResult.Merge(SaveOneAppBusinessPartnerInviteUserEntityExDto_ProcessNewDto(aAppBusinessPartnerInviteUserExDto));
            }
            else if (aAppBusinessPartnerInviteUserExDto.IsModified)
            {
                validationResult.Merge(SaveOneAppBusinessPartnerInviteUserEntityExDto_ProcessDirtyDto(aAppBusinessPartnerInviteUserExDto));
            }


            if (!validationResult.HasErrors)
            {

                aOperationCallResult.Object = RetrieveOneAppBusinessPartnerInviteUserExDto(aAppBusinessPartnerInviteUserExDto.Id);
            }

            return aOperationCallResult;
        }



        public static int? GetDefaultOrgniazationId(DataAccessAdapter adpater)
        {
            int? defaultOrganizationId = null;

            EntityCollection<AppComOrganizationEntity> organizationList = new EntityCollection<AppComOrganizationEntity>();


            IRelationPredicateBucket filter = new RelationPredicateBucket(
                AppComOrganizationFields.UserTypeEm == 2
                & AppComOrganizationFields.BelongToId == System.DBNull.Value);

            adpater.FetchEntityCollection(organizationList, filter);

            if (organizationList.Count > 0)
            {
                defaultOrganizationId = organizationList[0].OrganizationId;
            }

            return defaultOrganizationId;
        }

        public static int? GetDefaultLanguageId(DataAccessAdapter adpater)
        {
            int? defaultLanguageId = null;

            EntityCollection<AppLanguageEntity> entityList = new EntityCollection<AppLanguageEntity>();

            adpater.FetchEntityCollection(entityList, null);

            if (entityList.Count > 0)
            {
                defaultLanguageId = entityList[0].LanguageId;
            }

            return defaultLanguageId;
        }


        public static string EncryptParamString(string paramString, string token = "")
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                token = DefaultEncryptSaltKey;
            }

            return EnDeCrypt.Encrypt(paramString, token);
        }

        public static string[] DecryptParamString(string paramString, string token = "")
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                token = DefaultEncryptSaltKey;
            }

            if (!string.IsNullOrWhiteSpace(paramString))
            {
                paramString = paramString.Replace(" ", "+");
                int mod4 = paramString.Length % 4;
                if (mod4 > 0)
                {
                    paramString += new string('=', 4 - mod4);
                }

                string paramStr_decrypted = EnDeCrypt.Decrypt(paramString, token);

                if (!paramStr_decrypted.IsEmpty())
                {
                    string[] paramArray = paramStr_decrypted.Split('|');

                    return paramArray;
                }
            }

            return null;
        }

        public static string EncryptCompanyIdString(int? companyId)
        {
            if (companyId.HasValue)
            {
                return EncryptParamString(companyId.Value.ToString());
            }
            else
            {
                return "";
            }
        }

        public static int? DecryptCompanyIdString(string paramString)
        {
            string token = DefaultEncryptSaltKey;

            if (!string.IsNullOrWhiteSpace(paramString))
            {
                paramString = paramString.Replace(" ", "+");
                int mod4 = paramString.Length % 4;
                if (mod4 > 0)
                {
                    paramString += new string('=', 4 - mod4);
                }

                string paramStr_decrypted = EnDeCrypt.Decrypt(paramString, token);

                return ControlTypeValueConverter.ConvertValueToInt(paramStr_decrypted);
            }

            return null;
        }


        public static AppSecurityUserInvitationEntity RetrieveOneAppSecurityUserInvitationEntityByCompany(int? invitingCompanyId, int? invitationId)
        {
            string connString = "";
            string dbname = "";

            AppSaasAccountUserBL.GetInvitingCompanyDBConnection(invitingCompanyId, ref connString, ref dbname);

            if (!string.IsNullOrWhiteSpace(connString) && !string.IsNullOrWhiteSpace(dbname))
            {
                using (var adpater = new DataAccessAdapter(connString))
                {
                    AppSecurityUserInvitationEntity invitationEntity = new AppSecurityUserInvitationEntity(invitationId.Value);
                    adpater.FetchEntity(invitationEntity);

                    return invitationEntity;
                }
            }

            return null;
        }

        public static AppSecurityUserEntity SaveSaasNewUser(AppSecurityUserExDto appSecurityUserExDto, ValidationResult aValidationResult, Guid? newUserGuid = null)
        {
            AppSecurityUserEntity aAppSecurityUserEntity = new AppSecurityUserEntity();
            AppSecurityUserConverter.CopyDtoToEntity(aAppSecurityUserEntity, appSecurityUserExDto);
            if (!newUserGuid.HasValue)
            {
                newUserGuid = Guid.NewGuid();
            }

            aAppSecurityUserEntity.GlobalGuid = newUserGuid;
            aAppSecurityUserEntity.AppCreatedDate = System.DateTime.UtcNow;
            aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;
            aAppSecurityUserEntity.IsBuiltIntUser = false;


            if (string.IsNullOrWhiteSpace(aAppSecurityUserEntity.TimeZoneInfoToken))
            {
                aAppSecurityUserEntity.TimeZoneInfoToken = "Eastern Standard Time";
            }

            if (string.IsNullOrWhiteSpace(aAppSecurityUserEntity.CultureInfoCode))
            {
                aAppSecurityUserEntity.CultureInfoCode = "en-CA";
            }


            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    if (!aAppSecurityUserEntity.LanguageId.HasValue)
                    {
                        int? defaultLanguageId = GetDefaultLanguageId(adapter);
                        aAppSecurityUserEntity.LanguageId = defaultLanguageId;
                    }

                    aAppSecurityUserEntity.UserLanguage = aAppSecurityUserEntity.LanguageId;


                    int? userId = null;
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityUserEntity);
                    userId = aAppSecurityUserEntity.UserId;
                    adapter.Commit();
                    appSecurityUserExDto.Id = userId;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_Save_OK", ValidationItemType.Message, "Save Successful"));

                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_SecurityUserEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aAppSecurityUserEntity;
        }







        public static OperationCallResult<object> UninstallSaasAccount(int companyId)
        {
            OperationCallResult<object> operationCallResult = new OperationCallResult<object>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;


            if (!AppSecurityUserBL.IsAdminUser())
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_accessdenied_error", ValidationItemType.Error, "Access Denied."));
                return operationCallResult;
            }


            AppCompanyDto companyDto = AppCompanyBL.RetrieveOneSaasCompanyDto(companyId);

            if (!(companyDto.DataSourceRegisterInfo != null && !string.IsNullOrWhiteSpace(companyDto.DataSourceRegisterInfo.DatabaseName)))
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot find company database."));
            }
            else if (companyDto.DataSourceRegisterInfo.DatabaseName.ToLower() == HostCompanyDbName.ToLower())
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot uninstall host company database."));
            }
            else if (string.IsNullOrWhiteSpace(companyDto.CompanyDomainIdentityToken))
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot find company domain."));
            }

            if (!validationResult.HasErrors)
            {
                UninstallSaasAccount_RemoveDatabase(validationResult, companyDto);

                UninstallSaasAccount_RemoveWebApplication(validationResult, companyDto);

                UninstallSaasAccount_RemoveFiles(validationResult, companyDto);
            }

            if (!validationResult.HasErrors)
            {
                if (validationResult.HasWarnings)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserInvitationEntity), "app_UninstallSaasAccount_OK", ValidationItemType.Message, "Uninstall Successful With Warning.\n"));
                }
                else
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserInvitationEntity), "app_UninstallSaasAccount_OK", ValidationItemType.Message, "Uninstall Successful"));
                }

            }

            return operationCallResult;
        }


        public static OperationCallResult<object> ConvertOneSaasAccountToStandaloneApplication(int companyId)
        {
            OperationCallResult<object> operationCallResult = new OperationCallResult<object>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;


            if (!AppSecurityUserBL.IsAdminUser())
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_accessdenied_error", ValidationItemType.Error, "Access Denied."));
                return operationCallResult;
            }


            AppCompanyDto companyDto = AppCompanyBL.RetrieveOneSaasCompanyDto(companyId);

            if (!(companyDto.DataSourceRegisterInfo != null && !string.IsNullOrWhiteSpace(companyDto.DataSourceRegisterInfo.DatabaseName)))
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot find company database."));
            }
            else if (companyDto.DataSourceRegisterInfo.DatabaseName.ToLower() == HostCompanyDbName.ToLower())
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot convert host company database to standalone application database."));
            }
            else if (string.IsNullOrWhiteSpace(companyDto.CompanyDomainIdentityToken))
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot find company domain."));
            }

            if (!validationResult.HasErrors)
            {
                ConvertToStandaloneApplication_ConvertFiles(validationResult, companyDto);

                if (!validationResult.HasErrors)
                {
                    ConvertToStandaloneApplication_ConvertWebApplication(validationResult, companyDto);

                    if (!validationResult.HasErrors)
                    {
                        ConvertToStandaloneApplication_ConvertDatabase(validationResult, companyDto);
                        UninstallSaasAccount_RemoveFiles(validationResult, companyDto);
                    }
                }
            }

            if (!validationResult.HasErrors)
            {
                if (validationResult.HasWarnings)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserInvitationEntity), "app_UninstallSaasAccount_OK", ValidationItemType.Message, "Convert Successful With Warning.\n"));
                }
                else
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserInvitationEntity), "app_UninstallSaasAccount_OK", ValidationItemType.Message, "Convert Successful"));
                }

            }

            return operationCallResult;
        }

        private static void ConvertToStandaloneApplication_ConvertFiles(ValidationResult validationResult, AppCompanyDto companyDto)
        {
            int companyId = (int)companyDto.Id;

            string hostMgtPhsyicalPath = AppDomain.CurrentDomain.BaseDirectory;
            string companyFilesPhsyicalPath = Path.Combine(hostMgtPhsyicalPath, @"FileRepository\Company_" + companyId);

            string parentPath = RemoveLastTwoSubfolders(hostMgtPhsyicalPath);

            if (!string.IsNullOrWhiteSpace(parentPath))
            { 
                try
                {
                    string targetFolderPath = Path.Combine(parentPath, StandAloneWebAppFolderName + @"\" + StandAloneWebAppCompanyFolderPrefix + companyId);
                    

                    DirectoryInfo directoryInfo = Directory.CreateDirectory(targetFolderPath);                  
                    DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();


                    string appPoolName = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationPoolName);
                    string appPoolIdentity = @"IIS AppPool\" + appPoolName;
                    FileSystemAccessRule rule = new FileSystemAccessRule(
                        appPoolIdentity,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow
                    );

                    // Add the rule to the directory's security descriptor
                    directorySecurity.AddAccessRule(rule);

                    // Set the modified security descriptor back to the directory
                    directoryInfo.SetAccessControl(directorySecurity);



                    // Specify the Users group
                    string usersGroup = "Users";

                    // Grant read and execute access to the Users group
                    FileSystemAccessRule ruleUsers = new FileSystemAccessRule(
                        usersGroup,
                        FileSystemRights.ReadAndExecute,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow
                    );

                    // Add the rule to the directory's security descriptor
                    directorySecurity.AddAccessRule(ruleUsers);

                    // Set the modified security descriptor back to the directory
                    directoryInfo.SetAccessControl(directorySecurity);


                    string comapnyOrgSubPath = @"\FileRepository\Company_" + companyId;

                    foreach (string orgPath in Directory.GetDirectories(hostMgtPhsyicalPath, "*", SearchOption.AllDirectories))
                    {

                        if (orgPath.IndexOf(@"\FileRepository\Company_") >= 0 && orgPath.IndexOf(comapnyOrgSubPath + @"\") < 0 && !orgPath.EndsWith(comapnyOrgSubPath))
                        {
                            continue;
                        }
                        else
                        {
                            Directory.CreateDirectory(orgPath.Replace(hostMgtPhsyicalPath, targetFolderPath + @"\"));
                        }
                    }

                    foreach (string orgFilePath in Directory.GetFiles(hostMgtPhsyicalPath, "*.*", SearchOption.AllDirectories))
                    {
                        if (orgFilePath.IndexOf(@"\FileRepository\Company_") >= 0 && orgFilePath.IndexOf(comapnyOrgSubPath + @"\") < 0)
                        {
                            continue;
                        }
                        else
                        {
                            File.Copy(orgFilePath, orgFilePath.Replace(hostMgtPhsyicalPath, @targetFolderPath + @"\"), true);
                        }

                    }

                    ConvertToStandaloneApplicatoin_UptateWebConfigDbName(companyDto, targetFolderPath);

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_ConvertToStandaloneApplication_error", ValidationItemType.Error, "\"Convert files failed.\n" + ex.ToString()));
                }
            }
            else
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_ConvertToStandaloneApplication_error", ValidationItemType.Error, "\"Convert files failed. Cannot find destination folder."));
            }
        }

        private static void ConvertToStandaloneApplicatoin_UptateWebConfigDbName(AppCompanyDto companyDto, string targetFolderPath)
        {
            string webConfigFilePath = Path.Combine(targetFolderPath, "web.config");
            string originalString = File.ReadAllText(webConfigFilePath);
            string dbName = companyDto.DataSourceRegisterInfo.DatabaseName;
            originalString = originalString.Replace("value=\"" + HostCompanyDbName + "\"", "value=\"" + dbName + "\"");
            originalString = originalString.Replace("=" + HostCompanyDbName + ";", "=" + dbName + ";");

            File.WriteAllText(webConfigFilePath, originalString);
        }

        private static string RemoveLastTwoSubfolders(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path = path.Substring(0, path.Length - 1);
            }

            string[] pathParts = path.Split(Path.DirectorySeparatorChar);

            if (pathParts.Length >= 3)
            {
                int newLength = pathParts.Length - 2;
                string[] newPathParts = new string[newLength];
                Array.Copy(pathParts, newPathParts, newLength);

                return string.Join(Path.DirectorySeparatorChar.ToString(), newPathParts) + Path.DirectorySeparatorChar;
            }
            else
            {
                return "";
            }
        }

        private static void ConvertToStandaloneApplication_ConvertWebApplication(ValidationResult validationResult, AppCompanyDto companyDto)
        {
            string applicationName = companyDto.CompanyDomainIdentityToken;
            string mgtApplicationPath = applicationName + "/MGT";
            string iisWebSiteName = AppSystemSettingBL.GetStringValue(EmSystemSettings.IISWebSiteName);

            string hostMgtPhsyicalPath = AppDomain.CurrentDomain.BaseDirectory;
            string companyFilesPhsyicalPath = Path.Combine(hostMgtPhsyicalPath, @"FileRepository\Company_" + companyDto.Id);
            string parentPath = RemoveLastTwoSubfolders(hostMgtPhsyicalPath);

            if (!string.IsNullOrWhiteSpace(parentPath))
            {

                try
                {
                    string mgtPhsyicalPath = Path.Combine(parentPath, StandAloneWebAppFolderName + @"\" + StandAloneWebAppCompanyFolderPrefix + companyDto.Id);
                    string defaultSitePhsyicalPath = Path.Combine(mgtPhsyicalPath, @"FileRepository\Company_" + companyDto.Id + @"\WebSite\Site_1");

                    bool isUpdateDefaultSiteSuccess = IISHelper.ChangeApplicatoinPhysicalPath(iisWebSiteName, applicationName, defaultSitePhsyicalPath);
                    bool isUpdateMgtSuccess = IISHelper.ChangeApplicatoinPhysicalPath(iisWebSiteName, mgtApplicationPath, mgtPhsyicalPath);


                    if (!isUpdateDefaultSiteSuccess || !isUpdateMgtSuccess)
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Warning, "Update IIS Application Path Faield."));
                    }

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Warning, "Update IIS Application Path Faield. \n\n" + ex.ToString()));
                }
            }
        }



        private static void ConvertToStandaloneApplication_ConvertDatabase(ValidationResult validationResult, AppCompanyDto companyDto)
        {
            int companyId = (int)companyDto.Id;
            string dbName = companyDto.DataSourceRegisterInfo.DatabaseName;
            int dataSourceRegId = (int)companyDto.DataSourceRegisterInfo.Id;

            string query = @"";

            query += @"
                            DELETE FROM AppDataSourceRegister where DataSourceID = " + dataSourceRegId + @"
                            DELETE FROM AppDataSourceRegister where DataSourceOwnerCompanyID = " + companyId + @";
                            DELETE FROM AppSecurityUserSession where UserID in (
	                            SELECT UserID FROM AppSecurityUser where MyOwnCompnanyID = " + companyId + @"
                            );
                            DELETE FROM AppSecurityUser where MyOwnCompnanyID = " + companyId + @";
                            DELETE FROM AppCompany where AppCompanyID = " + companyId + @";
                        ";

            using (var adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    adpater.ExecuteExecuteNonQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Warning,
                        "Error occurs during converting database [" + dbName + "]. \n" + ex.ToString()));
                }
            }

        }



        private static void UninstallSaasAccount_RemoveWebApplication(ValidationResult validationResult, AppCompanyDto companyDto)
        {
            string applicationName = companyDto.CompanyDomainIdentityToken;
            string iisWebSiteName = AppSystemSettingBL.GetStringValue(EmSystemSettings.IISWebSiteName);


            try
            {
                bool isSuccess = IISHelper.DeleteApplicatoin(iisWebSiteName, applicationName);

                if (!isSuccess)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Warning, "Remove IIS Application " + applicationName + " Faield."));
                }

            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Warning, "Remove IIS Application " + applicationName + " Faield. \n\n" + ex.ToString()));
            }
        }


        private static void UninstallSaasAccount_RemoveFiles(ValidationResult validationResult, AppCompanyDto companyDto)
        {
            int companyId = (int)companyDto.Id;

            string hostMgtPhsyicalPath = AppDomain.CurrentDomain.BaseDirectory;
            string companyFilesPhsyicalPath = hostMgtPhsyicalPath + "FileRepository\\Company_" + companyId;

            try
            {
                bool isSuccess = AppEsiteFileBL.DeleteOneFileFolderByPath(companyFilesPhsyicalPath);

                if (!isSuccess)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Warning, "Some files cannot be deleted from the original company files folder. They may be currently in use..\n\n"));
                }

            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Some files cannot be deleted from the original company files folder. They may be currently in use..\n\n"));
            }
        }


        private static void UninstallSaasAccount_RemoveDatabase(ValidationResult validationResult, AppCompanyDto companyDto)
        {
            int companyId = (int)companyDto.Id;
            string dbName = companyDto.DataSourceRegisterInfo.DatabaseName;
            int dataSourceRegId = (int)companyDto.DataSourceRegisterInfo.Id;



            string query = @"
                            ALTER DATABASE [" + dbName + @"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE [" + dbName + @"];                      
                        ";

            query += @"
                            DELETE FROM AppDataSourceRegister where DataSourceID = " + dataSourceRegId + @";
                            DELETE FROM AppDataSourceRegister where DataSourceOwnerCompanyID = " + companyId + @";
                            DELETE FROM AppSecurityUserSession where UserID in (
	                            SELECT UserID FROM AppSecurityUser where MyOwnCompnanyID = " + companyId + @"
                            );
                            DELETE FROM AppSecurityUser where MyOwnCompnanyID = " + companyId + @";
                            DELETE FROM AppCompany where AppCompanyID = " + companyId + @";
                        ";

            //DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(AppCacheManagerBL.HostCompayDataBaseID, null);
            //string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

            //if (!string.IsNullOrWhiteSpace(errorMsg))
            //{
            //    validationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_databasedoesnotexist", ValidationItemType.Error, "Uninstall failed. \n\nError Details:\n" + errorMsg));
            //}

            using (var adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    adpater.ExecuteExecuteNonQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Warning,
                        "Cannot delete database [" + dbName + "]. It's currently in use. \n"));
                }


            }


        }

        private static void SetUserRegisterCompleted(bool setUserActive, ValidationResult aValidationResult, int userId)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    AppSecurityUserEntity userEntity = new AppSecurityUserEntity();
                    userEntity.IsActive = setUserActive;
                    userEntity.IsRegisterCompleted = true;

                    adapter.UpdateEntitiesDirectly(userEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == userId));
                    adapter.Commit();
                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_AppTransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }
        }



        private static void SendAccountCreatedConfirmationMessage(AppSecurityUserExDto userDto)
        {
            AppMessageDto messageDto = new AppMessageDto();
            messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
            messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
            messageDto.Subject = "Your account has been created successfully";

            string url = string.Empty;
            if (string.IsNullOrWhiteSpace(userDto.PostEmailActivationRedirectUrl))
            {
                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

                string paramStr = userDto.Id.ToString();
                string paramStr_encrypted = AppSaasAccountUserBL.EncryptParamString(paramStr);

                url = applicationUrl + "/Home/RegisteredUserMgtLogin/?parameter=" + paramStr_encrypted;
            }
            else
            {
                url = userDto.PostEmailActivationRedirectUrl;
            }


            messageDto.Message = "<a target='_blank' href='" + url + "' style=''>" + "Your account has been created successfully. Click here to login." + "</a>";
            messageDto.ToList = userDto.Email + ";";
            //messageDto.IsForceUseGlobalSetting = true;
            AppMessageBL.SaveOneAppMessageDto(messageDto);
        }

        private static ValidationResult SendBusinessPaternerUserInvitationMessage(AppSecurityUserInvitationExDto userInvitationExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            //AppSecurityUserInvitationEntity invitationEntity = RetrieveOneAppSecurityUserInvitationEntity(invitationId);


            string invitingCompanyIdStr = userInvitationExDto.AppCreatedByCompanyId.ToString();
            string invitationIdStr = userInvitationExDto.Id.ToString();
            string sentDateTicksStr = DateTime.UtcNow.Ticks.ToString();
            string paramStr = EmAppGlobalServiceAction.AddBusinessParternerInvitedUserToCompany.ToString()
                + "|" + invitingCompanyIdStr
                + "|" + invitationIdStr
                + "|" + sentDateTicksStr;

            string paramStr_encrypted = EncryptParamString(paramStr);
            var companyEntity = AppCompanyBL.RetrieveOneAppCompanyEntity(userInvitationExDto.AppCreatedByCompanyId);


            AppMessageDto messageDto = new AppMessageDto();
            messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Global;
            messageDto.MessagePostType = (int)EmAppMessgaePostType.UserNotification;
            messageDto.Subject = "Account Activation";
            string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
            string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;

            //messageDto.Message = "You received an invitation from company " + companyEntity.Code + " " + companyEntity.FullName + "<br />"
            //    + "<a target='_blank' href='" + url + "' style=''>" + "To Join the Company As A " + ((EmAppUserType)(userInvitationExDto.EmUserType.Value)).ToString() + ": Click This Link" + "</a>";


            string companyName = companyEntity.Code;

            string messageBodyText = GetPartnerInvitationEmailTemplate();

            messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.RegistrationUserName.ToString() + "]", ((EmAppUserType)(userInvitationExDto.EmUserType.Value)).ToString());
            messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.UserRegisterActivationUrl.ToString() + "]", url);
            messageBodyText = messageBodyText.Replace("[" + EmAppMessagePlaceHolderToken.CurrentCompanyName.ToString() + "]", companyName);

            messageDto.Message = messageBodyText;



            messageDto.ToList = userInvitationExDto.InvitedUserEmail + ";";
            //messageDto.IsForceUseGlobalSetting = true;

            return AppMessageBL.SaveOneAppMessageDto(messageDto).ValidationResult;
        }


        private static AppSecurityUserEntity RetrieveOneAppSecurityUserEntity(object userId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                AppSecurityUserEntity userEntity = new AppSecurityUserEntity(int.Parse(userId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSecurityUserEntity);

                adpater.FetchEntity(userEntity, rootPath);
                return userEntity;
            }
        }


        //private static bool SaasUserRegister_PostProcess(AppSecurityUserExDto aNewSecurityUserExDto, int? compnayId)
        //{
        //    bool isPostProcessSuccess = false;

        //    if (compnayId.HasValue)
        //    {
        //        isPostProcessSuccess = AddRegisteredUserIntoCompanyByCompanyId(aNewSecurityUserExDto, compnayId);
        //    }
        //    else
        //    {
        //        isPostProcessSuccess = CreateNewCompanyForRegistedUser(aNewSecurityUserExDto);
        //    }


        //    return isPostProcessSuccess;

        //}




        private static bool AddRegisteredUserIntoCompanyByCompanyId(AppSecurityUserExDto aNewSecurityUserExDto, int? compnayId, int? orgnizationId = null)
        {
            string connString = "";
            string dbname = "";
            bool isSuccess = false;
            GetInvitingCompanyDBConnection(compnayId, ref connString, ref dbname);

            if (!string.IsNullOrWhiteSpace(connString) && !string.IsNullOrWhiteSpace(dbname))
            {
                using (var adpater = new DataAccessAdapter(connString))
                {
                    try
                    {
                        CopyMasterDBUserToUserCompanyDatabaseByAdapter(adpater, aNewSecurityUserExDto, compnayId, orgnizationId);

                        //  AppCacheManagerBL.RefreshAllCustomerDbRegAndFixtureCache();

                        isSuccess = true;


                    }
                    catch (Exception ex)
                    {
                        adpater.Rollback();
                    }

                }
            }

            return isSuccess;
        }

        internal static bool CopyMasterDBUserToUserCompanyDatabaseByAdapter(DataAccessAdapter userDbAdpater, AppSecurityUserExDto aNewSecurityUserExDto, int? compnayId, int? orgnizationId = null, int? businessPartnerId = null)
        {
            int? defaultOrganizationId = GetDefaultOrgniazationId(userDbAdpater);
            int? defaultLanguageId = GetDefaultLanguageId(userDbAdpater);



            try
            {
                string insertUserQuery = @"SET IDENTITY_INSERT [dbo].AppSecurityUser ON 

                        INSERT INTO[dbo].[AppSecurityUser]
                                   (UserID
		                           ,[LoginName]
                                   ,[UserName]
                                   ,[Email]
                                   ,[DomainID]         
                                   ,[Password]         
                                   ,[IsActive]
                                   ,[IsDeleted]
                                   ,IsRegisterCompleted
                                   ,GlobalGuid
                                   ,OrganizationID
                                    ,UserLanguage
                                ,CultureInfoCode
                                ,LanguageID
                                ,TimeZoneInfoToken
                                ,MyOwnCompnanyID
                                ,MappingExternalEmployeeAccountID                             

                                  )
                             VALUES
                                   (@userId, @loginName, @userName, @email, @domainId, @password, @isActive, @isDeleted, @IsRegisterCompleted, @GlobalGuid, @OrganizationID, @UserLanguage, @CultureInfoCode, @LanguageID, @TimeZoneInfoToken, @MyOwnCompnanyID, @MappingExternalEmployeeAccountID)


                        SET IDENTITY_INSERT[dbo].AppSecurityUser OFF";

                List<System.Data.SqlClient.SqlParameter> insertUserParameters = new List<System.Data.SqlClient.SqlParameter>();
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@userId", (int)aNewSecurityUserExDto.Id));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@loginName", aNewSecurityUserExDto.LoginName));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@userName", aNewSecurityUserExDto.UserName));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@email", aNewSecurityUserExDto.Email));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@domainId", aNewSecurityUserExDto.DomainId));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@password", aNewSecurityUserExDto.Password));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@isActive", true));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@isDeleted", false));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@IsRegisterCompleted", true));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@GlobalGuid", aNewSecurityUserExDto.GlobalGuid));

                if (orgnizationId.HasValue)
                {
                    insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@OrganizationID", orgnizationId.Value));
                }
                else
                {
                    insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@OrganizationID", defaultOrganizationId));
                }


                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@UserLanguage", defaultLanguageId));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@CultureInfoCode", "en-CA"));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@LanguageID", defaultLanguageId));
                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@TimeZoneInfoToken", "Eastern Standard Time"));


                if (compnayId.HasValue)
                {
                    insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@MyOwnCompnanyID", compnayId));
                }
                else
                {
                    insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@MyOwnCompnanyID", DBNull.Value));
                }

                insertUserParameters.Add(new System.Data.SqlClient.SqlParameter("@MappingExternalEmployeeAccountID", aNewSecurityUserExDto.MappingExternalEmployeeAccountId));







                userDbAdpater.ExecuteExecuteNonQuery(insertUserQuery, insertUserParameters);

                //AddUserDefaultRolesOnDatabaseByAdapter(aNewSecurityUserExDto, userDbAdpater, businessPartnerId);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //private static void AddUserDefaultRolesOnDatabaseByAdapter(AppSecurityUserExDto aNewSecurityUserExDto, DataAccessAdapter adpater, int? businessPartnerId)
        //{
        //    int userId = (int)aNewSecurityUserExDto.Id;
        //    AppSecurityUserEntity userEntity = new AppSecurityUserEntity(userId);
        //    adpater.FetchEntity(userEntity);

        //    userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userId, IsDefault = true, GroupId = (int)EmAppBuiltInUserGroup.Default });

        //    if (userEntity.DomainId == (int)EmAppUserType.Employee)
        //    {
        //        userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userEntity.UserId, IsDefault = true, GroupId = (int)EmAppBuiltInUserGroup.Employee });
        //        AddEmployeeUserToOrgnizationBuiltInGroups(adpater, userEntity);
        //    }
        //    else if (userEntity.DomainId == (int)EmAppUserType.Customer)
        //    {
        //        userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userId, IsDefault = true, GroupId = (int)EmAppBuiltInUserGroup.Customer });
        //        AddBusinessPartnerUserToPartnerBuiltInGroups(adpater, userEntity, businessPartnerId);
        //    }
        //    else if (userEntity.DomainId == (int)EmAppUserType.Supplier)
        //    {
        //        userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userId, IsDefault = true, GroupId = (int)EmAppBuiltInUserGroup.Supplier });
        //        AddBusinessPartnerUserToPartnerBuiltInGroups(adpater, userEntity, businessPartnerId);
        //    }
        //    else if (userEntity.DomainId == (int)EmAppUserType.ClientAgent)
        //    {
        //        userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userId, IsDefault = true, GroupId = (int)EmAppBuiltInUserGroup.Agent });
        //    }


        //    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
        //    adpater.SaveEntity(userEntity);
        //    adpater.Commit();
        //}

        //private static void AddEmployeeUserToOrgnizationBuiltInGroups(DataAccessAdapter adpater, AppSecurityUserEntity userEntity)
        //{


        //    if (userEntity.OrganizationId.HasValue)
        //    {
        //        int organizationId = userEntity.OrganizationId.Value;
        //        AppComOrganizationEntity orgnizationEntity = new AppComOrganizationEntity(organizationId);
        //        adpater.FetchEntity(orgnizationEntity);

        //        if (orgnizationEntity.ClassificationLevel > 1)
        //        {
        //            EntityCollection<AppSecurityGroupEntity> builtInGroupEntityList = new EntityCollection<AppSecurityGroupEntity>();
        //            IRelationPredicateBucket groupFilter = new RelationPredicateBucket(AppSecurityGroupFields.OrganizationId == organizationId & AppSecurityGroupFields.IsBuiltIn == true);
        //            adpater.FetchEntityCollection(builtInGroupEntityList, groupFilter);

        //            if (builtInGroupEntityList.Count > 0)
        //            {
        //                var orgniazationBuiltInGorupEntity = builtInGroupEntityList.First();
        //                userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userEntity.UserId, IsDefault = true, GroupId = orgniazationBuiltInGorupEntity.GroupId });
        //            }

        //        }
        //    }
        //}

        //private static void AddBusinessPartnerUserToPartnerBuiltInGroups(DataAccessAdapter adpater, AppSecurityUserEntity userEntity, int? businessPartnerId)
        //{
        //    if (businessPartnerId.HasValue)
        //    {
        //        EntityCollection<AppSecurityGroupEntity> builtInGroupEntityList = new EntityCollection<AppSecurityGroupEntity>();
        //        IRelationPredicateBucket groupFilter = new RelationPredicateBucket(AppSecurityGroupFields.BusinessPartnerId == businessPartnerId.Value & AppSecurityGroupFields.IsBuiltIn == true);
        //        adpater.FetchEntityCollection(builtInGroupEntityList, groupFilter);

        //        if (builtInGroupEntityList.Count > 0)
        //        {
        //            foreach (var groupEntity in builtInGroupEntityList)
        //            {
        //                userEntity.AppSecurityGroupMember.Add(new AppSecurityGroupMemberEntity() { UserId = userEntity.UserId, IsDefault = true, GroupId = groupEntity.GroupId });
        //            }

        //        }

        //    }
        //}


        private static bool InitializeNewCompanyData(AppSecurityUserExDto aNewSecurityUserExDto, DataAccessAdapter adapter, AppDataSourceRegisterEntity dataSourceEntity)
        {
            bool isSuccess = false;

            AppCompanyEntity masterDbCompnayEntity = CreateNewCompanyData_PrepareMasterDBData(aNewSecurityUserExDto, adapter, dataSourceEntity);

            if (masterDbCompnayEntity != null)
            {
                //isSuccess = InitializeNewCompanyData_PrepareUserDBData(aNewSecurityUserExDto, masterDbCompnayEntity, dataSourceEntity);


                isSuccess = InsertNewCompanyAndUserToUserDB(aNewSecurityUserExDto, masterDbCompnayEntity, dataSourceEntity);

                // need to root fodler new createion compnay 
                int compnayId = masterDbCompnayEntity.AppCompanyId;
                AppCompanyBL.CreateMyCompanyFolder(compnayId);

                aNewSecurityUserExDto.MyOwnCompnanyId = compnayId;
                // *** to do CreateCompanyAnoymousTokenToUserDB
                //CreateCompanyAnoymousTokenToMasterDB(adapter, compnayId);

            }

            return isSuccess;
        }

        private static void CreateCompanyAnoymousTokenToMasterDB(DataAccessAdapter masterDBadapter, int compnayId)
        {
            // 1. Create Anoyms User ---  only need one anoymous user User
            // 2. Create Anoyms Role --- NO need
            // 3. Create Anoyms Token (Session) (one need to create compnay anoymouse user in Master DB)
            AppSecurityUserSessionEntity anoymousSessiontoken = new AppSecurityUserSessionEntity();
            anoymousSessiontoken.EmExternalSigninType = (int)EmAppExternalLoginType.Anonymous;
            // AnoymousUser Id alwasy =2
            anoymousSessiontoken.UserId = 2;
            anoymousSessiontoken.AppCreatedByCompanyId = compnayId;
            anoymousSessiontoken.ExternalAcessToken = System.Guid.NewGuid().ToString();
            anoymousSessiontoken.SessionId = anoymousSessiontoken.ExternalAcessToken;

            masterDBadapter.SaveEntity(anoymousSessiontoken);
        }



        //private static bool CreateCompanyAnonymousUser(DataAccessAdapter adapter, AppDataSourceRegisterEntity dataSourceEntity)
        //{

        //}

        private static AppCompanyEntity CreateNewCompanyData_PrepareMasterDBData(AppSecurityUserExDto aNewSecurityUserExDto, DataAccessAdapter adapter, AppDataSourceRegisterEntity dataSourceEntity)
        {

            // Create a new Compnay
            AppCompanyEntity masterDbCompnayEntity = new AppCompanyEntity();
            masterDbCompnayEntity.Code = aNewSecurityUserExDto.ApplicationCode;
            masterDbCompnayEntity.ShortName = aNewSecurityUserExDto.ApplicationCode;
            masterDbCompnayEntity.FullName = aNewSecurityUserExDto.ApplicationCode;

            masterDbCompnayEntity.GlobalGuid = new Guid();
            masterDbCompnayEntity.CompanyDomainIdentityToken = aNewSecurityUserExDto.ApplicationCode;
            // need to fitler publis gmail
            // use RegistrationNumber numver to retrive the persona accpount
            masterDbCompnayEntity.RegistrationNumber = aNewSecurityUserExDto.Email;
            adapter.SaveEntity(masterDbCompnayEntity);



            //  userDbCompnayEntity.AppCompanyId = masterDbCompnayEntity.AppCompanyId;

            // need to used to save to user db
            // masterDbCompnayEntity.IsNew = true;

            masterDbCompnayEntity = AppCompanyBL.RetrieveOneAppCompanyEntityFromMasterDB(masterDbCompnayEntity.AppCompanyId);

            int companyId = masterDbCompnayEntity.AppCompanyId;


            // Must set  CompnayOwnerId for a register User
            AppSecurityUserEntity updateAppSecurityUserEntity = new AppSecurityUserEntity();
            updateAppSecurityUserEntity.MyOwnCompnanyId = companyId;

            // update userEntity compnayId
            adapter.UpdateEntitiesDirectly(updateAppSecurityUserEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == aNewSecurityUserExDto.Id));

            dataSourceEntity.DataSourceOwnerCompanyId = companyId;
            adapter.UpdateEntitiesDirectly(dataSourceEntity, new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceId == dataSourceEntity.DataSourceId));

            return masterDbCompnayEntity;

        }

        private static bool InsertNewCompanyAndUserToUserDB(AppSecurityUserExDto aNewSecurityUserExDto, AppCompanyEntity masterDbCompnayEntity, AppDataSourceRegisterEntity appDataSourceRegisterEntity)
        {
            bool isSuccess = false;



            if (appDataSourceRegisterEntity != null && masterDbCompnayEntity != null)
            {

                string userDbName = appDataSourceRegisterEntity.DatabaseName;
                string applicationCode = aNewSecurityUserExDto.ApplicationCode;

                if (!string.IsNullOrWhiteSpace(userDbName))
                {
                    string companyId = masterDbCompnayEntity.AppCompanyId.ToString();
                    string userId = aNewSecurityUserExDto.Id.ToString();
                    string dataSourceId = appDataSourceRegisterEntity.DataSourceId.ToString();
                    string query = "";

                    query += string.Format(@"  update AppSecurityUser set IsRegisterCompleted = 1 WHERE UserID = {0}"
                        , userId
                        );

                    query += string.Format(@"SET IDENTITY_INSERT [{0}].[dbo].[AppCompany] ON 

                                         INSERT INTO [{1}].[dbo].[AppCompany]
                                                    (AppCompanyID
		                                            ,[Code]
                                                    ,RegistrationNumber
                                                    ,[GlobalGuid]
                                                    ,CompanyDomainIdentityToken)
                                        SELECT AppCompanyID
		                                            ,[Code]
                                                    ,RegistrationNumber
                                                    ,[GlobalGuid]
                                                    ,CompanyDomainIdentityToken
                                        FROM [AppCompany]
                                        WHERE AppCompanyID = {2}

                                        SET IDENTITY_INSERT [{3}].[dbo].[AppCompany] OFF




                                        SET IDENTITY_INSERT [{4}].[dbo].AppSecurityUser ON 

                                         INSERT INTO [{5}].[dbo].[AppSecurityUser]
                                                (UserID
		                                        ,[LoginName]
                                                ,[UserName]
                                                ,[Email]
                                                ,[DomainID]         
                                                ,[Password]         
                                                ,[IsActive]
                                                ,[IsDeleted]
                                                ,IsRegisterCompleted
                                                ,GlobalGuid
                                                ,OrganizationID
                                                ,UserLanguage
                                            ,CultureInfoCode
                                            ,LanguageID
                                            ,TimeZoneInfoToken
                                            ,MyOwnCompnanyID
                                            ,MappingExternalEmployeeAccountID)
                                        SELECT UserID
		                                        ,[LoginName]
                                                ,[UserName]
                                                ,[Email]
                                                ,[DomainID]         
                                                ,[Password]         
                                                ,[IsActive]
                                                ,[IsDeleted]
                                                ,IsRegisterCompleted
                                                ,GlobalGuid
                                                ,OrganizationID
                                                ,UserLanguage
                                            ,CultureInfoCode
                                            ,LanguageID
                                            ,TimeZoneInfoToken
                                            ,MyOwnCompnanyID
                                            ,MappingExternalEmployeeAccountID
                                        FROM [AppSecurityUser]
                                        WHERE UserID = {6}

                                        SET IDENTITY_INSERT [{7}].[dbo].AppSecurityUser OFF



                            ",
                               userDbName, userDbName, companyId, userDbName, userDbName, userDbName, userId, userDbName
                               );


                    query += string.Format(@"

                                SET IDENTITY_INSERT [{0}].[dbo].[AppDataSourceRegister] ON 

                                INSERT INTO  [{1}].[dbo].[AppDataSourceRegister]
                                           ([DataSourceID]
		                                   ,[DataSourceName]
                                           ,[Description]
                                           ,[DataSourceType]
                                           ,[ConnectionString]
                                           ,[AppCreatedByID]
                                           ,[AppCreatedDate]
                                           ,[AppModifiedDate]
                                           ,[AppModifiedByID]
                                           ,[DataSourceOwnerCompanyID]
                                           ,[DatabaseName]
                                           ,[IsCompanyMasterDB]
                                           ,[AppCreatedByCompanyID])
    
                                SELECT [DataSourceID]
		                                ,[DataSourceName]
                                        ,[Description]
                                        ,[DataSourceType]
                                        ,[ConnectionString]
                                        ,[AppCreatedByID]
                                        ,[AppCreatedDate]
                                        ,[AppModifiedDate]
                                        ,[AppModifiedByID]
                                        ,[DataSourceOwnerCompanyID]
                                        ,[DatabaseName]
                                        ,[IsCompanyMasterDB]
                                        ,[AppCreatedByCompanyID]
                                FROM [AppDataSourceRegister]
                                WHERE [DataSourceID] = {2}

                                SET IDENTITY_INSERT [{3}].[dbo].[AppDataSourceRegister] OFF
                            "
                        , userDbName, userDbName, dataSourceId, userDbName
                        );

                    query += string.Format(@"  update [{0}].[dbo].AppReport set DataSourceID = {1}
"
                        , userDbName, dataSourceId
                        );

                    query += string.Format(@"  update [{0}].[dbo].AppDataSet set DataSourceFrom = {1}
"
                        , userDbName, dataSourceId
                        );

                    query += string.Format(@"  update [{0}].[dbo].AppEntityInfo set DataSourceFrom = {1}
"
                        , userDbName, dataSourceId
                        );

                    query += string.Format(@"  update [{0}].[dbo].AppTransaction set DataSourceFrom = {1}
"
                        , userDbName, dataSourceId
                        );

                    query += string.Format(@"  update [{0}].[dbo].AppIntergrationSetting set DataSourceRegisterID = {1}
"
                        , userDbName, dataSourceId
                        );

                    query += string.Format(@"  update [{0}].[dbo].AppIntergrationSettingParameter set DataSourceID = {1}
"
                        , userDbName, dataSourceId
                        );

                    query += string.Format(@"  update [{0}].[dbo].[AppSecurityUserSession] set AppCreatedByCompanyID = {1}
"
                        , userDbName, companyId
                        );


                    query += string.Format(@"  
                            DECLARE @companyId int = (select top 1 AppCompanyID from [{0}].[dbo].AppCompany)
                            update [{1}].[dbo].[AppComOrganization] set AppCompanyID = @companyId
                            update [{2}].[dbo].AppSecurityUser set AppCreatedByCompanyID = @companyId
                            update [{3}].[dbo].AppComOrgLevel set AppCreatedByCompanyID = @companyId, AppCompanyID = @companyId
                            update [{4}].[dbo].[AppEsite] set AppCreatedByCompanyID = @companyId
                            "
                        , userDbName, userDbName, userDbName, userDbName, userDbName
                        );





                    string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

                    if (!string.IsNullOrWhiteSpace(applicationUrl) && !string.IsNullOrWhiteSpace(applicationCode))
                    {
                        if (!applicationUrl.EndsWith("/"))
                        {
                            applicationUrl += "/";
                        }

                        string currentAppDomain = ServerContext.Instance.ApplicationDomain;

                        if (!string.IsNullOrWhiteSpace(currentAppDomain) && applicationUrl.ToLower().EndsWith(currentAppDomain.ToLower() + "/"))
                        {
                            string appSiteBaseUrl = applicationUrl.ToLower().Replace(currentAppDomain.ToLower(), applicationCode);

                            query += string.Format(@"                                  
                                update [{0}].[dbo].[AppEsite] set SitePublishedBaseUrl = '{1}'
                               
                                "
                            , userDbName, appSiteBaseUrl
                            );


                            string appsetup_ApplicationURL = EnDeCrypt.Encrypt(appSiteBaseUrl + "mgt/", AppSetupConverter.AdSetupValueSaltKey);
                            string appsetup_InternalApiRestEndPoint = EnDeCrypt.Encrypt(appSiteBaseUrl + "mgt/PluginApi/", AppSetupConverter.AdSetupValueSaltKey);
                            string appsetup_SystemAgentUser = EnDeCrypt.Encrypt(userId, AppSetupConverter.AdSetupValueSaltKey);

                            query += string.Format(@"                                  
                                update [{0}].[dbo].[AppSetup] set SetupValue = '{1}' where setupCode = '{2}';                              
                                    "
                                   , userDbName, appsetup_ApplicationURL, "ApplicationURL"
                               );

                            query += string.Format(@"                                  
                                update [{0}].[dbo].[AppSetup] set SetupValue = '{1}' where setupCode = '{2}';                            
                                    "
                                   , userDbName, appsetup_InternalApiRestEndPoint, "InternalApiRestEndPoint"
                               );

                            query += string.Format(@"                                  
                                update [{0}].[dbo].[AppSetup] set SetupValue = '{1}' where setupCode = '{2}';                            
                                    "
                                   , userDbName, appsetup_SystemAgentUser, "SystemAgentUser"
                               );

                        }
                    }



                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(AppCacheManagerBL.HostCompayDataBaseID, null);

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

                    if (string.IsNullOrWhiteSpace(errorMsg))
                    {

                        //string udConnectionString = appDataSourceRegisterEntity.ConnectionString;

                        var generateESiteResult = AppEsiteConfigBL.GenerateClientApplicatoinDefaultWebSiteFiles(companyId, aNewSecurityUserExDto.ApplicationCode);

                        if (generateESiteResult.IsSuccessful)
                        {
                            isSuccess = true;
                        }

                        isSuccess = true;
                    }
                }
            }

            return isSuccess;
        }


        private static bool InitializeNewCompanyData_PrepareUserDBData(AppSecurityUserExDto aNewSecurityUserExDto, AppCompanyEntity masterDbCompnayEntity, AppDataSourceRegisterEntity appDataSourceRegisterEntity)
        {
            bool isSuccess = false;

            //  masterDbCompnayEntity.IsNew = true;

            if (appDataSourceRegisterEntity != null && masterDbCompnayEntity != null)
            {
                string connString = appDataSourceRegisterEntity.ConnectionString;
                string dbname = appDataSourceRegisterEntity.DatabaseName;

                if (!string.IsNullOrWhiteSpace(connString) && !string.IsNullOrWhiteSpace(dbname))
                {
                    using (var adpater = new DataAccessAdapter(connString))
                    {
                        try
                        {

                            InsertCompanyIntoUserDB(masterDbCompnayEntity, adpater);


                            CopyMasterDBUserToUserCompanyDatabaseByAdapter(adpater, aNewSecurityUserExDto, masterDbCompnayEntity.AppCompanyId);

                            isSuccess = true;
                        }
                        catch (Exception ex)
                        {
                            adpater.Rollback();
                        }
                    }
                }
            }

            return isSuccess;
        }




        private static AppBusinessPartnerInviteUserEntity RetrieveOneAppBusinessPartnerInviteUserEntity(object id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity = new AppBusinessPartnerInviteUserEntity(int.Parse(id.ToString()));

                adapter.FetchEntity(aAppBusinessPartnerInviteUserEntity);
                return aAppBusinessPartnerInviteUserEntity;
            }
        }

        internal static List<AppBusinessPartnerInviteUserEntity> RetrieveAppBusinessPartnerInviteUserEntityListByUserId(object userId)
        {
            EntityCollection<AppBusinessPartnerInviteUserEntity> entityList = new EntityCollection<AppBusinessPartnerInviteUserEntity>();

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppBusinessPartnerInviteUserFields.UserId == userId);
                adpater.FetchEntityCollection(entityList, filter);
            }

            return entityList.ToList();
        }






        private static ValidationResult SaveOneAppBusinessPartnerInviteUserEntityExDto_ProcessNewDto(AppBusinessPartnerInviteUserExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppBusinessPartnerInviteUserEntity aParentAppBusinessPartnerInviteUserEntity = new AppBusinessPartnerInviteUserEntity();
            AppBusinessPartnerInviteUserConverter.CopyDtoToEntity(aParentAppBusinessPartnerInviteUserEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppBusinessPartnerInviteUserEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerInviteUserExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppBusinessPartnerInviteUserEntity.ParternerInvitedUserId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerInviteUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult SaveOneAppBusinessPartnerInviteUserEntityExDto_ProcessDirtyDto(AppBusinessPartnerInviteUserExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity = RetrieveOneAppBusinessPartnerInviteUserEntity(aDto.Id);
            AppBusinessPartnerInviteUserConverter.CopyDtoToEntity(aAppBusinessPartnerInviteUserEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppBusinessPartnerInviteUserEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerInviteUserExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerInviteUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        private static void CreateStandAloneWebApplicationAndAppPoolForNewSaasUser(AppSecurityUserExDto aNewSecurityUserExDto)
        {

            try
            {
#if NETFRAMEWORK
                Runspace runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                Pipeline pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript("c:\\PowerShell\\CreateAppPool.ps1 " + aNewSecurityUserExDto.LoginName);
                var results = pipeline.Invoke();
                runspace.Close();
#endif
            }
            catch (Exception ex)
            {

            }


        }

        private static void GetInvitingCompanyDBConnection(int? compnayId, ref string connString, ref string dbname)
        {
            AppDataSourceRegisterEntity aAppDataSourceRegisterEntity = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(compnayId.Value);

            connString = aAppDataSourceRegisterEntity.ConnectionString;
            dbname = aAppDataSourceRegisterEntity.DatabaseName;

        }





        private static void InsertCompanyIntoUserDB(AppCompanyEntity userDbCompnayEntity, DataAccessAdapter adpater)
        {


            //adpater.SaveEntity(userDbCompnayEntity);

            string insertCompanyQuery = @"SET IDENTITY_INSERT [dbo].[AppCompany] ON 

                            INSERT INTO [dbo].[AppCompany]
                                       (AppCompanyID
		                               ,[Code]
                                       ,RegistrationNumber
                                       ,[GlobalGuid]
                                       ,CompanyDomainIdentityToken)
                                 VALUES
                                       (@AppCompanyID, @Code, @RegistrationNumber, @GlobalGuid, @CompanyDomainIdentityToken)                            

                            SET IDENTITY_INSERT [dbo].[AppCompany] OFF ";

            List<System.Data.SqlClient.SqlParameter> insertCompanyParameters = new List<System.Data.SqlClient.SqlParameter>();
            insertCompanyParameters.Add(new System.Data.SqlClient.SqlParameter("@AppCompanyID", userDbCompnayEntity.AppCompanyId));
            insertCompanyParameters.Add(new System.Data.SqlClient.SqlParameter("@Code", userDbCompnayEntity.Code));
            insertCompanyParameters.Add(new System.Data.SqlClient.SqlParameter("@RegistrationNumber", userDbCompnayEntity.RegistrationNumber));
            insertCompanyParameters.Add(new System.Data.SqlClient.SqlParameter("@GlobalGuid", userDbCompnayEntity.GlobalGuid));
            insertCompanyParameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyDomainIdentityToken", userDbCompnayEntity.CompanyDomainIdentityToken));

            adpater.ExecuteExecuteNonQuery(insertCompanyQuery, insertCompanyParameters);



            int userDbCompanyId = userDbCompnayEntity.AppCompanyId;

            AppComOrganizationEntity organizationEntity = new AppComOrganizationEntity();
            organizationEntity.AppCompanyId = userDbCompanyId;
            organizationEntity.AppCreatedByCompanyId = userDbCompanyId;
            adpater.UpdateEntitiesDirectly(organizationEntity, new RelationPredicateBucket(AppComOrganizationFields.AppCompanyId == System.DBNull.Value));

            AppComOrgLevelEntity orgLevelEntity = new AppComOrgLevelEntity();
            orgLevelEntity.AppCompanyId = userDbCompanyId;
            orgLevelEntity.AppCreatedByCompanyId = userDbCompanyId;
            adpater.UpdateEntitiesDirectly(orgLevelEntity, new RelationPredicateBucket(AppComOrgLevelFields.AppCompanyId == System.DBNull.Value));



        }

        private static AppCompanyEntity SetupUserCompanyOnMasterDB(AppSecurityUserExDto aNewSecurityUserExDto, AppCompanyEntity userDbCompnayEntity, ref string connString, ref string dbname)
        {
            AppCompanyEntity masterDbCompnayEntity = null;
            EntityCollection<AppCompanyEntity> appCompanyList = new EntityCollection<AppCompanyEntity>();
            int? companyId = null;



            using (var adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppCompanyFields.RegistrationNumber == aNewSecurityUserExDto.Email);

                    adapter.FetchEntityCollection(appCompanyList, filter);

                    if (appCompanyList.Count > 0)
                    {
                        masterDbCompnayEntity = appCompanyList[0];

                    }
                    else  // compnay doest't exist !!
                    {

                        masterDbCompnayEntity = new AppCompanyEntity();

                        userDbCompnayEntity.Code = masterDbCompnayEntity.Code = aNewSecurityUserExDto.LoginName;
                        userDbCompnayEntity.GlobalGuid = masterDbCompnayEntity.GlobalGuid = new Guid();
                        // need to fitler publis gmail
                        // use RegistrationNumber numver to retrive the persona accpount
                        userDbCompnayEntity.RegistrationNumber = masterDbCompnayEntity.RegistrationNumber = aNewSecurityUserExDto.Email;
                        adapter.SaveEntity(masterDbCompnayEntity);

                        userDbCompnayEntity.AppCompanyId = masterDbCompnayEntity.AppCompanyId;


                        // need to used to save to user db
                        masterDbCompnayEntity.IsNew = true;
                        companyId = masterDbCompnayEntity.AppCompanyId;


                        AppSecurityUserEntity appSecurityUserEntity = new AppSecurityUserEntity();
                        appSecurityUserEntity.MyOwnCompnanyId = companyId;
                        // update userEntity compnayId
                        adapter.UpdateEntitiesDirectly(appSecurityUserEntity, new RelationPredicateBucket(AppSecurityUserFields.UserId == aNewSecurityUserExDto.Id));


                        // Update AppDateSourceREgister
                        EntityCollection<AppDataSourceRegisterEntity> dataSourceList = new EntityCollection<AppDataSourceRegisterEntity>();

                        IRelationPredicateBucket dataSourceFilter = new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceOwnerCompanyId == DBNull.Value);

                        adapter.FetchEntityCollection(dataSourceList, dataSourceFilter);

                        if (dataSourceList.Count > 0)
                        {
                            AppDataSourceRegisterEntity appDataSourceRegisterEntity = dataSourceList[0];
                            appDataSourceRegisterEntity.DataSourceOwnerCompanyId = companyId;
                            adapter.UpdateEntitiesDirectly(appDataSourceRegisterEntity, new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceId == appDataSourceRegisterEntity.DataSourceId));

                            connString = appDataSourceRegisterEntity.ConnectionString;
                            dbname = appDataSourceRegisterEntity.DatabaseName;
                        }
                        adapter.Commit();
                    }
                }
                catch (Exception ex)
                {
                    adapter.Rollback();
                }
            }

            return masterDbCompnayEntity;
        }




        //private static void CreateAppSecurityUserFromSaasUser(AppSecurityUserExDto aAppSecurityUserExDto, ValidationResult aValidationResult, string newPassword)
        //      {
        //          AppSecurityUserExDto appSecurityUserExDto = new AppSecurityUserExDto();
        //          appSecurityUserExDto.UserName = aAppSecurityUserExDto.UserName;
        //          appSecurityUserExDto.LoginName = aAppSecurityUserExDto.LoginName;
        //          appSecurityUserExDto.Password = aAppSecurityUserExDto.Password;
        //          appSecurityUserExDto.ConfirmPassword = appSecurityUserExDto.Password;
        //          appSecurityUserExDto.Email = aAppSecurityUserExDto.Email;

        //          appSecurityUserExDto.DomainId = (int)EmAppUserType.Employee;

        //          appSecurityUserExDto.IsActive = true;
        //          aValidationResult.Merge(AppSecurityUserBL.SaveAppSecurityUserExDto(appSecurityUserExDto, newPassword).ValidationResult);
        //      }





        public static AppSecurityUserEntity RetrieveOneAppSecurityUserEntityByGuid(Guid userGuid)
        {
            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                adapter.FetchEntityCollection(users, new RelationPredicateBucket(new PredicateExpression(AppSecurityUserFields.GlobalGuid == userGuid)));
            }

            return users.FirstOrDefault();
        }

        private static AppSecurityUserEntity RetrieveOneAppSecurityUserEntityByEmail(string email)
        {
            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                adapter.FetchEntityCollection(users, new RelationPredicateBucket(new PredicateExpression(AppSecurityUserFields.Email == email)));
            }

            return users.FirstOrDefault();
        }

        private static List<AppSecurityUserEntity> RetrieveAppSecurityUserEntityListByEmail(string email, string loginName = "")
        {
            EntityCollection<AppSecurityUserEntity> users = new EntityCollection<AppSecurityUserEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                if (string.IsNullOrWhiteSpace(loginName))
                {
                    adapter.FetchEntityCollection(users, new RelationPredicateBucket(new PredicateExpression(AppSecurityUserFields.Email == email)));
                }
                else
                {
                    adapter.FetchEntityCollection(users, new RelationPredicateBucket(new PredicateExpression(AppSecurityUserFields.Email == email & AppSecurityUserFields.LoginName == loginName.Trim())));
                }
            }

            return users.ToList();
        }


        private static ValidationResult ValidateUserExDto(AppSecurityUserExDto appSecurityUserExDto)
        {
            var aValidationResult = appSecurityUserExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                return aValidationResult;
            }

            int? userId = ControlTypeValueConverter.ConvertValueToInt(appSecurityUserExDto.Id);

            aValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(appSecurityUserExDto.LoginName, userId));
            if (aValidationResult.HasErrors)
            {
                return aValidationResult;
            }

            aValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(appSecurityUserExDto.Email, userId));
            if (aValidationResult.HasErrors)
            {
                return aValidationResult;
            }

            return new ValidationResult();
        }

        private static ValidationResult ValidateNewSaasAccountUserExDto(AppSecurityUserExDto appSecurityUserExDto)
        {
            var aValidationResult = appSecurityUserExDto.ValidateDto();

            if (aValidationResult.HasErrors)
            {
                return aValidationResult;
            }

            //if (string.IsNullOrWhiteSpace(appSecurityUserExDto.ApplicationName))
            //{
            //    aValidationResult.AddItem(null, "app_applicatoinNameIsEmpty", ValidationItemType.Error, "Application Name Is Empty");
            //    return aValidationResult;
            //}

            //if (string.IsNullOrWhiteSpace(appSecurityUserExDto.ApplicationCode))
            //{
            //    aValidationResult.AddItem(null, "app_applicatoinCodeIsEmpty", ValidationItemType.Error, "Application Cose Is Empty");
            //    return aValidationResult;
            //}

            //if (string.IsNullOrWhiteSpace(appSecurityUserExDto.CompanyName))
            //{
            //    aValidationResult.AddItem(null, "app_CompanyNameIsEmpty", ValidationItemType.Error, "Company Name Is Empty");
            //    return aValidationResult;
            //}

            int? userId = ControlTypeValueConverter.ConvertValueToInt(appSecurityUserExDto.Id);

            aValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(appSecurityUserExDto.Email, userId));
            if (aValidationResult.HasErrors)
            {
                return aValidationResult;
            }

            aValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(appSecurityUserExDto.LoginName, userId));
            if (aValidationResult.HasErrors)
            {
                return aValidationResult;
            }

            string appCode = appSecurityUserExDto.ApplicationCode;
            int appCode_aliasCount = 2;

            while (IsApplicationCodeExist(appCode))
            {
                appCode = appSecurityUserExDto.ApplicationCode + "_" + appCode_aliasCount;
                appCode_aliasCount++;
            }

            appSecurityUserExDto.ApplicationCode = appCode;


            //if (isApplicationCodeExist)
            //{
            //    aValidationResult.AddItem(null, "app_applicatoinNameAlreadyExist", ValidationItemType.Error, "Application Name Already Exist");
            //    return aValidationResult;
            //}


            return new ValidationResult();
        }

        private static bool IsApplicationCodeExist(string applicationCode)
        {
            bool isExist = false;

            using (var masterDBadapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    EntityCollection<AppCompanyEntity> companyList = new EntityCollection<AppCompanyEntity>();
                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppCompanyFields.CompanyDomainIdentityToken == applicationCode);

                    // Need to refresh user Master DB !!

                    masterDBadapter.FetchEntityCollection(companyList, filter);

                    if (companyList.Count > 0)
                    {
                        isExist = true;
                    }
                    //  adapter.Commit();
                }
                catch (Exception ex)
                {

                }
            }

            return isExist;
        }

        private static bool CreateNewCompanyAndRestoreUserDBForRegistedUser(AppSecurityUserExDto aNewSecurityUserExDto, ValidationResult aValidationResult)
        {
            bool isSuccess = false;

            string baseUserDbBackupFilePath = AppSystemSettingBL.GetStringValue(EmSystemSettings.BaseUserDbBackupFilePath);
            string userDbFileDirectoryPath = AppSystemSettingBL.GetStringValue(EmSystemSettings.UserDbFileDirectoryPath);

            string applicationCode = aNewSecurityUserExDto.ApplicationCode;
            string newUserCompanyDbName = "APPUD_" + applicationCode;

            var masterDbDataSourceEntity = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(AppCacheManagerBL.HostCompayDataBaseID);

            string masterDbName = masterDbDataSourceEntity.DatabaseName;
            string connectionString = masterDbDataSourceEntity.ConnectionString.Replace(masterDbName, newUserCompanyDbName);


            string query = "";

            query += string.Format(@"  
                DECLARE @OOTBDBSourcePath  nvarchar(300) ='{0}'
                DECLARE @OOTBDBDestPath  nvarchar(300) ='{1}'  
                DECLARE @OOTBDBSourceLogicalMDF  nvarchar(300) ='APP_UserDB'
                DECLARE @OOTBDBSourceLogicalLog  nvarchar(300) ='APP_UserDB_log'                          
                DECLARE @destDBName varchar(200) = '{2}'
                DECLARE @destDBNameMdf varchar(200)
                DECLARE @destDBNameLog varchar(200)		

	            select @destDBNameMdf =@OOTBDBDestPath+@destDBName+'.mdf'
	            select @destDBNameLog =@OOTBDBDestPath + @destDBName+'_log.ldf'
                ",

                baseUserDbBackupFilePath,
                userDbFileDirectoryPath,
                newUserCompanyDbName
                );



            query += string.Format(@"  
                RESTORE DATABASE @destDBName FROM  DISK = @OOTBDBSourcePath
                WITH
                MOVE @OOTBDBSourceLogicalMDF TO @destDBNameMdf,
                MOVE @OOTBDBSourceLogicalLog  TO @destDBNameLog
                
	                
		                INSERT INTO [AppDataSourceRegister]
				                   ([DataSourceName]
				                   ,[Description]
				                   ,[DataSourceType]
				                   ,[ConnectionString]
				                   ,[AppCreatedByID]
				                   ,[AppCreatedDate]
				                   ,[AppModifiedDate]
				                   ,[AppModifiedByID]
				                   ,[AppCreatedByCompanyID]
				                   ,IsCompanyMasterDB
				                   ,[DatabaseName])
			                 VALUES
				                   ('{0}'
				                   ,''
				                   ,1
				                   ,N'{1}'
				                   ,null
				                   ,null
				                   ,null
				                   ,null
				                   ,null
				                   ,1
				                   ,'{2}')	
                    
                  "
               , newUserCompanyDbName, connectionString, newUserCompanyDbName
             );


            //DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(AppCacheManagerBL.HostCompayDataBaseID, null);

            //string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query, false);

            string errorMsg = "";

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    DataTable dataTableResult = adapter.ExecuteDataTableRetrievalQuery(query, new List<System.Data.SqlClient.SqlParameter>());
                }
                catch (Exception ex)
                {
                    errorMsg = ex.ToString();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UserRegister_CreateUserCompanyDatabaseFailed", ValidationItemType.Error, errorMsg));
                }
            }

            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                using (var adapter = new DataAccessAdapter(AppMasterDBConnectionString))
                {
                    try
                    {
                        //Find the Reserve  avialbe  DB for the new User
                        EntityCollection<AppDataSourceRegisterEntity> dataSourceList = new EntityCollection<AppDataSourceRegisterEntity>();
                        IRelationPredicateBucket dataSourceFilter = new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceOwnerCompanyId == DBNull.Value
                            & AppDataSourceRegisterFields.IsCompanyMasterDb == true & AppDataSourceRegisterFields.DatabaseName == newUserCompanyDbName);

                        // Need to refresh user Master DB !!

                        adapter.FetchEntityCollection(dataSourceList, dataSourceFilter);

                        if (dataSourceList.Count > 0)
                        {
                            AppDataSourceRegisterEntity dataSourceRegisterEntity = dataSourceList[0];

                            bool isPrepareNewCompanyDBSuccess = InitializeNewCompanyData(aNewSecurityUserExDto, adapter, dataSourceRegisterEntity);

                            if (isPrepareNewCompanyDBSuccess)
                            {
                                isSuccess = true;


                            }

                            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dataSourceRegisterEntity.DataSourceId);
                        }
                        else
                        {

                            isSuccess = false;
                        }

                        //  adapter.Commit();
                    }
                    catch (Exception ex)
                    {
                        adapter.Rollback();
                    }
                }
            }


            if (isSuccess)
            {
                // Test Create Stand Alone Application and AppPool
                bool isNeedToCreateStandAloneWebApp = false;

                if (isNeedToCreateStandAloneWebApp)
                {
                    CreateStandAloneWebApplicationAndAppPoolForNewSaasUser(aNewSecurityUserExDto);
                }


            }


            return isSuccess;
        }

        private static bool CreateNewCompanyForRegistedUser(AppSecurityUserExDto aNewSecurityUserExDto)
        {
            bool isSuccess = false;



            using (var masterDBadapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    //Find the Reserve  avialbe  DB for the new User
                    EntityCollection<AppDataSourceRegisterEntity> dataSourceList = new EntityCollection<AppDataSourceRegisterEntity>();
                    IRelationPredicateBucket dataSourceFilter = new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceOwnerCompanyId == DBNull.Value & AppDataSourceRegisterFields.IsCompanyMasterDb == true);

                    // Need to refresh user Master DB !!

                    masterDBadapter.FetchEntityCollection(dataSourceList, dataSourceFilter);

                    if (dataSourceList.Count > 0)
                    {
                        AppDataSourceRegisterEntity dataSourceRegisterEntity = dataSourceList[0];

                        bool isPrepareNewCompanyDBSuccess = InitializeNewCompanyData(aNewSecurityUserExDto, masterDBadapter, dataSourceRegisterEntity);

                        if (isPrepareNewCompanyDBSuccess)
                        {
                            isSuccess = true;


                        }

                        AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dataSourceRegisterEntity.DataSourceId);
                    }
                    else
                    {

                        isSuccess = false;
                    }

                    //  adapter.Commit();
                }
                catch (Exception ex)
                {
                    masterDBadapter.Rollback();
                }
            }

            if (isSuccess)
            {
                // Test Create Stand Alone Application and AppPool
                bool isNeedToCreateStandAloneWebApp = false;

                if (isNeedToCreateStandAloneWebApp)
                {
                    CreateStandAloneWebApplicationAndAppPoolForNewSaasUser(aNewSecurityUserExDto);
                }


            }


            return isSuccess;
        }


        private static AppSecurityUserEntity RetrieveInvitedBusinessPartnerUserEntityByEmailAddrees(AppSecurityUserInvitationEntity invitationEntity)
        {
            if (invitationEntity != null && invitationEntity.EmUserType.HasValue && !string.IsNullOrWhiteSpace(invitationEntity.InvitedUserEmail))
            {
                var existUserEntity = RetrieveOneAppSecurityUserEntityByEmail(invitationEntity.InvitedUserEmail);

                if (existUserEntity != null)
                {
                    return existUserEntity;
                }
            }

            return null;
        }

        private static AppSecurityUserEntity RetrieveInvitedBusinessPartnerUserEntity(int? invitingCompanyId, int? invitationId)
        {
            AppSecurityUserInvitationEntity invitationEntity = RetrieveOneAppSecurityUserInvitationEntityByCompany(invitingCompanyId, invitationId);

            if (invitationEntity != null && invitationEntity.EmUserType.HasValue && !string.IsNullOrWhiteSpace(invitationEntity.InvitedUserEmail))
            {
                var existUserEntity = RetrieveOneAppSecurityUserEntityByEmail(invitationEntity.InvitedUserEmail);

                if (existUserEntity != null)
                {
                    return existUserEntity;
                }
            }

            return null;
        }



        private static string GetAccountActivationEmailTemplate()
        {
            string strVar = "";
            strVar += "<table style=\"border-collapse:collapse;width: 100%;background-color:#e9e9e9;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">";
            strVar += "    <tr>";
            strVar += "        <td>";
            strVar += "            <table style=\"border-collapse:collapse; width:640px; margin:0 auto;color:#1C1D31;mso-line-height: exactly;line-height: 24px;background-color:#ffffff;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;\" cellspacing=\"0\"";
            strVar += "                cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;background-color: #1C1D31;\">";
            strVar += "                        <table style=\"border-collapse:collapse; width:100%;height:88px;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"vertical-align: middle;text-align: center;width:640px;height:88px;\">";
            strVar += "                                    <span style=\"color: #BCBEC8;font-weight: bold;font-size: 16px;\">";
            strVar += "                                        ACCOUNT ACTIVATION";
            strVar += "                                    </span>";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                        </table>";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "                <tr>";
            strVar += "                    <td style=\"padding-top:7px;\">";
            strVar += "                        <!-- <img alt=\"\" src=\"\" style=\"width: 640px;\" /> -->";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;padding:30px 70px;\">";
            strVar += "";
            strVar += "                        <table style=\"border-collapse:collapse; width:500px;color:#1C1D31;mso-line-height: exactly;line-height: 24px;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;text-align: left;\" ";
            strVar += "                        cellspacing=\"0\" cellpadding=\"0\"";
            strVar += "                            border=\"0\" >";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;text-transform: capitalize;\">";
            strVar += "                                    Dear [RegistrationUserName],";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;text-transform: uppercase;font-weight: bold;\">";
            strVar += "                                    Welcome to App Builder! ";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;\">";
            strVar += "                                    We are thrilled to have you on board and thank you for choosing our services. We're excited to embark on this journey together and provide you with a top-notch experience.";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                            </tr>                     ";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;\">";
            strVar += "                                    <span>Please Activate Your Account With  <a href=\"[UserRegisterActivationUrl]\" target=\"_blank\" class=\"c3\" style=\"text-decoration: underline;color: #1C1D31;text-underline-offset: 2px;\">This Link</a>.</span>";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                            ";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;\">";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                        </table>";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;background-color: #eeeeee;padding:30px 0px 60px 0px;\">";
            strVar += "";
            strVar += "                        <table style=\"border-collapse:collapse; width:500px;height: 300px; \" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"vertical-align: middle;text-align: left;\">";
            strVar += "                                    <table style=\"border-collapse:collapse;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">";
            strVar += "                                        ";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 20px 0px;font-size: 16px;mso-line-height: exactly;line-height: 24px;\">";
            strVar += "                                                We are committed to providing exceptional customer support, so please don't hesitate to reach out to us if you have any questions, concerns, or need assistance. Stay tuned for updates and exciting news from our team. We regularly introduce new features and enhancements to enhance your experience and ensure your satisfaction. ";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 5px 0px;mso-line-height: exactly;line-height: 24px;\">";
            strVar += "                                                Thank you once again for choosing App Builder. We're looking forward to serving you and exceeding your expectations.";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 30px 0px 10px 0px;\">";
            strVar += "                                                ";
            strVar += "                                                Best regards,";
            strVar += "";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"\">";
            strVar += "                                                ";
            strVar += "                                                App Builder Team";
            strVar += "";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                    </table>";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                             ";
            strVar += "                            </tr>";
            strVar += "                        </table>";
            strVar += "";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "";
            strVar += "";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;padding:40px 70px 20px 80px;background-color: #1C1D31;text-align: center;\">";
            strVar += "";
            strVar += "                        <table style=\"border-collapse:collapse; width:480px;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"vertical-align: middle;text-align: left;\">";
            strVar += "                                    <table style=\"border-collapse:collapse;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 5px 0px;text-transform: uppercase;font-size: 15px;font-weight: bold;line-height: 26px;color:#BCBEC8;\">";
            strVar += "";
            strVar += "                                                APP Builder";
            strVar += "";
            strVar += "                                            </td>";
            strVar += "                                        </tr>                                        ";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 10px 0px 20px 0px;font-size: 16px;color:#BCBEC8;font-weight: lighter;mso-line-height: exactly;line-height: 24px;\">";
            strVar += "                                                A visual tool built on top of a powerful Angular / HTML5 platform that has been proven over the past 20 years in the most demanding web applications, across many industries. ";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                       ";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 5px 0px;\">";
            strVar += "";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                       ";
            strVar += "                                    </table>";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                             ";
            strVar += "                            </tr>";
            strVar += "                        </table>";
            strVar += "";
            strVar += "                    </td>";
            strVar += "                </tr>              ";
            strVar += "";
            strVar += "               ";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;text-align: center;vertical-align: middle;font-size: 14px;font-weight: lighter;color:#010101;height: 60px;\">";
            strVar += "                        App Builder LLC | Montreal ";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "";
            strVar += "            </table>";
            strVar += "        </td>";
            strVar += "    </tr>";
            strVar += "</table>";

            return strVar;
        }


        private static string GetPartnerInvitationEmailTemplate()
        {
            string strVar = "";
            strVar += "<table style=\"border-collapse:collapse;width: 100%;background-color:#e9e9e9;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">";
            strVar += "    <tr>";
            strVar += "        <td>";
            strVar += "            <table style=\"border-collapse:collapse; width:640px; margin:0 auto;color:#1C1D31;mso-line-height: exactly;line-height: 24px;background-color:#ffffff;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;\" cellspacing=\"0\"";
            strVar += "                cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;background-color: #1C1D31;\">";
            strVar += "                        <table style=\"border-collapse:collapse; width:100%;height:88px;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"vertical-align: middle;text-align: center;width:640px;height:88px;\">";
            strVar += "                                    <span style=\"color: #BCBEC8;font-weight: bold;font-size: 16px;\">";
            strVar += "                                        ACCOUNT ACTIVATION";
            strVar += "                                    </span>";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                        </table>";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "                <tr>";
            strVar += "                    <td style=\"padding-top:7px;\">";
            strVar += "                        <!-- <img alt=\"\" src=\"\" style=\"width: 640px;\" /> -->";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;padding:30px 70px 50px 70px;\">";
            strVar += "";
            strVar += "                        <table style=\"border-collapse:collapse; width:500px;color:#1C1D31;mso-line-height: exactly;line-height: 24px;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;text-align: left;\" ";
            strVar += "                        cellspacing=\"0\" cellpadding=\"0\"";
            strVar += "                            border=\"0\" >";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;text-transform: capitalize;\">";
            strVar += "                                    Dear [RegistrationUserName] User,";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;\">";
            strVar += "                                    We are happy to welcome you to our platform, where you can manage your inventory, orders, and payments with ease and efficiency. To start using our services, you need to activate your new account from the link below. Once you activate your account, you can log in to our website and access your dashboard, where you can customize your profile, settings, and preferences. You can also browse our tutorials and FAQs to learn more about how to use our platform and its features. We hope you enjoy your experience with Suppler and we look forward to working with you.";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 12px 0px;\">";
            strVar += "                                    <span>Please activate your account with  <a href=\"[UserRegisterActivationUrl]\" target=\"_blank\" class=\"c3\" style=\"text-decoration: underline;color: #1C1D31;text-underline-offset: 2px;\">this link</a>.</span>";
            strVar += "                                </td>";
            strVar += "                            </tr>";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"padding: 30px 0px 10px 0px;\">";
            strVar += "                                                ";
            strVar += "                                                Sincerely,";
            strVar += "";
            strVar += "                                 </td>";
            strVar += "                             </tr>";
            strVar += "                             <tr>";
            strVar += "                                            <td style=\"\">";
            strVar += "                                                ";
            strVar += "                                               The [CurrentCompanyName] Team";
            strVar += "";
            strVar += "                                 </td>";
            strVar += "                             </tr>";
            strVar += "                            ";
            strVar += "                        </table>";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "";
            strVar += "";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;padding:40px 70px 20px 70px;background-color: #1C1D31;text-align: center;\">";
            strVar += "";
            strVar += "                        <table style=\"border-collapse:collapse; width:480px;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" align=\"center\">";
            strVar += "                            <tr>";
            strVar += "                                <td style=\"vertical-align: middle;text-align: left;\">";
            strVar += "                                    <table style=\"border-collapse:collapse;font-size: 16px;font-family:Helvetica Neue,Helvetica,Lucida Grande,tahoma,verdana,arial,sans-serif;\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 5px 0px;text-transform: uppercase;font-size: 15px;font-weight: bold;line-height: 26px;color:#BCBEC8;\">";
            strVar += "";
            strVar += "                                                APP Builder";
            strVar += "";
            strVar += "                                            </td>";
            strVar += "                                        </tr>                                        ";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 10px 0px 20px 0px;font-size: 16px;color:#BCBEC8;font-weight: lighter;mso-line-height: exactly;line-height: 24px;\">";
            strVar += "                                                A visual tool built on top of a powerful Angular / HTML5 platform that has been proven over the past 20 years in the most demanding web applications, across many industries. ";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                       ";
            strVar += "                                        <tr>";
            strVar += "                                            <td style=\"padding: 5px 0px;\">";
            strVar += "";
            strVar += "                                            </td>";
            strVar += "                                        </tr>";
            strVar += "                                       ";
            strVar += "                                    </table>";
            strVar += "";
            strVar += "                                </td>";
            strVar += "                             ";
            strVar += "                            </tr>";
            strVar += "                        </table>";
            strVar += "";
            strVar += "                    </td>";
            strVar += "                </tr>              ";
            strVar += "";
            strVar += "               ";
            strVar += "                <tr>";
            strVar += "                    <td style=\"width: 100%;text-align: center;vertical-align: middle;font-size: 14px;font-weight: lighter;color:#010101;height: 60px;\">";
            strVar += "                        App Builder LLC | Montreal ";
            strVar += "                    </td>";
            strVar += "                </tr>";
            strVar += "";
            strVar += "            </table>";
            strVar += "        </td>";
            strVar += "    </tr>";
            strVar += "</table>";

            return strVar;
        }

    }
}