# Generic AI Agent Platform — Dataflow & Sequence Reference

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-02  
**Audience:** Backend and frontend developers implementing or debugging the agent platform  

---

## 1. End-to-End Request Flow

```
User types message in GenericAgentChat.tsx
          │
          │  handleSend()
          │  Builds Messages[] from prior chat turns (multi-turn history)
          ▼
genericAgentSvc.RunAgent({ SkillKey, UserMessage, SessionId?, Messages[] })
          │
          │  POST /webapi/GenericAgent/RunAgent
          ▼
GenericAgentController.RunAgent()
  │  1. Extract AppClientIdentity from ServerContext
  │  2. Validate SkillKey and UserMessage
  │  3. GenericAgentSessionStore.CreateSession() → sessionId (GUID)
  │  4. Wire GenericAgentCallbacks (OnToken, OnStep, OnDone, OnError,
  │     OnPlanReady, OnSchemaReady) → Enqueue events into session queue
  │  5. Task.Run(GenericAgentBL.RunAsync(...)) — fire-and-forget
  │  6. Return HTTP 200 { IsStarted: true, SessionId }
          │
          │  Client receives { SessionId }
          ▼
genericAgentSvc opens EventSource → GET /webapi/GenericAgent/StreamEvents?sessionId=...
          │
          │  GenericAgentController.StreamEvents() — SSE loop:
          │    while !done:
          │      WaitForEventAsync(sessionId, 30s)  — blocks until event or keepalive
          │      DequeueAll(sessionId) → events[]
          │      foreach event: write "event: {type}\ndata: {json}\n\n"
          │      flush Response.Body
          │      if type == "done" or "error": done = true; break
          │      if no events: write keepalive ": keepalive\n\n"
          │
          ▼ (background thread running in Task.Run)

GenericAgentBL.RunAsync(skillKey, userMessage, chatHistory, callbacks, identity, ct)
  │  1. Validate skillKey (non-null/empty)
  │  2. Validate userMessage (non-null/empty)
  │  3. Call GenericAgentEngine.RunAsync(...)
  │     OperationCanceledException → SafeOnError("Agent run was cancelled.")
  │     Exception → SafeOnError("Agent error: " + ex.Message)
          │
          ▼
GenericAgentEngine.RunAsync(skillKey, userMessage, chatHistory, callbacks, identity, ct)
  │
  │  BOOT (see §2 for detail)
  │  1. AppAgentSkillSetBL.GetByKey(skillKey [, dsId]) → skillSet
  │     if null → callbacks.OnError("Skill key not found: ...")
  │  2. Build AgentToolContext (connectionString, databaseName, userId, companyId)
  │  3. BuildKernel(identity) → Kernel
  │  4. kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks))
  │  5. Load tool rows → WrapRegisteredTool each → kernel.Plugins.AddFromFunctions("tools", ...)
  │  6. Load MCP servers → CreateMcpPluginAsync each → kernel.Plugins.Add(plugin)
  │
  │  RUN
  │  7. BuildChatHistory(chatHistory) → history; history.AddUserMessage(userMessage)
  │  8. hasTools = kernel.Plugins.Any(p => p.Any())
  │     execSettings.FunctionChoiceBehavior = hasTools ? Auto() : none
  │  9. new ChatCompletionAgent { Kernel, Name="Assistant", Instructions=SystemPrompt, Arguments }
  │  10. callbacks.OnStep({ Type="thinking", ... })
  │  11. await foreach chunk in agent.InvokeStreamingAsync(thread, ct):
  │        text = chunk.Message.Content
  │        fullResponse.Append(text)
  │        callbacks.OnToken(text)
  │  12. callbacks.OnDone(fullResponse.ToString())
  │
  │  CLEANUP
  │  13. foreach mcpClient: Dispose/DisposeAsync
  │
  ▼ (on exception)
  log.Error(ex, "[skillKey]")
  callbacks.OnError("Agent error: " + msg)
```

Events flow from the background thread into `GenericAgentSessionStore` via the callbacks. The SSE loop in the controller drains them and writes them to the HTTP response body.

**React side event handling** in `genericAgentSvc.ts` via `EventSource`:

| SSE event type | React action |
|---|---|
| `token` | Append to last assistant bubble; create new bubble if none is streaming |
| `step` | Add to `steps[]` array; shown as step indicator chips |
| `plan` | Set `pendingPlan` → show Plan Review UI with Approve/Reject |
| `schema` | Set `pendingSchema` → show Schema Review UI |
| `done` | Mark last assistant message as not-streaming; `isRunning = false` |
| `error` | Set error banner; `isRunning = false` |

---

## 2. Agent Boot Sequence

This happens at the top of `GenericAgentEngine.RunAsync` before the SK loop starts.

```
Step 1 — Load SkillSet
  dsId = identity?.DataSourceId ?? 0
  skillSet = dsId > 0
    ? AppAgentSkillSetBL.GetByKey(skillKey, dsId)   // tenant-specific data source
    : AppAgentSkillSetBL.GetByKey(skillKey)           // default data source
  if null → OnError and return

Step 2 — Build AgentToolContext
  context = {
    ConnectionString: identity?.CurrentUserDbConnectionString ?? "",
    DatabaseName:     identity?.CurrentUserDataBaseName ?? "",
    SessionId:        "",
    SkillKey:         skillKey,
    UserId:           identity?.UserId → int,
    CompanyId:        identity?.CurrentWorkingCompanyId → int
  }

Step 3 — Build SK Kernel
  provider = AIConfigSettingBL.GetProvider(identity)  // tenant setting "AIConfigProvider"
  apiKey   = AIConfigSettingBL.GetApiKey(identity)    // provider-specific key
  model    = AIConfigSettingBL.GetModel(identity)     // provider-specific model
  Kernel.CreateBuilder()
    .Add[Provider]ChatCompletion(model, apiKey)
    .Build()

Step 4 — Register IFunctionInvocationFilter
  kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks))
  // AgentStepFilter fires callbacks.OnStep before/after each tool call

Step 5 — Load registered tools
  toolRows = AppAgentToolRegisterBL.GetBySkillKey(skillKey [, dsId])  (IsActive=1 only)
  foreach toolRow:
    parameters = ParseKernelParameters(toolRow.ParameterSchemaJson)
    KernelFunction = KernelFunctionFactory.CreateFromMethod(
      async (KernelArguments args, CancellationToken ct) =>
        Truncate(
          await AppAgentToolEngine.Dispatch(toolType, toolConfig, strArgs, context, ct),
          cap),
      functionName: SanitizeName(toolRow.ToolName),
      description:  toolRow.ToolDescription,
      parameters:   parameters)
  kernel.Plugins.AddFromFunctions("tools", kernelFunctions[])

Step 6 — Connect MCP servers
  mcpServers = AppAgentMcpServerBL.GetBySkillKey(skillKey [, dsId])  (IsActive=1 only)
  foreach srv where ServerType == "streamable-http" and ServerUrl non-empty:
    try:
      (client, plugin) = await CreateMcpPluginAsync(srv, skillSet.MaxToolResultChars, ct)
      mcpClients.Add(client)
      kernel.Plugins.Add(plugin)
    catch ex:
      log.Warn(ex, "MCP server {url} skipped")
      // agent continues with remaining tools

Step 7 — Memory injection (if InjectMemory flag set)
  [NOTE: InjectMemory is defined in the design and CapabilityFlags enum but the current
   GenericAgentEngine implementation inserts the SystemPrompt directly from skillSet.SystemPrompt.
   Memory injection is implemented at the AppBuilderAgent level via the search_memory tool.
   If InjectMemory flag is used as a gate, BuildSystemPrompt in GenericAgentBL handles prepending.]

Step 8 — Schema injection (if InjectSchema flag set)
  [InjectSchema causes DB schema summary to be prepended to the system prompt.
   Implementation is in BuildSystemPrompt / skill-specific setup before RunAsync is called.]
```

---

## 3. Tool Execution Flow per ToolType

When SK decides to invoke a tool, it calls the `KernelFunction` created in Boot Step 5. The function body:

```
KernelArguments args →
  strArgs = args.Where(kv.Value != null).ToDictionary(kv.Key, kv.Value.ToString())
AppAgentToolEngine.Dispatch(toolType, toolConfig, strArgs, context, ct)
  → result string
Truncate(result, skillSet.MaxToolResultChars)
  → truncated string returned to SK as tool result
```

### BuiltIn

```
toolConfig = { "TypeName": "APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin",
               "MethodName": "GetTableSchema" }
BuiltInToolExecutor.ExecuteAsync:
  type   = Type.GetType(TypeName) or scan loaded assemblies
  method = type.GetMethod(MethodName)
  result = method.Invoke(null, [args, context, ct])
  → await Task<string>
```

The method must be static and match the signature `Task<string> Method(IReadOnlyDictionary<string,string> args, AgentToolContext ctx, CancellationToken ct)`.

### SqlQuery

```
toolConfig = { "SqlBody": "SELECT ... WHERE Col=@paramName", "ReturnType": "json" }
SqlQueryToolExecutor.ExecuteAsync:
  Parse @paramName tokens from SqlBody
  foreach param: cmd.Parameters.Add(name, strArgs[name])
  SqlCommand.ExecuteReader()
  Serialize rows as JSON array
  → return JSON string
```

Never string-concatenates user-supplied values into the SQL. Only `@param` binding.

### HttpRest

```
toolConfig = { "Url": "https://api.internal/resource/{resourceId}",
               "Method": "GET",
               "TokenStoreKey": "erp-api" }
HttpRestToolExecutor.ExecuteAsync:
  foreach {placeholder} in Url: substitute strArgs[placeholder]
  if TokenStoreKey: retrieve bearer token from internal token store
  HttpClient.SendAsync(request)
  → return response body as string
```

### DynamicCSharp

```
toolConfig = { "ScriptBody": "return Args.UnitPrice * Args.Qty;",
               "AllowedNamespaces": ["System", "System.Linq"],
               "TimeoutSeconds": 10 }
DynamicCSharpToolExecutor.ExecuteAsync:
  ScriptOptions.Default
    .AddImports(AllowedNamespaces)
    .WithFilePath(null)  // no file access
  ScriptGlobals globals = { Args: strArgs }
  using cts = CancellationTokenSource(TimeoutSeconds)
  result = await CSharpScript.EvaluateAsync<object>(ScriptBody, options, globals, ct=cts.Token)
  Log(userId, skillKey, toolName, ScriptBody)  // audit every execution
  → result.ToString()

Blocked namespaces: System.IO, System.Net, System.Reflection,
  System.Diagnostics, APP.BL (all application namespaces)
```

---

## 4. MCP Tool Discovery Flow

```
CreateMcpPluginAsync(TbMcpDto server, int maxChars, CancellationToken ct):

  1. Build transport
     HttpClientTransportOptions {
       Endpoint      = new Uri(server.ServerUrl),
       TransportMode = HttpTransportMode.StreamableHttp
     }
     transport = new HttpClientTransport(options, McpHttpClient, NullLoggerFactory, ownsHttpClient=false)

  2. Connect
     client = await McpClient.CreateAsync(transport, cancellationToken: ct)

  3. Discover tools
     tools = await client.ListToolsAsync(cancellationToken: ct)

  4. For each tool:
     a. Parse tool.JsonSchema (JsonElement) for "properties" and "required"
     b. Build List<KernelParameterMetadata> from each property
     c. KernelFunctionFactory.CreateFromMethod(
          async (KernelArguments args, CancellationToken ct) =>
            using callCts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            callCts.CancelAfter(30s)
            result = await client.CallToolAsync(toolName, mcpArgs, callCts.Token)
            text = join(result.Content.OfType<TextContentBlock>().Select(c => c.Text))
            return Truncate(text, maxChars),
          functionName:    SanitizeName(tool.Name),
          description:     tool.Description,
          parameters:      parameters,
          returnParameter: { Schema: KernelJsonSchema.Parse("{\"type\":\"string\"}") })

  5. KernelPluginFactory.CreateFromFunctions("mcp_" + SanitizeName(server.ServerName), functions[])
  6. return (client, plugin)
```

**Important:** `AsKernelFunction()` is NOT used. All wrapping is done via `KernelFunctionFactory.CreateFromMethod` as shown above.

---

## 5. LLM Provider Selection Flow

```
GenericAgentEngine.BuildKernel(AppClientIdentity? identity):

  if identity.HasValue:
    providerStr = AIConfigSettingBL.GetProvider(identity.Value)
    provider    = Enum.Parse<EmLLMProvider>(providerStr) ?? EmLLMProvider.Anthropic
    apiKey      = AIConfigSettingBL.GetApiKey(identity.Value)
    model       = AIConfigSettingBL.GetModel(identity.Value)
  else:
    provider = KernelProviderHelper.GetProvider()       → LLMProviderHelper.GetConfiguredProvider()
    apiKey   = KernelProviderHelper.GetApiKey()
    model    = KernelProviderHelper.GetModel()          → AIConfigSettingBL.GetModel()

  builder = Kernel.CreateBuilder()
  switch provider:
    Anthropic:
      builder.Services.AddSingleton<IChatCompletionService>(
        new AnthropicChatCompletionService(model, apiKey))
    Gemini:
      builder.AddGoogleAIGeminiChatCompletion(model, apiKey,
        httpClient: new HttpClient(new GeminiRoleFixHandler()))
    default (OpenAI):
      builder.AddOpenAIChatCompletion(model, apiKey)
  return builder.Build()

AIConfigSettingBL.GetProvider(identity):
  raw = AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigProvider, identity)
  return NonEmpty(raw) ?? "Gemini"

AIConfigSettingBL.GetApiKey(identity):
  provider = GetProvider(identity).ToLower()
  switch provider:
    "openai"    → GetStringValue(AIConfigOpenAIApiKey, identity)
    "anthropic" → GetStringValue(AIConfigAnthropicApiKey, identity)
    _           → GetStringValue(AIConfigGeminiApiKey, identity)

AIConfigSettingBL.GetModel(identity):
  provider = GetProvider(identity).ToLower()
  switch provider:
    "openai"    → GetStringValue(AIConfigOpenAIModel) ?? "gpt-4o"
    "anthropic" → GetStringValue(AIConfigAnthropicModel) ?? "claude-3-5-sonnet-20241022"
    _           → GetStringValue(AIConfigGeminiModel) ?? "gemini-2.0-flash"
```

---

## 6. Error Handling Path

### Missing SkillKey or UserMessage (GenericAgentBL)

```
string.IsNullOrWhiteSpace(skillKey)    → SafeOnError(callbacks, "SkillKey is required.")
string.IsNullOrWhiteSpace(userMessage) → SafeOnError(callbacks, "UserMessage is required.")
  → callbacks.OnError(message) → controller enqueues error event → SSE sends error → client shows error banner
```

### SkillSet not found in DB (GenericAgentEngine)

```
skillSet == null
  → Safe(callbacks.OnError, "Skill key not found: {skillKey}")
  → return immediately (no kernel built)
```

### Empty API key

No explicit early-return for empty API key. The kernel is built with the empty string and the LLM provider will reject the first API call. The `HttpOperationException` is caught in the outer try/catch:

```
catch (Exception ex):
  log.Error(ex, "GenericAgentEngine [{skillKey}]")
  msg = ex.Message
  if ex is HttpOperationException && ResponseContent non-empty:
    msg += " | " + ResponseContent   // includes the raw API error body
  else if ex.InnerException:
    msg += " — " + ex.InnerException.Message
  Safe(callbacks.OnError, "Agent error: " + msg)
```

The `ResponseContent` extraction is important for Gemini and OpenAI — their HTTP errors contain a JSON body with a human-readable message that SK's exception otherwise discards.

### MCP server connection failure (per server)

```
try { (client, plugin) = await CreateMcpPluginAsync(srv, ...) }
catch (Exception ex):
  log.Warn(ex, "MCP server {srv.ServerUrl} skipped")
  // continues loop — other servers still load
```

The agent runs with the tools it has; the LLM is not informed of the missing server.

### Tool execution failure (per tool call)

Tool exceptions propagate to SK, which formats them as tool error messages returned to the LLM. The LLM typically responds by explaining the failure to the user or retrying with different arguments. The outer try/catch in `RunAsync` catches any unhandled exception from the SK loop.

### Gate timeout (plan/schema)

```
using cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
cts.Token.Register(() => tcs.TrySetResult(false));
```

If the user does not respond within 10 minutes, the gate auto-rejects (returns `false` for plan, `{ Confirmed: false, Feedback: "timed out" }` for schema). The agent treats a rejection as user disapproval and stops the planned action.

### OperationCanceledException (GenericAgentBL)

```
catch (OperationCanceledException):
  SafeOnError(callbacks, "Agent run was cancelled.")
```

Typically fired when the client disconnects and the cancellation token propagates to the SK streaming loop.

---

## 7. Streaming SSE Flow

```
Background thread                     Controller (SSE loop)              React (EventSource)
─────────────────────────────────────────────────────────────────────────────────────────────
callbacks.OnStep(stepEvent)
  → Enqueue(sessionId, {EventType="step", Step=stepEvent})
  → eventQueue.Add(event)
  → semaphore.Release()
                                     WaitForEventAsync returns
                                     DequeueAll(sessionId) → [stepEvent]
                                     write "event: step\ndata: {json}\n\n"
                                     flush Response.Body
                                                                     EventSource fires "step"
                                                                     onStep(step) → update steps[]

callbacks.OnToken(text)
  → Enqueue(sessionId, {EventType="token", Token=text})
  → semaphore.Release()
                                     WaitForEventAsync returns
                                     write "event: token\ndata: {json}\n\n"
                                     flush
                                                                     onToken(text) → append to bubble

callbacks.OnDone(finalResponse)
  → Enqueue(sessionId, {EventType="done", Done={FinalResponse=...}})
                                     write "event: done\ndata: {json}\n\n"
                                     flush
                                     done = true → exit loop
                                                                     onDone(done) → finalize bubble
                                                                     isRunning = false
                                                                     EventSource.close()
```

**Keepalive:** When `WaitForEventAsync` times out after 30 seconds with no events (LLM is thinking), the controller writes `: keepalive\n\n`. This prevents the SSE connection from being closed by load balancers or proxies.

**Polling fallback:** `GET /webapi/GenericAgent/PollEvents?sessionId=...` returns all queued events immediately. The React service can fall back to calling this every 500ms if EventSource is not supported.

---

## 8. Context Management Flow

Multi-turn context is managed client-side. The React component accumulates all messages in local state. On each send:

```
GenericAgentChat.handleSend():
  history = messages
    .filter(m => !m.isStreaming)
    .map(m => ({ role: m.role === 'user' ? 'user' : 'assistant', content: m.content }))
  genericAgentSvc.RunAgent({ SkillKey, UserMessage, Messages: history, SessionId? })
```

The server receives the full history in the request body. `GenericAgentEngine.BuildChatHistory` converts it:

```
foreach msg in chatHistory:
  if role == "user"                   → history.AddUserMessage(content)
  if role == "assistant" or "model"   → history.AddAssistantMessage(content)
history.AddUserMessage(userMessage)   // current turn appended last
```

**Tool result capping** (not summarization): Tool results are capped at `MaxToolResultChars` before being returned to SK. This is the primary defense against context overflow within a single session.

**Design note on summarization:** The design document describes a four-level context management strategy including in-session LLM summarization (`SummarizeThreshold`). The `SummarizeThreshold` column exists in the DB and DTO. The current `GenericAgentEngine` implementation does not call the LLM for summarization within a session — history management is delegated to the client-side message list. The `SummarizeThreshold` field is available for a future server-side session store implementation.

---

## 9. AgentStepFilter — IFunctionInvocationFilter

The step filter fires before and after each tool invocation:

```csharp
class AgentStepFilter : IFunctionInvocationFilter
{
    async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        // Before
        callbacks.OnStep(new AgentStepEvent {
            Type = "tool_call",
            ToolName = context.Function.Name,
            Description = "Calling " + context.Function.Name + "...",
            IsSuccess = true
        });

        await next(context);   // SK executes the tool

        // After
        callbacks.OnStep(new AgentStepEvent {
            Type = "tool_result",
            ToolName = context.Function.Name,
            Description = "Done",
            IsSuccess = true   // or false if exception was thrown
        });
    }
}
```

Step events are forwarded to the client via SSE and displayed as tool-call chips in the chat UI. Only the last 3 steps are shown at any time.

---

## 10. Backward-Compatible Controller Path

Existing controllers continue to work. Their `RunAgent` action bodies are replaced with:

```csharp
// AppBuilderAgentController
return Ok(await GenericAgentBL.RunAsync("app-builder", req.UserRequest, ctx, callbacks));

// AppReportAgentController
return Ok(await GenericAgentBL.RunAsync("app-report", req.UserRequest, ctx, callbacks));

// DbGenieController
return Ok(await GenericAgentBL.RunAsync("db-genie", req.UserMessage, ctx, callbacks));
```

The route URLs (`/webapi/AppBuilderAgent/RunAgent`, etc.) and request DTO shapes are unchanged. Existing frontends continue to call their dedicated controller paths without modification.

The `GenericAgentController` at `/webapi/GenericAgent/RunAgent` is the new general-purpose entry point, used by the admin test UI and any new agent consumers.
