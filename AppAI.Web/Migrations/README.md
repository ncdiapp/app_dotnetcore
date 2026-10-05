# Tenant migrations

Scripts in this folder are applied to every **company-master** tenant database
(`AppDataSourceRegister.IsCompanyMasterDb = 1`) by `AppTenantMigrationRunnerBL`, in file-name order.
Satellite data sources (ERP, DataWS, PLM, …) are never migrated by this runner.
A script is recorded in `_SchemaMigrations` by its **full file name** (without `.sql`), so renaming an applied
script makes it run again — never rename a script that has been deployed.

## Naming

```
V{NNN}__{ShortDescription}[_{initials}].sql        e.g. V040__AgentToolCatalogIndex_xhu.sql
```

- `NNN` is the **next free number** (look at the highest existing one *after* pulling).
- Two scripts with the same number still run, but order between them is only alphabetical and the history is
  confusing. Existing duplicates are listed in `check-duplicate-versions.ps1`; do not add new ones.
- Scripts must be idempotent (`IF NOT EXISTS` / `COL_LENGTH` checks) and split batches with `GO`.

## When do they run?

| Situation | What happens |
|---|---|
| New tenant is provisioned | All scripts are applied |
| `POST /webapi/TenantProvisioning/RunMigrations` (SysAdmin) | Pending scripts applied to all company-master tenants |
| `Migrations:RunOnStartup = true` (on in `appsettings.Development.json`) | Applied at startup, in the background (company-master only) |
| Setting is off (production default) | Startup logs a warning per company-master tenant with pending scripts; `GET /webapi/TenantProvisioning/PendingMigrations` returns the counts |

Symptom of forgetting: API errors such as `Invalid column name 'X'`.

## Check before you push

```powershell
./check-duplicate-versions.ps1
```

Fails when a new script re-uses a number.
