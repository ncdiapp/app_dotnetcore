# Multi-Tenant Architecture — AppAI / AppBuilder

**Last updated:** 2026-05-15  
**Status:** Phases 1–4 implemented + DB routing hardening + settings split + AppSetupBL removed + SysAdmin identity/authorization hardening + Company & Security SysAdmin redesign + Tenant login bug-fixes + DataSourceFrom routing + session-restore pre-identity guard (2026-05-15)

---

## 1. Overview

AppAI is a low-code/no-code platform that serves multiple independent companies (tenants) from a single application deployment. Each tenant has:

- Its own isolated SQL Server database containing all app definitions and business data
- A unique subdomain (`acme.appai.com`) or custom domain (`app.acme.com`)
- A branded login page showing the company name
- Full self-service administration of security groups, roles, and field permissions

The master database (`AppMasterDB`) handles identity, authentication, tenant registry, and installation-wide settings only. No tenant business data lives in the master DB.

---

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Internet / Browser                           │
└──────────────────┬────────────────────────────┬────────────────────┘
                   │ acme.appai.com              │ app.customdomain.com
                   ▼                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           nginx                                     │
│  *.appai.com  ──►  wildcard TLS cert                                │
│  custom domains ──► per-domain Let's Encrypt cert                   │
│                                                                     │
│  Sets X-Forwarded-Host: <original host>                             │
│  Proxies /appai/ ──► ASP.NET (IIS)                                  │
│  Proxies /       ──► React SPA (dev: port 3000)                     │
└─────────────────────────────┬───────────────────────────────────────┘
                               │
                   ┌───────────▼───────────┐
                   │   React SPA (client)   │
                   │  routes.tsx            │
                   │  ├─ GET /Tenant/Info   │  (on every page load)
                   │  ├─ session restore    │
                   │  └─ auth guard         │
                   └───────────┬───────────┘
                               │ /appai/webapi/*
                   ┌───────────▼────────────────────────────────────┐
                   │            ASP.NET Web API 2  (IIS)             │
                   │                                                  │
                   │  ┌─────────────────────────────────────────┐   │
                   │  │  SecureBaseWebApiController.Initialize() │   │
                   │  │  ① read sessionId cookie                 │   │
                   │  │  ② validate in AppMasterDB               │   │
                   │  │  ③ populate ServerContext.Instance        │   │
                   │  └─────────────────────────────────────────┘   │
                   │                                                  │
                   │  Controllers: Administration, AppTransaction,    │
                   │  AppSearch, DynamicLayout, Dashboard,            │
                   │  ProjectWorkFlow, Integration, ...               │
                   │                                                  │
                   │  Public (no session): Home, Tenant               │
                   │  SysAdmin only:       TenantProvisioning         │
                   └────────────┬─────────────────┬──────────────────┘
                                │                  │
              ┌─────────────────▼──┐   ┌───────────▼────────────────┐
              │   AppMasterDB       │   │   Tenant DB (per company)  │
              │  (28 tables+§3.1)   │   │   TenantDB_{CompanyCode}   │
              │  AppCompany         │   │                            │
              │  AppSecurityUser    │   │  AppForm                   │
              │  AppSecuritySession │   │  AppSearch / SearchView    │
              │  AppDataSourceReg.  │   │  AppListMenu               │
              │  AppSystemSetting*  │   │  AppReport                 │
              │  AppBusinessPartner │   │  AppDesktop                │
              │  AppSecurityRegDom. │   │  AppSecurityGroup          │
              │  AppCountry/Region  │   │  AppTransaction            │
              │  AppAISkill/Ref     │   │  AppTenantSetting          │
              │  AppLogTrack (+2)   │   │  AppDataSet                │
              └─────────────────────┘   │  AppLanguage/Key           │
                                        │  AppSysLabelLanguage       │
                                        │  Business data tables      │
                                        │  _SchemaMigrations         │
                                        └────────────────────────────┘
```

> `* AppSystemSetting` is in AppMasterDB (added by V003 migration); absent from `MasterDB_Cleanup.sql` which predates V003.

---

## 3. Database Architecture

### 3.0 Database Naming Convention

| Database | Name Pattern | Example | Purpose |
|----------|-------------|---------|---------|
| Master DB | `AppMasterDB` | `AppMasterDB` | Shared by all SaaS tenants — identity, auth, registry, platform settings |
| Template DB | `AppTenantTemplateDB` | `AppTenantTemplateDB` | Canonical schema source; used to generate migration scripts and baseline SQL for provisioning |
| Tenant DB | `TenantDB_{CompanyCode}` | `TenantDB_ACME`, `TenantDB_HVAC` | One per live tenant; `CompanyCode` = `AppCompany.CompanyDomainIdentityToken` (short, uppercase) |

`AppTenantTemplateDB` is the development tenant and schema source of truth. It is registered in `AppDataSourceRegister` so developers can log in and build/test features against it. When a feature is ready to ship, export the changed schema from `AppTenantTemplateDB` via SSMS → Generate Scripts, save as `V<NNN>__<description>.sql` in `PlmApplication/Migrations/`, and the migration runner applies it to all live tenant DBs.

### 3.1 Master DB (`AppMasterDB`)

**Purpose:** Identity, authentication, tenant registry, and installation-wide settings. No tenant business data.

The authoritative keep-list is `Document/Design/MasterDB_Cleanup.sql` (28 tables). `AppSystemSetting` is additionally present — added by V003 migration after that script was written.

**Tenant registry**

| Table | Role |
|-------|------|
| `AppCompany` | One row per tenant company. Holds `CompanyDomainIdentityToken`. |
| `AppDataSourceRegister` | Tenant DB registry. Encrypted connection string, `DomainToken`, `CustomDomain`. `AppMasterDB` itself is never stored here — read from `web.config AppCompanyRegistrationConnectionString`. |

**Authentication & security**

| Table | Role |
|-------|------|
| `AppSecurityUser` | All user accounts across all tenants. Login credentials stored here. |
| `AppSecurityUserSession` | Active session tokens. Session validation always hits master. |
| `AppSecurityRegDomain` | Domain / OAuth / SSO registration. |
| `AppSecurityRegDomainORG` | Org-level domain groupings. |
| `AppSecurityLoginAuditor` | Login attempt audit trail. |
| `AppSecurityAuthticationInfo` | Additional auth info (MFA, etc.). |
| `AppSecurityUserContact` | User contact details (email, phone). |
| `AppSecurityUserInvitation` | Pending user invitations. |

**Business partners**

| Table | Role |
|-------|------|
| `AppBusinessPartner` | Cross-company partner relationships. |
| `AppBusinessPartnerInviteUser` | Partner invitation — inviting user record. |
| `AppBusinessPartnerInviteUserChildUser` | Partner invitation — child user mapping. |

**Company configuration**

| Table | Role |
|-------|------|
| `AppCompanyOrderModule` | Modules ordered / licensed per company. |
| `AppCompanyUserTypeRegister` | User type definitions per company. |

**Platform lookups**

| Table | Role |
|-------|------|
| `AppTimeZoneAbbreviation` | Timezone abbreviation dictionary. |
| `AppCountry`, `AppCountryRegion` | Country and region reference data. |
| `AppCurrency` | Currency reference data. |
| `AppLanguage`, `AppLanguageKey`, `AppSysLabelLanguage` | Language definitions and translation keys for SysAdmin. Added by `MasterDB_AddLanguageTables.sql`. Also exist in every tenant DB — accessed via `GetContextAdapter()` which picks the right catalog automatically. |

**Setup & modules**

| Table | Role |
|-------|------|
| `AppSetup` | Platform installation setup records. |
| `AppModuleLibRegister` | Registered module libraries. |

**AI**

| Table | Role |
|-------|------|
| `AppAISkill`, `AppAISkillRef` | Platform-wide AI skill definitions and references. |
| `AppBuilderAgentSession` | AI agent session tracking. |

**Audit & operations**

| Table | Role |
|-------|------|
| `AppLogTrack`, `AppBackupLog`, `AppBatchLog` | Audit trail, backup logs, batch job logs. |

**Settings** *(V003 — absent from `MasterDB_Cleanup.sql` which predates V003)*

| Table | Role |
|-------|------|
| `AppSystemSetting` | Installation-wide settings (timeout, app URL, IIS pool name, etc.). One value per setting key, global to all tenants. |

### 3.2 Tenant DB (`TenantDB_{CompanyCode}`)

**Purpose:** All app definitions and business data for one tenant. Never shares rows with another tenant — isolation is enforced at the DB connection level.

| Table group | Tables |
|-------------|--------|
| App definitions | `AppForm`, `AppSearch`, `AppSearchView`, `AppListMenu`, `AppReport`, `AppDesktop`, `AppDesktopItem`, `AppDataSet`, `AppTransaction` |
| Language / i18n | `AppLanguage`, `AppLanguageKey`, `AppSysLabelLanguage` — per-tenant language definitions and translations |
| Security (tenant scope) | `AppSecurityGroup`, `AppSecurityGroupMember`, `AppSecurityEntityAction` |
| Tenant settings | `AppTenantSetting` — per-company configuration (SMTP, themes, entity mappings, Stripe keys, etc.) |
| Calendar / Project | `AppCalendar`, `AppProjectWorkFlow`, `AppProjectWorkFlowTask` |
| Business data | Customer-specific tables provisioned by the app builder |
| Migration tracking | `_SchemaMigrations` |

> **Critical boundary:** `AppSecurityGroup` and `AppSecurityGroupMember` (access control) live in the **tenant DB** only. `AppSecurityUser` (identity) lives in the **master DB** only. Never prefetch `AppSecurityGroupMember` through a master-DB-backed `DataAccessAdapter` — it will throw `Invalid object name 'AppMasterDB.dbo.AppSecurityGroupMember'`. Fetch group membership separately via `AppSecurityGroupBL` (which uses `AppTenantAdapterBL.GetTenantAdapter()`).

### 3.3 `AppDataSourceRegister` — Required Flags

When registering a tenant's primary database in `AppDataSourceRegister`, the `IsCompanyMasterDb` flag **must** be set to `true`. The session identity pipeline (`AppSaasUserSessionMgtBL`) queries:

```sql
SELECT * FROM AppDataSourceRegister
WHERE DataSourceOwnerCompanyId = @companyId
  AND IsCompanyMasterDb = 1
```

If this flag is missing, every tenant login fails with `"Cannot find User Master DB"`. `AppTenantProvisioningBL.RegisterTenantDataSource()` sets this flag automatically. Manually inserted rows must also set it.

A tenant admin may add additional data-source registrations (external DBs, exchange DBs) with `IsCompanyMasterDb = false`. Those are secondary sources and are never used for identity routing.

### 3.5 `DataSourceFrom` — Logical Data Source Reference

Several tenant tables (`AppEntityInfo`, `AppTransaction`, `AppDataSet`) have a `DataSourceFrom INT` column that stores an `AppDataSourceRegister.DataSourceId`. This tells the runtime which DB to execute raw SQL against when serving lookup items or search results.

**`AppDataSourceRegister` lives only in `AppMasterDB`** — it is the authoritative registry. Tenant DBs do *not* maintain a local copy. The FK constraints that originally enforced this reference in tenant DBs were dropped by V004 migration; `DataSourceFrom` is now a logical reference resolved at runtime.

**Runtime resolution — three-level fallback** (`AppEntityInfoBL.GetSystemTableNoQueryLookupItem`):

```
1. GetOneDatabaseFixture(entityInfo.DataSourceFrom)
   └─ table found in that fixture's schema cache?  → use it  ✓

2. GetOneDatabaseFixture(ServerContext.DataSourceId)   ← current identity's registered DB
   └─ table found there?  → use it  ✓
   (handles stale DataSourceFrom after template copy — custom tables like HvacService)

3. GetMasterDbFixture()                                ← AppMasterDBConnectionString
   (handles platform-only tables like AppSecurityUser)
```

`AppCacheManagerBL.GetMasterDbFixture()` creates and caches a `DatabaseFixture` for `AppMasterDBConnectionString` (1-hour TTL, same as schema cache).

**Stale `DataSourceFrom` after template seed** — when a new tenant DB is seeded from `AppTenantTemplateDB`, all `DataSourceFrom` columns are copied verbatim with the template's `DataSourceId` (e.g. 51). Provisioning Step 9 repairs this automatically. For tenants provisioned before this fix, run manually against the tenant DB:

```sql
DECLARE @oldId INT = 51;   -- template's DataSourceId
DECLARE @newId INT = 54;   -- tenant's DataSourceId (from AppMasterDB.AppDataSourceRegister)

UPDATE [dbo].[AppEntityInfo]  SET DataSourceFrom = @newId WHERE DataSourceFrom = @oldId;
UPDATE [dbo].[AppTransaction] SET DataSourceFrom = @newId WHERE DataSourceFrom = @oldId;
UPDATE [dbo].[AppDataSet]     SET DataSourceFrom = @newId WHERE DataSourceFrom = @oldId;
```

### 3.6 Connection String Security

Connection strings in `AppDataSourceRegister.ConnectionString` are encrypted with AES-256 before storage.

```
Plaintext: Data Source=localhost;Initial Catalog=TenantDB_ACME;...
Stored:    AES:base64encodedciphertext==
```

The `AES:` prefix allows backward-compatible decryption. Implemented in `AppConnectionStringEncryptionBL`.

---

## 4. Request Lifecycle

### 4.1 Authenticated Request

```
Browser
  │
  │  GET /appai/webapi/AppSearch/RetrieveAllSearchEntity
  │  Cookie: AppUserSessionId=abc123
  ▼
nginx  (adds X-Forwarded-Host: acme.appai.com)
  ▼
SecureBaseWebApiController.Initialize()
  │
  ├─ AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity("abc123")
  │    Reads AppMasterDB: AppSecurityUserSession WHERE SessionId = "abc123"
  │    Reads AppMasterDB: AppDataSourceRegister WHERE CompanyId = session.CompanyId
  │    → Decrypts connection string
  │    → Populates ServerContext.Instance:
  │         CurrentUserDbConnectionString = "Data Source=...;Initial Catalog=TenantDB_ACME"
  │         CurrentUserDataBaseName       = "TenantDB_ACME"
  │         CurrentCompanyId              = 55
  │         CurrentUid                    = 1001
  │
  ▼
AppSearchConfigBL.RetrieveAllSearchEntity()
  │
  ├─ AppTenantAdapterBL.GetTenantAdapter()
  │    Reads ServerContext.Instance.CurrentUserDbConnectionString
  │    Throws InvalidOperationException if not set — never falls back to master DB
  │    Returns DataAccessAdapter("Data Source=...;Initial Catalog=TenantDB_ACME")
  │
  └─ adapter.FetchEntityCollection(list, filter)
       → SQL executes against TenantDB_ACME only
```

### 4.2 Unauthenticated Tenant Resolution (Phase 3)

```
Browser (at acme.appai.com)
  │
  │  GET /appai/webapi/Tenant/Info
  ▼
nginx  (sets X-Forwarded-Host: acme.appai.com)
  ▼
TenantController.Info()  [extends ApiController — no session required]
  │
  ├─ Read X-Forwarded-Host → "acme.appai.com"
  ├─ AppDataSourceRegisterBL.GetTenantInfoByHost("acme.appai.com")
  │    Extract subdomain: "acme"
  │    Raw SQL on AppMasterDB:
  │      SELECT c.ShortName, ds.DomainToken, ds.CustomDomain
  │      FROM AppDataSourceRegister ds
  │      JOIN AppCompany c ON c.CompanyId = ds.DataSourceOwnerCompanyId
  │      WHERE ds.DomainToken = 'acme'  OR  ds.CustomDomain = 'acme.appai.com'
  │
  └─ Returns: { isFound: true, companyName: "Acme Corp", domainToken: "acme" }

React routes.tsx (useEffect on mount)
  │
  ├─ tenantSvc.GetTenantInfo()  →  GET /webapi/Tenant/Info
  └─ dispatch(setTenantBranding({ isFound: true, companyName: "Acme Corp" }))

Login.tsx
  └─ <h2>Acme Corp</h2>  (when isFound = true)
```

---

## 5. Tenant Resolution — URL Patterns

| URL Pattern | Resolution Method | Example |
|-------------|------------------|---------|
| `<token>.appai.com` | `AppDataSourceRegister.DomainToken` | `acme.appai.com` → token `"acme"` |
| Custom domain | `AppDataSourceRegister.CustomDomain` (exact match) | `app.acme.com` |
| `localhost` / IP | Neither matches → `isFound: false` (login still works, no branding) | Local development |

**DB columns involved:**

```sql
-- AppDataSourceRegister (AppMasterDB)
DomainToken   NVARCHAR(100)  -- lowercase, e.g. "acme"
CustomDomain  NVARCHAR(255)  -- e.g. "app.acme.com"

-- AppCompany (AppMasterDB) — source of truth for DomainToken
CompanyDomainIdentityToken  NVARCHAR(500)
```

---

## 6. Settings Architecture

Settings are split into two tiers with separate tables, enums, and BL classes.

### 6.1 Tiers

| Tier | Table | Enum | BL Class | Scope | DB |
|------|-------|------|----------|-------|----|
| System | `AppSystemSetting` | `EmSystemSettings` | `AppSystemSettingBL` | One value for the entire installation | AppMasterDB |
| Tenant | `AppTenantSetting` | `EmTenantSettings` | `AppTenantSettingBL` | One value per company | AppTenantDB |

### 6.2 System Settings (`EmSystemSettings` → `AppSystemSettingBL`)

Loaded once at application startup into a static dictionary. Safe to call before any user request.

```csharp
// Read a system-wide setting
int? timeout = AppSystemSettingBL.GetIntValue(EmSystemSettings.Timeout);
string appUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
AppSystemSettingBL.Reload();  // refresh without restarting
```

| Setting | Code | Description |
|---------|------|-------------|
| `Timeout` | 2 | Session timeout in minutes |
| `TimeoutWarningGracePeriod` | 3 | Warning period before timeout |
| `ApplicationTutorialUrl` | 5 | Help/tutorial URL |
| `ApplicationURL` | 10 | Base URL of the installation |
| `BaseUserDbBackupFilePath` | 210 | Backup file root path |
| `UserDbFileDirectoryPath` | 211 | DB file directory |
| `AppVersion` | 1000 | Platform version string |
| `InternalApiRestEndPoint` | 1003 | Internal plugin API URL |
| `ApplicationPoolName` | 1004 | IIS application pool name |
| `IISWebSiteName` | 1006 | IIS web site name |

### 6.3 Tenant Settings (`EmTenantSettings` → `AppTenantSettingBL`)

Cached per company in a `ConcurrentDictionary<int, Dictionary<string, string>>`. Loaded on first access; call `InvalidateCache(companyId)` after an admin saves settings.

```csharp
// Read a tenant-scoped setting (uses current request's company from ServerContext)
string smtpServer = AppTenantSettingBL.GetStringValue(EmTenantSettings.SmtpServer);
int? employeeEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.EmployeeEntity);
AppTenantSettingBL.InvalidateCache(companyId);  // call after settings saved
```

Key tenant settings include: SMTP configuration, entity ID mappings (Employee, Customer, Supplier), user transaction IDs, calendar search IDs, theme codes, partner transaction mappings, Stripe key, Figma access token, eShop configuration, and folder/file IDs.

### 6.4 Security Rule

```
AppCompanyBL.AppMasterDBConnectionString             →  master DB operations only
                                                         (auth, session, admin, settings load)

ServerContext.Instance.CurrentUserDbConnectionString →  tenant DB operations only
                                                         (all post-login business logic)
```

`AppTenantAdapterBL` exposes two adapter factory methods:

| Method | Use when | Catalog rewrite |
|--------|----------|----------------|
| `GetTenantAdapter()` | Table exists **only** in tenant DBs (e.g. `AppListMenu`, `AppSecurityGroup`, `AppDataSet`) | Always rewrites `AppMasterDB` → `TenantDB_X`. Throws for SysAdmin if tenant context is missing. |
| `GetContextAdapter()` | Table exists in **both** `AppMasterDB` and every tenant DB (e.g. `AppLanguage`, `AppLanguageKey`, `AppSysLabelLanguage`) | Skips rewrite when caller is SysAdmin (`dbName == "AppMasterDB"`). Applies rewrite for tenant users. |

Both methods **throw** `InvalidOperationException` if called before the per-request identity is registered.

**Pre-identity fallback pattern** — Some BL methods are called during login / session-restore before `RegisterUserIdentityTotheSystem` runs (e.g. `AppLanguageBL.RetrieveDefaultAppLanguageEntity`). These must not call `GetContextAdapter()` unconditionally. Pattern:

```csharp
DataAccessAdapter adapter;
try   { adapter = AppTenantAdapterBL.GetContextAdapter(); }
catch (InvalidOperationException)
{
    // Login / session-restore path — identity not yet registered.
    // Fall back to AppMasterDB where the default row is seeded.
    adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString);
}
```

**Why `GetTenantAdapter()` is mandatory — LLBLGen catalog override**

LLBLGen generates SQL with the catalog name baked in from when entity classes were generated (hardcoded as `AppMasterDB`). Passing a different connection string alone does **not** change the catalog in generated SQL — without the override, a query against `TenantDB_ACME` still emits `AppMasterDB.dbo.AppListMenu`, which fails.

`GetTenantAdapter()` applies the fix:

```csharp
var adapter = new DataAccessAdapter(connStr);
adapter.CatalogNameOverwrites.Add("AppMasterDB", dbName);  // e.g. "TenantDB_ACME"
```

**Rule:** Never write `new DataAccessAdapter(ServerContext.Instance.CurrentUserDbConnectionString)` directly in tenant BL files. Always use `AppTenantAdapterBL.GetTenantAdapter()`. The symptom of a missed call is:

```
ORMQueryExecutionException: Invalid object name 'AppMasterDB.dbo.<TableName>'
```

> **Renamed (2026-05-15):** `AppCompanyBL.AppCompanyRegistrationConnectionString` was renamed to `AppCompanyBL.AppMasterDBConnectionString` throughout all 18 BL files and `web.config`. The connection string key in `web.config` is now `name="AppMasterDBConnectionString"`.

---

## 7. Tenant Provisioning (Phase 4)

A single API call creates a fully operational tenant. All steps execute in `AppTenantProvisioningBL.ProvisionNewTenant()`.

```
POST /webapi/TenantProvisioning/Provision  [SysAdmin only]
{
  "companyName": "New Corp",
  "domainToken": "newcorp",
  "adminEmail": "admin@newcorp.com",
  "adminLoginName": "newcorp.admin",
  "adminPassword": "secret"
}

Step 1  Validate
        ├─ Required fields present
        ├─ DomainToken matches ^[a-z0-9\-]{2,50}$
        └─ DomainToken not already in AppDataSourceRegister or AppCompany

Step 2  INSERT INTO AppMasterDB.AppCompany
        → CompanyId = 55, CompanyDomainIdentityToken = "newcorp"

Step 3  Derive database name: "TenantDB_NEWCORP"
        (UPPER(CompanyDomainIdentityToken) → "NEWCORP")

Step 4  CREATE DATABASE [TenantDB_NEWCORP]
        (raw ADO.NET to SQL master catalog)

Step 5  Build tenant connection string:
        SqlConnectionStringBuilder(masterConnStr) { InitialCatalog = "TenantDB_NEWCORP" }

Step 6  AppTenantMigrationRunnerBL.RunPendingMigrations(tenantConnStr)
        ├─ Create _SchemaMigrations tracking table
        ├─ Apply V001__InitialTenantSchema.sql       (full schema)
        ├─ Apply V002__Phase3TenantDomain.sql        (domain columns)
        ├─ Apply V003__SystemAndTenantSettings.sql   (AppTenantSetting table)
        ├─ Apply V004__DropCrossDbForeignKeys.sql    (drop DataSourceFrom FKs)
        └─ Returns: 4 migrations applied

Step 6b (optional) Seed app definitions from template DB
        ├─ Tables: AppForm, AppSearch, AppSearchView, AppListMenu, AppReport,
        │          AppDesktop, AppDesktopItem, AppDataSet, AppEntityInfo,
        │          AppTransaction, AppSecurityGroup, AppSecurityGroupMember,
        │          AppSecurityEntityAction, AppTenantSetting
        └─ Cross-DB INSERT … SELECT via SQL master catalog

Step 7  INSERT INTO AppMasterDB.AppSecurityUser
        LoginName=newcorp.admin, Password=HashPassword("secret"),
        DomainId=6 (SaasCompanyAdmin), AppCreatedByCompanyId=55

Step 8  INSERT INTO AppMasterDB.AppDataSourceRegister
        ConnectionString = AES:encrypt(tenantConnStr)
        DomainToken = "newcorp"
        IsCompanyMasterDb = true          ← REQUIRED (login fails without this)
        → Returns newDataSourceId

Step 9  UPDATE DataSourceFrom references in tenant DB
        ├─ For every table with a DataSourceFrom column in the tenant DB,
        │  UPDATE SET DataSourceFrom = newDataSourceId WHERE DataSourceFrom = templateDataSourceId
        └─ Fixes stale IDs copied verbatim from the template (see §3.5)

Returns: { success: true, companyId: 55, loginUrl: "newcorp.appai.com",
           databaseName: "TenantDB_NEWCORP", migrationsApplied: 4 }
```

Target time: **< 60 seconds** end-to-end.

---

## 8. Schema Migration Runner

Maintains schema consistency across all tenant databases. Safe to call on every deployment.

```
PlmApplication/
└── Migrations/
    ├── V001__InitialTenantSchema.sql        ← full baseline schema
    ├── V002__Phase3TenantDomain.sql         ← Phase 3 domain columns
    ├── V003__SystemAndTenantSettings.sql    ← AppSystemSetting + AppTenantSetting
    └── V004__DropCrossDbForeignKeys.sql     ← drop DataSourceFrom FK constraints

_SchemaMigrations (in each tenant DB)
┌──────┬──────────────────────────────────────┬──────────────────────┐
│  Id  │  Version                             │  AppliedAt           │
├──────┼──────────────────────────────────────┼──────────────────────┤
│   1  │  V001__InitialTenantSchema           │  2026-05-08 10:00:00 │
│   2  │  V002__Phase3TenantDomain            │  2026-05-08 10:00:01 │
│   3  │  V003__SystemAndTenantSettings       │  2026-05-13 ...      │
│   4  │  V004__DropCrossDbForeignKeys        │  2026-05-15 ...      │
└──────┴──────────────────────────────────────┴──────────────────────┘
```

**V003 note:** The AppSystemSetting section (lines 1–55) must be run against **AppMasterDB** manually — the runner only targets tenant DBs. The AppTenantSetting section (lines 57–95) is applied automatically by the runner to each tenant DB.

**V004 note:** Drops `FK_AppEntityInfo_AppDataSourceRegister`, `FK_AppTransaction_AppDataSourceRegister`, and `FK_AppDataSet_AppDataSourceRegister` from every tenant DB. `AppDataSourceRegister` is authoritative in `AppMasterDB` only — see §3.5.

**Rules:**
- Scripts named `V<NNN>__<description>.sql`, applied in lexicographic order
- Each script applied exactly once — tracked in `_SchemaMigrations`
- `GO` statements are split into separate batches before execution
- `RunMigrationsOnAllTenants()` — called at deployment via `POST /webapi/TenantProvisioning/RunMigrations`

---

## 9. APP.BL Folder Structure

BL classes are organized into scope-based folders that enforce the security boundary visually.

```
APP.BL/
├── MasterAuth/          ← authenticate against AppMasterDB only
│   ├── AppSaasAccountUserBL.cs
│   ├── AppSecurityAuthenticationBL.cs
│   ├── AppSecurityManagementBL.cs
│   ├── AppSecurityPasswordHashBL.cs
│   ├── AppSecurityUserBL.cs
│   └── AppSecurityUserSessionBL.cs
│
├── MasterAdmin/         ← administer AppMasterDB (SysAdmin only)
│   ├── AppCompanyBL.cs
│   ├── AppDataSourceRegisterBL.cs
│   ├── AppSaasUserSessionMgtBL.cs
│   ├── AppSystemSettingBL.cs        ← reads AppMasterDB.AppSystemSetting
│   └── AppTenantProvisioningBL.cs
│
├── Infrastructure/      ← cross-cutting, startup, caching
│   ├── AppCacheManager.cs
│   ├── AppTimeZoneBL.cs            ← timezone dictionaries + PrepareOffsetToken
│   └── AppTenantAdapterBL.cs       ← GetTenantAdapter() — throws if no context
│
└── TenantBusiness/      ← all post-login tenant operations
    ├── AppTenantSettingBL.cs        ← reads AppTenantDB.AppTenantSetting
    ├── AppSearchBL.cs
    ├── AppTransactionBL.cs
    └── ... (90+ files)
```

**Rule:** Files in `MasterAuth/` and `MasterAdmin/` use `AppCompanyBL.AppMasterDBConnectionString`. Files in `TenantBusiness/` use `ServerContext.Instance.CurrentUserDbConnectionString` via `AppTenantAdapterBL.GetTenantAdapter()`.

---

## 10. Nginx Configuration

Two server block patterns are maintained in `Document/Design/`:

### Wildcard Subdomain (`nginx-wildcard-subdomain.conf`)

```nginx
server {
    listen 443 ssl;
    server_name *.appai.com;
    ssl_certificate /etc/letsencrypt/live/appai.com/fullchain.pem;  # DNS-01 wildcard cert

    proxy_set_header Host $host;                  # passes "acme.appai.com" to backend
    proxy_set_header X-Forwarded-Host $host;

    location /appai/ { proxy_pass http://localhost/appai/; }
    location /        { proxy_pass http://localhost:3000; }
}
```

### Custom Domain (`nginx-custom-domain.conf`)

One server block per custom-domain tenant. Certificates auto-provisioned via Certbot:

```bash
certbot --nginx -d app.acme.com
```

---

## 11. React Application Architecture

```
src/
├── routes.tsx                         ← app entry: loads tenant info + session restore
├── redux/
│   ├── rootReducer.ts                 ← combines all slices
│   └── features/
│       ├── admin/userSessionSlice.ts  ← auth state (login, restore, logout)
│       └── ui/
│           ├── tenantBrandingSlice.ts ← isFound, companyName, domainToken
│           ├── theme/themeSlice.ts    ← light/dark/custom theme
│           ├── navigation/tabnavSlice.ts
│           └── feedback/
└── webapi/
    ├── tenantSvc.ts                   ← GET /webapi/Tenant/Info
    ├── adminsvc.ts                    ← login, session, user menu
    └── endpoints.ts                   ← BASE_URL = "/appai"
```

**Startup sequence:**

```
App mounts
  │
  ├─① tenantSvc.GetTenantInfo()         (parallel — no auth needed)
  │    → dispatch(setTenantBranding)
  │
  └─② sessionId in localStorage?
        ├─ yes → dispatch(restoreSession())
        │         → adminSvc.checkCurrentSessionExists()
        │         → adminSvc.retrieveUserTreeMenu()
        └─ no  → render Login page  ← tenantBranding already in Redux
```

---

## 12. Security Model

### Tenant Data Isolation

| Layer | Mechanism |
|-------|-----------|
| DB connection | Each tenant has its own DB. No cross-tenant SQL is possible via the ORM. |
| Session validation | `SecureBaseWebApiController` validates session before every controller method and sets `ServerContext` to the authenticated user's company. |
| Tenant adapter | `AppTenantAdapterBL.GetTenantAdapter()` reads `ServerContext.CurrentUserDbConnectionString` and **throws** if it is not set — never falls back to master DB. |
| Settings isolation | `AppTenantSettingBL` reads from the current tenant's `AppTenantSetting` table; `AppSystemSettingBL` reads from `AppMasterDB.AppSystemSetting`. Crossing tiers is a compile error (different enum types). |

### User Identity vs Access Control — Two-DB Split

User data is deliberately split across two databases:

| Data | DB | Adapter | Tables |
|------|----|---------|--------|
| Identity (who the user is) | `AppMasterDB` | `MasterConnStr` | `AppSecurityUser`, `AppSecurityRegDomain`, `AppSecurityUserContact`, `AppComOrganization_` |
| Access control (what the user can do) | `TenantDB_{CODE}` | `AppTenantAdapterBL.GetTenantAdapter()` | `AppSecurityGroup`, `AppSecurityGroupMember`, `AppSecurityEntityAction` |

**Consequence for BL code:**

- `AppSecurityUserBL` (in `MasterAuth/`) — fetches user identity from master DB via `MasterConnStr`. Must **not** include `PrefetchPathAppSecurityGroupMember` in its prefetch paths.
- `AppSecurityGroupBL` (in `TenantBusiness/`) — fetches group membership from tenant DB via `GetTenantAdapter()`. Must **not** query `AppSecurityUser` from master DB directly.

To get a user's group memberships, call `AppSecurityGroupBL` after loading the user:

```csharp
// Step 1 — user identity from master
AppSecurityUserEntity user = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId);

// Step 2 — group membership from tenant DB
using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
{
    var members = new EntityCollection<AppSecurityGroupMemberEntity>();
    adapter.FetchEntityCollection(members,
        new RelationPredicateBucket(AppSecurityGroupMemberFields.UserId == userId));
}
```

### Authentication Flow

```
Login: POST /webapi/Home/MgtLogin
  ├─ Verify credentials against AppMasterDB.AppSecurityUser
  ├─ Lookup AppDataSourceRegister by CompanyId
  ├─ Decrypt connection string
  ├─ Create AppSecurityUserSession (SessionId = GUID)
  └─ Set cookie: AppUserSessionId=<guid>

Subsequent requests:
  ├─ Read cookie → SessionId
  ├─ Lookup AppSecurityUserSession in AppMasterDB
  ├─ Decrypt + cache tenant connection string
  └─ Populate ServerContext.Instance for this HTTP request
```

### Authorization Levels

| User Type | `DomainId` | Can access |
|-----------|-----------|------------|
| SysAdmin | 1 | All tenants; provisioning APIs; any AppCompany read/write; master DB directly |
| SaasCompanyAdmin | 6 | Own tenant only; own AppCompany record only; own users only |
| Employee | 2 | Own tenant; permissions via AppSecurityGroup |
| Customer | 3 | Own tenant; permissions via AppSecurityGroup; invited by tenant |
| Supplier | 4 | Own tenant; permissions via AppSecurityGroup; invited by tenant |
| ClientAgent | 5 | Own tenant; acts on behalf of a customer |
| SupplierAgent | 9 | Own tenant; acts on behalf of a supplier |
| Unknown | 7 | Invited but unclassified; pending domain assignment |
| AllUser | 8 | Query filter token only — not a real login domain |
| CompanyWinScheduleUser | 88 | Automated scheduled jobs in company context; no interactive login |
| Integration | 98 | Machine-to-machine API access; external system integrations |
| CompanyAnonymousUser | 99 | Public-facing anonymous access within a tenant context |
| Anonymous | — | `/webapi/Tenant/Info`, `/webapi/Home/Login` only |

### Domain Type Registry (`EmAppUserType`)

`DomainId` on `AppSecurityUser` maps to `EmAppUserType` (defined in `APP.Components.Dto/AppEnums.cs`). Each value is a row in `AppMasterDB.AppSecurityRegDomain`.

---

#### 1. SysAdmin (`DomainId = 1`)

Platform operator. The only domain that bypasses tenant context entirely.

**Identity registration fast-path** (see §12 — SysAdmin Identity Registration):

```
CurrentUserDbConnectionString = AppMasterDBConnectionString
CurrentWorkingCompanyId       = null   ← no tenant context
CompanySettings               = null   ← must null-guard all callers
```

**Responsibilities**

| # | Responsibility | Mechanism |
|---|---------------|-----------|
| 1.1 | **Manage all registered users** — view, create, edit, and deactivate user accounts across every tenant | `AppMasterDB.AppSecurityUser`; `AppSecurityUserBL.RetrieveAppSecurityUserDtoByCompanyId(companyId)` scoped per selected company; guarded by `RequireCompanyAccess` |
| 1.2 | **Manage all registered companies** — CRUD on company records, domain tokens, logos, business-partner settings | `AppMasterDB.AppCompany`; `AppCompanyBL`; `AdministrationController.RetrieveOneAppCompanyExDto / SaveOneAppCompanyExDto`; SysAdmin sees all companies in the list |
| 1.3 | **Tenant provisioning** — create new tenant: provision DB, run migrations, seed schema, register data source, create tenant admin user | `AppTenantProvisioningBL.ProvisionNewTenant()`; `POST /webapi/TenantProvisioning/Provision`; full 9-step flow (§7) |
| 1.4 | **System setup** — configure installation-wide settings (timeout, app URL, IIS pool name, backup paths, version) | `AppMasterDB.AppSystemSetting`; `AppSystemSettingBL`; `EmSystemSettings` enum; loaded at startup into a static dictionary; `POST /webapi/Administration/RetrieveAllAppSetupDtoList?isMasterDb=true` |

**Access scope:**
- Reads/writes `AppMasterDB` directly via `AppCompanyBL.AppMasterDBConnectionString`
- Can select any company in `CompanySecuritySetting` and view its users
- Sees **Company Info** and **Users** tabs only (not tenant role/menu/dashboard tabs)
- `IsAdminUser()` returns `true`; `RequireCompanyAccess()` always passes

---

#### 2. Employee (`DomainId = 2`)

Internal staff of a tenant company. Created by a `SaasCompanyAdmin`.

- Provisioned in `AppMasterDB.AppSecurityUser` with `AppCreatedByCompanyId = <tenantCompanyId>`
- Logs into the tenant's subdomain; `CurrentUserDbConnectionString` → `TenantDB_{CODE}`
- Permissions granted via `AppSecurityGroup` / `AppSecurityGroupMember` in the tenant DB
- Standard actor for all tenant business operations (forms, search, transactions, calendar)

---

#### 3. Customer (`DomainId = 3`)

External customer of a tenant. Invited at runtime — no self-registration.

- Account created when another company invites a customer via `AppBusinessPartnerInviteUser`
- Domain assigned at invitation time (not chosen by the user)
- Access restricted to customer-facing features within the inviting tenant
- Permissions scoped via `AppSecurityGroup`

---

#### 4. Supplier (`DomainId = 4`)

External supplier of a tenant. Invited at runtime — no self-registration.

- Mirrors Customer domain: account created via partner invitation
- Domain assigned at invitation time
- Access restricted to supplier-facing features within the inviting tenant
- Permissions scoped via `AppSecurityGroup`

---

#### 5. ClientAgent (`DomainId = 5`)

Agent acting on behalf of a customer within a tenant.

- Proxy identity for automated or delegated client-side operations
- Permissions via `AppSecurityGroup`; acts within the customer's access boundary

---

#### 6. SaasCompanyAdmin (`DomainId = 6`)

Tenant administrator. Self-registered owner/operator of one tenant.

- Created by `AppTenantProvisioningBL` (Step 7) during tenant provisioning, or self-registered
- `CurrentUserDbConnectionString` → `TenantDB_{CODE}`; `CurrentWorkingCompanyId` is set
- `IsAdminUser()` returns `true`; `RequireCompanyAccess()` passes only for own `companyId`

**Responsibilities (own tenant only)**

| Tab / Area | What they manage | Data source |
|------------|-----------------|-------------|
| Company Info | Company name, domain token, logo, business-partner settings | `AppMasterDB.AppCompany` (own row) |
| Users | Tenant users (Employee / Customer / Supplier) | `AppMasterDB.AppSecurityUser WHERE AppCreatedByCompanyId = ownCompanyId` |
| Security Groups | Role definitions and entity-action permissions | Tenant DB: `AppSecurityGroup`, `AppSecurityEntityAction` |
| Menu Groups | Menu role assignments | Tenant DB: `AppListMenu` + security |
| Contact Groups | Contact groupings | Tenant DB |
| Menu By User Group | Domain/user menu assignments | Tenant DB |
| Domain Dashboard | Dashboard management | Tenant DB: `AppDesktop` |
| Application Privileges | Feature access control | Tenant DB |
| Integration Tokens | API tokens for external integrations | Tenant DB |
| Tenant Settings | SMTP, themes, entity mappings, Stripe, Figma, eShop | Tenant DB: `AppTenantSetting` |

---

#### 7. Unknown (`DomainId = 7`)

Invited user without a home company registration.

- Assigned when a user is invited to a tenant but has not completed their own company registration
- Treated as a placeholder domain; access is limited until the domain is clarified or upgraded
- Not a standard operating domain; used transitionally

---

#### 8. AllUser (`DomainId = 8`)

Query filter sentinel — not a login domain.

- Used only as a filter value in BL queries that need to target users of any domain
- No user account should ever have `DomainId = 8` as their real domain
- See commented-out filter in `AdministrationControllerSecurity.cs`: `listUser.Where(o => o.DomainId < (int)EmAppUserType.AllUser)`

---

#### 9. SupplierAgent (`DomainId = 9`)

Agent acting on behalf of a supplier within a tenant.

- Mirrors `ClientAgent` but on the supplier side
- Proxy identity for automated or delegated supplier-side operations
- Permissions via `AppSecurityGroup`

---

#### 88. CompanyWinScheduleUser (`DomainId = 88`)

Background scheduled job identity for a tenant.

- Used by Windows Scheduled Tasks or batch jobs that run in a company context
- No interactive login; session established programmatically
- Operates within the tenant's DB context (`TenantDB_{CODE}`)

---

#### 98. Integration (`DomainId = 98`)

Machine-to-machine API integration identity.

- Used for external systems calling AppAI APIs (webhooks, third-party connectors, ETL pipelines)
- Authenticated via API token rather than username/password session
- Access scoped to the sponsoring tenant's DB

---

#### 99. CompanyAnonymousUser (`DomainId = 99`)

Anonymous user operating within a tenant's public-facing context.

- Represents unauthenticated visitors accessing tenant-scoped public endpoints (e.g. eShop, public forms)
- More privileged than the global Anonymous (which has no company context at all)
- Permissions controlled per tenant via `AppSecurityGroup`

### SysAdmin Identity Registration

SysAdmin (`DomainId = 1`) is a special case throughout the identity pipeline:

```
AppSaasUserSessionMgtBL.RegisterUserIdentityTotheSystem()
  │
  ├─ DomainId == 1?  → SysAdmin fast path
  │    CurrentLoginUserType        = SysAdmin (1)
  │    CurrentUserDbConnectionString = AppMasterDBConnectionString
  │    CurrentWorkingCompanyId     = null         ← no tenant context
  │    → RegisterIdentity() and return
  │
  └─ DomainId != 1?  → tenant user path
       Look up AppDataSourceRegister by CurrentWorkingCompanyId
       CurrentUserDbConnectionString = decrypt(tenantConnStr)
       CurrentWorkingCompanyId       = session.AppCreatedByCompanyId
```

**Consequence:** Any BL method that reads `ServerContext.Instance.CompanySettings` or `ServerContext.Instance.CurrentWorkingCompanyId` will find `null` for SysAdmin. All `MasterAdmin/` BL methods use `AppMasterDBConnectionString` directly and must not read `CompanySettings`.

### IsAdminUser()

`AppSecurityUserBL.IsAdminUser()` reads `CurrentLoginUserType` directly from the registered `AppClientIdentity` — it does **not** load `CurrentUserEntity` (which would fail for SysAdmin due to cache misses on tenant-prefetch paths):

```csharp
public static bool IsAdminUser()
{
    var identity = ServerContext.Instance.CurrnetClientIdentity;
    if (identity == null) return false;
    int loginType = identity.CurrentLoginUserType;
    return loginType == (int)EmAppUserType.SysAdmin
        || loginType == (int)EmAppUserType.SaasCompanyAdmin;
}
```

### Company Access Guard

`AdministrationController.RequireCompanyAccess(int companyId)` enforces:

| Caller | Rule |
|--------|------|
| SysAdmin | Always allowed (any companyId) |
| SaasCompanyAdmin | Only when `CurrentWorkingCompanyId == companyId` |
| Others | 403 Forbidden |

Applied on `RetrieveOneAppCompanyExDto`, `SaveOneAppCompanyExDto`, and `RetrieveAppSecurityUserDtoByCompanyId`.

---

## 13. Company & Security — SysAdmin vs Tenant Admin

The `CompanySecuritySetting` page is shared between SysAdmin and SaasCompanyAdmin but shows different tabs for each role.

### SysAdmin view

SysAdmin manages platform infrastructure, not tenant-level roles/menus/dashboards.

| Tab | Content | Data source |
|-----|---------|-------------|
| Company Info | Edit company name, code, domain token, logo, business partner settings | `AppMasterDB.AppCompany` |
| Users | All login accounts for the selected company | `AppMasterDB.AppSecurityUser WHERE AppCreatedByCompanyId = companyId` |

SysAdmin sees **all companies** in the company list (host + every provisioned tenant). Selecting a company loads that company's info and users.

### Tenant admin (SaasCompanyAdmin) view

| Tab | Content | Data source |
|-----|---------|-------------|
| Company Info | Edit own company info | `AppMasterDB.AppCompany` (own row only) |
| Users | Tenant users (Employee / Customer / Supplier / etc.) | Tenant DB user management |
| Security Groups | Role definitions | Tenant DB |
| Menu Groups | Menu role assignments | Tenant DB |
| Contact Groups | Contact groupings | Tenant DB |
| Menu By User Group | Domain/user menu assignments | Tenant DB |
| Domain Dashboard | Dashboard management | Tenant DB |
| Application Privileges | Feature access control | Tenant DB |
| Integration Tokens | API tokens | Tenant DB |

Tenant admin can only access their own company (enforced by `RequireCompanyAccess`).

### Host company constant

`AppSystemConstants.HostCompanyId = 1` (in `APP.Components.Dto`) is the single source of truth for the platform host company ID. Referenced by:
- `CompanySettingDto.IsHostCompany` (APP.Framework) — `CompanyId == 1`
- `AppCacheManagerBL.HostCompanyReserveId` (APP.BL) — alias for backward compatibility

---

## 14. Key Source Files

### Backend (.NET)

| Concern | File |
|---------|------|
| Tenant adapter | `APP.BL/Infrastructure/AppTenantAdapterBL.cs` |
| Tenant info lookup | `APP.BL/MasterAdmin/AppDataSourceRegisterBL.cs` → `GetTenantInfoByHost()` |
| Tenant provisioning | `APP.BL/MasterAdmin/AppTenantProvisioningBL.cs` |
| Schema migration runner | `APP.BL/TenantBusiness/AppTenantMigrationRunnerBL.cs` |
| Connection string encryption | `APP.BL/TenantBusiness/AppConnectionStringEncryptionBL.cs` |
| Session validation + SysAdmin identity | `APP.BL/MasterAdmin/AppSaasUserSessionMgtBL.cs` |
| Admin check (IsAdminUser) | `APP.BL/MasterAuth/AppSecurityUserBL.cs` |
| Company CRUD + image helpers | `APP.BL/MasterAdmin/AppCompanyBL.cs` |
| Company-scoped user retrieval | `APP.BL/MasterAuth/AppSecurityUserBL.cs` → `RetrieveAppSecurityUserDtoByCompanyId()` |
| System settings (master DB) | `APP.BL/MasterAdmin/AppSystemSettingBL.cs` |
| Tenant settings (tenant DB) | `APP.BL/TenantBusiness/AppTenantSettingBL.cs` |
| Timezone dictionaries + helpers | `APP.BL/Infrastructure/AppTimeZoneBL.cs` |
| Host company ID constant | `APP.Components.Dto/AppEnums.cs` → `AppSystemConstants.HostCompanyId` |
| Request context carrier | `APP.Framework/ServerContext.cs` |
| Public tenant endpoint | `PlmApplication/Server/WebApi/TenantController.cs` |
| Provisioning endpoint | `PlmApplication/Server/WebApi/TenantProvisioningController.cs` |
| Session validation hook | `PlmApplication/Server/WebApi/BaseWebApiController.cs` |
| Company + user endpoints (guarded) | `PlmApplication/Server/WebApi/AdministrationController.cs` → `RequireCompanyAccess()` |

### Frontend (React/TypeScript)

| Concern | File |
|---------|------|
| App entry / routing | `PlmApplication/AppReact/src/routes.tsx` |
| Tenant branding state | `PlmApplication/AppReact/src/redux/features/ui/tenantBrandingSlice.ts` |
| Tenant API service | `PlmApplication/AppReact/src/webapi/tenantSvc.ts` |
| Login page | `PlmApplication/AppReact/src/components/admin/Login.tsx` |
| Redux store | `PlmApplication/AppReact/src/redux/store.ts` |
| SysAdmin / admin permission helpers | `PlmApplication/AppReact/src/helper/adminPermissionHelper.ts` |
| Sidebar (role-aware menu) | `PlmApplication/AppReact/src/components/mainLayout/Sidebar.tsx` |
| Company & Security (role-aware tabs) | `PlmApplication/AppReact/src/components/admin/CompanySecuritySetting/CompanySecuritySetting.tsx` |
| Tenant admin users tab (SysAdmin only) | `PlmApplication/AppReact/src/components/admin/CompanySecuritySetting/TenantAdminUsersTab.tsx` |
| Tenant provisioning page | `PlmApplication/AppReact/src/components/admin/TenantProvisioning.tsx` |

### Database & Infrastructure

| Concern | File |
|---------|------|
| System + tenant settings tables | `PlmApplication/Migrations/V003__SystemAndTenantSettings.sql` |
| Drop cross-DB DataSourceFrom FKs | `PlmApplication/Migrations/V004__DropCrossDbForeignKeys.sql` |
| Baseline tenant schema | `PlmApplication/Migrations/V001__InitialTenantSchema.sql` |
| Domain columns (Phase 3) | `PlmApplication/Migrations/V002__Phase3TenantDomain.sql` |
| DB migration script (Phase 3) | `Document/Design/Phase3_AddTenantDomain.sql` |
| AppMasterDB cleanup | `Document/Design/MasterDB_Cleanup.sql` |
| Nginx wildcard subdomain | `Document/Design/nginx-wildcard-subdomain.conf` |
| Nginx custom domain | `Document/Design/nginx-custom-domain.conf` |

---

## 14. Phase Roadmap Summary

| Phase | What changed | Status |
|-------|-------------|--------|
| 1 | Added `AppCreatedByCompanyId` filters to all app-definition queries; AES-256 connection string encryption | ✅ Done |
| 2 | Created `AppTenantAdapterBL`; moved all BL reads/writes to tenant DB; removed Phase 1 company filters (isolation now from DB connection) | ✅ Done |
| 3 | `DomainToken` + `CustomDomain` on `AppDataSourceRegister`; `TenantController`; React tenant branding on login | ✅ Done |
| 4 | `AppTenantProvisioningBL` (8-step provisioning); `AppTenantMigrationRunnerBL`; `TenantProvisioningController` | ✅ Done |
| DB routing hardening | Corrected all 101 APP.BL files to use master vs tenant connection strings correctly; `GetTenantAdapter()` throws instead of falling back; reorganized `AppMgr/` into `MasterAuth/`, `MasterAdmin/`, `Infrastructure/`, `TenantBusiness/` folders | ✅ Done 2026-05-13 |
| Settings split | Split `EmApplicationSettings` → `EmSystemSettings` + `EmTenantSettings`; new `AppSystemSetting` (master DB) and `AppTenantSetting` (tenant DB) tables; new `AppSystemSettingBL` and `AppTenantSettingBL`; migrated 33 caller files; removed dropped WeChat feature | ✅ Done 2026-05-13 |
| AppSetupBL removal | Deleted `AppSetupBL.cs` and `AppSetupCrudBL.cs`; migrated all methods: file path helpers → `AppCompanyBL`, timezone helpers → new `AppTimeZoneBL`, cache/server methods → `AppSystemSettingBL`, web template listing → `AppTenantSettingBL`; settings admin UI now routes SysAdmin → `AppSystemSettingBL`, tenant → `AppTenantSettingBL` via identity type check; updated 18 caller files | ✅ Done 2026-05-15 |
| SysAdmin hardening | Connection string renamed `AppMasterDBConnectionString`; SysAdmin identity fast-path in `AppSaasUserSessionMgtBL` (no tenant lookup); `IsAdminUser()` reads identity directly (bypasses cache); `RequireCompanyAccess` guard on company CRUD endpoints; `CompanySettings` null-guarded in `ProcessDirtyAppCompanyExDto`; image URL helpers accept explicit `companyId` | ✅ Done 2026-05-15 |
| Company & Security redesign | SysAdmin sees Company Info + Users tabs only (not tenant role/menu/dashboard tabs); `TenantAdminUsersTab` queries master DB users by company; `AppSystemConstants.HostCompanyId = 1` centralized in `APP.Components.Dto`; `IsHostCompany` uses canonical constant; `AppCacheManagerBL.HostCompanyReserveId` references it | ✅ Done 2026-05-15 |
| Tenant login bug-fixes | (1) `AppTenantProvisioningBL.RegisterTenantDataSource` now sets `IsCompanyMasterDb = true` — without it `GetCurrentCompanyMasterDataSource` returned null and every tenant login threw "Cannot find User Master DB". (2) All tenant BL files (`AppTreeListMenuBL`, `AppDatabaseErDiagramBL`, etc.) replaced `new DataAccessAdapter(CurrentUserDbConnectionString)` with `AppTenantAdapterBL.GetTenantAdapter()` — direct adapter construction lacks the LLBLGen `CatalogNameOverwrites` and produces `Invalid object name 'AppMasterDB.dbo.X'`. (3) Removed `PrefetchPathAppSecurityGroupMember` from all four master-DB methods in `AppSecurityUserBL` — `AppSecurityGroupMember` is tenant-only; group membership must be fetched separately via `AppSecurityGroupBL`. | ✅ Done 2026-05-15 |
| DataSourceFrom routing + template seed repair | (1) `AppEntityInfoBL.GetSystemTableNoQueryLookupItem` now uses three-level fixture routing: DataSourceFrom DB → identity DB → master DB. Handles stale DataSourceFrom (template id after seed) and master-only tables (`AppSecurityUser`). (2) `AppCacheManagerBL.GetMasterDbFixture()` added. (3) `AppTenantProvisioningBL` extended: `AppEntityInfo` added to seed table list; `RegisterTenantDataSource` returns new `DataSourceId`; new Step 9 `UpdateDataSourceFromReferences` bulk-repairs all `DataSourceFrom` columns post-seed. (4) V004 migration drops cross-DB FK constraints on `DataSourceFrom` — `AppDataSourceRegister` stays authoritative in `AppMasterDB` only. (5) `AppTenantSettingBL.GetOrLoadCache` returns empty dict when `connStr` == `AppMasterDBConnectionString` (SysAdmin has no `AppTenantSetting`). (6) `AppLanguageBL.RetrieveDefaultAppLanguageEntity` falls back to `AppMasterDBConnectionString` when `GetContextAdapter()` throws — needed during session-restore before identity is registered. | ✅ Done 2026-05-15 |
| 5 | Replace `HttpContext.Current` with `IHttpContextAccessor` (requires .NET Core migration); per-tenant connection pooling | 🔲 Planned |

---

## 15. Known Constraints

| Constraint | Detail |
|-----------|--------|
| `ServerContext` is thread-static | Safe in ASP.NET 4.x (one thread per request) but breaks under `async/await`. Phase 5 will replace with `IHttpContextAccessor`. |
| LLBLGen entity classes not regenerated for new columns | `DomainToken` and `CustomDomain` on `AppDataSourceRegister` are accessed via raw ADO.NET. Regenerating LLBLGen would add them to the entity. |
| V003 AppSystemSetting section must be run manually | The migration runner only targets tenant DBs. The `AppSystemSetting` create/populate section of V003 must be applied to `AppMasterDB` manually via SSMS. |
| V001 migration placeholder | `PlmApplication/Migrations/V001__InitialTenantSchema.sql` must be populated with the full tenant DB schema via SSMS Generate Scripts before provisioning will produce a functional tenant. |
| Wildcard TLS cert requires DNS-01 challenge | Standard Certbot HTTP-01 challenge cannot issue `*.appai.com`. Use `certbot --dns-<provider>` plugin or supply an existing wildcard cert. |
| `MasterDB_Cleanup.sql` missing `AppSystemSetting` | The script's 28-table keep-list predates V003. Before running it against a production `AppMasterDB`, add `('AppSystemSetting')` to the `@keepTables` insert block, otherwise the settings table will be dropped. |
| `CompanySettings` is null for SysAdmin | `ServerContext.Instance.CompanySettings` is never set for SysAdmin (no tenant context). Any BL code that reads `CompanySettings.CompanyId` without a null-check will throw. Pattern: `if (ServerContext.Instance.CompanySettings != null && ...)`. |
| `TenantAdminUsersTab` requires `.AppCreatedByCompanyId` to be set | The Users tab for SysAdmin queries `AppSecurityUser WHERE AppCreatedByCompanyId = companyId`. Users created via `AppTenantProvisioningBL` have this set. Manually created users must also set this field or they will not appear. |
| `IsCompanyMasterDb` must be `true` on the primary tenant DB row | `AppSaasUserSessionMgtBL` filters `AppDataSourceRegister WHERE IsCompanyMasterDb = 1` at every login. Tenants provisioned before the 2026-05-15 fix have this flag unset and will fail to log in. One-time repair: `UPDATE AppDataSourceRegister SET IsCompanyMasterDb = 1 WHERE IsCompanyMasterDb IS NULL OR IsCompanyMasterDb = 0;` (run against `AppMasterDB`). |
| Always use `GetTenantAdapter()` — never construct adapter directly for tenant tables | `new DataAccessAdapter(CurrentUserDbConnectionString)` lacks the LLBLGen `CatalogNameOverwrites` entry that rewrites `AppMasterDB` → `TenantDB_X` in generated SQL. Symptom: `ORMQueryExecutionException: Invalid object name 'AppMasterDB.dbo.<TableName>'` even when the connection string points to the correct tenant DB. |
| `AppSecurityGroupMember` must not be prefetched via master-DB adapter | It does not exist in `AppMasterDB`. All four master-DB fetch methods in `AppSecurityUserBL` must omit this prefetch path. Fetch group membership separately through `AppSecurityGroupBL`. |
| `DataSourceFrom` stale after template seed | When a tenant DB is seeded from `AppTenantTemplateDB`, `AppEntityInfo.DataSourceFrom`, `AppTransaction.DataSourceFrom`, and `AppDataSet.DataSourceFrom` retain the template's `DataSourceId`. Run the repair SQL (see §3.5) or trigger it via `AppTenantProvisioningBL.RepairDataSourceFromReferences`. New tenants are repaired automatically in Step 9 of provisioning. |
| `AppDataSourceRegister` must NOT be duplicated into tenant DBs | It is authoritative in `AppMasterDB` only. V004 migration removes the FK constraints that previously required a local copy. Any code that inserts into the tenant DB's `AppDataSourceRegister` should be removed — the runtime routing uses `AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId)` which reads from master DB via `AppDataSourceRegisterBL`. |
| `AppTenantSetting` does not exist in `AppMasterDB` | `AppTenantSettingBL.GetOrLoadCache` returns an empty dict for SysAdmin (whose `CurrentUserDbConnectionString` points to `AppMasterDB`). BL code reading tenant settings for a SysAdmin context must handle empty dict gracefully. |
| BL methods called before identity registration must not call `GetContextAdapter()` | During login and session-restore, `RegisterUserIdentityTotheSystem` has not yet run. Calling `GetContextAdapter()` (or `GetTenantAdapter()`) at that point throws `InvalidOperationException`. Pattern: try `GetContextAdapter()`, catch and fall back to `AppMasterDBConnectionString` (see §6.4). |
