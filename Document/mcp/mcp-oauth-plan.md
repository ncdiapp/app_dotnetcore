# Plan: OAuth 2.1 for the AppAI MCP Gateway

Status: **implemented, not yet run against a real AppAI instance (see section 7g).** Companion to `mcp-gateway-architecture.md` (how the
gateway works today) and `mcp-gateway-merge-plan.md` (history and decisions).

---

## 1. Summary of the discussion so far

**Where we are.** The MCP gateway runs in-process in AppAI.Web at `/appai/mcp`. Every `/mcp*`
request must carry the header `IntergrationAccessToken: <Integration session id>`. The
`IntegrationTokenMiddleware` validates it (`TryRegisterIntegrationTokenIdentity`), registers the
user and tenant, and the gateway then applies role-based API access (security groups, V047) and
tenant-scoped audit (V046). `APP.McpGateway` is a class library loaded into the AppAI.Web
process; the decision is to keep it as a separate project (no merge).

**The problem.** External AI clients cannot be assumed to send our custom header.

- Claude (claude.ai / Claude Desktop) custom connectors support **OAuth** or **fixed request
  headers**. Request-header authentication is a **beta, limited to some organizations**; up to 4
  headers; Anthropic must approve each header name, so `IntergrationAccessToken` is not
  guaranteed. (Source: Claude docs, "remote MCP" custom connectors.)
- On an OAuth connection the `Authorization` header is owned by OAuth and cannot be set manually.
  Without OAuth, a fixed `Authorization: Bearer <token>` header can be entered, but it is one
  shared value for the connector, so all members of a team connector would act as one identity.
- ChatGPT: third-party docs say OAuth or no auth (sometimes a bearer field), no arbitrary header.
  Not confirmed from OpenAI documentation; verify on the real plan.
- In-house clients (n8n, scripts, Claude Code, apps on the Claude/OpenAI APIs) can send any header,
  so the current design already works for them.

**What we established about our server.**

- Accepting `Authorization: Bearer` in the middleware helps only the headers-only mode.
- For per-user sign-in from Claude/ChatGPT, **AppAI.Web must itself be an OAuth authorization
  server** (or delegate to one). A repo search found none: no OpenIddict/Duende/IdentityServer,
  no `.well-known` endpoints, and the only auth scheme registered is cookies
  (`Program.cs:83-84`).
- The discovery routes are needed, including the path variants
  (`.well-known/openid-configuration` and `.well-known/openid-configuration/{*issuerPath}`),
  because our issuer has a path (`/appai`).

**Decision needed from the chairman**: build OAuth (this plan) vs. stay header-only. See section 9.

---

## 2. Goals and non-goals

**Goals**

1. A Claude Desktop / claude.ai / ChatGPT user can add the connector URL, sign in once, and use
   `/mcp` as **themselves**, with their own roles and audit trail.
2. Reuse everything downstream: `TryRegisterIntegrationTokenIdentity`-style identity
   registration, `ApiAccessPolicy` (security-group grants), tenant audit, tenant-partitioned caches.
3. Keep the existing `IntergrationAccessToken` header working for in-house clients.

**Non-goals**

- Replacing AppAI's own login (cookie, SAML, Windows). OAuth reuses it for the sign-in step.
- Turning AppAI into a general-purpose identity provider for other apps.
- Per-tenant Swagger sources (still out of scope, see the merge plan).

---

## 3. The flow we must support

(From the MCP authorization flow as understood; **re-verify against the current MCP spec and the
Claude/ChatGPT docs before building**, since client behaviour (registration method, discovery
order) changes.)

```
Client                         AppAI.Web                              User browser
  │ POST /appai/mcp (no token)    │
  │──────────────────────────────►│  401 + WWW-Authenticate: resource_metadata="…/.well-known/oauth-protected-resource…"
  │◄──────────────────────────────│
  │ GET protected-resource metadata → names the authorization server (issuer)
  │ GET authorization-server metadata (oauth-authorization-server / openid-configuration variants)
  │ register client (pre-registered client_id, or dynamic registration)
  │ open browser ────────────────────────────────────────────────────►  /authorize?code_challenge=…
  │                               │  existing AppAI login (cookie/SAML/Windows) → consent
  │◄─────────────────────────────── redirect back with ?code=…
  │ POST /token (code + PKCE verifier)
  │──────────────────────────────►│  → access token (+ refresh token)
  │ POST /appai/mcp  Authorization: Bearer <access token>
  │──────────────────────────────►│  middleware validates → identity registered → tools
```

---

## 4. Architecture

### 4.1 Where it lives
New code in AppAI.Web (host concerns: login, cookies, tenant/master DB), with token storage and
validation in `APP.BL`. `APP.McpGateway` stays untouched: it keeps seeing only `IMcpCallerContext`
values from `HttpContext.Items`, so no gateway change is needed beyond what the middleware publishes.

| Piece | Project | Notes |
|---|---|---|
| Discovery endpoints | AppAI.Web (`OAuthDiscoveryController`) | see 4.2 |
| `/authorize`, `/token`, `/register` | AppAI.Web (`OAuthController`) | see 4.3 |
| Consent/login hand-off page | AppAI.Web + React | reuse existing login |
| Token + client + code storage | `APP.BL` + **master DB** | see 4.4 |
| Bearer validation | `IntegrationTokenMiddleware` | see 4.5 |

### 4.2 Discovery endpoints
Serve the same authorization-server document from every variant clients may probe:

- `/.well-known/oauth-authorization-server` and `/.well-known/oauth-authorization-server/{*issuerPath}`
- `/.well-known/openid-configuration` and `/.well-known/openid-configuration/{*issuerPath}`
- `{issuer}/.well-known/openid-configuration` (suffix style)
- `/.well-known/oauth-protected-resource` and `/.well-known/oauth-protected-resource/{*resourcePath}`
  (separate document describing `/mcp` and naming the authorization server)

Document content: `issuer`, `authorization_endpoint`, `token_endpoint`, `registration_endpoint`
(only if dynamic registration is built), `response_types_supported: ["code"]`,
`grant_types_supported: ["authorization_code","refresh_token"]`,
`code_challenge_methods_supported: ["S256"]`, `token_endpoint_auth_methods_supported`
(`none` for PKCE public clients, plus `client_secret_post`/`client_secret_basic` only if confidential
clients are supported), `scopes_supported`.

**Hosting catch: the `/appai` base path.** Well-known URLs are defined relative to the host root,
but the app runs under `UsePathBase("/appai")`. `Program.cs:147-157` already has a precedent: a small
middleware rewrites host-root `/openapi-doc` and `/webapi` into `/appai/...` before `UsePathBase`.
The same approach can map root `/.well-known/*` to `/appai/.well-known/*`. Under IIS a virtual
application at `/appai` cannot serve root requests, so deployment needs either a root rewrite rule
or an issuer of `https://host/appai` with all clients probing the path variants. **Confirm the
production topology first** (task T0).

### 4.3 Endpoints

| Endpoint | Behaviour |
|---|---|
| `GET /authorize` | Validate `client_id`, exact-match `redirect_uri`, `response_type=code`, PKCE `code_challenge` (S256 required), `state`, `scope`, `resource`. If the user has no AppAI session, send them to the existing login and return. Show a consent screen. On approval create a short-lived (≤ 60 s), single-use authorization code bound to client, redirect URI, challenge and user. |
| `POST /token` | `authorization_code`: verify code (single use, unexpired), redirect URI and PKCE verifier; issue access + refresh token. `refresh_token`: rotate (old refresh token invalidated on use). Return standard OAuth errors. |
| `POST /register` (optional) | RFC 7591 dynamic client registration. Start with **pre-registered clients only** (admin enters client id in the Claude connector settings); add dynamic registration only if a client requires it. |
| `POST /revoke` (optional) | Token revocation. Also needed as an admin action ("disconnect this user"). |

### 4.4 Storage (master DB)
Tokens identify users across tenants, like `AppSecurityUserSession`, so they belong in the
**master DB** (DB rule 5: identity/ACL data). There is no master migration folder; scripts are
ad hoc under `Document/Design/`. Proposed tables (names and columns to be finalised in the
implementation, after reading the real `AppSecurityUser`/`AppSecurityUserSession` definitions per
DB rules 1-2):

- `AppOAuthClient`: ClientId, Name, RedirectUris, IsPublic, (SecretHash), IsActive, CompanyId (scope).
- `AppOAuthAuthorizationCode`: CodeHash, ClientId, UserId, CompanyId, RedirectUri, CodeChallenge,
  Scope, ExpiresUtc, ConsumedUtc.
- `AppOAuthToken`: TokenHash (access), RefreshTokenHash, ClientId, UserId, CompanyId, Scope,
  AccessExpiresUtc, RefreshExpiresUtc, RevokedUtc.

Store **hashes**, never raw tokens. Every `catch` in the BL logs (project rule 3).

### 4.5 Validating the bearer token in `/mcp`
`IntegrationTokenMiddleware` becomes "MCP token middleware":

1. Read `IntergrationAccessToken`; if absent, read `Authorization: Bearer`.
2. `IntergrationAccessToken` path: unchanged (Integration session, `TryRegisterIntegrationTokenIdentity`).
3. Bearer path: look up the OAuth access token (hash), check expiry/revocation/client, then register
   the owning user's identity for the request and publish the same `Items` keys.
4. On failure, return `401` with `WWW-Authenticate: Bearer resource_metadata="<url>"` (plus
   `error="invalid_token"` when a token was supplied) so clients can start discovery.

Header only; never the query string. Never log either header or the token.

### 4.6 Identity mapping: DECIDED (option A)

**Decision (chairman):** two use cases, two credential types.

| Use case | Credential | Identity at `/mcp` |
|---|---|---|
| 1. Individual user signs in to Claude Desktop / ChatGPT with their own username and password | OAuth access token | The real user (option A) |
| 2. Internal AI agent runs service tasks; each agent/job gets its own service account | `IntergrationAccessToken` (Integration token, existing) | The Integration service account |

Rules that follow:
- The Integration-token path is unchanged and still accepts **only** `DomainId = Integration` users,
  so a normal login session id cannot be replayed as a header.
- The OAuth path maps to the signed-in real user; the Integration-user restriction does not apply
  to OAuth tokens. The two token types are stored and validated separately.
- Role grants (`AppSecurityGroupMember` via V047) apply to whichever identity is resolved, so admins
  grant MCP API groups to real users (case 1) and to service accounts (case 2).
- Least privilege for case 2: one service account per agent/job, each in its own groups.
- Option B (linking each person to an Integration user) is dropped.

#### Use case 3: the in-app agent acting for a signed-in user (chairman requirement)

When a regular user is logged in to the application and uses an agent, **the agent must run with
that user's personal security**. A service account is used only for unattended jobs.

Finding: `McpConnectionHelper.BuildHeaders` (`APP.BL/AIAgent/GenericAgent/`) builds MCP request
headers from the server registration (`AppAgentMcpServer`: encrypted static headers, headers from
environment variables, or a bearer token from an environment variable). These are identical for
every user, so pointing the agent at `/mcp` with a stored `IntergrationAccessToken` would run every
user's agent as one service account. That violates the requirement.

Design (recommended): **short-lived delegated user token.** When a user starts an agent run, AppAI
mints a token bound to that user (and run); the agent sends it as `Authorization: Bearer` on every
`/mcp` call; the middleware maps it to the real user (same path as the OAuth token in section 4.5:
roles, tenant, audit). It expires with the run and can be revoked. Alternative: call the gateway
in-process inside the user's request (identity already registered), which cannot serve background runs.

Requirements: no long-lived per-user credential stored by the agent; no fallback to a service account
when a user is present; audit rows record the real user and that an agent made the call. To check in
T0: how `GenericAgentEngine` receives the user context when it builds MCP headers.

**T0 finding (traced in code):**
- `GenericAgentEngine.RunAsync` already receives the caller's `AppClientIdentity` and copies user id,
  company id, tenant connection and the user's login session id (`UserSessionId`) into
  `AgentToolContext`. So the engine knows exactly who the user is.
- `CreateMcpPluginAsync(server, ..., context, ct)` (line ~473) receives that context but calls
  `McpConnectionHelper.BuildTransportOptions(server)` with **only the server registration**, so the
  headers are the static ones and the user is dropped. This is the single place to change.
- The MCP client is created per run in `CreateMcpPluginAsync`, so a token minted there naturally lives
  for one run.
- Passing the user's login session id as the header would not work: `/mcp` rejects non-Integration
  sessions on the Integration path by design (and a session id must not be handed to the agent's
  outbound calls anyway).
- Needed change: a registration flag (e.g. "forward caller identity", only valid for the in-house
  gateway URL) so `BuildTransportOptions` takes the context and adds `Authorization: Bearer <delegated
  token>` minted for that user and run; servers without the flag keep static headers.
- Unattended jobs (no signed-in user, `UserId = 0`) must use a service-account registration, never
  a user token.

Original options considered (kept for the record):
Today the middleware only accepts users with `DomainId = Integration` (service accounts). OAuth
sign-in authenticates a **real person**. Options:

| Option | Description | Pros | Cons |
|---|---|---|---|
| **A (recommended)** | OAuth token maps to the signed-in real user. Relax the "Integration user only" rule **for OAuth tokens only**; role grants (`AppSecurityGroupMember`) are checked against that person. | True per-user identity, audit and roles; no extra accounts. | Real users' BL permissions apply to AI-driven calls; admin must grant MCP groups to real users. |
| B | Each real user is linked to a dedicated Integration user; OAuth token maps to the linked account. | Reuses the Integration-user model unchanged. | Needs a link table and admin workflow; two identities per person. |

Whichever is chosen, `ApiAccessPolicy` stays deny-by-default and the downstream `/webapi` call is
made as that user, so BL-level object permissions remain a second layer.

---

## 5. Security requirements

- PKCE **S256 mandatory**; reject `plain`; no implicit flow.
- Exact-match redirect URIs; HTTPS only outside localhost; reject open redirects.
- Authorization codes: single use, ≤ 60 s, bound to client + redirect URI + challenge.
- Access tokens short-lived (e.g. 1 h); refresh tokens rotated on use, revocable; absolute lifetime cap.
- Opaque random tokens (≥ 256 bits) stored as hashes; constant-time comparison.
- Bind tokens to the `resource` (the `/mcp` URL) and reject tokens for other audiences.
- Consent screen names the client and the access being granted; CSRF protection on `/authorize`
  approval (the app registers no antiforgery today; use `state` plus an anti-forgery token on the
  consent form).
- Rate-limit `/token`, `/authorize` and `/register` separately from `/mcp`.
- Audit: sign-ins, consent, token issue/refresh/revoke, bearer validation failures (the failed-token
  gap noted in the architecture doc can be closed here, logged without any tenant data).
- No secrets in logs; `Authorization` stays redacted in HttpLogging.
- Admin kill switch: list and revoke a user's or client's OAuth tokens (extend the existing
  "Integration Tokens"/"MCP API Access" admin area).

---

## 6. Build vs. buy

| Option | Notes |
|---|---|
| Hand-written endpoints (this plan) | Small surface (authorize/token/metadata), full control, fits LLBLGen/master DB. Security-critical code we must test thoroughly. |
| OpenIddict (or similar) embedded | Handles protocol details, PKCE, rotation, discovery. Needs EF Core stores (the repo uses EF Core 9 only in places; storage conflicts with the LLBLGen-first convention) and a customisation layer for AppAI login. |
| External IdP (Entra ID, etc.) | AppAI validates tokens only. Least code, but requires the IdP to issue per-user tokens that map to AppAI users and tenants. Worth checking if SAML users already come from one. |

Recommendation: spike OpenIddict first (one day) to see if its store can sit on master DB without
EF Core friction; fall back to hand-written endpoints if not. Adding a package must be noted in the
plan (project rule).

---

## 7. Work breakdown

| # | Task | Output |
|---|---|---|
| T0 | **DONE, see 7a.** Confirm facts: current MCP authorization spec and the exact discovery/registration behaviour of Claude (claude.ai, Desktop) and ChatGPT; production hosting topology (IIS `/appai` vs root); whether SAML users come from an external IdP (identity option A is decided) | Short findings note |
| T1 | **DONE, see 7b.** Bearer fallback in the middleware (`Authorization: Bearer` → same validation), 401 + `WWW-Authenticate`, unit tests (header only, query string ignored, wrong scheme, precedence). Useful on its own for headers-only mode | PR |
| T2 | **DONE, see 7e** (four tables, ADO.NET stores, 23 tests). Master-DB script for the three tables; DTOs/mappers after reading real schema | SQL + BL |
| T3 | **DONE, see 7f.** Discovery endpoints incl. path variants and the root-to-`/appai` rewrite for `.well-known` | Controller + tests |
| T4 | **DONE, see 7g** (verified over HTTP with a fake session bridge; real-session part not run). `/authorize` + login hand-off + consent page (React) | Controller + UI |
| T5 | **DONE, see 7g.** `/token` (code + refresh, PKCE, rotation) and optional `/revoke` | Controller + BL + tests |
| T6 | **DONE, see 7g** (compiled, not run). Bearer validation path in the middleware, identity registration per section 4.6 | Middleware + BL |
| T7 | **DONE, see 7g** (compiled, not run in a browser). Admin screen: OAuth clients (pre-register client id and redirect URIs), connected users, revoke | React + API |
| T8 | **DONE, see 7g.** Audit events and rate limits for the OAuth endpoints | Code + tests |
| T9 | **DONE, see 7g** (automated review, no findings; no QA loop). Security review (`/security-review`) and QA review loop | Report |
| T6b | **DONE, see 7g** (compiled, not run). Delegated user token for in-app agent runs (mint per run, validate in middleware, wire into `GenericAgentEngine` / `McpConnectionHelper` instead of static headers) | BL + engine change + tests |
| T10 | **DONE.** Docs: update `mcp-gateway-architecture.md`, add client setup guide (Claude, ChatGPT) | Docs |

T1 can ship independently of T2-T10.

---

## 7a. T0 findings (researched 2026-10-14) and status

Sources: MCP authorization spec (draft revision: CIMD preferred, DCR a deprecated fallback), Claude docs
"Authentication for connectors" and "Remote MCP", OpenAI developer docs (ChatGPT auth, developer mode).
Details change; re-check before building each task.

**Status:** T0-T10 and T6b implemented; see section 7g for what was and was not verified.

### Claude (claude.ai, Desktop, mobile, Cowork)

| Topic | Finding | Impact on plan |
|---|---|---|
| Static headers | Beta, limited organizations. **Standard names `authorization`, `x-api-key`, `x-auth-token` work for every connector**; only other names need Anthropic approval. The credential is the organization's, shared by all members. | Corrects an earlier note: `Authorization: Bearer` (T1) needs no approval, a custom header name does. Still one shared identity, so not for per-user access. |
| OAuth + headers | `Authorization` cannot be set as a header on an OAuth connection. Other headers can be sent alongside the bearer token. | As before. |
| 401 | A `401` is **required** to start sign-in; `WWW-Authenticate` on a `200` is ignored. | Middleware must return `401` + `resource_metadata` (T3/T6). |
| Discovery | `resource_metadata` can point to any HTTPS URL. `resource` in the document must equal the MCP URL exactly. Claude uses **only the first** `authorization_servers` entry. Probing `/.well-known/oauth-protected-resource[/<path>]` is only a fallback and needs the platform to serve `/.well-known/*`. | The `/appai` hosting problem is avoided for the resource document by using the `resource_metadata` pointer. |
| Registration | CIMD is used only if AS metadata has **both** `client_id_metadata_document_supported: true` and `"none"` in `token_endpoint_auth_methods_supported`; otherwise DCR (`registration_endpoint`). An admin can instead enter their own client ID. DCR registers a new client on every fresh connection. | Support CIMD and pre-registered clients first; DCR as fallback. |
| Redirect | Hosted apps: `https://claude.ai/api/mcp/auth_callback`. Claude Code: loopback on any port; match `localhost` and `127.0.0.1` ignoring the port. | Redirect-URI allowlist (T2/T4). |
| PKCE | S256 on every request; AS must advertise `code_challenge_methods_supported: ["S256"]`. | As planned. |
| Token endpoint | Must accept `application/x-www-form-urlencoded`; return `invalid_grant` for bad refresh tokens; rotate refresh tokens for public clients; Claude refreshes on 401 and 5 min before expiry; appends `offline_access` if listed in `scopes_supported`. | T5. |
| Latency | 10 s for discovery, registration and token; 30 s for refresh. | Keep these endpoints fast. |
| **Reachability** | Traffic comes from **Anthropic's cloud, `160.79.104.0/21`**. The server needs an **HTTPS URL** reachable from there (private networks need Anthropic's MCP tunnels). The AS host must be reachable too; a WAF can break it. | **New blocking deployment question**: the gateway today is `localhost:52740/appai`. Claude and ChatGPT cannot reach it without a public HTTPS endpoint or a tunnel. |
| Not supported | The `client_credentials` machine-to-machine grant. | Service accounts keep the Integration-token path. |

### ChatGPT

OpenAI docs say ChatGPT supports CIMD (preferred when available), DCR, predefined OAuth clients and PKCE.
DCR runs once per connection and the client is reused, so do not delete or replace registered clients or
users get `invalid_client`. CIMD client metadata is `https://chatgpt.com/oauth/client.json` for servers
meeting the issuer-identification requirements, otherwise `https://chatgpt.com/oauth/{callback_id}/client.json`.
The exact redirect URI is shown on the connection's management page. Developer mode (custom MCP connectors)
is limited to Business and Enterprise/Edu plans on ChatGPT web, enabled by workspace admins, and supports
remote servers only. Not yet tested against a real ChatGPT workspace.

### MCP spec points that shape the server

- The MCP server is an OAuth resource server: it MUST implement protected-resource metadata (RFC 9728) and
  validate that tokens were issued **for it** (audience, via the RFC 8707 `resource` parameter). It MUST NOT
  accept tokens issued for other resources.
- `Authorization` header on every request; tokens never in the query string.
- The AS MUST provide RFC 8414 or OpenID Connect discovery (clients support both) and SHOULD include the
  `iss` parameter in authorization responses (RFC 9207, expected to become MUST).
- `401` for invalid or expired tokens; `403` with `insufficient_scope` for scope problems.
- Issuer with a path (`/appai`): clients try RFC 8414 and OIDC path-insertion forms at the host root, plus the
  OIDC suffix form `{issuer}/.well-known/openid-configuration`. The suffix form works inside the `/appai`
  application; the root forms need the root-to-`/appai` rewrite already used for `/webapi` and
  `/openapi-doc` (`Program.cs:147-157`) or a proxy rule. Confirm the probing order on the spec's discovery
  page before T3 (I did not read that page).

### Hosting and SAML

- The app runs under `UsePathBase("/appai")` with that rewrite. No `web.config` is in the repo, so the IIS
  rules are unknown: ask whoever owns deployment for the production topology.
- `Program.cs` registers only cookie authentication (line 83). No other authentication scheme is registered
  there, so nothing competes for `Authorization: Bearer`. (SAML handler registration was not found in
  `Program.cs`; where SAML is configured is unanswered.)

### Decisions needed before T2

1. **DECIDED (chairman): `/appai/mcp` will be publicly reachable over HTTPS** (no tunnel), together with the
   OAuth endpoints, reachable from `160.79.104.0/21`. Consequences, to handle before go-live:
   - The app has **no forwarded-headers handling** (no `UseForwardedHeaders` / known proxies in `Program.cs`).
     Behind a reverse proxy on the same host, `RemoteIpAddress` is the proxy (often loopback), which would
     (a) defeat the loopback-only check on `/swagger/*` (the full API catalogue) and (b) make the per-IP
     rate limit on `/mcp` count every caller as one. Configure forwarded headers with trusted proxies, or
     block `/swagger` at the proxy, before exposure.
   - Expose only `/appai/mcp`, the OAuth and `.well-known` paths publicly; keep the rest of the app
     (`/webapi`, admin) off the public route.
   - Add WAF or proxy rate limits; keep the OAuth host reachable from Anthropic's range (a WAF can break discovery).
2. **DECIDED (chairman): production runs under `/appai`.** The `/appai` issuer path and the root-to-`/appai`
   rewrite for `.well-known` (precedent `Program.cs:147-157`) apply.
3. **DECIDED (chairman): use OpenIddict with our own ADO.NET stores, and no EF Core anywhere.**
   Package addition, noted per project rules: `OpenIddict.AspNetCore` 7.7.2 only (no
   `OpenIddict.EntityFrameworkCore`). The four OpenIddict stores (applications, authorizations, scopes,
   tokens) are written by us in plain parameterized ADO.NET on the master DB. The unused EF Core packages
   were removed from `AppAI.Web.csproj` (nothing in the solution used them). Known upstream gaps: no native
   dynamic client registration (open issue #2404) and no CIMD, so we build a small registration endpoint on
   OpenIddict's application manager and start with admin pre-registered clients. See 7c and 7d.
4. Are SAML users in an external identity provider? Unanswered.

## 7b. T1 implemented: `Authorization: Bearer` fallback

- `APP.McpGateway/Services/McpCredentialReader.cs` reads the credential from headers only.
  `IntergrationAccessToken` wins; otherwise `Authorization: Bearer <token>` (scheme case-insensitive,
  trimmed). `Basic`, `Negotiate`, an empty bearer value and a query-string value give no credential.
- `AppAI.Web/Auth/IntegrationTokenMiddleware.cs` uses it. The token is still validated as an Integration
  token (`TryRegisterIntegrationTokenIdentity`), so a normal login session id is still rejected. A `401`
  now carries `WWW-Authenticate: Bearer` (bare when no credential, `error="invalid_token"` when one was
  rejected). `resource_metadata` is added with T3/T6, once the metadata exists.
- Tests: `McpCredentialReaderTests` (15 cases). Gateway test project: 92 passing. `AppAI.Web` builds.
- Not covered: the middleware class itself (it calls static BL methods), so no HTTP-level test yet.
- Effect: a Claude connector in headers-only mode (beta organizations) can send
  `Authorization: Bearer <Integration token>`; all members then share that one service identity.

---

## 7c. OpenIddict spike results (2026-10-14)

Throwaway project in the scratchpad (not in the repo): OpenIddict 7.7.2, EF Core in-memory store, net10.0,
`UsePathBase("/appai")`, a stand-in `/mcp` endpoint, and a fake login (always user 42). Every result below
was observed with real HTTP requests (curl).

**Works as needed**

| Check | Result |
|---|---|
| Discovery document under `/appai` | `/appai/.well-known/openid-configuration` served; endpoints are `/appai/oauth/*`. |
| `iss` in the authorization response (RFC 9207) | Present, and `authorization_response_iss_parameter_supported: true`. |
| Code flow + PKCE S256 | Authorize returns a code; token exchange returns access + refresh tokens. |
| Opaque (reference) tokens | Tokens are 43-character random strings, not JWTs, stored as hashes by OpenIddict. |
| Token accepted at the resource | `/mcp` with the token returns the subject and audience; garbage token returns 401. |
| Audience binding (RFC 8707 `resource`) | The access token's `aud` equals the MCP URL. Requests for an unregistered resource get `invalid_target`. |
| Authorization code single use | Replay returns `invalid_grant` ("already been redeemed"). |
| Wrong PKCE verifier | `invalid_grant`. A request with no `code_challenge` returns 400. |
| Redirect URI | A non-registered redirect URI returns 400 (exact match). |
| Refresh rotation | Each refresh returns a new refresh token. |
| Dynamic registration | A 25-line `POST /oauth/register` over `IOpenIddictApplicationManager` created a public PKCE client that then authorized successfully. |

**Required configuration (found the hard way)**

- `RegisterResources(<mcp url>)` on the server **and** a scope whose `Resources` contain the MCP URL **and**
  the `rsrc:` permission on each client; without them every `resource` is rejected.
- Remove plain PKCE: `o.Configure(x => x.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain))`
  (default metadata advertises `plain` and `S256`).
- `SetRefreshTokenReuseLeeway(TimeSpan.Zero)`: the default 30 s lets a just-used refresh token work again.
  With 0 the replay is rejected.
- Do not make the OpenIddict server scheme the default authentication scheme (it throws at startup). In
  `AppAI.Web` the default is Cookies, so `/mcp` must request the OpenIddict validation scheme explicitly.
- The discovery document's `token_endpoint_auth_methods_supported` lists only `client_secret_*` and
  `private_key_jwt`. Claude's CIMD path needs `"none"` and `client_id_metadata_document_supported`; a DCR
  public client works but the metadata should advertise `none` and `registration_endpoint`, so add them
  with a handler or by post-processing the document. Not yet done.

**Findings that change the plan** (items 1 and 2 are superseded by 7d: EF Core was dropped, and the
reuse-detection result below was an artifact of the in-memory EF store)

1. **EF Core version conflict.** OpenIddict.EntityFrameworkCore 7.7.2 depends on EF Core 10.0.11. `AppAI.Web`
   pins EF Core SqlServer and Design at 9.0.0. A project reference build can resolve, but the EF Core
   packages must be on one major version; in the spike, mixing 9.0.0 (InMemory) with 10.0.11 failed at
   runtime (`MissingMethodException`). Adding OpenIddict therefore needs the EF Core packages in `AppAI.Web`
   raised to 10.x, or an older OpenIddict line that targets EF Core 9 (not tested). A search of `AppAI.Web` and
   `APP.BL` found **no code using EF Core** (no `DbContext`, no `Microsoft.EntityFrameworkCore` usings), so the
   9.0.0 package references look unused and raising them to 10.x should be low-risk. Confirm with a build.
2. **Reuse detection does not revoke the chain.** After a replayed old refresh token was rejected, the newer
   refresh token and its access token still worked. Revoking all tokens of the authorization on replay would
   need a custom handler. Mitigated by short access tokens and the revoke endpoint; track as a gap.
3. **Not tested:** real SQL Server master DB store and its migrations (the spike used in-memory), the real AppAI
   login and consent screen, the `.well-known` root rewrite, the 401 + `resource_metadata` challenge from the real
   middleware, cleanup of unused dynamically registered clients, and Claude/ChatGPT themselves.

**Conclusion:** OpenIddict covers the protocol work (PKCE, codes, rotation, audience binding, `iss`). We still
build: login/consent hand-off, the registration endpoint (and later CIMD), metadata additions, the bearer
validation path in `/mcp`, and the EF Core store on the master DB.

---

## 7d. De-risk spike: OpenIddict with our own stores, no EF Core (2026-10-14)

Result: **it works.** Same throwaway project, but the EF Core packages are gone (the restored package graph
has zero EF Core entries) and the four stores are our own classes. Each field is a plain value or a JSON
string, i.e. what an ADO.NET column would hold; the spike keeps rows in dictionaries, the real version
swaps only those calls for SQL. The same curl test set as 7c was rerun.

| Check | Result |
|---|---|
| All four store interfaces implemented | Compiled first time apart from one constants import; 42 / 32 / 28 / 43 members. |
| Authorize, token, refresh, rotation, revoke, `iss` | All pass; revoking an access token (RFC 7009) makes `/mcp` return 401. |
| Code replay, wrong PKCE verifier, wrong resource, bad redirect URI | All rejected as before. |
| Dynamic registration stub | Created client signs in. |
| **LINQ-query methods** (`Count/Get/List` taking `Func<IQueryable<..>>`) | **Never called** during the whole flow (0 hits, instrumented). |
| Store calls actually used | Token: `Create`, `FindByReferenceId`, `Revoke*`; Authorization: `Create`, `FindById`; Application: `Create`, `FindByClientId`, `FindByRedirectUri`. |
| **Reuse detection** | Replaying an old refresh token returned `invalid_grant` **and revoked the whole chain**: the newer refresh token was rejected and its access token got 401. This corrects the earlier note in 7c; that result came from the in-memory EF store. |

What it means for the build:

- The real stores must implement the dedicated lookups and revokes above with indexed SQL (reference id,
  client id, redirect URI, subject, authorization id, status). The LINQ-query methods can throw
  `NotSupportedException` in production, since the flow never uses them. The risk is a future OpenIddict
  release or an admin feature that calls them, so keep a log line on that path and a test that fails if it
  is hit.
- `Prune` (expired tokens and authorizations) must be a scheduled job in our stores; OpenIddict only calls it.
- Hash secrets and reference ids at rest (OpenIddict hashes reference tokens and client secrets itself
  before they reach the store; verify against the SQL columns).
- Still untested: real SQL Server tables and indexes, concurrency (two refreshes at once), a stored
  procedure vs inline SQL choice, and the load of the token table.

Next in T2: design the four master-DB tables from the entity fields in the spike (`Stores.cs` in the
scratchpad is the field list), write the migration script, then the ADO.NET stores with tests.

---

## 7e. T2 implemented: master-DB tables and ADO.NET stores (2026-10-16)

**Status:** T0, T1, T2 done. T3-T10 not started. Nothing is wired into `AppAI.Web` yet (no endpoints, no
registration), so nothing here runs in production. The script has **not** been applied to any real database.

What exists:

- `Document/Design/MasterDB_OAuthTables.sql`: four master-DB tables `AppOAuthApplication`, `AppOAuthScope`,
  `AppOAuthAuthorization`, `AppOAuthToken` (run once against AppMasterDB, like the other master scripts). Each
  table and index is guarded separately, so a rerun repairs a partly applied script. Client id, scope name and
  token reference id use a binary collation (case-sensitive). Deleting a client cascades to its authorizations
  and tokens. `CompanyId` columns are filled from a `company_id` property when present (to be wired in T4/T6).
- New project `APP.OAuth` (class library, `OpenIddict.Core` + `Microsoft.Data.SqlClient`, no EF Core, no `APP.BL`
  dependency), added to `AppAI.Core.sln`: four stores (`ApplicationStore`, `ScopeStore`, `AuthorizationStore`,
  `TokenStore`) on a shared `SqlStoreBase` (parameterized SQL only, optimistic concurrency on `ConcurrencyToken`,
  no file over 200 lines), plus `AddAppOAuthStores(connectionString)` for DI. The LINQ-query overloads throw
  `NotSupportedException` (the spike showed they are never called).
- New project `APP.OAuth.Tests`: 23 tests against a throwaway **LocalDB** database built from the real script
  (skipped, not failed, when LocalDB is missing). Covers round trips, case sensitivity, exact redirect-URI and
  resource matching, concurrency (two simultaneous refresh redemptions: exactly one wins), cascade delete,
  revoke, prune rules and the unsupported overloads.

**End-to-end run on the real SQL stores** (the spike server with its in-memory stores swapped for `APP.OAuth`,
same curl script): authorize, token, `/mcp` access, refresh rotation, replay of an authorization code and of an
old refresh token (the whole chain is revoked), wrong PKCE verifier, wrong resource, bad redirect URI, a client
id in the wrong case, dynamic registration, access-token revocation: all behave as in the spike. A server restart
keeps tokens valid and refresh working (state is in SQL), re-seeding on startup creates no duplicates, and the raw
access and refresh token strings are not present in any column.

**Bugs the testing found and fixed:**

1. `sqlcmd` runs with `QUOTED_IDENTIFIER OFF`, which makes filtered-index creation fail; and index creation sat
   inside the create-table block, so a failed first run could never be repaired. The script now sets the options
   and guards every index on its own.
2. Token and authorization `Type` was `NVARCHAR(50)`, but OpenIddict 7 stores the type as a URN such as
   `urn:openiddict:params:oauth:token-type:authorization_code` (56 characters). Only the end-to-end run caught
   it (the unit tests used short values), so there is now a unit test with the real URN. Column is `NVARCHAR(150)`.

**Still to do before this is production-ready**

- Apply the script to a real master DB (dev) and run the end-to-end flow there.
- A scheduled job calling the stores' prune methods (expired tokens and authorizations).
- Index/performance check of the token table under load; two simultaneous refreshes through HTTP (the store-level
  race is tested, the HTTP path is not).
- T3-T10: discovery endpoints, authorize/consent with the real AppAI login, the registration endpoint, CIMD later,
  bearer validation in `/mcp`, admin screen, agent delegated tokens, security review.

---

## 7f. T3 implemented: discovery metadata and the 401 challenge (2026-10-16)

**Status:** T0-T3 done. T4-T10 not started. Everything below is **off unless `OAuth:Enabled` is true**, so
production behaviour is unchanged until it is configured. Enabling it today only publishes metadata and
the OpenIddict server skeleton: `/oauth/authorize` and `/oauth/token` have no handlers yet (T4/T5), so do not
enable it on a public host before then.

What exists (all in `APP.OAuth`, wired into `AppAI.Web`):

- `OAuthOptions` (section `OAuth`), `OAuthUrls` (issuer, resource, metadata and endpoint URLs built from one
  `PublicBaseUrl`; rejects a missing, non-absolute or non-https URL, http only for localhost; issuer has no
  trailing slash), `OAuthChallenge`, `OAuthServiceExtensions.AddAppOAuth(...)`, `MapAppOAuthDiscovery()`.
- Authorization-server metadata (served by OpenIddict) at every URL a client may probe:
  `/appai/.well-known/openid-configuration` and `.../oauth-authorization-server` (suffix form, in-app), plus the
  host-root `/.well-known/openid-configuration`, `/.well-known/oauth-authorization-server` and the path-insertion
  forms `/.well-known/openid-configuration/appai`, `/.well-known/oauth-authorization-server/appai`. Unknown suffixes
  return 404.
- Protected-resource metadata (RFC 9728) at `/appai/.well-known/oauth-protected-resource`, plus the root and
  path forms (`/.well-known/oauth-protected-resource[/appai/mcp]`); another resource path returns 404. Content:
  `resource` = the MCP URL, `authorization_servers` = the issuer, `scopes_supported`, bearer in header.
- Metadata content: `issuer` equals the PRM `authorization_servers` value exactly (no trailing slash), S256 only,
  `none` added to the token-endpoint auth methods (public PKCE clients), `authorization_response_iss_parameter_supported`,
  grant types authorization_code and refresh_token, scopes `openid offline_access mcp`; `registration_endpoint`
  only when `OAuth:RegistrationEnabled` is true (leave false until the endpoint exists).
- `Program.cs`: `AddAppOAuth(...)`; the existing root-to-`/appai` rewrite now also covers `/.well-known`;
  `MapAppOAuthDiscovery()`. `AppAI.Web.csproj` references `APP.OAuth`.
- `IntegrationTokenMiddleware`: a rejected `/mcp` request now returns
  `WWW-Authenticate: Bearer resource_metadata="<issuer>/.well-known/oauth-protected-resource", scope="mcp"`
  (with `error="invalid_token"` when a credential was sent), which Claude and ChatGPT require to start sign-in.
  With OAuth disabled the header is the plain `Bearer` as before.

Configuration to add (appsettings is a credential file, so the agent does not edit it):

```json
"OAuth": {
  "Enabled": false,
  "PublicBaseUrl": "https://mcp.example.com/appai",
  "McpPath": "/mcp",
  "Scope": "mcp",
  "TlsTerminatedUpstream": true,
  "RegistrationEnabled": false,
  "SigningCertificateThumbprint": "<cert thumbprint, required outside Development>",
  "EncryptionCertificateThumbprint": "<cert thumbprint, required outside Development>"
}
```

**Verified over real HTTP** with a test host that mirrors the `AppAI.Web` pipeline (root rewrite, `UsePathBase("/appai")`,
authentication) and uses exactly the `APP.OAuth` code: all 14 URL variants above, the metadata fields, issuer equality,
the two 401 challenges, the OAuth-disabled mode (all discovery URLs 404, plain `Bearer`). 56 `APP.OAuth.Tests` pass
(33 new: URL building and validation, path variants, challenge headers). `AppAI.Web` builds.

**Bugs the testing found and fixed**

1. OpenIddict resolves endpoint addresses against the issuer. An address with a leading slash
   (`/.well-known/openid-configuration`) points at the host root and drops `/appai`, so the metadata endpoint was
   silently unreachable (404, no log). Relative addresses without the leading slash are required.
2. OpenIddict printed the issuer with a trailing slash (needed internally so relative endpoints resolve under `/appai`),
   which would not equal the PRM `authorization_servers` value; strict clients compare them exactly. The metadata
   handler now writes the exact issuer.

**Not verified / still open**

- `AppAI.Web` itself was not run with OAuth enabled (it needs the databases and secrets); only built, and the same
  code was exercised in the test host. First real run happens with T4.
- Behind a reverse proxy, `TlsTerminatedUpstream` disables OpenIddict's https check; forwarded-header handling is
  still missing in the app (see section 7a decisions) and should be configured with trusted proxies.
- No CORS headers on the metadata documents (browser-based clients such as an MCP inspector in a page would need them;
  Claude and ChatGPT call from their servers).
- Production certificates for token signing/encryption: required outside Development, not yet provisioned.
- Whether Claude and ChatGPT accept this metadata end to end is untested (needs a public HTTPS URL).

---

## 7g. T4-T10 and T6b implemented (2026-10-16): sign-in, tokens, admin, agents, review

**Status:** every task in section 7 is implemented. **Verification differs by part, read the matrix first.** Everything
is still **off unless `OAuth:Enabled` is true**. Operations guide: `mcp-oauth-setup-guide.md`.

### Verification matrix (what was actually proven)

| Part | How verified | Result |
|---|---|---|
| OAuth protocol in `APP.OAuth`: dynamic registration, authorize, consent ticket, token, refresh and reuse detection, revoke, discovery, agent token, company scoping | **52 HTTP checks** (`APP.OAuth.E2E/flow.sh`) on the real code and real SQL stores in LocalDB, with a fake session bridge | 52 pass, 0 server errors |
| Same logic at unit level | `APP.OAuth.Tests`: **108** tests (stores on LocalDB, ticket, registration rules, agent token, URLs, challenge) | pass |
| Startup scope seeding (create, idempotent, repair, tables missing) | run against the e2e host | pass |
| Solution build, other test projects | `dotnet build AppAI.Core.sln`; gateway tests 92, BL tests 18 | pass |
| React (TypeScript, ESLint) | `tsc --noEmit` clean; ESLint clean on the new files | clean |
| **`AppOAuthGrantSessionBL` on the real master DB, `OAuthIdentityBridge`, both controllers, the `/mcp` bearer path in `IntegrationTokenMiddleware`, rate-limit buckets, the agent hook in `GenericAgentEngine`, `Program.cs` wiring** | **Compiled only.** `AppAI.Web` needs the real databases and secrets, which were not used | **not run** |
| **React pages in a browser** (consent, my/company connections, OAuth clients, login return) | **Not run** | **not run** |
| Claude and ChatGPT end to end | Needs a public HTTPS URL; not tested | **not run** |

### What was built

- **T4 sign-in and consent.** `GET/POST /oauth/authorize` (passthrough): without approval it redirects to the consent page
  (`OAuth:ConsentPageUrl`, the React route `/mcp-authorize`); with a valid ticket it creates a grant session and an
  authorization (properties `company_id`, `grant_session`) and lets OpenIddict issue the code. `prompt=none` returns
  `interaction_required`. The page calls `webapi/OAuthConsent/Describe|Approve|Deny`, which **re-validate** the URL
  (our endpoint, registered client and redirect URI, S256, the exact resource, allowed scopes, no client-supplied
  `consent_ticket`/`consent_deny`). The ticket is a 3-minute Data Protection payload bound to user, company and a hash of
  client, redirect URI, scope, resource, PKCE challenge and state. The login returns to the page afterwards
  (`helper/postLoginRedirect.ts`, allow-list of in-app paths).
- **T5 tokens.** `POST /oauth/token`: authorization code and refresh grants only; a connection ends when its grant session
  ends (sign-out of that session row, user deactivated or deleted, admin revoke, absolute lifetime `RefreshTokenDays`).
  Dynamic registration `POST /oauth/register` (`OAuth:RegistrationEnabled`): public PKCE clients only, https on an
  allow-listed host or loopback http, at most 5 redirect URIs, capped total, unused ones removed after
  `DynamicClientRetentionDays`. Startup step `OAuthBootstrapService` creates/repairs the `mcp` scope and its resource.
- **T6 `/mcp` bearer path.** `IntegrationTokenMiddleware` accepts (a) Integration tokens (unchanged), (b) OAuth access
  tokens (OpenIddict validation: audience = the MCP URL, scope `mcp`, then the grant session and active user),
  (c) agent run tokens (`aat.`). GUID-shaped bearers skip the OAuth lookup. Rejections are logged (credential kind and
  IP, never the token). **The id forwarded to this instance's own API is the grant session**, not the OAuth token.
- **T6b agent as the signed-in user.** `McpConnectionHelper.UserTokenServerUrls/UserTokenFactory` (set from
  `OAuth:AgentTokenServerUrls`): for those exact server URLs the engine sends a per-run token for the user who started the
  run and drops every stored credential header; with no signed-in user the server is skipped (fail closed).
- **T7 administration.** `webapi/OAuthAdmin`: company admin lists and ends the connections of their company
  (`Connections`, `RevokeConnection`); host SysAdmin lists, registers and deletes OAuth clients. `webapi/OAuthConsent`
  also gives every user `MyConnections` / `RevokeMyConnection`. React: tab "AI Connections" in company security
  settings, route `my-ai-connections` (all users) and `oauth-clients` (SysAdmin), neither in the DB-driven menu.
  Both controllers require an interactive login and are excluded from the API catalogue the gateway can expose.
- **T8 limits and audit.** Per-IP rate buckets: `/mcp` 120/min, `/oauth/register` 10, `/oauth/token` 30,
  `/oauth/authorize` 60, `/webapi/OAuthConsent|OAuthAdmin` 60, `/.well-known` 120. Consent, deny, disconnect and admin
  actions write tenant audit rows (A006 privileged action); every OAuth decision is also logged (NLog, no secrets).
  SysAdmin actions have no tenant, so they are logged only. Failed `/mcp` credentials are logged, not audited.
- **T9 review.** An automated security review of all new and changed code found no high-confidence vulnerability
  (consent ticket binding, redirect handling, PKCE, grant-session misuse, cross-company access, agent token scope, SQL,
  React). It is not an independent human review, and the planned QA loop with a second reviewer was not run.

### Bugs found by testing and fixed

1. A test-script mistake (greedy match picked the oldest connection) briefly looked like "revoke does not stop the token";
   the library had revoked exactly what it was told to. Test script fixed.
2. The e2e host was seeding the `mcp` scope that the real app had nobody to create. Added `OAuthBootstrapService`
   and removed the seeding from the e2e host, so the test now covers it.
3. A setup note said an AppAI source should use header `IntergrationAccessToken`. `/webapi` does not read it: it reads
   `CurrentUserSessionId`. Corrected below and in the guide.

### Still open before real use

- Run the whole flow on a **dev copy of AppAI** (apply the SQL, enable OAuth, sign in, connect, call a tool, revoke) and
  open the React pages. Until then the real-session code is unproven.
- **Unverified assumption:** nothing prevents a second `AppSecurityUserSession` row per user (no unique index on `UserId`
  was found in the repo, but the master schema script is not in the repo). If one exists, `CreateGrantSession` fails and
  authorize returns 500.
- `ForwardCallerToken` must be on **only** for this instance's own API source, with `AccessTokenHeaderName`
  **`CurrentUserSessionId`** (not `IntergrationAccessToken`); on any other source it would send a session id to a third party.
- Multi-node deployments need a shared, persisted Data Protection key ring (consent tickets and agent tokens depend on it).
- Behind a reverse proxy: trusted forwarded headers (rate limits and the loopback-only `/swagger` rule need the real client IP).
- Production signing and encryption certificates; a public HTTPS URL; the consent page reachable at the configured URL.
- No CIMD yet (dynamic registration and admin pre-registration only); consent is asked every time (no remembered consent);
  failed `/mcp` credentials are not written to the tenant audit table; ChatGPT unverified.

---

## 8. Verification plan

- Unit: PKCE verification, code single-use and expiry, redirect URI matching, refresh rotation,
  token hashing, middleware header precedence.
- Integration (HTTP): no token → 401 with `WWW-Authenticate`; discovery documents at every
  variant; full authorization-code + PKCE round trip; refresh; revoked token → 401; token for
  user in tenant A cannot reach tenant B data; role denial still returns "not found" and audits.
- Manual with real clients: add the connector in Claude Desktop and in ChatGPT, complete sign-in,
  list tools, run an allowed and a denied operation. **This has not been verified for either
  client and is the main risk.**

---

## 9. Risks and open decisions

1. **Identity mapping (4.6)**: decided, option A for OAuth users; service accounts keep Integration tokens.
2. **Hosting path (4.2)**: root `.well-known` under IIS `/appai`.
3. **Client behaviour** (registration method, which discovery URL is requested) can differ between
   Claude and ChatGPT and change over time; verify against live docs and real clients.
4. **Scope of the work**: this makes AppAI.Web an authorization server, a long-lived security
   surface to maintain. If only in-house clients matter, the header design (and T1) is enough.
5. **Alternative if OAuth is deferred**: headers-only connector with `Authorization: Bearer
   <Integration token>` where the org has the beta, accepting one shared identity per connector;
   or `mcp-remote` header shim for local desktop users.
6. Package additions (if OpenIddict) must be recorded in the plan before use.

## 10. Recommendation

1. Do **T0** and **T1** now (small, independently useful, resolves the open facts).
2. Identity option A is decided (see 4.6).
3. If Claude Desktop / ChatGPT per-user access is a real requirement, proceed with T2-T10,
   starting with a short OpenIddict spike.
