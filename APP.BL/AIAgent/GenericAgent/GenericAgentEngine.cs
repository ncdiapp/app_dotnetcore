#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.AiSkill;
using App.BL.AIAgent.GenericAgent.Memory;
using App.BL.AIAgent.GenericAgent.Plugins;
using App.BL.DbGenie;
using App.BL.GenericAgent;
using App.BL.TenantBusiness;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json.Linq;
using TbSkillBL = App.BL.AIAgent.AiSkill.AppAgentSkillSetBL;
using TbToolBL  = App.BL.TenantBusiness.AppAgentToolRegisterBL;
using TbMcpBL   = App.BL.TenantBusiness.AppAgentMcpServerBL;
using TbToolDto = App.BL.TenantBusiness.AppAgentToolRegisterDto;
using TbMcpDto  = App.BL.TenantBusiness.AppAgentMcpServerDto;
using TbSkillSetDto = App.BL.AIAgent.AiSkill.AppAgentSkillSetDto;
using SkillLib  = App.BL.AppMgr.AiSkill.AppAISkillBL;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Semantic Kernel–based agentic loop for the generic agent framework.
    /// Uses ChatCompletionAgent + InvokeStreamingAsync; SK handles all provider
    /// format differences (Anthropic/OpenAI/Gemini) internally.
    ///
    /// Registered tools (AppAgentToolRegister) are wrapped as KernelFunctions via
    /// KernelFunctionFactory.CreateFromMethod and dispatched through AppAgentToolEngine.
    /// MCP servers are connected via McpClient + HttpClientTransport (StreamableHttp);
    /// tools are wrapped manually — NOT via AsKernelFunction() which causes
    /// MissingMethodException due to Microsoft.Extensions.AI version mismatch.
    /// </summary>
    public static class GenericAgentEngine
    {
        private static readonly HttpClient McpHttpClient = McpConnectionHelper.CreateHttpClient();
        private const int DefaultMaxToolResultChars = 32000;
        /// <summary>Large PLM list tools (hundreds of wide rows) often exceed 30s; fail-closed cancel looked like “empty/partial” tool results.</summary>
        private const int McpToolCallTimeoutSeconds  = 180;

        /// <summary>Capability marker tool in library data-analyze. Presence (subscribed, not excluded) enables followups inject.</summary>
        private const string DataAnalyzeToolName = "data_analyze";
        private const string PromptHintToolType = "PromptHint";
        private const string UniversalBasePrompt = @"

## Universal Agent
You are Universal Agent, the built-in general work agent for this application.
Understand the user's goal, use the most relevant loaded skills, and call tools whenever tools can do the work.
Skills are reusable operating instructions for this chat run. Follow them when relevant, but do not claim a skill was used unless it was actually loaded.
Use library tools directly when they match the task. Ask the user concise questions when required inputs are missing.
When one configured Specialist Agent is clearly better for the next step, call propose_specialist_handoff with that agent's skillKey and a short reason.
Do not call call_agent. Nothing switches until the user confirms the hand-off card. Do not claim the chat was switched.";
        /// <summary>
        /// Platform hard rule — always appended to Instructions. Not stored in AppAgentSkillSet;
        /// admins cannot turn it off. Prefer tools over model-invented data/UI/files.
        /// </summary>
        private const string ToolsPreferHardRule = @"

## Tools Prefer (platform hard rule — cannot be disabled)
For ANY task, if a registered or built-in tool can do the work, you MUST call that tool. Never do the work only inside the model when a tool exists.
This applies to every capability that has a tool, including but not limited to:
- Fetch / query data (SQL, API, search)
- Large/unbounded row fetches → execute_sql (COUNT first; ≤250 rows auto-shows all unless user asked for N or SQL has hard TOP N; larger → confirm_row_limit). When user wants N rows: TOP N / SP args / render_interactive limit=N; FinalResponse must match display_rows/RowCount.
- Aggregations and analysis (GROUP BY, SUM, count, distribution, ranges, breakdowns) → ask_user(confirm_kpi_dashboard) then data_analyze and/or SQL — never FinalResponse-only tables
- Result UI (KPI, chart, dashboard, grid) → render_interactive / render_none_interactive
- Card/table field selection → prepare_ui_render_polish (templates + ≤10 sample rows) then cache_ui_render_polish with AiPolishedRenderSpec (structure only). Never merge Result rows into HTML in the model; GENERATE binds all N rows.
- Exports and documents (CSV, Excel, Word, PDF, PPTX) → export_* / document tools
- Any other subscribed or built-in tool that matches the user intent
FinalResponse may briefly explain outcomes and point to Result / files. It must NOT invent numeric tables, HTML dashboards, or file contents that a tool should produce.
If several tools could work, choose the most specific and call it. If no suitable tool is available, say that clearly — do not fake the output.";

        /// <summary>
        /// Injected when data_analyze is subscribed. Biases multi-part analysis asks toward
        /// ask_user(KPI) + data_analyze + render — not LLM-only markdown tables.
        /// </summary>
        private const string AnalysisPathHardRule = @"

## Analysis path (platform hard rule — when data_analyze is available)
Treat as analysis-like ONLY when the user asks to analyze / group by / break down / distribution / ranges / buckets / KPI / metrics / how many / count by / multi-part insight lists.
Do NOT treat plain ""show / display / grid view / card view"" as analysis — no ask_user KPI gate for those.

### Large data / row limit (Interactive — always)
For detail SELECT: execute_sql COUNTs first; may ask confirm_row_limit.
- User asked for N (e.g. 10 latest): TOP N, SP args (top/limit), render_interactive limit=N.
- Hard TOP N honored when total ≤ 250; else turn intent caps fetch/render.
- If counted rows ≤ 250 and no specific N: may fetch all without ask.
- If larger: TOP N (from user intent when set) / TOP 100 / TOP 1000 / All N.
- Stored procedures: pass row-limit args when the register supports them.
- Scalar COUNT(*) / GROUP BY skip this gate.

### Show grid + card on ONE Result page (not analysis)
When the user asks to show both grid and card (or table + cards) without analysis:
- Call render_interactive ONCE with layout=page, dataset_name, and specJson including views + sectionTitles.
- sectionTitles (required when stacking): short, user-facing labels derived from the user's question — NOT literal ""Grid""/""Cards""/""Table"". Example: {""views"":[""table"",""cards""],""cardsPerRow"":3,""sectionTitles"":{""table"":""Body parts — table"",""cards"":""Body parts — cards""}}.
- Result shows BOTH views stacked on one page (collapse/expand sections). There is NO view toggle or view selector — do not claim one in FinalResponse.
- Do NOT emit three separate Results (dashboard + grid + card).
- Do NOT call ask_user(confirm_kpi_dashboard) just for multi-view show.
- Prefer render_interactive; for row volume use confirm_row_limit (above), not a silent full fetch.

### Analysis-like asks (Interactive mode)
1. Call ask_user FIRST — contextKey=confirm_kpi_dashboard, mode=single_choice (kpi_dashboard vs fetch_display). Bias kpi_dashboard.
2. After kpi_dashboard: cache rows → data_analyze → render_interactive layout=dashboard (one Result).
3. After fetch_display: fetch + render_interactive table and/or layout=page — no data_analyze.
4. Forbidden: FinalResponse-only numeric tables when data_analyze/render tools exist.
If kpi_dashboard already confirmed this chat, do not re-ask.";

        private const string FollowupsPromptHint = @"

## Follow-up suggestions (soft chips)
Only after this turn used data_analyze (or a confirmed KPI/dashboard analysis path). Never after show/search/fetch_display/render-only answers.
When allowed: if there are natural next questions, end your FinalResponse with a fenced code block whose language tag is followups, containing a JSON array of 2–3 short, specific questions (under ~12 words each). Example: [""Break this down by region"",""Which styles have negative margin?""].
Skip the block for yes/no, finished answers, or any turn that did not call data_analyze. Chips are non-blocking suggestions for the *user* to click (not ask_user). Do not call data_analyze (or any tool) because of chips — only if the user later asks. Do NOT put chart/dashboard/visualize render requests in chips — use render_none_interactive when the user asks for visuals. Keep ask_user for required Gate/confirmations only.";

        public static async Task RunAsync(
            string                skillKey,
            string                userMessage,
            List<JObject>         chatHistory,
            GenericAgentCallbacks callbacks,
            AppClientIdentity?    identity,
            CancellationToken     ct,
            string                workflowId = null,
            string                chatSessionKey = null)
        {
            var log = NLog.LogManager.GetCurrentClassLogger();
            var runSw = System.Diagnostics.Stopwatch.StartNew();
            log.Info($"[Agent] START skill={skillKey}");
            var previousHitl = AgentHitlBridge.Current;
            AgentHitlBridge.Current = callbacks;
            string boundChatKey = null;
            try
            {
                var dsId = identity.HasValue ? identity.Value.DataSourceId : 0;
                var isUniversal = UniversalAgentConstants.IsUniversalAgentKey(skillKey);

                var skillSet = isUniversal
                    ? BuildUniversalSkillSet()
                    : (dsId > 0
                        ? TbSkillBL.GetByKey(skillKey, dsId)
                        : TbSkillBL.GetByKey(skillKey));
                if (skillSet == null)
                {
                    await Safe(callbacks?.OnError, $"Skill key not found: {skillKey}").ConfigureAwait(false);
                    return;
                }

                // Hard rule: always inject (not editable via SkillSet UI / migrations).
                var systemPrompt = (skillSet.SystemPrompt ?? "") + ToolsPreferHardRule;

                var userId    = identity.HasValue && identity.Value.UserId != null                      ? Convert.ToInt32(identity.Value.UserId)                      : 0;
                var companyId = identity.HasValue && identity.Value.CurrentWorkingCompanyId != null     ? Convert.ToInt32(identity.Value.CurrentWorkingCompanyId)      : 0;
                var resolvedChatKey = string.IsNullOrWhiteSpace(chatSessionKey)
                    ? (userId > 0 ? AppGenericAgentSessionBL.MakeFixedKey(skillKey, userId) : "")
                    : chatSessionKey.Trim();
                // Session fallback when tool dispatch loses AsyncLocal (row-limit ask_user).
                boundChatKey = resolvedChatKey;
                AgentHitlBridge.BindSession(boundChatKey, callbacks);
                if (!string.IsNullOrWhiteSpace(resolvedChatKey))
                    Documents.AgentDisplayRowLimitBL.SetTurnRowIntent(
                        resolvedChatKey,
                        Documents.AgentDisplayRowLimitBL.TryParseFromUserMessage(userMessage));
                var context   = new AgentToolContext
                {
                    ConnectionString = identity.HasValue ? identity.Value.CurrentUserDbConnectionString ?? "" : "",
                    DatabaseName     = identity.HasValue ? identity.Value.CurrentUserDataBaseName      ?? "" : "",
                    SessionId        = Guid.NewGuid().ToString("N"),
                    UserSessionId    = identity.HasValue ? identity.Value.SessionId?.ToString() ?? "" : "",
                    SkillKey         = skillKey,
                    UserId           = userId,
                    CompanyId        = companyId,
                    DataSourceId     = dsId,
                    LoginUserType    = identity.HasValue ? identity.Value.CurrentLoginUserType : 0,
                    IsDeterministic  = string.Equals(skillSet.ExecutionMode, "Deterministic", StringComparison.OrdinalIgnoreCase),
                    WorkflowId       = string.IsNullOrEmpty(workflowId) ? Guid.NewGuid().ToString("N") : workflowId,
                    ChatSessionKey   = resolvedChatKey
                };

                if (companyId > 0 && !string.IsNullOrWhiteSpace(resolvedChatKey))
                {
                    try { GenericAgentFileBL.EnsureRoot(resolvedChatKey, companyId, skillKey); }
                    catch (Exception swallowed) { SwallowLog.Write(swallowed, "official source seed must not fail the run"); }

                    try
                    {
                        var catalog = GenericAgentFileCatalogBL.LoadChatSourceCatalog(resolvedChatKey, companyId);
                        if (catalog?.Files == null || catalog.Files.Count == 0)
                            catalog = GenericAgentFileCatalogBL.LoadStarterCatalog(skillKey, companyId);
                        var catalogSummary = GenericAgentFileCatalogBL.BuildPromptSummary(catalog, "source/");
                        if (!string.IsNullOrWhiteSpace(catalogSummary))
                            systemPrompt = (systemPrompt ?? "") + "\n\n" + catalogSummary;
                    }
                    catch (Exception swallowed) { SwallowLog.Write(swallowed, "catalog inject must not fail the run"); }
                }

                // If this chat already fetched get_database_schema for a DS, tell the model not to re-call.
                if (!string.IsNullOrWhiteSpace(resolvedChatKey))
                {
                    try
                    {
                        var schemaHint = AgentSchemaCacheBL.BuildSessionPromptHint(resolvedChatKey);
                        if (!string.IsNullOrWhiteSpace(schemaHint))
                            systemPrompt = (systemPrompt ?? "") + schemaHint;
                    }
                    catch (Exception swallowed) { SwallowLog.Write(swallowed, "schema session hint must not fail the run"); }
                }

                // Per-session instance pool keeps stateful plugin instances (e.g. SchemaDesignerPlugin)
                // alive across multiple tool calls within the same agent run.
                var instancePool = new Dictionary<string, object>(StringComparer.Ordinal);

                var enableThinkMode = skillSet.EnableThinkMode;

                // Build SK kernel from tenant Application Settings default provider
<<<<<<< HEAD
                var kernel = KernelFactory.Build(identity);
                kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks));
=======
                var kernel = BuildKernel(identity);
                kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks, enableThinkMode));
>>>>>>> 3ca7037a509e2e7b3582f25c87da8e293af05187
                kernel.AutoFunctionInvocationFilters.Add(new GenericAgentPruneFilter(skillSet.MaxIterations, skillSet.MaxHistoryTokens));

                if (isUniversal)
                {
                    var skillPrompt = await BuildUniversalSkillPromptAsync(
                        dsId, userMessage, chatHistory, kernel, callbacks, ct).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(skillPrompt))
                        systemPrompt = (systemPrompt ?? "") + "\n\n" + skillPrompt;
                    var specialistPrompt = BuildUniversalSpecialistPrompt(dsId);
                    if (!string.IsNullOrWhiteSpace(specialistPrompt))
                        systemPrompt = (systemPrompt ?? "") + "\n\n" + specialistPrompt;
                }

                // Wrap AppAgentToolRegister rows as KernelFunctions (agent-owned + subscribed library tools).
                // PromptHint rows gate prompt inject only — not registered as callable KernelFunctions.
                var toolRows = isUniversal
                    ? (dsId > 0 ? TbToolBL.GetAllActiveLibraryTools(dsId) : TbToolBL.GetAllActiveLibraryTools())
                    : (dsId > 0 ? TbToolBL.GetBySkillKeyWithLibraries(skillKey, dsId) : TbToolBL.GetBySkillKeyWithLibraries(skillKey));
                toolRows = toolRows ?? new List<TbToolDto>();
                if (isUniversal)
                {
                    // call_agent would invoke a Specialist without the confirmed hand-off chat.
                    toolRows = toolRows
                        .Where(r => r != null && !string.Equals(r.ToolName, "call_agent", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
<<<<<<< HEAD
                // Layer 2/3 memory: saved facts and similar past conversations. On for agents with the InjectMemory
                // flag, or that subscribe to the platform-memory library (the no-code switch in the library tab).
                if (identity.HasValue && userId > 0
                    && (AgentMemoryInjector.IsEnabled(skillSet.CapabilityFlags) || AgentMemoryInjector.HasMemoryTools(toolRows.Select(r => r.ToolName))))
                {
                    try
                    {
                        systemPrompt += await AgentMemoryInjector.BuildAsync(
                            identity.Value, skillKey, userId, dsId, userMessage, chatHistory, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception swallowed) { SwallowLog.Write(swallowed, "memory inject must not fail the run"); }
                }
                var callableTools = toolRows.Where(r => !IsPromptHintTool(r)).ToList();
=======
                if (HasNamedTool(toolRows, DataAnalyzeToolName))
                {
                    if (systemPrompt.IndexOf("## Analysis path (platform hard rule", StringComparison.OrdinalIgnoreCase) < 0)
                        systemPrompt += AnalysisPathHardRule;
                    if (systemPrompt.IndexOf("Follow-up suggestions (soft chips)", StringComparison.OrdinalIgnoreCase) < 0)
                        systemPrompt += FollowupsPromptHint;
                }
                // Skip BuiltIn stubs with empty TypeName/MethodName (UI default) — they surface as
                // "BuiltIn type or method not found: ." and shadow real MCP tools with the same name.
                var callableTools = toolRows
                    .Where(r => !IsPromptHintTool(r) && !IsBrokenBuiltInTool(r))
                    .ToList();
>>>>>>> 3ca7037a509e2e7b3582f25c87da8e293af05187
                if (callableTools.Count > 0)
                    kernel.Plugins.AddFromFunctions("tools",
                        callableTools.Select(r => WrapRegisteredTool(r, context, skillSet.MaxToolResultChars, instancePool)).ToArray());

                // Interactive: ensure ask_user is always available (even if not subscribed via library).
                // Deterministic: never inject — ask_user returns error if somehow invoked.
                if (!context.IsDeterministic
                    && !KernelHasFunction(kernel, "ask_user"))
                {
                    kernel.Plugins.AddFromFunctions("hitl", new[] { CreateAskUserKernelFunction(context) });
                }

                if (isUniversal && !KernelHasFunction(kernel, "propose_specialist_handoff"))
                {
                    kernel.Plugins.AddFromFunctions("universal", new[]
                    {
                        CreateHandoffProposalKernelFunction(dsId, callbacks)
                    });
                }

                // Connect MCP servers (agent-owned + subscribed library MCP servers)
                var mcpClients = new List<McpClient>();
                var mcpServers = isUniversal
                    ? (dsId > 0 ? TbMcpBL.GetAllActiveLibraryServers(dsId) : TbMcpBL.GetAllActiveLibraryServers())
                    : (dsId > 0 ? TbMcpBL.GetBySkillKeyWithLibraries(skillKey, dsId) : TbMcpBL.GetBySkillKeyWithLibraries(skillKey));
                mcpServers = mcpServers ?? new List<TbMcpDto>();
                List<App.BL.TenantBusiness.AppAgentToolExclusionDto> exclusions = new List<App.BL.TenantBusiness.AppAgentToolExclusionDto>();
                if (!isUniversal)
                {
                try
                {
                    exclusions = dsId > 0
                        ? App.BL.TenantBusiness.AppAgentToolExclusionBL.GetBySkillKey(skillKey, dsId)
                        : App.BL.TenantBusiness.AppAgentToolExclusionBL.GetBySkillKey(skillKey);
                }
                catch (Exception ex)
                {
                    log.Warn(ex, "Tool exclusions not loaded — using all tools");
                    exclusions = new List<App.BL.TenantBusiness.AppAgentToolExclusionDto>();
                }
                }

                foreach (var srv in mcpServers)
                {
                    if (!string.Equals(srv.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.IsNullOrWhiteSpace(srv.ServerUrl)) continue;
                    try
                    {
                        var excludedMcp = App.BL.TenantBusiness.AppAgentToolExclusionBL.ExcludedNamesForLibrary(exclusions, srv.SkillKey);
                        if (isUniversal)
                            excludedMcp.Add("call_agent");
                        var (client, plugin) = await CreateMcpPluginAsync(srv, skillSet.MaxToolResultChars, excludedMcp, context, ct).ConfigureAwait(false);
                        mcpClients.Add(client);
                        kernel.Plugins.Add(plugin);
                    }
                    catch (Exception ex) { log.Warn(ex, $"MCP server {srv.ServerUrl} skipped"); }
                }

                try
                {
                    var canPersistSummary = userId > 0 && !string.IsNullOrWhiteSpace(resolvedChatKey);
                    var storedSummary = canPersistSummary
                        ? AgentSessionSummaryBL.Load(resolvedChatKey, skillKey, userId, dsId)
                        : null;
                    var history = await ChatHistoryBuilder.BuildAsync(
                        chatHistory, skillSet.RecentWindowSize, skillSet.SummarizeThreshold, kernel,
                        storedSummary,
                        canPersistSummary ? s => AgentSessionSummaryBL.Save(resolvedChatKey, skillKey, userId, dsId, s) : null)
                        .ConfigureAwait(false);
                    history.AddUserMessage(userMessage);

                    // Only enable auto tool calling when tools are actually registered;
                    // Gemini (and some other APIs) reject an empty tools array.
                    var hasTools = kernel.Plugins.Any(p => p.Any());
                    var execSettings = new PromptExecutionSettings();
                    if (hasTools) execSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

                    var agent = new ChatCompletionAgent
                    {
                        Kernel       = kernel,
                        Name         = "Assistant",
                        Instructions = systemPrompt,
                        Arguments    = new KernelArguments(execSettings)
                    };

                    if (enableThinkMode)
                    {
                        var opening = AgentThinkNarrator.Opening(userMessage);
                        if (!string.IsNullOrWhiteSpace(opening))
                        {
                            await Safe(callbacks?.OnStep, new AgentStepEvent
                            {
                                Type = "thinking",
                                Description = opening,
                                IsSuccess = true
                            }).ConfigureAwait(false);
                        }
                    }

                    var fullResponse = new StringBuilder();
                    var thread = new ChatHistoryAgentThread(history);
                    // Capture model prose that accompanies tool rounds as Think Mode (not the final answer).
                    var invokeOptions = new AgentInvokeOptions
                    {
                        OnIntermediateMessage = async msg =>
                        {
                            if (!enableThinkMode || msg == null) return;
                            var prose = (msg.Content ?? "").Trim();
                            if (string.IsNullOrEmpty(prose)) return;
                            var hasTool = false;
                            if (msg.Items != null)
                            {
                                foreach (var item in msg.Items)
                                {
                                    if (item is FunctionCallContent)
                                    {
                                        hasTool = true;
                                        break;
                                    }
                                }
                            }
                            if (!hasTool) return;
                            await Safe(callbacks?.OnStep, new AgentStepEvent
                            {
                                Type = "thinking",
                                Description = TruncateThink(prose, 2000),
                                IsSuccess = true
                            }).ConfigureAwait(false);
                        }
                    };
                    await foreach (var chunk in agent.InvokeStreamingAsync(thread, invokeOptions, cancellationToken: ct).ConfigureAwait(false))
                    {
                        var text = chunk.Message.Content ?? "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            fullResponse.Append(text);
                            await Safe(callbacks?.OnToken, text).ConfigureAwait(false);
                        }
                    }

                    runSw.Stop();
                    log.Info($"[Agent] DONE skill={skillKey} total={runSw.ElapsedMilliseconds}ms");
                    await Safe(callbacks?.OnDone, fullResponse.ToString()).ConfigureAwait(false);
                }
                finally
                {
                    foreach (var c in mcpClients)
                        try
                        {
                            if (c is IAsyncDisposable d) await d.DisposeAsync().ConfigureAwait(false);
                            else if (c is IDisposable s) s.Dispose();
                        }
                        catch (Exception swallowed) { SwallowLog.Write(swallowed); }
                }
            }
            catch (OperationCanceledException)
            {
                runSw.Stop();
                log.Info($"[Agent] STOPPED skill={skillKey} total={runSw.ElapsedMilliseconds}ms");
                await Safe(callbacks?.OnDone, "Stopped by user.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested
                    || ex is OperationCanceledException
                    || ex.GetBaseException() is OperationCanceledException)
                {
                    runSw.Stop();
                    log.Info($"[Agent] STOPPED skill={skillKey} total={runSw.ElapsedMilliseconds}ms");
                    await Safe(callbacks?.OnDone, "Stopped by user.").ConfigureAwait(false);
                    return;
                }
                log.Error(ex, $"GenericAgentEngine [{skillKey}]");
                var msg = ex.Message;
                // SK's HttpOperationException carries the raw response body in ResponseContent
                if (ex is HttpOperationException httpOpEx && !string.IsNullOrWhiteSpace(httpOpEx.ResponseContent))
                    msg += " | " + httpOpEx.ResponseContent;
                else if (ex.InnerException != null && ex.InnerException.Message != ex.Message)
                    msg += " — " + ex.InnerException.Message;
                await Safe(callbacks?.OnError, "Agent error: " + msg).ConfigureAwait(false);
            }
            finally
            {
                AgentHitlBridge.UnbindSession(boundChatKey);
                AgentHitlBridge.Current = previousHitl;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
<<<<<<< HEAD
=======
        // Kernel builder
        // ─────────────────────────────────────────────────────────────────────

        private static TbSkillSetDto BuildUniversalSkillSet() =>
            new TbSkillSetDto(
                SkillKey: UniversalAgentConstants.SkillKey,
                DisplayName: "Universal Agent",
                Description: "Built-in general agent",
                SystemPrompt: UniversalBasePrompt,
                CapabilityFlags: 0,
                IsActive: true,
                SortOrder: 0,
                Version: 1,
                MaxHistoryTokens: 80000,
                SummarizeThreshold: 60000,
                MaxToolResultChars: 4000,
                RecentWindowSize: 10,
                MaxIterations: 40,
                ExecutionMode: "Interactive",
                AgentUi: 1,
                AllowAgentFirstTurn: false,
                EnableThinkMode: false);

        private static async Task<string> BuildUniversalSkillPromptAsync(
            int dataSourceId,
            string userMessage,
            List<JObject> chatHistory,
            Kernel kernel,
            GenericAgentCallbacks callbacks,
            CancellationToken ct)
        {
            if (dataSourceId <= 0) return "";

            List<AppAISkillDto> catalog;
            try
            {
                catalog = SkillLib.GetActiveSkillCatalog(dataSourceId) ?? new List<AppAISkillDto>();
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal skill catalog load failed");
                return "";
            }
            if (catalog.Count == 0) return "";

            var selectedIds = await SelectUniversalSkillIdsAsync(catalog, userMessage, chatHistory, kernel, ct).ConfigureAwait(false);
            if (selectedIds.Count == 0) return "";

            var selected = catalog
                .Where(s => selectedIds.Contains(s.SkillId))
                .Take(UniversalAgentConstants.MaxSelectedSkills)
                .ToList();
            if (selected.Count == 0) return "";

            var sb = new StringBuilder();
            var loaded = new List<AppAISkillDto>();
            sb.AppendLine("## Loaded Skills");
            sb.AppendLine("The following reusable skills were selected from the current Universal Agent context. Follow them when relevant.");
            foreach (var skill in selected)
            {
                var content = "";
                try
                {
                    content = SkillLib.GetComposedSkillPrompt(dataSourceId, skill.SkillId, skipUnderscoreRefs: true) ?? "";
                }
                catch (Exception ex)
                {
                    SwallowLog.Write(ex, "Universal skill compose failed");
                }
                if (string.IsNullOrWhiteSpace(content)) continue;

                loaded.Add(skill);
                sb.AppendLine();
                sb.AppendLine($"### Skill: {skill.Name}");
                if (!string.IsNullOrWhiteSpace(skill.Description))
                    sb.AppendLine(skill.Description.Trim());
                sb.AppendLine();
                sb.AppendLine(content.Trim());
            }

            if (loaded.Count == 0) return "";

            if (sb.Length > 0)
            {
                await Safe(callbacks?.OnStep, new AgentStepEvent
                {
                    Type = "thinking",
                    ToolName = "skill_selection",
                    Description = "Loaded skills: " + string.Join(", ", loaded.Select(s => s.Name)),
                    Details = string.Join("\n", loaded.Select(s => $"{s.SkillId}: {s.Name}")),
                    IsSuccess = true
                }).ConfigureAwait(false);
            }
            return sb.ToString();
        }

        private static string BuildUniversalSpecialistPrompt(int dataSourceId)
        {
            try
            {
                var agents = dataSourceId > 0 ? TbSkillBL.GetAll(dataSourceId) : TbSkillBL.GetAll();
                var rows = (agents ?? new List<TbSkillSetDto>())
                    .Where(a => a != null && !UniversalAgentConstants.IsUniversalAgentKey(a.SkillKey))
                    .Take(30)
                    .Select(a => $"- {a.DisplayName ?? a.SkillKey} ({a.SkillKey}): {a.Description}")
                    .ToList();
                if (rows.Count == 0) return "";

                var sb = new StringBuilder();
                sb.AppendLine("## Available Specialist Agents");
                sb.AppendLine("These configured Specialist Agents can be better entry points for focused workflows:");
                foreach (var row in rows) sb.AppendLine(row);
                sb.AppendLine();
                sb.AppendLine("When the next step is clearly better handled by exactly one Specialist, call propose_specialist_handoff.");
                sb.AppendLine("Pass that agent's skillKey as targetSkillKey and a short reason. Do not call call_agent.");
                sb.AppendLine("The user must confirm before any Specialist chat opens. Do not claim the chat was switched.");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal specialist catalog load failed");
                return "";
            }
        }

        private static async Task<HashSet<int>> SelectUniversalSkillIdsAsync(
            List<AppAISkillDto> catalog,
            string userMessage,
            List<JObject> chatHistory,
            Kernel kernel,
            CancellationToken ct)
        {
            HashSet<int> Fallback() => SelectUniversalSkillIdsByKeyword(catalog, userMessage, chatHistory);
            try
            {
                var chat = kernel.GetRequiredService<IChatCompletionService>();
                var prompt = new ChatHistory();
                prompt.AddSystemMessage(
                    "Select up to 3 relevant skills for the user's current task. " +
                    "Return only JSON like {\"skillIds\":[1,2]}. Return an empty array when no skill is relevant.");
                var catalogText = string.Join("\n", catalog.Select(s =>
                    $"- {s.SkillId}: {s.Name} - {s.Description}"));
                prompt.AddUserMessage(
                    "Skill catalog:\n" + catalogText +
                    "\n\nRecent chat:\n" + BuildRecentPlainChat(chatHistory, 6) +
                    "\n\nCurrent user message:\n" + (userMessage ?? ""));
                var result = await chat.GetChatMessageContentAsync(prompt, cancellationToken: ct).ConfigureAwait(false);
                if (TryParseSkillSelection(result?.Content, catalog, out var ids))
                    return ids;
                return new HashSet<int>();
            }
            catch (Exception ex)
            {
                SwallowLog.Write(ex, "Universal skill model selection failed");
                return Fallback();
            }
        }

        /// <summary>
        /// True when the model returned a skillIds JSON object, including an empty array.
        /// Keyword fallback is only for a failed model call, not for an explicit empty selection.
        /// </summary>
        private static bool TryParseSkillSelection(string text, List<AppAISkillDto> catalog, out HashSet<int> ids)
        {
            ids = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(text)) return false;
            var raw = text.Trim();
            var fence = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (fence.Success) raw = fence.Groups[1].Value.Trim();
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) return false;
            try
            {
                var obj = JObject.Parse(raw.Substring(start, end - start + 1));
                var arr = obj["skillIds"] as JArray ?? obj["SkillIds"] as JArray;
                if (arr == null) return true;
                var allowed = new HashSet<int>(catalog.Select(s => s.SkillId));
                foreach (var token in arr)
                {
                    if (ids.Count >= UniversalAgentConstants.MaxSelectedSkills) break;
                    if (int.TryParse(token?.ToString(), out var id) && allowed.Contains(id))
                        ids.Add(id);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<int> SelectUniversalSkillIdsByKeyword(
            List<AppAISkillDto> catalog,
            string userMessage,
            List<JObject> chatHistory)
        {
            var hay = ((userMessage ?? "") + "\n" + BuildRecentPlainChat(chatHistory, 4)).ToLowerInvariant();
            var scored = catalog
                .Select(s => new
                {
                    Skill = s,
                    Score = ScoreSkill(hay, ((s.Name ?? "") + " " + (s.Description ?? "")).ToLowerInvariant())
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Skill.Name)
                .Take(UniversalAgentConstants.MaxSelectedSkills)
                .Select(x => x.Skill.SkillId);
            return new HashSet<int>(scored);
        }

        private static int ScoreSkill(string hay, string needleText)
        {
            if (string.IsNullOrWhiteSpace(hay) || string.IsNullOrWhiteSpace(needleText)) return 0;
            var score = 0;
            foreach (Match m in Regex.Matches(needleText, @"[a-zA-Z0-9_]{3,}"))
            {
                if (hay.Contains(m.Value.ToLowerInvariant()))
                    score++;
            }
            return score;
        }

        private static string BuildRecentPlainChat(List<JObject> chatHistory, int take)
        {
            if (chatHistory == null || chatHistory.Count == 0) return "";
            var rows = chatHistory
                .Where(m => m != null)
                .Skip(Math.Max(0, chatHistory.Count - Math.Max(1, take)))
                .Select(m => $"{m.Value<string>("role")}: {ExtractContentText(m["content"])}");
            return string.Join("\n", rows);
        }

        private static string ExtractContentText(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.String) return token.ToString();
            if (token.Type == JTokenType.Array)
                return string.Join(" ", token.Select(ExtractContentText));
            if (token.Type == JTokenType.Object)
                return token["text"]?.ToString() ?? token["content"]?.ToString() ?? token.ToString(Newtonsoft.Json.Formatting.None);
            return token.ToString();
        }

        public static async Task<string> CompleteOnceAsync(
            AppClientIdentity? identity,
            string systemPrompt,
            string userPrompt,
            CancellationToken ct)
        {
            var kernel = BuildKernel(identity);
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userPrompt ?? "");
            var result = await chat.GetChatMessageContentAsync(history, cancellationToken: ct).ConfigureAwait(false);
            return result?.Content ?? "";
        }

        private static KernelFunction CreateHandoffProposalKernelFunction(int dataSourceId, GenericAgentCallbacks callbacks)
        {
            var parameters = new List<KernelParameterMetadata>
            {
                new("targetSkillKey")
                {
                    Description = "SkillKey of the one active Specialist Agent to propose. Not the display name unless it matches exactly.",
                    IsRequired = true,
                    ParameterType = typeof(string)
                },
                new("reason")
                {
                    Description = "One or two sentences explaining why this Specialist is the better next step.",
                    IsRequired = true,
                    ParameterType = typeof(string)
                }
            };

            return KernelFunctionFactory.CreateFromMethod(
                async (KernelArguments args, CancellationToken ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    string Arg(string name) =>
                        args.TryGetValue(name, out var v) && v != null ? v.ToString() : "";

                    var target = (Arg("targetSkillKey") ?? "").Trim();
                    var reason = (Arg("reason") ?? "").Trim();
                    var agent = UniversalAgentWorkflowBL.FindActiveSpecialist(dataSourceId, target);
                    if (agent == null)
                        return "Handoff was not shown. targetSkillKey is not an active Specialist. Use a skillKey from Available Specialist Agents.";

                    var name = string.IsNullOrWhiteSpace(agent.DisplayName) ? agent.SkillKey : agent.DisplayName.Trim();
                    if (string.IsNullOrWhiteSpace(reason))
                        reason = name + " is a better fit for the next step.";

                    var payload = new JObject
                    {
                        ["targetSkillKey"] = agent.SkillKey,
                        ["name"] = name,
                        ["reason"] = reason.Length > 500 ? reason.Substring(0, 500) : reason,
                        ["description"] = agent.Description ?? ""
                    };

                    if (callbacks?.OnStep != null)
                    {
                        await callbacks.OnStep(new AgentStepEvent
                        {
                            Type = "handoff_proposal",
                            Description = "Handoff proposed: " + name,
                            Details = payload.ToString(Newtonsoft.Json.Formatting.None),
                            IsSuccess = true
                        }).ConfigureAwait(false);
                    }

                    return "Shown a handoff card for " + name + " (" + agent.SkillKey + "). "
                        + "The user has not confirmed. Stay in Universal Agent and do not claim the chat was switched.";
                },
                functionName: "propose_specialist_handoff",
                description: "Propose moving this task to one Specialist Agent. Shows a confirmation card. Does not open or continue a Specialist chat.",
                parameters: parameters);
        }

        private static Kernel BuildKernel(AppClientIdentity? identity)
        {
            EmLLMProvider provider;
            string        apiKey;
            string        model;

            if (identity.HasValue)
            {
                var providerStr = AIConfigSettingBL.GetProvider(identity.Value) ?? "";
                provider = Enum.TryParse<EmLLMProvider>(providerStr, true, out var parsed) ? parsed : EmLLMProvider.Gemini;
                apiKey   = AIConfigSettingBL.GetApiKey(identity.Value) ?? "";
                model    = AIConfigSettingBL.GetModel(identity.Value)  ?? "";
            }
            else
            {
                provider = KernelProviderHelper.GetProvider();
                apiKey   = KernelProviderHelper.GetApiKey() ?? "";
                model    = KernelProviderHelper.GetModel()  ?? "";
            }

            var builder = Kernel.CreateBuilder();
            switch (provider)
            {
                case EmLLMProvider.Anthropic:
                    // No official SK connector package for Anthropic — use a custom wrapper.
                    builder.Services.AddSingleton<IChatCompletionService>(new AnthropicChatCompletionService(model, apiKey));
                    break;
                case EmLLMProvider.Gemini:
                    // Bound hang risk: default HttpClient timeout is 100s and can stall forever on
                    // some streaming paths. Keep a hard ceiling; CancelRun still cancels via CT.
                    var geminiHttp = new HttpClient(new GeminiRoleFixHandler())
                    {
                        Timeout = TimeSpan.FromMinutes(10)
                    };
                    builder.AddGoogleAIGeminiChatCompletion(model, apiKey, httpClient: geminiHttp);
                    break;
                default:
                    builder.AddOpenAIChatCompletion(model, apiKey);
                    break;
            }
            return builder.Build();
        }

        // ─────────────────────────────────────────────────────────────────────
>>>>>>> 3ca7037a509e2e7b3582f25c87da8e293af05187
        // Registered tool wrapper (AppAgentToolEngine → KernelFunction)
        // ─────────────────────────────────────────────────────────────────────

        private static KernelFunction WrapRegisteredTool(TbToolDto row, AgentToolContext context, int maxChars, Dictionary<string, object>? instancePool = null)
        {
            var parameters = ParseKernelParameters(row.ParameterSchemaJson);
            var cap        = maxChars > 0 ? maxChars : DefaultMaxToolResultChars;
            var toolType   = row.ToolType;
            var toolConfig = row.ToolConfig;

            return KernelFunctionFactory.CreateFromMethod(
                async (KernelArguments args, CancellationToken ct) =>
                {
                    var strArgs = args
                        .Where(kv => kv.Value != null)
                        .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                    var result = await AppAgentToolEngine.Dispatch(toolType, toolConfig, strArgs, context, ct, instancePool, row.ToolName)
                                                         .ConfigureAwait(false);
                    result = DataAnalyze.AgentDataAnalyzeHook.MaybeCacheAndRewrite(context, toolType, row.ToolName, result, strArgs);
                    return CapResult(result, cap);
                },
                functionName: SanitizeName(row.ToolName),
                description:  row.Description ?? row.ToolName,
                parameters:   parameters);
        }

        private static bool KernelHasFunction(Kernel kernel, string functionName)
        {
            if (kernel == null || string.IsNullOrWhiteSpace(functionName)) return false;
            return kernel.Plugins
                .SelectMany(p => p)
                .Any(f => string.Equals(f.Name, functionName, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(f.Name, SanitizeName(functionName), StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsPromptHintTool(TbToolDto row) =>
            row != null
            && string.Equals(row.ToolType, PromptHintToolType, StringComparison.OrdinalIgnoreCase);

        private static bool HasNamedTool(IEnumerable<TbToolDto> tools, string toolName) =>
            tools != null
            && tools.Any(t => string.Equals(t?.ToolName, toolName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Auto-injected ask_user for Interactive agents when the library tool is not subscribed.
        /// AgentToolContext is closed over — not exposed as an LLM parameter.
        /// </summary>
        private static KernelFunction CreateAskUserKernelFunction(AgentToolContext context)
        {
            var parameters = new List<KernelParameterMetadata>
            {
                new("prompt")
                {
                    Description = "Question shown to the user",
                    IsRequired = true,
                    ParameterType = typeof(string)
                },
                new("mode")
                {
                    Description = "text | single_choice | multi_choice",
                    IsRequired = false,
                    ParameterType = typeof(string),
                    DefaultValue = "text"
                },
                new("fieldsJson")
                {
                    Description =
                        "JSON array of {name,label,required?,type?,options?} — type=text|select|radio. "
                        + "select=HTML dropdown, radio=radio buttons; both require options [{id,display}].",
                    IsRequired = false,
                    ParameterType = typeof(string)
                },
                new("optionsJson")
                {
                    Description = "REQUIRED for single_choice/multi_choice: JSON array LookupItemDto [{id,display}]. Empty/missing = no choice UI (do not list choices in prompt instead).",
                    IsRequired = false,
                    ParameterType = typeof(string)
                },
                new("contextKey")
                {
                    Description = "Optional shared-context key to merge answers into",
                    IsRequired = false,
                    ParameterType = typeof(string)
                },
                new("ui")
                {
                    Description =
                        "single_choice UI: button_group (default menus) | radio | dropdown. "
                        + "Synonyms for dropdown: ddl, drop down, combobox, option list, select. "
                        + "If the user/system prompt says ddl/dropdown/combobox, set ui=dropdown.",
                    IsRequired = false,
                    ParameterType = typeof(string)
                },
                new("layout")
                {
                    Description = "button_group layout: vertical (default) | horizontal",
                    IsRequired = false,
                    ParameterType = typeof(string),
                    DefaultValue = "vertical"
                }
            };

            var plugin = new AgentAskUserPlugin();
            return KernelFunctionFactory.CreateFromMethod(
                async (KernelArguments args, CancellationToken ct) =>
                {
                    string Arg(string name, string fallback = null) =>
                        args.TryGetValue(name, out var v) && v != null ? v.ToString() : fallback;

                    return await plugin.AskUser(
                        prompt: Arg("prompt", "") ?? "",
                        context: context,
                        mode: Arg("mode", "text"),
                        fieldsJson: Arg("fieldsJson"),
                        optionsJson: Arg("optionsJson"),
                        contextKey: Arg("contextKey"),
                        ui: Arg("ui"), // null → plugin defaults / infers from prompt keywords
                        layout: Arg("layout", "vertical")).ConfigureAwait(false);
                },
                functionName: "ask_user",
                description:
                    "System built-in (auto-injected for Interactive agents — not a library/private tool). "
                    + "Ask the user a structured question and wait for their answer. "
                    + "For large row fetches: call execute_sql first (platform COUNTs). If it returns need_ask_user, call ask_user with that prompt/optionsJson/contextKey=confirm_row_limit (include the counted row total). "
                    + "For analysis-like asks (group-by / ranges / multi-part metrics): MUST call with contextKey=confirm_kpi_dashboard, mode=single_choice, "
                    + "optionsJson=[{\"id\":\"kpi_dashboard\",\"label\":\"KPI / Dashboard — batch analysis\"},{\"id\":\"fetch_display\",\"label\":\"Only fetch and show the data I asked for\"}] BEFORE data_analyze or inventing tables. "
                    + "mode=text|single_choice|multi_choice. "
                    + "Choice UI (single_choice + optionsJson): "
                    + "ui=button_group (clickable buttons, default for menus), "
                    + "ui=radio (radio + Submit), "
                    + "ui=dropdown (HTML drop-down + Submit). "
                    + "When instructions say ddl / drop down / dropdown / combobox / option list / select list, "
                    + "MUST use mode=single_choice + ui=dropdown + optionsJson [{id,display}]. "
                    + "When they say radio buttons, use ui=radio (or fieldsJson type=radio on a form). "
                    + "When they say buttons/button group, use ui=button_group. "
                    + "Never put choices only in prompt text. "
                    + "fieldsJson: type=text|select|radio (select=DDL, radio=radio buttons; both need options).",
                parameters: parameters);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MCP plugin factory (McpClient + StreamableHttp → KernelPlugin)
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<(McpClient Client, KernelPlugin Plugin)> CreateMcpPluginAsync(
            TbMcpDto server, int maxChars, HashSet<string> excludedTools, AgentToolContext context, CancellationToken ct)
        {
            McpConnectionHelper.EnsureAllowed(server);
            // The in-house gateway is called as the signed-in user, never with a shared stored credential.
            var userToken = McpConnectionHelper.RequiresUserToken(server.ServerUrl)
                ? McpConnectionHelper.GetUserToken(server, context.UserId, context.CompanyId)
                : null;
            var transportOptions = McpConnectionHelper.BuildTransportOptions(server, userToken: userToken);
            var transport = new HttpClientTransport(transportOptions, McpHttpClient, NullLoggerFactory.Instance, ownsHttpClient: false);
            var client    = await McpClient.CreateAsync(transport, cancellationToken: ct).ConfigureAwait(false);
            var tools     = await client.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);

            var cap       = maxChars > 0 ? maxChars : DefaultMaxToolResultChars;
            var functions = tools.Where(t => excludedTools == null || !excludedTools.Contains(t.Name))
                                 .Select(t => BuildMcpKernelFunction(client, t, cap, context)).ToArray();
            var plugin    = KernelPluginFactory.CreateFromFunctions(McpPluginName(server.ServerName), functions);
            return (client, plugin);
        }

        // Plugin names must contain no "_" — SK's Gemini connector calls tools as "plugin_function" and
        // splits at the FIRST "_", so "mcp_plm_Tool" would resolve to plugin "mcp" and never be found.
        // Kept short because providers cap the combined "plugin_function" name at 64 chars.
        private static string McpPluginName(string? serverName)
        {
            var alnum = Regex.Replace(serverName ?? "", @"[^a-zA-Z0-9]", "");
            if (alnum.Length > 16) alnum = alnum.Substring(0, 16);
            return "mcp" + (alnum.Length > 0 ? alnum : "server");
        }

        private static KernelFunction BuildMcpKernelFunction(McpClient client, McpClientTool tool, int cap, AgentToolContext context)
        {
            var toolName    = tool.Name;
            var description = tool.Description ?? toolName;
            var parameters  = new List<KernelParameterMetadata>();

            if (tool.JsonSchema is JsonElement schema && schema.TryGetProperty("properties", out var props))
            {
                var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (schema.TryGetProperty("required", out var req))
                    foreach (var r in req.EnumerateArray())
                        if (r.GetString() is string s) required.Add(s);

                foreach (var prop in props.EnumerateObject())
                {
                    var paramDesc  = prop.Value.TryGetProperty("description", out var d) ? d.GetString() : null;
                    var schemaJson = JsonSerializer.Serialize(prop.Value);
                    parameters.Add(new KernelParameterMetadata(prop.Name)
                    {
                        Description = paramDesc,
                        IsRequired  = required.Contains(prop.Name),
                        Schema      = KernelJsonSchema.Parse(schemaJson)
                    });
                }
            }

            var returnParameter = new KernelReturnParameterMetadata
            {
                Schema = KernelJsonSchema.Parse("{\"type\":\"string\"}")
            };

            return KernelFunctionFactory.CreateFromMethod(
                async (KernelArguments args, CancellationToken ct) =>
                {
                    // SK/Gemini often pass JsonElement / objects; gateway tools (queryParamsJson) need
                    // real JSON strings, and typed MCP params need objects — normalize both.
                    var mcpArgs = NormalizeMcpArgs(args);

                    using var callCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    callCts.CancelAfter(TimeSpan.FromSeconds(McpToolCallTimeoutSeconds));

                    try
                    {
                        var result = await client.CallToolAsync(
                            toolName,
                            mcpArgs,
                            cancellationToken: callCts.Token).ConfigureAwait(false);

                        var text = string.Join("\n", result.Content
                            .OfType<TextContentBlock>()
                            .Select(c => c.Text ?? ""));

                        // Cache full payload before CapResult — char cap must never shrink dataset_cached rows.
                        text = DataAnalyze.AgentDataAnalyzeHook.MaybeCacheAndRewrite(context, "MCP", toolName, text);
                        return CapResult(text, cap);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        return JsonSerializer.Serialize(new
                        {
                            Error = $"MCP tool '{toolName}' timed out after {McpToolCallTimeoutSeconds}s. Retry or narrow filters."
                        });
                    }
                },
                functionName:    SanitizeName(toolName),
                description:     description,
                parameters:      parameters,
                returnParameter: returnParameter);
        }

        /// <summary>
        /// Convert SK KernelArguments into MCP JSON-RPC argument values.
        /// JsonElement/JToken → CLR primitives or parsed JSON; keep objects for typed schemas;
        /// string-named *Json params that arrive as objects are serialized to JSON text.
        /// </summary>
        private static IReadOnlyDictionary<string, object?> NormalizeMcpArgs(KernelArguments args)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (args == null) return dict;
            foreach (var kv in args)
            {
                if (kv.Value == null) continue;
                dict[kv.Key] = NormalizeMcpArgValue(kv.Key, kv.Value);
            }
            return dict;
        }

        private static object? NormalizeMcpArgValue(string name, object value)
        {
            var wantsJsonString = name != null
                && (name.EndsWith("Json", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith("_json", StringComparison.OrdinalIgnoreCase));

            switch (value)
            {
                case string s:
                    return s;
                case JsonElement je:
                    if (wantsJsonString)
                        return je.ValueKind == JsonValueKind.String ? je.GetString() : je.GetRawText();
                    return JsonElementToMcpValue(je);
                case JToken jt:
                    if (wantsJsonString)
                        return jt.Type == JTokenType.String ? jt.ToString() : jt.ToString(Newtonsoft.Json.Formatting.None);
                    if (jt.Type == JTokenType.String) return jt.ToString();
                    if (jt.Type == JTokenType.Integer) return jt.Value<long>();
                    if (jt.Type == JTokenType.Float) return jt.Value<double>();
                    if (jt.Type == JTokenType.Boolean) return jt.Value<bool>();
                    if (jt.Type == JTokenType.Null) return null;
                    return JsonSerializer.Deserialize<object>(jt.ToString(Newtonsoft.Json.Formatting.None));
                case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    return value;
                default:
                    if (wantsJsonString)
                    {
                        try { return JsonSerializer.Serialize(value); }
                        catch { return value.ToString(); }
                    }
                    return value;
            }
        }

        private static object? JsonElementToMcpValue(JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String: return je.GetString();
                case JsonValueKind.Number:
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDouble(out var d)) return d;
                    return je.GetRawText();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined: return null;
                default:
                    // Object / Array — keep as parsed structure for typed MCP schemas.
                    return JsonSerializer.Deserialize<object>(je.GetRawText());
            }
        }

        private static bool IsBrokenBuiltInTool(TbToolDto row)
        {
            if (row == null) return true;
            if (!string.Equals(row.ToolType, "BuiltIn", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(row.ToolType))
                return false;
            try
            {
                var obj = JObject.Parse(string.IsNullOrWhiteSpace(row.ToolConfig) ? "{}" : row.ToolConfig);
                var typeName = (obj["TypeName"] ?? obj["typeName"])?.ToString()?.Trim();
                var methodName = (obj["MethodName"] ?? obj["methodName"])?.ToString()?.Trim();
                return string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodName);
            }
            catch
            {
                return true;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static List<KernelParameterMetadata> ParseKernelParameters(string? schemaJson)
        {
            var result = new List<KernelParameterMetadata>();
            if (string.IsNullOrWhiteSpace(schemaJson)) return result;
            try
            {
                var schema = JObject.Parse(schemaJson);

                // Standard JSON Schema: {"properties":{...}, "required":[...]}
                var props = schema["properties"] as JObject;
                if (props != null)
                {
                    var required = new HashSet<string>(
                        (schema["required"] as JArray)?.Select(t => t.ToString()) ?? Enumerable.Empty<string>(),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in props.Properties())
                    {
                        var desc     = prop.Value["description"]?.ToString();
                        var propJson = prop.Value.ToString(Newtonsoft.Json.Formatting.None);
                        KernelJsonSchema? ks = null;
                        try { ks = KernelJsonSchema.Parse(propJson); } catch (Exception swallowed) { SwallowLog.Write(swallowed); }
                        result.Add(new KernelParameterMetadata(prop.Name)
                        {
                            Description   = desc,
                            IsRequired    = required.Contains(prop.Name),
                            ParameterType = typeof(string),
                            Schema        = ks
                        });
                    }
                    return result;
                }

                // Flat format: {"paramName":{"type":"string","description":"...","required":true}, ...}
                // Top-level keys are parameter names; each value is a parameter definition object.
                // This is the format stored in AppAgentToolRegister.ParameterSchemaJson.
                foreach (var prop in schema.Properties())
                {
                    if (prop.Value is not JObject def) continue;
                    var desc       = def["description"]?.ToString();
                    var isRequired = def["required"]?.Value<bool>() ?? false;
                    // Remove the non-standard "required" key before sending to the LLM schema
                    var cleanDef = new JObject(def.Properties().Where(p => p.Name != "required"));
                    var propJson = cleanDef.ToString(Newtonsoft.Json.Formatting.None);
                    KernelJsonSchema? ks = null;
                    try { ks = KernelJsonSchema.Parse(propJson); } catch (Exception swallowed) { SwallowLog.Write(swallowed); }
                    result.Add(new KernelParameterMetadata(prop.Name)
                    {
                        Description   = desc,
                        IsRequired    = isRequired,
                        ParameterType = typeof(string),
                        Schema        = ks
                    });
                }
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed); }
            return result;
        }

        private static string SanitizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "tool";
            var s = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            // Gemini requires function names to start with a letter or underscore
            if (s.Length > 0 && char.IsDigit(s[0])) s = "_" + s;
            return s;
        }

        private static string CapResult(string? s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            var preview = s.Substring(0, max);
            var omitted = s.Length - max;
            var trimmed  = s.TrimStart();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                return $"{{\"note\":\"Result truncated ({omitted} chars omitted)\",\"preview\":{Newtonsoft.Json.JsonConvert.ToString(preview)}}}";
            return preview + $"… [{omitted} chars truncated]";
        }

        private static string TruncateThink(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max) + "…";
        }

        private static async Task Safe<T>(Func<T, Task>? callback, T arg)
        {
            if (callback == null) return;
            try { await callback(arg).ConfigureAwait(false); } catch (Exception swallowed) { SwallowLog.Write(swallowed); }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Auto-function invocation filter: context pruning + iteration cap
        // Mirrors AppBuilderAgentBL.PruneMessages() — runs before every LLM
        // call inside SK's agentic loop so the context window never overflows.
        // ─────────────────────────────────────────────────────────────────────

        private sealed class GenericAgentPruneFilter : IAutoFunctionInvocationFilter
        {
            private readonly int _tokenBudget;
            private readonly int _maxIterations;

            public GenericAgentPruneFilter(int maxIterations, int tokenBudget = 0)
            {
                _maxIterations = maxIterations > 0 ? maxIterations : 40;
                _tokenBudget   = tokenBudget   > 0 ? tokenBudget   : 120_000;
            }

            public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext ctx, Func<AutoFunctionInvocationContext, Task> next)
            {
                // RequestSequenceIndex is 0-based; cap at _maxIterations rounds
                if (ctx.RequestSequenceIndex >= _maxIterations)
                    throw new OperationCanceledException($"Agent stopped: reached {_maxIterations} tool-call iterations.");

                PruneHistory(ctx.ChatHistory, _tokenBudget);
                await next(ctx).ConfigureAwait(false);
            }

            private static void PruneHistory(ChatHistory? history, int tokenBudget)
            {
                if (history == null || history.Count < 4) return;
                var total = history.Sum(EstimateLen);
                if (total / 4 < tokenBudget) return;

                // Drop oldest messages (keep index 0 = original user message, keep last 2)
                for (int i = 1; i < history.Count - 2 && total / 4 >= tokenBudget; i++)
                {
                    total -= EstimateLen(history[i]);
                    history.RemoveAt(i);
                    i--;
                }
            }

            private static int EstimateLen(ChatMessageContent m) =>
                (m.Content?.Length ?? 0) + m.Items.Sum(item => (item as TextContent)?.Text?.Length ?? 0) + 50;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Gemini HTTP fix handler
        //   Workaround for SK Connectors.Google 1.74.0-alpha bug:
        //   the connector sends tool results with role "function", but the
        //   Gemini API only accepts "user" for that turn. We patch the role
        //   in every outgoing request body before the bytes leave the process.
        // ─────────────────────────────────────────────────────────────────────

        internal sealed class GeminiRoleFixHandler : DelegatingHandler
        {
            private static readonly NLog.Logger DiagLog = NLog.LogManager.GetCurrentClassLogger();

            public GeminiRoleFixHandler() : base(new HttpClientHandler()) { }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Content != null)
                {
                    var body    = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var patched = PatchFunctionRole(body);
                    request.Content = new StringContent(patched, Encoding.UTF8, "application/json");
                }

                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // Read body NOW — SK's ResponseHeadersRead streaming never reads it
                    var errBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    DiagLog.Error($"Gemini {(int)response.StatusCode}: {errBody}");
                    response.Content = new StringContent(errBody, Encoding.UTF8, "application/json");
                }

                return response;
            }

            // Targets only contents[].role — safe against "function" appearing in other values
            private static string PatchFunctionRole(string json)
            {
                try
                {
                    var j = JObject.Parse(json);
                    if (j["contents"] is JArray contents)
                        foreach (var item in contents)
                            if (item["role"]?.ToString() == "function")
                                item["role"] = "user";
                    return j.ToString(Newtonsoft.Json.Formatting.None);
                }
                catch (Exception swallowed) { SwallowLog.Write(swallowed); return json; }
            }
        }
    }
}
