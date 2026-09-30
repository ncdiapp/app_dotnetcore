# Generic AI Agent Platform — Technical Architecture

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-02 (first version) · **Revised:** 2026-09-30  
**Audience:** Senior developers, backend engineers  

**Revision 2026-09-30 — what changed since the first version:** Tool Libraries and subscriptions (§3.5), library-owned MCP servers with auth headers and Test/Sync (§3.3, §8), per-agent tool exclusions and the unified tool catalog (§3.6, §12), server-side conversation persistence (§9), orchestrator/child agents and shared context (§3.7), MCP plugin-name rule (§8; see `ToolNameConvention-ProviderLimits.md`), and a prioritised improvement backlog (§13).

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  React UI (AppReact/src/components/aiskill/)                        │
│  AgentSkillSetManagement.tsx    GenericAgentChat.tsx                │
│  AgentLibraryTab / AgentLibraryRow / AgentMcpServerTab              │
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
│  1. Load SkillSet (AppAgentSkillSetBL); resolve chat session key    │
│  2. Inject file catalog into the system prompt (agent-files)        │
│  3. Build SK Kernel (AIConfigSettingBL → provider connector)        │
│  4. Register AgentStepFilter + GenericAgentPruneFilter              │
│  5. Tools: agent-owned + subscribed-library tools, minus the        │
│     agent's exclusions → KernelFunctions (plugin "tools")           │
│  6. Interactive mode: add ask_user (plugin "hitl")                  │
│  7. Subscribed-library MCP servers → McpClient → plugin             │
│     "mcp<name>" (auth headers applied, excluded tools skipped)      │
│  8. Run ChatCompletionAgent.InvokeStreamingAsync                    │
│  9. Fire OnToken / OnStep / OnDone / OnError callbacks              │
└────────┬───────────────────────────────────┬────────────────────────┘
         │                                   │
         ▼                                   ▼
┌─────────────────────┐         ┌────────────────────────────────────┐
│  Semantic Kernel     │         │  External Systems                  │
│  ChatCompletionAgent │         │  ┌─────────────────────────────┐  │
│  + LLM provider     │         │  │ Tool rows (AppAgentToolReg.  │  │
│  (Anthropic /       │         │  │  / AppAgentLibraryTool)      │  │
│   Gemini / OpenAI)  │         │  │  BuiltIn  → APP.BL plugins  │  │
└─────────────────────┘         │  │  SqlQuery → tenant DB       │  │
                                │  │  HttpRest → external API    │  │
                                │  │  DynamicCSharp → Roslyn     │  │
                                │  │  ExternalDll / PowerShell   │  │
                                │  └─────────────────────────────┘  │
                                │  ┌─────────────────────────────┐  │
                                │  │ MCP Servers (library-owned) │  │
                                │  │  streamable-http transport  │  │
                                │  │  bearer / custom headers    │  │
                                │  └─────────────────────────────┘  │
                                └────────────────────────────────────┘
```

Existing agent controllers (`AppBuilderAgentController`, `AppReportAgentController`, `DbGenieController`) continue to expose their original routes. Their bodies resolve the authenticated tenant execution context and forward it to `GenericAgentBL.RunAsync` with a hardcoded `skillKey`. The generic `GenericAgentController` additionally accepts any `skillKey` and is the entry point for the admin test UI.

---

## 2. Component Responsibilities

| Class / File | Location | Purpose | Key Methods |
|---|---|---|---|
| `GenericAgentBL` | `APP.BL/AIAgent/GenericAgent/` | Public entry point. Validates inputs, captures/accepts tenant execution context, delegates to engine. | `RunAsync(...)` |
| `GenericAgentEngine` | `APP.BL/AIAgent/GenericAgent/` | SK agentic loop. Loads tenant-scoped config, builds kernel, runs streaming. | `RunAsync(...)`, `BuildKernel()`, `WrapRegisteredTool()`, `CreateMcpPluginAsync()`, `McpPluginName()`, `BuildChatHistoryAsync()` |
| `McpConnectionHelper` | `APP.BL/AIAgent/GenericAgent/` | Builds MCP transport options (bearer/headers resolved from env vars), **Test connection**, and **list tools for the catalog**. | `BuildTransportOptions()`, `TestConnectionAsync()`, `ListCatalogToolsAsync()` |
| `AIConfigSettingBL` | `APP.BL/AIAgent/GenericAgent/` | Reads LLM provider/key/model from the selected tenant's `AppTenantSetting`. No appsettings.json fallback. | `GetProvider/GetApiKey/GetModel(executionContext)` |
| `KernelProviderHelper` | `APP.BL/AIAgent/GenericAgent/` | Optional facade over `AIConfigSettingBL` with an explicit `AgentExecutionContext`. | same |
| `AppAgentSkillSetBL` | `APP.BL/AIAgent/GenericAgent/` | Tenant-scoped CRUD over `AppAgentSkillSet`. Parameterized queries, no ORM. | `GetAll`, `GetByKey`, `Upsert`, `Delete` |
| `AppAgentToolRegisterBL` | `APP.BL/TenantBusiness/` | Agent-private tools; `GetBySkillKeyWithLibraries` merges private + subscribed-library tools, applies the agent's exclusions, de-duplicates by tool name (agent wins). | `GetBySkillKeyWithLibraries()` |
| `AppAgentLibraryToolBL` / `AppAgentToolLibraryBL` | `APP.BL/TenantBusiness/` | Domains, libraries, library-owned tools, subscriptions. | `GetByLibraryKey()`, `SetSubscriptions()` |
| `AppAgentMcpServerBL` | `APP.BL/TenantBusiness/` | MCP server registry. A server belongs to a **library**; `GetBySkillKeyWithLibraries` returns servers of the libraries an agent subscribes to. | `Upsert()`, `LibraryExists()` |
| `AppAgentToolExclusionBL` | `APP.BL/TenantBusiness/` | Per-agent "do not use" list (library tools and MCP tools). Failure to read is logged and treated as "nothing excluded". | `GetBySkillKey()`, `ReplaceForSkill()` |
| `AppAgentToolCatalogBL` + `ToolCatalogHelpers` | `APP.BL/TenantBusiness/` | Unified searchable tool catalog, risk guess (read/write/delete), retriever seam for RAG. | `SyncLibraryTools()`, `ReplaceMcpServerTools()`, `GetAll()` |
| `AppAgentToolEngine` | `APP.BL/TenantBusiness/` | Strategy dispatcher: routes tool calls by `ToolType`. | `Dispatch(...)`, `BuildInvokersAsync()` |
| `*ToolExecutor` (6) | `APP.BL/TenantBusiness/AgentToolExecutors/` | BuiltIn, ExternalDll, SqlQuery, PowerShell, HttpRest, DynamicCSharp. | `ExecuteAsync(...)` |
| `AgentAskUserPlugin` / `AgentHitlBridge` | `APP.BL/AIAgent/GenericAgent/` | Structured ask-the-user tool for Interactive agents (plugin `hitl`). | `AskUser(...)` |
| `AgentCallPlugin`, `AppAgentChildMappingBL`, `AppAgentSharedContextBL` | `APP.BL/AIAgent/GenericAgent/` | Orchestrator → child agent calls and the shared-context blackboard. | `call_agent`, `read/write_shared_context` |
| `AppGenericAgentSessionBL` | `APP.BL/AIAgent/GenericAgent/` | Server-side conversation persistence per user and agent (see §9). | `MakeFixedKey()` |
| `GenericAgentFileBL` and friends | `APP.BL/AIAgent/GenericAgent/` | Per-chat workspace files and the catalog injected into the prompt. | `EnsureRoot()` |
| `GenericAgentCallbacks` / `GenericAgentSessionStore` | `APP.BL/AIAgent/GenericAgent/` | Event delegates and in-memory session queue + gate state (single-node). | `Enqueue()`, `WaitForEventAsync()`, `ConfirmPlan()` |
| `GenericAgentController`, `AgentSkillSetController` | `AppAI.Web/Controllers/` | HTTP layer: run/stream/gates, and admin CRUD for agents, libraries, MCP servers, exclusions, catalog, AI design. | see controllers |
| `AgentSkillSetManagement.tsx`, `AgentLibraryTab.tsx`, `AgentLibraryRow.tsx`, `AgentMcpServerTab.tsx`, `AiRecommendedTools.tsx` | `AppReact/src/components/aiskill/` | Admin UI: agent editor, library browser (Domains › Libraries › Tools / MCP), per-tool checkboxes, AI design review. | — |
| `GenericAgentChat.tsx` | same | Reusable streaming chat component. | — |
| `agentSkillSetSvc.ts`, `genericAgentSvc.ts` | `AppReact/src/webapi/` | Admin API client and run/stream client. | — |

---

## 3. Database Schema

All tables below live in the **tenant database**. Migrations are applied by `AppTenantMigrationRunnerBL` (see §13 for the operational caveats).

### 3.1 AppAgentSkillSet — Agent Persona Registry

Created by V008; later columns added by V011 (`MaxIterations`, default 40), V016 (`ExecutionMode`: `Interactive` | `Deterministic`) and V018 (`AgentUi`).

| Column | Type | Description |
|---|---|---|
| `SkillKey` | NVARCHAR(100) PK | Unique identifier used in every API call |
| `DisplayName`, `Description` | NVARCHAR | Shown in the admin UI |
| `SystemPrompt` | NVARCHAR(MAX) | Complete instruction text. Prompt history kept in `AppAgentSkillSetHistory` (V019, last 10) |
| `CapabilityFlags` | INT | Bitmask of behaviours (§4) |
| `IsActive`, `SortOrder`, `Version` | | Soft-delete, ordering, prompt version |
| `MaxHistoryTokens` / `SummarizeThreshold` / `RecentWindowSize` | INT | History pruning and summarisation |
| `MaxToolResultChars` | INT | Hard cap on one tool result |
| `MaxIterations` | INT | Max tool-call rounds before the run stops |
| `ExecutionMode` | VARCHAR(20) | `Interactive` (gates wait for the user, `ask_user` available) or `Deterministic` (gates auto-approve, no `ask_user`) |
| `AgentUi` | INT | Which chat shell runs the agent (GenericChat, ConfigurationAndIntegration, DbManagement, ImageAndFileProcess) |

**Seeded rows (V008):** `app-builder` (31), `app-report` (3), `db-genie` (35), `data-integration` (65), plus inactive templates from V019.

### 3.2 AppAgentToolRegister — Agent-private Tools

| Column | Type | Description |
|---|---|---|
| `ToolRegisterId` | INT IDENTITY PK | |
| `SkillKey` | NVARCHAR(100) | FK to `AppAgentSkillSet` |
| `ToolName` | NVARCHAR(200) | LLM-facing function name (unique within the agent) |
| `ToolDescription`, `ParameterSchemaJson` | NVARCHAR(MAX) | What the tool does; JSON Schema of its parameters |
| `ToolType` | NVARCHAR(50) | BuiltIn, ExternalDll, SqlQuery, PowerShell, HttpRest, DynamicCSharp |
| `ToolConfig` | NVARCHAR(MAX) | JSON whose shape depends on `ToolType` |
| `IsActive` | BIT | Inactive tools are not loaded |

Most shared tools now live in **libraries** (§3.5); private tools are for one-off needs.

### 3.3 AppAgentMcpServer — MCP Server Registry (library-owned)

An MCP server belongs to a **Tool Library**. An agent gets a server's tools by subscribing to that library (V037 moved earlier agent-owned rows into generated `<agent>-mcp` libraries and subscribed the agent).

| Column | Type | Description |
|---|---|---|
| `McpServerId` | INT IDENTITY PK | |
| `SkillKey` | NVARCHAR(100) | **Holds the LibraryKey** (name kept from the original schema) |
| `ServerName` | NVARCHAR(200) | Display name; the SK plugin name is derived from it (§8). Prefer short names such as `plm`. |
| `ServerType` | NVARCHAR(50) | `streamable-http` (supported). `stdio` rows are skipped. |
| `ServerUrl`, `Command` | NVARCHAR(500) | HTTP endpoint; stdio command |
| `IsActive` | BIT | Inactive servers are skipped |
| `BearerTokenEnvVar` | NVARCHAR(200) | **Name** of an environment variable; its value is sent as `Authorization: Bearer <value>` (V036). Only names starting with `MCP_` are allowed. |
| `Headers` | NVARCHAR(MAX) | JSON `{"Header":"value"}` — static headers, **stored encrypted** (`AES:` prefix, same key as tenant connection strings). The API returns every value masked (`********`); a masked value sent back on save keeps the stored one. Legacy plain-text rows still work and are encrypted on their next save. |
| `HeadersFromEnv` | NVARCHAR(MAX) | JSON `{"Header":"MCP_ENV_VAR_NAME"}` — values read from the environment at connect time; names must start with `MCP_` |

### 3.4 Tenant Database Ownership and Routing

The App-netore tenancy model uses one tenant database per company. `AppMasterDB` owns identity, sessions, company registration and `AppDataSourceRegister`; tenant databases own app definitions, security groups, business data, tenant settings and all agent configuration above.

The controller resolves the authenticated session to a company and captures an immutable `AgentExecutionContext` before starting background work (user ID, company ID, login type, tenant data-source identity, authorization info). The engine and every tool executor use it for tenant routing; no LLM argument or request-body field may select a database, connection string or company.

Tenant database access must use `AppTenantAdapterBL.GetTenantAdapter()` and the existing data-source routing rules. SysAdmin has no implicit tenant context; a SysAdmin agent run must explicitly select a company.

### 3.5 Tool Libraries (V018, V020, V025)

| Table | Purpose |
|---|---|
| `AppAgentToolDomain` | Grouping of libraries (`Platform Built-in`, `MCP Servers`, `Custom SQL Queries`, `External REST APIs`, …) |
| `AppAgentToolLibrary` | A named set of tools (`LibraryKey`, `DomainKey`, `IsActive`, …) |
| `AppAgentLibraryTool` | Tools owned by a library (same shape as `AppAgentToolRegister`, incl. `ToolType`/`ToolConfig`/`IsActive`) |
| `AppAgentLibrarySubscription` | `(SkillKey, LibraryKey)` — the agent uses everything active in the library |

Runtime rule: an agent receives a library tool when it subscribes to the library **and** the tool's `IsActive` is on **and** the agent has not excluded it. The library's own `IsActive` flag is **not** enforced at run time (same for MCP servers).

### 3.6 Per-agent Tool Exclusions and the Tool Catalog (V038, V039)

| Table | Purpose |
|---|---|
| `AppAgentToolExclusion` | `(SkillKey, LibraryKey, ToolName)` — tools an agent does **not** use from libraries it subscribes to. Default is allow-all; new tools on a library or MCP server are available automatically. |
| `AppAgentToolCatalog` | One row per tool from every source: `Source`, `LibraryKey`, `ToolName`, `Description`, `InputSummary`, `Risk` (`read`/`write`/`delete`), `McpServerId`, `Embedding` (reserved for RAG), `SyncedAt`. Library tools sync automatically when AI design is generated; MCP tools via **Sync tools**. |

### 3.7 Conversations, Orchestration and Shared State

| Table | Purpose |
|---|---|
| `AppGenericAgentSession` (V021, V034, V035) | Persisted conversation per user and agent or chat: `SessionKey`, `SkillKey`, `UserId`, `MessagesJson`, `DisplayTitle`, `UpdatedAt` |
| `AppAgentChildMapping` (V036) | Orchestrator → child agents (many-to-many, no self-reference) |
| `AppAgentSharedContext` (V022) | Blackboard `(ScopeId = WorkflowId, ContextKey, DataJson)` for multi-agent workflows |
| `AppAgentSkillSetHistory` (V019) | Last 10 system prompt versions per agent |
| `CursorCloudAgentSession` (V023) | Separate external-backend path; not part of the SK loop |

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
| `StreamTokens` | 1 | Engine fires `OnToken`; controller SSE-streams each token | All built-in agents |
| `MultiTurn` | 2 | Prior messages are converted to `ChatHistory` | All built-in agents |
| `PlanGate` | 4 | `propose_plan` pauses until `ConfirmPlan` | `app-builder` |
| `SchemaGate` | 8 | `propose_schema` pauses until `ConfirmSchema` | `app-builder` |
| `InjectMemory` | 16 | Relevant memory appended to the system prompt | `app-builder` |
| `InjectSchema` | 32 | DB schema summary injected into the system prompt | `db-genie` |
| `ExternalBackend` | 64 | SK loop skipped; delegated to the Cursor cloud backend | `data-integration` |

Composite examples: `31` = App Builder, `35` = DB Genie, `65` = Data Integration.

---

## 5. ToolType Strategy Pattern

`AppAgentToolEngine.Dispatch()` routes by `ToolType`. `BuiltIn` also receives the per-run **instance pool** so stateful plugin instances survive across tool calls in one run.

| ToolType | ToolConfig shape | Mechanism | Security notes |
|---|---|---|---|
| `BuiltIn` | `{"TypeName":"Namespace.Class","MethodName":"Method"}` | Reflection; method takes `(IReadOnlyDictionary<string,string> args, AgentToolContext ctx, CancellationToken ct)` | In-process, full trust; developer-only |
| `ExternalDll` | `{"AssemblyName":"Tenant.dll","TypeName":"...","MethodName":"Run"}` | `Assembly.LoadFrom`; type implements `IAgentTool` | Full trust; version-check the assembly |
| `SqlQuery` | `{"SqlBody":"SELECT ... WHERE Col=@param","ReturnType":"json"}` | Parameterized `SqlCommand` against the tenant DB | Parameterized only; SELECT preferred |
| `PowerShell` | `{"ScriptPath":"scripts/export.ps1"}` | Runs a script file, returns stdout | Super-admin only; restrict directory |
| `HttpRest` | `{"Url":"https://.../{argName}","Method":"GET","TokenStoreKey":"key"}` | `HttpClient` with placeholder substitution; token from key store | URL validated; no arbitrary redirect |
| `DynamicCSharp` | `{"ScriptBody":"...","AllowedNamespaces":["System"],"TimeoutSeconds":10}` | Roslyn script with namespace whitelist | Allowed: System, Linq, Collections.Generic, Text, Text.Json. Blocked: IO, Net, Reflection, Diagnostics. Every execution logged. |

All results are truncated to `MaxToolResultChars` before entering chat history.

---

## 6. LLM Provider Configuration Chain

`AIConfigSettingBL` is the single source of truth. It reads only from the selected tenant's `AppTenantSetting` rows (V009, V017). There is no `appsettings.json` fallback.

```
AIConfigSettingBL.GetProvider(executionContext)
  → tenant setting AIConfigProvider  → default "Gemini"
AIConfigSettingBL.GetApiKey / GetModel
  → per provider: OpenAI (gpt-4o), Anthropic (claude-3-5-sonnet-20241022), Gemini (gemini-2.0-flash)
```

Requests without tenant identity cannot use tenant settings; the admin test path must select a company first.

| Provider | SK registration | Special handling |
|---|---|---|
| `Anthropic` | `AnthropicChatCompletionService(model, apiKey)` | Custom wrapper; sends the bare function name (plugin dropped) |
| `Gemini` | `AddGoogleAIGeminiChatCompletion(...)` with `GeminiRoleFixHandler` | Tool-result role patched `function` → `user` (SK Google 1.74.0-alpha bug). Tool names are `plugin_function` split at the first `_` — see §8. |
| `OpenAI` | `AddOpenAIChatCompletion(...)` | Standard; names are `plugin-function` |

---

## 7. Semantic Kernel Integration

- The kernel is built **per request** and never cached.
- `FunctionChoiceBehavior.Auto()` is set only when at least one plugin function exists (Gemini rejects an empty tools array).
- `AgentStepFilter` fires `OnStep` before/after each tool call; `GenericAgentPruneFilter` enforces `MaxIterations` and `MaxHistoryTokens`.
- `ChatCompletionAgent` uses `skillSet.SystemPrompt` (plus the injected file catalog) as instructions.
- Streaming uses `agent.InvokeStreamingAsync(thread, …)`; each chunk is fired as `OnToken`.
- Plugin layout seen by the model: `tools` (private + library tools), `hitl` (`ask_user`, Interactive only), `mcp<name>` (one per MCP server), plus feature plugins (files, scripts, call_agent, shared context) registered as library tools.

---

## 8. MCP Integration

The engine uses `ModelContextProtocol` 1.2.0. `AsKernelFunction()` is **not** used (it throws `MissingMethodException` because of a `Microsoft.Extensions.AI` version conflict); tools are wrapped manually.

**Connection flow (per MCP server of a subscribed library):**

```csharp
// 1. Transport: URL + bearer / custom headers (env vars resolved here)
var options   = McpConnectionHelper.BuildTransportOptions(server);   // AdditionalHeaders
var transport = new HttpClientTransport(options, McpHttpClient, NullLoggerFactory.Instance, ownsHttpClient: false);

// 2. Connect, list tools, drop the agent's excluded tools
var client = await McpClient.CreateAsync(transport, cancellationToken: ct);
var tools  = (await client.ListToolsAsync(cancellationToken: ct))
                 .Where(t => !excludedForThisLibrary.Contains(t.Name));

// 3. Wrap each as a KernelFunction (CreateFromMethod) → plugin
kernel.Plugins.Add(KernelPluginFactory.CreateFromFunctions(McpPluginName(server.ServerName), functions));
```

**Plugin naming rule.** Gemini's connector splits `plugin_function` at the first underscore, so a plugin name containing `_` makes every tool unresolvable (`Requested function could not be found`). `McpPluginName` returns `"mcp"` + the letters/digits of the server name, cut to 16 characters. Provider limit on the combined name is 64 characters. Full rules and provider table: `ToolNameConvention-ProviderLimits.md`.

**Headers.** `BuildHeaders` merges static `Headers` (decrypted), then `HeadersFromEnv` (env var values), then `BearerTokenEnvVar` as `Authorization`. Missing or disallowed env vars are reported as warnings by Test connection.

**Security policy (`McpSecurityPolicy`).** (1) Only environment variables named `MCP_*` can be read, so a registration cannot pull unrelated server secrets into a header; each resolve logs the variable *name* (never the value). (2) `ServerUrl` must be http(s) without embedded credentials, and must not resolve to link-local (including the cloud metadata address `169.254.169.254`), unspecified, broadcast or multicast addresses. Loopback and private networks stay allowed because PLM/ERP MCP servers usually run there; a per-tenant host allow-list is the next step. (3) HTTP redirects are not followed. The policy is enforced on save, Test connection, Sync tools and again before every agent connect.

**Operations.** A single static `HttpClient` (`Timeout = Infinite`) serves all MCP connections; each tool call has a 30 s linked timeout. A server that fails to connect is skipped with a `log.Warn` and the run continues. MCP clients are disposed in the `finally` of `RunAsync`.

**Admin actions.** *Test Connection* (connect and list tool names) and *Sync tools* (store name, description, parameter names and risk hint in `AppAgentToolCatalog`; requires a saved server).

---

## 9. Session and Context Management

**Run lifecycle:**

1. `RunAgent` validates the caller, resolves the company and creates an immutable `AgentExecutionContext`.
2. `GenericAgentSessionStore.CreateSession()` returns a GUID `sessionId` bound to user, company and context.
3. `GenericAgentCallbacks` are wired to enqueue events for that session.
4. Background execution starts with the captured context (no dependence on thread-static `ServerContext`). The HTTP response returns `{ IsStarted: true, SessionId }` immediately.
5. The client opens `GET /StreamEvents?sessionId=…` (SSE). The controller verifies session and company ownership, then loops `WaitForEventAsync` (≤30 s), flushing events.
6. On `OnDone`/`OnError` the client closes the connection; the session is cleaned up after a retention period.

**Conversation persistence.** Conversations are stored server-side in `AppGenericAgentSession` (key `{SkillKey}:{UserId}` by default, or an explicit chat session key), so they survive refresh and tab switches; the auto-title lives in `DisplayTitle`. The client still sends the `Messages` array for the current turn, and `BuildChatHistoryAsync` converts it (with summarisation above `SummarizeThreshold`).

**Plan gate flow:** `propose_plan` → `OnPlanReady` → controller registers a gate ID, enqueues a `plan` event → background task awaits (≤10 min, then auto-reject) → `ConfirmPlan` (verifies ownership, gate freshness, one-time use) resolves it. The schema gate follows the same pattern. In `Deterministic` mode both gates auto-approve.

**Tool result truncation:** every result is cut to `MaxToolResultChars` before it enters history.

---

## 10. NuGet Packages Required

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.SemanticKernel` | 1.74.0 | Agentic loop, kernel, plugins |
| `Microsoft.SemanticKernel.Agents.Core` | 1.74.0 | `ChatCompletionAgent`, `ChatHistoryAgentThread` |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | 1.74.0 | OpenAI / Azure OpenAI |
| `Microsoft.SemanticKernel.Connectors.Google` | 1.74.0-alpha | Gemini (needs `GeminiRoleFixHandler`) |
| `ModelContextProtocol` | 1.2.0 | MCP client. Do NOT use `AsKernelFunction()`. |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | latest stable | `DynamicCSharp` Roslyn sandbox |
| `Anthropic` | 12.42.0 | Official Anthropic SDK |

---

## 11. Key Design Decisions

### Why not extend AppAISkill?
`AppAISkill` is a flat prompt library with no notion of tools, MCP servers or capability flags. A dedicated `AppAgentSkillSet` avoids invasive schema changes to a stable production feature.

### Why no base-class / addon concept between agents?
The duplication was infrastructure (the agentic loop), not persona configuration. One flat `AppAgentSkillSet` row with a full `SystemPrompt` is simpler to debug and edit. Reuse of *tools* is handled by libraries and subscriptions; reuse of *agents* by orchestrator/child mapping.

### Why are MCP servers owned by libraries, not agents? (2026-09)
One server registered once can serve many agents, and libraries are already the unit of sharing for tools. Agent-owned rows created duplication and an ambiguous "Agent Code" field. Trade-off: the column is still named `SkillKey` but holds a library key.

### Why exclusions (deny-list) instead of an allow-list?
Subscribing stays simple and new tools appear automatically, which suits trusted read-only libraries. The cost is that an agent can silently gain new tools, including write tools. An allow-list mode is a backlog item (§13).

### Why fire-and-forget with SSE?
SSE is HTTP/1.1 compatible, works through most proxies and needs no load-balancer session handling. A polling fallback (`PollEvents`) exists.

### Why manual KernelFunction wrapping for MCP tools?
`AsKernelFunction()` breaks at runtime on an `Microsoft.Extensions.AI.Abstractions` version conflict. Manual wrapping is a one-time cost, proven in the BC-MCP-Client codebase.

### Why persist conversations server-side now?
The first design kept history client-side to stay stateless. Users need conversations to survive refresh and device switches, and orchestrator flows need history on the server, so `AppGenericAgentSession` was added (V021). The run itself remains stateless apart from active gate objects.

### Why the GeminiRoleFixHandler?
SK's Google connector 1.74.0-alpha sends tool-result turns with role `function`; Gemini accepts only `user`. The handler patches outgoing JSON. Remove it when the connector is fixed.

---

## 12. Tool Selection and AI Agent Design

**Goal:** a domain expert describes a use case (for example "publish available PLM styles to the ERP") and the platform picks the right tools from thousands.

```
use case text
   │
   ▼  SyncLibraryTools (only changed rows)        ┌──────────────────────────┐
AppAgentToolCatalog  ◄───────────────────────────┤ Sync tools (per MCP srv) │
   │  IToolCatalogRetriever.Retrieve(useCase, 300)└──────────────────────────┘
   ▼   (all tools if ≤300, otherwise keyword shortlist; embeddings later)
LLM: plan Workflow → name exact tools → JSON { SystemPrompt, RecommendedTools, … }
   │
   ▼  server validation: drop any tool not in the catalog; drop ask_user; de-duplicate
Review dialog: tools grouped by library with risk badge and reason (checkboxes)
   │
   ▼  Use this
subscribe to the libraries of ticked tools  +  exclude that library's other tools
```

Rules given to the model: copy names exactly, choose the fewest tools, put a confirmation step (`ask_user`, always available at runtime, or `propose_plan`) before any `[write]`/`[delete]` tool, and never recommend `ask_user` as a registered tool.

`risk` is a **hint only** (server annotations when sent, otherwise verbs in the tool name); it gates nothing at run time.

**RAG path (not built):** implement `IToolCatalogRetriever` with an embedding lookup over `AppAgentToolCatalog.Embedding` and assign it to `ToolCatalogRetrieval.Current`. Nothing else changes.

---

## 13. Improvement Backlog

Priority: **P1** fix soon (security or correctness), **P2** next, **P3** when scale demands.

| # | Pri | Area | Problem | Recommendation |
|---|---|---|---|---|
| 1 | P1 | Security | `HeadersFromEnv` and `BearerTokenEnvVar` read **any** environment variable of the server process. A tenant admin could name a sensitive variable and point the server URL at a host they control, and the value is sent there. The same applies to *Test connection* and *Sync tools*. | **Done 2026-09-30:** only `MCP_*` variables readable; names logged on every resolve (§8). A tenant-scoped secret store is still an option. |
| 2 | P1 | Security | `ServerUrl` is arbitrary (including internal addresses), so MCP connect/test/sync can reach internal hosts. | **Done 2026-09-30 (baseline):** scheme, credentials, link-local/metadata/multicast blocking and no redirects (§8). Still open: a per-tenant host allow-list. |
| 3 | P1 | Security | Static `Headers` are stored in plain text in the tenant DB (the bearer/`IntergrationAccessToken` value is visible in the UI). | **Done 2026-09-30:** encrypted at rest, masked in API and UI (§3.3). Set a strong `AppConnectionStringEncryptionKey` in production. |
| 4 | P1 | Ops | Migrations are **not applied at startup**; they run only for new tenants or via `POST RunMigrations`. After a deploy, screens fail with "Invalid column name" until someone runs it. `RunMigrationsOnAllTenants` also swallows the failure reason (returns `-1`). | **Done 2026-09-30:** `Migrations:RunOnStartup` (on in Development), startup warning per tenant with pending scripts, `GET TenantProvisioning/PendingMigrations`, failures now logged (`AppAI.Web/Migrations/README.md`). |
| 5 | P1 | Ops | Migration numbers collide (two `V036`, two `V038`). They work only because the runner tracks the full filename; ordering between same-number files is alphabetical. | **Done 2026-09-30:** naming rules in `Migrations/README.md`, `check-duplicate-versions.ps1`, startup warning for duplicates. Wire the script into CI. |
| 6 | P1 | Code rule | Silent `catch { }` blocks remain in the engine (for example around file-source seeding and catalog injection), against the handbook rule. | **Done 2026-09-30 for `APP.BL/AIAgent/GenericAgent`** via `SwallowLog.Write`. Other agents (App Builder, Report, Cursor, DbGenie) and `TenantBusiness` still have silent catches. |
| 7 | P2 | Performance | Every run reconnects to every subscribed MCP server and calls `ListTools` before the LLM is invoked. A slow or down server delays each message. | Cache the tool list per server (short TTL, refresh on Sync), connect lazily on first call, and time-box connection (per-server timeout). |
| 8 | P2 | Safety | Deny-list exclusions mean new tools on a server or library are silently available to agents, including write tools. | Add an allow-list mode per subscription (default for AI-designed agents), and require `Risk` review before a `write`/`delete` tool becomes available. |
| 9 | P2 | Correctness | Exclusions are matched by tool **name**; a renamed MCP tool returns as available. Two subscribed libraries with the same tool name keep only the first, with no indication. | Show "shadowed/duplicate" in the library row; warn on name collisions at subscribe time. |
| 10 | P2 | Correctness | Tool name collisions after `SanitizeName`, and plugin names truncated to 16 characters, can collide across servers (the second plugin is skipped with a warning). | Detect and report collisions in Test/Sync; consider a stable short id in the plugin name. |
| 11 | P2 | Maintainability | `GenericAgentEngine.cs` is far above the 300-line file limit and mixes kernel building, tool wrapping, MCP, history and filters. | Split: `McpPluginFactory`, `ToolWrapper`, `ChatHistoryBuilder`, `KernelFactory`. |
| 12 | P2 | Observability | No per-run record of which tools were offered, called and truncated, or of token use. | Persist a run summary (tools offered/used, iterations, truncations, duration) for debugging and cost review. |
| 13 | P2 | Testing | No automated tests cover tool selection, exclusions, header resolution or plugin naming. | xUnit tests for `McpPluginName`, `BuildHeaders`, exclusion filtering, risk guess; a Playwright scenario for the library tools UI. |
| 14 | P3 | Scale | The catalog prompt is capped at 300 tools with keyword scoring; beyond ~500 tools quality drops. | Implement the embedding retriever (§12), keep the two-stage pick (libraries then tools). |
| 15 | P3 | Scale | `GenericAgentSessionStore` is in-memory (single node) and gate state lives in process. | Move to a distributed store (Redis/SQL) before multi-node deployment. |
| 16 | P3 | MCP | Only `streamable-http` is supported; `stdio` rows are skipped, no `sse`, no OAuth. | Add transports as needed; OAuth client-credentials for enterprise MCP servers. |
| 17 | P3 | Risk hint | Risk is guessed from names and is not editable. | Allow manual override in the library UI and show it next to every tool. |
| 18 | P3 | Providers | The Google SK connector is an alpha with a role workaround, and tool naming differs per provider. | Track the connector fix, add a provider matrix test for tool calling, and keep `ToolNameConvention-ProviderLimits.md` current. |
