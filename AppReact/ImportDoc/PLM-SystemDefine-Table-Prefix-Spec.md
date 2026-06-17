# PLM System Define Table Copy — Prefix Design (Phase 1 / §2.2)

> **Status:** Confirmed design — ready for implementation.  
> **Scope:** Physical tables copied from PLM into **Tenant DB** for System Define entities with **`DataSourceFrom = 1`**.  
> **Related:** [PLM Migration Plan.md §1 / §2.2](./PLM%20Migration%20Plan.md), [PLM-Import-Wizard-Baseline.md](./PLM-Import-Wizard-Baseline.md), [PLM-Template-Import-Spec.md §2](./PLM-Template-Import-Spec.md)  
> **Backend today:** `PlmMigrationBL.Export.cs` (copy), `PlmMigrationBL.SystemDefineEntity.cs` (metadata + physical check)

---

## 1. Problem

Today, Step 1 **table export** copies PLM `SysTableName` into the Tenant DB **without renaming**. Step 2 System Define import writes the same name into `AppEntityInfo.TableName`.

| Risk | Example |
|------|---------|
| Name collision with existing tenant tables | PLM `Color` vs tenant `Color` |
| Inconsistent namespace vs template import | Template creates `Plm_ReferenceBasicInfo`; copied PLM table stays `ReferenceBasicInfo` |
| No isolation for trial re-import | Cannot distinguish PLM-imported tables from native APP tables |

User decision: **apply a configurable prefix** to copied System Define (DSF=1) physical tables, aligned with the wizard **Table prefix** already captured in Step 1.

---

## 2. Which prefix setting?

| Setting | Default | Applies to System Define table copy? |
|---------|---------|--------------------------------------|
| **`TablePrefix`** | `Plm_` | **Yes** |
| **`EntityWideTablePrefix`** | `Plm_entity_` | **No** — User Define wide EAV tables only |

**Rationale:** DSF=1 copies are PLM-native physical tables (same class as template product tables). They share the `Plm_` namespace with `{prefix}ReferenceBasicInfo`, `{prefix}FabricInfo`, etc. User Define wide tables remain on `Plm_entity_`.

---

## 3. Two-name model

Every DSF=1 table participates in two names:

| Name | Meaning | Used where |
|------|---------|------------|
| **`SourceTableName`** | PLM `pdmEntity.SysTableName` (unchanged) | Read from PLM source DB; source existence check; issues/logs |
| **`TargetTableName`** | Prefixed physical name in Tenant DB | `CREATE TABLE`, bulk copy destination, `AppEntityInfo.TableName`, tenant existence check |

**Schema:** `SchemaOwner` is **not** prefixed (typically `dbo`). Only the table name changes.

**Grouping:** Multiple `pdmEntity` rows may reference one physical table — one copy per `(SchemaOwner, SourceTableName)` → one `TargetTableName`.

---

## 4. Naming algorithm

Shared helper (same session sanitization as Step 1):

```
ResolveSystemDefineTargetTableName(sourceTableName, tablePrefix):
  prefix = SanitizeImportTablePrefix(tablePrefix, "Plm_")
  if sourceTableName is null/empty → return null
  if sourceTableName starts with prefix (ordinal ignore case) → return Truncate(sourceTableName, 100)
  else → return Truncate(prefix + sourceTableName, 100)
```

| PLM `SysTableName` | `TablePrefix` | `TargetTableName` |
|--------------------|---------------|-------------------|
| `Color` | `Plm_` | `Plm_Color` |
| `ReferenceBasicInfo` | `Plm_` | `Plm_ReferenceBasicInfo` |
| `Plm_Color` | `Plm_` | `Plm_Color` (no double prefix) |
| `My-Table` | `Plm_` | `Plm_My-Table` (no extra sanitization beyond prefix; PLM names are already valid) |

**Length:** `AppEntityInfo.TableName` and SQL identifier limit **100** chars — truncate after prefixing.

**PK constraint on create:** `PK_{TargetTableName}_Export` (use target name, not source).

---

## 5. Phase 1 — Table export (`ExportPlmTablesToTenant`)

### 5.1 Inputs

- `plmConnectionString`, `tenantConnectionString` (unchanged)
- **`tablePrefix`** from `ResolveImportPrefixes(session.StepStateJson).TablePrefix`

### 5.2 Flow (per exportable table)

```
Source qualified: [SchemaOwner].[SourceTableName]   ← PLM DB
Target qualified: [SchemaOwner].[TargetTableName]   ← Tenant DB

1. TableExists(PLM, schema, SourceTableName)
2. ReadColumnDefinitions / ReadPrimaryKeyColumns from PLM source table
3. EnsureSchemaExists(tenant, schema)
4. DropTableIfExists(tenant, schema, TargetTableName)
5. CreateTable(tenant, schema, TargetTableName, ...)
6. SqlBulkCopy: SELECT * FROM source → DestinationTableName = target qualified
```

**Re-import:** Always **DROP + CREATE** target table (current behavior), then bulk copy. Changing `TablePrefix` between runs creates a **new** physical table; old unprefixed or differently-prefixed tables are **not** auto-dropped (trial data — manual cleanup if needed).

### 5.3 Preview plan DTO

Extend `PlmTableExportPlanItemDto` / `PlmTableExportResultItemDto`:

| Field | Description |
|-------|-------------|
| `TableName` | **Source** table name (backward compatible) |
| **`TargetTableName`** | Prefixed tenant table name (**new**) |

Issues may reference both: `dbo.Color → dbo.Plm_Color: EntityID=…`.

### 5.4 Progress message

`Importing dbo.Color → dbo.Plm_Color...`

---

## 6. Phase 2 — System Define entity import

### 6.1 Staging row fields

After reading PLM metadata, for entities with **`PlmDataSourceFrom = 1`** only:

```
entity.SourceTableName = SysTableName from PLM   // optional explicit field; or keep TableName as source until transform
entity.TargetTableName = ResolveSystemDefineTargetTableName(entity.SourceTableName, tablePrefix)
entity.TableName       = entity.TargetTableName  // value written to AppEntityInfo
```

For **DSF 2–4** (ERP / DataWS / OtherEx): **no prefix** — `TableName` remains PLM `SysTableName`; physical table lives in external DB.

### 6.2 Physical table validation

`ValidatePhysicalTables` must check **`TargetTableName`** (tenant) for DSF=1, not source name.

Skip reason unchanged: `Physical table not found in datasource database`.

### 6.3 Insert / update `AppEntityInfo`

| Column | DSF=1 value |
|--------|-------------|
| `TableName` | `TargetTableName` |
| `SchemaOwner` | PLM `SchemaOwner` (default `dbo`) |
| `IntegrationId` | PLM `EntityID` (unchanged) |

Re-import still **updates by `IntegrationId` only**; table rename on prefix change updates `TableName` on next entity import execute.

### 6.4 Preview grid (System Define tab)

| Column | Binding |
|--------|---------|
| Source table | `SourceTableName` or PLM `SysTableName` |
| Target table | `TargetTableName` / `TableName` after transform |
| DS | `PlmDataSourceFrom` |

For DSF=1 rows, **Table** column should show **target** (prefixed) name so it matches post-export reality.

---

## 7. UI changes (wizard)

### Step 1 — Connect & Discover

No new field (reuse **Table prefix**).

### Step 2 — System Define → Import PLM Entity Tables

| Grid column | Binding |
|-------------|---------|
| Schema | `SchemaOwner` |
| **Source table** | `TableName` |
| **Target table** | `TargetTableName` (**new**) |
| Entities | `PlmEntityCount` |
| Source OK | `SourceTableExists` |

### Step 2 — System Define → Import PLM Entities

**Table** column shows prefixed name for DSF=1.

---

## 8. API / job wiring

| Endpoint / job | Change |
|----------------|--------|
| `previewPlmTableExportPlan` | Pass `TablePrefix` from session |
| `executePlmTableExport` job | Pass `TablePrefix` from session |
| `previewSystemDefineEntities` | Pass `TablePrefix`; apply to DSF=1 staging |
| `executeSystemDefineEntities` job | Same |

Pattern: same as User Define wide tables using `EntityWideTablePrefix` in `PlmMigrationBL.Jobs.cs`.

---

## 9. Alignment with template import (Phase 5)

| PLM source table | After copy (`TablePrefix=Plm_`) | Template-created table |
|------------------|--------------------------------|------------------------|
| `ReferenceBasicInfo` | `Plm_ReferenceBasicInfo` | Root unit → `Plm_ReferenceBasicInfo` |
| Tab-specific PLM tables (if any) | `Plm_{Name}` | Sibling `Plm_{TabName}` |

**Intent:** Copied PLM system tables and template-generated product tables share one namespace. If both steps create `Plm_ReferenceBasicInfo`, **order matters**:

1. **Table export** may create `Plm_ReferenceBasicInfo` from PLM data first.
2. **Template import** must **detect existing table** and **skip CREATE** (or ALTER-add missing columns) — template spec §5.1 already says “create once”.

Document this dependency in template BL when implemented.

---

## 10. Logging

`AppPlmImportLog` detail for export:

```
dbo.Color → dbo.Plm_Color | EntityID=123 | ...
```

Issue types unchanged (`MissingSourceTable`, `ExportFailed`); messages include source → target.

---

## 11. SQL reference scripts

`SqlReferenceSpecs/ImportPlmSystemDefineEntitiesToAppEntityInfo.sql` — update header comment:

- DSF=1: `TableName` in APP = `{TablePrefix}{SysTableName}` (with no-double-prefix rule)
- DSF 2–4: original `SysTableName`

Scripts remain **reference only**; C# is production path.

---

## 12. Implementation checklist

| # | File | Task |
|---|------|------|
| 1 | `PlmMigrationBL.Connection.cs` | Add `ResolveSystemDefineTargetTableName` |
| 2 | `PlmMigrationBL.Export.cs` | Plan + export use source/target names; bulk copy to target |
| 3 | `PlmMigrationBL.Jobs.cs` | Pass `TablePrefix` into export preview/job |
| 4 | `PlmMigrationBL.SystemDefineEntity.cs` | Apply prefix for DSF=1; validate physical on target name |
| 5 | `PlmMigrationBL.Entity.cs` | Pass prefix into system define preview if centralized here |
| 6 | `PlmTableExportDtos.cs` | Add `TargetTableName` |
| 7 | System define DTOs (if needed) | `SourceTableName` / `TargetTableName` on preview rows |
| 8 | `EntityStep.tsx` | Table export grid: Source + Target columns |
| 9 | `plmMigrationSvc` / types | TypeScript DTO fields |
| 10 | Plan docs | This spec + Migration Plan §1 / §2.2 |

---

## 13. Test scenarios

| # | Scenario | Expected |
|---|----------|----------|
| T1 | Export `Color` with `Plm_` | Tenant has `dbo.Plm_Color`, rows = PLM `dbo.Color` |
| T2 | Re-run export | `Plm_Color` dropped and recreated; row count matches |
| T3 | Entity import DSF=1 after export | `AppEntityInfo.TableName = Plm_Color`; not skipped |
| T4 | Entity import before export | Skipped — physical table not found |
| T5 | DSF=2 ERP entity | `TableName` = original `SysTableName`; no prefix |
| T6 | PLM table already `Plm_X` | Target stays `Plm_X` |
| T7 | Change prefix `Plm_` → `Trial_` | New `Trial_Color`; old `Plm_Color` orphaned |
| T8 | Multiple entities, one table | One copy; all entities reference same `TargetTableName` |

---

## 14. Revision history

| Date | Change |
|------|--------|
| 2026-06-16 | Initial design — `TablePrefix` on DSF=1 table copy + `AppEntityInfo.TableName` |
