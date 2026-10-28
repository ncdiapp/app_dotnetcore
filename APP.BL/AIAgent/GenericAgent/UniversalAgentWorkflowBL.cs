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
            if (string.IsNullOrWhiteSpace(evidence.Text))
                throw new InvalidOperationException("This chat has no completed work to save as a skill.");

            string raw = "";
            try
            {
                raw = await SharedAgentEngine.CompleteOnceAsync(
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
            draft.SkillContent = EnsureSkillHeadings(draft.SkillContent);
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
            var contentTrim = EnsureSkillHeadings(skillContent);

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
                "Transferred from General Agent.\n\n" + summary.Trim();

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
                GeneralAgentConstants.SkillKey,
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
            if (string.IsNullOrWhiteSpace(key) || GeneralAgentConstants.IsGeneralAgentKey(key))
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
                raw = await SharedAgentEngine.CompleteOnceAsync(
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
                GeneralAgentConstants.SkillKey,
                userId,
                dsId);
            return BuildEvidence(detail?.Messages);
        }

        private static ChatEvidence BuildEvidence(List<JObject> messages)
        {
            var ev = new ChatEvidence();
            if (messages == null) return ev;
            var sb = new StringBuilder();
            var userCount = 0;
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
                    userCount++;
                    if (userCount > 8) continue;
                    var line = TrimTo(OneLine(content), 400);
                    var isDecision = m.Value<bool?>("askUserAnswer") == true;
                    if (!isDecision && string.IsNullOrWhiteSpace(ev.Goal))
                        ev.Goal = line;
                    sb.AppendLine(isDecision ? "User decision: " + line : "User request: " + line);
                    continue;
                }

                if (!string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)) continue;
                if (m["handoff"] == null && !string.IsNullOrWhiteSpace(content))
                    ev.FinalAnswer = TrimTo(OneLine(content), 600);

                var steps = m["toolSteps"] as JArray;
                if (steps == null) continue;
                foreach (var step in steps.OfType<JObject>())
                {
                    if (ev.ToolNames.Count >= 16) break;
                    var name = (step.Value<string>("toolName") ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (string.Equals(name, "propose_specialist_handoff", StringComparison.OrdinalIgnoreCase))
                        continue;
                    ev.ToolNames.Add(name);
                    sb.AppendLine("Tool call: " + name);
                    var args = TrimTo(OneLine(step.Value<string>("args")), 350);
                    if (!string.IsNullOrWhiteSpace(args))
                        sb.AppendLine("Arguments: " + args);
                    sb.AppendLine("Result shape: " + ResultShape(step.Value<string>("result")));
                }
            }

            if (!string.IsNullOrWhiteSpace(ev.FinalAnswer))
                sb.AppendLine("Final answer: " + ev.FinalAnswer);
            ev.Text = sb.ToString().Trim();
            if (ev.Text.Length > 12000)
                ev.Text = ev.Text.Substring(0, 12000);
            return ev;
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
            var tools = evidence.ToolNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var toolLines = tools.Count == 0
                ? "- Use the library tool that matches the task."
                : string.Join("\n", tools.Select(t => "- " + t));
            var content = string.Join("\n\n", new[]
            {
                "# Role",
                "Carry out this repeatable workflow for a similar user request.",
                "# Inputs",
                string.IsNullOrWhiteSpace(evidence.Goal)
                    ? "- Use the current user request. Ask when a required input is missing."
                    : "- Typical request: " + evidence.Goal,
                "# Steps",
                tools.Count == 0
                    ? "1. Confirm the goal and any missing inputs.\n2. Complete the task and report the result."
                    : "1. Confirm required inputs before calling tools.\n2. Call " + string.Join(", ", tools) + " with the fields used in the original run.\n3. Check the result shape before answering.",
                "# Tools",
                toolLines,
                "# Outputs",
                string.IsNullOrWhiteSpace(evidence.FinalAnswer)
                    ? "- The completed result for the user's request."
                    : "- " + evidence.FinalAnswer,
                "# Constraints",
                "- Do not invent tool results.\n- Ask the user before a destructive or irreversible step."
            });
            var name = string.IsNullOrWhiteSpace(evidence.Goal) ? "Saved skill" : TrimTo(evidence.Goal, 60);
            return new UniversalSkillDraft
            {
                Name = name,
                Description = TrimTo(evidence.Goal ?? "Reusable workflow saved from a General Agent chat.", 180),
                SkillContent = content
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

        private static string EnsureSkillHeadings(string content)
        {
            var text = (content ?? "").Trim();
            var sb = new StringBuilder(text);
            foreach (var heading in new[] { "# Role", "# Inputs", "# Steps", "# Tools", "# Outputs", "# Constraints" })
            {
                if (sb.ToString().IndexOf(heading, StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (sb.Length > 0) sb.AppendLine().AppendLine();
                sb.AppendLine(heading);
                sb.AppendLine();
                sb.AppendLine("- Review and complete this section.");
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
You write a reusable skill from a compact evidence package.
Return only JSON with keys name, description, and skillContent.
skillContent must be markdown with these headings, in this order:
# Role
# Inputs
# Steps
# Tools
# Outputs
# Constraints
Steps must name the tools to call again, the important input fields, and any user confirmations required before those tools.
Summarize the repeatable workflow. Do not paste the chat transcript, raw tool results, or row data.
name is under 80 characters. description is one sentence under 400 characters.";

        private const string HandoffSummarySystemPrompt = @"
Write a handoff summary for one Specialist Agent.
Use exactly these headings and short bullets under each:
Goal:
Decisions already made:
Relevant tool outcomes:
Known inputs and constraints:
Do not paste the raw transcript or full tool results. Mention tool names and field names only.";

        private sealed class ChatEvidence
        {
            public string Text = "";
            public string Goal = "";
            public string FinalAnswer = "";
            public List<string> ToolNames = new List<string>();
        }
    }
}
