# Agent Tools — External DLL (platform capability)

`ToolType = ExternalDll` is a **general** Agent Tool execution mode. Any tenant can drop a DLL and register it as a Library tool or agent-owned tool. It is **not** limited to PLM Integration (or any single product).

Related (different product): Command/Form plugins use `IAppPlugin` + `ExternalDllRepository\` — see `Document/Design/TechPack_Plugin_Architecture.md`.

**Integration Agents (product + App Config):** see **[Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md)** (register-id Connect, AppConfigPack BuiltIn, seed layout).

---

## Reference sample in this repo

| Item | Path |
|---|---|
| Project | `APP.AgentPlugins.Sample/` (`HelloTool : IAgentTool`) |
| DLL drop | `AppAI.Web/AgentPlugins/APP.AgentPlugins.Sample.dll` (post-build) |
| Tenant seed | `AppAI.Web/TenantAgentSeeds/SampleExternalDll/Seed_SampleHelloTool.sql` |
| ToolName | `sample_hello` |
| ToolConfig | `{ "AssemblyName": "APP.AgentPlugins.Sample.dll", "TypeName": "APP.AgentPlugins.Sample.HelloTool" }` |

```bat
dotnet build APP.AgentPlugins.Sample\APP.AgentPlugins.Sample.csproj
sqlcmd -S <server> -d <TenantDB> -E -i AppAI.Web\TenantAgentSeeds\SampleExternalDll\Seed_SampleHelloTool.sql
```

Then subscribe any agent to library `sample-agent-plugins`, add SystemPrompt: call `sample_hello` with `{ "name": "…" }`, restart web if needed.

---

## Why ExternalDll

| Need | Use |
|---|---|
| Customer / ISV logic without changing APP.BL | ExternalDll |
| Platform built-in C# in APP.BL | BuiltIn |
| SQL / HTTP / script only | SqlQuery / HttpRest / PowerShell / DynamicCSharp |

All types go through the same dispatcher: `AppAgentToolEngine.Dispatch`.

---

## Registration model

| Scope | Table | When |
|---|---|---|
| **Library tool** | `AppAgentLibraryTool` | Share across agents (recommended) |
| **Agent-owned tool** | `AppAgentToolRegister` | One SkillKey only |

Agents receive library tools via **`AppAgentLibrarySubscription`** (Agent Management → subscribe libraries).

Tenant-specific seed SQL (agents, libraries, tools) belongs under `AppAI.Web/TenantAgentSeeds/` — **not** numbered `Migrations/Vxxx` schema upgrades.

---

## How to ship your own External DLL tool

### A. Write the DLL

1. Class library targeting the same .NET as AppAI.Web (net10.0).
2. Reference **`APP.Framework`** (optional small DTOs). Prefer **not** referencing APP.BL.
3. Implement `IAgentTool`:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace Acme.MyTools;

public sealed class HelloTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("name", out var name);
        return Task.FromResult(JsonConvert.SerializeObject(new
        {
            ok = true,
            hello = name ?? "world",
            companyId = context.CompanyId,
            dataSourceId = context.DataSourceId
        }));
    }
}
```

4. Deploy the DLL (and private dependencies) to the **web server**:

| Config | Folder |
|---|---|
| Default | `{AppAI.Web BaseDirectory}/AgentPlugins/` |
| Override | AppConfig / env `Agent.ExternalDllRepo` |

5. Recycle AppAI.Web after replacing a DLL (`Assembly.LoadFrom` does not unload).

### B. Register a Library (optional but recommended)

Agent Management → Tool Libraries → create e.g. `acme-custom-tools`  
(`DomainKey` must already exist: `platform`, `external-rest`, …).

### C. Register the Tool

| Field | Value |
|---|---|
| ToolName | e.g. `acme_hello` (LLM-visible name) |
| ToolType | `ExternalDll` |
| Description | When/why the agent should call it |
| ParameterSchemaJson | JSON Schema for LLM arguments |
| ToolConfig | see below |
| IsActive | true |

**ToolConfig (executor contract):**

```json
{
  "AssemblyName": "Acme.MyTools.dll",
  "TypeName": "Acme.MyTools.HelloTool"
}
```

- `AssemblyName` = file name under `AgentPlugins` (include `.dll`)
- `TypeName` = full type implementing `IAgentTool`
- No `MethodName` (unlike BuiltIn). One type = one tool row, or branch inside `ExecuteAsync` on `args["operation"]`.

**ParameterSchemaJson example:**

```json
{
  "type": "object",
  "properties": {
    "name": { "type": "string", "description": "Name to greet" }
  },
  "required": ["name"]
}
```

### D. Attach to an Agent

1. Edit SkillSet SystemPrompt so the LLM knows the ToolName and when to use it.
2. Subscribe the library (or add the same row under agent-owned tools).
3. Chat / RUN → LLM tool call → Dispatch → your `ExecuteAsync`.

---

## Runtime path

```
LLM (or any host calling Dispatch)
  → AppAgentToolEngine.Dispatch(ToolType=ExternalDll, ToolConfig, args, AgentToolContext)
  → ExternalDllToolExecutor
       OverrideThreadIdentity (User / Company / DataSource / …)
       Assembly.LoadFrom(AgentPlugins/{AssemblyName})
       Activator.CreateInstance(TypeName) as IAgentTool
       tool.ExecuteAsync(args, context, ct)
```

`AgentToolContext`: ConnectionString, DatabaseName, UserId, CompanyId, DataSourceId, WorkflowId, ChatSessionKey, SkillKey, …

Code: `APP.BL/TenantBusiness/AgentToolExecutors/ExternalDllToolExecutor.cs`, contract `APP.Framework/Plugin/IAgentTool.cs`.

---

## Checklist

1. [ ] DLL implements `IAgentTool`, under `AgentPlugins`
2. [ ] Library and/or agent-owned tool registered
3. [ ] ToolType=`ExternalDll`, ToolConfig=`AssemblyName` + `TypeName`
4. [ ] ParameterSchemaJson matches arg keys in code
5. [ ] Agent subscribed / owns the tool; SystemPrompt mentions ToolName
6. [ ] App pool recycled after DLL update

---

## Example only: PLM Integration

PLM Import/Integration is **one optional consumer** of ExternalDll (and of BuiltIn wrappers while migrating). It does not own this platform feature.

| Item | Location |
|---|---|
| Optional tenant seed | `AppAI.Web/TenantAgentSeeds/PlmIntegration/` (or MultiAgent pack) |
| PLM docs home | `AppReact/ImportDoc/PlmAgentIntegration/` |
| Image vertical slice | [PlmImport-Image-ExternalDll-Slice.md](../../AppReact/ImportDoc/PlmAgentIntegration/PlmImport-Image-ExternalDll-Slice.md) + `APP.AgentPlugins.PlmImport` |
| Entity vertical slice | [PlmImport-Entity-ExternalDll-Slice.md](../../AppReact/ImportDoc/PlmAgentIntegration/PlmImport-Entity-ExternalDll-Slice.md) |

Other integrations (ERP, custom imports, company rules) use the same Library + ExternalDll flow above.
