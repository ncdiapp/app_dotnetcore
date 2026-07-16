# Phase A Preview Report — RunId `pilot1`

**Date:** 2026-07-15  
**Status:** Preview only — **no import executed**

---

## Connections used (local run)

| Role | Server | Database |
|------|--------|----------|
| PLM | `PC3B\MSSQLSERVER01` | `plm_live_20260602` |
| ERP (SizeRun) | `PC3B\MSSQLSERVER01` | `SourceERP` |
| App Tenant (Tchp target) | `PC3B\MSSQLSERVER01` | `TenantDB_PLM27` |
| plmDW | `plm_live_20260602` / `plmDW` | **Not used** in this master-data import |

> Passwords were used only in the local sqlcmd session and are **not** stored in this repo.

---

## SizeRun resolution (`pdmEntity` — authoritative)

| EntityID | EntityCode | SysTableName | DataSourceFrom | Resolved DB |
|----------|------------|--------------|----------------|-------------|
| 10 | SizeRun | `tblSizeRun` | **2 (ERP)** | `SourceERP` |
| 63 | SizeRunDetail | `tblSizeRunRotate` | **2 (ERP)** | `SourceERP` |

`pdmDataSource` confirms ERP connection → `SourceERP` (matches user-provided ERP string).

**Import implication:** SizeRun SQL must use `[SourceERP].dbo.tblSizeRun*` (or linked server), **not** PLM catalog.

---

## Source row counts

| Entity | Source | All rows | Proposed import filter |
|--------|--------|----------|------------------------|
| SizeRun | SourceERP | 79 | **`isVisibleInPLM = 1` → 20** |
| SizeRunRotate | SourceERP | 536 | **Sizes under visible runs only → 152** |
| BodyPart | PLM | 540 | No `IsActive` column — see §Blockers |
| BodyType (Template) | PLM | 5 | No `IsActive` column |
| BodyTypeDetail | PLM | 11 | — |
| SpecBodyPartGrading | PLM | 65 | G-B: all 65 join to Detail |

### POM Templates (PLM)

| BodyTypeID | BodyTypeName | Normalized TemplateCode | Grade rules (G-B) |
|------------|--------------|-------------------------|-------------------|
| 17 | Test Html | `TEST_HTML` | 15 |
| 18 | Test SL | `TEST_SL` | 10 |
| 19 | teste22 | `TESTE22` | 10 |
| 20 | est11 | `EST11` | 20 |
| 21 | teste | `TESTE` | 10 |

---

## Target readiness (`TenantDB_PLM27`)

| Table | Exists | Current rows |
|-------|--------|--------------|
| TchpSizeRun | Yes | 0 |
| TchpSizeRunSize | Yes | 0 |
| TchpBodyPart | Yes | 0 |
| TchpPomTemplate | Yes | 0 |
| TchpPomTemplatePart | Yes | 0 |
| TchpGradeRuleSet | Yes | **2** (ASTM seed) |
| TchpGradeRule | Yes | **15** (ASTM seed) |

**PK note (M-B):** Imported `GradeRuleSetId` = BodyTypeID (17–21) — **no conflict** with ASTM seed IDs 1–2.

---

## Blockers / decisions needed before Phase B

### 1. D5 “IsActive = 1 only” — column mapping

| Table | IsActive column? | Recommendation |
|-------|------------------|----------------|
| `tblSizeRun` | No — has **`isVisibleInPLM`** | Import where `isVisibleInPLM = 1` (20 runs) |
| `tblSizeRunRotate` | No | Import rotates for visible runs only (152) |
| `pdmV2kBodyPart` | **No** | **Confirm:** import all 540, or another filter (FolderID / ProductReferenceID)? |
| `pdmv2kBodyType` | **No** | Import all 5 templates |

### 2. BodyPart `Code` uniqueness (HIGH)

- **1** row with NULL/empty Code (`BodyPartID=445`, name `tttt111`)
- **107** duplicate Code groups (e.g. Code `-` × 48, `005` × 30)

`TchpBodyPart` has **`UQ_TchpBodyPart_Code`**. Straight import will **fail** unless we:

| Option | Behavior |
|--------|----------|
| **B-A (recommended)** | Import by **BodyPartID** as PK; Code = `COALESCE(NULLIF(Code,''), 'BP_' + BodyPartID)`; duplicates get suffix `_{BodyPartID}` |
| **B-B** | Import only one row per Code (max BodyPartID); log dropped IDs |
| **B-C** | Stop and fix source PLM data first |

### 3. SizeRun “active” scope

| Option | SizeRun | Sizes |
|--------|---------|-------|
| **S-A (recommended)** | 20 visible | 152 |
| **S-B** | All 79 | 536 |

### 4. Cross-database import SQL

PLM and ERP are on the **same server** → Phase B can use three-part names:

```sql
FROM [SourceERP].dbo.tblSizeRun ...
FROM [plm_live_20260602].dbo.pdmV2kBodyPart ...
```

Scripts run **connected to `TenantDB_PLM27`**.

---

## G-B GradeRule validation

- **65 / 65** grading rows join: `SpecBodyPartGrading` → `BodyTypeDetail` → `BodyType` → `BodyPart`
- **0** orphan grading rows
- **5** RuleSets to create (BodyTypeID 17–21)

---

## Recommended import order (Phase B)

1. `1_Tchp_Import_SizeRun.sql` (SourceERP, visible filter)
2. `2_Tchp_Import_BodyPart.sql` (PLM, with Code dedupe rule confirmed)
3. `3_Tchp_Import_PomTemplate.sql` (PLM)
4. `4_Tchp_Import_GradeRules.sql` (PLM, G-B)
5. `5_Tchp_Import_Validate.sql`

---

## Confirm → Phase B executed

User confirmed: **S-A**, **B-A**, **all 540**. Scripts generated and run against `TenantDB_PLM27`.

### Result

| Table | Expected | Actual |
|-------|----------|--------|
| TchpSizeRun | 20 | **20** |
| TchpSizeRunSize | 152 | **152** |
| TchpBodyPart | 540 | **540** |
| TchpPomTemplate | 5 | **5** |
| TchpPomTemplatePart | 11 | **11** |
| TchpGradeRuleSet CUSTOM | 5 | **5** (+2 ASTM seed remains) |
| TchpGradeRule CUSTOM | — | **11** |

Integrity: 0 orphan TemplateParts, 0 duplicate Codes, 0 GradeRule codes missing BodyPart.

**GradeRule note:** Source has 65 `SpecBodyPartGrading` rows; after G-B + B-A, one rule per `(BodyTypeID, BodyPartCode)` → **11** rules (duplicate grading rows collapsed, keep max `BodyPartGradingID`).

**DefaultBaseSizeId:** all 5 templates NULL — source default size IDs (98/99/100) are **not** in S-A visible SizeRunRotate set.
