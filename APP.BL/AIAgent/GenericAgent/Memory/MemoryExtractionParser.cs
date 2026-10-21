using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    public sealed class ExtractedFact
    {
        public string Key        { get; init; } = "";
        public string Value      { get; init; } = "";
        public string Category   { get; init; } = "note";
        public string Evidence   { get; init; } = "";
        public double Confidence { get; init; }
        /// <summary>True when the user's own words asked to remember it; such facts need no confirmation.</summary>
        public bool   Explicit   { get; init; }
    }

    public sealed class MemoryExtractionResult
    {
        public bool                ParsedOk       { get; set; }
        public string              EpisodeSummary { get; set; } = "";
        public string              Topics         { get; set; } = "";
        public List<ExtractedFact> Facts          { get; } = new();
        /// <summary>Why each proposal was dropped; logged so the prompt can be tuned.</summary>
        public List<string>        Dropped        { get; } = new();
    }

    /// <summary>
    /// Builds the extraction prompt and validates the model's JSON. The model is not trusted:
    /// every fact must quote the user verbatim and pass the secret scan and size limits.
    /// </summary>
    public static class MemoryExtractionParser
    {
        private const int MaxTranscriptChars = 20000;
        private const int MaxEpisodeChars    = 2000;
        private const double MinConfidence   = 0.5;

        private static readonly Regex ExplicitRemember =
            new(@"(?i)\b(remember|don'?t forget|keep in mind)\b", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        public static string BuildPrompt(IReadOnlyList<(string Role, string Text)> newTurns, IEnumerable<AgentMemoryFactDto> existingFacts)
        {
            var transcript = new StringBuilder();
            for (var i = newTurns.Count - 1; i >= 0; i--)
            {
                var line = $"{newTurns[i].Role}: {newTurns[i].Text}\n";
                if (transcript.Length + line.Length > MaxTranscriptChars) break;
                transcript.Insert(0, line);
            }

            var known = string.Join("\n", existingFacts.Select(f => $"- {f.FactKey}: {f.FactValue}"));
            return @"You extract long-term memory from a chat between a user and an AI assistant.
Return ONLY a JSON object, no prose and no code fences, in exactly this shape:
{""episode"":{""summary"":""3-5 sentences: the user's goal, what was done, the outcome, open items"",""topics"":[""short topic""],""worth_keeping"":true},
 ""facts"":[{""key"":""snake_case_name"",""value"":""the fact"",""category"":""preference|entity|rule|note"",""evidence"":""exact words copied from a USER message"",""confidence"":0.0}]}

Rules for facts:
- Only stable, reusable things about the user, their systems or their rules (preferences, names/IDs they use, standing rules). Not one-off task details.
- The evidence MUST be copied verbatim from a message written by the user. Never infer facts from the assistant's messages.
- Never include passwords, API keys, tokens or connection strings.
- If a fact below already exists with the same meaning, reuse its key; use a new value only if the user changed it.
- When unsure, leave the fact out. An empty facts array is a good answer.
Rules for the episode: set worth_keeping to false for greetings, trivial questions, or chats with no outcome.

Facts already known:
" + (known.Length == 0 ? "(none)" : known) + @"

Chat transcript:
" + transcript;
        }

        public static MemoryExtractionResult Parse(string llmText, IEnumerable<string> userTexts)
        {
            var result = new MemoryExtractionResult();
            JObject root;
            try
            {
                root = JObject.Parse(StripFences(llmText));
            }
            catch (Exception ex)
            {
                result.Dropped.Add("response is not valid JSON: " + ex.Message);
                return result;
            }
            result.ParsedOk = true;

            var userBlob = Normalize(string.Join("\n", userTexts));

            var episode = root["episode"] as JObject;
            var worth   = episode?["worth_keeping"]?.Type == JTokenType.Boolean ? episode["worth_keeping"].Value<bool>() : true;
            var summary = (episode?["summary"]?.ToString() ?? "").Trim();
            if (!worth) result.Dropped.Add("episode: worth_keeping=false");
            else if (summary.Length == 0) result.Dropped.Add("episode: empty summary");
            else if (MemorySecretScanner.ContainsSecret(summary)) result.Dropped.Add("episode: looks like it contains a secret");
            else
            {
                result.EpisodeSummary = summary.Length > MaxEpisodeChars ? summary.Substring(0, MaxEpisodeChars) : summary;
                var topics = (episode?["topics"] as JArray)?.Select(t => t.ToString().Trim()).Where(t => t.Length > 0) ?? Enumerable.Empty<string>();
                result.Topics = string.Join(", ", topics);
            }

            foreach (var item in (root["facts"] as JArray) ?? new JArray())
            {
                if (item is not JObject f) continue;
                var key      = AgentMemoryFactBL.NormalizeKey(f["key"]?.ToString());
                var value    = (f["value"]?.ToString() ?? "").Trim();
                var evidence = (f["evidence"]?.ToString() ?? "").Trim();
                var conf     = f["confidence"]?.Type is JTokenType.Float or JTokenType.Integer ? f["confidence"].Value<double>() : 0.7;
                var label    = key.Length > 0 ? key : "(no key)";

                if (key.Length == 0 || value.Length == 0) { result.Dropped.Add($"fact {label}: missing key or value"); continue; }
                if (value.Length > AgentMemoryFactBL.MaxValueLength) { result.Dropped.Add($"fact {label}: value too long"); continue; }
                if (conf < MinConfidence) { result.Dropped.Add($"fact {label}: confidence {conf:0.##} below {MinConfidence}"); continue; }
                if (evidence.Length == 0) { result.Dropped.Add($"fact {label}: no evidence quote"); continue; }
                if (!userBlob.Contains(Normalize(evidence))) { result.Dropped.Add($"fact {label}: evidence not found in a user message"); continue; }
                if (MemorySecretScanner.ContainsSecret(value) || MemorySecretScanner.ContainsSecret(evidence))
                { result.Dropped.Add($"fact {label}: looks like it contains a secret"); continue; }

                result.Facts.Add(new ExtractedFact
                {
                    Key        = key,
                    Value      = value,
                    Category   = AgentMemoryFactBL.NormalizeCategory(f["category"]?.ToString()),
                    Evidence   = evidence,
                    Confidence = Math.Min(1.0, conf),
                    Explicit   = ExplicitRemember.IsMatch(evidence)
                });
            }
            return result;
        }

        internal static string Normalize(string text) =>
            Whitespace.Replace((text ?? "").Trim().Trim('"', '\'', '`', '“', '”').ToLowerInvariant(), " ");

        private static string StripFences(string text)
        {
            var t = (text ?? "").Trim();
            if (t.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = t.IndexOf('\n');
                if (firstNewline >= 0) t = t.Substring(firstNewline + 1);
                var lastFence = t.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0) t = t.Substring(0, lastFence);
            }
            var start = t.IndexOf('{');
            var end   = t.LastIndexOf('}');
            return start >= 0 && end > start ? t.Substring(start, end - start + 1) : t;
        }
    }
}
