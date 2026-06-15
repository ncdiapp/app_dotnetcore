using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;

using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Timers;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Framework;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;


namespace App.BL
{

    // must agaist APP Master DB to do all session validation !!!
    public class AppSecurityUserSessionBL
    {



        public static readonly Timer SessionTimer = new Timer();
        private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;
       

        public static Dictionary<int, int> RetrieveCurrentUserByDomain()
        {
            // EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();
            string querySesstion = @"SELECT DISTINCT  AppSecurityUser.DomainID , AppSecurityUserSession.SessionID, AppSecurityUserSession.UserID
                 FROM          AppSecurityUser INNER JOIN   AppSecurityUserSession ON  AppSecurityUser.UserID = AppSecurityUserSession.UserID";

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {

                DataTable result = adapter.ExecuteDataTableRetrievalQuery(querySesstion, null);
                return result.AsEnumerable().GroupBy(row => (int)row["DomainID"])
                                     .Select(grp => new { grp.Key, Count = grp.Count() })
                                     .ToDictionary(grp => grp.Key, grp => grp.Count);


            }

        }

        public static bool UpdateLoginUserExpiredDate()
        {
            if (ServerContext.Instance.CurrentSessionId != null)
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    AppSecurityUserSessionEntity aAppSecurityUserSessionEntity = new AppSecurityUserSessionEntity();
                    aAppSecurityUserSessionEntity.ExpirationDate = System.DateTime.UtcNow.AddMinutes(SessionAdnGracePeriodTime);
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserSessionFields.SessionId == ServerContext.Instance.CurrentSessionId 
                        & (AppSecurityUserSessionFields.EmExternalSigninType == System.DBNull.Value | (AppSecurityUserSessionFields.EmExternalSigninType != (int)EmAppExternalLoginType.Anonymous & AppSecurityUserSessionFields.EmExternalSigninType != (int)EmAppExternalLoginType.Integration)));

                    adapter.UpdateEntitiesDirectly(aAppSecurityUserSessionEntity, filter);

                    return true;
                }
            }
            else
            {
                return false;

            }
        }

        internal static int SessionAdnGracePeriodTime
        {
            get
            {
               
                    int? timoutValueInmintus = AppSystemSettingBL.GetIntValue(EmSystemSettings.Timeout);
                    timoutValueInmintus = timoutValueInmintus.HasValue ? timoutValueInmintus.Value : 30;
                    int? timoutGracingValueInmintus = AppSystemSettingBL.GetIntValue(EmSystemSettings.TimeoutWarningGracePeriod);
                    timoutGracingValueInmintus = timoutGracingValueInmintus.HasValue ? timoutGracingValueInmintus.Value : 5;
                    return timoutValueInmintus.Value + timoutGracingValueInmintus.Value;
             
               

                
            }
        }

        public static void CreateNewAppSecurityUserSession(UserContext aUserContext)
        {
            AppSecurityUserSessionEntity aAppSecurityUserSessionEntity = new AppSecurityUserSessionEntity();
            aAppSecurityUserSessionEntity.UserId = (int)aUserContext.UserId;
            aAppSecurityUserSessionEntity.SessionId = aUserContext.SessionId as string;


            
            
                aAppSecurityUserSessionEntity.ExpirationDate = System.DateTime.UtcNow.AddMinutes(SessionAdnGracePeriodTime);
                aAppSecurityUserSessionEntity.ApplicationType = 1;
           
          

            aAppSecurityUserSessionEntity.AppCreatedByCompanyId = aUserContext.ServerSideCurrentCompnayId;

            aAppSecurityUserSessionEntity.ExternalAcessToken = aUserContext.ExternalAcessToken;
            aAppSecurityUserSessionEntity.EmExternalSigninType = aUserContext.EmExternalSigninType;


            //need to call master DB ConnectionStringFromConfig
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityUserSessionEntity);

                    adapter.Commit();
                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adapter.Rollback();
                }
            }

        }

        public static AppSecurityUserSessionEntity GetSessionEntityBySessionID(string currentSessionId)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserSessionFields.SessionId == currentSessionId);

                EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();
                adapter.FetchEntityCollection(list, filter);

                if (list.Count > 0)
                {
                    return list[0];
                }

                return null;

            }
        }

        public static int? GetUserIDBySessionID(string currentSessionId)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserSessionFields.SessionId == currentSessionId);

                EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();
                adapter.FetchEntityCollection(list, filter);

                if (list.Count > 0)
                {
                    return list[0].UserId;
                }

                return null;

            }
        }

        public static bool DeleteSecurityWebUserSession(object currentSessionId)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(

                        AppSecurityUserSessionFields.SessionId == currentSessionId));

                    adapter.Commit();
                    return true;
                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adapter.Rollback();

                    return false;
                }
            }
        }

        public static bool DeleteEXpiredSecurityWebUserSession()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(AppSecurityUserSessionFields.ExpirationDate < System.DateTime.UtcNow 
                        & (AppSecurityUserSessionFields.EmExternalSigninType == System.DBNull.Value | AppSecurityUserSessionFields.EmExternalSigninType != (int)EmAppExternalLoginType.Anonymous)));

                    adapter.Commit();
                    return true;
                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adapter.Rollback();

                    return false;
                }
            }
        }

        public static bool DeleteAllSecurityWebUserSession()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(AppSecurityUserSessionFields.UserSessionId > 0));


                    adapter.Commit();
                    return true;
                }

                // Entity Logical Validation Exception
                catch (Exception ex)
                {
                    adapter.Rollback();

                    return false;
                }
            }
        }

        internal static void StartSessionTimer()
        {
            DeleteEXpiredSecurityWebUserSession();

            SessionTimer.Elapsed += new ElapsedEventHandler(SessionTimer_Elapsed);
            SessionTimer.Interval = 5 * 60 * 1000;
            SessionTimer.Start();
        }

        private static void SessionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DeleteEXpiredSecurityWebUserSession();
            // throw new NotImplementedException();
        }



        public static bool CheckIfUserAlreayLogIn(int Uid)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserSessionFields.UserId == Uid);

                EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();
                adapter.FetchEntityCollection(list, filter);

                return list.Count > 0;
            }
        }

        private static AppSecurityUserSessionEntity RetrieveOneAppSecurityUserSessionEntity(object id)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                AppSecurityUserSessionEntity aEntity = new AppSecurityUserSessionEntity(int.Parse(id.ToString()));
                adapter.FetchEntity(aEntity);

                return aEntity;
            }
        }

        public static AppSecurityUserSessionExDto RetrieveOneAppSecurityUserSessionExDto(object id)
        {
            AppSecurityUserSessionEntity aAppSecurityUserSessionEntity = RetrieveOneAppSecurityUserSessionEntity(id);
            AppSecurityUserSessionExDto aAppSecurityUserSessionExDto = AppSecurityUserSessionConverter.ConvertEntityToExDto(aAppSecurityUserSessionEntity);
            return aAppSecurityUserSessionExDto;
        }

        public static ObservableSet<AppSecurityUserSessionDto> RetrieveAllAppSecurityUserSessionDto()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();
                adapter.FetchEntityCollection(list, null);

                var aDtoList = new ObservableSet<AppSecurityUserSessionDto>();

                foreach (var o in list)
                {
                    aDtoList.Add(AppSecurityUserSessionConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }

        public static OperationCallResult<AppSecurityUserSessionExDto> SaveAppSecurityUserSessionExDto(AppSecurityUserSessionExDto aAppSecurityUserSessionExDto)
        {
            OperationCallResult<AppSecurityUserSessionExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserSessionExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSecurityUserSessionEntity aAppSecurityUserSessionEntity;

            // Save New
            if (aAppSecurityUserSessionExDto.IsNew)
            {
                aAppSecurityUserSessionEntity = new AppSecurityUserSessionEntity();
                AppSecurityUserSessionConverter.CopyDtoToEntity(aAppSecurityUserSessionEntity, aAppSecurityUserSessionExDto);

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppSecurityUserSessionEntity);
                        adapter.Commit();

                        aAppSecurityUserSessionExDto.Id = aAppSecurityUserSessionEntity.UserSessionId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppSecurityUserSessionExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(SaveAppSecurityUserSessionExDto_ProcessDirtyTblAppSecurityUserSessionDto(aAppSecurityUserSessionExDto));
            }

            // if no any errors, refresh from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSecurityUserSessionExDto(aAppSecurityUserSessionExDto.Id);
            }

            return aOperationCallResult;
        }

        private static ValidationResult SaveAppSecurityUserSessionExDto_ProcessDirtyTblAppSecurityUserSessionDto(AppSecurityUserSessionExDto aAppSecurityUserSessionExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();
            AppSecurityUserSessionEntity aAppSecurityUserSessionEntity = RetrieveOneAppSecurityUserSessionEntity(aAppSecurityUserSessionExDto.Id);
            AppSecurityUserSessionConverter.CopyDtoToEntity(aAppSecurityUserSessionEntity, aAppSecurityUserSessionExDto);

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSecurityUserSessionEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
            return aValidationResult;
        }

        public static OperationCallResult<object> DeleteAppSecurityUserSession(object userSessionId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string applicationToken = "6601508d-e7e0-4ed6-892b-879c834676af";

            if (userSessionId != null && userSessionId.ToString() != applicationToken)
            {

                using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(AppSecurityUserSessionFields.SessionId == userSessionId));

                        aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_Delete_Ok", ValidationItemType.Message, "plm_AppSecurityUserSessionEntity_Delete_Ok"));
                        adapter.Commit();
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserSessionEntity), "plm_AppSecurityUserSessionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // if no any errors
                    if (!aValidationResult.ValidationResult.HasErrors)
                    {
                        aValidationResult.Object = userSessionId;
                    }
                }
            }

            return aValidationResult;
        }


        // need to check againt master DB



        public static double CheckCurrenSessionIsExsit(object sessionId)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserSessionFields.SessionId == sessionId);
                adapter.FetchEntityCollection(list, filter);
                if (list.Count > 0)
                {
                    bool isAnoymouseSession = list.FirstOrDefault(o => o.EmExternalSigninType.HasValue && o.EmExternalSigninType.Value == (int)EmAppExternalLoginType.Anonymous) != null;
                    if (isAnoymouseSession)
                    {
                        return 10000;
                    }
                    else
                    {
                        DateTime expireDate = list.Max(o => o.ExpirationDate);
                        TimeSpan span = expireDate.Subtract(DateTime.UtcNow);
                        double timeRemainInMinutes = span.TotalMinutes;
                        return timeRemainInMinutes;
                    }
                }
            }

            return -1;
        }

        private static int? GetUserIdBySessionId()
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(MasterConnStr))
            {
                EntityCollection<AppSecurityUserSessionEntity> list = new EntityCollection<AppSecurityUserSessionEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserSessionFields.SessionId == ServerContext.Instance.CurrentSessionId);
                adapter.FetchEntityCollection(list, filter);
                if (list.Count > 0)
                {

                    return list[0].UserId;

                }




            }

            return null;


        }


        public static bool TouchServer(object currentSessionId)
        {
            return AppSecurityUserSessionBL.UpdateLoginUserExpiredDate();
        }


        public static List<string> GetActUserList()
        {
            return ADHelp.ActUserList;
        }


        public static bool SetupClientIdentity(string sessionId)
        {
            //  int userId = Convert.ToInt32(HttpContext.Current.Request.Headers["userId"]);

            int? userId = AppSecurityUserSessionBL.GetUserIDBySessionID(sessionId);
            if (userId.HasValue)
            {

                AppSecurityUserSessionBL.TouchServer(sessionId);

                return true;

            }
            else
            {
                return false;
            }



        }
    }


}