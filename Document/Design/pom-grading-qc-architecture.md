# PLM: POM, Grading & QC — Re-Design Architecture

**Version:** 1.0 | **Author:** Architecture Review | **Date:** 2026-06-15  
**Scope:** Point of Measure (POM), Size Grading, Fit Iteration, and Quality Control modules

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Industry Standards Reference](#2-industry-standards-reference)
3. [Current Architecture (As-Is)](#3-current-architecture-as-is)
4. [Gap Analysis](#4-gap-analysis)
5. [Target Domain Model (To-Be)](#5-target-domain-model-to-be)
6. [Grading Engine Re-Design](#6-grading-engine-re-design)
7. [Fit Iteration Workflow](#7-fit-iteration-workflow)
8. [QC Pipeline Re-Design](#8-qc-pipeline-re-design)
9. [Service & API Layer](#9-service--api-layer)
10. [Data Model (Entity Diagram)](#10-data-model-entity-diagram)
11. [Phased Implementation Plan](#11-phased-implementation-plan)
12. [Key Invariants & Business Rules](#12-key-invariants--business-rules)

---

## 1. Executive Summary

The current POM/Grading/QC implementation is functionally correct but reflects a first-generation architecture: static helper classes, direct data-adapter access, and an AngularJS 1.x frontend. As the fashion industry moves toward standardized data exchange (Tech Pack interchange, digital samples, 3D fit), the platform needs a domain-model-first redesign that:

- Aligns with **ASTM D5585 / ISO 8559** measurement standards
- Separates **grade rules** (reusable library) from **grade instances** (applied per style)
- Introduces **AQL-based QC sampling** alongside per-garment measurement
- Expresses the **fit iteration loop** as a first-class state machine
- Enables **downstream integrations** (factory portals, 3D CAD, ERP WMS)

The redesign is additive — it preserves backward-compatible storage while introducing new service contracts.

---

## 2. Industry Standards Reference

### 2.1 Measurement Standards

| Standard | Scope | Relevance |
|----------|-------|-----------|
| ASTM D5585 | Adult female misses body measurements | Base size tables, grade increments |
| ASTM D6960 | Adult male body measurements | Base size tables |
| ASTM D7878 | Children's body measurements | Pediatric size runs |
| ISO 8559-1 | Garment construction — vocabulary | POM naming conventions |
| ISO 8559-2 | Garment dimensions — presentation | Spec sheet format |
| ANSI/AATCC TM135 | Dimensional change in laundering | Before/after wash QC |
| ISO 6330 | Textile washing procedures | Wash testing protocol |

### 2.2 Grade Increment Reference (ASTM)

```
Women's (Misses, Half-step system):
  Bust / Chest:     2.0 cm per size step (full)  → 1.0 cm half-step
  Waist:            2.0 cm per step
  Hip:              2.0 cm per step
  Front/Back rise:  0.6 cm per step
  Inseam:           0.6 cm per step
  Shoulder width:   0.6 cm per step
  Sleeve length:    0.6 cm per step (height-related)
  Neck width:       0.3 cm per step

Men's (Shirt):
  Chest:            2.5 cm per step
  Waist:            2.5 cm per step
  Seat:             2.5 cm per step
  Back length:      1.5 cm per step
  Sleeve length:    1.0 cm per step
```

### 2.3 QC Sampling — AQL (ISO 2859-1)

Fashion production uses **Acceptable Quality Level** sampling, not 100% inspection:

| AQL Level | Usage | Example defect |
|-----------|-------|----------------|
| 1.0 (Critical) | Safety hazards, functional failures | Choking hazard hardware |
| 2.5 (Major) | Measurements outside tolerance | Garment fails spec |
| 4.0 (Minor) | Cosmetic, customer dissatisfaction | Pilling, color variance |

Sampling plan selects the **number of garments** to inspect per lot size.

### 2.4 Tech Pack Lifecycle (Industry Standard Flow)

```
Design Brief
    ↓
Initial Spec (base size only)
    ↓
Grade Rules Applied → Full Size Run Spec
    ↓
PP Sample 1 → Fit Evaluation → Revision
    ↓ (iterate up to N rounds)
PP Sample Approved
    ↓
TOP Sample → Final QC lock
    ↓
Production QC (Before Wash → After Wash → After Iron)
    ↓
Shipment Pass / Fail
```

---

## 3. Current Architecture (As-Is)

### 3.1 Layering

```
┌──────────────────────────────────────────────────┐
│  Frontend: AngularJS 1.x (Wijmo FlexGrid)        │
│  pomEditorCtrl.js / pomManagementCtrl.js         │
└─────────────────┬────────────────────────────────┘
                  │ HTTP REST (JSON)
┌─────────────────▼────────────────────────────────┐
│  WebApi Controllers                               │
│  DesignController / ReferenceController           │
└─────────────────┬────────────────────────────────┘
                  │ static call
┌─────────────────▼────────────────────────────────┐
│  Service Facades (static singletons)              │
│  DesignServiceFacade / TechPackServiceFacade      │
└─────────────────┬────────────────────────────────┘
                  │ static call
┌─────────────────▼────────────────────────────────┐
│  Business Logic (static classes)                  │
│  PomHelper · PdmPOMBL · PdmSpecGradingFitGridBL  │
│  PdmProductQcSizeBL                              │
└─────────────────┬────────────────────────────────┘
                  │ DataAccessAdapter (LLBLGen)
┌─────────────────▼────────────────────────────────┐
│  SQL Server Database                              │
│  PdmV2kBodyPart · PdmV2kBodyType · QcSize        │
└──────────────────────────────────────────────────┘
```

### 3.2 Key Entities (Current)

| Entity | Role |
|--------|------|
| `PdmV2kBodyPart` | POM point definition (code, name, tolerance, default grade) |
| `PdmV2kBodyType` | POM Template (named collection of body parts) |
| `PdmV2kBodyTypeDetail` | Junction: template ↔ body part with sort order |
| `PdmV2kSpecBodyPartGrading` | Grade rule per size attached to a template detail |
| `TblSizeRun` | Named size range (e.g., "Women's Standard") |
| `TblSizeRunRotate` | Individual size label within a run (XS, S, M …) |
| `PdmProductQcSize` | Which sizes are selected for QC in a tech pack |

### 3.3 Calculation Engine (PomHelper.cs)

The static `PomHelper` class handles all math:

```
Grading delta storage:
  XS    S    M    L    XL   XXL
 -2.0  -2.0  [0]  +2.0 +2.0 +2.0
               ↑ base = 0 (always)

Forward pass (grading → sizes):
  Walk outward from base position using cumulative delta sum

Reverse pass (sizes → grading):
  gradingValue[i] = sizeValue[i+1] − sizeValue[i]
  gradingValue[baseIndex] = 0
```

### 3.4 Current Limitations

| # | Pain Point | Impact |
|---|-----------|--------|
| L1 | Grade rules embedded in body part (flat defaults) — no named grade rule library | Cannot reuse "ASTM Women's" rule across styles |
| L2 | No AQL sampling model — all QC is per-garment measurement only | Cannot run statistically valid factory audits |
| L3 | Fit iteration stored as flat columns (Revise1…Revise6) | Hard limit of 6 rounds; no approval sign-off workflow |
| L4 | Static BL classes — no dependency injection | Cannot unit-test in isolation; coupling is high |
| L5 | AngularJS 1.x frontend | End-of-life framework; no SSR; poor mobile |
| L6 | Asymmetric grading (GradingPlusValue / GradingMinusValue) not exposed in templates | Half-step and asymmetric grades require workarounds |
| L7 | No multi-region size mapping (US 6 = EU 36 = JP 9) | Global brand support limited |
| L8 | Unit conversion happens in BL, not at a boundary layer | Conversion bugs spread across layers |
| L9 | QC pass/fail is per-POM; no aggregate pass/fail per garment per AQL | Cannot produce factory audit certificates |
| L10 | No version/audit trail on spec revisions | No evidence trail for compliance |

---

## 4. Gap Analysis

```
Industry Standard Concept          Current Support      Gap
─────────────────────────────────────────────────────────────────
Named grade rule library            ✗ (flat defaults)   HIGH
Per-POM asymmetric grading          Partial             MED
Fixed POMs (no-grade flag)          ✓                   —
Multi-system size mapping           ✗                   HIGH
AQL sampling plan                   ✗                   HIGH
Shrinkage / wash testing            ✓ (formula exists)  —
Fit iteration state machine         Partial (6 revise)  MED
Approval workflow (sign-off)        ✗                   HIGH
Aggregate QC pass/fail              ✗                   MED
Spec version history                ✗                   HIGH
Digital tech pack export (PDF/Excel)Partial             MED
3D integration hooks                ✗                   LOW
Unit-at-boundary conversion         ✗                   MED
```

---

## 5. Target Domain Model (To-Be)

### 5.1 Bounded Contexts

```
┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐
│    POM LIBRARY       │  │   SPEC & GRADING     │  │   QC & SAMPLING      │
│                      │  │                      │  │                      │
│  · Body Part         │  │  · Style Spec        │  │  · QC Order          │
│  · POM Group         │  │  · Grade Rule Set    │  │  · AQL Plan          │
│  · POM Template      │  │  · Spec Version      │  │  · QC Measurement    │
│  · Grade Rule Lib    │  │  · Fit Round         │  │  · Defect Record     │
│  · Size System       │  │  · Approval          │  │  · Wash Test         │
└──────────────────────┘  └──────────────────────┘  └──────────────────────┘
           │                          │                          │
           └──────────────────────────┴──────────────────────────┘
                              Shared Kernel
                        (Size Run, Unit System,
                         Product Reference, User)
```

### 5.2 Core Aggregate: Style Spec

```
StyleSpec (Aggregate Root)
├── styleId            FK → Product/Style
├── sizeRunId          FK → SizeRun
├── baseSizeId         FK → SizeRunRotate
├── unitOfMeasure      CM | INCH
├── status             DRAFT | APPROVED | LOCKED
├── version            integer (auto-increment on approval)
│
├── PomSpecLines[]     one per body part
│   ├── bodyPartId
│   ├── gradeRuleSetId FK → GradeRuleSet (nullable = use defaults)
│   ├── baseValue      decimal (base size measurement)
│   ├── tolerance      decimal
│   ├── isFixed        bool (no grading applied)
│   └── GradeValues[]  one entry per size in the run
│       ├── sizeRotateId
│       └── delta      decimal (Δ from adjacent size toward base)
│
└── FitRounds[]        ordered list
    ├── roundNumber    int
    ├── roundType      PP_SAMPLE_1 | PP_SAMPLE_2 | TOP_SAMPLE | …
    ├── status         PENDING | SUBMITTED | APPROVED | REJECTED
    ├── submittedBy    userId
    ├── approvedBy     userId (nullable)
    └── FitMeasurements[]
        ├── pomSpecLineId
        └── actualValue decimal
```

### 5.3 Core Aggregate: Grade Rule Library

```
GradeRuleSet (Aggregate Root)
├── name               e.g., "ASTM Women's Misses"
├── description
├── standard           ASTM | ISO | CUSTOM
├── createdBy
└── GradeRules[]
    ├── bodyPartCode   matches POM code (loose coupling)
    ├── plusDelta      decimal per step going up
    ├── minusDelta     decimal per step going down
    └── isSymmetric    bool (shortcut: plus == minus)
```

Decoupled from body parts by **code** (not FK) — allows template-agnostic rules.

### 5.4 Core Aggregate: QC Order

```
QcOrder (Aggregate Root)
├── styleId            FK → Style
├── specVersionId      FK → StyleSpec version
├── lotNumber          string
├── factoryId          FK → Vendor
├── aqlLevel           CRITICAL_1 | MAJOR_2_5 | MINOR_4_0
├── lotQuantity        int
├── sampleSize         int (computed from AQL table)
├── status             OPEN | IN_PROGRESS | PASSED | FAILED
│
├── SelectedSizes[]    sizes being QC'd
│   └── sizeRotateId
│
└── QcGarments[]       one per sampled garment
    ├── garmentSerial  string
    └── QcMeasurements[]
        ├── pomSpecLineId
        ├── beforeWash   decimal?
        ├── afterWash    decimal?
        ├── afterIron    decimal?
        └── Computed:
            ├── shrinkage     = beforeWash − afterWash
            ├── recovery      = afterIron − afterWash
            ├── diff          = afterIron − specValue
            └── pass          = |diff| ≤ tolerance
```

### 5.5 Size System (Shared Kernel)

```
SizeSystem
├── systemCode    US | EU | UK | JP | CN | INTL
└── SizeMappings[]
    ├── sizeRunId
    ├── sizeLabel  (e.g., "6")
    └── equivalents
        ├── US: "6"
        ├── EU: "36"
        └── JP: "9"
```

---

## 6. Grading Engine Re-Design

### 6.1 Separation of Concerns

```
Current (mixed):
  PomHelper.cs — defaults baked into BodyPart entity

Proposed (separated):
  GradeRuleSet  — named, reusable rule library
  StyleSpec     — applies rules per POM line
  GradingEngine — pure calculation (no IO)
```

### 6.2 GradingEngine Interface

```csharp
public interface IGradingEngine
{
    /// Given grading deltas + base value, produce full size value array.
    IReadOnlyList<SizeValue> ComputeSizeValues(
        decimal baseValue,
        int baseSizeIndex,
        IReadOnlyList<decimal> gradingDeltas);   // adjacent-step deltas

    /// Given full size values, produce grading deltas.
    IReadOnlyList<decimal> ComputeGradingDeltas(
        IReadOnlyList<decimal> sizeValues,
        int baseSizeIndex);

    /// Apply a GradeRuleSet to derive per-size deltas from rule increments.
    IReadOnlyList<decimal> ApplyGradeRuleSet(
        GradeRuleSet ruleSet,
        string bodyPartCode,
        IReadOnlyList<SizeRunRotate> sizes,
        int baseSizeIndex);
}
```

**Invariants enforced by the engine:**
1. Delta at `baseSizeIndex` is always `0`
2. All sizes must belong to the same `SizeRun`
3. `baseValue` must be positive and non-zero
4. Fixed POMs (`isFixed = true`) return uniform size values equal to `baseValue`

### 6.3 Grade Application Flow

```
                     ┌──────────────────┐
                     │  GradeRuleSet    │
                     │  (library)       │
                     └────────┬─────────┘
                              │ lookup by bodyPartCode
                              ▼
StyleSpec.PomSpecLine  ─→  GradingEngine.ApplyGradeRuleSet()
   baseValue                    │
   baseSizeIndex                │ deltas[]
                                ▼
                         GradingEngine.ComputeSizeValues()
                                │
                                ▼
                         SizeValue[] per size
                         (displayed in spec grid)
```

### 6.4 Asymmetric Grading

Stored as directed deltas per step, not per size:

```
PomSpecLine.GradeValues:
  Size  | Delta (toward smaller)
  ------|----------------------
  XS    | −1.0   ← going down: −1.0 per step
  S     | −1.0
  M     |  0     ← base
  L     | +2.0   ← going up:   +2.0 per step
  XL    | +2.0
  XXL   | +2.0
```

### 6.5 Base Size Change Algorithm

When user changes base size from M → L:

```
1. Compute all absolute size values using current base (M)
2. Shift base index to L (new baseSizeIndex)
3. Set sizeValues[L] = existing sizeValues[L] (unchanged)
4. Recompute all deltas: delta[i] = sizeValues[i+1] − sizeValues[i]
5. delta[baseSizeIndex] = 0
6. Store new deltas; new baseValue = sizeValues[L]
```

This is a pure in-memory operation — no data is lost.

---

## 7. Fit Iteration Workflow

### 7.1 State Machine

```
                  ┌──────────────────────────────────────────┐
                  │              FIT ROUND                   │
                  │                                          │
     createRound()│    submitMeasurements()   approve()      │
PENDING ──────────┤──────────► SUBMITTED ─────────► APPROVED │
                  │                │                         │
                  │                │ reject()                │
                  │                ▼                         │
                  │            REJECTED ──► new round        │
                  └──────────────────────────────────────────┘
```

**Rules:**
- A new round can only be created when no round is `PENDING` or `SUBMITTED`
- Spec revisions are locked once a round is `APPROVED`
- `TOP_SAMPLE` approval transitions `StyleSpec.status` → `LOCKED`
- Locked specs seed `QcOrder.specVersionId`

### 7.2 Fit Round Data Model

```
FitRound
├── id
├── styleSpecId
├── roundNumber        auto-increment within spec
├── roundType          PP1 | PP2 | PP3 | TOP | INTERNAL
├── status             PENDING | SUBMITTED | APPROVED | REJECTED
├── comment            text (tech designer notes)
├── submittedAt        timestamp
├── submittedBy        userId
├── approvedAt         timestamp (nullable)
├── approvedBy         userId (nullable)
├── rejectionReason    text (nullable)
└── FitMeasurements[]
    ├── pomSpecLineId
    ├── actualValue       decimal
    └── Computed:
        ├── specValue     (from parent StyleSpec at round creation time)
        ├── difference    = actualValue − specValue  (null if missing)
        └── withinTol     = |difference| ≤ tolerance
```

**FinalSpec derivation:** last APPROVED round's `actualValue`; falls back to `StyleSpec.baseValue`.

### 7.3 Difference Display Modes

```
DELTA mode:      difference = actualValue − specValue       (raw cm or inch)
PERCENT mode:    difference = (actual − spec) / spec        (4 decimal places)
```

Display mode is a **user preference per style**, not a calculation mode — underlying storage is always raw delta.

---

## 8. QC Pipeline Re-Design

### 8.1 AQL Sampling Table (ISO 2859-1, Level II)

```csharp
public static int GetSampleSize(int lotSize, AqlLevel level)
{
    // Letter code from lot size
    var letter = lot size < 51   ? 'D'
               : lot size < 91   ? 'E'
               : lot size < 151  ? 'F'
               : lot size < 281  ? 'G'
               : lot size < 501  ? 'H'
               : lot size < 1201 ? 'J'
               :                   'K';

    // AQL 2.5 (Major) sample sizes
    return letter switch {
        'D' => 8,  'E' => 13, 'F' => 20,
        'G' => 32, 'H' => 50, 'J' => 80, 'K' => 125, _ => 125
    };
}
```

### 8.2 QC Measurement Stages

```
For each sampled garment × each selected POM × each selected size:

Stage 1: Production (no wash)
  → QcMeasurement.productionValue
  → diff1 = productionValue − specGradingValue
  → pass1 = |diff1| ≤ tolerance

Stage 2: Before wash
  → QcMeasurement.beforeWashValue
  → diff2 = beforeWashValue − specGradingValue

Stage 3: After wash
  → QcMeasurement.afterWashValue
  → shrinkage = beforeWashValue − afterWashValue

Stage 4: After iron
  → QcMeasurement.afterIronValue
  → recovery  = afterIronValue − afterWashValue
  → diff4     = afterIronValue − specGradingValue
  → pass4     = |diff4| ≤ tolerance  ← FINAL QC PASS
```

All four stages are optional — `null` means "not measured", not "zero difference".

### 8.3 Aggregate Pass/Fail

```
Garment passes QC if:
  ALL measured POMs at ALL selected sizes have pass4 = true

Order passes QC if:
  # failed garments ≤ Ac (acceptance number from AQL table)

Example:
  Lot = 500 garments, AQL Major 2.5 → sample 50 garments
  Ac = 3 (from AQL table)
  If 2 garments fail → ORDER PASSES
  If 4 garments fail → ORDER FAILS → reinspect or reject lot
```

### 8.4 Defect Classification

| Class | Measurement Criterion | Examples |
|-------|----------------------|---------|
| Critical | Any safety/function measurement OOT | Pocket hole on children's wear |
| Major | Measurement outside tolerance by > 50% | Chest 52 vs spec 50 ± 1 = +3 out |
| Minor | Measurement outside tolerance by ≤ 50% | Hem 61.4 vs spec 61 ± 1 = borderline |

### 8.5 QC Result Entity

```
QcResult
├── qcOrderId
├── qcGarmentId
├── pomSpecLineId
├── sizeRotateId
├── productionValue    decimal?
├── beforeWashValue    decimal?
├── afterWashValue     decimal?
├── afterIronValue     decimal?
├── specValue          decimal  (snapshot from StyleSpec at QC time)
├── tolerance          decimal  (snapshot)
├── shrinkage          decimal? (computed)
├── recovery           decimal? (computed)
├── finalDiff          decimal? (computed)
├── pass               bool?    (computed, null if not yet measured)
└── defectClass        CRITICAL | MAJOR | MINOR | null
```

---

## 9. Service & API Layer

### 9.1 Service Boundaries

```
IPomLibraryService
  GetBodyParts(filter)  → PagedResult<BodyPartDto>
  GetBodyPart(id)       → BodyPartDetailDto
  SaveBodyPart(dto)     → BodyPartDetailDto
  GetPomTemplates()     → IEnumerable<PomTemplateDto>
  GetPomTemplate(id)    → PomTemplateDetailDto
  SavePomTemplate(dto)  → PomTemplateDetailDto

IGradeRuleService
  GetGradeRuleSets()    → IEnumerable<GradeRuleSetDto>
  GetGradeRuleSet(id)   → GradeRuleSetDetailDto
  SaveGradeRuleSet(dto) → GradeRuleSetDetailDto
  ApplyToSpec(styleId, gradeRuleSetId) → void

IStyleSpecService
  GetSpec(styleId)           → StyleSpecDto
  SaveSpecLine(dto)          → PomSpecLineDto
  ApproveSpec(specVersionId) → void
  LockSpec(specVersionId)    → void
  ExportTechPack(styleId)    → TechPackDocument

IFitIterationService
  CreateFitRound(styleId, roundType)  → FitRoundDto
  SubmitMeasurements(roundId, data[]) → FitRoundDto
  ApproveFitRound(roundId, comment)   → FitRoundDto
  RejectFitRound(roundId, reason)     → FitRoundDto
  GetFitHistory(styleId)              → IEnumerable<FitRoundDto>

IQcService
  CreateQcOrder(dto)                → QcOrderDto
  RecordMeasurement(orderId, data)  → QcResultDto
  CloseQcOrder(orderId)             → QcOrderSummaryDto
  GetAqlSampleSize(lotSize, level)  → int
```

### 9.2 REST API Endpoints

```
POM Library
  GET    /api/pom/body-parts
  GET    /api/pom/body-parts/{id}
  POST   /api/pom/body-parts
  PUT    /api/pom/body-parts/{id}
  GET    /api/pom/templates
  GET    /api/pom/templates/{id}
  POST   /api/pom/templates

Grade Rules
  GET    /api/grading/rule-sets
  GET    /api/grading/rule-sets/{id}
  POST   /api/grading/rule-sets
  POST   /api/grading/rule-sets/{id}/apply/{styleId}

Style Spec
  GET    /api/styles/{styleId}/spec
  PUT    /api/styles/{styleId}/spec/lines/{pomLineId}
  POST   /api/styles/{styleId}/spec/approve
  GET    /api/styles/{styleId}/spec/export

Fit Iteration
  GET    /api/styles/{styleId}/fit-rounds
  POST   /api/styles/{styleId}/fit-rounds
  POST   /api/styles/{styleId}/fit-rounds/{roundId}/measurements
  POST   /api/styles/{styleId}/fit-rounds/{roundId}/approve
  POST   /api/styles/{styleId}/fit-rounds/{roundId}/reject

QC
  POST   /api/qc/orders
  GET    /api/qc/orders/{orderId}
  POST   /api/qc/orders/{orderId}/measurements
  POST   /api/qc/orders/{orderId}/close
  GET    /api/qc/aql-sample-size?lotSize={n}&level={l}
```

### 9.3 Unit Conversion Boundary

All units are stored and computed in **CM**. Conversion happens at the **API boundary only**:

```
Request:
  X-PLM-Unit: INCH   → middleware converts all measurement fields to CM before service call

Response:
  X-PLM-Unit: INCH   → middleware converts all measurement fields back to INCH

Rules:
  - No conversion logic inside service or domain layer
  - Precision: 3 decimal places for measurements, 4 for percentages
  - Culture-aware decimal parsing in request deserialization
```

---

## 10. Data Model (Entity Diagram)

```
┌─────────────┐       ┌──────────────────┐       ┌──────────────────┐
│  SizeRun    │──1:N──│ SizeRunRotate    │       │  GradeRuleSet    │
│  id         │       │  id              │       │  id              │
│  code       │       │  sizeLabel       │       │  name            │
│  name       │       │  sortOrder       │       │  standard        │
└─────────────┘       │  sizeRunId       │       └────────┬─────────┘
                       └──────────────────┘                │ 1:N
                                                   ┌───────▼──────────┐
┌─────────────┐       ┌──────────────────┐         │  GradeRule       │
│  BodyPart   │──N:M──│ PomTemplate      │         │  id              │
│  id         │       │  id              │         │  bodyPartCode    │
│  code       │       │  name            │         │  plusDelta       │
│  name       │       │  defaultSizeRunId│         │  minusDelta      │
│  tolerance  │       │  defaultBaseSizeId│         └──────────────────┘
│  groupId    │       └──────────────────┘
└─────────────┘
                                │ 1:N
                       ┌────────▼─────────┐
                       │   StyleSpec      │
                       │   id             │
                       │   styleId        │
                       │   sizeRunId      │
                       │   baseSizeId     │
                       │   version        │
                       │   status         │
                       └────────┬─────────┘
                                │ 1:N
              ┌─────────────────┴──────────────────┐
              │                                    │
   ┌──────────▼──────────┐             ┌───────────▼──────────┐
   │  PomSpecLine        │             │   FitRound           │
   │  id                 │             │   id                 │
   │  bodyPartId         │             │   roundNumber        │
   │  gradeRuleSetId     │             │   roundType          │
   │  baseValue          │             │   status             │
   │  tolerance          │             │   approvedBy         │
   │  isFixed            │             └───────────┬──────────┘
   └──────────┬──────────┘                         │ 1:N
              │ 1:N                      ┌──────────▼──────────┐
   ┌──────────▼──────────┐              │  FitMeasurement     │
   │  GradeValue         │              │  pomSpecLineId       │
   │  sizeRotateId       │              │  actualValue         │
   │  delta              │              └─────────────────────┘
   └─────────────────────┘

                       ┌──────────────────┐
                       │   QcOrder        │
                       │   id             │
                       │   styleSpecId    │
                       │   lotNumber      │
                       │   aqlLevel       │
                       │   sampleSize     │
                       │   status         │
                       └────────┬─────────┘
                                │ 1:N
                       ┌────────▼─────────┐
                       │   QcGarment      │
                       │   id             │
                       │   garmentSerial  │
                       └────────┬─────────┘
                                │ 1:N
                       ┌────────▼─────────┐
                       │   QcResult       │
                       │   pomSpecLineId  │
                       │   sizeRotateId   │
                       │   beforeWash     │
                       │   afterWash      │
                       │   afterIron      │
                       │   pass           │
                       └──────────────────┘
```

---

## 11. Phased Implementation Plan

### Phase 0 — Foundation (2 weeks)

**Goal:** No user-visible changes; internal refactoring to enable future phases.

| Task | Effort | Deliverable |
|------|--------|-------------|
| Extract `IGradingEngine` interface from `PomHelper` static methods | 1d | Testable pure calculation class |
| Add unit tests for grading calculation (delta ↔ size value) | 1d | Test coverage for edge cases |
| Introduce unit conversion middleware (HTTP header-based) | 2d | `X-PLM-Unit` header support |
| Document DB schema for current tables | 1d | `docs/db-schema-as-is.md` |
| Set up DI container registration for BL classes | 2d | `services.AddPomServices()` registration |

### Phase 1 — Grade Rule Library (3 weeks)

**Goal:** Named, reusable grade rule sets; decouple from flat body part defaults.

| Task | Effort | Deliverable |
|------|--------|-------------|
| DB migration: `GradeRuleSet` + `GradeRule` tables | 2d | Migration script |
| `IGradeRuleService` implementation + CRUD API | 3d | `/api/grading/rule-sets` endpoints |
| Seed ASTM Women's Misses and ASTM Men's rule sets | 1d | Reference data migration |
| Update grading grid UI to show/apply named rule sets | 3d | Grade rule dropdown in spec editor |
| Migrate existing per-body-part defaults to rule sets | 2d | Data migration script |

### Phase 2 — Style Spec Versioning (3 weeks)

**Goal:** Immutable spec versions; approval workflow.

| Task | Effort | Deliverable |
|------|--------|-------------|
| DB migration: `StyleSpec` + `PomSpecLine` + `GradeValue` tables | 2d | Migration script |
| `IStyleSpecService` implementation | 3d | Core service layer |
| Spec version API endpoints | 2d | GET/PUT spec endpoints |
| Approval and lock workflow | 2d | State machine implementation |
| Spec version history UI panel | 3d | Version history sidebar |
| Spec PDF/Excel export | 3d | `TechPackExportService` |

### Phase 3 — Fit Iteration State Machine (2 weeks)

**Goal:** Replace flat Revise1…Revise6 columns with proper rounds.

| Task | Effort | Deliverable |
|------|--------|-------------|
| DB migration: `FitRound` + `FitMeasurement` tables | 1d | Migration script + backfill |
| `IFitIterationService` with state machine | 3d | PENDING → APPROVED flow |
| Fit round approval/rejection API | 2d | POST /fit-rounds/{id}/approve |
| Fit history timeline UI | 3d | Visual round progression |
| Notification hook: "spec revised" event | 1d | Email/push on approval |

### Phase 4 — AQL QC Sampling (4 weeks)

**Goal:** Replace flat per-garment QC with AQL order model.

| Task | Effort | Deliverable |
|------|--------|-------------|
| DB migration: `QcOrder` + `QcGarment` + `QcResult` tables | 2d | Migration script |
| AQL sample size calculator | 1d | `AqlSamplingService` |
| `IQcService` implementation | 4d | Full QC order lifecycle |
| QC measurement recording API | 2d | Batch measurement endpoint |
| Aggregate pass/fail calculation | 2d | `QcOrderSummaryDto` |
| QC order dashboard UI | 5d | Factory audit view |
| Defect classification logic | 2d | Critical/Major/Minor tagging |

### Phase 5 — Multi-Region Size Mapping (2 weeks)

**Goal:** US/EU/UK/JP size equivalence for global brands.

| Task | Effort | Deliverable |
|------|--------|-------------|
| DB migration: `SizeSystemMapping` table | 1d | Migration script |
| Size mapping service and admin UI | 3d | Map US 6 → EU 36 etc. |
| Spec sheet display by region | 2d | Toggle in spec grid header |

### Phase 6 — Frontend Modernization (6 weeks)

**Goal:** Replace AngularJS grading/QC screens with modern component.

| Task | Effort | Notes |
|------|--------|-------|
| React/Vue component for spec grid | 2w | Retain Wijmo FlexGrid or replace |
| Fit round timeline component | 1w | |
| QC order management SPA | 2w | |
| Mobile-responsive measurement input | 1w | Factory floor tablet use |

---

## 12. Key Invariants & Business Rules

### Grading

1. **Base size delta is always 0** — never store a non-zero delta at `baseSizeIndex`
2. **Deltas are adjacent-step** — not cumulative from base
3. **Fixed POMs ignore grade rules** — when `isFixed = true`, all sizes equal `baseValue`
4. **Base size is shared across all POMs** — cannot have POM A graded from M and POM B graded from L in the same spec
5. **Max 20 sizes per spec** — configurable via `EmApplicationSettings.MaxiumGradingSizeCounter`
6. **Asymmetric grading** — `plusDelta ≠ minusDelta` is valid; use directed delta array

### Fit Iteration

7. **FinalSpec = last approved round's actualValue** — falling back through rounds in reverse order to `baseValue`
8. **Spec is locked after TOP sample approval** — no measurement changes allowed after lock
9. **Locked spec is the QC benchmark** — all QC differences computed against the locked `specValue` snapshot

### QC

10. **QC diff = null when either value is missing** — not zero; distinguishes "not measured" from "exact match"
11. **Pass/fail uses the locked spec value** — not the current spec (which may have been revised)
12. **Shrinkage = beforeWash − afterWash** — always positive if garment shrinks
13. **Final QC pass is based on afterIron** — the post-recovery measurement

### Units

14. **Storage unit is always CM** — INCH is only a display format
15. **Conversion happens only at the API boundary** — never inside the domain or service layer
16. **Culture-aware decimal parsing** — comma vs period decimal separator per user locale

---

*Document maintained in `docs/pom-grading-qc-architecture.md`*  
*Companion concepts reference: `docs/pom-grading-qc-concepts.md`*
