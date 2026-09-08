# Agent Platform: Full No-Code Agent Builder Plan

**Goal:** End users (non-developers) can build and deploy AI agents without writing any code.
**Stack:** Three sequential deliverables — Bug Fixes → Plan 2 → Plan 1.

---

## BUG FIXES — Ship Before Plan 2

Two regressions found in the current codebase. Fix these first; they block non-developers today.

### Bug 1: ToolType Dropdown Shows Wrong Values

**File:** `AppReact/src/components/aiskill/AgentToolRegisterTab.tsx`

Current dropdown options: `BuiltIn / Plugin / External`
Backend expects: `BuiltIn / SqlQuery / HttpRest / DynamicCSharp / ExternalDll / PowerShell`

If a user selects "External", the backend receives `"External"` → `AppAgentToolEngine.Dispatch`
hits the default case → returns `{"Error":"Unknown ToolType"}`. HttpRest and SqlQuery tools are
completely inaccessible via the UI today.

**Fix:** Replace the `<select>` options with the correct values:
```tsx
<option value="BuiltIn">Built-in (C# plugin)</option>
<option value="SqlQuery">SQL Query</option>
<option value="HttpRest">HTTP REST</option>
<option value="DynamicCSharp">Dynamic C# Script</option>
{isSuperAdmin && <option value="ExternalDll">External DLL</option>}
{isSuperAdmin && <option value="PowerShell">PowerShell</option>}
```
ExternalDll and PowerShell are hidden for regular admins (super-admin only, matching existing
security intent). Also update the ToolConfig textarea placeholder per selected ToolType with the
correct JSON starter (see Plan 1 §Tool Description Helper).

### Bug 2: InjectSchema Flag Is Dead in GenericAgentEngine

**File:** `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs`

`GenericAgentEngine` never reads `skillSet.CapabilityFlags`. The `InjectSchema` (bit 32) logic
that injects DB schema into the system prompt lives only in the old `AppDbGenieBL.cs` path.
`db-genie` now runs through GenericAgentEngine — its schema injection silently does nothing.
This is a regression from the generic agent refactor.

**Fix:** In `GenericAgentEngine.BuildSystemPrompt()` (or wherever system prompt is assembled):

```csharp
var prompt = skillSet.SystemPrompt;

if ((skillSet.CapabilityFlags & (int)AgentCapabilityFlags.InjectSchema) != 0)
{
    // Reuse the existing schema-fetch logic from AppDbGenieBL
    var schemaContext = await AppDbSchemaBL.GetSchemaSummaryAsync(identity, ct);
    if (!string.IsNullOrEmpty(schemaContext))
        prompt = prompt + "\n\n## DATABASE SCHEMA\n" + schemaContext;
}
```

Extract the schema-fetch call from `AppDbGenieBL` into a shared static method
`AppDbSchemaBL.GetSchemaSummaryAsync` so both paths use the same logic.

---

---

# PLAN 2 — Tool Library Management
**Sprint 1. Builds the shared tool library infrastructure and fixes the tool registration UX.**

## Context

Tools in `AppAgentToolRegister` are siloed by `SkillKey`. No sharing across agents.
At Shopify scale, this means 100×N duplicate rows. The fix is Domain → Library → Subscription:
a three-level hierarchy where agents subscribe to libraries at session start.

**Key insight:** `AppAgentToolRegister.SkillKey` has no FK constraint — it's plain NVARCHAR(100).
A LibraryKey is a SkillKey for tool rows in that library. No existing tables change.

---

## Three-Level Hierarchy

```
AppAgentToolDomain  'shopify' → 'Shopify Commerce API'
  AppAgentToolLibrary  'shopify-products' → 'Product Catalog Tools'  (HttpRest)
    AppAgentToolRegister  SkillKey='shopify-products', ToolName='get_products'

AppAgentLibrarySubscription
  SkillKey='api-create-agent'  →  LibraryKey='shopify-products'
```

Agent-owned tools (`SkillKey = agent SkillKey`) + subscribed library tools — UNION-merged
at session start. Agent-owned tool wins if names collide (dedup by ToolName).

---

## Seeded Domains + Example Tools

```sql
-- Domains (V017 seed)
INSERT INTO dbo.AppAgentToolDomain VALUES
    ('platform',         'Platform Built-in',   'Built-in C# tools',          1, 1),
    ('mcp-servers',      'MCP Servers',         'MCP server connections',      2, 1),
    ('database-queries', 'Custom SQL Queries',  'SqlQuery tools',              3, 1),
    ('external-rest',    'External REST APIs',  'HttpRest third-party tools',  4, 1);

-- Example library: tenant settings queries
INSERT INTO dbo.AppAgentToolLibrary VALUES
    ('platform-queries', 'database-queries', 'Platform Query Tools',
     'Common SqlQuery tools for reading platform config', 'SqlQuery', 1);

-- Example SqlQuery tool (non-developer can copy + adapt)
INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'platform-queries',
    'get_tenant_settings',
    'Call this tool when the user asks about platform configuration or tenant settings. Returns key-value settings for the current tenant.',
    '{"properties":{"category":{"description":"Settings category to filter by (optional)","type":"string"}},"required":[]}',
    'SqlQuery',
    '{"SqlBody":"SELECT SettingKey, SettingValue FROM dbo.AppTenantSetting WHERE (@category IS NULL OR SettingKey LIKE ''%'' + @category + ''%'') ORDER BY SettingKey","ReturnType":"json"}',
    1
);
```

At least one SqlQuery and one HttpRest example seeded so non-developers have a working template
to copy and adapt.

---

## Database Migration (V017)

```sql
CREATE TABLE dbo.AppAgentToolDomain (
    DomainKey   NVARCHAR(100) NOT NULL,
    DomainName  NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_AppAgentToolDomain PRIMARY KEY (DomainKey)
);

CREATE TABLE dbo.AppAgentToolLibrary (
    LibraryKey   NVARCHAR(100) NOT NULL,
    DomainKey    NVARCHAR(100) NOT NULL,
    LibraryName  NVARCHAR(200) NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    ToolCategory NVARCHAR(50)  NULL,   -- UI hint: HttpRest/SqlQuery/BuiltIn/Mcp
    IsActive     BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_AppAgentToolLibrary PRIMARY KEY (LibraryKey),
    CONSTRAINT FK_AppAgentToolLibrary_Domain
        FOREIGN KEY (DomainKey) REFERENCES dbo.AppAgentToolDomain(DomainKey)
);

CREATE TABLE dbo.AppAgentLibrarySubscription (
    SkillKey   NVARCHAR(100) NOT NULL,
    LibraryKey NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_AppAgentLibrarySub PRIMARY KEY (SkillKey, LibraryKey),
    CONSTRAINT FK_AppAgentLibrarySub_Library
        FOREIGN KEY (LibraryKey) REFERENCES dbo.AppAgentToolLibrary(LibraryKey)
        ON DELETE CASCADE
);

CREATE INDEX IX_AppAgentLibrarySub_LibraryKey
    ON dbo.AppAgentLibrarySubscription (LibraryKey);

-- Seed domains + example library + example tools (see above)
```

---

## Backend Changes

### 1. New: `APP.BL/TenantBusiness/AppAgentToolLibraryBL.cs`

Pattern: identical fixture pattern as `AppAgentToolRegisterBL.cs`.

DTOs:
```csharp
public sealed record AppAgentToolDomainDto(
    string DomainKey, string DomainName, string Description, int SortOrder, bool IsActive);

public sealed record AppAgentToolLibraryDto(
    string LibraryKey, string DomainKey, string LibraryName,
    string Description, string ToolCategory, bool IsActive, int ToolCount);

public sealed record AppAgentLibrarySubscriptionDto(string SkillKey, string LibraryKey);

public sealed record LibraryToolPreviewDto(string ToolName, string ToolDescription);
```

Methods (each with default-datasource + `int dsId` overloads):
- Domain: `GetAllDomains`, `UpsertDomain`, `DeleteDomain`
- Library: `GetAllLibraries` (includes ToolCount via COUNT JOIN), `GetByDomain`,
  `SearchLibraries(string query)` — LIKE on LibraryName+Description+DomainName,
  `GetLibraryToolPreview(string libraryKey)` — ToolName+ToolDescription only (NO ToolConfig),
  `UpsertLibrary`, `DeleteLibrary`
- `DeleteLibrary` execution order (critical):
  1. `DELETE FROM dbo.AppAgentMcpServer WHERE SkillKey = @LibraryKey`
  2. `DELETE FROM dbo.AppAgentToolRegister WHERE SkillKey = @LibraryKey`
  3. `DELETE FROM dbo.AppAgentToolLibrary WHERE LibraryKey = @LibraryKey`
  (Subscription rows removed by ON DELETE CASCADE in step 3)
- Subscription: `GetSubscriptions(string skillKey)`,
  `SetSubscriptions(string skillKey, IEnumerable<string> libraryKeys)` — DELETE + INSERT batch

### 2. Modify: `APP.BL/TenantBusiness/AppAgentToolRegisterBL.cs`

Add `GetBySkillKeyWithLibraries` (same trio overloads, existing methods untouched):

```sql
SELECT t.*, 0 AS IsLibraryTool
FROM dbo.AppAgentToolRegister t
WHERE t.SkillKey = @SkillKey AND t.IsActive = 1
UNION ALL
SELECT t.*, 1 AS IsLibraryTool
FROM dbo.AppAgentToolRegister t
INNER JOIN dbo.AppAgentLibrarySubscription s ON t.SkillKey = s.LibraryKey
WHERE s.SkillKey = @SkillKey AND t.IsActive = 1
ORDER BY IsLibraryTool, ToolRegisterId
```

**Dedup after query** (agent-owned wins on name collision):
```csharp
var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
return rows.Where(r => seen.Add(r.ToolName)).ToList();
// Rows ordered IsLibraryTool=0 first, so agent-owned always added first
```

### 3. Modify: `APP.BL/TenantBusiness/AppAgentMcpServerBL.cs`

Same UNION pattern for `AppAgentMcpServer`.

### 4. Modify: `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs`

Two changes:
- Lines ~99/106: `GetBySkillKey` → `GetBySkillKeyWithLibraries` (2-line swap)
- Add InjectSchema flag handling in system prompt assembly (see Bug 2 fix above)

### 5. Modify: `AppAI.Web/Controllers/AgentSkillSetController.cs`

Add 11 endpoints (`OperationCallResult<T>` + `GetDsId()` pattern):
```
GET  GetAllDomains()
POST UpsertDomain([FromBody] AppAgentToolDomainDto)
DEL  DeleteDomain(string domainKey)
GET  GetAllLibraries()
GET  GetLibrariesByDomain(string domainKey)
GET  SearchLibraries(string query)
GET  GetLibraryToolPreview(string libraryKey)   ← names+desc only, no ToolConfig
POST UpsertLibrary([FromBody] AppAgentToolLibraryDto)
DEL  DeleteLibrary(string libraryKey)
GET  GetSubscriptions(string skillKey)
POST SetSubscriptions([FromBody] SetSubscriptionsRequest)  // { SkillKey, LibraryKeys[] }
```

Also add **BuiltIn browser endpoint** (needed for tool registration UX):
```
GET  GetAvailableBuiltInTools()
```
Returns all distinct `TypeName + MethodName + ToolDescription` from seeded `BuiltIn` rows
(`SELECT DISTINCT ToolName, ToolDescription, ToolConfig FROM AppAgentToolRegister WHERE ToolType='BuiltIn' AND IsActive=1`).
Non-developers can pick from this list instead of typing JSON manually.

---

## Frontend Changes

### 6. Modify: `AppReact/src/webapi/agentSkillSetSvc.ts`

Add 4 DTO interfaces + 12 service methods (11 library endpoints + `GetAvailableBuiltInTools`).

### 7. Fix: `AppReact/src/components/aiskill/AgentToolRegisterTab.tsx`

Five improvements (includes Bug 1 fix):

**A — Fix ToolType dropdown** (Bug 1):
Replace hardcoded wrong options with correct ToolType values. Show ExternalDll/PowerShell
only for super-admins.

**B — ToolConfig starter templates per ToolType:**
When ToolType changes, auto-fill ToolConfig textarea with a correct JSON starter:
```typescript
const TOOL_CONFIG_TEMPLATES = {
  SqlQuery:     '{"SqlBody":"SELECT ... FROM dbo.YourTable WHERE Col=@param","ReturnType":"json"}',
  HttpRest:     '{"Url":"https://api.example.com/{param}","Method":"GET","Headers":{"Authorization":"Bearer YOUR_TOKEN"}}',
  DynamicCSharp:'{"ScriptBody":"// return a string result","AllowedNamespaces":["System","System.Linq"],"TimeoutSeconds":10}',
  BuiltIn:      '{"TypeName":"","MethodName":""}',
}
```

**C — BuiltIn tool picker:**
When ToolType = `BuiltIn`, show a searchable dropdown of available built-in methods:
```
Built-in Method  [Search or pick...  ▾]
  propose_plan        — Call when user confirms a plan is ready
  create_form         — Creates a new form in the platform
  create_table        — Creates a DB table via schema migration
  get_search_screens  — Lists available search screens
  ...
```
Selecting one auto-fills `ToolConfig` with the correct TypeName/MethodName JSON.

**D — Schema browser for SqlQuery ToolType:**
When ToolType = `SqlQuery`, show a collapsible **"DB Schema Browser"** below the ToolConfig:
```
DB Schema  [▾ Browse tables]
  AppTenantSetting    SettingKey, SettingValue, IsActive
  AppApiRegister      ApiId, ApiName, Url, Method, IsActive
  AppBusinessPartner  BpId, BpName, BpType, ...
  [click column name → inserts @columnName into SQL at cursor]
```
Calls a new `GET SchemaMetaData/GetTableList` endpoint (check if `SchemaMetaDataController`
already exposes this — it likely does given `DatabaseSchemaReader` exists in the solution).

**E — Tool description helper** (word count + placeholder):
```
Description
[________________________________________________________]
 Placeholder: "Call this tool when [trigger]. Returns [data]. Use it when the user [intent]."
 Word count badge: 8 words  ⚠ Aim for 25+ words for reliable LLM tool selection
```

### 8. New: `AppReact/src/components/aiskill/AgentLibraryTab.tsx`

Three-panel library management screen:

**Left (w-48):** Domain list — +New Domain button, click domain → loads its libraries

**Middle (w-64):** Library list for selected domain — FlexGrid with LibraryKey, LibraryName,
ToolCount, IsActive; +New Library / Delete Library buttons

**Right:** Context-sensitive:
- Domain selected → domain edit form
- Library selected → two sections:
  - Library form (LibraryKey, DomainKey, LibraryName, ToolCategory, Description, IsActive)
  - **Embedded tool list:** reuse `AgentToolRegisterTab` component, passing
    `skillKey={selectedLibraryKey}` — admins add/edit/delete library tools inline here

### 9. Modify: `AppReact/src/components/aiskill/AgentSkillSetManagement.tsx`

**Change A — 4th tab:** `'libraries'` → renders `<AgentLibraryTab />`

**Change B — Subscription browser in agent editor** (after Active checkbox):
```
Tool Libraries
  [Search: ____________]

  Subscribed (2)
    ☑ shopify-products  Product Catalog Tools (6 tools)  [Preview ▾]
    ☑ shopify-orders    Order Management (8 tools)         [Preview ▾]

  [ External REST APIs ]
    ☐ shopify-customers  Customer Data (4 tools)           [Preview ▾]

  [ MCP Servers ]
    ☐ erp-mcp  BlueCherry ERP (auto-discovers tools)
```
Preview expands inline with tool names + descriptions from `GetLibraryToolPreview`.
Toggling: `subsChanged = true`, `setIsDirty(true)`.
`handleSave`: if `subsChanged` → call `SetSubscriptions`.

---

## Plan 2 Implementation Sequence

| Day | Work |
|---|---|
| 0 | **Bug fixes:** fix ToolType dropdown; implement InjectSchema in GenericAgentEngine |
| 1 | V017 migration (3 tables + domain seeds + example tool rows) |
| 1 | `AppAgentToolLibraryBL.cs` — all methods |
| 2 | `GetBySkillKeyWithLibraries` + dedup in ToolRegisterBL + McpServerBL |
| 2 | 2-line engine swap → deploy + smoke test |
| 3 | 12 controller endpoints (11 library + GetAvailableBuiltInTools) |
| 3 | `agentSkillSetSvc.ts` additions |
| 4 | `AgentToolRegisterTab.tsx` — Bug 1 fix + ToolType templates + BuiltIn picker + schema browser + description helper |
| 5 | `AgentLibraryTab.tsx` — three-panel with embedded tool sub-panel |
| 5 | `AgentSkillSetManagement.tsx` — 4th tab + subscription browser |
| 6 | E2E verification |

---

## Backward Compatibility

- `AppAgentToolRegister` / `AppAgentMcpServer` schemas: **unchanged**
- All existing BL `GetBySkillKey` methods: **unchanged**
- All existing controller endpoints: **unchanged**
- Agents with zero subscriptions: UNION Branch 2 = 0 rows → **identical behavior**
- InjectSchema fix restores db-genie behavior (was silently broken, now fixed)

---

## Plan 2 Verification

1. **Bug fixes:** Select SqlQuery in dropdown → ToolConfig template fills → save → agent calls tool correctly. Run db-genie → schema appears in tool steps.
2. **Dedup:** Agent + library both define `get_products` → only agent-owned fires, no SK crash.
3. **BuiltIn picker:** Select BuiltIn ToolType → picker shows available methods → select `propose_plan` → ToolConfig auto-fills → save → works.
4. **SqlQuery schema browser:** Select SqlQuery → browse tables → click column → inserts `@colName` in SQL.
5. **Library E2E:** Create domain → library → add tool in library's embedded tool panel → subscribe agent → run → library tool fires.
6. **DeleteLibrary cascade:** Delete library → tool rows, MCP rows, subscription rows all gone.
7. **Shopify MCP:** `shopify-mcp` library + one MCP server row → subscribe agent → 100+ tools auto-discovered.

---

---

# PLAN 1 — Agent Design Experience
**Sprint 2. Builds the UX for non-developers to create and iterate on agents.**
**Depends on:** Plan 2 (Bug Fixes + library infrastructure) must ship first.

## Context

The platform now has working tool types, a shared library, and a subscription model.
The remaining gap: a non-developer still faces a blank system prompt textarea and no
understanding of what makes an agent good or bad.

Goal: templates + iteration tools, not a wizard. Domain experts can write their own instructions
for their own workflow — what they need is structure, starting points, and fast feedback.

---

## What's NOT in Plan 1 (Dropped)

**Agent Builder Wizard (dropped):** A conversational AI that interviews the user and generates
a system prompt adds a meta-layer that domain experts don't need. They know their workflow —
they just need a structured form and a template. Templates are also more reliable (expert-written,
tested) than AI-generated prompts from a 6-question interview. The wizard can be revisited
as Phase 3 for users who need it.

---

## New Things to Build for Plan 1

### 1. Agent Template Library

**No new table needed.** Templates are pre-built SkillSet configurations delivered as a
"New from Template" picker. Five templates seeded in V018 migration as `AppAgentSkillSet` rows
with `IsActive = 0` (inactive = not shown in agent list, but available as template source).

| Template SkillKey | Template Name | Best for |
|---|---|---|
| `tmpl-api-integration` | API Integration Agent | Registering / managing external REST APIs |
| `tmpl-data-query` | Data Query Agent | Translating questions to SQL / search results |
| `tmpl-crud-assistant` | CRUD Assistant | Guiding users through create/edit/delete records |
| `tmpl-workflow-automation` | Workflow Automation | Multi-step processes with plan approval gate |
| `tmpl-report-analytics` | Report & Analytics | Finding data, formatting as grids and summaries |

Each template has a fully-written `SystemPrompt`, correct `CapabilityFlags`, and sensible
threshold defaults. Written by a developer who knows the platform.

**UI:** In `AgentSkillSetManagement.tsx`, the "+New" button becomes a dropdown:
```
[ + New Agent ▾ ]
  ├── Blank
  ├── API Integration Agent
  ├── Data Query Agent
  ├── CRUD Assistant
  ├── Workflow Automation
  └── Report & Analytics
```

Selecting a template: copies the template row's SystemPrompt, CapabilityFlags, and thresholds
into the new agent editor form. SkillKey and DisplayName left blank for the user to fill.

**New endpoint:** `GET GetTemplates()` — returns rows where `SkillKey LIKE 'tmpl-%' AND IsActive=0`.

### 2. System Prompt Structured Sections

Replace the single `SystemPrompt` raw textarea with four collapsible labeled sections that
assemble into the final prompt on Save:

```
▸ Role         [You are a ____________ for AppAI. Your job is to...]
▸ Workflow     [Step 1: ... Step 2: ... Step 3: ...]
▸ Rules        [- Always ask for X before doing Y\n- Never do Z without approval]
▸ Output Format [When returning a list, format as: ```mcp-ui {...}```]
```

Each section has a placeholder and a collapsible example. The four sections are stored as
a single string in `SystemPrompt` with `## Role`, `## Workflow`, `## Rules`, `## Output Format`
H2 headers as separators (standard markdown, readable by the LLM).

On Save: concatenate sections → store in `SystemPrompt`. On Load: split on `## ` headers →
populate section fields. Falls back to showing the full text in a single textarea if no headers
found (backward compatible with existing agents).

### 3. System Prompt Version History

**New table in V018 migration:**
```sql
CREATE TABLE dbo.AppAgentSkillSetHistory (
    HistoryId    INT IDENTITY(1,1) PRIMARY KEY,
    SkillKey     NVARCHAR(100) NOT NULL,
    SystemPrompt NVARCHAR(MAX) NOT NULL,
    SavedAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    SavedBy      NVARCHAR(200) NULL
);
-- Keep last 10 versions per agent (BL prunes on insert)
```

On every `UpsertSkillSet`: insert the PREVIOUS SystemPrompt into history before overwriting.
In the UI: "History" dropdown in the agent editor toolbar showing last 5 saves with timestamp.
Click a version → previews the old prompt in a read-only panel → "Restore" button copies it
into the editor (user must Save to commit the restore).

**New BL:** `AppAgentSkillSetHistoryBL.GetRecent(skillKey, limit=5)` + `Insert(dto)`.
**New endpoint:** `GET GetPromptHistory(string skillKey)`.

### 4. Tool Diagnostic Upgrade

**File:** `AppReact/src/components/aiskill/GenericAgentChat.tsx`

Replace the current "last 3 steps" chip list with a collapsible **Tool Activity** sidebar:

```
Tool Activity                              [▾ collapse]
  ✓ get_products
    Args: { "store": "acme" }
    Result: 12 products returned (first 200 chars shown)

  ✗ create_order  [error]
    Args: { "productId": "123" }
    Error: HTTP 422 {"errors":{"line_items":...}}

  ○ No tools called
    The agent responded from its system prompt knowledge only.
    If a tool should have fired, check its description is specific enough.
```

Show ALL tool calls in the session (not just last 3). Show:
- Tool name + success/error icon
- Args the LLM passed
- Result snippet (first 300 chars) or error message
- Explicit "No tools called" message when the agent responded without any tool invocation

This is the most critical diagnostic for non-developers iterating on tool descriptions.

---

## Plan 1 Files

| File | Change |
|---|---|
| V018 migration | 5 template SkillSet rows (IsActive=0) + `AppAgentSkillSetHistory` table |
| `AppAgentSkillSetHistoryBL.cs` (new) | `GetRecent`, `Insert`, `Prune` methods |
| `AgentSkillSetController.cs` | Add `GetTemplates()`, `GetPromptHistory(string skillKey)` |
| `agentSkillSetSvc.ts` | Add `GetTemplates`, `GetPromptHistory` methods |
| `AgentSkillSetManagement.tsx` | "+New from Template" dropdown; structured prompt sections; History dropdown + Restore |
| `GenericAgentChat.tsx` | Full Tool Activity sidebar with args + result + "no tools called" message |

---

## Plan 1 Implementation Sequence

| Day | Work |
|---|---|
| 1 | V018 migration (templates + history table); `AppAgentSkillSetHistoryBL.cs` |
| 1 | `GetTemplates` + `GetPromptHistory` endpoints |
| 2 | "+New from Template" picker in agent list; structured prompt sections (Role/Workflow/Rules/Output) |
| 2 | History dropdown + Restore in agent editor |
| 3 | Tool Activity sidebar in `GenericAgentChat.tsx` (args + result + "no tools called") |
| 3 | E2E verification |

---

## Plan 1 Verification

1. **Template:** Click "+New → API Integration Agent" → SystemPrompt pre-filled → SkillKey blank → fill in, save → run → agent behaves according to template.
2. **Structured sections:** Edit Role section → save → SystemPrompt stored with `## Role` header → reload → sections split correctly.
3. **Version history:** Save prompt → break it → save again → open History → restore previous version → prompt recovered.
4. **Tool diagnostic:**
   - Tool fires correctly → shows green chip with args + result snippet
   - Tool fires with error → shows red chip with error body
   - Agent replies without calling a tool → shows "No tools called" message with hint
5. **No-code full journey:** Non-developer opens "+New → API Integration Agent" → edits Role section → subscribes to `shopify-products` library → adds one private SqlQuery tool using schema browser → runs → sees tool firing in Tool Activity → iterates description → all without writing any code or SQL schema knowledge.

---

## Credential Management Note (Known Limitation)

HttpRest ToolConfig stores API keys as plain text in the tenant DB (e.g., `X-Shopify-Access-Token`).
Per-tenant DB isolation prevents cross-tenant exposure. Any admin with UI access can view the key.
Future upgrade path: `AppAgentCredential` encrypted store + `TokenStoreKey` reference in ToolConfig
(the field is already reserved in `HttpRestToolExecutor`). Out of scope for these two sprints.
