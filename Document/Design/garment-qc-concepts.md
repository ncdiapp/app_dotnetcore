# Garment QC Concepts — Q&A Summary

**Date:** 2026-08-13  
**Context:** Architecture review of `QcOrder` aggregate in `pom-grading-qc-architecture.md`

---

## 1. What are `lotNumber` and `lotQuantity`?

**`lotNumber`** is a string identifier for a specific production batch from the factory. A "lot" (also called a production lot or cut ticket) is a group of garments cut and sewn together in one run, sharing the same fabric roll, dye batch, and production line. It is used for root-cause traceability if a defect is found.

Example: `"LOT-2026-08-CN-042"`

**`lotQuantity`** is the total number of garments in that production lot. This is the key input to the AQL sampling plan — the system looks up `lotQuantity` in the ISO 2859-1 table to compute how many garments need to be physically pulled and inspected (`sampleSize`).

---

## 2. Why does a lot of 500 units need AQL sampling, not 100% inspection?

Inspecting every garment has two fatal problems in practice:

1. **Cost** — measuring every garment in a large production run would cost more than the garments themselves.
2. **Inspection fatigue** — humans doing repetitive measurement for hours become less accurate over time. A focused 50-garment random sample is statistically more reliable than an exhausted 500-garment full inspection.

AQL (Acceptable Quality Level, ISO 2859-1) solves this with statistics: if garments are pulled randomly and the sample passes, probability theory guarantees (at a known confidence level) that the whole lot is acceptable.

---

## 3. What is the Acceptance Number (Ac)?

The acceptance number is the **maximum number of defective garments allowed in the sample before the entire lot is rejected**.

For lot = 500, AQL 2.5 Major → sample 50 garments, **Ac = 3**:

| Defects found in 50 sampled | Decision |
|---|---|
| 0, 1, 2, or 3 | Lot **PASSES** — ship it |
| 4 or more | Lot **FAILS** — reject or reinspect all 500 |

AQL 2.5 means you accept lots where up to 2.5% of garments are defective. ISO 2859-1 converts this to a whole-number Ac using statistical tables that account for sampling uncertainty.

---

## 4. How is `sampleSize` computed from the AQL table?

The ISO 2859-1 standard is a **two-step lookup** (General Inspection Level II, the fashion industry default):

### Step 1 — Lot Size → Code Letter

| Lot Size | Code Letter |
|---|---|
| 2 – 50 | D |
| 51 – 90 | E |
| 91 – 150 | F |
| 151 – 280 | G |
| 281 – 500 | H |
| 501 – 1200 | J |
| 1201 – 3200 | K |
| 3201 – 10000 | L |

### Step 2 — Code Letter → Sample Size + Ac/Re per AQL Level

| Code | Sample Size | AQL 1.0 (Critical) Ac/Re | AQL 2.5 (Major) Ac/Re | AQL 4.0 (Minor) Ac/Re |
|---|---|---|---|---|
| D | 8 | 0 / 1 | 0 / 1 | 0 / 1 |
| E | 13 | 0 / 1 | 0 / 1 | 1 / 2 |
| F | 20 | 0 / 1 | 1 / 2 | 1 / 2 |
| G | 32 | 0 / 1 | 1 / 2 | 2 / 3 |
| **H** | **50** | **1 / 2** | **3 / 4** | **5 / 6** |
| J | 80 | 1 / 2 | 3 / 4 | 5 / 6 |
| K | 125 | 2 / 3 | 5 / 6 | 7 / 8 |

**Ac** = acceptance number (max defects → lot passes)  
**Re** = rejection number (always Ac + 1)

### Key insight: sample size does not change with AQL level

For lot 500 (Code H), you always pull **50 garments** regardless of AQL level. What changes is the Ac/Re threshold:

| AQL Level | Sample Size | Defects allowed (Ac) |
|---|---|---|
| 1.0 Critical | 50 | ≤ 1 |
| 2.5 Major | 50 | ≤ 3 |
| 4.0 Minor | 50 | ≤ 5 |

### Implementation note

The `GetSampleSize()` method in the architecture doc is correct but incomplete — it computes sample size (which depends only on lot size) but not Ac. A complete implementation needs a second lookup for Ac based on both code letter and AQL level.

---

## 5. Is `garmentSerial` a physically unique identifier like a SKU?

**No.** `garmentSerial` is not a globally unique physical identifier. It is a **local label the inspector writes on a tag or sticker** when pulling garments from the lot for sampling. It only needs to be unique within one `QcOrder`.

Unlike electronics (each phone has an IMEI), mass-produced garments are identical units in a batch with no individual serial number — just a style label, size, and lot tag shared by all garments.

When an inspector pulls 50 garments from a box of 500, they label each one to tie the physical garment to its measurement rows:

| Method | Example `garmentSerial` |
|---|---|
| Masking tape + marker | `"1"`, `"2"`, ... `"50"` |
| Pre-printed sticky labels | `"A001"`, `"A002"` |
| Factory carton tag | `"CTN-03-PCS-07"` |

**Uniqueness constraint:** unique within one `QcOrder`, not globally. If the lot is reinspected under a new `QcOrder`, `"1"` can appear again with no conflict.

---

## 6. Should color be tracked in QC?

### For measurement QC — color is not needed

POM measurements (chest, waist, sleeve length) are dimension-only. A red dress and a blue dress cut from the same pattern have identical measurements. `QcMeasurement` correctly ignores color.

### Color is implicitly captured by `lotNumber`

Factories run one colorway per production lot. So `lotNumber` already identifies the colorway in practice:

```
Lot "2026-08-RED-042"  →  red colorway, 500 units  →  one QcOrder
Lot "2026-08-BLU-043"  →  blue colorway, 500 units →  separate QcOrder
```

### Color QC is a separate process

Color quality checks are independent of measurement QC:

| Color QC Check | What it tests |
|---|---|
| Shade banding | All garments match the approved color standard |
| Color fastness (AATCC TM61) | Color does not bleed when washed |
| Metamerism | Color matches under daylight vs. fluorescent light |
| Print registration | Pattern lines up at seams |

This would require its own entity (e.g., `ColorQcResult`) separate from `QcMeasurement`.

### Recommendation: add `colorwayId` to `QcOrder`

Not because measurement math requires it, but for traceability and cross-colorway reporting:

```
QcOrder
├── styleId
├── specVersionId
├── colorwayId     ← add: FK → StyleColorway
├── lotNumber
├── lotQuantity
...
```

This enables questions like: "Did the blue colorway have more measurement failures than the red?" — useful for supplier audits. The measurement calculations remain unchanged.

---

## 7. Do we need to inspect all sizes for one style + color?

**No — a representative subset of sizes is selected, not all of them.**

The model already reflects this with `SelectedSizes[]` on `QcOrder`.

### Why not inspect all sizes

A style might have 8 sizes (XS → 3XL). Inspecting all 8 sizes × 50 sampled garments × 20 POMs = **8,000 measurements** per QC order. That's excessive because:

- Sizes are not independent — they are **mathematically derived from the base size** via grading rules
- If the base size passes and the grade rule is correct, intermediate sizes are predictable
- Grading errors compound toward the **extremes** (smallest and largest), not the middle

### Industry standard: inspect 3 sizes

| Size selected | Why |
|---|---|
| **Base size** (e.g., M) | The pattern origin — if this fails, everything fails |
| **Smallest size** (e.g., XS) | Maximum negative grading accumulation — where shrinkage errors amplify |
| **Largest size** (e.g., 2XL) | Maximum positive grading accumulation — where stretch errors amplify |

Some brands also add the **top commercial size** (L or XL) if it accounts for most units sold.

### How this interacts with AQL sampling

The AQL sample (e.g., 50 garments) is drawn from the **whole lot across all sizes**, not 50 per size:

```
Lot = 500 garments (mixed sizes)
  XS: 40 units     ← in lot
  S:  80 units
  M:  120 units    ← base size
  L:  130 units
  XL: 80 units
  2XL: 50 units    ← in lot

Pull 50 garments randomly → mix of sizes naturally
SelectedSizes = [XS, M, 2XL]

For each pulled garment:
  - If garment is XS  → measure all POMs against XS spec
  - If garment is M   → measure all POMs against M spec
  - If garment is 2XL → measure all POMs against 2XL spec
  - If garment is S, L, XL → skip measurement (size not selected)
```

### Pass/fail aggregation rule

```
Garment passes if:
  ALL QcResults where sizeRotateId ∈ SelectedSizes have pass = true
  (garments whose size is not selected are excluded from pass/fail)

Order passes if:
  # failed garments (among those whose size IS selected) ≤ Ac
```

### Recommendation: add selection strategy to the model

`SelectedSizes[]` is currently a bare list. Adding a `selectionReason` field supports audit trail documentation:

```
QcOrder
├── SelectedSizes[]
│   ├── sizeRotateId
│   └── selectionReason    BASE | EXTREME_MIN | EXTREME_MAX | HIGH_VOLUME | MANUAL
```

This lets a QC certificate explain: "XS selected as minimum extreme, M as base size, 2XL as maximum extreme" — satisfying factory audit documentation requirements.

---

*Companion document: `docs/pom-grading-qc-architecture.md`*
