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
using CatalogBL = App.BL.TenantBusiness.AppAgentToolCatalogBL;

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
        private const int MaxMcpApiNames = 40;

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

            var mcpApis = await LoadMcpApisByAgentAsync(dataSourceId, catalog, ct).ConfigureAwait(false);
            var scored = await ScoreWithLlmAsync(dataSourceId, catalog, mcpApis, userMessage, chatHistory, kernel, ct)
                .ConfigureAwait(false);
            scored = MergeScores(scored, FallbackByName(catalog, userMessage));
            scored = MergeScores(scored, FallbackByCatalog(dataSourceId, catalog, userMessage, chatHistory));
            scored = ApplyMcpApiBoost(scored, catalog, mcpApis, userMessage);

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
            Dictionary<string, List<string>> mcpApis,
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
                    "Specialist agents:\n" + BuildCatalogForLlm(dataSourceId, catalog, mcpApis) +
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
                "- Judge only the specialist's Code, Display, Description, Tools, MCP APIs, and Prompt against the user request.\n" +
                "- Do not assume product or domain synonyms. Distinct specialists stay distinct unless the user's words match that specialist's own settings.\n" +
                "- If an MCP API name (split on _ or camel case) matches the user's words, score that specialist 90-100. " +
                "An API like Product_ForwardTimeline matches a request for product forward timeline.\n" +
                "- A specialist that can technically query or search data is not a match unless those settings or MCP APIs describe this request.\n" +
                "- Overlap with General Agent tools is not a reason to raise a specialist's score.\n" +
                "- If the user asks for the SQL, query, or statement that produced the current Result/card view, score a specialist whose tools or prompt are SQL/database work at 88-95. Do not leave every specialist below threshold just because General could recap the chat.\n" +
                "- Score every listed skillKey. Do not invent keys.\n" +
                "Return only JSON: {\"scores\":[{\"skillKey\":\"x\",\"score\":82,\"reason\":\"one short sentence\"}]}.";
        }

        private static string BuildCatalogForLlm(
            int dataSourceId,
            List<TbSkillSetDto> catalog,
            Dictionary<string, List<string>> mcpApis)
        {
            var sb = new StringBuilder();
            foreach (var a in catalog)
            {
                sb.AppendLine("### Specialist");
                sb.AppendLine("Code: " + (a.SkillKey ?? ""));
                sb.AppendLine("Display: " + (a.DisplayName ?? a.SkillKey ?? ""));
                sb.AppendLine("Description: " + TrimTo(a.Description, 400));
                sb.AppendLine("Tools: " + LoadToolSummary(dataSourceId, a.SkillKey));
                var apis = mcpApis != null && mcpApis.TryGetValue(a.SkillKey, out var list) ? list : null;
                sb.AppendLine("MCP APIs: " + (
                    apis == null || apis.Count == 0
                        ? "(none listed)"
                        : string.Join(", ", apis.Take(MaxMcpApiNames))));
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
                foreach (var srv in LoadMcpServers(dataSourceId, skillKey))
                {
                    if (srv == null || string.IsNullOrWhiteSpace(srv.ServerName)) continue;
                    var label = "mcp:" + srv.ServerName.Trim();
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

        private static List<App.BL.TenantBusiness.AppAgentMcpServerDto> LoadMcpServers(int dataSourceId, string skillKey)
        {
            var list = new List<App.BL.TenantBusiness.AppAgentMcpServerDto>();
            var seen = new HashSet<int>();
            void Add(IEnumerable<App.BL.TenantBusiness.AppAgentMcpServerDto> rows)
            {
                foreach (var m in rows ?? new List<App.BL.TenantBusiness.AppAgentMcpServerDto>())
                {
                    if (m == null || seen.Contains(m.McpServerId)) continue;
                    seen.Add(m.McpServerId);
                    list.Add(m);
                }
            }
            try
            {
                Add(dataSourceId > 0
                    ? TbMcpBL.GetBySkillKey(skillKey, dataSourceId)
                    : TbMcpBL.GetBySkillKey(skillKey));
            }
            catch (Exception ex) { SwallowLog.Write(ex, "Universal specialist MCP by skill"); }
            try
            {
                Add(dataSourceId > 0
                    ? TbMcpBL.GetBySkillKeyWithLibraries(skillKey, dataSourceId)
                    : TbMcpBL.GetBySkillKeyWithLibraries(skillKey));
            }
            catch (Exception ex) { SwallowLog.Write(ex, "Universal specialist MCP by library"); }
            return list;
        }

        private static async Task<Dictionary<string, List<string>>> LoadMcpApisByAgentAsync(
            int dataSourceId,
            List<TbSkillSetDto> catalog,
            CancellationToken ct)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (catalog == null) return map;
            List<App.BL.TenantBusiness.AppAgentToolCatalogDto> catalogRows;
            try
            {
                catalogRows = dataSourceId > 0
                    ? CatalogBL.GetAll(dataSourceId)
                    : new List<App.BL.TenantBusiness.AppAgentToolCatalogDto>();
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal specialist MCP catalog");
                catalogRows = new List<App.BL.TenantBusiness.AppAgentToolCatalogDto>();
            }

            foreach (var agent in catalog)
            {
                var servers = LoadMcpServers(dataSourceId, agent.SkillKey);
                var names = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var srv in servers)
                {
                    var fromDb = catalogRows
                        .Where(t => t.McpServerId == srv.McpServerId && !string.IsNullOrWhiteSpace(t.ToolName))
                        .Select(t => t.ToolName.Trim())
                        .ToList();
                    if (fromDb.Count == 0)
                    {
                        try
                        {
                            var live = await McpConnectionHelper.ListCatalogToolsAsync(srv, ct).ConfigureAwait(false);
                            if (live.Tools != null)
                                fromDb = live.Tools
                                    .Where(t => !string.IsNullOrWhiteSpace(t.ToolName))
                                    .Select(t => t.ToolName.Trim())
                                    .ToList();
                        }
                        catch (Exception ex)
                        {
                            SwallowLog.Write(ex, "Universal specialist live MCP tools");
                        }
                    }
                    foreach (var n in fromDb)
                    {
                        if (seen.Add(n)) names.Add(n);
                        if (names.Count >= MaxMcpApiNames) break;
                    }
                    if (names.Count >= MaxMcpApiNames) break;
                }
                map[agent.SkillKey] = names;
            }
            return map;
        }

        private static List<SpecialistHandoffCandidate> ApplyMcpApiBoost(
            List<SpecialistHandoffCandidate> scored,
            List<TbSkillSetDto> catalog,
            Dictionary<string, List<string>> mcpApis,
            string userMessage)
        {
            var byKey = (scored ?? new List<SpecialistHandoffCandidate>())
                .ToDictionary(c => c.SkillKey, c => c, StringComparer.OrdinalIgnoreCase);
            foreach (var agent in catalog)
            {
                mcpApis.TryGetValue(agent.SkillKey, out var apis);
                var hit = BestMcpApiMatch(userMessage, apis);
                if (hit.Score < GeneralAgentConstants.HandoffMinScore) continue;
                var name = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
                if (!byKey.TryGetValue(agent.SkillKey, out var row))
                {
                    row = new SpecialistHandoffCandidate
                    {
                        SkillKey = agent.SkillKey,
                        Name = name,
                        Description = agent.Description ?? "",
                        Score = hit.Score,
                        PhraseScore = hit.Score,
                        Reason = hit.Reason
                    };
                    byKey[agent.SkillKey] = row;
                }
                else if (hit.Score > row.Score)
                {
                    row.Score = hit.Score;
                    row.PhraseScore = hit.Score;
                    row.Reason = hit.Reason;
                }
                else
                    row.PhraseScore = Math.Max(row.PhraseScore, hit.Score);
            }
            return byKey.Values.ToList();
        }

        private static (int Score, string Reason) BestMcpApiMatch(string userMessage, List<string> apiNames)
        {
            if (string.IsNullOrWhiteSpace(userMessage) || apiNames == null || apiNames.Count == 0)
                return (0, "");
            var hay = " " + NormalizeWords(userMessage) + " ";
            var best = 0;
            var bestName = "";
            foreach (var api in apiNames)
            {
                var parts = SplitApiName(api);
                if (parts.Count < 2) continue;
                var hits = parts.Count(p => hay.IndexOf(" " + p + " ", StringComparison.Ordinal) >= 0);
                if (hits < 2) continue;
                var score = 0;
                for (var n = Math.Min(3, parts.Count); n >= 2; n--)
                {
                    for (var i = 0; i + n <= parts.Count; i++)
                    {
                        var phrase = " " + string.Join(" ", parts.Skip(i).Take(n)) + " ";
                        if (hay.IndexOf(phrase, StringComparison.Ordinal) < 0) continue;
                        score = Math.Max(score, n >= 3 ? 96 : 93);
                    }
                }
                if (score == 0 && hits == parts.Count)
                    score = 94;
                else if (score == 0 && hits * 2 >= parts.Count)
                    score = 88;
                if (score > best)
                {
                    best = score;
                    bestName = api;
                }
            }
            if (best <= 0) return (0, "");
            return (best, "MCP API " + bestName + " matches this request.");
        }

        private static List<string> SplitApiName(string name)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(name)) return list;
            var spaced = Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2");
            spaced = Regex.Replace(spaced, @"[_\-./]+", " ");
            foreach (var p in Regex.Split(spaced.ToLowerInvariant(), @"[^a-z0-9]+"))
            {
                if (p.Length < 3) continue;
                if (p == "get" || p == "set" || p == "list" || p == "find" || p == "search"
                    || p == "query" || p == "tool" || p == "api" || p == "data")
                    continue;
                list.Add(p);
            }
            return list;
        }

        private static string NormalizeWords(string text)
        {
            var s = Regex.Replace((text ?? "").ToLowerInvariant(), @"[^a-z0-9\u4e00-\u9fff]+", " ");
            return Regex.Replace(s, @"\s+", " ").Trim();
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

        private static List<SpecialistHandoffCandidate> MergeScores(
            List<SpecialistHandoffCandidate> primary,
            List<SpecialistHandoffCandidate> extra)
        {
            var byKey = (primary ?? new List<SpecialistHandoffCandidate>())
                .ToDictionary(c => c.SkillKey, c => c, StringComparer.OrdinalIgnoreCase);
            foreach (var row in extra ?? new List<SpecialistHandoffCandidate>())
            {
                if (row == null || string.IsNullOrWhiteSpace(row.SkillKey)) continue;
                if (!byKey.TryGetValue(row.SkillKey, out var cur) || row.Score > cur.Score)
                    byKey[row.SkillKey] = row;
            }
            return byKey.Values.ToList();
        }

        private static List<SpecialistHandoffCandidate> FallbackByCatalog(
            int dataSourceId,
            List<TbSkillSetDto> catalog,
            string userMessage,
            List<JObject> chatHistory)
        {
            var list = new List<SpecialistHandoffCandidate>();
            var hay = NormalizeWords(userMessage + " " + BuildRecentPlain(chatHistory, 6));
            if (string.IsNullOrWhiteSpace(hay)) return list;
            var wantsSql = hay.IndexOf(" query ", StringComparison.Ordinal) >= 0
                || hay.IndexOf(" sql ", StringComparison.Ordinal) >= 0
                || hay.IndexOf(" select ", StringComparison.Ordinal) >= 0
                || hay.IndexOf("query", StringComparison.Ordinal) >= 0
                || hay.IndexOf("sql", StringComparison.Ordinal) >= 0;

            foreach (var agent in catalog)
            {
                var name = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
                var tools = LoadToolSummary(dataSourceId, agent.SkillKey) ?? "";
                var blob = NormalizeWords(
                    (agent.SkillKey ?? "") + " " + name + " " + (agent.Description ?? "") + " " + tools);
                if (string.IsNullOrWhiteSpace(blob)) continue;

                var score = 0;
                var reason = "";
                if (wantsSql
                    && (blob.IndexOf("sql", StringComparison.Ordinal) >= 0
                        || blob.IndexOf("database", StringComparison.Ordinal) >= 0
                        || tools.IndexOf("execute_sql", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    score = 88;
                    reason = "This request is about the SQL/query behind the current result.";
                }

                var userParts = hay.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var hits = 0;
                foreach (var p in userParts)
                {
                    if (p.Length < 4) continue;
                    if (blob.IndexOf(p, StringComparison.Ordinal) >= 0) hits++;
                }
                if (hits >= 2)
                    score = Math.Max(score, 86);

                if (score < GeneralAgentConstants.HandoffMinScore) continue;
                list.Add(new SpecialistHandoffCandidate
                {
                    SkillKey = agent.SkillKey,
                    Name = name,
                    Description = agent.Description ?? "",
                    Reason = string.IsNullOrWhiteSpace(reason) ? TrimTo(agent.Description, 220) : reason,
                    Score = score,
                    LexicalScore = score
                });
            }
            return list;
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
