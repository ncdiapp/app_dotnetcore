# Plan: Hot-Path BL Async Conversion

**Status:** TODO  
**Goal:** Add `*Async` variants to the three highest-traffic BL areas (Auth, Search, Transaction Tier-1) and wire their controllers, improving thread-pool efficiency under concurrent load with zero breaking API changes.

## Overview

All BL methods that call the LLBLGen ORM get a paired `*Async` sibling using the existing async ORM API (`FetchEntityAsync`, `SaveEntityAsync`, etc.) with `ConfigureAwait(false)` on every await. Sync methods are kept untouched. Controllers are updated to `async Task<>` and call the new `*Async` BL methods. Three areas executed in order: Area C (Auth, 3 files) → Area A (Search, 4 files) → Area B (Transaction Tier-1 LLBLGen path, 4 files).

**LLBLGen swap map used throughout:**

| Sync | Async |
|---|---|
| `FetchEntity` | `FetchEntityAsync` |
| `FetchEntityCollection` | `FetchEntityCollectionAsync` |
| `SaveEntity` | `SaveEntityAsync` |
| `DeleteEntity` | `DeleteEntityAsync` |
| `UpdateEntitiesDirectly` | `UpdateEntitiesDirectlyAsync` |
| `ExecuteScalarQuery` | `ExecuteScalarQueryAsync` |
| `ExecuteDataTableRetrievalQuery` | `ExecuteDataTableRetrievalQueryAsync` |

---

## Tasks

- [ ] 1. [S] Create `AppMasterAdapterBL` factory
  - Files: `APP.BL/MasterAuth/AppMasterAdapterBL.cs` *(new file)*
  - Build: Create `internal static class AppMasterAdapterBL` in namespace `App.BL` with one method: `internal static DataAccessAdapter GetMasterAdapter() => new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString);`. Then in `AppSecurityUserSessionBL.cs`, `AppSecurityAuthenticationBL.cs`, and `AppSecurityUserBL.cs` replace every `new DataAccessAdapter(MasterConnStr)` with `AppMasterAdapterBL.GetMasterAdapter()` and remove the now-unused `private static string MasterConnStr` property from each.
  - Done when: `dotnet build` passes; `grep -r "new DataAccessAdapter(MasterConnStr)" APP.BL/MasterAuth/` returns zero results.

- [ ] 2. [M] Async-ify `AppSecurityUserSessionBL`
  - Files: `APP.BL/MasterAuth/AppSecurityUserSessionBL.cs`
  - Build: For every public/internal method that opens a `DataAccessAdapter` and calls an ORM method, add a paired `*Async` sibling. Minimum set: `RetrieveCurrentUserByDomainAsync`, `UpdateLoginUserExpiredDateAsync`, `CreateNewAppSecurityUserSessionAsync`, `DeleteAppSecurityUserSessionAsync`, `CheckCurrenSessionIsExsitAsync`. Each: signature `public static async Task<T>`, swap ORM calls per the map above, add `.ConfigureAwait(false)` on every `await`. Do not remove any sync method.
  - Done when: All original sync methods still present; every new method ends in `Async`, returns `Task` or `Task<T>`, contains no `.Result` / `.GetAwaiter().GetResult()`; `dotnet build` zero errors.

- [ ] 3. [M] Async-ify `AppSecurityAuthenticationBL` + `AppSecurityUserBL` auth hot path
  - Files: `APP.BL/MasterAuth/AppSecurityAuthenticationBL.cs`
  - Build: `AppSecurityAuthenticationBL`: add `AuthenticateAsync` and `AuthenticateEStoreAsync` — each mirrors its sync counterpart but awaits ORM calls and calls `AppSecurityUserSessionBL.*Async` variants from Task 2. `AppSecurityUserBL`: add `*Async` siblings for `GetUserContextBySessionId`, `SendUserNameAndPassword`, `RetrieveOneAppSecurityUserEntity`, `SaveAppSecurityUserEntity`, `DeleteAppSecurityUserEntity`, and all remaining DB-touching public methods (~30 total). All awaits `.ConfigureAwait(false)`.
  - Done when: `AppSecurityAuthenticationBL` has `AuthenticateAsync` and `AuthenticateEStoreAsync`; `AppSecurityUserBL` has `GetUserContextBySessionIdAsync`; `dotnet build` passes; no sync method removed.

- [ ] 4. [S] Update `HomeController` to async
  - Files: `AppAI.Web/Controllers/HomeController.cs`
  - Build: Convert `Login`, `MgtLogin`, `Logout`, `UpdateSession`, and `GetUserContext` actions from `public UserContext Foo()` to `public async Task<UserContext> Foo()`. Call `await AppSecurityAuthenticationBL.AuthenticateAsync(...)` / `await AppSecurityUserSessionBL.*Async(...)` etc. from Tasks 2–3. No route attributes change. Add `using System.Threading.Tasks;` if missing.
  - Done when: All five converted actions return `Task<UserContext>` or `Task<>`; no direct calls to sync auth BL methods remain in converted actions; `dotnet build` zero errors.

- [ ] 5. [M] Async-ify `AppSearchConfigBL`
  - Files: `APP.BL/TenantBusiness/AppSearchConfigBL.cs`
  - Build: Add `*Async` sibling for every method that opens `GetTenantAdapter()` (~25 methods). Minimum set: `RetrieveOneAppSearchEntityAsync`, `SaveAppSearchEntityAsync`, `DeleteAppSearchEntityAsync`, `RetrieveAllAppSearchEntityAsync`, `RetrieveUserSavedSearchListAsync`, `SaveUserSavedSearchAsync`, `DeleteUserSavedSearchAsync`. Pattern for each:
    ```csharp
    public static async Task<AppSearchEntity> RetrieveOneAppSearchEntityAsync(object searchId)
    {
        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        {
            var entity = new AppSearchEntity(int.Parse(searchId.ToString()));
            var rootPath = new PrefetchPath2(EntityType.AppSearchEntity);
            rootPath.Add(AppSearchEntity.PrefetchPathAppSearchField);
            await adapter.FetchEntityAsync(entity, rootPath).ConfigureAwait(false);
            return entity;
        }
    }
    ```
    All awaits `.ConfigureAwait(false)`.
  - Done when: At least 20 new `*Async` methods present; no sync method removed; `dotnet build` passes.

- [ ] 6. [M] Async-ify `AppSearchBL`, `AppSearchViewConfigBL`, `AppCascadingSearchBL`
  - Files: `APP.BL/TenantBusiness/AppSearchBL.cs`
  - Build: `AppSearchBL`: add `RetrieveDefaultSearchAsync`, `RetrieveOneReferenceViewDtoAsync`, `FullTextLatestVersionFileSearchAsync`, and all DB-touching methods; where they call `AppSearchConfigBL.*` use the `*Async` variants from Task 5. `AppSearchViewConfigBL`: add `*Async` variants for all ~15 view-definition CRUD methods. `AppCascadingSearchBL`: add `*Async` for both public methods. All awaits `.ConfigureAwait(false)`.
  - Done when: All three files have `*Async` additions; `dotnet build` passes; no `.Result` introduced.

- [ ] 7. [S] Update `AppSearchController` + `AppSearchViewConfigController` to async
  - Files: `AppAI.Web/Controllers/AppSearchController.cs`
  - Build: Convert all action methods in both controllers to `async Task<>` calling the `*Async` BL methods from Tasks 5–6. Example: `public SearchDto RetrieveDefaultSearch(...)` → `public async Task<SearchDto> RetrieveDefaultSearch(...)` with `return await AppSearchBL.RetrieveDefaultSearchAsync(...).ConfigureAwait(false);`. `FullTextSearch` (returns empty set today, no BL call) stays sync.
  - Done when: All DB-calling actions in both controllers return `Task<>`; `dotnet build` zero errors.

- [ ] 8. [M] Async-ify `AppTransactionBL`
  - Files: `APP.BL/TenantBusiness/AppTransactionBL.cs`
  - Build: Add `*Async` siblings for all ~35 public methods that open `GetTenantAdapter()`. Minimum: `RetrieveAllAppTransactionDtoAsync`, `RetrieveOneAppTransactionDtoAsync`, `SaveAppTransactionEntityAsync`, `DeleteAppTransactionEntityAsync`, `RetrieveAppTransactionUnitListAsync`, `SaveAppTransactionUnitAsync`. All awaits `.ConfigureAwait(false)`.
  - Done when: At least 30 new `*Async` methods present; no sync removed; `dotnet build` passes.

- [ ] 9. [M] Async-ify `AppTransactionCommandBL`, `AppMasterDetailFormDataLoadBL`, `AppListEditFormDataLoadBL`
  - Files: `APP.BL/TenantBusiness/AppTransactionCommandBL.cs`
  - Build: `AppTransactionCommandBL`: add `*Async` for all ~35 command/workflow-execution methods using `GetTenantAdapter()`; where they call `AppTransactionBL.*` use `*Async` from Task 8. `AppMasterDetailFormDataLoadBL`: add `*Async` for the ~15 form-data-load methods that use `GetTenantAdapter()` — skip the `DatabaseFixture` code paths entirely. `AppListEditFormDataLoadBL`: add `*Async` for all ~6 grid-data methods. All awaits `.ConfigureAwait(false)`.
  - Done when: All three files have `*Async` additions; no `.Result` / `.GetAwaiter().GetResult()` introduced; `dotnet build` passes.

- [ ] 10. [M] Update `AppTransactionController` hot actions to async
  - Files: `AppAI.Web/Controllers/AppTransactionController.cs`
  - Build: Convert only the hot-path actions — those calling `AppMasterDetailFormDataLoadBL`, `AppTransactionCommandBL`, or `AppTransactionBL.RetrieveAll*` / `RetrieveOne*`. Each: `public async Task<OperationCallResult<T>> Foo(...)` calling `await FooBL.FooAsync(...).ConfigureAwait(false)`. Leave non-DB actions (pure DTO assembly, redirect logic) as sync. Do not attempt to convert all ~2700 lines.
  - Done when: All identified hot-path form-load, form-save, and command-execution actions return `Task<>`; non-DB actions unchanged; `dotnet build` zero errors; no `.Result` introduced.

---

## Out of Scope

- `AppMasterDetailFormDataSaveBL` — uses `DatabaseFixture` (raw ADO.NET), not LLBLGen; needs separate `DatabaseFixture` async work item
- Area B Tier-2 (~14 additional transaction BL files, e.g. `AppTransactionDataTransferBL`, `AppTransactionConditionalActionBL`) — separate phase
- `DatabaseFixture.RetriveDataTable` / `ExecuteNonQueryResult` async overloads — separate work item
- Any BL file not listed above
- xUnit test additions (no BL-layer tests exist today)
- `ServerContext` / `IAppClientContext` migration — separate Phase 5 task
