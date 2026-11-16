using System;

namespace App.BL.AIAgent.GenericAgent
{
    public static class AppBuilderEditorConstants
    {
        public const string SkillKey = "app-builder";
        public const string ChatKeyPrefix = "app-builder-editor:";

        /// <summary>app-builder-editor:{userId}:{saasApplicationId}:{scope}</summary>
        public static string MakeChatKey(int userId, int saasApplicationId, string scope)
        {
            var s = string.IsNullOrWhiteSpace(scope) ? "app" : scope.Trim();
            return $"{ChatKeyPrefix}{userId}:{saasApplicationId}:{s}";
        }

        public static bool IsAppBuilderEditorChatKeyForUser(string sessionKey, int userId) =>
            !string.IsNullOrWhiteSpace(sessionKey)
            && sessionKey.StartsWith($"{ChatKeyPrefix}{userId}:", StringComparison.OrdinalIgnoreCase);
    }
}
