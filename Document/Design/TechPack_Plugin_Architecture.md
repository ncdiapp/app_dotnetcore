# APP.TechPack — Plugin Module Architecture Design

**Module:** POM / Grading / QC (TechPack)  
**Date:** 2026-06-16  
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
┌─────────────────────────────────────────────────────────┐
│  PLATFORM (AppAI.Core.sln) — no knowledge of plugins    │
│                                                          │
│  AppAI.Web ──→ APP.BL ──→ APP.Framework                 │
│                       └──→ APP.Components.Dto            │
│                       └──→ LLBL (LLBLGen ORM)           │
│                                                          │
│  AppExternalMethodRegisterBL (in APP.BL)                 │
│    runtime: Assembly.LoadFrom(ExternalDllRepository\)    │
│    → reflection invoke                                   │
└──────────────────────┬──────────────────────────────────┘
                       │ runtime only (Assembly.LoadFrom)
                       ▼
┌─────────────────────────────────────────────────────────┐
│  APP.TechPack.dll  (plugin, lives in ExternalDllRepository\)
│                                                          │
│  APP.TechPack ──→ APP.Framework       (no LLBLGen ✅)   │
│               ──→ APP.Components.Dto  (no LLBLGen ✅)   │
│               ──→ System.Data.SqlClient                  │
│                                                          │
│  PluginEntry       ← registered entry points            │
│  GradeRuleService  ← pure ADO.NET (SqlConnection)       │
│  GradingEngine     ← pure math, no I/O                  │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Platform Plugin Infrastructure

### 4.1 How the platform loads plugins

**File:** `APP.BL/TenantBusiness/AppExternalMethodRegisterBL.cs`

```csharp
public static OperationCallResult<AppMasterDetailDto> CallExternalMethodMasterDetail(
    object methodRegisterId, object[] paramter)
{
    var dto = RetrieveOneAppExternalMethodRegisterEntity(methodRegisterId);

    string pathToDomain = ExternalDllRepository + dto.AssemblyName + ".dll";
    Assembly domainAssembly = Assembly.LoadFrom(pathToDomain);
    Type type = domainAssembly.GetType(dto.TypeName);

    return type.GetMethod(dto.MethodName).Invoke(null, paramter)
               as OperationCallResult<AppMasterDetailDto>;
}

private static readonly string ExternalDllRepository =
    AppDomain.CurrentDomain.BaseDirectory + @"ExternalDllRepository\";
```

### 4.2 AppExternalMethodRegister table

| Column | Type | Description |
|---|---|---|
| `MethodRegisterId` | INT PK | Auto-identity |
| `MethodDisplayName` | NVARCHAR(100) | UI label |
| `AssemblyName` | NVARCHAR(500) | DLL filename without `.dll` |
| `TypeName` | NVARCHAR(500) | Fully-qualified class name |
| `MethodName` | NVARCHAR(500) | Static method name |
| `InputParameterList` | NVARCHAR(500) | Pipe-delimited parameter descriptor |

### 4.3 Existing API endpoint (no new controller needed)

```
POST /webapi/apptransaction/CallLinkTargetExternalMethod
```

The platform's `AppTransactionController` already handles this. The request body is `AppMasterDetailDto` with `ExternalMethodRegId` set.

---

## 5. Plugin Contract

Every method registered in `AppExternalMethodRegister` must be a **public static method** with this signature:

```csharp
public static OperationCallResult<AppMasterDetailDto> MethodName(AppMasterDetailDto formData)
```

Input values are passed via `formData.DictOneToOneFields` (Dictionary\<string, object\>).  
Output values are written back into the same dictionary before returning.

---

## 6. Project Structure

```
APP.TechPack/
├── APP.TechPack.Core.csproj          ← standalone build; refs APP.Framework + APP.Components.Dto
├── PluginEntry.cs                     ← registered entry points (platform contract)
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

## 7. PluginEntry.cs — Platform Entry Points

```csharp
using System;
using System.Collections.Generic;
using APP.Components.EntityDto;       // AppMasterDetailDto
using APP.Components.Dto;             // AppClientIdentity
using APP.Framework;                  // ServerContext
using APP.Framework.Communication;   // OperationCallResult<T>
using APP.TechPack.Services;

namespace APP.TechPack;

public static class PluginEntry
{
    /// <summary>
    /// Applies a named grade rule set to every non-fixed POM spec line.
    /// Registered as: AssemblyName=APP.TechPack, TypeName=APP.TechPack.PluginEntry,
    ///                MethodName=ApplyGradeRuleSet
    /// </summary>
    public static OperationCallResult<AppMasterDetailDto> ApplyGradeRuleSet(
        AppMasterDetailDto formData)
    {
        int ruleSetId   = Convert.ToInt32(formData.DictOneToOneFields?["RuleSetId"]);
        int styleSpecId = Convert.ToInt32(formData.DictOneToOneFields?["StyleSpecId"]);

        GradeRuleService.ApplyRuleSetToSpec(GetConnStr(), ruleSetId, styleSpecId);

        return new OperationCallResult<AppMasterDetailDto> { Object = formData };
    }

    /// <summary>
    /// Returns coverage: which body parts have a matching rule in the rule set.
    /// Registered as: AssemblyName=APP.TechPack, TypeName=APP.TechPack.PluginEntry,
    ///                MethodName=GetGradeRuleCoverage
    /// </summary>
    public static OperationCallResult<AppMasterDetailDto> GetGradeRuleCoverage(
        AppMasterDetailDto formData)
    {
        int ruleSetId   = Convert.ToInt32(formData.DictOneToOneFields?["RuleSetId"]);
        int styleSpecId = Convert.ToInt32(formData.DictOneToOneFields?["StyleSpecId"]);

        var coverage = GradeRuleService.GetCoverage(GetConnStr(), ruleSetId, styleSpecId);

        formData.DictOneToOneFields ??= new Dictionary<string, object>();
        formData.DictOneToOneFields["MatchedCount"]   = coverage.MatchedSpecLines;
        formData.DictOneToOneFields["TotalCount"]     = coverage.TotalSpecLines;
        formData.DictOneToOneFields["UnmatchedCodes"] = string.Join(", ", coverage.UnmatchedBodyPartCodes);

        return new OperationCallResult<AppMasterDetailDto> { Object = formData };
    }

    // Connection string is already plain text in AppClientIdentity at runtime.
    // AppTenantAdapterBL.GetTenantAdapter() passes it directly to DataAccessAdapter
    // without decryption — confirming it is plain at this point.
    private static string GetConnStr()
    {
        var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
        return identity?.CurrentUserDbConnectionString ?? string.Empty;
    }
}
```

---

## 8. Connection String Resolution

The plugin resolves the tenant connection string from `ServerContext` — the same pattern used by `AppTenantAdapterBL.GetTenantAdapter()` in the platform core.

```
ServerContext.Instance.CurrnetClientIdentity   (note typo: one 'r' in Currnet — preserved)
  → cast to (AppClientIdentity?)
  → .CurrentUserDbConnectionString             (plain text at runtime)
  → passed to new SqlConnection(connStr)
```

`ServerContext` (in `APP.Framework`) is a singleton already loaded in the AppDomain when the plugin runs inside AppAI.Web. No additional infrastructure is needed.

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
**No I/O. No DB. No platform references. Fully unit-tested (15 tests).**

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

---

## 12. Build and Deployment

### Build

```bash
# From the repo root — builds only the plugin, not the full platform
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

## 13. Platform Cleanup Required

The following incorrect compile-time dependencies were introduced and must be removed:

| Action | File | Reason |
|---|---|---|
| Remove ProjectReference to APP.TechPack | `APP.BL/APP.BL.Core.csproj` | Platform core must not know plugin |
| Delete | `APP.BL/POM/GradeRuleApplyBL.cs` | Was a bridge that imported APP.TechPack.Services |
| Delete | `APP.BL/POM/PomServiceExtensions.cs` | Was a DI helper that imported APP.TechPack.Engine |
| Delete | `AppAI.Web/Controllers/GradingController.cs` | Not needed — existing endpoint handles dispatch |
| Edit | `AppAI.Web/Program.cs` | Remove `using APP.BL.POM` + `AddPomServices()` call |

After cleanup:
- `AppAI.Web.csproj` — **unchanged**
- `AppAI.Core.sln` — **unchanged** (APP.TechPack can stay in the solution for dev IDE support, but is not referenced by platform projects)

---

## 14. Verification Checklist

- [ ] `dotnet build APP.BL/APP.BL.Core.csproj` in isolation — zero errors, no APP.TechPack types
- [ ] `dotnet build AppAI.Core.sln` — zero errors, zero warnings
- [ ] `dotnet test APP.BL.Tests/APP.BL.Tests.csproj` — all 15 GradingEngineTests pass
- [ ] `dotnet build APP.TechPack/APP.TechPack.Core.csproj` — builds against APP.Framework + APP.Components.Dto only
- [ ] Run `POM_Grading_QC_NewSchema.sql` on tenant DB
- [ ] Execute DB registration INSERT
- [ ] `POST /webapi/apptransaction/CallLinkTargetExternalMethod` with valid IDs → rows written to `TchpGradeValue`
