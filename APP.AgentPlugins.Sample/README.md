# Sample ExternalDll Agent Plugin

Reference implementation of `IAgentTool` for customer / ISV plugins.

## Build & deploy

```bat
dotnet build APP.AgentPlugins.Sample\APP.AgentPlugins.Sample.csproj
```

Post-build copies `APP.AgentPlugins.Sample.dll` to:

- `AppAI.Web/bin/{Configuration}/net10.0/AgentPlugins/`
- `AppAI.Web/AgentPlugins/` (convenient drop folder)

Restart AppAI.Web if the DLL was already loaded.

## Register (UI)

1. Agent Management → Tool Libraries → create e.g. `sample-agent-plugins`
2. Add tool:
   - ToolName: `sample_hello`
   - ToolType: `ExternalDll`
   - ToolConfig:
     ```json
     { "AssemblyName": "APP.AgentPlugins.Sample.dll", "TypeName": "APP.AgentPlugins.Sample.HelloTool" }
     ```
   - ParameterSchemaJson: see `../AppAI.Web/TenantAgentSeeds/SampleExternalDll/Seed_SampleHelloTool.sql`
3. Subscribe an agent to the library; SystemPrompt: call `sample_hello` with `{ "name": "..." }`.

## Register (SQL)

Optional tenant seed: `AppAI.Web/TenantAgentSeeds/SampleExternalDll/Seed_SampleHelloTool.sql`

Docs: `Document/AgentDesign/Agent-ExternalDll-Tools.md`
