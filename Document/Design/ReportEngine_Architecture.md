# Report Engine Architecture

**Replaces:** Crystal Reports (`.rpt`) and DataDynamics ActiveReports (`.rdlx`)
**Stack:** .NET 10 + React 18 + Playwright headless Chromium
**Date:** 2026-07-07

---

## 1. Overview

The report engine is a platform-native HTML + PDF print engine that replaces the legacy Crystal Reports and ActiveReports files used in the old BlueCherry PLM system. It preserves the original design philosophy:

- A DBA or developer writes a **stored procedure** (or API endpoint) that shapes the data for a specific report.
- A **report designer** (power user, not developer) opens the Report Template Designer, picks tokens from the SP result, and builds an HTML layout without writing code.
- Users open the **Print Dialog** from any transaction form, select which reports to print, and download a PDF.

**Key principle:** Reports are _read-only display documents_ fully decoupled from the transaction form's field structure. The SP does all joins, calculated fields, and formatting. The template contains only HTML + `{{token}}` placeholders.

---

## 2. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        BROWSER (React)                          │
│                                                                 │
│  ReportManagement.tsx          ReportTemplateDesigner.tsx       │
│  ─ CRUD list of reports        ─ Monaco HTML editor             │
│  ─ Create: name + data sources ─ Block library (starter tmpl)  │
│  ─ Delete with template        ─ Token picker (from SP result)  │
│                                ─ Live iframe preview (HTML)     │
│  TabPrintDialog.tsx            ─ PDF Preview button → new tab  │
│  ─ Lists reports for a form    ─ Save template                  │
│  ─ Checkbox section select                                      │
│  ─ Save as PDF button          DataSourceEditor.tsx             │
│                                ─ Multi-source (SP + API)        │
│                                ─ Alias naming for tokens        │
└───────────────┬─────────────────────────┬───────────────────────┘
                │ REST (JSON)              │ REST (JSON)
┌───────────────▼─────────────────────────▼───────────────────────┐
│                   AppReportController.cs                        │
│                                                                 │
│  POST  /PreviewHtml   → rendered HTML string (live preview)     │
│  POST  /PreviewPdf    → inline PDF (opens in new tab)           │
│  POST  /GeneratePdf   → attachment PDF (download)               │
│  GET   /GetTokens     → token list from SP result schema        │
│  POST  /CreateRequest → store batch print request               │
│  GET   /ValidatePrintToken → Playwright auth bypass             │
└──────────┬─────────────────────────┬────────────────────────────┘
           │                         │
┌──────────▼──────────┐  ┌───────────▼─────────────┐
│ AppReportTemplate   │  │  AppReportPdfService     │
│ Service.cs          │  │  .cs                     │
│                     │  │                          │
│ ParseDataSources()  │  │ GeneratePdfAsync()       │
│ FetchData()         │  │ BuildCombinedHtml()      │
│ RenderTemplate()    │  │ RenderHtmlToPdfAsync()   │
│ GetAvailableTokens()│  │   └─ Playwright          │
│ MergeResultSets()   │  │      headless Chromium   │
└──────────┬──────────┘  └──────────────────────────┘
           │
┌──────────▼──────────┐
│  SQL Server          │
│  (Tenant DB)         │
│                      │
│  AppReport           │
│  AppReportTemplate   │
│  AppReportLog        │
│  sp_* (report SPs)   │
└──────────────────────┘
```

---

## 3. Database Schema

### `AppReport`
Existing table extended with new columns:

| Column | Type | Purpose |
|--------|------|---------|
| `ReportId` | int PK | Primary key |
| `ReportName` | nvarchar(200) | Display name |
| `ReportFileName` | nvarchar(200) | Legacy / primary SP name (backward compat) |
| `Description` | nvarchar(max) | Optional description |
| `Active` | bit | Soft-disable a report |
| `TransactionId` | int FK | Links report to a form/transaction type |

### `AppReportTemplate`
New table storing the HTML template and config:

| Column | Type | Purpose |
|--------|------|---------|
| `Id` | int PK identity | Primary key (**note: `Id`, not `ReportId`**) |
| `ReportId` | int FK | Parent `AppReport` |
| `TemplateHtml` | nvarchar(max) | HTML layout with `{{token}}` placeholders |
| `DataSpName` | nvarchar(200) | Primary SP name (legacy single-source field) |
| `PageSize` | nvarchar(10) | `A4` / `Letter` / `A3` / `Legal` |
| `Orientation` | nvarchar(11) | `portrait` / `landscape` |
| `MarginMm` | int | Page margin in mm (default 15) |
| `ExtraParamConfig` | nvarchar(max) | JSON — multi-source config (see §6) |

### `AppReportLog`
Audit trail for every PDF generation:

| Column | Type | Purpose |
|--------|------|---------|
| `LogId` | int PK identity | |
| `ReportId` | int | Which report was printed |
| `RequestId` | int? | Batch request ref (optional) |
| `MainReferenceId` | int | Record ID that was printed |
| `RequestedBy` | int | User ID |
| `RequestedAt` | datetime | Timestamp |
| `DurationMs` | int | Playwright render time |
| `PageCount` | int | PDF page count |
| `ClientIp` | nvarchar(50) | |

---

## 4. Token System

Templates use a Handlebars-inspired syntax. Token resolution is done server-side in `AppReportTemplateService.RenderTemplate()`.

### 4.1 Syntax

| Syntax | Meaning |
|--------|---------|
| `{{header.StyleNumber}}` | Scalar field from the first result set |
| `{{#each rs1}}...{{/each}}` | Loop over rows of a result set |
| `{{FieldName}}` | Field reference _inside_ an `#each` block |
| `{{#if header.Brand}}...{{/if}}` | Conditional block |
| `<img src="{{header.ImageUrl}}" />` | Image token |

### 4.2 Token Context Keys

**Primary data source** (first in the list) maps to backward-compatible keys:

| Result set | Context key | Template usage |
|-----------|-------------|----------------|
| RS0 (1 row) | `header` | `{{header.FieldName}}` |
| RS1 | `rs1` | `{{#each rs1}}...{{/each}}` |
| RS2 | `rs2` | `{{#each rs2}}...{{/each}}` |

**Additional sources** (alias `"bom"` as example):

| Result set | Context key | Template usage |
|-----------|-------------|----------------|
| RS0 (1 row) | `bom` | `{{bom.FieldName}}` |
| RS1 | `bom_rs1` | `{{#each bom_rs1}}...{{/each}}` |
| RS2 | `bom_rs2` | `{{#each bom_rs2}}...{{/each}}` |

---

## 5. Stored Procedure Contract

Every report SP must accept these parameters:

```sql
CREATE PROCEDURE sp_MyReport
    @MainReferenceId    INT,
    @MasterReferenceId  INT          = NULL,
    @ExtraParams        NVARCHAR(MAX) = NULL   -- JSON for optional filters
AS
BEGIN
    -- Result set 1: header scalars (1 row)
    -- All values must be display-ready strings — no FK IDs, no coded values
    -- Resolve dropdowns via JOIN, format dates/numbers in SQL
    SELECT s.StyleNumber,
           b.BrandName  AS Brand,              -- JOIN to resolve dropdown
           FORMAT(s.CreatedDate, 'yyyy-MM-dd') AS PrintedAt
    FROM   Style s
    LEFT   JOIN Brand b ON b.BrandId = s.BrandId
    WHERE  s.StyleId = @MainReferenceId

    -- Result set 2+: list rows
    SELECT f.Position, f.Code, f.Description, f.Colour
    FROM   StyleFabric f
    WHERE  f.StyleId = @MainReferenceId
    ORDER  BY f.Position
END
```

**Rules:**
- RS0 should be a single row (becomes the `header` scalar dict)
- RS1+ are lists (become `rs1`, `rs2`, … array dicts)
- All display values resolved by the SP — no IDs or enum codes in the template
- No interactive controls in the template output (no dropdowns, no FlexGrid, no buttons)

---

## 6. Multi-Source Data Config

`AppReportTemplate.ExtraParamConfig` stores a JSON object:

```json
{
  "dataSources": [
    { "name": "header", "type": "sp", "value": "sp_StyleSummary" },
    { "name": "bom",    "type": "sp", "value": "sp_StyleBomDetail" }
  ],
  "extraParams": [
    { "Name": "ShowCost", "Label": "Show Cost", "DefaultValue": "false" }
  ]
}
```

**Backward compatibility:**
- If `ExtraParamConfig` starts with `{` → parsed as `TemplateConfig` (new format)
- If `ExtraParamConfig` starts with `[` → treated as legacy `ExtraParamDef[]` array only
- If `ExtraParamConfig` is null/empty → falls back to `DataSpName` as single SP source

`AppReportTemplateService.ParseDataSources()` handles all three cases transparently.

---

## 7. Backend Services

### `AppReportTemplateService.cs` (`APP.BL/TenantBusiness/`)

Pure static service, no DI dependency.

| Method | Purpose |
|--------|---------|
| `FetchData(template, mainRefId, ...)` | Calls all configured SPs, merges results into token context dict |
| `RenderTemplate(html, template, context)` | Injects page CSS, resolves `#each`, `#if`, scalar tokens |
| `GetAvailableTokens(template)` | Calls each SP with `@MainReferenceId=0` to discover field names for the token picker |
| `ParseDataSources(template)` | Reads `ExtraParamConfig.dataSources` or falls back to `DataSpName` |
| `MergeResultSets(context, ds, isPrimary, alias)` | Maps DataSet tables to context keys with correct primary/alias naming |

**ADO.NET pattern used** (LLBLGen `DataAccessAdapter` does not expose `CreateCommand`):
```csharp
private static string GetConnectionString()
{
    using var adapter = AppTenantAdapterBL.GetTenantAdapter();
    return adapter.ConnectionString;
}

using var conn = new SqlConnection(GetConnectionString());
using var cmd  = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };
```

### `AppReportPdfService.cs` (`APP.BL/TenantBusiness/`)

| Method | Purpose |
|--------|---------|
| `GeneratePdfAsync(request)` | Multi-section PDF — fetches, renders, page-breaks, calls Playwright |
| `BuildCombinedHtml(request)` | Assembles sections separated by `page-break-after:always` |
| `RenderHtmlToPdfAsync(html, pageSize, orientation, marginMm)` | Raw Playwright call — accepts pre-rendered HTML |

**Playwright PDF options** (v1.49 `Margin` struct API):
```csharp
new PagePdfOptions
{
    Format          = pageSize,
    Landscape       = orientation == "landscape",
    PrintBackground = true,
    Margin = new Margin { Top = margin, Bottom = margin, Left = margin, Right = margin }
}
```

### `AppReportTemplateBL.cs` (`APP.BL/TenantBusiness/`)

CRUD layer for `AppReport` + `AppReportTemplate` entities, plus `WriteLog()`.

---

## 8. API Endpoints

All under `AppReportController` at route prefix `/webapi/AppReport/`:

| Method | Endpoint | Request | Response | Purpose |
|--------|----------|---------|----------|---------|
| POST | `/PreviewHtml` | `PreviewHtmlRequest` | `text/html` | Live preview in designer iframe |
| POST | `/PreviewPdf` | `PreviewPdfRequest` | `application/pdf` inline | PDF Preview button — opens in new tab |
| POST | `/GeneratePdf` | `GeneratePdfRequest` | `application/pdf` attachment | Download PDF from TabPrintDialog |
| GET | `/GetTokens?reportId=N` | — | `TokenDescriptor[]` | Token picker in designer |
| POST | `/CreateRequest` | `CreatePrintRequestDto` | `{ RequestId }` | Batch print queue |
| GET | `/ValidatePrintToken?token=X` | — | `{ Valid, PrintParam }` | One-time Playwright auth |

**`PreviewPdf` vs `GeneratePdf`:**
- `PreviewPdf` accepts `templateHtmlOverride` (current unsaved HTML) and returns `Content-Disposition: inline` so the browser renders the PDF natively in a new tab
- `GeneratePdf` loads saved templates by `ReportId`, returns `Content-Disposition: attachment`

---

## 9. Frontend Components

### `ReportManagement.tsx` (`AppReact/src/components/admin/`)
- Wijmo FlexGrid listing all `AppReport` records
- **Create Report** modal: report name, description, multi-source data sources (`DataSourceEditor` full mode), Active toggle
- **Delete** with confirmation (also deletes the linked `AppReportTemplate`)
- Context menu: Design (opens `ReportTemplateDesigner`) + Delete
- Route: `/report-management` — accessible from the sidebar under **Report Management**

### `ReportTemplateDesigner.tsx` (`AppReact/src/components/admin/`)
Full-screen designer popup with:

```
┌─ Toolbar ──────────────────────────────────────────────────────────────────┐
│  [Report Name]  [N Sources ▾]  Size:[A4]  [portrait]  Margin:[15]          │
│                               [PDF Preview]  [💾 Save]  [✕]               │
├─ Data Sources panel (expandable) ──────────────────────────────────────────┤
│  DataSourceEditor compact mode — one row per source                        │
├─ Left panel (w-52) ──┬─ Right side ────────────────────────────────────────┤
│  ⊞ Blocks | ⟨⟩ Tokens│  Preview — A4 portrait    Ref ID: [___] [Refresh]  │
│                      │  ┌─ iframe live preview ─────────────────────────┐  │
│  START FROM TEMPLATE │  │  (HTML from PreviewHtml endpoint)             │  │
│  · Style Summary     │  │  auto-updates 700ms after keystroke           │  │
│  · Order Detail      │  └───────────────────────────────────────────────┘  │
│  · Simple List       ├─ HTML / CSS editor ─────────────────────────────────┤
│                      │  Monaco HTML editor                               │  │
│  INSERT BLOCK        │  click blocks to insert at cursor                 │  │
│  · Report Header     │                                                   │  │
│  · Info Grid         │                                                   │  │
│  · Data Table        │                                                   │  │
│  · Section Title     │                                                   │  │
│  · Text Paragraph    │                                                   │  │
│  · Image             │                                                   │  │
│  · Page Styles       │                                                   │  │
└──────────────────────┴───────────────────────────────────────────────────┘
```

**PDF Preview flow:**
1. User sets Ref ID (a real record ID for live data)
2. Clicks **PDF Preview** button
3. `POST /PreviewPdf` with current HTML + page settings → Playwright renders PDF
4. Blob URL opened in new browser tab → browser PDF viewer

### `DataSourceEditor.tsx` (`AppReact/src/components/admin/`)
Shared component for managing `DataSourceDef[]`. Two modes:

| Mode | Used in | Layout |
|------|---------|--------|
| `compact` | Designer toolbar panel | Horizontal row per source |
| full (default) | Create Report modal | Vertical cards with description |

Exported utilities:
- `parseSourcesFromConfig(extraParamConfig, dataSpName)` — parse from DB
- `serializeConfig(sources, existingConfig)` — serialize to `ExtraParamConfig` JSON

### `TabPrintDialog.tsx` (`AppReact/src/components/formMgt/`)
Modal opened from the Print button on any transaction form:
- Fetches reports linked to `transactionId` via `/RetrieveAllAppTranscationReportListByTransactionId`
- Checkbox selection of which reports to include
- **Save as PDF** → `POST /GeneratePdf` → file download

Wired in `FormMainMenus.tsx`: `handleReport()` sets `tabPrintOpen = true` → renders `<TabPrintDialog>`.

---

## 10. Playwright Setup

**Package:** `Microsoft.Playwright` v1.49.0 in `AppAI.Web/AppAI.Web.csproj`

**Chromium install** (one-time, run on dev machine and server):
```bash
# From the AppAI.Web project directory
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

Playwright runs headless within the .NET process — no separate browser process management needed.

---

## 11. File Map

| File | Role |
|------|------|
| `APP.BL/TenantBusiness/AppReportTemplateService.cs` | SP data fetch, token rendering, token discovery |
| `APP.BL/TenantBusiness/AppReportPdfService.cs` | Playwright PDF orchestration |
| `APP.BL/TenantBusiness/AppReportTemplateBL.cs` | CRUD for AppReport / AppReportTemplate / AppReportLog |
| `AppAI.Web/Controllers/AppReportController.cs` | REST endpoints |
| `AppAI.Web/Services/PrintTokenService.cs` | One-time token for Playwright auth |
| `AppReact/src/components/admin/ReportManagement.tsx` | Report list + Create/Delete UI |
| `AppReact/src/components/admin/ReportTemplateDesigner.tsx` | Visual report designer |
| `AppReact/src/components/admin/DataSourceEditor.tsx` | Multi-source editor component |
| `AppReact/src/components/formMgt/TabPrintDialog.tsx` | Print dialog for transaction forms |
| `AppReact/src/webapi/appReportSvc.ts` | Frontend API service |
| `AppReact/src/components/mainLayout/Sidebar.tsx` | Static menu entry (Sort: 2.8) |
| `AppReact/src/routes.shared.tsx` | Route: `/report-management` |

---

## 12. Design Decisions & Rationale

| Decision | Rationale |
|----------|-----------|
| HTML + Playwright, not SSRS/Crystal | No license cost; HTML/CSS is universally understood; Playwright is already in the .NET ecosystem |
| SP returns display-ready strings | Template stays simple (no lookups, no formatting logic); mirrors how Crystal Reports SPs worked |
| `ExtraParamConfig` JSON in existing column | Zero schema migration for multi-source; backward compat maintained via prefix detection (`{` vs `[`) |
| `header`/`rs1` keys preserved | Reports created before multi-source feature continue to work unchanged |
| `RenderHtmlToPdfAsync` accepts raw HTML | Decouples PDF rendering from template loading — enables `PreviewPdf` with unsaved HTML |
| `Content-Disposition: inline` for PreviewPdf | Browser opens PDF natively without triggering a download, better UX in designer |
| `blob:` URL + `window.open` | Avoids navigating away from the designer; new tab = clean PDF viewer |
