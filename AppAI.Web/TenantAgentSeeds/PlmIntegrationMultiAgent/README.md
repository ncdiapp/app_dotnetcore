# PLM Migration Multi-Agent — Tenant seed pack

**Folder:** `AppAI.Web/TenantAgentSeeds/PlmIntegrationMultiAgent/`

For a **new or restored** tenant DB (structure through **V031+**). Do not mix with `PlmIntegration/Seed_PlmIntegrationOrchestrator.sql`.

## Files (RUN_ALL order)

| # | File | Purpose |
|---|---|---|
| 1 | `01_Seed_IntegrationPlmImportLibrary.sql` | Library + ExternalDll tools (incl. `list_tenant_data_sources`, `list_tenant_saas_applications`) |
| 2 | `02_Seed_PlmIntegrationImportDw_Child.sql` | Child agent `plm-integration-import-dw` |
| 3 | `03_Seed_PlmIntegrationOrchestrator_Root.sql` | ROOT Wizard orchestrator + subscriptions (UPDATEs prompt on re-run) |
| 4 | `99_Verify.sql` | Smoke checks |
| — | `RUN_ALL.bat` | Runs 01→02→03→99 |

## Apply

```bat
cd AppAI.Web\TenantAgentSeeds\PlmIntegrationMultiAgent
RUN_ALL.bat YourServer\Instance YourTenantDb
```

Example: `RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34`

Prereqs: V031+ migrated; `APP.AgentPlugins.PlmImport.dll` in `AppAI.Web/AgentPlugins/`; tenant AI key set.

## What you get

| SkillKey | Role |
|---|---|
| `plm-integration-orchestrator` | ROOT Interactive Wizard (`AllowAgentFirstTurn=1`) |
| `plm-integration-import-dw` | Deterministic DW child |

Gate-0: `list_tenant_saas_applications` + `list_tenant_data_sources` → `ask_user` selects (App + PLM/DW/ERP/ExDb). Progress: `plm.integration.wizard`.
