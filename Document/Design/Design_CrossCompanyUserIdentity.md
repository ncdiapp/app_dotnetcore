# Cross-Company User Identity & Multi-Company Login Design

**Project:** AppAI / AppBuilder Platform  
**Branch:** feature/dotnet10-migration  
**Last updated:** 2026-06-09  
**Status:** Implementation Ready

---

## 1. Problem Statement

### 1.1 Domain Type Conflict

`AppSecurityUser.DomainId` is a single INT column representing a user's role. When Company B invites a user who is already a `SaasCompanyAdmin` (DomainId = 6) of Company A as a Supplier (DomainId = 4), the system has two options — both wrong:

- **Overwrite DomainId = 4** → user loses their SaasCompanyAdmin role in their own company
- **Leave DomainId = 6** → user is classified as SaasCompanyAdmin when logging into Company B

The root cause is that **home domain** and **cross-company role** are conflated into a single field.

### 1.2 Shared-URL Multi-Company Login

When multiple companies share the same application URL (no tenant-specific subdomain), the system cannot auto-resolve which company a user is logging into from the URL alone. A user who belongs to multiple companies has no way to select context.

### 1.3 AppBusinessPartnerInviteUser Key Inconsistency

The invite table uses `AppCompanyId` as the company reference in queries and duplicate checks, but `AppCreatedByCompanyId` is the semantically correct ownership field. The two are set identically at creation but queried inconsistently, and `EmInvitedUserType` (the per-user cross-company role) is never populated even though the column already exists.

---

## 2. Core Design Principle

```
AppSecurityUser.DomainId             = HOME domain
                                       Set once at account creation.
                                       Never modified by invitation flows.

AppBusinessPartnerInviteUser         = CROSS-COMPANY role
  .EmInvitedUserType                   The user's effective role when logged
                                       into the inviting company's tenant.

AppBusinessPartner.PartnerType       = COMPANY-LEVEL fallback role
                                       Used when EmInvitedUserType is absent
                                       (backward compat with existing data).
```

---

## 3. Invitation Flow — Account Creation Decision

When Company B invites a user by email, the system must first determine whether that user already exists. The answer drives the entire account creation path.

### 3.1 Decision Tree

```
Company B invites user@email.com as Supplier
  │
  ├─ LOOKUP AppSecurityUser by email
  │
  ├─── FOUND (user already has an account)
  │         │
  │         ├─ Has own company (MyOwnCompnanyId is set)?
  │         │    e.g. SaasCompanyAdmin of Company A
  │         │    → Link only: CREATE AppBusinessPartnerInviteUser
  │         │                   { UserId, AppCreatedByCompanyId=B, EmInvitedUserType=4 }
  │         │    → AppSecurityUser.DomainId UNTOUCHED (stays 6)
  │         │    → Send "You've been invited" notification email
  │         │
  │         └─ No own company (MyOwnCompnanyId = null)?
  │              e.g. a pure external user from a prior invite
  │              → Link only: CREATE AppBusinessPartnerInviteUser (same as above)
  │              → DomainId UNTOUCHED
  │
  └─── NOT FOUND (no account exists for this email)
            │
            ├─── Path 1: Pre-create  (simple external users)
            │    Used when: invited user is a customer/supplier with no own platform company
            │
            │    Inviting company creates the account:
            │      AppSecurityUser {
            │        DomainId         = 4 (Supplier)   ← home domain IS the invited role
            │        MyOwnCompnanyId  = null            ← no home company
            │        AppCreatedByCompanyId = Company B  ← created by inviting company
            │      }
            │    AppBusinessPartnerInviteUser NOT needed
            │    (DomainId already expresses the role; ClassifyCurrentLoginUserType
            │     routes via MyOwnCompnanyId=null → SetupBusinessParterUserType →
            │     finds AppBusinessPartner.PartnerType)
            │    Send activation email → user sets password → can log in
            │
            └─── Path 2: Self-register  (B2B partners who need their own company)
                 Used when: invited user will administer their own platform company

                 Send invitation link with token → user clicks
                   → Self-registration page: user registers own company
                   → AppSecurityUser {
                       DomainId         = 6 (SaasCompanyAdmin)
                       MyOwnCompnanyId  = new CompanyId
                     }
                   → Invitation auto-links on registration completion:
                       AppBusinessPartnerInviteUser {
                         UserId                = new user
                         AppCreatedByCompanyId = Company B
                         EmInvitedUserType     = 4 (Supplier)
                       }
                   → User has full admin in own company + Supplier role in Company B
```

### 3.2 How DomainId Invariant Holds in Every Path

| Path | Who creates the account | DomainId set to | MyOwnCompnanyId | EmInvitedUserType |
|---|---|---|---|---|
| Existing user with own company | Already exists | Unchanged (e.g. 6) | Existing | Set in invite row |
| Existing user with no company | Already exists | Unchanged (e.g. 4) | null | Set in invite row |
| Pre-create (new, no own company) | Inviting company | Invited role (3 or 4) at creation | null | Not needed — DomainId IS the role |
| Self-register (new, own company) | User themselves | 6 (SaasCompanyAdmin) at self-registration | Own company | Set in invite row on completion |

**In every case, DomainId is set exactly once — at the moment the account row is first inserted — and is never written again by an invitation flow.**

### 3.3 AppSecurityUserInvitation — Pending Invite Token

`AppMasterDB.AppSecurityUserInvitation` tracks the pending invitation state for Path 2 (self-register):

```
AppSecurityUserInvitation
├── InvitationId           PK
├── AppCreatedByCompanyId  ← inviting company
├── InvitedEmail           ← who was invited
├── EmInvitedUserType      ← role they will get (Supplier = 4)
├── AppBusinessPartnerId   ← which partner record to link to
├── InvitationToken        ← encrypted link token (sent in email)
├── ExpiresAt              ← token expiry
└── IsCompleted            ← true after self-registration finishes
```

On self-registration completion, BL reads this record via the token, creates the `AppBusinessPartnerInviteUser` row, and marks `IsCompleted = true`.

For Path 1 (pre-create), no `AppSecurityUserInvitation` row is needed — the account is active immediately; only an activation email (password-set link) is sent.

### 3.4 Choosing Between Path 1 and Path 2

The inviting company admin makes this choice at invite time:

| Signal | Recommended path |
|---|---|
| Inviting a known customer or one-off supplier with no platform presence | Path 1 — Pre-create |
| Inviting a business partner company that will manage their own users and data | Path 2 — Self-register |
| Invited email already found in `AppSecurityUser` | Neither — link only |

The UI should surface this as a simple toggle: **"Create account for them"** (Path 1) vs **"Send self-registration link"** (Path 2).

---

## 3. Data Model

### 3.1 AppBusinessPartnerInviteUser (AppMasterDB) — Stays in MasterDB

```
AppBusinessPartnerInviteUser
├── PartnernerInvitedUserID    INT IDENTITY  PK
├── AppBusinessPartnerID       INT NULL      FK → AppBusinessPartner
├── UserID                     INT NULL      FK → AppSecurityUser
├── AppCreatedByCompanyID      INT NULL      ← owning/inviting company (LOGICAL KEY field)
├── AppCompanyID               INT NULL      ← keep for backward compat only; same value
├── EmInvitedUserType          INT NULL      ← per-user role in the inviting company
│                                              values from EmAppUserType enum
├── AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID  (audit)

UNIQUE CONSTRAINT (AppBusinessPartnerID, AppCreatedByCompanyID, UserID)
```

**Logical key:** `(AppBusinessPartnerID, AppCreatedByCompanyID)` identifies the partner-company relationship.  
**Row uniqueness:** `(AppBusinessPartnerID, AppCreatedByCompanyID, UserID)` — one invite per user per partner per company.

> `EmInvitedUserType` column already exists in the DB, entity class, DTO, and converter. It is simply never written or read — this plan fixes that.

### 3.2 AppBusinessPartner — Move to Tenant DB

#### Why It Belongs in Tenant DB

`AppBusinessPartner` is currently in `AppMasterDB` but every query filters by `AppCompanyId` — it is tenant-scoped CRM data (vendor/customer records), not shared identity infrastructure. Evidence:

| Indicator | Detail |
|---|---|
| Every query filters by `AppCompanyId` | `RetrieveCompanyPartnerDtoList` line 94: `AppBusinessPartnerFields.AppCompanyId == compnayId` |
| BL file is in `TenantBusiness/` folder | `APP.BL/TenantBusiness/AppComBusinessPartnerBL.cs` — wrong folder for MasterDB data |
| BL uses `MasterConnStr` as a workaround | `private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;` (line 30) — this is the anomaly to fix |
| CRM fields | Code, ShortName, FullName, Address, ContactPhone, CurrencyCode — tenant-specific business records |
| No cross-tenant sharing | A vendor record for Company A is invisible to Company B by design |

#### What Was Blocking the Move

`SetupBusinessParterUserType` in `AppSaasUserSessionMgtBL.cs` called `GetCurrentUserWorkingCompanyBusinessPartner()` to read `AppBusinessPartner.PartnerType` during **session identity registration** — before any tenant context is established. Querying a tenant DB table at that point is impossible.

**§4.2 and §4.3 of this plan remove that dependency.** After those fixes, session identity reads `EmInvitedUserType` from `AppBusinessPartnerInviteUser` (MasterDB) only — `AppBusinessPartner` is never touched during the identity pipeline.

#### Target Schema in Tenant DB

```
AppBusinessPartner  (TenantDB_{CODE} — one copy per tenant)
├── AppBusinessPartnerID       INT IDENTITY  PK
├── AppCreatedByCompanyID      NOT NULL      ← tenant owner; replaces AppCompanyID
├── Code                       NOT NULL
├── ShortName, FullName
├── Adress1, Adress2, Adress3, City, State, PostCode, Country
├── Language, CurrencyCode, Status
├── ContactPhone, ContactName, ContactFax
├── PartnerType                INT NULL      ← CRM classification (not used for identity after §4.3)
├── ShipToID, BillToID, IsBillToSameShipTo
├── MappingExternalBusinessPartnerAccountId
├── AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID  (audit)
DROP AppCompanyID                            ← redundant; same value as AppCreatedByCompanyID

UNIQUE (Code, AppCreatedByCompanyID)         ← no duplicate vendor codes per tenant
```

`AppBusinessPartnerInviteUser` stays in **MasterDB** and keeps `AppBusinessPartnerID` as a **logical reference only** (no FK enforced cross-DB). The physical FK was already dropped by the V004 migration pattern.

#### Migration Steps

**Step 1 — Add to tenant DB schema** (new migration `V005__AddAppBusinessPartner.sql`):

```sql
CREATE TABLE [dbo].[AppBusinessPartner] (
    AppBusinessPartnerID   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AppCreatedByCompanyID  INT NOT NULL,
    Code                   NVARCHAR(50)  NOT NULL,
    ShortName              NVARCHAR(100) NULL,
    FullName               NVARCHAR(200) NULL,
    Adress1                NVARCHAR(200) NULL,
    Adress2                NVARCHAR(200) NULL,
    Adress3                NVARCHAR(200) NULL,
    City                   NVARCHAR(100) NULL,
    Language               NVARCHAR(10)  NULL,
    State                  NVARCHAR(100) NULL,
    PostCode               NVARCHAR(20)  NULL,
    Country                NVARCHAR(100) NULL,
    Status                 INT           NULL,
    CurrencyCode           NVARCHAR(10)  NULL,
    ContactPhone           NVARCHAR(50)  NULL,
    ContactName            NVARCHAR(100) NULL,
    ContactFax             NVARCHAR(50)  NULL,
    PartnerType            INT           NULL,
    ShipToID               INT           NULL,
    BillToID               INT           NULL,
    IsBillToSameShipTo     BIT           NULL,
    MappingExternalBusinessPartnerAccountId NVARCHAR(200) NULL,
    AppCreatedByID         INT           NULL,
    AppCreatedDate         DATETIME      NULL,
    AppModifiedDate        DATETIME      NULL,
    AppModifiedByID        INT           NULL,
    CONSTRAINT UQ_AppBusinessPartner_Code UNIQUE (Code, AppCreatedByCompanyID)
);
```

**Step 2 — Copy existing data per tenant**:

```sql
-- Run once per tenant (substitute @companyId and target catalog)
INSERT INTO [TenantDB_ACME].[dbo].[AppBusinessPartner]
    (AppCreatedByCompanyID, Code, ShortName, FullName,
     Adress1, Adress2, Adress3, City, Language, State, PostCode, Country,
     Status, CurrencyCode, ContactPhone, ContactName, ContactFax,
     PartnerType, ShipToID, BillToID, IsBillToSameShipTo,
     MappingExternalBusinessPartnerAccountId,
     AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID)
SELECT
    AppCreatedByCompanyID, Code, ShortName, FullName,
    Adress1, Adress2, Adress3, City, Language, State, PostCode, Country,
    Status, CurrencyCode, ContactPhone, ContactName, ContactFax,
    PartnerType, ShipToID, BillToID, IsBillToSameShipTo,
    MappingExternalBusinessPartnerAccountId,
    AppCreatedByID, AppCreatedDate, AppModifiedDate, AppModifiedByID
FROM [AppMasterDB].[dbo].[AppBusinessPartner]
WHERE AppCompanyID = @companyId;
```

> Run via `AppTenantProvisioningBL.RunMigrationsOnAllTenants()` or SSMS per tenant. Verify row counts match before cutover.

**Step 3 — Switch BL to tenant adapter** (`APP.BL/TenantBusiness/AppComBusinessPartnerBL.cs`):

```csharp
// BEFORE (line 30): using MasterDB as workaround
private static string MasterConnStr => AppCompanyBL.AppMasterDBConnectionString;
// every method: new DataAccessAdapter(MasterConnStr)

// AFTER: use tenant adapter — remove MasterConnStr entirely
// every method: AppTenantAdapterBL.GetTenantAdapter()
```

Remove `AppCompanyId` filter from all queries (isolation is now enforced by the DB connection itself):

```csharp
// BEFORE
filter.PredicateExpression.AddWithAnd(AppBusinessPartnerFields.AppCompanyId == compnayId.Value);

// AFTER — no filter needed; tenant adapter connects to the right DB
```

**Step 4 — Update AppCacheManager** (`APP.BL/Infrastructure/AppCacheManager.cs`):

`GetCurrentUserWorkingCompanyBusinessPartner` currently queries `AppBusinessPartnerInviteUser` (MasterDB) then prefetches `AppBusinessPartner`. After the move, the prefetch path crosses databases — remove it. The cache method returns only the invite row; `AppBusinessPartner` is loaded separately via tenant adapter when needed.

```csharp
// Remove this prefetch from GetCurrentUserWorkingCompanyBusinessPartner:
// prefetchPath.Add(AppBusinessPartnerInviteUserEntity.PrefetchPathAppBusinessPartner);

// The method now only caches EmInvitedUserType (already added in §4.2)
// AppBusinessPartner CRM data is fetched on-demand via AppBusinessPartnerBL using GetTenantAdapter()
```

**Step 5 — Archive from MasterDB** (after verification period):

```sql
-- Rename rather than drop — safe rollback window
EXEC sp_rename 'AppBusinessPartner', 'AppBusinessPartner_MasterArchive';
-- Drop after 2–4 weeks of confirmed stable operation
```

#### BL File Changes for the Move

| File | Change |
|---|---|
| `APP.BL/TenantBusiness/AppComBusinessPartnerBL.cs` | Replace `MasterConnStr` with `GetTenantAdapter()`; remove `AppCompanyId` filter from all queries |
| `APP.BL/Infrastructure/AppCacheManager.cs` | Remove `PrefetchPathAppBusinessPartner` from invite-user query; cache only returns `EmInvitedUserType` |
| `APP.BL/MasterAuth/AppSaasAccountUserBL.cs` | `RetrieveOneAppBusinessPartnerEntity` called in §4.1 must use tenant adapter after the move |
| `PlmApplication/Migrations/V005__AddAppBusinessPartner.sql` | New migration — create table in tenant DB schema |

### 3.3 GetAccessibleCompanies Query

For the multi-company login picker, enumerate all companies a user can access:

```sql
-- Home company
SELECT c.CompanyId, c.ShortName, c.CompanyDomainIdentityToken, 'Home' AS AccessType
FROM AppCompany c
WHERE c.CompanyId = @homeCompanyId

UNION

-- Invited companies (cross-company)
SELECT DISTINCT c.CompanyId, c.ShortName, c.CompanyDomainIdentityToken, 'Partner' AS AccessType
FROM AppBusinessPartnerInviteUser inv
JOIN AppCompany c ON c.CompanyId = inv.AppCreatedByCompanyId
WHERE inv.UserId = @userId
  AND inv.EmInvitedUserType IS NOT NULL
```

---

## 4. Backend Implementation

### 4.1 Fix: AppSaasAccountUserBL.cs — RegisterInvitingUserAndCompnay (~line 646)

**Duplicate check** — add `UserId`, switch to `AppCreatedByCompanyId`:

```csharp
// BEFORE (line 653–654)
var existEntity = inviteUserEntityList.FirstOrDefault(o =>
    o.AppCompanyId.HasValue && o.AppCompanyId.Value == invitingCompanyIdFromEmail
    && o.AppBusinessPartnerId.HasValue && o.AppBusinessPartnerId.Value == businessPartnerId.Value);

// AFTER
var existEntity = inviteUserEntityList.FirstOrDefault(o =>
    o.AppCreatedByCompanyId.HasValue && o.AppCreatedByCompanyId.Value == invitingCompanyIdFromEmail
    && o.AppBusinessPartnerId.HasValue && o.AppBusinessPartnerId.Value == businessPartnerId.Value
    && o.UserId.HasValue && o.UserId.Value == newUserId);
```

**Save new invite record** — set `AppCreatedByCompanyId` and populate `EmInvitedUserType`:

```csharp
// BEFORE (line 657–660)
appBusinessPartnerInviteUserExDto.AppBusinessPartnerId = businessPartnerId.Value;
appBusinessPartnerInviteUserExDto.UserId               = newUserId;
appBusinessPartnerInviteUserExDto.AppCompanyId         = invitingCompanyIdFromEmail;

// AFTER
var partnerEntity = AppBusinessPartnerBL.RetrieveOneAppBusinessPartnerEntity(businessPartnerId.Value);
appBusinessPartnerInviteUserExDto.AppBusinessPartnerId  = businessPartnerId.Value;
appBusinessPartnerInviteUserExDto.UserId                = newUserId;
appBusinessPartnerInviteUserExDto.AppCreatedByCompanyId = invitingCompanyIdFromEmail;
appBusinessPartnerInviteUserExDto.AppCompanyId          = invitingCompanyIdFromEmail; // backward compat
appBusinessPartnerInviteUserExDto.EmInvitedUserType     = partnerEntity?.PartnerType;
```

**Add GetAccessibleCompaniesForUser** (new method in same file):

```csharp
public static List<AppCompanyDto> GetAccessibleCompaniesForUser(int userId)
{
    var result = new List<AppCompanyDto>();
    using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
    {
        // Home company
        var userEntity = AppCacheManagerBL.GetCurrentUserEntityFromMasterDataSource(userId);
        if (userEntity?.MyOwnCompnanyId.HasValue == true)
        {
            var homeCompany = AppCompanyBL.RetrieveOneAppCompanyDto(userEntity.MyOwnCompnanyId.Value);
            if (homeCompany != null) result.Add(homeCompany);
        }

        // Invited companies
        var invites = new EntityCollection<AppBusinessPartnerInviteUserEntity>();
        adapter.FetchEntityCollection(invites,
            new RelationPredicateBucket(
                AppBusinessPartnerInviteUserFields.UserId == userId
                & AppBusinessPartnerInviteUserFields.EmInvitedUserType != DBNull.Value));

        var companyIds = invites
            .Where(i => i.AppCreatedByCompanyId.HasValue)
            .Select(i => i.AppCreatedByCompanyId.Value)
            .Distinct()
            .Where(id => result.All(c => (int)c.Id != id));

        foreach (var cid in companyIds)
        {
            var company = AppCompanyBL.RetrieveOneAppCompanyDto(cid);
            if (company != null) result.Add(company);
        }
    }
    return result;
}
```

### 4.2 Fix: AppCacheManager.cs — GetCurrentUserWorkingCompanyBusinessPartner

**Change query filter** from `AppCompanyId` to `AppCreatedByCompanyId` (~line 378):

```csharp
// BEFORE
AppBusinessPartnerInviteUserFields.AppCompanyId == workingCompanyId

// AFTER
AppBusinessPartnerInviteUserFields.AppCreatedByCompanyId == workingCompanyId
```

**Add parallel EmInvitedUserType cache** (~line 100):

```csharp
private static readonly ConcurrentDictionary<string, CacheEntry<int?>> _dictInviteUserType
    = new ConcurrentDictionary<string, CacheEntry<int?>>();
```

**Populate it inside GetCurrentUserWorkingCompanyBusinessPartner** (after the invite entity is fetched, ~line 384):

```csharp
// After: var partnerInviteUserEntity = partnerInviteUserList.FirstOrDefault();
_dictInviteUserType[cacheKey] = new CacheEntry<int?>(
    partnerInviteUserEntity?.EmInvitedUserType, DefaultCacheTtl);
```

**Add new accessor method** (near line 1047):

```csharp
internal static int? GetInviteUserType(int workingCompanyId, int userId)
{
    string cacheKey = $"{workingCompanyId}_{userId}";
    if (_dictInviteUserType.TryGetValue(cacheKey, out var entry) && !entry.IsExpired)
        return entry.Value;
    // warm both caches in one DB call
    GetCurrentUserWorkingCompanyBusinessPartner(workingCompanyId, userId);
    return _dictInviteUserType.TryGetValue(cacheKey, out entry) ? entry.Value : null;
}
```

**Add to cleanup/clear blocks** (~lines 1147 and 1189):

```csharp
CleanupCache(_dictInviteUserType);   // in periodic cleanup
_dictInviteUserType.Clear();          // in full cache flush
```

### 4.3 Fix: AppSaasUserSessionMgtBL.cs — SetupBusinessParterUserType (lines 321–338)

Check per-user `EmInvitedUserType` first, fall back to company-level `PartnerType`:

```csharp
private static void SetupBusinessParterUserType(ref AppClientIdentity client,
    int? currentWorkingCompanyId, int loginUserId)
{
    // 1. Per-user explicit role (most specific)
    int? invitedUserType = AppCacheManagerBL.GetInviteUserType(currentWorkingCompanyId.Value, loginUserId);
    if (invitedUserType.HasValue)
    {
        client.CurrentLoginUserType = invitedUserType.Value;
        var partner = AppCacheManagerBL.GetCurrentUserWorkingCompanyBusinessPartner(
                          currentWorkingCompanyId.Value, loginUserId);
        if (partner != null)
            client.CurrentPartnerId = partner.AppBusinessPartnerId;
        return;
    }

    // 2. Fall back to company-level PartnerType (existing data / backward compat)
    AppBusinessPartnerEntity aAppBusinessPartnerEntity =
        AppCacheManagerBL.GetCurrentUserWorkingCompanyBusinessPartner(
            currentWorkingCompanyId.Value, loginUserId);

    if (aAppBusinessPartnerEntity != null)
    {
        client.CurrentLoginUserType = aAppBusinessPartnerEntity.PartnerType.Value;
        client.CurrentPartnerId     = aAppBusinessPartnerEntity.AppBusinessPartnerId;
    }
    else
    {
        client.CurrentLoginUserType = (int)EmAppUserType.Unknow;
    }
}
```

### 4.4 Tenant Resolution vs Company Picker — Decision Logic

The company picker is only triggered when the URL cannot resolve a specific tenant. The two flows are mutually exclusive:

```
User arrives at login page
  │
  ├─ URL has tenant subdomain / custom domain?
  │   e.g. acme.appai.com  OR  app.acme.com
  │    │
  │    ├─ TenantController resolves CompanyId from Host header (existing §4.2 of Architecture_MultiTenant.md)
  │    │   → tenantBranding.isFound = true, companyId known BEFORE login
  │    │   → MgtLogin receives companyId in request body
  │    │   → NO company picker — user logs directly into that tenant
  │    │   → If user has no access to that company → 401 Unauthorized
  │    │
  │    └─ Works today (Phases 1–4 already implemented)
  │
  └─ URL has NO tenant subdomain?
      e.g. localhost  OR  app.appai.com  (shared platform URL)
       │
       ├─ tenantBranding.isFound = false, no companyId from URL
       ├─ MgtLogin validates credentials only (no company context)
       ├─ companies.Count == 1 → auto-select → no picker shown
       └─ companies.Count > 1 → show CompanyPicker → user selects → SelectCompany call
```

**MgtLogin request contract change** — accept optional `companyId` from the React branding state:

```csharp
public class MgtLoginDto
{
    public string LoginName  { get; set; }
    public string Password   { get; set; }
    public int?   CompanyId  { get; set; }  // set by React when tenantBranding.isFound = true
}
```

When `CompanyId` is provided (subdomain flow):
- Validate credentials as normal
- Validate user has access to `CompanyId` → if not, return 403
- Activate session for that `CompanyId` immediately — skip the picker

When `CompanyId` is null (shared-URL flow):
- Validate credentials
- Enumerate accessible companies → picker if > 1

**React: pass companyId when tenant is known** (in `Login.tsx`):

```tsx
// tenantBranding is already in Redux state (loaded on app mount)
const { companyId } = useSelector(selectTenantBranding);

adminSvc.MgtLogin({
    loginName,
    password,
    companyId: companyId ?? undefined,   // undefined = shared-URL flow
});
```

> **Subdomain resolution is already fully implemented** in `TenantController.Info()`, `AppDataSourceRegisterBL.GetTenantInfoByHost()`, nginx config, and `tenantBrandingSlice.ts`. No changes needed to that layer.

---

### 4.6 New: Multi-Company Login — HomeController.cs

**Phase 1 — Credential validation** (modify existing `MgtLogin`):

```csharp
// After credential validation succeeds, before returning session:
var companies = AppSaasAccountUserBL.GetAccessibleCompaniesForUser(userId);
if (companies.Count > 1)
{
    // Create session with null CompanyId — not yet fully activated
    string sessionId = CreatePendingSession(userId);
    return Ok(new {
        requiresCompanySelection = true,
        sessionId                = sessionId,
        companies                = companies.Select(c => new {
            companyId   = c.Id,
            shortName   = c.ShortName,
            domainToken = c.CompanyDomainIdentityToken
        })
    });
}
// Single company — activate session immediately (existing flow)
```

**Phase 2 — Company selection** (new endpoint):

```csharp
[HttpPost]
[AllowAnonymous]
public IHttpActionResult SelectCompany(SelectCompanyDto dto)
{
    // Validate the pending session belongs to this request
    var session = AppSecurityUserSessionBL.GetSessionEntityBySessionID(dto.SessionId);
    if (session == null || session.AppCreatedByCompanyId != null)
        return Unauthorized();

    // Validate user has access to the requested company
    var accessible = AppSaasAccountUserBL.GetAccessibleCompaniesForUser(session.UserId);
    if (accessible.All(c => (int)c.Id != dto.CompanyId))
        return Unauthorized();

    // Activate session for the selected company
    AppSaasUserSessionMgtBL.UpdateCurrentUserSessionCompnayIdAfterPassAuthentication(
        dto.SessionId, dto.CompanyId);

    return Ok(new { success = true });
}
```

---

## 5. Frontend Implementation (React)

### 5.1 Login Flow with Company Picker

```
Login form submit
  ↓
POST /webapi/Home/MgtLogin
  ↓
response.requiresCompanySelection == true?
  ├─ YES → render <CompanyPicker companies={response.companies} sessionId={response.sessionId} />
  │          → user selects company
  │          → POST /webapi/Home/SelectCompany { sessionId, companyId }
  │          → on success → dispatch(retrieveUserTreeMenu())  ← existing flow
  │
  └─ NO  → dispatch(retrieveUserTreeMenu())  ← existing single-company flow (unchanged)
```

### 5.2 CompanyPicker Component

New file: `PlmApplication/AppReact/src/components/admin/CompanyPicker.tsx`

```tsx
interface CompanyOption {
  companyId: number;
  shortName: string;
  domainToken: string;
}

interface CompanyPickerProps {
  companies: CompanyOption[];
  sessionId: string;
  onSelected: () => void;
}
```

- Renders as a modal or full-screen step between credential entry and session activation
- Calls `POST /webapi/Home/SelectCompany` then calls `onSelected()` to complete the login flow
- Styled with existing `theme.button_default` and `theme.inputBox` tokens

### 5.3 New adminSvc method

In `PlmApplication/AppReact/src/webapi/adminsvc.ts`:

```typescript
SelectCompany(sessionId: string, companyId: number): Promise<any> {
    return axiosInstance.post(`${BASE_URL}/webapi/Home/SelectCompany`, {
        sessionId,
        companyId,
    });
}
```

---

## 6. Session Identity Flow (After Fix)

```
User logs in to Company B (cross-company — different from home company)
  │
  ├─ RegisterUserIdentityTotheSystem()
  │    └─ ClassifyCurrentLoginUserType()
  │         ├─ DomainId == SysAdmin? → SysAdmin fast path
  │         ├─ MyOwnCompnanyId == currentWorkingCompanyId?
  │         │    YES → home company branch
  │         │         use AppSecurityUser.DomainId  (unchanged, correct)
  │         │    NO  → SetupBusinessParterUserType()
  │         │              ① GetInviteUserType(companyB, userId)
  │         │                   → AppBusinessPartnerInviteUser.EmInvitedUserType = 4 (Supplier)
  │         │                   → client.CurrentLoginUserType = 4  ✓
  │         │              ② if no EmInvitedUserType → AppBusinessPartner.PartnerType (fallback)
  │         │              ③ if no partner at all → Unknown
  └─ AppSecurityUser.DomainId = 6 (SaasCompanyAdmin) — untouched ✓
```

---

## 7. No-Change Items

| Item | Status |
|---|---|
| `EmInvitedUserType` DB column | Already exists in `AppBusinessPartnerInviteUser` |
| `AppBusinessPartnerInviteUserEntity.EmInvitedUserType` | Already mapped (field index 9) |
| `AppBusinessPartnerInviteUserDto.EmInvitedUserType` | Already declared and registered |
| `AppBusinessPartnerInviteUserConverter` | Already maps EmInvitedUserType in both directions |
| DB migration | Not needed for §4.1–4.3 |
| `UpdateCurrentUserSessionCompnayIdAfterPassAuthentication` | Already exists in AppSaasUserSessionMgtBL.cs (line 36) |

---

## 8. Key Invariant

> **`AppSecurityUser.DomainId` is the home domain — set once at account creation, never modified by invitation or cross-company flows.**
>
> Cross-company roles live exclusively in `AppBusinessPartnerInviteUser.EmInvitedUserType`.

---

## 9. Verification

### Domain Conflict Fix

1. **Invite a `SaasCompanyAdmin`** (Company A) into Company B as Supplier:
   - `AppBusinessPartnerInviteUser` row: `AppCreatedByCompanyId = CompanyB.Id`, `EmInvitedUserType = 4`
   - `AppSecurityUser.DomainId` unchanged (still 6)

2. **Log in as that user to Company A** (home):
   - `ClassifyCurrentLoginUserType` → home company branch → `CurrentLoginUserType = 6 (SaasCompanyAdmin)` ✓

3. **Log in as that user to Company B** (cross-company):
   - `SetupBusinessParterUserType` → `GetInviteUserType` returns 4 → `CurrentLoginUserType = 4 (Supplier)` ✓

4. **Duplicate invite guard**: invite same user again → `existEntity` found → no duplicate row ✓

### Multi-Company Login (Shared URL)

5. **Single-company user (shared URL)**: login returns `requiresCompanySelection = false` → existing flow unchanged ✓

6. **Multi-company user (shared URL)**: login returns `requiresCompanySelection = true` + company list → CompanyPicker shown ✓

7. **Select Company B**: `POST /webapi/Home/SelectCompany` → session activated for Company B → user enters as Supplier ✓

8. **Invalid company selection**: user attempts to select a company they have no invite for → 401 Unauthorized ✓

### Subdomain Flow (no change to existing logic)

9. **User at `acme.appai.com`**: React sends `companyId` in login body → `MgtLogin` validates access and activates session for that company → CompanyPicker never shown ✓

10. **User at `acme.appai.com` with no access to ACME**: `MgtLogin` returns 403 Forbidden ✓

11. **`tenantBranding.isFound = false` (localhost / dev)**: `companyId` is null in login body → shared-URL flow applies ✓

---

## 10. Files to Change

| File | Change |
|---|---|
**Phase A — Domain conflict fix + shared-URL login (unblocks AppBusinessPartner move)**

| File | Change |
|---|---|
| `APP.BL/MasterAuth/AppSaasAccountUserBL.cs` | Fix duplicate check; fix save (`AppCreatedByCompanyId` + `EmInvitedUserType`); add `GetAccessibleCompaniesForUser` |
| `APP.BL/Infrastructure/AppCacheManager.cs` | Fix query filter; add `_dictInviteUserType` cache; add `GetInviteUserType`; remove `PrefetchPathAppBusinessPartner` (Step 4 of §3.2) |
| `APP.BL/MasterAdmin/AppSaasUserSessionMgtBL.cs` | Update `SetupBusinessParterUserType` to check `EmInvitedUserType` first |
| `PlmApplication/Server/WebApi/HomeController.cs` | Modify `MgtLogin` to accept optional `companyId`; return company list when > 1; add `SelectCompany` endpoint |
| `PlmApplication/AppReact/src/webapi/adminsvc.ts` | Add `SelectCompany` method; pass `companyId` from branding state in login call |
| `PlmApplication/AppReact/src/components/admin/CompanyPicker.tsx` | New component |
| `PlmApplication/AppReact/src/components/admin/Login.tsx` | Wire `CompanyPicker` into login flow |

**Phase B — AppBusinessPartner move to Tenant DB (requires Phase A complete)**

| File | Change |
|---|---|
| `PlmApplication/Migrations/V005__AddAppBusinessPartner.sql` | New migration — create `AppBusinessPartner` table in tenant DB schema |
| `APP.BL/TenantBusiness/AppComBusinessPartnerBL.cs` | Replace `MasterConnStr` with `GetTenantAdapter()`; remove `AppCompanyId` filter |
| `APP.BL/MasterAuth/AppSaasAccountUserBL.cs` | Update partner entity fetch to use tenant adapter |
| Data migration script | Per-tenant `INSERT … SELECT` from `AppMasterDB.AppBusinessPartner WHERE AppCompanyID = @companyId` |
| `AppMasterDB.AppBusinessPartner` | Rename to `_MasterArchive` after verification; drop after stable period |
