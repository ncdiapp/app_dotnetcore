using System;
using System.Text;
using App.BL;
using APP.Components.Dto;
using APP.Framework;
using APP.LBL.EntityClasses;

namespace AppWebPluin.Models;

public class HttpBasicAuthenticator
{
    private readonly string authHeader;

    public HttpBasicAuthenticator(string username, string password)
    {
        string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        authHeader = $"Basic {token}";
    }

    public static void RegisterUserIdentity(int? userId)
    {
        if (!userId.HasValue)
            userId = 1;

        AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId);

        if (userEntity != null)
        {
            AppClientIdentity client = new AppClientIdentity()
            {
                UserId = userId,
                SessionId = Guid.NewGuid().ToString(),
                IsCallingFromBrowser = true,
                LanguageId = userEntity.LanguageId,
                CurrentWorkingCompanyId = userEntity.MyOwnCompnanyId,
                TimeZoneKey = userEntity.TimeZoneInfoToken,
            };
            ServerContext.Instance.HttpIdentityProvider.RegisterIdentity(client);
        }
    }

    public static void RegisterSysTemAgentWebUserIdentity()
    {
        int? systemAgentUserId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemAgentUser);
        RegisterUserIdentity(systemAgentUserId);
    }
}
