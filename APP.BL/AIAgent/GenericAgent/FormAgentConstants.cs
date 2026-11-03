using System;
using System.Security.Cryptography;
using System.Text;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Reserved Specialist that runs inside Master Detail and List Edit forms.
    /// AgentCode is fixed. Display name, prompt, and subscriptions may change.
    /// </summary>
    public static class FormAgentConstants
    {
        public const string SkillKey = "app-transaction-form-agent";
        public const string DisplayName = "App Form Agent";

        /// <summary>AppMessage.TransactionRootValueID for a whole List Edit transaction.</summary>
        public const string ListCommunicationRoot = "__list__";

        public const string ChatKeyPrefix = "form-host:";

        public static bool IsFormAgentKey(string skillKey)
        {
            return string.Equals(skillKey?.Trim(), SkillKey, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFormHostChatKeyForUser(string sessionKey, int userId)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(sessionKey)) return false;
            return sessionKey.StartsWith(ChatKeyPrefix + userId + ":", StringComparison.Ordinal);
        }

        public static string MakeChatKey(int userId, int transactionId, string scope)
        {
            var safeScope = string.IsNullOrWhiteSpace(scope) ? "list" : scope.Trim();
            var key = ChatKeyPrefix + userId + ":" + transactionId + ":" + safeScope;
            if (key.Length <= 200) return key;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(safeScope))).Substring(0, 16).ToLowerInvariant();
            var trimmed = ChatKeyPrefix + userId + ":" + transactionId + ":" + hash;
            return trimmed.Length <= 200 ? trimmed : trimmed.Substring(0, 200);
        }
    }
}
