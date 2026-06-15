# AppBuilder AI Agent — Architecture Design

> **Location:** `APP.BL/AppBuilderAgent/`
> **Last updated:** 2026-03-13

---

## 1. Overview

The AppBuilder AI Agent lets a user describe a business application in plain language and have it fully built on the AppAI no-code platform automatically — the same actions a human would perform manually through the UI (design tables, create transactions, build forms, generate search views).

```
User (React Chat UI)
        │  natural-language request
        ▼
AppBuilderAgentController  (Web API)
        │  fire-and-forget Task.Run
        ▼
AppBuilderAgentBL.RunAgentAsync()   ◄──── AgentCallbacks (SignalR wiring)
        │
        ├─ System prompt (static instructions, no memory pre-load)
        ├─ Discover Tools (reflection over plugin instances)
        ├─ Build LLM Tool Definitions
        ├─ Trim ConversationHistory (last N turns)
        │
        └─ Agentic Loop (max 40 iterations, configurable)
               │
               ├──► PruneMessages()  ← sliding-window context guard
               ├──► LLM API (Anthropic / OpenAI / Gemini)
               │       returns: text | tool_use
               │
               ├──► Tool Invocation (reflection)
               │       CapToolResult()  ← size-cap before adding to context
               │       [all plugin tools — see §3]
               │
               └──► Stream events via SignalR ──► React Chat UI
                       agentStep / agentToken / agentDone / agentError
```

---

## 2. The Brain — LLM Integration

The agent uses the **same LLM provider** configured for DbGenie (read from `web.config` via `LLMProviderHelper`).

### Supported Providers

| Provider  | API Endpoint | Tool Protocol |
|-----------|--------------|---------------|
| Anthropic | `https://api.anthropic.com/v1/messages` | `tools` + `tool_use` content block |
| OpenAI    | `https://api.openai.com/v1/chat/completions` | `tools` + `tool_calls` finish reason |
| Gemini    | `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent` | `functionDeclarations` + `functionCall` parts |

### Tool Calling Flow (Anthropic example)

```
BL sends:  { model, system, messages, tools: [...] }
LLM returns: stop_reason = "tool_use"  →  tool_use content blocks
BL invokes: plugin method via reflection → CapToolResult()
BL sends:  { role: "user", content: [{ type: "tool_result", ... }] }
LLM returns: stop_reason = "end_turn"  →  final text response
```

### Key Files

| File | Role |
|------|------|
| `AppBuilderAgentBL.cs` | Agentic loop, LLM HTTP calls, tool dispatch, context management |
| `AgentFunctionAttribute.cs` | `[AgentFunction]` / `[AgentParam]` attributes |
| `App.BL.DbGenie/LLMProviderHelper.cs` | Reads API key & provider from web.config |

---

## 3. The Body — Plugin System (Tools)

Tools are plain C# methods decorated with `[AgentFunction]`. No external framework — discovery and invocation use `System.Reflection`.

### Plugin Discovery

```csharp
// AppBuilderAgentBL.DiscoverTools()
foreach plugin instance
  foreach method with [AgentFunction] attribute
    → build ToolDescriptor { Name, Description, Method, Instance, Parameters }
```

### Tool Invocation

```csharp
// AppBuilderAgentBL.InvokeToolAsync()
JObject inputArgs = JObject.Parse(toolCall.InputJson);
object[] args = MapJsonArgsToParameters(inputArgs, method.Parameters);
var raw = method.Invoke(instance, args);
if (raw is Task<string> t) return await t;   // async support

// result is capped before being added to the message list:
var cappedResult = CapToolResult(toolResult);  // default: 10,000 chars max
```

### Plugins and Their Tools

#### `PlatformExplorerPlugin` — Explore what exists

| Tool | Description |
|------|-------------|
| `explore_platform` | Compact overview: all apps, transactions, entity sources, and DB tables (returns `Formatting.None` — call first) |
| `search_platform(query)` | **RAG tool** — filter apps / transactions / entities / tables by name keyword; max 20 matches per category |
| `list_applications` | Full child tree of every app: transactions + fields + search screens (use for modification work) |
| `get_database_tables` | List all tables and views |
| `get_existing_transactions` | List all configured transaction units |
| `get_transaction_details` | Full config of one transaction unit (fields, search views) |

#### `PlanConfirmPlugin` — Human-in-the-loop gates

| Tool | Description |
|------|-------------|
| `propose_plan` | Pauses the agent; shows a build plan to the user for approval before any DDL |
| `confirm_drop_tables` | Pauses the agent; asks user whether to physically DROP database tables on delete |

Both tools use a `TaskCompletionSource<bool>` gate — the agentic loop suspends until the user responds via the React UI.

#### `SchemaDesignerPlugin` — Two-step schema review

| Tool | Description |
|------|-------------|
| `propose_schema` | Pauses the agent; shows the full table/column/FK design to the user for inline editing. Returns `{Confirmed, SchemaJson, Feedback}`. Schema is stored in-memory on the session store. |
| `execute_approved_schema` | Executes the schema stored from `propose_schema`. Creates DB tables + master-detail transaction in one call. Uses a `TaskCompletionSource<AgentSchemaResponse>` gate. |

#### `ApplicationManagerPlugin` — Application packages

| Tool | Description |
|------|-------------|
| `create_app_package` | Create a named application package; returns `SaasApplicationId` |
| `delete_application` | Delete an entire application (transactions, searches, entities, optionally DB tables) |
| `list_applications` | (Also on PlatformExplorerPlugin) Full child tree |

#### `EntityDataSourcePlugin` — Dropdown data sources

| Tool | Description |
|------|-------------|
| `create_entity_simple_list` | Create a fixed-enumeration dropdown (Sex, Status, Priority, etc.) |
| `create_entity_from_table` | Create a database-table-linked dropdown (Customer, Department, etc.) |
| `list_entity_data_sources` | List all entity sources for the current data source |

#### `TransactionBuilderPlugin` — Build application screens

| Tool | Description |
|------|-------------|
| `create_application` | Full pipeline: AI → schema → tables → transaction hierarchy + forms |
| `create_transaction_from_table` | Create a transaction unit from a pre-existing DB table |
| `create_hierarchy_from_tables` | Build a master-detail transaction from existing tables (recovery path) |
| `create_list_edit_form` | Generate an editable-grid form for a lookup/reference transaction |
| `create_search_view` | Generate a default search/list view; auto-adds to navigation menu |
| `add_search_to_menu` | Manually add a search view to the navigation menu |

#### `TransactionModifierPlugin` — Modify existing screens

| Tool | Description |
|------|-------------|
| `update_transaction_field` | Change `displayName`, `controlType`, `entityId`, or `defaultValue` on a field |
| `set_field_entity` | Link a field to an entity data source (makes it a dropdown) |
| `delete_transaction` | Remove a transaction screen (does NOT drop the DB table) |

#### `SchemaBuilderPlugin` — Database schema

| Tool | Description |
|------|-------------|
| `get_table_schema` | Get column definitions for a table |
| `create_database_table` | Execute a `CREATE TABLE` SQL script |

#### `SchemaAlterPlugin` — Modify existing tables

| Tool | Description |
|------|-------------|
| `alter_table` | Execute `ALTER TABLE ... ADD column` and sync the AppAI data model field in one call |

#### `DataQueryPlugin` — Verify and inspect data

| Tool | Description |
|------|-------------|
| `execute_sql` | Run a `SELECT` query (SELECT only — DDL/DML blocked) |
| `check_table_exists` | Returns true/false for a given table name |
| `insert_mockup_data` | Insert realistic demo/seed rows into a table |

#### `SearchBuilderPlugin` — Standalone report screens

| Tool | Description |
|------|-------------|
| `create_search` | Create a Dataset (SQL) + SearchView (columns) + Search in one step |
| `list_searches` | List all searches for a given application |

#### `MemorySearchPlugin` — RAG memory retrieval

| Tool | Description |
|------|-------------|
| `search_memory(query)` | **RAG tool** — searches build history, platform state, and agent notes for sections matching the query keywords. Returns up to 5 matching sections, each capped at 1,000 chars. |

---

## 4. The Memory — RAG-Based Cross-Session Context

Memory is stored as plain markdown files under `{AppRoot}/memory/appbuilder/`.

```
memory/
└── appbuilder/
    ├── platform-state.md   ← snapshot of recently created tables & transactions
    ├── build-history.md    ← last 5 build sessions (request + summary + created items)
    └── agent-notes.md      ← free-form notes the agent writes to itself
```

### RAG Pattern — On-Demand Retrieval

Memory is **not pre-loaded into the system prompt**. Instead, the agent calls `search_memory(query)` when it needs historical context. This avoids injecting 5,000–9,000 chars of memory overhead on every API call, regardless of relevance.

```
Agent starts:
  system prompt = static instructions only (~8,300 chars)

Agent Step 1 (if building something similar to past work):
  LLM → tool_use: search_memory("inventory management")
  MemorySearchPlugin → AppBuilderAgentMemoryBL.SearchMemory("inventory management")
    splits files by "---" sections
    returns up to 5 matching sections (each ≤1,000 chars)
  LLM receives: only the relevant history context
```

### Memory Lifecycle

```
RunAgentAsync()
  │
  ├─ START: no memory load — agent calls search_memory() when needed
  │
  └─ END:   AppBuilderAgentMemoryBL.RecordBuildSession()
             → AppendBuildHistory()   (trim to last 5 entries)
             → UpdatePlatformState()  (overwrite with latest snapshot)
```

### File-Level Size Guards

Each memory file section is capped when read to prevent unbounded growth:

| File | Cap |
|------|-----|
| `platform-state.md` | 3,000 chars (tail — most recent content kept) |
| `build-history.md` | 4,000 chars (tail — most recent 5 sessions kept) |
| `agent-notes.md` | 2,000 chars (tail — most recent notes kept) |

Implemented via `AppBuilderAgentMemoryBL.TailChars()`, used by `LoadMemoryContext()` (still available for diagnostic use).

### Key File

| File | Role |
|------|------|
| `AppBuilderAgentMemoryBL.cs` | Read, write, search markdown memory files |

---

## 5. Context Management — Token Budget

Token explosion is prevented by four independent layers applied at different points in the loop.

### Layer 1 — Conversation History Trim (at session seed)

When a multi-turn conversation is resumed from the database, only the last **N turns** are included:

```
default: MaxHistoryTurns = 4  →  max 8 messages pre-loaded
override: web.config <add key="Agent.MaxHistoryTurns" value="4" />
```

### Layer 2 — Tool Result Cap (per tool invocation)

Every tool result is capped before being added to the LLM message list:

```
default: MaxToolResultChars = 10,000 chars
override: web.config <add key="Agent.MaxToolResultChars" value="10000" />

Large results get a trailing note:
"[... 45231 chars truncated — ask me to fetch specific details if needed ...]"
```

The UI callback still receives the full result (via the separate `Truncate(result, 600)` path) — only the LLM message is capped.

### Layer 3 — Sliding-Window Message Pruner (per iteration, before LLM call)

Before every LLM API call, the total estimated token count is checked:

```
EstimateTokens = (systemPrompt.Length + all messages serialized) / 4

If EstimateTokens > TokenBudget:
  Drop oldest assistant + tool_result message pair
  Repeat until under budget or fewer than 4 messages remain
  Always preserve messages[0] (original user request)

default: TokenBudget = 120,000 tokens (~480,000 chars)
override: web.config <add key="Agent.TokenBudget" value="120000" />
```

### Layer 4 — Memory RAG (replaces bulk system prompt injection)

See §4. The system prompt is static (~8,300 chars). Memory is retrieved on demand via `search_memory`, returning only relevant sections.

### Combined Budget Example (worst case, defaults)

| Source | Size | Notes |
|--------|------|-------|
| System prompt | ~2,075 tokens | Static, never grows |
| Conversation history seed | ~1,000 tokens | 4 turns max |
| Per iteration (tool results) | ≤2,500 tokens | 10,000 chars ÷ 4 |
| Memory (search_memory call) | ≤1,250 tokens | 5 sections × 1,000 chars ÷ 4 |
| **Sliding window triggers at** | **120,000 tokens** | Drops oldest pairs |

---

## 6. The Loop — Agentic Execution

```
iteration 0..39  (default max 40, configurable)
│
├─ PruneMessages(messages, systemPrompt)  ← context guard
│
├─ emit: agentStep { type: "thinking" }
│
├─ call LLM API
│   ├─ stop_reason = "end_turn" / "stop"  →  DONE (emit agentDone)
│   └─ stop_reason = "tool_use"           →  CONTINUE
│
├─ foreach tool_call in response
│   ├─ emit: agentStep { type: "tool_call", toolName, details (400 chars) }
│   ├─ invoke tool via reflection
│   ├─ emit: agentStep { type: "tool_result", toolName, details (600 chars) }
│   ├─ CapToolResult(result)              ← LLM gets ≤10,000 chars
│   └─ append capped result to messages
│
└─ repeat
```

**Max iterations:** 40 (default) — configurable via `web.config`:
```xml
<add key="Agent.MaxIterations" value="40" />
```

**On LLM text content:** streamed immediately via `OnToken` callback.

---

## 7. Human-in-the-Loop Gates

Two tools pause the agentic loop and wait for user input before proceeding:

### `propose_plan` Gate (PlanConfirmPlugin)

```
Agent calls propose_plan(planSummary, tablesToCreate, screensToCreate)
  │
  ├─ AppBuilderAgentSessionStore stores TaskCompletionSource<bool>
  ├─ SignalR → agentPlan event → React shows plan dialog
  │
  │  [User clicks Approve or Reject]
  │
  ├─ React POST /AppBuilderAgent/RespondToPlan { sessionId, confirmed }
  ├─ Controller calls AppBuilderAgentSessionStore.RespondToPlan(sessionId, bool)
  └─ TCS.SetResult(bool) → agentic loop resumes
       confirmed = true  → proceed with build
       confirmed = false → agent re-plans
```

### `propose_schema` Gate (SchemaDesignerPlugin)

```
Agent calls propose_schema(requirements, appName)
  │
  ├─ LLM generates schema JSON (tables, columns, FKs, relationships)
  ├─ AppBuilderAgentSessionStore stores TaskCompletionSource<AgentSchemaResponse>
  ├─ SignalR → agentSchema event → React shows schema designer UI
  │
  │  [User reviews/edits schema, clicks Build or Reject]
  │
  ├─ React POST /AppBuilderAgent/RespondToSchema { sessionId, confirmed, schemaJson, feedback }
  ├─ Controller calls AppBuilderAgentSessionStore.RespondToSchema(sessionId, response)
  └─ TCS.SetResult(response) → agentic loop resumes
       confirmed = true  → schemaJson stored on session; agent calls execute_approved_schema
       confirmed = false → agent adjusts and re-proposes
```

### Session Store

`AppBuilderAgentSessionStore` is an in-memory `ConcurrentDictionary` keyed by `sessionId`. It holds the TCS gates and (after schema confirmation) the approved schema JSON so `execute_approved_schema` can retrieve it without the agent re-submitting it.

---

## 8. Real-Time Streaming — SignalR

The BL layer has **zero SignalR dependency**. It accepts `AgentCallbacks` (plain `Func<T, Task>` delegates). The Controller wires these delegates to SignalR at the web layer.

### AgentCallbacks

```csharp
public class AgentCallbacks
{
    public Func<AgentStepEvent, Task>   OnStep;       // tool_call / tool_result / thinking
    public Func<string, Task>           OnToken;      // streaming text chunks
    public Func<AgentDoneEvent, Task>   OnDone;       // final response + created items
    public Func<string, Task>           OnError;      // error message
    public Func<AgentPlanEvent, Task>   OnPlanReady;  // propose_plan gate
    public Func<AgentSchemaEvent, Task> OnSchemaReady;// propose_schema gate
}
```

### SignalR Events (server → client)

| Event | Payload | When |
|-------|---------|------|
| `agentStep` | `AgentStepEvent` | Each thinking / tool_call / tool_result step |
| `agentToken` | `string` | Each text chunk from LLM |
| `agentDone` | `AgentDoneEvent` | Agent finished; includes created transaction IDs, updated history, checkpoint |
| `agentError` | `string` | Unrecoverable error |
| `agentPlan` | `AgentPlanEvent` | `propose_plan` gate — React shows approval dialog |
| `agentSchema` | `AgentSchemaEvent` | `propose_schema` gate — React shows schema designer |

### Request Flow

```
React UI
  │ 1. GET /appBuilderAgentHub (SignalR connect)
  │ 2. invoke GetConnectionId() → store connectionId
  │ 3. POST /webapi/AppBuilderAgent/RunAgent { connectionId, userMessage, sessionId, ... }
  ▼
AppBuilderAgentController
  │ 4. Build AgentCallbacks wired to hubContext.Clients.Client(connectionId)
  │ 5. Task.Run → AppBuilderAgentBL.RunAgentAsync(request, callbacks)
  │ 6. Return 202 Accepted immediately
  ▼
React UI receives real-time events via SignalR connection
```

### Key Files

| File | Role |
|------|------|
| `PlmApplication/Server/SignalR/AppBuilderAgentHub.cs` | Hub: `GetConnectionId()` |
| `PlmApplication/Server/WebApi/AppBuilderAgentController.cs` | POST endpoint + callback wiring + gate response endpoints |
| `PlmApplication/AppReact/src/webapi/appbuilderagentsvc.ts` | SignalR client + API calls |
| `PlmApplication/AppReact/src/components/integration/AppBuilderAgent.tsx` | Chat UI component |

---

## 9. Data Flow — Full Example

> **User says:** "Build an inventory management app with products, categories, and stock movements."

```
1. React POST /webapi/AppBuilderAgent/RunAgent
   { connectionId, sessionId, userMessage: "Build inventory...", dataSourceId: 1 }

2. Agent starts:
   a. System prompt = static instructions only (no memory pre-load)
   b. Discovers 26 tools from 11 plugins (via reflection)
   c. Trims ConversationHistory to last 4 turns

3. Iteration 1 — Explore + memory check:
   LLM → tool_use: explore_platform()
   Result: compact JSON, Formatting.None (~3k chars) → CapToolResult passes through
   LLM → tool_use: search_memory("inventory product stock")
   Result: 2 matching sections from build-history.md (~800 chars)

4. Iteration 2 — Propose schema:
   LLM → tool_use: propose_schema(requirements, appName)
   SchemaDesignerPlugin → LLM generates schema JSON
   SignalR → agentSchema event → React shows schema designer
   [User edits columns, clicks Build]
   Controller → RespondToSchema(confirmed=true, schemaJson=...)
   TCS.SetResult → loop resumes

5. Iteration 3 — Create entity sources:
   LLM → tool_use: create_entity_simple_list("Category Status", [...])
   Result: { EntityId: 5, EntityCode: "CategoryStatus" }

6. Iteration 4 — Execute schema:
   LLM → tool_use: execute_approved_schema(saasApplicationId: 12, entityMapJson: {...})
   SchemaDesignerPlugin → creates DB tables + master-detail transaction
   Result: { TablesCreated: ["Category","Product","StockMovement"], TransactionId: 42, LookupTables: ["Category"] }

7. Iterations 5-7 — Lookup table screens + search views:
   create_entity_from_table("Category", 12)
   create_transaction_from_table + create_list_edit_form + create_search_view for Category
   create_search_view(transactionId: 42)   ← master-detail screen

8. Iterations 8-10 — Mockup data:
   insert_mockup_data("Category", [...])
   insert_mockup_data("Product", [...])
   insert_mockup_data("StockMovement", [...])

9. Iteration 11 — Done:
   LLM → stop_reason: "end_turn"
   SignalR → agentToken "I have built your Inventory Management application..."
   SignalR → agentDone { createdTransactions: [{42, "Inventory"}, ...], checkpoint: {...} }

10. Memory updated:
    build-history.md ← appended new session entry
    platform-state.md ← updated with new tables/transactions
```

---

## 10. File Structure

```
APP.BL/AppBuilderAgent/
├── ARCHITECTURE.md                       ← this file
├── AgentFunctionAttribute.cs             ← [AgentFunction] / [AgentParam] attributes
├── AppBuilderAgentBL.cs                  ← core orchestrator (loop, LLM calls, context management)
├── AppBuilderAgentMemoryBL.cs            ← markdown memory read / write / RAG search
├── AppBuilderAgentSessionStore.cs        ← in-memory TCS gates for propose_plan / propose_schema
├── AppBuilderAgentSessionBL.cs           ← persist session state to database
└── Plugins/
    ├── PlatformExplorerPlugin.cs         ← explore_platform, search_platform, list_applications, get_*
    ├── PlanConfirmPlugin.cs              ← propose_plan, confirm_drop_tables
    ├── SchemaDesignerPlugin.cs           ← propose_schema, execute_approved_schema
    ├── ApplicationManagerPlugin.cs       ← create_app_package, delete_application
    ├── EntityDataSourcePlugin.cs         ← create_entity_simple_list, create_entity_from_table
    ├── TransactionBuilderPlugin.cs       ← create_application, create_search_view, create_transaction_from_table, create_hierarchy_from_tables, create_list_edit_form, add_search_to_menu
    ├── TransactionModifierPlugin.cs      ← update_transaction_field, set_field_entity, delete_transaction
    ├── SchemaBuilderPlugin.cs            ← get_table_schema, create_database_table
    ├── SchemaAlterPlugin.cs              ← alter_table
    ├── DataQueryPlugin.cs                ← execute_sql, check_table_exists, insert_mockup_data
    ├── SearchBuilderPlugin.cs            ← create_search, list_searches
    └── MemorySearchPlugin.cs             ← search_memory (RAG over build history / notes)

APP.Components.Dto/UserDefine/AppBuilderAgent/
└── AppBuilderAgentDto.cs                 ← all request/response/event DTOs + AgentCallbacks

PlmApplication/Server/
├── SignalR/AppBuilderAgentHub.cs         ← SignalR hub (GetConnectionId)
└── WebApi/AppBuilderAgentController.cs  ← RunAgent + RespondToPlan + RespondToSchema

PlmApplication/AppReact/src/
├── webapi/appbuilderagentsvc.ts          ← SignalR client + API service
└── components/integration/
    └── AppBuilderAgent.tsx               ← chat UI component

memory/appbuilder/                        ← runtime, not in source control
├── platform-state.md
├── build-history.md
└── agent-notes.md
```

---

## 11. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **No Microsoft.SemanticKernel** | SK 1.70+ requires Azure.AI.OpenAI ≥ 2.8.0-beta, incompatible with .NET 4.6.2. Custom `[AgentFunction]` attributes + `System.Reflection` achieve the same plugin pattern with zero external dependencies. |
| **AgentCallbacks delegates** | Keeps BL infrastructure-free. SignalR stays in the web layer. BL is testable without a running SignalR hub. |
| **Fire-and-forget Task.Run** | Agent runs take 30–180 seconds. Controller returns HTTP 202 immediately; progress streams via SignalR. |
| **Raw HttpClient** | Direct HTTP calls to Anthropic/OpenAI/Gemini APIs. No SDK dependency. Provider-specific parsing in separate `CallXxxWithToolsAsync` methods. |
| **RAG memory (search_memory)** | Memory is not pre-loaded into the system prompt. The agent calls `search_memory(query)` on demand. Saves ~2,000–2,500 tokens per API call on sessions where history is irrelevant. |
| **search_platform tool** | Filtered platform lookup instead of dumping all 200 tables every time. Agent calls `explore_platform` for the index, then `search_platform("Foo")` to find a specific item. |
| **Tool result cap (10,000 chars)** | Prevents a single large tool result (schema dumps, entity lists) from consuming 50–200k chars in one shot. Model receives a truncation notice and can request specific details. |
| **Sliding-window pruner (120k token budget)** | Drops oldest assistant+tool_result pairs when the context grows too large. Preserves the original user request (messages[0]) so the model never forgets the task. |
| **Conversation history trim (4 turns)** | On multi-turn sessions loaded from DB, limits pre-seeded history to avoid blowing the budget before the first tool call. |
| **Two TCS gate patterns** | `propose_plan` uses `TCS<bool>` (approved/rejected). `propose_schema` uses `TCS<AgentSchemaResponse>` (includes edited schema JSON + feedback). Both gates suspend the agentic loop without blocking a thread pool thread. |
| **SELECT-only guard in execute_sql** | Prevents the agent from running destructive SQL through the verification tool. All DDL goes through dedicated tools (`create_database_table`, `alter_table`, `execute_approved_schema`). |
| **Configurable limits via web.config** | `Agent.MaxIterations`, `Agent.TokenBudget`, `Agent.MaxToolResultChars`, `Agent.MaxHistoryTurns` — all tunable without recompile. |
