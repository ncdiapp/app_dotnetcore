# PLM Migration Multi-Agent — Tenant seed pack

**Folder:** `AppAI.Web/TenantAgentSeeds/PlmIntegrationMultiAgent/`

Apply these scripts to a **new tenant DB** (after structure upgrade) so you can test Migration Multi-Agent without hand-configuring Agent Management.

Source of ROOT/CHILD prompts: working config on **TenantDB_PLM32**.

---

## What you get

| SkillKey | Role | Mode | Libraries |
|---|---|---|---|
| `plm-integration-orchestrator` | **ROOT** — HITL (`ask_user`), menus, `call_agent` | Interactive | `platform-multi-agent`, `integration-plm-import`, `platform-application`, `platform-transaction`, `agent-files` |
| `plm-integration-import-dw` | **CHILD** — ImportFromPLMDW Phase A/B | **Deterministic** | `platform-multi-agent`, `agent-files`, `agent-scripts`, `platform-database`, `platform-application`, `platform-memory`, `platform-search`, `platform-transaction` |
| `app-config-pack-orchestrator` | Standalone App Config Pack NL→JSON→Execute | Interactive | `platform-app-config-pack` |

| Library | Tools |
|---|---|
| `platform-multi-agent` | `call_agent`, `write_shared_context`, `read_shared_context`, `ask_user` |
| `integration-plm-import` | Full ExternalDll PLM import tool set (Connect / Entity / Image / Folder / Color / POM / DW / Search…) |
| `platform-app-config-pack` | `get/validate/preview/execute_app_config_pack` |

Shared context prefix: `plm.integration.*` (see ROOT prompt).

---

## Prerequisites

1. New tenant DB already created and **structure-migrated** (so `AppAgentSkillSet`, `AppAgentToolLibrary`, `AppAgentLibraryTool`, `AppAgentSharedContext`, and platform libraries like `agent-files` / `platform-database` exist — normally from V022–V030).
2. Build + copy `APP.AgentPlugins.PlmImport.dll` → `AppAI.Web/AgentPlugins/` (for ExternalDll tools).
3. Tenant AI API key configured (Application Settings).

---

## How to apply

```bat
cd AppAI.Web\TenantAgentSeeds\PlmIntegrationMultiAgent
RUN_ALL.bat YourServer\Instance YourNewTenantDb
```

Windows auth (`-E`). For SQL auth, edit `RUN_ALL.bat` to add `-U` / `-P`.

Or run files **in order**:

1. `00_Ensure_PlatformMultiAgent_AskUser.sql`
2. `01_Seed_IntegrationPlmImportLibrary.sql`
3. `02_Seed_PlatformAppConfigPackLibrary.sql`
4. `03_Seed_AppConfigPackOrchestrator.sql`
5. `04_Seed_PlmIntegrationImportDw_Child.sql`
6. `05_Seed_PlmIntegrationOrchestrator_Root.sql`
7. `99_Verify.sql`

Scripts are idempotent (IF NOT EXISTS + UPDATE prompts on re-run).

**Notes:** Every `UPDATE AppAgentSkillSet` is scoped with `WHERE SkillKey=...`. Prompts use ASCII separators (`===`, `->`). `RUN_ALL.bat` uses `sqlcmd -f 65001` (UTF-8).

---

## How to test

1. Open Agent chat → select **`plm-integration-orchestrator`** (not the child).
2. On `[session_start]` ROOT should call **`ask_user`** for PLM/DW DataSourceIds, then the task menu.
3. Choose **Import Template TAB…** → TemplateId → Phase A via `call_agent(plm-integration-import-dw)` → detailed confirm → Phase B.
4. Optional: open **`app-config-pack-orchestrator`** for pack-only flows.

Docs: `Document/AgentDesign/MultiAgent-Architecture.md`, `AppReact/ImportDoc/PlmAgentIntegration/Interactive-E2E-Checklist.md`.

---

## Note vs older single-agent seeds

| Folder | Purpose |
|---|---|
| **This pack** (`PlmIntegrationMultiAgent/`) | Multi-agent ROOT + DW CHILD (+ AppConfigPack) |
| `PlmIntegration/Seed_PlmIntegrationOrchestrator.sql` | Older/alternate Ex-DLL Interactive single-agent prompt — **do not** run it after this pack or it will overwrite the ROOT prompt |
