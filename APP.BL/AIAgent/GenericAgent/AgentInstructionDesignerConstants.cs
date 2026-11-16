using System;
using System.Security.Cryptography;
using System.Text;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Reserved Specialist that authors other agents' SystemPrompt + Tools in Agent Management.
    /// AgentCode is fixed. Display name, prompt, and subscriptions may change.
    /// </summary>
    public static class AgentInstructionDesignerConstants
    {
        public const string SkillKey = "agent-instruction-designer";
        public const string DisplayName = "Agent Instruction Designer";

        public const string ChatKeyPrefix = "agent-design:";

        public static bool IsDesignerKey(string skillKey)
        {
            return string.Equals(skillKey?.Trim(), SkillKey, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDesignerChatKeyForUser(string sessionKey, int userId)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(sessionKey)) return false;
            return sessionKey.StartsWith(ChatKeyPrefix + userId + ":", StringComparison.Ordinal);
        }

        /// <summary>
        /// One stable Designer chat per (user + target). Unsaved drafts use scope "new:{clientDraftId}".
        /// </summary>
        public static string MakeChatKey(int userId, string targetScope)
        {
            var safeScope = string.IsNullOrWhiteSpace(targetScope) ? "unknown" : targetScope.Trim();
            var key = ChatKeyPrefix + userId + ":" + safeScope;
            if (key.Length <= 200) return key;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(safeScope))).Substring(0, 16).ToLowerInvariant();
            var trimmed = ChatKeyPrefix + userId + ":" + hash;
            return trimmed.Length <= 200 ? trimmed : trimmed.Substring(0, 200);
        }
    }
}
