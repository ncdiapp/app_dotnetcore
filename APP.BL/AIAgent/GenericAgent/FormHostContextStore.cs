using System.Collections.Concurrent;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// HostContextJson for the current agent run, keyed by ChatSessionKey.
    /// Form host tools read transactionId from it. Cleared when the run ends.
    /// </summary>
    public static class FormHostContextStore
    {
        static readonly ConcurrentDictionary<string, string> Map = new ConcurrentDictionary<string, string>();

        public static void Set(string chatSessionKey, string hostContextJson)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            Map[chatSessionKey.Trim()] = hostContextJson ?? "";
        }

        public static string Get(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return null;
            return Map.TryGetValue(chatSessionKey.Trim(), out var json) ? json : null;
        }

        public static void Clear(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            Map.TryRemove(chatSessionKey.Trim(), out _);
        }
    }
}
