# Agent replace PLM Data Import Wizard — progress

**Docs home:** `AppReact/ImportDoc/PlmAgentIntegration/` (not under `Document/`).

| Doc | Purpose |
|---|---|
| [Agent-Replace-Wizard-Progress.md](./Agent-Replace-Wizard-Progress.md) | Roadmap / status |
| [Interactive-E2E-Checklist.md](./Interactive-E2E-Checklist.md) | Deploy + Interactive smoke |
| [PlmImport-Image-ExternalDll-Slice.md](./PlmImport-Image-ExternalDll-Slice.md) | Image → ExternalDll slice |
| [PlmImport-Entity-ExternalDll-Slice.md](./PlmImport-Entity-ExternalDll-Slice.md) | Entity → ExternalDll slice |
| Platform ExternalDll | `Document/AgentDesign/Agent-ExternalDll-Tools.md` |
| Integration playbook | `Document/AgentDesign/Agent-Integration-Playbook.md` |

## Decisions

- Agent tools = ExternalDll in `APP.AgentPlugins.PlmImport` (may call shared APP.BL).
- **Controllers must not call the plugin / ExternalDll.** `PlmMigrationController` removed; AppAI.Web does not ProjectReference the plugin.
- Engine renamed: `PlmImportEngine` in namespace `APP.AgentPlugins.PlmImport` (no `APP.BL.DataMigration…`).
- **JSON → Transaction/Search/Menu** goes through shared **`AppConfigPackBL`** (`APP.BL/AppConfigPack/`). Dw/Search blueprints convert → `AppConfigPackDto` then Preview/Execute.

## Status

| Item | Status |
|---|---|
| Folder/Color/Pom/Dw/Search ExternalDll tools + orchestrator seed | Done |
| Hide Wizard UI | Done |
| Move engine out of APP.BL; delete BuiltIn plugins | Done |
| Remove Web→plugin + delete PlmMigrationController | Done |
| Rename `PlmMigrationBL` → `PlmImportEngine` | Done |
| Dw / Search Preview+Execute via AppConfigPackBL | Done (post-process remains) |
| `build_dw_app_config_pack` / `build_search_app_config_pack` tools | Done |
| Smoke: sample 3354 DW blueprint → AppConfigPack | Done |
| **Secure Connect: DataSourceRegisterId only (no connection strings)** | Done |
| **AppConfigPack BuiltIn tools + orchestrator skill** | Done |
| Color/Pom still use `CreateHierarchy…` (not yet AppConfigPack) | **Partial** — Color RGB TX+list + POM BodyPart TX+list via pack; folder-nav + POM Template hierarchy still custom |
| Sibling SearchView attach | **Done** (custom by design — no CreateHierarchy; DataSet patch + SearchView only) |
| MassUpdate Hierarchical ListEdit via AppConfigPack | **Done** (`MassUpdateListEditAppConfigPackBuilder`; SearchView attach remains in engine) |
| Dw FieldMapping/pivot fully in pack; remove post SQL | **Done** (pivots/links in pack; residual BOM staging field cleanup only) |

## Secure Connect

- Tools: `list_tenant_data_sources`, `test_plm_connection(dataSourceRegisterId)`, `save_plm_import_session` with `plmDataSourceRegisterId` / `plmDwDataSourceRegisterId` / `erpDataSourceRegisterId`.
- `discover_plm_data_sources` disabled. Session never returns connection strings.
- Admins register PLM/DW/ERP in tenant Data Source Register UI before Agent Connect.

## App Config Pack BuiltIn (platform)

| Piece | Location |
|---|---|
| Plugin | `APP.BL/AIAgent/GenericAgent/Plugins/AppConfigPackPlugin.cs` |
| Tools | `get_app_config_pack_contract`, `validate_app_config_pack`, `preview_app_config_pack`, `execute_app_config_pack` |
| Seeds | `AppAI.Web/TenantAgentSeeds/AppConfigPack/` |
| Skill | `app-config-pack-orchestrator` |
| Contract file | `AppReact/ImportDoc/ImportAppConfig/PROMPT.md` (+ copy under `AppAI.Web/wwwroot/ImportDoc/...` for host) |

## Shared App Config (reuse)

| Piece | Location |
|---|---|
| BL | `APP.BL/AppConfigPack/AppConfigPackBL*.cs` |
| Public steps | `AppConfigPackBL.Steps.cs` — `BeginSteps` / `StepApplyDdl` … `RunAllSteps`; `Execute` uses the same sequence |
| Step context | `AppConfigPackStepContext` in `AppConfigPackDtos.cs` |
| API | `/webapi/AppConfigPack/` |
| UI | My Application Editor → Import Config |
| Contract | `AppReact/ImportDoc/ImportAppConfig/PROMPT.md` |

**Boundary:** Ex DLL owns PLM read + blueprint → pack compose. AppConfigPackBL owns App Config write (no PLM types). Default path: **Compose → Validate → Execute**. Partial steps only when pack cannot express something yet (goal: post-process → 0).

PLM converters: `DwBlueprintAppConfigPackBuilder`, `SearchImportAppConfigPackBuilder`, `ColorImportAppConfigPackBuilder`, `PomImportAppConfigPackBuilder`, `MassUpdateListEditAppConfigPackBuilder`.

| Follow-up | Status |
|---|---|
| AppConfigPackBL public step API | Done |
| Secure Connect (DataSourceRegisterId only) | Done |
| AppConfigPack BuiltIn + orchestrator | Done |
| Agent-Integration-Playbook.md | Done |
| Deepen Dw FieldMapping / pivot into pack; remove Dw post SQL | Done (staging cleanup residual) |
| Color / Pom BodyPart via AppConfigPack | Done |
| MassUpdate ListEdit via AppConfigPack | Done |
| Sibling (SearchView attach only — no pack TX) | Done (documented) |
| POM Template hierarchy / folder-nav residual | Open |

## Tenant seeds

1. `Seed_IntegrationPlmImportLibrary.sql`
2. `Seed_PlmIntegrationOrchestrator.sql`

**Deploy + Interactive E2E:** [Interactive-E2E-Checklist.md](./Interactive-E2E-Checklist.md)

| Deploy item | Status |
|---|---|
| Post-build copy DLL → `AppAI.Web/AgentPlugins/` | Done (csproj target) |
| Tenant seeds Plm + AppConfigPack | Done (manual apply) |
| Interactive E2E checklist doc | Done |
| Run checklist on customer tenant | Pending (operator) |
| Delete Wizard UI + plmMigrationSvc | Blocked until E2E green |
