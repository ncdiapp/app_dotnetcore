# PLM Data Import — Complete Specification

> **Version:** 2026-06-16  
> **Single source of truth:** Management, product, and engineering share this file; read by section.  
> **Entry point:** Database Design → **PLM Data Import** wizard  
> **PLM reference code:** `C:\Dev\PLM3\PLM`  
> **SQL reference scripts:** [SqlReferenceSpecs/](./SqlReferenceSpecs/) (production path is **C#**)

---

## How to Read This Document

| Part | Audience | Time | Contents |
|------|----------|------|----------|
| [Part I — Overview](#part-i--overview) | Management, product, business | ~5 min | Goals, four-step wizard, template strategy, progress, risks |
| [Part II — Wizard](#part-ii--wizard) | Product, QA, engineering | ~15 min | Per-step rules, flow diagram, APIs, implementation phases |
| [Part III — Details](#part-iii--details) | Engineering | As needed | Migration scope, entities, table prefix, template structure, mapping DTO |

### Table of Contents

**Part I — Overview**

- [1.1 What We Are Doing](#11-what-we-are-doing)
- [1.2 Four-Step Wizard](#12-four-step-wizard-user-view)
- [1.3 Template Import Essentials](#13-template-import-essentials-confirmed)
- [1.4 Implementation Progress](#14-implementation-progress)
- [1.5 Risks and Constraints](#15-risks-and-constraints)

**Part II — Wizard**

- [2.1 Entry and Permissions](#21-entry-and-permissions)
- [2.2 Step 1 — Connect & Discover](#22-step-1--connect--discover)
- [2.3 Step 2 — Entity Import](#23-step-2--entity-import)
- [2.4 Step 3 — Template Structure Import](#24-step-3--template-structure-import)
- [2.5 Step 4 — Other Data](#25-step-4--other-data)
- [2.6 Wizard Flow Diagram](#26-wizard-flow-diagram)
- [2.7 Tenant Database Extensions](#27-tenant-database-extensions)
- [2.8 API Reference](#28-api-reference)
- [2.9 Implementation Phases](#29-implementation-phases)

**Part III — Details**

- [3.1 Migration Scope](#31-migration-scope)
- [3.2 Architecture](#32-architecture)
- [3.3 Entity Import](#33-entity-import)
- [3.4 System-Define Table Prefix](#34-system-define-table-prefix)
- [3.5 Template Structure Import](#35-template-structure-import)
- [3.6 Template Mapping UI and DTO](#36-template-mapping-ui-and-dto)
- [3.7 SQL Reference Scripts](#37-sql-reference-scripts)
- [3.8 Change Log](#38-change-log)

---

## Part I — Overview

> Executive summary for management and product. Technical detail is in [Part II](#part-ii--wizard) and [Part III](#part-iii--details).

### 1.1 What We Are Doing

Migrate configuration and master data from **legacy Apparel PLM** into the **App-Builder (low-code platform)** tenant database in guided steps. Rebuild reference management, entities, templates, and related capabilities in APP **without** a long-term dependency on direct PLM database access.

| Point | Description |
|-------|-------------|
| Approach | Wizard: Connect → Entities → Template structure → (future) other data |
| Data destination | **Tenant database**; PLM is migration source only |
| Re-runnable | `IntegrationId` identifies imported objects; supports update and re-run |
| Permissions | Tenant admin (`SaasCompanyAdmin`) or system admin |

### 1.2 Four-Step Wizard (User View)

```
┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│ 1 Connect &      │ → │ 2 Entity Import  │ → │ 3 Template       │ → │ 4 Other Data     │
│   Discover       │   │                  │   │    Structure     │   │                  │
└──────────────────┘   └──────────────────┘   └──────────────────┘   └──────────────────┘
```

| Step | Business meaning | Import target | Status |
|------|------------------|---------------|--------|
| **1 Connect & Discover** | PLM connection, table prefix, data-source discovery and registration | Session + `AppDataSourceRegister` | **Done** |
| **2 Entity Import** | Copy PLM system tables; system/user-defined entity metadata | `AppEntityInfo`, wide tables, etc. | **Done** |
| **3 Template Structure** | PLM template layout → data-model template + Tab transactions + physical tables | `AppSearch`, `AppTransaction`, DDL | **In progress** |
| **4 Other Data** | Colors, POM, product row data, etc. | TBD | **Not started** |

Step 3 imports **structure only** (tables, transactions, fields). It does **not** import PLM product row data.

### 1.3 Template Import Essentials (Confirmed)

| Principle | Description |
|-----------|-------------|
| Default | No user action → **one business table per Tab** (same as early design) |
| Mapping UI | Main grid lists **Template × Tab** with transaction group/name to be created; import after confirmation |
| Optional dedup | Tab warning dialog: ① merge similar Tabs (≥80%); ② move Blocks to a shared table or `ReferenceBasicInfo` |
| Out of scope | One table per Block automatically; no automatic Block clustering |
| Default naming | Transaction group = **template name**; transaction = **Tab name**; `IntegrationId` = `Tab_{TabId}` |

**Physical table vs transaction fields:** When Tabs share a table, DDL uses the **column union**; each Tab’s transaction **only binds columns that Tab needs**.

### 1.4 Implementation Progress

| Phase | Content | Status |
|-------|---------|--------|
| 0–1 | Wizard shell, connection, session/job infrastructure | Done |
| 2–4 | System table copy, system/user-defined entities | Done |
| 5a | Template DDL + transactions (background job) | Basic path works; performance tuning in progress |
| 5b | Mapping grid + Tab dialog + Import Setting DTO | Scoped P0→P3 |
| 6+ | Colors, POM, product data | Planned |

### 1.5 Risks and Constraints

| Item | Description |
|------|-------------|
| Cross-instance | PLM and APP may be on different SQL Server instances; logic runs in **C#** |
| Failure rollback | Batch import uses a single transaction; failure rolls back the whole batch |
| After import | **IIS/Web restart** required to refresh cache (wizard prompts; not automatic) |
| v1 skipped | Copy Tab, master reference header Tab, special Grid runtime, product row data |
| Not imported | Enum entities, RelationFK, PLM Rest data sources |

---

## Part II — Wizard

> Wizard behavior spec: per-step rules, end-to-end flow, APIs, and delivery phases. Entity/template detail is in [Part III](#part-iii--details).

### 2.1 Entry and Permissions

| Item | Description |
|------|-------------|
| UI entry | `DatabaseDesignManagement` → sidebar **PLM Data Import** → `PlmDataImportManagement.tsx` |
| Who can run | `SaasCompanyAdmin` or `SysAdmin` (SysAdmin must select target **Company** in Step 1) |
| Session | Tenant table `AppPlmImportSession`; supports resume/discard |
| Discard session | **Discard Session** does **not** delete already-imported physical data |

### 2.2 Step 1 — Connect & Discover

| Item | Rule |
|------|------|
| Application | **SaasApplication** required in Step 1 |
| Table prefix | `TablePrefix` defaults to `Plm_`; user-defined wide tables = `{TablePrefix}Entity_` |
| PLM connection | Entered once by user; **not** stored in `AppDataSourceRegister` |
| `pdmDataSource` | DSF 1 → company main DB; 2–4 → register external DBs in Master DB; 5–6 ignored |
| Company lock | Same connection string cannot be reused across companies |
| Discovery grid | Shows raw connection strings; `OK` only when non-empty and test passes |

### 2.3 Step 2 — Entity Import

Single **Import Entities** button runs in order: table copy → system define → user define.

#### System define (two phases)

| Phase | Action | Grid |
|-------|--------|------|
| 1 | List / Execute **Import Tables** | PLM system tables to copy |
| 2 | List / Execute **Import Entities** | Rows for `AppEntityInfo` (unlocked after phase 1) |

#### Key rules

| Rule | Description |
|------|-------------|
| Update key | `IntegrationId` = PLM `EntityID` |
| User-define row data | **TRUNCATE + full reload** |
| Execution scope | All entities in tab; **any failure → entire tab rolls back** |
| Long-running work | Async job + poll `GET ImportJob/{id}` |

Technical detail: [§3.3 Entity Import](#33-entity-import), [§3.4 System-Define Table Prefix](#34-system-define-table-prefix).

### 2.4 Step 3 — Template Structure Import

#### User flow (new design)

```
Analyze → Main grid (Template × Tab) → [optional Tab warning dialog] → Save → Validate → Run Import (job)
```

#### Main grid columns (summary)

| Column | Description |
|--------|-------------|
| Template | Template name |
| Tab ID / name | PLM Tab identifier |
| Transaction Group | Default = template name → `AppSearch.Name` |
| Transaction | Default = Tab name |
| Sibling table name | Editable; default is one table per Tab |
| Status | Validation/import status |
| Warn | Icon when similar Tabs or Block reuse is detected |

#### Tab warning dialog (only when `ShowTabWarning`)

| Area | Behavior |
|------|----------|
| **Blocks** | BlockId, name, how many Tabs reference it; context menu: move to `ReferenceBasicInfo` or a shared Sibling table |
| **Similar tabs** | When Jaccard ≥ 0.80, suggest merging Tabs into one table; default name is editable |

If the dialog is never opened, the Import Setting DTO matches **one table per Tab**.

Technical detail: [§3.5 Template Structure Import](#35-template-structure-import), [§3.6 Template Mapping UI and DTO](#36-template-mapping-ui-and-dto).

### 2.5 Step 4 — Other Data

Placeholder step for colors, POM, product data, etc. New steps can be added via the step registry.

### 2.6 Wizard Flow Diagram

```
Step 1  Connect & Discover
        Application · TablePrefix · PLM test · pdmDataSource · register ERP/DataWS/OtherEx

Step 2  Entity
        System Define: Tables (job) → Entities (job)
        User Define: preview + import (job)
        Footer: Previous / Next

Step 3  Template
        GetTemplateTabMappingGrid → Save → Validate → ExecuteTemplateImport (job)

Step 4  Other Data (placeholder)
```

### 2.7 Tenant Database Extensions

Created by C# — no standalone `.sql` migration file.

`PlmMigrationBL.EnsurePlmImportSchema()` provisions:

- `IntegrationId` column (on relevant tables)
- `AppPlmImportSession` — wizard session
- `AppPlmImportJob` — background jobs
- `AppPlmImportLog` — audit log

### 2.8 API Reference

| Method | Purpose |
|--------|---------|
| `POST ImportSession` / `discard` | Save/discard session |
| `POST PreviewPlmTableExportPlan` / `ExecutePlmTableExport` | System table copy |
| `POST PreviewSystemDefineEntityImport` / `ExecuteSystemDefineEntityImport` | System-define entities |
| `POST PreviewUserDefineEntityImport` / `ExecuteUserDefineEntityImport` | User-define entities |
| `POST GetTemplateTabMappingGrid` | Template mapping grid + analysis |
| `POST SaveTemplateMapping` / `ValidateTemplateMapping` | Save/validate mapping |
| `POST ExecuteTemplateImport` | Template structure import job |
| `GET ImportJob/{id}` / `cancel` | Poll/cancel job |
| `GET ImportLog` | Audit log |

**Code locations**

| Layer | Path |
|-------|------|
| Frontend | `AppReact/src/components/dbmgt/plmImport/` |
| API | `AppAI.Web/Controllers/PlmMigrationController.cs` |
| BL | `APP.BL/DataMigration/PlmMigration/` (Connection, Export, Entity, Template, TemplateMapping, Jobs) |

### 2.9 Implementation Phases

| Phase | Deliverable | Status |
|-------|-------------|--------|
| 0 | Wizard shell + schema + session/job | Done |
| 1 | Connect & discover | Done |
| 2 | PLM table copy + job infrastructure | Done |
| 3 | System-define preview/execute | Done |
| 4 | User-define preview/execute | Done |
| 5a | Template DDL + transactions | In progress |
| 5b | Mapping grid + DTO + import reads plan | Planned P0–P3 |
| 6 | Other data placeholder | Pending |

---

## Part III — Details

> Engineering reference: data model, naming rules, DTO contracts, and SQL cross-checks.

### 3.1 Migration Scope

| Legacy PLM capability | Wizard step | Status |
|-----------------------|-------------|--------|
| Multi-DB / data sources | Step 1–2 | Specified |
| Reference management (template structure) | Step 3 | Specified |
| Color management | Other Data | Pending |
| POM / POM templates | Other Data | Pending |
| Files and images | TBD | |
| Search views / batch update | Partially covered by template import | |
| User/domain/role security | Not in this wizard | |

**Cross-step rules:** Cross SQL instances · PLM connection lives in wizard session only · `IntegrationId` upsert · single-transaction rollback · IIS recycle prompt after import.

### 3.2 Architecture

| Layer | Technology |
|-------|------------|
| Frontend | React `AppReact/.../plmImport/` |
| API | `PlmMigrationController` |
| BL | `APP.BL/DataMigration/PlmMigration/` |
| Session/jobs | Tenant tables + background `Task.Run` (capture identity on request thread; register in `WindowsIdentityProvider` on background thread) |

### 3.3 Entity Import

#### User-defined (`EntityType = 4`)

| Rule | Description |
|------|-------------|
| Table shape | ≤2 columns → `SimpleValueList`; >2 columns → wide table `{TablePrefix}Entity_{code}` |
| Update key | `IntegrationId` = PLM `EntityID` |
| Row data | Re-import = **TRUNCATE + full reload** |
| SQL reference | `SqlReferenceSpecs/ImportPlmUserDefineEntitiesToAppEntityInfo.sql` |

#### System-defined (`EntityType = 1`, non-RelationFK)

| Rule | Description |
|------|-------------|
| Metadata | → `AppEntityInfo` (tenant DB) |
| DSF = 1 | Table must exist in tenant DB (after §3.4 copy); `TableName` = `{TablePrefix}{SysTableName}` |
| DSF 2–4 | Row data not copied; table must exist on target data source |
| Not imported | Enum(3), RelationFK(5) |
| SQL reference | `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql` |

### 3.4 System-Define Table Prefix

| Setting | Rule |
|---------|------|
| `TablePrefix` | Captured in Step 1; default `Plm_` |
| Copy to tenant | Only DSF=1 system tables referenced by `pdmEntity` |
| Tenant physical name | `{TablePrefix}{SysTableName}` |
| `AppEntityInfo.TableName` | Same as above (DSF=1) |
| User-define wide table | `{TablePrefix}Entity_{code}` (`Entity_` is a fixed suffix) |

**Implementation:** `PlmMigrationBL.Export.cs`, `PlmMigrationBL.SystemDefineEntity.cs`

### 3.5 Template Structure Import

#### PLM hierarchy

```
pdmTemplate
  └── pdmTemplateTab → pdmTab → PdmTabBlock → pdmBlock → pdmBlockSubItem
        Grid(6) → pdmGrid → pdmGridMetaColumn (processed at import; columns not listed in mapping tree)
```

#### APP mapping

| PLM | APP |
|-----|-----|
| `pdmTemplate` | `AppSearch` (DataModelTemplate) `Template_{id}` |
| Tab (normal/header) | `AppTransaction` `Tab_{TabId}`; Main / Shared Item |
| Global root | `{prefix}ReferenceBasicInfo` |
| Tab non-Grid fields | Sibling table + Unit |
| Grid SubItem | Child table `{prefix}Grid_{SubItemId}` + Unit |

#### Per-Tab transaction units (default)

```
AppTransaction (MasterDetail)
  ├── Root Unit     → ReferenceBasicInfo
  ├── Sibling Unit  → Tab-specific table (default)
  └── Child Unit(s) → Grid table(s)
```

#### v1 skipped

- Copy Tab
- Master Reference Header Tab
- Product row data

#### IntegrationId

| Object | Format |
|--------|--------|
| Template | `Template_{TemplateID}` |
| Transaction | `Tab_{TabID}` |
| Grid unit | `Unit_Grid_{SubItemID}` |

#### Import order (job)

1. **DDL:** Root + all Sibling/Grid tables → **one** schema cache refresh
2. **Transactions:** Per `TabId`, create/update Transaction + Units/Fields
3. **Templates:** Per template, upsert `AppSearch`

**BL:** `PlmMigrationBL.Template.cs`

#### Re-import behavior

| Scenario | Behavior |
|----------|----------|
| Object match | By `IntegrationId` |
| Physical table | **Add columns only; never drop columns** |
| Removed PLM fields | Warning + keep APP column |

### 3.6 Template Mapping UI and DTO

#### Design principles

- **Zero extra action** = one table per Tab
- **Dedup** is opt-in via Tab dialog only

#### Confirmed rules (summary)

| # | Rule |
|---|------|
| R1 | Transaction group name = template name → `AppSearch.Name` |
| R2 | Transaction name = Tab name; same `TabId` → same name |
| R3 | `IntegrationId` = `Tab_{TabId}` |
| R4 | Suggest merge only when Jaccard similarity ≥ **0.80** |
| R5 | Shared table DDL = **column union**; transaction fields = **per-Tab subset** |
| R6 | **No** automatic `{prefix}Block_{BlockId}` tables |
| R7 | Block → `ReferenceBasicInfo` (header-eligible) or user-named **shared Sibling table** |
| R8 | Grid Blocks do not participate in Sibling table merge |

#### Import Setting DTO (contract summary)

```text
PlmTemplateImportSettingDto
  Rows[]                    // Main grid: Template × Tab
  TabSharedTableGroups[]    // Similar Tab shared tables
  BlockStorageOverrides[]   // Block → Root or shared Sibling
```

#### Delivery phases

| Phase | Content |
|-------|---------|
| P0 | `GetTemplateTabMappingGrid` + read-only grid |
| P1 | Dialog + Save/Validate + session persistence |
| P2 | Import reads plan (shared tables + overrides + per-Tab fields) |
| P3 | Re-import edge cases, column conflict validation |

| Layer | Path (planned) |
|-------|----------------|
| BL | `PlmMigrationBL.TemplateMapping.cs` |
| UI | `TemplateStep.tsx` |

### 3.7 SQL Reference Scripts

| File | Purpose |
|------|---------|
| `ImportPlmSystemDefineEntitiesToAppEntityInfo.sql` | System-define entity prototype |
| `ImportPlmUserDefineEntitiesToAppEntityInfo.sql` | User-define entity prototype |
| `ImportPlmEnumEntitiesToAppEntityInfo.sql` | Enum (**not executed currently**) |
| `ExportSourceDbTablesToNewDatabase.sql` | Table copy reference |

Production path is **C#**; scripts are for comparison and manual validation.

### 3.8 Change Log

| Date | Summary |
|------|---------|
| 2026-06-16 | Consolidated into single document; Overview + Wizard + Details; template mapping and dedup strategy finalized |
| 2026-06-16 | Rewritten in English |
