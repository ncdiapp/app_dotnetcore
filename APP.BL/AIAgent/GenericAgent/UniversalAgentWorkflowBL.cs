using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkillLib = App.BL.AppMgr.AiSkill.AppAISkillBL;
using SkillSetDto = App.BL.AIAgent.AiSkill.AppAgentSkillSetDto;
using AgentCatalogBL = App.BL.AIAgent.AiSkill.AppAgentSkillSetBL;

namespace App.BL.AIAgent.GenericAgent
{
    public sealed class UniversalSkillDraft
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string SkillContent { get; set; } = "";
    }

    public sealed class UniversalSkillSaveResult
    {
        public int SkillId { get; set; }
        public string Name { get; set; } = "";
        public bool Overwritten { get; set; }
    }

    public sealed class UniversalHandoffResult
    {
        public string TargetSkillKey { get; set; } = "";
        public string TargetSessionKey { get; set; } = "";
        public string TargetName { get; set; } = "";
        public string Summary { get; set; } = "";
    }

    /// <summary>
    /// Universal-only skill draft/save and Specialist hand-off.
    /// Skills stay in AppAISkill. Hand-off metadata stays in MessagesJson.
    /// </summary>
    public static class UniversalAgentWorkflowBL
    {
        public const string SourceContextFileName = "_source_context.md";

        public static async Task<UniversalSkillDraft> DraftSkillAsync(
            AppClientIdentity identity,
            string sessionKey,
            CancellationToken ct)
        {
            var evidence = LoadEvidence(identity, sessionKey);
            if (!HasSavableWork(evidence))
                throw new InvalidOperationException("This chat has no completed work to save as a skill.");

            string raw = "";
            try
            {
                raw = await GenericAgentEngine.CompleteOnceAsync(
                    identity,
                    SkillDraftSystemPrompt,
                    evidence.Text,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal skill draft LLM failed");
            }

            var draft = ParseSkillDraft(raw) ?? FallbackSkillDraft(evidence);
            draft.Name = TrimTo(string.IsNullOrWhiteSpace(draft.Name) ? "Saved skill" : draft.Name.Trim(), 80);
            draft.Description = TrimTo((draft.Description ?? "").Trim(), 400);
            draft.SkillContent = FinalizeDraftSkillContent(draft.SkillContent, evidence);
            return draft;
        }

        public static UniversalSkillSaveResult SaveSkill(
            AppClientIdentity identity,
            string sessionKey,
            string name,
            string description,
            string skillContent,
            bool overwriteExisting = false)
        {
            var dsId = ResolveDataSourceId(identity);
            if (dsId <= 0)
                throw new InvalidOperationException("Data source is not available.");
            if (string.IsNullOrWhiteSpace(skillContent))
                throw new InvalidOperationException("Skill instructions are required.");

            var evidence = LoadEvidence(identity, sessionKey);
            var trimmedName = NormalizeSkillName(name);
            var existing = SkillLib.FindSkillByName(dsId, trimmedName);
            if (existing != null && !overwriteExisting)
                throw new InvalidOperationException(
                    "A skill named \"" + existing.Name + "\" already exists. Confirm overwrite to replace its instructions.");

            var descriptionTrim = TrimTo((description ?? "").Trim(), 500);
            var contentTrim = FinalizeSavedSkillContent(skillContent);

            if (existing != null)
            {
                existing.Description = descriptionTrim;
                existing.SkillContent = contentTrim;
                existing.IsActive = true;
                SkillLib.UpdateSkill(dsId, existing);
                UpsertSourceContextRef(dsId, existing.SkillId, evidence.Text);
                return new UniversalSkillSaveResult
                {
                    SkillId = existing.SkillId,
                    Name = existing.Name,
                    Overwritten = true
                };
            }

            var dto = new AppAISkillDto
            {
                Name = trimmedName,
                Description = descriptionTrim,
                SkillContent = contentTrim,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                References = new List<AppAISkillRefDto>()
            };
            var skillId = SkillLib.CreateSkill(dsId, dto);
            if (skillId <= 0)
                throw new InvalidOperationException("Could not save skill.");

            UpsertSourceContextRef(dsId, skillId, evidence.Text);

            return new UniversalSkillSaveResult { SkillId = skillId, Name = trimmedName, Overwritten = false };
        }

        private static string NormalizeSkillName(string name)
        {
            var baseName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Saved skill";
            if (baseName.Length > 200) baseName = baseName.Substring(0, 200).TrimEnd();
            return baseName;
        }

        private static void UpsertSourceContextRef(int dataSourceId, int skillId, string evidenceText)
        {
            if (string.IsNullOrWhiteSpace(evidenceText)) return;
            var body = TrimTo(
                "Audit context for this skill. Not instructions.\n\n" + evidenceText,
                30000);
            try
            {
                var refs = SkillLib.GetRefsBySkillId(dataSourceId, skillId);
                var existing = refs?.FirstOrDefault(r =>
                    string.Equals((r.FileName ?? "").Trim(), SourceContextFileName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.FileContent = body;
                    existing.SortOrder = 1000;
                    SkillLib.UpdateRef(dataSourceId, existing);
                    return;
                }
                SkillLib.CreateRef(dataSourceId, new AppAISkillRefDto
                {
                    SkillId = skillId,
                    FileName = SourceContextFileName,
                    FileContent = body,
                    SortOrder = 1000,
                    CreatedDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal skill source context ref failed");
            }
        }

        public static async Task<UniversalHandoffResult> CreateHandoffAsync(
            AppClientIdentity identity,
            string sourceSessionKey,
            string targetSkillKey,
            string reason,
            CancellationToken ct)
        {
            var userId = UserIdOf(identity);
            var dsId = ResolveDataSourceId(identity);
            if (userId <= 0 || dsId <= 0)
                throw new InvalidOperationException("User identity required.");

            var agent = FindActiveSpecialist(dsId, targetSkillKey);
            if (agent == null)
                throw new InvalidOperationException("Specialist agent was not found or is inactive.");

            var evidence = LoadEvidence(identity, sourceSessionKey);
            var summary = await WriteHandoffSummaryAsync(identity, evidence, agent, reason, ct).ConfigureAwait(false);
            var targetName = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
            var content =
                "Transferred from Universal Agent.\n\n" + summary.Trim();

            var targetSessionKey = AppGenericAgentSessionBL.CreateChat(agent.SkillKey, userId, dsId);
            if (string.IsNullOrWhiteSpace(targetSessionKey))
                throw new InvalidOperationException("Could not create the Specialist chat.");

            AppGenericAgentSessionBL.AppendMessage(
                targetSessionKey,
                agent.SkillKey,
                userId,
                dsId,
                new JObject
                {
                    ["role"] = "user",
                    ["content"] = content,
                    ["sourceAgent"] = "UniversalAgent",
                    ["sourceSessionKey"] = sourceSessionKey.Trim(),
                    ["handoffSummary"] = summary.Trim()
                });
            AppGenericAgentSessionBL.TryAutoTitle(targetSessionKey, agent.SkillKey, userId, content);

            AppGenericAgentSessionBL.AppendMessage(
                sourceSessionKey.Trim(),
                UniversalAgentConstants.SkillKey,
                userId,
                dsId,
                new JObject
                {
                    ["role"] = "assistant",
                    ["content"] = "Handed off to " + targetName + ".",
                    ["handoff"] = new JObject
                    {
                        ["targetSkillKey"] = agent.SkillKey,
                        ["targetSessionKey"] = targetSessionKey,
                        ["targetName"] = targetName,
                        ["summary"] = summary.Trim()
                    }
                });

            return new UniversalHandoffResult
            {
                TargetSkillKey = agent.SkillKey,
                TargetSessionKey = targetSessionKey,
                TargetName = targetName,
                Summary = summary.Trim()
            };
        }

        public static SkillSetDto FindActiveSpecialist(int dataSourceId, string skillKeyOrName)
        {
            var key = (skillKeyOrName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key) || UniversalAgentConstants.IsUniversalAgentKey(key))
                return null;
            var agents = dataSourceId > 0 ? AgentCatalogBL.GetAll(dataSourceId) : AgentCatalogBL.GetAll();
            return (agents ?? new List<SkillSetDto>())
                .FirstOrDefault(a => a != null && (
                    string.Equals(a.SkillKey, key, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.DisplayName, key, StringComparison.OrdinalIgnoreCase)));
        }

        private static async Task<string> WriteHandoffSummaryAsync(
            AppClientIdentity identity,
            ChatEvidence evidence,
            SkillSetDto agent,
            string reason,
            CancellationToken ct)
        {
            var targetName = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
            var user = new StringBuilder();
            user.AppendLine("Target Specialist: " + targetName + " (" + agent.SkillKey + ")");
            if (!string.IsNullOrWhiteSpace(agent.Description))
                user.AppendLine("Specialist description: " + agent.Description.Trim());
            if (!string.IsNullOrWhiteSpace(reason))
                user.AppendLine("Why this Specialist was proposed: " + TrimTo(reason.Trim(), 500));
            user.AppendLine();
            user.AppendLine(string.IsNullOrWhiteSpace(evidence.Text) ? "(no chat evidence)" : evidence.Text);

            string raw = "";
            try
            {
                raw = await GenericAgentEngine.CompleteOnceAsync(
                    identity,
                    HandoffSummarySystemPrompt,
                    user.ToString(),
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal handoff summary LLM failed");
            }

            var summary = StripFences(raw);
            if (string.IsNullOrWhiteSpace(summary) || summary.IndexOf("Goal:", StringComparison.OrdinalIgnoreCase) < 0)
                summary = FallbackHandoffSummary(evidence, reason);
            return summary.Trim();
        }

        private static ChatEvidence LoadEvidence(AppClientIdentity identity, string sessionKey)
        {
            var userId = UserIdOf(identity);
            var dsId = ResolveDataSourceId(identity);
            var detail = AppGenericAgentSessionBL.LoadBySessionKey(
                sessionKey?.Trim(),
                UniversalAgentConstants.SkillKey,
                userId,
                dsId);
            return BuildEvidence(detail?.Messages);
        }

        private static ChatEvidence BuildEvidence(List<JObject> messages)
        {
            var ev = new ChatEvidence();
            if (messages == null) return ev;

            var trace = new StringBuilder();
            var userCount = 0;
            string pendingUserVerbatim = null;
            List<ProvenToolStep> lastProvenChain = null;
            string goalUserVerbatim = null;

            foreach (var m in messages)
            {
                if (m == null) continue;
                var role = m.Value<string>("role") ?? "";
                if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase)) continue;
                var content = ExtractContent(m["content"]);
                if (content == "[session_start]" || content == "[run_in_progress]") continue;

                if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    var isDecision = m.Value<bool?>("askUserAnswer") == true;
                    if (!isDecision)
                    {
                        pendingUserVerbatim = content.Trim();
                        if (string.IsNullOrWhiteSpace(goalUserVerbatim))
                            goalUserVerbatim = pendingUserVerbatim;
                    }
                    userCount++;
                    if (userCount > 8) continue;
                    var line = TrimTo(OneLine(content), 400);
                    if (!isDecision && string.IsNullOrWhiteSpace(ev.Goal))
                        ev.Goal = line;
                    trace.AppendLine(isDecision ? "User decision: " + line : "User request: " + line);
                    continue;
                }

                if (!string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)) continue;
                if (m["handoff"] == null && !string.IsNullOrWhiteSpace(content))
                    ev.FinalAnswer = TrimTo(OneLine(content), 600);

                var steps = m["toolSteps"] as JArray;
                if (steps == null || steps.Count == 0) continue;

                var chain = BuildProvenChainFromSteps(steps);
                if (chain.Count > 0)
                {
                    lastProvenChain = chain;
                    if (!string.IsNullOrWhiteSpace(pendingUserVerbatim))
                        goalUserVerbatim = pendingUserVerbatim;
                }

                foreach (var step in steps.OfType<JObject>())
                {
                    if (ev.ToolNames.Count >= 24) break;
                    var name = (step.Value<string>("toolName") ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (string.Equals(name, "propose_specialist_handoff", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!ev.ToolNames.Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase)))
                        ev.ToolNames.Add(name);
                    trace.AppendLine("Tool call: " + name);
                    var args = TrimTo(OneLine(step.Value<string>("args")), 350);
                    if (!string.IsNullOrWhiteSpace(args))
                        trace.AppendLine("Arguments: " + args);
                    trace.AppendLine("Result shape: " + ResultShape(step.Value<string>("result")));
                }
            }

            ev.GoalVerbatim = goalUserVerbatim;
            ev.ProvenChain = lastProvenChain ?? new List<ProvenToolStep>();
            if (ev.ProvenChain.Count > 0)
            {
                ev.AllowedTools = ev.ProvenChain
                    .Select(s => s.ToolName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                ev.ToolNames = ev.AllowedTools.ToList();
            }
            ev.RowLimitHint = ExtractRowLimitHint(ev.ProvenChain, ev.GoalVerbatim);

            var sb = new StringBuilder();
            sb.AppendLine("=== DRAFT RULES ===");
            sb.AppendLine("- skillContent must follow the proven tool chain order in # Do.");
            sb.AppendLine("- List ONLY tools under ALLOWED TOOLS. Do not add tools that never ran.");
            sb.AppendLine("- No persona / Role section. Use # When to use, # Do, # Tools, # Notes only.");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(ev.GoalVerbatim))
            {
                sb.AppendLine("=== PRIMARY USER REQUEST (verbatim) ===");
                sb.AppendLine(TrimTo(ev.GoalVerbatim, 800));
                sb.AppendLine();
            }
            if (ev.ProvenChain.Count > 0)
            {
                sb.AppendLine("=== PROVEN TOOL CHAIN (successful last run; preserve order) ===");
                for (var i = 0; i < ev.ProvenChain.Count; i++)
                {
                    var s = ev.ProvenChain[i];
                    sb.AppendLine((i + 1) + ". " + s.ToolName
                        + " | args: " + (string.IsNullOrWhiteSpace(s.ArgsSummary) ? "{}" : s.ArgsSummary)
                        + " | result: " + (string.IsNullOrWhiteSpace(s.ResultHints) ? ResultShape(s.RawResult) : s.ResultHints));
                }
                sb.AppendLine();
                sb.AppendLine("=== ALLOWED TOOLS (only these may appear in skillContent) ===");
                sb.AppendLine(string.Join(", ", ev.AllowedTools));
                sb.AppendLine();
            }
            if (ev.RowLimitHint.HasValue)
            {
                sb.AppendLine("=== ROW / DISPLAY LIMIT ===");
                sb.AppendLine(ev.RowLimitHint.Value.ToString());
                sb.AppendLine();
            }
            sb.AppendLine("=== CHAT TRACE (reference) ===");
            sb.Append(trace);
            if (!string.IsNullOrWhiteSpace(ev.FinalAnswer))
                sb.AppendLine("Final answer: " + ev.FinalAnswer);

            ev.Text = sb.ToString().Trim();
            if (ev.Text.Length > 12000)
                ev.Text = ev.Text.Substring(0, 12000);
            return ev;
        }

        private static List<ProvenToolStep> BuildProvenChainFromSteps(JArray steps)
        {
            var chain = new List<ProvenToolStep>();
            if (steps == null) return chain;
            foreach (var step in steps.OfType<JObject>())
            {
                if (!IsToolStepSuccess(step)) continue;
                var name = (step.Value<string>("toolName") ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (string.Equals(name, "propose_specialist_handoff", StringComparison.OrdinalIgnoreCase))
                    continue;
                var rawArgs = step.Value<string>("args") ?? "";
                var rawResult = step.Value<string>("result") ?? "";
                chain.Add(new ProvenToolStep
                {
                    ToolName = name,
                    ArgsSummary = SummarizeToolArgs(rawArgs),
                    ResultHints = SummarizeToolResultHints(rawResult),
                    RawResult = rawResult
                });
            }
            return chain;
        }

        private static bool IsToolStepSuccess(JObject step)
        {
            if (step == null) return false;
            var token = step["isSuccess"] ?? step["IsSuccess"];
            if (token == null || token.Type == JTokenType.Null) return true;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            return true;
        }

        private static string SummarizeToolArgs(string rawArgs)
        {
            if (string.IsNullOrWhiteSpace(rawArgs)) return "{}";
            var trimmed = rawArgs.Trim();
            try
            {
                if (trimmed.StartsWith("{", StringComparison.Ordinal))
                {
                    var obj = JObject.Parse(trimmed);
                    if (obj.Count == 0) return "{}";
                    var parts = new List<string>();
                    foreach (var p in obj.Properties().Take(12))
                    {
                        var v = p.Value;
                        if (v.Type == JTokenType.String)
                            parts.Add(p.Name + "=" + TrimTo(v.ToString(), 40));
                        else if (v.Type is JTokenType.Integer or JTokenType.Float or JTokenType.Boolean)
                            parts.Add(p.Name + "=" + v.ToString(Formatting.None));
                        else
                            parts.Add(p.Name + "=(set)");
                    }
                    return "{" + string.Join(", ", parts) + "}";
                }
            }
            catch
            {
                /* fall through */
            }
            return TrimTo(OneLine(trimmed), 120);
        }

        private static string SummarizeToolResultHints(string rawResult)
        {
            if (string.IsNullOrWhiteSpace(rawResult)) return "";
            var candidate = ExtractJsonFence(rawResult) ?? rawResult.Trim();
            try
            {
                var token = JToken.Parse(candidate);
                if (token is not JObject obj) return "";
                var hints = new List<string>();
                foreach (var key in new[] { "display_row_limit", "DisplayRowLimit", "totalRows", "TotalRows", "dataset_name", "datasetName", "cache_key", "CacheKey", "rowCount", "RowCount" })
                {
                    var val = obj[key];
                    if (val == null || val.Type == JTokenType.Null) continue;
                    hints.Add(key + "=" + TrimTo(val.ToString(), 40));
                }
                return hints.Count == 0 ? "" : string.Join(", ", hints);
            }
            catch
            {
                return "";
            }
        }

        private static int? ExtractRowLimitHint(IList<ProvenToolStep> chain, string goalVerbatim)
        {
            int? fromGoal = ParseLimitFromText(goalVerbatim);
            int? fromSteps = null;
            if (chain != null)
            {
                foreach (var step in chain)
                {
                    var fromArgs = ParseLimitFromText(step.ArgsSummary);
                    if (fromArgs.HasValue) fromSteps = fromArgs;
                    var fromResult = ParseLimitFromText(step.ResultHints);
                    if (fromResult.HasValue) fromSteps = fromResult;
                }
            }
            if (fromGoal.HasValue && fromSteps.HasValue)
                return Math.Min(fromGoal.Value, fromSteps.Value);
            return fromGoal ?? fromSteps;
        }

        private static int? ParseLimitFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"(?:display_row_limit|limit|top|rowlimit|maxrows)\s*[=:]\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0) return n;
            m = Regex.Match(text, @"(?:first|top|latest|最近|前)\s*(\d+)\s*(?:条|行|records|rows)?", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out n) && n > 0) return n;
            return null;
        }

        private static string ResultShape(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "(empty)";
            var candidate = ExtractJsonFence(raw) ?? raw.Trim();
            try
            {
                return DescribeShape(JToken.Parse(candidate));
            }
            catch
            {
                var one = OneLine(raw);
                return one.Length > 160 ? one.Substring(0, 160) + "…" : one;
            }
        }

        private static string DescribeShape(JToken token)
        {
            if (token is JObject obj)
            {
                var keys = obj.Properties().Select(p => p.Name).Take(40).ToList();
                return keys.Count == 0 ? "object" : "object fields: " + string.Join(", ", keys);
            }
            if (token is JArray arr)
            {
                if (arr.Count == 0) return "array[0]";
                if (arr[0] is JObject first)
                {
                    var keys = first.Properties().Select(p => p.Name).Take(40).ToList();
                    return "array[" + arr.Count + "] fields: " + string.Join(", ", keys);
                }
                return "array[" + arr.Count + "]";
            }
            return token.Type.ToString();
        }

        private static UniversalSkillDraft ParseSkillDraft(string raw)
        {
            var json = ExtractJsonObject(raw);
            if (json == null) return null;
            var content = json.Value<string>("skillContent") ?? json.Value<string>("SkillContent") ?? "";
            if (string.IsNullOrWhiteSpace(content)) return null;
            return new UniversalSkillDraft
            {
                Name = json.Value<string>("name") ?? json.Value<string>("Name") ?? "",
                Description = json.Value<string>("description") ?? json.Value<string>("Description") ?? "",
                SkillContent = content
            };
        }

        private static UniversalSkillDraft FallbackSkillDraft(ChatEvidence evidence)
        {
            var name = string.IsNullOrWhiteSpace(evidence.Goal)
                ? "Saved skill"
                : TrimTo(OneLine(evidence.Goal), 60);
            var whenLine = string.IsNullOrWhiteSpace(evidence.GoalVerbatim)
                ? (string.IsNullOrWhiteSpace(evidence.Goal)
                    ? "Similar requests handled in the source Universal Agent chat."
                    : evidence.Goal)
                : TrimTo(OneLine(evidence.GoalVerbatim), 300);
            return new UniversalSkillDraft
            {
                Name = name,
                Description = TrimTo(
                    "Repeat the proven tool workflow for: " + whenLine,
                    400),
                SkillContent = BuildFallbackSkillContentBody(evidence)
            };
        }

        private static string FallbackHandoffSummary(ChatEvidence evidence, string reason)
        {
            var tools = evidence.ToolNames.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Goal:");
            sb.AppendLine(string.IsNullOrWhiteSpace(evidence.Goal) ? "- Continue the user's current task." : "- " + evidence.Goal);
            sb.AppendLine();
            sb.AppendLine("Decisions already made:");
            sb.AppendLine(string.IsNullOrWhiteSpace(reason) ? "- None recorded beyond the chat so far." : "- " + TrimTo(OneLine(reason), 400));
            sb.AppendLine();
            sb.AppendLine("Relevant tool outcomes:");
            if (tools.Count == 0)
                sb.AppendLine("- No tool calls recorded.");
            else
                foreach (var t in tools) sb.AppendLine("- " + t);
            sb.AppendLine();
            sb.AppendLine("Known inputs and constraints:");
            sb.AppendLine(string.IsNullOrWhiteSpace(evidence.FinalAnswer)
                ? "- Use only information already established in this handoff."
                : "- Latest result: " + evidence.FinalAnswer);
            return sb.ToString().Trim();
        }

        private static string BuildFallbackSkillContentBody(ChatEvidence evidence)
        {
            var when = string.IsNullOrWhiteSpace(evidence.GoalVerbatim)
                ? (string.IsNullOrWhiteSpace(evidence.Goal)
                    ? "When the user has a similar request to the saved chat."
                    : "When the user asks like: " + evidence.Goal)
                : "When the user asks like: \"" + TrimTo(OneLine(evidence.GoalVerbatim), 280) + "\"";

            var doLines = new List<string>();
            if (evidence.ProvenChain != null && evidence.ProvenChain.Count > 0)
            {
                for (var i = 0; i < evidence.ProvenChain.Count; i++)
                {
                    var s = evidence.ProvenChain[i];
                    doLines.Add((i + 1) + ". Call `" + s.ToolName + "` with args " + (string.IsNullOrWhiteSpace(s.ArgsSummary) ? "{}" : s.ArgsSummary) + ".");
                }
            }
            else if (evidence.AllowedTools != null && evidence.AllowedTools.Count > 0)
            {
                doLines.Add("1. Call `" + evidence.AllowedTools[0] + "` using the same argument pattern as the saved chat.");
            }
            else
            {
                doLines.Add("1. Confirm the user goal and required inputs.");
                doLines.Add("2. Use the library tool that matches the task; do not invent results.");
            }
            if (evidence.RowLimitHint.HasValue)
                doLines.Add((doLines.Count + 1) + ". Show at most " + evidence.RowLimitHint.Value + " rows in the UI when displaying tabular data.");
            doLines.Add((doLines.Count + 1) + ". Summarize what was retrieved for the user.");

            var tools = evidence.AllowedTools != null && evidence.AllowedTools.Count > 0
                ? evidence.AllowedTools
                : evidence.ToolNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var toolLines = tools.Count == 0
                ? "- (Use the tool that matched the saved chat.)"
                : string.Join("\n", tools.Select(t => "- `" + t + "`"));

            var notes = new List<string>
            {
                "- Do not invent tool results or data.",
                "- Do not call tools that were not in the proven chain unless the user asks for something new."
            };
            if (evidence.RowLimitHint.HasValue)
                notes.Add("- Honor display limit " + evidence.RowLimitHint.Value + " when rendering lists.");
            notes.Add("- Ask the user before destructive or irreversible actions.");

            return string.Join("\n\n", new[]
            {
                "# When to use",
                when,
                "# Do",
                string.Join("\n", doLines),
                "# Tools",
                toolLines,
                "# Notes",
                string.Join("\n", notes)
            });
        }

        private static string FinalizeDraftSkillContent(string content, ChatEvidence evidence)
        {
            var text = NormalizeSkillContent(content, evidence);
            if (evidence != null && !SkillContentReferencesOnlyAllowedTools(text, evidence))
                text = BuildFallbackSkillContentBody(evidence);
            return TrimTo(text, 2500);
        }

        private static string FinalizeSavedSkillContent(string content)
        {
            return TrimTo((content ?? "").Trim(), 12000);
        }

        private static bool SkillContentReferencesOnlyAllowedTools(string content, ChatEvidence evidence)
        {
            if (evidence?.AllowedTools == null || evidence.AllowedTools.Count == 0)
                return true;
            var allowed = new HashSet<string>(evidence.AllowedTools, StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(content ?? "", @"`([^`]+)`"))
            {
                var token = (m.Groups[1].Value ?? "").Trim();
                if (string.IsNullOrWhiteSpace(token)) continue;
                if (token.IndexOf(' ') >= 0) continue;
                if (allowed.Contains(token)) continue;
                if (LooksLikeToolName(token)) return false;
            }
            return true;
        }

        private static bool LooksLikeToolName(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 80) return false;
            return token.Contains('_') || token.StartsWith("PLM_", StringComparison.OrdinalIgnoreCase)
                || token.StartsWith("render_", StringComparison.OrdinalIgnoreCase)
                || token.StartsWith("execute_", StringComparison.OrdinalIgnoreCase)
                || token.StartsWith("prepare_", StringComparison.OrdinalIgnoreCase)
                || token.StartsWith("cache_", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSkillContent(string content, ChatEvidence evidence)
        {
            var text = (content ?? "").Trim();
            if (evidence == null) return text;
            if (string.IsNullOrWhiteSpace(text))
                return BuildFallbackSkillContentBody(evidence);

            var fallback = BuildFallbackSkillContentBody(evidence);
            foreach (var heading in new[] { "# When to use", "# Do", "# Tools", "# Notes" })
            {
                if (text.IndexOf(heading, StringComparison.OrdinalIgnoreCase) >= 0) continue;
                var section = ExtractMarkdownSection(fallback, heading);
                if (string.IsNullOrWhiteSpace(section)) continue;
                text = text + "\n\n" + section;
            }
            return text.Trim();
        }

        private static string ExtractMarkdownSection(string markdown, string heading)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return "";
            var lines = markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            var inSection = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("# ", StringComparison.Ordinal) && !line.StartsWith(heading, StringComparison.OrdinalIgnoreCase))
                {
                    if (inSection) break;
                    continue;
                }
                if (line.StartsWith(heading, StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    sb.AppendLine(line);
                    continue;
                }
                if (inSection) sb.AppendLine(line);
            }
            return sb.ToString().Trim();
        }

        private static JObject ExtractJsonObject(string raw)
        {
            var text = StripFences(raw);
            if (string.IsNullOrWhiteSpace(text)) return null;
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            try
            {
                return JObject.Parse(text.Substring(start, end - start + 1));
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractJsonFence(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var m = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        private static string StripFences(string raw)
        {
            var text = (raw ?? "").Trim();
            var fenced = ExtractJsonFence(text);
            return string.IsNullOrWhiteSpace(fenced) ? text : fenced.Trim();
        }

        private static string ExtractContent(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.String) return token.ToString();
            return token.ToString(Formatting.None);
        }

        private static string OneLine(string text)
        {
            return Regex.Replace(text ?? "", @"\s+", " ").Trim();
        }

        private static string TrimTo(string text, int max)
        {
            var s = text ?? "";
            if (max < 1 || s.Length <= max) return s;
            return s.Substring(0, max).TrimEnd();
        }

        private static bool HasSavableWork(ChatEvidence evidence)
        {
            if (evidence == null) return false;
            if (evidence.ProvenChain != null && evidence.ProvenChain.Count > 0) return true;
            if (!string.IsNullOrWhiteSpace(evidence.FinalAnswer)) return true;
            if (!string.IsNullOrWhiteSpace(evidence.Goal)) return true;
            if (!string.IsNullOrWhiteSpace(evidence.GoalVerbatim)) return true;
            return false;
        }

        private static int ResolveDataSourceId(AppClientIdentity identity)
        {
            if (identity.DataSourceId > 0) return identity.DataSourceId;
            return SkillLib.GetDefaultDataSourceId() ?? 0;
        }

        private static int UserIdOf(AppClientIdentity identity)
        {
            return identity.UserId == null ? 0 : Convert.ToInt32(identity.UserId);
        }

        private const string SkillDraftSystemPrompt = @"
You write a concise reusable skill from the evidence package (especially PROVEN TOOL CHAIN and ALLOWED TOOLS).
Return only JSON with keys name, description, and skillContent.

skillContent must be markdown with exactly these headings in order:
# When to use
# Do
# Tools
# Notes

Rules:
- # Do: numbered steps (max 8). Each data/UI step must name tools with backticks. Follow PROVEN TOOL CHAIN order and args.
- # Tools: one bullet per tool; only tools listed under ALLOWED TOOLS in the evidence.
- Do NOT invent tools, polish/render steps, personas, or extra confirmations not in the evidence.
- Do NOT use # Role, # Inputs, # Steps, # Outputs, or long agent personas.
- Keep skillContent under 2000 characters. No transcript, raw JSON rows, or full tool results.
- name: under 80 characters. description: one sentence under 400 characters describing when to select this skill.";

        private const string HandoffSummarySystemPrompt = @"
Write a handoff summary for one Specialist Agent.
Use exactly these headings and short bullets under each:
Goal:
Decisions already made:
Relevant tool outcomes:
Known inputs and constraints:
Do not paste the raw transcript or full tool results. Mention tool names and field names only.";

        private sealed class ProvenToolStep
        {
            public string ToolName = "";
            public string ArgsSummary = "";
            public string ResultHints = "";
            public string RawResult = "";
        }

        private sealed class ChatEvidence
        {
            public string Text = "";
            public string Goal = "";
            public string GoalVerbatim = "";
            public string FinalAnswer = "";
            public List<string> ToolNames = new List<string>();
            public List<string> AllowedTools = new List<string>();
            public List<ProvenToolStep> ProvenChain = new List<ProvenToolStep>();
            public int? RowLimitHint;
        }
    }
}
