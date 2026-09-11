# Multi-Agent Coordination — Test Guide

Simple end-to-end test using a **Production → Sales + Warehouse** scenario.

---

## Scenario

| Agent | SkillKey | Role | ExecutionMode |
|---|---|---|---|
| Production Coordinator | `prod-agent` | Orchestrator | Normal |
| Sales Order Agent | `sales-agent` | Worker | **Deterministic** |
| WS Manager Agent | `ws-agent` | Worker | **Deterministic** |

Workers **must** be `Deterministic` — in Normal mode they block waiting for user approval that never arrives when called headlessly.

---

## Step 1 — Run Migration V022

Start the backend. Flyway/Evolve applies `V022__AgentCoordination.sql` automatically. Then verify:

```sql
-- Table exists?
SELECT * FROM AppAgentSharedContext;
-- → 0 rows (empty, not missing)

-- Library seeded?
SELECT LibraryKey, IsActive FROM AppAgentToolLibrary
WHERE LibraryKey = 'platform-multi-agent';
-- → 1 row, IsActive = 1

-- Three tools seeded?
SELECT ToolName FROM AppAgentToolRegister
WHERE SkillKey = 'platform-multi-agent';
-- → call_agent, write_shared_context, read_shared_context
```

---

## Step 2 — Create Three Agents

In Agent Management, create:

1. **prod-agent** — name "Production Coordinator", ExecutionMode = Normal
2. **sales-agent** — name "Sales Order Agent", ExecutionMode = **Deterministic**
3. **ws-agent** — name "WS Manager Agent", ExecutionMode = **Deterministic**

---

## Step 3 — Subscribe All Three to `platform-multi-agent`

For each agent: **Tools tab → Add Library → `platform-multi-agent`**

After subscribing, each agent's tool list shows:
- `call_agent`
- `write_shared_context`
- `read_shared_context`

Verify (9 rows expected):

```sql
SELECT SkillKey, ToolName FROM AppAgentToolRegister
WHERE SkillKey IN ('prod-agent','sales-agent','ws-agent')
  AND ToolName IN ('call_agent','write_shared_context','read_shared_context')
ORDER BY SkillKey, ToolName;
```

---

## Step 4 — Configure System Prompts

### prod-agent (Orchestrator)

```
You are a Production Coordinator in a Fashion PLM system.

When a production lot is reported complete:
1. Call write_shared_context with key "production-lot" and a JSON object:
   {"lotNumber":"...","productCode":"...","quantity":0}
   Fill in the values from the user's message.
2. Call call_agent("sales-agent", "A production lot is ready. Read shared context for details and confirm a sales order.")
3. Call call_agent("ws-agent", "A production lot is ready. Read shared context for details and confirm a stock update.")
4. Summarise: lot details, sales order status, warehouse status.

Use the exact SkillKeys: sales-agent and ws-agent.
```

### sales-agent (Worker)

```
You are a Sales Order Agent in a Fashion PLM system.

When invoked:
1. Call read_shared_context with key "production-lot".
2. Respond with exactly one sentence:
   "Sales order SO-XXXX created for [quantity] units of [productCode] (lot [lotNumber])."
```

### ws-agent (Worker)

```
You are a Warehouse Stock Manager in a Fashion PLM system.

When invoked:
1. Call read_shared_context with key "production-lot".
2. Respond with exactly one sentence:
   "Warehouse stock updated: [quantity] units of [productCode] added from lot [lotNumber]."
```

---

## Step 5 — Run the Test

Open **GenericAgentChat** → select `prod-agent` → send:

> Production lot #LOT-2026-001 is done. Product: SKU-BLAZE-42, quantity: 500 units. Coordinate downstream.

**Expected response** contains all three of:
- Lot: `LOT-2026-001`, `SKU-BLAZE-42`, `500 units`
- "Sales order SO-XXXX created for 500 units of SKU-BLAZE-42…"
- "Warehouse stock updated: 500 units of SKU-BLAZE-42…"

---

## Step 6 — Verify Three Checkpoints

### A · NLog — same WorkflowId across all agents

Search `logs/AppAI-*.log` for `[Agent] START`:

```
INFO [Agent] START skill=prod-agent  workflowId=a1b2c3d4-e5f6-...
INFO [Agent] START skill=sales-agent workflowId=a1b2c3d4-e5f6-...  ← same GUID
INFO [Agent] START skill=ws-agent    workflowId=a1b2c3d4-e5f6-...  ← same GUID
```

All three must share the **identical** WorkflowId.

### B · Database — shared context row exists

```sql
SELECT ScopeId, ContextKey, DataJson, UpdatedAt
FROM AppAgentSharedContext
ORDER BY UpdatedAt DESC;

-- Expected: 1 row
-- ScopeId    = WorkflowId GUID from NLog
-- ContextKey = 'production-lot'
-- DataJson   = {"lotNumber":"LOT-2026-001","productCode":"SKU-BLAZE-42","quantity":500}
```

### C · Tool trace — workers read from blackboard

NLog should show each worker calling `read_shared_context` and receiving the JSON:

```
INFO [Tool] read_shared_context key=production-lot
     → {"lotNumber":"LOT-2026-001","productCode":"SKU-BLAZE-42","quantity":500}
```

---

## Full Pass Criteria

- [ ] Migration V022 applied, table and 3 tools exist
- [ ] Three agents created with correct ExecutionMode
- [ ] All 9 tool subscriptions present
- [ ] prod-agent response aggregates both worker replies
- [ ] NLog: same WorkflowId in all three `[Agent] START` entries
- [ ] DB: one row in `AppAgentSharedContext` with correct JSON
- [ ] NLog: both workers called `read_shared_context` before replying
