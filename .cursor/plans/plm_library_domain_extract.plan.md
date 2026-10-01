# PLM library cleanup: domain + extract reusable BuiltIn tools

## Decisions (locked)

1. New domain: `DomainKey=plm-integration`, display **PLM Integration**
2. Move library `integration-plm-import` → domain `plm-integration`
3. Extract scope **B**:
   - `list_tenant_data_sources` → **`platform-database`** (BuiltIn / BL)
   - `list_tenant_saas_applications` → **`platform-application`** (BuiltIn / BL)
   - `test_plm_connection` → rename **`test_data_source_connection`** → **`platform-database`** (BuiltIn / BL)
4. Remove the three ExternalDll rows from `integration-plm-import` after BuiltIns exist
5. Update PLM seeds / prompts / subscriptions / verify scripts

## Migration (Flyway) — APP-wide only (`V040__PlatformTenantCatalogTools_xhu.sql`)
PLM domain / library move / DELETE / prompt rename → `TenantAgentSeeds/PlmIntegrationMultiAgent/00_Upgrade_CatalogExtract.sql` (not Flyway).

## Migration notes (superseded mixed V040)

Note: repo already has `V039__DataUiRenderLibrary_xhu.sql` and `V039__AgentToolCatalog.sql`. Use **`V040__PlmIntegrationDomainAndPlatformListTools_xhu.sql`** (idempotent).

Contents:

1. `INSERT` domain `plm-integration` if missing (`SortOrder` after existing, e.g. 5)
2. `UPDATE AppAgentToolLibrary SET DomainKey=N'plm-integration' WHERE LibraryKey=N'integration-plm-import'`
3. `INSERT` BuiltIn tools into `AppAgentLibraryTool`:
   - `platform-database` / `list_tenant_data_sources`
   - `platform-database` / `test_data_source_connection`
   - `platform-application` / `list_tenant_saas_applications`
4. `DELETE` from `AppAgentLibraryTool` where `LibraryKey=integration-plm-import` and ToolName in those three (including old `test_plm_connection`)

ToolConfig shape (match V028):

```json
{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"ListTenantDataSources"}
```

(and `TestDataSourceConnection`, `ListTenantSaasApplications`)

## Backend (APP.BL)

New plugin e.g. [`TenantCatalogPlugin.cs`](APP.BL/AIAgent/GenericAgent/Plugins/TenantCatalogPlugin.cs) (or split Database/Application if preferred):

| Method | Behavior (port from PlmImport, drop PLM-only gates) |
|--------|-----------------------------------------------------|
| `ListTenantDataSources` | `AppDataSourceRegisterBL.GetDataSourceRegisterList()` → Id, Name, DatabaseName only; never connection strings |
| `TestDataSourceConnection` | Open connection by `dataSourceRegisterId`; return ok/error; **no PLM migration-admin requirement** |
| `ListTenantSaasApplications` | Slim SaasApplicationId + ApplicationName (`GetSaasApplicationList` / menu fallback as today) |

Reuse logic currently in:

- [`ConnectTools.cs`](APP.AgentPlugins.PlmImport/ConnectTools.cs)
- [`PlmImportEngine.Connection.cs`](APP.AgentPlugins.PlmImport/Migration/PlmImportEngine.Connection.cs) (`ListTenantDataSources`, SaaS list, connection test)

PLM DLL: leave old ExternalDll classes as thin wrappers that call BL **or** delete after seed removal (prefer delete + update any C# references to tool names). Prefer **move implementation to BL**; PlmImport ExternalDll methods for these three can be removed or become obsolete stubs that return “use platform library”.

## Tenant seeds / prompts

Update under [`TenantAgentSeeds/PlmIntegrationMultiAgent/`](AppAI.Web/TenantAgentSeeds/PlmIntegrationMultiAgent/):

| File | Change |
|------|--------|
| `01_Seed_IntegrationPlmImportLibrary.sql` | DomainKey `plm-integration`; remove the 3 tool inserts; keep rest |
| `03_Seed_PlmIntegrationOrchestrator_Root.sql` | Subscribe `platform-database` + `platform-application` if missing; replace `test_plm_connection` → `test_data_source_connection` in SystemPrompt |
| Other child seeds that mention `test_plm_connection` | Rename |
| `99_Verify.sql` | Expect BuiltIn tools on platform libs; PLM lib without the 3 names |
| Older `TenantAgentSeeds/PlmIntegration/` pack | Same cleanup for consistency |

Orchestrator already uses `list_applications` from platform-application in places; Gate-0 must keep using slim `list_tenant_saas_applications`.

## Out of scope

- Moving remaining ~46 PLM tools out of ExternalDll
- Changing Tool Activity / chat button themes
- Renaming library key `integration-plm-import`

## Todos

1. V040 migration: domain + move library + insert 3 BuiltIns + delete PLM ExternalDll copies
2. APP.BL `TenantCatalogPlugin` (3 methods) ported from PlmImport connection helpers
3. Update multi-agent + legacy PLM seeds/prompts/verify; remove obsolete ExternalDll tool classes or stub
4. Smoke: subscribe platform-database/application on orchestrator; Gate-0 list + test connection still works
