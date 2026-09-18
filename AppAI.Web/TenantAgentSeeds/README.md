# Tenant Agent Seeds (NOT schema migrations)

These SQL scripts seed **tenant-specific** Agent SkillSets, Libraries, Tools, and subscriptions
(e.g. PLM Integration for one customer). They are **not** part of the numbered `Migrations/Vxxx__*.sql`
structure upgrade pipeline.

## Rules (pending product confirmation)

1. Do **not** auto-run these on every new tenant / Flyway-style migrate.
2. Apply manually only to tenants that need that product pack:
   ```bat
   sqlcmd -S <server> -d <TenantDB> -E -i Seed_IntegrationPlmImportLibrary.sql
   ```
3. Prefer configuring Library / Tool / Agent via **Agent Management UI** when possible;
   keep SQL here for repeatable demos and customer packs.

## Folders

| Path | Purpose |
|---|---|
| `PlmIntegration/` | PLM Integration library tools (optional pack) |
| `SampleExternalDll/` | Demo ExternalDll `sample_hello` (APP.AgentPlugins.Sample) |

Confirm folder naming / location with product owner before expanding.
