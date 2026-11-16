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

        // ── Server: debug target Test Run / chat (MessagesJson + toolSteps only) ─

        [AgentTool("list_target_chats",
            "List recent chats for the OPEN edit target (current user). Prefer Test Run (IsFixedTestSession) when debugging Agent Management runs.")]
        public string ListTargetChats(
            AgentToolContext context,
            [AgentParam("Max chats (default 10, max 30).")] int? take = null)
        {
            try
            {
                if (!TryResolveTarget(context, null, out var target, out var err))
                    return Fail(err);
                var dsId = context?.DataSourceId ?? 0;
                var userId = context?.UserId ?? 0;
                if (dsId <= 0 || userId <= 0) return Fail("UserId and DataSourceId are required.");
                var n = take.GetValueOrDefault(10);
                if (n < 1) n = 10;
                if (n > 30) n = 30;
                var chats = AppGenericAgentSessionBL.ListChats(target, userId, n, dsId)
                    .Select(c => new
                    {
                        sessionKey = c.SessionKey,
                        title = c.Title,
                        updatedAt = c.UpdatedAt,
                        isFixedTestSession = c.IsFixedTestSession
                    })
                    .ToList();
                return Ok(new
                {
                    targetAgentCode = target,
                    chats,
                    count = chats.Count,
                    note = "Default get_target_last_run uses the newest chat (often the Test Run fixed session)."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("get_target_last_run",
            "Load the target agent's recent user→assistant turn(s) with ordered toolSteps from MessagesJson. Default takeTurns=3 (last few rounds). Judge by tool sequences, not chat tone.")]
        public string GetTargetLastRun(
            AgentToolContext context,
            [AgentParam("Optional SessionKey from list_target_chats; default = newest chat for target.")] string sessionKey = null,
            [AgentParam("How many recent complete turns to return (default 3, max 10). Newest first.")] int? takeTurns = null,
            [AgentParam("Skip this many newest turns before the window (default 0).")] int? turnIndex = null)
        {
            try
            {
                if (!TryLoadTargetTurns(context, sessionKey, takeTurns, turnIndex,
                        out var target, out var key, out var title, out var isFixed,
                        out var window, out var totalTurns, out var take, out var start, out var err))
                    return Fail(err);

                var turnsPayload = window.Select((t, i) => new
                {
                    turnIndex = start + i,
                    userMessage = Truncate(t.UserText, 1500),
                    assistantText = Truncate(t.AssistantText, 1200),
                    toolSequence = t.ToolSteps,
                    toolNames = t.ToolSteps.Select(s => (string)s["toolName"]).Where(n => !string.IsNullOrWhiteSpace(n)).ToList()
                }).ToList();

                var allToolNames = turnsPayload
                    .SelectMany(t => (IEnumerable<string>)t.toolNames)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var failedTools = window
                    .SelectMany(t => t.ToolSteps)
                    .Where(s => s["ok"]?.Value<bool>() == false
                        || string.Equals(s["ok"]?.ToString(), "false", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s["toolName"]?.ToString())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Flat fields = newest turn in the window (compat for single-turn callers).
                var newest = turnsPayload.Count > 0 ? turnsPayload[0] : null;
                return Ok(new
                {
                    targetAgentCode = target,
                    sessionKey = key,
                    title,
                    isFixedTestSession = isFixed,
                    takeTurns = take,
                    turnIndexStart = start,
                    turnCountReturned = turnsPayload.Count,
                    turnCountTotal = totalTurns,
                    turns = turnsPayload,
                    userMessage = newest?.userMessage,
                    assistantText = newest?.assistantText,
                    toolSequence = newest?.toolSequence,
                    toolNames = allToolNames,
                    failedTools,
                    judgeHint = "Compare toolSequence across turns (patterns / repeated failures). Prefer §9.4 over rewriting tone."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("explain_run_vs_instruction",
            "Compare recent target turn(s) tool sequences to draft Instruction/subscriptions; suggest a §9.4 fix class. Default takeTurns=3. Pass systemPrompt + libraryKeysJson from get_draft_* when possible.")]
        public string ExplainRunVsInstruction(
            AgentToolContext context,
            [AgentParam("Optional SessionKey; default newest target chat.")] string sessionKey = null,
            [AgentParam("Current draft SystemPrompt (from get_draft_system_prompt).")] string systemPrompt = null,
            [AgentParam("JSON array of subscribed LibraryKey.")] string libraryKeysJson = null,
            [AgentParam("Optional exclusions JSON [{libraryKey,toolName}].")] string exclusionsJson = null,
            [AgentParam("How many recent turns to analyze (default 3, max 10).")] int? takeTurns = null,
            [AgentParam("Skip this many newest turns before the window (default 0).")] int? turnIndex = null)
        {
            try
            {
                var runJson = GetTargetLastRun(context, sessionKey, takeTurns, turnIndex);
                var run = JObject.Parse(runJson);
                if (run["ok"]?.Value<bool>() != true)
                    return runJson;

                var prompt = systemPrompt ?? "";
                var toolNames = (run["toolNames"] as JArray)?.Select(t => t?.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                    ?? new List<string>();
                var failed = (run["failedTools"] as JArray)?.Select(t => t?.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                    ?? new List<string>();

                // Detect same tool failing in 2+ turns within the window.
                var failCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (run["turns"] is JArray turnArr)
                {
                    foreach (var t in turnArr.OfType<JObject>())
                    {
                        var seenThisTurn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var step in (t["toolSequence"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                        {
                            var ok = step["ok"]?.Value<bool>();
                            var name = step["toolName"]?.ToString();
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            if (ok == false && seenThisTurn.Add(name))
                            {
                                failCounts.TryGetValue(name, out var c);
                                failCounts[name] = c + 1;
                            }
                        }
                    }
                }
                var repeatedFails = failCounts.Where(kv => kv.Value >= 2).Select(kv => kv.Key).ToList();

                var mentioned = ExtractLikelyToolNames(prompt);
                var libs = ParseStringArray(libraryKeysJson);
                var excl = new HashSet<string>(
                    ParseExclusions(exclusionsJson).Select(e => Key(e.libraryKey, e.toolName)),
                    StringComparer.OrdinalIgnoreCase);
                var subscribed = new HashSet<string>(AutoInjected, StringComparer.OrdinalIgnoreCase);
                var dsId = context?.DataSourceId ?? 0;
                if (dsId > 0 && libs.Count > 0)
                {
                    foreach (var lib in libs)
                    {
                        foreach (var t in LibToolBL.GetByLibraryKey(dsId, lib).Where(x => x != null && x.IsActive))
                        {
                            if (excl.Contains(Key(lib, t.ToolName))) continue;
                            if (!string.IsNullOrWhiteSpace(t.ToolName))
                                subscribed.Add(t.ToolName.Trim());
                        }
                    }
                }

                var notInPrompt = toolNames
                    .Where(n => !AutoInjected.Contains(n) && !prompt.Contains(n, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var inPromptNotRun = mentioned
                    .Where(n => !toolNames.Any(t => string.Equals(t, n, StringComparison.OrdinalIgnoreCase)))
                    .Take(15)
                    .ToList();
                var notSubscribed = toolNames
                    .Where(n => libs.Count > 0 && !subscribed.Contains(n) && !AutoInjected.Contains(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string fixClass;
                string advice;
                if (repeatedFails.Count > 0)
                {
                    fixClass = "plugin_hard_block";
                    advice = "Same tool(s) failed in 2+ recent turns: " + string.Join(", ", repeatedFails)
                        + ". Prefer plugin hard-block / Description fix over more prompt text.";
                }
                else if (failed.Count > 0)
                {
                    fixClass = "plugin_or_description";
                    advice = "Tool(s) returned ok=false across the window: " + string.Join(", ", failed)
                        + ". Prefer fixing tool Description/ParameterSchema or plugin — not a long prompt essay.";
                }
                else if (notSubscribed.Count > 0)
                {
                    fixClass = "subscription";
                    advice = "Runs used tools not in the draft subscription set: " + string.Join(", ", notSubscribed)
                        + ". Subscribe (or stop inventing) those tools.";
                }
                else if (toolNames.Count == 0)
                {
                    fixClass = "prompt_sequence";
                    advice = "No tools ran in the recent window. Add a numbered scenario tool sequence, or check bans that blocked tools.";
                }
                else if (notInPrompt.Count > 0 && mentioned.Count > 0)
                {
                    fixClass = "prompt_order_or_ban";
                    advice = "Tools ran but not named in Instruction: " + string.Join(", ", notInPrompt)
                        + ". Add one hard step/ban sentence, or exclude those tools.";
                }
                else if (inPromptNotRun.Count > 0)
                {
                    fixClass = "prompt_order_or_ban";
                    advice = "Instruction mentions tools that did not run in the window: " + string.Join(", ", inPromptNotRun)
                        + ". Strengthen the numbered sequence for the hardest scenario.";
                }
                else
                {
                    fixClass = "review_result_shape";
                    advice = "Tool sequences look aligned across recent turns. If outcomes are still wrong, inspect truncated results / Description mismatch (plugin), not chat tone.";
                }

                return Ok(new
                {
                    targetAgentCode = run["targetAgentCode"]?.ToString(),
                    sessionKey = run["sessionKey"]?.ToString(),
                    takeTurns = run["takeTurns"],
                    turnIndexStart = run["turnIndexStart"],
                    turnCountReturned = run["turnCountReturned"],
                    turns = run["turns"],
                    toolNames,
                    failedTools = failed,
                    repeatedFailedTools = repeatedFails,
                    toolsRunButNotInPrompt = notInPrompt,
                    toolsInPromptButNotRun = inPromptNotRun,
                    toolsRunButNotSubscribed = notSubscribed,
                    fixClass,
                    advice,
                    repairMap = new
                    {
                        wrong_tool_or_params = "subscription or tool Description/ParameterSchema",
                        wrong_order_or_weak_ban = "add ONE prompt sentence",
                        lying_or_wrong_shaped_returns = "plugin / tool implementation",
                        same_error_twice = "prefer plugin hard-block over more prompt text"
                    }
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
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

        static bool TryLoadTargetTurns(
            AgentToolContext context,
            string sessionKey,
            int? takeTurns,
            int? turnIndex,
            out string target,
            out string key,
            out string title,
            out bool isFixed,
            out List<ChatTurn> window,
            out int totalTurns,
            out int take,
            out int start,
            out string error)
        {
            target = null;
            key = null;
            title = null;
            isFixed = false;
            window = new List<ChatTurn>();
            totalTurns = 0;
            take = 3;
            start = 0;
            error = null;

            if (!TryResolveTarget(context, null, out target, out error))
                return false;
            var dsId = context?.DataSourceId ?? 0;
            var userId = context?.UserId ?? 0;
            if (dsId <= 0 || userId <= 0)
            {
                error = "UserId and DataSourceId are required.";
                return false;
            }

            key = (sessionKey ?? "").Trim();
            if (key.Length == 0)
            {
                var newest = AppGenericAgentSessionBL.ListChats(target, userId, 1, dsId).FirstOrDefault();
                if (newest == null || string.IsNullOrWhiteSpace(newest.SessionKey))
                {
                    error = "No chats found for target '" + target + "'. Run Test on the target agent first.";
                    return false;
                }
                key = newest.SessionKey;
            }

            var detail = AppGenericAgentSessionBL.LoadBySessionKey(key, target, userId, dsId);
            if (detail?.Messages == null || detail.Messages.Count == 0)
            {
                error = "Chat is empty or not found for this user/target.";
                return false;
            }

            var turns = ExtractCompleteTurns(detail.Messages);
            totalTurns = turns.Count;
            if (totalTurns == 0)
            {
                error = "No complete user→assistant turns in this chat yet.";
                return false;
            }

            take = takeTurns.GetValueOrDefault(3);
            if (take < 1) take = 1;
            if (take > 10) take = 10;
            start = turnIndex.GetValueOrDefault(0);
            if (start < 0) start = 0;
            if (start >= totalTurns)
            {
                error = "turnIndex out of range (0.." + (totalTurns - 1) + ").";
                return false;
            }

            window = turns.Skip(start).Take(take).ToList();
            title = detail.Title;
            isFixed = string.Equals(key, AppGenericAgentSessionBL.MakeFixedKey(target, userId), StringComparison.Ordinal);
            return true;
        }

        static bool TryResolveTarget(
            AgentToolContext context, string clientTargetAgentCode, out string targetCode, out string error)
        {
            targetCode = null;
            error = null;
            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            if (string.IsNullOrWhiteSpace(hostJson))
            {
                error = "No HostContext. Debug tools only run inside Agent Management Instruction Designer.";
                return false;
            }
            try
            {
                var root = JObject.Parse(hostJson);
                var mode = (root["mode"] ?? root["Mode"])?.ToString();
                if (!string.Equals(mode, "instruction-designer", StringComparison.OrdinalIgnoreCase))
                {
                    error = "HostContext.mode must be instruction-designer.";
                    return false;
                }
                var isNew = root["isNew"]?.Value<bool>() == true;
                var code = (root["targetAgentCode"] ?? root["TargetAgentCode"])?.ToString()?.Trim() ?? "";
                if (isNew || code.Length == 0)
                {
                    error = "Target has no AgentCode yet. Save the agent, run Test, then debug.";
                    return false;
                }
                var client = (clientTargetAgentCode ?? "").Trim();
                if (client.Length > 0 && !string.Equals(client, code, StringComparison.OrdinalIgnoreCase))
                {
                    error = "targetAgentCode does not match the open draft HostContext.";
                    return false;
                }
                targetCode = code;
                return true;
            }
            catch (Exception ex)
            {
                error = "Invalid HostContextJson: " + ex.Message;
                return false;
            }
        }

        sealed class ChatTurn
        {
            public string UserText { get; set; }
            public string AssistantText { get; set; }
            public List<JObject> ToolSteps { get; set; }
        }

        /// <summary>
        /// Newest-first complete user→assistant turns (skips empty session_start noise).
        /// </summary>
        static List<ChatTurn> ExtractCompleteTurns(IList<JObject> messages)
        {
            var chronological = new List<ChatTurn>();
            if (messages == null) return chronological;
            string pendingUser = null;
            foreach (var m in messages)
            {
                if (m == null) continue;
                var role = (m["role"] ?? m["Role"])?.ToString()?.Trim().ToLowerInvariant() ?? "";
                var content = (m["content"] ?? m["Content"] ?? m["text"] ?? m["Text"])?.ToString() ?? "";
                if (role == "user")
                {
                    if (IsNoiseUser(content)) { pendingUser = null; continue; }
                    pendingUser = content;
                    continue;
                }
                if (role == "assistant" && pendingUser != null)
                {
                    chronological.Add(new ChatTurn
                    {
                        UserText = pendingUser,
                        AssistantText = content,
                        ToolSteps = CompactToolSteps(m["toolSteps"] as JArray ?? m["ToolSteps"] as JArray)
                    });
                    pendingUser = null;
                }
            }
            chronological.Reverse();
            return chronological;
        }

        static bool IsNoiseUser(string content)
        {
            var t = (content ?? "").Trim();
            if (t.Length == 0) return true;
            if (t.StartsWith("[session_start]", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static List<JObject> CompactToolSteps(JArray steps)
        {
            var list = new List<JObject>();
            if (steps == null) return list;
            foreach (var step in steps.OfType<JObject>().Take(40))
            {
                var name = (step["toolName"] ?? step["ToolName"] ?? step["name"])?.ToString()?.Trim() ?? "";
                if (name.Length == 0) continue;
                var okToken = step["isSuccess"] ?? step["ok"] ?? step["IsSuccess"];
                bool? ok = null;
                if (okToken != null && okToken.Type != JTokenType.Null)
                {
                    if (okToken.Type == JTokenType.Boolean) ok = okToken.Value<bool>();
                    else if (bool.TryParse(okToken.ToString(), out var b)) ok = b;
                }
                var args = Truncate(OneLine((step["args"] ?? step["Args"] ?? step["arguments"])?.ToString()), 500);
                var result = Truncate(OneLine((step["result"] ?? step["Result"])?.ToString()), 500);
                var label = (step["label"] ?? step["Label"] ?? step["description"])?.ToString();
                list.Add(new JObject
                {
                    ["toolName"] = name,
                    ["label"] = string.IsNullOrWhiteSpace(label) ? null : Truncate(label, 120),
                    ["args"] = args,
                    ["result"] = result,
                    ["ok"] = ok
                });
            }
            return list;
        }

        static string OneLine(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return Regex.Replace(s, @"\s+", " ").Trim();
        }
    }
}
