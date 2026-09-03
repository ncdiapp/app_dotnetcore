# Generic AI Agent — Execution Plan

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-02  
**Status:** Design Complete — Pending Implementation

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
POST /webapi/GenericAgent/Run   POST /webapi/AppBuilderAgent/RunAgent  (routes unchanged)
        │                                              │
        └──────────────────────┬───────────────────────┘
                               ▼
                   GenericAgentBL.RunAsync(skillKey, ...)
                               │
                   1. Load SkillSet config  (AppAgentSkillSet)
                   2. Read system prompt    (AppAgentSkillSet.SystemPrompt)
                   3. Load manual tools     (AppAgentToolEngine → 6 ToolTypes)
                   4. Connect MCP servers   (AppAgentMcpServer → McpClient → KernelFunctions)
                   5. Run SK agentic loop
                               │
                        GenericAgentEngine
                               │
          ┌────────────────────┼────────────────────┐
   KernelFunction        KernelFunction        KernelFunction
   (BuiltIn/SqlQuery/…)  (MCP auto-discovered)  (DynamicCSharp)
                               │
                      SignalR stream events
                               │
                      <GenericAgentChat />   ←── MCPAppRenderer (9 UI components)
```

---

## 5. DB Schema Design

### 5.1 `AppAgentSkillSet` — Agent Persona Registry (NEW)

```sql
CREATE TABLE dbo.AppAgentSkillSet (
    SkillKey           NVARCHAR(100) NOT NULL PRIMARY KEY,
    DisplayName        NVARCHAR(200) NOT NULL,
    Description        NVARCHAR(MAX) NULL,
    SystemPrompt       NVARCHAR(MAX) NULL,      -- full agent system prompt; migrated from hardcoded BL text
    CapabilityFlags    INT  NOT NULL DEFAULT 0,
    IsActive           BIT  NOT NULL DEFAULT 1,
    SortOrder          INT  NOT NULL DEFAULT 0,
    Version            INT  NOT NULL DEFAULT 1,
    MaxHistoryTokens   INT  NOT NULL DEFAULT 80000,   -- prune when history exceeds this
    SummarizeThreshold INT  NOT NULL DEFAULT 60000,   -- 0 = summarization off
    MaxToolResultChars INT  NOT NULL DEFAULT 4000,    -- cap per tool result string
    RecentWindowSize   INT  NOT NULL DEFAULT 10       -- always keep last N turns unpruned
);

-- Seed the 4 existing agents (SystemPrompt content migrated from hardcoded BL source — see actual SQL file)
INSERT INTO dbo.AppAgentSkillSet
    (SkillKey, DisplayName, SystemPrompt, CapabilityFlags, MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize)
VALUES
('app-builder',      'App Builder Agent',      N'<migrated from AppBuilderAgentBL>',           31, 80000, 60000, 4000, 10),
('app-report',       'App Report Agent',        N'<migrated from AppReportAgentBL>',             3, 40000,     0, 2000, 10),
('db-genie',         'DB Genie',                N'<migrated from sqlskill.md>',                 35, 40000,     0, 8000, 10),
('data-integration', 'Data Integration Agent',  N'<migrated from AppDataIntegrationAgentSkillBL>', 65, 20000, 0, 2000, 10);
```

### 5.2 `CapabilityFlags` Enum

```csharp
[Flags]
public enum AgentCapabilityFlags
{
    None            = 0,
    StreamTokens    = 1,   // stream tokens via SignalR
    MultiTurn       = 2,   // keep conversation history
    PlanGate        = 4,   // pause on propose_plan → wait for ConfirmPlan
    SchemaGate      = 8,   // pause on propose_schema → wait for approval
    InjectMemory    = 16,  // prepend AppBuilderAgentMemoryBL.SearchMemory() to prompt
    InjectSchema    = 32,  // prepend DB schema summary to prompt
    ExternalBackend = 64,  // skip SK loop → delegate to Cursor BL
}
```

**End-user exposure:** flags are NOT shown as raw numbers. New SkillSet wizard uses guided behavior questions:

| Question shown to user | Flag(s) set |
|---|---|
| "Propose changes that need approval before executing?" | `PlanGate + SchemaGate` |
| "Remember context from previous sessions?" | `InjectMemory` |
| "Include database schema information automatically?" | `InjectSchema` |
| "Stream replies word-by-word as they generate?" | `StreamTokens` |
| "Maintain conversation history across messages?" | `MultiTurn` |
| "Delegate entirely to an external backend?" | `ExternalBackend` |

Built-in personas are read-only in the UI (flags visible but not editable).

### 5.3 `AppAISkill` — Not Changed

`AppAISkill` is **unchanged by this refactor**. It remains a separate user-created custom prompt library — unrelated to agent personas.

| Current use | After refactor |
|---|---|
| `AppBuilderAgentBL` hardcoded system prompt | Migrated into `AppAgentSkillSet.SystemPrompt` for `'app-builder'` |
| `AppReportAgentBL` hardcoded prompt | Migrated into `AppAgentSkillSet.SystemPrompt` for `'app-report'` |
| `DbGenie` `sqlskill.md` | Migrated into `AppAgentSkillSet.SystemPrompt` for `'db-genie'` |
| Existing `AppAISkill` rows (user-created) | Keep as-is — separate non-agent feature |

No DDL changes. No new columns. No seed data. `AppAISkillBL.cs` and `AISkillManagement.tsx` remain on their current flat layout.

### 5.4 `AppAgentToolRegister` — Tool Registry (NEW)

```sql
CREATE TABLE dbo.AppAgentToolRegister (
    ToolRegisterId      INT IDENTITY PRIMARY KEY,
    SkillKey            NVARCHAR(100) NOT NULL,
    ToolName            NVARCHAR(200) NOT NULL,    -- LLM-facing name
    ToolDescription     NVARCHAR(MAX) NULL,        -- LLM-facing description
    ParameterSchemaJson NVARCHAR(MAX) NULL,        -- JSON Schema for LLM
    ToolType            NVARCHAR(50)  NOT NULL DEFAULT 'BuiltIn',
    ToolConfig          NVARCHAR(MAX) NULL,        -- JSON, shape varies by ToolType
    IsActive            BIT NOT NULL DEFAULT 1
);
```

### 5.5 `AppAgentMcpServer` — MCP Server Registry (NEW)

MCP servers expose their tool list dynamically — registering each MCP tool individually in `AppAgentToolRegister` would defeat the purpose. Instead, register the server once and let SK auto-discover all its tools at session start.

```sql
CREATE TABLE dbo.AppAgentMcpServer (
    McpServerId   INT IDENTITY PRIMARY KEY,
    SkillKey      NVARCHAR(100) NOT NULL,    -- FK to AppAgentSkillSet
    ServerName    NVARCHAR(200) NOT NULL,    -- display name, used as SK plugin group name
    ServerType    NVARCHAR(50)  NOT NULL,    -- 'streamable-http' | 'stdio'
    ServerUrl     NVARCHAR(500) NULL,        -- SSE: HTTP endpoint URL
    Command       NVARCHAR(500) NULL,        -- stdio only: executable + args
    IsActive      BIT NOT NULL DEFAULT 1
);

-- Example: app-builder connects to BlueCherry MCP server
INSERT INTO AppAgentMcpServer (SkillKey, ServerName, ServerType, ServerUrl) VALUES
('app-builder', 'BlueCherry ERP MCP', 'streamable-http', 'http://localhost:5100/mcp');
```

How `GenericAgentEngine` loads MCP tools alongside manual tools:

```csharp
// Step 3 + 4 in GenericAgentBL.RunAsync
var manualTools = await AppAgentToolEngine.LoadToolsForSkillAsync(skillKey, ctx);
kernel.Plugins.AddFromFunctions("agent_tools", manualTools);

var mcpServers = AppAgentMcpServerBL.GetActiveBySkillKey(skillKey);
foreach (var server in mcpServers)
{
    // Use StreamableHttp transport — matches BC-MCP-Client McpPluginFactory.cs pattern
    // NOTE: SseClientTransport is NOT used; AsKernelFunction() is also broken due to
    // Microsoft.Extensions.AI version mismatch — tools must be manually wrapped.
    // ServerType: 'streamable-http' (HTTP-based MCP) or 'stdio' (local process)
    var transport = server.ServerType == "streamable-http"
        ? (ITransport)new HttpClientTransport(new HttpClientTransportOptions
          {
              Url = server.ServerUrl,
              TransportMode = HttpTransportMode.StreamableHttp
          })
        : new StdioClientTransport(new StdioClientTransportOptions
          {
              Command = server.Command!.Split(' ')[0],
              Arguments = server.Command.Split(' ').Skip(1).ToArray()
          });
    var mcpClient = await McpClient.CreateAsync(transport);
    var tools = await mcpClient.ListToolsAsync();

    // Manual wrap — mirrors McpPluginFactory.BuildKernelFunction() in BC-MCP-Client
    var functions = tools.Select(tool => KernelFunctionFactory.CreateFromMethod(
        async (KernelArguments args, CancellationToken ct) =>
        {
            var result = await mcpClient.CallToolAsync(tool.Name, MapArgs(args), ct);
            var text = string.Join("", result.Content.OfType<TextContentBlock>().Select(b => b.Text));
            return text.Length > persona.MaxToolResultChars
                ? text[..persona.MaxToolResultChars] + "\n[...truncated]"
                : text;
        },
        functionName: tool.Name,
        description: tool.Description,
        parameters: ParseToolSchema(tool.JsonSchema))).ToList();

    var plugin = KernelPluginFactory.CreateFromFunctions("mcp_" + server.ServerName, functions);
    kernel.Plugins.Add(plugin);
}
```

`ModelContextProtocol` NuGet (v1.2.0) provides `McpClient`, `HttpClientTransport`, `HttpClientTransportOptions`, `HttpTransportMode` — no additional NuGet required.  
See `BC-MCP-Client\server\McpChatAgent.Api\Services\McpPluginFactory.cs` for the complete `BuildKernelFunction` and `ParseToolSchema` implementations to copy.

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

### ToolConfig JSON shapes

**BuiltIn:**
```json
{ "TypeName": "App.BL.AppBuilderAgent.Plugins.SchemaBuilderPlugin", "MethodName": "GetTableSchema" }
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

### Strategy pattern in `AppAgentToolEngine`

```csharp
IToolExecutor executor = reg.ToolType switch
{
    "BuiltIn"       => new BuiltInToolExecutor(reg, ctx),
    "ExternalDll"   => new ExternalDllToolExecutor(reg, ctx),
    "SqlQuery"      => new SqlQueryToolExecutor(reg, ctx),
    "PowerShell"    => new PowerShellToolExecutor(reg, ctx),
    "HttpRest"      => new HttpRestToolExecutor(reg, ctx),
    "DynamicCSharp" => new DynamicCSharpToolExecutor(reg, ctx),
    _ => throw new NotSupportedException(reg.ToolType)
};
```

---

## 7. SkillSet Management UI

`AppAISkillBL.cs` and `AISkillManagement.tsx` are **not changed** — they remain the non-agent prompt library.

New management lives in a dedicated screen for `AppAgentSkillSet`:

### New BL class — `AppAgentSkillSetBL.cs`

```csharp
GetAllSkillSets(int dsId)                           // list for admin UI
GetSkillSetByKey(int dsId, string skillKey)         // load one for editing
CreateSkillSet(int dsId, AppAgentSkillSetDto dto)   // wizard step 2 save
UpdateSkillSet(int dsId, AppAgentSkillSetDto dto)   // inline edit
DeleteSkillSet(int dsId, string skillKey)           // disable only (IsActive=0)
```

### New controller endpoints (new `AgentSkillSetController.cs` or extend existing admin controller)

```
GET  GetAllSkillSets(dataSourceId)
GET  GetSkillSetByKey(dataSourceId, skillKey)
POST CreateSkillSet(dataSourceId, dto)
POST UpdateSkillSet(dataSourceId, dto)
POST DeleteSkillSet(dataSourceId, skillKey)
```

### React UI — `AgentSkillSetManagement.tsx` (NEW)

**Single-level layout — one row per agent persona:**

```
┌───────────────────────────────────────────────────────────────────────────────┐
│  Agent Personas                                                    [+ New]      │
│  ─────────────────────────────────────────────────────────────────────────── │
│  SkillKey         │ Display Name            │ Flags                │ Actions  │
│  app-builder      │ App Builder Agent       │ Stream MultiTurn ... │ Edit ▶   │
│  app-report       │ App Report Agent        │ Stream MultiTurn     │ Edit ▶   │
│  db-genie         │ DB Genie                │ Stream Schema        │ Edit ▶   │
│  data-integration │ Data Integration Agent  │ Stream External      │ Edit ▶   │
└───────────────────────────────────────────────────────────────────────────────┘
```

Editor panel: `SkillKey` (read-only after create), `DisplayName`, `Description`, `SystemPrompt` (large textarea), behavior checkboxes (wizard flags), context threshold fields (`MaxHistoryTokens` etc.), `IsActive`.

**New SkillSet wizard — guided behavior questions (no raw flag numbers shown):**

```
Create New Agent Persona                                          Step 1 of 2
────────────────────────────────────────────────────────────────────────────
Agent Key    [_______________]   Display Name  [___________________________]

How does this agent behave?

☐  Propose changes that need approval before executing
   └─ Agent pauses and asks "shall I proceed?" on plans and schema changes

☐  Remember context from previous sessions
   └─ Agent reads past conversation memory before each reply

☐  Include database schema information automatically
   └─ Agent gets table/column definitions injected into its prompt

☐  Stream replies word-by-word as they are generated (recommended)
   └─ Responses appear live via SignalR

☐  Maintain conversation history across messages (recommended)
   └─ Agent remembers earlier messages in the same session

☐  Delegate entirely to an external backend
   └─ Forwards requests to an external API instead of built-in AI loop

                                               [Cancel]  [Next: Write Prompt →]
```

Step 2: write the system prompt. On save: creates one `AppAgentSkillSet` row with `SystemPrompt` set. No `AppAISkill` row created.

---

## 8. GenericAgentBL — Core Entry Point

```csharp
public static class GenericAgentBL
{
    public static async Task<string> RunAsync(
        string skillKey, string userMessage, AgentContext ctx, GenericAgentCallbacks callbacks)
    {
        // 1. Load persona config
        var persona = AppAgentSkillSetBL.GetByKey(skillKey)
            ?? throw new InvalidOperationException($"Unknown skillKey: {skillKey}");

        // 2. Read system prompt directly from persona
        var systemPrompt = BuildSystemPrompt(userMessage, persona);

        // 3. Load tools (all ToolTypes resolved by AppAgentToolEngine)
        var tools = await AppAgentToolEngine.LoadToolsForSkillAsync(skillKey, ctx);

        // 4. Run SK loop (or delegate if ExternalBackend flag set)
        var session = GenericAgentSessionStore.CreateOrGet(ctx.SessionId);
        await GenericAgentEngine.RunAsync(persona, systemPrompt, tools, userMessage, session, callbacks);
        return session.SessionId;
    }

    private static string BuildSystemPrompt(string userMessage, AppAgentSkillSetDto persona)
    {
        var flags = (AgentCapabilityFlags)persona.CapabilityFlags;

        // System prompt stored directly in AppAgentSkillSet.SystemPrompt
        var prompt = persona.SystemPrompt ?? "";

        // Memory injection (cross-session RAG)
        if (flags.HasFlag(AgentCapabilityFlags.InjectMemory))
        {
            var memory = AppBuilderAgentMemoryBL.SearchMemory(userMessage, maxSections: 5);
            if (!string.IsNullOrEmpty(memory))
                prompt += $"\n\n━━━ MEMORY CONTEXT ━━━\n{memory}";
        }

        return prompt;
    }
}
```

### Controller changes (routes unchanged)

```csharp
// AppBuilderAgentController — after
[HttpPost("RunAgent")]
public async Task<IActionResult> RunAgent([FromBody] AppBuilderAgentRequestDto req)
    => Ok(await GenericAgentBL.RunAsync("app-builder", req.UserRequest, ctx, callbacks));

[HttpPost("ConfirmPlan")]
public IActionResult ConfirmPlan([FromBody] ConfirmDto dto)
{ GenericAgentSessionStore.ResolvePlanGate(dto.SessionId, dto.Approved); return Ok(); }

// AppReportAgentController — after
[HttpPost("RunAgent")]
public async Task<IActionResult> RunAgent([FromBody] AppReportAgentRequestDto req)
    => Ok(await GenericAgentBL.RunAsync("app-report", req.UserRequest, ctx, callbacks));

// DbGenieController — after
[HttpPost("Chat")]
public async Task<IActionResult> Chat([FromBody] DbGenieChatRequestDto req)
    => Ok(await GenericAgentBL.RunAsync("db-genie", req.UserMessage, ctx, callbacks));
```

---

## 9. MCP Data Rendering — `MCPAppRenderer`

BC-MCP-Client's `client/src/mcp-components/` folder contains 9 actively-used React components that render structured MCP tool results. All are wired and in production — zero dead code.

### Rendering Chain

```
MessageItem.tsx
  └── <MCPAppRenderer toolResult={...} />          ← single entry point
        ├── reads toolResult.ui_hint
        ├── if absent → detectUiHint() infers by data shape
        │     Dashboard > ActionMenu > Form > RecordCard > PivotTable > ChartView > FlexGrid
        ├── runs adapter(toolResult) → normalises data
        └── renders matched component inside <Suspense>
```

`MCPAppRenderer` is triggered in `MessageItem.tsx` in two ways:
1. `role === 'tool'` message with structured `toolResult` JSON attached
2. Markdown code fence tagged `` ```mcp-ui `` or `` ```mcp `` — body is JSON-parsed

### 9 Active Components

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

Agent columns auto-derived from first-row keys when `columns` is absent (BlueCherry naming rules: `_date` → date, `net_price` → currency, `_code` → hidden).

### Teaching the Agent to Emit `ui_hint` JSON

Add rendering instructions directly inside the `AppAgentSkillSet.SystemPrompt` for each SkillSet that returns data:

```markdown
## Rendering Data Tables
When a tool returns a list of rows, format the result as:
```mcp-ui
{ "ui_hint": "FlexGrid", "data": [...], "columns": [...], "meta": { "title": "..." } }
```
The frontend will render this as an interactive grid with export.
```

This block is part of the stored `SystemPrompt` — no separate `AppAISkill` row needed.

### Integration in App-netore

Copy (or npm-link) `mcp-components/` from BC-MCP-Client into `AppReact/src/`.  
Import in the new `GenericAgentChat.tsx`:

```tsx
import { MCPAppRenderer } from '../mcp-components';

// In message renderer:
{msg.toolResult
    ? <MCPAppRenderer toolResult={msg.toolResult} onAction={handleMcpAction} />
    : <MarkdownRenderer>{msg.content}</MarkdownRenderer>
}
```

---

## 10. Launching GenericAgent from SkillSet Management

Admins can test any SkillSet directly from the management UI without a dedicated page per agent.

### New Generic API Endpoint

```csharp
// AppAI.Web/Controllers/GenericAgentController.cs  (NEW)
[HttpPost("Run")]
public async Task<IActionResult> Run([FromBody] GenericAgentRunDto request)
    => Ok(await GenericAgentBL.RunAsync(request.SkillKey, request.UserMessage, ctx, callbacks));

[HttpPost("ConfirmGate")]
public IActionResult ConfirmGate([FromBody] ConfirmGateDto dto)
{ GenericAgentSessionStore.ResolvePlanGate(dto.SessionId, dto.Approved); return Ok(); }

public class GenericAgentRunDto
{
    public string  SkillKey    { get; set; }
    public string  UserMessage { get; set; }
    public string? SessionId   { get; set; }  // null = start new session
}
```

### `<GenericAgentChat />` — Reusable Chat Component

```tsx
// AppReact/src/components/aiskill/GenericAgentChat.tsx
// Props: skillKey, title (optional), sessionId (optional)
<GenericAgentChat skillKey="app-builder" title="App Builder Agent" />
```

SignalR stream event types the component handles:

| Event type | Action |
|---|---|
| `token` | Append text to current message bubble |
| `tool` | Show tool-call chip (name + args) |
| `gate` | Show **Approve / Reject** buttons; POST `/ConfirmGate` on click |
| `mcp_ui` | Render `<MCPAppRenderer toolResult={...} />` |
| `done` | Save `sessionId` for next turn (multi-turn) |
| `error` | Show error banner |

### SkillSet Management Layout with Run Button

```
┌──────────────────────────┬──────────────────────────────────────────────────────────┐
│  Agent Personas           │  ▶ Testing: App Builder Agent                      [✕]  │
│  ───────────────────────  │  ──────────────────────────────────────────────────────  │
│  App Builder Agent [▶Run] │                                                          │
│  App Report Agent  [▶Run] │  [agent streaming response + MCPAppRenderer renders]     │
│  DB Genie          [▶Run] │                                                          │
│  Data Integration  [▶Run] │  ──────────────────────────────────────────────────────  │
│                           │  Type a message...                          [Send]        │
│  [+ New Persona]          │                                                          │
└──────────────────────────┴──────────────────────────────────────────────────────────┘
```

Implementation in `AgentSkillSetManagement.tsx`:

```tsx
const [testSkillKey, setTestSkillKey] = useState<string | null>(null);

// In persona list row:
<button onClick={() => setTestSkillKey(row.SkillKey)}>▶ Run</button>

// Right drawer:
{testSkillKey && (
    <RightDrawer title={`Testing: ${testSkillKey}`} onClose={() => setTestSkillKey(null)}>
        <GenericAgentChat skillKey={testSkillKey} />
    </RightDrawer>
)}
```

### `<GenericAgentChat />` Is Reusable Everywhere

| Launch point | Usage |
|---|---|
| SkillSet management | Right drawer — admin test mode |
| Application home page | Floating button → chat drawer |
| Dedicated route `/agent-chat?skillKey=db-genie` | Full-page layout |
| Embedded in any form page | Side panel alongside the form |

The component only needs `skillKey` — all configuration (prompt, tools, flags, MCP servers) comes from the DB.

---

## 11. NuGet Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.SemanticKernel` | 1.74.0 | SK agentic loop — native `net10.0` build confirmed |
| `Microsoft.SemanticKernel.Agents.Core` | 1.74.0 | `ChatCompletionAgent` |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | 1.74.0 | OpenAI / Azure OpenAI provider |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | latest stable | `DynamicCSharp` ToolType — Roslyn sandbox |
| `ModelContextProtocol` | 1.2.0 | MCP client — `McpClient`, `HttpClientTransport`, `HttpTransportMode.StreamableHttp`; **do NOT use `AsKernelFunction()`** — broken due to `Microsoft.Extensions.AI` version conflict; wrap tools manually via `KernelFunctionFactory.CreateFromMethod` |
| `Anthropic` | 12.42.0 | Official Anthropic SDK (not community `Anthropic.SDK`) |

> **Note:** BC-MCP-Client switched from community `Anthropic.SDK` to official `Anthropic` v12.42.0.  
> Use the official package — it is maintained by Anthropic directly and has no `Microsoft.Extensions.AI.Abstractions` version conflict.

---

## 12. Long Session Context Management

### What Causes Context Overflow

In a multi-turn agent session, tokens accumulate from four sources:

```
System prompt         2K–8K   (fixed per SkillSet)
Conversation history  grows with every turn
Tool call records     [name + args] per call
Tool results          10K+ per call (SQL dumps, MCP data payloads)
Memory injection      1K–3K   (prepended each turn when InjectMemory set)
```

A 10-turn AppBuilder session with heavy schema work easily reaches 80K–120K tokens.

### Four-Level Strategy

#### Level 1 — Token Budget + Pruning (always on)

Sliding window: always keep system prompt + first 2 turns (initial intent) + last N turns. Prune from the middle.

```csharp
// GenericAgentEngine — runs before every LLM call
PruneHistory(session.History, persona.MaxHistoryTokens, persona.RecentWindowSize);

// Tool results capped before being added to history
var cappedResult = result.Length > persona.MaxToolResultChars
    ? result[..persona.MaxToolResultChars] + $"\n[...truncated — {result.Length} chars total]"
    : result;
```

#### Level 2 — Summarization (for long sessions)

When token count exceeds `SummarizeThreshold`, call the LLM to compress the oldest messages into a single summary block — don't discard them:

```csharp
if (persona.SummarizeThreshold > 0
    && CountTokens(session.History) > persona.SummarizeThreshold)
{
    var oldMessages = session.History.Take(session.History.Count - persona.RecentWindowSize);
    var summary = await kernel.InvokePromptAsync(
        $"Summarize this conversation so far in under 500 words, preserving key decisions and facts:\n"
        + FormatMessages(oldMessages));

    session.History.RemoveRange(0, oldMessages.Count);
    session.History.Insert(0, new ChatMessageContent(AuthorRole.System,
        $"[Conversation summary — earlier turns]\n{summary}"));
}
```

Cost: one extra LLM call per summarization event (rare — only when threshold is crossed).

#### Level 3 — Cross-Session RAG Memory (already planned)

`InjectMemory` capability flag retrieves relevant facts from **past sessions** via `AppBuilderAgentMemoryBL.SearchMemory()`. This is RAG — semantic/keyword search over a persistent memory store, injected at session start. Solves long-term context across sessions, not within a single session.

#### Level 4 — Knowledge Base RAG (optional)

For SkillSets that reason over large corpora (500+ DB tables, large product docs), vector-search the relevant chunks instead of injecting the full corpus. `InjectSchema` flag is a simple version of this — for very large schemas, upgrade to a vector retrieval over `AppEntitySchema`.

### Do We Need a Vector DB?

| Use case | Solution | Vector DB needed? |
|---|---|---|
| In-session history overflow | Level 1 pruning + Level 2 summarization | **No** |
| Cross-session memory | Level 3 RAG via `AppBuilderAgentMemoryBL` | Depends on existing impl |
| Large schema / doc corpus | Level 4 knowledge RAG | Only if corpus > 20K tokens |
| Tool result overflow | Hard cap at `MaxToolResultChars` | **No** |

**Conclusion:** RAG is already in the design via `InjectMemory`. In-session overflow is solved by pruning + summarization with no vector DB required.

### Per-SkillSet Context Config

Four threshold columns are part of the `AppAgentSkillSet` CREATE TABLE (§5.1) — editable per agent in the admin UI without code changes.

Recommended values per agent:

| SkillSet | MaxHistory | Summarize | MaxToolResult | Why |
|---|---|---|---|---|
| `app-builder` | 80K | 60K | 4K | Long multi-step sessions; benefits from summarization |
| `db-genie` | 40K | 0 | 8K | Shorter sessions; SQL results need more chars |
| `app-report` | 40K | 0 | 2K | Single-turn reports; no summarization needed |
| `data-integration` | 20K | 0 | 2K | Delegates to external backend; minimal history |

These columns are editable in the admin UI's Agent Personas screen — no code change to adjust thresholds.

---

## 13. File Map

### Files to CREATE

| File | Purpose |
|---|---|
| `doc-deploy/AppAgentSkillSet_Create.sql` | Persona table DDL + 4 seed rows (with `SystemPrompt` populated from migrated BL text) |
| `doc-deploy/AppAgentToolRegister_Create.sql` | Tool table DDL + ~31 built-in seed rows |
| `APP.Framework/Plugin/IAgentTool.cs` | External DLL contract |
| `APP.Framework/Plugin/AgentToolAttribute.cs` | Replaces `[AgentFunction]`/`[AgentParam]` |
| `APP.BL/TenantBusiness/AppAgentToolEngine.cs` | Strategy dispatcher |
| `APP.BL/TenantBusiness/AppAgentToolRegisterBL.cs` | CRUD over AppAgentToolRegister |
| `APP.BL/TenantBusiness/AgentToolExecutors/BuiltInToolExecutor.cs` | |
| `APP.BL/TenantBusiness/AgentToolExecutors/ExternalDllToolExecutor.cs` | |
| `APP.BL/TenantBusiness/AgentToolExecutors/SqlQueryToolExecutor.cs` | |
| `APP.BL/TenantBusiness/AgentToolExecutors/PowerShellToolExecutor.cs` | |
| `APP.BL/TenantBusiness/AgentToolExecutors/HttpRestToolExecutor.cs` | |
| `APP.BL/TenantBusiness/AgentToolExecutors/DynamicCSharpToolExecutor.cs` | Roslyn sandbox |
| `APP.BL/AIAgent/GenericAgent/GenericAgentBL.cs` | Main entry point |
| `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs` | SK agentic loop |
| `APP.BL/AIAgent/GenericAgent/GenericAgentSession.cs` | Unified session model |
| `APP.BL/AIAgent/GenericAgent/GenericAgentSessionStore.cs` | Replaces 3 stores |
| `APP.BL/AIAgent/GenericAgent/GenericAgentCallbacks.cs` | SignalR streaming |
| `APP.BL/AIAgent/GenericAgent/KernelProviderHelper.cs` | SK kernel factory |
| `APP.BL/AIAgent/AiSkill/AppAgentSkillSetBL.cs` | SkillSet CRUD |
| `APP.BL/AIAgent/AiSkill/AppAgentMcpServerBL.cs` | MCP server registry CRUD |
| `doc-deploy/AppAgentMcpServer_Create.sql` | MCP server table DDL |
| `AppAI.Web/Controllers/GenericAgentController.cs` | Generic `Run` + `ConfirmGate` endpoints |
| `AppAI.Web/Controllers/AgentSkillSetController.cs` | Persona CRUD endpoints |
| `AppReact/src/webapi/genericAgentSvc.ts` | SignalR subscription + `Run`/`ConfirmGate` calls |
| `AppReact/src/webapi/agentSkillSetSvc.ts` | `AppAgentSkillSetDto` interface + persona CRUD service methods |
| `AppReact/src/components/aiskill/GenericAgentChat.tsx` | Reusable chat component (streaming + gates + MCPAppRenderer) |
| `AppReact/src/components/aiskill/AgentSkillSetManagement.tsx` | Persona grid + editor panel + behavior wizard + `[▶ Run]` button |
| `AppReact/src/mcp-components/` (copy from BC-MCP-Client) | 9 MCP render components + adapters + registry |

### Files to MODIFY

| File | Change |
|---|---|
| `APP.Components.Dto/UserDefine/AISkill/AppAISkillDto.cs` | Add new `AppAgentSkillSetDto` class only (`AppAISkillDto` itself unchanged) |
| `AppAI.Web/Controllers/AppBuilderAgentController.cs` | Body → `GenericAgentBL.RunAsync("app-builder",...)` |
| `AppAI.Web/Controllers/AppReportAgentController.cs` | Body → `GenericAgentBL.RunAsync("app-report",...)` |
| `AppAI.Web/Controllers/DbGenieController.cs` | Body → `GenericAgentBL.RunAsync("db-genie",...)` |
| `APP.BL/AIAgent/AppBuilderAgent/Plugins/*.cs` (12 files) | `[AgentFunction]` → `[AgentTool]` attribute only |
| `APP.BL/AIAgent/AppReportAgent/Plugins/*.cs` (3 files) | Same attribute migration |
| `APP.BL/APP.BL.csproj` | Add `Microsoft.SemanticKernel` + `Microsoft.CodeAnalysis.CSharp.Scripting` + `ModelContextProtocol` NuGet |

### Files to DELETE

| File | Reason |
|---|---|
| `APP.BL/AIAgent/AppBuilderAgent/AppBuilderAgentBL.cs` | Replaced by GenericAgentBL |
| `APP.BL/AIAgent/AppReportAgent/AppReportAgentBL.cs` | Replaced by GenericAgentBL |
| `APP.BL/AIAgent/DbGenie/AppDbGenieBL.cs` | Replaced by GenericAgentBL |
| `APP.BL/AIAgent/AppBuilderAgent/AgentFunctionAttribute.cs` | Replaced by AgentToolAttribute |
| `APP.BL/AIAgent/DbGenie/LLMProviderHelper.cs` | Promoted to KernelProviderHelper |
| 3× separate `*AgentSessionStore.cs` files | Replaced by GenericAgentSessionStore |

---

## 14. Implementation Phases

### Phase 0 — DB Migration (prerequisite for all phases)

1. Run `AppAgentSkillSet_Create.sql` — create table, seed 4 agent personas with `SystemPrompt` populated from migrated BL text
2. Run `AppAgentToolRegister_Create.sql` — create table, seed ~31 built-in tool rows
3. Run `AppAgentMcpServer_Create.sql` — create table (seed example row optional)

**`AppAISkill` is not touched.** No migration SQL for it.

**Verify:** 4 rows in `AppAgentSkillSet` (each with non-null `SystemPrompt`); ~31 rows in `AppAgentToolRegister`; `AppAgentMcpServer` table exists

### Phase 1 — APP.Framework Extensions

Create `IAgentTool.cs` and `AgentToolAttribute.cs` in `APP.Framework/Plugin/`.  
No changes to existing `IAppPlugin.cs` or `PluginContext.cs`.

### Phase 2 — Plugin Attribute Migration

Replace `[AgentFunction]` + `[AgentParam]` with `[AgentTool]` on all 15 plugin files.  
**Plugin body: unchanged.** Delete `AgentFunctionAttribute.cs`.

### Phase 3 — AppAgentToolEngine + Executors

Create `AppAgentToolEngine.cs`, `AppAgentToolRegisterBL.cs`, and 6 executor files in `AgentToolExecutors/`.  
Add NuGet: `Microsoft.CodeAnalysis.CSharp.Scripting`.

### Phase 4 — GenericAgent Infrastructure

Create 6 files in `APP.BL/AIAgent/GenericAgent/`.  
Create `APP.BL/AIAgent/AiSkill/AppAgentMcpServerBL.cs` (required by `GenericAgentBL.RunAsync` — loads MCP server configs).  
Create `AppAI.Web/Controllers/GenericAgentController.cs` (generic `Run` + `ConfirmGate` endpoints for admin test UI).  
Add NuGet: `Microsoft.SemanticKernel`.  
Reference BC-MCP-Client patterns:
- `KernelBuilderService.cs` → `KernelProviderHelper` (multi-provider kernel factory)
- `McpPluginFactory.cs` → MCP tool loading (`McpClient.CreateAsync`, `HttpClientTransport`, manual `KernelFunctionFactory.CreateFromMethod` wrap — **do NOT use `AsKernelFunction()`**)
- `ConversationGrain.cs` → `GenericAgentEngine` (SK loop, history summarization, `IFunctionInvocationFilter` pipeline)

### Phase 5 — SkillSet Admin Layer

Add `AppAgentSkillSetDto` to `AppAISkillDto.cs` (existing file, new class only).  
Create `AppAgentSkillSetBL.cs` (CRUD for `AppAgentSkillSet`).  
Create `AgentSkillSetController.cs` (REST endpoints for persona management).  
Create `agentSkillSetSvc.ts` (TypeScript service + `AppAgentSkillSetDto` interface).  
Create `genericAgentSvc.ts` (SignalR subscription + `Run`/`ConfirmGate` client calls — required by `GenericAgentChat.tsx`).  
Create `AgentSkillSetManagement.tsx` (persona grid + editor + behavior wizard + `[▶ Run]` button).  
Create `GenericAgentChat.tsx` (reusable streaming chat component + `MCPAppRenderer` integration).  
Copy `mcp-components/` from BC-MCP-Client into `AppReact/src/`.

### Phase 6 — Controller Updates + Cleanup

Update 3 controllers → `GenericAgentBL.RunAsync(skillKey, ...)`.  
Delete 3 old BL files, 3 old session stores, `LLMProviderHelper.cs`.

---

## 15. Adding a New Agent Persona (Zero Recompile)

```
1. Admin UI → Agent Personas → [+ New Persona]
   → Fill SkillKey, DisplayName
   → Answer behavior questions (wizard computes CapabilityFlags)
   → Write system prompt
   → Save: creates one AppAgentSkillSet row with SystemPrompt set (no AppAISkill row created)

2. Admin UI → Tools → [+ Add Tool]
   → Select SkillKey = new persona
   → Select ToolType (SqlQuery / HttpRest / ExternalDll etc.)
   → Fill tool config
   → Save: creates AppAgentToolRegister row

3. Frontend calls: POST /webapi/SomeController/RunAgent
   → Body includes skillKey = 'new-persona'
   → GenericAgentBL picks up the DB config automatically
   → No code change, no recompile, no redeploy
```

---

## 16. Verification Checklist

| # | Test | Expected |
|---|---|---|
| 0 | Run migration SQLs | 4 SkillSet rows (each with non-null `SystemPrompt`), ~31 tool rows; `AppAISkill` unchanged |
| 1 | `dotnet build AppAI.Core.sln` | Zero errors |
| 2 | POST `/webapi/AppBuilderAgent/RunAgent` | SSE events stream; steps + final response arrive |
| 3 | Trigger `propose_plan` → POST `/ConfirmPlan` | Agent resumes after gate |
| 4 | POST `/webapi/AppReportAgent/RunAgent` | Report data returned |
| 5 | POST `/webapi/DbGenie/Chat` | SQL response returned |
| 6 | POST `/webapi/AppDataIntegrationAgent/StartSession` | Cursor cloud events arrive |
| 7 | Admin UI: create new SkillSet via wizard | Row created; behavior questions → correct CapabilityFlags bitmask |
| 8 | Admin UI: add SqlQuery tool to new SkillSet | Row in AppAgentToolRegister; agent calls it correctly |
| 9 | Drop ExternalDll + register via admin | Agent picks up new tool without restart |
| 10 | DynamicCSharp tool: LLM passes code with `System.IO` | Executor rejects — not in whitelist |
| 11 | DynamicCSharp tool: LLM passes valid LINQ expression | Compiles and returns correct result |
| 12 | Register MCP server in `AppAgentMcpServer`; run agent | MCP tools auto-discovered; agent calls them |
| 13 | MCP tool returns `{ "ui_hint": "FlexGrid", "data": [...] }` | `MCPAppRenderer` renders `AGGridMCP` (AG Grid) in chat |
| 14 | Admin UI: click [▶ Run] on App Builder SkillSet | Right drawer opens with `<GenericAgentChat />` |
| 15 | POST `/webapi/GenericAgent/Run { skillKey: "db-genie", ... }` | Streams response; same result as DbGenieController |
| 16 | Gate triggered in test drawer → Approve clicked | Agent resumes; gate resolved via `ConfirmGate` |

---

## 17. Risk Register

| Risk | Mitigation |
|---|---|
| ~~`Microsoft.SemanticKernel` incompatible with `net10.0`~~ | **Not a risk.** SK 1.74.0 ships native `net10.0` lib folder (confirmed from local cache). Use version 1.74.0 matching BC-MCP-Client. |
| Roslyn sandbox escape via reflection | Whitelist enforced at `ScriptOptions` level; `System.Reflection` not in safe list |
| `ExternalDll` DLL loads wrong version | Use `Assembly.LoadFrom` with full path; version-check the assembly |
| System prompt too long | Prune in `GenericAgentEngine` at token budget (Level 1 pruning + Level 2 summarization) |
| PlanGate/SchemaGate session not cleaned up on timeout | `GenericAgentSessionStore` TTL; clean up on session expiry |
| MCP server unavailable at session start | Catch `McpClient.CreateAsync` / `HttpClientTransport` exceptions per server; log + skip; agent continues with remaining tools |
| `mcp-components` React library version drift from BC-MCP-Client | Pin to a specific commit/tag when copying; document source version |
| Official `Anthropic` SDK (v12.42.0) breaking API vs community SDK | BC-MCP-Client already migrated — use its integration pattern as reference |
