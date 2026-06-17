# PLM Template Import — Detailed Specification (Phase 5)

> **Status:** Confirmed — ready for implementation.  
> **Wizard step:** Step 3 — Template Import  
> **Migration plan:** [PLM Migration Plan.md §6](./PLM%20Migration%20Plan.md)  
> **Execution baseline:** [PLM-Import-Wizard-Baseline.md](./PLM-Import-Wizard-Baseline.md)  
> **PLM reference:** `C:\Dev\PLM3\PLM` — `PdmTemplateBL.cs`, `ReferenceTabValueLoadBL.cs`, tab/block/grid tables  
> **APP UI reference:** `TransactionGroupEditor.tsx` (Data Model Template Editor)

---

## 1. Scope

### In scope (v1 — structure import)

- Import **PLM template layout skeleton** from `pdmTemplate` hierarchy into App-Builder **Data Model Template** (`AppSearch`, `Type = DataModelTemplate`).
- Create/update per-tab **`AppTransaction`** (MasterDetail), units, fields, physical tables, forms, search dataset/view, folder linkage.
- **No product row data** in this step (`pdmBlockSubItemValue`, `pdmGridProductValue`).

### Out of scope (v1)

| Item | Handling |
|------|----------|
| **Master Reference Header** tab (`IsMasterReferenceHeaderTab`) | Skip — future phase |
| **Copy Tab** (`IsAllowProductTabCopy`, copy-tab fields) | Skip |
| Special grid **runtime behavior** (Size run, BOM, Color grid, Matrix logic, etc.) | Structure only; log `EmGridType` warning |
| Product reference **data** migration | Separate future step; this spec **prepares traceability** (§15) |

### Prerequisites

- Wizard **Step 1** complete (connection, application, table prefix).
- Wizard **Step 2** Entity import complete (for `EntityId` / DDL resolution via `AppEntityInfo.IntegrationId`).

---

## 2. Table prefix (wizard session)

Captured in **Step 1** (Connect & Discover) when session starts or on first open. Stored in `AppPlmImportSession.StepStateJson`.

| Setting | Default / rule | Used for |
|---------|----------------|----------|
| **`TablePrefix`** | `Plm_` | Template physical tables (§5); System Define PLM table copy (DSF=1) |
| **User Define wide** | `{TablePrefix}Entity_` | User Define wide tables (Step 2) — `Entity_` suffix fixed, not user-editable |

**Naming examples** (`TablePrefix = Plm_`):

| Object | Physical table |
|--------|----------------|
| Global product root | `Plm_ReferenceBasicInfo` |
| Tab sibling (Fabric Info) | `Plm_FabricInfo` |
| Grid child unit | `Plm_{SanitizedGridSubItemName}` |
| User Define wide entity | `Plm_Entity_{EntityCode}` (Step 2: `{TablePrefix}Entity_{code}`) |

Re-import trial data may be discarded; **no migration** of old names required — always apply current session prefix.

---

## 3. PLM source hierarchy (read path)

```
pdmTemplate
  └── pdmTemplateTab (TemplateID, TabID, Sort)
        └── pdmTab
              ├── IsTemplateHeaderTab → Header Tab
              ├── IsTabHeader + TabHeaderId → inner grouping (v1: fields stay on parent tab transaction)
              └── PdmTabBlock (TabID, BlockID, OrderId)
                    └── pdmBlock
                          └── pdmBlockSubItem
                                ├── ControlType ≠ Grid(6) → sibling field
                                └── ControlType = Grid(6), GridId → pdmGrid → pdmGridMetaColumn
```

**Product linkage (for future data import, not v1 execute):**

- `pdmProduct` ↔ `pdmTemplate` via **`pdmProductTemplate`** (M:N). No `TemplateId` on `pdmProduct`.
- Product key: **`ProductReferenceID`**.
- Block field values: `pdmProductVersion` + `pdmBlockSubItemValue`.
- Grid values: `pdmGridProductRow` + `pdmGridProductValue`.

---

## 4. APP target mapping

### 4.1 Data Model Template (= one PLM Template)

| PLM | APP object | Notes |
|-----|------------|-------|
| `pdmTemplate` | **`AppSearch`** (`EmAppSearchUsageType.DataModelTemplate`) | UI: Data Model Template / `TransactionGroupEditor` |
| Template name/description | `AppSearch.Name`, `Description` | |
| — | `AppSearch.SaasApplicationId` | From wizard Step 1 |
| — | **Dataset** (`AppDataSet`) | Query `{prefix}ReferenceBasicInfo` (§10) |
| — | **Search view fields / filters** | Template View Fields + Template Filters tabs |
| — | **Folder** | PLM template folder → APP folder navigation (§10) |

> **Not** `AppTransactionGroup` (that is business navigation grouping). Template editor APIs use `RetrieveAllAppTransactionGroupDto` naming but persist to **`AppSearch`**.

### 4.2 Template items (= PLM Tabs)

| PLM Tab type | APP Template item | `TemplateItemType` |
|--------------|-------------------|---------------------|
| Normal tab (`IsTemplateHeaderTab` null/false) | **Template Main Item** | `MainItem = 1` |
| Header tab (`IsTemplateHeaderTab = 1`) | **Template Shared Item** | `TemplateHeader = 2` |
| Copy tab | **Skip** | — |
| Master Reference Header | **Skip** (v1) | — |

Each imported tab → one **`AppTransaction`** (`EmTransactionOrganizedType.MasterDetail = 1`).

**Tab reuse:** Same `TabId` → **one** `AppTransaction`; multiple templates reference it via link targets. Update by `IntegrationId = Tab_{TabId}`.

### 4.3 Per-transaction unit model

Every tab transaction has:

```
AppTransaction (MasterDetail)
  ├── Root Unit (level 1, master)     → table {prefix}ReferenceBasicInfo  [GLOBAL, shared across all templates/tabs]
  ├── Sibling Unit (level 1)          → table {prefix}{SanitizedTabName}  [one per tab]
  └── Child Unit(s) (level 2, grid)   → table {prefix}{SanitizedGridSubItemName}  [one per Grid SubItem]
```

| PLM source | APP unit | APP fields |
|------------|----------|------------|
| Non-grid `pdmBlockSubItem` on tab | **Sibling Unit** columns | `AppTransactionField` on sibling unit |
| Grid `pdmBlockSubItem` (`ControlType=6`) | **Child Unit** (not a sibling column) | Fields from `pdmGridMetaColumn` |
| `Label(10)`, `Empty(17)` | **No DB column** | Form layout only (§8) |

**Root unit:** Always `{prefix}ReferenceBasicInfo`. **One table per tenant** (all templates share). `ReferenceId` = `int IDENTITY` PK.

**Sibling unit:** PK = `ReferenceId` `int NOT NULL` — **not identity**; links to root PK (`IsMasterSiblingUnit = true`, `IsLinkToParentPrimaryKey` on `ReferenceId` field).

**Child unit:** Parent = root unit. Table columns:

| Column | Rule |
|--------|------|
| `RowId` | `int IDENTITY` PK |
| `ReferenceId` | `int NOT NULL` FK → root |
| `Sort` | `int NULL` — row order |
| Meta columns | One per `pdmGridMetaColumn` (§6) |

---

## 5. Physical table DDL rules

### 5.1 `{prefix}ReferenceBasicInfo` (create once)

Standard columns (extend only via re-import **add column** policy §14):

```sql
ReferenceId       int IDENTITY(1,1) NOT NULL  -- PK
ReferenceCode     nvarchar(255) NULL
Description       nvarchar(255) NULL
Description2      nvarchar(255) NULL
Image             nvarchar(255) NULL
MasterReferenceId int NULL
FolderId          int NULL
AppCreatedByID    int NULL
AppCreatedDate    datetime NULL
AppModifiedByID   int NULL
AppModifiedDate   datetime NULL
```

Additional columns may be added when PLM `ReferenceStaticFiledId` / system subitems map here (§7).

### 5.2 Sibling table `{prefix}{SanitizedTabName}`

- **Required:** `ReferenceId int NOT NULL` (PK, FK to root).
- **Columns:** one per non-grid subitem on tab (all blocks flattened).
- **Column name:** `SanitizedSubItemName_{SubItemID}` (unique; SubItemID wins on collision).
- **SQL type:** from `EmControlType` / `Nbdecimal` (§6). Not all `nvarchar(255)`.

### 5.3 Child grid table `{prefix}{SanitizedGridSubItemName}`

- `RowId int IDENTITY PK`
- `ReferenceId int NOT NULL`
- `Sort int NULL`
- Grid meta columns: `SanitizedColumnName_{GridColumnID}` or `SanitizedColumnName` if unique within grid

### 5.4 Sanitization

- Remove spaces; replace non-alphanumeric with `_`; trim underscores.
- Prefix `T_` if first char is digit.
- Max length 100 for table names (truncate with `_{id}` suffix if needed).

---

## 6. Field mapping

### 6.1 SubItem → Sibling field

| PLM (`pdmBlockSubItem`) | APP (`AppTransactionField`) |
|---------------------------|-----------------------------|
| `SubItemName` | `DisplayName` |
| Sanitized name + `_{SubItemID}` | `DataBaseFieldName` |
| `ControlType` | `ControlType` (`EmAppControlType`) — §6.3 |
| `Nbdecimal` | Numeric precision (control params) |
| `EntityId` | Resolve → `EntityId` on APP field (§6.4) |
| `SortOrder` | `SortOrder` |
| `SubItemID` | `IntegrationId = SubItem_{SubItemID}` |

**Skip DB column:** `ControlType` in `Label(10)`, `Empty(17)` — layout only.

**Skip sibling column:** `ControlType = Grid(6)` — use child unit instead.

### 6.2 Grid MetaColumn → Child field

| PLM (`pdmGridMetaColumn`) | APP field |
|---------------------------|-----------|
| `ColumnName` | `DisplayName` |
| Sanitized + `_{GridColumnID}` | `DataBaseFieldName` |
| `ColumnTypeId` | `ControlType` |
| `Nbdecimal` | Numeric params |
| `EntityId` | Resolved APP `EntityId` |
| `ColumnOrder` | `SortOrder` |
| `GridColumnID` | `IntegrationId = Grid_{GridColumnID}` |

### 6.3 Control type mapping (PLM `EmControlType` → APP `EmAppControlType`)

v1: **same numeric value** when both enums define the type. Map by value; display names are similar.

| Value | PLM / APP (shared) |
|------:|-------------------|
| 1 | DDL |
| 2 | TextBox |
| 4 | Memo |
| 5 | Image |
| 6 | Grid (subitem only — unit level, not field on sibling) |
| 7 | Date |
| 9 | File |
| 10 | Label (no DB column) |
| 13 | CheckBox |
| 17 | Empty (no DB column) |
| 20 | Numeric |
| 23 | AutoGeneration |
| 24 | RGBColorDisplay |
| 27 | DateTimeDetail |
| 33 | SearchAndView |
| 38 | AutoComplete |
| 39 | RadioButtons |
| 99 | InvalidControlType — preview warning, skip or label-only |

PLM-only types without APP match → nearest type or `InvalidControlType` + **preview warning** + log.

### 6.4 Entity ID resolution (DDL / entity-bound fields)

```
PLM EntityId (pdmEntity.EntityID)
  → AppEntityInfo WHERE IntegrationId = PLM EntityID
  → AppEntityInfo.EntityInfoID → AppTransactionField.EntityId
```

If entity not imported → preview **blocker** or **warning** (configurable per field criticality).

### 6.5 SQL column types (from control type)

| Control type | SQL type (typical) |
|--------------|-------------------|
| DDL (entity FK) | `int NULL` |
| TextBox, Memo, File, Image path | `nvarchar(n)` per max length |
| Numeric | `decimal(18, n)` per `Nbdecimal` |
| Date / DateTimeDetail | `datetime NULL` |
| CheckBox | `bit NULL` |
| AutoGeneration | `int` or `nvarchar` per PLM `DataType` |

Empty string defaults for NOT NULL APP columns where needed (align with Entity import policy).

---

## 7. ReferenceStaticFieldId / system subitems

If `pdmBlockSubItem.ReferenceStaticFiledId` maps to a **standard product header field**:

1. If `{prefix}ReferenceBasicInfo` **already has** that column → map field to **root unit**, no sibling column.
2. Else → map to **sibling unit** column (or add column to ReferenceBasicInfo on re-import if business rules later require).

Known root candidates: `ReferenceCode`, `Description`, `Description2`, `Image`, `FolderId`, `MasterReferenceId`.

---

## 8. Form layout generation

**Required** for each imported `AppTransaction`.

1. **Preferred:** Read relational PLM layout tables:
   - `pdmTabLayout` / `pdmTabLayoutItem` / `pdmTabLayoutSubitem`
   - Block order, row/column, `ImageOrMemoWidth`, `ImageOrMemoHight`, subitem positions
   - Generate `AppForm` + `AppFormLayoutItem` (approximate PLM layout)

2. **Fallback:** APP built-in **auto-create form** from data model (field `SortOrder` vertical stack).

3. **Label / Empty:** Include in form layout only (no `AppTransactionField` DB binding, or label-only layout item).

`pdmTab.Uilayout` (binary blob) — **do not parse in v1**.

---

## 9. Template Search / Dataset / Folder (TransactionGroupEditor parity)

Import must populate what admins configure manually today:

| Editor area | Import creates/updates |
|-------------|------------------------|
| **Dataset** | `AppDataSet` querying `{prefix}ReferenceBasicInfo` |
| **Template Main Items** | Link targets → normal tab transactions; sort = `pdmTemplateTab.Sort` |
| **Template Shared Items** | Link targets → header tab transactions; `TemplateItemType = TemplateHeader` |
| **View field mapping** | Source view column → `ReferenceId` (and display columns) |
| **Template Filters** | Initial filter fields from PLM search flags where applicable |
| **Folder navigation** | `TemplateFolderNavigationSection` config from PLM template folder |

Each link target: `TargetColumn` / PK mapping → **`ReferenceId`**.

---

## 10. IntegrationId scheme (re-import keys)

| Object | IntegrationId format | Example |
|--------|---------------------|---------|
| Data Model Template | `Template_{TemplateID}` | `Template_42` |
| Transaction (tab) | `Tab_{TabID}` | `Tab_79096` |
| Root unit | `Unit_ReferenceBasicInfo` | fixed singleton per app |
| Sibling unit | `Unit_Sibling_{TabID}` | `Unit_Sibling_79096` |
| Child unit (grid) | `Unit_Grid_{SubItemID}` | `Unit_Grid_12345` |
| SubItem field | `SubItem_{SubItemID}` | `SubItem_999` |
| Grid column field | `Grid_{GridColumnID}` | `Grid_555` |

Stored on `AppTransaction`, `AppTransactionUnit`, `AppTransactionField` (`nvarchar(100)`).  
`AppSearch` template: store PLM `TemplateID` in searchable metadata or dedicated session map.

**Update rule:** Match existing rows by `IntegrationId` only; insert when missing.

---

## 11. Re-import policy

| Object | Add | Remove | Alter |
|--------|-----|--------|-------|
| `AppTransaction` / units / fields | Yes | Warn only | Update metadata |
| Physical tables / columns | **ADD** columns | **Never drop** | `ALTER TABLE ADD` |
| Removed PLM subitem/column | — | — | **Warning** in preview + log; leave orphan APP column |

Full template execute: **one transaction**; failure → **full rollback** (wizard rule F25).

---

## 12. Preview grid (wizard UI)

Suggested columns: Template name, Tab name, Tab type, Transaction action (Insert/Update), Unit/table names, Field count, Blockers, Warnings.

**Warnings (non-blocking):**

- `EmGridType` ≠ `RegularGrid(1)` — special behavior deferred
- Unmapped control type
- Orphan entity reference
- Removed PLM field still in APP DB

Write warnings to **`AppPlmImportLog`** for later reference.

---

## 13. Import order (single template execute)

```
1. Ensure {prefix}ReferenceBasicInfo exists
2. Upsert AppSearch (template) + dataset + folder
3. For each tab (header tabs first in shared list, then main tabs by Sort):
   a. Upsert AppTransaction
   b. Ensure root unit → ReferenceBasicInfo
   c. Create/alter sibling table + sibling unit + fields
   d. For each grid subitem: create/alter child table + child unit + fields
   e. Generate/update AppForm layout
4. Save template link targets (main + shared items)
5. Save search view fields / filters
```

**Dependency order across templates:** Import shared tabs once (by `TabId`); then attach to each template.

---

## 14. Product reference import — preparation (future)

v1 structure import must preserve enough mapping for a future **Product Reference** step:

| PLM | APP storage location |
|-----|---------------------|
| `ProductReferenceID` | `{prefix}ReferenceBasicInfo.ReferenceId` |
| Block subitem value | Sibling column `SanitizedSubItemName_{SubItemID}` OR root column |
| Grid cell | Child table `{prefix}{GridSubItem}` row + column `_{GridColumnID}` |
| Template/tab/block/subitem/grid ids | `IntegrationId` on transaction/unit/field |

Retain in import log / optional mapping table:  
`(PlmTemplateId, PlmTabId, PlmBlockId, PlmSubItemId, PlmGridColumnId)` → `(AppTransactionId, AppUnitId, AppFieldId, TableName, ColumnName)`.

---

## 15. Inner Tab Header (`IsTabHeader`)

v1: Treat as **part of the parent tab's transaction** (same sibling unit / form). Do not create separate transaction.

---

## 16. API / BL (implementation pointers)

| Endpoint | Purpose |
|----------|---------|
| `POST PreviewTemplateMapping` | Build preview DTO from PLM + tenant |
| `POST ExecuteTemplateImport` | Async job |

**BL file:** `PlmMigrationBL.Template.cs` (replace stub).  
**Job type:** `TemplateImport`.

**Session state:** `tablePrefix` from Step 1 (`entityWideTablePrefix` derived as `{tablePrefix}Entity_`).

---

## 17. Change log

| Date | Change |
|------|--------|
| 2026-06-16 | Initial detailed spec from PLM code review + confirmed business rules |
