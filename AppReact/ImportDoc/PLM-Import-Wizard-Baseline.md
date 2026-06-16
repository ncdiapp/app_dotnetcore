# PLM Data Import Wizard — Baseline Plan

> **Status:** Living document — update this file whenever requirements or design decisions change.  
> **Purpose:** Single source of truth for implementing the PLM → App-Builder migration wizard.  
> **Related:** [PLM Migration Plan.md](./PLM%20Migration%20Plan.md) (feature scope), SQL scripts in this folder.  
> **PLM reference solution:** `C:\Dev\PLM3\PLM`  
> **Last updated:** 2026-06-16 (Q1–Q10 resolved)

---

## 1. Objective

Add a **PLM Data Import** wizard to the React **Database Design** page (`DatabaseDesignManagement.tsx`). Users migrate legacy PLM data into App-Builder step by step, guided by scripts and logic in `AppReact/ImportDoc/`.

**UI entry:** Left sidebar button **「PLM Data Import」** → right panel loads `PLMDataImport` child component (same layout/theme as other Database Design sections).

---

## 2. Scope Overview

| Wizard step (initial) | Migration plan section | Status |
|----------------------|------------------------|--------|
| Connect & Discover | §1 Database | Specified |
| Entity Data Source | §2 Data Source Management | Specified |
| Template Import | §6 Reference Management (structure) | Framework only |
| Other Data | §3–8 (Color, POM, File, Search, Security…) | Placeholder |

**Out of scope (confirmed):**

- PLM Enum entity import (`SqlReferenceSpecs/ImportPlmEnumEntitiesToAppEntityInfo.sql` — do not use)
- RelationFK entities (`EntityType = 5`)
- Excel/CSV export of preview results

---

## 3. Confirmed Decisions

### 3.1 Environment & connections

| # | Decision |
|---|----------|
| A1 | PLM and APP **may be on different SQL Server instances** — must support cross-instance from day one |
| A2 | PLM data sources live in **`pdmDataSource`** (`SELECT * FROM pdmDataSource`). Reference: `PdmDataSourceBL`, `EmDataSourceFrom` in PLM solution |
| A3 | User enters **PLM connection string once** in a UI input (not persisted to `AppDataSourceRegister`) |
| A4 | **Tenant DB**, **Master DB**, **`SaasApplicationID`**, **`CompanyId`** are resolved automatically from the logged-in session |

**PLM `EmDataSourceFrom` (relevant values):**

| Value | Name | Notes |
|-------|------|-------|
| 1 | PLM | Empty connection in `pdmDataSource` falls back to main PDM connection (`PdmDataSourceBL.GetConnectionInfoWithCode`) |
| 2 | ERP | Register as external `AppDataSourceRegister` |
| 3 | DataWS | Register as external `AppDataSourceRegister` |
| 4 | OtherEx | Register as external `AppDataSourceRegister` |
| 5, 6 | RestJson, RestXML | Ignore in this wizard phase |

### 3.2 Data source registration (Step: Connect & Discover)

| # | Decision |
|---|----------|
| B5 | External DBs (ERP, DataWS, OtherEx): **register in MasterDB** as `AppDataSourceRegister` |
| B5a | Before insert: **validate connection string uniqueness by company** |
| B5b | Same normalized connection + **same `CompanyId`** → reuse existing register, continue |
| B5c | Same normalized connection + **different `CompanyId`** → **block** with warning (connection string is company-locked; prevents cross-tenant data/permission conflicts) |
| B6 | Multiple PLM `DataSourceFrom` values **may map to the same** APP `DataSourceRegister.Id` |
| B7 | Naming for **new** external registers: `{TenantDatabaseName}_ERP`, `{TenantDatabaseName}_DataWS`, `{TenantDatabaseName}_OtherEx` |
| B8 | **PLM (1):** **No** new register. Map to **Company Master DB** register (`IsCompanyMasterDb = true`). APP does **not** keep a long-term link to original PLM DB — PLM-sourced data is imported into **Tenant DB** |
| B9 | Step 1: `pdmDataSource` `DataSourceFrom=1` is used for **discover/validate only**; no new `AppDataSourceRegister` row |
| B10 | `DataSourceFrom=1` empty `ConnectionString` → treat as main PLM connection (unlikely in practice) |

**Connection string comparison:** Normalize via `SqlConnectionStringBuilder` (Server, Database, auth) before compare. APP stores encrypted strings (`AppConnectionStringEncryptionBL`).

### 3.3 Entity import (Step: Entity Data Source)

| # | Decision |
|---|----------|
| C8 | **No** enum import |
| C9 | **No** RelationFK import |
| C10 | Re-import: **Update** by **`IntegrationId` only** (= PLM `EntityID`) |
| C10a | UserDefine row data: update matched by `IntegrationId` on entity header (same key scope as SQL spec) |
| C11 | Physical table prefix for UserDefine wide tables: **configurable**, default `Plm_entity_` |
| C12 | **Integrate** table export for SystemDefine PLM-sourced tables |
| C12a | Fix `SqlReferenceSpecs/ExportSourceDbTablesToNewDatabase.sql` issue: **`SELECT * INTO` does not preserve PRIMARY KEY** — implement in C# with explicit PK |
| C12b | SystemDefine entity `DataSourceFrom = PLM (1)` → copy **only tables referenced by `pdmEntity`** (SystemDefine, `DataSourceFrom=1`) into Tenant DB |
| C12c | SystemDefine entity `DataSourceFrom = ERP / DataWS / OtherEx` → **reference external DB only** (no table/data copy) |
| C13 | After import: **prompt user to recycle IIS** / refresh schema cache (no auto-recycle) |
| C25 | Execute failures: **full transaction rollback** per batch |

**Import tabs in this step:**

1. **User Define** — port `SqlReferenceSpecs/ImportPlmUserDefineEntitiesToAppEntityInfo.sql` to C#
2. **System Define** — port `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql` to C#

**User Define behavior (from SQL spec):**

- ≤ 2 columns → `EmAppEntityType.SimpleValueList` + `AppEntitySimpleListValue`
- \> 2 columns → `EmAppEntityType.SystemDefineTable` + `CREATE TABLE` in Tenant DB + EAV pivot data
- `IntegrationId` = PLM `EntityID`
- `DataSourceFrom` — **Company Master DB** register Id (same for UserDefine and SystemDefine PLM entities)

**System Define behavior (from SQL spec):**

- Metadata only in `AppEntityInfo` (no row copy for external DB entities)
- `DataSourceFrom` mapped via PLM `DataSourceFrom` 1–4 → APP register IDs
- Physical table must exist in target DB before insert
- NULL / 5 / 6 / other `DataSourceFrom` → skip with preview warning

### 3.4 Template import (Step: Template)

| # | Decision |
|---|----------|
| D14 | Reference PLM solution for template/tab/block/grid logic; **fully customer-customized** per deployment |
| D15 | **One PLM Template → one APP Transaction Group** |
| D16 | Do **not** skip special blocks/grids — import as **ordinary** block/grid first; special logic later |
| D17 | Bind imports to current **`SaasApplicationID`** |

**Mapping (initial):**

| PLM | APP |
|-----|-----|
| `pdmTemplate` | Transaction Group (`TemplateHeader`) |
| Tab | Transaction (child) |
| Block | Unit |
| SubItem / Grid column | Field |

**Template `IntegrationId` (re-import):**

| APP object | `IntegrationId` stores |
|------------|------------------------|
| Transaction Group | PLM `TemplateId` |
| Transaction | PLM `TabId` |
| Unit | PLM `BlockId` |
| Field | PLM `ColumnId` (grid column or subitem column id) |

Re-import updates by `IntegrationId` on each level.

### 3.5 Wizard product rules

| # | Decision |
|---|----------|
| E18 | Step **「Other Data」** placeholder; architecture supports **insert / reorder / append** steps |
| E19 | **Server-side session** in **Tenant DB** table `AppPlmImportSession` |
| E19a | On wizard start: detect resumable session → user chooses **discard & start fresh** or **continue** |
| E19b | Session tracks per-step completion. If **all steps fully succeeded**, session is **not** offered for resume (delete or mark completed) |
| E19c | Only **in-progress** (partial) sessions are resumable |
| E20 | **Tenant Admin** only: `EmAppUserType.SaasCompanyAdmin` via `ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType` |
| E21 | No Angular legacy UI — **new feature** |
| E22 | One wizard run = **one tenant, one application** |
| E23 | PLM connection string stored **encrypted in session**; on resume **pre-filled**, user may edit or keep |
| F23 | Cross-instance required in MVP |
| F24 | No Excel/CSV export for preview |
| F25 | Full rollback on execute failure |
| F26 | No standard network/firewall assumption; UI shows **connection failure** details when PLM SQL is unreachable |

---

## 4. Architecture

### 4.1 Frontend (`AppReact/src/components/dbmgt/`)

```
DatabaseDesignManagement.tsx     ← add section「PLM Data Import」
PlmDataImportManagement.tsx      ← section entry (like DbToDbImportManagement)
plmImport/
├── PlmImportWizard.tsx          ← step state machine
├── plmImportStepRegistry.ts     ← extensible step definitions (order, insert, reorder)
├── steps/
│   ├── StepConnection.tsx       ← Connect & Discover (merged step 1+2)
│   ├── StepEntityImport.tsx     ← UserDefine + SystemDefine tabs
│   ├── StepTemplateImport.tsx   ← framework + basic mapping
│   └── StepOtherData.tsx        ← placeholder
└── types.ts
```

**UI conventions:** Match existing Database Design sections — `theme.*`, Wijmo grids for preview, `useTabDataAutoCache` for UI state, URL `param1=PlmDataImportManagement`.

**Step registry pattern (extensibility):**

```typescript
type PlmImportStepDef = {
  id: string;
  label: string;
  order: number;
  component: React.FC;
  canEnter: (session) => boolean;
  isComplete: (session) => boolean;
};
```

### 4.2 Backend — C# module layout (`APP.BL/DataMigration/PlmMigration/`)

> **Status:** Planned locations only — **files not created yet**. Create when Phase 0 starts; register each `.cs` in `APP.BL.csproj`.

**Layering rule (same as rest of APP):**

| Layer | Location | Responsibility |
|-------|----------|----------------|
| **Controller** | `AppAI.Web/Controllers/PlmMigrationController.cs` | HTTP, auth gate, DTO in/out — **no business logic, no `DatabaseFixture`** |
| **BL** | `APP.BL/DataMigration/PlmMigration/PlmMigrationBL.*.cs` | All logic — **one class, three partial files** |
| **DTO** | `APP.Components.Dto/UserDefine/PlmMigration/` | Request/response/session DTOs |

Controller calls **`PlmMigrationBL`** only. `AppCacheManagerBL.GetOneDatabaseFixture` is **internal** to APP.BL — never call from controller.

**Single class, three files (`partial class PlmMigrationBL`):**

All files share the same class name and namespace. Split by wizard phase, not by type name.

```
APP.BL/DataMigration/PlmMigration/
├── PlmMigrationBL.Connection.cs    ← partial — Step 1: auth, session, PLM connect, pdmDataSource, register
├── PlmMigrationBL.Entity.cs        ← partial — Step 2: table export, UserDefine, SystemDefine
└── PlmMigrationBL.Template.cs      ← partial — Step 3: template mapping import

AppAI.Web/Controllers/
└── PlmMigrationController.cs       ← extends SecureBaseController

APP.Components.Dto/UserDefine/PlmMigration/
├── PlmImportSessionDto.cs
├── PlmConnectionTestRequestDto.cs
├── PlmDiscoverDataSourcesResultDto.cs
├── PlmEntityImportPreviewDto.cs
└── …

AppReact/src/webapi/
└── plmMigrationSvc.ts
```

**Namespace:** `APP.BL.DataMigration.PlmMigration`

**Skeleton (each file):**

```csharp
namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        // methods for this slice only
    }
}
```

| File | Methods (planned) |
|------|-------------------|
| **`PlmMigrationBL.Connection.cs`** | `RequireTenantAdmin()`, session CRUD (`AppPlmImportSession`), resume vs completed, `TestPlmConnection`, `DiscoverPlmDataSources`, register ERP/DataWS/OtherEx + company lock, map PLM(1) → Company Master register |
| **`PlmMigrationBL.Entity.cs`** | `ExportPlmTablesToTenant` (PK preserved), `PreviewUserDefineEntityImport` / `Execute…`, `PreviewSystemDefineEntityImport` / `Execute…` |
| **`PlmMigrationBL.Template.cs`** | `PreviewTemplateMapping`, `ExecuteTemplateImport` |

**`APP.BL.csproj` entries:**

```xml
<Compile Include="DataMigration\PlmMigration\PlmMigrationBL.Connection.cs" />
<Compile Include="DataMigration\PlmMigration\PlmMigrationBL.Entity.cs" />
<Compile Include="DataMigration\PlmMigration\PlmMigrationBL.Template.cs" />
```

**Tenant DB table (new migration):** `AppPlmImportSession` — session id, company/app ids, encrypted PLM conn, step status JSON, `SessionStatus` (`InProgress` | `Completed`), timestamps.

**Controller:** `PlmMigrationController` — thin; each action: `PlmMigrationBL.RequireTenantAdmin()` then delegate to `PlmMigrationBL` method.

**SQL scripts:** `ImportDoc/SqlReferenceSpecs/` — **reference spec** for C# ports; runtime is **C# only**.

**Data access:** Direct `SqlConnection` / `DatabaseFixture` for PLM (arbitrary connection string); `AppCacheManagerBL.GetOneDatabaseFixture` for APP tenant/master DBs inside `PlmMigrationBL` only.

### 4.3 API endpoints (planned)

| Endpoint | Purpose |
|----------|---------|
| `POST TestPlmConnection` | Validate PLM connection string |
| `POST DiscoverPlmDataSources` | Read `pdmDataSource` 1–4; register/reuse APP registers |
| `GET ImportSession/active` | Get resumable session for current tenant/app (if any) |
| `POST ImportSession` | Create or update session |
| `POST ImportSession/discard` | User chose to clear previous session |
| `POST PreviewUserDefineEntityImport` | Preview UserDefine |
| `POST ExecuteUserDefineEntityImport` | Execute UserDefine |
| `POST PreviewSystemDefineEntityImport` | Preview SystemDefine (incl. table export plan) |
| `POST ExecuteSystemDefineEntityImport` | Execute SystemDefine |
| `POST PreviewTemplateMapping` | Template mapping preview |
| `POST ExecuteTemplateImport` | Template import |

---

## 5. Wizard flow (current)

```
┌─────────────────────────────────────────────────────────────────┐
│  Step 1: Connect & Discover                                      │
│  • Input PLM connection string → test                            │
│  • Read pdmDataSource (1–4)                                      │
│  • Auto-resolve Tenant / Master / AppId / CompanyId              │
│  • Register ERP/DataWS/OtherEx (company lock)                    │
│  • Map PLM(1) → Company Master DB register                       │
└────────────────────────────┬────────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 2: Entity Data Source                                      │
│  Tab A: User Define  — preview → execute (update by IntegrationId)│
│  Tab B: System Define — export PLM tables to tenant if DSF=1     │
│                       — preview → execute                         │
└────────────────────────────┬────────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 3: Template Import                                         │
│  • 1 Template → 1 Transaction Group                              │
│  • Ordinary block/grid mapping first                             │
│  • Preview → execute (update by IntegrationId on Group/Transaction/Unit/Field)│
└────────────────────────────┬────────────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│  Step 4: Other Data (placeholder)                                │
│  • Future: Color, POM, File, Search, Security…                 │
└─────────────────────────────────────────────────────────────────┘
```

Each execute step: **single transaction, full rollback on error**.

---

## 6. Implementation phases

| Phase | Deliverable | Est. |
|-------|-------------|------|
| **0** | Sidebar entry + Wizard shell + step registry + session table/API stub | 2–3 days |
| **1** | Connect & Discover + DataSource register + company lock | 3–4 days |
| **2** | `PlmTableExportBL` (PK-correct copy PLM → Tenant) | 3–5 days |
| **3** | SystemDefine preview/execute + update | ~1 week |
| **4** | UserDefine preview/execute (cross-instance EAV) | 1–2 weeks |
| **5** | Template framework + basic mapping | 1–2 weeks |
| **6** | Other Data placeholder + docs | 1 day |

---

## 7. Resolved questions (2026-06-16)

| ID | Answer |
|----|--------|
| **Q1** | No long-term PLM DB link. `DataSourceFrom=1` SystemDefine → `AppEntityInfo.DataSourceFrom` = Company Master register. Step 1: `pdmDataSource` DSF=1 discover/validate only, no new register |
| **Q2** | Empty DSF=1 connection → main PLM connection (edge case) |
| **Q3** | **(A)** Export only tables referenced by `pdmEntity` SystemDefine with `DataSourceFrom=1` |
| **Q4** | **(A)** Update match **`IntegrationId` only** |
| **Q5** | UserDefine `DataSourceFrom` = Company Master register Id — **yes** |
| **Q6** | Session in **Tenant DB**. On start: offer resume or discard. Completed wizard → no resume (clean up session) |
| **Q7** | Connection **pre-filled** from session (encrypted); user may change |
| **Q8** | `IntegrationId` on Group / Transaction / Unit / Field = PLM TemplateId / TabId / BlockId / ColumnId |
| **Q9** | **`EmAppUserType.SaasCompanyAdmin`** (`CurrentLoginUserType`) |
| **Q10** | No network assumption; UI shows connection failure |

---

## 8. Reference files

| File | Role |
|------|------|
| `SqlReferenceSpecs/ImportPlmUserDefineEntitiesToAppEntityInfo.sql` | UserDefine import spec |
| `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql` | SystemDefine import spec |
| `SqlReferenceSpecs/ExportSourceDbTablesToNewDatabase.sql` | Table list reference; **do not use as-is** (PK issue) |
| `SqlReferenceSpecs/ImportPlmEnumEntitiesToAppEntityInfo.sql` | **Not used** |
| `PLM Migration Plan.md` | High-level feature migration roadmap |

**PLM solution key files:**

- `Com.Visual2000.BL\System\PdmDataSourceBL.cs`
- `V2K.PLM.Components.Dto\Enums.cs` — `EmDataSourceFrom`
- `Com.Visual2000.BL\TabBlock\PdmTemplateBL.cs` — template structure

---

## 9. Change log

| Date | Author | Change |
|------|--------|--------|
| 2026-06-16 | — | Initial baseline from design discussion + user Q&A (sections A–F) |
| 2026-06-16 | — | `PLM Migration Plan.md` updated with business rules + wizard mapping; cross-linked from plan |
| 2026-06-16 | — | SQL scripts moved to `SqlReferenceSpecs/`; doc paths updated |
| 2026-06-16 | — | Q1–Q10 resolved; C# module layout documented (not yet implemented) |
| 2026-06-16 | — | BL layout: `DataMigration/PlmMigration/`, 3 files, `partial class PlmMigrationBL` |
