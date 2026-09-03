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
                    SessionId        = "",
                    SkillKey         = skillKey,
                    UserId           = userId,
                    CompanyId        = companyId
                };

                // Build SK kernel (provider-specific connector registered here)
                var kernel = BuildKernel(identity);
                kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks));

                // Wrap AppAgentToolRegister rows as KernelFunctions
                var toolRows = (dsId > 0 ? TbToolBL.GetBySkillKey(skillKey, dsId) : TbToolBL.GetBySkillKey(skillKey)) ?? new List<TbToolDto>();
                if (toolRows.Count > 0)
                    kernel.Plugins.AddFromFunctions("tools",
                        toolRows.Select(r => WrapRegisteredTool(r, context, skillSet.MaxToolResultChars)).ToArray());

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

        private static KernelFunction WrapRegisteredTool(TbToolDto row, AgentToolContext context, int maxChars)
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
                    var result = await AppAgentToolEngine.Dispatch(toolType, toolConfig, strArgs, context, ct)
                                                         .ConfigureAwait(false);
                    return Truncate(result, cap);
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

                    return Truncate(text, cap);
                },
                functionName:    SanitizeName(toolName),
                description:     description,
                parameters:      parameters,
                returnParameter: returnParameter);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ChatHistory builder
        // ─────────────────────────────────────────────────────────────────────

        private static ChatHistory BuildChatHistory(List<JObject>? messages)
        {
            var history = new ChatHistory();
            if (messages == null) return history;
            foreach (var msg in messages)
            {
                var role    = msg["role"]?.ToString() ?? "user";
                var content = msg["content"]?.ToString() ?? "";
                if (role == "user")                        history.AddUserMessage(content);
                else if (role == "assistant" || role == "model") history.AddAssistantMessage(content);
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
                var schema   = JObject.Parse(schemaJson);
                var props    = schema["properties"] as JObject;
                if (props == null) return result;
                var required = new HashSet<string>(
                    (schema["required"] as JArray)?.Select(t => t.ToString()) ?? Enumerable.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);
                foreach (var prop in props.Properties())
                {
                    var desc = prop.Value["description"]?.ToString();
                    result.Add(new KernelParameterMetadata(prop.Name)
                    {
                        Description   = desc,
                        IsRequired    = required.Contains(prop.Name),
                        ParameterType = typeof(string)
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

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max) + "…";
        }

        private static async Task Safe<T>(Func<T, Task>? callback, T arg)
        {
            if (callback == null) return;
            try { await callback(arg).ConfigureAwait(false); } catch { }
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
