using System;
using Microsoft.Extensions.Caching.Memory;

namespace AppAI.Web.Services
{
    /// <summary>
    /// Short-lived tokens for interactive agent Result pages (iframe src).
    /// Unlike print tokens, these are reusable within the TTL so the iframe can reload.
    /// </summary>
    public class AgentUiRenderTokenService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(60);

        public AgentUiRenderTokenService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string Issue(AgentUiRenderTokenPayload payload)
        {
            var token = Guid.NewGuid().ToString("N");
            _cache.Set(CacheKey(token), payload, Ttl);
            return token;
        }

        public bool TryGet(string token, out AgentUiRenderTokenPayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(token)) return false;
            return _cache.TryGetValue(CacheKey(token), out payload) && payload != null;
        }

        private static string CacheKey(string token) => "agent_ui_render:" + token;
    }

    public class AgentUiRenderTokenPayload
    {
        public string Html { get; set; }
        public string RenderId { get; set; }
        public string SessionKey { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
    }
}
