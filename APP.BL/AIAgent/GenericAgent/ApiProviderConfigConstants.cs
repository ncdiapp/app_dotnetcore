using System;
using System.Security.Cryptography;
using System.Text;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Reserved Specialist for API Management durable configuration
    /// (App API Provider + 3rd-party + OpenAPI docs). Listed in left menu.
    /// Host dock chats use prefix api-config:{userId}:{scope}.
    /// </summary>
    public static class ApiProviderConfigConstants
    {
        public const string SkillKey = "app-api-configurator";
        public const string DisplayName = "API Provider Configurator Agent";
        public const string ConfigLibraryKey = "platform-api-integration-config";
        public const string ExecuteLibraryKey = "platform-api-provider";
        public const string ChatKeyPrefix = "api-config:";

        public static bool IsApiProviderConfigKey(string skillKey)
        {
            return string.Equals(skillKey?.Trim(), SkillKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>api-config:{userId}:{scope} — scope e.g. provider:1, api:4401, openapi:3, list:app-api.</summary>
        public static string MakeChatKey(int userId, string scope)
        {
            var safeScope = string.IsNullOrWhiteSpace(scope) ? "default" : scope.Trim();
            var key = ChatKeyPrefix + userId + ":" + safeScope;
            if (key.Length <= 200) return key;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(safeScope))).Substring(0, 16).ToLowerInvariant();
            var trimmed = ChatKeyPrefix + userId + ":" + hash;
            return trimmed.Length <= 200 ? trimmed : trimmed.Substring(0, 200);
        }

        public static bool IsApiProviderConfigChatKeyForUser(string sessionKey, int userId)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(sessionKey)) return false;
            return sessionKey.StartsWith(ChatKeyPrefix + userId + ":", StringComparison.Ordinal);
        }
    }
}
