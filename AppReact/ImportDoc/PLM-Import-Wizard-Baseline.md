# PLM Data Import Wizard — Baseline Plan

> **Status:** Living document — update when requirements or design change.  
> **Purpose:** Single source of truth for **how** to build the wizard (API, BL, DB, UI).  
> **Feature scope:** [PLM Migration Plan.md](./PLM%20Migration%20Plan.md)  
> **SQL import specs:** [SqlReferenceSpecs/](./SqlReferenceSpecs/)  
> **PLM reference:** `C:\Dev\PLM3\PLM`  
> **Last updated:** 2026-06-16 (System Define table copy prefix design; Template Import spec Phase 5)

---

## Document map

| Section | Read when you need… |
|---------|-------------------|
| **[§0 Quick reference](#0-quick-reference)** | One-page summary of wizard flow and rules |
| **[§1 Objective](#1-objective)** | What we are building |
| **[§2 Scope](#2-scope-overview)** | In / out of scope |
| **[§3 Decisions](#3-confirmed-decisions)** | All confirmed business rules (A–G) |
| **[§4 Architecture](#4-architecture)** | React, C#, API, DB, logs |
| **[§5 Wizard flow](#5-wizard-flow)** | Step-by-step diagram |
| **[§6 Phases](#6-implementation-phases)** | Delivery order and estimates |
| **[§7 Q&A](#7-questions)** | Resolved and open questions |
| **[§8 References](#8-reference-files)** | Files and PLM solution pointers |
| **Template import (Phase 5)** | [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md) — full rules |
| **System Define table prefix** | [PLM-SystemDefine-Table-Prefix-Spec.md](./PLM-SystemDefine-Table-Prefix-Spec.md) — DSF=1 copy + `AppEntityInfo.TableName` |

---

## 0. Quick reference

**Entry:** Database Design → sidebar **PLM Data Import** → `PlmDataImportManagement.tsx`

**Who can run:** `SaasCompanyAdmin` or `SysAdmin` (SysAdmin must pick **target Company** in Step 1).

**Wizard steps:**

| Step | What happens |
|------|----------------|
| **1 Connect & Discover** | Pick **Application** + PLM connection → **`TablePrefix`** (default `Plm_`; User Define wide tables use `{TablePrefix}Entity_`) → read `pdmDataSource` → register ERP/DataWS/OtherEx (company lock). Discovery grid: **Data source name**, **Connection string** (raw from `pdmDataSource`), **Status** (`OK` only when row has its own connection string and test passed; blank connection → blank status). |
| **2 Entity** | **One button:** **Import Entities** — runs table copy → system define metadata → user define in sequence. Two expandable sections (System Defined / User Defined) with grids and unified progress below the button. |
| **3 Template** | 1 PLM Template → 1 **Data Model Template** (`AppSearch`); tabs → transactions; preview → execute job. Spec: [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md) |
| **4 Other Data** | Placeholder (Color, POM, …) |

**System Define sub-flow (Entity step, Tab 1):** Two phases on one screen pattern — **List** (sync preview API) + **Execute** (async job) buttons above the grid.

| Phase | Workflow step | Buttons | Grid title |
|-------|---------------|---------|------------|
| **1** | Import PLM Entity Tables | List PLM Importing Tables · Execute Import Tables | Need To Import PLM System Defined Entity Tables |
| **2** | Import PLM Entities | List PLM Importing Entities · Execute Import Entities | Need To Import PLM System Defined Entities |

Phase 2 unlocks after phase 1 execute completes. **Re-import** is allowed (Execute stays enabled when rows are importable). Wizard footer: **Previous / Next** centered, prominent nav between top-level steps.

**Session:** Header shows `Session #id` and **Discard Session** (calls `ImportSession/discard` — marks `InProgress` session completed; resets wizard UI; does **not** delete imported physical tables or `AppEntityInfo`).

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
| Template Import | §6 Reference (structure) | **Specified** — [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md) |
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
| A5 | **`TablePrefix`** (default `Plm_`) — template tables, System Define PLM table copy (DSF=1), and base for User Define wide tables. Stored in session `StepStateJson`. |
| A5a | User Define wide entity physical tables: **`{TablePrefix}Entity_{sanitized code}`** — `Entity_` suffix is fixed; not user-editable. |
| B5–B7 | Register ERP/DataWS/OtherEx in Master DB; naming `{TenantDb}_ERP` / `_DataWS` / `_OtherEx` |
| B5a–c | **Company lock** on connection string (same company → reuse; different company → block) |
| B8–B9 | PLM (1): **no new register**; map to Company Master DB; discover/validate only |
| B10 | Discovery uses **only** `pdmDataSource.ConnectionString` (no wizard PLM fallback). Empty or connection test failed → **skip** row (no external register). **Error** only when required: cannot read PLM, company lock, register save failure |
| B10a | Discovery grid shows raw `pdmDataSource.ConnectionString`; Status `OK` only when non-empty and test passed |
| B10c | Register map validation (entity import): only sources with **`RegisteredDataSourceId`** from discovery must resolve; skipped DataWS/OtherEx (no PLM connection) do **not** block preview |

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
| C11 | Wide table prefix **`{TablePrefix}Entity_`** (derived; `Entity_` fixed) |
| C12b | SystemDefine DSF=1: copy **only tables referenced by `pdmEntity`**; tenant physical name = **`{TablePrefix}{SysTableName}`** (read PLM source by original name) |
| C12b1 | DSF=1 `AppEntityInfo.TableName` = prefixed target name; DSF 2–4 unchanged. No double prefix if `SysTableName` already starts with `TablePrefix`. |
| C12c | SystemDefine DSF=2–4: reference external DB only |
| C12a | Table copy in C# with **PK preserved** (not `SELECT * INTO`) |
| C13 | Prompt IIS recycle after import |

**Step 2 order:** System Define tab **first** → User Define tab unlocked after System Define entity metadata import completes (or explicit skip when nothing to import).

**System Define UI (implemented):** Two workflow phases per table above — not four separate preview/import sub-steps. Each phase: user runs **List** then **Execute**; grid stays on the same panel.

**`StepStateJson` (Entity step):** `connectionTested`, `systemDefineTablesComplete`, `systemDefineEntitiesComplete` (legacy alias `systemDefineComplete` = tables complete).

**Specs:** `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql`, `ImportPlmUserDefineEntitiesToAppEntityInfo.sql`

**User Define (implemented):** SimpleValueList empty `Code` / `Description` → `''` not NULL. Wide table physical name = `{TablePrefix}Entity_{sanitized code}` — **not** re-derived from `Plm_`-prefixed `EntityCode`.

### 3.3 Template import (D)

**Full spec:** [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md)

| ID | Rule |
|----|------|
| D15 | 1 PLM `pdmTemplate` → 1 **Data Model Template** (`AppSearch`, `DataModelTemplate`) — UI: `TransactionGroupEditor` |
| D16 | Import special blocks/grids as **ordinary** structure first; `EmGridType` → preview warning + log |
| D17 | Bind to `SaasApplicationID` from Step 1 |
| D18 | Global root table **`{TablePrefix}ReferenceBasicInfo`** — one per tenant, all templates/tabs share |
| D19 | Each tab → `AppTransaction` (MasterDetail): **Root unit** + **Sibling unit** (`{prefix}{TabName}`) + **Child unit** per grid subitem |
| D20 | Non-grid subitem → sibling field, column `SanitizedSubItemName_{SubItemID}`; grid subitem → child unit only |
| D21 | `Label` / `Empty` → form layout only, no DB column |
| D22 | Header tab → Template **Shared Item**; normal tab → **Main Item**; Copy tab / Master Ref Header → skip (v1) |
| D23 | Auto-create **Dataset**, search view fields, filters, folder, **AppForm** (PLM `pdmTabLayout*` or auto-form fallback) |
| D24 | Re-import: update by `IntegrationId`; **ALTER TABLE ADD** only; never drop columns — warn on orphans |
| D25 | Prepare **Product Reference** traceability: PLM ids → APP table/column/field via `IntegrationId` |

| PLM | APP | `IntegrationId` |
|-----|-----|----------------|
| Template | `AppSearch` (Data Model Template) | `Template_{TemplateId}` |
| Tab | `AppTransaction` | `Tab_{TabId}` |
| Root unit | `{prefix}ReferenceBasicInfo` | `Unit_ReferenceBasicInfo` |
| Sibling unit | `{prefix}{SanitizedTabName}` | `Unit_Sibling_{TabId}` |
| Grid child unit | `{prefix}{SanitizedGridSubItemName}` | `Unit_Grid_{SubItemId}` |
| SubItem field | Sibling unit field | `SubItem_{SubItemId}` |
| Grid column field | Child unit field | `Grid_{GridColumnId}` |

**Entity binding:** PLM `EntityId` → `AppEntityInfo.IntegrationId` → `AppTransactionField.EntityId`.

**Tab reuse:** Same `TabId` → one `AppTransaction`; multiple templates reference it.

### 3.4 Wizard product rules (E, F)

| ID | Rule |
|----|------|
| E18 | Extensible step registry; **Other Data** placeholder |
| E19–c | Session in Tenant DB; resume only if **in progress**; completed → no resume |
| E19d | **Discard Session** button in wizard header — `POST ImportSession/discard`; clears resumable state only (not tenant data) |
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
| BL | `APP.BL/DataMigration/PlmMigration/PlmMigrationBL.*.cs` (multiple partials, one class) |
| DTOs | `APP.Components.Dto/UserDefine/PlmMigration/` |

**`partial class PlmMigrationBL`:**

| File | Responsibility |
|------|----------------|
| `PlmMigrationBL.cs` | Constants, shared types |
| `PlmMigrationBL.Connection.cs` | Auth, session, **`EnsurePlmImportSchema()`**, save/discard session, logging helper |
| `PlmMigrationBL.Discover.cs` | `TestPlmConnection`, `DiscoverPlmDataSources`, datasource register |
| `PlmMigrationBL.Export.cs` | `BuildPlmTableExportPlan`, `ExportPlmTablesToTenant` |
| `PlmMigrationBL.Jobs.cs` | Job queue/run, table export job, System Define entity import job, preview entry points |
| `PlmMigrationBL.SystemDefineEntity.cs` | System Define entity preview & import logic |
| `PlmMigrationBL.UserDefineEntity.cs` | User Define entity preview & import (SimpleValueList + wide tables) |
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
| `AppTransaction` | `nvarchar(100) NULL` | `Template_{id}` / `Tab_{id}` |
| `AppTransactionUnit` | `nvarchar(100) NULL` | `Unit_ReferenceBasicInfo` / `Unit_Sibling_{tabId}` / `Unit_Grid_{subItemId}` |
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
| POST | `ImportSession/discard` | Mark active `InProgress` session completed; user starts fresh (UI: **Discard Session**) |
| POST | `PreviewPlmTableExportPlan` | Sync table list for System Define phase 1 |
| POST | `ExecutePlmTableExport` | Start `PlmTableExport` job (phase 1) |
| POST | `PreviewUserDefineEntityImport` | Sync preview |
| POST | `PreviewSystemDefineEntityImport` | Sync entity metadata preview (System Define phase 2) |
| POST | `ExecuteUserDefineEntityImport` | Start job |
| POST | `ExecuteSystemDefineEntityImport` | Start `SystemDefineEntityImport` job (phase 2) |
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
        ├─ Table prefix: TablePrefix (default Plm_); wide entities = {TablePrefix}Entity_
        ├─ SysAdmin: select Company
        ├─ PLM connection → test
        ├─ pdmDataSource 1–4 → test each external conn
        └─ Register / reuse DataSource registers

Step 2  Entity
        ├─ Tab 1: System Define (FIRST)
        │     Phase 1 — Import PLM Entity Tables
        │       ├─ List PLM Importing Tables  → grid (Schema, Table, Source OK, …)
        │       └─ Execute Import Tables      → async job (PlmTableExport)
        │     Phase 2 — Import PLM Entities (unlocked after phase 1 execute)
        │       ├─ List PLM Importing Entities → grid (Entity code, Status, …)
        │       └─ Execute Import Entities    → async job (SystemDefineEntityImport)
        └─ Tab 2: User Define → List + Execute  (locked until System Define complete; Phase 4)

        Footer: Previous / Next (centered) between wizard steps 1–4
        Header: Session # · Discard Session (when session exists)

Step 3  Template Import
        ├─ List PLM Templates → preview (tabs, units, tables, blockers, GridType warnings)
        ├─ Execute Import Templates → async job (TemplateImport)
        └─ Creates: AppSearch + transactions + tables + forms + dataset/search/folder
            Spec: PLM-Template-Import-Spec.md

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
| **4** | UserDefine preview/execute | ~1 wk | **Done** — preview + async import |
| **5** | Template import (structure) | 1–2 wk | **Specified** — [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md) |
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
| Q24 | Template = `AppSearch` (Data Model Template), not `AppTransactionGroup` |
| Q25 | Table prefix in Step 1; template + System Define use `TablePrefix`; User Define wide tables use `{TablePrefix}Entity_` |
| Q26 | Template structure: Root + Sibling + Child units per tab — see Template Import Spec |

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
| [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md) | Template import detailed rules (Phase 5) |

**PLM solution:** `PdmTemplateBL.cs`, `PdmTabBL.cs`, `ReferenceTabValueLoadBL.cs`, `Enums.cs`

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
| 2026-06-16 | **Implemented UI:** System Define 4 sub-steps → **2 phases** (List + Execute each); grid titles; centered wizard nav; **Discard Session** button; table export APIs; BL partial file list; re-import allowed; `StepStateJson` fields documented |
| 2026-06-16 | **Phase 4:** User Define preview + async import job; User Define tab UI with List/Execute |
| 2026-06-16 | **Phase 5 spec:** [PLM-Template-Import-Spec.md](./PLM-Template-Import-Spec.md) — Data Model Template mapping, Root/Sibling/Child units, table prefix (Step 1), form/dataset/folder, product-import prep |
| 2026-06-16 | Entity import: `Plm_entity_*` naming fix; SimpleValueList `''` for empty Code/Description |
| 2026-06-16 | **System Define table copy prefix:** [PLM-SystemDefine-Table-Prefix-Spec.md](./PLM-SystemDefine-Table-Prefix-Spec.md) — `TablePrefix` on DSF=1 export + `AppEntityInfo.TableName` |
| 2026-06-17 | **B10 simplified:** only `pdmDataSource.ConnectionString`; skip on empty/failed test; no PLM connection fallback |
| 2026-06-17 | **Entity wide prefix:** derived `{TablePrefix}Entity_` — removed separate Step 1 field |
