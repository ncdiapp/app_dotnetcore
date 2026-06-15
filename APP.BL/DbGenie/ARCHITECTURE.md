# DBA-Genie Chat Agent — Architecture

> **Location:** `APP.BL/DbGenie/`
> **Last updated:** 2026-03-08

---

## 1. Overview

DBA-Genie is a conversational SQL assistant. Users describe what they want in plain language and the agent generates SQL queries, explains schemas, and helps with database design.

```
User (React Chat UI)
        │  message + sessionId + conversationHistory
        ▼
DbGenieController  (Web API)
        │
        ▼
AppDbGenieBL.ChatWithAgentAsync()
        │
        ├─ Load Session from disk (if sessionId matches a saved file)
        ├─ Load Cross-Session Memory (memory.md → system prompt)
        ├─ Call LLM (Gemini / Anthropic / OpenAI)
        │
        ├─ Save Session to disk  (sessions/{sessionId}.json)
        └─ Update Cross-Session Memory (memory.md)
```

---

## 2. Memory System — Two Tiers

### Tier 1 — Per-Session Persistence

**File:** `memory/dbgenie/sessions/{sessionId}.json`

Each session gets its own JSON file containing the full list of `DbGenieChatMessageDto` objects. When a request arrives with a `SessionId`, the BL:

1. Checks if `sessions/{sessionId}.json` exists on disk
2. If yes → loads the stored messages as the conversation history (overrides or merges with what the frontend sent)
3. After the LLM responds → appends the new exchange and saves the file back

This allows sessions to be **resumed exactly** even after a page refresh or browser close.

```json
// memory/dbgenie/sessions/abc123.json
[
  { "role": "user",      "content": "Show me all tickets",   "timestamp": "..." },
  { "role": "assistant", "content": "Here's the SQL: ...",   "timestamp": "..." }
]
```

**Retention:** Sessions files older than 30 days are pruned on startup.

---

### Tier 2 — Cross-Session Summary

**File:** `memory/dbgenie/memory.md`

A single human-readable markdown file injected into the LLM **system prompt** at the start of every chat (new or resumed). Keeps the last 5 session summaries so the agent has context even in a brand-new session.

```markdown
## DBA-Genie Memory

### Recent Sessions (last 5)

---
*2026-03-08 10:30 UTC* | DataSource: 1
**Question:** Show all open tickets grouped by category
**Tables used:** Unknown_Tickets, Unknown_Categories
**SQL:**
```sql
SELECT c.Name, COUNT(t.TicketId) ...
```

---
...
```

---

## 3. File Structure

```
memory/dbgenie/               ← runtime directory (not in source control)
├── memory.md                 ← cross-session summary (injected into every system prompt)
└── sessions/
    ├── {sessionId-1}.json    ← full conversation for session 1
    ├── {sessionId-2}.json    ← full conversation for session 2
    └── ...
```

---

## 4. Key Classes

| Class | File | Role |
|-------|------|------|
| `DbGenieMemoryBL` | `DbGenieMemoryBL.cs` | Read/write both memory tiers |
| `AppDbGenieBL.ChatWithAgentAsync` | `AppDbGenieBL.cs` | Calls memory BL at start and end of each turn |

---

## 5. Memory Lifecycle

```
ChatWithAgentAsync(request)
  │
  ├─ 1. DbGenieMemoryBL.LoadSession(sessionId)
  │       → reads sessions/{sessionId}.json
  │       → if found, use as conversationHistory (authoritative)
  │
  ├─ 2. DbGenieMemoryBL.LoadMemoryContext()
  │       → reads memory.md
  │       → appends to system prompt if non-empty
  │
  ├─ 3. Call LLM with enriched system prompt + full history
  │
  └─ 4. DbGenieMemoryBL.SaveExchange(sessionId, userMsg, assistantMsg, sql)
          → appends to sessions/{sessionId}.json
          → updates memory.md (prepend new entry, keep last 5)
```

---

## 6. Design Decisions

| Decision | Rationale |
|----------|-----------|
| **JSON for session files** | Exact round-trip of `DbGenieChatMessageDto[]`; easy to deserialize for session resume |
| **Markdown for cross-session summary** | Human-readable, diffable, injected as plain text into LLM prompt; no parsing needed |
| **SessionId as filename** | One file per session, no DB needed, trivial to prune old sessions by file date |
| **Merge strategy: disk wins** | If `sessions/{id}.json` exists on disk, it is the authoritative history (overrides the potentially stale frontend copy) |
| **Non-fatal** | All memory read/write operations are wrapped in try/catch — a disk error never crashes the chat |
