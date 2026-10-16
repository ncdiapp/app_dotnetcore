using System;
using System.Data;
using APP.Components.Dto;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace App.BL
{
    /// <summary>
    /// Grant sessions: one AppSecurityUserSession row (master DB) per OAuth connection or in-app agent run. They give the
    /// OAuth server the same user + company + tenant identity registration every other request gets, without ever
    /// handing a login session id to an AI client. Rows use EmExternalSigninType = Integration so the normal session
    /// timeout does not slide their expiry (see AppSecurityUserSessionBL.UpdateLoginUserExpiredDate), and every
    /// statement here filters on that type, so a user's own login sessions are never touched.
    /// Used through APP.OAuth's IOAuthIdentityBridge (AppAI.Web/Services/OAuthIdentityBridge.cs).
    /// </summary>
    public static class AppOAuthGrantSessionBL
    {
        private static NLog.Logger Log => NLog.LogManager.GetCurrentClassLogger();

        public static string Create(int userId, int companyId, DateTime expiresUtc)
        {
            var session = new AppSecurityUserSessionEntity();
            session.UserId = userId;
            session.SessionId = Guid.NewGuid().ToString();
            session.AppCreatedDate = DateTime.UtcNow;
            session.ExpirationDate = expiresUtc;
            session.ApplicationType = 1;
            session.EmExternalSigninType = (int)EmAppExternalLoginType.Integration;
            session.AppCreatedByCompanyId = companyId;

            using (DataAccessAdapter adapter = AppMasterAdapterBL.GetMasterAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(session);
                    adapter.Commit();
                }
                catch (Exception ex)
                {
                    adapter.Rollback();
                    Log.Error(ex, nameof(Create));
                    throw;   // the caller must not hand out a credential that has no session behind it
                }
            }
            return session.SessionId;
        }

        public static void Delete(string grantSessionId)
        {
            if (string.IsNullOrWhiteSpace(grantSessionId)) return;
            try
            {
                using (DataAccessAdapter adapter = AppMasterAdapterBL.GetMasterAdapter())
                {
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserSessionEntity), new RelationPredicateBucket(
                        AppSecurityUserSessionFields.SessionId == grantSessionId
                        & AppSecurityUserSessionFields.EmExternalSigninType == (int)EmAppExternalLoginType.Integration));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(Delete));
                throw;
            }
        }

        public static bool IsValid(string grantSessionId, int userId, int companyId) =>
            Load(grantSessionId, userId, companyId) != null;

        /// <summary>Same checks as <see cref="IsValid"/>, then registers the user's identity for the current request.</summary>
        public static bool TryRegister(string grantSessionId, int userId, int companyId)
        {
            AppSecurityUserSessionEntity session = Load(grantSessionId, userId, companyId);
            if (session == null) return false;
            try
            {
                AppSaasUserSessionMgtBL.RegisterUserIdentityTotheSystem(session);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(TryRegister));
                return false;
            }
        }

        // The session must exist, be an Integration-type row for this user and company, be unexpired, and the user must
        // still be an active person account (not SysAdmin, a service account or an anonymous token).
        private static AppSecurityUserSessionEntity Load(string grantSessionId, int userId, int companyId)
        {
            if (string.IsNullOrWhiteSpace(grantSessionId) || userId <= 0 || companyId <= 0) return null;
            try
            {
                AppSecurityUserSessionEntity session = AppSecurityUserSessionBL.GetSessionEntityBySessionID(grantSessionId);
                if (session == null
                    || session.UserId != userId
                    || session.AppCreatedByCompanyId != companyId
                    || session.EmExternalSigninType != (int)EmAppExternalLoginType.Integration
                    || session.ExpirationDate < DateTime.UtcNow)
                    return null;

                AppSecurityUserEntity user = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(userId);
                if (user == null || !user.IsActive || user.IsDeleted || !IsPersonAccount(user.DomainId)) return null;
                return session;
            }
            catch (Exception ex)
            {
                Log.Error(ex, nameof(Load));
                return null;
            }
        }

        /// <summary>The login name of a user (master DB cache), or null when unknown.</summary>
        public static string GetLoginName(int userId)
        {
            try { return AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(userId)?.LoginName; }
            catch (Exception ex) { Log.Error(ex, nameof(GetLoginName)); return null; }
        }

        /// <summary>True for a real person's account type; false for SysAdmin, Integration/service and anonymous users.</summary>
        public static bool IsPersonAccount(int domainId) =>
            domainId != (int)EmAppUserType.SysAdmin
            && domainId != (int)EmAppUserType.Integration
            && domainId != (int)EmAppUserType.CompanyAnonymousUser
            && domainId != (int)EmAppUserType.CompanyWinScheduleUser;
    }
}
