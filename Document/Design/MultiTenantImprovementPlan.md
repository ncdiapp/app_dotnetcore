# Multi-Tenant Improvement Plan

**Project:** AppAI / AppBuilder Platform  
**Author:** Architecture Review  
**Date:** 2026-05-08  
**Status:** Draft

---

## 1. Current State Summary

### Architecture

The platform uses a **hybrid two-database model**:

| Database | Stores | Isolation mechanism |
|---|---|---|
| Master / Host DB | Company registrations, user accounts, sessions, app definitions (forms, searches, workflows), security rules, menus, `AppDataSourceRegister` | Row-level via `AppCreatedByCompanyId` filter (manual) |
| Tenant DB (per company) | Runtime business data | Physical — separate database per tenant |

### Tenant Identity Flow (current)

```
User login
  → Session lookup in Master DB → get CurrentWorkingCompanyId
  → AppDataSourceRegister lookup → get tenant ConnectionString
  → AppClientIdentity populated { CompanyId, ConnectionString, DatabaseName }
  → Stored in HttpContext.Current.Items for request duration
  → BL reads ServerContext.Instance.CurrentUserDbConnectionString per call
```

### Key Files

| File | Role |
|---|---|
| `APP.BL/AppMgr/AppCompanyBL.cs` | Company registration, domain token lookup |
| `APP.BL/AppMgr/AppSaasUserSessionMgtBL.cs` | Session validation, tenant identity setup |
| `APP.BL/AppMgr/AppCacheManager.cs` | Per-company DataSource caching (`GetCurrentCompanyMasterDataSource`) |
| `APP.BL/AppMgr/AppSecurityUserBL.cs` | User CRUD, user-company assignment |
| `APP.BL/AppMgr/AppSaasAccountUserBL.cs` | Partner invitation, cross-company user access |
| `APP.BL/AppMgr/AppSecurityAuthenticationBL.cs` | Login, user type resolution per company |
| `APP.Framework/AppClientIdentity.cs` | Tenant identity carrier struct |
| `APP.Framework/HttpClientIdentityProvider.cs` | Stores identity in `HttpContext.Current.Items` |
| `PlmApplication/Server/WebApi/BaseWebApiController.cs` | Per-request session/identity initialization |

---

### User Model (current)

`AppSecurityUser` has two distinct company fields:

```
AppSecurityUser (Master DB)
├── MyOwnCompnanyId       ← the company this user BELONGS TO (home company)
└── AppCreatedByCompanyId ← the company that created this account
```

Users can access **multiple companies** via the partner invitation table:

```
AppBusinessPartnerInviteUser (Master DB)
├── UserId               ← user identity (from AppSecurityUser)
├── AppCompanyId         ← which company is granting access
└── AppBusinessPartnerId ← their business partner record in that company
```

User types (`EmAppUserType`) and their company relationship:

| User Type | `MyOwnCompnanyId` | Access model |
|---|---|---|
| `SysAdmin` | Platform company | All tenants |
| `SaasCompanyAdmin` | Own company | Own company only |
| `Employee` | Own company | Own company only |
| `Customer` | null | Via `AppBusinessPartnerInviteUser` |
| `Supplier` | null | Via `AppBusinessPartnerInviteUser` |
| `ClientAgent` | null | Via `AppBusinessPartnerInviteUser` |
| `SupplierAgent` | null | Via `AppBusinessPartnerInviteUser` |
| `Integration` | null | API key access |

**Effective role per login** is determined dynamically by `UpdateUserContextDomainUserType()`:
- Checks if the user has a `BusinessPartner` record linked to the working company
- If yes → their role becomes the partner's `PartnerType` (not their base `DomainId`)
- Meaning: the same user account can be an **Employee** in Company A and a **Customer** in Company B

---

## 2. Current Problems

### P1 — CRITICAL: Manual tenant filter in shared Master DB

App definitions (forms, searches, workflows, menus, security rules) live in the Master DB filtered by `AppCreatedByCompanyId`. This filter must be added manually to every query.

**Evidence of gaps found:**

| BL Class | `AppCreatedByCompanyId` references |
|---|---|
| `AppTransactionBL` | 1 |
| `AppFormBL` | **0** |
| `AppSearchBL` | **0** |

`AppFormBL` and `AppSearchBL` have zero references to the company filter. A query missing this filter exposes one tenant's form/search configuration to another tenant.

**Risk:** Silent cross-tenant data leak in platform configuration tables.

### P2 — CRITICAL: Plaintext connection strings in Master DB

```csharp
public System.String ConnectionString  // stored as plain text in AppDataSourceRegister
```

If the Master DB is compromised, every tenant's database credentials are exposed in a single query.

### P3 — HIGH: `HttpContext.Current` not async-safe

Tenant context is stored in `HttpContext.Current.Items`. Any `async/await` code that does not capture `HttpContext` before the `await` can lose the tenant identity or pick up another request's context under load.

### P4 — HIGH: No schema migration strategy across tenant DBs

No automated mechanism to run schema migrations against all registered tenant databases at deploy time. Tenant DBs can drift out of sync with the Master DB schema.

### P5 — HIGH: No connection pool management per tenant

Each `new DataAccessAdapter(connectInfo, dbName)` relies on ADO.NET default pooling keyed by connection string. No visibility into pool health or size limits per tenant. Can hit SQL Server connection limits under load.

### P6 — MEDIUM: No custom URL per tenant

`CompanyDomainIdentityToken` exists on `AppCompany` but is not wired to request routing. All tenants share the same application URL. No subdomain or custom domain support.

### P7 — MEDIUM: No tenant provisioning automation

Creating a new tenant requires manual steps: create database, run SQL scripts, insert into `AppDataSourceRegister`. No provisioning API.

### P8 — MEDIUM: Master DB is a single point of failure

All authentication, session management, and tenant routing depend on one database. If it goes down, every tenant is offline simultaneously.

### P9 — LOW: `_hostCompanyId = 1` hardcoded

```csharp
private static readonly int _hostCompanyId = 1;
```

### P10 — MEDIUM: No self-service user administration per tenant

Security groups (`AppSecurityGroup`) and group membership (`AppSecuritySysObjGroupUser`) live in the Master DB. A company admin managing their own users and roles is operating on shared infrastructure with manual company filters. There is no clean boundary that prevents a misconfigured query from leaking role assignments across tenants.

---

## 3. Target Architecture

### 3.1 Database Layout

```
┌─────────────────────────────────────────────────────┐
│              Master DB  (lean — auth & routing)     │
│                                                     │
│  AppCompany           — company registration        │
│  AppSecurityUser      — user accounts (shared)      │
│  AppSecurityUserSession — sessions                  │
│  AppDataSourceRegister  — tenant DB pointers        │
│  TemplateLibrary        — shared starter templates  │
└─────────────────────────────────────────────────────┘
          │                   │                   │
          ▼                   ▼                   ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│   Tenant A DB    │ │   Tenant B DB    │ │   Tenant C DB    │
│                  │ │                  │ │                  │
│ App definitions  │ │ App definitions  │ │ App definitions  │
│ Forms, searches  │ │ Forms, searches  │ │ Forms, searches  │
│ Workflows        │ │ Workflows        │ │ Workflows        │
│ Security rules   │ │ Security rules   │ │ Security rules   │
│ Menus            │ │ Menus            │ │ Menus            │
│ ─────────────    │ │ ─────────────    │ │ ─────────────    │
│ Runtime data     │ │ Runtime data     │ │ Runtime data     │
└──────────────────┘ └──────────────────┘ └──────────────────┘
```

**Key change:** App definitions, security rules, and menus move from Master DB to each tenant's database. The `AppCreatedByCompanyId` filter is no longer needed on these tables — isolation is enforced by the database connection itself.

### 3.2 What Stays in Master DB

Only these concerns genuinely need to be shared:

| Table | Reason it stays in Master DB |
|---|---|
| `AppCompany` | Company registration, billing, domain token lookup |
| `AppSecurityUser` | Users can belong to multiple companies — must be a single global identity |
| `AppBusinessPartnerInviteUser` | Cross-company access grants — spans two tenants by definition |
| `AppDataSourceRegister` | The routing directory itself — must be accessible before tenant is known |
| `AppSecurityUserSession` | Sessions (or migrate to Redis entirely) |
| Template library | Read-only shared starter templates for new tenant onboarding |

### 3.3 Tenant Identity Flow (target)

```
User login
  → Session lookup in Master DB → get CurrentWorkingCompanyId   (unchanged)
  → AppDataSourceRegister lookup → get tenant ConnectionString   (unchanged)
  → AppClientIdentity populated                                  (unchanged)
  ↓
  ALL subsequent queries (config + data) → tenant DB
  No AppCreatedByCompanyId filter needed
```

### 3.4 URL Routing Architecture

```
DNS Layer
  *.appai.com          → platform IP  (wildcard A record)
  app.companya.com     → CNAME appai.com  (tenant custom domain)

Reverse Proxy (nginx / Azure Front Door)
  - SSL termination
    · Wildcard cert for *.appai.com
    · Let's Encrypt auto-provision for custom domains
  - Passes Host header to .NET application

Tenant Resolution Middleware (.NET)
  Host: companyA.appai.com
    → extract subdomain "companyA"
    → AppCompany WHERE CompanyDomainIdentityToken = "companyA"
    → CompanyId = 42
    → resolve AppDataSourceRegister
    → set pre-auth tenant context

  Host: app.companya.com
    → AppDataSourceRegister WHERE CustomDomain = "app.companya.com"
    → CompanyId = 42
```

### 3.5 User Management Model (target)

Responsibility is split cleanly between Master DB (identity) and Tenant DB (authorization):

```
MASTER DB — who you are
┌──────────────────────────────────────────────────┐
│ AppSecurityUser                                  │
│   UserId, LoginName, PasswordHash, Email,        │
│   MyOwnCompnanyId                                │
│                                                  │
│ AppBusinessPartnerInviteUser                     │
│   UserId ↔ AppCompanyId ↔ AppBusinessPartnerId  │
│   (cross-company access grants)                  │
└──────────────────────────────────────────────────┘

TENANT DB — what you can do in this company
┌──────────────────────────────────────────────────┐
│ AppSecurityGroup      (roles for this company)   │
│ AppSecuritySysObjGroupUser  (UserId → GroupId)   │
│ AppSecuritySysObj     (object-level permissions) │
│ AppUserProfile        (tenant-specific settings) │
└──────────────────────────────────────────────────┘
```

**User management flows per tenant:**

**Add internal employee:**
```
Company Admin → User Management
  → creates account: INSERT master.AppSecurityUser { MyOwnCompnanyId = A }
  → assigns role:    INSERT tenantA.AppSecuritySysObjGroupUser { UserId, GroupId }
  → user can log in to companyA.appai.com only
```

**Invite external partner (supplier / customer):**
```
Company Admin → Business Partner → Invite
  → checks master.AppSecurityUser by email
      exists  → INSERT master.AppBusinessPartnerInviteUser { UserId, AppCompanyId=A }
      missing → INSERT master.AppSecurityUser (pending) + send invite email
  → INSERT tenantA.AppSecuritySysObjGroupUser { UserId, GroupId=SupplierGroup }
  → partner accesses companyA.appai.com as Supplier
  → their own company account is unaffected
```

**Remove user access:**
```
Company Admin → removes user
  → DELETE tenantA.AppSecuritySysObjGroupUser WHERE UserId = X
  → DELETE master.AppBusinessPartnerInviteUser WHERE UserId=X, AppCompanyId=A
  → user X can no longer log into this tenant
  → user X's accounts with other companies are untouched
```

**Company admin self-service boundary:**

| Action | Where | Admin can do it? |
|---|---|---|
| Create / deactivate user login | Master DB | Yes (scoped to own company) |
| Reset password | Master DB | Yes (own company users only) |
| Create security groups / roles | Tenant DB | Yes — fully self-service |
| Assign users to roles | Tenant DB | Yes — fully self-service |
| Set field-level permissions | Tenant DB | Yes — fully self-service |
| See other company's users | Master DB | No — filter enforced |
| See other company's roles | Tenant DB | No — wrong DB connection |

### 3.6 Connection String Security

```
AppDataSourceRegister stores:
  SecretReference = "keyvault://appai-prod/tenant-42-db"  ← secret name only, never the credentials

At request time:
  Azure Key Vault / AWS Secrets Manager
    → returns actual connection string
    → cached in-process for TTL (15 min)
    → never persisted in application DB
```

---

## 4. Improvement Phases

---

### Phase 1 — Fix Critical Security Issues
**Timeline:** 2–3 weeks  
**Risk:** Low (no data movement, additive changes only)

#### 1.1 Audit and fix missing `AppCreatedByCompanyId` filters

Scan every query against Master DB tables that hold tenant-scoped configuration. Any query missing the filter is a potential data leak.

**Tables to audit:**
- `AppForm` / `AppTransactionUnit` / `AppTransactionField`
- `AppSearch` / `AppSearchView` / `AppSearchCriteria`
- `AppListMenu` / `AppListMenuItem`
- `AppSecuritySysObjGroupUser` / `AppSecurityGroup`
- `AppWorkflow` / `AppConditionalAction`
- `AppDashboard` / `AppReport`
- `AppMessageTemplate`

**Action:** Add `AppCreatedByCompanyId == currentCompanyId` predicate to every affected query in `AppFormBL`, `AppSearchBL`, and all other BL classes with zero references.

**Verification:** Integration test — create two companies each with a form, query forms as Company A, assert Company B forms are not returned.

#### 1.2 Encrypt connection strings

Replace plaintext `ConnectionString` in `AppDataSourceRegister` with a secret reference.

**Option A (quick):** Encrypt at-rest using AES with a key stored in `web.config` (not ideal but immediate improvement).

**Option B (proper):** Store secret name in `AppDataSourceRegister.SecretReference`, resolve actual connection string from Azure Key Vault or AWS Secrets Manager at startup, cache in `AppCacheManager`.

```csharp
// Replace:
string connectInfo = appDataSourceRegister.ConnectionString;

// With:
string connectInfo = SecretResolverBL.ResolveConnectionString(
    appDataSourceRegister.SecretReference);
```

---

### Phase 2 — Move App Definitions to Tenant DB
**Timeline:** 6–8 weeks  
**Risk:** Medium (data migration required, BL changes)

#### 2.1 Schema preparation

Add all platform configuration tables to the tenant DB schema. These tables already exist in Master DB — the schema is known. Add them to the tenant DB creation script.

Tables to move:

| Category | Tables |
|---|---|
| Forms | `AppTransactionUnit`, `AppTransactionField`, `AppTransactionUnitLayout`, `AppForm` |
| Search | `AppSearch`, `AppSearchView`, `AppSearchCriteria`, `AppSearchViewFormula` |
| Workflow | `AppWorkflow`, `AppConditionalAction`, `AppWorkflowTask` |
| Navigation | `AppListMenu`, `AppListMenuItem`, `AppDesktopItem` |
| Security (authorization) | `AppSecurityGroup`, `AppSecuritySysObjGroupUser`, `AppSecuritySysObj` |
| User profile | `AppUserProfile` (tenant-specific user preferences) |
| Dashboard | `AppDashboard`, `AppDashboardWidget` |
| Messaging | `AppMessageTemplate`, `AppMessageNotificationSetting` |
| API | `AppWebApiProvider`, `AppWebApiConfig` |

Tables that **stay** in Master DB (identity layer):

| Table | Reason |
|---|---|
| `AppSecurityUser` | Global identity — user can belong to multiple companies |
| `AppBusinessPartnerInviteUser` | Cross-company access links |

#### 2.2 Dual-write migration (safe, reversible)

For all new writes on configuration tables, write to **both** Master DB and tenant DB. Reads still come from Master DB. Enables rollback at any point.

```
Write form definition
  → INSERT into Master DB (existing, unchanged)
  → INSERT into Tenant DB (new, additive)
```

#### 2.3 Migrate existing tenant data

For each registered company, copy their configuration data from Master DB to their tenant DB:

```sql
-- Example for AppForm
INSERT INTO [TenantA_DB].[dbo].[AppTransactionUnit]
SELECT * FROM [MasterDB].[dbo].[AppTransactionUnit]
WHERE AppCreatedByCompanyId = 42
```

Run per-tenant, verify row counts, spot-check data integrity.

#### 2.4 Switch reads to tenant DB

Update BL classes to read configuration from tenant DB connection instead of Master DB. Remove `AppCreatedByCompanyId` filter from moved tables.

```csharp
// Before: reads from Master DB with company filter
using (var adapter = new DataAccessAdapter())  // master DB
{
    filter.Add(AppTransactionUnitFields.AppCreatedByCompanyId == companyId);
    adapter.FetchEntityCollection(list, filter);
}

// After: reads from tenant DB — no filter needed
using (var adapter = new DataAccessAdapter(
    tenantConnString, tenantDbName))  // tenant DB
{
    adapter.FetchEntityCollection(list, filter);  // no company filter
}
```

#### 2.5 Stop dual-write, remove from Master DB

After verification period (2–4 weeks parallel run):
1. Disable dual-write
2. Archive (do not immediately drop) configuration tables from Master DB
3. Remove `AppCreatedByCompanyId` columns from moved tables in tenant DBs (optional cleanup)

---

### Phase 3 — Custom URL per Tenant
**Timeline:** 3–4 weeks  
**Risk:** Low (infrastructure + middleware only, no data changes)

#### 3.1 Add `CustomDomain` to `AppDataSourceRegister`

```sql
ALTER TABLE AppDataSourceRegister
ADD CustomDomain NVARCHAR(255) NULL,
    DomainToken  NVARCHAR(100) NULL;
```

Populate `DomainToken` from existing `AppCompany.CompanyDomainIdentityToken`.

#### 3.2 Tenant resolution middleware

Add middleware that runs before session validation. Reads the `Host` header and resolves tenant from URL.

**Subdomain resolution:**
```csharp
// Host: companyA.appai.com → token = "companyA"
string token = ExtractSubdomain(host, platformDomain);
int? companyId = AppCompanyBL.GetCompanyIdFromDomainToken(token);
```

**Custom domain resolution:**
```csharp
// Host: app.companya.com
int? companyId = AppDataSourceRegisterBL
    .GetCompanyIdFromCustomDomain(host);
```

Store resolved `companyId` in `HttpContext.Current.Items` before session check. Session check cross-validates: URL tenant must match session tenant.

#### 3.3 Nginx / reverse proxy configuration

**Wildcard subdomain:**
```nginx
server {
    listen 443 ssl;
    server_name *.appai.com;
    ssl_certificate /certs/wildcard-appai.crt;
    ssl_certificate_key /certs/wildcard-appai.key;

    location / {
        proxy_pass http://localhost/appai;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

**Custom domain (Let's Encrypt):**
```nginx
server {
    listen 443 ssl;
    server_name app.companya.com;
    ssl_certificate /etc/letsencrypt/live/app.companya.com/fullchain.pem;

    location / {
        proxy_pass http://localhost/appai;
        proxy_set_header Host $host;
    }
}
```

Auto-provision certificates using Certbot ACME when a tenant registers a custom domain.

#### 3.4 React frontend — tenant-aware login page

On load, the React app calls `/api/tenant/info` (unauthenticated endpoint). The server resolves tenant from the Host header and returns branding config:

```json
{
  "companyName": "Company A",
  "logoUrl": "/tenant/42/logo.png",
  "primaryColor": "#003399",
  "ssoEnabled": false,
  "loginMessage": "Welcome to Company A Portal"
}
```

Login page renders with tenant branding before authentication.

---

### Phase 4 — Tenant Provisioning Automation
**Timeline:** 2–3 weeks  
**Risk:** Low

#### 4.1 Provisioning service

Single API call creates a fully operational tenant:

```
POST /api/admin/tenants
{
  "companyName": "New Corp",
  "domainToken": "newcorp",
  "adminEmail": "admin@newcorp.com",
  "templateId": "default-erp"
}

Steps executed:
  1. INSERT INTO master.AppCompany → CompanyId = 55
  2. CREATE DATABASE AppAI_Tenant55 on configured SQL Server
  3. Run all schema migrations against Tenant55
  4. Copy template definitions from TemplateLibrary → Tenant55
  5. Create admin user account in master.AppSecurityUser
  6. Store connection string in Key Vault as "tenant-55-db"
  7. INSERT INTO master.AppDataSourceRegister
       { CompanyId=55, SecretRef="tenant-55-db", DomainToken="newcorp" }
  8. Return { companyId: 55, loginUrl: "newcorp.appai.com" }
```

#### 4.2 Schema migration runner

At every deployment, run pending migrations against all tenant databases:

```csharp
var tenants = AppDataSourceRegisterBL.GetAllTenants();
foreach (var tenant in tenants)
{
    var connStr = SecretResolverBL.Resolve(tenant.SecretReference);
    MigrationRunner.RunPendingMigrations(connStr);
}
```

Use FluentMigrator or Evolve for migration management. Each migration script is versioned and idempotent.

---

### Phase 5 — Async-Safe Tenant Context & Connection Pooling
**Timeline:** 3–4 weeks (as part of any .NET Core migration)  
**Risk:** Medium (requires framework changes)

#### 5.1 Replace `HttpContext.Current` with `IHttpContextAccessor`

Applicable when migrating to ASP.NET Core. The `AppClientIdentity` carrier moves from `HttpContext.Current.Items` (thread-static, not async-safe) to `IHttpContextAccessor` injected via DI, which is `AsyncLocal`-based and safe across `await` boundaries.

#### 5.2 Explicit connection pool per tenant

Register a `DbConnectionFactory` in DI that maintains one pool per tenant:

```csharp
services.AddSingleton<ITenantConnectionFactory, TenantConnectionFactory>();

// TenantConnectionFactory maintains:
// ConcurrentDictionary<companyId, SqlConnectionPool>
// with configurable max pool size per tenant
// and health check / eviction on connection failure
```

#### 5.3 Master DB high availability

Move Master DB to Always On Availability Group or Azure SQL with read replicas. The authentication and routing path (the most critical path) gets HA treatment separately from any individual tenant's DB.

---

## 5. Summary Table

| Phase | Focus | Timeline | Risk | Priority |
|---|---|---|---|---|
| 1 | Fix missing company filters + encrypt connection strings | 2–3 weeks | Low | **IMMEDIATE** |
| 2 | Move app definitions + security/user authorization to tenant DB | 6–8 weeks | Medium | High |
| 3 | Custom URL per tenant (subdomain + custom domain + branded login) | 3–4 weeks | Low | High |
| 4 | Tenant provisioning automation | 2–3 weeks | Low | Medium |
| 5 | Async-safe context + connection pooling | 3–4 weeks | Medium | With .NET migration |

**Total:** ~16–22 weeks for Phases 1–4 with 1–2 developers.

### User Management Responsibility After All Phases

| Concern | Managed by | Where |
|---|---|---|
| Login credentials, password reset | User self-service | Master DB |
| Which companies a user can access | Company admin (invite flow) | Master DB |
| Security groups and roles | **Company admin — fully self-service** | Tenant DB |
| Role assignments (user → group) | **Company admin — fully self-service** | Tenant DB |
| Field/transaction permissions | **Company admin — fully self-service** | Tenant DB |
| User profile / preferences | User self-service | Tenant DB |

---

## 6. Verification Checklist

After each phase, verify:

### Phase 1
- [ ] Integration test: Company A user cannot see Company B's form definitions
- [ ] Integration test: Company A user cannot see Company B's search configurations  
- [ ] No plaintext connection strings in `AppDataSourceRegister` table
- [ ] Connection string resolution from secret store works and is cached

### Phase 2
- [ ] All tenant config data exists in tenant DB before cutover
- [ ] Row counts match between Master DB (filtered) and tenant DB
- [ ] BL queries for config use tenant DB connection
- [ ] No regression in form builder, search editor, workflow editor
- [ ] New form created by Tenant A is not visible to Tenant B
- [ ] Company admin can create/edit security groups in tenant DB
- [ ] Company admin can assign users to roles in tenant DB
- [ ] Removing user from `AppBusinessPartnerInviteUser` revokes tenant access
- [ ] User removed from one tenant retains access to other tenants they belong to

### Phase 3
- [ ] `companyA.appai.com` resolves to Company A login page with correct branding
- [ ] `companyB.appai.com` resolves to Company B login page
- [ ] Custom domain `app.companya.com` routes to Company A
- [ ] User authenticated on Company A's URL cannot access Company B data
- [ ] SSL certificates valid for all tenant domains

### Phase 4
- [ ] New tenant provisioned via API in under 60 seconds
- [ ] Tenant DB created with full schema
- [ ] Template data copied correctly
- [ ] Schema migrations run on all tenant DBs at deployment
- [ ] No tenant DB schema drift after 3 deployment cycles

---

## 7. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Data migration error in Phase 2 (wrong company filter) | Medium | High | Dual-write period + row-count verification before cutover |
| Missed query in Phase 1 audit (filter still missing) | Medium | High | Automated test: create two tenants, assert isolation |
| User role data migrated with wrong UserId mapping | Medium | High | Pre-migration: verify UserId exists in master DB for every row in `AppSecuritySysObjGroupUser` |
| Cross-company access breaks after Phase 2 (partner users) | Medium | High | Integration test: invite user from Company B into Company A, verify access survives migration |
| Tenant DB schema drift across versions | Medium | Medium | Migration runner in CI/CD, fails deployment if any tenant fails migration |
| Key Vault unavailable at startup | Low | High | Cache connection strings in encrypted local file as fallback |
| Custom domain SSL auto-provision failure | Low | Medium | Manual fallback, monitoring alert on cert expiry |

---

*Document saved: `Document/Design/MultiTenantImprovementPlan.md`*  
*Last updated: 2026-05-08 — added user management model, user type table, per-tenant user flows, admin self-service boundary, cross-company invite flow, and updated Phase 2 scope and risk table.*
