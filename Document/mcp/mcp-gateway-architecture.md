# MCP Gateway Architecture

How AppAI.Web exposes its own APIs (and other Swagger-described APIs) to external AI clients
(Claude Desktop, ChatGPT Desktop, n8n) through one MCP endpoint, with per-user authentication,
role-based API access and tenant-isolated audit.

> Companion to `mcp-gateway-merge-plan.md` (decisions, phases, status history).
> This document describes the code as it is. Verified by reading the source; the end-to-end flow
> has **not** been run against a real database yet (see [Verification status](#verification-status)).
> Section 14 (Security design) also lists **planned** controls (OAuth, delegated agent tokens);
> Section 14 marks every control [implemented], [gap] or [partly]. For how far each part was verified, see `mcp-oauth-plan.md` section 7g: the OAuth protocol was exercised over HTTP, but the code that uses AppAI sessions and the React pages were only compiled.

---

## 1. What it is

An in-process MCP server hosted inside AppAI.Web at `/appai/mcp` (HTTP transport).
It does not expose one MCP tool per API. Instead it exposes a small, fixed set of tools that let an
LLM **search** the API catalogue, **read** an endpoint's contract, and **execute** it:

```
External AI client ──(IntergrationAccessToken header)──► /mcp ──► search / details / execute ──► AppAI /webapi/* (or other source)
```

This keeps the tool list constant (cheap for the model's context) while the set of reachable APIs
is controlled per user by the admin.

---

## 2. Components

| Layer | Location | Responsibility |
|---|---|---|
| Host wiring | `AppAI.Web/Mcp/McpGatewayExtensions.cs` | `AddMcpGateway()` (DI, HttpClients, rate limiter, MCP server) and `UseMcpGatewayEarly()` / `UseMcpGateway()` (pipeline) |
| Token middleware | `AppAI.Web/Auth/IntegrationTokenMiddleware.cs` | Authenticates every `/mcp*` request |
| Gateway library | `APP.McpGateway/` (assembly/namespace `McpGateway`) | Tools, Swagger index, semantic search, API client, caches, audit queue, access policy |
| Host adapters | `AppAI.Web/Services/` | `McpApiAccessProvider` (reads grants), `McpTenantAuditSink` (writes audit), `McpSwaggerSetup` (OpenAPI for AppAI controllers) |
| Business logic | `APP.BL/` | `AppSaasUserSessionMgtBL.TryRegisterIntegrationTokenIdentity`, `McpAuditBL`, `McpApiAccessBL` |
| Admin API | `AppAI.Web/Controllers/McpManagementController.cs` | Catalogue and group grants (company admin only) |
| Admin UI | `AppReact/src/components/admin/CompanySecuritySetting/CompanyMcpApiAccess.tsx`, `webapi/mcpManagementSvc.ts` | "MCP API Access" tab |
| Token UI | `.../CompanyIntegrationTokenManagement.tsx` | "Integration Tokens" tab (issue / rotate / expire) |
| Schema | `AppAI.Web/Migrations/V046__McpAuditLog.sql`, `V047__McpExposedApi.sql` | Tenant DB tables |
| Tests | `APP.McpGateway.Tests/` | xUnit + Moq |
| OAuth server | `APP.OAuth/` (library), `APP.OAuth.Tests/`, `APP.OAuth.E2E/` (developer tool) | OpenIddict configuration, own ADO.NET stores, consent tickets, dynamic registration, bearer and agent-token authentication, admin services; 108 tests; HTTP flow script |
| OAuth host parts | `AppAI.Web/Controllers/OAuthConsentController.cs`, `OAuthAdminController.cs`, `Services/OAuthIdentityBridge.cs`, `Mcp/OAuthAgentWiring.cs`; `APP.BL/MasterAuth/AppOAuthGrantSessionBL.cs` | Session-authenticated consent and admin APIs; grant sessions in the master DB |
| OAuth screens | `AppReact/src/components/mcp/` (`McpAuthorize`, `AiConnections`, `OAuthClients`) | Consent page, connections lists, OAuth clients |
| OAuth schema | `Document/Design/MasterDB_OAuthTables.sql` | Master DB tables `AppOAuthApplication/Scope/Authorization/Token` |

The gateway library cannot reference AppAI.Web, so the two sides communicate through interfaces
(`IAuditSink`, `IApiAccessProvider`) implemented in the host, and through `HttpContext.Items`
keys published by the middleware and read by `IMcpCallerContext`.

### Project references

```
AppAI.Web ───────► APP.McpGateway     (AppAI.Web.csproj line 78)
AppAI.Web ───────► APP.BL.Core        (AppAI.Web.csproj line 71)
APP.McpGateway.Tests ─► APP.McpGateway
APP.McpGateway ──✗──► AppAI.Web, APP.BL   (no ProjectReference; NuGet packages only)
```

The dependency is one-way. AppAI.Web must reference the gateway because it implements the
gateway's interfaces and registers its classes in `AddMcpGateway()`. The gateway cannot reference
AppAI.Web (circular) and deliberately does not reference `APP.BL`, so it has no access to LLBLGen,
tenant adapters or the AppAI session code. Everything it needs from them goes through the
interfaces below.

| Interface | Defined in | Implemented in | Implementation |
|---|---|---|---|
| `IAuditSink` | `APP.McpGateway` | **AppAI.Web** | `McpTenantAuditSink` (`AppAI.Web/Services/`) → `McpAuditBL` in `APP.BL` |
| `IApiAccessProvider` | `APP.McpGateway` | **AppAI.Web** | `McpApiAccessProvider` (`AppAI.Web/Services/`) → `McpApiAccessBL` in `APP.BL` |
| `IMcpCallerContext` | `APP.McpGateway` | **`APP.McpGateway`** | `McpCallerContext`, reads `HttpContext.Items` via `IHttpContextAccessor` |

`IMcpCallerContext` is implemented inside the gateway because it needs only ASP.NET Core
(`IHttpContextAccessor`), no database. The AppAI.Web part is `IntegrationTokenMiddleware`, which
writes `IntegrationAccessToken`, `IntegrationUserId` and `IntegrationCompanyId` into
`HttpContext.Items`; `McpCallerContext` reads the same key strings (duplicated, with a comment
that they must match).

All three are registered in `AddMcpGateway()` (`AppAI.Web/Mcp/McpGatewayExtensions.cs`), so the
host does the wiring.

Why the split exists:

- **Boundary**: the gateway cannot touch tenant DB or session code except through these interfaces.
- **Tests**: `APP.McpGateway.Tests` needs no web host or database; the interfaces are mocked.
- **Portability**: the gateway could be hosted elsewhere by supplying other implementations.

Merging the gateway into AppAI.Web is possible (about 39 files, 8 packages, tests would then
reference AppAI.Web), but it would remove this boundary; the current decision is to keep the
projects separate.

---

## 3. Request flow

```
                         ┌────────────────────────────────────────────────────────┐
 Client                  │ AppAI.Web                                              │
   │  POST /appai/mcp    │                                                        │
   │  IntergrationAccess │  CorrelationIdMiddleware ─► RateLimiter (/mcp only)    │
   │  Token: <session>   │        │                                               │
   └────────────────────►│  ... UseAuthentication ...                             │
                         │        │                                               │
                         │  IntegrationTokenMiddleware  (UseWhen /mcp)            │
                         │   • header only, anonymous tokens rejected             │
                         │   • TryRegisterIntegrationTokenIdentity(token)         │
                         │       → AppClientIdentity (user, company, tenant DB)   │
                         │   • Items: token, userId, companyId                    │
                         │   • else 401                                           │
                         │        │                                               │
                         │  MapMcp("/mcp")  → tool call                           │
                         │        │                                               │
                         │  ┌─────▼───────────────────────────────┐               │
                         │  │ IMcpCallerContext.RequireIdentity() │               │
                         │  │ IApiAccessPolicy (allowed ops)      │               │
                         │  │ ISwaggerService / SemanticSearch    │               │
                         │  │ IApiClient ─► downstream HTTP       │──► AppAI /webapi/*
                         │  │ IAuditService (queue)               │               │
                         │  └─────────────────────────────────────┘               │
                         └────────────────────────────────────────────────────────┘
```

Pipeline order in `Program.cs`: `UseMcpGatewayEarly` (line 162) → … → `UseAuthentication` (234) →
`UseMcpGateway` (238). The token middleware therefore runs **after** the normal authentication
middleware, scoped to `/mcp` with `UseWhen`.

---

## 4. Authentication

- **Credential**: header `IntergrationAccessToken` (spelling intentional, matches the rest of the codebase).
  Header only — a query-string token is never read.
- **Token = an existing session.** Each external user is an `AppSecurityUser` with
  `DomainId = EmAppUserType.Integration` plus an `AppSecurityUserSession`
  (`EmExternalSigninType.Integration = 98`) whose `ExpirationDate` is chosen by the admin.
  Re-saving the token in the admin UI rotates it. No new token table exists.
- **Validation**: `AppSaasUserSessionMgtBL.TryRegisterIntegrationTokenIdentity` checks the session
  exists and is not expired, the user is an active Integration user, and registers the
  `AppClientIdentity` for the request (so tenant DB resolution works exactly as for `/webapi`).
- **Failure** → HTTP 401 `{"error":"IntergrationAccessToken required or invalid"}`.
- **Identity for the gateway** is published only from the validated session
  (`IntegrationToken`, `IntegrationUserId`, `IntegrationCompanyId` in `HttpContext.Items`),
  never from client-supplied headers. `IMcpCallerContext.RequireIdentity()` throws if there is no
  authenticated caller, so there is no shared/"default" bucket anywhere.
- **Token admin endpoints** (`AdministrationController.RetrieveAllIntegrationTokenDto`,
  `SaveOneIntegrationTokenExDto`) are admin-only and company-scoped.

Removed in the merge: shared `X-Api-Key` guard, in-memory `TokenStore`, `api_login` tool,
`/auth/login` browser form, `"default"` session fallback, static caller tokens.

---

## 5. MCP tools

Registered via `WithToolsFromAssembly` on classes marked `[McpServerToolType]`:

| Tool | File | Purpose | Access filtering |
|---|---|---|---|
| `semantic_search_endpoints` | `MCP/Tools/SwaggerTools.cs` | Hybrid (embedding + keyword) search of the API catalogue | Results limited to operations the caller may use |
| `browse_endpoints` | `SwaggerTools.cs` | Browse the catalogue by source/tag | Same |
| `get_endpoint_details` | `SwaggerTools.cs` | Full contract for one operation | Not allowed → "not found" |
| `api_execute` | `MCP/Tools/ApiTools.cs` | Call an operation downstream | Re-checked at call time → "not found" + audit |
| `data_analyze` | `MCP/Tools/DataAnalysisTools.cs` | Query/aggregate a cached result dataset | Per-user / per-company cache |

`AdminTools` (`admin_get_config`, `admin_set_llm_enrichment`) has no `[McpServerToolType]`, so it is
**not** exposed. It is dead code and still can write `appsettings.json`.

---

## 6. Role-based API access

Deny by default. An operation is usable by an external user only if **all** hold:

1. It is **catalogued** in `dbo.AppMcpExposedApi` (V047),
2. `IsEnabled = 1`,
3. The user is in a security group granted on it: `AppMcpExposedApiGroup.GroupID` →
   `AppSecurityGroup.GroupID`, with membership via `AppSecurityGroupMember(GroupID, UserID)`.

Details:

- Membership is **direct only** (no nesting exists in the schema; organisation/user-type derived
  membership from `AppSecuritySysObjGroupUserBL` is not considered).
- `ApiAccessPolicy` caches the allowed set per company+user for `Mcp:AccessCacheSeconds`
  (default 60s) and clears it immediately when an admin changes grants.
- If grants cannot be loaded, the answer is **deny**.
- `get_endpoint_details` and `api_execute` answer "not found" for disallowed operations,
  identical to a missing one, so operation ids cannot be probed. Each denial writes an
  `A022_AccessDenied` audit row.
- Second layer: the downstream call carries the caller's own identity, so BL-level object
  permissions still apply.

### Management API (`webapi/McpManagement/*`, company admin only)

`GetExposableApis`, `GetSecurityGroups`, `SaveExposedApi`, `DeleteExposedApi`,
`GetUserEffectiveApis` (preview what a user can call), `RefreshApiCatalog`.
The catalogue entry is taken from the server's own spec, so only operations that really exist can
be exposed. Admin changes are audited. A SysAdmin session cannot use it (tables are per tenant).

---

## 7. Downstream calls (`ApiClient`)

- Sources are configured under `ApiSources:Sources` (`SwaggerEndpoint.cs` → `ApiSourceConfig`).
  Each gets a named `HttpClient` with a Polly pipeline: timeout (default 30s), retry ×2 with
  exponential back-off and jitter (on `HttpRequestException` / 5xx), circuit breaker
  (50% failures over 30s, min 5 calls, 30s break).
- `ForwardCallerToken` (default `false`): when `true` the caller's token is sent in
  `AccessTokenHeaderName` (default `IntergrationAccessToken`, which suits systems that read it, such as PLM). For a source that is
  this AppAI instance use **`CurrentUserSessionId`**: that is the header `/webapi` reads. Turn `true` on only for a source that is
  this instance; keep it `false` for systems with their own identity store (PLM has its own master DB and would not recognise an AppAI session).
- `AccessTokenValue` is used **only to download the Swagger spec**, never on caller requests.
- The token is not logged by `ApiClient` or stored in audit.

Example source for this instance:

```json
{ "Name": "AppAI", "BaseUrl": "http://localhost:52740/appai/", "SwaggerJsonPath": "swagger/v1/swagger.json",
  "AccessTokenHeaderName": "CurrentUserSessionId", "ForwardCallerToken": true }
```

---

## 8. OpenAPI for AppAI's own controllers

`McpSwaggerSetup` generates the spec the gateway indexes as the "AppAI" source:

- 875 operations, 0 duplicate operation ids (checked with a throwaway host).
- Only actions with an explicit `[HttpGet]/[HttpPost]/…` attribute are described; add one to expose
  an action.
- `McpManagementController` is excluded from the exposable spec.
- `/swagger/*` is served to **loopback only** (404 for other callers). Swagger UI is Development only.

---

## 9. Tenant isolation

| Data | Partitioning |
|---|---|
| Dataset cache (`data_analyze`) | Key `{company}:{user}:{dataset}`; "global" datasets are per company; disk path `{CacheDirectory}/{companyId}/` |
| Dataset names | Validated `[A-Za-z0-9_-]` before touching the file system (path-traversal guard) |
| Allowed-API cache | Per company + user |
| Audit rows | Tenant DB of the caller (`AppMcpAuditLog`) |
| Embedding / enrichment caches | Global — they hold endpoint metadata only |

Global datasets fill on the first `api_execute` per company and reload from disk after restart.
Legacy un-partitioned files in the old flat cache folder are ignored (safe to delete).

---

## 10. Audit

- Table `dbo.AppMcpAuditLog` (V046, tenant DB): CompanyId, UserId, EventCode, Action, Success,
  McpSessionId (informational), IpAddress, CorrelationId, AppSource, HttpMethod, ResourcePath,
  HttpStatus, ErrorMessage, AdditionalContext (JSON). The access token is never stored.
- `QueuedAuditService` implements `IAuditService` and `IHostedService`: stamps company/user from
  the validated caller, buffers in a bounded `Channel` (`Audit:QueueCapacity`, default 10000),
  writes in batches (`Audit:BatchSize`, default 100), counts and logs overflow (`DroppedCount`),
  logs failed writes, and drains the queue on shutdown.
- `McpTenantAuditSink` (host) → `McpAuditBL`: `CaptureTarget()` takes the tenant connection from
  the registered identity on the request thread; `WriteAsync()` runs one parameterized batch insert
  in a transaction. Plain SQL, because new tables have no generated LLBLGen entity.
- Audited: tool calls, access denials (`A022_AccessDenied`), admin changes.
  **Not** audited: 401s at the middleware (no tenant context for a failed token).
- No retention/purge for `AppMcpAuditLog`.

---

## 11. Configuration summary

| Setting | Purpose |
|---|---|
| `ApiSources:Sources[]` | Downstream APIs (Name, BaseUrl, SwaggerJsonPath, AccessTokenHeaderName, ForwardCallerToken, TimeoutSeconds) |
| `ApiSources:StatelessMode` | MCP HTTP transport stateless (default false) |
| `LlmEnrichment:Provider` | `Anthropic` (default) / `OpenAI` / `AzureOpenAI` / `Gemini` — enriches endpoint descriptions |
| `SemanticSearch:Provider` | `None` (default) / `LocalOnnx` / `AzureOpenAI` / `OpenAI` |
| `Mcp:AccessCacheSeconds` | Allowed-API cache TTL (default 60) |
| `Audit:QueueCapacity` / `Audit:BatchSize` | Audit queue sizing |
| Rate limit | 120 requests / minute / IP, sliding window, `/mcp*` only (hard-coded) |

`LocalOnnx` needs `all-MiniLM-L6-v2.onnx` in `APP.McpGateway/EmbedModels/` (git-ignored). With 875
endpoints expect a slower first load. The gateway is always on; there is no feature flag.

---

## 12. Setup checklist

1. Apply V046 and V047 (`POST /webapi/TenantProvisioning/RunMigrations`, or
   `Migrations:RunOnStartup` in Development) and confirm both tables exist.
2. Add the AppAI source under `ApiSources:Sources` (see section 7).
3. Optional: place the ONNX model for `SemanticSearch:Provider=LocalOnnx`.
4. As company admin: create Integration tokens (set expiry), add the integration users to security
   groups, open **MCP API Access**, enable operations and tick the groups.
5. Configure the client with the `/mcp` URL and the `IntergrationAccessToken` header.

---

## 13. Security notes

- Header redaction: `Program.cs` logs `RequestHeaders` in HttpLogging without an allow-list;
  ASP.NET Core redacts unlisted headers, so the token should appear as `[Redacted]`. Confirm at runtime.
- `McpHeaderSecrets.MaskForClient` is only applied in `AgentSkillSetController`; nothing in the
  gateway returns this header to a UI.
- Rate limiting is per IP and scoped to `/mcp` only.
- `/webapi` still accepts Integration tokens via `CurrentUserSessionId` (deliberate decision).
- Secrets for PLM/Azure/SQL previously committed in `appsettings.json` are out of scope here but
  should be rotated.

---

## 14. Security design

Section 14 states the security model as a whole. Each control is marked **[implemented]**,
Each control is marked **[implemented]**, **[partly]** or **[gap]**; see `mcp-oauth-plan.md` section 7g for how each part was verified.

### 14.1 Principles

1. **Every `/mcp` call runs as a known identity.** No anonymous access, no shared or "default" bucket.
2. **Effective permission = the caller's own permission.** An agent or AI client never has more than
   the person or service account behind it.
3. **Deny by default.** An operation is reachable only if catalogued, enabled and granted to one of
   the caller's security groups.
4. **Two layers.** The gateway filters which operations are visible; the downstream call runs with
   the caller's identity, so BL-level object permissions still apply.
5. **Tenant isolation.** Caches, audit and data access are partitioned by company (and user).
6. **Credentials are never logged, stored raw, or returned to a UI.**

### 14.2 Who may call, and with what credential

| Caller | Credential | Resolved identity | Status |
|---|---|---|---|
| In-house automation or an internal agent doing unattended service tasks (n8n, scripts, background jobs) | `IntergrationAccessToken` header (Integration token) | An Integration **service account**, one per agent or job, each in its own security groups | **[implemented]** |
| Individual user in Claude Desktop / claude.ai / ChatGPT, signing in with their own username and password | OAuth 2.1 access token in `Authorization: Bearer` | The **real user** (their own groups and audit) | **[implemented]** (not run on a real AppAI) |
| In-app agent run by a signed-in user | Short-lived delegated user token minted per run, `Authorization: Bearer` | The **real user** | **[implemented]** (not run on a real AppAI) |

Rules:
- The Integration path accepts only `DomainId = Integration` users and checks expiry and active
  state, so an ordinary login session id cannot be replayed as a header **[implemented]**.
- OAuth and delegated tokens map to real users and are stored and validated separately from
  Integration sessions **[implemented]**: a grant session row is its own Integration-type session, and every use also checks the user is an active person account.
- A run with a signed-in user must never fall back to a service account; unattended runs (no user)
  must use a service-account registration, never a user token **[implemented]**.
- Least privilege for service accounts: one per agent or job, each granted only the operations it needs.

### 14.3 Authentication controls

| Control | Status |
|---|---|
| Token read from header only, never the query string | [implemented] |
| Anonymous tokens rejected | [implemented] |
| Expiry, active user and Integration-type checks (`TryRegisterIntegrationTokenIdentity`) | [implemented] |
| Integration token admin endpoints admin-only and company-scoped | [implemented] |
| Identity published to the gateway only from the validated session, never from client headers | [implemented] |
| `Authorization: Bearer <Integration token>` accepted as an alternative to `IntergrationAccessToken` (`McpCredentialReader`; `IntergrationAccessToken` wins if both are sent; non-Bearer schemes ignored) | [implemented] (T1) |
| `401` carries `WWW-Authenticate: Bearer` (`error="invalid_token"` when a credential was rejected) | [implemented] (T1) |
| `401` with `WWW-Authenticate: Bearer resource_metadata=..., scope="mcp"` so OAuth clients can discover sign-in (Claude requires it); only when `OAuth:Enabled` | [implemented] (T3) |
| OAuth discovery: authorization-server metadata (all path variants) and protected-resource metadata (RFC 9728), under `/appai` and at the host root through the existing rewrite; off unless `OAuth:Enabled` | [implemented] (T3, test-host verified; `/oauth/authorize` and `/oauth/token` handlers still missing) |
| OAuth 2.1 authorization server (OpenIddict, own ADO.NET stores on the master DB): dynamic registration, `/oauth/authorize` with an AppAI sign-in and consent page, `/oauth/token`, revoke; PKCE S256 only; exact redirect-URI match; 3-minute consent tickets bound to the exact request; opaque tokens bound to the `/mcp` resource and the `mcp` scope; refresh rotation with zero reuse leeway (a replay ends the connection) | [implemented] (protocol verified over HTTP with a fake session layer; AppAI session code compiled, not run) |
| Per-run user token for in-app agents (`OAuth:AgentTokenServerUrls`): `McpConnectionHelper` sends it instead of stored headers and drops every stored credential; a run without a signed-in user skips the server | [implemented] (T6b, compiled, not run on a real AppAI) |

Why the agent change is needed: `McpConnectionHelper.BuildHeaders` builds the same headers for every
user, and `CreateMcpPluginAsync` receives the user context but does not pass it on. Pointing an agent
at `/mcp` with a stored service token would run every user's agent as that service account.

### 14.4 Authorization controls

| Control | Status |
|---|---|
| Catalogue + enabled + security-group grant required per operation (`AppMcpExposedApi`, `AppMcpExposedApiGroup`, `AppSecurityGroupMember`) | [implemented] |
| `semantic_search_endpoints` / `browse_endpoints` return only allowed operations | [implemented] |
| `get_endpoint_details` / `api_execute` answer "not found" for disallowed operations, so ids cannot be probed | [implemented] |
| Load failure of grants means deny | [implemented] |
| Allowed-set cache of at most 60 s, cleared on admin change | [implemented] |
| Management API company-admin only | [implemented] |
| Direct group membership only; organisation or user-type derived membership ignored | [gap] |
| A SysAdmin session cannot use the management API (tables are per tenant) | [gap] |

### 14.5 Data protection and tenant isolation

| Control | Status |
|---|---|
| Dataset cache keyed `{company}:{user}:{dataset}`; global datasets per company | [implemented] |
| Dataset names validated `[A-Za-z0-9_-]` (path traversal) | [implemented] |
| Audit rows written to the caller's tenant DB; token never stored | [implemented] |
| Spec endpoint (`/swagger/*`) loopback only; Swagger UI Development only | [implemented] |
| `ForwardCallerToken` off by default; the caller's token goes only to sources that opt in, and only in the configured header | [implemented] |
| Static `AccessTokenValue` used only to download the spec, never on caller requests | [implemented] |
| OAuth tokens are opaque reference tokens: only hashes and protected payloads are stored (checked: the raw token string is in no column) | [implemented] |

### 14.6 Abuse resistance

| Control | Status |
|---|---|
| Rate limit 120 requests per minute per IP, sliding window, on `/mcp` | [implemented] |
| Separate per-IP limits: `/oauth/token` 30, `/oauth/authorize` 60, `/oauth/register` 10, consent/admin APIs 60, `/.well-known` 120 per minute | [implemented] |
| Polly timeout, retry and circuit breaker on downstream calls | [implemented] |
| Bounded audit queue with overflow counter | [implemented] |
| Anti-forgery token on the consent approval (it relies on the custom session header and a JSON-only body) | [gap] |

### 14.7 Audit and revocation

| Control | Status |
|---|---|
| Tool calls, access denials (`A022_AccessDenied`) and admin changes audited with user, company and correlation id | [implemented] |
| Failed token checks (401) audited | [gap] |
| Audit rows record that an agent made the call on behalf of the user | [gap] |
| OAuth consent, decline, disconnect and admin changes written to the tenant audit table (A006); token issue/refresh/revoke and registration logged (NLog) but not in the tenant table | [implemented, partly] |
| Admin can list and revoke connections: company admin for the company, a user for their own, SysAdmin manages OAuth clients | [implemented] (compiled, not run on a real AppAI) |
| Integration tokens revoked by expiry or re-save in the admin screen | [implemented] |
| Retention or purge of `AppMcpAuditLog` | [gap] |

### 14.8 Client compatibility (affects which design applies)

- Claude (claude.ai / Desktop) custom connectors support OAuth or fixed request headers. Header
  authentication is a **beta limited to some organizations** and allows up to four headers. The standard
  names `authorization`, `x-api-key` and `x-auth-token` work for every connector; any other name (such as
  `IntergrationAccessToken`) needs Anthropic's approval. The credential is shared by the whole
  organization, so it identifies one service identity, not a person.
- Claude and ChatGPT reach the server from the vendors' cloud (Claude: `160.79.104.0/21`), so `/mcp` and the
  OAuth endpoints need a **public HTTPS URL** (or a tunnel). A `localhost` gateway is not reachable.
- On an OAuth connection Claude owns the `Authorization` header; it cannot be set manually. A fixed
  `Authorization: Bearer` header is possible only without OAuth and is one shared value per connector.
- ChatGPT: third-party sources say OAuth or no authentication (sometimes a bearer field), with no
  arbitrary header. Not confirmed from OpenAI documentation.
- Therefore per-user sign-in from Claude and ChatGPT requires OAuth (section 14.3). Header tokens
  remain right for in-house automation.
- Not yet verified with real clients.

### 14.9 Open security items

1. Run the whole OAuth flow on a dev copy of AppAI (apply the SQL, enable OAuth, sign in, connect, call a tool, revoke) and open the React pages: the AppAI-session code and the pages are only compiled so far.
2. Real-client verification (Claude, ChatGPT) over a public HTTPS URL; production certificates; shared Data Protection key ring for several nodes; trusted forwarded headers behind a proxy.
3. Unverified: whether the master session table allows a second row per user (a unique index on the user id would make authorize fail).
4. Failed `/mcp` credentials are logged but not written to the tenant audit table (a failed token has no tenant); audit rows do not yet say that an agent made the call.
5. No anti-forgery token on the consent approval; no remembered consent (asked every time); no CIMD (dynamic registration and admin pre-registration only).
6. The `/mcp` middleware class itself has no automated test (header parsing, ticket, registration and agent-token logic do).
7. Rotate previously committed PLM/Azure/SQL secrets.

---

## Verification status

- Verified (per merge plan): builds; 77 gateway tests and 18 BL tests pass; React type-check and
  ESLint clean on the new files; spec generation and DI wiring checked with throwaway hosts.
- **Not verified**: V046/V047 on a real database; the full token → `/mcp` → tools → tenant DB flow;
  the React screen in a browser; Claude Desktop / ChatGPT sending the custom header.

## Known gaps

- Dead code that can write `appsettings.json`: `RuntimeConfigService`, `AdminTools`, and the
  `[NonController]`-disabled `McpServerManagerController`.
- Failed token checks are not audited; no audit retention.
- Group membership via organisation/user type is not considered.
- Edits to a selected operation in the React grid are discarded if another row is clicked before saving.
- No automated tests for `IntegrationTokenMiddleware` itself (header parsing is covered by
  `McpCredentialReaderTests`).
