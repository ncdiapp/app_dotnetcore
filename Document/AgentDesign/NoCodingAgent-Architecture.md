# No-Code Agent Builder — Technical Architecture

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-09-08  
**Status:** Implemented  
**Audience:** Backend and frontend developers  
**Depends on:** `GenericAgent-Architecture.md` (base platform)

---

## 1. System Overview

This document covers the **Tool Library subsystem** and the **AI Generate endpoint** introduced in Plan 1 / Plan 2. The base `GenericAgentEngine` execution path is documented in `GenericAgent-Architecture.md`.

```
┌─────────────────────────────────────────────────────────────────────┐
│  Agent Management UI  (AgentSkillSetManagement.tsx)                 │
│                                                                     │
│  [Agent Editor]   [Tools Tab]   [MCP Tab]   [Libraries Tab]         │
│       │                                         │                   │
│  ✨ AI Generate modal                    Domain/Library/Subs panels │
└──────┬──────────────────────────────────────────┬───────────────────┘
       │                                          │
       │  POST GenerateAgentDesign                │  GET/POST/DELETE
       │  GET  GetAvailableBuiltInTools           │  Domain, Library, Subscription endpoints
       ▼                                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│  AgentSkillSetController  (AppAI.Web/Controllers/)                  │
│  Route: webapi/AgentSkillSet/[action]                               │
└──────┬──────────────────────────────────────────┬───────────────────┘
       │                                          │
       ▼                                          ▼
┌─────────────────────┐                ┌──────────────────────────────┐
│  LLMProviderHelper  │                │  AppAgentToolLibraryBL       │
│  .CallLLMAsync()    │                │  GetAllLibraries             │
│  (DbGenie namespace)│                │  GetAllDomains               │
│                     │                │  GetSubscriptions            │
│  AIConfigSettingBL  │                │  SetSubscriptions            │
│  .GetModel()        │                │  GetAvailableBuiltInTools    │
└─────────────────────┘                └──────────────────────────────┘
                                                  │
                                                  ▼
                                       ┌──────────────────────────────┐
                                       │  GenericAgentEngine          │
                                       │  (at session start)          │
                                       │  GetBySkillKeyWithLibraries  │
                                       │  = agent tools UNION         │
                                       │    subscribed library tools  │
                                       └──────────────────────────────┘
```

---

## 2. Database Schema

### New Tables (V017 migration)

```sql
-- Level 1: Domain (grouping concept, e.g. "database-queries", "external-rest")
CREATE TABLE dbo.AppAgentToolDomain (
    DomainKey   NVARCHAR(100) NOT NULL,
    DomainName  NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_AppAgentToolDomain PRIMARY KEY (DomainKey)
);

-- Level 2: Library (a named collection of tools, e.g. "platform-queries")
CREATE TABLE dbo.AppAgentToolLibrary (
    LibraryKey   NVARCHAR(100) NOT NULL,
    DomainKey    NVARCHAR(100) NOT NULL,
    LibraryName  NVARCHAR(200) NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    ToolCategory NVARCHAR(50)  NULL,   -- UI hint: SqlQuery / HttpRest / BuiltIn / Mcp
    IsActive     BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_AppAgentToolLibrary PRIMARY KEY (LibraryKey),
    CONSTRAINT FK_AppAgentToolLibrary_Domain
        FOREIGN KEY (DomainKey) REFERENCES dbo.AppAgentToolDomain(DomainKey)
);

-- Level 3: Subscription (many-to-many: agent SkillKey subscribes to LibraryKey)
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
```

**Key insight:** `AppAgentToolRegister.SkillKey` has no FK constraint — it's a plain `NVARCHAR(100)`. A `LibraryKey` is just a `SkillKey` for rows that belong to that library. No existing tables were modified.

### Existing Table: AppAgentSkillSetHistory (V018 migration)

```sql
CREATE TABLE dbo.AppAgentSkillSetHistory (
    HistoryId    INT IDENTITY(1,1) PRIMARY KEY,
    SkillKey     NVARCHAR(100) NOT NULL,
    SystemPrompt NVARCHAR(MAX) NOT NULL,
    SavedAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    SavedBy      NVARCHAR(200) NULL
);
```

Pruned to the last 10 entries per agent on each insert.

---

## 3. Tool Merge at Session Start

When `GenericAgentEngine` loads tools for a session, it calls `GetBySkillKeyWithLibraries` instead of the old `GetBySkillKey`:

```sql
-- Agent-owned tools (IsLibraryTool = 0) come first so they win dedup
SELECT t.*, 0 AS IsLibraryTool
FROM dbo.AppAgentToolRegister t
WHERE t.SkillKey = @SkillKey AND t.IsActive = 1

UNION ALL

-- Library tools from all subscribed libraries
SELECT t.*, 1 AS IsLibraryTool
FROM dbo.AppAgentToolRegister t
INNER JOIN dbo.AppAgentLibrarySubscription s ON t.SkillKey = s.LibraryKey
WHERE s.SkillKey = @SkillKey AND t.IsActive = 1

ORDER BY IsLibraryTool, ToolRegisterId
```

**Dedup in C# (agent-owned wins on name collision):**

```csharp
var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
return rows.Where(r => seen.Add(r.ToolName)).ToList();
// IsLibraryTool=0 rows are first — agent-owned always added first
```

The same UNION pattern applies to `AppAgentMcpServer` rows (`GetMcpBySkillKeyWithLibraries`).

**Backward compatibility:** Agents with zero subscriptions produce an empty UNION branch → identical result to the old `GetBySkillKey`. No behavior change for existing agents.

---

## 4. AI Generate Endpoint

### Request / Response

```
POST /webapi/AgentSkillSet/GenerateAgentDesign
Content-Type: application/json

{ "Description": "An agent that helps warehouse managers..." }

→ OperationCallResult<GenerateAgentResult>
  {
    "Object": {
      "SystemPrompt": "## Role\n...\n## Workflow\n...",
      "RecommendedLibraryKeys": ["platform-queries"],
      "RecommendedBuiltInToolNames": ["execute_sql", "get_database_tables"]
    },
    "IsSuccessful": true,
    "ValidationResult": { "Items": [], "IsValid": true }
  }
```

### C# Implementation (`AgentSkillSetController.GenerateAgentDesign`)

```csharp
[HttpPost]
public async Task<OperationCallResult<GenerateAgentResult>> GenerateAgentDesign(
    [FromBody] GenerateAgentRequest req)
{
    // 1. Load both catalogs from the actual DB — never hardcode
    var libraries = LibBL.GetAllLibraries(dsId);
    var builtIns  = LibBL.GetAvailableBuiltInTools(dsId);

    // 2. Build catalog strings for the meta-prompt
    var libCatalog  = string.Join("\n", libraries.Select(l =>
        $"- {l.LibraryKey} ({l.ToolCategory}/{l.DomainKey}): {l.LibraryName} — {l.Description}"));
    var toolCatalog = string.Join("\n", builtIns.Select(t =>
        $"- {t.ToolName}: {t.ToolDescription}"));

    // 3. Meta-prompt instructs LLM to output JSON only, pick from catalogs only
    var metaPrompt = $@"...catalogs injected...
Output ONLY valid JSON:
{{ ""SystemPrompt"": ""..."",
   ""RecommendedLibraryKeys"": [...],
   ""RecommendedBuiltInToolNames"": [...] }}";

    // 4. Call tenant-configured LLM
    var llmReq = new LLMRequestDto {
        Provider     = LLMProviderHelper.GetConfiguredProvider(),
        ApiKey       = LLMProviderHelper.GetConfiguredApiKey(),
        Model        = AIConfigSettingBL.GetModel(),
        SystemPrompt = metaPrompt,
        Prompt       = req.Description,
        MaxTokens    = 2048,
    };
    var llmRes = await LLMProviderHelper.CallLLMAsync(llmReq);

    // 5. Strip markdown fences if LLM wraps in ```json...```
    var raw = llmRes.Content?.Trim() ?? "";
    if (raw.StartsWith("```"))
        raw = Regex.Replace(raw, @"^```[a-z]*\r?\n?|```$", "", RegexOptions.Multiline).Trim();

    // 6. Deserialize; fallback: raw text as SystemPrompt, empty arrays
    result.Object = JsonSerializer.Deserialize<GenerateAgentResult>(raw, options)
        ?? new GenerateAgentResult(raw, new List<string>(), new List<string>());
}

public sealed class GenerateAgentRequest { public string Description { get; set; } }
public sealed record GenerateAgentResult(
    string       SystemPrompt,
    List<string> RecommendedLibraryKeys,
    List<string> RecommendedBuiltInToolNames);
```

### Infrastructure Reused

| Concern | Class | Namespace |
|---|---|---|
| LLM call | `LLMProviderHelper.CallLLMAsync(LLMRequestDto)` | `App.BL.DbGenie` |
| Configured model | `AIConfigSettingBL.GetModel()` | `App.BL.GenericAgent` |
| LLM request DTO | `LLMRequestDto` | `APP.Components.Dto` |
| Tool library catalog | `AppAgentToolLibraryBL.GetAllLibraries(dsId)` | `App.BL.TenantBusiness` |
| Built-in tool catalog | `AppAgentToolLibraryBL.GetAvailableBuiltInTools(dsId)` | `App.BL.TenantBusiness` |
| Response wrapper | `OperationCallResult<T>` | `APP.Framework.Communication` |

---

## 5. Backend BL Layer

### `AppAgentToolLibraryBL` (new, `APP.BL/TenantBusiness/`)

All methods use the same `DatabaseFixture` pattern as `AppAgentToolRegisterBL` — parameterized raw SQL, no ORM.

| Method | SQL Summary |
|---|---|
| `GetAllDomains(dsId)` | `SELECT * FROM AppAgentToolDomain ORDER BY SortOrder` |
| `UpsertDomain(dsId, dto)` | `IF EXISTS UPDATE ELSE INSERT` |
| `DeleteDomain(dsId, key)` | `DELETE FROM AppAgentToolDomain` |
| `GetAllLibraries(dsId)` | `SELECT lib.*, COUNT(t.ToolRegisterId) AS ToolCount FROM AppAgentToolLibrary lib LEFT JOIN AppAgentToolRegister t ON t.SkillKey = lib.LibraryKey GROUP BY ...` |
| `GetLibrariesByDomain(dsId, domainKey)` | Same as above + `WHERE DomainKey = @DomainKey` |
| `SearchLibraries(dsId, query)` | LIKE on LibraryName + Description + DomainName |
| `GetLibraryToolPreview(dsId, libraryKey)` | `SELECT ToolName, ToolDescription FROM AppAgentToolRegister WHERE SkillKey = @LibraryKey` — **no ToolConfig** (security: config may contain API keys) |
| `UpsertLibrary(dsId, dto)` | IF EXISTS UPDATE ELSE INSERT |
| `DeleteLibrary(dsId, key)` | 3-step: DELETE McpServer → DELETE ToolRegister → DELETE Library (cascade handles subscription rows) |
| `GetSubscriptions(dsId, skillKey)` | `SELECT * FROM AppAgentLibrarySubscription WHERE SkillKey = @SkillKey` |
| `SetSubscriptions(dsId, skillKey, keys)` | DELETE all for skillKey + batch INSERT new keys |
| `GetAvailableBuiltInTools(dsId)` | `SELECT ToolName, ToolDescription, ToolConfig FROM AppAgentToolRegister WHERE ToolType='BuiltIn' AND IsActive=1` |

### `AppAgentSkillSetHistoryBL` (new, `APP.BL/TenantBusiness/`)

| Method | Behavior |
|---|---|
| `GetRecent(dsId, skillKey, limit=10)` | SELECT TOP @limit ORDER BY SavedAt DESC |
| `Insert(dsId, skillKey, prompt)` | INSERT row; DELETE rows beyond limit |

History insert is called from `AppAgentSkillSetBL.UpsertSkillSet` before overwriting `SystemPrompt`.

---

## 6. Controller Endpoints

All under `[Route("webapi/[controller]/[action]")]` on `AgentSkillSetController`.

### Tool Library Endpoints (added Plan 2)

| Method | Route | Purpose |
|---|---|---|
| GET | `GetAllDomains` | Domain list |
| POST | `UpsertDomain` | Create/update domain |
| DELETE | `DeleteDomain?domainKey=` | Remove domain |
| GET | `GetAllLibraries` | Library list with tool counts |
| GET | `GetLibrariesByDomain?domainKey=` | Filter by domain |
| GET | `SearchLibraries?query=` | LIKE search |
| GET | `GetLibraryToolPreview?libraryKey=` | Tool names+desc for a library (no ToolConfig) |
| POST | `UpsertLibrary` | Create/update library |
| DELETE | `DeleteLibrary?libraryKey=` | Cascade delete library |
| GET | `GetSubscriptions?skillKey=` | Agent's current subscriptions |
| POST | `SetSubscriptions` | Replace all subscriptions for an agent |
| GET | `GetAvailableBuiltInTools` | All active BuiltIn tools (for picker + AI Generate) |

### AI Generate Endpoint (added Plan 1 extension)

| Method | Route | Purpose |
|---|---|---|
| POST | `GenerateAgentDesign` | LLM-assisted prompt + tool recommendations |

---

## 7. Frontend Architecture

### Service Layer (`agentSkillSetSvc.ts`)

New TypeScript interfaces:

```typescript
export interface AppAgentToolDomainDto { DomainKey, DomainName, Description, SortOrder, IsActive }
export interface AppAgentToolLibraryDto { LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive, ToolCount }
export interface AppAgentLibrarySubscriptionDto { SkillKey, LibraryKey }
export interface LibraryToolPreviewDto { ToolName, ToolDescription, ToolConfig? }
export interface AppAgentPromptHistoryDto { HistoryId, SkillKey, SystemPrompt, SavedAt, SavedBy? }
export interface GenerateAgentResult {
    SystemPrompt: string;
    RecommendedLibraryKeys: string[];
    RecommendedBuiltInToolNames: string[];
}
```

New service methods (12 library endpoints + `GenerateAgentDesign` + `GetPromptHistory`).

### Component: `AgentSkillSetManagement.tsx`

**State added for AI Generate feature:**

```typescript
const [showAiGenerate, setShowAiGenerate]         = useState(false);
const [aiDescription, setAiDescription]           = useState('');
const [aiGenerating, setAiGenerating]             = useState(false);
const [aiResult, setAiResult]                     = useState<GenerateAgentResult | null>(null);
const [aiAcceptedLibs, setAiAcceptedLibs]         = useState<Set<string>>(new Set());
const [aiAcceptedBuiltIns, setAiAcceptedBuiltIns] = useState<Set<string>>(new Set());
const [allBuiltInTools, setAllBuiltInTools]       = useState<LibraryToolPreviewDto[]>([]);
```

**Two-phase modal flow:**

```
Phase 1 (aiResult === null):
  textarea → description input
  [Cancel]  [✨ Generate]  → calls GenerateAgentDesign, spinner shown

Phase 2 (aiResult !== null):
  read-only textarea → generated SystemPrompt preview
  checkboxes → RecommendedLibraryKeys (filtered against allLibraries catalog)
  checkboxes → RecommendedBuiltInToolNames (filtered against allBuiltInTools catalog)
  static note → MCP servers: manual configuration required
  [← Regenerate]  [Cancel]  [✓ Use this]
```

**`handleApplyAiResult` — "Use this" action:**

```typescript
const handleApplyAiResult = () => {
    // 1. Fill system prompt textarea
    update('SystemPrompt', aiResult.SystemPrompt);

    // 2. Merge accepted library subscriptions (additive, not replace)
    if (aiAcceptedLibs.size > 0) {
        setSubscribedKeys(prev => {
            const next = new Set(prev);
            aiAcceptedLibs.forEach(k => next.add(k));
            return next;
        });
        setSubsChanged(true);
    }

    // 3. Register accepted built-in tools immediately via UpsertTool
    if (aiAcceptedBuiltIns.size > 0) {
        const newTools = allBuiltInTools
            .filter(t => aiAcceptedBuiltIns.has(t.ToolName))
            .map(t => ({ Id: 0, SkillKey: selected!.SkillKey, ToolName: t.ToolName,
                          Description: t.ToolDescription, ToolType: 'BuiltIn',
                          ToolConfig: t.ToolConfig ?? '', IsActive: true, SortOrder: 0 }));
        newTools.forEach(t => agentSkillSetSvc.UpsertTool(t));
        reloadTools();
    }
    setShowAiGenerate(false);
};
```

**Left panel dimensions (as of 2026-09-08):**
- Default width: 300px (was 220px)
- Resize range: 200px – 500px
- Agent Code column: `width="*"` (takes all remaining space; spacer column is `width={20}`)

---

## 8. Security Considerations

| Risk | Mitigation |
|---|---|
| LibraryKey invented by AI (hallucination) | UI filters `RecommendedLibraryKeys` against actual catalog before rendering checkboxes |
| ToolName invented by AI (hallucination) | UI filters `RecommendedBuiltInToolNames` against `allBuiltInTools` before rendering |
| `GetLibraryToolPreview` leaks ToolConfig (may contain API keys) | Endpoint explicitly SELECTs only `ToolName, ToolDescription` — ToolConfig excluded |
| AI Generate using wrong LLM endpoint | Uses same `LLMProviderHelper` as the agent runtime — same tenant config, no separate call path |
| Prompt injection via Description field | Description is placed in the `Prompt` field (user turn), not the `SystemPrompt` — standard LLM boundary |

---

## 9. Files Changed

| File | Change |
|---|---|
| `APP.BL/TenantBusiness/AppAgentToolLibraryBL.cs` | **New** — all domain/library/subscription methods |
| `APP.BL/TenantBusiness/AppAgentSkillSetHistoryBL.cs` | **New** — prompt version history |
| `APP.BL/AIAgent/GenericAgent/AppAgentToolRegisterBL.cs` | **Modified** — added `GetBySkillKeyWithLibraries` |
| `APP.BL/AIAgent/GenericAgent/AppAgentMcpServerBL.cs` | **Modified** — added `GetMcpBySkillKeyWithLibraries` |
| `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs` | **Modified** — swap to `WithLibraries` methods; prompt history snapshot on save |
| `APP.BL/AIAgent/GenericAgent/AppAgentSkillSetBL.cs` | **Modified** — calls `AppAgentSkillSetHistoryBL.Insert` before overwriting SystemPrompt |
| `AppAI.Web/Controllers/AgentSkillSetController.cs` | **Modified** — 12 library endpoints + `GetAvailableBuiltInTools` + `GenerateAgentDesign` |
| `AppReact/src/webapi/agentSkillSetSvc.ts` | **Modified** — DTOs and service methods for all new endpoints |
| `AppReact/src/components/aiskill/AgentSkillSetManagement.tsx` | **Modified** — AI Generate modal, subscription browser, history UI, wider left panel |

---

## 10. Extension Points

| Future Capability | Current Hook |
|---|---|
| Encrypted credential store for HttpRest | `ToolConfig.TokenStoreKey` field reserved in `HttpRestToolExecutor` |
| Agent-level MCP server discovery | `GetMcpBySkillKeyWithLibraries` already exists — just add library MCP rows |
| AI Generate for tool description | Same `GenerateAgentDesign` pattern reusable for per-tool description suggestions |
| Template seeding via library | Libraries with `ToolCategory='Template'` could seed agent configs without code changes |
