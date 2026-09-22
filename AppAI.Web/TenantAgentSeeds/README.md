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
| **`PlmIntegrationMultiAgent/`** | **Preferred for new tenants** — Migration Multi-Agent pack (ROOT + DW CHILD + PLM import library + wizard progress tools). Use `RUN_ALL.bat`. See `CHILD_AGENT_CONTRACTS.md` for draft child SkillKeys. |
| `PlmIntegration/` | PLM Integration library only (+ alternate single-agent orchestrator seed) |
| `AppConfigPack/` | Platform App Config Pack BuiltIn tools + orchestrator |
| `SampleExternalDll/` | Demo ExternalDll `sample_hello` (APP.AgentPlugins.Sample) |

**New tenant Multi-Agent:** `TenantAgentSeeds/PlmIntegrationMultiAgent/README.md`  
**PLM Interactive E2E:** `AppReact/ImportDoc/PlmAgentIntegration/Interactive-E2E-Checklist.md`

Confirm folder naming / location with product owner before expanding.
