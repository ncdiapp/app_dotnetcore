# Merge Swagger MCP Gateway into AppAI.Web with IntergrationAccessToken auth

## Context
`Swager-Mcp-Server-netcore` (MCP Gateway: Swagger -> hybrid RAG -> MCP tools `semantic_search_endpoints`, `get_endpoint_details`, `api_execute`, `data_analyze`, `api_login`) is already partly wired into AppAI.Web behind the `MCP_GATEWAY` compile flag (`Program.cs:286-307, 405-439`, `ApiSources:Sources[PLM]` in appsettings). Its auth is unfit for a multi-tenant host:
- shared `X-Api-Key` guard (global, blocks cookie users)
- in-memory `TokenStore` keyed by `mcp-session-id`, falling back to `"default"`, which leaks tokens across users
- browser `/auth/login` form
- static `AccessTokenValue` that makes every caller one identity
- audit with no user or tenant

Decisions (confirmed by user):
- **Token** = existing session token (`AppSecurityUserSession`, `EmExternalSigninType.Integration=98`), sent in HTTP header **`IntergrationAccessToken`** (spelling is intentional and already used in the codebase).
- **Old auth replaced fully.**
- **Hosting** = in-process in AppAI.Web.

## Design
**Inbound:** every `/mcp*` request must carry `IntergrationAccessToken: <session token>`.
1. New `IntegrationTokenMiddleware` (`AppAI.Web/Auth/`), scoped with `UseWhen(path starts with /mcp)`, placed before `UseAuthentication`.
2. It reads the header (header only, never query string, matching `SessionValidationFilter`) and rejects anonymous tokens, as `SessionValidationFilter` does.
3. It calls `AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity(token)`, which registers the `AppClientIdentity` (user, tenant DB, company) for the request. 401 on failure.
4. It stores the raw token in `HttpContext.Items["IntegrationToken"]` for outbound forwarding. Reuse the filter's logic by extracting a shared helper rather than copying it.

**Outbound (per-request forwarding):**
- `ApiClient.ResolveAuthToken` reads the token from the current request's `HttpContext.Items` and sends it in `ApiSourceConfig.AccessTokenHeaderName` (already `IntergrationAccessToken` for PLM).
- Delete `TokenStore`, `AuthTools.api_login`, `AuthController` (`/auth/login`), the `X-Api-Key` middleware and `Auth:ApiKey`, the `"default"` session fallback, and static `AccessTokenValue` support (remove the PLM value from appsettings).
- Because the same identity is registered, cross-system calls to PLM carry the caller's own session.

**Tenant isolation:**
- `DataAnalysisCacheService` and dataset keys become `(CompanyId, UserId, …)` instead of session-only. Global dataset cache and the `data-analysis-cache/` path are partitioned by company.
- Embedding and enrichment caches stay global (they hold only endpoint metadata), unless sources become per-tenant (out of scope, noted below).

**Audit -> LLBLGen + tenant DB:**
- Replace the raw ADO.NET `AuditService` with a tenant-scoped BL (`AppTenantAdapterBL.GetTenantAdapter()`; use `CreateTenantAdapter` in the background writer, which captures the identity first).
- New migration **V046** adding an MCP audit table in the tenant DB with `TenantId`/`CompanyId`, `UserId`, `UserName`, `SessionId`, `CorrelationId`, event code and detail. Read the migration SQL before writing DTO/mapper (project DB rules 1-2), and log every catch (rule 3).
- Writes go through a bounded `Channel<AuditEntry>` with a hosted writer, not `Task.Run`.
- Confirm V046 is free with `check-duplicate-versions.ps1`.

**Host cleanup (remove `MCP_GATEWAY` flag):**
- Move the McpGateway code into a proper project/folder (`APP.McpGateway` or `AppAI.Web/Mcp/`) so `Program.cs` only calls `AddMcpGateway()` / `MapMcpGateway()` extension methods.
- Keep the existing Serilog/NLog setup, global rate limiter and antiforgery out of the gateway. Keep the rate limiter scoped to `/mcp` only.
- Drop Swashbuckle gateway-only Swagger, or keep it Development-only.
- Route clashes: only `/mcp` remains. `/auth/login`, `/api/management/*` and the gateway's `/health` and `/` are removed or folded into AppAI admin.
- Move `appsettings.json` runtime rewriting (`RuntimeConfigService`, audit toggle) to DB or config-read-only.

**Security hygiene:**
- Redact the `IntergrationAccessToken` header in Development `HttpLogging` (`Program.cs:140`; use `RequestHeaders.Add` allow-list, which leaves it as `[Redacted]`).
- Add the header to `McpHeaderSecrets` masking so it is never returned to a UI.
- Do not log it in `ApiClient` or audit.

## Critical files
- `AppAI.Web/Program.cs`: pipeline, flag removal, middleware registration
- `AppAI.Web/Auth/SessionValidationFilter.cs`: source of the session-validation logic to share
- `APP.BL/MasterAdmin/AppSaasUserSessionMgtBL.cs:73`: `ViladateSessionIdAndCompanyIdRegisterIdentity`
- `APP.BL/Infrastructure/AppTenantAdapterBL.cs`: tenant adapter
- `AppAI.Web/Migrations/V046__McpAuditLog.sql` (new)
- Gateway sources: `ApiClient.cs`, `TokenStore.cs`, `AuthTools.cs`, `AuthController.cs`, `AuditService.cs`, `DataAnalysisCacheService.cs`, `McpGateway.csproj`

## Phases
1. Auth: middleware + forwarding + remove old auth/static token. Rotate and remove committed secrets in gateway config.
2. Tenant-partition caches and the session fallback fix.
3. Audit -> V046 + LLBLGen BL + channel writer.
4. Host cleanup (drop flag, extensions, route cleanup) and docs update (`Document/AgentDesign`).

## Risks and open items
- **Master DB**: tokens live in master DB session table, so no new master table is needed. Only the V046 tenant audit table is added. If audit should be cross-tenant, it needs a master-DB script (no master migration folder seen).
- **Session lifetime**: `Home/ExternalLogin` session TTL governs the token. Long-running n8n/automation clients would need re-login. A dedicated long-lived API token (the rejected option) can be added later behind the same header.
- **Out of scope**: per-tenant Swagger sources/registry, the pdf-ingestion plan (docs only, EF Core, would conflict with LLBLGen), and rotating the PLM/Azure/SQL secrets already committed in appsettings.
- Stale gateway docs (`mcp-session-auth-flow.md`, `api endpoint.-exposed- MCP tool.md`, `CLAUDE.md`) describe code that does not exist and should not be used as a spec.

## Verification
- `dotnet build AppAI.Core.sln` with no `MCP_GATEWAY` define.
- Manual/Playwright-free HTTP checks against `/mcp`:
  - no header -> 401
  - bad token -> 401
  - anonymous token -> 401
  - valid token from `Home/ExternalLogin` -> tools list works
- `api_execute` against PLM: confirm outbound request carries the caller's token (check via audit row, not logs).
- Two users in two tenants: confirm datasets and audit rows are isolated, and no `"default"` session sharing.
- Confirm token is absent from logs and from MCP header API responses.
- Run V046 on a test tenant via `POST /webapi/TenantProvisioning/RunMigrations`.
- xUnit: middleware tests (header only, query string ignored), cache key partition tests. Gateway's 57 existing tests updated for removed auth.

---

# Addendum: external users (Claude Desktop / ChatGPT Desktop) and MCP management API

## Requirement
Each external user has their own `IntergrationAccessToken`. We need a management API (plus React admin screen) to control which APIs external users can access.

## Findings that change the plan above
- **Integration tokens already exist; do not build a new token table.**
  - `AdministrationController.cs:1027-1037` (`RetrieveAllIntegrationTokenDto`, `SaveOneIntegrationTokenExDto`), BL in `AppSecurityUserBL.cs:894-1140`.
  - Each token is an `AppSecurityUser` with `DomainId = EmAppUserType.Integration` plus an `AppSecurityUserSession` with admin-chosen `ExpirationDate`. Re-saving rotates the token.
  - UI: `CompanyIntegrationTokenManagement.tsx`, tab "Integration Tokens" in `CompanySecuritySetting.tsx`.
  - So the `IntergrationAccessToken` header value is that session id, validated with `ViladateSessionIdAndCompanyIdRegisterIdentity`. This also resolves the "session TTL" risk: admins set expiry.
- **Existing token endpoints are insecure (fix first):** neither has an admin check, and `RetrieveAllIntegrationTokenDto` is not company-scoped, so it returns raw session ids across all tenants. Add `IsAdminUser()` and filter by working company.
- **There is no per-endpoint ACL in AppAI.** `SessionValidationFilter` is authentication only; authorization happens inside BL methods per business object (`AppSecuritySysObjGroupUserBL`, groups/actions in tenant DB) or via ad hoc `IsAdminUser()`. There is no way to ask "can user X call endpoint Y".
- **Controllers have no Swagger metadata** (no `ProducesResponseType`, no XML docs; `AddSwaggerGen` only under `MCP_GATEWAY`).
- No master-DB migration folder exists; master scripts are ad hoc under `Document/Design/`.

## Consequence for "reuse existing role permissions"
Role permissions only protect data objects inside BL. They cannot decide which *endpoints* an external user may call through MCP, so an admin cannot say "this integration user may use only these 10 APIs". Decision needed (see chat): add a small tenant-DB **exposed-API allowlist** in addition to role permissions.

## Proposed management API (if allowlist is approved)
Tenant migration V046 (alongside the audit table):
- `AppMcpExposedApi` (operationId, source, controller/action route, HTTP method, description, IsEnabled)
- `AppMcpTokenApiGrant` (integration UserId, ExposedApiId), or grant by group of APIs

`McpManagementController : SecureBaseController` (admin only, `IsAdminUser()`, company-scoped):
- `GetExposableApis`: from the Swagger index, merged with catalog state
- `SaveExposedApi` / `SetApiEnabled`
- `GetTokenGrants(userId)` / `SaveTokenGrants(userId, apiIds)`
- Reuse the existing token endpoints for issue/rotate/revoke

Gateway enforcement: after the middleware registers identity, `semantic_search_endpoints` / `get_endpoint_details` return only granted operations, and `api_execute` returns 403 for anything else. Role/BL permission checks still apply on the real call.

React: extend `CompanyIntegrationTokenManagement.tsx` with an "API access" panel per token (grid of exposable APIs with checkboxes), service methods added to `adminsvc.ts`, following the master/detail pattern in `AgentMcpServerTab.tsx`.

## Extra prerequisites
- Generate OpenAPI for AppAI controllers in all environments and protect the spec, with summaries on the actions chosen for exposure.
- Client note: Claude Desktop and ChatGPT must be able to send a custom header (`IntergrationAccessToken`) to a remote MCP server. Verify per client; if not, an OAuth-style flow or `mcp-remote` header shim is needed.

---

# Addendum 2: role-based endpoint control (supersedes the per-token grant design above)

## Security flow (confirmed by user)
1. External client sends `IntergrationAccessToken: <token>` on every `/mcp` request.
2. Middleware resolves token -> session -> user + tenant (`ViladateSessionIdAndCompanyIdRegisterIdentity`). Reject 401 if invalid/expired.
3. Load the user's **set of security roles** (a user can have several).
4. Every MCP-exposed API endpoint has a list of roles allowed to call it. The user may call it if **any** of their roles is in that list (union; deny by default).
5. Gateway enforces this on all three tools:
   - `semantic_search_endpoints` / `get_endpoint_details`: return only operations the user's roles allow (the model never sees the rest).
   - `api_execute`: re-check at call time; 403 if not allowed, even if the operationId was guessed.
6. The downstream call is made with the caller's identity, so BL-level object permissions still apply as a second layer.
7. Audit every allow/deny with user, roles, operationId, correlation id.

## Data model (tenant DB, migration V046, next to audit table)
- `AppMcpExposedApi`: operationId (unique), source, route, HTTP method, summary, IsEnabled
- `AppMcpExposedApiRole`: ExposedApiId, RoleId (many-to-many)
- Read the real role table definitions in migrations before writing DTOs/mappers (project DB rule 1). **RoleId must reference whichever role store AppAI actually uses** (see open question).
- Endpoints not in `AppMcpExposedApi`, or with no roles, are not exposed.

## Management API (`McpManagementController : SecureBaseController`, admin only, company-scoped)
- `GetExposableApis`: Swagger index merged with catalog state and role assignments
- `SaveExposedApi`, `SetApiEnabled`
- `GetApiRoles(apiId)` / `SaveApiRoles(apiId, roleIds)`; plus bulk assign by tag/controller
- `GetUserEffectiveApis(userId)`: preview of what a given integration user can call (union of roles) for admin verification
- Token issue/rotate/revoke stay on the existing Integration Token endpoints (after adding the admin check and company scoping).

## React
- New "MCP API Access" tab beside "Integration Tokens" in `CompanySecuritySetting.tsx`: master grid of exposable APIs (enable toggle), detail pane with role multi-select; follows `AgentMcpServerTab.tsx` master/detail pattern. Service methods in `adminsvc.ts`.
- Integration token screen: assign roles to the integration user (reuse existing user-role UI if present).

## Caching and performance
- Cache per-request: user's role set (short TTL, invalidated on role change) and the endpoint->roles map per tenant. Filter search results after ranking, not before.

## Open question
- Which role store is authoritative: tenant `AppSecurityGroup`/`AppSecurityGroupMember`, or legacy `AppSecurityUserRolePrevilege`? Confirm before V046.

## Resolved: role store = AppSecurityGroup
- "Security role" means `AppSecurityGroup` (tenant DB), with users linked through `AppSecurityGroupMember`.
- `AppMcpExposedApiRole.RoleId` becomes `SecurityGroupId` referencing `AppSecurityGroup`.
- User's role set = groups the user belongs to via `AppSecurityGroupMember` (check whether membership can also come through organization/user-type mappings in `AppSecuritySysObjGroupUserBL`, and whether groups nest).
- Before writing V046, DTOs or mappers: read the `AppSecurityGroup` / `AppSecurityGroupMember` CREATE TABLE statements in the V001 migration and copy column names exactly. Legacy `AppSecurityUserRolePrevilege` is not used.

---

# Status: Phase 1 implemented (build-verified only, not runtime-tested)

- Gateway source copied into `APP.McpGateway/` (class library, assembly/namespace `McpGateway`); added to `AppAI.Core.sln`; `AppAI.Web.csproj` now references it (still behind `EnableMcpGateway`). Not copied: `Program.cs`, tests, caches, publish output, zips. `EmbedModels/*.onnx` is git-ignored (as upstream); place `all-MiniLM-L6-v2.onnx` there locally.
- `IntegrationTokenMiddleware` guards `/mcp`; `AppSaasUserSessionMgtBL.TryRegisterIntegrationTokenIdentity` validates (expiry, Integration user, active).
- Integration token admin endpoints now admin-only and company-scoped; save refuses non-Integration/other-company targets; new token sessions stamped `EmExternalSigninType=Integration`.
- Removed: X-Api-Key guard, `TokenStore`/`ITokenStore`, `AuthTools` (`api_login`), `AuthController` (`/auth/login`), `Login*` source settings, `"default"` session fallback, antiforgery registration.
- `ApiSourceConfig.ForwardCallerToken` (default false): when true the caller's token is forwarded in `AccessTokenHeaderName`. `AccessTokenValue` is now used only to download the Swagger spec.
- `McpServerManagerController` disabled with `[NonController]` (unauthenticated, rewrites appsettings.json) until replaced by an admin-only controller.
- Added `McpNullAuditService` (was referenced but missing).

## Config to add (appsettings.json, not edited by the agent)
For a source that is this AppAI instance:
```json
{ "Name": "AppAI", "BaseUrl": "http://localhost:52740/appai/", "SwaggerJsonPath": "swagger/v1/swagger.json",
  "AccessTokenHeaderName": "IntergrationAccessToken", "ForwardCallerToken": true }
```
Existing PLM source: leave `ForwardCallerToken` false (PLM has its own master DB and will not recognise an AppAI token). Remove its committed `AccessTokenValue` (only needed for spec download; rotate it).

## Known gaps / next
- `/webapi` still accepts Integration tokens via `CurrentUserSessionId` (user decision: leave as is).
- Gateway `AuditService`, dataset caches and `RuntimeConfigService` are still non-tenant (phases 2-3).
- No automated tests yet for the middleware / BL changes.

---

# Status: Phase 2 implemented (tenant isolation of caches and caller identity)

- `IntegrationTokenMiddleware` now also publishes `IntegrationUserId` / `IntegrationCompanyId` (from the validated session, never from client headers). Gateway reads them via `IMcpCallerContext` (`APP.McpGateway/Services/McpCallerContext.cs`); `RequireIdentity()` throws when there is no authenticated caller, so there is no `"shared"` / `"default"` bucket anywhere.
- `DataAnalysisCacheService`: per-user cache keyed `{company}:{user}:{dataset}` (no more client-supplied `Mcp-Session-Id`); "global" datasets are now per company, in memory and on disk under `{CacheDirectory}/{companyId}/`. Legacy un-partitioned files in the old flat folder are ignored (safe to delete).
- Dataset names are validated (`[A-Za-z0-9_-]` only) before touching the file system, closing a path-traversal route via `data_analyze(datasetName)`.
- `DataAnalysisStartupService` removed: it had no caller identity. Global datasets now fill on the first `api_execute` per company and reload from disk after restart.
- New test project `APP.McpGateway.Tests` (xUnit + Moq, test-only packages): 58 tests, all passing - ported `SwaggerServiceTests`, rewritten `ApiClientTests` (token forwarding, two callers, no static token on calls), new `DataAnalysisCacheServiceTests` (user/company isolation, restart, no-caller, unsafe names). Run with `DOTNET_ROOT=C:\Program Files\dotnet` if the user-local .NET 9 install shadows the machine .NET 10.

Remaining: phase 3 (tenant audit, V046), phase 4 (host cleanup, drop flag, management API/UI).

---

# Status: Phase 3 implemented (tenant audit)

- **V046__McpAuditLog.sql**: tenant table `dbo.AppMcpAuditLog` (CompanyId, UserId, EventCode, Action, Success, McpSessionId (informational), IpAddress, CorrelationId, AppSource, HttpMethod, ResourcePath, HttpStatus, ErrorMessage, AdditionalContext JSON). The access token is never stored. **Not yet applied to any database** - run `POST /webapi/TenantProvisioning/RunMigrations` (or Migrations:RunOnStartup in Development) and check the table exists.
- `APP.BL/TenantBusiness/McpAuditBL.cs`: `CaptureTarget()` (tenant connection from the registered identity, request thread) and `WriteAsync()` (parameterized batch insert in one transaction). Plain SQL like the other recent tenant tables, because new tables have no generated LLBLGen entity.
- Gateway `QueuedAuditService` (`IAuditService` + `IHostedService`): stamps CompanyId/UserId from the validated caller, bounded queue (`Audit:QueueCapacity`, default 10000), batched writes (`Audit:BatchSize`, 100), overflow counted (`DroppedCount`) and logged, failed writes logged, queue drained on shutdown. `IAuditSink` is implemented in AppAI.Web (`McpTenantAuditSink`).
- Removed: old ADO.NET `AuditService` (single global `AuditLog` DB, `ConnectionStrings:AuditDb`), `McpNullAuditService`, and the audit toggle that rewrote appsettings.json (the runtime toggle is now in-memory only).
- Bug found and fixed while testing: a `BackgroundService`-based writer lost queued events at shutdown (task cancelled before draining; 178/200 runs lost the event). Now a plain `IHostedService` that completes the queue and awaits the drain.
- Tests: 67 passing (9 new for the queue). `check-duplicate-versions.ps1` still fails on the pre-existing V039 duplicate; V046 is unique.

Not done / known gaps:
- Authentication failures (401 at the middleware) and role denials are not audited yet (middleware has no tenant context for a failed token). Role denials come with phase 4.
- No retention/purge for AppMcpAuditLog.
- `McpServerManagerController` (disabled) and `AdminTools` (not registered as MCP tools) still persist settings to appsettings.json; both are replaced in phase 4.

---

# Status: Phase 4 implemented (role-based API access, management API + screen, flag removed)

## What exists now
- **V047__McpExposedApi.sql**: `dbo.AppMcpExposedApi` (catalogued operations: AppSource, OperationId, IsEnabled, snapshot of method/path/summary) and `dbo.AppMcpExposedApiGroup` (ExposedApiId, GroupID -> `AppSecurityGroup.GroupID`, cascade delete). **Not yet applied to any database.**
- **Rule** (`McpApiAccessBL.GetAllowedForUserAsync`): an operation is usable by an external user only if it is catalogued, enabled, AND the user is in a granted group via `AppSecurityGroupMember(GroupID, UserID)`. Direct membership only (no nesting exists in the schema). Deny by default.
- **Enforcement in the gateway** (`ApiAccessPolicy`, cached per company+user for `Mcp:AccessCacheSeconds`, default 60, cleared immediately on admin changes): `semantic_search_endpoints` and `browse_endpoints` return only allowed operations; `get_endpoint_details` and `api_execute` answer "not found" for anything not allowed (identical to a missing operation, so ids cannot be probed) and write an `A022_AccessDenied` audit row. If grants cannot be loaded the answer is deny.
- **Management API** `webapi/McpManagement/*` (`McpManagementController`, company admin only): `GetExposableApis`, `GetSecurityGroups`, `SaveExposedApi`, `DeleteExposedApi`, `GetUserEffectiveApis` (preview), `RefreshApiCatalog`. The catalogue entry is taken from the server's own spec, so only operations that really exist can be exposed. Changes are audited. The controller itself is excluded from the exposable spec.
- **React**: tab "MCP API Access" next to "Integration Tokens" (`CompanyMcpApiAccess.tsx`, `mcpManagementSvc.ts`): searchable grid + detail pane (enable, tick security groups), warnings for sources that do not forward the caller token and for GET operations whose name suggests a change.
- **OpenAPI for AppAI's own controllers** (`McpSwaggerSetup`): 875 operations, 0 duplicate operation ids (verified with a throwaway host). Actions without an explicit `[HttpGet]/[HttpPost]/...` are left out (Swagger cannot describe them; add a verb attribute to expose one). The document is served to loopback only (404 otherwise); Swagger UI only in Development.
- **Compile flag removed.** `MCP_GATEWAY` / `EnableMcpGateway` are gone; the gateway always compiles and is wired by `AppAI.Web/Mcp/McpGatewayExtensions.cs`. It is always on (the earlier `Mcp:Enabled` switch was removed; a leftover `Mcp` section in appsettings.json is ignored). `Program.cs` shrank from ~460 to 270 lines.

## How to set it up (the gateway is always on; appsettings is a credential file, so not edited by the agent)
1. Apply V046 and V047 (`POST /webapi/TenantProvisioning/RunMigrations`, or Migrations:RunOnStartup in Development).
2. Add an AppAI source under `ApiSources:Sources` in appsettings (no `Mcp` section is needed):
   `{ "Name": "AppAI", "BaseUrl": "http://localhost:52740/appai/", "SwaggerJsonPath": "swagger/v1/swagger.json", "AccessTokenHeaderName": "IntergrationAccessToken", "ForwardCallerToken": true }`
   Keep PLM's `ForwardCallerToken` false (PLM has its own master DB).
3. Optional: `SemanticSearch:Provider=LocalOnnx` needs `all-MiniLM-L6-v2.onnx` in `APP.McpGateway/EmbedModels` (git-ignored). With 875 endpoints, expect a slower first load.
4. As company admin: create Integration tokens, add the integration users to security groups, then open "MCP API Access" and enable operations per group.

## Verified vs not
- Verified: builds; 77 gateway tests + 18 BL tests pass; React type-check (0 errors) and ESLint clean on new files; spec generation and DI wiring checked with throwaway hosts (validated on build/scopes).
- **Not verified**: V046/V047 against a real database; the end-to-end flow (token -> /mcp -> tools -> tenant DB); the React screen in a browser; Claude Desktop / ChatGPT actually sending the custom header.

## Known gaps / follow-ups
- A SysAdmin session cannot use the management API (tables are per tenant).
- Group membership derived from organisation/user type (AppSecuritySysObjGroupUserBL) is not considered: only direct `AppSecurityGroupMember` rows.
- `/webapi` still accepts Integration tokens (decision: leave as is).
- Dead code that can write appsettings.json remains: `RuntimeConfigService`, `AdminTools` (not registered as MCP tools) and the disabled `McpServerManagerController`.
- Role denials are audited; failed token checks are not. No retention for `AppMcpAuditLog`.
- Edits to a selected operation in the screen are discarded if another row is clicked before saving.

---

> **Migration numbers:** the audit and access-control scripts were first written as V040/V041, then renumbered to **V046/V047** because V040-V045 were taken on master in the meantime. Earlier sections of this document were updated to match.
