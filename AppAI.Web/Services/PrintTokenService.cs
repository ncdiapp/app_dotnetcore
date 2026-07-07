using System;
using Microsoft.Extensions.Caching.Memory;

namespace AppAI.Web.Services
{
    /// <summary>
    /// Issues short-lived one-time tokens so the internal Playwright server call
    /// can authenticate to the print page without exposing user credentials.
    /// Each token is valid for 60 seconds and consumed on first use.
    /// </summary>
    public class PrintTokenService
    {
        private readonly IMemoryCache _cache;

        public PrintTokenService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public string IssueToken(int userId, string printParam)
        {
            var token = Guid.NewGuid().ToString("N");
            _cache.Set(CacheKey(token), new PrintTokenEntry { UserId = userId, PrintParam = printParam },
                TimeSpan.FromSeconds(60));
            return token;
        }

        public bool ValidateToken(string token, out int userId, out string printParam)
        {
            userId = 0;
            printParam = null;
            if (string.IsNullOrEmpty(token)) return false;

            var key = CacheKey(token);
            if (!_cache.TryGetValue<PrintTokenEntry>(key, out var entry)) return false;

            _cache.Remove(key); // one-time use
            userId = entry.UserId;
            printParam = entry.PrintParam;
            return true;
        }

        private static string CacheKey(string token) => $"print_token:{token}";

        private class PrintTokenEntry
        {
            public int    UserId     { get; set; }
            public string PrintParam { get; set; }
        }
    }
}
