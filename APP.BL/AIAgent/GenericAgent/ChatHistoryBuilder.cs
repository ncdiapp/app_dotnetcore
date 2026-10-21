#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Layer 1 (working memory): turns the stored conversation into the ChatHistory sent to the model.
    /// History is sent verbatim until its estimated size passes SummarizeThreshold. After that a rolling
    /// summary covers the oldest turns and the verbatim tail stays between RecentWindowSize and
    /// 2 x RecentWindowSize turns, so the summary is refreshed about once per window, not every run.
    /// </summary>
    internal static class ChatHistoryBuilder
    {
        private const int DefaultWindow    = 10;
        private const int DefaultThreshold = 60000;
        private const int CharsPerToken    = 4;

        /// <param name="stored">Summary saved by an earlier run, if any.</param>
        /// <param name="persist">Called with the new summary when it changed.</param>
        public static async Task<ChatHistory> BuildAsync(
            List<JObject>? messages, int recentWindowSize, int summarizeThreshold, Kernel kernel,
            RollingSummary? stored = null, Action<RollingSummary>? persist = null)
        {
            var window    = recentWindowSize > 0 ? recentWindowSize : DefaultWindow;
            var threshold = summarizeThreshold > 0 ? summarizeThreshold : DefaultThreshold;
            var turns     = ToTextTurns(messages);
            var history   = new ChatHistory();
            if (turns.Count == 0) return history;

            var valid = IsUsable(stored, turns) ? stored : null;

            if (valid == null && (turns.Count <= window || EstimateTokens(turns) <= threshold))
            {
                AddTurns(history, turns);
                return history;
            }

            string summaryText;
            int    covered;
            if (valid != null && turns.Count - valid.TurnCount <= window * 2)
            {
                summaryText = valid.Text;
                covered     = valid.TurnCount;
            }
            else
            {
                covered = Math.Max(0, turns.Count - window);
                var from      = valid?.TurnCount ?? 0;
                var newlyOld  = turns.Skip(from).Take(covered - from).ToList();
                var text      = newlyOld.Count == 0 && valid != null
                    ? valid.Text
                    : await SummarizeAsync(valid?.Text, newlyOld, kernel).ConfigureAwait(false);

                if (string.IsNullOrEmpty(text))
                {
                    // Summary failed: keep what we had, and fall back to the verbatim recent window.
                    summaryText = valid?.Text ?? "";
                    covered     = valid?.TurnCount ?? covered;
                }
                else
                {
                    summaryText = text;
                    persist?.Invoke(new RollingSummary
                    {
                        Text = text, TurnCount = covered, Fingerprint = Fingerprint(turns[0].Text)
                    });
                }
            }

            if (!string.IsNullOrEmpty(summaryText))
                history.AddSystemMessage($"[Earlier conversation summary: {summaryText}]");
            AddTurns(history, turns.Skip(covered).ToList());
            return history;
        }

        private static bool IsUsable(RollingSummary? s, List<(string Role, string Text)> turns) =>
            s != null
            && !string.IsNullOrWhiteSpace(s.Text)
            && s.TurnCount > 0
            && s.TurnCount <= turns.Count
            && string.Equals(s.Fingerprint, Fingerprint(turns[0].Text), StringComparison.Ordinal);

        internal static string Fingerprint(string firstTurnText)
        {
            var head = firstTurnText.Length > 200 ? firstTurnText.Substring(0, 200) : firstTurnText;
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(head))).Substring(0, 16);
        }

        /// <summary>
        /// Plain-text user/assistant turns only; array-content turns and tool roles are skipped.
        /// An assistant turn that ran tools keeps a compact note of them (from its stored toolSteps), so the
        /// model still knows next turn which tools were called and what they returned.
        /// </summary>
        internal static List<(string Role, string Text)> ToTextTurns(List<JObject>? messages)
        {
            var turns = new List<(string, string)>();
            if (messages == null) return turns;
            foreach (var msg in messages)
            {
                var role    = msg["role"]?.ToString() ?? "user";
                var content = msg["content"];
                if (content == null || content.Type == JTokenType.Array) continue;
                var text = content.ToString();
                if (role == "user")
                {
                    if (!string.IsNullOrEmpty(text)) turns.Add(("user", text));
                }
                else if (role == "assistant" || role == "model")
                {
                    var note = ToolNote(msg["toolSteps"] as JArray);
                    if (text.Length == 0 && note.Length == 0) continue;
                    turns.Add(("assistant", note.Length == 0 ? text : (text.Length == 0 ? note : text + "\n" + note)));
                }
            }
            return turns;
        }

        private const int MaxToolNoteSteps = 6;
        private const int MaxToolNoteChars = 800;

        internal static string ToolNote(JArray? steps)
        {
            if (steps == null || steps.Count == 0) return "";
            var sb = new StringBuilder("[Tools used this turn:");
            foreach (var step in steps.OfType<JObject>().Take(MaxToolNoteSteps))
            {
                var name = step.Value<string>("toolName");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var ok     = step["isSuccess"]?.Type == JTokenType.Boolean && !step.Value<bool>("isSuccess") ? "failed" : "ok";
                var args   = Squash(step["args"]?.ToString(), 100);
                var result = Squash(step["result"]?.ToString(), 160);
                var line   = $" {name}({args}) {ok}" + (result.Length > 0 ? ": " + result : "") + ";";
                if (sb.Length + line.Length > MaxToolNoteChars) break;
                sb.Append(line);
            }
            if (sb.Length == "[Tools used this turn:".Length) return "";
            return sb.Append(']').ToString();
        }

        private static string Squash(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var s = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        private static int EstimateTokens(List<(string Role, string Text)> turns)
            => (int)(turns.Sum(t => (long)t.Text.Length) / CharsPerToken);

        private static void AddTurns(ChatHistory history, List<(string Role, string Text)> turns)
        {
            foreach (var (role, text) in turns)
            {
                if (role == "user") history.AddUserMessage(text);
                else history.AddAssistantMessage(text);
            }
        }

        private static async Task<string?> SummarizeAsync(
            string? previousSummary, List<(string Role, string Text)> turns, Kernel kernel)
        {
            try
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(previousSummary))
                    sb.AppendLine("Existing summary of the earlier conversation:\n" + previousSummary + "\n\nNew turns to fold in:");
                foreach (var (role, text) in turns)
                    sb.AppendLine($"{role}: {text}");

                var prompt = "Summarize the following conversation in 3-5 sentences, preserving key facts, user goals, identifiers (IDs, names, files) and any decisions made. If an existing summary is given, merge the new turns into it:\n\n" + sb;
                var chat = kernel.GetRequiredService<IChatCompletionService>();
                var h = new ChatHistory();
                h.AddUserMessage(prompt);
                var result = await chat.GetChatMessageContentAsync(h).ConfigureAwait(false);
                return result.Content;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "ChatHistoryBuilder summary failed — no summary injected");
                return null;
            }
        }
    }
}
