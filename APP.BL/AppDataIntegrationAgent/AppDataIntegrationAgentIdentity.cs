using System;
using App.BL;
using APP.Components.Dto;
using APP.Framework;
using Newtonsoft.Json;

namespace App.BL.AppDataIntegrationAgent
{
    /// <summary>
    /// Background Task.Run has no HttpContext, so ServerContext falls through to
    /// WindowsIdentityProvider. App Builder Agent registers the captured identity there;
    /// App Data Integration Agent must do the same. MCP requests do have HttpContext, so identity
    /// must also be written to HttpIdentityProvider or CurrnetClientIdentity is null.
    /// </summary>
    internal static class AppDataIntegrationAgentIdentity
    {
        public static void Capture(AppDataIntegrationAgentSessionStore.SessionData live, AppClientIdentity? identity)
        {
            if (live == null) return;
            if (!identity.HasValue && ServerContext.Instance.CurrnetClientIdentity is AppClientIdentity current)
                identity = current;
            if (!identity.HasValue) return;

            live.Identity = identity;
            live.IdentityJson = Serialize(identity);
            if (identity.Value.SessionId != null)
                live.AppSessionId = identity.Value.SessionId.ToString();
            if (live.CreatedById == null && identity.Value.UserId != null)
                live.CreatedById = Convert.ToInt32(identity.Value.UserId);
            if (live.CompanyId == null && identity.Value.CurrentWorkingCompanyId != null)
                live.CompanyId = Convert.ToInt32(identity.Value.CurrentWorkingCompanyId);
        }

        public static void Restore(AppDataIntegrationAgentSessionStore.SessionData live)
        {
            if (live == null) return;

            var identity = live.Identity;
            if (!identity.HasValue)
                identity = Deserialize(live.IdentityJson);

            if (!identity.HasValue && !string.IsNullOrWhiteSpace(live.AppSessionId))
            {
                AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity(live.AppSessionId);
                if (ServerContext.Instance.CurrnetClientIdentity is AppClientIdentity rebuilt)
                    identity = rebuilt;
            }

            if (!identity.HasValue) return;

            live.Identity = identity;
            live.IdentityJson = Serialize(identity);

            if (live.CompanyId == null && identity.Value.CurrentWorkingCompanyId != null)
                live.CompanyId = Convert.ToInt32(identity.Value.CurrentWorkingCompanyId);

            var sc = ServerContext.Instance;
            if (sc.WindowsIdentityProvider != null)
                sc.WindowsIdentityProvider.RegisterIdentity(identity);
            if (sc.HttpIdentityProvider != null)
                sc.HttpIdentityProvider.RegisterIdentity(identity);
        }

        public static string Serialize(AppClientIdentity? identity)
        {
            if (!identity.HasValue) return null;
            try { return JsonConvert.SerializeObject(identity.Value); }
            catch { return null; }
        }

        public static AppClientIdentity? Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonConvert.DeserializeObject<AppClientIdentity>(json); }
            catch { return null; }
        }
    }
}
