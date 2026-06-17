# APP.TechPack — Plugin Module Architecture Design

**Module:** POM / Grading / QC (TechPack)  
**Date:** 2026-06-17  
**Author:** Platform Engineering

---

## 1. Purpose

`APP.TechPack` implements the POM / Grading / Fit / QC business logic for the PLM module. It is designed as a **true runtime plugin** — the platform solution (`AppAI.Core.sln`) has zero compile-time knowledge of this module. Deployment requires only:

1. Drop `APP.TechPack.dll` into `ExternalDllRepository\`
2. Insert rows into `AppExternalMethodRegister`
3. No platform recompile. No AppAI.Web changes.

---

## 2. Core Design Principle

> **The platform must never depend on a plugin at compile time.**  
> Plugins may depend on platform infrastructure (framework + DTOs), but not vice versa.

### What is explicitly forbidden

| Forbidden | Reason |
|---|---|
| `APP.BL.Core.csproj` → `APP.TechPack` | Platform core must not know plugins |
| `AppAI.Web.csproj` → `APP.TechPack` | Platform host must not know plugins |
| `APP.TechPack` → `APP.BL` | Plugin must not pull in LLBLGen transitively |
| `APP.TechPack` → `LLBL` | LLBLGen is platform-internal ORM |

---

## 3. Dependency Map

```
┌─────────────────────────────────────────────────────────────┐
│  PLATFORM (AppAI.Core.sln) — no knowledge of plugins        │
│                                                              │
│  AppAI.Web ──→ APP.BL ──→ APP.Framework                     │
│                       └──→ APP.Components.Dto                │
│                       └──→ LLBL (LLBLGen ORM)               │
│                                                              │
│  APP.Framework/Plugin/                                       │
│    IAppPlugin      ← contract (object? in / object? out)    │
│    PluginContext   ← per-request context (connStr, method)   │
│                                                              │
│  AppPluginEngine (APP.BL)                                    │
│    IAppPlugin dispatch → Activator.CreateInstance + Execute  │
│    static-method fallback → reflection (legacy)              │
│                                                              │
│  AppExternalMethodRegisterBL                                 │
│    delegates to AppPluginEngine                              │
└──────────────────────────┬──────────────────────────────────┘
                           │ runtime only (Assembly.LoadFrom)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  APP.TechPack.dll  (plugin, lives in ExternalDllRepository\) │
│                                                              │
│  APP.TechPack ──→ APP.Framework       (no LLBLGen ✅)       │
│               ──→ APP.Components.Dto  (no LLBLGen ✅)       │
│               ──→ System.Data.SqlClient                      │
│                                                              │
│  PluginEntry : IAppPlugin  ← dispatches on context.MethodName│
│  GradeRuleService          ← pure ADO.NET (SqlConnection)   │
│  GradingEngine             ← pure math, no I/O              │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Generic Plugin Engine

The platform ships a three-part engine in `APP.Framework` and `APP.BL`. All new modules use this rather than ad-hoc reflection.

### 4.1 IAppPlugin — `APP.Framework/Plugin/IAppPlugin.cs`

```csharp
namespace APP.Framework.Plugin;

public interface IAppPlugin
{
    object? Execute(object? input, PluginContext context);
}
```

`object?` input/output keeps the interface free of DTO assembly dependencies. The engine casts the result to the required concrete type after invocation.

### 4.2 PluginContext — `APP.Framework/Plugin/PluginContext.cs`

```csharp
namespace APP.Framework.Plugin;

public sealed class PluginContext
{
    public string ConnectionString { get; }
    public string MethodName { get; }

    public PluginContext(string connectionString, string methodName) { ... }

    public static PluginContext FromServerContext(string methodName)
    {
        var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
        return new PluginContext(
            identity?.CurrentUserDbConnectionString ?? string.Empty,
            methodName);
    }
}
```

Plugins receive `ConnectionString` and `MethodName` without any direct dependency on `ServerContext` or `AppClientIdentity`. `AppClientIdentity` is defined in `APP.Framework` (namespace `APP.Components.Dto`), so `PluginContext` can reference it without a cross-assembly dependency.

`MethodName` carries the `AppExternalMethodRegister.MethodName` value — multi-method plugin classes switch on it internally.

### 4.3 AppPluginEngine — `APP.BL/TenantBusiness/AppPluginEngine.cs`

Central loader for all platform plugins.

```csharp
public static class AppPluginEngine
{
    public static readonly string DllRoot =
        AppDomain.CurrentDomain.BaseDirectory + @"ExternalDllRepository\";

    public static TResult Invoke<TResult>(
        string assemblyName, string typeName, string methodName, object? input)
        where TResult : class
    {
        var context = PluginContext.FromServerContext(methodName);
        var assembly = Assembly.LoadFrom(Path.Combine(DllRoot, assemblyName + ".dll"));
        var type = assembly.GetType(typeName) ?? throw ...;

        // IAppPlugin path (preferred for all new modules)
        if (typeof(IAppPlugin).IsAssignableFrom(type))
        {
            var plugin = (IAppPlugin)Activator.CreateInstance(type)!;
            return plugin.Execute(input, context) as TResult ?? throw ...;
        }

        // Static method reflection fallback (legacy plugins)
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) ?? throw ...;
        return method.Invoke(null, [input]) as TResult ?? throw ...;
    }
}
```

**Loading strategy:**
1. **IAppPlugin path** — if the resolved type implements `IAppPlugin`, the engine instantiates it and calls `Execute(input, context)`. `MethodName` travels via `PluginContext` for internal dispatch. This is the preferred path for all new modules.
2. **Static method fallback** — for legacy plugins compiled before this engine. The named static method is invoked via reflection with `[input]`. These plugins receive no `PluginContext`.

### 4.4 AppExternalMethodRegisterBL integration

`CallExternalMethodMasterDetail` and `GetFieldTargetExternalMethod` delegate to the engine:

```csharp
public static OperationCallResult<AppMasterDetailDto> CallExternalMethodMasterDetail(
    object methodRegisterId, object[] paramter)
{
    var dto = RetrieveOneAppExternalMethodRegisterEntity(methodRegisterId);
    return AppPluginEngine.Invoke<OperationCallResult<AppMasterDetailDto>>(
        dto.AssemblyName, dto.TypeName, dto.MethodName,
        paramter.Length > 0 ? paramter[0] : null);
}
```

### 4.5 AppExternalMethodRegister table

| Column | Type | Description |
|---|---|---|
| `MethodRegisterId` | INT PK | Auto-identity |
| `MethodDisplayName` | NVARCHAR(100) | UI label |
| `AssemblyName` | NVARCHAR(500) | DLL filename without `.dll` |
| `TypeName` | NVARCHAR(500) | Fully-qualified class name |
| `MethodName` | NVARCHAR(500) | Passed to plugin as `context.MethodName` for dispatch |
| `InputParameterList` | NVARCHAR(500) | Pipe-delimited parameter descriptor |

### 4.6 API endpoint (no new controller needed)

```
POST /webapi/apptransaction/CallLinkTargetExternalMethod
```

`AppTransactionController` already handles this. The request body is `AppMasterDetailDto` with `ExternalMethodRegId` set.

---

## 5. Plugin Contract

All new modules implement `IAppPlugin` with a **parameterless public constructor**:

```csharp
public sealed class MyPluginEntry : IAppPlugin
{
    public object? Execute(object? input, PluginContext context)
    {
        var formData = (AppMasterDetailDto)input!;
        return context.MethodName switch
        {
            "OperationA" => OperationA(formData, context.ConnectionString),
            "OperationB" => OperationB(formData, context.ConnectionString),
            _ => throw new InvalidOperationException($"Unknown method: {context.MethodName}")
        };
    }
}
```

Input values are passed via `formData.DictOneToOneFields` (Dictionary\<string, object\>).  
Output values are written back into the same dictionary before returning.

**Legacy static method contract** (supported via fallback, not recommended for new modules):

```csharp
public static OperationCallResult<AppMasterDetailDto> MethodName(AppMasterDetailDto formData)
```

---

## 6. Project Structure

```
APP.TechPack/
├── APP.TechPack.Core.csproj          ← standalone build; refs APP.Framework + APP.Components.Dto
├── PluginEntry.cs                     ← implements IAppPlugin; dispatches on context.MethodName
├── Engine/
│   ├── GradeRuleInput.cs              ← record: BodyPartCode, PlusValue, MinuValue
│   ├── IGradingEngine.cs              ← interface: ComputeSizeValues, ApplyGradeRuleSet, etc.
│   └── GradingEngine.cs              ← pure math, zero I/O, fully unit-tested
└── Services/
    ├── GradeRuleService.cs            ← ADO.NET against Tchp* tables; takes string connStr
    └── GradeRuleApplyCoverage.cs      ← DTO: matched / unmatched body-part codes
```

### 6.1 APP.TechPack.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>APP.TechPack</AssemblyName>
    <RootNamespace>APP.TechPack</RootNamespace>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Data.SqlClient" Version="4.8.6" />
  </ItemGroup>
  <ItemGroup>
    <!-- Platform infra — no LLBLGen in either project -->
    <ProjectReference Include="..\APP.Framework\APP.Framework.Core.csproj" />
    <ProjectReference Include="..\APP.Components.Dto\APP.Components.Dto.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## 7. PluginEntry.cs

```csharp
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Plugin;
using APP.TechPack.Services;

namespace APP.TechPack;

public sealed class PluginEntry : IAppPlugin
{
    public object? Execute(object? input, PluginContext context)
    {
        var formData = input as AppMasterDetailDto
            ?? throw new ArgumentException("Expected AppMasterDetailDto input.");

        return context.MethodName switch
        {
            nameof(ApplyGradeRuleSet)    => ApplyGradeRuleSet(formData, context.ConnectionString),
            nameof(GetGradeRuleCoverage) => GetGradeRuleCoverage(formData, context.ConnectionString),
            _ => throw new InvalidOperationException(
                $"APP.TechPack.PluginEntry: unknown method '{context.MethodName}'.")
        };
    }

    private static OperationCallResult<AppMasterDetailDto> ApplyGradeRuleSet(
        AppMasterDetailDto formData, string connectionString)
    {
        int ruleSetId   = Convert.ToInt32(formData.DictOneToOneFields?["RuleSetId"]);
        int styleSpecId = Convert.ToInt32(formData.DictOneToOneFields?["StyleSpecId"]);
        GradeRuleService.ApplyRuleSetToSpec(connectionString, ruleSetId, styleSpecId);
        return new OperationCallResult<AppMasterDetailDto> { Object = formData };
    }

    private static OperationCallResult<AppMasterDetailDto> GetGradeRuleCoverage(
        AppMasterDetailDto formData, string connectionString)
    {
        int ruleSetId   = Convert.ToInt32(formData.DictOneToOneFields?["RuleSetId"]);
        int styleSpecId = Convert.ToInt32(formData.DictOneToOneFields?["StyleSpecId"]);
        var coverage = GradeRuleService.GetCoverage(connectionString, ruleSetId, styleSpecId);
        formData.DictOneToOneFields ??= new Dictionary<string, object>();
        formData.DictOneToOneFields["MatchedCount"]   = coverage.MatchedSpecLines;
        formData.DictOneToOneFields["TotalCount"]     = coverage.TotalSpecLines;
        formData.DictOneToOneFields["UnmatchedCodes"] = string.Join(", ", coverage.UnmatchedBodyPartCodes);
        return new OperationCallResult<AppMasterDetailDto> { Object = formData };
    }
}
```

Key differences from the original static-method design:
- `sealed class` (not `static class`) — required for `Activator.CreateInstance`
- Implements `IAppPlugin` — enables typed dispatch without raw reflection
- No `GetConnStr()` — connection string arrives via `context.ConnectionString`
- No `ServerContext` or `AppClientIdentity` import — zero platform identity coupling

---

## 8. Connection String Resolution

Connection string flows through `PluginContext`, not through a per-plugin helper:

```
AppExternalMethodRegisterBL.CallExternalMethodMasterDetail
  → AppPluginEngine.Invoke<TResult>(assemblyName, typeName, methodName, input)
      → PluginContext.FromServerContext(methodName)                 [APP.Framework]
          → ServerContext.Instance.CurrnetClientIdentity            [typo preserved]
          → cast (AppClientIdentity?)
          → .CurrentUserDbConnectionString  (plain text at runtime)
      → plugin.Execute(input, context)
          → context.ConnectionString → new SqlConnection(...)
```

Plugins never reference `ServerContext`, `AppClientIdentity`, or any platform identity type. The connection string arrives as a plain `string` in `PluginContext`.

---

## 9. GradeRuleService — ADO.NET Layer

**File:** `APP.TechPack/Services/GradeRuleService.cs`

All public methods take `string connectionString` as the first parameter. This keeps the service purely testable without any platform context.

### ApplyRuleSetToSpec algorithm

```
1. Load TchpGradeRule rows by ruleSetId → List<GradeRuleInput>
2. Load TchpStyleSpec (BaseSizeDetailId, SizeRunId)
3. Load active TchpStyleSpecDimension (IsActive = 1) → DimensionCode
4. Load ordered sizes: TchpSizeRunSize JOIN TchpSizeRunDimension
   WHERE SizeRunId = @sizeRunId AND DimensionCode = @dim ORDER BY SortOrder
5. baseSizeIndex = indexOf(BaseSizeDetailId) in sizes list
6. Load non-fixed POM lines: TchpPomSpecLine JOIN TchpBodyPart WHERE IsFixed = 0
7. For each POM line:
   a. GradingEngine.ApplyGradeRuleSet(rules, bodyPartCode, sizeCount, baseSizeIndex)
   b. MERGE TchpGradeValue (upsert one row per size)
   c. If no matching rule → skip (leave existing deltas)
All upserts in a single SqlTransaction.
```

### Tables accessed

| Table | Operation |
|---|---|
| `TchpGradeRule` | SELECT by GradeRuleSetId |
| `TchpStyleSpec` | SELECT by StyleSpecId |
| `TchpStyleSpecDimension` | SELECT IsActive=1 by StyleSpecId |
| `TchpSizeRunSize` | SELECT via JOIN with TchpSizeRunDimension |
| `TchpSizeRunDimension` | JOIN with TchpSizeRunSize |
| `TchpPomSpecLine` | SELECT non-fixed lines |
| `TchpBodyPart` | JOIN with TchpPomSpecLine |
| `TchpGradeValue` | MERGE (upsert) |

---

## 10. GradingEngine — Pure Math Layer

**Namespace:** `APP.TechPack.Engine`  
**No I/O. No DB. No platform references. Fully unit-tested (15 tests in `APP.BL.Tests`).**

### Delta convention

- Delta at base size = 0 (always)
- Above base: `delta[i] = value[i] − value[i−1]` (positive = size up)
- Below base: `delta[i] = value[i] − value[i+1]` (negative = size down)

### Methods

| Method | Description |
|---|---|
| `ComputeSizeValues(baseValue, baseSizeIndex, deltas)` | Forward pass: deltas + base → full size values |
| `ComputeGradingDeltas(sizeValues, baseSizeIndex)` | Reverse pass: size values → deltas |
| `ApplyGradeRuleSet(rules, bodyPartCode, sizeCount, baseSizeIndex)` | Uniform grade from rule library |
| `ChangeBaseSize(currentBase, currentDeltas, currentIdx, newIdx)` | Rebase without changing absolute measurements |

---

## 11. DB Registration SQL

Run once per tenant after deploying the DLL:

```sql
INSERT INTO AppExternalMethodRegister
    (MethodDisplayName, AssemblyName, TypeName, MethodName, InputParameterList)
VALUES
    ('Apply Grade Rule Set to Spec',
     'APP.TechPack', 'APP.TechPack.PluginEntry', 'ApplyGradeRuleSet',
     'AppMasterDetailDto'),
    ('Get Grade Rule Coverage Preview',
     'APP.TechPack', 'APP.TechPack.PluginEntry', 'GetGradeRuleCoverage',
     'AppMasterDetailDto');
```

`MethodName` is used as `context.MethodName` inside `PluginEntry.Execute` for dispatch. The value must match the `nameof(...)` used in the switch expression.

---

## 12. Build and Deployment

### Build

```bash
# Plugin only — does not require the full platform solution
dotnet build APP.TechPack/APP.TechPack.Core.csproj -c Release
```

Output: `APP.TechPack\bin\Release\net10.0\APP.TechPack.dll`

### Deploy

```
Copy APP.TechPack.dll → [AppAI.Web deploy root]\ExternalDllRepository\APP.TechPack.dll
```

The `ExternalDllRepository\` folder must exist in the web app's base directory.  
No IIS restart required — DLL is loaded on first invocation via `Assembly.LoadFrom`.

### Frontend invocation

```
POST /webapi/apptransaction/CallLinkTargetExternalMethod
Content-Type: application/json

{
  "ExternalMethodRegId": <id from AppExternalMethodRegister>,
  "DictOneToOneFields": {
    "RuleSetId": 1,
    "StyleSpecId": 42
  }
}
```

---

## 13. Writing a New Plugin Module

Any future module follows the same pattern as `APP.TechPack`:

1. **Create a new class library** referencing only `APP.Framework` and `APP.Components.Dto`
2. **Implement `IAppPlugin`** with a parameterless constructor and a method-name switch
3. **Build and drop** the DLL into `ExternalDllRepository\`
4. **Insert rows** into `AppExternalMethodRegister` (one row per operation)

```csharp
// MyModule/MyModuleEntry.cs
public sealed class MyModuleEntry : IAppPlugin
{
    public object? Execute(object? input, PluginContext context)
    {
        var formData = (AppMasterDetailDto)input!;
        return context.MethodName switch
        {
            "DoFoo" => DoFoo(formData, context.ConnectionString),
            "DoBar" => DoBar(formData, context.ConnectionString),
            _       => throw new InvalidOperationException($"Unknown: {context.MethodName}")
        };
    }
}
```

```sql
INSERT INTO AppExternalMethodRegister (MethodDisplayName, AssemblyName, TypeName, MethodName, InputParameterList)
VALUES ('Do Foo', 'MyModule', 'MyModule.MyModuleEntry', 'DoFoo', 'AppMasterDetailDto');
```

---

## 14. Platform Cleanup — Completed

The following incorrect compile-time dependencies were removed:

| Action | File | Status |
|---|---|---|
| Remove ProjectReference to APP.TechPack | `APP.BL/APP.BL.Core.csproj` | ✅ Done |
| Delete bridge file | `APP.BL/POM/GradeRuleApplyBL.cs` | ✅ Done |
| Delete DI helper | `APP.BL/POM/PomServiceExtensions.cs` | ✅ Done |
| Delete unused controller | `AppAI.Web/Controllers/GradingController.cs` | ✅ Done |
| Remove `using APP.BL.POM` + `AddPomServices()` | `AppAI.Web/Program.cs` | ✅ Done |

---

## 15. Verification Checklist — All Passing

- [x] `dotnet build APP.BL/APP.BL.Core.csproj` — zero errors, no APP.TechPack types
- [x] `dotnet build AppAI.Core.sln` — zero errors, zero warnings
- [x] `dotnet test APP.BL.Tests/APP.BL.Tests.csproj` — 15/15 GradingEngineTests pass
- [x] `dotnet build APP.TechPack/APP.TechPack.Core.csproj` — builds against APP.Framework + APP.Components.Dto only
- [ ] Run `POM_Grading_QC_NewSchema.sql` on tenant DB
- [ ] Execute DB registration INSERT
- [ ] `POST /webapi/apptransaction/CallLinkTargetExternalMethod` with valid IDs → rows written to `TchpGradeValue`
