# PLM Migration Multi-Agent — Tenant seed pack

**Folder:** `AppAI.Web/TenantAgentSeeds/PlmIntegrationMultiAgent/`

Apply these scripts to a **new tenant DB** (after structure upgrade through **V031+**) so you can test Migration Multi-Agent without hand-configuring Agent Management.

**New-tenant only:** scripts `INSERT` missing libraries/agents; they do **not** `UPDATE` existing agents.

---

## What you get

| SkillKey | Role | Mode | Libraries |
|---|---|---|---|
| `plm-integration-orchestrator` | **ROOT** — HITL (`ask_user`), menus, `call_agent` | Interactive (`AllowAgentFirstTurn=1`) | `platform-multi-agent`, `integration-plm-import`, `platform-application`, `platform-transaction`, `agent-files` |
| `plm-integration-import-dw` | **CHILD** — ImportFromPLMDW Phase A/B | **Deterministic** | `platform-multi-agent`, `agent-files`, `agent-scripts`, `platform-database`, `platform-application`, `platform-memory`, `platform-search`, `platform-transaction` |

| Library | Tools |
|---|---|
| `platform-multi-agent` | From structure migrations (V022/V028/V030) — not seeded here |
| `integration-plm-import` | Full ExternalDll PLM import tool set (Connect / Entity / Image / Folder / Color / POM / DW / Search…) |

Shared context prefix: `plm.integration.*` (see ROOT prompt).

App Config Pack agents/libraries live under `TenantAgentSeeds/AppConfigPack/` (not part of this pack).

---

## Prerequisites

1. New tenant DB created and **structure-migrated through V031** (`AllowAgentFirstTurn` column; `platform-multi-agent` + `ask_user`; platform libs like `agent-files` / `platform-database`).
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

1. `01_Seed_IntegrationPlmImportLibrary.sql`
2. `02_Seed_PlmIntegrationImportDw_Child.sql`
3. `03_Seed_PlmIntegrationOrchestrator_Root.sql`
4. `99_Verify.sql`

`RUN_ALL.bat` uses `sqlcmd -f 65001` (UTF-8). Prompts use ASCII separators (`===`, `->`).

---

## How to test

1. Open Agent chat → select **`plm-integration-orchestrator`** (not the child).
2. On `[session_start]` ROOT should call **`ask_user`** for PLM/DW DataSourceIds, then the task menu.
3. Choose **Import Template TAB…** → TemplateId → Phase A via `call_agent(plm-integration-import-dw)` → detailed confirm → Phase B.

Docs: `Document/AgentDesign/MultiAgent-Architecture.md`, `AppReact/ImportDoc/PlmAgentIntegration/Interactive-E2E-Checklist.md`.

---

## Note vs older single-agent seeds

| Folder | Purpose |
|---|---|
| **This pack** (`PlmIntegrationMultiAgent/`) | Multi-agent ROOT + DW CHILD |
| `PlmIntegration/Seed_PlmIntegrationOrchestrator.sql` | Older/alternate Ex-DLL Interactive single-agent — **do not** run it after this pack |
| `AppConfigPack/` | Standalone App Config Pack library + orchestrator |
