#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    public sealed class MemoryExtractionOutcome
    {
        public bool         Ok            { get; set; } = true;
        public string       Error         { get; set; } = "";
        /// <summary>Text turns in the chat at extraction time; the caller stores it as ExtractedTurnCount.</summary>
        public int          TurnCount     { get; set; }
        public int          FactsProposed { get; set; }
        public int          FactsAdded    { get; set; }
        public int          FactsUpdated  { get; set; }
        public int          FactsSkipped  { get; set; }
        public bool         EpisodeStored { get; set; }
        public List<string> Dropped       { get; } = new();

        public override string ToString() =>
            $"ok={Ok} turns={TurnCount} facts proposed={FactsProposed} added={FactsAdded} updated={FactsUpdated} skipped={FactsSkipped} episode={EpisodeStored}"
            + (Dropped.Count > 0 ? " dropped=[" + string.Join("; ", Dropped) + "]" : "")
            + (Ok ? "" : " error=" + Error);
    }

    /// <summary>
    /// Distils one chat into L2 facts and an L3 episode with a single LLM call.
    /// Facts from here are scoped to the agent (SkillKey) and start as pending unless the user said "remember".
    /// </summary>
    public static class AgentMemoryExtractor
    {
        public static async Task<MemoryExtractionOutcome> ExtractAsync(
            AppClientIdentity identity, string skillKey, int userId, string sessionKey,
            List<JObject> messages, int processedTurns, int minNewTurns, int dataSourceId,
            bool dryRun, CancellationToken ct)
        {
            var outcome = new MemoryExtractionOutcome();
            var turns   = ChatHistoryBuilder.ToTextTurns(messages);
            outcome.TurnCount = turns.Count;

            var from     = processedTurns > turns.Count ? 0 : processedTurns;
            var newTurns = turns.Skip(from).ToList();
            if (newTurns.Count < Math.Max(1, minNewTurns)) return outcome;

            try
            {
                var existing = AgentMemoryFactBL.List(userId, skillKey, dataSourceId);
                var prompt   = MemoryExtractionParser.BuildPrompt(newTurns, existing);

                var chat    = KernelFactory.CreateChatService(identity);
                var history = new ChatHistory();
                history.AddUserMessage(prompt);
                var reply   = await chat.GetChatMessageContentAsync(history, cancellationToken: ct).ConfigureAwait(false);

                var userTexts = newTurns.Where(t => t.Role == "user").Select(t => t.Text);
                var parsed    = MemoryExtractionParser.Parse(reply?.Content ?? "", userTexts);
                outcome.Dropped.AddRange(parsed.Dropped);
                if (!parsed.ParsedOk)
                {
                    outcome.Ok    = false;
                    outcome.Error = "model response could not be parsed";
                    return outcome;
                }

                outcome.FactsProposed = parsed.Facts.Count;
                if (dryRun) return outcome;

                foreach (var fact in parsed.Facts)
                {
                    var result = AgentMemoryFactBL.Upsert(
                        userId, skillKey, fact.Key, fact.Value, fact.Category,
                        fact.Explicit ? "confirmed" : "pending", "extracted", fact.Confidence, sessionKey, dataSourceId);
                    switch (result)
                    {
                        case FactWriteResult.Added:   outcome.FactsAdded++;   break;
                        case FactWriteResult.Updated: outcome.FactsUpdated++; break;
                        case FactWriteResult.Invalid: outcome.Dropped.Add($"fact {fact.Key}: rejected on save"); break;
                        default:                      outcome.FactsSkipped++; break;
                    }
                }

                if (parsed.EpisodeSummary.Length > 0)
                {
                    var embedding = await EmbeddingClient.EmbedAsync(
                        parsed.EpisodeSummary + (parsed.Topics.Length > 0 ? "\nTopics: " + parsed.Topics : ""), identity, ct)
                        .ConfigureAwait(false);
                    outcome.EpisodeStored = AgentEpisodeBL.Insert(
                        userId, skillKey, sessionKey, parsed.EpisodeSummary, parsed.Topics, embedding, dataSourceId);
                }
                return outcome;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "AgentMemoryExtractor skill={0} session={1}", skillKey, sessionKey);
                outcome.Ok    = false;
                outcome.Error = ex.Message;
                return outcome;
            }
        }
    }
}
