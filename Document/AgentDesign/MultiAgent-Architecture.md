# Multi-Agent Coordination Architecture

## Context

Currently every agent in this platform runs in complete isolation — there is no mechanism for
one agent to trigger another, share data, or coordinate a workflow. Each agent has its own
independent in-memory session store (`GenericAgentSessionStore`, `AppBuilderAgentSessionStore`,
etc.) with no cross-agent visibility.

The requirement: **Agent A** (Production) completes its work, then **Agent B** (Sales Order) and
**Agent C** (WS Management) can start — receiving the production data A produced. The user talks
only to Agent A; A orchestrates B and C internally.

---

## Architecture: Orchestrator + Worker Pattern

Agent A is the **orchestrator**. It gets three new tools via the `platform-multi-agent` library:
- `write_shared_context(key, value_json)` — writes structured data to a workflow-scoped blackboard
- `read_shared_context(key)` — reads from the same blackboard
- `call_agent(targetSkillKey, message)` — calls a named agent synchronously, returns its response

Agent B and C are **worker agents** configured with `ExecutionMode='Deterministic'`
(already in the schema, V016 — auto-approves all gates, runs headlessly to completion).
They subscribe to `platform-multi-agent` so they can also call `read_shared_context`.

**Data flow:**
```
User → Agent A ("Production lot #123 done — coordinate downstream")
  Agent A:
    1. write_shared_context("production-lot", {lotNumber, quantity, productCode, ...})
    2. call_agent("sales-order-agent", "Lot ready. Place orders.")    ─┐ SK fires
    3. call_agent("ws-manager-agent",  "Lot ready. Update stock.")    ─┘ both in parallel
  Agent A: aggregates B + C responses → replies to user
```

SK's `FunctionChoiceBehavior.Auto()` (already enabled in `GenericAgentEngine.cs`) allows the
LLM to invoke multiple tools in one round — so B and C run concurrently via `Task.WhenAll`
at the SK layer automatically. No special parallel code is needed.

---

## Shared Context: Workflow Instance ID (DB-backed)

The scope key is a **WorkflowId** (GUID) threaded automatically through `AgentToolContext`.
The LLM never sees it — it just calls `write_shared_context(key, value)` and the plugin
uses `context.WorkflowId` as the DB scope key automatically.

| Agent | WorkflowId source |
|---|---|
| Orchestrator (A) | `GenericAgentEngine` generates `Guid.NewGuid()` on first run |
| Worker B, C | `AgentCallPlugin` propagates the **same** WorkflowId into child runs |

**Why DB-backed (not in-memory):**
- Survives server restarts between workflow steps
- Works across different users/roles (Production Manager → Sales Manager in same workflow)
- Works for async approval gates (hours gap between steps)
- Auditable — you can query shared context rows for debugging

---

## Components

### AppAgentSharedContext (DB table)

```sql
CREATE TABLE dbo.AppAgentSharedContext (
    ScopeId    NVARCHAR(200) NOT NULL,   -- WorkflowId GUID
    ContextKey NVARCHAR(200) NOT NULL,
    DataJson   NVARCHAR(MAX) NOT NULL,
    UpdatedAt  DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AppAgentSharedContext PRIMARY KEY (ScopeId, ContextKey)
);
```

### AgentCallPlugin (BuiltIn tool)

```csharp
// TypeName: App.BL.AIAgent.GenericAgent.AgentCallPlugin
// MethodName: CallAgent
public static async Task<string> CallAgent(
    string targetSkillKey, string message,
    AgentToolContext context, CancellationToken ct)
```

Propagates `context.WorkflowId` to the child via `GenericAgentBL.RunAsync(..., workflowId: context.WorkflowId)`.

### AgentSharedContextPlugin (BuiltIn tool)

```csharp
// TypeName: App.BL.AIAgent.GenericAgent.AgentSharedContextPlugin
// WriteContext(key, valueJson, context)  — UPSERT to AppAgentSharedContext
// ReadContext(key, context)              — SELECT from AppAgentSharedContext
```

---

## Files Changed

| File | Change |
|---|---|
| `AgentToolContext` class | Add `WorkflowId` string property |
| `GenericAgentBL.cs` | Add optional `workflowId` param to `RunAsync` |
| `GenericAgentEngine.cs` | Add `workflowId` param; generate UUID if absent |
| `V022__AgentCoordination.sql` | `AppAgentSharedContext` table + `platform-multi-agent` library seed |
| `AgentCallPlugin.cs` | **New** — `CallAgent` BuiltIn |
| `AppAgentSharedContextBL.cs` | **New** — ReadContext / WriteContext (DB) |
| `AgentSharedContextPlugin.cs` | **New** — ReadContext / WriteContext BuiltIn methods |

---

## Admin Configuration (after deploy)

1. Run migration V022
2. Open the **Orchestrator agent** in Agent Management → subscribe to `platform-multi-agent`
3. Open each **Worker agent** → set `ExecutionMode = Deterministic` → subscribe to `platform-multi-agent`
4. In the orchestrator's system prompt, describe the downstream agents:
   > "When production completes: call `write_shared_context` with lot data, then call
   > `call_agent('sales-order-agent', ...)` and `call_agent('ws-manager-agent', ...)`.

---

## Verification

1. Create three skill sets: `prod-agent` (orchestrator), `sales-agent` (Deterministic), `ws-agent` (Deterministic)
2. Subscribe all three to `platform-multi-agent`
3. Give `prod-agent` a system prompt that writes shared context then calls both sub-agents
4. In GenericAgentChat, send: "Production lot #1 is done — coordinate downstream"
5. Check NLog: two `[Agent] START skill=sales-agent` / `skill=ws-agent` entries with the same WorkflowId
6. Verify `prod-agent` response aggregates both sub-agent results
7. Query `SELECT * FROM AppAgentSharedContext` — verify the production-lot data row exists
