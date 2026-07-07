# Create Transaction Stored Procedure — Agent Prompt

> **Folder:** `AppReact/ImportDoc/CreateTransactionStoredProcedure/`  
> **Generator:** `_gen_SP_FromTransaction.ps1`  
> **Output:** `OUTPUT/SP_{TransactionName}.sql` (deploy to tenant DB)  
> **Purpose:** Build a **Report Engine** stored procedure for one APP Transaction. DDL fields return **display text**, not raw IDs.

---

## User input (required)

The user must supply **at least one** item:

```text
TransactionId — one integer, e.g. 2256
```

Optional (defaults if omitted):

| Parameter | Default | Notes |
|-----------|---------|-------|
| Tenant DB | `TenantDB_PLM26` | Target tenant database |
| ERP DB | `SourceERP` | Resolves `AppEntityInfo.DataSourceFrom = 1071` |
| Tenant DataSource id | `1070` | Same-tenant entity tables |
| ERP DataSource id | `1071` | Cross-database entity lookups |
| SQL Server | `PC3B\MSSQLSERVER01` | |
| Deploy | `false` | Pass `-Deploy` on generator to run `sqlcmd` |

### Gate 0 — missing input → ask user, do nothing else

If the user only references this file and does **not** provide a **TransactionId**:

1. **STOP.** Do not connect to SQL or generate files.
2. Ask for **TransactionId** and (optionally) tenant DB name / ERP DB name.
3. Do not guess TransactionId from prior chats.

---

## Hard rules

| Rule | Detail |
|------|--------|
| **Report Engine contract** | SP must match `AppReportTemplateService.FetchData` — see **Report Engine SP contract** below. |
| **Report use** | SP is for `AppReportTemplate.DataSpName`. DDL must return **display text**, not item IDs. |
| **No visible-field filter** | Include **all** fields on level-1 units. Do not filter by `IsVisible`. |
| **Level-1 units only** | Join only root units (`ParentTransactionUnitID IS NULL`). |
| **Token-safe column aliases** | Column aliases must be `[A-Za-z0-9_]` only — no spaces, `/`, `#`. Maps to `{{header.AliasName}}`. |
| **Output location** | Write SQL to `OUTPUT/SP_{CleanTransactionName}.sql`. |
| **Prefer generator** | Run `_gen_SP_FromTransaction.ps1` when possible. |

---

## Report Engine SP contract (required)

The Report Engine (`APP.BL/TenantBusiness/AppReportTemplateService.cs`) **always** calls SPs with:

```sql
CREATE PROCEDURE dbo.SP_{Name}
    @MainReferenceId    INT,
    @MasterReferenceId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: header (exactly 1 row) → tokens {{header.FieldName}}
    SELECT ...
    WHERE ... = @MainReferenceId;

    -- Result set 2+ (optional): detail rows → {{#each rs1}}...{{/each}}
    -- SELECT ... WHERE ... = @MainReferenceId;
END
```

| Requirement | Detail |
|-------------|--------|
| **Parameters** | `@MainReferenceId INT` (required), `@MasterReferenceId INT = NULL` (optional). **Do NOT use `@ReferenceId`.** |
| **Filter** | `WHERE {PK} = @MainReferenceId` (usually `ReferenceId`) |
| **RS0** | First `SELECT` → mapped to `header`. Must return **exactly 1 row** for scalar tokens `{{header.*}}`. |
| **RS1+** | Additional `SELECT`s → `rs1`, `rs2`, … for `{{#each rs1}}` blocks (optional). |
| **Column aliases** | Token-safe: `ProductClass`, `StyleNumber` — **not** `Product Class`, `Style #`. Regex: `\{\{([\w.]+)\}\}`. |
| **Errors** | SP failures are **silently swallowed** — wrong params = blank preview with no error message. |

**Not passed by engine:** `@ExtraParams` (UI hint only; engine passes individual params from `ExtraParamConfig.extraParams` when configured).

---

## What the SP must do

For Transaction `{TransactionId}`:

1. **Read metadata** from tenant DB:
   - `AppTransaction`, `AppTransactionUnit`, `AppTransactionField`, `AppEntityInfo`
   - `AppTransactionUnitJoin` — optional; often empty (infer PK join on `ReferenceId`)

2. **JOIN level-1 unit tables** on shared primary key (usually `ReferenceId`):
   ```sql
   FROM dbo.{Unit1Table} AS u1
   INNER JOIN dbo.{Unit2Table} AS u2 ON u2.ReferenceId = u1.ReferenceId
   WHERE u1.ReferenceId = @MainReferenceId
   ```

3. **SELECT columns** — alias with **token-safe names** derived from `DisplayName`:
   - `Product Class` → `ProductClass`
   - `Style #` → `Style`
   - `Dimension/Inseam` → `DimensionInseam`
   - Dedupe with unit prefix if needed: `PlmStyleHeader_ReferenceId`

4. **Resolve DDL display text** (`ControlType = 1`):

   | `EntityType` | JOIN | Display |
   |--------------|------|---------|
   | **1** SystemDefineTable | `LEFT JOIN [{ErpDb\|TenantDb}].dbo.[Table] ON [IdentityField] = field` | `DisplayFiled1` |
   | **4** SimpleValueList | `LEFT JOIN AppEntitySimpleListValue ON EntityInfoID = ? AND InternalKey = field` | **`Code`** |
   | **2** Enum | `AppEntityEnumValue` | `Description` |
   | **3** SimpleQuery | per `QueryText` | per query |

   **DataSource routing:** `1070` → tenant DB, `1071` → ERP DB (often `SourceERP` on dev).

5. **Other control types:**
   - TextBox/Memo/Numeric — raw value
   - CheckBox (`13`) — `Yes` / `No`
   - Image/File — ID or path only

6. **Procedure name:** `SP_{TransactionName}` (alphanumeric only)  
   Example: `Style Header` (2256) → `SP_StyleHeader`

---

## Step-by-step workflow (agent)

### Step 1 — Validate Transaction

```sql
SELECT TransactionID, TransactionName, Description FROM AppTransaction WHERE TransactionID = @TransactionId;
```

### Step 2 — List level-1 units + PK

```sql
SELECT tu.TransactionUnitID, tu.UnitDisplayName, tu.DataBaseTableName, pk.DataBaseFieldName AS PKField
FROM AppTransactionUnit tu
OUTER APPLY (
    SELECT TOP 1 DataBaseFieldName FROM AppTransactionField tf
    WHERE tf.TransactionUnitID = tu.TransactionUnitID AND tf.IsPrimaryKey = 1
    ORDER BY tf.SortOrder
) pk
WHERE tu.TransactionID = @TransactionId AND tu.ParentTransactionUnitID IS NULL
ORDER BY tu.TransactionUnitID;
```

### Step 3 — List all fields

```sql
SELECT tu.UnitDisplayName, tf.DisplayName, tf.DataBaseFieldName, tf.ControlType, tf.EntityID,
       ei.EntityType, ei.TableName, ei.IdentityField, ei.DisplayFiled1, ei.DataSourceFrom
FROM AppTransactionField tf
JOIN AppTransactionUnit tu ON tu.TransactionUnitID = tf.TransactionUnitID
LEFT JOIN AppEntityInfo ei ON ei.EntityInfoID = tf.EntityID
WHERE tu.TransactionID = @TransactionId AND tu.ParentTransactionUnitID IS NULL
ORDER BY tu.TransactionUnitID, tf.SortOrder;
```

### Step 4 — Resolve ERP DB name

```sql
SELECT name FROM sys.databases WHERE name IN ('SourceERP', 'TenantDB_PLM26_ERP');
```

### Step 5 — Generate SQL

```powershell
cd AppReact/ImportDoc/CreateTransactionStoredProcedure
.\ _gen_SP_FromTransaction.ps1 -TransactionId 2256 -TenantDb TenantDB_PLM26 -ErpDb SourceERP
.\ _gen_SP_FromTransaction.ps1 -TransactionId 2256 -Deploy
```

### Step 6 — Smoke test (Report Engine style)

```sql
DECLARE @id INT = 31614;
EXEC dbo.SP_StyleHeader @MainReferenceId = @id, @MasterReferenceId = NULL;
```

Verify:
- Returns 1 header row
- DDL columns show text (`Development`, not `4204`)
- Column names are token-safe (`ProductClass`, not `Product Class`)

Template tokens: `{{header.ReferenceId}}`, `{{header.ProductClass}}`, `{{header.StyleNumber}}`

---

## Example session message

```text
@AppReact/ImportDoc/CreateTransactionStoredProcedure/Prompt.md

TransactionId: 2256
Tenant DB: TenantDB_PLM26
ERP DB: SourceERP
```

---

## Reference: Transaction 2256 (Style Header)

| Item | Value |
|------|-------|
| Transaction | 2256 — Style Header |
| Level-1 units | `Plm_ReferenceBasicInfo`, `Plm_Style_Header` |
| Join key | `ReferenceId` |
| SP name | `SP_StyleHeader` |
| Test | `EXEC SP_StyleHeader @MainReferenceId = 31614` |

Example tokens:

| Token | Sample value (Ref 31614) |
|-------|--------------------------|
| `{{header.ReferenceCode}}` | TAS27764BT |
| `{{header.ProductClass}}` | Pjk Tech Apparel |
| `{{header.ProductType}}` | Knit Ss |
| `{{header.Season_3}}` | 2027 Spring |
| `{{header.Style_Status}}` | Development |
| `{{header.Gender_7171}}` | Mens |

---

## Known limitations

1. **Child units excluded** — only level-1 unit tables.
2. **Cross-database** — ERP lookups need `SourceERP` (or configured ERP DB) on same SQL instance.
3. **Single result set** — generator produces RS0 only; add RS1 manually for `{{#each rs1}}` detail sections.
4. **SimpleValueList** — join on `InternalKey`, display `Code`.
5. **Regenerate** when Transaction form fields change.

---

## Deliverables checklist

- [ ] `OUTPUT/SP_{TransactionName}.sql` created
- [ ] Parameters: `@MainReferenceId INT`, `@MasterReferenceId INT = NULL`
- [ ] `WHERE` uses `@MainReferenceId`
- [ ] Column aliases are token-safe (`[A-Za-z0-9_]` only)
- [ ] All level-1 fields included; DDL returns display text
- [ ] Smoke test with `@MainReferenceId` documented
