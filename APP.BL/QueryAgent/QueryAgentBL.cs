using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.BL.AppBuilderAgent;
using App.BL.DbGenie;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.QueryAgent
{
    /// <summary>
    /// Query AI Agent — generates T-SQL SELECT queries using an agentic LLM loop.
    ///
    /// Pattern mirrors WorkflowAutomationAgentBL:
    ///   - Reflection-based plugin discovery ([AgentFunction] / [AgentParam])
    ///   - Polling-based streaming via QueryAgentCallbacks
    ///   - Tools: list_selected_tables, get_table_schema
    ///   - Final response contains SQL in a ```sql block; GeneratedQuery is extracted
    /// </summary>
    public static class QueryAgentBL
    {
        private static int MaxIterations
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxIterations");
                if (int.TryParse(raw, out int v))
                    return Math.Max(5, Math.Min(100, v));
                return 20;
            }
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

        private class ToolDescriptor
        {
            public string        Name        { get; set; }
            public string        Description { get; set; }
            public MethodInfo    Method      { get; set; }
            public object        Instance    { get; set; }
            public ParameterInfo[] Parameters { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // System prompt
        // ─────────────────────────────────────────────────────────────────────

        private const string SystemPrompt = @"You are DBA-Genie, a Super DBA AI embedded in the AppAI platform.
You translate natural language into highly complex, syntactically correct, and optimized SQL statements.
You behave like a senior database administrator — not just generating SQL, but explaining and optimizing it.

━━━ WORKFLOW ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1 — DETECT DIALECT
  Call get_database_dialect first. This tells you the SQL platform (SQL Server/T-SQL, MySQL, Oracle/PL-SQL)
  and the exact syntax rules to apply. All subsequent SQL must use that dialect's syntax.

Step 2 — DISCOVER TABLES
  Call list_selected_tables to see which tables/views are in scope.

Step 3 — UNDERSTAND SCHEMA
  Call get_table_schema for each relevant table. NEVER guess column names — always verify first.
  If tables need to be JOINed, also call get_table_relationships to find FK links.

Step 4 — WRITE THE QUERY
  Compose a complete, syntactically correct query using the detected dialect:
  - Use proper identifier quoting for the dialect ([brackets] for SQL Server, `backticks` for MySQL, ""double-quotes"" for Oracle)
  - Include proper JOINs using FK relationships found in Step 3
  - Add WHERE clauses as specified
  - For open-ended requests: SQL Server → TOP 1000; MySQL → LIMIT 1000; Oracle → FETCH FIRST 1000 ROWS ONLY
  - Format SQL with clear line breaks between clauses; indent subqueries and CTEs

Step 5 — EXPLAIN & OPTIMIZE
  After the SQL block, provide:
  a) **Summary**: One sentence describing what the query retrieves.
  b) **Strategy**: Brief explanation of key decisions (e.g. why a CTE was used, why a subquery vs JOIN).
  c) **Optimization Tips** (if applicable): Index suggestions, potential performance concerns, or alternative approaches.

━━━ RESPONSE FORMAT ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

```sql
-- Your complete query here
SELECT ...
FROM   ...
```

**Summary:** [one sentence]
**Strategy:** [brief explanation of query structure choices]
**Optimization Tips:** [optional — index hints, performance notes, or alternatives]

━━━ MULTI-DIALECT SYNTAX REFERENCE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

SQL Server (T-SQL):  TOP n, ISNULL(), GETDATE(), [brackets], STRING_AGG(), CROSS/OUTER APPLY,
                     OFFSET n ROWS FETCH NEXT n ROWS ONLY, TRY_CAST()

MySQL:               LIMIT n, IFNULL(), NOW(), `backticks`, GROUP_CONCAT(), STR_TO_DATE()

Oracle (PL/SQL):     FETCH FIRST n ROWS ONLY or ROWNUM, NVL(), SYSDATE, ""double-quotes"",
                     LISTAGG(), CONNECT BY PRIOR (hierarchical), MODEL clause, PIVOT/UNPIVOT

━━━ ADVANCED SQL CAPABILITIES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

You fully support and should use when appropriate:
- CTEs (WITH clause): use for readability and to avoid repeating subqueries
- Recursive CTEs: for hierarchical/tree data traversal
- Window Functions: ROW_NUMBER(), RANK(), DENSE_RANK(), LEAD(), LAG(), NTILE(),
                    SUM() OVER (PARTITION BY ...), running totals, moving averages
- PIVOT / UNPIVOT: for cross-tab / matrix reports
- Complex JOINs: INNER, LEFT/RIGHT/FULL OUTER, CROSS, SELF-JOIN
- Multi-level nested subqueries
- EXISTS / NOT EXISTS, IN / NOT IN with subqueries
- CASE expressions and conditional aggregation
- JSON functions (SQL Server: FOR JSON PATH, JSON_VALUE; MySQL: JSON_EXTRACT)

━━━ SECURITY GUARDRAILS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- NEVER write DELETE, UPDATE, INSERT, MERGE, DROP, CREATE, ALTER, or any DDL/DML statement.
- If the user requests a destructive operation, refuse and explain why, then offer a safe SELECT equivalent.
- NEVER concatenate user-supplied values directly into SQL strings — always use parameterized patterns with placeholder comments.
- If a query would filter on a high-cardinality unindexed column, note it in Optimization Tips.

━━━ ALIAS & NAMING INTELLIGENCE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Map natural language terms to actual schema names using the column/table names from get_table_schema.
Example: user says 'sales staff' → find tbl_Employee_Sales or similar from the schema.
When mapping is ambiguous, show your reasoning briefly.";

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────

        public static async Task RunAgentAsync(
            QueryAgentRequestDto request,
            QueryAgentCallbacks callbacks,
            AppClientIdentity? agentIdentity = null)
        {
            try
            {
                agentIdentity = RegisterSystemAgentIdentity(agentIdentity);

                var plugin   = new QueryDesignerPlugin(request.DataSourceId, request.SelectedTables);
                var tools    = DiscoverTools(plugin);
                var provider = LLMProviderHelper.GetConfiguredProvider();
                var toolDefs = BuildToolDefinitions(tools, provider);

                var trimmedHistory = TrimConversationHistory(request.ConversationHistory);
                var messages = new List<JObject>();

                // Seed selected tables into the user message for context
                var contextPrefix = request.SelectedTables?.Any() == true
                    ? $"[Selected tables: {string.Join(", ", request.SelectedTables)}]\n\n"
                    : "";

                var fullMessage = contextPrefix + request.UserMessage;

                if (provider == EmLLMProvider.Gemini)
                {
                    if (trimmedHistory?.Any() == true)
                        foreach (var h in trimmedHistory)
                        {
                            var r = h.Role == "assistant" ? "model" : "user";
                            messages.Add(JObject.FromObject(new { role = r, parts = new[] { new { text = h.Content } } }));
                        }
                    messages.Add(JObject.FromObject(new { role = "user", parts = new[] { new { text = fullMessage } } }));
                }
                else
                {
                    if (trimmedHistory?.Any() == true)
                        foreach (var h in trimmedHistory)
                            messages.Add(JObject.FromObject(new { role = h.Role, content = h.Content }));
                    messages.Add(JObject.FromObject(new { role = "user", content = fullMessage }));
                }

                string finalResponse = null;

                for (int iteration = 0; iteration < MaxIterations; iteration++)
                {
                    messages = PruneMessages(messages, SystemPrompt);

                    await SafeCallback(callbacks.OnStep, new AgentStepEvent
                    {
                        Type        = "thinking",
                        Description = iteration == 0 ? "DBA-Genie is analyzing your request…" : "Processing results…",
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
                                Description = success ? FriendlyLabel(toolCall.Name) + " — done"
                                                      : toolCall.Name + " failed",
                                Details     = Truncate(toolResult, 600),
                                IsSuccess   = success
                            });

                            var capped = CapToolResult(toolResult);

                            if (provider == EmLLMProvider.Anthropic)
                                toolResults.Add(new { type = "tool_result", tool_use_id = toolCall.Id, content = capped });
                            else if (provider == EmLLMProvider.Gemini)
                                toolResults.Add(new { functionResponse = new { name = toolCall.Name, response = new { result = capped } } });
                            else
                                toolResults.Add(new { role = "tool", tool_call_id = toolCall.Id, content = capped });
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

                var generatedQuery = ExtractSqlFromResponse(finalResponse);
                var updatedHistory = BuildUpdatedHistory(request, finalResponse);

                await SafeCallback(callbacks.OnDone, new QueryAgentDoneEvent
                {
                    FinalResponse  = finalResponse,
                    GeneratedQuery = generatedQuery,
                    UpdatedHistory = updatedHistory
                });
            }
            catch (Exception ex)
            {
                await SafeCallback(callbacks.OnError, "Agent error: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Extract SQL from a ```sql ... ``` fenced code block
        // ─────────────────────────────────────────────────────────────────────

        private static string ExtractSqlFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return null;

            // ```sql ... ```
            var m = Regex.Match(response, @"```sql\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.Trim();

            // ``` ... ``` (generic block)
            var m2 = Regex.Match(response, @"```\s*([\s\S]*?)```");
            if (m2.Success) return m2.Groups[1].Value.Trim();

            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tool discovery (reflection)
        // ─────────────────────────────────────────────────────────────────────

        private static List<ToolDescriptor> DiscoverTools(QueryDesignerPlugin plugin)
        {
            var tools = new List<ToolDescriptor>();
            var methods = plugin.GetType()
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
                    Instance    = plugin,
                    Parameters  = method.GetParameters()
                });
            }
            return tools;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build provider-specific tool definitions
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
            if (t == typeof(int) || t == typeof(long))                           return "integer";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "number";
            if (t == typeof(bool))                                               return "boolean";
            return "string";
        }

        private static string MapToJsonTypeGemini(Type t)
        {
            if (t == null) return "STRING";
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int) || t == typeof(long))                           return "INTEGER";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "NUMBER";
            if (t == typeof(bool))                                               return "BOOLEAN";
            return "STRING";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Reflection-based tool invocation
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<string> InvokeToolAsync(
            List<ToolDescriptor> tools, AgentToolCallDto toolCall)
        {
            var tool = tools.FirstOrDefault(t =>
                string.Equals(t.Name, toolCall.Name, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
                return JsonConvert.SerializeObject(new { Error = $"Tool '{toolCall.Name}' not found" });

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
                {
                    args[i] = param.HasDefaultValue ? param.DefaultValue : null;
                }
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
        // LLM API callers (Anthropic / OpenAI / Gemini)
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
                    return new AgentLLMResponseDto { IsSuccess = false, Error = $"Anthropic: {raw}" };

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

                var allMsgs = new JArray(
                    JObject.FromObject(new { role = "system", content = systemPrompt }));
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
                    return new AgentLLMResponseDto { IsSuccess = false, Error = $"OpenAI: {raw}" };

                var json         = JObject.Parse(raw);
                var choice       = json["choices"]?[0];
                var finishReason = choice?["finish_reason"]?.ToString() ?? "stop";
                var message      = choice?["message"] as JObject ?? new JObject();
                var textContent  = message["content"]?.ToString();
                var toolCallsJ   = message["tool_calls"] as JArray;

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
            var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

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
                    return new AgentLLMResponseDto { IsSuccess = false, Error = $"Gemini: {raw}" };

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

                return new AgentLLMResponseDto
                {
                    IsSuccess           = true,
                    StopReason          = toolCalls.Count > 0 ? "tool_use" : "end_turn",
                    TextContent         = string.Join("\n", textParts),
                    ToolCalls           = toolCalls,
                    AssistantMessageRaw = content
                };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static async Task SafeCallback<T>(Func<T, Task> cb, T arg)
        {
            if (cb == null) return;
            try { await cb(arg).ConfigureAwait(false); } catch { }
        }

        private static string FriendlyLabel(string n)
        {
            switch (n)
            {
                case "get_database_dialect":    return "Detecting database dialect…";
                case "list_selected_tables":    return "Listing selected tables…";
                case "get_table_schema":        return "Reading table schema…";
                case "get_table_relationships": return "Analyzing table relationships…";
                default:                        return n;
            }
        }

        private static string Truncate(string s, int max) =>
            s == null ? null : s.Length > max ? s.Substring(0, max) + "…" : s;

        private static string CapToolResult(string result)
        {
            if (result == null) return null;
            int max = MaxToolResultChars;
            if (result.Length <= max) return result;
            int dropped = result.Length - max;
            return result.Substring(0, max) + $"\n[... {dropped} chars truncated ...]";
        }

        private static int EstimateTokens(string systemPrompt, List<JObject> messages)
        {
            int chars = systemPrompt?.Length ?? 0;
            foreach (var m in messages)
                chars += m.ToString(Formatting.None).Length;
            return chars / 4;
        }

        private static List<JObject> PruneMessages(List<JObject> messages, string systemPrompt)
        {
            if (messages.Count <= 4) return messages;
            int budget = TokenBudget;
            while (messages.Count > 4 && EstimateTokens(systemPrompt, messages) > budget)
            {
                if (messages.Count > 3)
                {
                    messages.RemoveAt(1);
                    if (messages.Count > 2)
                        messages.RemoveAt(1);
                }
                else break;
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

        private static List<AppBuilderAgentMessageDto> BuildUpdatedHistory(
            QueryAgentRequestDto request, string finalResponse)
        {
            var history = new List<AppBuilderAgentMessageDto>(
                request.ConversationHistory ?? new List<AppBuilderAgentMessageDto>());
            history.Add(new AppBuilderAgentMessageDto { Role = "user",      Content = request.UserMessage });
            if (!string.IsNullOrEmpty(finalResponse))
                history.Add(new AppBuilderAgentMessageDto { Role = "assistant", Content = finalResponse });
            return history;
        }

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
                            UserId                  = systemAgentUserId,
                            SessionId               = Guid.NewGuid().ToString(),
                            IsCallingFromBrowser    = true,
                            LanguageId              = userEntity.LanguageId,
                            CurrentWorkingCompanyId = userEntity.MyOwnCompnanyId,
                            TimeZoneKey             = userEntity.TimeZoneInfoToken,
                        };
                }
            }
            if (agentIdentity.HasValue && ServerContext.Instance.WindowsIdentityProvider != null)
                ServerContext.Instance.WindowsIdentityProvider.RegisterIdentity(agentIdentity);
            return agentIdentity;
        }
    }
}
