# Report Engine — User Guide

> **Who this guide is for:** Power users and report designers who want to create or edit print reports.
> No coding required. You only need to know which stored procedure your DBA set up for your report.

---

## What Is the Report Engine?

The Report Engine lets you design a print-ready PDF report by:
1. Connecting to a **stored procedure** (or API) that fetches your data
2. Building an **HTML layout** using visual blocks — no writing raw HTML
3. **Previewing** the output live with real data before saving
4. Printing it as a PDF directly from any transaction form

---

## Where to Find It

In the left sidebar, click **Report Management** (under Workflow Automation).

---

## Step 1 — Create a New Report

1. Click **+ Create Report**
2. Fill in the form:

   | Field | What to enter |
   |-------|--------------|
   | **Report Name** | A clear display name, e.g. `Style Summary` |
   | **Description** | Optional notes |
   | **Data Source** | Select **SP** and enter the stored procedure name, e.g. `sp_StyleSummaryReport` |
   | **Active** | Leave checked |

3. Click **Create**

> **Multiple data sources:** Click **+ Add Another Source** to add a second SP or API.
> Each extra source needs a short **alias** (e.g. `bom`, `costs`).
> Tokens from the second source use that alias as a prefix — see §5.

---

## Step 2 — Open the Designer

In the report list, click the **pencil / design icon** (context menu → Design) to open the Report Template Designer.

The designer has three areas:

```
┌── Left panel ──┬──────────── Right side ──────────────────────────┐
│  ⊞ Blocks      │  Live Preview (top half)                         │
│  ⟨⟩ Tokens    │  HTML / CSS editor (bottom half)                  │
└────────────────┴──────────────────────────────────────────────────┘
```

---

## Step 3 — Pick a Starter Template

In the left panel under **START FROM TEMPLATE**, click one that matches your report type:

| Template | Good for |
|----------|---------|
| **Style Summary** | Header info + one detail table |
| **Order Detail** | Header + line items + totals |
| **Simple List** | Plain list of rows |

The template appears in the editor and the live preview updates immediately.

---

## Step 4 — Insert Blocks

Use **INSERT BLOCK** to add sections without writing HTML:

| Block | What it inserts |
|-------|----------------|
| **Report Header** | Bold title + subtitle line |
| **Info Grid** | Two-column key-value table (good for header fields) |
| **Data Table** | Striped table with header row + `{{#each}}` loop |
| **Section Title** | Thin divider line with a heading |
| **Text Paragraph** | A single text field |
| **Image** | An `<img>` tag with a token URL |
| **Page Styles** | CSS reset for fonts, table borders, colours |

Click any block to insert it at the cursor in the editor.

---

## Step 5 — Place Tokens

Tokens are placeholders that get replaced with real data when the PDF is generated.

### Open the Tokens tab
1. In the left panel, click **⟨⟩ Tokens**
2. Click the **↻** (refresh) button — this calls your SP with a sample record to discover all available fields
3. Click any token to insert it at the cursor

### Token syntax

| What you type | What it renders |
|---------------|----------------|
| `{{header.StyleNumber}}` | A single field from the first result set (header row) |
| `{{header.Brand}}` | Another header field |
| `{{#each rs1}} … {{/each}}` | Repeats the inner HTML for every row in the second result set |
| `{{FieldName}}` | A column name **inside** an `#each` block |
| `{{#if header.Notes}} … {{/if}}` | Only shows if the field is not empty |
| `<img src="{{header.ImageUrl}}" />` | Image from a URL field |

> **Tip:** Your SP result sets map to these keys automatically:
> - **First result set (1 row)** → `header`
> - **Second result set (rows)** → `rs1`
> - **Third result set (rows)** → `rs2`

---

## Step 6 — Preview with Real Data

1. In the preview bar, enter a real **Ref ID** (e.g. the Style ID you want to test with)
2. Click **Refresh**
3. The preview iframe updates with live data from your SP

To see the exact PDF output:
- Click **PDF Preview** (red button, top right) — generates a real PDF and opens it in a new browser tab

---

## Step 7 — Adjust Page Settings

In the toolbar:

| Setting | Options |
|---------|---------|
| **Size** | A4, Letter, A3, Legal |
| **Orientation** | portrait, landscape |
| **Margin** | mm value (default 15) |

The preview reflects your page size choice immediately.

---

## Step 8 — Save

Click **Save** (top right). The template is saved and will be available in the Print dialog on transaction forms.

---

## Step 9 — Print from a Form

1. Open any transaction record (e.g. a Style record)
2. Click the **Report / Print** button in the form toolbar
3. The **Print Dialog** opens, listing all reports linked to that form
4. Tick the reports you want
5. Click **Save as PDF** — the PDF downloads to your browser

---

## Worked Example — Style Summary Report

### What we want to print
A one-page PDF per style showing:
- Style number, brand, season, gender in a header
- A fabric composition table

### SP contract (ask your DBA to create this)

```sql
CREATE PROCEDURE sp_StyleSummaryReport
    @MainReferenceId    INT,         -- the StyleId
    @MasterReferenceId  INT = NULL,
    @ExtraParams        NVARCHAR(MAX) = NULL
AS
BEGIN
    -- Result set 1: header (1 row)
    SELECT s.StyleNumber,
           b.BrandName          AS Brand,
           ss.SeasonName        AS Season,
           s.Gender,
           FORMAT(GETDATE(), 'yyyy-MM-dd') AS PrintedAt
    FROM   Style s
    LEFT   JOIN Brand  b  ON b.BrandId  = s.BrandId
    LEFT   JOIN Season ss ON ss.SeasonId = s.SeasonId
    WHERE  s.StyleId = @MainReferenceId

    -- Result set 2: fabric rows
    SELECT f.Position, f.FabricCode AS Code,
           f.Description, f.Colour
    FROM   StyleFabric f
    WHERE  f.StyleId = @MainReferenceId
    ORDER  BY f.Position
END
```

### Create the report
1. Report Management → **+ Create Report**
2. Name: `Style Summary`
3. Data Source: SP → `sp_StyleSummaryReport`
4. Click Create

### Design the template
1. Open Designer
2. Left panel → **Style Summary** starter template
3. The editor pre-fills with this HTML:

```html
<style>
  body { font-family: Arial, sans-serif; color: #222; padding: 24px }
  h1   { color: #1d4ed8; font-size: 22px; border-bottom: 2px solid #1d4ed8; padding-bottom: 8px }
  .meta{ font-size: 11px; color: #555; margin: 4px 0 16px }
  table{ border-collapse: collapse; width: 100%; font-size: 12px }
  th   { background: #1d4ed8; color: white; padding: 6px 10px; text-align: left }
  td   { padding: 5px 10px; border-bottom: 1px solid #e5e7eb }
  tr:nth-child(even) td { background: #f0f4ff }
</style>

<h1>{{header.StyleNumber}}</h1>
<div class="meta">
  <strong>{{header.Brand}}</strong> &nbsp;|&nbsp;
  Season: {{header.Season}} &nbsp;|&nbsp;
  Gender: {{header.Gender}} &nbsp;|&nbsp;
  Printed: {{header.PrintedAt}}
</div>

<h3>Fabric Composition</h3>
<table>
  <tr><th>Pos</th><th>Code</th><th>Description</th><th>Colour</th></tr>
  {{#each rs1}}
  <tr>
    <td>{{Position}}</td>
    <td>{{Code}}</td>
    <td>{{Description}}</td>
    <td>{{Colour}}</td>
  </tr>
  {{/each}}
</table>
```

4. Enter a real Style ID in **Ref ID**, click **Refresh**
5. Preview shows the style data populated
6. Click **PDF Preview** to verify the PDF layout
7. Click **Save**

### Result
Opening a Style form → Print button → select **Style Summary** → **Save as PDF** downloads a PDF like:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Style Summary: SHIRT-2026-001
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  CGS Brand  |  Season: Spring 2026  |  Gender: M  |  Printed: 2026-07-07

  Fabric Composition
  ┌─────┬──────────┬────────────────┬────────┐
  │ Pos │ Code     │ Description    │ Colour │
  ├─────┼──────────┼────────────────┼────────┤
  │  1  │ FAB-001  │ Cotton 100%    │ Blue   │
  │  2  │ FAB-002  │ Polyester Lining│ White │
  └─────┴──────────┴────────────────┴────────┘
```

---

## Adding a Filter Parameter (Extra Param)

If your SP accepts an optional filter — e.g. `@ShowCostPrice BIT = 0` — you can expose it as a user input in the Print Dialog.

Ask your DBA to add the parameter to the SP, then in the designer:
1. Click **N Sources** → expand the data sources panel
2. The `ExtraParamConfig` JSON can be edited to add:

```json
{
  "dataSources": [
    { "alias": "main", "type": "sp", "value": "sp_StyleSummaryReport" }
  ],
  "extraParams": [
    { "Name": "ShowCostPrice", "Label": "Show Cost Price", "DefaultValue": "0" }
  ]
}
```

The Print Dialog will show a **Show Cost Price** checkbox that passes the value to the SP.

---

## Quick Token Cheat Sheet

```
{{header.FieldName}}          ← single field from the header row
{{header.Brand}}              ← example

{{#each rs1}}                 ← start loop over detail rows
  {{FieldName}}               ← column inside the loop
{{/each}}                     ← end loop

{{#if header.Notes}}          ← conditional — only shows if not empty
  {{header.Notes}}
{{/if}}

<img src="{{header.ThumbnailUrl}}" />   ← image

-- Two data sources (alias "bom") --
{{bom.TotalFabrics}}          ← scalar from secondary source
{{#each bom_rs1}}             ← loop rows from secondary source
  {{FabricCode}}
{{/each}}
```

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Preview shows no data | Check the **Ref ID** — use a real record ID that exists in the DB |
| Token shows blank | Field name is case-sensitive; check spelling against the **Tokens** tab |
| "PDF Preview failed" | SP may have errored; check with your DBA that the SP runs with that ID |
| Table header shows but no rows | The `{{#each rs1}}` block uses `rs1` — confirm your SP returns a second result set |
| PDF Preview button is greyed out | The editor must have some HTML content; paste or select a starter template first |
