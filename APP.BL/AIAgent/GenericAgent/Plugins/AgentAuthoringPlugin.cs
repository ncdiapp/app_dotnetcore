using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.BL.TenantBusiness;
using APP.Components.Dto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CatalogBL = App.BL.TenantBusiness.AppAgentToolCatalogBL;
using LibBL = App.BL.TenantBusiness.AppAgentToolLibraryBL;
using LibToolBL = App.BL.TenantBusiness.AppAgentLibraryToolBL;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Authoring tools for agent-instruction-designer.
    /// Draft inspect/apply go through the Agent Management host (onHostTool).
    /// Catalog search / validate / propose run on the server.
    /// </summary>
    public class AgentAuthoringPlugin
    {
        static readonly HashSet<string> AutoInjected = new(StringComparer.OrdinalIgnoreCase) { "ask_user" };
        static readonly HashSet<string> HeavyDbTools = new(StringComparer.OrdinalIgnoreCase)
        {
            "execute_sql", "stored_procedure_execute"
        };

        // ── Host: inspect / apply (live draft in React) ───────────────────

        [AgentTool("get_draft_agent",
            "Inspect the open Agent Management draft (code, name, mode, limits, subscription summary). Full prompt via get_draft_system_prompt.")]
        public Task<string> GetDraftAgent(AgentToolContext context) =>
            AskHost(context, "get_draft_agent", new { });

        [AgentTool("get_draft_system_prompt",
            "Return the full SystemPrompt text of the open draft (not the HostContext preview).")]
        public Task<string> GetDraftSystemPrompt(AgentToolContext context) =>
            AskHost(context, "get_draft_system_prompt", new { });

        [AgentTool("get_draft_subscriptions",
            "Return subscribed library keys, tool exclusions, and whether the draft has an AgentCode yet.")]
        public Task<string> GetDraftSubscriptions(AgentToolContext context) =>
            AskHost(context, "get_draft_subscriptions", new { });

        [AgentTool("apply_system_prompt_to_draft",
            "Stage a full SystemPrompt into the open editor draft. Does not Save. Use ask_user before large overwrites.")]
        public Task<string> ApplySystemPromptToDraft(
            AgentToolContext context,
            [AgentParam("Full SystemPrompt body only — no preamble.", isRequired: true)] string systemPrompt = null) =>
            AskHost(context, "apply_system_prompt_to_draft", new { systemPrompt });

        [AgentTool("apply_subscriptions_to_draft",
            "Stage library subscriptions on the open draft (replaces the subscribed set). Does not Save. libraryKeysJson = JSON string array.")]
        public Task<string> ApplySubscriptionsToDraft(
            AgentToolContext context,
            [AgentParam("JSON array of LibraryKey strings.", isRequired: true)] string libraryKeysJson = null) =>
            AskHost(context, "apply_subscriptions_to_draft", new { libraryKeysJson });

        [AgentTool("apply_exclusions_to_draft",
            "Stage per-tool exclusions on the open draft. Does not Save. exclusionsJson = [{libraryKey,toolName},...].")]
        public Task<string> ApplyExclusionsToDraft(
            AgentToolContext context,
            [AgentParam("JSON array of {libraryKey,toolName}.", isRequired: true)] string exclusionsJson = null) =>
            AskHost(context, "apply_exclusions_to_draft", new { exclusionsJson });

        // ── Server: catalog ───────────────────────────────────────────────

        [AgentTool("list_libraries",
            "List active tool libraries (LibraryKey, name, description, category). Use before subscribing.")]
        public string ListLibraries(AgentToolContext context)
        {
            try
            {
                var dsId = context?.DataSourceId ?? 0;
                if (dsId <= 0) return Fail("DataSourceId is required.");
                var libs = LibBL.GetAllLibraries(dsId)
                    .Where(l => l != null && l.IsActive)
                    .Select(l => new
                    {
                        libraryKey = l.LibraryKey,
                        name = l.LibraryName,
                        description = Truncate(l.Description, 200),
                        category = l.ToolCategory,
                        domainKey = l.DomainKey
                    })
                    .ToList();
                return Ok(new { libraries = libs, count = libs.Count });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("get_library_tools",
            "List tools in one library (names + short descriptions). Prefer exact LibraryKey from list_libraries.")]
        public string GetLibraryTools(
            AgentToolContext context,
            [AgentParam("LibraryKey", isRequired: true)] string libraryKey = null)
        {
            try
            {
                var dsId = context?.DataSourceId ?? 0;
                if (dsId <= 0) return Fail("DataSourceId is required.");
                var key = (libraryKey ?? "").Trim();
                if (key.Length == 0) return Fail("libraryKey is required.");
                var tools = LibToolBL.GetByLibraryKey(dsId, key)
                    .Where(t => t != null && t.IsActive)
                    .Select(t => new
                    {
                        libraryKey = t.LibraryKey,
                        toolName = t.ToolName,
                        description = Truncate(t.Description, 180),
                        toolType = t.ToolType
                    })
                    .ToList();
                return Ok(new { libraryKey = key, tools, count = tools.Count });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("search_tool_catalog",
            "Keyword-shortlist tools from the synced catalog for a use-case description. Never invent ToolNames.")]
        public string SearchToolCatalog(
            AgentToolContext context,
            [AgentParam("What the agent needs to do (scenarios / keywords).", isRequired: true)] string query = null,
            [AgentParam("Max tools to return (default 80, max 300).")] int? max = null)
        {
            try
            {
                var dsId = context?.DataSourceId ?? 0;
                if (dsId <= 0) return Fail("DataSourceId is required.");
                var q = (query ?? "").Trim();
                if (q.Length == 0) return Fail("query is required.");
                var take = max.GetValueOrDefault(80);
                if (take < 1) take = 80;
                if (take > 300) take = 300;

                var warnings = new List<string>();
                try
                {
                    CatalogBL.SyncLibraryTools(dsId);
                    foreach (var s in CatalogBL.GetUnsyncedMcpServers(dsId))
                        warnings.Add($"MCP server {s} has not been synced.");
                }
                catch (Exception ex)
                {
                    warnings.Add("Catalog sync failed: " + ex.Message);
                }

                var full = CatalogBL.GetAll(dsId)
                    .Where(t => t != null && !AutoInjected.Contains(t.ToolName ?? ""))
                    .ToList();
                var shortlist = ToolCatalogRetrieval.Current.Retrieve(full, q, take);
                var tools = shortlist.Select(t => new
                {
                    libraryKey = t.LibraryKey,
                    toolName = t.ToolName,
                    risk = t.Risk,
                    description = Truncate(t.Description, 140),
                    args = t.InputSummary
                }).ToList();
                return Ok(new { query = q, tools, count = tools.Count, warnings });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        // ── Server: propose / validate / limits ───────────────────────────

        [AgentTool("propose_system_prompt",
            "Normalize a proposed SystemPrompt for ask_user review. Does not apply. Prefer §9.2 blocks (concepts → hardest scenario → other scenarios → shared bans).")]
        public string ProposeSystemPrompt(
            AgentToolContext context,
            [AgentParam("Full proposed SystemPrompt body.", isRequired: true)] string systemPrompt = null,
            [AgentParam("Short note for the user about what changed.")] string summary = null)
        {
            var prompt = systemPrompt ?? "";
            if (string.IsNullOrWhiteSpace(prompt))
                return Fail("systemPrompt is required.");
            var looksFourH2 = Regex.IsMatch(prompt, @"##\s*Role\b", RegexOptions.IgnoreCase)
                && Regex.IsMatch(prompt, @"##\s*Workflow\b", RegexOptions.IgnoreCase)
                && !Regex.IsMatch(prompt, @"\b(scenario|tool sequence|numbered)\b", RegexOptions.IgnoreCase);
            return Ok(new
            {
                proposal = prompt,
                summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                chars = prompt.Length,
                warning = looksFourH2
                    ? "Looks like four-H2 Role/Workflow without scenario tool sequences — reshape to §9.2 before apply."
                    : null
            });
        }

        [AgentTool("propose_tool_plan",
            "Normalize a tool plan (libraries + optional exclusions) for ask_user. Does not apply. Never include ask_user as a private BuiltIn.")]
        public string ProposeToolPlan(
            AgentToolContext context,
            [AgentParam("JSON array of LibraryKey strings.", isRequired: true)] string libraryKeysJson = null,
            [AgentParam("Optional JSON array of {libraryKey,toolName} exclusions.")] string exclusionsJson = null,
            [AgentParam("Optional JSON array of private BuiltIn ToolNames (0–5; never ask_user).")] string privateBuiltInNamesJson = null,
            [AgentParam("Short reason for the plan.")] string reason = null)
        {
            try
            {
                var libs = ParseStringArray(libraryKeysJson);
                var excl = ParseExclusions(exclusionsJson);
                var builtIns = ParseStringArray(privateBuiltInNamesJson)
                    .Where(n => !AutoInjected.Contains(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList();
                if (libs.Count == 0 && builtIns.Count == 0)
                    return Fail("At least one libraryKey or privateBuiltIn is required.");
                return Ok(new
                {
                    libraryKeys = libs,
                    exclusions = excl,
                    privateBuiltInToolNames = builtIns,
                    reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                    note = builtIns.Count > 0
                        ? "Private BuiltIns require a saved AgentCode; unsaved drafts can only stage libraries/exclusions."
                        : null
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("validate_prompt_tool_alignment",
            "Warn when ToolNames mentioned in the prompt are missing from the subscribed set (or exclusions hide them). Pass snapshots from get_draft_*.")]
        public string ValidatePromptToolAlignment(
            AgentToolContext context,
            [AgentParam("Full SystemPrompt text.", isRequired: true)] string systemPrompt = null,
            [AgentParam("JSON array of subscribed LibraryKey strings.")] string libraryKeysJson = null,
            [AgentParam("Optional JSON array of {libraryKey,toolName} exclusions.")] string exclusionsJson = null)
        {
            try
            {
                var dsId = context?.DataSourceId ?? 0;
                if (dsId <= 0) return Fail("DataSourceId is required.");
                var prompt = systemPrompt ?? "";
                var libs = ParseStringArray(libraryKeysJson);
                var excl = new HashSet<string>(
                    ParseExclusions(exclusionsJson).Select(e => Key(e.libraryKey, e.toolName)),
                    StringComparer.OrdinalIgnoreCase);

                var subscribedTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var lib in libs)
                {
                    foreach (var t in LibToolBL.GetByLibraryKey(dsId, lib).Where(x => x != null && x.IsActive))
                    {
                        if (excl.Contains(Key(lib, t.ToolName))) continue;
                        if (!string.IsNullOrWhiteSpace(t.ToolName))
                            subscribedTools.Add(t.ToolName.Trim());
                    }
                }
                foreach (var a in AutoInjected) subscribedTools.Add(a);

                var mentioned = ExtractLikelyToolNames(prompt);
                var missing = mentioned.Where(n => !subscribedTools.Contains(n) && !AutoInjected.Contains(n)).ToList();
                var unusedHint = subscribedTools
                    .Where(n => !AutoInjected.Contains(n) && !prompt.Contains(n, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .ToList();

                return Ok(new
                {
                    mentionedInPrompt = mentioned,
                    missingFromSubscriptions = missing,
                    subscribedButNotMentionedSample = unusedHint,
                    ok = missing.Count == 0
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("suggest_limit_tweaks",
            "Suggest MaxToolResultChars when heavy DB tools (execute_sql / stored_procedure_execute) appear in the plan or prompt.")]
        public string SuggestLimitTweaks(
            AgentToolContext context,
            [AgentParam("Current MaxToolResultChars.")] int? currentMaxToolResultChars = null,
            [AgentParam("JSON array of ToolNames in the plan.")] string toolNamesJson = null,
            [AgentParam("Optional SystemPrompt to scan.")] string systemPrompt = null)
        {
            var names = ParseStringArray(toolNamesJson);
            var prompt = systemPrompt ?? "";
            var heavy = names.Any(n => HeavyDbTools.Contains(n))
                || HeavyDbTools.Any(h => prompt.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0);
            const int recommended = 400000;
            var cur = currentMaxToolResultChars.GetValueOrDefault(0);
            if (!heavy)
                return Ok(new { heavyDb = false, suggestion = (object)null });
            return Ok(new
            {
                heavyDb = true,
                suggestion = cur < recommended
                    ? new { field = "MaxToolResultChars", from = cur, to = recommended, reason = "Heavy DB tool results need a high CapResult budget." }
                    : null,
                note = cur >= recommended ? "Current limit already sufficient." : "Host can update MaxToolResultChars on the draft when the user accepts."
            });
        }

        // ── helpers ───────────────────────────────────────────────────────

        static async Task<string> AskHost(AgentToolContext context, string toolName, object args)
        {
            var callbacks = AgentHitlBridge.Resolve(context?.ChatSessionKey);
            if (callbacks?.OnHostTool == null)
            {
                return Fail("This tool only runs inside Agent Management Instruction Designer.");
            }

            var evt = new FormHostToolEvent
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                ArgsJson = JsonConvert.SerializeObject(args)
            };
            try
            {
                var result = await callbacks.OnHostTool(evt).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(result)
                    ? Fail("Empty host result.")
                    : result;
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static string Ok(object payload)
        {
            var jo = JObject.FromObject(payload ?? new { });
            jo["ok"] = true;
            return jo.ToString(Formatting.None);
        }

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { ok = false, error });

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max) + "…";
        }

        static string Key(string lib, string tool) => (lib ?? "").Trim() + "\u0001" + (tool ?? "").Trim();

        static List<string> ParseStringArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            var token = JToken.Parse(json);
            if (token is JArray arr)
                return arr.Select(t => t?.Type == JTokenType.String ? t.Value<string>() : t?["libraryKey"]?.ToString() ?? t?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            return new List<string>();
        }

        static List<(string libraryKey, string toolName)> ParseExclusions(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<(string, string)>();
            var arr = JArray.Parse(json);
            var list = new List<(string, string)>();
            foreach (var t in arr)
            {
                var lib = (t?["libraryKey"] ?? t?["LibraryKey"])?.ToString()?.Trim();
                var name = (t?["toolName"] ?? t?["ToolName"])?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(lib) && !string.IsNullOrWhiteSpace(name))
                    list.Add((lib, name));
            }
            return list;
        }

        static List<string> ExtractLikelyToolNames(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return new List<string>();
            var matches = Regex.Matches(prompt, @"\b([a-z][a-z0-9]*(?:_[a-z0-9]+)+)\b");
            return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Where(n => n.Length >= 5 && !n.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(80)
                .ToList();
        }
    }
}
