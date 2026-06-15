using System;
using System.Collections.Generic;
using System.Linq;
#if NETFRAMEWORK
using System.Web;
#else
using Microsoft.AspNetCore.Http;
#endif
using APP.Framework.Communication;


namespace APP.Framework.Communication
{
    public class HttpClientIdentityProvider : IClientIdentityProvider<APP.Components.Dto.AppClientIdentity>
    {
        private const string _CurrentREquestIdentityKey = "CB93D768-5EB5-492B-87D1-E3D265CA4FDB";

#if !NETFRAMEWORK
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HttpClientIdentityProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private Microsoft.AspNetCore.Http.HttpContext? CurrentHttpContext => _httpContextAccessor?.HttpContext;
#else
        private System.Web.HttpContext? CurrentHttpContext => System.Web.HttpContext.Current;
#endif

        public APP.Components.Dto.AppClientIdentity? ProvideIdentity()
        {
            if (CurrentHttpContext?.Items.ContainsKey(_CurrentREquestIdentityKey) == true)
            {
                return (APP.Components.Dto.AppClientIdentity?)CurrentHttpContext.Items[_CurrentREquestIdentityKey];
            }
            return null;
        }

        public void RegisterIdentity(APP.Components.Dto.AppClientIdentity? identity)
        {
            if (CurrentHttpContext != null)
                CurrentHttpContext.Items[_CurrentREquestIdentityKey] = identity;
        }

        public string Name
        {
            get { return APP.Components.Dto.AppClientIdentity.Key; }
        }

        public string Namespace
        {
            get { return "http://visual-2000.com/plms/"; }
        }

        public string SerializeIdentity(APP.Components.Dto.AppClientIdentity identity)
        {
            return identity.Serialize();
        }

        public APP.Components.Dto.AppClientIdentity DeserializeIdentity(string identity)
        {
            return APP.Components.Dto.AppClientIdentity.Deserialize(identity);
        }
    }
}