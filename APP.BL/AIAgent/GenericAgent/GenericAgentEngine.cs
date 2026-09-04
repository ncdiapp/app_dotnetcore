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
        private static readonly HttpClient McpHttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        private const int DefaultMaxToolResultChars = 4000;
        private const int McpToolCallTimeoutSeconds  = 30;

        public static async Task RunAsync(
            string                skillKey,
            string                userMessage,
            List<JObject>         chatHistory,
            GenericAgentCallbacks callbacks,
            AppClientIdentity?    identity,
            CancellationToken     ct)
        {
            var log = NLog.LogManager.GetCurrentClassLogger();
            try
            {
                var dsId = identity.HasValue ? identity.Value.DataSourceId : 0;

                var skillSet = dsId > 0
                    ? TbSkillBL.GetByKey(skillKey, dsId)
                    : TbSkillBL.GetByKey(skillKey);
                if (skillSet == null)
                {
                    await Safe(callbacks?.OnError, $"Skill key not found: {skillKey}").ConfigureAwait(false);
                    return;
                }

                var userId    = identity.HasValue && identity.Value.UserId != null                      ? Convert.ToInt32(identity.Value.UserId)                      : 0;
                var companyId = identity.HasValue && identity.Value.CurrentWorkingCompanyId != null     ? Convert.ToInt32(identity.Value.CurrentWorkingCompanyId)      : 0;
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
                    IsDeterministic  = string.Equals(skillSet.ExecutionMode, "Deterministic", StringComparison.OrdinalIgnoreCase)
                };

                // Per-session instance pool keeps stateful plugin instances (e.g. SchemaDesignerPlugin)
                // alive across multiple tool calls within the same agent run.
                var instancePool = new Dictionary<string, object>(StringComparer.Ordinal);

                // Build SK kernel (provider-specific connector registered here)
                var kernel = BuildKernel(identity);
                kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks));
                kernel.AutoFunctionInvocationFilters.Add(new GenericAgentPruneFilter(skillSet.MaxIterations));

                // Wrap AppAgentToolRegister rows as KernelFunctions
                var toolRows = (dsId > 0 ? TbToolBL.GetBySkillKey(skillKey, dsId) : TbToolBL.GetBySkillKey(skillKey)) ?? new List<TbToolDto>();
                if (toolRows.Count > 0)
                    kernel.Plugins.AddFromFunctions("tools",
                        toolRows.Select(r => WrapRegisteredTool(r, context, skillSet.MaxToolResultChars, instancePool)).ToArray());

                // Connect MCP servers
                var mcpClients = new List<McpClient>();
                var mcpServers = (dsId > 0 ? TbMcpBL.GetBySkillKey(skillKey, dsId) : TbMcpBL.GetBySkillKey(skillKey)) ?? new List<TbMcpDto>();
                foreach (var srv in mcpServers)
                {
                    if (!string.Equals(srv.ServerType, "streamable-http", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.IsNullOrWhiteSpace(srv.ServerUrl)) continue;
                    try
                    {
                        var (client, plugin) = await CreateMcpPluginAsync(srv, skillSet.MaxToolResultChars, ct).ConfigureAwait(false);
                        mcpClients.Add(client);
                        kernel.Plugins.Add(plugin);
                    }
                    catch (Exception ex) { log.Warn(ex, $"MCP server {srv.ServerUrl} skipped"); }
                }

                try
                {
                    var history = BuildChatHistory(chatHistory);
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
                        Instructions = skillSet.SystemPrompt ?? "",
                        Arguments    = new KernelArguments(execSettings)
                    };

                    await Safe(callbacks?.OnStep, new AgentStepEvent
                    {
                        Type = "thinking", Description = "Analyzing your request…", IsSuccess = true
                    }).ConfigureAwait(false);

                    var fullResponse = new StringBuilder();
                    var thread = new ChatHistoryAgentThread(history);
                    await foreach (var chunk in agent.InvokeStreamingAsync(thread, cancellationToken: ct).ConfigureAwait(false))
                    {
                        var text = chunk.Message.Content ?? "";
                        if (!string.IsNullOrEmpty(text))
                        {
                            fullResponse.Append(text);
                            await Safe(callbacks?.OnToken, text).ConfigureAwait(false);
                        }
                    }

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
                        catch { }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"GenericAgentEngine [{skillKey}]");
                var msg = ex.Message;
                // SK's HttpOperationException carries the raw response body in ResponseContent
                if (ex is HttpOperationException httpOpEx && !string.IsNullOrWhiteSpace(httpOpEx.ResponseContent))
                    msg += " | " + httpOpEx.ResponseContent;
                else if (ex.InnerException != null && ex.InnerException.Message != ex.Message)
                    msg += " — " + ex.InnerException.Message;
                await Safe(callbacks?.OnError, "Agent error: " + msg).ConfigureAwait(false);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Kernel builder
        // ─────────────────────────────────────────────────────────────────────

        private static Kernel BuildKernel(AppClientIdentity? identity)
        {
            EmLLMProvider provider;
            string        apiKey;
            string        model;

            if (identity.HasValue)
            {
                var providerStr = AIConfigSettingBL.GetProvider(identity.Value) ?? "";
                provider = Enum.TryParse<EmLLMProvider>(providerStr, true, out var parsed) ? parsed : EmLLMProvider.Anthropic;
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
                    builder.AddGoogleAIGeminiChatCompletion(model, apiKey,
                        httpClient: new HttpClient(new GeminiRoleFixHandler()));
                    break;
                default:
                    builder.AddOpenAIChatCompletion(model, apiKey);
                    break;
            }
            return builder.Build();
        }

        // ─────────────────────────────────────────────────────────────────────
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
                    var result = await AppAgentToolEngine.Dispatch(toolType, toolConfig, strArgs, context, ct, instancePool)
                                                         .ConfigureAwait(false);
                    return CapResult(result, cap);
                },
                functionName: SanitizeName(row.ToolName),
                description:  row.Description ?? row.ToolName,
                parameters:   parameters);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MCP plugin factory (McpClient + StreamableHttp → KernelPlugin)
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<(McpClient Client, KernelPlugin Plugin)> CreateMcpPluginAsync(
            TbMcpDto server, int maxChars, CancellationToken ct)
        {
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint      = new Uri(server.ServerUrl),
                TransportMode = HttpTransportMode.StreamableHttp
            };
            var transport = new HttpClientTransport(transportOptions, McpHttpClient, NullLoggerFactory.Instance, ownsHttpClient: false);
            var client    = await McpClient.CreateAsync(transport, cancellationToken: ct).ConfigureAwait(false);
            var tools     = await client.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);

            var cap       = maxChars > 0 ? maxChars : DefaultMaxToolResultChars;
            var functions = tools.Select(t => BuildMcpKernelFunction(client, t, cap)).ToArray();
            var plugin    = KernelPluginFactory.CreateFromFunctions("mcp_" + SanitizeName(server.ServerName ?? "server"), functions);
            return (client, plugin);
        }

        private static KernelFunction BuildMcpKernelFunction(McpClient client, McpClientTool tool, int cap)
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
                    var mcpArgs = args
                        .Where(kv => kv.Value != null)
                        .ToDictionary(kv => kv.Key, kv => kv.Value!);

                    using var callCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    callCts.CancelAfter(TimeSpan.FromSeconds(McpToolCallTimeoutSeconds));

                    var result = await client.CallToolAsync(
                        toolName,
                        (IReadOnlyDictionary<string, object?>)mcpArgs,
                        cancellationToken: callCts.Token).ConfigureAwait(false);

                    var text = string.Join("\n", result.Content
                        .OfType<TextContentBlock>()
                        .Select(c => c.Text ?? ""));

                    return CapResult(text, cap);
                },
                functionName:    SanitizeName(toolName),
                description:     description,
                parameters:      parameters,
                returnParameter: returnParameter);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ChatHistory builder
        // ─────────────────────────────────────────────────────────────────────

        private const int MaxHistoryMessages = 8;

        private static ChatHistory BuildChatHistory(List<JObject>? messages)
        {
            var history = new ChatHistory();
            if (messages == null || messages.Count == 0) return history;

            // Trim to last N messages to keep initial context within budget
            var source = messages.Count > MaxHistoryMessages
                ? messages.Skip(messages.Count - MaxHistoryMessages).ToList()
                : messages;

            foreach (var msg in source)
            {
                var role    = msg["role"]?.ToString() ?? "user";
                var content = msg["content"];

                // Skip tool-call turns (content is array) or messages with no plain-text content
                if (content == null || content.Type == JTokenType.Array) continue;
                var contentStr = content.ToString();
                if (string.IsNullOrEmpty(contentStr)) continue;

                if (role == "user")
                    history.AddUserMessage(contentStr);
                else if (role == "assistant" || role == "model")
                    history.AddAssistantMessage(contentStr);
                // roles "tool"/"function" are intentionally skipped
            }
            return history;
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
                        try { ks = KernelJsonSchema.Parse(propJson); } catch { }
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
                    try { ks = KernelJsonSchema.Parse(propJson); } catch { }
                    result.Add(new KernelParameterMetadata(prop.Name)
                    {
                        Description   = desc,
                        IsRequired    = isRequired,
                        ParameterType = typeof(string),
                        Schema        = ks
                    });
                }
            }
            catch { }
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

        private static async Task Safe<T>(Func<T, Task>? callback, T arg)
        {
            if (callback == null) return;
            try { await callback(arg).ConfigureAwait(false); } catch { }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Auto-function invocation filter: context pruning + iteration cap
        // Mirrors AppBuilderAgentBL.PruneMessages() — runs before every LLM
        // call inside SK's agentic loop so the context window never overflows.
        // ─────────────────────────────────────────────────────────────────────

        private sealed class GenericAgentPruneFilter : IAutoFunctionInvocationFilter
        {
            private const int TokenBudget = 120_000;
            private readonly int _maxIterations;

            public GenericAgentPruneFilter(int maxIterations) =>
                _maxIterations = maxIterations > 0 ? maxIterations : 40;

            public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext ctx, Func<AutoFunctionInvocationContext, Task> next)
            {
                // RequestSequenceIndex is 0-based; cap at _maxIterations rounds
                if (ctx.RequestSequenceIndex >= _maxIterations)
                    throw new OperationCanceledException($"Agent stopped: reached {_maxIterations} tool-call iterations.");

                PruneHistory(ctx.ChatHistory);
                await next(ctx).ConfigureAwait(false);
            }

            private static void PruneHistory(ChatHistory? history)
            {
                if (history == null || history.Count < 4) return;
                var total = history.Sum(EstimateLen);
                if (total / 4 < TokenBudget) return;

                // Drop oldest messages (keep index 0 = original user message, keep last 2)
                for (int i = 1; i < history.Count - 2 && total / 4 >= TokenBudget; i++)
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

        private sealed class GeminiRoleFixHandler : DelegatingHandler
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
                catch { return json; }
            }
        }
    }
}
