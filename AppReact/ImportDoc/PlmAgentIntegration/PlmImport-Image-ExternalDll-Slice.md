# PLM Image Import → Agent Tools → ExternalDll (vertical slice)

Status: **Phase B2 done** — Execute path: DLL exports PLM binaries to staging; Host inserts AppFile.

**Docs home:** `AppReact/ImportDoc/PlmAgentIntegration/` (moved from `Document/AgentDesign/`).

## Split

| Layer | Responsibility |
|---|---|
| **ExternalDll** `SketchPreviewTool` | Preview counts |
| **ExternalDll** `SketchImportExporter` / `SketchExportTool` | Read `tblSketch` → staging files + manifest |
| **Host** `PreviewPlmSketchImport` | Session decrypt → bridge preview |
| **Host** `ImportPlmSketchesToAppFile` | Existing FileIDs → export via bridge → `INSERT AppFile` → cleanup staging |
| **Host** Job table / admin / company image path | Unchanged |

Progress: export 0–50%, AppFile write 50–99%.

## Deploy

```bat
dotnet build APP.AgentPlugins.PlmImport\APP.AgentPlugins.PlmImport.csproj
```

Restart AppAI.Web after replacing the DLL.

## Tools (`integration-plm-import`)

| ToolName | Type |
|---|---|
| `preview_plm_sketch_import` | BuiltIn (session) |
| `plm_build_sketch_import_preview` | ExternalDll |
| `plm_export_sketches_to_staging` | ExternalDll (optional/direct) |
| `execute_plm_sketch_import` | BuiltIn → Host job → DLL export + Host insert |
| `get_plm_import_job` / `cancel_plm_import_job` | BuiltIn |

## Phase plan

| Phase | Work |
|---|---|
| **A** | BuiltIn + Wizard Dispatch |
| **B** | Preview → ExternalDll |
| **B2** | Execute export → ExternalDll staging; Host AppFile |
| **C** | Entity import — **complete** (`PlmImport-Entity-ExternalDll-Slice.md`) |

See: platform [Agent-ExternalDll-Tools.md](../../../Document/AgentDesign/Agent-ExternalDll-Tools.md), [Agent-Replace-Wizard-Progress.md](./Agent-Replace-Wizard-Progress.md).
