# AppAI Platform: .NET 10 Full Migration Plan

## Context

The AppAI solution (11 projects, all .NET Framework 4.8) must be migrated to .NET 10 to gain async throughput, long-term support, container deployment, and modern library access. This is a **full rewrite**, not an upgrade — `System.Web` (the foundation of `PlmApplication`) was removed in .NET 5, meaning every infrastructure layer must be replaced. The React SPA frontend is **unchanged** and stays as-is.

User decisions:
- APP.LBL/LBLDBSpecific source IS available at `Com.Visual2000.LBL/` — already generated with **LLBLGen Pro 5.13** (Adapter pattern, generated May 2026)
- Keep LLBLGen ORM — already at v5.13; verify NuGet runtime packages support .NET 10 (may need v6.x upgrade)
- Full rewrite approach (not strangler fig / incremental)

**LBL facts confirmed**: Adapter pattern (`DataAccessAdapter : DataAccessAdapterBase`), entity base class = `CommonEntityBase : EntityBase2`, `LinqMetaData : ILinqMetaData` present. The `APP.Components.EntityConverter` stale reference to `SD.LLBLGen.Pro.ORMSupportClasses.NET20 v2.6` has been **removed and build confirmed passing** — project was never actually using it.

---

## Key Challenges

### Critical Blockers
| # | Challenge | Impact | Fix |
|---|-----------|--------|-----|
| C1 | `System.Web` removed entirely in .NET 5+ | All of PlmApplication | ASP.NET Core middleware pipeline |
| C2 | LLBLGen Pro 5.13 runtime DLLs target .NET 4.8 — need .NET 10 NuGet runtime | All BL/converter code | Verify `SD.LLBLGen.Pro.ORMSupportClasses` NuGet 5.13+ supports net10.0; if not, upgrade to LLBLGen Pro 6.x |
| C3 | Web Forms (.aspx) / IHttpHandler (.ashx) removed | 15 pages/handlers | Minimal API endpoints + Razor fallback |
| C4 | ~~VBIDE COM reference in APP.BL~~ | **NOT A BLOCKER** — `OfficeDocumentHelper.cs` is entirely commented out; zero active COM calls found | Remove stale `<COMReference>` from .csproj, no code changes needed |
| C5 | OWIN + SignalR 2.x removed | Real-time agent hub | ASP.NET Core SignalR + new JS client |

### High-Impact Items
| # | Challenge | Fix |
|---|-----------|-----|
| H1 | ServiceStack.Redis v5 (.NET Framework only) | Replace with `StackExchange.Redis` |
| H2 | Microsoft.Exchange.WebServices (deprecated, .NET FX only) | Replace with `Microsoft.Graph` SDK v5+ |
| H3 | Oracle.ManagedDataAccess DLL v4.121.2 | NuGet `Oracle.ManagedDataAccess.Core` 23.x |
| H4 | Hand-rolled SAML + custom session token auth | `Sustainsys.Saml2.AspNetCore` + cookie auth |
| H5 | Entity Framework 6.4 (AppMasterDB migrations) | EF Core 10, re-scaffold DbContext |
| H6 | GrapeCity ActiveReports .ar12 — confirm .NET 10 support | Vendor check before Phase 4 |
| H7 | CodeDom runtime compilation in plmevaluator | Roslyn `CSharpScript.EvaluateAsync<T>` |
| H8 | PayPalCoreSDK 1.7.1 deprecated | `PayPalCheckoutSdk` or Braintree .NET SDK |
| H9 | ActiveUp.Net.Mail DLLs (old .NET 2 mail) | Replace with `MailKit` / `MimeKit` |
| H10 | `HttpContext.Current` static pattern throughout APP.BL | `IHttpContextAccessor` injection |

### Medium Items
- `ConfigurationManager.AppSettings` everywhere → `IConfiguration` (use NuGet shim as bridge in Phases 1-3, inject in Phase 5)
- NLog 4.7.5 → 5.x NuGet
- Newtonsoft.Json 4.0.5 fragments → 13.x NuGet across all projects
- `System.Drawing` in image handlers → `System.Drawing.Common` NuGet (Windows server acceptable)
- **Tesseract OCR replaced by LLM vision API** — use existing configured LLM providers (Claude/GPT-4o/Gemini) via vision API; no Tesseract dependency needed
- `RestSharp` DLL ref → `RestSharp` 110.x NuGet (breaking API change in v107+)
- `BouncyCastle` DLL → `BouncyCastle.Cryptography` NuGet 2.x

---

## Per-Project Migration Summary

| Project | Type | Effort | Phase | Notes |
|---------|------|--------|-------|-------|
| APP.LBL + APP.LBLDBSpecific (`Com.Visual2000.LBL/`) | Retarget + NuGet runtime upgrade | 1–2 wks | 0 | Already at LLBLGen 5.13/Adapter; verify runtime NuGet net10.0 support |
| APP.Components.Dto | Port | 0.5 wks | 1 | Pure POCOs, zero System.Web |
| APP.Framework | Moderate rewrite | 1.5 wks | 1 | Remove System.Web, Redis swap, ServerContext async-safety |
| APP.BL | **Targeted port** | **0.5–1 wk** | 2 | 91% pure business logic, zero COM, System.Web = unused imports only |
| APP.Components.EntityConverter | Port | 0.5 wks | 2 | 258 converters; no LLBLGen API changes since already v5.13 |
| DatabaseSchemaReader | **Port + Optimize** | **3–4 wks** | 3 | `System.Data.SqlClient` blocker, N+1 queries, no caching, DataTable → DataReader |
| Belgrade.SqlClient | Port | 0.25 wks | 3 | Common.Logging → ILogger |
| NJsonSchema (3 projects) | Port (SDK-style) | 0.25 wks | 3 | Just retarget |
| AppEvaluator + CodeDom | Targeted rewrite | 0.75 wks | 3 | CodeDom → Roslyn |
| AppTools | Port + targeted rewrite | 0.5 wks | 3 | EWS → Graph |
| PlmApplication → AppAI.Web | **Full rewrite** | 10–14 wks | 4 | New ASP.NET Core 10 project |

**Total**: ~23–30 dev-weeks (APP.BL targeted port saves 5–7 wks; DatabaseSchemaReader optimization adds 3–4 wks). Team of 3 = 8–10 calendar weeks.

---

## Phase Breakdown

### Phase 0 — LLBLGen Runtime Compatibility Check (Prerequisite Gate)
**Duration**: 1–2 weeks | **Blocks all other phases**

The entity source (`Com.Visual2000.LBL/`) is already at **LLBLGen Pro 5.13** using the **Adapter pattern**. Phase 0 is a compatibility verification, not a full regeneration.

1. **Check `SD.LLBLGen.Pro.ORMSupportClasses` NuGet for net10.0 support.**
   - If v5.13 NuGet supports `net10.0` or `netstandard2.1`: convert `APP.LBL.csproj` and `APP.LBLDBSpecific.csproj` to SDK-style targeting `net10.0`. Add NuGet refs (`SD.LLBLGen.Pro.ORMSupportClasses`, `SD.LLBLGen.Pro.DQE.SqlServer`, `SD.LLBLGen.Pro.DQE.MySql`, `SD.LLBLGen.Pro.DQE.Oracle`).
   - If v5.13 does not support net10.0: upgrade to **LLBLGen Pro 6.x** and re-generate entities from the existing `.llblgenproj` file targeting `net10.0` with the Adapter template. ~2 extra weeks.

2. Convert both `APP.LBL.csproj` + `APP.LBLDBSpecific.csproj` to SDK-style.

3. Remove all `<HintPath>` DLL references to `Libraries/Business/SD.LLBLGen.*`; replace with NuGet PackageReferences.

4. ~~Fix the stale reference in `APP.Components.EntityConverter`~~ — **DONE**. `SD.LLBLGen.Pro.ORMSupportClasses.NET20 v2.6` removed, build passes. Project confirmed not dependent on it.

5. Write one integration test: `DataAccessAdapter` → fetch `AppSecurityUserEntity` by PK from dev DB → must pass.

6. **Key Adapter-pattern async benefit**: LLBLGen 5.x Adapter supports `await adapter.FetchEntityAsync(...)`, `await adapter.SaveEntityAsync(...)`, `await adapter.FetchEntityCollectionAsync(...)`. Flag all synchronous `adapter.FetchEntity` calls in APP.BL for async upgrade in Phase 5.

**Parallel opportunity**: Phase 1 can start immediately alongside Phase 0.

---

### Phase 1 — Foundation Libraries (parallel with Phase 0)
**Duration**: 2–3 weeks | **No dependencies**

#### APP.Components.Dto
- Convert to SDK-style `net10.0` csproj.
- Remove `packages.config`. Update Newtonsoft.Json → 13.x NuGet.
- 200+ DTO/EntityDto classes: zero body changes needed.
- Add `System.Configuration.ConfigurationManager` NuGet shim if any config reads found.

#### APP.Framework
- Convert to SDK-style `net10.0` csproj.
- **Remove `System.Web` reference entirely.** Key changes:
  - `ADHelp.cs` → Add `System.DirectoryServices.AccountManagement` NuGet (Windows); add `#if WINDOWS` guard.
  - `ServerContext.cs` → **Phase 1 fix** (3 changes):
    1. Add static `IHttpContextAccessor` holder + `SetHttpContextAccessor()`. Replace line 156 `HttpContext.Current != null` → `_httpContextAccessor?.HttpContext != null`. Wire in `Program.cs` after `builder.Build()`. No APP.BL changes needed.
    2. **Thread-safety fix**: `WinContext` Dictionary → `ConcurrentDictionary` (plain `Dictionary` is not safe for concurrent async reads+writes).
    3. **Thread-safety fix**: `CompanySettings` setter → `Interlocked.Exchange` or `volatile` (shared mutable state on singleton causes race conditions under concurrent async load).
  - `HttpClientIdentityProvider.ProvideIdentity()` (in PlmApplication) — replace `HttpContext.Current.Request.Headers[...]` with `_httpContextAccessor.HttpContext?.Request.Headers[...]`. **Verify it has no static identity cache** — caching on a singleton causes cross-request identity bleed.
  - **Phase 5 goal**: `IAppClientContext` scoped service. `CompanySettings` and `CurrnetClientIdentity` are per-request data — move them off the singleton to a scoped `HttpAppClientContext` class injected per-request. Migrate BL classes incrementally.
  - `RedisCacheProvider.cs` → Replace all `ServiceStack.Redis` with `StackExchange.Redis` (`IConnectionMultiplexer`). `ICacheProvider` interface stays identical.
  - `CSComplierTools.cs` → Compile-guard with `#if NETFRAMEWORK` for now (migrated in Phase 3).
  - `CssMinifier.cs` / `JsMinifier.cs` → Delete (React owns build pipeline).
  - `DBInteractionBase.cs` → Remove `System.Web.SessionState` references.
- NuGet updates: NLog → 5.x, Newtonsoft.Json → 13.x, add `StackExchange.Redis`, `System.DirectoryServices`, `System.Configuration.ConfigurationManager` shim.
- Remove WPF assembly refs (`PresentationCore`, `WindowsBase`, etc.).

---

### Phase 2 — Business Layer
**Duration**: 1–2 weeks | **Depends on Phase 0 + 1**

Two streams run in parallel:

#### Stream A — APP.Components.EntityConverter (0.5 weeks)
- ~~Remove stale `SD.LLBLGen.Pro.ORMSupportClasses.NET20 v2.6`~~ — **DONE**, build passing.
- Remaining: convert to SDK-style `net10.0` csproj and project-reference `APP.LBL.Core`.
- 258 converters: no logic changes needed (already at LLBLGen v5.13).
- Fix any compile errors at the LLBLGen API boundary: `EntityBase2.Fields[n]` → direct property, `IEntityCore` → `IEntity2`.

#### Stream B — APP.BL (0.5–1 week)

**Finding**: APP.BL does NOT need a rewrite. 208 source files; 91% is pure business logic. Already uses `async`/`await` and `HttpClient` in agent and integration layers.

| Risk | Reality |
|------|---------|
| VBIDE COM interop | `OfficeDocumentHelper.cs` entirely commented out — zero active COM calls |
| System.Web deep coupling | 19 files with `using System.Web` — nearly all are unused imports only |
| LLBLGen API churn | Already at v5.13; just swap DLL HintPaths for NuGet refs |
| No async | Agent and integration layers already use `async`/`await` + `HttpClient` |

**Tasks (~17–27 hours total):**

1. **Convert to SDK-style csproj, target `net10.0`** — remove `packages.config`, add PackageReferences, remove stale `<COMReference>` for VBIDE.

2. **LLBLGen reference swap** — Replace `<HintPath>Libraries/Business/SD.LLBLGen.*` with project-ref `APP.LBL.Core` + NuGet DQE packages.

3. **Remove System.Web imports** — 19 files. Strip unused `using System.Web.*`. Two files with actual usage: `AppPluginClient.cs` (`HttpResponseMessage` → `System.Net.Http`), `AppNextJsAppConfigBL.cs` (`System.Web.Routing` → `Microsoft.AspNetCore.Routing`).

4. **Exchange.WebServices → Microsoft Graph** — `Email/EmailHelper.cs` + `EmailAgentBL.cs`: Replace `ExchangeService` / `EmailMessage` with `GraphServiceClient`. MSAL (`Microsoft.Identity.Client` already present) handles OAuth2.

5. **NuGet DLL swaps** (mechanical):
   - `Oracle.ManagedDataAccess.dll` → `Oracle.ManagedDataAccess.Core` 23.x NuGet
   - `MySql.Data` → `MySqlConnector` 2.x NuGet
   - `DynamicExpresso.Core.dll` → `DynamicExpresso.Core` 2.x NuGet
   - `RestSharp` DLL → `RestSharp` 110.x NuGet (v107+ has breaking API changes — fix call sites)
   - `ActiveUp.Net.Mail` DLLs → `MailKit` + `MimeKit` NuGet
   - `PayPalCoreSDK 1.7.1` → `PayPalCheckoutSdk` NuGet

6. **ServiceStack** — Used in 20 files for serialization (not HTTP infrastructure). ServiceStack v6+ supports .NET 10. Upgrade version; or replace `ServiceStack.Text` usages with `System.Text.Json` / `Newtonsoft.Json` if license cost is a concern.

7. **Firebase / Google APIs / AWS / Stripe / Twilio** — Update all from `packages.config` → PackageReference with current NuGet versions.

8. **ConfigurationManager** — 8 files. Keep NuGet shim for now; mark `// TODO-PHASE5`.

9. **Fix blocking HttpClient calls** — Convert `.GetAsync().Result` / `.PostAsync().Wait()` patterns to `await`. Coordinate with Phase 5 async sweep.

---

### Phase 3 — Supporting Projects
**Duration**: 3–4 weeks | **Depends on Phase 1** | **Mostly parallel**

- **NJsonSchema (3 projects)**: Change target framework. Done.
- **Belgrade.SqlClient**: SDK-style, `net10.0`. Replace `Common.Logging 3.4.1` with `Microsoft.Extensions.Logging.Abstractions` (`ILog` → `ILogger<T>`).
- **AppEvaluator**: Replace `CSharpCodeProvider` + `CompilerParameters` with `Microsoft.CodeAnalysis.CSharp.Scripting` 4.x NuGet. `CSharpScript.EvaluateAsync<T>(expression, options)` is the replacement pattern.
- **CodeDom**: Add `System.CodeDom` NuGet. If used only for code gen (not compilation), keep. If superseded, archive.
- **AppTools**: SDK-style console, `net10.0`. Replace EWS with Microsoft Graph. Replace `app.config` with `appsettings.json` + `Microsoft.Extensions.Configuration.Json`.

---

#### DatabaseSchemaReader — Port + Optimize (3–4 weeks)

**Facts**: 186 C# files, 11+ database providers, factory pattern. Already has async partial classes (`SchemaReader.Async.cs`, `DatabaseFixture.Async.cs`) with `CancellationToken` support. Public API (`DatabaseFixture`, `DatabaseSchema`) stays unchanged.

##### Sub-step 1 — Fix Migration Blockers (Week 1)

1. **`System.Data.SqlClient` → `Microsoft.Data.SqlClient`** — will not compile on .NET 10. Affects `DatabaseFixturePart.cs` + `DatabaseFixturePart.Async.cs`. Add `Microsoft.Data.SqlClient` 5.1.x NuGet; guard with `#if NET6_0_OR_GREATER`.
2. **`System.Data.OracleClient`** — removed in .NET Core. Replace with `Oracle.ManagedDataAccess.Core` 23.x NuGet.
3. **`System.Web` import** — 1 file (`DatabaseFixturePart.cs` line 13). Remove unused import.
4. **csproj** → SDK-style multi-target: `<TargetFrameworks>net48;net10.0</TargetFrameworks>`. Retains .NET 4.8 during transition.
5. **NuGet upgrades**: `System.Text.Json` 7→8.x, `MySql.Data` → `MySqlConnector` 2.x, `Google.Protobuf` → latest.

##### Sub-step 2 — Fix N+1 Query Pattern (Week 1–2)

Current `Table(tableName)` fires **5 separate queries per table**. On a 200-table database = **1,000 round-trips** for a full schema read.

**Fix**: Load all constraint/index metadata in one batch query per type, then join in memory:
```csharp
var allPrimaryKeys = await LoadAllPrimaryKeysAsync(conn);   // 1 query, was N
var allForeignKeys = await LoadAllForeignKeysAsync(conn);   // 1 query, was N
var allIndexes     = await LoadAllIndexesAsync(conn);       // 1 query, was N
// Join to tables by name in memory via Dictionary<tableName, ...>
```
Applies to: `ColumnLoader.cs`, `IndexLoader.cs`, `ConstraintLoader.cs`.

**Result**: Schema read for 200 tables goes from ~1,000 queries to ~10–15 queries. **50–80% reduction in DB round-trips.**

##### Sub-step 3 — Add Schema Caching (Week 2)

Currently zero caching — every `ReadAll()` re-queries the database.

```csharp
public interface ISchemaCache
{
    bool TryGet(string cacheKey, out DatabaseSchema schema);
    void Set(string cacheKey, DatabaseSchema schema, TimeSpan ttl);
    void Invalidate(string cacheKey);
}
```
- `MemorySchemaCache` — `IMemoryCache` backed, default TTL 5 min
- Cache key = `SHA256(connectionString + providerName)` (never store raw connection string in key)
- Injected into `DatabaseFixture` as optional parameter — **fully backward-compatible**

**Result**: Repeated schema reads (e.g., DbaGenie AI agent re-reading schema on each query) cost near-zero after first load.

##### Sub-step 4 — Replace DataTable with DataReader in Internal Pipeline (Week 2–3)

106+ `DataTable` / `foreach (DataRow row in dt.Rows)` patterns materialize entire results into memory before processing.

Replace hand-built queries in `Conversion/SchemaConverter.cs` and provider readers with streaming `DbDataReader`:
```csharp
// Before: fills entire DataTable into RAM
DataTable dt = new DataTable(); adapter.Fill(dt);
foreach (DataRow row in dt.Rows) { ... }

// After: streams row-by-row, no full materialization
await using var reader = await cmd.ExecuteReaderAsync(ct);
while (await reader.ReadAsync(ct))
    yield return new DatabaseColumn { Name = reader.GetString(0), ... };
```
Keep `DataTable` only where ADO.NET `GetSchema()` API returns it (no reader equivalent exists).

**Result**: **20–40% memory reduction** on large schemas.

##### Sub-step 5 — IAsyncEnumerable Streaming (Week 3)

Add streaming overloads for large databases (1000+ tables). Additive — does not break existing API:
```csharp
public IAsyncEnumerable<DatabaseTable> StreamTablesAsync(CancellationToken ct = default)
```
DbaGenie and AppBuilderAgent schema analysis callers can iterate tables as they arrive rather than waiting for all 1000 to load.

##### Sub-step 6 — Merge Sync/Async Partial Classes (Week 3–4)

Merge `SchemaReader.cs` + `SchemaReader.Async.cs` (and similar pairs) into single files. Mark old sync public methods `[Obsolete("Use async overload")]` wrapping `GetAwaiter().GetResult()`. Reduces ~8 file pairs.

##### Optimization Summary

| Optimization | Impact | Effort |
|---|---|---|
| Fix `System.Data.SqlClient` | Compiles on .NET 10 | 4 hrs |
| Batch N+1 constraint queries | **50–80% fewer DB round-trips** | 3 days |
| Schema caching (`ISchemaCache`) | **Near-zero cost on repeated reads** | 2 days |
| DataTable → DataReader streaming | **20–40% memory reduction** | 1 week |
| `IAsyncEnumerable<DatabaseTable>` | Scalable for 1000+ table schemas | 3 days |
| Merge partial async classes | Cleaner, fewer files | 2 days |

---

### Phase 4 — New Web Host: AppAI.Web (Full Rewrite)
**Duration**: 10–14 weeks | **Depends on Phase 1+2**

Create a brand-new project `AppAI.Web/AppAI.Web.csproj` (SDK-style, `net10.0`, `Microsoft.NET.Sdk.Web`). The old `AppBuilder.csproj` is never touched — it remains compilable as production fallback.

#### New Project Layout
```
AppAI.Web/
  Program.cs                  ← Global.asax + WebApiConfig + StartupSignalR + Web.config
  appsettings.json            ← all Web.config <appSettings>, connection strings
  Middleware/                 ← replaces HttpModules
  Controllers/                ← all 35 existing controllers, ported
    Base/SecureBaseController.cs  ← replaces SecureBaseWebApiController
  Hubs/AppBuilderAgentHub.cs  ← ASP.NET Core SignalR
  Endpoints/                  ← replaces all .aspx and .ashx
  Auth/                       ← cookie auth + SAML
  wwwroot/                    ← React SPA build output
```

#### Program.cs (replaces Global.asax + WebApiConfig + StartupSignalR + Web.config)
```csharp
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddSaml2(/* Sustainsys.Saml2 config */);
builder.Services.AddDistributedMemoryCache(); // or AddStackExchangeRedisCache()
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
// LLBLGen v5 startup, GemBox licenses from IConfiguration

var app = builder.Build();
ServerContext.SetHttpContextAccessor(app.Services.GetRequiredService<IHttpContextAccessor>());

app.UseAuthentication(); app.UseAuthorization(); app.UseSession();
app.MapControllers();
app.MapHub<AppBuilderAgentHub>("/signalr/apphub");
app.MapFallbackToFile("index.html");  // SPA shell
// Minimal API endpoints for .aspx/.ashx replacements (see table below)
```

#### Controllers (35 total) — Mechanical Port
Pattern per controller:
- `using System.Web.Http` → `using Microsoft.AspNetCore.Mvc`
- `ApiController` → `ControllerBase` + `[ApiController]`
- `[RoutePrefix("webapi/X")]` → `[Route("webapi/X")]`
- `HttpContext.Current` → `HttpContext` (built into `ControllerBase`)
- `Initialize()` override → `[ServiceFilter(typeof(SessionValidationFilter))]`
- Return types: wrap existing POCOs in `Ok()` / `BadRequest()`

#### Authentication Middleware
```csharp
// SessionValidationFilter replaces SecureBaseWebApiController.Initialize()
public class SessionValidationFilter : IActionFilter {
    public void OnActionExecuting(ActionExecutingContext context) {
        var sessionId = context.HttpContext.Request.Headers["CurrentUserSessionId"]
                        .FirstOrDefault()
                     ?? context.HttpContext.Request.Cookies["CurrentUserSessionId"];
        if (string.IsNullOrEmpty(sessionId)) { context.Result = new UnauthorizedResult(); return; }
        AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity(sessionId);
    }
}
```

#### ServerContext — Async Safety

`ServerContext` is a singleton with two async-safety issues that are harmless on .NET Framework's synchronous pipeline but will cause problems under .NET 10's concurrent async model:

| Issue | Risk | Fix |
|---|---|---|
| `HttpContext.Current` (line 156) | Null after `await` on different thread | Replace with `_httpContextAccessor?.HttpContext` |
| `WinContext = new Dictionary<>()` | Race condition on concurrent reads+writes | Replace with `ConcurrentDictionary` |
| `CompanySettings { get; set; }` on singleton | Cross-request data corruption | `Interlocked.Exchange` or move to scoped service |

`IHttpContextAccessor` uses `AsyncLocal<HttpContext>` internally — it flows correctly through `await` continuations, so it is the correct .NET 10 equivalent of `HttpContext.Current`.

#### SAML Migration
Replace `SAMLModels.cs` / `AuthRequest` / `SAMLResponse` / `LogoutRequest` / `LogoutResponse` with `Sustainsys.Saml2.AspNetCore`:
- `SAMLEndPoint`, `SAMLIssuer`, `SAMLCertificate` keys → `Saml2Options`
- `SAMLAuthenticationCallBackUrl` → `SPOptions.ReturnUrl`

#### SignalR Hub
- Change `using Microsoft.AspNet.SignalR` → `using Microsoft.AspNetCore.SignalR`
- `Hub` base class name is the same (different namespace); `Clients.Client(id).SendAsync("method", data)` unchanged.
- React frontend: replace old jQuery-based SignalR proxy with `@microsoft/signalr` npm package:
  - Old: `$.connection.appBuilderAgentHub.client.agentStep = fn`
  - New: `connection.on("agentStep", fn)` + `connection.start()`

#### .aspx / .ashx → Minimal API Endpoints

| Legacy File | Replacement Endpoint | Notes |
|-------------|---------------------|-------|
| `Default.aspx` | `MapFallbackToFile("index.html")` | React SPA shell |
| `DataImage.ashx` | `POST /api/files/upload` | LLM vision OCR (replaces Tesseract), image resize — largest handler |
| `AppCalendarIcs.ashx` | `GET /api/calendar/{id}.ics` | iCal export |
| `AppResourceHandler.ashx` | `GET /api/resources/{*path}` | Resource serving |
| `ExportExcel.aspx` | `GET /api/export/excel` | GemBox.Spreadsheet |
| `GetRegularImage.aspx` | `GET /api/files/image/{id}` | Image serving |
| `GetThumbnailImage.aspx` | `GET /api/files/thumbnail/{id}` | Thumbnail |
| `GetLatestFile.aspx` | `GET /api/files/latest/{id}` | Latest file |
| `RequestByteStream.aspx` | `GET /api/files/stream/{id}` | Byte stream |
| `QRDisplay.aspx` | `GET /api/qr/{data}` | QR code |
| `AppScript.aspx` | `POST /api/admin/script` | Admin SQL; require admin role |
| `AppServerSource.aspx` | `GET /api/server/source` | JS source |
| `AppServerWakeUp.aspx` | `GET /api/server/warmup` | Health/warmup |
| `AudioRecordAndPlay.aspx` | `GET /audio` (Razor Page) | Audio UI |

`DataImage.ashx` is the most complex (~500 lines). Port to `DataImageEndpoint.cs` using `IFormFile` instead of `HttpPostedFile`.

#### OCR: Replace Tesseract with LLM Vision API

Remove the `Tesseract 3.3.0 DLL` entirely. The app already has three LLM providers configured (`DbaGenieAnthropicModel`, `DbaGenieGeminiModel`, `DbaGenieOpenAIModel` in `appsettings.json`). Use the same provider infrastructure for OCR:

**`IOcrService` interface** (new, in APP.BL or `AppAI.Web/Services/`):
```csharp
public interface IOcrService
{
    Task<string> ExtractTextAsync(string base64Image, string mediaType,
                                   CancellationToken ct = default);
}
```
- Implementation selects provider from config (`DbaGenieLLMProvider` = `Anthropic` / `OpenAI` / `Gemini`)
- Prompt: `"Extract all text from this image. Return only the extracted text, preserving layout where possible."`
- No native binaries, no extra NuGet packages, far higher accuracy than Tesseract

#### EF Core Migration (AppMasterDB)
- Create new `AppDbContext : DbContext` (EF Core 10).
- `dotnet ef dbcontext scaffold "..." Microsoft.EntityFrameworkCore.SqlServer` from dev DB.
- Generate new initial migration. Replace `using System.Data.Entity` → `using Microsoft.EntityFrameworkCore`.

#### ActiveReports
**Action before coding**: Contact GrapeCity to confirm ActiveReports 16+ NuGet supports .NET 10 and existing license is upgradeable. If blocked, isolate report generation to a .NET 4.8 sidecar microservice called via HTTP from AppAI.Web.

---

### Phase 5 — Hardening & Async Completion
**Duration**: 4–6 weeks | **Depends on Phase 4 functionally running**

- **IConfiguration injection sweep** — Replace all `// TODO-PHASE5` `ConfigurationManager.AppSettings` calls with `IOptions<T>` pattern.
- **`IAppClientContext` scoped service** — Introduce `IAppClientContext` interface mirroring `ServerContext.Instance.*` properties. Implement `HttpAppClientContext : IAppClientContext` (scoped, injected `IHttpContextAccessor`, per-request identity cache). Register `services.AddScoped<IAppClientContext, HttpAppClientContext>()`. Migrate BL classes incrementally by injecting `IAppClientContext` via constructor. The Phase 1 static `ServerContext` shim coexists until all callers are migrated.
- **Async sweep** — Add `async Task<IActionResult>` to all 35 controllers. `await` all LLBLGen Adapter calls (`FetchEntityAsync`, `SaveEntityAsync`), file I/O, and HTTP calls.
- **Redis session** — `services.AddStackExchangeRedisCache()` for distributed session in production.
- **Health checks** — `builder.Services.AddHealthChecks()` with DB + Redis probes at `/health`.
- **Security hardening**:
  - Remove `ServicePointManager.SecurityProtocol` (TLS 1.2+ is the .NET 10 default).
  - Add `app.UseHsts()` in production.
  - Move license keys / API keys / connection strings to environment variables or Azure Key Vault.
  - Rotate `AppConnectionStringEncryptionKey`.
- **Grep audit**:
  - Zero `System.Web` references in entire solution
  - Zero `ConfigurationManager` calls without `IConfiguration`
  - Zero `HttpContext.Current` static accesses

---

## New Solution Structure (net10.0)

```
AppAI.sln
├── Foundation/
│   ├── APP.LBL.Core/                  ← LLBLGen v5.13 entities (Adapter pattern)
│   ├── APP.LBLDBSpecific.Core/        ← LLBLGen v5.13 DB-specific layer
│   ├── APP.Components.Dto/            ← pure DTOs, no System.Web
│   └── APP.Framework/                 ← no System.Web, StackExchange.Redis
├── BusinessLayer/
│   ├── APP.BL/                        ← targeted port, async-ready
│   └── APP.Components.EntityConverter/
├── Infrastructure/
│   ├── DatabaseSchemaReader/          ← optimized: caching, batching, streaming
│   ├── Belgrade.SqlClient/
│   └── NJsonSchema/ (3 projects)
├── Evaluator/
│   ├── AppEvaluator/                  ← Roslyn-based
│   └── CodeDom/
├── Web/
│   └── AppAI.Web/                     ← ASP.NET Core 10 (new project)
└── Tools/
    └── AppTools/
```

---

## Risk Register

| # | Risk | Probability | Impact | Mitigation |
|---|------|-------------|--------|------------|
| R1 | LLBLGen Pro 5.13 NuGet runtime may not yet support `net10.0` | Medium | High | Check `SD.LLBLGen.Pro.ORMSupportClasses` NuGet target frameworks first. If net10.0 is missing, upgrade to LLBLGen Pro 6.x (entity source available; regeneration from existing `.llblgenproj` adds ~2 weeks). Fallback: EF Core 10 (+4–6 wks). |
| R2 | ~~VBIDE COM scope~~ | **RESOLVED** — `OfficeDocumentHelper.cs` entirely commented out; zero active COM calls. Remove stale `<COMReference>` only. | N/A |
| R3 | GrapeCity ActiveReports .NET 10 compatibility | Medium | High | Contact vendor in Phase 0. If unsupported: isolate reports to a .NET 4.8 sidecar HTTP service. |
| R4 | Custom session-token auth embedded in 35 controllers | Medium | High | Keep session token pattern in Phase 4 via `IActionFilter`. Cookie auth + SAML are additive. Defer JWT to post-cutover. |
| R5 | React SignalR client scope | Low | Medium | Audit all `$.connection.*` calls. If limited to AppBuilderAgentHub: 1–2 days. If broader: +1 week. |

---

## Build Branch Strategy

1. Create branch `feature/dotnet10-migration` from `main`.
2. New projects use `*.Core` suffix or live in `AppAI.Web/`. Old `.csproj` files are never modified.
3. Create `AppAI.Core.sln` referencing only the new projects.
4. CI: `dotnet build AppAI.Core.sln -c Release` on every PR to `feature/dotnet10-migration`.
5. Cutover: `AppAI.Web` replaces `PlmApplication` in production after Phase 4 verification. Old `AppBuilder.csproj` moves to `archive/` — retained 6 months post-cutover.

---

## Verification Per Phase

| Phase | Gate |
|-------|------|
| 0 | `dotnet test APP.LBL.Core.Tests` — one `DataAccessAdapter` entity fetch from dev DB passes |
| 1 | `dotnet build APP.Framework.csproj -f net10.0` — zero errors, zero `System.Web` in IL |
| 2 | `dotnet build APP.BL.csproj APP.Components.EntityConverter.csproj` — zero errors; one auth + one transaction integration test passes; zero COM refs |
| 3 | All supporting projects build on net10.0; `AppEvaluator`: `CSharpScript.EvaluateAsync<int>("1+1")` = 2; DatabaseSchemaReader N+1 benchmark shows ≥50% query reduction |
| 4 | AppAI.Web starts → `/health` 200 → login returns session cookie → one API controller returns data → SignalR WebSocket connects → file upload stores file → one report generates |
| 5 | Full regression suite passes; k6 load test 50 users P95 < 500ms; zero `System.Web` / `HttpContext.Current` / `ConfigurationManager` in grep audit |
