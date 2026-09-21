# Agent Integration Playbook

How to add a **product Integration** (e.g. PLM) as Interactive Agent tools, without putting product logic in Controllers or inventing a second App Config writer.

Related:

- [Agent-ExternalDll-Tools.md](./Agent-ExternalDll-Tools.md) — ExternalDll platform capability
- [AppReact/ImportDoc/ImportAppConfig/PROMPT.md](../../AppReact/ImportDoc/ImportAppConfig/PROMPT.md) — App Config Pack JSON contract
- PLM progress: [AppReact/ImportDoc/PlmAgentIntegration/Agent-Replace-Wizard-Progress.md](../../AppReact/ImportDoc/PlmAgentIntegration/Agent-Replace-Wizard-Progress.md)

---

## Architecture

```text
Interactive Agent (SkillKey)
  → Library tools (preview_* / execute_* / list_*)
      → ExternalDll (product)  and/or  BuiltIn (platform)
          → APP.BL shared services (AppConfigPackBL, DataSourceRegister, …)
```

| Concern | Where |
|---|---|
| Product connect / read / data copy / session / jobs | **ExternalDll** (`APP.AgentPlugins.{Product}`) |
| App Config JSON → DDL/TX/Search/Menu | **BuiltIn** `platform-app-config-pack` → `AppConfigPackBL` |
| HTTP Controllers | Must **not** ProjectReference product plugins |

---

## Security: no connection strings in Agent

- Never ask_user for a SQL connection string; never put one in tool args or tool results.
- Bind roles (PLM, PLMDW, ERP, …) to **tenant `DataSourceRegisterId`** values that already exist for the company.
- Resolve/decrypt connection strings only inside the Ex DLL / BL.
- Admins register sources in Data Source Register UI **before** the Agent Connect step.

PLM reference: `list_tenant_data_sources` → ask_user → `save_plm_import_session(plmDataSourceRegisterId, …)`.

---

## App Config Pack (platform BuiltIn)

Library: `platform-app-config-pack`  
Skill example: `app-config-pack-orchestrator`

| Tool | Purpose |
|---|---|
| `get_app_config_pack_contract` | Fixed JSON contract (PROMPT.md) so the Agent can draft packs from NL |
| `validate_app_config_pack` | Schema/validate |
| `preview_app_config_pack` | Insert/Update/Skip plan |
| `execute_app_config_pack` | Apply via `AppConfigPackBL.Execute` |

Flow: contract → draft JSON → validate → preview → ask_user confirm → execute.

Seeds: `AppAI.Web/TenantAgentSeeds/AppConfigPack/`.

Product Ex DLLs may **compose** `AppConfigPackDto` in code and call `AppConfigPackBL` in-process; Agent-facing apply should prefer the BuiltIn tools when the user is editing JSON.

---

## Checklist: new Integration

1. Create `APP.AgentPlugins.{Product}` implementing `IAgentTool`; post-build copy DLL to Web `AgentPlugins/`.
2. Session/job tables product-prefixed if needed; Connect via register ids only.
3. Tenant seeds under `AppAI.Web/TenantAgentSeeds/{Product}/` (not Flyway `Vxxx`):
   - Library `integration-{product}-…` with tools
   - Interactive skill `{product}-integration-orchestrator` + ask_user menu
4. App Config writes: compose pack → `AppConfigPackBL` (or BuiltIn execute). No parallel CreateHierarchy for Agent-scoped config. Product-only Search enrichment (e.g. PLM Sibling View attach) may stay outside the pack.
5. Subscribe skill to product library (+ `platform-app-config-pack` if NL pack edit is needed).
6. Document under `AppReact/ImportDoc/` for product-specific prompts.

---

## Naming

| Kind | Pattern |
|---|---|
| LibraryKey | `integration-{product}-…` or `platform-…` |
| SkillKey | `{product}-integration-orchestrator` |
| Tools | `list_*`, `preview_*`, `execute_*`, optional `build_*_app_config_pack` |
