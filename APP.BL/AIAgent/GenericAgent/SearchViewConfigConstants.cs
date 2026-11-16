using System;

namespace App.BL.AIAgent.GenericAgent
{
    public static class SearchViewConfigConstants
    {
        public const string SkillKey = "app-search-view-config";
        public const string ChatKeyPrefix = "search-view-config:";

        public static bool IsSearchViewConfigSkill(string skillKey) =>
            string.Equals(skillKey?.Trim(), SkillKey, StringComparison.OrdinalIgnoreCase);

        /// <summary>search-view-config:{userId}:{searchId}:{scope}</summary>
        public static string MakeChatKey(int userId, int searchId, string scope)
        {
            var s = string.IsNullOrWhiteSpace(scope) ? "default" : scope.Trim();
            return $"{ChatKeyPrefix}{userId}:{searchId}:{s}";
        }

        public static bool IsSearchViewConfigChatKeyForUser(string sessionKey, int userId) =>
            !string.IsNullOrWhiteSpace(sessionKey)
            && sessionKey.StartsWith($"{ChatKeyPrefix}{userId}:", StringComparison.OrdinalIgnoreCase);
    }
}
