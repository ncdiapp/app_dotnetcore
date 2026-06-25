# PLM Style Template — Form Builder Configuration Guide

**Version:** 1.0 | **Date:** 2026-06-25  
**Platform:** AppAI/AppBuilder  
**Purpose:** Step-by-step guide to configure the Fashion PLM "Style Template" transaction in AppAI Form Builder. This produces a reference template that each brand tenant imports and customizes — no SQL schema changes, no code.

---

## Architecture Reminder

All Style Template data (header fields, colors, BOM lines, construction, costing) is stored in AppAI's generic `AppFormData / DictOneToOneFields` store. **Each customer adds, removes, or renames fields** through the Form Builder without touching SQL. Only the Grading and QC modules (Phase 2+) use structured Tchp* tables.

---

## Pre-Requisite: Register Enums

Navigate to: **My Applications → Entity Management → Entity List of Value**

Create the following enum lists before opening Form Builder (fields that reference these enums will fail if enums don't exist yet):

| Enum Name | Values |
|---|---|
| `EmStyleStatus` | NEW, DEVELOPMENT, IN_REVIEW, APPROVED, LOCKED, CANCELLED |
| `EmProductType` | PAR, KNIT, WOVEN, DENIM, OUTERWEAR, SWIMWEAR, ACCESSORIES |
| `EmSeason` | SS25, FW25, SS26, FW26, RESORT25, HOLIDAY25 |
| `EmDivision` | WOMENS, MENS, KIDS, UNISEX |
| `EmStyleType` | FINISHED_GOODS, RAW_MATERIAL, COMPONENT |
| `EmComponentType` | STYLE, SHELL, LINING, TRIM |
| `EmFitRoundType` | PP1, PP2, PP3, SMS, TOP, INTERNAL |
| `EmFitRoundStatus` | PENDING, SUBMITTED, APPROVED, REJECTED |
| `EmApprovalStatus` | PENDING, APPROVED, REJECTED |

> **Customer-specific tip:** Customers edit enum VALUES freely (add seasons, add product types) — the enum NAMES above are fixed identifiers used by the platform.

---

## Step 1 — Create the Transaction

Navigate to: **My Applications → Form Builder (Application Form Builder)**

1. Click **New Transaction**
2. Fill in:
   - **Transaction Name:** `StyleTemplate`
   - **Display Name:** `Style Template`
   - **Transaction Type:** `MasterDetail` (EmTransactionOrganizedType = 1)
   - **Icon:** choose a clothing/style icon
3. Save

---

## Step 2 — Create Transaction Unit Hierarchy

The left sidebar of the legacy PLM maps to AppAI Transaction Units. Each group header is a **Root Unit**; each tab within a group is a **Child Unit**.

Navigate to: **Data Model Design** tab within the StyleTemplate transaction.

### 2.1 Root Unit: 01 Style Template

| Field | Value |
|---|---|
| Unit Name | `StyleHeader` |
| Display Name | `01 Style Template` |
| Level | Root (1) |
| Sort Order | 1 |

**Child units under 01 Style Template** (Level = Child, sorted as listed):

| Sort | Unit Name | Display Name | Notes |
|---|---|---|---|
| 1 | `StyleSummary` | Summary | Main style header fields |
| 2 | `StyleColorDetails` | Style Color Details | Product colors + SKU details |
| 3 | `Construction` | Construction | Garment construction instructions |
| 4 | `PlacementFolding` | Placement & Folding | Packing/folding specifications |
| 5 | `BillOfMaterial` | Bill of Material | BOM lines — customer-defined columns |
| 6 | `BomColorways` | BOM Colorways | BOM per colorway breakdown |
| 7 | `MasterCosting` | BC Master Costing | Cost sheet |
| 8 | `Testing` | Testing | Lab test requests and results |
| 9 | `Timeline` | Timeline | Milestone dates |
| 10 | `CriticalPath` | Critical Path | Gantt view (DayPilot) |
| 11 | `AuditLog` | Audit Log | Read-only platform audit trail |

### 2.2 Root Unit: 02a Fit & Grading

| Field | Value |
|---|---|
| Unit Name | `FitGrading` |
| Display Name | `02a Fit & Grading` |
| Level | Root (1) |
| Sort Order | 2 |

**Child units:**

| Sort | Unit Name | Display Name | Notes |
|---|---|---|---|
| 1 | `MasterGrading` | Master Grading | Wired to APP.TechPack plugin (Phase 2) |
| 2 | `HowToMeasure` | How to Measure | TchpBodyPart instructions (Phase 2) |
| 3 | `ThreeDViews` | 3D Views | Image attachments grid |
| 4 | `Fit1` | Fit 1 & Comments | TchpFitRound RoundNumber=1 (Phase 3) |
| 5 | `Fit2` | Fit 2 & Comments | TchpFitRound RoundNumber=2 (Phase 3) |
| 6 | `Fit3` | Fit 3 & Comments | TchpFitRound RoundNumber=3 (Phase 3) |
| 7 | `Fit4` | Fit 4 & Comments | TchpFitRound RoundNumber=4 (Phase 3) |
| 8 | `FitSummary` | Fit Summary | Read-only aggregate (Phase 3) |
| 9 | `FitMultiSize` | Fit - Multi Size (Master) | Multi-size fit view (Phase 3) |

### 2.3 Root Unit: 02b PP

| Field | Value |
|---|---|
| Unit Name | `PreProduction` |
| Display Name | `02b PP` |
| Level | Root (1) |
| Sort Order | 3 |

**Child units:**

| Sort | Unit Name | Display Name | Notes |
|---|---|---|---|
| 1 | `PP1` | PP 1 | TchpFitRound RoundType=PP1 (Phase 3) |
| 2 | `PP2` | PP 2 | TchpFitRound RoundType=PP2 (Phase 3) |
| 3 | `PP3` | PP 3 | TchpFitRound RoundType=PP3 (Phase 3) |
| 4 | `PPSummary` | PP Summary | Read-only summary (Phase 3) |
| 5 | `PPMultiSize` | PP - Multi Size (Master) | Multi-size PP view (Phase 3) |

---

## Step 3 — Configure Form Design: Summary (Style Header)

Navigate to: **Form Design** for the `StyleSummary` unit.

This is the most important form — it drives search, filtering, and identity for the whole style record.

### Header Section

| Field Name | Display Label | Field Type | Source / Enum |
|---|---|---|---|
| `DesignNo` | Design No | TextBox | Free text |
| `BoardNumber` | Board Number | TextBox | Free text |
| `StyleNumber` | Style # | TextBox | Free text, **unique key** |
| `ProductName` | Product Name | TextArea | Free text |
| `Description` | Description | TextArea | Free text |
| `PrimaryVendorId` | Primary Vendor | FKLookup | → Vendor entity |
| `CountryCode` | Country Code | TextBox | Free text |
| `SupplierArticleNumber` | Supplier Article Number | TextBox | Free text |
| `SizeRangeId` | Size Range | FKLookup | → Size Run entity |
| `ProductType` | Product Type | Dropdown | `EmProductType` |
| `HsNumber` | HS Number | TextBox | Free text |
| `ProductStatus` | Product Status | Dropdown | `EmStyleStatus` |

### Classification Section

| Field Name | Display Label | Field Type | Source |
|---|---|---|---|
| `Division` | Division | Dropdown | `EmDivision` |
| `StyleType` | Style Type | Dropdown | `EmStyleType` |
| `ComponentType` | Component Type | Dropdown | `EmComponentType` |
| `Season` | Season | Dropdown | `EmSeason` |
| `Department` | Department | TextBox or FKLookup | Customer configures |
| `Brand` | Brand | TextBox or FKLookup | Customer configures |
| `SubDepartment` | Sub Department | TextBox | Customer configures |
| `Category` | Category | TextBox | Customer configures |
| `Collection` | Collection | TextBox | Customer configures |

### ERP Flags Section

| Field Name | Display Label | Field Type | Notes |
|---|---|---|---|
| `IsAvailable` | Available | Checkbox | Default: true |
| `PublishToErp` | Publish to ERP | Checkbox | Triggers publish workflow |
| `IsPublishedToErp` | Published to ERP | Checkbox | Read-only, set by plugin |
| `PublishFailedToErp` | Publish Failed | Checkbox | Read-only, set by plugin |
| `PublishToErpMessage` | Publish to ERP Message | TextArea | Read-only, set by plugin |

### Composition Section

| Field Name | Display Label | Field Type | Notes |
|---|---|---|---|
| `StyleContent` | Style Content | FKLookup | → Fabric/Content library |
| `TotalComposition` | Total Composition | TextArea | Formula-computed or free text |

### Roles Section

| Field Name | Display Label | Field Type | Notes |
|---|---|---|---|
| `MerchandiserId` | Merchandiser | FKLookup | → User entity |
| `DesignerId` | Designer | FKLookup | → User entity |
| `ProductDeveloperId` | Product Developer | FKLookup | → User entity |
| `SourcingId` | Sourcing | FKLookup | → User entity |

### Image Fields

| Field Name | Display Label | Field Type |
|---|---|---|
| `MainImage` | Main Image | ImageUpload |
| `AdditionalImage1` | Additional Image | ImageUpload |
| `AdditionalImage2` | Additional Image | ImageUpload |

---

## Step 4 — Configure Form Design: Style Color Details

Unit: `StyleColorDetails` — two grids, stacked.

### Grid 1: Product Colors

| Column | Field Type | Notes |
|---|---|---|
| Active | Checkbox | |
| Color | FKLookup | → Color library |
| Image | ImageUpload | Per-color image |
| Swatch | ImageUpload | Color swatch image |
| SketchId | TextBox | |
| RGB | TextBox | e.g. "92,64,51" |
| Color Family | FKLookup | → Color Family library |
| New/Carryover | Dropdown | NEW, CARRYOVER |
| Approv Date | DatePicker | |
| Approved | Checkbox | |

### Grid 2: Style Color Details (child of Product Colors)

| Column | Field Type | Notes |
|---|---|---|
| Color | FKLookup | Inherited from parent |
| SKU | TextBox | Auto-generated or manual |
| UPC | TextBox | |
| Active | Checkbox | |
| Season | Dropdown | `EmSeason` |
| Label | TextBox | |
| Dimension | TextBox | e.g. MA, UA, XA |
| Size Range | FKLookup | → TchpSizeRun |
| Final Merch Group A | TextBox | |

---

## Step 5 — Configure Form Design: Bill of Material

Unit: `BillOfMaterial` — **fully customer-configurable columns**.

> **Key principle:** Do NOT define fixed columns here. Configure a child grid with a minimal set of starter columns. Each customer adds their own columns via Form Builder.

**Starter columns (customer will add/remove/rename):**

| Column | Field Type | Notes |
|---|---|---|
| Sort | NumberInput | Row ordering |
| Category | TextBox | e.g. Main Fabric, Lining, Trim |
| Description | TextBox | Material description |
| Supplier | FKLookup | → Vendor entity (optional) |
| Content | TextBox | e.g. 100% Cotton |
| Color | TextBox | |
| Quantity | DecimalInput | |
| Unit | Dropdown | M, YD, PC, KG, SET |
| Notes | TextBox | |

**Formula example (Formula Engine):**
```
TotalMeterage = Quantity * StyleColorCount
```
Configure this via the **Calculation Validation Flow** tab in Form Builder.

---

## Step 6 — Configure Form Design: Construction, Placement & Folding, Costing, Testing

These are intentionally minimal — each customer fills in their own fields.

For each unit, configure a **Section-based layout** with:
- One rich-text/textarea field called `Instructions` or `Notes` (immediate value to customers)
- One child grid for structured rows (customer adds columns as needed)

Do not over-engineer these at the template level. The point is to give customers the section placeholders — they customize the fields.

---

## Step 7 — Configure Workflow: Style Status Transitions

Navigate to: **Workflow Engine** → New Workflow on `StyleTemplate` transaction.

| Transition | From Status | To Status | Approver Role | Trigger |
|---|---|---|---|---|
| Submit for Review | NEW / DEVELOPMENT | IN_REVIEW | — | User action button |
| Approve | IN_REVIEW | APPROVED | Merchandiser role | Approval stage |
| Reject | IN_REVIEW | DEVELOPMENT | Merchandiser role | Rejection action |
| Lock | APPROVED | LOCKED | System / auto | On ERP publish |
| Cancel | Any | CANCELLED | Admin role | Admin action |

Configure **NotifyTaskUser** on each approval stage to notify the submitter.

---

## Step 8 — Configure SearchEditor: Style List View

Navigate to: **Report & View → SearchEditor** → New View on `StyleTemplate`.

**Columns:**

| Column | Field | Sortable |
|---|---|---|
| Style # | StyleNumber | ✅ |
| Product Name | ProductName | ✅ |
| Season | Season | ✅ |
| Division | Division | ✅ |
| Brand | Brand | ✅ |
| Status | ProductStatus | ✅ |
| Designer | DesignerId | |
| Merchandiser | MerchandiserId | |
| Revised Date | AppModifiedDate | ✅ |

**Filters:**

| Filter | Type | Default |
|---|---|---|
| Season | Multi-select dropdown `EmSeason` | Current season |
| Status | Multi-select dropdown `EmStyleStatus` | All active |
| Division | Multi-select dropdown `EmDivision` | — |
| Style # / Product Name | Text search | — |
| Designer | FKLookup | — |

---

## Step 9 — Phase 2 Placeholders (do NOT configure yet)

The following units are created in the Unit hierarchy (Step 2) but left empty until Phase 2:
- `MasterGrading` — will be wired to `APP.TechPack.PluginEntry` via PluginWebApiCall
- `HowToMeasure` — will be wired to `TchpBodyPart` data
- `Fit1`–`Fit4`, `PP1`–`PP3` — will be wired to `TchpFitRound` via plugin

Leave these units with a placeholder text field (`PlaceholderNote`) so the tab renders in the sidebar but shows "Coming in Phase 2."

---

## Verification Checklist

After completing all steps, verify:

- [ ] Opening "Style Template" shows the 3 root groups in the left sidebar
- [ ] Expanding "01 Style Template" shows all 11 child tabs
- [ ] "Summary" tab renders all header fields, classification, roles, images
- [ ] "Style Color Details" shows two nested grids
- [ ] "Bill of Material" grid is editable with the starter columns
- [ ] Creating a new style and saving a color + BOM line succeeds
- [ ] Workflow "Submit for Review" button appears and changes status to IN_REVIEW
- [ ] Approval by a Merchandiser role changes status to APPROVED
- [ ] Style list view shows all columns and filters work correctly
- [ ] "Master Grading" tab shows placeholder note (not wired yet)

---

## Customer Customization Guide (hand to customers)

After importing this template, each brand customer should:

1. **Edit BOM columns** — go to Form Builder → BillOfMaterial unit → Form Design → add/remove/rename columns to match your BOM sheet
2. **Edit enum values** — add your seasons, product types, divisions
3. **Add Construction fields** — add specific fields for your garment type (e.g. seam type, stitch count for denim)
4. **Add Costing fields** — add your costing methodology (CM, CMT, FOB, DDP)
5. **Adjust workflow roles** — map Merchandiser/Designer roles to your actual user roles
6. **Add custom tabs** — create additional Child Units under any root group for brand-specific sections
