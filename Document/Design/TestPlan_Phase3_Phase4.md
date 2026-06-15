# Test Plan — Phase 3 & Phase 4 Multi-Tenant

**Scope:** Phase 3 (Custom URL per Tenant) and Phase 4 (Tenant Provisioning Automation)  
**Date:** 2026-05-08

---

## 1. Phase 3 — Custom URL per Tenant

### 1.1 DB Migration (`Phase3_AddTenantDomain.sql`)

| # | Test | Steps | Expected |
|---|------|-------|----------|
| 3.1.1 | Columns added | Run script on AppMasterDB | `CustomDomain` and `DomainToken` columns appear in `AppDataSourceRegister` |
| 3.1.2 | Idempotent | Run script a second time | No error; "already exists" print messages only |
| 3.1.3 | DomainToken seeded | Check rows after script | Rows with a matching `AppCompany.CompanyDomainIdentityToken` have `DomainToken` set (lowercased) |
| 3.1.4 | NULL rows unchanged | Rows with no matching company token | `DomainToken` remains NULL |

**Verification query:**
```sql
SELECT ds.DataSourceId, ds.DataSourceName, ds.DomainToken, ds.CustomDomain,
       c.CompanyDomainIdentityToken
FROM   AppDataSourceRegister ds
LEFT JOIN AppCompany c ON c.CompanyId = ds.DataSourceOwnerCompanyId;
```

---

### 1.2 `AppDataSourceRegisterBL.GetTenantInfoByHost()`

| # | Test case | Input `host` | Expected |
|---|-----------|--------------|----------|
| 3.2.1 | Subdomain match | `acme.appai.com` | `IsFound=true`, `DomainToken="acme"`, `CompanyName` populated |
| 3.2.2 | Subdomain with port | `acme.appai.com:443` | Same as 3.2.1 (port stripped) |
| 3.2.3 | Custom domain match | `app.acme.com` | `IsFound=true` (matches `CustomDomain` column) |
| 3.2.4 | Unknown subdomain | `unknown.appai.com` | `IsFound=false` |
| 3.2.5 | Root domain (no subdomain) | `appai.com` | `IsFound=false` (only 2 parts, no token extracted) |
| 3.2.6 | Empty host | `""` | `IsFound=false` |
| 3.2.7 | Null host | `null` | `IsFound=false` (no exception) |
| 3.2.8 | Case-insensitive | `ACME.appai.com` | `IsFound=true` (lowercase comparison) |

---

### 1.3 `GET /webapi/Tenant/Info` (TenantController)

| # | Test | Request | Expected |
|---|------|---------|----------|
| 3.3.1 | Known tenant via subdomain | Host: `acme.appai.com` | 200 `{ isFound: true, companyName: "...", domainToken: "acme" }` |
| 3.3.2 | Known tenant via custom domain | Host: `app.acme.com` | 200 `{ isFound: true, customDomain: "app.acme.com" }` |
| 3.3.3 | Unknown host | Host: `localhost` | 200 `{ isFound: false }` |
| 3.3.4 | No auth required | No session cookie | 200 (public endpoint, no 401) |
| 3.3.5 | Nginx proxy header used | `X-Forwarded-Host: acme.appai.com` | 200 `{ isFound: true }` (takes forwarded host over direct) |

**curl examples:**
```bash
# Direct
curl -H "Host: acme.appai.com" http://localhost/appai/webapi/Tenant/Info

# Via nginx proxy
curl -H "X-Forwarded-Host: acme.appai.com" http://localhost/appai/webapi/Tenant/Info
```

---

### 1.4 React — Tenant Branding

| # | Test | Steps | Expected |
|---|------|-------|----------|
| 3.4.1 | Branding loads on startup | Navigate to app at `acme.appai.com` | Redux `tenantBranding.isFound = true`, `companyName` populated (check Redux DevTools) |
| 3.4.2 | Login heading shows company name | Load login page as known tenant | `<h2>` shows `"Acme Corp"` (not `"Welcome"`) |
| 3.4.3 | Fallback to "Welcome" | Load login from `localhost` | `<h2>` shows `"Welcome"` |
| 3.4.4 | Tenant loaded before login renders | Network throttle to slow 3G | Company name visible before any auth call |
| 3.4.5 | API error graceful | Block `GET /webapi/Tenant/Info` (DevTools) | Login page renders normally with `"Welcome"` fallback |
| 3.4.6 | Single API call per session | Reload page multiple times | `GET /webapi/Tenant/Info` called exactly once per app lifetime (module-level `tenantInfoLoaded` flag) |

---

## 2. Phase 4 — Tenant Provisioning Automation

### 2.1 `AppTenantProvisioningBL.ProvisionNewTenant()`

#### 2.1.1 Validation

| # | Test | Input | Expected |
|---|------|-------|----------|
| 4.1.1 | Missing CompanyName | `companyName: null` | `Success=false`, `ErrorMessage` contains "required" |
| 4.1.2 | Missing DomainToken | `domainToken: null` | `Success=false` |
| 4.1.3 | Missing AdminEmail | `adminEmail: null` | `Success=false` |
| 4.1.4 | Missing AdminLoginName | `adminLoginName: null` | `Success=false` |
| 4.1.5 | Missing AdminPassword | `adminPassword: null` | `Success=false` |
| 4.1.6 | Invalid DomainToken (uppercase) | `domainToken: "ACME"` | `Success=false`, validation error |
| 4.1.7 | Invalid DomainToken (spaces) | `domainToken: "my corp"` | `Success=false` |
| 4.1.8 | Invalid DomainToken (too short) | `domainToken: "a"` | `Success=false` |
| 4.1.9 | Duplicate DomainToken | Token already in `AppDataSourceRegister` or `AppCompany` | `Success=false`, "already in use" message |

#### 2.1.2 Happy Path (end-to-end)

```json
POST /webapi/TenantProvisioning/Provision
{
  "companyName": "Test Corp",
  "domainToken": "testcorp",
  "adminEmail": "admin@testcorp.com",
  "adminLoginName": "testcorp.admin",
  "adminPassword": "Test@1234"
}
```

| # | Test | Check | Expected |
|---|------|-------|----------|
| 4.1.10 | Success flag | Response body | `success: true` |
| 4.1.11 | CompanyId assigned | Response body | `companyId` is a positive integer |
| 4.1.12 | DB name | Response body | `databaseName: "AppTenantDB_{companyId}"` |
| 4.1.13 | Login URL | Response body | `loginUrl: "testcorp.appai.com"` |
| 4.1.14 | AppCompany created | `SELECT * FROM AppMasterDB..AppCompany WHERE CompanyDomainIdentityToken = 'testcorp'` | 1 row; `ShortName = "Test Corp"`, `Status = "A"` |
| 4.1.15 | DB created | `SELECT DB_ID('AppTenantDB_{companyId}')` on SQL Server | Not null |
| 4.1.16 | Admin user created | `SELECT * FROM AppMasterDB..AppSecurityUser WHERE LoginName = 'testcorp.admin'` | 1 row; `IsActive=1`, `IsDeleted=0`, `DomainId=6` (SaasCompanyAdmin), password is hashed (not plaintext) |
| 4.1.17 | DataSource registered | `SELECT * FROM AppMasterDB..AppDataSourceRegister WHERE DataSourceOwnerCompanyId = {companyId}` | 1 row; `DomainToken='testcorp'`, `ConnectionString` starts with `AES:` |
| 4.1.18 | Provisioning time | Stopwatch | Completes in < 60 seconds |

---

### 2.2 `AppTenantMigrationRunnerBL`

| # | Test | Steps | Expected |
|---|------|-------|----------|
| 4.2.1 | Tracking table auto-created | Run against fresh DB with no `_SchemaMigrations` table | Table created; no error |
| 4.2.2 | V001 applied | Run on fresh DB | `V001__InitialTenantSchema` recorded in `_SchemaMigrations`; `MigrationsApplied = 1` |
| 4.2.3 | Idempotent | Run on DB where V001 already applied | `MigrationsApplied = 0` |
| 4.2.4 | New migration picked up | Add `V003__Test.sql` with `SELECT 1` to Migrations folder | V003 applied; V001/V002 skipped |
| 4.2.5 | GO splitting | Script with multiple GO-separated batches | All batches execute; no parse error |
| 4.2.6 | Lexicographic order | V002 must apply after V001 | Check `AppliedAt` timestamps in `_SchemaMigrations` |
| 4.2.7 | RunMigrationsOnAllTenants | Two registered tenant DBs | Returns map `{ "TenantA": 0, "TenantB": 0 }` (all up-to-date) |
| 4.2.8 | Bad connection string | One tenant has invalid connection string | That tenant returns `-1`; other tenants not affected |
| 4.2.9 | Empty Migrations folder | Remove all `.sql` files | Returns `0`, no error |
| 4.2.10 | Migrations folder missing | Delete `~/Migrations/` directory | Returns `0`, no error |

---

### 2.3 `TenantProvisioningController` — Authorization

| # | Test | User | Expected |
|---|------|------|----------|
| 4.3.1 | No session | No cookie | 401 (from `SecureBaseWebApiController`) |
| 4.3.2 | Non-admin user | Regular user session | 403 Forbidden |
| 4.3.3 | SysAdmin user — Provision | SysAdmin session | 200 + provision result |
| 4.3.4 | SysAdmin user — RunMigrations | SysAdmin session | 200 + `{ "TenantA": 0, ... }` |
| 4.3.5 | SaasCompanyAdmin user | Company admin session | 403 (not SysAdmin) |

---

## 3. Security Tests

| # | Test | Expected |
|---|------|----------|
| S.1 | SQL injection via DomainToken | `domainToken: "'; DROP TABLE AppCompany;--"` → fails regex validation before any DB call |
| S.2 | SQL injection via DB name | DB name derived from `companyId` (integer), not user input → safe |
| S.3 | Cross-tenant branding leak | `GET /Tenant/Info` from tenant A URL → returns only tenant A data |
| S.4 | Encrypted connection string | `AppDataSourceRegister.ConnectionString` stored with `AES:` prefix, never plaintext |
| S.5 | Admin password hashed | `AppSecurityUser.Password` for new admin is hashed via `AppSecurityPasswordHashBL`, not stored plaintext |
| S.6 | TenantController is truly public | Call `/webapi/Tenant/Info` with no cookies and expired session → 200 (not 401) |
| S.7 | TenantProvisioning is locked | Call `/webapi/TenantProvisioning/Provision` with no cookies → 401 |

---

## 4. Regression Tests (existing functionality)

| # | Test | Expected |
|---|------|----------|
| R.1 | Login still works for existing tenant | Login to existing hvac tenant via existing URL | Success |
| R.2 | Tenant DB isolation still enforced | Company A user cannot read Company B data | Same as Phase 2 verification |
| R.3 | `GET /webapi/Tenant/Info` from non-tenant host | No company record matching `localhost` | `{ isFound: false }` — does not break login |
| R.4 | Existing AppDataSourceRegister rows unaffected | Rows before migration have `DomainToken=NULL` unless seeded | Correct; no data corruption |

---

## 5. Test Data Setup

```sql
-- Seed a test tenant entry for Phase 3 tests
UPDATE AppDataSourceRegister
SET    DomainToken = 'acme'
WHERE  DataSourceOwnerCompanyId = (
    SELECT TOP 1 AppCompanyId FROM AppCompany WHERE ShortName = 'hvac');

-- Verify
SELECT DataSourceId, DataSourceName, DomainToken, CustomDomain
FROM   AppDataSourceRegister
WHERE  DomainToken IS NOT NULL;
```

---

## 6. Cleanup After Testing

```sql
-- Remove test provisioning data (run on AppMasterDB)
-- Replace {companyId} with the ID returned by the provisioning test

DELETE FROM AppSecurityUser          WHERE AppCreatedByCompanyId = {companyId};
DELETE FROM AppDataSourceRegister    WHERE DataSourceOwnerCompanyId = {companyId};
DELETE FROM AppCompany               WHERE AppCompanyId = {companyId};

-- Drop the test tenant DB
IF DB_ID('AppTenantDB_{companyId}') IS NOT NULL
    DROP DATABASE [AppTenantDB_{companyId}];
```

---

## 7. Go/No-Go Criteria (matches plan verification checklist)

| Criterion | Test(s) | Status |
|-----------|---------|--------|
| New tenant provisioned via API in < 60 seconds | 4.1.18 | ☐ |
| Tenant DB created with full schema | 4.1.15, 4.2.2 | ☐ |
| Template data copied correctly | 4.1.16 (admin user) | ☐ |
| Schema migrations run on all tenant DBs at deployment | 4.2.7 | ☐ |
| No tenant DB schema drift (idempotent runner) | 4.2.3 | ☐ |
| Branded login page shows tenant company name | 3.4.2 | ☐ |
| Public `/Tenant/Info` endpoint resolves from Host header | 3.3.1, 3.3.5 | ☐ |
| Provisioning endpoint locked to SysAdmin | 4.3.1–4.3.5 | ☐ |
