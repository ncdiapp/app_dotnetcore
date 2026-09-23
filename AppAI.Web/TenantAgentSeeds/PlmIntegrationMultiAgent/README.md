# PLM Migration Multi-Agent — Tenant seed pack

**Folder:** `AppAI.Web/TenantAgentSeeds/PlmIntegrationMultiAgent/`

For a **new or restored** tenant DB (structure through **V031+**). Do not mix with `PlmIntegration/Seed_PlmIntegrationOrchestrator.sql`.

## Files (RUN_ALL order)

| # | File | Purpose |
|---|---|---|
| 1 | `01_Seed_IntegrationPlmImportLibrary.sql` | Library + ExternalDll tools (Gate-0, wizard progress, import tools) |
| 2 | `02_Seed_PlmIntegrationImportDw_Child.sql` | Child agent `plm-integration-import-dw` |
| 3 | `03_Seed_PlmIntegrationOrchestrator_Root.sql` | ROOT Wizard orchestrator + subscriptions (UPDATEs prompt on re-run; **MaxIterations=400**) |
| 4 | `99_Verify.sql` | Smoke checks |
| — | `CHILD_AGENT_CONTRACTS.md` | Draft SkillKeys + `call_agent` contracts (entity/folder/image/color/pom) |
| — | `RUN_ALL.bat` | Runs 01→02→03 → force MaxIterations=400 → 99 |

## Apply

```bat
cd AppAI.Web\TenantAgentSeeds\PlmIntegrationMultiAgent
RUN_ALL.bat YourServer\Instance YourTenantDb
```

Example: `RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34`

Prereqs: V022+ (`AppAgentSharedContext`); rebuild/copy `APP.AgentPlugins.PlmImport.dll` (plugin Ensures PLM job tables only when those tools run). Tenant AI key set.

## What you get

| SkillKey | Role |
|---|---|
| `plm-integration-orchestrator` | ROOT Interactive Wizard (`AllowAgentFirstTurn=1`) |
| `plm-integration-import-dw` | Deterministic DW child |

Gate-0: `list_tenant_saas_applications` + `list_tenant_data_sources` → `ask_user` selects (App + PLM/DW/ERP/ExDb).

**Progress:** in-chat `write_shared_context` (WorkflowId, this turn) + durable `update_plm_wizard_progress` on `AppAgentSharedContext` with ScopeId=`ChatSessionKey`. PLM job table is plugin-only, not a Flyway Vxxx.

**Future children:** see `CHILD_AGENT_CONTRACTS.md` (draft only; ROOT still runs entity/folder/image/color/pom tools locally until those SkillSets are seeded).
