using System;
using System.Collections.Concurrent;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Tracks whether app-configurator chat sessions have successfully loaded
    /// <c>commands-knowledge.md</c> via <c>file_read</c>. Survives across runs
    /// for the same ChatSessionKey so the model need not re-read (and re-inject)
    /// the full KB into every turn.
    /// </summary>
    public static class AgentCommandsKbSessionStore
    {
        public const string KbFileName = "commands-knowledge.md";

        static readonly ConcurrentDictionary<string, byte> Loaded = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        public static bool IsCommandsKnowledgePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;
            var n = relativePath.Replace('\\', '/').Trim().TrimStart('/');
            return n.EndsWith(KbFileName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasLoaded(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            return Loaded.ContainsKey(chatSessionKey.Trim());
        }

        public static void MarkLoaded(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            Loaded[chatSessionKey.Trim()] = 1;
        }

        public static void Clear(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            Loaded.TryRemove(chatSessionKey.Trim(), out _);
        }

        /// <summary>
        /// True when <paramref name="content"/> looks like the real KB body (not a disable stub / empty).
        /// Truncated CapResult delivery is handled separately in SharedAgentEngine (Clear on clip).
        /// </summary>
        public static bool IsUsableKbContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            var c = content.Trim();
            if (c.Length < 200) return false;
            if (c.StartsWith("THIS_KB", StringComparison.OrdinalIgnoreCase)) return false;
            if (c.Equals("this file is disabled", StringComparison.OrdinalIgnoreCase)) return false;
            if (c.IndexOf("commands-knowledge", StringComparison.OrdinalIgnoreCase) < 0
                && c.IndexOf("Data Model Operation", StringComparison.OrdinalIgnoreCase) < 0
                && c.IndexOf("[Now]", StringComparison.OrdinalIgnoreCase) < 0)
            {
                // Prefer known markers; still accept long markdown that lacks them only if substantial.
                if (c.Length < 2000) return false;
            }
            return true;
        }

        /// <summary>
        /// True when a <c>file_read</c> JSON result carries the full commands-knowledge body
        /// (not the AlreadyLoaded stub).
        /// </summary>
        public static bool LooksLikeFullKbFileReadResult(string toolName, string resultJson)
        {
            if (!string.Equals(toolName, "file_read", StringComparison.OrdinalIgnoreCase)) return false;
            if (string.IsNullOrEmpty(resultJson)) return false;
            if (resultJson.IndexOf(KbFileName, StringComparison.OrdinalIgnoreCase) < 0) return false;
            // AlreadyLoaded stub (Content=null) — not a full body; do not exempt from CapResult.
            if (resultJson.IndexOf("\"AlreadyLoaded\"", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (resultJson.IndexOf("this file is disabled", StringComparison.OrdinalIgnoreCase) >= 0
                && resultJson.Length < 500)
                return false;
            return resultJson.IndexOf("\"Content\"", StringComparison.OrdinalIgnoreCase) >= 0
                   && resultJson.Length >= 2000;
        }

        /// <summary>JSON error for upsert_* when KB was not file_read yet this chat.</summary>
        public static string RequireLoadedOrError(string chatSessionKey)
        {
            if (HasLoaded(chatSessionKey)) return null;
            return "COMMANDS_KB_REQUIRED: Call file_read path=source/commands-knowledge.md once in this chat before upsert_command / upsert_data_load / upsert_data_transfer / upsert_message_template / upsert_linked_search. Do not invent Operation rules from memory. After a successful read, do not file_read the same KB again in this chat.";
        }
    }
}
