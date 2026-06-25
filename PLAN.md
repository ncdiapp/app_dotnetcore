# Fashion PLM — SaaS Product Plan

**Status:** Phase 1A IN PROGRESS  
**Date:** 2026-06-25  
**Product:** Fashion PLM SaaS — multiple brand tenants, each with their own template/fields/formulas

---

## Architecture Decisions (Final)

### Two-Layer Hybrid

| Layer | What goes here | How stored | Who configures |
|---|---|---|---|
| **Config Layer** | Style template, BOM, Construction, Costing, Colors, Workflow, Formulas | AppFormData / DictOneToOneFields (generic, schema-less) | Brand tenant — self-service via Form Builder |
| **Algorithm Layer** | Grading math, AQL sampling, Fit round state machine, QC aggregate | Tchp* structured tables + APP.TechPack plugin | Platform team — built once, shared by all brands |

### Rules
- **Never** create static SQL tables for customer-configurable data (BOM fields, style header fields, costing, construction — every brand has different columns)
- **Always** use Form Builder → AppFormData for anything a customer might want to customize
- **Only** write plugin code + Tchp* tables for algorithmic modules where the math is identical across all fashion brands

---

## Phase 1A — Style Template Reference Configuration ✅ GUIDE WRITTEN

**Goal:** A fully configured "Style Template" transaction in Form Builder. Any brand tenant imports this reference template and customizes fields, tabs, and formulas for their own needs. No SQL, no code.

**Deliverable:** `Document/Design/PLM_StyleTemplate_Config_Guide.md` ✅

**Config checklist (done in platform admin UI):**

- [ ] Register enums: `EmStyleStatus`, `EmProductType`, `EmSeason`, `EmDivision`, `EmStyleType`, `EmComponentType`, `EmFitRoundType`, `EmFitRoundStatus`, `EmApprovalStatus`
- [ ] Create Transaction: `StyleTemplate` (MasterDetail type)
- [ ] Create Root Unit: **01 Style Template** + child units:
  - Summary (style header fields, images, roles)
  - Style Color Details (product colors + SKU grid)
  - Construction (customer-defined fields)
  - Placement & Folding (customer-defined fields)
  - Bill of Material (customer-defined BOM columns)
  - BOM Colorways (BOM per colorway)
  - BC Master Costing (customer-defined cost fields)
  - Testing (lab test requests)
  - Timeline (milestone dates)
  - Critical Path (Gantt — DayPilot already in platform)
  - Audit Log (platform built-in)
- [ ] Create Root Unit: **02a Fit & Grading** + child units:
  - Master Grading ← Phase 2 placeholder stub
  - How to Measure Instructions ← Phase 2 placeholder stub
  - 3D Views (image attachments grid)
  - Fit 1–4 & Comments ← Phase 3 placeholder stubs
  - Fit Summary ← Phase 3 placeholder stub
  - Fit - Multi Size (Master) ← Phase 3 placeholder stub
- [ ] Create Root Unit: **02b PP** + child units:
  - PP 1 / PP 2 / PP 3 ← Phase 3 placeholder stubs
  - PP Summary ← Phase 3 placeholder stub
  - PP - Multi Size (Master) ← Phase 3 placeholder stub
- [ ] Configure Form Design for Summary (style header fields per guide)
- [ ] Configure Form Design for Style Color Details (2 nested grids)
- [ ] Configure Form Design for Bill of Material (starter columns — customers customize)
- [ ] Configure Form Design for Construction, Placement, Costing, Testing (minimal placeholders)
- [ ] Configure SearchEditor: Style list (Code, Name, Season, Division, Brand, Status + filters)
- [ ] Configure Workflow: NEW → IN_REVIEW → APPROVED → LOCKED with NotifyTaskUser

**Verify:**
- [ ] 3-group sidebar renders (01 Style Template / 02a Fit & Grading / 02b PP)
- [ ] Summary form saves a new style record end-to-end
- [ ] Workflow approval changes status correctly
- [ ] Pilot brand imports template, adds their own BOM columns without developer help

---

## Phase 1B — Style Template Data Migration

**Goal:** ETL scripts to move existing style data from legacy PLM DB into the new AppAI transaction store.

**Deliverable:** `Document/Design/PLM_Migration_ETL.sql`

- [ ] Map legacy style header columns → AppFormData DictOneToOneFields keys
- [ ] Map legacy color/colorway tables → child AppFormData records
- [ ] Map legacy BOM lines → child AppFormData records (per-brand field mapping)
- [ ] Validate migrated records open correctly in new form

**Blocked by:** Phase 1A config verified with pilot brand

---

## Phase 2 — Grading Module

**Goal:** Working grading grid for any style — PivotEditGrid UI, grading engine math, per-customer size run configuration.

**Schema:** `Document/Design/POM_Grading_QC_NewSchema.sql` ✅ already written (19 Tchp* tables)

**Config tasks:**
- [ ] Run `POM_Grading_QC_NewSchema.sql` on tenant DB
- [ ] Register enums: `EmSpecStatus` (DRAFT/APPROVED/LOCKED), `EmUnitOfMeasure` (CM/INCH)
- [ ] Configure Size Runs CRUD form (TchpSizeRun + TchpSizeRunSize child grid)
- [ ] Configure Size Dimensions form (TchpSizeRunDimension — assign MA/UA/XA to sizes)
- [ ] Configure Body Parts CRUD form (TchpBodyPart)
- [ ] Configure POM Templates CRUD form (TchpPomTemplate + TchpPomTemplatePart child)
- [ ] Configure Grade Rule Sets CRUD form (TchpGradeRuleSet + TchpGradeRule child)
- [ ] Wire "Master Grading" unit → APP.TechPack.PluginEntry via PluginWebApiCall
- [ ] Configure Style Spec form header (status badge, base size picker, UOM toggle)

**Code tasks:**
- [ ] `PivotEditGridPanel.tsx` — editable Wijmo FlexGrid with dynamic size columns (reads `AppPivotDto` from `DictUnitIdPivotGrid`). **This is the only custom React component in the PLM.**
  - Rows = POM spec lines (BodyPartCode, BaseValue, Tolerance, IsFixed)
  - Columns = dynamic per size run (one column per TchpSizeRunSize)
  - Cell edit → saves TchpGradeValue row via plugin
  - Base size column locked + highlighted amber
  - On "Apply Rule Set" button → calls `APP.TechPack.ApplyGradeRuleSet` via PluginWebApiCall
- [ ] Seed ASTM grade rule data (Women's Misses, Men's Shirt) — already in `POM_Grading_QC_NewSchema.sql`

**Verify:**
- [ ] Size Run with MA/UA/XA dimensions created and showing correct sizes
- [ ] Grade Rule Set ASTM Women's Misses visible in dropdown
- [ ] Opening a style → Master Grading tab → pivot grid renders with correct size columns
- [ ] Editing a cell saves delta to TchpGradeValue
- [ ] "Apply Rule Set" button applies ASTM values across all POM lines

---

## Phase 3 — Fit Rounds + Pre-Production

**Goal:** Unlimited fit iteration rounds with approval sign-off; PP samples; TOP approval locks the spec.

**Schema:** TchpFitRound + TchpFitMeasurement (in `POM_Grading_QC_NewSchema.sql` ✅)

**Config tasks:**
- [ ] Configure Fit Round form (TchpFitRound header + TchpFitMeasurement child grid)
- [ ] Configure Workflow: PENDING → SUBMITTED → APPROVED / REJECTED
- [ ] Configure notification: "spec revised" on APPROVED
- [ ] Wire Fit 1–4 units to TchpFitRound (filtered by RoundNumber)
- [ ] Wire PP 1–3 units to TchpFitRound (filtered by RoundType = PP1/PP2/PP3)
- [ ] TOP approval → auto-transition TchpStyleSpec.SpecStatus = LOCKED

**Code tasks:**
- [ ] FitRound state machine guard in APP.TechPack — new round only creatable when no round is PENDING or SUBMITTED

---

## Phase 4 — QC Pipeline

**Goal:** AQL order model, 4-stage wash measurements, aggregate order pass/fail.

**Schema:** TchpQcOrder, TchpQcOrderSize, TchpQcGarment, TchpQcResult (in `POM_Grading_QC_NewSchema.sql` ✅)

**Config tasks:**
- [ ] Register enums: `EmAqlLevel` (CRITICAL_1/MAJOR_2_5/MINOR_4_0), `EmQcOrderStatus` (OPEN/IN_PROGRESS/PASSED/FAILED)
- [ ] Configure QC Order form (AQL level, lot qty, factory, size selector)
- [ ] Configure QC Garment + Result grid (4 wash stage columns, read-only computed Shrinkage/Recovery/FinalDiff)
- [ ] Configure Workflow: OPEN → IN_PROGRESS → PASSED / FAILED
- [ ] Configure QC dashboard search view (factory audit summary)

**Code tasks:**
- [ ] `AqlSamplingService` in APP.TechPack — ISO 2859-1 lot size → sample size lookup
- [ ] `QcAggregateService` in APP.TechPack — garment pass/fail; order pass/fail vs acceptance number

---

## Phase 5 — ERP Integration (Publish to BlueCherry)

**Goal:** Publish approved styles, BOM, and cost to BlueCherry ERP from the style template.

**Config tasks:**
- [ ] Configure "Publish to BlueCherry" button as PluginWebApiCall command on StyleTemplate
- [ ] Configure "Publish BOM Colorways" command
- [ ] Configure "Publish Cost Colorways" command
- [ ] Set IsPublishedToErp / PublishFailedToErp / PublishToErpMessage fields (read-only, updated by plugin)

**Code tasks:**
- [ ] BlueCherry publish plugin (new plugin DLL or extend APP.TechPack) — calls BC REST API with style/BOM/cost payload

---

## Deleted / Superseded

- ~~`PLM_Materials_BOM_Schema.sql`~~ — deleted 2026-06-25. BOM and Materials are customer-configurable via Form Builder. Static SQL tables are wrong for SaaS multi-tenant PLM where each brand has different fields.

---

## Key Files

| File | Purpose |
|---|---|
| `Document/Design/PLM_StyleTemplate_Config_Guide.md` | Phase 1A — step-by-step Form Builder config guide |
| `Document/Design/POM_Grading_QC_NewSchema.sql` | Phase 2–4 — 19 Tchp* tables (grading, fit, QC) |
| `Document/Design/POM_Grading_QC_Implementation_Plan.md` | Phase 2–4 detailed implementation reference |
| `Document/Design/TechPack_Plugin_Architecture.md` | APP.TechPack plugin architecture reference |
| `APP.TechPack/PluginEntry.cs` | Plugin entry point (grading methods already wired) |
| `APP.TechPack/Engine/GradingEngine.cs` | Pure math grading engine (15 unit tests passing) |
