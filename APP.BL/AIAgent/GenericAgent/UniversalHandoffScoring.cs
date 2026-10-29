using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;
using TbSkillSetDto = App.BL.AIAgent.AiSkill.AppAgentSkillSetDto;
using TbToolBL = App.BL.TenantBusiness.AppAgentToolRegisterBL;
using TbMcpBL = App.BL.TenantBusiness.AppAgentMcpServerBL;

namespace App.BL.AIAgent.GenericAgent
{
    internal sealed class SpecialistHandoffCandidate
    {
        public string SkillKey { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Reason { get; set; } = "";
        public int Score { get; set; }
        public int LexicalScore { get; set; }
        public int PhraseScore { get; set; }
        public int ModelScore { get; set; }
    }

    /// <summary>
    /// LLM scores each Specialist against the current General turn using Code, Display,
    /// Description, Tools, and Prompt, plus the scoring rubric. C# only filters by
    /// HandoffMinScore and takes the top HandoffMaxSuggestions.
    /// </summary>
    internal static class UniversalHandoffScoring
    {
        private const int PromptExcerptChars = 1200;
        private const int MaxToolNames = 24;

        public static async Task<List<SpecialistHandoffCandidate>> SelectAsync(
            int dataSourceId,
            string userMessage,
            List<JObject> chatHistory,
            Kernel kernel,
            CancellationToken ct)
        {
            var empty = new List<SpecialistHandoffCandidate>();
            if (string.IsNullOrWhiteSpace(userMessage)) return empty;
            if (string.Equals(userMessage.Trim(), "[session_start]", StringComparison.OrdinalIgnoreCase))
                return empty;
            if (userMessage.IndexOf(GeneralAgentConstants.StayInGeneralMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                return empty;

            List<TbSkillSetDto> agents;
            try
            {
                agents = dataSourceId > 0
                    ? App.BL.AIAgent.AiSkill.AppAgentSkillSetBL.GetAll(dataSourceId)
                    : App.BL.AIAgent.AiSkill.AppAgentSkillSetBL.GetAll();
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal specialist catalog load failed");
                return empty;
            }

            var catalog = (agents ?? new List<TbSkillSetDto>())
                .Where(a => a != null && !GeneralAgentConstants.IsGeneralAgentKey(a.SkillKey))
                .Take(30)
                .ToList();
            if (catalog.Count == 0) return empty;

            var scored = await ScoreWithLlmAsync(dataSourceId, catalog, userMessage, chatHistory, kernel, ct)
                .ConfigureAwait(false);
            if (scored.Count == 0)
                scored = FallbackByName(catalog, userMessage);

            return scored
                .Where(x => x.Score >= GeneralAgentConstants.HandoffMinScore)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Take(GeneralAgentConstants.HandoffMaxSuggestions)
                .ToList();
        }

        public static JObject ToProposalPayload(SpecialistHandoffCandidate c)
        {
            return new JObject
            {
                ["targetSkillKey"] = c.SkillKey,
                ["name"] = c.Name,
                ["reason"] = c.Reason,
                ["description"] = c.Description ?? "",
                ["score"] = c.Score,
                ["lexicalScore"] = c.LexicalScore,
                ["phraseScore"] = c.PhraseScore,
                ["modelScore"] = c.ModelScore
            };
        }

        private static async Task<List<SpecialistHandoffCandidate>> ScoreWithLlmAsync(
            int dataSourceId,
            List<TbSkillSetDto> catalog,
            string userMessage,
            List<JObject> chatHistory,
            Kernel kernel,
            CancellationToken ct)
        {
            var result = new List<SpecialistHandoffCandidate>();
            if (kernel == null) return result;
            try
            {
                var chat = kernel.GetRequiredService<IChatCompletionService>();
                var prompt = new ChatHistory();
                prompt.AddSystemMessage(BuildRubric());
                prompt.AddUserMessage(
                    "Specialist agents:\n" + BuildCatalogForLlm(dataSourceId, catalog) +
                    "\n\nRecent chat:\n" + BuildRecentPlain(chatHistory, 6) +
                    "\n\nCurrent user message:\n" + (userMessage ?? ""));
                var raw = await chat.GetChatMessageContentAsync(prompt, cancellationToken: ct).ConfigureAwait(false);
                var parsed = ParseModelScores(raw?.Content, catalog);
                foreach (var agent in catalog)
                {
                    if (!parsed.TryGetValue(agent.SkillKey, out var hit)) continue;
                    var name = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
                    result.Add(new SpecialistHandoffCandidate
                    {
                        SkillKey = agent.SkillKey,
                        Name = name,
                        Description = agent.Description ?? "",
                        Reason = string.IsNullOrWhiteSpace(hit.Reason) ? TrimTo(agent.Description, 220) : hit.Reason,
                        Score = hit.Score,
                        ModelScore = hit.Score
                    });
                }
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal specialist score model failed");
            }
            return result;
        }

        private static string BuildRubric()
        {
            return
                "You score Specialist Agents for a General Agent triage step. " +
                "Use the current user message, recent chat, and each specialist's Code, Display, Description, Tools, and Prompt. " +
                "Score 0-100 how well THAT specialist should own the next step.\n" +
                "90-100: user named this agent, or its tools/prompt/description are the specific capability asked for.\n" +
                GeneralAgentConstants.HandoffMinScore + "-89: same job/domain; this specialist is a better entry than a generic helper.\n" +
                "40-69: only loosely related.\n" +
                "0-39: not a fit.\n" +
                "Rules:\n" +
                "- Judge only the specialist's Code, Display, Description, Tools, and Prompt against the user request.\n" +
                "- Do not assume product or domain synonyms. Distinct specialists stay distinct unless the user's words match that specialist's own settings.\n" +
                "- A specialist that can technically query or search data is not a match unless those settings describe this request.\n" +
                "- Overlap with General Agent tools is not a reason to raise a specialist's score.\n" +
                "- Score every listed skillKey. Do not invent keys.\n" +
                "Return only JSON: {\"scores\":[{\"skillKey\":\"x\",\"score\":82,\"reason\":\"one short sentence\"}]}.";
        }

        private static string BuildCatalogForLlm(int dataSourceId, List<TbSkillSetDto> catalog)
        {
            var sb = new StringBuilder();
            foreach (var a in catalog)
            {
                sb.AppendLine("### Specialist");
                sb.AppendLine("Code: " + (a.SkillKey ?? ""));
                sb.AppendLine("Display: " + (a.DisplayName ?? a.SkillKey ?? ""));
                sb.AppendLine("Description: " + TrimTo(a.Description, 400));
                sb.AppendLine("Tools: " + LoadToolSummary(dataSourceId, a.SkillKey));
                sb.AppendLine("Prompt: " + TrimTo(a.SystemPrompt, PromptExcerptChars));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string LoadToolSummary(int dataSourceId, string skillKey)
        {
            var names = new List<string>();
            try
            {
                var tools = dataSourceId > 0
                    ? TbToolBL.GetBySkillKeyWithLibraries(skillKey, dataSourceId)
                    : TbToolBL.GetBySkillKeyWithLibraries(skillKey);
                foreach (var t in tools ?? new List<App.BL.TenantBusiness.AppAgentToolRegisterDto>())
                {
                    if (t == null || string.IsNullOrWhiteSpace(t.ToolName)) continue;
                    names.Add(t.ToolName.Trim());
                    if (names.Count >= MaxToolNames) break;
                }
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal specialist tools for score");
            }
            try
            {
                var mcps = dataSourceId > 0
                    ? TbMcpBL.GetBySkillKeyWithLibraries(skillKey, dataSourceId)
                    : TbMcpBL.GetBySkillKeyWithLibraries(skillKey);
                foreach (var m in mcps ?? new List<App.BL.TenantBusiness.AppAgentMcpServerDto>())
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.ServerName)) continue;
                    var label = "mcp:" + m.ServerName.Trim();
                    if (!names.Contains(label, StringComparer.OrdinalIgnoreCase))
                        names.Add(label);
                    if (names.Count >= MaxToolNames) break;
                }
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal specialist MCP for score");
            }
            return names.Count == 0 ? "(none listed)" : string.Join(", ", names);
        }

        private sealed class ModelHit
        {
            public int Score;
            public string Reason = "";
        }

        private static Dictionary<string, ModelHit> ParseModelScores(string text, List<TbSkillSetDto> catalog)
        {
            var map = new Dictionary<string, ModelHit>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text) || catalog == null) return map;
            var raw = text.Trim();
            var fence = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (fence.Success) raw = fence.Groups[1].Value.Trim();
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return map;
            JObject obj;
            try { obj = JObject.Parse(raw.Substring(start, end - start + 1)); }
            catch { return map; }
            var arr = obj["scores"] as JArray ?? obj["Scores"] as JArray;
            if (arr == null) return map;
            var allowed = new HashSet<string>(catalog.Select(a => a.SkillKey), StringComparer.OrdinalIgnoreCase);
            foreach (var token in arr.OfType<JObject>())
            {
                var key = (token.Value<string>("skillKey") ?? token.Value<string>("SkillKey") ?? "").Trim();
                if (!allowed.Contains(key)) continue;
                var scoreToken = token["score"] ?? token["Score"];
                int score;
                if (scoreToken == null || !int.TryParse(Convert.ToString(scoreToken, CultureInfo.InvariantCulture), out score))
                    continue;
                var reason = (token.Value<string>("reason") ?? token.Value<string>("Reason") ?? "").Trim();
                map[key] = new ModelHit { Score = Clamp(score), Reason = TrimTo(reason, 220) };
            }
            return map;
        }

        private static List<SpecialistHandoffCandidate> FallbackByName(List<TbSkillSetDto> catalog, string userMessage)
        {
            var hay = (userMessage ?? "").ToLowerInvariant();
            var list = new List<SpecialistHandoffCandidate>();
            foreach (var agent in catalog)
            {
                var name = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
                var score = 0;
                if (name.Length >= 3 && hay.IndexOf(name.ToLowerInvariant(), StringComparison.Ordinal) >= 0)
                    score = 92;
                else if ((agent.SkillKey ?? "").Length >= 3 && hay.IndexOf(agent.SkillKey.ToLowerInvariant(), StringComparison.Ordinal) >= 0)
                    score = 90;
                if (score < GeneralAgentConstants.HandoffMinScore) continue;
                list.Add(new SpecialistHandoffCandidate
                {
                    SkillKey = agent.SkillKey,
                    Name = name,
                    Description = agent.Description ?? "",
                    Reason = TrimTo(agent.Description, 220),
                    Score = score,
                    LexicalScore = score
                });
            }
            return list;
        }

        private static int Clamp(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        private static string TrimTo(string text, int max)
        {
            var s = (text ?? "").Trim();
            if (s.Length <= max) return s;
            return s.Substring(0, max);
        }

        private static string BuildRecentPlain(List<JObject> chatHistory, int take)
        {
            if (chatHistory == null || chatHistory.Count == 0) return "";
            return string.Join("\n", chatHistory
                .Where(m => m != null)
                .Skip(Math.Max(0, chatHistory.Count - Math.Max(1, take)))
                .Select(m => (m.Value<string>("role") ?? "") + ": " + Extract(m["content"])));
        }

        private static string Extract(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.String) return token.ToString();
            if (token.Type == JTokenType.Array) return string.Join(" ", token.Select(Extract));
            if (token.Type == JTokenType.Object)
                return token["text"]?.ToString() ?? token["content"]?.ToString() ?? "";
            return token.ToString();
        }
    }
}
