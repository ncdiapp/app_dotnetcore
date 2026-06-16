# Apparel PLM Migration Plan

> **Execution baseline (wizard, SQL ports, API, confirmed decisions):**  
> [PLM-Import-Wizard-Baseline.md](./PLM-Import-Wizard-Baseline.md)  
>
> This document defines **what** to migrate and **the business rules / conditions**.  
> The baseline document defines **how** those rules are implemented in the Migration Wizard UI and backend flow.

**PLM reference solution:** `C:\Dev\PLM3\PLM`  
**Import scripts & specs:** `AppReact/ImportDoc/SqlReferenceSpecs/`

---

## Objective: Rebuild Legacy PLM on App-Builder

### Legacy PLM Main Features

Features that need to be rebuilt on App-Builder:

- Multiple Databases
- Data Source Management
- Color Management
- POM and POM Template Management
- File and Image Management
- Reference management
- Search View & Mass update
- User, Domain, Role Security

---

## How This Plan Is Delivered

Migration is delivered through a **PLM Data Import** wizard in the React **Database Design** page (`DatabaseDesignManagement`). The wizard steps implement the migration steps below in order. Steps not yet built appear as **Other Data** (placeholder); the wizard framework supports inserting or reordering steps later (e.g. Color import between Entity and Template).

| Migration plan section | Wizard step (initial) | Baseline reference |
|------------------------|----------------------|-------------------|
| §1 Database | Connect & Discover | Baseline §5 Step 1 |
| §2 Data Source Management | Entity Data Source | Baseline §5 Step 2 |
| §6 Reference Management (structure) | Template Import | Baseline §5 Step 3 |
| §3–5, §7–8 | Other Data (future) | Baseline §5 Step 4 |

**Wizard access:** Tenant Admin only (`EmAppUserType.SaasCompanyAdmin`). One wizard run = one tenant, one application.  
**Resume:** Server-side session in **Tenant DB** (`AppPlmImportSession`). On start, if a **partial** session exists, user chooses **continue** or **discard & start fresh**. Fully completed runs do not offer resume (session cleaned up). PLM connection string is stored encrypted in session and **pre-filled** on resume (user may edit).  
**Connectivity:** No assumed network topology; UI shows PLM connection failures clearly.

---

## Cross-Cutting Rules (apply to all migration steps)

| Rule | Description |
|------|-------------|
| **Cross-instance** | PLM SQL Server and App-Builder SQL Server **may be on different instances**. All reads from PLM use the user-supplied PLM connection string; all writes use App tenant/master connections. |
| **PLM connection** | User enters PLM connection string **once** in the wizard (not stored in `AppDataSourceRegister`). Tenant DB, Master DB, `SaasApplicationID`, and `CompanyId` are taken from the logged-in session. |
| **Traceability** | Use `AppEntityInfo.IntegrationId` (and equivalent on templates) = original PLM id for **update** on re-import. |
| **Transactions** | Each execute operation uses a **single transaction**; failure → **full rollback**. |
| **After import** | Prompt user to **recycle IIS** / refresh schema cache (not automated). |
| **Runtime** | SQL scripts in `ImportDoc/` are **reference specs**; production path is **C#** (required for cross-instance). |

### Explicit exclusions (current phase)

- **PLM Enum entities** (`EntityType = 3`) — do not import
- **RelationFK entities** (`EntityType = 5`) — do not import
- PLM REST data sources (`EmDataSourceFrom` RestJson / RestXML = 5, 6) — out of scope for database wizard phase

---

## Migration Steps

### 1. Database — Link Database or Import Tables

**Goal:** Connect to legacy PLM, discover all databases PLM uses, and register or import them in App-Builder.

**PLM model:** PLM uses one primary PLM database plus external databases. Connection definitions are in **`pdmDataSource`** (`SELECT * FROM pdmDataSource`), keyed by **`EmDataSourceFrom`**:

| `DataSourceFrom` | Name | App-Builder handling |
|------------------|------|----------------------|
| 1 | PLM | **No new register.** Discover/validate only in Step 1. Map to **Company Master DB** register (`IsCompanyMasterDb = true`). **APP does not retain a long-term connection to the original PLM database** — PLM-sourced tables and data are imported into **Tenant DB**. Empty `ConnectionString` in `pdmDataSource` → **display empty** in the discovery grid; **connection test / register** still uses the wizard main PLM connection (B10). |
| 2 | ERP | Register as external `AppDataSourceRegister` in Master DB |
| 3 | DataWS (Warehouse) | Register as external `AppDataSourceRegister` in Master DB |
| 4 | OtherEx | Register as external `AppDataSourceRegister` in Master DB |
| 5, 6 | RestJson, RestXML | Ignore in this phase |

**Register external databases (ERP, DataWS, OtherEx):**

- Auto-create rows in **Master DB** `AppDataSourceRegister` with `DataSourceOwnerCompanyId` = current user's company.
- **Naming:** `{TenantDatabaseName}_ERP`, `{TenantDatabaseName}_DataWS`, `{TenantDatabaseName}_OtherEx`.
- Multiple PLM `DataSourceFrom` values **may** map to the same APP `DataSourceRegister.Id`.

**Connection string company lock (security):**

Before creating a register, normalize the connection string and check Master DB for an existing register with the same connection:

| Condition | Action |
|-----------|--------|
| No existing register | Create new register |
| Exists, **same** `CompanyId` | Reuse register, continue |
| Exists, **different** `CompanyId` | **Block** — show warning. Same external DB cannot be shared across companies (data integrity and permissions). |

**Table import vs link (feeds §2):**

| PLM entity `DataSourceFrom` | Physical tables |
|----------------------------|-----------------|
| **PLM (1)** — system-defined tables | Copy **only tables referenced by `pdmEntity`** (SystemDefine, `DataSourceFrom=1`) into **Tenant DB** (PK preserved) |
| **ERP / DataWS / OtherEx (2–4)** | **Do not copy** — reference external DB via registered `AppDataSourceRegister` only |

**Notes (original):** If PLM has logic that pushes data into an external database, extra App-Builder code may be needed later. For migration, external DBs are used as entity data sources unless the rule above requires copying PLM-hosted tables into the tenant.

---

### 2. Migrate Data Source Management

**Goal:** Create App-Builder entity data sources from PLM `pdmEntity` metadata and data.

**Wizard step:** Entity Data Source — two tabs: **System Define** (first) and **User Define** (unlocked after System Define entity metadata import).  
**System Define sub-flow (UI):** See [PLM-Import-Wizard-Baseline.md §0 / §5](./PLM-Import-Wizard-Baseline.md) — two phases (Import PLM Entity Tables → Import PLM Entities), each with **List** + **Execute** above the grid.

#### 2.1 User Define entities (`EntityType = 4`, UserDefineTable)

**Source (PLM EAV):** `pdmEntity`, `pdmUserDefineEntityColumn`, `pdmUserDefineEntityRow`, `pdmUserDefineEntityRowValue`  
**Spec:** `SqlReferenceSpecs/ImportPlmUserDefineEntitiesToAppEntityInfo.sql`

| PLM column count | App entity type | Data location |
|------------------|-----------------|---------------|
| ≤ 2 | `SimpleValueList` (4) | `AppEntityInfo` + `AppEntitySimpleListValue` in **Tenant DB** |
| > 2 | `SystemDefineTable` (1) | `CREATE TABLE` in **Tenant DB** (default prefix `Plm_entity_`, configurable) + pivot EAV row data |

**Mapping rules:**

- `EntityCode` — sanitized PLM `EntityCode`; prefix `Plm_` if duplicate
- `Description` — original PLM description
- `IntegrationId` — PLM `EntityID` (**update key on re-import — `IntegrationId` only**)
- `DataSourceFrom` — Company Master DB register Id
- Simple list: col1 → Code, col2 → Description (by `DataRowSort`); `RowID` → InternalKey & Sort
- Wide table: `RowID` = primary key; column names sanitized from PLM definitions
- FK dependencies between UserDefine entities: import in dependency order
- **Re-import:** **Update** by `IntegrationId` only

#### 2.2 System Define entities (`EntityType = 1`, SystemDefineTable, not relation)

**Source:** `pdmEntity`, `pdmUserDefineEntityColumn` (PK + display columns)  
**Spec:** `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql`

| Rule | Detail |
|------|--------|
| Metadata | Insert/update `AppEntityInfo` in **Tenant DB** only |
| Row data | **Not** copied for ERP/DataWS/OtherEx entities — table must already exist in target datasource DB |
| PLM-hosted tables (`DataSourceFrom = 1`) | Tables must exist in **Tenant DB** after §1 copy step |
| `DataSourceFrom` mapping | PLM 1→4 → corresponding APP `DataSourceRegister.Id` from §1 |
| `TableName` | PLM `SysTableName` (no `Plm_entity_` prefix for system define) |
| `SchemaOwner` | PLM `SchemaOwner`, default `dbo` |
| `IdentityField` | Exactly one `IsPrimaryKey` column |
| Display fields | `UsedByDropDownList` columns by `DataRowSort` → DisplayField1–3 |
| Skip | `DataSourceFrom` NULL, 5, 6, or other → preview warning, not inserted |
| Physical table check | Must exist in target DB before insert; else skip with reason |
| **Re-import** | **Update** by `IntegrationId` |

#### 2.3 Relationship entities

**Original note:** Re-configure relationships in App-Builder with a simple list editor form.  
**Current phase:** RelationFK (`EntityType = 5`) is **not** imported by the wizard.

#### 2.4 Enum entities

**Original note:** Simple lists copy values.  
**Decision:** **Do not import** PLM enum entities (`EntityType = 3`). Script `SqlReferenceSpecs/ImportPlmEnumEntitiesToAppEntityInfo.sql` is not used.

---

### 3. Migrate Color Management

In App-Builder, create color table, import color data from PLM. Configure folder structure.

**Notes:** In PLM, color library is managed by multiple folders. In App Builder, one color belongs to one folder. PLM Color editor has special functions (swatch, color group, similar group) — may need extra coding.

**Wizard:** Future step (insertable before or after Template via step registry). Placeholder: **Other Data**.

---

### 4. Migrate POM and POM Template Management

In App-Builder, create POM, PomTemplate, PomTemplateDetail tables, import data from PLM.

**Notes:** PLM uses CM/Inch display but saves in cm. Size-run detail column mapping for template grids may need extra coding.

**Wizard:** Future step — **Other Data**.

---

### 5. Migrate File and Image Management

**Notes:** PLM AI/PDF multiple child image feature may need extra coding.

**Wizard:** Future step — **Other Data**.

---

### 6. Migrate Reference Management

PLM has system-defined blocks and grids with specific logic. Three areas: Product Management, Vendor Management, Vendor Request Management.

**Wizard — Template Import (structure phase):**

Implements the **layout skeleton** of reference management before special block logic.

| PLM | App-Builder |
|-----|-------------|
| `pdmTemplate` | **One** Transaction **Group** (`TemplateHeader`) |
| Tab | Transaction (child) |
| Block | Unit |
| SubItem / Grid column | Field |

**`IntegrationId` on re-import:**

| APP object | Stores |
|------------|--------|
| Transaction Group | PLM `TemplateId` |
| Transaction | PLM `TabId` |
| Unit | PLM `BlockId` |
| Field | PLM column id (grid/subitem) |

**Conditions:**

- Bind to current **`SaasApplicationID`**
- **Do not skip** special blocks/grids in v1 — import as **ordinary** block/grid; special behavior (size run, BOM, color grid, etc.) is **additional coding** after structure import
- Customer deployments are **fully customized** — mapping may vary per customer; reference PLM solution (`PdmTemplateBL`, tab/block/grid tables)
- Re-import / update by `IntegrationId` at each level (see table above)
- Sibling tables for fields on multiple tables: handled in template mapping logic (see original note below)

#### Special Notes (behavior after structure import)

**6.1 Vendor block, control security** — Need coding.

**6.2 Size run block/grid** — Select size, auto populate size details. Might be configurable.

**6.3 Spec Fit, Grading, QC block and grid** — Need coding for data transfer, grading calculation, POM/template integration.

**Color Grid — add color from library** — Mostly configurable; extra coding: swatch, duplicate check, DDL source, colorway column.

**BOM Grids** — Mostly configurable; extra coding: special columns, colorway name columns.

**Matrix Grid** — Configurable.

**Vendor Request** — Special request/approval flow; may need extra coding.

**Copy Tabs** — Extra coding or alternative solution.

> **Note:** For PLM templates and tabs, sibling tables may place fields on many tables by logic.

---

### 7. Search View & Mass Update

- Configurable.
- Reference PLM; create search, view, dataset in App-Builder.

**Wizard:** Future step — **Other Data**.

---

### 8. User, Domain, Role Security

- Configurable.

**Wizard:** Future step — **Other Data**.

---

## Wizard UI Summary (implements §1–2 and §6 structure)

Entry: Database Design → sidebar **「PLM Data Import」** → right panel `PLMDataImport` (same theme/layout as other sections).

```
Step 1  Connect & Discover     → §1
Step 2  Entity Data Source     → §2 (System Define first, then User Define)
Step 3  Template Import        → §6 (structure)
Step 4  Other Data             → §3, §4, §5, §7, §8 (future)
```

**Entity step — System Define (implemented):** Phase 1 copy PLM tables (DSF=1) to Tenant DB; Phase 2 import `AppEntityInfo` metadata. Each phase: **List** (preview grid) → **Execute** (async job). Full layout: [Baseline §5](./PLM-Import-Wizard-Baseline.md#5-wizard-flow).

Each data step: **Preview** (read-only grid, blockers/warnings) → **Execute** (transactional or async job where noted).  
No Excel/CSV export of preview. **Discard Session** available for in-progress wizard sessions (does not remove imported tenant data).

Full API, component paths, and open questions: [PLM-Import-Wizard-Baseline.md](./PLM-Import-Wizard-Baseline.md).

---

## Reference Files

| File | Role |
|------|------|
| [PLM-Import-Wizard-Baseline.md](./PLM-Import-Wizard-Baseline.md) | Execution baseline — wizard flow, API, phases, open Q&A |
| `SqlReferenceSpecs/ImportPlmUserDefineEntitiesToAppEntityInfo.sql` | User Define import spec |
| `SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql` | System Define import spec |
| `SqlReferenceSpecs/ExportSourceDbTablesToNewDatabase.sql` | Table list reference; PK must be fixed in C# |
| `SqlReferenceSpecs/ImportPlmEnumEntitiesToAppEntityInfo.sql` | **Not used** |

---

## Change Log

| Date | Change |
|------|--------|
| (original) | Initial migration scope (features §1–§8) |
| 2026-06-16 | Linked execution baseline; added cross-cutting rules, `pdmDataSource` / company lock, entity import conditions, table copy vs link, wizard mapping, exclusions |
| 2026-06-16 | Moved SQL reference scripts to `SqlReferenceSpecs/` subfolder |
| 2026-06-16 | Q1–Q10 resolved; session/resume rules, IntegrationId, C# module layout in baseline |
| 2026-06-16 | Aligned §2 / Wizard UI with Baseline: System Define first; 2-phase List+Execute UI; Discard Session; link to Baseline §5 |
