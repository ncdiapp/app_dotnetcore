# Garment QC: Defect Classification, Aggregation & AQL Pass/Fail

**Date:** 2026-08-13  
**Context:** Architecture design for `QcOrder` → `QcGarment` → `QcResult` aggregation pipeline

---

## 1. Three Defect Classes and How to Obtain Each

AQL uses three parallel defect levels — Critical, Major, Minor — each with its own acceptance number (Ac) and its own inspection method.

### 1.1 Major — from `QcMeasurements` (tape measure)

Major defects are **dimensional failures** found by measuring garment dimensions against the approved spec.

**How to get the value:**
```
For each QcResult row on a garment:
  FinalDiff = AfterIronValue − SpecValue   (fall back: AfterWash → BeforeWash → Production)
  Pass      = |FinalDiff| ≤ Tolerance

Garment has a Major defect if:
  any QcResult.Pass = false
  (measurement is outside tolerance)
```

**Examples:** Chest 52 cm vs spec 50 ± 1 cm (+3 out); inseam 81 cm vs spec 79 ± 1 cm.

---

### 1.2 Critical — from `QcDefectRecord` (visual + physical test)

Critical defects are **safety or functional failures** that cannot be detected by measuring. They require a separate visual/physical inspection.

**How to get the value:**
```
Inspector physically tests and visually examines each sampled garment.
Any safety failure → INSERT QcDefectRecord with DefectClass = 'CRITICAL'

Garment has a Critical defect if:
  any QcDefectRecord.DefectClass = 'CRITICAL' exists for that garment
  OR any QcResult where the POM is flagged as safety-critical AND Pass = false
```

**Examples:** Drawstring on child's hood (strangulation risk), sharp metal clasp (laceration), button that snaps off (choking hazard for children ≤ 3), toxic dye detected, flammability failure.

---

### 1.3 Minor — from `QcDefectRecord` (visual inspection)

Minor defects are **cosmetic or quality-perception failures** found by looking at the garment, not measuring it.

**How to get the value:**
```
Inspector visually examines each sampled garment.
Any cosmetic issue → INSERT QcDefectRecord with DefectClass = 'MINOR'

Garment has a Minor defect if:
  any QcDefectRecord.DefectClass = 'MINOR' exists for that garment
  OR any QcResult with borderline diff (e.g., within 50% of tolerance band)
```

**Examples:** Loose thread, pilling, uneven stitching, color variance, crooked label, print misregistration.

---

### 1.4 The Missing Entity: `QcDefectRecord`

The base architecture doc's `QcResult` only covers dimensional measurements (Major). Critical and Minor defects require a separate entity:

```
QcDefectRecord
├── QcDefectRecordId   INT PK
├── QcGarmentId        FK → QcGarment
├── DefectClass        CRITICAL | MAJOR | MINOR
├── DefectCode         string   e.g. "SHARP_HARDWARE", "LOOSE_BUTTON", "COLOR_FADE"
├── DefectLocation     string   e.g. "COLLAR", "LEFT_SLEEVE", "BACK_HEM"
├── Description        string   (inspector free-text notes)
├── Quantity           int      (instances on this garment)
└── audit fields
```

---

### 1.5 Three Parallel Inspection Tracks Per Garment

```
One sampled garment → three separate inspections:

Track 1 — Tape measure
  → QcResult rows (before/after wash/iron)
  → produces: Major (OOT measurement)

Track 2 — Physical safety test
  → QcDefectRecord (DefectClass = CRITICAL)
  → "Does this button snap off?" "Is there a sharp edge?"

Track 3 — Visual cosmetic inspection
  → QcDefectRecord (DefectClass = MINOR)
  → "Loose thread?" "Color consistent?" "Label straight?"
```

---

## 2. ISO 2859-1 AQL Table: GetSampleSize and GetAcceptanceNumber

### 2.1 Two-Step Lookup

**Step 1 — `lotQuantity` → Code Letter** (General Inspection Level II):

| Lot Size | Code Letter |
|---|---|
| 2 – 50 | D |
| 51 – 90 | E |
| 91 – 150 | F |
| 151 – 280 | G |
| 281 – 500 | H |
| 501 – 1200 | J |
| 1201 – 3200 | K |
| > 3200 | L |

**Step 2 — Code Letter → Sample Size + Ac per AQL Level:**

| Code | Sample Size | Ac Critical_1 | Ac Major_2_5 | Ac Minor_4_0 |
|---|---|---|---|---|
| D | 8 | 0 | 0 | 0 |
| E | 13 | 0 | 0 | 1 |
| F | 20 | 0 | 1 | 1 |
| G | 32 | 0 | 1 | 2 |
| H | 50 | 0* | 3 | 5 |
| J | 80 | 0* | 3 | 5 |
| K | 125 | 0* | 5 | 7 |
| L | 200 | 0* | 7 | 10 |

> *ISO technically allows Ac=1–3 for Critical at larger lots, but **industry practice mandates Ac = 0 for Critical across all lot sizes** — any single critical defect rejects the entire lot. The BL should hard-code Critical Ac = 0 always.

### 2.2 C# Implementation

```csharp
public static class QcAqlBL
{
    private static char GetCodeLetter(int lotQuantity)
    {
        if (lotQuantity <= 50)   return 'D';
        if (lotQuantity <= 90)   return 'E';
        if (lotQuantity <= 150)  return 'F';
        if (lotQuantity <= 280)  return 'G';
        if (lotQuantity <= 500)  return 'H';
        if (lotQuantity <= 1200) return 'J';
        if (lotQuantity <= 3200) return 'K';
        return 'L';
    }

    public static int GetSampleSize(int lotQuantity)
    {
        return GetCodeLetter(lotQuantity) switch
        {
            'D' => 8,
            'E' => 13,
            'F' => 20,
            'G' => 32,
            'H' => 50,
            'J' => 80,
            'K' => 125,
            _   => 200
        };
    }

    // aqlLevel: "CRITICAL_1" | "MAJOR_2_5" | "MINOR_4_0"
    public static int GetAcceptanceNumber(int lotQuantity, string aqlLevel)
    {
        if (aqlLevel == "CRITICAL_1")
            return 0;  // zero tolerance always — industry override of ISO table

        var letter = GetCodeLetter(lotQuantity);

        if (aqlLevel == "MAJOR_2_5")
            return letter switch
            {
                'D' => 0, 'E' => 0,
                'F' => 1, 'G' => 1,
                'H' => 3, 'J' => 3,
                'K' => 5, _   => 7
            };

        // MINOR_4_0
        return letter switch
        {
            'D' => 0,
            'E' => 1, 'F' => 1,
            'G' => 2,
            'H' => 5, 'J' => 5,
            'K' => 7, _   => 10
        };
    }
}
```

**Example calls:**
```csharp
QcAqlBL.GetSampleSize(500)                         // → 50
QcAqlBL.GetAcceptanceNumber(500, "CRITICAL_1")     // → 0
QcAqlBL.GetAcceptanceNumber(500, "MAJOR_2_5")      // → 3
QcAqlBL.GetAcceptanceNumber(500, "MINOR_4_0")      // → 5
```

---

## 3. QC Pass/Fail Aggregation

### 3.1 Garment-Level Aggregation

A garment's status is determined independently for each defect class:

```
GarmentCriticalFail = any QcDefectRecord.DefectClass = 'CRITICAL' on this garment
                      OR any safety-POM QcResult.Pass = false

GarmentMajorFail    = any QcResult.Pass = false on this garment

GarmentMinorFail    = any QcDefectRecord.DefectClass = 'MINOR' on this garment

GarmentPassStatus:
  → FAILED  if GarmentCriticalFail OR GarmentMajorFail OR GarmentMinorFail
  → PASSED  if all measured results pass and no defect records
  → PENDING if no measurements recorded yet
```

### 3.2 Order-Level Aggregation (AQL)

Three independent checks run against three Ac values:

```
AcCritical = GetAcceptanceNumber(order.LotQuantity, "CRITICAL_1")  // always 0
AcMajor    = GetAcceptanceNumber(order.LotQuantity, "MAJOR_2_5")
AcMinor    = GetAcceptanceNumber(order.LotQuantity, "MINOR_4_0")

failCritical = count of garments with GarmentCriticalFail = true
failMajor    = count of garments with GarmentMajorFail    = true
failMinor    = count of garments with GarmentMinorFail    = true

Order FAILS if:
  failCritical > AcCritical   (Ac=0: even 1 critical → fail immediately)
  OR failMajor > AcMajor
  OR failMinor > AcMinor

Order PASSES if:
  all three checks pass AND all sampled garments have been recorded

Order is IN_PROGRESS otherwise.
```

### 3.3 Aggregation Chain on Each Measurement Save

```
Inspector records result → SaveQcResult(dto)
  ↓
INSERT/UPDATE QcResult
Compute Pass = |FinalDiff| ≤ Tolerance
  ↓
UpdateGarmentPassStatus(qcGarmentId)
  SELECT QcResult.Pass + QcDefectRecord.DefectClass for this garment
  → evaluate Critical / Major / Minor flags
  → UPDATE QcGarment.GarmentPassStatus
  ↓
UpdateOrderStatus(qcOrderId)
  SELECT count by GarmentPassStatus + defect class breakdown
  → compare each count against its Ac
  → UPDATE QcOrder.Status
```

### 3.4 Worked Example

**Lot = 500 garments, AQL Major:**
- Sample = 50 garments, AcCritical = 0, AcMajor = 3, AcMinor = 5

| Garment | Critical Defects | Major (OOT) | Minor Defects | Garment Status |
|---|---|---|---|---|
| G-01 | 0 | 0 | 1 | FAILED (minor) |
| G-02 | 0 | 1 | 0 | FAILED (major) |
| G-03 | 1 | 0 | 0 | FAILED (critical) |
| G-04..50 | 0 | 0 | 0 | PASSED |

Order result:
- failCritical = 1 → **ORDER FAILS IMMEDIATELY** (Ac = 0)
- Even though failMajor = 1 ≤ Ac=3 and failMinor = 1 ≤ Ac=5, the critical failure overrides everything

---

## 4. Critical Special Rule

> **Any single Critical defect in the sample = reject the entire lot of 500 garments. No Ac. No exceptions.**

This is a brand/industry override of the ISO table (which technically allows Ac=1–3 for larger lots). Critical defects are safety hazards — a statistical acceptance threshold does not apply to consumer safety.

---

*Companion documents: `docs/pom-grading-qc-architecture.md`, `docs/garment-qc-concepts.md`*
