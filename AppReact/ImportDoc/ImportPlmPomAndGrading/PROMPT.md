# PLM → Tchp Master Data Import — Agent Prompt

> **Folder:** `AppReact/ImportDoc/ImportPlmPomAndGrading/`  
> **Outputs:** `output/{runId}/` — Preview report + import SQL (see §Deliverables)  
> **Scope:** SizeRun / Size, POM (`TchpBodyPart`), POM Template, GradeRuleSet / GradeRule  
> **Target:** Tenant APP database `Tchp*` tables (see `Document/Design/POM_Grading_QC_NewSchema.sql`)

---

## Confirmed decisions (locked)

| ID | Choice | Detail |
|----|--------|--------|
| **Source** | **PLM original tables only** | Read `PdmV2kBodyPart`, `pdmv2kBodyType`, `pdmV2kBodyTypeDetail`, `pdmV2kSpecBodyPartGrading` from the **PLM database**. Do **not** use APP `Plm_PdmV2kBodyPart` (or any `Plm_*` mirror). |
| **SizeRun source** | **`pdmEntity.DataSourceFrom`** | Do **not** guess PLM vs ERP by table existence. Read PLM **`pdmEntity`** for `SizeRun` / `SizeRunDetail` entities; use **`DataSourceFrom`**: **1 = PLM**, **2 = ERP** (see `PlmMigrationBL.GetPlmDataSourceFromName`). Resolve physical DB via **`pdmDataSource.ConnectionString`** for that `DataSourceFrom`. Physical table names from `pdmEntity.SysTableName` (typically `tblSizeRun`, `tblSizeRunRotate`). Never use APP `Plm_*`. |
| **D1 GradeRule** | **G-B** | One `TchpGradeRuleSet` per POM Template (`BodyType`); rules derived from `pdmV2kSpecBodyPartGrading` (via BodyTypeDetail). Standard = `CUSTOM`. |
| **D2 ID** | **M-B** | Preserve PLM/ERP primary keys via `SET IDENTITY_INSERT ON`. |
| **D3 TemplateCode** | Normalize `BodyTypeName` | Uppercase, non-alphanumeric → `_`, collapse `_`, trim, truncate to 50. On collision append `_{BodyTypeID}`. |
| **D4 Delivery** | SQL + batch first | Preview report + executable SQL in this folder. **No** Migration UI / C# / WebAPI in this phase. |
| **D5 Filter** | **IsActive = 1 only** | Discover active flag column name via probe; exclude inactive/deleted. |

### Out of scope (this folder)

- Dimension / `TchpSizeRunDimension` / `TchpSizeSystemMapping`
- Product `TchpStyleSpec` / `TchpPomSpecLine` / `TchpGradeValue`
- Fit / QC
- Writing into `Plm_*` tables
- ASTM seed conflict handling beyond “skip if name exists” (custom RuleSets use BodyTypeId as PK)

---

## User input (required)

**The user must supply in the same message (or before any probe):**

```text
1. PLM connection string (source — BodyPart / BodyType / Detail / SpecBodyPartGrading)
   Server=...;Database=...;...

2. Tenant APP connection string (target — Tchp* tables)
   Server=...;Database=...;...

3. (Conditional) ERP connection string
   Required only if Phase A resolves SizeRun `DataSourceFrom = 2` and `pdmDataSource` has no usable ConnectionString for ERP (user override).
```

Optional: `@RunId` label for output folder (default: UTC date `yyyyMMdd` or `preview1`).

### Gate 0 — missing input → ask user, do nothing else

If the user only references this file without connection strings:

1. **STOP.** Do not run sqlcmd / probes / generate import SQL.
2. Ask for PLM + Tenant APP connections (ERP override only if `pdmEntity` → `DataSourceFrom = 2` and `pdmDataSource` is empty).
3. Do not reuse passwords from other ImportDoc folders unless the user repeats them in this request.

---

## Hard rules

| Rule | Detail |
|------|--------|
| **No APP Plm_*** | Source queries use three-part or linked PLM/ERP names only. |
| **No server code** | Deliverables = SQL (+ optional PowerShell) under this folder only. |
| **Two phases** | **Phase A Preview** → STOP for user confirm. **Phase B Execute SQL** only after confirm. |
| **Idempotent upsert** | Re-run safe: MERGE/UPDATE by preserved PK; do not duplicate Codes. |
| **IDENTITY_INSERT** | Required for all identity PKs we preserve (see §ID map). |
| **Active only** | Filter source with discovered active flag (=1 / true). |
| **Tchp schema required** | Target must already have tables from `POM_Grading_QC_NewSchema.sql`. If missing → STOP and say so. |
| **Passwords** | Do not commit connection strings or passwords into git. Use sqlcmd env / local-only config. |

---

## ID preservation map (M-B)

| Source PK | Target PK | Notes |
|-----------|-----------|--------|
| `tblSizeRun.SizeRunId` | `TchpSizeRun.SizeRunId` | |
| `tblSizeRunRotate.SizeRunRotateID` | `TchpSizeRunSize.SizeRunSizeId` | |
| `PdmV2kBodyPart.BodyPartID` | `TchpBodyPart.BodyPartId` | |
| `pdmv2kBodyType.BodyTypeID` | `TchpPomTemplate.PomTemplateId` | Same ID used as GradeRuleSetId (G-B) |
| `pdmV2kBodyTypeDetail` PK (probe name) | `TchpPomTemplatePart.PomTemplatePartId` | If no single PK, use IDENTITY and unique (TemplateId, BodyPartId) |
| `BodyTypeID` | `TchpGradeRuleSet.GradeRuleSetId` | 1:1 with Template under G-B |
| SpecBodyPartGrading PK (probe) | `TchpGradeRule.GradeRuleId` | If unusable, IDENTITY + UQ (SetId, BodyPartCode) |

---

## Column mapping (canonical)

### SizeRun

| Target | Source (typical) |
|--------|------------------|
| SizeRunId | SizeRunId |
| SizeRunCode | SizeRunCode |
| SizeRunName | SizeRunName / Description / Name (probe) |
| IsActive | 1 (filtered) |

### SizeRunSize

| Target | Source (typical) |
|--------|------------------|
| SizeRunSizeId | SizeRunRotateID |
| SizeRunId | SizeRunId |
| SizeLabel | SizeName (truncate 20) |
| SizeOrder | Sort / SizeOrder / Rotate order (probe; else ROW_NUMBER) |
| IsActive | 1 |

### BodyPart

| Target | Source (typical) |
|--------|------------------|
| BodyPartId | BodyPartID |
| Code | Code (required unique; blank → exception) |
| BodyPartName | BodyPartName / Name |
| Tolerance | Tolerance |
| GradingPlusValue | GradingPlusValue |
| GradingMinuValue | GradingMinuValue / GradingMinusValue |
| IsActive | 1 |

### PomTemplate

| Target | Source |
|--------|--------|
| PomTemplateId | BodyTypeID |
| TemplateCode | Normalized BodyTypeName (D3) |
| TemplateName | BodyTypeName |
| DefaultBaseSizeId | Default base size FK if present **and** exists in TchpSizeRunSize; else NULL |
| IsActive | 1 |

### PomTemplatePart

| Target | Source |
|--------|--------|
| PomTemplateId | BodyTypeID |
| BodyPartId | BodyPartID |
| BodypartAliasName | Alias / display name if any |
| Sort | Sort / Sequence |

### GradeRuleSet (G-B)

| Target | Rule |
|--------|------|
| GradeRuleSetId | = BodyTypeID |
| GradeRuleSetName | `Template: {BodyTypeName}` (truncate 100) |
| Description | `Imported from pdmV2kSpecBodyPartGrading for BodyTypeID={id}` |
| Standard | `CUSTOM` |
| IsActive | 1 |

Create a RuleSet **only** for Active templates that have ≥1 usable grading row (or always create empty set — **default: only if ≥1 rule row**).

### GradeRule (G-B)

| Target | Rule |
|--------|------|
| GradeRuleId | Spec grading PK if preservable |
| GradeRuleSetId | BodyTypeID |
| BodyPartCode | Join Detail → BodyPart.Code (not FK id alone) |
| GradingPlusValue / GradingMinuValue | From SpecBodyPartGrading columns (probe names) |
| IsSymmetric | 1 iff Plus == Minu |
| Sort | Detail Sort |

If multiple grading rows collapse to same `(SetId, BodyPartCode)`, keep one (prefer latest / max PK) and list duplicates in Preview warnings.

---

## Phase A — Preview (STOP after)

### A1. Parse connections → `@PlmServer/@PlmDb`, `@AppServer/@AppDb`, optional `@Erp*` (override)

Auth: `sqlcmd -E` or env `PLM_SQL_USER` / `PLM_SQL_PASSWORD` (and ERP/APP equivalents). Never commit secrets.

### A1b. Resolve SizeRun physical database (authoritative)

Run `source/_plm_probe_sizerun_entity.sql` against **PLM**.

1. Read **`pdmEntity`** where `EntityCode IN (N'SizeRun', N'SizeRunDetail')` and `EntityType = 1`.
2. For each entity, record `DataSourceFrom`, `SysTableName`, `SchemaOwner`.
3. Map `DataSourceFrom` → database connection:

| DataSourceFrom | Name | Connection |
|----------------|------|------------|
| **1** | PLM | Same as user PLM connection string |
| **2** | ERP | `pdmDataSource.ConnectionString` where `DataSourceFrom = 2`; if missing → ask user for ERP override |

4. Run `source/_probe_sizerun_tables.sql` against the **resolved** database(s) to list columns + active flag.
5. Document in Preview: which DB hosts SizeRun vs SizeRunDetail (usually same `DataSourceFrom`; if different, import from each resolved DB).

**Wrong:** connect to PLM, not find `tblSizeRun`, assume ERP.  
**Right:** `pdmEntity` for SizeRun says `DataSourceFrom = 2` → read `tblSizeRun` from ERP via `pdmDataSource`.

### A2. Target readiness (APP)

Confirm tables exist:

`TchpSizeRun`, `TchpSizeRunSize`, `TchpBodyPart`, `TchpPomTemplate`, `TchpPomTemplatePart`, `TchpGradeRuleSet`, `TchpGradeRule`

### A3. Run probes (this folder `source/`)

| Script | Against | Purpose |
|--------|---------|---------|
| `_plm_probe_tables.sql` | PLM | Existence + column lists for POM tables |
| `_plm_probe_sizerun_entity.sql` | PLM | `pdmEntity` + `pdmDataSource` → SizeRun DB resolution |
| `_probe_sizerun_tables.sql` | Resolved SizeRun DB | Physical table columns + active flag |
| `_plm_probe_counts.sql` | PLM + resolved SizeRun DB | Active-only counts |
| `_plm_probe_grade_link.sql` | PLM | SpecBodyPartGrading → Detail → BodyType → BodyPart join path |

### A4. Emit Preview report

Write:

`output/{runId}/0_Preview_Report.md`

Must include:

- SizeRun resolution: `pdmEntity.DataSourceFrom`, `SysTableName`, resolved server/database
- Source DB names used (PLM for POM; PLM or ERP for SizeRun per entity metadata)
- Active row counts per entity
- Code collision candidates (BodyPart.Code, TemplateCode)
- SizeLabel collisions per SizeRun
- SpecBodyPartGrading rows with missing Code / missing Detail
- Templates with zero grading rows (no RuleSet under default rule)
- Existing target PK overlaps (same Id already in Tchp*)
- Recommended MERGE strategy notes

Also emit:

`output/{runId}/0_Preview_Counts.sql` — re-runnable count queries

### A5. STOP

Ask user to confirm Preview before generating / running Phase B import SQL.

---

## Phase B — Generate & execute import SQL (after confirm)

### B1. Generate scripts into `output/{runId}/`

| File | Content |
|------|---------|
| `1_Tchp_Import_SizeRun.sql` | SizeRun + Size (IDENTITY_INSERT) |
| `2_Tchp_Import_BodyPart.sql` | POM |
| `3_Tchp_Import_PomTemplate.sql` | Template + Part |
| `4_Tchp_Import_GradeRules.sql` | GradeRuleSet + GradeRule (G-B) |
| `5_Tchp_Import_Validate.sql` | Post-counts + orphan checks |
| `README.md` | Run order, sqlcmd examples (no passwords) |

Prefer **linked server / same-server three-part names** or parameters:

```sql
-- User sets once at top of each script:
DECLARE @PlmDb sysname = N'YourPlmDb';
DECLARE @SizeRunDb sysname = N'YourSizeRunDb'; -- from pdmEntity DataSourceFrom (PLM or ERP)
-- Scripts run connected to Tenant APP database
```

Pattern:

- POM / Template / GradeRule: `INSERT … SELECT … FROM [{PlmDb}].dbo.PdmV2kBodyPart WHERE …`
- SizeRun: `INSERT … SELECT … FROM [{SizeRunDb}].dbo.tblSizeRun WHERE …` where `@SizeRunDb` is chosen by **`pdmEntity.DataSourceFrom`**, not by guessing.

If cross-server: document OPENROWSET / linked server requirement in README (do not invent credentials).

### B2. Import order

1. SizeRun  
2. SizeRunSize  
3. BodyPart  
4. PomTemplate  
5. PomTemplatePart  
6. GradeRuleSet  
7. GradeRule  
8. Validate  

### B3. TemplateCode function (D3)

Implement consistently in SQL, e.g.:

```sql
-- Pseudocode: upper(BodyTypeName) → replace non [A-Z0-9] with _ → collapse __ → trim _ → LEFT(50)
-- If UQ conflict with another BodyTypeID: LEFT(40, code) + N'_' + CAST(BodyTypeID AS NVARCHAR(10))
```

### Active filter (D5)

After probe, bind `@ActivePredicate` per table, e.g. `ISNULL(IsActive,1) = 1` or `Active = 1`. Document discovered column in Preview.

### B4. Validate

- Count source active vs target inserted  
- No orphan TemplatePart BodyPartId  
- No GradeRule BodyPartCode missing from TchpBodyPart (warn OK for loose coupling; list them)  
- PK = source PK for mapped entities  

---

## Deliverables checklist

- [ ] `PROMPT.md` (this file)  
- [ ] `source/*_probe_*.sql`  
- [ ] `output/{runId}/0_Preview_Report.md` (+ counts SQL)  
- [ ] After confirm: `output/{runId}/1_…5_…sql` + README  

---

## Example session message

```text
@AppReact/ImportDoc/ImportPlmPomAndGrading/PROMPT.md

1. PLM: Server=...;Database=MyPlm;Trusted_Connection=True;
2. APP: Server=...;Database=TenantDB_xxx;Trusted_Connection=True;
3. RunId: pilot1

Please run Phase A Preview only.
```

After user confirms Preview:

```text
Confirm Phase A. Generate Phase B import SQL for RunId pilot1.
Do not execute against APP until I say so.
```

---

## Reference

- Schema: `Document/Design/POM_Grading_QC_NewSchema.sql`  
- Domain design: `Document/Design/POM_Grading_QC_Implementation_Plan.md`  
- Do **not** follow `ImportFromPLMDW` (different goal: DW → Plm_* Forms)  
- Do **not** use `PlmMigrationBL.PomImport` (writes Plm_* mirrors + Form config)
