using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using App.BL;
using App.BL.AppBuilderAgent;
using App.BL.DbGenie;
using App.BL.WorkflowAutomationAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.WorkflowAutomationAgent
{
    /// <summary>
    /// Workflow Automation AI Agent orchestrator.
    ///
    /// Mirrors the AppBuilderAgentBL pattern:
    ///   - Reflection-based plugin discovery ([AgentFunction] / [AgentParam])
    ///   - Polling-based streaming via WfAgentCallbacks
    ///   - Plan confirmation gate (propose_workflow_changes) before any writes
    ///   - Supports Anthropic, OpenAI, and Gemini providers
    /// </summary>
    public static class WorkflowAutomationAgentBL
    {
        private static int MaxIterations
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxIterations");
                if (int.TryParse(raw, out int v))
                    return Math.Max(5, Math.Min(100, v));
                return 40;
            }
        }

        private class ToolDescriptor
        {
            public string       Name        { get; set; }
            public string       Description { get; set; }
            public MethodInfo   Method      { get; set; }
            public object       Instance    { get; set; }
            public ParameterInfo[] Parameters { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // System prompt
        // ─────────────────────────────────────────────────────────────────────

        private const string SystemPrompt = @"You are Workflow AI, an intelligent assistant embedded in the AppAI Workflow Automation Editor.
You help users build complete workflows from scratch OR modify existing ones.

━━━ WORKFLOW CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A workflow = a named sequence of operation tasks executed in ascending SortOrder.
Each task has an ActionType that determines what it does.

OPERATION TASK ACTION TYPES:
  42  = Execute SQL Statement       — runs raw SQL; ALWAYS write a complete valid SQL statement
  49  = Save                        — saves the current form record
  50  = Refresh                     — refreshes the UI/data
  59  = Call API Operation          — calls a configured API operation by ID
  60  = Send Email
  61  = Send SMS
  62  = Send Push Notification
  63  = Send Message
  66  = Import JSON                 — import from a JSON file
  67  = Import Excel                — import from an Excel file
  68  = Execute External Executable — runs an external process
  83  = Call Shopify API            — integrationConfigJson: {""shopUrl"":""..."",""apiKey"":""..."",""resource"":""Orders"",""operation"":""Get""}
  84  = Call Google Sheets API      — integrationConfigJson: {""spreadsheetId"":""..."",""sheetName"":""Sheet1"",""operation"":""Read""}
  85  = Call Netsuite API           — integrationConfigJson: {""accountId"":""..."",""recordType"":""SalesOrder"",""operation"":""Get""}
  86  = Call External REST API      — integrationConfigJson: {""url"":""..."",""method"":""POST"",""authType"":""Bearer"",""headersJson"":""{}"",""bodyTemplate"":""...""}
  200 = Composition Command         — root container (auto-created; never create this manually)

━━━ STRICT GATE RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ALWAYS call propose_workflow_changes BEFORE any create_task, update_task, or delete_task.
NEVER skip this gate. The user must approve the plan before you make any changes.

If propose_workflow_changes returns 'rejected...', ask the user what to change, revise
your plan, and call propose_workflow_changes again with the revised plan.

━━━ WORKFLOW DESIGN PROCESS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to MODIFY an existing workflow:
  1. Call get_workflow_state to see the current tasks.
  2. Analyze which tasks need to change.
  3. Call propose_workflow_changes with a clear summary of all planned changes.
  4. After approval: call create_task / update_task / delete_task.
  5. Call save_workflow when all changes are complete.

When the user asks to CREATE a workflow from scratch:
  1. Ask clarifying questions if the goal is unclear.
  2. Plan the full task sequence (what each step should do and in what order).
  3. Call propose_workflow_changes with the complete plan.
  4. After approval: call create_task for each task in SortOrder.
  5. Call save_workflow when all tasks are created.

━━━ SQL GENERATION RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
For ActionType = 42 (Execute SQL Statement):
  - ALWAYS write the COMPLETE SQL statement. Never write a placeholder.
  - Use standard T-SQL syntax.
  - Include real table names, column names, WHERE clauses, and JOINs as needed.
  - For bulk operations use INSERT INTO ... SELECT or UPDATE ... FROM patterns.
  - Example: TRUNCATE TABLE staging_import_data

━━━ EXTERNAL INTEGRATION RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
For types 83-86, fill integrationConfigJson with all required fields.
Use sensible placeholder values (e.g. ""YOUR_SHOP_URL"") when the user has not specified.

━━━ AFTER CHANGES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
After completing all task operations, always:
  1. Call save_workflow to persist.
  2. Give the user a plain-English summary of what was done.
  3. Mention that they may need to refresh the editor view to see the changes.";

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────

        public static async Task RunAgentAsync(
            WfAgentRequestDto request,
            WfAgentCallbacks callbacks,
            AppClientIdentity? agentIdentity = null)
        {
            try
            {
                agentIdentity = RegisterSystemAgentIdentity(agentIdentity);

                var tools    = DiscoverTools(request.TransactionId, callbacks, agentIdentity);
                var provider = LLMProviderHelper.GetConfiguredProvider();
                var toolDefs = BuildToolDefinitions(tools, provider);

                var trimmedHistory = TrimConversationHistory(request.ConversationHistory);

                var messages = new List<JObject>();
                if (provider == EmLLMProvider.Gemini)
                {
                    if (trimmedHistory?.Any() == true)
                        foreach (var h in trimmedHistory)
                        {
                            var r = h.Role == "assistant" ? "model" : "user";
                            messages.Add(JObject.FromObject(new { role = r, parts = new[] { new { text = h.Content } } }));
                        }
                    messages.Add(JObject.FromObject(new { role = "user", parts = new[] { new { text = request.UserMessage } } }));
                }
                else
                {
                    if (trimmedHistory?.Any() == true)
                        foreach (var h in trimmedHistory)
                            messages.Add(JObject.FromObject(new { role = h.Role, content = h.Content }));
                    messages.Add(JObject.FromObject(new { role = "user", content = request.UserMessage }));
                }

                string finalResponse = null;
                var modifiedTasks    = new List<WfAgentTaskResult>();

                for (int iteration = 0; iteration < MaxIterations; iteration++)
                {
                    messages = PruneMessages(messages, SystemPrompt, provider);

                    await SafeCallback(callbacks.OnStep, new AgentStepEvent
                    {
                        Type        = "thinking",
                        Description = iteration == 0 ? "Analyzing your request…" : "Processing results…",
                        IsSuccess   = true
                    });

                    var llmResponse = provider == EmLLMProvider.Anthropic
                        ? await CallAnthropicWithToolsAsync(messages, toolDefs, SystemPrompt)
                        : provider == EmLLMProvider.Gemini
                            ? await CallGeminiWithToolsAsync(messages, toolDefs, SystemPrompt)
                            : await CallOpenAIWithToolsAsync(messages, toolDefs, SystemPrompt);

                    if (!llmResponse.IsSuccess)
                    {
                        await SafeCallback(callbacks.OnError, "LLM error: " + llmResponse.Error);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(llmResponse.TextContent))
                        await SafeCallback(callbacks.OnToken, llmResponse.TextContent);

                    if (llmResponse.StopReason == "end_turn" || llmResponse.StopReason == "stop")
                    {
                        finalResponse = llmResponse.TextContent;
                        break;
                    }

                    if (llmResponse.ToolCalls?.Any() == true)
                    {
                        messages.Add((JObject)llmResponse.AssistantMessageRaw);
                        var toolResults = new List<object>();

                        foreach (var toolCall in llmResponse.ToolCalls)
                        {
                            await SafeCallback(callbacks.OnStep, new AgentStepEvent
                            {
                                Type        = "tool_call",
                                ToolName    = toolCall.Name,
                                Description = FriendlyLabel(toolCall.Name),
                                Details     = Truncate(toolCall.InputJson, 400),
                                IsSuccess   = true
                            });

                            string toolResult;
                            bool   success = true;
                            try
                            {
                                toolResult = await InvokeToolAsync(tools, toolCall);
                                ExtractModifiedTasks(toolCall.Name, toolResult, modifiedTasks);
                            }
                            catch (Exception ex)
                            {
                                toolResult = JsonConvert.SerializeObject(new { Error = ex.Message });
                                success    = false;
                            }

                            await SafeCallback(callbacks.OnStep, new AgentStepEvent
                            {
                                Type        = "tool_result",
                                ToolName    = toolCall.Name,
                                Description = success ? FriendlyLabel(toolCall.Name) + " — done" : toolCall.Name + " failed",
                                Details     = Truncate(toolResult, 600),
                                IsSuccess   = success
                            });

                            var cappedResult = CapToolResult(toolResult);

                            if (provider == EmLLMProvider.Anthropic)
                                toolResults.Add(new { type = "tool_result", tool_use_id = toolCall.Id, content = cappedResult });
                            else if (provider == EmLLMProvider.Gemini)
                                toolResults.Add(new { functionResponse = new { name = toolCall.Name, response = new { result = cappedResult } } });
                            else
                                toolResults.Add(new { role = "tool", tool_call_id = toolCall.Id, content = cappedResult });
                        }

                        if (provider == EmLLMProvider.Anthropic)
                            messages.Add(JObject.FromObject(new { role = "user", content = toolResults }));
                        else if (provider == EmLLMProvider.Gemini)
                            messages.Add(JObject.FromObject(new { role = "user", parts = toolResults }));
                        else
                            foreach (var tr in toolResults)
                                messages.Add(JObject.FromObject(tr));
                    }
                    else
                    {
                        finalResponse = llmResponse.TextContent;
                        break;
                    }
                }

                var updatedHistory = BuildUpdatedHistory(request, finalResponse);

                await SafeCallback(callbacks.OnDone, new WfAgentDoneEvent
                {
                    FinalResponse        = finalResponse,
                    UpdatedHistory       = updatedHistory,
                    CreatedOrModifiedTasks = modifiedTasks
                });
            }
            catch (Exception ex)
            {
                await SafeCallback(callbacks.OnError, "Agent error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Plugin discovery
        // ─────────────────────────────────────────────────────────────────────

        private static List<ToolDescriptor> DiscoverTools(
            int transactionId,
            WfAgentCallbacks callbacks,
            AppClientIdentity? identity)
        {
            var pluginInstances = new object[]
            {
                new WfAgentConfirmPlugin(callbacks?.OnPlanReady),
                new WorkflowExplorerPlugin(transactionId, identity),
                new WorkflowTaskBuilderPlugin(transactionId, identity),
                new WorkflowSaverPlugin(transactionId, identity)
            };

            var tools = new List<ToolDescriptor>();

            foreach (var instance in pluginInstances)
            {
                var methods = instance.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<AgentFunctionAttribute>() != null);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<AgentFunctionAttribute>();
                    tools.Add(new ToolDescriptor
                    {
                        Name        = attr.Name,
                        Description = attr.Description,
                        Method      = method,
                        Instance    = instance,
                        Parameters  = method.GetParameters()
                    });
                }
            }

            return tools;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tool definitions (same as AppBuilderAgentBL)
        // ─────────────────────────────────────────────────────────────────────

        private static List<object> BuildToolDefinitions(List<ToolDescriptor> tools, EmLLMProvider provider)
        {
            if (provider == EmLLMProvider.Gemini)
            {
                var decls = new List<object>();
                foreach (var tool in tools)
                {
                    var props = new JObject();
                    var req   = new JArray();
                    foreach (var param in tool.Parameters)
                    {
                        var pa = param.GetCustomAttribute<AgentParamAttribute>();
                        props[param.Name] = new JObject
                        {
                            ["type"]        = MapToJsonTypeGemini(param.ParameterType),
                            ["description"] = pa?.Description ?? param.Name
                        };
                        if (pa?.IsRequired == true) req.Add(param.Name);
                    }
                    decls.Add(new
                    {
                        name        = tool.Name,
                        description = tool.Description,
                        parameters  = new JObject
                        {
                            ["type"]       = "OBJECT",
                            ["properties"] = props,
                            ["required"]   = req
                        }
                    });
                }
                return new List<object> { new { functionDeclarations = decls } };
            }

            var result = new List<object>();
            foreach (var tool in tools)
            {
                var properties = new JObject();
                var required   = new JArray();

                foreach (var param in tool.Parameters)
                {
                    var paramAttr = param.GetCustomAttribute<AgentParamAttribute>();
                    properties[param.Name] = new JObject
                    {
                        ["type"]        = MapToJsonType(param.ParameterType),
                        ["description"] = paramAttr?.Description ?? param.Name
                    };
                    if (paramAttr?.IsRequired == true) required.Add(param.Name);
                }

                var schema = new JObject
                {
                    ["type"]       = "object",
                    ["properties"] = properties,
                    ["required"]   = required
                };

                if (provider == EmLLMProvider.Anthropic)
                    result.Add(new { name = tool.Name, description = tool.Description, input_schema = schema });
                else
                    result.Add(new { type = "function", function = new { name = tool.Name, description = tool.Description, parameters = schema } });
            }

            return result;
        }

        private static string MapToJsonType(Type t)
        {
            if (t == null) return "string";
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int) || t == typeof(long))           return "integer";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "number";
            if (t == typeof(bool))                               return "boolean";
            return "string";
        }

        private static string MapToJsonTypeGemini(Type t)
        {
            if (t == null) return "STRING";
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int) || t == typeof(long))           return "INTEGER";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "NUMBER";
            if (t == typeof(bool))                               return "BOOLEAN";
            return "STRING";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tool invocation via reflection
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<string> InvokeToolAsync(
            List<ToolDescriptor> tools, AgentToolCallDto toolCall)
        {
            var tool = tools.FirstOrDefault(t =>
                string.Equals(t.Name, toolCall.Name, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
                return JsonConvert.SerializeObject(new { Error = "Tool '" + toolCall.Name + "' not found" });

            var inputObj = string.IsNullOrWhiteSpace(toolCall.InputJson)
                ? new JObject()
                : JObject.Parse(toolCall.InputJson);

            var args = new object[tool.Parameters.Length];
            for (int i = 0; i < tool.Parameters.Length; i++)
            {
                var param = tool.Parameters[i];
                if (inputObj.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    var targetType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                    try   { args[i] = token.ToObject(targetType); }
                    catch { args[i] = param.HasDefaultValue ? param.DefaultValue : null; }
                }
                else
                    args[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }

            var rawResult = tool.Method.Invoke(tool.Instance, args);

            if (rawResult is Task<string> taskStr)
                return await taskStr.ConfigureAwait(false);

            if (rawResult is Task task)
            {
                await task.ConfigureAwait(false);
                var prop = task.GetType().GetProperty("Result");
                return prop?.GetValue(task)?.ToString() ?? "null";
            }

            return rawResult?.ToString() ?? "null";
        }

        // ─────────────────────────────────────────────────────────────────────
        // LLM API calls (identical to AppBuilderAgentBL)
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<AgentLLMResponseDto> CallAnthropicWithToolsAsync(
            List<JObject> messages, List<object> tools, string systemPrompt)
        {
            var apiKey = LLMProviderHelper.GetConfiguredApiKey();
            var model  = LLMProviderHelper.AnthropicDefaultModel;

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("x-api-key", apiKey);
                http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                http.Timeout = TimeSpan.FromMinutes(10);

                var body = new JObject
                {
                    ["model"]      = model,
                    ["max_tokens"] = 8192,
                    ["system"]     = systemPrompt,
                    ["messages"]   = JArray.FromObject(messages),
                    ["tools"]      = JArray.FromObject(tools)
                };

                var resp = await http.PostAsync("https://api.anthropic.com/v1/messages",
                    new StringContent(body.ToString(), Encoding.UTF8, "application/json"))
                    .ConfigureAwait(false);
                var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    return new AgentLLMResponseDto { IsSuccess = false, Error = "Anthropic: " + raw };

                var json       = JObject.Parse(raw);
                var stopReason = json["stop_reason"]?.ToString() ?? "end_turn";
                var contentArr = json["content"] as JArray ?? new JArray();

                var textParts = new List<string>();
                var toolCalls = new List<AgentToolCallDto>();

                foreach (var item in contentArr)
                {
                    var itype = item["type"]?.ToString();
                    if (itype == "text")
                        textParts.Add(item["text"]?.ToString() ?? "");
                    else if (itype == "tool_use")
                        toolCalls.Add(new AgentToolCallDto
                        {
                            Id        = item["id"]?.ToString(),
                            Name      = item["name"]?.ToString(),
                            InputJson = item["input"]?.ToString(Formatting.None) ?? "{}"
                        });
                }

                return new AgentLLMResponseDto
                {
                    IsSuccess           = true,
                    StopReason          = stopReason,
                    TextContent         = string.Join("\n", textParts),
                    ToolCalls           = toolCalls,
                    AssistantMessageRaw = JObject.FromObject(new { role = "assistant", content = contentArr })
                };
            }
        }

        private static async Task<AgentLLMResponseDto> CallOpenAIWithToolsAsync(
            List<JObject> messages, List<object> tools, string systemPrompt)
        {
            var apiKey = LLMProviderHelper.GetConfiguredApiKey();
            var model  = LLMProviderHelper.OpenAIDefaultModel;

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                http.Timeout = TimeSpan.FromMinutes(10);

                var allMsgs = new JArray(JObject.FromObject(new { role = "system", content = systemPrompt }));
                foreach (var m in messages) allMsgs.Add(m);

                var body = new JObject
                {
                    ["model"]       = model,
                    ["messages"]    = allMsgs,
                    ["tools"]       = JArray.FromObject(tools),
                    ["tool_choice"] = "auto",
                    ["max_tokens"]  = 8192
                };

                var resp = await http.PostAsync("https://api.openai.com/v1/chat/completions",
                    new StringContent(body.ToString(), Encoding.UTF8, "application/json"))
                    .ConfigureAwait(false);
                var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    return new AgentLLMResponseDto { IsSuccess = false, Error = "OpenAI: " + raw };

                var json        = JObject.Parse(raw);
                var choice      = json["choices"]?[0];
                var finishReason = choice?["finish_reason"]?.ToString() ?? "stop";
                var message     = choice?["message"] as JObject ?? new JObject();
                var textContent = message["content"]?.ToString();
                var toolCallsJ  = message["tool_calls"] as JArray;

                var toolCalls = new List<AgentToolCallDto>();
                if (toolCallsJ != null)
                    foreach (var tc in toolCallsJ)
                        toolCalls.Add(new AgentToolCallDto
                        {
                            Id        = tc["id"]?.ToString(),
                            Name      = tc["function"]?["name"]?.ToString(),
                            InputJson = tc["function"]?["arguments"]?.ToString() ?? "{}"
                        });

                message["role"] = "assistant";

                return new AgentLLMResponseDto
                {
                    IsSuccess           = true,
                    StopReason          = finishReason == "tool_calls" ? "tool_use" : "end_turn",
                    TextContent         = textContent,
                    ToolCalls           = toolCalls,
                    AssistantMessageRaw = message
                };
            }
        }

        private static async Task<AgentLLMResponseDto> CallGeminiWithToolsAsync(
            List<JObject> messages, List<object> tools, string systemPrompt)
        {
            var apiKey = LLMProviderHelper.GetConfiguredApiKey();
            var model  = LLMProviderHelper.GeminiDefaultModel;
            var url    = "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent?key=" + apiKey;

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                http.Timeout = TimeSpan.FromMinutes(10);

                var body = new JObject
                {
                    ["systemInstruction"] = JObject.FromObject(new { parts = new[] { new { text = systemPrompt } } }),
                    ["contents"]          = JArray.FromObject(messages),
                    ["tools"]             = JArray.FromObject(tools)
                };

                var resp = await http.PostAsync(url,
                    new StringContent(body.ToString(), Encoding.UTF8, "application/json"))
                    .ConfigureAwait(false);
                var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                    return new AgentLLMResponseDto { IsSuccess = false, Error = "Gemini: " + raw };

                var json      = JObject.Parse(raw);
                var candidate = json["candidates"]?[0];
                var content   = candidate?["content"] as JObject ?? new JObject();
                var parts     = content["parts"] as JArray ?? new JArray();

                var textParts = new List<string>();
                var toolCalls = new List<AgentToolCallDto>();

                foreach (var part in parts)
                {
                    var text = part["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text)) textParts.Add(text);

                    var fc = part["functionCall"] as JObject;
                    if (fc != null)
                        toolCalls.Add(new AgentToolCallDto
                        {
                            Id        = fc["name"]?.ToString(),
                            Name      = fc["name"]?.ToString(),
                            InputJson = fc["args"]?.ToString(Formatting.None) ?? "{}"
                        });
                }

                var stopReason = toolCalls.Count > 0 ? "tool_use" : "end_turn";

                return new AgentLLMResponseDto
                {
                    IsSuccess           = true,
                    StopReason          = stopReason,
                    TextContent         = string.Join("\n", textParts),
                    ToolCalls           = toolCalls,
                    AssistantMessageRaw = content
                };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static AppClientIdentity? RegisterSystemAgentIdentity(AppClientIdentity? agentIdentity)
        {
            if (!agentIdentity.HasValue)
            {
                var systemAgentUserId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemAgentUser);
                if (systemAgentUserId.HasValue)
                {
                    var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(systemAgentUserId);
                    if (userEntity != null)
                        agentIdentity = new AppClientIdentity
                        {
                            UserId                 = systemAgentUserId,
                            SessionId              = Guid.NewGuid().ToString(),
                            IsCallingFromBrowser   = true,
                            LanguageId             = userEntity.LanguageId,
                            CurrentWorkingCompanyId = userEntity.MyOwnCompnanyId,
                            TimeZoneKey            = userEntity.TimeZoneInfoToken
                        };
                }
            }
            if (agentIdentity.HasValue && ServerContext.Instance.WindowsIdentityProvider != null)
                ServerContext.Instance.WindowsIdentityProvider.RegisterIdentity(agentIdentity);
            return agentIdentity;
        }

        private static async Task SafeCallback<T>(Func<T, Task> cb, T arg)
        {
            if (cb == null) return;
            try { await cb(arg).ConfigureAwait(false); } catch { }
        }

        private static List<AppBuilderAgentMessageDto> BuildUpdatedHistory(
            WfAgentRequestDto request, string finalResponse)
        {
            var history = new List<AppBuilderAgentMessageDto>(
                request.ConversationHistory ?? new List<AppBuilderAgentMessageDto>());
            history.Add(new AppBuilderAgentMessageDto { Role = "user",      Content = request.UserMessage });
            if (!string.IsNullOrEmpty(finalResponse))
                history.Add(new AppBuilderAgentMessageDto { Role = "assistant", Content = finalResponse });
            return history;
        }

        private static void ExtractModifiedTasks(
            string toolName, string toolResult, List<WfAgentTaskResult> list)
        {
            try
            {
                if (toolName != "create_task" && toolName != "update_task") return;
                var json = JObject.Parse(toolResult);
                var taskId = json["TaskId"]?.Value<int>();
                if (taskId.HasValue && taskId.Value > 0)
                {
                    list.Add(new WfAgentTaskResult
                    {
                        TaskId     = taskId.Value,
                        Name       = json["Name"]?.ToString(),
                        ActionType = json["ActionType"]?.Value<int>() ?? 0,
                        SortOrder  = json["SortOrder"]?.Value<int>() ?? 0
                    });
                }
            }
            catch { }
        }

        private static string FriendlyLabel(string n)
        {
            switch (n)
            {
                case "propose_workflow_changes": return "Proposing workflow changes — awaiting your approval…";
                case "get_workflow_state":       return "Reading current workflow state…";
                case "create_task":              return "Creating workflow task…";
                case "update_task":              return "Updating workflow task…";
                case "delete_task":              return "Deleting workflow task…";
                case "save_workflow":            return "Saving workflow…";
                case "explain_workflow":         return "Explaining workflow…";
                default:                         return n;
            }
        }

        private static string Truncate(string s, int max) =>
            s == null ? null : s.Length > max ? s.Substring(0, max) + "…" : s;

        private static readonly string[] GateToolNames = { "propose_workflow_changes" };

        private static int GetGroupSize(List<JObject> messages, int startIdx, EmLLMProvider provider)
        {
            if (provider != EmLLMProvider.OpenAI)
                return startIdx + 1 < messages.Count ? 2 : 1;

            int count = 1;
            while (startIdx + count < messages.Count &&
                   string.Equals(messages[startIdx + count]["role"]?.ToString(), "tool", StringComparison.Ordinal))
                count++;
            return count;
        }

        private static bool IsGateGroup(List<JObject> messages, int startIdx, int groupSize)
        {
            int end = Math.Min(startIdx + groupSize, messages.Count);
            for (int k = startIdx; k < end; k++)
            {
                var msgStr = messages[k].ToString(Newtonsoft.Json.Formatting.None);
                foreach (var name in GateToolNames)
                    if (msgStr.Contains("\"" + name + "\""))
                        return true;
            }
            return false;
        }

        private static int MaxToolResultChars
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxToolResultChars");
                return int.TryParse(raw, out int v) ? Math.Max(2000, v) : 10000;
            }
        }

        private static int TokenBudget
        {
            get
            {
                var raw = AppConfig.Get("Agent.TokenBudget");
                return int.TryParse(raw, out int v) ? Math.Max(20000, v) : 120000;
            }
        }

        private static int MaxHistoryTurns
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxHistoryTurns");
                return int.TryParse(raw, out int v) ? Math.Max(1, v) : 4;
            }
        }

        private static string CapToolResult(string result)
        {
            if (result == null) return null;
            int max = MaxToolResultChars;
            if (result.Length <= max) return result;
            int kept    = max;
            int dropped = result.Length - kept;
            return result.Substring(0, kept)
                + "\n[... " + dropped + " chars truncated — ask me to fetch specific details if needed ...]";
        }

        private static int EstimateTokens(string systemPrompt, List<JObject> messages)
        {
            int chars = systemPrompt?.Length ?? 0;
            foreach (var m in messages)
                chars += m.ToString(Newtonsoft.Json.Formatting.None).Length;
            return chars / 4;
        }

        private static List<JObject> PruneMessages(
            List<JObject> messages, string systemPrompt, EmLLMProvider provider)
        {
            if (messages.Count <= 4) return messages;

            int budget = TokenBudget;
            while (messages.Count > 4 && EstimateTokens(systemPrompt, messages) > budget)
            {
                int dropIdx = -1, dropCount = 0, i = 1;
                while (i < messages.Count - 1)
                {
                    int groupSize = GetGroupSize(messages, i, provider);
                    if (!IsGateGroup(messages, i, groupSize))
                    {
                        dropIdx   = i;
                        dropCount = groupSize;
                        break;
                    }
                    i += groupSize;
                }

                if (dropIdx < 0) break;

                for (int d = 0; d < dropCount && dropIdx < messages.Count; d++)
                    messages.RemoveAt(dropIdx);
            }

            return messages;
        }

        private static List<AppBuilderAgentMessageDto> TrimConversationHistory(
            List<AppBuilderAgentMessageDto> history)
        {
            if (history == null || history.Count == 0) return history;
            int maxMessages = MaxHistoryTurns * 2;
            if (history.Count <= maxMessages) return history;
            return history.Skip(history.Count - maxMessages).ToList();
        }
    }
}
