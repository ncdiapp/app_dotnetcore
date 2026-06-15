using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework;
using System.Net;

namespace App.BL
{

    /// <summary>
    ///  need to rename the following fields
    ///   AppSecurityUserFields.AppCreatedByCompanyId:  MyOnwerCompnayID
    ///      AppSecurityUserSessionFields.AppCreatedByCompanyId :  CurrentWorkingCompanyId
    ///   AppDataSourceRegisterFields.AppCreatedByCompanyId : AssignedToCompnayID
    ///   need to drop  AppBusinessPartnerInviteUser.AppCreatedByCompanyID 
    /// </summary>
    public static class AppSaasUserSessionMgtBL
    {

        // need to update from MVC controler 
        public static void UpdateCurrentUserSessionCompnayIdAfterPassAuthentication(string sessionId, int? currentWorkingCompanyId)
        {
            AppSecurityUserSessionEntity aAppSecurityUserSessionEntity = new AppSecurityUserSessionEntity();

            aAppSecurityUserSessionEntity.SessionId = sessionId;

            aAppSecurityUserSessionEntity.ExpirationDate = System.DateTime.UtcNow.AddMinutes(AppSecurityUserSessionBL.SessionAdnGracePeriodTime);
            aAppSecurityUserSessionEntity.ApplicationType = 1;

            //  aAppSecurityUserSessionEntity.AppCreatedByCompanyId alwasy store  currentWorkingCompanyId !!
            aAppSecurityUserSessionEntity.AppCreatedByCompanyId = currentWorkingCompanyId;

            //need to call master DB ConnectionStringFromConfig
            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.UpdateEntitiesDirectly(aAppSecurityUserSessionEntity, new RelationPredicateBucket(

                         AppSecurityUserSessionFields.SessionId == sessionId
                        ));

                    adapter.Commit();
                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adapter.Rollback();
                }
            }

        }



        public static void ViladateSessionIdAndCompanyIdRegisterIdentity(string SessionId)
        {
            if (string.IsNullOrWhiteSpace(SessionId))
            {
                throw new Exception("HttpStatusCode:" + HttpStatusCode.Unauthorized);
            }
            else
            {

                AppSecurityUserSessionEntity sessionEntity = AppSecurityUserSessionBL.GetSessionEntityBySessionID(SessionId);

                if (sessionEntity == null)
                {
                    //var httpResponseException = new HttpResponseException(
                    //String innerMessage = (ex.InnerException != null) 
                    //   ? ex.InnerException.Message
                    //   : ""
                    // httpResponseException.InnerException = "Invalid Session";

                    throw new Exception("HttpStatusCode:" + HttpStatusCode.Unauthorized);
                }
                else
                {
                    // get userID and compnayID  from Master
                    // AppSecurityManagementBL.RegisterUserIdentityTotheSystem(currentUserId, SessionId, currentCompanyId);

                    AppSaasUserSessionMgtBL.RegisterUserIdentityTotheSystem(sessionEntity);

                }

            }
        }

        public static void UpdateUserTimeZoneWithSession(UserContext userContext, string timzoneToken, string timezoneShortDisplayName, string timeZoneOffset)
        {
            if (userContext != null && userContext.SessionId != null && !string.IsNullOrWhiteSpace(timezoneShortDisplayName))
            {
                string sessionId = userContext.SessionId.ToString();

                AppSecurityUserSessionEntity sessionEntity = AppSecurityUserSessionBL.GetSessionEntityBySessionID(sessionId);

                timeZoneOffset = AppTimeZoneBL.PrepareOffsetToken(timeZoneOffset);

                using (DataAccessAdapter adpater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
                {
                    try
                    {
                        AppSecurityUserEntity userEntity = new AppSecurityUserEntity(sessionEntity.UserId);
                                  
                       
                        userEntity.AdloginName = timezoneShortDisplayName;
                        userEntity.Adpassword = timeZoneOffset;

                        if (!string.IsNullOrWhiteSpace(timzoneToken))
                        {                            
                            userEntity.TimeZoneInfoToken = timzoneToken;                          
                        }

                        //adpater.SaveEntity(userEntity);

                        adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adpater.UpdateEntitiesDirectly(userEntity, new RelationPredicateBucket(

                             AppSecurityUserFields.UserId == sessionEntity.UserId
                            ));

                        adpater.Commit();

                        AppSecurityUserEntity currentyuserEntity = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(sessionEntity.UserId);
                        currentyuserEntity.AdloginName = timezoneShortDisplayName;
                        currentyuserEntity.Adpassword = timeZoneOffset;

                        userContext.TimeZoneDisplay = timezoneShortDisplayName;

                        if (!string.IsNullOrWhiteSpace(timzoneToken))
                        {                            
                            currentyuserEntity.TimeZoneInfoToken = timzoneToken;
                            userContext.TimeZoneKey = timzoneToken;
                        }

                        
                       
                    }

                    // Entity Logical Validation Exception
                    catch (Exception ex)
                    {
                        adpater.Rollback();
                    }
                }
            }

        }



        public static void RegisterUserDevicdeIdWithSession(string currentUserSessionId, string refreshTokenDeviceId)
        {


            AppSecurityUserSessionEntity sessionEntity = AppSecurityUserSessionBL.GetSessionEntityBySessionID(currentUserSessionId);

            AppSecurityUserEntity currentyuserEntity = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(sessionEntity.UserId);

            currentyuserEntity.RefreshToken = refreshTokenDeviceId;


            using (DataAccessAdapter adpater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                try
                {
                    AppSecurityUserEntity userEntity = new AppSecurityUserEntity(sessionEntity.UserId);
                    userEntity.RefreshToken = refreshTokenDeviceId;

                    //adpater.SaveEntity(userEntity);

                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adpater.UpdateEntitiesDirectly(userEntity, new RelationPredicateBucket(

                         AppSecurityUserFields.UserId == sessionEntity.UserId
                        ));

                    adpater.Commit();
                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adpater.Rollback();
                }





            }




        }


        //TODO  IMPORTANT need to add compnayID for each client call
        internal static void RegisterUserIdentityTotheSystem(AppSecurityUserSessionEntity sessionEntity)
        {
            // Session Managment  in  Master DB
            AppSecurityUserSessionBL.TouchServer(sessionEntity.SessionId);

            int? currentWorkingCompanyId = sessionEntity.AppCreatedByCompanyId;
            int loginUserId = sessionEntity.UserId;

            //  from Master DB, need to get from Master cache as well
            AppSecurityUserEntity currentyuserEntity = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(loginUserId);

            bool isMasterUser = currentyuserEntity.DomainId == (int)EmAppUserType.SysAdmin;

            // SysAdmin operates on the master DB directly — skip tenant datasource lookup entirely.
            if (isMasterUser)
            {
                string masterConnStr = AppCompanyBL.AppMasterDBConnectionString;
                var csb = new System.Data.SqlClient.SqlConnectionStringBuilder(masterConnStr);

                AppClientIdentity sysAdminClient = new AppClientIdentity();
                sysAdminClient.UserId = currentyuserEntity.UserId;
                sysAdminClient.LanguageId = currentyuserEntity.LanguageId;
                sysAdminClient.TimeZoneKey = currentyuserEntity.TimeZoneInfoToken;
                sysAdminClient.SessionId = sessionEntity.SessionId;
                sysAdminClient.CurrentLoginUserType = (int)EmAppUserType.SysAdmin;
                sysAdminClient.CurrentUserDbConnectionString = masterConnStr;
                sysAdminClient.CurrentUserDataBaseName = csb.InitialCatalog;
                sysAdminClient.CurrentWorkingCompanyId = null;

                ServerContext.Instance.HttpIdentityProvider.RegisterIdentity(sysAdminClient);
                return;
            }

            // Need to Cache Master DB user
            AppDataSourceRegisterEntity currentCompanyMasterDatasource = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(currentWorkingCompanyId.Value);

            // user master DB store all Transcation data module,
            // user exchange db store all exteral data source ( ddl entoty info, data presention etc...)
            if (currentCompanyMasterDatasource != null)
            {
                AppClientIdentity client = SetupCurrentSaasUserClientInfo(sessionEntity, currentyuserEntity, currentCompanyMasterDatasource);

                client = ClassifyCurrentLoginUserType(currentWorkingCompanyId, currentyuserEntity, client);

                ServerContext.Instance.HttpIdentityProvider.RegisterIdentity(client);
            }
            else
            {
                throw new Exception("Cannot find User Master DB");
            }
        }

        private static AppClientIdentity ClassifyCurrentLoginUserType(int? currentWorkingCompanyId, AppSecurityUserEntity currentyuserEntity, AppClientIdentity client)
        {
            int loginUserId = currentyuserEntity.UserId;

            // SysAdmin is classified before entering this method; guard here as safety net.
            if (currentyuserEntity.DomainId == (int)EmAppUserType.SysAdmin)
            {
                client.CurrentLoginUserType = (int)EmAppUserType.SysAdmin;
                return client;
            }

            if (currentyuserEntity.MyOwnCompnanyId.HasValue)
            {
                // it is currnet compnay ( employee
                if (currentyuserEntity.MyOwnCompnanyId == currentWorkingCompanyId)
                {
                    if (currentyuserEntity.DomainId == (int)EmAppUserType.SaasCompanyAdmin
                        || currentyuserEntity.DomainId == (int)EmAppUserType.CompanyAnonymousUser
                        || currentyuserEntity.DomainId == (int)EmAppUserType.CompanyWinScheduleUser
                        || currentyuserEntity.DomainId == (int)EmAppUserType.Integration

                        )
                    {
                        client.CurrentLoginUserType = currentyuserEntity.DomainId;

                    }                    
                    else
                    {
                        client.CurrentLoginUserType = (int)EmAppUserType.Employee;
                    }

                    // need to confirm !!!
                    //client.RuningTimeBusinessAccountId = currentyuserEntity.MappingExternalEmployeeAccountId;

                }
                else // It is not the smae compnay, it is partner 
                {
                    SetupBusinessParterUserType(ref client, currentWorkingCompanyId, loginUserId);

                }

            }//// My OwnCompnay is empty It partner type //Unknow type, if the user register his own compnay: need to set to 6 (appAdminDomain
            else
            {

                SetupBusinessParterUserType(ref client, currentWorkingCompanyId, loginUserId);

            }

            return client;
        }

        private static void SetupBusinessParterUserType(ref AppClientIdentity client, int? currentWorkingCompanyId, int loginUserId)
        {
            // 1. Per-user explicit role (most specific — set by invitation flow)
            int? invitedUserType = AppCacheManagerBL.GetInviteUserType(currentWorkingCompanyId.Value, loginUserId);
            if (invitedUserType.HasValue)
            {
                client.CurrentLoginUserType = invitedUserType.Value;
                var partnerForId = AppCacheManagerBL.GetCurrentUserWorkingCompanyBusinessPartner(
                    currentWorkingCompanyId.Value, loginUserId);
                if (partnerForId != null)
                    client.CurrentPartnerId = partnerForId.AppBusinessPartnerId;
                return;
            }

            // 2. Fall back to company-level PartnerType (backward compat with existing data)
            AppBusinessPartnerEntity aAppBusinessPartnerEntity =
                AppCacheManagerBL.GetCurrentUserWorkingCompanyBinessPartner(currentWorkingCompanyId.Value, loginUserId);

            if (aAppBusinessPartnerEntity != null)
            {
                client.CurrentLoginUserType = aAppBusinessPartnerEntity.PartnerType.Value;
                client.CurrentPartnerId     = aAppBusinessPartnerEntity.AppBusinessPartnerId;
            }
            else
            {
                client.CurrentLoginUserType = (int)EmAppUserType.Unknow;
            }
        }

        private static AppClientIdentity SetupCurrentSaasUserClientInfo(AppSecurityUserSessionEntity sessionEntity, AppSecurityUserEntity currentyuserEntity,
             AppDataSourceRegisterEntity currentCompanyMasterDatasource)
        {
            AppClientIdentity client = new AppClientIdentity();
            client.UserId = currentyuserEntity.UserId;
            client.LanguageId = currentyuserEntity.LanguageId;
            client.TimeZoneKey = currentyuserEntity.TimeZoneInfoToken;
            client.SessionId = sessionEntity.SessionId;



            client.CurrentUserDbConnectionString = AppConnectionStringEncryptionBL.Decrypt(currentCompanyMasterDatasource.ConnectionString);
            client.CurrentUserDataBaseName = currentCompanyMasterDatasource.DatabaseName;
            client.CurrentWorkingCompanyId = sessionEntity.AppCreatedByCompanyId;
            client.DataSourceId = currentCompanyMasterDatasource.DataSourceId;

            return client;

            //  client.CurrentLoginUserType = (int)EmAppUserType.SysAdmin;


        }






    }

}