# PLM Migration Multi-Agent — Tenant seed pack

**Folder:** `AppAI.Web/TenantAgentSeeds/PlmIntegrationMultiAgent/`

For a **new** tenant DB (structure through **V034+**). Do not mix with `PlmIntegration/Seed_PlmIntegrationOrchestrator.sql`.

## Rules (keep forever)

1. **INSERT only.** These scripts create the pack on empty tables. Do **not** add `UPDATE` / `DELETE` for existing tenants. When a prompt or tool changes, edit the `INSERT` values.
2. **Only ROOT is on the left menu.** Child agents seed with `IsActive=0`. ROOT (`plm-integration-orchestrator`) is `IsActive=1`. `call_agent` loads children by SkillKey and does not require Active.

## Files (RUN_ALL order)

| # | File | Purpose |
|---|---|---|
| 1 | `01_Seed_IntegrationPlmImportLibrary.sql` | Library + ExternalDll tools |
| 2 | `02_Seed_PlmIntegrationImportDw_Child.sql` | Child `plm-integration-import-dw` (inactive) |
| 3 | `03_Seed_PlmIntegrationOrchestrator_Root.sql` | ROOT Wizard (`IsActive=1`, MaxIterations=400) |
| 4 | `04_Seed_PlmIntegrationEntity_Child.sql` | Child `plm-integration-entity` (inactive) |
| 5 | `05_Seed_PlmIntegrationFolder_Child.sql` | Child `plm-integration-folder` (inactive) |
| 6 | `06_Seed_PlmIntegrationImage_Child.sql` | Child `plm-integration-image` (inactive) |
| 7 | `07_Seed_PlmIntegrationColor_Child.sql` | Child `plm-integration-color` (inactive) |
| 8 | `08_Seed_PlmIntegrationPom_Child.sql` | Child `plm-integration-pom` (inactive) |
| 9 | `09_Seed_PlmIntegrationSearch_Child.sql` | Child `plm-integration-search` (inactive) |
| 10 | `10_Seed_PlmIntegrationMassUpdate_Child.sql` | Child `plm-integration-massupdate` (inactive) |
| 11 | `99_Verify.sql` | Smoke checks |
| — | `CHILD_AGENT_CONTRACTS.md` | SkillKeys + `call_agent` contracts |
| — | `RUN_ALL.bat` | Runs 01→10 → 99 |

## Apply

```bat
cd AppAI.Web\TenantAgentSeeds\PlmIntegrationMultiAgent
RUN_ALL.bat YourServer\Instance YourTenantDb
```

Example: `RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34`

Prereqs: V022+ (`AppAgentSharedContext`); rebuild/copy `APP.AgentPlugins.PlmImport.dll` (plugin Ensures PLM job tables only when those tools run). Tenant AI key set.

## What you get

| SkillKey | Role | Left menu |
|---|---|---|
| `plm-integration-orchestrator` | ROOT Interactive Wizard (`AllowAgentFirstTurn=1`) | Yes (`IsActive=1`) |
| `plm-integration-import-dw` | Deterministic DW child (Phase A / B / APPLY) | No |
| `plm-integration-entity` | Deterministic Entity child | No |
| `plm-integration-folder` | Deterministic Folder + placement child | No |
| `plm-integration-image` | Deterministic Image / Sketch child | No |
| `plm-integration-color` | Deterministic Color child | No |
| `plm-integration-pom` | Deterministic POM child | No |
| `plm-integration-search` | Deterministic Search + additional View (A / B / APPLY) | No |
| `plm-integration-massupdate` | Deterministic Mass Update View (A / B / APPLY) | No |

Gate-0: `list_tenant_saas_applications` + `list_tenant_data_sources` → `ask_user` selects (App + PLM/DW/ERP/ExDb).

**Progress:** in-chat `write_shared_context` (WorkflowId, this turn) + durable `update_plm_wizard_progress` on `AppAgentSharedContext` with ScopeId=`ChatSessionKey`. PLM job table is plugin-only, not a Flyway Vxxx.

**Search / MassUpdate (Wave 2):** `call_agent` children. Additional Search View is the same Search menu (`mode=additional-view`); no Sibling menu item. APPLY uses `apply_agent_output_plan` with `outputsContextKey` = `plm.integration.search.outputs` or `plm.integration.massupdate.outputs`. Official probe/example files are auto-seeded from `AppReact/ImportDoc/ImportPLMSearchView/MultiAgent/source/` into chat `source/` — users do not upload them.

If a child SkillKey is missing, ROOT must **stop** (Retry / Back). No local preview/execute fallback for entity/folder/image/color/pom/import-dw/search/massupdate.
