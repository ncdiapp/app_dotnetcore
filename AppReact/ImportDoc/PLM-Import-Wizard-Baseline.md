# PLM Data Import Wizard — Baseline Plan

> **Status:** Living document — update when requirements or design change.  
> **Purpose:** Single source of truth for **how** to build the wizard (API, BL, DB, UI).  
> **Feature scope:** [PLM Migration Plan.md](./PLM%20Migration%20Plan.md)  
> **SQL import specs:** [SqlReferenceSpecs/](./SqlReferenceSpecs/)  
> **PLM reference:** `C:\Dev\PLM3\PLM`  
> **Last updated:** 2026-06-16

---

## Document map

| Section | Read when you need… |
|---------|-------------------|
| **[§0 Quick reference](#0-quick-reference)** | One-page summary of wizard flow and rules |
| **[§1 Objective](#1-objective)** | What we are building |
| **[§2 Scope](#2-scope-overview)** | In / out of scope |
| **[§3 Decisions](#3-confirmed-decisions)** | All confirmed business rules (A–F) |
| **[§4 Architecture](#4-architecture)** | React, C#, API, DB, logs |
| **[§5 Wizard flow](#5-wizard-flow)** | Step-by-step diagram |
| **[§6 Phases](#6-implementation-phases)** | Delivery order and estimates |
| **[§7 Q&A](#7-questions)** | Resolved and open questions |
| **[§8 References](#8-reference-files)** | Files and PLM solution pointers |

---

## 0. Quick reference

**Entry:** Database Design → sidebar **PLM Data Import** → `PlmDataImportManagement.tsx`

**Who can run:** `SaasCompanyAdmin` or `SysAdmin` (SysAdmin must pick **target Company** in Step 1).

**Wizard steps:**

| Step | What happens |
|------|----------------|
| **1 Connect & Discover** | Pick **Application** + PLM connection → read `pdmDataSource` → register ERP/DataWS/OtherEx (company lock) |
| **2 Entity** | **System Define first** (export PLM tables → preview → execute job) → then **User Define** (preview → execute job) |
| **3 Template** | 1 PLM Template → 1 Transaction Group; preview → execute job |
| **4 Other Data** | Placeholder (Color, POM, …) |

**Hard rules:**

- PLM and APP may be on **different SQL Server instances** (C# only, not cross-DB SQL scripts).
- PLM DB is **not** kept as a permanent APP datasource — data lands in **Tenant DB**.
- Re-import matches **`IntegrationId` only** (entity = PLM `EntityID`; field = `SubItem_{id}` / `Grid_{id}`).
- UserDefine row data: **TRUNCATE / clear list → full reload** per entity.
- Execute: **all entities in tab**, one transaction; **any failure → full tab rollback**.
- Long execute: **async job** + UI **polls** `GET ImportJob/{id}`.
- After import: **prompt IIS recycle** (not automatic).

**Tenant DB schema (planned — C# only at implementation):**

`IntegrationId` columns + `AppPlmImportSession` / `Job` / `Log` tables are created by **`PlmMigrationBL.EnsurePlmImportSchema()`** in C# (`DatabaseFixture.ExecuteNonQueryResult`, idempotent `IF NOT EXISTS` checks). **Do not add `.sql` files** for this feature (not in `APP.BL/`, not in `AppAI.Web/Migrations/`, not in `ImportDoc/`).

---

## 1. Objective

Add a **PLM Data Import** wizard to the React **Database Design** page (`DatabaseDesignManagement.tsx`). Users migrate legacy PLM into App-Builder step by step.

**UI:** Left sidebar **「PLM Data Import」** → right panel `PLMDataImport` (same theme/layout as other Database Design sections).

---

## 2. Scope overview

| Wizard step | Migration plan | Status |
|-------------|----------------|--------|
| Connect & Discover | §1 Database | Specified |
| Entity Data Source | §2 Data Source Management | Specified |
| Template Import | §6 Reference (structure) | Framework |
| Other Data | §3–5, §7–8 | Placeholder |

**Out of scope:** Enum entities · RelationFK · Excel export of preview · PLM RestJson/RestXML datasources

---

## 3. Confirmed decisions

### 3.1 Connections & environment (A, B)

| ID | Rule |
|----|------|
| A1 | Cross-instance from day one |
| A2 | PLM sources in `pdmDataSource`; see `EmDataSourceFrom` in PLM `Enums.cs` |
| A3 | PLM connection: user types once in UI; **not** stored in `AppDataSourceRegister` |
| A4 | Tenant DB, Master DB, `CompanyId` from session; **`SaasApplicationID` from Step 1 dropdown** (E22) |
| B5–B7 | Register ERP/DataWS/OtherEx in Master DB; naming `{TenantDb}_ERP` / `_DataWS` / `_OtherEx` |
| B5a–c | **Company lock** on connection string (same company → reuse; different company → block) |
| B8–B9 | PLM (1): **no new register**; map to Company Master DB; discover/validate only |
| B10 | Empty PLM connection in `pdmDataSource` → use main PLM connection |

**`EmDataSourceFrom`:**

| Value | Name | APP handling |
|-------|------|----------------|
| 1 | PLM | Company Master register; tables copied to Tenant |
| 2 | ERP | External register |
| 3 | DataWS | External register |
| 4 | OtherEx | External register |
| 5–6 | Rest | Ignored |

### 3.2 Entity import (C)

| ID | Rule |
|----|------|
| C8–C9 | No enum · no RelationFK |
| C10 | Update by `IntegrationId` = PLM `EntityID` on `AppEntityInfo` |
| C10a | UserDefine rows: **TRUNCATE** wide table or clear SimpleList → **full reload** |
| C10b–c | Execute **all** entities in tab; **any failure → full tab rollback** |
| C11 | Wide table prefix default `Plm_entity_` (configurable) |
| C12b | SystemDefine DSF=1: copy **only tables referenced by `pdmEntity`** |
| C12c | SystemDefine DSF=2–4: reference external DB only |
| C12a | Table copy in C# with **PK preserved** (not `SELECT * INTO`) |
| C13 | Prompt IIS recycle after import |

**Step 2 order:** System Define tab **first** → User Define tab unlocked after System complete (or explicit skip when nothing to import).

**Specs:** `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql`, `ImportPlmUserDefineEntitiesToAppEntityInfo.sql`

### 3.3 Template import (D)

| ID | Rule |
|----|------|
| D15 | 1 PLM Template → 1 APP Transaction Group |
| D16 | Import special blocks as ordinary first |
| D17 | Bind to `SaasApplicationID` from Step 1 |

| PLM | APP | `IntegrationId` |
|-----|-----|----------------|
| Template | Transaction Group / header | PLM `TemplateId` |
| Tab | Transaction | PLM `TabId` |
| Block | Unit | PLM `BlockId` |
| SubItem column | Field | `SubItem_{columnId}` |
| Grid column | Field | `Grid_{columnId}` |

> **Note:** Template group may live on `AppTransaction` (TemplateHeader) rather than `AppTransactionGroup` — confirm in Phase 5.

### 3.4 Wizard product rules (E, F)

| ID | Rule |
|----|------|
| E18 | Extensible step registry; **Other Data** placeholder |
| E19–c | Session in Tenant DB; resume only if **in progress**; completed → no resume |
| E20–a | `SaasCompanyAdmin` or `SysAdmin`; SysAdmin picks target Company |
| E23 | PLM connection encrypted in session; pre-filled on resume |
| F27 | Long execute → async job + poll |
| F25 | Full rollback on execute failure |
| F26 | UI shows PLM connection errors (no network assumptions) |

---

## 4. Architecture

### 4.1 Frontend

```
AppReact/src/components/dbmgt/
├── DatabaseDesignManagement.tsx    ← add section
├── PlmDataImportManagement.tsx
└── plmImport/
    ├── PlmImportWizard.tsx
    ├── plmImportStepRegistry.ts
    ├── steps/ (Connection, Entity, Template, OtherData)
    └── types.ts
```

`AppReact/src/webapi/plmMigrationSvc.ts` — API client.

### 4.2 Backend

| Layer | Path |
|-------|------|
| Controller | `AppAI.Web/Controllers/PlmMigrationController.cs` |
| BL | `APP.BL/DataMigration/PlmMigration/PlmMigrationBL.*.cs` (3 partials, one class) |
| DTOs | `APP.Components.Dto/UserDefine/PlmMigration/` |

**`partial class PlmMigrationBL`:**

| File | Responsibility |
|------|----------------|
| `PlmMigrationBL.Connection.cs` | Auth, session, **`EnsurePlmImportSchema()`**, test/discover, register datasources, logging helper |
| `PlmMigrationBL.Entity.cs` | Export tables, UserDefine/SystemDefine preview & execute, jobs |
| `PlmMigrationBL.Template.cs` | Template preview & execute, jobs |

Controller → `PlmMigrationBL` only. No `DatabaseFixture` in controller.

### 4.3 Tenant database schema (planned — C# only)

**Rule:** PLM Import wizard schema is applied **only from C#** in `PlmMigrationBL`. **No new `.sql` files** for this feature anywhere in the repo.

| What | How (at implementation) |
|------|---------------------------|
| `IntegrationId` columns | `EnsurePlmImportSchema()` — idempotent `ALTER TABLE` via `DatabaseFixture` on tenant fixture |
| `AppPlmImportSession` / `Job` / `Log` | Same method — idempotent `CREATE TABLE` if not exists |
| When called | First wizard API that needs schema (e.g. `GetImportSession/active`, `TestPlmConnection`) — once per app domain / tenant fixture |

**Pattern:** Same as `AppDataSourceRegisterBL.ExecuteStructureUpdateScript` — DDL as **C# string constants**, not `.sql` files on disk.

**Why not runtime ALTER during Execute?** Schema is ensured at wizard **entry**, not mid-import. Import execute assumes columns/tables already exist.

**`IntegrationId` columns (tenant DB):**

| Table | Type | Stores |
|-------|------|--------|
| `AppEntityInfo` | `int NULL` | PLM `EntityID` |
| `AppTransaction` | `nvarchar(100) NULL` | `TemplateId` / `TabId` |
| `AppTransactionUnit` | `nvarchar(100) NULL` | `BlockId` |
| `AppTransactionField` | `nvarchar(100) NULL` | `SubItem_{id}` / `Grid_{id}` |

**Wizard tables:** see §4.4 column lists (`AppPlmImportSession`, `AppPlmImportJob`, `AppPlmImportLog`).

> `SqlReferenceSpecs/ImportPlm*.sql` are **legacy import logic references** for porting entity data only — not used to deploy schema and **do not add** companion migration `.sql` files.

### 4.4 Session, jobs, logs

**`AppPlmImportSession`** — wizard state, encrypted PLM conn, `StepStateJson`, `SessionStatus` (`InProgress` | `Completed`)

**`AppPlmImportJob`** — async execute; `JobType`, `Status`, `ProgressPercent`, `ProgressMessage`, result/error JSON

**`AppPlmImportLog`** — audit: `StepCode`, `Action`, `Status`, `TargetKey`, `PlmIntegrationKey`, `RowsAffected`, `DurationMs`

Every Preview/Execute writes log rows (started + final).

### 4.5 API endpoints

| Method | Path | Purpose |
|--------|------|---------|
| POST | `TestPlmConnection` | Validate PLM SQL |
| POST | `DiscoverPlmDataSources` | Read `pdmDataSource`, register, test external conns |
| GET | `ImportSession/active` | Resumable session |
| POST | `ImportSession` | Save session |
| POST | `ImportSession/discard` | Clear session |
| POST | `PreviewUserDefineEntityImport` | Sync preview |
| POST | `PreviewSystemDefineEntityImport` | Sync preview + export plan |
| POST | `ExecuteUserDefineEntityImport` | Start job |
| POST | `ExecuteSystemDefineEntityImport` | Start job |
| GET | `ImportJob/{jobId}` | Poll progress |
| POST | `ImportJob/{jobId}/cancel` | Cancel (best-effort) |
| GET | `ImportLog` | Session audit log |
| POST | `PreviewTemplateMapping` | Sync preview |
| POST | `ExecuteTemplateImport` | Start job |

---

## 5. Wizard flow

```
Step 1  Connect & Discover
        ├─ Select Application (required)
        ├─ SysAdmin: select Company
        ├─ PLM connection → test
        ├─ pdmDataSource 1–4 → test each external conn
        └─ Register / reuse DataSource registers

Step 2  Entity (System Define FIRST)
        ├─ Tab B: export PLM tables (DSF=1) → preview → execute [job]
        └─ Tab A: User Define → preview → execute [job]  (locked until B done)

Step 3  Template → preview → execute [job]

Step 4  Other Data (placeholder)
```

---

## 6. Implementation phases

| Phase | Deliverable | Est. |
|-------|-------------|------|
| **0** | Wizard shell + `EnsurePlmImportSchema()` (C#) + session/job API stub | 3–4 d |
| **1** | Connect & Discover + pickers + company lock | 3–4 d |
| **2** | `ExportPlmTablesToTenant` + job infrastructure | 3–5 d |
| **3** | SystemDefine preview/execute | ~1 wk |
| **4** | UserDefine preview/execute | 1–2 wk |
| **5** | Template framework | 1–2 wk |
| **6** | Other Data placeholder | 1 d |

---

## 7. Questions

### Resolved

| ID | Answer |
|----|--------|
| Q1–Q10 | See [change log](#9-change-log); summarized in §3 |
| Q11 | `SaasApplicationID` — Step 1 dropdown |
| Q12 | UserDefine rows: TRUNCATE + full reload |
| Q13 | All entities per tab; full tab rollback on failure |
| Q14 | `IntegrationId` + wizard tables via **`PlmMigrationBL.EnsurePlmImportSchema()`** (C# only, no `.sql` files) |
| Q15 | `SaasCompanyAdmin` or `SysAdmin` (+ company picker) |
| Q16 | Async job + UI polling |
| Q17 | System Define before User Define |
| Q18 | `SubItem_{columnId}` / `Grid_{columnId}` |
| Q19 | `AppPlmImportLog` table created in **`EnsurePlmImportSchema()`** |
| Q20 | Test each external datasource on Discover |

### Open

| ID | Question |
|----|----------|
| Q21 | Session auto-cleanup retention (e.g. 30 days)? |
| Q22 | Mutex if two admins run wizard concurrently? |
| Q23 | PLM `pdmDataSource` connection string plaintext when comparing to APP register? |

---

## 8. Reference files

| Path | Role |
|------|------|
| `SqlReferenceSpecs/ImportPlm*.sql` | Entity import logic reference for C# port (**pre-existing**; do not add new `.sql` for wizard schema) |
| `PLM Migration Plan.md` | Feature roadmap |

**PLM solution:** `PdmDataSourceBL.cs`, `Enums.cs` (`EmDataSourceFrom`), `PdmTemplateBL.cs`

---

## 9. Change log

| Date | Change |
|------|--------|
| 2026-06-16 | Initial baseline; Q1–Q10 |
| 2026-06-16 | Linked plan; SQL → `SqlReferenceSpecs/` |
| 2026-06-16 | Q11–Q20; partial `PlmMigrationBL` in `DataMigration/PlmMigration/` |
| 2026-06-16 | Doc restructure (TOC, quick reference) |
| 2026-06-16 | Tenant DDL **spec only** in `SqlReferenceSpecs/TenantMigration_*.sql` (no AppAI.Web migrations until Phase 0) |
| 2026-06-16 | **Policy:** wizard schema via C# `EnsurePlmImportSchema()` only — **no `.sql` files** for PLM Import; removed TenantMigration `*.sql` from ImportDoc |
