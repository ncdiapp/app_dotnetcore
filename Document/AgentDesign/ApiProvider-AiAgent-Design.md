# API Provider / API Management Agent — Design

**Status:** Phase 1 (library + agent seed) + Phase 2 (API MANAGEMENT Configurator dock) implemented.  
**Date:** 2026-11-25; Phase 2 dock 2026-11-25  
**Related:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md), [SearchViewConfig-AiAgent-Design.md](./SearchViewConfig-AiAgent-Design.md), [SharedAgentEngine-Architecture.md](./SharedAgentEngine-Architecture.md), [ToolNameConvention-ProviderLimits.md](./ToolNameConvention-ProviderLimits.md), [Document/APIManagement/01-Api-Editor-Config-And-Mcp.md](../APIManagement/01-Api-Editor-Config-And-Mcp.md), [Document/Design/OpenAPI-Documents-Management.md](../Design/OpenAPI-Documents-Management.md), [Document/mcp/mcp-gateway-architecture.md](../mcp/mcp-gateway-architecture.md)  
**UI surfaces:** Sidebar **API MANAGEMENT** — 3rd Party API Provider, App API Provider, OpenAPI (Swagger) Documents.

---

## 0. Dependency / revisit gate

| # | Gate | Status |
|---|------|--------|
| 1 | Form / Search Configurator patterns (library + SkillSet + `ask_user` / `confirmed` / `dryRun`) accepted | Passed |
| 2 | Existing execute library `platform-api-provider` (search → detail → execute) healthy | Passed (V045; General + Form subscribe) |
| 3 | CRUD BL exists: `AppIntergrationSettingBL`, `AppStoredProcedureApiBL`, `AppOpenApiDocumentBL` — no new persistence layer | Passed |
| 4 | Product lock: **one Config Agent**, execution stays **Library only**, tool breadth **FULL** | Passed (2026-11-25) |

---

## 1. Why

Users configure and run APIs today via **API MANAGEMENT** UI:

| Menu | Job |
|------|-----|
| App API Provider | Generate / edit APIs from Data Model, Search/View, SQL query, Stored Procedure, Excel import |
| 3rd Party API Provider | Providers (base URL, keys/tokens) + HTTP operations |
| OpenAPI (Swagger) Documents | Compose, regenerate, publish docs from selected ActionCodes |

Agents today can **search / detail / execute** published APIs (`platform-api-provider`) but **cannot** configure providers, create App/3rd-party APIs, or manage OpenAPI documents.

We need a specialist that owns durable **API Management configuration**, while keeping **runtime invoke** as a reusable library for General Agent, Form Agent, and this Config Agent (test path).

---

## 2. Goal / Non-goals

### Goal

- Managed SkillSet **API Provider Configurator Agent** (`app-api-configurator`) that can:
  - Query and CRUD **3rd Party providers** and their API operations
  - Query and CRUD **App API Provider** operations (all create types in FULL)
  - Query and CRUD **OpenAPI documents** (members, regenerate, publish)
  - Test an operation (UI “Send Request” equivalent) after confirmation when mutating
- Keep **`platform-api-provider`** as the shared **execute** library (no dedicated Execution Agent).

### Non-goals (v1)

- Replacing API Management screens (parallel enhancement only).
- Dedicated **API Execution Agent** (execution = library subscription only).
- MCP grant / group admin (`AppMcpExposedApi`) as primary scope — remains MCP Admin UI; per-row `IsUsedByMcp` may be set when saving ops/docs but grant matrix is out of scope.
- Unifying MCP Gateway `operationId` stack with internal `actionCode` DataIntegration path.
- JSON-file / staging-table **import pipeline** as primary tools (helpers may come later; FULL App API types = Data Model, Search/View, SQL, SP, Excel, Standard/3rd-party HTTP).

---

## 3. Locked decisions (2026-11-25)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Config agent count | **One** SkillSet covering App Provider + 3rd Party + OpenAPI docs |
| 2 | Execution | **Library only** — `platform-api-provider`; **no** `api-execution-agent` |
| 3 | Tool breadth | **FULL** — all App API create types + provider/op CRUD + OpenAPI lifecycle + deletes |
| 4 | Identity | AgentCode / SkillKey **`app-api-configurator`**. Display name **API Provider Configurator Agent** |
| 5 | Config library | **`platform-api-integration-config`** (new BuiltIn) |
| 6 | Execute library | Existing **`platform-api-provider`** (unchanged contract; optional later `api-provider-validate-input`) |
| 7 | Write model | Durable DB write via existing BL. **`ask_user` before every durable write / delete / publish / regenerate-that-overwrites**. Mutate tools: `dryRun` / `confirmed` triad (Form/Search pattern) |
| 8 | Secrets | Never echo raw keys/tokens in tool results; redact on get; accept write-only secret fields on upsert |
| 9 | Menu | **Listed** in Agent Management and left **AI Agent** menu (unlike form/search host-only configurators). Admin / API Management workflow is standalone |
| 10 | HostContext | **Optional**. When opened from an API editor page, may pass `providerId` / `settingParameterId` / `openApiDocId`; mutate tools still accept explicit ids. Test Run without host requires ids |
| 11 | Built-in provider | App API Provider id **`1`** (`AppBuiltInProviderId`) — never delete provider row; delete **operations** only |
| 12 | MCP | Out of primary scope; do not filter catalog by `IsUsedByMcp` |

---

## 4. Naming

| Field | Value |
|-------|--------|
| Display name | API Provider Configurator Agent |
| AgentCode / SkillKey | `app-api-configurator` |
| Config LibraryKey | `platform-api-integration-config` |
| Execute LibraryKey | `platform-api-provider` |
| Plugin | `ApiIntegrationConfigPlugin` |
| Constants | `ApiProviderConfigConstants` |

**Tool names:** `api_cfg_*` / `openapi_*` (letters, digits, `_`; fit behind Semantic Kernel `tools_` prefix ≤ 64). Do not put `_` in the SK plugin registration name (library tools use fixed plugin `tools`).

---

## 5. Architecture (split of responsibility)

```
┌──────────────────────────────────────────────────────────────────┐
│ API MANAGEMENT UI (human)                                        │
│  App API Provider | 3rd Party Provider | OpenAPI Documents       │
└──────────────────────────────────────────────────────────────────┘
         │ parallel
         ▼
┌──────────────────────────────────────────────────────────────────┐
│ app-api-configurator                                             │
│  └─ platform-api-integration-config  (CRUD + test)               │
│  └─ platform-api-provider            (search/detail/execute)     │
│  └─ platform-tenant-catalog          (optional: DS list)         │
│  └─ hitl ask_user                                                │
└──────────────────────────────────────────────────────────────────┘

General Agent / Form Agent (unchanged)
  └─ platform-api-provider only (invoke published APIs)

MCP Gateway (external clients) — separate stack (Swagger operationId)
```

| Agent / library | Job |
|-----------------|-----|
| `app-api-configurator` | Durable API Management config + optional test |
| `platform-api-provider` | Find / inspect / execute published `actionCode` APIs |
| `__general_agent__` / Form Agent | Subscribe execute library; **not** config library by default |

---

## 6. HostContext (optional)

```json
{
  "surface": "api-management",
  "mode": "api-configurator",
  "providerId": 12,
  "providerKind": "ThirdParty",
  "settingParameterId": 4401,
  "actionCode": "GetOrders",
  "openApiDocId": 3,
  "saasApplicationId": 12
}
```

- Prefer HostContext ids when present and client-supplied ids must match or return `{ ok: false }`.
- Without HostContext (Agent Management chat): require explicit ids / ActionCodes on mutate tools.

---

## 7. Tools

### 7.1 Execute library — `platform-api-provider` (existing)

| ToolName | Purpose |
|----------|---------|
| `api-provider-search` | Keyword search published App + 3rd-party ops |
| `api-provider-detail` | INPUT schema / sample / method / confirm flag |
| `api-provider-execute` | Run via DataIntegration; writes need `confirmed` |

SOP: `search → detail → (ask missing INPUT) → execute`.

Optional later (not blocking v1): `api-provider-validate-input`.

### 7.2 Config library — `platform-api-integration-config` (FULL)

HITL on every durable write: **Preview (`dryRun` or `confirmed!=true`) → `ask_user` → `confirmed=true`**.

#### Providers

| ToolName | Purpose |
|----------|---------|
| `api_cfg_list_providers` | App built-in + all 3rd-party (summary; secrets redacted) |
| `api_cfg_get_provider` | One provider + nested API list summary |
| `api_cfg_upsert_provider` | Create/update **3rd-party** provider (base URL, auth headers/tokens write-only) |
| `api_cfg_delete_provider` | Delete 3rd-party provider (**forbidden** for id=1) |

#### Operations (APIs)

| ToolName | Purpose |
|----------|---------|
| `api_cfg_list_apis` | Filter by `providerId`, `apiType`, keyword |
| `api_cfg_get_api` | Full op config (sanitized); prefer `settingParameterId` or `actionCode` |
| `api_cfg_upsert_api` | Create/update Standard / 3rd-party HTTP op (JSON patch / dtoJson) |
| `api_cfg_delete_api` | Delete one op by id |
| `api_cfg_batch_delete_apis` | Batch delete by ids |
| `api_cfg_create_from_datamodel` | Ensure GET/POST App Data Model APIs for `transactionId` (wraps `GetAppTransactionDefaultProviderApi`) |
| `api_cfg_create_from_search` | Ensure App Report/View (Search) API for `searchId` |
| `api_cfg_create_sql_query` | Create/update SQL JSON Query API (`IsSimpleQuery`) via dtoJson / scaffold |
| `api_cfg_list_sp_catalog` | List SPs for a data source (builder catalog) |
| `api_cfg_create_from_sp_batch` | `BatchCreate` SP APIs |
| `api_cfg_create_excel_import` | Create/update Excel Import Update Data API (bind dataset id + mapping via dtoJson) |
| `api_cfg_create_tx_simple_query` | `GenerateTransactionProvideSimpleQueryAPI` |
| `api_cfg_test_api` | `ExecuteOneOperationWithTestParameters` (simulate optional); mutating ops need confirm |
| `api_cfg_api_where_used` | Transactions + Searches referencing an API |

#### OpenAPI documents

| ToolName | Purpose |
|----------|---------|
| `openapi_list` | List documents (no giant JSON body) |
| `openapi_get` | One doc + members (+ optional truncated OpenApiJson) |
| `openapi_list_selectable` | Selectable ActionCodes (`mcpOnly` optional) |
| `openapi_upsert` | Save metadata + member ActionCodes (`SaveRequest`) |
| `openapi_delete` | Delete document |
| `openapi_set_published` | Publish / unpublish |
| `openapi_regenerate` | Rebuild OpenAPI JSON from members |

### 7.3 Subscriptions for `app-api-configurator`

| LibraryKey | Why |
|------------|-----|
| `platform-api-integration-config` | Primary config tools |
| `platform-api-provider` | Discover + execute after configure / for “call this API” in same chat |
| `platform-database` | `list_tenant_data_sources` / connection test when scaffolding |
| `platform-application` | `list_tenant_saas_applications` when scaffolding |

Engine built-in: `ask_user` (HITL).

---

## 8. System prompt (outline)

You are the **API Provider Configurator Agent**. You configure **API MANAGEMENT**: App API Provider, 3rd Party API Providers, and OpenAPI (Swagger) documents.

1. **Inspect first** — `api_cfg_list_*` / `api_cfg_get_*` / `openapi_*` / `api-provider-search`.
2. **Never invent** ActionCodes, provider ids, or secrets — read tools or ask_user.
3. **Every durable write** — preview → ask_user → call again with `confirmed=true`.
4. **App APIs** — prefer typed create tools (`api_cfg_create_from_datamodel`, `_from_search`, `_from_sp_batch`, `_create_sql_query`, `_create_excel_import`) over raw upsert when the user names a source object.
5. **3rd Party** — upsert provider (auth) then upsert operations (method, path, headers, body sample).
6. **OpenAPI** — upsert members → regenerate → set_published when asked.
7. **Execute** — for “call an API”, use `api-provider-detail` then `api-provider-execute` (or `api_cfg_test_api` for editor test params). Writes need confirmation.
8. **Do not** manage MCP grants/groups; do not delete App built-in provider (id=1).

---

## 9. Security

| Rule | Detail |
|------|--------|
| HITL | All create/update/delete/publish/regenerate |
| Redaction | Strip Authorization / ApiKey / Token / Password / ClientSecret from get/list results |
| App provider | Cannot `api_cfg_delete_provider(1)` |
| Execute writes | Existing `api-provider-execute` confirm path |
| Auth | Same tenant session as Integration / OpenAPI controllers |

---

## 10. UX / entry points

| Entry | Behavior |
|-------|----------|
| Left menu **AI Agent** → API Provider Configurator | Full chat; ids explicit |
| Agent Management → Test Run | Same |
| **API MANAGEMENT pages** (Phase 2) | Right **Configurator** dock when `IsAiConfigured`; HostContext + `request_api_mgmt_reload` |

### 10.1 Phase 2 dock

| Piece | Detail |
|-------|--------|
| SkillKey | `app-api-configurator` |
| Chat ensure | `POST EnsureApiProviderConfigChat` — scope e.g. `list:app-api`, `provider:12`, `api:4401`, `openapi:3` |
| UI | `ApiProviderConfiguratorLayout` + `ApiProviderConfiguratorDock` |
| Hosted pages | App API Provider list + all App API editors; 3rd-party list/editor/API editor; OpenAPI list/editor |
| Gate | Dock shown only when `UserContext.IsAiConfigured` |
| Reload | Tool `request_api_mgmt_reload` → host confirms Reload (dirty gate mirrors Form/Search) |

---

## 11. Implementation plan

| Step | Deliverable |
|------|-------------|
| 1 | This design doc |
| 2 | `ApiProviderConfigConstants`, `ApiIntegrationConfigPlugin`, helper BL (sanitize, Ok/Fail, HostContext resolve) |
| 3 | Migration **V089** — library tools + SkillSet + subscriptions |
| 4 | Protect AgentCode from rename/delete if product has reserved-key list; **do not** exclude from `GetActiveAgents` |
| 5 | Smoke: list providers → create from datamodel (dryRun/confirm) → openapi upsert → regenerate → api-provider-search finds new ActionCode |
| 6 | Phase 2: EnsureApiProviderConfigChat + dock on API MANAGEMENT pages + `request_api_mgmt_reload` (in V089) |

---

## 12. Out of scope / later

- Semantic hybrid search for `api-provider-search` (MCP-style RAG)
- MCP Admin agent tools
- JSON staging import / schema DDL pipeline as agent tools
- `api-provider-validate-input`

---

## 13. Glossary

| Term | Meaning |
|------|---------|
| ActionCode | Public route key under `/webapi/DataIntegration/{actionCode}` |
| Provider id 1 | App API Provider (built-in) |
| ApiOperation | `MappingInternalCode` for published callable APIs |
| FULL | All App create types + 3rd-party CRUD + OpenAPI lifecycle + deletes |
