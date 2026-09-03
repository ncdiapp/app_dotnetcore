# AppBuilder AI Agent — Architecture

> **Location:** `APP.BL/AppBuilderAgent/`
> **Last updated:** 2026-07-23

---

## 1. Overview

The AppBuilder AI Agent accepts a plain-language description of a business application and builds it automatically on the AppAI no-code platform — the same actions a human performs manually (design tables, create transactions, build forms, generate search views).

```
User (React Chat UI)
        │  natural-language request
        ▼
AppBuilderAgentController  POST /RunAgent
        │  returns SessionId immediately; fires Task.Run in background
        │
        │◄── React polls GET /PollEvents every ~500ms
        │         drains ConcurrentQueue<AgentEventDto>
        │
        ▼
AppBuilderAgentBL.RunAgentAsync()    ◄──── AgentCallbacks (enqueue to session store)
        │
        ├─ RegisterSystemAgentIdentity   ← agent gets a DB-connected user identity
        ├─ AppBuilderAgentSessionBL.SaveSession  ← row in dbo.AppBuilderAgentSession
        ├─ DiscoverTools()               ← reflection over 12 plugin instances → 31 tools
        ├─ BuildToolDefinitions()        ← provider-specific JSON schemas
        ├─ TrimConversationHistory()     ← last MaxHistoryTurns × 2 messages
        │
        └─ Agentic Loop  (max 40 iterations, configurable)
               │
               ├──► PruneMessages()   ← sliding-window token budget guard
               ├──► LLM API call      ← Anthropic / OpenAI / Gemini
               │       returns: text | tool_use
               │
               ├──► Tool Invocation via reflection
               │       CapToolResult()  ← cap before adding to context
               │
               └──► Enqueue AgentEventDto → React poll receives it

Human gates: React POST /ConfirmPlan or /ConfirmSchema
        → controller resolves TaskCompletionSource
        → agentic loop resumes
```

---

## 2. LLM Integration

The agent uses the same LLM provider configured for DbGenie (read from `web.config` via `LLMProviderHelper`).

### Supported Providers

| Provider  | API Endpoint | Tool Protocol |
|-----------|--------------|---------------|
| Anthropic | `https://api.anthropic.com/v1/messages` | `tools` array + `tool_use` content block |
| OpenAI    | `https://api.openai.com/v1/chat/completions` | `tools` array + `tool_calls` finish reason |
| Gemini    | `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent` | `functionDeclarations` + `functionCall` parts |

Config keys: `DbaGenieLLMProvider`, `DbaGenieApiKey`, `DbaGenieAnthropicModel`, `DbaGenieOpenAIModel`, `DbaGenieGeminiModel`

### Tool Calling Flow (Anthropic example)

```
BL sends:   { model, system, messages, tools: [...], max_tokens: 8192 }
LLM returns: stop_reason = "tool_use"  →  tool_use content blocks
BL invokes:  plugin method via reflection
BL sends:   { role: "user", content: [{ type: "tool_result", tool_use_id, content }] }
LLM returns: stop_reason = "end_turn"  →  final text response
```

All three providers share one `HttpClient` per call (10-minute timeout). No provider SDK is used — raw HTTP only.

### Key Files

| File | Role |
|------|------|
| `AppBuilderAgentBL.cs` | Agentic loop, all three LLM HTTP callers, tool dispatch, context management |
| `AgentFunctionAttribute.cs` | `[AgentFunction]` / `[AgentParam]` attributes |
| `App.BL.DbGenie/LLMProviderHelper.cs` | Reads API key and provider from `web.config` |

---

## 3. Plugin System (Tools)

Tools are plain C# methods decorated with `[AgentFunction]`. No external framework — `System.Reflection` handles discovery and invocation. `[AgentParam]` on each parameter provides description and required/optional.

### Discovery

```csharp
// AppBuilderAgentBL.DiscoverTools()
foreach plugin instance  // 12 instances, instantiated in fixed order
  foreach method with [AgentFunction]
    → ToolDescriptor { Name, Description, Method, Instance, Parameters }
```

### Invocation

```csharp
// AppBuilderAgentBL.InvokeToolAsync()
JObject args = JObject.Parse(toolCall.InputJson);
object[] typedArgs = MapJsonArgsToParameters(args, method.Parameters);
var raw = method.Invoke(instance, typedArgs);
if (raw is Task<string> t) return await t;   // async tools supported

// result capped before being added to the LLM message list:
string capped = CapToolResult(toolResult);   // default: 10,000 chars max
```

### All 12 Plugins and 31 Tools

#### `PlanConfirmPlugin` — Human-in-the-loop gates
| Tool | Description |
|------|-------------|
| `propose_plan` | Shows build plan to user (tablesToCreate, screensToCreate); pauses loop until approved/rejected via `TCS<bool>` |
| `confirm_drop_tables` | Shows table drop list; pauses loop for drop confirmation via `TCS<bool>` |

#### `SchemaDesignerPlugin` — Two-step schema review
| Tool | Description |
|------|-------------|
| `propose_schema` | Calls `AppDbGenieBL.ExtractSchemaFromTextAsync` (second LLM call); shows table/column/FK design for inline user editing; stores approved schema on plugin instance; pauses via `TCS<AgentSchemaResponse>` |
| `execute_approved_schema` | Reads schema stored by `propose_schema`; calls `AppDbGenieBL.CreateHierarchyFromApprovedSchemaAsync`; auto-rolls back (DROP TABLE) on partial failure |

#### `ApplicationManagerPlugin` — Application packages
| Tool | Description |
|------|-------------|
| `create_app_package` | Creates named application package; returns `SaasApplicationId` |
| `delete_application` | Deletes entire application: searches → transactions → entities → optionally DROP tables → package |
| `add_transaction_to_menu` | Adds a list-transaction link to the navigation menu |
| `add_search_to_menu` | Adds a search/dataset link to the navigation menu |

#### `PlatformExplorerPlugin` — Inspect existing state
| Tool | Description |
|------|-------------|
| `explore_platform` | Compact overview: all apps, transactions, entity sources, DB tables (compact JSON; call first) |
| `search_platform` | Keyword-filtered lookup across apps / transactions / entities / tables (≤20 matches per category) |
| `list_applications` | Full child tree per app: transactions + fields + search screens |
| `get_database_tables` | All tables and views for the configured data source |
| `get_existing_transactions` | All configured transaction units |
| `get_transaction_details` | Full config of one transaction: all units, fields, search views |

#### `EntityDataSourcePlugin` — Dropdown data sources
| Tool | Description |
|------|-------------|
| `list_entity_data_sources` | All entity sources for the current data source |
| `create_entity_simple_list` | Fixed-enumeration dropdown (Sex, Status, Priority, …); `EntityType=4` |
| `create_entity_from_table` | Database-table-linked dropdown (Customer, Department, …); `EntityType=1` |

#### `TransactionBuilderPlugin` — Build application screens
| Tool | Description |
|------|-------------|
| `create_application` | Full pipeline: AI schema extraction → DDL → transaction hierarchy; optional FK field entity wiring; auto-rollback on failure |
| `create_search_view` | Generates default search/list view for a transaction; auto-adds nav link |
| `create_transaction_from_table` | Creates a transaction unit from an existing DB table |
| `create_hierarchy_from_tables` | Builds master-detail transaction from existing tables; validates FKs via `sys.foreign_keys` |
| `create_list_edit_form` | Generates editable-grid form for a lookup/reference transaction; auto-generates search view |

#### `TransactionModifierPlugin` — Modify existing screens
| Tool | Description |
|------|-------------|
| `update_transaction_field` | Changes `displayName`, `controlType`, `entityId`, or `defaultValue` on a field; always resolves by `fieldId` (not name, which is non-unique across a hierarchy) |
| `set_field_entity` | Links a field to an entity data source (`ControlType=1` DDL); pass `null` to unlink |
| `delete_transaction` | Removes a transaction screen (does NOT drop the DB table) |

#### `SchemaBuilderPlugin` — Raw schema tools
| Tool | Description |
|------|-------------|
| `get_table_schema` | Column definitions for a specific table |
| `create_database_table` | Executes a `CREATE TABLE` DDL script |

#### `SchemaAlterPlugin` — Modify existing tables
| Tool | Description |
|------|-------------|
| `alter_table` | Executes `ALTER TABLE … ADD column` and optionally syncs the AppAI transaction field in one call |

#### `DataQueryPlugin` — Inspect and seed data
| Tool | Description |
|------|-------------|
| `execute_sql` | Runs a `SELECT` query only (DDL/DML blocked at BL level) |
| `check_table_exists` | Returns true/false for a given table name via `INFORMATION_SCHEMA.TABLES` |
| `insert_mockup_data` | Inserts realistic demo rows; caller must supply INSERT SQL; executed statement by statement |

#### `SearchBuilderPlugin` — Standalone report screens
| Tool | Description |
|------|-------------|
| `create_search` | Creates Dataset (SQL query) + SearchView (auto-columns) + Search record in one step |
| `list_searches` | Lists all searches for a given application |

#### `MemorySearchPlugin` — RAG memory retrieval
| Tool | Description |
|------|-------------|
| `search_memory` | Keyword search over build history, platform state, and agent notes; up to 5 matching sections, each ≤1,000 chars |

---

## 4. Memory — RAG-Based Cross-Session Context

Memory is stored as plain markdown files under `{AppRoot}/memory/appbuilder/`.

```
memory/
└── appbuilder/
    ├── platform-state.md   ← snapshot of recently created tables & transactions
    ├── build-history.md    ← last 5 build sessions (request + summary + created items)
    └── agent-notes.md      ← free-form notes the agent writes to itself
```

### RAG Pattern — On-Demand Retrieval

Memory is **not pre-loaded into the system prompt**. The agent calls `search_memory(query)` when it needs historical context. This avoids injecting 5,000–9,000 chars of memory on every API call regardless of relevance.

```
Agent starts:
  system prompt = static instructions only (~8,300 chars)

Agent Step 1 (when building something with prior history):
  LLM → tool_use: search_memory("inventory product stock")
  MemorySearchPlugin → splits files by "---" sections, keyword-matches, returns ≤5 sections
  LLM receives: only the relevant historical context
```

### Memory Lifecycle

```
RunAgentAsync()
  ├─ START: no memory load — agent calls search_memory() on demand
  └─ END:   AppBuilderAgentMemoryBL.RecordBuildSession()
               → build-history.md  (appended; trimmed to last 5 entries)
               → platform-state.md (overwritten with latest snapshot)
```

Memory read failures are silently swallowed — never crash the agent.

### File Size Guards

| File | Cap (tail-truncated) |
|------|------|
| `platform-state.md` | 3,000 chars |
| `build-history.md` | 4,000 chars |
| `agent-notes.md` | 2,000 chars |

---

## 5. Context Management — Token Budget

Four independent layers prevent token explosion at different points in the loop.

### Layer 1 — Conversation History Trim (at session seed)

```
default: MaxHistoryTurns = 4  →  max 8 messages seeded
config:  <add key="Agent.MaxHistoryTurns" value="4" />
```

### Layer 2 — Tool Result Cap (per tool invocation)

```
default: MaxToolResultChars = 10,000 chars
config:  <add key="Agent.MaxToolResultChars" value="10000" />

Truncated results append:
"[... 45231 chars truncated — ask me to fetch specific details if needed ...]"
```

The step event shown in the UI uses a separate 600-char truncation and is not affected.

### Layer 3 — Sliding-Window Message Pruner (per iteration, before LLM call)

```
EstimateTokens = (systemPrompt.Length + serialized messages) / 4

If over TokenBudget:
  Drop oldest logical group (assistant message + its tool_result messages)
  Repeat until under budget or only gate groups remain
  Always preserve messages[0] (original user request)

default: TokenBudget = 120,000 tokens
config:  <add key="Agent.TokenBudget" value="120000" />
```

**Gate tool protection** — these four tool names are never evicted from the window:
```csharp
private static readonly string[] GateToolNames =
{
    "propose_schema",
    "propose_plan",
    "execute_approved_schema",
    "confirm_drop_tables",
};
```
If any message in a group references a gate tool name, the group is skipped by the pruner.

### Layer 4 — Memory RAG (replaces bulk system prompt injection)

System prompt is static (~8,300 chars). Memory is fetched on demand via `search_memory`.

### Token Budget Summary (worst case, defaults)

| Source | Approximate Size |
|--------|------|
| System prompt | ~2,075 tokens (static) |
| Seeded conversation history | ~1,000 tokens (4 turns max) |
| Per-iteration tool results | ≤2,500 tokens (10,000 chars ÷ 4) |
| Memory (`search_memory` call) | ≤1,250 tokens (5 sections × 1,000 chars ÷ 4) |
| Sliding window triggers at | **120,000 tokens** |

---

## 6. Agentic Loop

```
iteration 0..39  (default max 40)
│
├─ PruneMessages()                 ← token budget guard
├─ emit step { type: "thinking" }
│
├─ call LLM API
│   ├─ stop_reason = "end_turn"/"stop"  →  set finalResponse, BREAK
│   └─ stop_reason = "tool_use"         →  CONTINUE
│
├─ foreach tool_call in response
│   ├─ emit step { type: "tool_call", toolName, input (400 chars) }
│   ├─ InvokeToolAsync()           ← reflection dispatch
│   ├─ ExtractCreatedItems()       ← harvest IDs/names for checkpoint
│   ├─ emit step { type: "tool_result", toolName, result (600 chars) }
│   ├─ CapToolResult()             ← LLM gets ≤10,000 chars
│   └─ append to messages
│
└─ repeat

On loop exit:
  AppBuilderAgentMemoryBL.RecordBuildSession()
  BuildCheckpoint(createdItems)   ← AgentCheckpointDto
  AppBuilderAgentSessionBL.UpdateSession("Completed", checkpoint, ...)
  emit OnDone { FinalResponse, CreatedTransactions, UpdatedHistory, Checkpoint }

On unhandled exception:
  UpdateSession("Failed")
  emit OnError
```

Config: `<add key="Agent.MaxIterations" value="40" />`

---

## 7. 8-Step Build Workflow (System Prompt)

The system prompt (embedded constant, ~344 lines) instructs the LLM to follow this workflow for every new build:

| Step | Tools Used |
|------|-----------|
| 1 — Explore | `explore_platform`, `search_platform`, `search_memory` |
| 2 — Create App Package | `create_app_package` → obtain `SaasApplicationId` |
| 3 — Create Entity Data Sources | `create_entity_simple_list` (fixed enumerations), `create_entity_from_table` (managed DB tables) |
| 4a — Propose Schema | `propose_schema(requirements, appName)` → user approves/edits |
| 4b — Execute Schema | `execute_approved_schema(saasApplicationId)` → DDL + transactions; process `LookupTables[]` |
| 5 — Search Views | `create_search_view` for master-detail; `create_list_edit_form` + `create_search_view` for lookup tables |
| 5b — Mockup Data | `insert_mockup_data` per table in FK-dependency order |
| 6–8 — Search, Menu, Summary | `create_search`, `add_search_to_menu` / `add_transaction_to_menu`, final text report |

**Key rules embedded in the prompt:**
- `propose_plan` is required before `create_application` or `create_database_table` (not before `execute_approved_schema` — that has its own schema gate)
- FK in child table → composition (master-detail hierarchy); FK in current table pointing outward → lookup (DDL dropdown)
- Maximum 3-level hierarchy: master → child → grandchild
- Always use `fieldId` over `fieldName` for modifications (`fieldName` is not unique across a hierarchy)
- Delete workflow: `list_applications` → `propose_plan` → `confirm_drop_tables` → `delete_application`
- Resume workflow: inspect checkpoint, skip completed steps, jump to first incomplete step

---

## 8. Human-in-the-Loop Gates

Two tools pause the agentic loop and wait for user input via `TaskCompletionSource`.

### `propose_plan` Gate

```
Agent calls propose_plan(planSummary, tablesToCreate, screensToCreate)
  │
  ├─ SessionStore.RegisterPlanConfirmation(sessionId) → stores TCS<bool>
  ├─ Enqueue AgentEventDto { EventType: "plan", Plan: AgentPlanEvent }
  │
  │  React poll receives event → shows plan approval dialog
  │  [User clicks Approve or Reject]
  │  React POST /webapi/AppBuilderAgent/ConfirmPlan { sessionId, confirmed }
  │
  ├─ Controller: SessionStore.ConfirmPlan(sessionId, confirmed)
  └─ TCS.SetResult(bool) → loop resumes
       confirmed = true  → proceed with build
       confirmed = false → agent re-plans
```

### `propose_schema` Gate

```
Agent calls propose_schema(requirements, appName)
  │
  ├─ AppDbGenieBL.ExtractSchemaFromTextAsync()  ← second LLM call, structured output
  ├─ SessionStore.RegisterSchemaConfirmation(sessionId) → stores TCS<AgentSchemaResponse>
  ├─ Enqueue AgentEventDto { EventType: "schema", Schema: AgentSchemaEvent }
  │
  │  React poll receives event → shows schema designer (editable tables/columns/FKs)
  │  [User edits schema, clicks Build or Reject]
  │  React POST /webapi/AppBuilderAgent/ConfirmSchema { sessionId, confirmed, schemaJson, feedback }
  │
  ├─ Controller: SessionStore.ConfirmSchema(sessionId, AgentSchemaResponse)
  └─ TCS.SetResult(response) → loop resumes
       confirmed = true  → schemaJson stored on SchemaDesignerPlugin instance
                           agent calls execute_approved_schema
       confirmed = false → agent adjusts and re-proposes
```

### Session Store

`AppBuilderAgentSessionStore` (static, `ConcurrentDictionary`-backed):
- `Sessions` — one `ConcurrentQueue<AgentEventDto>` per active session
- `PendingConfirmations` — one `TCS<bool>` per plan gate
- `PendingSchemaConfirmations` — one `TCS<AgentSchemaResponse>` per schema gate
- Sessions older than 30 minutes are garbage-collected on `CreateSession()`; expired TCS gates are auto-resolved with `false` / `{ Confirmed = false }` on cleanup

---

## 9. Transport — HTTP Polling

The BL layer has **zero transport dependency**. It accepts `AgentCallbacks` (plain `Func<T, Task>` delegates). The Controller wires these to the session store event queue.

### AgentCallbacks

```csharp
public class AgentCallbacks
{
    public Func<AgentStepEvent, Task>                        OnStep;        // tool_call / tool_result / thinking
    public Func<string, Task>                                OnToken;       // streaming text chunks
    public Func<AgentDoneEvent, Task>                        OnDone;        // final response + created items
    public Func<string, Task>                                OnError;       // error message
    public Func<AgentPlanEvent, Task<bool>>                  OnPlanReady;   // propose_plan gate (blocking)
    public Func<AgentSchemaEvent, Task<AgentSchemaResponse>> OnSchemaReady; // propose_schema gate (blocking)
}
```

### Polling Flow

```
React UI
  │ 1. POST /webapi/AppBuilderAgent/RunAgent
  │     { userMessage, dataSourceRegisterId, conversationHistory, ... }
  │     ← 200 { SessionId, IsStarted }
  │
  │ 2. poll every ~500ms:
  │     GET /webapi/AppBuilderAgent/PollEvents?sessionId=...
  │     ← { Events: [...], SessionExists: true/false }
  │
  │    Each AgentEventDto.EventType: "step" | "token" | "done" | "error" | "plan" | "schema"
  │
  │ 3. On "plan" event:
  │     Show approval dialog → POST /ConfirmPlan { sessionId, confirmed }
  │
  │ 4. On "schema" event:
  │     Show schema designer → POST /ConfirmSchema { sessionId, confirmed, schemaJson }
  │
  └─ On "done" event: stop polling
```

**Note:** `AppBuilderAgentHub` (`AppAI.Web/Hubs/AppBuilderAgentHub.cs`) is registered but not used by the controller. It is a retained artifact from an earlier SignalR-push design.

---

## 10. Session Persistence and Resume

`AppBuilderAgentSessionBL` persists each run to `dbo.AppBuilderAgentSession` (auto-created on first use, no migration script required).

### Table Schema

| Column | Type | Notes |
|--------|------|-------|
| `SessionGuid` | `NVARCHAR(50)` | PK, generated GUID |
| `CreatedAt` / `UpdatedAt` | `DATETIME` | |
| `UserRequest` | `NVARCHAR(2000)` | |
| `Status` | `NVARCHAR(20)` | `InProgress` \| `Completed` \| `Failed` |
| `CurrentStep` | `NVARCHAR(200)` | |
| `CreatedById` | `INT` | User who started the run |
| `CheckpointJson` | `NVARCHAR(MAX)` | Serialized `AgentCheckpointDto` |
| `ConversationHistoryJson` | `NVARCHAR(MAX)` | Serialized `List<AppBuilderAgentMessageDto>` |
| `FinalResponse` | `NVARCHAR(4000)` | |

### AgentCheckpointDto

Stored after each run; enables **resumable builds**:

```
SaasApplicationId     — ID of the created application package
ApplicationName       — display name
TablesCreated[]       — DB tables successfully created
TransactionsCreated[] — platform transaction IDs and names
EntitiesCreated[]     — entity data source IDs and names
ApprovedSchemaJson    — schema approved but not yet executed (mid-run resume)
IsComplete            — whether the run finished
Timestamp
```

The system prompt's RESUME RULE tells the LLM to inspect the checkpoint and skip steps already complete.

---

## 11. Data Flow — Full Example

> **User says:** "Build an inventory management app with products, categories, and stock movements."

```
1.  POST /RunAgent { userMessage: "Build inventory...", dataSourceRegisterId: 1 }
    ← { SessionId: "abc-123", IsStarted: true }

2.  Agent starts:
    a. Registers system agent identity for DB access
    b. Discovers 31 tools from 12 plugins
    c. Seeds ConversationHistory (empty for first run)

3.  Iter 1 — Explore:
    explore_platform()           → compact overview of existing apps/tables
    search_memory("inventory")   → 0 matches (nothing in history yet)

4.  Iter 2 — Propose schema:
    propose_schema("inventory app with...", "Inventory")
      → ExtractSchemaFromTextAsync (second LLM call)
      → Enqueue { EventType: "schema", Tables: [Category, Product, StockMovement] }
    React shows schema designer → user edits columns → POST /ConfirmSchema
    TCS resolved → loop resumes with approved schemaJson stored on plugin

5.  Iter 3 — App package + entity sources:
    create_app_package("Inventory Management") → { SaasApplicationId: 12 }
    create_entity_simple_list("MovementType", "IN|OUT|ADJUST", 12)

6.  Iter 4 — Execute schema:
    execute_approved_schema(saasApplicationId: 12)
      → CreateHierarchyFromApprovedSchemaAsync
      → tables: Category, Product, StockMovement
      → master transaction: Inventory (Product master, StockMovement child)
      → LookupTables: ["Category"]

7.  Iters 5-7 — Lookup screens and search views:
    create_entity_from_table("Category", "Category", "dbo", "CategoryId", "CategoryName", 12)
    create_transaction_from_table("Category", 12) + create_list_edit_form + create_search_view
    create_search_view(transactionId: 42)         ← master Inventory screen

8.  Iters 8-10 — Mockup data (FK order: Category first, then Product, then StockMovement):
    insert_mockup_data("Category", "INSERT INTO ...")
    insert_mockup_data("Product",  "INSERT INTO ...")
    insert_mockup_data("StockMovement", "INSERT INTO ...")

9.  Iter 11 — Menu:
    create_search("Inventory Overview", "SELECT ...", 12) → { SearchId: 7 }
    add_search_to_menu(7, "Inventory")
    add_transaction_to_menu("Category", "Inventory")

10. Iter 12 — Done:
    LLM stop_reason: "end_turn"
    Enqueue { EventType: "token", Token: "I have built your Inventory Management app..." }
    Enqueue { EventType: "done", Done: { CreatedTransactions: [{42, "Inventory"}], ... } }

11. Memory updated:
    build-history.md ← new session entry appended
    platform-state.md ← updated with Category, Product, StockMovement tables
```

---

## 12. File Structure

```
APP.BL/AppBuilderAgent/
├── ARCHITECTURE.md                       ← this file
├── AgentFunctionAttribute.cs             ← [AgentFunction] / [AgentParam] attributes
├── AppBuilderAgentBL.cs                  ← core orchestrator: loop, LLM calls, context management
├── AppBuilderAgentMemoryBL.cs            ← markdown memory: read / write / RAG search
├── AppBuilderAgentSessionBL.cs           ← persist session to dbo.AppBuilderAgentSession
├── AppBuilderAgentSessionStore.cs        ← in-memory event queues + TCS gates (polling transport)
└── Plugins/
    ├── PlanConfirmPlugin.cs              ← propose_plan, confirm_drop_tables
    ├── SchemaDesignerPlugin.cs           ← propose_schema, execute_approved_schema
    ├── ApplicationManagerPlugin.cs       ← create_app_package, delete_application, add_*_to_menu
    ├── PlatformExplorerPlugin.cs         ← explore_platform, search_platform, list_applications, get_*
    ├── EntityDataSourcePlugin.cs         ← list_entity_data_sources, create_entity_*
    ├── TransactionBuilderPlugin.cs       ← create_application, create_search_view, create_*_from_*, create_list_edit_form
    ├── TransactionModifierPlugin.cs      ← update_transaction_field, set_field_entity, delete_transaction
    ├── SchemaBuilderPlugin.cs            ← get_table_schema, create_database_table
    ├── SchemaAlterPlugin.cs              ← alter_table
    ├── DataQueryPlugin.cs                ← execute_sql, check_table_exists, insert_mockup_data
    ├── SearchBuilderPlugin.cs            ← create_search, list_searches
    └── MemorySearchPlugin.cs             ← search_memory (RAG over build history / notes)

APP.Components.Dto/UserDefine/AppBuilderAgent/
└── AppBuilderAgentDto.cs                 ← all request / response / event DTOs + AgentCallbacks

AppAI.Web/Controllers/
└── AppBuilderAgentController.cs          ← RunAgent, PollEvents, ConfirmPlan, ConfirmSchema,
                                             RecentSessions, GetSession, DeleteSession

AppAI.Web/Hubs/
└── AppBuilderAgentHub.cs                 ← SignalR hub (GetConnectionId only; not used by controller)

AppReact/src/webapi/
└── appbuilderagentsvc.ts                 ← polling client + RunAgent / ConfirmPlan / ConfirmSchema calls

AppReact/src/components/integration/
└── AppBuilderAgent.tsx                   ← chat UI component

memory/appbuilder/                        ← runtime files, not in source control
├── platform-state.md
├── build-history.md
└── agent-notes.md
```

---

## 13. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **No Microsoft.SemanticKernel** | SK 1.70+ requires Azure.AI.OpenAI ≥ 2.8.0-beta, incompatible with .NET 4.6.2. `[AgentFunction]` + `System.Reflection` achieve the same plugin pattern with zero external dependencies. |
| **HTTP polling over SignalR push** | Polling (`ConcurrentQueue` + `GET /PollEvents`) works across all deployment environments without WebSocket config. SignalR hub exists but is not used by the current controller. |
| **AgentCallbacks delegates** | Keeps BL infrastructure-free. Transport (queue vs SignalR) stays in the web layer. BL is testable without a running HTTP server. |
| **Fire-and-forget `Task.Run`** | Agent runs take 30–180 seconds. Controller returns immediately with `SessionId`; client drives progress via polling. |
| **Raw `HttpClient`** | Direct HTTP to all three LLM APIs. No SDK dependency. Provider-specific parsing in three separate `CallXxxWithToolsAsync` methods. |
| **RAG memory (`search_memory`)** | Memory not pre-loaded into system prompt. Agent fetches on demand. Saves ~2,000–2,500 tokens per call on sessions where history is irrelevant. |
| **Gate tool pruner protection** | `propose_plan`, `propose_schema`, `execute_approved_schema`, `confirm_drop_tables` messages are never evicted from the sliding window. Evicting them would cause the LLM to re-invoke approval dialogs. |
| **Tool result cap (10,000 chars)** | Prevents a single large result (schema dump, entity list) from consuming the entire context. LLM receives a truncation notice and can request specific details. |
| **Sliding-window pruner (120k token budget)** | Drops oldest non-gate turn pairs when context grows too large. `messages[0]` (original user request) always preserved. |
| **Conversation history trim (4 turns)** | On multi-turn sessions resumed from DB, limits pre-seeded history to avoid blowing the budget before the first tool call. |
| **Two TCS gate patterns** | `propose_plan` uses `TCS<bool>` (approved/rejected). `propose_schema` uses `TCS<AgentSchemaResponse>` (includes user-edited schema JSON + feedback). Both gates suspend the agentic loop without blocking a thread-pool thread. |
| **`SELECT`-only guard in `execute_sql`** | Prevents the agent from running destructive SQL through the verification tool. All DDL goes through `create_database_table`, `alter_table`, or `execute_approved_schema`. |
| **Configurable limits via `web.config`** | `Agent.MaxIterations`, `Agent.TokenBudget`, `Agent.MaxToolResultChars`, `Agent.MaxHistoryTurns` — all tunable without recompile. |
| **Resumable checkpoint** | `AgentCheckpointDto` persisted to DB after every run. System prompt's RESUME RULE lets the LLM skip already-completed steps when the user re-submits after an interruption. |
| **Auto-create session table** | `dbo.AppBuilderAgentSession` is created by `AppBuilderAgentSessionBL.EnsureTable()` on first use — no migration script required. |
