# POM / Grading / QC — Implementation Plan

**Version:** 1.2 | **Date:** 2026-07-06 | **Author:** Sean Zhang  
**Platform:** AppAI/AppBuilder (App-netore)  
**Source Reference:** `C:\Users\sean.zhang\dev-space\PLM\docs\pom-grading-qc-architecture.md`

---

## 1. Executive Summary

This plan migrates the legacy PLM POM/Grading/QC module from a first-generation AngularJS 1.x + static C# BL architecture into the AppAI/AppBuilder low-code platform.

**Approach: Hybrid (Config + Custom Code)**

| Layer | Method | Effort |
|---|---|---|
| CRUD forms, list views, search | Platform Form Builder (config) | ~60% |
| Approval workflows, notifications | Platform Workflow Engine (config) | included above |
| Grading math, AQL sampling, QC aggregate | Custom C# BL classes (code) | ~40% |
| Pivot-edit grading grid | New React component (code) | included above |

Hard-coding everything was considered and rejected — the platform's Form Builder produces a complete CRUD screen in hours vs days of hand-coded React + WebAPI. Custom code is limited to logic that genuinely cannot be expressed as config (array-walking algorithms, lookup tables, aggregate roll-ups).

---

## 2. Current State (As-Is) — Key Limitations

| # | Problem | Impact |
|---|---|---|
| L1 | Grade rules baked into body part defaults — no named library | Cannot reuse ASTM rules across styles |
| L2 | Fit iteration stored as flat Revise1…Revise6 columns | Hard limit of 6 rounds; no sign-off workflow |
| L3 | No AQL sampling — all QC is per-garment only | Cannot produce factory audit certificates |
| L4 | Static BL classes, no DI | Cannot unit-test; high coupling |
| L5 | AngularJS 1.x frontend | End-of-life; poor mobile |
| L6 | No spec versioning or audit trail | No compliance evidence trail |
| L7 | QC pass/fail per POM only, no aggregate | Cannot determine order pass/fail |

---

## 3. Platform Capability Assessment

### What the AppAI Platform Handles via Config

| Capability | Platform Tool |
|---|---|
| CRUD forms for any entity | Form Builder (Data Model Design + Form Design) |
| Master-detail layouts | Form Builder — parent/child transaction units |
| Search grids, list views | Report & View (SearchEditor) |
| Approval workflows | Workflow Engine — approval stage type |
| Notifications on status change | Workflow action: NotifyTaskUser |
| Computed fields (shrinkage, recovery, diff) | Formula Engine (DynamicExpresso) |
| Dropdown/enum values | Entity List of Values |
| External API calls from buttons | Command: PluginWebApiCall |

### What Requires Custom Code

| Capability | Why config cannot do it |
|---|---|
| `GradingEngine` — delta ↔ size value math | Array-walking over dynamic size run; formula engine is scalar only |
| Base-size change algorithm | 6-step recomputation across all POM lines |
| `AqlSamplingService` — lot size → sample size | Nested lookup table |
| `QcAggregateService` — order pass/fail roll-up | Cross-garment aggregation query |
| Unit conversion middleware (CM ↔ INCH) | HTTP filter on API boundary |
| `PivotEditGridPanel.tsx` | Dynamic column count per size run; cell-level save to TchpGradeValue |

### PivotEditGrid — Platform Already Has the Infrastructure

`EmAppTransactionGridDisplayType.PivotEditGrid = 3` is defined in `APP.Components.Dto/AppEnums.cs`.  
`AppTransactionStructureLoadBL.cs` builds `AppPivotDto` with `PivotRowFields`, `PivotColumnFields`, `PivotValueFields`, `IsPivotEdit = true` and sends it to the frontend via `DictUnitIdPivotGrid`.

**Gap:** The React runtime component for editable pivot transactions does not yet exist. `PivotViewLayout.tsx` is read-only OLAP only.  
**Action:** Build `PivotEditGridPanel.tsx` — this unlocks the grading grid AND becomes reusable for any future pivot-edit scenario in the platform.

---

## 4. Data Model — New Tables

SQL script: `Document/Design/POM_Grading_QC_NewSchema.sql`

### Foundation Tables (replacing legacy tbl/V2k names)

| New table | Replaces | Purpose |
|---|---|---|
| `TchpSizeRun` | `tblSizeRun` | Named size range (e.g. SCHOOL GIRLS TOPS) |
| `TchpSizeRunSize` | `tblSizeRunRotate` | Individual size within a run (PK: SizeRunSizeId). `SizeLabel` stores the **internal/canonical label** (e.g. "M", "30", "260") — this is the fallback display label used in POM specs, grading tables, and QC when no regional context is set. |
| `TchpBodyPart` | `PdmV2kBodyPart` | POM body part library (Code, Name, Tolerance, GradingPlusValue, GradingMinuValue) |
| `TchpPomTemplate` | `PdmV2kBodyType` | POM template — named collection of body parts |
| `TchpPomTemplatePart` | `PdmV2kBodyTypeDetail` | Junction: template ↔ body part with sort order |

### New Tables (14 total)

```
TchpGradeRuleSet          Named grade rule library (ASTM Women's Misses, etc.)
  └── TchpGradeRule       Rule per body-part Code — loose coupling, no FK

TchpStyleSpec             Spec aggregate root per style (DRAFT → APPROVED → LOCKED)
  ├── TchpStyleSpecDimension  Dimensions active for this spec (MA/UA/XA); IsActive = currently open in grading pivot
  ├── TchpPomSpecLine     One POM line per spec (BaseValue, Tolerance, IsFixed)
  │     └── TchpGradeValue    Adjacent-step delta per size (GradingDelta)
  └── TchpFitRound        Fit iteration round (PENDING → SUBMITTED → APPROVED/REJECTED)
        └── TchpFitMeasurement  Actual measurement per POM per round

TchpQcOrder               QC order (linked to LOCKED StyleSpec)
  ├── TchpQcOrderSize     Selected sizes for QC inspection
  └── TchpQcGarment       Individual sampled garment
        └── TchpQcResult  Measurement: 4 wash stages + computed Shrinkage/Recovery/FinalDiff

TchpSizeRunDimension      Global mapping: TchpSizeRunSize → DimensionCode (one size : one dimension)
TchpSizeSystemMapping     Regional size equivalence per size. SystemCode values: US | EU | UK | JP | CN | INTL
                          SizeLabel here is the regional label (e.g. EU "38", JP "9", CN "165/88A").
                          UI shows TchpSizeRunSize.SizeLabel by default; queries this table only when a regional buyer context is set.
```

### Dimension Design

A **Dimension** segments a size run into sub-ranges that are graded independently because their proportions scale differently (e.g., toddler vs. kids vs. big-girl grading increments differ).

| Dimension | Example sizes | Notes |
|---|---|---|
| MA | 2T, 3T, 4T | Toddler range |
| UA | 4, 5, 6, 6X | Kids range |
| XA | 7, 8, 10, 12, 14 | Big girls range |

**Where dimension lives:**

| Level | Has dimension? | Why |
|---|---|---|
| `TchpBodyPart` / `TchpPomTemplate` | No | Template is dimension-agnostic |
| `TchpGradeRuleSet` | No | Rule sets are completely separate from templates and dimensions |
| `TchpStyleSpec` | Via child table | `TchpStyleSpecDimension` records which dimensions apply and which is active |
| `TchpGradeValue` | Implicitly | Links to `TchpSizeRunSize`; dimension is derivable via `TchpSizeRunDimension` |

**Grading workflow per dimension:**
1. User opens spec → picks Dimension = MA → `TchpStyleSpecDimension.IsActive` set to MA
2. `TchpSizeRunDimension` filters `TchpSizeRunSize` to MA sizes → these become pivot columns
3. User grades, saves `TchpGradeValue` rows for each MA size
4. User switches to Dimension = UA → repeat for UA sizes

**Key naming conventions:**
- `GradingMinuValue` — "Minu" not "Minus" (preserved from legacy `PdmV2kBodyPart` DTO convention)
- `SizeRunSizeId` — PK on `TchpSizeRunSize` (replaces legacy `SizeRunRotateId`)
- `ProductReferenceId` — FK column name for product/style reference (matches legacy PLM convention)
- `BaseSizeDetailId` — FK column on `TchpStyleSpec` pointing to `TchpSizeRunSize`; matches `TchpPomTemplate.DefaultBaseSizeId` convention

---

## 5. Pre-Configuration Checklist

Complete these before opening the Form Builder. Configuration will fail if tables or reference data are missing.

### Step 1 — Run SQL Migration (Required)
```
Run: Document/Design/POM_Grading_QC_NewSchema.sql
Against: tenant database
Verify: all 19 tables created (5 foundation + 14 domain), ASTM seed data inserted
```

### Step 2 — Verify Reference Data
```sql
SELECT COUNT(*) FROM TchpSizeRun         -- must have rows
SELECT COUNT(*) FROM TchpSizeRunSize     -- must have rows
SELECT COUNT(*) FROM TchpBodyPart        -- must have rows
```
If empty, migrate from legacy tables (`tblSizeRun`, `tblSizeRunRotate`, `PdmV2kBodyPart`) or run the old PLM import scripts from `AppReact/ImportDoc/`.

### Step 3 — Register Enum Values in Platform Admin
Navigate to **My Applications → Entity Management → Entity List of Value** and create:

| Enum Name | Values |
|---|---|
| `EmSpecStatus` | DRAFT, APPROVED, LOCKED |
| `EmFitRoundType` | PP1, PP2, PP3, TOP, INTERNAL |
| `EmFitRoundStatus` | PENDING, SUBMITTED, APPROVED, REJECTED |
| `EmAqlLevel` | CRITICAL_1, MAJOR_2_5, MINOR_4_0 |
| `EmQcOrderStatus` | OPEN, IN_PROGRESS, PASSED, FAILED |
| `EmUnitOfMeasure` | CM, INCH |

### Step 4 — Plan Application Structure
Agree on the menu layout before configuring (changes later are expensive):

```
Application: "Tech Pack"
├── POM Library
│   ├── Body Parts          (TchpBodyPart)
│   └── POM Templates       (TchpPomTemplate + TchpPomTemplatePart)
├── Size Setup
│   ├── Size Runs           (TchpSizeRun + TchpSizeRunSize)
│   └── Size Dimensions     (TchpSizeRunDimension — assign MA/UA/XA to each size)
├── Grade Rules
│   └── Grade Rule Sets     (TchpGradeRuleSet + TchpGradeRule)
├── Style Spec
│   └── Spec Editor         (TchpStyleSpec + TchpStyleSpecDimension + TchpPomSpecLine + TchpGradeValue)
│       — dimension selector switches pivot columns between MA/UA/XA
├── Fit Iteration
│   └── Fit Rounds          (TchpFitRound + TchpFitMeasurement)
└── QC
    ├── QC Orders           (TchpQcOrder + TchpQcOrderSize)
    └── QC Garments         (TchpQcGarment + TchpQcResult)
```

### Step 5 — Build PivotEditGridPanel.tsx (Unblocks Grading Screen)
This React component must exist before the Spec Editor screen can be configured.  
See Section 7.2 for spec.

---

## 6. Implementation Phases

### Phase 0 — Foundation (1 week)
**Goal:** Database + DI + unit conversion ready. No user-visible features.

| Task | Type | Effort |
|---|---|---|
| Run `POM_Grading_QC_NewSchema.sql` | SQL | 0.5d |
| Register 6 enum values in platform admin | Config | 0.5d |
| Extract `IGradingEngine` interface; implement `GradingEngine` in `APP.BL` | Code | 2d |
| Add unit conversion action filter (`X-PLM-Unit` HTTP header) | Code | 1d |
| Register new BL services in DI (`services.AddPomServices()`) | Code | 0.5d |
| Unit tests for `GradingEngine` (forward pass, reverse pass, base-size change) | Code | 1d |

**Deliverables:** Tested `GradingEngine` class; `X-PLM-Unit` middleware; all 19 tables in DB.

---

### Phase 1 — Grade Rule Library (1 week)
**Goal:** Named, reusable grade rule sets; ASTM seeds available in UI.

| Task | Type | Effort |
|---|---|---|
| Configure Grade Rule Sets CRUD form (TchpGradeRuleSet) | Config | 0.5d |
| Configure Grade Rules child grid (TchpGradeRule) | Config | 0.5d |
| Add search/list view for Grade Rule Sets | Config | 0.5d |
| Implement `IGradeRuleService.ApplyToSpec()` in BL | Code | 1d |
| Add `POST /api/grading/rule-sets/{id}/apply/{styleId}` endpoint | Code | 0.5d |
| Wire "Apply Rule Set" button in spec editor (PluginWebApiCall command) | Config | 0.5d |
| Verify ASTM seed data in UI | Config | 0.5d |

**Deliverables:** Working Grade Rule Sets CRUD; ASTM Women's Misses and Men's Shirt available in dropdowns.

---

### Phase 2 — Style Spec + Grading Grid (2 weeks)
**Goal:** Spec editor with working pivot grid; approval and lock flow.

| Task | Type | Effort |
|---|---|---|
| Build `PivotEditGridPanel.tsx` React component | Code | 4d |
| Configure StyleSpec header form (status, base size, unit) | Config | 1d |
| Configure PomSpecLine grid columns (left panel: Code, Name, BaseValue, Tolerance, IsFixed) | Config | 1d |
| Set `EmGridViewDisplayType = PivotEditGrid` on TchpGradeValue unit; flag fields | Config | 0.5d |
| Implement `IStyleSpecService` (GetSpec, SaveSpecLine, ApproveSpec, LockSpec) in BL | Code | 2d |
| Add spec API endpoints | Code | 1d |
| Configure Workflow: DRAFT → APPROVED → LOCKED state transitions | Config | 1d |

**Deliverables:** Full spec editor with editable grading pivot grid; approval/lock workflow working.

---

### Phase 3 — Fit Iteration (1 week)
**Goal:** Replace flat Revise1…Revise6 with proper state-machine rounds.

| Task | Type | Effort |
|---|---|---|
| Configure Fit Round form (FitRound + FitMeasurement child grid) | Config | 1d |
| Configure Workflow: PENDING → SUBMITTED → APPROVED/REJECTED | Config | 1d |
| Implement `IFitIterationService` state machine guards in BL | Code | 1.5d |
| Add fit round API endpoints | Code | 1d |
| Configure notification: "spec revised" on APPROVED | Config | 0.5d |
| Configure fit history list view | Config | 0.5d |

**Deliverables:** Unlimited fit rounds with approval sign-off; TOP sample approval locks the spec.

---

### Phase 4 — AQL QC Pipeline (2 weeks)
**Goal:** AQL order model; 4-stage wash measurements; aggregate pass/fail.

| Task | Type | Effort |
|---|---|---|
| Implement `AqlSamplingService.GetSampleSize()` (ISO 2859-1 lookup table) | Code | 0.5d |
| Implement `QcAggregateService` (garment pass/fail; order pass/fail vs Ac) | Code | 2d |
| Implement `IQcService` (CreateOrder, RecordMeasurement, CloseOrder) in BL | Code | 2d |
| Configure QC Order form (header: AQL level, lot qty, factory) | Config | 1d |
| Configure QcOrderSize selector (available/selected grid pair) | Config | 0.5d |
| Configure QcGarment + QcResult measurement grid (4 stage columns) | Config | 1.5d |
| Configure Workflow: OPEN → IN_PROGRESS → PASSED/FAILED | Config | 0.5d |
| Configure QC dashboard search view (factory audit summary) | Config | 1d |

**Deliverables:** Full QC order lifecycle with AQL sampling; factory audit dashboard.

---

### Phase 5 — Multi-Region Size Mapping (3 days)
**Goal:** US/EU/UK/JP/CN/INTL size equivalence; region toggle on spec grid header.

**Design rule:** `TchpSizeRunSize.SizeLabel` is always the fallback. The region toggle calls `ISizeMappingService.GetLabel(sizeRunSizeId, systemCode)` which returns `TchpSizeRunSize.SizeLabel` when no mapping row exists for that `SystemCode`.

| Task | Type | Effort |
|---|---|---|
| Configure SizeSystemMapping CRUD form (child grid under Size Runs) | Config | 0.5d |
| Populate SystemCode dropdown: `US \| EU \| UK \| JP \| CN \| INTL` | Config | 0.25d |
| Implement `ISizeMappingService.GetLabel(sizeRunSizeId, systemCode)` — fallback to `TchpSizeRunSize.SizeLabel` if no mapping row | Code | 1d |
| Add region toggle to spec grid header; pass SystemCode to column headers | Code | 0.5d |

---

### Phase 6 — Polish & Export (1 week)
**Goal:** Tech pack PDF/Excel export; mobile-responsive QC entry.

| Task | Type | Effort |
|---|---|---|
| Implement `TechPackExportService` (PDF/Excel spec sheet) | Code | 3d |
| Mobile-responsive measurement input (QC factory floor use) | Code | 2d |

---

## 7. Custom Code Specifications

### 7.1 GradingEngine (`APP.BL/POM/GradingEngine.cs`)

```csharp
public interface IGradingEngine
{
    // Forward pass: deltas + baseValue → full size value array
    IReadOnlyList<decimal> ComputeSizeValues(
        decimal baseValue, int baseSizeIndex, IReadOnlyList<decimal> gradingDeltas);

    // Reverse pass: size values → adjacent-step deltas
    IReadOnlyList<decimal> ComputeGradingDeltas(
        IReadOnlyList<decimal> sizeValues, int baseSizeIndex);

    // Apply named rule set → derive per-size deltas
    IReadOnlyList<decimal> ApplyGradeRuleSet(
        TchpGradeRuleSet ruleSet, string bodyPartCode,
        IReadOnlyList<TchpSizeRunSize> sizes, int baseSizeIndex);
}
```

**Invariants:**
- Delta at `baseSizeIndex` is always 0
- `IsFixed = true` → all size values equal `baseValue`
- `baseValue` must be positive and non-zero
- Max 20 sizes per spec (`EmApplicationSettings.MaxiumGradingSizeCounter`)

### 7.2 PivotEditGridPanel.tsx

**Purpose:** Editable FlexGrid with dynamic size columns, reading `AppPivotDto` from `DictUnitIdPivotGrid`.

**Data flow:**
```
Load:
  GET /api/styles/{styleId}/spec
    → PomSpecLines[] with GradeValues[] per line
    → transform to wide format: one column per TchpSizeRunSize
    → CollectionView rows = POM lines

Edit cell (e.g. row "Waist", column "S"):
    → identify PomSpecLineId + SizeRunSizeId
    → PATCH /api/spec-lines/{pomLineId}/grade-values
    → GradingEngine recomputes adjacent deltas
    → return updated row

Base size column (locked, highlighted):
    → GradingDelta = 0, isReadOnly = true
    → FlexGridCellTemplate: bg-amber-400 when sizeRunSizeId === baseSizeId
```

**Field roles (set in Data Model designer):**
- `IsPivotRow`: BodyPartCode, BodypartAliasName, BaseValue, Tolerance, IsFixed
- `IsPivotColumn`: SizeRunSizeId (displays as `TchpSizeRunSize.SizeLabel`)
- `IsPivotValue`: GradingDelta

### 7.3 AqlSamplingService (`APP.BL/QC/AqlSamplingService.cs`)

ISO 2859-1 Level II — lot size → letter code → sample size lookup. Returns `int sampleSize` and `int acceptanceNumber` for a given `(lotSize, AqlLevel)`.

### 7.4 Unit Conversion Middleware

HTTP action filter reads `X-PLM-Unit` request header.  
- `INCH` request → convert all decimal measurement fields to CM before service call  
- Response → convert back to INCH  
- No conversion logic inside service or domain layer  
- Precision: 3 decimal places for measurements, 4 for percentages

---

## 8. Key Business Rules (Do Not Break)

### Grading
1. Delta at base size is always 0 — never store non-zero at `baseSizeIndex`
2. Deltas are adjacent-step, not cumulative from base
3. `IsFixed = true` → all sizes equal `BaseValue`; grade rules ignored
4. Base size is shared across all POMs in a spec

### Fit Iteration
5. New round can only be created when no round is PENDING or SUBMITTED
6. Spec revisions locked once any round is APPROVED
7. TOP sample approval → `StyleSpec.SpecStatus = LOCKED`
8. FinalSpec = last APPROVED round's `ActualValue`, fallback to `BaseValue`

### QC
9. `FinalDiff` is null when `AfterIronValue` is missing — not zero
10. Pass/fail uses locked spec value snapshot — not the live spec
11. `Shrinkage = BeforeWashValue − AfterWashValue`
12. Final QC pass is based on `AfterIronValue` (post-recovery)

### Units
13. Storage always CM — INCH is display only
14. Conversion only at API boundary — never inside BL or domain

---

## 9. File Locations

| Artifact | Path |
|---|---|
| SQL migration script | `Document/Design/POM_Grading_QC_NewSchema.sql` |
| Architecture spec (new platform) | `Document/Design/pom-grading-qc-architecture.md` |
| Old PLM concepts reference | `C:\Users\sean.zhang\dev-space\PLM\docs\pom-grading-qc-concepts.md` |
| Old PLM architecture reference | `C:\Users\sean.zhang\dev-space\PLM\docs\pom-grading-qc-architecture.md` |
| Old PLM BL (grading math) | `C:\Users\sean.zhang\dev-space\PLM\Com.Visual2000.BL\ProductReference\PdmSpecGradingFitGridBL.cs` |
| Old PLM BL (QC) | `C:\Users\sean.zhang\dev-space\PLM\Com.Visual2000.BL\ProductReference\PdmProductQcSizeBL.cs` |
| Old PLM BL (POM) | `C:\Users\sean.zhang\dev-space\PLM\Com.Visual2000.BL\ProductReference\PdmPOMBL.cs` |
| This implementation plan | `Document/Design/POM_Grading_QC_Implementation_Plan.md` |

---

## 10. Decisions Log (this session — 2026-06-16)

| Decision | Detail |
|---|---|
| Table prefix | `Tchp` (TechPack) — replaces old `Pdm` across all new tables |
| Foundation tables | 5 new tables replace legacy `tbl*` / `PdmV2k*` names (see Section 4) |
| Total schema | 19 tables: 5 foundation + 14 domain |
| Dimension scope | Lives on `TchpStyleSpec` only via `TchpStyleSpecDimension`; NOT on POM template or grade rules |
| Grade rules on template | No — completely separate; applied on demand to spec only |
| Dimension-to-size mapping | `TchpSizeRunDimension` — global per size run; UQ on `SizeRunSizeId` (one size = one dimension) |
| `SizeRunRotateId` renamed | `SizeRunSizeId` — PK on `TchpSizeRunSize`; updated in all FK columns |
| `TchpSizeRunSize.SizeLabel` role | Stores the **internal/canonical label** (e.g. "M", "30", "260") — used as the fallback display label in POM spec, grading grid, and QC when no regional context is known. Not tied to any regional standard. |
| `TchpSizeSystemMapping.SystemCode` values | `US \| EU \| UK \| JP \| CN \| INTL` — discriminator for regional sizing standard. `SizeLabel` in that row is the buyer-facing label for that region. INTL row is optional if `TchpSizeRunSize.SizeLabel` already serves as the neutral label. |
| `SystemCode = INTL` guidance | Do NOT add an INTL row that duplicates `TchpSizeRunSize.SizeLabel` — it is redundant. Only add INTL if the internal label differs from what buyers expect to see. |

---

## 11. Next Actions

| Priority | Action | Owner | Blocked by |
|---|---|---|---|
| 1 | Run `POM_Grading_QC_NewSchema.sql` on tenant DB | Dev | Nothing |
| 2 | Verify `TchpSizeRun`, `TchpSizeRunSize`, `TchpBodyPart` rows exist; migrate from legacy tables if empty | Dev | Step 1 |
| 3 | Populate `TchpSizeRunDimension` — assign dimension codes (MA/UA/XA) to each size in every size run | Dev | Step 2 |
| 4 | Register 6 enum values in platform admin | Dev | Step 1 |
| 5 | Build `PivotEditGridPanel.tsx` | Dev | Nothing (parallel with 1–4) |
| 6 | Implement `GradingEngine` + unit tests (Phase 0) | Dev | Nothing (parallel with 1–4) |
| 7 | Configure Size Runs + Size Dimensions CRUD (Size Setup menu) | Dev | Steps 1–3 |
| 8 | Configure Grade Rule Sets CRUD (Phase 1) | Dev | Steps 1–4 |
| 9 | Configure Style Spec + dimension selector (Phase 2) | Dev | Steps 1–4 + Step 5 |
| 10 | Configure Fit Round workflow (Phase 3) | Dev | Steps 1–4 |
