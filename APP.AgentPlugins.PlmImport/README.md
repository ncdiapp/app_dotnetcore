# APP.AgentPlugins.PlmImport

PLM Integration **ExternalDll** (agent tools). Hosts `PlmImportEngine` (formerly PlmMigrationBL).

- Namespace: `APP.AgentPlugins.PlmImport`
- Calls shared APP.BL for Register / Transaction / AppFile / **AppConfigPackBL**
- Host Controllers must **not** reference this project (LoadFrom via AgentPlugins only)

## App Config path

Dw / Search blueprints convert to `AppConfigPackDto` then run through `AppConfigPackBL.Preview/Execute`:

- `DwBlueprintAppConfigPackBuilder`
- `SearchImportAppConfigPackBuilder`
- Tools: `build_dw_app_config_pack`, `build_search_app_config_pack`

Platform Import Config UI/docs: `AppReact/ImportDoc/ImportAppConfig/`.

```bat
dotnet build APP.AgentPlugins.PlmImport\APP.AgentPlugins.PlmImport.csproj
```

Progress: `AppReact/ImportDoc/PlmAgentIntegration/Agent-Replace-Wizard-Progress.md`
