# Generic AI Agent Platform — Technical Architecture

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-02  
**Audience:** Senior developers, backend engineers  

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  React UI (AppReact/src/components/aiskill/)                        │
│  AgentSkillSetManagement.tsx    GenericAgentChat.tsx                │
└──────────────────────────┬──────────────────────────────────────────┘
                           │  POST /webapi/GenericAgent/RunAgent
                           │  GET  /webapi/GenericAgent/StreamEvents  (SSE)
                           │  POST /webapi/GenericAgent/ConfirmPlan
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  GenericAgentController  (AppAI.Web/Controllers/)                   │
│  • Validates request, captures AgentExecutionContext                │
│  • Creates session and starts background execution                  │
│  • Wires GenericAgentCallbacks → GenericAgentSessionStore queue    │
│  • StreamEvents: SSE loop draining queue until done/error          │
│  • ConfirmPlan / ConfirmSchema: resolves TaskCompletionSource       │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  GenericAgentBL  (APP.BL/AIAgent/GenericAgent/)                     │
│  • Validates skillKey + userMessage                                 │
│  • Delegates directly to GenericAgentEngine.RunAsync               │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  GenericAgentEngine  (APP.BL/AIAgent/GenericAgent/)                 │
│  1. Load SkillSet (AppAgentSkillSetBL)                              │
│  2. Build SK Kernel (AIConfigSettingBL → KernelProviderHelper)      │
│  3. Register AgentStepFilter (IFunctionInvocationFilter)           │
│  4. Wrap AppAgentToolRegister rows → KernelFunctions               │
│     (via AppAgentToolEngine.Dispatch → 6 ToolType executors)       │
│  5. Connect MCP servers → McpClient → KernelPlugin                  │
│  6. Run ChatCompletionAgent.InvokeStreamingAsync                    │
│  7. Fire OnToken / OnDone / OnError callbacks                       │
└────────┬───────────────────────────────────┬────────────────────────┘
         │                                   │
         ▼                                   ▼
┌─────────────────────┐         ┌────────────────────────────────────┐
│  Semantic Kernel     │         │  External Systems                  │
│  ChatCompletionAgent │         │  ┌─────────────────────────────┐  │
│  + LLM provider     │         │  │ AppAgentToolRegister tools   │  │
│  (Anthropic /       │         │  │  BuiltIn  → APP.BL plugins  │  │
│   Gemini / OpenAI)  │         │  │  SqlQuery → tenant DB       │  │
└─────────────────────┘         │  │  HttpRest → external API    │  │
                                │  │  DynamicCSharp → Roslyn     │  │
                                │  └─────────────────────────────┘  │
                                │  ┌─────────────────────────────┐  │
                                │  │ MCP Servers                 │  │
                                │  │  streamable-http transport  │  │
                                │  │  auto-discovered tools      │  │
                                │  └─────────────────────────────┘  │
                                └────────────────────────────────────┘
```

Existing agent controllers (`AppBuilderAgentController`, `AppReportAgentController`, `DbGenieController`) continue to expose their original routes. Their bodies now resolve the authenticated tenant execution context and forward it to `GenericAgentBL.RunAsync` with a hardcoded `skillKey`. The generic `GenericAgentController` additionally accepts any `skillKey` and is the entry point for the admin test UI.

---

## 2. Component Responsibilities

| Class / File | Location | Purpose | Key Methods |
|---|---|---|---|
| `GenericAgentBL` | `APP.BL/AIAgent/GenericAgent/` | Public entry point. Validates inputs, captures/accepts tenant execution context, delegates to engine. | `RunAsync(executionContext, userMessage, chatHistory, callbacks, ct)` |
| `GenericAgentEngine` | `APP.BL/AIAgent/GenericAgent/` | SK agentic loop. Loads tenant-scoped config, builds kernel, runs streaming. | `RunAsync(...)`, `BuildKernel()`, `WrapRegisteredTool()`, `CreateMcpPluginAsync()`, `BuildChatHistory()` |
| `AIConfigSettingBL` | `APP.BL/AIAgent/GenericAgent/` | Reads LLM provider/key/model from the explicitly selected tenant's `AppTenantSetting`. No appsettings.json fallback. | `GetProvider(executionContext)`, `GetApiKey(executionContext)`, `GetModel(executionContext)` |
| `KernelProviderHelper` | `APP.BL/AIAgent/GenericAgent/` | Optional facade over `AIConfigSettingBL` that accepts an explicit `AgentExecutionContext`; it must not resolve shared credentials without a tenant context. | `GetProvider(executionContext)`, `GetApiKey(executionContext)`, `GetModel(executionContext)` |
| `AppAgentSkillSetBL` | `APP.BL/AIAgent/AiSkill/` | Tenant-scoped CRUD over `AppAgentSkillSet` in the current tenant database. Parameterized queries, no ORM. | `GetAll(executionContext)`, `GetByKey(executionContext, skillKey)`, `Upsert(executionContext, dto)`, `Delete(executionContext, skillKey)` |
| `AppAgentToolEngine` | `APP.BL/TenantBusiness/` | Strategy dispatcher: routes tool calls by `ToolType` to the correct executor. | `Dispatch(toolType, toolConfig, args, context, ct)`, `BuildInvokersAsync()` |
| `BuiltInToolExecutor` | `APP.BL/TenantBusiness/AgentToolExecutors/` | Reflects a C# method by `TypeName.MethodName` from `ToolConfig`. | `ExecuteAsync(toolConfig, args, context, ct)` |
| `SqlQueryToolExecutor` | `APP.BL/TenantBusiness/AgentToolExecutors/` | Runs a parameterized SQL query from `ToolConfig.SqlBody`. Never string-concatenates. | `ExecuteAsync(toolConfig, args, context, ct)` |
| `HttpRestToolExecutor` | `APP.BL/TenantBusiness/AgentToolExecutors/` | HTTP GET/POST with `{argName}` URL substitution from `ToolConfig`. | `ExecuteAsync(toolConfig, args, context, ct)` |
| `DynamicCSharpToolExecutor` | `APP.BL/TenantBusiness/AgentToolExecutors/` | Roslyn `CSharpScript.EvaluateAsync` with namespace whitelist and timeout. | `ExecuteAsync(toolConfig, args, context, ct)` |
| `ExternalDllToolExecutor` | `APP.BL/TenantBusiness/AgentToolExecutors/` | `Assembly.LoadFrom` external DLL, invokes `IAgentTool.ExecuteAsync`. | `ExecuteAsync(toolConfig, args, context, ct)` |
| `PowerShellToolExecutor` | `APP.BL/TenantBusiness/AgentToolExecutors/` | Runs a PowerShell script file. Super-admin only. | `ExecuteAsync(toolConfig, args, context, ct)` |
| `GenericAgentCallbacks` | `APP.BL/AIAgent/GenericAgent/` | Delegate container wired by the controller to the session queue. | `OnToken`, `OnStep`, `OnDone`, `OnError`, `OnPlanReady`, `OnSchemaReady` |
| `GenericAgentSessionStore` | `APP.BL/AIAgent/GenericAgent/` | Session event queue + gate state. An in-memory implementation is single-node only; distributed storage is required for multi-node deployment. | `CreateSession()`, `Enqueue()`, `DequeueAll()`, `WaitForEventAsync()`, `RegisterPlanConfirmation()`, `ConfirmPlan()`, `ConfirmSchema()` |
| `GenericAgentController` | `AppAI.Web/Controllers/` | HTTP layer: creates session, fires background Task, streams SSE, resolves gates. | `RunAgent()`, `StreamEvents()`, `PollEvents()`, `ConfirmPlan()`, `ConfirmSchema()` |
| `AgentSkillSetManagement.tsx` | `AppReact/src/components/aiskill/` | Admin UI: FlexGrid skill list, editor panel with capability checkboxes, Run button. | — |
| `GenericAgentChat.tsx` | `AppReact/src/components/aiskill/` | Reusable streaming chat component. Handles tokens, steps, plan gate, session state. | — |
| `agentSkillSetSvc.ts` | `AppReact/src/webapi/` | TypeScript service for SkillSet CRUD API calls. | `GetAllSkillSets()`, `UpsertSkillSet()`, `DeleteSkillSet()` |
| `genericAgentSvc.ts` | `AppReact/src/webapi/` | TypeScript service for running agents. SSE via EventSource. | `RunAgent()`, `ConfirmPlan()`, `disconnect()` |

---

## 3. Database Schema

### 3.1 AppAgentSkillSet — Agent Persona Registry

Created by migration V008.

| Column | Type | Description |
|---|---|---|
| `SkillKey` | NVARCHAR(100) PK | Unique identifier used in every API call (e.g. `app-builder`, `db-genie`) |
| `DisplayName` | NVARCHAR(200) | Human-readable name shown in the admin UI |
| `Description` | NVARCHAR(MAX) | Short description of what the agent does |
| `SystemPrompt` | NVARCHAR(MAX) | Complete agent instruction text. Migrated from hardcoded C# strings. |
| `CapabilityFlags` | INT | Bitmask of enabled behaviours (see §4) |
| `IsActive` | BIT | Soft-delete flag; inactive agents are excluded from GetAll() |
| `SortOrder` | INT | Display order in the admin list |
| `Version` | INT | Schema version; incremented on significant prompt changes |
| `MaxHistoryTokens` | INT | Prune conversation history when it exceeds this token estimate |
| `SummarizeThreshold` | INT | LLM summarizes old turns when history exceeds this; 0 = off |
| `MaxToolResultChars` | INT | Hard cap on characters returned from any single tool call |
| `RecentWindowSize` | INT | Always keep the last N turns unpruned when sliding the history window |

**Seeded rows (V008, applied to each tenant schema during provisioning):**

| SkillKey | CapabilityFlags | MaxHistoryTokens | MaxToolResultChars |
|---|---|---|---|
| `app-builder` | 31 (Stream+MultiTurn+PlanGate+SchemaGate+InjectMemory) | 80 000 | 4 000 |
| `app-report` | 3 (Stream+MultiTurn) | 40 000 | 2 000 |
| `db-genie` | 35 (Stream+MultiTurn+InjectSchema) | 40 000 | 8 000 |
| `data-integration` | 65 (Stream+ExternalBackend) | 20 000 | 2 000 |

### 3.2 AppAgentToolRegister — Tool Registry

| Column | Type | Description |
|---|---|---|
| `ToolRegisterId` | INT IDENTITY PK | Auto-increment row ID |
| `SkillKey` | NVARCHAR(100) | FK to AppAgentSkillSet.SkillKey |
| `ToolName` | NVARCHAR(200) | LLM-facing function name (must be unique within SkillKey) |
| `ToolDescription` | NVARCHAR(MAX) | LLM-facing description of what the tool does and when to call it |
| `ParameterSchemaJson` | NVARCHAR(MAX) | JSON Schema `{"properties":{...},"required":[...]}` for the tool's parameters |
| `ToolType` | NVARCHAR(50) | Executor strategy: BuiltIn, ExternalDll, SqlQuery, PowerShell, HttpRest, DynamicCSharp |
| `ToolConfig` | NVARCHAR(MAX) | JSON configuration whose shape is determined by ToolType |
| `IsActive` | BIT | Inactive tools are excluded from kernel loading |

V008 seeds approximately 37 tool rows for the four built-in agent personas.

### 3.3 AppAgentMcpServer — MCP Server Registry

| Column | Type | Description |
|---|---|---|
| `McpServerId` | INT IDENTITY PK | Auto-increment row ID |
| `SkillKey` | NVARCHAR(100) | FK to AppAgentSkillSet.SkillKey |
| `ServerName` | NVARCHAR(200) | Display name; also used as the SK plugin group name prefix (`mcp_<name>`) |
| `ServerType` | NVARCHAR(50) | Transport type: `streamable-http` or `stdio` |
| `ServerUrl` | NVARCHAR(500) | Full HTTP URL for streamable-http servers |
| `Command` | NVARCHAR(500) | Executable + arguments for stdio servers |
| `IsActive` | BIT | Inactive servers are skipped at session start |

The engine currently handles `streamable-http` transport. `stdio` rows are skipped with a log warning until that executor is implemented.

### 3.4 Tenant Database Ownership and Routing

The existing App-netore tenancy model uses one tenant database per company. `AppMasterDB` owns identity, sessions, company registration, and `AppDataSourceRegister`; tenant databases own app definitions, security groups, business data, tenant settings, and tenant-scoped agent configuration.

The Generic Agent tables `AppAgentSkillSet`, `AppAgentToolRegister`, and `AppAgentMcpServer` are tenant-scoped and should be created in the tenant schema/template. Built-in personas and tools are seeded into a tenant during provisioning. If platform-wide templates are introduced later, they must be copied or resolved into the target tenant before editing or execution.

The controller resolves the authenticated session to a company and captures an immutable `AgentExecutionContext` before starting background work. The context contains the authenticated user ID, company ID, login type, tenant data-source identity, and authorization information. The engine and every tool executor use this context for tenant routing; no LLM argument or request-body field may select a database, connection string, or company.

Tenant database access must use `AppTenantAdapterBL.GetTenantAdapter()` and the existing data-source routing rules. Direct construction of a data adapter from a request or from `SkillKey` is prohibited. SysAdmin has no implicit tenant context; a SysAdmin agent run must explicitly select a company and then create a scoped execution context.

---

## 4. CapabilityFlags Bitmask

```csharp
[Flags]
public enum AgentCapabilityFlags
{
    None            = 0,
    StreamTokens    = 1,
    MultiTurn       = 2,
    PlanGate        = 4,
    SchemaGate      = 8,
    InjectMemory    = 16,
    InjectSchema    = 32,
    ExternalBackend = 64,
}
```

| Flag | Value | Runtime effect | Example persona |
|---|---|---|---|
| `StreamTokens` | 1 | Engine fires `OnToken` callbacks; controller SSE-streams each token | All built-in agents |
| `MultiTurn` | 2 | Client includes prior `Messages` in the request body; engine converts them to `ChatHistory` | All built-in agents |
| `PlanGate` | 4 | `propose_plan` tool pauses execution via `TaskCompletionSource` until `ConfirmPlan` endpoint is called | `app-builder` |
| `SchemaGate` | 8 | `propose_schema` tool pauses execution via `TaskCompletionSource` until `ConfirmSchema` endpoint is called | `app-builder` |
| `InjectMemory` | 16 | `GenericAgentBL.BuildSystemPrompt` calls `AppBuilderAgentMemoryBL.SearchMemory()` and appends relevant context | `app-builder` |
| `InjectSchema` | 32 | DB schema summary is injected into the system prompt (used by DB Genie to reason over table/column names) | `db-genie` |
| `ExternalBackend` | 64 | SK loop is skipped; request delegated to Cursor cloud or other external backend | `data-integration` |

**Composite examples:**
- `31` = 1+2+4+8+16 = Stream + MultiTurn + PlanGate + SchemaGate + InjectMemory (App Builder)
- `35` = 1+2+32 = Stream + MultiTurn + InjectSchema (DB Genie)
- `65` = 1+64 = Stream + ExternalBackend (Data Integration)

---

## 5. ToolType Strategy Pattern

`AppAgentToolEngine.Dispatch()` is the central dispatcher:

```csharp
return (toolType ?? "BuiltIn") switch
{
    "BuiltIn"       => BuiltInToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
    "ExternalDll"   => ExternalDllToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
    "SqlQuery"      => SqlQueryToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
    "PowerShell"    => PowerShellToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
    "HttpRest"      => HttpRestToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
    "DynamicCSharp" => DynamicCSharpToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
    _               => Task.FromResult("{\"Error\":\"Unknown ToolType\"}")
};
```

| ToolType | ToolConfig shape | Mechanism | Code required? | Security notes |
|---|---|---|---|---|
| `BuiltIn` | `{"TypeName":"Namespace.Class","MethodName":"Method"}` | Reflection into running assembly; method signature must accept `(IReadOnlyDictionary<string,string> args, AgentToolContext ctx, CancellationToken ct)` | Yes (C# class in APP.BL) | Runs in-process with full trust; developer-only |
| `ExternalDll` | `{"AssemblyName":"Tenant.dll","TypeName":"...","MethodName":"Run"}` | `Assembly.LoadFrom` from `ExternalDllRepository\` directory; type must implement `IAgentTool` | Yes (implement `IAgentTool` interface) | Loaded with full trust; version-check the assembly |
| `SqlQuery` | `{"SqlBody":"SELECT ... WHERE Col=@param","ReturnType":"json"}` | Parameterized `SqlCommand` against tenant DB; `@param` binding from LLM args | No (SQL only) | Never string-concatenates; parameterized only; SELECT preferred |
| `PowerShell` | `{"ScriptPath":"scripts/export.ps1"}` | `PowerShell.Create()`, runs script file, returns stdout | Script file only | Super-admin use only; restrict directory |
| `HttpRest` | `{"Url":"https://.../{argName}","Method":"GET","TokenStoreKey":"key"}` | `HttpClient` with `{argName}` placeholder substitution; token from key store | No (config only) | URL validated; no arbitrary redirect |
| `DynamicCSharp` | `{"ScriptBody":"...","AllowedNamespaces":["System"],"TimeoutSeconds":10}` | Roslyn `CSharpScript.EvaluateAsync` with `ScriptOptions` namespace whitelist | No (LLM or admin writes script) | Allowed: System, Linq, Collections.Generic, Text, Text.Json. Blocked: IO, Net, Reflection, Diagnostics. Every execution logged with userId, skillKey, toolName, code. |

All six executor results are truncated to `MaxToolResultChars` by `GenericAgentEngine` before being stored in chat history.

---

## 6. LLM Provider Configuration Chain

`AIConfigSettingBL` is the single source of truth. It reads exclusively from the selected tenant's `AppTenantSetting` rows seeded by V009. There is no `appsettings.json` fallback — a missing or empty key means the provider returns an empty string, which the engine will detect when building the kernel.

**Resolution order for a request with an `AgentExecutionContext` (including background execution):**

```
AIConfigSettingBL.GetProvider(executionContext)
  → AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigProvider, executionContext.tenantContext)
  → Default: "Gemini"

AIConfigSettingBL.GetApiKey(executionContext)
  → switch(provider):
      "openai"    → AIConfigOpenAIApiKey tenant setting
      "anthropic" → AIConfigAnthropicApiKey tenant setting
      default     → AIConfigGeminiApiKey tenant setting

AIConfigSettingBL.GetModel(executionContext)
  → switch(provider):
      "openai"    → AIConfigOpenAIModel (default: "gpt-4o")
      "anthropic" → AIConfigAnthropicModel (default: "claude-3-5-sonnet-20241022")
      default     → AIConfigGeminiModel (default: "gemini-2.0-flash")
```

**Requests without tenant identity** cannot use tenant-scoped settings. The admin test path must authenticate the caller and explicitly select a company before creating an `AgentExecutionContext`; a shared/global provider-key fallback is not permitted in production.

**Kernel construction per provider** (`GenericAgentEngine.BuildKernel`):

| Provider | SK registration | Special handling |
|---|---|---|
| `Anthropic` | `builder.Services.AddSingleton<IChatCompletionService>(new AnthropicChatCompletionService(model, apiKey))` | Custom wrapper; no official SK connector package for Anthropic |
| `Gemini` | `builder.AddGoogleAIGeminiChatCompletion(model, apiKey, httpClient: new HttpClient(new GeminiRoleFixHandler()))` | `GeminiRoleFixHandler` patches tool-result role from `"function"` to `"user"` in outgoing JSON body — workaround for SK Connectors.Google 1.74.0-alpha bug |
| `OpenAI` | `builder.AddOpenAIChatCompletion(model, apiKey)` | Standard SK extension |

---

## 7. Semantic Kernel Integration

**Kernel construction** happens per-request in `GenericAgentEngine.BuildKernel`. The kernel is not cached — a fresh instance is built for each `RunAsync` call to avoid state leakage between sessions.

**Tools are only registered when present.** `FunctionChoiceBehavior.Auto()` is set on `PromptExecutionSettings` only when `kernel.Plugins.Any(p => p.Any())` is true. Gemini (and some other providers) reject requests with an empty tools array; this guard prevents that error.

**IFunctionInvocationFilter** (`AgentStepFilter`) is added to the kernel after construction. It fires `OnStep` callbacks before and after each tool invocation so the controller can emit step events to the client.

**ChatCompletionAgent** is configured with:
- `Kernel` — the built kernel with all plugins
- `Instructions` — `skillSet.SystemPrompt`
- `Arguments` — `KernelArguments` wrapping the `PromptExecutionSettings`

**Streaming** uses `agent.InvokeStreamingAsync(thread, cancellationToken: ct)`. Each chunk's `Message.Content` is appended to `fullResponse` and fired as an `OnToken` callback.

**ChatHistory** is built from the `chatHistory` parameter (a `List<JObject>` passed by the client from prior turns). Roles `"user"` and `"assistant"`/`"model"` are mapped; other roles are ignored.

---

## 8. MCP Integration

The engine uses `ModelContextProtocol` v1.2.0. The `AsKernelFunction()` extension method available in that package is **not used** — it causes `MissingMethodException` at runtime due to a `Microsoft.Extensions.AI` version conflict with the rest of the project's dependencies. Tools are wrapped manually instead.

**Connection flow (per MCP server row):**

```csharp
// 1. Build transport
var transportOptions = new HttpClientTransportOptions
{
    Endpoint      = new Uri(server.ServerUrl),
    TransportMode = HttpTransportMode.StreamableHttp
};
var transport = new HttpClientTransport(transportOptions, McpHttpClient,
    NullLoggerFactory.Instance, ownsHttpClient: false);

// 2. Connect and list tools
var client = await McpClient.CreateAsync(transport, cancellationToken: ct);
var tools  = await client.ListToolsAsync(cancellationToken: ct);

// 3. Wrap each tool as KernelFunction
var functions = tools.Select(t => KernelFunctionFactory.CreateFromMethod(
    async (KernelArguments args, CancellationToken ct) => {
        var result = await client.CallToolAsync(toolName, mcpArgs, ct);
        return Truncate(text, cap);
    },
    functionName:    SanitizeName(toolName),
    description:     description,
    parameters:      parameters,      // parsed from tool.JsonSchema
    returnParameter: returnParameter
)).ToArray();

// 4. Add plugin group
kernel.Plugins.Add(KernelPluginFactory.CreateFromFunctions("mcp_" + serverName, functions));
```

A single shared static `HttpClient` (`McpHttpClient`) with `Timeout = InfiniteTimeSpan` is used for all MCP connections. MCP tool calls have a 30-second per-call `CancellationTokenSource` timeout linked to the outer cancellation token.

If `McpClient.CreateAsync` throws, the server is skipped with a `log.Warn` and the agent continues with remaining tools.

MCP clients implement `IAsyncDisposable` (or `IDisposable` as fallback); all are disposed in the `finally` block of `RunAsync`.

**Tool name sanitization:** `SanitizeName()` replaces any character that is not `[a-zA-Z0-9_]` with `_`, and prepends `_` if the name starts with a digit (Gemini requirement).

---

## 9. Session and Context Management

**Session lifecycle:**

1. `GenericAgentController.RunAgent` validates the caller, resolves the authenticated company, and creates an immutable `AgentExecutionContext` from the validated identity and tenant data-source registration.
2. `GenericAgentSessionStore.CreateSession()` returns a new GUID `sessionId` bound to the user, company, and execution context.
3. A `GenericAgentCallbacks` object is constructed, wiring all delegates to enqueue events for that session ID.
4. Background execution starts with the captured context; it must not depend on thread-static `ServerContext` surviving across `Task.Run` or `async/await`. The HTTP response returns immediately with `{ IsStarted: true, SessionId: "..." }`.
5. The client opens a GET `/StreamEvents?sessionId=...` SSE connection. The controller verifies session ownership and company ownership, then loops, calling `WaitForEventAsync` (up to 30s long-poll), dequeuing and flushing events.
6. When `OnDone` or `OnError` is enqueued, the client closes the SSE connection; the controller's loop exits and the session is cleaned up after its retention period.

**Multi-turn context:** The client (React component) maintains the conversation history locally. On each send, it builds a `Messages` array from all prior messages and includes it in the request body. `GenericAgentEngine.BuildChatHistory` converts this list into a `ChatHistory` object. There is no server-side history store between requests.

**Plan gate flow:**

1. The BuiltIn `propose_plan` tool calls `OnPlanReady(planEvent)`.
2. The controller registers a gate ID and confirmation state atomically, then enqueues a `plan` event containing that gate ID. The event is bound to the session owner and company.
3. `await tcs.Task` suspends the background thread (up to 10 minutes; then auto-rejects).
4. The user clicks Approve or Reject → client POSTs to `ConfirmPlan` with the session ID, gate ID, and decision.
5. The endpoint verifies session ownership, company ownership, gate freshness, and one-time use before resolving the gate.
6. The background execution resumes; the tool returns `{Confirmed:true/false}` to the LLM.

Schema gate follows the same pattern via `ConfirmSchema`.

**Tool result truncation:** Before being stored in chat history, every tool result is truncated at `skillSet.MaxToolResultChars` characters with a `…` suffix appended. This prevents runaway context growth from large SQL dumps or MCP payloads.

---

## 10. NuGet Packages Required

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.SemanticKernel` | 1.74.0 | Agentic loop, `ChatCompletionAgent`, kernel, plugins |
| `Microsoft.SemanticKernel.Agents.Core` | 1.74.0 | `ChatCompletionAgent`, `ChatHistoryAgentThread` |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | 1.74.0 | OpenAI + Azure OpenAI provider |
| `Microsoft.SemanticKernel.Connectors.Google` | 1.74.0-alpha | Gemini provider (requires `GeminiRoleFixHandler` workaround) |
| `ModelContextProtocol` | 1.2.0 | MCP client — `McpClient`, `HttpClientTransport`, `HttpTransportMode.StreamableHttp`. Do NOT use `AsKernelFunction()`. |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | latest stable | `DynamicCSharp` ToolType — Roslyn sandbox |
| `Anthropic` | 12.42.0 | Official Anthropic SDK (not community `Anthropic.SDK`) |

---

## 11. Key Design Decisions

### Why not extend AppAISkill?

`AppAISkill` is a flat prompt library — individual prompt snippets users can store and retrieve. It has no concept of tools, MCP servers, capability flags, or context thresholds. Adding agent persona semantics to it would have required invasive schema changes to a stable feature used in production. The cleaner path was a dedicated `AppAgentSkillSet` table designed exactly for agent personas, with `AppAISkill` left completely untouched.

### Why no base-class / addon concept between agents?

The original design considered making agents composable (a base persona + optional addon modules). This was rejected because it added indirection without solving the core problem. The real duplication was infrastructure code (the agentic loop), not persona configuration. A single flat `AppAgentSkillSet` row with a full `SystemPrompt` is simpler to reason about, simpler to debug, and simpler to edit through the admin UI.

### Why fire-and-forget with SSE instead of long-polling WebSocket?

WebSocket requires explicit session management across load balancer nodes. SSE is HTTP/1.1 compatible, works through most proxies without configuration, and is simpler to implement in ASP.NET Core. The polling fallback (`PollEvents`) is included for environments where SSE is blocked.

### Why manual KernelFunction wrapping for MCP tools?

The `AsKernelFunction()` extension in `ModelContextProtocol` 1.2.0 causes `MissingMethodException` at runtime because it references a version of `Microsoft.Extensions.AI.Abstractions` that conflicts with the version pulled in by `Microsoft.SemanticKernel`. Manual wrapping via `KernelFunctionFactory.CreateFromMethod` is a one-time implementation cost and is already proven by the BC-MCP-Client codebase that this project references.

### Why is chat history passed client-side?

Server-side history storage would require a distributed cache or database writes on every message exchange, adding latency and operational complexity. The client already renders all messages; including them in the next request adds negligible payload size for typical conversations. This approach also means the server is stateless between requests (except for active gate TCS objects), which simplifies horizontal scaling.

### Why the GeminiRoleFixHandler?

Semantic Kernel's Google connector v1.74.0-alpha sets the role of tool-result turns to `"function"` in the JSON it sends to the Gemini API. The Gemini API only accepts `"user"` for that turn. The fix is a `DelegatingHandler` that intercepts every outgoing HTTP request and patches `contents[].role` from `"function"` to `"user"` before the bytes leave the process. This is a pure workaround for an upstream SDK bug and should be removed when the connector is fixed.
