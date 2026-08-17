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
4. Overlay field metadata (control type, entity/LOV, **query datasource**, visibility, pivot flags, cascading DDL, PK / parent-link)
5. Overlay unit display names, grid display type, Available Select pairing (`AvailableSourceUnitId` + field mapping)
6. Auto-wire child/sibling **Link To Parent PK** (DB FK, same column name, or StyleSpecId↔ReferenceId). VIEW units without a SQL PK get a logical PK.
7. Default Form layout (v1 does **not** round-trip Flex layout)
8. **Commands** — upsert by `name` on the transaction (Execute SQL / Refresh / Composition); rewrite `[TF:Table.Column]` tokens; optional CommandActionButton above a child grid
9. Child-grid **Link Targets** (Create/Edit/Delete) — after all transaction ids exist
10. Transaction Group (Data Model Template)
11. **Searches** — DataSet SQL, criteria, SearchView fields, linkTargets, optional main menu
12. Attach TX + Search as Application assets

## Matching / safety

| Rule | Behavior |
|------|----------|
| Stable key | `integrationId` (required on each transaction and search) |
| Exists | **Update** name + field overlay (incl. query datasource) + commands + form-if-missing. Does not rebuild unit tree. |
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
  "commands": [],
  "formMode": "Default"
}
```

- Root = master table. Optional `rootDisplayName`. Siblings share the root PK (`ReferenceId` pattern when present).
- `siblingUnits[]` overlays sibling display names. `siblingTableNames` still creates the sibling units.
- Child / grandchild = grids. Optional `gridDisplayType` (`1` RegularGrid, `5` AvailableSelectGridPair, `6` MultipleSelectBox, `7` ChildUnitPivotColumns).
- Optional `layoutTab` on a child unit: after default form create, rename that unit's form Tab to this title (e.g. `"QC Results Preview"`). Full Flex layout (moving a grid out of the tab container) is not round-tripped.
- Optional child flags: `isReadOnly`, `isSynchToDatabaseTable` (set `false` for VIEW units).
- **Available Select pair:** on the *selected* child set `availableSourceTableName` (the pool unit in the same TX), `availableSelectSelectedColumn`, and optional `availableSelectSourceColumn` (defaults to the selected column). Import sets `AvailableSourceUnitId` + field mapping. The source unit is marked `IsUsedForLoadingAvailableSource`.
- **Child grid Link Targets:** `linkTargets[]` on a child unit — `actionType` `Create` / `Edit` / `Delete`, `transactionIntegrationId`, `sourceColumn`, optional `targetColumn` (defaults to `sourceColumn`). Applied after all transactions exist. `null` = leave existing links; `[]` = clear.
- `fields[]` overlay after create. `controlType` uses `EmAppControlType` (1=DDL, 2=TextBox, 7=Date, 13=CheckBox, 20=Numeric). Optional `sortOrder` sets field sequence (and default form field order).
- `entityCode` resolves `AppEntityInfo.EntityCode` (LOV). Unknown codes become warnings.
- **Query Datasource:** on a DDL field set `ddlQueryText` (first column = id, second = display; `@p0`… parameters) and `ddlQueryParameterColumns` as `["Table.Column", …]`. Import writes `DdlQueryText` + `WhereClauseExpress` (pipe-separated field ids) and **clears EntityId**. Omit `entityCode` when using a query.
- Pivot: `isPivotRow` / `isPivotColumn` / `isPivotValue`; optional `matrixSourceTable` + `matrixSourceColumn` to set `MatrixForeignKeyFieldId`.
- Cascading DDL: `dependsOnTable` + `dependsOnColumn` (resolves `DDLParentLevelID`); optional `cascadingRelationTable`, `cascadingRelationSchemaOwner`, `cascadingParentKey`, `cascadingChildKey`.
- VIEW units: set `isPrimaryKey` on the unique key column and `isLinkToParentPrimaryKey` on the parent key column (views have no SQL PK/FK). Import also **auto-wires** child/sibling links without a database FK: same column name as the parent PK, or the StyleSpecId ↔ ReferenceId product/spec alias. Pack `fields[]` flags still win when present.

Physical tables/views named in `unitStructure` must exist after the DDL step.

## Commands

Optional `commands[]` on a transaction. Matched on import by **Name** within that transaction (AppProjectWorkFlowAction has no IntegrationId column). `integrationId` is pack-local only, for composition children.

| Field | Notes |
|-------|--------|
| `integrationId` | Pack-local key, e.g. `CMD_ReloadPOMFromGrading` |
| `name` | Stored command name (match key) |
| `actionType` | `42` Execute SQL Statement, `50` Refresh, `200` Composition |
| `sqlStatement` | Required for 42. Use `[TF:Table.Column]` (never numeric FieldId). `[CurrentUserId]` is left as-is. SQL is stored in `NotificationMessage`. |
| `childCommandIntegrationIds` | Composition children, in order |
| `linkToUI` / `isShowOnTopMenu` | Written into `FormulaExpression` (ActionAttribute JSON) |
| `layoutHostTable` | After default form create, insert a CommandActionButton **above** this unit's grid if missing |

Helpers that should not appear on the toolbar: `isShowOnTopMenu: false`, `linkToUI: false`. Put the user-facing composition on the form with `linkToUI: true` + `layoutHostTable`.

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
- Full Flex Form layout round-trip (e.g. moving Selected QC Sizes out of the tab container)
- DROP / column type changes
- PLM TabId / FieldMapping / TechPack pivots
