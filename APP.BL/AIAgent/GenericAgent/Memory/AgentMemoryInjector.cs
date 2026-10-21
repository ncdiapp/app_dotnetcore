using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    /// <summary>
    /// Memory blocks appended to the system prompt at the start of a run (agents with the InjectMemory flag).
    /// L2: all confirmed facts, capped. L3: top similar past conversations for the new message, if any pass
    /// the similarity threshold. Code decides; the model is never asked whether to look.
    /// </summary>
    internal static class AgentMemoryInjector
    {
        public const int InjectMemoryFlag = 16;

        private const int FactsMaxChars  = 1500;
        private const int EpisodesTop    = 3;
        private const int MinQueryChars  = 12;
        private const int ShortFollowUp  = 40;

        private static readonly Regex Trivial = new(
            @"^\s*(hi|hello|hey|thanks|thank you|ok|okay|yes|no|sure|continue|go on|\[[a-z_]+\])[\s\.\!\?]*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public const string MemoryLibraryKey = "platform-memory";

        public static bool IsEnabled(int capabilityFlags) => (capabilityFlags & InjectMemoryFlag) != 0;

        /// <summary>True when the agent's enabled tools include the platform-memory library's tools.</summary>
        public static bool HasMemoryTools(IEnumerable<string> toolNames) =>
            toolNames.Any(n => string.Equals(n, "remember_fact", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(n, "recall_memory", StringComparison.OrdinalIgnoreCase));

        public static async Task<string> BuildAsync(
            AppClientIdentity identity, string skillKey, int userId, int dataSourceId,
            string userMessage, List<JObject> chatHistory, CancellationToken ct)
        {
            if (userId <= 0) return "";
            var log   = NLog.LogManager.GetCurrentClassLogger();
            var facts = AgentMemoryFactBL.BuildPromptBlock(userId, skillKey, dataSourceId, FactsMaxChars);

            var episodes = new List<AgentEpisodeDto>();
            var query    = BuildQuery(userMessage, chatHistory);
            if (query != null)
            {
                var embedding = await EmbeddingClient.EmbedAsync(query, identity, ct).ConfigureAwait(false);
                if (embedding != null)
                    episodes = AgentEpisodeBL.Recall(userId, skillKey, dataSourceId, embedding, EpisodesTop);
            }

            log.Info("[Memory] skill={0} user={1} factsChars={2} episodes={3}{4}", skillKey, userId, facts.Length, episodes.Count,
                episodes.Count > 0 ? " ids=" + string.Join(",", episodes.Select(e => $"{e.EpisodeId}:{e.Score:0.00}")) : "");
            return facts + AgentEpisodeBL.FormatPromptBlock(episodes);
        }

        /// <summary>Text to embed for recall, or null when the message is too trivial to be worth a lookup.</summary>
        internal static string BuildQuery(string userMessage, List<JObject> chatHistory)
        {
            var msg = (userMessage ?? "").Trim();
            if (msg.Length < MinQueryChars || Trivial.IsMatch(msg)) return null;
            if (msg.Length >= ShortFollowUp) return msg;

            // Short follow-up ("do it again"): add the assistant's previous reply for context.
            var lastAssistant = chatHistory?.LastOrDefault(m =>
                (m["role"]?.ToString() == "assistant" || m["role"]?.ToString() == "model") && m["content"]?.Type == JTokenType.String);
            var prior = lastAssistant?["content"]?.ToString() ?? "";
            if (prior.Length > 300) prior = prior.Substring(0, 300);
            return prior.Length == 0 ? msg : prior + "\n" + msg;
        }
    }
}
