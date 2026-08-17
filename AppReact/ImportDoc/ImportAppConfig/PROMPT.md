# Import App Config Pack — JSON contract (v1)

Use this file to **write or generate** a portable App Config Pack JSON, then import it from **My Application Editor → Import Config**.

The same schema is used for:

- **Hand-written / AI-generated** specs (`source.generatedBy`: `manual` or `ai`)
- **Export** from an existing Application (`source.generatedBy`: `export`)

Runtime APIs live under `/webapi/AppConfigPack/` (not PLM Migration). No FieldMapping, no PLM connection.

## Pipeline

1. **DDL** — create missing tables; **ADD** missing columns only; `CREATE OR ALTER VIEW`
2. Refresh tenant schema cache
3. **Transactions** — upsert by `integrationId` via table hierarchy (Root / Sibling / Child / Grandchild)
4. Overlay field metadata (control type, entity/LOV, visibility, pivot flags, cascading DDL)
5. Overlay unit display names, grid display type, Available Select pairing (`AvailableSourceUnitId` + field mapping)
6. Default Form layout (v1 does **not** round-trip Flex layout)
7. Child-grid **Link Targets** (Create/Edit/Delete) — after all transaction ids exist
8. Transaction Group (Data Model Template)
9. **Searches** — DataSet SQL, criteria, SearchView fields, linkTargets, optional main menu
10. Attach TX + Search as Application assets

## Matching / safety

| Rule | Behavior |
|------|----------|
| Stable key | `integrationId` (required on each transaction and search) |
| Exists | **Update** name + field overlay + form-if-missing. Does not rebuild unit tree. |
| Missing | **Insert** |
| DROP | Never drop tables or columns |
| Type change | Warning only; existing column types are not altered |
| Views | Always `CREATE OR ALTER VIEW` |
| IDs | Never put numeric TransactionId / SearchId / FieldId in the pack. Use names + integrationId |

## Top-level shape

```json
{
  "schemaVersion": 1,
  "generatedAt": "2026-08-17T00:00:00Z",
  "source": { "generatedBy": "manual", "applicationName": "Demo", "notes": "" },
  "tables": [ ],
  "views": [ ],
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

## Transactions

```json
{
  "integrationId": "TX_DemoOrder",
  "name": "Demo Order",
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
  "formMode": "Default"
}
```

- Root = master table. Optional `rootDisplayName`. Siblings share the root PK (`ReferenceId` pattern when present).
- `siblingUnits[]` overlays sibling display names. `siblingTableNames` still creates the sibling units.
- Child / grandchild = grids. Optional `gridDisplayType` (`1` RegularGrid, `5` AvailableSelectGridPair, `7` ChildUnitPivotColumns).
- Optional child flags: `isReadOnly`, `isSynchToDatabaseTable` (set `false` for VIEW units).
- **Available Select pair:** on the *selected* child set `availableSourceTableName` (the pool unit in the same TX), `availableSelectSelectedColumn`, and optional `availableSelectSourceColumn` (defaults to the selected column). Import sets `AvailableSourceUnitId` + field mapping. The source unit is marked `IsUsedForLoadingAvailableSource`.
- **Child grid Link Targets:** `linkTargets[]` on a child unit — `actionType` `Create` / `Edit` / `Delete`, `transactionIntegrationId`, `sourceColumn`, optional `targetColumn` (defaults to `sourceColumn`). Applied after all transactions exist. `null` = leave existing links; `[]` = clear.
- `fields[]` overlay after create. `controlType` uses `EmAppControlType` (1=DDL, 2=TextBox, 7=Date, 13=CheckBox, 20=Numeric).
- `entityCode` resolves `AppEntityInfo.EntityCode` (LOV). Unknown codes become warnings.
- Pivot: `isPivotRow` / `isPivotColumn` / `isPivotValue`; optional `matrixSourceTable` + `matrixSourceColumn` to set `MatrixForeignKeyFieldId`.
- Cascading DDL: `dependsOnTable` + `dependsOnColumn` (resolves `DDLParentLevelID`); optional `cascadingRelationTable`, `cascadingRelationSchemaOwner`, `cascadingParentKey`, `cascadingChildKey`.
- VIEW units: set `isPrimaryKey` on the unique key column and `isLinkToParentPrimaryKey` on the parent FK column (views have no SQL PK/FK). Put PK overlays before parent-link overlays in `fields[]`.

Physical tables/views named in `unitStructure` must exist after the DDL step.

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

1. List physical tables and views the app needs (columns + PK + FKs).
2. Assign a stable `integrationId` per transaction and search (`TX_...`, `Search_...`). Do not reuse ids across packs unless you intend Update.
3. For each transaction, describe Root / Sibling / Child tables that already exist or are included in `tables`/`views`.
4. Add field overlays only where the default TextBox is wrong (DDL, date, numeric, hidden, pivot).
5. Write one Search per list screen: SQL that returns a unique root id column, mark that column `isTransRootId`.
6. Point `linkTargets` at transaction `integrationId`s in the same file (or already in the tenant).
7. Validate mentally: every `unitStructure` table appears in `tables` or `views` (or already exists in the tenant DB).

See [`sample.appConfigPack.json`](sample.appConfigPack.json) for a minimal runnable pack.

## Out of scope (v1)

- Reports
- Full Flex Form layout round-trip
- DROP / column type changes
- PLM TabId / FieldMapping / TechPack pivots
