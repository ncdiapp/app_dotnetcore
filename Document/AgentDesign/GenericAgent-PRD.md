# Generic AI Agent Platform — Product Requirements Document

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-02  
**Audience:** Product managers, stakeholders, SaaS administrators  

---

## 1. Problem Statement

App-netore historically shipped four independent AI agents (App Builder, App Report, DB Genie, Data Integration). Each had its own business-logic class containing the same ~300-line agentic loop, copied nearly verbatim:

| Agent | Class | Duplicated infrastructure |
|---|---|---|
| App Builder | `AppBuilderAgentBL.cs` | BuildToolDefinitions, InvokeToolAsync, PruneMessages, CallLLMWithToolsAsync |
| App Report | `AppReportAgentBL.cs` | Same four functions, copy-pasted |
| DB Genie | `AppDbGenieBL.cs` | Same four functions, copy-pasted again |
| Data Integration | `AppDataIntegrationAgentBL.cs` | Same pattern with Cursor cloud adapter |

This created four compounding problems:

1. **Copy-paste debt.** A bug fix or improvement to the agentic loop had to be applied four times. In practice it never was — each class drifted independently.
2. **Recompile per agent.** Adding a fifth agent persona required writing a new C# class, testing, and redeploying the server — a developer task that blocked business users.
3. **Hardcoded system prompts.** Each agent's personality and instructions lived inside C# string literals. Updating wording required a code change and a redeploy.
4. **No SaaS extensibility.** Every new tool capability required a developer to write, compile, and ship C# code. There was no way for a SaaS tenant to add their own agent tools without access to the source.

The cumulative effect: building a sixth agent meant inheriting all the technical debt of the five before it. The platform could not scale to a general-purpose AI assistant layer.

---

## 2. Goals

| Goal | Success condition |
|---|---|
| Single agentic infrastructure | One `GenericAgentBL` + `GenericAgentEngine` replaces all four agent BL classes |
| Zero-recompile new agents | A new agent persona is created entirely through the admin UI, with no code change |
| DB-driven system prompts | Agent personalities are stored in `AppAgentSkillSet.SystemPrompt` and editable at runtime |
| Pluggable tools | New tools (SQL queries, REST calls, C# scripts) registered via admin UI without code |
| MCP server integration | Any external MCP server auto-exposes its tools to any agent persona |
| Per-tenant LLM config | Each SaaS tenant supplies their own API key and chooses their provider |
| Backward-compatible routes | All existing controller URL routes remain unchanged; no frontend migration required |
| Reusable chat UI | A single `<GenericAgentChat />` component works for any agent, anywhere in the app |

---

## 3. Non-Goals

The following are explicitly out of scope for this refactor:

- **AppAISkill feature.** The existing user-created custom prompt library (`AppAISkill` table, `AISkillManagement.tsx`) is unchanged. It remains a separate feature for storing ad-hoc prompts, not agent personas.
- **Frontend agent UIs.** Existing dedicated UIs (AppBuilder chat, DbGenie chat, etc.) are not replaced; they continue to work by calling the same backend routes.
- **Vector database.** Cross-session RAG memory uses the existing `AppBuilderAgentMemoryBL.SearchMemory()` implementation. No new vector store is introduced.
- **Multi-agent orchestration.** This refactor covers single-agent sessions. Orchestrating multiple agents in a pipeline is a future concern.
- **Mobile / offline support.** All streaming is SSE over HTTP, which requires an active server connection.
- **LLM provider beyond OpenAI / Gemini / Anthropic.** Other providers (Cohere, Mistral, Llama) are not added.

---

## 4. User Personas

### Persona A — SaaS Platform Administrator

The administrator manages the platform for one company tenant. They configure which AI providers the platform uses and create or adjust agent personas to suit business workflows. They are technically literate but not .NET developers.

Key tasks:
- Enter API keys for their chosen LLM provider (OpenAI, Gemini, or Anthropic)
- Browse built-in agent personas; adjust system prompts, capability settings, and context thresholds
- Create new agent personas for business-specific workflows (e.g. a "Purchase Order Assistant")
- Register SqlQuery tools that let an agent read from specific database tables
- Connect external MCP servers so agents can call ERP or third-party APIs

Pain points solved: no longer needs a developer to add a new agent or change its behaviour; changes take effect immediately with no server restart.

### Persona B — End User (Employee / Business User)

The end user interacts with AI agents through the chat UI embedded in the platform. They may use App Builder to create a new data module, ask DB Genie to explain query results, or use the App Report agent to pull a status report.

Key tasks:
- Type a natural-language message and receive a streaming response
- Review and approve (or reject) plan proposals from the App Builder agent before any schema is applied
- See live tool-call activity while the agent is working
- Continue a multi-turn conversation across several messages

Pain points solved: consistent chat experience across all agents; real-time streaming instead of waiting for a full response; clear approve/reject gates before destructive operations.

### Persona C — Platform Developer

The developer maintains App-netore. They implement new BuiltIn tool classes in `APP.BL`, run database migrations, and deploy the server.

Key tasks:
- Run V008 and V009 migrations to create the three new tables
- Implement C# plugin methods that agents call via the BuiltIn tool type
- Troubleshoot MCP connectivity or LLM API errors through NLog output
- Add a new LLM provider connector to `GenericAgentEngine`

Pain points solved: no more copy-paste agentic loops; a single codebase path to maintain; NuGet packages managed in one `.csproj`; clear separation between infrastructure (engine) and behaviour (DB config).

---

## 5. Key Features

### 5.1 DB-Driven Agent Personas

Agent personas are stored as rows in the `AppAgentSkillSet` table. Each row holds the agent's identity (`SkillKey`, `DisplayName`), its complete system prompt (`SystemPrompt`), capability flags, and context-management thresholds. Creating a new persona requires only inserting a row — no C# class, no recompile.

Four built-in personas are seeded by migration V008: `app-builder`, `app-report`, `db-genie`, and `data-integration`.

### 5.2 Capability Flags

Each agent persona has an integer bitmask (`CapabilityFlags`) that controls runtime behaviour. The admin UI exposes these as named checkboxes; raw flag values are never shown to users. Supported flags: StreamTokens, MultiTurn, PlanGate, SchemaGate, InjectMemory, InjectSchema, ExternalBackend. (See §7 Glossary for definitions.)

### 5.3 Pluggable Tools via AppAgentToolRegister

Tools are registered per agent in `AppAgentToolRegister`. Each row specifies a `ToolType` that determines how it is executed: BuiltIn (reflect a C# method), SqlQuery (parameterized SQL, no code required), HttpRest (HTTP call, no code required), DynamicCSharp (Roslyn sandbox), ExternalDll (drop a DLL), or PowerShell (script file). SqlQuery and HttpRest tools can be added entirely through the admin UI.

### 5.4 MCP Server Integration

Any MCP-compatible server can be registered in `AppAgentMcpServer`. When a session starts, the engine connects to each registered server, discovers its tool list automatically, and makes all tools available to the agent. No per-tool registration is needed.

### 5.5 Per-Tenant LLM Configuration

Each SaaS tenant stores their LLM settings in `AppTenantSetting` (migration V009). The tenant chooses a provider (OpenAI, Gemini, or Anthropic), enters the API key for that provider, and optionally overrides the default model name. All three providers' keys are stored independently so tenants can switch providers without losing their other keys.

### 5.6 Streaming Chat UI

The `GenericAgentChat` React component connects to the backend via Server-Sent Events. Tokens stream into the message bubble as they arrive. Tool-call activity appears as step indicators with success/failure icons. Plan-gate proposals show an Approve/Reject confirmation UI that pauses the agent until the user responds.

### 5.7 Agent Management UI

`AgentSkillSetManagement` is a three-tab admin screen covering Skill Sets, Tool Register, and MCP Servers. Administrators can create, edit, and delete personas. A Run button opens an inline test chat panel that uses the real backend, so changes can be validated immediately without leaving the admin screen.

---

## 6. Success Metrics

| Metric | Target |
|---|---|
| New agent persona deployed | Zero recompile, zero server restart |
| New SqlQuery tool deployed | Zero recompile; added through UI only |
| Per-tenant API key isolation | Each tenant's key stored in their own `AppTenantSetting` rows; never shared across tenants |
| SSE streaming latency | First token visible within 2 seconds of send |
| MCP server unavailability | Engine skips the unavailable server, logs a warning, and runs with remaining tools — agent does not crash |
| Existing agent routes | 100% backward-compatible; POST `/webapi/AppBuilderAgent/RunAgent` etc. continue to work |
| Admin UI change propagation | Changes to SystemPrompt or CapabilityFlags take effect on the next session start, no restart required |

---

## 7. Glossary

| Term | Definition |
|---|---|
| **SkillKey** | A short, unique string identifier for an agent persona (e.g. `app-builder`, `db-genie`). Primary key of `AppAgentSkillSet`. Used in every API call to select which agent to run. |
| **SkillSet** | A complete agent persona: system prompt, capability flags, context thresholds, and associated tools/MCP servers. Stored as one row in `AppAgentSkillSet`. |
| **CapabilityFlags** | An integer bitmask where each bit enables a specific agent behaviour. Managed as named checkboxes in the UI. |
| **StreamTokens** (flag=1) | Agent streams response tokens over SSE so users see words appearing in real time. |
| **MultiTurn** (flag=2) | Agent maintains conversation history across messages within a session. |
| **PlanGate** (flag=4) | Agent calls `propose_plan` before executing a build; pauses until the user approves or rejects. |
| **SchemaGate** (flag=8) | Agent calls `propose_schema` before applying DDL; pauses until the user approves or rejects. |
| **InjectMemory** (flag=16) | Agent searches cross-session memory (RAG) and prepends relevant past context to the system prompt. |
| **InjectSchema** (flag=32) | Agent has DB schema summary injected into its system prompt automatically. |
| **ExternalBackend** (flag=64) | Agent delegates the entire request to an external backend (e.g. Cursor cloud) instead of running the SK loop. |
| **ToolType** | Determines how a registered tool is executed. One of: BuiltIn, ExternalDll, SqlQuery, PowerShell, HttpRest, DynamicCSharp. |
| **BuiltIn** (ToolType) | Reflects and calls a C# method in the running assembly. Requires a developer to write the method. |
| **SqlQuery** (ToolType) | Executes a parameterized SQL query. No code required — configured entirely in the admin UI. |
| **HttpRest** (ToolType) | Makes an HTTP request to an external URL with argument substitution. No code required. |
| **DynamicCSharp** (ToolType) | Runs a C# script in a Roslyn sandbox with a whitelisted namespace set. |
| **MCP** | Model Context Protocol — an open standard for tools servers that expose their capability list dynamically. Agents connect to MCP servers at session start and auto-discover all available tools. |
| **AppAgentToolRegister** | Database table where all per-agent tool registrations are stored. Each row is one tool. |
| **AppAgentMcpServer** | Database table where MCP server URLs are registered per agent persona. |
| **SystemPrompt** | The full instruction text that defines an agent's persona, workflow rules, and response format. Stored in `AppAgentSkillSet.SystemPrompt`. Editable at runtime. |
| **GenericAgentEngine** | The Semantic Kernel–based agentic loop. Receives a fully configured kernel (provider + tools + MCP plugins) and runs `ChatCompletionAgent.InvokeStreamingAsync`. |
| **GenericAgentBL** | The public entry point. Validates inputs and calls `GenericAgentEngine.RunAsync`. Called by all agent controllers and the generic `/GenericAgent/RunAgent` endpoint. |
| **GenericAgentCallbacks** | A callback object passed through the stack carrying `OnToken`, `OnStep`, `OnDone`, `OnError`, `OnPlanReady`, `OnSchemaReady` delegates. The controller wires these to the session event queue. |
| **GenericAgentSessionStore** | A static in-memory store keyed by session ID. Holds the event queue for SSE delivery and the TaskCompletionSource objects for plan/schema gate resolution. |
| **SK / Semantic Kernel** | Microsoft.SemanticKernel — the .NET agentic AI library that manages the LLM chat loop, tool definitions, function invocation, and streaming. Version 1.74.0 used here. |
| **AppAISkill** | A separate, unrelated feature: a user-managed library of reusable prompt snippets. Not used for agent system prompts and not modified by this refactor. |
