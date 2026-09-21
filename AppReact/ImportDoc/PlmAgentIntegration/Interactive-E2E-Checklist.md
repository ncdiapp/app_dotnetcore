# PLM Integration Agent — deploy & Interactive E2E checklist

Use this after building `APP.AgentPlugins.PlmImport` and before deleting the hidden Wizard UI (`p3-cleanup`).

Related: [Agent-Replace-Wizard-Progress.md](./Agent-Replace-Wizard-Progress.md), [Agent-Integration-Playbook.md](../../../Document/AgentDesign/Agent-Integration-Playbook.md).

---

## 1. Deploy (dev machine)

```bat
dotnet build APP.AgentPlugins.PlmImport\APP.AgentPlugins.PlmImport.csproj
```

Post-build copies the DLL to:

- `AppAI.Web\AgentPlugins\APP.AgentPlugins.PlmImport.dll`
- `AppAI.Web\bin\<Config>\net10.0\AgentPlugins\`

Restart `AppAI.Web` so ExternalDll tools reload.

Optional App Config Pack BuiltIn (already in APP.BL — no Ex DLL): rebuild Web if `AppConfigPackPlugin` / seeds changed.

---

## 2. Tenant seeds (manual — not Flyway)

On the target tenant DB (e.g. `TenantDB_PLM32`):

```bat
sqlcmd -S <server> -d <TenantDB> -E -i AppAI.Web\TenantAgentSeeds\PlmIntegration\Seed_IntegrationPlmImportLibrary.sql
sqlcmd -S <server> -d <TenantDB> -E -i AppAI.Web\TenantAgentSeeds\PlmIntegration\Seed_PlmIntegrationOrchestrator.sql
sqlcmd -S <server> -d <TenantDB> -E -i AppAI.Web\TenantAgentSeeds\AppConfigPack\Seed_PlatformAppConfigPackLibrary.sql
sqlcmd -S <server> -d <TenantDB> -E -i AppAI.Web\TenantAgentSeeds\AppConfigPack\Seed_AppConfigPackOrchestrator.sql
```

Or configure the same Library / Skill / subscriptions in **Agent Management UI**.

Prerequisites:

- [ ] PLM / PLMDW / ERP rows already in tenant **Data Source Register** (no connection strings in Agent)
- [ ] Known `saasApplicationId` for the PLM SaaS app
- [ ] User can open Interactive Agent skill `plm-integration-orchestrator`

---

## 3. Interactive smoke (happy path)

Open skill **PLM Integration Orchestrator**. On `[session_start]` you should get an `ask_user` main menu (not a long essay).

| # | Menu / flow | Expect |
|---|---|---|
| 1 | **Connect** | `list_tenant_data_sources` → pick register ids → `test_plm_connection(dataSourceRegisterId)` → `save_plm_import_session` — session returns **ids only**, never connection strings |
| 2 | **Entity** (optional) | preview → confirm → execute (or job poll) |
| 3 | **Image / Sketch** (optional) | preview counts → execute job → `get_plm_import_job` |
| 4 | **Folder** (optional) | preview → execute |
| 5 | **Color** | preview → execute; RGB TX + list Search via **AppConfigPackBL** |
| 6 | **POM** | preview → execute; BodyPart TX+list via pack; Template hierarchy may still be custom |
| 7 | **DW blueprint** | load sample / table → preview → execute; Transaction/Search/Menu via pack; pivots in pack |
| 8 | **Search import** | load blueprint → preview → execute via pack |
| 9 | **Sibling** (after Search exists) | `preview/execute_search_sibling_view` — DataSet patch + SearchView only (no new TX) |
| 10 | **MassUpdate** (Hierarchical) | `preview/execute_search_massupdate_view` with `listEditCreate.CreateNew` — ListEdit via **AppConfigPack**; then Mass Update SearchView attach |
| 11 | **Job status / Import log / Discard** | ops tools work |

Also optional: skill **`app-config-pack-orchestrator`** — contract → draft JSON → validate → preview → execute.

---

## 4. Regression spot-checks (SQL / UI)

After a successful Color / DW / MassUpdate run:

- [ ] `AppTransaction` has expected `IntegrationId` and `TransactionOrganizedType` (List for MU ListEdit)
- [ ] Child ListEdit fields: `IsLinkToParentPrimaryKey = 1` on FK (e.g. `ReferenceId`)
- [ ] Search / SearchView / Menu as expected for Color list and DW tabs
- [ ] Agent tool results and session JSON contain **no** connection strings

Sample MassUpdate blueprint: `AppReact/ImportDoc/ImportPLMSearchView/output/23902/3_PlmSearch_MassUpdateView_111.json`  
Sample DW pack: `AppReact/ImportDoc/PlmAgentIntegration/sample-3354.appConfigPack.json`

---

## 5. Gate for Wizard deletion (`p3`)

Only when the checklist above is green on a real tenant:

1. Remove hidden Wizard UI under `AppReact/src/components/dbmgt/plmImport/`
2. Remove `AppReact/src/webapi/plmMigrationSvc.ts` and any dead menu/routes
3. Confirm no Controllers call the PlmImport plugin

Until then, keep Wizard hidden but present as fallback reference.
