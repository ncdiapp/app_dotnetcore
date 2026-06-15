# SysAdmin (Host Company) — Responsibilities, UI, BL & Database

**Project:** AppAI / AppBuilder Platform  
**Last updated:** 2026-06-10  
**Status:** Reference

---

## 1. Who Is SysAdmin

SysAdmin is the platform operator — the person running the AppAI installation on behalf of the host company. There is exactly one host company (`AppSystemConstants.HostCompanyId = 1`).

```
AppSecurityUser.DomainId = 1  (EmAppUserType.SysAdmin)
AppSecurityUser.MyOwnCompnanyId = 1 (host company)
```

**Session identity fast-path** — SysAdmin bypasses tenant DB lookup entirely:

```
RegisterUserIdentityTotheSystem()
  └─ DomainId == 1?
       CurrentUserDbConnectionString = AppMasterDBConnectionString
       CurrentWorkingCompanyId       = null   ← no tenant context
       CompanySettings               = null   ← must null-guard all BL callers
```

---

## 2. Responsibilities

| # | Responsibility | What it covers |
|---|---|---|
| 2.1 | **Manage all registered companies** | Create, edit, view, delete any `AppCompany` record across all tenants |
| 2.2 | **Manage all registered users** | View and manage user accounts for any tenant, scoped by selected company |
| 2.3 | **Tenant provisioning** | Create a new tenant end-to-end: DB creation, schema migration, seed, admin user, data source registration |
| 2.4 | **System setup** | Configure installation-wide settings (timeout, app URL, IIS pool name, backup paths, version) |
| 2.5 | **Database registration** | Register and manage tenant DB connection strings in `AppDataSourceRegister` |
| 2.6 | **Schema migrations** | Run pending SQL migrations across all tenant databases at deployment |
| 2.7 | **Module licensing** | Assign ordered modules to companies via `AppCompanyOrderModule` |
| 2.8 | **Audit & operations** | Read audit trail, backup logs, batch job logs |

---

## 3. UI Components

### 3.1 Route Access

| Route | SysAdmin | Tenant Admin | Notes |
|---|---|---|---|
| `/tenant-provisioning` | Yes | No | `sysAdminOnly: true` guard |
| `/db-driver-management` | Yes | No | `sysAdminOnly: true` guard |
| `/company-security` | Yes (Company Info + Users tabs only) | Yes (all role/menu/dashboard tabs) | Tab set differs by role |
| `/database-registration` | Yes | Yes | Shared |
| `/system-setting` | Yes (AppSystemSetting) | Yes (AppTenantSetting) | Different data source per role |

### 3.2 Sidebar Menu

SysAdmin sees a **hardcoded** menu — not loaded from `AppListMenu`. Tenant users see a dynamic menu.

```
Tenant Administration
  ├─ Tenant Provisioning      → /tenant-provisioning
  ├─ Company and Users        → /company-security
  └─ Database Registration    → /database-registration

System
  └─ System Setting           → /system-setting
```

### 3.3 Component Inventory

| Component | File | SysAdmin role |
|---|---|---|
| `adminPermissionHelper.ts` | `src/helper/` | `isMasterSysAdminFromContext()` — DomainId=1 only; `isAdminUserFromContext()` — DomainId=1 or 6; `getDefaultAuthenticatedPath()` — routes SysAdmin to `/company-security` |
| `TenantProvisioning.tsx` | `src/components/admin/` | Provision new tenant, run migrations on all tenants, repair admin users |
| `CompanySecuritySetting.tsx` | `src/components/admin/CompanySecuritySetting/` | Container; uses `SYSADMIN_SECTIONS` (Company Info + Users) when SysAdmin; `ALL_SECTIONS` for tenant admin |
| `TenantAdminUsersTab.tsx` | `src/components/admin/CompanySecuritySetting/` | Users grid scoped to the selected company — SysAdmin only |
| `CompanyEditor.tsx` | `src/components/admin/CompanySecuritySetting/` | Edit company name, domain token, logo, business-partner settings |
| `SysAdminGuard` (in routes.tsx) | `src/routes.tsx` | Route wrapper — redirects non-SysAdmin away from `sysAdminOnly` routes |

### 3.4 API Service Methods (adminsvc.ts)

| Method | Endpoint | Purpose |
|---|---|---|
| `provisionTenant(data)` | `POST /webapi/TenantProvisioning/Provision` | Full 9-step tenant creation |
| `runMigrationsOnAllTenants()` | `POST /webapi/TenantProvisioning/RunMigrations` | Apply pending SQL migrations to all tenant DBs |
| `repairTenantAdminUsers()` | `POST /webapi/TenantProvisioning/RepairAdminUsers` | Fix null/missing `IsRegisterCompleted` flags |
| `retrieveAllRootCompanyDtoList()` | `GET /webapi/Administration/RetrieveAllRootCompanyDtoList` | All companies list (SysAdmin only) |
| `deleteOneAppCompany(id)` | `DELETE /webapi/Administration/DeleteOneAppCompany` | Delete a company record |
| `RetrieveAllAppSetupDtoList(true)` | `GET /webapi/Administration/RetrieveAllAppSetupDtoList?isMasterDb=true` | Load `AppSystemSetting` values |

---

## 4. BL Classes

### 4.1 MasterAdmin/ — SysAdmin operations on AppMasterDB

| File | Key methods | Notes |
|---|---|---|
| `AppTenantProvisioningBL.cs` | `ProvisionNewTenant()` — 9-step pipeline<br/>`RunMigrationsOnAllTenants()`<br/>`RepairTenantAdminUsers()`<br/>`UpdateDataSourceFromReferences()` | Only called from `TenantProvisioningController` which is SysAdmin-gated |
| `AppSystemSettingBL.cs` | `GetStringValue(key)`, `GetIntValue(key)`, `GetBoolValue(key)`<br/>`RetrieveAllAsDto()`<br/>`SaveAll(settings)`<br/>`Reload()` | Reads `AppMasterDB.AppSystemSetting`; loaded at startup into a static dictionary |
| `AppCompanyBL.cs` | `AppMasterDBConnectionString` (static)<br/>`HostCompanyId = 1` (constant)<br/>`RetrieveOneAppCompanyEntity(id)`<br/>`CreateMyCompanyFolder(companyId)` | All methods use master connection string directly |
| `AppDataSourceRegisterBL.cs` | `GetTenantInfoByHost(host)`<br/>`RegisterTenantDataSource()`<br/>`GetCurrentCompanyMasterDataSource(companyId)` | Tenant DB registry; enforces `IsCompanyMasterDb = true` |
| `AppSaasUserSessionMgtBL.cs` | `ViladateSessionIdAndCompanyIdRegisterIdentity(sessionId)`<br/>`RegisterUserIdentityTotheSystem(session)`<br/>`ClassifyCurrentLoginUserType()` | SysAdmin fast-path skips tenant DB lookup |

### 4.2 MasterAuth/ — Identity & authentication

| File | Key methods | Notes |
|---|---|---|
| `AppSecurityUserBL.cs` | `IsAdminUser()` — reads `CurrentLoginUserType` (never loads entity)<br/>`RetrieveAppSecurityUserDtoByCompanyId(companyId)`<br/>`CurrentUserId`, `CurrentUserEntity` | `IsAdminUser()` returns true for DomainId=1 and DomainId=6 |
| `AppSecurityManagementBL.cs` | 40+ security operation methods | All gated by `if (!AppSecurityUserBL.IsAdminUser()) throw` |
| `AppSecurityAuthenticationBL.cs` | Login pipeline | Special handling for SaasCompanyAdmin at login |
| `AppSaasAccountUserBL.cs` | `GetAccessibleCompaniesForUser(userId)`<br/>User registration, invite flows | `IsAdminUser()` checked before privileged user-management operations |

### 4.3 Controller

| File | SysAdmin endpoints |
|---|---|
| `TenantProvisioningController.cs` | `POST Provision`, `POST RunMigrations`, `POST RepairAdminUsers` — all require DomainId=1 |
| `AdministrationController.cs` | `RequireCompanyAccess()` guard — SysAdmin always passes; `RetrieveOneAppCompanyExDto`, `SaveOneAppCompanyExDto`, `RetrieveAppSecurityUserDtoByCompanyId` |

---

## 5. Database Tables

### 5.1 SysAdmin-Exclusive (no tenant user ever accesses these)

| Table | Purpose |
|---|---|
| `AppCompany` | Tenant registry — one row per tenant; source of truth for `CompanyDomainIdentityToken` |
| `AppDataSourceRegister` | Tenant DB pointers — AES-256 encrypted connection string, `DomainToken`, `CustomDomain`, `IsCompanyMasterDb` flag |
| `AppSystemSetting` | Installation-wide settings — one global value per key, applies to all tenants |
| `AppSetup` | Platform installation setup records |
| `AppModuleLibRegister` | Registered module/plugin libraries |
| `AppCompanyOrderModule` | Modules licensed per company |
| `AppCompanyUserTypeRegister` | User type definitions per company (customisable role names) |

### 5.2 Shared — SysAdmin manages platform-wide; tenant admin manages own scope only

| Table | Purpose |
|---|---|
| `AppSecurityUser` | All user accounts across all tenants (`DomainId`, `MyOwnCompnanyId`, `AppCreatedByCompanyId`) |
| `AppSecurityUserSession` | Active session tokens — validated on every authenticated request |
| `AppSecurityRegDomain` | OAuth / SSO domain registrations |
| `AppSecurityRegDomainORG` | Org-level domain groupings |
| `AppSecurityLoginAuditor` | Login attempt audit trail |
| `AppSecurityAuthticationInfo` | MFA / 2FA settings |
| `AppSecurityUserContact` | User contact details (email, phone) |
| `AppSecurityUserInvitation` | Pending invite tokens (self-register flow) |
| `AppBusinessPartner` | Cross-company partner records *(→ moving to Tenant DB — see Design_CrossCompanyUserIdentity.md)* |
| `AppBusinessPartnerInviteUser` | Cross-company invite links (`EmInvitedUserType`) |
| `AppBusinessPartnerInviteUserChildUser` | Partner invitation child user mapping |

### 5.3 Reference Data (SysAdmin maintains; all tenants read)

| Table | Purpose |
|---|---|
| `AppCountry`, `AppCountryRegion` | Country and region lookups |
| `AppCurrency` | Currency definitions |
| `AppTimeZoneAbbreviation` | Timezone abbreviation dictionary |
| `AppLanguage`, `AppLanguageKey`, `AppSysLabelLanguage` | Platform-level translations (also replicated per tenant DB; accessed via `GetContextAdapter()`) |
| `AppLogTrack`, `AppBackupLog`, `AppBatchLog` | Audit trail, backup logs, batch job logs |

---

## 6. Access Boundary Summary

| Concern | SysAdmin (DomainId=1) | SaasCompanyAdmin (DomainId=6) | Tenant User (DomainId 2–5) |
|---|---|---|---|
| DB context | AppMasterDB only | Own TenantDB only | Own TenantDB only |
| `CompanySettings` in ServerContext | `null` — must null-guard | Populated | Populated |
| Default landing page | `/company-security` | `/home` | `/home` |
| Sidebar | Hardcoded 4 items | Dynamic from `AppListMenu` | Dynamic from `AppListMenu` |
| See all companies | Yes | Own company only | No |
| Provision new tenant | Yes | No | No |
| System settings | Yes (`AppSystemSetting`) | No | No |
| Tenant settings | No | Yes (`AppTenantSetting`) | No |
| Security groups / menus | No (tenant DB — no access) | Yes (own tenant) | Read / use only |
| Tenant business data | No | Yes | Yes |

---

## 7. Key Invariants

1. **`CompanySettings` is always `null` for SysAdmin.** Any BL reading `ServerContext.Instance.CompanySettings` must null-check first.

2. **`GetTenantAdapter()` throws for SysAdmin.** It requires `CurrentUserDbConnectionString` to point to a tenant DB — which SysAdmin never has. Use `AppCompanyBL.AppMasterDBConnectionString` in all `MasterAdmin/` BL code.

3. **`IsAdminUser()` returns `true` for both DomainId=1 and DomainId=6.** Use `isMasterSysAdminFromContext()` (React) or compare `CurrentLoginUserType == (int)EmAppUserType.SysAdmin` (C#) when SysAdmin-only behaviour is needed.

4. **`AppDataSourceRegister.IsCompanyMasterDb = true` is required** on every tenant's primary DB row. Missing flag causes "Cannot find User Master DB" on every login for that tenant.

5. **`HostCompanyId = 1` is the single source of truth** — defined in `AppSystemConstants` in `APP.Components.Dto/AppEnums.cs`. Never hardcode `1` directly elsewhere.
