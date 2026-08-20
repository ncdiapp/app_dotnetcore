# Import App Config Pack — JSON contract (v1)

Use this file to **write or generate** a portable App Config Pack JSON, then import it from **My Application Editor → Import Config**.

The same schema is used for:

- **Hand-written / AI-generated** specs (`source.generatedBy`: `manual` or `ai`)
- **Export** from an existing Application (`source.generatedBy`: `export`)

Runtime APIs live under `/webapi/AppConfigPack/` (not PLM Migration). No FieldMapping, no PLM connection.

## Pipeline

1. **DDL** — create missing tables; **ADD** missing columns only; `CREATE OR ALTER VIEW`
2. Refresh tenant schema cache
3. **Simple list entities** — upsert by `entityCode` (`EmAppEntityType.SimpleValueList` = 4); replace values by `code`
4. **Transactions** — upsert by `integrationId` via table hierarchy (Root / Sibling / Child / Grandchild)
5. Overlay field metadata (control type, entity/LOV, **query datasource**, visibility, pivot flags, cascading DDL, PK / parent-link, **decimal places**)
6. Overlay unit display names, grid display type, Available Select pairing (`AvailableSourceUnitId` + field mapping)
7. Auto-wire child/sibling **Link To Parent PK** (DB FK, same column name, or StyleSpecId↔ReferenceId). VIEW units without a SQL PK get a logical PK.
8. **Form layout** — if `formLayout.items` is present, delete the existing Flex tree and rebuild it (portable widget names, bind by table/column/command). If omitted, create the default Flex form when missing, then apply `layoutTab` / `layoutHostTable`
9. **Commands** — upsert by `name` on the transaction (Execute SQL / Refresh / Composition); rewrite `[TF:Table.Column]` tokens; optional CommandActionButton above a child grid
10. Child-grid **Link Targets** (Create/Edit/Delete) — after all transaction ids exist
11. Transaction Group (Data Model Template)
12. **Searches** — DataSet SQL, criteria, SearchView fields, linkTargets, optional main menu
13. **Transaction extras** (omit = keep default / existing; `[]` = clear): header buttons + read-only, Data Load, Unit Formula, Conditional Action, Linked Search mappings
14. Attach TX + Search as Application assets

## Matching / safety

| Rule | Behavior |
|------|----------|
| Stable key | `integrationId` (required on each transaction and search) |
| Exists | **Update** name + header flags (when set) + field overlay (incl. query datasource) + commands + form-if-missing. Does not rebuild unit tree. Optional extras (`dataLoads` / `unitFormulas` / `conditionalActions` / `linkedSearches`): omit = leave existing; `[]` = clear; otherwise replace. |
| Missing | **Insert** |
| DROP | Never drop tables or columns |
| Type change | Warning only; existing column types are not altered |
| Views | Always `CREATE OR ALTER VIEW` |
| IDs | Never put numeric TransactionId / SearchId / FieldId / CommandId in the pack. Use names + integrationId. SQL tokens are `[TF:Table.Column]`. |

## Top-level shape

```json
{
  "schemaVersion": 1,
  "generatedAt": "2026-08-17T00:00:00Z",
  "source": { "generatedBy": "manual", "applicationName": "Demo", "notes": "" },
  "tables": [ ],
  "views": [ ],
  "simpleListEntities": [ ],
  "transactions": [ ],
  "transactionGroup": { },
  "searches": [ ]
}
```

One file may contain **many** transactions and **many** searches.

## Tables

Each table: `name`, optional `schemaOwner` (default `dbo`), `columns[]`.

Column fields:

- `name` (required)
- `dataType` — SQL Server type without length, e.g. `INT`, `NVARCHAR`, `DECIMAL`, `DATETIME`, `BIT`
- `length` — for `NVARCHAR`/`VARCHAR` (`-1` = MAX)
- `precision` / `scale` — for `DECIMAL`/`NUMERIC`
- `isPrimaryKey`, `isNullable`, `isAutoIncrement`
- `defaultValue` — raw SQL fragment (e.g. `0`, `GETDATE()`)

Relationships (optional): `type` = `MANY_TO_ONE`, `targetTable`, `foreignKeyColumn`, `referencedColumn`.

On import: `IF NOT EXISTS` → `CREATE TABLE`; existing table → `ALTER TABLE ... ADD` for missing columns only.

## Views

- `name`
- `createOrAlterSql` — full statement beginning with `CREATE OR ALTER VIEW` (or `CREATE VIEW`, importer wraps it)

Views run **after** tables so they can select from newly created tables.

## Simple list entities

Top-level `simpleListEntities[]`. Upserted **before** transactions so `fields[].entityCode` can resolve.

```json
{
  "entityCode": "QcTargetMarket",
  "description": "QC Target Market",
  "values": [
    { "internalKey": 1, "code": "US", "description": "United States", "sort": 10 },
    { "internalKey": 2, "code": "EU", "description": "European Union", "sort": 20 }
  ]
}
```

- `entityCode` (required) — `AppEntityInfo.EntityCode`. Existing rows are updated; missing rows are inserted (`EntityType` = 4).
- `values[]` are matched by `code`. Import updates existing values, inserts missing ones, and **deletes** values whose code is no longer in the pack.
- `internalKey` is the DDL lookup **Id**. Simple-list dropdowns **store InternalKey** (as a string in NVARCHAR columns), not `code`. SQL that compares codes (e.g. `TargetMarket = N'US'`) must join `AppEntitySimpleListValue` and accept either InternalKey or leftover Code.

Wire a field with `"controlType": 1` and `"entityCode": "QcTargetMarket"`. Do **not** set `ddlQueryText` on that field (query datasource clears `EntityId`).

## Screen patterns (choose first)

Before writing JSON, pick **one** pattern. Do not invent a Search when the user asked for List Edit.

| Pattern | When | Pack shape | Main menu |
|---------|------|------------|-----------|
| **A. Search + MasterDetail** | Complex form (tabs, siblings, create/edit dialog from a query list) | `organizedType: "MasterDetail"` (or omit) + `searches[]` with `linkTargets` | `searches[].menu` |
| **B. ListEdit Transaction** | Simple grid CRUD on one table (or hierarchical List root+children). User says **List Edit / ListEdit / LIST EDIT TRANSACTION** | **One** transaction: `organizedType: "List"` (alias `ListEdit`). **No** Search, **no** Create/Edit `linkTargets` | `transactions[].menu` |

**Default when ambiguous:** ask which pattern.  
**When user says List Edit / ListEdit:** always **B** — never split into Search + MasterDetail.

ListEdit example (single table + main menu):

```json
{
  "integrationId": "ListEdit_ErpCustomer",
  "name": "Customers",
  "organizedType": "List",
  "unitStructure": {
    "rootTableName": "Erp_Customer",
    "rootDisplayName": "Customer",
    "siblingTableNames": [],
    "childUnits": []
  },
  "fields": [
    { "tableName": "Erp_Customer", "columnName": "CustomerId", "isVisible": false, "isPrimaryKey": true },
    { "tableName": "Erp_Customer", "columnName": "CustomerCode", "controlType": 2, "isVisible": true },
    { "tableName": "Erp_Customer", "columnName": "CustomerName", "controlType": 2, "isVisible": true },
    { "tableName": "Erp_Customer", "columnName": "IsActive", "controlType": 13, "isVisible": true }
  ],
  "menu": {
    "registerInMainMenu": true,
    "menuTitle": "Customers",
    "menuOrder": 100
  }
}
```

- `organizedType`: `MasterDetail` (1, default) | `List` / `ListEdit` (3). Import creates hierarchy then sets `TransactionOrganizedType`.
- `transactions[].menu` only for **List** — opens `FormListEdit`. For MasterDetail list screens use a Search menu instead.

## Transactions

```json
{
  "integrationId": "TX_DemoOrder",
  "name": "Demo Order",
  "organizedType": "MasterDetail",
  "unitStructure": {
    "rootTableName": "Demo_Order",
    "rootDisplayName": "Order",
    "siblingTableNames": ["Demo_OrderHeader"],
    "siblingUnits": [
      { "tableName": "Demo_OrderHeader", "displayName": "Order Header" }
    ],
    "childUnits": [
      {
        "tableName": "Demo_OrderLine",
        "displayName": "Order Lines",
        "grandChildTableNames": ["Demo_OrderLineNote"],
        "gridDisplayType": 1,
        "linkTargets": [
          {
            "name": "Edit Line",
            "actionType": "Edit",
            "transactionIntegrationId": "TX_DemoOrderLine",
            "sourceColumn": "OrderLineId",
            "targetColumn": "OrderLineId"
          }
        ]
      }
    ]
  },
  "fields": [
    { "tableName": "Demo_Order", "columnName": "StatusId", "controlType": 1, "entityCode": "OrderStatus", "isVisible": true }
  ],
  "commands": [],
  "formMode": "Flex",
  "formLayout": { "defaultNbColumns": 4, "items": [] }
}
```

- Root = master table. Optional `rootDisplayName`. Siblings share the root PK (`ReferenceId` pattern when present).
- Optional `organizedType`: `MasterDetail` (default) or `List` / `ListEdit` (editable grid). See **Screen patterns** above.
- Optional `menu` on a **List** transaction: `registerInMainMenu`, `menuTitle`, `menuOrder` — same shape as search menu; adds a FormListEdit shortcut.
- `siblingUnits[]` overlays sibling display names. `siblingTableNames` still creates the sibling units.
- Child / grandchild = grids. Optional `gridDisplayType` (`1` RegularGrid, `5` AvailableSelectGridPair, `6` MultipleSelectBox, `7` ChildUnitPivotColumns).
- Optional `layoutTab` on a child unit: after default form create, rename that unit's form Tab to this title. Ignored when `formLayout` is present (tab titles live on tab nodes).
- Optional child flags: `isReadOnly`, `isSynchToDatabaseTable` (set `false` for VIEW units), `isDisableAddButton`, `isDisableDeleteButton` (hide the child grid Add / Delete row buttons; use Link Targets to open the child form instead).
- **Available Select pair:** on the *selected* child set `availableSourceTableName` (the pool unit in the same TX), `availableSelectSelectedColumn`, and optional `availableSelectSourceColumn` (defaults to the selected column). Import sets `AvailableSourceUnitId` + field mapping. The source unit is marked `IsUsedForLoadingAvailableSource`.
- **Child grid Link Targets:** `linkTargets[]` on a child unit — `actionType` `Create` / `Edit` / `Delete`, `transactionIntegrationId`, `sourceColumn`, optional `targetColumn` (defaults to `sourceColumn`). `isPopup` defaults to `true`; set `false` to open in a full tab. Applied after all transactions exist. `null` = leave existing links; `[]` = clear.
- `fields[]` overlay after create. `controlType` uses `EmAppControlType` (1=DDL, 2=TextBox, 7=Date, 9=File, 13=CheckBox, 20=Numeric). Optional `sortOrder` sets field sequence (and default form field order). Optional `nbDecimal` sets numeric decimal places (`AppTransactionField.NBDecimal`). Optional `displayWidth` sets `AppTransactionField.DisplayWidth` (grid column width).
- `entityCode` resolves `AppEntityInfo.EntityCode` (LOV). Unknown codes become warnings.
- **Query Datasource:** on a DDL field set `ddlQueryText` (first column = id, second = display; `@p0`… parameters) and `ddlQueryParameterColumns` as `["Table.Column", …]`. Import writes `DdlQueryText` + `WhereClauseExpress` (pipe-separated field ids) and **clears EntityId**. Omit `entityCode` when using a query.
- Pivot: `isPivotRow` / `isPivotColumn` / `isPivotValue`; optional `matrixSourceTable` + `matrixSourceColumn` to set `MatrixForeignKeyFieldId`.
- Cascading DDL: `dependsOnTable` + `dependsOnColumn` (resolves `DDLParentLevelID`); optional `cascadingRelationTable`, `cascadingRelationSchemaOwner`, `cascadingParentKey`, `cascadingChildKey`.
- VIEW units: set `isPrimaryKey` on the unique key column and `isLinkToParentPrimaryKey` on the parent key column (views have no SQL PK/FK). Import also **auto-wires** child/sibling links without a database FK: same column name as the parent PK, or the StyleSpecId ↔ ReferenceId product/spec alias. Pack `fields[]` flags still win when present.

Physical tables/views named in `unitStructure` must exist after the DDL step.

## Transaction header / extras

All of the following are **optional**. **Omit or null** → import keeps the current default (new TX from hierarchy create, existing TX unchanged). **`[]`** on a list → **clear** that collection. Export always writes the live values (empty arrays when none).

| Field | Notes |
|-------|--------|
| `isShowSaveButton` / `isShowPrintButton` / `isShowCalculateButton` | Form toolbar buttons (`AppTransaction`) |
| `isReadOnly` | Whole-transaction read-only |
| `dataLoads[]` | Data Load definitions + dataset SQL + field mappings. Bind unit with `tableName`; mappings use `tableName`/`columnName` + `dataSetColumn`. |
| `unitFormulas[]` | Unit formulas. `tableName` = host unit. Expression tokens are `[TF:Table.Column]` (never numeric FieldId). |
| `conditionalActions[]` | Conditional lock/hide. Field/unit refs by table + column name. Formula tokens `[TF:Table.Column]`. |
| `linkedSearches[]` | Unit linked-search + criteria/result mappings. `searchIntegrationId` required. `fieldMappings` / `viewFieldMappings` bind TX fields to Search / SearchView columns by name. |

Data Load / Linked Search run **after** Searches so `searchIntegrationId` can resolve. Hand-written packs may omit every extras key.

## Commands

Optional `commands[]` on a transaction. Matched on import by **Name** within that transaction (AppProjectWorkFlowAction has no IntegrationId column). `integrationId` is pack-local only, for composition children.

| Field | Notes |
|-------|--------|
| `integrationId` | Pack-local key, e.g. `CMD_ReloadPOMFromGrading` |
| `name` | Stored command name (match key) |
| `actionType` | `42` Execute SQL Statement, `49` Save, `50` Refresh, `200` Composition |
| `sqlStatement` | Required for 42. Use `[TF:Table.Column]` (never numeric FieldId). `[CurrentUserId]` is left as-is. SQL is stored in `NotificationMessage`. |
| `childCommandIntegrationIds` | Composition children, in order (e.g. Save → Execute SQL → Refresh) |
| `linkToUI` / `isShowOnTopMenu` | Written into `FormulaExpression` (ActionAttribute JSON) |
| `layoutHostTable` | After **default** form create, insert a CommandActionButton **above** this unit's grid if missing. Skipped when `formLayout` is present (put a `commandButton` node in the tree instead). |

Helpers that should not appear on the toolbar: `isShowOnTopMenu: false`, `linkToUI: false`. Put the user-facing composition on the form with `linkToUI: true` plus either `layoutHostTable` (default form) or a `formLayout` `commandButton`.

## Form layout

Optional `formLayout` on a transaction. When `items` is non-empty, Import **replaces** the Flex tree (delete + rebuild). Export always writes the current tree.

Do **not** put FormLayoutItemID / FieldId / UnitId / CommandId in the pack.

```json
{
  "formMode": "Flex",
  "formLayout": {
    "defaultNbColumns": 4,
    "items": [
      {
        "type": "row",
        "children": [
          {
            "type": "stack",
            "displayName": "Order",
            "colSpan": 24,
            "defaultNbColumns": 4,
            "children": [
              {
                "type": "row",
                "children": [
                  {
                    "type": "field",
                    "tableName": "Demo_Order",
                    "columnName": "StatusId",
                    "displayName": "Status",
                    "widgetDisplayType": 1,
                    "colSpan": 24
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        "type": "row",
        "children": [
          {
            "type": "commandButton",
            "commandName": "Reload POM From Grading",
            "colSpan": 4
          }
        ]
      },
      {
        "type": "row",
        "children": [
          {
            "type": "grid",
            "tableName": "Demo_OrderLine",
            "displayName": "Order Lines",
            "colSpan": 24,
            "height": 400,
            "transcationUnitLevel": 2
          }
        ]
      }
    ]
  }
}
```

| `type` | Widget | Bind with |
|--------|--------|-----------|
| `row` | 101 LayoutRow | — |
| `stack` | 102 Section | — |
| `tabContainer` | 107 | — |
| `tab` | 102 Section + `isTab: true` | — |
| `field` | control type (1/2/13/20/…) | `tableName` + `columnName` |
| `grid` | 6 | `tableName` |
| `commandButton` | 106 | `commandName` (must exist in `commands[]`) |
| `linkedSearch` | 109 | `searchIntegrationId` |
| `content` / `space` / `addButton` / `tableContainer` / `htmlContentContainer` | 103/105/104/110/111 | optional `htmlContent` |
| `widget` | uncommon `widgetDisplayType` | as needed |

Style overlay (all optional): `displayName`, `sort`, `colSpan`, `defaultNbColumns`, `height`, `backgroundColor`, `textColor`, `isCollapsible`, `isTab`, `emUnitLabelPosition`, `transcationUnitLevel`, `htmlContent`, `visibleExpression`.

Commands must be upserted **before** formLayout so `commandButton` can resolve `commandName`. If `formLayout` is omitted, Import still creates the default Flex form and applies `layoutTab` / `layoutHostTable`.

Query Datasource example (Size filtered to QC Order selected sizes):

```json
{
  "tableName": "TchpQcGarment",
  "columnName": "SizeRunSizeId",
  "displayName": "Size",
  "controlType": 1,
  "ddlQueryText": "SELECT srs.SizeRunSizeId, srs.SizeLabel FROM dbo.TchpQcOrderSize AS os INNER JOIN dbo.TchpSizeRunSize AS srs ON srs.SizeRunSizeId = os.SizeRunSizeId WHERE os.QcOrderId = @p0 ORDER BY srs.SizeOrder, srs.SizeLabel",
  "ddlQueryParameterColumns": ["TchpQcGarment.QcOrderId"]
}
```

Available Select example (source VIEW + selected table):

```json
{
  "tableName": "View_TchpQcOrderAvailableSize",
  "displayName": "Available QC Sizes",
  "isReadOnly": true,
  "isSynchToDatabaseTable": false
},
{
  "tableName": "TchpQcOrderSize",
  "displayName": "Selected QC Sizes",
  "gridDisplayType": 5,
  "availableSourceTableName": "View_TchpQcOrderAvailableSize",
  "availableSelectSelectedColumn": "SizeRunSizeId",
  "availableSelectSourceColumn": "SizeRunSizeId"
}
```

## Transaction group

Optional. Members listed by transaction `integrationId`. `primaryTransactionIntegrationId` is marked as group shared header.

## Searches

Each search:

- `integrationId` (required)
- `usageType`: `Management` (default) or `DataModelTemplate`
- `dataSet.queryText` (required) — SELECT used as the search dataset
- `searchView.fields` — at least one field; **exactly one** with `isTransRootId: true` (usually the root PK, e.g. `OrderId`)
- `criteriaFields[]` — optional filter panel
- `linkTargets[]` — `transactionIntegrationId` + `actionType` (`Create`/`Edit`/`Delete`)
- `menu.registerInMainMenu` — add Search to the Application main menu

`sysTableFiledPath` is the **result-set column name**, not a numeric id.

## How to generate this JSON (AI / user)

**In App Data Integration Agent (App Config Builder):** ask clarifying questions on the first reply and wait. Only write the pack after the user confirms. Then stop — the chat UI offers **Start Build** to import. Do not import from tools.

1. **Pick the screen pattern** (A Search+MasterDetail vs B ListEdit). If the user said List Edit / ListEdit / LIST EDIT TRANSACTION → **B only**.
2. List physical tables and views the app needs (columns + PK + FKs).
3. Assign a stable `integrationId` per transaction and search (`TX_...`, `ListEdit_...`, `Search_...`). Do not reuse ids across packs unless you intend Update.
4. For each transaction, set `organizedType` and describe Root / Sibling / Child tables that already exist or are included in `tables`/`views`.
5. Add field overlays only where the default TextBox is wrong (DDL, date, numeric, hidden, pivot).
6. **Pattern A only:** write one Search per list screen: SQL that returns a unique root id column, mark that column `isTransRootId`; point `linkTargets` at transaction `integrationId`s; put main menu on the Search.
7. **Pattern B only:** put main menu on the List transaction (`transactions[].menu`). Do **not** add a Search for the same screen.
8. Validate mentally: every `unitStructure` table appears in `tables` or `views` (or already exists in the tenant DB).

See [`sample.appConfigPack.json`](sample.appConfigPack.json) for Pattern A (Search + MasterDetail). See [`sample-listedit.appConfigPack.json`](sample-listedit.appConfigPack.json) for Pattern B (ListEdit).

## Out of scope (v1)

- Reports
- DROP / column type changes
- PLM TabId / FieldMapping / TechPack pivots
