# Generic AI Agent — Execution Plan

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-02 (last audited: 2026-09-03)  
**Status:** Phases 0–5 Complete · Phase 6 In Progress

---

## Implementation Status Summary

| Phase | Description | Status | Notes |
|---|---|---|---|
| 0 | DB Migration (3 tables + seed data) | ✅ Done | V008 + V009 deployed |
| 1 | APP.Framework Extensions | ✅ Done | IAgentTool + AgentToolAttribute |
| 2 | Plugin Attribute Migration | 🔄 Partial | Old plugin files being deleted; attribute migration incomplete |
| 3 | AppAgentToolEngine + 6 Executors | ✅ Done | All executors in TenantBusiness |
| 4 | GenericAgent Infrastructure | ✅ Done | + AnthropicChatCompletionService + AgentStepFilter added |
| 5 | SkillSet Admin UI Layer | ✅ Done | React + BL + controller all implemented |
| 6 | Old Controller Cleanup | 🔄 In Progress | Old BL files being deleted; controllers not yet migrated |

---

## 1. Problem Statement

App-netore currently has 4 independent AI agents:

| Agent | Entry Point | Problem |
|---|---|---|
| AppBuilder Agent | `AppBuilderAgentBL.cs` | ~300-line agentic loop, hardcoded system prompt |
| AppReport Agent | `AppReportAgentBL.cs` | Same loop copy-pasted |
| DB Genie | `AppDbGenieBL.cs` | Same loop copy-pasted again |
| Data Integration Agent | `AppDataIntegrationAgentBL.cs` | Cursor cloud adapter, same pattern |

**Root problems:**
- `BuildToolDefinitions`, `InvokeToolAsync`, `PruneMessages`, `CallLLMWithToolsAsync` copy-pasted ~600 lines across 3 BL files
- Adding a 5th agent means copying the entire infrastructure again
- No SaaS extensibility — new agent tools require recompile and redeploy
- System prompts hardcoded in C# (no runtime editing)

---

## 2. Goal

One **GenericAgentBL** entry point + one **GenericAgentEngine** (Semantic Kernel loop).  
Agent behavior driven entirely by **DB configuration** — no C# class needed per agent persona.  
New agent personas and tools deploy without recompiling App-netore.

**Backward compatibility:** All existing controller routes unchanged. No frontend changes required for agent invocation.

---

## 3. Terminology

| Term | What it means | Where stored |
|---|---|---|
| **SkillSet** | An agent persona — identity, system prompt, capability flags | `AppAgentSkillSet` table |
| **Tool** | A KernelFunction the agent can call | `AppAgentToolRegister` table |
| **AppAISkill** | Separate user-created prompt library — **not used for agent system prompts** | `AppAISkill` table (unchanged) |

**Key rule:** Agent system prompt lives entirely in `AppAgentSkillSet.SystemPrompt`. `AppAISkill` is a separate feature (non-agent custom prompt library) and is **not extended or modified** by this refactor.

A SkillSet owns:
- One **system prompt** column (`AppAgentSkillSet.SystemPrompt`) — the core persona instruction
- Zero or more **tools** (registered in `AppAgentToolRegister`)
- Zero or more **MCP servers** (registered in `AppAgentMcpServer`)

---

## 4. Architecture Overview

```
Admin UI (AgentSkillSetManagement)            Existing agent UIs
  [▶ Run] button per SkillSet                 (AppBuilder, DbGenie, etc.)
        │                                              │
        ▼                                              ▼
GenericAgentController          AppBuilderAgentController / AppReportAgentController / DbGenieController
POST /webapi/GenericAgent/RunAgent   POST /webapi/AppBuilderAgent/RunAgent  (routes unchanged)
GET  /webapi/GenericAgent/StreamEvents                 │
        │                                              │
        └──────────────────────┬───────────────────────┘
                               ▼
                   GenericAgentBL.RunAsync(skillKey, userMessage, chatHistory, callbacks, identity, ct)
                               │
                   1. Resolve AppClientIdentity → dsId, userId, companyId
                   2. Load SkillSet via AiSkill.AppAgentSkillSetBL.GetByKey()
                   3. Build SK Kernel (provider from AIConfigSettingBL)
                      └─ Anthropic → AnthropicChatCompletionService (custom, no official SK package)
                      └─ Gemini   → AddGoogleAIGeminiChatCompletion + GeminiRoleFixHandler
                      └─ OpenAI   → AddOpenAIChatCompletion
                   4. Register AgentStepFilter (IFunctionInvocationFilter)
                   5. Load tools  (TenantBusiness.AppAgentToolRegisterBL → WrapRegisteredTool)
                   6. Connect MCP (TenantBusiness.AppAgentMcpServerBL → McpClient + KernelFunctionFactory)
                   7. Run SK ChatCompletionAgent.InvokeStreamingAsync loop
                               │
                      GenericAgentEngine (inner)
                               │
          ┌────────────────────┼────────────────────┐
   KernelFunction        KernelFunction        KernelFunction
   (BuiltIn/SqlQuery/…)  (MCP auto-discovered)  (DynamicCSharp)
                               │
                      SSE stream events (StreamEvents endpoint)
                               │
                      <GenericAgentChat />
```

---

## 5. DB Schema Design

### 5.1 `AppAgentSkillSet` — Agent Persona Registry ✅ COMPLETE (V008)

```sql
CREATE TABLE dbo.AppAgentSkillSet (
    SkillKey           NVARCHAR(100) NOT NULL,
    DisplayName        NVARCHAR(200) NOT NULL,
    Description        NVARCHAR(MAX) NULL,
    SystemPrompt       NVARCHAR(MAX) NULL,
    CapabilityFlags    INT           NOT NULL DEFAULT 0,
    IsActive           BIT           NOT NULL DEFAULT 1,
    SortOrder          INT           NOT NULL DEFAULT 0,
    Version            INT           NOT NULL DEFAULT 1,
    MaxHistoryTokens   INT           NOT NULL DEFAULT 80000,
    SummarizeThreshold INT           NOT NULL DEFAULT 60000,
    MaxToolResultChars INT           NOT NULL DEFAULT 4000,
    RecentWindowSize   INT           NOT NULL DEFAULT 10,
    CONSTRAINT PK_AppAgentSkillSet PRIMARY KEY (SkillKey)
);
```

4 rows seeded with full system prompts (migrated from hardcoded BL text).

### 5.2 `CapabilityFlags` Enum ✅ COMPLETE (AppEnums.cs EmTenantSettings range 3201–3207)

```csharp
[Flags]
public enum AgentCapabilityFlags
{
    None            = 0,
    StreamTokens    = 1,   // stream tokens via SSE
    MultiTurn       = 2,   // keep conversation history
    PlanGate        = 4,   // pause on propose_plan → wait for ConfirmPlan
    SchemaGate      = 8,   // pause on propose_schema → wait for ConfirmSchema
    InjectMemory    = 16,  // prepend AppBuilderAgentMemoryBL.SearchMemory() to prompt
    InjectSchema    = 32,  // prepend DB schema summary to prompt
    ExternalBackend = 64,  // skip SK loop → delegate to external backend
}
```

Seeded flag values:
- `app-builder` = 31 (Stream + MultiTurn + PlanGate + SchemaGate + InjectMemory)
- `app-report`  = 3  (Stream + MultiTurn)
- `db-genie`    = 35 (Stream + MultiTurn + InjectSchema)
- `data-integration` = 65 (Stream + ExternalBackend)

**End-user exposure:** flags are NOT shown as raw numbers in the admin UI. The wizard uses guided behavior questions that compute the bitmask.

### 5.3 `AppAgentToolRegister` — Tool Registry ✅ COMPLETE (V008)

```sql
CREATE TABLE dbo.AppAgentToolRegister (
    ToolRegisterId      INT           IDENTITY(1,1) PRIMARY KEY,
    SkillKey            NVARCHAR(100) NOT NULL,
    ToolName            NVARCHAR(200) NOT NULL,
    ToolDescription     NVARCHAR(MAX) NULL,    -- NOTE: column is "ToolDescription", not "Description"
    ParameterSchemaJson NVARCHAR(MAX) NULL,
    ToolType            NVARCHAR(50)  NOT NULL DEFAULT 'BuiltIn',
    ToolConfig          NVARCHAR(MAX) NULL,
    IsActive            BIT           NOT NULL DEFAULT 1
    -- NOTE: No SortOrder column in this schema
);
```

~37 built-in tool rows seeded (app-builder: 34, app-report: 5).

> **Schema Mismatch Warning:** `APP.BL/AIAgent/GenericAgent/AppAgentToolRegisterBL.cs` reads
> columns `Id`, `Description`, `SortOrder` which do NOT exist in V008. This file is stale
> and will fail at runtime. The **correct** BL for the engine is
> `APP.BL/TenantBusiness/AppAgentToolRegisterBL.cs` which reads the actual V008 columns
> (`ToolRegisterId`, `ToolDescription`, `ParameterSchemaJson`).
> See §17 (Known Deviations) for full list.

### 5.4 `AppAgentMcpServer` — MCP Server Registry ✅ COMPLETE (V008)

```sql
CREATE TABLE dbo.AppAgentMcpServer (
    McpServerId   INT           IDENTITY(1,1) PRIMARY KEY,
    SkillKey      NVARCHAR(100) NOT NULL,
    ServerName    NVARCHAR(200) NOT NULL,
    ServerType    NVARCHAR(50)  NOT NULL,   -- 'streamable-http' | 'stdio'
    ServerUrl     NVARCHAR(500) NULL,
    Command       NVARCHAR(500) NULL,
    IsActive      BIT           NOT NULL DEFAULT 1
);
```

> **Schema Mismatch Warning:** `APP.BL/AIAgent/GenericAgent/AppAgentMcpServerBL.cs` reads
> columns `McpServerKey`, `Transport`, `AuthType`, `AuthValue` which do NOT exist in V008.
> The **correct** BL for the engine is `APP.BL/TenantBusiness/AppAgentMcpServerBL.cs` which
> reads the actual V008 columns (`McpServerId`, `SkillKey`, `ServerName`, `ServerType`,
> `ServerUrl`, `Command`). The GenericAgent namespace file is stale/forward-looking.

### 5.5 `AppTenantSetting` — AI Config Rows ✅ COMPLETE (V009)

7 rows added to `AppTenantSetting` (idempotent):

| SetupCode | Default | EmTenantSettings enum |
|---|---|---|
| `AIConfigProvider` | `Gemini` | 3201 |
| `AIConfigOpenAIApiKey` | `` | 3202 |
| `AIConfigGeminiApiKey` | `` | 3203 |
| `AIConfigAnthropicApiKey` | `` | 3204 |
| `AIConfigOpenAIModel` | `gpt-4o` | 3205 |
| `AIConfigGeminiModel` | `gemini-2.0-flash` | 3206 |
| `AIConfigAnthropicModel` | `claude-3-5-sonnet-20241022` | 3207 |

Each provider keeps its own API key row — tenants can configure multiple providers and switch via `AIConfigProvider`.

---

## 6. Tool Execution Strategy

`AppAgentToolRegister.ToolType` drives a strategy pattern. Six types supported:

| ToolType | Mechanism | Who configures | Code required? |
|---|---|---|---|
| `BuiltIn` | Reflect C# method from running assembly | Platform dev (SQL seed) | Yes — C# class in APP.BL |
| `ExternalDll` | `Assembly.LoadFrom(ExternalDllRepository\)` → `IAgentTool.ExecuteAsync` | Tenant dev (DLL drop) | Yes — implement `IAgentTool` |
| `SqlQuery` | `SqlCommand` with `@param` binding; never string concat | Tenant admin (UI only) | **No** |
| `PowerShell` | `PowerShell.Create()` → script → `Invoke()` | Super-admin only | Script file only |
| `HttpRest` | `HttpClient` → `{argName}` URL placeholders → response | Tenant admin (UI only) | **No** |
| `DynamicCSharp` | Roslyn `CSharpScript.EvaluateAsync` — whitelisted sandbox | Admin configures; LLM generates code | **No** (LLM writes C#) |

### Strategy dispatch in `AppAgentToolEngine.Dispatch` ✅ COMPLETE

```csharp
// APP.BL/TenantBusiness/AppAgentToolEngine.cs
public static Task<string> Dispatch(
    string                              toolType,
    string                              toolConfig,
    IReadOnlyDictionary<string, string> args,
    AgentToolContext                    context,
    CancellationToken                   ct)
{
    return (toolType ?? "BuiltIn") switch
    {
        "BuiltIn"       => BuiltInToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
        "ExternalDll"   => ExternalDllToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
        "SqlQuery"      => SqlQueryToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
        "PowerShell"    => PowerShellToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
        "HttpRest"      => HttpRestToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
        "DynamicCSharp" => DynamicCSharpToolExecutor.ExecuteAsync(toolConfig, args, context, ct),
        _               => Task.FromResult(JsonConvert.SerializeObject(new { Error = $"Unknown ToolType: {toolType}" }))
    };
}
```

### ToolConfig JSON shapes

**BuiltIn:**
```json
{ "TypeName": "APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin", "MethodName": "GetTableSchema" }
```

**ExternalDll:**
```json
{ "AssemblyName": "Tenant.Reports.dll", "TypeName": "Tenant.Reports.ReportTool", "MethodName": "Run" }
```

**SqlQuery:**
```json
{ "SqlBody": "SELECT AppId, AppName FROM SaasApplication WHERE IsActive=1 AND AppId=@appId", "ReturnType": "json" }
```

**PowerShell:**
```json
{ "ScriptPath": "scripts/export.ps1" }
```

**HttpRest:**
```json
{ "Url": "https://api.erp.internal/apps/{appId}", "Method": "GET", "TokenStoreKey": "erp-api" }
```

**DynamicCSharp — stored script mode:**
```json
{ "ScriptBody": "return Args.UnitPrice * Args.Qty * (1 - Args.Discount);", "AllowedNamespaces": ["System"], "TimeoutSeconds": 10 }
```

**DynamicCSharp — live dynamic mode** (LLM passes `code` as arg at call time):
```json
{ "AllowedNamespaces": ["System", "System.Linq", "System.Collections.Generic"], "TimeoutSeconds": 10 }
```

### DynamicCSharp Security Requirements

- **Whitelist namespaces only:** `System`, `System.Linq`, `System.Collections.Generic`, `System.Text`, `System.Text.Json`
- **No access to:** `System.IO`, `System.Net`, `System.Reflection`, `System.Diagnostics`, APP.BL
- **`ScriptGlobals`:** exposes only pre-fetched `Dictionary<string, object> Args` — no raw `SqlConnection`
- **Timeout:** enforced via `CancellationTokenSource`
- **Audit log:** every code execution logged with `userId`, `skillKey`, `toolName`, `code`

---

## 7. SkillSet Management UI ✅ COMPLETE

`AppAISkillBL.cs` and `AISkillManagement.tsx` are **not changed** — they remain the non-agent prompt library.

### BL layer — two separate classes ✅

**`App.BL.AIAgent.AiSkill.AppAgentSkillSetBL`** (for the runtime engine):
```csharp
// APP.BL/AIAgent/AiSkill/AppAgentSkillSetBL.cs
GetAll()                           // list all active
GetByKey(string skillKey)          // engine uses this
GetByKey(string skillKey, int dsId)// admin controller uses this overload
```

**`App.BL.AIAgent.GenericAgent.AppAgentSkillSetBL`** (for admin UI with explicit dsId):
```csharp
// APP.BL/AIAgent/GenericAgent/AppAgentSkillSetBL.cs
GetAllSkillSets(int dataSourceId)
UpsertSkillSet(int dataSourceId, AppAgentSkillSetDto dto)  // create or update (no separate Create/Update)
DeleteSkillSet(int dataSourceId, string skillKey)           // hard delete (not IsActive=0)
GetDebugInfo(int dataSourceId)                              // diagnostics: conn string + row count
```

> **Design deviation:** The plan described separate `CreateSkillSet` and `UpdateSkillSet` methods.
> The implementation uses a single `UpsertSkillSet` (IF EXISTS UPDATE ELSE INSERT).
> `DeleteSkillSet` performs a hard DELETE, not soft-delete to IsActive=0.

### Controller endpoints ✅ (`AgentSkillSetController.cs`)

```
GET  GetDefaultDataSourceId()
GET  GetDebugInfo()
GET  GetAllSkillSets()
POST UpsertSkillSet([FromBody] AppAgentSkillSetDto dto)
DEL  DeleteSkillSet(string skillKey)
GET  GetToolsBySkillKey(string skillKey)
POST UpsertTool([FromBody] AppAgentToolRegisterDto dto)
DEL  DeleteTool(int id)
GET  GetAllMcpServers()
POST UpsertMcpServer([FromBody] AppAgentMcpServerDto dto)
DEL  DeleteMcpServer(int mcpServerId)
```

Note: `ToolBL = App.BL.TenantBusiness.AppAgentToolRegisterBL` and
`McpBL = App.BL.TenantBusiness.AppAgentMcpServerBL` — the controller uses the TenantBusiness
versions (which match V008), NOT the GenericAgent namespace versions.

### React UI ✅

```
AppReact/src/components/aiskill/
  AgentSkillSetManagement.tsx    — persona grid + editor panel + behavior wizard + [▶ Run] button
  GenericAgentChat.tsx           — reusable streaming chat component
  AgentToolRegisterTab.tsx       — tool CRUD tab within skill editor
  AgentMcpServerTab.tsx          — MCP server CRUD tab within skill editor
AppReact/src/webapi/
  agentSkillSetSvc.ts            — SkillSet/Tool/MCP CRUD service calls
  genericAgentSvc.ts             — RunAgent / SSE stream / ConfirmPlan / ConfirmSchema calls
```

---

## 8. GenericAgentBL — Core Entry Point ✅ COMPLETE

**Actual signature (differs from original plan):**

```csharp
// APP.BL/AIAgent/GenericAgent/GenericAgentBL.cs
public static class GenericAgentBL
{
    public static async Task RunAsync(
        string                skillKey,
        string                userMessage,
        List<JObject>         chatHistory,    // ← multi-turn history passed by caller (not stored server-side)
        GenericAgentCallbacks callbacks,
        AppClientIdentity?    identity,       // ← replaces AgentContext ctx from plan
        CancellationToken     ct)
    {
        // Delegates directly to GenericAgentEngine.RunAsync
    }
}
```

**Key design differences from original plan:**
- No `AgentContext ctx` parameter — identity is `AppClientIdentity?`
- History is passed by caller as `List<JObject>` (user/assistant alternating, LLM-native format) — not fetched from session store
- No `GenericAgentSession` object — `GenericAgentSessionStore` is used for event queuing only (not history storage)
- Returns `Task` (not `Task<string>`) — session ID is created by the controller before calling this method

**GenericAgentCallbacks (all delegates are optional):**

```csharp
// APP.BL/AIAgent/GenericAgent/GenericAgentCallbacks.cs
public sealed class GenericAgentCallbacks
{
    public Func<string, Task>                          OnToken     { get; set; }  // each streamed token
    public Func<AgentStepEvent, Task>                  OnStep      { get; set; }  // tool_call / tool_result / thinking
    public Func<string, Task>                          OnDone      { get; set; }  // final response
    public Func<string, Task>                          OnError     { get; set; }  // unrecoverable error
    public Func<AgentPlanEvent, Task<bool>>            OnPlanReady { get; set; }  // PlanGate (optional)
    public Func<AgentSchemaEvent, Task<AgentSchemaResponse>> OnSchemaReady { get; set; }  // SchemaGate (optional)
}
```

### Controller changes (Phase 6 — PENDING)

```csharp
// AppBuilderAgentController — after (NOT YET DONE)
[HttpPost("RunAgent")]
public async Task<IActionResult> RunAgent([FromBody] AppBuilderAgentRequestDto req)
{
    var sessionId = GenericAgentSessionStore.CreateSession();
    var callbacks = BuildCallbacks(sessionId);
    Task.Run(() => GenericAgentBL.RunAsync("app-builder", req.UserMessage,
        req.Messages ?? new List<JObject>(), callbacks, agentIdentity, CancellationToken.None));
    return Ok(new { IsStarted = true, SessionId = sessionId });
}

// AppReportAgentController — after (NOT YET DONE)
// DbGenieController — after (NOT YET DONE)
```

---

## 9. GenericAgentEngine — SK Agentic Loop ✅ COMPLETE

```csharp
// APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs
public static async Task RunAsync(
    string                skillKey,
    string                userMessage,
    List<JObject>         chatHistory,
    GenericAgentCallbacks callbacks,
    AppClientIdentity?    identity,
    CancellationToken     ct)
```

**Engine flow:**
1. Resolve `dsId`, `userId`, `companyId` from `AppClientIdentity`
2. Load `AppAgentSkillSetDto` via `AiSkill.AppAgentSkillSetBL.GetByKey(skillKey, dsId)`
3. Build SK `Kernel` via `BuildKernel(identity)` — provider selected from `AIConfigSettingBL`
4. Add `AgentStepFilter` (IFunctionInvocationFilter) — fires `OnStep` for tool_call / tool_result events
5. Load registered tools from `TenantBusiness.AppAgentToolRegisterBL.GetBySkillKey(skillKey, dsId)`
   → Each row wrapped via `KernelFunctionFactory.CreateFromMethod` → dispatched through `AppAgentToolEngine.Dispatch`
6. Connect MCP servers from `TenantBusiness.AppAgentMcpServerBL.GetBySkillKey(skillKey, dsId)`
   → Only `streamable-http` ServerType supported; stdio is ignored
   → `McpClient.CreateAsync(HttpClientTransport)` → `KernelFunctionFactory.CreateFromMethod` per tool
7. Build `ChatHistory` from `List<JObject>` (user/assistant messages)
8. Only enable `FunctionChoiceBehavior.Auto()` when tools are present (Gemini rejects empty tools array)
9. Run `ChatCompletionAgent.InvokeStreamingAsync` → fires `OnToken` per chunk, `OnDone` on completion

**Provider routing in BuildKernel:**

```csharp
switch (provider)
{
    case EmLLMProvider.Anthropic:
        // No official SK Anthropic connector — use custom wrapper
        builder.Services.AddSingleton<IChatCompletionService>(new AnthropicChatCompletionService(model, apiKey));
        break;
    case EmLLMProvider.Gemini:
        builder.AddGoogleAIGeminiChatCompletion(model, apiKey,
            httpClient: new HttpClient(new GeminiRoleFixHandler()));
        break;
    default:  // OpenAI
        builder.AddOpenAIChatCompletion(model, apiKey);
        break;
}
```

**Note:** In-session history pruning (Section 12 Level 1) and summarization (Level 2) are **not yet implemented** in the engine. All chat history passed in is used as-is. This is a pending enhancement.

---

## 10. AnthropicChatCompletionService ✅ COMPLETE (not in original plan)

```csharp
// APP.BL/AIAgent/GenericAgent/AnthropicChatCompletionService.cs
internal sealed class AnthropicChatCompletionService : IChatCompletionService
```

**Purpose:** SK has no official NuGet package for Anthropic. This class implements `IChatCompletionService` using direct HTTP calls to `https://api.anthropic.com/v1/messages`.

**What it handles:**
- Extracts system prompt from `ChatHistory` (System role messages)
- Serializes user/assistant turns to Anthropic message format
- Maps `FunctionCallContent` → Anthropic `tool_use` blocks
- Maps `FunctionResultContent` → Anthropic `tool_result` content
- Extracts tools from `FunctionChoiceBehavior` configuration and serializes as Anthropic tool definitions
- Parses response — handles `text` and `tool_use` content blocks into SK's `FunctionCallContent`
- Streaming is simulated (non-streaming request, yields results one chunk per item)

**NuGet note:** Uses direct HTTP, **not** the official `Anthropic` NuGet package (v12.42.0). The plan mentioned that package but it is not used here — direct HTTP is simpler and avoids `Microsoft.Extensions.AI` version conflicts.

---

## 11. AgentStepFilter ✅ COMPLETE (not in original plan)

```csharp
// APP.BL/AIAgent/GenericAgent/AgentStepFilter.cs
internal sealed class AgentStepFilter : IFunctionInvocationFilter
```

**What it does:** Wraps every SK tool call to fire `OnStep` events:
- Before tool call: fires `AgentStepEvent { Type = "tool_call", ToolName, Details = truncated args JSON }`
- After tool call: fires `AgentStepEvent { Type = "tool_result", ToolName, IsSuccess, Details = truncated result }`

Registered in `GenericAgentEngine.BuildKernel`:
```csharp
kernel.FunctionInvocationFilters.Add(new AgentStepFilter(callbacks));
```

---

## 12. GeminiRoleFixHandler (inner class in GenericAgentEngine — not in plan)

SK Connectors.Google 1.74.0-alpha bug: sends tool results with `role = "function"` but Gemini API only accepts `"user"` for that turn. The `GeminiRoleFixHandler : DelegatingHandler` patches every outgoing request body, replacing `role:"function"` with `role:"user"` in the `contents[].role` field. Also captures and logs Gemini error response bodies (SK's streaming mode never reads them otherwise).

---

## 13. GenericAgentController ✅ COMPLETE

```csharp
// AppAI.Web/Controllers/GenericAgentController.cs
// Route: webapi/[controller]/[action]
```

**Endpoints (actual implementation — differs from plan's simplified design):**

| Endpoint | Method | Description |
|---|---|---|
| `POST RunAgent` | fire-and-forget | Creates session, starts agent in background Task.Run, returns `{ IsStarted, SessionId }` |
| `GET  StreamEvents?sessionId=` | SSE | Long-poll SSE stream; sends `event: <type>\ndata: <json>` per event |
| `GET  PollEvents?sessionId=` | polling fallback | Dequeues all events for sessionId and returns them |
| `POST ConfirmPlan` | gate resolve | Resolves `PlanGate` TCS with `{ SessionId, Confirmed }` |
| `POST ConfirmSchema` | gate resolve | Resolves `SchemaGate` TCS with `{ SessionId, Confirmed, SchemaJson, Feedback }` |

**GenericAgentRequestDto (actual):**
```csharp
public class GenericAgentRequestDto
{
    public string        SkillKey    { get; set; }
    public string        UserMessage { get; set; }
    public string?       SessionId   { get; set; }   // unused by server (client manages sessions)
    public List<JObject> Messages    { get; set; }   // prior chat turns (user/assistant)
}
```

**SSE event types emitted:**

| EventType | When |
|---|---|
| `step` | Tool call starts or completes (fires `AgentStepFilter`) |
| `token` | Each text token from LLM |
| `plan` | PlanGate triggered — client should show Approve/Reject |
| `schema` | SchemaGate triggered — client shows schema for review |
| `done` | Run complete, `FinalResponse` field has full text |
| `error` | Unrecoverable error |

---

## 14. MCP Data Rendering — `MCPAppRenderer`

BC-MCP-Client's `client/src/mcp-components/` folder contains 9 actively-used React components that render structured MCP tool results.

> **Status: NOT YET copied to App-netore.** `AppReact/src/mcp-components/` does not exist.
> This is a remaining task for Phase 5 completion.

### 9 Active Components (from BC-MCP-Client)

| `ui_hint` | Component | Renders |
|---|---|---|
| `FlexGrid` | `AGGridMCP` | Sortable grid, CSV/Excel export |
| `ChartView` | `ChartView` | Bar / line / area (Recharts), type switcher |
| `Dashboard` / `DataAnalysis` | `Dashboard` | KPIs + charts + key insights + PDF/PPT export |
| `SelectorGrid` | `SelectorGrid` | Tile picker for single-item selection |
| `RecordCard` | `RecordCard` | Single record detail, color-coded status badge |
| `ActionMenu` | `ActionMenu` | Action button list (emits ACTION_SELECTED) |
| `DateRangeFilter` | `DateRangeFilter` | Date range + status filter |
| `Form` | `DynamicForm` | Multi-section form, cascade dropdowns via MCP tool |
| `PivotTable` | `PivotTable` | Cross-tab pivot with heat-map toggle |

### MCP Tool Result Shape

```json
{
  "ui_hint": "FlexGrid",
  "data":    [ { "PoId": 1, "Vendor": "Acme", "Amount": 5000 } ],
  "columns": [
    { "field": "PoId",   "headerName": "PO #" },
    { "field": "Vendor", "headerName": "Vendor" },
    { "field": "Amount", "headerName": "Amount", "dataType": "currency" }
  ],
  "meta":   { "title": "Open Purchase Orders", "total": 42 },
  "config": { "detailTool": "get_po_detail", "detailKeys": ["PoId"] }
}
```

The `ui_hint` rendering instruction block is already included in `AppAgentSkillSet.SystemPrompt` for both `app-builder` and `app-report` (seeded in V008).

---

## 15. Launching GenericAgent from SkillSet Management ✅ COMPLETE

The `[▶ Run]` button in `AgentSkillSetManagement.tsx` opens `<GenericAgentChat skillKey={selectedSkillKey} />` which calls:
1. `POST /webapi/GenericAgent/RunAgent` → receives `{ IsStarted, SessionId }`
2. `GET /webapi/GenericAgent/StreamEvents?sessionId=...` → SSE stream

### `<GenericAgentChat />` Is Reusable Everywhere

| Launch point | Usage |
|---|---|
| SkillSet management | Right panel — admin test mode |
| Application home page | Floating button → chat drawer |
| Dedicated route `/agent-chat?skillKey=db-genie` | Full-page layout |
| Embedded in any form page | Side panel alongside the form |

---

## 16. NuGet Dependencies ✅ COMPLETE (in APP.BL.Core.csproj)

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.SemanticKernel` | 1.74.0 | SK agentic loop |
| `Microsoft.SemanticKernel.Agents.Core` | 1.74.0 | `ChatCompletionAgent` |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | 1.74.0 | OpenAI + Azure OpenAI provider |
| `Microsoft.SemanticKernel.Connectors.Google` | 1.74.0-alpha | Gemini provider |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | latest stable | DynamicCSharp ToolType — Roslyn sandbox |
| `ModelContextProtocol` | 1.2.0 | MCP client — `McpClient`, `HttpClientTransport`, `HttpTransportMode.StreamableHttp` |

**Anthropic NuGet NOT used** — `AnthropicChatCompletionService` uses direct HTTP.
**Do NOT call `AsKernelFunction()`** on MCP tools — broken due to `Microsoft.Extensions.AI` version mismatch. Tools wrapped manually via `KernelFunctionFactory.CreateFromMethod`.

---

## 17. Long Session Context Management ⬜ NOT YET IMPLEMENTED

The architecture is designed for four levels but only Level 3 (cross-session RAG via InjectMemory flag) and Level 4 (InjectSchema flag) are wired. Levels 1 and 2 are pending.

### Level 1 — Token Budget + Pruning (NOT YET DONE)

Sliding window: keep system prompt + first 2 turns + last N turns. Prune from middle.

```csharp
// GenericAgentEngine — add BEFORE calling InvokeStreamingAsync
PruneHistory(history, persona.MaxHistoryTokens, persona.RecentWindowSize);

// Tool results capped before being added to history (DONE — MaxToolResultChars applied)
var cappedResult = result.Length > persona.MaxToolResultChars
    ? result[..persona.MaxToolResultChars] + $"\n[...truncated — {result.Length} chars total]"
    : result;
```

### Level 2 — Summarization for Long Sessions (NOT YET DONE)

When token count exceeds `SummarizeThreshold`, compress oldest messages into a summary block.

### Level 3 — Cross-Session RAG Memory (flag-driven)

`InjectMemory` flag (bit 16) is designed to call `AppBuilderAgentMemoryBL.SearchMemory()` and prepend results. Not yet wired in GenericAgentEngine — the flag is read but the injection is not implemented.

### Level 4 — InjectSchema (flag-driven, not yet wired)

`InjectSchema` flag (bit 32) is designed to prepend the DB schema summary. Not yet wired.

---

## 18. File Map — Actual State

### Files CREATED ✅

| File | Namespace / Class | Notes |
|---|---|---|
| `AppAI.Web/Migrations/V008__GenericAgentSchema.sql` | — | 3 tables + seed data; has stale debug SQL at end (lines 665-674) |
| `AppAI.Web/Migrations/V009__AIConfigTenantSettings.sql` | — | 7 tenant setting rows |
| `APP.Framework/Plugin/IAgentTool.cs` | `APP.Framework.Plugin` | Also defines `AgentToolContext` |
| `APP.Framework/Plugin/AgentToolAttribute.cs` | `APP.Framework.Plugin` | Also defines `AgentParamAttribute` |
| `APP.BL/TenantBusiness/AppAgentToolEngine.cs` | `App.BL.TenantBusiness` | Strategy dispatcher |
| `APP.BL/TenantBusiness/AppAgentToolRegisterBL.cs` | `App.BL.TenantBusiness` | Reads V008 schema (ToolRegisterId, ToolDescription) |
| `APP.BL/TenantBusiness/AppAgentMcpServerBL.cs` | `App.BL.TenantBusiness` | Reads V008 schema (McpServerId, SkillKey, ServerType) |
| `APP.BL/TenantBusiness/AgentToolExecutors/BuiltInToolExecutor.cs` | — | |
| `APP.BL/TenantBusiness/AgentToolExecutors/ExternalDllToolExecutor.cs` | — | |
| `APP.BL/TenantBusiness/AgentToolExecutors/SqlQueryToolExecutor.cs` | — | |
| `APP.BL/TenantBusiness/AgentToolExecutors/PowerShellToolExecutor.cs` | — | |
| `APP.BL/TenantBusiness/AgentToolExecutors/HttpRestToolExecutor.cs` | — | |
| `APP.BL/TenantBusiness/AgentToolExecutors/DynamicCSharpToolExecutor.cs` | — | Roslyn sandbox |
| `APP.BL/AIAgent/AiSkill/AppAgentSkillSetBL.cs` | `App.BL.AIAgent.AiSkill` | Engine-facing; `GetByKey(skillKey)` + `GetByKey(skillKey, dsId)` |
| `APP.BL/AIAgent/GenericAgent/AppAgentSkillSetBL.cs` | `App.BL.AIAgent.GenericAgent` | Admin-UI-facing; `UpsertSkillSet(dsId, dto)` |
| `APP.BL/AIAgent/GenericAgent/AppAgentToolRegisterBL.cs` | `App.BL.AIAgent.GenericAgent` | Admin-UI-facing; different schema than V008 (stale) |
| `APP.BL/AIAgent/GenericAgent/AppAgentMcpServerBL.cs` | `App.BL.AIAgent.GenericAgent` | Admin-UI-facing; different schema than V008 (stale) |
| `APP.BL/AIAgent/GenericAgent/GenericAgentBL.cs` | `App.BL.AIAgent.GenericAgent` | Entry point; delegates to GenericAgentEngine |
| `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs` | `App.BL.AIAgent.GenericAgent` | Full SK loop |
| `APP.BL/AIAgent/GenericAgent/GenericAgentSessionStore.cs` | `App.BL.AIAgent.GenericAgent` | Event queue; PlanGate + SchemaGate TCS |
| `APP.BL/AIAgent/GenericAgent/GenericAgentCallbacks.cs` | `App.BL.AIAgent.GenericAgent` | Callback delegates |
| `APP.BL/AIAgent/GenericAgent/KernelProviderHelper.cs` | `App.BL.AIAgent.GenericAgent` | Delegates to LLMProviderHelper/AIConfigSettingBL |
| `APP.BL/AIAgent/GenericAgent/AIConfigSettingBL.cs` | `App.BL.GenericAgent` | Reads AIConfig* from AppTenantSetting |
| `APP.BL/AIAgent/GenericAgent/AnthropicChatCompletionService.cs` | `App.BL.AIAgent.GenericAgent` | Custom SK IChatCompletionService for Anthropic |
| `APP.BL/AIAgent/GenericAgent/AgentStepFilter.cs` | `App.BL.AIAgent.GenericAgent` | IFunctionInvocationFilter — fires OnStep |
| `APP.Components.Dto/UserDefine/AISkill/GenericAgentDto.cs` | `APP.Components.Dto` | Request/result DTOs for GenericAgentController |
| `AppAI.Web/Controllers/GenericAgentController.cs` | — | RunAgent + StreamEvents + PollEvents + ConfirmPlan + ConfirmSchema |
| `AppAI.Web/Controllers/AgentSkillSetController.cs` | — | Full CRUD for SkillSet + Tool + McpServer |
| `AppReact/src/webapi/agentSkillSetSvc.ts` | — | SkillSet/Tool/MCP service calls |
| `AppReact/src/webapi/genericAgentSvc.ts` | — | RunAgent + SSE stream client |
| `AppReact/src/components/aiskill/AgentSkillSetManagement.tsx` | — | Admin UI — persona grid + editor + wizard |
| `AppReact/src/components/aiskill/GenericAgentChat.tsx` | — | Reusable chat component |
| `AppReact/src/components/aiskill/AgentToolRegisterTab.tsx` | — | Tool CRUD tab |
| `AppReact/src/components/aiskill/AgentMcpServerTab.tsx` | — | MCP server CRUD tab |

### Files NOT YET CREATED ⬜

| File | Purpose |
|---|---|
| `AppReact/src/mcp-components/` (copy from BC-MCP-Client) | 9 MCP render components + adapters + registry |
| In-session pruning logic in `GenericAgentEngine` | Level 1 pruning (Section 17) |
| Summarization logic in `GenericAgentEngine` | Level 2 (Section 17) |
| InjectMemory implementation in `GenericAgentEngine` | Level 3 (read AppBuilderAgentMemoryBL) |
| InjectSchema implementation in `GenericAgentEngine` | Level 4 |

### Files to MODIFY (Phase 6 — PENDING)

| File | Change needed |
|---|---|
| `AppAI.Web/Controllers/AppBuilderAgentController.cs` | Migrate body to `GenericAgentBL.RunAsync("app-builder",...)` |
| `AppAI.Web/Controllers/AppReportAgentController.cs` | Migrate body to `GenericAgentBL.RunAsync("app-report",...)` |
| `AppAI.Web/Controllers/DbGenieController.cs` | Migrate body to `GenericAgentBL.RunAsync("db-genie",...)` |
| `AppAI.Web/Migrations/V008__GenericAgentSchema.sql` | Remove stale debug SQL at lines 665-674 |
| `APP.BL/AIAgent/GenericAgent/AppAgentToolRegisterBL.cs` | Fix column names to match V008 (Id→ToolRegisterId, Description→ToolDescription, add ParameterSchemaJson, remove SortOrder) OR delete if admin uses TenantBusiness version |
| `APP.BL/AIAgent/GenericAgent/AppAgentMcpServerBL.cs` | Fix column names to match V008 (McpServerKey→McpServerId+ServerName, Transport→ServerType, remove AuthType/AuthValue) OR delete if admin uses TenantBusiness version |

### Files to DELETE (Phase 6 — IN PROGRESS from git status)

| File | Status | Reason |
|---|---|---|
| `APP.BL/AppBuilderAgent/AppBuilderAgentBL.cs` | ` D` (deleted, not staged) | Replaced by GenericAgentBL |
| `APP.BL/AppBuilderAgent/AgentFunctionAttribute.cs` | `D ` (staged) | Replaced by AgentToolAttribute |
| `APP.BL/AppBuilderAgent/AppBuilderAgentMemoryBL.cs` | `D ` (staged) | May need to keep if InjectMemory is implemented |
| `APP.BL/AppBuilderAgent/AppBuilderAgentSessionStore.cs` | ` D` (deleted) | Replaced by GenericAgentSessionStore |
| `APP.BL/AppBuilderAgent/Plugins/*.cs` (12 files) | `D ` (staged) | Replaced by DB-registered BuiltIn tools |
| `APP.BL/AppReportAgent/*.cs` | ` D` (deleted) | Replaced by GenericAgentBL |
| `APP.BL/AppDataIntegrationAgent/*.cs` | ` D` (deleted) | Replaced by GenericAgentBL |

### Files KEPT (plan said DELETE — now kept with delegation pattern)

| File | Reason kept |
|---|---|
| `APP.BL/AIAgent/DbGenie/LLMProviderHelper.cs` | Kept — `KernelProviderHelper` delegates to it; also used by `DbGenieBL` for non-SK paths |

---

## 19. Implementation Phases — Status

### Phase 0 — DB Migration ✅ COMPLETE

Run:
1. `V008__GenericAgentSchema.sql` — 3 tables + 37 tool seed rows + 4 agent personas with system prompts
2. `V009__AIConfigTenantSettings.sql` — 7 AI config rows in AppTenantSetting

**Verify:** `SELECT COUNT(*) FROM AppAgentSkillSet` = 4; `SELECT COUNT(*) FROM AppAgentToolRegister` ≈ 37; AppAgentMcpServer table exists; AppTenantSetting has 7 AIConfig* rows.

### Phase 1 — APP.Framework Extensions ✅ COMPLETE

`IAgentTool.cs` and `AgentToolAttribute.cs` in `APP.Framework/Plugin/`. Also contains `AgentToolContext` and `AgentParamAttribute`.

### Phase 2 — Plugin Attribute Migration 🔄 PARTIAL

Old plugin files staged for deletion. Attribute migration from `[AgentFunction]` to `[AgentTool]` is incomplete since the plugins themselves are being deleted (not migrated). The tools are now DB-registered as BuiltIn entries pointing to type + method — no attribute needed on the plugin class itself.

**Action required:** Verify the plugin classes being deleted are referenced correctly in V008 `ToolConfig` JSON (TypeName must be the fully-qualified class name that still exists in the assembly).

### Phase 3 — AppAgentToolEngine + Executors ✅ COMPLETE

All 6 executors in `APP.BL/TenantBusiness/AgentToolExecutors/`. Engine in `APP.BL/TenantBusiness/AppAgentToolEngine.cs`.

### Phase 4 — GenericAgent Infrastructure ✅ COMPLETE

All 8 files in `APP.BL/AIAgent/GenericAgent/` + `APP.BL/AIAgent/AiSkill/AppAgentSkillSetBL.cs` + `APP.BL/TenantBusiness/AppAgentMcpServerBL.cs` + `GenericAgentController.cs`.

### Phase 5 — SkillSet Admin Layer ✅ COMPLETE (except mcp-components)

All BL, controller, and React files exist. `AppReact/src/mcp-components/` not yet copied from BC-MCP-Client.

**Remaining:** Copy `client/src/mcp-components/` from BC-MCP-Client into `AppReact/src/`. Import `<MCPAppRenderer>` in `GenericAgentChat.tsx` for `mcp_ui` event handling.

### Phase 6 — Controller Updates + Cleanup ⬜ NOT DONE

**Steps to complete Phase 6:**

1. Migrate `AppBuilderAgentController.RunAgent` to call `GenericAgentBL.RunAsync("app-builder", ...)`:
   - Replace `AppBuilderAgentSessionStore.CreateSession()` → `GenericAgentSessionStore.CreateSession()`
   - Replace `AgentCallbacks` → `GenericAgentCallbacks`
   - Remove call to old `AppBuilderAgentBL.RunAgentAsync`
   - Wire `PollEvents` → `GenericAgentSessionStore.DequeueAll`

2. Migrate `AppReportAgentController.RunAgent` similarly (`"app-report"` skill key).

3. Migrate `DbGenieController.Chat` similarly (`"db-genie"` skill key).

4. Stage and commit the already-deleted old BL files.

5. Remove stale debug SQL from `V008__GenericAgentSchema.sql` lines 665–674.

6. Resolve the `GenericAgent.AppAgentToolRegisterBL` and `GenericAgent.AppAgentMcpServerBL` schema mismatch — either:
   - Update them to read V008 columns (`ToolRegisterId`, `ToolDescription`, etc.)
   - Or delete them if the admin controller exclusively uses `TenantBusiness` versions

---

## 20. Known Deviations from Original Design

| # | Area | Plan said | Actual implementation |
|---|---|---|---|
| 1 | `GenericAgentBL.RunAsync` signature | `(skillKey, userMessage, AgentContext ctx, GenericAgentCallbacks)` | `(skillKey, userMessage, List<JObject> chatHistory, GenericAgentCallbacks, AppClientIdentity?, CancellationToken)` |
| 2 | `GenericAgentSession.cs` | Listed as file to create | Not created; session state lives only in `GenericAgentSessionStore` |
| 3 | `AnthropicChatCompletionService` | Not mentioned | Created — SK has no official Anthropic connector |
| 4 | `AgentStepFilter` | Not mentioned | Created — `IFunctionInvocationFilter` for tool call logging |
| 5 | `GeminiRoleFixHandler` | Not mentioned | Created — inner class in `GenericAgentEngine` fixing SK Gemini bug |
| 6 | `AppAgentSkillSetBL` file location | Plan: `APP.BL/AIAgent/AiSkill/` | Two files: one in `AiSkill/` (engine-facing), one in `GenericAgent/` (admin-facing) |
| 7 | `AppAgentToolRegisterBL` | Plan: one file in `TenantBusiness/` | Two files: `TenantBusiness/` (engine + controller, V008 schema) and `GenericAgent/` (admin UI, different schema — stale) |
| 8 | `AppAgentMcpServerBL` | Plan: `APP.BL/AIAgent/AiSkill/` | Two files: `TenantBusiness/` (engine + controller, V008 schema) and `GenericAgent/` (admin UI, different schema with McpServerKey/Transport/AuthType/AuthValue — stale) |
| 9 | `AppAgentSkillSetBL` methods | `CreateSkillSet` + `UpdateSkillSet` (separate) | `UpsertSkillSet` (merged); no `DeleteSkillSet` soft-delete, only hard DELETE |
| 10 | `GenericAgentController` endpoints | `Run` + `ConfirmGate` | `RunAgent` + `StreamEvents` + `PollEvents` + `ConfirmPlan` + `ConfirmSchema` |
| 11 | LLMProviderHelper | DELETE — promoted to KernelProviderHelper | KEPT — `KernelProviderHelper` delegates to it; still used by existing non-SK paths |
| 12 | Session history storage | Stored server-side in session store | Passed by caller in each request as `List<JObject> Messages` |
| 13 | In-session pruning + summarization | Section 12 Levels 1 + 2 planned | Not yet implemented |
| 14 | InjectMemory flag wiring | Described in §8 BuildSystemPrompt | Not yet implemented in GenericAgentEngine |
| 15 | InjectSchema flag wiring | Described conceptually | Not yet implemented |
| 16 | Tool count in AppAgentToolRegister | Estimated ~31 | Actual seed: ~37 rows |
| 17 | Anthropic NuGet package | Official `Anthropic` v12.42.0 | Not used; direct HTTP in `AnthropicChatCompletionService` |
| 18 | `mcp-components/` copy | Listed as Phase 5 task | Not yet done |
| 19 | V008 migration cleanliness | Production SQL only | Stale debug queries at lines 665-674 |

---

## 21. Critical Notes for Implementation (Next Developer)

These are non-obvious gotchas discovered during implementation:

### 1. `AsKernelFunction()` is broken — use `KernelFunctionFactory.CreateFromMethod`

SK 1.74.0's `ModelContextProtocol.Client.McpClientTool.AsKernelFunction()` throws `MissingMethodException` at runtime due to `Microsoft.Extensions.AI.Abstractions` version mismatch. Do NOT use it. Instead, manually wrap each MCP tool:

```csharp
var f = KernelFunctionFactory.CreateFromMethod(
    async (KernelArguments args, CancellationToken ct) => { ... },
    functionName: toolName,
    description: description,
    parameters: parameters,
    returnParameter: returnParameter);
```

### 2. Anthropic needs a custom `IChatCompletionService` — no official SK connector

SK has no NuGet package for Anthropic. The official `Anthropic` SDK (v12.42.0) conflicts with `Microsoft.Extensions.AI.Abstractions`. Use `AnthropicChatCompletionService` (direct HTTP) already implemented at:
`APP.BL/AIAgent/GenericAgent/AnthropicChatCompletionService.cs`

### 3. Gemini SK connector sends wrong role — `GeminiRoleFixHandler` required

SK Connectors.Google 1.74.0-alpha sends `role:"function"` for tool result turns. Gemini API rejects this. The `GeminiRoleFixHandler : DelegatingHandler` patches every outgoing request body. It is wired in `GenericAgentEngine.BuildKernel`. Do not remove it.

### 4. Empty tool array crashes Gemini — check before enabling FunctionChoiceBehavior.Auto()

Gemini (and some other providers) reject a request with an empty `tools` array. Guard:
```csharp
var hasTools = kernel.Plugins.Any(p => p.Any());
if (hasTools) execSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
```

### 5. MCP `ServerType` must be `'streamable-http'` — SSE not supported

`HttpClientTransport` uses `HttpTransportMode.StreamableHttp`. The `stdio` type uses `StdioClientTransport`. Old SSE transport is not available in `ModelContextProtocol` 1.2.0. The V008 migration and BL both use `'streamable-http'` as the value.

### 6. `LLMProviderHelper` is KEPT — do not delete

`KernelProviderHelper` (in GenericAgent) calls `LLMProviderHelper.GetConfiguredProvider()` and `GetConfiguredApiKey()`. `LLMProviderHelper` is also used by old DB Genie non-SK paths. Do not delete it.

### 7. Each provider has its own API key in tenant settings

`AIConfigSettingBL.GetApiKey()` routes to the correct per-provider key based on the current `AIConfigProvider` setting. There is no shared key field. If a tenant switches from Gemini to Anthropic, they must configure `AIConfigAnthropicApiKey` separately.

### 8. Admin BL files in `GenericAgent/` namespace have stale schema

`APP.BL/AIAgent/GenericAgent/AppAgentToolRegisterBL.cs` and `APP.BL/AIAgent/GenericAgent/AppAgentMcpServerBL.cs` read column names that do not exist in V008 (`Id` instead of `ToolRegisterId`, `Description` instead of `ToolDescription`, `McpServerKey` instead of `McpServerId`, `Transport` instead of `ServerType`). These files will throw `IndexOutOfRangeException` at runtime if called. The engine and AgentSkillSetController both use the `TenantBusiness` namespace versions (which correctly match V008). The GenericAgent namespace files need to be either corrected or removed.

### 9. Plugin class names in ToolConfig TypeName must still exist in the assembly

V008 seeds tool rows like `{"TypeName":"APP.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin","MethodName":"GetTableSchema"}`. If `SchemaBuilderPlugin.cs` is deleted from APP.BL (Phase 2 cleanup), the `BuiltInToolExecutor` will fail to reflect the type. The plugin classes must remain in the assembly even after their `[AgentFunction]` attributes are removed.

### 10. DB history: caller owns it — server is stateless per request

`GenericAgentEngine` receives chat history as `List<JObject>` per request. The server does not store or retrieve history — the React client accumulates messages in state and passes them back on every request. This is different from the original plan which described server-side session history in `GenericAgentSession`. Do not implement server-side history persistence.

### 11. SanitizeName is required for tool function names

Gemini requires function names matching `^[a-zA-Z_][a-zA-Z0-9_]*$`. `SanitizeName()` in `GenericAgentEngine` replaces non-alphanumeric chars with `_` and prepends `_` if the name starts with a digit. Apply to both registered tool names and MCP tool names.

---

## 22. Verification Checklist

| # | Test | Expected |
|---|---|---|
| 0 | Run V008 + V009 migrations | 4 SkillSet rows (each with non-null SystemPrompt); ~37 tool rows; MCP table exists; 7 AppTenantSetting AIConfig rows |
| 1 | `dotnet build AppAI.Core.sln` | Zero errors |
| 2 | POST `/webapi/GenericAgent/RunAgent` with `skillKey:"db-genie"`, `userMessage:"list tables"` | Returns `{ IsStarted: true, SessionId: "..." }` |
| 3 | GET `/webapi/GenericAgent/StreamEvents?sessionId=<id>` | SSE events stream: `step`, `token`, `done` |
| 4 | GET `/webapi/AgentSkillSet/GetAllSkillSets` | Returns 4 skill sets |
| 5 | GET `/webapi/AgentSkillSet/GetToolsBySkillKey?skillKey=app-builder` | Returns ~34 tool rows |
| 6 | Admin UI → Agent Personas → click [▶ Run] on DB Genie | Chat panel opens; agent responds to SQL question |
| 7 | Admin UI → create new SkillSet via wizard | Row in AppAgentSkillSet; CapabilityFlags matches wizard checkboxes |
| 8 | Admin UI → add SqlQuery tool to new SkillSet; run agent | Agent calls the SQL tool; result returned |
| 9 | Register MCP server in AppAgentMcpServer (ServerType='streamable-http'); run agent | MCP tools auto-discovered; agent calls them |
| 10 | DynamicCSharp tool: LLM passes code with `System.IO` | Executor rejects — not in whitelist |
| 11 | DynamicCSharp tool: LLM passes valid LINQ | Compiles and returns correct result |
| 12 | POST `/webapi/AppBuilderAgent/RunAgent` (after Phase 6) | Routes through GenericAgentBL; same events as test #3 |
| 13 | Trigger `propose_plan` gate → POST `/webapi/GenericAgent/ConfirmPlan` | Agent resumes |
| 14 | Trigger `propose_schema` gate → POST `/webapi/GenericAgent/ConfirmSchema` | Agent resumes |

---

## 23. Risk Register

| Risk | Mitigation |
|---|---|
| `GenericAgent.AppAgentToolRegisterBL` schema mismatch causes IndexOutOfRangeException | Use `TenantBusiness` version; fix or remove GenericAgent version (§19 Phase 6) |
| `GenericAgent.AppAgentMcpServerBL` schema mismatch | Same — use TenantBusiness version |
| Plugin class deleted but still referenced in ToolConfig TypeName | Keep plugin .cs files in APP.BL even after attribute cleanup; or update TypeName in DB |
| `AsKernelFunction()` used anywhere | Never call it — always use `KernelFunctionFactory.CreateFromMethod` |
| Gemini rejects empty tools array | Guard with `kernel.Plugins.Any(p => p.Any())` before setting FunctionChoiceBehavior.Auto() |
| Anthropic API key missing from tenant settings | AIConfigSettingBL returns empty string; Anthropic service will throw 401; surface as OnError event |
| MCP server unavailable at session start | Try/catch per server in engine; log + skip; agent continues with remaining tools |
| In-session context overflow (no pruning yet) | Add Level 1 pruning to GenericAgentEngine before next long-session use |
| V008 debug SQL at end of migration file | Remove lines 665-674 before next environment deployment |
| Stale GeminiRoleFixHandler — fixed in future SK release | Monitor SK release notes; remove handler when SK Connectors.Google is fixed |
| `mcp-components/` not copied — MCPAppRenderer unavailable | Copy from BC-MCP-Client before enabling UI rendering in GenericAgentChat |
