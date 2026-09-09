# No-Code Agent Builder — Product Requirements Document

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-08  
**Status:** Implemented (Plan 1 + Plan 2 shipped)  
**Audience:** Product managers, SaaS administrators, domain experts

---

## 1. Problem Statement

After the Generic Agent Platform refactor (see `GenericAgent-PRD.md`), the platform could *run* any agent configured in the database — but creating a good agent still required developer involvement:

| Gap | Impact |
|---|---|
| Blank system prompt textarea | Domain experts didn't know what to write or how to structure it |
| Tools siloed per agent | The same SQL query registered 10× for 10 agents; no sharing |
| No way to discover built-in tools | Admins had to guess TypeName/MethodName strings from the C# source |
| No version history | A bad system prompt edit had no rollback |
| No AI assistance | Starting from scratch was intimidating for non-developers |

This PRD covers the two sprints that closed those gaps, making agent creation a self-service task for domain experts with no developer involvement.

---

## 2. Goals

| Goal | Success Condition |
|---|---|
| Domain expert can build a working agent in one session | No developer required; no code written |
| Tool libraries shared across agents | One library subscription instead of N duplicate tool rows |
| Built-in tools discoverable without reading C# source | Searchable picker shows available methods with descriptions |
| System prompt guided by AI | One description → AI generates structured four-section prompt + recommends tools |
| Version history with rollback | Last 10 prompt versions browseable and restorable |
| Tool recommendations pre-checked | AI-recommended libraries and built-in tools shown as checkboxes, not forced |

---

## 3. Non-Goals

- **MCP server auto-configuration.** MCP servers require per-agent connection details (URL, command, auth). The AI Generate feature notes relevant MCP patterns in the generated prompt but cannot configure the server automatically. Admin configures manually in the MCP Servers tab.
- **Agent-to-agent orchestration.** Single-agent sessions only.
- **Credential vault.** HttpRest ToolConfig stores API keys as plain text in the tenant DB. A future `AppAgentCredential` encrypted store is reserved but out of scope.
- **End-user agent creation.** These features target platform *admins* only (SaaS tenant administrators, not the employees who use the agents day-to-day).

---

## 4. User Personas

### Persona A — Domain Expert / SaaS Administrator

A warehouse manager, finance team lead, or operations analyst who has been given admin access. They know their business domain deeply but are not .NET developers. They can write plain English, understand what a "query" is, and can read a SQL SELECT if someone explains the columns.

**Before (pain):** Created a ticket for a developer to add a new agent. Waited a sprint. Got back something that didn't quite match the workflow they described.

**After (now):** Opens Agent Management → clicks ✨ AI Generate → types a paragraph description → reviews and accepts AI-recommended tool libraries → clicks "Use this" → saves → tests in the embedded chat → iterates prompt in minutes.

### Persona B — Platform Developer / AppAI Administrator

Builds and curates the tool library for domain experts to consume. Creates domains and libraries, writes SqlQuery and HttpRest tools in the Library Management tab, and seeds built-in tool wrappers so domain experts can pick them from a dropdown.

**Their task:** Publish one `platform-queries` library with ten SQL query tools → all warehouse agents can subscribe to it → zero duplication.

---

## 5. Feature Set

### 5.1 Tool Library Hierarchy (Plan 2)

Three-level shared tool infrastructure:

```
AppAgentToolDomain     'database-queries'  →  'Custom SQL Queries'
  AppAgentToolLibrary  'platform-queries'  →  'Platform Query Tools'  (SqlQuery)
    AppAgentToolRegister  SkillKey='platform-queries', ToolName='get_tenant_settings'
    AppAgentToolRegister  SkillKey='platform-queries', ToolName='get_open_orders'

AppAgentLibrarySubscription
  SkillKey='warehouse-agent'  →  LibraryKey='platform-queries'
  SkillKey='finance-agent'    →  LibraryKey='platform-queries'
```

**Key design decision:** `AppAgentToolRegister.SkillKey` already had no FK constraint — a LibraryKey *is* a SkillKey for tool rows owned by that library. No schema change to existing tables. The UNION merge at session start is transparent to the LLM.

**Dedup rule:** If an agent-owned tool and a library tool share the same `ToolName`, the agent-owned tool wins. Libraries are additive, never overriding.

#### Seeded Domains

| DomainKey | Purpose |
|---|---|
| `platform` | Built-in C# plugin tools |
| `mcp-servers` | MCP server connections |
| `database-queries` | Custom SqlQuery tools |
| `external-rest` | HttpRest third-party API tools |

### 5.2 Tool Library Management UI

Three-panel screen in the Libraries tab of Agent Management:

- **Left panel:** Domain list — create / delete domains
- **Middle panel:** Library list for the selected domain — LibraryKey, LibraryName, ToolCount, IsActive
- **Right panel:** Library edit form + embedded `AgentToolRegisterTab` for managing that library's tools inline

Domain experts can browse all available libraries without editing them. Platform developers maintain the libraries.

### 5.3 Built-In Tool Browser

When registering a BuiltIn tool, instead of guessing TypeName/MethodName:

- A searchable dropdown lists every active BuiltIn tool in the register
- Selecting one auto-fills ToolConfig with the correct `{"TypeName":"...","MethodName":"..."}` JSON
- Each entry shows the tool description so the admin understands what it does

Available via `GET /webapi/AgentSkillSet/GetAvailableBuiltInTools` — returns ToolName, ToolDescription, ToolConfig for all `ToolType='BuiltIn' AND IsActive=1` rows.

### 5.4 Library Subscription in Agent Editor

In the agent editor's Tool Libraries section:

- Search box filters the full library catalog
- "Subscribed" section shows currently subscribed libraries with tool counts
- Domain-grouped unsubscribed libraries shown below
- Toggle a library on/off → marks subscriptions as dirty → saved with the agent on Save
- Preview expander shows the library's tool names and descriptions before subscribing

### 5.5 Prompt Version History

Every `UpsertSkillSet` call that changes `SystemPrompt` first snapshots the *previous* prompt into `AppAgentSkillSetHistory`. The agent editor shows a History button that:

- Lists the last 10 saves with timestamps
- Clicking a version previews the old prompt in a read-only panel
- "Restore" copies it into the active editor (user must Save to commit)

Table: `AppAgentSkillSetHistory (HistoryId, SkillKey, SystemPrompt, SavedAt, SavedBy)` — pruned to 10 entries per agent on each insert.

### 5.6 AI Generate — System Prompt + Tool Recommendations

The flagship no-code feature. A ✨ AI Generate button sits beside the System Prompt label in the agent editor.

#### Phase 1 — Describe the Agent

A modal asks: *"Describe what this agent should do."*

The domain expert types a plain-English paragraph, e.g.:

> "An agent that helps warehouse managers check current inventory levels, pending purchase orders, and supplier lead times from our ERP database. Read-only. Results should be formatted as clear tables."

#### Phase 2 — Review AI Output

The AI call returns three things:

1. **Generated system prompt** — four-section structure (Role / Workflow / Rules / Output Format), preview in a read-only textarea
2. **Recommended tool libraries** — pre-checked checkboxes for libraries whose description matches the use case (e.g. `platform-queries`)
3. **Recommended built-in tools** — pre-checked checkboxes for built-in tools the agent clearly needs (e.g. `execute_sql`, `get_database_tables`)

The domain expert reviews:
- Uncheck any recommendation they don't want
- Click **← Regenerate** to go back to Phase 1 with the description kept
- Click **✓ Use this** to apply everything in one action

#### What "Use this" does

1. Fills the System Prompt textarea with the generated prompt
2. Adds accepted library keys to the agent's subscriptions (merged, not replaced)
3. Calls `UpsertTool` immediately for each accepted built-in tool, then refreshes the tools list

MCP servers are not auto-configured. If the generated prompt mentions an MCP integration (e.g. "connect to Shopify"), the admin configures that server manually in the MCP Servers tab.

#### How the AI knows which tools exist

The backend endpoint loads the actual catalog before calling the LLM:

- **Tool library catalog** — all `AppAgentToolLibrary` rows (LibraryKey, ToolCategory, DomainKey, description)
- **Built-in tool catalog** — all `AppAgentToolRegister WHERE ToolType='BuiltIn' AND IsActive=1` rows

Both catalogs are injected into the meta-prompt. The LLM is instructed to pick only from the listed keys/names, never invent them. Recommendations that don't match the catalog are filtered out by the UI before displaying checkboxes.

---

## 6. User Journeys

### Journey 1 — New Agent from Scratch (AI Assisted)

1. Admin opens Agent Management → clicks **+New Agent**
2. Types a SkillKey and DisplayName
3. Clicks **✨ AI Generate** beside System Prompt
4. Enters a plain-English description of the agent's purpose
5. Clicks **Generate** → reviews the four-section prompt and pre-checked tool recommendations
6. Unchecks one library that doesn't fit → clicks **✓ Use this**
7. System Prompt fills in; Library Subscriptions updated; Built-in tools registered
8. Clicks **Save**
9. Switches to the Chat tab → types a test message → verifies tool firing in the Tool Activity sidebar
10. Iterates the prompt (edits a section, saves → auto-snapshots history) → done in under 30 minutes

### Journey 2 — Add a New Query Tool to a Shared Library

1. Developer opens the Libraries tab
2. Selects domain `database-queries` → library `platform-queries`
3. In the embedded tool panel, clicks **+Tool** → ToolType: SqlQuery
4. Writes ToolName, Description (25+ words for reliable LLM selection), and SQL body
5. Saves → tool immediately available to all agents subscribed to `platform-queries`
6. No agent needs to be touched; no redeployment

### Journey 3 — Restore a Broken Prompt

1. Admin edited the system prompt, saved, and now the agent is mis-behaving
2. Clicks **History** button in the agent editor
3. Sees 5 timestamped saves; clicks the version from before the edit
4. Previews the old prompt → clicks **Restore** → old text fills the editor
5. Saves → agent behaviour restored

---

## 7. Acceptance Criteria

| # | Criterion | Verified |
|---|---|---|
| AC-1 | ✨ AI Generate modal opens when button clicked for any selected agent | ✅ |
| AC-2 | Typing a warehouse description generates a four-section prompt | ✅ |
| AC-3 | Recommended libraries are pre-checked and match the actual catalog | ✅ |
| AC-4 | Recommended built-in tools are pre-checked and match the actual catalog | ✅ |
| AC-5 | "Use this" fills System Prompt and updates subscriptions and tools list | ✅ |
| AC-6 | History button lists last 10 saves; restore works | ✅ |
| AC-7 | Library subscriptions save with agent and merge at session start | ✅ |
| AC-8 | Deleting a library cascades to tool rows, MCP rows, and subscription rows | ✅ |
| AC-9 | Agent with zero subscriptions behaves identically to before (no regressions) | ✅ |
| AC-10 | Built-in tool picker in tool editor shows all active BuiltIn tools | ✅ |

---

## 8. Known Limitations

- **MCP server auto-configuration not available.** The AI Generate prompt may reference MCP integration patterns by name, but the admin must configure the server connection manually.
- **HttpRest API keys stored as plain text** in ToolConfig JSON within the tenant DB. Per-tenant DB isolation prevents cross-tenant exposure but any admin with UI access can view the key. Future upgrade: `AppAgentCredential` encrypted store.
- **AI recommendations depend on catalog quality.** If the tool library has no relevant libraries or descriptions are vague, recommendations will be empty arrays. Catalog maintenance by platform developers is essential.
- **LLM provider must be configured.** The AI Generate call uses the same tenant-configured LLM as agents themselves. If no LLM is configured in `AppTenantSetting`, the Generate button returns an error.
