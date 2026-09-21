# PLM Entity Import → Agent Tools → ExternalDll (vertical slice)

Status: **Phase C complete** (C-A … C-D).

**Docs home:** `AppReact/ImportDoc/PlmAgentIntegration/` (moved from `Document/AgentDesign/`).

## Surfaces

| Surface | Preview | Execute | Notes |
|---|---|---|---|
| **TableExport** | `preview_plm_table_export_plan` | `execute_plm_table_export` | Plan + physical copy → ExternalDll |
| **SystemDefine** | `preview_system_define_entity_import` | `execute_system_define_entity_import` | Preview → ExternalDll; Execute Host AppEntityInfo write |
| **UserDefine** | `preview_user_define_entity_import` | `execute_user_define_entity_import` | Preview → ExternalDll; Execute Host write + row import |

Jobs reuse `get_plm_import_job` / `cancel_plm_import_job` (Image plugin).

## Split

| Layer | Responsibility |
|---|---|
| **ExternalDll** `TableExportPlanTool` | Plan: `pdmEntity` + source table existence + target prefix |
| **ExternalDll** `TableExportExporter` / `TableExportTool` | Physical PLM → tenant table copy |
| **ExternalDll** `SystemDefineEntityPreviewTool` | Staging + tenant validation (Host supplies `dataSourceMapsJson`) |
| **ExternalDll** `UserDefineEntityPreviewTool` | Staging + tenant validation (Host supplies `tenantDatabaseName`) |
| **Host** | Session decrypt, admin gate, `AppDataSourceRegister` resolution, AppEntityInfo / job table, UD row import |

## Deploy

```bat
dotnet build APP.AgentPlugins.PlmImport\APP.AgentPlugins.PlmImport.csproj
```

Re-run tenant seed: `AppAI.Web/TenantAgentSeeds/PlmIntegration/Seed_IntegrationPlmImportLibrary.sql`.  
Restart AppAI.Web after replacing the DLL.

## Tools (`integration-plm-import`)

| ToolName | Type |
|---|---|
| `preview_plm_table_export_plan` | BuiltIn (session) |
| `plm_build_table_export_plan` | ExternalDll |
| `execute_plm_table_export` | BuiltIn → Host job → DLL copy |
| `plm_export_tables_to_tenant` | ExternalDll (direct) |
| `preview_system_define_entity_import` | BuiltIn |
| `plm_build_system_define_entity_preview` | ExternalDll |
| `execute_system_define_entity_import` | BuiltIn → Host |
| `preview_user_define_entity_import` | BuiltIn |
| `plm_build_user_define_entity_preview` | ExternalDll |
| `execute_user_define_entity_import` | BuiltIn → Host |

## Phase plan

| Phase | Work | Status |
|---|---|---|
| **C-A** | BuiltIn Entity plugin + Wizard Dispatch + seed | Done |
| **C-B** | TableExport **plan** → ExternalDll | Done |
| **C-C** | SystemDefine / UserDefine **preview** → ExternalDll | Done |
| **C-D** | TableExport physical copy → ExternalDll | Done |

**Left Host-side (by design):** System/User Define **execute** (AppEntityInfo + UD data rows) — same pattern as Image B2 Host AppFile insert.

See: platform [Agent-ExternalDll-Tools.md](../../../Document/AgentDesign/Agent-ExternalDll-Tools.md), Image slice [PlmImport-Image-ExternalDll-Slice.md](./PlmImport-Image-ExternalDll-Slice.md).
