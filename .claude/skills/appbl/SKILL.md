# APP.BL Project Rules

## Adding New C# Files to APP.BL.csproj

When you create a new `.cs` file anywhere inside the `APP.BL` project folder, you **must** add it to `APP.BL\APP.BL.csproj` as a `<Compile Include="..." />` entry inside the existing `<ItemGroup>` that contains the other `<Compile>` entries.

### Rule: DbGenie folder files must be registered in APP.BL.csproj

The active DbGenie files live in `APP.BL\DbGenie\` (NOT `APP.BL\AppMgr\DbGenie\`).
Only `DbGenie\` entries are in the csproj — do NOT add `AppMgr\DbGenie\` entries as they are duplicate classes in the same namespace and will cause build errors.

Any `.cs` file added to `APP.BL\DbGenie\` must be registered in the csproj:

```xml
<Compile Include="DbGenie\YourNewFile.cs" />
```

### General pattern

- Use the **relative path** from the project root (i.e., from `APP.BL\`) as the `Include` value.
- Place the new `<Compile>` entry near other files in the same subfolder for easy navigation.
- This applies to ALL subfolders: `AppMgr\`, `DbGenie\`, `Integration\`, `Email\`, `ThirdPartIT\`, etc.

### Example

After creating `APP.BL\DbGenie\NewHelperBL.cs`, add to `APP.BL.csproj`:

```xml
<Compile Include="DbGenie\NewHelperBL.cs" />
```

## Database Execution Patterns in APP.BL

`DBInteractionBase` does **not** exist in this project. Use `AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId)` instead.

### SELECT queries

```csharp
var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);
var dataTable = fixture.RetriveDataTable(sql, new List<DbParameter>());
```

### Non-SELECT queries (INSERT / UPDATE / DELETE / DDL)

`ExecuteNonQueryResult` returns **void** — do NOT assign to a variable.

```csharp
f
fixture.ExecuteNonQueryResult(sql, new List<DbParameter>());
```

### Creating parameters

`CreateParameter` takes **only one argument** (the parameter name). Set the value on the returned object:

```csharp
var p = fixture.CreateParameter("@SkillId");
p.Value = skillId;
// then add to the list:
new List<DbParameter> { p }
```

Do **NOT** call `fixture.CreateParameter("@Name", value)` — the two-argument overload does not exist.

### Notes
- `dataSourceRegisterId` must be a non-nullable `int` — check `.HasValue` before calling if the source is nullable.
- Add `using System.Data.Common;` for `DbParameter`.

## Getting the Default / Master DataSource

### Master database fixture

Use `AppDataSourceRegisterBL.MasterDataSourceRegisterId` (= `2147483647`) to get a fixture connected to the master (registration) database:

```csharp
var fixture = AppCacheManagerBL.GetOneDatabaseFixture(AppDataSourceRegisterBL.MasterDataSourceRegisterId);
```

> **IMPORTANT**: `AppCacheManagerBL.GetOneDatabaseFixture` is `internal` — only callable from within the `APP.BL` assembly. **Never call it from a WebAPI controller** (different assembly → compile error). Always put fixture logic in a BL class and have the controller call the BL method.

### Looking up the default dataSourceId from web.config

To find the `DataSourceRegisterId` that matches `AppCompanyRegistrationConnectionString`:

```csharp
using System.Data.SqlClient;

var connStr = System.Configuration.ConfigurationManager
    .ConnectionStrings["AppCompanyRegistrationConnectionString"]?.ConnectionString;
var catalog = new SqlConnectionStringBuilder(connStr).InitialCatalog;

var fixture = AppCacheManagerBL.GetOneDatabaseFixture(AppDataSourceRegisterBL.MasterDataSourceRegisterId);
var p = fixture.CreateParameter("@DbName");
p.Value = catalog;
var dt = fixture.RetriveDataTable(
    "SELECT DataSourceRegisterId FROM AppDataSourceRegister WHERE DatabaseName = @DbName",
    new List<DbParameter> { p });
int? defaultId = (dt != null && dt.Rows.Count > 0) ? Convert.ToInt32(dt.Rows[0]["DataSourceRegisterId"]) : (int?)null;
```

Fallback: `AppDataSourceRegisterBL.GetDefaultDataSourceRegId()` returns `ServerContext.Instance.DataSourceId`.

## C# Verbatim String Literals — Escaping Double Quotes

Inside a verbatim string (`@"..."`), a literal `"` character must be written as `""` (two consecutive double quotes). A single `"` ends the string, causing `CS1002: ; expected` parse errors.

```csharp
// WRONG — the inner quotes close and reopen the string, breaking compilation:
private const string Prompt = @"If it returns "Cannot Find Table", use create_application.";

// CORRECT — escape each inner quote as "":
private const string Prompt = @"If it returns ""Cannot Find Table"", use create_application.";
```

This is a common mistake when embedding quoted examples or JSON keys in multi-line `const string` prompts.

## EditableObject.Id is typed as `object`

`EditableObject.Id` (base class of all entity DTOs) has type `object`, not `int`. Assigning it to an `int` field requires an explicit conversion:

```csharp
// WRONG — CS0266: cannot implicitly convert object to int
result.TransactionId = someDto?.Id ?? 0;           // ?. boxes the int → object
result.TransactionId = (int)someDto.Id;             // direct cast works but can throw

// CORRECT — safe conversion, handles any boxed numeric type
result.TransactionId = someDto != null ? Convert.ToInt32(someDto.Id) : 0;
```

The null-conditional `?.` on a value-type property (like `int Id`) boxes the result to `object`, which then cannot be implicitly assigned back to `int`. Always use a regular null check + `Convert.ToInt32()`.

### WebAPI controller must be in AppBuilder.csproj

New WebAPI controller `.cs` files must be added to `PlmApplication\AppBuilder.csproj` — otherwise the controller is silently excluded from compilation and all routes return **404**:

```xml
<Compile Include="Server\WebApi\MyNewController.cs" />
```
